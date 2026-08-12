using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QASmartClass.LearningTools.Controls;
using QASmartClass.LearningTools.Models;

namespace QASmartClass.LearningTools.Views.Math
{
    public partial class PrimeNumberTool : BaseToolControl
    {
        public PrimeNumberTool()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
                if (menuTextCheck != null) menuTextCheck.Text = isVN ? "Kiểm tra" : "Check Prime";
                if (menuTextFactor != null) menuTextFactor.Text = isVN ? "Phân tích" : "Factorization";
                if (menuTextSieve != null) menuTextSieve.Text = isVN ? "Sàng Eratosthenes" : "Sieve of Eratosthenes";
                if (menuTextGcd != null) menuTextGcd.Text = isVN ? "ƯCLN & BCNN" : "GCD & LCM";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Real-world Applications";

                BuildSieve();
                // Bàn phím số mini
                if (txtCheckNumber != null) TouchNumPad.Attach(txtCheckNumber, step: 1, min: 1, allowDecimal: false);
                if (txtFactorNumber != null) TouchNumPad.Attach(txtFactorNumber, step: 1, min: 2, allowDecimal: false);
                if (txtGcdA != null) TouchNumPad.Attach(txtGcdA, step: 1, min: 1, allowDecimal: false);
                if (txtGcdB != null) TouchNumPad.Attach(txtGcdB, step: 1, min: 1, allowDecimal: false);

                // Initialize placeholders
                ShowCheckPlaceholder();
                ShowFactorPlaceholder();
                ShowGcdPlaceholder();
                LoadPracticalApps();
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  KIỂM TRA SỐ NGUYÊN TỐ
        // ═══════════════════════════════════════════════════════════

        private void CheckNumber_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) Check_Click(sender, e);
        }

        private void Check_Click(object sender, RoutedEventArgs e)
        {
            DoCheck();
        }

        private void Check_Click_Border(object sender, MouseButtonEventArgs e)
        {
            DoCheck();
        }

        private void DoCheck()
        {
            checkResultPanel.Children.Clear();
            if (!long.TryParse(txtCheckNumber.Text.Trim(), out long n) || n < 1)
            {
                SetTextBorderError(txtCheckNumber, true);
                UI.ResultRow("⚠️ Vui lòng nhập số nguyên dương hợp lệ (tối đa 15 chữ số)", "#E65100", checkResultPanel);
                return;
            }
            SetTextBorderError(txtCheckNumber, false);

            bool isPrime = IsPrime(n);

            if (isPrime)
            {
                UI.ResultRow($"✅ {n:N0} là SỐ NGUYÊN TỐ", "#2E7D32", checkResultPanel);
                if (n <= 100000)
                {
                    int pos = CountPrimesUpTo(n);
                    UI.ResultRow($"📊 Là số nguyên tố thứ {pos:N0} trong dãy", "#1565C0", checkResultPanel);
                }
                else
                {
                    double estimate = n / System.Math.Log(n);
                    UI.ResultRow($"📊 Vị trí ước lượng trong dãy: ~{estimate:N0} (theo Định lý số nguyên tố)", "#1565C0", checkResultPanel);
                }
            }
            else
            {
                UI.ResultRow($"❌ {n:N0} KHÔNG phải số nguyên tố", "#C62828", checkResultPanel);

                if (n > 1)
                {
                    var factors = Factorize(n);
                    string factorStr = string.Join(" × ", factors.Select(f =>
                        f.Value == 1 ? $"{f.Key}" : $"{f.Key}^{f.Value}"));
                    UI.ResultRow($"📐 Phân tích: {n:N0} = {factorStr}", "#283593", checkResultPanel);

                    var divisors = GetDivisors(n);
                    if (divisors.Count <= 30)
                        UI.ResultRow($"📋 Ước số ({divisors.Count}): {string.Join(", ", divisors)}", "#424242", checkResultPanel);
                }
            }

            long nextP = NextPrime(n);
            long prevP = PrevPrime(n);
            if (prevP > 1)
                UI.ResultRow($"⬅️ Số NT trước: {prevP:N0}", "#757575", checkResultPanel);
            
            if (nextP > 0)
                UI.ResultRow($"➡️ Số NT tiếp: {nextP:N0}", "#757575", checkResultPanel);
            else
                UI.ResultRow("➡️ Số NT tiếp: Vượt quá giới hạn tính toán", "#757575", checkResultPanel);
        }

        private void ShowPlaceholder(StackPanel panel, string title, string description)
        {
            if (panel == null) return;
            panel.Children.Clear();
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(250, 250, 250)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 8)
            };
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(40, 53, 147)),
                Margin = new Thickness(0, 0, 0, 6)
            });
            sp.Children.Add(new TextBlock
            {
                Text = description,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117)),
                TextWrapping = TextWrapping.Wrap
            });
            border.Child = sp;
            panel.Children.Add(border);
        }

        private void ShowCheckPlaceholder()
        {
            ShowPlaceholder(checkResultPanel, "🔍 Kết quả kiểm tra", "Nhập số nguyên dương ở cột bên trái và bấm 'Kiểm tra' để xem chi tiết tính chất nguyên tố.");
        }

        private void ShowFactorPlaceholder()
        {
            ShowPlaceholder(factorResultPanel, "🧮 Phân tích thừa số", "Nhập số nguyên dương ≥ 2 ở cột bên trái và bấm 'Phân tích' để xem các bước phân tích ra thừa số nguyên tố.");
        }

        private void ShowGcdPlaceholder()
        {
            ShowPlaceholder(gcdResultPanel, "🔗 Kết quả ƯCLN & BCNN", "Nhập hai số nguyên dương A và B ở cột bên trái để xem ước chung lớn nhất, bội chung nhỏ nhất và các bước giải chi tiết.");
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
                        Icon = "🔐",
                        Title = isVN ? "Mật mã học & Bảo mật RSA" : "Cryptography & RSA Security",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_prime_1_{suffix}.png",
                        Description = isVN 
                            ? "Mật mã hóa khóa công khai RSA hoạt động dựa trên tính chất cực kỳ khó phân tích tích số của hai số nguyên tố rất lớn thành các nhân tử nguyên tố ban đầu." 
                            : "RSA public key cryptography relies on the extreme difficulty of factoring the product of two very large prime numbers back into their original prime factors."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🦟",
                        Title = isVN ? "Chu kỳ sinh trưởng của Ve sầu" : "Cicada Lifecycle",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_prime_2_{suffix}.png",
                        Description = isVN 
                            ? "Loài ve sầu Magicicada sinh trưởng dưới lòng đất và ngoi lên theo chu kỳ 13 hoặc 17 năm (đều là số nguyên tố) nhằm tránh gặp chu kỳ sinh trưởng của các loài ký sinh và săn mồi." 
                            : "Magicicada cicadas emerge from underground in cycles of 13 or 17 years (both prime numbers) to avoid sync with the biological cycles of their predators and parasites."
                    },
                    new PracticalAppItem
                    {
                        Icon = "⚙️",
                        Title = isVN ? "Thiết kế Bánh răng cơ khí" : "Mechanical Gear Design",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_prime_3_{suffix}.png",
                        Description = isVN 
                            ? "Khi thiết kế hai bánh răng ăn khớp, kỹ sư chọn số răng của một bánh là số nguyên tố để đảm bảo các răng tiếp xúc luân phiên đều nhau, giảm thiểu mài mòn cục bộ." 
                            : "When designing meshing gears, engineers choose a prime number of teeth for one gear to ensure contact points rotate evenly, minimizing localized wear."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🌐",
                        Title = isVN ? "An toàn Internet & HTTPS/SSL" : "Internet Security & HTTPS/SSL",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_prime_4_{suffix}.png",
                        Description = isVN 
                            ? "Mọi giao dịch trực tuyến bảo mật, truy cập HTTPS, và chữ ký điện tử đều sử dụng các giao thức khóa được xây dựng trực tiếp từ các thuật toán số nguyên tố lớn." 
                            : "All secure online transactions, HTTPS access, and digital signatures use key exchange protocols built directly upon large prime number algorithms."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🎵",
                        Title = isVN ? "Nhịp điệu và Âm nhạc" : "Musical Rhythm",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_prime_5_{suffix}.png",
                        Description = isVN 
                            ? "Trong soạn nhạc, việc lặp lại các vòng lặp tiết tấu có độ dài là số nguyên tố (3, 5, 7, 11) tạo ra cấu trúc polyrhythm (đa nhịp điệu) phức tạp, không bao giờ trùng khớp sớm." 
                            : "In composition, repeating rhythmic loops of prime lengths (3, 5, 7, 11) creates complex polyrhythms that avoid matching up too early, adding depth."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📡",
                        Title = isVN ? "Phân tán tín hiệu Mảng Ăng-ten" : "Antenna Arrays & Signal Dispersion",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_prime_6_{suffix}.png",
                        Description = isVN 
                            ? "Trong viễn thông, mảng phát ăng-ten phân bố khoảng cách theo các số nguyên tố giúp định hướng búp sóng tối ưu và hạn chế nhiễu chồng chéo điện từ giữa các nguồn." 
                            : "In telecommunications, antenna array spacing based on prime numbers helps optimize beamforming and minimizes electromagnetic interference between sources."
                    }
                ,
                    new PracticalAppItem
                    {
                        Icon = "🛡️",
                        Title = isVN ? "Blockchain & Hợp đồng thông minh" : "Blockchain & Smart Contracts",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_prime_7_{suffix}.png",
                        Description = isVN 
                            ? "Mật mã học khóa công khai dựa trên số nguyên tố lớn để xác thực giao dịch phi tập trung an toàn." 
                            : "Public key cryptography based on large prime numbers secures decentralized transactions."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🔑",
                        Title = isVN ? "Tạo khóa bảo mật RSA" : "RSA Cryptographic Key Generation",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_prime_8_{suffix}.png",
                        Description = isVN 
                            ? "Nhân hai số nguyên tố cực lớn để tạo ra cặp khóa công khai và khóa bí mật giúp mã hóa thông tin tài khoản ngân hàng." 
                            : "Multiply two massive prime numbers to generate public-private key pairs that secure bank accounts and sensitive data."
                    }};

                practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading practical images: " + ex.Message);
            }
        }

        private void txtCheckNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            SetTextBorderError(txtCheckNumber, false);
        }

        private void txtFactorNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            SetTextBorderError(txtFactorNumber, false);
        }

        private void SetTextBorderError(TextBox txt, bool isError)
        {
            if (txt == null) return;
            if (isError)
            {
                txt.BorderBrush = new SolidColorBrush(Color.FromRgb(230, 81, 0)); // #E65100
                txt.BorderThickness = new Thickness(0, 0, 0, 3);
            }
            else
            {
                txt.BorderBrush = new SolidColorBrush(Color.FromRgb(159, 168, 218)); // #9FA8DA
                txt.BorderThickness = new Thickness(0, 0, 0, 2);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  PHÂN TÍCH THỪA SỐ NGUYÊN TỐ
        // ═══════════════════════════════════════════════════════════

        private void FactorNumber_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) Factor_Click(sender, e);
        }

        private void Factor_Click(object sender, RoutedEventArgs e)
        {
            DoFactor();
        }

        private void Factor_Click_Border(object sender, MouseButtonEventArgs e)
        {
            DoFactor();
        }

        private void DoFactor()
        {
            factorResultPanel.Children.Clear();
            if (!long.TryParse(txtFactorNumber.Text.Trim(), out long n) || n < 2)
            {
                SetTextBorderError(txtFactorNumber, true);
                UI.ResultRow("⚠️ Nhập số nguyên ≥ 2 (tối đa 15 chữ số)", "#E65100", factorResultPanel);
                return;
            }
            SetTextBorderError(txtFactorNumber, false);

            var factors = Factorize(n);
            string factorStr = string.Join(" × ", factors.Select(f =>
                f.Value == 1 ? $"{f.Key}" : $"{f.Key}^{f.Value}"));

            UI.ResultRow($"📐 {n:N0} = {factorStr}", "#283593", factorResultPanel);

            // Show step by step
            var stepPanel = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 8, 0, 0)
            };
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock
            {
                Text = "📝 Các bước phân tích:",
                FontSize = 14, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(40, 53, 147)),
                Margin = new Thickness(0, 0, 0, 6)
            });

            long remaining = n;
            foreach (var (prime, count) in factors)
            {
                for (int i = 0; i < count; i++)
                {
                    long next = remaining / prime;
                    sp.Children.Add(new TextBlock
                    {
                        Text = $"  {remaining:N0} ÷ {prime} = {next:N0}",
                        FontSize = 14,
                        FontFamily = new FontFamily("Segoe UI"),
                        Margin = new Thickness(0, 1, 0, 1)
                    });
                    remaining = next;
                }
            }
            stepPanel.Child = sp;
            factorResultPanel.Children.Add(stepPanel);

            // Divisors
            var divisors = GetDivisors(n);
            UI.ResultRow($"📋 Số ước: {divisors.Count}", "#424242", factorResultPanel);
            if (divisors.Count <= 50)
                UI.ResultRow($"📋 Ước: {string.Join(", ", divisors)}", "#757575", factorResultPanel);
        }

        // ═══════════════════════════════════════════════════════════
        //  SÀNG ERATOSTHENES
        // ═══════════════════════════════════════════════════════════

        private void Sieve_Changed(object sender, SelectionChangedEventArgs e) => BuildSieve();

        private void BuildSieve()
        {
            if (sievePanel == null || cboSieveMax == null) return;
            sievePanel.Children.Clear();

            int max = cboSieveMax.SelectedIndex switch
            {
                0 => 50,
                2 => 200,
                3 => 500,
                _ => 100
            };

            // Sieve of Eratosthenes
            bool[] isPrime = new bool[max + 1];
            Array.Fill(isPrime, true);
            isPrime[0] = isPrime[1] = false;

            for (int i = 2; i * i <= max; i++)
                if (isPrime[i])
                    for (int j = i * i; j <= max; j += i)
                        isPrime[j] = false;

            int primeCount = 0;
            for (int i = 1; i <= max; i++)
            {
                if (i >= 2 && isPrime[i]) primeCount++;

                bool isOne = (i == 1);
                var cell = new Border
                {
                    Width = max <= 100 ? 42 : 36,
                    Height = max <= 100 ? 36 : 30,
                    Margin = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Background = isOne
                        ? new SolidColorBrush(Color.FromRgb(224, 224, 224))
                        : (isPrime[i]
                            ? new SolidColorBrush(Color.FromRgb(40, 53, 147))
                            : new SolidColorBrush(Color.FromRgb(245, 245, 245))),
                    Cursor = Cursors.Hand,
                    ToolTip = isOne
                        ? "1 — Không phải số nguyên tố cũng không phải hợp số"
                        : (isPrime[i] ? $"{i} — Số nguyên tố" : $"{i} — Hợp số")
                };

                cell.Child = new TextBlock
                {
                    Text = $"{i}",
                    FontSize = max <= 100 ? 12 : 9,
                    FontWeight = (!isOne && isPrime[i]) ? FontWeights.Bold : FontWeights.Normal,
                    Foreground = isOne
                        ? new SolidColorBrush(Color.FromRgb(158, 158, 158))
                        : (isPrime[i] ? Brushes.White : new SolidColorBrush(Color.FromRgb(117, 117, 117))),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                sievePanel.Children.Add(cell);
            }

            txtPrimeCount.Text = $"Tìm thấy {primeCount} số nguyên tố trong [1, {max}]";
        }

        // ═══════════════════════════════════════════════════════════
        //  ƯCLN & BCNN
        // ═══════════════════════════════════════════════════════════

        private void Gcd_Changed(object sender, TextChangedEventArgs e)
        {
            if (gcdResultPanel == null) return;
            gcdResultPanel.Children.Clear();

            bool hasA = !string.IsNullOrWhiteSpace(txtGcdA?.Text);
            bool hasB = !string.IsNullOrWhiteSpace(txtGcdB?.Text);
            
            SetTextBorderError(txtGcdA, false);
            SetTextBorderError(txtGcdB, false);

            if (!hasA && !hasB)
            {
                ShowPlaceholder(gcdResultPanel, "🔗 Kết quả ƯCLN & BCNN", "Nhập hai số nguyên dương A và B ở cột bên trái để xem ước chung lớn nhất, bội chung nhỏ nhất và các bước giải chi tiết.");
                return;
            }

            if (!long.TryParse(txtGcdA?.Text, out long a) || !long.TryParse(txtGcdB?.Text, out long b) || a < 1 || b < 1)
            {
                bool parseA = long.TryParse(txtGcdA?.Text, out long valA) && valA >= 1;
                bool parseB = long.TryParse(txtGcdB?.Text, out long valB) && valB >= 1;
                if (!parseA && hasA) SetTextBorderError(txtGcdA, true);
                if (!parseB && hasB) SetTextBorderError(txtGcdB, true);

                UI.ResultRow("⚠️ Vui lòng nhập 2 số nguyên dương ≥ 1 (tối đa 15 chữ số)", "#E65100", gcdResultPanel);
                return;
            }

            long gcd = Gcd(a, b);
            long lcm = 0;
            bool overflow = false;
            try
            {
                checked
                {
                    lcm = (a / gcd) * b;
                }
            }
            catch (OverflowException)
            {
                overflow = true;
            }

            UI.ResultRow($"📐 ƯCLN({a:N0}, {b:N0}) = {gcd:N0}", "#283593", gcdResultPanel);
            if (!overflow)
                UI.ResultRow($"📐 BCNN({a:N0}, {b:N0}) = {lcm:N0}", "#1565C0", gcdResultPanel);
            else
                UI.ResultRow($"📐 BCNN({a:N0}, {b:N0}) = ⚠️ Vượt quá giới hạn tính toán", "#C62828", gcdResultPanel);

            // 1. Show Prime Factorization Method (for small numbers <= 1,000,000)
            if (a <= 1000000 && b <= 1000000)
            {
                var factA = Factorize(a);
                var factB = Factorize(b);

                string factStrA = string.Join(" × ", factA.Select(f => f.Value == 1 ? $"{f.Key}" : $"{f.Key}^{f.Value}"));
                string factStrB = string.Join(" × ", factB.Select(f => f.Value == 1 ? $"{f.Key}" : $"{f.Key}^{f.Value}"));

                var primeStepPanel = new Border
                {
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 8, 0, 0)
                };
                var psp = new StackPanel();
                psp.Children.Add(new TextBlock
                {
                    Text = "📝 Phương pháp Phân tích thừa số nguyên tố:",
                    FontSize = 14, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(40, 53, 147)),
                    Margin = new Thickness(0, 0, 0, 6)
                });

                psp.Children.Add(new TextBlock { Text = $"  • Phân tích: {a:N0} = {factStrA}", FontSize = 14, FontFamily = new FontFamily("Segoe UI"), Margin = new Thickness(0, 1, 0, 1) });
                psp.Children.Add(new TextBlock { Text = $"  • Phân tích: {b:N0} = {factStrB}", FontSize = 14, FontFamily = new FontFamily("Segoe UI"), Margin = new Thickness(0, 1, 0, 1) });

                var keysA = factA.Keys.ToList();
                var keysB = factB.Keys.ToList();
                var commonPrimes = keysA.Intersect(keysB).ToList();
                var allPrimes = keysA.Union(keysB).ToList();

                if (commonPrimes.Count > 0)
                {
                    psp.Children.Add(new TextBlock { Text = $"  • Thừa số nguyên tố chung: {string.Join(", ", commonPrimes)}", FontSize = 14, FontFamily = new FontFamily("Segoe UI"), Margin = new Thickness(0, 1, 0, 1) });
                    
                    var gcdTermList = new List<string>();
                    foreach (var p in commonPrimes)
                    {
                        int exp = System.Math.Min(factA[p], factB[p]);
                        gcdTermList.Add(exp == 1 ? $"{p}" : $"{p}^{exp}");
                    }
                    psp.Children.Add(new TextBlock { Text = $"  ⇒ ƯCLN = Tích các thừa số chung với số mũ nhỏ nhất: {string.Join(" × ", gcdTermList)} = {gcd:N0}", FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50)), Margin = new Thickness(0, 2, 0, 2) });
                }
                else
                {
                    psp.Children.Add(new TextBlock { Text = "  • Không có thừa số nguyên tố chung (Hai số nguyên tố cùng nhau)", FontSize = 14, FontFamily = new FontFamily("Segoe UI"), Margin = new Thickness(0, 1, 0, 1) });
                    psp.Children.Add(new TextBlock { Text = $"  ⇒ ƯCLN = 1", FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50)), Margin = new Thickness(0, 2, 0, 2) });
                }

                var lcmTermList = new List<string>();
                foreach (var p in allPrimes)
                {
                    int expA = factA.GetValueOrDefault(p, 0);
                    int expB = factB.GetValueOrDefault(p, 0);
                    int exp = System.Math.Max(expA, expB);
                    lcmTermList.Add(exp == 1 ? $"{p}" : $"{p}^{exp}");
                }
                
                if (!overflow)
                {
                    psp.Children.Add(new TextBlock { Text = $"  ⇒ BCNN = Tích thừa số chung và riêng với số mũ lớn nhất: {string.Join(" × ", lcmTermList)} = {lcm:N0}", FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(21, 101, 192)), Margin = new Thickness(0, 2, 0, 2) });
                }
                else
                {
                    psp.Children.Add(new TextBlock { Text = "  ⇒ BCNN = Vượt quá giới hạn tính toán.", FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40)), Margin = new Thickness(0, 2, 0, 2) });
                }

                primeStepPanel.Child = psp;
                gcdResultPanel.Children.Add(primeStepPanel);
            }

            // 2. Show Euclidean steps (always shown for completeness)
            var stepPanel = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 8, 0, 0)
            };
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock
            {
                Text = "📝 Phương pháp Thuật toán Euclid (Chia liên tiếp):",
                FontSize = 14, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(40, 53, 147)),
                Margin = new Thickness(0, 0, 0, 6)
            });

            long x = a, y = b;
            while (y != 0)
            {
                sp.Children.Add(new TextBlock
                {
                    Text = $"  {x:N0} = {y:N0} × {x / y} + {x % y}",
                    FontSize = 14, FontFamily = new FontFamily("Segoe UI"),
                    Margin = new Thickness(0, 1, 0, 1)
                });
                long temp = x % y;
                x = y;
                y = temp;
            }
            sp.Children.Add(new TextBlock
            {
                Text = $"  ⇒ ƯCLN = {x:N0}",
                FontSize = 14, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(40, 53, 147)),
                Margin = new Thickness(0, 4, 0, 0)
            });
            stepPanel.Child = sp;
            gcdResultPanel.Children.Add(stepPanel);
        }

        // ═══════════════════════════════════════════════════════════
        //  MATH HELPERS
        // ═══════════════════════════════════════════════════════════

        private static bool IsPrime(long n)
        {
            if (n < 2) return false;
            if (n < 4) return true;
            if (n % 2 == 0 || n % 3 == 0) return false;
            for (long i = 5; i <= n / i; i += 6)
                if (n % i == 0 || n % (i + 2) == 0) return false;
            return true;
        }

        private static Dictionary<long, int> Factorize(long n)
        {
            var factors = new Dictionary<long, int>();
            for (long d = 2; d <= n / d; d++)
            {
                while (n % d == 0)
                {
                    factors[d] = factors.GetValueOrDefault(d) + 1;
                    n /= d;
                }
            }
            if (n > 1) factors[n] = 1;
            return factors;
        }

        private static List<long> GetDivisors(long n)
        {
            var divs = new List<long>();
            for (long i = 1; i <= n / i; i++)
            {
                if (n % i == 0)
                {
                    divs.Add(i);
                    if (i != n / i) divs.Add(n / i);
                }
            }
            divs.Sort();
            return divs;
        }

        private static long Gcd(long a, long b)
        {
            while (b != 0) { long t = b; b = a % b; a = t; }
            return a;
        }

        private static int CountPrimesUpTo(long n)
        {
            int count = 0;
            for (long i = 2; i <= n; i++)
                if (IsPrime(i)) count++;
            return count;
        }

        private static long NextPrime(long n)
        {
            if (n >= 9223372036854775783) return 0;
            long p = n + 1;
            while (!IsPrime(p)) p++;
            return p;
        }

        private static long PrevPrime(long n)
        {
            if (n <= 2) return 0;
            long p = n - 1;
            while (p > 1 && !IsPrime(p)) p--;
            return p;
        }

        /* AddResult / AddR replaced by UI.ResultRow */
        private void ResetAll_Click(object sender, MouseButtonEventArgs e)
        {
            if (txtCheckNumber != null) txtCheckNumber.Text = "";
            if (txtFactorNumber != null) txtFactorNumber.Text = "";
            if (txtGcdA != null) txtGcdA.Text = "";
            if (txtGcdB != null) txtGcdB.Text = "";
            
            ShowCheckPlaceholder();
            ShowFactorPlaceholder();
            ShowGcdPlaceholder();
        }

        // ═══ Graph — Prime Number Scatter Plot ═══
        private void OpenGraph_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var ci = CultureInfo.InvariantCulture;
                int max = cboSieveMax?.SelectedIndex switch
                {
                    0 => 50,
                    2 => 200,
                    3 => 500,
                    _ => 100
                };

                // Collect primes and composites
                var primes = new List<int>();
                var composites = new List<int>();
                for (int i = 2; i <= max; i++)
                {
                    if (IsPrime(i)) primes.Add(i);
                    else composites.Add(i);
                }

                // Build list expressions for Graph
                var pX = string.Join(",", primes);
                var pY = string.Join(",", primes.Select((_, idx) => (idx + 1).ToString()));
                var cX = string.Join(",", composites);
                var cY = string.Join(",", composites.Select((_, idx) => (idx + 1).ToString()));

                int maxY = System.Math.Max(primes.Count, composites.Count);

                string expJs = $@"
        calc.setExpression({{id:'primes', latex:'([{pX}],[{pY}])', color:'#283593', pointSize:6, pointStyle:'POINT', label:'Số nguyên tố', showLabel:true}});
        calc.setExpression({{id:'comps', latex:'([{cX}],[{cY}])', color:'#E0E0E0', pointSize:3, pointStyle:'POINT'}});
        calc.setMathBounds({{left:0, right:{max + 5}, bottom:0, top:{maxY + 5}}});";

                var win = new GraphWindow(expJs, $"🔍 Số nguyên tố đến {max} ({primes.Count} số)");
                win.Owner = Window.GetWindow(this);
                win.Show();
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}"); }
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewCheck == null || viewFactor == null || viewSieve == null || viewGcdLCM == null || viewPractical == null)
                return;

            viewGuide.Visibility = Visibility.Collapsed;
            viewCheck.Visibility = Visibility.Collapsed;
            viewFactor.Visibility = Visibility.Collapsed;
            viewSieve.Visibility = Visibility.Collapsed;
            viewGcdLCM.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            

            switch (sideMenu.SelectedIndex)
            {
                case 0:
                    viewGuide.Visibility = Visibility.Visible;
                    break;
                case 1:
                    viewCheck.Visibility = Visibility.Visible;
                    break;
                case 2:
                    viewFactor.Visibility = Visibility.Visible;
                    break;
                case 3:
                    viewSieve.Visibility = Visibility.Visible;
                    break;
                case 4:
                    viewGcdLCM.Visibility = Visibility.Visible;
                    break;
                case 5:
                    viewPractical.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}


