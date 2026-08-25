using System;
using System.IO;
using BepInEx;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using UnityEngine.SceneManagement;
using SceneLoadedHandler = UnityEngine.Events.UnityAction<UnityEngine.SceneManagement.Scene, UnityEngine.SceneManagement.LoadSceneMode>;

namespace MitaTrueEnding;

/// <summary>
/// 阶段 0 侦察组件（只读不写，不影响游戏行为；正式版可移除）。
///
/// 功能：
/// 1) 每次场景切换自动记录：追加写入 BepInEx/config/MitaTE-recon.log，并同步进游戏主日志（前缀 [MitaTE-Recon]）；
/// 2) 游戏内按 F9：把当前场景的根对象清单（名字 + 激活状态）写入同一日志。
///
/// 用法见 docs/recon-checklist.md 第 0-1.5 节。
/// </summary>
public class ReconBehaviour : MonoBehaviour
{
    /// <summary>IL2CPP 注入类型的标准构造（Il2CppInterop 约定）。</summary>
    public ReconBehaviour(IntPtr ptr) : base(ptr) { }

    public ReconBehaviour() : base(ClassInjector.DerivedConstructorPointer<ReconBehaviour>())
        => ClassInjector.DerivedConstructorBody(this);

    private static string LogFile => Path.Combine(Paths.ConfigPath, "MitaTE-recon.log");

    public void Awake()
    {
        LogLine("=== Recon 组件启动：场景切换记录已开启；游戏内按 F9 打印当前场景根对象 ===");
        // IL2CPP 注意：interop stub 里的 UnityAction<,> 被生成为"类"而非原生委托，
        // 方法组无法直接 +=；正确姿势：先转托管 Action，再 DelegateSupport.ConvertDelegate 包装
        SceneManager.sceneLoaded += DelegateSupport.ConvertDelegate<SceneLoadedHandler>(
            (Action<Scene, LoadSceneMode>)OnSceneLoaded);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        LogLine($"场景加载: \"{scene.name}\"  path=\"{scene.path}\"  roots={scene.rootCount}");
    }

    public void Update()
    {
        if (!Input.GetKeyDown(KeyCode.F9)) return;

        var scene = SceneManager.GetActiveScene();
        LogLine($"--- F9 快照: 当前场景 \"{scene.name}\"，共 {scene.rootCount} 个根对象 ---");
        foreach (var go in scene.GetRootGameObjects())
        {
            LogLine($"    {(go.activeSelf ? "[on ]" : "[off]")} {go.name}");
        }
    }

    private static void LogLine(string msg)
    {
        Plugin.Log.LogInfo($"[MitaTE-Recon] {msg}");
        try
        {
            File.AppendAllText(LogFile, $"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
        }
        catch
        {
            // 侦察日志失败不影响任何功能
        }
    }
}
