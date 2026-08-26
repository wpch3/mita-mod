# 阶段 0：逆向侦察清单（Recon Checklist）

> 输入：MiSide 游戏 + BepInEx 6 IL2CPP + UnityExplorer（BepInEx IL2CPP CoreCLR 版）+ dnSpy + Il2CppDumper。
> 产出（本阶段唯一交付物）：**下方对照表全部填实**，并有一个能在游戏日志里打印的 hello patch。
> ⚠️ 不搞完本阶段不要写剧情补丁 —— v1 Gemini 方案里的类名全是占位符。
> 📖 剧情侧基线：原版剧情/三结局/角色遭遇已固化在 `docs/vanilla-plot-baseline.md`（社区资料+置信度标记），
> 写任何拦截/台词前必读；doc 中每条 🔴 待办都靠本清单的动态侦察数据实锤。
>
> 📌 进度：**静态侧已基本完成**（0-1.6 ReconDump 报告已入库 `docs/recon-reports/`，结论见文末 §0-7）。
> 剩余：动态侧场景名对照（边玩边跑 0-1.5 收集的 `MitaTE-recon.log`）。

## 0-1 环境搭建

- [x] BepInEx 6（IL2CPP / CoreCLR bleeding edge）装进游戏根目录，运行一次，生成 `BepInEx/interop/`（2026-08-25 完成，be.785，确认 Unity 2021.3.35f1 + .NET 6）
- [x] interop DLL 拷到 `lib/interop/`，`dotnet build` 通过，插件日志出现 `[MitaTE]`（v0.1.0 验证）
- [ ] UnityExplorer：去 **yukieiji fork** 发布页选文件名带 **`.Unity.`** 的版本：`UnityExplorer.BepInEx.Unity.IL2CPP.CoreCLR.zip`（⚠️ BepInEx be.577+ 把插件基类程序集改名 `BepInEx.Unity.IL2CPP`，不带 `.Unity.` 的旧 zip 会被**静默跳过**——2026-08-26 已用 strings 实锤验证，见 `docs/unityexplorer-fix.md`）dll 放进 `BepInEx/plugins/`，进游戏按 **F7** 打开
- [ ] （可选）dnSpy 打开 interop 的 `Assembly-CSharp.dll` —— 常规搜索已被 0-1.6 的 ReconDump 替代，dnSpy 只留作深挖单个类时用；Il2CppDumper + Ghidra 推后到需要看方法逻辑时再用
- [x] hello patch/部署链路验证（由 v0.2.0 内置 Recon 组件接管，见 0-1.5）

## 0-1.5 内置侦察：边玩边自动收集（v0.3.0 起全自动快照）

部署新版插件后**正常玩游戏即可，不需要按任何键**：

- **每次场景切换** → 追加时间序日志 `<游戏目录>/BepInEx/config/MitaTE-recon.log`
  （时间戳 + 场景名 + 场景路径 + 根对象数 + 加载模式；也同步进主日志，前缀 `[MitaTE-Recon]`）
- **每次场景切换后 1.5s 与 8s** → 自动抓取该场景的根对象清单，写入
  `<游戏目录>/BepInEx/config/MitaTE-scene-snapshots.log`：
  - 规则 = **同场景覆盖、异场景保留**（重复到访刷新最新状态，去过的场景一个不落；
    恐怖场景忘记手动操作也完全没关系）
  - 两次抓取是为了等物体/UI 延迟生成（8s 那张覆盖 1.5s 那张）
  - 跨游戏会话合并：重启游戏会读回旧快照继续累积
- **F9**（可选兜底）：立即对当前全部已加载场景补抓一张快照
- **过场雷达**（v0.4.0 起，`Patches/CutsceneRadar.cs`）：游戏里每触发一段剧情演出
  （`Playable_Animation.Play` / `PlayAsset`），自动把 **Timeline 资源名 + 场景名 + 挂载物体名**
  追加进 `MitaTE-recon.log` —— 这是锁定「处决过场资源名」（§0-3 表格第 2 列）的直接数据来源。
  纯侦察不拦截；通关后跟着日志一起交给 Agent 即可。

场景名格式已确认：`Scene <章节号> - <代号>`（例：`Scene 3 - WeTogether`）——
通关一遍即可拿全"章节号 ↔ 场景名"对照。

建议流程：

1. 关掉游戏 → Git 拉取最新代码 → 重跑 `tools/Build-And-Deploy.ps1 -MiSideDir "<游戏目录>"` → 重开游戏；
2. 从头通关（**不用管 F9**），插件自动记录一切。怕追杀的先在 plugins 里放上 **MitaGodMode**
   （无敌+怪物不攻击+1.6 倍速，见 `docs/godmode.md`）——不影响侦察，拦截日志还能补死因表。结局采集优先级（2026-08-26 定）：
   - **必须·卡带结局**：走标准主线到底（衣柜事件选"继续查真相/离开"）—— 全章节场景 + 处决过场资源名全靠它；
   - **推荐·自杀结局**（顺手）：任一含保险箱的章节输 **4970** 拔卡带即可，约 10 分钟，
     **不需要完整二周目**；它提供"原版结局演出 → 回标题"的完整收口流程，我们的双结局收口就仿它；
   - **暂缓·留下结局**：它在衣柜事件**提前终结游戏**且要 6 个条件（含"已通关一次"），与主线采集冲突；
     等以后做和平模式菜单联动时再单开一局（条件已知，约 15-20 分钟），现在不用打。
3. 通关后把 `MitaTE-recon.log` 和 `MitaTE-scene-snapshots.log` 一起发给 Agent。

## 0-1.6 静态扫描：一条命令代替 dnSpy 手工搜索（ReconDump）

```powershell
./tools/Recon-Scan.ps1
```

用 MetadataLoadContext **只读**解析 `lib/interop/Assembly-CSharp.dll`（不执行任何游戏代码），
在 `recon/` 目录生成：

- `assembly-csharp.all-types.txt` —— 游戏全部类型清单（逐行全名，可随便 grep）
- `assembly-csharp.keyword-report.txt` —— 按关键词分组命中的类型 + 方法签名 + 字段，
  覆盖：存档 / 对话 / 过场 / 结局 / 死亡重置 / 场景章节 / 角色 / 核心终端 / 交互物品 / 管理器

把这两个文件（至少 keyword-report）发给 Agent，即可开始填 §0-3 对照表。
`recon/` 已加入 .gitignore，不会被误提交。

> 以后想看某个方法的具体逻辑时，再开 dnSpy（只能看签名）或上 Il2CppDumper + Ghidra。

## 0-2 侦察对象清单

| # | 目标 | 要找什么 | 首选工具 | 状态 |
|---|---|---|---|---|
| 1 | 存档系统 | 存档类、读写方法、flag/变量存储格式（只调研，不写入） | ReconDump + Ghidra | ✅ 静态定位：`Scene_Load.SaveGame()/SilentSave(int)`、`World.SaveStoryMita/SaveStoryCartridge`；flag 存储结构待定 |
| 2 | 对话系统 | 对话显示函数、台词 ID 与头像映射、本地化表位置与格式 | ReconDump + 运行时 | ✅ 静态定位：`GameController.PrintDialogue(Dialogue_3DText,bool)`（显示）、`DialogueAdd(Dialogue_3DText,string)`（喂文本）、`DialogueChangerStart`；自定义台词可直接走 DialogueAdd，本地化表注入备选 |
| 3 | 场景清单 | 全部关卡场景名；终章核心区域场景名；和平模式场景名 | MitaTE-recon.log | 🔄 待动态：玩通关后从日志把 LocationN ↔ 场景名 对上 |
| 4 | 掌机小游戏 | 掌机交互对象与得分/通关事件 | 运行时 | 🔄 候选类型已见：`MinigamesTelevisionController`（CanTalkAboutGame/TalkReadyListener）、`GamesCore_Main`（核心小游戏） |
| 5 | 结局判定 | 结局选择/场景跳转函数（阶段 1 的首选拦截层） | ReconDump + Ghidra | ✅ 静态候选：`Scene_Load.GoScene/SaveGame`、`Menu.NextLocation` 流程；参考点 `Basement_SafeConsole.TakeCartridge`（自杀结局） |

## 0-3 剧情事件 → 拦截点对照表（核心交付物）

逐条对照游戏实际剧情核对"原版事件"列；把触发链反推到可 patch 的方法。
（2026-08-26 静态侧已填入 ReconDump 确认的候选；🟡 = 待动态确认精确触发点）

| 章节 | 原版事件（待核对） | 触发链：类.方法（静态候选） | 拦截方式（Prefix 改写 / 结局函数改向） | 写入 flag | 状态 |
|---|---|---|---|---|---|
| 循环回廊 | 小米塔被困循环 | `Location8_InfinityRoom`（TalkWindow/循环房间逻辑）、`Location8_MitaBrokeLife`（受损小米塔） | 🟡 待动态锁定具体事件方法 → Prefix 打断循环 + 带离演出 | `RescuedTinyMita` | 🟡 静态✓ |
| 2D 世界 | 米拉世界崩塌/被抹除 | `Location18_Novella`（PlayDialogue/NextDialogue/UpdateDialogue）、`Location18_Mita`；"崩塌"大概率是 `Playable_Animation` 过场 | 🟡 拦截崩塌触发（过场或流程方法）→ "备份世界数据"替代演出 | `RescuedMila` | 🟡 静态✓ |
| 卡比 | 遭疯米塔毒手 | `Location7_MitaCapRepeat`、`Location7_RingWork.DialogueAngryCap` | 🟡 同上思路 | `RescuedCappie` | 🟡 静态✓ |
| 终章·核心区域 | 善良米塔被杀 | `Location15` + `Location15_MitaKind_Follow`（GoSit/FollowStop）+ `Location15_ScreenID`（编号输入）+ `Core_Entry`（核心门 OnTriggerEnter/DoorClick）；死亡推定为 `Playable_Animation.Play/PlayAsset` 过场 | **首选层：Prefix 拦截 `Playable_Animation.PlayAsset(PlayableAsset)`，按 asset 资源名判定是否处决过场** → 改播自制和解演出 | `RescuedKindMita` | 🟡 静态✓ 动态重 |
| 终局 | 结局结算/场景跳转 | `Scene_Load.GoScene/SaveGame`、`World.isContinue`/`eventContinueScene`、`MenuNextLocation.Click` | `TrueEndingUnlocked` → 改向自制真结局场景；否则全放行 | — | 🟡 静态✓ |
| （参考）自杀结局 | 拔卡带自删 | `Basement_SafeConsole.TakeCartridge()` + `DataMoshActive/ExitGame` | 不改，仅作"改写结局流向"的现成参考 | — | ✅ |

**动态核对追加（2026-08-26）：**

- 🟡 补偿检查：每个候选拦截事件被跳过/改写后，原版本该调用的剧情进度标记
  （`World.SaveStoryMita` / `SaveStoryCartridge` 等）还会不会发生？若不会，拦截补丁必须**补偿调用**，
  否则"继续游戏/读档"的进度链可能断裂。
- 🟡 存档点机制：`Scene_Load.SilentSave(int)` 的 id 清单（存档点 ↔ 章节映射）——
  后续测试演出、复看结局全靠它跳关。
- 🟡 `MenuEnding` 与"和平模式"菜单解锁逻辑（留下结局回主界面后出现）—— 日后做分歧点/菜单联动用，暂缓。

## 0-4 伏笔触发点

| 伏笔 | 隐藏位置 | 交互对象 / 触发方法 | 写入 flag | 状态 |
|---|---|---|---|---|
| 未损坏的核心数据盘 | 二楼房间 / 特定柜子 | `<待填>` | `HasCoreDataDisk` | ☐ |
| 修复补丁 | 掌机隐藏关卡 | `<待填>` | `HasRepairPatch` | ☐ |

## 0-5 终局素材盘点

- [ ] 各米塔模型/预制体资源名（参考 Nexus Custom Models Loader 的替换目录：`crazy_mita`、`kind_mita`、`mila`……）
- [ ] 各米塔 Animator 的可用状态/剪辑名（和解演出能复用哪些：对话、惊讶、走路、表情……）
- [ ] 和平模式场景的厨房/客厅布局节点（终局站位用）
- [ ] 掌机/电视屏幕材质与 RenderTexture 挂点（米拉的终局呈现方式）
- [ ] 台词表本地化：注入中文/英文新条目的位置
- [ ] 配音：确认 Silence 方案或评估 AI 配音素材清单

## 0-6 验收标准

1. hello patch 在 `BepInEx/LogOutput.log` 正常输出；✅（v0.2.1）
2. 0-3 对照表每一格填实（类名、方法名、签名、调用方）；🟡（静态部分已填，调用方待 Ghidra/运行时）
3. 明确每个米塔的"最小可行拯救演出"需要哪些资源（动画状态名、台词 ID、声音），全部能在游戏里找到；
4. 记录游戏版本号与构建哈希（后续游戏更新后用于快速判断失效点）。

## 0-7 静态侦察结论汇总（2026-08-26，源自 ReconDump 报告 `docs/recon-reports/`）

**已确认的骨架（写补丁可直接引用）：**

- **中枢 `GameController`**：`CutscenePlay(Transform)` / `CutsceneStop()`；
  `DialogueChangerStart(DialogueChanger)`、`PrintDialogue(Dialogue_3DText,bool)`、
  `DialogueAdd(Dialogue_3DText,string)`（**直接喂自定义台词的现成口**）；
  `AddKeyItem/RemoveKeyItem/GetKeyItem(GameObject)`（**现成道具系统，伏笔物直接挂**）
- **过场系统**：`Playable_Animation.Play()` / `PlayAsset(PlayableAsset)`（拦截首选，
  有 `eventStart/eventStop` UnityEvent）、`World.cutsceneStart`、
  `World.eventStart/eventContinueScene/eventFirstStart`
- **米塔基类 `MitaPerson`**：`animMita`（Animator）+ `FaceEmotion(string)/FaceEmotionFast`
  （表情库！）+ `AiWalkToTarget(Transform,UnityEvent)`、`MagnetToTarget`、`MitaTeleport`
  （**和解演出的走位/表情工具全套现成**）
- **存档与流程**：`Scene_Load.SaveGame()/SilentSave(int)/GoScene()`、
  `World.SaveStoryMita/SaveStoryCartridge/isContinue`
- **终章（Location15 = 核心章节）**：`Location15_ScreenID`（角色界面编号输入）、
  `Location15_MitaKind_Follow`（善良米塔跟随+坐下）、`Core_Entry`（核心门）、
  `Core_Chair/Core_Life/CoreScreens`（核心装置）、`MitaCore`（纯骨骼跟随，无剧情逻辑）
- **2D 世界（Location18）**：`Location18_Novella`（视觉小说控制器 PlayDialogue/NextDialogue）、`Location18_Mita`（米拉）
- **卡比（Location7）**：`Location7_MitaCapRepeat`、`Location7_RingWork.DialogueAngryCap`
- **循环回廊（Location8）**：`Location8_InfinityRoom`、`Location8_MitaBrokeLife`
- **终局场景候选**：`Location21`（Cooking/Eating 厨房+对话）与 `Location34`（Communication/Glasses/PositionForMita）
- **其他**：`Time_Events`（YieldRestart 系列，时机事件引擎）、`Location6_MitaKiller`（追杀米塔）、
  `ConsoleCommandsGame`（内置作弊控制台，调试可参考）

**仍待动态确认：** 各 LocationN ↔ 场景名/章节对照；处决过场的确切 `PlayableAsset` 资源名；
`Time_Events` 在结局链中的角色；亲测回撤能否用 `Playable_Animation` 单独拦下一段过场而不牵连同场景其他过场。
