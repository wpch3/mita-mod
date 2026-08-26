#Requires -Version 5.1
<#
.SYNOPSIS
    BepInEx 插件兼容性 / UniverseLib 撞车 自检与修复工具（MiSide mod 排障专用）。
.DESCRIPTION
    模式一（-DllPath）：验明单个 dll —— 列出它引用的 BepInEx / UniverseLib 程序集，
        判断与 be.577+ 的兼容性；若它本身是 UniverseLib，则探测是否含 yukieiji
        fork 的扩展字段 Disable_Setup_Force_ReLoad_ManagedAssemblies（UE 4.13.6 必需）。
    模式二（-ScanDir）：递归扫描目录里所有 UniverseLib*.dll，逐个报告
        程序集名 / 大小 / 哈希 / 是否含 fork 扩展字段，并指出同名不同内容的撞车。
    模式三（-ScanDir + -FixSource）：把扫描到的【与 FixSource 同名】的 dll 全部
        覆盖为 FixSource（自动先备份为 .bak）。用于统一被旧版抢占的 UniverseLib。
.EXAMPLE
    ./tools/Test-PluginCompat.ps1 -DllPath "D:\downloads\UnityExplorer.BIE.Unity.IL2CPP.CoreCLR.dll"
.EXAMPLE
    ./tools/Test-PluginCompat.ps1 -ScanDir "D:\steam\steamapps\common\MiSide\BepInEx"
.EXAMPLE
    ./tools/Test-PluginCompat.ps1 -ScanDir "D:\steam\steamapps\common\MiSide\BepInEx" `
        -FixSource "D:\steam\steamapps\common\MiSide\BepInEx\plugins\UnityExplorer\UniverseLib.BIE.IL2CPP.Interop.dll"
#>
param(
    [Parameter(Position = 0)]
    [string]$DllPath,
    [string]$ScanDir,
    [string]$FixSource
)

$ErrorActionPreference = 'Stop'
$ForkField = 'Disable_Setup_Force_ReLoad_ManagedAssemblies'

function Test-ForkField([string]$Path) {
    # 只读字节做字符串探测：.NET 元数据的字段名以 UTF-8 明文存在 #Strings 堆里
    $s = [System.Text.Encoding]::ASCII.GetString([System.IO.File]::ReadAllBytes($Path))
    return $s.Contains($ForkField)
}

function Show-DllVerdict([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        Write-Host "找不到文件: $Path" -ForegroundColor Red
        exit 1
    }
    try {
        $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $Path).Path)
        $asm = [System.Reflection.Assembly]::ReflectionOnlyLoad($bytes)
    } catch {
        Write-Host "这不是一个有效的 .NET 程序集: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }

    Write-Host ""
    Write-Host ("程序集名 : " + $asm.GetName().Name)
    Write-Host ("版本     : " + $asm.GetName().Version)

    $refs    = @($asm.GetReferencedAssemblies() | ForEach-Object { $_.Name })
    $bepRefs = @($refs | Where-Object { $_ -like 'BepInEx*' })
    $uniRefs = @($refs | Where-Object { $_ -like 'UniverseLib*' })

    if ($bepRefs.Count -gt 0) {
        Write-Host ""
        Write-Host "BepInEx 相关引用:"
        $bepRefs | ForEach-Object { Write-Host ("  - " + $_) }
        Write-Host ""
        if ($bepRefs -contains 'BepInEx.Unity.IL2CPP') {
            Write-Host "[OK]  新命名 BepInEx.Unity.IL2CPP -> 与 be.577+ 兼容，能正常加载。" -ForegroundColor Green
        } elseif ($bepRefs -contains 'BepInEx.IL2CPP') {
            Write-Host "[!!]  旧命名 BepInEx.IL2CPP -> 在 be.577+ 会被静默跳过。请换文件名带 .Unity. 的版本。" -ForegroundColor Red
        }
    }

    if ($uniRefs.Count -gt 0) {
        Write-Host ""
        Write-Host "UniverseLib 相关引用:"
        $uniRefs | ForEach-Object { Write-Host ("  - " + $_) }
        if ($uniRefs -contains 'UniverseLib.BIE.IL2CPP.Interop') {
            Write-Host "      -> 需要 yukieiji 命名的 UniverseLib.BIE.IL2CPP.Interop.dll 陪跑。"
        }
        if ($uniRefs -contains 'UniverseLib.IL2CPP.Interop') {
            Write-Host "      -> 需要官方命名的 UniverseLib.IL2CPP.Interop.dll 陪跑。"
        }
    }

    if ($asm.GetName().Name -like 'UniverseLib*') {
        Write-Host ""
        if (Test-ForkField (Resolve-Path -LiteralPath $Path).Path) {
            Write-Host "[OK]  含 yukieiji fork 扩展字段 ($ForkField)" -ForegroundColor Green
            Write-Host "      -> 新版 fork，可与 UnityExplorer 4.13.6 搭配。"
        } else {
            Write-Host "[!!]  不含扩展字段 ($ForkField)" -ForegroundColor Red
            Write-Host "      -> 旧版/官方 mainline，UnityExplorer 4.13.6 会抛 MissingFieldException。"
        }
    }
    Write-Host ""
}

function Invoke-Scan([string]$Dir) {
    if (-not (Test-Path -LiteralPath $Dir)) {
        Write-Host "找不到目录: $Dir" -ForegroundColor Red
        exit 1
    }
    $dlls = @(Get-ChildItem -LiteralPath $Dir -Recurse -File -Filter 'UniverseLib*.dll')
    Write-Host ""
    if ($dlls.Count -eq 0) {
        Write-Host "目录下没有发现任何 UniverseLib*.dll : $Dir" -ForegroundColor Yellow
        return $dlls
    }
    Write-Host ("发现 " + $dlls.Count + " 个 UniverseLib dll：")
    Write-Host ""
    foreach ($f in $dlls) {
        $hash = (Get-FileHash -LiteralPath $f.FullName -Algorithm SHA256).Hash.Substring(0, 12)
        $fork = if (Test-ForkField $f.FullName) { '新版fork(有字段)' } else { '旧版/官方(无字段)' }
        Write-Host ("  " + $f.Name)
        Write-Host ("    路径: " + $f.FullName)
        Write-Host ("    大小: " + $f.Length + "  哈希: " + $hash + "  判定: " + $fork)
    }
    Write-Host ""

    # 同名撞车检查
    $groups = $dlls | Group-Object Name
    $conflict = $false
    foreach ($g in $groups) {
        if ($g.Count -gt 1) {
            $hashes = @($g.Group | ForEach-Object { (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash } | Sort-Object -Unique)
            if ($hashes.Count -gt 1) {
                $conflict = $true
                Write-Host ("[!!] 撞车: " + $g.Name + " 有 " + $g.Count + " 份且内容不同 —— 哪份先被加载全凭运气，") -ForegroundColor Red
                Write-Host "      上面的 '旧版/官方(无字段)' 那份如果抢跑，UnityExplorer 就会 MissingFieldException。" -ForegroundColor Red
            }
        }
    }
    if (-not $conflict) {
        Write-Host "[OK]  未发现同名不同内容的 UniverseLib 撞车。" -ForegroundColor Green
    }
    Write-Host ""
    return $dlls
}

# ============================== 主流程 ==============================

if ($DllPath) {
    Show-DllVerdict $DllPath
    if (-not $ScanDir) { exit 0 }
}

if ($ScanDir) {
    $dlls = Invoke-Scan $ScanDir

    if ($FixSource) {
        if (-not (Test-Path -LiteralPath $FixSource)) {
            Write-Host "FixSource 文件不存在: $FixSource" -ForegroundColor Red
            exit 1
        }
        $srcFull = (Resolve-Path -LiteralPath $FixSource).Path
        $srcName = Split-Path $srcFull -Leaf
        if (-not (Test-ForkField $srcFull)) {
            Write-Host "[警告] FixSource 本身不含 fork 扩展字段，拿它统一可能没效果！" -ForegroundColor Yellow
        }
        $targets = @($dlls | Where-Object { $_.Name -eq $srcName -and $_.FullName -ne $srcFull })
        if ($targets.Count -eq 0) {
            Write-Host ("没有其他名为 " + $srcName + " 的副本需要统一。") -ForegroundColor Green
        } else {
            foreach ($t in $targets) {
                Copy-Item -LiteralPath $t.FullName -Destination ($t.FullName + '.bak') -Force
                Copy-Item -LiteralPath $srcFull -Destination $t.FullName -Force
                Write-Host ("已统一（原文件备份为 .bak）: " + $t.FullName) -ForegroundColor Green
            }
            Write-Host ""
            Write-Host "统一完成，重开游戏验证。MS_CustomModels 若异常，把对应 .bak 改回原名即可还原。" -ForegroundColor Cyan
        }
    } else {
        Write-Host "提示：带 -FixSource <正确dll路径> 可一键把同名副本全部统一（自动备份）。" -ForegroundColor Cyan
    }
    exit 0
}

if (-not $DllPath) {
    Write-Host "用法: ./tools/Test-PluginCompat.ps1 -DllPath <dll> | -ScanDir <目录> [-FixSource <统一基准dll>]"
    exit 1
}
