// ═══════════════════════════════════════════════════════════
//  DATA FOR REAL-WORLD APPLICATIONS (PRACTICAL APP ITEMS)
// ═══════════════════════════════════════════════════════════
const practicalAppItems = [
    {
        icon: "🌉",
        title: "Thiết kế Kỹ thuật & Kiến trúc",
        formula: "$$\\vec{F}_{net} = \\sum \\vec{F}_i = 0$$",
        formulaDesc: "Vận dụng lượng giác, hình học tọa độ, vectơ lực và phép tính tích phân để thiết kế kết cấu cầu dây văng.",
        desc: "Ứng dụng hình học phẳng và giải tích nâng cao trong phân tích tải trọng của các kiến trúc hạ tầng giao thông. Kỹ sư xây dựng sử dụng phương trình cơ học để tính toán ứng suất và độ bền vật liệu.",
        analysis: "Mô hình toán học mô phỏng cầu dây văng thông qua hệ tọa độ Đề-các và phương trình Parabol cho dây cáp chính. Sử dụng phép tính Vectơ để biểu diễn lực kéo của từng sợi dây cáp lên trụ cầu chính, đảm bảo tổng lực triệt tiêu đạt trạng thái cân bằng cơ học tĩnh.",
        discussion: "1. Tại sao hình parabol lại được chọn để thiết kế dây võng của cầu thay vì một đường thẳng dốc?\n2. Hãy chỉ ra sự khác biệt về lực truyền tải giữa cầu vòm đá truyền thống và cầu treo dây văng hiện đại.",
        task: "Sử dụng tọa độ 2D vẽ đường cong dây cáp cầu dạng Parabol $y = ax^2 + bx + c$. Điều chỉnh tham số $a$ để quan sát sự thay đổi độ võng và tính toán lực kéo phân bổ đều tại các điểm treo liên kết."
    },
    {
        icon: "🧪",
        title: "Toán học trong Hóa học & Phòng thí nghiệm",
        formula: "$$\\text{pH} = -\\log_{10}[\\text{H}^+]$$",
        formulaDesc: "Sử dụng hàm số logarit cơ số 10 để đo lường độ axit/bazơ của dung dịch sinh hóa học.",
        desc: "Ứng dụng phương trình vi phân và các hàm số logarit trong tính toán nồng độ phản ứng, xác định thời gian bán rã của chất phóng xạ và đo lường nồng độ pH của dung dịch hóa chất thực tế.",
        analysis: "Độ pH là một thang đo logarit nghịch đảo đại diện cho hoạt độ của các ion hydro. Vì giá trị $[\\text{H}^+]$ dao động ở phạm vi cực rộng ($10^{-1}$ đến $10^{-14}$ mol/L), việc sử dụng hàm logarit giúp chuyển đổi dữ liệu thành một thang đo tuyến tính 0-14 trực quan, dễ quản lý hơn trong thực tế.",
        discussion: "1. Nếu nồng độ ion $[\\text{H}^+]$ tăng lên gấp 100 lần, thì độ pH của dung dịch sẽ thay đổi như thế nào?\n2. Tại sao nước cất tinh khiết có độ pH trung tính bằng 7 ở nhiệt độ $25^\\circ\\text{C}$? Viết phương trình tự phân ly của nước.",
        task: "Hãy thiết lập một phương trình toán học biểu diễn nồng độ của một chất phản ứng hóa học giảm dần theo thời gian dựa trên phương trình tốc độ phản ứng bậc nhất: $C(t) = C_0 e^{-kt}$. Tính toán thời gian bán rã $t_{1/2}$."
    },
    {
        icon: "⚛️",
        title: "Vật lý Toán & Cơ học Lượng tử",
        formula: "$$\\hat{H}\\psi = E\\psi$$",
        formulaDesc: "Phương trình Schrödinger độc lập thời gian - phương trình cốt lõi của vật lý cơ học lượng tử.",
        desc: "Sử dụng đại số tuyến tính vô hạn chiều, toán tử Hermite và phương trình vi phân đạo hàm riêng để mô hình hóa trạng thái năng lượng của các hạt cơ bản cấp độ nguyên tử.",
        analysis: "Toán tử Hamiltonian $\\hat{H}$ đại diện cho tổng năng lượng của hệ (động năng + thế năng). Phương trình lượng tử thực chất là một bài toán tìm Trị riêng (Eigenvalue) $E$ và Hàm riêng (Eigenvector/Wavefunction) $\\psi$. Hàm riêng $\\psi$ bình phương biểu diễn mật độ xác suất tìm thấy hạt tại một điểm.",
        discussion: "1. Tại sao các mức năng lượng của electron trong nguyên tử lại có tính chất rời rạc (lượng tử hóa) chứ không liên tục?\n2. Giải thích ý nghĩa vật lý của nguyên lý bất định Heisenberg thông qua toán học toán tử không giao hoán: $[\\hat{x}, \\hat{p}] = i\\hbar$.",
        task: "Giải phương trình trị riêng cho bài toán 'Hạt trong giếng thế một chiều' có độ rộng $L$. Hãy xác định công thức các mức năng lượng $E_n$ và vẽ hình dáng của 3 hàm sóng trạng thái đầu tiên $\\psi_1, \\psi_2, \\psi_3$."
    },
    {
        icon: "🎮",
        title: "Đồ họa Máy tính & Mô phỏng Vật lý",
        formula: "$$\\vec{p} = m\\vec{v}, \\quad \\vec{F} = m\\vec{a}$$",
        formulaDesc: "Các phương trình động lực học Newton dùng để cập nhật tọa độ và vận tốc của vật thể.",
        desc: "Ứng dụng các công thức động học, vectơ lực và ma trận biến đổi không gian để mô phỏng chuyển động thực tế, hiệu ứng va chạm vật lý và phản xạ ánh sáng trong đồ họa 3D và game engine.",
        analysis: "Game engine liên tục chạy một vòng lặp (Game Loop) với tần số 60Hz+. Tại mỗi khung hình, vị trí vật thể được tích phân xấp xỉ bằng phương pháp Euler: $x(t+\\Delta t) = x(t) + v(t)\\Delta t$. Va chạm giữa các vật thể được tính toán qua thuật toán kiểm tra giao cắt hình học và bảo toàn động lượng để sinh lực phản hồi.",
        discussion: "1. Tại sao ma trận Quaternion lại được sử dụng rộng rãi hơn ma trận xoay Euler 3D truyền thống trong việc biểu diễn chuyển động quay của camera? (Gợi ý: Tìm hiểu lỗi Gimbal Lock).\n2. Làm thế nào để mô phỏng một sợi dây mềm chuyển động chân thực trong game? (Gợi ý: Hệ lò xo đàn hồi lò xo - mass spring system).",
        task: "Viết đoạn giả mã thuật toán phát hiện va chạm giữa hai vòng tròn trong mặt phẳng tọa độ Oxy dựa trên khoảng cách giữa hai tâm và tổng các bán kính của chúng."
    },
    {
        icon: "💻",
        title: "Khoa học Máy tính & Logic Boolean",
        formula: "$$A \\land (B \\lor C) = (A \\land B) \\lor (A \\land C)$$",
        formulaDesc: "Định luật phân phối của đại số Boolean, nền tảng tối ưu hóa cổng logic phần cứng.",
        desc: "Ứng dụng đại số Boolean, lý thuyết đồ thị và số học nhị phân để thiết kế các bộ vi xử lý máy tính, xây dựng thuật toán tìm đường tối ưu và truy vấn dữ liệu quan hệ.",
        analysis: "Đại số logic chỉ hoạt động trên hai giá trị chân trị {0, 1} tương ứng với mức điện áp vật lý thấp và cao trong transistor. Mọi thuật toán tìm kiếm (như Dijkstra trong bản đồ số) hoặc thiết kế vi mạch tích hợp đều bắt nguồn từ các biểu thức logic và lý thuyết đồ thị biểu diễn liên kết mạng.",
        discussion: "1. Trình bày cách xây dựng bộ cộng nhị phân 1-bit (Half Adder) chỉ sử dụng các cổng logic cơ bản (AND, OR, NOT, XOR).\n2. Tại sao lý thuyết đồ thị (Graph Theory) lại cực kỳ quan trọng trong việc thiết kế định tuyến dữ liệu trên mạng Internet?",
        task: "Sử dụng bìa Karnaugh (K-Map) hoặc định luật đại số Boolean để rút gọn biểu thức logic sau: $F = A\\bar{B}C + AB\\bar{C} + ABC + \\bar{A}BC$."
    },
    {
        icon: "🤖",
        title: "Học sâu & Thiết kế Mạng Nơ-ron",
        formula: "$$\\frac{\partial L}{\partial w} = \\frac{\partial L}{\partial a} \\cdot \\frac{\partial a}{\partial z} \\cdot \\frac{\partial z}{\partial w}$$",
        formulaDesc: "Quy tắc xích trong vi phân được ứng dụng để cập nhật trọng số liên kết mạng nơ-ron.",
        desc: "Khám phá toán học đằng sau trí tuệ nhân tạo thông qua việc vi phân lan truyền ngược, tính đạo hàm hàm mất mát và tối ưu hóa trọng số dựa trên Gradient Descent.",
        analysis: "Mạng nơ-ron thực chất là một chuỗi các phép nhân ma trận trọng số xen kẽ bởi các hàm kích hoạt phi tuyến tính. Để tối ưu hóa hàng triệu trọng số $w$ sao cho dự đoán chính xác nhất, thuật toán Lan truyền ngược (Backpropagation) sử dụng quy tắc chuỗi (Chain Rule) để tính đạo hàm riêng của hàm mất mát đối với từng trọng số từ lớp đầu ra ngược về lớp đầu vào.",
        discussion: "1. Tại sao nếu không có hàm kích hoạt phi tuyến tính, mạng nơ-ron sâu nhiều lớp cũng chỉ tương đương với một mô hình hồi quy tuyến tính đơn giản? Hãy chứng minh bằng toán học ma trận.\n2. Hiện tượng tiêu biến gradient (vanishing gradient) là gì? Nó liên quan thế nào đến đạo hàm của hàm Sigmoid khi giá trị đầu vào rất lớn hoặc rất nhỏ?",
        task: "Sử dụng công cụ mô phỏng để thay đổi tốc độ học (learning rate $\\eta$). Quan sát sự hội tụ của hàm mất mát. Hãy thử đặt $\\eta$ quá lớn (ví dụ: 10) và quá nhỏ (ví dụ: 0.0001) và giải thích hiện tượng xảy ra đối với đồ thị hàm mất mát."
    }
];

// ═══════════════════════════════════════════════════════════
//  RENDER NEURAL NETWORK DYNAMICALLY (SVG)
// ═══════════════════════════════════════════════════════════
function drawNeuralNetwork() {
    const svg = document.querySelector(".nn-svg");
    const connectionsGroup = document.getElementById("connections-group");
    const nodesGroup = document.getElementById("nodes-group");
    
    if (!svg || !connectionsGroup || !nodesGroup) return;

    // Clear previous elements
    connectionsGroup.innerHTML = "";
    nodesGroup.innerHTML = "";

    // Layers configuration
    const layers = [
        { name: "input", count: 4, x: 90, color: "url(#grad-node-input)", nodePrefix: "x" },
        { name: "hidden1", count: 5, x: 210, color: "url(#grad-node-hidden1)", nodePrefix: "h1_" },
        { name: "hidden2", count: 5, x: 370, color: "url(#grad-node-hidden2)", nodePrefix: "h2_" },
        { name: "output", count: 3, x: 510, color: "url(#grad-node-output)", nodePrefix: "y" }
    ];

    const height = 450;
    const nodeRadius = 14;

    // Calculate node coordinates
    const nodeCoords = {};
    layers.forEach(layer => {
        const layerHeight = (layer.count - 1) * 55;
        const startY = (height - layerHeight) / 2 + 10; // offset slightly downwards
        nodeCoords[layer.name] = [];
        
        for (let i = 0; i < layer.count; i++) {
            nodeCoords[layer.name].push({
                x: layer.x,
                y: startY + i * 55,
                id: `${layer.nodePrefix}${i}`
            });
        }
    });

    // Draw Connections (Lines)
    const activePaths = [
        { from: "x1", to: "h1_2" },
        { from: "x2", to: "h1_2" },
        { from: "h1_2", to: "h2_1" },
        { from: "h1_2", to: "h2_3" },
        { from: "h2_1", to: "y1" },
        { from: "h2_3", to: "y1" }
    ];

    for (let l = 0; l < layers.length - 1; l++) {
        const currentLayerName = layers[l].name;
        const nextLayerName = layers[l + 1].name;
        
        const currentNodes = nodeCoords[currentLayerName];
        const nextNodes = nodeCoords[nextLayerName];

        currentNodes.forEach(currNode => {
            nextNodes.forEach(nextNode => {
                const line = document.createElementNS("http://www.w3.org/2000/svg", "line");
                line.setAttribute("x1", currNode.x);
                line.setAttribute("y1", currNode.y);
                line.setAttribute("x2", nextNode.x);
                line.setAttribute("y2", nextNode.y);

                // Check if connection is in active paths
                const isActive = activePaths.some(path => path.from === currNode.id && path.to === nextNode.id);
                if (isActive) {
                    line.setAttribute("stroke", "url(#grad-active)");
                    line.setAttribute("stroke-width", "3.5");
                    line.classList.add("nn-line-active");
                } else {
                    line.setAttribute("stroke", "#e2e8f0");
                    line.setAttribute("stroke-width", "1");
                    line.setAttribute("opacity", "0.6");
                }

                connectionsGroup.appendChild(line);
            });
        });
    }

    // Draw Nodes (Circles)
    layers.forEach(layer => {
        const nodes = nodeCoords[layer.name];
        nodes.forEach((node, index) => {
            // Node group for hover effects
            const g = document.createElementNS("http://www.w3.org/2000/svg", "g");
            g.style.cursor = "pointer";

            // Circle shadow/glow
            const circleGlow = document.createElementNS("http://www.w3.org/2000/svg", "circle");
            circleGlow.setAttribute("cx", node.x);
            circleGlow.setAttribute("cy", node.y);
            circleGlow.setAttribute("r", nodeRadius + 3);
            circleGlow.setAttribute("fill", "white");
            circleGlow.setAttribute("opacity", "0.5");
            g.appendChild(circleGlow);

            // Circle node
            const circle = document.createElementNS("http://www.w3.org/2000/svg", "circle");
            circle.setAttribute("cx", node.x);
            circle.setAttribute("cy", node.y);
            circle.setAttribute("r", nodeRadius);
            circle.setAttribute("fill", layer.color);
            circle.setAttribute("stroke", "#ffffff");
            circle.setAttribute("stroke-width", "2");
            
            // Check if node is active
            const isActiveNode = ["x1", "x2", "h1_2", "h2_1", "h2_3", "y1"].includes(node.id);
            if (isActiveNode) {
                circle.classList.add("node-active");
                circle.setAttribute("stroke", "#ffffff");
                circle.setAttribute("stroke-width", "2.5");
            }
            g.appendChild(circle);

            // Label text inside node (e.g. w_ij, or x_i)
            const text = document.createElementNS("http://www.w3.org/2000/svg", "text");
            text.setAttribute("x", node.x);
            text.setAttribute("y", node.y + 4);
            text.setAttribute("fill", "white");
            text.setAttribute("font-size", "10px");
            text.setAttribute("font-weight", "bold");
            text.setAttribute("text-anchor", "middle");
            text.setAttribute("font-family", "Inter, sans-serif");
            
            // Generate node symbol text
            let symbolText = "";
            if (layer.name === "input") symbolText = `x${index + 1}`;
            else if (layer.name === "hidden1") symbolText = `a${index + 1}`;
            else if (layer.name === "hidden2") symbolText = `z${index + 1}`;
            else if (layer.name === "output") symbolText = `y${index + 1}`;
            text.textContent = symbolText;
            g.appendChild(text);

            // Add simple hover scale animation
            g.addEventListener("mouseenter", () => {
                circle.setAttribute("r", nodeRadius + 3);
                circleGlow.setAttribute("r", nodeRadius + 7);
                circleGlow.setAttribute("opacity", "0.8");
            });

            g.addEventListener("mouseleave", () => {
                circle.setAttribute("r", nodeRadius);
                circleGlow.setAttribute("r", nodeRadius + 3);
                circleGlow.setAttribute("opacity", "0.5");
            });

            nodesGroup.appendChild(g);
        });
    });
}

// ═══════════════════════════════════════════════════════════
//  TAB NAVIGATION INTERACTION
// ═══════════════════════════════════════════════════════════
function selectSubTab(index) {
    // 1. Update active states on sidebar items
    const subItems = document.querySelectorAll(".sub-item");
    subItems.forEach((item, i) => {
        if (i === index) {
            item.classList.add("active");
        } else {
            item.classList.remove("active");
        }
    });

    // 2. Fetch selected data
    const data = practicalAppItems[index];
    if (!data) return;

    // 3. Update main details
    document.getElementById("detail-title").textContent = data.title;
    document.getElementById("detail-desc").textContent = data.desc;
    document.getElementById("math-formula").textContent = data.formula;
    document.getElementById("formula-desc").textContent = data.formulaDesc;

    // Update Accordions with animation/fade
    const analysisText = document.getElementById("analysis-text");
    const discussionText = document.getElementById("discussion-text");
    const taskText = document.getElementById("task-text");

    // Helper to format line breaks & math
    analysisText.innerHTML = formatText(data.analysis);
    discussionText.innerHTML = formatText(data.discussion);
    taskText.innerHTML = formatText(data.task);

    // Trigger MathJax typeset to render LaTeX
    if (window.MathJax && window.MathJax.typesetPromise) {
        window.MathJax.typesetPromise();
    }

    // 4. Update Visual representation
    const visualCard = document.querySelector(".image-card");
    if (index === 5) {
        // Show active Neural Network SVG for Deep Learning
        visualCard.innerHTML = `<div class="nn-container">
            <svg class="nn-svg" viewBox="0 0 600 450" width="100%" height="100%"></svg>
        </div>`;
        drawNeuralNetwork();
    } else {
        // For other tabs, display dynamic fallback graphic
        const categoryGradients = [
            "linear-gradient(135deg, #1e3c72 0%, #2a5298 100%)", // Engineering
            "linear-gradient(135deg, #11998e 0%, #38ef7d 100%)", // Chemistry
            "linear-gradient(135deg, #833ab4 0%, #fd1d1d 50%, #fcb045 100%)", // Physics
            "linear-gradient(135deg, #f12711 0%, #f5af19 100%)", // Graphics
            "linear-gradient(135deg, #cb2d3e 0%, #ef473a 100%)", // CS
        ];
        const gradient = categoryGradients[index] || categoryGradients[0];
        
        visualCard.innerHTML = `
            <div style="
                width: 100%;
                height: 100%;
                background: ${gradient};
                border-radius: var(--radius-card);
                display: flex;
                flex-direction: column;
                justify-content: center;
                align-items: center;
                color: white;
                text-align: center;
                padding: 24px;
                position: relative;
                overflow: hidden;
            ">
                <!-- Abstract Vector Pattern (SVG) -->
                <svg style="position: absolute; width: 100%; height: 100%; top: 0; left: 0; opacity: 0.08;" viewBox="0 0 200 200">
                    <path fill="none" stroke="white" stroke-width="1" d="M 0,20 L 200,20 M 0,40 L 200,40 M 0,60 L 200,60 M 0,80 L 200,80 M 0,100 L 200,100 M 0,120 L 200,120 M 0,140 L 200,140 M 0,160 L 200,160 M 0,180 L 200,180"/>
                    <path fill="none" stroke="white" stroke-width="1" d="M 20,0 L 20,200 M 40,0 L 40,200 M 60,0 L 60,200 M 80,0 L 80,200 M 100,0 L 100,200 M 120,0 L 120,200 M 140,0 L 140,200 M 160,0 L 160,200 M 180,0 L 180,200"/>
                </svg>
                <div style="font-size: 80px; margin-bottom: 16px; filter: drop-shadow(0 4px 6px rgba(0,0,0,0.25));">${data.icon}</div>
                <h3 style="font-family: var(--font-heading); font-size: 24px; font-weight: 800; filter: drop-shadow(0 2px 4px rgba(0,0,0,0.15));">${data.title}</h3>
                <p style="font-size: 13px; max-width: 80%; margin-top: 8px; opacity: 0.85; line-height: 1.4;">${data.formulaDesc}</p>
            </div>
        `;
    }
}

// Format line breaks and custom html elements
function formatText(text) {
    return text.replace(/\n/g, "<br>");
}

// ═══════════════════════════════════════════════════════════
//  APP INITIALIZATION
// ═══════════════════════════════════════════════════════════
document.addEventListener("DOMContentLoaded", () => {
    // Select the last sub-tab by default ("Học sâu & Thiết kế Mạng Nơ-ron")
    selectSubTab(5);
});
