using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartLibrary.Desktop.Helpers;
using SmartLibrary.Desktop.Services;

namespace SmartLibrary.Desktop.ViewModels
{
    public partial class ProposalViewModel : ObservableObject
    {
        [ObservableProperty] private string _proposalType = "Thể loại mới";
        [ObservableProperty] private string _proposalName = "";
        [ObservableProperty] private string _proposalReason = "";
        [ObservableProperty] private string _statusMessage = "";
        [ObservableProperty] private bool _isSuccess = false;
        [ObservableProperty] private bool _isBusy;
        
        public string[] ProposalTypes { get; } = { "Thể loại mới", "NXB mới", "Bộ môn mới" };
        public ObservableCollection<ProposalItem> MyProposals { get; } = new();
        
        public ICommand SubmitProposalCommand { get; }

        private readonly ApiService _apiService;

        public ProposalViewModel(ApiService apiService)
        {
            _apiService = apiService;
            SubmitProposalCommand = new AsyncRelayCommand(SubmitAsync);
            _ = LoadMyProposalsFromServerAsync();
        }

        private string GetProposalsFilePath()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SmartLibrary", "data");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, "proposals.json");
        }

        private System.Collections.Generic.List<ProposalItem> LoadProposalsFromFile()
        {
            try
            {
                var path = GetProposalsFilePath();
                if (!File.Exists(path)) return new System.Collections.Generic.List<ProposalItem>();
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<System.Collections.Generic.List<ProposalItem>>(json) 
                       ?? new System.Collections.Generic.List<ProposalItem>();
            }
            catch
            {
                return new System.Collections.Generic.List<ProposalItem>();
            }
        }

        public async Task LoadMyProposalsFromServerAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                await SyncPendingProposalsAsync(_apiService);
                var list = await _apiService.GetAsync<ProposalItem[]>("/Proposals");
                MyProposals.Clear();
                if (list != null)
                {
                    var ssoId = AuthService.CurrentUserSsoId ?? "GV";
                    var filtered = list.Where(p => p.SubmittedBy == ssoId).OrderByDescending(p => p.SubmittedDate);
                    foreach (var item in filtered)
                    {
                        MyProposals.Add(item);
                    }
                }
            }
            catch
            {
                LoadMyProposals(); // Offline fallback
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void LoadMyProposals()
        {
            MyProposals.Clear();
            var allProposals = LoadProposalsFromFile();
            var ssoId = AuthService.CurrentUserSsoId ?? "GV";
            
            var filtered = allProposals.Where(p => p.SubmittedBy == ssoId).OrderByDescending(p => p.SubmittedDate);
            foreach (var item in filtered)
            {
                MyProposals.Add(item);
            }
        }

        private async Task SubmitAsync()
        {
            if (string.IsNullOrWhiteSpace(ProposalName))
            {
                StatusMessage = "Vui lòng nhập tên đề xuất!";
                IsSuccess = false;
                return;
            }

            var proposal = new ProposalItem
            {
                Type = ProposalType,
                Name = InputHelper.NormalizeInput(ProposalName),
                Reason = InputHelper.TrimInput(ProposalReason),
                SubmittedBy = AuthService.CurrentUserSsoId ?? "GV",
                SubmittedDate = DateTime.Now,
                Status = "Chờ duyệt"
            };

            try
            {
                await _apiService.PostAsync("/Proposals", proposal);
                MyProposals.Insert(0, proposal);
                await AuditLogService.WriteLogAsync("Đề xuất danh mục", 
                    $"GV {proposal.SubmittedBy} đề xuất {proposal.Type}: '{proposal.Name}'", true);

                StatusMessage = $"Cảm ơn đóng góp ý nghĩa của bạn! Ý kiến về '{proposal.Name}' sẽ giúp tủ sách nhà trường ngày một phong phú và bổ ích hơn. 📚";
                IsSuccess = true;
                ProposalName = "";
                ProposalReason = "";
            }
            catch
            {
                // Fallback offline:
                var proposals = LoadProposalsFromFile();
                proposals.Add(proposal);
                
                try
                {
                    var json = JsonSerializer.Serialize(proposals);
                    File.WriteAllText(GetProposalsFilePath(), json);
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Lỗi lưu file: {ex.Message}";
                    IsSuccess = false;
                    return;
                }

                MyProposals.Insert(0, proposal);
                await AuditLogService.WriteLogAsync("Đề xuất danh mục", 
                    $"GV {proposal.SubmittedBy} đề xuất {proposal.Type}: '{proposal.Name}' (Ngoại tuyến)", true);

                StatusMessage = $"Cảm ơn đóng góp ý nghĩa của bạn! Ý kiến về '{proposal.Name}' sẽ giúp tủ sách nhà trường ngày một phong phú và bổ ích hơn (Ngoại tuyến). 📚";
                IsSuccess = true;
                ProposalName = "";
                ProposalReason = "";
            }
        }

        public static async Task SyncPendingProposalsAsync(ApiService apiService)
        {
            try
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SmartLibrary", "data");
                var filePath = Path.Combine(dir, "proposals.json");
                
                if (!File.Exists(filePath)) return;

                string json = await File.ReadAllTextAsync(filePath);
                if (string.IsNullOrWhiteSpace(json)) return;

                var pendingProposals = JsonSerializer.Deserialize<System.Collections.Generic.List<ProposalItem>>(json);
                if (pendingProposals == null || pendingProposals.Count == 0) return;

                bool allSynced = true;
                var successfullySynced = new System.Collections.Generic.List<ProposalItem>();
                foreach (var proposal in pendingProposals)
                {
                    try
                    {
                        await apiService.PostAsync("/Proposals", proposal, silent: true);
                        successfullySynced.Add(proposal);
                    }
                    catch
                    {
                        allSynced = false;
                    }
                }

                if (allSynced)
                {
                    File.Delete(filePath);
                }
                else if (successfullySynced.Count > 0)
                {
                    foreach (var item in successfullySynced)
                    {
                        pendingProposals.Remove(item);
                    }
                    string syncJson = JsonSerializer.Serialize(pendingProposals);
                    await File.WriteAllTextAsync(filePath, syncJson);
                }
            }
            catch { }
        }
    }

    public class ProposalItem
    {
        public string Type { get; set; } = "";
        public string Name { get; set; } = "";
        public string Reason { get; set; } = "";
        public string SubmittedBy { get; set; } = "";
        public DateTime SubmittedDate { get; set; }
        public string Status { get; set; } = "Chờ duyệt";
    }
}
