using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Serilog;
using QASmartClass.Data;

namespace QASmartClass.Classroom.Views
{
    public partial class RandomPickerPage : Page
    {
        private List<Student> _allStudents = new();
        private List<Student> _filteredAllStudents = new();
        private List<Student> _available = new();   // not yet picked this round
        private List<string> _history = new();
        private readonly Random _rand = new();
        private int _pickMode = 0; // 0=Single 1=Group
        private int _groupSize = 3;
        private int _singlePickCounter = 0;
        private HashSet<int> _absentStudentIds = new();

        public RandomPickerPage()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                LoadStudents();
                try
                {
                    var app = (QASmartTouch.App)Application.Current;
                    app.ClassRoster.ActiveRosterChanged += OnActiveRosterChanged;
                }
                catch { }
            };
            Unloaded += (_, _) =>
            {
                try
                {
                    var app = (QASmartTouch.App)Application.Current;
                    app.ClassRoster.ActiveRosterChanged -= OnActiveRosterChanged;
                }
                catch { }
            };
        }

        private void OnActiveRosterChanged(object? sender, Data.ClassRoster? e)
        {
            Dispatcher.Invoke(() =>
            {
                LoadStudents();
                _history.Clear();
                _singlePickCounter = 0;
                RefreshHistory();
            });
        }

        // ═════════════════════════════════════════════════════
        //  LOAD
        // ═════════════════════════════════════════════════════

        private void LoadStudents()
        {
            try
            {
                var students = QASmartClass.Classroom.Services.RosterHelper.GetStudents();
                if (students != null && students.Any())
                {
                    _allStudents = students;
                    if (emptyStateOverlay != null) emptyStateOverlay.Visibility = Visibility.Collapsed;
                }
                else
                {
                    _allStudents = new List<Student>();
                    if (emptyStateOverlay != null) emptyStateOverlay.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("RandomPicker failed to load students: {Err}", ex.Message);
                _allStudents = new List<Student>();
                if (emptyStateOverlay != null) emptyStateOverlay.Visibility = Visibility.Visible;
            }

            LoadAttendanceData();
            ResetAvailable();
            
            var rosterName = QASmartClass.Classroom.Services.RosterHelper.GetActiveRosterName();
            Log.Information("RandomPicker loaded: {Count} students ({Roster})", _allStudents.Count, rosterName);
        }

        private void LoadAttendanceData()
        {
            _absentStudentIds.Clear();
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                var targetDate = DateTime.Today.Date;
                var activeRoster = app.ClassRoster.ActiveRoster;
                int rosterId = activeRoster?.Id ?? 0;

                var absentRecords = app.Database.AttendanceRecords
                    .Where(r => r.RosterId == rosterId && r.Date.Date == targetDate && r.Status == "absent")
                    .Select(r => r.StudentId)
                    .ToList();

                _absentStudentIds = new HashSet<int>(absentRecords);
            }
            catch (Exception ex)
            {
                Log.Warning("RandomPicker failed to load attendance: {Err}", ex.Message);
            }
        }

        private void ResetAvailable()
        {
            if (chkExcludeAbsent != null && chkExcludeAbsent.IsChecked == true)
            {
                _filteredAllStudents = _allStudents.Where(s => !_absentStudentIds.Contains(s.Id)).ToList();
            }
            else
            {
                _filteredAllStudents = new List<Student>(_allStudents);
            }

            _available = new List<Student>(_filteredAllStudents);
            UpdateCount();
        }

        private void UpdateCount()
        {
            if (txtRemaining == null || txtRemaining2 == null) return;

            if (chkAllowDuplicates != null && chkAllowDuplicates.IsChecked == true)
            {
                txtRemaining.Text = $"Chế độ gọi trùng: {_filteredAllStudents.Count} HS sẵn sàng";
                txtRemaining2.Text = $"Tổng số sẵn sàng: {_filteredAllStudents.Count} HS (Gọi lặp)";
            }
            else
            {
                txtRemaining.Text = $"Còn lại: {_available.Count}/{_filteredAllStudents.Count} HS";
                txtRemaining2.Text = $"Chưa được gọi: {_available.Count}/{_filteredAllStudents.Count} HS";
            }
        }

        private void Option_Changed(object sender, RoutedEventArgs e)
        {
            ResetAvailable();
        }

        // ═════════════════════════════════════════════════════
        //  MODE & GROUP SIZE
        // ═════════════════════════════════════════════════════

        private void ModeSingle_Click(object sender, RoutedEventArgs e)
        {
            _pickMode = 0;
            btnModeSingle.Background = new SolidColorBrush(Color.FromRgb(25, 118, 210));
            btnModeSingle.Foreground = Brushes.White;
            btnModeGroup.Background  = new SolidColorBrush(Color.FromRgb(245, 245, 245));
            btnModeGroup.Foreground  = new SolidColorBrush(Color.FromRgb(66, 66, 66));
            groupSizePanel.Visibility = Visibility.Collapsed;

            // Reset Card back to default state when switching modes
            txtPickResult.Visibility = Visibility.Visible;
            groupResultsList.Visibility = Visibility.Collapsed;
            txtPickResult.FontSize = 28;
            txtPickResult.Text    = "Nhấn 🎲 để bắt đầu";
            txtPickAvatar.Text    = "🎲";
            txtPickResultSub.Text = "Chọn học sinh ngẫu nhiên";
            txtPickResult.Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66));
            resultCard.Background    = Brushes.White;
        }

        private void ModeGroup_Click(object sender, RoutedEventArgs e)
        {
            _pickMode = 1;
            btnModeGroup.Background  = new SolidColorBrush(Color.FromRgb(2, 136, 209));
            btnModeGroup.Foreground  = Brushes.White;
            btnModeSingle.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
            btnModeSingle.Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66));
            groupSizePanel.Visibility = Visibility.Visible;

            // Reset Card back to default state when switching modes
            txtPickResult.Visibility = Visibility.Visible;
            groupResultsList.Visibility = Visibility.Collapsed;
            txtPickResult.FontSize = 28;
            txtPickResult.Text    = "Nhấn 🎲 để bắt đầu";
            txtPickAvatar.Text    = "🎲";
            txtPickResultSub.Text = "Chọn học sinh ngẫu nhiên";
            txtPickResult.Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66));
            resultCard.Background    = Brushes.White;
        }

        private void GroupMinus_Click(object sender, RoutedEventArgs e)
        {
            _groupSize = Math.Max(2, _groupSize - 1);
            txtGroupSize.Text = _groupSize.ToString();
        }

        private void GroupPlus_Click(object sender, RoutedEventArgs e)
        {
            _groupSize = Math.Min(8, _groupSize + 1);
            txtGroupSize.Text = _groupSize.ToString();
        }

        // ═════════════════════════════════════════════════════
        //  SPIN = PICK
        // ═════════════════════════════════════════════════════

        private void SetControlsEnabled(bool enabled)
        {
            btnModeSingle.IsEnabled = enabled;
            btnModeGroup.IsEnabled = enabled;
            btnSpin.IsEnabled = enabled;
            btnReset.IsEnabled = enabled;
            btnClearHistory.IsEnabled = enabled;
            chkAllowDuplicates.IsEnabled = enabled;
            chkExcludeAbsent.IsEnabled = enabled;
            btnGroupMinus.IsEnabled = enabled;
            btnGroupPlus.IsEnabled = enabled;
        }

        private void PlaySpinSound()
        {
            try
            {
                System.Media.SystemSounds.Beep.Play();
            }
            catch { }
        }

        private void PlayDingSound()
        {
            try
            {
                System.Media.SystemSounds.Asterisk.Play();
            }
            catch { }
        }

        private async void Spin_Click(object sender, RoutedEventArgs e)
        {
            if (_filteredAllStudents.Count == 0)
            {
                MessageBox.Show("Không có học sinh nào khả dụng để chọn!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool allowDup = chkAllowDuplicates?.IsChecked == true;
            if (!allowDup && _available.Count == 0)
            {
                var r = MessageBox.Show("Đã chọn hết học sinh!\nBắt đầu lại vòng mới?",
                    "Hết danh sách", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (r == MessageBoxResult.Yes) ResetAvailable();
                else return;
            }

            SetControlsEnabled(false);
            txtPickResult.Foreground = new SolidColorBrush(Color.FromRgb(97, 97, 97));

            try
            {
                if (_pickMode == 0)
                    await PickSingleAsync();
                else
                    await PickGroupAsync();
            }
            catch (Exception ex)
            {
                Log.Error("RandomPicker error during spin: {Err}", ex.Message);
                MessageBox.Show("Đã xảy ra lỗi trong quá trình chọn học sinh ngẫu nhiên.", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetControlsEnabled(true);
                UpdateCount();
                RefreshHistory();
            }
        }

        private async System.Threading.Tasks.Task PickSingleAsync()
        {
            // Slot machine animation
            int steps  = 18;
            int delay  = 40;
            Student picked = null!;
            bool allowDup = chkAllowDuplicates?.IsChecked == true;

            txtPickResult.Visibility = Visibility.Visible;
            groupResultsList.Visibility = Visibility.Collapsed;
            txtPickResult.FontSize = 32;

            for (int i = 0; i < steps; i++)
            {
                var sample = _available.Count > 0
                    ? _available[_rand.Next(_available.Count)]
                    : _filteredAllStudents[_rand.Next(_filteredAllStudents.Count)];
                txtPickResult.Text     = sample.FullName;
                txtPickAvatar.Text     = sample.FullName.Length > 0 ? sample.FullName[0].ToString() : "?";
                txtPickResultSub.Text  = $"🎲 {i + 1}/{steps}...";
                PlaySpinSound();
                await System.Threading.Tasks.Task.Delay(delay + i * 8);
            }

            // Final pick
            if (allowDup)
            {
                picked = _filteredAllStudents[_rand.Next(_filteredAllStudents.Count)];
            }
            else
            {
                picked = _available[_rand.Next(_available.Count)];
                _available.Remove(picked);
            }

            _singlePickCounter++;
            _history.Insert(0, $"#{_singlePickCounter} — {picked.FullName}");

            txtPickResult.Text    = picked.FullName;
            txtPickAvatar.Text    = picked.FullName.Length > 0 ? picked.FullName[0].ToString() : "✓";
            txtPickResultSub.Text = "🎉 Được chọn!";
            txtPickResult.Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210));
            PlayDingSound();

            // Flash animation
            for (int i = 0; i < 3; i++)
            {
                resultCard.Background = new SolidColorBrush(Color.FromRgb(227, 242, 253));
                await System.Threading.Tasks.Task.Delay(200);
                resultCard.Background = Brushes.White;
                await System.Threading.Tasks.Task.Delay(200);
            }
            resultCard.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233));
            Log.Information("Random pick: {Name}", picked.FullName);
        }

        private async System.Threading.Tasks.Task PickGroupAsync()
        {
            bool allowDup = chkAllowDuplicates?.IsChecked == true;
            int maxAvailable = allowDup ? _filteredAllStudents.Count : _available.Count;
            int count = Math.Min(_groupSize, maxAvailable);

            if (count == 0)
            {
                txtPickResult.Visibility = Visibility.Visible;
                groupResultsList.Visibility = Visibility.Collapsed;
                txtPickResult.FontSize = 18;
                txtPickResult.Text = "Không đủ học sinh để lập nhóm!";
                txtPickAvatar.Text = "⚠️";
                txtPickResultSub.Text = "Vui lòng đặt lại hoặc chọn chế độ gọi trùng";
                return;
            }

            txtPickResult.Visibility = Visibility.Visible;
            groupResultsList.Visibility = Visibility.Collapsed;
            txtPickResult.FontSize = 16;
            txtPickResult.Text    = $"Đang chọn {count} học sinh...";
            txtPickAvatar.Text    = "👥";
            txtPickResultSub.Text = $"Nhóm {count} người";

            await System.Threading.Tasks.Task.Delay(800);

            var picked = new List<Student>();
            var pool   = allowDup ? new List<Student>(_filteredAllStudents) : new List<Student>(_available);
            
            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int idx = _rand.Next(pool.Count);
                var s = pool[idx];
                picked.Add(s);
                if (!allowDup)
                {
                    _available.Remove(s);
                }
                pool.RemoveAt(idx);
            }

            _history.Insert(0, $"Nhóm — {string.Join(", ", picked.Select(s => s.FullName))}");
            
            txtPickResult.Visibility = Visibility.Collapsed;
            groupResultsList.Visibility = Visibility.Visible;
            groupResultsList.ItemsSource = picked.Select(s => s.FullName).ToList();

            txtPickAvatar.Text    = "👥";
            txtPickResultSub.Text = $"🎉 {count} học sinh được chọn!";
            resultCard.Background = new SolidColorBrush(Color.FromRgb(243, 229, 245));
            PlayDingSound();
            Log.Information("Group pick ({Count}): {Names}", count, string.Join(", ", picked.Select(s => s.FullName)));
        }

        // ═════════════════════════════════════════════════════
        //  HISTORY & RESET
        // ═════════════════════════════════════════════════════

        private void RefreshHistory()
        {
            historyList.ItemsSource = null;
            historyList.ItemsSource = _history.Take(20).ToList();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            ResetAvailable();
            _singlePickCounter = 0;
            txtPickResult.Visibility = Visibility.Visible;
            groupResultsList.Visibility = Visibility.Collapsed;
            txtPickResult.FontSize = 28;
            txtPickResult.Text    = "Nhấn 🎲 để bắt đầu";
            txtPickAvatar.Text    = "🎲";
            txtPickResultSub.Text = "Chọn học sinh ngẫu nhiên";
            txtPickResult.Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66));
            resultCard.Background    = Brushes.White;
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            _history.Clear();
            _singlePickCounter = 0;
            RefreshHistory();
        }
    }
}
