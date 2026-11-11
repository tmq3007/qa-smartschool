@echo off
echo =========================================================
echo   Launching Android Student Tablet Emulator (1920x1200)
echo   AVD: QA_Student_Tablet
echo =========================================================

taskkill /F /IM qemu-system-x86_64.exe >nul 2>&1
taskkill /F /IM emulator.exe >nul 2>&1

if not exist "%USERPROFILE%\.android\avd\QA_Student_Tablet.avd" (
    mkdir "%USERPROFILE%\.android\avd\QA_Student_Tablet.avd"
)

copy /Y "%~dp0tablet-avd-config.ini" "%USERPROFILE%\.android\avd\QA_Student_Tablet.avd\config.ini" >nul

(
echo avd.ini.encoding=UTF-8
echo path=%USERPROFILE%\.android\avd\QA_Student_Tablet.avd
echo path.rel=avd\QA_Student_Tablet.avd
echo target=android-34
) > "%USERPROFILE%\.android\avd\QA_Student_Tablet.ini"

del /F /Q "%USERPROFILE%\.android\avd\QA_Student_Tablet.avd\*.lock" >nul 2>&1
for /d %%d in ("%USERPROFILE%\.android\avd\QA_Student_Tablet.avd\*.lock") do rmdir /S /Q "%%d" >nul 2>&1

echo Registered AVDs:
"C:\Android\Sdk\emulator\emulator.exe" -list-avds

start "QA Student Tablet Emulator" "C:\Android\Sdk\emulator\emulator.exe" -avd QA_Student_Tablet -gpu auto

echo Waiting for Android Tablet to boot...
"C:\Android\Sdk\platform-tools\adb.exe" wait-for-device

:wait_boot
set BOOT=
for /f "tokens=*" %%i in ('"C:\Android\Sdk\platform-tools\adb.exe" -s emulator-5554 shell getprop sys.boot_completed 2^>nul') do set BOOT=%%i
if not "%BOOT%"=="1" (
    timeout /t 2 /nobreak >nul
    goto wait_boot
)

echo Installing and launching QA SmartClass Student...
"C:\Android\Sdk\platform-tools\adb.exe" -s emulator-5554 install -r "%~dp0releases\QASmartClass-Student-v4.2.0-debug.apk"
"C:\Android\Sdk\platform-tools\adb.exe" -s emulator-5554 shell am start -n com.qasmartclass.student.debug/com.qasmartclass.student.MainActivity
echo Done! Android Tablet window is ready.
