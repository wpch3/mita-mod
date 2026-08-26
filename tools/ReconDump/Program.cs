using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

// ============================================================
// MitaTE ReconDump —— interop 程序集静态扫描器
// 作用：代替手工 dnSpy 搜索，只读解析游戏的 Assembly-CSharp.dll，
//       生成全类型清单 + 按关键词分组的方法签名报告。
// 用法：dotnet run -c Release --project tools/ReconDump -- <interop目录> <输出目录>
//       （一般直接用 tools/Recon-Scan.ps1 包装调用）
// 安全说明：只做元数据只读解析，不执行任何游戏代码。
// ============================================================

string interopDir = Path.GetFullPath(args.Length >= 1 ? args[0] : "lib/interop");
string outDir = Path.GetFullPath(args.Length >= 2 ? args[1] : "recon");
Directory.CreateDirectory(outDir);

string target = Path.Combine(interopDir, "Assembly-CSharp.dll");
if (!File.Exists(target))
{
    Console.Error.WriteLine($"[ReconDump] 找不到 {target}");
    Console.Error.WriteLine("[ReconDump] 先把游戏 interop 拷到 lib/interop（跑 Build-And-Deploy.ps1 会自动复制）。");
    return 2;
}

// 依赖解析 = interop 目录（游戏壳，同名优先）+ .NET 运行时目录（提供真正的 corlib/基元类型：
// Il2Cppmscorlib 壳里缺 System.IntPtr 等定义，上次单文件喂法在你的环境没生效，
// 这里改用 RuntimeEnvironment.GetRuntimeDirectory() 全量喂入——这是官方文档的标准姿势。
// 必须按简单名去重：PathAssemblyResolver 不允许同名程序集出现两次。
var byName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
foreach (string p in Directory.GetFiles(interopDir, "*.dll"))
    byName[Path.GetFileNameWithoutExtension(p)] = p;
foreach (string p in Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll"))
    byName.TryAdd(Path.GetFileNameWithoutExtension(p), p);
using var mlc = new MetadataLoadContext(new PathAssemblyResolver(byName.Values), "System.Private.CoreLib");
Assembly asm = mlc.LoadFromAssemblyPath(target);

Type[] types;
try
{
    types = asm.GetTypes();
}
catch (ReflectionTypeLoadException ex)
{
    int failed = ex.LoaderExceptions.Count(e => e != null);
    types = ex.Types.Where(t => t != null).ToArray()!;
    Console.WriteLine($"[ReconDump] 警告：{failed} 个类型未能解析，已跳过（不影响其余结果）。");
}
types = types.OrderBy(t => Safe(() => t.FullName ?? t.Name, "??")).ToArray();
Console.WriteLine($"[ReconDump] Assembly-CSharp.dll 共 {types.Length} 个类型。");

// ---------- 输出 1：全类型清单（逐行全名，后续可随便 grep） ----------
string allTypesPath = Path.Combine(outDir, "assembly-csharp.all-types.txt");
File.WriteAllLines(allTypesPath, types.Select(t => Safe(() => t.FullName ?? t.Name, "??")));
Console.WriteLine($"[ReconDump] 写出 {allTypesPath}");

// ---------- 输出 2：关键词命中报告（类型名 + 方法名两级） ----------
(string Group, string[] Keys)[] typeGroups =
{
    ("存档与进度",   new[] { "save", "storage", "progress", "prefs" }),
    ("对话与文本",   new[] { "dialog", "talk", "phrase", "subtitle", "voice", "speech" }),
    ("剧情与过场",   new[] { "cutscene", "cinema", "timeline", "sequence", "playable" }),
    ("结局相关",     new[] { "ending", "finale" }),
    ("死亡与重置",   new[] { "kill", "death", "dead", "die", "restart", "reset", "reboot", "erase", "destroy" }),
    ("场景与章节",   new[] { "scene", "chapter", "level", "location", "world" }),
    ("角色与玩偶",   new[] { "mita", "mila", "cappie", "player", "character", "doll" }),
    ("核心与终端",   new[] { "core", "terminal", "console", "computer", "cartridge" }),
    ("交互与物品",   new[] { "interact", "item", "inventory", "pickup" }),
    ("流程与管理器", new[] { "manager", "controller", "trigger", "event", "state", "fsm", "quest" }),
};
string[] methodKeywords =
{
    "kill", "death", "execute", "erase", "delete", "restart", "reset", "reboot",
    "dialogue", "talk", "cutscene", "ending", "savegame", "loadgame",
    "cartridge", "glitch", "spawn", "banish", "punish"
};

const int MaxTypesPerGroup = 250;
const int MaxMethodsPerType = 80;
const int MaxMethodHitsPerKey = 60;
var sb = new StringBuilder();
sb.AppendLine("# Assembly-CSharp 关键词命中报告（ReconDump 生成）");
sb.AppendLine($"# interop 目录: {interopDir}");
sb.AppendLine($"# 类型总数: {types.Length}");
sb.AppendLine();

foreach (var (group, keys) in typeGroups)
{
    var hits = types.Where(t =>
    {
        string n = Safe(() => t.FullName ?? "", "").ToLowerInvariant();
        return keys.Any(k => n.Contains(k));
    }).ToList();

    sb.AppendLine($"================ 关键词组: {group}（{string.Join(", ", keys)}） — 命中 {hits.Count} 个类型 ================");
    foreach (var t in hits.Take(MaxTypesPerGroup))
    {
        sb.AppendLine($"[TYPE] {Safe(() => t.FullName ?? t.Name, "??")}");
        foreach (var f in SafeFields(t).Take(30))
            sb.AppendLine($"    field {(f.IsPublic ? "+" : "-")} {Safe(() => Simple(f.FieldType), "?")} {f.Name}");
        foreach (var p in SafeProps(t).Take(30))
            sb.AppendLine($"    prop  {(p.GetMethod?.IsPublic == true ? "+" : "-")} {(p.CanRead ? Safe(() => Simple(p.PropertyType), "?") : "?")} {p.Name}");
        foreach (var m in SafeMethods(t).Where(m => !m.IsSpecialName).Take(MaxMethodsPerType))
            sb.AppendLine($"    meth  {(m.IsPublic ? "+" : "-")} {Safe(() => Simple(m.ReturnType), "?")} {m.Name}({Params(m)})");
        sb.AppendLine();
    }
    if (hits.Count > MaxTypesPerGroup)
        sb.AppendLine($"    …… 另有 {hits.Count - MaxTypesPerGroup} 个类型略（见 all-types.txt）");
    sb.AppendLine();
}

sb.AppendLine("================ 方法名关键词补捕（类型名没命中、但方法名可疑的） ================");
foreach (string kw in methodKeywords)
{
    var hits = types
        .SelectMany(t => SafeMethods(t)
            .Where(m => !m.IsSpecialName && m.Name.Contains(kw, StringComparison.OrdinalIgnoreCase))
            .Select(m => $"{Safe(() => t.FullName ?? t.Name, "??")} :: {m.Name}({Params(m)})"))
        .Distinct()
        .ToList();
    sb.AppendLine($"---- [{kw}] 命中 {hits.Count} 处 ----");
    foreach (string h in hits.Take(MaxMethodHitsPerKey))
        sb.AppendLine($"    {h}");
    if (hits.Count > MaxMethodHitsPerKey)
        sb.AppendLine($"    …… 另有 {hits.Count - MaxMethodHitsPerKey} 处略");
    sb.AppendLine();
}

string reportPath = Path.Combine(outDir, "assembly-csharp.keyword-report.txt");
File.WriteAllText(reportPath, sb.ToString());
Console.WriteLine($"[ReconDump] 写出 {reportPath}");
Console.WriteLine("[ReconDump] 完成。把这两个 txt 发回即可。");
return 0;

// ---------- 工具函数 ----------
static string Safe(Func<string?> f, string fallback)
{
    try { return f() ?? fallback; } catch { return fallback; }
}

static string Simple(Type t)
{
    try
    {
        string n = t.Name;
        int tick = n.IndexOf('`');
        if (tick > 0) n = n[..tick];
        if (t.IsGenericType && !t.IsGenericTypeDefinition)
            n += "<" + string.Join(",", t.GetGenericArguments().Select(Simple)) + ">";
        return n;
    }
    catch { return "?"; }
}

static string Params(MethodBase m)
{
    try
    {
        return string.Join(", ", m.GetParameters().Select(p => Simple(p.ParameterType) + " " + p.Name));
    }
    catch { return "<?>"; }
}

static BindingFlags AllDeclFlags() =>
    BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static |
    BindingFlags.Public | BindingFlags.NonPublic;

static IEnumerable<MethodInfo> SafeMethods(Type t)
{
    try { return t.GetMethods(AllDeclFlags()); } catch { return Enumerable.Empty<MethodInfo>(); }
}

static IEnumerable<FieldInfo> SafeFields(Type t)
{
    try { return t.GetFields(AllDeclFlags()); } catch { return Enumerable.Empty<FieldInfo>(); }
}

static IEnumerable<PropertyInfo> SafeProps(Type t)
{
    try { return t.GetProperties(AllDeclFlags()); } catch { return Enumerable.Empty<PropertyInfo>(); }
}
