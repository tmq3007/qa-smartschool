using System;

using System.CodeDom.Compiler;

using System.Collections.Generic;

using System.ComponentModel;

using System.Diagnostics;

using System.Globalization;

using System.IO;

using System.Linq;

using System.Runtime.InteropServices;

using System.Text.Json;

using System.Threading.Tasks;

using System.Windows;

using System.Windows.Controls;

using System.Windows.Data;

using System.Windows.Ink;

using System.Windows.Input;

using System.Windows.Interop;

using System.Windows.Markup;

using System.Windows.Media;

using System.Windows.Media.Animation;

using System.Windows.Media.Effects;

using System.Windows.Media.Imaging;

using System.Windows.Threading;

using Microsoft.Win32;

using QASmartClass.Classroom.Services;

using QASmartClass.Data;

using QASmartClass.LearningTools.Models;

using QASmartClass.LearningTools.Views;

using QASmartClass.LearningTools.Views.Language;

using QASmartClass.Services;

using QASmartClass.LearningTools;

using QASmartClass.StudentClient.Services;

using QASmartClass.StudentClient.Views;

using QASmartTouch;

using QASmartTouch.Services;

using Serilog;

using QASmartClass.StudentClient.Models;



namespace QASmartClass.StudentClient.Views

{



	public partial class StudentShell : Window

	{





		private static class NativeMethods

		{

			[DllImport("user32.dll")]

			public static extern nint GetDC(nint hWnd);



			[DllImport("user32.dll")]

			public static extern int ReleaseDC(nint hWnd, nint hDC);



			[DllImport("gdi32.dll")]

			public static extern nint CreateCompatibleDC(nint hdc);



			[DllImport("gdi32.dll")]

			public static extern nint CreateCompatibleBitmap(nint hdc, int nWidth, int nHeight);



			[DllImport("gdi32.dll")]

			public static extern nint SelectObject(nint hdc, nint hgdiobj);



			[DllImport("gdi32.dll")]

			public static extern bool BitBlt(nint hdcDest, int xDest, int yDest, int wDest, int hDest, nint hdcSrc, int xSrc, int ySrc, int rop);



			[DllImport("gdi32.dll")]

			public static extern bool DeleteObject(nint hObject);



			[DllImport("gdi32.dll")]

			public static extern bool DeleteDC(nint hdc);



			[DllImport("user32.dll")]

			[return: MarshalAs(UnmanagedType.Bool)]

			public static extern bool SetForegroundWindow(nint hWnd);

		}



		private readonly DispatcherTimer _clockTimer;



		private string _currentPage = "S1";



		private bool _isSidebarCollapsed = false;



		private DateTime _lastQuickRaiseTime = DateTime.MinValue;

		private bool _isResettingNetwork = false;



		private bool _isExitPinRequired = false;



		private string _exitPinCode = "";



		private int _failedPinAttempts = 0;



		private DateTime? _lockoutEndTime = null;



		private bool _isCloseAuthorized = false;



		private int _currentTeacherWebPort = 80;



		private string _currentBroadcastToken = "";



		private int _stuUnreadCount = 0;



		private StudentChatPage? _chatPage;



		private Grid? _toolFocusOverlay;



		private string _lastCommandHash = "";



		private DateTime _lastCommandTime = DateTime.MinValue;



		private static readonly TimeSpan CommandDedupWindow = TimeSpan.FromSeconds(2.0);



		private Grid? _surveyOverlay;



		private DispatcherTimer? _surveyCountdownTimer;



		private Grid? _quizReviewOverlay;



		private Grid? _broadcastOverlay;



		// === UPGRADE_01: Kiosk Mode variables ===

		private System.Windows.Threading.DispatcherTimer? _kioskRefocusTimer;

		private KioskConfig? _activeKioskConfig;

		private int _consecutiveHttpPullFailures = 0;

		// === UPGRADE_04: Hard Timeout Anchor ===

		private DateTime _overlayCreatedTime = DateTime.MinValue;

		private const int HARD_TIMEOUT_SECONDS = 15;

		// === UPGRADE_08: Pre-check OpenCV native DLL cho WebP decode ===
		private static readonly bool _isOpenCvAvailable = CheckOpenCvAvailability();
		private static bool CheckOpenCvAvailability()
		{
			try
			{
				string baseDir = AppDomain.CurrentDomain.BaseDirectory;
				if (System.IO.Directory.GetFiles(baseDir, "OpenCvSharpExtern.dll").Length > 0) return true;
				if (System.IO.Directory.GetFiles(baseDir, "opencv_world*.dll").Length > 0) return true;
				Serilog.Log.Warning("[Broadcast] UPGRADE_08: OpenCV native DLL không tìm thấy tại {Dir}. WebP decode sẽ bị disable, fallback sang JPEG/PNG.", baseDir);
				return false;
			}
			catch (Exception ex)
			{
				Serilog.Log.Error("[Broadcast] UPGRADE_08: Lỗi khi kiểm tra OpenCV: {Err}", ex.Message);
				return false;
			}
		}

		// === UPGRADE_13: Cờ trạng thái chế độ truyền phát siêu tốc ===
		private bool _isTurboModeActive = false;
		private Border? _turboNeonBorder;
		private string _turboStudentBg = "FlatBlack";

		private Image? _broadcastImage;



		private InkCanvas? _broadcastInkCanvas;
		private Canvas? _broadcastShapeCanvas;



		private TextBlock? _broadcastTimeText; private TextBlock? _broadcastTitleText;

		private StackPanel? _broadcastConnectionStatus;

		private TextBlock? _broadcastStatusText;

		private DateTime _broadcastStartTime = DateTime.Now;

		// V22-10: Font nhất quán cho overlay

		private static readonly FontFamily _broadcastFont = new FontFamily("Segoe UI");

		// V22-17: Static HttpClient tránh socket exhaustion

		private static readonly System.Net.Http.HttpClient _sharedHttpClient = new()

		{

			Timeout = TimeSpan.FromSeconds(5)

		};



		private Grid? _fileBroadcastOverlay;



		private Grid? _noticeOverlay;



		private bool _isWebBlocked = false;

		private bool _isWebWhitelistActive = false;

		private bool _isQuizFocusActive = false;

		private bool _isFocusModeActive = false;

		private readonly System.Collections.Generic.List<string> _allowedUrls = new();

		private SecureWebWindow? _secureWebWindow;

		private DispatcherTimer? _webBlockTimer;

		private DateTime _lastBrowserWarningTime = DateTime.MinValue;

		private DateTime _lastBlockNotificationTime = DateTime.MinValue;



		private Grid? _lockOverlay;



		private Grid? _screenCastOverlay;



		private Grid? _silenceOverlay;



		private Grid? _warningOverlay;



		private DispatcherTimer? _warningTimer;



		private Grid? _classTimerOverlay;

		private DispatcherTimer? _classTimer;



		private string? _lastSectionFocusToolId;



		private UserControl? _lastSectionFocusTool;



		private Border? _lastHighlightedBorder;



		private Brush? _lastHighlightedOrigBrush;



		private Thickness _lastHighlightedOrigThickness;



		private DateTime _lastFocusHeartbeatTime = DateTime.MinValue;



		private System.Windows.Threading.DispatcherTimer? _focusWatchdogTimer;



		private bool _isBroadcastForceWatch = false; private bool _isBroadcastPausedByTeacher = false; private string _currentLessonTitle = "";
		private string _currentBroadcastProtocol = "UDP_MULTICAST";

		private Button? _broadcastCloseButton;

		private DateTime _lastBroadcastUpdateTime = DateTime.MinValue;

		private DateTime _lastHttpPullTime = DateTime.MinValue;

		private System.Windows.Threading.DispatcherTimer? _broadcastWatchdogTimer;

		private double _broadcastHeartbeatTimeout = 15.0; // === PHASE6-GD1 + UPGRADE_07: Giảm 30→15s cho môi trường lớp học ===

		private bool _userDismissedBroadcast = false;
		private System.Diagnostics.Process? _vncClientProcess = null;
		private bool _isVncClientRunning = false;
		private System.Windows.Threading.DispatcherTimer? _vncWatchdogTimer = null;
		private string _vncServerIp = "";
		private int _vncServerPort = 5901;
		private string _vncSessionCode = "";
		private bool _vncForceWatch = false;

		private DateTime _lastTcpCommandTime = DateTime.MinValue; // === PHASE6-GD1 ===

		private DateTime _pauseStartTime = DateTime.MinValue; // === PHASE6.1-BUG2 ===

		private TextBlock? _broadcastWarningTextBlock;

		private DateTime _lastAutoRecoveryTime = DateTime.MinValue;

		private System.Windows.Media.Animation.Storyboard? _pulseStoryboard;



		public StudentShell()

		{

			InitializeComponent();

			this.Closing += StudentShell_Closing;

			contentFrame.Navigated += (s, e) =>

			{

				if (contentFrame.Content is Page activePage)

				{

					var method = activePage.GetType().GetMethod("ApplyTheme", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

					method?.Invoke(activePage, null);

				}



				bool isUnitTest = AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName?.StartsWith("xunit", StringComparison.OrdinalIgnoreCase) ?? false);

				if (isUnitTest)

				{

					UpdateDynamicBadges();

					return;

				}



				try

				{

					var fadeAnim = new DoubleAnimation

					{

						From = 0.0,

						To = 1.0,

						Duration = new Duration(TimeSpan.FromMilliseconds(200)),

						DecelerationRatio = 0.8

					};



					var translateAnim = new DoubleAnimation

					{

						From = 15.0,

						To = 0.0,

						Duration = new Duration(TimeSpan.FromMilliseconds(200)),

						DecelerationRatio = 0.8

					};



					var transform = new TranslateTransform();

					contentFrame.RenderTransform = transform;

					contentFrame.RenderTransformOrigin = new Point(0.5, 0.5);



					var sb = new Storyboard();

					Storyboard.SetTarget(fadeAnim, contentFrame);

					Storyboard.SetTargetProperty(fadeAnim, new PropertyPath("Opacity"));



					Storyboard.SetTarget(translateAnim, contentFrame);

					Storyboard.SetTargetProperty(translateAnim, new PropertyPath("RenderTransform.(TranslateTransform.Y)"));



					sb.Children.Add(fadeAnim);

					sb.Children.Add(translateAnim);

					sb.Begin();

				}

				catch (Exception ex)

				{

					Log.Warning("Error playing page transition animation: {Err}", ex.Message);

				}



				UpdateDynamicBadges();

			};

			_clockTimer = new DispatcherTimer

			{

				Interval = TimeSpan.FromSeconds(1.0)

			};

			_clockTimer.Tick += delegate

			{

				txtClock.Text = DateTime.Now.ToString("HH:mm");

			};

			_clockTimer.Start();

			txtClock.Text = DateTime.Now.ToString("HH:mm");

			_webBlockTimer = new DispatcherTimer

			{

				Interval = TimeSpan.FromSeconds(1.5)

			};

			_webBlockTimer.Tick += (s, ev) => EnforceWebPolicy();

			_webBlockTimer.Start();

			base.Loaded += async delegate

		{

			LoadSavedExitPinConfig();

			NavigateTo("S1");

			LoadStudentDataFromDB();

			LoadTeacherInfo();

			LoadSavedAvatar();

			await StartStudentNetworkAsync();

			SyncWithTeacherState();

			CleanupCacheDirectory();



			if (QASmartClass.Data.AppDbContext.FallbackInMemoryConnection != null)

			{

				ramDbWarningBanner.Visibility = Visibility.Visible;

			}



			// Start global file transfer listener

			try

			{

				var app = (QASmartTouch.App)System.Windows.Application.Current;

				var sft = app?.StudentFileTransfer;

				if (sft != null)

				{

					sft.FileReceived -= Global_OnFileReceived;

					sft.FileReceived += Global_OnFileReceived;



					if (!sft.IsListening)

					{

						_ = Task.Run(async () =>

						{

							try { await sft.StartListeningAsync(); }

							catch (Exception ex) { Log.Warning("Global StartListeningAsync error: {Err}", ex.Message); }

						});

					}

				}

			}

			catch (Exception exSft) { Log.Warning("Global StartFileListener error: {Err}", exSft.Message); }

		};

			base.Activated += (sender, e) =>

			{

				SyncWithTeacherState();

				if (_broadcastOverlay != null && _isBroadcastForceWatch && !_isBroadcastPausedByTeacher)

				{

					base.Topmost = true;

				}

			};

			base.Deactivated += StudentShell_Deactivated;

			PreviewKeyDown += StudentShell_PreviewKeyDown;

			InitFocusWatchdogTimer();

			InitBroadcastWatchdogTimer();

			App? appInstance = Application.Current as App;
			if (appInstance?.ClassRoster != null)
			{
				appInstance.ClassRoster.ActiveRosterChanged += OnActiveRosterChanged;
			}
			
			Log.Information("StudentShell initialized");

		}



		private void OnActiveRosterChanged(object? sender, QASmartClass.Data.ClassRoster? roster)
		{
			base.Dispatcher.Invoke(delegate
			{
				try
				{
					if (roster != null)
					{
						txtClassInfo.Text = roster.ClassName + " — " + roster.TeacherName;
						txtTeacherInfo.Text = "GV: " + roster.TeacherName;
						
						if (contentFrame.Content is StudentLessonPage studentLessonPage)
						{
							studentLessonPage.LoadTodaysLesson();
						}
					}
					else
					{
						txtClassInfo.Text = "Chờ giáo viên bắt đầu...";
						txtTeacherInfo.Text = "GV: --";
					}
				}
				catch (Exception ex)
				{
					Log.Warning("OnActiveRosterChanged error: {Err}", ex.Message);
				}
			});
		}

		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			try
			{
				App? appInstance = Application.Current as App;
				if (appInstance?.ClassRoster != null)
				{
					appInstance.ClassRoster.ActiveRosterChanged -= OnActiveRosterChanged;
				}
				// Hủy đăng ký FrameReceived của UdpScreenBroadcastService.Instance để tránh rò rỉ bộ nhớ
				QASmartClass.Services.UdpScreenBroadcastService.Instance.FrameReceived -= OnUdpFrameReceived;
			}
			catch (Exception ex)
			{
				Log.Warning("Error unsubscribing ActiveRosterChanged or FrameReceived: {Err}", ex.Message);
			}
		}

		public static bool IsDarkMode { get; set; } = false;



		private void ThemeToggle_Click(object sender, RoutedEventArgs e)

		{

			IsDarkMode = !IsDarkMode;

			ApplyTheme();



			if (contentFrame.Content is Page activePage)

			{

				var method = activePage.GetType().GetMethod("ApplyTheme", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

				method?.Invoke(activePage, null);

			}

		}



		public void ApplyTheme()

		{

			try

			{

				if (IsDarkMode)

				{

					btnThemeToggle.Content = "LIGHT";

					btnThemeToggle.Foreground = new SolidColorBrush(Color.FromRgb(250, 204, 21));

					topBarBorder.Background = new SolidColorBrush(Color.FromRgb(30, 41, 59));

					topBarBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(55, 65, 81));

					txtPageTitle.Foreground = new SolidColorBrush(Color.FromRgb(243, 244, 246));

					txtClock.Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175));

					mainContentAreaGrid.Background = new SolidColorBrush(Color.FromRgb(15, 23, 42));



					// Dark Theme Sidebar Resources

					this.Resources["SidebarBg"] = new LinearGradientBrush(

						Color.FromRgb(11, 25, 41),

						Color.FromRgb(17, 34, 64),

						90.0

					);

					this.Resources["SidebarNavFg"] = new SolidColorBrush(Color.FromRgb(203, 213, 225));

					this.Resources["SidebarBottomBg"] = new SolidColorBrush(Color.FromRgb(8, 15, 26));

					this.Resources["SidebarSecHeaderBg"] = new SolidColorBrush(Color.FromRgb(15, 40, 71));

					this.Resources["SidebarSecHeaderFgChinh"] = new SolidColorBrush(Color.FromRgb(96, 165, 250));

					this.Resources["SidebarSecHeaderFgHocTap"] = new SolidColorBrush(Color.FromRgb(74, 222, 128));

					this.Resources["SidebarSecHeaderFgTuongTac"] = new SolidColorBrush(Color.FromRgb(244, 114, 182));

					this.Resources["SidebarSecHeaderFgCaNhan"] = new SolidColorBrush(Color.FromRgb(148, 163, 184));

					this.Resources["SidebarSepBg"] = new SolidColorBrush(Color.FromRgb(26, 48, 80));

					this.Resources["SidebarLogoFg"] = Brushes.White;

					this.Resources["SidebarToggleFg"] = new SolidColorBrush(Color.FromRgb(100, 116, 139));

				}

				else

				{

					btnThemeToggle.Content = "DARK";

					btnThemeToggle.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));

					topBarBorder.Background = Brushes.White;

					topBarBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235));

					txtPageTitle.Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59));

					txtClock.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));

					mainContentAreaGrid.Background = new SolidColorBrush(Color.FromRgb(240, 242, 245));



					// Light Theme Sidebar Resources

					this.Resources["SidebarBg"] = new LinearGradientBrush(

						Color.FromRgb(248, 250, 252),

						Color.FromRgb(241, 245, 249),

						90.0

					);

					this.Resources["SidebarNavFg"] = new SolidColorBrush(Color.FromRgb(51, 65, 85));

					this.Resources["SidebarBottomBg"] = new SolidColorBrush(Color.FromRgb(226, 232, 240));

					this.Resources["SidebarSecHeaderBg"] = new SolidColorBrush(Color.FromRgb(226, 232, 240));

					this.Resources["SidebarSecHeaderFgChinh"] = new SolidColorBrush(Color.FromRgb(29, 78, 216));

					this.Resources["SidebarSecHeaderFgHocTap"] = new SolidColorBrush(Color.FromRgb(21, 128, 61));

					this.Resources["SidebarSecHeaderFgTuongTac"] = new SolidColorBrush(Color.FromRgb(190, 24, 93));

					this.Resources["SidebarSecHeaderFgCaNhan"] = new SolidColorBrush(Color.FromRgb(71, 85, 105));

					this.Resources["SidebarSepBg"] = new SolidColorBrush(Color.FromRgb(203, 213, 225));

					this.Resources["SidebarLogoFg"] = new SolidColorBrush(Color.FromRgb(15, 23, 42));

					this.Resources["SidebarToggleFg"] = new SolidColorBrush(Color.FromRgb(71, 85, 105));

				}

			}

			catch (Exception ex)

			{

				Log.Warning("ApplyTheme error: {Err}", ex.Message);

			}

		}



		private void ToggleSidebar_Click(object sender, RoutedEventArgs e)

		{

			_isSidebarCollapsed = !_isSidebarCollapsed;



			double targetWidth = _isSidebarCollapsed ? 64 : 236;

			var widthAnimation = new DoubleAnimation

			{

				To = targetWidth,

				Duration = TimeSpan.FromMilliseconds(200),

				DecelerationRatio = 0.8

			};

			sidebarBorder.BeginAnimation(WidthProperty, widthAnimation);



			var textVisibility = _isSidebarCollapsed ? Visibility.Collapsed : Visibility.Visible;

			if (logoTextPanel != null) logoTextPanel.Visibility = textVisibility;

			if (sessionInfoCard != null) sessionInfoCard.Visibility = textVisibility;

			if (studentTextPanel != null) studentTextPanel.Visibility = textVisibility;

			if (xpPanel != null) xpPanel.Visibility = textVisibility;



			if (btnToggleSidebar != null)

			{

				btnToggleSidebar.Content = _isSidebarCollapsed ? "\uE00F" : "\uE00E";

				btnToggleSidebar.ToolTip = _isSidebarCollapsed ? "Mở rộng thanh điều hướng" : "Thu gọn thanh điều hướng";

			}



			UpdateSidebarVisualState(_isSidebarCollapsed);

		}



		private void UpdateSidebarVisualState(bool collapsed)

		{

			if (btnToggleSidebar != null)

			{

				btnToggleSidebar.HorizontalAlignment = collapsed ? HorizontalAlignment.Center : HorizontalAlignment.Right;

			}

			if (studentAvatarBorder != null)

			{

				studentAvatarBorder.Margin = collapsed ? new Thickness(0) : new Thickness(0, 0, 10, 0);

				studentAvatarBorder.HorizontalAlignment = collapsed ? HorizontalAlignment.Center : HorizontalAlignment.Left;

			}



			if (navPanel == null) return;



			foreach (var child in navPanel.Children)

			{

				if (child is Border border)

				{

					border.Visibility = collapsed ? Visibility.Collapsed : Visibility.Visible;

				}

				else if (child is Button btn)

				{

					if (collapsed)

					{

						btn.Padding = new Thickness(0);

						btn.HorizontalContentAlignment = HorizontalAlignment.Center;

					}

					else

					{

						btn.Padding = new Thickness(14, 0, 14, 0);

						btn.HorizontalContentAlignment = HorizontalAlignment.Left;

					}



					TextBlock? iconTextBlock = null;

					TextBlock? labelTextBlock = null;

					Border? badgeBorder = null;



					if (btn.Content is Grid grid)

					{

						foreach (var gridChild in grid.Children)

						{

							if (gridChild is DockPanel dp)

							{

								FindDockPanelElements(dp, ref iconTextBlock, ref labelTextBlock);

							}

							else if (gridChild is Border b && (b.Name == "quizBadge" || b.Name == "assignmentBadge" || b.Name == "stuMsgBadge"))

							{

								badgeBorder = b;

							}

						}

					}

					else if (btn.Content is DockPanel dpDirect)

					{

						FindDockPanelElements(dpDirect, ref iconTextBlock, ref labelTextBlock);

					}



					if (labelTextBlock != null)

					{

						labelTextBlock.Visibility = collapsed ? Visibility.Collapsed : Visibility.Visible;

						if (collapsed)

						{

							btn.ToolTip = labelTextBlock.Text;

						}

						else

						{

							if (btn.Tag != null)

							{

								btn.ToolTip = labelTextBlock.Text;

							}

						}

					}



					if (iconTextBlock != null)

					{

						iconTextBlock.Margin = collapsed ? new Thickness(0) : new Thickness(0, 0, 10, 0);

					}



					if (badgeBorder != null)

					{

						if (collapsed)

						{

							badgeBorder.HorizontalAlignment = HorizontalAlignment.Center;

							badgeBorder.VerticalAlignment = VerticalAlignment.Top;

							badgeBorder.Margin = new Thickness(18, -4, 0, 0);

							badgeBorder.Width = 16;

							badgeBorder.Height = 16;

							badgeBorder.CornerRadius = new CornerRadius(8);

							if (badgeBorder.Child is TextBlock tbBadge)

							{

								tbBadge.FontSize = 9.0;

							}

						}

						else

						{

							badgeBorder.HorizontalAlignment = HorizontalAlignment.Right;

							badgeBorder.VerticalAlignment = VerticalAlignment.Center;

							badgeBorder.Margin = new Thickness(0, 0, 4, 0);

							badgeBorder.Width = badgeBorder.Name == "stuMsgBadge" ? 22 : 20;

							badgeBorder.Height = badgeBorder.Name == "stuMsgBadge" ? 22 : 20;

							badgeBorder.CornerRadius = badgeBorder.Name == "stuMsgBadge" ? new CornerRadius(11) : new CornerRadius(10);

							if (badgeBorder.Child is TextBlock tbBadge)

							{

								tbBadge.FontSize = 11.0;

							}

						}

					}

				}

			}

		}



		private void FindDockPanelElements(DockPanel dp, ref TextBlock? iconTextBlock, ref TextBlock? labelTextBlock)

		{

			foreach (var element in dp.Children)

			{

				if (element is TextBlock tb)

				{

					if (tb.FontFamily != null && tb.FontFamily.Source.Contains("Segoe MDL2 Assets"))

					{

						iconTextBlock = tb;

					}

					else

					{

						labelTextBlock = tb;

					}

				}

			}

		}



		public async void UpdateDynamicBadges()

		{

			try

			{

				var app = Application.Current as App;

				if (app?.Database == null)

				{

					if (quizBadge != null) quizBadge.Visibility = Visibility.Collapsed;

					if (assignmentBadge != null) assignmentBadge.Visibility = Visibility.Collapsed;

					return;

				}



				string className = app.StudentNetwork?.ClassName ?? "";

				var identityService = new QASmartClass.StudentClient.Services.StudentIdentityService(app.Database);

				var (studentId, studentCode, _) = identityService.GetCurrentStudent();



				// 1. Check active quiz asynchronously

				int quizCount = await Task.Run(() =>

				{

					try

					{

						lock (app.Database)

						{

							var quiz = (from q in app.Database.Quizzes

										join l in app.Database.Lessons on q.LessonId equals l.Id

										where string.IsNullOrEmpty(className) || l.ClassName == className || l.Grade == className

										orderby q.CreatedAt descending

										select q).FirstOrDefault();



							if (quiz != null)

							{

								var existingResult = app.Database.QuizResults

									.FirstOrDefault(r => r.QuizId == quiz.Id && r.StudentId == studentId);



								if (existingResult == null)

								{

									return 1;

								}

							}

						}

					}

					catch (Exception exDb)

					{

						Log.Warning("UpdateDynamicBadges Quiz DB error: {Err}", exDb.Message);

					}

					return 0;

				});



				// 2. Check pending assignment asynchronously

				int assignmentCount = await Task.Run(() =>

				{

					try

					{

						lock (app.Database)

						{

							if (!string.IsNullOrEmpty(App.AssessmentState.AssignmentDescription) &&

								App.AssessmentState.AssignmentSentTime > DateTime.MinValue)

							{

								bool hasSubmitted = app.Database.FileTransfers.Any(ft =>

									ft.StudentId == studentId &&

									ft.Direction == "StudentToTeacher" &&

									ft.Status == "Completed" &&

									ft.CreatedAt >= App.AssessmentState.AssignmentSentTime

								);



								if (!hasSubmitted)

								{

									return 1;

								}

							}

						}

					}

					catch (Exception exDb)

					{

						Log.Warning("UpdateDynamicBadges Assignment DB error: {Err}", exDb.Message);

					}

					return 0;

				});



				// Update UI on Dispatcher Thread

				base.Dispatcher.Invoke(() =>

				{

					if (_isFocusModeActive)

					{

						if (quizBadge != null) quizBadge.Visibility = Visibility.Collapsed;

						if (assignmentBadge != null) assignmentBadge.Visibility = Visibility.Collapsed;

						return;

					}



					if (quizBadge != null && txtQuizBadge != null)

					{

						if (quizCount > 0)

						{

							quizBadge.Visibility = Visibility.Visible;

							txtQuizBadge.Text = quizCount.ToString();

						}

						else

						{

							quizBadge.Visibility = Visibility.Collapsed;

						}

					}



					if (assignmentBadge != null && txtAssignmentBadge != null)

					{

						if (assignmentCount > 0)

						{

							assignmentBadge.Visibility = Visibility.Visible;

							txtAssignmentBadge.Text = assignmentCount.ToString();

						}

						else

						{

							assignmentBadge.Visibility = Visibility.Collapsed;

						}

					}

				});

			}

			catch (Exception ex)

			{

				Log.Warning("UpdateDynamicBadges error: {Err}", ex.Message);

			}

		}





		private bool IsFileLocked(System.IO.FileInfo file)

		{

			try

			{

				using (System.IO.FileStream stream = file.Open(System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.None))

				{

					stream.Close();

				}

			}

			catch (System.IO.IOException)

			{

				return true;

			}

			return false;

		}



		private void CleanupCacheDirectory()

		{

			System.Threading.Tasks.Task.Run(() =>

			{

				try

				{

					string tempDir = QASmartClass.Services.AppPaths.TempDir;

					if (System.IO.Directory.Exists(tempDir))

					{

						var files = System.IO.Directory.GetFiles(tempDir);

						int count = 0;

						int retentionDays = QASmartTouch.Services.AppSettings.CacheRetentionDays;

						DateTime threshold = DateTime.Now.AddDays(-retentionDays);

						foreach (var file in files)

						{

							var info = new System.IO.FileInfo(file);

							if (info.LastWriteTime < threshold)

							{

								if (IsFileLocked(info))

								{

									Log.Debug("Cache file is locked, skipping: {Path}", file);

									continue;

								}

								try

								{

									System.IO.File.Delete(file);

									count++;

								}

								catch (Exception exDel)

								{

									Log.Debug("Cache delete file error: {Err}", exDel.Message);

								}

							}

						}

						Log.Information("Cleaned up {Count} files in cache directory.", count);

					}

				}

				catch (Exception ex)

				{

					Log.Warning("CleanupCacheDirectory error: {Err}", ex.Message);

				}

			});

		}



		private void InitFocusWatchdogTimer()

		{

			_focusWatchdogTimer = new System.Windows.Threading.DispatcherTimer();

			_focusWatchdogTimer.Interval = TimeSpan.FromSeconds(5);

			_focusWatchdogTimer.Tick += (s, e) =>

			{

				try

				{

					if (Application.Current is App app)

					{

						var activeFocusId = App.ClassControl.ActiveToolFocusId;

						if (!string.IsNullOrEmpty(activeFocusId))

						{

							if ((DateTime.Now - _lastFocusHeartbeatTime).TotalSeconds > 300.0)

							{

								Log.Warning("Focus watchdog timeout! No heartbeat from teacher for 5m. Releasing focus.");

								App.ClassControl.ActiveToolFocusId = "";

								App.ClassControl.ActiveToolSectionId = "";

								CloseToolFocusOverlay();

							}

						}

					}

				}

				catch (Exception ex)

				{

					Log.Warning("Focus watchdog timer error: {Err}", ex.Message);

				}

			};

			_focusWatchdogTimer.Start();

		}

		private void InitBroadcastWatchdogTimer()
		{
			_broadcastWatchdogTimer = new System.Windows.Threading.DispatcherTimer();
			_broadcastWatchdogTimer.Interval = TimeSpan.FromMilliseconds(250);
			_broadcastWatchdogTimer.Tick += (s, e) =>
			{
				try
				{
					if (_broadcastOverlay == null)
					{
						return;
					}

					if (!_isBroadcastPausedByTeacher)
					{
						var kioskConfig = LoadKioskConfig();
						bool allowFallback = kioskConfig.StreamFallbackMode == "AutoFallback" || kioskConfig.StreamFallbackMode == "HttpOnly";
						double elapsedUdp = (DateTime.UtcNow - _lastUdpFrameTime).TotalSeconds;
						double pullInterval = kioskConfig.StreamFallbackMode == "HttpOnly" ? 0.5 : 1.0;
						if (allowFallback && (elapsedUdp > 1.0 || kioskConfig.StreamFallbackMode == "HttpOnly") && (DateTime.UtcNow - _lastHttpPullTime).TotalSeconds >= pullInterval)
						{
							_lastHttpPullTime = DateTime.UtcNow;
							UpdateScreenBroadcast("");
						}
					}
					else
					{
						// === PHASE6.1-BUG2: Safety timeout ===
						double pauseElapsed = (DateTime.UtcNow - _pauseStartTime).TotalSeconds;

						if (_pauseStartTime != DateTime.MinValue && pauseElapsed > 600)

						{

							Log.Warning("[Watchdog] Pause exceeded 10 min without RESUME");

							_isBroadcastPausedByTeacher = false;

							_pauseStartTime = DateTime.MinValue;

							CloseScreenBroadcast();

							ShowNotification("\u26a0\ufe0f M\u1ea5t k\u1ebft n\u1ed1i", "GV pause qu\u00e1 l\u00e2u, t\u1ef1 \u0111\u1ed9ng tho\u00e1t.", "#C62828");

						}

					}



					if (_broadcastOverlay != null && !_isBroadcastPausedByTeacher)

					{

						if (_isBroadcastForceWatch)

						{

							TryAutoRecoverForceWatch("Watchdog");

						}



						// === PHASE6-GD1: Dual-channel watchdog ===

						double elapsedUdp2 = (DateTime.UtcNow - _lastBroadcastUpdateTime).TotalSeconds;

						double elapsedTcp = (DateTime.UtcNow - _lastTcpCommandTime).TotalSeconds;

						double effectiveElapsed = Math.Min(elapsedUdp2, elapsedTcp);

						if (effectiveElapsed > _broadcastHeartbeatTimeout)

						{

							Log.Warning("Broadcast watchdog timeout! No screen update from teacher for {Timeout}s. Releasing broadcast.", _broadcastHeartbeatTimeout);

							base.Dispatcher.Invoke(() =>

							{

								CloseScreenBroadcast();

								if (_isBroadcastForceWatch)

								{

									ShowNotification("⚠️ Mất kết nối", "Đã tự động thoát chế độ trình chiếu do mất tín hiệu từ GV.", "#C62828");

								}

								else

								{

									ShowNotification("Dừng trình chiếu do mất tín hiệu đường truyền.", "Đường truyền mạng bị gián đoạn.", "#757575");

								}

							});

						}

						else if (effectiveElapsed > 5.0) // PHASE6-GD1

						{

							base.Dispatcher.Invoke(() =>

							{

								if (_broadcastOverlay != null)

								{

									_broadcastOverlay.Opacity = 0.7; // Cấp độ 1: Làm mờ nhẹ 70%

								}

								if (_broadcastWarningTextBlock != null)

								{

									_broadcastWarningTextBlock.Text = "⚠️ Đang kết nối lại...";

									_broadcastWarningTextBlock.Visibility = Visibility.Visible;

								}

								if (_broadcastConnectionStatus != null)

								{

									_broadcastConnectionStatus.Visibility = Visibility.Visible;

									if (_broadcastStatusText != null)

									{

										_broadcastStatusText.Text = "Luồng hình ảnh bị gián đoạn, đang kết nối lại...";

									}

								}

							});

						}

						else

						{

							base.Dispatcher.Invoke(() =>

							{

								if (_broadcastOverlay != null)

								{

									_broadcastOverlay.Opacity = 1.0; // Khôi phục độ sáng

								}

								if (_broadcastWarningTextBlock != null)

								{

									_broadcastWarningTextBlock.Visibility = Visibility.Collapsed;

								}

								if (_broadcastConnectionStatus != null && _broadcastImage != null && _broadcastImage.Source != null)

								{

									_broadcastConnectionStatus.Visibility = Visibility.Collapsed;

								}

							});

						}

					}

				}

				catch (Exception ex)

				{

					Log.Warning("Broadcast watchdog timer error: {Err}", ex.Message);

				}

			};

			_broadcastWatchdogTimer.Start();

		}



		private void StudentShell_PreviewKeyDown(object sender, KeyEventArgs e)

		{

			// Debug Hotkey: Ctrl+Alt+Shift+F9 to simulate sudden network loss

			if (e.Key == Key.F9 && (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift)) == (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift))

			{

				var app = Application.Current as App;

				if (app?.StudentNetwork != null)

				{

					Log.Information("[Debug] Hotkey Ctrl+Alt+Shift+F9 pressed. Simulating network loss...");

					app.StudentNetwork.Stop();

					e.Handled = true;

					return;

				}

			}

			// Debug Hotkey: Ctrl+Shift+F12 to toggle telemetry debug overlay
			if (e.Key == Key.F12 && (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == (ModifierKeys.Control | ModifierKeys.Shift))
			{
				ToggleTelemetryOverlay();
				e.Handled = true;
				return;
			}



			bool isFocusActive = (App.FocusState.ActiveFocusSort >= 0) || _isQuizFocusActive;

			if (isFocusActive)

			{

				if (e.Key == Key.BrowserBack)

				{

					e.Handled = true;

					Log.Information("PreviewKeyDown: BrowserBack blocked because Focus Mode is active.");

					return;

				}

				if (e.Key == Key.Back)

				{

					var focused = Keyboard.FocusedElement;

					if (focused is not TextBox && focused is not PasswordBox && focused is not RichTextBox)

					{

						e.Handled = true;

						Log.Information("PreviewKeyDown: Backspace blocked because Focus Mode is active.");

						return;

					}

				}

				if (e.Key == Key.Left && (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)

				{

					e.Handled = true;

					Log.Information("PreviewKeyDown: Alt+Left blocked because Focus Mode is active.");

					return;

				}

			}

		}



		private void SyncWithTeacherState()

		{

			try

			{

				if (!(Application.Current is App))

				{

					return;

				}

				if (App.LessonState.IsLessonActive && App.LessonState.ActiveLessonId > 0)

				{

					if (_currentPage != "S2")

					{

						NavigateTo("S2");

						Log.Information("Student synced: active lesson #{Id}, stage {Stage}", App.LessonState.ActiveLessonId, App.LessonState.ActiveLessonStage);

					}

					int currentStage = App.LessonState.ActiveLessonStage;

					if (1 == 0)

					{

					}

					string text = currentStage switch

					{

						1 => "Chuẩn bị", 

						2 => "Kiểm tra bài cũ", 

						3 => "Bài giảng mới", 

						4 => "Thực hành / Trò chơi", 

						5 => "Đánh giá", 

						6 => "Tổng kết", 

						_ => $"Giai đoạn {currentStage}", 

					};

					if (1 == 0)

					{

					}

					string text2 = text;

					txtPageTitle.Text = "\ud83d\udcd6 Bài giảng — " + text2;

					DispatcherTimer stageTimer = new DispatcherTimer

					{

						Interval = TimeSpan.FromMilliseconds(500.0)

					};

					stageTimer.Tick += delegate

					{

						stageTimer.Stop();

						try

						{

							if (contentFrame.Content is StudentLessonPage studentLessonPage)

							{

								studentLessonPage.SetStageView(currentStage);

								Log.Information("SyncWithTeacherState: SetStageView({Stage})", currentStage);

							}

						}

						catch (Exception ex2)

						{

							Log.Warning("Delayed SetStageView error: {Err}", ex2.Message);

						}

					};

					stageTimer.Start();

					if ((DateTime.Now - App.LessonState.LastCommandTime).TotalSeconds < 30.0)

					{

						ShowNotification("\ud83d\udcd6 Tiết học đang diễn ra", "GV đang ở giai đoạn: " + text2, "#1976D2");

					}

				}

				if (App.BroadcastState.IsScreenBroadcastActive && !string.IsNullOrEmpty(App.BroadcastState.ScreenCapturePath) && _broadcastOverlay == null)

				{

					// === UPGRADE_02 FIX: Chỉ tái tạo nếu CÓ kết nối TCP active ===

					bool hasActiveConnection = ((App)Application.Current)?.StudentNetwork?.IsConnected == true;

					if (hasActiveConnection)

					{

						ShowScreenBroadcast(App.BroadcastState.ScreenCapturePath);

						Log.Information("SyncWithTeacher: auto-restored broadcast overlay (TCP active)");

					}

					else

					{

						// Reset state cũ vì không còn kết nối — tránh zombie broadcast

						App.BroadcastState.IsScreenBroadcastActive = false;

						App.BroadcastState.ScreenCapturePath = string.Empty;

						Log.Warning("[Broadcast] Stale broadcast state detected. Reset without reconnection.");

					}

				}

				if (string.IsNullOrEmpty(App.AssessmentState.ActiveSurveyQuestion) || App.AssessmentState.SurveyAnswered || _surveyOverlay != null || !((DateTime.Now - App.AssessmentState.ActiveSurveyTime).TotalMinutes < 30.0))

				{

					return;

				}

				if (App.AssessmentState.ActiveSurveyQuestion.Trim().StartsWith("{"))

				{

					try

					{

						var optionsObj = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };

						var msg = System.Text.Json.JsonSerializer.Deserialize<JsonNetworkMessage>(App.AssessmentState.ActiveSurveyQuestion, optionsObj);

						if (msg != null && msg.Action == "SURVEY_START")

						{

							var payload = System.Text.Json.JsonSerializer.Deserialize<SurveyStartPayload>(msg.Payload.GetRawText(), optionsObj);

							if (payload != null && IsStudentInTargetClasses(payload.TargetClasses))

							{

								int num2 = (int)(DateTime.Now - App.AssessmentState.ActiveSurveyTime).TotalSeconds;

								int num3 = ((payload.TimeLimitSeconds > 0) ? Math.Max(0, payload.TimeLimitSeconds - num2) : 0);

								if (payload.TimeLimitSeconds == 0 || num3 > 5)

								{

									ShowSurveyPopup(payload.QuestionText, payload.Options, num3);

									Log.Information("SyncWithTeacher (JSON): auto-showed pending survey: {Q}", payload.QuestionText);

								}

							}

						}

					}

					catch (Exception exSync)

					{

						Log.Warning("SyncWithTeacherState JSON error: {Err}", exSync.Message);

					}

					return;

				}

				string[] array = App.AssessmentState.ActiveSurveyQuestion.Split('|');

				if (array.Length < 5 || !(array[1] == "SURVEY_CUSTOM"))

				{

					return;

				}

				bool flag = false;

				string text3 = "ALL";

				string text4 = "";

				if (array.Length >= 6)

				{

					string text5 = array[3];

					if (text5 == "ALL" || !text5.Contains(" ") || text5.Split(',').All((string c) => c.Length < 15 && !c.Contains("?")))

					{

						flag = true;

					}

				}

				List<string> options;

				if (flag)

				{

					text3 = array[3];

					text4 = array[4];

					options = array.Skip(5).ToList();

				}

				else

				{

					text3 = "ALL";

					text4 = array[3];

					options = array.Skip(4).ToList();

				}

				if (IsStudentInTargetClasses(text3))

				{

					int result;

					int num = (int.TryParse(array[2], out result) ? result : 0);

					int num2 = (int)(DateTime.Now - App.AssessmentState.ActiveSurveyTime).TotalSeconds;

					int num3 = ((num > 0) ? Math.Max(0, num - num2) : 0);

					if (num == 0 || num3 > 5)

					{

						ShowSurveyPopup(text4, options, num3);

						Log.Information("SyncWithTeacher: auto-showed pending survey: {Q}", text4);

					}

				}

				else

				{

					Log.Information("SyncWithTeacher: survey ignored for student class (target was {Target})", text3);

				}

			}

			catch (Exception ex)

			{

				Log.Warning("SyncWithTeacherState error: {Err}", ex.Message);

			}

		}



		private static void EnsureStudentFirewallRules()

		{

			try

			{

				// Read Master Config

				bool autoConfigureFirewall = true;

				try

				{

					using var db = new AppDbContext();

					var autoConfigureSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Network_AutoConfigureFirewall");

					autoConfigureFirewall = (autoConfigureSetting?.Value ?? "Enabled") == "Enabled";

				}

				catch { }



				if (!autoConfigureFirewall)

				{

					Log.Information("StudentShell: Auto firewall configuration is disabled by admin setting.");

					return;

				}



				bool isAdmin;

				using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent())

				{

					var principal = new System.Security.Principal.WindowsPrincipal(identity);

					isAdmin = principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);

				}



				if (!isAdmin)

				{

					Log.Debug("StudentShell: Not running as administrator; skipping automatic firewall registration.");

					return;

				}



				string exePath = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";

				if (string.IsNullOrEmpty(exePath)) return;



				Log.Information("StudentShell: Ensuring Windows Firewall inbound rules for QA SmartClass...");



				// Clean up previous rules

				RunCommand("netsh", "advfirewall firewall delete rule name=\"QA SmartClass Client\"");

				RunCommand("netsh", "advfirewall firewall delete rule name=\"QA SmartClass Ports TCP\"");

				RunCommand("netsh", "advfirewall firewall delete rule name=\"QA SmartClass Ports UDP\"");

				

				// Add friendly program rules

				RunCommand("netsh", $"advfirewall firewall add rule name=\"QA SmartClass Client\" dir=in action=allow program=\"{exePath}\" enable=yes profile=any");

				

				// Add specific port rules

				RunCommand("netsh", "advfirewall firewall add rule name=\"QA SmartClass Ports TCP\" dir=in action=allow protocol=TCP localport=29877,29879 enable=yes profile=any");

				RunCommand("netsh", "advfirewall firewall add rule name=\"QA SmartClass Ports UDP\" dir=in action=allow protocol=UDP localport=8088 enable=yes profile=any");



				// Try PowerShell as fallback/addition

				RunCommand("powershell", "-Command \"New-NetFirewallRule -DisplayName 'QA SmartClass TCP' -Direction Inbound -LocalPort 29877,29879 -Protocol TCP -Action Allow -ErrorAction SilentlyContinue\"");

				RunCommand("powershell", "-Command \"New-NetFirewallRule -DisplayName 'QA SmartClass UDP' -Direction Inbound -LocalPort 8088 -Protocol UDP -Action Allow -ErrorAction SilentlyContinue\"");

			}

			catch (Exception ex)

			{

				Log.Warning("StudentShell: Failed to configure firewall: {Err}", ex.Message);

			}

		}



		private static void RunCommand(string filename, string arguments)

		{

			try

			{

				var psi = new System.Diagnostics.ProcessStartInfo

				{

					FileName = filename,

					Arguments = arguments,

					CreateNoWindow = true,

					UseShellExecute = false

				};

				using var p = System.Diagnostics.Process.Start(psi);

				p?.WaitForExit();

			}

			catch { }

		}



		private async Task StartStudentNetworkAsync()

		{

			try

			{

				EnsureStudentFirewallRules();

				App app = (App)Application.Current;

				StudentNetworkClient client = app.StudentNetwork;

				if (client.IsConnected)

				{

					return;

				}

				string studentCode = "HS001";

				string studentName = "Học sinh";

				string teacherIP = "";

				int networkPort = 29877;

				bool rememberMe = false;

				try

				{

					string profilePath = AppPaths.StudentProfileFile;

					if (File.Exists(profilePath))

					{

						string json = QASmartClass.StudentClient.Services.SecureProfileHelper.ReadProfileText(profilePath);

						StudentProfileCache profile = JsonSerializer.Deserialize<StudentProfileCache>(json);

						if (profile != null)

						{

							studentCode = profile.StudentCode;

							studentName = profile.StudentName;

							teacherIP = profile.TeacherIP;

							networkPort = profile.NetworkPort;

							rememberMe = profile.RememberMe;

						}

					}

				}

				catch

				{

				}

				client.StudentName = studentName;

				client.StudentCode = studentCode;

				client.PCName = Environment.MachineName;

				_chatPage = new StudentChatPage();



				// Unsubscribe to prevent memory leak and event duplication

				client.PropertyChanged -= Client_PropertyChanged;

				client.Connected -= Client_Connected;

				client.Disconnected -= Client_Disconnected;

				client.CommandReceived -= Client_CommandReceived;

				client.MessageReceived -= Client_MessageReceived;



				// Subscribe using named methods

				client.PropertyChanged += Client_PropertyChanged;

				client.Connected += Client_Connected;

				client.Disconnected += Client_Disconnected;

				client.CommandReceived += Client_CommandReceived;

				client.MessageReceived += Client_MessageReceived;



				 base.Dispatcher.Invoke(() => UpdateQuickHandRaiseUI(client.IsHandRaised));

				 base.Dispatcher.Invoke(() => UpdateWifiIndicator());



				if (rememberMe && !string.IsNullOrEmpty(teacherIP))

				{

					Log.Information("RememberMe active, attempting direct connect to {IP}:{Port}...", teacherIP, networkPort);

					try

					{

						await Task.WhenAny(client.ConnectDirectAsync(teacherIP, networkPort), Task.Delay(2000));

					}

					catch (Exception exDirect)

					{

						Log.Warning("Auto direct connect failed: {Err}", exDirect.Message);

					}

				}



				if (!client.IsConnected)

				{

					await client.StartAsync();

				}



				if (!client.IsConnected)

				{

					txtConnectionStatus.Text = "Đang tìm GV...";

					txtConnectionStatus.Foreground = new SolidColorBrush(Color.FromRgb(byte.MaxValue, 152, 0));

				}

			}

			catch (Exception ex)

			{

				Exception ex2 = ex;

				Log.Warning("Student network start error: {Err}", ex2.Message);

				txtConnectionStatus.Text = "Lỗi kết nối";

				txtConnectionStatus.Foreground = new SolidColorBrush(Color.FromRgb(239, 83, 80));

			}

			try

			{

				Application current = Application.Current;

				if (!(current is App app2))

				{

					return;

				}

				app2.LocalCommandReceived -= App_LocalCommandReceived;

				app2.LocalCommandReceived += App_LocalCommandReceived;

				txtConnectionStatus.Text = "✅ Đã kết nối (cùng máy)";

				txtConnectionStatus.Foreground = new SolidColorBrush(Color.FromRgb(102, 187, 106));

				Log.Information("Local command bus connected — same machine mode");

				SyncBroadcastState(app2);

			}

			catch (Exception ex)

			{

				Exception ex3 = ex;

				Log.Warning("Local command bus error: {Err}", ex3.Message);

			}

		}



		private void SyncBroadcastState(App app)

		{

			try

			{

				if (App.BroadcastState.IsScreenBroadcastActive && !string.IsNullOrEmpty(App.BroadcastState.ScreenCapturePath) && File.Exists(App.BroadcastState.ScreenCapturePath))

				{

					Log.Information("Late-join: Screen broadcast active, showing overlay: {Path}", App.BroadcastState.ScreenCapturePath);

					ShowScreenBroadcast(App.BroadcastState.ScreenCapturePath);

					ShowNotification("\ud83d\udcfa GV đang chiếu", "Giáo viên đang chiếu màn hình. Hãy theo dõi!", "#7B1FA2");

				}

				if (!string.IsNullOrEmpty(App.BroadcastState.FileBroadcastPath) && File.Exists(App.BroadcastState.FileBroadcastPath))

				{

					string text = Path.GetExtension(App.BroadcastState.FileBroadcastPath).ToLower();

					if (1 == 0)

					{

					}

					string text2;

					switch (text)

					{

					case ".png":

					case ".jpg":

					case ".jpeg":

					case ".gif":

					case ".bmp":

						text2 = "IMAGE";

						break;

					case ".mp4":

					case ".avi":

					case ".mkv":

						text2 = "VIDEO";

						break;

					case ".pdf":

						text2 = "PDF";

						break;

					default:

						text2 = "FILE";

						break;

					}

					if (1 == 0)

					{

					}

					string fileType = text2;

					Log.Information("Late-join: File broadcast active: {Path}", App.BroadcastState.FileBroadcastPath);

					ShowFileBroadcast(fileType, App.BroadcastState.FileBroadcastPath);

					ShowNotification("\ud83d\udcc1 GV đang phát file", "GV đang phát: " + Path.GetFileName(App.BroadcastState.FileBroadcastPath), "#1976D2");

				}

				if (App.LessonState.IsLessonActive && App.LessonState.ActiveLessonId > 0)

				{

					Log.Information("Late-join: Lesson active #{Id}, stage {Stage}", App.LessonState.ActiveLessonId, App.LessonState.ActiveLessonStage);

					NavigateTo("S2");

					ShowNotification("\ud83d\udcd6 Tiết học đang diễn ra", "GV đang dạy. Hãy theo dõi bài giảng!", "#2E7D32");

				}

				if (App.ClassControl.IsScreenLocked)

				{

					if (App.ClassControl.ScreenLockType == "BLACK")

					{

						ShowScreenLockOverlay("\ud83d\udcf4 Tắt màn hình", "GV đã tắt màn hình. Hãy chú ý lên bảng.", "#000000");

						Log.Information("Late-join: Black screen active");

					}

					else

					{

						ShowScreenLockOverlay("\ud83d\udd12 Màn hình đã bị khóa", "GV đã khóa màn hình. Vui lòng chờ.", "#C62828");

						Log.Information("Late-join: Screen lock active");

					}

				}

				if (App.ClassControl.IsSilenceActive)

				{

					ShowSilenceOverlay();

					Log.Information("Late-join: Silence mode active");

				}

				if (!string.IsNullOrEmpty(App.ClassControl.ActiveToolFocusId))

				{

					if (!string.IsNullOrEmpty(App.ClassControl.ActiveToolSectionId))

					{

						ShowToolSectionFocusOverlay(App.ClassControl.ActiveToolFocusId, App.ClassControl.ActiveToolSectionId);

						Log.Information("Late-join: Tool section focus: {Tool}/{Section}", App.ClassControl.ActiveToolFocusId, App.ClassControl.ActiveToolSectionId);

					}

					else

					{

						ShowToolFocusOverlay(App.ClassControl.ActiveToolFocusId);

						Log.Information("Late-join: Tool focus: {Tool}", App.ClassControl.ActiveToolFocusId);

					}

				}

				if (App.FocusState.ActiveFocusSort >= 0 && App.LessonState.IsLessonActive)

				{

					DispatcherTimer focusTimer = new DispatcherTimer

					{

						Interval = TimeSpan.FromMilliseconds(800.0)

					};

					int sortOrder = App.FocusState.ActiveFocusSort;

					string focusType = App.FocusState.ActiveFocusType;

					focusTimer.Tick += delegate

					{

						focusTimer.Stop();

						try

						{

							if (contentFrame.Content is StudentLessonPage studentLessonPage)

							{

								studentLessonPage.FocusBlock(sortOrder, focusType);

								Log.Information("Late-join: FocusBlock({Sort}, {Type})", sortOrder, focusType);

							}

						}

						catch (Exception ex2)

						{

							Log.Warning("Late-join FocusBlock error: {Err}", ex2.Message);

						}

					};

					focusTimer.Start();

				}

				if (App.BroadcastState.IsBroadcastCasting)

				{

					ShowScreenCastOverlay("1920×1080", "Đang chiếu");

					Log.Information("Late-join: Broadcast casting active");

				}

			}

			catch (Exception ex)

			{

				Log.Warning("SyncBroadcastState error: {Err}", ex.Message);

			}

		}



		public class StringOrArrayConverter : System.Text.Json.Serialization.JsonConverter<string>

		{

			public override string Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)

			{

				if (reader.TokenType == System.Text.Json.JsonTokenType.String)

				{

					return reader.GetString() ?? string.Empty;

				}

				if (reader.TokenType == System.Text.Json.JsonTokenType.StartArray)

				{

					var list = new System.Collections.Generic.List<string>();

					while (reader.Read() && reader.TokenType != System.Text.Json.JsonTokenType.EndArray)

					{

						if (reader.TokenType == System.Text.Json.JsonTokenType.String)

						{

							list.Add(reader.GetString() ?? string.Empty);

						}

						else

						{

							reader.Skip();

						}

					}

					return string.Join(",", list);

				}

				using (var doc = System.Text.Json.JsonDocument.ParseValue(ref reader))

				{

					return doc.RootElement.ToString();

				}

			}



			public override void Write(System.Text.Json.Utf8JsonWriter writer, string value, System.Text.Json.JsonSerializerOptions options)

			{

				writer.WriteStringValue(value);

			}

		}



		public class IntOrStringConverter : System.Text.Json.Serialization.JsonConverter<string>

		{

			public override string Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)

			{

				if (reader.TokenType == System.Text.Json.JsonTokenType.String)

				{

					return reader.GetString() ?? string.Empty;

				}

				if (reader.TokenType == System.Text.Json.JsonTokenType.Number)

				{

					return reader.GetInt64().ToString();

				}

				using (var doc = System.Text.Json.JsonDocument.ParseValue(ref reader))

				{

					return doc.RootElement.ToString();

				}

			}



			public override void Write(System.Text.Json.Utf8JsonWriter writer, string value, System.Text.Json.JsonSerializerOptions options)

			{

				writer.WriteStringValue(value);

			}

		}



		public class SurveyStartPayload

		{

			[System.Text.Json.Serialization.JsonConverter(typeof(IntOrStringConverter))]

			public string SurveyId { get; set; } = string.Empty;

			public string Title { get; set; } = string.Empty;

			public string SurveyType { get; set; } = string.Empty;

			public string QuestionText { get; set; } = string.Empty;

			public System.Collections.Generic.List<string> Options { get; set; } = new();

			public int TimeLimitSeconds { get; set; }

			public bool IsAnonymous { get; set; }



			[System.Text.Json.Serialization.JsonConverter(typeof(StringOrArrayConverter))]

			public string TargetClasses { get; set; } = string.Empty;

		}



		public class JsonNetworkMessage

		{

			public string Action { get; set; } = string.Empty;

			public System.Text.Json.JsonElement Payload { get; set; }

		}



		private void HandleJsonCommand(string cmd)

		{

			try

			{

				var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };

				var msg = System.Text.Json.JsonSerializer.Deserialize<JsonNetworkMessage>(cmd, options);

				if (msg == null) return;



				if (msg.Action == "SURVEY_START")

				{

					var payload = System.Text.Json.JsonSerializer.Deserialize<SurveyStartPayload>(msg.Payload.GetRawText(), options);

					if (payload == null) return;



					// Update global state

					App.AssessmentState.ActiveSurveyQuestion = cmd; // Store raw JSON string

					App.AssessmentState.ActiveSurveyTime = DateTime.Now;

					App.AssessmentState.SurveyAnswered = false;

					App.AssessmentState.ActivePollId = payload.SurveyId;



					if (!IsStudentInTargetClasses(payload.TargetClasses))

					{

						Log.Information("Survey custom ignored. Student class not in target: {Target}", payload.TargetClasses);

						return;

					}



					// Save to local database for StudentSurveyPage to consume

					try

					{

						var app = (App)Application.Current;

						if (app?.Database != null)

						{

							app.Database.EventLogs.Add(new QASmartClass.Data.EventLog

							{

								EventType = "SURVEY",

								Actor = "GV",

								Details = cmd,

								Timestamp = DateTime.Now

							});

							app.Database.SaveChanges();

							Log.Information("Saved SURVEY_START command to local EventLogs");

						}

					}

					catch (Exception exDb)

					{

						Log.Warning("Failed to save SURVEY_START to local database: {Err}", exDb.Message);

					}



					base.Dispatcher.Invoke(() =>

					{

						ShowSurveyPopup(payload.QuestionText, payload.Options, payload.TimeLimitSeconds);

						ShowNotification("Khảo sát từ GV", "\"" + payload.QuestionText + "\"", "#1565C0");

					});

				}

				else if (msg.Action == "SURVEY_END")

				{

					base.Dispatcher.Invoke(() =>

					{

						CloseSurveyPopup();

						_surveyCountdownTimer?.Stop();

						ShowNotification("⏰ Hết giờ khảo sát", "Thời gian khảo sát đã kết thúc", "#E65100");

					});

				}

			}

			catch (Exception ex)

			{

				Log.Warning("HandleJsonCommand error: {Err}", ex.Message);

			}

		}



		public class StudentQuizQuestionDto

		{

			public string? Content { get; set; }

			public string? OptionsJson { get; set; }

			public int Points { get; set; }

			public int SortOrder { get; set; }

			public string? ImageUrl { get; set; }

			public string? Image { get; set; }

		}



		private void SaveQuizDataToDatabase(int quizId, string questionsJson)

		{

			try

			{

				var app = (App)Application.Current;

				if (app?.Database == null)

				{

					Log.Warning("SaveQuizDataToDatabase: app or app.Database is null");

					return;

				}



				int lessonId = App.LessonState.ActiveLessonId;

				if (lessonId <= 0) lessonId = 1;



				var lesson = app.Database.Lessons.FirstOrDefault(l => l.Id == lessonId);

				if (lesson == null)

				{

					lesson = new QASmartClass.Data.Lesson

					{

						Id = lessonId,

						Title = "Default Lesson",

						Subject = "General",

						Grade = "10"

					};

					app.Database.Lessons.Add(lesson);

					app.Database.SaveChanges();

				}



				var quiz = app.Database.Quizzes.FirstOrDefault(q => q.Id == quizId);

				if (quiz == null)

				{

					quiz = new QASmartClass.Data.Quiz

					{

						Id = quizId,

						Title = "Fast Quiz",

						QuizType = "Competition",

						TimeLimitSeconds = 300,

						LessonId = lessonId,

						CreatedAt = DateTime.Now

					};

					app.Database.Quizzes.Add(quiz);

				}

				else

				{

					quiz.LessonId = lessonId;

				}

				app.Database.SaveChanges();



				var existingQuestions = app.Database.Questions.Where(q => q.QuizId == quizId).ToList();

				if (existingQuestions.Any())

				{

					app.Database.Questions.RemoveRange(existingQuestions);

					app.Database.SaveChanges();

				}



				var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };

				var deserialized = System.Text.Json.JsonSerializer.Deserialize<List<StudentQuizQuestionDto>>(questionsJson, options);

				if (deserialized != null)

				{

					int index = 1;

					foreach (var dto in deserialized)

					{

						var question = new QASmartClass.Data.Question

						{

							QuizId = quizId,

							Content = dto.Content ?? string.Empty,

							OptionsJson = dto.OptionsJson ?? "[]",

							Points = dto.Points != 0 ? dto.Points : 10,

							SortOrder = dto.SortOrder != 0 ? dto.SortOrder : index++,

							ImageUrl = dto.ImageUrl ?? dto.Image ?? string.Empty,

							CorrectAnswer = string.Empty,

							QuestionType = "MultipleChoice",

							Difficulty = "Medium"

						};



						app.Database.Questions.Add(question);

					}

					app.Database.SaveChanges();

					Log.Information("Saved {Count} questions for quizId {QuizId} to database", deserialized.Count, quizId);

				}



				base.Dispatcher.Invoke(() =>

				{

					if (contentFrame.Content is StudentQuizPage quizPage)

					{

						quizPage.CheckForQuiz();

						Log.Information("Triggered CheckForQuiz() on StudentQuizPage");

					}

				});

			}

			catch (Exception ex)

			{

				Log.Error("Error in SaveQuizDataToDatabase: {Err}", ex.Message);

			}

		}



		private void UpdateQuizResultAndCorrectAnswers(int quizId, double score, int correctCount, int totalQuestions, string correctAnswersJson)

		{

			try

			{

				var app = (App)Application.Current;

				if (app?.Database == null) return;



				var identityService = new QASmartClass.StudentClient.Services.StudentIdentityService(app.Database);

				var (studentId, _, _) = identityService.GetCurrentStudent();



				var correctAnswers = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(correctAnswersJson);

				if (correctAnswers == null) return;



				var questions = app.Database.Questions.Where(q => q.QuizId == quizId).ToList();

				foreach (var q in questions)

				{

					if (correctAnswers.TryGetValue(q.Id.ToString(), out var correctAns))

					{

						q.CorrectAnswer = correctAns;

					}

				}



				var existingResult = app.Database.QuizResults.FirstOrDefault(r => r.QuizId == quizId && r.StudentId == studentId);

				if (existingResult != null)

				{

					existingResult.Score = (int)score;

					existingResult.CorrectCount = correctCount;

					existingResult.TotalQuestions = totalQuestions;

					existingResult.SubmittedAt = DateTime.Now;

				}

				else

				{

					var result = new Data.QuizResult

					{

						QuizId = quizId,

						StudentId = studentId,

						Score = (int)score,

						TotalPoints = totalQuestions * 10,

						CorrectCount = correctCount,

						TotalQuestions = totalQuestions,

						TimeSpentSeconds = 0,

						AnswersJson = "{}",

						SubmittedAt = DateTime.Now

					};

					app.Database.QuizResults.Add(result);

				}



				string className = app.StudentNetwork?.ClassName ?? "";

				var roster = app.Database.ClassRosters.FirstOrDefault(r => r.IsActive && r.ClassName == className);

				if (roster != null)

				{

					var gradeType = app.Database.GradeTypeMasters.FirstOrDefault(g => g.Code == "Quiz" || g.ShortName == "15p") 

								 ?? app.Database.GradeTypeMasters.FirstOrDefault();



					if (gradeType != null)

					{

						double scale10 = Math.Round(score / 10.0, 1);

						var existingGrade = app.Database.StudentGrades.FirstOrDefault(g => g.StudentId == studentId && g.RosterId == roster.Id && g.Notes != null && g.Notes.Contains($"Quiz '{quizId}'"));

						

						if (existingGrade == null)

						{

							var quizObj = app.Database.Quizzes.Find(quizId);

							string title = quizObj?.Title ?? "Quiz";

							app.Database.StudentGrades.Add(new Data.StudentGrade

							{

								StudentId = studentId,

								RosterId = roster.Id,

								GradeTypeId = gradeType.Id,

								Attempt = 1,

								Score = scale10,

								Notes = $"Auto-graded từ Quiz '{title}' ({correctCount}/{totalQuestions}đ)",

								IsConfirmed = true

							});

						}

					}

				}



				app.Database.SaveChanges();

				Log.Information("StudentShell: Updated quiz {QuizId} grade: Score={Score}, CorrectCount={CorrectCount}/{TotalQuestions}", quizId, score, correctCount, totalQuestions);



				base.Dispatcher.Invoke(() =>

				{

					if (contentFrame.Content is StudentQuizPage quizPage)

					{

						quizPage.CheckForQuiz();

						Log.Information("Triggered CheckForQuiz() on StudentQuizPage after grading");

					}

				});

			}

			catch (Exception ex)

			{

				Log.Error("Error in UpdateQuizResultAndCorrectAnswers: {Err}", ex.Message);

			}

		}



		private void Global_OnFileReceived(object? sender, StudentFileEventArgs e)

	{

		base.Dispatcher.Invoke(() =>

		{

			// 1. Show notification toast

			ShowNotification("Nhận file từ GV", $"Đã nhận tệp: {e.FileName}", "#1565C0");



			// 2. Only show the modal popup window if StudentSubmitPage is NOT currently active

			if (contentFrame.Content?.GetType().Name != "StudentSubmitPage")

			{

				var win = new Window

				{

					Title = "Nhận file từ Giáo viên",

					Width = 450,

					Height = 260,

					WindowStartupLocation = WindowStartupLocation.CenterScreen,

					ResizeMode = ResizeMode.NoResize,

					Background = new SolidColorBrush(Color.FromRgb(250, 250, 250)),

					Topmost = true

				};



				var mainStack = new StackPanel { Margin = new Thickness(20) };

				mainStack.Children.Add(new TextBlock

				{

					Text = "Đã nhận tài liệu mới từ Giáo viên!",

					FontSize = 16,

					FontWeight = FontWeights.Bold,

					Foreground = new SolidColorBrush(Color.FromRgb(21, 101, 192)),

					Margin = new Thickness(0, 0, 0, 12)

				});



				var fileInfoBorder = new Border

				{

					Background = Brushes.White,

					BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),

					BorderThickness = new Thickness(1),

					CornerRadius = new CornerRadius(8),

					Padding = new Thickness(12),

					Margin = new Thickness(0, 0, 0, 16)

				};



				var fileInfoStack = new StackPanel();

				fileInfoStack.Children.Add(new TextBlock

				{

					Text = $"Tên file: {e.FileName}",

					FontSize = 13,

					FontWeight = FontWeights.SemiBold,

					Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),

					Margin = new Thickness(0, 0, 0, 4),

					TextTrimming = TextTrimming.CharacterEllipsis

				});

				fileInfoStack.Children.Add(new TextBlock

				{

					Text = $"Dung lượng: {e.FileSize / 1024.0:F1} KB",

					FontSize = 12,

					Foreground = Brushes.Gray,

					Margin = new Thickness(0, 0, 0, 4)

				});

				fileInfoStack.Children.Add(new TextBlock

				{

					Text = $"Thư mục: {System.IO.Path.GetDirectoryName(e.SavePath)}",

					FontSize = 11,

					Foreground = Brushes.Gray,

					TextTrimming = TextTrimming.CharacterEllipsis

				});

				fileInfoBorder.Child = fileInfoStack;

				mainStack.Children.Add(fileInfoBorder);



				var btnDock = new DockPanel();

				var btnClose = new Button

				{

					Content = "Đóng",

					Width = 90,

					Height = 32,

					Background = new SolidColorBrush(Color.FromRgb(238, 238, 238)),

					Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66)),

					BorderThickness = new Thickness(0),

					Cursor = System.Windows.Input.Cursors.Hand,

					FontWeight = FontWeights.SemiBold

				};

				btnClose.Click += (s, ev) => win.Close();

				DockPanel.SetDock(btnClose, Dock.Right);

				btnDock.Children.Add(btnClose);



				var btnOpenFolder = new Button

				{

					Content = "Mở thư mục",

					Width = 140,

					Height = 32,

					Background = new SolidColorBrush(Color.FromRgb(21, 101, 192)),

					Foreground = Brushes.White,

					BorderThickness = new Thickness(0),

					Cursor = System.Windows.Input.Cursors.Hand,

					FontWeight = FontWeights.SemiBold,

					Margin = new Thickness(0, 0, 8, 0)

				};

				btnOpenFolder.Click += (s, ev) =>

				{

					var savePath = e.SavePath;

					Task.Run(() =>

					{

						try { System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{savePath}\""); }

						catch (Exception ex)

						{

							System.Windows.Application.Current?.Dispatcher.Invoke(() =>

							{

								MessageBox.Show($"Không thể mở thư mục: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);

							});

						}

					});

					try { win.Close(); } catch { }

				};

				DockPanel.SetDock(btnOpenFolder, Dock.Left);

				btnDock.Children.Add(btnOpenFolder);



				mainStack.Children.Add(btnDock);

				win.Content = mainStack;

				win.Owner = this;

				win.WindowStartupLocation = WindowStartupLocation.CenterOwner;

				win.Show();

			}

		});

	}



	private void HandleTeacherCommand(string cmd)

		{

			if (string.IsNullOrWhiteSpace(cmd)) return;

			if (cmd.Trim().StartsWith("{"))

			{

				HandleJsonCommand(cmd);

				return;

			}

			string[] parts = cmd.Split('|');

			if (parts.Length < 2)

			{

				return;

			}

			if (cmd == _lastCommandHash && DateTime.Now - _lastCommandTime < CommandDedupWindow)

			{

				Log.Debug("Skipping duplicate command (within {Window}s): {Cmd}", CommandDedupWindow.TotalSeconds, cmd);

				return;

			}

			_lastCommandHash = cmd;

			_lastCommandTime = DateTime.Now;

			switch (parts[1])

			{

			// ═══ LOI_VID_21 FIX: Xử lý lệnh đồng bộ phiên mới từ GV ═══

			case "UPDATE_SESSION":

				{

					// ClassCode & SessionSalt đã được cập nhật trực tiếp trong 

					// StudentNetworkClient.ReceiveLoopAsync (tầng mạng) trước khi sự kiện này kích hoạt.

					// Handler này chỉ ghi log và hiển thị thông báo nhẹ trên UI.

					Log.Information("[SessionSync] UI đã nhận lệnh đồng bộ phiên từ Giáo viên.");

					ShowNotification("Phiên học mới", "Giáo viên đã bắt đầu lớp học. Kết nối ổn định.", "#1B5E20");

				}

				break;

			case "POLICY":

				{

					try

					{

						bool blockInternet = false;

						bool eduOnly = false;

						bool blockSocial = false;

						bool blockGames = false;

						bool lockDesktop = false;

						bool showTeacher = false;

						bool quietMode = false;

						bool disableTask = false;

						bool blockUsb = false;

						bool blockApps = false;

						bool whitelistOnly = false;

						bool blockPrint = false;

						bool isReset = false;



						foreach (var part in parts)

						{

							if (part.StartsWith("internet=")) blockInternet = part.Substring("internet=".Length) == "True";

							else if (part.StartsWith("eduonly=")) eduOnly = part.Substring("eduonly=".Length) == "True";

							else if (part.StartsWith("social=")) blockSocial = part.Substring("social=".Length) == "True";

							else if (part.StartsWith("games=")) blockGames = part.Substring("games=".Length) == "True";

							else if (part.StartsWith("desktop=")) lockDesktop = part.Substring("desktop=".Length) == "True";

							else if (part.StartsWith("teacher=")) showTeacher = part.Substring("teacher=".Length) == "True";

							else if (part.StartsWith("quiet=")) quietMode = part.Substring("quiet=".Length) == "True";

							else if (part.StartsWith("taskbar=")) disableTask = part.Substring("taskbar=".Length) == "True";

							else if (part.StartsWith("usb=")) blockUsb = part.Substring("usb=".Length) == "True";

							else if (part.StartsWith("apps=")) blockApps = part.Substring("apps=".Length) == "True";

							else if (part.StartsWith("whitelist=")) whitelistOnly = part.Substring("whitelist=".Length) == "True";

							else if (part.StartsWith("print=")) blockPrint = part.Substring("print=".Length) == "True";

							else if (part.StartsWith("reset=")) isReset = part.Substring("reset=".Length) == "true";

						}



						if (isReset)

						{

							_isWebBlocked = false;

							_isWebWhitelistActive = false;

							if (Application.Current is App)

							{

								App.ClassControl.IsScreenLocked = false;

								App.ClassControl.ScreenLockType = "";

								App.ClassControl.IsSilenceActive = false;

							}

							CloseScreenLockOverlay();

							CloseScreenBroadcast();

							ShowNotification("Đã gỡ bỏ chính sách", "Giáo viên đã đặt lại tất cả chính sách bảo mật.", "#2E7D32");

							Log.Information("All policies reset to default by teacher.");

						}

						else

						{

							_isWebBlocked = blockInternet;

							_isWebWhitelistActive = whitelistOnly;



							if (lockDesktop)

							{

								if (Application.Current is App)

								{

									App.ClassControl.IsScreenLocked = true;

									App.ClassControl.ScreenLockType = "LOCK";

								}

								ShowScreenLockOverlay("Màn hình đã bị khóa", "GV đã áp dụng chính sách khóa màn hình học sinh.", "#C62828");

							}

							else

							{

								if (Application.Current is App && App.ClassControl.ScreenLockType == "LOCK")

								{

									App.ClassControl.IsScreenLocked = false;

									App.ClassControl.ScreenLockType = "";

									CloseScreenLockOverlay();

								}

							}



							int rulesCount = new[] { blockInternet, eduOnly, blockSocial, blockGames, lockDesktop, showTeacher, quietMode, disableTask, blockUsb, blockApps, whitelistOnly, blockPrint }.Count(x => x);

							ShowNotification("Chính sách bảo mật mới", $"Đã áp dụng {rulesCount} quy tắc bảo mật từ GV.", "#1976D2");

							Log.Information("Applied policy: InternetBlocked={I}, WhitelistActive={W}, DesktopLocked={D}", blockInternet, whitelistOnly, lockDesktop);

						}

					}

					catch (Exception exPolicy)

					{

						Log.Warning("Error parsing POLICY command: {Err}", exPolicy.Message);

					}

				}

				break;

			case "LOCK_KEYBOARD":

				{

					try

					{

						bool enabled = false;

						foreach (var part in parts)

						{

							if (part.StartsWith("enabled=")) enabled = part.Substring("enabled=".Length) == "true";

						}

						if (enabled)

						{

							KeyboardHookHelper.EnableHook();

							ShowNotification("⌨️ Khóa bàn phím + chuột", "GV đã khóa bàn phím và chuột của em.", "#E65100");

							Log.Information("Keyboard and mouse locked by teacher.");

						}

						else

						{

							KeyboardHookHelper.DisableHook();

							ShowNotification("⌨️ Mở khóa bàn phím + chuột", "GV đã mở khóa bàn phím và chuột của em.", "#1565C0");

							Log.Information("Keyboard and mouse unlocked by teacher.");

						}

					}

					catch (Exception exKbd)

					{

						Log.Warning("Error parsing LOCK_KEYBOARD command: {Err}", exKbd.Message);

					}

				}

				break;

			case "QUIET_MODE":

				{

					try

					{

						bool enabled = false;

						foreach (var part in parts)

						{

							if (part.StartsWith("enabled=")) enabled = part.Substring("enabled=".Length).ToLower() == "true";

						}

						if (enabled)

						{

							ShowNotification("Chế độ im lặng", "GV đã bật chế độ im lặng.", "#7B1FA2");

							Log.Information("Quiet mode enabled by teacher.");

						}

						else

						{

							ShowNotification("Tắt chế độ im lặng", "GV đã tắt chế độ im lặng.", "#2E7D32");

							Log.Information("Quiet mode disabled by teacher.");

						}

					}

					catch (Exception exQuiet)

					{

						Log.Warning("Error parsing QUIET_MODE command: {Err}", exQuiet.Message);

					}

				}

				break;

			case "EXIT_PIN_CONFIG":

				if (parts.Length >= 3)

				{

					if (bool.TryParse(parts[2], out bool isPinReq))

					{

						_isExitPinRequired = isPinReq;

					}

					if (parts.Length >= 4)

					{

						_exitPinCode = parts[3];

					}

					Log.Information("Cấu hình Exit PIN nhận từ GV: Yêu cầu={Req}, PIN={Pin}", _isExitPinRequired, _exitPinCode);



					// Save configuration locally

					try

					{

						string profilePath = AppPaths.StudentProfileFile;

						StudentProfileCache profile = null;

						if (File.Exists(profilePath))

						{

							string json = QASmartClass.StudentClient.Services.SecureProfileHelper.ReadProfileText(profilePath);

							profile = JsonSerializer.Deserialize<StudentProfileCache>(json);

						}

						if (profile == null)

						{

							profile = new StudentProfileCache();

						}

						profile.IsExitPinRequired = _isExitPinRequired;

						profile.ExitPinCode = _exitPinCode;

						

						string updatedJson = JsonSerializer.Serialize(profile);

						QASmartClass.StudentClient.Services.SecureProfileHelper.WriteProfileText(profilePath, updatedJson);

						Log.Information("Đã lưu cấu hình Exit PIN cục bộ: Yêu cầu={Req}, PIN={Pin}", _isExitPinRequired, _exitPinCode);

					}

					catch (Exception exPin)

					{

						Log.Warning("Lỗi lưu cấu hình Exit PIN cục bộ: {Err}", exPin.Message);

					}

				}

				break;

			case "FOCUS_MODE":

				if (parts.Length >= 3)

				{

					string focusState = parts[2].ToUpper();

					_isFocusModeActive = (focusState == "ON" || focusState == "TRUE");

					Log.Information("[FocusMode] Focus Mode updated to: {State}", _isFocusModeActive);

					base.Dispatcher.Invoke(() =>

					{

						if (_isFocusModeActive)

						{

							if (quizBadge != null) quizBadge.Visibility = Visibility.Collapsed;

							if (assignmentBadge != null) assignmentBadge.Visibility = Visibility.Collapsed;

							if (stuMsgBadge != null) stuMsgBadge.Visibility = Visibility.Collapsed;

						}

						else

						{

							UpdateDynamicBadges();

							if (_stuUnreadCount > 0)

							{

								stuMsgBadge.Visibility = Visibility.Visible;

							}

						}

					});

				}

				break;

			 case "FOCUS_TIMER_SYNC":

				  if (parts.Length >= 8)

				  {

					if (int.TryParse(parts[2], out int focusMins) &&

						int.TryParse(parts[3], out int breakMins) &&

						int.TryParse(parts[4], out int remainingSecs) &&

						bool.TryParse(parts[5], out bool isRunning) &&

						bool.TryParse(parts[6], out bool isBreak) &&

						int.TryParse(parts[7], out int sessionCount))

					{

						bool autoStart = false;

						string base64Goals = "";

						if (parts.Length >= 9)

						{

							bool.TryParse(parts[8], out autoStart);

						}

						if (parts.Length >= 10)

						{

							base64Goals = parts[9];

						}



						base.Dispatcher.Invoke(delegate

						{

							var timerTool = FindActiveFocusTimerTool();

							if (timerTool != null)

							{

								timerTool.SyncFromTeacher(focusMins, breakMins, remainingSecs, isRunning, isBreak, sessionCount, autoStart, base64Goals);

							}

						});

					}

				  }

				  break;

			case "HMAC_KEY_SYNC":

				if (parts.Length >= 3)

				{

					QASmartClass.Utilities.SecurityKeyProvider.UpdateHmacKey(parts[2]);

					Log.Information("HMAC Key synchronized from teacher: {Key}", parts[2]);

				}

				break;

			case "TURN_GRANTED":

				if (parts.Length >= 3)

				{

					string targetCode = parts[2];

					var app = (QASmartTouch.App)Application.Current;

					if (targetCode == app.StudentNetwork.StudentCode)

					{

						// Auto-lower student's hand raised state

						app.StudentNetwork.IsHandRaised = false;



						string reason = app.StudentNetwork.LastRaiseReason ?? "Phát biểu";

						string popupMessage = "Đến lượt em phát biểu! Hãy tự tin phát biểu ý kiến nhé.";

						if (reason == "Xin ra ngoài")

						{

							popupMessage = "Thầy/Cô đã cho phép em ra ngoài. Hãy đi nhanh và sớm quay lại lớp học nhé!";

						}

						else if (reason == "Lỗi máy tính")

						{

							popupMessage = "Thầy/Cô đã ghi nhận sự cố máy tính của em và sẽ hỗ trợ em ngay.";

						}



						base.Dispatcher.Invoke(() =>

						{

							try

							{

								System.Media.SystemSounds.Asterisk.Play();

							}

							catch (Exception ex)

							{

								Log.Warning("Failed to play turn granted system chime: {Err}. Falling back to motherboard beep.", ex.Message);

								Task.Run(() =>

								{

									try

									{

										// Play double beep (high pitch) as fallback on background thread

										System.Console.Beep(900, 150);

										System.Threading.Thread.Sleep(50);

										System.Console.Beep(900, 150);

									}

									catch (Exception exBeep)

									{

										Log.Warning("Motherboard beep fallback failed on background thread: {Err}", exBeep.Message);

									}

								});

							}

							MessageBox.Show(popupMessage, "Phát biểu", MessageBoxButton.OK, MessageBoxImage.Information);

						});

					}

				}

				break;

			case "CLASS_CHAT_MUTE":

				if (parts.Length >= 3)

				{

					bool isMuted = parts[2].Equals("True", StringComparison.OrdinalIgnoreCase);

					base.Dispatcher.Invoke(() =>

					{

						_chatPage ??= new StudentChatPage();

						_chatPage.SetChatMuteStatus(isMuted);

					});

				}

				break;

			case "CLASS_CHAT_MUTE_STUDENT":

				if (parts.Length >= 4)

				{

					string targetCode = parts[2];

					bool isMuted = parts[3].Equals("True", StringComparison.OrdinalIgnoreCase);

					base.Dispatcher.Invoke(() =>

					{

						_chatPage ??= new StudentChatPage();

						_chatPage.SetIndividualChatMuteStatus(targetCode, isMuted);

					});

				}

				break;

			case "CLASS_CHAT_ANONYMOUS":

				if (parts.Length >= 3)

				{

					bool isAnonymous = parts[2].Equals("True", StringComparison.OrdinalIgnoreCase);

					base.Dispatcher.Invoke(() =>

					{

						_chatPage ??= new StudentChatPage();

						_chatPage.SetAnonymousFeedbackStatus(isAnonymous);

					});

				}

				break;

			case "LOCK":

				if (Application.Current is App)

				{

					App.ClassControl.IsScreenLocked = true;

					App.ClassControl.ScreenLockType = "LOCK";

				}

				ShowScreenLockOverlay("Màn hình đã bị khóa", "GV đã khóa màn hình. Vui lòng chờ.", "#C62828");

				ShowNotification("Khóa màn hình", "GV đã khóa màn hình của em", "#C62828");

				Log.Information("Screen locked by teacher (LOCK)");

				break;

			case "HAND_LOWER":

				{

					var app = (QASmartTouch.App)Application.Current;

					app.StudentNetwork.IsHandRaised = false;

					Log.Information("Hand lowered by teacher command (HAND_LOWER)");

				}

				break;

			case "UNLOCK":

				if (Application.Current is App)

				{

					App.ClassControl.IsScreenLocked = false;

					App.ClassControl.ScreenLockType = "";

					App.ClassControl.IsSilenceActive = false;

				}

				base.Dispatcher.Invoke(delegate

				{

					CloseScreenLockOverlay();

					CloseTeacherWarning();

					if (_silenceOverlay != null)

					{

						try

						{

							Grid grid = (Grid)base.Content;

							grid.Children.Remove(_silenceOverlay);

						}

						catch (Exception ex6)

						{

							Log.Warning("UNLOCK remove silence: {Err}", ex6.Message);

						}

						_silenceOverlay = null;

					}

					ShowNotification("Đã mở khóa", "GV đã mở khóa màn hình", "#2E7D32");

				});

				Log.Information("Screen unlocked by teacher (UNLOCK)");

				break;

			case "QUIZ_START":

				NavigateTo("S3");

				ShowNotification("\ud83d\udcdd Bài kiểm tra", "GV đã phát bài kiểm tra mới!", "#E65100");

				UpdateDynamicBadges();

				break;

			case "QUIZ_DATA":

				if (parts.Length >= 4)

				{

					if (int.TryParse(parts[2], out int quizId))

					{

						string questionsJson = string.Join("|", parts.Skip(3));

						SaveQuizDataToDatabase(quizId, questionsJson);

					}

				}

				break;

			case "QUIZ_END":

				ShowNotification("\ud83d\udcdd Kết thúc kiểm tra", "GV đã kết thúc bài kiểm tra!", "#1976D2");

				UpdateDynamicBadges();

				break;

			case "QUIZ_GRADE":

				if (parts.Length >= 7)

				{

					if (int.TryParse(parts[2], out int gradeQuizId))

					{

						double.TryParse(parts[3], out double score);

						int.TryParse(parts[4], out int correctCount);

						int.TryParse(parts[5], out int totalQuestions);

						string correctAnswersJson = parts[6];



						UpdateQuizResultAndCorrectAnswers(gradeQuizId, score, correctCount, totalQuestions, correctAnswersJson);

					}

				}

				break;

			case "LESSON_START":

				if (parts.Length >= 3 && int.TryParse(parts[2], out var startLessonId))

				{

					App.LessonState.IsLessonActive = true;

					App.LessonState.ActiveLessonId = startLessonId;

					App.LessonState.ActiveLessonStage = 3; // Mặc định là giai đoạn Giảng bài mới

				}

				NavigateTo("S2");

				ShowNotification("\ud83d\udcd6 Tiết học bắt đầu", "GV đã bắt đầu bài giảng. Hãy tập trung!", "#2E7D32");

				Log.Information("Lesson started by teacher, navigated to lesson page. ActiveLessonId: {Id}", App.LessonState.ActiveLessonId);

				break;

			case "LESSON_STAGE":

			{

				if (parts.Length >= 3 && int.TryParse(parts[2], out var result2))

				{

					App.LessonState.ActiveLessonStage = result2;

					if (1 == 0)

					{

					}

					string text3 = result2 switch

					{

						1 => "Chuẩn bị", 

						2 => "Kiểm tra bài cũ", 

						3 => "Bài giảng mới", 

						4 => "Thực hành / Trò chơi", 

						5 => "Đánh giá", 

						6 => "Tổng kết", 

						_ => $"Giai đoạn {result2}", 

					};

					if (1 == 0)

					{

					}

					string text6 = text3;

					txtPageTitle.Text = "\ud83d\udcd6 Bài giảng — " + text6;

					switch (result2)

					{

					case 2:

						ShowNotification("\ud83d\udccb Kiểm tra bài cũ", "GV đang kiểm tra bài cũ. Hãy chuẩn bị!", "#FF9800");

						break;

					case 3:

						ShowNotification("\ud83d\udcda Bài giảng mới", "GV đang giảng bài mới. Hãy tập trung!", "#1976D2");

						break;

					case 4:

						ShowNotification("\ud83c\udfae Thực hành", "GV đang tổ chức hoạt động. Hãy tham gia!", "#7B1FA2");

						break;

					case 5:

						ShowNotification("\ud83c\udfc6 Đánh giá", "GV đang đánh giá. Chờ kết quả!", "#E65100");

						break;

					}

					try

					{

						if (contentFrame.Content is StudentLessonPage studentLessonPage2)

						{

							studentLessonPage2.SetStageView(result2);

						}

					}

					catch (Exception ex2)

					{

						Log.Warning("SetStageView error: {Err}", ex2.Message);

					}

				}

				Log.Information("Lesson stage changed to {Stage}", (parts.Length >= 3) ? parts[2] : "?");

				break;

			}

			case "LESSON_FOCUS":

			{

				if (parts.Length >= 3 && int.TryParse(parts[2], out var result6))

				{

					string text12 = ((parts.Length >= 4) ? parts[3] : "Nội dung");

					if (Application.Current is App)

					{

						App.FocusState.ActiveFocusSort = result6;

						App.FocusState.ActiveFocusType = text12;

					}

					if (_currentPage != "S2")

					{

						NavigateTo("S2");

					}

					try

					{

						if (contentFrame.Content is StudentLessonPage studentLessonPage3)

						{

							studentLessonPage3.FocusBlock(result6, text12);

						}

					}

					catch (Exception ex5)

					{

						Log.Warning("Focus page error: {Err}", ex5.Message);

					}

					ShowNotification("\ud83c\udfaf Tập trung", $"GV yêu cầu xem: {text12} (#{result6})", "#1565C0");

				}

				Log.Information("Lesson focus command received");

				break;

			}

			case "LESSON_UNFOCUS":

				if (Application.Current is App)

				{

					App.FocusState.ActiveFocusSort = -1;

					App.FocusState.ActiveFocusType = "";

				}

				try

				{

					if (contentFrame.Content is StudentLessonPage studentLessonPage)

					{

						studentLessonPage.UnfocusAll();

					}

				}

				catch (Exception ex)

				{

					Log.Warning("Unfocus page error: {Err}", ex.Message);

				}

				ShowNotification("\ud83d\udd13 Bỏ Focus", "GV đã bỏ chế độ tập trung. Em có thể xem tất cả nội dung.", "#666");

				Log.Information("Lesson unfocus command received");

				break;

			case "SCREEN_BROADCAST":

			case "SCREEN_BROADCAST_START":

				_lastBroadcastUpdateTime = DateTime.UtcNow;

				_lastTcpCommandTime = DateTime.UtcNow; // PHASE6-HOTFIX: Dual-channel TCP heartbeat

				var cleanPartsList2 = new System.Collections.Generic.List<string>(parts);

				if (cleanPartsList2.Count > 0 && cleanPartsList2[cleanPartsList2.Count - 1].StartsWith("id="))

				{

					cleanPartsList2.RemoveAt(cleanPartsList2.Count - 1);

				}

				var activeParts2 = cleanPartsList2.ToArray();



				if (activeParts2.Length >= 3)
				{
					string imagePath2 = activeParts2[2];
					string token = "";

					Log.Information("[StudentShell] Nhận lệnh phát màn hình từ GV. Lệnh thô: {RawCmd}", string.Join("|", activeParts2));

					// Đồng bộ cấu hình động bảo mật & thay đổi giao thức (V2.2.2)
					string protocol = "UDP_MULTICAST";
					int customPort = -1;
					string customKey = "";
					int customTtl = -1;

					foreach (var part in activeParts2)
					{
						if (part.Contains('='))
						{
							var kv = part.Split('=');
							if (kv.Length == 2)
							{
								string key = kv[0];
								string val = kv[1];
								if (key == "PROTOCOL") protocol = val;
								else if (key == "PORT" && int.TryParse(val, out int p)) customPort = p;
								else if (key == "KEY") customKey = val;
								else if (key == "TTL" && int.TryParse(val, out int t)) customTtl = t;
							}
						}
					}

					_currentBroadcastProtocol = protocol;
					Log.Information("[StudentShell] Bóc tách tham số: PROTOCOL={Protocol}, PORT={Port}, KEY={Key}, TTL={Ttl}", 
						protocol, customPort > 0 ? customPort : QASmartClass.Services.UdpScreenBroadcastService.MulticastPort, 
						customKey, customTtl > 0 ? customTtl : QASmartClass.Services.UdpScreenBroadcastService.MulticastTTL);
					
					// Giải phóng bộ thu cũ để tránh tranh chấp/giữ port
					QASmartClass.Services.UdpScreenBroadcastService.Instance.StopReceiver();

					if (customPort > 0)
					{
						QASmartClass.Services.UdpScreenBroadcastService.MulticastPort = customPort;
					}
					if (!string.IsNullOrEmpty(customKey))
					{
						QASmartClass.Services.UdpScreenBroadcastService.EncryptionKey = customKey;
					}
					if (customTtl > 0)
					{
						QASmartClass.Services.UdpScreenBroadcastService.MulticastTTL = customTtl;
					}

					if (activeParts2.Length >= 4)

					{

						if (int.TryParse(activeParts2[activeParts2.Length - 1], out int portVal))

						{

							_currentTeacherWebPort = portVal;

							token = "";

						}

						else

						{

							token = activeParts2[activeParts2.Length - 1];

							if (int.TryParse(activeParts2[activeParts2.Length - 2], out int portVal2))

							{

								_currentTeacherWebPort = portVal2;

							}

						}

					}

					bool isForce = cmd.Contains("FORCE_WATCH");

					if (isForce)

					{

						_userDismissedBroadcast = false;

						if (btnShowBroadcast != null) btnShowBroadcast.Visibility = Visibility.Collapsed;

					}



					if (!isForce && _userDismissedBroadcast)

					{

						Log.Information("Screen broadcast start ignored because student dismissed it.");

						if (btnShowBroadcast != null) btnShowBroadcast.Visibility = Visibility.Visible;

					}

					else

					{

						if (btnShowBroadcast != null) btnShowBroadcast.Visibility = Visibility.Collapsed;

						if (_broadcastOverlay == null)

						{

							ShowScreenBroadcast(imagePath2, isForce, token);

						}

						else

						{

							_currentBroadcastToken = token;

							ApplyKioskMode(isForce);

							UpdateScreenBroadcast(imagePath2);

						}

					}

					ShowNotification("Màn hình giáo viên", "GV đang chia sẻ màn hình. Hãy theo dõi!", "#7B1FA2");

				}

				Log.Information("Screen broadcast START received");

				break;

			case "SCREEN_BROADCAST_UPDATE":

				_lastBroadcastUpdateTime = DateTime.UtcNow;

				_lastTcpCommandTime = DateTime.UtcNow; // PHASE6-HOTFIX: Dual-channel TCP heartbeat

				var cleanPartsListUpdate = new System.Collections.Generic.List<string>(parts);

				if (cleanPartsListUpdate.Count > 0 && cleanPartsListUpdate[cleanPartsListUpdate.Count - 1].StartsWith("id="))

				{

					cleanPartsListUpdate.RemoveAt(cleanPartsListUpdate.Count - 1);

				}

				var activePartsUpdate = cleanPartsListUpdate.ToArray();



				if (activePartsUpdate.Length >= 3)

				{

					string imagePath = activePartsUpdate[2];

					string token = "";

					if (activePartsUpdate.Length >= 4)

					{

						if (int.TryParse(activePartsUpdate[activePartsUpdate.Length - 1], out int portVal))

						{

							_currentTeacherWebPort = portVal;

							token = "";

						}

						else

						{

							token = activePartsUpdate[activePartsUpdate.Length - 1];

							if (int.TryParse(activePartsUpdate[activePartsUpdate.Length - 2], out int portVal2))

							{

								_currentTeacherWebPort = portVal2;

							}

						}

					}

					if (_broadcastOverlay == null)

					{

						bool isForceUpdate = cmd.Contains("FORCE_WATCH");

						if (isForceUpdate)

						{

							_userDismissedBroadcast = false;

							if (btnShowBroadcast != null) btnShowBroadcast.Visibility = Visibility.Collapsed;

						}



						if (!isForceUpdate && _userDismissedBroadcast)

						{

							Log.Information("Screen broadcast update ignored because student dismissed it.");

							if (btnShowBroadcast != null) btnShowBroadcast.Visibility = Visibility.Visible;

						}

						else

						{

							if (btnShowBroadcast != null) btnShowBroadcast.Visibility = Visibility.Collapsed;

							ShowScreenBroadcast(imagePath, isForceUpdate, token);

						}

					}

					else

					{

						UpdateScreenBroadcast(imagePath);

					}

				}

				break;

			case "SCREEN_BROADCAST_STOP":

				_userDismissedBroadcast = false; // PHASE6-GD2

				CloseScreenBroadcast();

				ShowNotification("\ud83d\udcfa Dừng phát bảng", "GV đã tắt chế độ phát bảng trắng.", "#666");

				Log.Information("Screen broadcast STOP received");

				break;

			case "VNC_BROADCAST_START":
				_lastBroadcastUpdateTime = DateTime.UtcNow;
				_lastTcpCommandTime = DateTime.UtcNow;
				if (parts.Length >= 5)
				{
					string ip = parts[2];
					string teacherIP = (((App)Application.Current)?.StudentNetwork)?.ServerIP;
					if (!string.IsNullOrEmpty(teacherIP))
					{
						ip = teacherIP; // Ghi đè bằng IP giáo viên đang kết nối thực tế để tránh sai adapter mạng ảo (V2.2.5)
					}
					int port = 5901;
					int.TryParse(parts[3], out port);
					string sessionCode = parts[4];
					bool isForce = false;
					if (parts.Length >= 6)
					{
						isForce = parts[5].Contains("FORCE_WATCH");
					}
					ShowVncBroadcast(ip, port, sessionCode, isForce);
				}
				break;

			case "VNC_BROADCAST_STOP":
				CloseVncBroadcast();
				ShowNotification("📺 Dừng phát VNC", "GV đã tắt chế độ phát VNC.", "#666");
				Log.Information("VNC broadcast STOP received");
				break;

			case "VNC_HEARTBEAT":
				_lastBroadcastUpdateTime = DateTime.UtcNow;
				_lastTcpCommandTime = DateTime.UtcNow;
				break;

			// === PHASE6.1-BUG2: BROADCAST_PAUSE/RESUME handlers ===

			case "BROADCAST_PAUSE":

				_isBroadcastPausedByTeacher = true;

				_lastTcpCommandTime = DateTime.UtcNow;

				_pauseStartTime = DateTime.UtcNow;

				Log.Information("[Broadcast] Teacher PAUSED");

				Dispatcher.Invoke(() =>

				{

					if (_broadcastWarningTextBlock != null)

					{

						_broadcastWarningTextBlock.Text = "\u23f8\ufe0f GV t\u1ea1m d\u1eebng";

						_broadcastWarningTextBlock.Visibility = Visibility.Visible;

					}

					if (_broadcastOverlay != null) _broadcastOverlay.Opacity = 0.85;

				});

				break;

			case "BROADCAST_RESUME":

				_isBroadcastPausedByTeacher = false;

				_pauseStartTime = DateTime.MinValue;

				_lastBroadcastUpdateTime = DateTime.UtcNow;

				_lastTcpCommandTime = DateTime.UtcNow;

				Log.Information("[Broadcast] Teacher RESUMED");

				Dispatcher.Invoke(() =>

				{

					if (_broadcastWarningTextBlock != null) _broadcastWarningTextBlock.Visibility = Visibility.Collapsed;

					if (_broadcastOverlay != null) _broadcastOverlay.Opacity = 1.0;

				});

				break;

			case "SESSION_CONFIG":

				{

					try

					{

						foreach (var part in parts.Skip(2))

						{

							var kv = part.Split('=');

							if (kv.Length == 2)

							{

								string key = kv[0];

								string val = kv[1];

								if (key == "Broadcast_UdpHeartbeatTimeout" && double.TryParse(val, out double tVal))

								{

									_broadcastHeartbeatTimeout = tVal;

									Log.Information("Broadcast heartbeat timeout updated to {Val}s", tVal);

								}

								else if (key == "Broadcast_EmergencyOTP" && !string.IsNullOrEmpty(val))

								{

									QASmartClass.StudentClient.Services.KeyboardHookHelper.EmergencyOTP = val;

									Log.Information("[StudentShell] UPGRADE_11: Emergency OTP đã nhận từ GV.");

								}

								// === UPGRADE_13: Cấu hình chế độ truyền phát siêu tốc ===
								else if (key == "Broadcast_TurboMode")

								{

									_isTurboModeActive = val.ToLower() == "true";

									Log.Information("[StudentShell] UPGRADE_13: Nhận chế độ Turbo Mode = {Val}", _isTurboModeActive);

									base.Dispatcher.Invoke(() => ApplyTurboModeOptimization());

								}
								else if (key == "MulticastIP" && !string.IsNullOrEmpty(val))
								{
									QASmartClass.Services.UdpScreenBroadcastService.MulticastIP = val;
									Log.Information("[StudentShell] MulticastIP updated to {Val}", val);
								}
								else if (key == "MulticastPort" && int.TryParse(val, out int pVal))
								{
									QASmartClass.Services.UdpScreenBroadcastService.MulticastPort = pVal;
									Log.Information("[StudentShell] MulticastPort updated to {Val}", pVal);
								}
								else if (key == "MulticastTTL" && int.TryParse(val, out int tVal2))
								{
									QASmartClass.Services.UdpScreenBroadcastService.MulticastTTL = tVal2;
									Log.Information("[StudentShell] MulticastTTL updated to {Val}", tVal2);
								}

								else if (key == "Broadcast_TurboStudentBg")

								{

									_turboStudentBg = val;

									Log.Information("[StudentShell] UPGRADE_13: Nhận cấu hình nền Turbo = {Bg}", val);

									base.Dispatcher.Invoke(() => ApplyTurboModeOptimization());

								}

							}

						}

					}

					catch (Exception ex)

					{

						Log.Warning("Error handling SESSION_CONFIG: {Err}", ex.Message);

					}

				}

				break;

			case "WHITEBOARD_DRAW":

				if (parts.Length < 5 || _broadcastInkCanvas == null)

				{

					break;

				}

				base.Dispatcher.Invoke(delegate

				{

					try

					{

						Color color = (Color)ColorConverter.ConvertFromString(parts[2]);

						double num = double.Parse(parts[3], CultureInfo.InvariantCulture);

						double scaleX = 1.0;

						double scaleY = 1.0;

						if (parts.Length >= 7 &&

							double.TryParse(parts[5], NumberStyles.Any, CultureInfo.InvariantCulture, out var teacherWidth) &&

							double.TryParse(parts[6], NumberStyles.Any, CultureInfo.InvariantCulture, out var teacherHeight) &&

							teacherWidth > 0 && teacherHeight > 0 &&

							_broadcastInkCanvas.ActualWidth > 0 && _broadcastInkCanvas.ActualHeight > 0)

						{

							scaleX = _broadcastInkCanvas.ActualWidth / teacherWidth;

							scaleY = _broadcastInkCanvas.ActualHeight / teacherHeight;

						}

						double scaledThickness = num * ((scaleX + scaleY) / 2);

						string[] array = parts[4].Split(';');

						StylusPointCollection stylusPointCollection = new StylusPointCollection();

						string[] array2 = array;

						foreach (string text14 in array2)

						{

							string[] array3 = text14.Split(',');

							if (array3.Length == 2 && double.TryParse(array3[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var result7) && double.TryParse(array3[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var result8))

							{

								stylusPointCollection.Add(new StylusPoint(result7 * scaleX, result8 * scaleY));

							}

						}

						if (stylusPointCollection.Count > 0)

						{

							DrawingAttributes drawingAttributes = new DrawingAttributes

							{

								Color = color,

								Width = scaledThickness,

								Height = scaledThickness

							};

							Stroke item = new Stroke(stylusPointCollection, drawingAttributes);

							_broadcastInkCanvas.Strokes.Add(item);

						}

					}

					catch (Exception ex6)

					{

						Log.Warning("WHITEBOARD_DRAW draw error: {Err}", ex6.Message);

					}

				});

				break;

			case "WHITEBOARD_SHAPE":
				if (parts.Length < 7 || _broadcastShapeCanvas == null)
				{
					break;
				}
				base.Dispatcher.Invoke(delegate
				{
					try
					{
						string shapeType = parts[2];
						string[] coords = parts[3].Split('|');
						Color color = (Color)ColorConverter.ConvertFromString(parts[4]);
						double thickness = double.Parse(parts[5], CultureInfo.InvariantCulture);

						double scaleX = 1.0;
						double scaleY = 1.0;
						if (parts.Length >= 8 &&
							double.TryParse(parts[6], NumberStyles.Any, CultureInfo.InvariantCulture, out var teacherWidth) &&
							double.TryParse(parts[7], NumberStyles.Any, CultureInfo.InvariantCulture, out var teacherHeight) &&
							teacherWidth > 0 && teacherHeight > 0 &&
							_broadcastShapeCanvas.ActualWidth > 0 && _broadcastShapeCanvas.ActualHeight > 0)
						{
							scaleX = _broadcastShapeCanvas.ActualWidth / teacherWidth;
							scaleY = _broadcastShapeCanvas.ActualHeight / teacherHeight;
						}

						double scaledThickness = thickness * ((scaleX + scaleY) / 2);
						SolidColorBrush brush = new SolidColorBrush(color);

						if (coords.Length >= 2)
						{
							string[] p1 = coords[0].Split(',');
							string[] p2 = coords[1].Split(',');
							if (p1.Length == 2 && p2.Length == 2 &&
								double.TryParse(p1[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var x1) &&
								double.TryParse(p1[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var y1) &&
								double.TryParse(p2[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var x2) &&
								double.TryParse(p2[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var y2))
							{
								x1 *= scaleX; y1 *= scaleY;
								x2 *= scaleX; y2 *= scaleY;

								if (shapeType == "Rectangle")
								{
									var rect = new System.Windows.Shapes.Rectangle
									{
										Width = Math.Abs(x2 - x1),
										Height = Math.Abs(y2 - y1),
										Stroke = brush,
										StrokeThickness = scaledThickness,
										Fill = Brushes.Transparent
									};
									Canvas.SetLeft(rect, Math.Min(x1, x2));
									Canvas.SetTop(rect, Math.Min(y1, y2));
									_broadcastShapeCanvas.Children.Add(rect);
								}
								else if (shapeType == "Ellipse")
								{
									var ellipse = new System.Windows.Shapes.Ellipse
									{
										Width = Math.Abs(x2 - x1),
										Height = Math.Abs(y2 - y1),
										Stroke = brush,
										StrokeThickness = scaledThickness,
										Fill = Brushes.Transparent
									};
									Canvas.SetLeft(ellipse, Math.Min(x1, x2));
									Canvas.SetTop(ellipse, Math.Min(y1, y2));
									_broadcastShapeCanvas.Children.Add(ellipse);
								}
								else if (shapeType == "Line")
								{
									var line = new System.Windows.Shapes.Line
									{
										X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
										Stroke = brush,
										StrokeThickness = scaledThickness,
										StrokeStartLineCap = PenLineCap.Round,
										StrokeEndLineCap = PenLineCap.Round
									};
									_broadcastShapeCanvas.Children.Add(line);
								}
								else if (shapeType == "Arrow")
								{
									double num = Math.Atan2(y2 - y1, x2 - x1);
									double num2 = Math.Max(scaledThickness * 4.0, 12.0);
									double num3 = Math.PI / 6.0;
									Point endPoint = new Point(x2 - num2 * Math.Cos(num - num3), y2 - num2 * Math.Sin(num - num3));
									Point endPoint2 = new Point(x2 - num2 * Math.Cos(num + num3), y2 - num2 * Math.Sin(num + num3));
									GeometryGroup geometryGroup = new GeometryGroup();
									geometryGroup.Children.Add(new LineGeometry(new Point(x1, y1), new Point(x2, y2)));
									geometryGroup.Children.Add(new LineGeometry(new Point(x2, y2), endPoint));
									geometryGroup.Children.Add(new LineGeometry(new Point(x2, y2), endPoint2));
									var path = new System.Windows.Shapes.Path
									{
										Data = geometryGroup,
										Stroke = brush,
										StrokeThickness = scaledThickness,
										StrokeStartLineCap = PenLineCap.Round,
										StrokeEndLineCap = PenLineCap.Round
									};
									_broadcastShapeCanvas.Children.Add(path);
								}
							}
						}
					}
					catch (Exception ex)
					{
						Log.Warning("WHITEBOARD_SHAPE draw error: {Err}", ex.Message);
					}
				});
				break;

			case "WHITEBOARD_TEXT":
				if (parts.Length < 6 || _broadcastShapeCanvas == null)
				{
					break;
				}
				base.Dispatcher.Invoke(delegate
				{
					try
					{
						string content = parts[2];
						string[] posParts = parts[3].Split(',');
						Color color = (Color)ColorConverter.ConvertFromString(parts[4]);
						double fontSize = double.Parse(parts[5], CultureInfo.InvariantCulture);

						double scaleX = 1.0;
						double scaleY = 1.0;
						if (parts.Length >= 8 &&
							double.TryParse(parts[6], NumberStyles.Any, CultureInfo.InvariantCulture, out var teacherWidth) &&
							double.TryParse(parts[7], NumberStyles.Any, CultureInfo.InvariantCulture, out var teacherHeight) &&
							teacherWidth > 0 && teacherHeight > 0 &&
							_broadcastShapeCanvas.ActualWidth > 0 && _broadcastShapeCanvas.ActualHeight > 0)
						{
							scaleX = _broadcastShapeCanvas.ActualWidth / teacherWidth;
							scaleY = _broadcastShapeCanvas.ActualHeight / teacherHeight;
						}

						if (posParts.Length == 2 &&
							double.TryParse(posParts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var px) &&
							double.TryParse(posParts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var py))
						{
							px *= scaleX;
							py *= scaleY;

							var textBlock = new TextBlock
							{
								Text = content,
								FontSize = fontSize * ((scaleX + scaleY) / 2),
								Foreground = new SolidColorBrush(color),
								FontFamily = new FontFamily("Segoe UI")
							};
							Canvas.SetLeft(textBlock, px);
							Canvas.SetTop(textBlock, py);
							_broadcastShapeCanvas.Children.Add(textBlock);
						}
					}
					catch (Exception ex)
					{
						Log.Warning("WHITEBOARD_TEXT draw error: {Err}", ex.Message);
					}
				});
				break;

			case "WHITEBOARD_CLEAR":
				base.Dispatcher.Invoke(delegate
				{
					try
					{
						if (_broadcastInkCanvas != null)
						{
							_broadcastInkCanvas.Strokes.Clear();
						}
						if (_broadcastShapeCanvas != null)
						{
							_broadcastShapeCanvas.Children.Clear();
						}
					}
					catch (Exception ex6)
					{
						Log.Warning("WHITEBOARD_CLEAR error: {Err}", ex6.Message);
					}
				});
				break;

			case "LESSON_END":

				ShowNotification("\ud83c\udfc1 Kết thúc tiết học", "GV đã kết thúc tiết học. Cảm ơn em!", "#2E7D32");

				txtPageTitle.Text = ((_currentPage == "S2") ? "\ud83d\udcd6 Bài giảng — Đã kết thúc" : txtPageTitle.Text);

				Log.Information("Lesson ended by teacher");

				break;

			case "HOMEWORK":

			{

				string text13 = ((parts.Length >= 3) ? parts[2] : "Bài tập về nhà");

				ShowNotification("\ud83d\udcdd Bài tập về nhà", text13, "#E65100");

				Log.Information("Homework received: {Text}", text13);

				break;

			}

			case "FILE_BROADCAST":

				if (parts.Length >= 3)

				{

					string text = parts[2];

					if (parts.Length >= 4)

					{

						if (int.TryParse(parts[3], out int portVal))

						{

							_currentTeacherWebPort = portVal;

							text = parts[2];

						}

						else

						{

							text = parts[3];

						}

					}

					if (parts.Length >= 5)

					{

						_currentBroadcastToken = parts[4];

					}

					string text2 = Path.GetExtension(text).ToLower();

					if (1 == 0)

					{

					}

					string text3;

					switch (text2)

					{

					case ".png":

					case ".jpg":

					case ".jpeg":

					case ".gif":

					case ".bmp":

						text3 = "IMAGE";

						break;

					case ".mp4":

					case ".avi":

					case ".mkv":

						text3 = "VIDEO";

						break;

					case ".pdf":

						text3 = "PDF";

						break;

					default:

						text3 = "FILE";

						break;

					}

					if (1 == 0)

					{

					}

					string text4 = text3;

					ShowFileBroadcast(text4, text);

					if (1 == 0)

					{

					}

					text3 = text4 switch

					{

						"IMAGE" => "\ud83d\uddbc\ufe0f Ảnh", 

						"VIDEO" => "\ud83c\udfac Video", 

						"PDF" => "\ud83d\udcc4 PDF", 

						_ => "\ud83d\udcc1 Tài liệu", 

					};

					if (1 == 0)

					{

					}

					string text5 = text3;

					ShowNotification(text5 + " từ GV", "GV đã phát " + Path.GetFileName(text), "#1976D2");

				}

				Log.Information("File broadcast received: {Cmd}", cmd);

				break;

			case "NOTICE":

				if (parts.Length >= 5)

				{

					string type = parts[2];

					int result4;

					int durationSec2 = (int.TryParse(parts[3], out result4) ? result4 : 30);

					string title2 = parts[4];

					string body = "";

					if (parts.Length >= 6)

					{

						var bodyParts = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Skip(parts, 5));

						if (bodyParts.Count > 0 && bodyParts[bodyParts.Count - 1].StartsWith("id=", StringComparison.OrdinalIgnoreCase))

						{

							bodyParts.RemoveAt(bodyParts.Count - 1);

						}

						body = string.Join("|", bodyParts);

					}

					ShowNoticePopup(title2, body, type, durationSec2);

				}

				Log.Information("Notice received: {Cmd}", cmd);

				break;

			case "BLOCK_WEB_ON":

				_isWebBlocked = true;

				_isWebWhitelistActive = false;

				CloseSecureWebWindow();

				ShowNotification("Web đã khóa", "GV đã tắt tính năng duyệt Web trên máy của em.", "#C62828");

				Log.Information("Web access blocked by teacher");

				break;

			case "BLOCK_WEB_OFF":

				_isWebBlocked = false;

				ShowNotification("✅ Web đã mở", "GV đã cho phép truy cập Web trở lại.", "#2E7D32");

				Log.Information("Web access allowed by teacher");

				break;

			case "WEB_WHITELIST_ON":

				_isWebWhitelistActive = true;

				_isWebBlocked = false;

				ShowNotification("Chế độ giới hạn Web", "Em chỉ có thể truy cập các website được GV chỉ định.", "#FF9800");

				Log.Information("Web Whitelist mode active");

				break;

			case "WEB_WHITELIST_OFF":

				_isWebWhitelistActive = false;

				CloseSecureWebWindow();

				ShowNotification("Đã mở giới hạn Web", "Đã gỡ bỏ giới hạn truy cập website.", "#2E7D32");

				Log.Information("Web Whitelist mode inactive");

				break;

			case "WHITELIST_ADD":

				if (parts.Length >= 3)

				{

					string newUrl = parts[2];

					if (!string.IsNullOrEmpty(newUrl))

					{

						foreach (var urlItem in newUrl.Split(','))

						{

							AddUrlToLocalWhitelist(urlItem);

						}

					}

				}

				break;

			case "OPEN_URL":

				if (parts.Length >= 3)

				{

					string text7 = parts[2];

					if (!string.IsNullOrEmpty(text7))

					{

						if (_isWebBlocked)

						{

							ShowNotification("Không thể mở Web", "Giáo viên đã khóa tính năng duyệt Web.", "#C62828");

							Log.Information("Ignored OPEN_URL command because Web is blocked");

							break;

						}



						if (_isWebWhitelistActive)

						{

							AddUrlToLocalWhitelist(text7);

							OpenInSecureWebWindow(text7);

						}

						else

						{

							try

							{

								Process.Start(new ProcessStartInfo

								{

									FileName = text7,

									UseShellExecute = true

								});

								ShowNotification("Mở website", "GV yêu cầu mở: " + text7, "#7B1FA2");

							}

							catch (Exception ex3)

							{

								Log.Warning("Open URL failed: {Err}", ex3.Message);

							}

						}

					}

				}

				Log.Information("Open URL received: {Cmd}", cmd);

				break;

			case "REQUEST_SCREENSHOT":

				Task.Run(delegate

				{

					CaptureAndSendScreenshot();

				});

				break;

			case "LOCK_SCREEN":

				if (Application.Current is App)

				{

					App.ClassControl.IsScreenLocked = true;

					App.ClassControl.ScreenLockType = "LOCK";

				}

				ShowScreenLockOverlay("\ud83d\udd12 Màn hình đã bị khóa", "GV đã khóa màn hình. Vui lòng chờ.", "#C62828");

				ShowNotification("\ud83d\udd12 Khóa màn hình", "GV đã khóa màn hình của em", "#C62828");

				Log.Information("Screen locked by teacher");

				break;

			case "UNLOCK_SCREEN":

				if (Application.Current is App)

				{

					App.ClassControl.IsScreenLocked = false;

					App.ClassControl.ScreenLockType = "";

					App.ClassControl.IsSilenceActive = false;

				}

				base.Dispatcher.Invoke(delegate

				{

					CloseScreenLockOverlay();

					CloseTeacherWarning();

					if (_silenceOverlay != null)

					{

						try

						{

							Grid grid = (Grid)base.Content;

							grid.Children.Remove(_silenceOverlay);

						}

						catch (Exception ex6)

						{

							Log.Warning("UNLOCK remove silence: {Err}", ex6.Message);

						}

						_silenceOverlay = null;

					}

					ShowNotification("\ud83d\udd13 Đã mở khóa", "GV đã mở khóa màn hình", "#2E7D32");

				});

				Log.Information("Screen unlocked by teacher — all overlays cleared");

				break;

			case "BLACK_SCREEN":

				if (Application.Current is App)

				{

					App.ClassControl.IsScreenLocked = true;

					App.ClassControl.ScreenLockType = "BLACK";

				}

				ShowScreenLockOverlay("\ud83d\udcf4 Tắt màn hình", "GV đã tắt màn hình. Hãy chú ý lên bảng.", "#000000");

				ShowNotification("\ud83d\udcf4 Tắt màn hình", "GV đã tắt màn hình của em", "#424242");

				Log.Information("Black screen by teacher");

				break;

			case "BROADCAST_START":

			{

				if (Application.Current is App)

				{

					App.BroadcastState.IsBroadcastCasting = true;

				}

				string resolution = ((parts.Length >= 3) ? parts[2] : "1920×1080");

				string fps = ((parts.Length >= 4) ? parts[3] : "30 fps");

				ShowScreenCastOverlay(resolution, fps);

				Log.Information("Broadcast start received");

				break;

			}

			case "BROADCAST_STOP":

				if (Application.Current is App)

				{

					App.BroadcastState.IsBroadcastCasting = false;

				}

				CloseScreenCastOverlay();

				ShowNotification("\ud83d\udce1 Dừng chiếu", "GV đã tắt chiếu màn hình", "#666");

				Log.Information("Broadcast stop received");

				break;

			case "SURVEY_QUESTION":

				if (parts.Length >= 3)

				{

					string question = string.Join("|", parts.Skip(2));

					ShowSurveyPopup(question, new List<string> { "\ud83d\udc4d Đồng ý", "\ud83d\udc4e Không đồng ý", "\ud83e\udd14 Có thể" }, 0);

				}

				break;

			case "SURVEY_CUSTOM":

				if (parts.Length >= 5)

				{

					int result5;

					int timeSec = (int.TryParse(parts[2], out result5) ? result5 : 0);

					string text9 = "ALL";

					string text10 = "";

					bool flag = false;

					if (parts.Length >= 6)

					{

						string text11 = parts[3];

						if (text11 == "ALL" || !text11.Contains(" ") || text11.Split(',').All((string c) => c.Length < 15 && !c.Contains("?")))

						{

							flag = true;

						}

					}

					List<string> options;

					if (flag)

					{

						text9 = parts[3];

						text10 = parts[4];

						options = parts.Skip(5).ToList();

					}

					else

					{

						text9 = "ALL";

						text10 = parts[3];

						options = parts.Skip(4).ToList();

					}

					if (!IsStudentInTargetClasses(text9))

					{

						Log.Information("Survey custom ignored. Student class not in target: {Target}", text9);

						break;

					}

					ShowSurveyPopup(text10, options, timeSec);

					ShowNotification("\ud83d\udcca Khảo sát từ GV", "\"" + text10 + "\"", "#1565C0");

				}

				Log.Information("Survey custom received: {Cmd}", cmd);

				break;

			case "SURVEY_END":

				CloseSurveyPopup();

				_surveyCountdownTimer?.Stop();

				ShowNotification("⏰ Hết giờ khảo sát", "Thời gian khảo sát đã kết thúc", "#E65100");

				break;

			case "ASSIGNMENT":

				if (parts.Length >= 4)

				{

					string s = parts[2];

					string text8 = string.Join("|", parts.Skip(3));

					if (Application.Current is App)

					{

						App.AssessmentState.AssignmentDescription = text8;

						App.AssessmentState.AssignmentSentTime = DateTime.Now;

						if (DateTime.TryParse(s, out var result3))

						{

							App.AssessmentState.AssignmentDeadline = result3;

						}

						else

						{

							App.AssessmentState.AssignmentDeadline = null;

						}

					}

					NavigateTo("S4");

					ShowNotification("\ud83d\udccb Bài tập mới!", (text8.Length > 50) ? (text8.Substring(0, 50) + "...") : text8, "#1976D2");

				}

				Log.Information("Assignment received: {Cmd}", cmd);

				UpdateDynamicBadges();

				break;

			case "SILENCE":

				if (Application.Current is App)

				{

					App.ClassControl.IsSilenceActive = true;

				}

				ShowSilenceOverlay();

				Log.Information("SILENCE command received");

				break;

			case "KILL_APPS":

				KillNonEducationalApps();

				Log.Information("KILL_APPS command received");

				break;

			case "TEACHER_WARNING":

			{

				int result;

				int durationSec = ((parts.Length >= 3 && int.TryParse(parts[2], out result)) ? result : 10);

				string message = ((parts.Length >= 4) ? string.Join("|", parts.Skip(3)) : "⚠\ufe0f GV yêu cầu: Hãy tập trung!");

				ShowTeacherWarning(message, durationSec);

				Log.Information("Teacher warning received: {Cmd}", cmd);

				break;

			}

			case "CLEAR_SILENCE":

				if (Application.Current is App)

				{

					App.ClassControl.IsSilenceActive = false;

				}

				base.Dispatcher.Invoke(delegate

				{

					if (_silenceOverlay != null)

					{

						try

						{

							Grid grid = (Grid)base.Content;

							grid.Children.Remove(_silenceOverlay);

							Log.Information("Silence overlay removed from grid");

						}

						catch (Exception ex6)

						{

							Log.Warning("Remove silence error: {Err}", ex6.Message);

						}

						_silenceOverlay = null;

					}

					else

					{

						Log.Warning("CLEAR_SILENCE: _silenceOverlay was null");

					}

					ShowNotification("\ud83d\udd0a Mở IM LẶNG", "GV đã gỡ trạng thái IM LẶNG", "#2E7D32");

				});

				Log.Information("CLEAR_SILENCE received");

				break;

			case "CLEAR_WARNING":

				base.Dispatcher.Invoke(delegate

				{

					CloseTeacherWarning();

					ShowNotification("✅ Hết cảnh báo", "GV đã tắt cảnh báo", "#2E7D32");

				});

				Log.Information("CLEAR_WARNING received");

				break;

			case "CLEAR_ALL":

				if (Application.Current is App)

				{

					App.ClassControl.IsScreenLocked = false;

					App.ClassControl.ScreenLockType = "";

					App.ClassControl.IsSilenceActive = false;

					App.ClassControl.ActiveToolFocusId = "";

					App.ClassControl.ActiveToolSectionId = "";

					App.FocusState.ActiveFocusSort = -1;

					App.FocusState.ActiveFocusType = "";

					App.BroadcastState.IsBroadcastCasting = false;

				}

				_isWebBlocked = false;

				_isWebWhitelistActive = false;

				lock (_allowedUrls) { _allowedUrls.Clear(); }

				base.Dispatcher.Invoke(delegate

				{

					CloseSecureWebWindow();

					CloseScreenLockOverlay();

					CloseTeacherWarning();

					CloseToolFocusOverlay();

					CloseQuizReviewOverlay();

					CloseFileBroadcast();

					CloseScreenCastOverlay();

					CloseClassTimer();

					// === UPGRADE_02 FIX: Thêm dòng bị thiếu — dọn dẹp broadcast ===

					CloseScreenBroadcast();

					try

					{

						if (contentFrame.Content is StudentQuizPage studentQuizPage)

						{

							studentQuizPage.UnfocusAll();

						}

					}

					catch

					{

					}

					try

					{

						if (contentFrame.Content is StudentLessonPage studentLessonPage4)

						{

							studentLessonPage4.UnfocusAll();

						}

					}

					catch

					{

					}

					if (_silenceOverlay != null)

					{

						try

						{

							Grid grid = (Grid)base.Content;

							grid.Children.Remove(_silenceOverlay);

						}

						catch (Exception ex6)

						{

							Log.Warning("Remove silence (CLEAR_ALL) error: {Err}", ex6.Message);

						}

						_silenceOverlay = null;

					}

					ShowNotification("✅ Đã mở tất cả", "GV đã gỡ mọi khóa/cảnh báo", "#2E7D32");

				});

				Log.Information("CLEAR_ALL received");

				break;

			case "QUIZ_REVIEW":

				if (parts.Length >= 3)

				{

					string reviewJson = string.Join("|", parts.Skip(2));

					base.Dispatcher.Invoke(delegate

					{

						ShowQuizReviewOverlay(reviewJson);

					});

					ShowNotification("\ud83d\udccb Bài làm mẫu", "GV đang trình chiếu bài làm mẫu cho lớp!", "#1976D2");

				}

				Log.Information("QUIZ_REVIEW command received");

				break;

			case "TOOL_FOCUS":

				if (parts.Length >= 3)

				{

					string focusToolId = parts[2];

					if (Application.Current is App)

					{

						App.ClassControl.ActiveToolFocusId = focusToolId;

					}

					_lastFocusHeartbeatTime = DateTime.Now;

					base.Dispatcher.Invoke(delegate

					{

						ShowToolFocusOverlay(focusToolId);

					});

				}

				Log.Information("Tool focus command received: {Cmd}", cmd);

				break;

			case "TOOL_UNFOCUS":

				if (Application.Current is App)

				{

					App.ClassControl.ActiveToolFocusId = "";

					App.ClassControl.ActiveToolSectionId = "";

				}

				base.Dispatcher.Invoke(delegate

				{

					CloseToolFocusOverlay();

				});

				ShowNotification("\ud83d\udd13 Bỏ Focus", "GV đã bỏ chế độ tập trung công cụ học tập.", "#666");

				Log.Information("Tool unfocus command received");

				break;

			case "TOOL_SECTION_FOCUS":

				if (parts.Length >= 4)

				{

					string secToolId = parts[2];

					string sectionId = parts[3];

					string title = "";

					string detail = "";

					if (parts.Length >= 6)

					{

						try

						{

							title = Uri.UnescapeDataString(parts[4]);

							detail = Uri.UnescapeDataString(parts[5]);

						}

						catch (Exception ex4)

						{

							Log.Warning("Failed to decode step details: {Err}", ex4.Message);

							title = parts[4];

							detail = ((parts.Length > 5) ? parts[5] : "");

						}

					}

					_lastFocusHeartbeatTime = DateTime.Now;

					base.Dispatcher.Invoke(delegate

					{

						ShowToolSectionFocusOverlay(secToolId, sectionId, title, detail);

					});

				}

				Log.Information("Tool section focus command received: {Cmd}", cmd);

				break;

			case "TOOL_SECTION_UNFOCUS":

				base.Dispatcher.Invoke(delegate

				{

					ClearToolSectionHighlight();

					CloseToolFocusOverlay();

				});

				Log.Information("Tool section unfocus command received");

				break;

			case "QUIZ_FOCUS":

			{

				_isQuizFocusActive = true;

				if (parts.Length >= 3 && int.TryParse(parts[2], out var focusQNum))

				{

					base.Dispatcher.Invoke(delegate

					{

						if (!(contentFrame.Content is StudentQuizPage))

						{

							NavigateTo("S3");

						}

						try

						{

							if (contentFrame.Content is StudentQuizPage studentQuizPage)

							{

								studentQuizPage.FocusQuestion(focusQNum);

							}

						}

						catch (Exception ex6)

						{

							Log.Warning("Quiz focus page error: {Err}", ex6.Message);

						}

					});

					ShowNotification("\ud83c\udfaf Tập trung", $"GV yêu cầu xem câu #{focusQNum}", "#1565C0");

				}

				Log.Information("Quiz focus command received: {Cmd}", cmd);

				UpdateDynamicBadges();

				break;

			}

			case "QUIZ_UNFOCUS":

				_isQuizFocusActive = false;

				base.Dispatcher.Invoke(delegate

				{

					try

					{

						if (contentFrame.Content is StudentQuizPage studentQuizPage)

						{

							studentQuizPage.UnfocusAll();

						}

					}

					catch (Exception ex6)

					{

						Log.Warning("Quiz unfocus error: {Err}", ex6.Message);

					}

				});

				ShowNotification("\ud83d\udd13 Bỏ Focus", "GV đã bỏ chế độ tập trung câu hỏi.", "#666");

				Log.Information("Quiz unfocus command received");

				UpdateDynamicBadges();

				break;

			case "QUIZ_REVIEW_FOCUS":

			{

				if (parts.Length >= 3 && int.TryParse(parts[2], out var reviewQIdx))

				{

					base.Dispatcher.Invoke(delegate

					{

						FocusQuizReviewQuestion(reviewQIdx);

					});

				}

				Log.Information("Quiz review focus: question {Idx}", (parts.Length >= 3) ? parts[2] : "?");

				break;

			}

			case "FILE_FOCUS":

			{

				if (parts.Length >= 3 && int.TryParse(parts[2], out var filePage))

				{

					base.Dispatcher.Invoke(delegate

					{

						FocusFileBroadcastPage(filePage);

					});

				}

				Log.Information("File focus page: {Page}", (parts.Length >= 3) ? parts[2] : "?");

				break;

			}

			case "TIMER_START":

				if (parts.Length >= 3 && int.TryParse(parts[2], out var timerSec))

				{

					base.Dispatcher.Invoke(delegate

					{

						ShowClassTimer(timerSec);

					});

				}

				break;

			case "TIMER_STOP":

				base.Dispatcher.Invoke(delegate

				{

					CloseClassTimer();

				});

				break;

			}

		}



		private static string GetToolDisplayName(string toolId)

		{

			ToolDefinition byId = ToolRegistry.GetById(toolId);

			if (byId != null)

			{

				return byId.Name;

			}

			if (1 == 0)

			{

			}

			string result = toolId switch

			{

				"stem_tools" => "Công cụ STEM",

				"quickgraph" => "Đồ Thị Nhanh STEM",

				"eqbalance" => "Cân Bằng Phương Trình Hóa Học",

				"multiplication" => "Bảng Cửu Chương", 

				"trigonometry" => "Bảng Lượng Giác", 

				"geometry" => "Hình Học Calculator", 

				"logarithm" => "Logarithm & Lũy thừa", 

				"prime_numbers" => "Số Nguyên Tố", 

				"number_base" => "Chuyển Đổi Hệ Số", 

				"identities" => "Hằng Đẳng Thức Đáng Nhớ", 

				"periodic_table" => "Bảng Tuần Hoàn", 

				"constants" => "Hằng Số Vật Lý", 

				"unit_converter" => "Đổi Đơn Vị", 

				"ph_scale" => "Thang pH", 

				"density" => "Khối Lượng Riêng", 

				"wave_speed" => "Tốc Độ Sóng", 

				"boiling_freezing" => "Nhiệt Độ Sôi/Đông", 

				"irregular_verbs" => "Động Từ Bất Quy Tắc", 

				"vocabulary" => "Từ Vựng Theo Chủ Đề", 

				"grammar" => "Ngữ Pháp Tiếng Anh", 

				"ipa" => "Bảng Phiên Âm IPA", 

				"math_symbols" => "Ký Hiệu Toán Học", 

				"planets" => "Hành Tinh & Vệ Tinh", 

				"countries" => "Nước & Thủ Đô", 

				"literature" => "Tác Phẩm Văn Học", 

				"dynasties" => "Triều Đại Lịch Sử", 

				"textbooks" => "Sách Giáo Khoa Điện Tử", 

				"mental_math" => "Tính Nhẩm Nhanh", 

				"iq_quiz" => "Luyện IQ & Logic", 

				"formulas" => "Bảng Công Thức", 

				_ => toolId, 

			};

			if (1 == 0)

			{

			}

			return result;

		}



		private bool IsStudentInTargetClasses(string targetClasses)

		{

			if (string.IsNullOrEmpty(targetClasses) || targetClasses.Equals("ALL", StringComparison.OrdinalIgnoreCase))

			{

				return true;

			}

			string studentClass = (((App)Application.Current)?.StudentNetwork)?.ClassName ?? "";

			if (string.IsNullOrEmpty(studentClass))

			{

				return true;

			}

			List<string> source = (from c in targetClasses.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries)

				select c.Trim()).ToList();

			return source.Any((string t) => t.Equals(studentClass, StringComparison.OrdinalIgnoreCase));

		}



		private void ShowSurveyPopup(string question, List<string> options, int timeSec)

		{

			try

			{

				CloseSurveyPopup();

				_surveyOverlay = new Grid

				{

					Background = new SolidColorBrush(Color.FromArgb(200, 20, 20, 40)),

					Tag = "SurveyOverlay"

				};

				Border border = new Border

				{

					Background = Brushes.White,

					CornerRadius = new CornerRadius(16.0),

					MaxWidth = 560.0,

					Padding = new Thickness(0.0),

					HorizontalAlignment = HorizontalAlignment.Center,

					VerticalAlignment = VerticalAlignment.Center,

					Effect = new DropShadowEffect

					{

						BlurRadius = 30.0,

						ShadowDepth = 8.0,

						Opacity = 0.4,

						Color = Color.FromRgb(21, 101, 192)

					}

				};

				StackPanel stackPanel = new StackPanel();

				Border border2 = new Border

				{

					Background = new LinearGradientBrush(Color.FromRgb(21, 101, 192), Color.FromRgb(13, 71, 161), 90.0),

					CornerRadius = new CornerRadius(16.0, 16.0, 0.0, 0.0),

					Padding = new Thickness(24.0, 14.0, 24.0, 14.0)

				};

				DockPanel dockPanel = new DockPanel();

				dockPanel.Children.Add(new TextBlock

				{

					Text = "\ud83d\udcca Khảo sát từ Giáo viên",

					FontSize = 17.0,

					FontWeight = FontWeights.Bold,

					Foreground = Brushes.White

				});

				if (timeSec > 0)

				{

					TextBlock timerText = new TextBlock

					{

						Text = $"⏱️ {timeSec / 60:D2}:{timeSec % 60:D2}",

						FontSize = 14.0,

						FontWeight = FontWeights.Bold,

						Foreground = new SolidColorBrush(Color.FromRgb(byte.MaxValue, 213, 79)),

						HorizontalAlignment = HorizontalAlignment.Right,

						Tag = "countdown"

					};

					DockPanel.SetDock(timerText, Dock.Right);

					dockPanel.Children.Add(timerText);

					int remaining = timeSec;

					_surveyCountdownTimer?.Stop();

					_surveyCountdownTimer = new DispatcherTimer

					{

						Interval = TimeSpan.FromSeconds(1.0)

					};

					_surveyCountdownTimer.Tick += delegate

					{

						remaining--;

						timerText.Text = $"⏱️ {remaining / 60:D2}:{remaining % 60:D2}";

						if (remaining <= 10)

						{

							timerText.Foreground = new SolidColorBrush(Color.FromRgb(byte.MaxValue, 82, 82));

						}

						if (remaining <= 0)

						{

							_surveyCountdownTimer?.Stop();

							CloseSurveyPopup();

							ShowNotification("⏰ Hết giờ", "Thời gian khảo sát đã hết!", "#C62828");

						}

					};

					_surveyCountdownTimer.Start();

				}

				border2.Child = dockPanel;

				stackPanel.Children.Add(border2);

				Border border3 = new Border

				{

					Background = new SolidColorBrush(Color.FromRgb(227, 242, 253)),

					Margin = new Thickness(24.0, 16.0, 24.0, 0.0),

					CornerRadius = new CornerRadius(10.0),

					Padding = new Thickness(16.0, 12.0, 16.0, 12.0)

				};

				border3.Child = new TextBlock

				{

					Text = question,

					FontSize = 15.0,

					FontWeight = FontWeights.SemiBold,

					Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),

					TextWrapping = TextWrapping.Wrap,

					TextAlignment = TextAlignment.Center

				};

				stackPanel.Children.Add(border3);

				string[] array = new string[5] { "#4CAF50", "#1976D2", "#FF9800", "#EF5350", "#9C27B0" };

				string[] array2 = new string[5] { "A", "B", "C", "D", "E" };

				StackPanel stackPanel2 = new StackPanel

				{

					Margin = new Thickness(24.0, 12.0, 24.0, 20.0)

				};

				for (int num = 0; num < options.Count; num++)

				{

					Color btnColor = (Color)ColorConverter.ConvertFromString(array[num % array.Length]);

					int optIndex = num;

					Border optBtn = new Border

					{

						Background = new SolidColorBrush(Color.FromArgb(20, btnColor.R, btnColor.G, btnColor.B)),

						CornerRadius = new CornerRadius(10.0),

						Padding = new Thickness(14.0, 10.0, 14.0, 10.0),

						Margin = new Thickness(0.0, 0.0, 0.0, 6.0),

						Cursor = Cursors.Hand,

						BorderBrush = new SolidColorBrush(Color.FromArgb(60, btnColor.R, btnColor.G, btnColor.B)),

						BorderThickness = new Thickness(1.5)

					};

					StackPanel stackPanel3 = new StackPanel

					{

						Orientation = Orientation.Horizontal

					};

					Border border4 = new Border

					{

						Background = new SolidColorBrush(btnColor),

						CornerRadius = new CornerRadius(6.0),

						Width = 28.0,

						Height = 28.0,

						Margin = new Thickness(0.0, 0.0, 10.0, 0.0)

					};

					border4.Child = new TextBlock

					{

						Text = array2[num % array2.Length],

						FontSize = 13.0,

						FontWeight = FontWeights.Bold,

						Foreground = Brushes.White,

						HorizontalAlignment = HorizontalAlignment.Center,

						VerticalAlignment = VerticalAlignment.Center

					};

					stackPanel3.Children.Add(border4);

					stackPanel3.Children.Add(new TextBlock

					{

						Text = options[num],

						FontSize = 13.0,

						Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),

						VerticalAlignment = VerticalAlignment.Center,

						TextWrapping = TextWrapping.Wrap

					});

					optBtn.Child = stackPanel3;

					optBtn.MouseEnter += delegate

					{

						optBtn.Background = new SolidColorBrush(Color.FromArgb(50, btnColor.R, btnColor.G, btnColor.B));

						optBtn.BorderBrush = new SolidColorBrush(btnColor);

					};

					optBtn.MouseLeave += delegate

					{

						optBtn.Background = new SolidColorBrush(Color.FromArgb(20, btnColor.R, btnColor.G, btnColor.B));

						optBtn.BorderBrush = new SolidColorBrush(Color.FromArgb(60, btnColor.R, btnColor.G, btnColor.B));

					};

					optBtn.MouseLeftButtonDown += delegate

					{

						HandleSurveyVote($"opt{optIndex}", question, options[optIndex]);

					};

					stackPanel2.Children.Add(optBtn);

				}

				stackPanel.Children.Add(stackPanel2);

				border.Child = stackPanel;

				_surveyOverlay.Children.Add(border);

				Grid grid = (Grid)base.Content;

				Grid.SetColumnSpan(_surveyOverlay, 10);

				Grid.SetRowSpan(_surveyOverlay, 10);

				grid.Children.Add(_surveyOverlay);

				Log.Information("Survey popup shown: {Q} — {N} options — {T}s", question, options.Count, timeSec);

			}

			catch (Exception ex)

			{

				Log.Warning("ShowSurveyPopup error: {Err}", ex.Message);

			}

		}



		private void HandleSurveyVote(string voteKey, string question, string optionText)

		{

			if (App.AssessmentState.SurveyAnswered)

			{

				return;

			}

			App.AssessmentState.SurveyAnswered = true;

			try

			{

				App app = (App)Application.Current;

				string text = App.AssessmentState.ActivePollId ?? "";

				string text2 = "Student";

				try

				{

					Student student = app?.Database?.Students?.FirstOrDefault((Student s) => s.IsOnline);

					if (student != null)

					{

						text2 = student.FullName;

					}

				}

				catch

				{

				}

				if (app?.Database != null)

				{

					app.Database.EventLogs.Add(new QASmartClass.Data.EventLog

					{

						EventType = "POLL_VOTE",

						Actor = text2,

						Details = $"PollId:{text}|{voteKey}|{optionText}|Q:{question}",

						Timestamp = DateTime.Now

					});

					app.Database.SaveChanges();

				}

				if (app != null && app.StudentNetwork?.IsConnected == true)

				{

					var client = app.StudentNetwork;

					_ = Task.Run(async () =>

					{

						int retryCount = 0;

						bool sent = false;

						while (retryCount < 3 && !sent)

						{

							try

							{

								await client.SendAsync("SURVEY_RESPONSE|" + voteKey + "|" + text2);

								sent = true;

							}

							catch (Exception ex)

							{

								retryCount++;

								Log.Warning("Failed to send survey response, retry {Count}/3: {Err}", retryCount, ex.Message);

								if (retryCount < 3) await Task.Delay(1000);

							}

						}

					});

				}

				else

				{

					app?.RaiseLocalCommand("CMD|SURVEY_RESPONSE|" + voteKey + "|" + text2);

				}

				_surveyCountdownTimer?.Stop();

				CloseSurveyPopup();

				ShowNotification("✅ Da gui!", "Ban da chon: " + optionText, "#2E7D32");

				Log.Information("Survey voted: {Key} - {Text} - PollId:{PollId}", voteKey, optionText, text);

			}

			catch (Exception ex)

			{

				Log.Warning("HandleSurveyVote error: {Err}", ex.Message);

			}

		}



		private void CloseSurveyPopup()

		{

			try

			{

				if (_surveyOverlay != null)

				{

					Grid grid = (Grid)base.Content;

					grid.Children.Remove(_surveyOverlay);

					_surveyOverlay = null;

				}

			}

			catch (Exception ex)

			{

				Log.Warning("CloseSurveyPopup error: {Err}", ex.Message);

			}

		}



		private void ShowQuizReviewOverlay(string json)

		{

			try

			{

				CloseQuizReviewOverlay();

				JsonDocument jsonDocument = JsonDocument.Parse(json);

				JsonElement rootElement = jsonDocument.RootElement;

				string text = rootElement.GetProperty("StudentName").GetString() ?? "HS";

				JsonElement value2;

				int value = (rootElement.TryGetProperty("TotalScore", out value2) ? value2.GetInt32() : 0);

				JsonElement value4;

				int value3 = (rootElement.TryGetProperty("TotalPoints", out value4) ? value4.GetInt32() : 0);

				JsonElement value6;

				int value5 = (rootElement.TryGetProperty("CorrectCount", out value6) ? value6.GetInt32() : 0);

				_quizReviewOverlay = new Grid();

				Grid.SetColumnSpan(_quizReviewOverlay, 10);

				Grid.SetRowSpan(_quizReviewOverlay, 10);

				_quizReviewOverlay.Children.Add(new Border

				{

					Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0))

				});

				Border border = new Border

				{

					Background = Brushes.White,

					CornerRadius = new CornerRadius(16.0),

					Width = 650.0,

					MaxHeight = 580.0,

					HorizontalAlignment = HorizontalAlignment.Center,

					VerticalAlignment = VerticalAlignment.Center,

					BorderBrush = new SolidColorBrush(Color.FromRgb(25, 118, 210)),

					BorderThickness = new Thickness(2.0),

					Effect = new DropShadowEffect

					{

						BlurRadius = 30.0,

						ShadowDepth = 4.0,

						Opacity = 0.3

					}

				};

				Grid grid = new Grid();

				grid.RowDefinitions.Add(new RowDefinition

				{

					Height = GridLength.Auto

				});

				grid.RowDefinitions.Add(new RowDefinition

				{

					Height = new GridLength(1.0, GridUnitType.Star)

				});

				grid.RowDefinitions.Add(new RowDefinition

				{

					Height = GridLength.Auto

				});

				Border border2 = new Border

				{

					Background = new SolidColorBrush(Color.FromRgb(25, 118, 210)),

					CornerRadius = new CornerRadius(14.0, 14.0, 0.0, 0.0),

					Padding = new Thickness(20.0, 14.0, 20.0, 14.0)

				};

				DockPanel dockPanel = new DockPanel();

				Border border3 = new Border

				{

					Background = new SolidColorBrush(Color.FromArgb(80, byte.MaxValue, byte.MaxValue, byte.MaxValue)),

					CornerRadius = new CornerRadius(6.0),

					Padding = new Thickness(8.0, 4.0, 8.0, 4.0),

					Cursor = Cursors.Hand,

					VerticalAlignment = VerticalAlignment.Center

				};

				border3.Child = new TextBlock

				{

					Text = "✖ Dong",

					FontSize = 11.0,

					Foreground = Brushes.White,

					FontWeight = FontWeights.SemiBold

				};

				border3.MouseLeftButtonDown += delegate

				{

					CloseQuizReviewOverlay();

				};

				DockPanel.SetDock(border3, Dock.Right);

				dockPanel.Children.Add(border3);

				StackPanel stackPanel = new StackPanel();

				stackPanel.Children.Add(new TextBlock

				{

					Text = "\ud83d\udccb Bai lam mau — " + text,

					FontSize = 16.0,

					FontWeight = FontWeights.Bold,

					Foreground = Brushes.White

				});

				stackPanel.Children.Add(new TextBlock

				{

					Text = $"Diem: {value}/{value3} | Dung: {value5} cau",

					FontSize = 12.0,

					Foreground = new SolidColorBrush(Color.FromRgb(187, 222, 251))

				});

				dockPanel.Children.Add(stackPanel);

				border2.Child = dockPanel;

				Grid.SetRow(border2, 0);

				grid.Children.Add(border2);

				ScrollViewer scrollViewer = new ScrollViewer

				{

					VerticalScrollBarVisibility = ScrollBarVisibility.Auto,

					Padding = new Thickness(16.0, 10.0, 16.0, 10.0)

				};

				StackPanel stackPanel2 = new StackPanel();

				if (rootElement.TryGetProperty("Questions", out var value7))

				{

					foreach (JsonElement item in value7.EnumerateArray())

					{

						JsonElement value9;

						int value8 = (item.TryGetProperty("Number", out value9) ? value9.GetInt32() : 0);

						JsonElement value10;

						string text2 = (item.TryGetProperty("Content", out value10) ? (value10.GetString() ?? "") : "");

						JsonElement value11;

						string text3 = (item.TryGetProperty("CorrectAnswer", out value11) ? (value11.GetString() ?? "") : "");

						JsonElement value12;

						string text4 = (item.TryGetProperty("StudentAnswer", out value12) ? (value12.GetString() ?? "?") : "?");

						JsonElement value14;

						int value13 = (item.TryGetProperty("Points", out value14) ? value14.GetInt32() : 10);

						bool flag = text3.Equals(text4, StringComparison.OrdinalIgnoreCase);

						Border border4 = new Border

						{

							CornerRadius = new CornerRadius(10.0),

							Padding = new Thickness(14.0, 10.0, 14.0, 10.0),

							Margin = new Thickness(0.0, 0.0, 0.0, 8.0),

							Background = (flag ? new SolidColorBrush(Color.FromRgb(232, 245, 233)) : new SolidColorBrush(Color.FromRgb(byte.MaxValue, 235, 238))),

							BorderBrush = (flag ? new SolidColorBrush(Color.FromRgb(76, 175, 80)) : new SolidColorBrush(Color.FromRgb(239, 83, 80))),

							BorderThickness = new Thickness(1.5)

						};

						StackPanel stackPanel3 = new StackPanel();

						DockPanel dockPanel2 = new DockPanel

						{

							Margin = new Thickness(0.0, 0.0, 0.0, 6.0)

						};

						Border border5 = new Border

						{

							Background = (flag ? new SolidColorBrush(Color.FromRgb(46, 125, 50)) : new SolidColorBrush(Color.FromRgb(211, 47, 47))),

							CornerRadius = new CornerRadius(4.0),

							Padding = new Thickness(8.0, 2.0, 8.0, 2.0)

						};

						border5.Child = new TextBlock

						{

							Text = (flag ? "✅ DUNG" : "❌ SAI"),

							FontSize = 10.0,

							FontWeight = FontWeights.Bold,

							Foreground = Brushes.White

						};

						DockPanel.SetDock(border5, Dock.Right);

						dockPanel2.Children.Add(border5);

						Border border6 = new Border

						{

							Background = new SolidColorBrush(Color.FromRgb(25, 118, 210)),

							CornerRadius = new CornerRadius(6.0),

							Padding = new Thickness(8.0, 2.0, 8.0, 2.0),

							Margin = new Thickness(0.0, 0.0, 6.0, 0.0)

						};

						border6.Child = new TextBlock

						{

							Text = $"Cau {value8}",

							FontSize = 10.0,

							FontWeight = FontWeights.SemiBold,

							Foreground = Brushes.White

						};

						dockPanel2.Children.Add(border6);

						dockPanel2.Children.Add(new TextBlock

						{

							Text = $"{value13} diem",

							FontSize = 10.0,

							Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)),

							VerticalAlignment = VerticalAlignment.Center

						});

						stackPanel3.Children.Add(dockPanel2);

						stackPanel3.Children.Add(new TextBlock

						{

							Text = text2,

							FontSize = 13.0,

							FontWeight = FontWeights.SemiBold,

							TextWrapping = TextWrapping.Wrap,

							Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),

							Margin = new Thickness(0.0, 0.0, 0.0, 6.0)

						});

						JsonElement value15;

						string json2 = (item.TryGetProperty("OptionsJson", out value15) ? (value15.GetString() ?? "[]") : "[]");

						try

						{

							string[] array = JsonSerializer.Deserialize<string[]>(json2);

							string[] array2 = new string[5] { "A", "B", "C", "D", "E" };

							if (array != null)

							{

								for (int num = 0; num < array.Length; num++)

								{

									string text5 = ((num < array2.Length) ? array2[num] : $"{num + 1}");

									bool flag2 = text3.Equals(text5, StringComparison.OrdinalIgnoreCase);

									bool flag3 = text4.Equals(text5, StringComparison.OrdinalIgnoreCase);

									Border border7 = new Border

									{

										CornerRadius = new CornerRadius(6.0),

										Padding = new Thickness(8.0, 5.0, 8.0, 5.0),

										Margin = new Thickness(0.0, 0.0, 0.0, 3.0),

										BorderThickness = new Thickness((!(flag2 || flag3)) ? 1 : 2)

									};

									if (flag2)

									{

										border7.Background = new SolidColorBrush(Color.FromRgb(200, 230, 201));

										border7.BorderBrush = new SolidColorBrush(Color.FromRgb(56, 142, 60));

									}

									else if (flag3)

									{

										border7.Background = new SolidColorBrush(Color.FromRgb(byte.MaxValue, 205, 210));

										border7.BorderBrush = new SolidColorBrush(Color.FromRgb(211, 47, 47));

									}

									else

									{

										border7.Background = new SolidColorBrush(Color.FromRgb(250, 250, 250));

										border7.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));

									}

									DockPanel dockPanel3 = new DockPanel();

									if (flag2 && flag3)

									{

										Border element = MakeTag("✅ DUNG", "#2E7D32");

										DockPanel.SetDock(element, Dock.Right);

										dockPanel3.Children.Add(element);

									}

									else if (flag2)

									{

										Border element2 = MakeTag("✅ DAP AN DUNG", "#388E3C");

										DockPanel.SetDock(element2, Dock.Right);

										dockPanel3.Children.Add(element2);

									}

									else if (flag3)

									{

										Border element3 = MakeTag("❌ HS CHON", "#D32F2F");

										DockPanel.SetDock(element3, Dock.Right);

										dockPanel3.Children.Add(element3);

									}

									StackPanel stackPanel4 = new StackPanel

									{

										Orientation = Orientation.Horizontal

									};

									Border border8 = new Border

									{

										Width = 22.0,

										Height = 22.0,

										CornerRadius = new CornerRadius(11.0),

										Margin = new Thickness(0.0, 0.0, 6.0, 0.0),

										Background = (flag2 ? new SolidColorBrush(Color.FromRgb(56, 142, 60)) : (flag3 ? new SolidColorBrush(Color.FromRgb(211, 47, 47)) : new SolidColorBrush(Color.FromRgb(189, 189, 189))))

									};

									border8.Child = new TextBlock

									{

										Text = text5,

										FontSize = 10.0,

										FontWeight = FontWeights.Bold,

										Foreground = Brushes.White,

										HorizontalAlignment = HorizontalAlignment.Center,

										VerticalAlignment = VerticalAlignment.Center

									};

									stackPanel4.Children.Add(border8);

									string text6 = array[num];

									if (text6.Length > 3 && text6[1] == '.' && text6[2] == ' ')

									{

										text6 = text6.Substring(3);

									}

									stackPanel4.Children.Add(new TextBlock

									{

										Text = text6,

										FontSize = 12.0,

										FontWeight = ((flag2 || flag3) ? FontWeights.SemiBold : FontWeights.Normal),

										Foreground = (flag2 ? new SolidColorBrush(Color.FromRgb(27, 94, 32)) : (flag3 ? new SolidColorBrush(Color.FromRgb(183, 28, 28)) : new SolidColorBrush(Color.FromRgb(66, 66, 66)))),

										VerticalAlignment = VerticalAlignment.Center

									});

									dockPanel3.Children.Add(stackPanel4);

									border7.Child = dockPanel3;

									stackPanel3.Children.Add(border7);

								}

							}

						}

						catch

						{

						}

						border4.Child = stackPanel3;

						stackPanel2.Children.Add(border4);

					}

				}

				scrollViewer.Content = stackPanel2;

				Grid.SetRow(scrollViewer, 1);

				grid.Children.Add(scrollViewer);

				Border border9 = new Border

				{

					Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),

					CornerRadius = new CornerRadius(0.0, 0.0, 14.0, 14.0),

					Padding = new Thickness(16.0, 10.0, 16.0, 10.0)

				};

				border9.Child = new TextBlock

				{

					Text = $"GV dang trinh chieu bai lam mau cua {text} — Diem: {value}/{value3} — Dung: {value5} cau",

					FontSize = 11.0,

					Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)),

					TextWrapping = TextWrapping.Wrap

				};

				Grid.SetRow(border9, 2);

				grid.Children.Add(border9);

				border.Child = grid;

				_quizReviewOverlay.Children.Add(border);

				Grid grid2 = (Grid)base.Content;

				Grid.SetColumnSpan(_quizReviewOverlay, 10);

				Grid.SetRowSpan(_quizReviewOverlay, 10);

				grid2.Children.Add(_quizReviewOverlay);

				Log.Information("Quiz review overlay shown for {Name}", text);

			}

			catch (Exception ex)

			{

				Log.Warning("ShowQuizReviewOverlay error: {Err}", ex.Message);

			}

		}



		private Border MakeTag(string text, string colorHex)

		{

			Color color = (Color)ColorConverter.ConvertFromString(colorHex);

			Border border = new Border

			{

				Background = new SolidColorBrush(color),

				CornerRadius = new CornerRadius(4.0),

				Padding = new Thickness(6.0, 1.0, 6.0, 1.0),

				Margin = new Thickness(4.0, 0.0, 0.0, 0.0),

				VerticalAlignment = VerticalAlignment.Center

			};

			border.Child = new TextBlock

			{

				Text = text,

				FontSize = 9.0,

				FontWeight = FontWeights.Bold,

				Foreground = Brushes.White

			};

			return border;

		}



		private void CloseQuizReviewOverlay()

		{

			try

			{

				if (_quizReviewOverlay != null)

				{

					Grid grid = (Grid)base.Content;

					grid.Children.Remove(_quizReviewOverlay);

					_quizReviewOverlay = null;

				}

			}

			catch (Exception ex)

			{

				Log.Warning("CloseQuizReviewOverlay error: {Err}", ex.Message);

			}

		}



		private void FocusQuizReviewQuestion(int questionIndex)

		{

			try

			{

				if (_quizReviewOverlay == null)

				{

					Log.Warning("FocusQuizReviewQuestion: overlay not open");

					return;

				}

				ScrollViewer scrollViewer = null;

				StackPanel stackPanel = null;

				foreach (ScrollViewer item in FindVisualChildren<ScrollViewer>(_quizReviewOverlay))

				{

					if (item.Content is StackPanel stackPanel2 && stackPanel2.Children.Count > 0)

					{

						scrollViewer = item;

						stackPanel = stackPanel2;

						break;

					}

				}

				if (stackPanel == null || scrollViewer == null)

				{

					Log.Warning("FocusQuizReviewQuestion: questions panel not found");

					return;

				}

				SolidColorBrush borderBrush = new SolidColorBrush(Color.FromRgb(25, 118, 210));

				SolidColorBrush solidColorBrush = new SolidColorBrush(Color.FromRgb(227, 242, 253));

				for (int i = 0; i < stackPanel.Children.Count; i++)

				{

					if (stackPanel.Children[i] is Border border)

					{

						if (i + 1 == questionIndex)

						{

							border.BorderBrush = borderBrush;

							border.BorderThickness = new Thickness(4.0);

							border.Padding = new Thickness(18.0, 14.0, 18.0, 14.0);

							border.Margin = new Thickness(0.0, 8.0, 0.0, 8.0);

							border.Opacity = 1.0;

							border.Effect = new DropShadowEffect

							{

								BlurRadius = 30.0,

								ShadowDepth = 4.0,

								Color = Color.FromRgb(25, 118, 210),

								Opacity = 0.5

							};

							border.LayoutTransform = new ScaleTransform(1.1, 1.1);

							border.BringIntoView();

						}

						else

						{

							border.Opacity = 0.35;

							border.BorderThickness = new Thickness(1.5);

							border.Effect = null;

							border.LayoutTransform = null;

							border.Padding = new Thickness(14.0, 10.0, 14.0, 10.0);

							border.Margin = new Thickness(0.0, 0.0, 0.0, 8.0);

						}

					}

				}

				ShowNotification("\ud83c\udfaf Review Focus", $"GV đang chỉ vào câu #{questionIndex}", "#1565C0");

				Log.Information("Quiz review focused on question {Idx}", questionIndex);

			}

			catch (Exception ex)

			{

				Log.Warning("FocusQuizReviewQuestion error: {Err}", ex.Message);

			}

		}



		private void FocusFileBroadcastPage(int pageNumber)

		{

			try

			{

				if (_fileBroadcastOverlay == null)

				{

					Log.Warning("FocusFileBroadcastPage: overlay not open");

					return;

				}

				Border border = _fileBroadcastOverlay.Children.OfType<Border>().FirstOrDefault((Border b) => b.Tag as string == "FilePageIndicator");

				if (border != null)

				{

					_fileBroadcastOverlay.Children.Remove(border);

				}

				Border indicator = new Border

				{

					Background = new SolidColorBrush(Color.FromArgb(230, 25, 118, 210)),

					CornerRadius = new CornerRadius(12.0),

					Padding = new Thickness(20.0, 12.0, 20.0, 12.0),

					HorizontalAlignment = HorizontalAlignment.Center,

					VerticalAlignment = VerticalAlignment.Bottom,

					Margin = new Thickness(0.0, 0.0, 0.0, 24.0),

					Tag = "FilePageIndicator",

					Effect = new DropShadowEffect

					{

						BlurRadius = 20.0,

						ShadowDepth = 4.0,

						Color = Color.FromRgb(0, 0, 0),

						Opacity = 0.4

					}

				};

				StackPanel stackPanel = new StackPanel

				{

					Orientation = Orientation.Horizontal

				};

				stackPanel.Children.Add(new TextBlock

				{

					Text = "\ud83c\udfaf",

					FontSize = 22.0,

					VerticalAlignment = VerticalAlignment.Center,

					Margin = new Thickness(0.0, 0.0, 10.0, 0.0)

				});

				stackPanel.Children.Add(new TextBlock

				{

					Text = $"GV đang chỉ vào — Trang {pageNumber}",

					FontSize = 16.0,

					FontWeight = FontWeights.Bold,

					Foreground = Brushes.White,

					VerticalAlignment = VerticalAlignment.Center

				});

				indicator.Child = stackPanel;

				_fileBroadcastOverlay.Children.Add(indicator);

				DispatcherTimer hideTimer = new DispatcherTimer

				{

					Interval = TimeSpan.FromSeconds(5.0)

				};

				hideTimer.Tick += delegate

				{

					hideTimer.Stop();

					try

					{

						if (_fileBroadcastOverlay != null && _fileBroadcastOverlay.Children.Contains(indicator))

						{

							DoubleAnimation doubleAnimation = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(400.0));

							doubleAnimation.Completed += delegate

							{

								try

								{

									_fileBroadcastOverlay?.Children.Remove(indicator);

								}

								catch

								{

								}

							};

							indicator.BeginAnimation(UIElement.OpacityProperty, doubleAnimation);

						}

					}

					catch

					{

					}

				};

				hideTimer.Start();

				indicator.Opacity = 0.0;

				DoubleAnimation animation = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(300.0));

				indicator.BeginAnimation(UIElement.OpacityProperty, animation);

				ShowNotification("\ud83d\udcc4 Trang " + pageNumber, "GV đang chỉ vào trang này", "#1565C0");

				Log.Information("File broadcast focused on page {Page}", pageNumber);

			}

			catch (Exception ex)

			{

				Log.Warning("FocusFileBroadcastPage error: {Err}", ex.Message);

			}

		}



		public void ShowNotification(string title, string message, string colorHex)

		{

			try

			{

				Color color = (Color)ColorConverter.ConvertFromString(colorHex);

				Border notifBorder = new Border

				{

					Background = new SolidColorBrush(Color.FromArgb(245, color.R, color.G, color.B)),

					CornerRadius = new CornerRadius(8.0),

					Padding = new Thickness(16.0, 12.0, 16.0, 12.0),

					Margin = new Thickness(0.0, 0.0, 20.0, 20.0),

					HorizontalAlignment = HorizontalAlignment.Right,

					VerticalAlignment = VerticalAlignment.Bottom,

					MaxWidth = 350.0,

					Effect = new DropShadowEffect

					{

						BlurRadius = 15.0,

						ShadowDepth = 3.0,

						Opacity = 0.3

					}

				};

				StackPanel stackPanel = new StackPanel();

				stackPanel.Children.Add(new TextBlock

				{

					Text = title,

					FontSize = 13.0,

					FontWeight = FontWeights.Bold,

					Foreground = Brushes.White,

					Margin = new Thickness(0.0, 0.0, 0.0, 4.0)

				});

				stackPanel.Children.Add(new TextBlock

				{

					Text = message,

					FontSize = 12.0,

					Foreground = new SolidColorBrush(Color.FromArgb(220, byte.MaxValue, byte.MaxValue, byte.MaxValue)),

					TextWrapping = TextWrapping.Wrap

				});

				notifBorder.Child = stackPanel;

				Grid mainGrid = (Grid)base.Content;

				Grid.SetColumn(notifBorder, 1);

				Grid.SetRow(notifBorder, 1);

				mainGrid.Children.Add(notifBorder);

				DispatcherTimer timer = new DispatcherTimer

				{

					Interval = TimeSpan.FromSeconds(5.0)

				};

				timer.Tick += delegate

				{

					timer.Stop();

					mainGrid.Children.Remove(notifBorder);

				};

				timer.Start();

			}

			catch (Exception ex)

			{

				Log.Warning("ShowNotification error: {Err}", ex.Message);

			}

		}



		public void ShowFirewallWarningNotification(string title, string message, string colorHex)

		{

			try

			{

				Color color = (Color)ColorConverter.ConvertFromString(colorHex);

				Border notifBorder = new Border

				{

					Background = new SolidColorBrush(Color.FromArgb(245, color.R, color.G, color.B)),

					CornerRadius = new CornerRadius(8.0),

					Padding = new Thickness(16.0, 12.0, 16.0, 12.0),

					Margin = new Thickness(0.0, 0.0, 20.0, 20.0),

					HorizontalAlignment = HorizontalAlignment.Right,

					VerticalAlignment = VerticalAlignment.Bottom,

					MaxWidth = 360.0,

					Effect = new DropShadowEffect

					{

						BlurRadius = 15.0,

						ShadowDepth = 3.0,

						Opacity = 0.3

					}

				};

				StackPanel stackPanel = new StackPanel();

				stackPanel.Children.Add(new TextBlock

				{

					Text = title,

					FontSize = 13.0,

					FontWeight = FontWeights.Bold,

					Foreground = Brushes.White,

					Margin = new Thickness(0.0, 0.0, 0.0, 4.0)

				});

				stackPanel.Children.Add(new TextBlock

				{

					Text = message,

					FontSize = 12.0,

					Foreground = new SolidColorBrush(Color.FromArgb(220, byte.MaxValue, byte.MaxValue, byte.MaxValue)),

					TextWrapping = TextWrapping.Wrap,

					Margin = new Thickness(0.0, 0.0, 0.0, 8.0)

				});



				Button fixBtn = new Button

				{

					Content = "🛠️ Tự sửa lỗi tường lửa",

					FontSize = 12.0,

					FontWeight = FontWeights.Bold,

					Foreground = Brushes.White,

					Background = new SolidColorBrush(Color.FromRgb(46, 125, 50)), // Green

					Padding = new Thickness(8.0, 4.0, 8.0, 4.0),

					Cursor = Cursors.Hand,

					HorizontalAlignment = HorizontalAlignment.Left

				};

				fixBtn.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));

				fixBtn.Click += (s, e) =>

				{

					Log.Information("[Firewall] Clicked Auto-Fix from Notification.");

					Task.Run(async () =>

					{

						bool success = await QASmartClass.Services.NetworkDiagnosticsService.RunFirewallAutoFixAsync();

						if (success)

						{

							Dispatcher.Invoke(() => ShowNotification("✅ Thành công", "Đã cấu hình tường lửa. Vui lòng đợi kết nối lại.", "#2E7D32"));

						}

					});

				};

				stackPanel.Children.Add(fixBtn);



				notifBorder.Child = stackPanel;

				Grid mainGrid = (Grid)base.Content;

				Grid.SetColumn(notifBorder, 1);

				Grid.SetRow(notifBorder, 1);

				mainGrid.Children.Add(notifBorder);

				DispatcherTimer timer = new DispatcherTimer

				{

					Interval = TimeSpan.FromSeconds(15.0)

				};

				timer.Tick += delegate

				{

					timer.Stop();

					mainGrid.Children.Remove(notifBorder);

				};

				timer.Start();

			}

			catch (Exception ex)

			{

				Log.Warning("ShowFirewallWarningNotification error: {Err}", ex.Message);

			}

		}



		private static bool IsWebPImage(byte[] data)

		{

			return data != null && data.Length > 12 &&

				   data[0] == (byte)'R' && data[1] == (byte)'I' && data[2] == (byte)'F' && data[3] == (byte)'F' &&

				   data[8] == (byte)'W' && data[9] == (byte)'E' && data[10] == (byte)'B' && data[11] == (byte)'P';

		}



		private static System.Windows.Media.Imaging.BitmapSource ConvertBitmapToBitmapSource(System.Drawing.Bitmap bmp)

		{

			var rect = new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height);

			var bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			try

			{

				var size = bmpData.Stride * bmpData.Height;

				var bitmapSource = System.Windows.Media.Imaging.BitmapSource.Create(

					bmp.Width, bmp.Height,

					bmp.HorizontalResolution, bmp.VerticalResolution,

					System.Windows.Media.PixelFormats.Bgr32,

					null, bmpData.Scan0, size, bmpData.Stride);

				bitmapSource.Freeze();

				return bitmapSource;

			}

			finally

			{

				bmp.UnlockBits(bmpData);

			}

		}



		private DateTime _lastUdpFrameTime = DateTime.MinValue;
		private DateTime _lastFrameRenderTime = DateTime.MinValue;



		private void OnUdpFrameReceived(byte[] imgBytes)
		{
			var now = DateTime.UtcNow;
			if ((now - _lastFrameRenderTime).TotalMilliseconds < 60)
			{
				return;
			}
			_lastFrameRenderTime = now;

			// Decode on background thread (OUTSIDE Dispatcher.Invoke)
			BitmapSource? source = null;
			try
			{
				// === UPGRADE_08: Chỉ dùng OpenCV decode WebP khi DLL tồn tại ===
				if (_isOpenCvAvailable && IsWebPImage(imgBytes))
				{
					using (var mat = OpenCvSharp.Cv2.ImDecode(imgBytes, OpenCvSharp.ImreadModes.Color))
					{
						using (var bmp = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(mat))
						{
							source = ConvertBitmapToBitmapSource(bmp);
						}
					}
				}

				if (source == null)
				{
					var bitmap = new BitmapImage();
					using (var ms = new MemoryStream(imgBytes))
					{
						bitmap.BeginInit();
						bitmap.CacheOption = BitmapCacheOption.OnLoad;
						bitmap.StreamSource = ms;
						bitmap.EndInit();
					}
					bitmap.Freeze();
					source = bitmap;
				}

				if (source != null && source.CanFreeze) source.Freeze(); // MUST freeze for cross-thread
			}
			catch (Exception exDecode)
			{
				Log.Warning("Failed to decode UDP frame: {Err}", exDecode.Message);
				return;
			}

			// Only marshal the UI assignment to the dispatcher asynchronously to prevent blocking
			base.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, new Action(() =>
			{
				try
				{
					if (_broadcastImage != null)
					{
						_broadcastImage.Source = source;
						_lastUdpFrameTime = DateTime.UtcNow;
						_lastBroadcastUpdateTime = DateTime.UtcNow;
						_consecutiveHttpPullFailures = 0;

						if (_broadcastConnectionStatus != null && _broadcastConnectionStatus.Visibility == Visibility.Visible)
						{
							_broadcastConnectionStatus.Visibility = Visibility.Collapsed;
						}

						if (_broadcastTimeText != null)
						{
							_broadcastTimeText.Text = $"Giờ: {DateTime.Now:HH:mm:ss} (UDP)";
						}
					}
				}
				catch (Exception ex)
				{
					Log.Warning("Failed to display UDP multicast frame: {Err}", ex.Message);
				}
			}));
		}



		private void ParseBroadcastCommand(string[] parts, string cmd, out string imagePath, out bool isForce, out string token)

		{

			imagePath = parts.Length >= 3 ? parts[2] : "";

			isForce = cmd.Contains("FORCE_WATCH");

			token = "";

			string lessonTitle = "";



			int lastIdx = parts.Length - 1;

			if (lastIdx >= 3 && parts[lastIdx].StartsWith("TITLE="))

			{

				lessonTitle = System.Uri.UnescapeDataString(parts[lastIdx].Substring(6));

				lastIdx--;

			}



			_currentLessonTitle = lessonTitle;



			if (lastIdx >= 3)

			{

				if (int.TryParse(parts[lastIdx], out int portVal))

				{

					_currentTeacherWebPort = portVal;

					token = "";

				}

				else

				{

					token = parts[lastIdx];

					if (int.TryParse(parts[lastIdx - 1], out int portVal2))

					{

						_currentTeacherWebPort = portVal2;

					}

				}

			}

		}



		private void ShowScreenBroadcast(string imagePath, bool isForce = false, string token = "")

		{

			try

			{

				_currentBroadcastToken = token; // Assign token early



				// === PHASE6-HOTFIX TC-050: Restart watchdog khi mo broadcast moi ===

				if (_broadcastWatchdogTimer != null && !_broadcastWatchdogTimer.IsEnabled)

				{

					_broadcastWatchdogTimer.Start();

					Log.Information("[Broadcast] Watchdog timer restarted for new broadcast session");

				}



			_consecutiveHttpPullFailures = 0; // === UPGRADE_03 ===



				// Khởi động bộ giám sát tài nguyên (V2.2.4)
				QASmartClass.Services.AppPerformanceMonitor.Instance.Start();

				// Set up UDP Multicast Receiver (V2.2.2)
				QASmartClass.Services.UdpScreenBroadcastService.Instance.FrameReceived -= OnUdpFrameReceived;
				if (_currentBroadcastProtocol != "HTTP_PULL")
				{
					QASmartClass.Services.UdpScreenBroadcastService.Instance.FrameReceived += OnUdpFrameReceived;
					QASmartClass.Services.UdpScreenBroadcastService.Instance.StartReceiver();
				}
				else
				{
					Log.Information("[StudentShell] Chế độ HTTP_PULL được yêu cầu. Bỏ qua khởi động bộ nhận UDP.");
				}

				_lastUdpFrameTime = DateTime.MinValue;

				_lastHttpPullTime = DateTime.MinValue;

				if (!File.Exists(imagePath))
				{
					var config = LoadKioskConfig();
					bool allowFallback = config.StreamFallbackMode == "AutoFallback" || config.StreamFallbackMode == "HttpOnly";
					double elapsedUdp = (DateTime.UtcNow - _lastUdpFrameTime).TotalSeconds;

					if (_currentBroadcastProtocol != "HTTP_PULL" && (!allowFallback || elapsedUdp <= 3.0))
					{
						return; // Bỏ qua kéo HTTP để tránh quá tải CPU/Mạng khi đang dùng UDP
					}

					string teacherIP = (((App)Application.Current)?.StudentNetwork)?.ServerIP;

					if (!string.IsNullOrEmpty(teacherIP))

					{

						imagePath = $"http://{teacherIP}:{_currentTeacherWebPort}/api/broadcast_image?t={DateTime.Now.Ticks}" + (!string.IsNullOrEmpty(_currentBroadcastToken) ? $"&token={_currentBroadcastToken}" : "");

					}

					else

					{

						Log.Warning("Screen broadcast image not found and no teacher IP: {Path}", imagePath);

						return;

					}

				}

				if (_broadcastOverlay != null)

				{

					UpdateScreenBroadcast(imagePath);

					return;

				}

				_broadcastOverlay = new Grid

				{

					Background = new SolidColorBrush(Color.FromArgb(245, 15, 15, 25)),

					Tag = "ScreenBroadcastOverlay"

				};

				Border border = new Border

				{

					Background = new LinearGradientBrush(Color.FromArgb(230, 123, 31, 162), Color.FromArgb(230, 81, 45, 168), 0.0),

					Height = 48.0,

					VerticalAlignment = VerticalAlignment.Top,

					Padding = new Thickness(16.0, 0.0, 16.0, 0.0)

				};

				DockPanel dockPanel = new DockPanel();

				StackPanel stackPanel = new StackPanel

				{

					Orientation = Orientation.Horizontal,

					VerticalAlignment = VerticalAlignment.Center

				};

				stackPanel.Children.Add(new Border

				{

					Width = 10.0,

					Height = 10.0,

					CornerRadius = new CornerRadius(5.0),

					Background = Brushes.Red,

					Margin = new Thickness(0.0, 0.0, 8.0, 0.0),

					VerticalAlignment = VerticalAlignment.Center

				});

				stackPanel.Children.Add(new TextBlock

				{

					// V22-05: Tiếng Việt hóa

					Text = "ĐANG PHÁT",

					FontSize = 11.0,

					FontWeight = FontWeights.Bold,

					Foreground = new SolidColorBrush(Color.FromRgb(byte.MaxValue, 82, 82)),

					VerticalAlignment = VerticalAlignment.Center,

					Margin = new Thickness(0.0, 0.0, 12.0, 0.0),

					FontFamily = _broadcastFont

				});

				stackPanel.Children.Add(new TextBlock

				{

					Text = "Màn hình giáo viên đang chiếu",

					FontSize = 15.0,

					FontWeight = FontWeights.SemiBold,

					Foreground = Brushes.White,

					VerticalAlignment = VerticalAlignment.Center,

					FontFamily = _broadcastFont

				});

				dockPanel.Children.Add(stackPanel);

				_broadcastTimeText = new TextBlock

				{

					// V22-27: Hiện "Vừa bắt đầu" thay 00:00

					Text = $"Giờ: {DateTime.Now:HH:mm:ss} | Vừa bắt đầu",

					FontSize = 12.0,

					Foreground = new SolidColorBrush(Color.FromArgb(180, byte.MaxValue, byte.MaxValue, byte.MaxValue)),

					VerticalAlignment = VerticalAlignment.Center,

					Margin = new Thickness(12.0, 0.0, 0.0, 0.0),

					FontFamily = _broadcastFont

				};

				_broadcastStartTime = DateTime.Now;

				dockPanel.Children.Add(_broadcastTimeText);

				// V22-26: Nút lớn hơn cho tablet

				Button button = new Button

				{

					Content = "✕ Đóng",

					FontSize = 15.0,

					Padding = new Thickness(18.0, 8.0, 18.0, 8.0),

					Background = new SolidColorBrush(Color.FromArgb(200, 229, 57, 53)),

					Foreground = Brushes.White,

					BorderThickness = new Thickness(0.0),

					Cursor = Cursors.Hand,

					VerticalAlignment = VerticalAlignment.Center,

					FontFamily = _broadcastFont

				};

				_broadcastCloseButton = button;

				_isBroadcastForceWatch = isForce;

				base.Topmost = isForce;

				DockPanel.SetDock(button, Dock.Right);

				if (isForce)

				{

					button.Visibility = Visibility.Collapsed;

				}

				dockPanel.Children.Insert(0, button);

				border.Child = dockPanel;

				_broadcastImage = new Image

				{

					Source = null,

					Stretch = Stretch.Uniform,

					Margin = new Thickness(20.0, 58.0, 20.0, 20.0),

					HorizontalAlignment = HorizontalAlignment.Center,

					VerticalAlignment = VerticalAlignment.Center

				};

				_broadcastInkCanvas = new InkCanvas

				{

					Background = Brushes.Transparent,

					EditingMode = InkCanvasEditingMode.None,

					IsHitTestVisible = false,

					Margin = new Thickness(20.0, 58.0, 20.0, 20.0),

					HorizontalAlignment = HorizontalAlignment.Center,

					VerticalAlignment = VerticalAlignment.Center

				};

				Binding binding = new Binding("ActualWidth")

				{

					Source = _broadcastImage

				};

				_broadcastInkCanvas.SetBinding(FrameworkElement.WidthProperty, binding);

				Binding binding2 = new Binding("ActualHeight")

				{

					Source = _broadcastImage

				};

				_broadcastInkCanvas.SetBinding(FrameworkElement.HeightProperty, binding2);

				_broadcastShapeCanvas = new Canvas
				{
					Background = Brushes.Transparent,
					IsHitTestVisible = false,
					Margin = new Thickness(20.0, 58.0, 20.0, 20.0),
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center
				};
				Binding binding3 = new Binding("ActualWidth") { Source = _broadcastImage };
				_broadcastShapeCanvas.SetBinding(FrameworkElement.WidthProperty, binding3);
				Binding binding4 = new Binding("ActualHeight") { Source = _broadcastImage };
				_broadcastShapeCanvas.SetBinding(FrameworkElement.HeightProperty, binding4);

				_broadcastOverlay.Children.Add(_broadcastImage);
				_broadcastOverlay.Children.Add(_broadcastInkCanvas);
				_broadcastOverlay.Children.Add(_broadcastShapeCanvas);
				_broadcastOverlay.Children.Add(border);



				// V4.1: Giao diện chờ đồng bộ trực quan (UPGRADE_09: Responsive Glassmorphic Loading Card)

				var glassBorder = new Border

				{

					MaxWidth = 380, // Dùng MaxWidth thay vì Width cố định để tự co giãn

					Height = 200,

					CornerRadius = new CornerRadius(12), // Bo góc chuẩn hệ thống

					Background = new LinearGradientBrush(

						Color.FromArgb(180, 10, 15, 30),

						Color.FromArgb(180, 20, 25, 55),

						45.0),

					BorderBrush = new LinearGradientBrush(

						Color.FromRgb(123, 31, 162), // Tím

						Color.FromRgb(25, 118, 210), // Xanh dương

						45.0),

					BorderThickness = new Thickness(1.5),

					Padding = new Thickness(20),

					HorizontalAlignment = HorizontalAlignment.Center,

					VerticalAlignment = VerticalAlignment.Center

				};



				var cardContent = new StackPanel

				{

					VerticalAlignment = VerticalAlignment.Center,

					HorizontalAlignment = HorizontalAlignment.Center

				};



				var pulseIcon = new TextBlock

				{

					Text = "📺 ĐANG TRÌNH CHIẾU", // Việt hóa + thêm emoji trực quan

					FontSize = 22.0,			// Giảm kích thước để chống tràn dòng

					FontFamily = DS.FontPrimary, // Segoe UI

					FontWeight = FontWeights.Bold,

					Foreground = new SolidColorBrush(Color.FromRgb(79, 195, 247)),

					HorizontalAlignment = HorizontalAlignment.Center,

					Margin = new Thickness(0.0, 0.0, 0.0, 12.0),

					TextAlignment = TextAlignment.Center

				};

				cardContent.Children.Add(pulseIcon);



				_broadcastStatusText = new TextBlock

				{

					Text = "Đang kết nối luồng hình ảnh thời gian thực...",

					FontSize = 14.0,

					FontWeight = FontWeights.Bold,

					Foreground = Brushes.White,

					HorizontalAlignment = HorizontalAlignment.Center,

					Margin = new Thickness(0.0, 0.0, 0.0, 8.0),

					FontFamily = _broadcastFont,

					TextWrapping = TextWrapping.Wrap,

					TextAlignment = TextAlignment.Center

				};

				cardContent.Children.Add(_broadcastStatusText);



				var descText = new TextBlock

				{

					Text = "Vui lòng chờ trong giây lát...",

					FontSize = 11.0,

					Foreground = new SolidColorBrush(Color.FromArgb(150, byte.MaxValue, byte.MaxValue, byte.MaxValue)),

					HorizontalAlignment = HorizontalAlignment.Center,

					FontFamily = _broadcastFont

				};

				cardContent.Children.Add(descText);



				glassBorder.Child = cardContent;



				_broadcastConnectionStatus = new StackPanel

				{

					VerticalAlignment = VerticalAlignment.Center,

					HorizontalAlignment = HorizontalAlignment.Center,

					Visibility = Visibility.Visible // Hiện ban đầu làm Loading Screen

				};

				_broadcastConnectionStatus.Children.Add(glassBorder);



				// Pulse Animation cho Loading Card

				var pulseAnim = new System.Windows.Media.Animation.DoubleAnimation

				{

					From = 0.85,

					To = 1.0,

					Duration = new Duration(TimeSpan.FromSeconds(1.2)),

					AutoReverse = true,

					RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever

				};

				System.Windows.Media.Animation.Storyboard.SetTarget(pulseAnim, glassBorder);

				System.Windows.Media.Animation.Storyboard.SetTargetProperty(pulseAnim, new PropertyPath("Opacity"));



				var sb = new System.Windows.Media.Animation.Storyboard();

				sb.Children.Add(pulseAnim);

				sb.Begin();

			_pulseStoryboard = sb;



				_broadcastOverlay.Children.Add(_broadcastConnectionStatus);



				// === UPGRADE_04: Nút đóng khẩn cấp (ẩn, hiện sau 10 giây nếu stream chưa kết nối) ===

				var emergencyCloseBtn = new Button

				{

					Content = "✖ Thoát chế độ trình chiếu",

					FontFamily = DS.FontPrimary,

					FontSize = DS.FontNote, // 13px

					FontWeight = FontWeights.Bold,

					Foreground = Brushes.White,

					Background = new SolidColorBrush(DS.ResultDanger),

					Padding = new Thickness(DS.PadChip),

					MinHeight = DS.TouchMinHeight,

					HorizontalAlignment = HorizontalAlignment.Right,

					VerticalAlignment = VerticalAlignment.Top,

					Margin = new Thickness(0, 20, 20, 0),

					Visibility = Visibility.Collapsed,

					Cursor = Cursors.Hand

				};

				emergencyCloseBtn.SetValue(Border.CornerRadiusProperty, new CornerRadius(DS.RadiusChip));



				emergencyCloseBtn.Click += (s, e) =>

				{

					Log.Warning("[Kiosk] Emergency close button clicked by student.");

					CloseScreenBroadcast();

					ShowNotification("ℹ️ Đã thoát", "Đã thoát chế độ trình chiếu.", "#757575");

				};



				_broadcastOverlay.Children.Add(emergencyCloseBtn);



				// === UPGRADE_12: Hiển thị đếm ngược thời gian chờ kết nối trước khi hiện nút đóng khẩn cấp ===

				_ = Task.Run(async () =>

				{

					for (int i = 10; i > 0; i--)

					{

						if (_broadcastOverlay == null || _broadcastConnectionStatus == null || _broadcastConnectionStatus.Visibility != Visibility.Visible)

						{

							break;

						}

						

						int count = i;

						base.Dispatcher.Invoke(() =>

						{

							if (_broadcastStatusText != null)

							{

								_broadcastStatusText.Text = $"Đang kết nối luồng hình ảnh... ({count}s)";

							}

						});

						await Task.Delay(1000);

					}

					base.Dispatcher.Invoke(() =>

					{

						if (_broadcastOverlay != null && 

							_broadcastConnectionStatus != null && 

							_broadcastConnectionStatus.Visibility == Visibility.Visible)

						{

							emergencyCloseBtn.Visibility = Visibility.Visible;

							if (_broadcastStatusText != null)

							{

								_broadcastStatusText.Text = "Không kết nối được luồng hình ảnh. Bạn có thể đóng hoặc đợi thêm.";

							}

							Log.Information("[Kiosk] Emergency close button shown after 10s timeout.");

						}

					});

				});



				_broadcastWarningTextBlock = new TextBlock

				{

					Text = "",

					FontSize = 16.0,

					FontFamily = _broadcastFont,

					Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0)), // Màu cam

					FontWeight = FontWeights.Bold,

					HorizontalAlignment = HorizontalAlignment.Center,

					VerticalAlignment = VerticalAlignment.Bottom,

					Margin = new Thickness(0.0, 0.0, 0.0, 80.0),

					Visibility = Visibility.Collapsed

				};

				_broadcastOverlay.Children.Add(_broadcastWarningTextBlock);

				_lastBroadcastUpdateTime = DateTime.UtcNow;



				Grid grid = (Grid)base.Content;

				Grid.SetColumnSpan(_broadcastOverlay, 10);

				Grid.SetRowSpan(_broadcastOverlay, 10);

				grid.Children.Add(_broadcastOverlay);

				ApplyKioskMode(isForce);

				_overlayCreatedTime = DateTime.UtcNow; // Ghi nhận thời điểm tạo overlay



				// === UPGRADE_03: TCP Port Handshake Check ===

				string ipToHandshake = (((App)Application.Current)?.StudentNetwork)?.ServerIP;

				int port = _currentTeacherWebPort;

				if (!string.IsNullOrEmpty(ipToHandshake) && port > 0)

				{

					Task.Run(async () =>

					{

						var kioskConfig = LoadKioskConfig();

						if (!kioskConfig.PortHandshakeEnabled) return;



						bool isReachable = await IsPortReachableAsync(ipToHandshake, port, kioskConfig.PortHandshakeTimeoutMs);

						if (!isReachable)

						{

							Log.Warning("[Kiosk] Teacher HTTP stream port {Port} is UNREACHABLE.", port);

							

							// Chờ 3.5 giây xem có gói tin UDP Multicast dự phòng nào đến không

							await Task.Delay(3500);

							

							double udpElapsed = (DateTime.UtcNow - _lastUdpFrameTime).TotalSeconds;

							double overlayAge = (DateTime.UtcNow - _overlayCreatedTime).TotalSeconds;

							

							// Đóng overlay nếu: không nhận UDP frame > 3 giây hoặc đã đạt hard timeout

							bool shouldClose = (udpElapsed > 3.0) || (overlayAge >= HARD_TIMEOUT_SECONDS);

							

							if (_broadcastOverlay != null && shouldClose)

							{

								string reason = (overlayAge >= HARD_TIMEOUT_SECONDS)

									? $"HARD_TIMEOUT ({overlayAge:F1}s >= {HARD_TIMEOUT_SECONDS}s)"

									: $"NO_UDP_FRAME ({udpElapsed:F1}s > 3.0s)";

								

								Log.Warning("[Kiosk] Closing stuck overlay. Reason: {Reason}", reason);

								

								base.Dispatcher.Invoke(() =>

								{

									CloseScreenBroadcast();

									ShowFirewallWarningNotification(

										"⚠️ Cổng kết nối bị chặn", 

										"Không kết nối được luồng hình ảnh từ giáo viên. Cổng truyền hình ảnh có thể bị chặn bởi tường lửa.", 

										"#C62828"

									);

								});

							}

							else if (_broadcastOverlay != null)

							{

								// Có UDP frame nhưng chưa đạt hard timeout -> Lên lịch kiểm tra lại

								Log.Information("[Kiosk] UDP frames detected. Scheduling hard timeout check.");

								

								_ = Task.Run(async () =>

								{

									int remainingMs = Math.Max(0, (int)((HARD_TIMEOUT_SECONDS - overlayAge) * 1000));

									await Task.Delay(remainingMs);

									

									if (_broadcastOverlay != null)

									{

										Log.Warning("[Kiosk] HARD TIMEOUT reached after {Timeout}s. Force closing overlay.", HARD_TIMEOUT_SECONDS);

										base.Dispatcher.Invoke(() =>

										{

											CloseScreenBroadcast();

											ShowNotification(

												"⚠️ Mất kết nối", 

												"Đã tự động thoát chế độ trình chiếu do không nhận được hình ảnh.", 

												"#C62828"

											);

										});

									}

								});

							}

						}

					});

				}



				button.Click += delegate

				{

					// === PHASE6.1-BUG1: Close FIRST, set flag AFTER ===

					if (btnShowBroadcast != null)

					{

						btnShowBroadcast.Visibility = Visibility.Visible;

					}

					CloseScreenBroadcast();

					_userDismissedBroadcast = true; // PHASE6.1-BUG1: Set AFTER Close

					try

					{

						var app = (QASmartTouch.App)Application.Current;

						if (app?.StudentNetwork != null)

						{

							string studentCode = app.StudentNetwork.StudentCode ?? "HS00001";

							_ = app.StudentNetwork.SendAsync($"CMD|STUDENT_BROADCAST_STATUS|{studentCode}|DISMISSED");

						}

					}

					catch (Exception ex)

					{

						Log.Warning("[Kiosk] Send dismiss status error: {Err}", ex.Message);

					}

				};

				Log.Information("Screen broadcast overlay OPENED: {Path}", imagePath);

				

				// V4.1-FIX: Tải ảnh đầu tiên + cập nhật trạng thái đầy đủ để tránh "Đang kết nối..."

				Task.Run(() =>

				{

					BitmapSource? bitmapImage = LoadBitmapFromFile(imagePath);

					if (bitmapImage != null)

					{

						base.Dispatcher.Invoke(() =>

						{

							if (_broadcastImage != null)

							{

								_broadcastImage.Source = bitmapImage;

								_lastBroadcastUpdateTime = DateTime.UtcNow; // Cập nhật thời gian để watchdog không hiện cảnh báo

							}

							// Ẩn thông báo "Đang kết nối" ngay khi có ảnh đầu tiên

							if (_broadcastConnectionStatus != null)

							{

								_broadcastConnectionStatus.Visibility = Visibility.Collapsed;

							}

						});

					}

					else

					{

						// Tải lần đầu thất bại → hiện thông báo đang kết nối

						base.Dispatcher.Invoke(() =>

						{

							if (_broadcastConnectionStatus != null)

							{

								_broadcastConnectionStatus.Visibility = Visibility.Visible;

							}

						});

						Log.Warning("First broadcast image load failed, waiting for UDP/HTTP fallback: {Path}", imagePath);

					}

				});

			}

			catch (Exception ex)

			{

				Log.Warning("ShowScreenBroadcast error: {Err}", ex.Message);

			}

		}



		private void UpdateScreenBroadcast(string imagePath)

		{

			try

			{

				if (_broadcastOverlay == null || _broadcastImage == null)

				{

					return;

				}



				// If UDP is active, bypass HTTP updates to save bandwidth

				if ((DateTime.UtcNow - _lastUdpFrameTime).TotalSeconds < 2.0)

				{

					return;

				}

				if (!File.Exists(imagePath))
				{
					var config = LoadKioskConfig();
					bool allowFallback = config.StreamFallbackMode == "AutoFallback" || config.StreamFallbackMode == "HttpOnly";
					double elapsedUdp = (DateTime.UtcNow - _lastUdpFrameTime).TotalSeconds;

					if (_currentBroadcastProtocol != "HTTP_PULL" && (!allowFallback || elapsedUdp <= 3.0))
					{
						return; // Bỏ qua kéo HTTP để tránh quá tải CPU/Mạng khi đang dùng UDP
					}

					string teacherIP = (((App)Application.Current)?.StudentNetwork)?.ServerIP;

					if (!string.IsNullOrEmpty(teacherIP))

					{

						imagePath = $"http://{teacherIP}:{_currentTeacherWebPort}/api/broadcast_image?t={DateTime.Now.Ticks}" + (!string.IsNullOrEmpty(_currentBroadcastToken) ? $"&token={_currentBroadcastToken}" : "");

					}

					else

					{

						return;

					}

				}

				Task.Run(() =>

				{

					BitmapSource? bitmapImage = LoadBitmapFromFile(imagePath);

					if (bitmapImage != null)

					{

						_consecutiveHttpPullFailures = 0; // === UPGRADE_03 ===

						base.Dispatcher.Invoke(() =>

						{

							if (_broadcastImage != null)

							{

								_broadcastImage.Source = bitmapImage;

								_lastBroadcastUpdateTime = DateTime.UtcNow;

							}

							if (_broadcastConnectionStatus != null && _broadcastConnectionStatus.Visibility == Visibility.Visible)

							{

								_broadcastConnectionStatus.Visibility = Visibility.Collapsed;

							}

							if (_broadcastTimeText != null)

							{

								_broadcastTimeText.Text = $"🕒 {DateTime.Now:HH:mm:ss} (HTTP)";

							}

						});

					}

					else

					{

						// === UPGRADE_03: HTTP Pull Failure Counter (Thread-safe) ===

						var kioskConfig = LoadKioskConfig();

						int currentFailures = System.Threading.Interlocked.Increment(ref _consecutiveHttpPullFailures);

						Log.Warning("[Broadcast] HTTP Pull failed ({Count}/{Max}): {Path}", currentFailures, kioskConfig.MaxConsecutiveHttpFailures, imagePath);

						if (currentFailures >= kioskConfig.MaxConsecutiveHttpFailures)

						{

							System.Threading.Interlocked.Exchange(ref _consecutiveHttpPullFailures, 0);

							base.Dispatcher.Invoke(() =>

							{

								CloseScreenBroadcast();

								ShowNotification("⚠️ Lỗi kết nối", "Mất kết nối tới luồng hình ảnh của Giáo viên (HTTP Pull hỏng liên tục).", "#C62828");

							});

						}

					}

				});

			}

			catch (Exception ex)

			{

				Log.Warning("UpdateScreenBroadcast error: {Err}", ex.Message);

			}

		}



		// === UPGRADE_02: Cleanup toàn diện khi dừng broadcast ===

		internal void CloseScreenBroadcast()

		{

			try
			{
				// Dừng bộ giám sát tài nguyên (V2.2.4)
				QASmartClass.Services.AppPerformanceMonitor.Instance.Stop();

				// Tự động đóng VNC Client nếu đang chạy
				if (_isVncClientRunning)
				{
					CloseVncBroadcast();
				}

				// === UPGRADE_01: Tắt Kiosk Mode khi dừng broadcast ===

				ApplyKioskMode(false);



				_isBroadcastPausedByTeacher = false; // Reset pause state
				_pauseStartTime = DateTime.MinValue; // Reset pause safety timer

				_overlayCreatedTime = DateTime.MinValue; // === UPGRADE_04: Reset hard timeout anchor ===

				// === UPGRADE_13: Reset Turbo Mode variables ===
				_isTurboModeActive = false;
				_turboNeonBorder = null;

				_broadcastCloseButton = null;

				_broadcastWarningTextBlock = null;



				// === UPGRADE_02 FIX 1: Dừng Watchdog Timer ===

				if (_broadcastWatchdogTimer != null)

				{

					_broadcastWatchdogTimer.Stop();

					Log.Information("[Broadcast] Watchdog timer STOPPED");

				}



				if (_pulseStoryboard != null) { _pulseStoryboard.Stop(); _pulseStoryboard = null; }



				if (_broadcastOverlay != null)

				{

					Grid grid = (Grid)base.Content;

					grid.Children.Remove(_broadcastOverlay);

					_broadcastOverlay = null;

					_broadcastImage = null;
					_broadcastInkCanvas = null;
					_broadcastShapeCanvas = null;
					_broadcastTimeText = null;



					// Hủy đăng ký event handler TRƯỚC khi stop receiver

					QASmartClass.Services.UdpScreenBroadcastService.Instance.FrameReceived -= OnUdpFrameReceived;

					QASmartClass.Services.UdpScreenBroadcastService.Instance.StopReceiver();



					Log.Information("Screen broadcast overlay CLOSED — UDP receiver stopped");

				}



				// === UPGRADE_02 FIX 3: Reset App.BroadcastState ===

				if (Application.Current is App)

				{

					App.BroadcastState.IsScreenBroadcastActive = false;

					App.BroadcastState.ScreenCapturePath = string.Empty;

					App.BroadcastState.IsBroadcastCasting = false;

					Log.Information("[Broadcast] App.BroadcastState RESET");

				}

			}

			catch (Exception ex)

			{

				Log.Warning("CloseScreenBroadcast error: {Err}", ex.Message);

			}

		}



		// === UPGRADE_13: Phương thức tối ưu hóa hiệu năng giải phóng lag cho máy trạm yếu ===
		private void ApplyTurboModeOptimization()
		{
			if (_broadcastOverlay == null) return;

			if (_isTurboModeActive)
			{
				// 1. Tắt hiệu ứng nền, đổi sang đen phẳng nhẹ nhàng (nếu cấu hình là FlatBlack)
				if (_turboStudentBg == "FlatBlack")
				{
					_broadcastOverlay.Background = Brushes.Black;
				}
				else
				{
					_broadcastOverlay.Background = new SolidColorBrush(Color.FromArgb(245, 15, 15, 25));
				}

				// 2. Thêm viền Neon mỏng 2px xanh để báo hiệu kết nối Turbo đang chạy
				if (_turboNeonBorder == null)
				{
					_turboNeonBorder = new Border
					{
						BorderBrush = new SolidColorBrush(Color.FromRgb(0, 229, 255)), // Neon Blue #00E5FF
						BorderThickness = new Thickness(2),
						IsHitTestVisible = false
					};
					Grid.SetRowSpan(_turboNeonBorder, 10);
					Grid.SetColumnSpan(_turboNeonBorder, 10);
				}
				if (!_broadcastOverlay.Children.Contains(_turboNeonBorder))
				{
					_broadcastOverlay.Children.Add(_turboNeonBorder);
				}

				// 3. Ẩn và khóa bảng vẽ tương tác để tiết kiệm tài nguyên xử lý luồng
				if (_broadcastInkCanvas != null)
				{
					_broadcastInkCanvas.Visibility = Visibility.Collapsed;
				}
				if (_broadcastShapeCanvas != null)
				{
					_broadcastShapeCanvas.Visibility = Visibility.Collapsed;
				}

				// 4. Giảm tải tần suất timer refocus của Kiosk Mode
				if (_kioskRefocusTimer != null)
				{
					_kioskRefocusTimer.Interval = TimeSpan.FromSeconds(5); // Kéo giãn từ 1s lên 5s
				}
				
				Log.Information("[StudentShell] UPGRADE_13: Đã áp dụng các cấu hình tối ưu hóa phần cứng.");
			}
			else
			{
				// Trả lại cấu hình mặc định khi tắt Turbo
				_broadcastOverlay.Background = new SolidColorBrush(Color.FromArgb(245, 15, 15, 25));
				if (_turboNeonBorder != null && _broadcastOverlay.Children.Contains(_turboNeonBorder))
				{
					_broadcastOverlay.Children.Remove(_turboNeonBorder);
					_turboNeonBorder = null;
				}
				if (_broadcastInkCanvas != null)
				{
					_broadcastInkCanvas.Visibility = Visibility.Visible;
				}
				if (_broadcastShapeCanvas != null)
				{
					_broadcastShapeCanvas.Visibility = Visibility.Visible;
				}
				if (_kioskRefocusTimer != null)
				{
					_kioskRefocusTimer.Interval = TimeSpan.FromSeconds(1);
				}
			}
		}



		private void ShowBroadcast_Click(object sender, RoutedEventArgs e)

		{

			_userDismissedBroadcast = false;

			if (btnShowBroadcast != null)

			{

				btnShowBroadcast.Visibility = Visibility.Collapsed;

			}

			string cleanImagePath = App.BroadcastState?.ScreenCapturePath;

			if (string.IsNullOrEmpty(cleanImagePath))

			{

				string teacherIP = (((App)Application.Current)?.StudentNetwork)?.ServerIP;

				if (!string.IsNullOrEmpty(teacherIP))

				{

					cleanImagePath = $"http://{teacherIP}:{_currentTeacherWebPort}/api/broadcast_image";

				}

			}

			if (!string.IsNullOrEmpty(cleanImagePath))

			{

				ShowScreenBroadcast(cleanImagePath, _isBroadcastForceWatch, _currentBroadcastToken);

			}

		}



		// === UPGRADE_01: Helper methods cho Kiosk Mode ===

		private void ApplyKioskMode(bool isForce)

		{

			_isBroadcastForceWatch = isForce;

			if (isForce)

			{

				_activeKioskConfig = LoadKioskConfig();

				

				if (_activeKioskConfig.BroadcastMode == "FullLock")

				{

					KeyboardHookHelper.EnableHook();

					base.Topmost = true;

					base.WindowState = WindowState.Maximized;

					base.WindowStyle = WindowStyle.None;

					StartKioskRefocusTimer(_activeKioskConfig.RefocusIntervalMs);

				}

				else if (_activeKioskConfig.BroadcastMode == "TopmostOnly")

				{

					KeyboardHookHelper.DisableHook();

					base.Topmost = true;

					base.WindowState = WindowState.Maximized;

					base.WindowStyle = WindowStyle.None;

					StartKioskRefocusTimer(_activeKioskConfig.RefocusIntervalMs);

				}

				else // FreeView

				{

					KeyboardHookHelper.DisableHook();

					base.Topmost = false;

					base.WindowState = WindowState.Normal;

					base.WindowStyle = WindowStyle.SingleBorderWindow;

					StopKioskRefocusTimer();

				}



				if (_broadcastCloseButton != null)

				{

					_broadcastCloseButton.Visibility = Visibility.Collapsed;

				}

			}

			else

			{

				KeyboardHookHelper.DisableHook();

				StopKioskRefocusTimer();

				base.Topmost = false;

				base.WindowState = WindowState.Maximized;

				base.WindowStyle = WindowStyle.None;

				_activeKioskConfig = null;

				

				if (_broadcastCloseButton != null)

				{

					_broadcastCloseButton.Visibility = Visibility.Visible;

				}

			}

		}



		private KioskConfig? _cachedKioskConfig;
		private DateTime _lastKioskConfigLoadTime = DateTime.MinValue;

		private KioskConfig LoadKioskConfig()
		{
			var now = DateTime.UtcNow;
			if (_cachedKioskConfig != null && (now - _lastKioskConfigLoadTime).TotalSeconds < 5.0)
			{
				return _cachedKioskConfig;
			}

			var config = new KioskConfig();
			try
			{
				using var db = new AppDbContext();
				var modeSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Kiosk_BroadcastMode");
				config.BroadcastMode = modeSetting?.Value ?? "FullLock";

				var intervalSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Kiosk_RefocusInterval");
				if (intervalSetting != null && int.TryParse(intervalSetting.Value, out var ms))
				{
					config.RefocusIntervalMs = ms;
				}

				var timeoutSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Kiosk_WatchdogTimeout");
				if (timeoutSetting != null && int.TryParse(timeoutSetting.Value, out var sec))
				{
					config.WatchdogTimeoutSec = sec;
				}

				var bypassSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Kiosk_EmergencyBypass");
				config.EmergencyBypassEnabled = (bypassSetting?.Value ?? "Enabled") == "Enabled";

				var notifySetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Kiosk_StudentNotifyMode");
				config.StudentNotifyMode = notifySetting?.Value ?? "Overlay";

				// === UPGRADE_03 Master Settings ===
				var fallbackModeSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Kiosk_StreamFallbackMode");
				config.StreamFallbackMode = fallbackModeSetting?.Value ?? "AutoFallback";

				var maxFailuresSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Kiosk_MaxConsecutiveHttpFailures");
				if (maxFailuresSetting != null && int.TryParse(maxFailuresSetting.Value, out var maxVal))
				{
					config.MaxConsecutiveHttpFailures = maxVal;
				}

				var handshakeTimeoutSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Kiosk_PortHandshakeTimeoutMs");
				if (handshakeTimeoutSetting != null && int.TryParse(handshakeTimeoutSetting.Value, out var timeoutVal))
				{
					config.PortHandshakeTimeoutMs = timeoutVal;
				}

				var handshakeEnabledSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Kiosk_PortHandshakeEnabled");
				config.PortHandshakeEnabled = (handshakeEnabledSetting?.Value ?? "Enabled") == "Enabled";

				_cachedKioskConfig = config;
				_lastKioskConfigLoadTime = now;
			}
			catch (Exception ex)
			{
				Log.Warning("[Kiosk] Failed to load Kiosk config from DB: {Err}. Using defaults.", ex.Message);
				if (_cachedKioskConfig != null) return _cachedKioskConfig;
			}

			return config;
		}



		private void StartKioskRefocusTimer(int intervalMs)

		{

			if (_kioskRefocusTimer != null) return;

			

			_kioskRefocusTimer = new System.Windows.Threading.DispatcherTimer();

			_kioskRefocusTimer.Interval = TimeSpan.FromMilliseconds(intervalMs);

			_kioskRefocusTimer.Tick += (s, e) =>

			{

				try

				{

					if (_broadcastOverlay != null && _isBroadcastForceWatch && !_isBroadcastPausedByTeacher)

					{

						if (!this.IsActive)

						{

							base.Topmost = true;

							this.Activate();

							this.Focus();

							

							var handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;

							if (handle != IntPtr.Zero)

							{

								NativeMethods.SetForegroundWindow(handle);

							}

							

							Log.Warning("[Kiosk] Auto-refocus triggered - student attempted to switch window.");

						}

						

						if (_activeKioskConfig != null && _activeKioskConfig.BroadcastMode == "FullLock" && !KeyboardHookHelper.IsEnabled)

						{

							KeyboardHookHelper.EnableHook();

							Log.Warning("[Kiosk] Keyboard hook was lost, re-enabled.");

						}

					}

				}

				catch (Exception ex)

				{

					Log.Warning("[Kiosk] Refocus timer error: {Err}", ex.Message);

				}

			};

			_kioskRefocusTimer.Start();

			Log.Information("[Kiosk] Refocus timer started (interval: {Ms}ms).", intervalMs);

		}



		private void StopKioskRefocusTimer()

		{

			if (_kioskRefocusTimer != null)

			{

				_kioskRefocusTimer.Stop();

				_kioskRefocusTimer = null;

				Log.Information("[Kiosk] Refocus timer stopped.");

			}

		}



		private async Task<bool> IsPortReachableAsync(string host, int port, int timeoutMs)

		{

			try

			{

				using (var client = new System.Net.Sockets.TcpClient())

				{

					var connectTask = client.ConnectAsync(host, port);

					var delayTask = Task.Delay(timeoutMs);

					var completedTask = await Task.WhenAny(connectTask, delayTask);

					if (completedTask == connectTask)

					{

						await connectTask;

						return true;

					}

					return false;

				}

			}

			catch

			{

				return false;

			}

		}



		private class KioskConfig

		{

			public string BroadcastMode { get; set; } = "FullLock";

			public int RefocusIntervalMs { get; set; } = 500;

			public int WatchdogTimeoutSec { get; set; } = 10;

			public bool EmergencyBypassEnabled { get; set; } = true;

			public string StudentNotifyMode { get; set; } = "Overlay";



			// UPGRADE_03 additions:

			public string StreamFallbackMode { get; set; } = "AutoFallback";

			public int MaxConsecutiveHttpFailures { get; set; } = 5;

			public int PortHandshakeTimeoutMs { get; set; } = 1500;

			public bool PortHandshakeEnabled { get; set; } = true;

		}







		private static BitmapSource? LoadBitmapFromFile(string path)

		{

			try

			{

				byte[] data;

				if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 

					path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))

				{

					// V22-17: Static HttpClient thay WebClient deprecated

					data = _sharedHttpClient.GetByteArrayAsync(path).GetAwaiter().GetResult();

				}

				else

				{

					data = File.ReadAllBytes(path);

				}



				if (data == null || data.Length == 0) return null;



				if (IsWebPImage(data))

				{

					using (var mat = OpenCvSharp.Cv2.ImDecode(data, OpenCvSharp.ImreadModes.Color))

					{

						using (var bmp = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(mat))

						{

							return ConvertBitmapToBitmapSource(bmp);

						}

					}

				}



				BitmapImage bitmapImage = new BitmapImage();

				bitmapImage.BeginInit();

				bitmapImage.CacheOption = BitmapCacheOption.OnLoad;

				bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache;

				bitmapImage.StreamSource = new MemoryStream(data);

				bitmapImage.EndInit();

				bitmapImage.Freeze();

				return bitmapImage;

			}

			catch (Exception ex)

			{

				Log.Warning("LoadBitmapFromFile error: {Err} for path {Path}", ex.Message, path);

				return null;

			}

		}



		private void ShowFileBroadcast(string fileType, string filePath)

		{

			try

			{

				if (!File.Exists(filePath))

				{

					string teacherIP = (((App)Application.Current)?.StudentNetwork)?.ServerIP;

					if (!string.IsNullOrEmpty(teacherIP))

					{

						string nameParam = !string.IsNullOrEmpty(filePath) ? $"?name={Uri.EscapeDataString(System.IO.Path.GetFileName(filePath))}" : "";
						string tokenParam = !string.IsNullOrEmpty(_currentBroadcastToken) ? $"&token={_currentBroadcastToken}" : "";
						if (string.IsNullOrEmpty(nameParam) && !string.IsNullOrEmpty(tokenParam))
						{
							tokenParam = $"?token={_currentBroadcastToken}";
						}
						else if (!string.IsNullOrEmpty(nameParam) && !string.IsNullOrEmpty(tokenParam))
						{
							tokenParam = $"&token={_currentBroadcastToken}";
						}
						filePath = $"http://{teacherIP}:{_currentTeacherWebPort}/api/file_broadcast" + nameParam + tokenParam;

					}

					else

					{

						Log.Warning("File broadcast: file not found: {Path}", filePath);

						return;

					}

				}

				CloseFileBroadcast();

				

				bool isUrl = filePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 

							 filePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

				string fileName = "Tài liệu";

				string text = "Đang tải...";

				if (!isUrl)

				{

					fileName = Path.GetFileName(filePath);

					FileInfo fileInfo = new FileInfo(filePath);

					text = ((fileInfo.Length < 1048576) ? $"{(double)fileInfo.Length / 1024.0:F1} KB" : $"{(double)fileInfo.Length / 1048576.0:F1} MB");

				}

				_fileBroadcastOverlay = new Grid

				{

					Background = new SolidColorBrush(Color.FromArgb(240, 20, 20, 30)),

					Tag = "FileBroadcastOverlay"

				};

				if (1 == 0)

				{

				}

				string text2 = fileType switch

				{

					"IMAGE" => "\ud83d\uddbc\ufe0f", 

					"VIDEO" => "\ud83c\udfac", 

					"PDF" => "\ud83d\udcc4", 

					_ => "\ud83d\udcc1", 

				};

				if (1 == 0)

				{

				}

				string text3 = text2;

				if (1 == 0)

				{

				}

				text2 = fileType switch

				{

					"IMAGE" => "Ảnh", 

					"VIDEO" => "Video", 

					"PDF" => "Tài liệu PDF", 

					_ => "Tài liệu", 

				};

				if (1 == 0)

				{

				}

				string text4 = text2;

				Border border = new Border

				{

					Background = new LinearGradientBrush(Color.FromArgb(230, 25, 118, 210), Color.FromArgb(230, 13, 71, 161), 0.0),

					Height = 48.0,

					VerticalAlignment = VerticalAlignment.Top,

					Padding = new Thickness(16.0, 0.0, 16.0, 0.0)

				};

				DockPanel dockPanel = new DockPanel();

				Button button = new Button

				{

					Content = "✕ Đóng",

					FontSize = 13.0,

					Padding = new Thickness(14.0, 6.0, 14.0, 6.0),

					Background = new SolidColorBrush(Color.FromArgb(200, 229, 57, 53)),

					Foreground = Brushes.White,

					BorderThickness = new Thickness(0.0),

					Cursor = Cursors.Hand,

					VerticalAlignment = VerticalAlignment.Center

				};

				DockPanel.SetDock(button, Dock.Right);

				dockPanel.Children.Add(button);

				StackPanel stackPanel = new StackPanel

				{

					Orientation = Orientation.Horizontal,

					VerticalAlignment = VerticalAlignment.Center

				};

				stackPanel.Children.Add(new TextBlock

				{

					Text = $"{text3} {text4} từ GV — {fileName}",

					FontSize = 14.0,

					FontWeight = FontWeights.SemiBold,

					Foreground = Brushes.White,

					VerticalAlignment = VerticalAlignment.Center

				});

				stackPanel.Children.Add(new TextBlock

				{

					Text = "  (" + text + ")",

					FontSize = 11.0,

					Foreground = new SolidColorBrush(Color.FromArgb(180, byte.MaxValue, byte.MaxValue, byte.MaxValue)),

					VerticalAlignment = VerticalAlignment.Center

				});

				dockPanel.Children.Add(stackPanel);

				border.Child = dockPanel;

				UIElement element;

				switch (fileType)

				{
				case "PDF":
				{
					var webView = new Microsoft.Web.WebView2.Wpf.WebView2
					{
						Margin = new Thickness(20.0, 58.0, 20.0, 20.0),
						HorizontalAlignment = HorizontalAlignment.Stretch,
						VerticalAlignment = VerticalAlignment.Stretch
					};
					
					webView.Loaded += async (s, e) =>
					{
						try
						{
							await webView.EnsureCoreWebView2Async(null);
							
							bool isUrl2 = filePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
										 filePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
										 
							if (isUrl2)
							{
								webView.CoreWebView2.Navigate(filePath);
							}
							else
							{
								string folder = Path.GetDirectoryName(filePath) ?? "";
								string fileName2 = Path.GetFileName(filePath);
								
								if (Directory.Exists(folder))
								{
									webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
										"smartclass.assets", 
										folder, 
										Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);
										
									string encodedFileName = Uri.EscapeDataString(fileName2);
									webView.CoreWebView2.Navigate($"http://smartclass.assets/{encodedFileName}");
								}
								else
								{
									webView.CoreWebView2.Navigate(new Uri(filePath).AbsoluteUri);
								}
							}
						}
						catch (Exception ex)
						{
							Log.Warning("Student WebView2 PDF load error: {Err}", ex.Message);
						}
					};

					element = webView;
					break;
				}
            

				case "IMAGE":

				{

					BitmapSource? source = LoadBitmapFromFile(filePath);

					element = new Image

					{

						Source = source,

						Stretch = Stretch.Uniform,

						Margin = new Thickness(20.0, 58.0, 20.0, 20.0),

						HorizontalAlignment = HorizontalAlignment.Center,

						VerticalAlignment = VerticalAlignment.Center

					};

					break;

				}

				case "VIDEO":

				{

					Grid grid = new Grid

					{

						Margin = new Thickness(20.0, 58.0, 20.0, 60.0)

					};

					MediaElement mediaElement = new MediaElement

					{

						Source = new Uri(filePath, UriKind.Absolute),

						LoadedBehavior = MediaState.Manual,

						Stretch = Stretch.Uniform,

						HorizontalAlignment = HorizontalAlignment.Center,

						VerticalAlignment = VerticalAlignment.Center

					};

					grid.Children.Add(mediaElement);

					StackPanel stackPanel3 = new StackPanel

					{

						Orientation = Orientation.Horizontal,

						HorizontalAlignment = HorizontalAlignment.Center,

						VerticalAlignment = VerticalAlignment.Bottom,

						Margin = new Thickness(0.0, 0.0, 0.0, 10.0)

					};

					Button button3 = CreateVideoButton("▶ Phát", "#4CAF50");

					Button button4 = CreateVideoButton("⏸ Dừng", "#FF9800");

					Button button5 = CreateVideoButton("\ud83d\udd04 Từ đầu", "#2196F3");

					button3.Click += delegate

					{

						mediaElement.Play();

					};

					button4.Click += delegate

					{

						mediaElement.Pause();

					};

					button5.Click += delegate

					{

						mediaElement.Position = TimeSpan.Zero;

						mediaElement.Play();

					};

					stackPanel3.Children.Add(button3);

					stackPanel3.Children.Add(button4);

					stackPanel3.Children.Add(button5);

					grid.Children.Add(stackPanel3);

					mediaElement.Play();

					element = grid;

					break;

				}

				default:

				{

					StackPanel stackPanel2 = new StackPanel

					{

						HorizontalAlignment = HorizontalAlignment.Center,

						VerticalAlignment = VerticalAlignment.Center

					};

					stackPanel2.Children.Add(new TextBlock

					{

						Text = text3,

						FontSize = 80.0,

						HorizontalAlignment = HorizontalAlignment.Center,

						Margin = new Thickness(0.0, 0.0, 0.0, 20.0)

					});

					stackPanel2.Children.Add(new TextBlock

					{

						Text = fileName,

						FontSize = 20.0,

						FontWeight = FontWeights.Bold,

						Foreground = Brushes.White,

						HorizontalAlignment = HorizontalAlignment.Center,

						Margin = new Thickness(0.0, 0.0, 0.0, 8.0)

					});

					stackPanel2.Children.Add(new TextBlock

					{

						Text = "Kích thước: " + text,

						FontSize = 14.0,

						Foreground = new SolidColorBrush(Color.FromArgb(200, byte.MaxValue, byte.MaxValue, byte.MaxValue)),

						HorizontalAlignment = HorizontalAlignment.Center,

						Margin = new Thickness(0.0, 0.0, 0.0, 20.0)

					});

					Button button2 = new Button

					{

						Content = "\ud83d\udcc2 Mở " + text4,

						FontSize = 14.0,

						Padding = new Thickness(24.0, 10.0, 24.0, 10.0),

						Background = new SolidColorBrush(Color.FromRgb(25, 118, 210)),

						Foreground = Brushes.White,

						BorderThickness = new Thickness(0.0),

						Cursor = Cursors.Hand,

						HorizontalAlignment = HorizontalAlignment.Center

					};

					button2.Click += delegate

					{

						Task.Run(() =>

						{

							try

							{

								Process.Start("explorer.exe", filePath);

							}

							catch (Exception ex2)

							{

								Log.Warning("Open file error: {Err}", ex2.Message);

							}

						});

					};

					stackPanel2.Children.Add(button2);

					element = stackPanel2;

					break;

				}

				}

				_fileBroadcastOverlay.Children.Add(element);

				_fileBroadcastOverlay.Children.Add(border);

				Grid grid2 = (Grid)base.Content;

				Grid.SetColumnSpan(_fileBroadcastOverlay, 10);

				Grid.SetRowSpan(_fileBroadcastOverlay, 10);

				grid2.Children.Add(_fileBroadcastOverlay);

				button.Click += delegate

				{

					CloseFileBroadcast();

				};

				Log.Information("File broadcast overlay OPENED: {Type} {Path}", fileType, filePath);

			}

			catch (Exception ex)

			{

				Log.Warning("ShowFileBroadcast error: {Err}", ex.Message);

			}

		}



		private void CloseFileBroadcast()

		{

			try

			{

				if (_fileBroadcastOverlay == null)

				{

					return;

				}

				foreach (MediaElement item in FindVisualChildren<MediaElement>(_fileBroadcastOverlay))

				{

					item.Stop();

				}

				Grid grid = (Grid)base.Content;

				grid.Children.Remove(_fileBroadcastOverlay);

				_fileBroadcastOverlay = null;

				Log.Information("File broadcast overlay CLOSED");

			}

			catch (Exception ex)

			{

				Log.Warning("CloseFileBroadcast error: {Err}", ex.Message);

			}

		}



		private static Button CreateVideoButton(string text, string colorHex)

		{

			Color color = (Color)ColorConverter.ConvertFromString(colorHex);

			return new Button

			{

				Content = text,

				FontSize = 13.0,

				Padding = new Thickness(16.0, 8.0, 16.0, 8.0),

				Background = new SolidColorBrush(color),

				Foreground = Brushes.White,

				BorderThickness = new Thickness(0.0),

				Cursor = Cursors.Hand,

				Margin = new Thickness(4.0, 0.0, 4.0, 0.0)

			};

		}



		private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject

		{

			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)

			{

				DependencyObject child = VisualTreeHelper.GetChild(parent, i);

				if (child is T t)

				{

					yield return t;

				}

				foreach (T item in FindVisualChildren<T>(child))

				{

					yield return item;

				}

			}

		}



		private void LoadStudentDataFromDB()

		{

			try

			{

				App app = (App)Application.Current;

				if (app?.Database == null)

				{

					return;

				}

				string studentCode = "HS001";

				string name = "Học sinh";

				string machineName = Environment.MachineName;

				try

				{

					string studentProfileFile = AppPaths.StudentProfileFile;

					if (File.Exists(studentProfileFile))

					{

						string json = QASmartClass.StudentClient.Services.SecureProfileHelper.ReadProfileText(studentProfileFile);

						StudentProfileCache studentProfileCache = JsonSerializer.Deserialize<StudentProfileCache>(json);

						if (studentProfileCache != null)

						{

							studentCode = studentProfileCache.StudentCode;

							name = studentProfileCache.StudentName;

						}

					}

				}

				catch

				{

				}

				SetStudentInfo(name, studentCode, machineName);

				Student student = app.Database.Students.FirstOrDefault((Student s) => s.StudentCode == studentCode);

				if (student != null)

				{

					QASmartClass.Data.Classroom classroom = app.Database.Classrooms.Find(student.ClassroomId);

					if (classroom != null)

					{

						txtClassInfo.Text = classroom.Name + " — " + classroom.TeacherName;

					}

				}

				else

				{

					txtClassInfo.Text = "Chưa tham gia lớp (Local Mode)";

				}

				NetworkDiscoveryService networkService = app.NetworkService;

				if (networkService != null && networkService.IsBroadcasting)

				{

					txtConnectionStatus.Text = "Đã kết nối";

					txtConnectionStatus.Foreground = new SolidColorBrush(Color.FromRgb(102, 187, 106));

				}

				else

				{

					txtConnectionStatus.Text = "Chưa kết nối";

					txtConnectionStatus.Foreground = new SolidColorBrush(Color.FromRgb(158, 158, 158));

				}

				UpdateWifiIndicator();

			}

			catch (Exception ex)

			{

				Log.Warning("LoadStudentData error: {Err}", ex.Message);

			}

		}



		private void LoadTeacherInfo()

		{

			try

			{

				App app = (App)Application.Current;

				if (app?.Database != null)

				{

					TeacherProfile teacherProfile = app.Database.TeacherProfiles.FirstOrDefault();

					if (teacherProfile != null)

					{

						txtTeacherInfo.Text = "GV: " + teacherProfile.FullName;

					}

				}

			}

			catch (Exception ex)

			{

				Log.Warning("LoadTeacherInfo error: {Err}", ex.Message);

			}

		}



		private void LoadSavedAvatar()

		{

			try

			{

				string path = Path.Combine(AppPaths.RootDir, "Avatars", "Students");

				if (!Directory.Exists(path))

				{

					return;

				}

				string text = "HS001";

				try

				{

					string studentProfileFile = AppPaths.StudentProfileFile;

					if (File.Exists(studentProfileFile))

					{

						string json = QASmartClass.StudentClient.Services.SecureProfileHelper.ReadProfileText(studentProfileFile);

						StudentProfileCache studentProfileCache = JsonSerializer.Deserialize<StudentProfileCache>(json);

						if (studentProfileCache != null && !string.IsNullOrEmpty(studentProfileCache.StudentCode))

						{

							text = studentProfileCache.StudentCode;

						}

					}

				}

				catch

				{

				}

				string[] files = Directory.GetFiles(path, "avatar_" + text + ".*");

				if (files.Length != 0)

				{

					string text2 = files[0];

					BitmapImage bitmapImage = new BitmapImage();

					bitmapImage.BeginInit();

					bitmapImage.UriSource = new Uri(text2, UriKind.Absolute);

					bitmapImage.CacheOption = BitmapCacheOption.OnLoad;

					bitmapImage.DecodePixelWidth = 80;

					bitmapImage.EndInit();

					imgStudentAvatar.Source = bitmapImage;

					imgStudentAvatar.Visibility = Visibility.Visible;

					txtStudentInitials.Visibility = Visibility.Collapsed;

					Log.Information("Student avatar loaded from: {Path}", text2);

				}

			}

			catch (Exception ex)

			{

				Log.Warning("LoadSavedAvatar error: {Err}", ex.Message);

			}

		}



		private void NavButton_Click(object sender, RoutedEventArgs e)

		{

			if (sender is Button { Tag: string tag })

			{

				NavigateTo(tag);

			}

		}



		internal void NavigateTo(string tag)

		{

			if (_broadcastOverlay != null)
			{
				Log.Information("Navigation blocked: Screen broadcast is active");
				if ((DateTime.Now - _lastBlockNotificationTime).TotalSeconds > 2.0)
				{
					_lastBlockNotificationTime = DateTime.Now;
					ShowNotification("Tính năng bị khóa", "Giáo viên đang trình chiếu màn hình. Hãy tập trung theo dõi!", "#EF4444");
				}
				return;
			}

			if (App.FocusState.ActiveFocusSort >= 0 && tag != "S2" && tag != "S6" && tag != "S12")

			{

				Log.Information("Navigation blocked: Lesson focus is active. Exception allowed for Chat (S6) and Whiteboard (S12).");

				if ((DateTime.Now - _lastBlockNotificationTime).TotalSeconds > 2.0)

				{

					_lastBlockNotificationTime = DateTime.Now;

					ShowNotification("Tính năng bị khóa", "Giáo viên đang bật chế độ Tập trung giảng bài. Hãy quay lại bài giảng hoặc bảng vẽ nhé!", "#EF4444");

				}

				return;

			}

			if (_isQuizFocusActive && tag != "S3")

			{

				Log.Information("Navigation blocked: Quiz focus is active");

				if ((DateTime.Now - _lastBlockNotificationTime).TotalSeconds > 2.0)

				{

					_lastBlockNotificationTime = DateTime.Now;

					ShowNotification("Tính năng bị khóa", "Em đang trong quá trình làm bài kiểm tra. Hãy tập trung hoàn thành bài trước nhé!", "#EF4444");

				}

				return;

			}

			_currentPage = tag;

			if (tag == "S6")

			{

				ResetStudentBadge();

			}

			if (1 == 0)

			{

			}

			Page page = tag switch

			{

				"S1" => new StudentDashboardPage(), 

				"S2" => new StudentLessonPage(), 

				"S3" => new StudentQuizPage(), 

				"S4" => new StudentSubmitPage(), 

				"S5" => new StudentSurveyPage(), 

				"S6" => (_chatPage ??= new StudentChatPage()), 

				"S7" => new StudentHandRaisePage(), 

				"S8" => new StudentResultsPage(), 

				"S9" => new StudentSettingsPage(), 

				"S10" => new StudentGuidePage(), 

				"S11" => new AnalyticsPage(), 

				"S12" => new StudentLocalWhiteboardPage(), 

				_ => null, 

			};

			if (1 == 0)

			{

			}

			Page page2 = page;

			if (page2 != null)

			{

				contentFrame.Navigate(page2);

				TextBlock textBlock = txtPageTitle;

				if (1 == 0)

				{

				}

				string text = tag switch

				{

					"S1" => "Trang chủ", 

					"S2" => "Bài giảng hôm nay", 

					"S3" => "Bài kiểm tra", 

					"S4" => "Bài tập học tập", 

					"S5" => "Khảo sát", 

					"S6" => "Tin nhắn", 

					"S7" => "Hỏi bài Giáo viên", 

					"S8" => "Kết quả học tập", 

					"S9" => "Cài đặt", 

					"S10" => "Hướng dẫn sử dụng", 

					"S11" => "Tiến trình học tập", 

					"S12" => "Bảng trắng học sinh", 

					_ => "Trang chủ", 

				};

				if (1 == 0)

				{

				}

				textBlock.Text = text;

				UpdateNavHighlight(tag);

			}

		}



		private void UpdateNavHighlight(string activeTag)

		{

			Style style = FindResource("StuNavActive") as Style;

			Style style2 = FindResource("StuNav") as Style;

			foreach (object child in navPanel.Children)

			{

				if (child is Button button)

				{

					button.Style = ((button.Tag?.ToString() == activeTag) ? style : style2);

				}

			}

			UpdateSidebarVisualState(_isSidebarCollapsed);

		}



		private void StudentAvatar_Click(object sender, MouseButtonEventArgs e)

		{

			try

			{

				OpenFileDialog openFileDialog = new OpenFileDialog

				{

					Title = "Chọn ảnh đại diện",

					Filter = "Hình ảnh (*.jpg, *.jpeg, *.png, *.bmp, *.webp)|*.jpg;*.jpeg;*.png;*.bmp;*.webp",

					InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)

				};

				if (openFileDialog.ShowDialog() != true)

				{

					return;

				}

				string text = Path.Combine(AppPaths.RootDir, "Avatars", "Students");

				Directory.CreateDirectory(text);

				string text2 = "HS001";

				try

				{

					string studentProfileFile = AppPaths.StudentProfileFile;

					if (File.Exists(studentProfileFile))

					{

						string json = QASmartClass.StudentClient.Services.SecureProfileHelper.ReadProfileText(studentProfileFile);

						StudentProfileCache studentProfileCache = JsonSerializer.Deserialize<StudentProfileCache>(json);

						if (studentProfileCache != null && !string.IsNullOrEmpty(studentProfileCache.StudentCode))

						{

							text2 = studentProfileCache.StudentCode;

						}

					}

				}

				catch

				{

				}

				string[] files = Directory.GetFiles(text, "avatar_" + text2 + ".*");

				foreach (string path in files)

				{

					try

					{

						File.Delete(path);

					}

					catch

					{

					}

				}

				string text3 = Path.Combine(text, "avatar_" + text2 + ".jpg");

				try

				{

					using (FileStream fs = new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.Read))

					{

						System.Windows.Media.Imaging.BitmapDecoder decoder = System.Windows.Media.Imaging.BitmapDecoder.Create(fs, System.Windows.Media.Imaging.BitmapCreateOptions.None, System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);

						System.Windows.Media.Imaging.BitmapFrame frame = decoder.Frames[0];

						

						double maxDim = 128.0;

						double width = frame.PixelWidth;

						double height = frame.PixelHeight;

						

						if (width > maxDim || height > maxDim)

						{

							double scale = Math.Min(maxDim / width, maxDim / height);

							width *= scale;

							height *= scale;

						}

						

						System.Windows.Media.Imaging.TransformedBitmap scaledBitmap = new System.Windows.Media.Imaging.TransformedBitmap(frame, new ScaleTransform(width / frame.PixelWidth, height / frame.PixelHeight));

						

						System.Windows.Media.Imaging.JpegBitmapEncoder encoder = new System.Windows.Media.Imaging.JpegBitmapEncoder

						{

							QualityLevel = 75

						};

						encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(scaledBitmap));

						

						using (FileStream outStream = new FileStream(text3, FileMode.Create, FileAccess.Write))

						{

							encoder.Save(outStream);

						}

					}

				}

				catch (Exception exImage)

				{

					Log.Warning("Failed to compress avatar, falling back to copy raw file: {Err}", exImage.Message);

					string extension = Path.GetExtension(openFileDialog.FileName).ToLower();

					string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".webp" };

					if (!allowedExtensions.Contains(extension))

					{

						MessageBox.Show("Định dạng tệp tin không hợp lệ! Chỉ cho phép chọn hình ảnh.", "Lỗi định dạng", MessageBoxButton.OK, MessageBoxImage.Warning);

						return;

					}

					// Sanitize file path

					string safeFileName = "avatar_" + text2 + extension;

					text3 = Path.Combine(text, safeFileName);

					File.Copy(openFileDialog.FileName, text3, overwrite: true);

				}

				System.Windows.Media.Imaging.BitmapImage bitmapImage = new System.Windows.Media.Imaging.BitmapImage();

				bitmapImage.BeginInit();

				bitmapImage.UriSource = new Uri(text3, UriKind.Absolute);

				bitmapImage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;

				bitmapImage.DecodePixelWidth = 80;

				bitmapImage.EndInit();

				imgStudentAvatar.Source = bitmapImage;

				imgStudentAvatar.Visibility = Visibility.Visible;

				txtStudentInitials.Visibility = Visibility.Collapsed;

				try

				{

					App app = (App)Application.Current;

					if (app?.Database != null)

					{

						Student student = app.Database.Students.FirstOrDefault();

						if (student != null)

						{

							student.AvatarPath = text3;

							app.Database.SaveChanges();

						}

					}

				}

				catch

				{

				}

				Log.Information("Student avatar updated: {Path}", text3);

			}

			catch (Exception ex)

			{

				Log.Warning("Student avatar error: {Err}", ex.Message);

				MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Exclamation);

			}

		}



		public void SetStudentInfo(string name, string code, string pc)

		{

			txtStudentName.Text = name;

			txtStudentId.Text = "Mã HS: " + code + "  •  " + pc;

			try

			{

				string[] array = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

				txtStudentInitials.Text = ((array.Length >= 2) ? (array[0].Substring(0, 1) + array[^1].Substring(0, 1)).ToUpper() : ((array.Length != 0) ? array[0].Substring(0, 1).ToUpper() : "HS"));

			}

			catch

			{

				txtStudentInitials.Text = "HS";

			}

 

			try

			{

				App app = (App)Application.Current;

				if (app?.Database != null)

				{

					var student = app.Database.Students.FirstOrDefault(s => s.StudentCode == code);

					if (student != null)

					{

						int totalXp = student.TotalXp;

						int level = (int)Math.Floor(Math.Sqrt(totalXp / 100.0)) + 1;

						int xpForCurrentLevel = (level - 1) * (level - 1) * 100;

						int xpForNextLevel = level * level * 100;

						int xpInThisLevel = totalXp - xpForCurrentLevel;

						int xpNeededForNextLevel = xpForNextLevel - xpForCurrentLevel;

 

						lblLevel.Text = $"Cấp {level}";

						lblXpValue.Text = $"{xpInThisLevel}/{xpNeededForNextLevel} điểm";

						pbXpProgress.Maximum = xpNeededForNextLevel;

						pbXpProgress.Value = xpInThisLevel;

					}

				}

			}

			catch (Exception exXp)

			{

				Log.Warning("Failed to load student XP: {Err}", exXp.Message);

			}

		}



		private void SwitchToTeacher_Click(object sender, RoutedEventArgs e)

		{

			try

			{

				if (ShowTeacherAuthDialog())

				{

					App app = (App)Application.Current;

					app.ModeService.GoToClass();

					Log.Information("Switched to Teacher mode from StudentShell (authenticated)");

				}

			}

			catch (Exception ex)

			{

				Log.Warning("Switch to teacher error: {Err}", ex.Message);

				MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Exclamation);

			}

		}



		private bool ShowTeacherAuthDialog()

		{

			Window dlg = new Window

			{

				Title = "\ud83d\udd12 Xác thực Giáo viên",

				Width = 380.0,

				Height = 250.0,

				WindowStartupLocation = WindowStartupLocation.CenterScreen,

				WindowStyle = WindowStyle.ToolWindow,

				ResizeMode = ResizeMode.NoResize,

				Background = new SolidColorBrush(Color.FromRgb(250, 250, 250))

			};

			StackPanel stackPanel = new StackPanel

			{

				Margin = new Thickness(24.0)

			};

			TextBlock element = new TextBlock

			{

				Text = "\ud83d\udc68\u200d\ud83c\udfeb Chuyển sang giao diện Giáo viên",

				FontSize = 15.0,

				FontWeight = FontWeights.Bold,

				Foreground = new SolidColorBrush(Color.FromRgb(30, 30, 30)),

				Margin = new Thickness(0.0, 0.0, 0.0, 4.0)

			};

			TextBlock element2 = new TextBlock

			{

				Text = "Vui lòng nhập mật khẩu giáo viên để tiếp tục",

				FontSize = 11.0,

				Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)),

				Margin = new Thickness(0.0, 0.0, 0.0, 16.0)

			};

			TextBlock element3 = new TextBlock

			{

				Text = "Mật khẩu:",

				FontSize = 12.0,

				FontWeight = FontWeights.SemiBold,

				Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66)),

				Margin = new Thickness(0.0, 0.0, 0.0, 6.0)

			};

			PasswordBox passBox = new PasswordBox

			{

				FontSize = 14.0,

				Padding = new Thickness(10.0, 8.0, 10.0, 8.0),

				MaxLength = 50

			};

			passBox.KeyDown += delegate(object s, KeyEventArgs ke)

			{

				if (ke.Key == Key.Return)

				{

					dlg.DialogResult = true;

				}

			};

			TextBlock lblError = new TextBlock

			{

				Text = "",

				FontSize = 11.0,

				Foreground = new SolidColorBrush(Color.FromRgb(211, 47, 47)),

				Margin = new Thickness(0.0, 4.0, 0.0, 0.0),

				Visibility = Visibility.Collapsed

			};

			StackPanel stackPanel2 = new StackPanel

			{

				Orientation = Orientation.Horizontal,

				HorizontalAlignment = HorizontalAlignment.Right,

				Margin = new Thickness(0.0, 16.0, 0.0, 0.0)

			};

			Button button = new Button

			{

				Content = "Hủy",

				Padding = new Thickness(20.0, 8.0, 20.0, 8.0),

				FontSize = 12.0,

				Margin = new Thickness(0.0, 0.0, 8.0, 0.0),

				Cursor = Cursors.Hand

			};

			button.Click += delegate

			{

				dlg.DialogResult = false;

			};

			Button button2 = new Button

			{

				Content = "\ud83d\udd13 Xác nhận",

				Padding = new Thickness(20.0, 8.0, 20.0, 8.0),

				FontSize = 12.0,

				Background = new SolidColorBrush(Color.FromRgb(230, 81, 0)),

				Foreground = Brushes.White,

				BorderThickness = new Thickness(0.0),

				Cursor = Cursors.Hand

			};

			button2.Click += delegate

			{

				dlg.DialogResult = true;

			};

			stackPanel2.Children.Add(button);

			stackPanel2.Children.Add(button2);

			stackPanel.Children.Add(element);

			stackPanel.Children.Add(element2);

			stackPanel.Children.Add(element3);

			stackPanel.Children.Add(passBox);

			stackPanel.Children.Add(lblError);

			stackPanel.Children.Add(stackPanel2);

			dlg.Content = stackPanel;

			dlg.Loaded += delegate

			{

				passBox.Focus();

			};

			bool authenticated = false;

			int attempts = 0;

			dlg.Closing += delegate(object? s, CancelEventArgs ev)

			{

				if (dlg.DialogResult == true)

				{

					string password = passBox.Password;

					bool flag = false;

					try

					{

						App app = (App)Application.Current;

						TeacherProfile teacherProfile = app.Database?.TeacherProfiles.FirstOrDefault();

						flag = (teacherProfile != null && !string.IsNullOrEmpty(teacherProfile.PasswordHash) && AuthenticationService.VerifyPassword(password, teacherProfile.PasswordHash));

					}

					catch

					{

						flag = false;

					}

					if (flag)

					{

						authenticated = true;

					}

					else

					{

						attempts++;

						lblError.Text = $"❌ Sai mật khẩu! ({3 - attempts} lần thử còn lại)";

						lblError.Visibility = Visibility.Visible;

						passBox.Clear();

						passBox.Focus();

						if (attempts >= 3)

						{

							MessageBox.Show("Đã vượt quá số lần thử cho phép.\nVui lòng liên hệ giáo viên.", "Từ chối truy cập", MessageBoxButton.OK, MessageBoxImage.Exclamation);

							dlg.DialogResult = false;

							authenticated = false;

						}

						else

						{

							ev.Cancel = true;

						}

					}

				}

			};

			dlg.ShowDialog();

			return authenticated;

		}



		private void SwitchToScreen_Click(object sender, RoutedEventArgs e)

		{

			try

			{

				if (ShowTeacherAuthDialog())

				{

					App app = (App)Application.Current;

					app.ModeService.GoToScreen();

					Log.Information("Switched to Smart Touch from StudentShell (authenticated)");

				}

			}

			catch (Exception ex)

			{

				Log.Warning("Switch to screen error: {Err}", ex.Message);

				MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Exclamation);

			}

		}



		private void CloseApp_Click(object sender, RoutedEventArgs e)

		{

			// V4.1-FIX: R4 — Thao tác thoát BẮT BUỘC có dialog xác nhận

			var result = MessageBox.Show(

				"Bạn có chắc chắn muốn thoát chương trình?\n\nDữ liệu chưa lưu có thể bị mất.",

				"Xác nhận thoát — QA Smart Class",

				MessageBoxButton.YesNo,

				MessageBoxImage.Question,

				MessageBoxResult.No // Mặc định = Không thoát (fail-safe)

			);

			if (result == MessageBoxResult.Yes)

			{

				this.Close();

			}

		}



		private async void Reconnect_Click(object sender, RoutedEventArgs e)

		{

			if (_isResettingNetwork) return;

			_isResettingNetwork = true;

			var reconnectButton = sender as Button;

			if (reconnectButton != null) reconnectButton.IsEnabled = false;



			try

			{

				Log.Information("Manual reconnection requested by student");

				txtConnectionStatus.Text = "Đang thử lại...";

				txtConnectionStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0));

				App app = (App)Application.Current;

				if (app.StudentNetwork != null)

				{

					if (app.StudentNetwork.IsConnected)

					{

						txtConnectionStatus.Text = "Đã kết nối — " + app.StudentNetwork.ClassName;

						txtConnectionStatus.Foreground = new SolidColorBrush(Color.FromRgb(102, 187, 106));

						UpdateWifiIndicator();

						return;

					}

					app.StudentNetwork.Stop();

					await Task.Delay(500);

					await StartStudentNetworkAsync();

					UpdateWifiIndicator();

				}

			}

			catch (Exception ex)

			{

				Log.Warning("Manual reconnection failed: {Err}", ex.Message);

				txtConnectionStatus.Text = "Lỗi kết nối";

				txtConnectionStatus.Foreground = new SolidColorBrush(Color.FromRgb(239, 83, 80));

				UpdateWifiIndicator();

			}

			finally

			{

				if (reconnectButton != null) reconnectButton.IsEnabled = true;

				_isResettingNetwork = false;

			}

		}



		private async void QuickHandRaise_Click(object sender, RoutedEventArgs e)

		{

			if ((DateTime.Now - _lastQuickRaiseTime).TotalMilliseconds < 1000)

			{

				return;

			}

			_lastQuickRaiseTime = DateTime.Now;



			try

			{

				App app = (App)Application.Current;

				var client = app.StudentNetwork;

				var net = app.NetworkService;

				bool targetState = !client.IsHandRaised;



				bool hasNetwork = client.IsConnected || (net != null && net.IsBroadcasting);

				if (!hasNetwork)

				{

					MessageBox.Show("Không có kết nối mạng. Vui lòng kiểm tra lại đường truyền!", "Lỗi kết nối", MessageBoxButton.OK, MessageBoxImage.Warning);

					return;

				}



				client.IsHandRaised = targetState;

				UpdateQuickHandRaiseUI(targetState);



				string cleanReason = "Phát biểu";



				if (client.IsConnected)

				{

					await client.SendHandRaiseWithReason(targetState, cleanReason);

				}

				else if (net != null && net.IsBroadcasting)

				{

					string classCode = client.ClassCode;

					if (string.IsNullOrEmpty(classCode)) classCode = "DEFAULT_CLASS";

					

					long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

					if (client.ClockDrift != 0)

					{

						timestamp += client.ClockDrift;

					}

					

					string payload = $"raised={targetState}|reason={cleanReason}|ts={timestamp}";

					string encryptedPayload = QASmartClass.Utilities.CryptoHelper.Encrypt(payload, classCode);

					await net.SendCommandAsync($"HAND_RAISE_ENC|payload={encryptedPayload}");

				}



				if (app.Database != null)

				{

					app.Database.EventLogs.Add(new QASmartClass.Data.EventLog

					{

						EventType = "HAND_RAISE",

						Actor = "Student",

						Details = $"Giơ tay nhanh: {targetState} | Lý do: {cleanReason}",

						Timestamp = DateTime.Now

					});

					await app.Database.SaveChangesAsync();

				}



				Log.Information("Quick hand raise state changed: {State}", targetState);

			}

			catch (Exception ex)

			{

				Log.Warning("Quick hand raise click error: {Err}", ex.Message);

			}

		}



		private void UpdateQuickHandRaiseUI(bool raised)

		{

			base.Dispatcher.Invoke(delegate

			{

				if (btnQuickHandRaise == null) return;

				if (raised)

				{

					btnQuickHandRaise.Content = "✋ Hạ tay xuống";

					btnQuickHandRaise.ToolTip = "Bấm để hạ tay phát biểu";

					var border = btnQuickHandRaise.Template.FindName("b", btnQuickHandRaise) as Border;

					if (border != null)

					{

						border.Background = new SolidColorBrush(Color.FromRgb(230, 81, 0));

					}

				}

				else

				{

					btnQuickHandRaise.Content = "✋ Giơ tay phát biểu";

					btnQuickHandRaise.ToolTip = "Bấm để giơ tay phát biểu";

					var border = btnQuickHandRaise.Template.FindName("b", btnQuickHandRaise) as Border;

					if (border != null)

					{

						border.Background = new SolidColorBrush(Color.FromRgb(71, 85, 105));

					}

				}

			});

		}



		private void LoadSavedExitPinConfig()

		{

			try

			{

				string profilePath = AppPaths.StudentProfileFile;

				if (File.Exists(profilePath))

				{

					string json = QASmartClass.StudentClient.Services.SecureProfileHelper.ReadProfileText(profilePath);

					StudentProfileCache profile = JsonSerializer.Deserialize<StudentProfileCache>(json);

					if (profile != null)

					{

						_isExitPinRequired = profile.IsExitPinRequired;

						_exitPinCode = profile.ExitPinCode;

						Log.Information("Đã nạp cấu hình Exit PIN cục bộ: Yêu cầu={Req}, PIN={Pin}", _isExitPinRequired, _exitPinCode);

					}

				}

			}

			catch (Exception ex)

			{

				Log.Warning("Lỗi nạp cấu hình Exit PIN cục bộ: {Err}", ex.Message);

			}

		}



		private void Client_PropertyChanged(object? sender, PropertyChangedEventArgs e)

		{

			if (e.PropertyName == "IsHandRaised")

			{

				var app = Application.Current as App;

				if (app?.StudentNetwork != null)

				{

					UpdateQuickHandRaiseUI(app.StudentNetwork.IsHandRaised);

				}

			}

		}



		private void UpdateWifiIndicator()

		{

			try

			{

				var app = Application.Current as App;

				bool isConnected = app?.StudentNetwork?.IsConnected ?? false;

				if (isConnected)

				{

					txtWifiIndicator.Text = "ONLINE";

					txtWifiIndicator.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // Green #10B981

					txtWifiIndicator.ToolTip = "Đã kết nối trực tuyến";

				}

				else

				{

					txtWifiIndicator.Text = "OFFLINE";

					txtWifiIndicator.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red #EF4444

					txtWifiIndicator.ToolTip = "Mất kết nối mạng";

				}

			}

			catch (Exception ex)

			{

				Log.Warning("UpdateWifiIndicator error: {Err}", ex.Message);

			}

		}



		private void Client_Connected(object? sender, EventArgs e)

		{

			base.Dispatcher.Invoke(delegate

			{

				var app = Application.Current as App;

				if (app?.StudentNetwork != null)

				{

					var client = app.StudentNetwork;

					txtConnectionStatus.Text = "Đã kết nối — " + client.ClassName;

					txtConnectionStatus.Foreground = new SolidColorBrush(Color.FromRgb(102, 187, 106));

					txtClassInfo.Text = client.ClassName + " — " + client.TeacherName;

					txtTeacherInfo.Text = "GV: " + client.TeacherName;

					Log.Information("Student connected to teacher: {Teacher}", client.TeacherName);

					

					// Crash Recovery (Đề xuất 2): Query teacher for forced broadcast on reconnect/startup

					_ = client.SendAsync("CMD|GET_BROADCAST_STATUS");

				}

				UpdateWifiIndicator();

			});

		}



		private void Client_Disconnected(object? sender, EventArgs e)

		{

			base.Dispatcher.Invoke(delegate

			{

				txtConnectionStatus.Text = "Mất kết nối";

				txtConnectionStatus.Foreground = new SolidColorBrush(Color.FromRgb(239, 83, 80));

				CloseScreenBroadcast();

				CloseScreenLockOverlay();

				UpdateWifiIndicator();

			});

		}



		private void Client_CommandReceived(object? sender, string cmd)

		{

			base.Dispatcher.Invoke(delegate

			{

				Log.Information("Student received command: {Cmd}", cmd);

				HandleTeacherCommand(cmd);

			});

		}



		private void Client_MessageReceived(object? sender, string msg)

		{

			base.Dispatcher.Invoke(delegate

			{

				Log.Information("Student received message: {Msg}", msg);

				HandleTeacherMessage(msg);

			});

		}



		private void App_LocalCommandReceived(object? sender, string cmd)

		{

			base.Dispatcher.Invoke(delegate

			{

				Log.Information("Student received LOCAL command: {Cmd}", cmd);

				HandleTeacherCommand(cmd);

			});

		}



	private void StudentShell_Closing(object? sender, System.ComponentModel.CancelEventArgs e)

	{

		if (_broadcastOverlay != null && _isBroadcastForceWatch && !_isBroadcastPausedByTeacher)

		{

			bool isUnitTest = AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName?.StartsWith("xunit", StringComparison.OrdinalIgnoreCase) ?? false);

			if (!isUnitTest)

			{

				ShowNotification("Trình chiếu bắt buộc", "Không thể đóng chương trình khi Giáo viên đang trình chiếu bắt buộc.", "#C62828");

			}

			e.Cancel = true;

			return;

		}



		if (_isCloseAuthorized)

		{

			return;

		}



		if (_isExitPinRequired && !string.IsNullOrEmpty(_exitPinCode))

		{

			if (_lockoutEndTime.HasValue && DateTime.Now < _lockoutEndTime.Value)

			{

				double remainingSecs = Math.Ceiling((_lockoutEndTime.Value - DateTime.Now).TotalSeconds);

				ShowNotification("Khóa bảo mật", $"Chức năng thoát bị khóa do nhập sai nhiều lần. Vui lòng thử lại sau {remainingSecs} giây.", "#C62828");

				e.Cancel = true;

				return;

			}



			string inputPin = PromptForTeacherPinDialog();

			if (string.IsNullOrEmpty(inputPin))

			{

				e.Cancel = true;

				return;

			}



			App appInstance = (App)Application.Current;

			string classCode = appInstance.StudentNetwork?.ClassCode ?? "DEFAULT_CLASS";

			if (string.IsNullOrEmpty(classCode)) classCode = "DEFAULT_CLASS";

			string sessionSalt = appInstance.StudentNetwork?.SessionSalt ?? string.Empty;

			string salt = !string.IsNullOrEmpty(sessionSalt) ? sessionSalt : classCode;



			string hashedInput = QASmartClass.Services.ClassControlService.ComputeSha256Hash(inputPin, salt);

			if (hashedInput != _exitPinCode)

			{

				_failedPinAttempts++;

				if (_failedPinAttempts >= 5)

				{

					_lockoutEndTime = DateTime.Now.AddMinutes(5);

					ShowNotification("Cảnh báo bảo mật", "Mã PIN của giáo viên không chính xác! Bạn đã nhập sai quá 5 lần. Chức năng thoát sẽ bị khóa trong 5 phút.", "#C62828");

				}

				else

				{

					ShowNotification("Cảnh báo bảo mật", $"Mã PIN của giáo viên không chính xác! ({5 - _failedPinAttempts} lần thử còn lại) Không thể đóng chương trình.", "#C62828");

				}

				e.Cancel = true;

				return;

			}

			else

			{

				_failedPinAttempts = 0;

				_lockoutEndTime = null;

			}

		}



		bool isTestEnv = AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName?.StartsWith("xunit", StringComparison.OrdinalIgnoreCase) ?? false);

		MessageBoxResult messageBoxResult = MessageBoxResult.Yes;

		if (!isTestEnv)

		{

			messageBoxResult = MessageBox.Show("Em có chắc chắn muốn đóng chương trình?\n\nTất cả kết nối sẽ bị ngắt.", "Đóng chương trình", MessageBoxButton.YesNo, MessageBoxImage.Question);

		}

		if (messageBoxResult == MessageBoxResult.Yes)

		{

			try

			{

				_clockTimer?.Stop();

				_webBlockTimer?.Stop();

				if (_focusWatchdogTimer != null)

				{

					_focusWatchdogTimer.Stop();

				}

				App app = (App)Application.Current;

				// === UPGRADE_02 FIX: Cleanup broadcast socket khi đóng cửa sổ ===

				CloseScreenBroadcast();

				if (_broadcastWatchdogTimer != null)

				{

					_broadcastWatchdogTimer.Stop();

				}

				if (app.StudentNetwork != null)

				{

					app.StudentNetwork.PropertyChanged -= Client_PropertyChanged;

					app.StudentNetwork.Connected -= Client_Connected;

					app.StudentNetwork.Disconnected -= Client_Disconnected;

					app.StudentNetwork.CommandReceived -= Client_CommandReceived;

					app.StudentNetwork.MessageReceived -= Client_MessageReceived;

					app.StudentNetwork.Stop();

				}

				app.LocalCommandReceived -= App_LocalCommandReceived;

				Log.Information("Application closed from StudentShell");

			}

			catch (Exception ex)

			{

				Log.Warning(ex, "Error during StudentShell cleanup on closing");

			}

			_isCloseAuthorized = true;

		}

		else

		{

			e.Cancel = true;

		}

	}



	private void TryAutoRecoverForceWatch(string source)

	{

		if (!_isBroadcastForceWatch || _isBroadcastPausedByTeacher) return;

		if (_broadcastOverlay == null) return;



		// Debounce: no-op if last recovery was < 2 seconds ago

		if ((DateTime.UtcNow - _lastAutoRecoveryTime).TotalSeconds < 2) return;

		_lastAutoRecoveryTime = DateTime.UtcNow;



		base.Dispatcher.Invoke(() =>

		{

			if (this.WindowState == WindowState.Minimized)

				this.WindowState = WindowState.Maximized;

			if (!this.Topmost)

				this.Topmost = true;

			base.Activate();

			base.Focus();

			ShowNotification("⚠️ Cảnh báo", "Không được ẩn màn hình trình chiếu bắt buộc!", "#E65100");

		});

	}



	private void StudentShell_Deactivated(object? sender, EventArgs e)

	{

		TryAutoRecoverForceWatch("Deactivated");

	}



	private string PromptForTeacherPinDialog()

	{

		string pinResult = "";

		base.Dispatcher.Invoke(delegate

		{

			Window pinWindow = new Window

			{

				Title = "Giáo Viên Nhập PIN Bảo Mật",

				Width = 350,

				Height = 160,

				WindowStartupLocation = WindowStartupLocation.CenterScreen,

				WindowStyle = WindowStyle.ToolWindow,

				ResizeMode = ResizeMode.NoResize,

				Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)),

				Foreground = Brushes.White

			};



			Grid grid = new Grid { Margin = new Thickness(15) };

			grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

			grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) });

			grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

			grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(15) });

			grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });



			TextBlock lblPrompt = new TextBlock

			{

				Text = "Nhập mã PIN của Giáo viên để thoát chương trình:",

				Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240)),

				FontSize = 12,

				FontWeight = FontWeights.SemiBold

			};

			Grid.SetRow(lblPrompt, 0);

			grid.Children.Add(lblPrompt);



			PasswordBox txtPin = new PasswordBox

			{

				Height = 28,

				Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)),

				Foreground = Brushes.White,

				BorderBrush = new SolidColorBrush(Color.FromRgb(71, 85, 105)),

				BorderThickness = new Thickness(1),

				Padding = new Thickness(4, 2, 4, 2),

				VerticalContentAlignment = VerticalAlignment.Center

			};

			Grid.SetRow(txtPin, 2);

			grid.Children.Add(txtPin);



			StackPanel btnPanel = new StackPanel

			{

				Orientation = Orientation.Horizontal,

				HorizontalAlignment = HorizontalAlignment.Right

			};

			Grid.SetRow(btnPanel, 4);



			Button btnOk = new Button

			{

				Content = "Xác nhận",

				Width = 80,

				Height = 26,

				IsDefault = true,

				Margin = new Thickness(0, 0, 10, 0),

				Background = new SolidColorBrush(Color.FromRgb(14, 165, 233)),

				Foreground = Brushes.White,

				BorderThickness = new Thickness(0)

			};

			btnOk.Click += (s, ev) =>

			{

				pinResult = txtPin.Password;

				pinWindow.DialogResult = true;

				pinWindow.Close();

			};



			Button btnCancel = new Button

			{

				Content = "Hủy",

				Width = 80,

				Height = 26,

				IsCancel = true,

				Background = new SolidColorBrush(Color.FromRgb(71, 85, 105)),

				Foreground = Brushes.White,

				BorderThickness = new Thickness(0)

			};

			btnCancel.Click += (s, ev) =>

			{

				pinWindow.DialogResult = false;

				pinWindow.Close();

			};



			DispatcherTimer? timer = null;

			Action updateLockoutState = null!;

			updateLockoutState = () =>

			{

				if (_lockoutEndTime.HasValue && DateTime.Now < _lockoutEndTime.Value)

				{

					double remainingSecs = Math.Ceiling((_lockoutEndTime.Value - DateTime.Now).TotalSeconds);

					lblPrompt.Text = $"Thử sai quá nhiều lần. Vui lòng đợi {remainingSecs} giây...";

					lblPrompt.Foreground = Brushes.Tomato;

					txtPin.IsEnabled = false;

					btnOk.IsEnabled = false;

				}

				else

				{

					lblPrompt.Text = "Nhập mã PIN của Giáo viên để thoát chương trình:";

					lblPrompt.Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240));

					txtPin.IsEnabled = true;

					btnOk.IsEnabled = true;

					if (timer != null)

					{

						timer.Stop();

						timer = null;

					}

				}

			};



			if (_lockoutEndTime.HasValue && DateTime.Now < _lockoutEndTime.Value)

			{

				timer = new DispatcherTimer

				{

					Interval = TimeSpan.FromSeconds(1)

				};

				timer.Tick += (s, ev) => updateLockoutState();

				timer.Start();

			}



			updateLockoutState();



			pinWindow.Closed += (s, ev) =>

			{

				if (timer != null)

				{

					timer.Stop();

					timer = null;

				}

			};



			btnPanel.Children.Add(btnOk);

			btnPanel.Children.Add(btnCancel);

			grid.Children.Add(btnPanel);



			pinWindow.Content = grid;

			txtPin.Focus();

			pinWindow.ShowDialog();

		});

		return pinResult;

	}



		private void ShowNoticePopup(string title, string body, string type, int durationSec)

		{

			try

			{

				if (_noticeOverlay != null)

				{

					Grid grid = (Grid)base.Content;

					grid.Children.Remove(_noticeOverlay);

					_noticeOverlay = null;

				}

				if (1 == 0)

				{

				}

				(Color, string, Color) tuple = type switch

				{

					"warning" => (Color.FromArgb(200, 50, 40, 10), "⚠\ufe0f", Color.FromRgb(230, 81, 0)), 

					"urgent" => (Color.FromArgb(220, 60, 10, 10), "\ud83d\udea8", Color.FromRgb(198, 40, 40)), 

					"celebrate" => (Color.FromArgb(200, 10, 40, 60), "\ud83c\udf89", Color.FromRgb(46, 125, 50)), 

					_ => (Color.FromArgb(200, 20, 30, 60), "ℹ\ufe0f", Color.FromRgb(21, 101, 192)), 

				};

				if (1 == 0)

				{

				}

				(Color, string, Color) tuple2 = tuple;

				Color item = tuple2.Item1;

				string item2 = tuple2.Item2;

				Color item3 = tuple2.Item3;

				_noticeOverlay = new Grid

				{

					Background = new SolidColorBrush(item)

				};

				Border border = new Border

				{

					Background = Brushes.White,

					CornerRadius = new CornerRadius(16.0),

					MaxWidth = 500.0,

					Padding = new Thickness(0.0),

					HorizontalAlignment = HorizontalAlignment.Center,

					VerticalAlignment = VerticalAlignment.Center,

					Effect = new DropShadowEffect

					{

						BlurRadius = 30.0,

						ShadowDepth = 6.0,

						Opacity = 0.4

					}

				};

				StackPanel stackPanel = new StackPanel();

				Border border2 = new Border

				{

					Background = new SolidColorBrush(item3),

					CornerRadius = new CornerRadius(16.0, 16.0, 0.0, 0.0),

					Padding = new Thickness(24.0, 14.0, 24.0, 14.0)

				};

				border2.Child = new TextBlock

				{

					Text = item2 + " Thông báo từ Giáo viên",

					FontSize = 17.0,

					FontWeight = FontWeights.Bold,

					Foreground = Brushes.White

				};

				stackPanel.Children.Add(border2);

				if (!string.IsNullOrWhiteSpace(title))

				{

					stackPanel.Children.Add(new TextBlock

					{

						Text = title,

						FontSize = 16.0,

						FontWeight = FontWeights.Bold,

						Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),

						Margin = new Thickness(24.0, 16.0, 24.0, 0.0),

						TextWrapping = TextWrapping.Wrap

					});

				}

				if (!string.IsNullOrWhiteSpace(body))

				{

					stackPanel.Children.Add(new TextBlock

					{

						Text = body,

						FontSize = 14.0,

						Foreground = new SolidColorBrush(Color.FromRgb(97, 97, 97)),

						Margin = new Thickness(24.0, 8.0, 24.0, 0.0),

						TextWrapping = TextWrapping.Wrap

					});

				}

				Border border3 = new Border

				{

					Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),

					CornerRadius = new CornerRadius(8.0),

					Padding = new Thickness(16.0, 10.0, 16.0, 10.0),

					Margin = new Thickness(24.0, 16.0, 24.0, 20.0),

					Cursor = Cursors.Hand,

					HorizontalAlignment = HorizontalAlignment.Center

				};

				border3.Child = new TextBlock

				{

					Text = "✅ Đã hiểu",

					FontSize = 13.0,

					FontWeight = FontWeights.SemiBold,

					Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66))

				};

				border3.MouseLeftButtonDown += delegate

				{

					Grid grid3 = (Grid)base.Content;

					grid3.Children.Remove(_noticeOverlay);

					_noticeOverlay = null;

				};

				stackPanel.Children.Add(border3);

				border.Child = stackPanel;

				_noticeOverlay.Children.Add(border);

				Grid grid2 = (Grid)base.Content;

				Grid.SetColumnSpan(_noticeOverlay, 10);

				Grid.SetRowSpan(_noticeOverlay, 10);

				grid2.Children.Add(_noticeOverlay);

				if (durationSec > 0)

				{

					DispatcherTimer timer = new DispatcherTimer

					{

						Interval = TimeSpan.FromSeconds(durationSec)

					};

					timer.Tick += delegate

					{

						timer.Stop();

						if (_noticeOverlay != null)

						{

							Grid grid3 = (Grid)base.Content;

							grid3.Children.Remove(_noticeOverlay);

							_noticeOverlay = null;

						}

					};

					timer.Start();

				}

				Log.Information("Notice popup shown: {Title}", title);

			}

			catch (Exception ex)

			{

				Log.Warning("ShowNoticePopup error: {Err}", ex.Message);

			}

		}



		private void ShowScreenLockOverlay(string title, string subtitle, string colorHex)

		{

			try

			{

				CloseScreenLockOverlay();

				Color color = ((colorHex == "#000000") ? Color.FromArgb(250, 0, 0, 0) : Color.FromArgb(230, 40, 20, 20));

				_lockOverlay = new Grid

				{

					Background = new SolidColorBrush(color)

				};

				StackPanel stackPanel = new StackPanel

				{

					VerticalAlignment = VerticalAlignment.Center,

					HorizontalAlignment = HorizontalAlignment.Center

				};

				stackPanel.Children.Add(new TextBlock

				{

					Text = title,

					FontSize = 36.0,

					FontWeight = FontWeights.Bold,

					Foreground = Brushes.White,

					HorizontalAlignment = HorizontalAlignment.Center

				});

				stackPanel.Children.Add(new TextBlock

				{

					Text = subtitle,

					FontSize = 16.0,

					Foreground = new SolidColorBrush(Color.FromArgb(180, byte.MaxValue, byte.MaxValue, byte.MaxValue)),

					HorizontalAlignment = HorizontalAlignment.Center,

					Margin = new Thickness(0.0, 12.0, 0.0, 0.0)

				});

				stackPanel.Children.Add(new TextBlock

				{

					Text = $"\ud83d\udd50 {DateTime.Now:HH:mm}",

					FontSize = 48.0,

					FontWeight = FontWeights.Light,

					Foreground = new SolidColorBrush(Color.FromArgb(100, byte.MaxValue, byte.MaxValue, byte.MaxValue)),

					HorizontalAlignment = HorizontalAlignment.Center,

					Margin = new Thickness(0.0, 30.0, 0.0, 0.0)

				});

				_lockOverlay.Children.Add(stackPanel);

				Grid grid = (Grid)base.Content;

				Grid.SetColumnSpan(_lockOverlay, 10);

				Grid.SetRowSpan(_lockOverlay, 10);

				grid.Children.Add(_lockOverlay);

				KeyboardHookHelper.EnableHook();

				Log.Information("Screen lock overlay shown: {Title}", title);

			}

			catch (Exception ex)

			{

				Log.Warning("ShowScreenLockOverlay error: {Err}", ex.Message);

			}

		}



		private void CloseScreenLockOverlay()

		{

			try

			{

				KeyboardHookHelper.DisableHook();

				if (_lockOverlay != null)

				{

					Grid grid = (Grid)base.Content;

					grid.Children.Remove(_lockOverlay);

					_lockOverlay = null;

				}

			}

			catch

			{

			}

		}



		private void ShowScreenCastOverlay(string resolution, string fps)

		{

			try

			{

				CloseScreenCastOverlay();

				_screenCastOverlay = new Grid

				{

					Background = new SolidColorBrush(Color.FromArgb(240, 13, 27, 42))

				};

				StackPanel stackPanel = new StackPanel

				{

					VerticalAlignment = VerticalAlignment.Center,

					HorizontalAlignment = HorizontalAlignment.Center,

					MaxWidth = 500.0

				};

				Border border = new Border

				{

					Background = new SolidColorBrush(Color.FromRgb(244, 67, 54)),

					CornerRadius = new CornerRadius(8.0),

					Padding = new Thickness(16.0, 6.0, 16.0, 6.0),

					HorizontalAlignment = HorizontalAlignment.Center,

					Margin = new Thickness(0.0, 0.0, 0.0, 20.0)

				};

				border.Child = new TextBlock

				{

					Text = "● LIVE — GV đang chiếu màn hình",

					FontSize = 14.0,

					FontWeight = FontWeights.Bold,

					Foreground = Brushes.White

				};

				stackPanel.Children.Add(border);

				stackPanel.Children.Add(new TextBlock

				{

					Text = "\ud83d\udce1",

					FontSize = 64.0,

					HorizontalAlignment = HorizontalAlignment.Center,

					Margin = new Thickness(0.0, 0.0, 0.0, 16.0)

				});

				stackPanel.Children.Add(new TextBlock

				{

					Text = "Giáo viên đang chiếu màn hình",

					FontSize = 22.0,

					FontWeight = FontWeights.Bold,

					Foreground = new SolidColorBrush(Color.FromRgb(79, 195, 247)),

					HorizontalAlignment = HorizontalAlignment.Center

				});

				stackPanel.Children.Add(new TextBlock

				{

					Text = "Hãy theo dõi nội dung trên bảng/màn hình trình chiếu của GV",

					FontSize = 14.0,

					Foreground = new SolidColorBrush(Color.FromRgb(144, 164, 174)),

					HorizontalAlignment = HorizontalAlignment.Center,

					Margin = new Thickness(0.0, 8.0, 0.0, 20.0),

					TextWrapping = TextWrapping.Wrap,

					TextAlignment = TextAlignment.Center

				});

				Border border2 = new Border

				{

					Background = new SolidColorBrush(Color.FromArgb(40, byte.MaxValue, byte.MaxValue, byte.MaxValue)),

					CornerRadius = new CornerRadius(10.0),

					Padding = new Thickness(20.0, 12.0, 20.0, 12.0),

					HorizontalAlignment = HorizontalAlignment.Center

				};

				StackPanel stackPanel2 = new StackPanel

				{

					Orientation = Orientation.Horizontal

				};

				stackPanel2.Children.Add(new TextBlock

				{

					Text = "\ud83d\udda5\ufe0f " + resolution,

					FontSize = 12.0,

					Foreground = new SolidColorBrush(Color.FromRgb(144, 164, 174)),

					Margin = new Thickness(0.0, 0.0, 20.0, 0.0)

				});

				stackPanel2.Children.Add(new TextBlock

				{

					Text = "\ud83c\udfac " + fps,

					FontSize = 12.0,

					Foreground = new SolidColorBrush(Color.FromRgb(144, 164, 174)),

					Margin = new Thickness(0.0, 0.0, 20.0, 0.0)

				});

				stackPanel2.Children.Add(new TextBlock

				{

					Text = $"\ud83d\udd50 {DateTime.Now:HH:mm}",

					FontSize = 12.0,

					Foreground = new SolidColorBrush(Color.FromRgb(144, 164, 174))

				});

				border2.Child = stackPanel2;

				stackPanel.Children.Add(border2);

				_screenCastOverlay.Children.Add(stackPanel);

				Grid grid = (Grid)base.Content;

				Grid.SetColumnSpan(_screenCastOverlay, 10);

				Grid.SetRowSpan(_screenCastOverlay, 10);

				grid.Children.Add(_screenCastOverlay);

				Log.Information("Broadcast overlay shown: {Res} {Fps}", resolution, fps);

			}

			catch (Exception ex)

			{

				Log.Warning("ShowBroadcastOverlay error: {Err}", ex.Message);

			}

		}



		private void CloseScreenCastOverlay()

		{

			try

			{

				if (_screenCastOverlay != null)

				{

					Grid grid = (Grid)base.Content;

					grid.Children.Remove(_screenCastOverlay);

					_screenCastOverlay = null;

				}

			}

			catch

			{

			}

		}



		private UIElement CreateRunningWidget(out TextBlock txtTimer)

		{

			Border border = new Border

			{

				Background = new SolidColorBrush(Color.FromArgb(220, 21, 101, 192)), // Semi-transparent blue

				CornerRadius = new CornerRadius(8.0),

				Width = 180,

				Height = 50,

				HorizontalAlignment = HorizontalAlignment.Right,

				VerticalAlignment = VerticalAlignment.Top,

				Margin = new Thickness(0, 60, 20, 0),

				Padding = new Thickness(12, 0, 12, 0),

				Cursor = Cursors.SizeAll, // Show move cursor

				Effect = new DropShadowEffect

				{

					BlurRadius = 15.0,

					ShadowDepth = 2.0,

					Opacity = 0.3

				}

			};



			StackPanel panel = new StackPanel

			{

				Orientation = Orientation.Horizontal,

				HorizontalAlignment = HorizontalAlignment.Center,

				VerticalAlignment = VerticalAlignment.Center

			};



			TextBlock icon = new TextBlock

			{

				Text = "⏱️",

				FontSize = 20,

				VerticalAlignment = VerticalAlignment.Center,

				Margin = new Thickness(0, 0, 8, 0)

			};

			panel.Children.Add(icon);



			txtTimer = new TextBlock

			{

				Text = "00:00",

				FontSize = 22,

				FontWeight = FontWeights.Bold,

				FontFamily = new FontFamily("Consolas"),

				Foreground = Brushes.White,

				VerticalAlignment = VerticalAlignment.Center

			};

			panel.Children.Add(txtTimer);



			border.Child = panel;



			// Drag and drop implementation

			border.MouseLeftButtonDown += (s, e) =>

			{

				if (_classTimerOverlay != null)

				{

					border.CaptureMouse();

					var mousePos = e.GetPosition(_classTimerOverlay);

					border.Tag = new Point(mousePos.X + border.Margin.Right, mousePos.Y - border.Margin.Top);

				}

			};



			border.MouseMove += (s, e) =>

			{

				if (border.IsMouseCaptured && _classTimerOverlay != null && border.Tag is Point offset)

				{

					var mousePos = e.GetPosition(_classTimerOverlay);

					double newRight = offset.X - mousePos.X;

					double newTop = mousePos.Y - offset.Y;



					// Bounds check

					double maxRight = _classTimerOverlay.ActualWidth - border.Width;

					double maxTop = _classTimerOverlay.ActualHeight - border.Height;



					newRight = Math.Max(0, Math.Min(newRight, maxRight));

					newTop = Math.Max(0, Math.Min(newTop, maxTop));



					border.Margin = new Thickness(0, newTop, newRight, 0);

				}

			};



			border.MouseLeftButtonUp += (s, e) =>

			{

				border.ReleaseMouseCapture();

			};



			return border;

		}



		private UIElement CreateEndedWidget()

		{

			Border border = new Border

			{

				Background = Brushes.White,

				CornerRadius = new CornerRadius(16.0),

				Width = 320,

				Height = 220,

				HorizontalAlignment = HorizontalAlignment.Center,

				VerticalAlignment = VerticalAlignment.Center,

				Effect = new DropShadowEffect

				{

					BlurRadius = 30.0,

					ShadowDepth = 6.0,

					Opacity = 0.4

				}

			};



			StackPanel stack = new StackPanel();



			Border headerBorder = new Border

			{

				Background = new SolidColorBrush(Color.FromRgb(211, 47, 47)), // Red header for time up

				CornerRadius = new CornerRadius(16.0, 16.0, 0.0, 0.0),

				Padding = new Thickness(16, 12, 16, 12)

			};

			headerBorder.Child = new TextBlock

			{

				Text = "HẾT GIỜ LÀM BÀI",

				FontSize = 14,

				FontWeight = FontWeights.Bold,

				Foreground = Brushes.White,

				HorizontalAlignment = HorizontalAlignment.Center

			};

			stack.Children.Add(headerBorder);



			TextBlock txtTimer = new TextBlock

			{

				Text = "00:00",

				FontSize = 48,

				FontWeight = FontWeights.Bold,

				FontFamily = new FontFamily("Consolas"),

				Foreground = new SolidColorBrush(Color.FromRgb(211, 47, 47)),

				Margin = new Thickness(0, 24, 0, 12),

				HorizontalAlignment = HorizontalAlignment.Center

			};

			stack.Children.Add(txtTimer);



			TextBlock txtMsg = new TextBlock

			{

				Text = "Vui lòng dừng mọi hoạt động làm bài!",

				FontSize = 12,

				FontWeight = FontWeights.SemiBold,

				Foreground = Brushes.DimGray,

				HorizontalAlignment = HorizontalAlignment.Center

			};

			stack.Children.Add(txtMsg);



			border.Child = stack;

			return border;

		}



		private void ShowClassTimer(int seconds)

		{

			try

			{

				CloseClassTimer();

				Grid grid = (Grid)base.Content;

				_classTimerOverlay = new Grid

				{

					Background = null, // Set null to make background pass mouse events through

					IsHitTestVisible = true, // Must be true so widget children can get events

					Tag = "ClassTimerOverlay"

				};

				

				UIElement runningWidget = CreateRunningWidget(out TextBlock txtTimer);

				_classTimerOverlay.Children.Add(runningWidget);

				

				Grid.SetColumnSpan(_classTimerOverlay, 10);

				Grid.SetRowSpan(_classTimerOverlay, 10);

				grid.Children.Add(_classTimerOverlay);

				

				int remaining = seconds;

				txtTimer.Text = $"{remaining / 60:D2}:{remaining % 60:D2}";

				

				_classTimer = new DispatcherTimer

				{

					Interval = TimeSpan.FromSeconds(1.0)

				};

				_classTimer.Tick += delegate

				{

					remaining--;

					if (remaining < 0)

					{

						remaining = 0;

					}

					

					// Blinking colon logic based on remaining seconds

					string separator = (remaining % 2 == 0) ? ":" : " ";

					txtTimer.Text = $"{remaining / 60:D2}{separator}{remaining % 60:D2}";

					

					if (remaining <= 10 && remaining > 0)

					{

						txtTimer.Foreground = new SolidColorBrush(Color.FromRgb(255, 112, 67)); // Orange-red warning

					}

					

					if (remaining <= 0)

					{

						_classTimer?.Stop();

						

						// Change to screen lock and show ended widget

						_classTimerOverlay.Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0));

						_classTimerOverlay.IsHitTestVisible = true; // Lock interaction

						_classTimerOverlay.Children.Clear();

						_classTimerOverlay.Children.Add(CreateEndedWidget());

					}

				};

				_classTimer.Start();

				Log.Information("Class countdown timer started: {Sec} seconds", seconds);

			}

			catch (Exception ex)

			{

				Log.Warning("ShowClassTimer error: {Err}", ex.Message);

			}

		}



		private void CloseClassTimer()

		{

			try

			{

				if (_classTimer != null)

				{

					_classTimer.Stop();

					_classTimer = null;

				}

				if (_classTimerOverlay != null)

				{

					Grid grid = (Grid)base.Content;

					grid.Children.Remove(_classTimerOverlay);

					_classTimerOverlay = null;

					Log.Information("Class countdown timer closed");

				}

			}

			catch (Exception ex)

			{

				Log.Warning("CloseClassTimer error: {Err}", ex.Message);

			}

		}



		private void ShowSilenceOverlay()

		{

			try

			{

				if (_silenceOverlay != null)

				{

					try

					{

						Grid grid = (Grid)base.Content;

						grid.Children.Remove(_silenceOverlay);

					}

					catch

					{

					}

					_silenceOverlay = null;

				}

				CloseScreenLockOverlay();

				CloseTeacherWarning();

				_silenceOverlay = new Grid

				{

					Background = new SolidColorBrush(Color.FromArgb(byte.MaxValue, 0, 0, 0))

				};

				StackPanel stackPanel = new StackPanel

				{

					VerticalAlignment = VerticalAlignment.Center,

					HorizontalAlignment = HorizontalAlignment.Center

				};

				stackPanel.Children.Add(new TextBlock

				{

					Text = "\ud83d\udd07",

					FontSize = 80.0,

					FontFamily = new FontFamily("Segoe UI Emoji"),

					HorizontalAlignment = HorizontalAlignment.Center,

					Margin = new Thickness(0.0, 0.0, 0.0, 20.0)

				});

				stackPanel.Children.Add(new TextBlock

				{

					Text = "IM LẶNG",

					FontSize = 72.0,

					FontWeight = FontWeights.Black,

					Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54)),

					HorizontalAlignment = HorizontalAlignment.Center,

					FontFamily = new FontFamily("Segoe UI Black")

				});

				stackPanel.Children.Add(new TextBlock

				{

					Text = "Giáo viên yêu cầu trật tự!",

					FontSize = 24.0,

					FontWeight = FontWeights.SemiBold,

					Foreground = new SolidColorBrush(Color.FromArgb(200, byte.MaxValue, byte.MaxValue, byte.MaxValue)),

					HorizontalAlignment = HorizontalAlignment.Center,

					Margin = new Thickness(0.0, 16.0, 0.0, 0.0)

				});

				stackPanel.Children.Add(new TextBlock

				{

					Text = "⚠\ufe0f Hãy ngồi yên, không nói chuyện",

					FontSize = 18.0,

					Foreground = new SolidColorBrush(Color.FromRgb(byte.MaxValue, 183, 77)),

					HorizontalAlignment = HorizontalAlignment.Center,

					Margin = new Thickness(0.0, 12.0, 0.0, 0.0)

				});

				stackPanel.Children.Add(new TextBlock

				{

					Text = $"\ud83d\udd50 {DateTime.Now:HH:mm}",

					FontSize = 36.0,

					FontWeight = FontWeights.Light,

					Foreground = new SolidColorBrush(Color.FromArgb(80, byte.MaxValue, byte.MaxValue, byte.MaxValue)),

					HorizontalAlignment = HorizontalAlignment.Center,

					Margin = new Thickness(0.0, 40.0, 0.0, 0.0)

				});

				_silenceOverlay.Children.Add(stackPanel);

				Grid grid2 = (Grid)base.Content;

				Grid.SetColumnSpan(_silenceOverlay, 10);

				Grid.SetRowSpan(_silenceOverlay, 10);

				grid2.Children.Add(_silenceOverlay);

				ShowNotification("\ud83d\udd07 IM LẶNG", "GV yêu cầu trật tự lớp học!", "#C62828");

				Log.Information("Silence overlay shown");

			}

			catch (Exception ex)

			{

				Log.Warning("ShowSilenceOverlay error: {Err}", ex.Message);

			}

		}



		private void ShowTeacherWarning(string message, int durationSec)

		{

			try

			{

				CloseTeacherWarning();

				_warningOverlay = new Grid

				{

					Background = new SolidColorBrush(Color.FromArgb(220, 30, 30, 30))

				};

				StackPanel stackPanel = new StackPanel

				{

					VerticalAlignment = VerticalAlignment.Center,

					HorizontalAlignment = HorizontalAlignment.Center,

					MaxWidth = 600.0

				};

				stackPanel.Children.Add(new TextBlock

				{

					Text = "⚠\ufe0f",

					FontSize = 64.0,

					FontFamily = new FontFamily("Segoe UI Emoji"),

					HorizontalAlignment = HorizontalAlignment.Center,

					Margin = new Thickness(0.0, 0.0, 0.0, 16.0)

				});

				stackPanel.Children.Add(new TextBlock

				{

					Text = "CẢNH BÁO TỪ GIÁO VIÊN",

					FontSize = 28.0,

					FontWeight = FontWeights.Bold,

					Foreground = new SolidColorBrush(Color.FromRgb(byte.MaxValue, 183, 77)),

					HorizontalAlignment = HorizontalAlignment.Center

				});

				stackPanel.Children.Add(new TextBlock

				{

					Text = message,

					FontSize = 22.0,

					FontWeight = FontWeights.SemiBold,

					Foreground = Brushes.White,

					TextWrapping = TextWrapping.Wrap,

					TextAlignment = TextAlignment.Center,

					HorizontalAlignment = HorizontalAlignment.Center,

					Margin = new Thickness(20.0, 16.0, 20.0, 0.0)

				});

				TextBlock countdownText = new TextBlock

				{

					FontSize = 16.0,

					Foreground = new SolidColorBrush(Color.FromArgb(150, byte.MaxValue, byte.MaxValue, byte.MaxValue)),

					HorizontalAlignment = HorizontalAlignment.Center,

					Margin = new Thickness(0.0, 20.0, 0.0, 0.0)

				};

				stackPanel.Children.Add(countdownText);

				_warningOverlay.Children.Add(stackPanel);

				Grid grid = (Grid)base.Content;

				Grid.SetColumnSpan(_warningOverlay, 10);

				Grid.SetRowSpan(_warningOverlay, 10);

				grid.Children.Add(_warningOverlay);

				ShowNotification("⚠\ufe0f Cảnh báo GV", (message.Length > 40) ? (message.Substring(0, 40) + "...") : message, "#E65100");

				if (durationSec > 0)

				{

					int remaining = durationSec;

					countdownText.Text = $"Tự đóng sau {remaining} giây...";

					_warningTimer?.Stop();

					_warningTimer = new DispatcherTimer

					{

						Interval = TimeSpan.FromSeconds(1.0)

					};

					_warningTimer.Tick += delegate

					{

						remaining--;

						if (remaining <= 0)

						{

							_warningTimer?.Stop();

							CloseTeacherWarning();

						}

						else

						{

							countdownText.Text = $"Tự đóng sau {remaining} giây...";

						}

					};

					_warningTimer.Start();

				}

				else

				{

					countdownText.Text = "GV sẽ tắt khi sẵn sàng";

				}

				Log.Information("Teacher warning shown: {Msg}, duration: {Sec}s", message, durationSec);

			}

			catch (Exception ex)

			{

				Log.Warning("ShowTeacherWarning error: {Err}", ex.Message);

			}

		}



		private void CloseTeacherWarning()

		{

			try

			{

				_warningTimer?.Stop();

				if (_warningOverlay != null)

				{

					Grid grid = (Grid)base.Content;

					grid.Children.Remove(_warningOverlay);

					_warningOverlay = null;

				}

			}

			catch

			{

			}

		}



		private void KillNonEducationalApps()

		{

			try

			{

				HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase)

				{

					"QASmartClass", "explorer", "svchost", "services", "lsass", "csrss", "dwm", "System", "Idle", "winlogon",

					"smss", "wininit", "conhost", "dllhost", "WINWORD", "EXCEL", "POWERPNT", "ONENOTE", "OUTLOOK", "notepad",

					"mspaint", "calc", "Calculator", "SnippingTool", "TextInputHost", "msteams", "SearchHost", "StartMenuExperienceHost", "ShellExperienceHost", "RuntimeBroker",

					"ApplicationFrameHost", "SystemSettings", "SecurityHealthSystray", "taskhostw", "SettingSyncHost", "ctfmon", "fontdrvhost", "sihost", "SearchIndexer", "Taskmgr",

					"spoolsv", "WmiPrvSE", "audiodg"

				};



				// Read Master Config

				bool killBrowsers = true;

				try

				{

					using var db = new QASmartClass.Data.AppDbContext();

					var killBrowsersSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Security_KillBrowsers");

					killBrowsers = (killBrowsersSetting?.Value ?? "Enabled") == "Enabled";

				}

				catch (Exception exDb)

				{

					Log.Debug("Failed to read Security_KillBrowsers setting: {Err}", exDb.Message);

				}



				List<string> blacklist = new List<string>

				{

					"steam", "game", "minecraft", "roblox", "fortnite", "league", "valorant", "epicgames", "discord", "spotify",

					"vlc", "obs", "twitch", "tiktok", "zalo", "telegram", "messenger"

				};



				if (killBrowsers)

				{

					blacklist.AddRange(new[] { "chrome", "msedge", "firefox", "opera", "brave", "whale", "cococ" });

				}



				int killedCount = 0;

				List<string> killedNames = new List<string>();

				Process[] processes = Process.GetProcesses();

				foreach (Process process in processes)

				{

					try

					{

						string name = process.ProcessName;

						if (!hashSet.Contains(name) && process.Id != Process.GetCurrentProcess().Id && blacklist.Any((string kw) => name.Contains(kw, StringComparison.OrdinalIgnoreCase)))

						{

							killedNames.Add(name);

							if (!process.CloseMainWindow())

							{

								try

								{

									process.Kill(true);

								}

								catch

								{

									process.Kill();

								}

							}

							killedCount++;

							Log.Information("Killed non-educational app: {Name}", name);

						}

					}

					catch

					{

					}

				}

				base.Dispatcher.Invoke(delegate

				{

					if (killedCount > 0)

					{

						ShowTeacherWarning($"\ud83d\udeab GV đã tắt {killedCount} ứng dụng không phải học tập:\n" + string.Join(", ", killedNames.Take(5)) + "\n\nHãy tập trung vào bài học!", 10);

					}

					else

					{

						ShowNotification("✅ Kiểm tra hoàn tất", "Không tìm thấy ứng dụng ngoài học tập", "#2E7D32");

					}

				});

				Log.Information("KillNonEducationalApps: killed {Count} apps", killedCount);

			}

			catch (Exception ex)

			{

				Log.Warning("KillNonEducationalApps error: {Err}", ex.Message);

			}

		}



		private void ShowToolFocusOverlay(string toolId)

		{

			try

			{

				CloseToolFocusOverlay();

				string toolDisplayName = GetToolDisplayName(toolId);

				UserControl userControl = LearningToolsHub.CreateToolControl(toolId);

				if (userControl == null)

				{

					ShowNotification("\ud83c\udfaf Tập trung", "GV yêu cầu xem: " + toolDisplayName, "#2E7D32");

					return;

				}

				_toolFocusOverlay = new Grid

				{

					Background = new SolidColorBrush(Color.FromArgb(220, 15, 15, 35)),

					Tag = "ToolFocusOverlay"

				};

				Border border = new Border

				{

					Background = Brushes.White,

					CornerRadius = new CornerRadius(16.0),

					MaxWidth = double.PositiveInfinity,

					MaxHeight = double.PositiveInfinity,

					Margin = new Thickness(10.0),

					HorizontalAlignment = HorizontalAlignment.Stretch,

					VerticalAlignment = VerticalAlignment.Stretch,

					Effect = new DropShadowEffect

					{

						BlurRadius = 40.0,

						ShadowDepth = 8.0,

						Opacity = 0.5,

						Color = Color.FromRgb(25, 118, 210)

					}

				};

				Grid grid = new Grid();

				grid.RowDefinitions.Add(new RowDefinition

				{

					Height = GridLength.Auto

				});

				grid.RowDefinitions.Add(new RowDefinition

				{

					Height = new GridLength(1.0, GridUnitType.Star)

				});

				Border border2 = new Border

				{

					CornerRadius = new CornerRadius(16.0, 16.0, 0.0, 0.0),

					Padding = new Thickness(24.0, 14.0, 24.0, 14.0),

					Background = new LinearGradientBrush(Color.FromRgb(25, 118, 210), Color.FromRgb(13, 71, 161), 90.0)

				};

				DockPanel dockPanel = new DockPanel();

				bool isMaximized = true;

				Border maximizeBtn = new Border

				{

					Background = new SolidColorBrush(Color.FromArgb(80, byte.MaxValue, byte.MaxValue, byte.MaxValue)),

					CornerRadius = new CornerRadius(8.0),

					Padding = new Thickness(14.0, 6.0, 14.0, 6.0),

					Cursor = Cursors.Hand,

					VerticalAlignment = VerticalAlignment.Center,

					Margin = new Thickness(0.0, 0.0, 10.0, 0.0)

				};

				TextBlock maxText = new TextBlock

				{

					Text = "Thu nhỏ",

					FontSize = 14.0,

					Foreground = Brushes.White,

					FontWeight = FontWeights.SemiBold

				};

				maximizeBtn.Child = maxText;

				maximizeBtn.MouseLeftButtonDown += delegate

				{

					if (!isMaximized)

					{

						border.MaxWidth = double.PositiveInfinity;

						border.MaxHeight = double.PositiveInfinity;

						border.Margin = new Thickness(10.0);

						maxText.Text = "Thu nhỏ";

						isMaximized = true;

					}

					else

					{

						border.MaxWidth = 1100.0;

						border.MaxHeight = 700.0;

						border.Margin = new Thickness(40.0, 30.0, 40.0, 30.0);

						maxText.Text = "⛶ Phóng to";

						isMaximized = false;

					}

				};

				DockPanel.SetDock(maximizeBtn, Dock.Right);

				dockPanel.Children.Add(maximizeBtn);



				StackPanel stackPanel = new StackPanel();

				stackPanel.Children.Add(new TextBlock

				{

					Text = "\ud83c\udfaf " + toolDisplayName,

					FontSize = 20.0,

					FontWeight = FontWeights.Bold,

					Foreground = Brushes.White

				});

				stackPanel.Children.Add(new TextBlock

				{

					Text = "GV đang yêu cầu tập trung — Hãy quan sát!",

					FontSize = 13.0,

					Foreground = new SolidColorBrush(Color.FromRgb(187, 222, 251)),

					Margin = new Thickness(0.0, 2.0, 0.0, 0.0)

				});

				dockPanel.Children.Add(stackPanel);

				border2.Child = dockPanel;

				Grid.SetRow(border2, 0);

				grid.Children.Add(border2);

				Border border4 = new Border

				{

					CornerRadius = new CornerRadius(0.0, 0.0, 16.0, 16.0),

					Background = new SolidColorBrush(Color.FromRgb(250, 250, 252)),

					Padding = new Thickness(8.0)

				};

				border4.Child = userControl;

				Grid.SetRow(border4, 1);

				grid.Children.Add(border4);

				border.Child = grid;

				_toolFocusOverlay.Children.Add(border);

				Grid grid2 = (Grid)base.Content;

				Grid.SetColumnSpan(_toolFocusOverlay, 10);

				Grid.SetRowSpan(_toolFocusOverlay, 10);

				grid2.Children.Add(_toolFocusOverlay);

				ShowNotification("\ud83c\udfaf Tập trung", "GV yêu cầu xem: " + toolDisplayName, "#2E7D32");

				Log.Information("Tool focus overlay shown: {ToolId} ({Name})", toolId, toolDisplayName);

			}

			catch (Exception ex)

			{

				Log.Warning("ShowToolFocusOverlay error: {Err}", ex.Message);

				ShowNotification("\ud83c\udfaf Tập trung", "GV yêu cầu xem: " + GetToolDisplayName(toolId), "#2E7D32");

			}

		}



	private void CloseToolFocusOverlay()

	{

		try

		{

			if (_toolFocusOverlay != null)

			{

				Grid grid = (Grid)base.Content;

				grid.Children.Remove(_toolFocusOverlay);

				_toolFocusOverlay = null;

			}



			// Đóng toàn bộ cửa sổ phụ đang mở (đồ thị, lịch sử, trình duyệt bảo mật,...)

			var windowsToClose = new System.Collections.Generic.List<Window>();

			if (Application.Current != null)

			{

				foreach (Window win in Application.Current.Windows)

				{

					if (win != this && win.GetType().Name != "StudentShell" && win.GetType().Name != "StudentLoginWindow")

					{

						windowsToClose.Add(win);

					}

				}

			}

			foreach (var win in windowsToClose)

			{

				try

				{

					win.Close();

				}

				catch (Exception ex)

				{

					Log.Warning("Failed to close child window {WindowType}: {Err}", win.GetType().Name, ex.Message);

				}

			}

		}

		catch (Exception ex)

		{

			Log.Warning("CloseToolFocusOverlay error: {Err}", ex.Message);

		}

	}



		private void ShowFloatingStepCard(string title, string detail)

		{

			if (_toolFocusOverlay == null)

			{

				return;

			}

			FrameworkElement frameworkElement = _toolFocusOverlay.Children.OfType<FrameworkElement>().FirstOrDefault((FrameworkElement x) => x.Name == "FloatingStepCard" || x.Tag?.ToString() == "FloatingStepCard");

			if (frameworkElement != null)

			{

				_toolFocusOverlay.Children.Remove(frameworkElement);

			}

			if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(detail))

			{

				return;

			}

			Border card = new Border

			{

				Name = "FloatingStepCard",

				Tag = "FloatingStepCard",

				Background = Brushes.White,

				BorderBrush = new SolidColorBrush(Color.FromRgb(byte.MaxValue, 160, 0)),

				BorderThickness = new Thickness(3.0),

				CornerRadius = new CornerRadius(16.0),

				Padding = new Thickness(24.0),

				MaxWidth = 750.0,

				MinHeight = 220.0,

				MaxHeight = 450.0,

				HorizontalAlignment = HorizontalAlignment.Center,

				VerticalAlignment = VerticalAlignment.Center,

				Effect = new DropShadowEffect

				{

					BlurRadius = 35.0,

					ShadowDepth = 6.0,

					Opacity = 0.45,

					Color = Color.FromRgb(byte.MaxValue, 160, 0)

				}

			};

			Grid grid = new Grid();

			grid.RowDefinitions.Add(new RowDefinition

			{

				Height = GridLength.Auto

			});

			grid.RowDefinitions.Add(new RowDefinition

			{

				Height = new GridLength(1.0, GridUnitType.Star)

			});

			DockPanel dockPanel = new DockPanel

			{

				LastChildFill = true,

				Margin = new Thickness(0.0, 0.0, 0.0, 16.0)

			};

			Border border = new Border

			{

				Background = new SolidColorBrush(Color.FromRgb(byte.MaxValue, 235, 238)),

				CornerRadius = new CornerRadius(6.0),

				Padding = new Thickness(10.0, 5.0, 10.0, 5.0),

				Cursor = Cursors.Hand,

				VerticalAlignment = VerticalAlignment.Center,

				Margin = new Thickness(12.0, 0.0, 0.0, 0.0)

			};

			border.Child = new TextBlock

			{

				Text = "✕",

				FontSize = 14.0,

				Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40)),

				FontWeight = FontWeights.Bold

			};

			border.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)

			{

				_toolFocusOverlay.Children.Remove(card);

				if (_toolFocusOverlay.Children.Count > 0 && _toolFocusOverlay.Children[0] is FrameworkElement frameworkElement2)

				{

					frameworkElement2.Visibility = Visibility.Visible;

				}

				e.Handled = true;

			};

			DockPanel.SetDock(border, Dock.Right);

			dockPanel.Children.Add(border);

			TextBlock element = new TextBlock

			{

				Text = "\ud83c\udfaf ",

				FontSize = 22.0,

				Foreground = new SolidColorBrush(Color.FromRgb(byte.MaxValue, 160, 0)),

				VerticalAlignment = VerticalAlignment.Center

			};

			DockPanel.SetDock(element, Dock.Left);

			dockPanel.Children.Add(element);

			TextBlock element2 = new TextBlock

			{

				Text = title,

				FontSize = 18.0,

				FontWeight = FontWeights.Bold,

				Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)),

				VerticalAlignment = VerticalAlignment.Center,

				TextWrapping = TextWrapping.Wrap

			};

			dockPanel.Children.Add(element2);

			Grid.SetRow(dockPanel, 0);

			grid.Children.Add(dockPanel);

			ScrollViewer scrollViewer = new ScrollViewer

			{

				VerticalScrollBarVisibility = ScrollBarVisibility.Auto,

				HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled

			};

			TextBlock content = new TextBlock

			{

				Text = detail,

				FontSize = 15.0,

				LineHeight = 24.0,

				Foreground = new SolidColorBrush(Color.FromRgb(55, 71, 79)),

				FontFamily = new FontFamily("Segoe UI"),

				TextWrapping = TextWrapping.Wrap

			};

			scrollViewer.Content = content;

			Grid.SetRow(scrollViewer, 1);

			grid.Children.Add(scrollViewer);

			card.Child = grid;

			_toolFocusOverlay.Children.Add(card);

		}



		private void ShowToolSectionFocusOverlay(string toolId, string sectionId, string title = "", string detail = "")

		{

			try

			{

				if (_toolFocusOverlay == null || _lastSectionFocusToolId != toolId)

				{

					ShowToolFocusOverlay(toolId);

					_lastSectionFocusToolId = toolId;

				}

				if (_toolFocusOverlay != null && _toolFocusOverlay.Children.Count > 0 && _toolFocusOverlay.Children[0] is FrameworkElement frameworkElement)

				{

					if (toolId == "grammar")

					{

						frameworkElement.Visibility = Visibility.Visible;

					}

					else

					{

						frameworkElement.Visibility = Visibility.Collapsed;

					}

				}

				DispatcherTimer timer = new DispatcherTimer

				{

					Interval = TimeSpan.FromMilliseconds(300.0)

				};

				timer.Tick += delegate

				{

					timer.Stop();

					_lastSectionFocusTool = FindToolControlInOverlay();

					if (_lastSectionFocusTool != null)

					{

						HighlightSectionInTool(_lastSectionFocusTool, sectionId);

						if (_lastSectionFocusTool is GrammarTool grammarTool)

						{

							grammarTool.FilterFocusedSection(sectionId);

						}

					}

					if (toolId != "grammar" && (!string.IsNullOrEmpty(title) || !string.IsNullOrEmpty(detail)))

					{

						ShowFloatingStepCard(title, detail);

					}

				};

				timer.Start();

				string toolDisplayName = GetToolDisplayName(toolId);

				ShowNotification("\ud83c\udfaf Focus Section", "GV tập trung vào: " + sectionId.Replace('_', ' '), "#2E7D32");

			}

			catch (Exception ex)

			{

				Log.Warning("ShowToolSectionFocusOverlay error: {Err}", ex.Message);

			}

		}



		private UserControl? FindToolControlInOverlay()

		{

			if (_toolFocusOverlay == null)

			{

				return null;

			}

			// Duyệt cây logical trước tiên để tìm UserControl ngay lập tức (tránh bất đồng bộ của visual tree)

			foreach (var child in _toolFocusOverlay.Children)

			{

				if (child is Border border && border.Child is Grid innerGrid)

				{

					foreach (var innerChild in innerGrid.Children)

					{

						if (innerChild is Border border4 && border4.Child is UserControl uc)

						{

							return uc;

						}

					}

				}

			}

			return FindVisualChild<UserControl>(_toolFocusOverlay);

		}



		private QASmartClass.LearningTools.Views.Multi.FocusTimerTool? FindActiveFocusTimerTool()

		{

			if (_toolFocusOverlay == null) return null;

			return FindVisualChild<QASmartClass.LearningTools.Views.Multi.FocusTimerTool>(_toolFocusOverlay);

		}



		private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject

		{

			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)

			{

				DependencyObject child = VisualTreeHelper.GetChild(parent, i);

				if (child is T result)

				{

					return result;

				}

				T val = FindVisualChild<T>(child);

				if (val != null)

				{

					return val;

				}

			}

			return null;

		}



		private void HighlightSectionInTool(UserControl tool, string sectionId)

		{

			try

			{

				ClearToolSectionHighlight();

				List<Grid> list = FindAllVisualChildren<Grid>(tool);

				foreach (Grid item in list)

				{

					foreach (object child in LogicalTreeHelper.GetChildren(item))

					{

						if (child is Border border && (CheckSectionMatch(item, sectionId) || item.Tag?.ToString() == sectionId))

						{

							_lastHighlightedBorder = border;

							_lastHighlightedOrigBrush = border.BorderBrush;

							_lastHighlightedOrigThickness = border.BorderThickness;

							border.BorderBrush = new SolidColorBrush(Color.FromRgb(byte.MaxValue, 160, 0));

							border.BorderThickness = new Thickness(3.0);

							border.BringIntoView();

							return;

						}

					}

				}

				List<Border> list2 = FindAllVisualChildren<Border>(tool);

				string value = sectionId.Replace('_', ' ');

				foreach (Border item2 in list2)

				{

					string text = item2.ToolTip?.ToString() ?? "";

					if (text.Contains(value, StringComparison.OrdinalIgnoreCase))

					{

						_lastHighlightedBorder = item2;

						_lastHighlightedOrigBrush = item2.BorderBrush;

						_lastHighlightedOrigThickness = item2.BorderThickness;

						item2.BorderBrush = new SolidColorBrush(Color.FromRgb(byte.MaxValue, 160, 0));

						item2.BorderThickness = new Thickness(3.0);

						item2.BringIntoView();

						break;

					}

				}

			}

			catch (Exception ex)

			{

				Log.Warning("HighlightSectionInTool error: {Err}", ex.Message);

			}

		}



		private static bool CheckSectionMatch(Grid wrapper, string sectionId)

		{

			foreach (object child in LogicalTreeHelper.GetChildren(wrapper))

			{

				if (!(child is StackPanel { Tag: var tag } stackPanel) || !(tag?.ToString() == "SectionToolbar"))

				{

					continue;

				}

				foreach (Button item in stackPanel.Children.OfType<Button>())

				{

					string text = item.ToolTip?.ToString() ?? "";

					if (text.Contains(sectionId.Replace('_', ' '), StringComparison.OrdinalIgnoreCase))

					{

						return true;

					}

				}

			}

			return false;

		}



		private void ClearToolSectionHighlight()

		{

			try

			{

				if (_lastHighlightedBorder != null)

				{

					_lastHighlightedBorder.BorderBrush = _lastHighlightedOrigBrush;

					_lastHighlightedBorder.BorderThickness = _lastHighlightedOrigThickness;

					_lastHighlightedBorder = null;

				}

				if (_lastSectionFocusTool is GrammarTool grammarTool)

				{

					grammarTool.ResetFocus();

				}

				_lastSectionFocusTool = null;

				if (_toolFocusOverlay != null)

				{

					if (_toolFocusOverlay.Children.Count > 0 && _toolFocusOverlay.Children[0] is FrameworkElement frameworkElement)

					{

						frameworkElement.Visibility = Visibility.Visible;

					}

					FrameworkElement frameworkElement2 = _toolFocusOverlay.Children.OfType<FrameworkElement>().FirstOrDefault((FrameworkElement x) => x.Name == "FloatingStepCard" || x.Tag?.ToString() == "FloatingStepCard");

					if (frameworkElement2 != null)

					{

						_toolFocusOverlay.Children.Remove(frameworkElement2);

					}

				}

			}

			catch

			{

			}

		}



		private static List<T> FindAllVisualChildren<T>(DependencyObject parent) where T : DependencyObject

		{

			List<T> list = new List<T>();

			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)

			{

				DependencyObject child = VisualTreeHelper.GetChild(parent, i);

				if (child is T item)

				{

					list.Add(item);

				}

				list.AddRange(FindAllVisualChildren<T>(child));

			}

			return list;

		}



		private void HandleTeacherMessage(string msg)

		{

			try

			{

				// V4.1-FIX LOI_VID_30: Lớp phòng thủ thứ 2 — chặn tin nhắn hệ thống tại UI

				var guard = QASmartClass.Utilities.MessageGuard.Evaluate(msg);

				if (!guard.IsAllowedInChat)

				{

					Log.Debug("HandleTeacherMessage: Blocked system message [{Category}] from UI display", guard.Category);

					return;

				}



				string text;

				string accentColorHex;

				if (msg.StartsWith("\ud83d\udce2"))

				{

					text = "\ud83d\udce2 Thông báo từ Giáo viên";

					accentColorHex = "#1565C0";

				}

				else if (msg.StartsWith("\ud83d\udce8"))

				{

					text = "\ud83d\udce8 Tin nhắn nhóm từ GV";

					accentColorHex = "#7B1FA2";

				}

				else if (msg.StartsWith("\ud83d\udd12"))

				{

					text = "\ud83d\udd12 Tin nhắn riêng từ GV";

					accentColorHex = "#E65100";

				}

				else if (msg.StartsWith("\ud83d\udcda"))

				{

					text = "\ud83d\udcda Tin nhắn nhóm học tập";

					accentColorHex = "#2E7D32";

				}

				else

				{

					text = "\ud83d\udcac Tin nhắn từ Giáo viên";

					accentColorHex = "#1976D2";

				}



				string cleanText = msg;

				string channel = "ALL";

				bool isHistory = msg.StartsWith("MSG_HISTORY|");



				if (msg.StartsWith("\ud83d\udce8 ["))

				{

					try

					{

						var appDb = (QASmartTouch.App)Application.Current;

						var log = appDb.Database?.EventLogs

							.Where(e => e.EventType == "CHAT_GROUPS")

							.OrderByDescending(e => e.Timestamp)

							.FirstOrDefault();

						if (log != null && !string.IsNullOrEmpty(log.Details))

						{

							var groups = System.Text.Json.JsonSerializer.Deserialize<List<StudentChatPage.ChatGroup>>(log.Details);

							if (groups != null)

							{

								foreach (var grp in groups)

								{

									if (msg.Contains($"[{grp.Name}]"))

									{

										channel = $"GROUP_{grp.Id}";

										break;

									}

								}

							}

						}

					}

					catch { }

					if (channel == "ALL")

					{

						var start = msg.IndexOf("[");

						var end = msg.IndexOf("]");

						if (start >= 0 && end > start)

						{

							string grpName = msg.Substring(start + 1, end - start - 1);

							channel = $"GROUP_DYN_{grpName}";

						}

					}

					var colIdx = msg.IndexOf("] GV: ");

					cleanText = colIdx > 0 ? msg.Substring(colIdx + 6) : msg;

				}

				else if (msg.StartsWith("\ud83d\udcda ["))

				{

					try

					{

						var app = (QASmartTouch.App)Application.Current;

						if (app.CurrentGroups != null)

						{

							foreach (var sg in app.CurrentGroups)

							{

								if (msg.Contains($"[{sg.Name}]"))

								{

									channel = $"STUDY_{sg.Index}";

									break;

								}

							}

						}

					}

					catch { }

					var colIdx = msg.IndexOf("] GV: ");

					cleanText = colIdx > 0 ? msg.Substring(colIdx + 6) : msg;

				}

				else if (msg.StartsWith("\ud83d\udd12"))

				{

					channel = "TEACHER";

					var colonIdx = msg.IndexOf(": ");

					cleanText = colonIdx > 0 ? msg.Substring(colonIdx + 2) : msg;

				}

				else if (isHistory)

				{

					var parts = msg.Split('|', 3);

					cleanText = parts.Length >= 3 ? parts[2] : msg;

					channel = "ALL";

					if (cleanText.StartsWith("\ud83d\udce2 GV: ")) 

						cleanText = cleanText.Substring(7);

				}

				else if (msg.StartsWith("\ud83d\udce2 GV: "))

				{

					channel = "ALL";

					cleanText = msg.Substring(msg.IndexOf("GV: ") + 4);

				}

				else

				{

					channel = "ALL";

					cleanText = msg.StartsWith("💬 ") ? msg.Substring(3) : (msg.StartsWith("💬") ? msg.Substring(2) : msg);

				}



				if (string.IsNullOrWhiteSpace(cleanText))

				{

					return;

				}



				if (!isHistory)

				{

					string studentCode = "HS00001";

					var appObj = (QASmartTouch.App)Application.Current;

					if (appObj?.StudentNetwork != null && !string.IsNullOrEmpty(appObj.StudentNetwork.StudentCode))

					{

						studentCode = appObj.StudentNetwork.StudentCode;

					}

					else

					{

						try

						{

							var dbStudent = appObj?.Database?.Students.FirstOrDefault(s => s.IsOnline);

							if (dbStudent != null)

							{

								studentCode = dbStudent.StudentCode ?? studentCode;

							}

						}

						catch { }

					}



					string dbEventType = "TEACHER_CHAT";

					string dbChannel = "ALL";

					if (channel == "TEACHER")

					{

						dbEventType = "PRIVATE_CHAT";

						dbChannel = studentCode;

					}

					else if (channel.StartsWith("GROUP_") || channel.StartsWith("STUDY_"))

					{

						dbEventType = "GROUP_CHAT";

						dbChannel = channel;

					}



					Task.Run(() =>

					{

						try

						{

							lock (StudentChatPage._dbLock)

							{

								using (var dbContext = new QASmartClass.Data.AppDbContext())

								{

									dbContext.EventLogs.Add(new QASmartClass.Data.EventLog

									{

										EventType = dbEventType,

										Actor = "GV",

										Details = $"Tin nhắn: {cleanText} [CH:{dbChannel}]",

										Timestamp = DateTime.Now

									});

									dbContext.SaveChanges();

								}

							}

						}

						catch (Exception ex)

						{

							Log.Warning("Failed to save received teacher message to DB: {Err}", ex.Message);

						}

					});

				}



				if (!isHistory && _currentPage != "S6")

				{

					ShowStudentToast(text, cleanText, accentColorHex);

					try

					{

						System.Media.SystemSounds.Asterisk.Play();

					}

					catch { }

				}



				if (!isHistory && _currentPage != "S6")

				{

					IncrementStudentBadge();

				}



				_chatPage ??= new StudentChatPage();

				_chatPage.AppendNewMessage(msg);

				Log.Information("Teacher message handled: {Title} — {Msg}", text, msg);

			}

			catch (Exception ex)

			{

				Log.Warning("HandleTeacherMessage error: {Err}", ex.Message);

			}

		}



		private void ShowStudentToast(string title, string message, string accentColorHex)

		{

			try

			{

				Color color = (Color)ColorConverter.ConvertFromString(accentColorHex);

				Color color2 = Color.FromArgb(250, byte.MaxValue, byte.MaxValue, byte.MaxValue);

				Border toast = new Border

				{

					Background = new SolidColorBrush(color2),

					BorderBrush = new SolidColorBrush(color),

					BorderThickness = new Thickness(0.0, 0.0, 0.0, 3.0),

					CornerRadius = new CornerRadius(8.0),

					Margin = new Thickness(0.0, 0.0, 0.0, 8.0),

					Padding = new Thickness(14.0, 10.0, 14.0, 10.0),

					Cursor = Cursors.Hand,

					Opacity = 0.0,

					Effect = new DropShadowEffect

					{

						BlurRadius = 16.0,

						ShadowDepth = 4.0,

						Opacity = 0.2,

						Color = Colors.Black,

						Direction = 270.0

					}

				};

				Grid grid = new Grid();

				grid.ColumnDefinitions.Add(new ColumnDefinition

				{

					Width = GridLength.Auto

				});

				grid.ColumnDefinitions.Add(new ColumnDefinition

				{

					Width = new GridLength(1.0, GridUnitType.Star)

				});

				grid.ColumnDefinitions.Add(new ColumnDefinition

				{

					Width = GridLength.Auto

				});

				TextBlock element = new TextBlock

				{

					Text = "\ud83d\udc68\u200d\ud83c\udfeb",

					FontSize = 24.0,

					VerticalAlignment = VerticalAlignment.Center,

					Margin = new Thickness(0.0, 0.0, 10.0, 0.0)

				};

				Grid.SetColumn(element, 0);

				grid.Children.Add(element);

				StackPanel stackPanel = new StackPanel();

				stackPanel.Children.Add(new TextBlock

				{

					Text = title,

					FontSize = 12.0,

					FontWeight = FontWeights.SemiBold,

					Foreground = new SolidColorBrush(color)

				});

				stackPanel.Children.Add(new TextBlock

				{

					Text = ((message.Length > 100) ? (message.Substring(0, 100) + "...") : message),

					FontSize = 11.0,

					Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#424242")),

					TextWrapping = TextWrapping.Wrap,

					Margin = new Thickness(0.0, 3.0, 0.0, 0.0)

				});

				stackPanel.Children.Add(new TextBlock

				{

					Text = DateTime.Now.ToString("HH:mm:ss"),

					FontSize = 9.0,

					Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9E9E9E")),

					Margin = new Thickness(0.0, 3.0, 0.0, 0.0)

				});

				Grid.SetColumn(stackPanel, 1);

				grid.Children.Add(stackPanel);

				Button button = new Button

				{

					Content = "✕",

					FontSize = 12.0,

					Background = Brushes.Transparent,

					Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9E9E9E")),

					BorderThickness = new Thickness(0.0),

					Cursor = Cursors.Hand,

					VerticalAlignment = VerticalAlignment.Top,

					Padding = new Thickness(4.0, 0.0, 0.0, 0.0)

				};

				button.Click += delegate

				{

					DismissStudentToast(toast);

				};

				Grid.SetColumn(button, 2);

				grid.Children.Add(button);

				toast.Child = grid;

				toast.MouseLeftButtonDown += delegate

				{

					DismissStudentToast(toast);

					NavigateTo("S6");

				};

				stuToastPanel.Children.Insert(0, toast);

				while (stuToastPanel.Children.Count > 4)

				{

					stuToastPanel.Children.RemoveAt(stuToastPanel.Children.Count - 1);

				}

				DoubleAnimation animation = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(300.0))

				{

					EasingFunction = new CubicEase

					{

						EasingMode = EasingMode.EaseOut

					}

				};

				toast.BeginAnimation(UIElement.OpacityProperty, animation);

				DispatcherTimer timer = new DispatcherTimer

				{

					Interval = TimeSpan.FromSeconds(8.0)

				};

				timer.Tick += delegate

				{

					timer.Stop();

					DismissStudentToast(toast);

				};

				timer.Start();

			}

			catch (Exception ex)

			{

				Log.Warning("ShowStudentToast error: {Err}", ex.Message);

			}

		}



		private void DismissStudentToast(Border toast)

		{

			if (!stuToastPanel.Children.Contains(toast))

			{

				return;

			}

			DoubleAnimation doubleAnimation = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(200.0));

			doubleAnimation.Completed += delegate

			{

				if (stuToastPanel.Children.Contains(toast))

				{

					stuToastPanel.Children.Remove(toast);

				}

			};

			toast.BeginAnimation(UIElement.OpacityProperty, doubleAnimation);

		}



		private void IncrementStudentBadge()

		{

			if (!(_currentPage == "S6"))

			{

				_stuUnreadCount++;

				txtStuMsgBadge.Text = ((_stuUnreadCount > 99) ? "99+" : _stuUnreadCount.ToString());

				if (_isFocusModeActive)

				{

					stuMsgBadge.Visibility = Visibility.Collapsed;

				}

				else

				{

					stuMsgBadge.Visibility = Visibility.Visible;

				}

			}

		}



		private void ResetStudentBadge()

		{

			_stuUnreadCount = 0;

			stuMsgBadge.Visibility = Visibility.Collapsed;

		}



		private void CaptureAndSendScreenshot()

		{

			try

			{

				bool isViewingBroadcast = false;

				base.Dispatcher.Invoke(() =>

				{

					isViewingBroadcast = _broadcastOverlay != null && _broadcastOverlay.Visibility == Visibility.Visible;

				});



				string text;

				if (isViewingBroadcast)

				{

					// V4.1: Sinh ảnh trạng thái phẳng trên bộ nhớ RAM để tránh đệ quy và giảm tải CPU/GPU

					using (var bmp = new System.Drawing.Bitmap(800, 600))

					{

						using (var g = System.Drawing.Graphics.FromImage(bmp))

						{

							g.Clear(System.Drawing.Color.FromArgb(20, 20, 30));

							using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(123, 31, 162)))

							{

								g.FillRectangle(brush, 0, 220, 800, 160);

							}

							using (var fontTitle = new System.Drawing.Font("Segoe UI", 24, System.Drawing.FontStyle.Bold))

							using (var fontSub = new System.Drawing.Font("Segoe UI", 14, System.Drawing.FontStyle.Regular))

							using (var brushWhite = new System.Drawing.SolidBrush(System.Drawing.Color.White))

							using (var brushMuted = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(200, 200, 200)))

							{

								var sf = new System.Drawing.StringFormat

								{

									Alignment = System.Drawing.StringAlignment.Center,

									LineAlignment = System.Drawing.StringAlignment.Center

								};

								g.DrawString("ĐANG THEO DÕI BÀI GIẢNG", fontTitle, brushWhite, new System.Drawing.RectangleF(0, 220, 800, 90), sf);

								g.DrawString("Màn hình học sinh đang hiển thị bài giảng của Giáo viên", fontSub, brushMuted, new System.Drawing.RectangleF(0, 300, 800, 60), sf);

							}

						}

						using (MemoryStream memoryStream = new MemoryStream())

						{

							bmp.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Jpeg);

							text = Convert.ToBase64String(memoryStream.ToArray());

						}

					}

				}

				else

				{

					int screenW = 1024;

					int screenH = 768;

					double scaleX = 1.0;

					double scaleY = 1.0;

					base.Dispatcher.Invoke(delegate

					{

						screenW = (int)SystemParameters.PrimaryScreenWidth;

						screenH = (int)SystemParameters.PrimaryScreenHeight;

						DpiScale dpi = VisualTreeHelper.GetDpi(this);

						scaleX = dpi.DpiScaleX;

						scaleY = dpi.DpiScaleY;

					});

					int num = (int)((double)screenW * scaleX);

					int num2 = (int)((double)screenH * scaleY);

					if (num <= 0 || num2 <= 0)

					{

						return;

					}

					nint dC = NativeMethods.GetDC(IntPtr.Zero);

					nint num3 = NativeMethods.CreateCompatibleDC(dC);

					nint num4 = NativeMethods.CreateCompatibleBitmap(dC, num, num2);

					nint hgdiobj = NativeMethods.SelectObject(num3, num4);

					NativeMethods.BitBlt(num3, 0, 0, num, num2, dC, 0, 0, 13369376);

					int pixelWidth = 800;

					int pixelHeight = (int)((double)num2 * (800.0 / (double)num));

					BitmapSource source = Imaging.CreateBitmapSourceFromHBitmap(num4, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(pixelWidth, pixelHeight));

					NativeMethods.SelectObject(num3, hgdiobj);

					NativeMethods.DeleteObject(num4);

					NativeMethods.DeleteDC(num3);

					NativeMethods.ReleaseDC(IntPtr.Zero, dC);

					JpegBitmapEncoder jpegBitmapEncoder = new JpegBitmapEncoder

					{

						QualityLevel = 60

					};

					jpegBitmapEncoder.Frames.Add(BitmapFrame.Create(source));

					using (MemoryStream memoryStream = new MemoryStream())

					{

						jpegBitmapEncoder.Save(memoryStream);

						text = Convert.ToBase64String(memoryStream.ToArray());

					}

				}

				if (!(Application.Current is App { StudentNetwork: not null } app))

				{

					return;

				}

				string text2 = app.StudentNetwork.StudentCode ?? "HS00001";

				if (text2 == "HS00001")

				{

					Student student = app.Database?.Students.FirstOrDefault((Student s) => s.IsOnline);

					if (student != null)

					{

						text2 = student.StudentCode ?? text2;

					}

				}

				string text3 = "SCREENSHOT|" + text2 + "|" + text;

				app.StudentNetwork.SendAsync(text3);

				Log.Debug("Student screen captured and sent to teacher ({Length} Base64 chars)", text3.Length);

			}

			catch (Exception ex)

			{

				Log.Warning("CaptureAndSendScreenshot error: {Err}", ex.Message);

			}

		}



		private void EnforceWebPolicy()

		{

			try

			{

				bool shouldBlock = _isWebBlocked || _isWebWhitelistActive;

				if (!shouldBlock) return;



				string[] browserNames = { "chrome", "msedge", "firefox", "opera", "brave", "iexplore" };

				bool killedAny = false;

				

				foreach (var name in browserNames)

				{

					try

					{

						var processes = System.Diagnostics.Process.GetProcessesByName(name);

						foreach (var p in processes)

						{

							try

							{

								p.Kill();

								killedAny = true;

								Log.Information("Killed unauthorized browser process: {Name}", name);

							}

							catch { }

						}

					}

					catch { }

				}



				if (killedAny)

				{

					if ((DateTime.Now - _lastBrowserWarningTime).TotalSeconds > 5)

					{

						_lastBrowserWarningTime = DateTime.Now;

						ShowNotification("Web bị khóa/hạn chế", "Giáo viên đã giới hạn truy cập Web ngoài học tập.", "#C62828");

					}

				}

			}

			catch (Exception ex)

			{

				Log.Warning("Error in EnforceWebPolicy: {Err}", ex.Message);

			}

		}



		private void AddUrlToLocalWhitelist(string url)

		{

			if (string.IsNullOrEmpty(url)) return;

			lock (_allowedUrls)

			{

				if (!_allowedUrls.Contains(url))

				{

					_allowedUrls.Add(url);

					Log.Information("Added URL to local whitelist: {Url}", url);

					

					// If secure browser is open, sync to it

					if (_secureWebWindow != null)

					{

						_secureWebWindow.AddAllowedUrl(url);

					}

				}

			}

		}



		private void OpenInSecureWebWindow(string url)

		{

			base.Dispatcher.Invoke(() =>

			{

				try

				{

					if (_secureWebWindow == null)

					{

						_secureWebWindow = new SecureWebWindow(url);

						_secureWebWindow.Closed += (s, ev) => _secureWebWindow = null;

						

						// Sync current whitelisted URLs

						lock (_allowedUrls)

						{

							foreach (var u in _allowedUrls)

							{

								_secureWebWindow.AddAllowedUrl(u);

							}

						}

						

						_secureWebWindow.Show();

					}

					else

					{

						_secureWebWindow.NavigateTo(url);

						_secureWebWindow.Activate();

					}

				}

				catch (Exception ex)

				{

					Log.Error("Failed to open SecureWebWindow: {Err}", ex.Message);

				}

			});

		}



		private void CloseSecureWebWindow()

		{

			base.Dispatcher.Invoke(() =>

			{

				try

				{

					if (_secureWebWindow != null)

					{

						_secureWebWindow.Close();

						_secureWebWindow = null;

					}

				}

				catch { }

			});

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



		public void SendToolSubmission(string toolId, string submissionData)

		{

			try

			{

				var app = Application.Current as App;

				if (app?.StudentNetwork != null && app.StudentNetwork.IsConnected)

				{

					string code = app.StudentNetwork.StudentCode ?? "HS00001";

					string name = "Học sinh";

					if (app.Database != null)

					{

						var student = app.Database.Students.FirstOrDefault(s => s.IsOnline);

						if (student != null)

						{

							code = student.StudentCode ?? code;

							name = student.FullName ?? name;

						}

					}

					string msg = $"STUDENT_SUBMISSION|{toolId}|{code}|{name}|{submissionData}";

					app.StudentNetwork.SendAsync(msg);

				}

			}

			catch (Exception ex)

			{

				Log.Warning("SendToolSubmission error: {Err}", ex.Message);

			}

		}

		private System.Windows.Threading.DispatcherTimer? _telemetryTimer;

		private void ToggleTelemetryOverlay()
		{
			if (telemetryOverlay.Visibility == Visibility.Visible)
			{
				telemetryOverlay.Visibility = Visibility.Collapsed;
				_telemetryTimer?.Stop();
			}
			else
			{
				telemetryOverlay.Visibility = Visibility.Visible;
				if (_telemetryTimer == null)
				{
					_telemetryTimer = new System.Windows.Threading.DispatcherTimer();
					_telemetryTimer.Interval = TimeSpan.FromSeconds(1);
					_telemetryTimer.Tick += TelemetryTimer_Tick;
				}
				_telemetryTimer.Start();
				UpdateTelemetryOverlay();
			}
		}

		private void TelemetryTimer_Tick(object? sender, EventArgs e)
		{
			UpdateTelemetryOverlay();
		}

		private void UpdateTelemetryOverlay()
		{
			try
			{
				double latency = StreamTelemetryCollector.Instance.LastLatencyMs;
				double fps = StreamTelemetryCollector.Instance.GetCurrentFps();
				double jitter = StreamTelemetryCollector.Instance.GetCurrentJitter();
				double dropRate = StreamTelemetryCollector.Instance.GetFrameDropRate();
				double lossRate = StreamTelemetryCollector.Instance.GetPacketLossRate();

				txtTelemetryLatency.Text = $"{latency:F1} ms";
				txtTelemetryFps.Text = $"{fps:F1}";
				txtTelemetryJitter.Text = $"{jitter:F1} ms";
				txtTelemetryFrameDrop.Text = $"{dropRate:P2}";
				txtTelemetryPacketLoss.Text = $"{lossRate:P2}";

				// Color thresholds (SP-01 to SP-04)
				// Latency
				if (latency <= QaStandards.MaxLatencyMs)
					txtTelemetryLatency.Foreground = new SolidColorBrush(Color.FromRgb(74, 222, 128)); // Green
				else if (latency <= QaStandards.PeakLatencyMs)
					txtTelemetryLatency.Foreground = new SolidColorBrush(Color.FromRgb(251, 191, 36)); // Yellow
				else
					txtTelemetryLatency.Foreground = new SolidColorBrush(Color.FromRgb(248, 113, 113)); // Red

				// FPS
				if (fps >= 25)
					txtTelemetryFps.Foreground = new SolidColorBrush(Color.FromRgb(74, 222, 128));
				else if (fps >= 15)
					txtTelemetryFps.Foreground = new SolidColorBrush(Color.FromRgb(251, 191, 36));
				else
					txtTelemetryFps.Foreground = new SolidColorBrush(Color.FromRgb(248, 113, 113));

				// Jitter
				if (jitter <= 10)
					txtTelemetryJitter.Foreground = new SolidColorBrush(Color.FromRgb(74, 222, 128));
				else if (jitter <= 30)
					txtTelemetryJitter.Foreground = new SolidColorBrush(Color.FromRgb(251, 191, 36));
				else
					txtTelemetryJitter.Foreground = new SolidColorBrush(Color.FromRgb(248, 113, 113));

				// Frame Drop
				if (dropRate <= QaStandards.MaxFrameDropRate)
					txtTelemetryFrameDrop.Foreground = new SolidColorBrush(Color.FromRgb(74, 222, 128));
				else if (dropRate <= 0.05)
					txtTelemetryFrameDrop.Foreground = new SolidColorBrush(Color.FromRgb(251, 191, 36));
				else
					txtTelemetryFrameDrop.Foreground = new SolidColorBrush(Color.FromRgb(248, 113, 113));

				// Packet Loss
				if (lossRate <= 0.02)
					txtTelemetryPacketLoss.Foreground = new SolidColorBrush(Color.FromRgb(74, 222, 128));
				else if (lossRate <= 0.10)
					txtTelemetryPacketLoss.Foreground = new SolidColorBrush(Color.FromRgb(251, 191, 36));
				else
					txtTelemetryPacketLoss.Foreground = new SolidColorBrush(Color.FromRgb(248, 113, 113));
			}
			catch (Exception ex)
			{
				Log.Warning("Telemetry overlay update error: {Msg}", ex.Message);
			}
		}

		// === VNC_BROADCAST: Logic phía Học sinh khởi chạy và giám sát vnctool-client.exe ===
		private string GetVncDirectory()
		{
			string localDir = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "vnctool");
			if (System.IO.Directory.Exists(localDir) && System.IO.File.Exists(System.IO.Path.Combine(localDir, "vnctool-client.exe")))
			{
				return localDir;
			}
			return @"D:\JOB\vnctool";
		}

		private void ShowVncBroadcast(string ip, int port, string sessionCode, bool isForce)
		{
			try
			{
				_vncServerIp = ip;
				_vncServerPort = port;
				_vncSessionCode = sessionCode;
				_vncForceWatch = isForce;
				_lastBroadcastUpdateTime = DateTime.UtcNow;
				_lastTcpCommandTime = DateTime.UtcNow;

				// Ghi file client.ini
				string vncDir = GetVncDirectory();
				if (!System.IO.Directory.Exists(vncDir))
				{
					System.IO.Directory.CreateDirectory(vncDir);
				}

				string iniPath = System.IO.Path.Combine(vncDir, "client.ini");
				string content = $"; client.ini - generated by QA SmartClass C#\n" +
				                 $"[server]\n" +
				                 $"host = {ip}\n" +
				                 $"port = {port}\n\n" +
				                 $"[network]\n" +
				                 $"reconnect_initial_ms = 500\n" +
				                 $"reconnect_max_ms = 10000\n" +
				                 $"handshake_timeout_ms = 8000\n\n" +
				                 $"[auth]\n" +
				                 $"session_code = {sessionCode}\n\n" +
				                 $"[ui]\n" +
				                 $"window_width = 1280\n" +
				                 $"window_height = 800\n" +
				                 $"fit_image = true\n" +
				                 $"fullscreen = true\n";

				System.IO.File.WriteAllText(iniPath, content, System.Text.Encoding.ASCII);
				Log.Information("[Student-VNC] Ghi file client.ini thành công: {Path}", iniPath);

				// Hiển thị Overlay Đang tải bài giảng (Sư phạm)
				Dispatcher.Invoke(() =>
				{
					if (_broadcastOverlay == null)
					{
						_broadcastOverlay = new Grid
						{
							Background = new SolidColorBrush(Color.FromArgb(245, 15, 15, 25)),
							Tag = "ScreenBroadcastOverlay"
						};

						var loadingText = new TextBlock
						{
							Text = "⏳ Đang kết nối tới màn hình giáo viên qua VNC-LAN...",
							FontSize = 16.0,
							FontWeight = FontWeights.SemiBold,
							Foreground = Brushes.White,
							HorizontalAlignment = HorizontalAlignment.Center,
							VerticalAlignment = VerticalAlignment.Center
						};
						_broadcastOverlay.Children.Add(loadingText);

						Grid grid = (Grid)base.Content;
						Grid.SetColumnSpan(_broadcastOverlay, 10);
						Grid.SetRowSpan(_broadcastOverlay, 10);
						grid.Children.Add(_broadcastOverlay);
					}
					
					ApplyKioskMode(isForce);
				});

				// Tắt tiến trình cũ nếu có
				StopVncClientProcessOnly();

				// Khởi chạy vnctool-client.exe
				string exePath = System.IO.Path.Combine(vncDir, "vnctool-client.exe");
				if (!System.IO.File.Exists(exePath))
				{
					throw new System.IO.FileNotFoundException($"Không tìm thấy tệp vnctool-client.exe ở đường dẫn {vncDir}");
				}

				var startInfo = new System.Diagnostics.ProcessStartInfo
				{
					FileName = exePath,
					WorkingDirectory = vncDir,
					UseShellExecute = false,
					CreateNoWindow = false
				};

				_vncClientProcess = System.Diagnostics.Process.Start(startInfo);
				_isVncClientRunning = true;
				
				StartVncWatchdog();

				Log.Information("[Student-VNC] Đã khởi động VNC Client kết nối tới {IP}:{Port}", ip, port);
				ShowNotification("Màn hình giáo viên (VNC)", "Đang trình chiếu màn hình qua VNC...", "#7B1FA2");
			}
			catch (Exception ex)
			{
				Log.Error("[Student-VNC] Lỗi khi mở VNC Client: {Err}", ex.Message);
				ShowNotification("Lỗi kết nối VNC", "Không thể chạy VNC Client: " + ex.Message, "#C62828");
				CloseVncBroadcast();
			}
		}

		private void CloseVncBroadcast()
		{
			try
			{
				_isVncClientRunning = false;
				StopVncWatchdog();
				StopVncClientProcessOnly();

				Dispatcher.Invoke(() =>
				{
					if (_broadcastOverlay != null)
					{
						Grid grid = (Grid)base.Content;
						grid.Children.Remove(_broadcastOverlay);
						_broadcastOverlay = null;
					}
					ApplyKioskMode(false);
				});

				Log.Information("[Student-VNC] Đã đóng trình chiếu VNC");
			}
			catch (Exception ex)
			{
				Log.Warning("[Student-VNC] Đóng VNC lỗi: {Err}", ex.Message);
			}
		}

		private void StopVncClientProcessOnly()
		{
			try
			{
				// Tiêu diệt tiến trình VNC Client zombie cũ nếu có (V2.2.5)
				foreach (var proc in System.Diagnostics.Process.GetProcessesByName("vnctool-client"))
				{
					try { proc.Kill(); proc.Dispose(); } catch {}
				}

				if (_vncClientProcess != null && !_vncClientProcess.HasExited)
				{
					_vncClientProcess.Kill();
					_vncClientProcess.Dispose();
					_vncClientProcess = null;
					Log.Information("[Student-VNC] vnctool-client.exe process killed");
				}
			}
			catch (Exception ex)
			{
				Log.Debug("[Student-VNC] Kill process error: {Err}", ex.Message);
			}
		}

		private void StartVncWatchdog()
		{
			Dispatcher.Invoke(() =>
			{
				if (_vncWatchdogTimer == null)
				{
					_vncWatchdogTimer = new System.Windows.Threading.DispatcherTimer
					{
						Interval = TimeSpan.FromMilliseconds(500)
					};
					_vncWatchdogTimer.Tick += VncWatchdogTimer_Tick;
				}
				_vncWatchdogTimer.Start();
			});
		}

		private void StopVncWatchdog()
		{
			Dispatcher.Invoke(() =>
			{
				_vncWatchdogTimer?.Stop();
			});
		}

		private void VncWatchdogTimer_Tick(object? sender, EventArgs e)
		{
			if (!_isVncClientRunning) return;

			// 1. Kiểm tra nếu tiến trình bị tắt đột ngột thì bật lại (chống học sinh gian lận)
			try
			{
				if (_vncClientProcess == null || _vncClientProcess.HasExited)
				{
					Log.Warning("[Student-VNC] vnctool-client.exe exited unexpectedly. Restarting...");
					
					string vncDir = GetVncDirectory();
					string exePath = System.IO.Path.Combine(vncDir, "vnctool-client.exe");
					if (System.IO.File.Exists(exePath))
					{
						var startInfo = new System.Diagnostics.ProcessStartInfo
						{
							FileName = exePath,
							WorkingDirectory = vncDir,
							UseShellExecute = false,
							CreateNoWindow = false
						};
						_vncClientProcess = System.Diagnostics.Process.Start(startInfo);
						Log.Information("[Student-VNC] VNC Client restarted successfully.");
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error("[Student-VNC] Watchdog restart VNC Client failed: {Err}", ex.Message);
			}

			// 2. Kiểm tra Safety Auto-Unlock (watchdog mất kết nối 15 giây)
			var timeSinceLastUpdate = (DateTime.UtcNow - _lastBroadcastUpdateTime).TotalSeconds;
			var timeSinceLastTcp = (DateTime.UtcNow - _lastTcpCommandTime).TotalSeconds;
			
			if (timeSinceLastUpdate > 15.0 && timeSinceLastTcp > 15.0)
			{
				Log.Warning("[Student-VNC] Safety Auto-Unlock triggered: No VNC signal for {Sec}s.", timeSinceLastUpdate);
				CloseVncBroadcast();
				ShowNotification("⚠️ Kết nối gián đoạn", "Mất tín hiệu từ giáo viên quá 15 giây. Hệ thống tự động mở khóa.", "#C62828");
			}
		}
	}
}

