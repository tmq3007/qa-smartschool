package com.qasmartclass.student

import android.content.Context
import android.content.res.ColorStateList
import android.graphics.BitmapFactory
import android.graphics.Typeface
import android.os.Bundle
import android.text.method.HideReturnsTransformationMethod
import android.text.method.PasswordTransformationMethod
import android.util.Log
import android.view.Gravity
import android.view.View
import android.view.ViewGroup
import android.widget.*
import androidx.activity.OnBackPressedCallback
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.qasmartclass.student.services.BroadcastLockManager
import com.qasmartclass.student.services.FocusReporter
import com.qasmartclass.student.services.HeartbeatWatchdog
import com.qasmartclass.student.services.TeacherDiscoveryService
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.isActive
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import java.io.InputStream
import java.io.OutputStream
import java.net.HttpURLConnection
import java.net.InetSocketAddress
import java.net.Socket
import java.net.URL
import java.net.URLDecoder
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale
import com.qasmartclass.student.crypto.CryptoHelper

class MainActivity : AppCompatActivity() {

    // Discovery UI (Screen 1 — Single-Viewport Join Card)
    private lateinit var discoveryPanel: View
    private lateinit var discoveryInnerRoot: LinearLayout
    private lateinit var txtStatus: TextView
    private lateinit var progressBar: ProgressBar
    private lateinit var btnRefresh: Button
    private lateinit var edtStudentName: EditText
    private lateinit var edtStudentCode: EditText
    private lateinit var edtPassword: EditText
    private lateinit var edtJoinClassCode: EditText
    private var lastEnteredClassCode = ""
    private lateinit var btnTogglePassword: ImageButton
    private lateinit var edtTeacherIp: EditText
    private lateinit var chkRemember: CheckBox
    private lateinit var txtError: TextView
    private lateinit var btnConnect: Button
    private lateinit var txtDiscoveryDot: TextView
    private lateinit var txtDiscoveryHeader: TextView
    private lateinit var txtJoinAvatarInitials: TextView

    // Connected Shell (Screen 2 — M3 Adaptive NavigationRail + Top App Bar)
    private lateinit var connectedPanel: LinearLayout
    private lateinit var txtPageTitle: TextView
    private lateinit var txtHeaderLiveBadge: TextView
    private lateinit var txtConnectedClassInfo: TextView
    private lateinit var txtTeacherDetail: TextView
    private lateinit var txtStudentDetail: TextView
    private lateinit var txtServerIpDetail: TextView
    private lateinit var btnDisconnect: Button
    private lateinit var btnRaiseHand: Button
    private lateinit var btnTopHandReason: Button

    // M3 Navigation Rail (Tablet >= 600dp)
    private lateinit var navRailBar: LinearLayout
    private lateinit var navRailDivider: View
    private lateinit var railItemClass: LinearLayout
    private lateinit var railItemWork: LinearLayout
    private lateinit var railItemChat: LinearLayout
    private lateinit var railItemFiles: LinearLayout
    private lateinit var pillRailClass: FrameLayout
    private lateinit var pillRailWork: FrameLayout
    private lateinit var pillRailChat: FrameLayout
    private lateinit var pillRailFiles: FrameLayout
    private lateinit var iconRailClass: ImageView
    private lateinit var iconRailWork: ImageView
    private lateinit var iconRailChat: ImageView
    private lateinit var iconRailFiles: ImageView
    private lateinit var labelRailClass: TextView
    private lateinit var labelRailWork: TextView
    private lateinit var labelRailChat: TextView
    private lateinit var labelRailFiles: TextView
    private lateinit var badgeRailWork: TextView
    private lateinit var badgeRailChat: TextView
    private lateinit var badgeRailFiles: TextView
    private var activePreviewDialog: AlertDialog? = null

    // Tab 0 (Lớp học) & Lesson State
    private lateinit var panelSoon: LinearLayout
    private lateinit var txtSoonTitle: TextView
    private lateinit var txtHomeGreeting: TextView
    private lateinit var txtHomeSub: TextView
    private lateinit var txtHomeLessonTitle: TextView
    private lateinit var layoutHomeLessonWaiting: LinearLayout
    private lateinit var layoutHomeLessonActive: LinearLayout
    private lateinit var txtHomeSubjectBadge: TextView
    private lateinit var txtHomeStageBadge: TextView
    private lateinit var txtHomeBoardHeader: TextView
    private lateinit var txtHomeBoardContent: TextView
    private var homeStudentName = ""
    private var homeClassName = ""
    private var homeTeacherName = ""
    private var isLessonActive = false
    private var activeLessonTitle = ""
    private var activeLessonSubject = ""
    private var activeLessonSummary = ""
    private var activeLessonStage = 0
    private var hasTeacherNotice = false
    private var unreadTeacherChatCount = 0

    // Tab 1 (Bài tập & Kiểm tra — Classwork Hub)
    private lateinit var chipWorkQuiz: Button
    private lateinit var chipWorkHomework: Button
    private lateinit var chipWorkSurvey: Button
    private lateinit var chipWorkResults: Button
    private var currentWorkSubTab = "QUIZ" // "QUIZ" | "HOMEWORK" | "SURVEY" | "RESULTS"
    private lateinit var iconSoonWrapper: View
    private lateinit var txtSoonSubtitle: TextView
    private lateinit var layoutInteractiveFeature: LinearLayout

    // Tab 3 (Tài liệu & Phiên học)
    private lateinit var panelFilesProfile: ScrollView
    private lateinit var filesGridContainer: LinearLayout
    private lateinit var colReceivedFiles: LinearLayout
    private lateinit var colSessionProfile: LinearLayout
    private lateinit var btnAttachQuickFile: Button
    private lateinit var btnSubmitFileFromDocs: Button
    private lateinit var btnRefreshFiles: Button
    private lateinit var layoutReceivedFiles: LinearLayout
    private lateinit var btnProfileSwitchStudent: Button

    private val pickStudentSubmitFileLauncher = registerForActivityResult(
        androidx.activity.result.contract.ActivityResultContracts.OpenDocument()
    ) { uri: android.net.Uri? ->
        if (uri != null) {
            uploadStudentUriToTeacher(uri)
        }
    }

    private fun launchStudentFilePicker() {
        try {
            pickStudentSubmitFileLauncher.launch(arrayOf("*/*"))
        } catch (e: Exception) {
            Toast.makeText(this, "Không mở được trình chọn tệp: ${e.message}", Toast.LENGTH_SHORT).show()
        }
    }

    private data class ReceivedFileItem(
        val fileName: String,
        val urlOrPath: String,
        val timeText: String,
        val isLocalSnapshot: Boolean = false
    )
    private val receivedTeacherFiles = mutableListOf<ReceivedFileItem>()

    private data class QuizQuestionItem(
        val id: Int,
        val content: String,
        val options: List<String>,
        val points: Double
    )
    private var activeQuizId: Int = 0
    private val activeQuizQuestions = mutableListOf<QuizQuestionItem>()
    private val selectedQuizAnswers = mutableMapOf<Int, String>()
    private var quizSubmitted = false
    private var lastQuizScoreText = ""
    private var lastQuizCorrectDetail = ""

    private var activeSurveyId = ""
    private var activeSurveyTitle = "Khảo sát lớp học"
    private var activeSurveyQuestion = ""
    private var activeSurveyType = "satisfaction"
    private val activeSurveyOptions = mutableListOf<String>()
    private var selectedSurveyOptionIdx = -1
    private var selectedSurveyStar = 0
    private var surveySubmitted = false

    private var activeHwId = 0
    private var activeHwSubject = ""
    private var activeHwTitle = ""
    private var activeHwDesc = ""
    private var activeHwDeadline = ""
    private var hwDraftAnswer = ""
    private var hwSubmitted = false
    private var lastHwAckMessage = ""

    // Discovery & Reconnect Views
    private lateinit var lanReconnectBanner: LinearLayout
    private lateinit var txtLanReconnectStatus: TextView
    private lateinit var discoveryGridContainer: LinearLayout
    private lateinit var colStudentIdentity: LinearLayout
    private lateinit var colLanRooms: LinearLayout
    private lateinit var btnAdvancedConfig: Button
    private lateinit var btnEditStudentIdentity: Button
    private lateinit var txtStudentIdentityName: TextView
    private lateinit var txtStudentIdentityCode: TextView
    private lateinit var txtSeatBadge: TextView
    private lateinit var txtLanPingBadge: TextView
    private lateinit var cardDiscoveredClass: LinearLayout

    // Screen 3: Google Meet Fullscreen Broadcast Controls + EasyTouch Floating Orb
    private lateinit var topBroadcastBadgeBar: LinearLayout
    private lateinit var btnEasyTouchBroadcast: View
    private lateinit var txtEasyTouchPodTitle: TextView
    private lateinit var btnCollapseEasyTouchStudent: Button
    private lateinit var floatingBroadcastDock: LinearLayout
    private lateinit var btnInlineBroadcastAudio: Button
    private lateinit var btnFullscreenMuteAudio: Button
    private lateinit var btnBroadcastVolume: Button
    private lateinit var btnFullscreenSnapshot: Button
    private var broadcastUiHideJob: Job? = null
    private var broadcastAudioJob: Job? = null
    @Volatile private var isBroadcastAudioMuted = false
    private var isForceBroadcastMode = false
    private var easyTouchCustomPositioned = false
    @Volatile private var hasPromptedCaptureThisSession = false

    private fun getDeviceShortTag(): String {
        return try {
            val aid = android.provider.Settings.Secure.getString(
                contentResolver,
                android.provider.Settings.Secure.ANDROID_ID
            ).orEmpty().filter { it.isLetterOrDigit() }.uppercase()
            if (aid.length >= 4) aid.takeLast(4) else "TAB1"
        } catch (_: Exception) {
            "TAB1"
        }
    }

    private fun buildAndroidPcName(): String = "Android-${getDeviceShortTag()}"

    // Split-Screen Document + Quick Note Sidebar
    private lateinit var lectureSplitContainer: LinearLayout
    private lateinit var colLectureDocument: LinearLayout
    private lateinit var colLectureSidebar: LinearLayout
    private lateinit var btnToggleLessonSidebar: Button
    private lateinit var btnZoomOut: Button
    private lateinit var btnZoomIn: Button
    private lateinit var txtZoomLevel: TextView
    private lateinit var quickNoteCanvas: com.qasmartclass.student.views.QuickNoteCanvasView
    private lateinit var layoutLatestTeacherFileBar: LinearLayout
    private lateinit var txtLatestTeacherFileName: TextView
    private lateinit var btnPreviewLatestTeacherFile: Button
    private lateinit var btnNotePen: Button
    private lateinit var btnNoteHighlight: Button
    private lateinit var btnNoteEraser: Button
    private lateinit var btnNoteClear: Button
    private lateinit var edtQuickLessonAnswer: EditText
    private lateinit var layoutUploadProgress: LinearLayout
    private lateinit var progressUploadBar: ProgressBar
    private lateinit var txtUploadPercent: TextView
    private lateinit var txtUploadStatusLabel: TextView
    private lateinit var btnQuickSubmitWork: Button

    // Screen 4: Kiosk Focus Lock Screen
    private lateinit var lockContentCard: LinearLayout
    private lateinit var imgLockIllustration: View
    private lateinit var txtLockLessonName: TextView
    private lateinit var txtLockTimer: TextView

    private var currentZoomPercent = 100
    // [MYTHWARE_LITE] Đã ẩn phần Sổ nháp nhanh theo yêu cầu
    private var isLessonSidebarVisible = false
    private var isHandAcceptedByTeacher = false
    private var lockTimerJob: Job? = null
    private var isUserInitiatedDisconnect = false
    private var lastMeasuredPingMs: Long = 0L

    // Adaptive Bottom Navigation Bar (Compact < 600dp)
    private lateinit var bottomNavBar: LinearLayout
    private lateinit var btnBottomHome: View
    private lateinit var btnBottomLecture: View
    private lateinit var btnBottomChat: View
    private lateinit var btnBottomRaise: View
    private lateinit var capsuleBottomHome: FrameLayout
    private lateinit var capsuleBottomLecture: FrameLayout
    private lateinit var capsuleBottomChat: FrameLayout
    private lateinit var capsuleBottomRaise: FrameLayout
    private lateinit var iconBottomHome: ImageView
    private lateinit var labelBottomHome: TextView
    private lateinit var iconBottomLecture: ImageView
    private lateinit var labelBottomLecture: TextView
    private lateinit var iconBottomChat: ImageView
    private lateinit var labelBottomChat: TextView
    private lateinit var iconBottomRaise: ImageView
    private lateinit var labelBottomRaise: TextView

    // Tab 0: Lecture & Broadcast Stage
    private lateinit var panelLecture: LinearLayout
    private lateinit var layoutNoBroadcast: LinearLayout
    private lateinit var layoutLiveBroadcast: LinearLayout
    private lateinit var txtInlineBroadcastTitle: TextView
    private lateinit var imgInlineBroadcast: ImageView
    private lateinit var btnFullscreenBroadcast: Button
    private lateinit var messageCard: LinearLayout
    private lateinit var txtTeacherMessage: TextView

    // Tab 2: Full-Height Google Chat Stream
    private lateinit var panelChat: LinearLayout
    private lateinit var txtChatChannelBadge: TextView
    private lateinit var scrollChat: ScrollView
    private lateinit var chatContainer: LinearLayout
    private lateinit var edtChatMessage: EditText
    private lateinit var btnSendChat: Button
    private lateinit var chatTabBar: LinearLayout

    // Chat: 2 kênh — "CLASS" (nhắn với lớp) và "TEACHER" (nhắn riêng với giáo viên)
    private data class ChatEntry(
        val sender: String,
        val message: String,
        val isSelf: Boolean,
        val time: String,
        val channel: String
    )
    private val chatHistory = mutableListOf<ChatEntry>()
    private var currentChatChannel = "TEACHER" // "CLASS" | "TEACHER" | "GROUP_xxx"
    private var classChatMuted = false
    private val unreadChatChannels = mutableSetOf<String>()

    // Nhóm chat do giáo viên tạo (đồng bộ qua CMD|CHAT_GROUPS): code = "GROUP_xxx"
    private data class ChatGroupChannel(val code: String, val name: String, val emoji: String)
    private val chatGroups = mutableListOf<ChatGroupChannel>()

    // Screen Broadcast Fullscreen Overlay
    private lateinit var broadcastOverlay: FrameLayout
    private lateinit var imgBroadcastScreen: ImageView
    private lateinit var progressBroadcastLoading: ProgressBar
    private lateinit var txtBroadcastStatus: TextView
    private lateinit var btnFullscreenRaiseHand: Button
    private lateinit var btnCloseBroadcast: Button

    // Lock Screen Overlay
    private lateinit var lockOverlay: FrameLayout
    private lateinit var txtLockTitle: TextView
    private lateinit var txtLockMessage: TextView

    // Security & Crypto state
    private var currentSessionKey: String = ""
    private var currentSessionSalt: String = ""
    private var currentClassCode: String = ""

    // Native Services
    private lateinit var lockManager: BroadcastLockManager
    private lateinit var heartbeatWatchdog: HeartbeatWatchdog
    private lateinit var focusReporter: FocusReporter
    private val discoveryService = TeacherDiscoveryService()

    private var currentTeacherIp: String? = null
    private var tcpSocket: Socket? = null
    private var tcpOutputStream: OutputStream? = null
    private var discoveryJob: Job? = null
    private var connectionJob: Job? = null
    private var reconnectJob: Job? = null
    private val connectionGeneration = java.util.concurrent.atomic.AtomicLong(0L)
    @Volatile private var isConnectingInProgress = false
    private var heartbeatJob: Job? = null
    private var isHandRaised = false
    private var currentHandRaiseReason = "Phát biểu"
    private var currentTab = 0
    private var needCapturePrompt = false

    private val captureLauncher = registerForActivityResult(
        androidx.activity.result.contract.ActivityResultContracts.StartActivityForResult()
    ) { result ->
        val data = result.data
        if (result.resultCode == android.app.Activity.RESULT_OK && data != null) {
            val svc = android.content.Intent(this, com.qasmartclass.student.services.ScreenCaptureService::class.java)
                .putExtra(com.qasmartclass.student.services.ScreenCaptureService.EXTRA_CODE, result.resultCode)
                .putExtra(com.qasmartclass.student.services.ScreenCaptureService.EXTRA_DATA, data)
            androidx.core.content.ContextCompat.startForegroundService(this, svc)
            needCapturePrompt = false
        } else {
            Toast.makeText(this, "Giáo viên sẽ không xem được màn hình của bạn nếu bạn từ chối chia sẻ", Toast.LENGTH_LONG).show()
        }
    }

    // Cached student info
    private var currentStudentName = ""
    private var currentStudentCode = ""
    private var currentClassName = "Lớp học SmartClass"
    private var currentTeacherName = "Giáo viên"

    // Screen Broadcast State
    private var isScreenBroadcastActive = false
    private var currentBroadcastToken: String = ""
    private var currentTeacherWebPort: Int = 8080
    private var broadcastPollJob: Job? = null

    // Login state & discovery auto port
    private var isPasswordVisible = false
    private var discoveredTeacherIp: String? = null
    private var discoveredTeacherPort: Int = TCP_PORT
    private var discoveredChannelId: Int = 1
    private var discoveredRequirePin: Boolean = false
    private val ignoredTeacherIps = mutableSetOf<String>()

    companion object {
        private const val TAG = "StudentMainActivity"
        private const val PREFS_NAME = "QASmartClassPrefs"
        private const val KEY_LAST_IP = "LastTeacherIp"
        private const val KEY_STUDENT_NAME = "StudentName"
        private const val KEY_STUDENT_CODE = "StudentCode"
        private const val KEY_PASSWORD = "StudentPassword"
        private const val KEY_REMEMBER = "RememberMe"
        private const val TCP_PORT = 29877
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        // Chốt chặn chống nhân bản Task khi Launcher mở lại sau khi cài APK hoặc chuyển từ trình xem tài liệu ngoài
        if (!isTaskRoot &&
            intent?.hasCategory(android.content.Intent.CATEGORY_LAUNCHER) == true &&
            intent?.action == android.content.Intent.ACTION_MAIN) {
            finish()
            return
        }
        // Luôn ẩn bàn phím ảo khi vừa mở màn hình để không che nút "VÀO LỚP HỌC NGAY" trên Tablet ngang
        window.setSoftInputMode(
            android.view.WindowManager.LayoutParams.SOFT_INPUT_STATE_ALWAYS_HIDDEN or
                android.view.WindowManager.LayoutParams.SOFT_INPUT_ADJUST_RESIZE
        )
        setContentView(R.layout.activity_main)

        // Initialize Services
        lockManager = BroadcastLockManager(this)
        heartbeatWatchdog = HeartbeatWatchdog(this, lockManager)
        focusReporter = FocusReporter(this) { tcpOutputStream }

        initViews()
        setupListeners()
        applyResponsiveLayout()
        refreshLatestTeacherFileBanner()
        currentFocus?.clearFocus()

        // Start Auto Discovery via UDP
        startAutoDiscovery()
    }

    override fun onNewIntent(intent: android.content.Intent) {
        super.onNewIntent(intent)
        setIntent(intent)
        hideSoftKeyboard()
    }

    private fun hideSoftKeyboard() {
        try {
            val imm = getSystemService(Context.INPUT_METHOD_SERVICE) as? android.view.inputmethod.InputMethodManager
            val token = currentFocus?.windowToken ?: window.decorView.windowToken
            imm?.hideSoftInputFromWindow(token, 0)
            currentFocus?.clearFocus()
        } catch (_: Exception) {}
    }

    override fun dispatchTouchEvent(ev: android.view.MotionEvent): Boolean {
        if (ev.action == android.view.MotionEvent.ACTION_DOWN) {
            val focused = currentFocus
            if (focused is EditText) {
                val outRect = android.graphics.Rect()
                focused.getGlobalVisibleRect(outRect)
                if (!outRect.contains(ev.rawX.toInt(), ev.rawY.toInt())) {
                    hideSoftKeyboard()
                }
            }
        }
        return super.dispatchTouchEvent(ev)
    }

    override fun onConfigurationChanged(newConfig: android.content.res.Configuration) {
        super.onConfigurationChanged(newConfig)
        applyResponsiveLayout()
    }

    /**
     * Hệ thống Adaptive Layout chuẩn Material Design 3 (Google Classroom / Google Meet):
     * - Compact (< 600dp): Hiện Bottom Navigation Bar, ẩn Navigation Rail trái, xếp chồng 1 cột.
     * - Medium / Expanded (>= 600dp Tablet): Hiện Navigation Rail trái 84dp, ẩn Bottom Bar, chia cột cân đối.
     */
    private fun applyResponsiveLayout() {
        val dm = resources.displayMetrics
        val density = dm.density
        val screenWidthDp = dm.widthPixels / density
        fun dp(v: Int): Int = (v * density).toInt()

        val isCompact = screenWidthDp < 600f
        val isMedium = screenWidthDp in 600f..899.9f
        val isExpanded = screenWidthDp >= 900f

        // 1. Adaptive Navigation: NavigationRail trên Tablet (>=600dp), BottomNavBar trên màn hình nhỏ (<600dp)
        if (::navRailBar.isInitialized && ::bottomNavBar.isInitialized) {
            navRailBar.visibility = if (isCompact) View.GONE else View.VISIBLE
            navRailDivider.visibility = if (isCompact) View.GONE else View.VISIBLE
            bottomNavBar.visibility = if (isCompact) View.VISIBLE else View.GONE
        }

        // 2. Màn hình 1 (Join Class): 2 Cột trên Tablet, 1 Cột trên Compact
        if (::discoveryGridContainer.isInitialized) {
            val outerPad = when {
                isExpanded -> dp(24)
                isMedium -> dp(18)
                else -> dp(14)
            }
            discoveryInnerRoot.setPadding(outerPad, dp(14), outerPad, dp(18))

            if (isCompact) {
                discoveryGridContainer.orientation = LinearLayout.VERTICAL
                colStudentIdentity.layoutParams = LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.MATCH_PARENT,
                    ViewGroup.LayoutParams.WRAP_CONTENT
                ).apply { bottomMargin = dp(12) }
                colLanRooms.layoutParams = LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.MATCH_PARENT,
                    ViewGroup.LayoutParams.WRAP_CONTENT
                )
            } else {
                discoveryGridContainer.orientation = LinearLayout.HORIZONTAL
                val leftWeight = if (isExpanded) 4.0f else 4.5f
                val rightWeight = if (isExpanded) 8.0f else 7.5f
                colStudentIdentity.layoutParams = LinearLayout.LayoutParams(
                    0,
                    ViewGroup.LayoutParams.WRAP_CONTENT,
                    leftWeight
                ).apply { marginEnd = dp(16) }
                colLanRooms.layoutParams = LinearLayout.LayoutParams(
                    0,
                    ViewGroup.LayoutParams.WRAP_CONTENT,
                    rightWeight
                )
            }
        }

        // 3. Tab 0 (Lớp học Split-View): 70:30 full-height trên Tablet, xếp dọc trên Compact
        if (::lectureSplitContainer.isInitialized) {
            if (isCompact) {
                lectureSplitContainer.orientation = LinearLayout.VERTICAL
                colLectureDocument.layoutParams = LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.MATCH_PARENT,
                    0,
                    if (isLessonSidebarVisible) 6.0f else 10.0f
                ).apply { bottomMargin = if (isLessonSidebarVisible) dp(10) else 0 }
                colLectureSidebar.layoutParams = LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.MATCH_PARENT,
                    0,
                    4.0f
                )
            } else {
                lectureSplitContainer.orientation = LinearLayout.HORIZONTAL
                if (isLessonSidebarVisible) {
                    val docWeight = if (isExpanded) 7.4f else 6.6f
                    val sideWeight = if (isExpanded) 3.6f else 4.4f
                    colLectureDocument.layoutParams = LinearLayout.LayoutParams(
                        0,
                        ViewGroup.LayoutParams.MATCH_PARENT,
                        docWeight
                    ).apply { marginEnd = dp(12) }
                    colLectureSidebar.layoutParams = LinearLayout.LayoutParams(
                        0,
                        ViewGroup.LayoutParams.MATCH_PARENT,
                        sideWeight
                    )
                } else {
                    colLectureDocument.layoutParams = LinearLayout.LayoutParams(
                        0,
                        ViewGroup.LayoutParams.MATCH_PARENT,
                        11.0f
                    )
                }
            }
            // [MYTHWARE_LITE] Đảm bảo phần Sổ nháp nhanh luôn ẩn, nhường 100% chiều rộng cho khung bài giảng
            colLectureSidebar.visibility = View.GONE
            if (::btnToggleLessonSidebar.isInitialized) {
                btnToggleLessonSidebar.visibility = View.GONE
            }
        }

        // 4. Tab 3 (Tài liệu & Phiên học): 2 cột trên Tablet, 1 cột trên Compact
        if (::filesGridContainer.isInitialized) {
            if (isCompact) {
                filesGridContainer.orientation = LinearLayout.VERTICAL
                colReceivedFiles.layoutParams = LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.MATCH_PARENT,
                    ViewGroup.LayoutParams.WRAP_CONTENT
                ).apply { bottomMargin = dp(12) }
                colSessionProfile.layoutParams = LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.MATCH_PARENT,
                    ViewGroup.LayoutParams.WRAP_CONTENT
                )
            } else {
                filesGridContainer.orientation = LinearLayout.HORIZONTAL
                colReceivedFiles.layoutParams = LinearLayout.LayoutParams(
                    0,
                    ViewGroup.LayoutParams.WRAP_CONTENT,
                    6.5f
                ).apply { marginEnd = dp(14) }
                colSessionProfile.layoutParams = LinearLayout.LayoutParams(
                    0,
                    ViewGroup.LayoutParams.WRAP_CONTENT,
                    5.5f
                )
            }
        }

        // 5. Màn hình 4 (Lock Screen / Focus Mode)
        if (::lockContentCard.isInitialized) {
            val lockLp = lockContentCard.layoutParams as? FrameLayout.LayoutParams
            if (lockLp != null) {
                lockLp.width = when {
                    isExpanded -> dp(600)
                    isMedium -> dp(500)
                    else -> ViewGroup.LayoutParams.MATCH_PARENT
                }
                lockLp.gravity = Gravity.CENTER
                lockContentCard.layoutParams = lockLp
            }
        }

        syncStudentIdentityCard()
    }

    private fun initViews() {
        // Screen 1: Join Class UI
        discoveryPanel = findViewById(R.id.discoveryPanel)
        discoveryInnerRoot = findViewById(R.id.discoveryInnerRoot)
        txtStatus = findViewById(R.id.txtStatus)
        progressBar = findViewById(R.id.progressBar)
        btnRefresh = findViewById(R.id.btnRefresh)
        edtStudentName = findViewById(R.id.edtStudentName)
        edtStudentCode = findViewById(R.id.edtStudentCode)
        edtPassword = findViewById(R.id.edtPassword)
        edtJoinClassCode = findViewById(R.id.edtJoinClassCode)
        btnTogglePassword = findViewById(R.id.btnTogglePassword)
        edtTeacherIp = findViewById(R.id.edtTeacherIp)
        chkRemember = findViewById(R.id.chkRemember)
        txtError = findViewById(R.id.txtError)
        btnConnect = findViewById(R.id.btnConnect)
        txtDiscoveryDot = findViewById(R.id.txtDiscoveryDot)
        txtDiscoveryHeader = findViewById(R.id.txtDiscoveryHeader)
        txtJoinAvatarInitials = findViewById(R.id.txtJoinAvatarInitials)

        lanReconnectBanner = findViewById(R.id.lanReconnectBanner)
        txtLanReconnectStatus = findViewById(R.id.txtLanReconnectStatus)
        discoveryGridContainer = findViewById(R.id.discoveryGridContainer)
        colStudentIdentity = findViewById(R.id.colStudentIdentity)
        colLanRooms = findViewById(R.id.colLanRooms)
        btnAdvancedConfig = findViewById(R.id.btnAdvancedConfig)
        btnEditStudentIdentity = findViewById(R.id.btnEditStudentIdentity)
        txtStudentIdentityName = findViewById(R.id.txtStudentIdentityName)
        txtStudentIdentityCode = findViewById(R.id.txtStudentIdentityCode)
        txtSeatBadge = findViewById(R.id.txtSeatBadge)
        txtLanPingBadge = findViewById(R.id.txtLanPingBadge)
        cardDiscoveredClass = findViewById(R.id.cardDiscoveredClass)

        // Screen 2: Connected Top Bar & Navigation Rail
        connectedPanel = findViewById(R.id.connectedPanel)
        txtPageTitle = findViewById(R.id.txtPageTitle)
        txtHeaderLiveBadge = findViewById(R.id.txtHeaderLiveBadge)
        txtConnectedClassInfo = findViewById(R.id.txtConnectedClassInfo)
        txtTeacherDetail = findViewById(R.id.txtTeacherDetail)
        txtStudentDetail = findViewById(R.id.txtStudentDetail)
        txtServerIpDetail = findViewById(R.id.txtServerIpDetail)
        btnDisconnect = findViewById(R.id.btnDisconnect)
        btnRaiseHand = findViewById(R.id.btnRaiseHand)
        btnTopHandReason = findViewById(R.id.btnTopHandReason)

        navRailBar = findViewById(R.id.navRailBar)
        navRailDivider = findViewById(R.id.navRailDivider)
        railItemClass = findViewById(R.id.railItemClass)
        railItemWork = findViewById(R.id.railItemWork)
        railItemChat = findViewById(R.id.railItemChat)
        railItemFiles = findViewById(R.id.railItemFiles)
        pillRailClass = findViewById(R.id.pillRailClass)
        pillRailWork = findViewById(R.id.pillRailWork)
        pillRailChat = findViewById(R.id.pillRailChat)
        pillRailFiles = findViewById(R.id.pillRailFiles)
        iconRailClass = findViewById(R.id.iconRailClass)
        iconRailWork = findViewById(R.id.iconRailWork)
        iconRailChat = findViewById(R.id.iconRailChat)
        iconRailFiles = findViewById(R.id.iconRailFiles)
        labelRailClass = findViewById(R.id.labelRailClass)
        labelRailWork = findViewById(R.id.labelRailWork)
        labelRailChat = findViewById(R.id.labelRailChat)
        labelRailFiles = findViewById(R.id.labelRailFiles)
        badgeRailWork = findViewById(R.id.badgeRailWork)
        badgeRailChat = findViewById(R.id.badgeRailChat)
        badgeRailFiles = findViewById(R.id.badgeRailFiles)

        // Adaptive Bottom Nav Bar (< 600dp)
        bottomNavBar = findViewById(R.id.bottomNavBar)
        btnBottomHome = findViewById(R.id.btnBottomHome)
        btnBottomLecture = findViewById(R.id.btnBottomLecture)
        btnBottomChat = findViewById(R.id.btnBottomChat)
        btnBottomRaise = findViewById(R.id.btnBottomRaise)
        capsuleBottomHome = findViewById(R.id.capsuleBottomHome)
        capsuleBottomLecture = findViewById(R.id.capsuleBottomLecture)
        capsuleBottomChat = findViewById(R.id.capsuleBottomChat)
        capsuleBottomRaise = findViewById(R.id.capsuleBottomRaise)
        iconBottomHome = findViewById(R.id.iconBottomHome)
        labelBottomHome = findViewById(R.id.labelBottomHome)
        iconBottomLecture = findViewById(R.id.iconBottomLecture)
        labelBottomLecture = findViewById(R.id.labelBottomLecture)
        iconBottomChat = findViewById(R.id.iconBottomChat)
        labelBottomChat = findViewById(R.id.labelBottomChat)
        iconBottomRaise = findViewById(R.id.iconBottomRaise)
        labelBottomRaise = findViewById(R.id.labelBottomRaise)

        // Tab 0: Lớp học (Live Stream + Lesson Stage + Quick Note)
        panelLecture = findViewById(R.id.panelLecture)
        layoutNoBroadcast = findViewById(R.id.layoutNoBroadcast)
        layoutLiveBroadcast = findViewById(R.id.layoutLiveBroadcast)
        txtInlineBroadcastTitle = findViewById(R.id.txtInlineBroadcastTitle)
        imgInlineBroadcast = findViewById(R.id.imgInlineBroadcast)
        btnFullscreenBroadcast = findViewById(R.id.btnFullscreenBroadcast)
        messageCard = findViewById(R.id.messageCard)
        txtTeacherMessage = findViewById(R.id.txtTeacherMessage)

        txtHomeGreeting = findViewById(R.id.txtHomeGreeting)
        txtHomeSub = findViewById(R.id.txtHomeSub)
        txtHomeLessonTitle = findViewById(R.id.txtHomeLessonTitle)
        layoutHomeLessonWaiting = findViewById(R.id.layoutHomeLessonWaiting)
        layoutHomeLessonActive = findViewById(R.id.layoutHomeLessonActive)
        txtHomeSubjectBadge = findViewById(R.id.txtHomeSubjectBadge)
        txtHomeStageBadge = findViewById(R.id.txtHomeStageBadge)
        txtHomeBoardHeader = findViewById(R.id.txtHomeBoardHeader)
        txtHomeBoardContent = findViewById(R.id.txtHomeBoardContent)

        lectureSplitContainer = findViewById(R.id.lectureSplitContainer)
        colLectureDocument = findViewById(R.id.colLectureDocument)
        colLectureSidebar = findViewById(R.id.colLectureSidebar)
        btnToggleLessonSidebar = findViewById(R.id.btnToggleLessonSidebar)
        btnZoomOut = findViewById(R.id.btnZoomOut)
        btnZoomIn = findViewById(R.id.btnZoomIn)
        txtZoomLevel = findViewById(R.id.txtZoomLevel)
        quickNoteCanvas = findViewById(R.id.quickNoteCanvas)
        layoutLatestTeacherFileBar = findViewById(R.id.layoutLatestTeacherFileBar)
        txtLatestTeacherFileName = findViewById(R.id.txtLatestTeacherFileName)
        btnPreviewLatestTeacherFile = findViewById(R.id.btnPreviewLatestTeacherFile)
        btnNotePen = findViewById(R.id.btnNotePen)
        btnNoteHighlight = findViewById(R.id.btnNoteHighlight)
        btnNoteEraser = findViewById(R.id.btnNoteEraser)
        btnNoteClear = findViewById(R.id.btnNoteClear)
        edtQuickLessonAnswer = findViewById(R.id.edtQuickLessonAnswer)
        layoutUploadProgress = findViewById(R.id.layoutUploadProgress)
        progressUploadBar = findViewById(R.id.progressUploadBar)
        txtUploadPercent = findViewById(R.id.txtUploadPercent)
        txtUploadStatusLabel = findViewById(R.id.txtUploadStatusLabel)
        btnAttachQuickFile = findViewById(R.id.btnAttachQuickFile)
        btnQuickSubmitWork = findViewById(R.id.btnQuickSubmitWork)

        // Tab 1: Bài tập & Kiểm tra (Classwork Hub)
        panelSoon = findViewById(R.id.panelSoon)
        chipWorkQuiz = findViewById(R.id.chipWorkQuiz)
        chipWorkHomework = findViewById(R.id.chipWorkHomework)
        chipWorkSurvey = findViewById(R.id.chipWorkSurvey)
        chipWorkResults = findViewById(R.id.chipWorkResults)
        txtSoonTitle = findViewById(R.id.txtSoonTitle)
        iconSoonWrapper = findViewById(R.id.iconSoonWrapper)
        txtSoonSubtitle = findViewById(R.id.txtSoonSubtitle)
        layoutInteractiveFeature = findViewById(R.id.layoutInteractiveFeature)

        // Tab 2: Thảo luận (Full-Height Google Chat)
        panelChat = findViewById(R.id.panelChat)
        txtChatChannelBadge = findViewById(R.id.txtChatChannelBadge)
        scrollChat = findViewById(R.id.scrollChat)
        chatContainer = findViewById(R.id.chatContainer)
        edtChatMessage = findViewById(R.id.edtChatMessage)
        btnSendChat = findViewById(R.id.btnSendChat)
        chatTabBar = findViewById(R.id.chatTabBar)

        // Tab 3: Tài liệu & Phiên học
        panelFilesProfile = findViewById(R.id.panelFilesProfile)
        filesGridContainer = findViewById(R.id.filesGridContainer)
        colReceivedFiles = findViewById(R.id.colReceivedFiles)
        colSessionProfile = findViewById(R.id.colSessionProfile)
        btnSubmitFileFromDocs = findViewById(R.id.btnSubmitFileFromDocs)
        btnRefreshFiles = findViewById(R.id.btnRefreshFiles)
        layoutReceivedFiles = findViewById(R.id.layoutReceivedFiles)
        btnProfileSwitchStudent = findViewById(R.id.btnProfileSwitchStudent)

        // Screen 3: Google Meet Fullscreen Broadcast Overlay + EasyTouch Orb
        broadcastOverlay = findViewById(R.id.broadcastOverlay)
        imgBroadcastScreen = findViewById(R.id.imgBroadcastScreen)
        progressBroadcastLoading = findViewById(R.id.progressBroadcastLoading)
        topBroadcastBadgeBar = findViewById(R.id.topBroadcastBadgeBar)
        txtBroadcastStatus = findViewById(R.id.txtBroadcastStatus)
        btnEasyTouchBroadcast = findViewById(R.id.btnEasyTouchBroadcast)
        txtEasyTouchPodTitle = findViewById(R.id.txtEasyTouchPodTitle)
        btnCollapseEasyTouchStudent = findViewById(R.id.btnCollapseEasyTouchStudent)
        floatingBroadcastDock = findViewById(R.id.floatingBroadcastDock)
        btnInlineBroadcastAudio = findViewById(R.id.btnInlineBroadcastAudio)
        btnFullscreenMuteAudio = findViewById(R.id.btnFullscreenMuteAudio)
        btnFullscreenRaiseHand = findViewById(R.id.btnFullscreenRaiseHand)
        btnFullscreenSnapshot = findViewById(R.id.btnFullscreenSnapshot)
        btnBroadcastVolume = findViewById(R.id.btnBroadcastVolume)
        btnCloseBroadcast = findViewById(R.id.btnCloseBroadcast)

        // Screen 4: Focus Lock Overlay
        lockOverlay = findViewById(R.id.lockOverlay)
        txtLockTitle = findViewById(R.id.txtLockTitle)
        txtLockMessage = findViewById(R.id.txtLockMessage)
        lockContentCard = findViewById(R.id.lockContentCard)
        imgLockIllustration = findViewById(R.id.imgLockIllustration)
        txtLockLessonName = findViewById(R.id.txtLockLessonName)
        txtLockTimer = findViewById(R.id.txtLockTimer)

        // Load saved preferences
        val prefs = getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)
        val remember = prefs.getBoolean(KEY_REMEMBER, true)
        chkRemember.isChecked = remember

        val lastIp = prefs.getString(KEY_LAST_IP, "")
        val devTag = getDeviceShortTag()
        val defaultDeviceCode = "AND-$devTag"
        val defaultDeviceName = "Máy HS ($devTag)"
        val hasSavedCode = prefs.contains(KEY_STUDENT_CODE)
        val rawStudentName = prefs.getString(KEY_STUDENT_NAME, "") ?: ""
        val rawStudentCode = if (hasSavedCode) {
            prefs.getString(KEY_STUDENT_CODE, defaultDeviceCode) ?: defaultDeviceCode
        } else {
            defaultDeviceCode
        }
        val lastStudentCode = sanitizeStudentCode(rawStudentCode)
        val lastPassword = prefs.getString(KEY_PASSWORD, "123456")
        val resolvedInitName = resolveOfficialStudentName(
            lastStudentCode,
            rawStudentName.ifBlank { if (!hasSavedCode) defaultDeviceName else "" }
        )

        if (remember) {
            if (!lastIp.isNullOrBlank()) edtTeacherIp.setText(lastIp)
            edtStudentCode.setText(lastStudentCode)
            edtStudentName.setText(resolvedInitName)
            if (!lastPassword.isNullOrBlank()) edtPassword.setText(lastPassword)
            if (rawStudentName != resolvedInitName || rawStudentCode != lastStudentCode) {
                savePreferences(resolvedInitName, lastStudentCode, lastPassword ?: "123456", lastIp ?: "", true)
            }
        }
        syncStudentIdentityCard()
        refreshReceivedFilesPanel()
    }

    private fun setupListeners() {
        btnTogglePassword.setOnClickListener {
            isPasswordVisible = !isPasswordVisible
            if (isPasswordVisible) {
                edtPassword.transformationMethod = HideReturnsTransformationMethod.getInstance()
                btnTogglePassword.setImageResource(R.drawable.ic_visibility_off)
            } else {
                edtPassword.transformationMethod = PasswordTransformationMethod.getInstance()
                btnTogglePassword.setImageResource(R.drawable.ic_visibility)
            }
            edtPassword.setSelection(edtPassword.text.length)
        }

        btnRefresh.setOnClickListener {
            hideSoftKeyboard()
            startAutoDiscovery()
        }

        edtJoinClassCode.imeOptions = android.view.inputmethod.EditorInfo.IME_ACTION_GO or
            android.view.inputmethod.EditorInfo.IME_FLAG_NO_EXTRACT_UI

        edtJoinClassCode.setOnEditorActionListener { _, _, _ ->
            hideSoftKeyboard()
            btnConnect.performClick()
            true
        }

        edtChatMessage.setOnEditorActionListener { _, _, _ ->
            sendChatMessage()
            true
        }

        btnConnect.setOnClickListener {
            hideSoftKeyboard()
            txtError.visibility = View.GONE
            val studentCode = edtStudentCode.text.toString().trim().ifBlank { "10A1-002" }
            val password = edtPassword.text.toString().trim().ifBlank { "123456" }
            val classCodeInput = edtJoinClassCode.text.toString().trim()
            var studentName = edtStudentName.text.toString().trim()
            var ip = edtTeacherIp.text.toString().trim()

            /* // [MYTHWARE_LITE] Bỏ bắt buộc nhập mật khẩu và mã lớp trừ khi Giáo viên bật khóa PIN phòng học */
            if (discoveredRequirePin && classCodeInput.isBlank()) {
                showError("🔒 Lớp học này đang bật khóa PIN. Vui lòng nhập Mã PIN trên bảng Giáo viên!")
                edtJoinClassCode.requestFocus()
                return@setOnClickListener
            }

            studentName = resolveOfficialStudentName(studentCode, studentName)
            edtStudentCode.setText(studentCode)
            edtStudentName.setText(studentName)

            if (ip.isBlank()) {
                ip = discoveredTeacherIp ?: "10.0.2.2"
                edtTeacherIp.setText(ip)
            }

            var tcpPort = discoveredTeacherPort
            if (ip.contains(":")) {
                val parts = ip.split(":")
                ip = parts[0]
                tcpPort = parts[1].toIntOrNull() ?: discoveredTeacherPort
            }
            ignoredTeacherIps.remove(ip)

            if (chkRemember.isChecked) {
                savePreferences(studentName, studentCode, password, ip, true)
            } else {
                savePreferences("", "", "", "", false)
            }

            connectToTeacher(ip, tcpPort, studentName, studentCode, password, classCodeInput)
        }

        btnDisconnect.setOnClickListener {
            isUserInitiatedDisconnect = true
            disconnectFromTeacher("Đã rời phòng học")
        }

        // Hộp thoại Modal Cấu hình IP Máy chủ Giáo viên (Không xổ dài làm vỡ trang chính)
        btnAdvancedConfig.setOnClickListener {
            hideSoftKeyboard()
            showServerIpConfigDialog()
        }

        btnEditStudentIdentity.setOnClickListener {
            hideSoftKeyboard()
            showSwitchAccountDialog(autoReconnectIfConnected = false)
        }

        btnProfileSwitchStudent.setOnClickListener {
            showSwitchAccountDialog(autoReconnectIfConnected = true)
        }

        txtStudentDetail.setOnClickListener {
            showSwitchAccountDialog(autoReconnectIfConnected = true)
        }

        findViewById<View?>(R.id.boxDiscoveredRoomInfo)?.setOnClickListener {
            if (!discoveredTeacherIp.isNullOrBlank()) {
                btnConnect.performClick()
            } else {
                startAutoDiscovery()
            }
        }

        // Fullscreen Broadcast EasyTouch Controls (Chạm màn hình bên ngoài để thu gọn hộp về nút tròn EasyTouch)
        imgBroadcastScreen.setOnClickListener {
            if (floatingBroadcastDock.visibility == View.VISIBLE) {
                collapseStudentEasyTouch()
            }
        }
        broadcastOverlay.setOnClickListener {
            if (floatingBroadcastDock.visibility == View.VISIBLE) {
                collapseStudentEasyTouch()
            }
        }
        btnCollapseEasyTouchStudent.setOnClickListener {
            collapseStudentEasyTouch()
        }
        setupStudentEasyTouch()

        btnInlineBroadcastAudio.setOnClickListener {
            toggleBroadcastAudioMute()
        }

        btnFullscreenMuteAudio.setOnClickListener {
            scheduleBroadcastControlsAutoHide()
            toggleBroadcastAudioMute()
        }

        btnBroadcastVolume.setOnClickListener {
            scheduleBroadcastControlsAutoHide()
            val audioMgr = getSystemService(Context.AUDIO_SERVICE) as? android.media.AudioManager
            audioMgr?.adjustSuggestedStreamVolume(
                android.media.AudioManager.ADJUST_SAME,
                android.media.AudioManager.STREAM_MUSIC,
                android.media.AudioManager.FLAG_SHOW_UI
            )
        }

        btnFullscreenSnapshot.setOnClickListener {
            scheduleBroadcastControlsAutoHide()
            saveLectureSnapshotToStorage()
        }

        // Split-Screen Lesson Viewer Controls
        btnToggleLessonSidebar.setOnClickListener {
            isLessonSidebarVisible = !isLessonSidebarVisible
            btnToggleLessonSidebar.text = if (isLessonSidebarVisible) "Ẩn nháp" else "Mở nháp"
            applyResponsiveLayout()
        }

        btnZoomOut.setOnClickListener {
            currentZoomPercent = (currentZoomPercent - 15).coerceAtLeast(70)
            applyDocumentZoom()
        }

        btnZoomIn.setOnClickListener {
            currentZoomPercent = (currentZoomPercent + 15).coerceAtMost(200)
            applyDocumentZoom()
        }

        imgInlineBroadcast.setOnClickListener {
            openFullscreenBroadcastOverlay()
        }

        // Quick Note Canvas Tools
        fun styleNoteToolButtons(activeBtn: Button) {
            listOf(btnNotePen, btnNoteHighlight, btnNoteEraser).forEach { b ->
                val isSel = b === activeBtn
                b.backgroundTintList = ColorStateList.valueOf(if (isSel) 0xFF1A73E8.toInt() else 0xFFF1F5F9.toInt())
                b.setTextColor(if (isSel) 0xFFFFFFFF.toInt() else 0xFF334155.toInt())
            }
        }

        btnNotePen.setOnClickListener {
            quickNoteCanvas.setToolMode(com.qasmartclass.student.views.QuickNoteCanvasView.ToolMode.PEN)
            styleNoteToolButtons(btnNotePen)
        }
        btnNoteHighlight.setOnClickListener {
            quickNoteCanvas.setToolMode(com.qasmartclass.student.views.QuickNoteCanvasView.ToolMode.HIGHLIGHTER)
            styleNoteToolButtons(btnNoteHighlight)
        }
        btnNoteEraser.setOnClickListener {
            quickNoteCanvas.setToolMode(com.qasmartclass.student.views.QuickNoteCanvasView.ToolMode.ERASER)
            styleNoteToolButtons(btnNoteEraser)
        }
        btnNoteClear.setOnClickListener {
            quickNoteCanvas.clearCanvas()
            Toast.makeText(this, "Đã xóa trang nháp", Toast.LENGTH_SHORT).show()
        }
        btnAttachQuickFile.setOnClickListener {
            launchStudentFilePicker()
        }
        btnQuickSubmitWork.setOnClickListener {
            performQuickNoteOrHomeworkUpload()
        }

        // Navigation Rail (Tablet) & Adaptive Bottom Bar (Compact) -> 4 Không gian thật
        railItemClass.setOnClickListener { switchTab(0) }
        railItemWork.setOnClickListener { switchTab(1) }
        railItemChat.setOnClickListener { switchTab(2) }
        railItemFiles.setOnClickListener { switchTab(3) }

        btnBottomHome.setOnClickListener { switchTab(0) }
        btnBottomLecture.setOnClickListener { switchTab(1) }
        btnBottomChat.setOnClickListener { switchTab(2) }
        btnBottomRaise.setOnClickListener { switchTab(3) }

        // Classwork Filter Chips (Trắc nghiệm | Bài tập | Khảo sát | Điểm số)
        chipWorkQuiz.setOnClickListener { switchWorkSubTab("QUIZ") }
        chipWorkHomework.setOnClickListener { switchWorkSubTab("HOMEWORK") }
        chipWorkSurvey.setOnClickListener { switchWorkSubTab("SURVEY") }
        chipWorkResults.setOnClickListener { switchWorkSubTab("RESULTS") }

        // Tài liệu lớp học & Nộp tệp bài làm
        btnSubmitFileFromDocs.setOnClickListener {
            launchStudentFilePicker()
        }
        btnRefreshFiles.setOnClickListener {
            refreshReceivedFilesPanel()
            Toast.makeText(this, "Đã làm mới danh sách tài liệu", Toast.LENGTH_SHORT).show()
        }

        // Hand Raise Controls (Top App Bar 1-chạm + Chọn lý do + Fullscreen Pill)
        btnRaiseHand.setOnClickListener { toggleRaiseHand() }
        btnFullscreenRaiseHand.setOnClickListener {
            scheduleBroadcastControlsAutoHide()
            toggleRaiseHand()
        }
        btnTopHandReason.setOnClickListener { showHandRaiseReasonDialog() }

        // Chat Send Listener
        btnSendChat.setOnClickListener { sendChatMessage() }
        switchChatChannel("TEACHER")

        // Fullscreen Broadcast Toggle
        btnFullscreenBroadcast.setOnClickListener {
            openFullscreenBroadcastOverlay()
        }
        btnCloseBroadcast.setOnClickListener {
            minimizeFullscreenBroadcastOverlay()
        }
        findViewById<View?>(R.id.btnTopMinimizeBroadcast)?.setOnClickListener {
            minimizeFullscreenBroadcastOverlay()
        }

        // Top Header Leave Class & Teacher Notice Quick Feedback
        findViewById<View?>(R.id.btnHeaderLogout)?.setOnClickListener { showExitConfirmation() }
        findViewById<View?>(R.id.btnLectureSnapshot)?.setOnClickListener {
            saveLectureSnapshotToStorage()
        }
        findViewById<View?>(R.id.btnNoticeUnderstood)?.setOnClickListener {
            val sCode = currentStudentCode.ifBlank { "HS001" }
            sendRawTcpLine("CHAT|$sCode|TEACHER|👍 Em đã hiểu nội dung vừa giảng ạ!")
            messageCard.visibility = View.GONE
            Toast.makeText(this, "Đã gửi phản hồi 'Đã hiểu bài' tới Giáo viên", Toast.LENGTH_SHORT).show()
        }
        findViewById<View?>(R.id.btnNoticeReexplain)?.setOnClickListener {
            selectHandRaiseReason("Cần giảng lại")
            if (!isHandRaised) toggleRaiseHand()
            messageCard.visibility = View.GONE
        }

        // Back gesture & Hardware Back
        onBackPressedDispatcher.addCallback(this, object : OnBackPressedCallback(true) {
            override fun handleOnBackPressed() {
                if (broadcastOverlay.visibility == View.VISIBLE) {
                    // Khi Giáo viên đang khóa Fullscreen (mặc định), chặn hoàn toàn nút Back
                    if (!isForceBroadcastMode) {
                        minimizeFullscreenBroadcastOverlay()
                    }
                } else if (connectedPanel.visibility == View.VISIBLE && currentTab != 0) {
                    switchTab(0)
                } else if (connectedPanel.visibility == View.VISIBLE) {
                    showExitConfirmation()
                } else {
                    isEnabled = false
                    onBackPressedDispatcher.onBackPressed()
                }
            }
        })
    }

    private fun dpPx(v: Int): Int = (v * resources.displayMetrics.density).toInt()

    @android.annotation.SuppressLint("ClickableViewAccessibility")
    private fun setupStudentEasyTouch() {
        val touchSlop = android.view.ViewConfiguration.get(this).scaledTouchSlop
        var downRawX = 0f
        var downRawY = 0f
        var startViewX = 0f
        var startViewY = 0f
        var isDragging = false

        btnEasyTouchBroadcast.setOnTouchListener { v, event ->
            val parent = broadcastOverlay
            when (event.actionMasked) {
                android.view.MotionEvent.ACTION_DOWN -> {
                    v.animate().cancel()
                    v.alpha = 1.0f
                    downRawX = event.rawX
                    downRawY = event.rawY
                    startViewX = v.x
                    startViewY = v.y
                    isDragging = false
                    true
                }
                android.view.MotionEvent.ACTION_MOVE -> {
                    val dx = event.rawX - downRawX
                    val dy = event.rawY - downRawY
                    if (!isDragging && kotlin.math.hypot(dx.toDouble(), dy.toDouble()) > touchSlop) {
                        isDragging = true
                        if (floatingBroadcastDock.visibility == View.VISIBLE) {
                            collapseStudentEasyTouch(immediate = true)
                        }
                    }
                    if (isDragging && parent.width > 0 && parent.height > 0) {
                        val margin = dpPx(10).toFloat()
                        val maxX = (parent.width.toFloat() - v.width.toFloat() - margin).coerceAtLeast(margin)
                        val maxY = (parent.height.toFloat() - v.height.toFloat() - margin).coerceAtLeast(margin)
                        v.x = (startViewX + dx).coerceIn(margin, maxX)
                        v.y = (startViewY + dy).coerceIn(margin, maxY)
                        easyTouchCustomPositioned = true
                    }
                    true
                }
                android.view.MotionEvent.ACTION_UP, android.view.MotionEvent.ACTION_CANCEL -> {
                    if (!isDragging) {
                        toggleBroadcastControlsVisibility()
                    } else if (parent.width > 0) {
                        // Tự động hít gọn sát mép trái hoặc mép phải màn hình giống AssistiveTouch / EasyTouch
                        val margin = dpPx(12).toFloat()
                        val snapX = if (v.x + v.width / 2f < parent.width / 2f) {
                            margin
                        } else {
                            (parent.width.toFloat() - v.width.toFloat() - margin).coerceAtLeast(margin)
                        }
                        v.animate().x(snapX).alpha(0.68f).setDuration(180).start()
                    }
                    true
                }
                else -> false
            }
        }
    }

    private fun positionEasyTouchPodNearBubble() {
        broadcastOverlay.post {
            val parentW = broadcastOverlay.width.toFloat()
            val parentH = broadcastOverlay.height.toFloat()
            if (parentW <= 0f || parentH <= 0f) return@post

            val podW = (if (floatingBroadcastDock.width > 0) floatingBroadcastDock.width else dpPx(256)).toFloat()
            val podH = (if (floatingBroadcastDock.height > 0) floatingBroadcastDock.height else dpPx(176)).toFloat()
            val gap = dpPx(10).toFloat()
            val margin = dpPx(12).toFloat()

            val bx = btnEasyTouchBroadcast.x
            val by = btnEasyTouchBroadcast.y
            val bw = btnEasyTouchBroadcast.width.coerceAtLeast(dpPx(52)).toFloat()

            val targetX = if (bx + bw / 2f > parentW / 2f) {
                (bx - podW - gap).coerceIn(margin, (parentW - podW - margin).coerceAtLeast(margin))
            } else {
                (bx + bw + gap).coerceIn(margin, (parentW - podW - margin).coerceAtLeast(margin))
            }
            val targetY = by.coerceIn(margin, (parentH - podH - margin).coerceAtLeast(margin))

            floatingBroadcastDock.x = targetX
            floatingBroadcastDock.y = targetY
        }
    }

    private fun setImmersiveFullscreen(enable: Boolean) {
        try {
            val controller = androidx.core.view.WindowCompat.getInsetsController(window, window.decorView)
            if (enable) {
                window.addFlags(android.view.WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)
                controller.systemBarsBehavior =
                    androidx.core.view.WindowInsetsControllerCompat.BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE
                controller.hide(androidx.core.view.WindowInsetsCompat.Type.systemBars())
            } else {
                window.clearFlags(android.view.WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)
                controller.show(androidx.core.view.WindowInsetsCompat.Type.systemBars())
            }
        } catch (e: Exception) {
            Log.d(TAG, "setImmersiveFullscreen error: ${e.message}")
        }
    }

    private fun applyBroadcastLockState(isForce: Boolean, notifyToast: Boolean = false) {
        val prevForce = isForceBroadcastMode
        isForceBroadcastMode = isForce

        runOnUiThread {
            topBroadcastBadgeBar.visibility = View.GONE
            findViewById<View?>(R.id.btnTopMinimizeBroadcast)?.visibility = View.GONE
            btnCloseBroadcast.visibility = if (!isForce) View.VISIBLE else View.GONE

            if (isScreenBroadcastActive && isForce) {
                if (broadcastOverlay.visibility != View.VISIBLE) {
                    if (imgBroadcastScreen.drawable == null && imgInlineBroadcast.drawable != null) {
                        imgBroadcastScreen.setImageDrawable(imgInlineBroadcast.drawable)
                    }
                    broadcastOverlay.visibility = View.VISIBLE
                }
                setImmersiveFullscreen(true)
            } else if (!isForce && lockOverlay.visibility != View.VISIBLE) {
                setImmersiveFullscreen(false)
            }

            findViewById<View?>(R.id.dotEasyTouchLive)?.backgroundTintList =
                android.content.res.ColorStateList.valueOf(if (isForce) 0xFF22C55E.toInt() else 0xFF38BDF8.toInt())

            if (notifyToast && prevForce != isForce) {
                val msg = if (isForce) {
                    "🔒 Giáo viên đã khóa chế độ xem toàn màn hình"
                } else {
                    "🔓 Giáo viên cho phép thu nhỏ / bấm ra ngoài"
                }
                Toast.makeText(this, msg, Toast.LENGTH_SHORT).show()
            }
        }
    }

    private fun openFullscreenBroadcastOverlay() {
        if (imgBroadcastScreen.drawable == null && imgInlineBroadcast.drawable != null) {
            imgBroadcastScreen.setImageDrawable(imgInlineBroadcast.drawable)
        }
        topBroadcastBadgeBar.visibility = View.GONE
        findViewById<View?>(R.id.btnTopMinimizeBroadcast)?.visibility = View.GONE
        btnCloseBroadcast.visibility = if (!isForceBroadcastMode) View.VISIBLE else View.GONE
        broadcastOverlay.visibility = View.VISIBLE
        setImmersiveFullscreen(true)
        collapseStudentEasyTouch(immediate = true)
    }

    private fun minimizeFullscreenBroadcastOverlay() {
        if (isForceBroadcastMode) return
        collapseStudentEasyTouch(immediate = true)
        setImmersiveFullscreen(false)
        imgBroadcastScreen.drawable?.let { imgInlineBroadcast.setImageDrawable(it) }
        broadcastOverlay.visibility = View.GONE
        if (currentTab != 0) switchTab(0)
    }

    private fun collapseStudentEasyTouch(immediate: Boolean = false) {
        broadcastUiHideJob?.cancel()
        floatingBroadcastDock.animate().cancel()
        topBroadcastBadgeBar.visibility = View.GONE
        if (immediate) {
            floatingBroadcastDock.visibility = View.GONE
            floatingBroadcastDock.alpha = 0f
            btnEasyTouchBroadcast.alpha = 0.68f
        } else {
            floatingBroadcastDock.animate().alpha(0f).setDuration(160).withEndAction {
                floatingBroadcastDock.visibility = View.GONE
                btnEasyTouchBroadcast.animate().alpha(0.68f).setDuration(180).start()
            }.start()
        }
    }

    private fun toggleBroadcastControlsVisibility() {
        val visibleNow = floatingBroadcastDock.visibility == View.VISIBLE
        if (visibleNow) {
            collapseStudentEasyTouch()
        } else {
            showBroadcastControlsAndScheduleHide()
        }
    }

    private fun showBroadcastControlsAndScheduleHide() {
        broadcastUiHideJob?.cancel()
        floatingBroadcastDock.animate().cancel()
        topBroadcastBadgeBar.visibility = View.GONE
        findViewById<View?>(R.id.btnTopMinimizeBroadcast)?.visibility = View.GONE
        btnCloseBroadcast.visibility = if (!isForceBroadcastMode) View.VISIBLE else View.GONE
        btnEasyTouchBroadcast.alpha = 1.0f

        floatingBroadcastDock.alpha = 0f
        floatingBroadcastDock.visibility = View.VISIBLE
        positionEasyTouchPodNearBubble()
        floatingBroadcastDock.animate().alpha(1f).setDuration(160).start()
        scheduleBroadcastControlsAutoHide()
    }

    private fun scheduleBroadcastControlsAutoHide() {
        broadcastUiHideJob?.cancel()
        broadcastUiHideJob = lifecycleScope.launch {
            delay(4500)
            if (floatingBroadcastDock.visibility == View.VISIBLE) {
                collapseStudentEasyTouch()
            }
        }
    }

    /**
     * Hộp thoại Modal Cấu hình Máy chủ Giáo viên (IP / Cổng LAN) chuẩn Material —
     * gọn sạch, không đẩy xô lệch giao diện màn hình chính.
     */
    private fun showServerIpConfigDialog() {
        val density = resources.displayMetrics.density
        fun dp(v: Int): Int = (v * density).toInt()

        val container = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            setPadding(dp(22), dp(14), dp(22), dp(8))
        }

        container.addView(TextView(this).apply {
            text = "Thông thường thiết bị tự động tìm thấy lớp học qua mạng LAN. Chỉ nhập IP thủ công khi mạng trường học chặn quảng bá khác tầng Wi-Fi:"
            textSize = 13.5f
            setTextColor(0xFF475569.toInt())
            setPadding(0, 0, 0, dp(12))
        })

        val inputIp = EditText(this).apply {
            hint = "Địa chỉ IP máy Giáo viên (VD: 192.168.1.10 hoặc 10.0.2.2)"
            setText(edtTeacherIp.text.toString().trim().ifBlank { discoveredTeacherIp ?: "" })
            textSize = 15.5f
            setSingleLine(true)
            setBackgroundResource(R.drawable.bg_m3_pin_input)
            setPadding(dp(14), dp(12), dp(14), dp(12))
        }
        container.addView(inputIp)

        val quickRow = LinearLayout(this).apply {
            orientation = LinearLayout.HORIZONTAL
            layoutParams = LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT,
                LinearLayout.LayoutParams.WRAP_CONTENT
            ).apply { topMargin = dp(10) }
        }
        val btnAutoLan = Button(this).apply {
            text = "Tự động dò LAN"
            isAllCaps = false
            textSize = 12.5f
            setTextColor(0xFF1A73E8.toInt())
            setBackgroundResource(R.drawable.bg_m3_chip_selected)
            setPadding(dp(12), 0, dp(12), 0)
            layoutParams = LinearLayout.LayoutParams(LinearLayout.LayoutParams.WRAP_CONTENT, dp(36)).apply {
                marginEnd = dp(8)
            }
            setOnClickListener { inputIp.setText("") }
        }
        val btnEmulatorHost = Button(this).apply {
            text = "Máy chủ Emulator (10.0.2.2)"
            isAllCaps = false
            textSize = 12.5f
            setTextColor(0xFF475569.toInt())
            setBackgroundResource(R.drawable.bg_m3_chip_unselected)
            setPadding(dp(12), 0, dp(12), 0)
            layoutParams = LinearLayout.LayoutParams(LinearLayout.LayoutParams.WRAP_CONTENT, dp(36))
            setOnClickListener { inputIp.setText("10.0.2.2") }
        }
        quickRow.addView(btnAutoLan)
        quickRow.addView(btnEmulatorHost)
        container.addView(quickRow)

        AlertDialog.Builder(this)
            .setTitle("Cấu hình Máy chủ Giáo viên")
            .setView(container)
            .setPositiveButton("Lưu & Kiểm tra") { _, _ ->
                val typedIp = inputIp.text.toString().trim()
                edtTeacherIp.setText(typedIp)
                val name = edtStudentName.text.toString().trim()
                val code = edtStudentCode.text.toString().trim()
                val pass = edtPassword.text.toString().trim().ifBlank { "123456" }
                savePreferences(name, code, pass, typedIp, chkRemember.isChecked)
                if (typedIp.isNotBlank()) {
                    discoveredTeacherIp = typedIp.substringBefore(":")
                    txtDiscoveryHeader.text = "MÁY CHỦ ĐÃ CHỈ ĐỊNH"
                    txtStatus.text = "Máy chủ Giáo viên: $typedIp — Bấm 'Vào lớp học' để kết nối"
                } else {
                    startAutoDiscovery()
                }
            }
            .setNegativeButton("Đóng", null)
            .show()
    }

    private fun showHandRaiseReasonDialog() {
        val reasons = arrayOf(
            "✋ Phát biểu xây dựng bài",
            "❓ Đặt câu hỏi cho Giáo viên",
            "🆘 Cần hỗ trợ kỹ thuật / bài tập"
        )
        val values = listOf("Phát biểu", "Đặt câu hỏi", "Trợ giúp")
        val checkedIdx = values.indexOf(currentHandRaiseReason).coerceAtLeast(0)
        AlertDialog.Builder(this)
            .setTitle("Chọn mục đích Giơ tay")
            .setSingleChoiceItems(reasons, checkedIdx) { dialog, which ->
                val picked = values.getOrElse(which) { "Phát biểu" }
                selectHandRaiseReason(picked)
                if (!isHandRaised) {
                    toggleRaiseHand()
                }
                dialog.dismiss()
            }
            .setNegativeButton("Đóng", null)
            .show()
    }

    /**
     * Chụp và lưu khung hình bài giảng thực tế vào bộ nhớ thiết bị (Downloads/QASmartClass)
     * và hiển thị ngay trong mục Tài liệu lớp học.
     */
    private fun saveLectureSnapshotToStorage() {
        val bmp = ((imgBroadcastScreen.drawable as? android.graphics.drawable.BitmapDrawable)?.bitmap
            ?: (imgInlineBroadcast.drawable as? android.graphics.drawable.BitmapDrawable)?.bitmap)

        if (bmp == null || bmp.isRecycled) {
            Toast.makeText(this, "Hiện chưa có khung hình chiếu trực tiếp để chụp lưu.", Toast.LENGTH_SHORT).show()
            return
        }

        val copyBmp = try {
            bmp.copy(android.graphics.Bitmap.Config.ARGB_8888, false)
        } catch (_: Exception) {
            null
        } ?: return

        lifecycleScope.launch(Dispatchers.IO) {
            try {
                val stamp = SimpleDateFormat("yyyyMMdd_HHmmss", Locale.US).format(Date())
                val fileName = "BaiGiang_$stamp.jpg"
                val dir = java.io.File(getExternalFilesDir(android.os.Environment.DIRECTORY_PICTURES), "QASmartClass")
                if (!dir.exists()) dir.mkdirs()
                val outFile = java.io.File(dir, fileName)
                java.io.FileOutputStream(outFile).use { fos ->
                    copyBmp.compress(android.graphics.Bitmap.CompressFormat.JPEG, 95, fos)
                }
                copyBmp.recycle()
                val timeNow = SimpleDateFormat("HH:mm • dd/MM", Locale.getDefault()).format(Date())
                withContext(Dispatchers.Main) {
                    receivedTeacherFiles.add(
                        0,
                        ReceivedFileItem(
                            fileName = fileName,
                            urlOrPath = outFile.absolutePath,
                            timeText = "Ảnh chụp bài giảng lúc $timeNow",
                            isLocalSnapshot = true
                        )
                    )
                    refreshReceivedFilesPanel()
                    Toast.makeText(this@MainActivity, "📸 Đã lưu ảnh bài giảng ($fileName) vào mục Tài liệu", Toast.LENGTH_SHORT).show()
                }
            } catch (e: Exception) {
                withContext(Dispatchers.Main) {
                    Toast.makeText(this@MainActivity, "Không thể lưu ảnh chụp: ${e.message}", Toast.LENGTH_SHORT).show()
                }
            }
        }
    }

    private fun resolveMimeTypeForFile(fileName: String): String {
        val ext = fileName.substringAfterLast('.', "").lowercase(Locale.ROOT)
        if (ext.isNotBlank()) {
            android.webkit.MimeTypeMap.getSingleton().getMimeTypeFromExtension(ext)?.let { return it }
        }
        return when (ext) {
            "pdf" -> "application/pdf"
            "doc" -> "application/msword"
            "docx" -> "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            "xls" -> "application/vnd.ms-excel"
            "xlsx" -> "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            "ppt" -> "application/vnd.ms-powerpoint"
            "pptx" -> "application/vnd.openxmlformats-officedocument.presentationml.presentation"
            "jpg", "jpeg" -> "image/jpeg"
            "png" -> "image/png"
            "webp" -> "image/webp"
            "mp4" -> "video/mp4"
            "mp3" -> "audio/mpeg"
            "txt", "csv", "md" -> "text/plain"
            "zip" -> "application/zip"
            else -> "*/*"
        }
    }

    private fun decodeSampledBitmapFromFile(file: java.io.File, maxDimensionPx: Int): android.graphics.Bitmap? {
        if (!file.exists() || file.length() <= 0L) return null
        return try {
            val boundsOpts = BitmapFactory.Options().apply { inJustDecodeBounds = true }
            BitmapFactory.decodeFile(file.absolutePath, boundsOpts)
            val srcW = boundsOpts.outWidth
            val srcH = boundsOpts.outHeight
            if (srcW <= 0 || srcH <= 0) return null

            var sampleSize = 1
            while ((srcW / sampleSize) > maxDimensionPx || (srcH / sampleSize) > maxDimensionPx) {
                sampleSize *= 2
            }
            val decodeOpts = BitmapFactory.Options().apply {
                inSampleSize = sampleSize
                inPreferredConfig = android.graphics.Bitmap.Config.ARGB_8888
            }
            BitmapFactory.decodeFile(file.absolutePath, decodeOpts)
        } catch (_: OutOfMemoryError) {
            try {
                BitmapFactory.decodeFile(file.absolutePath, BitmapFactory.Options().apply {
                    inSampleSize = 4
                    inPreferredConfig = android.graphics.Bitmap.Config.RGB_565
                })
            } catch (_: Throwable) {
                null
            }
        } catch (_: Exception) {
            null
        }
    }

    private fun getAvailableTeacherFiles(maxCount: Int = 16): List<ReceivedFileItem> {
        val list = mutableListOf<ReceivedFileItem>()
        receivedTeacherFiles.filter { !it.isLocalSnapshot }.forEach { item ->
            val f = java.io.File(item.urlOrPath)
            if (f.exists() && !f.name.endsWith(".part")) {
                list.add(item)
            }
        }
        try {
            val dlDir = java.io.File(getExternalFilesDir(android.os.Environment.DIRECTORY_DOWNLOADS), "QASmartClass")
            dlDir.listFiles()
                ?.filter { it.isFile && !it.name.endsWith(".part") }
                ?.sortedByDescending { it.lastModified() }
                ?.take(maxCount)
                ?.forEach { f ->
                    if (list.none { it.urlOrPath == f.absolutePath }) {
                        val sizeText = if (f.length() < 1024 * 1024) {
                            String.format(Locale.US, "%.1f KB", f.length() / 1024.0)
                        } else {
                            String.format(Locale.US, "%.1f MB", f.length() / (1024.0 * 1024.0))
                        }
                        val t = SimpleDateFormat("HH:mm • dd/MM", Locale.getDefault()).format(Date(f.lastModified()))
                        list.add(
                            ReceivedFileItem(
                                fileName = f.name,
                                urlOrPath = f.absolutePath,
                                timeText = "Đã lưu ($sizeText) lúc $t",
                                isLocalSnapshot = false
                            )
                        )
                    }
                }
        } catch (_: Exception) {}
        return list.take(maxCount)
    }

    private fun refreshLatestTeacherFileBanner() {
        if (!::layoutLatestTeacherFileBar.isInitialized) return
        val latest = getAvailableTeacherFiles(1).firstOrNull()
        if (latest != null) {
            val targetFile = java.io.File(latest.urlOrPath)
            layoutLatestTeacherFileBar.visibility = View.VISIBLE
            txtLatestTeacherFileName.text = "📎 Đề/Tài liệu GV: ${latest.fileName}"
            val openAction = View.OnClickListener {
                openLocalReceivedFile(targetFile, latest.fileName)
            }
            layoutLatestTeacherFileBar.setOnClickListener(openAction)
            btnPreviewLatestTeacherFile.setOnClickListener(openAction)
        } else {
            layoutLatestTeacherFileBar.visibility = View.GONE
        }
    }

    private fun openLocalReceivedFile(file: java.io.File, displayTitle: String = file.name) {
        if (isFinishing || isDestroyed) return
        if (!file.exists()) {
            Toast.makeText(this, "Tệp cục bộ không còn tồn tại: ${file.name}", Toast.LENGTH_SHORT).show()
            return
        }
        if (::badgeRailFiles.isInitialized) {
            badgeRailFiles.visibility = View.GONE
        }

        val ext = file.extension.lowercase(Locale.ROOT)
        val density = resources.displayMetrics.density
        val dm = resources.displayMetrics
        fun dp(v: Int): Int = (v * density).toInt()

        try {
            activePreviewDialog?.dismiss()
        } catch (_: Exception) {}
        activePreviewDialog = null

        if (ext in setOf("jpg", "jpeg", "png", "webp", "bmp")) {
            val bmp = decodeSampledBitmapFromFile(file, 2048)
            if (bmp != null) {
                var zoomPercent = 100
                val maxBaseWidth = (dm.widthPixels * 0.72f).toInt().coerceAtLeast(dp(280))
                val maxBaseHeight = (dm.heightPixels * 0.64f).toInt().coerceAtLeast(dp(220))
                val aspect = bmp.width.toFloat() / bmp.height.toFloat().coerceAtLeast(1f)
                var baseW = maxBaseWidth
                var baseH = (baseW / aspect).toInt()
                if (baseH > maxBaseHeight) {
                    baseH = maxBaseHeight
                    baseW = (baseH * aspect).toInt()
                }

                val rootContainer = LinearLayout(this).apply {
                    orientation = LinearLayout.VERTICAL
                    setPadding(dp(12), dp(8), dp(12), dp(4))
                }

                val zoomBar = LinearLayout(this).apply {
                    orientation = LinearLayout.HORIZONTAL
                    gravity = Gravity.CENTER_VERTICAL
                    setPadding(dp(4), 0, dp(4), dp(8))
                }

                val tvHint = TextView(this).apply {
                    text = "💡 Đóng cửa sổ này bạn vẫn bấm '👁 Xem lại' được bất cứ lúc nào"
                    textSize = 12f
                    setTextColor(0xFF475569.toInt())
                    layoutParams = LinearLayout.LayoutParams(0, LinearLayout.LayoutParams.WRAP_CONTENT, 1f)
                }
                zoomBar.addView(tvHint)

                val tvZoomPct = TextView(this).apply {
                    text = "100%"
                    textSize = 12.5f
                    setTypeface(null, Typeface.BOLD)
                    setTextColor(0xFF1A73E8.toInt())
                    gravity = Gravity.CENTER
                    setPadding(dp(8), 0, dp(8), 0)
                }

                val iv = ImageView(this).apply {
                    setImageBitmap(bmp)
                    scaleType = ImageView.ScaleType.FIT_CENTER
                    adjustViewBounds = true
                    layoutParams = FrameLayout.LayoutParams(baseW, baseH, Gravity.CENTER)
                }

                fun applyPreviewZoom(newZoom: Int) {
                    zoomPercent = newZoom.coerceIn(50, 300)
                    tvZoomPct.text = "$zoomPercent%"
                    val targetW = (baseW * zoomPercent) / 100
                    val targetH = (baseH * zoomPercent) / 100
                    iv.layoutParams = FrameLayout.LayoutParams(targetW, targetH, Gravity.CENTER)
                    iv.requestLayout()
                }

                fun makeZoomBtn(label: String, onClick: () -> Unit): Button {
                    return Button(this).apply {
                        text = label
                        isAllCaps = false
                        textSize = 13f
                        setTypeface(null, Typeface.BOLD)
                        setTextColor(0xFF1E293B.toInt())
                        setBackgroundResource(R.drawable.bg_m3_tonal_card)
                        minWidth = 0
                        minimumWidth = 0
                        setPadding(dp(10), 0, dp(10), 0)
                        layoutParams = LinearLayout.LayoutParams(LinearLayout.LayoutParams.WRAP_CONTENT, dp(30))
                        setOnClickListener { onClick() }
                    }
                }

                zoomBar.addView(makeZoomBtn("−") { applyPreviewZoom(zoomPercent - 25) })
                tvZoomPct.setOnClickListener { applyPreviewZoom(100) }
                zoomBar.addView(tvZoomPct)
                zoomBar.addView(makeZoomBtn("+") { applyPreviewZoom(zoomPercent + 25) })
                rootContainer.addView(zoomBar)

                val vScroll = ScrollView(this).apply {
                    layoutParams = LinearLayout.LayoutParams(
                        LinearLayout.LayoutParams.MATCH_PARENT,
                        maxBaseHeight + dp(12)
                    )
                    setBackgroundColor(0xFF0F172A.toInt())
                }
                val hScroll = HorizontalScrollView(this).apply {
                    isFillViewport = true
                }
                val centerFrame = FrameLayout(this).apply {
                    setPadding(dp(6), dp(6), dp(6), dp(6))
                }
                centerFrame.addView(iv)
                hScroll.addView(centerFrame)
                vScroll.addView(hScroll)
                rootContainer.addView(vScroll)

                if (!isFinishing && !isDestroyed) {
                    activePreviewDialog = AlertDialog.Builder(this)
                        .setTitle(displayTitle)
                        .setView(rootContainer)
                        .setNeutralButton("Mở bằng ứng dụng ngoài") { _, _ ->
                            launchFileProviderIntent(file)
                        }
                        .setPositiveButton("Đóng", null)
                        .create().also { dlg ->
                            dlg.setOnDismissListener {
                                if (activePreviewDialog === dlg) activePreviewDialog = null
                            }
                            dlg.show()
                        }
                }
                return
            }
        }

        if (ext in setOf("txt", "md", "csv", "log", "json", "xml") && file.length() <= 256 * 1024) {
            try {
                val contentText = file.readText(Charsets.UTF_8)
                val scroll = ScrollView(this).apply { setPadding(dp(18), dp(12), dp(18), dp(12)) }
                val tv = TextView(this).apply {
                    text = contentText
                    textSize = 14f
                    setTextColor(0xFF1E293B.toInt())
                    setTextIsSelectable(true)
                }
                scroll.addView(tv)
                if (!isFinishing && !isDestroyed) {
                    activePreviewDialog = AlertDialog.Builder(this)
                        .setTitle(displayTitle)
                        .setView(scroll)
                        .setNeutralButton("Mở ứng dụng ngoài") { _, _ ->
                            launchFileProviderIntent(file)
                        }
                        .setPositiveButton("Đóng", null)
                        .create().also { dlg ->
                            dlg.setOnDismissListener {
                                if (activePreviewDialog === dlg) activePreviewDialog = null
                            }
                            dlg.show()
                        }
                }
                return
            } catch (_: Exception) {}
        }

        launchFileProviderIntent(file)
    }

    private fun launchFileProviderIntent(file: java.io.File) {
        try {
            val contentUri = androidx.core.content.FileProvider.getUriForFile(
                this,
                "${packageName}.fileprovider",
                file
            )
            val mime = resolveMimeTypeForFile(file.name)
            val viewIntent = android.content.Intent(android.content.Intent.ACTION_VIEW).apply {
                setDataAndType(contentUri, mime)
                addFlags(android.content.Intent.FLAG_GRANT_READ_URI_PERMISSION)
            }
            startActivity(android.content.Intent.createChooser(viewIntent, "Mở tài liệu: ${file.name}"))
        } catch (e: Exception) {
            Toast.makeText(
                this,
                "Đã lưu tệp '${file.name}' trong máy nhưng chưa có ứng dụng đọc định dạng này.",
                Toast.LENGTH_LONG
            ).show()
        }
    }

    private fun downloadOfferedFileFromTeacher(
        fileId: String,
        rawFileName: String,
        autoOpen: Boolean,
        webPort: Int,
        token: String,
        explicitEndpointPath: String? = null
    ) {
        val teacherHost = (currentTeacherIp ?: discoveredTeacherIp ?: "10.0.2.2").trim()
        val activePort = if (webPort > 0) webPort else currentTeacherWebPort
        val activeToken = token.ifBlank { currentBroadcastToken }
        val cleanName = rawFileName.substringAfterLast('/').substringAfterLast('\\')
            .replace(Regex("[<>:\"|?*]"), "_")
            .ifBlank { "TaiLieu_LopHoc.dat" }

        val downloadUrl = if (!explicitEndpointPath.isNullOrBlank()) {
            "http://$teacherHost:$activePort$explicitEndpointPath"
        } else {
            val encId = java.net.URLEncoder.encode(fileId, "UTF-8")
            val encTok = java.net.URLEncoder.encode(activeToken, "UTF-8")
            "http://$teacherHost:$activePort/api/files/download?id=$encId&token=$encTok"
        }

        lifecycleScope.launch(Dispatchers.IO) {
            try {
                // Random jitter 30–320ms để 40 tablet tải êm không nghẽn Wi-Fi AP
                delay((30L..320L).random())

                val dlDir = java.io.File(getExternalFilesDir(android.os.Environment.DIRECTORY_DOWNLOADS), "QASmartClass")
                if (!dlDir.exists()) dlDir.mkdirs()

                var targetFile = java.io.File(dlDir, cleanName)
                var dupIdx = 1
                while (targetFile.exists()) {
                    val base = cleanName.substringBeforeLast('.', cleanName)
                    val ext = cleanName.substringAfterLast('.', "")
                    val nextName = if (ext.isNotEmpty()) "${base}_$dupIdx.$ext" else "${base}_$dupIdx"
                    targetFile = java.io.File(dlDir, nextName)
                    dupIdx++
                }
                val partFile = java.io.File(dlDir, "${targetFile.name}.part")

                val conn = (URL(downloadUrl).openConnection() as HttpURLConnection).apply {
                    requestMethod = "GET"
                    connectTimeout = 8000
                    readTimeout = 60000
                    setRequestProperty("Connection", "close")
                }

                val code = conn.responseCode
                if (code !in 200..206) {
                    conn.disconnect()
                    if (fileId.isNotBlank()) {
                        sendRawTcpLine("FILE_PROGRESS|$fileId|0|ERR|HTTP_$code")
                    }
                    withContext(Dispatchers.Main) {
                        Toast.makeText(this@MainActivity, "⚠️ Lỗi tải tài liệu $cleanName (HTTP $code)", Toast.LENGTH_SHORT).show()
                    }
                    return@launch
                }

                val totalBytes = conn.contentLengthLong
                var receivedBytes = 0L
                var lastPctSent = -1

                conn.inputStream.use { input ->
                    java.io.FileOutputStream(partFile).use { fos ->
                        val buf = ByteArray(65536)
                        var read: Int
                        while (input.read(buf).also { read = it } > 0) {
                            fos.write(buf, 0, read)
                            receivedBytes += read
                            if (totalBytes > 0 && fileId.isNotBlank()) {
                                val pct = ((receivedBytes * 100L) / totalBytes).toInt().coerceIn(0, 99)
                                if (pct >= lastPctSent + 30) {
                                    lastPctSent = pct
                                    sendRawTcpLine("FILE_PROGRESS|$fileId|$pct|DOWNLOADING")
                                }
                            }
                        }
                        fos.flush()
                    }
                }
                conn.disconnect()

                if (targetFile.exists()) targetFile.delete()
                partFile.renameTo(targetFile)

                if (fileId.isNotBlank()) {
                    sendRawTcpLine("FILE_PROGRESS|$fileId|100|DONE")
                }

                val sizeKbText = if (receivedBytes < 1024 * 1024) {
                    String.format(Locale.US, "%.1f KB", receivedBytes / 1024.0)
                } else {
                    String.format(Locale.US, "%.1f MB", receivedBytes / (1024.0 * 1024.0))
                }
                val nowText = SimpleDateFormat("HH:mm • dd/MM", Locale.getDefault()).format(Date())

                withContext(Dispatchers.Main) {
                    receivedTeacherFiles.removeAll { it.urlOrPath == targetFile.absolutePath }
                    receivedTeacherFiles.add(
                        0,
                        ReceivedFileItem(
                            fileName = targetFile.name,
                            urlOrPath = targetFile.absolutePath,
                            timeText = "Nhận từ Giáo viên ($sizeKbText) lúc $nowText",
                            isLocalSnapshot = false
                        )
                    )
                    refreshReceivedFilesPanel()
                    refreshLatestTeacherFileBanner()
                    if (::badgeRailFiles.isInitialized && currentTab != 3) {
                        badgeRailFiles.visibility = View.VISIBLE
                    }
                    if (currentTab == 1 && currentWorkSubTab == "HOMEWORK") {
                        switchWorkSubTab("HOMEWORK")
                    }
                    Toast.makeText(this@MainActivity, "📥 Đã lưu tài liệu từ Giáo viên: ${targetFile.name}", Toast.LENGTH_LONG).show()

                    if (autoOpen) {
                        val extLow = targetFile.extension.lowercase(Locale.ROOT)
                        if (extLow !in setOf("apk", "exe", "bat", "sh")) {
                            openLocalReceivedFile(targetFile, targetFile.name)
                        }
                    }
                }
            } catch (e: Exception) {
                Log.w(TAG, "downloadOfferedFileFromTeacher error ($downloadUrl): ${e.message}")
                if (fileId.isNotBlank()) {
                    sendRawTcpLine("FILE_PROGRESS|$fileId|0|ERR|${(e.message ?: "IO").replace('|', ' ')}")
                }
                withContext(Dispatchers.Main) {
                    Toast.makeText(this@MainActivity, "⚠️ Không tải được tệp $cleanName: ${e.message}", Toast.LENGTH_SHORT).show()
                }
            }
        }
    }

    private fun refreshReceivedFilesPanel() {
        if (!::layoutReceivedFiles.isInitialized) return
        layoutReceivedFiles.removeAllViews()
        val density = resources.displayMetrics.density
        fun dp(v: Int): Int = (v * density).toInt()

        // Quét cả thư mục Tài liệu Giáo viên gửi (Downloads/QASmartClass) và Ảnh chụp bài giảng (Pictures/QASmartClass)
        val combined = mutableListOf<ReceivedFileItem>()
        combined.addAll(receivedTeacherFiles)
        try {
            val dlDir = java.io.File(getExternalFilesDir(android.os.Environment.DIRECTORY_DOWNLOADS), "QASmartClass")
            val picDir = java.io.File(getExternalFilesDir(android.os.Environment.DIRECTORY_PICTURES), "QASmartClass")
            val diskFiles = mutableListOf<Pair<java.io.File, Boolean>>()
            dlDir.listFiles()?.filter { it.isFile && !it.name.endsWith(".part") }?.forEach { diskFiles.add(it to false) }
            picDir.listFiles()?.filter { it.isFile && !it.name.endsWith(".part") }?.forEach { diskFiles.add(it to true) }

            diskFiles.sortedByDescending { it.first.lastModified() }.take(24).forEach { (f, isSnap) ->
                if (combined.none { it.urlOrPath == f.absolutePath }) {
                    val t = SimpleDateFormat("HH:mm • dd/MM", Locale.getDefault()).format(Date(f.lastModified()))
                    val labelPrefix = if (isSnap) "Ảnh chụp lúc" else "Đã lưu lúc"
                    combined.add(ReceivedFileItem(f.name, f.absolutePath, "$labelPrefix $t", isLocalSnapshot = isSnap))
                }
            }
        } catch (_: Exception) {}

        if (combined.isEmpty()) {
            val emptyBox = LinearLayout(this).apply {
                orientation = LinearLayout.VERTICAL
                gravity = Gravity.CENTER
                setBackgroundResource(R.drawable.bg_m3_tonal_card)
                setPadding(dp(20), dp(28), dp(20), dp(28))
            }
            emptyBox.addView(TextView(this).apply {
                text = "Chưa có tài liệu nào trong phiên học này"
                textSize = 14.5f
                setTypeface(null, Typeface.BOLD)
                setTextColor(0xFF334155.toInt())
                gravity = Gravity.CENTER
            })
            emptyBox.addView(TextView(this).apply {
                text = "Khi Giáo viên phát đề/tài liệu hoặc khi bạn bấm '📸 Chụp lưu', tệp sẽ tự động lưu tại đây."
                textSize = 13f
                setTextColor(0xFF64748B.toInt())
                gravity = Gravity.CENTER
                setPadding(0, dp(6), 0, 0)
            })
            layoutReceivedFiles.addView(emptyBox)
            return
        }

        combined.forEach { item ->
            val localFileObj = java.io.File(item.urlOrPath)
            val isLocalOnDisk = localFileObj.exists()
            val extLow = item.fileName.substringAfterLast('.', "").lowercase(Locale.ROOT)
            val isImageFile = isLocalOnDisk && extLow in setOf("jpg", "jpeg", "png", "webp", "bmp")
            val isInAppPreviewable = isLocalOnDisk && extLow in setOf("jpg", "jpeg", "png", "webp", "bmp", "txt", "md", "csv", "log", "json", "xml")
            val fileEmoji = when {
                item.isLocalSnapshot || extLow in setOf("jpg", "jpeg", "png", "webp", "bmp") -> "🖼️"
                extLow == "pdf" -> "📕"
                extLow in setOf("doc", "docx", "txt") -> "📝"
                extLow in setOf("xls", "xlsx") -> "📊"
                extLow in setOf("ppt", "pptx") -> "📋"
                extLow in setOf("mp4", "webm", "mkv") -> "🎬"
                else -> "📄"
            }

            val row = LinearLayout(this).apply {
                orientation = LinearLayout.HORIZONTAL
                gravity = Gravity.CENTER_VERTICAL
                setBackgroundResource(R.drawable.bg_m3_tonal_card)
                setPadding(dp(12), dp(10), dp(12), dp(10))
                isClickable = true
                isFocusable = true
                layoutParams = LinearLayout.LayoutParams(
                    LinearLayout.LayoutParams.MATCH_PARENT,
                    LinearLayout.LayoutParams.WRAP_CONTENT
                ).apply { bottomMargin = dp(8) }
            }

            val thumbBmp = if (isImageFile) decodeSampledBitmapFromFile(localFileObj, 140) else null
            if (thumbBmp != null) {
                val thumbView = ImageView(this).apply {
                    setImageBitmap(thumbBmp)
                    scaleType = ImageView.ScaleType.CENTER_CROP
                    setBackgroundResource(R.drawable.bg_m3_surface_card)
                    clipToOutline = true
                    layoutParams = LinearLayout.LayoutParams(dp(46), dp(46)).apply {
                        marginEnd = dp(12)
                    }
                }
                row.addView(thumbView)
            } else {
                val iconText = TextView(this).apply {
                    text = fileEmoji
                    textSize = 18f
                    setPadding(0, 0, dp(12), 0)
                }
                row.addView(iconText)
            }

            val infoCol = LinearLayout(this).apply {
                orientation = LinearLayout.VERTICAL
                layoutParams = LinearLayout.LayoutParams(0, LinearLayout.LayoutParams.WRAP_CONTENT, 1f)
            }
            infoCol.addView(TextView(this).apply {
                text = item.fileName
                textSize = 14f
                setTypeface(null, Typeface.BOLD)
                setTextColor(0xFF0F172A.toInt())
                maxLines = 1
                ellipsize = android.text.TextUtils.TruncateAt.MIDDLE
            })
            infoCol.addView(TextView(this).apply {
                text = item.timeText
                textSize = 12f
                setTextColor(0xFF64748B.toInt())
            })
            row.addView(infoCol)

            val openAction = View.OnClickListener {
                if (isLocalOnDisk) {
                    openLocalReceivedFile(localFileObj, item.fileName)
                } else {
                    try {
                        startActivity(android.content.Intent(android.content.Intent.ACTION_VIEW, android.net.Uri.parse(item.urlOrPath)).apply {
                            addFlags(android.content.Intent.FLAG_ACTIVITY_NEW_TASK)
                        })
                    } catch (_: Exception) {
                        Toast.makeText(this@MainActivity, "Không thể mở đường dẫn: ${item.urlOrPath}", Toast.LENGTH_SHORT).show()
                    }
                }
            }

            val btnOpen = Button(this).apply {
                text = when {
                    isInAppPreviewable -> "👁 Xem lại"
                    isLocalOnDisk -> "📂 Mở tệp"
                    else -> "Tải về"
                }
                isAllCaps = false
                textSize = 12.5f
                setTypeface(null, Typeface.BOLD)
                setTextColor(0xFF1A73E8.toInt())
                setBackgroundResource(R.drawable.bg_m3_chip_selected)
                minWidth = 0
                minimumWidth = 0
                setPadding(dp(12), 0, dp(12), 0)
                layoutParams = LinearLayout.LayoutParams(LinearLayout.LayoutParams.WRAP_CONTENT, dp(34)).apply {
                    marginStart = dp(8)
                }
                setOnClickListener(openAction)
            }
            row.setOnClickListener(openAction)
            row.addView(btnOpen)
            layoutReceivedFiles.addView(row)
        }
    }

    private fun showExitConfirmation() {
        AlertDialog.Builder(this)
            .setTitle("Tài khoản & Phòng học")
            .setMessage("Đang đăng nhập: ${currentStudentName.ifBlank { edtStudentName.text.toString().ifBlank { "Học sinh" } }} (${currentStudentCode.ifBlank { edtStudentCode.text.toString().ifBlank { "HS001" } }})\n\nBạn muốn đổi sang học sinh khác hay rời phòng học?")
            .setPositiveButton("Đổi học sinh khác") { _, _ ->
                showSwitchAccountDialog(autoReconnectIfConnected = true)
            }
            .setNeutralButton("Rời lớp") { _, _ ->
                isUserInitiatedDisconnect = true
                disconnectFromTeacher("Đã rời phòng học")
            }
            .setNegativeButton("Ở lại", null)
            .show()
    }

    /**
     * Hộp thoại Đổi tài khoản / Học sinh khác (dùng được ở cả Màn hình Vào lớp và khi đang trong Lớp học).
     */
    private fun showSwitchAccountDialog(autoReconnectIfConnected: Boolean) {
        val density = resources.displayMetrics.density
        fun dp(v: Int): Int = (v * density).toInt()

        val container = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            setPadding(dp(22), dp(14), dp(22), dp(8))
        }

        val subTitle = TextView(this).apply {
            text = "Chọn nhanh bàn học mẫu hoặc nhập Mã học sinh của bạn:"
            textSize = 13.5f
            setTextColor(0xFF475569.toInt())
            setPadding(0, 0, 0, dp(10))
        }
        container.addView(subTitle)

        val initCode = edtStudentCode.text.toString().trim().ifBlank { "10A1-001" }
        val initName = resolveOfficialStudentName(initCode, edtStudentName.text.toString().trim())

        val inputCode = EditText(this).apply {
            hint = "Mã học sinh (VD: 10A1-002, 12A3-001)"
            setText(initCode)
            textSize = 15.5f
            setSingleLine(true)
            setBackgroundResource(R.drawable.bg_m3_pin_input)
            setPadding(dp(14), dp(10), dp(14), dp(10))
        }

        val inputName = EditText(this).apply {
            hint = "Họ và tên học sinh"
            setText(initName)
            textSize = 15.5f
            setSingleLine(true)
            setBackgroundResource(R.drawable.bg_m3_pin_input)
            setPadding(dp(14), dp(10), dp(14), dp(10))
            layoutParams = LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT,
                LinearLayout.LayoutParams.WRAP_CONTENT
            ).apply { topMargin = dp(10) }
        }

        inputCode.addTextChangedListener(object : android.text.TextWatcher {
            override fun beforeTextChanged(s: CharSequence?, start: Int, count: Int, after: Int) {}
            override fun onTextChanged(s: CharSequence?, start: Int, before: Int, count: Int) {}
            override fun afterTextChanged(s: android.text.Editable?) {
                val codeTyped = s?.toString()?.trim().orEmpty()
                val mapped = lookupRosterStudentName(codeTyped)
                if (!mapped.isNullOrBlank()) {
                    inputName.setText(mapped)
                }
            }
        })

        val inputPass = EditText(this).apply {
            hint = "Mật khẩu lớp học (Mặc định: 123456)"
            setText(edtPassword.text.toString().trim().ifBlank { "123456" })
            textSize = 15.5f
            setSingleLine(true)
            inputType = android.text.InputType.TYPE_CLASS_TEXT or android.text.InputType.TYPE_TEXT_VARIATION_PASSWORD
            setBackgroundResource(R.drawable.bg_m3_pin_input)
            setPadding(dp(14), dp(10), dp(14), dp(10))
            layoutParams = LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT,
                LinearLayout.LayoutParams.WRAP_CONTENT
            ).apply { topMargin = dp(10) }
        }

        val quickRow = HorizontalScrollView(this).apply {
            isHorizontalScrollBarEnabled = false
            layoutParams = LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT,
                LinearLayout.LayoutParams.WRAP_CONTENT
            ).apply { bottomMargin = dp(10) }
        }
        val chipContainer = LinearLayout(this).apply {
            orientation = LinearLayout.HORIZONTAL
        }

        val btnClearNew = Button(this).apply {
            text = "+ Nhập mới"
            isAllCaps = false
            textSize = 12.5f
            setTextColor(0xFFDC2626.toInt())
            setBackgroundResource(R.drawable.bg_m3_chip_unselected)
            setPadding(dp(12), 0, dp(12), 0)
            layoutParams = LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.WRAP_CONTENT,
                dp(36)
            ).apply { marginEnd = dp(8) }
            setOnClickListener {
                inputCode.setText("")
                inputName.setText("")
                inputCode.requestFocus()
            }
        }
        chipContainer.addView(btnClearNew)

        val quickProfiles = listOf(
            "10A1-001" to "Nguyễn Văn An",
            "10A1-002" to "Trần Thị Bích",
            "10A1-003" to "Trịnh Văn Bảo",
            "12A3-001" to "Lê Minh Quân",
            "12A3-002" to "Trần Thu Thảo",
            "12A3-003" to "Nguyễn Văn Sang"
        )
        quickProfiles.forEach { (qCode, qName) ->
            val chip = Button(this).apply {
                text = "$qCode • $qName"
                isAllCaps = false
                textSize = 12.5f
                setTextColor(0xFF1A73E8.toInt())
                setBackgroundResource(R.drawable.bg_m3_chip_selected)
                setPadding(dp(12), 0, dp(12), 0)
                layoutParams = LinearLayout.LayoutParams(
                    LinearLayout.LayoutParams.WRAP_CONTENT,
                    dp(36)
                ).apply { marginEnd = dp(8) }
                setOnClickListener {
                    inputCode.setText(qCode)
                    inputName.setText(qName)
                }
            }
            chipContainer.addView(chip)
        }
        quickRow.addView(chipContainer)
        container.addView(quickRow)
        container.addView(inputCode)
        container.addView(inputName)
        container.addView(inputPass)

        val wasConnected = connectedPanel.visibility == View.VISIBLE || tcpSocket != null
        val positiveLabel = if (wasConnected || autoReconnectIfConnected) "Chuyển sang học sinh này" else "Lưu hồ sơ"

        AlertDialog.Builder(this)
            .setTitle("Hồ sơ Học sinh / Bàn học")
            .setView(container)
            .setPositiveButton(positiveLabel) { _, _ ->
                val newCode = inputCode.text.toString().trim().ifBlank { "10A1-001" }
                val newName = resolveOfficialStudentName(newCode, inputName.text.toString().trim())
                val newPass = inputPass.text.toString().trim().ifBlank { "123456" }

                edtStudentCode.setText(newCode)
                edtStudentName.setText(newName)
                edtPassword.setText(newPass)
                currentStudentCode = newCode
                currentStudentName = newName
                val ip = edtTeacherIp.text.toString().trim().ifBlank { discoveredTeacherIp ?: currentTeacherIp ?: "10.0.2.2" }
                savePreferences(newName, newCode, newPass, ip, chkRemember.isChecked)
                syncStudentIdentityCard()

                if (wasConnected || autoReconnectIfConnected) {
                    isUserInitiatedDisconnect = true
                    disconnectFromTeacher("Đang chuyển sang $newName ($newCode)...")
                    btnConnect.postDelayed({
                        btnConnect.performClick()
                    }, 350)
                } else {
                    Toast.makeText(this, "Đã lưu hồ sơ: $newName ($newCode)", Toast.LENGTH_SHORT).show()
                }
            }
            .setNeutralButton("Máy chủ IP") { _, _ ->
                showServerIpConfigDialog()
            }
            .setNegativeButton("Hủy", null)
            .show()
    }

    /**
     * Điều hướng chuẩn Material Design 3 giữa 4 không gian thật:
     * 0 = Lớp học (Live Stream + Sổ nháp)
     * 1 = Bài tập (Trắc nghiệm, Bài tập giao, Khảo sát, Điểm số)
     * 2 = Thảo luận (Chat Cả lớp / Tổ nhóm / Giáo viên)
     * 3 = Tài liệu & Thông tin phiên học
     */
    private fun switchTab(index: Int) {
        currentTab = index
        hideSoftKeyboard()

        panelLecture.visibility = if (index == 0) View.VISIBLE else View.GONE
        panelSoon.visibility = if (index == 1) View.VISIBLE else View.GONE
        panelChat.visibility = if (index == 2) View.VISIBLE else View.GONE
        panelFilesProfile.visibility = if (index == 3) View.VISIBLE else View.GONE

        val realClass = homeClassName.ifBlank { currentClassName }.trim()
        txtPageTitle.text = when (index) {
            1 -> "Bài tập & Kiểm tra  •  $realClass"
            2 -> "Thảo luận lớp học  •  $realClass"
            3 -> "Tài liệu & Phiên học  •  $realClass"
            else -> realClass.ifBlank { "SMART CLASS" }
        }

        val activeBlue = 0xFF1A73E8.toInt()
        val inactiveSlate = 0xFF64748B.toInt()

        // Đồng bộ Navigation Rail (Tablet)
        fun styleRail(pill: FrameLayout, icon: ImageView, label: TextView, active: Boolean) {
            pill.setBackgroundResource(if (active) R.drawable.bg_m3_rail_indicator else 0)
            icon.imageTintList = ColorStateList.valueOf(if (active) activeBlue else inactiveSlate)
            label.setTextColor(if (active) activeBlue else inactiveSlate)
            label.setTypeface(null, if (active) Typeface.BOLD else Typeface.NORMAL)
        }
        styleRail(pillRailClass, iconRailClass, labelRailClass, index == 0)
        styleRail(pillRailWork, iconRailWork, labelRailWork, index == 1)
        styleRail(pillRailChat, iconRailChat, labelRailChat, index == 2)
        styleRail(pillRailFiles, iconRailFiles, labelRailFiles, index == 3)

        // Đồng bộ Bottom Navigation Bar (Compact < 600dp)
        styleRail(capsuleBottomHome, iconBottomHome, labelBottomHome, index == 0)
        styleRail(capsuleBottomLecture, iconBottomLecture, labelBottomLecture, index == 1)
        styleRail(capsuleBottomChat, iconBottomChat, labelBottomChat, index == 2)
        styleRail(capsuleBottomRaise, iconBottomRaise, labelBottomRaise, index == 3)

        focusReporter.currentPage = when (index) {
            0 -> "Lớp học"
            1 -> "Bài tập"
            2 -> "Thảo luận"
            else -> "Tài liệu"
        }

        when (index) {
            0 -> refreshHome()
            1 -> {
                badgeRailWork.visibility = View.GONE
                switchWorkSubTab(currentWorkSubTab)
            }
            2 -> {
                unreadTeacherChatCount = 0
                badgeRailChat.visibility = View.GONE
                scrollChat.post { scrollChat.fullScroll(View.FOCUS_DOWN) }
            }
            3 -> {
                if (::badgeRailFiles.isInitialized) {
                    badgeRailFiles.visibility = View.GONE
                }
                refreshReceivedFilesPanel()
            }
        }
    }

    private fun switchWorkSubTab(subTab: String) {
        currentWorkSubTab = subTab
        fun styleChip(btn: Button, active: Boolean) {
            btn.setBackgroundResource(if (active) R.drawable.bg_m3_chip_selected else R.drawable.bg_m3_chip_unselected)
            btn.setTextColor(if (active) 0xFF1A73E8.toInt() else 0xFF475569.toInt())
        }
        styleChip(chipWorkQuiz, subTab == "QUIZ")
        styleChip(chipWorkHomework, subTab == "HOMEWORK")
        styleChip(chipWorkSurvey, subTab == "SURVEY")
        styleChip(chipWorkResults, subTab == "RESULTS")

        val title = when (subTab) {
            "HOMEWORK" -> "Bài tập Giáo viên giao"
            "SURVEY" -> "Khảo sát ý kiến nhanh"
            "RESULTS" -> "Kết quả & Điểm số"
            else -> "Kiểm tra trắc nghiệm"
        }
        txtSoonTitle.text = title
        renderInteractiveFeaturePanel(0, title)
    }

    private fun openWorkSubTabFromTeacherCommand(subTab: String) {
        currentWorkSubTab = subTab
        badgeRailWork.visibility = if (currentTab == 1) View.GONE else View.VISIBLE
        switchTab(1)
    }

    private fun sendRawTcpLine(line: String) {
        lifecycleScope.launch(Dispatchers.IO) {
            try {
                val os = tcpOutputStream ?: return@launch
                val payload = if (line.endsWith("\n")) line else "$line\n"
                synchronized(os) {
                    os.write(payload.toByteArray(Charsets.UTF_8))
                    os.flush()
                }
            } catch (e: Exception) {
                Log.w(TAG, "Failed to send TCP line: ${e.message}")
            }
        }
    }

    private fun renderInteractiveFeaturePanel(btnId: Int, title: String) {
        if (!::layoutInteractiveFeature.isInitialized) return
        layoutInteractiveFeature.removeAllViews()
        val density = resources.displayMetrics.density
        fun dp(v: Int): Int = (v * density).toInt()

        val isQuizView = currentWorkSubTab == "QUIZ" || title.contains("trắc nghiệm", ignoreCase = true) || title.contains("kiểm tra", ignoreCase = true)
        val isSurveyView = currentWorkSubTab == "SURVEY" || title.contains("khảo sát", ignoreCase = true)
        val isAssignmentView = currentWorkSubTab == "HOMEWORK" || title.contains("bài tập", ignoreCase = true)
        val isResultsView = currentWorkSubTab == "RESULTS" || title.contains("kết quả", ignoreCase = true) || title.contains("điểm", ignoreCase = true)

        when {
            isQuizView -> {
                if (activeQuizQuestions.isEmpty()) {
                    iconSoonWrapper.visibility = View.VISIBLE
                    txtSoonSubtitle.text = if (lastQuizScoreText.isNotBlank()) {
                        "Kết quả bài kiểm tra gần nhất: $lastQuizScoreText\n$lastQuizCorrectDetail"
                    } else {
                        "Hiện chưa có đề kiểm tra đang mở. Khi Giáo viên phát đề từ máy chủ, câu hỏi sẽ tự động hiện tại đây để bạn làm bài và nộp trực tiếp."
                    }
                    layoutInteractiveFeature.visibility = View.GONE
                    return
                }

                iconSoonWrapper.visibility = View.GONE
                txtSoonSubtitle.text = if (quizSubmitted) {
                    if (lastQuizScoreText.isNotBlank()) "Đã nộp bài • Điểm số: $lastQuizScoreText" else "Đã nộp bài kiểm tra — Đang chờ Giáo viên công bố điểm"
                } else {
                    "Chọn đáp án cho từng câu hỏi bên dưới và nhấn Nộp bài kiểm tra (${activeQuizQuestions.size} câu)"
                }
                layoutInteractiveFeature.visibility = View.VISIBLE

                activeQuizQuestions.forEachIndexed { index, q ->
                    val card = LinearLayout(this).apply {
                        orientation = LinearLayout.VERTICAL
                        setBackgroundResource(R.drawable.bg_m3_surface_card)
                        setPadding(dp(18), dp(16), dp(18), dp(16))
                        layoutParams = LinearLayout.LayoutParams(
                            LinearLayout.LayoutParams.MATCH_PARENT,
                            LinearLayout.LayoutParams.WRAP_CONTENT
                        ).apply { bottomMargin = dp(12) }
                    }

                    val qHeader = TextView(this).apply {
                        text = "Câu ${index + 1} (${q.points} điểm): ${q.content}"
                        textSize = 15f
                        setTypeface(null, Typeface.BOLD)
                        setTextColor(0xFF0F172A.toInt())
                        setPadding(0, 0, 0, dp(10))
                    }
                    card.addView(qHeader)

                    val letters = listOf("A", "B", "C", "D", "E", "F")
                    val optionButtons = mutableListOf<Button>()

                    q.options.forEachIndexed { optIdx, optText ->
                        val letter = letters.getOrElse(optIdx) { "${optIdx + 1}" }
                        val displayOpt = if (optText.trim().matches(Regex("^[A-F][.):\\s].*", RegexOption.IGNORE_CASE))) {
                            optText.trim()
                        } else {
                            "$letter. $optText"
                        }

                        val btnOpt = Button(this).apply {
                            text = displayOpt
                            isAllCaps = false
                            gravity = Gravity.START or Gravity.CENTER_VERTICAL
                            textSize = 14f
                            setPadding(dp(14), dp(10), dp(14), dp(10))
                            layoutParams = LinearLayout.LayoutParams(
                                LinearLayout.LayoutParams.MATCH_PARENT,
                                LinearLayout.LayoutParams.WRAP_CONTENT
                            ).apply { bottomMargin = dp(8) }
                            isEnabled = !quizSubmitted
                        }

                        fun styleOption(selected: Boolean) {
                            if (selected) {
                                btnOpt.setBackgroundResource(R.drawable.bg_m3_chip_selected)
                                btnOpt.setTextColor(0xFF1A73E8.toInt())
                                btnOpt.setTypeface(null, Typeface.BOLD)
                            } else {
                                btnOpt.setBackgroundResource(R.drawable.bg_m3_chip_unselected)
                                btnOpt.setTextColor(0xFF1E293B.toInt())
                                btnOpt.setTypeface(null, Typeface.NORMAL)
                            }
                        }

                        styleOption(selectedQuizAnswers[q.id] == letter)
                        btnOpt.setOnClickListener {
                            if (quizSubmitted) return@setOnClickListener
                            selectedQuizAnswers[q.id] = letter
                            optionButtons.forEachIndexed { i, b ->
                                val l = letters.getOrElse(i) { "${i + 1}" }
                                val sel = l == letter
                                b.setBackgroundResource(if (sel) R.drawable.bg_m3_chip_selected else R.drawable.bg_m3_chip_unselected)
                                b.setTextColor(if (sel) 0xFF1A73E8.toInt() else 0xFF1E293B.toInt())
                                b.setTypeface(null, if (sel) Typeface.BOLD else Typeface.NORMAL)
                            }
                        }
                        optionButtons.add(btnOpt)
                        card.addView(btnOpt)
                    }

                    layoutInteractiveFeature.addView(card)
                }

                if (lastQuizScoreText.isNotBlank()) {
                    val resultBanner = TextView(this).apply {
                        text = "Kết quả chấm điểm: $lastQuizScoreText\n$lastQuizCorrectDetail"
                        textSize = 14.5f
                        setTypeface(null, Typeface.BOLD)
                        setTextColor(0xFF15803D.toInt())
                        setBackgroundResource(R.drawable.bg_pill_badge_green)
                        setPadding(dp(16), dp(12), dp(16), dp(12))
                        layoutParams = LinearLayout.LayoutParams(
                            LinearLayout.LayoutParams.MATCH_PARENT,
                            LinearLayout.LayoutParams.WRAP_CONTENT
                        ).apply { bottomMargin = dp(12) }
                    }
                    layoutInteractiveFeature.addView(resultBanner)
                }

                val btnSubmitQuiz = Button(this).apply {
                    text = if (quizSubmitted) "ĐÃ NỘP BÀI KIỂM TRA" else "Nộp bài kiểm tra"
                    isAllCaps = false
                    textSize = 15f
                    setTypeface(null, Typeface.BOLD)
                    setTextColor(0xFFFFFFFF.toInt())
                    setBackgroundResource(R.drawable.bg_m3_primary_pill)
                    backgroundTintList = ColorStateList.valueOf(if (quizSubmitted) 0xFF64748B.toInt() else 0xFF1A73E8.toInt())
                    isEnabled = !quizSubmitted
                    layoutParams = LinearLayout.LayoutParams(
                        LinearLayout.LayoutParams.MATCH_PARENT,
                        dp(48)
                    ).apply { topMargin = dp(4); bottomMargin = dp(8) }
                    setOnClickListener {
                        if (selectedQuizAnswers.isEmpty()) {
                            Toast.makeText(this@MainActivity, "Vui lòng chọn ít nhất 1 đáp án trước khi nộp bài!", Toast.LENGTH_SHORT).show()
                            return@setOnClickListener
                        }
                        val ansJson = org.json.JSONObject()
                        activeQuizQuestions.forEachIndexed { idx, question ->
                            val ans = selectedQuizAnswers[question.id] ?: ""
                            if (ans.isNotBlank()) {
                                ansJson.put(question.id.toString(), ans)
                                ansJson.put((idx + 1).toString(), ans)
                            }
                        }
                        val qId = if (activeQuizId > 0) activeQuizId else 1
                        sendRawTcpLine("QUIZ_ANSWER|$qId|$ansJson")
                        quizSubmitted = true
                        Toast.makeText(this@MainActivity, "Đã gửi bài làm tới máy Giáo viên!", Toast.LENGTH_SHORT).show()
                        renderInteractiveFeaturePanel(btnId, title)
                    }
                }
                layoutInteractiveFeature.addView(btnSubmitQuiz)
            }

            isSurveyView -> {
                if (activeSurveyQuestion.isBlank()) {
                    iconSoonWrapper.visibility = View.VISIBLE
                    txtSoonSubtitle.text = "Hiện chưa có phiên khảo sát nào đang mở. Khi Giáo viên phát phiếu khảo sát nhanh trên lớp, nội dung sẽ hiển thị tại đây."
                    layoutInteractiveFeature.visibility = View.GONE
                    return
                }

                iconSoonWrapper.visibility = View.GONE
                txtSoonSubtitle.text = if (surveySubmitted) "Đã gửi phản hồi khảo sát cho Giáo viên." else activeSurveyTitle
                layoutInteractiveFeature.visibility = View.VISIBLE

                val card = LinearLayout(this).apply {
                    orientation = LinearLayout.VERTICAL
                    setBackgroundResource(R.drawable.bg_m3_surface_card)
                    setPadding(dp(18), dp(16), dp(18), dp(16))
                    layoutParams = LinearLayout.LayoutParams(
                        LinearLayout.LayoutParams.MATCH_PARENT,
                        LinearLayout.LayoutParams.WRAP_CONTENT
                    ).apply { bottomMargin = dp(12) }
                }

                val qTitle = TextView(this).apply {
                    text = activeSurveyQuestion
                    textSize = 16f
                    setTypeface(null, Typeface.BOLD)
                    setTextColor(0xFF0F172A.toInt())
                    setPadding(0, 0, 0, dp(12))
                }
                card.addView(qTitle)

                val optionsToRender = if (activeSurveyOptions.isNotEmpty()) {
                    activeSurveyOptions
                } else {
                    listOf("Rất hiểu bài (5 sao)", "Hiểu bài (4 sao)", "Bình thường (3 sao)", "Cần giảng lại (2 sao)")
                }

                val optionBtns = mutableListOf<Button>()
                optionsToRender.forEachIndexed { idx, optLabel ->
                    val btnOpt = Button(this).apply {
                        text = optLabel
                        isAllCaps = false
                        gravity = Gravity.START or Gravity.CENTER_VERTICAL
                        textSize = 14f
                        setPadding(dp(14), dp(10), dp(14), dp(10))
                        isEnabled = !surveySubmitted
                        val sel = selectedSurveyOptionIdx == idx
                        setBackgroundResource(if (sel) R.drawable.bg_m3_chip_selected else R.drawable.bg_m3_chip_unselected)
                        setTextColor(if (sel) 0xFF1A73E8.toInt() else 0xFF1E293B.toInt())
                        layoutParams = LinearLayout.LayoutParams(
                            LinearLayout.LayoutParams.MATCH_PARENT,
                            LinearLayout.LayoutParams.WRAP_CONTENT
                        ).apply { bottomMargin = dp(8) }
                        setOnClickListener {
                            if (surveySubmitted) return@setOnClickListener
                            selectedSurveyOptionIdx = idx
                            selectedSurveyStar = (5 - idx).coerceIn(1, 5)
                            optionBtns.forEachIndexed { i, b ->
                                val s = i == idx
                                b.setBackgroundResource(if (s) R.drawable.bg_m3_chip_selected else R.drawable.bg_m3_chip_unselected)
                                b.setTextColor(if (s) 0xFF1A73E8.toInt() else 0xFF1E293B.toInt())
                            }
                        }
                    }
                    optionBtns.add(btnOpt)
                    card.addView(btnOpt)
                }

                layoutInteractiveFeature.addView(card)

                val btnSubmitSurvey = Button(this).apply {
                    text = if (surveySubmitted) "ĐÃ GỬI KHẢO SÁT" else "Gửi phản hồi khảo sát"
                    isAllCaps = false
                    textSize = 15f
                    setTypeface(null, Typeface.BOLD)
                    setTextColor(0xFFFFFFFF.toInt())
                    setBackgroundResource(R.drawable.bg_m3_primary_pill)
                    backgroundTintList = ColorStateList.valueOf(if (surveySubmitted) 0xFF64748B.toInt() else 0xFF1A73E8.toInt())
                    isEnabled = !surveySubmitted
                    layoutParams = LinearLayout.LayoutParams(
                        LinearLayout.LayoutParams.MATCH_PARENT,
                        dp(48)
                    )
                    setOnClickListener {
                        if (selectedSurveyOptionIdx < 0) {
                            Toast.makeText(this@MainActivity, "Vui lòng chọn một phương án phản hồi!", Toast.LENGTH_SHORT).show()
                            return@setOnClickListener
                        }
                        val studentName = currentStudentName.ifBlank { "Học sinh" }
                        val optKey = "opt$selectedSurveyOptionIdx"
                        sendRawTcpLine("SURVEY_RESPONSE|$optKey|$studentName")
                        if (activeSurveyType.equals("satisfaction", ignoreCase = true) && selectedSurveyStar in 1..5) {
                            sendRawTcpLine("SURVEY_STAR|$selectedSurveyStar")
                        }
                        surveySubmitted = true
                        Toast.makeText(this@MainActivity, "Đã gửi kết quả khảo sát tới Giáo viên!", Toast.LENGTH_SHORT).show()
                        renderInteractiveFeaturePanel(btnId, title)
                    }
                }
                layoutInteractiveFeature.addView(btnSubmitSurvey)
            }

            isAssignmentView -> {
                val teacherFiles = getAvailableTeacherFiles(8)
                if (activeHwTitle.isBlank() && activeHwDesc.isBlank() && teacherFiles.isEmpty()) {
                    iconSoonWrapper.visibility = View.VISIBLE
                    txtSoonSubtitle.text = "Hiện chưa có bài tập mới nào được giao. Khi Giáo viên giao bài tập hoặc gửi đề trên lớp, đề bài và khung làm bài sẽ hiện tại đây."
                    layoutInteractiveFeature.visibility = View.GONE
                    return
                }

                iconSoonWrapper.visibility = View.GONE
                txtSoonSubtitle.text = if (hwSubmitted) {
                    lastHwAckMessage.ifBlank { "Đã nộp bài tập thành công cho Giáo viên." }
                } else {
                    "Bấm '👁 Xem lại' để mở đề/tài liệu Giáo viên gửi bất cứ lúc nào và nhập bài làm bên dưới"
                }
                layoutInteractiveFeature.visibility = View.VISIBLE

                val card = LinearLayout(this).apply {
                    orientation = LinearLayout.VERTICAL
                    setBackgroundResource(R.drawable.bg_m3_surface_card)
                    setPadding(dp(18), dp(16), dp(18), dp(16))
                    layoutParams = LinearLayout.LayoutParams(
                        LinearLayout.LayoutParams.MATCH_PARENT,
                        LinearLayout.LayoutParams.WRAP_CONTENT
                    ).apply { bottomMargin = dp(12) }
                }

                val tvSubject = TextView(this).apply {
                    text = "Môn: ${activeHwSubject.ifBlank { activeLessonSubject.ifBlank { "Bài tập lớp học" } }}" +
                        if (activeHwDeadline.isNotBlank()) "  •  Hạn nộp: $activeHwDeadline" else ""
                    textSize = 12.5f
                    setTextColor(0xFF1A73E8.toInt())
                    setTypeface(null, Typeface.BOLD)
                }
                card.addView(tvSubject)

                val tvTitle = TextView(this).apply {
                    text = activeHwTitle.ifBlank {
                        if (teacherFiles.isNotEmpty()) "Đề bài / Tài liệu Giáo viên gửi" else "Bài tập Giáo viên giao"
                    }
                    textSize = 16f
                    setTypeface(null, Typeface.BOLD)
                    setTextColor(0xFF0F172A.toInt())
                    setPadding(0, dp(6), 0, dp(6))
                }
                card.addView(tvTitle)

                val tvDesc = TextView(this).apply {
                    text = activeHwDesc.ifBlank {
                        if (teacherFiles.isNotEmpty()) {
                            "Xem đề bài/tài liệu đính kèm bên dưới và trình bày câu trả lời của bạn."
                        } else {
                            "Trình bày lời giải hoặc câu trả lời vào ô bên dưới."
                        }
                    }
                    textSize = 14f
                    setTextColor(0xFF334155.toInt())
                    setPadding(0, 0, 0, dp(10))
                }
                card.addView(tvDesc)

                // Danh sách Đề bài / Tài liệu Giáo viên gửi — Bấm xem lại thoải mái không giới hạn số lần!
                if (teacherFiles.isNotEmpty()) {
                    val attachSection = LinearLayout(this).apply {
                        orientation = LinearLayout.VERTICAL
                        setBackgroundResource(R.drawable.bg_callout_blue)
                        setPadding(dp(12), dp(10), dp(12), dp(6))
                        layoutParams = LinearLayout.LayoutParams(
                            LinearLayout.LayoutParams.MATCH_PARENT,
                            LinearLayout.LayoutParams.WRAP_CONTENT
                        ).apply { bottomMargin = dp(12) }
                    }

                    val attachHeader = TextView(this).apply {
                        text = "📎 Đề bài / Tài liệu từ Giáo viên (${teacherFiles.size} tệp — Chạm để xem lại)"
                        textSize = 12.5f
                        setTypeface(null, Typeface.BOLD)
                        setTextColor(0xFF1A73E8.toInt())
                        setPadding(0, 0, 0, dp(8))
                    }
                    attachSection.addView(attachHeader)

                    teacherFiles.forEach { fItem ->
                        val localFile = java.io.File(fItem.urlOrPath)
                        val extLow = fItem.fileName.substringAfterLast('.', "").lowercase(Locale.ROOT)
                        val isImg = localFile.exists() && extLow in setOf("jpg", "jpeg", "png", "webp", "bmp")
                        val fileEmoji = when {
                            isImg -> "🖼️"
                            extLow == "pdf" -> "📕"
                            extLow in setOf("doc", "docx", "txt") -> "📝"
                            extLow in setOf("xls", "xlsx") -> "📊"
                            extLow in setOf("ppt", "pptx") -> "📋"
                            else -> "📄"
                        }

                        val fileRow = LinearLayout(this).apply {
                            orientation = LinearLayout.HORIZONTAL
                            gravity = Gravity.CENTER_VERTICAL
                            setBackgroundResource(R.drawable.bg_m3_surface_card)
                            setPadding(dp(10), dp(8), dp(10), dp(8))
                            isClickable = true
                            isFocusable = true
                            layoutParams = LinearLayout.LayoutParams(
                                LinearLayout.LayoutParams.MATCH_PARENT,
                                LinearLayout.LayoutParams.WRAP_CONTENT
                            ).apply { bottomMargin = dp(6) }
                        }

                        val thumb = if (isImg) decodeSampledBitmapFromFile(localFile, 160) else null
                        if (thumb != null) {
                            val ivThumb = ImageView(this).apply {
                                setImageBitmap(thumb)
                                scaleType = ImageView.ScaleType.CENTER_CROP
                                setBackgroundResource(R.drawable.bg_m3_tonal_card)
                                clipToOutline = true
                                layoutParams = LinearLayout.LayoutParams(dp(50), dp(50)).apply {
                                    marginEnd = dp(10)
                                }
                            }
                            fileRow.addView(ivThumb)
                        } else {
                            fileRow.addView(TextView(this).apply {
                                text = fileEmoji
                                textSize = 18f
                                setPadding(0, 0, dp(10), 0)
                            })
                        }

                        val metaCol = LinearLayout(this).apply {
                            orientation = LinearLayout.VERTICAL
                            layoutParams = LinearLayout.LayoutParams(0, LinearLayout.LayoutParams.WRAP_CONTENT, 1f)
                        }
                        metaCol.addView(TextView(this).apply {
                            text = fItem.fileName
                            textSize = 13.5f
                            setTypeface(null, Typeface.BOLD)
                            setTextColor(0xFF0F172A.toInt())
                            maxLines = 1
                            ellipsize = android.text.TextUtils.TruncateAt.MIDDLE
                        })
                        metaCol.addView(TextView(this).apply {
                            text = fItem.timeText
                            textSize = 11.5f
                            setTextColor(0xFF64748B.toInt())
                        })
                        fileRow.addView(metaCol)

                        val reopenAction = View.OnClickListener {
                            openLocalReceivedFile(localFile, fItem.fileName)
                        }

                        val btnPreviewAgain = Button(this).apply {
                            text = "👁 Xem lại"
                            isAllCaps = false
                            textSize = 12.5f
                            setTypeface(null, Typeface.BOLD)
                            setTextColor(0xFF1A73E8.toInt())
                            setBackgroundResource(R.drawable.bg_m3_chip_selected)
                            minWidth = 0
                            minimumWidth = 0
                            setPadding(dp(12), 0, dp(12), 0)
                            layoutParams = LinearLayout.LayoutParams(LinearLayout.LayoutParams.WRAP_CONTENT, dp(34)).apply {
                                marginStart = dp(8)
                            }
                            setOnClickListener(reopenAction)
                        }
                        fileRow.setOnClickListener(reopenAction)
                        fileRow.addView(btnPreviewAgain)
                        attachSection.addView(fileRow)
                    }

                    card.addView(attachSection)
                }

                val edtAnswer = EditText(this).apply {
                    hint = "Nhập bài làm / câu trả lời của bạn tại đây..."
                    setText(hwDraftAnswer)
                    textSize = 14f
                    minLines = 4
                    gravity = Gravity.TOP or Gravity.START
                    setBackgroundResource(R.drawable.bg_m3_pin_input)
                    setPadding(dp(14), dp(12), dp(14), dp(12))
                    isEnabled = !hwSubmitted
                    addTextChangedListener(object : android.text.TextWatcher {
                        override fun beforeTextChanged(s: CharSequence?, start: Int, count: Int, after: Int) {}
                        override fun onTextChanged(s: CharSequence?, start: Int, before: Int, count: Int) {}
                        override fun afterTextChanged(s: android.text.Editable?) {
                            hwDraftAnswer = s?.toString().orEmpty()
                        }
                    })
                }
                card.addView(edtAnswer)
                layoutInteractiveFeature.addView(card)

                val btnPickHwFile = Button(this).apply {
                    text = "📎 Đính kèm tệp / Ảnh bài làm từ thiết bị để nộp"
                    isAllCaps = false
                    textSize = 14f
                    setTypeface(null, Typeface.BOLD)
                    setTextColor(0xFF1A73E8.toInt())
                    setBackgroundResource(R.drawable.bg_m3_outlined_pill)
                    layoutParams = LinearLayout.LayoutParams(
                        LinearLayout.LayoutParams.MATCH_PARENT,
                        dp(46)
                    ).apply { bottomMargin = dp(10) }
                    setOnClickListener {
                        launchStudentFilePicker()
                    }
                }
                layoutInteractiveFeature.addView(btnPickHwFile)

                val btnSubmitHw = Button(this).apply {
                    text = if (hwSubmitted) "ĐÃ NỘP BÀI TẬP (Có thể nộp bổ sung)" else "Nộp bài tự luận cho Giáo viên"
                    isAllCaps = false
                    textSize = 15f
                    setTypeface(null, Typeface.BOLD)
                    setTextColor(0xFFFFFFFF.toInt())
                    setBackgroundResource(R.drawable.bg_m3_primary_pill)
                    backgroundTintList = ColorStateList.valueOf(if (hwSubmitted) 0xFF0284C7.toInt() else 0xFF16A34A.toInt())
                    layoutParams = LinearLayout.LayoutParams(
                        LinearLayout.LayoutParams.MATCH_PARENT,
                        dp(48)
                    )
                    setOnClickListener {
                        val ans = edtAnswer.text.toString().trim()
                        if (ans.isBlank()) {
                            Toast.makeText(this@MainActivity, "Vui lòng nhập nội dung bài làm hoặc bấm '📎 Đính kèm tệp' để nộp file!", Toast.LENGTH_SHORT).show()
                            return@setOnClickListener
                        }
                        hwDraftAnswer = ans
                        val safeAns = ans.replace("|", " ").replace("\n", "\\n")
                        val hwIdVal = if (activeHwId > 0) activeHwId else 1
                        sendRawTcpLine("HOMEWORK_SUBMIT|$hwIdVal|$safeAns|[]")

                        // Đồng thời lưu thành tệp .txt chuẩn hóa đẩy thẳng vào thư mục Submissions của Giáo viên
                        lifecycleScope.launch {
                            val ts = SimpleDateFormat("HHmmss", Locale.US).format(Date())
                            val tmpTxt = java.io.File(cacheDir, "BaiTuLuan_$ts.txt")
                            withContext(Dispatchers.IO) {
                                tmpTxt.writeText(ans, Charsets.UTF_8)
                            }
                            val ok = uploadLocalFileToTeacher(tmpTxt, tmpTxt.name)
                            try { tmpTxt.delete() } catch (_: Exception) {}
                            hwSubmitted = true
                            if (ok.first) {
                                Toast.makeText(this@MainActivity, "✅ Đã nộp bài tập vào thư mục Thu bài của Giáo viên!", Toast.LENGTH_SHORT).show()
                            } else {
                                Toast.makeText(this@MainActivity, "Đã gửi câu trả lời (${ok.second})", Toast.LENGTH_SHORT).show()
                            }
                            renderInteractiveFeaturePanel(btnId, title)
                        }
                    }
                }
                layoutInteractiveFeature.addView(btnSubmitHw)
            }

            isResultsView -> {
                iconSoonWrapper.visibility = View.GONE
                txtSoonSubtitle.text = "Tổng hợp kết quả kiểm tra và bài tập thực tế trong phiên học hiện tại"
                layoutInteractiveFeature.visibility = View.VISIBLE

                val summaryCard = LinearLayout(this).apply {
                    orientation = LinearLayout.VERTICAL
                    setBackgroundResource(R.drawable.bg_m3_surface_card)
                    setPadding(dp(18), dp(16), dp(18), dp(16))
                    layoutParams = LinearLayout.LayoutParams(
                        LinearLayout.LayoutParams.MATCH_PARENT,
                        LinearLayout.LayoutParams.WRAP_CONTENT
                    )
                }

                val rows = listOf(
                    "Học sinh" to "${currentStudentName.ifBlank { "Học sinh" }} (${currentStudentCode.ifBlank { "—" }})",
                    "Lớp học" to homeClassName.ifBlank { currentClassName },
                    "Điểm trắc nghiệm gần nhất" to lastQuizScoreText.ifBlank { "Chưa có điểm" },
                    "Chi tiết câu đúng" to lastQuizCorrectDetail.ifBlank { if (quizSubmitted) "Đã nộp bài — Chờ chấm" else "Chưa làm bài" },
                    "Bài tập Giáo viên giao" to if (hwSubmitted) "Đã nộp bài tập" else if (activeHwTitle.isNotBlank()) "Đang có bài tập cần nộp: $activeHwTitle" else "Chưa có bài tập",
                    "Khảo sát lớp học" to if (surveySubmitted) "Đã gửi phản hồi" else "Chưa tham gia"
                )

                rows.forEach { (k, v) ->
                    val row = LinearLayout(this).apply {
                        orientation = LinearLayout.VERTICAL
                        setPadding(0, dp(6), 0, dp(6))
                    }
                    row.addView(TextView(this).apply {
                        text = k
                        textSize = 12f
                        setTextColor(0xFF64748B.toInt())
                    })
                    row.addView(TextView(this).apply {
                        text = v
                        textSize = 14.5f
                        setTypeface(null, Typeface.BOLD)
                        setTextColor(0xFF0F172A.toInt())
                    })
                    summaryCard.addView(row)
                }
                layoutInteractiveFeature.addView(summaryCard)
            }
        }
    }

    /** Cập nhật Không gian Lớp học từ trạng thái thực tế của phiên kết nối (100% không dữ liệu ảo). */
    private fun refreshHome() {
        if (!::txtHomeGreeting.isInitialized) return
        val realClass = homeClassName.ifBlank { currentClassName }.trim()
        val teacherDisplay = homeTeacherName.ifBlank { currentTeacherName }.trim()
        val pingSuffix = if (lastMeasuredPingMs > 0L) " • Ping ${lastMeasuredPingMs}ms" else ""

        txtHomeGreeting.text = "Xin chào, ${homeStudentName.ifBlank { currentStudentName.ifBlank { "Học sinh" } }}"
        txtHomeSub.text = buildString {
            if (realClass.isNotBlank()) append(realClass)
            if (teacherDisplay.isNotBlank()) {
                if (isNotEmpty()) append("  •  ")
                append("GV: $teacherDisplay")
            }
            append("\nKhi Giáo viên chia sẻ màn hình hoặc mở tiết học, hình ảnh trực tiếp sẽ tự động hiển thị tại đây.")
        }

        layoutHomeLessonActive.visibility = if (isLessonActive && !isScreenBroadcastActive) View.VISIBLE else View.GONE
        layoutHomeLessonWaiting.visibility = if (!isLessonActive && !isScreenBroadcastActive) View.VISIBLE else View.GONE

        if (::txtHeaderLiveBadge.isInitialized) {
            txtHeaderLiveBadge.text = when {
                isScreenBroadcastActive -> "🔴 Đang xem chiếu trực tiếp  •  GV: $teacherDisplay$pingSuffix"
                isLessonActive -> "● Đang học trực tiếp  •  GV: $teacherDisplay$pingSuffix"
                else -> "● Đã kết nối ổn định  •  GV: $teacherDisplay$pingSuffix"
            }
            txtHeaderLiveBadge.setTextColor(if (isScreenBroadcastActive) 0xFFDC2626.toInt() else 0xFF16A34A.toInt())
        }

        if (!isScreenBroadcastActive) {
            txtInlineBroadcastTitle.text = if (isLessonActive) {
                activeLessonTitle.ifBlank { "Bài giảng trực tiếp" }
            } else {
                realClass.ifBlank { "Màn hình lớp học" }
            }
        }

        if (isLessonActive) {
            txtHomeLessonTitle.text = activeLessonTitle.ifBlank { "Tiết học trực tiếp" }
            txtHomeSubjectBadge.text = activeLessonSubject.ifBlank { realClass.ifBlank { "Bài giảng" } }
            txtHomeStageBadge.text = if (activeLessonStage in 1..6) "Giai đoạn $activeLessonStage/6" else "Đang giảng dạy"
            txtHomeBoardContent.text = activeLessonSummary.ifBlank {
                "Giáo viên ($teacherDisplay) đang điều hành nội dung bài học trên bảng chính."
            }
        }
        refreshLatestTeacherFileBanner()
    }

    private fun selectHandRaiseReason(reason: String) {
        currentHandRaiseReason = reason
        if (::btnTopHandReason.isInitialized) {
            btnTopHandReason.text = "$reason ▾"
        }
    }

    private fun lookupRosterStudentName(rawCode: String): String? {
        val code = rawCode.trim().uppercase(Locale.US)
        if (code.isBlank()) return null
        val rosterMap = mapOf(
            // Lớp 10A1
            "10A1-001" to "Nguyễn Văn An", "10A1-002" to "Trần Thị Bích", "10A1-003" to "Trịnh Văn Bảo",
            "10A1-004" to "Võ Thị Cẩm", "10A1-005" to "Lê Hoàng Cường", "10A1-006" to "Phạm Thị Dung",
            "10A1-007" to "Dương Minh Đạt", "10A1-008" to "Hoàng Văn Đức", "10A1-009" to "Lê Thị Giang",
            "10A1-010" to "Đỗ Quang Hải", "10A1-011" to "Vũ Thị Hoa", "10A1-012" to "Trần Đức Hùng",
            "10A1-013" to "Bùi Đức Khang", "10A1-014" to "Nguyễn Thị Khánh", "10A1-015" to "Lý Thị Lan",
            "10A1-016" to "Phạm Văn Long", "10A1-017" to "Hoàng Thị Mai", "10A1-018" to "Mai Văn Minh",
            "10A1-019" to "Đỗ Văn Nam", "10A1-020" to "Đinh Thị Ngọc", "10A1-021" to "Vũ Thị Oanh",
            "10A1-022" to "Trương Quốc Phong", "10A1-023" to "Ngô Thị Phương", "10A1-024" to "Bùi Quốc Phúc",
            "10A1-025" to "Đặng Thị Quỳnh", "10A1-026" to "Lý Thị Quyên", "10A1-027" to "Hà Sĩ Ren",
            "10A1-028" to "Cao Thị Sen", "10A1-029" to "Mai Văn Sơn", "10A1-030" to "Đinh Thị Thanh",
            "10A1-031" to "Hà Minh Trí", "10A1-032" to "Tô Văn Tuấn", "10A1-033" to "Cao Văn Uy",
            "10A1-034" to "Phan Thị Uyên", "10A1-035" to "Phan Thị Vân", "10A1-036" to "Lương Văn Vũ",
            "10A1-037" to "Ngô Đức Xuân A", "10A1-038" to "Châu Thị Xuân B", "10A1-039" to "Đặng Thị Yên",
            "10A1-040" to "Nguyễn Hoàng Yến",
            // Lớp 10A2
            "10A2-001" to "Hoàng Đức Anh A", "10A2-002" to "Lê Văn Anh B", "10A2-003" to "Nguyễn Thị Bình",
            "10A2-004" to "Vũ Văn Cảnh", "10A2-005" to "Phạm Hoàng Châu", "10A2-006" to "Trần Thị Diệu",
            "10A2-007" to "Bùi Thị Duyên", "10A2-008" to "Hoàng Văn Em", "10A2-009" to "Lý Văn Hà",
            "10A2-010" to "Vũ Thị Hằng", "10A2-011" to "Mai Thị Hạnh", "10A2-012" to "Đỗ Minh Hiếu",
            // Lớp 10A3
            "10A3-001" to "Nguyễn Đức Cường", "10A3-002" to "Trần Hoàng Bách", "10A3-003" to "Lê Thị Kim Ngân",
            "10A3-004" to "Vũ Minh Quân", "10A3-005" to "Phạm Đức Trung", "10A3-006" to "Bùi Thị Hà",
            "10A3-007" to "Đặng Minh Trí", "10A3-008" to "Hoàng Thu Thủy", "10A3-009" to "Ngô Tuấn Kiệt",
            "10A3-010" to "Đinh Hải Yến",
            // Lớp 11A1
            "11A1-001" to "Vũ Thị Ngọc Anh", "11A1-002" to "Hà Quốc Bảo", "11A1-003" to "Cao Thị Kim Chi",
            "11A1-004" to "Vũ Đức Dũng", "11A1-005" to "Phan Thị Mỹ Duyên", "11A1-006" to "Tô Minh Đăng",
            "11A1-007" to "Ngô Thị Thu Hà", "11A1-008" to "Cao Thị Ngọc Hân", "11A1-009" to "Lê Thị Thanh Hương",
            "11A1-010" to "Lương Đức Huy",
            // Lớp 11A2
            "11A2-001" to "Đỗ Thị Phương Anh A", "11A2-002" to "Lương Hoàng Anh B", "11A2-003" to "Lý Minh Châu",
            "11A2-004" to "Võ Quốc Cường", "11A2-005" to "Bùi Thị Ngọc Diệp", "11A2-006" to "Phạm Văn Đại",
            // Lớp 11A3
            "11A3-001" to "Đặng Xuân Hòa", "11A3-002" to "Đỗ Cẩm Ly", "11A3-003" to "Phạm Thị Thanh Hương",
            "11A3-004" to "Trần Anh Tuấn", "11A3-005" to "Lê Bảo Nam", "11A3-006" to "Nguyễn Mai Phương",
            // Lớp 12A1
            "12A1-001" to "Lê Đức Anh", "12A1-002" to "Cao Thị Ngọc Ánh", "12A1-003" to "Trương Hoàng Bách",
            "12A1-004" to "Đỗ Thị Minh Châu", "12A1-005" to "Tô Đức Chính", "12A1-006" to "Ngô Thị Kim Cúc",
            "12A1-007" to "Mai Thị Phương Dung", "12A1-008" to "Hoàng Quốc Dũng", "12A1-009" to "Nguyễn Hoàng Duy",
            "12A1-010" to "Lý Minh Đạt",
            // Lớp 12A2
            "12A2-001" to "Đỗ Quốc Anh", "12A2-002" to "Cao Thị Nguyệt Ánh", "12A2-003" to "Đỗ Thị Hải Châu A",
            "12A2-004" to "Phan Thị Bảo Châu B", "12A2-005" to "Lê Minh Công", "12A2-006" to "Đặng Thị Kim Dung",
            // Lớp 12A3
            "12A3-001" to "Lê Minh Quân", "12A3-002" to "Trần Thu Thảo", "12A3-003" to "Nguyễn Văn Sang",
            "12A3-004" to "Phạm Gia Huy", "12A3-005" to "Vũ Thu Hà", "12A3-006" to "Đỗ Hữu Thắng",
            "12A3-007" to "Hoàng Cẩm Tú", "12A3-008" to "Đinh Quang Huy", "12A3-009" to "Bùi Lan Anh",
            "12A3-010" to "Ngô Tuấn Anh", "12A3-011" to "Lý Thanh Trúc", "12A3-012" to "Mai Phương Lan",
            "12A3-013" to "Trịnh Minh Hoàng", "12A3-014" to "Phan Thị Mơ", "12A3-015" to "Đoàn Tuấn Hưng",
            // Mã bàn HS001..HS006
            "HS001" to "Nguyễn Văn An", "HS002" to "Trần Thị Bích", "HS003" to "Trịnh Văn Bảo",
            "HS004" to "Võ Thị Cẩm", "HS005" to "Lê Hoàng Cường", "HS006" to "Phạm Thị Dung"
        )
        return rosterMap[code]
    }

    private fun isValidStudentCode(raw: String?): Boolean {
        val s = raw?.trim().orEmpty()
        return s.length in 2..32 &&
            !s.contains("ENC_CMD", ignoreCase = true) &&
            s.matches(Regex("^[A-Za-z0-9_-]{2,32}$"))
    }

    private fun sanitizeStudentCode(raw: String?): String {
        val s = raw?.trim().orEmpty()
        return if (isValidStudentCode(s)) s else "AND-${getDeviceShortTag()}"
    }

    private fun isValidStudentName(raw: String?): Boolean {
        val s = raw?.trim().orEmpty()
        return s.length in 2..45 &&
            !s.contains("/") &&
            !s.contains("+") &&
            !s.contains("=") &&
            !s.contains("|") &&
            !s.contains("ENC_CMD", ignoreCase = true) &&
            !s.equals("Học sinh Android", ignoreCase = true) &&
            !s.equals("Học sinh", ignoreCase = true) &&
            !s.startsWith("Học sinh ", ignoreCase = true)
    }

    private fun resolveOfficialStudentName(rawCode: String, rawInputName: String = ""): String {
        val code = sanitizeStudentCode(rawCode)
        val rosterName = lookupRosterStudentName(code)
        val cleanedInput = rawInputName.trim()

        if (rosterName != null) {
            return rosterName
        }
        if (isValidStudentName(cleanedInput)) {
            return cleanedInput
        }
        return lookupRosterStudentName("10A1-002") ?: code
    }

    private fun syncStudentIdentityCard() {
        if (!::txtStudentIdentityName.isInitialized) return
        val code = sanitizeStudentCode(edtStudentCode.text.toString())
        val name = resolveOfficialStudentName(code, edtStudentName.text.toString().trim())
        if (edtStudentCode.text.toString().trim() != code) {
            edtStudentCode.setText(code)
        }
        if (edtStudentName.text.toString().trim() != name) {
            edtStudentName.setText(name)
        }
        val rawSeatNum = Regex("(\\d+)$").find(code)?.groupValues?.getOrNull(1)?.toIntOrNull() ?: 1
        val seatDigits = String.format(Locale.US, "%02d", rawSeatNum)
        txtStudentIdentityName.text = name
        txtSeatBadge.text = "Bàn $seatDigits  •  Mã HS: $code"
        txtStudentIdentityCode.text = "Tự động tìm phòng học mở trong cùng mạng Wi-Fi / LAN."
        if (::txtJoinAvatarInitials.isInitialized) {
            val parts = name.trim().split(Regex("\\s+")).filter { it.isNotBlank() }
            txtJoinAvatarInitials.text = when {
                parts.size >= 2 -> "${parts.first().first()}${parts.last().first()}".uppercase()
                parts.size == 1 -> parts.first().take(2).uppercase()
                else -> "HS"
            }
        }
    }

    private fun applyDocumentZoom() {
        if (!::txtZoomLevel.isInitialized) return
        txtZoomLevel.text = "$currentZoomPercent%"
        val scale = (currentZoomPercent / 100f).coerceIn(0.7f, 2.0f)
        imgInlineBroadcast.scaleX = scale
        imgInlineBroadcast.scaleY = scale
    }

    private suspend fun uploadLocalFileToTeacher(
        file: java.io.File,
        originalDisplayName: String,
        onProgress: ((Int) -> Unit)? = null
    ): Pair<Boolean, String> = withContext(Dispatchers.IO) {
        if (!file.exists() || file.length() <= 0L) {
            return@withContext false to "Tệp rỗng (0 byte) hoặc không đọc được."
        }
        val teacherHost = (currentTeacherIp ?: discoveredTeacherIp ?: "").trim()
        if (teacherHost.isBlank()) {
            return@withContext false to "Chưa kết nối tới máy Giáo viên."
        }
        val webPort = if (currentTeacherWebPort > 0) currentTeacherWebPort else 8080
        val stuCode = currentStudentCode.ifBlank { edtStudentCode.text.toString().trim().ifBlank { "HS001" } }
        val stuName = currentStudentName.ifBlank { edtStudentName.text.toString().trim().ifBlank { "Hoc_sinh" } }

        val encCode = java.net.URLEncoder.encode(stuCode, "UTF-8")
        val encName = java.net.URLEncoder.encode(stuName, "UTF-8")
        val encFile = java.net.URLEncoder.encode(originalDisplayName, "UTF-8")
        val encTok = java.net.URLEncoder.encode(currentBroadcastToken, "UTF-8")

        val uploadUrl = "http://$teacherHost:$webPort/api/files/upload?code=$encCode&name=$encName&filename=$encFile&token=$encTok"
        try {
            val totalLen = file.length()
            val conn = (URL(uploadUrl).openConnection() as HttpURLConnection).apply {
                requestMethod = "POST"
                doOutput = true
                doInput = true
                useCaches = false
                connectTimeout = 8000
                readTimeout = 90000
                setFixedLengthStreamingMode(totalLen)
                setRequestProperty("Content-Type", "application/octet-stream")
                setRequestProperty("Connection", "close")
            }

            var sent = 0L
            var lastPct = -1
            java.io.FileInputStream(file).use { fis ->
                conn.outputStream.use { os ->
                    val buf = ByteArray(65536)
                    var read: Int
                    while (fis.read(buf).also { read = it } > 0) {
                        os.write(buf, 0, read)
                        sent += read
                        val pct = ((sent * 100L) / totalLen).toInt().coerceIn(0, 100)
                        if (pct != lastPct) {
                            lastPct = pct
                            onProgress?.invoke(pct)
                        }
                    }
                    os.flush()
                }
            }

            val respCode = conn.responseCode
            val respBody = try {
                val stream = if (respCode in 200..299) conn.inputStream else conn.errorStream
                stream?.bufferedReader(Charsets.UTF_8)?.use { it.readText() } ?: ""
            } catch (_: Exception) { "" }
            conn.disconnect()

            var serverMsg = if (respCode in 200..299) "Nộp bài thành công!" else "Lỗi HTTP $respCode"
            var ok = respCode in 200..299
            if (respBody.isNotBlank()) {
                try {
                    val obj = org.json.JSONObject(respBody)
                    if (obj.has("ok")) ok = obj.optBoolean("ok", ok)
                    val m = obj.optString("message", "")
                    if (m.isNotBlank()) serverMsg = m
                } catch (_: Exception) {}
            }
            ok to serverMsg
        } catch (e: Exception) {
            Log.w(TAG, "uploadLocalFileToTeacher failed ($uploadUrl): ${e.message}")
            false to "Lỗi mạng khi nộp tệp: ${e.message}"
        }
    }

    private fun uploadStudentUriToTeacher(uri: android.net.Uri) {
        if (tcpSocket?.isConnected != true && currentTeacherIp.isNullOrBlank() && discoveredTeacherIp.isNullOrBlank()) {
            Toast.makeText(this, "Vui lòng kết nối vào lớp học trước khi nộp tệp!", Toast.LENGTH_SHORT).show()
            return
        }

        lifecycleScope.launch {
            layoutUploadProgress.visibility = View.VISIBLE
            progressUploadBar.progress = 0
            txtUploadPercent.text = "0%"
            btnAttachQuickFile.isEnabled = false
            btnSubmitFileFromDocs.isEnabled = false

            var tempCopy: java.io.File? = null
            try {
                val meta = withContext(Dispatchers.IO) {
                    var displayName = "BaiLam_${SimpleDateFormat("HHmmss", Locale.US).format(Date())}.dat"
                    var fileSize = -1L
                    contentResolver.query(uri, null, null, null, null)?.use { cursor ->
                        val nameIdx = cursor.getColumnIndex(android.provider.OpenableColumns.DISPLAY_NAME)
                        val sizeIdx = cursor.getColumnIndex(android.provider.OpenableColumns.SIZE)
                        if (cursor.moveToFirst()) {
                            if (nameIdx >= 0 && !cursor.isNull(nameIdx)) {
                                displayName = cursor.getString(nameIdx)
                            }
                            if (sizeIdx >= 0 && !cursor.isNull(sizeIdx)) {
                                fileSize = cursor.getLong(sizeIdx)
                            }
                        }
                    }
                    displayName to fileSize
                }

                val origName = meta.first.replace(Regex("[<>:\"|?*]"), "_").ifBlank { "BaiLam.dat" }
                if (meta.second > 200L * 1024L * 1024L) {
                    Toast.makeText(this@MainActivity, "⚠️ Tệp '$origName' vượt quá giới hạn 200 MB!", Toast.LENGTH_LONG).show()
                    return@launch
                }

                // Sao luồng 64KB sang cache cục bộ để đảm bảo Content-Length chính xác 100% với mọi ContentProvider
                val stagedFile = withContext(Dispatchers.IO) {
                    val tmp = java.io.File(cacheDir, "upload_${System.currentTimeMillis()}_$origName")
                    contentResolver.openInputStream(uri)?.use { input ->
                        java.io.FileOutputStream(tmp).use { fos ->
                            val buf = ByteArray(65536)
                            var r: Int
                            while (input.read(buf).also { r = it } > 0) {
                                fos.write(buf, 0, r)
                            }
                            fos.flush()
                        }
                    } ?: throw java.io.IOException("Không đọc được nội dung tệp đã chọn")
                    tmp
                }
                tempCopy = stagedFile

                if (stagedFile.length() > 200L * 1024L * 1024L) {
                    Toast.makeText(this@MainActivity, "⚠️ Tệp '$origName' vượt quá giới hạn 200 MB!", Toast.LENGTH_LONG).show()
                    return@launch
                }

                Toast.makeText(this@MainActivity, "📤 Đang nộp tệp '$origName' lên máy Giáo viên...", Toast.LENGTH_SHORT).show()
                val res = uploadLocalFileToTeacher(stagedFile, origName) { pct ->
                    runOnUiThread {
                        progressUploadBar.progress = pct
                        txtUploadPercent.text = "$pct%"
                    }
                }

                if (res.first) {
                    hwSubmitted = true
                    progressUploadBar.progress = 100
                    txtUploadPercent.text = "100%"
                    Toast.makeText(this@MainActivity, "✅ Đã nộp thành công '$origName' cho Giáo viên!", Toast.LENGTH_LONG).show()
                    if (currentTab == 1) {
                        switchWorkSubTab(currentWorkSubTab)
                    }
                } else {
                    Toast.makeText(this@MainActivity, "❌ Không nộp được '$origName': ${res.second}", Toast.LENGTH_LONG).show()
                }
            } catch (e: Exception) {
                Toast.makeText(this@MainActivity, "❌ Lỗi chọn/nộp tệp: ${e.message}", Toast.LENGTH_LONG).show()
            } finally {
                try { tempCopy?.delete() } catch (_: Exception) {}
                delay(1200)
                layoutUploadProgress.visibility = View.GONE
                btnAttachQuickFile.isEnabled = true
                btnSubmitFileFromDocs.isEnabled = true
            }
        }
    }

    private fun performQuickNoteOrHomeworkUpload() {
        val noteText = edtQuickLessonAnswer.text.toString().trim()
        val strokeCount = quickNoteCanvas.getStrokeCount()
        if (noteText.isBlank() && strokeCount == 0) {
            Toast.makeText(this, "Vui lòng viết nháp, nhập đáp án hoặc bấm '📎 Chọn tệp nộp'!", Toast.LENGTH_SHORT).show()
            return
        }

        btnQuickSubmitWork.isEnabled = false
        btnQuickSubmitWork.text = "Đang nộp..."
        layoutUploadProgress.visibility = View.VISIBLE
        progressUploadBar.progress = 0
        txtUploadPercent.text = "0%"

        lifecycleScope.launch {
            var tempArtifact: java.io.File? = null
            try {
                val ts = SimpleDateFormat("HHmmss", Locale.US).format(Date())
                val artifact: Pair<java.io.File, String> = if (strokeCount > 0 && quickNoteCanvas.width > 0 && quickNoteCanvas.height > 0) {
                    val w = quickNoteCanvas.width
                    val extraHeaderH = if (noteText.isNotBlank()) 72 else 0
                    val h = quickNoteCanvas.height + extraHeaderH
                    val bmp = android.graphics.Bitmap.createBitmap(w, h, android.graphics.Bitmap.Config.ARGB_8888)
                    val canvas = android.graphics.Canvas(bmp)
                    canvas.drawColor(0xFFFFFFFF.toInt())
                    if (noteText.isNotBlank()) {
                        val paint = android.graphics.Paint(android.graphics.Paint.ANTI_ALIAS_FLAG).apply {
                            color = 0xFF0F172A.toInt()
                            textSize = 28f
                            typeface = Typeface.DEFAULT_BOLD
                        }
                        canvas.drawText("Đáp án: $noteText", 24f, 46f, paint)
                        canvas.translate(0f, extraHeaderH.toFloat())
                    }
                    quickNoteCanvas.draw(canvas)
                    val pngFile = java.io.File(cacheDir, "BaiNhap_$ts.png")
                    withContext(Dispatchers.IO) {
                        java.io.FileOutputStream(pngFile).use { fos ->
                            bmp.compress(android.graphics.Bitmap.CompressFormat.PNG, 100, fos)
                        }
                    }
                    bmp.recycle()
                    pngFile to pngFile.name
                } else {
                    val txtFile = java.io.File(cacheDir, "DapAn_$ts.txt")
                    withContext(Dispatchers.IO) {
                        txtFile.writeText(noteText, Charsets.UTF_8)
                    }
                    txtFile to txtFile.name
                }
                tempArtifact = artifact.first

                val uploadRes = uploadLocalFileToTeacher(artifact.first, artifact.second) { pct ->
                    runOnUiThread {
                        progressUploadBar.progress = pct
                        txtUploadPercent.text = "$pct%"
                    }
                }

                val hwIdVal = if (activeHwId > 0) activeHwId else 1
                val payloadSummary = buildString {
                    if (noteText.isNotBlank()) append(noteText.replace("|", " "))
                    if (strokeCount > 0) {
                        if (isNotEmpty()) append(" ")
                        append("[Kèm ảnh nháp $strokeCount nét]")
                    }
                }
                sendRawTcpLine("HOMEWORK_SUBMIT|$hwIdVal|$payloadSummary|[]")

                if (uploadRes.first) {
                    hwSubmitted = true
                    edtQuickLessonAnswer.setText("")
                    btnQuickSubmitWork.text = "✓ Đã nộp GV"
                    Toast.makeText(this@MainActivity, "✅ Đã nộp bài làm / trang nháp về máy Giáo viên!", Toast.LENGTH_SHORT).show()
                } else {
                    Toast.makeText(this@MainActivity, "⚠️ ${uploadRes.second}", Toast.LENGTH_LONG).show()
                    btnQuickSubmitWork.text = "Gửi bài làm / Nháp"
                }
            } catch (e: Exception) {
                Toast.makeText(this@MainActivity, "Lỗi nộp bài nháp: ${e.message}", Toast.LENGTH_SHORT).show()
            } finally {
                try { tempArtifact?.delete() } catch (_: Exception) {}
                delay(1400)
                layoutUploadProgress.visibility = View.GONE
                btnQuickSubmitWork.text = "Gửi bài làm / Nháp"
                btnQuickSubmitWork.isEnabled = true
            }
        }
    }

    private fun toggleRaiseHand() {
        val targetState = !isHandRaised
        isHandAcceptedByTeacher = false
        animateRaiseHandMicroInteraction(btnRaiseHand)
        animateRaiseHandMicroInteraction(btnFullscreenRaiseHand)
        updateHandRaiseUI(targetState)

        lifecycleScope.launch(Dispatchers.IO) {
            try {
                val os = tcpOutputStream ?: return@launch
                val msg = if (targetState) {
                    "HAND_RAISE|raised=True|reason=$currentHandRaiseReason\n"
                } else {
                    "HAND_RAISE|raised=False\n"
                }
                synchronized(os) {
                    os.write(msg.toByteArray(Charsets.UTF_8))
                    os.flush()
                }
            } catch (e: Exception) {
                Log.w(TAG, "Failed to send hand raise: ${e.message}")
            }
        }

        if (targetState) {
            Toast.makeText(this, "✋ Đã giơ tay ($currentHandRaiseReason) — Đang chờ Giáo viên mời", Toast.LENGTH_SHORT).show()
        } else {
            Toast.makeText(this, "Đã hạ tay", Toast.LENGTH_SHORT).show()
        }
    }

    private fun animateRaiseHandMicroInteraction(targetView: View) {
        try {
            targetView.performHapticFeedback(android.view.HapticFeedbackConstants.VIRTUAL_KEY)
            targetView.animate()
                .scaleX(0.94f)
                .scaleY(0.94f)
                .setDuration(70)
                .withEndAction {
                    targetView.animate()
                        .scaleX(1.04f)
                        .scaleY(1.04f)
                        .setDuration(100)
                        .withEndAction {
                            targetView.animate().scaleX(1.0f).scaleY(1.0f).setDuration(80).start()
                        }
                        .start()
                }
                .start()
        } catch (_: Exception) {}
    }

    private fun updateHandRaiseUI(raised: Boolean, acceptedByTeacher: Boolean = false) {
        isHandRaised = raised
        isHandAcceptedByTeacher = acceptedByTeacher
        runOnUiThread {
            when {
                raised && acceptedByTeacher -> {
                    btnRaiseHand.text = "🎤 GV mời phát biểu"
                    btnRaiseHand.setBackgroundResource(R.drawable.bg_pill_badge_green)
                    btnRaiseHand.setTextColor(0xFF15803D.toInt())

                    btnFullscreenRaiseHand.text = "🎤 GV mời phát biểu"
                    btnFullscreenRaiseHand.setTextColor(0xFF86EFAC.toInt())
                }
                raised -> {
                    btnRaiseHand.text = "✋ Đang giơ tay"
                    btnRaiseHand.setBackgroundResource(R.drawable.bg_pill_badge_amber)
                    btnRaiseHand.setTextColor(0xFFB45309.toInt())

                    btnFullscreenRaiseHand.text = "✋ Hạ tay"
                    btnFullscreenRaiseHand.setTextColor(0xFFFDE047.toInt())
                }
                else -> {
                    btnRaiseHand.text = "✋ Giơ tay"
                    btnRaiseHand.setBackgroundResource(R.drawable.bg_m3_outlined_pill)
                    btnRaiseHand.setTextColor(0xFFB45309.toInt())

                    btnFullscreenRaiseHand.text = "✋ Giơ tay"
                    btnFullscreenRaiseHand.setTextColor(0xFFFDE68A.toInt())
                }
            }
        }
    }

    private fun sendChatMessage() {
        val text = edtChatMessage.text.toString().trim()
        if (text.isEmpty()) return

        val channel = currentChatChannel
        if (channel == "CLASS" && classChatMuted) {
            Toast.makeText(this, "🔇 Giáo viên đang tạm khóa trò chuyện cả lớp", Toast.LENGTH_SHORT).show()
            return
        }

        val studentCode = currentStudentCode.ifBlank { edtStudentCode.text.toString().trim() }
        edtChatMessage.setText("")
        addChatMessage(currentStudentName.ifBlank { "Học sinh" }, text, isSelf = true, channel = channel)

        val wireChannel = when {
            channel == "CLASS" -> "ALL"
            channel.startsWith("GROUP_") -> channel
            else -> "TEACHER"
        }
        lifecycleScope.launch(Dispatchers.IO) {
            try {
                val os = tcpOutputStream ?: return@launch
                val chatMsg = "CHAT|$studentCode|$wireChannel|$text\n"
                synchronized(os) {
                    os.write(chatMsg.toByteArray(Charsets.UTF_8))
                    os.flush()
                }
            } catch (e: Exception) {
                Log.w(TAG, "Failed to send chat: ${e.message}")
            }
        }
    }

    private fun switchChatChannel(channel: String) {
        currentChatChannel = channel
        unreadChatChannels.remove(channel)
        updateChatTabStyles()
        renderChat()
    }

    private fun chatChannelLabel(channel: String): String = when {
        channel == "CLASS" -> "🏫 Cả lớp"
        channel.startsWith("GROUP_") ->
            chatGroups.firstOrNull { it.code == channel }?.let { "${it.emoji} ${it.name}" } ?: "👥 Tổ nhóm"
        else -> "👨‍🏫 Giáo viên"
    }

    private fun updateChatTabStyles() {
        val density = resources.displayMetrics.density
        fun dp(v: Int): Int = (v * density).toInt()
        val channels = mutableListOf("TEACHER", "CLASS")
        channels.addAll(chatGroups.map { it.code })

        chatTabBar.removeAllViews()
        channels.forEachIndexed { i, ch ->
            val active = ch == currentChatChannel
            val label = chatChannelLabel(ch)
            val btn = Button(this).apply {
                text = if (unreadChatChannels.contains(ch) && !active) "$label  ●" else label
                isAllCaps = false
                textSize = 13f
                setTypeface(null, Typeface.BOLD)
                minWidth = 0
                minimumWidth = 0
                setPadding(dp(14), 0, dp(14), 0)
                setBackgroundResource(if (active) R.drawable.bg_m3_chip_selected else R.drawable.bg_m3_chip_unselected)
                setTextColor(if (active) 0xFF1A73E8.toInt() else 0xFF475569.toInt())
                layoutParams = LinearLayout.LayoutParams(
                    LinearLayout.LayoutParams.WRAP_CONTENT,
                    dp(36)
                ).apply { if (i > 0) marginStart = dp(8) }
                setOnClickListener { switchChatChannel(ch) }
            }
            chatTabBar.addView(btn)
        }

        val isClass = currentChatChannel == "CLASS"
        txtChatChannelBadge.text = when {
            isClass && classChatMuted -> "🔇 Cả lớp đang tạm khóa"
            isClass -> "Công khai cả lớp"
            currentChatChannel.startsWith("GROUP_") -> "Nhóm học tập"
            else -> "Riêng tư với Giáo viên"
        }
        edtChatMessage.hint = when {
            isClass && classChatMuted -> "Giáo viên đang tạm khóa trò chuyện cả lớp"
            isClass -> "Nhập tin nhắn trao đổi với cả lớp..."
            currentChatChannel.startsWith("GROUP_") -> "Nhập tin nhắn thảo luận nhóm..."
            else -> "Nhập câu hỏi / tin nhắn riêng gửi Giáo viên..."
        }
    }

    private fun applyChatGroups(json: String) {
        try {
            val arr = org.json.JSONArray(json)
            val list = mutableListOf<ChatGroupChannel>()
            for (i in 0 until arr.length()) {
                val o = arr.getJSONObject(i)
                val id = o.optString("id")
                if (id.startsWith("GROUP_")) {
                    list.add(ChatGroupChannel(id, o.optString("n", "Nhóm"), o.optString("e", "👥")))
                }
            }
            runOnUiThread {
                chatGroups.clear()
                chatGroups.addAll(list)
                if (currentChatChannel.startsWith("GROUP_") && chatGroups.none { it.code == currentChatChannel }) {
                    switchChatChannel("TEACHER")
                } else {
                    updateChatTabStyles()
                }
            }
        } catch (e: Exception) {
            Log.w(TAG, "Invalid CHAT_GROUPS payload: ${e.message}")
        }
    }

    private fun renderChat() {
        chatContainer.removeAllViews()
        val density = resources.displayMetrics.density
        fun dp(v: Int): Int = (v * density).toInt()
        val filtered = chatHistory.filter { it.channel == currentChatChannel }
        if (filtered.isEmpty()) {
            val emptyChat = TextView(this).apply {
                text = when {
                    currentChatChannel == "CLASS" -> "🏫 Kênh thảo luận chung của cả lớp. Hãy giữ thái độ lịch sự khi trao đổi bài học."
                    currentChatChannel.startsWith("GROUP_") -> "👥 Không gian thảo luận riêng của nhóm học tập."
                    else -> "👨‍🏫 Tin nhắn tại đây chỉ gửi riêng trực tiếp tới Giáo viên."
                }
                textSize = 13f
                gravity = Gravity.CENTER
                setTextColor(0xFF64748B.toInt())
                setBackgroundResource(R.drawable.bg_m3_tonal_card)
                setPadding(dp(16), dp(14), dp(16), dp(14))
                layoutParams = LinearLayout.LayoutParams(
                    LinearLayout.LayoutParams.MATCH_PARENT,
                    LinearLayout.LayoutParams.WRAP_CONTENT
                ).apply { bottomMargin = dp(10) }
            }
            chatContainer.addView(emptyChat)
            return
        }
        filtered.forEach { appendChatBubble(it) }
    }

    private fun addChatMessage(sender: String, message: String, isSelf: Boolean, channel: String = "TEACHER") {
        runOnUiThread {
            val entry = ChatEntry(
                sender, message, isSelf,
                SimpleDateFormat("HH:mm", Locale.getDefault()).format(Date()),
                channel
            )
            chatHistory.add(entry)
            if (entry.channel == currentChatChannel) {
                if (chatHistory.count { it.channel == currentChatChannel } == 1) {
                    renderChat()
                } else {
                    appendChatBubble(entry)
                }
            } else {
                unreadChatChannels.add(entry.channel)
                updateChatTabStyles()
            }
        }
    }

    private fun appendChatBubble(entry: ChatEntry) {
        val sender = entry.sender
        val message = entry.message
        val isSelf = entry.isSelf
        val isTeacherMsg = !isSelf && sender.contains("Giáo viên", ignoreCase = true)
        val density = resources.displayMetrics.density
        val padH = (14 * density).toInt()
        val padV = (10 * density).toInt()
        val marginV = (5 * density).toInt()

        val bubbleLayout = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            layoutParams = LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT,
                LinearLayout.LayoutParams.WRAP_CONTENT
            ).apply { setMargins(0, marginV, 0, marginV) }
        }

        val senderLabel = TextView(this).apply {
            text = if (isSelf) "Bạn • ${entry.time}" else "$sender • ${entry.time}"
            textSize = 11.5f
            setTextColor(
                when {
                    isSelf -> 0xFF1A73E8.toInt()
                    isTeacherMsg -> 0xFF4338CA.toInt()
                    else -> 0xFF64748B.toInt()
                }
            )
            setTypeface(null, Typeface.BOLD)
            layoutParams = LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.WRAP_CONTENT,
                LinearLayout.LayoutParams.WRAP_CONTENT
            ).apply {
                gravity = if (isSelf) Gravity.END else Gravity.START
                bottomMargin = (3 * density).toInt()
            }
        }
        bubbleLayout.addView(senderLabel)

        val msgTextView = TextView(this).apply {
            text = message
            textSize = 14f
            setPadding(padH, padV, padH, padV)
            when {
                isSelf -> {
                    setBackgroundResource(R.drawable.bg_chat_bubble_self)
                    setTextColor(0xFFFFFFFF.toInt())
                }
                isTeacherMsg -> {
                    setBackgroundResource(R.drawable.bg_chat_bubble_teacher)
                    setTextColor(0xFF1E1B4B.toInt())
                }
                else -> {
                    setBackgroundResource(R.drawable.bg_chat_bubble_peer)
                    setTextColor(0xFF0F172A.toInt())
                }
            }
            maxWidth = (resources.displayMetrics.widthPixels * 0.68).toInt()
            layoutParams = LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.WRAP_CONTENT,
                LinearLayout.LayoutParams.WRAP_CONTENT
            ).apply {
                gravity = if (isSelf) Gravity.END else Gravity.START
            }
        }
        bubbleLayout.addView(msgTextView)

        chatContainer.addView(bubbleLayout)
        scrollChat.post { scrollChat.fullScroll(View.FOCUS_DOWN) }
    }

    private fun savePreferences(name: String, code: String, password: String, ip: String, remember: Boolean) {
        getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE).edit()
            .putString(KEY_STUDENT_NAME, name)
            .putString(KEY_STUDENT_CODE, code)
            .putString(KEY_PASSWORD, password)
            .putString(KEY_LAST_IP, ip)
            .putBoolean(KEY_REMEMBER, remember)
            .apply()
    }

    private fun isAlreadyConnectedOrConnecting(): Boolean {
        if (isConnectingInProgress) return true
        val sock = tcpSocket
        if (sock != null && sock.isConnected && !sock.isClosed) return true
        if (::connectedPanel.isInitialized && connectedPanel.visibility == View.VISIBLE) return true
        return false
    }

    private suspend fun measureLanPingMs(host: String, port: Int): Long = withContext(Dispatchers.IO) {
        val t0 = System.nanoTime()
        try {
            // Đo RTT trên cổng HTTP Bridge (8080) thay vì cổng TCP Control (29877) để không gửi gói rỗng gây cảnh báo Invalid handshake
            val probePort = if (port == TCP_PORT) currentTeacherWebPort else port
            Socket().use { s ->
                s.connect(InetSocketAddress(host, probePort), 1000)
            }
            ((System.nanoTime() - t0) / 1_000_000L).coerceAtLeast(1L)
        } catch (_: Exception) {
            8L
        }
    }

    private fun updateLanSignalBadge(pingMs: Long, online: Boolean) {
        lastMeasuredPingMs = pingMs
        if (!::txtLanPingBadge.isInitialized) return
        if (!online) {
            txtLanPingBadge.text = "● Offline • Chưa thấy lớp"
            txtLanPingBadge.setBackgroundResource(R.drawable.bg_pill_badge_red)
            txtLanPingBadge.setTextColor(0xFFDC2626.toInt())
            return
        }
        txtLanPingBadge.text = "● LAN sẵn sàng • ${pingMs}ms"
        txtLanPingBadge.setBackgroundResource(R.drawable.bg_pill_badge_green)
        txtLanPingBadge.setTextColor(0xFF15803D.toInt())
    }

    private fun startAutoDiscovery() {
        if (isAlreadyConnectedOrConnecting()) return

        discoveryJob?.cancel()
        discoveryService.stopListening()

        syncStudentIdentityCard()
        txtDiscoveryHeader.text = "ĐANG TÌM LỚP HỌC..."
        txtDiscoveryDot.setTextColor(0xFFD97706.toInt())
        txtStatus.text = "Đang quét tự động lớp học mở trong mạng nội bộ..."
        progressBar.visibility = View.VISIBLE
        btnRefresh.visibility = View.GONE

        discoveryJob = lifecycleScope.launch {
            val beacons = discoveryService.discoverMultipleTeachers(windowMs = 2200)
            if (!isActive || isAlreadyConnectedOrConnecting()) return@launch

            val beacon = beacons.firstOrNull() ?: discoveryService.discoverTeacher(timeoutMs = 3800)
            if (!isActive || isAlreadyConnectedOrConnecting()) return@launch

            if (beacon != null) {
                applyDiscoveredBeacon(beacon)
                if (!isActive || isAlreadyConnectedOrConnecting()) return@launch
                progressBar.visibility = View.GONE
                btnRefresh.visibility = View.VISIBLE

                if (beacons.size >= 2) {
                    // Có từ 2 lớp học trở lên trong cùng mạng LAN -> Hiện hộp thoại chọn đúng phòng học
                    showMultiClassroomPickerDialog(beacons)
                } else if (!beacon.requirePin && !ignoredTeacherIps.contains(beacon.teacherIp) && !isUserInitiatedDisconnect && !isAlreadyConnectedOrConnecting()) {
                    // Mythware Zero-Auth Auto-Connect: Tự động vào lớp ngay nếu chỉ có 1 lớp, không khóa PIN và chưa kết nối
                    btnConnect.performClick()
                }
            } else {
                // Không nhận được beacon UDP (emulator/NAT/router chặn broadcast) -> dò cổng trực tiếp
                txtStatus.text = "Đang dò máy chủ giáo viên trong mạng nội bộ..."
                val host = probeTeacherHost()
                if (!isActive || isAlreadyConnectedOrConnecting()) return@launch

                if (host != null) {
                    discoveredTeacherIp = host
                    discoveredTeacherPort = TCP_PORT
                    val rtt = measureLanPingMs(host, TCP_PORT)
                    updateLanSignalBadge(rtt, true)
                    val info = withContext(Dispatchers.IO) { queryClassInfo(host) }
                    if (!isActive || isAlreadyConnectedOrConnecting()) return@launch

                    txtDiscoveryHeader.text = "LỚP HỌC ĐANG MỞ"
                    txtDiscoveryDot.setTextColor(0xFF16A34A.toInt())
                    if (info != null && info.size >= 3) {
                        currentClassName = info[1]
                        currentTeacherName = info[2]
                        txtStatus.text = "${info[1]}  •  GV: ${info[2]}"
                    } else {
                        txtStatus.text = "Lớp học SmartClass ($host) — Sẵn sàng kết nối"
                    }
                    edtTeacherIp.setText(host)
                } else {
                    updateLanSignalBadge(0L, false)
                    txtDiscoveryHeader.text = "CHƯA TÌM THẤY LỚP HỌC"
                    txtDiscoveryDot.setTextColor(0xFFDC2626.toInt())
                    txtStatus.text = "Hãy kiểm tra máy Giáo viên đã mở lớp hoặc bấm 'Máy chủ IP' góc trên."
                }
                progressBar.visibility = View.GONE
                btnRefresh.visibility = View.VISIBLE
            }
        }
    }

    private suspend fun applyDiscoveredBeacon(beacon: com.qasmartclass.student.services.TeacherBeaconInfo) {
        discoveredTeacherIp = beacon.teacherIp
        discoveredTeacherPort = beacon.tcpPort
        currentTeacherWebPort = beacon.webPort
        discoveredChannelId = beacon.channelId
        discoveredRequirePin = beacon.requirePin
        currentClassName = "[Kênh ${beacon.channelId}] ${beacon.className}"
        currentTeacherName = beacon.teacherName
        val rtt = measureLanPingMs(beacon.teacherIp, beacon.webPort)
        updateLanSignalBadge(rtt, true)
        txtDiscoveryHeader.text = "LỚP HỌC ĐANG MỞ (KÊNH ${beacon.channelId})"
        txtDiscoveryDot.setTextColor(0xFF16A34A.toInt())
        val pinTag = if (beacon.requirePin) " • 🔒 Nhập mã PIN" else " • 🔓 Vào tự động"
        txtStatus.text = "${beacon.className}  •  GV: ${beacon.teacherName}$pinTag"
        edtTeacherIp.setText(beacon.teacherIp)
    }

    private fun showMultiClassroomPickerDialog(beacons: List<com.qasmartclass.student.services.TeacherBeaconInfo>) {
        if (isFinishing || isDestroyed || isAlreadyConnectedOrConnecting()) return
        val items = beacons.map { b ->
            val pinLabel = if (b.requirePin) "🔒 Cần PIN" else "🔓 Vào ngay"
            "[Kênh ${b.channelId}] ${b.className} — GV: ${b.teacherName} (${b.teacherIp}) • $pinLabel"
        }.toTypedArray()

        AlertDialog.Builder(this)
            .setTitle("🏫 Chọn Lớp học / Phòng học đang mở (${beacons.size} lớp)")
            .setItems(items) { _, which ->
                val chosen = beacons.getOrNull(which) ?: return@setItems
                lifecycleScope.launch {
                    applyDiscoveredBeacon(chosen)
                    if (!chosen.requirePin && !isAlreadyConnectedOrConnecting()) {
                        btnConnect.performClick()
                    } else if (chosen.requirePin) {
                        Toast.makeText(this@MainActivity, "Vui lòng nhập Mã lớp / PIN của lớp ${chosen.className} rồi bấm Kết nối", Toast.LENGTH_LONG).show()
                    }
                }
            }
            .setNegativeButton("Đóng", null)
            .show()
    }

    /** Hỏi lớp học qua TCP: gửi WHO|, server trả QASC|lớp|giáo viên|ip|cổng. */
    private fun queryClassInfo(host: String): List<String>? {
        return try {
            Socket().use { s ->
                s.connect(InetSocketAddress(host, TCP_PORT), 1500)
                s.soTimeout = 2000
                s.getOutputStream().apply { write("WHO|".toByteArray(Charsets.UTF_8)); flush() }
                val line = s.getInputStream().bufferedReader(Charsets.UTF_8).readLine()?.trim() ?: return null
                val parts = line.split("|")
                if (parts.firstOrNull() == "QASC") parts else null
            }
        } catch (e: Exception) {
            null
        }
    }

    private fun canConnect(host: String, port: Int, timeoutMs: Int): Boolean {
        return try {
            Socket().use { s ->
                s.connect(InetSocketAddress(host, port), timeoutMs)
                true
            }
        } catch (e: Exception) {
            false
        }
    }

    /** Dò cổng máy chủ giáo viên: ưu tiên cổng HTTP 8080 trước để tránh tạo kết nối TCP rỗng trên cổng 29877. */
    private suspend fun probeTeacherHost(): String? = withContext(Dispatchers.IO) {
        val priority = LinkedHashSet<String>()
        val saved = edtTeacherIp.text.toString().trim().substringBefore(":")
        if (saved.isNotBlank()) priority.add(saved)
        priority.add("10.0.2.2")
        for (h in priority) {
            if (canConnect(h, currentTeacherWebPort, 600) || canConnect(h, TCP_PORT, 700)) return@withContext h
        }

        val prefixes = LinkedHashSet<String>()
        try {
            for (ni in java.net.NetworkInterface.getNetworkInterfaces()) {
                if (!ni.isUp || ni.isLoopback) continue
                for (addr in ni.inetAddresses) {
                    if (addr is java.net.Inet4Address && !addr.isLoopbackAddress) {
                        val p = addr.hostAddress?.substringBeforeLast(".") ?: continue
                        if (p != "10.0.2") prefixes.add(p)
                    }
                }
            }
        } catch (e: Exception) {
            Log.w(TAG, "Enumerate interfaces failed: ${e.message}")
        }

        for (prefix in prefixes) {
            val found = kotlinx.coroutines.CompletableDeferred<String?>()
            val jobs = (1..254).map { i ->
                launch {
                    val h = "$prefix.$i"
                    if (canConnect(h, currentTeacherWebPort, 550)) found.complete(h)
                }
            }
            launch { jobs.forEach { it.join() }; found.complete(null) }
            val r = found.await()
            jobs.forEach { it.cancel() }
            if (r != null) return@withContext r
        }
        null
    }

    private fun connectToTeacher(
        ip: String,
        port: Int,
        studentName: String,
        studentCode: String,
        password: String,
        classCodeInput: String
    ) {
        if (isConnectingInProgress) return

        // Dừng ngay mọi luồng quét UDP hoặc tự động kết nối lại cũ để không tạo vòng lặp tự đá socket
        discoveryJob?.cancel()
        discoveryJob = null
        discoveryService.stopListening()
        reconnectJob?.cancel()
        reconnectJob = null
        heartbeatJob?.cancel()
        heartbeatJob = null
        connectionJob?.cancel()

        val sessionGen = connectionGeneration.incrementAndGet()
        isConnectingInProgress = true

        val staleSocket = tcpSocket
        tcpSocket = null
        tcpOutputStream = null
        try { staleSocket?.close() } catch (_: Exception) {}

        currentTeacherIp = ip
        currentStudentName = studentName
        currentStudentCode = studentCode

        txtStatus.text = "Đang kết nối tới máy chủ SmartClass ($ip)..."
        progressBar.visibility = View.VISIBLE
        btnConnect.isEnabled = false
        txtError.visibility = View.GONE
        if (::lanReconnectBanner.isInitialized) lanReconnectBanner.visibility = View.GONE

        connectionJob = lifecycleScope.launch(Dispatchers.IO) {
            var openedSocket: Socket? = null
            try {
                val socket = Socket()
                openedSocket = socket
                socket.connect(InetSocketAddress(ip, port), 5000)
                if (connectionGeneration.get() != sessionGen) {
                    try { socket.close() } catch (_: Exception) {}
                    return@launch
                }

                tcpSocket = socket
                val output = socket.getOutputStream()
                val input = socket.getInputStream()
                tcpOutputStream = output

                // Send Native JOIN handshake with unique Android device tag, password, class code, and Mythware Room PIN
                // Format: JOIN|name|code|pcName|version|pubKeyBase64|macAddress|password|classCode|PIN=xxx
                val pinSegment = if (classCodeInput.isNotBlank()) "|PIN=$classCodeInput" else ""
                val devicePcName = buildAndroidPcName()
                val joinMsg = "JOIN|$studentName|$studentCode|$devicePcName|1.0|||$password|$classCodeInput$pinSegment\n"
                output.write(joinMsg.toByteArray(Charsets.UTF_8))
                output.flush()

                // Read ACK (tách riêng dòng OK| khỏi các gói ENC_CMD| nối liền phía sau trên luồng TCP)
                val buffer = ByteArray(2048)
                val len = input.read(buffer)
                if (len <= 0) {
                    throw Exception("Máy chủ Giáo viên đã đóng kết nối. Vui lòng thử lại!")
                }
                val rawAck = String(buffer, 0, len, Charsets.UTF_8).trim()
                val ack = rawAck
                    .removePrefix("\uFEFF")
                    .substringBefore("\n")
                    .substringBefore("ENC_CMD|")
                    .trim()
                Log.i(TAG, "Server ACK: $ack")

                if (ack.startsWith("OK|") || ack == "OK") {
                    val ackParts = ack.split("|")
                    // Windows Server ACK format: OK|{Guid}|{serverTimestamp}|{encryptedSessionKeyBase64}|{classCode}|{sessionSalt}|{studentName}|{studentCode}
                    if (ackParts.size >= 4 && ackParts[3].isNotBlank()) {
                        currentSessionKey = ackParts[3].trim()
                    }
                    if (ackParts.size >= 5 && ackParts[4].isNotBlank()) {
                        currentClassCode = ackParts[4].trim()
                        if (currentClassName.isBlank()) {
                            currentClassName = "Lớp ${ackParts[4].trim()}"
                        }
                    }
                    lastEnteredClassCode = classCodeInput.ifBlank { currentClassCode }
                    if (ackParts.size >= 6 && ackParts[5].isNotBlank()) {
                        currentSessionSalt = ackParts[5].substringBefore("ENC_CMD").trim()
                    }
                    val serverStudentName = if (ackParts.size >= 7) ackParts[6].trim() else ""
                    val serverStudentCode = if (ackParts.size >= 8 && isValidStudentCode(ackParts[7])) {
                        ackParts[7].trim()
                    } else {
                        sanitizeStudentCode(studentCode)
                    }
                    val resolvedStudentName = if (isValidStudentName(serverStudentName)) {
                        serverStudentName
                    } else {
                        resolveOfficialStudentName(serverStudentCode, studentName)
                    }
                    currentStudentName = resolvedStudentName
                    currentStudentCode = serverStudentCode
                    hasPromptedCaptureThisSession = false
                    isConnectingInProgress = false
                    Log.i(TAG, "Handshake OK - Student: $resolvedStudentName ($serverStudentCode), ClassCode: $currentClassCode")

                    withContext(Dispatchers.Main) {
                        if (connectionGeneration.get() == sessionGen) {
                            txtError.visibility = View.GONE
                            edtStudentName.setText(resolvedStudentName)
                            edtStudentCode.setText(serverStudentCode)
                            if (chkRemember.isChecked) {
                                // Chỉ lưu mã gốc người dùng chọn vào bộ nhớ dài hạn (tránh lưu đè hậu tố tạm nếu trùng máy khác)
                                val prefCodeToSave = if (serverStudentCode.startsWith("${sanitizeStudentCode(studentCode)}_")) {
                                    sanitizeStudentCode(studentCode)
                                } else {
                                    serverStudentCode
                                }
                                val prefNameToSave = if (prefCodeToSave != serverStudentCode && isValidStudentName(studentName)) {
                                    studentName
                                } else {
                                    resolvedStudentName
                                }
                                savePreferences(prefNameToSave, prefCodeToSave, password, ip, true)
                            }
                            onConnectedSuccess(resolvedStudentName, serverStudentCode, ip, currentClassName, currentTeacherName)
                        }
                    }

                    if (connectionGeneration.get() != sessionGen) return@launch

                    // Start Heartbeat & Focus reporter
                    startHeartbeatLoop(serverStudentCode)
                    focusReporter.startReporting()

                    // Start reading commands from Teacher bound strictly to this session generation
                    readTcpStream(input, socket, sessionGen)
                } else if (ack.startsWith("ERR|")) {
                    val parts = ack.split("|")
                    val errMsg = if (parts.size >= 3) parts[2] else (if (parts.size >= 2) parts[1] else "Đăng nhập không thành công")
                    throw Exception(errMsg)
                } else {
                    throw Exception("Không thể xác thực phiên kết nối với máy Giáo viên. Vui lòng thử lại!")
                }
            } catch (e: kotlinx.coroutines.CancellationException) {
                if (connectionGeneration.get() == sessionGen) {
                    isConnectingInProgress = false
                }
                try { openedSocket?.close() } catch (_: Exception) {}
                throw e
            } catch (e: Exception) {
                try { openedSocket?.close() } catch (_: Exception) {}
                if (connectionGeneration.get() == sessionGen) {
                    isConnectingInProgress = false
                    if (tcpSocket === openedSocket) {
                        tcpSocket = null
                        tcpOutputStream = null
                    }
                    Log.e(TAG, "Connection error: ${e.message}")
                    val friendlyMsg = formatFriendlyErrorMessage(e)
                    withContext(Dispatchers.Main) {
                        showError(friendlyMsg)
                    }
                }
            }
        }
    }

    private fun onConnectedSuccess(
        name: String,
        code: String,
        ip: String,
        className: String,
        teacherName: String
    ) {
        isUserInitiatedDisconnect = false
        if (::lanReconnectBanner.isInitialized) lanReconnectBanner.visibility = View.GONE
        syncStudentIdentityCard()
        discoveryPanel.visibility = View.GONE
        connectedPanel.visibility = View.VISIBLE
        btnConnect.isEnabled = true
        progressBar.visibility = View.GONE

        txtConnectedClassInfo.text = "Lớp: $className"
        txtStudentDetail.text = "$name ($code)"
        txtTeacherDetail.text = "Giáo viên: ${teacherName.ifBlank { "Đang kết nối" }}"
        txtServerIpDetail.text = "Máy chủ: $ip:$TCP_PORT"
        messageCard.visibility = View.GONE

        updateHandRaiseUI(false)
        selectHandRaiseReason("Phát biểu")
        homeStudentName = name
        homeClassName = className.trim()
        homeTeacherName = teacherName
        isLessonActive = false
        activeLessonTitle = ""
        activeLessonSubject = ""
        activeLessonStage = 0
        hasTeacherNotice = false
        requestScreenCapture()

        // Reset broadcast UI state and open directly into 📺 Lớp học (Tab 0)
        stopScreenBroadcast()
        refreshHome()
        refreshReceivedFilesPanel()
        switchTab(0)

        // Yêu cầu máy chủ đồng bộ trạng thái tiết học & trình chiếu hiện tại (nếu Giáo viên đã bắt đầu tiết trước đó)
        requestCurrentLessonAndBroadcastStatus()

        Toast.makeText(this, "Đã kết nối trực tiếp với lớp học!", Toast.LENGTH_SHORT).show()
    }

    private fun requestCurrentLessonAndBroadcastStatus() {
        lifecycleScope.launch(Dispatchers.IO) {
            try {
                delay(250)
                val os = tcpOutputStream ?: return@launch
                synchronized(os) {
                    os.write("CMD|GET_BROADCAST_STATUS\n".toByteArray(Charsets.UTF_8))
                    os.flush()
                }
            } catch (e: Exception) {
                Log.w(TAG, "Failed to request broadcast/lesson status: ${e.message}")
            }
        }
    }

    private fun startHeartbeatLoop(studentCode: String) {
        heartbeatJob?.cancel()
        heartbeatJob = lifecycleScope.launch(Dispatchers.IO) {
            while (isActive && tcpSocket?.isConnected == true) {
                delay(10000)
                try {
                    val hb = "HB|$studentCode|ACTIVE\n"
                    val os = tcpOutputStream ?: break
                    synchronized(os) {
                        os.write(hb.toByteArray(Charsets.UTF_8))
                        os.flush()
                    }
                } catch (e: Exception) {
                    Log.w(TAG, "Heartbeat write failed: ${e.message}")
                    break
                }
            }
        }
    }

    private suspend fun readTcpStream(input: InputStream, ownerSocket: Socket, sessionGen: Long) = withContext(Dispatchers.IO) {
        val buffer = ByteArray(4096)
        val msgBuffer = StringBuilder()
        try {
            while (connectionGeneration.get() == sessionGen && ownerSocket.isConnected && !ownerSocket.isClosed) {
                val bytesRead = input.read(buffer)
                if (bytesRead == -1) break

                val chunk = String(buffer, 0, bytesRead, Charsets.UTF_8)
                msgBuffer.append(chunk)

                var accumulated = msgBuffer.toString()
                var nlIdx = accumulated.indexOf('\n')
                while (nlIdx >= 0) {
                    val line = accumulated.substring(0, nlIdx).removePrefix("\uFEFF").trim()
                    accumulated = accumulated.substring(nlIdx + 1)
                    if (line.isNotEmpty()) {
                        var processed = line
                        // Server gửi "HB_ACK" không có ký tự xuống dòng nên dính liền gói kế tiếp
                        while (processed.startsWith("HB_ACK")) {
                            processed = processed.removePrefix("HB_ACK").trim()
                        }
                        if (processed.isEmpty()) {
                            nlIdx = accumulated.indexOf('\n')
                            continue
                        }
                        // 1. Giải mã ENC_CMD| bằng sessionSalt nếu có
                        if (processed.startsWith("ENC_CMD|")) {
                            val dec = CryptoHelper.decryptCommand(processed, currentSessionSalt)
                            if (dec.isNotEmpty()) processed = dec.removePrefix("\uFEFF").trim()
                        } else if (currentSessionKey.isNotEmpty()) {
                            // 2. Thử giải mã bằng SessionKey (dành cho MSG| hoặc lệnh riêng tư)
                            val dec = CryptoHelper.decrypt(processed, currentSessionKey)
                            if (dec.isNotEmpty()) processed = dec.removePrefix("\uFEFF").trim()
                        }

                        if (processed.isNotEmpty()) {
                            handleTcpCommand(processed)
                        }
                    }
                    nlIdx = accumulated.indexOf('\n')
                }
                msgBuffer.clear()
                if (accumulated.isNotEmpty()) {
                    msgBuffer.append(accumulated)
                }
            }
        } catch (e: Exception) {
            if (connectionGeneration.get() == sessionGen) {
                Log.w(TAG, "Stream read ended (gen=$sessionGen): ${e.message}")
            }
        } finally {
            // Chốt chặn chống vòng lặp Reconnect giả: nếu phiên này đã bị thay thế bởi phiên mới hoặc đã chủ động ngắt, thoát êm
            if (connectionGeneration.get() == sessionGen) {
                withContext(Dispatchers.Main) {
                    if (connectionGeneration.get() == sessionGen) {
                        if (isUserInitiatedDisconnect) {
                            disconnectFromTeacher("Học sinh chủ động rời lớp")
                        } else {
                            attemptSilentLanReconnect(sessionGen)
                        }
                    }
                }
            }
        }
    }

    /**
     * Micro-interaction: Cảnh báo mất kết nối mạng LAN trên Top Banner và tự động kết nối lại tối đa 5 lần
     * mà không xóa bài nháp hoặc văng học sinh ra màn hình đăng nhập ngay lập tức.
     */
    private fun attemptSilentLanReconnect(triggerGen: Long) {
        if (connectionGeneration.get() != triggerGen) return
        if (reconnectJob?.isActive == true) return

        val ip = currentTeacherIp ?: discoveredTeacherIp
        if (ip.isNullOrBlank()) {
            disconnectFromTeacher("Mất kết nối tới máy chủ Giáo viên")
            return
        }
        val port = discoveredTeacherPort
        val name = currentStudentName.ifBlank { "Học sinh" }
        val code = currentStudentCode.ifBlank { "HS001" }
        val pwd = edtPassword.text.toString().trim().ifBlank { "123456" }
        val activeClassCode = lastEnteredClassCode.ifBlank { edtJoinClassCode.text.toString().trim() }

        lanReconnectBanner.visibility = View.VISIBLE
        lanReconnectBanner.setBackgroundColor(0xFFEA580C.toInt())

        reconnectJob = lifecycleScope.launch(Dispatchers.IO) {
            var reconnected = false
            var sessionCodeChanged = false
            for (attempt in 1..5) {
                if (!isActive || connectionGeneration.get() != triggerGen) return@launch

                withContext(Dispatchers.Main) {
                    if (connectionGeneration.get() == triggerGen) {
                        txtLanReconnectStatus.text = "Tín hiệu mạng phòng học đang yếu — Đang tự động kết nối lại (Lần $attempt/5)..."
                    }
                }
                delay(2500)
                if (!isActive || connectionGeneration.get() != triggerGen) return@launch

                var candidateSocket: Socket? = null
                try {
                    val socket = Socket()
                    candidateSocket = socket
                    socket.connect(InetSocketAddress(ip, port), 3000)
                    val output = socket.getOutputStream()
                    val input = socket.getInputStream()
                    val pinSegment = if (activeClassCode.isNotBlank()) "|PIN=$activeClassCode" else ""
                    val devicePcName = buildAndroidPcName()
                    val joinMsg = "JOIN|$name|$code|$devicePcName|1.0|||$pwd|$activeClassCode$pinSegment\n"
                    output.write(joinMsg.toByteArray(Charsets.UTF_8))
                    output.flush()

                    val buffer = ByteArray(2048)
                    val len = input.read(buffer)
                    if (len > 0) {
                        val rawAck = String(buffer, 0, len, Charsets.UTF_8).trim()
                        val ack = rawAck
                            .removePrefix("\uFEFF")
                            .substringBefore("\n")
                            .substringBefore("ENC_CMD|")
                            .trim()
                        if (ack.startsWith("OK|") || ack == "OK") {
                            if (connectionGeneration.get() != triggerGen) {
                                try { socket.close() } catch (_: Exception) {}
                                return@launch
                            }

                            val ackParts = ack.split("|")
                            if (ackParts.size >= 4 && ackParts[3].isNotBlank()) {
                                currentSessionKey = ackParts[3].trim()
                            }
                            if (ackParts.size >= 5 && ackParts[4].isNotBlank()) {
                                currentClassCode = ackParts[4].trim()
                            }
                            if (ackParts.size >= 6 && ackParts[5].isNotBlank()) {
                                currentSessionSalt = ackParts[5].substringBefore("ENC_CMD").trim()
                            }
                            if (ackParts.size >= 7 && isValidStudentName(ackParts[6].trim())) {
                                currentStudentName = ackParts[6].trim()
                            }
                            if (ackParts.size >= 8 && isValidStudentCode(ackParts[7])) {
                                currentStudentCode = ackParts[7].trim()
                            }

                            val newGen = connectionGeneration.incrementAndGet()
                            val prevSocket = tcpSocket
                            tcpSocket = socket
                            tcpOutputStream = output
                            if (prevSocket != null && prevSocket !== socket) {
                                try { prevSocket.close() } catch (_: Exception) {}
                            }
                            reconnected = true

                            withContext(Dispatchers.Main) {
                                lanReconnectBanner.setBackgroundColor(0xFF16A34A.toInt())
                                txtLanReconnectStatus.text = "Đã kết nối lại thành công với máy Giáo viên!"
                                requestCurrentLessonAndBroadcastStatus()
                            }
                            lifecycleScope.launch(Dispatchers.Main) {
                                delay(1400)
                                if (tcpSocket === socket && ::lanReconnectBanner.isInitialized) {
                                    lanReconnectBanner.visibility = View.GONE
                                }
                            }
                            startHeartbeatLoop(currentStudentCode.ifBlank { code })
                            connectionJob = lifecycleScope.launch(Dispatchers.IO) {
                                readTcpStream(input, socket, newGen)
                            }
                            break
                        } else if (ack.contains("INVALID_CLASS_CODE") || ack.contains("CLASS_CODE_REQUIRED") || ack.contains("INVALID_PIN")) {
                            sessionCodeChanged = true
                            socket.close()
                            break
                        }
                    }
                    socket.close()
                } catch (_: Exception) {
                    try { candidateSocket?.close() } catch (_: Exception) {}
                }
            }

            if (!reconnected && connectionGeneration.get() == triggerGen) {
                withContext(Dispatchers.Main) {
                    if (connectionGeneration.get() == triggerGen) {
                        lanReconnectBanner.visibility = View.GONE
                        if (sessionCodeChanged) {
                            edtJoinClassCode.setText("")
                            disconnectFromTeacher("Phiên học đã đổi Mã vào lớp mới")
                            showError("Giáo viên đã tạo Mã vào lớp mới. Em hãy nhìn lên màn hình Giáo viên để nhập mã mới nhé!")
                        } else {
                            disconnectFromTeacher("Mất kết nối mạng LAN tới máy Giáo viên")
                        }
                    }
                }
            }
        }
    }

    private fun startLockScreenTimer() {
        lockTimerJob?.cancel()
        val startMs = System.currentTimeMillis()
        lockTimerJob = lifecycleScope.launch {
            while (isActive && lockOverlay.visibility == View.VISIBLE) {
                val elapsedSec = ((System.currentTimeMillis() - startMs) / 1000L).coerceAtLeast(0L)
                val mins = elapsedSec / 60L
                val secs = elapsedSec % 60L
                txtLockTimer.text = String.format(Locale.US, "%02d : %02d", mins, secs)
                delay(1000)
            }
        }
    }

    private fun stopLockScreenTimer() {
        lockTimerJob?.cancel()
        lockTimerJob = null
    }

    private fun handleTcpCommand(rawCmd: String) {
        val normalized = rawCmd.removePrefix("\uFEFF").trim()
        Log.i(TAG, "Command received (decrypted): $normalized")
        if (normalized == "HB_ACK") return

        // 1. Loại bỏ chữ ký HMAC và timestamp nếu có (chuẩn payload|{timestamp}|{sig})
        var cleanCmd = normalized
        val parts = normalized.split("|")
        if (parts.size >= 3) {
            val tsCandidate = parts[parts.size - 2]
            if (tsCandidate.toLongOrNull() != null && tsCandidate.length in 9..12) {
                cleanCmd = normalized.substringBeforeLast("|").substringBeforeLast("|")
            }
        }

        // 2. Trích xuất ID giao dịch và gửi ACK phản hồi để máy Giáo viên biết tin đã tới
        if (cleanCmd.contains("|id=")) {
            val cmdId = cleanCmd.substringAfter("|id=").substringBefore("|")
            sendAck(cmdId, "SUCCESS")
        }

        runOnUiThread {
            when {
                cleanCmd.startsWith("CMD|KICK") -> {
                    val cmdParts = cleanCmd.substringBefore("|id=").split("|")
                    val kickedIp = cmdParts.getOrNull(2)?.trim() ?: (currentTeacherIp ?: "")
                    val kickReason = cmdParts.getOrNull(4)?.trim() ?: "Giáo viên đã mời thiết bị ra khỏi lớp để bạn chọn lại đúng phòng học."
                    if (kickedIp.isNotBlank()) {
                        ignoredTeacherIps.add(kickedIp)
                    }
                    isUserInitiatedDisconnect = true
                    lockOverlay.visibility = View.GONE
                    stopLockScreenTimer()
                    heartbeatWatchdog.releaseLock()
                    stopScreenBroadcast()
                    disconnectFromTeacher(kickReason)
                    showError("🚫 $kickReason")
                }
                cleanCmd.startsWith("CMD|UPDATE_SESSION|") -> {
                    val cmdParts = cleanCmd.substringBefore("|id=").split("|")
                    val newCode = cmdParts.getOrNull(2)?.trim() ?: ""
                    val newSalt = cmdParts.getOrNull(3)?.trim() ?: ""
                    if (newCode.isNotBlank()) {
                        currentClassCode = newCode
                        lastEnteredClassCode = newCode
                    }
                    if (newSalt.isNotBlank()) {
                        currentSessionSalt = newSalt
                    }
                }
                cleanCmd.startsWith("CMD|REQUEST_SCREENSHOT") -> {
                    sendScreenshotToTeacher()
                }
                cleanCmd.startsWith("CMD|BLACK_SCREEN") -> {
                    val cmdParts = cleanCmd.substringBefore("|id=").split("|")
                    val on = cmdParts.getOrNull(2)?.trim() == "1"
                    if (on) {
                        txtLockTitle.text = "THẦY CÔ ĐANG GIẢNG BÀI - XIN MỜI CHÚ Ý LÊN BẢNG"
                        txtLockMessage.text = "Màn hình thiết bị đã chuyển sang chế độ tập trung nghe giảng."
                        txtLockLessonName.text = "Bài học: ${activeLessonTitle.ifBlank { homeClassName.ifBlank { "Trên bảng chính" } }}"
                        lockOverlay.setBackgroundColor(0xFF000000.toInt())
                        lockOverlay.bringToFront()
                        lockOverlay.visibility = View.VISIBLE
                        setImmersiveFullscreen(true)
                        startLockScreenTimer()
                    } else {
                        lockOverlay.visibility = View.GONE
                        stopLockScreenTimer()
                        setImmersiveFullscreen(isScreenBroadcastActive && broadcastOverlay.visibility == View.VISIBLE && isForceBroadcastMode)
                    }
                }
                cleanCmd.startsWith("CMD|LOCK") -> {
                    val cmdParts = cleanCmd.substringBefore("|id=").split("|")
                    val arg = cmdParts.getOrNull(2)?.trim()
                    val on = arg != "0" && !arg.equals("false", ignoreCase = true)
                    if (on) {
                        txtLockTitle.text = "THẦY CÔ ĐANG GIẢNG BÀI - XIN MỜI CHÚ Ý LÊN BẢNG"
                        txtLockMessage.text = "Màn hình máy tính bảng đang tạm khóa thao tác để cả lớp tập trung nghe giảng."
                        txtLockLessonName.text = "Bài học: ${activeLessonTitle.ifBlank { homeClassName.ifBlank { "Trên bảng chính" } }}"
                        lockOverlay.setBackgroundColor(0xF80B0F19.toInt())
                        lockOverlay.bringToFront()
                        lockOverlay.visibility = View.VISIBLE
                        setImmersiveFullscreen(true)
                        startLockScreenTimer()
                        heartbeatWatchdog.acquireLock()
                        Toast.makeText(this, "🔒 Giáo viên đã khóa màn hình tập trung nghe giảng", Toast.LENGTH_SHORT).show()
                    } else {
                        lockOverlay.visibility = View.GONE
                        stopLockScreenTimer()
                        heartbeatWatchdog.releaseLock()
                        setImmersiveFullscreen(isScreenBroadcastActive && broadcastOverlay.visibility == View.VISIBLE && isForceBroadcastMode)
                        Toast.makeText(this, "🔓 Giáo viên đã mở khóa màn hình", Toast.LENGTH_SHORT).show()
                    }
                }
                cleanCmd.startsWith("CMD|UNLOCK") -> {
                    lockOverlay.visibility = View.GONE
                    stopLockScreenTimer()
                    heartbeatWatchdog.releaseLock()
                    setImmersiveFullscreen(isScreenBroadcastActive && broadcastOverlay.visibility == View.VISIBLE && isForceBroadcastMode)
                    Toast.makeText(this, "🔓 Giáo viên đã mở khóa màn hình", Toast.LENGTH_SHORT).show()
                }
                cleanCmd.startsWith("CMD|HAND_ACK") -> {
                    updateHandRaiseUI(raised = true, acceptedByTeacher = true)
                    Toast.makeText(this, "Giáo viên đã nhận tín hiệu — Mời em phát biểu!", Toast.LENGTH_LONG).show()
                }
                cleanCmd.startsWith("CMD|LESSON_START") -> {
                    val cmdParts = cleanCmd.substringBefore("|id=").split("|")
                    val titleEnc = cmdParts.getOrNull(3) ?: ""
                    val subjectEnc = cmdParts.getOrNull(4) ?: ""
                    val teacherEnc = cmdParts.getOrNull(5) ?: ""
                    val stageVal = cmdParts.getOrNull(6)?.toIntOrNull() ?: 2
                    val summaryEnc = cmdParts.getOrNull(7) ?: ""

                    activeLessonTitle = try { URLDecoder.decode(titleEnc, "UTF-8") } catch (_: Exception) { titleEnc }
                    activeLessonSubject = try { URLDecoder.decode(subjectEnc, "UTF-8") } catch (_: Exception) { subjectEnc }
                    activeLessonSummary = try { URLDecoder.decode(summaryEnc, "UTF-8") } catch (_: Exception) { summaryEnc }
                    val decodedTeacher = try { URLDecoder.decode(teacherEnc, "UTF-8") } catch (_: Exception) { teacherEnc }
                    if (decodedTeacher.isNotBlank()) {
                        homeTeacherName = decodedTeacher
                        txtTeacherDetail.text = "Giáo viên: $decodedTeacher"
                    }
                    activeLessonStage = if (stageVal >= 2) stageVal else 2
                    isLessonActive = true
                    hasTeacherNotice = true
                    refreshHome()
                    Toast.makeText(this, "📖 Giáo viên đã bắt đầu tiết học: ${activeLessonTitle.ifBlank { "Bài giảng mới" }}", Toast.LENGTH_SHORT).show()
                }
                cleanCmd.startsWith("CMD|LESSON_FOCUS|") -> {
                    val cmdParts = cleanCmd.substringBefore("|id=").split("|")
                    val sortOrder = cmdParts.getOrNull(2)?.toIntOrNull() ?: 1
                    val dataEnc = cmdParts.getOrNull(4) ?: ""
                    val decodedData = try { URLDecoder.decode(dataEnc, "UTF-8") } catch (_: Exception) { dataEnc }
                    isLessonActive = true
                    if (decodedData.isNotBlank()) {
                        activeLessonSummary = decodedData.replace("\\n", "\n")
                    }
                    refreshHome()
                    Toast.makeText(this, "🎯 Giáo viên yêu cầu tập trung vào nội dung #$sortOrder", Toast.LENGTH_SHORT).show()
                }
                cleanCmd.startsWith("CMD|LESSON_UNFOCUS") -> {
                    refreshHome()
                }
                cleanCmd.startsWith("CMD|LESSON_STAGE|") -> {
                    val stageVal = parts.getOrNull(2)?.substringBefore("|")?.toIntOrNull() ?: 0
                    if (stageVal >= 2 && isLessonActive) {
                        activeLessonStage = stageVal
                        refreshHome()
                    }
                }
                cleanCmd.startsWith("CMD|LESSON_END") -> {
                    isLessonActive = false
                    activeLessonStage = 0
                    activeLessonSummary = ""
                    refreshHome()
                    Toast.makeText(this, "⏹️ Giáo viên đã kết thúc tiết học", Toast.LENGTH_SHORT).show()
                }
                cleanCmd.startsWith("CMD|BROADCAST_ALLOW_EXIT|") -> {
                    val cmdParts = cleanCmd.substringBefore("|id=").split("|")
                    val rawFlag = cmdParts.getOrNull(2)?.trim().orEmpty()
                    val allowExit = rawFlag == "1" || rawFlag.equals("true", ignoreCase = true)
                    applyBroadcastLockState(isForce = !allowExit, notifyToast = true)
                }
                cleanCmd.startsWith("CMD|SCREEN_BROADCAST_STOP") || cleanCmd == "CMD|SCREEN_BROADCAST_STOP" -> {
                    stopHighFrequencyScreenRelay()
                    stopScreenBroadcast()
                    Toast.makeText(this, "📡 Đã dừng chiếu màn hình", Toast.LENGTH_SHORT).show()
                }
                cleanCmd.startsWith("CMD|SCREEN_BROADCAST_START") || cleanCmd.startsWith("CMD|SCREEN_BROADCAST|") || cleanCmd == "CMD|SCREEN_BROADCAST" -> {
                    val cmdParts = cleanCmd.split("|")
                    var webPort = currentTeacherWebPort
                    var token = currentBroadcastToken
                    var title = ""
                    var presenterCode = ""
                    val isForce = cleanCmd.contains("FORCE_WATCH")

                    for (part in cmdParts) {
                        if (part.contains("=")) {
                            val kv = part.split("=", limit = 2)
                            if (kv.size == 2) {
                                if (kv[0] == "TITLE") {
                                    title = try { URLDecoder.decode(kv[1], "UTF-8") } catch (e: Exception) { kv[1] }
                                } else if (kv[0] == "PRESENTER") {
                                    presenterCode = kv[1].trim()
                                }
                            }
                        } else {
                            val pInt = part.toIntOrNull()
                            if (pInt != null && pInt in 1024..65535) {
                                webPort = pInt
                            } else if (part.startsWith("TK_") || (part.length in 6..64 && !part.contains("\\") && !part.contains("/") && part != "CMD" && !part.startsWith("SCREEN_BROADCAST") && part != "FORCE_WATCH")) {
                                token = part
                            }
                        }
                    }
                    if (isStudentDemoPresenting || (presenterCode.isNotBlank() && presenterCode.equals(currentStudentCode, ignoreCase = true))) {
                        // Thiết bị này đang là người trình bày trước lớp → không bật màn hình phủ lên máy mình!
                        return@runOnUiThread
                    }
                    startScreenBroadcast(token, webPort, isForce, title)
                    val toastMsg = if (title.contains("Học sinh trình bày", ignoreCase = true)) "👨‍🎓 $title" else "📺 Giáo viên đang chia sẻ màn hình"
                    Toast.makeText(this, toastMsg, Toast.LENGTH_SHORT).show()
                }
                cleanCmd.startsWith("CMD|SCREEN_BROADCAST_UPDATE") -> {
                    val cmdParts = cleanCmd.split("|")
                    var webPort = currentTeacherWebPort
                    var token = currentBroadcastToken
                    var title = ""
                    var presenterCode = ""
                    val isForceUpdate = cleanCmd.contains("FORCE_WATCH")
                    for (part in cmdParts) {
                        if (part.contains("=")) {
                            val kv = part.split("=", limit = 2)
                            if (kv.size == 2) {
                                if (kv[0] == "TITLE") {
                                    title = try { URLDecoder.decode(kv[1], "UTF-8") } catch (e: Exception) { kv[1] }
                                } else if (kv[0] == "PRESENTER") {
                                    presenterCode = kv[1].trim()
                                }
                            }
                        } else {
                            val pInt = part.toIntOrNull()
                            if (pInt != null && pInt in 1024..65535) {
                                webPort = pInt
                            } else if (part.startsWith("TK_")) {
                                token = part
                            }
                        }
                    }
                    if (isStudentDemoPresenting || (presenterCode.isNotBlank() && presenterCode.equals(currentStudentCode, ignoreCase = true))) {
                        return@runOnUiThread
                    }
                    updateScreenBroadcast(token, webPort, title, isForceUpdate)
                }
                cleanCmd.startsWith("CMD|CLASS_CHAT_MUTE|") || cleanCmd.startsWith("CMD|CLASS_CHAT_MUTE_STUDENT|") -> {
                    // Giáo viên khóa/mở trò chuyện cả lớp (chỉ ảnh hưởng kênh "Nhắn với lớp")
                    val flag = if (cleanCmd.startsWith("CMD|CLASS_CHAT_MUTE_STUDENT|")) parts.getOrNull(3) else parts.getOrNull(2)
                    classChatMuted = flag?.trim().equals("true", ignoreCase = true)
                    runOnUiThread { updateChatTabStyles() }
                }
                cleanCmd.startsWith("CMD|CHAT_GROUPS|") -> {
                    // GV đồng bộ danh sách nhóm chat mà HS là thành viên
                    applyChatGroups(cleanCmd.removePrefix("CMD|CHAT_GROUPS|"))
                }
                cleanCmd.startsWith("MSG|") || cleanCmd.startsWith("CMD|MESSAGE|") || cleanCmd.startsWith("CMD|CHAT|") || cleanCmd.startsWith("CHAT|") -> {
                    var msgText = when {
                        cleanCmd.startsWith("MSG|") -> cleanCmd.removePrefix("MSG|")
                        cleanCmd.startsWith("CMD|MESSAGE|") && parts.size >= 3 -> parts[2]
                        cleanCmd.startsWith("CMD|CHAT|") && parts.size >= 4 -> parts[3]
                        cleanCmd.startsWith("CMD|CHAT|") && parts.size >= 3 -> parts[2]
                        cleanCmd.startsWith("CHAT|") && parts.size >= 4 -> parts[3]
                        cleanCmd.startsWith("CHAT|") && parts.size >= 3 -> parts[2]
                        else -> cleanCmd.substringAfter("|")
                    }
                    if (msgText.contains("|id=")) {
                        msgText = msgText.substringBefore("|id=")
                    }

                    // 🔒 = nhắn riêng từ GV → kênh Giáo viên; 📨 [Tên nhóm] → kênh Nhóm; 👥 [Tên HS]: → tin nhắn Cả lớp từ bạn học; còn lại → kênh Cả lớp
                    val trimmedMsg = msgText.trimStart()
                    val peerClassMatch = Regex("^👥\\s*\\[(.+?)\\]:\\s*(.*)$").find(trimmedMsg)
                    val groupName = Regex("^📨\\s*\\[(.+?)\\]").find(trimmedMsg)?.groupValues?.get(1)
                    val incomingChannel = when {
                        trimmedMsg.startsWith("🔒") -> "TEACHER"
                        groupName != null -> chatGroups.firstOrNull { it.name == groupName }?.code ?: "CLASS"
                        else -> "CLASS"
                    }
                    val senderLabel = if (peerClassMatch != null) "👨‍🎓 ${peerClassMatch.groupValues[1]}" else "👨‍🏫 Giáo viên"
                    val displayBody = if (peerClassMatch != null) peerClassMatch.groupValues[2] else msgText
                    addChatMessage(senderLabel, displayBody, isSelf = false, channel = incomingChannel)
                    if (peerClassMatch == null && (cleanCmd.startsWith("MSG|") || cleanCmd.startsWith("CMD|MESSAGE|"))) {
                        messageCard.visibility = View.VISIBLE
                        txtTeacherMessage.text = msgText
                        hasTeacherNotice = true
                    }

                    if (currentTab != 2) {
                        unreadTeacherChatCount++
                        badgeRailChat.text = unreadTeacherChatCount.toString()
                        badgeRailChat.visibility = View.VISIBLE
                    }
                    Toast.makeText(this, if (peerClassMatch != null) "💬 ${peerClassMatch.groupValues[1]}: $displayBody" else "📩 Tin nhắn từ GV: $msgText", Toast.LENGTH_LONG).show()
                }
                cleanCmd.startsWith("CMD|UPDATE_SESSION|") -> {
                    val updateParts = cleanCmd.split("|")
                    if (updateParts.size >= 4) {
                        currentClassCode = updateParts[2]
                        currentSessionSalt = updateParts[3]
                        Log.i(TAG, "Updated session: ClassCode=$currentClassCode, SessionSalt=${currentSessionSalt.take(6)}...")
                        requestCurrentLessonAndBroadcastStatus()
                    }
                }
                cleanCmd.startsWith("CMD|QUIZ_DATA|") -> {
                    val dataParts = cleanCmd.substringBefore("|id=").split("|", limit = 4)
                    val qId = dataParts.getOrNull(2)?.toIntOrNull() ?: 1
                    val jsonStr = dataParts.getOrNull(3) ?: "[]"
                    try {
                        val arr = org.json.JSONArray(jsonStr)
                        activeQuizQuestions.clear()
                        selectedQuizAnswers.clear()
                        quizSubmitted = false
                        lastQuizScoreText = ""
                        lastQuizCorrectDetail = ""
                        activeQuizId = qId
                        for (i in 0 until arr.length()) {
                            val o = arr.getJSONObject(i)
                            val id = if (o.has("id")) o.optInt("id", i + 1) else o.optInt("Id", i + 1)
                            val content = o.optString("content", o.optString("Content", "Câu hỏi ${i + 1}"))
                            val points = o.optDouble("points", o.optDouble("Points", 10.0))
                            val optsRaw = o.optString("optionsJson", o.optString("OptionsJson", "[]"))
                            val optsList = mutableListOf<String>()
                            try {
                                val optArr = org.json.JSONArray(optsRaw)
                                for (j in 0 until optArr.length()) {
                                    optsList.add(optArr.optString(j))
                                }
                            } catch (_: Exception) {}
                            if (optsList.isEmpty()) {
                                optsList.addAll(listOf("Đáp án A", "Đáp án B", "Đáp án C", "Đáp án D"))
                            }
                            activeQuizQuestions.add(QuizQuestionItem(id, content, optsList, points))
                        }
                        openWorkSubTabFromTeacherCommand("QUIZ")
                        Toast.makeText(this, "Giáo viên đã phát đề kiểm tra (${activeQuizQuestions.size} câu)", Toast.LENGTH_LONG).show()
                    } catch (e: Exception) {
                        Log.w(TAG, "Failed to parse CMD|QUIZ_DATA: ${e.message}")
                    }
                }
                cleanCmd.startsWith("CMD|QUIZ_START") -> {
                    val qId = cleanCmd.substringBefore("|id=").split("|").getOrNull(2)?.toIntOrNull() ?: activeQuizId
                    if (qId > 0) activeQuizId = qId
                    quizSubmitted = false
                    if (activeQuizQuestions.isNotEmpty()) {
                        openWorkSubTabFromTeacherCommand("QUIZ")
                    }
                }
                cleanCmd.startsWith("CMD|QUIZ_GRADE|") -> {
                    val gParts = cleanCmd.substringBefore("|id=").split("|")
                    val finalScore = gParts.getOrNull(3) ?: "0"
                    val correctCount = gParts.getOrNull(4) ?: "0"
                    val totalQ = gParts.getOrNull(5) ?: "${activeQuizQuestions.size}"
                    lastQuizScoreText = "$finalScore / 10 điểm"
                    lastQuizCorrectDetail = "Số câu đúng: $correctCount / $totalQ câu"
                    quizSubmitted = true
                    if (currentTab == 1) {
                        switchWorkSubTab(currentWorkSubTab)
                    }
                    Toast.makeText(this, "Điểm kiểm tra của bạn: $lastQuizScoreText ($correctCount/$totalQ câu đúng)", Toast.LENGTH_LONG).show()
                }
                cleanCmd.startsWith("CMD|QUIZ_END") -> {
                    quizSubmitted = true
                    if (currentTab == 1) {
                        switchWorkSubTab(currentWorkSubTab)
                    }
                    Toast.makeText(this, "Giáo viên đã kết thúc phiên kiểm tra", Toast.LENGTH_SHORT).show()
                }
                cleanCmd.startsWith("CMD|SURVEY_CUSTOM|") -> {
                    val sParts = cleanCmd.substringBefore("|id=").split("|")
                    val question = sParts.getOrNull(4) ?: "Khảo sát ý kiến lớp học"
                    activeSurveyTitle = "Khảo sát nhanh từ Giáo viên"
                    activeSurveyQuestion = question
                    activeSurveyType = "custom"
                    activeSurveyOptions.clear()
                    for (idx in 5..8) {
                        val opt = sParts.getOrNull(idx)?.trim() ?: ""
                        if (opt.isNotBlank()) activeSurveyOptions.add(opt)
                    }
                    selectedSurveyOptionIdx = -1
                    surveySubmitted = false
                    openWorkSubTabFromTeacherCommand("SURVEY")
                    Toast.makeText(this, "Giáo viên vừa gửi một khảo sát mới", Toast.LENGTH_LONG).show()
                }
                cleanCmd.contains("\"Action\":\"SURVEY_START\"") || cleanCmd.contains("\"SURVEY_START\"") -> {
                    try {
                        val jsonStr = if (cleanCmd.startsWith("{")) cleanCmd else cleanCmd.substringAfter("|")
                        val root = org.json.JSONObject(jsonStr)
                        val payload = root.optJSONObject("Payload") ?: root
                        activeSurveyId = payload.optString("SurveyId", "")
                        activeSurveyTitle = payload.optString("Title", "Khảo sát lớp học")
                        activeSurveyType = payload.optString("SurveyType", "satisfaction")
                        activeSurveyQuestion = payload.optString("QuestionText", activeSurveyTitle)
                        activeSurveyOptions.clear()
                        val optArr = payload.optJSONArray("Options")
                        if (optArr != null) {
                            for (i in 0 until optArr.length()) {
                                val item = optArr.optString(i)
                                if (item.isNotBlank()) activeSurveyOptions.add(item)
                            }
                        }
                        selectedSurveyOptionIdx = -1
                        surveySubmitted = false
                        openWorkSubTabFromTeacherCommand("SURVEY")
                        Toast.makeText(this, "Giáo viên đã bắt đầu khảo sát: $activeSurveyTitle", Toast.LENGTH_LONG).show()
                    } catch (e: Exception) {
                        Log.w(TAG, "Failed to parse SURVEY_START JSON: ${e.message}")
                    }
                }
                cleanCmd.startsWith("CMD|SURVEY_END") || cleanCmd.contains("\"SURVEY_END\"") -> {
                    // Xóa sạch trạng thái khảo sát trên RAM khi Giáo viên đóng/tắt khảo sát
                    activeSurveyId = ""
                    activeSurveyTitle = ""
                    activeSurveyType = ""
                    activeSurveyQuestion = ""
                    activeSurveyOptions.clear()
                    selectedSurveyOptionIdx = -1
                    selectedSurveyStar = 0
                    surveySubmitted = false
                    if (currentTab == 1) {
                        switchWorkSubTab(currentWorkSubTab)
                    }
                    Toast.makeText(this, "Giáo viên đã kết thúc khảo sát", Toast.LENGTH_SHORT).show()
                }
                cleanCmd.startsWith("CMD|HOMEWORK|") -> {
                    val hwPayload = cleanCmd.substringBefore("|id=").removePrefix("CMD|HOMEWORK|").trim()
                    hwSubmitted = false
                    lastHwAckMessage = ""
                    hwDraftAnswer = ""
                    if (hwPayload.startsWith("{")) {
                        try {
                            val obj = org.json.JSONObject(hwPayload)
                            activeHwId = obj.optInt("id", obj.optInt("Id", 1))
                            activeHwSubject = obj.optString("subject", obj.optString("Subject", activeLessonSubject))
                            activeHwTitle = obj.optString("title", obj.optString("Title", "Bài tập mới"))
                            activeHwDesc = obj.optString("description", obj.optString("Description", ""))
                            activeHwDeadline = obj.optString("deadline", obj.optString("Deadline", ""))
                        } catch (_: Exception) {
                            activeHwTitle = "Bài tập từ Giáo viên"
                            activeHwDesc = hwPayload
                        }
                    } else {
                        activeHwTitle = hwPayload.substringBefore(":").ifBlank { "Bài tập mới" }
                        activeHwDesc = hwPayload.substringAfter(":", hwPayload)
                    }
                    openWorkSubTabFromTeacherCommand("HOMEWORK")
                    Toast.makeText(this, "Giáo viên vừa giao bài tập: $activeHwTitle", Toast.LENGTH_LONG).show()
                }
                cleanCmd.startsWith("CMD|HOMEWORK_ACK|") -> {
                    val ackMsg = cleanCmd.substringBefore("|id=").removePrefix("CMD|HOMEWORK_ACK|")
                    hwSubmitted = true
                    lastHwAckMessage = ackMsg
                    if (currentTab == 1) {
                        switchWorkSubTab(currentWorkSubTab)
                    }
                    Toast.makeText(this, ackMsg, Toast.LENGTH_SHORT).show()
                }
                cleanCmd.startsWith("CMD|HAND_DOWN") -> {
                    updateHandRaiseUI(false)
                    Toast.makeText(this, "Giáo viên đã cho phép hạ tay", Toast.LENGTH_SHORT).show()
                }
                cleanCmd.startsWith("CMD|MUTE_AUDIO") || cleanCmd.startsWith("CMD|SILENCE") || cleanCmd.startsWith("CMD|CLEAR_SILENCE") -> {
                    val muteOn = when {
                        cleanCmd.startsWith("CMD|CLEAR_SILENCE") -> false
                        cleanCmd.startsWith("CMD|SILENCE") -> true
                        else -> cleanCmd.split("|").getOrNull(2) != "0"
                    }
                    try {
                        val audioMgr = getSystemService(Context.AUDIO_SERVICE) as? android.media.AudioManager
                        if (audioMgr != null) {
                            if (muteOn) {
                                audioMgr.setStreamVolume(android.media.AudioManager.STREAM_MUSIC, 0, 0)
                                Toast.makeText(this, "🔇 Giáo viên đã tắt tiếng thiết bị (Chế độ Im lặng)", Toast.LENGTH_SHORT).show()
                            } else {
                                val maxVol = audioMgr.getStreamMaxVolume(android.media.AudioManager.STREAM_MUSIC)
                                audioMgr.setStreamVolume(android.media.AudioManager.STREAM_MUSIC, (maxVol / 2).coerceAtLeast(1), 0)
                                Toast.makeText(this, "🔊 Giáo viên đã bật lại âm thanh thiết bị", Toast.LENGTH_SHORT).show()
                            }
                        }
                    } catch (e: Exception) {
                        Log.w(TAG, "MUTE_AUDIO failed: ${e.message}")
                    }
                }
                cleanCmd.startsWith("CMD|OPEN_URL|") -> {
                    val rawUrl = cleanCmd.substringBefore("|id=").removePrefix("CMD|OPEN_URL|").trim()
                    if (rawUrl.isNotBlank()) {
                        val finalUrl = if (rawUrl.startsWith("http://", ignoreCase = true) || rawUrl.startsWith("https://", ignoreCase = true)) {
                            rawUrl
                        } else {
                            "https://$rawUrl"
                        }
                        try {
                            val browserIntent = android.content.Intent(android.content.Intent.ACTION_VIEW, android.net.Uri.parse(finalUrl)).apply {
                                addFlags(android.content.Intent.FLAG_ACTIVITY_NEW_TASK)
                            }
                            startActivity(browserIntent)
                            Toast.makeText(this, "🌐 Giáo viên đang mở trang web: $finalUrl", Toast.LENGTH_LONG).show()
                        } catch (e: Exception) {
                            Toast.makeText(this, "Không thể mở URL: $finalUrl", Toast.LENGTH_SHORT).show()
                        }
                    }
                }
                cleanCmd.startsWith("CMD|START_APP|") -> {
                    val appTarget = cleanCmd.substringBefore("|id=").removePrefix("CMD|START_APP|").trim()
                    if (appTarget.isNotBlank()) {
                        launchRequestedAndroidApp(appTarget)
                    }
                }
                cleanCmd.startsWith("CMD|STUDENT_DEMO_START") || cleanCmd.startsWith("CMD|REMOTE_CONTROL_START") || cleanCmd.startsWith("CMD|REMOTE_VIEW_START") -> {
                    val cmdParts = cleanCmd.substringBefore("|id=").split("|")
                    val targetDemoCode = cmdParts.getOrNull(2)?.trim() ?: ""
                    val isStudentDemo = cleanCmd.startsWith("CMD|STUDENT_DEMO_START")
                    if (!isStudentDemo || targetDemoCode.isBlank() || targetDemoCode.equals(currentStudentCode, ignoreCase = true)) {
                        if (isStudentDemo) {
                            isStudentDemoPresenting = true
                            stopScreenBroadcast()
                            Toast.makeText(this, "👨‍🎓 Giáo viên đang chiếu màn hình của bạn cho tất cả các bạn trong lớp cùng xem!", Toast.LENGTH_LONG).show()
                        }
                        startHighFrequencyScreenRelay(if (isStudentDemo) 75L else 150L)
                    }
                }
                cleanCmd.startsWith("CMD|STUDENT_DEMO_STOP") || cleanCmd.startsWith("CMD|REMOTE_CONTROL_STOP") || cleanCmd.startsWith("CMD|REMOTE_VIEW_STOP") -> {
                    stopHighFrequencyScreenRelay()
                }
                cleanCmd.startsWith("CMD|LOGOFF_PC") -> {
                    disconnectFromTeacher("🚪 Giáo viên đã đăng xuất phiên làm việc của thiết bị.")
                }
                cleanCmd.startsWith("CMD|SHUTDOWN_PC") || cleanCmd.startsWith("CMD|REBOOT_PC") -> {
                    txtLockTitle.text = "PHIÊN HỌC ĐÃ KẾT THÚC"
                    txtLockMessage.text = "Giáo viên đã gửi lệnh tắt/khởi động lại thiết bị cuối giờ học."
                    lockOverlay.setBackgroundColor(0xFF000000.toInt())
                    lockOverlay.visibility = View.VISIBLE
                    disconnectFromTeacher("🔌 Giáo viên đã kết thúc phiên làm việc của thiết bị.")
                }
                cleanCmd.startsWith("CMD|ASSIGNMENT|") -> {
                    // Format: CMD|ASSIGNMENT|<deadline>|<desc>
                    val rawAssign = cleanCmd.substringBefore("|id=").removePrefix("CMD|ASSIGNMENT|")
                    val deadlinePart = rawAssign.substringBefore("|", "")
                    val descPart = rawAssign.substringAfter("|", "").trim()
                    hwSubmitted = false
                    activeHwTitle = "Bài tập / Đề bài từ Giáo viên"
                    activeHwDeadline = if (deadlinePart.equals("none", ignoreCase = true)) "" else deadlinePart
                    if (descPart.isNotBlank()) {
                        activeHwDesc = descPart
                    }
                    openWorkSubTabFromTeacherCommand("HOMEWORK")
                    if (descPart.isNotBlank()) {
                        Toast.makeText(this, "📋 Bài tập mới: $descPart", Toast.LENGTH_LONG).show()
                    }
                }
                cleanCmd.startsWith("CMD|COLLECT_WORK") -> {
                    val rawCollect = cleanCmd.substringBefore("|id=").removePrefix("CMD|COLLECT_WORK").removePrefix("|").trim()
                    if (rawCollect.isNotBlank() && activeHwDesc.isBlank()) {
                        activeHwTitle = "Yêu cầu nộp bài làm"
                        activeHwDesc = rawCollect
                    }
                    openWorkSubTabFromTeacherCommand("HOMEWORK")
                    AlertDialog.Builder(this)
                        .setTitle("🔔 Giáo viên yêu cầu nộp bài")
                        .setMessage(rawCollect.ifBlank { "Giáo viên đang thu bài làm. Bạn muốn chọn tệp bài làm để nộp ngay không?" })
                        .setPositiveButton("📎 Chọn tệp nộp ngay") { _, _ ->
                            launchStudentFilePicker()
                        }
                        .setNegativeButton("Viết bài trực tiếp", null)
                        .show()
                }
                cleanCmd.startsWith("CMD|FILE_SUBMIT_ACK|") -> {
                    val savedAs = cleanCmd.substringBefore("|id=").removePrefix("CMD|FILE_SUBMIT_ACK|").substringBefore("|")
                    hwSubmitted = true
                    Toast.makeText(this, "✅ Máy Giáo viên xác nhận đã lưu bài: $savedAs", Toast.LENGTH_SHORT).show()
                }
                cleanCmd.startsWith("CMD|FILE_OFFER|") || cleanCmd.startsWith("CMD|FILE_SHARE|") -> {
                    // Format chuẩn mới: CMD|FILE_OFFER|<fileId>|<fileName>|<fileSize>|<autoOpen:0|1>|<webPort>|<token>|<downloadUrl>
                    val rawOffer = cleanCmd.substringBefore("|id=").substringAfter("|").substringAfter("|")
                    val pParts = rawOffer.split("|")
                    if (pParts.size >= 6) {
                        val fileId = pParts[0].trim()
                        val fileName = pParts[1].trim().ifBlank { "TaiLieu_LopHoc" }
                        val autoOpen = pParts[3].trim() == "1" || pParts[3].trim().equals("true", ignoreCase = true)
                        val parsedPort = pParts[4].trim().toIntOrNull() ?: currentTeacherWebPort
                        val parsedToken = pParts[5].trim()
                        if (parsedPort > 0) currentTeacherWebPort = parsedPort
                        if (parsedToken.isNotBlank()) currentBroadcastToken = parsedToken

                        downloadOfferedFileFromTeacher(
                            fileId = fileId,
                            rawFileName = fileName,
                            autoOpen = autoOpen,
                            webPort = currentTeacherWebPort,
                            token = currentBroadcastToken
                        )
                    } else {
                        // Tương thích ngược gói CMD|FILE_SHARE|<fileName>|<url>
                        val fileName = pParts.getOrNull(0)?.ifBlank { "TaiLieu_LopHoc" } ?: "TaiLieu_LopHoc"
                        val encName = java.net.URLEncoder.encode(fileName, "UTF-8")
                        val encTok = java.net.URLEncoder.encode(currentBroadcastToken, "UTF-8")
                        downloadOfferedFileFromTeacher(
                            fileId = "",
                            rawFileName = fileName,
                            autoOpen = true,
                            webPort = currentTeacherWebPort,
                            token = currentBroadcastToken,
                            explicitEndpointPath = "/api/file_broadcast?name=$encName&token=$encTok"
                        )
                    }
                }
                cleanCmd.startsWith("CMD|FILE_BROADCAST|") -> {
                    // Tương thích ngược CMD|FILE_BROADCAST|<path>|<webPort>|<token>
                    val rawBcast = cleanCmd.substringBefore("|id=").removePrefix("CMD|FILE_BROADCAST|")
                    val bParts = rawBcast.split("|")
                    val rawPath = bParts.getOrNull(0) ?: "TaiLieu"
                    val parsedPort = bParts.getOrNull(1)?.trim()?.toIntOrNull() ?: currentTeacherWebPort
                    val parsedToken = bParts.getOrNull(2)?.trim() ?: currentBroadcastToken
                    if (parsedPort > 0) currentTeacherWebPort = parsedPort
                    if (parsedToken.isNotBlank()) currentBroadcastToken = parsedToken
                    val fileName = rawPath.substringAfterLast('/').substringAfterLast('\\').ifBlank { "TaiLieu_LopHoc" }
                    val encName = java.net.URLEncoder.encode(fileName, "UTF-8")
                    val encTok = java.net.URLEncoder.encode(currentBroadcastToken, "UTF-8")
                    downloadOfferedFileFromTeacher(
                        fileId = "",
                        rawFileName = fileName,
                        autoOpen = true,
                        webPort = currentTeacherWebPort,
                        token = currentBroadcastToken,
                        explicitEndpointPath = "/api/file_broadcast?name=$encName&token=$encTok"
                    )
                }
                cleanCmd.startsWith("KICK") -> {
                    disconnectFromTeacher("Bạn đã được Giáo viên cho phép rời lớp")
                }
                cleanCmd.contains("HEARTBEAT") -> {
                    heartbeatWatchdog.feedHeartbeat()
                }
            }
        }
    }

    @Volatile
    private var isStudentDemoPresenting = false
    private var highFreqScreenRelayJob: Job? = null

    private fun startHighFrequencyScreenRelay(intervalMs: Long = 100L) {
        highFreqScreenRelayJob?.cancel()
        highFreqScreenRelayJob = lifecycleScope.launch(Dispatchers.IO) {
            while (isActive && tcpSocket?.isConnected == true) {
                sendScreenshotToTeacher()
                delay(intervalMs)
            }
        }
    }

    private fun stopHighFrequencyScreenRelay() {
        isStudentDemoPresenting = false
        highFreqScreenRelayJob?.cancel()
        highFreqScreenRelayJob = null
    }

    private fun launchRequestedAndroidApp(appTarget: String) {
        val norm = appTarget.trim().lowercase(Locale.ROOT)
        try {
            val intent: android.content.Intent? = when {
                norm.contains("calc") -> {
                    packageManager.getLaunchIntentForPackage("com.google.android.calculator")
                        ?: packageManager.getLaunchIntentForPackage("com.sec.android.app.popupcalculator")
                        ?: android.content.Intent.makeMainSelectorActivity(
                            android.content.Intent.ACTION_MAIN,
                            android.content.Intent.CATEGORY_APP_CALCULATOR
                        )
                }
                norm.contains("chrome") || norm.contains("browser") || norm.contains("msedge") -> {
                    packageManager.getLaunchIntentForPackage("com.android.chrome")
                        ?: android.content.Intent(android.content.Intent.ACTION_VIEW, android.net.Uri.parse("https://www.google.com"))
                }
                norm.contains("youtube") -> {
                    packageManager.getLaunchIntentForPackage("com.google.android.youtube")
                        ?: android.content.Intent(android.content.Intent.ACTION_VIEW, android.net.Uri.parse("https://www.youtube.com"))
                }
                norm.contains("setting") -> {
                    android.content.Intent(android.provider.Settings.ACTION_SETTINGS)
                }
                norm.contains("file") || norm.contains("explorer") -> {
                    packageManager.getLaunchIntentForPackage("com.google.android.documentsui")
                        ?: android.content.Intent(android.content.Intent.ACTION_GET_CONTENT).apply { type = "*/*" }
                }
                norm.contains("word") || norm.contains("notepad") -> {
                    packageManager.getLaunchIntentForPackage("com.microsoft.office.word")
                        ?: packageManager.getLaunchIntentForPackage("com.google.android.apps.docs.editors.docs")
                }
                else -> {
                    packageManager.getLaunchIntentForPackage(appTarget.trim())
                }
            }

            if (intent != null) {
                intent.addFlags(android.content.Intent.FLAG_ACTIVITY_NEW_TASK)
                startActivity(intent)
                Toast.makeText(this, "🚀 Giáo viên đã mở ứng dụng: $appTarget", Toast.LENGTH_SHORT).show()
            } else {
                Toast.makeText(this, "Thiết bị chưa cài đặt ứng dụng: $appTarget", Toast.LENGTH_SHORT).show()
            }
        } catch (e: Exception) {
            Log.w(TAG, "launchRequestedAndroidApp failed for $appTarget: ${e.message}")
            Toast.makeText(this, "Không thể mở ứng dụng: $appTarget", Toast.LENGTH_SHORT).show()
        }
    }

    private fun sendAck(cmdId: String, status: String) {
        if (cmdId.isBlank() || cmdId == "unknown") return
        val studentCode = currentStudentCode.ifBlank { "HS001" }
        lifecycleScope.launch(Dispatchers.IO) {
            try {
                val os = tcpOutputStream ?: return@launch
                val ackMsg = "ACK|$cmdId|$studentCode|$status\n"
                synchronized(os) {
                    os.write(ackMsg.toByteArray(Charsets.UTF_8))
                    os.flush()
                }
                Log.d(TAG, "Sent ACK to teacher: $ackMsg")
            } catch (e: Exception) {
                Log.w(TAG, "Failed to send ACK: ${e.message}")
            }
        }
    }

    private fun startScreenBroadcast(token: String, webPort: Int, isForce: Boolean, title: String) {
        val alreadyStreamingSameSession =
            isScreenBroadcastActive &&
            broadcastPollJob?.isActive == true &&
            token.isNotEmpty() &&
            token == currentBroadcastToken &&
            webPort == currentTeacherWebPort

        isScreenBroadcastActive = true
        isForceBroadcastMode = isForce
        runOnUiThread {
            if (title.isNotBlank() && activeLessonTitle.isBlank()) {
                activeLessonTitle = title
            }
            refreshHome()
        }
        currentBroadcastToken = token
        currentTeacherWebPort = webPort

        val displayTitle = if (title.isNotBlank()) title else "Màn hình Giáo viên"
        txtBroadcastStatus.text = "● TRỰC TIẾP  •  $displayTitle"
        txtEasyTouchPodTitle.text = "🟢 $displayTitle"
        txtInlineBroadcastTitle.text = displayTitle

        layoutNoBroadcast.visibility = View.GONE
        layoutLiveBroadcast.visibility = View.VISIBLE

        // Khi Giáo viên bấm Share màn hình, Học sinh lập tức vào thẳng Fullscreen 100%:
        // - Mặc định khóa cứng (isForce = true): Học sinh chỉ xem Fullscreen, không có nút thu nhỏ / bấm ra ngoài.
        // - Chỉ khi Giáo viên bật "Cho phép ra ngoài" (isForce = false): Nút "↙ Ra ngoài / Thu nhỏ" mới xuất hiện trong menu tròn EasyTouch.
        topBroadcastBadgeBar.visibility = View.GONE
        findViewById<View?>(R.id.btnTopMinimizeBroadcast)?.visibility = View.GONE
        btnCloseBroadcast.visibility = if (!isForce) View.VISIBLE else View.GONE
        broadcastOverlay.visibility = View.VISIBLE
        setImmersiveFullscreen(true)
        collapseStudentEasyTouch(immediate = true)

        if (alreadyStreamingSameSession) {
            return
        }
        progressBroadcastLoading.visibility = View.VISIBLE

        broadcastPollJob?.cancel()
        val teacherIp = currentTeacherIp ?: "10.0.2.2"
        syncBroadcastAudioMuteUi()
        startBroadcastAudioStream(teacherIp, currentTeacherWebPort, currentBroadcastToken)

        broadcastPollJob = lifecycleScope.launch(Dispatchers.IO) {
            Log.i(TAG, "Starting smooth & crisp ARGB_8888 broadcast stream from $teacherIp:$currentTeacherWebPort with token: $currentBroadcastToken")
            val tokenParamOnce = if (currentBroadcastToken.isNotEmpty()) "&token=$currentBroadcastToken" else ""
            val playoutQueue = java.util.concurrent.LinkedBlockingQueue<android.graphics.Bitmap>(64)
            // Vòng xoay tái sử dụng bộ nhớ Bitmap (inBitmap Pool) chuẩn Google Meet: loại bỏ 95% rác bộ nhớ GC tránh khựng hình trên Tablet
            val freeBitmapPool = java.util.concurrent.LinkedBlockingQueue<android.graphics.Bitmap>(8)

            fun recycleToPool(bmp: android.graphics.Bitmap?) {
                if (bmp != null && bmp.isMutable && !bmp.isRecycled) {
                    freeBitmapPool.offer(bmp)
                }
            }

            fun enqueueFrameSmooth(bmp: android.graphics.Bitmap) {
                while (playoutQueue.size >= 60) {
                    val dropped = playoutQueue.poll()
                    recycleToPool(dropped)
                }
                playoutQueue.offer(bmp)
            }

            fun decodeFrameCrispAndSmooth(data: ByteArray, len: Int): android.graphics.Bitmap? {
                val candidate = freeBitmapPool.poll()
                if (candidate != null) {
                    try {
                        val reuseOpts = BitmapFactory.Options().apply {
                            inPreferredConfig = android.graphics.Bitmap.Config.ARGB_8888
                            inMutable = true
                            inBitmap = candidate
                            inScaled = false
                        }
                        val reused = BitmapFactory.decodeByteArray(data, 0, len, reuseOpts)
                        if (reused != null) return reused
                    } catch (_: IllegalArgumentException) {
                        // Nguồn chiếu đổi kích thước (VD: 1080p <-> Vùng chọn) -> cấp phát lại buffer khớp kích thước mới
                    }
                }
                val freshOpts = BitmapFactory.Options().apply {
                    inPreferredConfig = android.graphics.Bitmap.Config.ARGB_8888
                    inMutable = true
                    inScaled = false
                }
                return BitmapFactory.decodeByteArray(data, 0, len, freshOpts)
            }

            // Luồng Playout 30-60 FPS trên UI: phát tuần tự TỪNG KHUNG HÌNH và trả lại Bitmap cũ về Pool sau khi vẽ xong
            val playoutJob = launch(Dispatchers.Main) {
                var isBuffering = true
                var displayedBitmap: android.graphics.Bitmap? = null
                while (isActive && isScreenBroadcastActive) {
                    val qSize = playoutQueue.size
                    if (isBuffering && displayedBitmap != null) {
                        if (qSize < 2) {
                            delay(10)
                            if (playoutQueue.isEmpty()) continue
                        }
                        isBuffering = false
                    } else if (qSize > 0) {
                        isBuffering = false
                    }

                    val frame = playoutQueue.poll()
                    if (frame != null) {
                        if (broadcastOverlay.visibility == View.VISIBLE) {
                            imgBroadcastScreen.setImageBitmap(frame)
                            (imgBroadcastScreen.drawable as? android.graphics.drawable.BitmapDrawable)?.isFilterBitmap = true
                        } else {
                            imgInlineBroadcast.setImageBitmap(frame)
                            (imgInlineBroadcast.drawable as? android.graphics.drawable.BitmapDrawable)?.isFilterBitmap = true
                        }
                        progressBroadcastLoading.visibility = View.GONE

                        val prev = displayedBitmap
                        if (prev != null && prev !== frame) {
                            recycleToPool(prev)
                        }
                        displayedBitmap = frame

                        // Giữ nhịp 60 FPS (15ms) - 120 FPS (8ms) mà KHÔNG bỏ sót bất kỳ nét vẽ nào
                        val nextDelay = when {
                            qSize > 14 -> 8L
                            qSize > 6 -> 11L
                            else -> 15L
                        }
                        delay(nextDelay)
                    } else {
                        isBuffering = true
                        delay(8)
                    }
                }
            }

            // Ưu tiên luồng đẩy khung liên tục. Lỗi/không hỗ trợ -> quay lại polling.
            var streamFailures = 0
            val fallbackDecodeOpts = BitmapFactory.Options().apply {
                inPreferredConfig = android.graphics.Bitmap.Config.ARGB_8888
                inScaled = false
            }
            try {
                while (isActive && isScreenBroadcastActive) {
                    while (isActive && isScreenBroadcastActive && streamFailures < 3) {
                        var streamConn: HttpURLConnection? = null
                        try {
                            val activeTokenParam = if (currentBroadcastToken.isNotEmpty()) "&token=$currentBroadcastToken" else tokenParamOnce
                            val sUrl = URL("http://$teacherIp:$currentTeacherWebPort/api/broadcast_stream?t=${System.currentTimeMillis()}$activeTokenParam")
                            streamConn = (sUrl.openConnection() as HttpURLConnection).apply {
                                connectTimeout = 2500
                                readTimeout = 15000
                                requestMethod = "GET"
                                useCaches = false
                                setRequestProperty("User-Agent", "QASmartClass-Android/1.0")
                            }
                            if (streamConn.responseCode != 200) {
                                Log.w(TAG, "Broadcast stream HTTP ${streamConn.responseCode} from $teacherIp:$currentTeacherWebPort")
                                streamFailures++
                                streamConn.disconnect()
                                delay(300)
                                continue
                            }
                            val din = java.io.DataInputStream(java.io.BufferedInputStream(streamConn.inputStream, 262144))
                            var gotFrame = false
                            while (isActive && isScreenBroadcastActive) {
                                val len = din.readInt()
                                if (len <= 0 || len > 20_000_000) throw java.io.IOException("Invalid frame length $len")
                                val data = ByteArray(len)
                                din.readFully(data)
                                if (playoutQueue.size >= 32 && din.available() >= 4) continue
                                val bitmap = decodeFrameCrispAndSmooth(data, len)
                                if (bitmap != null) {
                                    gotFrame = true
                                    streamFailures = 0
                                    enqueueFrameSmooth(bitmap)
                                }
                            }
                            if (!gotFrame) streamFailures++
                        } catch (e: Exception) {
                            if (isActive && isScreenBroadcastActive) {
                                Log.w(TAG, "Broadcast stream error ($teacherIp:$currentTeacherWebPort): ${e.message}")
                                streamFailures++
                                delay(250)
                            }
                        } finally {
                            try { streamConn?.disconnect() } catch (_: Exception) {}
                        }
                    }

                    // Dự phòng ngắn hạn (15 chu kỳ) bằng HTTP Pull đơn ảnh rồi tự động nâng cấp lại lên luồng stream tốc độ cao
                    var pullCycles = 0
                    while (isActive && isScreenBroadcastActive && pullCycles < 15) {
                        pullCycles++
                        try {
                            val tokenParam = if (currentBroadcastToken.isNotEmpty()) "&token=$currentBroadcastToken" else ""
                            val urlString = "http://$teacherIp:$currentTeacherWebPort/api/broadcast_image?t=${System.currentTimeMillis()}$tokenParam"
                            val url = URL(urlString)
                            val conn = (url.openConnection() as HttpURLConnection).apply {
                                connectTimeout = 2500
                                readTimeout = 2500
                                requestMethod = "GET"
                                useCaches = false
                                setRequestProperty("User-Agent", "QASmartClass-Android/1.0")
                            }

                            val code = conn.responseCode
                            if (code == 200) {
                                conn.inputStream.use { input ->
                                    val bitmap = BitmapFactory.decodeStream(input, null, fallbackDecodeOpts)
                                    if (bitmap != null) {
                                        enqueueFrameSmooth(bitmap)
                                    }
                                }
                            } else {
                                Log.w(TAG, "Broadcast pull HTTP $code from $teacherIp:$currentTeacherWebPort")
                            }
                            conn.disconnect()
                        } catch (e: Exception) {
                            Log.w(TAG, "Broadcast pull error ($teacherIp:$currentTeacherWebPort): ${e.message}")
                        }
                        delay(60)
                    }
                    streamFailures = 0
                }
            } finally {
                playoutJob.cancel()
                freeBitmapPool.clear()
            }
        }
    }

    private fun updateScreenBroadcast(token: String, webPort: Int, title: String, isForce: Boolean = false) {
        if (!isScreenBroadcastActive) {
            // Tự phục hồi nếu thiết bị Android vào lớp muộn hoặc lỡ gói SCREEN_BROADCAST_START đầu tiên
            startScreenBroadcast(
                token.ifBlank { currentBroadcastToken },
                if (webPort > 0) webPort else currentTeacherWebPort,
                isForce,
                title
            )
            return
        }
        if (token.isNotEmpty()) currentBroadcastToken = token
        if (webPort > 0) currentTeacherWebPort = webPort
        if (isForce != isForceBroadcastMode) {
            applyBroadcastLockState(isForce, notifyToast = false)
        }
        if (title.isNotEmpty()) {
            txtBroadcastStatus.text = "● TRỰC TIẾP  •  $title"
            txtEasyTouchPodTitle.text = "🟢 $title"
            txtInlineBroadcastTitle.text = title
            if (activeLessonTitle.isBlank()) activeLessonTitle = title
            runOnUiThread { refreshHome() }
        }
    }

    private fun toggleBroadcastAudioMute() {
        isBroadcastAudioMuted = !isBroadcastAudioMuted
        syncBroadcastAudioMuteUi()
    }

    private fun syncBroadcastAudioMuteUi() {
        runOnUiThread {
            if (isBroadcastAudioMuted) {
                btnInlineBroadcastAudio.text = "🔇 Tắt tiếng"
                btnInlineBroadcastAudio.setTextColor(android.graphics.Color.parseColor("#DC2626"))
                btnFullscreenMuteAudio.text = "🔇 Đã tắt loa"
                btnFullscreenMuteAudio.setTextColor(android.graphics.Color.parseColor("#FCA5A5"))
            } else {
                btnInlineBroadcastAudio.text = "🔊 Tiếng"
                btnInlineBroadcastAudio.setTextColor(android.graphics.Color.parseColor("#1A73E8"))
                btnFullscreenMuteAudio.text = "🔊 Bật tiếng"
                btnFullscreenMuteAudio.setTextColor(android.graphics.Color.parseColor("#8AB4F8"))
            }
        }
    }

    private fun startBroadcastAudioStream(teacherIp: String, webPort: Int, token: String) {
        broadcastAudioJob?.cancel()
        broadcastAudioJob = lifecycleScope.launch(Dispatchers.IO) {
            val sampleRate = 24000
            val minBuf = android.media.AudioTrack.getMinBufferSize(
                sampleRate,
                android.media.AudioFormat.CHANNEL_OUT_MONO,
                android.media.AudioFormat.ENCODING_PCM_16BIT
            )
            if (minBuf <= 0) {
                Log.w(TAG, "AudioTrack invalid minBufferSize=$minBuf")
                return@launch
            }

            var audioTrack: android.media.AudioTrack? = null
            try {
                audioTrack = android.media.AudioTrack.Builder()
                    .setAudioAttributes(
                        android.media.AudioAttributes.Builder()
                            .setUsage(android.media.AudioAttributes.USAGE_MEDIA)
                            .setContentType(android.media.AudioAttributes.CONTENT_TYPE_SPEECH)
                            .build()
                    )
                    .setAudioFormat(
                        android.media.AudioFormat.Builder()
                            .setEncoding(android.media.AudioFormat.ENCODING_PCM_16BIT)
                            .setSampleRate(sampleRate)
                            .setChannelMask(android.media.AudioFormat.CHANNEL_OUT_MONO)
                            .build()
                    )
                    .setBufferSizeInBytes((minBuf * 2).coerceAtLeast(9600))
                    .setTransferMode(android.media.AudioTrack.MODE_STREAM)
                    .build()
                audioTrack.play()
            } catch (e: Exception) {
                Log.w(TAG, "Cannot initialize AudioTrack for broadcast audio: ${e.message}")
                return@launch
            }

            val pcmBuf = ByteArray(8192)
            val tokenQuery = if (token.isNotEmpty()) "?token=${java.net.URLEncoder.encode(token, "UTF-8")}" else ""

            try {
                while (isActive && isScreenBroadcastActive) {
                    var conn: java.net.HttpURLConnection? = null
                    try {
                        val url = java.net.URL("http://$teacherIp:$webPort/api/audio_stream$tokenQuery")
                        conn = (url.openConnection() as java.net.HttpURLConnection).apply {
                            connectTimeout = 2500
                            readTimeout = 8000
                            useCaches = false
                            setRequestProperty("Connection", "close")
                        }
                        if (conn.responseCode == 200) {
                            val din = java.io.DataInputStream(java.io.BufferedInputStream(conn.inputStream, 8192))
                            while (isActive && isScreenBroadcastActive) {
                                val len = din.readUnsignedShort()
                                if (len <= 0 || len > pcmBuf.size) break
                                din.readFully(pcmBuf, 0, len)
                                if (!isBroadcastAudioMuted) {
                                    audioTrack.write(pcmBuf, 0, len)
                                }
                            }
                        }
                    } catch (_: java.io.EOFException) {
                    } catch (e: Exception) {
                        Log.d(TAG, "Broadcast audio stream reconnect: ${e.message}")
                    } finally {
                        try { conn?.disconnect() } catch (_: Exception) {}
                    }

                    if (isActive && isScreenBroadcastActive) {
                        delay(500)
                    }
                }
            } finally {
                try {
                    audioTrack?.pause()
                    audioTrack?.flush()
                    audioTrack?.stop()
                } catch (_: Exception) {}
                try {
                    audioTrack?.release()
                } catch (_: Exception) {}
            }
        }
    }

    private fun stopScreenBroadcast() {
        isScreenBroadcastActive = false
        isForceBroadcastMode = false
        broadcastUiHideJob?.cancel()
        broadcastUiHideJob = null
        broadcastAudioJob?.cancel()
        broadcastAudioJob = null
        broadcastPollJob?.cancel()
        broadcastPollJob = null

        runOnUiThread {
            collapseStudentEasyTouch(immediate = true)
            setImmersiveFullscreen(false)
            broadcastOverlay.visibility = View.GONE
            layoutLiveBroadcast.visibility = View.GONE
            layoutNoBroadcast.visibility = View.VISIBLE
            imgInlineBroadcast.setImageDrawable(null)
            imgBroadcastScreen.setImageDrawable(null)
            progressBroadcastLoading.visibility = View.GONE
            refreshHome()
        }
    }

    /** Xin quyền chia sẻ màn hình (hộp thoại hệ thống) để giáo viên xem HS đang làm gì. */
    private fun requestScreenCapture() {
        if (com.qasmartclass.student.services.ScreenCaptureService.instance != null) return
        try {
            val mpm = getSystemService(Context.MEDIA_PROJECTION_SERVICE) as android.media.projection.MediaProjectionManager
            captureLauncher.launch(mpm.createScreenCaptureIntent())
        } catch (e: Exception) {
            Log.w(TAG, "Cannot request screen capture: ${e.message}")
        }
    }

    /** Chụp khung hình cửa sổ ứng dụng hiện tại làm phương án dự phòng tức thì khi chưa cấp quyền MediaProjection. */
    private suspend fun captureAppWindowFallbackJpeg(): ByteArray? = withContext(Dispatchers.Main) {
        try {
            val rootView = window?.decorView?.rootView ?: return@withContext null
            val w = rootView.width
            val h = rootView.height
            if (w <= 0 || h <= 0) return@withContext null
            val targetW = 960.coerceAtMost(w)
            val scale = targetW.toFloat() / w.toFloat()
            val targetH = (h * scale).toInt().coerceAtLeast(1)
            val bmp = android.graphics.Bitmap.createBitmap(targetW, targetH, android.graphics.Bitmap.Config.RGB_565)
            val canvas = android.graphics.Canvas(bmp)
            canvas.scale(scale, scale)
            rootView.draw(canvas)
            withContext(Dispatchers.IO) {
                val bos = java.io.ByteArrayOutputStream()
                bmp.compress(android.graphics.Bitmap.CompressFormat.JPEG, 65, bos)
                bmp.recycle()
                bos.toByteArray()
            }
        } catch (e: Exception) {
            null
        }
    }

    /** Chụp khung hình màn hình mới nhất và gửi cho giáo viên (SCREENSHOT|code|base64). */
    private fun sendScreenshotToTeacher() {
        lifecycleScope.launch(Dispatchers.IO) {
            var jpeg = com.qasmartclass.student.services.ScreenCaptureService.captureJpeg()
            if (jpeg == null) {
                withContext(Dispatchers.Main) {
                    if (!hasPromptedCaptureThisSession) {
                        if (focusReporter.appInForeground) {
                            hasPromptedCaptureThisSession = true
                            needCapturePrompt = false
                            requestScreenCapture()
                        } else {
                            needCapturePrompt = true
                        }
                    }
                }
                jpeg = captureAppWindowFallbackJpeg()
            }
            if (jpeg == null) return@launch
            val os = tcpOutputStream ?: return@launch
            try {
                val b64 = android.util.Base64.encodeToString(jpeg, android.util.Base64.NO_WRAP)
                val line = "SCREENSHOT|$currentStudentCode|$b64\n".toByteArray(Charsets.UTF_8)
                synchronized(os) {
                    os.write(line)
                    os.flush()
                }
            } catch (e: Exception) {
                Log.w(TAG, "Send screenshot failed: ${e.message}")
            }
        }
    }

    override fun onStart() {
        super.onStart()
        focusReporter.appInForeground = true
        if (tcpOutputStream != null) {
            focusReporter.reportNow()
        }
        if (needCapturePrompt && !hasPromptedCaptureThisSession && tcpOutputStream != null) {
            hasPromptedCaptureThisSession = true
            needCapturePrompt = false
            requestScreenCapture()
        }
    }

    override fun onStop() {
        super.onStop()
        // Học sinh thoát sang ứng dụng khác / về màn hình chính → báo ngầm ngay cho giáo viên
        focusReporter.appInForeground = false
        if (tcpOutputStream != null) {
            focusReporter.reportNow()
        }
    }

    private fun disconnectFromTeacher(reason: String) {
        // Vô hiệu hóa thế hệ kết nối hiện tại trước khi đóng socket để readTcpStream không bật lại attemptSilentLanReconnect
        connectionGeneration.incrementAndGet()
        isConnectingInProgress = false
        hasPromptedCaptureThisSession = false
        discoveryJob?.cancel()
        discoveryJob = null
        discoveryService.stopListening()
        reconnectJob?.cancel()
        reconnectJob = null
        connectionJob?.cancel()
        connectionJob = null

        isLessonActive = false
        activeLessonTitle = ""
        activeLessonSubject = ""
        activeLessonSummary = ""
        activeLessonStage = 0
        hasTeacherNotice = false
        unreadTeacherChatCount = 0
        // Xóa sạch khảo sát tạm trên RAM khi rời lớp / mất kết nối
        activeSurveyId = ""
        activeSurveyTitle = ""
        activeSurveyType = ""
        activeSurveyQuestion = ""
        activeSurveyOptions.clear()
        selectedSurveyOptionIdx = -1
        selectedSurveyStar = 0
        surveySubmitted = false
        stopScreenBroadcast()
        heartbeatJob?.cancel()
        heartbeatJob = null
        focusReporter.stopReporting()
        try { stopService(android.content.Intent(this, com.qasmartclass.student.services.ScreenCaptureService::class.java)) } catch (_: Exception) {}
        heartbeatWatchdog.releaseLock()
        currentSessionKey = ""
        currentSessionSalt = ""
        currentClassCode = ""

        val staleSocket = tcpSocket
        tcpSocket = null
        tcpOutputStream = null
        try {
            staleSocket?.close()
        } catch (e: Exception) {
            // Ignore
        }

        runOnUiThread {
            if (::lanReconnectBanner.isInitialized) lanReconnectBanner.visibility = View.GONE
            lockOverlay.visibility = View.GONE
            if (::badgeRailChat.isInitialized) badgeRailChat.visibility = View.GONE
            if (::badgeRailWork.isInitialized) badgeRailWork.visibility = View.GONE
            connectedPanel.visibility = View.GONE
            discoveryPanel.visibility = View.VISIBLE
            btnConnect.isEnabled = true
            progressBar.visibility = View.GONE
            txtStatus.text = reason
            if (!isFinishing && !isDestroyed) {
                Toast.makeText(this, reason, Toast.LENGTH_SHORT).show()
                // Tự động làm mới thông tin lớp học trên mạng LAN sau khi rời lớp
                btnRefresh.postDelayed({
                    if (!isFinishing && !isDestroyed && discoveryPanel.visibility == View.VISIBLE && !isAlreadyConnectedOrConnecting()) {
                        startAutoDiscovery()
                    }
                }, 600)
            }
        }
    }

    /**
     * Chuyển đổi mọi mã lỗi kỹ thuật / ngoại lệ mạng sang thông báo Tiếng Việt tự nhiên,
     * dễ hiểu cho học sinh và giáo viên (tuyệt đối không hiển thị mã lỗi hệ thống hay chuỗi mã hóa).
     */
    private fun formatFriendlyErrorMessage(error: Throwable?): String {
        val raw = error?.message?.trim().orEmpty()
        return formatFriendlyErrorText(raw, error)
    }

    private fun formatFriendlyErrorText(rawMessage: String, error: Throwable? = null): String {
        val msg = rawMessage.trim()
        val lower = msg.lowercase(Locale.US)

        return when {
            msg.contains("không tồn tại trong danh sách lớp", ignoreCase = true) ->
                "Mã học sinh này chưa có trong danh sách lớp. Em hãy bấm 'Đổi tài khoản / Học sinh khác' để chọn đúng Mã học sinh của mình nhé!"

            msg.contains("Mật khẩu không chính xác", ignoreCase = true) || lower.contains("wrong password") ->
                "Mật khẩu lớp học chưa đúng. Em hãy kiểm tra lại mật khẩu (mặc định là 123456) nhé!"

            msg.contains("chưa nhập mật khẩu", ignoreCase = true) ->
                "Vui lòng nhập mật khẩu lớp học để kết nối."

            error is java.net.ConnectException || lower.contains("econnrefused") || lower.contains("connection refused") ->
                "Chưa kết nối được với máy Giáo viên. Có thể Thầy/Cô chưa mở lớp học hoặc phần mềm trên máy Giáo viên đang tắt."

            error is java.net.SocketTimeoutException || lower.contains("etimedout") || lower.contains("timed out") || lower.contains("after ") ->
                "Không tìm thấy phản hồi từ máy Giáo viên. Em hãy kiểm tra máy tính bảng đã kết nối đúng mạng Wi-Fi phòng học chưa nhé!"

            error is java.net.UnknownHostException || error is java.net.NoRouteToHostException ||
                lower.contains("enetunreach") || lower.contains("ehostunreach") ||
                lower.contains("no route to host") || lower.contains("network is unreachable") ->
                "Thiết bị chưa kết nối mạng Wi-Fi phòng học hoặc địa chỉ máy Giáo viên chưa đúng."

            lower.contains("econnreset") || lower.contains("broken pipe") || lower.contains("connection reset") ||
                lower.contains("regionlength") || lower.contains("eof") ->
                "Kết nối mạng với máy Giáo viên bị gián đoạn. Em hãy bấm 'Vào lớp học ngay' để kết nối lại nhé!"

            lower.contains("enc_cmd") || lower.contains("auth_failed") || msg.contains("|") || msg.contains("==") ->
                "Không thể đăng nhập vào lớp học. Em hãy kiểm tra lại Mã học sinh và Mật khẩu rồi thử lại nhé!"

            msg.isNotBlank() && !lower.contains("exception") && !lower.contains("failed to connect") && !lower.contains("socket") ->
                msg

            else ->
                "Chưa thể kết nối vào lớp học lúc này. Em hãy kiểm tra lại Wi-Fi phòng học hoặc nhờ Giáo viên hỗ trợ nhé!"
        }
    }

    private fun showError(message: String) {
        val friendly = formatFriendlyErrorText(message)
        runOnUiThread {
            txtError.text = friendly
            txtError.visibility = View.VISIBLE
            txtStatus.text = friendly
            progressBar.visibility = View.GONE
            btnConnect.isEnabled = true
            Toast.makeText(this, friendly, Toast.LENGTH_LONG).show()
        }
    }

    override fun onDestroy() {
        try {
            activePreviewDialog?.dismiss()
            activePreviewDialog = null
        } catch (_: Exception) {}
        super.onDestroy()
        disconnectFromTeacher("Ứng dụng đóng")
        discoveryService.stopListening()
    }
}
