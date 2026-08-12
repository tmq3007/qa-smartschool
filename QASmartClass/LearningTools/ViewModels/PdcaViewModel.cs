using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using QASmartClass.LearningTools.Helpers;

namespace QASmartClass.LearningTools.ViewModels
{
    /// <summary>
    /// Thực thể lưu trữ trạng thái của một chu kỳ PDCA.
    /// </summary>
    public class PdcaCycleState
    {
        public int CycleNumber { get; set; }
        public string Plan { get; set; } = "";
        public string Do { get; set; } = "";
        public string Check { get; set; } = "";
        public string Act { get; set; } = "";
    }

    /// <summary>
    /// Cấu trúc tuần tự hóa để lưu vào database.
    /// </summary>
    public class PdcaState
    {
        public string Subject { get; set; } = "";
        public int CurrentCycle { get; set; } = 1;
        public List<PdcaCycleState> Cycles { get; set; } = new();
    }

    /// <summary>
    /// ViewModel quản lý dữ liệu và nghiệp vụ cho Chu trình PDCA.
    /// </summary>
    public partial class PdcaViewModel : ObservableObject
    {
        private const string ToolId = "pdca";

        [ObservableProperty]
        private string _subject = "";

        [ObservableProperty]
        private int _currentCycle = 1;

        [ObservableProperty]
        private string _planText = "";

        [ObservableProperty]
        private string _doText = "";

        [ObservableProperty]
        private string _checkText = "";

        [ObservableProperty]
        private string _actText = "";

        public List<PdcaCycleState> Cycles { get; set; } = new();

        public PdcaViewModel()
        {
            LoadState();
        }

        /// <summary>
        /// Nạp trạng thái từ CSDL SQLite.
        /// </summary>
        public void LoadState()
        {
            try
            {
                string json = DbManager.LoadWorkplaceState(ToolId);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var state = JsonConvert.DeserializeObject<PdcaState>(json);
                    if (state != null)
                    {
                        Subject = state.Subject;
                        CurrentCycle = state.CurrentCycle;
                        Cycles = state.Cycles ?? new List<PdcaCycleState>();

                        // Nạp dữ liệu của chu kỳ hiện tại lên giao diện
                        var current = Cycles.FirstOrDefault(c => c.CycleNumber == CurrentCycle);
                        if (current != null)
                        {
                            PlanText = current.Plan ?? "";
                            DoText = current.Do ?? "";
                            CheckText = current.Check ?? "";
                            ActText = current.Act ?? "";
                        }
                        else
                        {
                            ClearCycleInputs();
                        }
                        return;
                    }
                }
            }
            catch
            {
                // Bỏ qua lỗi nạp trạng thái để tránh crash ứng dụng
            }

            // Trạng thái mặc định nếu chưa có DB hoặc lỗi
            Subject = "";
            CurrentCycle = 1;
            Cycles = new List<PdcaCycleState>
            {
                new PdcaCycleState { CycleNumber = 1 }
            };
            ClearCycleInputs();
        }

        /// <summary>
        /// Lưu trạng thái hiện tại xuống CSDL SQLite.
        /// </summary>
        public void SaveState()
        {
            try
            {
                // Đồng bộ text hiện tại trên UI vào danh sách Cycles trước khi lưu
                UpdateCurrentCycleState();

                var state = new PdcaState
                {
                    Subject = Subject,
                    CurrentCycle = CurrentCycle,
                    Cycles = Cycles
                };

                string json = JsonConvert.SerializeObject(state);
                DbManager.SaveWorkplaceState(ToolId, json);
            }
            catch
            {
                // Bỏ qua lỗi ghi
            }
        }

        /// <summary>
        /// Chuyển đổi chu kỳ (tăng hoặc giảm).
        /// </summary>
        /// <param name="direction">1 là tiến, -1 là lùi</param>
        /// <param name="shouldClearOnNew">Có xóa bảng nếu tạo chu kỳ mới không</param>
        public void ChangeCycle(int direction, bool shouldClearOnNew = false)
        {
            int newCycle = CurrentCycle + direction;
            if (newCycle < 1) return;

            // 1. Lưu lại thông tin chu kỳ hiện tại
            UpdateCurrentCycleState();

            // 2. Tìm hoặc tạo mới chu kỳ tiếp theo
            var target = Cycles.FirstOrDefault(c => c.CycleNumber == newCycle);
            if (target == null)
            {
                target = new PdcaCycleState { CycleNumber = newCycle };
                if (!shouldClearOnNew)
                {
                    // Copy dữ liệu từ chu kỳ cũ sang làm bàn đạp (hoặc để trống tùy lựa chọn)
                    var current = Cycles.FirstOrDefault(c => c.CycleNumber == CurrentCycle);
                    if (current != null)
                    {
                        target.Plan = current.Plan;
                        target.Do = current.Do;
                        target.Check = current.Check;
                        target.Act = current.Act;
                    }
                }
                Cycles.Add(target);
            }

            // 3. Cập nhật thuộc tính hiển thị
            CurrentCycle = newCycle;
            PlanText = target.Plan ?? "";
            DoText = target.Do ?? "";
            CheckText = target.Check ?? "";
            ActText = target.Act ?? "";

            // 4. Lưu ngay xuống DB
            SaveState();
        }

        /// <summary>
        /// Áp dụng dữ liệu mẫu từ Template.
        /// </summary>
        public void ApplyTemplate(string plan, string @do, string check, string act)
        {
            PlanText = plan;
            DoText = @do;
            CheckText = check;
            ActText = act;
            SaveState();
        }

        /// <summary>
        /// Xóa sạch toàn bộ nội dung và đặt lại về Chu kỳ 1.
        /// </summary>
        public void ClearAll()
        {
            Subject = "";
            CurrentCycle = 1;
            Cycles.Clear();
            Cycles.Add(new PdcaCycleState { CycleNumber = 1 });
            ClearCycleInputs();
            SaveState();
        }

        private void UpdateCurrentCycleState()
        {
            var current = Cycles.FirstOrDefault(c => c.CycleNumber == CurrentCycle);
            if (current == null)
            {
                current = new PdcaCycleState { CycleNumber = CurrentCycle };
                Cycles.Add(current);
            }

            current.Plan = PlanText ?? "";
            current.Do = DoText ?? "";
            current.Check = CheckText ?? "";
            current.Act = ActText ?? "";
        }

        private void ClearCycleInputs()
        {
            PlanText = "";
            DoText = "";
            CheckText = "";
            ActText = "";
        }
    }
}
