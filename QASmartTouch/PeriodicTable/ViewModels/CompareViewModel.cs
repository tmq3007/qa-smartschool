using System;
using System.Globalization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json;
using QASmartTouch.PeriodicTable.Models;

namespace QASmartTouch.PeriodicTable.ViewModels
{
    public class CompareViewModel : INotifyPropertyChanged
    {
        private Element _element1;
        private Element _element2;
        private Element _element3;
        private ObservableCollection<ComparisonItem> _comparisonData;
        private ObservableCollection<ComparisonItem> _allComparisonData;
        private string _selectedTab;
        private int _compareMode = 2; // 2 or 3 elements

        public ObservableCollection<Element> AllElements { get; set; }
        
        public int CompareMode
        {
            get => _compareMode;
            set
            {
                _compareMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsThreeElementMode));
                UpdateComparison();
            }
        }
        
        public bool IsThreeElementMode => _compareMode == 3;
        
        public string SelectedTab
        {
            get => _selectedTab;
            set
            {
                _selectedTab = value;
                OnPropertyChanged();
                FilterComparisonData();
            }
        }
        
        public Element Element1
        {
            get => _element1;
            set
            {
                _element1 = value;
                OnPropertyChanged();
                UpdateComparison();
            }
        }

        public Element Element2
        {
            get => _element2;
            set
            {
                _element2 = value;
                OnPropertyChanged();
                UpdateComparison();
            }
        }
        
        public Element Element3
        {
            get => _element3;
            set
            {
                _element3 = value;
                OnPropertyChanged();
                UpdateComparison();
            }
        }

        public ObservableCollection<ComparisonItem> ComparisonData
        {
            get => _comparisonData;
            set
            {
                _comparisonData = value;
                OnPropertyChanged();
            }
        }

        public ICommand CloseCommand { get; set; }
        public ICommand SwapElementsCommand { get; set; }
        public ICommand ExportPdfCommand { get; set; }

        public CompareViewModel()
        {
            AllElements = new ObservableCollection<Element>();
            ComparisonData = new ObservableCollection<ComparisonItem>();
            _allComparisonData = new ObservableCollection<ComparisonItem>();
            _selectedTab = "General"; // Tab mặc định
            
            LoadElements();
            
            // Chọn mặc định: Hydro, Helium, và Liti
            if (AllElements.Count >= 3)
            {
                Element1 = AllElements[0]; // H
                Element2 = AllElements[1]; // He
                Element3 = AllElements[2]; // Li
            }

            CloseCommand = new RelayCommand(CloseWindow);
            SwapElementsCommand = new RelayCommand(SwapElements);
            ExportPdfCommand = new RelayCommand(ExportPdf);
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

        private void LoadElements()
        {
            try
            {
                AllElements.Clear();
                string baseDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PeriodicTable", "Data");
                
                var filesToLoad = new[]
                {
                    "elements_main_groups.json",
                    "elements_transition_metals.json",
                    "elements_lanthanides.json",
                    "elements_actinides.json"
                };

                var tempElements = new List<Element>();

                foreach (var fileName in filesToLoad)
                {
                    string jsonPath = Path.Combine(baseDataPath, fileName);
                    if (File.Exists(jsonPath))
                    {
                        string jsonContent = ReadJsonFileWithoutBom(jsonPath);
                        var elements = JsonConvert.DeserializeObject<Element[]>(jsonContent);
                        if (elements != null)
                        {
                            foreach (var element in elements)
                            {
                                NormalizeElementCategory(element);
                                SetElementColor(element);
                                tempElements.Add(element);
                            }
                        }
                    }
                }

                foreach (var element in tempElements.OrderBy(e => e.AtomicNumber))
                {
                    AllElements.Add(element);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        private void UpdateComparison()
        {
            _allComparisonData.Clear();

            if (Element1 == null || Element2 == null)
                return;
            
            // Check if we need 3rd element in 3-element mode
            if (CompareMode == 3 && Element3 == null)
                return;

            // ===== TÍNH CHẤT CHUNG (GENERAL) =====
            _allComparisonData.Add(new ComparisonItem { 
                Property = "Tên", 
                Value1 = Element1.NameVietnamese, 
                Value2 = Element2.NameVietnamese,
                Value3 = CompareMode == 3 ? Element3?.NameVietnamese : null,
                Category = "General"
            });
            _allComparisonData.Add(new ComparisonItem { 
                Property = "Ký hiệu", 
                Value1 = Element1.Symbol, 
                Value2 = Element2.Symbol,
                Value3 = CompareMode == 3 ? Element3?.Symbol : null,
                Category = "General"
            });
            _allComparisonData.Add(new ComparisonItem { 
                Property = "Số nguyên tử", 
                Value1 = Element1.AtomicNumber.ToString(), 
                Value2 = Element2.AtomicNumber.ToString(),
                Value3 = CompareMode == 3 ? Element3?.AtomicNumber.ToString() : null,
                Category = "General"
            });
            _allComparisonData.Add(new ComparisonItem { 
                Property = "Nguyên tử khối", 
                Value1 = Element1.AtomicMass.ToString("F3"), 
                Value2 = Element2.AtomicMass.ToString("F3"),
                Value3 = CompareMode == 3 ? Element3?.AtomicMass.ToString("F3") : null,
                Category = "General"
            });
            _allComparisonData.Add(new ComparisonItem { 
                Property = "Nhóm", 
                Value1 = Element1.Group.ToString(), 
                Value2 = Element2.Group.ToString(),
                Value3 = CompareMode == 3 ? Element3?.Group.ToString() : null,
                Category = "General"
            });
            _allComparisonData.Add(new ComparisonItem { 
                Property = "Chu kỳ", 
                Value1 = Element1.Period.ToString(), 
                Value2 = Element2.Period.ToString(),
                Value3 = CompareMode == 3 ? Element3?.Period.ToString() : null,
                Category = "General"
            });
            _allComparisonData.Add(new ComparisonItem { 
                Property = "Danh mục", 
                Value1 = Element1.Category, 
                Value2 = Element2.Category,
                Value3 = CompareMode == 3 ? Element3?.Category : null,
                Category = "General"
            });
            _allComparisonData.Add(new ComparisonItem { 
                Property = "Màu sắc", 
                Value1 = NormalizeText(Element1.ColorDescription ?? "Chưa xác định"), 
                Value2 = NormalizeText(Element2.ColorDescription ?? "Chưa xác định"),
                Value3 = CompareMode == 3 ? NormalizeText(Element3?.ColorDescription ?? "Chưa xác định") : null,
                Category = "General"
            });
            _allComparisonData.Add(new ComparisonItem { 
                Property = "Có tính phóng xạ", 
                Value1 = Element1.IsRadioactive ? "Có" : "Không", 
                Value2 = Element2.IsRadioactive ? "Có" : "Không",
                Value3 = CompareMode == 3 ? (Element3?.IsRadioactive == true ? "Có" : "Không") : null,
                Category = "General"
            });
            _allComparisonData.Add(new ComparisonItem { 
                Property = "Cấu trúc tinh thể", 
                Value1 = NormalizeText(Element1.CrystalStructure ?? "Chưa xác định"), 
                Value2 = NormalizeText(Element2.CrystalStructure ?? "Chưa xác định"),
                Value3 = CompareMode == 3 ? NormalizeText(Element3?.CrystalStructure ?? "Chưa xác định") : null,
                Category = "General"
            });
            _allComparisonData.Add(new ComparisonItem { 
                Property = "Hàm lượng trong vỏ trái đất", 
                Value1 = NormalizeText(Element1.EarthCrustAbundance ?? "Chưa xác định"), 
                Value2 = NormalizeText(Element2.EarthCrustAbundance ?? "Chưa xác định"),
                Value3 = CompareMode == 3 ? NormalizeText(Element3?.EarthCrustAbundance ?? "Chưa xác định") : null,
                Category = "General"
            });
            _allComparisonData.Add(new ComparisonItem { 
                Property = "Hàm lượng trong vũ trụ", 
                Value1 = NormalizeText(Element1.CosmicAbundance ?? "Chưa xác định"), 
                Value2 = NormalizeText(Element2.CosmicAbundance ?? "Chưa xác định"),
                Value3 = CompareMode == 3 ? NormalizeText(Element3?.CosmicAbundance ?? "Chưa xác định") : null,
                Category = "General"
            });

            // ===== TÍNH CHẤT VẬT LÝ (PHYSICAL) =====
            _allComparisonData.Add(new ComparisonItem { 
                Property = "Mật độ (g/cm³)", 
                Value1 = FormatValue(Element1.Density, "F3", "Chưa xác định"),
                Value2 = FormatValue(Element2.Density, "F3", "Chưa xác định"),
                Value3 = CompareMode == 3 ? FormatValue(Element3?.Density, "F3", "Chưa xác định") : null,
                Category = "Physical"
            });
            _allComparisonData.Add(new ComparisonItem { 
                Property = "Trạng thái vật chất", 
                Value1 = NormalizePhysicalState(Element1.PhysicalState), 
                Value2 = NormalizePhysicalState(Element2.PhysicalState),
                Value3 = CompareMode == 3 ? NormalizePhysicalState(Element3?.PhysicalState) : null,
                Category = "Physical"
            });
            _allComparisonData.Add(new ComparisonItem { 
                Property = "Nhiệt độ nóng chảy", 
                Value1 = FormatTemperature(Element1.MeltingPoint),
                Value2 = FormatTemperature(Element2.MeltingPoint),
                Value3 = CompareMode == 3 ? FormatTemperature(Element3?.MeltingPoint) : null,
                Category = "Physical"
            });
            _allComparisonData.Add(new ComparisonItem { 
                Property = "Nhiệt độ sôi", 
                Value1 = FormatTemperature(Element1.BoilingPoint),
                Value2 = FormatTemperature(Element2.BoilingPoint),
                Value3 = CompareMode == 3 ? FormatTemperature(Element3?.BoilingPoint) : null,
                Category = "Physical"
            });
            _allComparisonData.Add(new ComparisonItem { 
                Property = "Nhiệt lượng nóng chảy (kJ/mol)", 
                Value1 = FormatValue(Element1.HeatOfFusion, "F1", "Chưa xác định"),
                Value2 = FormatValue(Element2.HeatOfFusion, "F1", "Chưa xác định"),
                Value3 = CompareMode == 3 ? FormatValue(Element3?.HeatOfFusion, "F1", "Chưa xác định") : null,
                Category = "Physical"
            });
            _allComparisonData.Add(new ComparisonItem { 
                Property = "Nhiệt bay hơi (kJ/mol)", 
                Value1 = FormatValue(Element1.HeatOfVaporization, "F0", "Chưa xác định"),
                Value2 = FormatValue(Element2.HeatOfVaporization, "F0", "Chưa xác định"),
                Value3 = CompareMode == 3 ? FormatValue(Element3?.HeatOfVaporization, "F0", "Chưa xác định") : null,
                Category = "Physical"
            });
            _allComparisonData.Add(new ComparisonItem { 
                Property = "Nhiệt dung (J/g·K)", 
                Value1 = FormatValue(Element1.SpecificHeat, "F3", "Chưa xác định"),
                Value2 = FormatValue(Element2.SpecificHeat, "F3", "Chưa xác định"),
                Value3 = CompareMode == 3 ? FormatValue(Element3?.SpecificHeat, "F3", "Chưa xác định") : null,
                Category = "Physical"
            });
            _allComparisonData.Add(new ComparisonItem { 
                Property = "Độ dẫn nhiệt (W/cm·K)", 
                Value1 = FormatValue(Element1.ThermalConductivity, "F3", "Chưa xác định"),
                Value2 = FormatValue(Element2.ThermalConductivity, "F3", "Chưa xác định"),
                Value3 = CompareMode == 3 ? FormatValue(Element3?.ThermalConductivity, "F3", "Chưa xác định") : null,
                Category = "Physical"
            });

            // ===== TÍNH CHẤT HÓA HỌC (CHEMICAL) =====
            _allComparisonData.Add(new ComparisonItem { 
                Property = "Độ âm điện (Thang Pauling)", 
                Value1 = FormatValue(Element1.Electronegativity, "F2", "Không có"),
                Value2 = FormatValue(Element2.Electronegativity, "F2", "Không có"),
                Value3 = CompareMode == 3 ? FormatValue(Element3?.Electronegativity, "F2", "Không có") : null,
                Category = "Chemical"
            });
            _allComparisonData.Add(new ComparisonItem { 
                Property = "Trạng thái oxy hóa", 
                Value1 = NormalizeText(Element1.OxidationStates ?? "Chưa xác định"), 
                Value2 = NormalizeText(Element2.OxidationStates ?? "Chưa xác định"),
                Value3 = CompareMode == 3 ? NormalizeText(Element3?.OxidationStates ?? "Chưa xác định") : null,
                Category = "Chemical"
            });
            _allComparisonData.Add(new ComparisonItem { 
                Property = "Phân lớp", 
                Value1 = NormalizeText(Element1.Subshell ?? "Chưa xác định"), 
                Value2 = NormalizeText(Element2.Subshell ?? "Chưa xác định"),
                Value3 = CompareMode == 3 ? NormalizeText(Element3?.Subshell ?? "Chưa xác định") : null,
                Category = "Chemical"
            });

            // ===== TÍNH CHẤT NGUYÊN TỬ (ATOMIC) =====
            _allComparisonData.Add(new ComparisonItem { 
                Property = "Bán kính nguyên tử (pm)", 
                Value1 = FormatValue(Element1.AtomicRadius, "F0", "Chưa xác định"),
                Value2 = FormatValue(Element2.AtomicRadius, "F0", "Chưa xác định"),
                Value3 = CompareMode == 3 ? FormatValue(Element3?.AtomicRadius, "F0", "Chưa xác định") : null,
                Category = "Atomic"
            });
            _allComparisonData.Add(new ComparisonItem { 
                Property = "Bán kính cộng hóa trị (pm)", 
                Value1 = FormatValue(Element1.CovalentRadius, "F0", "Chưa xác định"),
                Value2 = FormatValue(Element2.CovalentRadius, "F0", "Chưa xác định"),
                Value3 = CompareMode == 3 ? FormatValue(Element3?.CovalentRadius, "F0", "Chưa xác định") : null,
                Category = "Atomic"
            });
            _allComparisonData.Add(new ComparisonItem { 
                Property = "Năng lượng ion hóa (eV)", 
                Value1 = FormatValue(Element1.IonizationEnergy, "F4", "Chưa xác định"),
                Value2 = FormatValue(Element2.IonizationEnergy, "F4", "Chưa xác định"),
                Value3 = CompareMode == 3 ? FormatValue(Element3?.IonizationEnergy, "F4", "Chưa xác định") : null,
                Category = "Atomic"
            });
            _allComparisonData.Add(new ComparisonItem { 
                Property = "Thể tích nguyên tử (cm³/mol)", 
                Value1 = FormatValue(Element1.AtomicVolume, "F1", "Chưa xác định"),
                Value2 = FormatValue(Element2.AtomicVolume, "F1", "Chưa xác định"),
                Value3 = CompareMode == 3 ? FormatValue(Element3?.AtomicVolume, "F1", "Chưa xác định") : null,
                Category = "Atomic"
            });
            _allComparisonData.Add(new ComparisonItem { 
                Property = "Cấu hình electron", 
                Value1 = Element1.ElectronConfiguration, 
                Value2 = Element2.ElectronConfiguration,
                Value3 = CompareMode == 3 ? Element3?.ElectronConfiguration : null,
                Category = "Atomic"
            });
            _allComparisonData.Add(new ComparisonItem { 
                Property = "Số electron mỗi phân lớp", 
                Value1 = NormalizeText(Element1.ElectronsPerShell ?? "Chưa xác định"), 
                Value2 = NormalizeText(Element2.ElectronsPerShell ?? "Chưa xác định"),
                Value3 = CompareMode == 3 ? NormalizeText(Element3?.ElectronsPerShell ?? "Chưa xác định") : null,
                Category = "Atomic"
            });

            // Áp dụng bộ lọc
            FilterComparisonData();
        }

        private string FormatTemperature(double? kelvin)
        {
            if (!kelvin.HasValue) return "Chưa xác định";
            double celsius = kelvin.Value - 273.15;
            double fahrenheit = celsius * 9.0 / 5.0 + 32;
            return $"{kelvin.Value:F2} K | {celsius:F2} °C | {fahrenheit:F1} °F";
        }

        // Helper method to format object values (handles both numbers and strings)
        private string FormatValue(object value, string format = "F2", string defaultValue = "Chưa xác định")
        {
            if (value == null) return defaultValue;
            
            if (value is double d)
                return d.ToString(format);
            
            if (value is int i)
                return i.ToString();
            
            if (value is string s)
            {
                // Try parse string as double
                if (double.TryParse(s.Replace(" kJ/mol", "").Replace(" pm", "").Replace(" W/m·K", "").Replace(" g/cm³", "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double parsed))
                    return parsed.ToString(format);
                return s;
            }
            
            // Try parse as double
            if (double.TryParse(value.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double result))
                return result.ToString(format);
            
            return value.ToString();
        }

        private void FilterComparisonData()
        {
            ComparisonData.Clear();
            
            foreach (var item in _allComparisonData)
            {
                if (item.Category == SelectedTab)
                {
                    ComparisonData.Add(item);
                }
            }
        }

        // Hàm normalize để xử lý giá trị null/empty
        private string NormalizeText(string value)
        {
            return string.IsNullOrEmpty(value) ? "Chưa xác định" : value;
        }

        private string NormalizePhysicalState(string value)
        {
            return NormalizeText(value);
        }

        private void SwapElements(object parameter)
        {
            // Đổi chỗ hai nguyên tố
            var temp = Element1;
            Element1 = Element2;
            Element2 = temp;
        }

        private void ExportPdf(object parameter)
        {
            try
            {
                var compareWindow = Application.Current.Windows.OfType<Views.CompareWindow>().FirstOrDefault();
                if (compareWindow != null)
                {
                    System.Windows.Controls.PrintDialog printDialog = new System.Windows.Controls.PrintDialog();
                    if (printDialog.ShowDialog() == true)
                    {
                        printDialog.PrintVisual(compareWindow, "So sánh nguyên tố hóa học");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi in hoặc xuất file PDF: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseWindow(object parameter)
        {
            Application.Current.Windows.OfType<Views.CompareWindow>().FirstOrDefault()?.Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ComparisonItem
    {
        public string Property { get; set; }
        public string Value1 { get; set; }
        public string Value2 { get; set; }
        public string Value3 { get; set; } // For 3-element comparison
        public string Category { get; set; } // General, Physical, Chemical, Atomic
    }
}
