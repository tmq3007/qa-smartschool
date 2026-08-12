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
    public class MainViewModel : INotifyPropertyChanged
    {
        private Element _selectedElement;
        private string _searchText;
        private ObservableCollection<Element> _elements;
        private ObservableCollection<Element> _mainElements;
        private ObservableCollection<Element> _lanthanideElements;
        private ObservableCollection<Element> _actinideElements;
        private ObservableCollection<Category> _categories;

        public ObservableCollection<Element> Elements 
        { 
            get => _elements;
            set
            {
                _elements = value;
                OnPropertyChanged(nameof(Elements));
            }
        }
        
        public ObservableCollection<Element> MainElements 
        { 
            get => _mainElements;
            set
            {
                _mainElements = value;
                OnPropertyChanged(nameof(MainElements));
            }
        }
        
        public ObservableCollection<Element> LanthanideElements 
        { 
            get => _lanthanideElements;
            set
            {
                _lanthanideElements = value;
                OnPropertyChanged(nameof(LanthanideElements));
            }
        }
        
        public ObservableCollection<Element> ActinideElements 
        { 
            get => _actinideElements;
            set
            {
                _actinideElements = value;
                OnPropertyChanged(nameof(ActinideElements));
            }
        }

        public ObservableCollection<Category> Categories 
        { 
            get => _categories;
            set
            {
                _categories = value;
                OnPropertyChanged(nameof(Categories));
            }
        }
        
        private string _selectedCategory;
        private int? _selectedPeriod;
        private int? _selectedGroup;
        
        public ICommand ElementSelectedCommand { get; set; }
        public ICommand SearchCommand { get; set; }
        public ICommand CategorySelectedCommand { get; set; }
        public ICommand PeriodSelectedCommand { get; set; }
        public ICommand GroupSelectedCommand { get; set; }
        public ICommand ClearSelectionCommand { get; set; }
        public ICommand OpenCompareCommand { get; set; }
        public ICommand OpenSolubilityCommand { get; set; }
        public ICommand OpenIntroductionCommand { get; set; }
        public ICommand OpenReactivityCommand { get; set; }
        
        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged(nameof(SelectedCategory));
                UpdateElementHighlights();
            }
        }

        public int? SelectedPeriod
        {
            get => _selectedPeriod;
            set
            {
                _selectedPeriod = value;
                OnPropertyChanged(nameof(SelectedPeriod));
                UpdateElementHighlights();
            }
        }

        public int? SelectedGroup
        {
            get => _selectedGroup;
            set
            {
                _selectedGroup = value;
                OnPropertyChanged(nameof(SelectedGroup));
                UpdateElementHighlights();
            }
        }

        // Collections for period and group numbers
        public List<int> Periods { get; } = new List<int> { 1, 2, 3, 4, 5, 6, 7 };
        public List<int> Groups { get; } = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 };

        public Element SelectedElement
        {
            get => _selectedElement;
            set
            {
                _selectedElement = value;
                OnPropertyChanged(nameof(SelectedElement));
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                if (string.IsNullOrEmpty(value))
                {
                    ClearHighlight();
                }
                else
                {
                    SearchElement(value);
                }
            }
        }

        public MainViewModel()
        {
            Elements = new ObservableCollection<Element>();
            MainElements = new ObservableCollection<Element>();
            LanthanideElements = new ObservableCollection<Element>();
            ActinideElements = new ObservableCollection<Element>();
            Categories = new ObservableCollection<Category>();
            
            // Khởi tạo rõ ràng để đảm bảo chi tiết nguyên tố ẩn ban đầu
            SelectedElement = null;
            SelectedCategory = null;
            
            ElementSelectedCommand = new RelayCommand(OnElementSelected);
            SearchCommand = new RelayCommand(OnSearch);
            CategorySelectedCommand = new RelayCommand(OnCategorySelected);
            PeriodSelectedCommand = new RelayCommand(OnPeriodSelected);
            GroupSelectedCommand = new RelayCommand(OnGroupSelected);
            ClearSelectionCommand = new RelayCommand(OnClearSelection);
            OpenCompareCommand = new RelayCommand(OnOpenCompare);
            OpenSolubilityCommand = new RelayCommand(OnOpenSolubility);
            OpenIntroductionCommand = new RelayCommand(OnOpenIntroduction);
            OpenReactivityCommand = new RelayCommand(OnOpenReactivity);
            
            LoadElementsFromJson();
            InitializeCategories();
        }

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

        private void InitializeCategories()
        {
            Categories.Clear();
            Categories.Add(new Category { Name = "Kim loại kiềm", Color = "#FFE57F" });
            Categories.Add(new Category { Name = "Kim loại kiềm thổ", Color = "#81D4FA" });
            Categories.Add(new Category { Name = "Kim loại chuyển tiếp", Color = "#FFCDD2" });
            Categories.Add(new Category { Name = "Kim loại yếu", Color = "#CE93D8" });
            Categories.Add(new Category { Name = "Phi kim", Color = "#A5D6A7" });
            Categories.Add(new Category { Name = "Á kim", Color = "#FFAB91" });
            Categories.Add(new Category { Name = "Halogen", Color = "#B39DDB" });
            Categories.Add(new Category { Name = "Khí trơ", Color = "#C8E6C9" });
            Categories.Add(new Category { Name = "Nhóm Lantan", Color = "#FFE082" });
            Categories.Add(new Category { Name = "Nhóm Actini", Color = "#BCAAA4" });
        }

        private void LoadElementsFromJson()
        {
            try
            {
                Elements.Clear();
                MainElements.Clear();
                LanthanideElements.Clear();
                ActinideElements.Clear();

                string baseDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PeriodicTable", "Data");
                
                // Load từ 4 file JSON riêng biệt
                var filesToLoad = new[]
                {
                    "elements_main_groups.json",
                    "elements_transition_metals.json",
                    "elements_lanthanides.json",
                    "elements_actinides.json"
                };

                bool anyFileLoaded = false;

                foreach (var fileName in filesToLoad)
                {
                    string jsonPath = Path.Combine(baseDataPath, fileName);
                    
                    if (File.Exists(jsonPath))
                    {
                        try
                        {
                            // Đọc file và loại bỏ BOM nếu có
                            string jsonContent = ReadJsonFileWithoutBom(jsonPath);
                            var elements = JsonConvert.DeserializeObject<Element[]>(jsonContent);
                            
                            if (elements != null && elements.Length > 0)
                            {
                                foreach (var element in elements)
                                {
                                    // Chuẩn hóa danh mục tiếng Việt
                                    NormalizeElementCategory(element);
                                    
                                    // Thiết lập Row và Column từ Period và Group
                                    SetRowAndColumn(element);
                                    
                                    // Thiết lập màu sắc theo category
                                    SetElementColor(element);
                                    Elements.Add(element);
                                    
                                    // Phân loại nguyên tố theo nhóm
                                    if (element.AtomicNumber >= 57 && element.AtomicNumber <= 71) // Lanthanides
                                    {
                                        LanthanideElements.Add(element);
                                    }
                                    else if (element.AtomicNumber >= 89 && element.AtomicNumber <= 103) // Actinides
                                    {
                                        ActinideElements.Add(element);
                                    }
                                    else // Main table elements (rows 1-7)
                                    {
                                        MainElements.Add(element);
                                    }
                                }
                                anyFileLoaded = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Lỗi khi đọc file {fileName}: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }

                if (anyFileLoaded && Elements.Count > 0)
                {
                    // Thêm placeholder cho Lanthanides và Actinides trong main table
                    AddPlaceholderElements();
                }
                else
                {
                    // Fallback: thử load file cũ elements_updated.json hoặc elements.json
                    string fallbackPath = Path.Combine(baseDataPath, "elements_updated.json");
                    if (!File.Exists(fallbackPath))
                    {
                        fallbackPath = Path.Combine(baseDataPath, "elements.json");
                    }

                    if (File.Exists(fallbackPath))
                    {
                        // Đọc file và loại bỏ BOM nếu có
                        string jsonContent = ReadJsonFileWithoutBom(fallbackPath);
                        var elements = JsonConvert.DeserializeObject<Element[]>(jsonContent);
                        
                        foreach (var element in elements)
                        {
                            // Chuẩn hóa danh mục tiếng Việt
                            NormalizeElementCategory(element);
                            
                            // Thiết lập Row và Column từ Period và Group
                            SetRowAndColumn(element);
                            
                            SetElementColor(element);
                            Elements.Add(element);
                            
                            // Phân loại nguyên tố theo nhóm
                            if (element.AtomicNumber >= 57 && element.AtomicNumber <= 71) // Lanthanides
                            {
                                LanthanideElements.Add(element);
                            }
                            else if (element.AtomicNumber >= 89 && element.AtomicNumber <= 103) // Actinides
                            {
                                ActinideElements.Add(element);
                            }
                            else // Main table elements (rows 1-7)
                            {
                                MainElements.Add(element);
                            }
                        }
                        
                        AddPlaceholderElements();
                    }
                    else
                    {
                        MessageBox.Show("Không tìm thấy file dữ liệu nguyên tố nào!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi đọc dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddPlaceholderElements()
        {
            // Placeholder cho Lanthanides trong hàng 6, cột 3
            var lanthanidePlaceholder = new Element
            {
                AtomicNumber = 0,
                Symbol = "*",
                Name = "Lanthanides",
                NameVietnamese = "Nhóm Lantan",
                AtomicMass = 0,
                Period = 6,
                Group = 3,
                Row = 6,
                Column = 3,
                Category = "Nhóm Lantan",
                Color = "#FFE082",
                ElectronConfiguration = ""
            };
            lanthanidePlaceholder.DisplayInfo = "Nhóm Lantan (57-71)\nXem dưới bảng chính";
            SetElementColor(lanthanidePlaceholder);
            Elements.Add(lanthanidePlaceholder);
            MainElements.Add(lanthanidePlaceholder);

            // Placeholder cho Actinides trong hàng 7, cột 3
            var actinidePlaceholder = new Element
            {
                AtomicNumber = 0,
                Symbol = "**",
                Name = "Actinides",
                NameVietnamese = "Nhóm Actini",
                AtomicMass = 0,
                Period = 7,
                Group = 3,
                Row = 7,
                Column = 3,
                Category = "Nhóm Actini",
                Color = "#BCAAA4",
                ElectronConfiguration = ""
            };
            actinidePlaceholder.DisplayInfo = "Nhóm Actini (89-103)\nXem dưới bảng chính";
            SetElementColor(actinidePlaceholder);
            Elements.Add(actinidePlaceholder);
            MainElements.Add(actinidePlaceholder);
        }

        private void SetRowAndColumn(Element element)
        {
            // Thiết lập Row từ Period
            element.Row = element.Period;
            
            // Thiết lập Column từ Group
            element.Column = element.Group;
        }

        private void NormalizeElementCategory(Element element)
        {
            if (element == null) return;
            var category = element.Category?.ToLower() ?? "";
            
            if (category.Contains("kim loại kiềm") && !category.Contains("thổ"))
            {
                element.Category = "Kim loại kiềm";
            }
            else if (category.Contains("kim loại kiềm thổ") || category.Contains("kiềm thổ"))
            {
                element.Category = "Kim loại kiềm thổ";
            }
            else if (category.Contains("kim loại chuyển tiếp") || category.Contains("transition metal"))
            {
                element.Category = "Kim loại chuyển tiếp";
            }
            else if (category.Contains("kim loại yếu") || category.Contains("hậu chuyển tiếp") || 
                     category.Contains("post-transition") || category.Contains("sau chuyển tiếp") ||
                     (category.Contains("kim loại") && !category.Contains("đất hiếm") && !category.Contains("actini") && !category.Contains("lantan")))
            {
                element.Category = "Kim loại yếu";
            }
            else if (category.Contains("á kim") || category.Contains("bán kim loại"))
            {
                element.Category = "Á kim";
            }
            else if (category.Contains("phi kim") && !category.Contains("halogen"))
            {
                element.Category = "Phi kim";
            }
            else if (category.Contains("halogen") || category.Contains("phi kim – halogen") || 
                     category.Contains("halogen – phi kim"))
            {
                element.Category = "Halogen";
            }
            else if (category.Contains("khí hiếm") || category.Contains("khí trơ") || 
                     category.Contains("noble gas"))
            {
                element.Category = "Khí trơ";
            }
            else if (category.Contains("lantan") || category.Contains("đất hiếm") || 
                     element.Category == "Nhóm Lantan" || element.Category == "Kim loại đất hiếm")
            {
                element.Category = "Nhóm Lantan";
            }
            else if (category.Contains("actini") || element.Category == "Nhóm Actini" || 
                     element.Category == "Kim loại actinide")
            {
                element.Category = "Nhóm Actini";
            }
            else
            {
                element.Category = "Phi kim"; // Default fallback
            }
        }

        private void SetElementColor(Element element)
        {
            switch (element.Category)
            {
                case "Kim loại kiềm":
                    element.Color = "#FFE57F";
                    break;
                case "Kim loại kiềm thổ":
                    element.Color = "#81D4FA";
                    break;
                case "Kim loại chuyển tiếp":
                    element.Color = "#FFCDD2";
                    break;
                case "Kim loại yếu":
                    element.Color = "#CE93D8";
                    break;
                case "Phi kim":
                    element.Color = "#A5D6A7";
                    break;
                case "Á kim":
                    element.Color = "#FFAB91";
                    break;
                case "Halogen":
                    element.Color = "#B39DDB";
                    break;
                case "Khí trơ":
                    element.Color = "#C8E6C9";
                    break;
                case "Nhóm Lantan":
                    element.Color = "#FFE082";
                    break;
                case "Nhóm Actini":
                    element.Color = "#BCAAA4";
                    break;
                default:
                    element.Color = "#E0E0E0";
                    break;
            }
        }

        private void OnElementSelected(object parameter)
        {
            if (parameter is Element element)
            {
                SelectedElement = element;
            }
        }

        private void OnClearSelection(object parameter)
        {
            SelectedElement = null;
            SelectedCategory = null;
            SearchText = "";
            ClearHighlight();
        }

        private void OnSearch(object parameter)
        {
            if (!string.IsNullOrEmpty(SearchText))
            {
                SearchElement(SearchText);
            }
        }

        private void SearchElement(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                // Nếu từ khóa rỗng, trở về trạng thái bình thường
                ClearHighlight();
                return;
            }

            var lowerKeyword = keyword.ToLower().Trim();

            // Tìm kiếm theo thứ tự ưu tiên:
            // 1. Tìm kiếm chính xác ký hiệu
            var found = Elements.FirstOrDefault(e => 
                e.Symbol.ToLower() == lowerKeyword);

            // 2. Nếu không có, tìm kiếm chính xác số hiệu nguyên tử
            if (found == null && int.TryParse(keyword, out int atomicNumber))
            {
                found = Elements.FirstOrDefault(e => e.AtomicNumber == atomicNumber);
            }

            // 3. Nếu không có, tìm kiếm chính xác tên tiếng Anh
            if (found == null)
            {
                found = Elements.FirstOrDefault(e => 
                    e.Name.ToLower() == lowerKeyword);
            }

            // 4. Nếu không có, tìm kiếm chính xác tên tiếng Việt
            if (found == null)
            {
                found = Elements.FirstOrDefault(e => 
                    e.NameVietnamese.ToLower() == lowerKeyword);
            }

            // 5. Cuối cùng, tìm kiếm contains (bao gồm)
            if (found == null)
            {
                found = Elements.FirstOrDefault(e => 
                    e.Symbol.ToLower().Contains(lowerKeyword) ||
                    e.Name.ToLower().Contains(lowerKeyword) ||
                    e.NameVietnamese.ToLower().Contains(lowerKeyword));
            }

            // Xóa tất cả highlight cũ
            foreach (var element in Elements)
            {
                element.IsHighlighted = false;
                element.OpacityValue = 1.0;
            }

            if (found != null)
            {
                // Đặt SelectedCategory thành "SEARCH" trước
                SelectedCategory = "SEARCH";
                // Sau đó highlight nguyên tố và set opacity
                found.IsHighlighted = true;
                SelectedElement = found;
                
                // Set opacity for search
                foreach (var element in Elements)
                {
                    element.OpacityValue = element.IsHighlighted ? 1.0 : 0.3;
                }
            }
            else
            {
                SelectedCategory = null; // Reset về trạng thái bình thường nếu không tìm thấy
            }
        }

        private void ClearHighlight()
        {
            foreach (var element in Elements)
            {
                element.IsHighlighted = false;
                element.OpacityValue = 1.0;
            }
            
            // Clear tất cả selections
            if (SelectedCategory == "SEARCH")
            {
                SelectedCategory = null;
            }
            SelectedPeriod = null;
            SelectedGroup = null;
        }

        private void OnCategorySelected(object parameter)
        {
            if (parameter is string category)
            {
                // Toggle selection - nếu click lại cùng category thì bỏ chọn
                SelectedCategory = SelectedCategory == category ? null : category;
                
                // Clear other selections và selected element khi chọn category
                if (SelectedCategory != null)
                {
                    SelectedPeriod = null;
                    SelectedGroup = null;
                    SelectedElement = null;
                }
            }
        }

        private void OnPeriodSelected(object parameter)
        {
            if (parameter is int period)
            {
                // Toggle selection - nếu click lại cùng period thì bỏ chọn
                SelectedPeriod = SelectedPeriod == period ? (int?)null : period;
                
                // Clear other selections và selected element khi chọn period
                if (SelectedPeriod != null)
                {
                    SelectedCategory = null;
                    SelectedGroup = null;
                    SelectedElement = null;
                }
            }
        }

        private void OnGroupSelected(object parameter)
        {
            if (parameter is int group)
            {
                // Toggle selection - nếu click lại cùng group thì bỏ chọn
                SelectedGroup = SelectedGroup == group ? (int?)null : group;
                
                // Clear other selections và selected element khi chọn group
                if (SelectedGroup != null)
                {
                    SelectedCategory = null;
                    SelectedPeriod = null;
                    SelectedElement = null;
                }
            }
        }

        private void UpdateElementHighlights()
        {
            foreach (var element in Elements)
            {
                bool shouldHighlight = false;
                
                // Ưu tiên: Search > Category > Period > Group
                if (SelectedCategory == "SEARCH")
                {
                    // Cho search, IsHighlighted đã được set trong OnSearch method
                    // Không cần thay đổi gì ở đây, chỉ skip
                    continue; 
                }
                else if (!string.IsNullOrEmpty(SelectedCategory))
                {
                    shouldHighlight = element.Category == SelectedCategory;
                }
                else if (SelectedPeriod.HasValue)
                {
                    shouldHighlight = element.Period == SelectedPeriod.Value;
                }
                else if (SelectedGroup.HasValue)
                {
                    // Loại trừ Lanthanides (57-71) và Actinides (89-103) khi filter theo Group
                    bool isLanthanideOrActinide = (element.AtomicNumber >= 57 && element.AtomicNumber <= 71) ||
                                                   (element.AtomicNumber >= 89 && element.AtomicNumber <= 103);
                    
                    shouldHighlight = !isLanthanideOrActinide && element.Group == SelectedGroup.Value;
                }
                else
                {
                    // Không có filter nào - tất cả nguyên tố hiển thị bình thường
                    shouldHighlight = false;
                }
                
                element.IsHighlighted = shouldHighlight;
                
                // Set opacity directly
                if (!string.IsNullOrEmpty(SelectedCategory) || SelectedPeriod.HasValue || SelectedGroup.HasValue)
                {
                    element.OpacityValue = shouldHighlight ? 1.0 : 0.3;
                }
                else
                {
                    element.OpacityValue = 1.0; // No filter active
                }
            }
        }

        private void OnOpenCompare(object parameter)
        {
            var compareWindow = new Views.CompareWindow();
            WindowHelper.ShowChildDialog(compareWindow);
        }

        private void OnOpenSolubility(object parameter)
        {
            var solubilityWindow = new Views.SolubilityWindow();
            WindowHelper.ShowChildDialog(solubilityWindow);
        }

        private void OnOpenIntroduction(object parameter)
        {
            var introductionWindow = new Views.IntroductionWindow();
            WindowHelper.ShowChildDialog(introductionWindow);
        }

        private void OnOpenReactivity(object parameter)
        {
            var reactivityWindow = new Views.ReactivityWindow();
            WindowHelper.ShowChildDialog(reactivityWindow);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
