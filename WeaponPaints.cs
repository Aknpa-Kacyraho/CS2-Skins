using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace WeaponPaints;

[MinimumApiVersion(338)]
public partial class WeaponPaints : BasePlugin, IPluginConfig<WeaponPaintsConfig>
{
	internal static WeaponPaints Instance { get; private set; } = new();

	public WeaponPaintsConfig Config { get; set; } = new();
    private static WeaponPaintsConfig _config { get; set; } = new();
    public override string ModuleAuthor => "Nereziel & daffyy";
	public override string ModuleDescription => "Skin, gloves, agents and knife selector, standalone and web-based";
	public override string ModuleName => "WeaponPaints";
	public override string ModuleVersion => "3.4.2";

	public override void Load(bool hotReload)
	{
		Instance = this;

		if (hotReload)
		{
			OnMapStart(string.Empty);
			
			GPlayerWeaponsInfo.Clear();
			GPlayersKnife.Clear();
			GPlayersGlove.Clear();
			GPlayersAgent.Clear();
			GPlayersPin.Clear();
			GPlayersMusic.Clear();

			foreach (var player in Enumerable
				         .OfType<CCSPlayerController>(Utilities.GetPlayers().TakeWhile(_ => WeaponSync != null))
				         .Where(player => player.IsValid &&
					         !string.IsNullOrEmpty(player.IpAddress) && player is
						         { IsBot: false, Connected: PlayerConnectedState.Connected }))
			{
				var steamId = player!.SteamID.ToString();
				SlotOwners[player.Slot] = steamId;
				var playerInfo = new PlayerInfo
				{
					UserId = player.UserId,
					Slot = player.Slot,
					Index = (int)player.Index,
					SteamId = steamId,
					Name = player.PlayerName,
					IpAddress = player.IpAddress?.Split(":")[0]
				};

				_ = Task.Run(async () =>
				{
					if (WeaponSync != null) await WeaponSync.GetPlayerData(playerInfo);
				});
			}
		}

		string dataLanguage = _config.SkinsLanguage;
		Utility.LoadSkinsFromFile(DataFile("skins", dataLanguage), Logger);
		Utility.LoadGlovesFromFile(DataFile("gloves", dataLanguage), Logger);
		Utility.LoadAgentsFromFile(DataFile("agents", dataLanguage), Logger);
		Utility.LoadMusicFromFile(DataFile("music", dataLanguage), Logger);
		Utility.LoadPinsFromFile(DataFile("collectibles", dataLanguage), Logger);

		RegisterListeners();
	}

	private string DataFile(string prefix, string language)
	{
		string dir = Path.Combine(ModuleDirectory, "data");
		string preferred = Path.Combine(dir, $"{prefix}_{language}.json");
		if (File.Exists(preferred))
		{
			return preferred;
		}

		string fallback = Path.Combine(dir, $"{prefix}_en.json");
		Logger.LogWarning("[WP] {Prefix} data for language '{Language}' not found; falling back to en ({Path}).", prefix, language, fallback);
		return fallback;
	}

	public override void Unload(bool hotReload)
	{
		// Native hook outlives the plugin's AssemblyLoadContext; without this, css_plugins reload
		// leaves the old OnGiveNamedItemPost delegate registered and the next GiveNamedItem crashes the server.
		if (!_giveNamedItemHooked) return;

		VirtualFunctions.GiveNamedItemFunc.Unhook(OnGiveNamedItemPost, HookMode.Post);
		_giveNamedItemHooked = false;
	}

	public void OnConfigParsed(WeaponPaintsConfig config)
	{
		Config = config;
		_config = config;

		config.Additional ??= new Additional();
		if (string.IsNullOrWhiteSpace(config.DatabaseHost) ||
		    string.IsNullOrWhiteSpace(config.DatabaseName) ||
		    string.IsNullOrWhiteSpace(config.DatabaseUser))
		{
			throw new InvalidOperationException("You need to setup Database credentials in \"configs/plugins/WeaponPaints/WeaponPaints.json\"!");
		}

		if (!File.Exists(Path.GetDirectoryName(Path.GetDirectoryName(ModuleDirectory)) + "/gamedata/weaponpaints.json"))
		{
			throw new InvalidOperationException("You need to upload \"weaponpaints.json\" to \"gamedata directory\"!");
		}
		
		var builder = new MySqlConnectionStringBuilder
		{
			Server = config.DatabaseHost,
			UserID = config.DatabaseUser,
			Password = config.DatabasePassword,
			Database = config.DatabaseName,
			Port = (uint)config.DatabasePort,
			Pooling = true,
			MaximumPoolSize = 640,
		};

		Database = new Database(builder.ConnectionString);
		// Created here instead of OnMapStart: on a cold boot the first map can start before the
		// listener is registered, leaving WeaponSync null and every command silently ignored.
		WeaponSync = new WeaponSynchronization(Database, config);

		_ = Task.Run(async () =>
		{
			try
			{
				await Utility.CheckDatabaseTables();
				Logger.LogInformation("[WP] Database tables are ready.");
			}
			catch (Exception ex)
			{
				// Keep the plugin loaded: commands will report a clear error instead of the
				// whole plugin disappearing from css_plugins list.
				Logger.LogError(ex, "[WP] Database tables are unavailable; skin features will degrade.");
			}
		});
		_localizer = Localizer;

		Utility.Config = config;
		Utility.ShowAd(ModuleVersion);
		Task.Run(async () => await Utility.CheckVersion(ModuleVersion, Logger));
	}

	public override void OnAllPluginsLoaded(bool hotReload)
	{
		try
		{
			MenuApi = MenuCapability.Get();
			
			if (Config.Additional.KnifeEnabled)
				SetupKnifeMenu();
			if (Config.Additional.SkinEnabled)
				SetupSkinsMenu();
			if (Config.Additional.GloveEnabled)
				SetupGlovesMenu();
			if (Config.Additional.AgentEnabled)
				SetupAgentsMenu();
			if (Config.Additional.MusicEnabled)
				SetupMusicMenu();
			if (Config.Additional.PinsEnabled)
				SetupPinsMenu();
		
			RegisterCommands();
		}
		catch (Exception)
		{
			MenuApi = null;
			Logger.LogError("Error while loading required plugins");
			throw;
		}
	}
}
