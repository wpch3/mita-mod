# interop 程序集放置说明（本目录故意为空，请勿删除此 README）

MiSide 是 **IL2CPP** 游戏，其程序集（含 `Assembly-CSharp.dll` 与 `UnityEngine.*.dll` 的类型包装）
由 BepInEx 在首次启动时自动生成。这些文件属于游戏资产，版权原因**不入库**，请自己生成：

## 步骤

1. 下载 **BepInEx 6（IL2CPP / CoreCLR，bleeding edge 版）**：
   <https://builds.bepinex.dev/projects/bepinex_be> —— 选 `BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.*.zip`
2. 解压到 MiSide **游戏根目录**（`MiSide.exe` 所在处）。
3. 运行一次游戏到主菜单后退出（首次启动会较慢，它正在生成 interop）。
4. 把 `<游戏根目录>/BepInEx/interop/` 下的**全部 dll** 复制到本目录，应能看到：
   - `Assembly-CSharp.dll`（游戏全部脚本壳，dnSpy 里可检索类名/方法名）
   - `UnityEngine.CoreModule.dll`、`UnityEngine.dll` 等
5. `dotnet build src/MitaTrueEnding/MitaTrueEnding.csproj` —— csproj 检测到
   `lib/interop/Assembly-CSharp.dll` 存在时会自动加入这些引用。

## 提示

- 游戏每次大更新后，interop 需要重新生成并覆盖到本目录。
- 用 dnSpy 打开 `Assembly-CSharp.dll` 只能看**签名**；要看逻辑用 Il2CppDumper + Ghidra/IDA，或 Cpp2IL。
- 也可设置环境变量 `MISIDE_DIR=<游戏根目录>`，csproj 会直接引用游戏目录下的 interop（跳过第 4 步）。
