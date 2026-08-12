/* ═══════════════════════════════════════════════════════════
   QA SmartClass — LocalStorage Manager
   ═══════════════════════════════════════════════════════════ */

const AppStorage = {
    KEYS: {
        STUDENT_INFO: 'qasc_student_info',
        SESSION: 'qasc_session',
        SETTINGS: 'qasc_settings',
        CHAT_HISTORY: 'qasc_chat_history',
        QUIZ_RESULTS: 'qasc_quiz_results',
        APP_STATE: 'qasc_app_state',
        GAMIFICATION: 'qasc_gamification'
    },

    // ─── Student Identity ───
    saveStudentInfo(info) {
        localStorage.setItem(this.KEYS.STUDENT_INFO, JSON.stringify(info));
    },

    getStudentInfo() {
        try {
            return JSON.parse(localStorage.getItem(this.KEYS.STUDENT_INFO)) || {};
        } catch { return {}; }
    },

    // ─── Session Data ───
    saveSession(data) {
        localStorage.setItem(this.KEYS.SESSION, JSON.stringify({
            ...data,
            timestamp: Date.now()
        }));
    },

    getSession() {
        try {
            return JSON.parse(localStorage.getItem(this.KEYS.SESSION)) || {};
        } catch { return {}; }
    },

    clearSession() {
        localStorage.removeItem(this.KEYS.SESSION);
    },

    // ─── Settings ───
    saveSettings(settings) {
        localStorage.setItem(this.KEYS.SETTINGS, JSON.stringify(settings));
    },

    getSettings() {
        try {
            return JSON.parse(localStorage.getItem(this.KEYS.SETTINGS)) || {
                soundEnabled: true,
                notificationsEnabled: true,
                fontSize: 'normal',
                theme: 'light'
            };
        } catch {
            return { soundEnabled: true, notificationsEnabled: true, fontSize: 'normal', theme: 'light' };
        }
    },

    // ─── Chat History ───
    saveChatMessage(msg) {
        const history = this.getChatHistory();
        history.push({ ...msg, timestamp: Date.now() });
        // Keep last 200 messages
        if (history.length > 200) history.splice(0, history.length - 200);
        localStorage.setItem(this.KEYS.CHAT_HISTORY, JSON.stringify(history));
    },

    getChatHistory() {
        try {
            return JSON.parse(localStorage.getItem(this.KEYS.CHAT_HISTORY)) || [];
        } catch { return []; }
    },

    clearChatHistory() {
        localStorage.removeItem(this.KEYS.CHAT_HISTORY);
    },

    // ─── Quiz Results ───
    saveQuizResult(result) {
        const results = this.getQuizResults();
        results.push({ ...result, timestamp: Date.now() });
        localStorage.setItem(this.KEYS.QUIZ_RESULTS, JSON.stringify(results));
    },

    getQuizResults() {
        try {
            return JSON.parse(localStorage.getItem(this.KEYS.QUIZ_RESULTS)) || [];
        } catch { return []; }
    },

    // ─── App State (Overlay Persistence) ───
    // Tracks active overlays & context so late-join/refresh can restore
    saveAppState(state) {
        localStorage.setItem(this.KEYS.APP_STATE, JSON.stringify({
            ...state,
            timestamp: Date.now()
        }));
    },

    getAppState() {
        try {
            const raw = JSON.parse(localStorage.getItem(this.KEYS.APP_STATE)) || {};
            // Expire after 4 hours to avoid stale ghost overlays
            if (raw.timestamp && (Date.now() - raw.timestamp > 4 * 60 * 60 * 1000)) {
                this.clearAppState();
                return {};
            }
            return raw;
        } catch { return {}; }
    },

    clearAppState() {
        localStorage.removeItem(this.KEYS.APP_STATE);
    },

    // Convenience: save just the overlay flags
    saveOverlayState(overlays) {
        const current = this.getAppState();
        this.saveAppState({ ...current, overlays });
    },

    // Convenience: save current page
    saveCurrentPage(page) {
        const current = this.getAppState();
        this.saveAppState({ ...current, currentPage: page });
    },

    // ─── Gamification (XP, Badges, Level) ───
    saveGamification(data) {
        localStorage.setItem(this.KEYS.GAMIFICATION, JSON.stringify({
            ...data,
            lastSaved: Date.now()
        }));
    },

    getGamification() {
        try {
            return JSON.parse(localStorage.getItem(this.KEYS.GAMIFICATION)) || null;
        } catch { return null; }
    }
};
