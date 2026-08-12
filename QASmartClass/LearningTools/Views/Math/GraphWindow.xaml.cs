using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QASmartClass.LearningTools.Views.Math
{
    public partial class GraphWindow : Window
    {
        private bool _isFullscreen;
        private WindowStyle _prevStyle;
        private WindowState _prevState;
        private bool _isCapturing;
        private string? _expressions2D;
        private string? _expressions3D;
        private bool _is3DMode;

        /// <summary>
        /// Kế thừa Topmost từ Owner và đảm bảo Focus/Activate khi GraphWindow hiển thị.
        /// Giải quyết lỗi hệ thống: GraphWindow bị chìm dưới MainDashboard khi MainDashboard
        /// ở chế độ Fullscreen (Topmost = true). Áp dụng cho tất cả 28 call site / 23 Math tool.
        /// </summary>
        private void GraphWindow_EnsureTopmostAndFocus(object sender, RoutedEventArgs e)
        {
            // Kế thừa Topmost từ Owner (nếu Owner đang Topmost)
            if (this.Owner != null && this.Owner.Topmost)
            {
                this.Topmost = true;
            }
            // Đảm bảo cửa sổ nhận Focus trên bảng tương tác SMART TOUCH
            this.Activate();
            this.Focus();
        }

        /// <summary>
        /// Constructor cho PT bậc 2: y = ax² + bx + c
        /// </summary>
        private static string FormatUiNumber(double value, string format = "G6")
        {
            string s = value.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
            s = s.Replace(".", ",");
            if (s.StartsWith("-"))
            {
                s = "−" + s.Substring(1);
            }
            return s;
        }

        public GraphWindow(double a, double b, double c)
        {
            if (a == 0)
            {
                throw new ArgumentException("Hệ số a của phương trình bậc 2 phải khác 0.");
            }
            InitializeComponent();
            this.Loaded += GraphWindow_EnsureTopmostAndFocus;
            btnMode2D.Visibility = Visibility.Collapsed;
            btnMode3D.Visibility = Visibility.Collapsed;

            double xV = -b / (2 * a);
            double yV = a * xV * xV + b * xV + c;
            double delta = b * b - 4 * a * c;
            var ci = System.Globalization.CultureInfo.InvariantCulture;

            // Chuỗi dùng cho JavaScript của Desmos (bắt buộc dùng dấu chấm thập phân)
            string aS_js = a.ToString(ci), bS_js = b.ToString(ci), cS_js = c.ToString(ci);
            string xVS_js = xV.ToString("G6", ci), yVS_js = yV.ToString("G6", ci);

            // Định dạng số hiển thị cho UI tiếng Việt
            string aS_ui = FormatUiNumber(a);
            string bS_ui = FormatUiNumber(b);
            string cS_ui = FormatUiNumber(c);
            string deltaS_ui = FormatUiNumber(delta);
            string xVS_ui = FormatUiNumber(xV);
            string yVS_ui = FormatUiNumber(yV);

            txtTitle.Text = $"📈 Đồ thị y = {aS_ui}x² + ({bS_ui})x + ({cS_ui})";
            txtInfo.Text = $"Δ = {deltaS_ui}  |  Đỉnh I({xVS_ui}; {yVS_ui})  |  " +
                           (delta > 0 ? "2 nghiệm" : delta == 0 ? "Nghiệm kép" : "Vô nghiệm thực");

            string rootsJs = "";
            if (delta > 0)
            {
                double x1 = (-b + System.Math.Sqrt(delta)) / (2 * a);
                double x2 = (-b - System.Math.Sqrt(delta)) / (2 * a);
                rootsJs = $@"
        calc.setExpression({{id:'r1', latex:'({x1.ToString("G8", ci)},0)', color:'#E53935', pointStyle:'POINT', pointSize:12, label:'x₁={FormatUiNumber(x1, "G4")}', showLabel:true}});
        calc.setExpression({{id:'r2', latex:'({x2.ToString("G8", ci)},0)', color:'#E53935', pointStyle:'POINT', pointSize:12, label:'x₂={FormatUiNumber(x2, "G4")}', showLabel:true}});";
            }
            else if (delta == 0)
            {
                rootsJs = $@"
        calc.setExpression({{id:'r1', latex:'({xVS_js},0)', color:'#FF6F00', pointStyle:'POINT', pointSize:12, label:'x₀={FormatUiNumber(xV)}', showLabel:true}});";
            }

            string html = BuildHtml($@"
        calc.setExpression({{id:'parabola', latex:'y={aS_js}x^2+({bS_js})x+({cS_js})', color:'#1B5E20', lineWidth:3.5}});
        calc.setExpression({{id:'vertex', latex:'({xVS_js},{yVS_js})', color:'#1565C0', pointStyle:'POINT', pointSize:14, label:'Đỉnh I', showLabel:true}});
        calc.setExpression({{id:'axis', latex:'x={xVS_js}', color:'#9E9E9E', lineStyle:'DASHED', lineWidth:1.5}});
        {rootsJs}
        calc.setExpression({{id:'yint', latex:'(0,{cS_js})', color:'#FF6F00', pointStyle:'POINT', pointSize:10, label:'(0;{FormatUiNumber(c)})', showLabel:true}});
        var range = Math.max(Math.abs(xV) + 5, Math.abs(yV) + 5, 10);
        calc.setMathBounds({{ left: {xVS_js} - range, right: {xVS_js} + range, bottom: {yVS_js} - range, top: {yVS_js} + range }});");

            LoadHtml(html);
        }

        /// <summary>
        /// Constructor cho Hệ PT bậc nhất: 2 đường thẳng + giao điểm
        /// </summary>
        public GraphWindow(string eq1Latex, string eq2Latex, string pointJs, string title)
        {
            InitializeComponent();
            this.Loaded += GraphWindow_EnsureTopmostAndFocus;
            btnMode2D.Visibility = Visibility.Collapsed;
            btnMode3D.Visibility = Visibility.Collapsed;
            txtTitle.Text = title;
            txtInfo.Text = "Đồ thị 2 đường thẳng — giao điểm là nghiệm của hệ";

            string html = BuildHtml($@"
        calc.setExpression({{id:'line1', latex:'{eq1Latex}', color:'#1565C0', lineWidth:3}});
        calc.setExpression({{id:'line2', latex:'{eq2Latex}', color:'#2E7D32', lineWidth:3}});
        {pointJs}
        calc.setMathBounds({{ left: -15, right: 15, bottom: -10, top: 10 }});");

            LoadHtml(html);
        }

        /// <summary>
        /// Constructor tổng quát — truyền JS expressions trực tiếp
        /// </summary>
        public GraphWindow(string expressionsJs, string title)
        {
            InitializeComponent();
            this.Loaded += GraphWindow_EnsureTopmostAndFocus;
            btnMode2D.Visibility = Visibility.Collapsed;
            btnMode3D.Visibility = Visibility.Collapsed;
            txtTitle.Text = title;
            txtInfo.Text = "Đồ thị — QA SmartClass";
            LoadHtml(BuildHtml(expressionsJs));
        }

        /// <summary>
        /// Constructor tổng quát — truyền JS expressions trực tiếp kèm thông tin chi tiết
        /// </summary>
        public GraphWindow(string expressionsJs, string title, string info)
        {
            InitializeComponent();
            this.Loaded += GraphWindow_EnsureTopmostAndFocus;
            btnMode2D.Visibility = Visibility.Collapsed;
            btnMode3D.Visibility = Visibility.Collapsed;
            txtTitle.Text = title;
            txtInfo.Text = info;
            LoadHtml(BuildHtml(expressionsJs));
        }

        /// <summary>
        /// Constructor hỗ trợ chuyển đổi Đồ thị 2D và 3D
        /// </summary>
        public GraphWindow(string expressions2D, string expressions3D, string title, string info, bool enableDualToggle)
        {
            InitializeComponent();
            this.Loaded += GraphWindow_EnsureTopmostAndFocus;
            _expressions2D = expressions2D;
            _expressions3D = expressions3D;
            txtTitle.Text = title;
            txtInfo.Text = info;

            // Show toggle buttons only if dual toggle is requested
            btnMode2D.Visibility = enableDualToggle ? Visibility.Visible : Visibility.Collapsed;
            btnMode3D.Visibility = enableDualToggle ? Visibility.Visible : Visibility.Collapsed;

            // Load 2D view by default
            _is3DMode = false;
            LoadHtml(BuildHtml(_expressions2D));
        }

        /// <summary>
        /// Cập nhật động các biểu thức Desmos và thông tin tiêu đề mà không cần tải lại trang
        /// </summary>
        public async void UpdateExpressions(string expressionsJs, string? title = null, string? info = null)
        {
            try
            {
                if (title != null && txtTitle != null)
                {
                    txtTitle.Text = title;
                    if (webView?.CoreWebView2 != null)
                    {
                        string escapedTitle = title.Replace("'", "\\'");
                        await webView.CoreWebView2.ExecuteScriptAsync($"graphTitle = '{escapedTitle}';");
                    }
                }
                if (info != null && txtInfo != null)
                {
                    txtInfo.Text = info;
                }
                if (webView?.CoreWebView2 != null)
                {
                    string safeJs = SanitizeExpressions(expressionsJs);
                    await webView.CoreWebView2.ExecuteScriptAsync(safeJs);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Update expressions error: {ex.Message}");
            }
        }

        private static string SanitizeExpressions(string js)
        {
            if (string.IsNullOrEmpty(js)) return "";
            string sanitized = js;
            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"<script[^>]*?>.*?</script>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"<[^>]*>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase); // HTML tags
            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"javascript:", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"\beval\b", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"\bdocument\b", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"\bwindow\b", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"\bcookie\b", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"\balert\b", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"\bprompt\b", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"\bconfirm\b", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"\bfetch\b", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"\bXMLHttpRequest\b", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return sanitized;
        }

        private static string BuildHtml(string expressionsJs)
        {
            string safeJs = SanitizeExpressions(expressionsJs);
            string htmlTemplate = @"<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        * { margin:0; padding:0; box-sizing:border-box; }
        body { background:#F5F7FA; overflow:hidden; font-family:'Segoe UI', sans-serif; }
        #calculator2D { width:100vw; height:100vh; display:block; }
        #calculator3D { width:100vw; height:100vh; display:none; }
        #offline-container {
            display: none;
            width: 100vw;
            height: 100vh;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            background: #F5F7FA;
            padding: 20px;
        }
        #offline-3d-message {
            display: none;
            width: 100vw;
            height: 100vh;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            background: #F5F7FA;
            padding: 20px;
            font-size: 16px;
            color: #C62828;
            text-align: center;
        }
        .offline-header {
            text-align: center;
            margin-bottom: 15px;
            color: #2E7D32;
        }
        .offline-header h3 {
            font-size: 20px;
            margin-bottom: 5px;
        }
        .offline-header p {
            font-size: 13px;
            color: #666;
        }
        #svg-canvas {
            background: #ffffff;
            border: 2px solid #E0E0E0;
            border-radius: 12px;
            box-shadow: 0 4px 12px rgba(0,0,0,0.05);
            max-width: 90%;
            max-height: 70vh;
        }
    </style>
    <script>
        var graphTitle = '[[WINDOW_TITLE]]';
        var isOffline = false;
        function showOffline() {
            if (isOffline) return;
            isOffline = true;
            var runShow = function() {
                var calc2D = document.getElementById('calculator2D');
                var calc3D = document.getElementById('calculator3D');
                if (calc2D) calc2D.style.display = 'none';
                if (calc3D) calc3D.style.display = 'none';
                var offElt = document.getElementById('offline-container');
                if (offElt) offElt.style.display = 'flex';
                if (typeof renderOfflineSvg === 'function') {
                    renderOfflineSvg(capturedExpressions, mathBounds);
                }
            };
            if (document.readyState === 'loading') {
                document.addEventListener('DOMContentLoaded', runShow);
            } else {
                runShow();
            }
        }
        function formatOfflineNumber(val) {
            if (typeof val !== 'number' || isNaN(val)) return '';
            var s = Number(val.toFixed(2)).toString();
            s = s.replace('.', ',');
            if (s.indexOf('-') === 0) {
                s = '−' + s.substring(1);
            }
            return s;
        }
        setTimeout(function() {
            if (typeof Desmos === 'undefined') {
                showOffline();
            }
        }, 1500);
    </script>
    <script src='https://www.desmos.com/api/v1.9/calculator.js?apiKey=dcb31709b452b1cf9dc26972add0fda6' onerror='showOffline()'></script>
</head>
<body>
    <div id='calculator2D'></div>
    <div id='calculator3D'></div>
    <div id='offline-container'>
        <div class='offline-header'>
            <h3>⚠️ Đang chạy ở chế độ ngoại tuyến (Offline)</h3>
            <p>Không thể kết nối tới Desmos API. Hệ thống đã tự động vẽ đồ thị hàm số trực quan dưới đây.</p>
        </div>
        <svg id='svg-canvas' viewBox='0 0 800 600'></svg>
    </div>
    <div id='offline-3d-message'>
        <h3>⚠️ Đồ thị 3D yêu cầu kết nối mạng (Internet)</h3>
        <p style='margin-top: 10px; color: #555;'>Vui lòng kiểm tra kết nối mạng của bạn để tải tính năng 3D, hoặc chuyển sang chế độ 2D để xem ngoại tuyến.</p>
    </div>

    <script>
        var capturedExpressions = [];
        var mathBounds = null;
        var calc = {
            setExpression: function(opt) {
                capturedExpressions.push(opt);
            },
            setMathBounds: function(bounds) {
                if (bounds) {
                    var containerRatio = 800 / 600;
                    var mathWidth = bounds.right - bounds.left;
                    var mathHeight = bounds.top - bounds.bottom;
                    var mathRatio = mathWidth / mathHeight;
                    if (mathRatio < containerRatio) {
                        var targetWidth = mathHeight * containerRatio;
                        var centerX = (bounds.left + bounds.right) / 2;
                        bounds.left = centerX - targetWidth / 2;
                        bounds.right = centerX + targetWidth / 2;
                    } else {
                        var targetHeight = mathWidth / containerRatio;
                        var centerY = (bounds.bottom + bounds.top) / 2;
                        bounds.bottom = centerY - targetHeight / 2;
                        bounds.top = centerY + targetHeight / 2;
                    }
                }
                mathBounds = bounds;
            }
        };

        var calc3D = null;
        var expressions3DStored = '';

        try {
            if (!isOffline && typeof Desmos !== 'undefined') {
                var elt = document.getElementById('calculator2D');
                calc = Desmos.GraphingCalculator(elt, {
                    expressions: true, settingsMenu: true, zoomButtons: true, lockViewport: false
                });
                if (calc && typeof calc.setMathBounds === 'function') {
                    var originalSetMathBounds = calc.setMathBounds;
                    var lastRequestedBounds = null;
                    var lastWidth = 0;
                    var lastHeight = 0;
                    calc.setMathBounds = function(bounds) {
                        lastRequestedBounds = bounds;
                        var w = elt.clientWidth || window.innerWidth;
                        var h = elt.clientHeight || window.innerHeight;
                        if (w > 0 && h > 0 && bounds) {
                            lastWidth = w;
                            lastHeight = h;
                            var containerRatio = w / h;
                            var mathWidth = bounds.right - bounds.left;
                            var mathHeight = bounds.top - bounds.bottom;
                            var mathRatio = mathWidth / mathHeight;
                            var adjustedBounds = {
                                left: bounds.left,
                                right: bounds.right,
                                bottom: bounds.bottom,
                                top: bounds.top
                            };
                            if (mathRatio < containerRatio) {
                                var targetWidth = mathHeight * containerRatio;
                                var centerX = (bounds.left + bounds.right) / 2;
                                adjustedBounds.left = centerX - targetWidth / 2;
                                adjustedBounds.right = centerX + targetWidth / 2;
                            } else {
                                var targetHeight = mathWidth / containerRatio;
                                var centerY = (bounds.bottom + bounds.top) / 2;
                                adjustedBounds.bottom = centerY - targetHeight / 2;
                                adjustedBounds.top = centerY + targetHeight / 2;
                            }
                            originalSetMathBounds.call(calc, adjustedBounds);
                        } else {
                            originalSetMathBounds.call(calc, bounds);
                        }
                    };

                    if (typeof ResizeObserver !== 'undefined') {
                        var ro = new ResizeObserver(function(entries) {
                            for (var entry of entries) {
                                var w = entry.contentRect.width;
                                var h = entry.contentRect.height;
                                if (w > 0 && h > 0 && lastRequestedBounds && (w !== lastWidth || h !== lastHeight)) {
                                    if (typeof calc.resize === 'function') {
                                        calc.resize();
                                    }
                                    calc.setMathBounds(lastRequestedBounds);
                                }
                            }
                        });
                        ro.observe(elt);
                    } else {
                        window.addEventListener('resize', function() {
                            var w = elt.clientWidth || window.innerWidth;
                            var h = elt.clientHeight || window.innerHeight;
                            if (w > 0 && h > 0 && lastRequestedBounds && (w !== lastWidth || h !== lastHeight)) {
                                if (typeof calc.resize === 'function') {
                                    calc.resize();
                                }
                                calc.setMathBounds(lastRequestedBounds);
                            }
                        });
                    }
                }
            }
        } catch(e) {
            console.error('Desmos init error:', e);
        }

        // Functions to switch modes called by WPF C#
        function show2DMode() {
            var off3D = document.getElementById('offline-3d-message');
            if (off3D) off3D.style.display = 'none';

            if (isOffline) {
                var offElt = document.getElementById('offline-container');
                if (offElt) offElt.style.display = 'flex';
                var h3 = document.querySelector('#offline-container .offline-header h3');
                if (h3) h3.innerText = '⚠️ Đang chạy ở chế độ ngoại tuyến (Offline)';
                var p = document.querySelector('#offline-container .offline-header p');
                if (p) p.innerText = 'Không thể kết nối tới Desmos API. Hệ thống đã tự động vẽ đồ thị hàm số trực quan dưới đây.';
                renderOfflineSvg(capturedExpressions, mathBounds);
                return;
            }
            document.getElementById('calculator2D').style.display = 'block';
            document.getElementById('calculator3D').style.display = 'none';
        }

        function show3DMode(expressions3DJs) {
            if (expressions3DJs) {
                expressions3DStored = expressions3DJs;
            }
            if (isOffline || typeof Desmos === 'undefined' || typeof Desmos.Calculator3D === 'undefined') {
                var calc2D = document.getElementById('calculator2D');
                var calc3D = document.getElementById('calculator3D');
                if (calc2D) calc2D.style.display = 'none';
                if (calc3D) calc3D.style.display = 'none';
                var offElt = document.getElementById('offline-container');
                if (offElt) offElt.style.display = 'flex';
                var off3D = document.getElementById('offline-3d-message');
                if (off3D) off3D.style.display = 'none';

                var h3 = document.querySelector('#offline-container .offline-header h3');
                if (h3) h3.innerText = '⚠️ Đang chạy ở chế độ ngoại tuyến (Offline) - Đồ thị 3D';
                var p = document.querySelector('#offline-container .offline-header p');
                if (p) p.innerText = 'Không có kết nối mạng để tải Desmos 3D. Dưới đây là hình vẽ phác thảo 3D trực quan của khối hình.';

                renderOffline3D(graphTitle);
                return;
            }
            document.getElementById('calculator2D').style.display = 'none';
            document.getElementById('calculator3D').style.display = 'block';

            if (!calc3D) {
                try {
                    var elt3D = document.getElementById('calculator3D');
                    calc3D = Desmos.Calculator3D(elt3D, {
                        expressions: true, settingsMenu: true, zoomButtons: true
                    });
                    if (expressions3DStored) {
                        // Run 3D expressions
                        var run3D = new Function('calc', expressions3DStored);
                        run3D(calc3D);
                    }
                } catch(e) {
                    console.error('Desmos 3D initialization failed:', e);
                }
            }
        }
    </script>
    <script>
        try {
            [[SAFE_JS]]
        } catch(e) {
            console.error('Error running expressions:', e);
        }
    </script>
    <script>
        if (isOffline || typeof Desmos === 'undefined') {
            renderOfflineSvg(capturedExpressions, mathBounds);
        }

        function renderOfflineSvg(expressions, bounds) {
            var svg = document.getElementById('svg-canvas');
            if (!svg) return;

            // Helper to get median using standard algorithm
            function jsGetMedian(arr) {
                if (!arr || arr.length === 0) return 0;
                var sorted = arr.slice().sort(function(a, b) { return a - b; });
                var len = sorted.length;
                if (len % 2 === 1) {
                    return sorted[Math.floor(len / 2)];
                } else {
                    return (sorted[len / 2 - 1] + sorted[len / 2]) / 2;
                }
            }

            // Helper to get quartiles using SGK standard (median-split method)
            function jsGetQuartiles(arr) {
                if (!arr || arr.length < 2) return { q1: 0, q3: 0 };
                var sorted = arr.slice().sort(function(a, b) { return a - b; });
                var len = sorted.length;
                var lowerHalf, upperHalf;
                if (len % 2 === 0) {
                    var halfSize = len / 2;
                    lowerHalf = sorted.slice(0, halfSize);
                    upperHalf = sorted.slice(halfSize);
                } else {
                    var halfSize = Math.floor(len / 2);
                    lowerHalf = sorted.slice(0, halfSize);
                    upperHalf = sorted.slice(halfSize + 1);
                }
                return { q1: jsGetMedian(lowerHalf), q3: jsGetMedian(upperHalf) };
            }

            // Scan for list L=[...]
            var dataList = null;
            expressions.forEach(function(exp) {
                var latex = exp.latex || '';
                var listMatch = latex.replace(/\s+/g, '').match(/^L=\[([0-9\.,-]+)\]$/);
                if (listMatch) {
                    dataList = listMatch[1].split(',').map(Number).filter(isFinite);
                }
            });

            // Default bounds if not specified
            var left = (bounds && typeof bounds.left === 'number') ? bounds.left : -10;
            var right = (bounds && typeof bounds.right === 'number') ? bounds.right : 10;
            var bottom = (bounds && typeof bounds.bottom === 'number') ? bounds.bottom : -10;
            var top = (bounds && typeof bounds.top === 'number') ? bounds.top : 10;

            // Prevent division by zero
            if (left === right) { left -= 5; right += 5; }
            if (bottom === top) { bottom -= 5; top += 5; }

            // Adjust bounds to match the SVG canvas aspect ratio (800 / 600 = 1.3333)
            var containerRatio = 800 / 600;
            var mathWidth = right - left;
            var mathHeight = top - bottom;
            var mathRatio = mathWidth / mathHeight;
            if (mathRatio < containerRatio) {
                var targetWidth = mathHeight * containerRatio;
                var centerX = (left + right) / 2;
                left = centerX - targetWidth / 2;
                right = centerX + targetWidth / 2;
            } else {
                var targetHeight = mathWidth / containerRatio;
                var centerY = (bottom + top) / 2;
                bottom = centerY - targetHeight / 2;
                top = centerY + targetHeight / 2;
            }

            var width = 800;
            var height = 600;

            function mapX(x) {
                return ((x - left) / (right - left)) * width;
            }

            function mapY(y) {
                return ((top - y) / (top - bottom)) * height;
            }

            var scaleX = width / (right - left);
            var scaleY = height / (top - bottom);

            var svgHtml = '<defs>' +
                          '<marker id=\'arrow-1565C0\' viewBox=\'0 0 10 10\' refX=\'6\' refY=\'5\' markerWidth=\'6\' markerHeight=\'6\' orient=\'auto\'><path d=\'M 0 1.5 L 8 5 L 0 8.5 z\' fill=\'#1565C0\'/></marker>' +
                          '<marker id=\'arrow-1B5E20\' viewBox=\'0 0 10 10\' refX=\'6\' refY=\'5\' markerWidth=\'6\' markerHeight=\'6\' orient=\'auto\'><path d=\'M 0 1.5 L 8 5 L 0 8.5 z\' fill=\'#1B5E20\'/></marker>' +
                          '<marker id=\'arrow-C62828\' viewBox=\'0 0 10 10\' refX=\'6\' refY=\'5\' markerWidth=\'6\' markerHeight=\'6\' orient=\'auto\'><path d=\'M 0 1.5 L 8 5 L 0 8.5 z\' fill=\'#C62828\'/></marker>' +
                          '<marker id=\'arrow-E65100\' viewBox=\'0 0 10 10\' refX=\'6\' refY=\'5\' markerWidth=\'6\' markerHeight=\'6\' orient=\'auto\'><path d=\'M 0 1.5 L 8 5 L 0 8.5 z\' fill=\'#E65100\'/></marker>' +
                          '<marker id=\'arrow-8E24AA\' viewBox=\'0 0 10 10\' refX=\'6\' refY=\'5\' markerWidth=\'6\' markerHeight=\'6\' orient=\'auto\'><path d=\'M 0 1.5 L 8 5 L 0 8.5 z\' fill=\'#8E24AA\'/></marker>' +
                          '<marker id=\'arrow-default\' viewBox=\'0 0 10 10\' refX=\'6\' refY=\'5\' markerWidth=\'6\' markerHeight=\'6\' orient=\'auto\'><path d=\'M 0 1.5 L 8 5 L 0 8.5 z\' fill=\'#757575\'/></marker>' +
                          '</defs>';

            // Draw Background Grid
            var gridColor = '#F0F0F0';
            var axisColor = '#9E9E9E';
            var labelColor = '#757575';
            
            // Calculate grid interval
            var rangeX = right - left;
            var interval = 1;
            if (rangeX > 200) interval = 20;
            else if (rangeX > 100) interval = 10;
            else if (rangeX > 50) interval = 5;
            else if (rangeX > 20) interval = 2;
            else if (rangeX > 10) interval = 1;
            else if (rangeX > 5) interval = 0.5;
            else if (rangeX > 2) interval = 0.2;
            else interval = 0.1;

            var startX = Math.ceil(left / interval) * interval;
            var endX = Math.floor(right / interval) * interval;
            var startY = Math.ceil(bottom / interval) * interval;
            var endY = Math.floor(top / interval) * interval;

            // Vertical Grid Lines
            for (var x = startX; x <= endX; x += interval) {
                if (Math.abs(x) < 0.0001) continue;
                var sx = mapX(x);
                svgHtml += '<line x1=\'' + sx + '\' y1=\'0\' x2=\'' + sx + '\' y2=\'' + height + '\' stroke=\'' + gridColor + '\' stroke-width=\'1\' />';
                var yPos = (bottom < 0 && top > 0) ? mapY(0) + 15 : height - 10;
                if (yPos < 10) yPos = 20;
                if (yPos > height - 10) yPos = height - 10;
                svgHtml += '<text x=\'' + sx + '\' y=\'' + yPos + '\' fill=\'' + labelColor + '\' font-size=\'10\' text-anchor=\'middle\'>' + formatOfflineNumber(x) + '</text>';
            }

            // Horizontal Grid Lines
            for (var y = startY; y <= endY; y += interval) {
                if (Math.abs(y) < 0.0001) continue;
                var sy = mapY(y);
                svgHtml += '<line x1=\'0\' y1=\'' + sy + '\' x2=\'' + width + '\' y2=\'' + sy + '\' stroke=\'' + gridColor + '\' stroke-width=\'1\' />';
                var xPos = (left < 0 && right > 0) ? mapX(0) - 10 : 15;
                if (xPos < 10) xPos = 10;
                if (xPos > width - 15) xPos = width - 20;
                svgHtml += '<text x=\'' + xPos + '\' y=\'' + (sy + 4) + '\' fill=\'' + labelColor + '\' font-size=\'10\' text-anchor=\'end\'>' + formatOfflineNumber(y) + '</text>';
            }

            // Draw Y-Axis (x = 0)
            if (left <= 0 && right >= 0) {
                var ax = mapX(0);
                svgHtml += '<line x1=\'' + ax + '\' y1=\'0\' x2=\'' + ax + '\' y2=\'' + height + '\' stroke=\'' + axisColor + '\' stroke-width=\'2\' />';
                svgHtml += '<polygon points=\'' + ax + ',0 ' + (ax-4) + ',8 ' + (ax+4) + ',8\' fill=\'' + axisColor + '\' />';
                svgHtml += '<text x=\'' + (ax+10) + '\' y=\'15\' fill=\'' + axisColor + '\' font-weight=\'bold\' font-size=\'12\'>y</text>';
            }

            // Draw X-Axis (y = 0)
            if (bottom <= 0 && top >= 0) {
                var ay = mapY(0);
                svgHtml += '<line x1=\'0\' y1=\'' + ay + '\' x2=\'' + width + '\' y2=\'' + ay + '\' stroke=\'' + axisColor + '\' stroke-width=\'2\' />';
                svgHtml += '<polygon points=\'' + width + ',' + ay + ' ' + (width-8) + ',' + (ay-4) + ' ' + (width-8) + ',' + (ay+4) + '\' fill=\'' + axisColor + '\' />';
                svgHtml += '<text x=\'' + (width-15) + '\' y=\'' + (ay-10) + '\' fill=\'' + axisColor + '\' font-weight=\'bold\' font-size=\'12\'>x</text>';
            }

            // Draw Expressions
            // Helper to evaluate basic mathematical latex expressions in Javascript
            function evaluateLatex(latex, xVal, nVal) {
                var expr = latex;
                expr = expr.replace(/^[a-zA-Z](?:\(x\))?\s*=\s*/, '');
                
                // a. Xử lý logarit cơ số bất kỳ (ví dụ: \log_2(x) hoặc \log_{2}(x))
                expr = expr.replace(/\\log_\{?([0-9.]+)\}?\((.+?)\)/g, '(Math.log($2)/Math.log($1))');
                
                // b. Xử lý \ln và \log (cơ số 10)
                expr = expr.replace(/\\ln\b/g, 'Math.log');
                expr = expr.replace(/\\log\b/g, 'Math.log10');
                
                // c. Xử lý trị tuyệt đối \left| ... \right|
                while (expr.indexOf('\\left|') !== -1) {
                    expr = expr.replace(/\\left\|([^|]+)\\right\|/g, 'Math.abs($1)');
                }
                
                // d. Xử lý hàm floor làm tròn xuống
                expr = expr.replace(/\\floor\b/g, 'Math.floor');

                // 1. Insert * for implicit multiplication (e.g. 2x -> 2*x, 2( -> 2*(, )x -> )*x)
                expr = expr.replace(/(\d+(?:\.\d+)?)\s*([a-zA-Z\(])/g, '$1*$2');
                expr = expr.replace(/\)\s*([a-zA-Z\d\(])/g, ')*$1');

                // 2. Replace x and n variables using word boundaries to prevent keyword replacement
                if (xVal !== null && xVal !== undefined) {
                    expr = expr.replace(/\bx\b/g, '(' + xVal + ')');
                }
                if (nVal !== null && nVal !== undefined) {
                    expr = expr.replace(/\bn\b/g, '(' + nVal + ')');
                }
                
                // 3. Replace dynamic slider variables (like time t) with their values, or default to 0
                for (var sliderVar in sliders) {
                    var val = (typeof sliders[sliderVar].value === 'number') ? sliders[sliderVar].value : sliders[sliderVar].start;
                    var regex = new RegExp('\\b' + sliderVar + '\\b', 'g');
                    expr = expr.replace(regex, '(' + val + ')');
                }
                // Fallback for t variable if slider not explicitly mapped
                expr = expr.replace(/\bt\b/g, '(0)');
                
                // e. Thay thế hằng số pi và e (euler)
                expr = expr.replace(/\bpi\b/g, Math.PI);
                expr = expr.replace(/\be\b/g, Math.E);
                
                expr = expr.replace(/\\cdot/g, '*');
                expr = expr.replace(/\\times/g, '*');
                
                while (expr.indexOf('\\frac') !== -1) {
                    expr = expr.replace(/\\frac\s*\{([^{}]+)\}\s*\{([^{}]+)\}/g, '(($1)/($2))');
                }
                
                // Replace ^ with ** and wrap in parenthesized group to prevent unary precedence syntax error
                while (expr.indexOf('^') !== -1) {
                    var bracePower = expr.match(/([a-zA-Z0-9\.\(\)]+)\^\{([^{}]+)\}/);
                    if (bracePower) {
                        expr = expr.replace(/([a-zA-Z0-9\.\(\)]+)\^\{([^{}]+)\}/g, '(($1)**($2))');
                    } else {
                        var simplePower = expr.match(/([a-zA-Z0-9\.\(\)]+)\^([a-zA-Z0-9\.\(\)]+)/);
                        if (simplePower) {
                            expr = expr.replace(/([a-zA-Z0-9\.\(\)]+)\^([a-zA-Z0-9\.\(\)]+)/g, '(($1)**($2))');
                        } else {
                            break;
                        }
                    }
                }
                
                expr = expr.replace(/\{/g, '(').replace(/\}/g, ')');
                
                expr = expr.replace(/\\?sin\b/g, 'Math.sin');
                expr = expr.replace(/\\?cos\b/g, 'Math.cos');
                expr = expr.replace(/\\?tan\b/g, 'Math.tan');
                expr = expr.replace(/\\?cot\b/g, '(1.0/Math.tan)');
                expr = expr.replace(/\\?sqrt/g, 'Math.sqrt');
                
                try {
                    return Function('return (' + expr + ')')();
                } catch(e) {
                    return NaN;
                }
            }

            // Find all sliders in expressions (e.g. n=[1,...,10] or t=0)
            var sliders = {};
            expressions.forEach(function(exp) {
                var latex = exp.latex || '';
                var sliderMatch = latex.match(/^([a-zA-Z])\s*=\s*\[\s*([0-9-]+)\s*,\s*(?:\d+,)?\s*\.\.\.\s*,\s*([0-9-]+)\s*\]$/);
                if (sliderMatch) {
                    var varName = sliderMatch[1];
                    var start = parseInt(sliderMatch[2]);
                    var end = parseInt(sliderMatch[3]);
                    sliders[varName] = { start: start, end: end };
                }
                // Also check for constant assignments or slider defaults (like t=0)
                var constMatch = latex.match(/^([a-zA-Z])\s*=\s*([0-9\.-]+)$/);
                if (constMatch) {
                    var varName = constMatch[1];
                    var val = parseFloat(constMatch[2]);
                    sliders[varName] = { value: val };
                }
            });

            // Draw Expressions
            expressions.forEach(function(exp) {
                var latex = exp.latex || '';

                var color = exp.color || '#2E7D32';
                var strokeWidth = exp.lineWidth || 2;
                var fillOpacity = (typeof exp.fillOpacity === 'number') ? exp.fillOpacity : 0;
                var fill = fillOpacity > 0 ? color : 'none';
                var lineStyle = exp.lineStyle || 'SOLID';
                var dashArray = lineStyle === 'DASHED' ? '6,6' : 'none';

                // Skip slider declarations
                if (/^[a-zA-Z]\s*=\s*\[.*\]$/.test(latex)) {
                    return;
                }

                // Check boxplot(L, offset, height)
                var boxMatch = latex.replace(/\s+/g, '').match(/^boxplot\(L,([0-9\.-]+),([0-9\.-]+)\)$/);
                if (boxMatch && dataList && dataList.length >= 2) {
                    var offset = parseFloat(boxMatch[1]);
                    var heightVal = parseFloat(boxMatch[2]);
                    
                    var minVal = Math.min.apply(null, dataList);
                    var maxVal = Math.max.apply(null, dataList);
                    var medianVal = jsGetMedian(dataList);
                    var qs = jsGetQuartiles(dataList);
                    var q1Val = qs.q1;
                    var q3Val = qs.q3;

                    var yCenter = mapY(offset);
                    var yTop = mapY(offset + heightVal / 2);
                    var yBottom = mapY(offset - heightVal / 2);
                    
                    var yCapTop = mapY(offset + heightVal / 4);
                    var yCapBottom = mapY(offset - heightVal / 4);

                    var xMin = mapX(minVal);
                    var xMax = mapX(maxVal);
                    var xQ1 = mapX(q1Val);
                    var xQ3 = mapX(q3Val);
                    var xMedian = mapX(medianVal);

                    // Whisker lines
                    svgHtml += '<line x1=\'' + xMin + '\' y1=\'' + yCenter + '\' x2=\'' + xQ1 + '\' y2=\'' + yCenter + '\' stroke=\'' + color + '\' stroke-width=\'' + strokeWidth + '\' />';
                    svgHtml += '<line x1=\'' + xQ3 + '\' y1=\'' + yCenter + '\' x2=\'' + xMax + '\' y2=\'' + yCenter + '\' stroke=\'' + color + '\' stroke-width=\'' + strokeWidth + '\' />';
                    
                    // Whisker caps
                    svgHtml += '<line x1=\'' + xMin + '\' y1=\'' + yCapTop + '\' x2=\'' + xMin + '\' y2=\'' + yCapBottom + '\' stroke=\'' + color + '\' stroke-width=\'' + strokeWidth + '\' />';
                    svgHtml += '<line x1=\'' + xMax + '\' y1=\'' + yCapTop + '\' x2=\'' + xMax + '\' y2=\'' + yCapBottom + '\' stroke=\'' + color + '\' stroke-width=\'' + strokeWidth + '\' />';

                    // Box rect
                    var boxW = xQ3 - xQ1;
                    var boxH = yBottom - yTop;
                    svgHtml += '<rect x=\'' + xQ1 + '\' y=\'' + yTop + '\' width=\'' + boxW + '\' height=\'' + boxH + '\' stroke=\'' + color + '\' stroke-width=\'' + strokeWidth + '\' fill=\'' + color + '\' fill-opacity=\'0.15\' />';

                    // Median line
                    svgHtml += '<line x1=\'' + xMedian + '\' y1=\'' + yTop + '\' x2=\'' + xMedian + '\' y2=\'' + yBottom + '\' stroke=\'' + color + '\' stroke-width=\'' + strokeWidth + '\' />';
                    return;
                }

                // Check dotplot(L)
                if (latex.replace(/\s+/g, '') === 'dotplot(L)' && dataList && dataList.length > 0) {
                    var counts = {};
                    dataList.forEach(function(v) {
                        counts[v] = (counts[v] || 0) + 1;
                    });
                    
                    var maxCount = 1;
                    for (var k in counts) {
                        if (counts[k] > maxCount) maxCount = counts[k];
                    }
                    
                    var stepY = 0.3;
                    if (maxCount > 5) {
                        stepY = 1.2 / maxCount;
                    }
                    
                    var pSize = exp.pointSize || 10;
                    
                    for (var k in counts) {
                        var val = parseFloat(k);
                        var count = counts[k];
                        var cx = mapX(val);
                        for (var idx = 0; idx < count; idx++) {
                            var cy = mapY(0.5 + idx * stepY);
                            svgHtml += '<circle cx=\'' + cx + '\' cy=\'' + cy + '\' r=\'' + (pSize / 2) + '\' fill=\'' + color + '\' stroke=\'none\' />';
                        }
                    }
                    return;
                }

                // Pre-process fractions in parentheses like (1/2) or (-3/4) to decimals
                latex = latex.replace(/([-+]?)\(([-+]?[0-9]+)\/([0-9]+)\)/g, function(match, sign, num, den) {
                    var val = parseFloat(num) / parseFloat(den);
                    var signVal = sign === '-' ? -1 : 1;
                    return (signVal * val).toString();
                });

                // D. Check Parametric Curve: (x(t), y(t))
                var paramMatch = latex.match(/^\s*\((.*)\)\s*$/);
                if (paramMatch && latex.indexOf('t') !== -1 && exp.parametricDomain) {
                    var content = paramMatch[1];
                    var parts = [];
                    var depth = 0;
                    var current = '';
                    for (var i = 0; i < content.length; i++) {
                        var c = content[i];
                        if (c === '(' || c === '{' || c === '[') depth++;
                        else if (c === ')' || c === '}' || c === ']') depth--;
                        
                        if (c === ',' && depth === 0) {
                            parts.push(current);
                            current = '';
                        } else {
                            current += c;
                        }
                    }
                    parts.push(current);
                    
                    if (parts.length === 2) {
                        var xExpr = parts[0].trim();
                        var yExpr = parts[1].trim();
                        var minT = parseFloat(exp.parametricDomain.min);
                        var maxT = parseFloat(exp.parametricDomain.max);
                        if (!isFinite(minT)) {
                            minT = evaluateLatex(exp.parametricDomain.min, null, null);
                        }
                        if (!isFinite(maxT)) {
                            maxT = evaluateLatex(exp.parametricDomain.max, null, null);
                        }
                        if (isFinite(minT) && isFinite(maxT)) {
                            var pts = [];
                            var steps = 100;
                            var startT = Math.min(minT, maxT);
                            var endT = Math.max(minT, maxT);
                            for (var j = 0; j <= steps; j++) {
                                var tVal = startT + (endT - startT) * (j / steps);
                                var tRegex = new RegExp('\\bt\\b', 'g');
                                var px = evaluateLatex(xExpr.replace(tRegex, '(' + tVal + ')'), null, null);
                                var py = evaluateLatex(yExpr.replace(tRegex, '(' + tVal + ')'), null, null);
                                if (isFinite(px) && isFinite(py)) {
                                    pts.push(mapX(px) + ',' + mapY(py));
                                }
                            }
                            if (pts.length > 1) {
                                var cleanColor = color.replace('#', '').toUpperCase();
                                var markerId = 'arrow-default';
                                if (cleanColor === '1565C0') markerId = 'arrow-1565C0';
                                else if (cleanColor === '1B5E20') markerId = 'arrow-1B5E20';
                                else if (cleanColor === 'C62828') markerId = 'arrow-C62828';
                                else if (cleanColor === 'E65100') markerId = 'arrow-E65100';
                                else if (cleanColor === '8E24AA') markerId = 'arrow-8E24AA';
                                var markerAttr = ' marker-end=\'url(#' + markerId + ')\'';
                                svgHtml += '<polyline points=\'' + pts.join(' ') + '\' stroke=\'' + color + '\' stroke-width=\'' + strokeWidth + '\' fill=\'none\' stroke-dasharray=\'' + dashArray + '\'' + markerAttr + ' />';
                            }
                        }
                        return;
                    }
                }

                // A. Check Point expressions containing variables / lists (e.g. (n, f(n)))
                var ptMatch = latex.match(/^\s*\((.*)\)\s*$/);
                if (ptMatch && latex.indexOf('polygon') === -1) {
                    var content = ptMatch[1];
                    var parts = [];
                    var depth = 0;
                    var current = '';
                    for (var i = 0; i < content.length; i++) {
                        var c = content[i];
                        if (c === '(' || c === '{' || c === '[') depth++;
                        else if (c === ')' || c === '}' || c === ']') depth--;
                        
                        if (c === ',' && depth === 0) {
                            parts.push(current);
                            current = '';
                        } else {
                            current += c;
                        }
                    }
                    parts.push(current);

                    if (parts.length === 2) {
                        var xExpr = parts[0].trim();
                        var yExpr = parts[1].trim();
                        var pSize = exp.pointSize || 8;

                        // HỖ TRỢ VẼ MẢNG ĐIỂM RỜI RẠC (MÔ HÌNH OFFLINE CHO BERNOULLI)
                        if (xExpr.indexOf('[') === 0 && yExpr.indexOf('[') === 0) {
                            try {
                                var xArr = JSON.parse(xExpr);
                                var yArr = JSON.parse(yExpr);
                                if (Array.isArray(xArr) && Array.isArray(yArr)) {
                                    var len = Math.min(xArr.length, yArr.length);
                                    for (var idx = 0; idx < len; idx++) {
                                        var px = parseFloat(xArr[idx]);
                                        var py = parseFloat(yArr[idx]);
                                        if (isFinite(px) && isFinite(py)) {
                                            var cx = mapX(px);
                                            var cy = mapY(py);
                                            svgHtml += '<circle cx=\'' + cx + '\' cy=\'' + cy + '\' r=\'' + (pSize/2) + '\' fill=\'' + color + '\' stroke=\'none\' />';
                                        }
                                    }
                                }
                            } catch(err) {
                                console.error('Lỗi phân tích cú pháp mảng điểm offline:', err);
                            }
                            return; // Kết thúc xử lý mảng điểm
                        }

                        var xExpr = parts[0];
                        var yExpr = parts[1];

                        // Detect if xExpr or yExpr uses any slider variable
                        var activeSliderVar = null;
                        for (var varName in sliders) {
                            var regex = new RegExp('\\b' + varName + '\\b');
                            if (regex.test(xExpr) || regex.test(yExpr)) {
                                activeSliderVar = varName;
                                break;
                            }
                        }

                        var pSize = exp.pointSize || 8;

                        if (activeSliderVar) {
                            var slider = sliders[activeSliderVar];
                            var step = slider.start <= slider.end ? 1 : -1;
                            for (var val = slider.start; step > 0 ? val <= slider.end : val >= slider.end; val += step) {
                                var px = evaluateLatex(xExpr, null, val);
                                var py = evaluateLatex(yExpr, null, val);
                                if (isFinite(px) && isFinite(py)) {
                                    var cx = mapX(px);
                                    var cy = mapY(py);
                                    if (exp.pointStyle === 'OPEN') {
                                        svgHtml += '<circle cx=\'' + cx + '\' cy=\'' + cy + '\' r=\'' + (pSize/2) + '\' fill=\'#ffffff\' stroke=\'' + color + '\' stroke-width=\'2\' />';
                                    } else {
                                        svgHtml += '<circle cx=\'' + cx + '\' cy=\'' + cy + '\' r=\'' + (pSize/2) + '\' fill=\'' + color + '\' stroke=\'none\' />';
                                    }
                                }
                            }
                        } else {
                            // Single point
                            var px = evaluateLatex(xExpr, null, null);
                            var py = evaluateLatex(yExpr, null, null);
                            if (isFinite(px) && isFinite(py)) {
                                var cx = mapX(px);
                                var cy = mapY(py);
                                if (exp.pointStyle === 'OPEN') {
                                    svgHtml += '<circle cx=\'' + cx + '\' cy=\'' + cy + '\' r=\'' + (pSize/2) + '\' fill=\'#ffffff\' stroke=\'' + color + '\' stroke-width=\'2\' />';
                                } else {
                                    svgHtml += '<circle cx=\'' + cx + '\' cy=\'' + cy + '\' r=\'' + (pSize/2) + '\' fill=\'' + color + '\' stroke=\'none\' />';
                                }
                                if (exp.showLabel && exp.label) {
                                    svgHtml += '<text x=\'' + (cx + 8) + '\' y=\'' + (cy - 8) + '\' fill=\'' + color + '\' font-weight=\'bold\' font-size=\'12\' text-anchor=\'start\'>' + exp.label + '</text>';
                                }
                            }
                        }
                        return;
                    }
                }

                // E. Check general linear equation: ax + by = c (or ax - by = c, etc.)
                // This is crucial for linear systems!
                var linMatch = latex.replace(/\s+/g, '').match(/^([-+]?[0-9.]*)x([-+]?[0-9.]*)y=([-+]?[0-9.]+)$/);
                if (linMatch) {
                    var a = linMatch[1] === '' || linMatch[1] === '+' ? 1 : linMatch[1] === '-' ? -1 : parseFloat(linMatch[1]);
                    var b = linMatch[2] === '' || linMatch[2] === '+' ? 1 : linMatch[2] === '-' ? -1 : parseFloat(linMatch[2]);
                    var c = parseFloat(linMatch[3]);
                    if (b !== 0) {
                        var pts = [];
                        var steps = 200;
                        for (var i = 0; i <= steps; i++) {
                            var x = left + (right - left) * (i / steps);
                            var y = (c - a * x) / b;
                            pts.push(mapX(x) + ',' + mapY(y));
                        }
                        if (pts.length > 1) {
                            svgHtml += '<polyline points=\'' + pts.join(' ') + '\' stroke=\'' + color + '\' stroke-width=\'' + strokeWidth + '\' fill=\'none\' stroke-dasharray=\'' + dashArray + '\' />';
                        }
                    } else if (a !== 0) {
                        var xVal = c / a;
                        var sx = mapX(xVal);
                        svgHtml += '<line x1=\'' + sx + '\' y1=\'0\' x2=\'' + sx + '\' y2=\'' + height + '\' stroke=\'' + color + '\' stroke-width=\'' + strokeWidth + '\' stroke-dasharray=\'' + dashArray + '\' />';
                    }
                    return;
                }

                // B. Check Function: f(x) = ... or y = ...
                if (/^[a-zA-Z](?:\(x\))?\s*=\s*/.test(latex) || /^y\s*=\s*/.test(latex)) {
                    // Make sure it doesn't match a simple vertical line or simple horizontal line which are handled below
                    if (!/^y\s*=\s*([-+]?[0-9]*\.?[0-9]+)$/.test(latex.replace(/\s+/g, '')) &&
                        !/^x\s*=\s*([-+]?[0-9]*\.?[0-9]+)$/.test(latex.replace(/\s+/g, '')) &&
                        !/polygon/.test(latex) && !/x\^2\s*\+\s*y\^2/.test(latex)) {
                        var pts = [];
                        var steps = 200;
                        for (var i = 0; i <= steps; i++) {
                            var x = left + (right - left) * (i / steps);
                            var y = evaluateLatex(latex, x, null);
                            if (isFinite(x) && isFinite(y)) {
                                pts.push(mapX(x) + ',' + mapY(y));
                            }
                        }
                        if (pts.length > 1) {
                            svgHtml += '<polyline points=\'' + pts.join(' ') + '\' stroke=\'' + color + '\' stroke-width=\'' + strokeWidth + '\' fill=\'none\' stroke-dasharray=\'' + dashArray + '\' />';
                        }
                        return;
                    }
                }

                // C. Check Polygon: polygon(...)
                if (latex.indexOf('polygon') !== -1) {
                    var pointRegex = /\(([-+]?[0-9]*\.?[0-9]+)\s*,\s*([-+]?[0-9]*\.?[0-9]+)\)/g;
                    var points = [];
                    var match;
                    while ((match = pointRegex.exec(latex)) !== null) {
                        points.push({ x: parseFloat(match[1]), y: parseFloat(match[2]) });
                    }

                    if (points.length === 2) {
                        svgHtml += '<line x1=\'' + mapX(points[0].x) + '\' y1=\'' + mapY(points[0].y) + '\' x2=\'' + mapX(points[1].x) + '\' y2=\'' + mapY(points[1].y) + '\' stroke=\'' + color + '\' stroke-width=\'' + strokeWidth + '\' stroke-dasharray=\'' + dashArray + '\' />';
                    } else if (points.length > 2) {
                        var pointsStr = points.map(function(p) {
                            return mapX(p.x) + ',' + mapY(p.y);
                        }).join(' ');
                        svgHtml += '<polygon points=\'' + pointsStr + '\' stroke=\'' + color + '\' stroke-width=\'' + strokeWidth + '\' fill=\'' + fill + '\' fill-opacity=\'' + fillOpacity + '\' stroke-dasharray=\'' + dashArray + '\' />';
                    }
                }
                // Check Circle: x^2 + y^2 = R^2
                else if (/x\^2\s*\+\s*y\^2\s*=\s*/.test(latex)) {
                    var radiusMatch = latex.match(/=\s*\{?\s*([-+]?[0-9]*\.?[0-9]+)\s*\}?(?:\^2)?/);
                    if (radiusMatch) {
                        var r = 0;
                        var sqMatch = latex.match(/=\s*\{?\s*([-+]?[0-9]*\.?[0-9]+)\s*\}?\^2/);
                        if (sqMatch) {
                            r = parseFloat(sqMatch[1]);
                        } else {
                            var rawVal = parseFloat(radiusMatch[1]);
                            r = Math.sqrt(rawVal);
                        }
                        
                        var cx = mapX(0);
                        var cy = mapY(0);
                        var rx = r * scaleX;
                        svgHtml += '<circle cx=\'' + cx + '\' cy=\'' + cy + '\' r=\'' + rx + '\' stroke=\'' + color + '\' stroke-width=\'' + strokeWidth + '\' fill=\'' + fill + '\' fill-opacity=\'' + fillOpacity + '\' stroke-dasharray=\'' + dashArray + '\' />';
                    }
                }
                // Check Ellipse
                else if ((latex.indexOf('x^2') !== -1 && latex.indexOf('y^2') !== -1 && latex.indexOf('+') !== -1 && latex.indexOf('=1') !== -1) ||
                         (/x\^2\s*\/\s*/.test(latex) && /=1/.test(latex.replace(/\s+/g, '')))) {
                    var cleaned = latex.replace(/\s+/g, '');
                    var ellRegex1 = /\\frac\{x\^2\}\{([0-9.]+)(\^2)?\}\+\\frac\{y\^2\}\{([0-9.]+)(\^2)?\}=1/i;
                    var ellRegex2 = /x\^2\/([0-9.]+)(\^2)?\+y\^2\/([0-9.]+)(\^2)?=1/i;
                    var ellRegex3 = /x\^2\/\{?([0-9.]+)\}?(?:\^2)?\+\(y-([-0-9.]+)\)\^2\/\{?([0-9.]+)\}?(?:\^2)?=1/i;
                    
                    var ellMatch = cleaned.match(ellRegex1) || cleaned.match(ellRegex2);
                    if (ellMatch) {
                        var rxVal = parseFloat(ellMatch[1]);
                        var ryVal = parseFloat(ellMatch[3]);
                        if (!ellMatch[2]) rxVal = Math.sqrt(rxVal);
                        if (!ellMatch[4]) ryVal = Math.sqrt(ryVal);
                        var cx = mapX(0);
                        var cy = mapY(0);
                        var rx = rxVal * scaleX;
                        var ry = ryVal * scaleY;
                        svgHtml += '<ellipse cx=\'' + cx + '\' cy=\'' + cy + '\' rx=\'' + rx + '\' ry=\'' + ry + '\' stroke=\'' + color + '\' stroke-width=\'' + strokeWidth + '\' fill=\'' + fill + '\' fill-opacity=\'' + fillOpacity + '\' stroke-dasharray=\'' + dashArray + '\' />';
                    } else {
                        var ellMatch3 = cleaned.match(ellRegex3);
                        if (ellMatch3) {
                            var rxVal = parseFloat(ellMatch3[1]);
                            var kVal = parseFloat(ellMatch3[2]);
                            var ryVal = parseFloat(ellMatch3[3]);
                            var cx = mapX(0);
                            var cy = mapY(kVal);
                            var rx = rxVal * scaleX;
                            var ry = ryVal * scaleY;
                            svgHtml += '<ellipse cx=\'' + cx + '\' cy=\'' + cy + '\' rx=\'' + rx + '\' ry=\'' + ry + '\' stroke=\'' + color + '\' stroke-width=\'' + strokeWidth + '\' fill=\'' + fill + '\' fill-opacity=\'' + fillOpacity + '\' stroke-dasharray=\'' + dashArray + '\' />';
                        }
                    }
                }
                // Check Hyperbola
                else if (latex.indexOf('x^2') !== -1 && latex.indexOf('y^2') !== -1 && latex.indexOf('-') !== -1 && latex.indexOf('=1') !== -1) {
                    var cleaned = latex.replace(/\s+/g, '');
                    var hypRegex1 = /\\frac\{x\^2\}\{([0-9.]+)(\^2)?\}-\\frac\{y\^2\}\{([0-9.]+)(\^2)?\}=1/i;
                    var hypRegex2 = /x\^2\/([0-9.]+)(\^2)?-y\^2\/([0-9.]+)(\^2)?=1/i;
                    var hypMatch = cleaned.match(hypRegex1) || cleaned.match(hypRegex2);
                    if (hypMatch) {
                        var aVal = parseFloat(hypMatch[1]);
                        var bVal = parseFloat(hypMatch[3]);
                        if (!hypMatch[2]) aVal = Math.sqrt(aVal);
                        if (!hypMatch[4]) bVal = Math.sqrt(bVal);
                        if (aVal > 0 && bVal > 0) {
                            // Draw right branch
                            var ptsRightTop = [];
                            var ptsRightBottom = [];
                            var steps = 100;
                            var xStart = aVal;
                            var xEnd = Math.max(right, aVal + 5);
                            for (var i = 0; i <= steps; i++) {
                                var x = xStart + (xEnd - xStart) * (i / steps);
                                var ySq = bVal * bVal * ((x * x) / (aVal * aVal) - 1);
                                var y = ySq >= 0 ? Math.sqrt(ySq) : 0;
                                ptsRightTop.push(mapX(x) + ',' + mapY(y));
                                ptsRightBottom.unshift(mapX(x) + ',' + mapY(-y));
                            }
                            var ptsRight = ptsRightBottom.concat(ptsRightTop);
                            svgHtml += '<polyline points=\'' + ptsRight.join(' ') + '\' stroke=\'' + color + '\' stroke-width=\'' + strokeWidth + '\' fill=\'none\' stroke-dasharray=\'' + dashArray + '\' />';

                            // Draw left branch
                            var ptsLeftTop = [];
                            var ptsLeftBottom = [];
                            var xStartLeft = -aVal;
                            var xEndLeft = Math.min(left, -aVal - 5);
                            for (var i = 0; i <= steps; i++) {
                                var x = xStartLeft + (xEndLeft - xStartLeft) * (i / steps);
                                var ySq = bVal * bVal * ((x * x) / (aVal * aVal) - 1);
                                var y = ySq >= 0 ? Math.sqrt(ySq) : 0;
                                ptsLeftTop.push(mapX(x) + ',' + mapY(y));
                                ptsLeftBottom.unshift(mapX(x) + ',' + mapY(-y));
                            }
                            var ptsLeft = ptsLeftBottom.concat(ptsLeftTop);
                            svgHtml += '<polyline points=\'' + ptsLeft.join(' ') + '\' stroke=\'' + color + '\' stroke-width=\'' + strokeWidth + '\' fill=\'none\' stroke-dasharray=\'' + dashArray + '\' />';
                        }
                    }
                }
                // Check Parabola
                else if (latex.indexOf('y^2') !== -1 && latex.indexOf('x') !== -1 && latex.indexOf('=') !== -1) {
                    var cleaned = latex.replace(/\s+/g, '');
                    var paraRegex = /y\^2=([0-9.]+)x/i;
                    var paraMatch = cleaned.match(paraRegex);
                    if (paraMatch) {
                        var coeff = parseFloat(paraMatch[1]);
                        if (coeff > 0) {
                            var ptsTop = [];
                            var ptsBottom = [];
                            var steps = 100;
                            var xStart = 0;
                            var xEnd = Math.max(right, 10);
                            for (var i = 0; i <= steps; i++) {
                                var x = xStart + (xEnd - xStart) * (i / steps);
                                var y = Math.sqrt(coeff * x);
                                ptsTop.push(mapX(x) + ',' + mapY(y));
                                ptsBottom.unshift(mapX(x) + ',' + mapY(-y));
                            }
                            var pts = ptsBottom.concat(ptsTop);
                            svgHtml += '<polyline points=\'' + pts.join(' ') + '\' stroke=\'' + color + '\' stroke-width=\'' + strokeWidth + '\' fill=\'none\' stroke-dasharray=\'' + dashArray + '\' />';
                        }
                    }
                }
                // Check Lines: y = C or x = C
                else if (/^y\s*=\s*([-+]?[0-9]*\.?[0-9]+)$/.test(latex.replace(/\s+/g, ''))) {
                    var yVal = parseFloat(latex.replace(/\s+/g, '').match(/^y=([-+]?[0-9]*\.?[0-9]+)$/)[1]);
                    var sy = mapY(yVal);
                    svgHtml += '<line x1=\'0\' y1=\'' + sy + '\' x2=\'' + width + '\' y2=\'' + sy + '\' stroke=\'' + color + '\' stroke-width=\'' + strokeWidth + '\' stroke-dasharray=\'' + dashArray + '\' />';
                    if (exp.showLabel && exp.label) {
                        svgHtml += '<text x=\'20\' y=\'' + (sy - 8) + '\' fill=\'' + color + '\' font-weight=\'bold\' font-size=\'12\'>' + exp.label + '</text>';
                    }
                }
                else if (/^x\s*=\s*([-+]?[0-9]*\.?[0-9]+)$/.test(latex.replace(/\s+/g, ''))) {
                    var xVal = parseFloat(latex.replace(/\s+/g, '').match(/^x=([-+]?[0-9]*\.?[0-9]+)$/)[1]);
                    var sx = mapX(xVal);
                    svgHtml += '<line x1=\'' + sx + '\' y1=\'0\' x2=\'' + sx + '\' y2=\'' + height + '\' stroke=\'' + color + '\' stroke-width=\'' + strokeWidth + '\' stroke-dasharray=\'' + dashArray + '\' />';
                    if (exp.showLabel && exp.label) {
                        svgHtml += '<text x=\'' + (sx + 8) + '\' y=\'40\' fill=\'' + color + '\' font-weight=\'bold\' font-size=\'12\'>' + exp.label + '</text>';
                    }
                }
            });

            svg.innerHTML = svgHtml;
        }

        function renderOffline3D(title) {
            var svg = document.getElementById('svg-canvas');
            if (!svg) return;

            var width = 800;
            var height = 600;
            var svgHtml = '';

            svgHtml += '<defs>' +
                       '<marker id=\'arrow-x\' viewBox=\'0 0 10 10\' refX=\'6\' refY=\'5\' markerWidth=\'6\' markerHeight=\'6\' orient=\'auto\'><path d=\'M 0 1.5 L 8 5 L 0 8.5 z\' fill=\'#546E7A\'/></marker>' +
                       '<marker id=\'arrow-y\' viewBox=\'0 0 10 10\' refX=\'6\' refY=\'5\' markerWidth=\'6\' markerHeight=\'6\' orient=\'auto\'><path d=\'M 0 1.5 L 8 5 L 0 8.5 z\' fill=\'#546E7A\'/></marker>' +
                       '<marker id=\'arrow-z\' viewBox=\'0 0 10 10\' refX=\'6\' refY=\'5\' markerWidth=\'6\' markerHeight=\'6\' orient=\'auto\'><path d=\'M 0 1.5 L 8 5 L 0 8.5 z\' fill=\'#546E7A\'/></marker>' +
                       '</defs>';

            svgHtml += '<rect width=\'800\' height=\'600\' fill=\'#ffffff\' rx=\'12\'/>';

            var cx = 400;
            var cy = 320;

            // Draw coordinate axes
            var axisColor = '#546E7A';
            var dashedColor = '#B0BEC5';
            // Ox\' (negative x): from (cx, cy) to (cx + 200, cy - 200)
            svgHtml += '<line x1=\'' + cx + '\' y1=\'' + cy + '\' x2=\'' + (cx + 200) + '\' y2=\'' + (cy - 200) + '\' stroke=\'' + dashedColor + '\' stroke-width=\'1.5\' stroke-dasharray=\'4,4\' />';
            // Oy\' (negative y): from (cx, cy) to (cx - 280, cy)
            svgHtml += '<line x1=\'' + cx + '\' y1=\'' + cy + '\' x2=\'' + (cx - 280) + '\' y2=\'' + cy + '\' stroke=\'' + dashedColor + '\' stroke-width=\'1.5\' stroke-dasharray=\'4,4\' />';
            // Oz\' (negative z): from (cx, cy) to (cx, cy + 250)
            svgHtml += '<line x1=\'' + cx + '\' y1=\'' + cy + '\' x2=\'' + cx + '\' y2=\'' + (cy + 250) + '\' stroke=\'' + dashedColor + '\' stroke-width=\'1.5\' stroke-dasharray=\'4,4\' />';

            // Ox (positive x): from (cx, cy) to (cx - 190, cy + 190)
            svgHtml += '<line x1=\'' + cx + '\' y1=\'' + cy + '\' x2=\'' + (cx - 190) + '\' y2=\'' + (cy + 190) + '\' stroke=\'' + axisColor + '\' stroke-width=\'2\' marker-end=\'url(#arrow-x)\' />';
            // Oy (positive y): from (cx, cy) to (cx + 270, cy)
            svgHtml += '<line x1=\'' + cx + '\' y1=\'' + cy + '\' x2=\'' + (cx + 270) + '\' y2=\'' + cy + '\' stroke=\'' + axisColor + '\' stroke-width=\'2\' marker-end=\'url(#arrow-y)\' />';
            // Oz (positive z): from (cx, cy) to (cx, cy - 240)
            svgHtml += '<line x1=\'' + cx + '\' y1=\'' + cy + '\' x2=\'' + cx + '\' y2=\'' + (cy - 240) + '\' stroke=\'' + axisColor + '\' stroke-width=\'2\' marker-end=\'url(#arrow-z)\' />';

            // Axis Labels
            svgHtml += '<text x=\'' + (cx - 205) + '\' y=\'' + (cy + 215) + '\' fill=\'' + axisColor + '\' font-weight=\'bold\' font-size=\'16\' font-style=\'italic\'>x</text>';
            svgHtml += '<text x=\'' + (cx + 285) + '\' y=\'' + (cy + 5) + '\' fill=\'' + axisColor + '\' font-weight=\'bold\' font-size=\'16\' font-style=\'italic\'>y</text>';
            svgHtml += '<text x=\'' + (cx - 15) + '\' y=\'' + (cy - 245) + '\' fill=\'' + axisColor + '\' font-weight=\'bold\' font-size=\'16\' font-style=\'italic\'>z</text>';
            // Origin Label O
            svgHtml += '<text x=\'' + (cx + 8) + '\' y=\'' + (cy - 8) + '\' fill=\'' + axisColor + '\' font-size=\'12\'>O</text>';

            var shapeType = '';
            var a = 4, b = 4, c = 4, r = 3, h = 6;
            var titleLower = title.toLowerCase();

            if (titleLower.indexOf('hộp') !== -1) {
                shapeType = 'cuboid';
                var match = titleLower.match(/([0-9.,]+)\s*×\s*([0-9.,]+)\s*×\s*([0-9.,]+)/);
                if (match) {
                    a = parseFloat(match[1].replace(',', '.'));
                    b = parseFloat(match[2].replace(',', '.'));
                    c = parseFloat(match[3].replace(',', '.'));
                }
            } else if (titleLower.indexOf('lập phương') !== -1) {
                shapeType = 'cube';
                var match = titleLower.match(/a\s*=\s*([0-9.,]+)/);
                if (match) {
                    a = parseFloat(match[1].replace(',', '.'));
                    b = a;
                    c = a;
                }
            } else if (titleLower.indexOf('chóp tam giác') !== -1) {
                shapeType = 'tri_pyramid';
                var matchA = titleLower.match(/a\s*=\s*([0-9.,]+)/);
                var matchH = titleLower.match(/h\s*=\s*([0-9.,]+)/);
                if (matchA) a = parseFloat(matchA[1].replace(',', '.'));
                if (matchH) h = parseFloat(matchH[1].replace(',', '.'));
            } else if (titleLower.indexOf('chóp tứ giác') !== -1 || titleLower.indexOf('chóp đều') !== -1) {
                shapeType = 'quad_pyramid';
                var matchA = titleLower.match(/a\s*=\s*([0-9.,]+)/);
                var matchH = titleLower.match(/h\s*=\s*([0-9.,]+)/);
                if (matchA) a = parseFloat(matchA[1].replace(',', '.'));
                if (matchH) h = parseFloat(matchH[1].replace(',', '.'));
            } else if (titleLower.indexOf('lăng trụ') !== -1) {
                shapeType = 'tri_prism';
                var matchA = titleLower.match(/a\s*=\s*([0-9.,]+)/);
                var matchH = titleLower.match(/h\s*=\s*([0-9.,]+)/);
                if (matchA) a = parseFloat(matchA[1].replace(',', '.'));
                if (matchH) h = parseFloat(matchH[1].replace(',', '.'));
            } else if (titleLower.indexOf('trụ') !== -1) {
                shapeType = 'cylinder';
                var matchR = titleLower.match(/r\s*=\s*([0-9.,]+)/);
                var matchH = titleLower.match(/h\s*=\s*([0-9.,]+)/);
                if (matchR) r = parseFloat(matchR[1].replace(',', '.'));
                if (matchH) h = parseFloat(matchH[1].replace(',', '.'));
            } else if (titleLower.indexOf('nón') !== -1) {
                shapeType = 'cone';
                var matchR = titleLower.match(/r\s*=\s*([0-9.,]+)/);
                var matchH = titleLower.match(/h\s*=\s*([0-9.,]+)/);
                if (matchR) r = parseFloat(matchR[1].replace(',', '.'));
                if (matchH) h = parseFloat(matchH[1].replace(',', '.'));
            } else if (titleLower.indexOf('cầu') !== -1) {
                shapeType = 'sphere';
                var matchR = titleLower.match(/r\s*=\s*([0-9.,]+)/);
                if (matchR) r = parseFloat(matchR[1].replace(',', '.'));
            }



            function drawLine(x1, y1, x2, y2, color, width, dashed) {
                var dash = dashed ? ' stroke-dasharray=\'6,4\'' : '';
                return '<line x1=\'' + x1 + '\' y1=\'' + y1 + '\' x2=\'' + x2 + '\' y2=\'' + y2 + '\' stroke=\'' + color + '\' stroke-width=\'' + width + '\'' + dash + ' />';
            }

            function drawPolygon(points, fill, stroke, strokeWidth, opacity) {
                return '<polygon points=\'' + points.join(' ') + '\' fill=\'' + fill + '\' fill-opacity=\'' + opacity + '\' stroke=\'' + stroke + '\' stroke-width=\'' + strokeWidth + '\' />';
            }

            function drawEllipse(cx, cy, rx, ry, stroke, strokeWidth, fill, opacity, dashed) {
                if (dashed) {
                    var pathSolid = 'M ' + (cx - rx) + ' ' + cy + ' A ' + rx + ' ' + ry + ' 0 0 0 ' + (cx + rx) + ' ' + cy;
                    var pathDashed = 'M ' + (cx - rx) + ' ' + cy + ' A ' + rx + ' ' + ry + ' 0 0 1 ' + (cx + rx) + ' ' + cy;
                    return '<path d=\'' + pathSolid + '\' fill=\'none\' stroke=\'' + stroke + '\' stroke-width=\'' + strokeWidth + '\' />' +
                           '<path d=\'' + pathDashed + '\' fill=\'none\' stroke=\'' + stroke + '\' stroke-width=\'' + strokeWidth + '\' stroke-dasharray=\'6,4\' />';
                } else {
                    return '<ellipse cx=\'' + cx + '\' cy=\'' + cy + '\' rx=\'' + rx + '\' ry=\'' + ry + '\' stroke=\'' + stroke + '\' stroke-width=\'' + strokeWidth + '\' fill=\'' + fill + '\' fill-opacity=\'' + opacity + '\' />';
                }
            }

            var color3D = '#1565C0';
            var colorDashed = '#90A4AE';

            if (shapeType === 'cube' || shapeType === 'cuboid') {
                var maxDim = Math.max(a, b, c);
                var scale = 200 / maxDim;
                var w = a * scale;
                var d = b * scale * 0.5;
                var hDim = c * scale;

                var x0 = cx - w/2 - d/2;
                var y0 = cy + hDim/2 - d/2;

                var A = [x0, y0];
                var B = [x0 + w, y0];
                var C = [x0 + w, y0 - hDim];
                var D = [x0, y0 - hDim];

                var A1 = [x0 + d, y0 - d];
                var B1 = [x0 + w + d, y0 - d];
                var C1 = [x0 + w + d, y0 - hDim - d];
                var D1 = [x0 + d, y0 - hDim - d];

                svgHtml += drawPolygon([A[0], A[1], B[0], B[1], C[0], C[1], D[0], D[1]], '#E3F2FD', color3D, 2.5, 0.4);
                svgHtml += drawPolygon([B[0], B[1], B1[0], B1[1], C1[0], C1[1], C[0], C[1]], '#E3F2FD', color3D, 2.5, 0.4);
                svgHtml += drawPolygon([D[0], D[1], D1[0], D1[1], C1[0], C1[1], C[0], C[1]], '#E3F2FD', color3D, 2.5, 0.3);

                svgHtml += drawLine(A1[0], A1[1], B1[0], B1[1], colorDashed, 2, true);
                svgHtml += drawLine(A1[0], A1[1], D1[0], D1[1], colorDashed, 2, true);
                svgHtml += drawLine(A1[0], A1[1], A[0], A[1], colorDashed, 2, true);

                svgHtml += '<text x=\'' + ((A[0]+B[0])/2) + '\' y=\'' + (A[1]+20) + '\' fill=\'#1565C0\' font-weight=\'bold\' font-size=\'14\' text-anchor=\'middle\'>a = ' + formatOfflineNumber(a) + '</text>';
                svgHtml += '<text x=\'' + (B[0]+d/2+10) + '\' y=\'' + (B[1]-d/2+15) + '\' fill=\'#1565C0\' font-weight=\'bold\' font-size=\'14\'>b = ' + formatOfflineNumber(b) + '</text>';
                svgHtml += '<text x=\'' + (A[0]-15) + '\' y=\'' + ((A[1]+D[1])/2) + '\' fill=\'#1565C0\' font-weight=\'bold\' font-size=\'14\' text-anchor=\'end\'>c = ' + formatOfflineNumber(c) + '</text>';

            } else if (shapeType === 'tri_pyramid') {
                var scale = 220 / Math.max(a, h);
                var baseW = a * scale;
                var pyH = h * scale;

                var A = [cx - baseW/2, cy + pyH/3];
                var B = [cx + baseW/2, cy + pyH/3];
                var C = [cx - baseW/6, cy + pyH/3 - baseW/3];
                var S = [cx, cy - pyH*2/3];

                svgHtml += drawPolygon([A[0], A[1], B[0], B[1], S[0], S[1]], '#E3F2FD', color3D, 2.5, 0.4);
                svgHtml += drawPolygon([B[0], B[1], C[0], C[1], S[0], S[1]], '#E3F2FD', color3D, 2.5, 0.3);

                svgHtml += drawLine(A[0], A[1], C[0], C[1], colorDashed, 2, true);
                svgHtml += drawLine(B[0], B[1], C[0], C[1], colorDashed, 2, true);
                svgHtml += drawLine(S[0], S[1], C[0], C[1], colorDashed, 2, true);

                var H_base = [cx - baseW/24, cy + pyH/3 - baseW/9];
                svgHtml += drawLine(S[0], S[1], H_base[0], H_base[1], '#C62828', 2, true);

                svgHtml += '<text x=\'' + ((A[0]+B[0])/2) + '\' y=\'' + (A[1]+20) + '\' fill=\'#1565C0\' font-weight=\'bold\' font-size=\'14\' text-anchor=\'middle\'>a = ' + formatOfflineNumber(a) + '</text>';
                svgHtml += '<text x=\'' + (S[0]+12) + '\' y=\'' + ((S[1]+H_base[1])/2) + '\' fill=\'#C62828\' font-weight=\'bold\' font-size=\'14\'>h = ' + formatOfflineNumber(h) + '</text>';

            } else if (shapeType === 'quad_pyramid') {
                var scale = 220 / Math.max(a, h);
                var baseW = a * scale;
                var pyH = h * scale;

                var A = [cx - baseW/2, cy + pyH/3];
                var B = [cx + baseW/6, cy + pyH/3];
                var C = [cx + baseW/2, cy + pyH/3 - baseW/3];
                var D = [cx - B[0] + A[0], cy + pyH/3 - baseW/3];
                var S = [cx, cy - pyH*2/3];

                svgHtml += drawPolygon([A[0], A[1], B[0], B[1], S[0], S[1]], '#E3F2FD', color3D, 2.5, 0.4);
                svgHtml += drawPolygon([B[0], B[1], C[0], C[1], S[0], S[1]], '#E3F2FD', color3D, 2.5, 0.4);

                svgHtml += drawLine(A[0], A[1], D[0], D[1], colorDashed, 2, true);
                svgHtml += drawLine(C[0], C[1], D[0], D[1], colorDashed, 2, true);
                svgHtml += drawLine(S[0], S[1], D[0], D[1], colorDashed, 2, true);

                var H_base = [cx, cy + pyH/3 - baseW/6];
                svgHtml += drawLine(S[0], S[1], H_base[0], H_base[1], '#C62828', 2, true);

                svgHtml += '<text x=\'' + ((A[0]+B[0])/2) + '\' y=\'' + (A[1]+20) + '\' fill=\'#1565C0\' font-weight=\'bold\' font-size=\'14\' text-anchor=\'middle\'>a = ' + formatOfflineNumber(a) + '</text>';
                svgHtml += '<text x=\'' + (S[0]+12) + '\' y=\'' + ((S[1]+H_base[1])/2) + '\' fill=\'#C62828\' font-weight=\'bold\' font-size=\'14\'>h = ' + formatOfflineNumber(h) + '</text>';

            } else if (shapeType === 'tri_prism') {
                var scale = 200 / Math.max(a, h);
                var baseW = a * scale;
                var prH = h * scale;

                var A = [cx - baseW/2, cy + prH/2];
                var B = [cx + baseW/2, cy + prH/2];
                var C = [cx - baseW/6, cy + prH/2 - baseW/3];

                var A1 = [A[0], A[1] - prH];
                var B1 = [B[0], B[1] - prH];
                var C1 = [C[0], C[1] - prH];

                svgHtml += drawPolygon([A[0], A[1], B[0], B[1], B1[0], B1[1], A1[0], A1[1]], '#E3F2FD', color3D, 2.5, 0.4);
                svgHtml += drawPolygon([B[0], B[1], C[0], C[1], C1[0], C1[1], B1[0], B1[1]], '#E3F2FD', color3D, 2.5, 0.3);
                svgHtml += drawPolygon([A1[0], A1[1], B1[0], B1[1], C1[0], C1[1]], '#BBDEFB', color3D, 2.5, 0.5);

                svgHtml += drawLine(A[0], A[1], C[0], C[1], colorDashed, 2, true);
                svgHtml += drawLine(B[0], B[1], C[0], C[1], colorDashed, 2, true);
                svgHtml += drawLine(C[0], C[1], C1[0], C1[1], colorDashed, 2, true);

                svgHtml += '<text x=\'' + ((A[0]+B[0])/2) + '\' y=\'' + (A[1]+20) + '\' fill=\'#1565C0\' font-weight=\'bold\' font-size=\'14\' text-anchor=\'middle\'>a = ' + formatOfflineNumber(a) + '</text>';
                svgHtml += '<text x=\'' + (A[0]-15) + '\' y=\'' + ((A[1]+A1[1])/2) + '\' fill=\'#1565C0\' font-weight=\'bold\' font-size=\'14\' text-anchor=\'end\'>h = ' + formatOfflineNumber(h) + '</text>';

            } else if (shapeType === 'cylinder') {
                var scale = 220 / Math.max(r*2, h);
                var rx = r * scale;
                var ry = rx * 0.25;
                var cyH = h * scale;

                var bottomY = cy + cyH/2;
                var topY = cy - cyH/2;

                svgHtml += '<rect x=\'' + (cx - rx) + '\' y=\'' + topY + '\' width=\'' + (rx*2) + '\' height=\'' + cyH + '\' fill=\'#E3F2FD\' fill-opacity=\'0.4\' />';
                
                svgHtml += drawEllipse(cx, bottomY, rx, ry, color3D, 2.5, '#E3F2FD', 0.4, true);
                svgHtml += drawEllipse(cx, topY, rx, ry, color3D, 2.5, '#BBDEFB', 0.5, false);

                svgHtml += drawLine(cx - rx, topY, cx - rx, bottomY, color3D, 2.5, false);
                svgHtml += drawLine(cx + rx, topY, cx + rx, bottomY, color3D, 2.5, false);

                svgHtml += drawLine(cx, topY, cx, bottomY, '#C62828', 1.5, true);
                svgHtml += drawLine(cx, bottomY, cx + rx, bottomY, '#1565C0', 2, true);

                svgHtml += '<text x=\'' + (cx + rx/2) + '\' y=\'' + (bottomY - 8) + '\' fill=\'#1565C0\' font-weight=\'bold\' font-size=\'14\' text-anchor=\'middle\'>r = ' + formatOfflineNumber(r) + '</text>';
                svgHtml += '<text x=\'' + (cx - 15) + '\' y=\'' + cy + '\' fill=\'#C62828\' font-weight=\'bold\' font-size=\'14\' text-anchor=\'end\'>h = ' + formatOfflineNumber(h) + '</text>';

            } else if (shapeType === 'cone') {
                var scale = 220 / Math.max(r*2, h);
                var rx = r * scale;
                var ry = rx * 0.25;
                var coH = h * scale;

                var bottomY = cy + coH/2;
                var apexY = cy - coH/2;

                svgHtml += '<polygon points=\'' + (cx - rx) + ',' + bottomY + ' ' + cx + ',' + apexY + ' ' + (cx + rx) + ',' + bottomY + '\' fill=\'#E3F2FD\' fill-opacity=\'0.4\' />';

                svgHtml += drawEllipse(cx, bottomY, rx, ry, color3D, 2.5, '#E3F2FD', 0.4, true);

                svgHtml += drawLine(cx - rx, bottomY, cx, apexY, color3D, 2.5, false);
                svgHtml += drawLine(cx + rx, bottomY, cx, apexY, color3D, 2.5, false);

                svgHtml += drawLine(cx, apexY, cx, bottomY, '#C62828', 2, true);
                svgHtml += drawLine(cx, bottomY, cx + rx, bottomY, '#1565C0', 2, true);

                svgHtml += '<text x=\'' + (cx + rx/2) + '\' y=\'' + (bottomY - 8) + '\' fill=\'#1565C0\' font-weight=\'bold\' font-size=\'14\' text-anchor=\'middle\'>r = ' + formatOfflineNumber(r) + '</text>';
                svgHtml += '<text x=\'' + (cx - 15) + '\' y=\'' + (cy + 10) + '\' fill=\'#C62828\' font-weight=\'bold\' font-size=\'14\' text-anchor=\'end\'>h = ' + formatOfflineNumber(h) + '</text>';

            } else if (shapeType === 'sphere') {
                var scale = 110;
                var rx = scale;
                var ry = scale * 0.25;

                svgHtml += '<circle cx=\'' + cx + '\' cy=\'' + cy + '\' r=\'' + rx + '\' stroke=\'' + color3D + '\' stroke-width=\'2.5\' fill=\'#E3F2FD\' fill-opacity=\'0.4\' />';
                svgHtml += drawEllipse(cx, cy, rx, ry, color3D, 1.5, 'none', 0, true);
                svgHtml += drawLine(cx, cy, cx + rx, cy, '#1565C0', 2.5, true);

                svgHtml += '<text x=\'' + (cx + rx/2) + '\' y=\'' + (cy - 8) + '\' fill=\'#1565C0\' font-weight=\'bold\' font-size=\'14\' text-anchor=\'middle\'>r = ' + formatOfflineNumber(r) + '</text>';
            }

            svg.innerHTML = svgHtml;
        }
    </script>
</body>
</html>";
            return htmlTemplate.Replace("[[SAFE_JS]]", safeJs);
        }

        private void LoadHtml(string html)
        {
            Loaded += async (_, _) =>
            {
                try
                {
                    await webView.EnsureCoreWebView2Async();
                    if (webView.CoreWebView2 != null && webView.CoreWebView2.Settings != null)
                    {
#if !DEBUG
                        webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                        webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
#else
                        webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
#endif
                    }
                    string finalHtml = html.Replace("[[WINDOW_TITLE]]", txtTitle.Text.Replace("'", "\\'"));
                    webView.CoreWebView2.NavigateToString(finalHtml);
                }
                catch (Exception ex)
                {
                    webView.Visibility = Visibility.Collapsed;
                    fallbackGrid.Visibility = Visibility.Visible;
                    Serilog.Log.Error("WebView2 environment initialization failed: {Err}", ex.Message);
                }
            };
        }

        private void DownloadWebView2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://go.microsoft.com/fwlink/p/?LinkId=2124703", // Link trực tiếp tải WebView2 Bootstrapper từ Microsoft
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Failed to open browser for WebView2 download: {Err}", ex.Message);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        // ═══ FULLSCREEN (F11) ═══
        private void Fullscreen_Click(object sender, RoutedEventArgs e) => ToggleFullscreen();

        private void ToggleFullscreen()
        {
            if (!_isFullscreen)
            {
                _prevStyle = WindowStyle;
                _prevState = WindowState;
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
                ResizeMode = ResizeMode.NoResize;
                btnFullscreen.Content = "⛶ Thoát toàn màn hình";
                _isFullscreen = true;
            }
            else
            {
                WindowStyle = _prevStyle;
                WindowState = _prevState;
                ResizeMode = ResizeMode.CanResize;
                btnFullscreen.Content = "⛶ Toàn màn hình";
                _isFullscreen = false;
            }
        }

        // ═══ SCREENSHOT → Clipboard ═══
        private void Screenshot_Click(object sender, RoutedEventArgs e) => CaptureToClipboard();

        private async void CaptureToClipboard()
        {
            if (_isCapturing) return;
            try
            {
                _isCapturing = true;

                if (webView.CoreWebView2 == null)
                {
                    MessageBox.Show("Môi trường hiển thị đồ thị chưa sẵn sàng để chụp ảnh.",
                        "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var ms = new MemoryStream())
                {
                    await webView.CoreWebView2.CapturePreviewAsync(
                        Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat.Png, ms);
                    ms.Position = 0;

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = ms;
                    bitmap.EndInit();
                    bitmap.Freeze();

                    Clipboard.SetImage(bitmap);
                }

                // Visual feedback
                var origContent = btnScreenshot.Content;
                btnScreenshot.Content = "✅ Đã copy!";
                var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
                timer.Tick += (_, _) => { btnScreenshot.Content = origContent; timer.Stop(); };
                timer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi chụp ảnh đồ thị: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isCapturing = false;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                if (webView != null)
                {
                    webView.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi giải phóng WebView2: {ex.Message}");
            }
            base.OnClosed(e);
        }

        // ═══ KEYBOARD SHORTCUTS ═══
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.F11)
            {
                ToggleFullscreen();
                e.Handled = true;
            }
            else if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                CaptureToClipboard();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape && _isFullscreen)
            {
                ToggleFullscreen();
                e.Handled = true;
            }
        }

        private async void btnMode2D_Click(object sender, RoutedEventArgs e)
        {
            _is3DMode = false;
            try
            {
                if (webView?.CoreWebView2 != null)
                {
                    await webView.CoreWebView2.ExecuteScriptAsync("show2DMode();");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to switch to 2D mode: {ex.Message}");
            }
        }

        private async void btnMode3D_Click(object sender, RoutedEventArgs e)
        {
            _is3DMode = true;
            try
            {
                if (webView?.CoreWebView2 != null)
                {
                    string escaped3DExpressions = _expressions3D != null 
                        ? System.Text.Json.JsonSerializer.Serialize(_expressions3D) 
                        : "null";
                    await webView.CoreWebView2.ExecuteScriptAsync($"show3DMode({escaped3DExpressions});");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to switch to 3D mode: {ex.Message}");
            }
        }
    }
}

