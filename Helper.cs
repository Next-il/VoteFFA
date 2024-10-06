using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using System.Text.RegularExpressions;

namespace VoteFFA;

public class Helper
{
	public static void AdvancedPrintToChat(CCSPlayerController player, string message, params object[] args)
	{
		for (int i = 0; i < args.Length; i++)
		{
			message = message.Replace($"{{{i}}}", args[i].ToString());
		}

		if (Regex.IsMatch(message, "{nextline}", RegexOptions.IgnoreCase))
		{
			string[] parts = Regex.Split(message, "{nextline}", RegexOptions.IgnoreCase);
			foreach (string part in parts)
			{
				string messages = part.Trim();
				player.PrintToChat(" " + VoteFFAPlugin.Stringlocalizer!["prefix"] + " " + messages);
			}
		}
		else
		{
			player.PrintToChat($"{VoteFFAPlugin.Stringlocalizer!["prefix"]} {message}");
		}
	}

	public static void PrintToChatAll(string message)
	{
		Server.PrintToChatAll($" {VoteFFAPlugin.Stringlocalizer!["prefix"]} {message}");
	}
}
