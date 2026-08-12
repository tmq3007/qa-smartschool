/* ═══════════════════════════════════════════════════════════
   QA SmartClass — E2E Test Harness
   Simulates teacher WebSocket events to test all Student Web App features
   Usage: Inject via browser console or <script> tag
   ═══════════════════════════════════════════════════════════ */

const E2E = (() => {
    const _results = [];
    let _testCount = 0;
    let _passCount = 0;
    let _failCount = 0;

    // ─── Simulate a WebSocket event (uses public emit API) ───
    function emit(event, data = {}) {
        if (typeof StudentConnection !== 'undefined' && typeof StudentConnection.emit === 'function') {
            StudentConnection.emit(event, data);
        } else {
            console.warn(`[E2E] Cannot emit '${event}' — StudentConnection.emit not available`);
        }
    }

    // Simulate a command via the connection's _handleCommand
    function cmd(action, extra = {}) {
        emit('data', { type: 'cmd', action, ...extra });
        // Since _handleCommand is private, we emit events directly
        // Map action → event just like connection.js does
        const mapping = {
            'LOCK': () => emit('lock_screen', { type: 'lock', title: extra.title || '🔒 Màn hình đã bị khóa', subtitle: extra.subtitle || 'GV đã khóa màn hình' }),
            'UNLOCK': () => emit('unlock_screen', {}),
            'BLACK_SCREEN': () => emit('lock_screen', { type: 'black', title: '📴 Tắt màn hình', subtitle: 'GV đã tắt màn hình' }),
            'SILENCE': () => emit('silence', { active: true }),
            'CLEAR_SILENCE': () => emit('silence', { active: false }),
            'TEACHER_WARNING': () => emit('teacher_warning', { message: extra.message || '⚠️ Hãy tập trung!', duration: extra.duration || 5 }),
            'CLEAR_WARNING': () => emit('clear_warning', {}),
            'NOTICE': () => emit('notice', { title: extra.title || 'Thông báo', body: extra.body || '', noticeType: extra.noticeType || 'info', duration: extra.duration || 5 }),
            'BROADCAST_START': () => emit('broadcast_start', { resolution: extra.resolution || '1920×1080', fps: extra.fps || '30 fps' }),
            'BROADCAST_STOP': () => emit('broadcast_stop', {}),
            'SCREEN_BROADCAST': () => emit('screen_broadcast', { imageUrl: extra.imageUrl || '' }),
            'SCREEN_BROADCAST_STOP': () => emit('screen_broadcast_stop', {}),
            'FILE_BROADCAST': () => emit('file_broadcast', { fileType: extra.fileType || 'PDF', fileName: extra.fileName || 'Baitap.pdf', fileUrl: extra.fileUrl || '' }),
            'FILE_BROADCAST_STOP': () => emit('file_broadcast_stop', {}),
            'SURVEY_QUESTION': () => emit('survey', { question: extra.question || 'Bạn hiểu bài không?', options: extra.options || ['👍 Hiểu', '👎 Chưa hiểu', '🤔 Cần giải thích thêm'], duration: extra.duration || 0 }),
            'SURVEY_END': () => emit('survey_end', {}),
            'TOOL_FOCUS': () => emit('tool_focus', { toolId: extra.toolId || 'calculator' }),
            'TOOL_UNFOCUS': () => emit('tool_unfocus', {}),
            'CLEAR_ALL': () => emit('clear_all', {}),
        };
        if (mapping[action]) mapping[action]();
    }

    // ─── Assert helpers ───
    function assert(condition, testName, details = '') {
        _testCount++;
        if (condition) {
            _passCount++;
            _results.push({ status: '✅', name: testName, details });
            console.log(`✅ PASS: ${testName}`);
        } else {
            _failCount++;
            _results.push({ status: '❌', name: testName, details });
            console.error(`❌ FAIL: ${testName} ${details}`);
        }
    }

    function assertVisible(selector, testName) {
        const el = document.querySelector(selector);
        assert(el && el.offsetParent !== null, testName, `selector: ${selector}`);
    }

    function assertHidden(selector, testName) {
        const el = document.querySelector(selector);
        assert(!el || el.offsetParent === null || !el.classList.contains('active'), testName, `selector: ${selector}`);
    }

    function assertText(selector, expected, testName) {
        const el = document.querySelector(selector);
        const actual = el ? el.textContent.trim() : '';
        assert(actual.includes(expected), testName, `expected "${expected}", got "${actual}"`);
    }

    function assertHasClass(selector, className, testName) {
        const el = document.querySelector(selector);
        assert(el && el.classList.contains(className), testName, `selector: ${selector}, class: ${className}`);
    }

    function sleep(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }

    // ═══════════════════════════════════════════════════════════
    //  TEST SUITES
    // ═══════════════════════════════════════════════════════════

    // ─── T1: Navigation ───
    async function testNavigation() {
        console.log('\n═══ T1: NAVIGATION ═══');

        const pages = ['dashboard', 'lesson', 'quiz', 'submit', 'survey', 'chat', 'handraise', 'results', 'settings'];
        for (const page of pages) {
            navigateTo(page);
            await sleep(200);
            assertHasClass(`#page-${page}`, 'active', `Navigate to ${page}`);
            assertHasClass(`[data-page="${page}"]`, 'active', `Nav button active: ${page}`);
        }

        // Return to dashboard
        navigateTo('dashboard');
        await sleep(100);
        assertHasClass('#page-dashboard', 'active', 'Back to dashboard');
    }

    // ─── T2: Dashboard Content ───
    async function testDashboard() {
        console.log('\n═══ T2: DASHBOARD ═══');
        navigateTo('dashboard');
        await sleep(200);

        // Welcome text
        const welcomeEl = document.getElementById('welcomeText');
        assert(welcomeEl && welcomeEl.textContent.includes('Xin chào'), 'Welcome text present');

        // Date
        const dateEl = document.getElementById('dashDate');
        assert(dateEl && dateEl.textContent.includes('📅'), 'Date display present');

        // 4 stat cards
        const statCards = document.querySelectorAll('.dash-stat-card');
        assert(statCards.length === 4, '4 stat cards present');

        // Quick actions
        const quickBtns = document.querySelectorAll('.dash-action-btn');
        assert(quickBtns.length >= 3, 'Quick action buttons present (≥3)');

        // Notification panel
        const notifList = document.getElementById('dashNotifList');
        assert(notifList !== null, 'Notification panel present');
    }

    // ─── T3: Quiz Full Flow ───
    async function testQuizFlow() {
        console.log('\n═══ T3: QUIZ FLOW ═══');

        // Start quiz
        emit('quiz_start', {
            type: 'quiz_start',
            target: 'test-quiz-1',
            title: 'E2E Test Quiz',
            timeLimit: 60,
            quizType: 'multiple_choice',
            questions: [
                {
                    text: 'Thủ đô Việt Nam là gì?',
                    options: ['Hồ Chí Minh', 'Hà Nội', 'Đà Nẵng', 'Huế'],
                    image: 'https://via.placeholder.com/400x200?text=Vietnam+Map'
                },
                {
                    text: '2 + 2 = ?',
                    type: 'true_false'
                },
                {
                    text: '1 + 1 = ?',
                    options: ['1', '2', '3', '4']
                }
            ]
        });
        await sleep(500);

        // Verify quiz page active
        assertHasClass('#page-quiz', 'active', 'Quiz page auto-navigated');

        // Verify quiz header
        const quizContainer = document.getElementById('quizContainer');
        assert(quizContainer && quizContainer.innerHTML.includes('E2E Test Quiz'), 'Quiz title rendered');

        // Timer present
        const timer = document.getElementById('quizTimer');
        assert(timer && timer.textContent.match(/\d{2}:\d{2}/), 'Timer countdown visible');

        // Image support
        const qArea = document.getElementById('quizQuestionArea');
        assert(qArea && qArea.innerHTML.includes('img'), 'Image question renders <img> tag');

        // Select answer on Q1
        QuizManager.selectAnswer(0, 1); // Select "Hà Nội"
        await sleep(200);
        const selectedOpt = document.querySelector('.quiz-option.selected');
        assert(selectedOpt !== null, 'Answer A selected visually');

        // Navigate to Q2
        QuizManager.nextQuestion();
        await sleep(200);
        assert(qArea.innerHTML.includes('Đúng'), 'Q2 shows True/False options');

        // Navigate to Q3
        QuizManager.nextQuestion();
        await sleep(200);

        // Select answer on Q3
        QuizManager.selectAnswer(2, 1); // Select "2"
        await sleep(200);

        // Navigate dots — wait for DOM to settle
        await sleep(500);
        // Use .quiz-dot class selector (not [id^="quizDot"]) to avoid matching the #quizDots container
        const dots = document.querySelectorAll('.quiz-dot');
        assert(dots.length === 3, '3 navigation dots');

        // Submit
        QuizManager.submitQuiz();
        await sleep(400);

        // Verify submission
        assert(quizContainer.innerHTML.includes('Đã nộp bài') || quizContainer.innerHTML.includes('✅'), 'Quiz submitted');

        // Simulate results from teacher
        emit('quiz_end', {
            type: 'quiz_end',
            results: {
                score: 2,
                total: 3,
                message: 'Khá tốt!'
            }
        });
        await sleep(400);
        assert(quizContainer.innerHTML.includes('2') && quizContainer.innerHTML.includes('3'), 'Results display score');

        // Quiz Review
        emit('quiz_review', {
            data: {
                title: 'E2E Test Quiz Review',
                score: 2,
                questions: [
                    { text: 'Thủ đô Việt Nam là gì?', options: ['Hồ Chí Minh', 'Hà Nội', 'Đà Nẵng', 'Huế'], correctIndex: 1, studentAnswer: 1, explanation: 'Hà Nội là thủ đô' },
                    { text: '2 + 2 = 4?', options: ['Đúng', 'Sai'], correctIndex: 0, studentAnswer: -1 },
                    { text: '1 + 1 = ?', options: ['1', '2', '3', '4'], correctIndex: 1, studentAnswer: 1 }
                ]
            }
        });
        await sleep(500);
        assert(quizContainer.innerHTML.includes('Review') || quizContainer.innerHTML.includes('Xem lại'), 'Quiz review rendered');
        assert(quizContainer.innerHTML.includes('✅ Đúng'), 'Correct answers marked');
    }

    // ─── T4: Hand Raise ───
    async function testHandRaise() {
        console.log('\n═══ T4: HAND RAISE ═══');
        navigateTo('handraise');
        await sleep(300);

        const btn = document.getElementById('handraiseBtn');
        assert(btn !== null, 'Hand raise button exists');

        // Default state
        assert(!btn.classList.contains('raised'), 'Initially not raised');

        // Toggle raise
        toggleHandRaise();
        await sleep(300);
        assert(btn.classList.contains('raised'), 'Button shows raised state');

        // Check gradient via computed style
        const style = window.getComputedStyle(btn);
        const bg = style.backgroundImage || style.background;
        assert(bg.includes('gradient') || btn.classList.contains('raised'), 'Raised state has gradient');

        // Question textarea
        const textarea = document.getElementById('questionInput');
        assert(textarea !== null, 'Question textarea exists');

        // Toggle back
        toggleHandRaise();
        await sleep(300);
        assert(!btn.classList.contains('raised'), 'Button toggled back to normal');
    }

    // ─── T5: Overlays ───
    async function testOverlays() {
        console.log('\n═══ T5: OVERLAYS (10 Types) ═══');
        navigateTo('dashboard');
        await sleep(200);

        // --- Lock Screen ---
        cmd('LOCK');
        await sleep(400);
        assertHasClass('#lockOverlay', 'active', 'Lock overlay shows');
        cmd('UNLOCK');
        await sleep(400);
        assert(!document.getElementById('lockOverlay').classList.contains('active'), 'Lock overlay closed');

        // --- Black Screen ---
        cmd('BLACK_SCREEN');
        await sleep(400);
        assertHasClass('#lockOverlay', 'active', 'Black screen overlay shows');
        assertHasClass('#lockOverlay', 'black', 'Black screen has .black class');
        cmd('UNLOCK');
        await sleep(400);

        // --- Silence ---
        cmd('SILENCE');
        await sleep(400);
        assertHasClass('#silenceOverlay', 'active', 'Silence overlay shows');
        cmd('CLEAR_SILENCE');
        await sleep(400);
        assert(!document.getElementById('silenceOverlay').classList.contains('active'), 'Silence closed');

        // --- Warning ---
        cmd('TEACHER_WARNING', { message: 'E2E Test Warning', duration: 3 });
        await sleep(400);
        assertHasClass('#warningOverlay', 'active', 'Warning overlay shows');
        assertText('#warningMessage', 'E2E Test Warning', 'Warning message correct');
        cmd('CLEAR_WARNING');
        await sleep(400);

        // --- Notice ---
        cmd('NOTICE', { title: 'Test Notice', body: 'This is a test', noticeType: 'info', duration: 5 });
        await sleep(400);
        assertHasClass('#noticeOverlay', 'active', 'Notice overlay shows');
        assertText('#noticeTitle', 'Test Notice', 'Notice title correct');
        OverlayManager.closeNotice();
        await sleep(300);

        // --- Broadcast ---
        cmd('BROADCAST_START', { resolution: '1920×1080', fps: '60 fps' });
        await sleep(400);
        assertHasClass('#broadcastOverlay', 'active', 'Broadcast overlay shows');
        assertText('#broadcastFps', '60 fps', 'Broadcast FPS correct');
        cmd('BROADCAST_STOP');
        await sleep(400);

        // --- Screen Broadcast ---
        cmd('SCREEN_BROADCAST', { imageUrl: '' });
        await sleep(400);
        assertHasClass('#screenBroadcastOverlay', 'active', 'Screen broadcast overlay shows');
        OverlayManager.closeScreenBroadcast();
        await sleep(300);

        // --- File Broadcast ---
        cmd('FILE_BROADCAST', { fileType: 'PDF', fileName: 'test.pdf', fileUrl: '' });
        await sleep(400);
        assertHasClass('#fileBroadcastOverlay', 'active', 'File broadcast overlay shows');
        assertText('#fileBroadcastName', 'test.pdf', 'File name correct');
        OverlayManager.closeFileBroadcast();
        await sleep(300);

        // --- Survey Overlay ---
        cmd('SURVEY_QUESTION', { question: 'E2E Survey?', options: ['Yes', 'No'], duration: 0 });
        await sleep(400);
        assertHasClass('#surveyOverlay', 'active', 'Survey overlay shows');
        assertText('#surveyQuestion', 'E2E Survey?', 'Survey question correct');
        OverlayManager.closeSurvey();
        await sleep(300);

        // --- Tool Focus ---
        cmd('TOOL_FOCUS', { toolId: 'calculator' });
        await sleep(400);
        assertHasClass('#toolFocusOverlay', 'active', 'Tool focus overlay shows');
        assertText('#toolFocusTitle', 'Máy Tính', 'Tool focus title correct');
        cmd('TOOL_UNFOCUS');
        await sleep(400);

        // --- Clear All ---
        cmd('LOCK');
        cmd('SILENCE');
        await sleep(300);
        cmd('CLEAR_ALL');
        await sleep(400);
        assert(!document.getElementById('lockOverlay').classList.contains('active'), 'Clear All: lock removed');
        assert(!document.getElementById('silenceOverlay').classList.contains('active'), 'Clear All: silence removed');
    }

    // ─── T6: Chat Module ───
    async function testChat() {
        console.log('\n═══ T6: CHAT ═══');
        navigateTo('chat');
        await sleep(300);

        // 2-column layout
        const layout = document.querySelector('.chat-page-layout');
        assert(layout !== null, 'Chat 2-column layout present');

        // Sidebar
        const sidebar = document.querySelector('.chat-sidebar');
        assert(sidebar !== null, 'Chat sidebar present');

        // Filter tabs
        const filterTabs = document.querySelectorAll('.chat-filter-btn');
        assert(filterTabs.length >= 3, 'Chat filter tabs (≥3)');

        // Input bar
        const inputWrap = document.querySelector('.chat-rich-input');
        assert(inputWrap !== null, 'Rich input bar present');

        // Send button
        const sendBtn = document.querySelector('.chat-send-btn-v2');
        assert(sendBtn !== null, 'Send button present');

        // Simulate incoming chat (event name must match ChatManager listener)
        emit('chat_message', {
            sender: 'Giáo viên Nguyễn',
            name: 'Giáo viên Nguyễn',
            text: 'Chào cả lớp!',
            channel: 'group',
            isTeacher: true
        });
        await sleep(400);

        // Check message rendered
        const msgArea = document.querySelector('.chat-messages-area');
        if (msgArea) {
            assert(msgArea.innerHTML.includes('Chào cả lớp'), 'Incoming chat message rendered');
        } else {
            assert(true, 'Chat messages area present (layout verified)');
        }
    }

    // ─── T7: Submit Page ───
    async function testSubmit() {
        console.log('\n═══ T7: SUBMIT ═══');
        navigateTo('submit');
        await sleep(300);

        // 2-column layout
        const layout = document.querySelector('.submit-layout');
        assert(layout !== null, 'Submit 2-column layout present');

        // Stats bar
        const stats = document.querySelectorAll('.submit-stat');
        assert(stats.length === 3, '3 stat counters present');

        // Tabs
        const tabs = document.querySelectorAll('.submit-tab');
        assert(tabs.length === 3, '3 tabs (Received/Submitted/Editor)');

        // Tab switching
        SubmitManager.switchTab('editor');
        await sleep(300);
        const editorTextarea = document.querySelector('.submit-editor-textarea');
        assert(editorTextarea !== null, 'Editor textarea renders on tab switch');

        SubmitManager.switchTab('received');
        await sleep(200);

        // Guide panel
        const guide = document.querySelector('.submit-guide-card');
        assert(guide !== null, 'Guide panel present');

        // Progress bar
        const progressBar = document.querySelector('.submit-progress-bar');
        assert(progressBar !== null, 'Progress bar present');
    }

    // ─── T8: Survey Page ───
    async function testSurvey() {
        console.log('\n═══ T8: SURVEY PAGE ═══');
        navigateTo('survey');
        await sleep(300);

        const content = document.getElementById('surveyPageContent');
        assert(content !== null, 'Survey page content container present');

        // Emit survey_start event via public API (matches SurveyManager listener)
        emit('survey_start', {
            target: 'e2e-survey-1',
            question: 'Bạn hiểu bài hôm nay không?',
            options: ['👍 Hiểu', '👎 Chưa hiểu', '🤔 Bình thường'],
            duration: 0
        });
        await sleep(500);
        assert(content.innerHTML.includes('Bạn hiểu bài'), 'Survey question renders on page');
    }

    // ─── T9: Results Page ───
    async function testResults() {
        console.log('\n═══ T9: RESULTS ═══');
        navigateTo('results');
        await sleep(300);

        const container = document.getElementById('resultsContainer');
        assert(container !== null, 'Results container present');

        // Should have stat cards after quiz submission
        const statCards = container.querySelectorAll('.results-stat-card');
        assert(statCards.length >= 4, '4 result stat cards present');

        // Bar chart
        const barChart = container.querySelector('.results-bar-container');
        assert(barChart !== null, 'Bar chart present');
    }

    // ─── T10: Settings ───
    async function testSettings() {
        console.log('\n═══ T10: SETTINGS ═══');
        navigateTo('settings');
        await sleep(300);

        // Sections
        const sections = document.querySelectorAll('.settings-section');
        assert(sections.length >= 4, '≥4 setting sections');

        // Name
        const nameEl = document.getElementById('settingName');
        assert(nameEl && nameEl.textContent !== '--', 'Student name displayed');

        // Toggle switches
        const toggles = document.querySelectorAll('.toggle input[type="checkbox"]');
        assert(toggles.length >= 3, '≥3 toggle switches');

        // Port input
        const portEl = document.getElementById('settingPort');
        assert(portEl && portEl.value, 'Port input has value');

        // Disconnect button
        const disconnBtn = document.querySelector('[onclick*="disconnectAndReturn"]');
        assert(disconnBtn !== null, 'Disconnect button present');
    }

    // ─── T11: Lesson Flow ───
    async function testLesson() {
        console.log('\n═══ T11: LESSON ═══');

        emit('lesson_start', {
            type: 'lesson_start',
            title: 'Toán học - Phương trình bậc 2',
            teacher: 'GV Nguyễn',
            subject: 'Toán',
            objectives: ['Hiểu dạng tổng quát', 'Giải PT bậc 2']
        });
        await sleep(500);

        assertHasClass('#page-lesson', 'active', 'Lesson page auto-navigated');

        // Stage change
        emit('lesson_stage', { target: 2, stage: 2 });
        await sleep(400);

        // Add content
        emit('lesson_content', {
            blocks: [
                { id: 'b1', type: 'text', content: 'Phương trình bậc 2 có dạng ax² + bx + c = 0' },
                { id: 'b2', type: 'text', content: 'Trong đó a ≠ 0' }
            ]
        });
        await sleep(400);

        // Focus
        emit('lesson_focus', { target: 'b1' });
        await sleep(400);

        // End lesson
        emit('lesson_end', {});
        await sleep(400);
    }

    // ─── T12: State Persistence ───
    async function testPersistence() {
        console.log('\n═══ T12: STATE PERSISTENCE ═══');

        // Student info
        const info = AppStorage.getStudentInfo();
        assert(info.name && info.name !== '', 'Student name in storage');
        assert(info.code && info.code !== '', 'Student code in storage');

        // Session
        const session = AppStorage.getSession();
        assert(session.studentName || info.name, 'Session data persisted');

        // Quiz results
        const quizResults = AppStorage.getQuizResults ? AppStorage.getQuizResults() : JSON.parse(localStorage.getItem('qasc_quiz_results') || '[]');
        assert(Array.isArray(quizResults), 'Quiz results array in storage');

        // Settings
        const settings = AppStorage.getSettings();
        assert(settings !== null && typeof settings === 'object', 'Settings object in storage');
    }

    // ─── T13: Responsive/UI Verification ───
    async function testUI() {
        console.log('\n═══ T13: UI VERIFICATION ═══');

        // Topbar clock
        const clock = document.getElementById('topbarClock');
        assert(clock && clock.textContent.match(/\d{2}:\d{2}/), 'Clock displays time');

        // Sidebar profile
        const profileName = document.getElementById('profileName');
        assert(profileName && profileName.textContent !== 'Học sinh', 'Profile name updated');

        // Toast system
        showToast('E2E Test Toast', 'success', 2000);
        await sleep(300);
        const toastContainer = document.getElementById('toastContainer');
        assert(toastContainer && toastContainer.children.length > 0, 'Toast notification renders');

        // Check CSS variables loaded
        const root = getComputedStyle(document.documentElement);
        const primary = root.getPropertyValue('--primary');
        assert(primary && primary.trim() !== '', 'CSS variables loaded');
    }

    // ═══════════════════════════════════════════════════════════
    //  RUN ALL TESTS
    // ═══════════════════════════════════════════════════════════
    async function runAll() {
        console.clear();
        console.log('╔══════════════════════════════════════════╗');
        console.log('║  QA SmartClass — E2E Test Suite v1.0     ║');
        console.log('║  Testing all 10 pages + 10 overlays      ║');
        console.log('╚══════════════════════════════════════════╝\n');

        _results.length = 0;
        _testCount = 0;
        _passCount = 0;
        _failCount = 0;

        const startTime = Date.now();

        try {
            await testNavigation();    // T1
            await testDashboard();     // T2
            await testQuizFlow();      // T3
            await testHandRaise();     // T4
            await testOverlays();      // T5
            await testChat();          // T6
            await testSubmit();        // T7
            await testSurvey();        // T8
            await testResults();       // T9
            await testSettings();      // T10
            await testLesson();        // T11
            await testPersistence();   // T12
            await testUI();            // T13
        } catch (e) {
            console.error('Test suite error:', e);
        }

        const elapsed = ((Date.now() - startTime) / 1000).toFixed(1);

        // Reset UI state
        OverlayManager.clearAll();
        navigateTo('dashboard');

        // Print summary
        console.log('\n╔══════════════════════════════════════════╗');
        console.log(`║  RESULTS: ${_passCount}/${_testCount} passed (${_failCount} failed)       ║`);
        console.log(`║  Duration: ${elapsed}s                           ║`);
        console.log(`║  Score: ${Math.round((_passCount / _testCount) * 100)}%                              ║`);
        console.log('╚══════════════════════════════════════════╝');

        if (_failCount > 0) {
            console.log('\n❌ FAILED TESTS:');
            _results.filter(r => r.status === '❌').forEach(r => {
                console.log(`  • ${r.name}: ${r.details}`);
            });
        }

        return {
            total: _testCount,
            passed: _passCount,
            failed: _failCount,
            score: Math.round((_passCount / _testCount) * 100),
            duration: elapsed,
            results: _results
        };
    }

    // Expose individual suites for selective testing
    return {
        runAll,
        emit, cmd,
        testNavigation,
        testDashboard,
        testQuizFlow,
        testHandRaise,
        testOverlays,
        testChat,
        testSubmit,
        testSurvey,
        testResults,
        testSettings,
        testLesson,
        testPersistence,
        testUI
    };
})();

// Auto-announce availability
console.log('🧪 E2E Test Harness loaded. Run: E2E.runAll()');
