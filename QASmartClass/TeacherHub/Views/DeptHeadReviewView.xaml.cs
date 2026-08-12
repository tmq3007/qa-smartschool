using QASmartClass.Data;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace QASmartClass.TeacherHub.Views
{
    public partial class DeptHeadReviewView : UserControl
    {
        public DeptHeadReviewView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await LoadSubjectsAsync();
            await LoadDataAsync();

            // V7 P2.3: Enforce role chỉ Tổ Trưởng trở lên mới được duyệt
            if (!QASmartClass.Services.UserSessionService.Instance.IsManager)
            {
                // Ẩn cột hành động cho GV thường thông qua đặt tên XAML trực tiếp
                ColActions.Visibility = Visibility.Collapsed;
            }
        }

        private async Task LoadSubjectsAsync()
        {
            try
            {
                using var db = new AppDbContext();
                var subjects = await db.QuestionBankItems
                                  .Select(q => q.Subject)
                                  .Distinct()
                                  .Where(s => !string.IsNullOrEmpty(s))
                                  .ToListAsync();

                // Lưu lại môn đang được chọn trước khi clear để tránh mất trạng thái lọc của giáo viên
                string selectedSubj = (CboSubject.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Tất cả";

                CboSubject.Items.Clear();
                
                var allItem = new ComboBoxItem { Content = "Tất cả" };
                CboSubject.Items.Add(allItem);
                
                ComboBoxItem selectedItemToRestore = allItem;

                foreach (var subj in subjects)
                {
                    var item = new ComboBoxItem { Content = subj };
                    CboSubject.Items.Add(item);
                    if (subj == selectedSubj)
                    {
                        selectedItemToRestore = item;
                    }
                }
                
                CboSubject.SelectedItem = selectedItemToRestore;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[DeptHeadReview] Load subjects error");
            }
        }

        private string MapStatusToVi(string status) => status switch
        {
            "Approved" => "Đã duyệt",
            "Pending" => "Chờ duyệt",
            "Rejected" => "Từ chối",
            _ => status
        };

        private async Task LoadDataAsync()
        {
            try
            {
                using var db = new AppDbContext();
                var query = db.QuestionBankItems.AsQueryable();

                string filterStatusVi = (CboStatus.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Chờ duyệt";
                if (filterStatusVi != "Tất cả")
                {
                    string filterStatus = filterStatusVi switch
                    {
                        "Chờ duyệt" => "Pending",
                        "Đã duyệt" => "Approved",
                        "Từ chối" => "Rejected",
                        _ => "Pending"
                    };
                    query = query.Where(q => q.ApprovalStatus == filterStatus);
                }

                string filterSubj = (CboSubject.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Tất cả";
                if (filterSubj != "Tất cả")
                {
                    query = query.Where(q => q.Subject == filterSubj);
                }

                var items = await query.OrderByDescending(q => q.CreatedAt).ToListAsync();
                var list = items.Select(q => new
                {
                    q.Id,
                    q.Subject,
                    q.Content,
                    CreatedBy = string.IsNullOrEmpty(q.CreatedBy) ? "(Chưa rõ)" : q.CreatedBy,
                    ApprovalStatus = MapStatusToVi(q.ApprovalStatus),
                    q.ReviewComments,
                    HasComments = string.IsNullOrEmpty(q.ReviewComments) ? Visibility.Collapsed : Visibility.Visible,
                    StatusBg = q.ApprovalStatus == "Approved" ? "#DCFCE7" : (q.ApprovalStatus == "Pending" ? "#FEF9C3" : "#FEE2E2"),
                    StatusFg = q.ApprovalStatus == "Approved" ? "#16A34A" : (q.ApprovalStatus == "Pending" ? "#CA8A04" : "#DC2626")
                }).ToList();

                DgQuestions.ItemsSource = list;
                PnlEmptyState.Visibility = list.Any() ? Visibility.Collapsed : Visibility.Visible;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[DeptHeadReview] Load error");
            }
        }

        private async void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadSubjectsAsync();
            await LoadDataAsync();
        }

        private async void BtnApprove_Click(object sender, RoutedEventArgs e)
        {
            if (!QASmartClass.Services.UserSessionService.Instance.IsManager)
            {
                MessageBox.Show("Thao tác bị chặn: Chỉ có Tổ trưởng hoặc Tổ phó chuyên môn mới có quyền duyệt câu hỏi.", "Lỗi phân quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (sender is Button btn && btn.Tag is int id)
            {
                try
                {
                    using var db = new AppDbContext();
                    var q = await db.QuestionBankItems.FindAsync(id);
                    if (q != null)
                    {
                        q.ApprovalStatus = "Approved";
                        q.ApprovedBy = QASmartClass.Services.UserSessionService.Instance.FullName;
                        q.ReviewComments = string.Empty;
                        
                        await db.SaveChangesAsync();
                        QASmartClass.Services.AuditHelper.Log(db, "Question_Approved", QASmartClass.Services.UserSessionService.Instance.FullName, $"Approved question {id}");
                        
                        // V7 P6.1: Gửi thông báo cho giáo viên tạo câu hỏi
                        if (!string.IsNullOrEmpty(q.CreatedBy))
                        {
                            var notifService = new QASmartClass.Services.NotificationService(db);
                            notifService.PushNotification(q.CreatedBy, new QASmartClass.Services.NotificationMessage
                            {
                                Type = "Question_Approved",
                                Title = "Câu hỏi đã được duyệt",
                                Content = $"Câu hỏi ID {id} của bạn đã được duyệt bởi {q.ApprovedBy}."
                            });
                        }
                        await LoadDataAsync();
                    }
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "[DeptHeadReview] Approve error");
                    MessageBox.Show($"Duyệt câu hỏi thất bại: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BtnReject_Click(object sender, RoutedEventArgs e)
        {
            if (!QASmartClass.Services.UserSessionService.Instance.IsManager)
            {
                MessageBox.Show("Thao tác bị chặn: Chỉ có Tổ trưởng hoặc Tổ phó chuyên môn mới có quyền góp ý câu hỏi.", "Lỗi phân quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (sender is Button btn && btn.Tag is int id)
            {
                try
                {
                    using var db = new AppDbContext();
                    var q = await db.QuestionBankItems.FindAsync(id);
                    if (q != null)
                    {
                        var parentWindow = Window.GetWindow(this);
                        var rejectDlg = new RejectCommentWindow
                        {
                            Owner = parentWindow
                        };
                        if (rejectDlg.ShowDialog() == true)
                        {
                            string comment = rejectDlg.Comment;
                            q.ApprovalStatus = "Rejected";
                            q.ApprovedBy = QASmartClass.Services.UserSessionService.Instance.FullName;
                            q.ReviewComments = $"[Góp ý]: {comment}";
                            
                            await db.SaveChangesAsync();
                            QASmartClass.Services.AuditHelper.Log(db, "Question_Rejected", QASmartClass.Services.UserSessionService.Instance.FullName, $"Rejected question {id}. Reason: {comment}");
                            
                            // V7 P6.1: Gửi thông báo cho giáo viên tạo câu hỏi
                            if (!string.IsNullOrEmpty(q.CreatedBy))
                            {
                                var notifService = new QASmartClass.Services.NotificationService(db);
                                notifService.PushNotification(q.CreatedBy, new QASmartClass.Services.NotificationMessage
                                {
                                    Type = "Question_Rejected",
                                    Title = "Câu hỏi yêu cầu chỉnh sửa",
                                    Content = $"Câu hỏi ID {id} cần chỉnh sửa. Góp ý từ {q.ApprovedBy}: {comment}"
                                });
                            }
                            await LoadDataAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "[DeptHeadReview] Reject error");
                    MessageBox.Show($"Từ chối câu hỏi thất bại: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void DgQuestions_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DgQuestions.SelectedItem == null) return;
            
            try
            {
                // Sử dụng reflection hoặc dynamic để đọc thuộc tính Id của kiểu nặc danh
                dynamic sel = DgQuestions.SelectedItem;
                int id = sel.Id;
                
                using var db = new AppDbContext();
                var q = await db.QuestionBankItems.FindAsync(id);
                if (q != null)
                {
                    var parentWindow = Window.GetWindow(this);
                    var detailDlg = new QuestionDetailWindow(q)
                    {
                        Owner = parentWindow
                    };
                    
                    if (detailDlg.ShowDialog() == true)
                    {
                        string action = detailDlg.ActionTaken;
                        if (action == "Approve")
                        {
                            q.ApprovalStatus = "Approved";
                            q.ApprovedBy = QASmartClass.Services.UserSessionService.Instance.FullName;
                            q.ReviewComments = string.Empty;
                            await db.SaveChangesAsync();
                            
                            QASmartClass.Services.AuditHelper.Log(db, "Question_Approved", q.ApprovedBy, $"Approved question {id} via detail dialog");
                            
                            if (!string.IsNullOrEmpty(q.CreatedBy))
                            {
                                var notifService = new QASmartClass.Services.NotificationService(db);
                                notifService.PushNotification(q.CreatedBy, new QASmartClass.Services.NotificationMessage
                                {
                                    Type = "Question_Approved",
                                    Title = "Câu hỏi đã được duyệt",
                                    Content = $"Câu hỏi ID {id} của bạn đã được duyệt bởi {q.ApprovedBy}."
                                });
                            }
                        }
                        else if (action == "Reject")
                        {
                            string comment = detailDlg.Comment;
                            q.ApprovalStatus = "Rejected";
                            q.ApprovedBy = QASmartClass.Services.UserSessionService.Instance.FullName;
                            q.ReviewComments = $"[Góp ý]: {comment}";
                            await db.SaveChangesAsync();
                            
                            QASmartClass.Services.AuditHelper.Log(db, "Question_Rejected", q.ApprovedBy, $"Rejected question {id} via detail dialog. Reason: {comment}");
                            
                            if (!string.IsNullOrEmpty(q.CreatedBy))
                            {
                                var notifService = new QASmartClass.Services.NotificationService(db);
                                notifService.PushNotification(q.CreatedBy, new QASmartClass.Services.NotificationMessage
                                {
                                    Type = "Question_Rejected",
                                    Title = "Câu hỏi yêu cầu chỉnh sửa",
                                    Content = $"Câu hỏi ID {id} cần chỉnh sửa. Góp ý từ {q.ApprovedBy}: {comment}"
                                });
                            }
                        }
                        await LoadDataAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[DeptHeadReview] DoubleClick error");
            }
        }
    }
}
