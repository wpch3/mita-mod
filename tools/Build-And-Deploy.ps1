# ============================================================
# MiSide「全员拯救」Mod —— 一键编译 + 部署脚本 (Windows PowerShell 5.1+)
#
# 用法示例：
#   ./tools/Build-And-Deploy.ps1 -MiSideDir "D:\steam\steamapps\common\MiSide"
#
# 前置：
#   1. .NET SDK >= 6.0（建议 .NET 8）：winget install Microsoft.DotNet.SDK.8
#   2. 已把 BepInEx 6 (IL2CPP) 解压到游戏根目录，并至少运行过一次游戏到主菜单
#      说明：脚本发现仓库 lib/interop 为空时，会自动从游戏目录复制 interop（需带 -MiSideDir）
# ============================================================
param(
    [string]$MiSideDir,
    [string]$Configuration = "Release"
)
$ErrorActionPreference = "Stop"

$Root       = Split-Path -Parent $PSScriptRoot
$Sln        = Join-Path $Root "MitaMod.sln"
$InteropDir = Join-Path $Root "lib/interop"
$DllPath    = Join-Path $Root "src/MitaTrueEnding/bin/$Configuration/net6.0/MitaTrueEnding.dll"

# --- 0. 检查 dotnet ---
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "未找到 dotnet。请先安装 .NET SDK 8：winget install Microsoft.DotNet.SDK.8 （装完请重开 PowerShell）"
}

# --- [1/3] interop 齐全检查（仓库没有则从游戏目录自动复制） ---
if (-not (Test-Path (Join-Path $InteropDir "Assembly-CSharp.dll"))) {
    $GameInterop = $null
    if (-not [string]::IsNullOrWhiteSpace($MiSideDir)) {
        $GameInterop = Join-Path $MiSideDir "BepInEx/interop"
    }
    if ($GameInterop -and (Test-Path (Join-Path $GameInterop "Assembly-CSharp.dll"))) {
        Write-Host "[1/3] 仓库 lib/interop 为空，已从游戏目录自动复制 interop。" -ForegroundColor Cyan
        New-Item -ItemType Directory -Force $InteropDir | Out-Null
        Copy-Item (Join-Path $GameInterop "*.dll") $InteropDir -Force
    } else {
        throw @"
lib/interop/ 下没有 Assembly-CSharp.dll，且在「$MiSideDir\BepInEx\interop\」里也没找到可自动复制的 interop。
请按顺序检查：
  1. BepInEx 6 (IL2CPP) 是否已解压到游戏根目录
     （MiSide.exe 旁边应直接出现 BepInEx 文件夹、winhttp.dll、doorstop_config.ini，
       注意不要多套一层文件夹）
  2. 是否运行过一次游戏并到达主菜单（首次运行会生成 BepInEx\interop\，耗时较长）
     成功后应存在：<游戏目录>\BepInEx\interop\Assembly-CSharp.dll
  3. 然后重跑本脚本（带 -MiSideDir），我会自动复制；
     或手动复制 <游戏目录>\BepInEx\interop\*.dll 到本仓库 lib\interop\
"@
    }
} else {
    Write-Host "[1/3] interop 已就位。" -ForegroundColor Cyan
}

# --- [2/3] 编译 ---
Write-Host "[2/3] 编译 $Sln ($Configuration) ..." -ForegroundColor Cyan
dotnet build $Sln -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "编译失败，请查看上方错误信息。" }

# --- [3/3] 部署 ---
if ([string]::IsNullOrWhiteSpace($MiSideDir)) {
    Write-Host "[3/3] 未指定 -MiSideDir，跳过部署。" -ForegroundColor Yellow
    Write-Host "      手动部署：复制 $DllPath 到 <游戏目录>\BepInEx\plugins\"
} else {
    if (-not (Test-Path (Join-Path $MiSideDir "MiSide.exe"))) {
        throw "在 $MiSideDir 下没找到 MiSide.exe，请确认 -MiSideDir 指向游戏根目录。"
    }
    $Plugins = Join-Path $MiSideDir "BepInEx/plugins"
    New-Item -ItemType Directory -Force $Plugins | Out-Null
    Copy-Item $DllPath $Plugins -Force
    Write-Host "[3/3] 已部署到 $Plugins" -ForegroundColor Green
}

Write-Host "完成。启动游戏后，在 <游戏目录>\BepInEx\LogOutput.log 里应能看到 [MitaTE] 日志。" -ForegroundColor Green
