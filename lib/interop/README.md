# interop 程序集放置说明（本目录在仓库里故意为空，请勿删除此 README）

MiSide 是 **IL2CPP** 游戏。编译本 mod 所需的游戏/引擎程序集
（`Assembly-CSharp.dll` 与 `UnityEngine.*.dll` 的壳）由 BepInEx 在首次启动时自动生成。
它们是**游戏资产的派生物**，按约定**不入库**（仓库为公开仓库，分发游戏资产有版权风险），
每台开发机按下面步骤本机生成一次即可，生成后会被 `.gitignore` 自动忽略、不会被提交。

## 生成步骤（Windows）

1. 下载 **BepInEx 6（IL2CPP / CoreCLR，bleeding edge 版）**：
   <https://builds.bepinex.dev/projects/bepinex_be> —— 选 `BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.*.zip`
2. 解压到 MiSide **游戏根目录**（`MiSideFull.exe` 所在处）。
3. 运行一次游戏到主菜单后退出（首次启动较慢，正在生成 interop）。
4. 把 `<游戏根目录>/BepInEx/interop/` 下的**全部 dll**（约 87 个）复制到本目录，
   应能看到 `Assembly-CSharp.dll`、`UnityEngine.CoreModule.dll` 等。
5. 仓库根目录执行 `dotnet build -c Release` —— csproj 检测到
   `lib/interop/Assembly-CSharp.dll` 存在时会自动加入这些引用。

## 其他

- 游戏每次大更新后，重复第 3、4 步换掉旧 dll，否则补丁可能对不上新版本。
- `MethodAddressToToken.db`、`MethodXrefScanCache.db`、`assembly-hash.txt` 是 BepInEx
  的本地缓存，与编译无关，无需复制。
- 用 dnSpy 打开 `Assembly-CSharp.dll` 只能看类名/方法签名；要看逻辑用
  Il2CppDumper + Ghidra/IDA，或 Cpp2IL（见 `docs/recon-checklist.md`）。
- 也可设置环境变量 `MISIDE_DIR=<游戏根目录>`，csproj 会直接引用游戏目录下的 interop
  （跳过第 4 步）。
