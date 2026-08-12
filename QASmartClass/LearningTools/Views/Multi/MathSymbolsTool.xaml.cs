using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QASmartClass.LearningTools.Controls;
using QASmartClass.LearningTools.Models;
using Microsoft.Data.Sqlite;

namespace QASmartClass.LearningTools.Views.Multi
{
    public partial class MathSymbolsTool : BaseToolControl
    {
        private SymbolEntry[] _loadedSymbols = Array.Empty<SymbolEntry>();
        private SymbolEntry? _activeSelectedSymbol = null;
        private System.Windows.Threading.DispatcherTimer? _copyStatusTimer;
        private static byte[]? _clickSoundBytes = null;
        private System.Media.SoundPlayer? _soundPlayer = null;
        private bool _isMuted = false;

        public MathSymbolsTool()
        {
            InitializeComponent();
            Loaded += MathSymbolsTool_Loaded;
            Unloaded += MathSymbolsTool_Unloaded;
        }

        private void MathSymbolsTool_Loaded(object sender, RoutedEventArgs e)
        {
            _loadedSymbols = LoadSymbolsFromDb();
            RenderSymbols(_loadedSymbols);
            if (_loadedSymbols.Length > 0)
            {
                SelectSymbol(_loadedSymbols[0]);
            }
            TouchTextPad.Attach(txtSymSearch, mode: "text");
            TouchTextPad.Attach(txtSandbox, mode: "text");
            LoadPracticalApps();

            bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
            if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Ghi chú" : "Study Guide & Notes";
            if (menuTextSymbols != null) menuTextSymbols.Text = isVN ? "Bảng ký hiệu" : "Symbol Table";
            if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Real-world Applications";

            if (sideMenu != null)
            {
                sideMenu.SelectedIndex = 0; // Default to symbol board
            }
        }

        private void MathSymbolsTool_Unloaded(object sender, RoutedEventArgs e)
        {
            Dispose();
        }

        // ═══════════════════════════════════════════════════════════
        //  SYMBOL DATA RECORD & DEFAULTS
        // ═══════════════════════════════════════════════════════════

        private record SymbolEntry(string Symbol, string Name, string Category, string LaTeX, string Description);

        private static readonly SymbolEntry[] DefaultSymbols =
        {
            // ── Arithmetic ──
            new("+" , "Phép cộng", "Phép tính", "+", "Phép toán cộng hai số hoặc hai biểu thức. Ví dụ: a + b."),
            new("−" , "Phép trừ", "Phép tính", "-", "Phép toán trừ hai số hoặc hai biểu thức. Ví dụ: a - b."),
            new("×" , "Phép nhân", "Phép tính", @"\times", "Phép toán nhân hai số hoặc hai biểu thức. Ví dụ: a × b."),
            new("÷" , "Phép chia", "Phép tính", @"\div", "Phép toán chia hai số hoặc hai biểu thức. Ví dụ: a ÷ b."),
            new("±" , "Cộng hoặc trừ", "Phép tính", @"\pm", "Biểu thị cả hai giá trị cộng hoặc trừ. Ví dụ: x = ±2."),
            new("∓" , "Trừ hoặc cộng", "Phép tính", @"\mp", "Biểu thị giá trị trừ hoặc cộng trái dấu với phép ±."),
            new("=" , "Bằng nhau", "Phép tính", "=", "Hai biểu thức có giá trị bằng nhau. Ví dụ: a = b."),
            new("≠" , "Khác nhau", "Phép tính", @"\neq", "Hai biểu thức có giá trị không bằng nhau. Ví dụ: a ≠ b."),
            new("≈" , "Xấp xỉ bằng", "Phép tính", @"\approx", "Giá trị gần đúng. Ví dụ: π ≈ 3.14."),
            new("≡" , "Đồng dư / Định nghĩa", "Phép tính", @"\equiv", "Phép đồng dư trong số học hoặc định nghĩa đồng nhất."),
            new("≤" , "Nhỏ hơn hoặc bằng", "Phép tính", @"\leq", "Biểu thức vế trái nhỏ hơn hoặc bằng vế phải. Ví dụ: a ≤ b."),
            new("≥" , "Lớn hơn hoặc bằng", "Phép tính", @"\geq", "Biểu thức vế trái lớn hơn hoặc bằng vế phải. Ví dụ: a ≥ b."),
            new("<" , "Nhỏ hơn", "Phép tính", "<", "Vế trái nhỏ hơn vế phải. Ví dụ: a < b."),
            new(">" , "Lớn hơn", "Phép tính", ">", "Vế trái lớn hơn vế phải. Ví dụ: a > b."),
            new("%" , "Phần trăm", "Phép tính", @"\%", "Biểu thị tỷ lệ trên 100. Ví dụ: 50% = 0.5."),
            new("‰" , "Phần nghìn", "Phép tính", @"\permil", "Biểu thị tỷ lệ trên 1000. Ví dụ: 5‰ = 0.005."),

            // ── Algebra ──
            new("|" , "Trị tuyệt đối", "Đại số", "|", "Giá trị không âm của số hoặc biểu thức. Ví dụ: |x|."),
            new("√" , "Căn bậc hai", "Đại số", @"\sqrt{}", "Tìm số có bình phương bằng số đã cho. Ví dụ: √4 = 2."),
            new("∛" , "Căn bậc ba", "Đại số", @"\sqrt[3]{}", "Tìm số có lập phương bằng số đã cho. Ví dụ: ∛8 = 2."),
            new("∜" , "Căn bậc bốn", "Đại số", @"\sqrt[4]{}", "Tìm số có lũy thừa bậc bốn bằng số đã cho. Ví dụ: ∜16 = 2."),
            new("∞" , "Vô cùng", "Đại số", @"\infty", "Biểu thị giá trị vô hạn, cực lớn hoặc cực nhỏ."),
            new("∝" , "Tỷ lệ thuận", "Đại số", @"\propto", "Hai đại lượng tỷ lệ thuận với nhau. Ví dụ: y ∝ x."),
            new("!" , "Giai thừa", "Đại số", "!", "Tích của các số nguyên dương từ 1 đến n. Ví dụ: 3! = 1 × 2 × 3 = 6."),
            new("∑" , "Phép tổng (Sigma)", "Đại số", @"\sum", "Tổng của một dãy số theo quy luật. Ví dụ: ∑(i = 1 đến n) của i."),
            new("∏" , "Phép tích (Pi)", "Đại số", @"\prod", "Tích của một dãy số theo quy luật. Ví dụ: ∏(i = 1 đến n) của i."),

            // ── Greek ──
            new("α" , "Alpha", "Hy Lạp", @"\alpha", "Ký hiệu góc hoặc hằng số phổ biến."),
            new("β" , "Beta", "Hy Lạp", @"\beta", "Ký hiệu góc hoặc hệ số."),
            new("γ" , "Gamma", "Hy Lạp", @"\gamma", "Ký hiệu góc hoặc hàm số Gamma."),
            new("δ" , "Delta (nhỏ)", "Hy Lạp", @"\delta", "Biểu thị lượng thay đổi cực nhỏ."),
            new("Δ" , "Delta (lớn)", "Hy Lạp", @"\Delta", "Biểu thị biệt thức phương trình bậc hai hoặc lượng thay đổi."),
            new("ε" , "Epsilon", "Hy Lạp", @"\epsilon", "Số dương cực kỳ nhỏ gần bằng 0."),
            new("θ" , "Theta", "Hy Lạp", @"\theta", "Góc trong hình học và lượng giác."),
            new("λ" , "Lambda", "Hy Lạp", @"\lambda", "Biểu thị bước sóng hoặc trị riêng trong ma trận."),
            new("μ" , "Mu", "Hy Lạp", @"\mu", "Tiền tố micro (10^-6) hoặc kỳ vọng toán học."),
            new("π" , "Số Pi", "Hy Lạp", @"\pi", "Hằng số tỷ lệ chu vi và đường kính đường tròn (~3.14159)."),
            new("ρ" , "Rho", "Hy Lạp", @"\rho", "Ký hiệu mật độ hoặc hệ số tương quan."),
            new("σ" , "Sigma (nhỏ)", "Hy Lạp", @"\sigma", "Độ lệch chuẩn trong xác suất thống kê."),
            new("Σ" , "Sigma (lớn)", "Hy Lạp", @"\Sigma", "Ký hiệu phép tổng hoặc không gian mẫu."),
            new("τ" , "Tau", "Hy Lạp", @"\tau", "Hằng số bằng 2*Pi (~6.28) hoặc thời gian hằng số."),
            new("φ" , "Phi (nhỏ)", "Hy Lạp", @"\varphi", "Tỷ lệ vàng (~1.618) hoặc hàm số góc."),
            new("ψ" , "Psi", "Hy Lạp", @"\psi", "Ký hiệu hàm sóng hoặc hàm số đặc biệt."),
            new("ω" , "Omega", "Hy Lạp", @"\omega", "Tần số góc trong vật lý hoặc không gian mẫu."),
            new("Ω" , "Omega (lớn)", "Hy Lạp", @"\Omega", "Ký hiệu điện trở Ohm hoặc không gian biến cố."),

            // ── Set Theory ──
            new("∈" , "Thuộc tập hợp", "Tập hợp", @"\in", "Phần tử thuộc về tập hợp. Ví dụ: 1 ∈ ℕ."),
            new("∉" , "Không thuộc tập hợp", "Tập hợp", @"\notin", "Phần tử không thuộc về tập hợp. Ví dụ: -1 ∉ ℕ."),
            new("⊂" , "Tập hợp con thực sự", "Tập hợp", @"\subset", "Tập hợp A nằm hoàn toàn trong tập B nhưng khác B."),
            new("⊃" , "Tập hợp chứa", "Tập hợp", @"\supset", "Tập hợp A chứa hoàn toàn tập hợp B."),
            new("⊆" , "Tập hợp con hoặc bằng", "Tập hợp", @"\subseteq", "Mọi phần tử của A đều là phần tử của B. Ví dụ: A ⊆ B."),
            new("∩" , "Phép giao tập hợp", "Tập hợp", @"\cap", "Tập các phần tử chung của cả hai tập hợp. Ví dụ: A ∩ B."),
            new("∪" , "Phép hợp tập hợp", "Tập hợp", @"\cup", "Tập hợp gồm tất cả phần tử thuộc ít nhất một trong hai tập hợp."),
            new("∅" , "Tập hợp rỗng", "Tập hợp", @"\emptyset", "Tập hợp không chứa phần tử nào."),
            new("ℕ" , "Tập số tự nhiên", "Tập hợp", @"\mathbb{N}", "Tập hợp các số 0, 1, 2, 3, ..."),
            new("ℤ" , "Tập số nguyên", "Tập hợp", @"\mathbb{Z}", "Tập hợp các số nguyên gồm số âm, số 0 và số dương."),
            new("ℚ" , "Tập số hữu tỉ", "Tập hợp", @"\mathbb{Q}", "Tập số viết được dưới dạng phân số a/b. Ví dụ: 1.5 ∈ ℚ."),
            new("ℝ" , "Tập số thực", "Tập hợp", @"\mathbb{R}", "Tập hợp bao gồm tất cả các số hữu tỉ và vô tỉ."),
            new("ℂ" , "Tập số phức", "Tập hợp", @"\mathbb{C}", "Tập hợp các số có dạng a + bi. Ví dụ: z ∈ ℂ."),

            // ── Logic ──
            new("∧" , "Phép hội (Và)", "Logic", @"\land", "Mệnh đề A ∧ B đúng khi cả A và B đều đúng."),
            new("∨" , "Phép tuyển (Hoặc)", "Logic", @"\lor", "Mệnh đề A ∨ B đúng khi có ít nhất A hoặc B đúng."),
            new("¬" , "Phép phủ định (Không)", "Logic", @"\neg", "Mệnh đề ¬A đúng khi mệnh đề A sai."),
            new("⇒" , "Mệnh đề kéo theo (Suy ra)", "Logic", @"\Rightarrow", "Nếu A đúng thì B đúng. Ví dụ: A ⇒ B."),
            new("⇔" , "Mệnh đề tương đương", "Logic", @"\Leftrightarrow", "A và B cùng đúng hoặc cùng sai. Ví dụ: A ⇔ B."),
            new("∀" , "Với mọi", "Logic", @"\forall", "Mệnh đề đúng cho tất cả các phần tử. Ví dụ: ∀x ∈ ℝ."),
            new("∃" , "Tồn tại", "Logic", @"\exists", "Có ít nhất một phần tử thỏa mãn. Ví dụ: ∃x ∈ ℝ."),
            new("∄" , "Không tồn tại", "Logic", @"\nexists", "Không có phần tử nào thỏa mãn mệnh đề."),
            new("⊢" , "Chứng minh được", "Logic", @"\vdash", "Mệnh đề vế phải được suy diễn logic từ vế trái."),
            new("⊨" , "Thỏa mãn ngữ nghĩa", "Logic", @"\models", "Mô hình vế trái thỏa mãn công thức vế phải."),

            // ── Calculus ──
            new("∫" , "Tích phân", "Giải tích", @"\int", "Tính tổng vô hạn của các lượng cực nhỏ để tìm diện tích, thể tích."),
            new("∬" , "Tích phân kép", "Giải tích", @"\iint", "Tích phân trên miền hai chiều."),
            new("∭" , "Tích phân bội ba", "Giải tích", @"\iiint", "Tích phân trên miền ba chiều."),
            new("∂" , "Đạo hàm riêng", "Giải tích", @"\partial", "Đạo hàm của hàm nhiều biến theo một biến duy nhất."),
            new("∇" , "Nabla (Gradient)", "Giải tích", @"\nabla", "Toán tử vi phân vector."),
            new("lim", "Giới hạn (Limit)", "Giải tích", @"\lim", "Giá trị mà hàm số hoặc dãy số tiến tới."),
            new("dx" , "Vi phân của x", "Giải tích", "dx", "Biểu thị lượng thay đổi vô cùng nhỏ của biến x."),

            // ── Geometry ──
            new("∠" , "Góc", "Hình học", @"\angle", "Biểu thị góc giữa hai đường thẳng. Ví dụ: ∠ABC = 90°."),
            new("⊥" , "Vuông góc", "Hình học", @"\perp", "Hai đường thẳng cắt nhau tạo thành góc 90 độ. Ví dụ: a ⊥ b."),
            new("∥" , "Song song", "Hình học", @"\parallel", "Hai đường thẳng không bao giờ cắt nhau. Ví dụ: a ∥ b."),
            new("△" , "Tam giác", "Hình học", @"\triangle", "Hình phẳng gồm ba đỉnh và ba cạnh. Ví dụ: △ABC."),
            new("□" , "Hình vuông", "Hình học", @"\square", "Tứ giác có 4 cạnh bằng nhau và 4 góc vuông."),
            new("⊙" , "Đường tròn", "Hình học", @"\odot", "Tập hợp các điểm cách đều tâm. Ví dụ: ⊙ O."),
            new("≅" , "Bằng nhau hình học (Congruent)", "Hình học", @"\cong", "Hai hình có kích thước và hình dạng giống hệt nhau."),
            new("∼" , "Đồng dạng", "Hình học", @"\sim", "Hai hình có hình dạng giống nhau nhưng tỷ lệ kích thước khác nhau."),
            new("°" , "Độ (Đo góc)", "Hình học", @"^\circ", "Đơn vị đo góc phẳng. Ví dụ: 90°."),

            // ── Arrows ──
            new("→" , "Mũi tên phải", "Mũi tên", @"\rightarrow", "Chỉ hướng biến thiên hoặc giới hạn. Ví dụ: x → ∞."),
            new("←" , "Mũi tên trái", "Mũi tên", @"\leftarrow", "Chỉ hướng liên kết ngược."),
            new("↑" , "Mũi tên lên", "Mũi tên", @"\uparrow", "Chỉ hướng tăng hoặc dịch chuyển lên."),
            new("↓" , "Mũi tên xuống", "Mũi tên", @"\downarrow", "Chỉ hướng giảm hoặc dịch chuyển xuống."),
            new("↔" , "Mũi tên hai chiều", "Mũi tên", @"\leftrightarrow", "Biểu thị sự tương tác hai chiều."),
            new("⟹" , "Suy ra (lớn)", "Mũi tên", @"\Longrightarrow", "Mũi tên suy ra lớn."),
            new("⟺" , "Tương đương (lớn)", "Mũi tên", @"\Longleftrightarrow", "Mũi tên tương đương lớn."),

            // ── Superscript / Subscript ──
            new("⁰" , "Mũ 0", "Chỉ số", "^0", "Lũy thừa bậc 0. Ví dụ: x^0 = 1."),
            new("¹" , "Mũ 1", "Chỉ số", "^1", "Lũy thừa bậc 1. Ví dụ: x^1 = x."),
            new("²" , "Mũ 2 (Bình phương)", "Chỉ số", "^2", "Lũy thừa bậc hai. Ví dụ: x^2."),
            new("³" , "Mũ 3 (Lập phương)", "Chỉ số", "^3", "Lũy thừa bậc ba. Ví dụ: x^3."),
            new("⁴" , "Mũ 4", "Chỉ số", "^4", "Lũy thừa bậc bốn. Ví dụ: x^4."),
            new("ⁿ" , "Mũ n", "Chỉ số", "^n", "Lũy thừa tổng quát bậc n. Ví dụ: x^n."),
            new("₀" , "Chỉ số dưới 0", "Chỉ số", "_0", "Dùng cho giá trị khởi đầu hoặc hằng số. Ví dụ: x_0."),
            new("₁" , "Chỉ số dưới 1", "Chỉ số", "_1", "Chỉ số thứ tự thứ nhất. Ví dụ: x_1."),
            new("₂" , "Chỉ số dưới 2", "Chỉ số", "_2", "Chỉ số thứ tự thứ hai. Ví dụ: x_2."),
            new("₃" , "Chỉ số dưới 3", "Chỉ số", "_3", "Chỉ số thứ tự thứ ba. Ví dụ: x_3."),
        };

        // ═══════════════════════════════════════════════════════════
        //  DATABASE INTEGRATION
        // ═══════════════════════════════════════════════════════════

        private SymbolEntry[] LoadSymbolsFromDb()
        {
            var list = new List<SymbolEntry>();
            var dbPath = QASmartClass.Services.AppPaths.DatabaseFile;

            try
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dbPath)!);

                var keyBytes = QASmartClass.Data.DbEncryptionKeyManager.GetOrInitializeKey();
                var hexKey = Convert.ToHexString(keyBytes);
                using (var connection = new SqliteConnection($"Data Source={dbPath};Password={hexKey};Default Timeout=5;"))
                {
                    connection.Open();
                    using (var pragmaCmd = connection.CreateCommand())
                    {
                        pragmaCmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;";
                        pragmaCmd.ExecuteNonQuery();
                    }

                    // Create table if not exists
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = @"
                            CREATE TABLE IF NOT EXISTS LearningToolMathSymbols (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                Symbol TEXT NOT NULL,
                                Name TEXT NOT NULL,
                                Category TEXT NOT NULL,
                                LaTeX TEXT NOT NULL,
                                Description TEXT NOT NULL
                            );";
                        cmd.ExecuteNonQuery();
                    }

                    // Check if update is needed (e.g. if the first record contains old LaTeX raw code in description)
                    bool needReseed = true;
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT Description FROM LearningToolMathSymbols LIMIT 1;";
                        var desc = cmd.ExecuteScalar() as string;
                        if (desc != null && !desc.Contains("\\times") && !desc.Contains("\\pm"))
                        {
                            // If descriptions are clean and size matches, no need to reseed
                            using (var countCmd = connection.CreateCommand())
                            {
                                countCmd.CommandText = "SELECT COUNT(*) FROM LearningToolMathSymbols;";
                                var countVal = countCmd.ExecuteScalar();
                                if (countVal != null && Convert.ToInt32(countVal) == DefaultSymbols.Length)
                                {
                                    needReseed = false;
                                }
                            }
                        }
                    }

                    // Re-seed if needed
                    if (needReseed)
                    {
                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.CommandText = "DELETE FROM LearningToolMathSymbols;";
                            cmd.ExecuteNonQuery();
                        }

                        using (var transaction = connection.BeginTransaction())
                        {
                            foreach (var sym in DefaultSymbols)
                            {
                                using (var cmd = connection.CreateCommand())
                                {
                                    cmd.CommandText = @"
                                        INSERT INTO LearningToolMathSymbols (Symbol, Name, Category, LaTeX, Description)
                                        VALUES ($Symbol, $Name, $Category, $LaTeX, $Description);";
                                    cmd.Parameters.AddWithValue("$Symbol", sym.Symbol);
                                    cmd.Parameters.AddWithValue("$Name", sym.Name);
                                    cmd.Parameters.AddWithValue("$Category", sym.Category);
                                    cmd.Parameters.AddWithValue("$LaTeX", sym.LaTeX);
                                    cmd.Parameters.AddWithValue("$Description", sym.Description);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            transaction.Commit();
                        }
                    }

                    // Read
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT Symbol, Name, Category, LaTeX, Description FROM LearningToolMathSymbols;";
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(new SymbolEntry(
                                    reader.GetString(0),
                                    reader.GetString(1),
                                    reader.GetString(2),
                                    reader.GetString(3),
                                    reader.GetString(4)
                                ));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("SQLite load error for MathSymbols, fallback: " + ex.Message);
                return DefaultSymbols;
            }

            return list.Count > 0 ? list.ToArray() : DefaultSymbols;
        }

        // ═══════════════════════════════════════════════════════════
        //  SEARCH (Vietnamese Accent Insensitive)
        // ═══════════════════════════════════════════════════════════

        private static string RemoveSign(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            string[] arr1 = new string[] { "á", "à", "ả", "ã", "ạ", "â", "ấ", "ầ", "ẩ", "ẫ", "ậ", "ă", "ắ", "ằ", "ẳ", "ẵ", "ặ",
                "đ", "é", "è", "ẻ", "ẽ", "ẹ", "ê", "ế", "ề", "ể", "ễ", "ệ", "í", "ì", "ỉ", "ĩ", "ị", "ó", "ò", "ỏ", "õ", "ọ", "ô", "ố", "ồ", "ổ", "ỗ", "ộ", "ơ", "ớ", "ờ", "ở", "ỡ", "ợ",
                "ú", "ù", "ủ", "ũ", "ụ", "ư", "ứ", "ừ", "ử", "ữ", "ự", "ý", "ỳ", "ỷ", "ỹ", "ỵ",
                "Á", "À", "Ả", "Ã", "Ạ", "Â", "Ấ", "Ầ", "Ẩ", "Ẫ", "Ậ", "Ă", "Ắ", "Ằ", "Ẳ", "Ẵ", "Ặ",
                "Đ", "É", "È", "Ẻ", "Ẽ", "Ẹ", "Ê", "Ế", "Ề", "Ể", "Ễ", "Ệ", "Í", "Ì", "Ỉ", "Ĩ", "Ị", "Ó", "Ò", "Ỏ", "Õ", "Ọ", "Ô", "Ố", "Ồ", "Ổ", "Ỗ", "Ộ", "Ơ", "Ớ", "Ờ", "Ở", "Ỡ", "Ợ",
                "Ú", "Ù", "Ủ", "Ũ", "Ụ", "Ư", "Ứ", "Ừ", "Ử", "Ữ", "Ự", "Ý", "Ỳ", "Ỷ", "Ỹ", "Ý" };

            string[] arr2 = new string[] { "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a",
                "d", "e", "e", "e", "e", "e", "e", "e", "e", "e", "e", "e", "i", "i", "i", "i", "i", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o",
                "u", "u", "u", "u", "u", "u", "u", "u", "u", "u", "u", "y", "y", "y", "y", "y",
                "A", "A", "A", "A", "A", "A", "A", "A", "A", "A", "A", "A", "A", "A", "A", "A", "A",
                "D", "E", "E", "E", "E", "E", "E", "E", "E", "E", "E", "E", "I", "I", "I", "I", "I", "O", "O", "O", "O", "O", "O", "O", "O", "O", "O", "O", "O", "O", "O", "O", "O", "O",
                "U", "U", "U", "U", "U", "U", "U", "U", "U", "U", "U", "Y", "Y", "Y", "Y", "Y" };

            for (int i = 0; i < arr1.Length; i++)
            {
                text = text.Replace(arr1[i], arr2[i]);
            }
            return text;
        }

        private void SymSearch_Changed(object sender, TextChangedEventArgs e)
        {
            ApplyCombinedFilter();
        }

        private void CategoryTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyCombinedFilter();
        }

        private void ApplyCombinedFilter()
        {
            if (txtSymSearch == null || lstCategoryTabs == null) return;
            string q = txtSymSearch.Text.Trim();
            string normQuery = RemoveSign(q.ToLowerInvariant());

            var selectedItem = lstCategoryTabs.SelectedItem as ListBoxItem;
            string selectedCat = selectedItem?.Content?.ToString() ?? "Tất cả";

            var filtered = _loadedSymbols.Where(s =>
            {
                bool matchesTab = (selectedCat == "Tất cả") || (s.Category == selectedCat);
                bool matchesQuery = string.IsNullOrEmpty(q) ||
                                    RemoveSign(s.Symbol.ToLowerInvariant()).Contains(normQuery) ||
                                    RemoveSign(s.Name.ToLowerInvariant()).Contains(normQuery) ||
                                    RemoveSign(s.Category.ToLowerInvariant()).Contains(normQuery) ||
                                    RemoveSign(s.LaTeX.ToLowerInvariant()).Contains(normQuery);
                return matchesTab && matchesQuery;
            }).ToArray();

            RenderSymbols(filtered);
        }

        // ═══════════════════════════════════════════════════════════
        //  RENDER SYSTEM
        // ═══════════════════════════════════════════════════════════

        private static readonly Dictionary<string, string> CategoryColors = new()
        {
            { "Phép tính", "#0D253F" }, { "Đại số", "#1B132C" },
            { "Hy Lạp", "#0F281E" }, { "Tập hợp", "#261707" },
            { "Logic", "#2A0E1A" }, { "Giải tích", "#052627" },
            { "Hình học", "#111428" }, { "Mũi tên", "#2B1611" },
            { "Chỉ số", "#1C1C1F" },
        };

        private void RenderSymbols(SymbolEntry[] symbols)
        {
            if (symbolsPanel == null) return;
            symbolsPanel.Children.Clear();

            var groups = symbols.GroupBy(s => s.Category);
            foreach (var group in groups)
            {
                string bgHex = CategoryColors.GetValueOrDefault(group.Key, "#1C1C1F");
                var bgColor = (Color)ColorConverter.ConvertFromString(bgHex);

                // Category header
                symbolsPanel.Children.Add(new TextBlock
                {
                    Text = $"📂 {group.Key} ({group.Count()})",
                    FontSize = 14, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(165, 180, 252)),
                    Margin = new Thickness(4, 16, 0, 8),
                    FontFamily = Application.Current?.TryFindResource("InterFont") as FontFamily ?? new FontFamily("Segoe UI")
                });

                var wrap = new WrapPanel();
                foreach (var sym in group)
                {
                    var cell = new Border
                    {
                        Width = 125, Height = 95,
                        Background = new SolidColorBrush(bgColor),
                        CornerRadius = new CornerRadius(8),
                        Margin = new Thickness(4),
                        Cursor = Cursors.Hand,
                        ToolTip = $"{sym.Name}\nLaTeX: {sym.LaTeX}\nClick để xem chi tiết & copy",
                        BorderBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                        BorderThickness = new Thickness(0.5)
                    };

                    var sp = new StackPanel
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(4)
                    };
                    
                    sp.Children.Add(new TextBlock
                    {
                        Text = sym.Symbol, FontSize = 26, FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        FontFamily = new FontFamily("Segoe UI Symbol"),
                        Foreground = Brushes.White
                    });
                    
                    sp.Children.Add(new TextBlock
                    {
                        Text = sym.Name, FontSize = 11.5,
                        Foreground = new SolidColorBrush(Color.FromRgb(170, 175, 200)),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center,
                        MaxWidth = 115,
                        Margin = new Thickness(0, 4, 0, 0)
                    });
                    
                    cell.Child = sp;

                    cell.MouseLeftButtonDown += (_, _) => SelectSymbol(sym);

                    wrap.Children.Add(cell);
                }
                symbolsPanel.Children.Add(wrap);
            }

            symbolsPanel.Children.Add(new TextBlock
            {
                Text = $"Tổng số: {symbols.Length} ký hiệu",
                FontSize = 11, Foreground = Brushes.Gray,
                Margin = new Thickness(4, 12, 0, 12),
                FontStyle = FontStyles.Italic,
                FontFamily = Application.Current?.TryFindResource("InterFont") as FontFamily ?? new FontFamily("Segoe UI")
            });
        }

        // ═══════════════════════════════════════════════════════════
        //  SELECTION & COPY OPERATIONS
        // ═══════════════════════════════════════════════════════════

        private void SelectSymbol(SymbolEntry sym)
        {
            _activeSelectedSymbol = sym;
            txtDetailSymbol.Text = sym.Symbol;
            txtDetailName.Text = sym.Name;
            txtDetailCategory.Text = $"📂 {sym.Category}";
            txtDetailLaTeX.Text = sym.LaTeX;
            txtDetailDescription.Text = sym.Description;
            txtCopyStatus.Text = "";

            PlayClickSound();
            UpdateLaTeXPreview(sym.LaTeX);
        }

        private void BtnCopySymbol_Click(object sender, RoutedEventArgs e)
        {
            if (_activeSelectedSymbol == null) return;
            PlayClickSound();
            try
            {
                Clipboard.SetText(_activeSelectedSymbol.Symbol);
                ShowCopyStatus("Đã copy ký hiệu vào bộ nhớ tạm!");
            }
            catch (Exception ex)
            {
                ShowCopyStatus("Lỗi: " + ex.Message);
            }
        }

        private void BtnCopyLaTeX_Click(object sender, RoutedEventArgs e)
        {
            if (_activeSelectedSymbol == null) return;
            PlayClickSound();
            try
            {
                Clipboard.SetText(_activeSelectedSymbol.LaTeX);
                ShowCopyStatus("Đã copy mã LaTeX vào bộ nhớ tạm!");
            }
            catch (Exception ex)
            {
                ShowCopyStatus("Lỗi: " + ex.Message);
            }
        }

        private void ShowCopyStatus(string message)
        {
            if (txtCopyStatus == null) return;
            txtCopyStatus.Text = message;
            _copyStatusTimer?.Stop();
            _copyStatusTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1.5)
            };
            _copyStatusTimer.Tick += (s, e) =>
            {
                txtCopyStatus.Text = "";
                _copyStatusTimer?.Stop();
            };
            _copyStatusTimer.Start();
        }

        // ═══════════════════════════════════════════════════════════
        //  AUDIO HAPTIC FEEDBACK (Synthesized)
        // ═══════════════════════════════════════════════════════════

        private void BtnMute_Click(object sender, RoutedEventArgs e)
        {
            _isMuted = btnMute.IsChecked == true;
            btnMute.Content = _isMuted ? "🔇" : "🔊";
            PlayClickSound();
        }

        private void PlayClickSound()
        {
            if (_isMuted) return;
            try
            {
                if (_clickSoundBytes == null)
                {
                    _clickSoundBytes = CreateClickWavBytes();
                }
                if (_soundPlayer == null && _clickSoundBytes != null)
                {
                    var ms = new System.IO.MemoryStream(_clickSoundBytes);
                    _soundPlayer = new System.Media.SoundPlayer(ms);
                    _soundPlayer.Load();
                }
                System.Threading.Tasks.Task.Run(() =>
                {
                    try { _soundPlayer?.Play(); } catch { }
                });
            }
            catch
            {
                try { System.Media.SystemSounds.Asterisk.Play(); } catch { }
            }
        }

        private static byte[] CreateClickWavBytes()
        {
            int sampleRate = 11025;
            short bitsPerSample = 16;
            short channels = 1;
            double duration = 0.025; // 25ms click
            int numSamples = (int)(sampleRate * duration);
            int dataSize = numSamples * channels * (bitsPerSample / 8);
            int chunkSize = 36 + dataSize;

            var stream = new System.IO.MemoryStream();
            using (var writer = new System.IO.BinaryWriter(stream))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(chunkSize);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

                writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16);
                writer.Write((short)1); // PCM
                writer.Write(channels);
                writer.Write(sampleRate);
                writer.Write(sampleRate * channels * (bitsPerSample / 8));
                writer.Write((short)(channels * (bitsPerSample / 8)));
                writer.Write(bitsPerSample);

                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                writer.Write(dataSize);

                double frequency = 1000.0;
                for (int i = 0; i < numSamples; i++)
                {
                    double t = (double)i / sampleRate;
                    double envelope = System.Math.Exp(-120.0 * t);
                    double angle = 2.0 * System.Math.PI * frequency * t;
                    short sample = (short)(System.Math.Sin(angle) * 6000.0 * envelope);
                    writer.Write(sample);
                }
            }
            return stream.ToArray();
        }

        // ═══════════════════════════════════════════════════════════
        //  EQUATION SANDBOX (Khay Nháp Công Thức)
        // ═══════════════════════════════════════════════════════════

        private void BtnAddToSandbox_Click(object sender, RoutedEventArgs e)
        {
            if (_activeSelectedSymbol == null) return;
            PlayClickSound();
            if (txtSandbox == null) return;

            int caretIndex = txtSandbox.CaretIndex;
            txtSandbox.Text = txtSandbox.Text.Insert(caretIndex, _activeSelectedSymbol.Symbol);
            txtSandbox.CaretIndex = caretIndex + _activeSelectedSymbol.Symbol.Length;
            txtSandbox.Focus();
        }

        private void BtnClearSandbox_Click(object sender, RoutedEventArgs e)
        {
            PlayClickSound();
            if (txtSandbox != null)
            {
                txtSandbox.Text = "";
            }
        }

        private void BtnCopySandbox_Click(object sender, RoutedEventArgs e)
        {
            PlayClickSound();
            if (txtSandbox == null || string.IsNullOrEmpty(txtSandbox.Text)) return;
            try
            {
                Clipboard.SetText(txtSandbox.Text);
                ShowCopyStatus("Đã copy công thức nháp!");
            }
            catch (Exception ex)
            {
                ShowCopyStatus("Lỗi: " + ex.Message);
            }
        }

        private void BtnCopySandboxLaTeX_Click(object sender, RoutedEventArgs e)
        {
            PlayClickSound();
            if (txtSandbox == null || string.IsNullOrEmpty(txtSandbox.Text)) return;
            try
            {
                string latex = GetLaTeXFromUnicodeText(txtSandbox.Text);
                Clipboard.SetText(latex);
                ShowCopyStatus("Đã copy LaTeX nháp!");
            }
            catch (Exception ex)
            {
                ShowCopyStatus("Lỗi: " + ex.Message);
            }
        }

        private void Sandbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateSandboxPreview();
        }

        private void UpdateSandboxPreview()
        {
            if (txtSandbox == null || sandboxPreviewCtrl == null) return;
            string text = txtSandbox.Text;
            if (string.IsNullOrEmpty(text))
            {
                sandboxPreviewCtrl.Content = null;
                return;
            }
            string latex = GetLaTeXFromUnicodeText(text);
            UpdateLaTeXControlPreview(latex, sandboxPreviewCtrl);
        }

        private string GetLaTeXFromUnicodeText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var result = new System.Text.StringBuilder();
            int i = 0;
            var sortedSymbols = _loadedSymbols.OrderByDescending(s => s.Symbol.Length).ToArray();

            while (i < text.Length)
            {
                bool matched = false;
                foreach (var sym in sortedSymbols)
                {
                    if (i + sym.Symbol.Length <= text.Length && 
                        text.Substring(i, sym.Symbol.Length) == sym.Symbol)
                    {
                        result.Append(sym.LaTeX).Append(" ");
                        i += sym.Symbol.Length;
                        matched = true;
                        break;
                    }
                }
                if (!matched)
                {
                    result.Append(text[i]);
                    i++;
                }
            }
            return result.ToString().Trim();
        }

        // ═══════════════════════════════════════════════════════════
        //  LATEX PREVIEW PARSER
        // ═══════════════════════════════════════════════════════════

        private void UpdateLaTeXControlPreview(string latex, ContentControl targetCtrl)
        {
            if (targetCtrl == null) return;
            
            var wp = new WrapPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Tiền xử lý ký hiệu độ (degree) để vẽ tự nhiên
            latex = latex.Replace(@"^\circ", "°").Replace(@"\circ", "°");

            try
            {
                int i = 0;
                while (i < latex.Length)
                {
                    // Xử lý ca đặc biệt: \% -> vẽ dấu %
                    if (latex[i] == '\\' && i + 1 < latex.Length && latex[i + 1] == '%')
                    {
                        AddText(wp, "%", 24);
                        i += 2;
                        continue;
                    }

                    if (latex[i] == '\\')
                    {
                        int start = i + 1;
                        while (i + 1 < latex.Length && char.IsLetter(latex[i + 1]))
                        {
                            i++;
                        }
                        string cmd = latex.Substring(start, i - start + 1);
                        i++;

                        if (cmd == "alpha") AddText(wp, "α", 24);
                        else if (cmd == "beta") AddText(wp, "β", 24);
                        else if (cmd == "gamma") AddText(wp, "γ", 24);
                        else if (cmd == "delta") AddText(wp, "δ", 24);
                        else if (cmd == "Delta") AddText(wp, "Δ", 24);
                        else if (cmd == "epsilon") AddText(wp, "ε", 24);
                        else if (cmd == "theta") AddText(wp, "θ", 24);
                        else if (cmd == "lambda") AddText(wp, "λ", 24);
                        else if (cmd == "mu") AddText(wp, "μ", 24);
                        else if (cmd == "pi") AddText(wp, "π", 24);
                        else if (cmd == "rho") AddText(wp, "ρ", 24);
                        else if (cmd == "sigma") AddText(wp, "σ", 24);
                        else if (cmd == "Sigma") AddText(wp, "Σ", 24);
                        else if (cmd == "tau") AddText(wp, "τ", 24);
                        else if (cmd == "varphi") AddText(wp, "φ", 24);
                        else if (cmd == "psi") AddText(wp, "ψ", 24);
                        else if (cmd == "omega") AddText(wp, "ω", 24);
                        else if (cmd == "Omega") AddText(wp, "Ω", 24);
                        else if (cmd == "times") AddText(wp, "×", 24);
                        else if (cmd == "div") AddText(wp, "÷", 24);
                        else if (cmd == "pm") AddText(wp, "±", 24);
                        else if (cmd == "mp") AddText(wp, "∓", 24);
                        else if (cmd == "neq") AddText(wp, "≠", 24);
                        else if (cmd == "approx") AddText(wp, "≈", 24);
                        else if (cmd == "equiv") AddText(wp, "≡", 24);
                        else if (cmd == "leq") AddText(wp, "≤", 24);
                        else if (cmd == "geq") AddText(wp, "≥", 24);
                        
                        // Bổ sung các lệnh bị thiếu
                        else if (cmd == "sum") AddText(wp, "∑", 28);
                        else if (cmd == "prod") AddText(wp, "∏", 28);
                        else if (cmd == "propto") AddText(wp, "∝", 24);
                        else if (cmd == "permil") AddText(wp, "‰", 24);

                        else if (cmd == "in") AddText(wp, "∈", 24);
                        else if (cmd == "notin") AddText(wp, "∉", 24);
                        else if (cmd == "subset") AddText(wp, "⊂", 24);
                        else if (cmd == "supset") AddText(wp, "⊃", 24);
                        else if (cmd == "subseteq") AddText(wp, "⊆", 24);
                        else if (cmd == "cap") AddText(wp, "∩", 24);
                        else if (cmd == "cup") AddText(wp, "∪", 24);
                        else if (cmd == "emptyset") AddText(wp, "∅", 24);
                        else if (cmd == "mathbb")
                        {
                            if (i < latex.Length && latex[i] == '{')
                            {
                                i++;
                                if (i < latex.Length)
                                {
                                    char letter = latex[i];
                                    if (letter == 'N') AddText(wp, "ℕ", 24);
                                    else if (letter == 'Z') AddText(wp, "ℤ", 24);
                                    else if (letter == 'Q') AddText(wp, "ℚ", 24);
                                    else if (letter == 'R') AddText(wp, "ℝ", 24);
                                    else if (letter == 'C') AddText(wp, "ℂ", 24);
                                    else AddText(wp, letter.ToString(), 24);
                                    i++;
                                }
                                if (i < latex.Length && latex[i] == '}') i++;
                            }
                        }
                        else if (cmd == "land") AddText(wp, "∧", 24);
                        else if (cmd == "lor") AddText(wp, "∨", 24);
                        else if (cmd == "neg") AddText(wp, "¬", 24);
                        else if (cmd == "Rightarrow") AddText(wp, "⇒", 24);
                        else if (cmd == "Leftrightarrow") AddText(wp, "⇔", 24);
                        else if (cmd == "forall") AddText(wp, "∀", 24);
                        else if (cmd == "exists") AddText(wp, "∃", 24);
                        else if (cmd == "nexists") AddText(wp, "∄", 24);
                        else if (cmd == "vdash") AddText(wp, "⊢", 24);
                        else if (cmd == "models") AddText(wp, "⊨", 24);
                        else if (cmd == "int") AddText(wp, "∫", 28);
                        else if (cmd == "iint") AddText(wp, "∬", 28);
                        else if (cmd == "iiint") AddText(wp, "∭", 28);
                        else if (cmd == "partial") AddText(wp, "∂", 24);
                        else if (cmd == "nabla") AddText(wp, "∇", 24);
                        else if (cmd == "lim") AddText(wp, "lim", 20, isItalic: false);
                        else if (cmd == "angle") AddText(wp, "∠", 24);
                        else if (cmd == "perp") AddText(wp, "⊥", 24);
                        else if (cmd == "parallel") AddText(wp, "∥", 24);
                        else if (cmd == "triangle") AddText(wp, "△", 24);
                        else if (cmd == "square") AddText(wp, "□", 24);
                        else if (cmd == "odot") AddText(wp, "⊙", 24);
                        else if (cmd == "cong") AddText(wp, "≅", 24);
                        else if (cmd == "sim") AddText(wp, "∼", 24);
                        else if (cmd == "circ") AddText(wp, "°", 24);
                        else if (cmd == "rightarrow" || cmd == "to") AddText(wp, "→", 24);
                        else if (cmd == "leftarrow") AddText(wp, "←", 24);
                        else if (cmd == "uparrow") AddText(wp, "↑", 24);
                        else if (cmd == "downarrow") AddText(wp, "↓", 24);
                        else if (cmd == "leftrightarrow") AddText(wp, "↔", 24);
                        else if (cmd == "Longrightarrow") AddText(wp, "⟹", 24);
                        else if (cmd == "Longleftrightarrow") AddText(wp, "⟺", 24);
                        else if (cmd == "sqrt")
                        {
                            // Trích xuất và hiển thị chỉ số bậc của căn (ví dụ: bậc 3, 4) dưới dạng superscript đứng trước căn
                            if (i < latex.Length && latex[i] == '[')
                            {
                                int endIdx = latex.IndexOf(']', i);
                                if (endIdx != -1)
                                {
                                    string degree = latex.Substring(i + 1, endIdx - i - 1);
                                    // Chuyển ký số sang ký tự Unicode superscript tương ứng để hiển thị gọn đẹp
                                    string prettyDegree = degree.Replace("3", "³").Replace("4", "⁴").Replace("n", "ⁿ");
                                    AddText(wp, prettyDegree, 14, isItalic: false, isSuperscript: true);
                                    i = endIdx + 1;
                                }
                            }
                            
                            AddText(wp, "√", 26, isItalic: false);

                            if (i < latex.Length && latex[i] == '{')
                            {
                                int endBrace = GetMatchingBrace(latex, i);
                                string arg = latex.Substring(i + 1, endBrace - i - 1);
                                i = endBrace + 1;

                                var contentBorder = new Border
                                {
                                    BorderBrush = Brushes.White,
                                    BorderThickness = new Thickness(0, 1.5, 0, 0),
                                    Margin = new Thickness(0, 2, 0, 0)
                                };
                                var contentWp = new WrapPanel();
                                AddText(contentWp, arg, 20);
                                contentBorder.Child = contentWp;
                                wp.Children.Add(contentBorder);
                            }
                        }
                        else
                        {
                            AddText(wp, "\\" + cmd, 18);
                        }
                    }
                    else if (latex[i] == '^')
                    {
                        i++;
                        if (i < latex.Length)
                        {
                            string super = "";
                            if (latex[i] == '{')
                            {
                                int endBrace = GetMatchingBrace(latex, i);
                                super = latex.Substring(i + 1, endBrace - i - 1);
                                i = endBrace + 1;
                            }
                            else
                            {
                                super = latex[i].ToString();
                                i++;
                            }
                            AddText(wp, super, 14, isSuperscript: true);
                        }
                    }
                    else if (latex[i] == '_')
                    {
                        i++;
                        if (i < latex.Length)
                        {
                            string sub = "";
                            if (latex[i] == '{')
                            {
                                int endBrace = GetMatchingBrace(latex, i);
                                sub = latex.Substring(i + 1, endBrace - i - 1);
                                i = endBrace + 1;
                            }
                            else
                            {
                                sub = latex[i].ToString();
                                i++;
                            }
                            AddText(wp, sub, 14, isSubscript: true);
                        }
                    }
                    else if (latex[i] == '{' || latex[i] == '}')
                    {
                        i++;
                    }
                    else
                    {
                        AddText(wp, latex[i].ToString(), 24);
                        i++;
                    }
                }
            }
            catch
            {
                wp.Children.Clear();
                AddText(wp, latex, 20);
            }

            targetCtrl.Content = wp;
        }

        private void UpdateLaTeXPreview(string latex)
        {
            UpdateLaTeXControlPreview(latex, latexPreviewCtrl);
        }

        private int GetMatchingBrace(string s, int startIdx)
        {
            int count = 0;
            for (int i = startIdx; i < s.Length; i++)
            {
                if (s[i] == '{') count++;
                else if (s[i] == '}')
                {
                    count--;
                    if (count == 0) return i;
                }
            }
            return s.Length - 1;
        }

        private void AddText(WrapPanel wp, string text, double size, bool isItalic = true, bool isSuperscript = false, bool isSubscript = false)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = size,
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Segoe UI Symbol")
            };
            if (isItalic && text.Any(char.IsLetter))
            {
                tb.FontStyle = FontStyles.Italic;
            }
            if (isSuperscript)
            {
                tb.Margin = new Thickness(0, 0, 0, 10);
                tb.VerticalAlignment = VerticalAlignment.Top;
            }
            else if (isSubscript)
            {
                tb.Margin = new Thickness(0, 10, 0, 0);
                tb.VerticalAlignment = VerticalAlignment.Bottom;
            }
            else
            {
                tb.VerticalAlignment = VerticalAlignment.Center;
            }
            wp.Children.Add(tb);
        }

        public override void Dispose()
        {
            Loaded -= MathSymbolsTool_Loaded;
            Unloaded -= MathSymbolsTool_Unloaded;

            if (_copyStatusTimer != null)
            {
                _copyStatusTimer.Stop();
                _copyStatusTimer = null;
            }
            if (_soundPlayer != null)
            {
                _soundPlayer.Dispose();
                _soundPlayer = null;
            }
        }

        private void LoadPracticalApps()
        {
            bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
            string suffix = isVN ? "VN" : "EN";

            var items = new System.Collections.Generic.List<PracticalAppItem>
            {
                new PracticalAppItem
                {
                    Icon = "📝",
                    Title = isVN ? "Nghiên cứu Khoa học & Xuất bản LaTeX" : "Scientific Research & LaTeX Publishing",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_math_symbols_1_{suffix}.png",
                    Description = isVN 
                        ? "Các nghiên cứu, giáo sư và sinh viên sử dụng mã lệnh LaTeX của các ký hiệu toán học (tích phân, tổng, ma trận) để soạn thảo luận văn, sách giáo khoa và các bài báo khoa học chuẩn quốc tế." 
                        : "Researchers, professors, and students use LaTeX commands for mathematical symbols (integrals, summations, matrices) to draft theses, textbooks, and international scientific papers."
                },
                new PracticalAppItem
                {
                    Icon = "💻",
                    Title = isVN ? "Khoa học Máy tính & Logic Boolean" : "Computer Science & Boolean Logic",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_math_symbols_2_{suffix}.png",
                    Description = isVN 
                        ? "Các kỹ sư phần mềm sử dụng các ký hiệu logic toán học (AND, OR, NOT, tập hợp con, giao, hợp) để thiết kế cổng logic phần cứng, lập trình điều kiện trong mã nguồn và tối ưu hóa truy vấn cơ sở dữ liệu." 
                        : "Software engineers use mathematical logic symbols (AND, OR, NOT, subsets, intersections, unions) to design hardware logic gates, program conditions in source code, and optimize database queries."
                },
                new PracticalAppItem
                {
                    Icon = "🤖",
                    Title = isVN ? "Học sâu & Thiết kế Mạng Nơ-ron" : "Deep Learning & Neural Network Design",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_math_symbols_3_{suffix}.png",
                    Description = isVN 
                        ? "Các chuyên gia AI áp dụng ký hiệu toán học đại số tuyến tính và giải tích (như gradient, nhân ma trận, đạo hàm riêng) để mô tả toán học cấu trúc mạng nơ-ron và thuật toán huấn luyện học sâu." 
                        : "AI specialists apply linear algebra and calculus symbols (like gradient, matrix multiplication, partial derivatives) to mathematically describe neural network structures and deep learning training algorithms."
                }
            };

            try
            {
                if (practicalAppViewer != null) practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for MathSymbolsTool: {Err}", ex.Message);
            }
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewPractice == null || viewPractical == null)
                return;

            viewGuide.Visibility = Visibility.Collapsed;
            viewPractice.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            

            switch (sideMenu.SelectedIndex)
            {
                case 0:
                    viewGuide.Visibility = Visibility.Visible;
                    break;
                case 1:
                    viewPractice.Visibility = Visibility.Visible;
                    break;
                case 2:
                    viewPractical.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}