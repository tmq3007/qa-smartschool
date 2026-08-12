using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Linq;
using QASmartTouch.PeriodicTable.ViewModels;

namespace QASmartTouch.PeriodicTable.Views
{
    public partial class ElementDetailWindow : Window
    {
        private ElementDetailViewModel ViewModel => DataContext as ElementDetailViewModel;

        public ElementDetailWindow()
        {
            InitializeComponent();
            Loaded += ElementDetailWindow_Loaded;
            DataContextChanged += ElementDetailWindow_DataContextChanged;
        }

        private void ElementDetailWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            BuildComparisonTable();
            BuildElectronShells();
        }

        private void ElementDetailWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateNavigationButtons();
            BuildComparisonTable();
            BuildElectronShells();
        }

        private void BuildElectronShells()
        {
            if (ViewModel?.CurrentElement == null || ElectronShellsPanel == null) return;

            var element = ViewModel.CurrentElement;
            if (element.ElectronShells == null || element.ElectronShells.Count == 0) return;

            ElectronShellsPanel.Children.Clear();

            string[] shellNames = { "K", "L", "M", "N", "O", "P", "Q" };

            for (int i = 0; i < element.ElectronShells.Count; i++)
            {
                int electronCount = element.ElectronShells[i];
                string shellName = i < shellNames.Length ? shellNames[i] : $"Shell {i + 1}";

                var shellPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 5, 0, 5)
                };

                // Shell Label
                var labelText = new TextBlock
                {
                    Text = "Lớp:",
                    FontSize = 14,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")),
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                shellPanel.Children.Add(labelText);

                // Shell Name Badge
                var shellBorder = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EEF2FF")),
                    CornerRadius = new CornerRadius(15),
                    Padding = new Thickness(10, 5, 10, 5),
                    Margin = new Thickness(0, 0, 15, 0),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#667EEA")),
                    BorderThickness = new Thickness(1)
                };
                var shellNameText = new TextBlock
                {
                    Text = shellName,
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#667EEA"))
                };
                shellBorder.Child = shellNameText;
                shellPanel.Children.Add(shellBorder);

                // Electron Dots
                var dotsText = new TextBlock
                {
                    Text = string.Join(" ", System.Linq.Enumerable.Repeat("●", electronCount)),
                    FontSize = 16,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#667EEA")),
                    VerticalAlignment = VerticalAlignment.Center
                };
                shellPanel.Children.Add(dotsText);

                // Electron Count
                var countText = new TextBlock
                {
                    Text = $"({electronCount} e⁻)",
                    FontSize = 14,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333")),
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(15, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                shellPanel.Children.Add(countText);

                ElectronShellsPanel.Children.Add(shellPanel);
            }
        }

        private void BuildComparisonTable()
        {
            if (ViewModel?.CurrentElement == null || ComparisonTablePanel == null) return;

            var element = ViewModel.CurrentElement;
            if (element.ComparisonTableData == null || element.ComparisonTableData.Count == 0) return;

            ComparisonTablePanel.Children.Clear();

            // Get column symbols from first row's Values keys
            var firstRow = element.ComparisonTableData[0];
            var columnSymbols = firstRow.Values.Keys.ToList();
            if (columnSymbols.Count == 0) return;

            int columnCount = columnSymbols.Count + 1; // +1 for property name column

            // Create header
            var headerBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#667EEA")),
                CornerRadius = new CornerRadius(12, 12, 0, 0),
                Padding = new Thickness(20, 15, 20, 15)
            };

            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
            for (int i = 0; i < columnSymbols.Count; i++)
            {
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            // Header: Property name
            var headerPropertyText = new TextBlock
            {
                Text = "Thuộc tính",
                FontWeight = FontWeights.SemiBold,
                FontSize = 14,
                Foreground = Brushes.White
            };
            Grid.SetColumn(headerPropertyText, 0);
            headerGrid.Children.Add(headerPropertyText);

            // Header: Element symbols
            for (int i = 0; i < columnSymbols.Count; i++)
            {
                var symbolText = new TextBlock
                {
                    Text = columnSymbols[i],
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 14,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetColumn(symbolText, i + 1);
                headerGrid.Children.Add(symbolText);
            }

            headerBorder.Child = headerGrid;
            ComparisonTablePanel.Children.Add(headerBorder);

            // Create rows (skip rows where all values are "—")
            foreach (var row in element.ComparisonTableData)
            {
                // Check if all values are empty ("—")
                bool allEmpty = true;
                foreach (var symbol in columnSymbols)
                {
                    var value = row.Values.ContainsKey(symbol) ? row.Values[symbol] : "—";
                    if (value != "—" && !string.IsNullOrWhiteSpace(value))
                    {
                        allEmpty = false;
                        break;
                    }
                }

                // Skip this row if all values are empty
                if (allEmpty) continue;

                var rowBorder = new Border
                {
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0")),
                    Padding = new Thickness(20, 15, 20, 15)
                };

                var rowGrid = new Grid();
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
                for (int i = 0; i < columnSymbols.Count; i++)
                {
                    rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                }

                // Property name
                var propertyText = new TextBlock
                {
                    Text = row.PropertyName,
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333")),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(propertyText, 0);
                rowGrid.Children.Add(propertyText);

                // Values
                for (int i = 0; i < columnSymbols.Count; i++)
                {
                    var symbol = columnSymbols[i];
                    var value = row.Values.ContainsKey(symbol) ? row.Values[symbol] : "—";
                    
                    var valueText = new TextBlock
                    {
                        Text = value,
                        FontSize = 14,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333")),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(valueText, i + 1);
                    rowGrid.Children.Add(valueText);
                }

                rowBorder.Child = rowGrid;
                ComparisonTablePanel.Children.Add(rowBorder);
            }
        }

        private void UpdateNavigationButtons()
        {
            if (ViewModel != null)
            {
                PrevButton.IsEnabled = ViewModel.CanNavigatePrevious();
                NextButton.IsEnabled = ViewModel.CanNavigateNext();
            }
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.NavigateToPrevious();
                UpdateNavigationButtons();
                BuildComparisonTable();
                BuildElectronShells();
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.NavigateToNext();
                UpdateNavigationButtons();
                BuildComparisonTable();
                BuildElectronShells();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

