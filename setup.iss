#define MyAppName "QA SmartBoard"
#define MyAppExeName "QASmartClass.exe"
#define BuildDir "QASmartClass\bin\x64\Release\net9.0-windows10.0.19041.0\win-x64\publish"
#define MyAppVer GetFileVersion(BuildDir + "\" + MyAppExeName)

[Setup]
; Thông tin chung về ứng dụng
AppName={#MyAppName}
AppVersion={#MyAppVer}
AppPublisher=QA SmartSchool
AppPublisherURL=https://qasmartschool.com
DefaultDirName={autopf}\QA SmartSchool\QA SmartBoard
DefaultGroupName=QA SmartSchool
UninstallDisplayName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2
SolidCompression=yes
; Nơi lưu file bộ cài (Setup.exe) sau khi build xong
OutputDir=Installer
; Tên file cài đặt sẽ tự động lấy version (vd: QASmartBoard_Setup_v1.0.0.0)
OutputBaseFilename=QASmartBoard_Setup_v{#MyAppVer}
; Yêu cầu Windows cập nhật liên kết định dạng file ngay sau khi cài đặt/gỡ cài đặt
ChangesAssociations=yes

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

[Registry]
; 1. Gắn đuôi file .qasc với định danh ProgID QASmartClass.Lecture
Root: HKA; Subkey: "Software\Classes\.qasc"; ValueType: string; ValueName: ""; ValueData: "QASmartClass.Lecture"; Flags: uninsdeletevalue
; 2. Đặt tên hiển thị cho loại tệp bài giảng
Root: HKA; Subkey: "Software\Classes\QASmartClass.Lecture"; ValueType: string; ValueName: ""; ValueData: "Bài giảng QA SmartBoard"; Flags: uninsdeletekey
; 3. Đặt biểu tượng Icon cho tệp bài giảng
Root: HKA; Subkey: "Software\Classes\QASmartClass.Lecture\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"
; 4. Cấu hình lệnh mở tệp khi người dùng click đúp
Root: HKA; Subkey: "Software\Classes\QASmartClass.Lecture\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

[Run]
; Tùy chọn chạy ứng dụng ngay sau khi cài đặt xong
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

