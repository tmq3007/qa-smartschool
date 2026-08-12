/* ═══════════════════════════════════════════════════════════
   QA SmartClass — Chat Module
   2-column chat with channel sidebar, rich input bar
   Mirrors WPF StudentChatPage.xaml
   ═══════════════════════════════════════════════════════════ */

const ChatManager = (() => {
    let _channels = [];
    let _activeChannel = 'group';
    let _filter = 'all'; // all | group | private
    let _sort = 'online';
    let _messages = {}; // channelId -> [msg]
    let _searchText = '';

    const CHANNEL_COLORS = [
        '#1976D2', '#E65100', '#2E7D32', '#7B1FA2', '#C62828',
        '#00695C', '#4527A0', '#AD1457', '#1565C0', '#EF6C00'
    ];

    function init() {
        StudentConnection.on('chat_message', handleIncomingMessage);
        StudentConnection.on('chat_channels', handleChannelList);
        loadData();
        initDefaultChannels();
        render();
    }

    function initDefaultChannels() {
        if (_channels.length > 0) return;
        _channels = [
            { id: 'group', name: 'Cả lớp', subtitle: 'Nhắn tin chung với GV và cả lớp',
              type: 'group', avatarText: '👥', online: true, unread: 0, lastMsg: '', lastTime: '' },
            { id: 'teacher', name: 'Giáo viên', subtitle: 'Nhắn riêng với GV',
              type: 'private', avatarText: '👨‍🏫', online: true, unread: 0, lastMsg: '', lastTime: '' }
        ];
    }

    // ═══ EVENT HANDLERS ═══
    function handleIncomingMessage(data) {
        const channelId = data.channel || 'group';
        const msg = {
            sender: data.sender || data.name || 'N/A',
            text: data.text || data.data || '',
            time: new Date().toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' }),
            timestamp: Date.now(),   // P2-5: for date separators
            isTeacher: data.isTeacher || false,
            self: false
        };

        if (!_messages[channelId]) _messages[channelId] = [];
        _messages[channelId].push(msg);

        // Update channel
        const ch = _channels.find(c => c.id === channelId);
        if (ch) {
            ch.lastMsg = msg.text.length > 30 ? msg.text.substring(0, 27) + '...' : msg.text;
            ch.lastTime = msg.time;
            if (channelId !== _activeChannel) {
                ch.unread = (ch.unread || 0) + 1;
            }
        }

        saveData();

        // If viewing this channel, re-render messages
        if (channelId === _activeChannel) {
            renderMessages();
        }
        renderChannelList();
    }

    function handleChannelList(data) {
        if (data.channels && Array.isArray(data.channels)) {
            data.channels.forEach(ch => {
                if (!_channels.find(c => c.id === ch.id)) {
                    _channels.push({
                        ...ch,
                        avatarText: ch.name ? ch.name.substring(0, 2).toUpperCase() : '?',
                        unread: 0, lastMsg: '', lastTime: ''
                    });
                }
            });
            render();
        }
    }

    // ═══ SEND MESSAGE ═══
    function sendMessage() {
        const input = document.getElementById('chatPageInput');
        if (!input) return;
        const text = input.value.trim();
        if (!text) return;

        const info = AppStorage.getStudentInfo();
        const msg = {
            sender: info.name || 'Học sinh',
            text: text,
            time: new Date().toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' }),
            self: true
        };

        if (!_messages[_activeChannel]) _messages[_activeChannel] = [];
        _messages[_activeChannel].push(msg);

        // Update channel
        const ch = _channels.find(c => c.id === _activeChannel);
        if (ch) {
            ch.lastMsg = text.length > 30 ? text.substring(0, 27) + '...' : text;
            ch.lastTime = msg.time;
        }

        StudentConnection.sendChat(text, _activeChannel);
        saveData();
        renderMessages();
        renderChannelList();

        input.value = '';
        input.focus();
    }

    function quickLike() {
        const input = document.getElementById('chatPageInput');
        if (input) { input.value = '👍'; }
        sendMessage();
    }

    // ═══ P1-3: ATTACH FILE ═══
    function attachFile() {
        let fileInput = document.getElementById('_chatFileInput');
        if (!fileInput) {
            fileInput = document.createElement('input');
            fileInput.type = 'file';
            fileInput.id = '_chatFileInput';
            fileInput.style.display = 'none';
            fileInput.accept = 'image/*,.pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx,.txt,.zip';
            document.body.appendChild(fileInput);
            fileInput.addEventListener('change', () => {
                if (fileInput.files && fileInput.files[0]) {
                    const file = fileInput.files[0];
                    const ext = file.name.split('.').pop().toLowerCase();
                    const icon = ['png','jpg','jpeg','gif','bmp','webp'].includes(ext) ? '🖼️'
                               : ['pdf'].includes(ext) ? '📄'
                               : ['doc','docx'].includes(ext) ? '📝'
                               : ['mp4','avi','mov'].includes(ext) ? '🎦'
                               : ['mp3','wav'].includes(ext) ? '🎥'
                               : '📎';
                    const input = document.getElementById('chatPageInput');
                    if (input) {
                        input.value = `${icon} [File: ${file.name}]`;
                        input.focus();
                    }
                    fileInput.value = '';
                }
            });
        }
        fileInput.click();
    }

    // ═══ P1-4: EMOJI PICKER ═══
    const _QUICK_EMOJIS = [
        '😀','😂','😍','😎','🥳','🤔','😢','😡','😱','🥰',
        '👍','👎','👏','✌️','🤝','💪','🥳','🙏','❤️','💔',
        '🔥','✨','🎉','🎁','🏆','🥇','💯','✅','❌','❗',
        '📚','✏️','🔍','💡','🔔','📢','📌','⏰','🚀','🌟'
    ];

    function toggleEmojiPicker() {
        let picker = document.getElementById('_chatEmojiPicker');
        if (picker) { picker.remove(); return; }

        picker = document.createElement('div');
        picker.id = '_chatEmojiPicker';
        picker.style.cssText = `
            position:fixed;bottom:80px;right:24px;z-index:9999;
            background:white;border:1.5px solid #E0E0E0;border-radius:16px;
            padding:12px;width:280px;box-shadow:0 8px 32px rgba(0,0,0,0.15);
            display:flex;flex-wrap:wrap;gap:4px;
        `;

        _QUICK_EMOJIS.forEach(em => {
            const btn = document.createElement('button');
            btn.textContent = em;
            btn.title = em;
            btn.style.cssText = 'width:36px;height:36px;border:none;background:transparent;cursor:pointer;font-size:20px;border-radius:8px;transition:background 0.1s';
            btn.onmouseover = () => btn.style.background = '#F5F5F5';
            btn.onmouseout = () => btn.style.background = 'transparent';
            btn.onclick = () => {
                const input = document.getElementById('chatPageInput');
                if (input) {
                    const pos = input.selectionStart || input.value.length;
                    input.value = input.value.substring(0, pos) + em + input.value.substring(pos);
                    input.focus();
                }
                picker.remove();
            };
            picker.appendChild(btn);
        });

        document.body.appendChild(picker);
        // Close on outside click
        setTimeout(() => document.addEventListener('click', function closePicker(e) {
            if (!picker.contains(e.target) && e.target.id !== '_chatEmojiBtn') {
                picker.remove();
                document.removeEventListener('click', closePicker);
            }
        }), 50);
    }

    // ═══ CHANNEL SELECTION ═══
    function selectChannel(channelId) {
        _activeChannel = channelId;
        const ch = _channels.find(c => c.id === channelId);
        if (ch) ch.unread = 0;
        saveData();
        render();
    }

    function setFilter(filter) {
        _filter = filter;
        renderChannelList();
    }

    function setSort(sort) {
        _sort = sort;
        renderChannelList();
    }

    function setSearch(text) {
        _searchText = text;
        renderChannelList();
    }

    function clearChat() {
        if (!confirm('Xóa toàn bộ tin nhắn trong kênh này?')) return;
        _messages[_activeChannel] = [];
        saveData();
        renderMessages();
    }

    // ═══ MAIN RENDER ═══
    function render() {
        const container = document.getElementById('chatPageContent');
        if (!container) return;

        const activeCh = _channels.find(c => c.id === _activeChannel) || _channels[0];
        const msgCount = (_messages[_activeChannel] || []).length;

        container.innerHTML = `
            <div class="chat-page-layout">
                <!-- LEFT SIDEBAR -->
                <div class="chat-sidebar" id="chatSidebar">
                    <!-- Header -->
                    <div class="chat-sidebar-header">
                        <div style="font-size:17px;font-weight:700;color:#1565C0">💬 Tin nhắn</div>
                        <div style="font-size:10.5px;color:#9E9E9E;margin-top:2px">🟢 Đang trực tuyến</div>
                    </div>

                    <!-- Search -->
                    <div class="chat-sidebar-search">
                        <input type="text" placeholder="🔍 Tìm kiếm..."
                               oninput="ChatManager.setSearch(this.value)" value="${escapeHtml(_searchText)}">
                    </div>

                    <!-- Filter Tabs -->
                    <div class="chat-filter-tabs">
                        <button class="chat-filter-btn ${_filter === 'all' ? 'active' : ''}"
                                onclick="ChatManager.setFilter('all')">📋 Tất cả</button>
                        <button class="chat-filter-btn ${_filter === 'group' ? 'active' : ''}"
                                onclick="ChatManager.setFilter('group')">🏫 Cả lớp</button>
                        <button class="chat-filter-btn ${_filter === 'private' ? 'active' : ''}"
                                onclick="ChatManager.setFilter('private')">🔒 Riêng</button>
                    </div>

                    <!-- Sort Options -->
                    <div class="chat-sort-bar">
                        <span style="font-size:10px;color:#9E9E9E">Sắp xếp:</span>
                        <button class="chat-sort-btn ${_sort === 'online' ? 'active' : ''}"
                                onclick="ChatManager.setSort('online')">🟢 Online</button>
                        <button class="chat-sort-btn ${_sort === 'name' ? 'active' : ''}"
                                onclick="ChatManager.setSort('name')">A→Z</button>
                        <button class="chat-sort-btn ${_sort === 'recent' ? 'active' : ''}"
                                onclick="ChatManager.setSort('recent')">🕐 Mới</button>
                        <button class="chat-sort-btn ${_sort === 'unread' ? 'active' : ''}"
                                onclick="ChatManager.setSort('unread')">🔴 Chưa đọc</button>
                    </div>

                    <!-- Channel List -->
                    <div class="chat-channel-list" id="chatChannelList">
                        ${renderChannelListHtml()}
                    </div>
                </div>

                <!-- RIGHT: CHAT AREA -->
                <div class="chat-main-area">
                    <!-- Chat Header -->
                    <div class="chat-area-header">
                        <div class="chat-area-header-left">
                            <div class="chat-header-avatar" style="background:linear-gradient(135deg,#2E7D32,#66BB6A)">
                                ${activeCh.avatarText || '👥'}
                            </div>
                            <div>
                                <div style="font-size:16px;font-weight:700;color:#1A1A1A">${escapeHtml(activeCh.name)}</div>
                                <div style="font-size:11px;color:#9E9E9E">${escapeHtml(activeCh.subtitle || '')}</div>
                            </div>
                        </div>
                        <div class="chat-area-header-actions">
                            <button class="chat-action-btn" style="background:#FFF3E0;color:#E65100" title="Thông báo">📢 Thông báo</button>
                            <button class="chat-action-btn" style="background:#E8EAF6;color:#283593" title="Lịch sử">📜 Lịch sử</button>
                            <button class="chat-action-btn" style="background:#FFEBEE;color:#C62828" title="Xóa"
                                    onclick="ChatManager.clearChat()">🗑️</button>
                        </div>
                    </div>

                    <!-- Sub-header -->
                    <div class="chat-area-subheader">
                        <div style="display:flex;gap:6px">
                            <span class="chat-sub-tag active">💬 Trò chuyện</span>
                            <span class="chat-sub-tag">👥 ${_channels.length} thành viên</span>
                        </div>
                        <span style="font-size:10px;color:#9E9E9E">${msgCount} tin nhắn</span>
                    </div>

                    <!-- Messages -->
                    <div class="chat-messages-area" id="chatMessagesArea">
                        ${renderMessagesHtml()}
                    </div>

                    <!-- Rich Input Bar -->
                    <div class="chat-rich-input">
                        <button class="chat-input-icon" title="Đính kèm tệp" onclick="ChatManager.attachFile()">➕</button>
                        <div class="chat-input-wrap">
                            <input type="text" id="chatPageInput" placeholder="Gõ và nhấn Enter để gửi tin nhắn"
                                   onkeydown="if(event.key==='Enter')ChatManager.sendMessage()">
                        </div>
                        <button class="chat-input-icon" id="_chatEmojiBtn" title="Biểu cảm" onclick="ChatManager.toggleEmojiPicker()">😊</button>
                        <button class="chat-input-icon" onclick="ChatManager.quickLike()" title="Thích">👍</button>
                        <button class="chat-input-icon" title="Đính kèm ảnh" onclick="ChatManager.attachFile()">📎</button>
                        <button class="chat-input-icon" title="Sticker">😃</button>
                        <button class="chat-send-btn-v2" onclick="ChatManager.sendMessage()" title="Gửi">▶</button>
                    </div>
                </div>
            </div>
        `;

        // Auto-scroll
        const area = document.getElementById('chatMessagesArea');
        if (area) area.scrollTop = area.scrollHeight;
    }

    // ═══ SUB-RENDERS ═══
    function renderChannelList() {
        const el = document.getElementById('chatChannelList');
        if (el) el.innerHTML = renderChannelListHtml();
    }

    function renderMessages() {
        const el = document.getElementById('chatMessagesArea');
        if (el) {
            el.innerHTML = renderMessagesHtml();
            el.scrollTop = el.scrollHeight;
        }
    }

    function renderChannelListHtml() {
        let filtered = _channels;

        // Filter
        if (_filter === 'group') filtered = filtered.filter(c => c.type === 'group');
        if (_filter === 'private') filtered = filtered.filter(c => c.type === 'private');

        // Search
        if (_searchText) {
            const q = _searchText.toLowerCase();
            filtered = filtered.filter(c => c.name.toLowerCase().includes(q));
        }

        // Sort
        if (_sort === 'name') filtered.sort((a, b) => a.name.localeCompare(b.name, 'vi'));
        if (_sort === 'unread') filtered.sort((a, b) => (b.unread || 0) - (a.unread || 0));
        if (_sort === 'recent') filtered.sort((a, b) => (b.lastTime || '').localeCompare(a.lastTime || ''));

        if (filtered.length === 0) {
            return `<div style="text-align:center;padding:20px;color:#BDBDBD;font-size:12px">Không tìm thấy</div>`;
        }

        return filtered.map(ch => {
            const isActive = ch.id === _activeChannel;
            const color = CHANNEL_COLORS[Math.abs(hashCode(ch.id)) % CHANNEL_COLORS.length];

            return `
                <div class="chat-channel-item ${isActive ? 'active' : ''}"
                     onclick="ChatManager.selectChannel('${ch.id}')">
                    <div class="chat-channel-avatar" style="background:${color}">
                        ${ch.avatarText || '?'}
                    </div>
                    <div class="chat-channel-info">
                        <div class="chat-channel-name">${escapeHtml(ch.name)}</div>
                        ${ch.lastMsg ? `<div class="chat-channel-last">${escapeHtml(ch.lastMsg)}</div>` : ''}
                        <div class="chat-channel-sub">${escapeHtml(ch.subtitle || '')}</div>
                    </div>
                    <div class="chat-channel-meta">
                        ${ch.lastTime ? `<div class="chat-channel-time">${ch.lastTime}</div>` : ''}
                        ${ch.unread > 0 ? `<div class="chat-channel-unread">${ch.unread}</div>` : ''}
                    </div>
                </div>
            `;
        }).join('');
    }

    function renderMessagesHtml() {
        const msgs = _messages[_activeChannel] || [];
        if (msgs.length === 0) {
            return `
                <div class="empty-state" style="padding:40px 0">
                    <div class="empty-icon">💬</div>
                    <div class="empty-text">Bắt đầu trò chuyện</div>
                </div>
            `;
        }

        // P2-5: Date separators between days
        let lastDateStr = null;
        return msgs.slice(-100).map(msg => {
            const isSelf = msg.self;
            const isTeacher = msg.isTeacher;
            const initials = getInitials(msg.sender || '?');

            // Date separator
            let separatorHtml = '';
            const msgDate = msg.time ? msg.time.split(' ')[0] : null; // 'HH:MM' - no date in time
            const msgDateObj = msg.timestamp ? new Date(msg.timestamp) : null;
            if (msgDateObj) {
                const today = new Date(); const yesterday = new Date(); yesterday.setDate(today.getDate() - 1);
                const isoDate = msgDateObj.toDateString();
                if (isoDate !== lastDateStr) {
                    lastDateStr = isoDate;
                    let label;
                    if (isoDate === today.toDateString()) label = 'Hôm nay';
                    else if (isoDate === yesterday.toDateString()) label = 'Hôm qua';
                    else label = msgDateObj.toLocaleDateString('vi-VN', { weekday:'short', day:'2-digit', month:'2-digit' });
                    separatorHtml = `<div style="text-align:center;margin:10px 0 6px;font-size:10.5px;color:#9E9E9E;
                        display:flex;align-items:center;gap:8px">
                        <div style="flex:1;height:1px;background:#E0E0E0"></div>
                        <span>${label}</span>
                        <div style="flex:1;height:1px;background:#E0E0E0"></div>
                    </div>`;
                }
            }

            return separatorHtml + `
                <div class="chat-msg-v2 ${isSelf ? 'self' : ''} ${isTeacher ? 'teacher' : ''}">
                    <div class="chat-msg-avatar-v2">${initials}</div>
                    <div class="chat-msg-content-v2">
                        <div class="chat-msg-name-v2">${isSelf ? 'Bạn' : escapeHtml(msg.sender)}</div>
                        <div class="chat-msg-bubble-v2">${escapeHtml(msg.text)}</div>
                        <div class="chat-msg-time-v2">${msg.time || ''}</div>
                    </div>
                </div>
            `;
        }).join('');
    }

    // ═══ HELPERS ═══
    function hashCode(str) {
        let hash = 0;
        for (let i = 0; i < str.length; i++) {
            hash = ((hash << 5) - hash) + str.charCodeAt(i);
            hash |= 0;
        }
        return hash;
    }

    function getInitials(name) {
        if (!name) return '?';
        const parts = name.split(' ').filter(Boolean);
        if (parts.length >= 2) return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
        return name.substring(0, 2).toUpperCase();
    }

    // ═══ PERSISTENCE ═══
    function saveData() {
        try {
            localStorage.setItem('qasc_chat_v2', JSON.stringify({
                channels: _channels,
                messages: _messages,
                activeChannel: _activeChannel
            }));
        } catch {}
    }

    function loadData() {
        try {
            const data = JSON.parse(localStorage.getItem('qasc_chat_v2'));
            if (data) {
                _channels = data.channels || [];
                _messages = data.messages || {};
                _activeChannel = data.activeChannel || 'group';
            }
        } catch {}
    }

    return {
        init,
        sendMessage,
        quickLike,
        attachFile,
        toggleEmojiPicker,
        selectChannel,
        setFilter,
        setSort,
        setSearch,
        clearChat,
        render
    };
})();
