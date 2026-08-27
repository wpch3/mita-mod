using System;
using System.Reflection;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MitaGodMode;

/// <summary>
/// 热键组件（v1.1）：F10 = 强制结束当前追杀/躲藏环节。
///
/// 背景（2026-08-26 用户实证）：电锯躲藏章开无敌被发现后，
/// 收场动作（StartAttack/Kill/PlayerLose）被拦截 → 剧本无法落幕 →
/// 外部计时器强制重置 → 她从入口无限轮回。
///
/// 解法不是"让攻击打空气"（目标重定向需要盲改她的目标字段，风险大），
/// 而是直接调用该环节**游戏自带的正牌落幕函数**：玩家正常逃出时游戏会调用
/// <c>Location6_MitaKiller.PlayerExit()</c> —— 我们替她提前宣布"他已经跑了"。
///
/// 安全性：
///   - 只在清单内的环节场景里起作用；当前场景没有目标时只写一行提示日志，无副作用；
///   - 落幕函数是原版正常路径（逃出的合法结局），不是乱改状态的野路子；
///   - 每个目标独立 try/catch，任何一步失败都不会波及游戏。
/// </summary>
public class GodBehaviour : MonoBehaviour
{
    public GodBehaviour(IntPtr ptr) : base(ptr) { }

    public GodBehaviour() : base(ClassInjector.DerivedConstructorPointer<GodBehaviour>())
        => ClassInjector.DerivedConstructorBody(this);

    /// <summary>
    /// 可强制结束的环节清单：（类型名, 落幕方法）。
    /// 想加新环节：先用 UE / [GodMode] 日志确认该场景的负责人类型和它的"逃出/完成"方法。
    /// </summary>
    private static readonly (string TypeName, string MethodName)[] SkipTargets =
    {
        ("Location6_MitaKiller", "PlayerExit"), // 地下室电锯躲藏章：玩家逃出 = 环节正式结束
    };

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.F10))
            TrySkipEncounter();
    }

    private void TrySkipEncounter()
    {
        string scene = "";
        try { scene = SceneManager.GetActiveScene().name; } catch { /* 场景名取不到不致命 */ }

        bool handled = false;
        foreach (var (typeName, methodName) in SkipTargets)
        {
            try
            {
                Type? type = AccessTools.TypeByName(typeName);
                if (type == null) continue;

                // IL2CPP 关键差异：interop 里 FindObjectsOfType 要的是 Il2CppSystem.Type，
                // 托管 System.Type 必须经 Il2CppType.From 换乘（CS1503 的修正）
                Il2CppSystem.Type? il2cppType = Il2CppInterop.Runtime.Il2CppType.From(type, false);
                if (il2cppType == null)
                {
                    Plugin.Log.LogWarning($"[GodMode] F10：{typeName} 的 IL2CPP 类型解析失败，跳过");
                    continue;
                }

                UnityEngine.Object[] found = UnityEngine.Object.FindObjectsOfType(il2cppType);
                if (found == null || found.Length == 0) continue;

                MethodInfo? exit = AccessTools.Method(type, methodName);
                if (exit == null)
                {
                    Plugin.Log.LogWarning($"[GodMode] F10：找到 {typeName} 但没有 {methodName} 方法，跳过");
                    continue;
                }

                foreach (UnityEngine.Object inst in found)
                    exit.Invoke(inst, null);

                handled = true;
                Plugin.Log.LogInfo(
                    $"[GodMode] F10 已强制结束环节：{typeName}.{methodName} × {found.Length} 个实例（场景 {scene}）");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"[GodMode] F10 处理 {typeName} 时出错（已忽略，不影响游戏）: {e.Message}");
            }
        }

        if (!handled)
        {
            Plugin.Log.LogInfo(
                $"[GodMode] F10：当前场景（{scene}）没有可强制结束的追杀/躲藏环节（白名单见 GodBehaviour.SkipTargets）");
        }
    }
}
