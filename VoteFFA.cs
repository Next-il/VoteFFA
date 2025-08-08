using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CS2MenuManager.API.Class;
using CS2MenuManager.API.Enum;
using CS2MenuManager.API.Menu;
using Microsoft.Extensions.Localization;
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

	public int DelayBetweenVotes = 60;
	public int VoteDuration = 30;
	public int LastVoteTime = 0;

	public bool isVoteActive = false;
	public bool IsFFAActive = false;

	public Dictionary<string, int> voteData = [];
	public List<CCSPlayerController> VotedPlayers = [];

	public void OnConfigParsed(Config config)
	{
		DelayBetweenVotes = config.Delay;
		VoteDuration = config.VoteDuration;
	}

	public override void Load(bool hotReload)
	{
		base.Load(hotReload);
		Stringlocalizer = Localizer;
	}

	[ConsoleCommand("css_ffa", "Start an FFA vote")]
	public void OnVoteCommand(CCSPlayerController caller, CommandInfo command)
	{
		if (isVoteActive)
		{
			Helper.AdvancedPrintToChat(caller, Localizer["vote.already-in-progress"]);
			return;
		}

		// Check if there is a delay between votes
		if (LastVoteTime + DelayBetweenVotes > (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds)
		{
			Helper.AdvancedPrintToChat(caller, Localizer["vote.delay", LastVoteTime + DelayBetweenVotes - (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds]);
			return;
		}

		StartFFAVote(caller);
	}

	public void StartFFAVote(CCSPlayerController player)
	{
		Console.WriteLine($"Starting FFA vote by {player.PlayerName} (FFA active: {(IsFFAActive ? "yes" : "no")})");
		ToggleVotes(true);
		var menu = new PanoramaVote("#SFUI_vote_panorama_vote_default", Localizer[$"vote.{(IsFFAActive ? "disable" : "enable")}.title"], VoteResultCallback, VoteHandlerCallback, this)
		{
			// VoteCaller = player
		};

		menu.DisplayVoteToAll(20);
	}

	public bool VoteResultCallback(YesNoVoteInfo info)
	{
		/*
		public int TotalVotes;
		public int YesVotes;
		public int NoVotes;
		public int TotalClients;
		public Dictionary<int, (int, int)> ClientInfo = [];
		*/

		if (info.YesVotes > info.NoVotes)
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
		else
		{
			Helper.PrintToChatAll(Localizer["vote.failed"]);
		}

		Server.PrintToChatAll("Vote failed!");
		return false;
	}

	public void VoteHandlerCallback(YesNoVoteAction action, int param1, CastVote param2)
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
				Console.WriteLine($"FFA Vote ended with {param1} votes (Yes: {param2 == CastVote.VOTE_OPTION1})");

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
