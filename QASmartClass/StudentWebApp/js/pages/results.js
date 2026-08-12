/* ═══════════════════════════════════════════════════════════
   QA SmartClass — Results / History Module
   Displays student quiz history and performance charts
   4-Stat Cards + Bar Chart + History List (WPF Parity)
   ═══════════════════════════════════════════════════════════ */

const ResultsManager = (() => {

    // ─── Initialize ───
    function init() {
        // Listen for quiz results from teacher
        StudentConnection.on('quiz_result', handleQuizResult);
        StudentConnection.on('quiz_results_batch', handleResultsBatch);
    }

    function handleQuizResult(data) {
        // Save result
        AppStorage.saveQuizResult({
            quizId: data.quizId || data.target,
            title: data.title || 'Bài kiểm tra',
            score: data.score || 0,
            total: data.total || 0,
            duration: data.duration || 0,
            receivedAt: new Date().toISOString()
        });

        renderResults();
        showToast(`📊 Kết quả: ${data.score || 0}/${data.total || 0}`, 'success');
        if (typeof addDashNotification === 'function') {
            addDashNotification('result', 'Kết quả mới', `${data.title || 'Quiz'}: ${data.score}/${data.total}`);
        }
    }

    function handleResultsBatch(data) {
        if (data.results && Array.isArray(data.results)) {
            data.results.forEach(r => {
                AppStorage.saveQuizResult({
                    ...r,
                    receivedAt: r.receivedAt || new Date().toISOString()
                });
            });
            renderResults();
        }
    }

    // ═══ RENDER RESULTS ═══
    function renderResults() {
        const container = document.getElementById('resultsContainer');
        if (!container) return;

        const results = AppStorage.getQuizResults();

        if (results.length === 0) {
            container.innerHTML = `
                <div class="empty-state">
                    <div class="empty-icon">📊</div>
                    <div class="empty-text">Chưa có kết quả học tập</div>
                    <p class="text-muted text-sm mt-2">Kết quả quiz sẽ được hiển thị ở đây sau khi em hoàn thành kiểm tra</p>
                </div>
            `;
            return;
        }

        // Calculate stats
        const totalQuizzes = results.length;
        const avgScore = results.length > 0
            ? Math.round(results.reduce((s, r) => s + ((r.score || 0) / (r.total || 1)) * 100, 0) / results.length)
            : 0;
        const bestScore = Math.max(...results.map(r => Math.round(((r.score || 0) / (r.total || 1)) * 100)));
        const totalCorrect = results.reduce((s, r) => s + (r.score || 0), 0);
        const totalQuestions = results.reduce((s, r) => s + (r.total || 0), 0);

        container.innerHTML = `
            <div class="results-layout animate-fade-in">
                <!-- 4 Stat Cards -->
                <div class="results-stats-grid">
                    ${_renderStatCard('📝', 'Số bài kiểm tra', totalQuizzes, '#DBEAFE', '#1976D2')}
                    ${_renderStatCard('📈', 'Điểm trung bình', `${avgScore}%`, avgScore >= 70 ? '#D1FAE5' : '#FEF3C7', avgScore >= 70 ? '#2E7D32' : '#F57F17')}
                    ${_renderStatCard('🏆', 'Điểm cao nhất', `${bestScore}%`, '#F3E8FF', '#7B1FA2')}
                    ${_renderStatCard('✅', 'Tổng câu đúng', `${totalCorrect}/${totalQuestions}`, '#ECFDF5', '#059669')}
                </div>

                <!-- Performance Bar Chart -->
                <div class="results-chart-card">
                    <div class="results-chart-title">📊 Biểu đồ tiến bộ</div>
                    <div class="results-bar-container">
                        ${results.slice(-10).map((r, i) => {
                            const pct = Math.round(((r.score || 0) / (r.total || 1)) * 100);
                            const color = pct >= 80 ? 'var(--green)' : pct >= 50 ? 'var(--orange)' : 'var(--red)';
                            return `
                                <div class="results-bar-item" title="${r.title || 'Quiz'}: ${pct}%">
                                    <div class="results-bar-label" style="color:${color}">${pct}%</div>
                                    <div class="results-bar" style="height:${Math.max(pct, 8)}%;background:${color}"></div>
                                    <div class="results-bar-index">${i + 1}</div>
                                </div>
                            `;
                        }).join('')}
                    </div>
                    <div style="text-align:center;font-size:10px;color:var(--text-muted);margin-top:8px">
                        10 bài kiểm tra gần nhất
                    </div>
                </div>

                <!-- Quiz History -->
                <div class="results-history-card">
                    <div class="results-chart-title">📋 Lịch sử kiểm tra</div>
                    ${results.slice().reverse().map(r => _renderResultRow(r)).join('')}
                </div>
            </div>
        `;

        // Update dashboard stats
        _updateDashboardStats(totalQuizzes, avgScore);
    }

    function _renderStatCard(icon, label, value, bgColor, valueColor) {
        return `
            <div class="results-stat-card">
                <div class="results-stat-icon" style="background:${bgColor}">${icon}</div>
                <div class="results-stat-value" style="color:${valueColor || 'var(--text-primary)'}">${value}</div>
                <div class="results-stat-label">${label}</div>
            </div>
        `;
    }

    function _renderResultRow(r) {
        const pct = Math.round(((r.score || 0) / (r.total || 1)) * 100);
        const color = pct >= 80 ? 'var(--green)' :
                      pct >= 50 ? 'var(--orange)' : 'var(--red)';
        const emoji = pct >= 80 ? '🎉' : pct >= 50 ? '👍' : '📖';
        // P1-7: use receivedAt or timestamp (both are valid ISO strings)
        const dateRaw = r.receivedAt || r.timestamp;
        const date = dateRaw
            ? new Date(dateRaw).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit' })
            : '--';

        return `
            <div class="results-row">
                <div class="results-row-emoji">${emoji}</div>
                <div class="results-row-info">
                    <div class="results-row-title">${escapeHtml(r.title || 'Bài kiểm tra')}</div>
                    <div class="results-row-meta">
                        ${date} • ${r.total || 0} câu ${r.duration ? `• ${Math.floor(r.duration / 60)}p${r.duration % 60}s` : ''}
                    </div>
                </div>
                <div class="results-row-score">
                    <div class="results-row-score-value" style="color:${color}">${r.score || 0}/${r.total || 0}</div>
                    <div class="results-row-score-pct" style="color:${color}">${pct}%</div>
                </div>
            </div>
        `;
    }

    function _updateDashboardStats(totalQuizzes, avgScore) {
        const statQuiz = document.getElementById('statQuiz');
        const statScore = document.getElementById('statScore');
        if (statQuiz) statQuiz.textContent = totalQuizzes;
        if (statScore) statScore.textContent = avgScore > 0 ? `${avgScore}%` : '—';
    }

    return {
        init,
        renderResults
    };
})();
