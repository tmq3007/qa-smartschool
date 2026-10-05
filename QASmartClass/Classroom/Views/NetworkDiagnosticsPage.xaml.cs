using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using QASmartClass.Classroom.Services;
using QASmartClass.Classroom.Helpers;
using Serilog;

namespace QASmartClass.Classroom.Views
{
    public partial class NetworkDiagnosticsPage : Page
    {
        private ObservableCollection<DiagnosticStudentItem> _studentsList = new();
        private CancellationTokenSource? _cts;
        private bool _isDiagRunning = false;

        public NetworkDiagnosticsPage()
        {
            InitializeComponent();
            lvDiagResults.ItemsSource = _studentsList;
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (_isDiagRunning)
            {
                _cts?.Cancel();
            }

            // → ClassroomAppContext
            var shell = System.Windows.Application.Current.MainWindow as ClassroomShell;
            if (shell != null)
            {
                // Go back to MonitorPage (FormID F04) or BroadcastPage (FormID F05)
                _ = shell.NavigateToAsync("F04");
            }
        }

        private void btnRunDiag_Click(object sender, RoutedEventArgs e)
        {
            RunDiagnosticsAsync();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadConnectedStudents();
            RunDiagnosticsAsync();
        }

        private void LoadConnectedStudents()
        {
            _studentsList.Clear();
            // → ClassroomAppContext
            if (ClassroomAppContext.Network == null) return;

            // Get connected students from network service
            var clients = ClassroomAppContext.Network.GetConnectedStudents();
            if (clients == null || !clients.Any())
            {
                // Fallback to roster/DB students for testing
                var fallbackStudents = RosterHelper.GetStudents();
                foreach (var s in fallbackStudents)
                {
                    _studentsList.Add(new DiagnosticStudentItem
                    {
                        StudentCode = s.StudentCode,
                        Name = s.FullName,
                        IPAddress = s.IPAddress ?? "127.0.0.1",
                        IsChecking = false
                    });
                }
            }
            else
            {
                foreach (var client in clients)
                {
                    _studentsList.Add(new DiagnosticStudentItem
                    {
                        StudentCode = client.Code,
                        Name = client.Name,
                        IPAddress = client.IPAddress,
                        IsChecking = false
                    });
                }
            }
        }

        private async void RunDiagnosticsAsync()
        {
            if (_isDiagRunning) return;

            _isDiagRunning = true;
            btnRunDiag.IsEnabled = false;
            _cts = new CancellationTokenSource();

            txtProgressLabel.Text = "Đang bắt đầu chẩn đoán mạng...";
            progressBar.Value = 0;
            txtProgressPercent.Text = "0%";

            foreach (var item in _studentsList)
            {
                item.IsChecking = true;
                item.LatencyDisplay = "Đang đo...";
                item.LossDisplay = "Đang đo...";
                item.Evaluation = "⏳ Đang đo...";
            }

            byte[] buffer = new byte[1472];
            new Random().NextBytes(buffer);
            var pingOptions = new PingOptions(64, true); // Don't fragment to verify MTU path

            int totalStudents = _studentsList.Count;
            int completedCount = 0;

            var tasks = _studentsList.Select(async student =>
            {
                try
                {
                    var result = await PingStudentAsync(student.IPAddress, buffer, pingOptions, _cts.Token);
                    
                    student.AvgLatency = result.AvgLatency;
                    student.PacketLoss = result.PacketLoss;
                    student.IsChecking = false;

                    if (result.PacketLoss >= 100)
                    {
                        student.LatencyDisplay = "Không phản hồi";
                        student.LossDisplay = "100%";
                        student.Evaluation = "🔴 Mất kết nối";
                    }
                    else
                    {
                        student.LatencyDisplay = $"{result.AvgLatency:F1} ms";
                        student.LossDisplay = $"{result.PacketLoss}%";

                        if (result.PacketLoss > 5 || result.AvgLatency > 150)
                            student.Evaluation = "🟡 Chậm";
                        else
                            student.Evaluation = "🟢 Tốt";
                    }
                }
                catch (Exception ex)
                {
                    student.IsChecking = false;
                    student.Evaluation = "🔴 Lỗi ping";
                    Log.Debug("[NetDiag] Ping task error for student {Code}: {Err}", student.StudentCode, ex.Message);
                }
                finally
                {
                    Interlocked.Increment(ref completedCount);
                    Dispatcher.Invoke(() =>
                    {
                        double percent = (double)completedCount / totalStudents * 100;
                        progressBar.Value = percent;
                        txtProgressPercent.Text = $"{(int)percent}%";
                        txtProgressLabel.Text = $"Đang chẩn đoán: đã hoàn thành {completedCount}/{totalStudents} học sinh.";
                    });
                }
            }).ToList();

            if (totalStudents > 0)
            {
                await Task.WhenAll(tasks);
            }

            _isDiagRunning = false;
            btnRunDiag.IsEnabled = true;

            txtProgressLabel.Text = "Hoàn thành chẩn đoán mạng.";
            UpdateSummaryStats();
        }

        private async Task<PingResult> PingStudentAsync(string ip, byte[] buffer, PingOptions options, CancellationToken ct)
        {
            int success = 0;
            long totalRtt = 0;
            int count = 5;

            for (int i = 0; i < count; i++)
            {
                if (ct.IsCancellationRequested) break;
                try
                {
                    using (var ping = new Ping())
                    {
                        var reply = await ping.SendPingAsync(ip, 800, buffer, options);
                        if (reply != null && reply.Status == IPStatus.Success)
                        {
                            success++;
                            totalRtt += reply.RoundtripTime;
                        }
                    }
                }
                catch { }
                await Task.Delay(100, ct);
            }

            int loss = ((count - success) * 100) / count;
            double avgRtt = success > 0 ? (double)totalRtt / success : 999.0;
            return new PingResult { PacketLoss = loss, AvgLatency = avgRtt };
        }

        private void UpdateSummaryStats()
        {
            var validPings = _studentsList.Where(s => s.PacketLoss < 100).ToList();
            double avgLatency = validPings.Any() ? validPings.Average(s => s.AvgLatency) : 0;
            double avgLoss = _studentsList.Any() ? _studentsList.Average(s => s.PacketLoss) : 0;

            txtAvgLatency.Text = $"{avgLatency:F1} ms";
            txtPacketLoss.Text = $"{avgLoss:F1}%";

            // Status label formatting
            if (avgLatency > 150)
            {
                txtLatencyStatus.Text = "Độ trễ cao (>150ms)";
                txtLatencyStatus.Foreground = new SolidColorBrush(Color.FromRgb(211, 47, 47));
            }
            else if (avgLatency > 50)
            {
                txtLatencyStatus.Text = "Độ trễ trung bình";
                txtLatencyStatus.Foreground = new SolidColorBrush(Color.FromRgb(245, 124, 0));
            }
            else
            {
                txtLatencyStatus.Text = "Độ trễ cực thấp (Tốt)";
                txtLatencyStatus.Foreground = new SolidColorBrush(Color.FromRgb(56, 142, 60));
            }

            if (avgLoss > 5.0)
            {
                txtLossStatus.Text = "Mất gói cao (>5.0%)";
                txtLossStatus.Foreground = new SolidColorBrush(Color.FromRgb(211, 47, 47));
            }
            else
            {
                txtLossStatus.Text = "Tín hiệu ổn định (Tốt)";
                txtLossStatus.Foreground = new SolidColorBrush(Color.FromRgb(56, 142, 60));
            }

            // Adjust streaming quality dynamically in the singleton
            QASmartClass.Services.VncBroadcastService.Instance.AdjustStreamingQuality(avgLatency, avgLoss);

            if (avgLoss > 5.0 || avgLatency > 150)
            {
                txtVncQuality.Text = "⚡ Thấp (Adaptive)";
                txtVncQuality.Foreground = new SolidColorBrush(Color.FromRgb(211, 47, 47));
                txtVncStatusDetails.Text = "FPS hạ còn 15, zlib nâng lên level 7.";
            }
            else
            {
                txtVncQuality.Text = "🚀 Cao (30 FPS)";
                txtVncQuality.Foreground = new SolidColorBrush(Color.FromRgb(56, 142, 60));
                txtVncStatusDetails.Text = "VNC phát đầy đủ băng thông và chất lượng.";
            }
        }
    }

    public struct PingResult
    {
        public int PacketLoss { get; set; }
        public double AvgLatency { get; set; }
    }

    public class DiagnosticStudentItem : System.ComponentModel.INotifyPropertyChanged
    {
        private double _avgLatency;
        private double _packetLoss;
        private string _latencyDisplay = "Chờ kiểm tra";
        private string _lossDisplay = "Chờ kiểm tra";
        private string _evaluation = "⏳ Chờ...";
        private bool _isChecking;

        public string StudentCode { get; set; } = "";
        public string Name { get; set; } = "";
        public string IPAddress { get; set; } = "";

        public double AvgLatency
        {
            get => _avgLatency;
            set { _avgLatency = value; OnPropertyChanged(); }
        }

        public double PacketLoss
        {
            get => _packetLoss;
            set { _packetLoss = value; OnPropertyChanged(); }
        }

        public string LatencyDisplay
        {
            get => _latencyDisplay;
            set { _latencyDisplay = value; OnPropertyChanged(); }
        }

        public string LossDisplay
        {
            get => _lossDisplay;
            set { _lossDisplay = value; OnPropertyChanged(); }
        }

        public string Evaluation
        {
            get => _evaluation;
            set { _evaluation = value; OnPropertyChanged(); }
        }

        public bool IsChecking
        {
            get => _isChecking;
            set { _isChecking = value; OnPropertyChanged(); }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string prop = "") =>
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(prop));
    }

    public class LatencyColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d && d > 150)
                return "High";
            return "Normal";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class LossColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d && d > 5.0)
                return "High";
            return "Normal";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
