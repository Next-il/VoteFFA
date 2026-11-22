using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using VotesApi;

namespace VoteFFA;

public class Config : BasePluginConfig
{
	[JsonPropertyName("Delay")]
	public int Delay { get; set; } = 25; // Delay between votes

	[JsonPropertyName("VoteDuration")]
	public int VoteDuration { get; set; } = 30; // Vote duration

	// Passes when yes reaches this share of everyone who can vote, and beats no - the rule every Votes vote uses.
	[JsonPropertyName("PassPercent")]
	public float PassPercent { get; set; } = 51f;
};

[MinimumApiVersion(333)]
public partial class VoteFFAPlugin : BasePlugin, IPluginConfig<Config>
{
	public override string ModuleName => "VoteFFA";
	public override string ModuleVersion => "1.1.0";
	public override string ModuleAuthor => "ShiNxz";
	public Config Config { get; set; } = new Config();

	internal static IStringLocalizer? Stringlocalizer;

	public int DelayBetweenVotes = 25;
	public int VoteDuration = 30;
	public int LastVoteTime = 0;

	public bool isVoteActive = false;
	public bool IsFFAActive = false;

	/// <summary>
	/// Next-il/Votes, looked up on every use because it can load after this plugin. Votes owns the game's vote
	/// controller, the sv_allow_votes window and the vote card; this plugin only says what to ask.
	/// </summary>
	private static IVotesApi? Votes
	{
		get
		{
			try { return IVotesApi.Capability.Get(); }
			catch { return null; }
		}
	}

	public void OnConfigParsed(Config config)
	{
		Config = config;
		DelayBetweenVotes = config.Delay;
		VoteDuration = config.VoteDuration;
	}

	public override void Load(bool hotReload)
	{
		base.Load(hotReload);
		Stringlocalizer = Localizer;
	}

	[ConsoleCommand("css_ffa", "Start an FFA vote")]
	public void OnVoteCommand(CCSPlayerController? caller, CommandInfo command)
	{
		if (caller is not { IsValid: true }) return;

		if (isVoteActive || (Votes?.IsVoteActive ?? false))
		{
			Helper.AdvancedPrintToChat(caller, Localizer["vote.already-in-progress"]);
			return;
		}

		// Check if there is a delay between votes
		int now = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
		if (LastVoteTime + DelayBetweenVotes > now)
		{
			Helper.AdvancedPrintToChat(caller, Localizer["vote.delay", LastVoteTime + DelayBetweenVotes - now]);
			return;
		}

		StartFFAVote(caller);
	}

	public void StartFFAVote(CCSPlayerController player)
	{
		IVotesApi? votes = Votes;
		if (votes == null)
		{
			Logger.LogError("Votes is not loaded: the FFA vote needs Next-il/Votes (plugins/Votes + shared/VotesApi).");
			Helper.AdvancedPrintToChat(player, Text("vote.unavailable", "Votes are not available right now."));
			return;
		}

		bool disabling = IsFFAActive;
		Logger.LogInformation("Starting FFA vote by {Player} (FFA active: {Active})", player.PlayerName, disabling ? "yes" : "no");

		isVoteActive = true;
		bool started = votes.StartYesNo(new YesNoVote
		{
			Owner = ModuleName,
			Caller = player,
			Eyebrow = Text("vote.eyebrow", "Vote · FFA"),
			Prefix = Localizer[$"vote.{(disabling ? "disable" : "enable")}.label"].Value.Trim(),
			Subject = "FFA",
			DurationSeconds = VoteDuration,
			PassPercent = Config.PassPercent,
			PassedText = disabling ? Text("vote.disable.card", "normal rounds next round") : Text("vote.enable.card", "FFA from next round"),
			OnEnd = result => OnVoteEnded(result, disabling),
		});

		if (!started)
		{
			isVoteActive = false;
			Helper.AdvancedPrintToChat(player, Localizer["vote.already-in-progress"]);
		}
	}

	private void OnVoteEnded(VoteResult result, bool disabling)
	{
		isVoteActive = false;

		// Unix timestamp
		LastVoteTime = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

		Logger.LogInformation("FFA vote ended ({Reason}): {Yes} yes, {No} no of {Voters} - {Outcome}",
			result.Reason, result.Tally.Yes, result.Tally.No, result.Tally.Voters, result.Passed ? "passed" : "failed");

		if (result.Passed)
		{
			// Set from the state the vote asked about, not flipped, so it can never toggle the wrong way.
			IsFFAActive = !disabling;
			Helper.PrintToChatAll(Localizer[disabling ? "vote.disable.success" : "vote.enable.success"]);
			return;
		}

		if (result.Reason != VoteEndReason.Cancelled)
			Helper.PrintToChatAll(Localizer["vote.failed"]);
	}

	[GameEventHandler]
	public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
	{
		// Start the FFA
		Server.ExecuteCommand($"mp_teammates_are_enemies {(IsFFAActive ? 1 : 0)}");
		if (IsFFAActive) Helper.PrintToChatAll(Localizer[$"ffa.state.enabled"]);

		return HookResult.Continue;
	}

	/// <summary>A lang key, or the English default when the server's lang file does not have it yet.</summary>
	private string Text(string key, string fallback)
	{
		var value = Localizer[key];
		return value.ResourceNotFound ? fallback : value.Value;
	}
}
