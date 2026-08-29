孩子们，我还是不适合恐怖游戏。

将“全员拯救”做成 Mod，核心逻辑在于**拦截主线后期的抹除事件，通过注入解密道具/条件触发器，引导多版本米塔达成数据和解**。

---

**剧情关键节点设计**

* **前置条件（伏笔收集）**：
* 在前期章节加入隐藏互动物品（例如：在二楼房间找到“未损坏的核心数据盘”，或在掌机小游戏中通关隐藏关卡获得“修复补丁”）。


* **冲突爆发与分支拦截（地下底层空间）**：
* 原版剧情中 3D 米塔准备抹除/处决善良米塔与 2D 小米塔。
* **Mod 分支触发**：检测到玩家持有修复数据，主角介入阻拦，触发特殊对话选项“共享世界权限”。


* **和解与重构（高潮演出）**：
* 主角使用终端运行修复代码，稳定濒临崩溃的虚拟空间底层。
* 3D 米塔放下偏执，接纳善良米塔（作为核心良知）与 2D 小米塔（作为底层管家）。


* **终局状态（真·和平模式）**：
* 场景重置为修缮一新的大房子，3D 米塔在客厅做饭，善良米塔在沙发看书，2D 小米塔在掌机/电视里互动。



---

**技术实现关键步骤**

**1. 标记位与道具注入（Flag & Inventory）**

* 用 dnSpy 检索 `PlayerInventory` 或 `GameManager` 中的物品列表。
* 编写 Harmony Patch，在玩家完成特定交互（如调查特定柜子/玩掌机）时，向存档字典写入自定义布尔值（如 `HasRepairPatch = true`）。

**2. 处决事件拦截（Scene & FSM Redirection）**

* 定位底层处决善良米塔的剧情控制器（通常是带有 `Timeline`、`PlayableDirector` 或特定 `Trigger` 脚本的 GameObject）。
* 使用 Harmony `Prefix` 拦截触发处决的方法：
```csharp
[HarmonyPatch(typeof(CutsceneController), "TriggerExecution")]
public static bool Prefix(CutsceneController __instance)
{
    if (SaveManager.GetBool("HasRepairPatch"))
    {
        // 阻止原版处决动画，启动自制拯救流程协程
        __instance.StartCoroutine(CustomTrueEndingRoutine());
        return false; // 拦截原版逻辑
    }
    return true;
}

```



**3. 对话与演出序列（Dialogue & Animation）**

* **台词替换**：Hook 游戏的 `DialogueManager.ShowDialogue`，按顺序喂入自定义文本 ID，驱动各角色的头像与字幕显示。
* **动作控制**：获取场上米塔角色的 `Animator` 组件，通过 `animator.Play("Talk")`、`animator.Play("Surprise")` 组合出和解动作。

**4. 终局场景多实体加载（Multi-Character Spawning）**

* 在和平场景（Peaceful Mode）加载后，利用 `GameObject.Instantiate` 动态生成善良米塔与 2D 小米塔的模型 Prefab。
* 将生成的实体挂载简单的碰撞体与交互脚本，使主角靠近时可分别触发独立日常对话。

---

**分步落地开发路线**

* **阶段 1（纯文本验证）**：先不调复杂动画，仅用 Harmony 拦截处决触发器，弹出自制拯救剧情文本，然后强制跳转至和平场景，跑通整个闭环。
* **阶段 2（场景与站位）**：在和平场景中动态生成另外两个米塔的模型，摆放好坐标与基础 Idle 待机动作。
* **阶段 3（润色交互）**：补充和解过场的运镜、音效，并为新共存的角色编写日常互动指令。
