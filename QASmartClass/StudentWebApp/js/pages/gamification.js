/* ═══════════════════════════════════════════════════════════
   QA SmartClass — Gamification Module (Student Side)
   Renders XP, badges, leaderboard, and level progression
   Phase 6: XP & Badge System
   ═══════════════════════════════════════════════════════════ */

const GamificationUI = (() => {
    let _profile = null;
    let _leaderboard = [];

    function init() {
        // Load saved profile
        _profile = AppStorage.getGamification() || _createDefaultProfile();

        // Listen for server events
        StudentConnection.on('badge_earned', handleBadgeEarned);
        StudentConnection.on('leaderboard_update', handleLeaderboardUpdate);
        StudentConnection.on('xp_earned', handleXpEarned);

        // Handle cmd-type events from connection.js
        StudentConnection.on('command', (msg) => {
            if (msg.action === 'BADGE_EARNED') handleBadgeEarnedCmd(msg);
            if (msg.action === 'LEADERBOARD_UPDATE') handleLeaderboardCmd(msg);
        });
    }

    function _createDefaultProfile() {
        return { xp: 0, level: 1, badges: [], streak: 0 };
    }

    // ═══ XP EARNED ═══
    function handleXpEarned(data) {
        const amount = data.amount || 0;
        const reason = data.reason || '';

        _profile.xp = data.totalXp || (_profile.xp + amount);
        _profile.level = data.level || _calculateLevel(_profile.xp);
        AppStorage.saveGamification(_profile);

        // Show XP popup
        showXpToast(amount, reason);
    }

    // ═══ BADGE EARNED ═══
    function handleBadgeEarned(data) {
        const badge = data.badge || data;
        if (!_profile.badges.find(b => b.badgeId === badge.badgeId)) {
            _profile.badges.push(badge);
            AppStorage.saveGamification(_profile);
        }
        showBadgeNotification(badge);
    }

    function handleBadgeEarnedCmd(msg) {
        try {
            const badge = typeof msg.data === 'string' ? JSON.parse(msg.data) : msg.data;
            handleBadgeEarned({ badge });
        } catch (e) { console.warn('Badge parse error:', e); }
    }

    // ═══ LEADERBOARD ═══
    function handleLeaderboardUpdate(data) {
        _leaderboard = data.entries || [];
        renderLeaderboardIfVisible();
    }

    function handleLeaderboardCmd(msg) {
        try {
            const data = typeof msg.data === 'string' ? JSON.parse(msg.data) : msg.data;
            _leaderboard = data.entries || [];
            renderLeaderboardIfVisible();
        } catch (e) { console.warn('Leaderboard parse error:', e); }
    }

    // ═══ XP TOAST ANIMATION ═══
    function showXpToast(amount, reason) {
        const toast = document.createElement('div');
        toast.className = 'xp-toast animate-bounce-in';
        toast.innerHTML = `
            <div style="display:flex;align-items:center;gap:8px">
                <span style="font-size:20px">⭐</span>
                <div>
                    <div style="font-weight:800;color:#FFB300;font-size:16px">+${amount} XP</div>
                    <div style="font-size:11px;color:rgba(255,255,255,0.7)">${escapeHtml(reason)}</div>
                </div>
            </div>
        `;
        toast.style.cssText = `
            position: fixed; top: 80px; right: 20px; z-index: 99999;
            background: linear-gradient(135deg, #1a1a2e, #16213e);
            color: white; padding: 12px 20px; border-radius: 12px;
            box-shadow: 0 4px 20px rgba(255,179,0,0.3), 0 0 0 1px rgba(255,179,0,0.2);
            pointer-events: none; transition: all 0.5s ease;
        `;
        document.body.appendChild(toast);
        setTimeout(() => { toast.style.opacity = '0'; toast.style.transform = 'translateY(-20px)'; }, 2500);
        setTimeout(() => toast.remove(), 3000);
    }

    // ═══ BADGE NOTIFICATION ═══
    function showBadgeNotification(badge) {
        const overlay = document.createElement('div');
        overlay.style.cssText = `
            position: fixed; inset: 0; z-index: 99998;
            background: rgba(0,0,0,0.6); display: flex;
            align-items: center; justify-content: center;
            animation: fadeIn 0.3s ease;
        `;
        overlay.innerHTML = `
            <div class="animate-bounce-in" style="
                background: linear-gradient(135deg, #0f0c29, #302b63, #24243e);
                border-radius: 20px; padding: 40px; text-align: center;
                max-width: 340px; width: 90%;
                box-shadow: 0 0 40px rgba(255,215,0,0.3), 0 0 0 2px rgba(255,215,0,0.15);
            ">
                <div style="font-size:56px;margin-bottom:16px;filter:drop-shadow(0 0 10px rgba(255,215,0,0.5))">${badge.name?.split(' ')[0] || '🏅'}</div>
                <h3 style="color:#FFD700;margin-bottom:8px;font-size:18px">${escapeHtml(badge.name || 'New Badge!')}</h3>
                <p style="color:rgba(255,255,255,0.7);font-size:13px;margin-bottom:16px">${escapeHtml(badge.description || '')}</p>
                ${badge.xpReward > 0 ? `<div style="color:#FFB300;font-weight:700;font-size:14px">+${badge.xpReward} XP</div>` : ''}
                <button onclick="this.closest('div[style*=fixed]').remove()" 
                        style="margin-top:20px;padding:8px 28px;background:#FFD700;color:#1a1a2e;
                               border:none;border-radius:8px;font-weight:700;cursor:pointer;font-size:14px">
                    🎉 Tuyệt vời!
                </button>
            </div>
        `;
        document.body.appendChild(overlay);
        setTimeout(() => { if (overlay.parentNode) overlay.remove(); }, 8000);
    }

    // ═══ RENDER GAMIFICATION PAGE ═══
    function renderPage() {
        const container = document.getElementById('gamificationContainer');
        if (!container) return;

        const level = _profile.level || 1;
        const xp = _profile.xp || 0;
        const progress = _calculateLevelProgress(xp);
        const nextLevelXp = _xpForNextLevel(level);

        container.innerHTML = `
            <!-- Profile Header -->
            <div style="background:linear-gradient(135deg,#667eea,#764ba2);border-radius:16px;
                        padding:24px;margin-bottom:20px;color:white;position:relative;overflow:hidden">
                <div style="position:absolute;inset:0;background:url('data:image/svg+xml,...') repeat;opacity:0.05"></div>
                <div style="display:flex;align-items:center;gap:16px;position:relative">
                    <div style="width:64px;height:64px;border-radius:50%;background:rgba(255,255,255,0.2);
                                display:flex;align-items:center;justify-content:center;font-size:28px;
                                border:3px solid rgba(255,255,255,0.3);flex-shrink:0">
                        ${_getLevelEmoji(level)}
                    </div>
                    <div style="flex:1">
                        <div style="font-size:12px;opacity:0.8;font-weight:600">LEVEL ${level}</div>
                        <div style="font-size:22px;font-weight:800">${xp} XP</div>
                        <div style="margin-top:6px;background:rgba(255,255,255,0.2);border-radius:6px;height:8px;overflow:hidden">
                            <div style="width:${progress * 100}%;height:100%;background:linear-gradient(90deg,#FFD700,#FFA000);
                                        border-radius:6px;transition:width 0.5s ease"></div>
                        </div>
                        <div style="font-size:10px;opacity:0.7;margin-top:4px">${Math.round(progress * 100)}% → Level ${level + 1} (${nextLevelXp} XP)</div>
                    </div>
                </div>
            </div>

            <!-- Stats Grid -->
            <div style="display:grid;grid-template-columns:repeat(4,1fr);gap:8px;margin-bottom:20px">
                ${_renderStatCard('🎯', _profile.quizCount || 0, 'Quiz')}
                ${_renderStatCard('🔥', _profile.streak || 0, 'Streak')}
                ${_renderStatCard('🏅', _profile.badges?.length || 0, 'Badges')}
                ${_renderStatCard('📅', _profile.attendCount || 0, 'Days')}
            </div>

            <!-- Badges Section -->
            <div style="margin-bottom:20px">
                <h4 style="margin-bottom:12px;color:var(--text-primary)">🏅 Huy Hiệu Đã Nhận</h4>
                <div style="display:flex;flex-wrap:wrap;gap:8px" id="badgeGrid">
                    ${_renderBadges()}
                </div>
            </div>

            <!-- Leaderboard -->
            <div>
                <h4 style="margin-bottom:12px;color:var(--text-primary)">🏆 Bảng Xếp Hạng</h4>
                <div id="leaderboardList">
                    ${_renderLeaderboard()}
                </div>
            </div>
        `;
    }

    function _renderStatCard(icon, value, label) {
        return `
            <div style="background:var(--card-bg);border-radius:12px;padding:12px;text-align:center;
                        border:1px solid var(--gray-100)">
                <div style="font-size:20px">${icon}</div>
                <div style="font-size:18px;font-weight:800;color:var(--text-primary)">${value}</div>
                <div style="font-size:10px;color:var(--text-muted)">${label}</div>
            </div>
        `;
    }

    function _renderBadges() {
        const badges = _profile.badges || [];
        if (badges.length === 0) {
            return `<div style="color:var(--text-muted);font-size:13px;padding:20px;text-align:center;width:100%">
                        Chưa có huy hiệu nào. Hãy hoàn thành quiz để nhận! 🎯
                    </div>`;
        }

        return badges.map(b => `
            <div style="background:linear-gradient(135deg,#1a1a2e,#16213e);border-radius:12px;padding:10px 14px;
                        display:flex;align-items:center;gap:8px;border:1px solid rgba(255,215,0,0.2);
                        min-width:140px">
                <span style="font-size:20px">${b.name?.split(' ')[0] || '🏅'}</span>
                <div>
                    <div style="font-size:11px;font-weight:700;color:#FFD700">${escapeHtml(b.name || '')}</div>
                    <div style="font-size:9px;color:rgba(255,255,255,0.5)">${escapeHtml(b.description || '')}</div>
                </div>
            </div>
        `).join('');
    }

    function _renderLeaderboard() {
        if (_leaderboard.length === 0) {
            return `<div style="color:var(--text-muted);font-size:13px;padding:20px;text-align:center">
                        Bảng xếp hạng sẽ cập nhật khi có kết quả quiz 🏆
                    </div>`;
        }

        const medals = ['🥇', '🥈', '🥉'];
        return _leaderboard.map((e, i) => `
            <div style="display:flex;align-items:center;gap:10px;padding:10px 12px;
                        background:${i < 3 ? 'var(--primary-50)' : 'var(--card-bg)'};
                        border-radius:10px;margin-bottom:6px;border:1px solid var(--gray-100)">
                <div style="width:28px;text-align:center;font-size:${i < 3 ? '18px' : '13px'};font-weight:700;
                            color:${i < 3 ? 'var(--primary)' : 'var(--text-secondary)'}">
                    ${i < 3 ? medals[i] : e.rank}
                </div>
                <div style="flex:1">
                    <div style="font-size:13px;font-weight:600;color:var(--text-primary)">${escapeHtml(e.name || e.code)}</div>
                    <div style="font-size:10px;color:var(--text-muted)">Level ${e.level} • ${e.badges || 0} badges</div>
                </div>
                <div style="text-align:right">
                    <div style="font-size:14px;font-weight:800;color:var(--primary)">${e.xp}</div>
                    <div style="font-size:9px;color:var(--text-muted)">XP</div>
                </div>
                ${e.streak > 0 ? `<div style="font-size:10px;color:#FF6B35;font-weight:700">🔥${e.streak}</div>` : ''}
            </div>
        `).join('');
    }

    function renderLeaderboardIfVisible() {
        const el = document.getElementById('leaderboardList');
        if (el) el.innerHTML = _renderLeaderboard();
    }

    // ═══ HELPERS ═══
    function _calculateLevel(xp) {
        let level = 1, acc = 0;
        while (true) {
            const needed = 50 * level;
            if (acc + needed > xp) break;
            acc += needed;
            level++;
        }
        return level;
    }

    function _calculateLevelProgress(xp) {
        let level = 1, acc = 0;
        while (true) {
            const needed = 50 * level;
            if (acc + needed > xp) return (xp - acc) / needed;
            acc += needed;
            level++;
        }
    }

    function _xpForNextLevel(level) {
        let acc = 0;
        for (let i = 1; i <= level; i++) acc += 50 * i;
        return acc;
    }

    function _getLevelEmoji(level) {
        if (level >= 20) return '👑';
        if (level >= 15) return '💎';
        if (level >= 10) return '🏅';
        if (level >= 5) return '⭐';
        return '🌱';
    }

    return { init, renderPage, showXpToast, showBadgeNotification };
})();
