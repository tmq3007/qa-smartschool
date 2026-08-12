using QASmartClass.LearningTools.Models;
using QASmartClass.LearningTools.Controls;
using QASmartClass.LearningTools.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Input;

namespace QASmartClass.LearningTools.Views.Math
{
    public partial class MatrixTool : UserControl
    {
        private int _size = 2;

        public MatrixTool()
        {
            InitializeComponent();
            Loaded += (_, _) => {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn chi tiết" : "Detailed Guide";
                if (menuTextPractice != null) menuTextPractice.Text = isVN ? "Tính toán & Ví dụ" : "Calculation & Examples";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Practical Apps";

                if (sideMenu != null)
                {
                    sideMenu.SelectionChanged += SideMenu_SelectionChanged;
                    sideMenu.SelectedIndex = 0;
                }

                BuildGrids();
                TouchNumPad.Attach(txtScalar, step: 1, allowDecimal: true, allowNegative: true);
                LoadPracticalApps();

                // Show submit button if running inside StudentShell
                var win = Window.GetWindow(this);
                if (win != null && win.GetType().Name == "StudentShell")
                {
                    btnSubmit.Visibility = Visibility.Visible;
                }
            };
        }

        private void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var a = ReadMatrix(gridA);
                if (a == null) return;
                
                var matrixStrings = new List<string>();
                for (int i = 0; i < _size; i++)
                {
                    var rowStrings = new List<string>();
                    for (int j = 0; j < _size; j++)
                    {
                        rowStrings.Add(a[i, j].ToString("G4"));
                    }
                    matrixStrings.Add("[" + string.Join(", ", rowStrings) + "]");
                }
                
                string data = $"Ma trận {_size}x{_size}: " + string.Join(" ", matrixStrings);
                
                var win = Window.GetWindow(this);
                if (win != null && win.GetType().Name == "StudentShell")
                {
                    var method = win.GetType().GetMethod("SendToolSubmission");
                    if (method != null)
                    {
                        method.Invoke(win, new object[] { "matrix_tool", data });
                        MessageBox.Show("Đã gửi ma trận của bạn lên máy giáo viên thành công!", "Nộp bài giải", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi nộp bài: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  GRID BUILDER — tạo lưới TextBox cho ma trận
        // ═══════════════════════════════════════════════════════════

        private void Size_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (cboSize?.SelectedItem is not ComboBoxItem item) return;
            _size = item.Content?.ToString() switch
            {
                "3 × 3" => 3,
                "4 × 4" => 4,
                "5 × 5" => 5,
                _ => 2
            };
            BuildGrids();
        }

        private void BuildGrids()
        {
            if (gridA == null || gridB == null) return;
            BuildMatrixGrid(gridA, _size);
            BuildMatrixGrid(gridB, _size);
            gridResult.Children.Clear();
            resultPanel.Visibility = Visibility.Collapsed;
        }

        private static void BuildMatrixGrid(UniformGrid grid, int size)
        {
            grid.Children.Clear();
            grid.Columns = size;
            grid.Rows = size;

            var style = grid.TryFindResource("MatrixInputTextBox") as Style;

            for (int i = 0; i < size * size; i++)
            {
                var tb = new TextBox
                {
                    Width = 65, Height = 44,
                    TextAlignment = TextAlignment.Center,
                    Text = "0",
                    Style = style
                };
                TouchNumPad.Attach(tb, step: 1, allowDecimal: true, allowNegative: true);
                grid.Children.Add(tb);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  READ / DISPLAY MATRIX & HELPERS
        // ═══════════════════════════════════════════════════════════

        private double[,]? ReadMatrix(UniformGrid grid)
        {
            var m = new double[_size, _size];
            int idx = 0;
            foreach (var child in grid.Children)
            {
                if (child is TextBox tb)
                {
                    if (!ParsingHelper.TryParseDouble(tb.Text, out double val))
                    {
                        MessageBox.Show($"Ô {idx + 1}: \"{tb.Text}\" không phải số hợp lệ.\nVui lòng nhập số (VD: 3, -1.5, 0)",
                            "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                        tb.Focus();
                        tb.SelectAll();
                        return null;
                    }
                    m[idx / _size, idx % _size] = val;
                    idx++;
                }
            }
            return m;
        }

        private void DisplayMatrixResult(string title, double[,] matrix, string? steps = null)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

            txtResultTitle.Text = title;
            txtResultValue.Text = "";
            txtResultValue.Visibility = Visibility.Collapsed;

            gridResult.Children.Clear();
            gridResult.Columns = cols;
            gridResult.Rows = rows;

            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                {
                    var tb = new TextBlock
                    {
                        Text = FormatNumber(matrix[i, j]),
                        FontSize = 18, FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(Color.FromRgb(27, 94, 32)),
                        FontFamily = new FontFamily("Segoe UI"),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(12, 6, 12, 6),
                        MinWidth = 60, TextAlignment = TextAlignment.Center
                    };
                    gridResult.Children.Add(tb);
                }

            if (!string.IsNullOrEmpty(steps))
            {
                txtSteps.Text = steps;
                stepsPanel.Visibility = Visibility.Visible;
            }
            else
            {
                stepsPanel.Visibility = Visibility.Collapsed;
            }

            resultPanel.Visibility = Visibility.Visible;
        }

        private void DisplayScalarResult(string title, double value, string? steps = null)
        {
            txtResultTitle.Text = title;
            txtResultValue.Text = double.IsNaN(value) ? "Không xác định" : FormatNumber(value);
            txtResultValue.Foreground = new SolidColorBrush(Color.FromRgb(27, 94, 32)); // Reset to default green
            txtResultValue.Visibility = Visibility.Visible;
            gridResult.Children.Clear();

            if (!string.IsNullOrEmpty(steps))
            {
                txtSteps.Text = steps;
                stepsPanel.Visibility = Visibility.Visible;
            }
            else
            {
                stepsPanel.Visibility = Visibility.Collapsed;
            }

            resultPanel.Visibility = Visibility.Visible;
        }

        private static string FormatNumber(double v)
        {
            if (System.Math.Abs(v - System.Math.Round(v)) < 1e-9) return ((long)System.Math.Round(v)).ToString();
            // Hiển thị phân số nếu mẫu nhỏ
            var (num, den) = ToFraction(v);
            if (den > 0 && den <= 100)
                return $"{num}/{den} ({v:G4})";
            return v.ToString("G6");
        }

        private static (long num, long den) ToFraction(double val, long maxDen = 100)
        {
            if (double.IsNaN(val) || double.IsInfinity(val)) return (0, 0);
            long sign = val < 0 ? -1 : 1;
            val = System.Math.Abs(val);

            long bestNum = (long)System.Math.Round(val), bestDen = 1;
            double bestErr = System.Math.Abs(val - bestNum);

            for (long d = 2; d <= maxDen; d++)
            {
                long n = (long)System.Math.Round(val * d);
                double err = System.Math.Abs(val - (double)n / d);
                if (err < bestErr - 1e-12)
                {
                    bestErr = err;
                    bestNum = n;
                    bestDen = d;
                }
            }

            if (bestErr > 1e-6) return (0, 0); // Không phải phân số đẹp
            return (sign * bestNum, bestDen);
        }

        private static double[,] GetSubMatrix(double[,] m, int row, int col, int size)
        {
            var sub = new double[size - 1, size - 1];
            int si = 0;
            for (int i = 0; i < size; i++)
            {
                if (i == row) continue;
                int sj = 0;
                for (int j = 0; j < size; j++)
                {
                    if (j == col) continue;
                    sub[si, sj] = m[i, j];
                    sj++;
                }
                si++;
            }
            return sub;
        }

        private static string FormatMatrixToString(double[,] matrix)
        {
            int r = matrix.GetLength(0);
            int c = matrix.GetLength(1);
            var lines = new List<string>();
            for (int i = 0; i < r; i++)
            {
                var rowVals = new List<string>();
                for (int j = 0; j < c; j++)
                {
                    rowVals.Add(FormatNumber(matrix[i, j]));
                }
                lines.Add("      [  " + string.Join("\t", rowVals) + "  ]");
            }
            return string.Join("\n", lines);
        }

        private static string FormatExpansionSum(double[,] m, int n)
        {
            var parts = new List<string>();
            for (int j = 0; j < n; j++)
            {
                int posSign = (j % 2 == 0) ? 1 : -1;
                double elementVal = m[0, j];
                double coeff = posSign * elementVal;
                double[,] sub = GetSubMatrix(m, 0, j, n);
                double subDet = Determinant(sub, n - 1);
                double termVal = coeff * subDet;
                
                string signStr = (j == 0) ? (termVal < 0 ? "-" : "") : (termVal < 0 ? " - " : " + ");
                parts.Add($"{signStr}{FormatNumber(System.Math.Abs(termVal))}");
            }
            return string.Join("", parts).Trim();
        }

        // ═══════════════════════════════════════════════════════════
        //  MATRIX OPERATIONS
        // ═══════════════════════════════════════════════════════════

        private void Calc_Det(object sender, RoutedEventArgs e)
        {
            var a = ReadMatrix(gridA);
            if (a == null) return;

            if (_size >= 5)
            {
                double det = Determinant(a, _size);
                DisplayScalarResult($"Det(A)  —  Ma trận {_size}×{_size}", det,
                    $"Det(A) = {FormatNumber(det)} (Không hiển thị chi tiết bước giải cho ma trận cấp {_size}x{_size} trở lên để tối ưu hiển thị)");
                return;
            }

            var steps = new List<string>();
            double detNormal = Determinant(a, _size, steps);

            DisplayScalarResult($"Det(A)  —  Ma trận {_size}×{_size}", detNormal,
                string.Join("\n", steps));
        }

        private void Calc_Inverse(object sender, RoutedEventArgs e)
        {
            var a = ReadMatrix(gridA);
            if (a == null) return;

            double det = Determinant(a, _size);

            if (System.Math.Abs(det) < 1e-12)
            {
                DisplayScalarResult("A⁻¹ — Không tồn tại", double.NaN,
                    "Det(A) = 0 → Ma trận suy biến → KHÔNG có nghịch đảo.\n" +
                    "💡 Ma trận có nghịch đảo khi và chỉ khi Det(A) ≠ 0.");
                txtResultValue.Text = "Ma trận suy biến (Det = 0)";
                txtResultValue.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
                return;
            }

            var inv = Inverse(a, _size, det);

            if (_size >= 5)
            {
                DisplayMatrixResult($"A⁻¹  —  Nghịch đảo (Det = {FormatNumber(det)})", inv,
                    $"Tìm ma trận nghịch đảo A⁻¹ cho ma trận cấp {_size}x{_size}:\n" +
                    $"  1. Tính định thức: Det(A) = {FormatNumber(det)}\n" +
                    $"  2. Do Det(A) ≠ 0 nên tồn tại ma trận nghịch đảo A⁻¹ = (1/Det(A)) × adj(A).\n" +
                    $"  (Không hiển thị chi tiết bước giải cho ma trận cấp {_size}x{_size} trở lên để tối ưu hiển thị)");
                return;
            }

            var steps = new List<string>();
            // Reuse the steps calculation
            _ = Determinant(a, _size, steps);

            var invSteps = new List<string>();
            invSteps.Add("Các bước tìm ma trận nghịch đảo A⁻¹:");
            invSteps.Add($"Bước 1: Tính định thức: Det(A) = {FormatNumber(det)}");
            
            invSteps.Add("Bước 2: Tìm các phần bù đại số Cᵢⱼ = (-1)ⁱ⁺ʲ × Mᵢⱼ");
            var cofactorMatrix = new double[_size, _size];
            for (int i = 0; i < _size; i++)
            {
                for (int j = 0; j < _size; j++)
                {
                    double[,] sub = GetSubMatrix(a, i, j, _size);
                    double subDet = Determinant(sub, _size - 1);
                    int sign = ((i + j) % 2 == 0) ? 1 : -1;
                    double cofactorVal = sign * subDet;
                    cofactorMatrix[i, j] = cofactorVal;

                    if (_size == 2 || (i == 0 && j < 2) || (i == 1 && j == 0 && _size == 3))
                    {
                        string subStr = _size == 2 
                            ? $"|{FormatNumber(sub[0,0])}|"
                            : $"|{FormatNumber(sub[0,0])} {FormatNumber(sub[0,1])}; {FormatNumber(sub[1,0])} {FormatNumber(sub[1,1])}|";
                        
                        invSteps.Add($"  • C_{i+1}{j+1} = (-1)^{i+j+2} × det{subStr} = {(sign > 0 ? "+" : "-")}({FormatNumber(subDet)}) = {FormatNumber(cofactorVal)}");
                    }
                }
            }
            if (_size > 2)
            {
                invSteps.Add("  • Tính tương tự cho các phần tử còn lại ta được ma trận phần bù đại số C:");
            }
            else
            {
                invSteps.Add("  • Ma trận phần bù đại số C:");
            }
            invSteps.Add(FormatMatrixToString(cofactorMatrix));

            var adj = new double[_size, _size];
            for (int i = 0; i < _size; i++)
                for (int j = 0; j < _size; j++)
                    adj[j, i] = cofactorMatrix[i, j];

            invSteps.Add("Bước 3: Chuyển vị ma trận C để được ma trận phó liên hợp adj(A) = Cᵀ:");
            invSteps.Add(FormatMatrixToString(adj));

            invSteps.Add($"Bước 4: Nhân hệ số 1/Det để tính ma trận nghịch đảo A⁻¹ = (1/Det) × adj(A):");
            invSteps.Add($"  A⁻¹ = (1/{FormatNumber(det)}) × adj(A)");

            DisplayMatrixResult($"A⁻¹  —  Nghịch đảo (Det = {FormatNumber(det)})", inv,
                string.Join("\n", invSteps) + "\n\nChi tiết tính định thức:\n" + string.Join("\n", steps));
        }

        private void Calc_Transpose(object sender, RoutedEventArgs e)
        {
            var a = ReadMatrix(gridA);
            if (a == null) return;

            var t = new double[_size, _size];
            for (int i = 0; i < _size; i++)
            {
                for (int j = 0; j < _size; j++)
                {
                    t[j, i] = a[i, j];
                }
            }

            if (_size >= 5)
            {
                DisplayMatrixResult("Aᵀ  —  Chuyển vị", t,
                    $"Chuyển vị ma trận cấp {_size}x{_size} (đổi hàng thành cột):\n" +
                    $"  Aᵀ[i, j] = A[j, i]\n" +
                    $"  (Không hiển thị chi tiết bước giải cho ma trận cấp {_size}x{_size} trở lên để tối ưu hiển thị)");
                return;
            }

            var transposeSteps = new List<string>();
            transposeSteps.Add("Các bước chuyển vị ma trận (dòng i đổi thành cột i):");

            for (int i = 0; i < _size; i++)
            {
                var rowElements = new List<string>();
                for (int j = 0; j < _size; j++)
                {
                    rowElements.Add(FormatNumber(a[i, j]));
                }
                transposeSteps.Add($"  • Dòng {i+1} [{string.Join(", ", rowElements)}] chuyển thành cột {i+1}");
            }

            DisplayMatrixResult("Aᵀ  —  Chuyển vị", t, string.Join("\n", transposeSteps));
        }

        private void Calc_Scalar(object sender, RoutedEventArgs e)
        {
            var a = ReadMatrix(gridA);
            if (a == null) return;

            if (!ParsingHelper.TryParseDouble(txtScalar?.Text, out double k))
            {
                MessageBox.Show("Hệ số k không hợp lệ. Vui lòng nhập số.",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var r = new double[_size, _size];
            for (int i = 0; i < _size; i++)
            {
                for (int j = 0; j < _size; j++)
                {
                    r[i, j] = k * a[i, j];
                }
            }

            if (_size >= 5)
            {
                DisplayMatrixResult($"k · A  —  (k = {FormatNumber(k)})", r,
                    $"Nhân ma trận cấp {_size}x{_size} với hệ số k = {FormatNumber(k)}:\n" +
                    $"  C[i, j] = {FormatNumber(k)} × A[i, j]\n" +
                    $"  (Không hiển thị chi tiết bước giải cho ma trận cấp {_size}x{_size} trở lên để tối ưu hiển thị)");
                return;
            }

            var scalarSteps = new List<string>();
            scalarSteps.Add($"Các bước nhân ma trận với hệ số k = {FormatNumber(k)} (cᵢⱼ = k × aᵢⱼ):");

            for (int i = 0; i < _size; i++)
            {
                for (int j = 0; j < _size; j++)
                {
                    scalarSteps.Add($"  • c_{i+1}{j+1} = {FormatNumber(k)} × ({FormatNumber(a[i, j])}) = {FormatNumber(r[i, j])}");
                }
            }

            DisplayMatrixResult($"k · A  —  (k = {FormatNumber(k)})", r, string.Join("\n", scalarSteps));
        }

        private void Calc_Add(object sender, RoutedEventArgs e)
        {
            var a = ReadMatrix(gridA);
            var b = ReadMatrix(gridB);
            if (a == null || b == null) return;

            var r = new double[_size, _size];
            for (int i = 0; i < _size; i++)
            {
                for (int j = 0; j < _size; j++)
                {
                    r[i, j] = a[i, j] + b[i, j];
                }
            }

            if (_size >= 5)
            {
                DisplayMatrixResult("A + B  —  Tổng 2 ma trận", r,
                    $"Cộng hai ma trận cấp {_size}x{_size} (cộng các phần tử tương ứng):\n" +
                    $"  C[i, j] = A[i, j] + B[i, j]\n" +
                    $"  (Không hiển thị chi tiết bước giải cho ma trận cấp {_size}x{_size} trở lên để tối ưu hiển thị)");
                return;
            }

            var addSteps = new List<string>();
            addSteps.Add("Các bước cộng 2 ma trận (cᵢⱼ = aᵢⱼ + bᵢⱼ):");

            for (int i = 0; i < _size; i++)
            {
                for (int j = 0; j < _size; j++)
                {
                    addSteps.Add($"  • c_{i+1}{j+1} = a_{i+1}{j+1} + b_{i+1}{j+1} = {FormatNumber(a[i, j])} + ({FormatNumber(b[i, j])}) = {FormatNumber(r[i, j])}");
                }
            }

            DisplayMatrixResult("A + B  —  Tổng 2 ma trận", r, string.Join("\n", addSteps));
        }

        private void Calc_Multiply(object sender, RoutedEventArgs e)
        {
            var a = ReadMatrix(gridA);
            var b = ReadMatrix(gridB);
            if (a == null || b == null) return;

            var r = new double[_size, _size];
            for (int i = 0; i < _size; i++)
            {
                for (int j = 0; j < _size; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < _size; k++)
                    {
                        sum += a[i, k] * b[k, j];
                    }
                    r[i, j] = sum;
                }
            }

            if (_size >= 5)
            {
                DisplayMatrixResult("A × B  —  Tích 2 ma trận", r,
                    $"Nhân hai ma trận cấp {_size}x{_size} (nhân dòng của A với cột của B):\n" +
                    $"  C[i, j] = ∑ (A[i, k] × B[k, j])\n" +
                    $"  (Không hiển thị chi tiết bước giải cho ma trận cấp {_size}x{_size} trở lên để tối ưu hiển thị)");
                return;
            }

            var mulSteps = new List<string>();
            mulSteps.Add("Các bước nhân 2 ma trận (nhân dòng của A với cột của B):");

            for (int i = 0; i < _size; i++)
            {
                for (int j = 0; j < _size; j++)
                {
                    var terms = new List<string>();
                    for (int k = 0; k < _size; k++)
                    {
                        terms.Add($"({FormatNumber(a[i, k])} × {FormatNumber(b[k, j])})");
                    }
                    mulSteps.Add($"  • c_{i+1}{j+1} = dòng {i+1} của A × cột {j+1} của B\n" +
                                 $"         = {string.Join(" + ", terms)} = {FormatNumber(r[i, j])}");
                }
            }

            DisplayMatrixResult("A × B  —  Tích 2 ma trận", r, string.Join("\n", mulSteps));
        }

        // ═══════════════════════════════════════════════════════════
        //  DETERMINANT (Laplace expansion)
        // ═══════════════════════════════════════════════════════════

        private static double Determinant(double[,] m, int n, List<string>? steps = null)
        {
            if (n == 1) return m[0, 0];

            if (n == 2)
            {
                double d = m[0, 0] * m[1, 1] - m[0, 1] * m[1, 0];
                steps?.Add($"Det = ({FormatNumber(m[0, 0])} × {FormatNumber(m[1, 1])}) - ({FormatNumber(m[0, 1])} × {FormatNumber(m[1, 0])}) = {FormatNumber(d)}");
                return d;
            }

            double det = 0;
            var parts = new List<string>();
            var details = new List<string>();

            for (int j = 0; j < n; j++)
            {
                int posSign = (j % 2 == 0) ? 1 : -1;
                double elementVal = m[0, j];
                double coeff = posSign * elementVal;

                double[,] sub = GetSubMatrix(m, 0, j, n);
                double subDet = Determinant(sub, n - 1);
                det += coeff * subDet;

                string signStr = (j == 0) ? (coeff < 0 ? "-" : "") : (coeff < 0 ? " - " : " + ");
                double absCoeff = System.Math.Abs(coeff);
                parts.Add($"{signStr}{FormatNumber(absCoeff)}×M₀{j}");

                if (n == 3)
                {
                    details.Add($"  • Minor M₀{j} (bỏ dòng 1, cột {j+1}): det|{FormatNumber(sub[0,0])} {FormatNumber(sub[0,1])}; {FormatNumber(sub[1,0])} {FormatNumber(sub[1,1])}| = ({FormatNumber(sub[0,0])}×{FormatNumber(sub[1,1])} - {FormatNumber(sub[0,1])}×{FormatNumber(sub[1,0])}) = {FormatNumber(subDet)}");
                }
                else // n == 4
                {
                    details.Add($"  • Minor M₀{j} (bỏ dòng 1, cột {j+1}) là ma trận 3x3 có định thức: M₀{j} = {FormatNumber(subDet)}");
                }
            }

            var prettyParts = parts.Select(p => p
                .Replace("M₀0", "M₀₀")
                .Replace("M₀1", "M₀₁")
                .Replace("M₀2", "M₀₂")
                .Replace("M₀3", "M₀₃")).ToList();

            var prettyDetails = details.Select(d => d
                .Replace("M₀0", "M₀₀")
                .Replace("M₀1", "M₀₁")
                .Replace("M₀2", "M₀₂")
                .Replace("M₀3", "M₀₃")).ToList();

            string formulaStr = "Det = " + string.Join("", prettyParts).Trim();
            
            var valParts = new List<string>();
            for (int j = 0; j < n; j++)
            {
                int posSign = (j % 2 == 0) ? 1 : -1;
                double coeff = posSign * m[0, j];
                double[,] sub = GetSubMatrix(m, 0, j, n);
                double subDet = Determinant(sub, n - 1);
                
                string signStr = (j == 0) ? (coeff < 0 ? "-" : "") : (coeff < 0 ? " - " : " + ");
                valParts.Add($"{signStr}{FormatNumber(System.Math.Abs(coeff))}×({FormatNumber(subDet)})");
            }
            string valStr = "      = " + string.Join("", valParts).Trim();

            steps?.Add($"Khai triển Laplace theo hàng 1:\n" +
                       $"  {formulaStr}\n" +
                       $"Chi tiết tính các định thức con (Minor):\n" +
                       $"{string.Join("\n", prettyDetails)}\n" +
                       $"Thế các giá trị vào công thức:\n" +
                       $"  {valStr}\n" +
                       $"      = {FormatExpansionSum(m, n)}\n" +
                       $"      = {FormatNumber(det)}");

            return det;
        }

        private static double Cofactor(double[,] m, int row, int col, int n)
        {
            var sub = GetSubMatrix(m, row, col, n);
            int sign = ((row + col) % 2 == 0) ? 1 : -1;
            return sign * Determinant(sub, n - 1);
        }

        // ═══════════════════════════════════════════════════════════
        //  INVERSE (adjugate / det)
        // ═══════════════════════════════════════════════════════════

        private static double[,] Inverse(double[,] m, int n, double det)
        {
            var adj = new double[n, n];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    adj[j, i] = Cofactor(m, i, j, n); // Transposed

            var inv = new double[n, n];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    inv[i, j] = adj[i, j] / det;
            return inv;
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (rootGrid == null || sideMenu == null || contentGuide == null || contentCalc == null || viewPractical == null)
                return;

            contentGuide.Visibility = Visibility.Collapsed;
            contentCalc.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            int index = sideMenu.SelectedIndex;
            if (index == 0)
            {
                contentGuide.Visibility = Visibility.Visible;
            }
            else if (index == 1)
            {
                contentCalc.Visibility = Visibility.Visible;
            }
            else if (index == 2)
            {
                viewPractical.Visibility = Visibility.Visible;
            }
        }

        private void Tab_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border clickedBorder && clickedBorder.Tag is string tag)
            {
                SwitchToTab(tag);
            }
        }

        private void SwitchToTab(string tag)
        {
            if (sideMenu == null) return;
            if (tag == "guide")
                sideMenu.SelectedIndex = 1;
            else if (tag == "calc" || tag == "practice")
                sideMenu.SelectedIndex = 1;
            else if (tag == "app" || tag == "practical")
                sideMenu.SelectedIndex = 1;
        }

        // ═══════════════════════════════════════════════════════════
        //  PRESETS — ví dụ nhanh
        // ═══════════════════════════════════════════════════════════

        private void Preset_Identity(object sender, RoutedEventArgs e)
        {
            var vals = new double[_size, _size];
            for (int i = 0; i < _size; i++) vals[i, i] = 1;
            FillGrid(gridA, vals);
        }

        private void Preset_Sample1(object sender, RoutedEventArgs e)
        {
            if (cboSize.SelectedIndex != 0)
            {
                cboSize.SelectedIndex = 0; // Kích hoạt sự thay đổi tự động chạy BuildGrids()
            }
            else
            {
                BuildGrids();
            }
            FillGrid(gridA, new double[,] { { 1, 2 }, { 3, 4 } });
        }

        private void Preset_Sample2(object sender, RoutedEventArgs e)
        {
            if (cboSize.SelectedIndex != 1)
            {
                cboSize.SelectedIndex = 1; // Kích hoạt sự thay đổi tự động chạy BuildGrids()
            }
            else
            {
                BuildGrids();
            }
            FillGrid(gridA, new double[,] { { 2, 1, 3 }, { 0, 4, 1 }, { 5, 2, 0 } });
        }
        private void Preset_Reset(object sender, RoutedEventArgs e)
        {
            if (gridA != null)
            {
                foreach (var child in gridA.Children)
                {
                    if (child is TextBox tb)
                    {
                        tb.Text = "0";
                    }
                }
            }
            if (gridB != null)
            {
                foreach (var child in gridB.Children)
                {
                    if (child is TextBox tb)
                    {
                        tb.Text = "0";
                    }
                }
            }
            if (txtScalar != null)
            {
                txtScalar.Text = "2";
            }
            if (resultPanel != null)
            {
                resultPanel.Visibility = Visibility.Collapsed;
            }
        }


        private void LoadPracticalApps()
        {
            try
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                string suffix = isVN ? "VN" : "EN";

                var items = new List<PracticalAppItem>
                {
                    new PracticalAppItem
                    {
                        Icon = "🎮",
                        Title = isVN ? "Đồ Họa Máy Tính" : "3D Computer Graphics",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_matrix_1_{suffix}.png",
                        Description = isVN 
                            ? "Ứng dụng: Ma trận biến đổi xoay, thu phóng và dịch chuyển các đối tượng đồ họa 2D/3D trong game và thiết kế CAD." 
                            : "Perform translation, rotation, and scaling of 3D objects and cameras by multiplying coordinate vectors with transformation matrices."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🔒",
                        Title = isVN ? "Mật Mã Học & Bảo Mật" : "Cryptography & Encryption",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_matrix_2_{suffix}.png",
                        Description = isVN 
                            ? "Ứng dụng: Mã hóa Hill sử dụng nhân ma trận để mã hóa dữ liệu văn bản và giải mã bằng ma trận nghịch đảo tương ứng." 
                            : "Encrypt and decrypt text messages using matrix multiplication key systems (e.g. Hill Cipher)."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🔍",
                        Title = isVN ? "Thuật Toán PageRank" : "Google PageRank Search",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_matrix_3_{suffix}.png",
                        Description = isVN 
                            ? "Ứng dụng: Google xếp hạng trang web bằng cách biểu diễn mạng lưới Internet thành ma trận kề khổng lồ chứa hàng tỷ phần tử." 
                            : "Rank billions of web pages by calculating eigenvectors of massive internet hyperlink matrices."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📈",
                        Title = isVN ? "Kinh Tế Học Leontief" : "Finite Element Analysis",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_matrix_4_{suffix}.png",
                        Description = isVN 
                            ? "Ứng dụng: Mô hình cân đối cung cầu liên ngành giúp chính phủ và doanh nghiệp tính toán mức sản xuất tối ưu của các lĩnh vực." 
                            : "Solve large mechanical stress matrices to evaluate structural safety in cars, buildings, and aircraft."
                    },
                    new PracticalAppItem
                    {
                        Icon = "⚡",
                        Title = isVN ? "Mạch Điện Kirchhoff" : "Quantum Computing Qubits",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_matrix_5_{suffix}.png",
                        Description = isVN 
                            ? "Ứng dụng: Giải các vòng lặp dòng điện trong mạch điện phức tạp bằng định luật Kirchhoff và quy tắc Cramer (Định thức Δ₁/Δ)." 
                            : "Model quantum logic gates and state superposition transformations using unitary complex matrices."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🚀",
                        Title = isVN ? "Mô Phỏng Vật Lý 3D" : "Image Digital Filters",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_matrix_6_{suffix}.png",
                        Description = isVN 
                            ? "Ứng dụng: Sử dụng ma trận quán tính (inertia tensor) để tính toán va chạm và xoay vật lý chân thực trong đồ họa trò chơi." 
                            : "Apply blurring, sharpening, and edge detection effects by convolving images with 3x3 pixel kernel matrices."
                    }
                ,
                    new PracticalAppItem
                    {
                        Icon = "🖼️",
                        Title = isVN ? "Xử lý ma trận ảnh 3D" : "3D Image Matrix Processing",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_matrix_7_{suffix}.png",
                        Description = isVN 
                            ? "Áp dụng phép nhân ma trận để xoay, co giãn, chiếu phối cảnh các đối tượng mô hình 3D trong lập trình đồ họa." 
                            : "Apply matrix multiplication to rotate, scale, and project 3D models in computer graphics programming."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🔍",
                        Title = isVN ? "Thuật toán PageRank của Google" : "Google PageRank Algorithm",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_matrix_8_{suffix}.png",
                        Description = isVN 
                            ? "Sử dụng ma trận chuyển đổi xác suất và vectơ riêng để xếp hạng mức độ uy tín của hàng tỷ trang web trên Internet." 
                            : "Use probability transition matrices and eigenvectors to rank the credibility of billions of web pages on the Internet."
                    }};

                contentApp.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for MatrixTool: {Err}", ex.Message);
            }
        }

        private void FillGrid(UniformGrid grid, double[,] vals)
        {
            int idx = 0;
            foreach (var child in grid.Children)
            {
                if (child is TextBox tb)
                {
                    int i = idx / vals.GetLength(1);
                    int j = idx % vals.GetLength(1);
                    if (i < vals.GetLength(0) && j < vals.GetLength(1))
                        tb.Text = vals[i, j].ToString("G10");
                    idx++;
                }
            }
        }
    }
}