/* ═══════════════════════════════════════════════════════════
   QA SmartClass — Survey Module
   Handles surveys from teacher: voting, star rating, history
   Mirrors WPF StudentSurveyPage.xaml
   ═══════════════════════════════════════════════════════════ */

const SurveyManager = (() => {
    let _activeSurvey = null;
    let _completedCount = 0;
    let _starRating = 0;
    let _history = [];
    let _isSubmittingStar = false;

    function init() {
        StudentConnection.on('survey_start', handleSurveyStart);
        StudentConnection.on('survey_end', handleSurveyEnd);
        loadHistory();
        render();
        setupDraggable();
    }

    // ═══ EVENTS ═══
    function handleSurveyStart(data) {
        let questionText = 'Khảo sát từ giáo viên';
        let options = null;
        let surveyId = `survey_${Date.now()}`;
        let isAnonymous = true;

        if (data) {
            let parsedData = data;
            if (typeof data === 'string') {
                try {
                    parsedData = JSON.parse(data);
                } catch (e) {
                    // Ignore, handle as legacy raw string
                }
            }

            if (parsedData && typeof parsedData === 'object') {
                if (parsedData.Action === 'SURVEY_START') {
                    const payload = parsedData.Payload || {};
                    surveyId = payload.SurveyId || surveyId;
                    questionText = payload.QuestionText || payload.Title || questionText;
                    options = payload.Options || options;
                    isAnonymous = payload.IsAnonymous !== false;
                } else if (parsedData.Payload) {
                    const payload = parsedData.Payload;
                    surveyId = payload.SurveyId || surveyId;
                    questionText = payload.QuestionText || payload.Title || questionText;
                    options = payload.Options || options;
                    isAnonymous = payload.IsAnonymous !== false;
                } else {
                    surveyId = parsedData.id || parsedData.target || surveyId;
                    questionText = parsedData.question || parsedData.data || questionText;
                    options = parsedData.options || options;
                    isAnonymous = parsedData.isAnonymous !== false;
                }
            } else if (typeof data === 'string') {
                if (data.includes('|')) {
                    const parts = data.split('|');
                    questionText = parts[0].replace('Khảo sát (custom): ', '').replace('Khảo sát (satisfaction): ', '');
                    if (parts.length > 1) {
                        options = parts[1].split(',');
                    }
                } else {
                    questionText = data;
                }
            }
        }

        _activeSurvey = {
            id: surveyId,
            question: questionText,
            options: options,
            isAnonymous: isAnonymous,
            voted: false,
            vote: null
        };
        render();
        showToast('📊 GV gửi khảo sát mới!', 'info', 5000);
        if (typeof addActivity === 'function') {
            addActivity('📊', `Khảo sát: ${_activeSurvey.question}`, '#EDE9FE');
        }
        if (typeof addDashNotification === 'function') {
            addDashNotification('info', 'Khảo sát mới', _activeSurvey.question);
        }
    }

    function handleSurveyEnd(data) {
        _activeSurvey = null;
        render();
    }

    // ═══ VOTE ═══
    async function vote(choice, voteKey) {
        if (!_activeSurvey || _activeSurvey.voted) return;

        // Immediately disable DOM buttons to prevent double click
        const buttons = document.querySelectorAll('.survey-vote-btn');
        buttons.forEach(btn => btn.disabled = true);

        let selectedIndex = 0;
        if (voteKey && typeof voteKey === 'string') {
            if (voteKey.startsWith('opt')) {
                selectedIndex = parseInt(voteKey.replace('opt', ''), 10) || 0;
            } else if (voteKey === 'yes') {
                selectedIndex = 0;
            } else if (voteKey === 'no') {
                selectedIndex = 1;
            } else if (voteKey === 'maybe') {
                selectedIndex = 2;
            }
        }

        const config = StudentConnection.getConfig() || {};
        let code = config.studentCode || 'HS001';
        let name = config.studentName || 'Học sinh';

        if (_activeSurvey.isAnonymous) {
            try {
                code = await sha256(code + _activeSurvey.id);
            } catch (e) {
                // Fallback to pure JS SHA-256 hash if SubtleCrypto is unavailable
                code = sha256Fallback(code + _activeSurvey.id);
            }
            name = 'Học sinh ẩn danh';
        }

        // Send to teacher bridge conforming to new SURVEY_VOTE payload structure
        const sent = StudentConnection.send({
            type: 'survey_vote',
            Payload: {
                SurveyId: _activeSurvey.id,
                StudentCode: code,
                StudentName: name,
                SelectedOptionIndex: selectedIndex,
                SelectedOptionText: choice
            }
        });

        if (!sent) {
            // Rollback if sending failed
            buttons.forEach(btn => btn.disabled = false);
            showToast('❌ Gửi khảo sát thất bại. Vui lòng kết nối lại mạng.', 'error');
            return;
        }

        _activeSurvey.voted = true;
        _activeSurvey.vote = choice;
        _completedCount++;

        // Save to history
        _history.unshift({
            question: _activeSurvey.question,
            vote: choice,
            time: new Date().toISOString()
        });
        saveHistory();

        // Gamification: Trigger Confetti & Click Sound!
        triggerConfetti();
        playClickSound();

        render();
        showToast('✅ Cảm ơn bạn đã tham gia khảo sát!', 'success');
        if (typeof addActivity === 'function') {
            addActivity('✅', `Đã khảo sát: ${choice}`, '#D1FAE5');
        }
    }

    // ═══ STAR RATING ═══
    function rateStar(stars) {
        if (_isSubmittingStar) return;
        _isSubmittingStar = true;

        const sent = StudentConnection.send({
            type: 'lesson_rating',
            stars: stars
        });

        if (!sent) {
            _isSubmittingStar = false;
            showToast('❌ Gửi đánh giá thất bại. Vui lòng thử lại.', 'error');
            return;
        }

        _starRating = stars;
        _isSubmittingStar = false;
        render();
        showToast(`⭐ Đã đánh giá ${stars}/5 sao!`, 'success', 2000);
    }

    // ═══ RENDER ═══
    function render() {
        const container = document.getElementById('surveyPageContent');
        if (!container) return;

        container.innerHTML = `
            <div class="survey-page-card animate-fade-in">
                <div class="survey-page-header">
                    <span style="font-size:22px">📋</span>
                    <div>
                        <div style="font-size:18px;font-weight:700;color:#212121">Khảo sát</div>
                        <div style="font-size:12px;color:#757575">Tham gia khảo sát từ giáo viên để cải thiện chất lượng giảng dạy</div>
                    </div>
                </div>

                ${renderActiveSurvey()}
                ${renderThankYou()}
                ${renderStarRating()}
                ${renderHistory()}
            </div>
        `;
    }

    function renderActiveSurvey() {
        if (!_activeSurvey || _activeSurvey.voted) return '';

        // P1-6: Render dynamic options if provided, otherwise use default
        let optionsHtml;
        const customOpts = _activeSurvey.options;
        if (customOpts && Array.isArray(customOpts) && customOpts.length > 0) {
            const colors = ['green', 'red', 'yellow', 'blue', 'purple'];
            optionsHtml = customOpts.map((opt, i) => `
                <button class="survey-vote-btn ${colors[i % colors.length]}"
                        onclick="SurveyManager.vote(${JSON.stringify(opt)}, 'opt' + ${i})">
                    ${escapeHtml(opt)}
                </button>
            `).join('');
        } else {
            optionsHtml = `
                <button class="survey-vote-btn green" onclick="SurveyManager.vote('👍 Đồng ý', 'opt0')">
                    👍 Đồng ý
                </button>
                <button class="survey-vote-btn red" onclick="SurveyManager.vote('👎 Không đồng ý', 'opt1')">
                    👎 Không đồng ý
                </button>
                <button class="survey-vote-btn yellow" onclick="SurveyManager.vote('🤔 Có thể', 'opt2')">
                    🤔 Có thể
                </button>
            `;
        }

        return `
            <div class="survey-active-card">
                <div style="font-size:14px;font-weight:700;color:#1565C0;margin-bottom:12px">
                    📊 Khảo sát từ GV
                </div>
                <div style="font-size:14px;font-weight:600;color:#212121;margin-bottom:16px;line-height:1.6">
                    ${escapeHtml(_activeSurvey.question)}
                </div>
                <div class="survey-vote-row">${optionsHtml}</div>
            </div>
        `;
    }

    function renderThankYou() {
        if (!_activeSurvey || !_activeSurvey.voted) return '';

        return `
            <div class="survey-thankyou-card">
                <div style="font-size:16px;font-weight:600;color:#2E7D32;text-align:center">
                    ✅ Cảm ơn bạn đã tham gia khảo sát!
                </div>
            </div>
        `;
    }

    function renderStarRating() {
        const stars = [1, 2, 3, 4, 5].map(n => {
            const filled = n <= _starRating;
            return `<span class="survey-star ${filled ? 'filled' : ''}"
                          onclick="SurveyManager.rateStar(${n})"
                          title="${n} sao">⭐</span>`;
        }).join('');

        const resultText = _starRating > 0
            ? `✅ Bạn đã đánh giá ${_starRating}/5 sao`
            : '';

        return `
            <div class="survey-star-card">
                <div style="font-size:13px;font-weight:600;color:#F57C00;margin-bottom:10px">
                    ⭐ Đánh giá bài giảng hôm nay
                </div>
                <div class="survey-star-row">${stars}</div>
                ${resultText ? `<div style="font-size:12px;color:#2E7D32;text-align:center;margin-top:10px">${resultText}</div>` : ''}
            </div>
        `;
    }

    function renderHistory() {
        return `
            <div class="survey-history-card">
                <div style="display:flex;justify-content:space-between;align-items:center">
                    <div style="font-size:12px;font-weight:600;color:#2E7D32">
                        ✅ Khảo sát đã hoàn thành
                    </div>
                    <div style="font-size:18px;font-weight:700;color:#2E7D32">
                        ${_completedCount}
                    </div>
                </div>
                <div style="font-size:10px;color:#9E9E9E;margin-top:4px">
                    ${_history.length > 0
                        ? `Gần nhất: ${escapeHtml(_history[0].question)} (${_history[0].vote})`
                        : 'Chưa có khảo sát nào được hoàn thành'}
                </div>
            </div>
        `;
    }

    // ═══ PERSISTENCE ═══
    function saveHistory() {
        try {
            localStorage.setItem('qasc_surveys', JSON.stringify({
                history: _history.slice(0, 20),
                completedCount: _completedCount,
                starRating: _starRating
            }));
        } catch {}
    }

    function loadHistory() {
        try {
            const data = JSON.parse(localStorage.getItem('qasc_surveys'));
            if (data) {
                _history = data.history || [];
                _completedCount = data.completedCount || 0;
                _starRating = data.starRating || 0;
            }
        } catch {}
    }

    async function sha256(message) {
        const msgBuffer = new TextEncoder().encode(message);
        const hashBuffer = await crypto.subtle.digest('SHA-256', msgBuffer);
        const hashArray = Array.from(new Uint8Array(hashBuffer));
        const hashHex = hashArray.map(b => b.toString(16).padStart(2, '0')).join('');
        return hashHex;
    }

    function sha256Fallback(ascii) {
        function rightRotate(value, amount) {
            return (value >>> amount) | (value << (32 - amount));
        }
        var mathPow = Math.pow;
        var maxWord = mathPow(2, 32);
        var lengthProperty = 'length';
        var i, j;
        var result = '';
        var words = [];
        var asciiLength = ascii[lengthProperty] * 8;
        
        var hash = sha256Fallback.h = sha256Fallback.h || [];
        var k = sha256Fallback.k = sha256Fallback.k || [];
        var primeCounter = k[lengthProperty];

        var getPrime = function(candidate) {
            for (var divisor = 2; divisor * divisor <= candidate; divisor++) {
                if (candidate % divisor === 0) return false;
            }
            return true;
        };
        var candidate = 2;
        while (primeCounter < 64) {
            if (getPrime(candidate)) {
                k[primeCounter] = (mathPow(candidate, 1/3) * maxWord) | 0;
                hash[primeCounter] = (mathPow(candidate, 1/2) * maxWord) | 0;
                primeCounter++;
            }
            candidate++;
        }
        
        var wordsLength = ((asciiLength + 64) >>> 9 << 4) + 15;
        for (i = 0; i < wordsLength; i++) words[i] = 0;
        for (i = 0; i < ascii[lengthProperty]; i++) {
            words[i >>> 2] |= ascii.charCodeAt(i) << (24 - (i % 4) * 8);
        }
        words[ascii[lengthProperty] >>> 2] |= 0x80 << (24 - (ascii[lengthProperty] % 4) * 8);
        words[wordsLength] = asciiLength;
        
        var tempHash = hash.slice(0);
        for (i = 0; i < wordsLength; i += 16) {
            var w = words.slice(i, i + 16);
            var oldHash = tempHash.slice(0);
            for (j = 0; j < 64; j++) {
                if (j >= 16) {
                    var s0 = rightRotate(w[j - 15], 7) ^ rightRotate(w[j - 15], 18) ^ (w[j - 15] >>> 3);
                    var s1 = rightRotate(w[j - 2], 17) ^ rightRotate(w[j - 2], 19) ^ (w[j - 2] >>> 10);
                    w[j] = (w[j - 16] + s0 + w[j - 7] + s1) | 0;
                }
                var a = tempHash[0], e = tempHash[4];
                var s1_e = rightRotate(e, 6) ^ rightRotate(e, 11) ^ rightRotate(e, 25);
                var ch = (e & tempHash[5]) ^ (~e & tempHash[6]);
                var temp1 = (tempHash[7] + s1_e + ch + k[j] + (w[j] || 0)) | 0;
                var s0_a = rightRotate(a, 2) ^ rightRotate(a, 13) ^ rightRotate(a, 22);
                var maj = (a & tempHash[1]) ^ (a & tempHash[2]) ^ (tempHash[1] & tempHash[2]);
                var temp2 = (s0_a + maj) | 0;
                
                tempHash.unshift((temp1 + temp2) | 0);
                tempHash[4] = (tempHash[4] + temp1) | 0;
                tempHash.length = 8;
            }
            for (j = 0; j < 8; j++) tempHash[j] = (tempHash[j] + oldHash[j]) | 0;
        }
        for (i = 0; i < 8; i++) {
            var hex = (tempHash[i] >>> 0).toString(16);
            result += ('00000000' + hex).slice(-8);
        }
        return result;
    }

    function triggerConfetti() {
        try {
            const container = document.createElement('div');
            container.style.position = 'fixed';
            container.style.top = '0';
            container.style.left = '0';
            container.style.width = '100vw';
            container.style.height = '100vh';
            container.style.pointerEvents = 'none';
            container.style.zIndex = '9999';
            document.body.appendChild(container);

            const colors = ['#f44336', '#e91e63', '#9c27b0', '#673ab7', '#3f51b5', '#2196f3', '#00bcd4', '#009688', '#4caf50', '#ffeb3b', '#ff9800'];
            for (let i = 0; i < 50; i++) {
                const el = document.createElement('div');
                el.style.position = 'absolute';
                el.style.width = (Math.random() * 6 + 6) + 'px';
                el.style.height = (Math.random() * 6 + 6) + 'px';
                el.style.backgroundColor = colors[Math.floor(Math.random() * colors.length)];
                el.style.borderRadius = '50%';
                el.style.left = (Math.random() * 100) + 'vw';
                el.style.top = '-20px';
                
                const fallDuration = Math.random() * 1.5 + 1.5;
                const fallDelay = Math.random();
                el.style.transition = `transform ${fallDuration}s linear ${fallDelay}s, opacity ${fallDuration}s linear ${fallDelay}s`;
                
                container.appendChild(el);

                requestAnimationFrame(() => {
                    requestAnimationFrame(() => {
                        el.style.transform = `translate(${(Math.random() * 200 - 100)}px, 105vh) rotate(${(Math.random() * 360)}deg)`;
                        el.style.opacity = '0';
                    });
                });
            }

            setTimeout(() => {
                container.remove();
            }, 4000);
        } catch (e) {
            console.warn('Confetti effect error:', e);
        }
    }

    function playClickSound() {
        try {
            const ctx = new (window.AudioContext || window.webkitAudioContext)();
            const osc = ctx.createOscillator();
            const gain = ctx.createGain();
            
            osc.type = 'sine';
            osc.frequency.setValueAtTime(600, ctx.currentTime);
            osc.frequency.exponentialRampToValueAtTime(800, ctx.currentTime + 0.1);
            
            gain.gain.setValueAtTime(0.1, ctx.currentTime);
            gain.gain.exponentialRampToValueAtTime(0.01, ctx.currentTime + 0.15);
            
            osc.connect(gain);
            gain.connect(ctx.destination);
            
            osc.start();
            osc.stop(ctx.currentTime + 0.15);
        } catch (e) {
            console.warn('AudioContext failed:', e);
        }
    }

    function setupDraggable() {
        const overlay = document.getElementById('surveyOverlay');
        const card = overlay ? overlay.querySelector('.survey-card') : null;
        const badge = overlay ? overlay.querySelector('.survey-badge') : null;
        if (card && badge) {
            makeElementDraggable(card, badge);
        }
    }

    function makeElementDraggable(card, handle) {
        let pos1 = 0, pos2 = 0, pos3 = 0, pos4 = 0;
        handle.onmousedown = dragMouseDown;
        handle.ontouchstart = dragMouseDown;

        function dragMouseDown(e) {
            e = e || window.event;
            if (e.type === 'mousedown') {
                e.preventDefault();
            }
            pos3 = e.clientX || (e.touches && e.touches[0].clientX);
            pos4 = e.clientY || (e.touches && e.touches[0].clientY);
            document.onmouseup = closeDragElement;
            document.ontouchend = closeDragElement;
            document.onmousemove = elementDrag;
            document.ontouchmove = elementDrag;
        }

        function elementDrag(e) {
            e = e || window.event;
            const clientX = e.clientX || (e.touches && e.touches[0].clientX);
            const clientY = e.clientY || (e.touches && e.touches[0].clientY);
            if (clientX === undefined || clientY === undefined) return;
            pos1 = pos3 - clientX;
            pos2 = pos4 - clientY;
            pos3 = clientX;
            pos4 = clientY;
            
            let newTop = card.offsetTop - pos2;
            let newLeft = card.offsetLeft - pos1;

            const buffer = 50;
            const minLeft = -card.offsetWidth + buffer;
            const maxLeft = window.innerWidth - buffer;
            const minTop = 0;
            const maxTop = window.innerHeight - buffer;

            if (newLeft < minLeft) newLeft = minLeft;
            if (newLeft > maxLeft) newLeft = maxLeft;
            if (newTop < minTop) newTop = minTop;
            if (newTop > maxTop) newTop = maxTop;

            card.style.top = newTop + "px";
            card.style.left = newLeft + "px";
            card.style.transform = "none";
            card.style.margin = "0";
            card.style.position = "fixed";
        }

        function closeDragElement() {
            document.onmouseup = null;
            document.onmousemove = null;
            document.ontouchend = null;
            document.ontouchmove = null;
        }
    }

    return {
        init,
        vote,
        rateStar,
        render
    };
})();
