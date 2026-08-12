using System.ComponentModel;

namespace QASmartTouch.PeriodicTable.Models
{
    public class Element : INotifyPropertyChanged
    {
        private string _color;
        private bool _isHighlighted;
        private double _opacityValue = 1.0;

        public int AtomicNumber { get; set; }
        public string Symbol { get; set; }
        public string Name { get; set; }
        public string NameVietnamese { get; set; }
        public string NameEn { get; set; }
        public double AtomicMass { get; set; }
        public int Period { get; set; }
        public int Group { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }
        public string Category { get; set; }
        public string ElectronConfiguration { get; set; }
        public string ElectronConfig { get; set; }             // Cấu hình electron (1s² 2s²)
        public string Block { get; set; }
        public double? Density { get; set; }
        
        private string _densityUnit;
        public string DensityUnit                              // Đơn vị mật độ (g/cm³)
        {
            get
            {
                // Tự động thêm ³ nếu thiếu
                if (string.IsNullOrEmpty(_densityUnit))
                    return "g/cm³";
                if (_densityUnit == "g/cm")
                    return "g/cm³";
                return _densityUnit;
            }
            set { _densityUnit = value; }
        }
        
        public double? MeltingPoint { get; set; }
        public string MeltingPointUnit { get; set; }           // Đơn vị nhiệt độ nóng chảy (°C)
        public double? BoilingPoint { get; set; }
        public string BoilingPointUnit { get; set; }           // Đơn vị nhiệt độ sôi (°C)
        public object Electronegativity { get; set; }          // Độ âm điện (có thể là số hoặc chuỗi)

        // Tính chất chung bổ sung
        public string ColorDescription { get; set; }           // "Xám", "Bạc", "Vàng"
        private bool _isRadioactive;
        public bool IsRadioactive
        {
            get => _isRadioactive || AtomicNumber >= 84;
            set => _isRadioactive = value;
        }
        public string CrystalStructure { get; set; }           // "Lập phương tâm khối"
        public string EarthCrustAbundance { get; set; }        // "6,3%"
        public string CosmicAbundance { get; set; }            // "0,11%"
        public string Subshell { get; set; }                   // "s", "p", "d", "f"
        
        // Tính chất vật lý bổ sung
        public string PhysicalState { get; set; }              // "Rắn", "Lỏng", "Khí"
        public double? HeatOfFusion { get; set; }              // kJ/mol
        public double? HeatOfVaporization { get; set; }        // kJ/mol
        public double? SpecificHeat { get; set; }              // J/g·K
        
        // Tính chất nguyên tử bổ sung
        public double? AtomicRadius { get; set; }              // pm (picometer)
        public double? CovalentRadius { get; set; }            // pm
        public double? IonizationEnergy { get; set; }          // eV
        public double? AtomicVolume { get; set; }              // cm³/mol
        public object ThermalConductivity { get; set; }        // Độ dẫn nhiệt (có thể là số hoặc chuỗi)
        public string OxidationStates { get; set; }            // "-2, -1, 1, 2, 3"
        public string ElectronsPerShell { get; set; }          // "2, 8, 14, 2"
        public object ElectronAffinity { get; set; }           // Ái lực electron (có thể là số hoặc chuỗi)

        // Property để hiển thị trong ComboBox
        public string DisplayName => $"{AtomicNumber} - {Symbol} - {NameVietnamese}";

        public string Color
        {
            get => _color;
            set
            {
                _color = value;
                OnPropertyChanged(nameof(Color));
            }
        }

        public bool IsHighlighted
        {
            get => _isHighlighted;
            set
            {
                _isHighlighted = value;
                OnPropertyChanged(nameof(IsHighlighted));
            }
        }

        public double OpacityValue
        {
            get => _opacityValue;
            set
            {
                _opacityValue = value;
                OnPropertyChanged(nameof(OpacityValue));
            }
        }

        // Thuộc tính để hiển thị thông tin đầy đủ
        public string DisplayInfo 
        { 
            get 
            { 
                if (!string.IsNullOrEmpty(_displayInfoOverride))
                    return _displayInfoOverride;
                return $"{Name} ({Symbol})\nSố hiệu: {AtomicNumber}\nKhối lượng: {AtomicMass}";
            }
            set
            {
                _displayInfoOverride = value;
                OnPropertyChanged(nameof(DisplayInfo));
            }
        }

        private string _displayInfoOverride;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Constructor
        public Element()
        {
            Color = "#FFFFFF"; // Màu mặc định
        }
    }
}
