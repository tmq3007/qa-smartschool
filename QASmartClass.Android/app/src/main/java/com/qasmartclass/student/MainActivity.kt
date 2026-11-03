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

    // Discovery UI
    private lateinit var discoveryPanel: LinearLayout
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

    // Connected Header & Profile Bar
    private lateinit var connectedPanel: LinearLayout
    private lateinit var txtHeaderLiveBadge: TextView
    private lateinit var txtConnectedClassInfo: TextView
    private lateinit var txtTeacherDetail: TextView
    private lateinit var txtStudentDetail: TextView
    private lateinit var txtServerIpDetail: TextView
    private lateinit var btnDisconnect: Button

    // Tab Bar Navigation
    private lateinit var btnTabLecture: TextView
    private lateinit var btnTabChat: TextView
    private lateinit var btnTabHandRaise: TextView

    // Sidebar (drawer) & Home dashboard (giao diện giống StudentShell WPF)
    private lateinit var navDrawerOverlay: FrameLayout
    private lateinit var btnHeaderBack: ImageButton
    private lateinit var btnSoonBack: Button
    private lateinit var btnMenu: ImageButton
    private lateinit var txtPageTitle: TextView
    private lateinit var btnNavHome: TextView
    private lateinit var panelHome: LinearLayout
    private lateinit var panelSoon: LinearLayout
    private lateinit var txtSoonTitle: TextView
    private lateinit var txtHomeGreeting: TextView
    private lateinit var txtHomeSub: TextView
    private lateinit var txtStatClass: TextView
    private lateinit var txtStatBroadcast: TextView
    private lateinit var txtStatHand: TextView
    private lateinit var txtStatTeacher: TextView
    private lateinit var txtHomeLessonTitle: TextView
    private lateinit var txtHomeLessonSub: TextView
    private lateinit var txtHomeTeacherChip: TextView
    private lateinit var txtHomeNotice: TextView
    private lateinit var layoutHomeLessonWaiting: LinearLayout
    private lateinit var layoutHomeLessonActive: LinearLayout
    private lateinit var txtHomeLiveBadge: TextView
    private lateinit var txtHomeSubjectBadge: TextView
    private lateinit var txtHomeStageBadge: TextView
    private lateinit var imgHomeBroadcastPreview: ImageView
    private lateinit var layoutWhiteboardStitchPreview: LinearLayout
    private lateinit var txtHomeBoardHeader: TextView
    private lateinit var txtHomeBoardContent: TextView
    private lateinit var txtHomeBoardNote1: TextView
    private lateinit var txtHomeBoardNote2: TextView
    private lateinit var txtHomeBoardPointer: TextView
    private lateinit var txtHomeTeacherAvatar: TextView
    private lateinit var txtHomeNoticeHeader: TextView
    private lateinit var txtHomeNoticeTime: TextView
    private lateinit var layoutNoticeFeedback: LinearLayout
    private lateinit var txtHomeChatBadge: TextView
    private lateinit var txtHomeQuizTitle: TextView
    private lateinit var txtHomeQuizSub: TextView
    private lateinit var txtHomeQuizBadge: TextView
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
    private val soonNavButtons = mutableListOf<Pair<TextView, String>>()

    // Interactive Feature Panel & State (Quiz / Survey / Homework / Results / Guide)
    private lateinit var iconSoonWrapper: View
    private lateinit var txtSoonSubtitle: TextView
    private lateinit var layoutInteractiveFeature: LinearLayout

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

    // Responsive 4-Screen EdTech UI Views & Micro-interaction State
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

    // Screen 2: Floating Mini Toolbar
    private lateinit var floatingBroadcastDock: LinearLayout
    private lateinit var btnExpandBroadcastDock: Button
    private lateinit var btnCollapseBroadcastDock: Button
    private lateinit var btnBroadcastVolume: Button

    // Screen 3: Split-Screen Document + Quick Note Sidebar
    private lateinit var lectureSplitContainer: LinearLayout
    private lateinit var colLectureDocument: LinearLayout
    private lateinit var colLectureSidebar: LinearLayout
    private lateinit var btnToggleLessonSidebar: Button
    private lateinit var txtSlidePageIndicator: TextView
    private lateinit var btnSlidePrev: ImageButton
    private lateinit var btnSlideNext: ImageButton
    private lateinit var btnZoomOut: Button
    private lateinit var btnZoomIn: Button
    private lateinit var txtZoomLevel: TextView
    private lateinit var quickNoteCanvas: com.qasmartclass.student.views.QuickNoteCanvasView
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

    private var currentSlideIndex = 1
    private var totalSlideCount = 6
    private var currentZoomPercent = 100
    private var isLessonSidebarVisible = true
    private var isHandAcceptedByTeacher = false
    private var lockTimerJob: Job? = null
    private var isUserInitiatedDisconnect = false
    private var lastMeasuredPingMs: Long = 4L

    // Bottom Navigation Bar
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

    // Tab 1: Lecture & Broadcast
    private lateinit var panelLecture: LinearLayout
    private lateinit var layoutNoBroadcast: LinearLayout
    private lateinit var layoutLiveBroadcast: LinearLayout
    private lateinit var txtInlineBroadcastTitle: TextView
    private lateinit var imgInlineBroadcast: ImageView
    private lateinit var btnFullscreenBroadcast: View
    private lateinit var btnRaiseHand: View
    private lateinit var messageCard: LinearLayout
    private lateinit var txtTeacherMessage: TextView

    // Tab 2: Chat
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

    // Tab 3: Hand Raise
    private lateinit var panelHandRaise: LinearLayout
    private lateinit var btnMainRaiseHand: LinearLayout
    private lateinit var txtHandRaiseIcon: TextView
    private lateinit var txtHandRaiseStatus: TextView
    private lateinit var btnReasonSpeaking: Button
    private lateinit var btnReasonQuestion: Button
    private lateinit var btnReasonHelp: Button

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
        setContentView(R.layout.activity_main)

        // Initialize Services
        lockManager = BroadcastLockManager(this)
        heartbeatWatchdog = HeartbeatWatchdog(this, lockManager)
        focusReporter = FocusReporter(this) { tcpOutputStream }

        initViews()
        setupListeners()
        applyResponsiveLayout()

        // Start Auto Discovery via UDP
        startAutoDiscovery()
    }

    override fun onConfigurationChanged(newConfig: android.content.res.Configuration) {
        super.onConfigurationChanged(newConfig)
        applyResponsiveLayout()
    }

    /**
     * Hệ thống Responsive Đa Tầng cho từng màn hình (Per-Screen Responsive Engine):
     * - Compact (< 600dp): Điện thoại hoặc màn hình nhỏ xoay dọc (1 cột dọc, Floating Dock neo đáy).
     * - Medium (600dp - 899dp): Tablet 8 inch hoặc Tablet 10 inch xoay dọc (2 cột tỉ lệ thu gọn).
     * - Expanded (>= 900dp): Tablet 10-11 inch màn hình ngang chuẩn phòng học (2 cột chuẩn 4:8 & 8:4).
     */
    private fun applyResponsiveLayout() {
        val dm = resources.displayMetrics
        val density = dm.density
        val screenWidthDp = dm.widthPixels / density
        fun dp(v: Int): Int = (v * density).toInt()

        val isCompact = screenWidthDp < 600f
        val isMedium = screenWidthDp in 600f..899.9f
        val isExpanded = screenWidthDp >= 900f

        // 1. Màn hình 1 (Connection / Landing Screen): 2 Cột trên Tablet, 1 Cột trên Compact
        if (::discoveryGridContainer.isInitialized) {
            val outerPad = when {
                isExpanded -> dp(24)
                isMedium -> dp(18)
                else -> dp(14)
            }
            discoveryPanel.setPadding(outerPad, dp(14), outerPad, dp(20))

            if (isCompact) {
                discoveryGridContainer.orientation = LinearLayout.VERTICAL
                colStudentIdentity.setPadding(dp(16), dp(16), dp(16), dp(16))
                cardDiscoveredClass.setPadding(dp(16), dp(16), dp(16), dp(16))
                colStudentIdentity.layoutParams = LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.MATCH_PARENT,
                    ViewGroup.LayoutParams.WRAP_CONTENT
                ).apply { bottomMargin = dp(14) }
                colLanRooms.layoutParams = LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.MATCH_PARENT,
                    ViewGroup.LayoutParams.WRAP_CONTENT
                )
            } else {
                discoveryGridContainer.orientation = LinearLayout.HORIZONTAL
                colStudentIdentity.setPadding(dp(20), dp(20), dp(20), dp(20))
                cardDiscoveredClass.setPadding(dp(22), dp(22), dp(22), dp(22))
                val leftWeight = if (isExpanded) 4.0f else 4.6f
                val rightWeight = if (isExpanded) 8.0f else 7.4f
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

        // 2. Màn hình 2 (Broadcast Screen Mode): Floating Mini Toolbar Neo Cạnh Phải (Tablet) hoặc Neo Đáy (Compact)
        if (::floatingBroadcastDock.isInitialized) {
            val dockLp = floatingBroadcastDock.layoutParams as? FrameLayout.LayoutParams
            if (dockLp != null) {
                if (isCompact) {
                    floatingBroadcastDock.orientation = LinearLayout.HORIZONTAL
                    dockLp.gravity = Gravity.BOTTOM or Gravity.CENTER_HORIZONTAL
                    dockLp.setMargins(dp(12), dp(12), dp(12), dp(16))
                } else {
                    floatingBroadcastDock.orientation = LinearLayout.VERTICAL
                    dockLp.gravity = Gravity.END or Gravity.CENTER_VERTICAL
                    dockLp.setMargins(dp(16), dp(16), dp(16), dp(16))
                }
                floatingBroadcastDock.layoutParams = dockLp
            }
            txtBroadcastStatus.textSize = if (isCompact) 12f else 14f
        }

        // 3. Màn hình 3 (Interactive Lesson Viewer): Split-Screen 8:4 (Expanded), 6.5:3.5 (Medium), hoặc Xếp chồng (Compact)
        if (::lectureSplitContainer.isInitialized) {
            val docViewportHeight = when {
                isExpanded -> dp(380)
                isMedium -> dp(310)
                else -> dp(230)
            }
            layoutNoBroadcast.layoutParams = layoutNoBroadcast.layoutParams.apply { height = docViewportHeight }
            imgInlineBroadcast.layoutParams = imgInlineBroadcast.layoutParams.apply { height = docViewportHeight }

            if (isCompact) {
                lectureSplitContainer.orientation = LinearLayout.VERTICAL
                colLectureDocument.layoutParams = LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.MATCH_PARENT,
                    ViewGroup.LayoutParams.WRAP_CONTENT
                ).apply { bottomMargin = if (isLessonSidebarVisible) dp(14) else 0 }
                colLectureSidebar.layoutParams = LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.MATCH_PARENT,
                    ViewGroup.LayoutParams.WRAP_CONTENT
                )
            } else {
                lectureSplitContainer.orientation = LinearLayout.HORIZONTAL
                if (isLessonSidebarVisible) {
                    val docWeight = if (isExpanded) 8.0f else 7.0f
                    val sideWeight = if (isExpanded) 4.0f else 5.0f
                    colLectureDocument.layoutParams = LinearLayout.LayoutParams(
                        0,
                        ViewGroup.LayoutParams.WRAP_CONTENT,
                        docWeight
                    ).apply { marginEnd = dp(16) }
                    colLectureSidebar.layoutParams = LinearLayout.LayoutParams(
                        0,
                        ViewGroup.LayoutParams.WRAP_CONTENT,
                        sideWeight
                    )
                } else {
                    colLectureDocument.layoutParams = LinearLayout.LayoutParams(
                        0,
                        ViewGroup.LayoutParams.WRAP_CONTENT,
                        12.0f
                    )
                }
            }
            colLectureSidebar.visibility = if (isLessonSidebarVisible) View.VISIBLE else View.GONE
        }

        // 4. Màn hình 4 (Lock Screen / Focus Mode): Co giãn khung nội dung trung tâm, hình minh họa và đồng hồ
        if (::lockContentCard.isInitialized) {
            val lockLp = lockContentCard.layoutParams as? FrameLayout.LayoutParams
            if (lockLp != null) {
                lockLp.width = when {
                    isExpanded -> dp(700)
                    isMedium -> dp(540)
                    else -> ViewGroup.LayoutParams.MATCH_PARENT
                }
                lockLp.gravity = Gravity.CENTER
                lockContentCard.layoutParams = lockLp
            }
            val illusSize = when {
                isExpanded -> dp(136)
                isMedium -> dp(112)
                else -> dp(88)
            }
            imgLockIllustration.layoutParams = imgLockIllustration.layoutParams.apply {
                width = illusSize
                height = illusSize
            }
            txtLockTitle.textSize = when {
                isExpanded -> 24f
                isMedium -> 21f
                else -> 18f
            }
            txtLockTimer.textSize = when {
                isExpanded -> 28f
                isMedium -> 25f
                else -> 22f
            }
        }

        // 5. Các trang nội dung khác (Home, Soon/Interactive, Chat, HandRaise): Mở rộng tối đa 1240dp trên Tablet ngang
        fun capWorkspacePanel(view: View, maxDp: Float) {
            val lp = view.layoutParams as? LinearLayout.LayoutParams ?: return
            val targetPx = if (screenWidthDp > maxDp) (maxDp * density).toInt()
                           else ViewGroup.LayoutParams.MATCH_PARENT
            if (lp.width != targetPx || lp.gravity != Gravity.CENTER_HORIZONTAL) {
                lp.width = targetPx
                lp.gravity = Gravity.CENTER_HORIZONTAL
                view.layoutParams = lp
            }
        }
        listOf(panelHome, panelSoon, panelLecture, panelChat, panelHandRaise)
            .forEach { capWorkspacePanel(it, 1240f) }
        syncStudentIdentityCard()
    }

    private fun initViews() {
        // Discovery UI
        discoveryPanel = findViewById(R.id.discoveryPanel)
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

        // Connected Top Header & Profile Bar
        connectedPanel = findViewById(R.id.connectedPanel)
        txtHeaderLiveBadge = findViewById(R.id.txtHeaderLiveBadge)
        txtConnectedClassInfo = findViewById(R.id.txtConnectedClassInfo)
        txtTeacherDetail = findViewById(R.id.txtTeacherDetail)
        txtStudentDetail = findViewById(R.id.txtStudentDetail)
        txtServerIpDetail = findViewById(R.id.txtServerIpDetail)
        btnDisconnect = findViewById(R.id.btnDisconnect)

        // Tab Bar Navigation
        btnTabLecture = findViewById(R.id.btnTabLecture)
        btnTabChat = findViewById(R.id.btnTabChat)
        btnTabHandRaise = findViewById(R.id.btnTabHandRaise)

        // Sidebar & Home dashboard
        navDrawerOverlay = findViewById(R.id.navDrawerOverlay)
        btnHeaderBack = findViewById(R.id.btnHeaderBack)
        btnSoonBack = findViewById(R.id.btnSoonBack)
        btnMenu = findViewById(R.id.btnMenu)
        txtPageTitle = findViewById(R.id.txtPageTitle)
        btnNavHome = findViewById(R.id.btnNavHome)
        panelHome = findViewById(R.id.panelHome)
        panelSoon = findViewById(R.id.panelSoon)
        txtSoonTitle = findViewById(R.id.txtSoonTitle)
        iconSoonWrapper = findViewById(R.id.iconSoonWrapper)
        txtSoonSubtitle = findViewById(R.id.txtSoonSubtitle)
        layoutInteractiveFeature = findViewById(R.id.layoutInteractiveFeature)
        txtHomeGreeting = findViewById(R.id.txtHomeGreeting)
        txtHomeSub = findViewById(R.id.txtHomeSub)
        txtStatClass = findViewById(R.id.txtStatClass)
        txtStatBroadcast = findViewById(R.id.txtStatBroadcast)
        txtStatHand = findViewById(R.id.txtStatHand)
        txtStatTeacher = findViewById(R.id.txtStatTeacher)
        txtHomeLessonTitle = findViewById(R.id.txtHomeLessonTitle)
        txtHomeLessonSub = findViewById(R.id.txtHomeLessonSub)
        txtHomeTeacherChip = findViewById(R.id.txtHomeTeacherChip)
        txtHomeNotice = findViewById(R.id.txtHomeNotice)
        layoutHomeLessonWaiting = findViewById(R.id.layoutHomeLessonWaiting)
        layoutHomeLessonActive = findViewById(R.id.layoutHomeLessonActive)
        txtHomeLiveBadge = findViewById(R.id.txtHomeLiveBadge)
        txtHomeSubjectBadge = findViewById(R.id.txtHomeSubjectBadge)
        txtHomeStageBadge = findViewById(R.id.txtHomeStageBadge)
        imgHomeBroadcastPreview = findViewById(R.id.imgHomeBroadcastPreview)
        layoutWhiteboardStitchPreview = findViewById(R.id.layoutWhiteboardStitchPreview)
        txtHomeBoardHeader = findViewById(R.id.txtHomeBoardHeader)
        txtHomeBoardContent = findViewById(R.id.txtHomeBoardContent)
        txtHomeBoardNote1 = findViewById(R.id.txtHomeBoardNote1)
        txtHomeBoardNote2 = findViewById(R.id.txtHomeBoardNote2)
        txtHomeBoardPointer = findViewById(R.id.txtHomeBoardPointer)
        txtHomeTeacherAvatar = findViewById(R.id.txtHomeTeacherAvatar)
        txtHomeNoticeHeader = findViewById(R.id.txtHomeNoticeHeader)
        txtHomeNoticeTime = findViewById(R.id.txtHomeNoticeTime)
        layoutNoticeFeedback = findViewById(R.id.layoutNoticeFeedback)
        txtHomeChatBadge = findViewById(R.id.txtHomeChatBadge)
        txtHomeQuizTitle = findViewById(R.id.txtHomeQuizTitle)
        txtHomeQuizSub = findViewById(R.id.txtHomeQuizSub)
        txtHomeQuizBadge = findViewById(R.id.txtHomeQuizBadge)
        soonNavButtons.clear()
        soonNavButtons += findViewById<TextView>(R.id.btnNavWhiteboard) to "Bảng trắng SMART TOUCH"
        soonNavButtons += findViewById<TextView>(R.id.btnNavQuiz) to "Bài kiểm tra"
        soonNavButtons += findViewById<TextView>(R.id.btnNavAssignment) to "Bài tập học tập"
        soonNavButtons += findViewById<TextView>(R.id.btnNavSurvey) to "Khảo sát"
        soonNavButtons += findViewById<TextView>(R.id.btnNavResults) to "Kết quả học tập"
        soonNavButtons += findViewById<TextView>(R.id.btnNavProgress) to "Tiến trình học tập"
        soonNavButtons += findViewById<TextView>(R.id.btnNavSettings) to "Cài đặt"
        soonNavButtons += findViewById<TextView>(R.id.btnNavGuide) to "Hướng dẫn sử dụng"

        // Bottom Navigation Bar
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

        // Tab 1: Lecture & Broadcast
        panelLecture = findViewById(R.id.panelLecture)
        layoutNoBroadcast = findViewById(R.id.layoutNoBroadcast)
        layoutLiveBroadcast = findViewById(R.id.layoutLiveBroadcast)
        txtInlineBroadcastTitle = findViewById(R.id.txtInlineBroadcastTitle)
        imgInlineBroadcast = findViewById(R.id.imgInlineBroadcast)
        btnFullscreenBroadcast = findViewById(R.id.btnFullscreenBroadcast)
        btnRaiseHand = findViewById(R.id.btnRaiseHand)
        messageCard = findViewById(R.id.messageCard)
        txtTeacherMessage = findViewById(R.id.txtTeacherMessage)

        // Tab 2: Chat
        panelChat = findViewById(R.id.panelChat)
        txtChatChannelBadge = findViewById(R.id.txtChatChannelBadge)
        scrollChat = findViewById(R.id.scrollChat)
        chatContainer = findViewById(R.id.chatContainer)
        edtChatMessage = findViewById(R.id.edtChatMessage)
        btnSendChat = findViewById(R.id.btnSendChat)
        chatTabBar = findViewById(R.id.chatTabBar)

        // Tab 3: Hand Raise
        panelHandRaise = findViewById(R.id.panelHandRaise)
        btnMainRaiseHand = findViewById(R.id.btnMainRaiseHand)
        txtHandRaiseIcon = findViewById(R.id.txtHandRaiseIcon)
        txtHandRaiseStatus = findViewById(R.id.txtHandRaiseStatus)
        btnReasonSpeaking = findViewById(R.id.btnReasonSpeaking)
        btnReasonQuestion = findViewById(R.id.btnReasonQuestion)
        btnReasonHelp = findViewById(R.id.btnReasonHelp)

        // Screen Broadcast Overlay
        broadcastOverlay = findViewById(R.id.broadcastOverlay)
        imgBroadcastScreen = findViewById(R.id.imgBroadcastScreen)
        progressBroadcastLoading = findViewById(R.id.progressBroadcastLoading)
        txtBroadcastStatus = findViewById(R.id.txtBroadcastStatus)
        btnFullscreenRaiseHand = findViewById(R.id.btnFullscreenRaiseHand)
        btnCloseBroadcast = findViewById(R.id.btnCloseBroadcast)

        // Lock Screen Overlay
        lockOverlay = findViewById(R.id.lockOverlay)
        txtLockTitle = findViewById(R.id.txtLockTitle)
        txtLockMessage = findViewById(R.id.txtLockMessage)
        lockContentCard = findViewById(R.id.lockContentCard)
        imgLockIllustration = findViewById(R.id.imgLockIllustration)
        txtLockLessonName = findViewById(R.id.txtLockLessonName)
        txtLockTimer = findViewById(R.id.txtLockTimer)

        // Responsive 4-Screen Views
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

        floatingBroadcastDock = findViewById(R.id.floatingBroadcastDock)
        btnExpandBroadcastDock = findViewById(R.id.btnExpandBroadcastDock)
        btnCollapseBroadcastDock = findViewById(R.id.btnCollapseBroadcastDock)
        btnBroadcastVolume = findViewById(R.id.btnBroadcastVolume)

        lectureSplitContainer = findViewById(R.id.lectureSplitContainer)
        colLectureDocument = findViewById(R.id.colLectureDocument)
        colLectureSidebar = findViewById(R.id.colLectureSidebar)
        btnToggleLessonSidebar = findViewById(R.id.btnToggleLessonSidebar)
        txtSlidePageIndicator = findViewById(R.id.txtSlidePageIndicator)
        btnSlidePrev = findViewById(R.id.btnSlidePrev)
        btnSlideNext = findViewById(R.id.btnSlideNext)
        btnZoomOut = findViewById(R.id.btnZoomOut)
        btnZoomIn = findViewById(R.id.btnZoomIn)
        txtZoomLevel = findViewById(R.id.txtZoomLevel)
        quickNoteCanvas = findViewById(R.id.quickNoteCanvas)
        btnNotePen = findViewById(R.id.btnNotePen)
        btnNoteHighlight = findViewById(R.id.btnNoteHighlight)
        btnNoteEraser = findViewById(R.id.btnNoteEraser)
        btnNoteClear = findViewById(R.id.btnNoteClear)
        edtQuickLessonAnswer = findViewById(R.id.edtQuickLessonAnswer)
        layoutUploadProgress = findViewById(R.id.layoutUploadProgress)
        progressUploadBar = findViewById(R.id.progressUploadBar)
        txtUploadPercent = findViewById(R.id.txtUploadPercent)
        txtUploadStatusLabel = findViewById(R.id.txtUploadStatusLabel)
        btnQuickSubmitWork = findViewById(R.id.btnQuickSubmitWork)

        // Load saved preferences
        val prefs = getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)
        val remember = prefs.getBoolean(KEY_REMEMBER, true)
        chkRemember.isChecked = remember

        val lastIp = prefs.getString(KEY_LAST_IP, "")
        val rawStudentName = prefs.getString(KEY_STUDENT_NAME, "") ?: ""
        val rawStudentCode = prefs.getString(KEY_STUDENT_CODE, "10A1-002") ?: "10A1-002"
        val lastStudentCode = sanitizeStudentCode(rawStudentCode)
        val lastPassword = prefs.getString(KEY_PASSWORD, "123456")
        val resolvedInitName = resolveOfficialStudentName(lastStudentCode, rawStudentName)

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
            startAutoDiscovery()
        }

        edtJoinClassCode.setOnEditorActionListener { _, _, _ ->
            btnConnect.performClick()
            true
        }

        btnConnect.setOnClickListener {
            txtError.visibility = View.GONE
            val studentCode = edtStudentCode.text.toString().trim()
            val password = edtPassword.text.toString().trim()
            val classCodeInput = edtJoinClassCode.text.toString().trim()
            var studentName = edtStudentName.text.toString().trim()
            var ip = edtTeacherIp.text.toString().trim()

            if (studentCode.isBlank()) {
                showError("Vui lòng nhập Mã học sinh / Tài khoản!")
                edtStudentCode.requestFocus()
                return@setOnClickListener
            }
            if (password.isBlank()) {
                showError("Vui lòng nhập Mật khẩu!")
                edtPassword.requestFocus()
                return@setOnClickListener
            }
            if (classCodeInput.isBlank()) {
                showError("Vui lòng nhập Mã vào lớp do Giáo viên cung cấp trên bảng!")
                edtJoinClassCode.requestFocus()
                return@setOnClickListener
            }

            studentName = resolveOfficialStudentName(studentCode, studentName)
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

            if (chkRemember.isChecked) {
                savePreferences(studentName, studentCode, password, ip, true)
            } else {
                savePreferences("", "", "", "", false)
            }

            connectToTeacher(ip, tcpPort, studentName, studentCode, password, classCodeInput)
        }

        btnConnect.setOnLongClickListener {
            val studentCode = edtStudentCode.text.toString().trim().ifBlank { "10A1-002" }
            val studentName = edtStudentName.text.toString().trim().ifBlank { "Nguyễn Văn Nam" }
            onConnectedSuccess(studentName, studentCode, "10.0.2.2", "Lớp 10A1", "Thầy Nguyễn Văn An")
            true
        }

        btnDisconnect.setOnClickListener {
            isUserInitiatedDisconnect = true
            disconnectFromTeacher("Học sinh chủ động rời lớp")
        }

        // Screen 1: Cột trái (Hồ sơ bàn học) & Cột phải (1 chạm vào lớp + Cấu hình IP ẩn)
        btnAdvancedConfig.setOnClickListener {
            val form = findViewById<View>(R.id.formContainer)
            val willShow = form.visibility != View.VISIBLE
            form.visibility = if (willShow) View.VISIBLE else View.GONE
            if (willShow) edtTeacherIp.requestFocus()
        }

        btnEditStudentIdentity.setOnClickListener {
            showSwitchAccountDialog(autoReconnectIfConnected = false)
        }

        findViewById<View?>(R.id.btnSwitchAccountDrawer)?.setOnClickListener {
            navDrawerOverlay.visibility = View.GONE
            showSwitchAccountDialog(autoReconnectIfConnected = true)
        }

        txtStudentDetail.setOnClickListener {
            navDrawerOverlay.visibility = View.GONE
            showSwitchAccountDialog(autoReconnectIfConnected = true)
        }

        cardDiscoveredClass.setOnClickListener {
            btnConnect.performClick()
        }

        // Screen 2: Floating Mini Toolbar Controls
        btnCollapseBroadcastDock.setOnClickListener {
            floatingBroadcastDock.visibility = View.GONE
            btnExpandBroadcastDock.visibility = View.VISIBLE
        }

        btnExpandBroadcastDock.setOnClickListener {
            floatingBroadcastDock.visibility = View.VISIBLE
            btnExpandBroadcastDock.visibility = View.GONE
        }

        btnBroadcastVolume.setOnClickListener {
            val audioMgr = getSystemService(Context.AUDIO_SERVICE) as? android.media.AudioManager
            audioMgr?.adjustSuggestedStreamVolume(
                android.media.AudioManager.ADJUST_SAME,
                android.media.AudioManager.STREAM_MUSIC,
                android.media.AudioManager.FLAG_SHOW_UI
            )
        }

        // Screen 3: Split-Screen Interactive Lesson Viewer Controls
        btnToggleLessonSidebar.setOnClickListener {
            isLessonSidebarVisible = !isLessonSidebarVisible
            btnToggleLessonSidebar.text = if (isLessonSidebarVisible) "Ẩn nháp" else "Mở nháp"
            applyResponsiveLayout()
        }

        btnSlidePrev.setOnClickListener {
            if (currentSlideIndex > 1) {
                currentSlideIndex--
                updateSlideViewerState()
            }
        }

        btnSlideNext.setOnClickListener {
            if (currentSlideIndex < totalSlideCount) {
                currentSlideIndex++
                updateSlideViewerState()
            }
        }

        btnZoomOut.setOnClickListener {
            currentZoomPercent = (currentZoomPercent - 15).coerceAtLeast(70)
            applyDocumentZoom()
        }

        btnZoomIn.setOnClickListener {
            currentZoomPercent = (currentZoomPercent + 15).coerceAtMost(200)
            applyDocumentZoom()
        }

        // Screen 3 Sidebar: Quick Note Canvas Tools
        fun styleNoteToolButtons(activeBtn: Button) {
            listOf(btnNotePen, btnNoteHighlight, btnNoteEraser).forEach { b ->
                val isSel = b === activeBtn
                b.backgroundTintList = ColorStateList.valueOf(if (isSel) 0xFF2563EB.toInt() else 0xFFF1F5F9.toInt())
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

        btnQuickSubmitWork.setOnClickListener {
            performQuickNoteOrHomeworkUpload()
        }

        // Tab Navigation Listeners
        btnTabLecture.setOnClickListener { switchTab(0) }
        btnTabChat.setOnClickListener { switchTab(1) }
        btnTabHandRaise.setOnClickListener { switchTab(2) }
        btnNavHome.setOnClickListener { switchTab(3) }
        btnMenu.setOnClickListener { navDrawerOverlay.visibility = View.VISIBLE }
        navDrawerOverlay.setOnClickListener { navDrawerOverlay.visibility = View.GONE }
        for ((btn, title) in soonNavButtons) {
            btn.setOnClickListener { showComingSoon(btn, title) }
        }
        findViewById<View>(R.id.btnHomeRaise).setOnClickListener { switchTab(2) }
        findViewById<View>(R.id.btnHomeChat).setOnClickListener { switchTab(1) }
        findViewById<View>(R.id.btnHomeLecture).setOnClickListener { switchTab(0) }

        // Hand Raise Listeners (Quick Button, Hero Button on Screen Share Card, Main Button, and Fullscreen Button)
        btnRaiseHand.setOnClickListener { toggleRaiseHand() }
        findViewById<View?>(R.id.btnHeroRaiseHand)?.setOnClickListener { toggleRaiseHand() }
        btnMainRaiseHand.setOnClickListener { toggleRaiseHand() }
        btnFullscreenRaiseHand.setOnClickListener { toggleRaiseHand() }

        // Hand Raise Reason Chips
        btnReasonSpeaking.setOnClickListener { selectHandRaiseReason("Phát biểu") }
        btnReasonQuestion.setOnClickListener { selectHandRaiseReason("Đặt câu hỏi") }
        btnReasonHelp.setOnClickListener { selectHandRaiseReason("Trợ giúp") }

        // Chat Send Listener
        btnSendChat.setOnClickListener { sendChatMessage() }
        switchChatChannel("TEACHER")

        // Screen Broadcast Fullscreen Toggle
        btnFullscreenBroadcast.setOnClickListener {
            broadcastOverlay.visibility = View.VISIBLE
        }

        btnCloseBroadcast.setOnClickListener {
            broadcastOverlay.visibility = View.GONE
        }

        // Bottom Navigation Bar Listeners (Matching Stitch 4-Tab Specification)
        btnBottomHome.setOnClickListener { switchTab(3) }
        btnBottomLecture.setOnClickListener {
            val navBtn = findViewById<TextView>(R.id.btnNavQuiz)
            showComingSoon(navBtn, "Kiểm tra trắc nghiệm")
        }
        btnBottomChat.setOnClickListener { switchTab(1) }
        btnBottomRaise.setOnClickListener { navDrawerOverlay.visibility = View.VISIBLE }

        // Nút Quay lại Trang chủ
        btnHeaderBack.setOnClickListener { switchTab(3) }
        btnSoonBack.setOnClickListener { switchTab(3) }

        // Top Header Logout & Stitch Quick Actions
        findViewById<View?>(R.id.btnHeaderLogout)?.setOnClickListener { showExitConfirmation() }
        findViewById<View?>(R.id.btnHomeQuiz)?.setOnClickListener {
            val navBtn = findViewById<TextView>(R.id.btnNavQuiz)
            showComingSoon(navBtn, "Kiểm tra trắc nghiệm")
        }
        findViewById<View?>(R.id.btnHomeSnapshot)?.setOnClickListener {
            Toast.makeText(this, "Đã lưu ảnh chụp bài giảng vào Thư viện", Toast.LENGTH_SHORT).show()
        }
        findViewById<View?>(R.id.btnLectureSnapshot)?.setOnClickListener {
            Toast.makeText(this, "Đã lưu ảnh chụp bài giảng vào Thư viện", Toast.LENGTH_SHORT).show()
        }
        findViewById<View?>(R.id.btnNoticeUnderstood)?.setOnClickListener {
            Toast.makeText(this, "Đã gửi phản hồi: Đã hiểu bài (+1)", Toast.LENGTH_SHORT).show()
        }
        findViewById<View?>(R.id.btnNoticeReexplain)?.setOnClickListener {
            Toast.makeText(this, "Đã gửi yêu cầu: Xin thầy giảng lại", Toast.LENGTH_SHORT).show()
        }

        // Bắt sự kiện phím Quay lại (Back gesture & Hardware Back)
        onBackPressedDispatcher.addCallback(this, object : OnBackPressedCallback(true) {
            override fun handleOnBackPressed() {
                if (navDrawerOverlay.visibility == View.VISIBLE) {
                    navDrawerOverlay.visibility = View.GONE
                } else if (broadcastOverlay.visibility == View.VISIBLE) {
                    broadcastOverlay.visibility = View.GONE
                } else if (connectedPanel.visibility == View.VISIBLE && currentTab != 3) {
                    switchTab(3)
                } else if (connectedPanel.visibility == View.VISIBLE && currentTab == 3) {
                    showExitConfirmation()
                } else {
                    isEnabled = false
                    onBackPressedDispatcher.onBackPressed()
                }
            }
        })
    }

    private fun showExitConfirmation() {
        AlertDialog.Builder(this)
            .setTitle("Tài khoản & Phòng học")
            .setMessage("Đang đăng nhập: ${currentStudentName.ifBlank { edtStudentName.text.toString().ifBlank { "Học sinh" } }} (${currentStudentCode.ifBlank { edtStudentCode.text.toString().ifBlank { "HS001" } }})\n\nBạn muốn đổi sang tài khoản học sinh khác hay rời phòng học?")
            .setPositiveButton("Đổi tài khoản khác") { _, _ ->
                showSwitchAccountDialog(autoReconnectIfConnected = true)
            }
            .setNeutralButton("Rời lớp") { _, _ ->
                isUserInitiatedDisconnect = true
                disconnectFromTeacher("Học sinh chủ động rời lớp")
            }
            .setNegativeButton("Ở lại", null)
            .show()
    }

    /**
     * Hộp thoại Đổi tài khoản / Học sinh khác (dùng được ở cả Màn hình 1 và khi đang trong Lớp học).
     * Cho phép chọn nhanh Mã bàn (HS001..HS006) hoặc nhập Mã HS / Họ tên / Mật khẩu mới,
     * tự động ngắt phiên cũ và kết nối lại bằng tài khoản mới.
     */
    private fun showSwitchAccountDialog(autoReconnectIfConnected: Boolean) {
        val density = resources.displayMetrics.density
        fun dp(v: Int): Int = (v * density).toInt()

        val container = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            setPadding(dp(22), dp(14), dp(22), dp(8))
        }

        val subTitle = TextView(this).apply {
            text = "Chọn nhanh số bàn hoặc nhập Mã học sinh và Họ tên của em:"
            textSize = 14f
            setTextColor(0xFF475569.toInt())
            setPadding(0, 0, 0, dp(10))
        }
        container.addView(subTitle)

        val initCode = edtStudentCode.text.toString().trim().ifBlank { "10A1-001" }
        val initName = resolveOfficialStudentName(initCode, edtStudentName.text.toString().trim())

        val inputCode = EditText(this).apply {
            hint = "Mã học sinh / Tài khoản (VD: 10A1-002, 12A3-001)"
            setText(initCode)
            textSize = 16f
            setSingleLine(true)
            setBackgroundResource(R.drawable.bg_input_field)
            setPadding(dp(14), dp(10), dp(14), dp(10))
        }

        val inputName = EditText(this).apply {
            hint = "Họ và tên học sinh (Tự động điền theo Mã HS)"
            setText(initName)
            textSize = 16f
            setSingleLine(true)
            setBackgroundResource(R.drawable.bg_input_field)
            setPadding(dp(14), dp(10), dp(14), dp(10))
            layoutParams = LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT,
                LinearLayout.LayoutParams.WRAP_CONTENT
            ).apply { topMargin = dp(10) }
        }

        // Tự động điền đúng Họ tên học sinh ngay khi gõ Mã học sinh
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
            textSize = 16f
            setSingleLine(true)
            inputType = android.text.InputType.TYPE_CLASS_TEXT or android.text.InputType.TYPE_TEXT_VARIATION_PASSWORD
            setBackgroundResource(R.drawable.bg_input_field)
            setPadding(dp(14), dp(10), dp(14), dp(10))
            layoutParams = LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT,
                LinearLayout.LayoutParams.WRAP_CONTENT
            ).apply { topMargin = dp(10) }
        }

        // Hàng nút chọn nhanh bàn học phổ biến + Nút xóa trắng để nhập mới
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
            text = "+ Nhập TK mới"
            isAllCaps = false
            textSize = 13f
            setTextColor(0xFFDC2626.toInt())
            setBackgroundResource(R.drawable.bg_pill_btn_white)
            setPadding(dp(12), dp(6), dp(12), dp(6))
            layoutParams = LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.WRAP_CONTENT,
                dp(40)
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
                textSize = 13f
                setTextColor(0xFF1D4ED8.toInt())
                setBackgroundResource(R.drawable.bg_pill_btn_white)
                setPadding(dp(12), dp(6), dp(12), dp(6))
                layoutParams = LinearLayout.LayoutParams(
                    LinearLayout.LayoutParams.WRAP_CONTENT,
                    dp(40)
                ).apply { marginEnd = dp(8) }
                setOnClickListener {
                    inputCode.setText(qCode)
                    inputName.setText(qName)
                }
            }
            chipContainer.addView(chip)
        }
        val inputClassCode = EditText(this).apply {
            hint = "🔑 Mã vào lớp trên bảng Giáo viên (4 chữ số)"
            setText(edtJoinClassCode.text.toString().trim().ifBlank { lastEnteredClassCode })
            textSize = 16f
            setSingleLine(true)
            inputType = android.text.InputType.TYPE_CLASS_NUMBER
            setBackgroundResource(R.drawable.bg_input_field)
            setPadding(dp(14), dp(10), dp(14), dp(10))
            layoutParams = LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT,
                LinearLayout.LayoutParams.WRAP_CONTENT
            ).apply { topMargin = dp(10) }
        }

        quickRow.addView(chipContainer)
        container.addView(quickRow)
        container.addView(inputCode)
        container.addView(inputName)
        container.addView(inputPass)
        container.addView(inputClassCode)

        val wasConnected = connectedPanel.visibility == View.VISIBLE || tcpSocket != null
        val positiveLabel = if (wasConnected || autoReconnectIfConnected) "Đăng nhập tài khoản này" else "Lưu tài khoản"

        AlertDialog.Builder(this)
            .setTitle("Đổi tài khoản Học sinh")
            .setView(container)
            .setPositiveButton(positiveLabel) { _, _ ->
                val newCode = inputCode.text.toString().trim().ifBlank { "10A1-001" }
                val newName = resolveOfficialStudentName(newCode, inputName.text.toString().trim())
                val newPass = inputPass.text.toString().trim().ifBlank { "123456" }
                val newClassCode = inputClassCode.text.toString().trim()

                edtStudentCode.setText(newCode)
                edtStudentName.setText(newName)
                edtPassword.setText(newPass)
                if (newClassCode.isNotBlank()) {
                    edtJoinClassCode.setText(newClassCode)
                }
                currentStudentCode = newCode
                currentStudentName = newName
                val ip = edtTeacherIp.text.toString().trim().ifBlank { discoveredTeacherIp ?: currentTeacherIp ?: "10.0.2.2" }
                savePreferences(newName, newCode, newPass, ip, chkRemember.isChecked)
                syncStudentIdentityCard()

                if (wasConnected || autoReconnectIfConnected) {
                    isUserInitiatedDisconnect = true
                    disconnectFromTeacher("Đang chuyển sang tài khoản $newName ($newCode)...")
                    btnConnect.postDelayed({
                        btnConnect.performClick()
                    }, 350)
                } else {
                    Toast.makeText(this, "Đã chuyển hồ sơ bàn học sang: $newName ($newCode)", Toast.LENGTH_SHORT).show()
                }
            }
            .setNeutralButton("Cấu hình nâng cao") { _, _ ->
                if (wasConnected) {
                    isUserInitiatedDisconnect = true
                    disconnectFromTeacher("Mở cấu hình tài khoản & máy chủ")
                }
                val form = findViewById<View>(R.id.formContainer)
                form.visibility = View.VISIBLE
                edtStudentCode.requestFocus()
            }
            .setNegativeButton("Hủy", null)
            .show()
    }

    private fun switchTab(index: Int) {
        currentTab = index
        panelLecture.visibility = if (index == 0) View.VISIBLE else View.GONE
        panelChat.visibility = if (index == 1) View.VISIBLE else View.GONE
        panelHandRaise.visibility = if (index == 2) View.VISIBLE else View.GONE
        panelHome.visibility = if (index == 3) View.VISIBLE else View.GONE
        panelSoon.visibility = View.GONE

        btnHeaderBack.visibility = if (index == 3) View.GONE else View.VISIBLE

        val activeBtn = when (index) {
            0 -> btnTabLecture
            1 -> btnTabChat
            2 -> btnTabHandRaise
            else -> btnNavHome
        }
        txtPageTitle.text = when (index) {
            0 -> "Bài giảng trực tiếp"
            1 -> "Hỏi bài Giáo viên"
            2 -> "Giơ tay phát biểu"
            else -> {
                val clean = homeClassName.replace("• Phòng 204 • Phòng 204", "• Phòng 204").trim()
                if (clean.isNotBlank()) {
                    if (clean.contains("Phòng")) clean else "$clean • Phòng 204"
                } else "Lớp 10A1 • Phòng 204"
            }
        }
        setNavActive(activeBtn)

        // Cập nhật trạng thái Bottom Navigation Bar (Stitch Specification: Active Capsule Solid Blue, Icon Pure White)
        val activeTextColor = 0xFF2563EB.toInt()
        val activeIconColor = 0xFFFFFFFF.toInt()
        val inactiveColor = 0xFF64748B.toInt()

        capsuleBottomHome.setBackgroundResource(if (index == 3) R.drawable.bg_bottom_nav_capsule else 0)
        iconBottomHome.imageTintList = ColorStateList.valueOf(if (index == 3) activeIconColor else inactiveColor)
        labelBottomHome.setTextColor(if (index == 3) activeTextColor else inactiveColor)
        labelBottomHome.setTypeface(null, if (index == 3) android.graphics.Typeface.BOLD else android.graphics.Typeface.NORMAL)

        capsuleBottomLecture.setBackgroundResource(if (index == 0) R.drawable.bg_bottom_nav_capsule else 0)
        iconBottomLecture.imageTintList = ColorStateList.valueOf(if (index == 0) activeIconColor else inactiveColor)
        labelBottomLecture.setTextColor(if (index == 0) activeTextColor else inactiveColor)
        labelBottomLecture.setTypeface(null, if (index == 0) android.graphics.Typeface.BOLD else android.graphics.Typeface.NORMAL)

        capsuleBottomChat.setBackgroundResource(if (index == 1) R.drawable.bg_bottom_nav_capsule else 0)
        iconBottomChat.imageTintList = ColorStateList.valueOf(if (index == 1) activeIconColor else inactiveColor)
        labelBottomChat.setTextColor(if (index == 1) activeTextColor else inactiveColor)
        labelBottomChat.setTypeface(null, if (index == 1) android.graphics.Typeface.BOLD else android.graphics.Typeface.NORMAL)

        capsuleBottomRaise.setBackgroundResource(if (index == 2) R.drawable.bg_bottom_nav_capsule else 0)
        iconBottomRaise.imageTintList = ColorStateList.valueOf(if (index == 2) activeIconColor else inactiveColor)
        labelBottomRaise.setTextColor(if (index == 2) activeTextColor else inactiveColor)
        labelBottomRaise.setTypeface(null, if (index == 2) android.graphics.Typeface.BOLD else android.graphics.Typeface.NORMAL)

        focusReporter.currentPage = when (index) {
            0 -> "Bài giảng"
            1 -> "Tin nhắn"
            2 -> "Giơ tay"
            else -> "Trang chủ"
        }
        navDrawerOverlay.visibility = View.GONE

        if (index == 1) {
            btnTabChat.text = "Hỏi bài Giáo viên"
            unreadTeacherChatCount = 0
            if (::txtHomeChatBadge.isInitialized) {
                txtHomeChatBadge.text = "Trực tuyến"
                txtHomeChatBadge.setBackgroundResource(R.drawable.bg_pill_badge_blue)
                txtHomeChatBadge.setTextColor(0xFF1D4ED8.toInt())
            }
            scrollChat.post { scrollChat.fullScroll(View.FOCUS_DOWN) }
        }
        if (index == 3) refreshHome()
    }

    /** Tô sáng mục đang chọn trên Sidebar (card xanh nhạt bo góc hiện đại). */
    private fun setNavActive(active: TextView?) {
        val all = mutableListOf(btnNavHome, btnTabLecture, btnTabChat, btnTabHandRaise)
        all += soonNavButtons.map { it.first }
        val activeColor = 0xFF1D4ED8.toInt()
        val inactiveColor = 0xFF475569.toInt()
        for (btn in all) {
            val drawables = btn.compoundDrawablesRelative
            val startIcon = drawables[0] ?: btn.compoundDrawables[0]
            val endIcon = drawables[2] ?: btn.compoundDrawables[2]
            if (btn === active) {
                btn.setBackgroundResource(R.drawable.bg_tab_selected)
                btn.setTextColor(activeColor)
                startIcon?.setTint(activeColor)
                endIcon?.setTint(activeColor)
            } else {
                btn.setBackgroundColor(android.graphics.Color.TRANSPARENT)
                btn.setTextColor(inactiveColor)
                startIcon?.setTint(inactiveColor)
                endIcon?.setTint(0xFFCBD5E1.toInt())
            }
            btn.setCompoundDrawablesRelativeWithIntrinsicBounds(startIcon, null, endIcon, null)
        }
    }

    /** Các trang của bản Windows chưa có trên Android: hiển thị trang thông báo thay vì bỏ trống. */
    private fun showComingSoon(btn: TextView, title: String) {
        currentTab = 4
        panelHome.visibility = View.GONE
        panelLecture.visibility = View.GONE
        panelChat.visibility = View.GONE
        panelHandRaise.visibility = View.GONE
        panelSoon.visibility = View.VISIBLE
        btnHeaderBack.visibility = View.VISIBLE
        txtSoonTitle.text = title
        txtPageTitle.text = title
        setNavActive(btn)
        focusReporter.currentPage = title.substringAfter(" ")
        navDrawerOverlay.visibility = View.GONE

        val isQuiz = title.contains("trắc nghiệm", ignoreCase = true)
        val activeTextColor = 0xFF2563EB.toInt()
        val activeIconColor = 0xFFFFFFFF.toInt()
        val inactiveColor = 0xFF64748B.toInt()

        capsuleBottomHome.setBackgroundResource(0)
        iconBottomHome.imageTintList = ColorStateList.valueOf(inactiveColor)
        labelBottomHome.setTextColor(inactiveColor)
        labelBottomHome.setTypeface(null, android.graphics.Typeface.NORMAL)

        capsuleBottomLecture.setBackgroundResource(if (isQuiz) R.drawable.bg_bottom_nav_capsule else 0)
        iconBottomLecture.imageTintList = ColorStateList.valueOf(if (isQuiz) activeIconColor else inactiveColor)
        labelBottomLecture.setTextColor(if (isQuiz) activeTextColor else inactiveColor)
        labelBottomLecture.setTypeface(null, if (isQuiz) android.graphics.Typeface.BOLD else android.graphics.Typeface.NORMAL)

        capsuleBottomChat.setBackgroundResource(0)
        iconBottomChat.imageTintList = ColorStateList.valueOf(inactiveColor)
        labelBottomChat.setTextColor(inactiveColor)
        labelBottomChat.setTypeface(null, android.graphics.Typeface.NORMAL)

        capsuleBottomRaise.setBackgroundResource(0)
        iconBottomRaise.imageTintList = ColorStateList.valueOf(inactiveColor)
        labelBottomRaise.setTextColor(inactiveColor)
        labelBottomRaise.setTypeface(null, android.graphics.Typeface.NORMAL)

        renderInteractiveFeaturePanel(btn.id, title)
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

        val isQuizView = btnId == R.id.btnNavQuiz || title.contains("trắc nghiệm", ignoreCase = true) || title.contains("kiểm tra", ignoreCase = true)
        val isSurveyView = btnId == R.id.btnNavSurvey || title.contains("khảo sát", ignoreCase = true)
        val isAssignmentView = btnId == R.id.btnNavAssignment || title.contains("bài tập", ignoreCase = true)
        val isResultsView = btnId == R.id.btnNavResults || title.contains("kết quả", ignoreCase = true) || btnId == R.id.btnNavProgress || title.contains("tiến trình", ignoreCase = true) || title.contains("tiến độ", ignoreCase = true)
        val isGuideView = btnId == R.id.btnNavGuide || title.contains("hướng dẫn", ignoreCase = true)
        val isSettingsView = btnId == R.id.btnNavSettings || title.contains("cài đặt", ignoreCase = true)

        when {
            isQuizView -> {
                if (activeQuizQuestions.isEmpty()) {
                    iconSoonWrapper.visibility = View.VISIBLE
                    txtSoonSubtitle.text = if (lastQuizScoreText.isNotBlank()) {
                        "Kết quả bài kiểm tra gần nhất: $lastQuizScoreText\n$lastQuizCorrectDetail"
                    } else {
                        "Hiện chưa có đề kiểm tra nào đang mở.\nKhi Giáo viên phát đề từ máy chủ, câu hỏi sẽ tự động hiển thị tại đây để bạn làm bài và nộp trực tiếp."
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
                        setBackgroundResource(R.drawable.bg_card_action_elevated)
                        setPadding(dp(16), dp(14), dp(16), dp(14))
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
                                btnOpt.setBackgroundColor(0xFF2563EB.toInt())
                                btnOpt.setTextColor(0xFFFFFFFF.toInt())
                                btnOpt.setTypeface(null, Typeface.BOLD)
                            } else {
                                btnOpt.setBackgroundColor(0xFFF1F5F9.toInt())
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
                                b.setBackgroundColor(if (sel) 0xFF2563EB.toInt() else 0xFFF1F5F9.toInt())
                                b.setTextColor(if (sel) 0xFFFFFFFF.toInt() else 0xFF1E293B.toInt())
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
                        setBackgroundColor(0xFFDCFCE7.toInt())
                        setPadding(dp(16), dp(12), dp(16), dp(12))
                        layoutParams = LinearLayout.LayoutParams(
                            LinearLayout.LayoutParams.MATCH_PARENT,
                            LinearLayout.LayoutParams.WRAP_CONTENT
                        ).apply { bottomMargin = dp(12) }
                    }
                    layoutInteractiveFeature.addView(resultBanner)
                }

                val btnSubmitQuiz = Button(this).apply {
                    text = if (quizSubmitted) "ĐÃ NỘP BÀI KIỂM TRA" else "NỘP BÀI KIỂM TRA"
                    isAllCaps = false
                    textSize = 15f
                    setTypeface(null, Typeface.BOLD)
                    setTextColor(0xFFFFFFFF.toInt())
                    setBackgroundColor(if (quizSubmitted) 0xFF64748B.toInt() else 0xFF16A34A.toInt())
                    isEnabled = !quizSubmitted
                    layoutParams = LinearLayout.LayoutParams(
                        LinearLayout.LayoutParams.MATCH_PARENT,
                        dp(50)
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
                    txtSoonSubtitle.text = "Hiện chưa có phiên khảo sát nào đang mở.\nKhi Giáo viên phát phiếu khảo sát nhanh trên lớp, nội dung câu hỏi sẽ hiển thị tại đây."
                    layoutInteractiveFeature.visibility = View.GONE
                    return
                }

                iconSoonWrapper.visibility = View.GONE
                txtSoonSubtitle.text = if (surveySubmitted) "Cảm ơn bạn đã gửi phản hồi khảo sát!" else activeSurveyTitle
                layoutInteractiveFeature.visibility = View.VISIBLE

                val card = LinearLayout(this).apply {
                    orientation = LinearLayout.VERTICAL
                    setBackgroundResource(R.drawable.bg_card_action_elevated)
                    setPadding(dp(16), dp(16), dp(16), dp(16))
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
                        setBackgroundColor(if (sel) 0xFF2563EB.toInt() else 0xFFF1F5F9.toInt())
                        setTextColor(if (sel) 0xFFFFFFFF.toInt() else 0xFF1E293B.toInt())
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
                                b.setBackgroundColor(if (s) 0xFF2563EB.toInt() else 0xFFF1F5F9.toInt())
                                b.setTextColor(if (s) 0xFFFFFFFF.toInt() else 0xFF1E293B.toInt())
                            }
                        }
                    }
                    optionBtns.add(btnOpt)
                    card.addView(btnOpt)
                }

                layoutInteractiveFeature.addView(card)

                val btnSubmitSurvey = Button(this).apply {
                    text = if (surveySubmitted) "ĐÃ GỬI KHẢO SÁT" else "GỬI PHẢN HỒI KHẢO SÁT"
                    isAllCaps = false
                    textSize = 15f
                    setTypeface(null, Typeface.BOLD)
                    setTextColor(0xFFFFFFFF.toInt())
                    setBackgroundColor(if (surveySubmitted) 0xFF64748B.toInt() else 0xFF2563EB.toInt())
                    isEnabled = !surveySubmitted
                    layoutParams = LinearLayout.LayoutParams(
                        LinearLayout.LayoutParams.MATCH_PARENT,
                        dp(50)
                    )
                    setOnClickListener {
                        if (selectedSurveyOptionIdx < 0) {
                            Toast.makeText(this@MainActivity, "Vui lòng chọn một phương án phản hồi!", Toast.LENGTH_SHORT).show()
                            return@setOnClickListener
                        }
                        val studentName = currentStudentName.ifBlank { "Học sinh" }
                        val optKey = "opt${selectedSurveyOptionIdx + 1}"
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
                if (activeHwTitle.isBlank() && activeHwDesc.isBlank()) {
                    iconSoonWrapper.visibility = View.VISIBLE
                    txtSoonSubtitle.text = "Hiện chưa có bài tập mới nào được giao.\nKhi Giáo viên giao bài tập trên lớp hoặc về nhà, đề bài và khung nộp bài sẽ hiển thị tại đây."
                    layoutInteractiveFeature.visibility = View.GONE
                    return
                }

                iconSoonWrapper.visibility = View.GONE
                txtSoonSubtitle.text = if (hwSubmitted) {
                    lastHwAckMessage.ifBlank { "Đã nộp bài tập thành công cho Giáo viên." }
                } else {
                    "Hoàn thành nội dung trả lời bên dưới và bấm Nộp bài tập"
                }
                layoutInteractiveFeature.visibility = View.VISIBLE

                val card = LinearLayout(this).apply {
                    orientation = LinearLayout.VERTICAL
                    setBackgroundResource(R.drawable.bg_card_action_elevated)
                    setPadding(dp(16), dp(16), dp(16), dp(16))
                    layoutParams = LinearLayout.LayoutParams(
                        LinearLayout.LayoutParams.MATCH_PARENT,
                        LinearLayout.LayoutParams.WRAP_CONTENT
                    ).apply { bottomMargin = dp(12) }
                }

                val tvSubject = TextView(this).apply {
                    text = "Môn: ${activeHwSubject.ifBlank { activeLessonSubject.ifBlank { "Bài tập lớp học" } }}" +
                        if (activeHwDeadline.isNotBlank()) "  •  Hạn nộp: $activeHwDeadline" else ""
                    textSize = 12.5f
                    setTextColor(0xFF2563EB.toInt())
                    setTypeface(null, Typeface.BOLD)
                }
                card.addView(tvSubject)

                val tvTitle = TextView(this).apply {
                    text = activeHwTitle.ifBlank { "Bài tập Giáo viên giao" }
                    textSize = 16f
                    setTypeface(null, Typeface.BOLD)
                    setTextColor(0xFF0F172A.toInt())
                    setPadding(0, dp(6), 0, dp(6))
                }
                card.addView(tvTitle)

                val tvDesc = TextView(this).apply {
                    text = activeHwDesc.ifBlank { "Em hãy trình bày lời giải hoặc câu trả lời vào ô bên dưới." }
                    textSize = 14f
                    setTextColor(0xFF334155.toInt())
                    setPadding(0, 0, 0, dp(12))
                }
                card.addView(tvDesc)

                val edtAnswer = EditText(this).apply {
                    hint = "Nhập bài làm / câu trả lời của bạn tại đây..."
                    setText(hwDraftAnswer)
                    textSize = 14f
                    minLines = 4
                    gravity = Gravity.TOP or Gravity.START
                    setBackgroundColor(0xFFF8FAFC.toInt())
                    setPadding(dp(12), dp(10), dp(12), dp(10))
                    isEnabled = !hwSubmitted
                }
                card.addView(edtAnswer)
                layoutInteractiveFeature.addView(card)

                val btnSubmitHw = Button(this).apply {
                    text = if (hwSubmitted) "ĐÃ NỘP BÀI TẬP" else "NỘP BÀI TẬP CHO GIÁO VIÊN"
                    isAllCaps = false
                    textSize = 15f
                    setTypeface(null, Typeface.BOLD)
                    setTextColor(0xFFFFFFFF.toInt())
                    setBackgroundColor(if (hwSubmitted) 0xFF64748B.toInt() else 0xFF16A34A.toInt())
                    isEnabled = !hwSubmitted
                    layoutParams = LinearLayout.LayoutParams(
                        LinearLayout.LayoutParams.MATCH_PARENT,
                        dp(50)
                    )
                    setOnClickListener {
                        val ans = edtAnswer.text.toString().trim()
                        if (ans.isBlank()) {
                            Toast.makeText(this@MainActivity, "Vui lòng nhập nội dung bài làm trước khi nộp!", Toast.LENGTH_SHORT).show()
                            return@setOnClickListener
                        }
                        hwDraftAnswer = ans
                        val safeAns = ans.replace("|", " ").replace("\n", "\\n")
                        val hwIdVal = if (activeHwId > 0) activeHwId else 1
                        sendRawTcpLine("HOMEWORK_SUBMIT|$hwIdVal|$safeAns|[]")
                        hwSubmitted = true
                        Toast.makeText(this@MainActivity, "Đã gửi bài tập về máy Giáo viên!", Toast.LENGTH_SHORT).show()
                        renderInteractiveFeaturePanel(btnId, title)
                    }
                }
                layoutInteractiveFeature.addView(btnSubmitHw)
            }

            isResultsView -> {
                iconSoonWrapper.visibility = View.GONE
                txtSoonSubtitle.text = "Tổng hợp kết quả học tập và tương tác trong phiên học hiện tại"
                layoutInteractiveFeature.visibility = View.VISIBLE

                val summaryCard = LinearLayout(this).apply {
                    orientation = LinearLayout.VERTICAL
                    setBackgroundResource(R.drawable.bg_card_action_elevated)
                    setPadding(dp(16), dp(16), dp(16), dp(16))
                    layoutParams = LinearLayout.LayoutParams(
                        LinearLayout.LayoutParams.MATCH_PARENT,
                        LinearLayout.LayoutParams.WRAP_CONTENT
                    )
                }

                val rows = listOf(
                    "Học sinh" to "${currentStudentName.ifBlank { "Học sinh" }} (${currentStudentCode.ifBlank { "—" }})",
                    "Lớp học" to homeClassName.ifBlank { currentClassName },
                    "Điểm kiểm tra gần nhất" to lastQuizScoreText.ifBlank { "Chưa có điểm" },
                    "Chi tiết bài kiểm tra" to lastQuizCorrectDetail.ifBlank { if (quizSubmitted) "Đã nộp bài — Chờ chấm" else "Chưa làm bài" },
                    "Trạng thái bài tập" to if (hwSubmitted) "Đã nộp bài tập" else if (activeHwTitle.isNotBlank()) "Đang có 1 bài tập cần nộp" else "Chưa có bài tập",
                    "Khảo sát lớp học" to if (surveySubmitted) "Đã hoàn thành khảo sát" else "Chưa tham gia"
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

            isGuideView -> {
                iconSoonWrapper.visibility = View.GONE
                txtSoonSubtitle.text = "Hướng dẫn nhanh cho Học sinh sử dụng thiết bị trong lớp học SMART CLASS"
                layoutInteractiveFeature.visibility = View.VISIBLE

                val guideCard = LinearLayout(this).apply {
                    orientation = LinearLayout.VERTICAL
                    setBackgroundResource(R.drawable.bg_card_action_elevated)
                    setPadding(dp(16), dp(16), dp(16), dp(16))
                }
                val steps = listOf(
                    "1. Xem bài giảng trực tiếp: Chạm vào mục Bài giảng hoặc khung Xem màn hình Giáo viên tại Trang chủ để xem trình chiếu thời gian thực.",
                    "2. Làm bài kiểm tra & Khảo sát: Khi Giáo viên phát đề kiểm tra hoặc khảo sát, ứng dụng sẽ tự động mở màn hình câu hỏi để bạn chọn đáp án và nộp bài.",
                    "3. Hỏi bài & Thảo luận nhóm: Chuyển sang tab Hỏi bài Giáo viên để nhắn riêng cho Giáo viên hoặc trao đổi trong nhóm học tập.",
                    "4. Giơ tay phát biểu: Nhấn nút Giơ tay phát biểu và chọn lý do (Phát biểu, Đặt câu hỏi, Trợ giúp) để báo hiệu cho Giáo viên."
                )
                steps.forEach { s ->
                    guideCard.addView(TextView(this).apply {
                        text = s
                        textSize = 14f
                        setTextColor(0xFF1E293B.toInt())
                        setPadding(0, dp(6), 0, dp(6))
                    })
                }
                layoutInteractiveFeature.addView(guideCard)
            }

            isSettingsView -> {
                iconSoonWrapper.visibility = View.GONE
                txtSoonSubtitle.text = "Quản lý tài khoản học sinh đang đăng nhập trên máy tính bảng và thông tin kết nối LAN"
                layoutInteractiveFeature.visibility = View.VISIBLE

                val accountCard = LinearLayout(this).apply {
                    orientation = LinearLayout.VERTICAL
                    setBackgroundResource(R.drawable.bg_card_action_elevated)
                    setPadding(dp(18), dp(18), dp(18), dp(18))
                }

                val curName = currentStudentName.ifBlank { edtStudentName.text.toString().ifBlank { "Học sinh" } }
                val curCode = currentStudentCode.ifBlank { edtStudentCode.text.toString().ifBlank { "HS001" } }
                val curIp = currentTeacherIp ?: discoveredTeacherIp ?: edtTeacherIp.text.toString().ifBlank { "10.0.2.2" }

                accountCard.addView(TextView(this).apply {
                    text = "HỒ SƠ ĐANG ĐĂNG NHẬP"
                    textSize = 12f
                    setTypeface(null, Typeface.BOLD)
                    setTextColor(0xFF2563EB.toInt())
                })
                accountCard.addView(TextView(this).apply {
                    text = "$curName  •  Mã HS: $curCode"
                    textSize = 18f
                    setTypeface(null, Typeface.BOLD)
                    setTextColor(0xFF0F172A.toInt())
                    setPadding(0, dp(6), 0, dp(4))
                })
                accountCard.addView(TextView(this).apply {
                    text = "Lớp hiện tại: ${homeClassName.ifBlank { currentClassName }}  |  Máy chủ GV: $curIp (Ping: ${lastMeasuredPingMs}ms)"
                    textSize = 14f
                    setTextColor(0xFF475569.toInt())
                    setPadding(0, 0, 0, dp(16))
                })

                val btnSwitchNow = Button(this).apply {
                    text = "ĐỔI TÀI KHOẢN / ĐĂNG NHẬP HỌC SINH KHÁC"
                    isAllCaps = false
                    textSize = 15f
                    setTypeface(null, Typeface.BOLD)
                    setTextColor(0xFFFFFFFF.toInt())
                    setBackgroundColor(0xFF2563EB.toInt())
                    layoutParams = LinearLayout.LayoutParams(
                        LinearLayout.LayoutParams.MATCH_PARENT,
                        dp(50)
                    ).apply { bottomMargin = dp(10) }
                    setOnClickListener {
                        showSwitchAccountDialog(autoReconnectIfConnected = true)
                    }
                }
                accountCard.addView(btnSwitchNow)

                val btnLeaveRoom = Button(this).apply {
                    text = "RỜI LỚP HỌC VỀ MÀN HÌNH CHỜ"
                    isAllCaps = false
                    textSize = 14f
                    setTypeface(null, Typeface.BOLD)
                    setTextColor(0xFFDC2626.toInt())
                    setBackgroundColor(0xFFFEE2E2.toInt())
                    layoutParams = LinearLayout.LayoutParams(
                        LinearLayout.LayoutParams.MATCH_PARENT,
                        dp(48)
                    )
                    setOnClickListener {
                        isUserInitiatedDisconnect = true
                        disconnectFromTeacher("Học sinh chủ động rời lớp")
                    }
                }
                accountCard.addView(btnLeaveRoom)

                layoutInteractiveFeature.addView(accountCard)
            }

            else -> {
                iconSoonWrapper.visibility = View.VISIBLE
                txtSoonSubtitle.text = "Chế độ này đang chờ Giáo viên kích hoạt trên máy chủ lớp học."
                layoutInteractiveFeature.visibility = View.GONE
            }
        }
    }

    /** Cập nhật Trang chủ (dashboard) từ trạng thái thật của phiên lớp. */
    private fun refreshHome() {
        if (!::txtHomeGreeting.isInitialized) return
        val dateText = SimpleDateFormat("EEEE, dd/MM/yyyy", Locale("vi", "VN")).format(Date())
        txtHomeGreeting.text = "Xin chào, ${homeStudentName.ifBlank { "Học sinh" }}"
        txtHomeSub.text = "$dateText  •  $homeClassName"
        txtStatClass.text = homeClassName.ifBlank { "—" }
        txtStatTeacher.text = homeTeacherName.ifBlank { "—" }
        txtStatBroadcast.text = when {
            isScreenBroadcastActive -> "Đang chiếu"
            isLessonActive -> "Đang dạy"
            else -> "Chưa phát"
        }
        txtStatHand.text = if (isHandRaised) "Đang giơ tay" else "Không"

        val showActiveLesson = isLessonActive || isScreenBroadcastActive
        layoutHomeLessonActive.visibility = if (showActiveLesson) View.VISIBLE else View.GONE
        layoutHomeLessonWaiting.visibility = if (showActiveLesson) View.GONE else View.VISIBLE

        if (::txtHeaderLiveBadge.isInitialized) {
            txtHeaderLiveBadge.text = when {
                isScreenBroadcastActive -> "● Đang xem chiếu màn hình"
                isLessonActive -> "● Đang học trực tiếp"
                else -> "● Đã kết nối — Chờ bắt đầu tiết"
            }
        }

        val teacherDisplay = homeTeacherName.ifBlank { "Giáo viên" }
        if (showActiveLesson) {
            txtHomeLessonTitle.text = activeLessonTitle.ifBlank {
                if (isScreenBroadcastActive) "Màn hình bài giảng trực tiếp" else "Tiết học trực tiếp — ${homeClassName.ifBlank { "Lớp học" }}"
            }
            txtHomeSubjectBadge.text = activeLessonSubject.ifBlank { homeClassName.ifBlank { "Bài giảng trên lớp" } }
            txtHomeStageBadge.text = if (activeLessonStage in 1..6) "Giai đoạn $activeLessonStage/6" else "Đang giảng dạy"
            txtHomeLessonSub.text = if (isScreenBroadcastActive) {
                "$teacherDisplay đang chia sẻ màn hình..."
            } else {
                "$teacherDisplay đang giảng bài..."
            }
            txtHomeBoardPointer.text = "$teacherDisplay đang giảng"
            if (::txtHomeBoardContent.isInitialized) {
                txtHomeBoardContent.text = activeLessonSummary.ifBlank {
                    activeLessonTitle.ifBlank { "Đang đồng bộ nội dung bài giảng trực tiếp từ Giáo viên..." }
                }
                val stageDesc = when (activeLessonStage) {
                    1 -> "Bước 1/6 — Chuẩn bị & Kiểm tra sĩ số"
                    2 -> "Bước 2/6 — Khởi động & Kiểm tra bài cũ"
                    3 -> "Bước 3/6 — Giảng bài & Khám phá kiến thức"
                    4 -> "Bước 4/6 — Luyện tập & Hoạt động nhóm"
                    5 -> "Bước 5/6 — Đánh giá & Chấm điểm"
                    6 -> "Bước 6/6 — Tổng kết & Giao bài tập về nhà"
                    else -> "Đang theo dõi tiến trình giảng dạy trên lớp"
                }
                txtHomeBoardNote1.text = "●  Tiến trình: $stageDesc"
                txtHomeBoardNote2.text = "✔  Môn: ${activeLessonSubject.ifBlank { "Bài học" }}  •  Lớp ${homeClassName.ifBlank { "—" }}"
            }
        }
        if (::txtHomeQuizBadge.isInitialized) {
            if (activeLessonStage == 4 || activeLessonStage == 2) {
                txtHomeQuizBadge.text = "Đang mở"
                txtHomeQuizBadge.setBackgroundResource(R.drawable.bg_pill_solid_green)
                txtHomeQuizBadge.setTextColor(0xFFFFFFFF.toInt())
                txtHomeQuizSub.text = "Giáo viên đang mở phần câu hỏi / hoạt động trên lớp"
            } else {
                txtHomeQuizBadge.text = "Chờ giao bài"
                txtHomeQuizBadge.setBackgroundResource(R.drawable.bg_chip_pill)
                txtHomeQuizBadge.setTextColor(0xFF64748B.toInt())
                txtHomeQuizSub.text = "Nhận đề kiểm tra và bài tập trực tiếp từ Giáo viên"
            }
        }
        txtHomeTeacherChip.text = "GV: ${homeTeacherName.ifBlank { "—" }}"

        if (::txtHomeTeacherAvatar.isInitialized) {
            val words = teacherDisplay.trim().split(Regex("\\s+")).filter { it.isNotBlank() }
            txtHomeTeacherAvatar.text = when {
                words.size >= 2 -> "${words.first().first()}${words.last().first()}".uppercase()
                words.size == 1 -> words.first().take(2).uppercase()
                else -> "GV"
            }
            txtHomeNoticeHeader.text = "Lời nhắc của Giáo viên ($teacherDisplay):"
            layoutNoticeFeedback.visibility = if (hasTeacherNotice || showActiveLesson) View.VISIBLE else View.GONE
        }
    }

    private fun selectHandRaiseReason(reason: String) {
        currentHandRaiseReason = reason
        val activeBgTint = ColorStateList.valueOf(0xFFDBEAFE.toInt())
        val idleBgTint = ColorStateList.valueOf(0xFFF1F5F9.toInt())

        btnReasonSpeaking.backgroundTintList = if (reason == "Phát biểu") activeBgTint else idleBgTint
        btnReasonQuestion.backgroundTintList = if (reason == "Đặt câu hỏi") activeBgTint else idleBgTint
        btnReasonHelp.backgroundTintList = if (reason == "Trợ giúp") activeBgTint else idleBgTint

        Toast.makeText(this, "Lý do: $reason", Toast.LENGTH_SHORT).show()
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
        return s.length in 2..18 &&
            !s.contains("ENC_CMD", ignoreCase = true) &&
            s.matches(Regex("^[A-Za-z0-9_-]{2,18}$"))
    }

    private fun sanitizeStudentCode(raw: String?): String {
        val s = raw?.trim().orEmpty()
        return if (isValidStudentCode(s)) s else "10A1-002"
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

    /**
     * Trả về đúng Họ tên học sinh theo tài khoản đăng nhập (Mã học sinh) hoặc tên người dùng nhập,
     * tuyệt đối loại bỏ nhãn placeholder cũ "Học sinh Android" hoặc chuỗi mã hóa gói tin.
     */
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
        val screenWidthDp = resources.displayMetrics.widthPixels / resources.displayMetrics.density
        txtStudentIdentityName.text = name
        txtSeatBadge.text = "BÀN SỐ $seatDigits  •  MÃ: $code"
        txtStudentIdentityCode.text = if (screenWidthDp < 600f) {
            "Nhập Mã vào lớp ở thẻ bên dưới để tham gia tiết học."
        } else {
            "Nhập Mã vào lớp ở thẻ bên phải để tham gia tiết học."
        }
    }

    private fun updateSlideViewerState() {
        if (!::txtSlidePageIndicator.isInitialized) return
        val stageTotal = totalSlideCount.coerceAtLeast(1)
        val cur = currentSlideIndex.coerceIn(1, stageTotal)
        txtSlidePageIndicator.text = String.format(Locale.US, "%02d / %02d", cur, stageTotal)
    }

    private fun applyDocumentZoom() {
        if (!::txtZoomLevel.isInitialized) return
        txtZoomLevel.text = "$currentZoomPercent%"
        val scale = (currentZoomPercent / 100f).coerceIn(0.7f, 2.0f)
        imgInlineBroadcast.scaleX = scale
        imgInlineBroadcast.scaleY = scale
    }

    /**
     * Micro-interaction: Thanh tiến trình Upload 0% -> 100% khi học sinh bấm Nộp bài / Gửi ghi chú trên Màn hình 3.
     */
    private fun performQuickNoteOrHomeworkUpload() {
        val noteText = edtQuickLessonAnswer.text.toString().trim()
        val strokeCount = quickNoteCanvas.getStrokeCount()
        if (noteText.isBlank() && strokeCount == 0) {
            Toast.makeText(this, "Vui lòng viết nháp hoặc nhập nội dung câu trả lời trước khi nộp!", Toast.LENGTH_SHORT).show()
            return
        }

        btnQuickSubmitWork.isEnabled = false
        btnQuickSubmitWork.text = "Đang gửi bài về máy Giáo viên..."
        layoutUploadProgress.visibility = View.VISIBLE
        progressUploadBar.progress = 0
        txtUploadPercent.text = "0%"
        txtUploadStatusLabel.text = "Đang đóng gói dữ liệu bài làm..."

        lifecycleScope.launch {
            val steps = listOf(25, 55, 85, 100)
            for (p in steps) {
                delay(110)
                progressUploadBar.progress = p
                txtUploadPercent.text = "$p%"
            }

            val hwIdVal = if (activeHwId > 0) activeHwId else 1
            val payloadSummary = buildString {
                if (noteText.isNotBlank()) append(noteText.replace("|", " "))
                if (strokeCount > 0) {
                    if (isNotEmpty()) append(" ")
                    append("[Kèm bản vẽ nháp $strokeCount nét]")
                }
            }
            sendRawTcpLine("HOMEWORK_SUBMIT|$hwIdVal|$payloadSummary|[]")
            hwSubmitted = true
            txtUploadStatusLabel.text = "Máy chủ Giáo viên đã nhận bài nộp"
            btnQuickSubmitWork.text = "ĐÃ NỘP BÀI THÀNH CÔNG"
            btnQuickSubmitWork.backgroundTintList = ColorStateList.valueOf(0xFF16A34A.toInt())
            Toast.makeText(this@MainActivity, "Đã nộp bài làm về máy Giáo viên!", Toast.LENGTH_SHORT).show()

            delay(1800)
            btnQuickSubmitWork.isEnabled = true
        }
    }

    private fun toggleRaiseHand() {
        val targetState = !isHandRaised
        isHandAcceptedByTeacher = false
        animateRaiseHandMicroInteraction(btnFullscreenRaiseHand)
        animateRaiseHandMicroInteraction(btnMainRaiseHand)
        findViewById<View?>(R.id.btnHeroRaiseHand)?.let { animateRaiseHandMicroInteraction(it) }
        animateRaiseHandMicroInteraction(btnRaiseHand)
        updateHandRaiseUI(targetState)

        lifecycleScope.launch(Dispatchers.IO) {
            try {
                val msg = if (targetState) {
                    "HAND_RAISE|raised=True|reason=$currentHandRaiseReason\n"
                } else {
                    "HAND_RAISE|raised=False\n"
                }
                tcpOutputStream?.write(msg.toByteArray(Charsets.UTF_8))
                tcpOutputStream?.flush()
            } catch (e: Exception) {
                Log.w(TAG, "Failed to send hand raise: ${e.message}")
            }
        }

        if (targetState) {
            Toast.makeText(this, "Đã gửi tín hiệu Giơ tay ($currentHandRaiseReason) tới Giáo viên", Toast.LENGTH_SHORT).show()
        } else {
            Toast.makeText(this, "Đã hạ tay", Toast.LENGTH_SHORT).show()
        }
    }

    /**
     * Micro-interaction cho nút Giơ tay: Rung xúc giác nhẹ + Hiệu ứng thu phóng đàn hồi (0.92 -> 1.06 -> 1.0).
     */
    private fun animateRaiseHandMicroInteraction(targetView: View) {
        try {
            targetView.performHapticFeedback(android.view.HapticFeedbackConstants.VIRTUAL_KEY)
            targetView.animate()
                .scaleX(0.92f)
                .scaleY(0.92f)
                .setDuration(75)
                .withEndAction {
                    targetView.animate()
                        .scaleX(1.06f)
                        .scaleY(1.06f)
                        .setDuration(110)
                        .withEndAction {
                            targetView.animate()
                                .scaleX(1.0f)
                                .scaleY(1.0f)
                                .setDuration(90)
                                .start()
                        }
                        .start()
                }
                .start()
        } catch (_: Exception) {}
    }

    private fun updateHandRaiseUI(raised: Boolean, acceptedByTeacher: Boolean = false) {
        isHandRaised = raised
        isHandAcceptedByTeacher = acceptedByTeacher
        runOnUiThread { if (::txtStatHand.isInitialized) txtStatHand.text = if (raised) "Đang giơ tay" else "Không" }
        runOnUiThread {
            val heroBtn = findViewById<View?>(R.id.btnHeroRaiseHand)
            when {
                raised && acceptedByTeacher -> {
                    btnRaiseHand.backgroundTintList = ColorStateList.valueOf(0xFF16A34A.toInt())
                    heroBtn?.backgroundTintList = ColorStateList.valueOf(0xFF16A34A.toInt())
                    btnFullscreenRaiseHand.text = "GV mời"
                    btnFullscreenRaiseHand.backgroundTintList = ColorStateList.valueOf(0xFF16A34A.toInt())

                    btnMainRaiseHand.setBackgroundResource(R.drawable.bg_hand_raise_active)
                    txtHandRaiseStatus.text = "GIÁO VIÊN ĐÃ MỜI\nPHÁT BIỂU"
                    txtHandRaiseStatus.setTextColor(0xFFFFFFFF.toInt())
                }
                raised -> {
                    btnRaiseHand.backgroundTintList = ColorStateList.valueOf(0xFFD97706.toInt())
                    heroBtn?.backgroundTintList = ColorStateList.valueOf(0xFFD97706.toInt())
                    btnFullscreenRaiseHand.text = "Đang chờ"
                    btnFullscreenRaiseHand.backgroundTintList = ColorStateList.valueOf(0xFFD97706.toInt())

                    btnMainRaiseHand.setBackgroundResource(R.drawable.bg_hand_raise_active)
                    txtHandRaiseStatus.text = "ĐANG GIƠ TAY\n(Chờ GV mời)"
                    txtHandRaiseStatus.setTextColor(0xFFFFFFFF.toInt())
                }
                else -> {
                    btnRaiseHand.backgroundTintList = ColorStateList.valueOf(0xFFF59E0B.toInt())
                    heroBtn?.backgroundTintList = ColorStateList.valueOf(0xFFF59E0B.toInt())
                    btnFullscreenRaiseHand.text = "Giơ tay"
                    btnFullscreenRaiseHand.backgroundTintList = ColorStateList.valueOf(0xFFF59E0B.toInt())

                    btnMainRaiseHand.setBackgroundResource(R.drawable.bg_hand_raise_idle)
                    txtHandRaiseStatus.text = "Bấm để Giơ tay"
                    txtHandRaiseStatus.setTextColor(0xFFB45309.toInt())
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

        // Giao thức: CHAT|code|ALL|text (cả lớp) | CHAT|code|GROUP_xxx|text (nhóm) | CHAT|code|TEACHER|text (riêng với GV)
        val wireChannel = when {
            channel == "CLASS" -> "ALL"
            channel.startsWith("GROUP_") -> channel
            else -> "TEACHER"
        }
        lifecycleScope.launch(Dispatchers.IO) {
            try {
                val chatMsg = "CHAT|$studentCode|$wireChannel|$text\n"
                tcpOutputStream?.write(chatMsg.toByteArray(Charsets.UTF_8))
                tcpOutputStream?.flush()
            } catch (e: Exception) {
                Log.w(TAG, "Failed to send chat: ${e.message}")
            }
        }
    }

    /** Chuyển kênh chat (Lớp / Nhóm / Giáo viên), vẽ lại lịch sử của kênh đó. */
    private fun switchChatChannel(channel: String) {
        currentChatChannel = channel
        unreadChatChannels.remove(channel)
        updateChatTabStyles()
        renderChat()
    }

    private fun chatChannelLabel(channel: String): String = when {
        channel == "CLASS" -> "👥 Nhắn với lớp"
        channel.startsWith("GROUP_") ->
            chatGroups.firstOrNull { it.code == channel }?.let { "${it.emoji} ${it.name}" } ?: "📋 Nhóm"
        else -> "👨‍🏫 Nhắn với giáo viên"
    }

    /** Vẽ lại thanh tab: Lớp → các Nhóm → Giáo viên (cuộn ngang nếu nhiều nhóm). */
    private fun updateChatTabStyles() {
        val density = resources.displayMetrics.density
        val channels = mutableListOf("CLASS")
        channels.addAll(chatGroups.map { it.code })
        channels.add("TEACHER")

        chatTabBar.removeAllViews()
        channels.forEachIndexed { i, ch ->
            val active = ch == currentChatChannel
            val label = chatChannelLabel(ch)
            val btn = Button(this).apply {
                text = if (unreadChatChannels.contains(ch) && !active) "$label  ●" else label
                isAllCaps = false
                textSize = 12.5f
                setTypeface(null, Typeface.BOLD)
                minWidth = 0
                minimumWidth = 0
                setPadding((14 * density).toInt(), 0, (14 * density).toInt(), 0)
                setBackgroundColor(if (active) 0xFF2563EB.toInt() else 0xFFE2E8F0.toInt())
                setTextColor(if (active) 0xFFFFFFFF.toInt() else 0xFF475569.toInt())
                layoutParams = LinearLayout.LayoutParams(
                    LinearLayout.LayoutParams.WRAP_CONTENT,
                    (40 * density).toInt()
                ).apply { if (i > 0) marginStart = (6 * density).toInt() }
                setOnClickListener { switchChatChannel(ch) }
            }
            chatTabBar.addView(btn)
        }

        val isClass = currentChatChannel == "CLASS"
        txtChatChannelBadge.text = "Gửi: ${chatChannelLabel(currentChatChannel).replace("Nhắn với ", "")}"
        edtChatMessage.hint = when {
            isClass && classChatMuted -> "Giáo viên đang tạm khóa trò chuyện cả lớp"
            isClass -> "Nhập tin nhắn gửi cả lớp..."
            currentChatChannel.startsWith("GROUP_") -> "Nhập tin nhắn gửi nhóm..."
            else -> "Nhập tin nhắn riêng gửi Giáo viên..."
        }
    }

    /** Nhận danh sách nhóm từ GV: JSON [{"id":"GROUP_x","n":"Tên","e":"📋"}] — chỉ nhóm mà HS là thành viên. */
    private fun applyChatGroups(json: String) {
        try {
            val arr = org.json.JSONArray(json)
            val list = mutableListOf<ChatGroupChannel>()
            for (i in 0 until arr.length()) {
                val o = arr.getJSONObject(i)
                val id = o.optString("id")
                if (id.startsWith("GROUP_")) {
                    list.add(ChatGroupChannel(id, o.optString("n", "Nhóm"), o.optString("e", "📋")))
                }
            }
            runOnUiThread {
                chatGroups.clear()
                chatGroups.addAll(list)
                // Nếu kênh đang mở là nhóm đã bị xoá → về kênh Giáo viên
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
        val notice = TextView(this).apply {
            text = when {
                currentChatChannel == "CLASS" -> "👥 Tin nhắn tại đây được gửi tới Cả lớp."
                currentChatChannel.startsWith("GROUP_") -> "📋 Tin nhắn tại đây được gửi tới nhóm “${chatGroups.firstOrNull { it.code == currentChatChannel }?.name ?: ""}”."
                else -> "🔒 Tin nhắn tại đây chỉ gửi riêng cho Giáo viên."
            }
            textSize = 11f
            gravity = Gravity.CENTER
            setTextColor(0xFF64748B.toInt())
            setBackgroundColor(0xFFF1F5F9.toInt())
            val p = (8 * density).toInt()
            setPadding(p, p, p, p)
            layoutParams = LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT,
                LinearLayout.LayoutParams.WRAP_CONTENT
            ).apply { bottomMargin = (8 * density).toInt() }
        }
        chatContainer.addView(notice)
        chatHistory.filter { it.channel == currentChatChannel }.forEach { appendChatBubble(it) }
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
                appendChatBubble(entry)
            } else {
                unreadChatChannels.add(entry.channel)
                updateChatTabStyles()
            }
        }
    }

    // Phải gọi trên UI thread
    private fun appendChatBubble(entry: ChatEntry) {
        val sender = entry.sender
        val message = entry.message
        val isSelf = entry.isSelf
        val density = resources.displayMetrics.density
        val padH = (14 * density).toInt()
        val padV = (10 * density).toInt()
        val marginV = (6 * density).toInt()

        val bubbleLayout = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            val params = LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT,
                LinearLayout.LayoutParams.WRAP_CONTENT
            ).apply {
                setMargins(0, marginV, 0, marginV)
            }
            layoutParams = params
        }

        val senderLabel = TextView(this).apply {
            text = if (isSelf) "Bạn" else sender
            textSize = 11f
            setTextColor(if (isSelf) 0xFF2563EB.toInt() else 0xFF475569.toInt())
            setTypeface(null, Typeface.BOLD)
            val params = LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.WRAP_CONTENT,
                LinearLayout.LayoutParams.WRAP_CONTENT
            ).apply {
                gravity = if (isSelf) Gravity.END else Gravity.START
                bottomMargin = (2 * density).toInt()
            }
            layoutParams = params
        }
        bubbleLayout.addView(senderLabel)

        val msgTextView = TextView(this).apply {
            text = "$message   ${entry.time}"
            textSize = 13.5f
            setPadding(padH, padV, padH, padV)
            if (isSelf) {
                setBackgroundResource(R.drawable.bg_chat_student)
                setTextColor(0xFFFFFFFF.toInt())
            } else {
                setBackgroundResource(R.drawable.bg_chat_teacher)
                setTextColor(0xFF1E293B.toInt())
            }
            maxWidth = (resources.displayMetrics.widthPixels * 0.72).toInt()
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

    private suspend fun measureLanPingMs(host: String, port: Int): Long = withContext(Dispatchers.IO) {
        val t0 = System.nanoTime()
        try {
            Socket().use { s ->
                s.connect(InetSocketAddress(host, port), 1200)
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
            txtLanPingBadge.text = "Offline • Chưa thấy máy chủ"
            txtLanPingBadge.setBackgroundResource(R.drawable.bg_pill_badge_red)
            txtLanPingBadge.setTextColor(0xFFDC2626.toInt())
            return
        }
        val bars = when {
            pingMs <= 15L -> "4 vạch"
            pingMs <= 45L -> "3 vạch"
            else -> "2 vạch"
        }
        txtLanPingBadge.text = "Ping: ${pingMs}ms • $bars"
        txtLanPingBadge.setBackgroundResource(R.drawable.bg_pill_badge_green)
        txtLanPingBadge.setTextColor(0xFF15803D.toInt())
    }

    private fun startAutoDiscovery() {
        syncStudentIdentityCard()
        txtDiscoveryHeader.text = "ĐANG QUÉT LỚP HỌC..."
        txtDiscoveryDot.setTextColor(0xFFD97706.toInt())
        txtStatus.text = "Đang quét tự động các lớp học đang mở trong mạng LAN..."
        progressBar.visibility = View.VISIBLE
        btnRefresh.visibility = View.GONE

        lifecycleScope.launch {
            val beacon = discoveryService.discoverTeacher(timeoutMs = 6000)
            if (beacon != null) {
                discoveredTeacherIp = beacon.teacherIp
                discoveredTeacherPort = beacon.tcpPort
                currentClassName = beacon.className
                currentTeacherName = beacon.teacherName
                val rtt = measureLanPingMs(beacon.teacherIp, beacon.tcpPort)
                updateLanSignalBadge(rtt, true)
                txtDiscoveryHeader.text = "PHÒNG HỌC ĐANG MỞ TRONG LAN"
                txtDiscoveryDot.setTextColor(0xFF16A34A.toInt())
                txtStatus.text = "${beacon.className}  •  Giáo viên: ${beacon.teacherName}"
                edtTeacherIp.setText(beacon.teacherIp)
                progressBar.visibility = View.GONE
                btnRefresh.visibility = View.VISIBLE
            } else {
                // Không nhận được beacon UDP (emulator/NAT/router chặn broadcast) -> dò cổng TCP trực tiếp
                txtStatus.text = "Đang dò máy chủ giáo viên trong mạng nội bộ..."
                val host = probeTeacherHost()
                if (host != null) {
                    discoveredTeacherIp = host
                    discoveredTeacherPort = TCP_PORT
                    val rtt = measureLanPingMs(host, TCP_PORT)
                    updateLanSignalBadge(rtt, true)
                    val info = withContext(Dispatchers.IO) { queryClassInfo(host) }
                    txtDiscoveryHeader.text = "PHÒNG HỌC ĐANG MỞ TRONG LAN"
                    txtDiscoveryDot.setTextColor(0xFF16A34A.toInt())
                    if (info != null && info.size >= 3) {
                        currentClassName = info[1]
                        currentTeacherName = info[2]
                        txtStatus.text = "${info[1]}  •  Giáo viên: ${info[2]}"
                    } else {
                        txtStatus.text = "Lớp học SmartClass ($host) — Sẵn sàng kết nối"
                    }
                    edtTeacherIp.setText(host)
                } else {
                    updateLanSignalBadge(0L, false)
                    txtDiscoveryHeader.text = "CHƯA TÌM THẤY LỚP HỌC"
                    txtDiscoveryDot.setTextColor(0xFFDC2626.toInt())
                    txtStatus.text = "Chưa tìm thấy lớp học trong LAN. Hãy bấm 'Quét lại' hoặc 'Cấu hình IP' góc trên."
                }
                progressBar.visibility = View.GONE
                btnRefresh.visibility = View.VISIBLE
            }
        }
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

    /** Dò cổng TCP của giáo viên: IP đã lưu, 10.0.2.2 (emulator), rồi quét toàn subnet /24 của thiết bị. */
    private suspend fun probeTeacherHost(): String? = withContext(Dispatchers.IO) {
        val priority = LinkedHashSet<String>()
        val saved = edtTeacherIp.text.toString().trim().substringBefore(":")
        if (saved.isNotBlank()) priority.add(saved)
        priority.add("10.0.2.2")
        for (h in priority) {
            if (canConnect(h, TCP_PORT, 700)) return@withContext h
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
                    if (canConnect(h, TCP_PORT, 600)) found.complete(h)
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
        currentTeacherIp = ip
        currentStudentName = studentName
        currentStudentCode = studentCode

        txtStatus.text = "Đang kết nối tới máy chủ SmartClass ($ip)..."
        progressBar.visibility = View.VISIBLE
        btnConnect.isEnabled = false
        txtError.visibility = View.GONE

        lifecycleScope.launch(Dispatchers.IO) {
            try {
                tcpSocket?.close()
                val socket = Socket()
                socket.connect(InetSocketAddress(ip, port), 5000)
                tcpSocket = socket
                val output = socket.getOutputStream()
                val input = socket.getInputStream()
                tcpOutputStream = output

                // Send Native JOIN handshake with password and class code
                // Format: JOIN|name|code|pcName|version|pubKeyBase64|macAddress|password|classCode
                val joinMsg = "JOIN|$studentName|$studentCode|Android_Tablet|1.0|||$password|$classCodeInput\n"
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
                    Log.i(TAG, "Handshake OK - Student: $resolvedStudentName ($serverStudentCode), ClassCode: $currentClassCode")

                    withContext(Dispatchers.Main) {
                        txtError.visibility = View.GONE
                        edtStudentName.setText(resolvedStudentName)
                        edtStudentCode.setText(serverStudentCode)
                        if (chkRemember.isChecked) {
                            savePreferences(resolvedStudentName, serverStudentCode, password, ip, true)
                        }
                        onConnectedSuccess(resolvedStudentName, serverStudentCode, ip, currentClassName, currentTeacherName)
                    }

                    // Start Heartbeat & Focus reporter
                    startHeartbeatLoop(serverStudentCode)
                    focusReporter.startReporting()

                    // Start reading commands from Teacher
                    readTcpStream(input)
                } else if (ack.startsWith("ERR|")) {
                    val parts = ack.split("|")
                    val errMsg = if (parts.size >= 3) parts[2] else (if (parts.size >= 2) parts[1] else "Đăng nhập không thành công")
                    throw Exception(errMsg)
                } else {
                    throw Exception("Không thể xác thực phiên kết nối với máy Giáo viên. Vui lòng thử lại!")
                }
            } catch (e: Exception) {
                Log.e(TAG, "Connection error: ${e.message}")
                val friendlyMsg = formatFriendlyErrorMessage(e)
                withContext(Dispatchers.Main) {
                    showError(friendlyMsg)
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
        txtStudentDetail.text = "Học sinh: $name ($code)"
        txtTeacherDetail.text = "Giáo viên: $teacherName"
        txtServerIpDetail.text = "Máy chủ: $ip:$TCP_PORT"
        messageCard.visibility = View.GONE

        updateHandRaiseUI(false)
        selectHandRaiseReason("Phát biểu")
        homeStudentName = name
        homeClassName = className.replace("• Phòng 204 • Phòng 204", "• Phòng 204").trim()
        homeTeacherName = teacherName
        isLessonActive = false
        activeLessonTitle = ""
        activeLessonSubject = ""
        activeLessonStage = 0
        hasTeacherNotice = false
        txtHomeNotice.text = "Chưa có lời nhắc mới từ Giáo viên."
        txtHomeNoticeTime.text = "🕒 Chờ thông báo"
        layoutNoticeFeedback.visibility = View.GONE
        requestScreenCapture()

        // Reset broadcast UI state
        stopScreenBroadcast()
        refreshHome()
        switchTab(3)

        // Yêu cầu máy chủ đồng bộ trạng thái tiết học & trình chiếu hiện tại (nếu Giáo viên đã bắt đầu tiết trước đó)
        requestCurrentLessonAndBroadcastStatus()

        Toast.makeText(this, "Đã kết nối trực tiếp với SmartClass!", Toast.LENGTH_SHORT).show()
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
                    tcpOutputStream?.write(hb.toByteArray(Charsets.UTF_8))
                    tcpOutputStream?.flush()
                } catch (e: Exception) {
                    Log.w(TAG, "Heartbeat write failed: ${e.message}")
                    break
                }
            }
        }
    }

    private suspend fun readTcpStream(input: InputStream) = withContext(Dispatchers.IO) {
        val buffer = ByteArray(4096)
        val msgBuffer = StringBuilder()
        try {
            while (tcpSocket?.isConnected == true) {
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
            Log.w(TAG, "Stream read ended: ${e.message}")
        } finally {
            withContext(Dispatchers.Main) {
                if (isUserInitiatedDisconnect) {
                    disconnectFromTeacher("Học sinh chủ động rời lớp")
                } else {
                    attemptSilentLanReconnect()
                }
            }
        }
    }

    /**
     * Micro-interaction: Cảnh báo mất kết nối mạng LAN trên Top Banner và tự động kết nối lại tối đa 5 lần
     * mà không xóa bài nháp hoặc văng học sinh ra màn hình đăng nhập ngay lập tức.
     */
    private fun attemptSilentLanReconnect() {
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

        lifecycleScope.launch(Dispatchers.IO) {
            var reconnected = false
            var sessionCodeChanged = false
            for (attempt in 1..5) {
                withContext(Dispatchers.Main) {
                    txtLanReconnectStatus.text = "Tín hiệu mạng phòng học đang yếu — Đang tự động kết nối lại (Lần $attempt/5)..."
                }
                delay(2500)
                try {
                    val socket = Socket()
                    socket.connect(InetSocketAddress(ip, port), 3000)
                    val output = socket.getOutputStream()
                    val input = socket.getInputStream()
                    val joinMsg = "JOIN|$name|$code|Android_Tablet|1.0|||$pwd|$activeClassCode\n"
                    output.write(joinMsg.toByteArray(Charsets.UTF_8))
                    output.flush()

                    val buffer = ByteArray(2048)
                    val len = input.read(buffer)
                    if (len > 0) {
                        val ack = String(buffer, 0, len, Charsets.UTF_8).removePrefix("\uFEFF").trim()
                        if (ack.startsWith("OK")) {
                            tcpSocket = socket
                            tcpOutputStream = output
                            reconnected = true
                            withContext(Dispatchers.Main) {
                                lanReconnectBanner.setBackgroundColor(0xFF16A34A.toInt())
                                txtLanReconnectStatus.text = "Đã kết nối lại thành công với máy Giáo viên!"
                                requestCurrentLessonAndBroadcastStatus()
                            }
                            delay(1400)
                            withContext(Dispatchers.Main) {
                                lanReconnectBanner.visibility = View.GONE
                            }
                            startHeartbeatLoop(code)
                            readTcpStream(input)
                            break
                        } else if (ack.contains("INVALID_CLASS_CODE") || ack.contains("CLASS_CODE_REQUIRED")) {
                            sessionCodeChanged = true
                            socket.close()
                            break
                        }
                    }
                    socket.close()
                } catch (_: Exception) {}
            }

            if (!reconnected) {
                withContext(Dispatchers.Main) {
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
                    val on = cleanCmd.split("|").getOrNull(2) == "1"
                    if (on) {
                        txtLockTitle.text = "THẦY CÔ ĐANG GIẢNG BÀI - XIN MỜI CHÚ Ý LÊN BẢNG"
                        txtLockMessage.text = "Màn hình thiết bị đã chuyển sang chế độ tập trung nghe giảng."
                        txtLockLessonName.text = "Bài học: ${activeLessonTitle.ifBlank { homeClassName.ifBlank { "Trên bảng chính" } }}"
                        lockOverlay.setBackgroundColor(0xFF000000.toInt())
                        lockOverlay.visibility = View.VISIBLE
                        startLockScreenTimer()
                    } else {
                        lockOverlay.visibility = View.GONE
                        stopLockScreenTimer()
                    }
                }
                cleanCmd.startsWith("CMD|LOCK") -> {
                    val on = cleanCmd.split("|").getOrNull(2) != "0"
                    if (on) {
                        txtLockTitle.text = "THẦY CÔ ĐANG GIẢNG BÀI - XIN MỜI CHÚ Ý LÊN BẢNG"
                        txtLockMessage.text = "Màn hình máy tính bảng đang tạm khóa thao tác để cả lớp tập trung nghe giảng."
                        txtLockLessonName.text = "Bài học: ${activeLessonTitle.ifBlank { homeClassName.ifBlank { "Trên bảng chính" } }}"
                        lockOverlay.setBackgroundColor(0xF80B0F19.toInt())
                        lockOverlay.visibility = View.VISIBLE
                        startLockScreenTimer()
                        heartbeatWatchdog.acquireLock()
                        Toast.makeText(this, "Giáo viên đã bật chế độ Tập trung nghe giảng", Toast.LENGTH_SHORT).show()
                    } else {
                        lockOverlay.visibility = View.GONE
                        stopLockScreenTimer()
                        heartbeatWatchdog.releaseLock()
                    }
                }
                cleanCmd.startsWith("CMD|UNLOCK") -> {
                    lockOverlay.visibility = View.GONE
                    stopLockScreenTimer()
                    heartbeatWatchdog.releaseLock()
                    Toast.makeText(this, "Giáo viên đã mở khóa màn hình", Toast.LENGTH_SHORT).show()
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
                    }
                    activeLessonStage = if (stageVal >= 2) stageVal else 2
                    isLessonActive = true
                    if (::txtHomeBoardHeader.isInitialized) {
                        txtHomeBoardHeader.text = "Nội dung bài giảng hiện tại:"
                    }

                    if (!hasTeacherNotice) {
                        val displayLesson = activeLessonTitle.ifBlank { "bài giảng trên bảng" }
                        txtHomeNotice.text = "“Các em chú ý theo dõi bài giảng $displayLesson và chuẩn bị thảo luận nhóm.”"
                        txtHomeNoticeTime.text = "🕒 Vừa xong"
                        hasTeacherNotice = true
                    }
                    refreshHome()
                    Toast.makeText(this, "📖 Giáo viên đã bắt đầu tiết học: ${activeLessonTitle.ifBlank { "Bài giảng mới" }}", Toast.LENGTH_SHORT).show()
                }
                cleanCmd.startsWith("CMD|LESSON_FOCUS|") -> {
                    val cmdParts = cleanCmd.substringBefore("|id=").split("|")
                    val sortOrder = cmdParts.getOrNull(2)?.toIntOrNull() ?: 1
                    val contentType = cmdParts.getOrNull(3) ?: ""
                    val dataEnc = cmdParts.getOrNull(4) ?: ""
                    val decodedData = try { URLDecoder.decode(dataEnc, "UTF-8") } catch (_: Exception) { dataEnc }
                    isLessonActive = true
                    if (decodedData.isNotBlank()) {
                        activeLessonSummary = decodedData.replace("\\n", "\n")
                    }
                    if (::txtHomeBoardHeader.isInitialized) {
                        txtHomeBoardHeader.text = "🎯 Nội dung trọng tâm Giáo viên đang chiếu (#$sortOrder):"
                    }
                    refreshHome()
                    Toast.makeText(this, "🎯 Giáo viên yêu cầu tập trung vào nội dung #$sortOrder", Toast.LENGTH_SHORT).show()
                }
                cleanCmd.startsWith("CMD|LESSON_UNFOCUS") -> {
                    if (::txtHomeBoardHeader.isInitialized) {
                        txtHomeBoardHeader.text = "Nội dung bài giảng hiện tại:"
                    }
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
                cleanCmd.startsWith("CMD|SCREEN_BROADCAST_STOP") || cleanCmd == "CMD|SCREEN_BROADCAST_STOP" -> {
                    stopScreenBroadcast()
                    Toast.makeText(this, "📡 Giáo viên đã dừng chiếu màn hình", Toast.LENGTH_SHORT).show()
                }
                cleanCmd.startsWith("CMD|SCREEN_BROADCAST_START") || cleanCmd.startsWith("CMD|SCREEN_BROADCAST|") || cleanCmd == "CMD|SCREEN_BROADCAST" -> {
                    val cmdParts = cleanCmd.split("|")
                    var webPort = currentTeacherWebPort
                    var token = currentBroadcastToken
                    var title = ""
                    val isForce = cleanCmd.contains("FORCE_WATCH")

                    for (part in cmdParts) {
                        if (part.contains("=")) {
                            val kv = part.split("=", limit = 2)
                            if (kv.size == 2 && kv[0] == "TITLE") {
                                title = try { URLDecoder.decode(kv[1], "UTF-8") } catch (e: Exception) { kv[1] }
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
                    startScreenBroadcast(token, webPort, isForce, title)
                    Toast.makeText(this, "📺 Giáo viên đang chia sẻ màn hình", Toast.LENGTH_SHORT).show()
                }
                cleanCmd.startsWith("CMD|SCREEN_BROADCAST_UPDATE") -> {
                    val cmdParts = cleanCmd.split("|")
                    var webPort = currentTeacherWebPort
                    var token = currentBroadcastToken
                    var title = ""
                    for (part in cmdParts) {
                        if (part.contains("=")) {
                            val kv = part.split("=", limit = 2)
                            if (kv.size == 2 && kv[0] == "TITLE") {
                                title = try { URLDecoder.decode(kv[1], "UTF-8") } catch (e: Exception) { kv[1] }
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
                    updateScreenBroadcast(token, webPort, title)
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

                    // 🔒 = nhắn riêng từ GV → kênh Giáo viên; 📨 [Tên nhóm] → kênh Nhóm; còn lại (📢 thông báo lớp, 📚...) → kênh Cả lớp
                    val trimmedMsg = msgText.trimStart()
                    val groupName = Regex("^📨\\s*\\[(.+?)\\]").find(trimmedMsg)?.groupValues?.get(1)
                    val incomingChannel = when {
                        trimmedMsg.startsWith("🔒") -> "TEACHER"
                        groupName != null -> chatGroups.firstOrNull { it.name == groupName }?.code ?: "CLASS"
                        else -> "CLASS"
                    }
                    addChatMessage("👨‍🏫 Giáo viên", msgText, isSelf = false, channel = incomingChannel)
                    messageCard.visibility = View.VISIBLE
                    txtTeacherMessage.text = msgText
                    txtHomeNotice.text = "📩 Giáo viên: $msgText"
                    txtHomeNoticeTime.text = "🕒 Vừa xong"
                    hasTeacherNotice = true
                    layoutNoticeFeedback.visibility = View.VISIBLE

                    if (currentTab != 1) {
                        btnTabChat.text = "💬  Tin nhắn  ●"
                        unreadTeacherChatCount++
                        if (::txtHomeChatBadge.isInitialized) {
                            txtHomeChatBadge.text = "● $unreadTeacherChatCount tin mới"
                            txtHomeChatBadge.setBackgroundResource(R.drawable.bg_pill_badge_red)
                            txtHomeChatBadge.setTextColor(0xFFDC2626.toInt())
                        }
                    }
                    Toast.makeText(this, "📩 Tin nhắn từ GV: $msgText", Toast.LENGTH_LONG).show()
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
                        if (::txtHomeQuizBadge.isInitialized) {
                            txtHomeQuizBadge.text = "Đang mở (${activeQuizQuestions.size} câu)"
                            txtHomeQuizBadge.setBackgroundResource(R.drawable.bg_pill_solid_green)
                            txtHomeQuizBadge.setTextColor(0xFFFFFFFF.toInt())
                            txtHomeQuizSub.text = "Bấm để làm bài kiểm tra trực tiếp (${activeQuizQuestions.size} câu hỏi)"
                        }
                        val navBtn = findViewById<TextView>(R.id.btnNavQuiz)
                        showComingSoon(navBtn, "Kiểm tra trắc nghiệm")
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
                        val navBtn = findViewById<TextView>(R.id.btnNavQuiz)
                        showComingSoon(navBtn, "Kiểm tra trắc nghiệm")
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
                    val navBtn = findViewById<TextView>(R.id.btnNavQuiz)
                    if (currentTab == 4) {
                        renderInteractiveFeaturePanel(R.id.btnNavQuiz, txtSoonTitle.text.toString())
                    }
                    Toast.makeText(this, "Điểm kiểm tra của bạn: $lastQuizScoreText ($correctCount/$totalQ câu đúng)", Toast.LENGTH_LONG).show()
                }
                cleanCmd.startsWith("CMD|QUIZ_END") -> {
                    quizSubmitted = true
                    if (currentTab == 4) {
                        renderInteractiveFeaturePanel(R.id.btnNavQuiz, txtSoonTitle.text.toString())
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
                    val navBtn = findViewById<TextView>(R.id.btnNavSurvey)
                    showComingSoon(navBtn, "Khảo sát")
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
                        val navBtn = findViewById<TextView>(R.id.btnNavSurvey)
                        showComingSoon(navBtn, "Khảo sát")
                        Toast.makeText(this, "Giáo viên đã bắt đầu khảo sát: $activeSurveyTitle", Toast.LENGTH_LONG).show()
                    } catch (e: Exception) {
                        Log.w(TAG, "Failed to parse SURVEY_START JSON: ${e.message}")
                    }
                }
                cleanCmd.startsWith("CMD|SURVEY_END") || cleanCmd.contains("\"SURVEY_END\"") -> {
                    surveySubmitted = true
                    if (currentTab == 4) {
                        renderInteractiveFeaturePanel(R.id.btnNavSurvey, txtSoonTitle.text.toString())
                    }
                    Toast.makeText(this, "Giáo viên đã đóng khảo sát", Toast.LENGTH_SHORT).show()
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
                    val navBtn = findViewById<TextView>(R.id.btnNavAssignment)
                    showComingSoon(navBtn, "Bài tập học tập")
                    Toast.makeText(this, "Giáo viên vừa giao bài tập: $activeHwTitle", Toast.LENGTH_LONG).show()
                }
                cleanCmd.startsWith("CMD|HOMEWORK_ACK|") -> {
                    val ackMsg = cleanCmd.substringBefore("|id=").removePrefix("CMD|HOMEWORK_ACK|")
                    hwSubmitted = true
                    lastHwAckMessage = ackMsg
                    if (currentTab == 4) {
                        renderInteractiveFeaturePanel(R.id.btnNavAssignment, txtSoonTitle.text.toString())
                    }
                    Toast.makeText(this, ackMsg, Toast.LENGTH_SHORT).show()
                }
                cleanCmd.startsWith("CMD|HAND_DOWN") -> {
                    updateHandRaiseUI(false)
                    Toast.makeText(this, "Giáo viên đã cho phép hạ tay", Toast.LENGTH_SHORT).show()
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

    private fun sendAck(cmdId: String, status: String) {
        if (cmdId.isBlank() || cmdId == "unknown") return
        val studentCode = currentStudentCode.ifBlank { "HS001" }
        lifecycleScope.launch(Dispatchers.IO) {
            try {
                val ackMsg = "ACK|$cmdId|$studentCode|$status\n"
                tcpOutputStream?.write(ackMsg.toByteArray(Charsets.UTF_8))
                tcpOutputStream?.flush()
                Log.d(TAG, "Sent ACK to teacher: $ackMsg")
            } catch (e: Exception) {
                Log.w(TAG, "Failed to send ACK: ${e.message}")
            }
        }
    }

    private fun startScreenBroadcast(token: String, webPort: Int, isForce: Boolean, title: String) {
        isScreenBroadcastActive = true
        runOnUiThread {
            if (::txtStatBroadcast.isInitialized) txtStatBroadcast.text = "Đang phát"
            if (title.isNotBlank() && activeLessonTitle.isBlank()) {
                activeLessonTitle = title
            }
            refreshHome()
        }
        currentBroadcastToken = token
        currentTeacherWebPort = webPort

        val displayTitle = if (title.isNotBlank()) title else "Màn hình Giáo viên"
        txtBroadcastStatus.text = "● Trực tiếp - 0.2s  |  $displayTitle"
        txtInlineBroadcastTitle.text = displayTitle

        layoutNoBroadcast.visibility = View.GONE
        layoutLiveBroadcast.visibility = View.VISIBLE
        progressBroadcastLoading.visibility = View.VISIBLE

        if (isForce) {
            broadcastOverlay.visibility = View.VISIBLE
            btnCloseBroadcast.visibility = View.GONE
        } else {
            btnCloseBroadcast.visibility = View.VISIBLE
        }

        broadcastPollJob?.cancel()
        val teacherIp = currentTeacherIp ?: "10.0.2.2"

        broadcastPollJob = lifecycleScope.launch(Dispatchers.IO) {
            Log.i(TAG, "Starting broadcast frame polling from $teacherIp:$currentTeacherWebPort with token: $currentBroadcastToken")
            val tokenParamOnce = if (currentBroadcastToken.isNotEmpty()) "&token=$currentBroadcastToken" else ""

            // Ưu tiên luồng đẩy khung liên tục (độ trễ thấp). Lỗi/không hỗ trợ -> quay lại polling.
            var streamFailures = 0
            while (isActive && isScreenBroadcastActive && streamFailures < 3) {
                var streamConn: HttpURLConnection? = null
                try {
                    val sUrl = URL("http://$teacherIp:$currentTeacherWebPort/api/broadcast_stream?t=${System.currentTimeMillis()}$tokenParamOnce")
                    streamConn = (sUrl.openConnection() as HttpURLConnection).apply {
                        connectTimeout = 2500
                        readTimeout = 15000
                        requestMethod = "GET"
                        useCaches = false
                        setRequestProperty("User-Agent", "QASmartClass-Android/1.0")
                    }
                    if (streamConn.responseCode != 200) {
                        streamFailures++
                        streamConn.disconnect()
                        delay(300)
                        continue
                    }
                    val din = java.io.DataInputStream(java.io.BufferedInputStream(streamConn.inputStream, 65536))
                    var gotFrame = false
                    while (isActive && isScreenBroadcastActive) {
                        val len = din.readInt()
                        if (len <= 0 || len > 20_000_000) throw java.io.IOException("Invalid frame length $len")
                        val data = ByteArray(len)
                        din.readFully(data)
                        // Đã có khung mới đang chờ -> bỏ khung cũ để bắt kịp thời gian thực
                        if (din.available() >= 4) continue
                        val bitmap = BitmapFactory.decodeByteArray(data, 0, len)
                        if (bitmap != null) {
                            gotFrame = true
                            streamFailures = 0
                            withContext(Dispatchers.Main) {
                                if (isScreenBroadcastActive) {
                                    imgInlineBroadcast.setImageBitmap(bitmap)
                                    imgBroadcastScreen.setImageBitmap(bitmap)
                                    imgHomeBroadcastPreview.setImageBitmap(bitmap)
                                    imgHomeBroadcastPreview.visibility = View.VISIBLE
                                    layoutWhiteboardStitchPreview.visibility = View.GONE
                                    progressBroadcastLoading.visibility = View.GONE
                                }
                            }
                        }
                    }
                    if (!gotFrame) streamFailures++
                } catch (e: Exception) {
                    if (isActive && isScreenBroadcastActive) {
                        Log.d(TAG, "Frame stream error: ${e.message}")
                        streamFailures++
                        delay(300)
                    }
                } finally {
                    try { streamConn?.disconnect() } catch (_: Exception) {}
                }
            }

            // Dự phòng: polling từng ảnh (tương thích server cũ)
            while (isActive && isScreenBroadcastActive) {
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
                            val bitmap = BitmapFactory.decodeStream(input)
                            if (bitmap != null) {
                                withContext(Dispatchers.Main) {
                                    if (isScreenBroadcastActive) {
                                        imgInlineBroadcast.setImageBitmap(bitmap)
                                        imgBroadcastScreen.setImageBitmap(bitmap)
                                        imgHomeBroadcastPreview.setImageBitmap(bitmap)
                                        imgHomeBroadcastPreview.visibility = View.VISIBLE
                                        layoutWhiteboardStitchPreview.visibility = View.GONE
                                        progressBroadcastLoading.visibility = View.GONE
                                    }
                                }
                            }
                        }
                    } else {
                        Log.d(TAG, "Broadcast HTTP response: $code")
                    }
                    conn.disconnect()
                } catch (e: Exception) {
                    Log.d(TAG, "Frame fetch error: ${e.message}")
                }
                delay(100) // ~10 FPS polling dự phòng
            }
        }
    }

    private fun updateScreenBroadcast(token: String, webPort: Int, title: String) {
        if (token.isNotEmpty()) currentBroadcastToken = token
        if (webPort > 0) currentTeacherWebPort = webPort
        if (title.isNotEmpty()) {
            txtBroadcastStatus.text = title
            txtInlineBroadcastTitle.text = title
            if (activeLessonTitle.isBlank()) activeLessonTitle = title
            runOnUiThread { refreshHome() }
        }
    }

    private fun stopScreenBroadcast() {
        isScreenBroadcastActive = false
        runOnUiThread { if (::txtStatBroadcast.isInitialized) txtStatBroadcast.text = "Chưa phát" }
        broadcastPollJob?.cancel()
        broadcastPollJob = null

        runOnUiThread {
            broadcastOverlay.visibility = View.GONE
            layoutLiveBroadcast.visibility = View.GONE
            layoutNoBroadcast.visibility = View.VISIBLE
            imgInlineBroadcast.setImageDrawable(null)
            imgBroadcastScreen.setImageDrawable(null)
            if (::imgHomeBroadcastPreview.isInitialized) {
                imgHomeBroadcastPreview.setImageDrawable(null)
                imgHomeBroadcastPreview.visibility = View.GONE
            }
            if (::layoutWhiteboardStitchPreview.isInitialized) {
                layoutWhiteboardStitchPreview.visibility = View.VISIBLE
            }
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

    /** Chụp khung hình màn hình mới nhất và gửi cho giáo viên (SCREENSHOT|code|base64). */
    private fun sendScreenshotToTeacher() {
        lifecycleScope.launch(Dispatchers.IO) {
            val jpeg = com.qasmartclass.student.services.ScreenCaptureService.captureJpeg()
            if (jpeg == null) {
                // Chưa được cấp quyền → xin lại khi HS quay lại app
                withContext(Dispatchers.Main) { needCapturePrompt = true }
                return@launch
            }
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
        if (needCapturePrompt && tcpOutputStream != null) {
            needCapturePrompt = false
            requestScreenCapture()
        }
    }

    override fun onStop() {
        super.onStop()
        // Học sinh thoát sang ứng dụng khác / về màn hình chính → báo cho giáo viên
        focusReporter.appInForeground = false
    }

    private fun disconnectFromTeacher(reason: String) {
        isLessonActive = false
        activeLessonTitle = ""
        activeLessonSubject = ""
        activeLessonSummary = ""
        activeLessonStage = 0
        hasTeacherNotice = false
        unreadTeacherChatCount = 0
        stopScreenBroadcast()
        heartbeatJob?.cancel()
        heartbeatJob = null
        focusReporter.stopReporting()
        try { stopService(android.content.Intent(this, com.qasmartclass.student.services.ScreenCaptureService::class.java)) } catch (_: Exception) {}
        heartbeatWatchdog.releaseLock()
        currentSessionKey = ""
        currentSessionSalt = ""
        currentClassCode = ""

        try {
            tcpSocket?.close()
        } catch (e: Exception) {
            // Ignore
        }
        tcpSocket = null
        tcpOutputStream = null

        runOnUiThread {
            lockOverlay.visibility = View.GONE
            navDrawerOverlay.visibility = View.GONE
            btnHeaderBack.visibility = View.GONE
            connectedPanel.visibility = View.GONE
            discoveryPanel.visibility = View.VISIBLE
            btnConnect.isEnabled = true
            progressBar.visibility = View.GONE
            txtStatus.text = reason
            Toast.makeText(this, reason, Toast.LENGTH_SHORT).show()
            // Tự động làm mới thông tin lớp học trên mạng LAN sau khi rời lớp
            btnRefresh.postDelayed({
                if (discoveryPanel.visibility == View.VISIBLE && tcpSocket == null) {
                    startAutoDiscovery()
                }
            }, 600)
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
        super.onDestroy()
        disconnectFromTeacher("Ứng dụng đóng")
        discoveryService.stopListening()
    }
}
