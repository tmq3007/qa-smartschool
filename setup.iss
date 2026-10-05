#define MyAppName "QA SmartClass"
#define MyAppExeName "QASmartClass.exe"
#define BuildDir "d:\Document\_Projects\qa-smartschool\QASmartClass\bin\x64\Release\net9.0-windows10.0.19041.0\win-x64\publish"
#define MyAppVer GetFileVersion(BuildDir + "\" + MyAppExeName)

[Setup]
; Thông tin chung về ứng dụng
AppName={#MyAppName}
AppVersion={#MyAppVer}
AppPublisher=QA SmartSchool
AppPublisherURL=https://qasmartschool.com
DefaultDirName={autopf}\QA SmartSchool\QA SmartClass
DefaultGroupName=QA SmartSchool
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
; Nơi lưu file bộ cài (Setup.exe) sau khi build xong
OutputDir=d:\Document\_Projects\qa-smartschool\Installer
; Tên file cài đặt sẽ tự động lấy version (vd: QASmartClass_Setup_v1.0.0.0)
OutputBaseFilename=QASmartClass_Setup_v{#MyAppVer}

[Tasks]
; Tạo tùy chọn cho phép người dùng tạo biểu tượng ngoài màn hình Desktop (mặc định chọn sẵn)
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce

[Files]
; Sao chép TẤT CẢ các file trong thư mục publish vào thư mục cài đặt ({app})
Source: "{#BuildDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Tạo icon trong Start Menu
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
; Tạo icon ngoài Desktop (nếu người dùng tick chọn ở bước cài đặt)
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Tùy chọn chạy ứng dụng ngay sau khi cài đặt xong
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
