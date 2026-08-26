using System;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace MitaTrueEnding.Patches;

/// <summary>
/// 阶段 1 前置 · 过场雷达（v0.4.0 引入）。
///
/// 拦截目标来自静态侦察（docs/recon-reports/assembly-csharp.keyword-report.txt）：
///   Playable_Animation
///     + Void Play()                            —— 播 director 上已挂好的资源
///     + Void PlayAsset(PlayableAsset _asset)   —— 指定资源播放（首选拦截层）
///     + UnityEvent eventStart / eventStop、PlayableDirector scrpd、GameController scrgc
///
/// 默认行为（安全，不改游戏任何流程）：
///   - 任意过场触发时，把 Timeline 资源名 + 当前场景名 + 挂载物体名写进
///     BepInEx/config/MitaTE-recon.log。通关后把该日志传回，即可锁定
///     「处决过场」的确切资源名（docs/recon-checklist.md §0-3 对照表）。
///   - 只有当资源名命中配置 Interception.CutsceneWatchList（逗号分隔，默认空）
///     时，才在 rescue.json 里标记 RescuedKindMita=true。
///
/// 刻意不做的事（等 §0-3 数据再定）：
///   - 不 return false 跳过原过场：场景推进可能依赖 PlayableDirector 的
///     stopped 回调，贸然跳过可能卡关。拿到确切资源名与场景流后再上「替代演出」。
/// </summary>
internal static class CutsceneRadar
{
    private static ConfigEntry<string>? _watchList;

    // 去重：同一入口 + 同一资源名在 DedupSeconds 秒内只记一条（Play 内部可能连带调 PlayAsset）
    private const double DedupSeconds = 2.0;
    private static string _lastKey = "";
    private static DateTime _lastAt = DateTime.MinValue;

    internal static void Configure(ConfigFile cfg)
    {
        _watchList = cfg.Bind(
            "Interception",
            "CutsceneWatchList",
            "",
            "触发「善良米塔获救」标记的 Timeline 资源名（逗号分隔，大小写不敏感）。\n" +
            "留空 = 纯侦察模式：只把过场资源名记进 MitaTE-recon.log，不改任何存档。\n" +
            "在根据 recon 日志锁定处决过场的确切资源名之前，请保持留空。");
    }

    internal static void Report(string via, string? assetName, Playable_Animation instance)
    {
        try
        {
            string asset = string.IsNullOrEmpty(assetName) ? "<未命名>" : assetName!;
            string key = via + "|" + asset;
            DateTime now = DateTime.UtcNow;
            if (key == _lastKey && (now - _lastAt).TotalSeconds < DedupSeconds)
                return;
            _lastKey = key;
            _lastAt = now;

            string scene = "";
            try { scene = SceneManager.GetActiveScene().name; } catch { /* 场景名取不到不致命 */ }
            string go = "";
            try { go = instance.gameObject.name; } catch { }

            ReconBehaviour.LogLine($"过场触发[{via}] asset=\"{asset}\" scene=\"{scene}\" go=\"{go}\"");

            if (IsWatched(asset))
            {
                Plugin.Rescue.MarkRescued("kind");
                ReconBehaviour.LogLine($"★ 命中监视名单 asset=\"{asset}\"：已标记 RescuedKindMita=true 并落盘");
            }
        }
        catch (Exception e)
        {
            // 前缀补丁绝不允许把异常抛回游戏流程
            Plugin.Log.LogWarning($"[MitaTE] 过场雷达记录失败（已忽略）: {e.Message}");
        }
    }

    private static bool IsWatched(string asset)
    {
        string raw = _watchList?.Value ?? "";
        if (string.IsNullOrWhiteSpace(raw)) return false;
        foreach (string item in raw.Split(','))
        {
            if (string.Equals(item.Trim(), asset, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}

/// <summary>雷达探针 A：PlayAsset(PlayableAsset) —— 资源名最直接的来源。</summary>
[HarmonyPatch(typeof(Playable_Animation), nameof(Playable_Animation.PlayAsset))]
internal static class PlayableAnimationPlayAssetPatch
{
    private static void Prefix(Playable_Animation __instance, PlayableAsset _asset)
    {
        string? name = null;
        try { name = _asset is null ? "<null>" : _asset.name; } catch { }
        CutsceneRadar.Report("PlayAsset", name, __instance);
    }
}

/// <summary>雷达探针 B：Play() —— 播 director 已挂资源的场景走这里，兜底从 scrpd 读资源名。</summary>
[HarmonyPatch(typeof(Playable_Animation), nameof(Playable_Animation.Play))]
internal static class PlayableAnimationPlayPatch
{
    private static void Prefix(Playable_Animation __instance)
    {
        string? name = null;
        try
        {
            PlayableDirector? pd = __instance.scrpd;
            PlayableAsset? pa = pd is null ? null : pd.playableAsset;
            name = pa is null ? "<director 未挂资源>" : pa.name;
        }
        catch { }
        CutsceneRadar.Report("Play", name, __instance);
    }
}
