using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QASmartTouch.PeriodicTable.Models;

namespace QASmartTouch.PeriodicTable.Views
{
    public partial class CompareMetalsDialog : Window
    {
        private List<MetalReaction> metals;

        public CompareMetalsDialog(List<MetalReaction> allMetals)
        {
            InitializeComponent();
            metals = allMetals;
            
            // Populate ComboBoxes
            foreach (var metal in metals)
            {
                Metal1Combo.Items.Add($"{metal.Symbol} - {metal.Name}");
                Metal2Combo.Items.Add($"{metal.Symbol} - {metal.Name}");
            }

            // Default selection
            if (metals.Count >= 2)
            {
                Metal1Combo.SelectedIndex = 4; // Na
                Metal2Combo.SelectedIndex = 16; // Fe
            }
        }

        private void MetalCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Metal1Combo.SelectedIndex >= 0 && Metal2Combo.SelectedIndex >= 0)
            {
                var metal1 = metals[Metal1Combo.SelectedIndex];
                var metal2 = metals[Metal2Combo.SelectedIndex];
                ShowComparison(metal1, metal2);
            }
        }

        private void ShowComparison(MetalReaction metal1, MetalReaction metal2)
        {
            ComparisonPanel.Children.Clear();

            // Title
            var titlePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 12)
            };

            var metal1Title = new TextBlock
            {
                Text = $"{metal1.Symbol} ({metal1.Name})",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1976D2"))
            };
            titlePanel.Children.Add(metal1Title);

            var vsText = new TextBlock
            {
                Text = " ⚡ VS ⚡ ",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6B00")),
                Margin = new Thickness(15, 0, 15, 0)
            };
            titlePanel.Children.Add(vsText);

            var metal2Title = new TextBlock
            {
                Text = $"{metal2.Symbol} ({metal2.Name})",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E64A19"))
            };
            titlePanel.Children.Add(metal2Title);

            ComparisonPanel.Children.Add(titlePanel);

            // Comparison Table
            var grid = new Grid { Margin = new Thickness(0, 8, 0, 0) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Pre-initialize exactly 4 row definitions (Row 0: Header, Rows 1-3: Reactions)
            for (int i = 0; i < 4; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            // Header Row
            AddHeaderCell(grid, "Loại phản ứng", 0, 0);
            AddHeaderCell(grid, $"{metal1.Symbol} ({metal1.Name})", 0, 1, "#1976D2");
            AddHeaderCell(grid, $"{metal2.Symbol} ({metal2.Name})", 0, 2, "#E64A19");

            // Cold Water Row
            AddComparisonRow(grid, 1, "💧 Nước lạnh", 
                metal1.ColdWater, metal1.ColdWaterColor, metal1.ColdWaterIcon, metal1.ColdWaterLevel,
                metal2.ColdWater, metal2.ColdWaterColor, metal2.ColdWaterIcon, metal2.ColdWaterLevel);

            // Hot Water Row
            AddComparisonRow(grid, 2, "💨 Hơi nước", 
                metal1.HotWater, metal1.HotWaterColor, metal1.HotWaterIcon, metal1.HotWaterLevel,
                metal2.HotWater, metal2.HotWaterColor, metal2.HotWaterIcon, metal2.HotWaterLevel);

            // Acid Row
            AddComparisonRow(grid, 3, "⚗️ Axit loãng", 
                metal1.Acid, metal1.AcidColor, metal1.AcidIcon, metal1.AcidLevel,
                metal2.Acid, metal2.AcidColor, metal2.AcidIcon, metal2.AcidLevel);

            ComparisonPanel.Children.Add(grid);

            // Conclusion
            var conclusionBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF3E0")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800")),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 15, 0, 0)
            };

            var conclusionStack = new StackPanel();

            var conclusionTitle = new TextBlock
            {
                Text = "🎯 KẾT LUẬN:",
                FontSize = 19,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E65100")),
                Margin = new Thickness(0, 0, 0, 10)
            };
            conclusionStack.Children.Add(conclusionTitle);

            // Calculate average activity
            double metal1Avg = (metal1.ColdWaterLevel + metal1.HotWaterLevel + metal1.AcidLevel) / 3.0;
            double metal2Avg = (metal2.ColdWaterLevel + metal2.HotWaterLevel + metal2.AcidLevel) / 3.0;

            string conclusion;
            string[] activeMetals = { "Cs", "Fr", "Rb", "K", "Na", "Li", "Ba", "Ra", "Sr", "Ca" };

            if (metal1.Order < metal2.Order)
            {
                conclusion = $"• {metal1.Symbol} ({metal1.Name}) hoạt động mạnh hơn {metal2.Symbol} ({metal2.Name})\n" +
                             $"• {metal1.Symbol} đứng trước {metal2.Symbol} trong dãy hoạt động hóa học\n";
                if (activeMetals.Contains(metal1.Symbol))
                {
                    conclusion += $"• Do {metal1.Symbol} phản ứng mạnh với nước ở điều kiện thường nên khi cho vào dung dịch muối của {metal2.Symbol}, {metal1.Symbol} sẽ phản ứng với nước trước tạo dung dịch kiềm, sau đó kiềm phản ứng trao đổi với muối (nếu thỏa mãn điều kiện), không đẩy trực tiếp kim loại {metal2.Symbol} ra ngoài.";
                }
                else
                {
                    conclusion += $"• {metal1.Symbol} có thể đẩy {metal2.Symbol} ra khỏi dung dịch muối (trừ các muối không tan hoặc điều kiện đặc biệt).";
                }
            }
            else if (metal1.Order > metal2.Order)
            {
                conclusion = $"• {metal2.Symbol} ({metal2.Name}) hoạt động mạnh hơn {metal1.Symbol} ({metal1.Name})\n" +
                             $"• {metal2.Symbol} đứng trước {metal1.Symbol} trong dãy hoạt động hóa học\n";
                if (activeMetals.Contains(metal2.Symbol))
                {
                    conclusion += $"• Do {metal2.Symbol} phản ứng mạnh với nước ở điều kiện thường nên khi cho vào dung dịch muối của {metal1.Symbol}, {metal2.Symbol} sẽ phản ứng với nước trước tạo dung dịch kiềm, sau đó kiềm phản ứng trao đổi với muối (nếu thỏa mãn điều kiện), không đẩy trực tiếp kim loại {metal1.Symbol} ra ngoài.";
                }
                else
                {
                    conclusion += $"• {metal2.Symbol} có thể đẩy {metal1.Symbol} ra khỏi dung dịch muối (trừ các muối không tan hoặc điều kiện đặc biệt).";
                }
            }
            else
            {
                conclusion = $"• {metal1.Symbol} và {metal2.Symbol} là cùng một kim loại";
            }

            var conclusionText = new TextBlock
            {
                Text = conclusion,
                FontSize = 16,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#424242")),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 26
            };
            conclusionStack.Children.Add(conclusionText);

            conclusionBorder.Child = conclusionStack;
            ComparisonPanel.Children.Add(conclusionBorder);

            // Notes
            var notesBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 12, 0, 0)
            };

            var notesStack = new StackPanel();

            var notesTitle = new TextBlock
            {
                Text = "💡 GHI NHỚ:",
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E7D32")),
                Margin = new Thickness(0, 0, 0, 8)
            };
            notesStack.Children.Add(notesTitle);

            if (!string.IsNullOrEmpty(metal1.Note))
            {
                var note1 = new TextBlock
                {
                    Text = $"• {metal1.Symbol}: {metal1.Note}",
                    FontSize = 14,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#424242")),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 6),
                    LineHeight = 22
                };
                notesStack.Children.Add(note1);
            }

            if (!string.IsNullOrEmpty(metal2.Note))
            {
                var note2 = new TextBlock
                {
                    Text = $"• {metal2.Symbol}: {metal2.Note}",
                    FontSize = 14,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#424242")),
                    TextWrapping = TextWrapping.Wrap,
                    LineHeight = 22
                };
                notesStack.Children.Add(note2);
            }

            notesBorder.Child = notesStack;
            ComparisonPanel.Children.Add(notesBorder);
        }

        private void AddHeaderCell(Grid grid, string text, int row, int col, string bgColor = "#616161")
        {
            var border = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgColor)),
                BorderBrush = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10)
            };
            Grid.SetRow(border, row);
            Grid.SetColumn(border, col);

            var textBlock = new TextBlock
            {
                Text = text,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.White),
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };
            border.Child = textBlock;
            grid.Children.Add(border);
        }

        private void AddComparisonRow(Grid grid, int row, string label,
            string reaction1, string color1, string icon1, int level1,
            string reaction2, string color2, string icon2, int level2)
        {
            // Label cell
            var labelBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0")),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10)
            };
            Grid.SetRow(labelBorder, row);
            Grid.SetColumn(labelBorder, 0);

            var labelText = new TextBlock
            {
                Text = label,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#424242")),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            labelBorder.Child = labelText;
            grid.Children.Add(labelBorder);

            // Metal 1 cell
            AddReactionCell(grid, row, 1, reaction1, color1, icon1, level1, level2, true);

            // Metal 2 cell
            AddReactionCell(grid, row, 2, reaction2, color2, icon2, level2, level1, false);
        }

        private void AddReactionCell(Grid grid, int row, int col, string reaction, string color, string icon, int level, int compareLevel, bool isLeft)
        {
            var border = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BDBDBD")),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8)
            };
            Grid.SetRow(border, row);
            Grid.SetColumn(border, col);

            // Highlight if stronger
            if (level > compareLevel && level > 0)
            {
                border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                border.BorderThickness = new Thickness(3);
            }

            var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };

            var iconText = new TextBlock
            {
                Text = icon,
                FontSize = 24,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            stack.Children.Add(iconText);

            var reactionText = new TextBlock
            {
                Text = reaction,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#424242")),
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 0)
            };
            stack.Children.Add(reactionText);

            // Winner badge
            if (level > compareLevel && level > 0)
            {
                var winnerBadge = new TextBlock
                {
                    Text = "🏆 Mạnh hơn",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E7D32")),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 4, 0, 0)
                };
                stack.Children.Add(winnerBadge);
            }

            border.Child = stack;
            grid.Children.Add(border);
        }

        private void SwapButton_Click(object sender, RoutedEventArgs e)
        {
            int temp = Metal1Combo.SelectedIndex;
            Metal1Combo.SelectedIndex = Metal2Combo.SelectedIndex;
            Metal2Combo.SelectedIndex = temp;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

