/* ═══════════════════════════════════════════════════════════
   QA SmartClass — Quiz Module
   Renders interactive quizzes sent by teacher via WebSocket
   ═══════════════════════════════════════════════════════════ */

const QuizManager = (() => {
    let _currentQuiz = null;
    let _selectedAnswers = {};
    let _currentIndex = 0;
    let _submitted = false;
    let _timerInterval = null;
    let _startTime = 0;

    // ─── Initialize event listeners ───
    function init() {
        StudentConnection.on('quiz_start', handleQuizStart);
        StudentConnection.on('quiz_end', handleQuizEnd);
        StudentConnection.on('quiz_review', handleQuizReview);
        StudentConnection.on('quiz_review_focus', handleQuizReviewFocus);
        StudentConnection.on('quiz_focus', handleQuizFocus);
        StudentConnection.on('quiz_unfocus', handleQuizUnfocus);
    }

    // ═══ QUIZ START — Received from teacher ═══
    function handleQuizStart(data) {
        _currentQuiz = {
            id: data.target || data.quizId || Date.now(),
            title: data.title || 'Bài kiểm tra',
            questions: data.questions || [],
            timeLimit: data.timeLimit || 0, // seconds, 0 = unlimited
            type: data.quizType || 'multiple_choice'
        };

        _selectedAnswers = {};
        
        // Recover draft answers if they exist in sessionStorage
        const draftKey = 'quiz_draft_' + _currentQuiz.id;
        const savedDraft = sessionStorage.getItem(draftKey);
        if (savedDraft) {
            try {
                _selectedAnswers = JSON.parse(savedDraft);
                console.log('[Quiz] Recovered answers from draft:', _selectedAnswers);
            } catch (e) {
                _selectedAnswers = {};
            }
        }

        _currentIndex = 0;
        _submitted = false;
        _startTime = Date.now();

        renderQuiz();

        // Start timer if time-limited
        if (_currentQuiz.timeLimit > 0) {
            startTimer(_currentQuiz.timeLimit);
        }
    }

    // ═══ QUIZ END — Teacher stops quiz ═══
    function handleQuizEnd(data) {
        if (!_submitted && _currentQuiz) {
            submitQuiz(true); // Auto-submit
        }

        if (data.results) {
            renderResults(data.results);
        }

        stopTimer();
    }

    // ═══ RENDER QUIZ ═══
    function renderQuiz() {
        const container = document.getElementById('quizContainer');
        if (!container || !_currentQuiz) return;

        const q = _currentQuiz;
        const total = q.questions.length;

        container.innerHTML = `
            <!-- Quiz Header -->
            <div class="quiz-header">
                <div style="display:flex;align-items:center;justify-content:space-between">
                    <div>
                        <h3>${escapeHtml(q.title)}</h3>
                        <div style="font-size:13px;opacity:0.8;margin-top:4px">
                            ${total} câu hỏi ${q.timeLimit > 0 ? `• ${formatTime(q.timeLimit)}` : '• Không giới hạn'}
                        </div>
                    </div>
                    <div class="quiz-timer" id="quizTimer" style="font-size:24px;font-weight:800;font-variant-numeric:tabular-nums">
                        ${q.timeLimit > 0 ? formatTime(q.timeLimit) : '⏱'}
                    </div>
                </div>
                <div class="quiz-progress">
                    <div class="quiz-progress-bar">
                        <div class="quiz-progress-fill" id="quizProgressFill" style="width:${(1/total)*100}%"></div>
                    </div>
                    <span class="quiz-progress-text" id="quizProgressText">1/${total}</span>
                </div>
            </div>

            <!-- Question Area -->
            <div id="quizQuestionArea"></div>

            <!-- Navigation -->
            <div style="display:flex;gap:12px;margin-top:16px">
                <button class="btn btn-outline" id="quizPrevBtn" onclick="QuizManager.prevQuestion()" disabled>
                    ◀ Trước
                </button>
                <div style="flex:1"></div>
                <button class="btn btn-primary" id="quizNextBtn" onclick="QuizManager.nextQuestion()">
                    Tiếp ▶
                </button>
                <button class="btn btn-green hidden" id="quizSubmitBtn" onclick="QuizManager.submitQuiz()">
                    ✅ Nộp bài
                </button>
            </div>

            <!-- Question Navigator (dots) -->
            <div style="display:flex;justify-content:center;gap:8px;margin-top:20px;flex-wrap:wrap" id="quizDots"></div>
        `;

        renderQuestionDots(total);
        renderQuestion(_currentIndex);
    }

    // ═══ RENDER SINGLE QUESTION ═══
    function renderQuestion(index) {
        if (!_currentQuiz || !_currentQuiz.questions[index]) return;

        _currentIndex = index;
        const q = _currentQuiz.questions[index];
        const total = _currentQuiz.questions.length;
        const area = document.getElementById('quizQuestionArea');

        if (!area) return;

        area.innerHTML = `
            <div class="quiz-question-card animate-fade-in">
                <div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:8px">
                    <div style="font-size:12px;color:var(--text-muted);font-weight:600">
                        Câu ${index + 1}/${total}
                    </div>
                    ${_getDifficultyBadge(q.difficulty)}
                </div>
                <div class="quiz-question-text">${escapeHtml(q.text || q.question || '')}</div>

                ${q.image ? `<img src="${q.image}" style="max-width:100%;border-radius:8px;margin-bottom:16px" alt="Question image">` : ''}

                <div id="quizOptions">
                    ${renderOptions(q, index)}
                </div>
            </div>
        `;

        // Update progress
        const fill = document.getElementById('quizProgressFill');
        const text = document.getElementById('quizProgressText');
        if (fill) fill.style.width = `${((index + 1) / total) * 100}%`;
        if (text) text.textContent = `${index + 1}/${total}`;

        // Update navigation buttons
        const prevBtn = document.getElementById('quizPrevBtn');
        const nextBtn = document.getElementById('quizNextBtn');
        const submitBtn = document.getElementById('quizSubmitBtn');
        if (prevBtn) prevBtn.disabled = index === 0;
        if (nextBtn && submitBtn) {
            if (index === total - 1) {
                nextBtn.classList.add('hidden');
                submitBtn.classList.remove('hidden');
            } else {
                nextBtn.classList.remove('hidden');
                submitBtn.classList.add('hidden');
            }
        }

        // Update dots
        updateQuestionDots(index);
    }

    function renderOptions(q, qIndex) {
        const options = q.options || q.answers || [];
        const letters = ['A', 'B', 'C', 'D', 'E', 'F'];
        const selected = _selectedAnswers[qIndex];
        const qType = (q.type || "").toLowerCase();

        // 1. True/False
        if (qType === 'true_false' || qType === 'truefalse' || qType === 'tf') {
            return ['Đúng', 'Sai'].map((opt, i) => {
                const isSelected = selected === i;
                return `
                    <div class="quiz-option ${isSelected ? 'selected' : ''}"
                         onclick="QuizManager.selectAnswer(${qIndex}, ${i})">
                        <div class="quiz-option-letter">${opt === 'Đúng' ? '✓' : '✗'}</div>
                        <div style="flex:1">${opt}</div>
                    </div>
                `;
            }).join('');
        }

        // 2. Short Answer (Self-composed) - Security: hide correct answer
        if (qType === 'shortanswer' || qType === 'short' || qType === 'short_answer') {
            const currentVal = typeof selected === 'string' ? selected : "";
            return `
                <div style="margin-top:8px">
                    <textarea class="form-control" style="width:100%;height:80px;padding:10px;font-size:14px;border:1px solid var(--gray-300);border-radius:8px;font-family:inherit" 
                              placeholder="Nhập câu trả lời tự luận của bạn..." 
                              oninput="QuizManager.selectAnswer(${qIndex}, this.value)">${escapeHtml(currentVal)}</textarea>
                </div>
            `;
        }

        // 3. Fill in the Blank (FIB)
        if (qType === 'fillblank' || qType === 'fib' || qType === 'fill_blank') {
            const currentAnswers = selected || {};
            const blanksCount = options.length || 1;
            let html = '<div style="display:flex;flex-direction:column;gap:10px;margin-top:8px">';
            for (let bIdx = 0; bIdx < blanksCount; bIdx++) {
                const val = currentAnswers[bIdx] || "";
                html += `
                    <div style="display:flex;align-items:center;gap:10px">
                        <span style="font-size:13px;color:var(--text-muted);font-weight:600;min-width:90px">Chỗ trống ${bIdx + 1}:</span>
                        <input type="text" style="flex:1;padding:8px 12px;border:1px solid var(--gray-300);border-radius:6px;font-size:14px"
                               placeholder="Nhập từ cần điền..." value="${escapeHtml(val)}"
                               oninput="QuizManager.selectBlankAnswer(${qIndex}, ${bIdx}, this.value)">
                    </div>
                `;
            }
            html += '</div>';
            return html;
        }

        // 4. Matching (MATCH)
        if (qType === 'matching' || qType === 'match') {
            const currentMatch = selected || {};
            const pairs = options; 
            const rightOptions = pairs.map(p => typeof p === 'string' ? p : p.Right).filter(Boolean);
            
            let html = '<div style="display:flex;flex-direction:column;gap:10px;margin-top:8px">';
            pairs.forEach((p, pIdx) => {
                const leftText = typeof p === 'string' ? p : p.Left;
                const matchedVal = currentMatch[pIdx] || "";
                html += `
                    <div style="display:flex;align-items:center;gap:12px">
                        <div style="flex:1;font-size:13px;font-weight:500">${escapeHtml(leftText)}</div>
                        <div style="font-size:14px;color:var(--primary);font-weight:bold">➔</div>
                        <select style="flex:1;padding:8px;border:1px solid var(--gray-300);border-radius:6px;font-size:13px"
                                onchange="QuizManager.selectMatchAnswer(${qIndex}, ${pIdx}, this.value)">
                            <option value="">-- Chọn vế khớp --</option>
                            ${rightOptions.map(ro => `<option value="${escapeHtml(ro)}" ${matchedVal === ro ? 'selected' : ''}>${escapeHtml(ro)}</option>`).join('')}
                        </select>
                    </div>
                `;
            });
            html += '</div>';
            return html;
        }

        // 5. Ordering (ORDER)
        if (qType === 'ordering' || qType === 'order') {
            const currentOrder = Array.isArray(selected) ? selected : options.map((_, idx) => idx);
            let html = '<div style="display:flex;flex-direction:column;gap:8px;margin-top:8px">';
            currentOrder.forEach((optIdx, orderIdx) => {
                const optText = options[optIdx];
                html += `
                    <div style="display:flex;align-items:center;gap:10px;padding:8px 12px;border:1px solid var(--gray-200);border-radius:8px;background:white">
                        <span style="font-weight:bold;color:var(--primary);min-width:20px">${orderIdx + 1}</span>
                        <div style="flex:1;font-size:13px">${escapeHtml(optText)}</div>
                        <div style="display:flex;gap:4px">
                            <button class="btn btn-outline" style="padding:2px 8px;font-size:11px" onclick="QuizManager.moveOrderItem(${qIndex}, ${orderIdx}, -1)" ${orderIdx === 0 ? 'disabled' : ''}>▲</button>
                            <button class="btn btn-outline" style="padding:2px 8px;font-size:11px" onclick="QuizManager.moveOrderItem(${qIndex}, ${orderIdx}, 1)" ${orderIdx === currentOrder.length - 1 ? 'disabled' : ''}>▼</button>
                        </div>
                    </div>
                `;
            });
            html += '</div>';
            return html;
        }

        // 6. Multiple Choice (MCQ) - Default
        return options.map((opt, i) => {
            const isSelected = selected === i;
            let optText = typeof opt === 'string' ? opt : (opt.text || '');
            if (typeof optText === 'string') {
                optText = optText.replace(/^[A-F]\.\s*/i, '');
            }
            return `
                <div class="quiz-option ${isSelected ? 'selected' : ''}"
                     onclick="QuizManager.selectAnswer(${qIndex}, ${i})">
                    <div class="quiz-option-letter">${letters[i] || (i + 1)}</div>
                    <div style="flex:1">${escapeHtml(optText)}</div>
                </div>
            `;
        }).join('');
    }

    // ═══ P0-5: DIFFICULTY BADGE ═══
    function _getDifficultyBadge(difficulty) {
        if (!difficulty) return '';
        const d = (difficulty + '').toLowerCase();
        let label, bg, color;
        if (d === 'easy' || d === 'de' || d === 'dễ') {
            label = '🟢 Dễ'; bg = '#E8F5E9'; color = '#2E7D32';
        } else if (d === 'hard' || d === 'kho' || d === 'khó') {
            label = '🔴 Khó'; bg = '#FFEBEE'; color = '#C62828';
        } else {
            label = '🟡 Trung bình'; bg = '#FFF8E1'; color = '#E65100';
        }
        return `<span style="padding:2px 10px;border-radius:12px;font-size:10px;font-weight:700;
                             background:${bg};color:${color};flex-shrink:0">${label}</span>`;
    }

    // ═══ QUESTION NAVIGATION DOTS ═══
    function renderQuestionDots(total) {
        const container = document.getElementById('quizDots');
        if (!container) return;

        let html = '';
        for (let i = 0; i < total; i++) {
            html += `
                <button class="quiz-dot ${i === 0 ? 'active' : ''}" id="quizDot${i}"
                        onclick="QuizManager.goToQuestion(${i})"
                        style="width:32px;height:32px;border-radius:50%;border:2px solid var(--gray-200);
                               background:white;font-size:11px;font-weight:700;cursor:pointer;
                               display:flex;align-items:center;justify-content:center;
                               transition:all 0.15s ease;color:var(--text-secondary)">
                    ${i + 1}
                </button>
            `;
        }
        container.innerHTML = html;
    }

    function updateQuestionDots(activeIndex) {
        const total = _currentQuiz?.questions?.length || 0;
        for (let i = 0; i < total; i++) {
            const dot = document.getElementById(`quizDot${i}`);
            if (!dot) continue;

            if (i === activeIndex) {
                dot.style.background = 'var(--primary)';
                dot.style.color = 'white';
                dot.style.borderColor = 'var(--primary)';
            } else if (_selectedAnswers[i] !== undefined) {
                dot.style.background = 'var(--green-100)';
                dot.style.color = 'var(--green-dark)';
                dot.style.borderColor = 'var(--green)';
            } else {
                dot.style.background = 'white';
                dot.style.color = 'var(--text-secondary)';
                dot.style.borderColor = 'var(--gray-200)';
            }
        }
    }

    // ═══ ANSWER SELECTION ═══
    function selectAnswer(qIndex, ansIndex) {
        if (_submitted) return;
        _selectedAnswers[qIndex] = ansIndex;
        playWebClickSound();
        
        // Cache current answers draft in sessionStorage
        if (_currentQuiz) {
            sessionStorage.setItem('quiz_draft_' + _currentQuiz.id, JSON.stringify(_selectedAnswers));
        }
        
        renderQuestion(qIndex);
    }

    function selectBlankAnswer(qIndex, blankIndex, value) {
        if (_submitted) return;
        if (!_selectedAnswers[qIndex]) {
            _selectedAnswers[qIndex] = {};
        }
        _selectedAnswers[qIndex][blankIndex] = value;
        if (_currentQuiz) {
            sessionStorage.setItem('quiz_draft_' + _currentQuiz.id, JSON.stringify(_selectedAnswers));
        }
    }

    function selectMatchAnswer(qIndex, leftIndex, rightValue) {
        if (_submitted) return;
        if (!_selectedAnswers[qIndex]) {
            _selectedAnswers[qIndex] = {};
        }
        _selectedAnswers[qIndex][leftIndex] = rightValue;
        if (_currentQuiz) {
            sessionStorage.setItem('quiz_draft_' + _currentQuiz.id, JSON.stringify(_selectedAnswers));
        }
    }

    function moveOrderItem(qIndex, orderIdx, direction) {
        if (_submitted) return;
        const q = _currentQuiz.questions[qIndex];
        if (!q) return;
        const options = q.options || [];
        if (!Array.isArray(_selectedAnswers[qIndex])) {
            _selectedAnswers[qIndex] = options.map((_, idx) => idx);
        }
        const arr = _selectedAnswers[qIndex];
        const targetIdx = orderIdx + direction;
        if (targetIdx >= 0 && targetIdx < arr.length) {
            const temp = arr[orderIdx];
            arr[orderIdx] = arr[targetIdx];
            arr[targetIdx] = temp;
            renderQuestion(qIndex);
        }
    }

    // ═══ NAVIGATION ═══
    function nextQuestion() {
        if (_currentIndex < (_currentQuiz?.questions?.length || 0) - 1) {
            renderQuestion(_currentIndex + 1);
        }
    }

    function prevQuestion() {
        if (_currentIndex > 0) {
            renderQuestion(_currentIndex - 1);
        }
    }

    function goToQuestion(index) {
        renderQuestion(index);
    }

    // ═══ SUBMIT ═══
    function submitQuiz(forced = false) {
        if (_submitted) return;

        const total = _currentQuiz?.questions?.length || 0;
        const answered = Object.keys(_selectedAnswers).length;

        if (!forced && answered < total) {
            const unanswered = total - answered;
            if (!confirm(`Bạn còn ${unanswered} câu chưa trả lời. Bạn có chắc muốn nộp bài?`)) {
                return;
            }
        }

        _submitted = true;
        
        // Clean up sessionStorage draft cache
        if (_currentQuiz) {
            sessionStorage.removeItem('quiz_draft_' + _currentQuiz.id);
        }
        
        stopTimer();

        const elapsed = Math.floor((Date.now() - _startTime) / 1000);

        const mappedAnswers = {};
        for (const [qIdx, ansIdx] of Object.entries(_selectedAnswers)) {
            const q = _currentQuiz.questions[qIdx];
            if (!q) continue;
            
            let ansValue = "";
            const qType = (q.type || "").toLowerCase();
            if (qType === "true_false" || qType === "truefalse" || qType === "tf") {
                ansValue = (ansIdx === 0) ? "True" : "False";
            } else if (qType === "shortanswer" || qType === "short" || qType === "short_answer") {
                ansValue = ansIdx;
            } else if (qType === "fillblank" || qType === "fib" || qType === "fill_blank") {
                const blanks = [];
                const blanksCount = q.options ? q.options.length : 1;
                for (let b = 0; b < blanksCount; b++) {
                    blanks.push((ansIdx && ansIdx[b]) ? ansIdx[b].trim() : "");
                }
                ansValue = blanks.join(";");
            } else if (qType === "matching" || qType === "match") {
                const pairs = q.options || [];
                const matchedList = [];
                pairs.forEach((p, pIdx) => {
                    const leftText = typeof p === 'string' ? p : p.Left;
                    const matchedRight = (ansIdx && ansIdx[pIdx]) ? ansIdx[pIdx] : "";
                    matchedList.push(`${leftText}->${matchedRight}`);
                });
                ansValue = "MATCH:" + matchedList.join(",");
            } else if (qType === "ordering" || qType === "order") {
                const orderArray = Array.isArray(ansIdx) ? ansIdx : q.options.map((_, idx) => idx);
                const steps = orderArray.map(idx => q.options[idx]);
                ansValue = steps.join(" → ");
            } else {
                ansValue = String.fromCharCode(65 + parseInt(ansIdx));
            }
            mappedAnswers[qIdx] = ansValue;
        }

        // Send answers to teacher
        StudentConnection.sendQuizAnswer(_currentQuiz.id, {
            answers: mappedAnswers,
            duration: elapsed,
            answeredCount: answered,
            totalCount: total
        });

        // Save locally
        AppStorage.saveQuizResult({
            quizId: _currentQuiz.id,
            title: _currentQuiz.title,
            answers: mappedAnswers,
            duration: elapsed,
            total: total
        });

        // Show submission confirmation
        const container = document.getElementById('quizContainer');
        if (container) {
            container.innerHTML = `
                <div style="text-align:center;padding:60px 20px">
                    <div style="font-size:72px;margin-bottom:24px" class="animate-bounce-in">✅</div>
                    <h2 style="margin-bottom:12px">Đã nộp bài!</h2>
                    <p style="color:var(--text-muted);font-size:14px;margin-bottom:8px">
                        Đã trả lời ${answered}/${total} câu • Thời gian: ${formatTime(elapsed)}
                    </p>
                    <p style="color:var(--text-muted);font-size:13px">
                        Kết quả sẽ được GV thông báo sau
                    </p>
                    <button class="btn btn-primary mt-4" onclick="navigateTo('dashboard')">
                        🏠 Quay về trang chủ
                    </button>
                </div>
            `;
        }

        showToast(`Đã nộp bài kiểm tra (${answered}/${total} câu)`, 'success');
        addActivity('✅', `Nộp bài kiểm tra: ${answered}/${total} câu`, '#D1FAE5');
    }

    // ═══ RESULTS ═══
    function renderResults(results) {
        const container = document.getElementById('quizContainer');
        if (!container) return;

        const score = results.score || 0;
        const total = results.total || 0;
        const percent = total > 0 ? Math.round((score / total) * 100) : 0;

        const color = percent >= 80 ? 'var(--green)' :
                      percent >= 50 ? 'var(--orange)' : 'var(--red)';

        container.innerHTML = `
            <div style="text-align:center;padding:40px 20px">
                <div style="font-size:64px;margin-bottom:16px" class="animate-bounce-in">
                    ${percent >= 80 ? '🎉' : percent >= 50 ? '👍' : '📖'}
                </div>
                <h2 style="margin-bottom:16px">Kết quả kiểm tra</h2>

                <div style="width:120px;height:120px;border-radius:50%;
                            border:6px solid ${color};margin:0 auto 20px;
                            display:flex;align-items:center;justify-content:center;
                            flex-direction:column;background:white;box-shadow:var(--shadow-md)">
                    <div style="font-size:32px;font-weight:800;color:${color}">${score}</div>
                    <div style="font-size:12px;color:var(--text-muted)">/ ${total}</div>
                </div>

                <div style="font-size:18px;font-weight:700;color:${color};margin-bottom:8px">
                    ${percent}% — ${percent >= 80 ? 'Xuất sắc!' : percent >= 50 ? 'Khá tốt!' : 'Cần cố gắng!'}
                </div>

                ${results.message ? `<p style="color:var(--text-secondary);font-size:14px">${escapeHtml(results.message)}</p>` : ''}

                <button class="btn btn-primary mt-4" onclick="navigateTo('dashboard')">
                    🏠 Quay về trang chủ
                </button>
            </div>
        `;

        showToast(`Kết quả: ${score}/${total} (${percent}%)`, percent >= 50 ? 'success' : 'warning');
    }

    // ═══ TIMER ═══
    function startTimer(seconds) {
        let remaining = seconds;
        const timerEl = document.getElementById('quizTimer');

        _timerInterval = setInterval(() => {
            remaining--;
            if (timerEl) {
                timerEl.textContent = formatTime(remaining);
                if (remaining <= 60) {           // P2-1: match WPF threshold (60s)
                    timerEl.style.color = '#FF6B6B';
                    if (remaining <= 10) {
                        timerEl.style.animation = 'pulse 1s ease-in-out infinite';
                    }
                }
            }

            if (remaining <= 0) {
                stopTimer();
                showToast('⏰ Hết giờ! Bài đang được nộp...', 'warning');
                submitQuiz(true);
            }
        }, 1000);
    }

    function stopTimer() {
        if (_timerInterval) {
            clearInterval(_timerInterval);
            _timerInterval = null;
        }
    }

    // ═══ HELPERS ═══
    function formatTime(secs) {
        const m = Math.floor(secs / 60);
        const s = secs % 60;
        return `${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
    }

    // ═══ QUIZ REVIEW — Teacher shares correct answers ═══
    let _reviewData = null;

    function handleQuizReview(data) {
        _reviewData = data.data || data;
        navigateTo('quiz');
        renderQuizReview();
    }

    function renderQuizReview() {
        const container = document.getElementById('quizContainer');
        if (!container || !_reviewData) return;

        const review = typeof _reviewData === 'string' ? JSON.parse(_reviewData) : _reviewData;
        const questions = review.questions || [];
        const title = review.title || _currentQuiz?.title || 'Xem lại bài kiểm tra';
        const score = review.score;
        const total = questions.length;

        let html = `
            <div class="quiz-review-wrapper" style="padding:4px 0">
                <div class="quiz-header" style="margin-bottom:16px">
                    <div style="display:flex;align-items:center;justify-content:space-between">
                        <div>
                            <h3>📋 ${escapeHtml(title)}</h3>
                            <div style="font-size:13px;opacity:0.8;margin-top:4px">
                                Xem lại ${total} câu hỏi
                                ${score !== undefined ? ` • Điểm: ${score}/${total}` : ''}
                            </div>
                        </div>
                        <div style="background:var(--primary-50);color:var(--primary);padding:4px 12px;border-radius:8px;font-size:12px;font-weight:700">
                            📝 REVIEW
                        </div>
                    </div>
                </div>
        `;

        const letters = ['A', 'B', 'C', 'D', 'E', 'F'];

        questions.forEach((q, idx) => {
            const correctIdx = q.correctIndex ?? q.correct ?? -1;
            const studentIdx = _selectedAnswers[idx] ?? q.studentAnswer ?? -1;
            const isCorrect = correctIdx === studentIdx && studentIdx >= 0;
            const options = q.options || q.answers || [];

            html += `
                <div class="quiz-question-card" id="quizReviewQ${idx}" style="margin-bottom:12px;border-left:4px solid ${isCorrect ? 'var(--green)' : studentIdx >= 0 ? 'var(--red)' : 'var(--gray-300)'}">
                    <div style="display:flex;align-items:center;gap:8px;margin-bottom:8px">
                        <span style="font-size:12px;color:var(--text-muted);font-weight:600">Câu ${idx + 1}</span>
                        ${isCorrect
                            ? '<span style="font-size:11px;background:var(--green-50);color:var(--green-dark);padding:2px 8px;border-radius:4px;font-weight:600">✅ Đúng</span>'
                            : studentIdx >= 0
                                ? '<span style="font-size:11px;background:var(--red-50);color:var(--red);padding:2px 8px;border-radius:4px;font-weight:600">❌ Sai</span>'
                                : '<span style="font-size:11px;background:var(--gray-100);color:var(--gray-500);padding:2px 8px;border-radius:4px;font-weight:600">⬜ Bỏ qua</span>'
                        }
                    </div>
                    <div class="quiz-question-text" style="font-size:14px;margin-bottom:10px">${escapeHtml(q.text || q.question || '')}</div>
            `;

            options.forEach((opt, oi) => {
                const optText = typeof opt === 'string' ? opt : opt.text || '';
                const isStudentPick = oi === studentIdx;
                const isCorrectOpt = oi === correctIdx;

                let borderColor = 'var(--gray-200)';
                let bgColor = 'white';
                let icon = '';

                if (isCorrectOpt) {
                    borderColor = 'var(--green)';
                    bgColor = 'var(--green-50)';
                    icon = ' ✅';
                }
                if (isStudentPick && !isCorrectOpt) {
                    borderColor = 'var(--red)';
                    bgColor = 'var(--red-50)';
                    icon = ' ❌';
                }

                html += `
                    <div style="display:flex;align-items:center;gap:10px;padding:8px 12px;margin-bottom:4px;
                                border:2px solid ${borderColor};border-radius:8px;background:${bgColor};
                                font-size:13px;transition:none">
                        <div style="width:24px;height:24px;border-radius:50%;background:${isCorrectOpt ? 'var(--green)' : isStudentPick ? 'var(--red)' : 'var(--gray-100)'};
                                    color:${isCorrectOpt || isStudentPick ? 'white' : 'var(--text-secondary)'};
                                    display:flex;align-items:center;justify-content:center;font-size:11px;font-weight:700;flex-shrink:0">
                            ${letters[oi] || (oi + 1)}
                        </div>
                        <div style="flex:1">${escapeHtml(optText)}${icon}</div>
                    </div>
                `;
            });

            // Explanation if available
            if (q.explanation) {
                html += `
                    <div style="margin-top:8px;padding:8px 12px;background:var(--primary-50);border-radius:8px;font-size:12px;color:var(--primary-dark)">
                        💡 ${escapeHtml(q.explanation)}
                    </div>
                `;
            }

            html += `</div>`;
        });

        html += `
                <div style="text-align:center;margin-top:20px">
                    <button class="btn btn-primary" onclick="navigateTo('dashboard')">
                        🏠 Quay về trang chủ
                    </button>
                </div>
            </div>
        `;

        container.innerHTML = html;

        // Trigger vibration & shake for wrong answers in review
        questions.forEach((q, idx) => {
            const correctIdx = q.correctIndex ?? q.correct ?? -1;
            const studentIdx = _selectedAnswers[idx] ?? q.studentAnswer ?? -1;
            const isCorrect = correctIdx === studentIdx && studentIdx >= 0;
            if (!isCorrect && studentIdx >= 0) {
                if (navigator.vibrate) {
                    navigator.vibrate(50);
                }
                const el = document.getElementById(`quizReviewQ${idx}`);
                if (el) {
                    el.classList.add('shake-animation');
                    el.addEventListener('animationend', () => {
                        el.classList.remove('shake-animation');
                    }, { once: true });
                }
            }
        });
    }

    function handleQuizReviewFocus(data) {
        const qIdx = (data.questionIndex || 1) - 1; // Convert 1-based to 0-based
        const el = document.getElementById(`quizReviewQ${qIdx}`);
        if (el) {
            el.scrollIntoView({ behavior: 'smooth', block: 'center' });
            // Pulse highlight
            el.style.boxShadow = '0 0 0 3px var(--primary), 0 4px 16px rgba(37,99,235,0.3)';
            setTimeout(() => { el.style.boxShadow = ''; }, 3000);
        }
        if (currentPage !== 'quiz') navigateTo('quiz');
    }

    // ═══ QUIZ FOCUS — Teacher highlights a specific question ═══
    function handleQuizFocus(data) {
        const qIdx = (data.questionIndex || 1) - 1;
        if (_currentQuiz && !_submitted) {
            renderQuestion(qIdx);
            showToast(`🎯 GV yêu cầu xem câu ${qIdx + 1}`, 'warning', 3000);
        }
        if (currentPage !== 'quiz') navigateTo('quiz');
    }

    function handleQuizUnfocus() {
        // No-op for now — focus naturally clears
    }

    function playWebClickSound() {
        try {
            const AudioContextClass = window.AudioContext || window.webkitAudioContext;
            if (!AudioContextClass) return;
            const audioCtx = new AudioContextClass();
            const osc = audioCtx.createOscillator();
            const gain = audioCtx.createGain();
            osc.connect(gain);
            gain.connect(audioCtx.destination);
            
            osc.type = 'sine';
            osc.frequency.setValueAtTime(600, audioCtx.currentTime);
            gain.gain.setValueAtTime(0.04, audioCtx.currentTime);
            gain.gain.exponentialRampToValueAtTime(0.001, audioCtx.currentTime + 0.08);
            
            osc.start(audioCtx.currentTime);
            osc.stop(audioCtx.currentTime + 0.08);
        } catch (e) {
            console.warn('[Audio] Play click failed:', e);
        }
    }

    return {
        init,
        selectAnswer,
        selectBlankAnswer,
        selectMatchAnswer,
        moveOrderItem,
        nextQuestion,
        prevQuestion,
        goToQuestion,
        submitQuiz: () => submitQuiz(false),
        renderQuizReview
    };
})();
