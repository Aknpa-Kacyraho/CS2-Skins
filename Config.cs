using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace WeaponPaints
{
	public class Additional
	{
		[JsonPropertyName("KnifeEnabled")]
		public bool KnifeEnabled { get; set; } = true;

		[JsonPropertyName("GloveEnabled")]
		public bool GloveEnabled { get; set; } = true;

		[JsonPropertyName("SkinEnabled")]
		public bool SkinEnabled { get; set; } = true;

		[JsonPropertyName("CommandWpEnabled")]
		public bool CommandWpEnabled { get; set; } = true;

		[JsonPropertyName("CommandKillEnabled")]
		public bool CommandKillEnabled { get; set; } = true;

		[JsonPropertyName("CommandKnife")]
		public List<string> CommandKnife { get; set; } = ["knife"];

		[JsonPropertyName("CommandGlove")]
		public List<string> CommandGlove { get; set; } = ["gloves"];

		[JsonPropertyName("CommandStattrak")]
		public List<string> CommandStattrak { get; set; } = ["stattrak", "st"];

		[JsonPropertyName("CommandSkin")]
		public List<string> CommandSkin { get; set; } = ["ws"];

		[JsonPropertyName("CommandSkinSelection")]
		public List<string> CommandSkinSelection { get; set; } = ["skins"];

		[JsonPropertyName("CommandRefresh")]
		public List<string> CommandRefresh { get; set; } = ["wp"];

		[JsonPropertyName("CommandKill")]
		public List<string> CommandKill { get; set; } = ["kill"];

		[JsonPropertyName("GiveRandomKnife")]
		public bool GiveRandomKnife { get; set; } = false;

		[JsonPropertyName("GiveRandomSkin")]
		public bool GiveRandomSkin { get; set; } = false;

	}

	public class WeaponPaintsConfig : BasePluginConfig
	{
		[JsonPropertyName("ConfigVersion")] public override int Version { get; set; } = 10;

		[JsonPropertyName("SkinsLanguage")]
		public string SkinsLanguage { get; set; } = "zh-cn";

		[JsonPropertyName("CmdRefreshCooldownSeconds")]
		public int CmdRefreshCooldownSeconds { get; set; } = 3;

		[JsonPropertyName("Additional")]
		public Additional Additional { get; set; } = new();

		[JsonPropertyName("MenuType")]
		public string MenuType { get; set; } = "selectable";
	}
}
