# 米塔「全员拯救」真结局 Mod — 技术方案 v2

> **v2 修订说明**（v1 为 Gemini 起草，经逐项评估后修订）：
>
> - ✅ 保留 v1 的正确骨架：自定义 flag 驱动分支、Harmony 拦截剧情事件、终局复用和平模式思路、分阶段开发路线。
> - ❌ **修正工具链**：MiSide 是 **IL2CPP** 打包，v1 的 "dnSpy 反编译" 方案基本不成立（dnSpy 只能看到 interop 壳里的类名/方法名，看不到逻辑）。正确工具链见 [§3](#三技术栈il2cpp-版)。
> - ❌ **拆分拦截点**：米塔们的遇害分散在**不同章节、不同机制**，不是"地下底层一次处决"，必须拆成多个拦截点 + 终章统一结算，见 [§2](#二剧情与分支设计按章节拆分拦截点)。
> - ⚠️ **v1 示例代码中的类名/方法名（`CutsceneController.TriggerExecution`、`SaveManager.GetBool` 等）全部是占位符**，不是游戏里的真实类型。正式编码前必须完成"阶段 0：逆向侦察"（见 [§5](#五开发路线) 与 `docs/recon-checklist.md`）。
> - ➕ 新增 v1 缺少的内容：IL2CPP 下的协程写法、台词配音缺口（游戏全程有配音）、本地化注入、官方和平模式大更新的撞车风险。

---

## 一、Mod 概述

目标：**拦截主线各章节的"米塔遇害/被抹除"事件，让所有米塔在终章达成"数据和解"，按完成度解锁双结局（v3，用户 2026-08-26 定稿）：**

- **完美结局「全员存活」**：四位米塔全部获救，在游戏世界的修缮大屋里安稳生活；
- **真结局「现实·米塔后宫」**：全员存活 + 集齐两件伏笔道具（核心数据盘+修复补丁），米塔们的数据随主角进入现实——不是室友，是**全员女友**的真·后宫结局（修罗场日常预定）。

设计原则：

1. **不修改游戏原文件**：全部通过 BepInEx 插件在运行时注入。
2. **不污染原存档**：拯救进度写入独立 JSON（`BepInEx/config/MitaTrueEnding.rescue.json`），避免坏档、避免与游戏更新冲突。
3. **可优雅降级**：任何拦截点未被触发时，游戏完全按原版流程运行。

---

## 二、剧情与分支设计（按章节拆分拦截点）

> ⚠️ 下表事件描述基于社区共识整理，**阶段 0 需逐条对照游戏实际剧情核对**（尤其是各事件的具体触发函数与时机）。

| 章节 | 原版事件 | Mod 拦截方案 | 章节内条件 | 写入 flag |
|---|---|---|---|---|
| 循环回廊 | 小米塔被疯米塔虐待、困在循环中 | 终止循环、带离 | 章节内隐藏条件（阶段 1 按 recon 定，如事件前交互特定物件） | `RescuedTinyMita` |
| 2D 世界 | 米拉的世界被疯米塔抹除/崩塌 | 在崩塌前备份世界数据 | 同上 | `RescuedMila` |
| 帽子米塔（卡比） | 遭疯米塔毒手 | 提前完成备份 → 事件被改写 | 同上 | `RescuedCappie` |
| 核心区域（终章） | 善良米塔被疯米塔杀害 | 主角介入阻拦，触发特殊对话"共享世界权限" | 前三位已获救（`RescuedTinyMita && RescuedMila && RescuedCappie`） | `RescuedKindMita` |

> 💡 v3 双结局改动（用户定稿）：两件伏笔道具**不再卡个别拯救**——否则拿不到道具就凑不齐
> "全员存活"的完美结局。道具的职责改为开启**真结局·现实线**的钥匙；
> 个别拯救只看各自章节内的条件，拯救链一个不能断（断了就回到原版悲剧线，零痕迹）。

**伏笔收集**（沿用 v1 思路）：

- 前期章节加入隐藏互动物品，如二楼房间的"未损坏的核心数据盘"、掌机小游戏隐藏关卡通关奖励"修复补丁"；
- 剧情定位：数据盘 = 把米塔们的数据带出游戏世界的**载体**；修复补丁 = 让她们在现实中**稳定存在**的修复程序。集齐 → 解锁"回现实"的资格。

**终局结算（双结局）**：

| 结局 | 触发条件 | 演出 |
|---|---|---|
| **完美结局「全员存活」** | `AllRescued`（四位 flag 全 true） | 场景重置为修缮一新的大房子：疯狂米塔在客厅做饭、善良米塔在沙发看书、小米塔在地毯上玩耍；米拉（2D）放进**掌机/电视屏幕**互动（RenderTexture，省掉 2D→3D 的巨大工作量，v1 最聪明的设计）——米塔们在游戏世界里迎来黎明 |
| **真结局「现实·米塔后宫」** | `AllRescued && HasCoreDataDisk && HasRepairPatch` | 复用 `Scene 1 - RealRoom`（现实房间）：主角把数据盘接入现实终端，米塔们的数据实体化走进现实。**王道后宫收尾**：全员以恋人身份入住主角家——疯狂米塔宣布"主角的所有权"被全员围攻（毒舌归毒舌，疯的根治好后她只是最黏人的那个）、善良米塔红着脸给大家分早餐、帽子米塔霸占联机手柄要和主角双排、米拉在**现实电视屏幕**里傲娇吃醋"凭什么我只能在屏幕里"；小米塔是全员投票的**团宠妹妹**（恋爱候补席永远不对她开放，谁提谁面壁）。片尾定格：主角被淹没在争坐他身边的修罗场里——拯救世界的报酬，就是这份甜蜜的焦头烂额 |
| 原版结局 | 上述均不满足 | 按原版运行，**零痕迹** |

- 同一周目两结局都满足时：在终章终点（候选 `Core_Entry`）做**分歧选择点**（两扇门/两个终端选项）——
  向左：留在这个世界与她们告别（完美）；向右：带她们回现实开后宫（真）。实现细节阶段 2 定。
- 判定常量见 `RescueState.cs`：`PerfectEndingUnlocked` / `TrueEndingUnlocked`。

**v3.1 终修改设定（用户 2026-08-26 定稿）**：

- **无损失原则**：我们的结局里没有任何人是"失败之作"——破损可以修复、疯狂可以治愈、
  记忆必须保全，全员以完整姿态迎来结局。"到时候谁都不能有损失"。
- **机械降神许可证**：允许光明正大地机械降神（数据盘/修复补丁就是干这个的），
  但所有超展开必须穿原版设定的外衣（数据/版本/复制/修复/再生），不发明违和的新概念。
- **小米塔必须是完好的**：循环回廊拯救 = 终止循环 + **修复她的再生能力**（原版她因疯米塔
  而失去再生能力、左臂缺失）；结局以**完好形态**登场。模型来源双轨——优先联动现成的
  "修复小米塔"模组（用户联系作者中），否则基于 **MS_CustomModels** 管线自制
  （机器上已装 4.0.4，天然兼容；该管线还能服务其他角色的结局换装需求）。

---

## 三、技术栈（IL2CPP 版）

| 用途 | 工具 | 说明 |
|---|---|---|
| Mod 框架 | **BepInEx 6**（IL2CPP / CoreCLR bleeding edge） | MiSide mod 社区事实标准，Nexus 上所有代码类 mod 均基于它 |
| 运行时补丁 | HarmonyX | BepInEx 自带 0Harmony.dll，无需玩家另行安装 |
| 运行时侦察 | **UnityExplorer**（yukieiji fork，文件名必须带 `.Unity.`：`UnityExplorer.BepInEx.Unity.IL2CPP.CoreCLR.zip`） | 场景树浏览、组件检查、方法试调；be.577+ 下不带 `.Unity.` 的旧版会被静默跳过（见 `docs/unityexplorer-fix.md`） |
| 类名/方法名检索 | dnSpy 打开 `BepInEx/interop/Assembly-CSharp.dll` | ⚠️ **只看得到签名，看不到方法体** |
| 逻辑逆向 | **Il2CppDumper + Ghidra/IDA**，或 Cpp2IL | 找剧情调用链、存档结构、结局判定 |
| 资源提取 | AssetRipper | 动画剪辑清单、prefab、台词表 |
| 资源制作 | **Unity 2021.3.35f1**（与游戏同版本） | 自制 AssetBundle / 模型替换必须用匹配版本，版本不符加载会炸 |
| 参考实现 | Miside Custom Models Loader（Nexus） | 已证实 kind_mita / mila / crazy_mita 等模型资源可定位替换 |

## 四、实现要点（修订版）

### 4.1 拯救进度：独立存档，不碰游戏存档（已实现）

- 代码已内置：`src/MitaTrueEnding/RescueState.cs`
- 读写 `BepInEx/config/MitaTrueEnding.rescue.json`；游戏本体存档仅用于**触发时机**（如：在读档点/交互点挂 hook），不做写入。

### 4.2 事件拦截：Harmony Prefix 改写分支

拦截骨架（完整模板见 `src/MitaTrueEnding/Templates/RescuePatchTemplate.cs.txt`）：

```csharp
// ⚠️ 类型名/方法名是占位符，阶段 0 逆向确认后再替换
// [HarmonyPatch(typeof(KindMitaCutscene), "PlayExecution")]
public static bool Prefix(/* CutsceneController */ object __instance)
{
    if (Plugin.Rescue.TrueEndingUnlocked)
    {
        // 拦截原版遇害演出，启动自制拯救流程
        // IL2CPP 注意：托管协程要 .WrapToIl2Cpp() 后再 StartCoroutine
        // （using BepInEx.Unity.IL2CPP.Utils.Collections）
        return false;                       // 跳过原版逻辑
    }
    return true;                            // 放行原版
}
```

**关键差异（v1 没提）**：IL2CPP 下 `StartCoroutine(托管 IEnumerator)` 不能直接用，需要 `using BepInEx.Unity.IL2CPP.Utils.Collections;` 提供 `WrapToIl2Cpp()` 扩展把 `System.Collections.IEnumerator` 包装成 `Il2CppSystem.Collections.IEnumerator`。

候选拦截层（按优先级，阶段 0 确认实际位置）：

1. 结局判定/场景切换函数（改结局指向）— 通常最稳；
2. 剧情触发器（Trigger / Cutscene Controller）— 拦截单点事件；
3. Timeline / PlayableDirector 回调 — 如果游戏过场用 Timeline 的话。

### 4.3 对话与本地化

- **文本注入**：游戏文本走本地化表，新台词需注入本地化条目 + Hook 对话显示函数按序喂入自定义文本 ID；
- **演出**：复用现有 Mita 的 Animator 状态（阶段 0 用 UnityExplorer 核对动画剪辑清单，`animator.Play(...)` 的状态名必须先确认存在）；
- **配音缺口（v1 完全没提）**：游戏全程有配音，自制台词会无声。二选一并写入设计文档：
  - 方案 A：静音字幕（零成本，沉浸感打折）；
  - 方案 B：AI 配音（TTS 克隆声线，注意版权与伦理，发布时在 Nexus 页面显著标注）。
- **v1 决策：方案 A**（方案 B 留作阶段 3 可选项，届时用户拍板）。
- **文案外置（新增）**：mod 全部台词放外部 JSON（`BepInEx/config/MitaTrueEnding.dialogue.json`），
  改文案不用重新编译；v1 简体中文，结构预留多语言 key。
  台词风格唯一指定参考：[fandom 全对话文本](https://miside.fandom.com/wiki/Dialogues/English)。

### 4.4 终局场景多实体加载

- 复用/仿制和平模式场景；加载后用 `GameObject.Instantiate` 生成善良米塔、小米塔的模型 prefab（模型资源名参考 Nexus 的 Custom Models Loader 替换目录：`kind_mita`、`mila`、`crazy_mita` 等）；
- 挂简单碰撞体 + 交互脚本，主角靠近触发各角色独立日常对话；
- 米拉不生成 3D 实体：走掌机/电视的 RenderTexture 方案；
- 注意导航网格（NavMesh）与碰撞，避免米塔们穿模卡墙。

## 五、开发路线

| 阶段 | 内容 | 验收 |
|---|---|---|
| **0. 侦察**（新增，v1 缺失） | 装 BepInEx 6 IL2CPP + UnityExplorer；生成 interop；dump 类结构；按 `docs/recon-checklist.md` 逐项核对；**产出「剧情事件 → 真实类/方法 → 拦截方式」对照表** | 对照表填完，hello patch 能进游戏日志 |
| 1. 单点闭环 | **只做善良米塔终章一条线**：拦截 → 写 flag → 自定义文本 → 跳和平场景 | 跑一次游戏完整触发，无崩溃 |
| 2. 全章节 + 终局场景 | 其余三个拦截点；终局场景多实体生成与站位；掌机里的米拉；**完好小米塔登场**（联动模组 or MS_CustomModels 自制）；**调试旗标**（config 强制设置拯救 flag，测试演出不必每次全收集） | 新档全收集进入真结局 |
| 3. 润色 | 和解过场运镜、音效、日常交互指令、（可选）AI 配音 | 发布候选版 |

## 六、风险与注意事项

1. **官方更新撞车**：官方"和平模式"大更新仍在开发（原计划 2026 上半年、已跳票；2026-06 社区汇总称含"拯救米塔"的好结局已制作完成、发布时间未定，可能与 2026 冬~2027 的最终更新一起到来）。
   - 差异化定位：我们走"**玩家主动修复 + 全员和解**"路线，与官方以疯狂米塔为中心的和平模式不同；
   - 大更新后游戏类结构必然变化，mod 需要跟进适配 —— 阶段 0 的对照表就是为快速重构准备的。
2. **工作量预期**：IL2CPP 逆向 + 剧情演出拼装属于"总转换"级 mod，按**月**预算，单人开发建议先保住阶段 1 的可玩 demo。
3. **版权合规**：不内置任何游戏资源；interop 程序集由开发者在各自机器上用 BepInEx 生成（见 `lib/interop/README.md`），仓库可安全保持公开；对外发布 mod 时只发布 `MitaTrueEnding.dll`，不附带任何游戏资源。
4. **拦截补偿（新增）**：原版被拦事件可能顺带写了剧情进度标记（`World.SaveStoryMita/SaveStoryCartridge` 等）——
   拦截后若不补偿调用，"继续游戏/读档"进度链可能断裂。每拦一个点都要核对并补偿（已列入 0-3 追加核对项）。
5. **多 mod 共存（新增）**：与 Better Movement / PhotoMode / MS_CustomModels（及将来联动的修复小米塔模组）
   共存是常态；本 mod 只加不改、无冲突面。外部模组到货先跑 `tools/Test-PluginCompat.ps1` 验明正身再装。

## 七、构建与安装（本仓库）

**前置**：

1. **.NET SDK ≥ 6.0**（建议 .NET 8 LTS）。
   - PowerShell 里运行 `dotnet --list-sdks` 检查；如果最高只到 3.x（报错信息里"生成引擎版本 16.3"就是 .NET Core SDK 3.0），**无法编译本项目**，请先安装新版：
     ```powershell
     winget install Microsoft.DotNet.SDK.8
     ```
     装完**关闭并重开 PowerShell**，用 `dotnet --version` 验证。
2. MiSide 游戏本体；已安装 BepInEx 6（IL2CPP bleeding edge）并运行过一次游戏（首次运行会生成 interop）。

```powershell
# 1. 把 interop 拷进本机仓库目录（每台开发机做一次，dll 不入库）：
#    复制 <游戏目录>/BepInEx/interop/*.dll  →  <本仓库>/lib/interop/
#    （详细步骤见 lib/interop/README.md）

# 2. 在仓库根目录直接编译（根目录自带 MitaMod.sln，不要先 cd 到别处）：
dotnet build -c Release

# 3. 部署（二选一）：
#    手动：复制 src/MitaTrueEnding/bin/Release/net6.0/MitaTrueEnding.dll → <游戏目录>/BepInEx/plugins/
#    脚本：./tools/Build-And-Deploy.ps1 -MiSideDir "D:\SteamLibrary\steamapps\common\MiSide"
```

常见报错速查：

| 报错 | 原因 | 解决 |
|---|---|---|
| `error MSB1003: 请指定项目或解决方案文件` | 在不含 .sln/.csproj 的目录执行了 `dotnet build` | 确认当前目录是仓库根（应有 `MitaMod.sln`）；若 ZIP 是旧的 main 分支版（只有 README），请重新下载 arena 分支 |
| `error NETSDK1045: 当前 .NET SDK 不支持将 .NET 6.0 设置为目标` | SDK 版本太旧（如 3.x） | 按上方前置第 1 条安装 .NET SDK 8 |
| restore 找不到 `BepInEx.Unity.IL2CPP 6.0.0-be.*` | nuget.bepinex.dev 源未生效 | 确认 `NuGet.config` 在仓库根目录；或把 csproj 中版本改为 [builds.bepinex.dev](https://builds.bepinex.dev/projects/bepinex_be) 上的具体版本号 |

运行后日志在 `<游戏目录>/BepInEx/LogOutput.log`，进度存档在 `<游戏目录>/BepInEx/config/MitaTrueEnding.rescue.json`。

## 八、仓库结构

```
├── README.md                        # 本方案
├── MitaMod.sln                      # 解决方案：仓库根目录可直接 dotnet build
├── NuGet.config                     # 添加 nuget.bepinex.dev 源
├── docs/recon-checklist.md          # 阶段 0 逆向侦察清单（含待填对照表）
├── src/MitaTrueEnding/              # BepInEx 6 IL2CPP 插件工程
│   ├── MitaTrueEnding.csproj
│   ├── Plugin.cs                    # 插件入口
│   ├── RescueState.cs               # 拯救进度（独立 JSON 存档）
│   └── Templates/                   # 补丁/演出模板（.txt，不参与编译，等阶段 0 落实）
├── lib/interop/                     # 游戏 interop 程序集（不入库，本机生成，见目录内 README）
└── tools/Build-And-Deploy.ps1       # 一键编译 + 部署到游戏 BepInEx/plugins（Windows）
```

## 九、参考链接

- BepInEx：https://github.com/BepInEx/BepInEx ｜ NuGet feed：https://nuget.bepinex.dev
- MiSide mod 社区：https://www.nexusmods.com/miside
- 新手 MiSide mod 记录（含 IL2CPP/UnityExplorer 版本提醒）：https://www.reddit.com/r/Modding/comments/1hq10al/
- IL2CPP 逆向指引（Interop/dnSpy/Il2CppDumper 章节）：https://hackmd.io/@ManlyMarco/Sy6BcHJQt
- Miside Custom Models Loader（Unity 2021.3.35 + 模型替换流程）：https://www.nexusmods.com/miside/mods/57
- 和平模式情报汇总（2026-06）：https://www.reddit.com/r/MiSideReddit/comments/1uj1ia8/
