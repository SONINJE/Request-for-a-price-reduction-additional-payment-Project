# PriceCalcApp 빌드 -> 게시(publish) -> 설치 파일(MSI) 생성까지 한 번에 처리하는 스크립트.
# 사용법: installer 폴더에서 실행
#   powershell -ExecutionPolicy Bypass -File build_installer.ps1
#
# 필요 조건:
#   - Visual Studio 2022 (Desktop C++, .NET 데스크톱 개발 워크로드)
#   - .NET 8 SDK
#   - dotnet tool로 설치된 wix (없으면 아래에서 자동 설치)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$sln = Join-Path $root "PriceCalcApp.sln"
$publishDir = Join-Path $root "publish\win-x64"
$installerDir = $PSScriptRoot
$outDir = Join-Path $installerDir "out"

function Find-MSBuild {
    $vswhere = "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswhere) {
        $p = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe"
        if ($p) { return $p }
    }
    throw "MSBuild.exe를 찾을 수 없습니다. Visual Studio 2022가 설치되어 있는지 확인하세요."
}

Write-Host "==> 1/4 솔루션 빌드 (Release|x64)" -ForegroundColor Cyan
$msbuild = Find-MSBuild
& $msbuild $sln /p:Configuration=Release /p:Platform=x64 /m /nologo /v:minimal
if ($LASTEXITCODE -ne 0) { throw "빌드 실패 (exit $LASTEXITCODE)" }

Write-Host "==> 2/4 self-contained 게시 (win-x64)" -ForegroundColor Cyan
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
dotnet publish (Join-Path $root "WpfPriceApp\WpfPriceApp.csproj") -c Release -p:Platform=x64 -r win-x64 --self-contained true -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "게시 실패 (exit $LASTEXITCODE)" }

if (-not (Test-Path (Join-Path $publishDir "PriceCalcEngine.dll"))) {
    throw "게시 폴더에 PriceCalcEngine.dll이 없습니다. WpfPriceApp.csproj의 DLL 복사 경로를 확인하세요."
}

Write-Host "==> 3/4 설치 파일 목록(Files.wxs) 생성" -ForegroundColor Cyan
& (Join-Path $installerDir "gen_wxs.ps1") -PublishDir $publishDir -OutFile (Join-Path $installerDir "Files.wxs")

Write-Host "==> 4/4 MSI 빌드" -ForegroundColor Cyan
$env:PATH = "$env:PATH;$env:USERPROFILE\.dotnet\tools"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
Push-Location $installerDir
try {
    # wix 확장은 전역(-g)으로 설치되어 있어야 한다:
    #   wix extension add --global WixToolset.UI.wixext/5.0.2
    wix build "Product.wxs" "Files.wxs" -ext WixToolset.UI.wixext -arch x64 -o (Join-Path $outDir "PriceCalcAppSetup.msi")
    if ($LASTEXITCODE -ne 0) { throw "MSI 빌드 실패 (exit $LASTEXITCODE)" }
} finally {
    Pop-Location
}

Write-Host "완료: $(Join-Path $outDir 'PriceCalcAppSetup.msi')" -ForegroundColor Green
