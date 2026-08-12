using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Newtonsoft.Json;
using QASmartTouch.PeriodicTable.Models;
using QASmartTouch.Shared;

namespace QASmartTouch.PeriodicTable.Views
{
    public partial class ReactivityWindow : Window
    {
        private List<MetalReaction> allMetals;

        public ReactivityWindow()
        {
            InitializeComponent();
            LoadMetalReactivityData();
        }

        // Helper method để loại bỏ UTF-8 BOM nếu có
        private string ReadJsonFileWithoutBom(string filePath)
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            int startIndex = 0;
            
            // Kiểm tra và loại bỏ UTF-8 BOM (EF BB BF)
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                startIndex = 3;
            }
            
            // Decode UTF-8 và loại bỏ zero-width characters
            string content = System.Text.Encoding.UTF8.GetString(bytes, startIndex, bytes.Length - startIndex);
            content = content.Replace("\uFEFF", "").Replace("\u200B", "").Replace("\u200C", "").Replace("\u200D", "");
            return content;
        }

        private void LoadMetalReactivityData()
        {
            try
            {
                string jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PeriodicTable", "Data", "metal_reactivity.json");
                
                if (File.Exists(jsonPath))
                {
                    string jsonContent = ReadJsonFileWithoutBom(jsonPath);
                    allMetals = JsonConvert.DeserializeObject<List<MetalReaction>>(jsonContent);

                    // Set màu sắc, text color và icon cho từng kim loại
                    foreach (var metal in allMetals)
                    {
                        // Background colors (Pastel)
                        metal.ColdWaterColor = MetalReaction.GetColorFromReaction(metal.ColdWater);
                        metal.HotWaterColor = MetalReaction.GetColorFromReaction(metal.HotWater);
                        metal.AcidColor = MetalReaction.GetColorFromReaction(metal.Acid);

                        // Text colors (Dark for contrast)
                        metal.ColdWaterTextColor = MetalReaction.GetTextColorFromReaction(metal.ColdWater);
                        metal.HotWaterTextColor = MetalReaction.GetTextColorFromReaction(metal.HotWater);
                        metal.AcidTextColor = MetalReaction.GetTextColorFromReaction(metal.Acid);

                        // Icons
                        metal.ColdWaterIcon = MetalReaction.GetIconFromReaction(metal.ColdWater);
                        metal.HotWaterIcon = MetalReaction.GetIconFromReaction(metal.HotWater);
                        metal.AcidIcon = MetalReaction.GetIconFromReaction(metal.Acid);

                        // Levels
                        metal.ColdWaterLevel = MetalReaction.GetLevelFromReaction(metal.ColdWater);
                        metal.HotWaterLevel = MetalReaction.GetLevelFromReaction(metal.HotWater);
                        metal.AcidLevel = MetalReaction.GetLevelFromReaction(metal.Acid);
                    }

                    MetalsItemsControl.ItemsSource = allMetals;
                }
                else
                {
                    MessageBox.Show("Không tìm thấy file dữ liệu metal_reactivity.json!", 
                                   "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi load dữ liệu: {ex.Message}", 
                               "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Search functionality
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyCurrentFilter();
        }

        // Filter functionality
        private void FilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyCurrentFilter();
        }

        private void ApplyCurrentFilter()
        {
            if (FilterCombo == null || allMetals == null) return;

            var selectedItem = FilterCombo.SelectedItem as ComboBoxItem;
            if (selectedItem == null) return;

            string searchText = SearchBox?.Text?.ToLower().Trim() ?? "";
            string filterText = selectedItem.Content.ToString();

            // Start with all metals
            List<MetalReaction> result = new List<MetalReaction>(allMetals);

            // Apply search filter first
            if (!string.IsNullOrEmpty(searchText))
            {
                result = result.Where(m =>
                    m.Symbol.ToLower().Contains(searchText) ||
                    m.Name.ToLower().Contains(searchText) ||
                    m.ColdWater.ToLower().Contains(searchText) ||
                    m.HotWater.ToLower().Contains(searchText) ||
                    m.Acid.ToLower().Contains(searchText)
                ).ToList();
            }

            // Apply dropdown filter
            if (filterText.Contains("Phản ứng mạnh"))
            {
                result = result.Where(m =>
                    m.ColdWater.Contains("Dữ dội") || m.ColdWater.Contains("Mạnh") ||
                    m.HotWater.Contains("Dữ dội") || m.HotWater.Contains("Mạnh") ||
                    m.Acid.Contains("Dữ dội") || m.Acid.Contains("Mạnh")
                ).ToList();
            }
            else if (filterText.Contains("Phản ứng vừa phải"))
            {
                result = result.Where(m =>
                    m.ColdWater.Contains("Vừa phải") ||
                    m.HotWater.Contains("Vừa phải") ||
                    m.Acid.Contains("Vừa phải")
                ).ToList();
            }
            else if (filterText.Contains("Không phản ứng"))
            {
                result = result.Where(m =>
                    m.ColdWater.Contains("Không") ||
                    m.HotWater.Contains("Không") ||
                    m.Acid.Contains("Không")
                ).ToList();
            }
            else if (filterText.Contains("Chỉ H"))
            {
                result = result.Where(m => m.Symbol == "H").ToList();
            }
            // "Tất cả" - no additional filter

            MetalsItemsControl.ItemsSource = null;
            MetalsItemsControl.ItemsSource = result;
        }

        // Reaction cell click handler
        private void ReactionCell_Click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border == null) return;

            var metal = border.DataContext as MetalReaction;
            if (metal == null) return;

            string reactionType = border.Tag as string;
            ShowDetailPopup(metal, reactionType);
        }

        private void ShowDetailPopup(MetalReaction metal, string reactionType)
        {
            PopupContent.Children.Clear();

            // Title
            string reactionName = "";
            string equation = "";
            string phenomenon = "";
            string reactionDesc = "";
            string iconEmoji = "";

            switch (reactionType)
            {
                case "ColdWater":
                    reactionName = "💧 NƯỚC LẠNH";
                    equation = metal.ColdWaterEquation;
                    phenomenon = metal.ColdWaterPhenomenon;
                    reactionDesc = metal.ColdWater;
                    iconEmoji = metal.ColdWaterIcon;
                    break;
                case "HotWater":
                    reactionName = "💨 HƠI NƯỚC / NƯỚC NÓNG";
                    equation = metal.HotWaterEquation;
                    phenomenon = metal.HotWaterPhenomenon;
                    reactionDesc = metal.HotWater;
                    iconEmoji = metal.HotWaterIcon;
                    break;
                case "Acid":
                    reactionName = "⚗️ AXIT LOÃNG (HCl, H₂SO₄)";
                    equation = metal.AcidEquation;
                    phenomenon = metal.AcidPhenomenon;
                    reactionDesc = metal.Acid;
                    iconEmoji = metal.AcidIcon;
                    break;
            }

            PopupTitle.Text = $"{iconEmoji} {metal.Symbol} ({metal.Name}) + {reactionName}";

            // Mức độ phản ứng
            var levelPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 20) };
            var levelBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(
                    reactionType == "ColdWater" ? metal.ColdWaterColor :
                    reactionType == "HotWater" ? metal.HotWaterColor : metal.AcidColor)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20, 15, 20, 15),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9E9E9E")),
                BorderThickness = new Thickness(1, 1, 1, 1)
            };
            var levelText = new TextBlock
            {
                Text = $"{iconEmoji} {reactionDesc}",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.Black),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            levelBorder.Child = levelText;
            levelPanel.Children.Add(levelBorder);
            PopupContent.Children.Add(levelPanel);

            // Phương trình hóa học
            if (!string.IsNullOrEmpty(equation) && equation != "—" && !equation.Contains("Không xảy ra"))
            {
                var equationTitle = new TextBlock
                {
                    Text = "🔬 PHƯƠNG TRÌNH HÓA HỌC:",
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1976D2")),
                    Margin = new Thickness(0, 0, 0, 12)
                };
                PopupContent.Children.Add(equationTitle);

                var equationBorder = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E3F2FD")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3")),
                    BorderThickness = new Thickness(2, 2, 2, 2),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(20, 16, 20, 16),
                    Margin = new Thickness(0, 0, 0, 20)
                };
                var equationText = new TextBlock
                {
                    Text = equation,
                    FontSize = 20,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0D47A1")),
                    TextWrapping = TextWrapping.Wrap
                };
                equationBorder.Child = equationText;
                PopupContent.Children.Add(equationBorder);
            }

            // Hiện tượng
            if (!string.IsNullOrEmpty(phenomenon))
            {
                var phenomenonTitle = new TextBlock
                {
                    Text = "🔥 HIỆN TƯỢNG:",
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E65100")),
                    Margin = new Thickness(0, 0, 0, 12)
                };
                PopupContent.Children.Add(phenomenonTitle);

                var phenomenonBorder = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF3E0")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800")),
                    BorderThickness = new Thickness(2, 2, 2, 2),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(20, 16, 20, 16),
                    Margin = new Thickness(0, 0, 0, 20)
                };
                var phenomenonText = new TextBlock
                {
                    Text = phenomenon,
                    FontSize = 17,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#424242")),
                    TextWrapping = TextWrapping.Wrap,
                    LineHeight = 26
                };
                phenomenonBorder.Child = phenomenonText;
                PopupContent.Children.Add(phenomenonBorder);
            }

            // Cảnh báo an toàn (Alkali / Alkaline Earth metals safety warning)
            string[] activeMetals = { "Cs", "Fr", "Rb", "K", "Na", "Li", "Ba", "Ra", "Sr", "Ca" };
            if (activeMetals.Contains(metal.Symbol) && (reactionType == "ColdWater" || reactionType == "Acid"))
            {
                var safetyTitle = new TextBlock
                {
                    Text = "⚠️ CẢNH BÁO AN TOÀN PHÒNG THÍ NGHIỆM:",
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D32F2F")),
                    Margin = new Thickness(0, 10, 0, 12)
                };
                PopupContent.Children.Add(safetyTitle);

                var safetyBorder = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEBEE")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF5350")),
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(20, 16, 20, 16),
                    Margin = new Thickness(0, 0, 0, 20)
                };
                var safetyText = new TextBlock
                {
                    Text = "Kim loại kiềm/kiềm thổ phản ứng cực kỳ mãnh liệt, tỏa nhiều nhiệt và có thể gây nổ. Trong thực tế, KHÔNG ĐƯỢC tự ý tiến hành phản ứng này nếu không có sự giám sát của giáo viên. Bắt buộc sử dụng lượng cực nhỏ, kẹp gắp y tế và kính bảo hộ.",
                    FontSize = 17,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C62828")),
                    TextWrapping = TextWrapping.Wrap,
                    LineHeight = 26
                };
                safetyBorder.Child = safetyText;
                PopupContent.Children.Add(safetyBorder);
            }

            // Ghi chú
            if (!string.IsNullOrEmpty(metal.Note))
            {
                var noteTitle = new TextBlock
                {
                    Text = "💡 GHI NHỚ:",
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#388E3C")),
                    Margin = new Thickness(0, 0, 0, 12)
                };
                PopupContent.Children.Add(noteTitle);

                var noteBorder = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")),
                    BorderThickness = new Thickness(2, 2, 2, 2),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(20, 16, 20, 16)
                };
                var noteText = new TextBlock
                {
                    Text = metal.Note,
                    FontSize = 17,
                    FontStyle = FontStyles.Italic,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#424242")),
                    TextWrapping = TextWrapping.Wrap,
                    LineHeight = 26
                };
                noteBorder.Child = noteText;
                PopupContent.Children.Add(noteBorder);
            }

            DetailPopup.Visibility = Visibility.Visible;
        }

        private void ClosePopup_Click(object sender, RoutedEventArgs e)
        {
            DetailPopup.Visibility = Visibility.Collapsed;
        }

        private void DetailPopup_Click(object sender, MouseButtonEventArgs e)
        {
            if (e.Source == DetailPopup)
            {
                DetailPopup.Visibility = Visibility.Collapsed;
            }
        }

        private void CompareButton_Click(object sender, RoutedEventArgs e)
        {
            var compareDialog = new CompareMetalsDialog(allMetals);
            WindowHelper.ShowChildDialog(compareDialog, this);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

