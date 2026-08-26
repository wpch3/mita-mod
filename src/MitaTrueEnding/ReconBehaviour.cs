using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using UnityEngine.SceneManagement;
using SceneLoadedHandler = UnityEngine.Events.UnityAction<UnityEngine.SceneManagement.Scene, UnityEngine.SceneManagement.LoadSceneMode>;

namespace MitaTrueEnding;

/// <summary>
/// 阶段 0 侦察组件 v2（只读不写，不影响游戏行为；正式版可移除）。
///
/// 1) 时间序日志：每次场景切换追加写入 BepInEx/config/MitaTE-recon.log（并同步进游戏主日志）。
/// 2) 自动快照：场景加载后分别在 1.5s / 8s 自动抓取根对象清单，写入
///    BepInEx/config/MitaTE-scene-snapshots.log —— 规则：同场景覆盖、异场景保留，
///    无需手动按 F9（恐怖场景忘了按也不会缺数据）。
/// 3) F9：立即对当前全部已加载场景补抓一张快照（可选，手动兜底）。
/// </summary>
public class ReconBehaviour : MonoBehaviour
{
    public ReconBehaviour(IntPtr ptr) : base(ptr) { }

    public ReconBehaviour() : base(ClassInjector.DerivedConstructorPointer<ReconBehaviour>())
        => ClassInjector.DerivedConstructorBody(this);

    private static string LogFile => Path.Combine(Paths.ConfigPath, "MitaTE-recon.log");
    private static string SnapshotFile => Path.Combine(Paths.ConfigPath, "MitaTE-scene-snapshots.log");

    private const float FirstShotDelay = 1.5f;
    private const float SecondShotDelay = 8f;

    // ---- 延迟抓拍队列（场景加载后对象会陆续生成，抓两个时间点，后到覆盖先到） ----
    private sealed class PendingShot
    {
        public string SceneName = "";
        public float AtTime;
    }

    private readonly List<PendingShot> _pending = new();

    // ---- 快照存储（key = 场景名；同名覆盖 → 满足"重复的覆盖、不重复的保留"） ----
    private sealed class SceneSnapshot
    {
        public string ScenePath = "";
        public string CapturedAt = "";
        public List<string> RootLines = new();
    }

    private readonly SortedDictionary<string, SceneSnapshot> _snapshots =
        new(StringComparer.OrdinalIgnoreCase);

    public void Awake()
    {
        LoadSnapshotsFromDisk();
        LogLine("=== Recon v2 启动：场景切换自动快照已开启（1.5s/8s 两次抓取，同场景覆盖、异场景保留）===");
        LogLine($"=== 快照文件: {SnapshotFile} · 时间序日志: {LogFile} · F9=立即补抓 ===");

        // IL2CPP 注意：interop stub 里的 UnityAction<,> 不是原生委托，需 ConvertDelegate 包装
        SceneManager.sceneLoaded += DelegateSupport.ConvertDelegate<SceneLoadedHandler>(
            (Action<Scene, LoadSceneMode>)OnSceneLoaded);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        LogLine($"场景加载: \"{scene.name}\"  path=\"{scene.path}\"  roots={scene.rootCount}  mode={mode}");
        Schedule(scene.name, FirstShotDelay);
        Schedule(scene.name, SecondShotDelay);
    }

    private void Schedule(string sceneName, float delaySeconds)
    {
        _pending.Add(new PendingShot { SceneName = sceneName, AtTime = Time.unscaledTime + delaySeconds });
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9))
            ForceSnapshotAllLoaded();

        if (_pending.Count == 0) return;

        float now = Time.unscaledTime;
        var due = _pending.Where(p => now >= p.AtTime).ToList();
        foreach (var p in due)
        {
            _pending.Remove(p);
            TryCapture(p.SceneName);
        }
    }

    /// <summary>F9：立即对当前全部已加载场景补抓一张快照（手动兜底，不影响自动计划）。</summary>
    private void ForceSnapshotAllLoaded()
    {
        LogLine($"--- F9 补抓: 对 {SceneManager.sceneCount} 个已加载场景立即快照 ---");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            TryCapture(SceneManager.GetSceneAt(i).name);
    }

    /// <summary>抓取指定场景的根对象并存入快照字典（存在则覆盖），随后整体落盘。</summary>
    private void TryCapture(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!string.Equals(scene.name, sceneName, StringComparison.OrdinalIgnoreCase) || !scene.isLoaded)
                continue;

            var roots = new List<string>();
            foreach (GameObject go in scene.GetRootGameObjects())
                roots.Add($"{(go.activeSelf ? "[on ]" : "[off]")} {go.name}");

            roots.Sort(StringComparer.Ordinal);
            _snapshots[scene.name] = new SceneSnapshot
            {
                ScenePath = scene.path,
                CapturedAt = DateTime.Now.ToString("HH:mm:ss"),
                RootLines = roots,
            };
            WriteSnapshotsToDisk();
            LogLine($"自动快照: \"{scene.name}\" roots={roots.Count}（快照文件已更新，共 {_snapshots.Count} 个场景）");
            return;
        }
        // 场景已被卸载（例如过场加载页）→ 静默跳过，不视为错误
    }

    // ---------- 快照文件读写（整体重写 = 去重的唯一事实源） ----------

    private void WriteSnapshotsToDisk()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# MitaTE 场景快照 · 规则: 同场景覆盖 / 异场景保留 · 最近写入 {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"# 已记录场景数: {_snapshots.Count}");
        foreach (var kv in _snapshots)
        {
            sb.AppendLine();
            sb.AppendLine($"===== 场景: \"{kv.Key}\" | path=\"{kv.Value.ScenePath}\" | roots={kv.Value.RootLines.Count} | 抓取: {kv.Value.CapturedAt} =====");
            foreach (string line in kv.Value.RootLines)
                sb.AppendLine($"    {line}");
        }
        try { File.WriteAllText(SnapshotFile, sb.ToString()); }
        catch (Exception e) { Plugin.Log.LogWarning($"[MitaTE-Recon] 快照写入失败（不影响功能）: {e.Message}"); }
    }

    /// <summary>启动时读回已有快照文件（跨会话合并：旧场景不丢）。解析失败静默重来。</summary>
    private void LoadSnapshotsFromDisk()
    {
        try
        {
            if (!File.Exists(SnapshotFile)) return;
            SceneSnapshot? current = null;
            string? currentName = null;
            foreach (string raw in File.ReadAllLines(SnapshotFile))
            {
                string line = raw.TrimEnd('\r');
                if (line.StartsWith("===== 场景:", StringComparison.Ordinal))
                {
                    currentName = ExtractBetween(line, "场景: \"", "\"");
                    string capturedPath = ExtractBetween(line, "path=\"", "\"");
                    current = new SceneSnapshot { ScenePath = capturedPath };
                    if (!string.IsNullOrEmpty(currentName))
                        _snapshots[currentName] = current;
                }
                else if (current != null && line.StartsWith("    "))
                {
                    current.RootLines.Add(line.TrimStart());
                }
            }
            if (_snapshots.Count > 0)
                Plugin.Log.LogInfo($"[MitaTE-Recon] 已读回历史快照 {_snapshots.Count} 个场景，继续累积。");
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning($"[MitaTE-Recon] 历史快照解析失败，本次从零开始: {e.Message}");
            _snapshots.Clear();
        }
    }

    private static string ExtractBetween(string text, string startMark, string endMark)
    {
        int s = text.IndexOf(startMark, StringComparison.Ordinal);
        if (s < 0) return "";
        s += startMark.Length;
        int e = text.IndexOf(endMark, s, StringComparison.Ordinal);
        return e < 0 ? "" : text.Substring(s, e - s);
    }

    internal static void LogLine(string msg)   // internal：Patches/CutsceneRadar 也用它写 MitaTE-recon.log
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
