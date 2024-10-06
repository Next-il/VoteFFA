using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
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

[MinimumApiVersion(244)]
public partial class VoteFFAPlugin : BasePlugin, IPluginConfig<Config>
{
	public override string ModuleName => "VoteFFA";
	public override string ModuleVersion => "1.0.0";
	public override string ModuleAuthor => "ShiNxz";
	public Config Config { get; set; } = new Config();

	internal static IStringLocalizer? Stringlocalizer;

	public int DelayBetweenVotes = 25;
	public int VoteDuration = 30;

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

		RegisterListener<Listeners.OnTick>(() =>
		{
			if (isVoteActive)
			{
				int choice = 1;

				string message = $"<font color='White'>{Localizer[$"vote.{(IsFFAActive ? "disable" : "enable")}.title"]}</font>";

				foreach (KeyValuePair<string, int> vote in voteData)
				{
					message += $"<br>!<font color='#a5feff'>{choice}</font> " + vote.Key + $" <font color='#a5feff'>[{vote.Value}]</font>";
					choice++;
				}

				Utilities.GetPlayers().Where(p => p is { IsValid: true, IsBot: false, IsHLTV: false }).ToList().ForEach(player =>
				{
					player.PrintToCenterHtml(message);
				});

				// Check if all players have voted
				if (VotedPlayers.Count == Utilities.GetPlayers().Where(p => p is { IsValid: true, IsBot: false, IsHLTV: false }).Count())
				{
					FinishVote();
				}
			}
		});
	}

	[ConsoleCommand("css_ffa", "Start an FFA vote")]
	public void OnVoteCommand(CCSPlayerController caller, CommandInfo command)
	{
		if (isVoteActive)
		{
			Helper.AdvancedPrintToChat(caller, Localizer["vote.already-in-progress"]);
			return;
		}

		StartFFAVote();
	}

	public void StartFFAVote()
	{
		string[] options = [Localizer[$"vote.answers.yes"], Localizer[$"vote.answers.no"]];

		foreach (var item in options)
		{
			voteData.Add(item, 0);
		}

		float duration = VoteDuration;
		isVoteActive = true;

		ChatMenu voteMenu = new($" {ChatColors.Green}=-=-=-=-= {ChatColors.Lime}{Localizer[$"vote.{(IsFFAActive ? "disable" : "enable")}.title"]} {ChatColors.Green}=-=-=-=-=");

		foreach (var item in options)
		{
			voteMenu.AddMenuOption(item, (x, i) =>
			{
				if (VotedPlayers.Contains(x))
				{
					Helper.AdvancedPrintToChat(x, Localizer["player.already-voted"]);
				}

				Helper.AdvancedPrintToChat(x, Localizer["player.vote-success", item]);
				AddVote(item);
				VotedPlayers.Add(x);
				MenuManager.CloseActiveMenu(x);
			});
		}

		Utilities.GetPlayers().Where(p => p is { IsValid: true, IsBot: false, IsHLTV: false }).ToList().ForEach(player =>
		{
			MenuManager.CloseActiveMenu(player);
			MenuManager.OpenChatMenu(player, voteMenu);
		});

		AddTimer(duration, () =>
		{
			FinishVote();
		});
	}

	void FinishVote()
	{
		Helper.PrintToChatAll($" {ChatColors.Green}=-=-=-=-= {ChatColors.Lime} {Localizer[$"vote.{(IsFFAActive ? "disable" : "enable")}.title"]} {ChatColors.Green}=-=-=-=-=");

		string winner = voteData.OrderByDescending(x => x.Value).First().Key;

		if (winner == Localizer[$"vote.answers.yes"] && !IsFFAActive)
		{
			IsFFAActive = true;
			Helper.PrintToChatAll(Localizer["vote.success"]);
		}
		else if (winner == Localizer[$"vote.answers.no"] && IsFFAActive)
		{
			IsFFAActive = false;
			Helper.PrintToChatAll(Localizer["vote.success"]);
		}
		else
		{
			Helper.PrintToChatAll(Localizer["vote.failed"]);
		}

		VotedPlayers.Clear();
		voteData.Clear();
		isVoteActive = false;
	}

	void AddVote(string candidate)
	{
		if (voteData.TryGetValue(candidate, out int value))
		{
			voteData[candidate] = ++value;
		}
		else
		{
			voteData[candidate] = 1;
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
}
