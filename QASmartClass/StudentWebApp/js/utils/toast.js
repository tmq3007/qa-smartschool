/* ═══════════════════════════════════════════════════════════
   QA SmartClass — Toast Notifications
   ═══════════════════════════════════════════════════════════ */

const TOAST_ICONS = {
    success: '✅',
    error: '❌',
    warning: '⚠️',
    info: 'ℹ️'
};

const TOAST_TITLES = {
    success: 'Thành công',
    error: 'Lỗi',
    warning: 'Cảnh báo',
    info: 'Thông báo'
};

/**
 * Show a toast notification
 * @param {string} message - Toast message
 * @param {string} type - 'success' | 'error' | 'warning' | 'info'
 * @param {number} duration - Auto-dismiss in ms (default: 4000)
 */
function showToast(message, type = 'info', duration = 4000) {
    const container = document.getElementById('toastContainer');
    if (!container) return;

    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    toast.innerHTML = `
        <span class="toast-icon">${TOAST_ICONS[type] || 'ℹ️'}</span>
        <div class="toast-body">
            <div class="toast-title">${TOAST_TITLES[type] || 'Thông báo'}</div>
            <div class="toast-message">${message}</div>
        </div>
        <button class="toast-close" onclick="dismissToast(this)">&times;</button>
    `;

    container.appendChild(toast);

    // Limit to 5 toasts
    while (container.children.length > 5) {
        container.removeChild(container.firstChild);
    }

    // Auto dismiss
    if (duration > 0) {
        setTimeout(() => dismissToast(toast), duration);
    }

    return toast;
}

function dismissToast(el) {
    const toast = el.closest ? el.closest('.toast') || el : el;
    if (!toast || !toast.parentNode) return;
    toast.classList.add('toast-exit');
    setTimeout(() => {
        if (toast.parentNode) toast.parentNode.removeChild(toast);
    }, 300);
}
