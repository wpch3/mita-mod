# interop 程序集说明

本目录存放 BepInEx 6（IL2CPP）**首次运行游戏时自动生成**的 interop 程序集
（`Assembly-CSharp.dll` 与 `UnityEngine.*.dll` 等游戏/引擎类型的壳）。

## 当前约定（2026-08-25，项目所有者拍板）

- **interop 已入库**：clone / 下载 ZIP 即可直接编译，免去每人手动生成。
- ⚠️ **这些是游戏资产的派生物：仓库必须保持私有，不得公开分发。**
- **游戏大更新后必须更换**：删除本目录旧 dll → 游戏目录跑一次 BepInEx →
  复制新的 `BepInEx/interop/*.dll` 到本目录 → commit。否则补丁会对不上新版本。

## 已移出的文件

`MethodAddressToToken.db`、`MethodXrefScanCache.db`、`assembly-hash.txt` 是 BepInEx 的
**本地搜索缓存**（与编译无关，csproj 只引用 `*.dll`），已按 `.gitignore` 约定不入库，
本机生成后可随手删除。

## 如何从零生成（游戏更新后照此执行）

1. 下载 BepInEx 6（IL2CPP / CoreCLR，bleeding edge）：
   <https://builds.bepinex.dev/projects/bepinex_be> —— 选 `BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.*.zip`
2. 解压到 MiSide 游戏根目录（`MiSide.exe` 所在处）。
3. 运行一次游戏到主菜单后退出（首次启动较慢，正在生成 interop）。
4. 用 `<游戏根目录>/BepInEx/interop/*.dll` 覆盖本目录内容。

## 提示

- 用 dnSpy 打开 `Assembly-CSharp.dll` 只能看类名/方法签名；要看逻辑用
  Il2CppDumper + Ghidra/IDA，或 Cpp2IL。
