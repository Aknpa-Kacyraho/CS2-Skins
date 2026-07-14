using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Concurrent;

namespace WeaponPaints;

/// <summary>
/// Simple WASD single-list picker for knives and gloves.
/// W/S = scroll, E = confirm, R/Space = cancel.
/// </summary>
public static class WasdPicker
{
    private static readonly ConcurrentDictionary<int, PickerState> _states = new();

    private class PickerState
    {
        public bool Active;
        public string Title = "";
        public List<PickerItem> Items = [];
        public int SelectedIndex;
        public PlayerButtons PrevButtons;
        public float InactiveTime;
        public Action<CCSPlayerController, PickerItem>? OnConfirm;
    }

    public class PickerItem
    {
        public string DisplayName = "";
        public string Key = "";
        public int Value;             // e.g. paint ID or defindex
    }

    private static List<PickerItem>? _knifeItems;
    private static List<PickerItem>? _gloveItems;

    public static void InitLists()
    {
        // Build knife items once
        if (_knifeItems == null)
        {
            _knifeItems = WeaponPaints.WeaponList
                .Where(kv => kv.Key.StartsWith("weapon_knife") || kv.Key.StartsWith("weapon_bayonet"))
                .Select(kv => new PickerItem { DisplayName = kv.Value, Key = kv.Key, Value = 0 })
                .ToList();
        }
        // Build glove items once
        if (_gloveItems == null)
        {
            _gloveItems = WeaponPaints.GlovesList
                .Select(g => new PickerItem
                {
                    DisplayName = g["paint_name"]?.ToString() ?? "?",
                    Key = g["weapon_defindex"]?.ToString() ?? "",
                    Value = int.TryParse(g["paint"]?.ToString(), out var v) ? v : 0
                })
                .ToList();
        }
    }

    public static void OpenKnife(CCSPlayerController player)
    {
        if (_knifeItems == null || _knifeItems.Count == 0) return;
        Open(player, "[WASD 选刀]", _knifeItems, (p, item) =>
        {
            if (!Utility.IsPlayerValid(p)) return;
            if (WeaponPaints.WeaponSync == null) return;

            var playerKnives = WeaponPaints.GPlayersKnife.GetOrAdd(p.Slot,
                _ => new ConcurrentDictionary<CsTeam, string>());
            var teams = p.TeamNum < 2
                ? new[] { CsTeam.Terrorist, CsTeam.CounterTerrorist }
                : new[] { p.Team };

            foreach (var t in teams) playerKnives[t] = item.Key;

            var pi = new PlayerInfo { UserId = p.UserId, Slot = p.Slot, Index = (int)p.Index, SteamId = p.SteamID.ToString(), Name = p.PlayerName, IpAddress = p.IpAddress?.Split(":")[0] };
            WeaponPaints.WeaponSync.SyncKnifeToDatabase(pi, item.Key, teams);

            if (WeaponPaints._gBCommandsAllowed && (LifeState_t)p.LifeState == LifeState_t.LIFE_ALIVE)
                WeaponPaints.Instance.RefreshWeaponsCaller(p);

            p.PrintToChat(" \x04[WP]\x01 刀 → " + item.DisplayName);
        });
    }

    public static void OpenGlove(CCSPlayerController player)
    {
        if (_gloveItems == null || _gloveItems.Count == 0) return;
        Open(player, "[WASD 选手套]", _gloveItems, (p, item) =>
        {
            if (!Utility.IsPlayerValid(p)) return;
            if (WeaponPaints.WeaponSync == null) return;

            var defIndex = (ushort)(int.TryParse(item.Key, out var di) ? di : 0);
            var paint = item.Value;
            if (defIndex == 0 || paint == 0) return;

            // Store glove defindex
            var playerGloves = WeaponPaints.GPlayersGlove.GetOrAdd(p.Slot,
                _ => new ConcurrentDictionary<CsTeam, ushort>());
            var teams = p.TeamNum < 2
                ? new[] { CsTeam.Terrorist, CsTeam.CounterTerrorist }
                : new[] { p.Team };

            foreach (var t in teams) playerGloves[t] = defIndex;

            // Store glove paint in GPlayerWeaponsInfo
            var ps = WeaponPaints.GPlayerWeaponsInfo.GetOrAdd(p.Slot,
                _ => new ConcurrentDictionary<CsTeam, ConcurrentDictionary<int, WeaponInfo>>());
            foreach (var t in teams)
            {
                var tw = ps.GetOrAdd(t, _ => new ConcurrentDictionary<int, WeaponInfo>());
                tw[defIndex] = new WeaponInfo { Paint = paint, Wear = 0.00f, Seed = 0 };
            }

            var pi = new PlayerInfo { UserId = p.UserId, Slot = p.Slot, Index = (int)p.Index, SteamId = p.SteamID.ToString(), Name = p.PlayerName, IpAddress = p.IpAddress?.Split(":")[0] };
            WeaponPaints.WeaponSync.SyncGloveToDatabase(pi, defIndex, teams);
            WeaponPaints.WeaponSync.SyncWeaponPaintsToDatabase(pi);

            WeaponPaints.Instance.AddTimer(0.15f, () => WeaponPaints.Instance.GivePlayerGlovesCaller(p));
            WeaponPaints.Instance.AddTimer(0.3f, () => WeaponPaints.Instance.GivePlayerGlovesCaller(p));

            p.PrintToChat(" \x04[WP]\x01 手套 → " + item.DisplayName);
        });
    }

    private static void Open(CCSPlayerController player, string title, List<PickerItem> items,
        Action<CCSPlayerController, PickerItem> onConfirm)
    {
        if (!Utility.IsPlayerValid(player)) return;

        var slot = player.Slot;
        if (_states.TryGetValue(slot, out var s) && s!.Active) { s.Active = false; return; }

        // Determine initial selected from current player state
        var startIdx = 0;
        if (title.Contains("选刀"))
        {
            if (WeaponPaints.GPlayersKnife.TryGetValue(slot, out var kdict) &&
                kdict.TryGetValue(player.Team, out var curKnife))
                startIdx = items.FindIndex(i => i.Key == curKnife);
        }
        else if (title.Contains("选手套"))
        {
            if (WeaponPaints.GPlayersGlove.TryGetValue(slot, out var gdict) &&
                gdict.TryGetValue(player.Team, out var curGlove))
                startIdx = items.FindIndex(i => int.TryParse(i.Key, out var d) && d == curGlove);
        }
        if (startIdx < 0) startIdx = 0;

        var st = new PickerState
        {
            Active = true, Title = title, Items = items,
            SelectedIndex = startIdx, PrevButtons = player.Buttons, InactiveTime = 0,
            OnConfirm = onConfirm
        };
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

            if (pr(PlayerButtons.Forward))
            { st.SelectedIndex--; st.InactiveTime = 0; }
            else if (pr(PlayerButtons.Back))
            { st.SelectedIndex++; st.InactiveTime = 0; }
            else if (pr(PlayerButtons.Use))            // E
            { st.OnConfirm?.Invoke(player, st.Items[st.SelectedIndex]); st.Active = false; st.PrevButtons = cur; continue; }
            else if (pr(PlayerButtons.Reload) || pr(PlayerButtons.Jump))  // R / Space
            { st.Active = false; st.PrevButtons = cur; continue; }

            // Clamp index
            var cnt = st.Items.Count;
            if (cnt > 0) st.SelectedIndex = (st.SelectedIndex % cnt + cnt) % cnt;

            st.PrevButtons = cur;
            Render(player, st);
        }
    }

    private static void Render(CCSPlayerController player, PickerState st)
    {
        var item = st.Items.Count > 0 ? st.Items[st.SelectedIndex] : new PickerItem { DisplayName = "无" };
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"<font color='#3b62d9'>{st.Title}</font><br>");
        sb.AppendLine($"<font color='#ccacfc'>{item.DisplayName}</font><br>");
        sb.AppendLine($"<br><font class='fontSize-s' color='#9ee1f0'>W/S 切换 | E 确认 | R 取消</font>");
        player.PrintToCenterHtml(sb.ToString());
    }
}
