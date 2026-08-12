using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartLibrary.Desktop.Services;

namespace SmartLibrary.Desktop.ViewModels
{
    public partial class GreenMetricsViewModel : ObservableObject, IActiveAwareViewModel
    {
        private readonly ApiService _apiService;

        [ObservableProperty]
        private int _ebookReadsCount = 0;

        [ObservableProperty]
        private int _ebookPagesRead = 0;

        [ObservableProperty]
        private int _donatedBooksCount = 0;

        [ObservableProperty]
        private double _treesSaved = 0;

        [ObservableProperty]
        private double _co2SavedKg = 0;

        [ObservableProperty]
        private int _totalPagesRead = 0;

        [ObservableProperty]
        private double _esgTreesSaved = 0;

        [ObservableProperty]
        private double _paperSavedGrams = 0;

        [ObservableProperty]
        private double _co2ReducedGrams = 0;

        [ObservableProperty]
        private string _statusMessage = "Đang tải dữ liệu...";

        [ObservableProperty]
        private bool _isLoading = false;

        public ObservableCollection<SchoolEcoRankDto> SchoolLeaderboard { get; } = new();
        public ObservableCollection<ClassEcoRankDto> ClassLeaderboard { get; } = new();

        public ICommand LoadMetricsCommand { get; }

        public GreenMetricsViewModel(ApiService apiService)
        {
            _apiService = apiService;
            LoadMetricsCommand = new AsyncRelayCommand(LoadMetricsAsync);
        }

        public void Activate()
        {
            _ = LoadMetricsAsync();
        }

        public async Task LoadMetricsAsync()
        {
            IsLoading = true;
            StatusMessage = "Đang kết nối đến máy chủ...";
            SchoolLeaderboard.Clear();

            try
            {
                var data = await _apiService.GetAsync<GreenMetricsResponseDto>("/Reports/green-metrics");
                if (data != null)
                {
                    EbookReadsCount = data.EbookReadsCount;
                    EbookPagesRead = data.EbookPagesRead;
                    DonatedBooksCount = data.DonatedBooksCount;
                    TreesSaved = data.TreesSaved;
                    Co2SavedKg = data.Co2SavedKg;

                    if (data.SchoolLeaderboard != null)
                    {
                        foreach (var s in data.SchoolLeaderboard)
                        {
                            SchoolLeaderboard.Add(new SchoolEcoRankDto
                            {
                                Rank = s.Rank,
                                SchoolName = s.SchoolName,
                                Co2Saved = s.Co2Saved,
                                TreesSaved = s.TreesSaved,
                                Score = s.Score
                            });
                        }
                    }

                    // Tải dữ liệu thi đua xanh lớp học (ESG)
                    try
                    {
                        var esgData = await _apiService.GetAsync<EsgSummaryResponseDto>("/Reports/GreenMetrics/esg-summary");
                        if (esgData != null)
                        {
                            TotalPagesRead = esgData.TotalPagesRead;
                            EsgTreesSaved = esgData.TreesSaved;
                            PaperSavedGrams = esgData.PaperSavedGrams;
                            Co2ReducedGrams = esgData.Co2ReducedGrams;

                            ClassLeaderboard.Clear();
                            if (esgData.TopClassLeague != null)
                            {
                                for (int i = 0; i < esgData.TopClassLeague.Count; i++)
                                {
                                    var c = esgData.TopClassLeague[i];
                                    ClassLeaderboard.Add(new ClassEcoRankDto
                                    {
                                        Rank = i + 1,
                                        ClassId = c.ClassId,
                                        Pages = c.Pages,
                                        TreesSaved = c.TreesSaved
                                    });
                                }
                            }
                        }
                    }
                    catch
                    {
                        LoadEsgFallbackData();
                    }

                    StatusMessage = $"Cập nhật lúc: {DateTime.Now:HH:mm:ss}";
                }
                else
                {
                    LoadFallbackData();
                }
            }
            catch (Exception)
            {
                // Fallback offline
                LoadFallbackData();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void LoadFallbackData()
        {
            EbookReadsCount = 1485;
            EbookPagesRead = 311850;
            DonatedBooksCount = 345;
            TreesSaved = 1559.3;
            Co2SavedKg = 6754.5;

            SchoolLeaderboard.Clear();
            SchoolLeaderboard.Add(new SchoolEcoRankDto { Rank = 1, SchoolName = "THPT Chuyên Nguyễn Huệ", Co2Saved = 7450.5, TreesSaved = 1790, Score = 95 });
            SchoolLeaderboard.Add(new SchoolEcoRankDto { Rank = 2, SchoolName = "THPT Chu Văn An", Co2Saved = 7120.2, TreesSaved = 1630, Score = 88 });
            SchoolLeaderboard.Add(new SchoolEcoRankDto { Rank = 3, SchoolName = "Tiểu học và THCS QA Smart School (Trường ta)", Co2Saved = Co2SavedKg, TreesSaved = (int)TreesSaved, Score = (int)(Co2SavedKg / 15) });
            SchoolLeaderboard.Add(new SchoolEcoRankDto { Rank = 4, SchoolName = "THPT Yên Hòa", Co2Saved = 5890.4, TreesSaved = 1310, Score = 65 });
            SchoolLeaderboard.Add(new SchoolEcoRankDto { Rank = 5, SchoolName = "THPT Phan Đình Phùng", Co2Saved = 4720.0, TreesSaved = 1050, Score = 52 });

            // Sort ranking
            var sorted = SchoolLeaderboard.OrderByDescending(x => x.Co2Saved).ToList();
            SchoolLeaderboard.Clear();
            for (int i = 0; i < sorted.Count; i++)
            {
                sorted[i].Rank = i + 1;
                SchoolLeaderboard.Add(sorted[i]);
            }

            StatusMessage = $"[Ngoại tuyến] Cập nhật lúc: {DateTime.Now:HH:mm:ss}";
            LoadEsgFallbackData();
        }

        private void LoadEsgFallbackData()
        {
            TotalPagesRead = 100000;
            EsgTreesSaved = 10.0;
            PaperSavedGrams = 5000.0;
            Co2ReducedGrams = 12000.0;

            ClassLeaderboard.Clear();
            ClassLeaderboard.Add(new ClassEcoRankDto { Rank = 1, ClassId = "10A1", Pages = 45000, TreesSaved = 4.5 });
            ClassLeaderboard.Add(new ClassEcoRankDto { Rank = 2, ClassId = "11B2", Pages = 35000, TreesSaved = 3.5 });
            ClassLeaderboard.Add(new ClassEcoRankDto { Rank = 3, ClassId = "10A2", Pages = 15000, TreesSaved = 1.5 });
            ClassLeaderboard.Add(new ClassEcoRankDto { Rank = 4, ClassId = "11A1", Pages = 5000, TreesSaved = 0.5 });
        }
    }

    public class SchoolEcoRankDto
    {
        public int Rank { get; set; }
        public string SchoolName { get; set; } = string.Empty;
        public double Co2Saved { get; set; }
        public int TreesSaved { get; set; }
        public int Score { get; set; }
        public bool IsMySchool => SchoolName.Contains("Kim Liên") || SchoolName.Contains("QA Smart School");
    }

    public class GreenMetricsResponseDto
    {
        public int EbookReadsCount { get; set; }
        public int EbookPagesRead { get; set; }
        public int DonatedBooksCount { get; set; }
        public double TreesSaved { get; set; }
        public double Co2SavedKg { get; set; }
        public System.Collections.Generic.List<SchoolEcoRankDto>? SchoolLeaderboard { get; set; }
    }

    public class ClassEcoRankDto
    {
        public int Rank { get; set; }
        public string ClassId { get; set; } = string.Empty;
        public int Pages { get; set; }
        public double TreesSaved { get; set; }
    }

    public class EsgSummaryResponseDto
    {
        public int TotalPagesRead { get; set; }
        public double TreesSaved { get; set; }
        public double PaperSavedGrams { get; set; }
        public double Co2ReducedGrams { get; set; }
        public System.Collections.Generic.List<ClassEcoRankDto>? TopClassLeague { get; set; }
    }
}
