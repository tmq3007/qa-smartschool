using System;

using System.IO;

using System.Linq;

using System.Threading.Tasks;

using System.Windows;

using System.Windows.Controls;

using System.Windows.Input;

using System.Windows.Media;

using System.Windows.Media.Animation;

using System.Windows.Media.Effects;

using System.Windows.Media.Imaging;

using System.Windows.Threading;

using Microsoft.Win32;

using QASmartClass.Shared;

using Serilog;



namespace QASmartClass.Classroom.Views

{

	public partial class ClassroomShell : Window

	{
		public string? SelectedStudentCode { get; set; } = null;


		private readonly ModeService _modeService;

		private readonly DispatcherTimer _clockTimer;

		private string _currentPage = "F1";

		private int _unreadCount = 0;

		private int _connectedStudentCount = 0;

		

		// Navigation Stack cho chức năng Back/Home

		private readonly System.Collections.Generic.Stack<string> _navigationStack = new();

		private const int MAX_STACK_SIZE = 20;

		

		// Theo dõi trang đang mở trước khi chuyển mode

		private string? _lastPageBeforeSwitch = null;

		private AIAssistantPage? _aiAssistantPage;



		// Page cache để tối ưu hóa nạp trang và tránh mất trạng thái

		private readonly System.Collections.Generic.Dictionary<string, Page> _pageCache = new();



		// Caching navigation buttons cho việc cập nhật style O(1)

		private readonly System.Collections.Generic.Dictionary<string, Button> _navButtonsMap = new();



		// Luồng bất đồng bộ của Custom Dialog Overlay

		private System.Threading.Tasks.TaskCompletionSource<bool>? _dialogTcs;



		public ClassroomShell()

		{

			InitializeComponent();



			// Register frame navigation event for active menu sync

			contentFrame.Navigated += MainFrame_Navigated;



			// Get ModeService from App

			_modeService = ((QASmartTouch.App)Application.Current).ModeService;



			// Clock timer

			_clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };

			_clockTimer.Tick += (s, e) => txtClock.Text = DateTime.Now.ToString("HH:mm");

			_clockTimer.Start();

			txtClock.Text = DateTime.Now.ToString("HH:mm");



			QASmartClass.Shared.LanguageManager.LanguageChanged += (lang) =>

			{

				UpdateOnlineStudentCountUI();

			};



			Log.Information("ClassroomShell initialized");



			// Xử lý lưu trạng thái khi ẩn/hiện cửa sổ (chuyển mode)

			this.IsVisibleChanged += (s, e) =>

			{

				if (!this.IsVisible)

				{

					// Lưu trạng thái trước khi ẩn

					_lastPageBeforeSwitch = _currentPage;

					Log.Information("ClassroomShell hidden. Saved state: {Page}", _lastPageBeforeSwitch);

				}

				else if (!string.IsNullOrEmpty(_lastPageBeforeSwitch) && _lastPageBeforeSwitch != _currentPage)

				{

					// Phục hồi trạng thái khi hiện lại

					Log.Information("ClassroomShell shown. Restoring state: {Page}", _lastPageBeforeSwitch);

					NavigateTo(_lastPageBeforeSwitch, addToStack: false);

					_lastPageBeforeSwitch = null;

				}

			};



			// Load teacher profile + trang chủ mặc định

			Loaded += async (s, e) =>

			{

				InitializeNavButtonsMap();

				LoadTeacherProfile();

				NavigateTo("F1");

				await StartNetworkAsync();

				// Subscribe VNC events (LOI_VID_46)
				QASmartClass.Services.VncBroadcastService.Instance.StatusChanged += OnVncBroadcastStatusChanged;
				QASmartClass.Services.VncBroadcastService.Instance.TimerTick += OnVncBroadcastTimerTick;

				// Sync initial VNC state if already broadcasting
				if (QASmartClass.Services.VncBroadcastService.Instance.IsVncBroadcasting)
				{
					brdBroadcastStatus.Visibility = Visibility.Visible;
					txtBroadcastTime.Text = $"🔴 Đang phát VNC ({QASmartClass.Services.VncBroadcastService.Instance.ElapsedTimeDisplay})";
				}
			};

			this.Unloaded += (s, e) =>
			{
				QASmartClass.Services.VncBroadcastService.Instance.StatusChanged -= OnVncBroadcastStatusChanged;
				QASmartClass.Services.VncBroadcastService.Instance.TimerTick -= OnVncBroadcastTimerTick;
			};

		}



		/// <summary>Auto-start network broadcast so students can connect</summary>

		private async Task StartNetworkAsync()

		{

			try

			{

				var app = (QASmartTouch.App)Application.Current;

				var net = app.NetworkService;

				if (net != null && !net.IsBroadcasting)

				{

					// Get teacher info from DB

					var teacher = app.Database.TeacherProfiles.FirstOrDefault();

					var classroom = app.Database.Classrooms.FirstOrDefault();



					string teacherName = teacher?.FullName ?? "Giáo viên";

					string className = QASmartClass.Classroom.Services.RosterHelper.GetActiveRosterName();



					await net.StartAsync(className, teacherName);



					// Update UI

					UpdateSessionInfo(className, net.ConnectedCount);



					// Subscribe to student events

					net.StudentConnected += (s, args) =>

					{

						Dispatcher.Invoke(() =>

						{

							_connectedStudentCount = net.ConnectedCount;

							txtStudentCount.Text = $"{_connectedStudentCount} HS online";

							UpdateOnlineStudentCountUI();

							Log.Information("Student connected: {Name} ({Code})", args.StudentName, args.StudentCode);

						});

					};

					net.StudentDisconnected += (s, code) =>

					{

						Dispatcher.Invoke(() =>

						{

							_connectedStudentCount = net.ConnectedCount;

							txtStudentCount.Text = $"{_connectedStudentCount} HS online";

							UpdateOnlineStudentCountUI();

						});

					};



					// ═══ Global notification: student questions, hand raises, chat ═══

					net.MessageReceived += (s, args) =>

					{

						Dispatcher.Invoke(() =>

						{

							var msg = args.Message;



							if (msg == "CMD|GET_BROADCAST_STATUS")

							{

								try

								{

									var app = (QASmartTouch.App)Application.Current;

									var state = QASmartClass.Services.BroadcastStateService.Instance;

									if (state != null && state.IsScreenBroadcastActive && app.NetworkService != null)

									{

										string filePath = state.ScreenCapturePath;

										string token = state.BroadcastToken;

										bool isForce = state.IsForceWatchActive;

										int webPort = 8080;

										if (app.NetworkService.WebBridge != null)

										{

											webPort = app.NetworkService.WebBridge.WebPort;

										}

										string cmd = $"CMD|SCREEN_BROADCAST_START|{filePath}|{(isForce ? "FORCE_WATCH|" : "")}{webPort}|{token}";

										_ = app.NetworkService.SendToStudentAsync(args.StudentCode, cmd);

										Log.Information("[BroadcastPage] Resent broadcast start to reconnected student {Code} (Force: {Force})", args.StudentCode, isForce);

									}

								}

								catch (Exception ex)

								{

									Log.Warning("[BroadcastPage] Resend broadcast on reconnect error: {Err}", ex.Message);

								}

							}

							

							// Decrypt encrypted student question (from fallback mode)

							if (msg.StartsWith("STUDENT_QUESTION_ENC|"))

							{

								try

								{

									var dbApp = (QASmartTouch.App)Application.Current;

									string classCode = dbApp?.ClassroomSession?.ClassCode ?? "DEFAULT_CLASS";

									if (string.IsNullOrEmpty(classCode)) classCode = "DEFAULT_CLASS";

									

									string encryptedText = msg.Replace("STUDENT_QUESTION_ENC|text=", "");

									string decrypted = QASmartClass.Utilities.CryptoHelper.Decrypt(encryptedText, classCode);

									if (!string.IsNullOrEmpty(decrypted))

									{

										long ts = 0;

										bool hasTs = false;

										var parts = decrypted.Split('|');

										var tsPart = parts.FirstOrDefault(p => p.StartsWith("ts="));

										if (tsPart != null && long.TryParse(tsPart.Substring(3), out ts))

										{

											hasTs = true;

										}



										if (hasTs)

										{

											long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

											if (Math.Abs(now - ts) > 10)

											{

												Log.Warning("Security alert: Rejecting drifted/replayed student question from {StudentCode}. Drift: {Drift}s", args.StudentCode, now - ts);

												return;

											}

										}



										string questionText = decrypted;

										if (hasTs && tsPart != null)

										{

											int idx = decrypted.LastIndexOf("|" + tsPart);

											if (idx >= 0)

											{

												questionText = decrypted.Substring(0, idx);

											}

										}

										if (questionText.StartsWith("text="))

										{

											questionText = questionText.Substring(5);

										}

										msg = $"STUDENT_QUESTION|text={questionText}";

									}

								}

								catch (Exception ex)

								{

									Log.Warning("Failed to decrypt fallback question: {Err}", ex.Message);

								}

							}



							// Decrypt encrypted student hand raise (from fallback mode)

							if (msg.StartsWith("HAND_RAISE_ENC|"))

							{

								try

								{

									var dbApp = (QASmartTouch.App)Application.Current;

									string classCode = dbApp?.ClassroomSession?.ClassCode ?? "DEFAULT_CLASS";

									if (string.IsNullOrEmpty(classCode)) classCode = "DEFAULT_CLASS";

									

									string encryptedText = msg.Replace("HAND_RAISE_ENC|payload=", "");

									string decrypted = QASmartClass.Utilities.CryptoHelper.Decrypt(encryptedText, classCode);

									if (!string.IsNullOrEmpty(decrypted))

									{

										long ts = 0;

										bool hasTs = false;

										var parts = decrypted.Split('|');

										var tsPart = parts.FirstOrDefault(p => p.StartsWith("ts="));

										if (tsPart != null && long.TryParse(tsPart.Substring(3), out ts))

										{

											hasTs = true;

										}



										if (hasTs)

										{

											long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

											if (Math.Abs(now - ts) > 10)

											{

												Log.Warning("Security alert: Rejecting drifted/replayed hand raise from {StudentCode}. Drift: {Drift}s", args.StudentCode, now - ts);

												return;

											}

										}



										string cleanPayload = decrypted;

										if (hasTs && tsPart != null)

										{

											int idx = decrypted.LastIndexOf("|" + tsPart);

											if (idx >= 0)

											{

												cleanPayload = decrypted.Substring(0, idx);

											}

										}

										msg = $"HAND_RAISE|{cleanPayload}";

									}

								}

								catch (Exception ex)

								{

									Log.Warning("Failed to decrypt fallback hand raise: {Err}", ex.Message);

								}

							}



							string studentName = args.StudentCode;

							try

							{

								foreach (var c in net.GetConnectedStudents())

									if (c.Code == args.StudentCode) { studentName = c.Name; break; }

							}

							catch { }



							// ── Determine event type and display text ──

							string eventType = "CHAT";

							string displayText = MessagingPage.SanitizeBadWords(msg);

							string channel = args.StudentCode; // default: private



							if (msg.StartsWith("STUDENT_QUESTION|"))

							{

								eventType = "STUDENT_QUESTION";

								var question = MessagingPage.SanitizeBadWords(msg.Replace("STUDENT_QUESTION|text=", ""));

								displayText = $"❓ {question}";

								ShowToast(studentName, question, "QUESTION");

								IncrementBadge();

							}

							else if (msg.StartsWith("HAND_RAISE|"))

							{

								eventType = "HAND_RAISE";

								bool raised = msg.Contains("raised=True");

								

								// Cập nhật trạng thái IsHandRaised trong ClassroomSession

								try

								{

									var app = (QASmartTouch.App)Application.Current;

									var session = app?.ClassroomSession;

									if (session != null)

									{

										var student = session.ConnectedStudents.FirstOrDefault(s =>

											(!string.IsNullOrEmpty(s.StudentCode) && s.StudentCode.Equals(args.StudentCode, StringComparison.OrdinalIgnoreCase)) ||

											s.Name.Equals(studentName, StringComparison.OrdinalIgnoreCase));

										if (student != null)

										{

											student.IsHandRaised = raised;

										}

									}

								}

								catch (Exception ex)

								{

									Log.Warning("Failed to update student hand raise state: {Err}", ex.Message);

								}



								string reason = "";

								if (msg.Contains("reason="))

								{

									var reasonPart = msg.Split('|').FirstOrDefault(p => p.StartsWith("reason="));

									if (reasonPart != null)

									{

										reason = reasonPart.Replace("reason=", "").Trim();

									}

								}



								if (raised)

								{

									// Phát âm thanh báo hiệu giơ tay (sử dụng chime chuyên dụng, fallback về bíp hệ thống)

									try

									{

										string soundPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "handraise_alert.wav");

										if (System.IO.File.Exists(soundPath))

										{

											using (var player = new System.Media.SoundPlayer(soundPath))

											{

												player.Play();

											}

										}

										else

										{

											System.Media.SystemSounds.Beep.Play();

											Log.Warning("Handraise alert sound file not found at {Path}, falling back to system beep.", soundPath);

										}

									}

									catch (Exception ex)

									{

										Log.Warning("Failed to play hand raise alert sound: {Err}. Fallback to system beep.", ex.Message);

										try

										{

											System.Media.SystemSounds.Beep.Play();

										}

										catch { }

									}



									string reasonDetail = !string.IsNullOrEmpty(reason) ? $" ({reason})" : "";

									displayText = $"🖐️ Giơ tay xin phát biểu{reasonDetail}";

									

									string toastMessage = !string.IsNullOrEmpty(reason) ? $"Giơ tay xin phát biểu: {reason}" : "Giơ tay xin phát biểu";

									ShowToast(studentName, toastMessage, "HAND_RAISE");

									IncrementBadge();



									// Award XP for hand raise via GamificationService

									try

									{

										var dbApp = (QASmartTouch.App)Application.Current;

										var gamification = dbApp.NetworkService.Gamification;

										if (gamification != null)

										{

											gamification.AwardHandRaiseXp(args.StudentCode);

											_ = gamification.BroadcastLeaderboardAsync();

										}

									}

									catch (Exception ex)

									{

										Log.Warning("Failed to award hand raise XP: {Err}", ex.Message);

									}

								}

								else

								{

									displayText = "✋ Đã hạ tay";

								}

							}

							else if (msg.StartsWith("CMD|STUDENT_FEEDBACK|"))

							{

								var parts = msg.Split('|');

								if (parts.Length >= 5)

								{

									string fbType = parts[2];  // "DISPLAY_ISSUE"

									string studentNameVal = parts[3];

									string fbMessage = parts[4];

									ShowToast(studentNameVal, fbMessage, "FEEDBACK");

									Log.Information("[ClassroomShell] Student feedback: {Student} — {Msg}", studentNameVal, fbMessage);

								}

							}

							else if (msg.StartsWith("CHAT|"))

							{

								eventType = "CHAT";

								var parts = msg.Split('|', 4);

								if (parts.Length >= 4)

								{

									// CHAT|code|channel|text

									var studentChannel = parts[2];

									displayText = MessagingPage.SanitizeBadWords(parts[3]);

									channel = studentChannel == "ALL" ? "ALL"

											: studentChannel == "TEACHER" ? args.StudentCode

											: studentChannel.StartsWith("GROUP_") || studentChannel.StartsWith("STUDY_") ? studentChannel

											: args.StudentCode;

								}

								else if (parts.Length >= 3)

								{

									displayText = MessagingPage.SanitizeBadWords(parts[2]);

								}



								if (_currentPage != "F13")

								{

									IncrementBadge();

									if (!QASmartTouch.App.LessonState.IsLessonActive)

									{

										ShowToast(studentName, displayText, "CHAT");

										try

										{

											System.Media.SystemSounds.Asterisk.Play();

										}

										catch { }

									}

								}

							}

							else if (msg.StartsWith("STUDENT_SUBMISSION|"))

							{

								var parts = msg.Split('|', 5);

								if (parts.Length >= 5)

								{

									var toolId = parts[1];

									var studentCode = parts[2];

									var subStudentName = parts[3];

									var resultData = parts[4];

									if (contentFrame.Content is QASmartClass.LearningTools.Views.LearningToolsHub hub)

									{

										hub.HandleStudentSubmission(toolId, studentCode, subStudentName, resultData);

									}

								}

								return;

							}



							// ── Save ALL messages to DB (so MessagingPage can load history) ──

							// Only save if MessagingPage is NOT currently open (it saves its own)

							if (_currentPage != "F13")

							{

								try

								{

									var dbApp = (QASmartTouch.App)Application.Current;

									if (dbApp?.Database != null)

									{

										dbApp.Database.EventLogs.Add(new QASmartClass.Data.EventLog

										{

											EventType = eventType,

											Actor = args.StudentCode,

											Details = $"Tin nhắn: {displayText} [CH:{channel}]",

											Timestamp = DateTime.Now

										});

										dbApp.Database.SaveChanges();

									}

								}

								catch (Exception ex)

								{

									Log.Warning("Shell DB save error: {Err}", ex.Message);

								}

							}

						});

					};



					Log.Information("Network started: {Class} — {Teacher} @ {IP}",

						className, teacherName, net.ServerIP);

				}

			}

			catch (Exception ex)

			{

				Log.Warning("Network auto-start error: {Err}", ex.Message);

			}

		}



		/// <summary>

		/// Navigation button click — chuyển page

		/// </summary>

		private void NavButton_Click(object sender, RoutedEventArgs e)

		{

			if (sender is Button btn && btn.Tag is string tag)

			{

				NavigateTo(tag);

			}

		}



		private void MainFrame_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)

		{

			if (e.Content == null) return;

			string pageTypeName = e.Content.GetType().Name;

			string? formId = pageTypeName switch

			{

				"DashboardPage" => "F1",

				"LessonListPage" => "F2",

				"LessonEditorPage" => "F3",

				"ClassroomPage" => "F4",

				"MonitorPage" => "F5",

				"QuizPage" => "F6",

				"ReportPage" => "F7",

				"SettingsPage" => "F8",

				"LibraryPage" => "F9",

				"GroupPage" => "F10",

				"FileTransferPage" => "F11",

				"MessagingPage" => "F13",

				"QuestionBankPage" => "F14",

				"StudentPage" => "F15",

				"BroadcastPage" => "F16",

				"PolicyPage" => "F17",

				"SurveyPage" => "F18",

				"EventLogPage" => "F19",

				"CanvasPage" => "F20",

				"ChartPage" => "F21",

				"TimerPage" => "F22",

				"RandomPickerPage" => "F23",

				"SplitScreenPage" => "F24",

				"LessonHistoryPage" => "F25",

				"TimetablePage" => "F26",

				"AttendancePage" => "F27",

				"HomeworkPage" => "F28",

				"AIAssistantPage" => "F29",

				"WebsitePushPage" => "F30",

				"StemToolsPage" => "F31",

				"LearningToolsHub" => "F32",

				"TeacherListPage" => "F33",

				"PracticalAppsPage" => "F34",

				_ => null

			};



			if (formId != null)

			{

				_currentPage = formId;

				UpdateActiveButtonStyles(formId);

			}

		}



		/// <summary>

		/// Chuyển đến form tương ứng

		/// </summary>

		/// <summary>

		/// Chuyển đến form tương ứng (Tương thích ngược đồng bộ)

		/// </summary>

		public void NavigateTo(string formId, bool addToStack = true)

		{

			_ = NavigateToAsync(formId, addToStack);

		}



		/// <summary>

		/// Chuyển đến form tương ứng bất đồng bộ (Hỗ trợ Caching, Async Dialogs và Transitions)

		/// </summary>

		public async Task NavigateToAsync(string formId, bool addToStack = true)

		{

			// Tránh vòng lặp hoặc lưu trùng lặp trang hiện tại

			if (formId == _currentPage) return;





			// Kiểm tra trạng thái trang hiện tại bằng dialog bất đồng bộ (tránh block UI thread)

			if (contentFrame.Content is LessonEditorPage editor && editor.HasUnsavedChanges)

			{

				bool confirm = await ShowAsyncConfirmDialog("Bạn có thay đổi chưa lưu trong Bài giảng. Bạn có muốn bỏ qua các thay đổi này và rời đi?");

				if (!confirm) return;

			}

			else if (contentFrame.Content is QuizPage quiz && quiz.IsQuizActive)

			{

				bool confirm = await ShowAsyncConfirmDialog("Bài kiểm tra đang diễn ra. Bạn có chắc muốn thoát?");

				if (!confirm) return;

			}

			else if (contentFrame.Content is LessonRunnerPage runner && runner.IsRunning)

			{

				bool confirm = await ShowAsyncConfirmDialog("Phiên giảng dạy đang diễn ra. Bạn có chắc muốn thoát?");

				if (!confirm) return;

			}



			// F20: Bảng vẽ tự do → chuyển sang SmartScreen

			if (formId == "F20")

			{

				Log.Information("Navigate to F20 -> Switching to SmartScreen drawing mode");

				_modeService.GoToScreen();

				return;

			}



			// Giải phóng tài nguyên Page cũ (chống Memory Leak)

			if (contentFrame.Content is IDisposable disposable)

			{

				try

				{

					disposable.Dispose();

					Log.Information("Successfully disposed previous page of type {Page}", contentFrame.Content.GetType().Name);

				}

				catch (Exception ex)

				{

					Log.Warning("Error disposing page: {Err}", ex.Message);

				}

			}



			// Xử lý Navigation Stack

			if (addToStack && !string.IsNullOrEmpty(_currentPage))

			{

				_navigationStack.Push(_currentPage);

				// Cắt bớt nếu vượt quá giới hạn để tránh tràn RAM

				if (_navigationStack.Count > MAX_STACK_SIZE)

				{

					var tempList = _navigationStack.ToList();

					tempList.RemoveAt(tempList.Count - 1); // remove bottom

					_navigationStack.Clear();

					for (int i = tempList.Count - 1; i >= 0; i--)

						_navigationStack.Push(tempList[i]);

				}

			}



			_currentPage = formId;



			// Cập nhật trạng thái hiển thị của các nút điều hướng TopBar

			if (btnGoBack != null)

				btnGoBack.Visibility = _navigationStack.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

			if (btnGoHome != null)

				btnGoHome.Visibility = _currentPage == "F1" ? Visibility.Collapsed : Visibility.Visible;



			// Reset notification badge when opening Messaging

			if (formId == "F13")

				ResetBadge();



			// Cập nhật Active Button Styles nhanh độ phức tạp O(1)

			UpdateActiveButtonStyles(formId);



			// Update page title

			txtPageTitle.Text = formId switch

			{

				"F1"  => "🏠 Trang chủ",

				"F2"  => "📚 Quản lý Bài giảng",

				"F3"  => "✏️ Soạn bài giảng",

				"F4"  => "🖥️ Phiên dạy",

				"F5"  => "👁️ Giám sát MH học sinh",

				"F6"  => "🏆 Kiểm tra / Quiz",

				"F7"  => "📊 Báo cáo & Thống kê",

				"F8"  => "⚙️ Cài đặt",

				"F9"  => "📖 Thư viện Tài nguyên",

				"F10" => "👥 Quản lý Nhóm",

				"F11" => "📁 Phát / Thu bài",

				"F12" => "👁️ Giám sát MH học sinh",  // F12 merged into F5

				"F13" => "💬 Tin nhắn & Thông báo",

				"F14" => "📋 Ngân hàng Câu hỏi",

				"F15" => "📋 Danh sách lớp & Học sinh",

				"F15S" => "👨\u200d🎓 Quản lý Học sinh",

				"F16" => "📺 Chiếu Màn hình",

				"F17" => "🛡️ Bảo mật & Quyền",

				"F18" => "🗳️ Khảo sát nhanh",

				"F19" => "🗒️ Nhật ký Sự kiện",

				"F20" => "🎨 Bảng vẽ tự do",

				"F21" => "📈 Biểu đồ tương tác",

				"F22" => "⏱️ Đồng hồ & Bấm giờ",

				"F23" => "🎲 Chọn Học sinh Ngẫu nhiên",

				"F24" => "🖥️ Màn hình phân chia",

				"F25" => "🕐 Lịch Sử Phiên Bản",

				"F26" => "📅 Thời khóa biểu",

				"F27" => "📋 Điểm danh",

				"F28" => "📝 Bài tập về nhà",

				"F29" => "🤖 AI Trợ giảng",

				"F30" => "🌐 Mở website trên máy HS",

				"F31" => "📐 Công cụ STEM",

				"F32" => "📚 Bộ Công Cụ Học Tập",

				"F33" => "👨‍🏫 Quản lý Giáo viên",

				"F34" => "🌍 Ứng dụng thực tế",

				"F35" => "🔧 Chẩn đoán Mạng LAN",

				_	 => formId

			};



			Log.Information("Navigate to: {FormId}", formId);

			QASmartClass.Services.TelemetryService.Instance.Track("PAGE_NAVIGATED", formId);



			// Kiểm tra xem Page đã có trong Cache chưa để tối ưu ẩn/hiện Loading

			Page? page = null;

			bool isCached = _pageCache.TryGetValue(formId, out page) && page != null;



			System.Threading.CancellationTokenSource? cts = null;



			// Bắt đầu hiệu ứng dịch chuyển trang cũ

			await AnimatePageOutAsync();



			// Chỉ hiển thị vòng quay nạp dữ liệu (Loading Indicator) khi tạo mới lần đầu và quá trình nạp kéo dài > 150ms

			if (!isCached)

			{

				cts = new System.Threading.CancellationTokenSource();

				var token = cts.Token;

				_ = Task.Delay(150, token).ContinueWith(t =>

				{

					if (!t.IsCanceled)

					{

						Dispatcher.Invoke(() =>

						{

							if (loadingOverlay != null)

							{

								loadingOverlay.Visibility = Visibility.Visible;

							}

						});

					}

				}, TaskScheduler.Default);

			}



			if (!isCached)

			{

				try
				{
					page = formId switch

					{

						"F1"  => new DashboardPage(),

						"F2"  => new LessonListPage(),

						"F3"  => new LessonEditorPage(),

						"F4"  => new ClassroomPage(),

						"F5"  => new MonitorPage(),

						"F6"  => new QuizPage(),

						"F7"  => new ReportPage(),

						"F8"  => new SettingsPage(),

						"F9"  => new LibraryPage(),

						"F10" => new GroupPage(),

						"F11" => new FileTransferPage(),

						"F12" => new MonitorPage(),  // Remote merged into Monitor

						"F13" => new MessagingPage(),

						"F14" => new QuestionBankPage(),

						"F15" => new StudentPage(showRosterFirst: true),

						"F15S" => new StudentPage(showRosterFirst: false),

						"F16" => new BroadcastPage(),

						"F17" => new PolicyPage(),

						"F18" => new SurveyPage(),

						"F19" => new EventLogPage(),

						"F20" => new CanvasPage(),

						"F21" => new ChartPage(),

						"F22" => new TimerPage(),

						"F23" => new RandomPickerPage(),

						"F24" => new SplitScreenPage(),

						"F25" => new LessonHistoryPage(),

						"F27" => new AttendancePage(),

						"F26" => new TimetablePage(),

						"F30" => new WebsitePushPage(),

						"F28" => new HomeworkPage(),

						"F29" => _aiAssistantPage ??= new AIAssistantPage(),

						"F31" => new StemToolsPage(),

						"F32" => new LearningTools.Views.LearningToolsHub(),

						"F33" => new TeacherListPage(),

						"F34" => new PracticalAppsPage(),

						"F35" => new NetworkDiagnosticsPage(),

						_ => null

					};



					if (page != null)

					{

						_pageCache[formId] = page;

					}
				}
				catch (Exception ex)
				{
					Log.Error("Failed to create page {FormId}: {Err}", formId, ex.Message);
					MessageBox.Show($"Không thể mở trang này do lỗi hệ thống: {ex.Message}", "Lỗi tải trang", MessageBoxButton.OK, MessageBoxImage.Error);
					page = null;

					if (cts != null)
					{
						cts.Cancel();
						cts.Dispose();
					}
					if (loadingOverlay != null)
					{
						loadingOverlay.Visibility = Visibility.Collapsed;
					}
					return;
				}

			}



			if (page != null)

			{

				// Thực thi tải lại dữ liệu bất đồng bộ đối với trang lấy từ Cache

				if (page is INavigatedPage navigatedPage)

				{

					try

					{

						await navigatedPage.OnNavigatedToAsync();

					}

					catch (Exception ex)

					{

						Log.Error("Error on page OnNavigatedToAsync: {Err}", ex.Message);

					}

				}



				contentFrame.Content = page;

				

				// Chờ nạp trang hoàn tất để kích hoạt hiệu ứng Fade In & Slide In mượt mà

				await AnimatePageInAsync();

			}

			else

			{

				contentFrame.Content = CreatePlaceholder(formId, txtPageTitle.Text);

				await AnimatePageInAsync();

			}



			if (cts != null)

			{

				cts.Cancel();

				cts.Dispose();

			}

			if (loadingOverlay != null)

			{

				loadingOverlay.Visibility = Visibility.Collapsed;

			}

		}



		// ═══════════════════════════════════════════════════════════

		//  PAGE TRANSITION & ASYNC DIALOG OVERLAYS

		// ═══════════════════════════════════════════════════════════



		private async Task AnimatePageOutAsync()

		{

			// Không làm mờ hay trượt trang cũ để tránh hiện tượng nháy hình gây đau mắt

			await Task.CompletedTask;

		}



		private async Task AnimatePageInAsync()

		{

			if (contentFrame == null || frameTranslate == null) return;



			var tcs = new TaskCompletionSource<bool>();

			var slideAnim = new DoubleAnimation(15, 0, TimeSpan.FromMilliseconds(100))

			{

				EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }

			};



			slideAnim.Completed += (s, e) => tcs.TrySetResult(true);



			// Giữ Opacity của contentFrame luôn là 1.0 (tránh chớp tắt/nháy hình)

			contentFrame.BeginAnimation(Frame.OpacityProperty, null);

			contentFrame.Opacity = 1.0;



			frameTranslate.BeginAnimation(TranslateTransform.XProperty, slideAnim);



			await tcs.Task;

		}



		private void InitializeNavButtonsMap()

		{

			_navButtonsMap.Clear();

			var buttons = GetNavButtons();

			foreach (var btn in buttons)

			{

				if (btn.Tag is string tag)

				{

					_navButtonsMap[tag] = btn;

				}

			}

		}



		private void UpdateActiveButtonStyles(string activeFormId)

		{

			if (activeFormId == "F15S") activeFormId = "F15";

			else if (activeFormId == "F12") activeFormId = "F5";



			if (_navButtonsMap.Count == 0)

			{

				InitializeNavButtonsMap();

			}



			foreach (var kvp in _navButtonsMap)

			{

				if (kvp.Value != null)

				{

					string buttonTag = kvp.Key;

					kvp.Value.Style = (Style)FindResource(buttonTag == activeFormId ? "NavButtonActive" : "NavButton");

				}

			}

		}



		public Task<bool> ShowAsyncConfirmDialog(string message)

		{

			_dialogTcs = new TaskCompletionSource<bool>();

			txtConfirmMessage.Text = message;

			confirmationOverlay.Visibility = Visibility.Visible;

			return _dialogTcs.Task;

		}



		private void btnConfirmCancel_Click(object sender, RoutedEventArgs e)

		{

			confirmationOverlay.Visibility = Visibility.Collapsed;

			_dialogTcs?.TrySetResult(false);

		}



		private void btnConfirmOK_Click(object sender, RoutedEventArgs e)

		{

			confirmationOverlay.Visibility = Visibility.Collapsed;

			_dialogTcs?.TrySetResult(true);

		}



		/// <summary>

		/// Mở LessonEditor với ID bài giảng cụ thể (edit mode)

		/// </summary>

		public void NavigateToEditor(int lessonId)

		{

			if (contentFrame.Content is IDisposable disposable)

			{

				try { disposable.Dispose(); } catch {}

			}

			txtPageTitle.Text = "✏️ Chỉnh sửa bài giảng";

			var page = new LessonEditorPage(lessonId);

			contentFrame.Content = page;

			Log.Information("Navigate to LessonEditor (Edit) — LessonID={Id}", lessonId);

		}



		/// <summary>Mở LessonEditor ở chế độ tạo mới</summary>

		public void NavigateToEditor()

		{

			if (contentFrame.Content is IDisposable disposable)

			{

				try { disposable.Dispose(); } catch {}

			}

			txtPageTitle.Text = "➕ Tạo bài giảng mới";

			contentFrame.Content = new LessonEditorPage();

			Log.Information("Navigate to LessonEditor (New)");

		}



		/// <summary>Mở bảng vẽ với nội dung bài giảng</summary>

		public void NavigateToCanvas(int lessonId)

		{

			if (contentFrame.Content is IDisposable disposable)

			{

				try { disposable.Dispose(); } catch {}

			}

			txtPageTitle.Text = "🎨 Bảng vẽ — Bài giảng";

			var page = new CanvasPage(lessonId);

			contentFrame.Content = page;

			Log.Information("Navigate to Canvas with LessonID={Id}", lessonId);

		}



		/// <summary>Mở LessonRunner — 6-giai đoạn giảng dạy</summary>

		public void NavigateToRunner(int lessonId = 0)

		{

			if (contentFrame.Content is IDisposable disposable)

			{

				try { disposable.Dispose(); } catch {}

			}

			txtPageTitle.Text = "🎬 Tiến Trình Giảng Dạy";

			contentFrame.Content = new LessonRunnerPage(lessonId);

			Log.Information("Navigate to LessonRunner, LessonID={Id}", lessonId);

		}



		/// <summary>Mở Lesson History page</summary>

		public void NavigateToHistory(int lessonId = 0)

		{

			if (contentFrame.Content is IDisposable disposable)

			{

				try { disposable.Dispose(); } catch {}

			}

			txtPageTitle.Text = "🕐 Lịch Sử Phiên Bản";

			contentFrame.Content = new LessonHistoryPage(lessonId);

			Log.Information("Navigate to LessonHistory, LessonID={Id}", lessonId);

		}



		/// <summary>Điều hướng theo tên page thay vì form ID</summary>

		public void NavigateToPage(string pageName)

		{

			var id = pageName switch

			{

				"Dashboard"  => "F1",  "Lesson"	 => "F2",  "Editor"	=> "F3",

				"Classroom"  => "F4",  "Monitor"	=> "F5",  "Quiz"	  => "F6",

				"Report"	 => "F7",  "Settings"   => "F8",  "Library"   => "F9",

				"Group"	  => "F10", "Transfer"   => "F11", "Remote"	=> "F5",  // merged into Monitor

				"Messaging"  => "F13", "QuizBank"   => "F14", "Student"   => "F15S",

				"Roster"	 => "F15",

				"Broadcast"  => "F16", "Policy"	 => "F17", "Survey"	=> "F18",

				"Eventlog"   => "F19", "Canvas"	 => "F20", "Chart"	 => "F21",

				"Timer"	  => "F22", "Picker"	 => "F23", "Split"	 => "F24",

				"History"	=> "F25", "Timetable"  => "F26", "Attendance"=> "F27",

				"Homework"   => "F28", "AI"		 => "F29", "Website"   => "F30",

				"STEM"	   => "F31",

				"LearningTools" => "F32",

				"Teachers"   => "F33",

				"PracticalApps" => "F34",

				_ => pageName

			};

			NavigateTo(id);

		}



		// ═══════════════════════════════════════════════════════════

		//  TOPBAR NAVIGATION (BACK / HOME)

		// ═══════════════════════════════════════════════════════════



		private void GoBack_Click(object sender, RoutedEventArgs e)

		{

			if (_navigationStack.TryPop(out var prevPage))

			{

				Log.Information("Navigate BACK to: {PrevPage}", prevPage);

				NavigateTo(prevPage, addToStack: false);

				

				// Nếu sau khi lấy ra mà stack trống, ẩn nút Back

				if (_navigationStack.Count == 0 && btnGoBack != null)

				{

					btnGoBack.Visibility = Visibility.Collapsed;

				}

			}

		}



		private void GoHome_Click(object sender, RoutedEventArgs e)

		{

			Log.Information("Navigate HOME to F1");

			_navigationStack.Clear();

			NavigateTo("F1", addToStack: false);

			

			// Ẩn nút Back và nút Home vì đã ở trang chủ

			if (btnGoBack != null) btnGoBack.Visibility = Visibility.Collapsed;

			if (btnGoHome != null) btnGoHome.Visibility = Visibility.Collapsed;

		}



		/// <summary>

		/// Tạo placeholder cho form chưa implement

		/// </summary>

		private static Border CreatePlaceholder(string formId, string title)

		{

			var sp = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };

			sp.Children.Add(new TextBlock { Text = "🚧", FontSize = 48, HorizontalAlignment = HorizontalAlignment.Center });

			sp.Children.Add(new TextBlock { Text = title, FontSize = 20, FontWeight = FontWeights.SemiBold, 

				Foreground = System.Windows.Media.Brushes.Gray, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 12, 0, 4) });

			sp.Children.Add(new TextBlock { Text = $"{formId} — Đang phát triển...", FontSize = 13,

				Foreground = System.Windows.Media.Brushes.LightGray, HorizontalAlignment = HorizontalAlignment.Center });

			return new Border { Child = sp, Background = System.Windows.Media.Brushes.White };

		}



		/// <summary>

		/// Chuyển sang Smart Touch mode

		/// </summary>

		private void SwitchToScreen_Click(object sender, RoutedEventArgs e)

		{

			_modeService.GoToScreen();

		}



		/// <summary>

		/// Click badge HS → chuyển sang Monitor

		/// </summary>

		private void GoToMonitor_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)

		{

			NavigateTo("F5");

		}



		private void UpdateOnlineStudentCountUI()

		{

			if (txtOnlineStudentCount == null) return;

			string format = QASmartClass.Shared.LanguageManager.Get("Shell_OnlineCountFormat");

			txtOnlineStudentCount.Text = string.Format(format, _connectedStudentCount);

		}



		/// <summary>

		/// Cập nhật thông tin session từ ClassroomSessionService

		/// </summary>

		public void UpdateSessionInfo(string className, int studentCount)

		{

			txtClassName.Text = className;

			_connectedStudentCount = studentCount;

			txtStudentCount.Text = $"{studentCount} HS online";

			UpdateOnlineStudentCountUI();

		}



		// ═══════════════════════════════════════════════════════════

		//  ROSTER QUICK SELECTOR

		// ═══════════════════════════════════════════════════════════



		/// <summary>Click sidebar session info → mở popup chọn lớp nhanh</summary>

		private void RosterSelector_Click(object sender, MouseButtonEventArgs e)

		{

			try

			{

				var app = (QASmartTouch.App)Application.Current;

				var rosters = app.ClassRoster.GetAllRosters();



				if (!rosters.Any())

				{

					// Chưa có lớp → chuyển đến trang Student để tạo

					System.Windows.MessageBox.Show(

						"Chưa có danh sách lớp nào.\nVui lòng vào Quản lý Học sinh → Danh sách lớp để tạo.",

						"Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);

					NavigateTo("F15");

					return;

				}



				// Tạo popup chọn lớp

				var popup = new Window

				{

					Title = "📋 Chọn lớp",

					Width = 380, Height = 320,

					WindowStartupLocation = WindowStartupLocation.CenterOwner,

					Owner = this, ResizeMode = ResizeMode.NoResize,

					Background = Brushes.White

				};



				var sp = new StackPanel { Margin = new Thickness(16) };

				sp.Children.Add(new TextBlock

				{

					Text = "📋 Chọn lớp đang dạy",

					FontSize = 16, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10),

					Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33))

				});



				var activeRoster = app.ClassRoster.ActiveRoster;

				var lstBox = new ListBox { FontSize = 13, Height = 180 };

				foreach (var r in rosters)

				{

					var item = new ListBoxItem

					{

						Content = $"{r.ClassName} — {r.Subject} (GV {r.TeacherName})  |  {r.StudentCount} HS",

						Tag = r,

						IsSelected = activeRoster != null && activeRoster.Id == r.Id,

						Padding = new Thickness(8, 6, 8, 6)

					};

					lstBox.Items.Add(item);

				}

				sp.Children.Add(lstBox);



				var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };



				var btnManage = new Button

				{

					Content = "⚙️ Quản lý", Width = 100, Height = 32, FontSize = 12,

					Margin = new Thickness(0, 0, 8, 0), Cursor = Cursors.Hand

				};

				btnManage.Click += (s, e2) => { popup.Close(); NavigateTo("F15"); };



				var btnSelect = new Button

				{

					Content = "✅ Chọn lớp", Width = 110, Height = 32, FontSize = 12,

					Background = new SolidColorBrush(Color.FromRgb(25, 118, 210)),

					Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = Cursors.Hand

				};

				btnSelect.Click += (s, e2) =>

				{

					if (lstBox.SelectedItem is ListBoxItem selected && selected.Tag is Data.ClassRoster roster)

					{

						app.ClassRoster.SetActiveRoster(roster);

						UpdateActiveRosterUI(roster);

						popup.Close();

					}

				};



				btnPanel.Children.Add(btnManage);

				btnPanel.Children.Add(btnSelect);

				sp.Children.Add(btnPanel);



				popup.Content = sp;

				popup.ShowDialog();

			}

			catch (Exception ex)

			{

				Log.Warning("RosterSelector error: {Err}", ex.Message);

			}

		}



		/// <summary>Cập nhật UI sidebar khi chọn lớp mới</summary>

		public void UpdateActiveRosterUI(Data.ClassRoster roster)

		{

			txtClassName.Text = roster.DisplayName;

			txtStudentCount.Text = $"{roster.StudentCount} HS  |  {roster.SchoolYear} {roster.Semester}";

			Log.Information("Active roster UI → {Name}", roster.ClassName);

		}



		private void ExitApp_Click(object sender, RoutedEventArgs e)

		{

			if (QASmartClass.Services.VncBroadcastService.Instance.IsVncBroadcasting)
			{
				var vncResult = System.Windows.MessageBox.Show(
					"Hệ thống đang phát sóng VNC. Nếu thoát, luồng trình chiếu sẽ bị ngắt.\n\nBạn vẫn muốn thoát?",
					"Cảnh báo đang phát sóng",
					MessageBoxButton.YesNo,
					MessageBoxImage.Warning);

				if (vncResult != MessageBoxResult.Yes) return;

				QASmartClass.Services.VncBroadcastService.Instance.StopBroadcast();
			}

			var result = System.Windows.MessageBox.Show(

				"Bạn có chắc muốn thoát QA Smart Class?\n\nTất cả kết nối với học sinh sẽ bị ngắt.",

				"Xác nhận thoát",

				MessageBoxButton.YesNo,

				MessageBoxImage.Question);



			if (result == MessageBoxResult.Yes)

			{

				Log.Information("Application exit requested by user");

				Application.Current.Shutdown();

			}

		}

		private void OnVncBroadcastStatusChanged(object? sender, string status)
		{
			Dispatcher.Invoke(() =>
			{
				if (status == "STARTED")
				{
					brdBroadcastStatus.Visibility = Visibility.Visible;
					txtBroadcastTime.Text = $"🔴 Đang phát VNC ({QASmartClass.Services.VncBroadcastService.Instance.ElapsedTimeDisplay})";
				}
				else if (status == "STOPPED" || status == "STOPPED_UNEXPECTED")
				{
					brdBroadcastStatus.Visibility = Visibility.Collapsed;
					if (status == "STOPPED_UNEXPECTED")
					{
						ShowToast("Hệ thống", "Tiến trình trình chiếu VNC bị tắt bất ngờ!", "FEEDBACK");
					}
				}
			});
		}

		private void OnVncBroadcastTimerTick(object? sender, string timeDisplay)
		{
			Dispatcher.Invoke(() =>
			{
				txtBroadcastTime.Text = $"🔴 Đang phát VNC ({timeDisplay})";
			});
		}

		private async void brdBroadcastStatus_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (QASmartClass.Services.VncBroadcastService.Instance.IsVncBroadcasting)
			{
				bool confirm = await ShowAsyncConfirmDialog("Bạn có chắc chắn muốn dừng trình chiếu VNC?");
				if (confirm)
				{
					QASmartClass.Services.VncBroadcastService.Instance.StopBroadcast();
				}
			}
		}



		protected override void OnKeyDown(KeyEventArgs e)

		{

			base.OnKeyDown(e);



			// Hotkeys Ctrl + Alt + Key

			if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt)) == (ModifierKeys.Control | ModifierKeys.Alt))

			{

				switch (e.Key)

				{

					case Key.B:

						GoBack_Click(this, new RoutedEventArgs());

						e.Handled = true;

						break;

					case Key.H:

						GoHome_Click(this, new RoutedEventArgs());

						e.Handled = true;

						break;

					case Key.S:

						SwitchToStudent_Click(this, new RoutedEventArgs());

						e.Handled = true;

						break;

					case Key.T:

						SwitchToScreen_Click(this, new RoutedEventArgs());

						e.Handled = true;

						break;

				}

			}

		}



		/// <summary>

		/// Chuyển sang giao diện Học sinh

		/// </summary>

		private void SwitchToStudent_Click(object sender, RoutedEventArgs e)

		{

			try

			{

				var app = (QASmartTouch.App)Application.Current;

				app.ShowWhiteboard();
				Log.Information("Switched to Whiteboard from top bar");

			}

			catch (Exception ex)

			{

				Log.Warning("Switch to student error: {Err}", ex.Message);

				System.Windows.MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);

			}

		}



		private void btnAccessBack_Click(object sender, RoutedEventArgs e)

		{

			GoBack_Click(sender, e);

		}



		private void btnAccessHome_Click(object sender, RoutedEventArgs e)

		{

			GoHome_Click(sender, e);

		}



		private void btnAccessStudent_Click(object sender, RoutedEventArgs e)

		{

			SwitchToStudent_Click(sender, e);

		}



		private void btnAccessScreen_Click(object sender, RoutedEventArgs e)

		{

			SwitchToScreen_Click(sender, e);

		}



		// ═══════════════════════════════════════════════════════════

		//  TEACHER AVATAR MANAGEMENT

		// ═══════════════════════════════════════════════════════════



		/// <summary>Load teacher profile from DB and display avatar</summary>

		private void LoadTeacherProfile()

		{

			try

			{

				var app = (QASmartTouch.App)Application.Current;

				var profile = app.Database.TeacherProfiles.FirstOrDefault();

				if (profile == null)

				{

					profile = new Data.TeacherProfile

					{

						FullName = "Nguyễn Văn A",

						Subject = "Toán học",

						School = "Trường THPT QA",

						Title = "GV"

					};

					app.Database.TeacherProfiles.Add(profile);

					app.Database.SaveChanges();

				}



				txtTeacherName.Text = $"{profile.Title}. {profile.FullName}";

				txtTeacherSchool.Text = $"{profile.School}  •  {profile.Subject}";

				txtTeacherInitials.Text = GetInitials(profile.FullName);



				// Load avatar image if exists

				if (!string.IsNullOrEmpty(profile.AvatarPath) && File.Exists(profile.AvatarPath))

					SetAvatarImage(profile.AvatarPath);

			}

			catch (Exception ex) { Log.Warning("Load teacher profile error: {Err}", ex.Message); }

		}



		/// <summary>Click on teacher avatar to change photo</summary>

		private void TeacherAvatar_Click(object sender, MouseButtonEventArgs e)

		{

			try

			{

				var dlg = new OpenFileDialog

				{

					Title = "Chọn ảnh đại diện giáo viên",

					Filter = "Hình ảnh|*.jpg;*.jpeg;*.png;*.bmp;*.webp|Tất cả|*.*",

					InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)

				};



				if (dlg.ShowDialog() == true)

				{

					// Copy to app data

					var avatarDir = Path.Combine(

						QASmartClass.Services.AppPaths.RootDir, "Avatars");

					Directory.CreateDirectory(avatarDir);



					var ext = Path.GetExtension(dlg.FileName);

					var targetPath = Path.Combine(avatarDir, $"teacher_avatar{ext}");

					File.Copy(dlg.FileName, targetPath, true);



					// Update DB

					var app = (QASmartTouch.App)Application.Current;

					var profile = app.Database.TeacherProfiles.FirstOrDefault();

					if (profile != null)

					{

						profile.AvatarPath = targetPath;

						profile.UpdatedAt = DateTime.Now;

						app.Database.SaveChanges();

					}



					// Display

					SetAvatarImage(targetPath);

					Log.Information("Teacher avatar updated: {Path}", targetPath);

				}

			}

			catch (Exception ex)

			{

				Log.Warning("Teacher avatar change error: {Err}", ex.Message);

				System.Windows.MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);

			}

		}



		/// <summary>Display avatar image in the circular border</summary>

		private void SetAvatarImage(string path)

		{

			try

			{

				var bitmap = new BitmapImage();

				bitmap.BeginInit();

				bitmap.UriSource = new Uri(path, UriKind.Absolute);

				bitmap.CacheOption = BitmapCacheOption.OnLoad;

				bitmap.DecodePixelWidth = 80;

				bitmap.EndInit();



				imgTeacherAvatar.Source = bitmap;

				imgTeacherAvatar.Visibility = Visibility.Visible;

				txtTeacherInitials.Visibility = Visibility.Collapsed;

			}

			catch { /* fallback to initials */ }

		}



		/// <summary>Get initials from Vietnamese name (first char of last word + first char)</summary>

		private static string GetInitials(string fullName)

		{

			if (string.IsNullOrWhiteSpace(fullName)) return "?";

			var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

			if (parts.Length == 1) return parts[0][..1].ToUpper();

			return $"{parts[^1][..1]}{parts[0][..1]}".ToUpper();

		}



		// ═══════════════════════════════════════════════════════════

		//  TOAST NOTIFICATION SYSTEM

		// ═══════════════════════════════════════════════════════════



		/// <summary>Show a toast notification overlay (auto-dismiss after 8s)</summary>

		public void ShowToast(string studentName, string message, string type)

		{

			try

			{

				switch (type)

				{

					case "QUESTION":

						System.Media.SystemSounds.Beep.Play();

						break;

					case "HAND_RAISE":

						System.Media.SystemSounds.Hand.Play();

						break;

					case "FEEDBACK":

						System.Media.SystemSounds.Exclamation.Play();

						break;

					case "CHAT":

						System.Media.SystemSounds.Asterisk.Play();

						break;

					default:

						System.Media.SystemSounds.Asterisk.Play();

						break;

				}

			}

			catch (Exception ex)

			{

				Log.Warning("Failed to play toast sound: {Err}", ex.Message);

			}



			var icon = type switch

			{

				"QUESTION" => "❓",

				"HAND_RAISE" => "🖐️",

				"CHAT" => "💬",

				"FEEDBACK" => "🙋",

				_ => "🔔"

			};

			var title = type switch

			{

				"QUESTION" => "Câu hỏi từ học sinh",

				"HAND_RAISE" => "Giơ tay",

				"CHAT" => "Tin nhắn mới",

				"FEEDBACK" => "Báo lỗi hiển thị",

				_ => "Thông báo"

			};

			var accentColor = type switch

			{

				"QUESTION" => "#1565C0",

				"HAND_RAISE" => "#E65100",

				"CHAT" => "#2E7D32",

				"FEEDBACK" => "#D32F2F",

				_ => "#1976D2"

			};

			var bgColor = type switch

			{

				"QUESTION" => "#E3F2FD",

				"HAND_RAISE" => "#FFF3E0",

				"CHAT" => "#E8F5E9",

				"FEEDBACK" => "#FFEBEE",

				_ => "#F5F5F5"

			};



			var toast = new Border

			{

				Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgColor)),

				BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(accentColor)),

				BorderThickness = new Thickness(0, 0, 0, 3),

				CornerRadius = new CornerRadius(10),

				Margin = new Thickness(0, 0, 0, 8),

				Padding = new Thickness(14, 10, 14, 10),

				Cursor = Cursors.Hand,

				Opacity = 0,

				Effect = new DropShadowEffect { BlurRadius = 16, ShadowDepth = 4, Opacity = 0.2, Color = Colors.Black, Direction = 270 }

			};



			var grid = new Grid();

			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });



			// Icon

			var iconTb = new TextBlock { Text = icon, FontSize = 24, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };

			Grid.SetColumn(iconTb, 0);

			grid.Children.Add(iconTb);



			// Content

			var contentSp = new StackPanel();

			contentSp.Children.Add(new TextBlock

			{

				Text = $"{title} — {studentName}",

				FontSize = 12, FontWeight = FontWeights.SemiBold,

				Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(accentColor))

			});

			contentSp.Children.Add(new TextBlock

			{

				Text = message.Length > 80 ? message.Substring(0, 80) + "..." : message,

				FontSize = 11, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#424242")),

				TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 0, 0)

			});

			contentSp.Children.Add(new TextBlock

			{

				Text = DateTime.Now.ToString("HH:mm:ss"),

				FontSize = 9, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9E9E9E")),

				Margin = new Thickness(0, 3, 0, 0)

			});

			Grid.SetColumn(contentSp, 1);

			grid.Children.Add(contentSp);



			// Close button

			var closeBtn = new Button

			{

				Content = "✕", FontSize = 12, Background = Brushes.Transparent,

				Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9E9E9E")),

				BorderThickness = new Thickness(0), Cursor = Cursors.Hand,

				VerticalAlignment = VerticalAlignment.Top, Padding = new Thickness(4, 0, 0, 0)

			};

			closeBtn.Click += (s, e) => DismissToast(toast);

			Grid.SetColumn(closeBtn, 2);

			grid.Children.Add(closeBtn);



			toast.Child = grid;



			// Click toast → go to Messaging

			toast.MouseLeftButtonDown += (s, e) =>

			{

				DismissToast(toast);

				NavigateTo("F13");

			};



			// Add to panel (max 4 toasts)

			toastPanel.Children.Insert(0, toast);

			while (toastPanel.Children.Count > 4)

				toastPanel.Children.RemoveAt(toastPanel.Children.Count - 1);



			// Fade in

			var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };

			toast.BeginAnimation(OpacityProperty, fadeIn);



			// Auto-dismiss after 8 seconds

			var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };

			timer.Tick += (s, e) => { timer.Stop(); DismissToast(toast); };

			timer.Start();



			Log.Information("Toast: {Title} — {Student}: {Msg}", title, studentName, message);

		}



		/// <summary>Fade out and remove a toast</summary>

		private void DismissToast(Border toast)

		{

			if (!toastPanel.Children.Contains(toast)) return;

			var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));

			fadeOut.Completed += (s, e) =>

			{

				if (toastPanel.Children.Contains(toast))

					toastPanel.Children.Remove(toast);

			};

			toast.BeginAnimation(OpacityProperty, fadeOut);

		}



		private void IncrementBadge()

		{

			if (_currentPage == "F13") return;

			_unreadCount++;

			SetBadgeCount(btnMessaging, _unreadCount); // Gán Attached Property động

		}



		/// <summary>Reset unread badge</summary>

		private void ResetBadge()

		{

			_unreadCount = 0;

			SetBadgeCount(btnMessaging, 0);

		}



		private void ToggleSection_Click(object sender, MouseButtonEventArgs e)

		{

			if (sender is Border headerBorder && headerBorder.Tag is string secId)

			{

				var panel = this.FindName("panel" + secId) as StackPanel;

				var textToggle = this.FindName("txtToggle" + secId) as TextBlock;

				if (panel != null)

				{

					if (panel.Visibility == Visibility.Visible)

					{

						panel.Visibility = Visibility.Collapsed;

						if (textToggle != null) textToggle.Text = "+";

					}

					else

					{

						panel.Visibility = Visibility.Visible;

						if (textToggle != null) textToggle.Text = "-";

					}

				}

			}

		}



		private bool _isSidebarCollapsed = false;



		private void ToggleSidebar_Click(object sender, RoutedEventArgs e)

		{

			_isSidebarCollapsed = !_isSidebarCollapsed;

			IsSidebarCollapsed = _isSidebarCollapsed;



			// Toggle width with animation

			double fromWidth = _isSidebarCollapsed ? 255 : 65;

			double toWidth = _isSidebarCollapsed ? 65 : 255;



			var widthAnim = new GridLengthAnimation

			{

				From = new GridLength(fromWidth),

				To = new GridLength(toWidth),

				Duration = TimeSpan.FromMilliseconds(250),

				EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }

			};

			sidebarColumn.BeginAnimation(ColumnDefinition.WidthProperty, widthAnim);



			if (_isSidebarCollapsed)

			{

				BorderSec1.Visibility = Visibility.Collapsed;

				BorderSec2.Visibility = Visibility.Collapsed;

				BorderSec3.Visibility = Visibility.Collapsed;

				BorderSec4.Visibility = Visibility.Collapsed;

				BorderSec5.Visibility = Visibility.Collapsed;

				BorderSec6.Visibility = Visibility.Collapsed;

				SepSec2.Visibility = Visibility.Collapsed;

				SepSec3.Visibility = Visibility.Collapsed;

				SepSec4.Visibility = Visibility.Collapsed;

				SepSec5.Visibility = Visibility.Collapsed;

				SepSec6.Visibility = Visibility.Collapsed;

				SepHome.Visibility = Visibility.Collapsed;

				SepAccess.Visibility = Visibility.Collapsed;



				var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));

				fadeOut.Completed += (s, ev) =>

				{

					if (_isSidebarCollapsed)

					{

						logoTextPanel.Visibility = Visibility.Collapsed;

						sessionDropdownArrow.Visibility = Visibility.Collapsed;

						txtClassName.Visibility = Visibility.Collapsed;

						sessionDetailsPanel.Visibility = Visibility.Collapsed;

						txtTeacherName.Visibility = Visibility.Collapsed;

						txtTeacherSchool.Visibility = Visibility.Collapsed;

						accessibilityPanel.Visibility = Visibility.Collapsed;

					}

				};



				logoTextPanel.BeginAnimation(UIElement.OpacityProperty, fadeOut);

				txtClassName.BeginAnimation(UIElement.OpacityProperty, fadeOut);

				sessionDropdownArrow.BeginAnimation(UIElement.OpacityProperty, fadeOut);

				sessionDetailsPanel.BeginAnimation(UIElement.OpacityProperty, fadeOut);

				txtTeacherName.BeginAnimation(UIElement.OpacityProperty, fadeOut);

				txtTeacherSchool.BeginAnimation(UIElement.OpacityProperty, fadeOut);

				accessibilityPanel.BeginAnimation(UIElement.OpacityProperty, fadeOut);

			}

			else

			{

				BorderSec1.Visibility = Visibility.Visible;

				BorderSec2.Visibility = Visibility.Visible;

				BorderSec3.Visibility = Visibility.Visible;

				BorderSec4.Visibility = Visibility.Visible;

				BorderSec5.Visibility = Visibility.Visible;

				BorderSec6.Visibility = Visibility.Visible;

				SepSec2.Visibility = Visibility.Visible;

				SepSec3.Visibility = Visibility.Visible;

				SepSec4.Visibility = Visibility.Visible;

				SepSec5.Visibility = Visibility.Visible;

				SepSec6.Visibility = Visibility.Visible;

				SepHome.Visibility = Visibility.Visible;

				SepAccess.Visibility = Visibility.Visible;



				logoTextPanel.Visibility = Visibility.Visible;

				sessionDropdownArrow.Visibility = Visibility.Visible;

				txtClassName.Visibility = Visibility.Visible;

				sessionDetailsPanel.Visibility = Visibility.Visible;

				txtTeacherName.Visibility = Visibility.Visible;

				txtTeacherSchool.Visibility = Visibility.Visible;

				accessibilityPanel.Visibility = Visibility.Visible;



				logoTextPanel.Opacity = 0;

				sessionDropdownArrow.Opacity = 0;

				txtClassName.Opacity = 0;

				sessionDetailsPanel.Opacity = 0;

				txtTeacherName.Opacity = 0;

				txtTeacherSchool.Opacity = 0;

				accessibilityPanel.Opacity = 0;



				var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250))

				{

					EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }

				};



				logoTextPanel.BeginAnimation(UIElement.OpacityProperty, fadeIn);

				sessionDropdownArrow.BeginAnimation(UIElement.OpacityProperty, fadeIn);

				txtClassName.BeginAnimation(UIElement.OpacityProperty, fadeIn);

				sessionDetailsPanel.BeginAnimation(UIElement.OpacityProperty, fadeIn);

				txtTeacherName.BeginAnimation(UIElement.OpacityProperty, fadeIn);

				txtTeacherSchool.BeginAnimation(UIElement.OpacityProperty, fadeIn);

				accessibilityPanel.BeginAnimation(UIElement.OpacityProperty, fadeIn);



				for (int i = 1; i <= 6; i++)

				{

					var panel = this.FindName($"panelSec{i}") as StackPanel;

					var toggleText = this.FindName($"txtToggleSec{i}") as TextBlock;

					if (panel != null && toggleText != null)

					{

						panel.Visibility = (toggleText.Text == "-") ? Visibility.Visible : Visibility.Collapsed;

					}

				}

			}

		}



		private System.Collections.Generic.List<Button> GetNavButtons()

		{

			var buttons = new System.Collections.Generic.List<Button>();

			foreach (var child in navPanel.Children)

			{

				if (child is StackPanel subPanel)

				{

					foreach (var subChild in subPanel.Children)

					{

						if (subChild is Button btn)

						{

							buttons.Add(btn);

						}

					}

				}

				else if (child is Button btn)

				{

					buttons.Add(btn);

				}

			}

			return buttons;

		}



		private string GetResourceKeyForTag(string tag)

		{

			return tag switch

			{

				"F1" => "Nav_Home",

				"F26" => "Nav_Timetable",

				"F2" => "Nav_Lessons",

				"F3" => "Nav_LessonEditor",

				"F9" => "Nav_Library",

				"F15" => "Nav_Roster",

				"F15S" => "Nav_Students",

				"F33" => "Nav_Teachers",

				"F27" => "Nav_Attendance",

				"F10" => "Nav_Groups",

				"F4" => "Nav_Classroom",

				"F5" => "Nav_Monitor",

				"F16" => "Nav_Broadcast",

				"F11" => "Nav_FileTransfer",

				"F30" => "Nav_WebPush",

				"F13" => "Nav_Messaging",

				"F6" => "Nav_Quiz",

				"F14" => "Nav_QuestionBank",

				"F18" => "Nav_Survey",

				"F28" => "Nav_Homework",

				"F7" => "Nav_Report",

				"F20" => "Nav_Canvas",

				"F22" => "Nav_Timer",

				"F23" => "Nav_RandomPicker",

				"F31" => "Nav_STEM",

				"F29" => "Nav_AI",

				"F32" => "Nav_LearningTools",

				"F8" => "Nav_Settings",

				"F17" => "Nav_Policy",

				"F19" => "Nav_EventLog",

				_ => null

			};

		}



		private string ExtractEmoji(string text)

		{

			if (string.IsNullOrEmpty(text)) return "";

			var parts = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

			if (parts.Length >= 2)

			{

				return parts[1];

			}

			return text;

		}



		private System.Diagnostics.Stopwatch _logoStopwatch = new System.Diagnostics.Stopwatch();



		private void Logo_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)

		{

			_logoStopwatch.Restart();

		}



		private void Logo_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)

		{

			_logoStopwatch.Stop();

			if (_logoStopwatch.ElapsedMilliseconds >= 3000)

			{

				try

				{

					Log.Information("SECURITY_AUDIT: System Diagnostics Window opened via logo long-press (3s).");

					var diagWin = new QASmartTouch.Controls.SystemDiagnosticsWindow();

					diagWin.Owner = this;

					diagWin.ShowDialog();

				}

				catch (Exception ex)

				{

					Log.Error(ex, "Failed to open diagnostics window");

				}

			}

		}



		// 1. Dependency Property để liên kết trạng thái Sidebar sang XAML

		public static readonly DependencyProperty IsSidebarCollapsedProperty =

			DependencyProperty.Register("IsSidebarCollapsed", typeof(bool), typeof(ClassroomShell), new PropertyMetadata(false));



		public bool IsSidebarCollapsed

		{

			get => (bool)GetValue(IsSidebarCollapsedProperty);

			set => SetValue(IsSidebarCollapsedProperty, value);

		}



		// 2. Attached Property quản lý số lượng Badge trên mỗi Button

		public static readonly DependencyProperty BadgeCountProperty =

			DependencyProperty.RegisterAttached("BadgeCount", typeof(int), typeof(ClassroomShell), new PropertyMetadata(0));



		public static int GetBadgeCount(DependencyObject obj) => (int)obj.GetValue(BadgeCountProperty);

		public static void SetBadgeCount(DependencyObject obj, int value) => obj.SetValue(BadgeCountProperty, value);

	}



	public class GridLengthAnimation : System.Windows.Media.Animation.AnimationTimeline

	{

		public override Type TargetPropertyType => typeof(GridLength);



		protected override System.Windows.Freezable CreateInstanceCore() => new GridLengthAnimation();



		public static readonly DependencyProperty FromProperty = DependencyProperty.Register(

			"From", typeof(GridLength), typeof(GridLengthAnimation));



		public GridLength From

		{

			get => (GridLength)GetValue(FromProperty);

			set => SetValue(FromProperty, value);

		}



		public static readonly DependencyProperty ToProperty = DependencyProperty.Register(

			"To", typeof(GridLength), typeof(GridLengthAnimation));



		public GridLength To

		{

			get => (GridLength)GetValue(ToProperty);

			set => SetValue(ToProperty, value);

		}



		public static readonly DependencyProperty EasingFunctionProperty = DependencyProperty.Register(

			"EasingFunction", typeof(IEasingFunction), typeof(GridLengthAnimation));



		public IEasingFunction EasingFunction

		{

			get => (IEasingFunction)GetValue(EasingFunctionProperty);

			set => SetValue(EasingFunctionProperty, value);

		}



		public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, System.Windows.Media.Animation.AnimationClock animationClock)

		{

			double fromVal = From.Value;

			double toVal = To.Value;



			if (animationClock.CurrentProgress == null)

				return From;



			double progress = animationClock.CurrentProgress.Value;

			if (EasingFunction != null)

			{

				progress = EasingFunction.Ease(progress);

			}

			double val = fromVal + (toVal - fromVal) * progress;

			return new GridLength(val, GridUnitType.Star == To.GridUnitType || GridUnitType.Star == From.GridUnitType ? GridUnitType.Star : GridUnitType.Pixel);

		}

	}



	// 3. Converter trích xuất Icon/Emoji từ chuỗi nội dung gốc

	public class SidebarIconConverter : System.Windows.Data.IValueConverter

	{

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)

		{

			if (value is string text)

			{

				var parts = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

				if (parts.Length >= 2)

				{

					if (parts[0].Contains(".") || double.TryParse(parts[0], out _))

					{

						return parts[1];

					}

					return parts[0];

				}

				else if (parts.Length == 1)

				{

					return parts[0];

				}

			}

			return "";

		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();

	}



	// 4. Converter trích xuất Text Label từ chuỗi nội dung gốc

	public class SidebarTextConverter : System.Windows.Data.IValueConverter

	{

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)

		{

			if (value is string text)

			{

				var parts = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

				if (parts.Length >= 2)

				{

					if (parts[0].Contains(".") || double.TryParse(parts[0], out _))

					{

						return parts[0] + " " + string.Join(" ", parts.Skip(2));

					}

					return string.Join(" ", parts.Skip(1));

				}

				return text;

			}

			return "";

		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();

	}



	// 5. Converter hiển thị/ẩn Badge dựa trên giá trị int (>0 hiển thị)

	public class IntToVisibilityConverter : System.Windows.Data.IValueConverter

	{

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)

		{

			if (value is int count && count > 0) return Visibility.Visible;

			return Visibility.Collapsed;

		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();

	}

}

