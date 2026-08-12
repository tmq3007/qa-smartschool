using System;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SmartLibrary.Desktop.ViewModels
{
    public partial class GreenEnergyViewModel : ObservableObject, IActiveAwareViewModel
    {
        [ObservableProperty] private double _savedEnergyKwh = 142.5;
        [ObservableProperty] private int _currentLoadWatts = 340;
        [ObservableProperty] private double _co2ReductionKg = 7.12;

        public ObservableCollection<SensorZoneDto> SensorZones { get; } = new();

        private readonly DispatcherTimer _timer;
        private readonly Random _random = new();

        public GreenEnergyViewModel()
        {
            // Nạp các vùng cảm biến giả lập
            SensorZones.Add(new SensorZoneDto { ZoneName = "Phòng tự học Độc giả lớn", ActiveDevicesText = "Đèn 6/6 - Điều hòa BẬT", IsPersonDetected = true });
            SensorZones.Add(new SensorZoneDto { ZoneName = "Khu kệ sách Giáo khoa khối 10", ActiveDevicesText = "Đèn TẮT - Tiết kiệm điện", IsPersonDetected = false });
            SensorZones.Add(new SensorZoneDto { ZoneName = "Phòng đọc Nhi đồng và tranh truyện", ActiveDevicesText = "Đèn 2/4 - Điều hòa TIẾT KIỆM", IsPersonDetected = true });
            SensorZones.Add(new SensorZoneDto { ZoneName = "Góc nghiên cứu Kế toán và Giáo viên", ActiveDevicesText = "Đèn TẮT - Tiết kiệm điện", IsPersonDetected = false });

            // Timer tự cập nhật chỉ số năng lượng giả lập
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(3);
            _timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            // Biến thiên tải điện nhẹ nhàng
            CurrentLoadWatts = _random.Next(280, 480);
            SavedEnergyKwh += _random.NextDouble() * 0.05;
            Co2ReductionKg = SavedEnergyKwh * 0.05; // 1 kWh = 0.05 kg CO2 giảm thiểu

            // Thay đổi trạng thái ngẫu nhiên của các cảm biến chuyển động
            foreach (var zone in SensorZones)
            {
                zone.IsPersonDetected = _random.Next(0, 2) == 1;
                if (zone.IsPersonDetected)
                {
                    zone.ActiveDevicesText = "Đèn BẬT - Phát hiện hoạt động";
                }
                else
                {
                    zone.ActiveDevicesText = "Đèn TẮT - Trạng thái tiết kiệm";
                }
            }
        }

        public void Activate()
        {
            _timer.Start();
        }

        public void Cleanup()
        {
            _timer.Stop();
        }
    }

    public partial class SensorZoneDto : ObservableObject
    {
        public string ZoneName { get; set; } = string.Empty;
        [ObservableProperty] private string _activeDevicesText = string.Empty;
        [ObservableProperty] private bool _isPersonDetected;
    }
}
