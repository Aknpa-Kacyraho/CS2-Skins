using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WeaponPaints
{
	internal static class Utility
	{
		internal static WeaponPaintsConfig? Config { get; set; }

		internal static bool IsPlayerValid(CCSPlayerController? player)
		{
			if (player is null || WeaponPaints.WeaponSync is null) return false;

			return player is { IsValid: true, IsBot: false, IsHLTV: false, UserId: not null };
		}

		internal static void LoadSkinsFromFile(string filePath, ILogger logger)
		{
			try
			{
				if (!File.Exists(filePath))
				{
					logger?.LogWarning("Skins data file not found: {path}", filePath);
					return;
				}
				var json = File.ReadAllText(filePath);
				var deserializedSkins = JsonConvert.DeserializeObject<List<JObject>>(json);
				WeaponPaints.SkinsList = deserializedSkins ?? [];
			}
			catch (Exception ex)
			{
				logger?.LogError("Failed to load skins file: {msg}", ex.Message);
			}
		}

		internal static void LoadGlovesFromFile(string filePath, ILogger logger)
		{
			try
			{
				if (!File.Exists(filePath))
				{
					logger?.LogWarning("Gloves data file not found: {path}", filePath);
					return;
				}
				var json = File.ReadAllText(filePath);
				var deserializedSkins = JsonConvert.DeserializeObject<List<JObject>>(json);
				WeaponPaints.GlovesList = deserializedSkins ?? [];
			}
			catch (Exception ex)
			{
				logger?.LogError("Failed to load gloves file: {msg}", ex.Message);
			}
		}

		internal static void Log(string message)
		{
			Console.WriteLine("[WeaponPaints] " + message);
		}

		/// <summary>
		/// Create a CSS built-in ChatMenu. No external MenuManager dependency needed.
		/// </summary>
		internal static ChatMenu CreateMenu(string title)
		{
			return new ChatMenu(title);
		}
	}
}
