# Changelog

## 3.5.0 — 2026-11-22

### Added

- Missing menu backend is no longer silent: `!knife`, `!gloves`, `!music`, `!pins` and
  `!skins` reply with `wp_menu_unavailable` when MenuManager is not loaded.
- Skin submenu has a “← 返回武器列表” option, so switching weapons does not require
  retyping the command.
- `!ws` help lists `!skins`, `!stattrak`, and the RollTheDice help entry.
- Refresh cooldown shows the remaining seconds.

### Changed

- Menu opening uses its own 1-second cooldown; `!wp` no longer blocks menus and menus no
  longer block `!wp`.
- Chinese copy fixes: 代理 → 特工, 固定项 → 胸章, “正在刷新” → “已刷新”.
- `lang/en.json` now carries the same Chinese copy as `lang/zh-cn.json`. CSS currently
  falls back to `en` (`ServerLanguage=zh-CN` is unsupported), so players previously saw
  English menu/help text.
- Selection messages now tell the player how to change the selection again.

## 3.4.2 — 2026-11-22

### Fixed

- StatTrak counts are persisted at round end in addition to disconnect. Previously a
  crash or map change lost every kill counted during the round.
- The round-end flush captures the `WeaponSynchronization` instance in a local, so an
  unload/reload cannot null it between the check and the background task.

## 3.4.1 — 2026-11-22

### Fixed

- Knife defindex lookup uses `TryGetValue` on both the slot and team dictionaries; a
  missing entry now skips the knife instead of throwing inside the skin applier.
- `SkinsLanguage` files fall back to `en` (with a warning) when the requested language
  file is missing. The loaders already tolerate a fully missing file, but a missing
  translation no longer disables skins for that language.

## 3.4.0 — 2026-11-22

Stability release based on upstream build-474, rebuilt for CS2 1.41.9.7 /
CounterStrikeSharp 1.0.380.

### Fixed

- **Lost guns on refresh**: `RefreshWeapons` returned from the whole method when
  `gun.VData` was null, after the weapon had already been scheduled for `Kill`. The
  re-give timer was never created. Guns are now re-skinned in place.
- **Knife world-prop / ghost knife slot**: refresh and pickup no longer `Kill` and
  re-give the held knife. The knife subclass/paint is applied in place. This is the
  same failure mode previously seen as "knife on the floor cannot be picked up".
- **Plugin unload on missing data file**: `File.ReadAllText` ran outside the try block
  for skins/pins, and JSON errors were not caught for the other lists. All data
  loaders now check the file, catch parse errors, log and fall back to an empty list.
- **Slot reuse cross-talk**: database results are written only when
  `SlotOwners[slot]` still equals the loading player's SteamID. Disconnect removes the
  slot owner before starting the StatTrak write.
- **`!wp` no-op**: the command used to start a background load and immediately apply the
  old cache. It now applies the loaded data on the next world update and verifies the
  player is still the same SteamID.
- **Bots could receive agents/pins**: spawn, agent, pin and StatTrak paths now reject
  bots/HLTV; the weapon-skin entry point is a single real-player choke point.
- **Version check**: 5 second timeout; network failures log a warning instead of an
  error. Database table creation failures are logged instead of being dropped.
- Glove application re-checks the current pawn after the 0.08s delay.

### Changed

- `VERSION` and `ModuleVersion` are `3.4.0`.
- `RefreshWeapons` only re-applies skins; it never rebuilds weapon entities.

### Notes

- Behaviour change: refreshing no longer recreates weapons. If a specific paint does not
  refresh in place on a future CS2 build, report it with the weapon name instead of
  reintroducing `Kill + regive`.
