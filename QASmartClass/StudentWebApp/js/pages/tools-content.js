/* ═══════════════════════════════════════════════════════════
   QA SmartClass — Learning Tools Content Renderer
   Renders actual tool content inside Tool Focus Overlay
   (Parity with WPF LearningToolsHub.CreateToolControl)
   ═══════════════════════════════════════════════════════════ */

const ToolsContent = (() => {

    // ═══ MULTIPLICATION TABLE ═══
    function renderMultiplication() {
        let html = '<div class="tc-scroll"><table class="tc-table multiplication-table">';
        html += '<thead><tr><th>×</th>';
        for (let j = 1; j <= 10; j++) html += `<th>${j}</th>`;
        html += '</tr></thead><tbody>';
        for (let i = 1; i <= 10; i++) {
            html += `<tr><th>${i}</th>`;
            for (let j = 1; j <= 10; j++) {
                html += `<td class="tc-cell" data-section="row_${i}">${i * j}</td>`;
            }
            html += '</tr>';
        }
        html += '</tbody></table></div>';
        return html;
    }

    // ═══ TRIGONOMETRY TABLE ═══
    function renderTrigonometry() {
        const angles = [0, 30, 45, 60, 90, 120, 135, 150, 180, 270, 360];
        const sinVals = ['0', '1/2', '√2/2', '√3/2', '1', '√3/2', '√2/2', '1/2', '0', '-1', '0'];
        const cosVals = ['1', '√3/2', '√2/2', '1/2', '0', '-1/2', '-√2/2', '-√3/2', '-1', '0', '1'];
        const tanVals = ['0', '√3/3', '1', '√3', '∞', '-√3', '-1', '-√3/3', '0', '∞', '0'];

        let html = '<div class="tc-scroll"><table class="tc-table trig-table">';
        html += '<thead><tr><th>Góc (°)</th>';
        angles.forEach(a => html += `<th>${a}°</th>`);
        html += '</tr></thead><tbody>';
        html += '<tr class="tc-row-sin"><th>sin</th>';
        sinVals.forEach(v => html += `<td>${v}</td>`);
        html += '</tr><tr class="tc-row-cos"><th>cos</th>';
        cosVals.forEach(v => html += `<td>${v}</td>`);
        html += '</tr><tr class="tc-row-tan"><th>tan</th>';
        tanVals.forEach(v => html += `<td>${v}</td>`);
        html += '</tr></tbody></table></div>';

        html += `<div class="tc-formulas"><h4>📐 Công thức cơ bản</h4>
            <div class="tc-formula-grid">
                <div class="tc-formula-item">sin²α + cos²α = 1</div>
                <div class="tc-formula-item">tan α = sin α / cos α</div>
                <div class="tc-formula-item">sin(α±β) = sinα·cosβ ± cosα·sinβ</div>
                <div class="tc-formula-item">cos(α±β) = cosα·cosβ ∓ sinα·sinβ</div>
            </div></div>`;
        return html;
    }

    // ═══ PERIODIC TABLE (simplified — first 20 elements) ═══
    function renderPeriodicTable() {
        const elements = [
            {n:1,s:'H',name:'Hydrogen',m:'1.008',g:'1'},
            {n:2,s:'He',name:'Helium',m:'4.003',g:'18'},
            {n:3,s:'Li',name:'Lithium',m:'6.941',g:'1'},
            {n:4,s:'Be',name:'Beryllium',m:'9.012',g:'2'},
            {n:5,s:'B',name:'Boron',m:'10.81',g:'13'},
            {n:6,s:'C',name:'Carbon',m:'12.01',g:'14'},
            {n:7,s:'N',name:'Nitrogen',m:'14.01',g:'15'},
            {n:8,s:'O',name:'Oxygen',m:'16.00',g:'16'},
            {n:9,s:'F',name:'Fluorine',m:'19.00',g:'17'},
            {n:10,s:'Ne',name:'Neon',m:'20.18',g:'18'},
            {n:11,s:'Na',name:'Sodium',m:'22.99',g:'1'},
            {n:12,s:'Mg',name:'Magnesium',m:'24.31',g:'2'},
            {n:13,s:'Al',name:'Aluminium',m:'26.98',g:'13'},
            {n:14,s:'Si',name:'Silicon',m:'28.09',g:'14'},
            {n:15,s:'P',name:'Phosphorus',m:'30.97',g:'15'},
            {n:16,s:'S',name:'Sulfur',m:'32.07',g:'16'},
            {n:17,s:'Cl',name:'Chlorine',m:'35.45',g:'17'},
            {n:18,s:'Ar',name:'Argon',m:'39.95',g:'18'},
            {n:19,s:'K',name:'Potassium',m:'39.10',g:'1'},
            {n:20,s:'Ca',name:'Calcium',m:'40.08',g:'2'}
        ];
        const colors = {'1':'#E53935','2':'#FB8C00','13':'#8E24AA','14':'#3949AB','15':'#00897B','16':'#FDD835','17':'#43A047','18':'#1E88E5'};

        let html = '<div class="tc-element-grid">';
        elements.forEach(el => {
            const c = colors[el.g] || '#757575';
            html += `<div class="tc-element" data-section="element_${el.s}" style="border-color:${c}">
                <div class="tc-el-number" style="color:${c}">${el.n}</div>
                <div class="tc-el-symbol" style="color:${c}">${el.s}</div>
                <div class="tc-el-name">${el.name}</div>
                <div class="tc-el-mass">${el.m}</div>
            </div>`;
        });
        html += '</div>';
        return html;
    }

    // ═══ IDENTITIES (Hằng đẳng thức) ═══
    function renderIdentities() {
        const items = [
            { title: 'Bình phương của tổng', formula: '(a + b)² = a² + 2ab + b²' },
            { title: 'Bình phương của hiệu', formula: '(a - b)² = a² - 2ab + b²' },
            { title: 'Hiệu hai bình phương', formula: 'a² - b² = (a + b)(a - b)' },
            { title: 'Lập phương của tổng', formula: '(a + b)³ = a³ + 3a²b + 3ab² + b³' },
            { title: 'Lập phương của hiệu', formula: '(a - b)³ = a³ - 3a²b + 3ab² - b³' },
            { title: 'Tổng hai lập phương', formula: 'a³ + b³ = (a + b)(a² - ab + b²)' },
            { title: 'Hiệu hai lập phương', formula: 'a³ - b³ = (a - b)(a² + ab + b²)' },
        ];
        let html = '<div class="tc-identity-list">';
        items.forEach((it, i) => {
            html += `<div class="tc-identity-card" data-section="identity_${i+1}">
                <div class="tc-id-num">${i+1}</div>
                <div class="tc-id-body">
                    <div class="tc-id-title">${it.title}</div>
                    <div class="tc-id-formula">${it.formula}</div>
                </div>
            </div>`;
        });
        html += '</div>';
        return html;
    }

    // ═══ UNIT CONVERTER ═══
    function renderUnitConverter() {
        const groups = [
            { title: '📏 Chiều dài', items: ['1 km = 1000 m', '1 m = 100 cm', '1 cm = 10 mm', '1 inch = 2.54 cm', '1 foot = 30.48 cm', '1 mile = 1.609 km'] },
            { title: '⚖️ Khối lượng', items: ['1 tấn = 1000 kg', '1 kg = 1000 g', '1 g = 1000 mg', '1 pound = 0.4536 kg', '1 ounce = 28.35 g'] },
            { title: '🧪 Thể tích', items: ['1 m³ = 1000 lít', '1 lít = 1000 ml', '1 gallon = 3.785 lít'] },
            { title: '🌡️ Nhiệt độ', items: ['°C = (°F - 32) × 5/9', '°F = °C × 9/5 + 32', 'K = °C + 273.15'] }
        ];
        let html = '<div class="tc-converter-grid">';
        groups.forEach(g => {
            html += `<div class="tc-converter-group"><h4>${g.title}</h4><ul>`;
            g.items.forEach(it => html += `<li>${it}</li>`);
            html += '</ul></div>';
        });
        html += '</div>';
        return html;
    }

    // ═══ CALCULATOR (placeholder — interactive calculator) ═══
    function renderCalculator() {
        return `<div class="tc-calculator">
            <div class="tc-calc-display" id="tcCalcDisplay">0</div>
            <div class="tc-calc-grid">
                ${['C','±','%','÷','7','8','9','×','4','5','6','-','1','2','3','+','0','.','='].map(k => {
                    const cls = ['÷','×','-','+','='].includes(k) ? 'tc-calc-op' :
                                ['C','±','%'].includes(k) ? 'tc-calc-fn' : '';
                    const span = k === '0' ? 'style="grid-column:span 2"' : '';
                    return `<button class="tc-calc-btn ${cls}" ${span} onclick="ToolsContent.calcPress('${k}')">${k}</button>`;
                }).join('')}
            </div>
        </div>`;
    }

    let _calcVal = '0', _calcOp = '', _calcPrev = 0, _calcNew = true;
    function calcPress(k) {
        const display = document.getElementById('tcCalcDisplay');
        if (!display) return;
        if (k === 'C') { _calcVal = '0'; _calcOp = ''; _calcPrev = 0; _calcNew = true; }
        else if (k === '±') { _calcVal = String(-parseFloat(_calcVal)); }
        else if (k === '%') { _calcVal = String(parseFloat(_calcVal) / 100); }
        else if (['+','-','×','÷'].includes(k)) {
            _calcPrev = parseFloat(_calcVal); _calcOp = k; _calcNew = true; return;
        }
        else if (k === '=') {
            const cur = parseFloat(_calcVal);
            let r = cur;
            if (_calcOp === '+') r = _calcPrev + cur;
            else if (_calcOp === '-') r = _calcPrev - cur;
            else if (_calcOp === '×') r = _calcPrev * cur;
            else if (_calcOp === '÷') r = _calcPrev !== 0 ? _calcPrev / cur : 0;
            _calcVal = String(Math.round(r * 1e10) / 1e10);
            _calcOp = ''; _calcNew = true;
        }
        else if (k === '.') { if (!_calcVal.includes('.')) _calcVal += '.'; _calcNew = false; }
        else {
            if (_calcNew) { _calcVal = k; _calcNew = false; }
            else { _calcVal += k; }
        }
        display.textContent = _calcVal;
    }

    // ═══ IRREGULAR VERBS ═══
    function renderIrregularVerbs() {
        const verbs = [
            ['be','was/were','been','là, thì'],['begin','began','begun','bắt đầu'],
            ['break','broke','broken','vỡ'],['bring','brought','brought','mang'],
            ['buy','bought','bought','mua'],['come','came','come','đến'],
            ['do','did','done','làm'],['drink','drank','drunk','uống'],
            ['eat','ate','eaten','ăn'],['find','found','found','tìm'],
            ['get','got','got/gotten','được'],['give','gave','given','cho'],
            ['go','went','gone','đi'],['have','had','had','có'],
            ['know','knew','known','biết'],['make','made','made','làm, tạo'],
            ['read','read','read','đọc'],['run','ran','run','chạy'],
            ['say','said','said','nói'],['see','saw','seen','thấy'],
            ['take','took','taken','lấy'],['write','wrote','written','viết'],
        ];
        let html = '<div class="tc-scroll"><table class="tc-table verb-table">';
        html += '<thead><tr><th>V1 (Base)</th><th>V2 (Past)</th><th>V3 (P.P.)</th><th>Nghĩa</th></tr></thead><tbody>';
        verbs.forEach(v => {
            html += `<tr data-section="verb_${v[0]}"><td><strong>${v[0]}</strong></td><td>${v[1]}</td><td>${v[2]}</td><td>${v[3]}</td></tr>`;
        });
        html += '</tbody></table></div>';
        return html;
    }

    // ═══ PHYSICS CONSTANTS ═══
    function renderConstants() {
        const consts = [
            ['c', 'Tốc độ ánh sáng', '3 × 10⁸ m/s'],
            ['g', 'Gia tốc trọng trường', '9.8 m/s²'],
            ['G', 'Hằng số hấp dẫn', '6.674 × 10⁻¹¹ N·m²/kg²'],
            ['h', 'Hằng số Planck', '6.626 × 10⁻³⁴ J·s'],
            ['k', 'Hằng số Boltzmann', '1.381 × 10⁻²³ J/K'],
            ['e', 'Điện tích electron', '1.602 × 10⁻¹⁹ C'],
            ['Nₐ', 'Số Avogadro', '6.022 × 10²³ mol⁻¹'],
            ['R', 'Hằng số khí', '8.314 J/(mol·K)'],
            ['ε₀', 'Hằng số điện môi chân không', '8.854 × 10⁻¹² F/m'],
            ['μ₀', 'Độ từ thẩm chân không', '4π × 10⁻⁷ H/m'],
        ];
        let html = '<div class="tc-constants-list">';
        consts.forEach(c => {
            html += `<div class="tc-const-card" data-section="const_${c[0]}">
                <div class="tc-const-symbol">${c[0]}</div>
                <div class="tc-const-body">
                    <div class="tc-const-name">${c[1]}</div>
                    <div class="tc-const-value">${c[2]}</div>
                </div>
            </div>`;
        });
        html += '</div>';
        return html;
    }

    // ═══ BRAINSTORM (Bức tường ý tưởng) ═══
    let _selectedStudentColor = '#FFF9C4';

    function escapeHtml(str) {
        if (!str) return '';
        return str.replace(/&/g, '&amp;')
                  .replace(/</g, '&lt;')
                  .replace(/>/g, '&gt;')
                  .replace(/"/g, '&quot;')
                  .replace(/'/g, '&#039;');
    }

    function renderBrainstorm(meta) {
        const topic = escapeHtml(meta.desc || 'Hôm nay em học được gì?');
        return `
            <div class="tc-brainstorm-container" style="padding: 20px; font-family: sans-serif; text-align: left; max-width: 500px; margin: 0 auto;">
                <div style="background: #E3F2FD; padding: 16px; border-radius: 10px; margin-bottom: 20px; border-left: 5px solid #1976D2;">
                    <strong style="color: #1565C0; font-size: 14px; display: block; margin-bottom: 4px;">💬 CHỦ ĐỀ THẢO LUẬN:</strong>
                    <p id="tcBrainstormTopic" style="margin: 0; font-weight: bold; font-size: 16px; color: #333;">${topic}</p>
                </div>
                
                <div style="margin-bottom: 15px;">
                    <label style="font-weight: 600; font-size: 13px; color: #555; display: block; margin-bottom: 6px;">Ý kiến đóng góp của em:</label>
                    <textarea id="tcBrainstormInput" rows="4" placeholder="Nhập ý kiến đóng góp tại đây..." 
                        style="width: 100%; padding: 12px; border: 1px solid #CCC; border-radius: 8px; resize: none; font-size: 14px; box-sizing: border-box; font-family: inherit;"></textarea>
                </div>
                
                <div style="margin-bottom: 20px; display: flex; align-items: center; gap: 10px;">
                    <span style="font-weight: 600; font-size: 13px; color: #555;">Màu note:</span>
                    <div style="display: flex; gap: 8px;">
                        <button onclick="ToolsContent.setStudentNoteColor(this, '#FFF9C4')" style="width:28px; height:28px; border-radius:14px; border:2.5px solid #2196F3; background:#FFF9C4; cursor:pointer;" class="color-btn active"></button>
                        <button onclick="ToolsContent.setStudentNoteColor(this, '#FFCDD2')" style="width:28px; height:28px; border-radius:14px; border:1px solid #CCC; background:#FFCDD2; cursor:pointer;" class="color-btn"></button>
                        <button onclick="ToolsContent.setStudentNoteColor(this, '#C8E6C9')" style="width:28px; height:28px; border-radius:14px; border:1px solid #CCC; background:#C8E6C9; cursor:pointer;" class="color-btn"></button>
                        <button onclick="ToolsContent.setStudentNoteColor(this, '#BBDEFB')" style="width:28px; height:28px; border-radius:14px; border:1px solid #CCC; background:#BBDEFB; cursor:pointer;" class="color-btn"></button>
                        <button onclick="ToolsContent.setStudentNoteColor(this, '#E1BEE7')" style="width:28px; height:28px; border-radius:14px; border:1px solid #CCC; background:#E1BEE7; cursor:pointer;" class="color-btn"></button>
                        <button onclick="ToolsContent.setStudentNoteColor(this, '#FFE0B2')" style="width:28px; height:28px; border-radius:14px; border:1px solid #CCC; background:#FFE0B2; cursor:pointer;" class="color-btn"></button>
                    </div>
                </div>
                
                <button onclick="ToolsContent.submitStudentNote()" 
                    style="width:100%; background: #4CAF50; color: white; border: none; padding: 12px; font-size: 14px; font-weight: bold; border-radius: 8px; cursor: pointer; transition: background 0.2s;">
                    🚀 Gửi ý kiến lên bảng
                </button>
            </div>
        `;
    }

    function setStudentNoteColor(btn, color) {
        document.querySelectorAll('.color-btn').forEach(b => {
            b.style.border = '1px solid #CCC';
            b.classList.remove('active');
        });
        btn.style.border = '2.5px solid #2196F3';
        btn.classList.add('active');
        _selectedStudentColor = color;
    }

    function submitStudentNote() {
        const text = document.getElementById('tcBrainstormInput').value.trim();
        if (!text) {
            showToast('Vui lòng nhập nội dung ý kiến trước khi gửi!', 'warning');
            return;
        }

        const submitBtn = document.querySelector('button[onclick="ToolsContent.submitStudentNote()"]');
        if (submitBtn) submitBtn.disabled = true;
        
        const payload = {
            type: 'tool_submit',
            toolId: 'brainstorm',
            resultData: `${text};${_selectedStudentColor}`
        };
        
        const success = StudentConnection.send(payload);
        if (success) {
            document.getElementById('tcBrainstormInput').value = '';
            showToast('Gửi ý tưởng thành công! Đang chờ giáo viên duyệt.', 'success');
        } else {
            showToast('Không thể kết nối với giáo viên. Vui lòng kiểm tra lại kết nối mạng!', 'error');
        }

        if (submitBtn) {
            setTimeout(() => { submitBtn.disabled = false; }, 2000);
        }
    }

    // ═══ GENERIC FALLBACK ═══
    function renderGeneric(toolId, meta) {
        return `<div style="text-align:center;padding:40px 20px">
            <div style="font-size:64px;margin-bottom:16px">${meta.icon}</div>
            <h3 style="margin-bottom:8px;color:var(--text-primary)">${meta.name}</h3>
            <p style="color:var(--text-secondary);font-size:14px">${meta.desc}</p>
            <div style="margin-top:24px;padding:16px;background:var(--primary-50);border-radius:12px;
                        border:2px solid var(--primary-200)">
                <p style="font-weight:600;color:var(--primary-dark)">🎯 GV đang yêu cầu tập trung</p>
                <p style="font-size:13px;color:var(--text-secondary);margin-top:4px">
                    Hãy chú ý theo dõi nội dung <strong>${meta.name}</strong> trên bảng giáo viên
                </p>
            </div>
        </div>`;
    }

    // ═══ MAIN RENDER FUNCTION ═══
    function render(toolId, meta) {
        switch (toolId) {
            case 'multiplication':    return renderMultiplication();
            case 'trigonometry':      return renderTrigonometry();
            case 'periodic_table':    return renderPeriodicTable();
            case 'identities':        return renderIdentities();
            case 'unit_converter':    return renderUnitConverter();
            case 'calculator':        return renderCalculator();
            case 'irregular_verbs':   return renderIrregularVerbs();
            case 'constants':         return renderConstants();
            case 'brainstorm':        return renderBrainstorm(meta);
            default:                  return renderGeneric(toolId, meta);
        }
    }

    // ═══ SECTION HIGHLIGHT ═══
    function highlightSection(sectionId) {
        // Remove previous highlights and reset displays
        document.querySelectorAll('.tc-section-highlighted').forEach(el => {
            el.classList.remove('tc-section-highlighted');
        });
        document.querySelectorAll('[data-section]').forEach(el => {
            el.style.display = '';
        });

        if (!sectionId) return;

        // Find element with matching data-section
        const target = document.querySelector(`[data-section="${sectionId}"]`);
        if (target) {
            target.classList.add('tc-section-highlighted');
            target.scrollIntoView({ behavior: 'smooth', block: 'center' });
            
            // Hide sibling cards in the same tool container to focus student attention
            const parent = target.parentElement;
            if (parent) {
                Array.from(parent.children).forEach(child => {
                    const secId = child.getAttribute('data-section');
                    if (secId && secId !== sectionId) {
                        child.style.display = 'none';
                    }
                });
            }
        }
    }

    return { render, highlightSection, calcPress, setStudentNoteColor, submitStudentNote };
})();
