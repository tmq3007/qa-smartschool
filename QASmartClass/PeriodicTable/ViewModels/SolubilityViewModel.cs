using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json;
using QASmartTouch.PeriodicTable.Models;
using QASmartTouch.Shared;

namespace QASmartTouch.PeriodicTable.ViewModels
{
    /// <summary>
    /// ViewModel cho cửa sổ Biểu đồ hòa tan
    /// </summary>
    public class SolubilityViewModel : INotifyPropertyChanged
    {
        #region Properties

        private SolubilityData _data;
        private ObservableCollection<SaltInfo> _salts;
        private ObservableCollection<SaltInfo> _filteredSalts;
        private SaltInfo _selectedSalt;
        private string _searchText;
        private string _selectedCationFilter;
        private string _selectedAnionFilter;
        private string _selectedStatusFilter;

        /// <summary>
        /// Dữ liệu đầy đủ từ JSON
        /// </summary>
        public SolubilityData Data
        {
            get => _data;
            set
            {
                _data = value;
                OnPropertyChanged(nameof(Data));
            }
        }

        /// <summary>
        /// Toàn bộ danh sách muối
        /// </summary>
        public ObservableCollection<SaltInfo> Salts
        {
            get => _salts;
            set
            {
                _salts = value;
                OnPropertyChanged(nameof(Salts));
            }
        }

        /// <summary>
        /// Danh sách muối sau khi lọc/tìm kiếm
        /// </summary>
        public ObservableCollection<SaltInfo> FilteredSalts
        {
            get => _filteredSalts;
            set
            {
                _filteredSalts = value;
                OnPropertyChanged(nameof(FilteredSalts));
            }
        }

        /// <summary>
        /// Muối được chọn để hiển thị chi tiết
        /// </summary>
        public SaltInfo SelectedSalt
        {
            get => _selectedSalt;
            set
            {
                _selectedSalt = value;
                OnPropertyChanged(nameof(SelectedSalt));
            }
        }

        /// <summary>
        /// Văn bản tìm kiếm
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                ApplyFilters();
            }
        }

        /// <summary>
        /// Cation được chọn để lọc
        /// </summary>
        public string SelectedCationFilter
        {
            get => _selectedCationFilter;
            set
            {
                _selectedCationFilter = value;
                OnPropertyChanged(nameof(SelectedCationFilter));
                ApplyFilters();
            }
        }

        /// <summary>
        /// Anion được chọn để lọc
        /// </summary>
        public string SelectedAnionFilter
        {
            get => _selectedAnionFilter;
            set
            {
                _selectedAnionFilter = value;
                OnPropertyChanged(nameof(SelectedAnionFilter));
                ApplyFilters();
            }
        }

        /// <summary>
        /// Trạng thái được chọn để lọc
        /// </summary>
        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                _selectedStatusFilter = value;
                OnPropertyChanged(nameof(SelectedStatusFilter));
                ApplyFilters();
            }
        }

        /// <summary>
        /// Danh sách cation để hiển thị trong ComboBox
        /// </summary>
        public ObservableCollection<string> CationsList { get; set; }

        /// <summary>
        /// Danh sách anion để hiển thị trong ComboBox
        /// </summary>
        public ObservableCollection<string> AnionsList { get; set; }

        /// <summary>
        /// Danh sách trạng thái để lọc
        /// </summary>
        public ObservableCollection<string> StatusList { get; set; }

        /// <summary>
        /// Danh sách quy tắc hòa tan
        /// </summary>
        public ObservableCollection<SolubilityRule> Rules { get; set; }

        #endregion

        #region Commands

        public ICommand ClearFiltersCommand { get; }
        public ICommand OpenRulesCommand { get; }
        public ICommand OpenSimulationCommand { get; }

        #endregion

        #region Constructor

        public SolubilityViewModel()
        {
            // Khởi tạo collections
            Salts = new ObservableCollection<SaltInfo>();
            FilteredSalts = new ObservableCollection<SaltInfo>();
            CationsList = new ObservableCollection<string>();
            AnionsList = new ObservableCollection<string>();
            StatusList = new ObservableCollection<string>
            {
                "Tất cả",
                "Tan",
                "Ít tan",
                "Không tan",
                "Phản ứng",
                "Không tồn tại"
            };
            Rules = new ObservableCollection<SolubilityRule>();

            // Khởi tạo commands
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            OpenRulesCommand = new RelayCommand(OpenRules);
            OpenSimulationCommand = new RelayCommand(OpenSimulation);

            // Load dữ liệu
            LoadData();
        }

        #endregion

        #region Methods

        // Helper method để loại bỏ UTF-8 BOM nếu có
        private string ReadJsonFileWithoutBom(string filePath)
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            int startIndex = 0;
            
            // Kiểm tra và loại bỏ UTF-8 BOM (EF BB BF)
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                startIndex = 3;
            }
            
            // Decode UTF-8 và loại bỏ zero-width characters
            string content = System.Text.Encoding.UTF8.GetString(bytes, startIndex, bytes.Length - startIndex);
            content = content.Replace("\uFEFF", "").Replace("\u200B", "").Replace("\u200C", "").Replace("\u200D", "");
            return content;
        }

        /// <summary>
        /// Load dữ liệu từ file JSON
        /// </summary>
        private void LoadData()
        {
            try
            {
                string jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PeriodicTable", "Data", "solubility_data.json");
                
                if (!File.Exists(jsonPath))
                {
                    MessageBox.Show($"Không tìm thấy file dữ liệu: {jsonPath}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string jsonContent = ReadJsonFileWithoutBom(jsonPath);
                Data = JsonConvert.DeserializeObject<SolubilityData>(jsonContent);

                if (Data == null)
                {
                    MessageBox.Show("Không thể đọc dữ liệu từ file JSON", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Load muối vào collection
                Salts.Clear();
                foreach (var salt in Data.SolubilityMatrix)
                {
                    Salts.Add(salt);
                }

                // Load cation list
                CationsList.Clear();
                CationsList.Add("Tất cả");
                foreach (var cation in Data.Cations)
                {
                    CationsList.Add(cation.Symbol);
                }

                // Load anion list
                AnionsList.Clear();
                AnionsList.Add("Tất cả");
                foreach (var anion in Data.Anions)
                {
                    AnionsList.Add(anion.Symbol);
                }

                // Load rules
                Rules.Clear();
                foreach (var rule in Data.Rules)
                {
                    Rules.Add(rule);
                }

                // Hiển thị tất cả ban đầu
                SelectedCationFilter = "Tất cả";
                SelectedAnionFilter = "Tất cả";
                SelectedStatusFilter = "Tất cả";
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi load dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Áp dụng bộ lọc và tìm kiếm
        /// </summary>
        private void ApplyFilters()
        {
            if (Salts == null || Salts.Count == 0)
                return;

            var filtered = Salts.AsEnumerable();

            // Lọc theo cation
            if (!string.IsNullOrEmpty(SelectedCationFilter) && SelectedCationFilter != "Tất cả")
            {
                filtered = filtered.Where(s => s.Cation == SelectedCationFilter);
            }

            // Lọc theo anion
            if (!string.IsNullOrEmpty(SelectedAnionFilter) && SelectedAnionFilter != "Tất cả")
            {
                filtered = filtered.Where(s => s.Anion == SelectedAnionFilter);
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(SelectedStatusFilter) && SelectedStatusFilter != "Tất cả")
            {
                string statusValue = SelectedStatusFilter.ToLower();
                switch (statusValue)
                {
                    case "tan":
                        filtered = filtered.Where(s => s.Status == "soluble");
                        break;
                    case "ít tan":
                        filtered = filtered.Where(s => s.Status == "slightly_soluble");
                        break;
                    case "không tan":
                        filtered = filtered.Where(s => s.Status == "insoluble");
                        break;
                    case "phản ứng":
                        filtered = filtered.Where(s => s.Status == "reacts");
                        break;
                    case "không tồn tại":
                        filtered = filtered.Where(s => s.Status == "na");
                        break;
                }
            }

            // Tìm kiếm theo văn bản
            if (!string.IsNullOrEmpty(SearchText))
            {
                string search = SearchText.ToLower();
                filtered = filtered.Where(s =>
                    s.SaltFormula.ToLower().Contains(search) ||
                    s.SaltName.ToLower().Contains(search) ||
                    s.Cation.ToLower().Contains(search) ||
                    s.Anion.ToLower().Contains(search) ||
                    s.Note.ToLower().Contains(search)
                );
            }

            // Cập nhật filtered collection
            FilteredSalts.Clear();
            foreach (var salt in filtered)
            {
                FilteredSalts.Add(salt);
            }
        }

        /// <summary>
        /// Xóa tất cả bộ lọc
        /// </summary>
        private void ClearFilters(object parameter)
        {
            SearchText = string.Empty;
            SelectedCationFilter = "Tất cả";
            SelectedAnionFilter = "Tất cả";
            SelectedStatusFilter = "Tất cả";
        }

        /// <summary>
        /// Mở cửa sổ Quy tắc tan
        /// </summary>
        private void OpenRules(object parameter)
        {
            var rulesWindow = new Views.RulesWindow();
            WindowHelper.ShowChildDialog(rulesWindow);
        }

        /// <summary>
        /// Mở cửa sổ Mô phỏng thí nghiệm
        /// </summary>
        private void OpenSimulation(object parameter)
        {
            var simulationWindow = new Views.SimulationWindow();
            WindowHelper.ShowChildDialog(simulationWindow);
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}

