# MitaGodMode —— 作弊模组「无敌与和平」使用说明

> 一句话：**抓不到、打不着、跑不过你**。专为不爱恐怖游戏追杀环节的玩家准备
> （也是我们 recon 侦察员的保命工作服）。独立插件，和真结局模组互不影响。

## 三个开关（安装后默认全开）

配置文件：`<游戏目录>\BepInEx\config\com.wpch3.miside.godmode.cfg`（改后重启游戏生效）

| 配置项 | 默认 | 作用 |
|---|---|---|
| `GodMode` | `true` | **无敌**：拦截一切对玩家的伤害/处决/判负判定——电锯砍到、回廊被撞、铁锤砸到、射击中弹，统统不掉血不死 |
| `PacifyMitas` | `true` | **和平**：拦截米塔/怪物的攻击发起——疯米塔不抬手、怪物米塔不扑、人偶不动、电梯怪不动 |
| `SpeedMultiplier` | `1.6` | **移速倍率**（挂在 `WorldPlayer.speed`）：1 = 关闭；1.6x 跑赢一切追杀；想要"更无敌"就开 2~3 |

## 拦截覆盖面（来自静态报告全量盘点）

| 场景 | 原事件 | 处理 |
|---|---|---|
| Location6 电锯章 | `Location6_MitaKiller.Kill` / `PlayerLose` / `StartAttack` | 处决不发生、不判负、不抬手 |
| Location12 | `CreepyMitaAttack` / `CreepyMitaStayAttack` | 怪物米塔不袭击 |
| Location10 人偶检查站 | `ManekenAttack` | 人偶不扑 |
| Location11 电梯 | `LiftEnemy.Attack` | 电梯怪不攻击 |
| Location20 逃亡回廊 | `Damage` / `KillPlayerStart` | 撞不伤、杀不死 |
| Location7 铁锤区 | `PlayerDamage` | 砸不伤 |
| 射击段落 | `Shooter_Player.Damage` / `Kill` | 中弹不掉血 |

## 刻意不做的事（以及为什么）

- **不拦小游戏的 Lose**（贪吃蛇/扫雷类 PC 小游戏）：那些游戏靠"输了重试"循环，拦了会卡死；
- **不拦剧情演出锁**（`PlayerDontMove` 等）：剧情序列需要定身，拦了可能卡剧情；
- **不删恐怖氛围特效**：只保证你"安全"，镜头晃动/音效这些保留——实在怕的话把灯打开 😄。

## 万一卡剧情了怎么办

0. **追杀章出现"消失→回入口→轮回对话"的循环**（2026-08-26 实证，Location6 地下室电锯章）：
   日志实证机理——前两处躲藏点开挂全程 0 拦截记录（正常推进），第三处被发现后
   `StartAttack`（扑）→拦→`PlayerLose` 判罚刷屏→拦→`Kill`→拦……剧本收场动作全被没收，
   外部计时器最终强制重置 = 轮回。**结论：躲藏章的关键不是"打不死"，而是"别被发现"。**

   试验梯（逐级试，把结果报告给 agent）：
   1. `PacifyMitas=false`、`GodMode=true`、`SpeedMultiplier=2.5` —— 把躲藏当真玩：
      按戒指指引**准时进每个躲藏点**（2.5 倍速余量巨大），目标是日志里
      StartAttack/Kill/PlayerLose 一行都不出现 = 干净通过；万一被发现，
      她扑空（StartAttack 演完有收场）你硬吃不掉血，继续跑位重新躲；
   2. 仍轮回 → 纯速度模式：`GodMode=false`、`PacifyMitas=false`、`SpeedMultiplier=3` 过这一段
      （被抓就正常读检查点，只亏 30 秒），过完再开回来；
   3. 终极兜底：把 `MitaGodMode.dll` 挪出 plugins，过完本章再放回。
   回报哪一级过的 + 当时日志行 → v1.1 做章节适配（候选：Location6 只拦 Kill、放 PlayerLose，
   或改拦 `RestartGame` 链——需要实测数据定夺）。
1. 先把 `PacifyMitas` 改成 `false`（只留无敌）——"怪物不动手"理论上可能影响个别演出触发，概率很低；
2. 还不行就整个 `MitaGodMode.dll` 从 `BepInEx\plugins\` 里删掉，游戏立刻回到原版；
3. 顺便把卡住的场景名告诉我（`MitaTE-recon.log` 里有），我写进例外清单。

## 日志（隐藏的侦察副产品）

每次拦下致命事件都会写一行（3 秒节流）：

```
[GodMode] 已拦下【无敌】Location6_MitaKiller.Kill —— 原本这里要出事
```

这些记录 = 游戏全部"死因"的实锤清单，对 MitaTrueEnding 设计拦截点是免费情报。

## 卸载

删 `BepInEx\plugins\MitaGodMode.dll`（配置文件可留可删），无任何残留。
