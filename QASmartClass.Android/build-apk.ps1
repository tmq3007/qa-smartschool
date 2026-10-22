# ═══════════════════════════════════════════════════════════════
# QA SmartClass — Script Build APK Android
# ═══════════════════════════════════════════════════════════════

param(
    [string]$BuildType = "debug"  # "debug" hoặc "release"
)

$ErrorActionPreference = "Stop"

Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host "  QA SMARTCLASS - TIẾN HÀNH XUẤT BẢN FILE ANDROID APK" -ForegroundColor Cyan
Write-Host "=====================================================" -ForegroundColor Cyan

# 1. Thiết lập JAVA_HOME (Ưu tiên JDK 17 hoặc 21 để tương thích Android Gradle Plugin)
if (-not $env:JAVA_HOME -or -not (Test-Path "$env:JAVA_HOME\bin\java.exe")) {
    $jdkPaths = @(
        "C:\Program Files\Java\jdk-17",
        "C:\Program Files\Java\jdk-21.0.10",
        "C:\Program Files\Java\jdk-23"
    )
    foreach ($path in $jdkPaths) {
        if (Test-Path "$path\bin\java.exe") {
            $env:JAVA_HOME = $path
            $env:PATH = "$path\bin;" + $env:PATH
            Write-Host "✓ Đã tự động cấu hình JAVA_HOME: $path" -ForegroundColor Green
            break
        }
    }
} else {
    Write-Host "✓ Sử dụng JAVA_HOME hiện tại: $env:JAVA_HOME" -ForegroundColor Green
}

# 2. Kiểm tra ANDROID_HOME / ANDROID_SDK_ROOT
if (-not $env:ANDROID_HOME -and -not $env:ANDROID_SDK_ROOT) {
    $standardSdkPaths = @(
        "$env:LOCALAPPDATA\Android\Sdk",
        "C:\Android\Sdk",
        "D:\Android\Sdk"
    )
    foreach ($sdk in $standardSdkPaths) {
        if (Test-Path $sdk) {
            $env:ANDROID_HOME = $sdk
            Write-Host "✓ Đã tìm thấy Android SDK tại: $sdk" -ForegroundColor Green
            break
        }
    }
}

if (-not $env:ANDROID_HOME) {
    Write-Host ""
    Write-Host "[!] Lưu ý: Chưa phát hiện Android SDK trên máy tính." -ForegroundColor Yellow
    Write-Host "    Để build trực tiếp ra file .apk, bạn có thể:" -ForegroundColor White
    Write-Host "    1. Mở thư mục này bằng Android Studio (Android Studio sẽ tự tải SDK và ấn Run/Build)." -ForegroundColor Cyan
    Write-Host "    2. Hoặc cài đặt Android Command Line Tools và đặt biến môi trường ANDROID_HOME." -ForegroundColor Cyan
    Write-Host "    Đường dẫn dự án Android: $PSScriptRoot" -ForegroundColor Yellow
    Write-Host ""
    return
}

# 3. Chạy lệnh Gradle Wrapper để build APK
Set-Location $PSScriptRoot
$task = if ($BuildType -eq "release") { "assembleRelease" } else { "assembleDebug" }

Write-Host "-> Đang thực thi lệnh: .\gradlew.bat $task..." -ForegroundColor Cyan
& .\gradlew.bat $task

if ($LASTEXITCODE -eq 0) {
    $outputDir = "$PSScriptRoot\app\build\outputs\apk\$BuildType"
    $releasesDir = "$PSScriptRoot\releases"
    if (-not (Test-Path $releasesDir)) {
        New-Item -ItemType Directory -Path $releasesDir -Force | Out-Null
    }

    Write-Host ""
    Write-Host "=====================================================" -ForegroundColor Green
    Write-Host "✓ XUẤT FILE APK THÀNH CÔNG!" -ForegroundColor Green
    Write-Host "  Thư mục chứa file APK gốc: $outputDir" -ForegroundColor White
    Write-Host "=====================================================" -ForegroundColor Green
    if (Test-Path $outputDir) {
        Get-ChildItem $outputDir -Filter "*.apk" | ForEach-Object {
            $destFile = Join-Path $releasesDir "QASmartClass-Student-v4.2.0-$BuildType.apk"
            Copy-Item -Path $_.FullName -Destination $destFile -Force
            Write-Host "  [APK Gốc]:     $($_.FullName) ($([math]::Round($_.Length / 1MB, 2)) MB)" -ForegroundColor Yellow
            Write-Host "  [Bản Release]: $destFile" -ForegroundColor Cyan
        }
    }
} else {
    Write-Host "[X] Quá trình build gặp lỗi (Exit code: $LASTEXITCODE)." -ForegroundColor Red
}
