/* ═══════════════════════════════════════════════════════════
   QA SmartClass — WebSocket Connection Client
   Bridges to WebSocketBridgeService on Teacher's PC
   Protocol: JSON messages over WebSocket
   ═══════════════════════════════════════════════════════════ */

const StudentConnection = (() => {
    let _ws = null;
    let _heartbeatTimer = null;
    let _screenshotTimer = null;   // FIX: periodic screenshot push
    let _reconnectTimer = null;
    let _reconnectAttempts = 0;
    let _config = {};
    let _callbacks = {};
    let _isConnected = false;
    const MAX_RECONNECT = 10;
    const HEARTBEAT_INTERVAL = 10000;
    const RECONNECT_BASE_DELAY = 2000;

    // ─── Event listeners ───
    const _listeners = {};

    function on(event, callback) {
        if (!_listeners[event]) _listeners[event] = [];
        _listeners[event].push(callback);
    }

    function off(event, callback) {
        if (!_listeners[event]) return;
        _listeners[event] = _listeners[event].filter(cb => cb !== callback);
    }

    function emit(event, data) {
        if (_listeners[event]) {
            _listeners[event].forEach(cb => {
                try { cb(data); } catch (e) { console.error(`Event handler error [${event}]:`, e); }
            });
        }
    }

    // ─── Connect ───
    function connect(wsUrl, options = {}) {
        _config = {
            url: wsUrl,
            studentName: options.studentName || 'Học sinh',
            studentCode: options.studentCode || 'HS001',
        };
        _callbacks = {
            onConnected: options.onConnected || (() => {}),
            onError: options.onError || (() => {}),
            onDisconnected: options.onDisconnected || (() => {})
        };

        // Save connection info for app.html
        AppStorage.saveSession({
            wsUrl: wsUrl,
            studentName: _config.studentName,
            studentCode: _config.studentCode
        });

        _doConnect();
    }

    function _doConnect() {
        try {
            console.log(`[WS] Connecting to ${_config.url}...`);
            _ws = new WebSocket(_config.url);
            _ws.onopen = _onOpen;
            _ws.onmessage = _onMessage;
            _ws.onclose = _onClose;
            _ws.onerror = _onError;
        } catch (err) {
            console.error('[WS] Connect error:', err);
            _callbacks.onError(err.message);
        }
    }

    // ─── Reconnect using saved session ───
    function reconnect() {
        const session = AppStorage.getSession();
        if (!session.wsUrl) return false;

        // Validate URL before attempting connection
        try {
            new URL(session.wsUrl);
        } catch {
            console.error('[WS] Invalid saved wsUrl:', session.wsUrl);
            return false;
        }

        _config = {
            url: session.wsUrl,
            studentName: session.studentName || 'Học sinh',
            studentCode: session.studentCode || 'HS001'
        };
        _callbacks = {
            onConnected: () => emit('connected', {}),
            onError: (err) => emit('error', err),
            onDisconnected: () => emit('disconnected', {})
        };

        _doConnect();
        return true;
    }

    function _onOpen() {
        console.log('[WS] Connected!');
        _reconnectAttempts = 0;

        // Send JOIN message
        send({
            type: 'join',
            name: _config.studentName,
            code: _config.studentCode,
            device: _getDeviceInfo(),
            version: '1.0.0'
        });
    }

    function _onMessage(event) {
        try {
            const msg = JSON.parse(event.data);
            console.log('[WS] Received:', msg.type, msg);

            switch (msg.type) {
                case 'ack':
                    _isConnected = true;
                    _startHeartbeat();
                    _callbacks.onConnected(msg);
                    emit('connected', msg);
                    break;

                case 'cmd':
                    _handleCommand(msg);
                    break;

                case 'message':
                    emit('message', msg);
                    break;

                case 'chat':
                    emit('chat', msg);
                    break;

                case 'quiz_start':
                    emit('quiz_start', msg);
                    break;

                case 'quiz_end':
                    emit('quiz_end', msg);
                    break;

                case 'lesson_start':
                    emit('lesson_start', msg);
                    break;

                case 'lesson_stage':
                    emit('lesson_stage', msg);
                    break;

                case 'lesson_focus':
                    emit('lesson_focus', msg);
                    break;

                case 'lesson_end':
                    emit('lesson_end', msg);
                    break;

                case 'hb_ack':
                    // Heartbeat acknowledged
                    break;

                case 'state_sync':
                    emit('state_sync', msg);
                    break;

                case 'lesson_content':
                    emit('lesson_content', msg);
                    break;

                default:
                    if (msg.type === 'data' && msg.raw) {
                        try {
                            const rawMsg = JSON.parse(msg.raw);
                            if (rawMsg.Action === 'SURVEY_START') {
                                const payload = rawMsg.Payload || {};
                                const surveyData = {
                                    id: payload.SurveyId,
                                    question: payload.QuestionText || payload.Title || '',
                                    options: payload.Options || [],
                                    duration: payload.TimeLimitSeconds || 0,
                                    isAnonymous: payload.IsAnonymous !== false
                                };
                                emit('survey_start', surveyData);
                                emit('survey', surveyData);
                                break;
                            } else if (rawMsg.Action === 'SURVEY_END') {
                                emit('survey_end', {});
                                break;
                            }
                        } catch (e) {
                            console.warn('[WS] Raw message JSON parse error:', e);
                        }
                    }
                    emit('data', msg);
                    break;
            }
        } catch (err) {
            console.warn('[WS] Parse error:', err, event.data);
        }
    }

    // ─── Command Deduplication (2s window — matches WPF _lastCommandHash) ───
    let _lastCmdHash = '';
    let _lastCmdTime = 0;
    const CMD_DEDUP_WINDOW = 2000;

    function _getCmdHash(msg) {
        return `${msg.action || ''}|${msg.data || ''}|${msg.target || ''}|${msg.toolId || ''}`;
    }

    function _handleCommand(msg) {
        const action = msg.action || '';
        console.log('[WS] Command:', action);

        // Send ACK back to teacher immediately if command has an ID
        if (msg.id) {
            send({
                type: 'cmd_ack',
                commandId: msg.id,
                studentCode: _config.studentCode,
                status: 'SUCCESS'
            });
        }

        // Dedup: skip if same command within 2s
        const hash = _getCmdHash(msg);
        const now = Date.now();
        if (hash === _lastCmdHash && (now - _lastCmdTime) < CMD_DEDUP_WINDOW) {
            console.log('[WS] Dedup: skipping duplicate command', action);
            return;
        }
        _lastCmdHash = hash;
        _lastCmdTime = now;

        switch (action) {
            // ═══ LOCK / UNLOCK / BLACK SCREEN ═══
            case 'LOCK':
            case 'LOCK_SCREEN':
                emit('lock_screen', { type: 'lock', title: msg.title || '🔒 Màn hình đã bị khóa', subtitle: msg.subtitle || 'GV đã khóa màn hình. Vui lòng chờ.' });
                break;
            case 'UNLOCK':
            case 'UNLOCK_SCREEN':
                emit('unlock_screen', {});
                break;
            case 'BLACK_SCREEN':
                emit('lock_screen', { type: 'black', title: msg.title || '📴 Tắt màn hình', subtitle: msg.subtitle || 'GV đã tắt màn hình. Hãy chú ý lên bảng.' });
                break;

            // ═══ SILENCE ═══
            case 'SILENCE':
                emit('silence', { active: true });
                break;
            case 'CLEAR_SILENCE':
                emit('silence', { active: false });
                break;

            // ═══ TEACHER WARNING ═══
            case 'TEACHER_WARNING':
                emit('teacher_warning', {
                    message: msg.message || msg.data || '⚠️ GV yêu cầu: Hãy tập trung!',
                    duration: msg.duration || 10
                });
                break;
            case 'CLEAR_WARNING':
                emit('clear_warning', {});
                break;

            // ═══ NOTICE POPUP ═══
            case 'NOTICE':
                emit('notice', {
                    title: msg.title || 'Thông báo',
                    body: msg.body || msg.data || '',
                    noticeType: msg.noticeType || 'info', // info, warning, urgent, celebrate
                    duration: msg.duration || 30
                });
                break;

            // ═══ BROADCAST (Screen Cast) ═══
            case 'BROADCAST_START':
                emit('broadcast_start', {
                    resolution: msg.resolution || '1920×1080',
                    fps: msg.fps || '30 fps'
                });
                break;
            case 'BROADCAST_STOP':
                emit('broadcast_stop', {});
                break;

            // ═══ SCREEN BROADCAST (Whiteboard) ═══
            case 'SCREEN_BROADCAST':
            case 'SCREEN_BROADCAST_START':
                emit('screen_broadcast', { 
                    imageUrl: msg.imageUrl || msg.data || '',
                    isForce: msg.isForce || (msg.data && msg.data.includes('FORCE_WATCH')) || false
                });
                break;
            case 'SCREEN_BROADCAST_UPDATE':
                emit('screen_broadcast_update', { imageUrl: msg.imageUrl || msg.data || '' });
                break;
            case 'SCREEN_BROADCAST_STOP':
                emit('screen_broadcast_stop', {});
                break;
            case 'WHITEBOARD_DRAW':
                emit('whiteboard_draw', {
                    color: msg.color,
                    width: msg.width,
                    points: msg.points,
                    teacherWidth: msg.teacherWidth,
                    teacherHeight: msg.teacherHeight
                });
                break;
            case 'WHITEBOARD_CLEAR':
                emit('whiteboard_clear', {});
                break;

            case 'WHITEBOARD_SHAPE':
                var parts = msg.data ? msg.data.split('|') : [];
                if (parts.length < 7 && msg.shapeType) {
                    var shapeData = {
                        shapeType: msg.shapeType,
                        coords: msg.coords,
                        color: msg.color,
                        strokeWidth: parseFloat(msg.strokeWidth),
                        teacherWidth: parseFloat(msg.teacherWidth),
                        teacherHeight: parseFloat(msg.teacherHeight || 0)
                    };
                    if (typeof handleWhiteboardShape === 'function') {
                        handleWhiteboardShape(shapeData);
                    }
                } else if (parts.length >= 7) {
                    var shapeData = {
                        shapeType: parts[2],
                        coords: parts[3],
                        color: parts[4],
                        strokeWidth: parseFloat(parts[5]),
                        teacherWidth: parseFloat(parts[6]),
                        teacherHeight: parts.length > 7 ? parseFloat(parts[7]) : 0
                    };
                    if (typeof handleWhiteboardShape === 'function') {
                        handleWhiteboardShape(shapeData);
                    }
                }
                break;

            case 'WHITEBOARD_TEXT':
                var parts = msg.data ? msg.data.split('|') : [];
                if (parts.length < 6 && msg.text) {
                    var textData = {
                        text: msg.text,
                        position: msg.position,
                        color: msg.color,
                        fontSize: parseFloat(msg.fontSize),
                        teacherWidth: parseFloat(msg.teacherWidth || 0),
                        teacherHeight: parseFloat(msg.teacherHeight || 0)
                    };
                    if (typeof handleWhiteboardText === 'function') {
                        handleWhiteboardText(textData);
                    }
                } else if (parts.length >= 6) {
                    var textData = {
                        text: parts[2],
                        position: parts[3],
                        color: parts[4],
                        fontSize: parseFloat(parts[5]),
                        teacherWidth: parts.length > 6 ? parseFloat(parts[6]) : 0,
                        teacherHeight: parts.length > 7 ? parseFloat(parts[7]) : 0
                    };
                    if (typeof handleWhiteboardText === 'function') {
                        handleWhiteboardText(textData);
                    }
                }
                break;

            case 'BOARD_INTERACTIVE_ON':
                var parts = msg.data ? msg.data.split('|') : [];
                if (typeof setBoardInteractiveMode === 'function') {
                    setBoardInteractiveMode(true, parts.length > 2 ? parts[2] : 'pen');
                }
                break;

            case 'BOARD_INTERACTIVE_OFF':
                if (typeof setBoardInteractiveMode === 'function') {
                    setBoardInteractiveMode(false);
                }
                break;

            case 'BOARD_SET_PEN':
                var parts = msg.data ? msg.data.split('|') : [];
                if (typeof setBoardPenConfig === 'function') {
                    setBoardPenConfig(parts.length > 2 ? parts[2] : null, parts.length > 3 ? parseFloat(parts[3]) : null);
                }
                break;

            case 'BOARD_SET_ERASER':
                var parts = msg.data ? msg.data.split('|') : [];
                if (typeof setBoardEraserSize === 'function') {
                    setBoardEraserSize(parts.length > 2 ? parseFloat(parts[2]) : 30);
                }
                break;

            // ═══ FILE BROADCAST ═══
            case 'FILE_BROADCAST':
            case 'FILE_BROADCAST_START':
                emit('file_broadcast', {
                    fileType: msg.fileType || 'FILE',
                    fileName: msg.fileName || '',
                    fileUrl: msg.fileUrl || msg.data || '',
                    fileSize: msg.fileSize || ''
                });
                break;
            case 'FILE_BROADCAST_STOP':
                emit('file_broadcast_stop', {});
                break;
            case 'FILE_FOCUS':
                emit('file_focus', { page: msg.page || msg.target || 1 });
                break;

            // ═══ SURVEY ═══
            case 'SURVEY_QUESTION':
            case 'SURVEY_CUSTOM':
                {
                    const surveyInfo = {
                        id: msg.target || `survey_${Date.now()}`,
                        question: msg.question || msg.data || '',
                        options: msg.options || ['👍 Đồng ý', '👎 Không đồng ý', '🤔 Có thể'],
                        duration: msg.duration || 0
                    };
                    emit('survey', surveyInfo);
                    emit('survey_start', surveyInfo);
                }
                break;
            case 'SURVEY_END':
                emit('survey_end', {});
                break;

            // ═══ QUIZ ═══
            case 'QUIZ_START':
                emit('quiz_start', msg);
                break;
            case 'QUIZ_END':
                emit('quiz_end', msg);
                break;
            case 'QUIZ_FOCUS':
                emit('quiz_focus', { questionIndex: msg.questionIndex || msg.target || 1 });
                break;
            case 'QUIZ_UNFOCUS':
                emit('quiz_unfocus', {});
                break;
            case 'QUIZ_REVIEW':
                emit('quiz_review', { data: msg.data || msg.reviewData || '' });
                break;
            case 'QUIZ_REVIEW_FOCUS':
                emit('quiz_review_focus', { questionIndex: msg.questionIndex || msg.target || 1 });
                break;

            // ═══ LESSON ═══
            case 'LESSON_START':
                emit('lesson_start', msg);
                break;
            case 'LESSON_STAGE':
                emit('lesson_stage', msg);
                break;
            case 'LESSON_FOCUS':
                emit('lesson_focus', msg);
                break;
            case 'LESSON_UNFOCUS':
                emit('lesson_unfocus', {});
                break;
            case 'LESSON_CONTENT':
                emit('lesson_content', msg);
                break;
            case 'LESSON_END':
                emit('lesson_end', msg);
                break;

            // ═══ HOMEWORK / ASSIGNMENT ═══
            case 'HOMEWORK':
                emit('homework', msg);
                break;
            case 'ASSIGNMENT':
                emit('homework', {
                    ...msg,
                    title: msg.description || msg.data || 'Bài tập mới',
                    dueDate: msg.deadline || ''
                });
                break;

            // ═══ TOOL FOCUS ═══
            case 'TOOL_FOCUS':
                emit('tool_focus', { toolId: msg.toolId || msg.target || '' });
                break;
            case 'TOOL_UNFOCUS':
                emit('tool_unfocus', {});
                break;
            case 'TOOL_SECTION_FOCUS':
                emit('tool_section_focus', { toolId: msg.toolId || '', sectionId: msg.sectionId || '' });
                break;
            case 'TOOL_SECTION_UNFOCUS':
                emit('tool_section_unfocus', {});
                break;

            // ═══ WEB CONTROL ═══
            case 'BLOCK_WEB_ON':
                emit('block_web_on', {});
                break;
            case 'BLOCK_WEB_OFF':
                emit('block_web_off', {});
                break;
            case 'WEB_WHITELIST_ON':
                emit('web_whitelist_on', {});
                break;
            case 'WEB_WHITELIST_OFF':
                emit('web_whitelist_off', {});
                break;
            case 'WHITELIST_ADD':
                emit('whitelist_add', { urls: msg.target || msg.data || '' });
                break;

            // ═══ OPEN URL ═══
            case 'OPEN_URL':
                emit('open_url', { url: msg.url || msg.data || '' });
                break;

            // ═══ KILL APPS — N/A for web ═══
            case 'KILL_APPS':
                emit('notice', {
                    title: '🚫 Kiểm tra ứng dụng',
                    body: 'GV đã kiểm tra ứng dụng trên thiết bị.',
                    noticeType: 'info',
                    duration: 5
                });
                break;

            // ═══ CLEAR ALL ═══
            case 'CLEAR_ALL':
                emit('clear_all', {});
                break;

            // ═══ SCREENSHOT — capture DOM and send back ═══
            case 'REQUEST_SCREENSHOT':
                // Bypass throttle for teacher-requested captures — always respond
                _lastScreenshotTime = 0;
                _captureAndSendScreenshot();
                break;

            // ═══ GAMIFICATION ═══
            case 'BADGE_EARNED':
                emit('badge_earned', msg);
                break;
            case 'LEADERBOARD_UPDATE':
                emit('leaderboard_update', msg);
                break;
            case 'XP_EARNED':
                emit('xp_earned', msg);
                break;

            // ═══ STATE SYNC — Late-join recovery ═══
            case 'STATE_SYNC':
                emit('state_sync', msg);
                break;

            default:
                console.warn('[WS] Unknown command:', action);
                emit('command', msg);
                break;
        }
    }

    function _onClose(event) {
        console.log('[WS] Disconnected', event.code, event.reason);
        _isConnected = false;
        _stopHeartbeat();

        _callbacks.onDisconnected();
        emit('disconnected', { code: event.code, reason: event.reason });

        // Auto-reconnect if not a clean close
        if (event.code !== 1000 && _reconnectAttempts < MAX_RECONNECT) {
            const delay = RECONNECT_BASE_DELAY * Math.pow(1.5, _reconnectAttempts);
            console.log(`[WS] Reconnecting in ${delay}ms (attempt ${_reconnectAttempts + 1})...`);
            _reconnectTimer = setTimeout(() => {
                _reconnectAttempts++;
                _doConnect();
            }, delay);
        }
    }

    function _onError(error) {
        console.error('[WS] Error:', error);
        _callbacks.onError('Không thể kết nối WebSocket');
    }

    // ─── Send ───
    function send(msg) {
        if (_ws && _ws.readyState === WebSocket.OPEN) {
            _ws.send(JSON.stringify(msg));
            return true;
        }
        console.warn('[WS] Not connected, cannot send');
        return false;
    }

    // ─── Heartbeat ───
    function _startHeartbeat() {
        _stopHeartbeat();
        _heartbeatTimer = setInterval(() => {
            send({
                type: 'heartbeat',
                code: _config.studentCode,
                status: 'online',
                activeApp: 'QASmartClass-Web',
                timestamp: Date.now()
            });
        }, HEARTBEAT_INTERVAL);

        // ═══ Proactively push screenshot every 10s (real-time monitoring) ═══
        setTimeout(() => _captureAndSendScreenshot(), 3000);
        _screenshotTimer = setInterval(() => _captureAndSendScreenshot(), 10000);
    }

    function _stopHeartbeat() {
        if (_heartbeatTimer) {
            clearInterval(_heartbeatTimer);
            _heartbeatTimer = null;
        }
        if (_screenshotTimer) {
            clearInterval(_screenshotTimer);
            _screenshotTimer = null;
        }
    }

    // ─── Convenience Methods ───
    function sendHandRaise(raised) {
        return send({ type: 'hand_raise', raised: raised });
    }

    function sendChat(text, channel = 'ALL') {
        return send({
            type: 'chat',
            code: _config.studentCode,
            channel: channel,
            text: text
        });
    }

    function sendQuizAnswer(quizId, answers) {
        return send({
            type: 'quiz_answer',
            quizId: quizId,
            answers: answers
        });
    }

    function sendQuestion(text) {
        return send({ type: 'student_question', text: text });
    }

    // ─── Disconnect ───
    function disconnect() {
        _stopHeartbeat();
        if (_reconnectTimer) clearTimeout(_reconnectTimer);
        if (_ws) {
            _ws.close(1000, 'Student disconnected');
            _ws = null;
        }
        _isConnected = false;
    }

    // ─── Device Info ───
    function _getDeviceInfo() {
        const ua = navigator.userAgent;
        let device = 'Unknown';
        if (/iPad/.test(ua)) device = 'iPad';
        else if (/Android/.test(ua) && !/Mobile/.test(ua)) device = 'Android Tablet';
        else if (/Android/.test(ua)) device = 'Android Phone';
        else if (/iPhone/.test(ua)) device = 'iPhone';
        else if (/Windows/.test(ua)) device = 'Windows';
        else if (/Mac/.test(ua)) device = 'Mac';
        return device;
    }

    // ─── Screenshot Capture ───
    let _lastScreenshotTime = 0;
    const SCREENSHOT_THROTTLE = 1500;  // 1.5s min between auto-captures
    const SCREENSHOT_WIDTH   = 640;    // 2x resolution for crisp detail view
    const SCREENSHOT_HEIGHT  = 360;
    const SCREENSHOT_QUALITY = 0.80;   // Higher quality → clearer image

    async function _captureAndSendScreenshot() {
        const now = Date.now();
        if (now - _lastScreenshotTime < SCREENSHOT_THROTTLE) return;
        _lastScreenshotTime = now;

        try {
            let dataUrl = null;

            if (typeof html2canvas === 'function') {
                // ═══ Primary: html2canvas (CDN) ═══
                const target = document.getElementById('appRoot') || document.body;
                const canvas = await html2canvas(target, {
                    scale: 0.5, logging: false, useCORS: true,
                    allowTaint: true, backgroundColor: '#0B1929',
                    width: target.scrollWidth, height: target.scrollHeight
                });
                const thumbCanvas = document.createElement('canvas');
                thumbCanvas.width = SCREENSHOT_WIDTH;
                thumbCanvas.height = SCREENSHOT_HEIGHT;
                const ctx = thumbCanvas.getContext('2d');
                ctx.drawImage(canvas, 0, 0, SCREENSHOT_WIDTH, SCREENSHOT_HEIGHT);
                dataUrl = thumbCanvas.toDataURL('image/jpeg', SCREENSHOT_QUALITY);
            } else {
                // ═══ Fallback: SVG-foreignObject method (no external library needed) ═══
                const el = document.getElementById('mainContent') || document.getElementById('appRoot') || document.body;
                const rect = el.getBoundingClientRect();
                const svgData = `<svg xmlns='http://www.w3.org/2000/svg' width='${rect.width}' height='${rect.height}'>
                    <foreignObject width='100%' height='100%'>
                        <div xmlns='http://www.w3.org/1999/xhtml' style='background:#0B1929'>
                            ${el.innerHTML}
                        </div>
                    </foreignObject>
                </svg>`;
                const blob = new Blob([svgData], { type: 'image/svg+xml;charset=utf-8' });
                const url = URL.createObjectURL(blob);
                const img = new Image();
                await new Promise((res, rej) => { img.onload = res; img.onerror = rej; img.src = url; });
                const canvas = document.createElement('canvas');
                canvas.width = SCREENSHOT_WIDTH; canvas.height = SCREENSHOT_HEIGHT;
                canvas.getContext('2d').drawImage(img, 0, 0, SCREENSHOT_WIDTH, SCREENSHOT_HEIGHT);
                URL.revokeObjectURL(url);
                dataUrl = canvas.toDataURL('image/jpeg', SCREENSHOT_QUALITY);
            }

            const base64 = dataUrl?.split(',')[1];
            if (base64 && base64.length > 100) {
                send({ type: 'screenshot', code: _config.studentCode, data: base64 });
                console.log(`[WS] Screenshot sent: ${(base64.length / 1024).toFixed(1)}KB`);
            }
        } catch (err) {
            console.warn('[WS] Screenshot capture error:', err.message);
        }
    }

    function isConnected() { return _isConnected; }
    function getConfig() { return { ..._config }; }
    function captureScreenshot() { return _captureAndSendScreenshot(); } // expose for manual trigger

    return {
        connect, reconnect, disconnect, send,
        sendHandRaise, sendChat, sendQuizAnswer, sendQuestion,
        captureScreenshot,
        on, off, emit, isConnected, getConfig
    };
})();
