using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using QASmartClass.LearningTools.Helpers;
using OxyPlot;
using OxyPlot.Series;

namespace QASmartClass.LearningTools.ViewModels
{
    /// <summary>
    /// Thực thể lưu trữ cho một nhiệm vụ trong Ma trận Eisenhower
    /// </summary>
    public partial class TaskItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _content = "";

        [ObservableProperty]
        private int _quadrant = 1; // 1: Làm ngay, 2: Lên lịch, 3: Ủy thác, 4: Loại bỏ

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsOverdue))]
        private bool _isCompleted = false;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsOverdue))]
        private DateTime? _dueDate;

        public bool IsOverdue => !IsCompleted && DueDate.HasValue && DueDate.Value.Date < DateTime.Today;

        // Báo hiệu khi có bất kỳ thuộc tính nào thay đổi để kích hoạt lưu DB
        public event EventHandler? OnItemChanged;

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.PropertyName == nameof(Content) || 
                e.PropertyName == nameof(Quadrant) || 
                e.PropertyName == nameof(IsCompleted) ||
                e.PropertyName == nameof(DueDate))
            {
                OnItemChanged?.Invoke(this, EventArgs.Empty);
            }

            // Game hóa: Phát âm thanh thành công khi việc được check hoàn thành
            if (e.PropertyName == nameof(IsCompleted) && IsCompleted)
            {
                try
                {
                    SoundHelper.Play(true);
                }
                catch {}
            }
        }
    }

    /// <summary>
    /// ViewModel chính của công cụ Ma trận Eisenhower
    /// </summary>
    public partial class EisenhowerViewModel : ObservableObject
    {
        private const string ToolId = "eisenhower";
        private bool _isSuppressingSave = false;
        private int _prevQ1Count = 0;

        public event EventHandler? OnConfettiTriggered;

        public ObservableCollection<TaskItemViewModel> Tasks { get; } = new();

        // Danh sách công việc theo từng ô để Binding lên ItemsControl trong View
        public ObservableCollection<TaskItemViewModel> Q1Tasks { get; } = new();
        public ObservableCollection<TaskItemViewModel> Q2Tasks { get; } = new();
        public ObservableCollection<TaskItemViewModel> Q3Tasks { get; } = new();
        public ObservableCollection<TaskItemViewModel> Q4Tasks { get; } = new();

        [ObservableProperty]
        private string _quickAddText = "";

        [ObservableProperty]
        private bool _isInsightVisible = false;

        [ObservableProperty]
        private ObservableCollection<string> _adviceList = new();

        [ObservableProperty]
        private PlotModel? _piePlotModel;

        public EisenhowerViewModel()
        {
            Tasks.CollectionChanged += Tasks_CollectionChanged;
            LoadState();
        }

        // ═══════════════════════════════════════════════════════════
        //  DYNAMIC COUNTS (Đếm số việc chưa hoàn thành)
        // ═══════════════════════════════════════════════════════════

        public int Q1Count => Tasks.Count(t => t.Quadrant == 1 && !t.IsCompleted);
        public int Q2Count => Tasks.Count(t => t.Quadrant == 2 && !t.IsCompleted);
        public int Q3Count => Tasks.Count(t => t.Quadrant == 3 && !t.IsCompleted);
        public int Q4Count => Tasks.Count(t => t.Quadrant == 4 && !t.IsCompleted);

        // ═══════════════════════════════════════════════════════════
        //  COMMANDS
        // ═══════════════════════════════════════════════════════════

        [RelayCommand]
        private void AddEmptyTask(int quadrant)
        {
            var newTask = new TaskItemViewModel { Quadrant = quadrant, Content = "", IsCompleted = false };
            Tasks.Add(newTask);
        }

        [RelayCommand]
        private void QuickAdd(int quadrant)
        {
            string text = QuickAddText.Trim();
            if (string.IsNullOrWhiteSpace(text)) return;

            var newTask = new TaskItemViewModel { Quadrant = quadrant, Content = text, IsCompleted = false };
            Tasks.Add(newTask);
            QuickAddText = "";
        }

        [RelayCommand]
        private void DeleteTask(TaskItemViewModel task)
        {
            if (task != null)
            {
                Tasks.Remove(task);
            }
        }

        [RelayCommand]
        private void ClearAll()
        {
            Tasks.Clear();
        }

        public void ApplyTemplateData(string[] q1, string[] q2, string[] q3, string[] q4)
        {
            _isSuppressingSave = true;
            try
            {
                Tasks.Clear();
                foreach (var t in q1) Tasks.Add(new TaskItemViewModel { Quadrant = 1, Content = t });
                foreach (var t in q2) Tasks.Add(new TaskItemViewModel { Quadrant = 2, Content = t });
                foreach (var t in q3) Tasks.Add(new TaskItemViewModel { Quadrant = 3, Content = t });
                foreach (var t in q4) Tasks.Add(new TaskItemViewModel { Quadrant = 4, Content = t });
            }
            finally
            {
                _isSuppressingSave = false;
                SaveState();
                UpdateCountsAndInsight();
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  STATE SYNC (Lọc danh sách các ô & cập nhật đếm)
        // ═══════════════════════════════════════════════════════════

        private void Tasks_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (TaskItemViewModel item in e.OldItems)
                {
                    item.OnItemChanged -= Item_OnItemChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (TaskItemViewModel item in e.NewItems)
                {
                    item.OnItemChanged += Item_OnItemChanged;
                }
            }

            SyncQuadrantLists();
            UpdateCountsAndInsight();
            SaveState();
        }

        private void Item_OnItemChanged(object? sender, EventArgs e)
        {
            SyncQuadrantLists();
            UpdateCountsAndInsight();
            SaveState();
        }

        private void SyncQuadrantLists()
        {
            // Đồng bộ danh sách của từng ô để XAML ItemsControl hiển thị
            SyncList(Q1Tasks, 1);
            SyncList(Q2Tasks, 2);
            SyncList(Q3Tasks, 3);
            SyncList(Q4Tasks, 4);
        }

        private void SyncList(ObservableCollection<TaskItemViewModel> targetList, int quadrant)
        {
            var sourceItems = Tasks.Where(t => t.Quadrant == quadrant).ToList();
            
            // Xóa các mục không còn trong danh sách gốc
            for (int i = targetList.Count - 1; i >= 0; i--)
            {
                if (!sourceItems.Contains(targetList[i]))
                {
                    targetList.RemoveAt(i);
                }
            }

            // Thêm các mục mới
            foreach (var item in sourceItems)
            {
                if (!targetList.Contains(item))
                {
                    targetList.Add(item);
                }
            }
        }

        private void UpdateCountsAndInsight()
        {
            int oldQ1 = _prevQ1Count;
            int newQ1 = Q1Count;
            _prevQ1Count = newQ1;

            OnPropertyChanged(nameof(Q1Count));
            OnPropertyChanged(nameof(Q2Count));
            OnPropertyChanged(nameof(Q3Count));
            OnPropertyChanged(nameof(Q4Count));

            // Confetti Trigger: dọn sạch Ô 1 (Làm ngay) từ > 0 việc về 0 việc
            if (oldQ1 > 0 && newQ1 == 0 && Tasks.Any(t => t.Quadrant == 1 && t.IsCompleted))
            {
                OnConfettiTriggered?.Invoke(this, EventArgs.Empty);
            }

            UpdateInsight();
            UpdatePlotModel();
        }

        // Vẽ biểu đồ tròn OxyPlot
        private void UpdatePlotModel()
        {
            var model = new PlotModel { Title = "Phân Bổ Nhiệm Vụ (Eisenhower)" };
            var series = new PieSeries
            {
                StrokeThickness = 1.0,
                InsideLabelPosition = 0.8,
                AngleSpan = 360,
                StartAngle = 0
            };

            if (Q1Count > 0) series.Slices.Add(new PieSlice("Làm ngay", Q1Count) { Fill = OxyColor.Parse("#FFCDD2") });
            if (Q2Count > 0) series.Slices.Add(new PieSlice("Lên lịch", Q2Count) { Fill = OxyColor.Parse("#C8E6C9") });
            if (Q3Count > 0) series.Slices.Add(new PieSlice("Ủy thác", Q3Count) { Fill = OxyColor.Parse("#FFE0B2") });
            if (Q4Count > 0) series.Slices.Add(new PieSlice("Loại bỏ", Q4Count) { Fill = OxyColor.Parse("#E0E0E0") });

            model.Series.Add(series);
            PiePlotModel = model;
        }

        // ═══════════════════════════════════════════════════════════
        //  AI INSIGHT LOGIC (Chỉ tính việc chưa hoàn thành)
        // ═══════════════════════════════════════════════════════════

        private void UpdateInsight()
        {
            int q1 = Q1Count;
            int q2 = Q2Count;
            int q3 = Q3Count;
            int q4 = Q4Count;
            int total = q1 + q2 + q3 + q4;

            if (total == 0)
            {
                IsInsightVisible = false;
                AdviceList.Clear();
                return;
            }

            IsInsightVisible = true;
            var newAdvice = new ObservableCollection<string>();

            if (q1 > q2 && q1 >= 3)
                newAdvice.Add("⚠️ Quá nhiều việc \"Làm ngay\" — bạn đang ở chế độ chữa cháy. Hãy dành thời gian cho Ô 2 để phòng ngừa.");

            if (q2 > q1)
                newAdvice.Add("✅ Tuyệt vời! Bạn đang tập trung vào Ô 2 — đây là dấu hiệu của người quản lý thời gian hiệu quả.");

            if (q4 >= 3)
                newAdvice.Add("💡 Có nhiều việc ở Ô 4 — hãy mạnh dạn loại bỏ hoặc giảm thời gian cho chúng.");

            if (q3 >= 2)
                newAdvice.Add("🤝 Hãy cân nhắc ủy thác các việc ở Ô 3 cho người khác để tập trung vào việc quan trọng.");

            if (newAdvice.Count == 0)
                newAdvice.Add("👍 Phân bổ hợp lý! Tiếp tục duy trì cách quản lý này.");

            AdviceList = newAdvice;
        }

        // ═══════════════════════════════════════════════════════════
        //  PERSISTENCE (Đọc/Ghi SQLite)
        // ═══════════════════════════════════════════════════════════

        private void SaveState()
        {
            if (_isSuppressingSave) return;
            try
            {
                var dtos = Tasks.Select(t => new TaskDto
                {
                    C = t.Content,
                    Q = t.Quadrant,
                    F = t.IsCompleted,
                    D = t.DueDate?.ToString("o")
                }).ToList();

                string json = JsonConvert.SerializeObject(dtos);
                DbManager.SaveWorkplaceState(ToolId, json);
            }
            catch
            {
                // Tránh đổ vỡ giao diện nếu lưu DB lỗi
            }
        }

        private void LoadState()
        {
            _isSuppressingSave = true;
            try
            {
                string json = DbManager.LoadWorkplaceState(ToolId);
                Tasks.Clear();

                if (!string.IsNullOrEmpty(json))
                {
                    var dtos = JsonConvert.DeserializeObject<System.Collections.Generic.List<TaskDto>>(json);
                    if (dtos != null)
                    {
                        foreach (var dto in dtos)
                        {
                            DateTime? due = null;
                            if (DateTime.TryParse(dto.D, out DateTime dt)) due = dt;

                            Tasks.Add(new TaskItemViewModel
                            {
                                Content = dto.C,
                                Quadrant = dto.Q,
                                IsCompleted = dto.F,
                                DueDate = due
                            });
                        }
                    }
                }
            }
            catch
            {
                // Tránh đổ vỡ giao diện nếu đọc DB lỗi
            }
            finally
            {
                _isSuppressingSave = false;
                SyncQuadrantLists();
                UpdateCountsAndInsight();
            }
        }

        private class TaskDto
        {
            public string C { get; set; } = "";
            public int Q { get; set; }
            public bool F { get; set; }
            public string? D { get; set; } // Hạn chót định dạng chuỗi ISO
        }
    }
}
