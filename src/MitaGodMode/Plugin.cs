using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace MitaGodMode;

/// <summary>
/// MiSide 作弊模组「无敌与和平」—— 给不爱追杀环节的玩家（以及我们的侦察员保命用）。
///
/// 三层开关（配置文件 BepInEx/config/com.wpch3.miside.godmode.cfg，改后重启生效）：
///   1. GodMode     —— 无敌：拦截一切对玩家的伤害/处决/判负判定（被抓到也不死）。
///   2. PacifyMitas —— 和平：拦截米塔/怪物的攻击发起（她不主动打你）。
///   3. SpeedMultiplier —— 移速倍率：挂在 WorldPlayer.speed 上，默认 1.6x，跑赢一切追杀。
/// v1.1 新增：**F10 强制跳过当前追杀环节**（见 GodBehaviour.cs）——
///   躲藏/追杀章开无敌被发现后会出现"收场动作被没收 → 剧本无法落幕 → 外部计时重置 →
///   从入口无限轮回"的问题；F10 直接调用该环节的正牌落幕函数（如 PlayerExit），
///   她收工、玩家走人，环节立即结束。
///
/// 设计要点：
///   - 全部用字符串反射定位（AccessTools.TypeByName/Method），不硬引用 interop；
///     找不到的目标只跳过+写日志，绝不让插件崩溃波及游戏。
///   - 命中拦截会写一行日志（3 秒节流）——这些"本来你要死在这里"的记录
///     同时就是 MitaTrueEnding 死因侦察的副产品。
///   - 刻意不拦：小游戏的 Lose/判负（PC 小游戏需要"输了重试"的循环，拦了会卡死）、
///     PlayerDontMove 等演出锁（剧情序列需要）、镜头惊吓特效（留作氛围，且多为剧情节点）。
///   目标清单来自 docs/recon-reports/ 静态报告（2026-08-26 全量盘点）。
/// </summary>
[BepInPlugin(GUID, NAME, VERSION)]
public class Plugin : BasePlugin
{
    public const string GUID = "com.wpch3.miside.godmode";
    public const string NAME = "MiSide God Mode";
    public const string VERSION = "1.1.1";

    /// <summary>全局日志（输出到 BepInEx/LogOutput.log）。</summary>
    internal static new ManualLogSource Log { get; private set; } = null!;

    private static ConfigEntry<bool> _godMode = null!;
    private static ConfigEntry<bool> _pacify = null!;
    private static ConfigEntry<float> _speed = null!;

    private static Harmony? _harmony;

    // ---- 拦截日志节流：同名方法 3 秒内只记一行 ----
    private static readonly Dictionary<string, DateTime> _lastLogAt = new();
    private static readonly TimeSpan LogThrottle = TimeSpan.FromSeconds(3);

    private sealed class Target
    {
        public string Type = "";
        public string Method = "";
        public bool Pacify; // false=无敌组(伤害/处决/判负)  true=和平组(攻击发起)
    }

    /// <summary>拦截目标清单（来源：docs/recon-reports 静态报告，均为 public void 方法）。</summary>
    private static readonly List<Target> Targets = new()
    {
        // ---- 无敌组：伤害 / 处决 / 判负 ----
        new() { Type = "Location6_MitaKiller",       Method = "Kill" },              // 电锯疯米塔处决玩家
        new() { Type = "Location6_MitaKiller",       Method = "PlayerLose" },        // 电锯章追抓判负
        new() { Type = "Location20_RunCorridor",     Method = "Damage" },            // 终章逃亡回廊被撞
        new() { Type = "Location20_RunCorridor",     Method = "KillPlayerStart" },   // 终章逃亡回廊致命杀招
        new() { Type = "Location7_HammerButton",     Method = "PlayerDamage" },      // 铁锤区域被砸
        new() { Type = "Shooter_Player",             Method = "Damage" },            // 射击段落受伤
        new() { Type = "Shooter_Player",             Method = "Kill" },              // 射击段落被杀

        // ---- 和平组：攻击发起 ----
        new() { Type = "Location6_MitaKiller",       Method = "StartAttack",          Pacify = true }, // 疯米塔抬手
        new() { Type = "Location12",                 Method = "CreepyMitaAttack",     Pacify = true }, // 怪物米塔袭击
        new() { Type = "Location12",                 Method = "CreepyMitaStayAttack", Pacify = true },
        new() { Type = "Location10_ManekenChekpoint", Method = "ManekenAttack",       Pacify = true }, // 人偶检查站
        new() { Type = "Location11_LiftEnemy",       Method = "Attack",               Pacify = true }, // 电梯怪物
    };

    public override void Load()
    {
        Log = base.Log;

        _godMode = Config.Bind("God", "GodMode", true,
            "无敌：拦截一切对玩家的伤害/处决/判负判定（被抓到也不会死）。");
        _pacify = Config.Bind("God", "PacifyMitas", true,
            "和平：拦截米塔/怪物的攻击发起（她们不会主动打你）。\n" +
            "⚠️ 躲藏/追杀章被看到后会因剧本无法落幕而轮回——那种章节请把本项关 false，" +
            "或直接用 F10 强制跳过该环节（推荐）。");
        _speed = Config.Bind("God", "SpeedMultiplier", 1.6f,
            "移速倍率：挂在 WorldPlayer.speed 上（1 = 关闭；默认 1.6x，跑赢一切追杀）。");

        _harmony = new Harmony(GUID);

        int ok = 0, skip = 0;
        foreach (Target t in Targets)
        {
            if (TryPatchNoOp(t)) ok++; else skip++;
        }
        if (TryPatchSpeed()) ok++; else skip++;

        Log.LogInfo($"[GodMode] {NAME} v{VERSION} 已加载：{ok} 个补丁生效，{skip} 个目标跳过（不影响游戏）");
        Log.LogInfo($"[GodMode] 开关：GodMode={_godMode.Value} Pacify={_pacify.Value} Speed={_speed.Value}x");

        // --- v1.1：F10 强制跳过追杀环节的热键组件 ---
        try
        {
            Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp<GodBehaviour>();
            AddComponent<GodBehaviour>();
            Log.LogInfo("[GodMode] 热键组件已启用：按 F10 强制结束当前追杀/躲藏环节（详见 docs/godmode.md）");
        }
        catch (Exception e)
        {
            Log.LogWarning($"[GodMode] 热键组件启动失败（无敌/和平/移速不受影响）: {e.Message}");
        }
    }

    // ================== 补丁挂载 ==================

    private static bool TryPatchNoOp(Target t)
    {
        try
        {
            Type? type = AccessTools.TypeByName(t.Type);
            if (type == null) { Log.LogWarning($"[GodMode] 类型不存在：{t.Type}"); return false; }
            MethodInfo? m = AccessTools.Method(type, t.Method);
            if (m == null) { Log.LogWarning($"[GodMode] 方法不存在：{t.Type}.{t.Method}"); return false; }

            string handler = t.Pacify ? nameof(PacifyPrefix) : nameof(GodPrefix);
            _harmony!.Patch(m, prefix: new HarmonyMethod(typeof(Plugin), handler));
            Log.LogInfo($"[GodMode] 已接管 {t.Type}.{t.Method}（{(t.Pacify ? "和平" : "无敌")}）");
            return true;
        }
        catch (Exception e)
        {
            Log.LogWarning($"[GodMode] 接管失败 {t.Type}.{t.Method}: {e.Message}");
            return false;
        }
    }

    private static bool TryPatchSpeed()
    {
        try
        {
            Type? type = AccessTools.TypeByName("WorldPlayer");
            PropertyInfo? prop = type == null ? null : AccessTools.Property(type, "speed");
            MethodInfo? getter = prop?.GetGetMethod();
            if (getter == null) { Log.LogWarning("[GodMode] 未找到 WorldPlayer.speed，移速倍率不生效"); return false; }

            _harmony!.Patch(getter, postfix: new HarmonyMethod(typeof(Plugin), nameof(SpeedPostfix)));
            Log.LogInfo($"[GodMode] 移速倍率已挂到 WorldPlayer.speed（当前 {_speed.Value}x）");
            return true;
        }
        catch (Exception e)
        {
            Log.LogWarning($"[GodMode] 移速补丁失败: {e.Message}");
            return false;
        }
    }

    // ================== Harmony 处理函数 ==================

    /// <summary>无敌组前缀：GodMode 开 → 跳过原方法（伤害/处决不发生）。</summary>
    public static bool GodPrefix(MethodBase __originalMethod)
        => Block("无敌", _godMode.Value, __originalMethod);

    /// <summary>和平组前缀：Pacify 开 → 跳过原方法（攻击不发起）。</summary>
    public static bool PacifyPrefix(MethodBase __originalMethod)
        => Block("和平", _pacify.Value, __originalMethod);

    private static bool Block(string label, bool enabled, MethodBase m)
    {
        if (!enabled) return true; // 开关关闭：放行原版

        string name = (m.DeclaringType == null ? "?" : m.DeclaringType.Name) + "." + m.Name;
        DateTime now = DateTime.UtcNow;
        if (!_lastLogAt.TryGetValue(name, out DateTime last) || now - last > LogThrottle)
        {
            _lastLogAt[name] = now;
            Log.LogInfo($"[GodMode] 已拦下【{label}】{name} —— 原本这里要出事");
        }
        return false; // 跳过原方法
    }

    /// <summary>移速倍率后缀：乘到 WorldPlayer.speed 的返回值上。</summary>
    public static void SpeedPostfix(ref float __result)
    {
        float mult = _speed.Value;
        if (mult > 0f && Math.Abs(mult - 1f) > 0.001f)
            __result *= mult;
    }
}
