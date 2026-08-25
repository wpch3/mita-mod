using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using BepInEx;

namespace MitaTrueEnding;

/// <summary>
/// 「全员拯救」进度存档。
///
/// 设计决策（见 README v2 §4.1）：
/// - 不写回原游戏存档，避免污染存档/坏档、避免与游戏更新冲突；
/// - 独立 JSON 存到 BepInEx/config 目录，由剧情拦截点在运行时读写；
/// - 拦截判定一律以 <see cref="TrueEndingUnlocked"/> 为准，未满足时游戏完全走原版流程。
/// </summary>
public sealed class RescueState
{
    // ---------- 伏笔道具 ----------
    /// <summary>二楼房间隐藏道具：未损坏的核心数据盘。</summary>
    public bool HasCoreDataDisk { get; set; }

    /// <summary>掌机隐藏关卡通关奖励：修复补丁。</summary>
    public bool HasRepairPatch { get; set; }

    // ---------- 各章节拯救结果（拦截点按 README §2 表格拆分） ----------
    /// <summary>小米塔：循环回廊。</summary>
    public bool RescuedTinyMita { get; set; }

    /// <summary>米拉：2D 世界崩塌前完成世界数据备份。</summary>
    public bool RescuedMila { get; set; }

    /// <summary>帽子米塔（卡比）：备份完成使其免遭毒手。</summary>
    public bool RescuedCappie { get; set; }

    /// <summary>善良米塔：终章核心区域介入阻拦。</summary>
    public bool RescuedKindMita { get; set; }

    /// <summary>四位米塔是否全部获救。</summary>
    [JsonIgnore]
    public bool AllRescued => RescuedTinyMita && RescuedMila && RescuedCappie && RescuedKindMita;

    /// <summary>真结局触发条件：全员获救 + 全部伏笔道具。</summary>
    [JsonIgnore]
    public bool TrueEndingUnlocked => AllRescued && HasCoreDataDisk && HasRepairPatch;

    // ---------- 持久化 ----------
    private static string FilePath => Path.Combine(Paths.ConfigPath, "MitaTrueEnding.rescue.json");

    public static RescueState Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                return JsonSerializer.Deserialize<RescueState>(File.ReadAllText(FilePath))
                       ?? new RescueState();
            }
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning($"[MitaTE] 读取拯救进度失败，使用空档: {e.Message}");
        }
        return new RescueState();
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(FilePath,
                JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception e)
        {
            Plugin.Log.LogError($"[MitaTE] 保存拯救进度失败: {e}");
        }
    }

    /// <summary>记录一次拯救并落盘。mitaId: tiny / mila / cappie / kind。</summary>
    public void MarkRescued(string mitaId)
    {
        switch (mitaId.ToLowerInvariant())
        {
            case "tiny":   RescuedTinyMita = true; break;
            case "mila":   RescuedMila     = true; break;
            case "cappie": RescuedCappie   = true; break;
            case "kind":   RescuedKindMita = true; break;
            default:
                Plugin.Log.LogWarning($"[MitaTE] 未知米塔 id: {mitaId}");
                return;
        }
        Plugin.Log.LogInfo($"[MitaTE] 已拯救 {mitaId}！进度: {this}");
        Save();
    }

    public override string ToString() =>
        $"Tiny={RescuedTinyMita}, Mila={RescuedMila}, Cappie={RescuedCappie}, Kind={RescuedKindMita}, " +
        $"Disk={HasCoreDataDisk}, Patch={HasRepairPatch} => TrueEnding={TrueEndingUnlocked}";
}
