# 阶段 0：逆向侦察清单（Recon Checklist）

> 输入：MiSide 游戏 + BepInEx 6 IL2CPP + UnityExplorer（BepInEx IL2CPP CoreCLR 版）+ dnSpy + Il2CppDumper。
> 产出（本阶段唯一交付物）：**下方对照表全部填实**，并有一个能在游戏日志里打印的 hello patch。
> ⚠️ 不搞完本阶段不要写剧情补丁 —— v1 Gemini 方案里的类名全是占位符。

## 0-1 环境搭建

- [ ] BepInEx 6（IL2CPP / CoreCLR bleeding edge）装进游戏根目录，运行一次，生成 `BepInEx/interop/`
- [ ] interop DLL 拷到 `lib/interop/`，`dotnet build` 通过
- [ ] UnityExplorer（BepInEx IL2CPP CoreCLR 版）能进游戏按快捷键打开
- [ ] dnSpy 打开 interop 的 `Assembly-CSharp.dll`（只看签名）；Il2CppDumper 生成 `dump.cs` / Ghidra 工程
- [ ] 编译一个 hello patch：日志打印当前场景名（练手 + 验证部署链路）

## 0-2 侦察对象清单

| # | 目标 | 要找什么 | 首选工具 | 状态 |
|---|---|---|---|---|
| 1 | 存档系统 | 存档类、读写方法、flag/变量存储格式（只调研，不写入） | dnSpy / UnityExplorer | ☐ |
| 2 | 对话系统 | 对话显示函数、台词 ID 与头像映射、本地化表位置与格式 | UnityExplorer / AssetRipper | ☐ |
| 3 | 场景清单 | 全部关卡场景名；终章核心区域场景名；和平模式场景名 | UnityExplorer（Scene 面板） | ☐ |
| 4 | 掌机小游戏 | 掌机交互对象与得分/通关事件 | UnityExplorer | ☐ |
| 5 | 结局判定 | 结局选择/场景跳转函数（阶段 1 的首选拦截层） | Il2CppDumper + Ghidra | ☐ |

## 0-3 剧情事件 → 拦截点对照表（核心交付物）

逐条对照游戏实际剧情核对"原版事件"列；把触发链反推到可 patch 的方法。

| 章节 | 原版事件（待核对） | 触发链：类.方法 | 拦截方式（Prefix 改写 / 结局函数改向） | 写入 flag | 状态 |
|---|---|---|---|---|---|
| 循环回廊 | 小米塔被困循环 | `<待填>` | `<待填>` | `RescuedTinyMita` | ☐ |
| 2D 世界 | 米拉世界崩塌/被抹除 | `<待填>` | `<待填>` | `RescuedMila` | ☐ |
| 卡比 | 遭疯米塔毒手 | `<待填>` | `<待填>` | `RescuedCappie` | ☐ |
| 终章·核心区域 | 善良米塔被杀 | `<待填>` | `<待填>` | `RescuedKindMita` | ☐ |
| 终局 | 结局结算/场景跳转 | `<待填>` | TrueEndingUnlocked → 进自制结局；否则放行 | — | ☐ |

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

1. hello patch 在 `BepInEx/LogOutput.log` 正常输出；
2. 0-3 对照表每一格填实（类名、方法名、签名、调用方）；
3. 明确每个米塔的"最小可行拯救演出"需要哪些资源（动画状态名、台词 ID、声音），全部能在游戏里找到；
4. 记录游戏版本号与构建哈希（后续游戏更新后用于快速判断失效点）。
