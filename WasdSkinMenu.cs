using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Concurrent;

namespace WeaponPaints;

public static class WasdSkinMenu
{
    private static readonly ConcurrentDictionary<int, WasdState> _states = new();

    private class WasdState
    {
        public bool Active;
        public int WeaponIndex;
        public int SkinIndex;
        public int WeaponDefIndex;
        public string WeaponName = "";
        public string SkinName = "";
        public int PaintId;
        public PlayerButtons PrevButtons;
        public float InactiveTime;
    }

    private static readonly List<(string ClassName, string DisplayName)> WList = [];
    private static bool _initialized;

    public static void Init()
    {
        if (_initialized) return;
        _initialized = true;
        foreach (var kvp in WeaponPaints.WeaponDefindexList)
        {
            if (WeaponPaints.WeaponList.TryGetValue(kvp.Value, out var dn))
                WList.Add((kvp.Value, dn));
        }
    }

    public static void Toggle(CCSPlayerController player)
    {
        if (!Utility.IsPlayerValid(player)) return;

        var slot = player.Slot;
        if (_states.TryGetValue(slot, out var s) && s!.Active) { s.Active = false; return; }

        var held = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
        var hDef = held?.AttributeManager.Item.ItemDefinitionIndex ?? 1;
        var startIdx = WList.FindIndex(w => w.Item1 == WeaponPaints.WeaponDefindexList.GetValueOrDefault(hDef, ""));
        if (startIdx < 0) startIdx = 0;

        var st = new WasdState { Active = true, WeaponIndex = startIdx, SkinIndex = 0, PrevButtons = player.Buttons, InactiveTime = 0 };
        RefreshWeaponState(st);
        _states[slot] = st;
    }

    public static void OnTick()
    {
        foreach (var (slot, st) in _states)
        {
            if (!st.Active) continue;

            st.InactiveTime += 1f / 64f;
            if (st.InactiveTime > 8f) { st.Active = false; continue; }

            var player = Utilities.GetPlayerFromSlot(slot);
            if (player == null || !player.IsValid || player.IsBot || !player.PawnIsAlive) { st.Active = false; continue; }

            var cur = player.Buttons;
            bool pr(PlayerButtons b) => (st.PrevButtons & b) == 0 && (cur & b) != 0;

            if (pr(PlayerButtons.Forward))             // W
            { st.SkinIndex--; st.InactiveTime = 0; RefreshWeaponState(st); }
            else if (pr(PlayerButtons.Back))           // S
            { st.SkinIndex++; st.InactiveTime = 0; RefreshWeaponState(st); }
            else if (pr(PlayerButtons.Moveleft))       // A
            { st.WeaponIndex--; st.SkinIndex = 0; st.InactiveTime = 0; RefreshWeaponState(st); }
            else if (pr(PlayerButtons.Moveright))      // D
            { st.WeaponIndex++; st.SkinIndex = 0; st.InactiveTime = 0; RefreshWeaponState(st); }
            else if (pr(PlayerButtons.Use))            // E
            { ApplySkin(player, st); st.Active = false; st.PrevButtons = cur; continue; }
            else if (pr(PlayerButtons.Reload))         // R
            { st.Active = false; st.PrevButtons = cur; continue; }
            else if (pr(PlayerButtons.Jump))           // Space
            { st.Active = false; st.PrevButtons = cur; continue; }

            st.PrevButtons = cur;
            Render(player, st);
        }
    }

    private static void RefreshWeaponState(WasdState st)
    {
        if (WList.Count == 0) return;
        st.WeaponIndex = (st.WeaponIndex % WList.Count + WList.Count) % WList.Count;

        var (cn, dn) = WList[st.WeaponIndex];
        st.WeaponName = dn;
        st.WeaponDefIndex = WeaponPaints.WeaponDefindexList.FirstOrDefault(kv => kv.Value == cn).Key;

        var skins = WeaponPaints.SkinsList
            .Where(s => ((int?)s["weapon_defindex"] ?? 0) == st.WeaponDefIndex).ToList();

        if (skins.Count == 0) { st.SkinName = "无皮肤"; st.PaintId = 0; st.SkinIndex = 0; return; }

        st.SkinIndex = (st.SkinIndex % skins.Count + skins.Count) % skins.Count;
        var sk = skins[st.SkinIndex];
        st.SkinName = sk["paint_name"]?.ToString() ?? "?";
        st.PaintId = (int?)sk["paint"] ?? 0;
    }

    private static void ApplySkin(CCSPlayerController player, WasdState st)
    {
        if (st.PaintId == 0 || st.WeaponDefIndex == 0) return;
        if (WeaponPaints.WeaponSync == null) return;

        var p = player;
        var ps = WeaponPaints.GPlayerWeaponsInfo.GetOrAdd(p.Slot,
            _ => new ConcurrentDictionary<CsTeam, ConcurrentDictionary<int, WeaponInfo>>());

        var teams = p.TeamNum < 2 ? new[] { CsTeam.Terrorist, CsTeam.CounterTerrorist } : new[] { p.Team };
        foreach (var t in teams)
        {
            var tw = ps.GetOrAdd(t, _ => new ConcurrentDictionary<int, WeaponInfo>());
            var inf = tw.GetOrAdd(st.WeaponDefIndex, _ => new WeaponInfo());
            inf.Paint = st.PaintId; inf.Wear = 0.01f; inf.Seed = 0;
        }

        var pi = new PlayerInfo { UserId = p.UserId, Slot = p.Slot, Index = (int)p.Index, SteamId = p.SteamID.ToString(), Name = p.PlayerName, IpAddress = p.IpAddress?.Split(":")[0] };
        WeaponPaints.WeaponSync.SyncWeaponPaintsToDatabase(pi);

        if (WeaponPaints.Instance.Config.Additional is { SkinEnabled: true } && (LifeState_t)p.LifeState == LifeState_t.LIFE_ALIVE)
            WeaponPaints.Instance.RefreshWeaponsCaller(player);

        player.PrintToChat(" \x04[WP]\x01 " + st.WeaponName + " → " + st.SkinName);
    }

    private static void Render(CCSPlayerController player, WasdState st)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<font color='#3b62d9'>[WASD 换肤]</font><br>");
        sb.AppendLine($"武器: {st.WeaponName}<br>");
        sb.AppendLine($"皮肤: <font color='#ccacfc'>{st.SkinName}</font> ({st.PaintId})<br>");
        sb.AppendLine("<br><font class='fontSize-s' color='#9ee1f0'>W/S 换皮 | A/D 换武器 | E 确认 | R 取消</font>");
        player.PrintToCenterHtml(sb.ToString());
    }
}
