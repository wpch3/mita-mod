# 阶段 0：逆向侦察清单（Recon Checklist）

> 输入：MiSide 游戏 + BepInEx 6 IL2CPP + UnityExplorer（BepInEx IL2CPP CoreCLR 版）+ dnSpy + Il2CppDumper。
> 产出（本阶段唯一交付物）：**下方对照表全部填实**，并有一个能在游戏日志里打印的 hello patch。
> ⚠️ 不搞完本阶段不要写剧情补丁 —— v1 Gemini 方案里的类名全是占位符。

## 0-1 环境搭建

- [x] BepInEx 6（IL2CPP / CoreCLR bleeding edge）装进游戏根目录，运行一次，生成 `BepInEx/interop/`（2026-08-25 完成，be.785，确认 Unity 2021.3.35f1 + .NET 6）
- [x] interop DLL 拷到 `lib/interop/`，`dotnet build` 通过，插件日志出现 `[MitaTE]`（v0.1.0 验证）
- [ ] UnityExplorer（发布页选 **BepInEx IL2CPP CoreCLR** 版：`UnityExplorer.BepInEx.IL2CPP.CoreCLR.zip`）dll 放进 `BepInEx/plugins/`，进游戏按 **F7** 打开
- [ ] （可选）dnSpy 打开 interop 的 `Assembly-CSharp.dll` —— 常规搜索已被 0-1.6 的 ReconDump 替代，dnSpy 只留作深挖单个类时用；Il2CppDumper + Ghidra 推后到需要看方法逻辑时再用
- [x] hello patch/部署链路验证（由 v0.2.0 内置 Recon 组件接管，见 0-1.5）

## 0-1.5 内置侦察：边玩边自动收集（v0.2.0 起插件自带 `ReconBehaviour`）

部署新版插件后正常玩游戏即可，无需任何额外操作：

- **每次场景切换** → 追加写入 `<游戏目录>/BepInEx/config/MitaTE-recon.log`
  （时间戳 + 场景名 + 场景路径 + 根对象数；也同步进主日志，前缀 `[MitaTE-Recon]`）
- **游戏内按 F9** → 把当前场景的根对象清单（名字 + 开关状态）写进同一日志
  → 走到关键位置（地下底层 / 核心区域 / 掌机旁 / 二楼房间 / 循环回廊 / 2D 世界）就按一下 F9，
  留下"当时场景里有哪些关键物体"

建议流程：

1. 关掉游戏 → Git 拉取最新代码 → 重跑 `tools/Build-And-Deploy.ps1 -MiSideDir "<游戏目录>"` → 重开游戏；
2. 读取**最接近后期的存档**（或新档速通），把 §0-3 对照表涉及的章节各走一遍；
3. 结束后把 `MitaTE-recon.log` 整个发给 Agent —— 它就是填下方场景清单和拦截点的一手数据。

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
