using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Localization;
using PanoramaVote;
using System.Text.Json.Serialization;

namespace VoteFFA;

public class Config : BasePluginConfig
{
	[JsonPropertyName("Delay")]
	public int Delay { get; set; } = 25; // Delay between votes

	[JsonPropertyName("VoteDuration")]
	public int VoteDuration { get; set; } = 30; // Vote duration
};

[MinimumApiVersion(333)]
public partial class VoteFFAPlugin : BasePlugin, IPluginConfig<Config>
{
	public override string ModuleName => "VoteFFA";
	public override string ModuleVersion => "1.0.0";
	public override string ModuleAuthor => "ShiNxz";
	public Config Config { get; set; } = new Config();

	internal static IStringLocalizer? Stringlocalizer;

	public int DelayBetweenVotes = 25;
	public int VoteDuration = 30;
	public int LastVoteTime = 0;

	public bool isVoteActive = false;
	public bool IsFFAActive = false;

	private CPanoramaVote _panoramaVote = null!;

	public void OnConfigParsed(Config config)
	{
		DelayBetweenVotes = config.Delay;
		VoteDuration = config.VoteDuration;
	}

	public override void Load(bool hotReload)
	{
		base.Load(hotReload);
		Stringlocalizer = Localizer;

		_panoramaVote = new CPanoramaVote(this);
		RegisterEventHandler<EventVoteCast>((@event, info) =>
		{
			_panoramaVote.VoteCast(@event);
			return HookResult.Continue;
		});
	}

	[ConsoleCommand("css_ffa", "Start an FFA vote")]
	public void OnVoteCommand(CCSPlayerController caller, CommandInfo command)
	{
		if (isVoteActive || _panoramaVote.IsVoteInProgress())
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
		Console.WriteLine($"Starting FFA vote by {player.PlayerName} (FFA active: {(IsFFAActive ? "yes" : "no")})");

		ToggleVotes(true);
		isVoteActive = true;

		string details = Localizer[$"vote.{(IsFFAActive ? "disable" : "enable")}.title"];
		string hintLabel = Localizer[$"vote.{(IsFFAActive ? "disable" : "enable")}.label"];

		Server.NextFrame(() =>
		{
			_panoramaVote.Init();
			if (!_panoramaVote.SendYesNoVoteToAll(
				VoteDuration,
				VoteConstants.VOTE_CALLER_SERVER,
				VoteConstants.SFUI_Vote_None,
				details,
				VoteResultCallback,
				VoteHandlerCallback,
				hintPrefix: hintLabel,
				hintRoundName: "FFA",
				hintSuffix: "?"))
			{
				ToggleVotes(false);
				isVoteActive = false;
			}
		});
	}

	public bool VoteResultCallback(YesNoVoteInfo info)
	{
		if (info.yes_votes > info.no_votes)
		{
			if (IsFFAActive)
			{
				Helper.PrintToChatAll(Localizer["vote.disable.success"]);
				IsFFAActive = false;
			}
			else
			{
				Helper.PrintToChatAll(Localizer["vote.enable.success"]);
				IsFFAActive = true;
			}

			return true;
		}

		Helper.PrintToChatAll(Localizer["vote.failed"]);
		return false;
	}

	public void VoteHandlerCallback(YesNoVoteAction action, int param1, int param2)
	{
		switch (action)
		{
			case YesNoVoteAction.VoteAction_Start:
				isVoteActive = true;
				Console.WriteLine("FFA Vote started!");
				break;

			case YesNoVoteAction.VoteAction_Vote:
				break;

			case YesNoVoteAction.VoteAction_End:
				Console.WriteLine($"FFA Vote ended (reason: {param1})");

				ToggleVotes(false);
				isVoteActive = false;

				// Unix timestamp
				LastVoteTime = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
				break;
		}
	}

	[GameEventHandler]
	public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
	{
		// Start the FFA
		Server.ExecuteCommand($"mp_teammates_are_enemies {(IsFFAActive ? 1 : 0)}");
		if (IsFFAActive) Helper.PrintToChatAll(Localizer[$"ffa.state.enabled"]);

		return HookResult.Continue;
	}

	public static void ToggleVotes(bool allow = false)
	{
		int option = allow ? 1 : 0;

		Server.ExecuteCommand($"sv_allow_votes {option}");
		Server.ExecuteCommand($"sv_vote_allow_in_warmup {option}");
		Server.ExecuteCommand($"sv_vote_allow_spectators {option}");
		Server.ExecuteCommand($"sv_vote_count_spectator_votes {option}");
	}
}
