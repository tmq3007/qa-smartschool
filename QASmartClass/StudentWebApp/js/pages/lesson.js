/* ═══════════════════════════════════════════════════════════
   QA SmartClass — Lesson Sync Module
   Displays lesson content stages synchronized with teacher
   ═══════════════════════════════════════════════════════════ */

const LessonManager = (() => {
    let _lessonData = null;
    let _currentStage = 0;
    let _focusedBlock = null;

    const STAGE_NAMES = [
        '', // index 0 unused
        '📌 Mở đầu — Khởi động',
        '📖 Hình thành kiến thức',
        '🔬 Luyện tập',
        '🎯 Vận dụng',
        '📝 Tổng kết — Đánh giá',
        '📚 Bài tập về nhà'
    ];

    const STAGE_COLORS = [
        '',
        'linear-gradient(135deg, #6366F1, #818CF8)', // Mở đầu
        'linear-gradient(135deg, #2563EB, #3B82F6)', // Hình thành
        'linear-gradient(135deg, #059669, #10B981)', // Luyện tập
        'linear-gradient(135deg, #D97706, #F59E0B)', // Vận dụng
        'linear-gradient(135deg, #7C3AED, #8B5CF6)', // Tổng kết
        'linear-gradient(135deg, #DC2626, #EF4444)'  // BTVN
    ];

    // ─── Initialize event listeners ───
    function init() {
        StudentConnection.on('lesson_start', handleLessonStart);
        StudentConnection.on('lesson_stage', handleStageChange);
        StudentConnection.on('lesson_focus', handleFocusContent);
        StudentConnection.on('lesson_content', handleLessonContent);
        StudentConnection.on('lesson_end', handleLessonEnd);
        StudentConnection.on('lesson_unfocus', handleLessonUnfocus);
    }

    // ═══ LESSON START ═══
    function handleLessonStart(data) {
        _lessonData = {
            id: data.target || data.lessonId || 0,
            title: data.title || 'Bài giảng',
            subject: data.subject || '',
            stages: data.stages || []
        };
        _currentStage = 0;

        renderLessonWaiting();
    }

    // ═══ STAGE CHANGE ═══
    function handleStageChange(data) {
        const stage = parseInt(data.target || data.stage || 0);
        if (stage >= 1 && stage <= 6) {
            _currentStage = stage;
            renderStage(stage, data);
        }
    }

    // ═══ FOCUS CONTENT BLOCK ═══
    function handleFocusContent(data) {
        const sortOrder = parseInt(data.target || data.sortOrder || 0);
        const contentType = data.data || data.contentType || '';
        const content = data.content || '';
        _focusedBlock = { sortOrder, contentType };

        console.log('[Lesson] handleFocusContent:', sortOrder, contentType, 'hasContent:', !!content);

        // Navigate to lesson page FIRST
        if (typeof navigateTo === 'function') {
            const currentPage = document.querySelector('.page.active');
            if (!currentPage || currentPage.id !== 'page-lesson') {
                navigateTo('lesson');
            }
        }

        // If content is included inline → render immediately (skip waiting)
        if (content && content.length > 0) {
            const lessonTitle = data.lessonTitle || _lessonData?.title || '';
            _focusedBlock.content = content;
            _retryRender(sortOrder, contentType, content, lessonTitle, 0);
            return;
        }

        // Try to highlight existing block first
        const existingBlocks = document.querySelectorAll('.lesson-block');
        if (existingBlocks.length > 0) {
            existingBlocks.forEach(el => el.classList.remove('focused'));
            const focusEl = document.getElementById(`lesson-block-${sortOrder}`);
            if (focusEl) {
                focusEl.classList.add('focused');
                focusEl.scrollIntoView({ behavior: 'smooth', block: 'center' });
                return;
            }
        }

        // Show waiting card — lesson_content event will follow with actual data
        renderFocusWaiting(sortOrder, contentType);
    }

    // ═══ LESSON CONTENT — Real content from teacher ═══
    function handleLessonContent(data) {
        console.log('[Lesson] handleLessonContent received:', data.contentType, 'sort:', data.sortOrder, 'len:', (data.content || '').length);

        const sortOrder = data.sortOrder || 0;
        const contentType = data.contentType || '';
        const content = data.content || '';
        const lessonTitle = data.lessonTitle || _lessonData?.title || '';
        const lessonSubject = data.lessonSubject || _lessonData?.subject || '';

        _focusedBlock = { sortOrder, contentType, content };

        // Update lesson metadata if available
        if (lessonTitle) {
            if (!_lessonData) _lessonData = {};
            _lessonData.title = lessonTitle;
            _lessonData.subject = lessonSubject;
        }

        // Navigate to lesson page FIRST
        if (typeof navigateTo === 'function') {
            const currentPage = document.querySelector('.page.active');
            if (!currentPage || currentPage.id !== 'page-lesson') {
                navigateTo('lesson');
            }
        }

        // Retry render with robust DOM check
        _retryRender(sortOrder, contentType, content, lessonTitle, 0);
    }

    // ═══ ROBUST RETRY RENDER — wait for DOM up to 1s ═══
    function _retryRender(sortOrder, contentType, content, lessonTitle, attempt) {
        const page = document.getElementById('page-lesson');
        if (page) {
            renderFocusedContent(page, sortOrder, contentType, content, lessonTitle);
            return;
        }
        if (attempt < 5) {
            console.warn(`[Lesson] page-lesson not found, retry ${attempt + 1}/5...`);
            setTimeout(() => _retryRender(sortOrder, contentType, content, lessonTitle, attempt + 1), 200);
        } else {
            console.error('[Lesson] page-lesson not found after 5 retries');
        }
    }

    // ═══ RENDER: Focused content with actual data ═══
    function renderFocusedContent(page, sortOrder, contentType, content, lessonTitle) {
        const typeLabels = {
            'Text':       { icon: '📄', label: 'Văn bản',      color: '#2563EB', bg: '#EFF6FF' },
            'Image':      { icon: '🖼️', label: 'Hình ảnh',     color: '#059669', bg: '#ECFDF5' },
            'Video':      { icon: '🎬', label: 'Video',        color: '#D97706', bg: '#FFFBEB' },
            'Simulation': { icon: '🔬', label: 'Mô phỏng',    color: '#7C3AED', bg: '#F5F3FF' },
            'PDF':        { icon: '📑', label: 'Tài liệu PDF', color: '#DC2626', bg: '#FEF2F2' },
            'Quiz':       { icon: '❓', label: 'Câu hỏi',      color: '#0D9488', bg: '#F0FDFA' }
        };
        const info = typeLabels[contentType] || { icon: '📋', label: contentType || 'Nội dung', color: '#6366F1', bg: '#EEF2FF' };

        // Build content HTML based on type
        let contentHtml = '';

        switch (contentType) {
            case 'Text':
                contentHtml = renderTextContent(content, info);
                break;
            case 'Image':
                contentHtml = renderImageContent(content, info);
                break;
            case 'PDF':
                contentHtml = renderPdfContent(content, info);
                break;
            case 'Video':
                contentHtml = renderVideoContent(content, info);
                break;
            case 'Quiz':
                contentHtml = renderQuizContent(content, info);
                break;
            default:
                contentHtml = renderTextContent(content, info);
                break;
        }

        page.innerHTML = `
            <!-- Focus Banner (matches WPF blue banner) -->
            <div class="animate-fade-in" style="
                background: linear-gradient(135deg, #1565C0, #1E88E5);
                border-radius: 12px; padding: 12px 16px; margin-bottom: 14px;
                display: flex; align-items: center; gap: 10px; color: white;
            ">
                <span style="font-size: 20px">🎯</span>
                <div style="flex: 1;">
                    <div style="font-weight: 700; font-size: 14px;">
                        GV yêu cầu tập trung — ${info.label}
                    </div>
                    <div style="font-size: 11px; opacity: 0.85; margin-top: 2px;">
                        Hãy đọc kỹ nội dung #${sortOrder} được đánh dấu bên dưới
                    </div>
                </div>
                <div style="
                    background: rgba(255,255,255,0.2); padding: 3px 10px; border-radius: 12px;
                    font-size: 10px; font-weight: 600; animation: pulse 2s ease-in-out infinite;
                ">● LIVE</div>
                <button onclick="document.getElementById('page-lesson').querySelector('.focus-banner')?.remove()"
                    style="background:rgba(255,255,255,0.2);border:none;color:white;width:28px;height:28px;
                    border-radius:50%;cursor:pointer;font-size:14px;display:flex;align-items:center;
                    justify-content:center;flex-shrink:0" title="Đóng banner">✕</button>
            </div>

            <!-- Content Block (matches WPF styled block) -->
            <div class="lesson-block focused animate-fade-in" id="lesson-block-${sortOrder}" style="
                border: 3px solid ${info.color}; border-radius: 14px;
                background: white; overflow: hidden;
                box-shadow: 0 4px 24px ${info.color}22, 0 0 0 4px ${info.color}11;
            ">
                <!-- Type Header Bar -->
                <div style="
                    background: ${info.bg}; padding: 10px 16px;
                    display: flex; align-items: center; justify-content: space-between;
                    border-bottom: 1px solid ${info.color}22;
                ">
                    <div style="display:flex;align-items:center;gap:8px;">
                        <span style="font-size:16px">${info.icon}</span>
                        <span style="font-weight:700;font-size:13px;color:${info.color}">${info.label}</span>
                    </div>
                    <div style="
                        font-size:11px; color:${info.color}; font-weight:600;
                        background:white; padding:2px 10px; border-radius:10px;
                        border:1px solid ${info.color}33;
                    ">Thứ tự: ${sortOrder}</div>
                </div>
                <!-- Content Body -->
                <div style="padding: 20px;">
                    ${contentHtml}
                </div>
            </div>

            <style>
                @keyframes pulse {
                    0%, 100% { opacity: 1; }
                    50% { opacity: 0.5; }
                }
            </style>
        `;
    }

    // ─── Text Content Renderer (Rich Formatting — parity with WPF BuildWhiteboardContent) ───
    function renderTextContent(content, info) {
        if (!content) return '<p style="color:var(--text-muted)">Không có nội dung</p>';

        const raw = content.replace(/\\n/g, '\n');
        const lines = raw.split('\n');
        let html = '';

        for (const rawLine of lines) {
            const line = rawLine.trim();
            if (!line) { html += '<div style="height:8px"></div>'; continue; }

            const safe = escapeHtml(line);

            // ── Heading: ALL UPPERCASE (>3 chars) or starts with Roman/Number prefix ──
            const isHeading = (line === line.toUpperCase() && line.length > 3 && /[A-ZÀ-Ỹ]/.test(line))
                || /^(I{1,3}V?|VI{0,3}|[0-9]+)\.\s/.test(line)
                || /^#{1,3}\s/.test(line);

            if (isHeading) {
                html += `<div style="font-size:1.3rem;font-weight:800;color:#1A237E;margin:14px 0 6px;
                    letter-spacing:0.02em;border-left:4px solid #1976D2;padding-left:12px">${safe.replace(/^#+\s*/, '')}</div>`;
                continue;
            }

            // ── Sub-heading: starts with lowercase roman or a), b) ──
            if (/^[a-z]\)|^[ivx]+\)/.test(line)) {
                html += `<div style="font-size:1.1rem;font-weight:600;color:#283593;margin:8px 0 4px 8px">${safe}</div>`;
                continue;
            }

            // ── Checkmark / Cross lines ──
            if (/^[✅✔☑]/.test(line)) {
                html += `<div style="display:flex;align-items:flex-start;gap:8px;margin:4px 0 4px 12px;font-size:1.1rem;color:#2E7D32">
                    <span style="flex-shrink:0">✅</span><span style="line-height:1.7">${safe.replace(/^[✅✔☑]\s*/, '')}</span></div>`;
                continue;
            }
            if (/^[✗✘❌✖]/.test(line)) {
                html += `<div style="display:flex;align-items:flex-start;gap:8px;margin:4px 0 4px 12px;font-size:1.1rem;color:#C62828">
                    <span style="flex-shrink:0">✗</span><span style="line-height:1.7">${safe.replace(/^[✗✘❌✖]\s*/, '')}</span></div>`;
                continue;
            }

            // ── Bullet points ──
            if (/^[•\-→▸▹●◦]\s/.test(line)) {
                html += `<div style="display:flex;align-items:flex-start;gap:8px;margin:3px 0 3px 16px;font-size:1.1rem;color:#37474F">
                    <span style="color:#1976D2;flex-shrink:0;font-weight:700">•</span>
                    <span style="line-height:1.7">${safe.replace(/^[•\-→▸▹●◦]\s*/, '')}</span></div>`;
                continue;
            }

            // ── Example / VD prefix ──
            if (/^(VD|Ví dụ|Example)[\s:]/i.test(line)) {
                html += `<div style="font-size:1.0rem;color:#4527A0;margin:6px 0 4px 12px;font-style:italic;
                    background:#F3E5F5;padding:8px 12px;border-radius:8px;border-left:3px solid #7B1FA2">${safe}</div>`;
                continue;
            }

            // ── Default paragraph ──
            html += `<div style="font-size:1.1rem;line-height:1.8;color:#212121;margin:3px 0">${safe}</div>`;
        }

        return `<div class="lesson-text-content">${html}</div>`;
    }

    // ─── Image Content Renderer ───
    function renderImageContent(content, info) {
        if (!content) return '<p style="color:var(--text-muted)">Không có hình ảnh</p>';

        // Content might be a URL or base64
        const isBase64 = content.startsWith('data:') || content.length > 500;
        const src = isBase64 ? (content.startsWith('data:') ? content : `data:image/png;base64,${content}`) : content;

        return `
            <div style="text-align: center;">
                <img src="${src}" alt="Hình ảnh bài giảng"
                     style="max-width: 100%; max-height: 70vh; border-radius: 10px;
                            box-shadow: 0 2px 12px rgba(0,0,0,0.1);"
                     onerror="this.outerHTML='<p style=\\'color:var(--text-muted)\\'>⚠️ Không thể tải hình ảnh</p>'"
                >
            </div>
        `;
    }

    // ─── PDF Content Renderer ───
    function renderPdfContent(content, info) {
        return `
            <div style="text-align: center; padding: 20px;">
                <div style="font-size: 48px; margin-bottom: 12px;">📑</div>
                <p style="font-weight: 600; margin-bottom: 8px;">Tài liệu PDF</p>
                <p style="font-size: 13px; color: var(--text-secondary); margin-bottom: 16px;">
                    GV đang chiếu tài liệu PDF trên màn hình lớn
                </p>
                ${content ? `<div style="font-size: 14px; line-height: 1.8; text-align: left;
                    background: var(--gray-50); padding: 16px; border-radius: 10px;">
                    ${escapeHtml(content).replace(/\n/g, '<br>')}
                </div>` : ''}
            </div>
        `;
    }

    // ─── Video Content Renderer ───
    function renderVideoContent(content, info) {
        if (content && (content.includes('youtube') || content.includes('youtu.be'))) {
            const videoId = extractYoutubeId(content);
            if (videoId) {
                return `
                    <div style="position: relative; padding-bottom: 56.25%; height: 0; overflow: hidden; border-radius: 10px;">
                        <iframe src="https://www.youtube.com/embed/${videoId}"
                                style="position: absolute; top: 0; left: 0; width: 100%; height: 100%; border: none;"
                                allowfullscreen></iframe>
                    </div>
                `;
            }
        }

        return `
            <div style="text-align: center; padding: 20px;">
                <div style="font-size: 48px; margin-bottom: 12px;">🎬</div>
                <p style="font-weight: 600;">Video bài giảng</p>
                <p style="font-size: 13px; color: var(--text-secondary);">
                    ${content ? escapeHtml(content) : 'GV đang phát video trên màn hình lớn'}
                </p>
            </div>
        `;
    }

    // ─── Quiz Content Renderer ───
    function renderQuizContent(content, info) {
        return `
            <div style="
                background: linear-gradient(135deg, #F0FDFA, #CCFBF1);
                padding: 20px; border-radius: 12px;
                border-left: 4px solid ${info.color};
            ">
                <div style="font-weight: 700; font-size: 15px; margin-bottom: 8px; color: ${info.color};">
                    ❓ Câu hỏi từ giáo viên
                </div>
                <div style="font-size: 14px; line-height: 1.8;">
                    ${content ? escapeHtml(content).replace(/\n/g, '<br>') : 'Hãy chú ý câu hỏi trên bảng'}
                </div>
            </div>
        `;
    }

    // ─── YouTube ID extractor ───
    function extractYoutubeId(url) {
        const match = url.match(/(?:youtube\.com\/(?:watch\?v=|embed\/)|youtu\.be\/)([a-zA-Z0-9_-]{11})/);
        return match ? match[1] : null;
    }

    // ═══ RENDER: Focus waiting (before content arrives) ═══
    function renderFocusWaiting(sortOrder, contentType) {
        const page = document.getElementById('page-lesson');
        if (!page) return;

        const typeLabels = {
            'Text': { icon: '📄', label: 'Văn bản', color: '#2563EB', bg: '#DBEAFE' },
            'Image': { icon: '🖼️', label: 'Hình ảnh', color: '#059669', bg: '#D1FAE5' },
            'Video': { icon: '🎬', label: 'Video', color: '#D97706', bg: '#FEF3C7' },
            'Simulation': { icon: '🔬', label: 'Mô phỏng', color: '#7C3AED', bg: '#EDE9FE' },
            'PDF': { icon: '📑', label: 'Tài liệu PDF', color: '#DC2626', bg: '#FEE2E2' },
            'Quiz': { icon: '❓', label: 'Câu hỏi', color: '#0D9488', bg: '#CCFBF1' }
        };
        const info = typeLabels[contentType] || { icon: '📋', label: contentType || 'Nội dung', color: '#6366F1', bg: '#E0E7FF' };

        page.innerHTML = `
            <div class="lesson-focus-card animate-fade-in" style="
                background: linear-gradient(135deg, ${info.bg}, white);
                border: 3px solid ${info.color};
                border-radius: 20px; padding: 40px 32px;
                text-align: center;
                box-shadow: 0 8px 32px ${info.color}33;
                position: relative; overflow: hidden;
            ">
                <div style="font-size: 48px; margin-bottom: 16px;">${info.icon}</div>
                <div class="spinner" style="margin: 0 auto 16px;"></div>
                <p style="font-weight: 600; color: ${info.color};">
                    Đang tải nội dung ${info.label}...
                </p>
                <p style="font-size: 12px; color: var(--text-muted); margin-top: 4px;">
                    Block #${sortOrder}
                </p>
            </div>
        `;
    }

    // ═══ LESSON UNFOCUS — reset all blocks ═══
    function handleLessonUnfocus() {
        _focusedBlock = null;
        document.querySelectorAll('.lesson-block.focused').forEach(el => {
            el.classList.remove('focused');
            el.style.opacity = '';
            el.style.transform = '';
            el.style.boxShadow = '';
        });
        console.log('[Lesson] Unfocused all blocks');
    }

    // ═══ LESSON END ═══
    function handleLessonEnd(data) {
        const page = document.getElementById('page-lesson');
        if (!page) return;

        page.innerHTML = `
            <div style="text-align:center;padding:60px 20px">
                <div style="font-size:72px;margin-bottom:24px" class="animate-bounce-in">🎉</div>
                <h2 style="margin-bottom:12px">Bài giảng đã kết thúc!</h2>
                <p style="color:var(--text-muted);font-size:14px;margin-bottom:24px">
                    Cảm ơn em đã chú ý lắng nghe
                </p>
                <button class="btn btn-primary" onclick="navigateTo('dashboard')">
                    🏠 Quay về trang chủ
                </button>
            </div>
        `;

        showToast('📖 Bài giảng đã kết thúc', 'info');
        addActivity('📖', 'Bài giảng kết thúc', '#D1FAE5');
    }

    // ═══ RENDER: Waiting for lesson ═══
    function renderLessonWaiting() {
        const page = document.getElementById('page-lesson');
        if (!page) return;

        page.innerHTML = `
            <div class="lesson-header" style="background:linear-gradient(135deg,#1E40AF,#3B82F6);
                 color:white;padding:24px;border-radius:14px;margin-bottom:20px">
                <div style="display:flex;align-items:center;gap:12px">
                    <div style="font-size:32px">📖</div>
                    <div>
                        <h3>${escapeHtml(_lessonData?.title || 'Bài giảng')}</h3>
                        <div style="font-size:12px;opacity:0.8;margin-top:4px">
                            ${_lessonData?.subject || ''} • GV đang chuẩn bị...
                        </div>
                    </div>
                </div>
            </div>

            <div class="lesson-stages" id="lessonStages">
                ${renderStageTimeline()}
            </div>

            <div style="text-align:center;padding:40px;color:var(--text-muted)">
                <div class="spinner" style="margin:0 auto 16px"></div>
                <p>Đang chờ giáo viên bắt đầu...</p>
            </div>
        `;
    }

    // ═══ RENDER: Active stage ═══
    function renderStage(stage, data) {
        const page = document.getElementById('page-lesson');
        if (!page) return;

        const stageName = STAGE_NAMES[stage] || `Giai đoạn ${stage}`;
        const stageColor = STAGE_COLORS[stage] || STAGE_COLORS[1];

        page.innerHTML = `
            <!-- Stage Header -->
            <div class="lesson-stage-header animate-fade-in"
                 style="background:${stageColor};color:white;padding:24px;border-radius:14px;margin-bottom:20px">
                <div style="display:flex;align-items:center;justify-content:space-between">
                    <div style="display:flex;align-items:center;gap:12px">
                        <div style="width:48px;height:48px;border-radius:50%;background:rgba(255,255,255,0.2);
                                    display:flex;align-items:center;justify-content:center;font-size:22px">
                            ${stageName.split(' ')[0]}
                        </div>
                        <div>
                            <h3 style="margin:0">${stageName.split('—')[0]}</h3>
                            <div style="font-size:12px;opacity:0.8;margin-top:2px">
                                ${stageName.includes('—') ? stageName.split('—')[1].trim() : ''}
                            </div>
                        </div>
                    </div>
                    <div style="font-size:13px;font-weight:600;background:rgba(255,255,255,0.2);
                                padding:6px 14px;border-radius:20px">
                        Giai đoạn ${stage}/6
                    </div>
                </div>
            </div>

            <!-- Stage Timeline -->
            <div class="lesson-stages" style="margin-bottom:20px">
                ${renderStageTimeline()}
            </div>

            <!-- Content blocks (placeholder — populated by teacher) -->
            <div id="lessonContent" class="lesson-content">
                ${renderContentBlocks(data)}
            </div>

            <!-- Teacher instruction -->
            <div style="background:var(--primary-50);border:1px solid var(--primary-200);
                        border-radius:12px;padding:16px;margin-top:16px;
                        display:flex;align-items:center;gap:12px">
                <span style="font-size:24px">👨‍🏫</span>
                <div>
                    <div style="font-weight:600;font-size:13px;color:var(--primary-dark)">
                        Hướng dẫn từ giáo viên
                    </div>
                    <div style="font-size:12px;color:var(--text-secondary);margin-top:2px">
                        ${data?.instruction || 'Chú ý lắng nghe và ghi chép'}
                    </div>
                </div>
            </div>
        `;
    }

    // ═══ STAGE TIMELINE BAR ═══
    function renderStageTimeline() {
        let html = '<div style="display:flex;gap:4px;margin-bottom:4px">';

        for (let i = 1; i <= 6; i++) {
            const isActive = i === _currentStage;
            const isPast = i < _currentStage;
            const color = isActive ? 'var(--primary)' :
                          isPast ? 'var(--green)' : 'var(--gray-200)';

            html += `
                <div style="flex:1;height:6px;border-radius:3px;background:${color};
                            transition:background 0.3s ease"></div>
            `;
        }

        html += '</div>';
        html += `<div style="display:flex;justify-content:space-between;font-size:9px;color:var(--text-muted)">
            <span>Mở đầu</span><span>Kiến thức</span><span>Luyện tập</span>
            <span>Vận dụng</span><span>Tổng kết</span><span>BTVN</span>
        </div>`;

        return html;
    }

    // ═══ CONTENT BLOCKS ═══
    function renderContentBlocks(data) {
        const blocks = data?.blocks || data?.content || [];

        if (blocks.length === 0) {
            return `
                <div style="text-align:center;padding:40px;color:var(--text-muted)">
                    <div style="font-size:48px;opacity:0.5;margin-bottom:12px">📋</div>
                    <p>Hãy tập trung vào bảng giáo viên</p>
                    <p style="font-size:12px;margin-top:4px">Nội dung sẽ hiển thị khi GV chia sẻ</p>
                </div>
            `;
        }

        return blocks.map((block, i) => {
            const isFocused = _focusedBlock?.sortOrder === (block.sortOrder || i);

            return `
                <div class="lesson-block card mb-3 ${isFocused ? 'focused' : ''}"
                     id="lesson-block-${block.sortOrder || i}"
                     style="${isFocused ? 'border-color:var(--primary);box-shadow:0 0 0 3px var(--primary-100)' : ''}">
                    ${block.type === 'text' ? `<div style="line-height:1.8">${block.content || ''}</div>` : ''}
                    ${block.type === 'image' ? `<img src="${block.url || ''}" style="max-width:100%;border-radius:8px" alt="">` : ''}
                    ${block.type === 'question' ? `
                        <div style="background:var(--yellow-50);padding:14px;border-radius:10px;border-left:4px solid var(--yellow)">
                            <div style="font-weight:600;font-size:13px">❓ ${block.content || 'Câu hỏi thảo luận'}</div>
                        </div>
                    ` : ''}
                    ${block.type === 'task' ? `
                        <div style="background:var(--green-50);padding:14px;border-radius:10px;border-left:4px solid var(--green)">
                            <div style="font-weight:600;font-size:13px">📝 ${block.content || 'Nhiệm vụ'}</div>
                        </div>
                    ` : ''}
                </div>
            `;
        }).join('');
    }

    return {
        init
    };
})();
