# ============================================================
# MitaTE 静态侦察扫描：一条命令代替 dnSpy 手工搜索
#
# 用法（在仓库根目录）：
#   ./tools/Recon-Scan.ps1
#
# 作用：构建并运行 tools/ReconDump，只读解析 lib/interop/Assembly-CSharp.dll，
#       在 recon/ 目录生成：
#         - assembly-csharp.all-types.txt        游戏全部类型清单
#         - assembly-csharp.keyword-report.txt   关键词命中的类型+方法签名
#       把这两个文件发给 Agent 即可开始填拦截点对照表。
# ============================================================
param(
    [string]$OutDir
)
$ErrorActionPreference = "Stop"

$Root    = Split-Path -Parent $PSScriptRoot
$Interop = Join-Path $Root "lib/interop"
if ([string]::IsNullOrWhiteSpace($OutDir)) {
    $OutDir = Join-Path $Root "recon"
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "未找到 dotnet。请先安装 .NET SDK 8：winget install Microsoft.DotNet.SDK.8 （装完重开 PowerShell）"
}
if (-not (Test-Path (Join-Path $Interop "Assembly-CSharp.dll"))) {
    throw "lib/interop/ 下没有 Assembly-CSharp.dll。先跑一次 Build-And-Deploy.ps1（它会从游戏目录自动复制 interop）。"
}

New-Item -ItemType Directory -Force $OutDir | Out-Null

Write-Host "[ReconScan] 构建并运行 ReconDump ..." -ForegroundColor Cyan
dotnet run -c Release --project (Join-Path $Root "tools/ReconDump") -- "$Interop" "$OutDir"
if ($LASTEXITCODE -ne 0) { throw "ReconDump 运行失败，请查看上方输出。" }

Write-Host "[ReconScan] 完成。报告目录：$OutDir" -ForegroundColor Green
Write-Host "          把 assembly-csharp.all-types.txt 和 assembly-csharp.keyword-report.txt 发给 Agent。"
