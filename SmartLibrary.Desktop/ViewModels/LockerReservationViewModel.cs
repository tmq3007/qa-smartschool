using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SmartLibrary.Desktop.ViewModels;

public partial class LockerReservationViewModel : ObservableObject
{
    [ObservableProperty]
    private LockerItemViewModel? _selectedLocker;

    [ObservableProperty]
    private string _inputPin = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _successMessage = string.Empty;

    [ObservableProperty]
    private bool _isPinDialogOpen = false;

    public ObservableCollection<LockerItemViewModel> Lockers { get; } = new();

    public ICommand SelectLockerCommand { get; }
    public ICommand SubmitPinCommand { get; }
    public ICommand CloseDialogCommand { get; }

    public LockerReservationViewModel()
    {
        SelectLockerCommand = new RelayCommand<LockerItemViewModel>(SelectLocker);
        SubmitPinCommand = new RelayCommand(SubmitPin);
        CloseDialogCommand = new RelayCommand(() => IsPinDialogOpen = false);

        InitializeLockers();
    }

    private void InitializeLockers()
    {
        Lockers.Clear();
        for (int i = 1; i <= 12; i++)
        {
            var status = "Empty";
            var pin = string.Empty;
            var book = string.Empty;

            if (i == 3)
            {
                status = "Reserved";
                pin = "1234";
                book = "Đắc Nhân Tâm";
            }
            else if (i == 5)
            {
                status = "Reserved";
                pin = "9876";
                book = "Nhà Giả Kim";
            }

            Lockers.Add(new LockerItemViewModel
            {
                LockerNumber = $"#{i:00}",
                Status = status,
                ReservedPin = pin,
                BookTitle = book
            });
        }
    }

    private void SelectLocker(LockerItemViewModel? locker)
    {
        if (locker == null) return;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        InputPin = string.Empty;

        if (locker.Status == "Empty")
        {
            System.Windows.MessageBox.Show("Ô tủ này đang trống. Hãy đăng ký đặt tủ từ chi tiết sách mượn trước!", "Thông báo", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }

        if (locker.Status == "Opened")
        {
            System.Windows.MessageBox.Show("Ô tủ này đã mở! Vui lòng lấy sách ra.", "Thông báo", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }

        SelectedLocker = locker;
        IsPinDialogOpen = true;
    }

    private void SubmitPin()
    {
        if (SelectedLocker == null) return;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        if (InputPin == SelectedLocker.ReservedPin)
        {
            SelectedLocker.Status = "Opened";
            SuccessMessage = $"🔑 MỞ TỦ THÀNH CÔNG!\nVui lòng lấy sách: '{SelectedLocker.BookTitle}'";
            IsPinDialogOpen = false;

            // Tự động giải phóng ô tủ sau 5 giây giả lập đóng cửa
            var currentLocker = SelectedLocker;
            Task.Delay(5000).ContinueWith(t =>
            {
                currentLocker.Status = "Empty";
                currentLocker.ReservedPin = string.Empty;
                currentLocker.BookTitle = string.Empty;
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
        else
        {
            ErrorMessage = "❌ Mã PIN không chính xác! Hãy kiểm tra lại tin nhắn hoặc email.";
            
            // Chụp lại đối tượng locker cụ thể để tránh Race Condition khi người dùng chọn locker khác
            var failedLocker = SelectedLocker;
            failedLocker.IsErrorState = true;
            Task.Delay(1000).ContinueWith(t =>
            {
                failedLocker.IsErrorState = false;
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
    }
}

public partial class LockerItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _lockerNumber = string.Empty;

    [ObservableProperty]
    private string _status = "Empty"; // Empty, Reserved, Opened

    [ObservableProperty]
    private string _reservedPin = string.Empty;

    [ObservableProperty]
    private string _bookTitle = string.Empty;

    [ObservableProperty]
    private bool _isErrorState = false;

    public string StatusText => Status switch
    {
        "Empty" => "Trống",
        "Reserved" => "Đã đặt chỗ",
        "Opened" => "Đã mở cửa",
        _ => "Không khả dụng"
    };

    public string StatusColor => Status switch
    {
        "Empty" => "#475569",     // Slate Gray
        "Reserved" => "#F59E0B",  // Amber Orange
        "Opened" => "#10B981",    // Emerald Green
        _ => "#94A3B8"
    };
}
