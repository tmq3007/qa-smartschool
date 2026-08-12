using System.Collections.Generic;
using System.Globalization;

namespace QASmartTouch.PeriodicTable.Models
{
    /// <summary>
    /// Model đầy đủ cho chi tiết nguyên tố hóa học
    /// </summary>
    public class ElementDetail
    {
        // ========== THÔNG TIN CƠ BẢN ==========
        public int AtomicNumber { get; set; }               // Số nguyên tử
        public string Symbol { get; set; }                  // Ký hiệu (Fe)
        public string Name { get; set; }                    // Tên tiếng Việt (Sắt)
        public string NameEn { get; set; }                  // Tên tiếng Anh (Iron)
        public double AtomicMass { get; set; }              // Khối lượng nguyên tử
        public string Category { get; set; }                // Phân loại (Kim loại chuyển tiếp)
        public int Period { get; set; }                     // Chu kỳ
        public int Group { get; set; }                      // Nhóm
        public string Block { get; set; }                   // Phân lớp (s, p, d, f)
        public string ElectronConfig { get; set; }          // Cấu hình electron
        public string ElectronConfigShort { get; set; }     // Cấu hình electron rút gọn
        public string FullElectronConfig { get; set; }      // Cấu hình electron đầy đủ (dùng trong Lanthanides/Actinides)
        public string Description { get; set; }             // Mô tả ngắn
        public string NaturalStateInfo { get; set; }        // Thông tin trạng thái tự nhiên và đồng vị (cho Tab Tổng quan)
        public string Abundance { get; set; }               // Độ phổ biến chung ("Hiếm", "Phổ biến"...)

        // ========== THUỘC TÍNH VẬT LÝ ==========
        public object Density { get; set; }                 // Khối lượng riêng (có thể là số hoặc chuỗi: 7.31, "7.31 g/cm³", "Chưa xác định")
        public string DensityUnit { get; set; }             // Đơn vị khối lượng riêng (g/cm³)
        public string DensityComparisonPrefix { get; set; } // Ký tự so sánh (~, >, <, ≈)
        public object DensityMin { get; set; }              // Giá trị nhỏ nhất (cho Status="Range", VD: 7.1)
        public object DensityMax { get; set; }              // Giá trị lớn nhất (cho Status="Range", VD: 7.3)
        public string DensityStatus { get; set; }           // Trạng thái dữ liệu ("Unknown", "Range")
        public object MeltingPoint { get; set; }            // Nhiệt độ nóng chảy (có thể là số hoặc chuỗi: 156.60, "156.60°C", "Chưa xác định")
        public string MeltingPointUnit { get; set; }        // Đơn vị nhiệt độ nóng chảy (°C)
        public string MeltingPointComparisonPrefix { get; set; } // Ký tự so sánh (~, >, <, ≈)
        public object BoilingPoint { get; set; }            // Nhiệt độ sôi (có thể là số hoặc chuỗi)
        public string BoilingPointUnit { get; set; }        // Đơn vị nhiệt độ sôi (°C)
        public string BoilingPointComparisonPrefix { get; set; } // Ký tự so sánh (~, >, <, ≈)
        public string BoilingPointStatus { get; set; }      // Trạng thái dữ liệu ("Unknown", "Numeric")
        public string PhaseAtSTP { get; set; }              // Trạng thái ở điều kiện chuẩn (Rắn/Lỏng/Khí)
        public string Color { get; set; }                   // Màu sắc
        public string Appearance { get; set; }              // Vẻ ngoài
        public object ThermalConductivity { get; set; }     // Độ dẫn nhiệt (có thể là số hoặc chuỗi: 81, "81 W/m·K", "Không rõ")
        public string ThermalConductivityUnit { get; set; } // Đơn vị độ dẫn nhiệt (W/m·K)
        public string ThermalConductivityStatus { get; set; } // Trạng thái dữ liệu ("Unknown", "Numeric")
        public double? ElectricalResistivity { get; set; }  // Điện trở suất (nΩ·m)
        public string MagneticOrdering { get; set; }        // Tính từ (Sắt từ, Thuận từ...)
        public string MagneticType { get; set; }            // Loại từ tính (dùng trong Lanthanides/Actinides, tương tự MagneticOrdering)
        public double? HeatCapacity { get; set; }           // Nhiệt dung riêng (J/(mol·K))
        public object SpecificHeatCapacity { get; set; }    // Nhiệt dung riêng dạng text (dùng trong Lanthanides/Actinides - LEGACY)
        public string SpecificHeatCapacityUnit { get; set; } // Đơn vị nhiệt dung riêng (J/g·K) - LEGACY
        public string SpecificHeatCapacityStatus { get; set; } // Trạng thái dữ liệu ("Unknown", "Numeric") - LEGACY
        public object SpecificHeat { get; set; }            // Nhiệt dung riêng (có thể là số hoặc null: 20.79, null)
        public string SpecificHeatUnit { get; set; }        // Đơn vị nhiệt dung riêng (J/mol·K)
        public string SpecificHeatComparisonPrefix { get; set; } // Ký tự so sánh (~, >, <, ≈)
        public string SpecificHeatStatus { get; set; }      // Trạng thái dữ liệu ("Unknown", "Numeric")
        public string Magnetism { get; set; }               // Tính từ (Nghịch từ, Thuận từ, Sắt từ...)
        public string CrystalStructure { get; set; }        // Cấu trúc tinh thể
        public List<PhysicalProperty> SpecialPhysicalProperties { get; set; } // Tính chất vật lý đặc biệt

        // ========== THUỘC TÍNH HÓA HỌC ==========
        public object Electronegativity { get; set; }       // Độ âm điện (có thể là số hoặc chuỗi: 2.2, "~1.3–1.4", "~1.3 (ước tính)")
        public string ElectronegativityComparisonPrefix { get; set; } // Ký tự so sánh (~, >, <, ≈)
        public List<double> IonizationEnergies { get; set; } // Năng lượng ion hóa (kJ/mol) - dạng array
        public object IonizationEnergy { get; set; }        // Năng lượng ion hóa (dạng text hoặc số, dùng trong Lanthanides/Actinides)
        public string IonizationEnergyUnit { get; set; }    // Đơn vị năng lượng ion hóa (kJ/mol)
        public object AtomicRadius { get; set; }            // Bán kính nguyên tử (có thể là số hoặc chuỗi: 167, "167 pm", "Chưa xác định")
        public string AtomicRadiusUnit { get; set; }       // Đơn vị bán kính nguyên tử (pm)
        public string AtomicRadiusComparisonPrefix { get; set; } // Ký tự so sánh (~, >, <, ≈)
        public object CovalentRadius { get; set; }          // Bán kính cộng hóa trị (có thể là số hoặc chuỗi)
        public string CovalentRadiusUnit { get; set; }     // Đơn vị bán kính cộng hóa trị (pm)
        public string CovalentRadiusComparisonPrefix { get; set; } // Ký tự so sánh (~, >, <, ≈)
        public object OxidationStates { get; set; }         // Số oxi hóa (có thể là string hoặc array)
        public object ElectronAffinity { get; set; }        // Ái lực electron (có thể là số hoặc chuỗi: 72.8, "28 kJ/mol", "Rất thấp")
        public string ElectronAffinityUnit { get; set; }   // Đơn vị ái lực electron (kJ/mol)
        public string Valence { get; set; }                 // Hóa trị (có thể nhiều giá trị: "3, 5")
        public List<string> ChemicalHighlights { get; set; } // Đặc điểm nổi bật (cho Tab Hóa học)
        public object TypicalReactions { get; set; }         // Các phản ứng điển hình (có thể là string hoặc List<string>)

        // ========== CẤU TRÚC ELECTRON ==========
        public List<int> ElectronShells { get; set; }       // Phân bố electron theo lớp [2, 8, 14, 2]
        public string OrbitalDiagram { get; set; }          // Sơ đồ orbital (text representation)
        public string GroundState { get; set; }             // Trạng thái cơ bản
        public string ElectronBehaviorDescription { get; set; } // Mô tả hành vi hóa học của electron (cho Tab Electron)
        public string ElectronBehavior { get; set; }        // Alias cho ElectronBehaviorDescription (dùng trong JSON Lanthanides/Actinides)

        // ========== ĐỒNG VỊ ==========
        public object Isotopes { get; set; }                // Danh sách đồng vị (có thể là string hoặc List<Isotope>)
        private bool _isRadioactive;
        public bool IsRadioactive
        {
            get => _isRadioactive || AtomicNumber >= 84;
            set => _isRadioactive = value;
        }

        // ========== ỨNG DỤNG ==========
        public List<ElementApplication> Applications { get; set; }      // Ứng dụng thực tế
        public List<ApplicationCategory> ApplicationsByCategory { get; set; } // Ứng dụng phân loại theo ngành (Proposal 9.A)
        public string MainApplication { get; set; }         // Ứng dụng chính
        public double? AnnualProduction { get; set; }       // Sản lượng hàng năm (tấn)
        public GlobalProductionStats ProductionStats { get; set; } // Thống kê sản xuất toàn cầu (Proposal 9.B)
        public List<string> FutureTrends { get; set; }      // Xu hướng tương lai (Proposal 9.C)
        public string ImportanceTitle { get; set; }         // Tiêu đề tầm quan trọng (VD: "Hydro - Năng lượng tương lai")
        public string ImportanceDescription { get; set; }   // Mô tả tầm quan trọng của nguyên tố

        // ========== LỊCH SỬ ==========
        public string DiscoveryYear { get; set; }           // Năm phát hiện (có thể là năm hoặc mô tả: "1766", "Cổ đại")
        public string Discoverer { get; set; }              // Người phát hiện
        public string Etymology { get; set; }               // Nguồn gốc tên
        public object HistoryEvents { get; set; }           // Timeline các sự kiện lịch sử (có thể là List<string> hoặc List<HistoryEvent>)

        // ========== AN TOÀN ==========
        public string SafetyHazards { get; set; }           // Nguy hiểm
        public string SafetyPrecautions { get; set; }       // Biện pháp phòng ngừa

        // ========== LIÊN QUAN ==========
        public List<RelatedElementGroup> RelatedElementGroups { get; set; } // Nhóm nguyên tố liên quan
        public List<ComparisonNote> ComparisonNotes { get; set; }           // Các điểm so sánh với nguyên tố khác
        public List<string> RelatedElementSymbols { get; set; }            // Danh sách ký hiệu nguyên tố liên quan (Proposal 10.A)
        public List<ComparisonTableRow> ComparisonTableData { get; set; }  // Dữ liệu bảng so sánh (Proposal 10.B)
        public List<string> TrendExplanations { get; set; }                // Giải thích xu hướng (Proposal 10.C)

        // Electronegativity comparisons for visualization (Proposal 8.C)
        public List<ElectronegativityComparison> ElectronegativityComparisons { get; set; }
        // ========== HIỂN THỊ ==========
        public string CategoryColor { get; set; }           // Màu theo phân loại (binding)
        public string HexColor { get; set; }                // Màu hex cho element box

        // ========== COMPUTED PROPERTIES FOR DISPLAY ==========
        public string MeltingPointDisplay
        {
            get
            {
                System.Diagnostics.Debug.WriteLine($"[MeltingPointDisplay] MeltingPoint={MeltingPoint}, Unit='{MeltingPointUnit}', IsNull={string.IsNullOrEmpty(MeltingPointUnit)}");
                
                if (MeltingPoint == null) return "Chưa xác định";
                
                // Tự động bổ sung đơn vị °C nếu thiếu
                string unit = string.IsNullOrEmpty(MeltingPointUnit) ? "°C" : MeltingPointUnit;
                
                // Hỗ trợ cả double, int, long, decimal (JSON deserialize có thể tạo long)
                if (MeltingPoint is double d)
                    return $"{d:F1} {unit}";
                if (MeltingPoint is int i)
                    return $"{i} {unit}";
                if (MeltingPoint is long l)
                    return $"{l} {unit}";
                if (MeltingPoint is decimal dec)
                    return $"{dec:F1} {unit}";
                    
                return $"{MeltingPoint} {unit}";
            }
        }

        public string BoilingPointDisplay
        {
            get
            {
                if (BoilingPoint == null) return "Chưa xác định";
                
                // Tự động bổ sung đơn vị °C nếu thiếu
                string unit = string.IsNullOrEmpty(BoilingPointUnit) ? "°C" : BoilingPointUnit;
                
                // Hỗ trợ cả double, int, long, decimal (JSON deserialize có thể tạo long)
                if (BoilingPoint is double d)
                    return $"{d:F1} {unit}";
                if (BoilingPoint is int i)
                    return $"{i} {unit}";
                if (BoilingPoint is long l)
                    return $"{l} {unit}";
                if (BoilingPoint is decimal dec)
                    return $"{dec:F1} {unit}";
                    
                return $"{BoilingPoint} {unit}";
            }
        }

        public string DensityDisplay
        {
            get
            {
                if (Density == null) return "Chưa xác định";
                
                // Tự động thêm ³ nếu thiếu hoặc mặc định g/cm³
                string unit = DensityUnit;
                if (string.IsNullOrEmpty(unit))
                    unit = "g/cm³";
                else if (unit == "g/cm")
                    unit = "g/cm³";
                
                // Hỗ trợ cả double, int, decimal - dùng F2 (2 chữ số thập phân) cho đồng bộ khoa học
                if (Density is double d)
                    return $"{d:F2} {unit}";
                if (Density is int i)
                    return $"{i} {unit}";
                if (Density is decimal dec)
                    return $"{dec:F2} {unit}";
                    
                return Density.ToString();
            }
        }

        // Constructor
        public ElementDetail()
        {
            IonizationEnergies = new List<double>();
            ElectronShells = new List<int>();
            Isotopes = new List<Isotope>();
            Applications = new List<ElementApplication>();
            ApplicationsByCategory = new List<ApplicationCategory>();
            FutureTrends = new List<string>();
            SpecialPhysicalProperties = new List<PhysicalProperty>();
            RelatedElementGroups = new List<RelatedElementGroup>();
            ComparisonNotes = new List<ComparisonNote>();
            RelatedElementSymbols = new List<string>();
            ComparisonTableData = new List<ComparisonTableRow>();
            TrendExplanations = new List<string>();
            ChemicalHighlights = new List<string>();
            TypicalReactions = new List<string>();
            ElectronegativityComparisons = new List<ElectronegativityComparison>();
        }
    }

    /// <summary>
    /// Model cho ứng dụng của nguyên tố
    /// </summary>
    public class ElementApplication
    {
        public string Icon { get; set; }                // Icon emoji (🚀, ⚡, 🏭...)
        public string Name { get; set; }                // Tên ứng dụng
        public string Description { get; set; }         // Mô tả chi tiết ứng dụng
    }

    /// <summary>
    /// Nhóm ứng dụng theo danh mục/ngành (Proposal 9.A)
    /// </summary>
    public class ApplicationCategory
    {
        public string Icon { get; set; }                // Icon emoji (🏭, 🔬, ⚕️, 🏠...)
        public string CategoryName { get; set; }       // Tên danh mục (Công nghiệp, Khoa học, Y học, Đời sống)
        public List<ElementApplication> Applications { get; set; } // Danh sách ứng dụng trong danh mục

        public ApplicationCategory()
        {
            Applications = new List<ElementApplication>();
        }
    }

    /// <summary>
    /// Thống kê sản xuất toàn cầu (Proposal 9.B)
    /// </summary>
    public class GlobalProductionStats
    {
        public double AnnualProduction { get; set; }    // Sản lượng hàng năm (tấn)
        public string AnnualProductionUnit { get; set; } // Đơn vị (triệu tấn, nghìn tấn...)
        public List<TopProducer> TopProducers { get; set; } // Quốc gia sản xuất lớn nhất
        public string MarketPrice { get; set; }         // Giá thị trường ($XXX/kg)

        public GlobalProductionStats()
        {
            TopProducers = new List<TopProducer>();
        }
    }

    /// <summary>
    /// Quốc gia sản xuất hàng đầu
    /// </summary>
    public class TopProducer
    {
        public string Country { get; set; }             // Tên quốc gia
        public double Percentage { get; set; }          // Tỷ lệ phần trăm sản lượng
    }

    /// <summary>
    /// Model cho tính chất vật lý đặc biệt
    /// </summary>
    public class PhysicalProperty
    {
        public string Icon { get; set; }                // Icon emoji (⚖️, 🔥, 💧...)
        public string Title { get; set; }               // Tiêu đề tính chất
        public string Description { get; set; }         // Mô tả chi tiết tính chất
    }

    /// <summary>
    /// Model cho đồng vị
    /// </summary>
    public class Isotope
    {
        public string Name { get; set; }                // Tên đồng vị (Protium, Deuterium, Tritium)
        public int MassNumber { get; set; }             // Số khối
        public string Symbol { get; set; }              // Ký hiệu (¹H, ²H, ³H)
        public int Neutrons { get; set; }               // Số neutron
        public double? Abundance { get; set; }          // Độ phổ biến (%)
        public string AbundanceText { get; set; }       // Độ phổ biến dạng text (99.985%, Vết)
        public bool IsStable { get; set; }              // Bền hay không
        public string HalfLife { get; set; }            // Chu kỳ bán rã
        public string Description { get; set; }         // Mô tả chi tiết đồng vị
        public string Application { get; set; }         // Ứng dụng đặc trưng của đồng vị (ví dụ: Nước nặng D2O)
    }

    /// <summary>
    /// So sánh độ âm điện cho visualization
    /// </summary>
    public class ElectronegativityComparison
    {
        public string Symbol { get; set; }              // Ký hiệu nguyên tố (F, H, Li)
        public string Name { get; set; }                // Tên đầy đủ (Fluorine)
        public object Value { get; set; }               // Giá trị độ âm điện (Pauling) - có thể là double hoặc string "—"
        
        // Helper property để lấy giá trị double
        public double? ValueAsDouble 
        { 
            get 
            {
                if (Value == null) return null;
                if (Value is double d) return d;
                if (Value is string s && double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double result)) return result;
                return null;
            }
        }
    }

    /// <summary>
    /// Model cho nhóm nguyên tố liên quan
    /// </summary>
    public class RelatedElementGroup
    {
        public string GroupName { get; set; }           // Tên nhóm (Cùng nhóm, Cùng chu kỳ, Phi kim phổ biến)
        public List<RelatedElement> Elements { get; set; } // Danh sách nguyên tố trong nhóm

        public RelatedElementGroup()
        {
            Elements = new List<RelatedElement>();
        }
    }

    /// <summary>
    /// Model cho nguyên tố liên quan
    /// </summary>
    public class RelatedElement
    {
        public string Symbol { get; set; }              // Ký hiệu (Li, Na, K...)
        public string Name { get; set; }                // Tên (Liti, Natri, Kali...)
    }

    /// <summary>
    /// Model cho ghi chú so sánh với nguyên tố khác
    /// </summary>
    public class ComparisonNote
    {
        public string Title { get; set; }               // Tiêu đề so sánh (So với Heli (He))
        public string Description { get; set; }         // Mô tả chi tiết so sánh
    }

    /// <summary>
    /// Dữ liệu một dòng trong bảng so sánh (Proposal 10.B)
    /// </summary>
    public class ComparisonTableRow
    {
        public string PropertyName { get; set; }        // Tên thuộc tính (Điểm nóng chảy, Độ âm điện, Bán kính)
        public Dictionary<string, string> Values { get; set; } // Giá trị cho từng nguyên tố (Symbol -> Value)

        public ComparisonTableRow()
        {
            Values = new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// Model cho sự kiện lịch sử
    /// </summary>
    public class HistoryEvent
    {
        public string Year { get; set; }                // Năm hoặc thời kỳ (1671, 1766, 1950s, Thế kỷ 21)
        public string Title { get; set; }               // Tiêu đề sự kiện (tên người/tổ chức)
        public string Description { get; set; }         // Mô tả chi tiết sự kiện
    }
}


