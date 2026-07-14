using System.Collections.Concurrent;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;

namespace WeaponPaints;

public partial class WeaponPaints
{
	private void OnCommandRefresh(CCSPlayerController? player, CommandInfo command)
	{
		if (!Config.Additional.CommandWpEnabled || !Config.Additional.SkinEnabled || !_gBCommandsAllowed) return;
		if (!Utility.IsPlayerValid(player)) return;

		if (player == null || !player.IsValid || player.UserId == null || player.IsBot) return;

		PlayerInfo? playerInfo = new PlayerInfo
		{
			UserId = player.UserId,
			Slot = player.Slot,
			Index = (int)player.Index,
			SteamId = player?.SteamID.ToString(),
			Name = player?.PlayerName,
			IpAddress = player?.IpAddress?.Split(":")[0]
		};

		try
		{
			if (player != null && !CommandsCooldown.TryGetValue(player.Slot, out var cooldownEndTime) ||
				player != null && DateTime.UtcNow >= (CommandsCooldown.TryGetValue(player.Slot, out cooldownEndTime) ? cooldownEndTime : DateTime.UtcNow))
			{
				CommandsCooldown[player.Slot] = DateTime.UtcNow.AddSeconds(Config.CmdRefreshCooldownSeconds);

				if (WeaponSync != null)
				{
					WeaponSync.GetPlayerData(playerInfo);
				}

				GivePlayerGloves(player);
				RefreshWeapons(player);

				if (!string.IsNullOrEmpty(Localizer["wp_command_refresh_done"]))
				{
					player!.Print(Localizer["wp_command_refresh_done"]);
				}
				return;
			}
			if (!string.IsNullOrEmpty(Localizer["wp_command_cooldown"]))
			{
				player!.Print(Localizer["wp_command_cooldown"]);
			}
		}
		catch (Exception) { }
	}

	private void OnCommandWS(CCSPlayerController? player, CommandInfo command)
	{
		if (!Config.Additional.SkinEnabled) return;
		if (!Utility.IsPlayerValid(player)) return;

		if (!string.IsNullOrEmpty(Localizer["wp_info_refresh"]))
		{
			player!.Print(Localizer["wp_info_refresh"]);
		}

		if (Config.Additional.GloveEnabled)
			if (!string.IsNullOrEmpty(Localizer["wp_info_glove"]))
			{
				player!.Print(Localizer["wp_info_glove"]);
			}

		if (!Config.Additional.KnifeEnabled) return;
		if (!string.IsNullOrEmpty(Localizer["wp_info_knife"]))
		{
			player!.Print(Localizer["wp_info_knife"]);
		}
	}

	private void RegisterCommands()
	{
		_config.Additional.CommandStattrak.ForEach(c =>
		{
			AddCommand($"css_{c}", "Stattrak toggle", (player, info) =>
			{
				if (!Utility.IsPlayerValid(player)) return;

				OnCommandStattrak(player, info);
			});
		});

		_config.Additional.CommandSkin.ForEach(c =>
		{
			AddCommand($"css_{c}", "Skins info", (player, info) =>
			{
				if (!Utility.IsPlayerValid(player)) return;
				OnCommandWS(player, info);
			});
		});

		_config.Additional.CommandRefresh.ForEach(c =>
		{
			AddCommand($"css_{c}", "Skins refresh", (player, info) =>
			{
				if (!Utility.IsPlayerValid(player)) return;
				OnCommandRefresh(player, info);
			});
		});

		if (Config.Additional.CommandKillEnabled)
		{
			_config.Additional.CommandKill.ForEach(c =>
			{
				AddCommand($"css_{c}", "kill yourself", (player, _) =>
				{
					if (player == null || !Utility.IsPlayerValid(player) || player.PlayerPawn.Value == null || !player.PlayerPawn.IsValid) return;

					player.PlayerPawn.Value.CommitSuicide(true, false);
				});
			});
		}

		AddCommand("wp_refresh", "Admin refresh player skins", (player, info) =>
		{
			OnCommandSkinRefresh(player, info);
		});
	}

	private void OnCommandSkinRefresh(CCSPlayerController? player, CommandInfo command)
	{
		if (!Config.Additional.CommandWpEnabled || !Config.Additional.SkinEnabled || !_gBCommandsAllowed) return;
		if (player != null)
		{
			return;
		}

		var args = command.GetArg(1);

		if (string.IsNullOrEmpty(args))
		{
			Console.WriteLine("[WeaponPaints] Usage: wp_refresh <steamid64|all>");
			Console.WriteLine("[WeaponPaints] Examples:");
			Console.WriteLine("[WeaponPaints]   wp_refresh all - Refresh skins for all players");
			Console.WriteLine("[WeaponPaints]   wp_refresh 76561198012345678 - Refresh skins by SteamID64");
			return;
		}

		var targetPlayers = new List<CCSPlayerController>();

		if (args.Equals("all", StringComparison.OrdinalIgnoreCase))
		{
			targetPlayers = Utilities.GetPlayers().Where(p =>
				p != null && p.IsValid && !p.IsBot && p.UserId != null).ToList();

			if (targetPlayers.Count == 0)
			{
				Console.WriteLine("[WeaponPaints] No players connected to refresh.");
				return;
			}

			Console.WriteLine($"[WeaponPaints] Refreshing skins for {targetPlayers.Count} players...");
		}
		else
		{
			var foundPlayer = Utilities.GetPlayers().FirstOrDefault(p =>
				p != null && p.IsValid && !p.IsBot && p.UserId != null &&
				 p.SteamID.ToString() == args);

			if (foundPlayer == null)
			{
				Console.WriteLine($"[WeaponPaints] Player with SteamID64 '{args}' not found.");
				return;
			}

			targetPlayers.Add(foundPlayer);
			Console.WriteLine($"[WeaponPaints] Refreshing skins for {foundPlayer.PlayerName}...");
		}

		foreach (var targetPlayer in targetPlayers)
		{
			try
			{
				PlayerInfo? playerInfo = new PlayerInfo
				{
					UserId = targetPlayer.UserId,
					Slot = targetPlayer.Slot,
					Index = (int)targetPlayer.Index,
					SteamId = targetPlayer.SteamID.ToString(),
					Name = targetPlayer.PlayerName,
					IpAddress = targetPlayer.IpAddress?.Split(":")[0]
				};

				if (WeaponSync != null)
				{
					WeaponSync.GetPlayerData(playerInfo);
				}

				GivePlayerGloves(targetPlayer);
				RefreshWeapons(targetPlayer);

				if (!string.IsNullOrEmpty(Localizer["wp_command_refresh_done"]))
				{
					targetPlayer.Print(Localizer["wp_command_refresh_done"]);
				}

				Console.WriteLine($"[WeaponPaints] Skins refreshed for {targetPlayer.PlayerName}");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[WeaponPaints] Error refreshing skins for {targetPlayer.PlayerName}: {ex.Message}");
			}
		}

		Console.WriteLine("[WeaponPaints] Refresh process completed.");
	}


	private void OnCommandStattrak(CCSPlayerController? player, CommandInfo commandInfo)
	{
		if (player == null || !player.IsValid) return;

		var weapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;

		if (weapon == null || !weapon.IsValid)
			return;

		if (!HasChangedPaint(player, weapon.AttributeManager.Item.ItemDefinitionIndex, out var weaponInfo) || weaponInfo == null)
			return;

		weaponInfo.StatTrak = !weaponInfo.StatTrak;
		RefreshWeapons(player);

		if (!string.IsNullOrEmpty(Localizer["wp_stattrak_action"]))
		{
			player.Print(Localizer["wp_stattrak_action"]);
		}
	}

	private void SetupKnifeMenu()
	{
		if (!Config.Additional.KnifeEnabled || !_gBCommandsAllowed) return;

		var knivesOnly = WeaponList
			.Where(pair => pair.Key.StartsWith("weapon_knife") || pair.Key.StartsWith("weapon_bayonet"))
			.ToDictionary(pair => pair.Key, pair => pair.Value);

		var giveItemMenu = Utility.CreateMenu(Localizer["wp_knife_menu_title"]);

		var handleGive = (CCSPlayerController player, ChatMenuOption option) =>
		{
			if (!Utility.IsPlayerValid(player)) return;

			var playerKnives = GPlayersKnife.GetOrAdd(player.Slot, new ConcurrentDictionary<CsTeam, string>());
			var teamsToCheck = player.TeamNum < 2
				? new[] { CsTeam.Terrorist, CsTeam.CounterTerrorist }
				: [player.Team];

			var knifeName = option.Text;
		var knifeKey = knivesOnly.FirstOrDefault(x => x.Value == knifeName).Key;
			if (string.IsNullOrEmpty(knifeKey)) return;
			if (!string.IsNullOrEmpty(Localizer["wp_knife_menu_select"]))
			{
				player.Print(Localizer["wp_knife_menu_select", knifeName]);
			}

			if (!string.IsNullOrEmpty(Localizer["wp_knife_menu_kill"]) && Config.Additional.CommandKillEnabled)
			{
				player.Print(Localizer["wp_knife_menu_kill"]);
			}

			PlayerInfo playerInfo = new PlayerInfo
			{
				UserId = player.UserId,
				Slot = player.Slot,
				Index = (int)player.Index,
				SteamId = player.SteamID.ToString(),
				Name = player.PlayerName,
				IpAddress = player.IpAddress?.Split(":")[0]
			};

			foreach (var team in teamsToCheck)
			{
				playerKnives[team] = knifeKey;
			}

			if (_gBCommandsAllowed && (LifeState_t)player.LifeState == LifeState_t.LIFE_ALIVE)
				RefreshWeapons(player);

			if (WeaponSync != null)
				WeaponSync.SyncKnifeToDatabase(playerInfo, knifeKey, teamsToCheck);
		};
		foreach (var knifePair in knivesOnly)
		{
			giveItemMenu?.AddMenuOption(knifePair.Value, handleGive);
		}
		_config.Additional.CommandKnife.ForEach(c =>
		{
			AddCommand($"css_{c}", "Knife Menu", (player, _) =>
			{
				if (giveItemMenu == null) return;
				if (!Utility.IsPlayerValid(player) || !_gBCommandsAllowed) return;

				if (player == null || player.UserId == null) return;

				if (!CommandsCooldown.TryGetValue(player.Slot, out var cooldownEndTime) ||
					DateTime.UtcNow >= (CommandsCooldown.TryGetValue(player.Slot, out cooldownEndTime) ? cooldownEndTime : DateTime.UtcNow))
				{
					CommandsCooldown[player.Slot] = DateTime.UtcNow.AddSeconds(Config.CmdRefreshCooldownSeconds);
					giveItemMenu.PostSelectAction = PostSelectAction.Close;

					giveItemMenu.Open(player);

					return;
				}
				if (!string.IsNullOrEmpty(Localizer["wp_command_cooldown"]))
				{
					player.Print(Localizer["wp_command_cooldown"]);
				}
			});
		});
	}

	private void SetupSkinsMenu()
	{
		var classNamesByWeapon = WeaponList.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

		var weaponSelectionMenu = Utility.CreateMenu(Localizer["wp_skin_menu_weapon_title"]);

		var handleWeaponSelection = (CCSPlayerController? player, ChatMenuOption option) =>
		{
			if (!Utility.IsPlayerValid(player)) return;

			var selectedWeapon = option.Text;

			if (!classNamesByWeapon.TryGetValue(selectedWeapon, out var selectedWeaponClassname)) return;
			var skinsForSelectedWeapon = SkinsList.Where(skin =>
				skin.TryGetValue("weapon_name", out var weaponName) &&
				weaponName?.ToString() == selectedWeaponClassname
			)?.ToList();

			var skinSubMenu = Utility.CreateMenu(Localizer["wp_skin_menu_skin_title", selectedWeapon]);

			var handleSkinSelection = (CCSPlayerController p, ChatMenuOption opt) =>
			{
				if (!Utility.IsPlayerValid(p)) return;

				var firstSkin = SkinsList.FirstOrDefault(skin =>
				{
					if (skin.TryGetValue("weapon_name", out var weaponName))
						return weaponName?.ToString() == selectedWeaponClassname;
					return false;
				});

				var selectedSkin = opt.Text;
				var selectedPaintId = selectedSkin[(selectedSkin.LastIndexOf('(') + 1)..].Trim(')');

				if (firstSkin == null ||
					!firstSkin.TryGetValue("weapon_defindex", out var weaponDefIndexObj) ||
					!int.TryParse(weaponDefIndexObj.ToString(), out var weaponDefIndex) ||
					!int.TryParse(selectedPaintId, out var paintId)) return;

				p.Print(Localizer["wp_skin_menu_select", selectedSkin]);
				var playerSkins = GPlayerWeaponsInfo.GetOrAdd(p.Slot, new ConcurrentDictionary<CsTeam, ConcurrentDictionary<int, WeaponInfo>>());

				// Always apply skin to both teams so CT/T share the same skin
				var teamsToCheck = new[] { CsTeam.Terrorist, CsTeam.CounterTerrorist };

				foreach (var team in teamsToCheck)
				{
					var teamWeapons = playerSkins.GetOrAdd(team, _ => new ConcurrentDictionary<int, WeaponInfo>());
					var value = teamWeapons.GetOrAdd(weaponDefIndex, _ => new WeaponInfo());
					value.Paint = paintId;
					value.Wear = 0.01f;
					value.Seed = 0;
				}

				var playerInfo = new PlayerInfo
				{
					UserId = p.UserId,
					Slot = p.Slot,
					Index = (int)p.Index,
					SteamId = p.SteamID.ToString(),
					Name = p.PlayerName,
					IpAddress = p.IpAddress?.Split(":")[0]
				};

				// Save to JSON immediately (even if dead — data persists)
				if (WeaponSync != null)
					WeaponSync.SyncWeaponPaintsToDatabase(playerInfo);

				// Apply the skin in-game if alive
				if (_gBCommandsAllowed && (LifeState_t)p.LifeState == LifeState_t.LIFE_ALIVE)
					RefreshWeapons(p);
			};

			if (skinsForSelectedWeapon != null)
			{
				foreach (var skin in skinsForSelectedWeapon)
				{
					if (!skin.TryGetValue("paint_name", out var paintNameObj) ||
						!skin.TryGetValue("paint", out var paintObj)) continue;
					var paintName = paintNameObj?.ToString();
					var paint = paintObj?.ToString();

					if (!string.IsNullOrEmpty(paintName) && !string.IsNullOrEmpty(paint))
					{
						skinSubMenu?.AddMenuOption($"{paintName} ({paint})", handleSkinSelection);
					}
				}
			}
			if (player != null && Utility.IsPlayerValid(player))
				skinSubMenu?.Open(player);
		};

		foreach (var weaponName in WeaponList
				.Select(kvp => kvp.Value))
		{
			weaponSelectionMenu?.AddMenuOption(weaponName, handleWeaponSelection);
		}

		_config.Additional.CommandSkinSelection.ForEach(c =>
		{
			AddCommand($"css_{c}", "Skins selection menu", (player, _) =>
			{
				if (!Utility.IsPlayerValid(player)) return;

				if (player == null || player.UserId == null) return;

				if (!CommandsCooldown.TryGetValue(player.Slot, out var cooldownEndTime) ||
					DateTime.UtcNow >= (CommandsCooldown.TryGetValue(player.Slot, out cooldownEndTime) ? cooldownEndTime : DateTime.UtcNow))
				{
					CommandsCooldown[player.Slot] = DateTime.UtcNow.AddSeconds(Config.CmdRefreshCooldownSeconds);
					weaponSelectionMenu?.Open(player);
					return;
				}
				if (!string.IsNullOrEmpty(Localizer["wp_command_cooldown"]))
				{
					player.Print(Localizer["wp_command_cooldown"]);
				}
			});
		});
	}

	private void SetupGlovesMenu()
	{
		var glovesSelectionMenu = Utility.CreateMenu(Localizer["wp_glove_menu_title"]);
		if (glovesSelectionMenu == null) return;
		glovesSelectionMenu.PostSelectAction = PostSelectAction.Close;

		var handleGloveSelection = (CCSPlayerController? player, ChatMenuOption option) =>
		{
			if (!Utility.IsPlayerValid(player) || player is null) return;

			var selectedPaintName = option.Text;

			var playerGloves = GPlayersGlove.GetOrAdd(player.Slot, new ConcurrentDictionary<CsTeam, ushort>());
			var teamsToCheck = player.TeamNum < 2
				? new[] { CsTeam.Terrorist, CsTeam.CounterTerrorist }
				: [player.Team];

			var selectedGlove = GlovesList.FirstOrDefault(g => g.ContainsKey("paint_name") && g["paint_name"]?.ToString() == selectedPaintName);
			if (selectedGlove == null ||
				!selectedGlove.ContainsKey("weapon_defindex") ||
				!selectedGlove.ContainsKey("paint") ||
				!int.TryParse(selectedGlove["weapon_defindex"]?.ToString(), out var weaponDefindex) ||
				!int.TryParse(selectedGlove["paint"]?.ToString(), out var paint)) return;

			if (!string.IsNullOrEmpty(Localizer["wp_glove_menu_select"]))
			{
				player.Print(Localizer["wp_glove_menu_select", selectedPaintName]);
			}

			PlayerInfo playerInfo = new PlayerInfo
			{
				UserId = player.UserId,
				Slot = player.Slot,
				Index = (int)player.Index,
				SteamId = player.SteamID.ToString(),
				Name = player.PlayerName,
				IpAddress = player.IpAddress?.Split(":")[0]
			};

			if (paint != 0)
			{
				if (!GPlayerWeaponsInfo.ContainsKey(player.Slot))
				{
					GPlayerWeaponsInfo[player.Slot] = new ConcurrentDictionary<CsTeam, ConcurrentDictionary<int, WeaponInfo>>();
				}

				foreach (var team in teamsToCheck)
				{
					if (!GPlayerWeaponsInfo[player.Slot].ContainsKey(team))
					{
						GPlayerWeaponsInfo[player.Slot][team] = new ConcurrentDictionary<int, WeaponInfo>();
					}

					playerGloves[team] = (ushort)weaponDefindex;

					if (!GPlayerWeaponsInfo[player.Slot][team].ContainsKey(weaponDefindex))
					{
						WeaponInfo weaponInfo = new()
						{
							Paint = paint
						};
						GPlayerWeaponsInfo[player.Slot][team][weaponDefindex] = weaponInfo;
					}
				}
			}
			else
			{
				GPlayersGlove.TryRemove(player.Slot, out _);
			}

			if (WeaponSync == null) return;

			WeaponSync.SyncGloveToDatabase(playerInfo, (ushort)weaponDefindex, teamsToCheck);
			WeaponSync.SyncWeaponPaintsToDatabase(playerInfo);

			AddTimer(0.1f, () => GivePlayerGloves(player));
			AddTimer(0.25f, () => GivePlayerGloves(player));
		};

		foreach (var paintName in GlovesList.Select(gloveObject => gloveObject["paint_name"]?.ToString() ?? "").Where(paintName => paintName.Length > 0))
		{
			glovesSelectionMenu.AddMenuOption(paintName, handleGloveSelection);
		}

		_config.Additional.CommandGlove.ForEach(c =>
		{
			AddCommand($"css_{c}", "Gloves selection menu", (player, info) =>
			{
				if (!Utility.IsPlayerValid(player) || !_gBCommandsAllowed) return;

				if (player == null || player.UserId == null) return;

				if (!CommandsCooldown.TryGetValue(player.Slot, out var cooldownEndTime) ||
					DateTime.UtcNow >= (CommandsCooldown.TryGetValue(player.Slot, out cooldownEndTime) ? cooldownEndTime : DateTime.UtcNow))
				{
					CommandsCooldown[player.Slot] = DateTime.UtcNow.AddSeconds(Config.CmdRefreshCooldownSeconds);
					glovesSelectionMenu?.Open(player);
					return;
				}
				if (!string.IsNullOrEmpty(Localizer["wp_command_cooldown"]))
				{
					player.Print(Localizer["wp_command_cooldown"]);
				}
			});
		});
	}
}
