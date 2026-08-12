using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace QASmartTouch.PeriodicTable.Models
{
    /// <summary>
    /// Quản lý việc load dữ liệu nguyên tố từ nhiều file JSON
    /// </summary>
    public class ElementDataManager
    {
        private static ElementDataManager _instance;
        private Dictionary<string, List<ElementDetail>> _cachedData;
        private List<ElementDetail> _allElements;
        private Dictionary<int, string> _elementFileMap; // Map atomic number to file name
        private readonly object _lock = new object();

        // Danh sách các file dữ liệu (split files used by the app)
        private readonly string[] _dataFiles = new[]
        {
            "elements_main_groups.json",
            "elements_transition_metals.json",
            "elements_lanthanides.json",
            "elements_actinides.json"
        };

        public static ElementDataManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ElementDataManager();
                }
                return _instance;
            }
        }

        private ElementDataManager()
        {
            _cachedData = new Dictionary<string, List<ElementDetail>>();
            _allElements = null;
            _elementFileMap = new Dictionary<int, string>();
            InitializeElementMap();
        }

        /// <summary>
        /// Khởi tạo map nguyên tố -> file JSON
        /// Main groups: 1-2, 5-10, 13-18, 31-36, 37-38, 49-54, 55-56, 81-86, 87-88, 113-118
        /// Transition metals: 21-30, 39-48, 72-80, 104-112
        /// Lanthanides: 57-71
        /// Actinides: 89-103
        /// </summary>
        private void InitializeElementMap()
        {
            // Main groups elements
            AddRangeToMap(1, 2, "elements_main_groups.json");
            AddRangeToMap(3, 4, "elements_main_groups.json"); // Li, Be
            AddRangeToMap(5, 10, "elements_main_groups.json");
            AddRangeToMap(11, 12, "elements_main_groups.json"); // Na, Mg
            AddRangeToMap(13, 18, "elements_main_groups.json");
            AddRangeToMap(19, 20, "elements_main_groups.json"); // K, Ca
            AddRangeToMap(31, 36, "elements_main_groups.json");
            AddRangeToMap(37, 38, "elements_main_groups.json");
            AddRangeToMap(49, 54, "elements_main_groups.json");
            AddRangeToMap(55, 56, "elements_main_groups.json");
            AddRangeToMap(81, 86, "elements_main_groups.json");
            AddRangeToMap(87, 88, "elements_main_groups.json");
            AddRangeToMap(114, 118, "elements_main_groups.json");

            // Transition metals
            AddRangeToMap(21, 30, "elements_transition_metals.json");
            AddRangeToMap(39, 48, "elements_transition_metals.json");
            AddRangeToMap(72, 80, "elements_transition_metals.json");
            AddRangeToMap(104, 113, "elements_transition_metals.json");

            // Lanthanides (57-71)
            AddRangeToMap(57, 71, "elements_lanthanides.json");

            // Actinides (89-103)
            AddRangeToMap(89, 103, "elements_actinides.json");
        }

        /// <summary>
        /// Helper method to add a range of atomic numbers to the file map
        /// </summary>
        private void AddRangeToMap(int start, int end, string fileName)
        {
            for (int z = start; z <= end; z++)
            {
                _elementFileMap[z] = fileName;
            }
        }

        /// <summary>
        /// Helper method để loại bỏ UTF-8 BOM nếu có
        /// </summary>
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
            
            // Loại bỏ các ký tự zero-width và BOM ẩn khác
            content = content.Replace("\uFEFF", ""); // Zero-width no-break space (BOM)
            content = content.Replace("\u200B", ""); // Zero-width space
            content = content.Replace("\u200C", ""); // Zero-width non-joiner
            content = content.Replace("\u200D", ""); // Zero-width joiner
            
            return content;
        }

        /// <summary>
        /// Load dữ liệu từ một file cụ thể
        /// </summary>
        private List<ElementDetail> LoadFromFile(string fileName)
        {
            try
            {
                string jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PeriodicTable", "Data", fileName);

                if (!File.Exists(jsonPath))
                {
                    throw new FileNotFoundException($"File không tồn tại: {jsonPath}");
                }

                string jsonContent = ReadJsonFileWithoutBom(jsonPath);
                
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    throw new Exception($"File {fileName} rỗng hoặc không đọc được");
                }
                
                jsonContent = jsonContent.Trim();

                var settings = new JsonSerializerSettings
                {
                    // Tolerant parsing mode - cho phép parse flexible
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Populate,
                    TypeNameHandling = TypeNameHandling.None, // Không cố gắng parse type cụ thể
                    Error = (sender, args) =>
                    {
                        // Log error nhưng tiếp tục parse - quan trọng!
                        string logMsg = $"[ElementDataManager] JSON parsing error in {fileName}:\n  Path: {args.ErrorContext.Path}\n  Error: {args.ErrorContext.Error.Message}\n";
                        System.Diagnostics.Debug.WriteLine(logMsg);
                        
                        // Also log to file for debugging
                        try
                        {
                            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "json_errors.log");
                            File.AppendAllText(logPath, logMsg + "\n");
                        }
                        catch { /* Ignore log file errors */ }
                        
                        args.ErrorContext.Handled = true; // Bỏ qua lỗi và tiếp tục
                    }
                };

                var elements = JsonConvert.DeserializeObject<List<ElementDetail>>(jsonContent, settings);

                if (elements != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[ElementDataManager] Successfully loaded {elements.Count} elements from {fileName}");
                }

                return elements ?? new List<ElementDetail>();
            }
            catch (JsonException jsonEx)
            {
                // Chi tiết lỗi JSON
                throw new Exception($"Lỗi JSON trong file {fileName}: {jsonEx.Message}", jsonEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi load file {fileName}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Load dữ liệu theo category với cache
        /// </summary>
        public List<ElementDetail> LoadElementsByCategory(string categoryFile)
        {
            lock (_lock)
            {
                if (_cachedData.ContainsKey(categoryFile))
                {
                    return _cachedData[categoryFile];
                }

                var data = LoadFromFile(categoryFile);
                _cachedData[categoryFile] = data;
                return data;
            }
        }

        /// <summary>
        /// Load tất cả nguyên tố từ file
        /// </summary>
        public List<ElementDetail> LoadAllElements()
        {
            lock (_lock)
            {
                if (_allElements != null)
                {
                    return _allElements;
                }

                _allElements = new List<ElementDetail>();

                foreach (var file in _dataFiles)
                {
                    var elements = LoadElementsByCategory(file);
                    _allElements.AddRange(elements);
                }

                // Sắp xếp theo số hiệu nguyên tử
                _allElements = _allElements.OrderBy(e => e.AtomicNumber).ToList();

                return _allElements;
            }
        }

        /// <summary>
        /// Tìm nguyên tố theo số hiệu nguyên tử - Sử dụng map để load chỉ file cần thiết
        /// </summary>
        public ElementDetail GetElementByAtomicNumber(int atomicNumber)
        {
            lock (_lock)
            {
                // Kiểm tra map xem nguyên tố nằm ở file nào
                if (_elementFileMap.ContainsKey(atomicNumber))
                {
                    string fileName = _elementFileMap[atomicNumber];
                    var elements = LoadElementsByCategory(fileName);
                    return elements.FirstOrDefault(e => e.AtomicNumber == atomicNumber);
                }

                // Fallback: tìm trong tất cả elements đã load
                var allElements = LoadAllElements();
                return allElements.FirstOrDefault(e => e.AtomicNumber == atomicNumber);
            }
        }

        /// <summary>
        /// Tìm nguyên tố theo ký hiệu - Tìm trong tất cả file
        /// </summary>
        public ElementDetail GetElementBySymbol(string symbol)
        {
            if (string.IsNullOrEmpty(symbol))
                return null;

            lock (_lock)
            {
                // Tìm trong từng file đã cache
                foreach (var kvp in _cachedData)
                {
                    var element = kvp.Value.FirstOrDefault(e => 
                        e.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase)
                    );
                    if (element != null)
                        return element;
                }

                // Nếu chưa tìm thấy, load tất cả và tìm
                var allElements = LoadAllElements();
                return allElements.FirstOrDefault(e => 
                    e.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase)
                );
            }
        }

        /// <summary>
        /// Xóa cache để reload dữ liệu
        /// </summary>
        public void ClearCache()
        {
            lock (_lock)
            {
                _cachedData.Clear();
                _allElements = null;
            }
        }

        /// <summary>
        /// Lấy tổng số nguyên tố đã load
        /// </summary>
        public int GetTotalElementsCount()
        {
            return LoadAllElements().Count;
        }

        /// <summary>
        /// Kiểm tra xem một nguyên tố có tồn tại không
        /// </summary>
        public bool ElementExists(int atomicNumber)
        {
            return GetElementByAtomicNumber(atomicNumber) != null;
        }
    }
}

