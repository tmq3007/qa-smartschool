using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QASmartClass.Classroom.Helpers;
using Serilog;

namespace QASmartClass.Classroom.Views
{
    public partial class PolicyPage : Page
    {
        private bool _isSilenced = false;
        private bool _isBindingPreset = false;
        private System.Windows.Threading.DispatcherTimer? _idleTimer;
        private int _idleTime = 0;
        private int _idleThresholdSeconds = 300;

        // 📡 Giám sát ACK và Trạng thái học sinh
        private System.Collections.ObjectModel.ObservableCollection<StudentStatusModel> _studentStatuses = new();
        private System.Windows.Threading.DispatcherTimer? _ackRetryTimer;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, CommandTracker> _activeTrackers = new();

        public PolicyPage()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                LoadRecentLogs();
                LoadCurrentPolicyState();
                LoadPresets();
                InitializeIdleTimer();

                // Liên kết danh sách trạng thái
                lstStudentStatus.ItemsSource = _studentStatuses;
                InitializeStudentStatusList();
                InitializeAckRetryTimer();

                // → ClassroomAppContext
                if (ClassroomAppContext.Network != null)
                {
                    ClassroomAppContext.Network.CommandAckReceived += NetworkService_CommandAckReceived;
                    ClassroomAppContext.Network.StudentConnected += NetworkService_StudentConnected;
                    ClassroomAppContext.Network.StudentDisconnected += NetworkService_StudentDisconnected;
                }
            };
            Unloaded += (_, _) =>
            {
                StopIdleTimer();
                StopAckRetryTimer();

                // → ClassroomAppContext
                if (ClassroomAppContext.Network != null)
                {
                    ClassroomAppContext.Network.CommandAckReceived -= NetworkService_CommandAckReceived;
                    ClassroomAppContext.Network.StudentConnected -= NetworkService_StudentConnected;
                    ClassroomAppContext.Network.StudentDisconnected -= NetworkService_StudentDisconnected;
                }

                foreach (var item in _studentStatuses)
                {
                    item.PropertyChanged -= StudentStatus_PropertyChanged;
                }
            };
        }

        private void LoadCurrentPolicyState()
        {
            try
            {
                var control = QASmartClass.Services.ClassControlService.Instance;
                // → ClassroomAppContext
                var db = ClassroomAppContext.Db;

                // Load Exit PIN config from Database using DPAPI
                var settingRequired = db.SystemSettings.FirstOrDefault(s => s.Id == "Security_IsExitPinRequired");
                var settingHash = db.SystemSettings.FirstOrDefault(s => s.Id == "Security_ExitPinHash");
                var settingEncrypted = db.SystemSettings.FirstOrDefault(s => s.Id == "Security_ExitPinEncrypted");

                if (settingRequired != null)
                {
                    control.IsExitPinRequired = settingRequired.Value == "True";
                }
                if (settingHash != null)
                {
                    control.ExitPinHash = settingHash.Value;
                }
                if (settingEncrypted != null && !string.IsNullOrEmpty(settingEncrypted.Value))
                {
                    // Giải mã bảo mật bằng DPAPI
                    control.ExitPinCode = QASmartClass.Utilities.CryptoHelper.DecryptWithDpapi(settingEncrypted.Value);
                }

                chkRequireExitPin.IsChecked = control.IsExitPinRequired;
                txtExitPin.Text = control.ExitPinCode;
                gridExitPin.Visibility = control.IsExitPinRequired ? Visibility.Visible : Visibility.Collapsed;

                // Khôi phục trạng thái các CheckBox chính sách khác từ CSDL SQLite
                chkBlockInternet.IsChecked = db.SystemSettings.FirstOrDefault(s => s.Id == "Policy_BlockInternet")?.Value == "True";
                chkEduOnly.IsChecked = db.SystemSettings.FirstOrDefault(s => s.Id == "Policy_EduOnly")?.Value == "True";
                chkBlockSocial.IsChecked = db.SystemSettings.FirstOrDefault(s => s.Id == "Policy_BlockSocial")?.Value == "True";
                chkBlockGames.IsChecked = db.SystemSettings.FirstOrDefault(s => s.Id == "Policy_BlockGames")?.Value == "True";
                chkLockDesktop.IsChecked = db.SystemSettings.FirstOrDefault(s => s.Id == "Policy_LockDesktop")?.Value == "True";
                chkShowTeacherScreen.IsChecked = db.SystemSettings.FirstOrDefault(s => s.Id == "Policy_ShowTeacherScreen")?.Value == "True";
                chkQuietMode.IsChecked = db.SystemSettings.FirstOrDefault(s => s.Id == "Policy_QuietMode")?.Value == "True";
                chkDisableTaskbar.IsChecked = db.SystemSettings.FirstOrDefault(s => s.Id == "Policy_DisableTaskbar")?.Value == "True";
                chkBlockUsb.IsChecked = db.SystemSettings.FirstOrDefault(s => s.Id == "Policy_BlockUsb")?.Value == "True";
                chkBlockApps.IsChecked = db.SystemSettings.FirstOrDefault(s => s.Id == "Policy_BlockApps")?.Value == "True";
                chkWhitelistOnly.IsChecked = db.SystemSettings.FirstOrDefault(s => s.Id == "Policy_WhitelistOnly")?.Value == "True";
                chkBlockPrint.IsChecked = db.SystemSettings.FirstOrDefault(s => s.Id == "Policy_BlockPrint")?.Value == "True";

                var whitelistSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Policy_AppWhitelist");
                if (whitelistSetting != null)
                {
                    control.AppWhitelist = whitelistSetting.Value;
                }

                var idleDurationSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Policy_IdleLockDuration");
                if (idleDurationSetting != null && int.TryParse(idleDurationSetting.Value, out int customDuration))
                {
                    _idleThresholdSeconds = customDuration;
                }
                else
                {
                    _idleThresholdSeconds = 300;
                }

                if (cboIdleDuration != null)
                {
                    foreach (ComboBoxItem item in cboIdleDuration.Items)
                    {
                        if (item.Tag?.ToString() == _idleThresholdSeconds.ToString())
                        {
                            cboIdleDuration.SelectedItem = item;
                            break;
                        }
                    }
                }

                // Đồng bộ biến nội bộ với trạng thái Silence (màn hình đen)
                _isSilenced = control.IsSilenceActive;
                if (_isSilenced)
                {
                    UpdateStatus("🖤 Màn hình đen đang BẬT — HS không thấy gì", "#37474F");
                }
                else
                {
                    UpdateStatus("⏳ Chưa áp dụng chính sách nào", "#558B2F");
                    
                    // Nếu có chính sách đang hoạt động khác thì hiển thị
                    int activeCount = new[] { 
                        chkBlockInternet.IsChecked == true, chkEduOnly.IsChecked == true, 
                        chkBlockSocial.IsChecked == true, chkBlockGames.IsChecked == true, 
                        chkLockDesktop.IsChecked == true, chkShowTeacherScreen.IsChecked == true, 
                        chkQuietMode.IsChecked == true, chkDisableTaskbar.IsChecked == true, 
                        chkBlockUsb.IsChecked == true, chkBlockApps.IsChecked == true, 
                        chkWhitelistOnly.IsChecked == true, chkBlockPrint.IsChecked == true, 
                        chkRequireExitPin.IsChecked == true 
                    }.Count(x => x);
                    
                    if (activeCount > 0)
                    {
                        UpdateStatus($"✅ Đã áp dụng {activeCount} chính sách", "#2E7D32");
                        badgePolicyActive.Visibility = Visibility.Visible;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Error loading current policy state: {Err}", ex.Message);
            }
        }

        private void chkRequireExitPin_Checked(object sender, RoutedEventArgs e)
        {
            if (_isBindingPreset) return;
            if (gridExitPin != null)
            {
                gridExitPin.Visibility = Visibility.Visible;
            }
        }

        private void chkRequireExitPin_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isBindingPreset) return;
            if (gridExitPin != null)
            {
                gridExitPin.Visibility = Visibility.Collapsed;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  🌐 CHẶN INTERNET / CHỈ TRANG GIÁO DỤC - LOẠI TRỪ LẪN NHAU
        // ═══════════════════════════════════════════════════════════

        private void chkBlockInternet_Checked(object sender, RoutedEventArgs e)
        {
            if (_isBindingPreset) return;
            if (chkEduOnly == null || chkBlockSocial == null || chkBlockGames == null) return;
            chkEduOnly.IsChecked = false;
            chkEduOnly.IsEnabled = false;
            chkBlockSocial.IsChecked = false;
            chkBlockSocial.IsEnabled = false;
            chkBlockGames.IsChecked = false;
            chkBlockGames.IsEnabled = false;
        }

        private void chkBlockInternet_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isBindingPreset) return;
            if (chkEduOnly == null || chkBlockSocial == null || chkBlockGames == null) return;
            chkEduOnly.IsEnabled = true;
            chkBlockSocial.IsEnabled = true;
            chkBlockGames.IsEnabled = true;
        }

        private void chkEduOnly_Checked(object sender, RoutedEventArgs e)
        {
            if (_isBindingPreset) return;
            if (chkBlockSocial == null || chkBlockGames == null) return;
            chkBlockSocial.IsChecked = false;
            chkBlockSocial.IsEnabled = false;
            chkBlockGames.IsChecked = false;
            chkBlockGames.IsEnabled = false;
        }

        private void chkEduOnly_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isBindingPreset) return;
            if (chkBlockSocial == null || chkBlockGames == null) return;
            if (chkBlockInternet?.IsChecked != true)
            {
                chkBlockSocial.IsEnabled = true;
                chkBlockGames.IsEnabled = true;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  💡 HƯỚNG DẪN SỬ DỤNG
        // ═══════════════════════════════════════════════════════════

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            gridHelpOverlay.Visibility = Visibility.Visible;
            SetBackgroundFocusEnabled(false);
        }

        private void CloseHelp_Click(object sender, RoutedEventArgs e)
        {
            gridHelpOverlay.Visibility = Visibility.Collapsed;
            SetBackgroundFocusEnabled(true);
        }

        // ═══════════════════════════════════════════════════════════
        //  ✅ ÁP DỤNG CHÍNH SÁCH
        // ═══════════════════════════════════════════════════════════

        private async void ApplyPolicy_Click(object sender, RoutedEventArgs e)
        {
            bool blockInternet = chkBlockInternet.IsChecked == true;
            bool eduOnly       = chkEduOnly.IsChecked == true;
            bool blockSocial   = chkBlockSocial.IsChecked == true;
            bool blockGames    = chkBlockGames.IsChecked == true;
            bool lockDesktop   = chkLockDesktop.IsChecked == true;
            bool showTeacher   = chkShowTeacherScreen.IsChecked == true;
            bool quietMode     = chkQuietMode.IsChecked == true;
            bool disableTask   = chkDisableTaskbar.IsChecked == true;
            bool blockUsb      = chkBlockUsb.IsChecked == true;
            bool blockApps     = chkBlockApps.IsChecked == true;
            bool whitelistOnly = chkWhitelistOnly.IsChecked == true;
            bool blockPrint    = chkBlockPrint.IsChecked == true;
            bool requireExitPin = chkRequireExitPin.IsChecked == true;
            string exitPinCode = txtExitPin.Text.Trim();

            if (requireExitPin)
            {
                if (string.IsNullOrEmpty(exitPinCode) || exitPinCode.Length < 4 || exitPinCode.Length > 6 || !exitPinCode.All(char.IsDigit))
                {
                    ClassroomDialog.Warn("Mã PIN thoát ứng dụng phải từ 4 đến 6 chữ số.", "Lỗi nhập liệu");
                    return;
                }
            }

            if (whitelistOnly)
            {
                var appWhitelistStr = QASmartClass.Services.ClassControlService.Instance.AppWhitelist;
                var list = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.List<string>>(appWhitelistStr) 
                           ?? new System.Collections.Generic.List<string>();
                if (list.Count == 0)
                {
                    ClassroomDialog.Warn("Danh sách Whitelist ứng dụng đang trống.\nVui lòng bấm 'Cấu hình' để thêm ít nhất một ứng dụng trước khi áp dụng.", "Danh sách trống");
                    return;
                }
            }

            // Count active policies
            int activeCount = new[] { blockInternet, eduOnly, blockSocial, blockGames, lockDesktop,
                showTeacher, quietMode, disableTask, blockUsb, blockApps, whitelistOnly, blockPrint, requireExitPin }
                .Count(x => x);

            if (activeCount == 0)
            {
                ClassroomDialog.Warn("Bạn chưa chọn chính sách nào để áp dụng.\nVui lòng tích chọn ít nhất 1 mục.", "Chưa chọn chính sách");
                return;
            }

            UpdateStatus("⏳ Đang áp dụng chính sách...", "#FF8F00");

            try
            {
                // → ClassroomAppContext
                var net = ClassroomAppContext.Network;

                var control = QASmartClass.Services.ClassControlService.Instance;
                control.IsExitPinRequired = requireExitPin;
                control.ExitPinCode = requireExitPin ? exitPinCode : string.Empty;

                string classCode = ClassroomAppContext.Session?.ClassCode ?? "DEFAULT_CLASS";
                if (string.IsNullOrEmpty(classCode)) classCode = "DEFAULT_CLASS";
                string sessionSalt = QASmartClass.Services.ClassControlService.Instance.SessionSalt;
                string salt = !string.IsNullOrEmpty(sessionSalt) ? sessionSalt : classCode;
                control.ExitPinHash = requireExitPin ? QASmartClass.Services.ClassControlService.ComputeSha256Hash(exitPinCode, salt) : string.Empty;

                // Sync ClassControlService memory state
                control.IsInternetBlocked = blockInternet;
                control.IsEduOnlyAllowed = eduOnly;
                control.IsSocialBlocked = blockSocial;
                control.IsGamesBlocked = blockGames;
                control.IsDesktopLocked = lockDesktop;
                control.IsTeacherScreenShown = showTeacher;
                control.IsTaskbarDisabled = disableTask;
                control.IsUsbBlocked = blockUsb;
                control.IsAppInstallBlocked = blockApps;
                control.IsAppWhitelistOnly = whitelistOnly;
                control.IsPrintBlocked = blockPrint;

                // Save to Database SQLite
                var db = ClassroomAppContext.Db;
                void SaveOrUpdateSetting(string id, string val, string cat = "Security")
                {
                    var setting = db.SystemSettings.FirstOrDefault(s => s.Id == id);
                    if (setting == null)
                    {
                        setting = new Data.SystemSetting { Id = id, Value = val, Category = cat, LastUpdated = DateTime.Now };
                        db.SystemSettings.Add(setting);
                    }
                    else
                    {
                        setting.Value = val;
                        setting.LastUpdated = DateTime.Now;
                    }
                }

                SaveOrUpdateSetting("Security_IsExitPinRequired", requireExitPin ? "True" : "False");
                SaveOrUpdateSetting("Security_ExitPinHash", control.ExitPinHash);
                
                // Sử dụng mã hóa DPAPI an toàn bảo mật, loại bỏ hardcoded key
                SaveOrUpdateSetting("Security_ExitPinEncrypted", requireExitPin ? QASmartClass.Utilities.CryptoHelper.EncryptWithDpapi(exitPinCode) : string.Empty);

                // Lưu trạng thái của tất cả các CheckBox chính sách để đồng bộ khi tải lại trang
                SaveOrUpdateSetting("Policy_BlockInternet", blockInternet ? "True" : "False", "Policy");
                SaveOrUpdateSetting("Policy_EduOnly", eduOnly ? "True" : "False", "Policy");
                SaveOrUpdateSetting("Policy_BlockSocial", blockSocial ? "True" : "False", "Policy");
                SaveOrUpdateSetting("Policy_BlockGames", blockGames ? "True" : "False", "Policy");
                SaveOrUpdateSetting("Policy_LockDesktop", lockDesktop ? "True" : "False", "Policy");
                SaveOrUpdateSetting("Policy_ShowTeacherScreen", showTeacher ? "True" : "False", "Policy");
                SaveOrUpdateSetting("Policy_QuietMode", quietMode ? "True" : "False", "Policy");
                SaveOrUpdateSetting("Policy_DisableTaskbar", disableTask ? "True" : "False", "Policy");
                SaveOrUpdateSetting("Policy_BlockUsb", blockUsb ? "True" : "False", "Policy");
                SaveOrUpdateSetting("Policy_BlockApps", blockApps ? "True" : "False", "Policy");
                SaveOrUpdateSetting("Policy_WhitelistOnly", whitelistOnly ? "True" : "False", "Policy");
                SaveOrUpdateSetting("Policy_BlockPrint", blockPrint ? "True" : "False", "Policy");

                if (net != null && net.IsBroadcasting)
                {
                    var policy = $"POLICY|internet={blockInternet}|eduonly={eduOnly}|social={blockSocial}|games={blockGames}" +
                                 $"|desktop={lockDesktop}|teacher={showTeacher}|quiet={quietMode}|taskbar={disableTask}" +
                                 $"|usb={blockUsb}|apps={blockApps}|whitelist={whitelistOnly}|print={blockPrint}";
                    
                    // Gán ID cho lệnh
                    policy = QASmartClass.Classroom.Services.NetworkDiscoveryService.AppendCommandId(policy);
                    string cmdId = ExtractIdFromCommand(policy);

                    var students = net.GetConnectedStudents();
                    foreach (var student in students)
                    {
                        var tracker = new CommandTracker
                        {
                            CommandId = cmdId,
                            Payload = policy,
                            StudentCode = student.Code,
                            PCName = student.PCName,
                            LastSentTime = DateTime.Now,
                            RetryCount = 0
                        };
                        string key = $"{cmdId}_{student.Code}";
                        _activeTrackers[key] = tracker;

                        UpdateStudentStatusUI(student.Code, student.PCName, "PENDING", "Đang chờ phản hồi...");
                    }

                    await net.SendCommandAsync(policy);

                    // Gán ID cho lệnh Exit PIN
                    var pinCmd = $"CMD|EXIT_PIN_CONFIG|{requireExitPin}|{control.ExitPinHash}";
                    pinCmd = QASmartClass.Classroom.Services.NetworkDiscoveryService.AppendCommandId(pinCmd);
                    string pinCmdId = ExtractIdFromCommand(pinCmd);

                    foreach (var student in students)
                    {
                        var tracker = new CommandTracker
                        {
                            CommandId = pinCmdId,
                            Payload = pinCmd,
                            StudentCode = student.Code,
                            PCName = student.PCName,
                            LastSentTime = DateTime.Now,
                            RetryCount = 0
                        };
                        string key = $"{pinCmdId}_{student.Code}";
                        _activeTrackers[key] = tracker;
                    }

                    await net.SendCommandAsync(pinCmd);
                }

                // Log event
                string appWhitelistDetails = whitelistOnly ? $" WhitelistApps:{control.AppWhitelist}" : string.Empty;
                ClassroomAppContext.Db.EventLogs.Add(new Data.EventLog
                {
                    EventType = "POLICY",
                    Actor = "GV",
                    Details = $"Áp dụng {activeCount} chính sách: Internet:{blockInternet} Desktop:{lockDesktop} USB:{blockUsb} Apps:{blockApps} Quiet:{quietMode} ExitPIN:{requireExitPin}{appWhitelistDetails}",
                    Timestamp = DateTime.Now
                });
                ClassroomAppContext.Db.SaveChanges();

                UpdateStatus($"✅ Đã áp dụng {activeCount} chính sách", "#2E7D32");
                txtPolicyTime.Text = $"Lúc {DateTime.Now:HH:mm:ss dd/MM/yyyy}";
                badgePolicyActive.Visibility = Visibility.Visible;

                Log.Information("Policy applied: {Count} rules, Internet={I} Desktop={D} USB={U} ExitPIN={E}", activeCount, blockInternet, lockDesktop, blockUsb, requireExitPin);
                LoadRecentLogs();

                ClassroomDialog.Info($"✅ Đã áp dụng {activeCount} chính sách cho tất cả máy học sinh đang kết nối!", "Áp dụng thành công");
            }
            catch (Exception ex)
            {
                UpdateStatus($"❌ Lỗi: {ex.Message}", "#C62828");
                Log.Warning("Policy apply error: {Err}", ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  🔓 ĐẶT LẠI MẶC ĐỊNH
        // ═══════════════════════════════════════════════════════════

        private async void ResetPolicy_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show("Bạn có chắc muốn đặt lại tất cả chính sách về mặc định?\nTất cả quy tắc sẽ được gỡ bỏ khỏi máy học sinh.",
                "Đặt lại chính sách", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            chkBlockInternet.IsChecked = false;
            chkEduOnly.IsChecked = false;
            chkBlockSocial.IsChecked = false;
            chkBlockGames.IsChecked = false;
            chkLockDesktop.IsChecked = false;
            chkShowTeacherScreen.IsChecked = false;
            chkQuietMode.IsChecked = false;
            chkDisableTaskbar.IsChecked = false;
            chkBlockUsb.IsChecked = false;
            chkBlockApps.IsChecked = false;
            chkWhitelistOnly.IsChecked = false;
            chkBlockPrint.IsChecked = false;
            chkRequireExitPin.IsChecked = false;
            txtExitPin.Text = string.Empty;

            try
            {
                // → ClassroomAppContext
                var net = ClassroomAppContext.Network;

                var control = QASmartClass.Services.ClassControlService.Instance;
                control.Reset();

                // Save to Database SQLite
                var db = ClassroomAppContext.Db;
                void SaveOrUpdateSetting(string id, string val, string cat = "Security")
                {
                    var setting = db.SystemSettings.FirstOrDefault(s => s.Id == id);
                    if (setting == null)
                    {
                        setting = new Data.SystemSetting { Id = id, Value = val, Category = cat, LastUpdated = DateTime.Now };
                        db.SystemSettings.Add(setting);
                    }
                    else
                    {
                        setting.Value = val;
                        setting.LastUpdated = DateTime.Now;
                    }
                }
                SaveOrUpdateSetting("Security_IsExitPinRequired", "False");
                SaveOrUpdateSetting("Security_ExitPinHash", string.Empty);
                SaveOrUpdateSetting("Security_ExitPinEncrypted", string.Empty);

                // Gỡ bỏ trạng thái các chính sách khác trong SQLite
                SaveOrUpdateSetting("Policy_BlockInternet", "False", "Policy");
                SaveOrUpdateSetting("Policy_EduOnly", "False", "Policy");
                SaveOrUpdateSetting("Policy_BlockSocial", "False", "Policy");
                SaveOrUpdateSetting("Policy_BlockGames", "False", "Policy");
                SaveOrUpdateSetting("Policy_LockDesktop", "False", "Policy");
                SaveOrUpdateSetting("Policy_ShowTeacherScreen", "False", "Policy");
                SaveOrUpdateSetting("Policy_QuietMode", "False", "Policy");
                SaveOrUpdateSetting("Policy_DisableTaskbar", "False", "Policy");
                SaveOrUpdateSetting("Policy_BlockUsb", "False", "Policy");
                SaveOrUpdateSetting("Policy_BlockApps", "False", "Policy");
                SaveOrUpdateSetting("Policy_WhitelistOnly", "False", "Policy");
                SaveOrUpdateSetting("Policy_BlockPrint", "False", "Policy");

                 if (net != null && net.IsBroadcasting)
                {
                    var resetCmd = "POLICY|reset=true";
                    resetCmd = QASmartClass.Classroom.Services.NetworkDiscoveryService.AppendCommandId(resetCmd);
                    string cmdId = ExtractIdFromCommand(resetCmd);

                    var students = net.GetConnectedStudents();
                    foreach (var student in students)
                    {
                        var tracker = new CommandTracker
                        {
                            CommandId = cmdId,
                            Payload = resetCmd,
                            StudentCode = student.Code,
                            PCName = student.PCName,
                            LastSentTime = DateTime.Now,
                            RetryCount = 0
                        };
                        string key = $"{cmdId}_{student.Code}";
                        _activeTrackers[key] = tracker;

                        UpdateStudentStatusUI(student.Code, student.PCName, "PENDING", "Đang hủy chính sách...");
                    }
                    await net.SendCommandAsync(resetCmd);

                    var pinCmd = "CMD|EXIT_PIN_CONFIG|false|";
                    pinCmd = QASmartClass.Classroom.Services.NetworkDiscoveryService.AppendCommandId(pinCmd);
                    string pinCmdId = ExtractIdFromCommand(pinCmd);

                    foreach (var student in students)
                    {
                        var tracker = new CommandTracker
                        {
                            CommandId = pinCmdId,
                            Payload = pinCmd,
                            StudentCode = student.Code,
                            PCName = student.PCName,
                            LastSentTime = DateTime.Now,
                            RetryCount = 0
                        };
                        string key = $"{pinCmdId}_{student.Code}";
                        _activeTrackers[key] = tracker;
                    }
                    await net.SendCommandAsync(pinCmd);
                }

                ClassroomAppContext.Db.EventLogs.Add(new Data.EventLog
                {
                    EventType = "POLICY_RESET", Actor = "GV",
                    Details = "Đặt lại tất cả chính sách về mặc định",
                    Timestamp = DateTime.Now
                });
                ClassroomAppContext.Db.SaveChanges();

                UpdateStatus("🔓 Tất cả chính sách đã được đặt lại", "#558B2F");
                txtPolicyTime.Text = $"Lúc {DateTime.Now:HH:mm:ss dd/MM/yyyy}";
                badgePolicyActive.Visibility = Visibility.Collapsed;

                Log.Information("Policy reset to defaults");
                LoadRecentLogs();
            }
            catch (Exception ex)
            {
                Log.Warning("Policy reset error: {Err}", ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  ⚡ QUICK ACTIONS
        // ═══════════════════════════════════════════════════════════

        private async void LockAll_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show("Bạn có chắc chắn muốn khóa màn hình tất cả học sinh?\nHọc sinh sẽ không thể thao tác trên máy tính cho đến khi bạn mở khóa.",
                "Khóa màn hình tất cả", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                // → ClassroomAppContext
                var net = ClassroomAppContext.Network;
                if (net != null) await net.LockAllScreensAsync();

                UpdateStatus("🔒 Đã khóa tất cả màn hình", "#C62828");
                txtPolicyTime.Text = $"Lúc {DateTime.Now:HH:mm:ss}";

                ClassroomAppContext.Db.EventLogs.Add(new Data.EventLog { EventType = "LOCK_SCREEN", Actor = "GV", Details = "Khóa tất cả màn hình HS", Timestamp = DateTime.Now });
                ClassroomAppContext.Db.SaveChanges();
                LoadRecentLogs();
            }
            catch (Exception ex) { Log.Warning("Lock error: {Err}", ex.Message); }
        }

        private async void UnlockAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // → ClassroomAppContext
                var net = ClassroomAppContext.Network;
                if (net != null) await net.UnlockAllScreensAsync();

                UpdateStatus("🔓 Đã mở khóa tất cả màn hình", "#2E7D32");
                txtPolicyTime.Text = $"Lúc {DateTime.Now:HH:mm:ss}";

                ClassroomAppContext.Db.EventLogs.Add(new Data.EventLog { EventType = "UNLOCK_SCREEN", Actor = "GV", Details = "Mở khóa tất cả màn hình HS", Timestamp = DateTime.Now });
                ClassroomAppContext.Db.SaveChanges();
                LoadRecentLogs();
            }
            catch (Exception ex) { Log.Warning("Unlock error: {Err}", ex.Message); }
        }

        private async void LockKeyboard_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show("Bạn có chắc chắn muốn khóa bàn phím và chuột của học sinh?\nHọc sinh chỉ có thể quan sát màn hình.",
                "Khóa bàn phím & chuột", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                // → ClassroomAppContext
                var net = ClassroomAppContext.Network;
                if (net != null && net.IsBroadcasting)
                    await net.SendCommandAsync("LOCK_KEYBOARD|enabled=true");

                UpdateStatus("⌨️ Đã khóa bàn phím + chuột tất cả HS", "#E65100");
                txtPolicyTime.Text = $"Lúc {DateTime.Now:HH:mm:ss}";

                ClassroomAppContext.Db.EventLogs.Add(new Data.EventLog { EventType = "LOCK_KEYBOARD", Actor = "GV", Details = "Khóa bàn phím + chuột tất cả HS", Timestamp = DateTime.Now });
                ClassroomAppContext.Db.SaveChanges();
                Log.Information("Keyboard locked for all students");
                LoadRecentLogs();
            }
            catch (Exception ex) { Log.Warning("Lock keyboard error: {Err}", ex.Message); }
        }

        private async void UnlockKeyboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // → ClassroomAppContext
                var net = ClassroomAppContext.Network;
                if (net != null && net.IsBroadcasting)
                    await net.SendCommandAsync("LOCK_KEYBOARD|enabled=false");

                UpdateStatus("⌨️ Đã mở khóa bàn phím + chuột", "#1565C0");
                txtPolicyTime.Text = $"Lúc {DateTime.Now:HH:mm:ss}";

                ClassroomAppContext.Db.EventLogs.Add(new Data.EventLog { EventType = "UNLOCK_KEYBOARD", Actor = "GV", Details = "Mở khóa bàn phím + chuột tất cả HS", Timestamp = DateTime.Now });
                ClassroomAppContext.Db.SaveChanges();
                Log.Information("Keyboard unlocked for all students");
                LoadRecentLogs();
            }
            catch (Exception ex) { Log.Warning("Unlock keyboard error: {Err}", ex.Message); }
        }

        private async void QuietMode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                chkQuietMode.IsChecked = !(chkQuietMode.IsChecked == true);
                bool quiet = chkQuietMode.IsChecked == true;

                // → ClassroomAppContext
                var net = ClassroomAppContext.Network;
                if (net != null && net.IsBroadcasting)
                    await net.SendCommandAsync($"QUIET_MODE|enabled={quiet}");

                // Đồng bộ và lưu trạng thái QuietMode vào SQLite để khôi phục sau này
                var db = ClassroomAppContext.Db;
                var setting = db.SystemSettings.FirstOrDefault(s => s.Id == "Policy_QuietMode");
                if (setting == null)
                {
                    db.SystemSettings.Add(new Data.SystemSetting { Id = "Policy_QuietMode", Value = quiet ? "True" : "False", Category = "Policy", LastUpdated = DateTime.Now });
                }
                else
                {
                    setting.Value = quiet ? "True" : "False";
                    setting.LastUpdated = DateTime.Now;
                }

                UpdateStatus(quiet ? "🔇 Chế độ im lặng đã BẬT" : "🔊 Chế độ im lặng đã TẮT",
                    quiet ? "#7B1FA2" : "#2E7D32");
                txtPolicyTime.Text = $"Lúc {DateTime.Now:HH:mm:ss}";

                ClassroomAppContext.Db.EventLogs.Add(new Data.EventLog
                {
                    EventType = quiet ? "QUIET_ON" : "QUIET_OFF", Actor = "GV",
                    Details = $"Chế độ im lặng: {(quiet ? "BẬT" : "TẮT")}",
                    Timestamp = DateTime.Now
                });
                ClassroomAppContext.Db.SaveChanges();
                Log.Information("Quiet mode: {Mode}", quiet);
                LoadRecentLogs();
            }
            catch (Exception ex) { Log.Warning("Quiet mode error: {Err}", ex.Message); }
        }

        private async void SilenceAll_Click(object sender, RoutedEventArgs e)
        {
            var actionText = _isSilenced ? "TẮT" : "BẬT";
            var confirm = MessageBox.Show($"Bạn có chắc chắn muốn {actionText} chế độ màn hình đen trên tất cả máy học sinh?",
                "Bật/Tắt màn hình đen", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                // → ClassroomAppContext
                var net = ClassroomAppContext.Network;

                _isSilenced = !_isSilenced;

                if (net != null && net.IsBroadcasting)
                {
                    await net.SendCommandAsync($"SILENCE|enabled={_isSilenced.ToString().ToLower()}");
                }

                if (!_isSilenced)
                {
                    UpdateStatus("🖥️ Màn hình đen đã TẮT — HS thao tác bình thường", "#2E7D32");
                }
                else
                {
                    UpdateStatus("🖤 Màn hình đen đang BẬT — HS không thấy gì", "#37474F");
                }
                txtPolicyTime.Text = $"Lúc {DateTime.Now:HH:mm:ss}";

                ClassroomAppContext.Db.EventLogs.Add(new Data.EventLog
                {
                    EventType = _isSilenced ? "SILENCE_ON" : "SILENCE_OFF", Actor = "GV",
                    Details = $"Màn hình đen: {(_isSilenced ? "BẬT" : "TẮT")}",
                    Timestamp = DateTime.Now
                });
                ClassroomAppContext.Db.SaveChanges();
                Log.Information("Silence mode: {Mode}", _isSilenced);
                LoadRecentLogs();
            }
            catch (Exception ex) { Log.Warning("Silence error: {Err}", ex.Message); }
        }

        // ═══════════════════════════════════════════════════════════
        //  📋 RECENT LOGS — Nhật ký gần đây
        // ═══════════════════════════════════════════════════════════

        private void LoadRecentLogs()
        {
            try
            {
                // → ClassroomAppContext
                var logs = ClassroomAppContext.Db.EventLogs
                    .Where(l => l.EventType.StartsWith("POLICY") || l.EventType.StartsWith("LOCK") ||
                                l.EventType.StartsWith("UNLOCK") || l.EventType.StartsWith("QUIET") ||
                                l.EventType.StartsWith("SILENCE"))
                    .OrderByDescending(l => l.Timestamp)
                    .Take(8)
                    .ToList();

                recentLogPanel.Children.Clear();

                if (logs.Count == 0)
                {
                    recentLogPanel.Children.Add(new TextBlock
                    {
                        Text = "Chưa có thao tác nào trong phiên này",
                        FontSize = 12, Foreground = Brushes.Gray, FontStyle = FontStyles.Italic
                    });
                    return;
                }

                foreach (var log in logs)
                {
                    string icon = log.EventType switch
                    {
                        "POLICY" => "📋",
                        "POLICY_RESET" => "🔓",
                        "LOCK_SCREEN" => "🔒",
                        "UNLOCK_SCREEN" => "🔓",
                        "LOCK_KEYBOARD" => "⌨️",
                        "UNLOCK_KEYBOARD" => "⌨️",
                        "QUIET_ON" => "🔇",
                        "QUIET_OFF" => "🔊",
                        "SILENCE_ON" => "🖤",
                        "SILENCE_OFF" => "🖥️",
                        _ => "📝"
                    };

                    var row = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(250, 250, 250)),
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(8, 5, 8, 5),
                        Margin = new Thickness(0, 0, 0, 3)
                    };

                    var dp = new DockPanel();
                    dp.Children.Add(new TextBlock
                    {
                        Text = log.Timestamp.ToString("HH:mm"),
                        FontSize = 10, Foreground = Brushes.Gray,
                        VerticalAlignment = VerticalAlignment.Center,
                        Width = 38
                    });
                    DockPanel.SetDock(dp.Children[0], Dock.Left);

                    dp.Children.Add(new TextBlock
                    {
                        Text = $"{icon} {log.Details}",
                        FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66)),
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        VerticalAlignment = VerticalAlignment.Center
                    });

                    row.Child = dp;
                    recentLogPanel.Children.Add(row);
                }

                // Update online count
                try
                {
                    var net = ClassroomAppContext.Network;
                    if (net != null)
                    {
                        int count = net.GetConnectedStudents()?.Count ?? 0;
                        txtOnlineCount.Text = $"{count} Học sinh online";
                        badgeOnline.Background = count > 0
                            ? new SolidColorBrush(Color.FromRgb(76, 175, 80))
                            : new SolidColorBrush(Color.FromRgb(158, 158, 158));
                    }
                }
                catch { }
            }
            catch (Exception ex)
            {
                Log.Warning("LoadRecentLogs error: {Err}", ex.Message);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  Helper
        // ═══════════════════════════════════════════════════════════

        private void UpdateStatus(string text, string colorHex)
        {
            txtPolicyStatus.Text = text;
            txtPolicyStatus.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom(colorHex)!;
        }

        private void txtExitPin_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private void txtExitPin_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (string.IsNullOrEmpty(text) || !text.All(char.IsDigit) || text.Length > 6)
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        // 🎲 Sinh mã PIN ngẫu nhiên an toàn
        private void btnRandomPin_Click(object sender, RoutedEventArgs e)
        {
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                byte[] bytes = new byte[4];
                rng.GetBytes(bytes);
                uint val = BitConverter.ToUInt32(bytes, 0) % 900000 + 100000; // Bảo đảm sinh ra số có 6 chữ số (100000 - 999999)
                txtExitPin.Text = val.ToString();
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  ⚡ GIAI ĐOẠN 5: TÊN TIẾN TRÌNH THÂN THIỆN TIẾNG VIỆT
        // ═══════════════════════════════════════════════════════════
        private static readonly System.Collections.Generic.Dictionary<string, string> ProcessVietnameseNames = new()
        {
            { "scratch.exe", "Lập trình Scratch" },
            { "powerpoint.exe", "Microsoft PowerPoint" },
            { "excel.exe", "Microsoft Excel" },
            { "winword.exe", "Microsoft Word" },
            { "chrome.exe", "Trình duyệt Google Chrome" }
        };

        private static string GetFriendlyName(string processName)
        {
            string cleanName = processName.Trim().ToLower();
            if (ProcessVietnameseNames.TryGetValue(cleanName, out string? vietnameseName))
            {
                return $"{vietnameseName} ({cleanName})";
            }
            return processName;
        }

        private static string GetProcessNameFromFriendly(string friendlyName)
        {
            if (friendlyName.Contains("(") && friendlyName.EndsWith(")"))
            {
                int start = friendlyName.LastIndexOf("(") + 1;
                int end = friendlyName.LastIndexOf(")");
                if (start < end)
                {
                    return friendlyName.Substring(start, end - start).Trim().ToLower();
                }
            }
            return friendlyName.Trim().ToLower();
        }

        // ⚙️ Popup Whitelist ứng dụng
        private void btnConfigAppWhitelist_Click(object sender, RoutedEventArgs e)
        {
            LoadWhitelistToListBox();
            gridWhitelistPopup.Visibility = Visibility.Visible;
            SetBackgroundFocusEnabled(false);
        }

        private void CloseWhitelist_Click(object sender, RoutedEventArgs e)
        {
            gridWhitelistPopup.Visibility = Visibility.Collapsed;
            SetBackgroundFocusEnabled(true);
        }

        private void LoadWhitelistToListBox()
        {
            lstWhitelistApps.Items.Clear();
            var control = QASmartClass.Services.ClassControlService.Instance;
            try
            {
                var list = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.List<string>>(control.AppWhitelist) 
                           ?? new System.Collections.Generic.List<string>();
                foreach (var app in list)
                {
                    lstWhitelistApps.Items.Add(GetFriendlyName(app));
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Error deserializing app whitelist: {Err}", ex.Message);
            }
        }

        private void btnAddApp_Click(object sender, RoutedEventArgs e)
        {
            string newApp = txtNewApp.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(newApp)) return;
            if (!newApp.EndsWith(".exe")) newApp += ".exe";
            
            string friendlyName = GetFriendlyName(newApp);
            bool exists = false;
            foreach (var item in lstWhitelistApps.Items)
            {
                if (GetProcessNameFromFriendly(item?.ToString() ?? string.Empty) == newApp)
                {
                    exists = true;
                    break;
                }
            }
            if (exists)
            {
                ClassroomDialog.Warn("Ứng dụng này đã tồn tại trong Whitelist.", "Trùng lặp");
                return;
            }
            lstWhitelistApps.Items.Add(friendlyName);
            txtNewApp.Clear();
        }

        private void btnDeleteApp_Click(object sender, RoutedEventArgs e)
        {
            if (lstWhitelistApps.SelectedItem != null)
            {
                lstWhitelistApps.Items.Remove(lstWhitelistApps.SelectedItem);
            }
            else
            {
                ClassroomDialog.Info("Vui lòng chọn một ứng dụng từ danh sách để xóa.", "Thông báo");
            }
        }

        private void SaveWhitelist_Click(object sender, RoutedEventArgs e)
        {
            var list = lstWhitelistApps.Items.Cast<object>()
                        .Select(item => GetProcessNameFromFriendly(item?.ToString() ?? string.Empty))
                        .ToList();
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(list);
            
            var control = QASmartClass.Services.ClassControlService.Instance;
            control.AppWhitelist = json;

            // Lưu vào SQLite
            try
            {
                // → ClassroomAppContext
                var db = ClassroomAppContext.Db;
                var setting = db.SystemSettings.FirstOrDefault(s => s.Id == "Policy_AppWhitelist");
                if (setting == null)
                {
                    db.SystemSettings.Add(new Data.SystemSetting 
                    { 
                        Id = "Policy_AppWhitelist", 
                        Value = json, 
                        Category = "Policy", 
                        LastUpdated = DateTime.Now 
                    });
                }
                else
                {
                    setting.Value = json;
                    setting.LastUpdated = DateTime.Now;
                }
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                Log.Warning("Error saving whitelist to database: {Err}", ex.Message);
            }

            gridWhitelistPopup.Visibility = Visibility.Collapsed;
            SetBackgroundFocusEnabled(true);
            ClassroomDialog.Info("Đã lưu danh sách Whitelist ứng dụng!", "Thành công");
        }

        // Khóa phím Tab nền khi mở Popup
        private void SetBackgroundFocusEnabled(bool enabled)
        {
            if (gridMainContent == null) return;
            gridMainContent.IsEnabled = enabled;
            if (btnHelp != null)
            {
                btnHelp.IsEnabled = enabled;
            }
        }

        private void txtNewApp_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                btnAddApp_Click(sender, e);
                e.Handled = true;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  ⚡ GIAI ĐOẠN 4: PRESETS, WHITELIST GỢI Ý & IDLE LOCK
        // ═══════════════════════════════════════════════════════════

        private void LoadPresets()
        {
            try
            {
                // → ClassroomAppContext
                var db = ClassroomAppContext.Db;

                // Giữ lại 3 mẫu mặc định ban đầu
                while (cboPresets.Items.Count > 3)
                {
                    cboPresets.Items.RemoveAt(cboPresets.Items.Count - 1);
                }

                var templates = db.SystemSettings.Where(s => s.Id.StartsWith("Policy_Template_")).ToList();
                foreach (var t in templates)
                {
                    string name = t.Id.Substring("Policy_Template_".Length);
                    cboPresets.Items.Add(new ComboBoxItem { Content = name });
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Error loading presets: {Err}", ex.Message);
            }
        }

        private void cboPresets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboPresets == null || cboPresets.SelectedItem == null) return;
            
            var selectedItem = (ComboBoxItem)cboPresets.SelectedItem;
            string presetName = selectedItem.Content.ToString() ?? string.Empty;

            if (string.IsNullOrEmpty(presetName)) return;

            _isBindingPreset = true;
            try
            {
                if (presetName == "Mặc định (Mở khóa)")
                {
                    SetPresetCheckboxes(false, false, false, false, false, false, false, false, false, false, false, false, false, string.Empty);
                    lstWhitelistApps.Items.Clear();
                }
                else if (presetName == "Thi cử nghiêm ngặt")
                {
                    SetPresetCheckboxes(true, false, true, true, true, false, true, true, true, true, false, true, true, "123456");
                    lstWhitelistApps.Items.Clear();
                }
                else if (presetName == "Thực hành Tin học")
                {
                    SetPresetCheckboxes(true, false, true, true, false, false, false, false, true, true, true, false, true, "888888");
                    lstWhitelistApps.Items.Clear();
                    lstWhitelistApps.Items.Add(GetFriendlyName("scratch.exe"));
                    lstWhitelistApps.Items.Add(GetFriendlyName("winword.exe"));
                    lstWhitelistApps.Items.Add(GetFriendlyName("excel.exe"));
                    lstWhitelistApps.Items.Add(GetFriendlyName("powerpoint.exe"));
                }
                else
                {
                    LoadCustomPresetFromDb(presetName);
                }
            }
            finally
            {
                _isBindingPreset = false;
            }
        }

        private void SetPresetCheckboxes(bool blockInt, bool eduOnly, bool blockSoc, bool blockGam, bool lockDesk, bool showTeach, bool quiet, bool disableTask, bool blockUsb, bool blockApps, bool whitelistOnly, bool blockPrint, bool reqPin, string pin)
        {
            chkBlockInternet.IsChecked = blockInt;
            chkEduOnly.IsChecked = eduOnly;
            chkBlockSocial.IsChecked = blockSoc;
            chkBlockGames.IsChecked = blockGam;
            chkLockDesktop.IsChecked = lockDesk;
            chkShowTeacherScreen.IsChecked = showTeach;
            chkQuietMode.IsChecked = quiet;
            chkDisableTaskbar.IsChecked = disableTask;
            chkBlockUsb.IsChecked = blockUsb;
            chkBlockApps.IsChecked = blockApps;
            chkWhitelistOnly.IsChecked = whitelistOnly;
            chkBlockPrint.IsChecked = blockPrint;
            
            // Xử lý loại trừ và Enabled thủ công tương thích
            if (blockInt)
            {
                chkEduOnly.IsEnabled = false;
                chkBlockSocial.IsEnabled = false;
                chkBlockGames.IsEnabled = false;
            }
            else if (eduOnly)
            {
                chkEduOnly.IsEnabled = true;
                chkBlockSocial.IsEnabled = false;
                chkBlockGames.IsEnabled = false;
            }
            else
            {
                chkEduOnly.IsEnabled = true;
                chkBlockSocial.IsEnabled = true;
                chkBlockGames.IsEnabled = true;
            }

            chkRequireExitPin.IsChecked = reqPin;
            txtExitPin.Text = pin;
            gridExitPin.Visibility = reqPin ? Visibility.Visible : Visibility.Collapsed;
        }

        private void btnSavePreset_Click(object sender, RoutedEventArgs e)
        {
            string name = CustomInputBox.Show("Nhập tên mẫu thiết lập bảo mật mới:", "Lưu mẫu thiết lập");
            if (string.IsNullOrEmpty(name)) return;

            if (name == "Mặc định (Mở khóa)" || name == "Thi cử nghiêm ngặt" || name == "Thực hành Tin học")
            {
                ClassroomDialog.Warn("Không thể ghi đè các mẫu mặc định của hệ thống.", "Lỗi");
                return;
            }

            try
            {
                // → ClassroomAppContext
                var db = ClassroomAppContext.Db;

                var data = new PresetData
                {
                    BlockInternet = chkBlockInternet.IsChecked == true,
                    EduOnly = chkEduOnly.IsChecked == true,
                    BlockSocial = chkBlockSocial.IsChecked == true,
                    BlockGames = chkBlockGames.IsChecked == true,
                    LockDesktop = chkLockDesktop.IsChecked == true,
                    ShowTeacherScreen = chkShowTeacherScreen.IsChecked == true,
                    QuietMode = chkQuietMode.IsChecked == true,
                    DisableTaskbar = chkDisableTaskbar.IsChecked == true,
                    BlockUsb = chkBlockUsb.IsChecked == true,
                    BlockApps = chkBlockApps.IsChecked == true,
                    WhitelistOnly = chkWhitelistOnly.IsChecked == true,
                    BlockPrint = chkBlockPrint.IsChecked == true,
                    RequireExitPin = chkRequireExitPin.IsChecked == true,
                    ExitPin = txtExitPin.Text.Trim(),
                    AppWhitelist = lstWhitelistApps.Items.Cast<object>()
                                    .Select(item => GetProcessNameFromFriendly(item?.ToString() ?? string.Empty))
                                    .ToList()
                };

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                string key = "Policy_Template_" + name;

                var setting = db.SystemSettings.FirstOrDefault(s => s.Id == key);
                if (setting == null)
                {
                    setting = new Data.SystemSetting { Id = key, Value = json, Category = "PolicyTemplate", LastUpdated = DateTime.Now };
                    db.SystemSettings.Add(setting);
                }
                else
                {
                    setting.Value = json;
                    setting.LastUpdated = DateTime.Now;
                }
                db.SaveChanges();

                LoadPresets();
                ClassroomDialog.Info($"Đã lưu mẫu thiết lập '{name}' thành công!", "Lưu mẫu");
            }
            catch (Exception ex)
            {
                ClassroomDialog.Error($"Lỗi khi lưu mẫu: {ex.Message}", "Lỗi hệ thống");
            }
        }


        private void LoadCustomPresetFromDb(string presetName)
        {
            try
            {
                // → ClassroomAppContext
                var db = ClassroomAppContext.Db;
                string key = "Policy_Template_" + presetName;

                var setting = db.SystemSettings.FirstOrDefault(s => s.Id == key);
                if (setting != null)
                {
                    PresetData? data = null;
                    try
                    {
                        data = Newtonsoft.Json.JsonConvert.DeserializeObject<PresetData>(setting.Value);
                    }
                    catch (Exception jsonEx)
                    {
                        Log.Warning("Preset '{Name}' JSON is corrupted: {Err}. Resetting to safe state.", presetName, jsonEx.Message);
                    }

                    if (data != null)
                    {
                        SetPresetCheckboxes(
                            data.BlockInternet, data.EduOnly, data.BlockSocial, data.BlockGames,
                            data.LockDesktop, data.ShowTeacherScreen, data.QuietMode, data.DisableTaskbar,
                            data.BlockUsb, data.BlockApps, data.WhitelistOnly, data.BlockPrint,
                            data.RequireExitPin, data.ExitPin
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Error loading custom preset '{Name}': {Err}", presetName, ex.Message);
            }
        }

        private void SuggestApp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string appName)
            {
                if (appName == "chrome.exe")
                {
                    MessageBox.Show("Lưu ý: Bật Whitelist cho trình duyệt Chrome sẽ mở quyền truy cập Web.\nHãy cân nhắc kết hợp với chính sách 'Chỉ cho phép truy cập các trang giáo dục (.edu.vn)' để kiểm soát nội dung học tập.",
                        "Lưu ý quan trọng", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                if (lstWhitelistApps.Items.Contains(appName))
                {
                    ClassroomDialog.Warn($"Ứng dụng '{appName}' đã tồn tại trong Whitelist.", "Trùng lặp");
                    return;
                }

                lstWhitelistApps.Items.Add(appName);
            }
        }

        private void InitializeIdleTimer()
        {
            _idleTime = 0;
            _idleTimer = new System.Windows.Threading.DispatcherTimer();
            _idleTimer.Interval = TimeSpan.FromSeconds(1);
            _idleTimer.Tick += IdleTimer_Tick;
            _idleTimer.Start();

            this.PreviewMouseMove += (_, _) => ResetIdleTime();
            this.PreviewKeyDown += (_, _) => ResetIdleTime();
        }

        private void StopIdleTimer()
        {
            if (_idleTimer != null)
            {
                _idleTimer.Stop();
                _idleTimer = null;
            }
        }

        private void ResetIdleTime()
        {
            _idleTime = 0;
        }

        private void IdleTimer_Tick(object? sender, EventArgs e)
        {
            _idleTime++;
            if (_idleTime >= _idleThresholdSeconds)
            {
                gridIdleLock.Visibility = Visibility.Visible;
                ClearOtpFields();
                txtPinDigit1.Focus();
                SetBackgroundFocusEnabled(false);
                
                if (txtIdleDurationDesc != null)
                {
                    txtIdleDurationDesc.Text = $"Không phát hiện thao tác của giáo viên trong {GetFriendlyDurationText(_idleThresholdSeconds)}.";
                }
            }
        }

        private void btnIdleUnlock_Click(object sender, RoutedEventArgs e)
        {
            string enteredPin = $"{txtPinDigit1.Text}{txtPinDigit2.Text}{txtPinDigit3.Text}{txtPinDigit4.Text}{txtPinDigit5.Text}{txtPinDigit6.Text}".Trim();
            var control = QASmartClass.Services.ClassControlService.Instance;

            string classCode = "DEFAULT_CLASS";
            try
            {
                // → ClassroomAppContext
                classCode = ClassroomAppContext.Session?.ClassCode ?? "DEFAULT_CLASS";
                if (string.IsNullOrEmpty(classCode)) classCode = "DEFAULT_CLASS";
            }
            catch { }

            string sessionSalt = QASmartClass.Services.ClassControlService.Instance.SessionSalt;
            string salt = !string.IsNullOrEmpty(sessionSalt) ? sessionSalt : classCode;
            string correctHash = control.ExitPinHash;

            bool isCorrect = false;
            if (!control.IsExitPinRequired || string.IsNullOrEmpty(correctHash))
            {
                // Nếu chưa cấu hình Exit PIN, cho phép mở khóa bằng mã PIN dự phòng khẩn cấp "999999"
                if (enteredPin == "999999" || string.IsNullOrEmpty(enteredPin))
                {
                    isCorrect = true;
                }
            }
            else
            {
                string hashedInput = QASmartClass.Services.ClassControlService.ComputeSha256Hash(enteredPin, salt);
                isCorrect = hashedInput == correctHash;
            }

            if (isCorrect)
            {
                gridIdleLock.Visibility = Visibility.Collapsed;
                SetBackgroundFocusEnabled(true);
                ResetIdleTime();
            }
            else
            {
                ClassroomDialog.Error("Mã PIN không chính xác. Vui lòng nhập lại.", "Mở khóa thất bại");
                ClearOtpFields();
                txtPinDigit1.Focus();
            }
        }

        private void OtpTextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (sender is TextBox currentTxt && currentTxt.Text.Length == 1)
            {
                var request = new System.Windows.Input.TraversalRequest(System.Windows.Input.FocusNavigationDirection.Next);
                currentTxt.MoveFocus(request);
            }
        }

        private void OtpTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Back && sender is TextBox currentTxt)
            {
                if (string.IsNullOrEmpty(currentTxt.Text))
                {
                    var request = new System.Windows.Input.TraversalRequest(System.Windows.Input.FocusNavigationDirection.Previous);
                    currentTxt.MoveFocus(request);
                }
            }
        }

        private void ClearOtpFields()
        {
            txtPinDigit1.Text = string.Empty;
            txtPinDigit2.Text = string.Empty;
            txtPinDigit3.Text = string.Empty;
            txtPinDigit4.Text = string.Empty;
            txtPinDigit5.Text = string.Empty;
            txtPinDigit6.Text = string.Empty;
        }

        private string GetFriendlyDurationText(int seconds)
        {
            int minutes = seconds / 60;
            return $"{minutes} phút";
        }

        private void cboIdleDuration_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboIdleDuration == null || cboIdleDuration.SelectedItem == null) return;
            var item = (ComboBoxItem)cboIdleDuration.SelectedItem;
            if (item.Tag != null && int.TryParse(item.Tag.ToString(), out int seconds))
            {
                _idleThresholdSeconds = seconds;
                ResetIdleTime();

                // Lưu cấu hình vào SQLite
                try
                {
                    // → ClassroomAppContext
                    var db = ClassroomAppContext.Db;
                    var setting = db.SystemSettings.FirstOrDefault(s => s.Id == "Policy_IdleLockDuration");
                    if (setting == null)
                    {
                        db.SystemSettings.Add(new Data.SystemSetting
                        {
                            Id = "Policy_IdleLockDuration",
                            Value = seconds.ToString(),
                            Category = "Policy",
                            LastUpdated = DateTime.Now
                        });
                    }
                    else
                    {
                        setting.Value = seconds.ToString();
                        setting.LastUpdated = DateTime.Now;
                    }
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    Log.Warning("Error saving Idle duration setting: {Err}", ex.Message);
                }
            }
        }

        private class PresetData
        {
            public bool BlockInternet { get; set; }
            public bool EduOnly { get; set; }
            public bool BlockSocial { get; set; }
            public bool BlockGames { get; set; }
            public bool LockDesktop { get; set; }
            public bool ShowTeacherScreen { get; set; }
            public bool QuietMode { get; set; }
            public bool DisableTaskbar { get; set; }
            public bool BlockUsb { get; set; }
            public bool BlockApps { get; set; }
            public bool WhitelistOnly { get; set; }
            public bool BlockPrint { get; set; }
            public bool RequireExitPin { get; set; }
            public string ExitPin { get; set; } = string.Empty;
            public System.Collections.Generic.List<string> AppWhitelist { get; set; } = new();
        }

        private static class CustomInputBox
        {
            public static string Show(string prompt, string title, string defaultValue = "")
            {
                var win = new Window
                {
                    Title = title,
                    Width = 400,
                    Height = 170,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize,
                    Topmost = true,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0F2F5")),
                    FontFamily = new FontFamily("Segoe UI")
                };

                var grid = new Grid { Margin = new Thickness(20) };
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var lbl = new TextBlock { Text = prompt, Margin = new Thickness(0, 0, 0, 10), FontSize = 13, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333")) };
                Grid.SetRow(lbl, 0);

                var txt = new TextBox { Text = defaultValue, Padding = new Thickness(5), FontSize = 13 };
                Grid.SetRow(txt, 1);

                var sp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
                Grid.SetRow(sp, 2);

                var btnOk = new Button { Content = "Đồng ý", Padding = new Thickness(15, 6, 15, 6), Margin = new Thickness(0, 0, 10, 0), IsDefault = true, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1976D2")), Foreground = Brushes.White, Cursor = System.Windows.Input.Cursors.Hand };
                btnOk.Click += (s, e) => { win.DialogResult = true; win.Close(); };
                sp.Children.Add(btnOk);

                var btnCancel = new Button { Content = "Hủy", Padding = new Thickness(15, 6, 15, 6), IsCancel = true, Background = Brushes.White, Cursor = System.Windows.Input.Cursors.Hand };
                btnCancel.Click += (s, e) => { win.DialogResult = false; win.Close(); };
                sp.Children.Add(btnCancel);

                grid.Children.Add(lbl);
                grid.Children.Add(txt);
                grid.Children.Add(sp);

                win.Content = grid;
                txt.Focus();
                txt.SelectAll();

                if (win.ShowDialog() == true)
                {
                    return txt.Text.Trim();
                }
                return string.Empty;
            }
        }

        // ─── GIÁM SÁT TRẠNG THÁI HỌC SINH & XỬ LÝ LẠI LỆNH (GIAI ĐOẠN 6) ───

        private void InitializeStudentStatusList()
        {
            foreach (var item in _studentStatuses)
            {
                item.PropertyChanged -= StudentStatus_PropertyChanged;
            }
            _studentStatuses.Clear();
            // → ClassroomAppContext
            var net = ClassroomAppContext.Network;
            if (net != null)
            {
                var students = net.GetConnectedStudents();
                foreach (var student in students)
                {
                    var newModel = new StudentStatusModel
                    {
                        StudentCode = student.Code,
                        PCName = student.PCName,
                        Status = "SUCCESS",
                        StatusText = "Kết nối trực tuyến"
                    };
                    newModel.PropertyChanged += StudentStatus_PropertyChanged;
                    _studentStatuses.Add(newModel);
                }
            }
            UpdateRatioProgressBar();
        }

        private void StudentStatus_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(StudentStatusModel.Status))
            {
                UpdateRatioProgressBar();
            }
        }

        private void UpdateRatioProgressBar()
        {
            Dispatcher.Invoke(() =>
            {
                int total = _studentStatuses.Count;
                if (total == 0)
                {
                    pbPolicyRatio.Value = 0;
                    txtRatioLabel.Text = "Đã áp dụng: 0/0 máy trạm (0%)";
                    return;
                }

                int success = _studentStatuses.Count(s => s.Status == "SUCCESS");
                double percentage = Math.Round((double)success / total * 100);

                pbPolicyRatio.Value = percentage;
                txtRatioLabel.Text = $"Đã áp dụng: {success}/{total} máy trạm ({percentage}%)";
            });
        }

        private async void btnRefreshConnection_Click(object sender, RoutedEventArgs e)
        {
            // → ClassroomAppContext
            var net = ClassroomAppContext.Network;
            if (net == null) return;

            // Debounce (chống spam 5 giây)
            btnRefreshConnection.IsEnabled = false;
            int secondsRemaining = 5;
            var countdownTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            countdownTimer.Tick += (s, ev) =>
            {
                secondsRemaining--;
                if (secondsRemaining <= 0)
                {
                    countdownTimer.Stop();
                    btnRefreshConnection.Content = "🔄 Làm mới";
                    btnRefreshConnection.IsEnabled = true;
                }
                else
                {
                    btnRefreshConnection.Content = $"🔄 Làm mới ({secondsRemaining}s)";
                }
            };
            btnRefreshConnection.Content = $"🔄 Làm mới ({secondsRemaining}s)";
            countdownTimer.Start();

            // Đặt trạng thái toàn bộ máy học sinh về PENDING
            foreach (var status in _studentStatuses)
            {
                status.Status = "PENDING";
                status.StatusText = "Đang kiểm tra kết nối...";
            }

            // Gửi lệnh PING
            string cmdId = Guid.NewGuid().ToString("N").Substring(0, 8); // Command ID ngắn
            string payload = $"CMD|PING|id={cmdId}";

            // Đăng ký tracker cho từng học sinh để xử lý timeout / resend
            var students = net.GetConnectedStudents();
            foreach (var student in students)
            {
                string key = $"{cmdId}_{student.Code}";
                var tracker = new CommandTracker
                {
                    CommandId = cmdId,
                    StudentCode = student.Code,
                    PCName = student.PCName,
                    Payload = payload,
                    LastSentTime = DateTime.Now,
                    RetryCount = 0,
                    IsAcked = false
                };
                _activeTrackers.TryAdd(key, tracker);
            }

            // Phát lệnh broadcast
            await net.SendCommandAsync(payload);
        }

        private void UpdateStudentStatusUI(string studentCode, string pcName, string status, string statusText)
        {
            Dispatcher.Invoke(() =>
            {
                var existing = _studentStatuses.FirstOrDefault(s => s.StudentCode == studentCode);
                if (existing != null)
                {
                    existing.Status = status;
                    existing.StatusText = statusText;
                }
                else
                {
                    var newModel = new StudentStatusModel
                    {
                        StudentCode = studentCode,
                        PCName = pcName,
                        Status = status,
                        StatusText = statusText
                    };
                    newModel.PropertyChanged += StudentStatus_PropertyChanged;
                    _studentStatuses.Add(newModel);
                }
                UpdateRatioProgressBar();
            });
        }

        private void InitializeAckRetryTimer()
        {
            _ackRetryTimer = new System.Windows.Threading.DispatcherTimer();
            _ackRetryTimer.Interval = TimeSpan.FromSeconds(1);
            _ackRetryTimer.Tick += AckRetryTimer_Tick;
            _ackRetryTimer.Start();
        }

        private void StopAckRetryTimer()
        {
            if (_ackRetryTimer != null)
            {
                _ackRetryTimer.Stop();
                _ackRetryTimer = null;
            }
        }

        private async void AckRetryTimer_Tick(object? sender, EventArgs e)
        {
            var now = DateTime.Now;
            // → ClassroomAppContext
            var net = ClassroomAppContext.Network;
            if (net == null) return;

            foreach (var key in _activeTrackers.Keys.ToList())
            {
                if (_activeTrackers.TryGetValue(key, out var tracker))
                {
                    if (tracker.IsAcked)
                    {
                        _activeTrackers.TryRemove(key, out _);
                        continue;
                    }

                    if ((now - tracker.LastSentTime).TotalSeconds >= 3)
                    {
                        if (tracker.RetryCount < 3)
                        {
                            tracker.RetryCount++;
                            tracker.LastSentTime = now;
                            
                            Log.Information("Resending command {CmdId} to student {Code} (Attempt {Count})", tracker.CommandId, tracker.StudentCode, tracker.RetryCount);
                            UpdateStudentStatusUI(tracker.StudentCode, tracker.PCName, "PENDING", $"Đang gửi lại lần {tracker.RetryCount}...");
                            
                            // Send unicast to this student
                            await net.SendToStudentAsync(tracker.StudentCode, tracker.Payload);
                        }
                        else
                        {
                            tracker.IsAcked = true; // Stop retrying
                            _activeTrackers.TryRemove(key, out _);
                            UpdateStudentStatusUI(tracker.StudentCode, tracker.PCName, "FAILED", "Thất bại: Không phản hồi (Timeout)");
                            Log.Warning("Command {CmdId} to student {Code} failed after 3 retries.", tracker.CommandId, tracker.StudentCode);
                        }
                    }
                }
            }
        }


        private void NetworkService_CommandAckReceived(object? sender, QASmartClass.Classroom.Services.CommandAckEventArgs e)
        {
            string key = $"{e.CommandId}_{e.StudentCode}";
            if (_activeTrackers.TryGetValue(key, out var tracker))
            {
                tracker.IsAcked = true;
                _activeTrackers.TryRemove(key, out _);
            }

            // Update UI status
            // → ClassroomAppContext
            var net = ClassroomAppContext.Network;
            string pcName = e.StudentCode;
            if (net != null)
            {
                var students = net.GetConnectedStudents();
                var s = students.FirstOrDefault(st => st.Code == e.StudentCode);
                if (s != null) pcName = s.PCName;
            }

            if (e.Status == "SUCCESS")
            {
                string statusText = e.ErrorMessage == "PONG" ? "Kết nối tốt" : "Đã áp dụng";
                UpdateStudentStatusUI(e.StudentCode, pcName, "SUCCESS", statusText);
            }
            else
            {
                UpdateStudentStatusUI(e.StudentCode, pcName, "FAILED", $"Lỗi: {e.ErrorMessage}");
            }
        }

        private void NetworkService_StudentConnected(object? sender, QASmartClass.Classroom.Services.StudentConnectedEventArgs e)
        {
            UpdateStudentStatusUI(e.StudentCode, e.PCName, "SUCCESS", "Kết nối trực tuyến");
        }

        private void NetworkService_StudentDisconnected(object? sender, string studentCode)
        {
            var existing = _studentStatuses.FirstOrDefault(s => s.StudentCode == studentCode);
            string pcName = existing?.PCName ?? studentCode;
            UpdateStudentStatusUI(studentCode, pcName, "FAILED", "Mất kết nối");
        }

        private string ExtractIdFromCommand(string command)
        {
            try
            {
                var parts = command.Split('|');
                foreach (var part in parts)
                {
                    if (part.StartsWith("id="))
                    {
                        return part.Substring(3);
                    }
                }
            }
            catch { }
            return "unknown";
        }

        // 📤 XUẤT MẪU THIẾT LẬP (EXPORT PRESET)
        private void btnExportPreset_Click(object sender, RoutedEventArgs e)
        {
            if (cboPresets.SelectedItem == null)
            {
                ClassroomDialog.Warn("Vui lòng chọn một mẫu thiết lập để xuất.", "Xuất mẫu");
                return;
            }

            var selectedItem = (ComboBoxItem)cboPresets.SelectedItem;
            string presetName = selectedItem.Content.ToString() ?? string.Empty;

            if (string.IsNullOrEmpty(presetName)) return;

            PresetData? data = null;
            if (presetName == "Mặc định (Mở khóa)")
            {
                data = new PresetData();
            }
            else if (presetName == "Thi cử nghiêm ngặt")
            {
                data = new PresetData
                {
                    BlockInternet = true, BlockSocial = true, BlockGames = true, LockDesktop = true,
                    QuietMode = true, DisableTaskbar = true, BlockUsb = true, BlockApps = true, BlockPrint = true,
                    RequireExitPin = true, ExitPin = "123456"
                };
            }
            else if (presetName == "Thực hành Tin học")
            {
                data = new PresetData
                {
                    BlockInternet = true, BlockSocial = true, BlockGames = true, BlockUsb = true, BlockApps = true,
                    WhitelistOnly = true, RequireExitPin = true, ExitPin = "888888",
                    AppWhitelist = new System.Collections.Generic.List<string> { "scratch.exe", "winword.exe", "excel.exe", "powerpoint.exe" }
                };
            }
            else
            {
                try
                {
                    // → ClassroomAppContext
                    var db = ClassroomAppContext.Db;
                    string key = "Policy_Template_" + presetName;
                    var setting = db.SystemSettings.FirstOrDefault(s => s.Id == key);
                    if (setting != null)
                    {
                        data = Newtonsoft.Json.JsonConvert.DeserializeObject<PresetData>(setting.Value);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("Failed to read preset from database for export: {Err}", ex.Message);
                }
            }

            if (data == null)
            {
                ClassroomDialog.Error("Không thể tải thông tin mẫu thiết lập này.", "Lỗi");
                return;
            }

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json",
                DefaultExt = "json",
                FileName = presetName.Replace(" ", "_") + ".json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
                    System.IO.File.WriteAllText(saveFileDialog.FileName, json, System.Text.Encoding.UTF8);
                    MessageBox.Show($"Đã xuất mẫu thiết lập ra file '{System.IO.Path.GetFileName(saveFileDialog.FileName)}' thành công!", 
                                    "Xuất mẫu thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception exWrite)
                {
                    ClassroomDialog.Error($"Lỗi ghi file: {exWrite.Message}", "Lỗi");
                }
            }
        }

        // 📥 NHẬP MẪU THIẾT LẬP (IMPORT PRESET)
        private void btnImportPreset_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON Files (*.json)|*.json",
                DefaultExt = "json"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string json = System.IO.File.ReadAllText(openFileDialog.FileName, System.Text.Encoding.UTF8);
                    
                    PresetData? data = null;
                    try
                    {
                        data = Newtonsoft.Json.JsonConvert.DeserializeObject<PresetData>(json);
                    }
                    catch (Exception jsonEx)
                    {
                        ClassroomDialog.Error($"File JSON sai cấu trúc hoặc bị hỏng:\n{jsonEx.Message}", "Lỗi cấu trúc tệp");
                        return;
                    }

                    if (data == null)
                    {
                        ClassroomDialog.Warn("Mẫu thiết lập trống hoặc không hợp lệ.", "Lỗi");
                        return;
                    }

                    string defaultName = System.IO.Path.GetFileNameWithoutExtension(openFileDialog.FileName).Replace("_", " ");
                    string name = CustomInputBox.Show("Nhập tên cho mẫu thiết lập nhập khẩu:", "Nhập khẩu thiết lập", defaultName);
                    
                    if (string.IsNullOrEmpty(name)) return;

                    if (name == "Mặc định (Mở khóa)" || name == "Thi cử nghiêm ngặt" || name == "Thực hành Tin học")
                    {
                        ClassroomDialog.Warn("Không thể ghi đè các mẫu mặc định của hệ thống.", "Lỗi");
                        return;
                    }

                    // → ClassroomAppContext
                    var db = ClassroomAppContext.Db;
                    string key = "Policy_Template_" + name;

                    string serializedData = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                    var setting = db.SystemSettings.FirstOrDefault(s => s.Id == key);
                    if (setting == null)
                    {
                        setting = new Data.SystemSetting { Id = key, Value = serializedData, Category = "PolicyTemplate", LastUpdated = DateTime.Now };
                        db.SystemSettings.Add(setting);
                    }
                    else
                    {
                        setting.Value = serializedData;
                        setting.LastUpdated = DateTime.Now;
                    }
                    db.SaveChanges();

                    LoadPresets();

                    foreach (ComboBoxItem item in cboPresets.Items)
                    {
                        if (item.Content.ToString() == name)
                        {
                            cboPresets.SelectedItem = item;
                            break;
                        }
                    }

                    ClassroomDialog.Info($"Nhập khẩu mẫu thiết lập '{name}' thành công!", "Thành công");
                }
                catch (Exception exImport)
                {
                    ClassroomDialog.Error($"Lỗi nhập khẩu mẫu: {exImport.Message}", "Lỗi hệ thống");
                }
            }
        }

        // ─── LỚP TIỆN ÍCH DỮ LIỆU GIÁM SÁT & THEO DÕI LỆNH ───

        public class StudentStatusModel : System.ComponentModel.INotifyPropertyChanged
        {
            private string _status = "PENDING";
            private string _statusText = "Đang chờ phản hồi...";

            public string StudentCode { get; set; } = string.Empty;
            public string PCName { get; set; } = string.Empty;

            public string Status
            {
                get => _status;
                set
                {
                    if (_status != value)
                    {
                        _status = value;
                        OnPropertyChanged(nameof(Status));
                    }
                }
            }

            public string StatusText
            {
                get => _statusText;
                set
                {
                    if (_statusText != value)
                    {
                        _statusText = value;
                        OnPropertyChanged(nameof(StatusText));
                    }
                }
            }

            public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }

        private class CommandTracker
        {
            public string CommandId { get; set; } = string.Empty;
            public string Payload { get; set; } = string.Empty;
            public string StudentCode { get; set; } = string.Empty;
            public string PCName { get; set; } = string.Empty;
            public DateTime LastSentTime { get; set; } = DateTime.Now;
            public int RetryCount { get; set; } = 0;
            public bool IsAcked { get; set; } = false;
        }
    }
}
