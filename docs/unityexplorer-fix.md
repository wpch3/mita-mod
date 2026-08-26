# UnityExplorer 不加载：实锤原因与修复（2026-08-26）

## 一句话结论

你装的 UnityExplorer 是**旧命名版本**（v4.9.0），它引用的插件基类程序集叫 `BepInEx.IL2CPP`；
而 BepInEx 从 **be.577** 起把这个程序集改名为 `BepInEx.Unity.IL2CPP`（[BepInEx issue #521](https://github.com/BepInEx/BepInEx/issues/521)），
于是旧版 UE 在加载时被**静默跳过**——日志里一个字都不会出现，与你那次日志（"4 plugins to load"，UE 零输出）完全吻合。

## 实锤证据（对你上传到分支的 dll 做 strings 解剖）

| 检查项 | 你的 `UnityExplorer.BIE.IL2CPP.CoreCLR.dll` | 正确版本应为 |
|---|---|---|
| 程序集名 | `UnityExplorer.BIE.IL2CPP.CoreCLR` | `UnityExplorer.BIE.Unity.IL2CPP.CoreCLR` |
| 版本 | **4.9.0**（2022 年的老版本） | ≥ 4.13.x（yukieiji fork 持续维护） |
| 引用的 BepInEx 插件基类 | **`BepInEx.IL2CPP`** ❌（be.577+ 已不存在这个名字） | **`BepInEx.Unity.IL2CPP`** ✅ |
| 结局 | 被 BepInEx 静默跳过 | 正常加载 |

旁证：yukieiji fork 的 `src/UnityExplorer.csproj` 里专门有一个 `BIPUNITY` 编译目标，
输出程序集名就叫 `UnityExplorer.BIE.Unity.IL2CPP.CoreCLR` —— 就是为这次改名准备的。

## 修复步骤（5 分钟）

1. 打开 yukieiji fork 发布页：<https://github.com/yukieiji/UnityExplorer/releases/latest>
2. **只下这一个文件**（最新为 v4.13.6，2026-04-30 发布）：

   | 文件名 | 要不要 |
   |---|---|
   | **`UnityExplorer.BepInEx.Unity.IL2CPP.CoreCLR.zip`** | ✅ **下这个** |
   | `UnityExplorer.BepInEx.IL2CPP.CoreCLR.zip` | ❌ 名字像但不是（旧命名，就是你踩的坑） |
   | `UnityExplorer.BepInEx.IL2CPP.zip` | ❌ 老框架版 |
   | `UnityExplorer.BepInEx5/6.Mono.zip`、`...MelonLoader...`、`...Standalone...` | ❌ 都不是 |

3. 关掉游戏。删掉旧文件夹：`D:\steam\steamapps\common\MiSide\BepInEx\plugins\sinai-dev-UnityExplorer\`
4. 把 zip 里的两个 dll **放进一个新文件夹**，例如
   `D:\steam\steamapps\common\MiSide\BepInEx\plugins\UnityExplorer\`：
   - `UnityExplorer.BIE.Unity.IL2CPP.CoreCLR.dll`
   - `UniverseLib.IL2CPP.Interop.dll`
5. 删掉 `D:\steam\steamapps\common\MiSide\BepInEx\cache\` 整个文件夹（清掉链式加载缓存，保险）。
6. 启动游戏。

## 验证（加载成功长什么样）

打开 `BepInEx\LogOutput.log`，应同时满足：

- 插件计数变成 **"5 plugins to load"**（原来 4 个 + UE）；
- 日志里出现 `UnityExplorer` 字样（版本 4.13.x）与 UniverseLib 的初始化行；
- 进游戏后按 **F7** 弹出场景树窗口。

> 备注：MS_CustomModels 4.0.4 自己也带一份 UniverseLib，两份共存是常态，不影响。

## 自助校验工具（以后下任何 BepInEx 插件都能先验明正身）

不用启动游戏，直接拿 dll 验：

```powershell
# 在仓库目录下（PowerShell）：
./tools/Test-PluginCompat.ps1 -DllPath "C:\path\to\任意插件.dll"
```

它会列出该 dll 引用的全部 BepInEx 程序集并给出结论：
引用 `BepInEx.Unity.IL2CPP` → 你的 be.697 能加载；引用 `BepInEx.IL2CPP` → 会被静默跳过。
