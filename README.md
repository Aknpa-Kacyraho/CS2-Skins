# WeaponPaints — CS2 武器皮肤插件（Kacyra 精简版）

[![License](https://img.shields.io/badge/License-GPLv3-blue)](LICENSE)

**基于 [Nereziel/cs2-WeaponPaints](https://github.com/Nereziel/cs2-WeaponPaints) 的精简改造版**，去除外部依赖，新增 WASD 按键换肤，本地 JSON 存储。

> **原项目**：[Nereziel/cs2-WeaponPaints](https://github.com/Nereziel/cs2-WeaponPaints) by [@Nereziel](https://github.com/Nereziel) & daffyy  
> 本项目遵循 GPLv3 协议，保留原作者全部版权。

---

## 目录

- [与原版的区别](#与原版的区别)
- [功能](#功能)
- [安装指南](#安装指南)
- [命令说明](#命令说明)
- [配置详解](#配置详解)
- [常见问题](#常见问题)
- [编译指南](#编译指南)

---

## 与原版的区别

| 改动 | 原版 | 本版 |
|------|------|------|
| **数据存储** | MySQL 数据库 | 本地 JSON 文件（`player_data.json`） |
| **菜单系统** | 依赖 MenuManagerCS2 + PlayerSettings + AnyBaseLibCS2 | CSS 内置 ChatMenu |
| **换肤方式** | 聊天菜单打字选择 | ChatMenu + **WASD 按键实时预览**（!wskin） |
| **功能范围** | 皮肤/刀/手套/探员/音乐盒/勋章 | 皮肤/刀/手套（三合一精简） |
| **NuGet 依赖** | CounterStrikeSharp + Newtonsoft.Json + Dapper + MySQL | CounterStrikeSharp + Newtonsoft.Json（仅 2 个） |
| **默认语言** | en | zh-cn |
| **命令前缀** | css_skin（与换模插件冲突） | css_wskin（独立前缀） |

### 删除的功能

- 探员（Agents）、音乐盒（Music）、勋章（Pins）、网站（Website）——场景服务器不需要
- MySQL 数据库 + Dapper ORM——本地 JSON 更简单，无需额外部署数据库

### 新增的功能

- **WASD 按键换肤**：W/S 切换皮肤、A/D 切换武器、E 确认，实时预览效果
- **!wknife / !wglove**：WASD 选刀/选手套，直观方便

---

## 功能

- 武器皮肤、刀、手套一键更换
- 保留原版 paint + seed + wear（图案、磨损度）完整支持
- 数据自动落盘，重连不丢
- 中文界面 + 中文皮肤/手套名称
- WASD 实时预览换肤（`!wskin`）

---

## 安装指南

### 前提条件

- CS2 **1.41.9.3**（2026-10-28 更新）
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) **1.0.379**（net10.0 运行时）
- 无需 MySQL、无需任何第三方插件依赖

### 安装步骤

1. 从 [Releases](../../releases) 下载最新版本
2. 将 `WeaponPaints.dll` 和 `Newtonsoft.Json.dll` 放到 `addons/counterstrikesharp/plugins/WeaponPaints/`
3. **必须**将 `gamedata/weaponpaints.json` 放到 `addons/counterstrikesharp/gamedata/`（不然后插件会拒绝加载）
4. 将 `data/` 文件夹放到 `plugins/WeaponPaints/data/`
5. 将 `lang/` 文件夹放到 `plugins/WeaponPaints/lang/`
6. 在 `configs/core.json` 中设置 `FollowCS2ServerGuidelines` 为 **`false`**（否则皮肤不显示）
7. **不要**在插件目录里保留 `CounterStrikeSharp.API.dll` 等运行时副本（旧版副本会覆盖 CSS 1.0.379 的 API）
8. 重启服务器

### 部署目录结构

```
addons/counterstrikesharp/
├── gamedata/
│   └── weaponpaints.json                ← 必须！
├── plugins/
│   └── WeaponPaints/
│       ├── WeaponPaints.dll
│       ├── Newtonsoft.Json.dll
│       ├── player_data.json             ← 自动生成（玩家数据）
│       ├── data/
│       │   ├── skins_zh-cn.json         ← 皮肤列表
│       │   └── gloves_zh-cn.json        ← 手套列表
│       └── lang/
│           ├── zh-cn.json               ← 中文语言
│           └── en.json                  ← 英文备用
└── configs/
    └── plugins/
        └── WeaponPaints/
            └── WeaponPaints.json        ← 自动生成（插件配置）
```

---

## 命令说明

### ChatMenu 命令（聊天框打字选择）

| 命令 | 说明 |
|------|------|
| `!knife` | 打开菜单选刀（按数字选择） |
| `!skins` | 打开菜单选武器皮肤 |
| `!gloves` | 打开菜单选手套 |
| `!wp` | 刷新皮肤（冷却 3 秒） |
| `!kill` | 自杀（刀皮肤需要重生才生效） |
| `!st` / `!stattrak` | StatTrak 计数 |

### WASD 命令（按键实时预览）

| 命令 | 说明 |
|------|------|
| `!wskin` | WASD 皮肤浏览器：W/S 换皮肤、A/D 换武器、E 确认、R 随机 |
| `!wknife` | WASD 选刀 |
| `!wglove` | WASD 选手套 |

**WASD 菜单操作提示**：
- 按 E 确认后即时生效，无需 `!kill` 自杀
- 菜单实时显示皮肤名称 + Paint ID，绿色高亮当前选项
- 再按一次同一个命令键退出菜单

---

## 配置详解

配置文件：`addons/counterstrikesharp/configs/plugins/WeaponPaints/WeaponPaints.json`

首次加载自动生成：

```json
{
  "ConfigVersion": 10,
  "SkinsLanguage": "zh-cn",
  "CmdRefreshCooldownSeconds": 3,
  "MenuType": "selectable",
  "Additional": {
    "KnifeEnabled": true,
    "GloveEnabled": true,
    "SkinEnabled": true,
    "CommandWpEnabled": true,
    "CommandKillEnabled": true,
    "CommandKnife": ["knife"],
    "CommandGlove": ["gloves"],
    "CommandStattrak": ["stattrak", "st"],
    "CommandSkin": ["ws"],
    "CommandSkinSelection": ["skins"],
    "CommandRefresh": ["wp"],
    "CommandKill": ["kill"],
    "GiveRandomKnife": false,
    "GiveRandomSkin": false
  }
}
```

### 配置项说明

| 配置项 | 默认值 | 说明 |
|--------|--------|------|
| `SkinsLanguage` | `"zh-cn"` | 皮肤数据语言。可选 `"en"`，会自动加载 `data/skins_{Language}.json` |
| `CmdRefreshCooldownSeconds` | `3` | !wp 刷新皮肤冷却（秒） |
| `MenuType` | `"selectable"` | ChatMenu 类型 |
| `KnifeEnabled` | `true` | 启用刀系统 |
| `GloveEnabled` | `true` | 启用手套系统 |
| `SkinEnabled` | `true` | 启用皮肤系统 |
| `CommandWpEnabled` | `true` | 启用刷新命令 |
| `CommandKillEnabled` | `true` | 启用自杀命令 |
| `CommandKnife` | `["knife"]` | 刀菜单命令（可改为 `["dao", "k"]`） |
| `CommandGlove` | `["gloves"]` | 手套菜单命令 |
| `CommandSkin` | `["ws"]` | 皮肤信息命令 |
| `CommandSkinSelection` | `["skins"]` | 皮肤菜单命令 |
| `CommandRefresh` | `["wp"]` | 刷新命令 |
| `CommandKill` | `["kill"]` | 自杀命令 |
| `GiveRandomKnife` | `false` | 新玩家自动随机送刀 |
| `GiveRandomSkin` | `false` | 新玩家自动随机送皮肤 |

### 自定义皮肤/手套列表

编辑 `data/skins_zh-cn.json` 和 `data/gloves_zh-cn.json`，格式：

```json
{
  "SkinsList": [
    {
      "Paint": 38,         // 皮肤 ID（AK 红线）
      "Weapon": "weapon_ak47",
      "DisplayName": "AK-47 | 红线",
      "WeaponType": "rifle"
    }
  ]
}
```

### !wskin 白名单

`!wskin` 菜单中出现的武器由 `Variables.cs` 里的 `SkinsList` 定义。添加新武器/皮肤需要同时更新 `data/skins_zh-cn.json` 和 `Variables.cs`。

---

## 常见问题

### Q: 皮肤不显示/不生效？

A: 三步排查：
1. `configs/core.json` 中 `FollowCS2ServerGuidelines` 必须是 **`false`**
2. `gamedata/weaponpaints.json` 是否放到了 `addons/counterstrikesharp/gamedata/`
3. 选了刀后需要 `!kill` 自杀一次重新生成

### Q: WASD 菜单闪一下就消失？

A: CS2 会在 1-2 秒后清除屏幕中央 HTML。WASD 菜单每帧渲染，关掉菜单不需要手动操作，再按一次命令键退出即可。

### Q: 怎么添加新皮肤？

A: 编辑 `data/skins_zh-cn.json`，找到对应武器的 Paint ID 加到列表里。CS2 皮肤 ID 可以在 [csgostash.com](http://csgostash.com) 查。

### Q: 可以改成英文吗？

A: `configs/plugins/WeaponPaints/WeaponPaints.json` 中设置 `"SkinsLanguage": "en"`，同时确保 `data/skins_en.json` 文件存在。

### Q: 插件不加载？

A: 最常见原因是 `gamedata/weaponpaints.json` 没放对位置。检查 `addons/counterstrikesharp/gamedata/` 目录下是否有该文件。

---

## 编译指南

### 环境要求

- .NET 10.0 SDK
- CounterStrikeSharp NuGet 包

### 编译

```bash
git clone https://github.com/你的用户名/cs2-weaponpaints.git
cd cs2-weaponpaints
dotnet restore
dotnet build
```

编译产物：`bin/Debug/net10.0/WeaponPaints.dll` + `Newtonsoft.Json.dll`

---

## 更新日志

### v3.3c（2026-11-10）
- 适配 CS2 **1.41.9.6**（CounterStrikeSharp 1.0.379）
- README / CLAUDE.md 补充部署规范：**不要**在插件目录保留旧版 CSS 运行时副本

### v3.3b（2026-11-02）
- 适配 CS2 1.41.9.3 / CounterStrikeSharp 1.0.379
- 修复 `RefreshWeapons` 0.23s 定时器：重查玩家有效性，并校验新发武器句柄后再写弹匣/备弹
- 修复 `GivePlayerGloves` 0.2s bodygroup 定时器：重解析 pawn，`SetBodygroup` 前判有效
- 修复 `OnEntityCreated` 的 `OwnerEntity` 判空（原先异常被吞，偶发不换肤）
- `player_data.json` 改为原子写入，进程中断不再损坏存档
- 安装注意：删除旧版插件目录里的 CSS 运行时副本

---

## 许可证

本项目基于 [Nereziel/cs2-WeaponPaints](https://github.com/Nereziel/cs2-WeaponPaints) 修改，遵循 **GNU General Public License v3.0 (GPLv3)**。

- 原项目版权 © [Nereziel](https://github.com/Nereziel) & daffyy
- 修改部分版权 © Kacyra

---

## 致谢

- [Nereziel](https://github.com/Nereziel) — 原版 WeaponPaints 作者
- daffyy — 原版开发
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) — CS2 插件框架
- [PlayerModelChanger](https://github.com/samyycX/CS2-PlayerModelChanger) — WASD 按键检测机制参考
