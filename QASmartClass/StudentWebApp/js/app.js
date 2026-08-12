/* ════════════════════════════════════════════════════════
   QA SmartClass — Main App Controller
   ════════════════════════════════════════════════════════ */

// ─── State ───
let currentPage = 'dashboard';
let isHandRaised = false;
let unreadMessages = 0;
let isWebBlocked = false;
let isWebWhitelistActive = false;
let allowedWebUrls = [];
let isTeacherBroadcasting = false;
let currentBroadcastImageUrl = '';
let whiteboardStrokes = [];
let whiteboardShapes = [];
let whiteboardTexts = [];
let whiteboardZoomPan = null;
let broadcastZoomPan = null;

// ═══════════════════════════════════════════════════════
// BOARD INTERACTIVE MODE — Vẽ/Tẩy trực tiếp trên Board
// Reference: eraser_tool_specification.md v2.1 Mục V.5
// ═══════════════════════════════════════════════════════
let boardInteractiveMode = false;  // true khi Board được phép vẽ/tẩy
let boardCurrentTool = 'pen';      // 'pen' | 'eraser'
let boardPenColor = '#ffffff';
let boardPenWidth = 3;
let boardEraserSize = 30;
let boardActivePointers = {};      // Track multi-touch: {pointerId: {startX, startY, canvas, ...}}

// ════════════════════════════════════════════════════════
//  OVERLAY MANAGER — Central controller for all fullscreen overlays
// ════════════════════════════════════════════════════════
const OverlayManager = (() => {
    let _warningTimer = null;
    let _noticeTimer = null;
    let _surveyTimer = null;
    let _clockTimer = null;

    function _updateClock(elId) {
        const el = document.getElementById(elId);
        if (el) {
            const now = new Date();
            el.textContent = `ðŸ• ${String(now.getHours()).padStart(2,'0')}:${String(now.getMinutes()).padStart(2,'0')}`;
        }
    }

    function _startClockUpdates() {
        _stopClockUpdates();
        _updateClock('lockClock');
        _updateClock('silenceClock');
        _clockTimer = setInterval(() => {
            _updateClock('lockClock');
            _updateClock('silenceClock');
        }, 30000);
    }

    function _stopClockUpdates() {
        if (_clockTimer) { clearInterval(_clockTimer); _clockTimer = null; }
    }

    // â”€â”€â”€ LOCK SCREEN â”€â”€â”€
    function showLockScreen(data) {
        const overlay = document.getElementById('lockOverlay');
        const icon = document.getElementById('lockIcon');
        const text = document.getElementById('lockText');
        const sub = document.getElementById('lockSubtext');
        if (data.type === 'black') {
            overlay.classList.add('black');
            icon.textContent = 'ðŸ“´';
        } else {
            overlay.classList.remove('black');
            icon.textContent = 'ðŸ”’';
        }
        text.textContent = data.title || 'MÃ n hÃ¬nh Ä‘Ã£ bá»‹ khÃ³a';
        sub.textContent = data.subtitle || 'GV Ä‘Ã£ khÃ³a mÃ n hÃ¬nh';
        overlay.classList.add('active');
        _startClockUpdates();
    }

    function closeLockScreen() {
        document.getElementById('lockOverlay').classList.remove('active', 'black');
    }

    // â”€â”€â”€ SILENCE â”€â”€â”€
    function showSilence() {
        closeLockScreen();
        closeWarning();
        document.getElementById('silenceOverlay').classList.add('active');
        _startClockUpdates();
    }

    function closeSilence() {
        document.getElementById('silenceOverlay').classList.remove('active');
    }

    // â”€â”€â”€ NOTICE â”€â”€â”€
    function showNotice(data) {
        const badge = document.getElementById('noticeBadge');
        const typeLabels = { info: 'ðŸ“¢ THÃ”NG BÃO', warning: 'âš ï¸ Cáº¢NH BÃO', urgent: 'ðŸš¨ KHáº¨N Cáº¤P', celebrate: 'ðŸŽ‰ CHÃšC Má»ªNG' };
        badge.textContent = typeLabels[data.noticeType] || typeLabels.info;
        badge.className = 'notice-badge' + (data.noticeType !== 'info' ? ` ${data.noticeType}` : '');
        document.getElementById('noticeTitle').textContent = data.title || '';
        document.getElementById('noticeBody').textContent = data.body || '';
        document.getElementById('noticeOverlay').classList.add('active');

        if (_noticeTimer) clearTimeout(_noticeTimer);
        if (data.duration > 0) {
            _noticeTimer = setTimeout(() => closeNotice(), data.duration * 1000);
        }
    }

    function closeNotice() {
        if (_noticeTimer) { clearTimeout(_noticeTimer); _noticeTimer = null; }
        document.getElementById('noticeOverlay').classList.remove('active');
    }

    // â”€â”€â”€ TEACHER WARNING â”€â”€â”€
    function showWarning(data) {
        closeWarning();
        document.getElementById('warningMessage').textContent = data.message || '';
        const countdownEl = document.getElementById('warningCountdown');
        document.getElementById('warningOverlay').classList.add('active');

        if (data.duration > 0) {
            let remaining = data.duration;
            countdownEl.textContent = `Tá»± Ä‘Ã³ng sau ${remaining} giÃ¢y...`;
            _warningTimer = setInterval(() => {
                remaining--;
                if (remaining <= 0) {
                    closeWarning();
                } else {
                    countdownEl.textContent = `Tá»± Ä‘Ã³ng sau ${remaining} giÃ¢y...`;
                }
            }, 1000);
        } else {
            countdownEl.textContent = 'GV sáº½ táº¯t khi sáºµn sÃ ng';
        }
    }

    function closeWarning() {
        if (_warningTimer) { clearInterval(_warningTimer); _warningTimer = null; }
        document.getElementById('warningOverlay').classList.remove('active');
    }

    // â”€â”€â”€ BROADCAST (Screen Cast) â”€â”€â”€
    function showBroadcast(data) {
        document.getElementById('broadcastRes').textContent = `ðŸ–¥ï¸ ${data.resolution || '1920Ã—1080'}`;
        document.getElementById('broadcastFps').textContent = `ðŸŽ¬ ${data.fps || '30 fps'}`;
        const now = new Date();
        document.getElementById('broadcastTime').textContent = `ðŸ• ${String(now.getHours()).padStart(2,'0')}:${String(now.getMinutes()).padStart(2,'0')}`;
        document.getElementById('broadcastOverlay').classList.add('active');
    }

    function closeBroadcast() {
        document.getElementById('broadcastOverlay').classList.remove('active');
    }

    // â”€â”€â”€ SURVEY â”€â”€â”€
    function showSurvey(data) {
        closeSurvey();
        document.getElementById('surveyQuestion').textContent = data.question || '';
        const optContainer = document.getElementById('surveyOptions');
        optContainer.innerHTML = '';

        (data.options || []).forEach((opt, idx) => {
            const btn = document.createElement('button');
            btn.className = 'survey-option-btn';
            btn.textContent = opt;
            btn.onclick = () => _selectSurveyOption(btn, idx, data.question);
            optContainer.appendChild(btn);
        });

        const countdownEl = document.getElementById('surveyCountdown');
        if (data.duration > 0) {
            let remaining = data.duration;
            countdownEl.textContent = `â° CÃ²n ${remaining} giÃ¢y`;
            _surveyTimer = setInterval(() => {
                remaining--;
                if (remaining <= 0) {
                    closeSurvey();
                    showToast('⌛ Hết giờ khảo sát', 'warning');
                } else {
                    countdownEl.textContent = `⌛ Còn ${remaining} giây`;
                }
            }, 1000);
        } else {
            countdownEl.textContent = '';
        }

        document.getElementById('surveyOverlay').classList.add('active');
    }

    function _selectSurveyOption(btn, idx, question) {
        document.querySelectorAll('.survey-option-btn').forEach(b => b.classList.remove('selected'));
        btn.classList.add('selected');
        StudentConnection.send({ type: 'survey_answer', questionText: question, answerIndex: idx, answerText: btn.textContent });
        showToast('✅ Đã gửi câu trả lời!', 'success', 2000);
        setTimeout(() => closeSurvey(), 1500);
    }

    function closeSurvey() {
        if (_surveyTimer) { clearInterval(_surveyTimer); _surveyTimer = null; }
        document.getElementById('surveyOverlay').classList.remove('active');
    }

    // ─── SCREEN BROADCAST (Whiteboard Image) ───
    function showScreenBroadcast(data) {
        isTeacherBroadcasting = true;
        currentBroadcastImageUrl = data.imageUrl || '';

        // Hiển thị badge LIVE trên menu
        const badge = document.getElementById('whiteboardLiveBadge');
        if (badge) badge.classList.remove('hidden');

        // Cập nhật trang Bảng trắng nếu đang xem
        if (currentPage === 'whiteboard') {
            renderWhiteboardPage();
        }

        const overlay = document.getElementById('screenBroadcastOverlay');
        const wrap = document.getElementById('screenBroadcastWrap');
        const placeholder = document.getElementById('screenBroadcastPlaceholder');
        const closeBtn = document.getElementById('screenBroadcastClose');

        if (closeBtn) {
            if (data.isForce) {
                closeBtn.style.display = 'none';
            } else {
                closeBtn.style.display = 'flex';
            }
        }

        if (data.imageUrl) {
            if (placeholder) placeholder.style.display = 'none';
            let img = wrap.querySelector('img');
            if (!img) {
                img = document.createElement('img');
                img.alt = 'Bảng GV';
                wrap.appendChild(img);
            }
            img.onload = function() {
                resizeAndRedrawCanvas('screenBroadcastCanvas', img);
            };
            img.src = data.imageUrl;
            if (img.complete) {
                resizeAndRedrawCanvas('screenBroadcastCanvas', img);
            }
        } else {
            if (placeholder) placeholder.style.display = 'flex';
            let img = wrap.querySelector('img');
            if (img) img.remove();
        }

        overlay.classList.add('active');
    }

    function updateScreenBroadcast(data) {
        currentBroadcastImageUrl = data.imageUrl || '';

        if (currentPage === 'whiteboard') {
            renderWhiteboardPage();
        }

        const wrap = document.getElementById('screenBroadcastWrap');
        const placeholder = document.getElementById('screenBroadcastPlaceholder');
        
        if (data.imageUrl) {
            if (placeholder) placeholder.style.display = 'none';
            let img = wrap.querySelector('img');
            if (!img) {
                img = document.createElement('img');
                img.alt = 'Bảng GV';
                wrap.appendChild(img);
            }
            img.onload = function() {
                resizeAndRedrawCanvas('screenBroadcastCanvas', img);
            };
            img.src = data.imageUrl;
            if (img.complete) {
                resizeAndRedrawCanvas('screenBroadcastCanvas', img);
            }
        }
    }

    function closeScreenBroadcast() {
        const overlay = document.getElementById('screenBroadcastOverlay');
        overlay.classList.remove('active');
        
        const wrap = document.getElementById('screenBroadcastWrap');
        const img = wrap.querySelector('img');
        if (img) img.remove();
        const placeholder = document.getElementById('screenBroadcastPlaceholder');
        if (placeholder) placeholder.style.display = 'flex';
    }

    function stopScreenBroadcast() {
        isTeacherBroadcasting = false;
        currentBroadcastImageUrl = '';

        const badge = document.getElementById('whiteboardLiveBadge');
        if (badge) badge.classList.add('hidden');

        if (currentPage === 'whiteboard') {
            renderWhiteboardPage();
        }

        closeScreenBroadcast();
    }

    // â”€â”€â”€ FILE BROADCAST â”€â”€â”€
    const _fileTypeIcons = {
        IMAGE: 'ðŸ–¼ï¸', VIDEO: 'ðŸŽ¬', PDF: 'ðŸ“„', AUDIO: 'ðŸŽµ',
        DOCUMENT: 'ðŸ“', SPREADSHEET: 'ðŸ“Š', FILE: 'ðŸ“'
    };

    function showFileBroadcast(data) {
        const overlay = document.getElementById('fileBroadcastOverlay');
        const badge = document.getElementById('fileBroadcastBadge');
        const nameEl = document.getElementById('fileBroadcastName');
        const content = document.getElementById('fileBroadcastContent');

        const ftype = (data.fileType || 'FILE').toUpperCase();
        const icon = _fileTypeIcons[ftype] || 'ðŸ“';
        badge.textContent = `${icon} ${ftype}`;
        nameEl.textContent = data.fileName || 'File tá»« GV';

        // Render content based on file type
        content.innerHTML = '';
        const url = data.fileUrl || '';

        if (!url) {
            content.innerHTML = '<div class="file-broadcast-placeholder"><div class="fb-icon">ðŸ“‚</div><div class="fb-text">KhÃ´ng cÃ³ file URL</div></div>';
        } else if (ftype === 'IMAGE' || /\.(png|jpg|jpeg|gif|bmp|webp|svg)$/i.test(url)) {
            const img = document.createElement('img');
            img.src = url;
            img.alt = data.fileName || 'Image';
            content.appendChild(img);
        } else if (ftype === 'VIDEO' || /\.(mp4|webm|ogg)$/i.test(url)) {
            const video = document.createElement('video');
            video.src = url;
            video.controls = true;
            video.autoplay = true;
            video.style.maxWidth = '100%';
            video.style.maxHeight = '100%';
            content.appendChild(video);
        } else if (ftype === 'PDF' || /\.pdf$/i.test(url)) {
            const iframe = document.createElement('iframe');
            iframe.src = url;
            iframe.title = data.fileName || 'PDF';
            content.appendChild(iframe);
        } else if (ftype === 'AUDIO' || /\.(mp3|wav|ogg|aac)$/i.test(url)) {
            const audio = document.createElement('audio');
            audio.src = url;
            audio.controls = true;
            audio.autoplay = true;
            content.appendChild(audio);
        } else {
            content.innerHTML = `<div class="file-broadcast-placeholder"><div class="fb-icon">${icon}</div><div class="fb-text">${data.fileName || 'File'}</div></div>`;
        }

        overlay.classList.add('active');
    }

    function closeFileBroadcast() {
        const overlay = document.getElementById('fileBroadcastOverlay');
        overlay.classList.remove('active');
        // Stop any playing media
        const content = document.getElementById('fileBroadcastContent');
        const video = content.querySelector('video');
        const audio = content.querySelector('audio');
        if (video) video.pause();
        if (audio) audio.pause();
    }

    function focusFilePage(page) {
        const content = document.getElementById('fileBroadcastContent');
        if (!content) return;
        // PDF iframe: append #page=N
        const iframe = content.querySelector('iframe');
        if (iframe && iframe.src) {
            const base = iframe.src.replace(/#.*$/, '');
            iframe.src = `${base}#page=${page}`;
        }
        // Show page indicator toast
        const badge = document.getElementById('fileBroadcastBadge');
        if (badge) badge.textContent = `ðŸ“„ Trang ${page}`;
    }

    // â”€â”€â”€ TOOL FOCUS â”€â”€â”€
    const _toolMeta = {
        multiplication: { icon: 'âœ–ï¸', name: 'Báº£ng Cá»­u ChÆ°Æ¡ng', desc: 'CÃ´ng cá»¥ há»c báº£ng nhÃ¢n' },
        trigonometry: { icon: 'ðŸ“', name: 'Báº£ng LÆ°á»£ng GiÃ¡c', desc: 'Báº£ng lÆ°á»£ng giÃ¡c Ä‘áº§y Ä‘á»§' },
        periodic_table: { icon: 'âš›ï¸', name: 'Báº£ng Tuáº§n HoÃ n', desc: 'Báº£ng tuáº§n hoÃ n cÃ¡c nguyÃªn tá»‘' },
        number_base: { icon: 'ðŸ”¢', name: 'Chuyá»ƒn Äá»•i Há»‡ Sá»‘', desc: 'Chuyá»ƒn Ä‘á»•i há»‡ cÆ¡ sá»‘' },
        sequence: { icon: 'ðŸ“', name: 'DÃ£y Sá»‘', desc: 'CÃ´ng cá»¥ dÃ£y sá»‘' },
        unit_converter: { icon: 'ðŸ“', name: 'Äá»•i ÄÆ¡n Vá»‹', desc: 'Chuyá»ƒn Ä‘á»•i Ä‘Æ¡n vá»‹ Ä‘o lÆ°á»ng' },
        Graph: { icon: 'ðŸ“ˆ', name: 'Äá»“ Thá»‹ Graph', desc: 'MÃ¡y tÃ­nh Ä‘á»“ thá»‹ Graph' },
        calculator: { icon: 'ðŸ§®', name: 'MÃ¡y TÃ­nh', desc: 'MÃ¡y tÃ­nh khoa há»c' },
        geometry: { icon: 'ðŸ“', name: 'HÃ¬nh Há»c', desc: 'CÃ´ng cá»¥ hÃ¬nh há»c' },
        logarithm: { icon: 'ðŸ“Š', name: 'Logarithm & LÅ©y thá»«a', desc: 'CÃ´ng cá»¥ logarithm' },
        prime_numbers: { icon: 'ðŸ”¢', name: 'Sá»‘ NguyÃªn Tá»‘', desc: 'Kiá»ƒm tra vÃ  tÃ¬m sá»‘ nguyÃªn tá»‘' },
        identities: { icon: 'ðŸ“', name: 'Háº±ng Äáº³ng Thá»©c', desc: 'Háº±ng Ä‘áº³ng thá»©c Ä‘Ã¡ng nhá»›' },
        constants: { icon: 'âš¡', name: 'Háº±ng Sá»‘ Váº­t LÃ½', desc: 'CÃ¡c háº±ng sá»‘ váº­t lÃ½ cÆ¡ báº£n' },
        ph_scale: { icon: 'ðŸ§ª', name: 'Thang pH', desc: 'Thang Ä‘o pH axit-bazÆ¡' },
        density: { icon: 'âš–ï¸', name: 'Khá»‘i LÆ°á»£ng RiÃªng', desc: 'Báº£ng khá»‘i lÆ°á»£ng riÃªng' },
        wave_speed: { icon: 'ðŸŒŠ', name: 'Tá»‘c Äá»™ SÃ³ng', desc: 'CÃ´ng cá»¥ tÃ­nh tá»‘c Ä‘á»™ sÃ³ng' },
        boiling_freezing: { icon: 'ðŸŒ¡ï¸', name: 'Nhiá»‡t Äá»™ SÃ´i/ÄÃ´ng', desc: 'Báº£ng nhiá»‡t Ä‘á»™ sÃ´i vÃ  Ä‘Ã´ng Ä‘áº·c' },
        irregular_verbs: { icon: 'ðŸ‡¬ðŸ‡§', name: 'Äá»™ng Tá»« Báº¥t Quy Táº¯c', desc: 'Báº£ng Ä‘á»™ng tá»« báº¥t quy táº¯c' },
        vocabulary: { icon: 'ðŸ“–', name: 'Tá»« Vá»±ng Theo Chá»§ Äá»', desc: 'Tá»« vá»±ng theo chá»§ Ä‘á»' },
        grammar: { icon: 'ðŸ“š', name: 'Ngá»¯ PhÃ¡p Tiáº¿ng Anh', desc: 'Ngá»¯ phÃ¡p tiáº¿ng Anh' },
        ipa: { icon: 'ðŸ”¤', name: 'Báº£ng PhiÃªn Ã‚m IPA', desc: 'Báº£ng phiÃªn Ã¢m quá»‘c táº¿' },
        vocab_en: { icon: 'ðŸ‡¬ðŸ‡§', name: 'Tá»« Vá»±ng Anh', desc: 'Há»c tá»« vá»±ng tiáº¿ng Anh' },
        vocab_jp: { icon: 'ðŸ‡¯ðŸ‡µ', name: 'Tá»« Vá»±ng Nháº­t', desc: 'Há»c tá»« vá»±ng tiáº¿ng Nháº­t' },
        vocab_cn: { icon: 'ðŸ‡¨ðŸ‡³', name: 'Tá»« Vá»±ng Trung', desc: 'Há»c tá»« vá»±ng tiáº¿ng Trung' },
        math_symbols: { icon: 'âˆ‘', name: 'KÃ½ Hiá»‡u ToÃ¡n Há»c', desc: 'Báº£ng kÃ½ hiá»‡u toÃ¡n há»c' },
        planets: { icon: 'ðŸª', name: 'HÃ nh Tinh & Vá»‡ Tinh', desc: 'Há»‡ máº·t trá»i' },
        countries: { icon: 'ðŸŒ', name: 'NÆ°á»›c & Thá»§ ÄÃ´', desc: 'Báº£ng quá»‘c gia vÃ  thá»§ Ä‘Ã´' },
        literature: { icon: 'ðŸ“œ', name: 'TÃ¡c Pháº©m VÄƒn Há»c', desc: 'Báº£ng tÃ¡c pháº©m vÄƒn há»c' },
        dynasties: { icon: 'ðŸ›ï¸', name: 'Triá»u Äáº¡i Lá»‹ch Sá»­', desc: 'CÃ¡c triá»u Ä‘áº¡i lá»‹ch sá»­' },
        textbooks: { icon: 'ðŸ“˜', name: 'SÃ¡ch GiÃ¡o Khoa', desc: 'SÃ¡ch giÃ¡o khoa Ä‘iá»‡n tá»­' },
        mental_math: { icon: 'ðŸ§ ', name: 'TÃ­nh Nháº©m Nhanh', desc: 'Luyá»‡n tÃ­nh nháº©m' },
        iq_quiz: { icon: 'ðŸ’¡', name: 'Luyá»‡n IQ & Logic', desc: 'CÃ¢u há»i IQ vÃ  logic' },
        formulas: { icon: 'ðŸ“‹', name: 'Báº£ng CÃ´ng Thá»©c', desc: 'Tá»•ng há»£p cÃ´ng thá»©c' },
        clock: { icon: 'ðŸ•', name: 'Äá»“ng Há»“', desc: 'Xem giá»' },
        stopwatch: { icon: 'â±ï¸', name: 'Báº¥m Giá»', desc: 'Äá»“ng há»“ báº¥m giá»' },
        random_student: { icon: 'ðŸŽ²', name: 'Chá»n Ngáº«u NhiÃªn', desc: 'Chá»n há»c sinh ngáº«u nhiÃªn' },
    };

    let _currentToolFocus = null;

    function showToolFocus(data) {
        const toolId = data.toolId || '';
        const meta = _toolMeta[toolId] || { icon: 'ðŸŽ¯', name: toolId || 'CÃ´ng cá»¥', desc: 'GV yÃªu cáº§u táº­p trung' };

        document.getElementById('toolFocusIcon').textContent = meta.icon;
        document.getElementById('toolFocusTitle').textContent = meta.name;
        document.getElementById('toolFocusSubtitle').textContent = 'GV yÃªu cáº§u táº­p trung â€” HÃ£y quan sÃ¡t!';

        // Render actual tool content (parity with WPF CreateToolControl)
        const body = document.getElementById('toolFocusBody');
        if (typeof ToolsContent !== 'undefined') {
            body.innerHTML = ToolsContent.render(toolId, meta);
        } else {
            body.innerHTML = `<div class="tf-msg">GV Ä‘ang yÃªu cáº§u em táº­p trung vÃ o<br><strong>${meta.name}</strong></div>`;
        }

        _currentToolFocus = toolId;
        document.getElementById('toolFocusOverlay').classList.add('active');
    }

    function showToolSectionFocus(data) {
        // If tool overlay not open, open it first
        if (!document.getElementById('toolFocusOverlay').classList.contains('active')) {
            showToolFocus(data);
        }
        // Highlight specific section within rendered tool
        if (typeof ToolsContent !== 'undefined' && data.sectionId) {
            setTimeout(() => ToolsContent.highlightSection(data.sectionId), 300);
        }
    }

    function closeToolFocus() {
        document.getElementById('toolFocusOverlay').classList.remove('active');
        _currentToolFocus = null;
    }

    // â•â•â• QUIZ REVIEW â•â•â•
    function showQuizReview(data) {
        const overlay = document.getElementById('quizReviewOverlay');
        const header = document.getElementById('quizReviewHeader');
        const body = document.getElementById('quizReviewBody');
        const footer = document.getElementById('quizReviewFooter');
        if (!overlay) return;

        const studentName = data.StudentName || 'HS';
        const totalScore = data.TotalScore || 0;
        const totalPoints = data.TotalPoints || 0;
        const correctCount = data.CorrectCount || 0;
        const questions = data.Questions || [];

        header.innerHTML = `<div>ðŸ“‹ BÃ i lÃ m máº«u â€” ${studentName}</div>
            <button class="overlay-close-btn" onclick="OverlayManager.closeQuizReview()" style="position:static;width:32px;height:32px;font-size:14px">âœ•</button>`;

        let qHtml = '';
        questions.forEach((q, i) => {
            const isCorrect = q.CorrectAnswer === q.StudentAnswer;
            const bgColor = isCorrect ? '#E8F5E9' : '#FFEBEE';
            const borderColor = isCorrect ? '#4CAF50' : '#EF5350';
            const statusTag = isCorrect
                ? '<span style="background:#2E7D32;color:white;padding:2px 8px;border-radius:4px;font-size:10px;font-weight:700">âœ… ÄÃšNG</span>'
                : '<span style="background:#D32F2F;color:white;padding:2px 8px;border-radius:4px;font-size:10px;font-weight:700">âŒ SAI</span>';

            qHtml += `<div class="qr-question" id="qr-q-${i}" style="background:${bgColor};border:2px solid ${borderColor};border-radius:10px;padding:14px;margin-bottom:8px">
                <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:6px">
                    <span style="background:#1976D2;color:white;padding:2px 8px;border-radius:4px;font-size:10px;font-weight:700">CÃ¢u ${q.Number || i+1}</span>
                    ${statusTag}
                </div>
                <div style="font-size:13px;font-weight:600;margin-bottom:6px">${q.Content || ''}</div>
            </div>`;
        });

        body.innerHTML = qHtml;
        footer.innerHTML = `<span style="font-size:11px;color:#757575">GV Ä‘ang trÃ¬nh chiáº¿u â€” Äiá»ƒm: ${totalScore}/${totalPoints} â€” ÄÃºng: ${correctCount} cÃ¢u</span>`;

        overlay.classList.add('active');
    }

    function focusQuizReviewQuestion(idx) {
        const body = document.getElementById('quizReviewBody');
        if (!body) return;
        body.querySelectorAll('.qr-question').forEach((el, i) => {
            if (i + 1 === idx) {
                el.style.transform = 'scale(1.03)';
                el.style.boxShadow = '0 4px 20px rgba(25,118,210,0.4)';
                el.style.opacity = '1';
                el.scrollIntoView({ behavior: 'smooth', block: 'center' });
            } else {
                el.style.transform = '';
                el.style.boxShadow = '';
                el.style.opacity = '0.35';
            }
        });
    }

    function closeQuizReview() {
        const overlay = document.getElementById('quizReviewOverlay');
        if (overlay) overlay.classList.remove('active');
    }

    // ─── CLEAR ALL ───
    function clearAll() {
        closeLockScreen();
        closeSilence();
        closeNotice();
        closeWarning();
        closeBroadcast();
        closeSurvey();
        stopScreenBroadcast();
        closeFileBroadcast();
        closeToolFocus();
        closeQuizReview();
        _stopClockUpdates();
    }

    return {
        showLockScreen, closeLockScreen,
        showSilence, closeSilence,
        showNotice, closeNotice,
        showWarning, closeWarning,
        showBroadcast, closeBroadcast,
        showSurvey, closeSurvey,
        showScreenBroadcast, updateScreenBroadcast, closeScreenBroadcast, stopScreenBroadcast,
        showFileBroadcast, closeFileBroadcast, focusFilePage,
        showToolFocus, showToolSectionFocus, closeToolFocus,
        showQuizReview, focusQuizReviewQuestion, closeQuizReview,
        clearAll
    };
})();

// â•â•â• INIT â•â•â•
document.addEventListener('DOMContentLoaded', () => {
    initApp();
    startClock();
    setupConnection();
});

function initApp() {
    // Load student info
    const info = AppStorage.getStudentInfo();
    if (!info.name) {
        // Not logged in â€” redirect to landing
        window.location.href = 'index.html';
        return;
    }

    // Update UI with student info
    const initials = getInitials(info.name);
    document.getElementById('profileName').textContent = info.name;
    document.getElementById('profileId').textContent = `MÃ£: ${info.code} â€¢ Web`;
    document.getElementById('profileId').textContent = `Mã: ${info.code} • Web`;
    document.getElementById('profileAvatar').textContent = initials;
    document.getElementById('welcomeText').textContent = `Xin chào, ${info.name} 👋`;

    // Dashboard date (matching WPF format)
    const now = new Date();
    const dayNames = ['Chủ nhật', 'Thứ hai', 'Thứ ba', 'Thứ tư', 'Thứ năm', 'Thứ sáu', 'Thứ bảy'];
    const dateStr = `📅 ${dayNames[now.getDay()]}, ${String(now.getDate()).padStart(2,'0')}/${String(now.getMonth()+1).padStart(2,'0')}/${now.getFullYear()}    •    Trường THPT QA`;
    const dashDate = document.getElementById('dashDate');
    if (dashDate) dashDate.textContent = dateStr;

    // Settings page — populate input value
    const nameInput = document.getElementById('settingName');
    if (nameInput) nameInput.value = info.name || '';
    document.getElementById('settingCode').textContent = info.code || '--';
    document.getElementById('settingIP').textContent = info.serverIP || '--';
    document.getElementById('settingDevice').textContent = info.deviceName || getDeviceType();

    // Load settings
    const settings = AppStorage.getSettings();
    document.getElementById('settingSound').checked = settings.soundEnabled;
    document.getElementById('settingNotif').checked = settings.notificationsEnabled;

    // Restore new settings fields
    const portEl = document.getElementById('settingPort');
    if (portEl) portEl.value = settings.port || 9000;
    const autoReconnEl = document.getElementById('settingAutoReconnect');
    if (autoReconnEl) autoReconnEl.checked = settings.autoReconnect !== false;
    const fontSizeEl = document.getElementById('settingFontSize');
    if (fontSizeEl) fontSizeEl.value = settings.fontSize || 'normal';

    // Apply font size
    _applyFontSize(settings.fontSize || 'normal');

    // Load chat history
    loadChatHistory();

    // Initialize Zoom & Pan
    setTimeout(() => {
        const whiteboardViewport = document.querySelector('.whiteboard-page-img-wrap');
        const whiteboardContainer = document.getElementById('whiteboardPageContainer');
        if (whiteboardViewport && whiteboardContainer) {
            whiteboardZoomPan = new ZoomPan(whiteboardViewport, whiteboardContainer);
        }
        
        const broadcastViewport = document.getElementById('screenBroadcastWrap');
        const broadcastContainer = document.getElementById('screenBroadcastContainer');
        if (broadcastViewport && broadcastContainer) {
            broadcastZoomPan = new ZoomPan(broadcastViewport, broadcastContainer);
        }

        const wbCanvas = document.getElementById('whiteboardPageCanvas');
        if (wbCanvas) enableBoardInteractive(wbCanvas);

        const sbCanvas = document.getElementById('screenBroadcastCanvas');
        if (sbCanvas) enableBoardInteractive(sbCanvas);
    }, 100);
}

// â•â•â• NAVIGATION â•â•â•
function navigateTo(pageId) {
    // Hide all pages
    document.querySelectorAll('.page').forEach(p => {
        p.classList.remove('active');
    });

    // Show target page
    const page = document.getElementById(`page-${pageId}`);
    if (page) {
        page.classList.add('active');
        currentPage = pageId;
        // Persist current page for refresh recovery
        AppStorage.saveCurrentPage(pageId);
    }

    // Update nav buttons
    document.querySelectorAll('.nav-btn').forEach(btn => {
        btn.classList.toggle('active', btn.dataset.page === pageId);
    });

    // Update topbar title
    const titles = {
        dashboard: 'ðŸ  Trang chá»§',
        lesson: 'ðŸ“– BÃ i giáº£ng hÃ´m nay',
        quiz: 'ðŸ“ Kiá»ƒm tra / Quiz',
        submit: 'ðŸ“¤ BÃ i táº­p / Ná»™p bÃ i',
        survey: 'ðŸ“‹ Kháº£o sÃ¡t',
        chat: 'ðŸ’¬ Tin nháº¯n',
        handraise: 'âœ‹ GiÆ¡ tay / Há»i GV',
        gamification: 'ðŸ† XP & Huy Hiá»‡u',
        results: 'ðŸ“Š Káº¿t quáº£ há»c táº­p',
        settings: 'âš™ï¸ CÃ i Ä‘áº·t'
    };
    titles.whiteboard = '📡 Bảng giáo viên';
    document.getElementById('topbarTitle').textContent = titles[pageId] || pageId;

    // Reset chat badge when viewing chat
    if (pageId === 'chat') {
        unreadMessages = 0;
        updateChatBadge();
    }

    // Trigger module renders on page entry
    if (pageId === 'whiteboard') {
        renderWhiteboardPage();
    }
    if (pageId === 'submit' && typeof SubmitManager !== 'undefined') {
        SubmitManager.switchTab(SubmitManager._activeTab || 'received');
    }
    if (pageId === 'results' && typeof ResultsManager !== 'undefined') {
        ResultsManager.renderResults();
    }
    if (pageId === 'gamification' && typeof GamificationUI !== 'undefined') {
        GamificationUI.renderPage();
    }

    // Close mobile sidebar
    closeSidebar();
}

function renderWhiteboardPage() {
    const activeEl = document.getElementById('whiteboardPageActive');
    const emptyEl = document.getElementById('whiteboardPageEmpty');
    const imgEl = document.getElementById('whiteboardPageImg');
    const timeEl = document.getElementById('whiteboardPageTime');

    if (isTeacherBroadcasting && currentBroadcastImageUrl) {
        if (activeEl) {
            activeEl.style.setProperty('display', 'flex', 'important');
            activeEl.classList.remove('hidden');
        }
        if (emptyEl) emptyEl.classList.add('hidden');
        if (imgEl) {
            imgEl.onload = function() {
                resizeAndRedrawCanvas('whiteboardPageCanvas', imgEl);
            };
            imgEl.src = currentBroadcastImageUrl;
            if (imgEl.complete) {
                resizeAndRedrawCanvas('whiteboardPageCanvas', imgEl);
            }
        }
        if (timeEl) {
            const now = new Date();
            timeEl.textContent = `⌛ Cập nhật: ${String(now.getHours()).padStart(2,'0')}:${String(now.getMinutes()).padStart(2,'0')}:${String(now.getSeconds()).padStart(2,'0')}`;
        }
    } else {
        if (activeEl) {
            activeEl.classList.add('hidden');
            activeEl.style.display = 'none';
        }
        if (emptyEl) emptyEl.classList.remove('hidden');
    }
}

function resizeAndRedrawCanvas(canvasId, imgOrId) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;
    const img = typeof imgOrId === 'string' ? document.getElementById(imgOrId) : imgOrId;
    if (!img) return;

    if (img.clientWidth > 0 && img.clientHeight > 0) {
        canvas.style.top = img.offsetTop + 'px';
        canvas.style.left = img.offsetLeft + 'px';
        canvas.style.width = img.clientWidth + 'px';
        canvas.style.height = img.clientHeight + 'px';
        canvas.width = img.clientWidth;
        canvas.height = img.clientHeight;

        redrawStrokes(canvas);
    }
}

function redrawStrokes(canvas) {
    const ctx = canvas.getContext('2d');
    ctx.clearRect(0, 0, canvas.width, canvas.height);

    for (const stroke of whiteboardStrokes) {
        drawStrokeOnCanvas(canvas, stroke);
    }
    
    whiteboardShapes.forEach(function(s) { drawShapeOnCanvas(canvas, s); });
    whiteboardTexts.forEach(function(t) { drawTextOnCanvas(canvas, t); });
}

function convertWpfColorToCss(wpfColor) {
    if (!wpfColor) return '#ffffff';
    let a = 1.0;
    let r = 255, g = 255, b = 255;
    
    if (wpfColor.startsWith('#') && wpfColor.length === 9) {
        a = parseInt(wpfColor.substring(1, 3), 16) / 255;
        r = parseInt(wpfColor.substring(3, 5), 16);
        g = parseInt(wpfColor.substring(5, 7), 16);
        b = parseInt(wpfColor.substring(7, 9), 16);
    } else if (wpfColor.startsWith('#') && wpfColor.length === 7) {
        r = parseInt(wpfColor.substring(1, 3), 16);
        g = parseInt(wpfColor.substring(3, 5), 16);
        b = parseInt(wpfColor.substring(5, 7), 16);
    } else {
        const lower = wpfColor.toLowerCase();
        if (lower === 'black' || lower === '#000000' || lower === '#000') {
            return '#fafafa';
        }
        return wpfColor;
    }
    
    // Tính toán độ sáng (Luminance) theo công thức Y.Q
    const luminance = 0.299 * r + 0.587 * g + 0.114 * b;
    // Nếu quá tối, đổi sang màu sáng tương phản cao trên nền tối
    if (luminance < 50) {
        return `rgba(250, 250, 250, ${a.toFixed(2)})`;
    }
    
    return `rgba(${r}, ${g}, ${b}, ${a.toFixed(2)})`;
}

function drawStrokeOnCanvas(canvas, stroke) {
    if (!stroke.points) return;
    const ptPairs = stroke.points.split(';');
    const points = [];
    for (const pair of ptPairs) {
        const coords = pair.split(',');
        if (coords.length === 2) {
            const x = parseFloat(coords[0]);
            const y = parseFloat(coords[1]);
            if (!isNaN(x) && !isNaN(y)) {
                points.push({ x, y });
            }
        }
    }
    if (points.length === 0) return;

    let scaleX = 1.0;
    let scaleY = 1.0;
    const teacherWidth = parseFloat(stroke.teacherWidth);
    const teacherHeight = parseFloat(stroke.teacherHeight);
    if (teacherWidth > 0 && teacherHeight > 0) {
        scaleX = canvas.width / teacherWidth;
        scaleY = canvas.height / teacherHeight;
    }

    const ctx = canvas.getContext('2d');
    ctx.beginPath();
    ctx.lineCap = 'round';
    ctx.lineJoin = 'round';

    const strokeWidth = parseFloat(stroke.width) || 3.0;
    ctx.lineWidth = strokeWidth * ((scaleX + scaleY) / 2);
    ctx.strokeStyle = convertWpfColorToCss(stroke.color);

    ctx.moveTo(points[0].x * scaleX, points[0].y * scaleY);
    for (let i = 1; i < points.length; i++) {
        ctx.lineTo(points[i].x * scaleX, points[i].y * scaleY);
    }
    ctx.stroke();

    if (points.length === 1) {
        ctx.beginPath();
        ctx.arc(points[0].x * scaleX, points[0].y * scaleY, ctx.lineWidth / 2, 0, Math.PI * 2);
        ctx.fillStyle = ctx.strokeStyle;
        ctx.fill();
    }
}

function handleWhiteboardDraw(data) {
    whiteboardStrokes.push(data);
    requestAnimationFrame(() => {
        const canvas1 = document.getElementById('whiteboardPageCanvas');
        if (canvas1) {
            drawStrokeOnCanvas(canvas1, data);
        }
        const canvas2 = document.getElementById('screenBroadcastCanvas');
        if (canvas2) {
            drawStrokeOnCanvas(canvas2, data);
        }
    });
}

function handleWhiteboardClear() {
    whiteboardStrokes = [];
    whiteboardShapes = [];
    whiteboardTexts = [];
    const canvas1 = document.getElementById('whiteboardPageCanvas');
    if (canvas1) {
        const ctx1 = canvas1.getContext('2d');
        ctx1.clearRect(0, 0, canvas1.width, canvas1.height);
    }
    const canvas2 = document.getElementById('screenBroadcastCanvas');
    if (canvas2) {
        const ctx2 = canvas2.getContext('2d');
        ctx2.clearRect(0, 0, canvas2.width, canvas2.height);
    }
}

function handleWhiteboardShape(data) {
    if (!data) return;
    whiteboardShapes.push(data);
    requestAnimationFrame(() => {
        const canvas1 = document.getElementById('whiteboardPageCanvas');
        if (canvas1) {
            drawShapeOnCanvas(canvas1, data);
        }
        const canvas2 = document.getElementById('screenBroadcastCanvas');
        if (canvas2) {
            drawShapeOnCanvas(canvas2, data);
        }
    });
}

function drawShapeOnCanvas(canvas, data) {
    if (!canvas) return;
    var ctx = canvas.getContext('2d');
    
    var scaleX = canvas.width / (data.teacherWidth || canvas.width);
    var scaleY = canvas.height / (data.teacherHeight || canvas.height);
    
    var color = data.color || '#ffffff';
    color = convertWpfColorToCss(color);
    var strokeWidth = (data.strokeWidth || 2) * Math.min(scaleX, scaleY);
    var coords = data.coords ? data.coords.split(',').map(Number) : [];
    
    ctx.strokeStyle = color;
    ctx.lineWidth = strokeWidth;
    ctx.beginPath();
    
    switch (data.shapeType) {
        case 'Rectangle':
            if (coords.length >= 4) {
                ctx.strokeRect(
                    coords[0] * scaleX, coords[1] * scaleY,
                    coords[2] * scaleX, coords[3] * scaleY
                );
            }
            break;
        case 'Ellipse':
            if (coords.length >= 4) {
                var cx = (coords[0] + coords[2] / 2) * scaleX;
                var cy = (coords[1] + coords[3] / 2) * scaleY;
                var rx = (coords[2] / 2) * scaleX;
                var ry = (coords[3] / 2) * scaleY;
                ctx.ellipse(cx, cy, rx, ry, 0, 0, 2 * Math.PI);
                ctx.stroke();
            }
            break;
        case 'Line':
            if (coords.length >= 4) {
                ctx.moveTo(coords[0] * scaleX, coords[1] * scaleY);
                ctx.lineTo(coords[2] * scaleX, coords[3] * scaleY);
                ctx.stroke();
            }
            break;
        case 'Arrow':
            if (coords.length >= 4) {
                var x1 = coords[0] * scaleX, y1 = coords[1] * scaleY;
                var x2 = coords[2] * scaleX, y2 = coords[3] * scaleY;
                // Draw line
                ctx.moveTo(x1, y1);
                ctx.lineTo(x2, y2);
                ctx.stroke();
                // Draw arrowhead
                var angle = Math.atan2(y2 - y1, x2 - x1);
                var headLen = 15;
                ctx.beginPath();
                ctx.moveTo(x2, y2);
                ctx.lineTo(x2 - headLen * Math.cos(angle - Math.PI / 6), y2 - headLen * Math.sin(angle - Math.PI / 6));
                ctx.moveTo(x2, y2);
                ctx.lineTo(x2 - headLen * Math.cos(angle + Math.PI / 6), y2 - headLen * Math.sin(angle + Math.PI / 6));
                ctx.stroke();
            }
            break;
    }
}

function handleWhiteboardText(data) {
    if (!data) return;
    whiteboardTexts.push(data);
    requestAnimationFrame(() => {
        const canvas1 = document.getElementById('whiteboardPageCanvas');
        if (canvas1) {
            drawTextOnCanvas(canvas1, data);
        }
        const canvas2 = document.getElementById('screenBroadcastCanvas');
        if (canvas2) {
            drawTextOnCanvas(canvas2, data);
        }
    });
}

function drawTextOnCanvas(canvas, data) {
    if (!canvas) return;
    var ctx = canvas.getContext('2d');
    
    var scaleX = canvas.width / (data.teacherWidth || canvas.width);
    var scaleY = canvas.height / (data.teacherHeight || canvas.height);
    
    var color = data.color || '#ffffff';
    color = convertWpfColorToCss(color);
    var fontSize = (data.fontSize || 16) * Math.min(scaleX, scaleY);
    var pos = data.position ? data.position.split(',').map(Number) : [0, 0];
    
    ctx.fillStyle = color;
    ctx.font = fontSize + 'px sans-serif';
    ctx.fillText(data.text || '', pos[0] * scaleX, pos[1] * scaleY);
}

function enableBoardInteractive(canvas) {
    if (!canvas) return;
    canvas.style.touchAction = 'none';  // Disable scroll/zoom on canvas
    
    canvas.addEventListener('pointerdown', function(e) {
        if (!boardInteractiveMode) return;
        e.preventDefault();
        
        var rect = canvas.getBoundingClientRect();
        var x = e.clientX - rect.left;
        var y = e.clientY - rect.top;
        
        if (boardCurrentTool === 'pen') {
            boardActivePointers[e.pointerId] = {
                points: [{x: x, y: y}],
                color: boardPenColor,
                width: boardPenWidth,
                pointerType: e.pointerType  // 'touch', 'pen', 'mouse'
            };
            canvas.setPointerCapture(e.pointerId);
        } else if (boardCurrentTool === 'eraser') {
            boardEraseAtPoint(canvas, x, y);
            boardActivePointers[e.pointerId] = { tool: 'eraser', lastX: x, lastY: y };
            canvas.setPointerCapture(e.pointerId);
        }
    });
    
    canvas.addEventListener('pointermove', function(e) {
        if (!boardInteractiveMode) return;
        e.preventDefault();
        var ptr = boardActivePointers[e.pointerId];
        if (!ptr) return;
        
        var rect = canvas.getBoundingClientRect();
        var x = e.clientX - rect.left;
        var y = e.clientY - rect.top;
        
        if (boardCurrentTool === 'pen' && ptr.points) {
            ptr.points.push({x: x, y: y});
            // Draw incremental line segment
            var ctx = canvas.getContext('2d');
            var prevPt = ptr.points[ptr.points.length - 2];
            ctx.strokeStyle = ptr.color;
            ctx.lineWidth = ptr.width;
            ctx.lineCap = 'round';
            ctx.lineJoin = 'round';
            ctx.beginPath();
            ctx.moveTo(prevPt.x, prevPt.y);
            ctx.lineTo(x, y);
            ctx.stroke();
        } else if (boardCurrentTool === 'eraser' && ptr.tool === 'eraser') {
            var dx = x - ptr.lastX;
            var dy = y - ptr.lastY;
            if (Math.sqrt(dx*dx + dy*dy) >= 5) {  // Throttle — same as TOUCH_ERASE_THROTTLE
                boardEraseAtPoint(canvas, x, y);
                ptr.lastX = x;
                ptr.lastY = y;
            }
        }
    });
    
    canvas.addEventListener('pointerup', function(e) {
        if (!boardInteractiveMode) return;
        e.preventDefault();
        var ptr = boardActivePointers[e.pointerId];
        if (!ptr) return;
        
        if (boardCurrentTool === 'pen' && ptr.points && ptr.points.length > 1) {
            // Store stroke locally
            var boardStroke = {
                points: ptr.points,
                color: ptr.color,
                width: ptr.width,
                isLocal: true  // Mark as drawn on Board
            };
            whiteboardStrokes.push(boardStroke);
            
            // Send to Laptop via WebSocket (Reverse Command)
            var pointsStr = ptr.points.map(function(p) { return p.x + ',' + p.y; }).join(';');
            sendBoardCommand('DRAW', ptr.color + '|' + ptr.width + '|' + pointsStr + '|' + canvas.width + '|' + canvas.height);
        }
        
        delete boardActivePointers[e.pointerId];
        try { canvas.releasePointerCapture(e.pointerId); } catch(ex) {}
    });
    
    canvas.addEventListener('pointercancel', function(e) {
        delete boardActivePointers[e.pointerId];
        try { canvas.releasePointerCapture(e.pointerId); } catch(ex) {}
    });
}

function boardEraseAtPoint(canvas, x, y) {
    // Strategy: Check whiteboardStrokes for proximity
    var eraserR = boardEraserSize;
    var erased = [];
    
    for (var i = whiteboardStrokes.length - 1; i >= 0; i--) {
        var stroke = whiteboardStrokes[i];
        if (!stroke || !stroke.points) continue;
        
        var hit = false;
        // Check if any point is within eraser radius
        // For stroke objects with array of {x,y} points
        if (Array.isArray(stroke.points)) {
            for (var j = 0; j < stroke.points.length; j++) {
                var p = stroke.points[j];
                var px = typeof p === 'object' ? p.x : 0;
                var py = typeof p === 'object' ? p.y : 0;
                var dx = px - x;
                var dy = py - y;
                if (dx * dx + dy * dy <= eraserR * eraserR) {
                    hit = true;
                    break;
                }
            }
        }
        // For stroke objects with string points (from teacher)
        else if (typeof stroke.points === 'string') {
            var scaleX = canvas.width / (stroke.teacherWidth || canvas.width);
            var scaleY = canvas.height / (stroke.teacherHeight || canvas.height);
            var pts = stroke.points.split(';');
            for (var j = 0; j < pts.length; j++) {
                var coords = pts[j].split(',');
                if (coords.length >= 2) {
                    var px = parseFloat(coords[0]) * scaleX;
                    var py = parseFloat(coords[1]) * scaleY;
                    var dx = px - x;
                    var dy = py - y;
                    if (dx * dx + dy * dy <= eraserR * eraserR) {
                        hit = true;
                        break;
                    }
                }
            }
        }
        
        if (hit) {
            erased.push(i);
        }
    }
    
    if (erased.length > 0) {
        // Remove erased strokes (from end to start to preserve indices)
        erased.forEach(function(idx) {
            whiteboardStrokes.splice(idx, 1);
        });
        
        // Redraw canvas
        var ctx = canvas.getContext('2d');
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        redrawStrokes(canvas);
        
        // Send erase command to Laptop (Reverse Command)
        sendBoardCommand('ERASE_STROKE', erased.join(','));
    }
}

function sendBoardCommand(action, data) {
    if (typeof StudentConnection !== 'undefined' && StudentConnection.isConnected && StudentConnection.isConnected()) {
        StudentConnection.send({
            type: 'board_command',
            action: 'BOARD|' + action + '|' + data
        });
        console.log('📤 Board command sent: BOARD|' + action);
    }
}

function setBoardInteractiveMode(enabled, tool) {
    boardInteractiveMode = enabled;
    if (tool) boardCurrentTool = tool;
    console.log('🖊️ Board interactive mode:', enabled, 'tool:', boardCurrentTool);
}

function setBoardPenConfig(color, width) {
    if (color) boardPenColor = color;
    if (width) boardPenWidth = width;
}

function setBoardEraserSize(size) {
    if (size > 0) boardEraserSize = size;
}

window.addEventListener('resize', () => {
    resizeAndRedrawCanvas('whiteboardPageCanvas', 'whiteboardPageImg');
    const broadcastWrap = document.getElementById('screenBroadcastWrap');
    if (broadcastWrap) {
        const img = broadcastWrap.querySelector('img');
        if (img) {
            resizeAndRedrawCanvas('screenBroadcastCanvas', img);
        }
    }
});

class ZoomPan {
    constructor(viewportEl, containerEl) {
        this.viewport = viewportEl;
        this.container = containerEl;
        
        this.scale = 1.0;
        this.translateX = 0;
        this.translateY = 0;
        
        this.isPanning = false;
        this.startX = 0;
        this.startY = 0;
        
        this.initialPinchDistance = 0;
        this.initialScale = 1.0;
        
        this.setupEvents();
    }
    
    updateTransform() {
        this.scale = Math.min(Math.max(this.scale, 1.0), 4.0);
        if (this.scale === 1.0) {
            this.translateX = 0;
            this.translateY = 0;
        }
        this.container.style.transform = `translate(${this.translateX}px, ${this.translateY}px) scale(${this.scale})`;
        this.viewport.style.cursor = this.scale > 1.0 ? (this.isPanning ? 'grabbing' : 'grab') : 'default';
    }
    
    zoom(factor, clientX, clientY) {
        const prevScale = this.scale;
        this.scale *= factor;
        this.scale = Math.min(Math.max(this.scale, 1.0), 4.0);
        
        if (clientX !== undefined && clientY !== undefined) {
            const rect = this.container.getBoundingClientRect();
            const x = clientX - rect.left;
            const y = clientY - rect.top;
            
            const ratio = this.scale / prevScale;
            this.translateX = x - (x - this.translateX) * ratio;
            this.translateY = y - (y - this.translateY) * ratio;
        }
        
        this.updateTransform();
    }
    
    reset() {
        this.scale = 1.0;
        this.translateX = 0;
        this.translateY = 0;
        this.updateTransform();
    }
    
    setupEvents() {
        this.viewport.addEventListener('mousedown', (e) => {
            if (this.scale <= 1.0) return;
            this.isPanning = true;
            this.startX = e.clientX - this.translateX;
            this.startY = e.clientY - this.translateY;
            this.viewport.style.cursor = 'grabbing';
            e.preventDefault();
        });
        
        window.addEventListener('mousemove', (e) => {
            if (!this.isPanning) return;
            this.translateX = e.clientX - this.startX;
            this.translateY = e.clientY - this.startY;
            this.updateTransform();
        });
        
        window.addEventListener('mouseup', () => {
            if (this.isPanning) {
                this.isPanning = false;
                this.updateTransform();
            }
        });
        
        this.viewport.addEventListener('wheel', (e) => {
            e.preventDefault();
            const factor = e.deltaY < 0 ? 1.15 : 0.85;
            this.zoom(factor, e.clientX, e.clientY);
        }, { passive: false });
        
        this.viewport.addEventListener('touchstart', (e) => {
            if (e.touches.length === 1 && this.scale > 1.0) {
                this.isPanning = true;
                this.startX = e.touches[0].clientX - this.translateX;
                this.startY = e.touches[0].clientY - this.translateY;
            } else if (e.touches.length === 2) {
                this.isPanning = false;
                this.initialPinchDistance = this.getTouchDistance(e.touches);
                this.initialScale = this.scale;
            }
        });
        
        this.viewport.addEventListener('touchmove', (e) => {
            if (this.isPanning && e.touches.length === 1) {
                this.translateX = e.touches[0].clientX - this.startX;
                this.translateY = e.touches[0].clientY - this.startY;
                this.updateTransform();
                e.preventDefault();
            } else if (e.touches.length === 2) {
                const dist = this.getTouchDistance(e.touches);
                if (dist > 0 && this.initialPinchDistance > 0) {
                    const factor = dist / this.initialPinchDistance;
                    const targetScale = Math.min(Math.max(this.initialScale * factor, 1.0), 4.0);
                    const zoomFactor = targetScale / this.scale;
                    
                    const centerX = (e.touches[0].clientX + e.touches[1].clientX) / 2;
                    const centerY = (e.touches[0].clientY + e.touches[1].clientY) / 2;
                    this.zoom(zoomFactor, centerX, centerY);
                }
                e.preventDefault();
            }
        }, { passive: false });
        
        this.viewport.addEventListener('touchend', () => {
            this.isPanning = false;
            this.updateTransform();
        });
    }
    
    getTouchDistance(touches) {
        const dx = touches[0].clientX - touches[1].clientX;
        const dy = touches[0].clientY - touches[1].clientY;
        return Math.sqrt(dx * dx + dy * dy);
    }
}

window.zoomIn = function() {
    if (whiteboardZoomPan) whiteboardZoomPan.zoom(1.25);
};
window.zoomOut = function() {
    if (whiteboardZoomPan) whiteboardZoomPan.zoom(0.8);
};
window.resetZoom = function() {
    if (whiteboardZoomPan) whiteboardZoomPan.reset();
};
window.zoomInBroadcast = function() {
    if (broadcastZoomPan) broadcastZoomPan.zoom(1.25);
};
window.zoomOutBroadcast = function() {
    if (broadcastZoomPan) broadcastZoomPan.zoom(0.8);
};
window.resetZoomBroadcast = function() {
    if (broadcastZoomPan) broadcastZoomPan.reset();
};

// ─── SIDEBAR (Mobile) ───
function toggleSidebar() {
    document.getElementById('sidebar').classList.toggle('open');
    document.getElementById('sidebarBackdrop').classList.toggle('open');
}

function closeSidebar() {
    document.getElementById('sidebar').classList.remove('open');
    document.getElementById('sidebarBackdrop').classList.remove('open');
}

// â•â•â• CLOCK â•â•â•
function startClock() {
    function update() {
        const now = new Date();
        const h = String(now.getHours()).padStart(2, '0');
        const m = String(now.getMinutes()).padStart(2, '0');
        document.getElementById('topbarClock').textContent = `${h}:${m}`;
    }
    update();
    setInterval(update, 30000);
}

// â•â•â• CONNECTION â•â•â•
function setupConnection() {
    const session = AppStorage.getSession();
    if (!session.wsUrl) {
        updateConnectionUI(false);
        return;
    }

    // â”€â”€â”€ Core connection events â”€â”€â”€
    StudentConnection.on('connected', (data) => {
        updateConnectionUI(true, data);
        showToast('ÄÃ£ káº¿t ná»‘i vá»›i giÃ¡o viÃªn!', 'success');
        addActivity('ðŸŸ¢', 'ÄÃ£ káº¿t ná»‘i thÃ nh cÃ´ng', '#D1FAE5');
    });

    StudentConnection.on('disconnected', () => {
        updateConnectionUI(false);
        showToast('Máº¥t káº¿t ná»‘i vá»›i giÃ¡o viÃªn', 'warning');
    });

    // â”€â”€â”€ Lock / Unlock / Black Screen â”€â”€â”€
    StudentConnection.on('lock_screen', (data) => {
        OverlayManager.showLockScreen(data);
        showToast(data.title || 'GV Ä‘Ã£ khÃ³a mÃ n hÃ¬nh', 'warning');
        addActivity('ðŸ”’', data.title || 'KhÃ³a mÃ n hÃ¬nh', '#FEE2E2');
    });

    StudentConnection.on('unlock_screen', () => {
        OverlayManager.closeLockScreen();
        OverlayManager.closeSilence();
        OverlayManager.closeWarning();
        showToast('ðŸ”“ MÃ n hÃ¬nh Ä‘Ã£ Ä‘Æ°á»£c má»Ÿ khÃ³a', 'success');
        addActivity('ðŸ”“', 'ÄÃ£ má»Ÿ khÃ³a mÃ n hÃ¬nh', '#D1FAE5');
    });

    // â”€â”€â”€ Silence â”€â”€â”€
    StudentConnection.on('silence', (data) => {
        if (data.active) {
            OverlayManager.showSilence();
            showToast('ðŸ”‡ IM Láº¶NG â€” GV yÃªu cáº§u tráº­t tá»±!', 'warning');
            addActivity('ðŸ”‡', 'GV yÃªu cáº§u IM Láº¶NG', '#FEE2E2');
        } else {
            OverlayManager.closeSilence();
            showToast('ðŸ”Š ÄÃ£ gá»¡ tráº¡ng thÃ¡i IM Láº¶NG', 'success');
            addActivity('ðŸ”Š', 'Gá»¡ IM Láº¶NG', '#D1FAE5');
        }
    });

    // â”€â”€â”€ Teacher Warning â”€â”€â”€
    StudentConnection.on('teacher_warning', (data) => {
        OverlayManager.showWarning(data);
        showToast('âš ï¸ Cáº£nh bÃ¡o tá»« GV', 'warning');
        addActivity('âš ï¸', data.message || 'Cáº£nh bÃ¡o tá»« GV', '#FEF3C7');
    });

    StudentConnection.on('clear_warning', () => {
        OverlayManager.closeWarning();
        showToast('âœ… Háº¿t cáº£nh bÃ¡o', 'success');
    });

    // â”€â”€â”€ Notice Popup â”€â”€â”€
    StudentConnection.on('notice', (data) => {
        OverlayManager.showNotice(data);
        addActivity('ðŸ“¢', data.title || 'ThÃ´ng bÃ¡o tá»« GV', '#DBEAFE');
    });

    // â”€â”€â”€ Broadcast (Screen Cast) â”€â”€â”€
    StudentConnection.on('broadcast_start', (data) => {
        OverlayManager.showBroadcast(data);
        showToast('ðŸ“¡ GV Ä‘ang chiáº¿u mÃ n hÃ¬nh', 'info');
        addActivity('ðŸ“¡', 'GV báº¯t Ä‘áº§u chiáº¿u mÃ n hÃ¬nh', '#DBEAFE');
    });

    StudentConnection.on('broadcast_stop', () => {
        OverlayManager.closeBroadcast();
        showToast('ðŸ“¡ GV dá»«ng chiáº¿u mÃ n hÃ¬nh', 'info');
        addActivity('ðŸ“¡', 'Dá»«ng chiáº¿u mÃ n hÃ¬nh', '#D1FAE5');
    });

    // ─── Web Control ───
    StudentConnection.on('block_web_on', () => {
        isWebBlocked = true;
        isWebWhitelistActive = false;
        showToast('🚫 Truy cập Web đã bị khóa bởi giáo viên!', 'warning');
        addActivity('🚫', 'GV khóa truy cập Web', '#FEE2E2');
    });

    StudentConnection.on('block_web_off', () => {
        isWebBlocked = false;
        showToast('✅ Đã mở truy cập Web', 'success');
        addActivity('✅', 'GV mở truy cập Web', '#D1FAE5');
    });

    StudentConnection.on('web_whitelist_on', () => {
        isWebWhitelistActive = true;
        isWebBlocked = false;
        showToast('🔒 Đang bật chế độ giới hạn Web', 'warning');
        addActivity('🔒', 'Chế độ giới hạn Web', '#FEF3C7');
    });

    StudentConnection.on('web_whitelist_off', () => {
        isWebWhitelistActive = false;
        showToast('🔓 Đã tắt chế độ giới hạn Web', 'success');
        addActivity('🔓', 'Tắt giới hạn Web', '#D1FAE5');
    });

    StudentConnection.on('whitelist_add', (data) => {
        if (data.urls) {
            data.urls.split(',').forEach(url => {
                if (url && !allowedWebUrls.includes(url)) {
                    allowedWebUrls.push(url);
                }
            });
            console.log('[Web Policy] Whitelist updated:', allowedWebUrls);
        }
    });

    // ─── Clear All ───
    StudentConnection.on('clear_all', () => {
        OverlayManager.clearAll();
        AppStorage.clearAppState();
        isWebBlocked = false;
        isWebWhitelistActive = false;
        allowedWebUrls = [];
        showToast('✅ GV đã gỡ mọi khóa/cảnh báo', 'success');
        addActivity('✅', 'Gỡ tất cả overlay', '#D1FAE5');
    });

    // ─── Open URL ───
    StudentConnection.on('open_url', (data) => {
        if (!data.url) return;
        
        if (isWebBlocked) {
            showToast('🚫 Truy cập bị chặn: Giáo viên đã khóa Web!', 'warning', 6000);
            addActivity('🚫', `Chặn mở URL (Web bị khóa): ${data.url}`, '#FEE2E2');
            return;
        }

        if (isWebWhitelistActive) {
            try {
                const targetHost = new URL(data.url).host.toLowerCase();
                const allowedHosts = ['accounts.google.com', 'login.microsoftonline.com', 'facebook.com', 'www.facebook.com', 'm.facebook.com'];
                
                allowedWebUrls.forEach(u => {
                    try {
                        const h = new URL(u).host.toLowerCase();
                        allowedHosts.push(h);
                    } catch {}
                });

                const isAllowed = allowedHosts.some(allowed => targetHost === allowed || targetHost.endsWith('.' + allowed));
                
                if (!isAllowed) {
                    showToast('🚫 Truy cập bị chặn: URL không nằm trong whitelist!', 'warning', 6000);
                    addActivity('🚫', `Chặn mở URL (Whitelist): ${data.url}`, '#FEE2E2');
                    return;
                }
            } catch (e) {
                showToast('🚫 URL không hợp lệ!', 'warning');
                return;
            }
        }

        window.open(data.url, '_blank');
        showToast(`🌐 Mở: ${data.url}`, 'info');
        addActivity('🌐', `GV mở URL: ${data.url}`, '#F3E8FF');
    });

    // ─── Screen Broadcast (Whiteboard Image) ───
    StudentConnection.on('screen_broadcast', (data) => {
        OverlayManager.showScreenBroadcast(data);
        showToast('📡 GV đang chiếu bảng', 'info');
        addActivity('📡', 'GV chiếu bảng trực tiếp', '#DBEAFE');
    });

    StudentConnection.on('screen_broadcast_update', (data) => {
        OverlayManager.updateScreenBroadcast(data);
    });

    StudentConnection.on('screen_broadcast_stop', () => {
        OverlayManager.stopScreenBroadcast();
        showToast('📡 Dừng chiếu bảng', 'info');
        addActivity('📡', 'Dừng chiếu bảng', '#D1FAE5');
    });

    // â”€â”€â”€ File Broadcast â”€â”€â”€
    StudentConnection.on('file_broadcast', (data) => {
        OverlayManager.showFileBroadcast(data);
        showToast(`ðŸ“ GV chia sáº»: ${data.fileName || 'File'}`, 'info', 5000);
        addActivity('ðŸ“', `GV chia sáº» file: ${data.fileName || ''}`, '#F3E8FF');
    });

    StudentConnection.on('file_broadcast_stop', () => {
        OverlayManager.closeFileBroadcast();
        showToast('ðŸ“ GV dá»«ng chia sáº» file', 'info');
        addActivity('ðŸ“', 'Dá»«ng chia sáº» file', '#D1FAE5');
    });

    StudentConnection.on('file_focus', (data) => {
        OverlayManager.focusFilePage(data.page);
        showToast(`ðŸ“„ GV chuyá»ƒn trang ${data.page}`, 'info');
        addActivity('ðŸ“„', `GV chuyá»ƒn trang ${data.page}`, '#DBEAFE');
    });

    // â”€â”€â”€ Tool Focus â”€â”€â”€
    StudentConnection.on('tool_focus', (data) => {
        OverlayManager.showToolFocus(data);
        showToast('ðŸŽ¯ GV yÃªu cáº§u táº­p trung cÃ´ng cá»¥!', 'warning', 5000);
        addActivity('ðŸŽ¯', `Focus: ${data.toolId || 'Tool'}`, '#FEF3C7');
    });

    StudentConnection.on('tool_unfocus', () => {
        OverlayManager.closeToolFocus();
        showToast('âœ… Háº¿t cháº¿ Ä‘á»™ Focus', 'success');
        addActivity('âœ…', 'Gá»¡ Tool Focus', '#D1FAE5');
    });

    StudentConnection.on('tool_section_focus', (data) => {
        OverlayManager.showToolSectionFocus(data);
        showToast(`ðŸŽ¯ Focus: ${data.toolId} â†’ ${data.sectionId}`, 'warning', 5000);
        addActivity('ðŸŽ¯', `Section Focus: ${data.toolId} â†’ ${data.sectionId}`, '#FEF3C7');
    });

    StudentConnection.on('tool_section_unfocus', () => {
        OverlayManager.closeToolFocus();
        showToast('âœ… Háº¿t Section Focus', 'success');
    });

    // â”€â”€â”€ Survey â”€â”€â”€
    StudentConnection.on('survey', (data) => {
        OverlayManager.showSurvey(data);
        showToast('ðŸ“Š Kháº£o sÃ¡t má»›i tá»« GV!', 'warning', 5000);
        addActivity('ðŸ“Š', `Kháº£o sÃ¡t: ${data.question || ''}`, '#DBEAFE');
    });

    StudentConnection.on('survey_end', () => {
        OverlayManager.closeSurvey();
        showToast('â° Kháº£o sÃ¡t Ä‘Ã£ káº¿t thÃºc', 'info');
    });

    // â”€â”€â”€ Clear All â”€â”€â”€
    StudentConnection.on('clear_all', () => {
        OverlayManager.clearAll();
        AppStorage.clearAppState();
        showToast('âœ… GV Ä‘Ã£ gá»¡ má»i khÃ³a/cáº£nh bÃ¡o', 'success');
        addActivity('âœ…', 'Gá»¡ táº¥t cáº£ overlay', '#D1FAE5');
    });

    // â”€â”€â”€ Open URL â”€â”€â”€
    StudentConnection.on('open_url', (data) => {
        if (data.url) {
            window.open(data.url, '_blank');
            showToast(`ðŸŒ Má»Ÿ: ${data.url}`, 'info');
            addActivity('ðŸŒ', `GV má»Ÿ URL: ${data.url}`, '#F3E8FF');
        }
    });

    // â”€â”€â”€ Message (generic) â”€â”€â”€
    StudentConnection.on('message', (data) => {
        showToast(data.text || 'ThÃ´ng bÃ¡o tá»« GV', 'info');
        addActivity('ðŸ“¢', data.text || 'ThÃ´ng bÃ¡o má»›i', '#DBEAFE');
    });

    // â”€â”€â”€ Chat â”€â”€â”€
    StudentConnection.on('chat', (data) => {
        // Route to ChatManager module (chat_message listener)
        StudentConnection.emit('chat_message', data);
        if (currentPage !== 'chat') {
            unreadMessages++;
            updateChatBadge();
        }
    });

    // â”€â”€â”€ Quiz â”€â”€â”€
    StudentConnection.on('quiz_start', (data) => {
        showToast('ðŸš¨ BÃ i kiá»ƒm tra má»›i!', 'warning', 6000);
        addActivity('ðŸ“', 'GV báº¯t Ä‘áº§u kiá»ƒm tra', '#FEF3C7');
        navigateTo('quiz');
    });

    // â”€â”€â”€ Lesson â”€â”€â”€
    StudentConnection.on('lesson_start', (data) => {
        showToast('ðŸ“– GV báº¯t Ä‘áº§u bÃ i giáº£ng', 'info');
        addActivity('ðŸ“–', 'BÃ i giáº£ng báº¯t Ä‘áº§u', '#DBEAFE');
        navigateTo('lesson');
        // Update dashboard stat (P0-4)
        const statLessons = document.getElementById('statLessons');
        if (statLessons) {
            const current = parseInt(statLessons.textContent) || 0;
            statLessons.textContent = current + 1;
        }
    });

    StudentConnection.on('lesson_stage', (data) => {
        const stageNames = ['', 'Má»Ÿ Ä‘áº§u', 'HÃ¬nh thÃ nh kiáº¿n thá»©c', 'Luyá»‡n táº­p', 'Váº­n dá»¥ng', 'Tá»•ng káº¿t', 'BTVN'];
        const stage = parseInt(data.target || data.stage || 0);
        const name = stageNames[stage] || `Giai Ä‘oáº¡n ${stage}`;
        showToast(`ðŸ“– GV chuyá»ƒn sang: ${name}`, 'info');
        addActivity('ðŸ“–', `Giai Ä‘oáº¡n: ${name}`, '#DBEAFE');
        if (currentPage !== 'lesson') navigateTo('lesson');
    });

    StudentConnection.on('lesson_focus', (data) => {
        showToast('ðŸŽ¯ GV yÃªu cáº§u táº­p trung ná»™i dung!', 'warning', 5000);
        addActivity('ðŸŽ¯', `Focus ná»™i dung #${data.target || ''}`, '#FEF3C7');
        if (currentPage !== 'lesson') navigateTo('lesson');
    });

    StudentConnection.on('lesson_end', (data) => {
        showToast('ðŸ“– BÃ i giáº£ng Ä‘Ã£ káº¿t thÃºc', 'info');
        addActivity('âœ…', 'BÃ i giáº£ng káº¿t thÃºc', '#D1FAE5');
    });

    // â”€â”€â”€ Lesson Unfocus â”€â”€â”€
    StudentConnection.on('lesson_unfocus', () => {
        document.querySelectorAll('.lesson-block.focused').forEach(el => {
            el.classList.remove('focused');
            el.style.opacity = '';
        });
        showToast('ðŸ”“ GV Ä‘Ã£ bá» cháº¿ Ä‘á»™ táº­p trung', 'success');
        addActivity('ðŸ”“', 'Bá» Focus bÃ i giáº£ng', '#D1FAE5');
    });

    // â”€â”€â”€ Quiz Review (GV trÃ¬nh chiáº¿u bÃ i lÃ m máº«u) â”€â”€â”€
    StudentConnection.on('quiz_review', (data) => {
        OverlayManager.showQuizReview(data);
        showToast('ðŸ“‹ GV Ä‘ang trÃ¬nh chiáº¿u bÃ i lÃ m máº«u', 'info', 5000);
        addActivity('ðŸ“‹', `BÃ i lÃ m máº«u: ${data.StudentName || 'HS'}`, '#DBEAFE');
    });

    StudentConnection.on('quiz_review_focus', (data) => {
        const idx = parseInt(data.target || data.questionIndex || 0);
        if (idx > 0) OverlayManager.focusQuizReviewQuestion(idx);
    });

    StudentConnection.on('quiz_review_close', () => {
        OverlayManager.closeQuizReview();
        showToast('ðŸ“‹ GV Ä‘Ã£ Ä‘Ã³ng bÃ i lÃ m máº«u', 'info');
    });

    // â”€â”€â”€ Homework â”€â”€â”€
    StudentConnection.on('homework', (data) => {
        showToast('ðŸ“š BÃ i táº­p vá» nhÃ  má»›i!', 'info', 6000);
        addActivity('ðŸ“š', `BTVN: ${data.data || data.title || ''}`, '#F3E8FF');
    });

    // â”€â”€â”€ State Sync (Late-Join Recovery) â”€â”€â”€
    StudentConnection.on('state_sync', (data) => {
        console.log('[StateSync] Received classroom state:', data);
        addActivity('ðŸ”„', 'Äá»“ng bá»™ tráº¡ng thÃ¡i lá»›p há»c', '#DBEAFE');

        // Restore active overlays from teacher state
        const state = data.state || data;

        // Lock screen
        if (state.locked) {
            OverlayManager.showLockScreen({
                type: state.lockType || 'lock',
                title: state.lockTitle || '',
                subtitle: state.lockSubtitle || ''
            });
        }

        // Black screen
        if (state.blackScreen) {
            OverlayManager.showLockScreen({
                type: 'black',
                title: state.blackTitle || 'ðŸ“´ Táº¯t mÃ n hÃ¬nh',
                subtitle: state.blackSubtitle || ''
            });
        }

        // Silence
        if (state.silenced) {
            OverlayManager.showSilence();
        }

        // Broadcast
        if (state.broadcasting) {
            OverlayManager.showBroadcast({
                resolution: state.broadcastResolution || '1920Ã—1080',
                fps: state.broadcastFps || '30 fps'
            });
        }

        // Screen Broadcast (whiteboard)
        if (state.screenBroadcast) {
            OverlayManager.showScreenBroadcast({
                imageUrl: state.screenBroadcastUrl || ''
            });
        }

        // File Broadcast
        if (state.fileBroadcast) {
            OverlayManager.showFileBroadcast({
                fileType: state.fileType || 'FILE',
                fileName: state.fileName || '',
                fileUrl: state.fileUrl || ''
            });
        }

        // Tool Focus
        if (state.toolFocus) {
            OverlayManager.showToolFocus({ toolId: state.toolFocusId || '' });
            if (state.toolSectionFocus) {
                OverlayManager.showToolSectionFocus({
                    toolId: state.toolFocusId || '',
                    sectionId: state.toolSectionId || ''
                });
            }
        }

        // Active Quiz
        if (state.activeQuiz) {
            showToast('ðŸš¨ CÃ³ bÃ i kiá»ƒm tra Ä‘ang diá»…n ra!', 'warning', 6000);
            navigateTo('quiz');
        }

        // Active Lesson
        if (state.activeLesson) {
            showToast('ðŸ“– BÃ i giáº£ng Ä‘ang diá»…n ra', 'info');
            navigateTo('lesson');
        }

        // Quiz Review
        if (state.quizReview) {
            showToast('ðŸ“ GV Ä‘ang xem láº¡i bÃ i', 'info');
        }

        // Save state for local recovery
        AppStorage.saveAppState({
            overlays: {
                locked: !!state.locked,
                blackScreen: !!state.blackScreen,
                silenced: !!state.silenced,
                broadcasting: !!state.broadcasting,
                screenBroadcast: !!state.screenBroadcast,
                fileBroadcast: !!state.fileBroadcast,
                toolFocus: !!state.toolFocus
            },
            currentPage: state.activeQuiz ? 'quiz' : state.activeLesson ? 'lesson' : currentPage
        });
    });

    // ─── Whiteboard Vector Drawing ───
    StudentConnection.on('whiteboard_draw', handleWhiteboardDraw);
    StudentConnection.on('whiteboard_clear', handleWhiteboardClear);

    // Reconnect
    StudentConnection.reconnect();
}

function updateConnectionUI(connected, data) {
    const statusEl = document.getElementById('sessionStatus');
    const classEl = document.getElementById('sessionClass');
    const teacherEl = document.getElementById('teacherName');
    const settingStatus = document.getElementById('settingStatus');
    const dashBadge = document.getElementById('dashConnectionBadge');
    const dashConnText = document.getElementById('dashConnText');

    if (connected) {
        statusEl.textContent = 'âœ… ÄÃ£ káº¿t ná»‘i';
        classEl.textContent = data?.className || 'Lá»›p há»c';
        teacherEl.textContent = `GV: ${data?.teacherName || 'ÄÃ£ káº¿t ná»‘i'}`;
        if (settingStatus) settingStatus.textContent = 'ðŸŸ¢ Äang káº¿t ná»‘i';
        // Dashboard badge
        if (dashBadge) dashBadge.classList.remove('disconnected');
        if (dashConnText) dashConnText.textContent = 'Äang káº¿t ná»‘i';
    } else {
        statusEl.textContent = 'â³ Äang káº¿t ná»‘i láº¡i...';
        classEl.textContent = 'Äang káº¿t ná»‘i...';
        teacherEl.textContent = 'GV: Äang káº¿t ná»‘i...';
        if (settingStatus) settingStatus.textContent = 'ðŸ”´ Máº¥t káº¿t ná»‘i';
        // Dashboard badge
        if (dashBadge) dashBadge.classList.add('disconnected');
        if (dashConnText) dashConnText.textContent = 'Máº¥t káº¿t ná»‘i';
    }
}

// â•â•â• CHAT â•â•â•
function sendChatMessage() {
    // Legacy function â€” ChatManager.sendMessage() is the active implementation.
    // Delegate to ChatManager if available.
    if (typeof ChatManager !== 'undefined' && typeof ChatManager.sendMessage === 'function') {
        ChatManager.sendMessage();
    }
}

function appendChatMessage(msg) {
    // Legacy function â€” ChatManager now handles all chat rendering.
    // This is kept as a no-op fallback to avoid reference errors.
    // Chat messages are routed via: chat event â†’ chat_message â†’ ChatManager.handleIncomingMessage
}

function loadChatHistory() {
    // Chat history is now managed by ChatManager via localStorage key 'qasc_chat_v2'.
    // This legacy function is a no-op.
}

function updateChatBadge() {
    const badge = document.getElementById('chatBadge');
    if (!badge) return;
    if (unreadMessages > 0) {
        badge.textContent = unreadMessages > 99 ? '99+' : unreadMessages;
        badge.classList.remove('hidden');
    } else {
        badge.classList.add('hidden');
    }
}

// â•â•â• HAND RAISE â•â•â•
function toggleHandRaise() {
    isHandRaised = !isHandRaised;
    const btn = document.getElementById('handraiseBtn');
    const text = document.getElementById('handraiseText');

    if (isHandRaised) {
        btn.classList.add('raised');
        text.textContent = 'ðŸ™‹ Äang giÆ¡ tay â€” Nháº¥n láº¡i Ä‘á»ƒ háº¡';
        showToast('Báº¡n Ä‘Ã£ giÆ¡ tay! GV sáº½ tháº¥y.', 'success');
        addActivity('âœ‹', 'GiÆ¡ tay há»i giÃ¡o viÃªn', '#FEF3C7');
    } else {
        btn.classList.remove('raised');
        text.textContent = 'Nháº¥n Ä‘á»ƒ giÆ¡ tay';
    }

    StudentConnection.sendHandRaise(isHandRaised);
}

function sendQuestionToTeacher() {
    const input = document.getElementById('questionInput');
    const text = input.value.trim();
    if (!text) {
        showToast('Vui lÃ²ng nháº­p cÃ¢u há»i', 'warning');
        return;
    }

    StudentConnection.sendQuestion(text);
    showToast('ÄÃ£ gá»­i cÃ¢u há»i cho giÃ¡o viÃªn!', 'success');
    addActivity('ðŸ’¡', `Há»i GV: ${text}`, '#F3E8FF');
    input.value = '';
}

// â•â•â• SETTINGS â•â•â•
function saveAppSettings() {
    const portEl = document.getElementById('settingPort');
    const autoReconnEl = document.getElementById('settingAutoReconnect');
    const fontSizeEl = document.getElementById('settingFontSize');

    const settings = {
        soundEnabled: document.getElementById('settingSound').checked,
        notificationsEnabled: document.getElementById('settingNotif').checked,
        port: portEl ? parseInt(portEl.value) || 9000 : 9000,
        autoReconnect: autoReconnEl ? autoReconnEl.checked : true,
        fontSize: fontSizeEl ? fontSizeEl.value : 'normal'
    };

    AppStorage.saveSettings(settings);

    // Apply font size immediately
    _applyFontSize(settings.fontSize);

    showToast('ÄÃ£ lÆ°u cÃ i Ä‘áº·t', 'success', 2000);
}

function _applyFontSize(size) {
    const root = document.documentElement;
    switch (size) {
        case 'small':
            root.style.fontSize = '13px';
            break;
        case 'large':
            root.style.fontSize = '16px';
            break;
        default:
            root.style.fontSize = '14px';
            break;
    }
}

function disconnectAndReturn() {
    StudentConnection.disconnect();
    AppStorage.clearSession();
    window.location.href = 'index.html';
}

// â•â•â• PROFILE SAVE (P0-1) â•â•â•
function saveStudentProfile() {
    const nameInput = document.getElementById('settingName');
    const name = nameInput ? nameInput.value.trim() : '';
    if (!name) {
        showToast('Vui lÃ²ng nháº­p há» tÃªn!', 'warning');
        return;
    }

    // Save to storage
    const info = AppStorage.getStudentInfo();
    info.name = name;
    info.deviceName = getDeviceType();
    AppStorage.saveStudentInfo(info);

    // Update sidebar immediately
    const initials = getInitials(name);
    const profileName = document.getElementById('profileName');
    const profileAvatar = document.getElementById('profileAvatar');
    const welcomeText = document.getElementById('welcomeText');
    if (profileName) profileName.textContent = name;
    if (profileAvatar) profileAvatar.textContent = initials;
    if (welcomeText) welcomeText.textContent = `Xin chÃ o, ${name} ðŸ‘‹`;

    showToast(`ðŸ’¾ ÄÃ£ lÆ°u há»“ sÆ¡: ${name}`, 'success');
    addActivity('ðŸ‘¤', `Cáº­p nháº­t tÃªn: ${name}`, '#D1FAE5');
}

// â•â•â• RECONNECT (P0-1) â•â•â•
function reconnectWithSettings() {
    const portEl = document.getElementById('settingPort');
    const port = portEl ? (parseInt(portEl.value) || 9000) : 9000;

    // Update port in session
    const session = AppStorage.getSession();
    if (session.wsUrl) {
        // Replace port in existing WS URL
        try {
            const url = new URL(session.wsUrl.replace('ws://', 'http://').replace('wss://', 'https://'));
            url.port = port;
            const newWs = (session.wsUrl.startsWith('wss') ? 'wss://' : 'ws://') + url.host + url.pathname;
            session.wsUrl = newWs;
            AppStorage.saveSession(session);
        } catch {}
    }

    // Trigger reconnect
    StudentConnection.reconnect();
    showToast('ðŸ”— Äang káº¿t ná»‘i láº¡i...', 'info');
    addActivity('ðŸ”—', 'Káº¿t ná»‘i láº¡i theo cÃ i Ä‘áº·t má»›i', '#DBEAFE');

    // Save settings
    saveAppSettings();
}

// â•â•â• ACTIVITY LOG â•â•â•
function addActivity(icon, text, bgColor) {
    const list = document.getElementById('activityList');

    // Clear empty state
    const emptyState = list.querySelector('.empty-state');
    if (emptyState) emptyState.remove();

    const time = new Date().toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });

    const el = document.createElement('div');
    el.className = 'activity-item animate-fade-in';
    el.innerHTML = `
        <div class="activity-icon" style="background:${bgColor || '#F1F5F9'}">${icon}</div>
        <div class="activity-text">${escapeHtml(text)}</div>
        <div class="activity-time">${time}</div>
    `;

    // Insert at top
    list.insertBefore(el, list.firstChild);

    // Limit to 20
    while (list.children.length > 20) {
        list.removeChild(list.lastChild);
    }
}

// â•â•â• DASHBOARD NOTIFICATIONS â•â•â•
const _notifColorMap = {
    quiz: { bg: '#FFF8E1', color: '#F57F17', icon: 'ðŸ“' },
    file: { bg: '#E8F5E9', color: '#2E7D32', icon: 'ðŸ“' },
    chat: { bg: '#E3F2FD', color: '#1565C0', icon: 'ðŸ’¬' },
    result: { bg: '#F3E5F5', color: '#7B1FA2', icon: 'ðŸ†' },
    lesson: { bg: '#DBEAFE', color: '#1976D2', icon: 'ðŸ“–' },
    warning: { bg: '#FEE2E2', color: '#DC2626', icon: 'âš ï¸' },
    info: { bg: '#F1F5F9', color: '#475569', icon: 'ðŸ“¢' }
};

function addDashNotification(type, title, desc) {
    const list = document.getElementById('dashNotifList');
    if (!list) return;

    // Remove empty state
    const empty = document.getElementById('dashNotifEmpty');
    if (empty) empty.remove();

    const meta = _notifColorMap[type] || _notifColorMap.info;
    const time = new Date().toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });

    const el = document.createElement('div');
    el.className = 'dash-notif-item';
    el.style.background = meta.bg;
    el.innerHTML = `
        <div class="dash-notif-title" style="color:${meta.color}">${meta.icon} ${escapeHtml(title)}</div>
        <div class="dash-notif-desc">${escapeHtml(desc)}</div>
        <div class="dash-notif-time">${time}</div>
    `;

    // Insert at top
    list.insertBefore(el, list.firstChild);

    // Limit to 10
    while (list.children.length > 10) {
        list.removeChild(list.lastChild);
    }
}

// â•â•â• DASHBOARD LESSON CARD â•â•â•
function updateDashLesson(title, subtitle, teacher, stage) {
    const empty = document.getElementById('dashLessonEmpty');
    const active = document.getElementById('dashLessonActive');
    if (!active) return;

    if (empty) empty.classList.add('hidden');
    active.classList.remove('hidden');

    document.getElementById('dashLessonTitle').textContent = title || '--';
    document.getElementById('dashLessonSubtitle').textContent = subtitle || '--';
    document.getElementById('dashLessonTeacher').textContent = teacher || 'GV: --';
    document.getElementById('dashLessonStage').textContent = stage || '--';
}

function clearDashLesson() {
    const empty = document.getElementById('dashLessonEmpty');
    const active = document.getElementById('dashLessonActive');
    if (empty) empty.classList.remove('hidden');
    if (active) active.classList.add('hidden');
}

// â•â•â• HELPERS â•â•â•
function getInitials(name) {
    if (!name) return '?';
    const parts = name.split(' ').filter(Boolean);
    if (parts.length >= 2) {
        return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
    }
    return name.substring(0, 2).toUpperCase();
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function getDeviceType() {
    const ua = navigator.userAgent;
    if (/iPad/.test(ua)) return 'iPad';
    if (/Android/.test(ua) && !/Mobile/.test(ua)) return 'Android Tablet';
    if (/Android/.test(ua)) return 'Android';
    if (/iPhone/.test(ua)) return 'iPhone';
    if (/Windows/.test(ua)) return 'Windows';
    if (/Mac/.test(ua)) return 'MacOS';
    return 'Web Browser';
}

