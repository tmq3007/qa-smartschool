using System;
using System.Windows;
using System.Windows.Controls;
using QASmartClass.Data;
using QASmartClass.Services;

namespace QASmartClass.HealthRoom.Views
{
    public partial class MedicalInventoryView : UserControl
    {
        private readonly AppDbContext _db;
        private readonly MedicalInventoryService _inventoryService;

        public MedicalInventoryView()
        {
            InitializeComponent();
            _db = new AppDbContext();
            _inventoryService = new MedicalInventoryService(_db);
            DpExpiryDate.SelectedDate = DateTime.Today.AddYears(1);
            LoadInventory();
            Unloaded += (s, e) => { _db?.Dispose(); };
        }

        private void LoadInventory()
        {
            LvInventory.ItemsSource = _inventoryService.GetAllSupplies();
        }

        private void BtnLoadAll_Click(object sender, RoutedEventArgs e)
        {
            LoadInventory();
        }

        private void BtnLoadExpiring_Click(object sender, RoutedEventArgs e)
        {
            LvInventory.ItemsSource = _inventoryService.GetExpiringSupplies(30);
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtName.Text) || 
                !int.TryParse(TxtQuantity.Text, out int quantity) || 
                quantity <= 0 || 
                string.IsNullOrWhiteSpace(TxtUnit.Text))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ tên vật tư, số lượng (phải lớn hơn 0) và đơn vị tính hợp lệ.", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int minAlert = 10;
            if (!string.IsNullOrWhiteSpace(TxtMinAlertQty.Text))
            {
                if (!int.TryParse(TxtMinAlertQty.Text, out minAlert) || minAlert < 0)
                {
                    MessageBox.Show("Vui lòng nhập mức cảnh báo hợp lệ (số nguyên >= 0).", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            var categoryItem = CmbCategory.SelectedItem as ComboBoxItem;
            var supply = new MedicalSupply
            {
                Name = TxtName.Text,
                Category = categoryItem?.Tag?.ToString() ?? "Medicine",
                Quantity = quantity,
                Unit = TxtUnit.Text,
                ExpiryDate = DpExpiryDate.SelectedDate ?? DateTime.Today.AddYears(1),
                MinAlertQty = minAlert
            };

            _inventoryService.AddSupply(supply);
            
            TxtName.Text = "";
            TxtQuantity.Text = "";
            TxtMinAlertQty.Text = "10";
            
            LoadInventory();
        }

        private void BtnConsume_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is MedicalSupply supply)
            {
                var parentWindow = Window.GetWindow(this);
                var dialog = new InputDialog($"Bạn muốn xuất dùng bao nhiêu {supply.Unit} '{supply.Name}'?\n(Hiện có: {supply.Quantity} {supply.Unit})", "1")
                {
                    Owner = parentWindow
                };

                if (dialog.ShowDialog() == true)
                {
                    string input = dialog.Answer;
                    if (!int.TryParse(input, out int deductQty) || deductQty <= 0)
                    {
                        MessageBox.Show("Số lượng xuất phải là số nguyên dương lớn hơn 0.", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (deductQty > supply.Quantity)
                    {
                        MessageBox.Show($"Không thể xuất quá số lượng tồn kho hiện tại ({supply.Quantity} {supply.Unit}).", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    int newQty = supply.Quantity - deductQty;
                    _inventoryService.UpdateQuantity(supply.Id, newQty);

                    // Add an Audit Log
                    string reporter = QASmartClass.Staff.Services.StaffSession.CurrentUser?.FullName ?? "Y tế học đường";
                    QASmartClass.Services.AuditHelper.Log(_db, "Medical_Inventory_Consumed", reporter, $"Consumed {deductQty} {supply.Unit} of {supply.Name}");

                    LoadInventory();

                    if (newQty == 0)
                    {
                        MessageBox.Show($"Vật tư '{supply.Name}' đã hết hoàn toàn trong kho.", "Cảnh báo hết kho", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else if (newQty < supply.MinAlertQty)
                    {
                        MessageBox.Show($"Cảnh báo: Số lượng '{supply.Name}' hiện tại ({newQty}) đã giảm xuống dưới mức tồn tối thiểu ({supply.MinAlertQty}).", "Cảnh báo tồn kho thấp", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }
    }

    public class InputDialog : Window
    {
        private TextBox txtInput;
        private Button btnOk;
        private Button btnCancel;
        public string Answer { get; private set; }

        public InputDialog(string question, string defaultAnswer = "")
        {
            Title = "Nhập số lượng xuất";
            Width = 350;
            Height = 150;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F8F9FA"));

            var grid = new Grid { Margin = new Thickness(15) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var lblQuestion = new TextBlock
            {
                Text = question,
                Margin = new Thickness(0, 0, 0, 10),
                FontWeight = FontWeights.SemiBold,
                Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1E293B")),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(lblQuestion, 0);
            grid.Children.Add(lblQuestion);

            txtInput = new TextBox
            {
                Text = defaultAnswer,
                Padding = new Thickness(5),
                Margin = new Thickness(0, 0, 0, 15)
            };
            Grid.SetRow(txtInput, 1);
            grid.Children.Add(txtInput);

            var spButtons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetRow(spButtons, 2);

            btnOk = new Button
            {
                Content = "Xác nhận",
                Width = 80,
                Height = 28,
                IsDefault = true,
                Margin = new Thickness(0, 0, 10, 0),
                Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#10B981")),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0)
            };
            btnOk.Click += (s, ev) => { Answer = txtInput.Text; DialogResult = true; Close(); };
            spButtons.Children.Add(btnOk);

            btnCancel = new Button
            {
                Content = "Hủy",
                Width = 80,
                Height = 28,
                IsCancel = true,
                Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#EF4444")),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0)
            };
            btnCancel.Click += (s, ev) => { DialogResult = false; Close(); };
            spButtons.Children.Add(btnCancel);

            grid.Children.Add(spButtons);
            Content = grid;
        }
    }

    public class MedicalCategoryConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string cat)
            {
                return cat switch
                {
                    "Medicine" => "Thuốc",
                    "FirstAid" => "Sơ cứu",
                    "Equipment" => "Thiết bị",
                    _ => cat
                };
            }
            return value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }

    public class QuantityAlertColorConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is MedicalSupply supply)
            {
                if (supply.Quantity < supply.MinAlertQty)
                {
                    return System.Windows.Media.Brushes.Red;
                }
            }
            return System.Windows.Media.Brushes.Black;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }

    public class QuantityAlertWeightConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is MedicalSupply supply)
            {
                if (supply.Quantity < supply.MinAlertQty)
                {
                    return System.Windows.FontWeights.Bold;
                }
            }
            return System.Windows.FontWeights.Normal;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }

    public class ExpiryAlertColorConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is DateTime expiryDate)
            {
                if (expiryDate.Date <= DateTime.Today)
                {
                    return System.Windows.Media.Brushes.Red;
                }
                else if (expiryDate.Date <= DateTime.Today.AddDays(30))
                {
                    return System.Windows.Media.Brushes.Orange;
                }
            }
            return System.Windows.Media.Brushes.Black;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }

    public class ExpiryAlertWeightConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is DateTime expiryDate)
            {
                if (expiryDate.Date <= DateTime.Today.AddDays(30))
                {
                    return System.Windows.FontWeights.Bold;
                }
            }
            return System.Windows.FontWeights.Normal;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }
}

