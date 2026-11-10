# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

CS2 (Counter-Strike 2) 武器皮肤/刀/手套插件，基于 CounterStrikeSharp API。从上游 `cs2-WeaponPaints` fork 后做了大幅精简改造。

**与上游的主要差异：**
- MySQL → 本地 JSON 文件存储（`player_data.json`）
- 删除 MenuManager 外部依赖，改用 CSS 内置 `ChatMenu`
- 删除 Agent/Music/Pin/Website 功能，只保留 Skin/Knife/Glove
- 删除 MySQL/Dapper 依赖，只保留 Newtonsoft.Json + CounterStrikeSharp.API
- 新增 WASD 按键换肤菜单（适配自 `../CS2-PlayerModelChanger-master/` 的按键上升沿检测模式）
- 默认语言 `zh-cn`，编译目标 `net10.0`

## 编译与部署

```bash
# 编译（.NET SDK 10）
dotnet build WeaponPaints.csproj -c Release
```

编译产物 `bin/Release/net10.0/WeaponPaints.dll`。

**部署只拷这些**：`WeaponPaints.dll`、`WeaponPaints.deps.json`、`Newtonsoft.Json.dll`、`data/`、`lang/`（`gamedata/weaponpaints.json` 放 `addons/counterstrikesharp/gamedata/`）。

**绝不要**把 `dotnet build` 复制出来的 `CounterStrikeSharp.API.dll`、`Microsoft.*`、`Serilog.*` 等运行时副本留在插件目录——它们会覆盖宿主 CSS 的 API（2026-11-02 曾残留 1.0.367 旧 API）。目标运行时：**CounterStrikeSharp 1.0.379 / CS2 1.41.9.3**。

## 部署目录结构

```
D:\...\addons\counterstrikesharp\
├── gamedata\
│   └── weaponpaints.json          ← 必须！OnConfigParsed 硬检查此路径
├── plugins\
│   └── WeaponPaints\
│       ├── WeaponPaints.dll       ← 编译产物
│       ├── Newtonsoft.Json.dll    ← NuGet 依赖，编译自动生成
│       ├── player_data.json       ← 运行时自动生成（玩家数据）
│       ├── data\
│       │   ├── skins_zh-cn.json   ← 皮肤列表数据
│       │   └── gloves_zh-cn.json  ← 手套列表数据
│       └── lang\
│           └── en.json            ← 语言文件（实际内容为中文翻译）
└── configs\plugins\WeaponPaints\
    └── WeaponPaints.json          ← 运行时配置，首次加载自动生成
```

## 文件职责

| 文件 | 职责 |
|------|------|
| `WeaponPaints.cs` | 插件入口：`Load()` / `OnConfigParsed()` / `OnAllPluginsLoaded()`，初始化 `WeaponSync` + `PlayerStorage`，注册 `!wskin` 命令 |
| `WasdSkinMenu.cs` | **WASD 换肤**：通过 OnTick 检测按键上升沿（`PrevButtons` vs `cur`），`PrintToCenterHtml` 每帧渲染，W/S 换皮肤、A/D 换武器、E 确认 |
| `Commands.cs` | ChatMenu 命令注册：`SetupKnifeMenu()` / `SetupSkinsMenu()` / `SetupGlovesMenu()` + 命令处理器 |
| `Events.cs` | CSS 事件/Listener 注册：`OnClientFullConnect`、`OnPlayerSpawn`、`OnPlayerDisconnect`、`OnMapStart`、`OnEntityCreated`、`OnGiveNamedItemPost`、`OnPlayerDeath`、`OnItemPickup`，注意不再注册 `OnTick`（WasdSkinMenu 自己在 WeaponPaints.cs 注册） |
| `WeaponAction.cs` | **核心皮肤应用**：`GivePlayerWeaponSkin()`、`RefreshWeapons()`、`GivePlayerGloves()`、`HasChangedKnife()`、`HasChangedPaint()` |
| `Variables.cs` | 静态状态字典、`WeaponList`、`WeaponDefindexList`、`CAttributeListSetOrAddAttributeValueByName` 签名 |
| `Storage.cs` | JSON 文件存储，线程安全（`lock` + `ConcurrentDictionary`） |
| `WeaponSynchronization.cs` | 内存字典 ↔ JSON 存储的数据加载/同步（**全部同步方法，无 async**） |
| `Utility.cs` | `IsPlayerValid()`、`LoadSkinsFromFile()`、`LoadGlovesFromFile()`、`CreateMenu()`（CSS 内置 ChatMenu） |
| `Config.cs` / `PlayerInfo.cs` / `WeaponInfo.cs` | 数据模型 |

## 三条数据流

**聊天菜单（ChatMenu）** — `!knife` `!skins` `!gloves`：
```
选择 → 写 GPlayersKnife/GPlayerWeaponsInfo/GPlayersGlove
     → Sync*ToDatabase() → player_data.json
     → 存活 → RefreshWeapons() → GivePlayerWeaponSkin()
```

**WASD 菜单** — `!wskin`：
```
OnTick 每帧检测按键上升沿 → RefreshWeaponState()（查 SkinsList 更新 display name + PaintId）
                        → E 按下 → ApplySkin() → 写 GPlayerWeaponsInfo → SyncWeaponPaintsToDatabase()
                        → 每帧 PrintToCenterHtml（不刷就消失）
```

**武器给予时自动应用** — `OnGiveNamedItemPost` / `OnEntityCreated`：
```
给予武器 → GivePlayerWeaponSkin()
         → isKnife? → HasChangedKnife() → SubclassChange() + ItemDefinitionIndex
         → isGun?  → HasChangedPaint() → FallbackPaintKit + seed + wear (双重属性列表)
```

## WASD 换肤按键检测机制

从 PlayerModelChanger 适配。利用 OnTick 检测按键**上升沿**：

```csharp
// 存储上一帧的 Buttons 位掩码
// 检测：前一帧没按 (=0)，当前帧按了 (≠0)
bool pressed(PlayerButtons b) => (st.PrevButtons & b) == 0 && (cur & b) != 0;
// 每帧结束时更新
st.PrevButtons = cur;
```

**关键**：PrintToCenterHtml 不持久，必须每帧调用。曾经因为只在按键时渲染导致菜单闪一下消失。

## 皮肤应用机制

`GivePlayerWeaponSkin()` 通过两套属性列表应用皮肤，两者**必须同时设置** `prefab` + `seed` + `wear` 三个属性：
1. `NetworkedDynamicAttributes` — 网络同步
2. `AttributeList` — 本地属性

只设一个列表或只设 `prefab` 都会导致皮肤不渲染。

## 历史致命陷阱

| 陷阱 | 后果 |
|------|------|
| `GivePlayerWeaponSkin()` 第22行 `GPlayerWeaponsInfo` 检查在 `isKnife` 判断之前 | 只换刀不换皮 → 空字典 return → 刀永远不生效 |
| 皮肤非随机路径只设 `prefab` 不设 `seed`/`wear` | 皮肤不渲染 |
| `SyncWeaponPaintsToDatabase` 在存活性检查之后调用 | 死亡选皮数据不落盘，重连丢失 |
| `gamedata/weaponpaints.json` 放错目录 | `OnConfigParsed` 直接 `Unload(false)`，插件静默退出 |
| `WeaponSync` 在 `OnMapStart` Listener 创建而非 `OnConfigParsed` | 已加载地图时 Listener 不触发 → `WeaponSync` 永远 null → `IsPlayerValid` 永远 false → 所有命令静默无反应 |
| `FollowCS2ServerGuidelines` 设为 `true` | CS2 复位武器属性，所有皮肤不生效 |
| `css_skin` 与 PlayerModelChanger 冲突 | 两个插件注册同名命令，改用 `css_wskin` |
| `PrintToCenterHtml` 不每帧调用 | 菜单闪一下消失（CS2 1-2s 后清除中央 HTML） |
| `SkinIndex` 变更后不调 `RefreshWeaponState` | 切换皮肤时 PaintId/SkinName 不更新 → 显示不变 |
