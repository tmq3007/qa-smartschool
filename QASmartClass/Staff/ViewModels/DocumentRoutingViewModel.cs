using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;
using QASmartClass.Staff.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace QASmartClass.Staff.ViewModels
{
    public class DocumentRouteDisplay
    {
        public int Id { get; set; }
        public string DocumentTitle { get; set; } = string.Empty;
        public string Sender { get; set; } = string.Empty;
        public string Receiver { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        public string FriendlyStatus
        {
            get
            {
                if (string.IsNullOrEmpty(Status)) return string.Empty;
                return Status switch
                {
                    "Pending" => "Chờ duyệt",
                    "Approved" => "Đã duyệt",
                    "Rejected" => "Bị từ chối",
                    "Forwarded" => "Đã chuyển tiếp",
                    _ => Status.StartsWith("PendingLevel") 
                        ? $"Chờ duyệt cấp {Status.Replace("PendingLevel", "")}" 
                        : Status
                };
            }
        }
    }

    public partial class DocumentRoutingViewModel : ObservableObject, IDisposable
    {
        private readonly System.Threading.SemaphoreSlim _searchLock = new(1, 1);

        [ObservableProperty] private ObservableCollection<DocumentRouteDisplay> _documents = new();
        [ObservableProperty] private string _docTitle = string.Empty;
        [ObservableProperty] private string _receiver = string.Empty;
        [ObservableProperty] private string _notes = string.Empty;
        [ObservableProperty] private int _selectedMaxLevelOffset = 0;
        [ObservableProperty] private string _approverComment = string.Empty;
        [ObservableProperty] private ObservableCollection<string> _staffList = new();
        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private bool _isLoading;

        public DocumentRoutingViewModel()
        {
        }

        public async Task InitializeAsync()
        {
            IsLoading = true;
            try
            {
                await LoadDocsAsync();
                await LoadStaffListAsync();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadStaffListAsync()
        {
            try
            {
                using var db = StaffDbFactory.Create();
                var teachers = await db.TeacherProfiles.Where(t => t.IsActive).Select(t => t.FullName).ToListAsync();
                var depts = new[] { "Ban Giám Hiệu", "Phòng Kế Toán", "Tổ Văn Thư", "Tổ Tự Nhiên", "Tổ Xã Hội" };
                var list = depts.Concat(teachers).ToList();
                StaffList = new ObservableCollection<string>(list);
                if (string.IsNullOrEmpty(Receiver))
                {
                    Receiver = StaffList.FirstOrDefault() ?? "Ban Giám Hiệu";
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[DocumentRouting] Load staff failed");
            }
        }

        [RelayCommand]
        private async Task LoadDocsAsync()
        {
            await _searchLock.WaitAsync();
            try
            {
                using var db = StaffDbFactory.Create();
                var query = db.DocumentRoutes.AsQueryable();
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var kw = SearchText.ToLower().Trim();
                    query = query.Where(d => d.DocumentTitle.ToLower().Contains(kw) || d.Sender.ToLower().Contains(kw));
                }
                
                var docs = await query.OrderByDescending(d => d.SentAt).Take(50).ToListAsync();
                var list = docs.Select(d => new DocumentRouteDisplay
                {
                    Id = d.Id,
                    DocumentTitle = d.DocumentTitle,
                    Sender = d.Sender,
                    Receiver = d.Receiver,
                    SentAt = d.SentAt,
                    Status = d.Status,
                    Notes = d.Notes
                }).ToList();
                
                Documents = new ObservableCollection<DocumentRouteDisplay>(list);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[DocumentRouting] Load failed");
            }
            finally
            {
                _searchLock.Release();
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            _ = LoadDocsAsync();
        }

        private bool CanUserProcessDoc(string receiver)
        {
            if (string.IsNullOrEmpty(receiver)) return false;
            string currentActor = StaffSession.DisplayName;
            string currentRole = StaffSession.Role;

            // Admin và Hiệu Trưởng có toàn quyền duyệt mọi công văn
            if (currentRole is "Admin" or "HieuTruong") return true;

            // Ban Giám Hiệu cho phép Hiệu Phó phê duyệt
            if (receiver == "Ban Giám Hiệu")
            {
                return currentRole is "HieuPho";
            }
            
            // Phòng Kế Toán cho phép Kế toán phê duyệt
            if (receiver == "Phòng Kế Toán")
            {
                return currentRole is "Ketoan";
            }
            
            // Tổ Văn Thư cho phép nhân viên Văn thư/Văn phòng phê duyệt
            if (receiver == "Tổ Văn Thư")
            {
                return currentRole is "VanThu" or "VT" or "Office";
            }

            // Mặc định: người nhận đích danh hoặc Hiệu Phó có quyền duyệt
            return receiver == currentActor || currentRole is "HieuPho";
        }

        [RelayCommand]
        private async Task SendDocAsync()
        {
            if (string.IsNullOrWhiteSpace(DocTitle) || string.IsNullOrWhiteSpace(Receiver))
            {
                await AppServices.UIService.ShowInfoAsync("Vui lòng nhập Tên công văn và Người nhận.", "Lỗi");
                return;
            }

            try
            {
                using var db = StaffDbFactory.Create();
                using (var transaction = await db.Database.BeginTransactionAsync())
                {
                    var doc = new DocumentRoute
                    {
                        DocumentTitle = DocTitle.Trim(),
                        Sender = StaffSession.DisplayName,
                        Receiver = Receiver.Trim(),
                        Notes = Notes.Trim(),
                        Status = "Pending",
                        SentAt = DateTime.Now,
                        MaxApprovalLevel = SelectedMaxLevelOffset + 1,
                        CurrentApprovalLevel = 0
                    };

                    db.DocumentRoutes.Add(doc);
                    await db.SaveChangesAsync();

                    // Ghi nhật ký kiểm toán gửi công văn đi
                    string actorCode = StaffSession.CurrentUser?.TeacherCode ?? "Admin";
                    QASmartClass.Services.AuditHelper.Log(db, "Document_Route_Created", actorCode, $"Khởi tạo luồng văn bản '{doc.DocumentTitle}' chuyển cho '{doc.Receiver}'");
                    await db.SaveChangesAsync();

                    await transaction.CommitAsync();

                    await AppServices.UIService.ShowInfoAsync("Đã gửi công văn thành công!", "Thành công");
                }
                
                DocTitle = string.Empty;
                Notes = string.Empty;
                SelectedMaxLevelOffset = 0;
                
                await LoadDocsAsync();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[DocumentRouting] Send failed");
            }
        }

        [RelayCommand]
        private async Task ApproveAsync(int docId)
        {
            using var db = StaffDbFactory.Create();
            var docRoute = await db.DocumentRoutes.FindAsync(docId);
            if (docRoute == null) return;

            if (!CanUserProcessDoc(docRoute.Receiver))
            {
                await AppServices.UIService.ShowInfoAsync("Bạn không có quyền xử lý công văn này.", "Quyền truy cập");
                return;
            }

            string approver = QASmartClass.Staff.Services.StaffSession.CurrentUser?.FullName ?? "BGH";
            string comment = string.IsNullOrWhiteSpace(ApproverComment) ? "Đồng ý trình ký." : ApproverComment.Trim();
            
            var workflow = new WorkflowEngineService(db);
            await workflow.ProcessDocumentRouteAsync(docId, "Approve", approver, comment);

            // Audit Log
            string actorCode = StaffSession.CurrentUser?.TeacherCode ?? "Admin";
            QASmartClass.Services.AuditHelper.Log(db, "Document_Approved", actorCode, $"Phê duyệt công văn '{docRoute.DocumentTitle}' lên cấp {docRoute.CurrentApprovalLevel}");
            await db.SaveChangesAsync();

            ApproverComment = string.Empty;
            await LoadDocsAsync();
        }

        [RelayCommand]
        private async Task RejectAsync(int docId)
        {
            using var db = StaffDbFactory.Create();
            var docRoute = await db.DocumentRoutes.FindAsync(docId);
            if (docRoute == null) return;

            if (!CanUserProcessDoc(docRoute.Receiver))
            {
                await AppServices.UIService.ShowInfoAsync("Bạn không có quyền xử lý công văn này.", "Quyền truy cập");
                return;
            }

            if (string.IsNullOrWhiteSpace(ApproverComment) || ApproverComment.Trim().Length < 5)
            {
                await AppServices.UIService.ShowInfoAsync("Vui lòng nhập ý kiến phản hồi chi tiết (tối thiểu 5 ký tự) trước khi bấm Từ chối công văn.", "Yêu cầu phản hồi");
                return;
            }

            string approver = QASmartClass.Staff.Services.StaffSession.CurrentUser?.FullName ?? "BGH";
            string comment = ApproverComment.Trim();
            
            var workflow = new WorkflowEngineService(db);
            await workflow.ProcessDocumentRouteAsync(docId, "Reject", approver, comment);

            // Audit Log
            string actorCode = StaffSession.CurrentUser?.TeacherCode ?? "Admin";
            QASmartClass.Services.AuditHelper.Log(db, "Document_Rejected", actorCode, $"Từ chối công văn '{docRoute.DocumentTitle}' với lý do: {comment}");
            await db.SaveChangesAsync();

            ApproverComment = string.Empty;
            await LoadDocsAsync();
        }

        [RelayCommand]
        private async Task ForwardAsync(int docId)
        {
            using var db = StaffDbFactory.Create();
            var docRoute = await db.DocumentRoutes.FindAsync(docId);
            if (docRoute == null) return;

            if (!CanUserProcessDoc(docRoute.Receiver))
            {
                await AppServices.UIService.ShowInfoAsync("Bạn không có quyền xử lý công văn này.", "Quyền truy cập");
                return;
            }

            if (string.IsNullOrWhiteSpace(ApproverComment) || ApproverComment.Trim().Length < 5)
            {
                await AppServices.UIService.ShowInfoAsync("Vui lòng nhập ý kiến phản hồi chi tiết (tối thiểu 5 ký tự) trước khi bấm Chuyển tiếp công văn.", "Yêu cầu phản hồi");
                return;
            }

            string approver = QASmartClass.Staff.Services.StaffSession.CurrentUser?.FullName ?? "BGH";
            string comment = ApproverComment.Trim();
            
            var workflow = new WorkflowEngineService(db);
            await workflow.ProcessDocumentRouteAsync(docId, "Forward", approver, comment);

            // Audit Log
            string actorCode = StaffSession.CurrentUser?.TeacherCode ?? "Admin";
            QASmartClass.Services.AuditHelper.Log(db, "Document_Forwarded", actorCode, $"Chuyển tiếp công văn '{docRoute.DocumentTitle}' cho: {docRoute.Receiver}");
            await db.SaveChangesAsync();

            ApproverComment = string.Empty;
            await LoadDocsAsync();
        }

        [RelayCommand]
        private async Task ResetFormAsync()
        {
            if (!string.IsNullOrWhiteSpace(DocTitle) || !string.IsNullOrWhiteSpace(Notes))
            {
                bool confirm = await AppServices.UIService.ShowConfirmAsync("Bạn có chắc chắn muốn hủy bỏ và nhập lại? Dữ liệu đang nhập sẽ bị xóa.", "Xác nhận", true);
                if (!confirm) return;
            }
            DocTitle = string.Empty;
            Notes = string.Empty;
            SelectedMaxLevelOffset = 0;
        }

        public void Dispose()
        {
            _searchLock.Dispose();
        }
    }
}

