# publish 폴더를 재귀적으로 스캔해 모든 파일/하위폴더를 WiX 컴포넌트로 자동 생성한다 (installer\Files.wxs).
# 파일명에 한글이 있어도 상관없다 (Component/File Id 는 경로 해시로 만든 ASCII 식별자를 쓰고,
# 실제 파일명(Name)과 원본 경로(Source)만 원래 값을 그대로 사용한다).
param(
    [string]$PublishDir = "C:\git_extension\PriceCalcApp\publish\win-x64",
    [string]$OutFile = "C:\git_extension\PriceCalcApp\installer\Files.wxs"
)

$ErrorActionPreference = "Stop"

function New-Id([string]$seed) {
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($seed)
    $hash = [System.Security.Cryptography.SHA1]::Create().ComputeHash($bytes)
    $hex = ($hash | ForEach-Object { $_.ToString("x2") }) -join ""
    return "f" + $hex.Substring(0, 24)
}

$publishFull = (Resolve-Path $PublishDir).Path
$allFiles = Get-ChildItem -Path $publishFull -Recurse -File

$relDirs = @{}
foreach ($f in $allFiles) {
    $rel = $f.DirectoryName.Substring($publishFull.Length).TrimStart('\')
    if ($rel -ne "") { $relDirs[$rel] = $true }
}
$sortedDirs = $relDirs.Keys | Sort-Object { $_.Split('\').Count }, { $_ }
$dirIdOf = @{ "" = "INSTALLFOLDER" }
foreach ($rel in $sortedDirs) { $dirIdOf[$rel] = "d_" + (New-Id $rel) }

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('<?xml version="1.0" encoding="utf-8"?>')
[void]$sb.AppendLine('<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">')
[void]$sb.AppendLine('  <Fragment>')

function Emit-Dir($relPath, $indent) {
    $pad = " " * $indent
    $children = $sortedDirs | Where-Object {
        $p = $_.Split('\')
        $parent = if ($p.Count -gt 1) { ($p[0..($p.Count-2)]) -join '\' } else { "" }
        $parent -eq $relPath
    }
    foreach ($c in $children) {
        $name = $c.Split('\')[-1]
        $dirId = $dirIdOf[$c]
        $escName = [System.Security.SecurityElement]::Escape($name)
        [void]$sb.AppendLine("$pad<Directory Id=`"$dirId`" Name=`"$escName`">")
        Emit-Dir $c ($indent + 2)
        [void]$sb.AppendLine("$pad</Directory>")
    }
}

[void]$sb.AppendLine('    <DirectoryRef Id="INSTALLFOLDER">')
Emit-Dir "" 6
[void]$sb.AppendLine('    </DirectoryRef>')

[void]$sb.AppendLine('    <ComponentGroup Id="AppFiles">')
foreach ($f in $allFiles) {
    $rel = $f.DirectoryName.Substring($publishFull.Length).TrimStart('\')
    $dirId = $dirIdOf[$rel]
    $relFile = $f.FullName.Substring($publishFull.Length).TrimStart('\')
    $compId = "c_" + (New-Id $relFile)
    $fileId = "fi_" + (New-Id $relFile)
    $escName = [System.Security.SecurityElement]::Escape($f.Name)
    $escSrc = [System.Security.SecurityElement]::Escape($f.FullName)
    [void]$sb.AppendLine("      <Component Id=`"$compId`" Directory=`"$dirId`" Guid=`"*`">")
    [void]$sb.AppendLine("        <File Id=`"$fileId`" Source=`"$escSrc`" Name=`"$escName`" KeyPath=`"yes`" />")
    [void]$sb.AppendLine("      </Component>")
}
[void]$sb.AppendLine('    </ComponentGroup>')
[void]$sb.AppendLine('  </Fragment>')
[void]$sb.AppendLine('</Wix>')

New-Item -ItemType Directory -Force -Path (Split-Path $OutFile) | Out-Null
[System.IO.File]::WriteAllText($OutFile, $sb.ToString(), (New-Object System.Text.UTF8Encoding($false)))
Write-Output "Wrote $OutFile with $($allFiles.Count) files, $($sortedDirs.Count) subdirectories."
