using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using Microsoft.Extensions.Logging;

namespace WeaponPaints;

[MinimumApiVersion(338)]
public partial class WeaponPaints : BasePlugin, IPluginConfig<WeaponPaintsConfig>
{
	internal static WeaponPaints Instance { get; private set; } = new();

	public WeaponPaintsConfig Config { get; set; } = new();
	private static WeaponPaintsConfig _config { get; set; } = new();
	public override string ModuleAuthor => "Nereziel & daffyy";
	public override string ModuleDescription => "Skin, gloves, and knife selector with auto-save";
	public override string ModuleName => "WeaponPaints";
	public override string ModuleVersion => "3.3b";

	public override void Load(bool hotReload)
	{
		Instance = this;

		if (hotReload)
		{
			OnMapStart(string.Empty);

			GPlayerWeaponsInfo.Clear();
			GPlayersKnife.Clear();
			GPlayersGlove.Clear();

			foreach (var player in Enumerable
					.OfType<CCSPlayerController>(Utilities.GetPlayers().TakeWhile(_ => WeaponSync != null))
					.Where(player => player.IsValid &&
						!string.IsNullOrEmpty(player.IpAddress) && player is
							{ IsBot: false, Connected: PlayerConnectedState.Connected }))
			{
				var playerInfo = new PlayerInfo
				{
					UserId = player.UserId,
					Slot = player.Slot,
					Index = (int)player.Index,
					SteamId = player?.SteamID.ToString(),
					Name = player?.PlayerName,
					IpAddress = player?.IpAddress?.Split(":")[0]
				};

				if (WeaponSync != null) WeaponSync.GetPlayerData(playerInfo);
			}
		}

		try
		{
			Utility.LoadSkinsFromFile(ModuleDirectory + $"/data/skins_{_config.SkinsLanguage}.json", Logger);
			Utility.LoadGlovesFromFile(ModuleDirectory + $"/data/gloves_{_config.SkinsLanguage}.json", Logger);
		}
		catch (Exception ex)
		{
			Logger.LogError("Error loading data files: {msg}", ex.Message);
		}

		WasdSkinMenu.Init();
		WasdPicker.InitLists();
		RegisterListeners();
		RegisterListener<Listeners.OnTick>(TickAll);
	}

	public void OnConfigParsed(WeaponPaintsConfig config)
	{
		Config = config;
		_config = config;

		var gamedataPath = Path.GetDirectoryName(Path.GetDirectoryName(ModuleDirectory)) + "/gamedata/weaponpaints.json";

		if (!File.Exists(gamedataPath))
		{
			Logger.LogError("You need to upload \"weaponpaints.json\" to \"gamedata directory\"!");
			Unload(false);
			return;
		}

		// Initialize JSON file storage + sync engine
		var storagePath = Path.Combine(ModuleDirectory, "player_data.json");
		PlayerStorage = new Storage(storagePath);
		WeaponSync = new WeaponSynchronization(PlayerStorage, config);

		_localizer = Localizer;
		Utility.Config = config;
	}

	public override void OnAllPluginsLoaded(bool hotReload)
	{
		if (Config.Additional.KnifeEnabled)
			SetupKnifeMenu();
		if (Config.Additional.SkinEnabled)
			SetupSkinsMenu();
		if (Config.Additional.GloveEnabled)
			SetupGlovesMenu();

		RegisterCommands();
		RegisterWASDCommand();
	}

	private void TickAll()
	{
		WasdSkinMenu.OnTick();
		WasdPicker.OnTick();
	}

	/// <summary>
	/// WASD 命令：!wskin / !wknife / !wglove
	/// </summary>
	private void RegisterWASDCommand()
	{
		AddCommand("css_wskin", "WASD skin browser", (player, _) =>
		{
			if (player == null) return;
			WasdSkinMenu.Toggle(player);
		});
		AddCommand("css_wknife", "WASD knife picker", (player, _) =>
		{
			if (player == null) return;
			WasdPicker.OpenKnife(player);
		});
		AddCommand("css_wglove", "WASD glove picker", (player, _) =>
		{
			if (player == null) return;
			WasdPicker.OpenGlove(player);
		});
	}

	/// <summary>
	/// Public entry points for WASD menus to call weapon actions.
	/// </summary>
	internal void RefreshWeaponsCaller(CCSPlayerController player)
	{
		// RefreshWeapons is defined in WeaponAction.cs partial class
		RefreshWeapons(player);
	}
	internal void GivePlayerGlovesCaller(CCSPlayerController player)
	{
		GivePlayerGloves(player);
	}
}
