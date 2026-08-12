using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace QASmartTouch.Forms
{
    public partial class Form5_3_ProgressDialog : Window
    {
        private CancellationTokenSource? _cancellationTokenSource;
        public bool IsCancelled { get; private set; }

        public Form5_3_ProgressDialog()
        {
            InitializeComponent();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Update progress bar value (0-100)
        /// </summary>
        public void UpdateProgress(double value, string? statusText = null)
        {
            Dispatcher.Invoke(() =>
            {
                progressBar.Value = value;
                txtPercentage.Text = $"{value:F0}%";
                
                if (!string.IsNullOrEmpty(statusText))
                {
                    txtStatus.Text = statusText;
                }

                // Change color to green when complete
                if (value >= 100)
                {
                    progressBar.Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4CAF50"));
                    txtStatus.Text = "Hoàn thành!";
                }
            });
        }

        /// <summary>
        /// Set dialog title
        /// </summary>
        public void SetTitle(string title)
        {
            Dispatcher.Invoke(() =>
            {
                txtTitle.Text = title;
            });
        }

        /// <summary>
        /// Set status text
        /// </summary>
        public void SetStatus(string status)
        {
            Dispatcher.Invoke(() =>
            {
                txtStatus.Text = status;
            });
        }

        /// <summary>
        /// Show indeterminate progress (spinning)
        /// </summary>
        public void SetIndeterminate(bool isIndeterminate)
        {
            Dispatcher.Invoke(() =>
            {
                progressBar.IsIndeterminate = isIndeterminate;
                if (isIndeterminate)
                {
                    txtPercentage.Visibility = Visibility.Collapsed;
                }
                else
                {
                    txtPercentage.Visibility = Visibility.Visible;
                }
            });
        }

        /// <summary>
        /// Enable/disable cancel button
        /// </summary>
        public void SetCancelable(bool canCancel)
        {
            Dispatcher.Invoke(() =>
            {
                btnCancel.Visibility = canCancel ? Visibility.Visible : Visibility.Collapsed;
            });
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Bạn có chắc chắn muốn hủy?", 
                                       "Xác nhận", 
                                       MessageBoxButton.YesNo, 
                                       MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                IsCancelled = true;
                _cancellationTokenSource?.Cancel();
                this.DialogResult = false;
                this.Close();
            }
        }

        /// <summary>
        /// Simulate a long-running task with progress updates
        /// </summary>
        public async Task SimulateProgressAsync(int durationSeconds = 5)
        {
            int steps = 100;
            int delayMs = (durationSeconds * 1000) / steps;

            for (int i = 0; i <= steps; i++)
            {
                if (_cancellationTokenSource?.Token.IsCancellationRequested == true)
                {
                    break;
                }

                UpdateProgress(i, $"Đang xử lý... ({i}/{steps})");
                await Task.Delay(delayMs);
            }

            if (!IsCancelled)
            {
                UpdateProgress(100, "Hoàn thành!");
                await Task.Delay(500);
                this.DialogResult = true;
                this.Close();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _cancellationTokenSource?.Dispose();
            base.OnClosed(e);
        }
    }
}
