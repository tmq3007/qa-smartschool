using System;
using QASmartClass.LearningTools.Helpers;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QASmartClass.LearningTools.Controls;
using QASmartClass.LearningTools.Models;

namespace QASmartClass.LearningTools.Views.Science
{
    public partial class WaveSpeedTool : BaseToolControl
    {
        private bool _updating;

        // Unit multipliers to SI
        private static readonly double[] FreqMul = { 1, 1e3, 1e6, 1e9, 1e12 };
        private static readonly double[] WaveMul = { 1, 0.01, 0.001, 1e-6, 1e-9, 1000 };
        private static readonly double[] VelMul = { 1, 1.0 / 3.6, 1000 };

        public WaveSpeedTool()
        {
            InitializeComponent();
            // Attach TouchNumPad
            QASmartClass.LearningTools.Controls.TouchNumPad.Attach(txtFreq, step: 1);
            QASmartClass.LearningTools.Controls.TouchNumPad.Attach(txtWavelength, step: 1);
            QASmartClass.LearningTools.Controls.TouchNumPad.Attach(txtVelocity, step: 1);
            QASmartClass.LearningTools.Controls.TouchNumPad.Attach(txtSourceSpeed, step: 1);
            QASmartClass.LearningTools.Controls.TouchNumPad.Attach(txtObserverSpeed, step: 1);

            Loaded += (_, _) => {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
                if (menuTextCalculator != null) menuTextCalculator.Text = isVN ? "Tính toán" : "Calculator";
                if (menuTextSpectrum != null) menuTextSpectrum.Text = isVN ? "Phổ sóng" : "Wave Spectrum";
                if (menuTextConstants != null) menuTextConstants.Text = isVN ? "Hằng số" : "Constants";
                if (menuTextMedium != null) menuTextMedium.Text = isVN ? "Môi trường" : "Medium";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Real-world Applications";

                BuildPresets(); BuildConstants(); BuildSpectrum(); BuildMedium(); LoadPracticalApps();
            };
        }

        // ═══ UNIT HELPERS ═══
        private double GetFreqHz()
        {
            if (!ParsingHelper.TryParseDouble(txtFreq?.Text, out double v) || v <= 0) return -1;
            return v * FreqMul[cmbFreqUnit?.SelectedIndex ?? 0];
        }
        private double GetWaveM()
        {
            if (!ParsingHelper.TryParseDouble(txtWavelength?.Text, out double v) || v <= 0) return -1;
            return v * WaveMul[cmbWaveUnit?.SelectedIndex ?? 0];
        }
        private double GetVelMs()
        {
            if (!ParsingHelper.TryParseDouble(txtVelocity?.Text, out double v) || v <= 0) return -1;
            return v * VelMul[cmbVelUnit?.SelectedIndex ?? 0];
        }

        private void WaveType_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            Wave_Changed(sender, EventArgs.Empty);
        }

        // ═══ CALCULATOR ═══
        private void Wave_Changed(object sender, EventArgs e)
        {
            if (_updating || waveResultPanel == null || !IsLoaded) return;
            waveResultPanel.Children.Clear();

            double f = GetFreqHz(), l = GetWaveM(), v = GetVelMs();
            bool hF = f > 0, hL = l > 0, hV = v > 0;

            bool isEMWave = cmbWaveType?.SelectedIndex == 1;

            if (hF && hL)
            {
                double vel = f * l;
                UI.ResultRow($"v = f × λ = {FormatSI(f, "Hz")} × {FormatSI(l, "m")} = {FormatSI(vel, "m/s")}", DS.ResultPrimary, waveResultPanel);
                UI.ResultRow($"Chu kỳ T = 1/f = {FormatSI(1.0 / f, "s")}", DS.ResultDark, waveResultPanel);
                if (isEMWave)
                {
                    UI.ResultRow($"Năng lượng lượng tử photon E = h·f = {FormatToPedagogicalScientific(6.626e-34 * f)} J", DS.ResultSpecial, waveResultPanel);
                }
            }
            else if (hF && hV)
            {
                double wl = v / f;
                UI.ResultRow($"λ = v/f = {FormatSI(v, "m/s")} / {FormatSI(f, "Hz")} = {FormatSI(wl, "m")}", DS.ResultSuccess, waveResultPanel);
                UI.ResultRow($"Chu kỳ T = 1/f = {FormatSI(1.0 / f, "s")}", DS.ResultDark, waveResultPanel);
                if (isEMWave)
                {
                    UI.ResultRow($"Năng lượng lượng tử photon E = h·f = {FormatToPedagogicalScientific(6.626e-34 * f)} J", DS.ResultSpecial, waveResultPanel);
                }
            }
            else if (hL && hV)
            {
                double freq = v / l;
                UI.ResultRow($"f = v/λ = {FormatSI(v, "m/s")} / {FormatSI(l, "m")} = {FormatSI(freq, "Hz")}", DS.ResultWarning, waveResultPanel);
                UI.ResultRow($"Chu kỳ T = 1/f = {FormatSI(1.0 / freq, "s")}", DS.ResultDark, waveResultPanel);
                if (isEMWave)
                {
                    UI.ResultRow($"Năng lượng lượng tử photon E = h·f = {FormatToPedagogicalScientific(6.626e-34 * freq)} J", DS.ResultSpecial, waveResultPanel);
                }
            }

            // Also calc Doppler if source/observer fields have values
            CalcDoppler();
        }

        // ═══ DOPPLER ═══
        private void Doppler_Changed(object sender, TextChangedEventArgs e) => CalcDoppler();

        private void CalcDoppler()
        {
            if (dopplerResultPanel == null || !IsLoaded) return;
            dopplerResultPanel.Children.Clear();

            double f = GetFreqHz(), v = GetVelMs();
            if (f <= 0 && v <= 0) return;

            // Need f and v for Doppler
            double l = GetWaveM();
            if (f <= 0 && l > 0 && v > 0) f = v / l;
            if (v <= 0 && f > 0 && l > 0) v = f * l;
            if (f <= 0 || v <= 0) return;

            bool hasSrc = ParsingHelper.TryParseDouble(txtSourceSpeed?.Text, out double vs);
            bool hasObs = ParsingHelper.TryParseDouble(txtObserverSpeed?.Text, out double vo);
            if (!hasSrc || vs < 0) vs = 0;
            if (!hasObs || vo < 0) vo = 0;

            // Enforce vs and vo in the TextBoxes to be non-negative if they were negative
            if (hasSrc && vs == 0 && txtSourceSpeed.Text != "0" && txtSourceSpeed.Text != "") txtSourceSpeed.Text = "0";
            if (hasObs && vo == 0 && txtObserverSpeed.Text != "0" && txtObserverSpeed.Text != "") txtObserverSpeed.Text = "0";

            if (vs >= v)
            {
                if (System.Math.Abs(vs - v) < 0.001)
                {
                    UI.ResultRow("⚠️ Nguồn di chuyển bằng tốc độ sóng: Hiện tượng Bức tường âm thanh (Sound Barrier). Sóng âm bị nén cực đại ở phía trước.", DS.ResultWarning, dopplerResultPanel);
                }
                else
                {
                    UI.ResultRow($"⚠️ Nguồn di chuyển siêu thanh (v_s > v): Tạo ra Sóng xung kích (Shockwave / Sonic Boom) hình nón Mach phía sau nguồn.", DS.ResultDanger, dopplerResultPanel);
                }
            }
            else
            {
                double fApproach = f * (v + vo) / (v - vs);
                UI.ResultRow($"🔴 Lại gần: f' = {FormatSI(fApproach, "Hz")} ({FormatVietnamese((fApproach / f) * 100)}% so với gốc)", DS.ResultDanger, dopplerResultPanel);
            }

            if (vo < v)
            {
                double fRecede = f * (v - vo) / (v + vs);
                if (fRecede > 0)
                {
                    UI.ResultRow($"🔵 Ra xa: f' = {FormatSI(fRecede, "Hz")} ({FormatVietnamese((fRecede / f) * 100)}% so với gốc)", DS.ResultPrimary, dopplerResultPanel);
                }
            }
            else
            {
                UI.ResultRow("ℹ️ Người quan sát đi xa nhanh hơn hoặc bằng tốc độ sóng: Không thể nhận được sóng đuổi theo từ phía sau.", DS.ResultInfo, dopplerResultPanel);
            }
        }

        // ═══ RESET ═══
        private void ResetAll_Click(object sender, MouseButtonEventArgs e)
        {
            if (sideMenu != null && sideMenu.SelectedIndex != 1)
            {
                sideMenu.SelectedIndex = 0;
            }
            _updating = true;
            txtFreq.Text = ""; txtWavelength.Text = ""; txtVelocity.Text = "";
            txtSourceSpeed.Text = ""; txtObserverSpeed.Text = "0";
            cmbFreqUnit.SelectedIndex = 0; cmbWaveUnit.SelectedIndex = 0; cmbVelUnit.SelectedIndex = 0;
            if (cmbWaveType != null) cmbWaveType.SelectedIndex = 0;
            _updating = false;
            waveResultPanel?.Children.Clear();
            dopplerResultPanel?.Children.Clear();
        }

        // ═══ PRESETS ═══
        private void BuildPresets()
        {
            if (presetPanel == null) return;
            var presets = new (string Label, string F, int Fi, string L, int Li, string V, int Vi, int Type, string Color)[]
            {
                ("🌟 Ánh sáng", "4.3e14", 0, "700", 4, "299792458", 0, 1, "#1565C0"),
                ("🔊 Âm thanh (trong thép)", "1000", 0, "", 0, "5960", 0, 0, "#455A64"),
                ("📻 FM Radio 100MHz", "100", 2, "", 0, "299792458", 0, 1, "#6A1B9A"),
                ("📶 Wi-Fi 2.4GHz", "2.4", 3, "", 0, "299792458", 0, 1, "#E65100"),
                ("🌊 Siêu âm y tế", "5", 2, "", 0, "1540", 0, 0, "#00838F"),
                ("🎵 Nốt La (A4)", "440", 0, "", 0, "343", 0, 0, "#AD1457"),
            };
            foreach (var (label, freq, fi, wl, wi, vel, vi, type, color) in presets)
            {
                var c = (Color)ColorConverter.ConvertFromString(color);
                string cf = freq; int cfi = fi; string cwl = wl; int cwi = wi; string cv = vel; int cvi = vi; int ctype = type;
                UI.PresetButton(label, c, () =>
                {
                    _updating = true;
                    txtFreq.Text = cf; cmbFreqUnit.SelectedIndex = cfi;
                    txtWavelength.Text = cwl; cmbWaveUnit.SelectedIndex = cwi;
                    txtVelocity.Text = cv; cmbVelUnit.SelectedIndex = cvi;
                    if (cmbWaveType != null) cmbWaveType.SelectedIndex = ctype;
                    _updating = false;
                    Wave_Changed(this, EventArgs.Empty);
                }, presetPanel);
            }
        }

        // ═══ SPECTRUM (Tab 2) ═══
        private static readonly (string Name, string Range, Color C)[] SpectrumBands =
        {
            ("Sóng radio", "λ > 1 m", Color.FromRgb(139, 69, 19)),
            ("Vi sóng", "1 mm – 1 m", Color.FromRgb(255, 140, 0)),
            ("Hồng ngoại", "760 nm – 1 mm", Color.FromRgb(220, 20, 60)),
            ("Ánh sáng đỏ", "640–760 nm", Color.FromRgb(255, 0, 0)),
            ("Ánh sáng cam", "590–640 nm", Color.FromRgb(255, 127, 0)),
            ("Ánh sáng vàng", "570–590 nm", Color.FromRgb(255, 255, 0)),
            ("Ánh sáng lục", "495–570 nm", Color.FromRgb(0, 200, 0)),
            ("Ánh sáng lam", "450–495 nm", Color.FromRgb(0, 100, 255)),
            ("Ánh sáng chàm", "430–450 nm", Color.FromRgb(75, 0, 130)),
            ("Ánh sáng tím", "380–430 nm", Color.FromRgb(128, 0, 128)),
            ("Tia cực tím", "10–380 nm", Color.FromRgb(106, 27, 154)),
            ("Tia X", "0.01–10 nm", Color.FromRgb(50, 50, 50)),
            ("Tia gamma", "< 0.01 nm", Color.FromRgb(20, 20, 20)),
        };

        private void BuildSpectrum()
        {
            if (spectrumPanel == null) return;
            spectrumPanel.Children.Clear();
            spectrumDetailsPanel?.Children.Clear();

            spectrumPanel.Children.Add(new TextBlock
            {
                Text = "🌈 Phổ Sóng Điện Từ", FontSize = 15, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(21, 101, 192)), Margin = new Thickness(0, 0, 0, 10)
            });

            // Visual bar
            var barGrid = new Grid { Height = 48, Margin = new Thickness(0, 0, 0, 6) };
            for (int i = 0; i < SpectrumBands.Length; i++)
                barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            for (int i = 0; i < SpectrumBands.Length; i++)
            {
                var (name, range, color) = SpectrumBands[i];
                var cell = new Border
                {
                    Background = new SolidColorBrush(color),
                    CornerRadius = i == 0 ? new CornerRadius(8, 0, 0, 8) : i == SpectrumBands.Length - 1 ? new CornerRadius(0, 8, 8, 0) : new CornerRadius(0),
                    ToolTip = $"{name}\n{range}"
                };
                Grid.SetColumn(cell, i);
                barGrid.Children.Add(cell);
            }
            spectrumPanel.Children.Add(barGrid);

            // Labels
            var labelRow = new Grid();
            labelRow.ColumnDefinitions.Add(new ColumnDefinition());
            labelRow.ColumnDefinitions.Add(new ColumnDefinition());
            labelRow.ColumnDefinitions.Add(new ColumnDefinition());
            AddLabel(labelRow, "📻 Sóng vô tuyến (Radio)", 0, HorizontalAlignment.Left);
            AddLabel(labelRow, "👁️ Ánh sáng nhìn thấy", 1, HorizontalAlignment.Center);
            AddLabel(labelRow, "☢️ Bức xạ ion hóa", 2, HorizontalAlignment.Right);
            spectrumPanel.Children.Add(labelRow);

            // Detail cards
            spectrumDetailsPanel?.Children.Add(new TextBlock
            {
                Text = "📋 Chi tiết các vùng phổ", FontSize = 14, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(21, 101, 192)), Margin = new Thickness(0, 0, 0, 8)
            });

            var wrap = new WrapPanel();
            foreach (var (name, range, color) in SpectrumBands)
            {
                var card = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(25, color.R, color.G, color.B)),
                    BorderBrush = new SolidColorBrush(color), BorderThickness = new Thickness(0, 0, 0, 3),
                    CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 0, 8, 8), MinWidth = 155
                };
                var sp = new StackPanel();
                sp.Children.Add(new TextBlock { Text = name, FontSize = 12, FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(color) });
                sp.Children.Add(new TextBlock { Text = range, FontSize = 11, FontFamily = new FontFamily("Segoe UI"),
                    Foreground = new SolidColorBrush(Color.FromRgb(97, 97, 97)) });
                card.Child = sp;
                wrap.Children.Add(card);
            }
            spectrumDetailsPanel?.Children.Add(wrap);
        }

        private static void AddLabel(Grid g, string text, int col, HorizontalAlignment ha)
        {
            var tb = new TextBlock { Text = text, FontSize = 10, FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = ha, Foreground = new SolidColorBrush(Color.FromRgb(97, 97, 97)),
                Margin = new Thickness(0, 4, 0, 0) };
            Grid.SetColumn(tb, col);
            g.Children.Add(tb);
        }

        // ═══ CONSTANTS (Tab 3) ═══
        private void BuildConstants()
        {
            if (constantsPanel == null) return;
            constantsPanel.Children.Clear();

            var categories = new (string Title, string Icon, string Color, (string N, string V, string Note)[] Items)[]
            {
                ("Tốc độ truyền sóng", "🚀", "#1565C0", new[]
                {
                    ("Ánh sáng (chân không)", "c = 299.792.458 m/s", "≈ 3 · 10⁸ m/s"),
                    ("Âm thanh (không khí 20°C)", "v = 343 m/s", "≈ 1.235 km/h"),
                    ("Âm thanh (nước)", "v ≈ 1.480 m/s", ""),
                    ("Âm thanh (thép)", "v ≈ 5.960 m/s", ""),
                }),
                ("Sóng điện từ", "📡", "#6A1B9A", new[]
                {
                    ("Sóng AM radio", "530 – 1700 kHz", "λ ≈ 176 – 566 m"),
                    ("Sóng FM radio", "87.5 – 108 MHz", "λ ≈ 2.8 – 3.4 m"),
                    ("Wi-Fi 2.4 GHz", "2.4 · 10⁹ Hz", "λ ≈ 12,5 cm"),
                    ("Wi-Fi 5 GHz", "5 · 10⁹ Hz", "λ ≈ 6 cm"),
                }),
                ("Ánh sáng nhìn thấy", "🌈", "#2E7D32", new[]
                {
                    ("Ánh sáng đỏ", "λ ≈ 760 nm", "f ≈ 4,0 · 10¹⁴ Hz"),
                    ("Ánh sáng cam", "λ ≈ 600 nm", "f ≈ 5,0 · 10¹⁴ Hz"),
                    ("Ánh sáng vàng", "λ ≈ 580 nm", "f ≈ 5,2 · 10¹⁴ Hz"),
                    ("Ánh sáng lục", "λ ≈ 530 nm", "f ≈ 5,7 · 10¹⁴ Hz"),
                    ("Ánh sáng lam", "λ ≈ 470 nm", "f ≈ 6,4 · 10¹⁴ Hz"),
                    ("Ánh sáng chàm", "λ ≈ 440 nm", "f ≈ 6,8 · 10¹⁴ Hz"),
                    ("Ánh sáng tím", "λ ≈ 380 nm", "f ≈ 7,9 · 10¹⁴ Hz"),
                }),
                ("Bức xạ năng lượng cao", "☢️", "#C62828", new[]
                {
                    ("Tia cực tím", "λ ≈ 10 – 380 nm", "gây cháy nắng"),
                    ("Tia X", "λ ≈ 0.01 – 10 nm", "chụp X-quang"),
                    ("Tia gamma", "λ < 0.01 nm", "năng lượng cao nhất"),
                }),
                ("Thính giác con người", "👂", "#E65100", new[]
                {
                    ("Tần số nghe được", "16 Hz → 20.000 Hz", ""),
                    ("Hạ âm (infrasound)", "< 16 Hz", "động đất, voi"),
                    ("Siêu âm (ultrasound)", "> 20 kHz", "dơi, y tế, sonar"),
                }),
            };

            foreach (var (title, icon, colorHex, items) in categories)
            {
                var c = (Color)ColorConverter.ConvertFromString(colorHex);

                // Category header
                var header = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(20, c.R, c.G, c.B)),
                    CornerRadius = new CornerRadius(10, 10, 0, 0),
                    Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 12, 0, 0)
                };
                header.Child = new TextBlock
                {
                    Text = $"{icon} {title}", FontSize = 14, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(c)
                };
                constantsPanel.Children.Add(header);

                // Items
                foreach (var (name, value, note) in items)
                {
                    var row = new Border
                    {
                        Background = Brushes.White, Padding = new Thickness(14, 8, 14, 8),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(238, 238, 238)),
                        BorderThickness = new Thickness(0, 0, 0, 1), Cursor = Cursors.Hand,
                        ToolTip = "Click để copy"
                    };
                    var sp = new StackPanel();
                    sp.Children.Add(new TextBlock { Text = name, FontSize = 12, FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(c) });
                    sp.Children.Add(new TextBlock { Text = value, FontSize = 12, FontFamily = new FontFamily("Segoe UI"),
                        Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33)) });
                    if (!string.IsNullOrEmpty(note))
                        sp.Children.Add(new TextBlock { Text = note, FontSize = 10, Foreground = Brushes.Gray, FontStyle = FontStyles.Italic });
                    row.Child = sp;
                    string cv = value;
                    row.MouseLeftButtonDown += (_, _) => { try { Clipboard.SetText(cv); } catch { } };
                    constantsPanel.Children.Add(row);
                }
            }
        }

        // ═══ MEDIUM COMPARISON (Tab 4) ═══
        private void BuildMedium()
        {
            if (mediumPanel == null) return;
            mediumPanel.Children.Clear();

            // EM wave section
            mediumPanel.Children.Add(new TextBlock
            {
                Text = "⚡ Tốc độ Sóng điện từ (Ánh sáng) trong các môi trường",
                FontSize = 14, FontWeight = FontWeights.Bold,
                Foreground = DS.Brush(DS.CatScience), Margin = new Thickness(0, 0, 0, 8)
            });

            var emMedia = new (string Name, double Speed, string Color, string Icon)[]
            {
                ("Chân không", 299792458, "#1565C0", "💡"),
                ("Nước", 225000000, "#2196F3", "💧"),
                ("Thủy tinh", 200000000, "#4CAF50", "🔍"),
                ("Kim cương", 124000000, "#00BCD4", "💎")
            };
            RenderMediumGroup(emMedia, 299792458);

            // Mechanical wave section
            mediumPanel.Children.Add(new TextBlock
            {
                Text = "🔊 Tốc độ Sóng cơ học (Âm thanh) trong các môi trường",
                FontSize = 14, FontWeight = FontWeights.Bold,
                Foreground = DS.Brush(DS.ResultSpecial), Margin = new Thickness(0, 16, 0, 8)
            });

            var mechMedia = new (string Name, double Speed, string Color, string Icon)[]
            {
                ("Thép", 5960, "#455A64", "🔩"),
                ("Nhôm", 5100, "#78909C", "⚙️"),
                ("Đá granite", 3950, "#795548", "🪨"),
                ("Nước biển", 1530, "#0277BD", "🌊"),
                ("Nước ngọt", 1480, "#0288D1", "💧"),
                ("Không khí 20°C", 343, "#66BB6A", "🌬️"),
                ("Không khí 0°C", 331, "#81C784", "❄️")
            };
            RenderMediumGroup(mechMedia, 5960);
        }

        private void RenderMediumGroup((string Name, double Speed, string Color, string Icon)[] media, double maxSpeed)
        {
            foreach (var (name, speed, colorHex, icon) in media)
            {
                var c = (Color)ColorConverter.ConvertFromString(colorHex);
                var card = new Border
                {
                    Background = Brushes.White, CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(16, 10, 16, 10), Margin = new Thickness(0, 0, 0, 6),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(238, 238, 238)), BorderThickness = new Thickness(1)
                };
                var g = new Grid();
                g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                // Row 0: name + speed
                var infoPanel = new DockPanel();
                infoPanel.Children.Add(new TextBlock
                {
                    Text = $"{icon} {name}", FontSize = 13, FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(c), VerticalAlignment = VerticalAlignment.Center
                });
                var speedTb = new TextBlock
                {
                    Text = FormatSI(speed, "m/s"), FontSize = 13, FontFamily = DS.FontPrimary,
                    Foreground = DS.Brush(DS.TextPrimary),
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                DockPanel.SetDock(speedTb, Dock.Right);
                infoPanel.Children.Add(speedTb);
                Grid.SetRow(infoPanel, 0);
                g.Children.Add(infoPanel);

                // Row 1: bar
                double ratio = speed / maxSpeed;
                ratio = System.Math.Clamp(ratio, 0.01, 1.0);

                var barBg = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                    CornerRadius = new CornerRadius(3), Height = 6, Margin = new Thickness(0, 6, 0, 0)
                };
                var barFill = new Border
                {
                    Background = new SolidColorBrush(c), CornerRadius = new CornerRadius(3), Height = 6,
                    HorizontalAlignment = HorizontalAlignment.Left, Width = 0
                };
                var barContainer = new Grid();
                barContainer.Children.Add(barBg);
                barContainer.Children.Add(barFill);
                Grid.SetRow(barContainer, 1);
                g.Children.Add(barContainer);

                card.Child = g;
                mediumPanel.Children.Add(card);

                // Animate bar on loaded
                card.Loaded += (_, _) =>
                {
                    barFill.Width = System.Math.Max(barContainer.ActualWidth * ratio, 10);
                };
            }
        }

        // ═══ HELPERS ═══
        // AddResult(), AddDopplerResult(), CopyWithFeedback() đã được thay thế bằng UI.ResultRow()

        private static string FormatSI(double val, string unit)
        {
            if (val == 0) return $"0 {unit}";
            double abs = System.Math.Abs(val);
            if (unit == "m/s")
            {
                if (abs >= 1e6 || abs < 1e-3)
                    return $"{FormatToPedagogicalScientific(val)} {unit}";
                return $"{FormatVietnamese(val)} {unit}";
            }
            if (abs >= 1e12) return $"{FormatVietnamese(val / 1e12)} T{unit}";
            if (abs >= 1e9) return $"{FormatVietnamese(val / 1e9)} G{unit}";
            if (abs >= 1e6) return $"{FormatVietnamese(val / 1e6)} M{unit}";
            if (abs >= 1e3) return $"{FormatVietnamese(val / 1e3)} k{unit}";
            if (abs >= 1) return $"{FormatVietnamese(val)} {unit}";
            if (abs >= 1e-3) return $"{FormatVietnamese(val * 1e3)} m{unit}";
            if (abs >= 1e-6) return $"{FormatVietnamese(val * 1e6)} μ{unit}";
            if (abs >= 1e-9) return $"{FormatVietnamese(val * 1e9)} n{unit}";
            return $"{FormatToPedagogicalScientific(val)} {unit}";
        }

        private void OpenGraph_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                double f = GetFreqHz(), l = GetWaveM(), v = GetVelMs();
                if (f <= 0 && l > 0 && v > 0) f = v / l;
                if (l <= 0 && f > 0 && v > 0) l = v / f;
                if (v <= 0 && f > 0 && l > 0) v = f * l;
                
                if (f <= 0) f = 440; 
                if (v <= 0) v = 343;
                if (l <= 0) l = v / f;

                double k = 2 * System.Math.PI / l;
                var ci = System.Globalization.CultureInfo.InvariantCulture;
                double scale = l * 3;

                string expJs = $@"
        calc.setExpression({{id:'t', latex:'t=0', play:true, sliderBounds: {{ min: 0, max: 100, step: 0.05 }}}});
        calc.setExpression({{id:'wave', latex:'y=\\sin({k.ToString(ci)}x - \\pi * t)', color:'#2E7D32', lineWidth:3}});
        calc.setExpression({{id:'env1', latex:'y=1', color:'#E53935', lineWidth:1, lineStyle:'DASHED'}});
        calc.setExpression({{id:'env2', latex:'y=-1', color:'#E53935', lineWidth:1, lineStyle:'DASHED'}});
        calc.setExpression({{id:'lam', latex:'x={l.ToString(ci)}', color:'#1976D2', lineWidth:2, lineStyle:'DASHED', label:'Bước sóng \\lambda', showLabel:true}});
        calc.setMathBounds({{ left: 0, right: {scale.ToString(ci)}, bottom: -2, top: 2 }});";

                var win = new Math.GraphWindow(expJs, $"🔊 Sóng — f={FormatSI(f,"Hz")}, λ={FormatSI(l,"m")}");
                win.Owner = Window.GetWindow(this);
                win.Show();
            }
            catch (System.Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}"); }
        }

        public static string FormatVietnamese(double value)
        {
            var nfi = new System.Globalization.NumberFormatInfo
            {
                NumberDecimalSeparator = ",",
                NumberGroupSeparator = ""
            };
            return value.ToString("G10", nfi);
        }

        public static string FormatToPedagogicalScientific(double value)
        {
            if (value == 0) return "0";
            double abs = System.Math.Abs(value);
            if (abs >= 1e6 || abs < 1e-4)
            {
                string s = value.ToString("E4", System.Globalization.CultureInfo.InvariantCulture);
                int eIdx = s.IndexOf('E');
                if (eIdx != -1)
                {
                    string mantissa = s.Substring(0, eIdx);
                    string exponent = s.Substring(eIdx + 1);

                    double mantissaVal = double.Parse(mantissa, System.Globalization.CultureInfo.InvariantCulture);
                    string formattedMantissa = FormatVietnamese(mantissaVal);

                    int expVal = int.Parse(exponent, System.Globalization.CultureInfo.InvariantCulture);
                    string superscriptExp = GetSuperscriptString(expVal);

                    return $"{formattedMantissa} \u00B7 10{superscriptExp}";
                }
            }
            return FormatVietnamese(value);
        }

        private static string GetSuperscriptString(int value)
        {
            string s = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string res = "";
            foreach (char c in s)
            {
                res += c switch
                {
                    '-' => "\u207B",
                    '+' => "",
                    '0' => "\u2070",
                    '1' => "\u00B9",
                    '2' => "\u00B2",
                    '3' => "\u00B3",
                    '4' => "\u2074",
                    '5' => "\u2075",
                    '6' => "\u2076",
                    '7' => "\u2077",
                    '8' => "\u2078",
                    '9' => "\u2079",
                    _ => c.ToString()
                };
            }
            return res;
        }

        private void LoadPracticalApps()
        {
            try
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                string suffix = isVN ? "VN" : "EN";

                var list = new System.Collections.Generic.List<PracticalAppItem>
                {
                    new PracticalAppItem
                    {
                        Icon = "🚔",
                        Title = isVN ? "Máy bắn tốc độ" : "Speed Radar",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_wavespeed_1_{suffix}.png",
                        Description = isVN 
                            ? "Sử dụng hiệu ứng Doppler bằng cách đo sự dịch chuyển tần số của sóng radar/laser phản xạ từ xe đang chạy để tính vận tốc tức thời." 
                            : "Uses the Doppler effect by measuring the frequency shift of radar or laser waves reflected from a moving vehicle to calculate its instantaneous speed."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🩺",
                        Title = isVN ? "Siêu âm y khoa" : "Medical Ultrasound",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_wavespeed_2_{suffix}.png",
                        Description = isVN 
                            ? "Sóng siêu âm truyền qua các mô cơ thể với tốc độ ≈ 1540 m/s, phản xạ tại ranh giới các mô để dựng lại hình ảnh thai nhi hoặc cơ quan nội tạng." 
                            : "Ultrasound waves propagate through body tissues at ≈ 1540 m/s, reflecting at tissue boundaries to reconstruct images of fetuses or internal organs."
                    },
                    new PracticalAppItem
                    {
                        Icon = "⚓",
                        Title = isVN ? "SONAR tàu ngầm" : "Submarine SONAR",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_wavespeed_3_{suffix}.png",
                        Description = isVN 
                            ? "Phát các xung sóng âm dưới nước (v ≈ 1500 m/s) và thu nhận sóng phản xạ để xác định khoảng cách, định vị tàu ngầm và chướng ngại vật." 
                            : "Emits underwater sound pulses (v ≈ 1500 m/s) and receives reflected waves to determine distances and locate submarines or underwater obstacles."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🌐",
                        Title = isVN ? "Cáp quang viễn thông" : "Fiber Optic Telecom",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_wavespeed_4_{suffix}.png",
                        Description = isVN 
                            ? "Sóng ánh sáng truyền trong lõi sợi thủy tinh với tốc độ ≈ 200.000 km/s bằng hiện tượng phản xạ toàn phần, mang thông tin Internet toàn cầu." 
                            : "Light waves travel inside glass fiber cores at ≈ 200,000 km/s via total internal reflection, transmitting global Internet data."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🌋",
                        Title = isVN ? "Sóng địa chấn" : "Seismic Waves",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_wavespeed_5_{suffix}.png",
                        Description = isVN 
                            ? "Đo đạc tốc độ lan truyền của sóng P và sóng S qua lòng đất giúp các nhà khoa học xác định tâm chấn động đất và cấu trúc các lớp Trái Đất." 
                            : "Measuring the propagation speed of P-waves and S-waves through the Earth helps scientists pinpoint earthquake epicenters and map Earth's internal structures."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🌦️",
                        Title = isVN ? "Radar thời tiết" : "Weather Radar",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_wavespeed_6_{suffix}.png",
                        Description = isVN 
                            ? "Sử dụng xung sóng vô tuyến phản xạ từ các giọt nước mưa để phân tích tốc độ gió và lượng mưa di chuyển trong các đám mây dông." 
                            : "Uses radio wave pulses reflected from raindrops to analyze wind speed and precipitation patterns within thunderstorm clouds."
                    }
                ,
                    new PracticalAppItem
                    {
                        Icon = "👶",
                        Title = isVN ? "Thiết bị siêu âm thai nhi" : "Fetal Ultrasound Equipment",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_wavespeed_7_{suffix}.png",
                        Description = isVN 
                            ? "Đo thời gian phản xạ của sóng siêu âm tần số cao qua các mô cơ thể để dựng lại hình ảnh thai nhi trong y khoa." 
                            : "Measure high-frequency ultrasound wave echo times through body tissues to reconstruct fetal images in medicine."
                    },
                    new PracticalAppItem
                    {
                        Icon = "⚓",
                        Title = isVN ? "Phân tích tiếng vang Sonar" : "Sonar Underwater Echo Analysis",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_wavespeed_8_{suffix}.png",
                        Description = isVN 
                            ? "Tính toán vận tốc sóng âm trong nước biển để thiết bị Sonar đo độ sâu đại dương và phát hiện tàu ngầm." 
                            : "Calculate sound wave velocity in seawater to enable Sonar devices to measure depth and detect submarines."
                    }};

                practicalAppViewer.SetItemsSource(list);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading practical images: " + ex.Message);
            }
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewCalculator == null || viewSpectrum == null || viewConstants == null || viewMedium == null || viewPractical == null)
                return;

            viewGuide.Visibility = Visibility.Collapsed;
            viewCalculator.Visibility = Visibility.Collapsed;
            viewSpectrum.Visibility = Visibility.Collapsed;
            viewConstants.Visibility = Visibility.Collapsed;
            viewMedium.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            

            switch (sideMenu.SelectedIndex)
            {
                case 0:
                    viewGuide.Visibility = Visibility.Visible;
                    break;
                case 1:
                    viewCalculator.Visibility = Visibility.Visible;
                    break;
                case 2:
                    viewSpectrum.Visibility = Visibility.Visible;
                    break;
                case 3:
                    viewConstants.Visibility = Visibility.Visible;
                    break;
                case 4:
                    viewMedium.Visibility = Visibility.Visible;
                    break;
                case 5:
                    viewPractical.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}
