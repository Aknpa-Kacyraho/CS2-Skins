using System.Collections.Concurrent;
using CounterStrikeSharp.API.Modules.Utils;

namespace WeaponPaints;

internal class WeaponSynchronization
{
	private readonly WeaponPaintsConfig _config;
	private readonly Storage _storage;

	internal WeaponSynchronization(Storage storage, WeaponPaintsConfig config)
	{
		_storage = storage;
		_config = config;
	}

	/// <summary>
	/// Load all player data from JSON storage into in-memory dictionaries.
	/// Called on player connect and hot reload.
	/// </summary>
	internal void GetPlayerData(PlayerInfo? player)
	{
		if (player == null || string.IsNullOrEmpty(player.SteamId))
			return;

		try
		{
			var data = _storage.GetPlayerData(player.SteamId);
			if (data == null) return;

			if (_config.Additional.KnifeEnabled)
				LoadKnifeFromStorage(player, data);
			if (_config.Additional.GloveEnabled)
				LoadGloveFromStorage(player, data);
			if (_config.Additional.SkinEnabled)
				LoadSkinsFromStorage(player, data);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[WeaponPaints] Error loading player data: {ex.Message}");
		}
	}

	private void LoadKnifeFromStorage(PlayerInfo player, PlayerData data)
	{
		if (data.Knife.Count == 0) return;

		var playerKnives = WeaponPaints.GPlayersKnife.GetOrAdd(player.Slot, _ => new ConcurrentDictionary<CsTeam, string>());

		foreach (var (teamNum, knife) in data.Knife)
		{
			if (string.IsNullOrEmpty(knife)) continue;

			CsTeam weaponTeam = teamNum switch
			{
				2 => CsTeam.Terrorist,
				3 => CsTeam.CounterTerrorist,
				_ => CsTeam.None,
			};

			if (weaponTeam == CsTeam.None)
			{
				playerKnives[CsTeam.Terrorist] = knife;
				playerKnives[CsTeam.CounterTerrorist] = knife;
			}
			else
			{
				playerKnives[weaponTeam] = knife;
			}
		}
	}

	private void LoadGloveFromStorage(PlayerInfo player, PlayerData data)
	{
		if (data.Gloves.Count == 0) return;

		var playerGloves = WeaponPaints.GPlayersGlove.GetOrAdd(player.Slot, _ => new ConcurrentDictionary<CsTeam, ushort>());

		foreach (var (teamNum, gloveId) in data.Gloves)
		{
			CsTeam weaponTeam = teamNum switch
			{
				2 => CsTeam.Terrorist,
				3 => CsTeam.CounterTerrorist,
				_ => CsTeam.None,
			};

			if (weaponTeam == CsTeam.None)
			{
				playerGloves[CsTeam.Terrorist] = gloveId;
				playerGloves[CsTeam.CounterTerrorist] = gloveId;
			}
			else
			{
				playerGloves[weaponTeam] = gloveId;
			}
		}
	}

	private void LoadSkinsFromStorage(PlayerInfo player, PlayerData data)
	{
		if (data.Skins.Count == 0) return;

		var playerWeapons = WeaponPaints.GPlayerWeaponsInfo.GetOrAdd(player.Slot,
			_ => new ConcurrentDictionary<CsTeam, ConcurrentDictionary<int, WeaponInfo>>());

		foreach (var (teamNum, weaponDict) in data.Skins)
		{
			CsTeam weaponTeam = teamNum switch
			{
				2 => CsTeam.Terrorist,
				3 => CsTeam.CounterTerrorist,
				_ => CsTeam.None,
			};

			var teamWeapons = new ConcurrentDictionary<int, WeaponInfo>();
			foreach (var (defIndex, weaponInfo) in weaponDict)
			{
				teamWeapons[defIndex] = weaponInfo;
			}

			if (weaponTeam == CsTeam.None)
			{
				playerWeapons[CsTeam.Terrorist] = teamWeapons;
				playerWeapons[CsTeam.CounterTerrorist] = teamWeapons;
			}
			else
			{
				playerWeapons[weaponTeam] = teamWeapons;
			}
		}
	}

	/// <summary>
	/// Collect current knife data from memory and save to JSON storage.
	/// </summary>
	internal void SyncKnifeToDatabase(PlayerInfo player, string knife, CsTeam[] teams)
	{
		if (!_config.Additional.KnifeEnabled || string.IsNullOrEmpty(player.SteamId) || string.IsNullOrEmpty(knife) || teams.Length == 0)
			return;

		try
		{
			var data = _storage.GetPlayerData(player.SteamId) ?? new PlayerData();
			foreach (var team in teams)
			{
				data.Knife[(int)team] = knife;
			}
			_storage.SavePlayerData(player.SteamId, data);
		}
		catch (Exception e)
		{
			Utility.Log($"Error syncing knife: {e.Message}");
		}
	}

	/// <summary>
	/// Collect current glove data from memory and save to JSON storage.
	/// </summary>
	internal void SyncGloveToDatabase(PlayerInfo player, ushort gloveDefIndex, CsTeam[] teams)
	{
		if (!_config.Additional.GloveEnabled || string.IsNullOrEmpty(player.SteamId) || teams.Length == 0)
			return;

		try
		{
			var data = _storage.GetPlayerData(player.SteamId) ?? new PlayerData();
			foreach (var team in teams)
			{
				data.Gloves[(int)team] = gloveDefIndex;
			}
			_storage.SavePlayerData(player.SteamId, data);
		}
		catch (Exception e)
		{
			Utility.Log($"Error syncing glove: {e.Message}");
		}
	}

	/// <summary>
	/// Collect current weapon skin data from memory and save to JSON storage.
	/// </summary>
	internal void SyncWeaponPaintsToDatabase(PlayerInfo player)
	{
		if (string.IsNullOrEmpty(player.SteamId) || !WeaponPaints.GPlayerWeaponsInfo.TryGetValue(player.Slot, out var teamWeaponInfos))
			return;

		try
		{
			var data = _storage.GetPlayerData(player.SteamId) ?? new PlayerData();

			foreach (var (team, weaponsInfo) in teamWeaponInfos)
			{
				var teamDict = new Dictionary<int, WeaponInfo>();
				foreach (var (weaponDefIndex, weaponInfo) in weaponsInfo)
				{
					teamDict[weaponDefIndex] = weaponInfo;
				}
				data.Skins[(int)team] = teamDict;
			}

			_storage.SavePlayerData(player.SteamId, data);
		}
		catch (Exception e)
		{
			Utility.Log($"Error syncing weapon paints: {e.Message}");
		}
	}

	/// <summary>
	/// Sync StatTrak counts from memory to JSON storage.
	/// </summary>
	internal void SyncStatTrakToDatabase(PlayerInfo player)
	{
		if (WeaponPaints.WeaponSync == null || WeaponPaints.GPlayerWeaponsInfo.IsEmpty) return;
		if (string.IsNullOrEmpty(player.SteamId)) return;

		// StatTrak is already part of WeaponInfo stored in GPlayerWeaponsInfo,
		// so SyncWeaponPaintsToDatabase covers it. No separate sync needed.
		SyncWeaponPaintsToDatabase(player);
	}
}
