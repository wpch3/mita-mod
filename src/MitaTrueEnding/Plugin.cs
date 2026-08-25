using System;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;

namespace MitaTrueEnding;

/// <summary>
/// 米塔「全员拯救」真结局 Mod —— BepInEx 6 (IL2CPP / CoreCLR) 插件入口。
/// 目标游戏：MiSide（Unity 2021.3.35f1，IL2CPP，x64）。
/// 方案与陷阱说明见仓库根 README.md（v2）。
/// </summary>
[BepInPlugin(GUID, NAME, VERSION)]
public class Plugin : BasePlugin
{
    public const string GUID = "com.wpch3.miside.trueending";
    public const string NAME = "MiSide True Ending - Save All Mitas";
    public const string VERSION = "0.2.1";

    /// <summary>全局日志（输出到 BepInEx/LogOutput.log）。</summary>
    internal static new ManualLogSource Log { get; private set; } = null!;

    /// <summary>拯救进度：独立于游戏存档的自定义 JSON（见 RescueState）。</summary>
    public static RescueState Rescue { get; private set; } = null!;

    private Harmony? _harmony;

    public override void Load()
    {
        Log = base.Log;
        Rescue = RescueState.Load();

        Log.LogInfo($"[MitaTE] {NAME} v{VERSION} 已加载");
        Log.LogInfo($"[MitaTE] 当前拯救进度: {Rescue}");

        _harmony = new Harmony(GUID);

        // =====================================================================
        // ⚠️ 阶段 0/1 提示：
        // 在完成逆向侦察（docs/recon-checklist.md）、把真实类名/方法名落实为
        // Patches/*.cs 之前，不要启用下面的补丁。
        // 模板见 src/MitaTrueEnding/Templates/*.cs.txt（.txt 不参与编译）。
        // =====================================================================
        // _harmony.PatchAll(typeof(Patches.KindMitaInterceptPatch));
        // _harmony.PatchAll(typeof(Patches.TinyMitaInterceptPatch));
        // _harmony.PatchAll(typeof(Patches.MilaInterceptPatch));
        // _harmony.PatchAll(typeof(Patches.CappieInterceptPatch));

        // --- 阶段 0 内置侦察：场景切换记录 + F9 打印当前场景根对象 ---
        try
        {
            ClassInjector.RegisterTypeInIl2Cpp<ReconBehaviour>();
            AddComponent<ReconBehaviour>();
            Log.LogInfo("[MitaTE] 侦察组件已启用：场景切换记录到 BepInEx/config/MitaTE-recon.log，游戏内按 F9 打印当前场景根对象");
        }
        catch (Exception e)
        {
            Log.LogWarning($"[MitaTE] 侦察组件启动失败（不影响主功能）: {e.Message}");
        }

        Log.LogInfo("[MitaTE] 未启用任何剧情拦截补丁（等待阶段 0 侦察结果）");
    }
}
