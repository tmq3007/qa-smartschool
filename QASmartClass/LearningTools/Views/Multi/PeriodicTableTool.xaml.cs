using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QASmartClass.LearningTools.Models;
using QASmartClass.LearningTools.Controls;

namespace QASmartClass.LearningTools.Views.Multi
{
    /// <summary>
    /// Wrapper cho module Bảng Tuần Hoàn (PeriodicTable) đã có sẵn trong dự án.
    /// Nhấn "Mở rộng toàn màn hình" sẽ mở cửa sổ PeriodicTable MainWindow đầy đủ tính năng.
    /// Trong Hub sẽ hiển thị một quick-view bảng tuần hoàn thu gọn.
    /// </summary>
    public partial class PeriodicTableTool : BaseToolControl, IDisposable
    {
        public PeriodicTableTool()
        {
            InitializeComponent();
            Background = null;
            Loaded += (s, e) =>
            {
                if (Parent is Panel p) p.Background = null;
            };
            Loaded += (_, _) => {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
                menuTextTable.Text = isVN ? "Bảng tuần hoàn" : "Periodic Table";
                menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Practical Apps";
                
                sideMenu.SelectedIndex = 0;
                BuildQuickView();
                LoadPracticalApps();
            };
        }

        /// <summary>
        /// Build a compact quick-view of the periodic table using colored blocks.
        /// This gives users a visual overview before opening the full interactive window.
        /// </summary>
        private void BuildQuickView()
        {
            TablePanel.Children.Clear();

            // ── Quick Stats ──
            var statsPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 15) };
            AddStatCard(statsPanel, "🔢 118", "Nguyên tố", "#E3F2FD", "#1565C0");
            AddStatCard(statsPanel, "⚗️ 7", "Chu kỳ", "#E8F5E9", "#2E7D32");
            AddStatCard(statsPanel, "📊 18", "Nhóm", "#FFF3E0", "#E65100");
            AddStatCard(statsPanel, "🔬 10", "Phân loại", "#FCE4EC", "#C62828");
            TablePanel.Children.Add(statsPanel);

            // ── Compact Periodic Table Grid ──
            var tableLabel = new TextBlock
            {
                Text = "📋 Xem nhanh Bảng Tuần Hoàn (Nhấn \"Mở rộng toàn màn hình\" để tương tác đầy đủ)",
                FontSize = 15, FontWeight = FontWeights.SemiBold, Foreground = Brushes.Gray,
                FontFamily = Application.Current?.TryFindResource("InterFont") as FontFamily ?? new FontFamily("Segoe UI"), Margin = new Thickness(0, 0, 0, 10)
            };
            TablePanel.Children.Add(tableLabel);

            // Simplified periodic table layout (element symbol + atomic number)
            // Standard periodic table: 7 periods, 18 groups
            // Using a Grid for proper positioning
            var tableGrid = new Grid();
            for (int r = 0; r < 10; r++) // 7 main rows + gap + 2 for lanthanides/actinides
                tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            for (int c = 0; c < 18; c++)
                tableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Element data: (Symbol, AtomicNumber, Row, Col, ColorHex)
            var elements = new (string Sym, int Num, int Row, int Col, string Color)[]
            {
                // Period 1
                ("H", 1, 0, 0, "#FF6B6B"), ("He", 2, 0, 17, "#A8E6CF"),
                // Period 2
                ("Li", 3, 1, 0, "#FFB347"), ("Be", 4, 1, 1, "#FFEAA7"), ("B", 5, 1, 12, "#DDA0DD"), ("C", 6, 1, 13, "#87CEEB"), ("N", 7, 1, 14, "#87CEEB"), ("O", 8, 1, 15, "#87CEEB"), ("F", 9, 1, 16, "#FFD700"), ("Ne", 10, 1, 17, "#A8E6CF"),
                // Period 3
                ("Na", 11, 2, 0, "#FFB347"), ("Mg", 12, 2, 1, "#FFEAA7"), ("Al", 13, 2, 12, "#C0C0C0"), ("Si", 14, 2, 13, "#DDA0DD"), ("P", 15, 2, 14, "#87CEEB"), ("S", 16, 2, 15, "#87CEEB"), ("Cl", 17, 2, 16, "#FFD700"), ("Ar", 18, 2, 17, "#A8E6CF"),
                // Period 4
                ("K", 19, 3, 0, "#FFB347"), ("Ca", 20, 3, 1, "#FFEAA7"),
                ("Sc", 21, 3, 2, "#B0C4DE"), ("Ti", 22, 3, 3, "#B0C4DE"), ("V", 23, 3, 4, "#B0C4DE"), ("Cr", 24, 3, 5, "#B0C4DE"), ("Mn", 25, 3, 6, "#B0C4DE"), ("Fe", 26, 3, 7, "#B0C4DE"), ("Co", 27, 3, 8, "#B0C4DE"), ("Ni", 28, 3, 9, "#B0C4DE"), ("Cu", 29, 3, 10, "#B0C4DE"), ("Zn", 30, 3, 11, "#B0C4DE"),
                ("Ga", 31, 3, 12, "#C0C0C0"), ("Ge", 32, 3, 13, "#DDA0DD"), ("As", 33, 3, 14, "#DDA0DD"), ("Se", 34, 3, 15, "#87CEEB"), ("Br", 35, 3, 16, "#FFD700"), ("Kr", 36, 3, 17, "#A8E6CF"),
                // Period 5
                ("Rb", 37, 4, 0, "#FFB347"), ("Sr", 38, 4, 1, "#FFEAA7"),
                ("Y", 39, 4, 2, "#B0C4DE"), ("Zr", 40, 4, 3, "#B0C4DE"), ("Nb", 41, 4, 4, "#B0C4DE"), ("Mo", 42, 4, 5, "#B0C4DE"), ("Tc", 43, 4, 6, "#B0C4DE"), ("Ru", 44, 4, 7, "#B0C4DE"), ("Rh", 45, 4, 8, "#B0C4DE"), ("Pd", 46, 4, 9, "#B0C4DE"), ("Ag", 47, 4, 10, "#B0C4DE"), ("Cd", 48, 4, 11, "#B0C4DE"),
                ("In", 49, 4, 12, "#C0C0C0"), ("Sn", 50, 4, 13, "#C0C0C0"), ("Sb", 51, 4, 14, "#DDA0DD"), ("Te", 52, 4, 15, "#DDA0DD"), ("I", 53, 4, 16, "#FFD700"), ("Xe", 54, 4, 17, "#A8E6CF"),
                // Period 6
                ("Cs", 55, 5, 0, "#FFB347"), ("Ba", 56, 5, 1, "#FFEAA7"),
                ("La*", 57, 5, 2, "#F0E68C"),
                ("Hf", 72, 5, 3, "#B0C4DE"), ("Ta", 73, 5, 4, "#B0C4DE"), ("W", 74, 5, 5, "#B0C4DE"), ("Re", 75, 5, 6, "#B0C4DE"), ("Os", 76, 5, 7, "#B0C4DE"), ("Ir", 77, 5, 8, "#B0C4DE"), ("Pt", 78, 5, 9, "#B0C4DE"), ("Au", 79, 5, 10, "#B0C4DE"), ("Hg", 80, 5, 11, "#B0C4DE"),
                ("Tl", 81, 5, 12, "#C0C0C0"), ("Pb", 82, 5, 13, "#C0C0C0"), ("Bi", 83, 5, 14, "#C0C0C0"), ("Po", 84, 5, 15, "#DDA0DD"), ("At", 85, 5, 16, "#FFD700"), ("Rn", 86, 5, 17, "#A8E6CF"),
                // Period 7
                ("Fr", 87, 6, 0, "#FFB347"), ("Ra", 88, 6, 1, "#FFEAA7"),
                ("Ac**", 89, 6, 2, "#D4A574"),
                ("Rf", 104, 6, 3, "#B0C4DE"), ("Db", 105, 6, 4, "#B0C4DE"), ("Sg", 106, 6, 5, "#B0C4DE"), ("Bh", 107, 6, 6, "#B0C4DE"), ("Hs", 108, 6, 7, "#B0C4DE"), ("Mt", 109, 6, 8, "#B0C4DE"), ("Ds", 110, 6, 9, "#B0C4DE"), ("Rg", 111, 6, 10, "#B0C4DE"), ("Cn", 112, 6, 11, "#B0C4DE"),
                ("Nh", 113, 6, 12, "#C0C0C0"), ("Fl", 114, 6, 13, "#C0C0C0"), ("Mc", 115, 6, 14, "#C0C0C0"), ("Lv", 116, 6, 15, "#C0C0C0"), ("Ts", 117, 6, 16, "#FFD700"), ("Og", 118, 6, 17, "#A8E6CF"),
                // Lanthanides row (row 8)
                ("Ce",58,8,3,"#F0E68C"),("Pr",59,8,4,"#F0E68C"),("Nd",60,8,5,"#F0E68C"),("Pm",61,8,6,"#F0E68C"),("Sm",62,8,7,"#F0E68C"),("Eu",63,8,8,"#F0E68C"),("Gd",64,8,9,"#F0E68C"),("Tb",65,8,10,"#F0E68C"),("Dy",66,8,11,"#F0E68C"),("Ho",67,8,12,"#F0E68C"),("Er",68,8,13,"#F0E68C"),("Tm",69,8,14,"#F0E68C"),("Yb",70,8,15,"#F0E68C"),("Lu",71,8,16,"#F0E68C"),
                // Actinides row (row 9)
                ("Th",90,9,3,"#D4A574"),("Pa",91,9,4,"#D4A574"),("U",92,9,5,"#D4A574"),("Np",93,9,6,"#D4A574"),("Pu",94,9,7,"#D4A574"),("Am",95,9,8,"#D4A574"),("Cm",96,9,9,"#D4A574"),("Bk",97,9,10,"#D4A574"),("Cf",98,9,11,"#D4A574"),("Es",99,9,12,"#D4A574"),("Fm",100,9,13,"#D4A574"),("Md",101,9,14,"#D4A574"),("No",102,9,15,"#D4A574"),("Lr",103,9,16,"#D4A574"),
            };

            foreach (var el in elements)
            {
                var cell = new Border
                {
                    Width = 48, Height = 48,
                    Margin = new Thickness(1),
                    CornerRadius = new CornerRadius(3),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(el.Color)),
                    ToolTip = $"{el.Sym} (Z={el.Num})"
                };

                var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                stack.Children.Add(new TextBlock
                {
                    Text = el.Num.ToString(), FontSize = 8, Foreground = Brushes.Black,
                    HorizontalAlignment = HorizontalAlignment.Center, Opacity = 0.6
                });
                stack.Children.Add(new TextBlock
                {
                    Text = el.Sym.Replace("*","").Replace("*",""), FontSize = 14, FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Black, HorizontalAlignment = HorizontalAlignment.Center
                });

                cell.Child = stack;

                Grid.SetRow(cell, el.Row);
                Grid.SetColumn(cell, el.Col);
                tableGrid.Children.Add(cell);
            }

            // Add gap label for lanthanides/actinides
            var gapLabel = new TextBlock
            {
                Text = "* Lantanit    ** Actinit", FontSize = 11, Foreground = Brushes.Gray,
                FontFamily = Application.Current?.TryFindResource("InterFont") as FontFamily ?? new FontFamily("Segoe UI"), FontStyle = FontStyles.Italic,
                Margin = new Thickness(5, 2, 0, 2)
            };
            Grid.SetRow(gapLabel, 7);
            Grid.SetColumnSpan(gapLabel, 18);
            tableGrid.Children.Add(gapLabel);

            var viewbox = new Viewbox
            {
                Child = tableGrid,
                Stretch = System.Windows.Media.Stretch.Uniform,
                StretchDirection = StretchDirection.Both,
                MaxHeight = 600
            };

            TablePanel.Children.Add(viewbox);

            // ── Legend ──
            var legendPanel = new WrapPanel { Margin = new Thickness(0, 15, 0, 0) };
            AddLegendItem(legendPanel, "#FFB347", "Kim loại kiềm");
            AddLegendItem(legendPanel, "#FFEAA7", "Kim loại kiềm thổ");
            AddLegendItem(legendPanel, "#B0C4DE", "Kim loại chuyển tiếp");
            AddLegendItem(legendPanel, "#C0C0C0", "Kim loại khác");
            AddLegendItem(legendPanel, "#DDA0DD", "Á kim");
            AddLegendItem(legendPanel, "#87CEEB", "Phi kim");
            AddLegendItem(legendPanel, "#FFD700", "Halogen");
            AddLegendItem(legendPanel, "#A8E6CF", "Khí hiếm");
            AddLegendItem(legendPanel, "#F0E68C", "Lantanit");
            AddLegendItem(legendPanel, "#D4A574", "Actinit");
            TablePanel.Children.Add(legendPanel);
        }

        private void AddStatCard(WrapPanel parent, string value, string label, string bgHex, string fgHex)
        {
            var bg = (Color)ColorConverter.ConvertFromString(bgHex);
            var fg = (Color)ColorConverter.ConvertFromString(fgHex);

            var card = new Border
            {
                Background = new SolidColorBrush(bg),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20, 12, 20, 12),
                Margin = new Thickness(0, 0, 10, 0)
            };

            var stack = new StackPanel { Orientation = Orientation.Horizontal };
            stack.Children.Add(new TextBlock
            {
                Text = value, FontSize = 20, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(fg), FontFamily = Application.Current?.TryFindResource("InterFont") as FontFamily ?? new FontFamily("Segoe UI"),
                Margin = new Thickness(0, 0, 8, 0)
            });
            stack.Children.Add(new TextBlock
            {
                Text = label, FontSize = 14, Foreground = new SolidColorBrush(fg),
                FontFamily = Application.Current?.TryFindResource("InterFont") as FontFamily ?? new FontFamily("Segoe UI"), VerticalAlignment = VerticalAlignment.Center
            });

            card.Child = stack;
            parent.Children.Add(card);
        }

        private void AddLegendItem(WrapPanel parent, string colorHex, string label)
        {
            var stack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 14, 4) };
            stack.Children.Add(new Border
            {
                Width = 16, Height = 16,
                CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex)),
                Margin = new Thickness(0, 0, 4, 0)
            });
            stack.Children.Add(new TextBlock
            {
                Text = label, FontSize = 12, Foreground = Brushes.Gray,
                FontFamily = Application.Current?.TryFindResource("InterFont") as FontFamily ?? new FontFamily("Segoe UI"), VerticalAlignment = VerticalAlignment.Center
            });
            parent.Children.Add(stack);
        }

        /// <summary>
        /// Opens the full interactive Periodic Table in a new window (existing module).
        /// </summary>
        private void BtnOpenFull_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new QASmartTouch.PeriodicTable.Views.MainWindow();
                window.WindowState = WindowState.Maximized;
                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể mở Bảng Tuần Hoàn: {ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public void Dispose()
        {
            TablePanel.Children.Clear();
        }

        private void LoadPracticalApps()
        {
            bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
            string suffix = isVN ? "VN" : "EN";

            var items = new System.Collections.Generic.List<PracticalAppItem>
            {
                new PracticalAppItem
                {
                    Icon = "🧪",
                    Title = isVN ? "Hóa học & Đời sống" : "Chemistry in Daily Life",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_periodictable_1_{suffix}.png",
                    Description = isVN 
                        ? "Cách các nguyên tố hóa học hiện diện trong các đồ vật xung quanh ta, từ khí Helium trong bóng bay đến khí trơ Neon dùng trong đèn quảng cáo." 
                        : "Discover how chemical elements are present in everyday objects, from Helium gas in balloons to inert Neon gas used in commercial signs."
                },
                new PracticalAppItem
                {
                    Icon = "🔋",
                    Title = isVN ? "Pin & Năng lượng sạch" : "Battery & Green Tech",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_periodictable_2_{suffix}.png",
                    Description = isVN 
                        ? "Giải thích cấu trúc và vai trò của các nguyên tố như Lithium, Sodium và Cobalt trong việc chế tạo pin và lưu trữ năng lượng thân thiện với môi trường." 
                        : "Explain the structure and role of elements like Lithium, Sodium, and Cobalt in manufacturing batteries and clean energy storage solutions."
                },
                new PracticalAppItem
                {
                    Icon = "🚀",
                    Title = isVN ? "Vật liệu Hàng không vũ trụ" : "Spacecraft Materials",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_periodictable_3_{suffix}.png",
                    Description = isVN 
                        ? "Ứng dụng các kim loại chuyển tiếp siêu bền như Titanium và các hợp chất siêu dẫn vào công nghệ hàng không và thiết kế vỏ tàu vũ trụ." 
                        : "Application of strong transition metals like Titanium and superconducting compounds in aviation technology and spacecraft design."
                }
            };

            try
            {
                contentApp.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for PeriodicTableTool: {Err}", ex.Message);
            }
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (rootGrid == null || sideMenu == null || viewGuide == null || viewPractice == null || viewPractical == null) return;
            
            viewGuide.Visibility = Visibility.Collapsed;
            viewPractice.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;
            
            int index = sideMenu.SelectedIndex;
            if (index == 0)
            {
                viewGuide.Visibility = Visibility.Visible;
            }
            else if (index == 1)
            {
                viewPractice.Visibility = Visibility.Visible;
            }
            else if (index == 2)
            {
                viewPractical.Visibility = Visibility.Visible;
            }
        }
    }
}