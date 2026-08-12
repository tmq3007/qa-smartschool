using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QASmartClass.Data;
using QASmartClass.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Staff.Services;

namespace QASmartClass.Staff.ViewModels
{
    public partial class OfficialDocumentDisplay
    {
        public int Id { get; set; }
        public string DocumentNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime IssuedDate { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Recipient { get; set; } = string.Empty;

        // SLA fields
        public DateTime ProcessingDeadline { get; set; }
        public bool IsEscalated { get; set; }
        public bool IsSlaOverdue => Status == "Pending" && (IsEscalated || ProcessingDeadline < DateTime.Today);
        public string SlaStatusText => IsSlaOverdue ? "QUÁ HẠN / LEO THANG!" : $"Hạn: {ProcessingDeadline:dd/MM/yyyy}";
        public string SlaStatusColor => IsSlaOverdue ? "#DC2626" : "#4B5563";

        public string FriendlyType => Type switch
        {
            "Incoming" => "Công văn đến",
            "Outgoing" => "Công văn đi",
            "Internal" => "Nội bộ",
            _ => Type
        };

        public string FriendlyStatus => Status switch
        {
            "Pending" => "Chờ xử lý",
            "Approved" => "Đã duyệt",
            _ => Status
        };
    }

    public partial class DocumentManagerViewModel : ObservableObject
    {
        [ObservableProperty] private ObservableCollection<OfficialDocumentDisplay> _documents = new();
        [ObservableProperty] private string _docNumber = string.Empty;
        [ObservableProperty] private string _title = string.Empty;
        [ObservableProperty] private string _selectedType = "Incoming";
        [ObservableProperty] private string _recipient = string.Empty;
        [ObservableProperty] private string _searchText = string.Empty;

        // DatePicker input property
        [ObservableProperty] private DateTime? _newProcessingDeadline;

        public DocumentManagerViewModel()
        {
        }

        public async Task InitializeAsync()
        {
            await LoadDocumentsAsync();
        }

        [RelayCommand]
        private async Task LoadDocumentsAsync()
        {
            try
            {
                using var db = StaffDbFactory.Create();
                var data = await db.OfficialDocuments.AsNoTracking().ToListAsync();
                var list = data.Select(d => new OfficialDocumentDisplay
                {
                    Id = d.Id,
                    DocumentNumber = d.DocumentNumber,
                    Title = d.Title,
                    IssuedDate = d.IssuedDate,
                    Type = d.Type,
                    Status = d.Status,
                    Recipient = d.Recipient,
                    ProcessingDeadline = d.ProcessingDeadline,
                    IsEscalated = d.IsEscalated
                }).ToList();
                Documents = new ObservableCollection<OfficialDocumentDisplay>(list);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[DocumentManager] Load failed");
            }
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(DocNumber))
            {
                await AppServices.UIService.ShowInfoAsync("Vui lòng nhập đầy đủ Số hiệu và Trích yếu.", "Lỗi");
                return;
            }

            try
            {
                using var db = StaffDbFactory.Create();
                using (var transaction = await db.Database.BeginTransactionAsync())
                {
                    string cleanDocNumber = DocNumber.Trim();
                    if (cleanDocNumber.Length > 50)
                    {
                        await AppServices.UIService.ShowInfoAsync("Số hiệu văn bản không được vượt quá 50 ký tự.", "Lỗi nhập liệu");
                        return;
                    }

                    bool exists = await db.OfficialDocuments.AnyAsync(d => d.DocumentNumber.ToLower() == cleanDocNumber.ToLower());
                    if (exists)
                    {
                        await AppServices.UIService.ShowInfoAsync($"Số hiệu văn bản '{cleanDocNumber}' đã tồn tại trong hệ thống. Vui lòng nhập số hiệu khác.", "Lỗi trùng số hiệu");
                        return;
                    }

                    // Get SLA Default Days from settings
                    var setting = await db.SystemSettings.FirstOrDefaultAsync(s => s.Id == "IT_Document_DefaultSLADays");
                    int defaultDays = setting != null && int.TryParse(setting.Value, out int dVal) ? dVal : 3;

                    var deadline = NewProcessingDeadline ?? DateTime.Today.AddDays(defaultDays);

                    var newDoc = new OfficialDocument
                    {
                        DocumentNumber = cleanDocNumber,
                        Title = Title.Trim(),
                        Type = SelectedType,
                        Recipient = Recipient.Trim(),
                        Status = "Pending",
                        IssuedDate = DateTime.Now,
                        ProcessingDeadline = deadline,
                        IsEscalated = false
                    };

                    db.OfficialDocuments.Add(newDoc);
                    await db.SaveChangesAsync();

                    string actor = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "VanThu";
                    QASmartClass.Services.AuditHelper.Log(db, "Upload_Document", actor, $"Uploaded official document: {newDoc.DocumentNumber} - {newDoc.Title}");
                    await db.SaveChangesAsync();

                    await transaction.CommitAsync();

                    await AppServices.UIService.ShowInfoAsync("Đã lưu văn bản thành công!", "Thành công");
                }
                
                // Reset form
                DocNumber = string.Empty;
                Title = string.Empty;
                Recipient = string.Empty;
                NewProcessingDeadline = null;
                
                await LoadDocumentsAsync();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[DocumentManager] Save failed");
                await AppServices.UIService.ShowInfoAsync("Lỗi khi lưu văn bản.", "Lỗi");
            }
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            try
            {
                using var db = StaffDbFactory.Create();
                var query = db.OfficialDocuments.AsNoTracking();
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    query = query.Where(d => d.DocumentNumber.Contains(SearchText) || d.Title.Contains(SearchText));
                }
                var data = await query.ToListAsync();
                var list = data.Select(d => new OfficialDocumentDisplay
                {
                    Id = d.Id,
                    DocumentNumber = d.DocumentNumber,
                    Title = d.Title,
                    IssuedDate = d.IssuedDate,
                    Type = d.Type,
                    Status = d.Status,
                    Recipient = d.Recipient,
                    ProcessingDeadline = d.ProcessingDeadline,
                    IsEscalated = d.IsEscalated
                }).ToList();
                Documents = new ObservableCollection<OfficialDocumentDisplay>(list);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[DocumentManager] Search failed");
            }
        }

        [RelayCommand]
        private async Task EscalateAsync(int id)
        {
            try
            {
                using var db = StaffDbFactory.Create();
                var doc = await db.OfficialDocuments.FindAsync(id);
                if (doc != null)
                {
                    doc.IsEscalated = true;
                    await db.SaveChangesAsync();

                    // Send push notifications to Ban Giam Hieu
                    var leaders = await db.TeacherProfiles.Where(t => t.IsActive && (t.Role.Contains("HieuTruong") || t.Role.Contains("HieuPho"))).ToListAsync();
                    var mobileApi = new MobileApiService(db);
                    foreach (var leader in leaders)
                    {
                        await mobileApi.SendPushNotificationAsync(
                            leader.Id,
                            "Teacher",
                            "Yêu cầu xử lý công văn khẩn (SLA Escalated)",
                            $"Văn bản khẩn '{doc.Title}' (Số hiệu: {doc.DocumentNumber}) đã bị quá hạn hoặc được yêu cầu xử lý khẩn cấp lên Ban Giám Hiệu.",
                            "Emergency"
                        );
                    }

                    string actor = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode ?? "Staff";
                    QASmartClass.Services.AuditHelper.Log(db, "Escalate_Document", actor, $"Escalated document {doc.DocumentNumber} - {doc.Title}");
                    await db.SaveChangesAsync();

                    await LoadDocumentsAsync();
                    await AppServices.UIService.ShowInfoAsync($"✅ Đã gửi yêu cầu xử lý khẩn cấp văn bản {doc.DocumentNumber} tới Ban Giám Hiệu.", "Thành công");
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[DocumentManager] Escalate failed");
            }
        }

        [RelayCommand]
        private async Task ResetFormAsync()
        {
            if (!string.IsNullOrWhiteSpace(DocNumber) || !string.IsNullOrWhiteSpace(Title) || !string.IsNullOrWhiteSpace(Recipient))
            {
                bool confirm = await AppServices.UIService.ShowConfirmAsync("Bạn có chắc chắn muốn hủy bỏ và nhập lại? Dữ liệu đang nhập sẽ bị xóa.", "Xác nhận", true);
                if (!confirm) return;
            }
            
            DocNumber = string.Empty;
            Title = string.Empty;
            Recipient = string.Empty;
            NewProcessingDeadline = null;
        }
    }
}
