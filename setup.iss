[Setup]
; Thông tin chung về ứng dụng
AppName=QA SmartClass
AppVersion=1.0
AppPublisher=QA SmartSchool
AppPublisherURL=https://qasmartschool.com
DefaultDirName={autopf}\QA SmartSchool\QA SmartClass
DefaultGroupName=QA SmartSchool
UninstallDisplayIcon={app}\QASmartClass.exe
Compression=lzma2
SolidCompression=yes
; Nơi lưu file bộ cài (Setup.exe) sau khi build xong
OutputDir=d:\Document\_Projects\qa-smartschool\Installer
OutputBaseFilename=QASmartClass_Setup_v1.0

[Tasks]
; Tạo tùy chọn cho phép người dùng tạo biểu tượng ngoài màn hình Desktop (mặc định chọn sẵn)
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce

[Files]
; Sao chép TẤT CẢ các file trong thư mục publish vào thư mục cài đặt ({app})
; LƯU Ý KHI SỬ DỤNG: Đường dẫn Source có thể thay đổi tùy thuộc vào tên phiên bản net9.0-windows
Source: "d:\Document\_Projects\qa-smartschool\QASmartClass\bin\x64\Release\net9.0-windows10.0.19041.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Tạo icon trong Start Menu
Name: "{group}\QA SmartClass"; Filename: "{app}\QASmartClass.exe"
; Tạo icon ngoài Desktop (nếu người dùng tick chọn ở bước cài đặt)
Name: "{autodesktop}\QA SmartClass"; Filename: "{app}\QASmartClass.exe"; Tasks: desktopicon

[Run]
; Tùy chọn chạy ứng dụng ngay sau khi cài đặt xong
Filename: "{app}\QASmartClass.exe"; Description: "{cm:LaunchProgram,QA SmartClass}"; Flags: nowait postinstall skipifsilent
