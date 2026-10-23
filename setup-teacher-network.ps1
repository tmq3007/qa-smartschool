# Chạy file này bằng PowerShell "Run as Administrator" MỘT LẦN trên máy giáo viên.
# Mục đích: cho phép điện thoại/máy tính bảng thật truy cập server chiếu màn hình (HTTP 8080-8090)
# mà không cần chạy cả app bằng quyền Admin.

$ErrorActionPreference = "Stop"
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "[X] Hãy mở PowerShell bằng 'Run as Administrator' rồi chạy lại." -ForegroundColor Red
    exit 1
}

foreach ($port in 8080..8090) {
    netsh http delete urlacl url="http://+:$port/" | Out-Null
    netsh http add urlacl url="http://+:$port/" user=Everyone | Out-Null
}

netsh advfirewall firewall delete rule name="QA SmartClass Web 8080-8090" | Out-Null
netsh advfirewall firewall add rule name="QA SmartClass Web 8080-8090" dir=in action=allow protocol=TCP localport=8080-8090 enable=yes profile=any | Out-Null

netsh advfirewall firewall delete rule name="QA SmartClass Ports TCP" | Out-Null
netsh advfirewall firewall add rule name="QA SmartClass Ports TCP" dir=in action=allow protocol=TCP localport=29877,29879 enable=yes profile=any | Out-Null

Write-Host "✓ Đã mở cổng 8080-8090 và 29877/29879. Hãy khởi động lại app giáo viên." -ForegroundColor Green
