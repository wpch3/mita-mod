# ============================================================
# MiSide「全员拯救」Mod —— 一键编译 + 部署脚本 (Windows PowerShell)
#
# 用法示例：
#   ./tools/Build-And-Deploy.ps1 -MiSideDir "D:\SteamLibrary\steamapps\common\MiSide"
#
# 前置：
#   1. .NET SDK >= 6.0（建议 .NET 8）：winget install Microsoft.DotNet.SDK.8
#   2. 已把 <游戏目录>/BepInEx/interop/*.dll 复制到本仓库 lib/interop/
#      （若仓库 ZIP 已自带 interop，此步可跳过）
# ============================================================
param(
    [string]$MiSideDir,
    [string]$Configuration = "Release"
)
$ErrorActionPreference = "Stop"

# --- 定位仓库根目录与本地产物 ---
$Root    = Split-Path -Parent $PSScriptRoot
$Sln     = Join-Path $Root "MitaMod.sln"
$DllPath = Join-Path $Root "src/MitaTrueEnding/bin/$Configuration/net6.0/MitaTrueEnding.dll"

# --- 检查 dotnet ---
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "未找到 dotnet。请先安装 .NET SDK 8：winget install Microsoft.DotNet.SDK.8 （装完重开 PowerShell）"
}

# --- 检查 interop ---
$InteropDir = Join-Path $Root "lib/interop"
if (-not (Test-Path (Join-Path $InteropDir "Assembly-CSharp.dll"))) {
    throw "lib/interop/ 下没有 Assembly-CSharp.dll。请按 lib/interop/README.md 从游戏目录生成并复制 interop DLL。"
}

# --- 编译 ---
Write-Host "[1/3] 编译 $Sln ($Configuration) ..." -ForegroundColor Cyan
dotnet build $Sln -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "编译失败，请查看上方错误信息。" }

# --- 部署 ---
if ([string]::IsNullOrWhiteSpace($MiSideDir)) {
    Write-Host "[2/3] 未指定 -MiSideDir，跳过部署。" -ForegroundColor Yellow
    Write-Host "      手动部署：复制 $DllPath 到 <游戏目录>/BepInEx/plugins/"
} else {
    if (-not (Test-Path (Join-Path $MiSideDir "MiSide.exe"))) {
        throw "在 $MiSideDir 下没找到 MiSide.exe，请确认 -MiSideDir 指向游戏根目录。"
    }
    $Plugins = Join-Path $MiSideDir "BepInEx/plugins"
    New-Item -ItemType Directory -Force $Plugins | Out-Null
    Copy-Item $DllPath $Plugins -Force
    Write-Host "[2/3] 已部署到 $Plugins" -ForegroundColor Green
}

Write-Host "[3/3] 完成。启动游戏后，在游戏目录 BepInEx/LogOutput.log 里应能看到 [MitaTE] 日志。" -ForegroundColor Green
