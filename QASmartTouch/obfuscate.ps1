param(
    [Parameter(Mandatory=$true)]
    [string]$TargetDllPath,
    [Parameter(Mandatory=$true)]
    [string]$ReferenceDir,
    [string]$SecondaryTargetDllPath = ""
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$obfuscarExe = Join-Path $repoRoot ".tools\obfuscar.console.exe"

if (-not (Test-Path $obfuscarExe)) {
    Write-Error "Obfuscar tool not found at: $obfuscarExe"
    exit 1
}

$targetDllFull = [System.IO.Path]::GetFullPath($TargetDllPath)
$refDirFull = [System.IO.Path]::GetFullPath($ReferenceDir).TrimEnd('\')

if (-not (Test-Path $targetDllFull)) {
    Write-Error "Target DLL not found: $targetDllFull"
    exit 1
}

$stampFile = "$targetDllFull.obf_stamp"
$currentTicks = (Get-Item $targetDllFull).LastWriteTimeUtc.Ticks.ToString()
if ((Test-Path $stampFile) -and ((Get-Content $stampFile -Raw).Trim() -eq $currentTicks)) {
    Write-Host "[Obfuscar] QASmartTouch.dll is already obfuscated (incremental build); skipping re-obfuscation." -ForegroundColor Green
    if (-not [string]::IsNullOrWhiteSpace($SecondaryTargetDllPath)) {
        $secFull = [System.IO.Path]::GetFullPath($SecondaryTargetDllPath)
        if (Test-Path ([System.IO.Path]::GetDirectoryName($secFull))) {
            Copy-Item $targetDllFull -Destination $secFull -Force
        }
    }
    exit 0
}

# Locate .NET 9.0 shared runtimes for NETCore and WPF
$dotnetShared = "C:\Program Files\dotnet\shared"
$netCoreDir = Get-ChildItem "$dotnetShared\Microsoft.NETCore.App" -Directory | Where-Object { $_.Name -like "9.*" } | Sort-Object Name -Descending | Select-Object -First 1
$wpfDir = Get-ChildItem "$dotnetShared\Microsoft.WindowsDesktop.App" -Directory | Where-Object { $_.Name -like "9.*" } | Sort-Object Name -Descending | Select-Object -First 1

$workDir = Join-Path ([System.IO.Path]::GetDirectoryName($targetDllFull)) "_obf_work"
$inDir = Join-Path $workDir "in"
$outDir = Join-Path $workDir "out"

if (Test-Path $workDir) {
    Remove-Item $workDir -Recurse -Force
}
New-Item -ItemType Directory -Path $inDir -Force | Out-Null
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

Copy-Item $targetDllFull -Destination (Join-Path $inDir "QASmartTouch.dll") -Force

$searchPathsXml = "  <AssemblySearchPath path=`"$refDirFull`" />`r`n"
if ($netCoreDir) {
    $searchPathsXml += "  <AssemblySearchPath path=`"$($netCoreDir.FullName)`" />`r`n"
}
if ($wpfDir) {
    $searchPathsXml += "  <AssemblySearchPath path=`"$($wpfDir.FullName)`" />`r`n"
}

$configXmlPath = Join-Path $workDir "obfuscar.xml"
$configContent = @"
<?xml version='1.0'?>
<Obfuscator>
  <Var name="InPath" value="$inDir" />
  <Var name="OutPath" value="$outDir" />
  <Var name="KeepPublicApi" value="true" />
  <Var name="HidePrivateApi" value="true" />
  <Var name="HideStrings" value="true" />
  <Var name="UseUnicodeNames" value="true" />
  <Var name="RenameProperties" value="true" />
  <Var name="RenameEvents" value="true" />
  <Var name="RenameFields" value="true" />
  <Var name="OptimizeMethods" value="true" />
  <Var name="SuppressIldasm" value="true" />
$searchPathsXml
  <Module file="`$(InPath)\QASmartTouch.dll">
    <SkipNamespace name="*Properties*" />
    <SkipType name="*AnonymousType*" skipProperties="true" skipMethods="true" skipFields="true" />
    <ForceType name="QASmartTouch.WhiteboardCore.*" forceProperties="true" forceMethods="true" forceFields="true" />
    <ForceType name="QASmartTouch.Handlers.*" forceProperties="true" forceMethods="true" forceFields="true" />
    <ForceType name="QASmartTouch.Managers.DrawingEngine" forceProperties="true" forceMethods="true" forceFields="true" />
    <ForceType name="QASmartTouch.Managers.EraserEngine" forceProperties="true" forceMethods="true" forceFields="true" />
    <ForceType name="QASmartTouch.Managers.TouchManager" forceProperties="true" forceMethods="true" forceFields="true" />
    <ForceType name="QASmartTouch.Managers.ZoomManager" forceProperties="true" forceMethods="true" forceFields="true" />
    <ForceType name="QASmartTouch.Services.TransformService" forceProperties="true" forceMethods="true" forceFields="true" />
    <ForceType name="QASmartTouch.Services.SelectionManager" forceProperties="true" forceMethods="true" forceFields="true" />
    <ForceType name="QASmartTouch.Services.CanvasExportService" forceProperties="true" forceMethods="true" forceFields="true" />
    <ForceType name="QASmartTouch.Services.ScreenRecorderService" forceProperties="true" forceMethods="true" forceFields="true" />
    <ForceType name="QASmartTouch.Services.ScreenCaptureService" forceProperties="true" forceMethods="true" forceFields="true" />
    <ForceType name="QASmartTouch.Services.SmartHandwritingRecognitionService" forceProperties="true" forceMethods="true" forceFields="true" />
    <ForceType name="QASmartTouch.Services.HandwritingRecognitionService" forceProperties="true" forceMethods="true" forceFields="true" />
    <ForceType name="QASmartTouch.Services.WindowsOCRService" forceProperties="true" forceMethods="true" forceFields="true" />
    <ForceType name="QASmartTouch.Services.GoogleVisionOCRService" forceProperties="true" forceMethods="true" forceFields="true" />
    <ForceType name="QASmartTouch.Services.License.HardwareFingerprint" forceProperties="true" forceMethods="true" forceFields="true" />
  </Module>
</Obfuscator>
"@

[System.IO.File]::WriteAllText($configXmlPath, $configContent, [System.Text.Encoding]::UTF8)

Write-Host "[Obfuscar] Encrypting strings and obfuscating IL for QASmartTouch.dll..." -ForegroundColor Cyan
& $obfuscarExe $configXmlPath
if ($LASTEXITCODE -ne 0) {
    Write-Error "Obfuscar failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

$obfuscatedDll = Join-Path $outDir "QASmartTouch.dll"
if (-not (Test-Path $obfuscatedDll)) {
    Write-Error "Obfuscated QASmartTouch.dll not found!"
    exit 1
}

Copy-Item $obfuscatedDll -Destination $targetDllFull -Force
$newTicks = (Get-Item $targetDllFull).LastWriteTimeUtc.Ticks.ToString()
[System.IO.File]::WriteAllText($stampFile, $newTicks, [System.Text.Encoding]::UTF8)

if (-not [string]::IsNullOrWhiteSpace($SecondaryTargetDllPath)) {
    $secFull = [System.IO.Path]::GetFullPath($SecondaryTargetDllPath)
    if (Test-Path ([System.IO.Path]::GetDirectoryName($secFull))) {
        Copy-Item $obfuscatedDll -Destination $secFull -Force
    }
}

Remove-Item $workDir -Recurse -Force
Write-Host "[Obfuscar] Successfully protected QASmartTouch.dll!" -ForegroundColor Green
