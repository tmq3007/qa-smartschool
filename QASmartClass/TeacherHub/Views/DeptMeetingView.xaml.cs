using QASmartClass.Data;
using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace QASmartClass.TeacherHub.Views
{
    public partial class DeptMeetingView : UserControl
    {
        public DeptMeetingView()
        {
            InitializeComponent();
            Loaded += Page_Loaded;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await InitAsync();
        }

        private async Task InitAsync()
        {
            try
            {
                using var db = new AppDbContext();
                var depts = await db.DeptMeetings.Select(m => m.DeptName).Distinct().ToListAsync();
                depts.Insert(0, "Tất cả");
                CboDept.ItemsSource = depts;
                CboDept.SelectedIndex = 0;
                await LoadMeetingsAsync();
            }
            catch (Exception ex)
            {
                Log.Warning("[DeptMeeting] Init error: {Err}", ex.Message);
            }
        }

        private async void CboDept_Changed(object sender, SelectionChangedEventArgs e)
        {
            await LoadMeetingsAsync();
        }

        private async Task LoadMeetingsAsync()
        {
            try
            {
                string dept = CboDept?.SelectedItem?.ToString() ?? "Tất cả";
                using var db = new AppDbContext();
                var meetings = await db.DeptMeetings.OrderByDescending(m => m.MeetingDate).ToListAsync();
                if (dept != "Tất cả")
                    meetings = meetings.Where(m => m.DeptName == dept).ToList();

                LvMeetings.ItemsSource = meetings.Select(m => new
                {
                    m.Id,
                    Title = $"[{m.DeptName}] {m.Agenda}",
                    DateStr = $"{m.MeetingDate:dd/MM/yyyy} | {m.Attendees}"
                }).ToList();
            }
            catch (Exception ex)
            {
                Log.Warning("[DeptMeeting] Load error: {Err}", ex.Message);
            }
        }

        private async void LvMeetings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LvMeetings.SelectedItem == null) return;
            dynamic sel = LvMeetings.SelectedItem;
            int meetingId = sel.Id;
            await LoadActionsAsync(meetingId);
        }

        private async Task LoadActionsAsync(int meetingId)
        {
            try
            {
                using var db = new AppDbContext();
                var actions = await db.DeptMeetingActions
                    .Where(a => a.MeetingId == meetingId)
                    .OrderBy(a => a.DueDate).ToListAsync();

                LstActions.ItemsSource = actions.Select(a => new
                {
                    a.Id,
                    a.Content,
                    a.IsCompleted,
                    Info = $"{a.AssignedTo} | Hạn: {a.DueDate:dd/MM}"
                }).ToList();

                TxtNoActions.Visibility = actions.Any() ? Visibility.Collapsed : Visibility.Visible;
            }
            catch (Exception ex)
            {
                Log.Warning("[DeptMeeting] Load actions error: {Err}", ex.Message);
            }
        }

        private async void ActionCheck_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.Tag is int id)
            {
                try
                {
                    using var db = new AppDbContext();
                    var action = await db.DeptMeetingActions.FindAsync(id);
                    if (action != null)
                    {
                        action.IsCompleted = cb.IsChecked == true;
                        await db.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("[DeptMeeting] Toggle error: {Err}", ex.Message);
                }
            }
        }

        private async void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var parentWindow = Window.GetWindow(this);
            var dlg = new AddMeetingWindow
            {
                Owner = parentWindow
            };
            if (dlg.ShowDialog() == true)
            {
                await InitAsync();
            }
        }
    }
}
