/* ═══════════════════════════════════════════════════════════
   QA SmartClass — Submit / Homework Module
   Handles homework display, file upload, text editor, and camera capture
   Mirrors WPF StudentSubmitPage.xaml with 3 tabs:
     📥 Tài liệu nhận  |  📤 Bài đã nộp  |  📝 Soạn bài
   ═══════════════════════════════════════════════════════════ */

const SubmitManager = (() => {
    let _receivedFiles = [];    // Files received from teacher
    let _submittedFiles = [];   // Files submitted to teacher
    let _pendingFiles = [];     // Files pending upload { name, dataUrl, size, type }
    let _activeTab = 'received';
    let _editorText = '';
    let _editorFileName = '';
    let _dragCounter = 0;
    let _assignment = null;     // Current assignment from teacher { title, description, deadline }
    let _deadlineInterval = null; // Countdown timer ID

    // ─── Initialize ───
    function init() {
        StudentConnection.on('homework', handleHomework);
        StudentConnection.on('homework_list', handleHomeworkList);
        StudentConnection.on('file_distribute', handleFileDistribute);
        StudentConnection.on('file_received', handleFileReceived);   // P0-3
        StudentConnection.on('assignment_info', handleAssignmentInfo); // P0-2
        StudentConnection.on('submit_confirmed', handleSubmitConfirmed);
        loadData();
        setupDragDrop();
    }

    // ═══ EVENT HANDLERS ═══
    function handleHomework(data) {
        const hw = {
            id: data.target || `hw_${Date.now()}`,
            title: data.title || data.data || 'Bài tập từ giáo viên',
            description: data.description || '',
            dueDate: data.dueDate || '',
            type: data.hwType || 'general',
            status: 'pending',
            receivedAt: new Date().toISOString()
        };

        _receivedFiles.unshift(hw);
        saveData();
        updateStats();
        if (_activeTab === 'received') renderTabContent();

        showToast(`📚 Bài tập mới: ${hw.title}`, 'info', 5000);
        if (typeof addActivity === 'function') {
            addActivity('📚', `BTVN: ${hw.title}`, '#F3E8FF');
        }
        if (typeof addDashNotification === 'function') {
            addDashNotification('file', 'Bài tập mới', hw.title);
        }
    }

    function handleHomeworkList(data) {
        if (data.items && Array.isArray(data.items)) {
            _receivedFiles = data.items;
            saveData();
            updateStats();
            if (_activeTab === 'received') renderTabContent();
        }
    }

    function handleFileDistribute(data) {
        const file = {
            id: data.fileId || `file_${Date.now()}`,
            fileName: data.fileName || 'Tài liệu',
            fileType: data.fileType || 'FILE',
            fileUrl: data.fileUrl || '',
            fileSize: data.fileSize || 0,
            receivedAt: new Date().toISOString()
        };
        _receivedFiles.unshift(file);
        saveData();
        updateStats();
        if (_activeTab === 'received') renderTabContent();

        showToast(`📁 Nhận tài liệu: ${file.fileName}`, 'info', 4000);
        if (typeof addActivity === 'function') {
            addActivity('📥', `Nhận file: ${file.fileName}`, '#DBEAFE');
        }
    }

    function handleSubmitConfirmed(data) {
        showToast('✅ GV xác nhận nhận bài!', 'success');
        if (typeof addActivity === 'function') {
            addActivity('✅', 'GV xác nhận bài nộp', '#D1FAE5');
        }
        // Mark latest submitted as confirmed
        if (_submittedFiles.length > 0) {
            _submittedFiles[0].confirmed = true;
            saveData();
            if (_activeTab === 'submitted') renderTabContent();
        }
    }

    // ═══ P0-2: ASSIGNMENT INFO from teacher ═══
    function handleAssignmentInfo(data) {
        _assignment = {
            title: data.title || data.data || 'Bài tập từ giáo viên',
            description: data.description || '',
            deadline: data.deadline || data.dueDate || null,
            receivedAt: new Date().toISOString()
        };
        saveData();
        _startDeadlineCountdown();
        if (_activeTab === 'received') renderTabContent();
        showToast(`📚 Bài tập: ${_assignment.title}`, 'info', 5000);
        if (typeof addActivity === 'function') {
            addActivity('📚', `GV giao bài: ${_assignment.title}`, '#F3E8FF');
        }
        if (typeof addDashNotification === 'function') {
            addDashNotification('file', 'Bài tập mới', _assignment.title);
        }
    }

    function _startDeadlineCountdown() {
        if (_deadlineInterval) clearInterval(_deadlineInterval);
        if (!_assignment || !_assignment.deadline) return;

        function _updateCountdown() {
            const el = document.getElementById('assignmentCountdown');
            if (!el) return;
            const deadline = new Date(_assignment.deadline);
            const diff = deadline - Date.now();
            if (diff <= 0) {
                el.textContent = '⚠️ HẾt hạn nộp bài!';
                el.style.color = '#C62828';
                if (_deadlineInterval) { clearInterval(_deadlineInterval); _deadlineInterval = null; }
                return;
            }
            const h = Math.floor(diff / 3600000);
            const m = Math.floor((diff % 3600000) / 60000);
            const s = Math.floor((diff % 60000) / 1000);
            if (h < 1) {
                el.textContent = `⏱️ Còn ${m} phút ${s} giây`;
                el.style.color = '#C62828';
            } else if (h < 24) {
                el.textContent = `⏱️ Còn ${h} giờ ${m} phút`;
                el.style.color = '#E65100';
            } else {
                const d = Math.floor(h / 24);
                el.textContent = `⏱️ Còn ${d} ngày ${h % 24} giờ`;
                el.style.color = '#2E7D32';
            }
        }
        _updateCountdown();
        _deadlineInterval = setInterval(_updateCountdown, 1000);
    }

    // ═══ P0-3: FILE RECEIVED from teacher ═══
    function handleFileReceived(data) {
        const file = {
            id: data.fileId || `file_${Date.now()}`,
            fileName: data.fileName || data.name || 'Tài liệu',
            fileType: data.fileType || 'FILE',
            fileUrl: data.fileUrl || data.url || '',
            fileSize: data.fileSize || data.size || 0,
            receivedAt: new Date().toISOString(),
            fromTeacher: true
        };
        _receivedFiles.unshift(file);
        saveData();
        updateStats();
        if (_activeTab === 'received') renderTabContent();

        showToast(`📥 Nhận tài liệu: ${file.fileName}`, 'info', 5000);
        if (typeof addActivity === 'function') {
            addActivity('📥', `Nhận file: ${file.fileName}`, '#DBEAFE');
        }
        if (typeof addDashNotification === 'function') {
            addDashNotification('file', 'Nhận tài liệu', file.fileName);
        }
    }

    // ═══ TAB SWITCHING ═══
    function switchTab(tabId) {
        _activeTab = tabId;

        // Update tab buttons
        document.querySelectorAll('.submit-tab').forEach(btn => {
            btn.classList.toggle('active', btn.dataset.tab === tabId);
        });

        renderTabContent();
    }

    // ═══ RENDER TAB CONTENT ═══
    function renderTabContent() {
        const container = document.getElementById('submitContainer');
        if (!container) return;

        switch (_activeTab) {
            case 'received':
                renderReceivedTab(container);
                break;
            case 'submitted':
                renderSubmittedTab(container);
                break;
            case 'editor':
                renderEditorTab(container);
                break;
        }
    }

    // ─── Tab 1: Received Files ───
    function renderReceivedTab(container) {
        // Assignment panel (P0-2)
        let assignmentHtml = '';
        if (_assignment) {
            const deadlineStr = _assignment.deadline
                ? `⏰ Hạn: ${new Date(_assignment.deadline).toLocaleString('vi-VN', { day:'2-digit', month:'2-digit', hour:'2-digit', minute:'2-digit' })}`
                : 'Không có hạn nộp';
            assignmentHtml = `
                <div style="background:linear-gradient(135deg,#E8F5E9,#F1F8E9);border:1.5px solid #A5D6A7;
                            border-radius:12px;padding:14px 18px;margin-bottom:16px">
                    <div style="font-size:13px;font-weight:700;color:#2E7D32;margin-bottom:6px">
                        📚 Bài tập từ GV
                    </div>
                    <div style="font-size:14px;font-weight:600;color:#1B5E20;margin-bottom:8px">
                        ${escapeHtml(_assignment.title)}
                    </div>
                    ${_assignment.description ? `<div style="font-size:12px;color:#388E3C;margin-bottom:8px">${escapeHtml(_assignment.description)}</div>` : ''}
                    <div style="display:flex;justify-content:space-between;align-items:center;flex-wrap:wrap;gap:6px">
                        <div style="font-size:11px;color:#558B2F">${deadlineStr}</div>
                        <div id="assignmentCountdown" style="font-size:11px;font-weight:700"></div>
                    </div>
                </div>
            `;
        }

        if (_receivedFiles.length === 0 && !_assignment) {
            container.innerHTML = `
                <div class="empty-state">
                    <div class="empty-icon">📥</div>
                    <div class="empty-text">Chưa nhận tài liệu nào</div>
                    <p class="text-muted text-sm mt-2">Tài liệu từ GV sẽ hiển thị khi GV phát bài</p>
                </div>
            `;
            return;
        }

        container.innerHTML = `
            ${assignmentHtml}
            <div class="submit-file-list">
                ${_receivedFiles.map((f, i) => renderReceivedItem(f, i)).join('')}
            </div>
        `;

        // Restart countdown after re-render
        _startDeadlineCountdown();
    }

    function renderReceivedItem(f, index) {
        const icon = _getFileIcon(f.fileType || f.type);
        const time = f.receivedAt
            ? new Date(f.receivedAt).toLocaleString('vi-VN', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' })
            : '--';
        const name = f.fileName || f.title || 'Tài liệu';
        const size = f.fileSize ? _formatFileSize(f.fileSize) : '';

        return `
            <div class="submit-file-item animate-fade-in" style="animation-delay:${index * 40}ms">
                <div class="submit-file-icon" style="background:#E3F2FD;color:#1565C0">${icon}</div>
                <div class="submit-file-info">
                    <div class="submit-file-name">${escapeHtml(name)}</div>
                    <div class="submit-file-meta">
                        📅 ${time}${size ? ` • ${size}` : ''}
                        ${f.description ? ` • ${escapeHtml(f.description).substring(0, 40)}` : ''}
                    </div>
                </div>
                <div class="submit-file-actions">
                    ${f.fileUrl ? `<button class="submit-file-btn blue" onclick="window.open('${f.fileUrl}','_blank')" title="Mở file">📂 Mở</button>` : ''}
                    ${f.dueDate ? `<span class="submit-file-due">📅 ${f.dueDate}</span>` : ''}
                </div>
            </div>
        `;
    }

    // ─── Tab 2: Submitted Files ───
    function renderSubmittedTab(container) {
        if (_submittedFiles.length === 0) {
            container.innerHTML = `
                <div class="empty-state">
                    <div class="empty-icon">📤</div>
                    <div class="empty-text">Chưa nộp bài nào</div>
                    <p class="text-muted text-sm mt-2">Bài nộp sẽ hiển thị khi em nộp bài cho GV</p>
                </div>
            `;
            return;
        }

        container.innerHTML = `
            <div class="submit-file-list">
                ${_submittedFiles.map((f, i) => renderSubmittedItem(f, i)).join('')}
            </div>
        `;
    }

    function renderSubmittedItem(f, index) {
        const icon = f.type === 'text' ? '📝' : '📄';
        const time = f.submittedAt
            ? new Date(f.submittedAt).toLocaleString('vi-VN', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' })
            : '--';
        const statusColor = f.confirmed ? '#2E7D32' : '#E65100';
        const statusText = f.confirmed ? '✅ Đã xác nhận' : '⏳ Đang gửi';

        return `
            <div class="submit-file-item animate-fade-in" style="animation-delay:${index * 40}ms">
                <div class="submit-file-icon" style="background:#E8F5E9;color:#2E7D32">${icon}</div>
                <div class="submit-file-info">
                    <div class="submit-file-name">${escapeHtml(f.fileName || f.name || 'Bài nộp')}</div>
                    <div class="submit-file-meta">
                        📅 ${time}${f.fileCount ? ` • ${f.fileCount} file` : ''}
                        ${f.textLength ? ` • ${f.textLength} ký tự` : ''}
                    </div>
                </div>
                <div class="submit-file-actions">
                    <span class="submit-file-status" style="color:${statusColor};font-size:10px;font-weight:600">${statusText}</span>
                </div>
            </div>
        `;
    }

    // ─── Tab 3: Text Editor ───
    function renderEditorTab(container) {
        container.innerHTML = `
            <div class="submit-editor-panel">
                <div style="margin-bottom:12px">
                    <label style="font-size:12px;font-weight:600;color:#424242;display:block;margin-bottom:6px">📌 Tên bài viết:</label>
                    <input type="text" id="editorFileName" value="${escapeHtml(_editorFileName)}"
                           placeholder="Nhập tên file (VD: bai_tap_toan.txt)"
                           style="width:100%;padding:10px 14px;border:2px solid var(--gray-200);border-radius:10px;
                                  font-size:13px;outline:none;transition:border-color 0.15s"
                           onfocus="this.style.borderColor='var(--primary)'"
                           onblur="this.style.borderColor='var(--gray-200)'"
                           oninput="SubmitManager._updateEditorFileName(this.value)">
                </div>

                <label style="font-size:12px;font-weight:600;color:#424242;display:block;margin-bottom:6px">✏️ Nội dung bài viết:</label>
                <textarea class="submit-editor-textarea" id="editorTextarea"
                          placeholder="Nhập bài làm của em ở đây...&#10;&#10;Hỗ trợ nhiều dòng, có thể viết tự do."
                          oninput="SubmitManager._updateEditorText(this.value)">${escapeHtml(_editorText)}</textarea>
                <div class="submit-word-count" id="editorWordCount">
                    ${_editorText.length} ký tự • ${_editorText.split(/\s+/).filter(Boolean).length} từ
                </div>

                <div style="display:flex;gap:10px;margin-top:16px">
                    <button class="submit-action-btn green" style="flex:1;padding:12px;font-size:13px"
                            onclick="SubmitManager.submitEditorText()">
                        📤 Nộp bài viết
                    </button>
                    <button class="submit-action-btn blue" style="padding:12px"
                            onclick="SubmitManager.clearEditor()">
                        🗑️ Xóa
                    </button>
                </div>
            </div>
        `;
    }

    // ═══ EDITOR HELPERS ═══
    function _updateEditorText(text) {
        _editorText = text;
        const counter = document.getElementById('editorWordCount');
        if (counter) {
            counter.textContent = `${text.length} ký tự • ${text.split(/\s+/).filter(Boolean).length} từ`;
        }
    }

    function _updateEditorFileName(name) {
        _editorFileName = name;
    }

    function clearEditor() {
        _editorText = '';
        _editorFileName = '';
        renderTabContent();
        showToast('🗑️ Đã xóa nội dung soạn', 'info', 2000);
    }

    function submitEditorText() {
        if (!_editorText.trim()) {
            showToast('Vui lòng nhập nội dung bài viết', 'warning');
            return;
        }

        const fileName = _editorFileName.trim() || `bai_lam_${Date.now()}.txt`;

        // Send to teacher
        StudentConnection.send({
            type: 'homework_submit',
            submitType: 'text',
            fileName: fileName,
            text: _editorText,
            submittedAt: new Date().toISOString()
        });

        // Record locally
        _submittedFiles.unshift({
            name: fileName,
            fileName: fileName,
            type: 'text',
            textLength: _editorText.length,
            submittedAt: new Date().toISOString(),
            confirmed: false
        });
        saveData();
        updateStats();

        // Update dashboard stat
        _incrementDashStat();

        // Clear editor
        _editorText = '';
        _editorFileName = '';

        // Show success feedback
        const container = document.getElementById('submitContainer');
        if (container) {
            container.innerHTML = `
                <div style="text-align:center;padding:40px 20px">
                    <div style="font-size:56px;margin-bottom:16px" class="animate-bounce-in">📮</div>
                    <h3 style="margin-bottom:8px;font-size:16px">Đã nộp bài viết!</h3>
                    <p style="color:var(--text-muted);font-size:13px;margin-bottom:20px">
                        📄 ${escapeHtml(fileName)}
                    </p>
                    <button class="submit-action-btn blue" onclick="SubmitManager.switchTab('submitted')"
                            style="padding:10px 20px;font-size:12px">
                        📋 Xem bài đã nộp
                    </button>
                </div>
            `;
        }

        showToast('✅ Đã nộp bài viết thành công!', 'success');
        if (typeof addActivity === 'function') {
            addActivity('📮', `Nộp bài viết: ${fileName}`, '#D1FAE5');
        }
    }

    // ═══ FILE UPLOAD ═══
    function pickFile() {
        // Create temporary file input
        const input = document.createElement('input');
        input.type = 'file';
        input.accept = 'image/*,.pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx,.txt,.zip';
        input.multiple = true;
        input.onchange = (e) => onFileSelected(e);
        input.click();
    }

    function takePhoto() {
        const input = document.createElement('input');
        input.type = 'file';
        input.accept = 'image/*';
        input.capture = 'environment';
        input.onchange = (e) => onFileSelected(e);
        input.click();
    }

    function onFileSelected(event) {
        const files = event.target.files;
        if (!files || files.length === 0) return;

        let uploadCount = 0;

        Array.from(files).forEach(file => {
            if (file.size > 10 * 1024 * 1024) {
                showToast(`File "${file.name}" quá lớn (> 10MB)`, 'warning');
                return;
            }

            const reader = new FileReader();
            reader.onload = (e) => {
                uploadCount++;

                // Send immediately
                StudentConnection.send({
                    type: 'homework_submit',
                    submitType: 'file',
                    fileName: file.name,
                    fileType: file.type,
                    fileSize: file.size,
                    data: e.target.result,
                    submittedAt: new Date().toISOString()
                });

                // Record locally
                _submittedFiles.unshift({
                    name: file.name,
                    fileName: file.name,
                    type: 'file',
                    fileSize: file.size,
                    submittedAt: new Date().toISOString(),
                    confirmed: false
                });

                saveData();
                updateStats();

                // Update progress bar
                _updateProgressBar(uploadCount, files.length);

                if (uploadCount === files.length) {
                    showToast(`✅ Đã nộp ${uploadCount} file!`, 'success');
                    _incrementDashStat();
                    if (typeof addActivity === 'function') {
                        addActivity('📤', `Nộp ${uploadCount} file`, '#D1FAE5');
                    }
                    // Reset progress after delay
                    setTimeout(() => _resetProgressBar(), 2000);
                }
            };
            reader.readAsDataURL(file);
        });
    }

    // ═══ OPEN EDITOR (quick switch) ═══
    function openEditor() {
        switchTab('editor');
    }

    // ═══ DRAG & DROP ═══
    function setupDragDrop() {
        document.addEventListener('dragenter', (e) => {
            if (currentPage !== 'submit') return;
            e.preventDefault();
            _dragCounter++;
            _showDropZone();
        });

        document.addEventListener('dragleave', (e) => {
            if (currentPage !== 'submit') return;
            e.preventDefault();
            _dragCounter--;
            if (_dragCounter <= 0) {
                _dragCounter = 0;
                _hideDropZone();
            }
        });

        document.addEventListener('dragover', (e) => {
            if (currentPage !== 'submit') return;
            e.preventDefault();
        });

        document.addEventListener('drop', (e) => {
            if (currentPage !== 'submit') return;
            e.preventDefault();
            _dragCounter = 0;
            _hideDropZone();

            if (e.dataTransfer.files.length > 0) {
                onFileSelected({ target: { files: e.dataTransfer.files } });
            }
        });
    }

    function _showDropZone() {
        const progressText = document.getElementById('submitProgressText');
        const progressTrack = document.getElementById('submitProgressTrack');
        if (progressText) {
            progressText.textContent = '📁 Thả file vào đây để nộp bài!';
            progressText.style.color = '#1565C0';
            progressText.style.fontWeight = '700';
        }
        if (progressTrack) {
            progressTrack.style.width = '100%';
            progressTrack.style.background = 'linear-gradient(90deg, #E3F2FD, #BBDEFB)';
        }
    }

    function _hideDropZone() {
        _resetProgressBar();
    }

    function _updateProgressBar(current, total) {
        const pct = Math.round((current / total) * 100);
        const track = document.getElementById('submitProgressTrack');
        const text = document.getElementById('submitProgressText');
        if (track) {
            track.style.width = `${pct}%`;
            track.style.background = 'linear-gradient(90deg, #C8E6C9, #81C784)';
        }
        if (text) {
            text.textContent = `📤 Đang nộp... ${current}/${total} file (${pct}%)`;
            text.style.color = '#2E7D32';
            text.style.fontWeight = '600';
        }
    }

    function _resetProgressBar() {
        const track = document.getElementById('submitProgressTrack');
        const text = document.getElementById('submitProgressText');
        if (track) {
            track.style.width = '0%';
            track.style.background = 'linear-gradient(90deg, #E3F2FD, #BBDEFB)';
        }
        if (text) {
            text.textContent = '📂 Sẵn sàng · Có thể kéo thả file vào trang này để nộp';
            text.style.color = '';
            text.style.fontWeight = '';
        }
    }

    // ═══ STATS ═══
    function updateStats() {
        const received = document.getElementById('submitStatReceived');
        const submitted = document.getElementById('submitStatSubmitted');
        const pending = document.getElementById('submitStatPending');

        if (received) received.textContent = _receivedFiles.length;
        if (submitted) submitted.textContent = _submittedFiles.length;
        if (pending) {
            const pendingCount = _receivedFiles.filter(f => f.status === 'pending').length;
            pending.textContent = pendingCount;
        }
    }

    function _incrementDashStat() {
        const statEl = document.getElementById('statSubmitted');
        if (statEl) {
            const current = parseInt(statEl.textContent) || 0;
            statEl.textContent = current + 1;
        }
    }

    // ═══ HELPERS ═══
    function _getFileIcon(type) {
        if (!type) return '📄';
        const t = type.toLowerCase();
        if (t.includes('image') || t === 'image') return '🖼️';
        if (t.includes('video') || t === 'video') return '🎬';
        if (t.includes('pdf') || t === 'pdf') return '📄';
        if (t.includes('audio') || t === 'audio') return '🎵';
        if (t.includes('word') || t.includes('doc') || t === 'document') return '📝';
        if (t.includes('sheet') || t.includes('xls') || t === 'spreadsheet') return '📊';
        if (t.includes('presentation') || t.includes('ppt')) return '📊';
        return '📁';
    }

    function _formatFileSize(bytes) {
        if (!bytes || bytes === 0) return '';
        if (bytes < 1024) return bytes + ' B';
        if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
        return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
    }

    // ═══ PERSISTENCE ═══
    function saveData() {
        try {
            localStorage.setItem('qasc_submit_v2', JSON.stringify({
                received: _receivedFiles.slice(0, 50),
                submitted: _submittedFiles.slice(0, 50),
                activeTab: _activeTab,
                assignment: _assignment      // P0-2
            }));
        } catch {}
    }

    function loadData() {
        try {
            const data = JSON.parse(localStorage.getItem('qasc_submit_v2'));
            if (data) {
                _receivedFiles = data.received || [];
                _submittedFiles = data.submitted || [];
                _activeTab = data.activeTab || 'received';
                _assignment = data.assignment || null;
            }
        } catch {}
        // Restart deadline countdown if assignment loaded
        if (_assignment && _assignment.deadline) {
            _startDeadlineCountdown();
        }
    }

    return {
        init,
        switchTab,
        pickFile,
        takePhoto,
        openEditor,
        submitEditorText,
        clearEditor,
        onFileSelected,
        renderTabContent,
        updateStats,
        _updateEditorText,
        _updateEditorFileName,
        get _activeTab() { return _activeTab; }
    };
})();
