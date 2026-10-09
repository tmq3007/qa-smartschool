using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QASmartClass.Classroom.Helpers;
using QASmartClass.Classroom.Services;

namespace QASmartClass.Classroom.Views
{
    public partial class ConfigurePeriodTimesDialog : Window
    {
        private ObservableCollection<PeriodTimeConfig> _morningPeriods = new();
        private ObservableCollection<PeriodTimeConfig> _afternoonPeriods = new();
        private string _activePreset = "Summer";

        public ConfigurePeriodTimesDialog()
        {
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void LoadCurrentSettings()
        {
            var current = PeriodScheduleService.Instance.GetCurrentConfig();
            _activePreset = PeriodScheduleService.Instance.ActivePreset;

            PopulatePeriodLists(current);
            HighlightPresetCard(_activePreset);
        }

        private void PopulatePeriodLists(List<PeriodTimeConfig> configs)
        {
            _morningPeriods.Clear();
            _afternoonPeriods.Clear();

            foreach (var c in configs.Where(p => p.Period <= 5).OrderBy(p => p.Period))
            {
                _morningPeriods.Add(c);
            }

            foreach (var c in configs.Where(p => p.Period > 5).OrderBy(p => p.Period))
            {
                _afternoonPeriods.Add(c);
            }

            icMorningPeriods.ItemsSource = _morningPeriods;
            icAfternoonPeriods.ItemsSource = _afternoonPeriods;

            if (_morningPeriods.Any())
            {
                SelectComboItem(cboMorningStart, _morningPeriods[0].Start);
            }
            if (_afternoonPeriods.Any())
            {
                SelectComboItem(cboAfternoonStart, _afternoonPeriods[0].Start);
            }
        }

        private void SelectComboItem(ComboBox combo, string text)
        {
            foreach (ComboBoxItem item in combo.Items)
            {
                if (string.Equals(item.Content?.ToString(), text, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedItem = item;
                    return;
                }
            }
        }

        private void HighlightPresetCard(string preset)
        {
            _activePreset = preset;
            var activeBorder = new SolidColorBrush(Color.FromRgb(37, 99, 235));
            var defaultBorder = new SolidColorBrush(Color.FromRgb(203, 213, 225));
            var activeBg = new SolidColorBrush(Color.FromRgb(239, 246, 255));
            var defaultBg = new SolidColorBrush(Color.FromRgb(241, 245, 249));

            cardSummer.BorderBrush = preset == "Summer" ? new SolidColorBrush(Color.FromRgb(245, 158, 11)) : defaultBorder;
            cardSummer.Background = preset == "Summer" ? new SolidColorBrush(Color.FromRgb(254, 243, 199)) : defaultBg;

            cardWinter.BorderBrush = preset == "Winter" ? activeBorder : defaultBorder;
            cardWinter.Background = preset == "Winter" ? activeBg : defaultBg;

            cardStandard.BorderBrush = preset == "Standard" ? new SolidColorBrush(Color.FromRgb(34, 197, 94)) : defaultBorder;
            cardStandard.Background = preset == "Standard" ? new SolidColorBrush(Color.FromRgb(240, 253, 244)) : defaultBg;
        }

        private void SelectPresetSummer_Click(object sender, MouseButtonEventArgs e)
        {
            var list = PeriodScheduleService.GetPresetSummer();
            PopulatePeriodLists(list);
            HighlightPresetCard("Summer");
        }

        private void SelectPresetWinter_Click(object sender, MouseButtonEventArgs e)
        {
            var list = PeriodScheduleService.GetPresetWinter();
            PopulatePeriodLists(list);
            HighlightPresetCard("Winter");
        }

        private void SelectPresetStandard_Click(object sender, MouseButtonEventArgs e)
        {
            var list = PeriodScheduleService.GetPresetStandard();
            PopulatePeriodLists(list);
            HighlightPresetCard("Standard");
        }

        private void Recalculate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string mStartStr = (cboMorningStart.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "07:00";
                string aStartStr = (cboAfternoonStart.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "13:30";

                var mStart = TimeSpan.Parse(mStartStr);
                var aStart = TimeSpan.Parse(aStartStr);

                var list = PeriodScheduleService.CalculateSchedule(mStart, aStart);
                PopulatePeriodLists(list);
                HighlightPresetCard("Custom");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tính toán: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var all = new List<PeriodTimeConfig>();
            all.AddRange(_morningPeriods);
            all.AddRange(_afternoonPeriods);

            if (all.Count != 10)
            {
                MessageBox.Show("Phải có đủ 10 tiết học!", "Thiếu dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var timeRegex = new Regex("^(0[0-9]|1[0-9]|2[0-3]):[0-5][0-9]$");
            for (int i = 0; i < all.Count; i++)
            {
                var p = all[i];
                p.Start = p.Start?.Trim() ?? "";
                p.End = p.End?.Trim() ?? "";

                if (!timeRegex.IsMatch(p.Start) || !timeRegex.IsMatch(p.End))
                {
                    MessageBox.Show($"Thời gian Tiết {p.Period} ({p.Start} - {p.End}) không đúng định dạng HH:mm (00:00 - 23:59)!", "Lỗi định dạng", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var sTime = TimeSpan.Parse(p.Start);
                var eTime = TimeSpan.Parse(p.End);
                if (eTime <= sTime)
                {
                    MessageBox.Show($"Tiết {p.Period}: Thời gian kết thúc ({p.End}) phải sau thời gian bắt đầu ({p.Start})!", "Lỗi thời gian", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            try
            {
                PeriodScheduleService.Instance.SaveSchedule(ClassroomAppContext.Db, all, _activePreset);
                ClassroomDialog.Info("Đã lưu khung giờ học thành công! Toàn bộ Thời khóa biểu đã được cập nhật.", "Thành công");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lưu cấu hình: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
