#Requires -Version 5.1
<#
.SYNOPSIS
    验明一个 BepInEx 插件 dll 与当前 BepInEx 6 (be.577+) 的兼容性。
.DESCRIPTION
    BepInEx 从 be.577 起把 IL2CPP 插件基类程序集 BepInEx.IL2CPP 改名为
    BepInEx.Unity.IL2CPP；引用旧名的插件会被静默跳过（日志毫无报错）。
    本工具只读解析 dll 元数据（读字节载入，不锁文件、不执行任何代码），
    列出全部 BepInEx 相关引用并给出结论。
.EXAMPLE
    ./tools/Test-PluginCompat.ps1 -DllPath "D:\downloads\UnityExplorer.BIE.Unity.IL2CPP.CoreCLR.dll"
#>
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$DllPath
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $DllPath)) {
    Write-Host "找不到文件: $DllPath" -ForegroundColor Red
    exit 1
}

try {
    $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $DllPath).Path)
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

Write-Host ""
if ($bepRefs.Count -eq 0) {
    Write-Host "它没有引用任何 BepInEx 程序集 —— 可能不是 BepInEx 插件，或是插件的附属库。" -ForegroundColor Yellow
} else {
    Write-Host "BepInEx 相关引用:"
    $bepRefs | ForEach-Object { Write-Host ("  - " + $_) }
}

Write-Host ""
if ($bepRefs -contains 'BepInEx.Unity.IL2CPP') {
    Write-Host "[OK]  新命名 BepInEx.Unity.IL2CPP -> 与 be.577+ (含你的 be.697) 兼容，能正常加载。" -ForegroundColor Green
} elseif ($bepRefs -contains 'BepInEx.IL2CPP') {
    Write-Host "[!!]  旧命名 BepInEx.IL2CPP -> 在 be.577+ 会被静默跳过。请换文件名带 .Unity. 的版本。" -ForegroundColor Red
} elseif ($bepRefs -contains 'BepInEx') {
    Write-Host "[??]  只引用 BepInEx（Mono/通用形态）-> IL2CPP 游戏请确认它不是 Mono 版。" -ForegroundColor Yellow
} else {
    Write-Host "[??]  未发现可判定的 BepInEx 引用。" -ForegroundColor Yellow
}
