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
    private lateinit var btnTogglePassword: ImageButton
    private lateinit var edtTeacherIp: EditText
    private lateinit var chkRemember: CheckBox
    private lateinit var txtError: TextView
    private lateinit var btnConnect: Button

    // Connected Header & Profile Bar
    private lateinit var connectedPanel: LinearLayout
    private lateinit var txtConnectedClassInfo: TextView
    private lateinit var txtTeacherDetail: TextView
    private lateinit var txtStudentDetail: TextView
    private lateinit var txtServerIpDetail: TextView
    private lateinit var btnDisconnect: Button

    // Tab Bar Navigation
    private lateinit var btnTabLecture: Button
    private lateinit var btnTabChat: Button
    private lateinit var btnTabHandRaise: Button

    // Sidebar (drawer) & Home dashboard (giao diện giống StudentShell WPF)
    private lateinit var navDrawerOverlay: FrameLayout
    private lateinit var btnMenu: Button
    private lateinit var txtPageTitle: TextView
    private lateinit var btnNavHome: Button
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
    private var homeStudentName = ""
    private var homeClassName = ""
    private var homeTeacherName = ""
    private val soonNavButtons = mutableListOf<Pair<Button, String>>()

    // Tab 1: Lecture & Broadcast
    private lateinit var panelLecture: LinearLayout
    private lateinit var layoutNoBroadcast: LinearLayout
    private lateinit var layoutLiveBroadcast: LinearLayout
    private lateinit var txtInlineBroadcastTitle: TextView
    private lateinit var imgInlineBroadcast: ImageView
    private lateinit var btnFullscreenBroadcast: Button
    private lateinit var btnRaiseHand: Button
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
     * Responsive: giới hạn chiều rộng khối nội dung theo kích thước màn hình
     * (điện thoại = full width, tablet/landscape = giới hạn và căn giữa).
     */
    private fun applyResponsiveLayout() {
        val dm = resources.displayMetrics
        val screenDp = dm.widthPixels / dm.density
        fun capWidth(view: View, maxDp: Float) {
            val lp = view.layoutParams as? LinearLayout.LayoutParams ?: return
            val targetPx = if (screenDp > maxDp) (maxDp * dm.density).toInt()
                           else ViewGroup.LayoutParams.MATCH_PARENT
            if (lp.width != targetPx || lp.gravity != android.view.Gravity.CENTER_HORIZONTAL) {
                lp.width = targetPx
                lp.gravity = android.view.Gravity.CENTER_HORIZONTAL
                view.layoutParams = lp
            }
        }
        capWidth(findViewById(R.id.formContainer), 520f)
        listOf(panelHome, panelSoon, panelLecture, panelChat, panelHandRaise)
            .forEach { capWidth(it, 900f) }
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
        btnTogglePassword = findViewById(R.id.btnTogglePassword)
        edtTeacherIp = findViewById(R.id.edtTeacherIp)
        chkRemember = findViewById(R.id.chkRemember)
        txtError = findViewById(R.id.txtError)
        btnConnect = findViewById(R.id.btnConnect)

        // Connected Top Header & Profile Bar
        connectedPanel = findViewById(R.id.connectedPanel)
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
        btnMenu = findViewById(R.id.btnMenu)
        txtPageTitle = findViewById(R.id.txtPageTitle)
        btnNavHome = findViewById(R.id.btnNavHome)
        panelHome = findViewById(R.id.panelHome)
        panelSoon = findViewById(R.id.panelSoon)
        txtSoonTitle = findViewById(R.id.txtSoonTitle)
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
        soonNavButtons.clear()
        soonNavButtons += findViewById<Button>(R.id.btnNavWhiteboard) to "🖍️ Bảng trắng học sinh"
        soonNavButtons += findViewById<Button>(R.id.btnNavQuiz) to "📝 Bài kiểm tra"
        soonNavButtons += findViewById<Button>(R.id.btnNavAssignment) to "📚 Bài tập học tập"
        soonNavButtons += findViewById<Button>(R.id.btnNavSurvey) to "📋 Khảo sát"
        soonNavButtons += findViewById<Button>(R.id.btnNavResults) to "📊 Kết quả học tập"
        soonNavButtons += findViewById<Button>(R.id.btnNavProgress) to "🎯 Tiến trình học tập"
        soonNavButtons += findViewById<Button>(R.id.btnNavSettings) to "⚙️ Cài đặt"
        soonNavButtons += findViewById<Button>(R.id.btnNavGuide) to "💡 Hướng dẫn sử dụng"

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

        // Load saved preferences
        val prefs = getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)
        val remember = prefs.getBoolean(KEY_REMEMBER, true)
        chkRemember.isChecked = remember

        val lastIp = prefs.getString(KEY_LAST_IP, "")
        val lastStudentName = prefs.getString(KEY_STUDENT_NAME, "")
        val lastStudentCode = prefs.getString(KEY_STUDENT_CODE, "HS001")
        val lastPassword = prefs.getString(KEY_PASSWORD, "123456")

        if (remember) {
            if (!lastIp.isNullOrBlank()) edtTeacherIp.setText(lastIp)
            if (!lastStudentName.isNullOrBlank()) edtStudentName.setText(lastStudentName)
            if (!lastStudentCode.isNullOrBlank()) edtStudentCode.setText(lastStudentCode)
            if (!lastPassword.isNullOrBlank()) edtPassword.setText(lastPassword)
        }
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

        btnConnect.setOnClickListener {
            txtError.visibility = View.GONE
            val studentCode = edtStudentCode.text.toString().trim()
            val password = edtPassword.text.toString().trim()
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

            if (studentName.isBlank()) {
                studentName = "Học sinh $studentCode"
            }

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

            connectToTeacher(ip, tcpPort, studentName, studentCode, password)
        }

        btnDisconnect.setOnClickListener {
            disconnectFromTeacher("Học sinh chủ động rời lớp")
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
        findViewById<Button>(R.id.btnHomeRaise).setOnClickListener { switchTab(2) }
        findViewById<Button>(R.id.btnHomeChat).setOnClickListener { switchTab(1) }
        findViewById<Button>(R.id.btnHomeLecture).setOnClickListener { switchTab(0) }

        // Hand Raise Listeners (Quick Button, Main Button, and Fullscreen Button)
        btnRaiseHand.setOnClickListener { toggleRaiseHand() }
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
    }

    private fun switchTab(index: Int) {
        currentTab = index
        panelLecture.visibility = if (index == 0) View.VISIBLE else View.GONE
        panelChat.visibility = if (index == 1) View.VISIBLE else View.GONE
        panelHandRaise.visibility = if (index == 2) View.VISIBLE else View.GONE
        panelHome.visibility = if (index == 3) View.VISIBLE else View.GONE
        panelSoon.visibility = View.GONE

        val activeBtn = when (index) {
            0 -> btnTabLecture
            1 -> btnTabChat
            2 -> btnTabHandRaise
            else -> btnNavHome
        }
        txtPageTitle.text = when (index) {
            0 -> "📺 Bài giảng hôm nay"
            1 -> "💬 Tin nhắn"
            2 -> "✋ Hỏi bài Giáo viên"
            else -> "🏠 Trang chủ"
        }
        setNavActive(activeBtn)
        focusReporter.currentPage = when (index) {
            0 -> "Bài giảng"
            1 -> "Tin nhắn"
            2 -> "Giơ tay"
            else -> "Trang chủ"
        }
        navDrawerOverlay.visibility = View.GONE

        if (index == 1) {
            btnTabChat.text = "💬  Tin nhắn"
            scrollChat.post { scrollChat.fullScroll(View.FOCUS_DOWN) }
        }
        if (index == 3) refreshHome()
    }

    /** Tô sáng mục đang chọn trên Sidebar (gradient xanh giống StuNavActive của WPF). */
    private fun setNavActive(active: Button?) {
        val all = mutableListOf(btnNavHome, btnTabLecture, btnTabChat, btnTabHandRaise)
        all += soonNavButtons.map { it.first }
        for (btn in all) {
            if (btn === active) {
                btn.setBackgroundResource(R.drawable.bg_tab_selected)
                btn.setTextColor(0xFFFFFFFF.toInt())
            } else {
                btn.setBackgroundColor(android.graphics.Color.TRANSPARENT)
                btn.setTextColor(0xFFCBD5E1.toInt())
            }
        }
    }

    /** Các trang của bản Windows chưa có trên Android: hiển thị trang thông báo thay vì bỏ trống. */
    private fun showComingSoon(btn: Button, title: String) {
        currentTab = 4
        panelHome.visibility = View.GONE
        panelLecture.visibility = View.GONE
        panelChat.visibility = View.GONE
        panelHandRaise.visibility = View.GONE
        panelSoon.visibility = View.VISIBLE
        txtSoonTitle.text = title
        txtPageTitle.text = title
        setNavActive(btn)
        focusReporter.currentPage = title.substringAfter(" ")
        navDrawerOverlay.visibility = View.GONE
    }

    /** Cập nhật Trang chủ (dashboard) từ trạng thái thật của phiên lớp. */
    private fun refreshHome() {
        if (!::txtHomeGreeting.isInitialized) return
        val dateText = SimpleDateFormat("EEEE, dd/MM/yyyy", Locale("vi", "VN")).format(Date())
        txtHomeGreeting.text = "Xin chào, ${homeStudentName.ifBlank { "Học sinh" }} 👋"
        txtHomeSub.text = "📅 $dateText  •  $homeClassName"
        txtStatClass.text = homeClassName.ifBlank { "—" }
        txtStatTeacher.text = homeTeacherName.ifBlank { "—" }
        txtStatBroadcast.text = if (isScreenBroadcastActive) "Đang phát" else "Chưa phát"
        txtStatHand.text = if (isHandRaised) "Đang giơ tay" else "Không"
        txtHomeLessonTitle.text = homeClassName.ifBlank { "—" }
        txtHomeLessonSub.text = if (isScreenBroadcastActive) "Giáo viên đang chia sẻ màn hình bài giảng" else "Chờ Giáo viên bắt đầu chia sẻ bài giảng"
        txtHomeTeacherChip.text = "GV: ${homeTeacherName.ifBlank { "—" }}"
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

    private fun toggleRaiseHand() {
        val targetState = !isHandRaised
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
            Toast.makeText(this, "✋ Đã giơ tay xin phát biểu: $currentHandRaiseReason", Toast.LENGTH_SHORT).show()
        } else {
            Toast.makeText(this, "Đã hạ tay", Toast.LENGTH_SHORT).show()
        }
    }

    private fun updateHandRaiseUI(raised: Boolean) {
        isHandRaised = raised
        runOnUiThread { if (::txtStatHand.isInitialized) txtStatHand.text = if (raised) "Đang giơ tay" else "Không" }
        runOnUiThread {
            if (raised) {
                btnRaiseHand.text = "✅ Đang Giơ Tay (Bấm để hạ tay)"
                btnRaiseHand.setBackgroundColor(0xFF059669.toInt())
                btnFullscreenRaiseHand.text = "✅ Đang giơ tay"
                btnFullscreenRaiseHand.backgroundTintList = ColorStateList.valueOf(0xFF059669.toInt())

                btnMainRaiseHand.setBackgroundResource(R.drawable.bg_hand_raise_active)
                txtHandRaiseIcon.text = "✋"
                txtHandRaiseStatus.text = "ĐANG GIƠ TAY\n(Chờ GV mời)"
                txtHandRaiseStatus.setTextColor(0xFFFFFFFF.toInt())
            } else {
                btnRaiseHand.text = "✋ Giơ Tay Phát Biểu"
                btnRaiseHand.setBackgroundColor(0xFFF59E0B.toInt())
                btnFullscreenRaiseHand.text = "✋ Giơ tay"
                btnFullscreenRaiseHand.backgroundTintList = ColorStateList.valueOf(0xFFF59E0B.toInt())

                btnMainRaiseHand.setBackgroundResource(R.drawable.bg_hand_raise_idle)
                txtHandRaiseIcon.text = "✋"
                txtHandRaiseStatus.text = "Bấm để Giơ tay"
                txtHandRaiseStatus.setTextColor(0xFFB45309.toInt())
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

    private fun startAutoDiscovery() {
        txtStatus.text = "Đang quét tìm lớp học qua WiFi/LAN..."
        progressBar.visibility = View.VISIBLE
        btnRefresh.visibility = View.GONE

        lifecycleScope.launch {
            val beacon = discoveryService.discoverTeacher(timeoutMs = 6000)
            if (beacon != null) {
                discoveredTeacherIp = beacon.teacherIp
                discoveredTeacherPort = beacon.tcpPort
                currentClassName = beacon.className
                currentTeacherName = beacon.teacherName
                txtStatus.text = "Đã tìm thấy lớp: ${beacon.className} (GV: ${beacon.teacherName})"
                edtTeacherIp.setText(beacon.teacherIp)
                progressBar.visibility = View.GONE
                btnRefresh.visibility = View.VISIBLE
            } else {
                // Không nhận được beacon UDP (emulator/NAT/router chặn broadcast) -> dò cổng TCP trực tiếp
                txtStatus.text = "Đang dò máy chủ giáo viên (cổng $TCP_PORT)..."
                val host = probeTeacherHost()
                if (host != null) {
                    discoveredTeacherIp = host
                    discoveredTeacherPort = TCP_PORT
                    val info = withContext(Dispatchers.IO) { queryClassInfo(host) }
                    if (info != null && info.size >= 3) {
                        currentClassName = info[1]
                        currentTeacherName = info[2]
                        txtStatus.text = "Đã tìm thấy lớp: ${info[1]} (GV: ${info[2]})"
                    } else {
                        txtStatus.text = "Đã tìm thấy máy giáo viên: $host:$TCP_PORT"
                    }
                    edtTeacherIp.setText(host)
                } else {
                    txtStatus.text = "Chưa tìm thấy lớp học. Hãy chắc chắn giáo viên đã bắt đầu lớp, rồi bấm quét lại."
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
        password: String
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

                // Send Native JOIN handshake with password
                // Format: JOIN|name|code|pcName|version|pubKeyBase64|macAddress|password
                val joinMsg = "JOIN|$studentName|$studentCode|Android_Tablet|1.0|||$password\n"
                output.write(joinMsg.toByteArray(Charsets.UTF_8))
                output.flush()

                // Read ACK
                val buffer = ByteArray(2048)
                val len = input.read(buffer)
                val rawAck = String(buffer, 0, len, Charsets.UTF_8).trim()
                val ack = rawAck.removePrefix("\uFEFF").trim()
                Log.i(TAG, "Server ACK: $ack")

                if (ack.startsWith("OK|") || ack == "OK") {
                    val ackParts = ack.split("|")
                    // Windows Server ACK format: OK|{Guid}|{serverTimestamp}|{encryptedSessionKeyBase64}|{classCode}|{sessionSalt}
                    if (ackParts.size >= 4 && ackParts[3].isNotBlank()) {
                        currentSessionKey = ackParts[3]
                    }
                    if (ackParts.size >= 5 && ackParts[4].isNotBlank()) {
                        currentClassCode = ackParts[4]
                        currentClassName = ackParts[4]
                    }
                    if (ackParts.size >= 6 && ackParts[5].isNotBlank()) {
                        currentSessionSalt = ackParts[5]
                    }
                    Log.i(TAG, "Handshake OK - ClassCode: $currentClassCode, SessionKey: ${currentSessionKey.take(6)}..., SessionSalt: ${currentSessionSalt.take(6)}...")

                    withContext(Dispatchers.Main) {
                        txtError.visibility = View.GONE
                        onConnectedSuccess(studentName, studentCode, ip, currentClassName, currentTeacherName)
                    }

                    // Start Heartbeat & Focus reporter
                    startHeartbeatLoop(studentCode)
                    focusReporter.startReporting()

                    // Start reading commands from Teacher
                    readTcpStream(input)
                } else if (ack.startsWith("ERR|")) {
                    val parts = ack.split("|")
                    val errMsg = if (parts.size >= 3) parts[2] else (if (parts.size >= 2) parts[1] else "Đăng nhập thất bại")
                    throw Exception(errMsg)
                } else {
                    throw Exception("Máy chủ từ chối kết nối: $ack")
                }
            } catch (e: Exception) {
                Log.e(TAG, "Connection error: ${e.message}")
                withContext(Dispatchers.Main) {
                    showError(e.message ?: "Kết nối thất bại")
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
        discoveryPanel.visibility = View.GONE
        connectedPanel.visibility = View.VISIBLE
        btnConnect.isEnabled = true
        progressBar.visibility = View.GONE

        txtConnectedClassInfo.text = "Lớp: $className"
        txtStudentDetail.text = "👤 Học sinh: $name ($code)"
        txtTeacherDetail.text = "👨‍🏫 Giáo viên: $teacherName"
        txtServerIpDetail.text = "🌐 Máy chủ: $ip:$TCP_PORT"
        messageCard.visibility = View.GONE

        updateHandRaiseUI(false)
        selectHandRaiseReason("Phát biểu")
        homeStudentName = name
        homeClassName = className
        homeTeacherName = teacherName
        txtHomeNotice.text = "Chưa có thông báo mới từ Giáo viên."
        requestScreenCapture()
        switchTab(3)

        // Reset broadcast UI state
        stopScreenBroadcast()

        Toast.makeText(this, "Đã kết nối trực tiếp với SmartClass!", Toast.LENGTH_SHORT).show()
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
                disconnectFromTeacher("Mất kết nối tới máy chủ Giáo viên")
            }
        }
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
                cleanCmd.startsWith("CMD|REQUEST_SCREENSHOT") -> {
                    sendScreenshotToTeacher()
                }
                cleanCmd.startsWith("CMD|BLACK_SCREEN") -> {
                    val on = cleanCmd.split("|").getOrNull(2) == "1"
                    if (on) {
                        txtLockTitle.text = "MÀN HÌNH ĐÃ TẮT"
                        txtLockMessage.text = "Giáo viên đã tắt màn hình. Hãy chú ý lên bảng."
                        lockOverlay.setBackgroundColor(0xFF000000.toInt())
                        lockOverlay.visibility = View.VISIBLE
                    } else {
                        lockOverlay.visibility = View.GONE
                    }
                }
                cleanCmd.startsWith("CMD|LOCK") -> {
                    val on = cleanCmd.split("|").getOrNull(2) != "0"
                    if (on) {
                        txtLockTitle.text = "MÀN HÌNH ĐÃ BỊ KHÓA"
                        txtLockMessage.text = "Giáo viên đã khóa màn hình tập trung. Vui lòng chú ý lắng nghe bài giảng."
                        lockOverlay.setBackgroundColor(0xF80B0F19.toInt())
                        lockOverlay.visibility = View.VISIBLE
                        heartbeatWatchdog.acquireLock()
                        Toast.makeText(this, "🔒 Giáo viên đã khóa màn hình tập trung", Toast.LENGTH_SHORT).show()
                    } else {
                        lockOverlay.visibility = View.GONE
                        heartbeatWatchdog.releaseLock()
                    }
                }
                cleanCmd.startsWith("CMD|UNLOCK") -> {
                    lockOverlay.visibility = View.GONE
                    heartbeatWatchdog.releaseLock()
                    Toast.makeText(this, "🔓 Giáo viên đã mở khóa màn hình", Toast.LENGTH_SHORT).show()
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

                    if (currentTab != 1) {
                        btnTabChat.text = "💬  Tin nhắn  ●"
                    }
                    Toast.makeText(this, "📩 Tin nhắn từ GV: $msgText", Toast.LENGTH_LONG).show()
                }
                cleanCmd.startsWith("CMD|UPDATE_SESSION|") -> {
                    val updateParts = cleanCmd.split("|")
                    if (updateParts.size >= 4) {
                        currentClassCode = updateParts[2]
                        currentSessionSalt = updateParts[3]
                        Log.i(TAG, "Updated session: ClassCode=$currentClassCode, SessionSalt=${currentSessionSalt.take(6)}...")
                    }
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
        runOnUiThread { if (::txtStatBroadcast.isInitialized) txtStatBroadcast.text = "Đang phát" }
        currentBroadcastToken = token
        currentTeacherWebPort = webPort

        val displayTitle = if (title.isNotBlank()) title else "Màn hình Giáo viên (Đang phát)"
        txtBroadcastStatus.text = displayTitle
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
            progressBroadcastLoading.visibility = View.GONE
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
            connectedPanel.visibility = View.GONE
            discoveryPanel.visibility = View.VISIBLE
            btnConnect.isEnabled = true
            progressBar.visibility = View.GONE
            txtStatus.text = reason
            Toast.makeText(this, reason, Toast.LENGTH_SHORT).show()
        }
    }

    private fun showError(message: String) {
        runOnUiThread {
            txtError.text = message
            txtError.visibility = View.VISIBLE
            txtStatus.text = "Lỗi: $message"
            progressBar.visibility = View.GONE
            btnConnect.isEnabled = true
            Toast.makeText(this, message, Toast.LENGTH_LONG).show()
        }
    }

    override fun onDestroy() {
        super.onDestroy()
        disconnectFromTeacher("Ứng dụng đóng")
        discoveryService.stopListening()
    }
}
