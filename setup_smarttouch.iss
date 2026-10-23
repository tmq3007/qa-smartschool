; =====================================================================
; Bộ cài QA Smart Touch (bản độc lập, 64-bit)
;
; BƯỚC 1 - Publish dạng self-contained (đã kèm .NET, máy đích KHÔNG cần cài .NET):
;   dotnet publish QASmartTouch/QASmartTouch.csproj -c Release -r win-x64 --self-contained true -p:Platform=x64
;
; BƯỚC 2 - Đóng gói bộ cài (chạy tại thư mục gốc repo):
;   "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" setup_smarttouch.iss
;   (tuỳ chọn: ISCC /DBuildDir=<thư mục publish khác> setup_smarttouch.iss)
;
; Lưu ý: file .iss này lưu UTF-8 có BOM để hiển thị đúng tiếng Việt.
; =====================================================================
#define MyAppName "QA Smart Touch"
#define MyAppExeName "QASmartTouch.exe"
#ifndef BuildDir
  #define BuildDir "QASmartTouch\bin\x64\Release\net9.0-windows10.0.19041.0\win-x64\publish"
#endif
#define MyAppVer GetFileVersion(BuildDir + "\" + MyAppExeName)

[Setup]
; Thông tin chung về phần mềm bảng viết QA Smart Touch
AppName={#MyAppName}
AppVersion={#MyAppVer}
AppPublisher=QA SmartSchool
AppPublisherURL=https://qasmartschool.com
DefaultDirName={autopf}\QA SmartSchool\QA Smart Touch
DefaultGroupName=QA SmartSchool
UninstallDisplayName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2
SolidCompression=yes
; Ứng dụng 64-bit: chỉ cài trên Windows x64 và cài vào Program Files (không phải Program Files (x86))
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Nơi lưu file bộ cài (Setup.exe) sau khi đóng gói
OutputDir=Installer
; Tên file cài đặt tự động lấy theo version (vd: QASmartTouch_Setup_v1.0.0.0.exe)
OutputBaseFilename=QASmartTouch_Setup_v{#MyAppVer}
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
; 1. Gắn đuôi file .qasc với định danh ProgID QA.SmartTouch.Lecture
Root: HKA; Subkey: "Software\Classes\.qasc"; ValueType: string; ValueName: ""; ValueData: "QA.SmartTouch.Lecture"; Flags: uninsdeletevalue
; 2. Đặt tên hiển thị cho loại tệp bài giảng
Root: HKA; Subkey: "Software\Classes\QA.SmartTouch.Lecture"; ValueType: string; ValueName: ""; ValueData: "Bài giảng QA Smart Touch"; Flags: uninsdeletekey
; 3. Đặt biểu tượng Icon cho tệp bài giảng
Root: HKA; Subkey: "Software\Classes\QA.SmartTouch.Lecture\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"
; 4. Cấu hình lệnh mở tệp khi người dùng click đúp
Root: HKA; Subkey: "Software\Classes\QA.SmartTouch.Lecture\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

[Run]
; Tùy chọn chạy ứng dụng ngay sau khi cài đặt xong
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// Kiểm tra Microsoft Edge WebView2 Runtime (cần cho các tính năng web: bản đồ, trình duyệt nhúng, ...)
// Runtime đăng ký phiên bản ở giá trị "pv" của khóa EdgeUpdate\Clients\{GUID}.
const
  WebView2Guid = '{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}';

function IsValidWebView2Version(const Ver: String): Boolean;
begin
  Result := (Ver <> '') and (Ver <> '0.0.0.0');
end;

function IsWebView2Installed: Boolean;
var
  Ver: String;
begin
  Result := False;
  // Cài theo máy (64-bit OS -> khóa WOW6432Node)
  if RegQueryStringValue(HKLM, 'SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\' + WebView2Guid, 'pv', Ver) and IsValidWebView2Version(Ver) then
    Result := True
  // Cài theo máy (khóa không qua WOW6432Node)
  else if RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\EdgeUpdate\Clients\' + WebView2Guid, 'pv', Ver) and IsValidWebView2Version(Ver) then
    Result := True
  // Cài theo người dùng
  else if RegQueryStringValue(HKCU, 'Software\Microsoft\EdgeUpdate\Clients\' + WebView2Guid, 'pv', Ver) and IsValidWebView2Version(Ver) then
    Result := True;
end;

function InitializeSetup: Boolean;
var
  ErrCode: Integer;
begin
  Result := True;
  if (not IsWebView2Installed) and (not WizardSilent) then
  begin
    if MsgBox('Máy tính chưa có "Microsoft Edge WebView2 Runtime" - thành phần cần cho một số tính năng web của QA Smart Touch.' + #13#10#13#10 +
              'Chọn "Yes" để mở trang tải WebView2 (cài xong hãy chạy lại bộ cài này).' + #13#10 +
              'Chọn "No" để tiếp tục cài đặt (các tính năng web có thể không hoạt động).',
              mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open', 'https://go.microsoft.com/fwlink/p/?LinkId=2124703', '', '', SW_SHOWNORMAL, ewNoWait, ErrCode);
      Result := False;
    end;
  end;
end;
