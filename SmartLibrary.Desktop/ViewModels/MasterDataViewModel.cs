using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartLibrary.Desktop.Helpers;
using SmartLibrary.Desktop.Models;
using SmartLibrary.Desktop.Services;
using ClosedXML.Excel;

namespace SmartLibrary.Desktop.ViewModels
{
    public partial class MasterDataViewModel : ObservableObject, IActiveAwareViewModel
    {
        private readonly ApiService? _apiService;
        
        private readonly System.Collections.Generic.List<PublisherDto> _allPublishers = new();
        private readonly System.Collections.Generic.List<BookTypeDto> _allBookTypes = new();
        private readonly System.Collections.Generic.List<DepartmentDto> _allDepartments = new();

        [ObservableProperty] private string _publisherSearchText = "";
        [ObservableProperty] private string _bookTypeSearchText = "";
        [ObservableProperty] private string _departmentSearchText = "";

        // Publishers List
        public ObservableCollection<PublisherDto> Publishers { get; } = new()
        {
            new PublisherDto { Id = 1, Name = "NXB Giáo Dục Việt Nam", Address = "Hà Nội", Contact = "024.38220801" },
            new PublisherDto { Id = 2, Name = "NXB Trẻ", Address = "TP. Hồ Chí Minh", Contact = "028.38447825" },
            new PublisherDto { Id = 3, Name = "NXB Kim Đồng", Address = "Hà Nội", Contact = "024.39434546" }
        };

        // Book Types List
        public ObservableCollection<BookTypeDto> BookTypes { get; } = new()
        {
            new BookTypeDto { Id = 1, Name = "Sách chuyên đề", Description = "Tài liệu chuyên sâu phục vụ nghiên cứu" },
            new BookTypeDto { Id = 2, Name = "Sách tham khảo", Description = "Sách đọc thêm, mở rộng kiến thức" },
            new BookTypeDto { Id = 3, Name = "Giáo trình", Description = "Tài liệu giảng dạy chính khóa" }
        };

        // Departments List
        public ObservableCollection<DepartmentDto> Departments { get; } = new()
        {
            new DepartmentDto { Id = 1, Name = "Khoa Công nghệ thông tin", Code = "CNTT" },
            new DepartmentDto { Id = 2, Name = "Khoa Cơ bản", Code = "CB" },
            new DepartmentDto { Id = 3, Name = "Phòng Đào tạo", Code = "PDT" }
        };

        // Publisher Editing Properties
        [ObservableProperty]
        private PublisherDto? _selectedPublisher;

        [ObservableProperty]
        private string _publisherName = "";

        [ObservableProperty]
        private string _publisherAddress = "";

        [ObservableProperty]
        private string _publisherContact = "";

        [ObservableProperty]
        private string _publisherError = "";

        // BookType Editing Properties
        [ObservableProperty]
        private BookTypeDto? _selectedBookType;

        [ObservableProperty]
        private string _bookTypeName = "";

        [ObservableProperty]
        private string _bookTypeDescription = "";

        [ObservableProperty]
        private string _bookTypeError = "";

        // Department Editing Properties
        [ObservableProperty]
        private DepartmentDto? _selectedDepartment;

        [ObservableProperty]
        private string _departmentName = "";

        [ObservableProperty]
        private string _departmentCode = "";

        [ObservableProperty]
        private string _departmentError = "";

        [ObservableProperty]
        private int _pendingProposalsCount = 0;

        [ObservableProperty]
        private bool _hasPendingProposals = false;

        [ObservableProperty]
        private bool _isBusy;

        // Commands
        public ICommand SavePublisherCommand { get; }
        public ICommand DeletePublisherCommand { get; }
        public ICommand ClearPublisherCommand { get; }

        public ICommand SaveBookTypeCommand { get; }
        public ICommand DeleteBookTypeCommand { get; }
        public ICommand ClearBookTypeCommand { get; }

        public ICommand SaveDepartmentCommand { get; }
        public ICommand DeleteDepartmentCommand { get; }
        public ICommand ClearDepartmentCommand { get; }

        public ICommand ImportPublishersExcelCommand { get; }
        public ICommand ExportPublishersExcelCommand { get; }
        public ICommand ImportBookTypesExcelCommand { get; }
        public ICommand ExportBookTypesExcelCommand { get; }
        public ICommand ImportDepartmentsExcelCommand { get; }
        public ICommand ExportDepartmentsExcelCommand { get; }
        public ICommand ExportPublishersTemplateCommand { get; }
        public ICommand ExportBookTypesTemplateCommand { get; }
        public ICommand ExportDepartmentsTemplateCommand { get; }

        public MasterDataViewModel(ApiService? apiService = null)
        {
            _apiService = apiService;
            SavePublisherCommand = new AsyncRelayCommand(SavePublisherAsync);
            DeletePublisherCommand = new AsyncRelayCommand(DeletePublisherAsync);
            ClearPublisherCommand = new RelayCommand(ClearPublisher);

            SaveBookTypeCommand = new AsyncRelayCommand(SaveBookTypeAsync);
            DeleteBookTypeCommand = new AsyncRelayCommand(DeleteBookTypeAsync);
            ClearBookTypeCommand = new RelayCommand(ClearBookType);

            SaveDepartmentCommand = new AsyncRelayCommand(SaveDepartmentAsync);
            DeleteDepartmentCommand = new AsyncRelayCommand(DeleteDepartmentAsync);
            ClearDepartmentCommand = new RelayCommand(ClearDepartment);

            ImportPublishersExcelCommand = new AsyncRelayCommand(ImportPublishersExcelAsync);
            ExportPublishersExcelCommand = new AsyncRelayCommand(ExportPublishersExcelAsync);
            ImportBookTypesExcelCommand = new AsyncRelayCommand(ImportBookTypesExcelAsync);
            ExportBookTypesExcelCommand = new AsyncRelayCommand(ExportBookTypesExcelAsync);
            ImportDepartmentsExcelCommand = new AsyncRelayCommand(ImportDepartmentsExcelAsync);
            ExportDepartmentsExcelCommand = new AsyncRelayCommand(ExportDepartmentsExcelAsync);
            ExportPublishersTemplateCommand = new AsyncRelayCommand(ExportPublishersTemplateAsync);
            ExportBookTypesTemplateCommand = new AsyncRelayCommand(ExportBookTypesTemplateAsync);
            ExportDepartmentsTemplateCommand = new AsyncRelayCommand(ExportDepartmentsTemplateAsync);

            // Populate master lists
            _allPublishers.AddRange(Publishers);
            _allBookTypes.AddRange(BookTypes);
            _allDepartments.AddRange(Departments);

            // Wire up property changes to populate forms
            this.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SelectedPublisher))
                {
                    if (SelectedPublisher != null)
                    {
                        PublisherName = SelectedPublisher.Name;
                        PublisherAddress = SelectedPublisher.Address;
                        PublisherContact = SelectedPublisher.Contact;
                        PublisherError = "";
                    }
                }
                else if (e.PropertyName == nameof(SelectedBookType))
                {
                    if (SelectedBookType != null)
                    {
                        BookTypeName = SelectedBookType.Name;
                        BookTypeDescription = SelectedBookType.Description;
                        BookTypeError = "";
                    }
                }
                else if (e.PropertyName == nameof(SelectedDepartment))
                {
                    if (SelectedDepartment != null)
                    {
                        DepartmentName = SelectedDepartment.Name;
                        DepartmentCode = SelectedDepartment.Code;
                        DepartmentError = "";
                    }
                }
                else if (e.PropertyName == nameof(PublisherSearchText))
                {
                    ApplyPublisherFilter();
                }
                else if (e.PropertyName == nameof(BookTypeSearchText))
                {
                    ApplyBookTypeFilter();
                }
                else if (e.PropertyName == nameof(DepartmentSearchText))
                {
                    ApplyDepartmentFilter();
                }
            };
        }

        public void Activate()
        {
            LoadPendingProposalsCount();
        }

        private void ApplyPublisherFilter()
        {
            Publishers.Clear();
            var query = _allPublishers.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(PublisherSearchText))
            {
                string kw = InputHelper.RemoveDiacritics(PublisherSearchText.Trim()).ToLower();
                query = query.Where(p => 
                    (p.Name != null && InputHelper.RemoveDiacritics(p.Name).ToLower().Contains(kw)) ||
                    (p.Address != null && InputHelper.RemoveDiacritics(p.Address).ToLower().Contains(kw)) ||
                    (p.Contact != null && InputHelper.RemoveDiacritics(p.Contact).ToLower().Contains(kw)));
            }
            foreach (var pub in query) Publishers.Add(pub);
        }

        private void ApplyBookTypeFilter()
        {
            BookTypes.Clear();
            var query = _allBookTypes.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(BookTypeSearchText))
            {
                string kw = InputHelper.RemoveDiacritics(BookTypeSearchText.Trim()).ToLower();
                query = query.Where(bt => 
                    (bt.Name != null && InputHelper.RemoveDiacritics(bt.Name).ToLower().Contains(kw)) ||
                    (bt.Description != null && InputHelper.RemoveDiacritics(bt.Description).ToLower().Contains(kw)));
            }
            foreach (var bt in query) BookTypes.Add(bt);
        }

        private void ApplyDepartmentFilter()
        {
            Departments.Clear();
            var query = _allDepartments.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(DepartmentSearchText))
            {
                string kw = InputHelper.RemoveDiacritics(DepartmentSearchText.Trim()).ToLower();
                query = query.Where(d => 
                    (d.Name != null && InputHelper.RemoveDiacritics(d.Name).ToLower().Contains(kw)) ||
                    (d.Code != null && InputHelper.RemoveDiacritics(d.Code).ToLower().Contains(kw)));
            }
            foreach (var dep in query) Departments.Add(dep);
        }

        // PUBLISHERS ACTIONS
        private async Task SavePublisherAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                PublisherError = "";
                if (string.IsNullOrWhiteSpace(PublisherName))
                {
                    PublisherError = "Tên nhà xuất bản không được để trống!";
                    return;
                }

                var cleanName = InputHelper.NormalizeInput(PublisherName);
                var cleanAddress = InputHelper.TrimInput(PublisherAddress);
                var cleanContact = InputHelper.TrimInput(PublisherContact);

                if (!InputHelper.ValidateLength(cleanName, 2, 150))
                {
                    PublisherError = "Tên NXB phải từ 2 đến 150 ký tự!";
                    return;
                }

                // Check duplicate
                var duplicate = _allPublishers.FirstOrDefault(p => p.Name.Equals(cleanName, StringComparison.OrdinalIgnoreCase) && p != SelectedPublisher);
                if (duplicate != null)
                {
                    PublisherError = "Tên nhà xuất bản đã tồn tại trong hệ thống!";
                    return;
                }

                if (SelectedPublisher == null)
                {
                    // Add
                    int newId = _allPublishers.Count > 0 ? _allPublishers.Max(p => p.Id) + 1 : 1;
                    var newPub = new PublisherDto { Id = newId, Name = cleanName, Address = cleanAddress, Contact = cleanContact };
                    _allPublishers.Add(newPub);
                    ApplyPublisherFilter();
                    await AuditLogService.WriteLogAsync("Thêm Danh mục", $"Thêm NXB mới '{cleanName}' thành công", true);
                }
                else
                {
                    // Update
                    string oldName = SelectedPublisher.Name;
                    SelectedPublisher.Name = cleanName;
                    SelectedPublisher.Address = cleanAddress;
                    SelectedPublisher.Contact = cleanContact;
                    
                    var indexAll = _allPublishers.IndexOf(SelectedPublisher);
                    if (indexAll >= 0)
                    {
                        _allPublishers[indexAll] = SelectedPublisher;
                    }
                    ApplyPublisherFilter();
                    
                    await AuditLogService.WriteLogAsync("Sửa Danh mục", $"Cập nhật NXB '{oldName}' thành '{cleanName}' thành công", true);
                }

                ClearPublisher();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task DeletePublisherAsync()
        {
            if (IsBusy) return;
            if (SelectedPublisher == null) return;
            IsBusy = true;
            try
            {
                string name = SelectedPublisher.Name;
                int id = SelectedPublisher.Id;

                var confirm = System.Windows.MessageBox.Show(
                    $"Bạn có chắc chắn muốn xóa nhà xuất bản '{name}' không?",
                    "Xác nhận xóa", System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (confirm != System.Windows.MessageBoxResult.Yes) return;

                // Kiểm tra FK qua API (ưu tiên)
                bool blocked = false;
                string blockReason = "";

                if (_apiService != null)
                {
                    try
                    {
                        var deps = await _apiService.GetAsync<DependencyCheckResult>(
                            $"/MasterData/CheckDependencies?type=publisher&id={id}");

                        if (deps != null && deps.HasDependencies)
                        {
                            blocked = true;
                            blockReason = $"Không thể xóa: NXB '{name}' đang được sử dụng bởi {deps.DependentCount} {deps.DependentType}.";
                        }
                    }
                    catch (Exception)
                    {
                        // API fail → fallback logic cũ
                        blocked = FallbackCheckDependency("publisher", name);
                        if (blocked)
                        {
                            blockReason = $"⚠️ Không thể xóa (kiểm tra offline): NXB '{name}' có thể đang được sử dụng. Kết nối mạng để xác nhận chính xác.";
                        }
                    }
                }
                else
                {
                    blocked = FallbackCheckDependency("publisher", name);
                    if (blocked) blockReason = $"NXB '{name}' đang có sách liên kết trong kho.";
                }

                if (blocked)
                {
                    System.Windows.MessageBox.Show(blockReason, "Cảnh báo bảo mật",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    await AuditLogService.WriteLogAsync("Xóa Danh mục",
                        $"Thử xóa NXB '{name}' bị chặn do ràng buộc", false);
                    return;
                }

                _allPublishers.Remove(SelectedPublisher);
                ApplyPublisherFilter();
                await AuditLogService.WriteLogAsync("Xóa Danh mục", $"Xóa NXB '{name}' thành công", true);
                ClearPublisher();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ClearPublisher()
        {
            SelectedPublisher = null;
            PublisherName = "";
            PublisherAddress = "";
            PublisherContact = "";
            PublisherError = "";
        }

        // BOOK TYPES ACTIONS
        private async Task SaveBookTypeAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                BookTypeError = "";
                if (string.IsNullOrWhiteSpace(BookTypeName))
                {
                    BookTypeError = "Tên thể loại không được để trống!";
                    return;
                }

                var cleanName = InputHelper.NormalizeInput(BookTypeName);
                var cleanDesc = InputHelper.TrimInput(BookTypeDescription);

                if (!InputHelper.ValidateLength(cleanName, 2, 150))
                {
                    BookTypeError = "Tên thể loại phải từ 2 đến 150 ký tự!";
                    return;
                }

                var duplicate = _allBookTypes.FirstOrDefault(t => t.Name.Equals(cleanName, StringComparison.OrdinalIgnoreCase) && t != SelectedBookType);
                if (duplicate != null)
                {
                    BookTypeError = "Tên thể loại sách đã tồn tại!";
                    return;
                }

                if (SelectedBookType == null)
                {
                    int newId = _allBookTypes.Count > 0 ? _allBookTypes.Max(t => t.Id) + 1 : 1;
                    var newType = new BookTypeDto { Id = newId, Name = cleanName, Description = cleanDesc };
                    _allBookTypes.Add(newType);
                    ApplyBookTypeFilter();
                    await AuditLogService.WriteLogAsync("Thêm Danh mục", $"Thêm thể loại sách '{cleanName}' thành công", true);
                }
                else
                {
                    string oldName = SelectedBookType.Name;
                    SelectedBookType.Name = cleanName;
                    SelectedBookType.Description = cleanDesc;
                    var indexAll = _allBookTypes.IndexOf(SelectedBookType);
                    if (indexAll >= 0)
                    {
                        _allBookTypes[indexAll] = SelectedBookType;
                    }
                    ApplyBookTypeFilter();
                    await AuditLogService.WriteLogAsync("Sửa Danh mục", $"Cập nhật thể loại sách '{oldName}' thành '{cleanName}' thành công", true);
                }

                ClearBookType();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task DeleteBookTypeAsync()
        {
            if (IsBusy) return;
            if (SelectedBookType == null) return;
            IsBusy = true;
            try
            {
                string name = SelectedBookType.Name;
                int id = SelectedBookType.Id;

                var confirm = System.Windows.MessageBox.Show(
                    $"Bạn có chắc chắn muốn xóa thể loại sách '{name}' không?",
                    "Xác nhận xóa", System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (confirm != System.Windows.MessageBoxResult.Yes) return;

                // Kiểm tra FK qua API
                bool blocked = false;
                string blockReason = "";

                if (_apiService != null)
                {
                    try
                    {
                        var deps = await _apiService.GetAsync<DependencyCheckResult>(
                            $"/MasterData/CheckDependencies?type=booktype&id={id}");

                        if (deps != null && deps.HasDependencies)
                        {
                            blocked = true;
                            blockReason = $"Không thể xóa: Thể loại '{name}' đang được sử dụng bởi {deps.DependentCount} {deps.DependentType}.";
                        }
                    }
                    catch (Exception)
                    {
                        blocked = FallbackCheckDependency("booktype", name);
                        if (blocked)
                        {
                            blockReason = $"⚠️ Không thể xóa (kiểm tra offline): Thể loại '{name}' có thể đang được sử dụng.";
                        }
                    }
                }
                else
                {
                    blocked = FallbackCheckDependency("booktype", name);
                    if (blocked) blockReason = $"Thể loại '{name}' đang có sách liên kết trong kho.";
                }

                if (blocked)
                {
                    System.Windows.MessageBox.Show(blockReason, "Cảnh báo bảo mật",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    await AuditLogService.WriteLogAsync("Xóa Danh mục",
                        $"Thử xóa thể loại '{name}' bị chặn do ràng buộc", false);
                    return;
                }

                _allBookTypes.Remove(SelectedBookType);
                ApplyBookTypeFilter();
                await AuditLogService.WriteLogAsync("Xóa Danh mục", $"Xóa thể loại sách '{name}' thành công", true);
                ClearBookType();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ClearBookType()
        {
            SelectedBookType = null;
            BookTypeName = "";
            BookTypeDescription = "";
            BookTypeError = "";
        }

        // DEPARTMENTS ACTIONS
        private async Task SaveDepartmentAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                DepartmentError = "";
                if (string.IsNullOrWhiteSpace(DepartmentName))
                {
                    DepartmentError = "Tên khoa/phòng không được để trống!";
                    return;
                }

                if (string.IsNullOrWhiteSpace(DepartmentCode))
                {
                    DepartmentError = "Mã khoa/phòng không được để trống!";
                    return;
                }

                var cleanName = InputHelper.NormalizeInput(DepartmentName);
                var cleanCode = InputHelper.TrimInput(DepartmentCode).ToUpper();

                if (!InputHelper.ValidateLength(cleanName, 2, 150))
                {
                    DepartmentError = "Tên khoa/phòng phải từ 2 đến 150 ký tự!";
                    return;
                }

                var duplicate = _allDepartments.FirstOrDefault(d => (d.Name.Equals(cleanName, StringComparison.OrdinalIgnoreCase) || d.Code.Equals(cleanCode, StringComparison.OrdinalIgnoreCase)) && d != SelectedDepartment);
                if (duplicate != null)
                {
                    DepartmentError = "Tên hoặc mã khoa/phòng đã tồn tại trong hệ thống!";
                    return;
                }

                if (SelectedDepartment == null)
                {
                    int newId = _allDepartments.Count > 0 ? _allDepartments.Max(d => d.Id) + 1 : 1;
                    var newDep = new DepartmentDto { Id = newId, Name = cleanName, Code = cleanCode };
                    _allDepartments.Add(newDep);
                    ApplyDepartmentFilter();
                    await AuditLogService.WriteLogAsync("Thêm Danh mục", $"Thêm khoa/bộ môn '{cleanName}' ({cleanCode}) thành công", true);
                }
                else
                {
                    string oldName = SelectedDepartment.Name;
                    SelectedDepartment.Name = cleanName;
                    SelectedDepartment.Code = cleanCode;
                    var indexAll = _allDepartments.IndexOf(SelectedDepartment);
                    if (indexAll >= 0)
                    {
                        _allDepartments[indexAll] = SelectedDepartment;
                    }
                    ApplyDepartmentFilter();
                    await AuditLogService.WriteLogAsync("Sửa Danh mục", $"Cập nhật khoa/bộ môn '{oldName}' thành '{cleanName}' ({cleanCode}) thành công", true);
                }

                ClearDepartment();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task DeleteDepartmentAsync()
        {
            if (IsBusy) return;
            if (SelectedDepartment == null) return;
            IsBusy = true;
            try
            {
                string name = SelectedDepartment.Name;

                var confirm = System.Windows.MessageBox.Show($"Bạn có chắc chắn muốn xóa khoa/bộ môn '{name}' không?", "Xác nhận xóa", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
                if (confirm == System.Windows.MessageBoxResult.Yes)
                {
                    _allDepartments.Remove(SelectedDepartment);
                    ApplyDepartmentFilter();
                    await AuditLogService.WriteLogAsync("Xóa Danh mục", $"Xóa khoa/bộ môn '{name}' thành công", true);
                    ClearDepartment();
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ClearDepartment()
        {
            SelectedDepartment = null;
            DepartmentName = "";
            DepartmentCode = "";
            DepartmentError = "";
        }

        /// <summary>
        /// Fallback khi API offline — kiểm tra tên hardcoded (logic cũ)
        /// </summary>
        private static bool FallbackCheckDependency(string type, string name)
        {
            if (type == "publisher")
                return name == "NXB Giáo Dục Việt Nam"; // Giữ logic cũ làm fallback
            if (type == "booktype")
                return name == "Giáo trình";
            return false;
        }

        public void LoadPendingProposalsCount()
        {
            System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                int count = 0;
                bool success = false;
                if (_apiService != null)
                {
                    try
                    {
                        var proposals = await _apiService.GetAsync<ProposalItemMock[]>("/Proposals");
                        if (proposals != null)
                        {
                            count = proposals.Count(p => p.Status == "Chờ duyệt");
                            success = true;
                        }
                    }
                    catch { }
                }

                if (!success)
                {
                    try
                    {
                        var filePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SmartLibrary", "data", "proposals.json");
                        if (System.IO.File.Exists(filePath))
                        {
                            var json = System.IO.File.ReadAllText(filePath);
                            var proposals = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<ProposalItemMock>>(json);
                            if (proposals != null)
                            {
                                count = proposals.Count(p => p.Status == "Chờ duyệt");
                            }
                        }
                    }
                    catch { }
                }

                PendingProposalsCount = count;
                HasPendingProposals = PendingProposalsCount > 0;
            });
        }

        private struct ColumnMapping
        {
            public int IdCol;
            public int NameCol;
            public int Extra1Col; // Address for Publisher, Description for BookType, Code for Department
            public int Extra2Col; // Contact for Publisher
        }

        private ColumnMapping DetectColumns(IXLWorksheet worksheet, int headerRowNum, string type)
        {
            var mapping = new ColumnMapping
            {
                IdCol = 1,
                NameCol = 2,
                Extra1Col = 3,
                Extra2Col = 4
            };

            var headerRow = worksheet.Row(headerRowNum);
            int lastCol = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 4;

            bool foundName = false;

            for (int col = 1; col <= lastCol; col++)
            {
                string headerText = headerRow.Cell(col).Value.ToString().Trim().ToLower();
                if (string.IsNullOrEmpty(headerText)) continue;

                if (headerText.Contains("mã") || headerText.Contains("id"))
                {
                    mapping.IdCol = col;
                }
                else if (headerText.Contains("tên") || headerText.Contains("name") || headerText.Contains("nxb") || headerText.Contains("thể loại") || headerText.Contains("khoa") || headerText.Contains("phòng") || headerText.Contains("bộ môn"))
                {
                    mapping.NameCol = col;
                    foundName = true;
                }
                else if (type == "publisher" && (headerText.Contains("địa chỉ") || headerText.Contains("address")))
                {
                    mapping.Extra1Col = col;
                }
                else if (type == "publisher" && (headerText.Contains("liên hệ") || headerText.Contains("contact") || headerText.Contains("sđt") || headerText.Contains("điện thoại")))
                {
                    mapping.Extra2Col = col;
                }
                else if (type == "booktype" && (headerText.Contains("mô tả") || headerText.Contains("description") || headerText.Contains("tóm tắt")))
                {
                    mapping.Extra1Col = col;
                }
                else if (type == "department" && (headerText.Contains("viết tắt") || headerText.Contains("code") || headerText.Contains("ký hiệu")))
                {
                    mapping.Extra1Col = col;
                }
            }

            if (!foundName)
            {
                mapping.NameCol = lastCol >= 2 ? 2 : 1;
                mapping.Extra1Col = lastCol >= 3 ? 3 : 2;
                mapping.Extra2Col = lastCol >= 4 ? 4 : 3;
            }

            return mapping;
        }

        private async Task ExportPublishersExcelAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx",
                    Title = "Xuất danh sách nhà xuất bản",
                    FileName = $"NhaXuatBan_{DateTime.Now:yyyyMMdd}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        string filePath = saveFileDialog.FileName;
                        var headers = new[] { "Mã NXB", "Tên Nhà Xuất Bản", "Địa chỉ", "Liên hệ" };
                        await ExcelExportService.ExportToXlsxAsync(
                            Publishers,
                            headers,
                            p => new object[] { p.Id, p.Name, p.Address, p.Contact },
                            filePath,
                            "Danh sách Nhà xuất bản"
                        );
                        ToastService.ShowSuccess("Xuất Excel danh sách NXB thành công!");
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Lỗi xuất Excel: {ex.Message}", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ImportPublishersExcelAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx",
                    Title = "Nhập danh sách nhà xuất bản từ Excel"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        string filePath = openFileDialog.FileName;
                        int importedCount = 0;
                        int duplicateCount = 0;
                        var duplicatesList = new System.Collections.Generic.List<string>();
                        var newPubs = new System.Collections.Generic.List<PublisherDto>();

                        await Task.Run(() =>
                        {
                            using var workbook = new XLWorkbook(filePath);
                            var worksheet = workbook.Worksheets.FirstOrDefault();
                            if (worksheet == null || worksheet.LastRowUsed() == null)
                            {
                                throw new InvalidOperationException("Tệp Excel rỗng hoặc không đúng định dạng mẫu!");
                            }

                            var firstCell = worksheet.Cell(1, 1).Value.ToString();
                            int startRow = 2;
                            int headerRow = 1;
                            if (firstCell.Contains("Exported") || firstCell.Contains("Danh sách"))
                            {
                                startRow = 4;
                                headerRow = 3;
                            }

                            var mapping = DetectColumns(worksheet, headerRow, "publisher");
                            int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

                            for (int rowNum = startRow; rowNum <= lastRow; rowNum++)
                            {
                                var row = worksheet.Row(rowNum);
                                if (row.IsEmpty()) continue;

                                string rawName = row.Cell(mapping.NameCol).Value.ToString();
                                if (string.IsNullOrWhiteSpace(rawName)) continue;

                                string cleanName = InputHelper.NormalizeInput(rawName);
                                string cleanAddress = InputHelper.TrimInput(row.Cell(mapping.Extra1Col).Value.ToString());
                                string cleanContact = InputHelper.TrimInput(row.Cell(mapping.Extra2Col).Value.ToString());

                                if (!InputHelper.ValidateLength(cleanName, 2, 150)) continue;

                                bool isDuplicate = false;
                                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                {
                                    isDuplicate = _allPublishers.Any(p => p.Name.Equals(cleanName, StringComparison.OrdinalIgnoreCase)) ||
                                                  newPubs.Any(p => p.Name.Equals(cleanName, StringComparison.OrdinalIgnoreCase));
                                });

                                if (isDuplicate)
                                {
                                    duplicateCount++;
                                    duplicatesList.Add(cleanName);
                                }
                                else
                                {
                                    int nextId = 0;
                                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        nextId = (_allPublishers.Count > 0 ? _allPublishers.Max(p => p.Id) : 0) + newPubs.Count + 1;
                                    });

                                    newPubs.Add(new PublisherDto
                                    {
                                        Id = nextId,
                                        Name = cleanName,
                                        Address = cleanAddress,
                                        Contact = cleanContact
                                    });
                                    importedCount++;
                                }
                            }
                        });

                        if (newPubs.Count > 0)
                        {
                            foreach (var pub in newPubs)
                            {
                                _allPublishers.Add(pub);
                            }
                            ApplyPublisherFilter();
                            await AuditLogService.WriteLogAsync("Nhập Excel", $"Nhập thành công {newPubs.Count} NXB từ Excel", true);
                        }

                        string msg = $"Nhập Excel hoàn tất!\n- Thành công: {importedCount} mục\n- Trùng lặp (Bỏ qua): {duplicateCount} mục";
                        if (duplicatesList.Count > 0)
                        {
                            msg += $"\nDanh sách trùng: {string.Join(", ", duplicatesList.Take(5))}";
                            if (duplicatesList.Count > 5) msg += "...";
                        }
                        System.Windows.MessageBox.Show(msg, "Kết quả nhập Excel", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                        ToastService.ShowSuccess($"Nhập thành công {importedCount} nhà xuất bản!");
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Lỗi nhập Excel: {ex.Message}", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExportBookTypesExcelAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx",
                    Title = "Xuất danh sách thể loại sách",
                    FileName = $"TheLoaiSach_{DateTime.Now:yyyyMMdd}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        string filePath = saveFileDialog.FileName;
                        var headers = new[] { "Mã thể loại", "Tên Thể Loại", "Mô tả" };
                        await ExcelExportService.ExportToXlsxAsync(
                            BookTypes,
                            headers,
                            bt => new object[] { bt.Id, bt.Name, bt.Description },
                            filePath,
                            "Danh sách Thể loại sách"
                        );
                        ToastService.ShowSuccess("Xuất Excel danh sách thể loại thành công!");
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Lỗi xuất Excel: {ex.Message}", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ImportBookTypesExcelAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx",
                    Title = "Nhập danh sách thể loại sách từ Excel"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        string filePath = openFileDialog.FileName;
                        int importedCount = 0;
                        int duplicateCount = 0;
                        var duplicatesList = new System.Collections.Generic.List<string>();
                        var newTypes = new System.Collections.Generic.List<BookTypeDto>();

                        await Task.Run(() =>
                        {
                            using var workbook = new XLWorkbook(filePath);
                            var worksheet = workbook.Worksheets.FirstOrDefault();
                            if (worksheet == null || worksheet.LastRowUsed() == null)
                            {
                                throw new InvalidOperationException("Tệp Excel rỗng hoặc không đúng định dạng mẫu!");
                            }

                            var firstCell = worksheet.Cell(1, 1).Value.ToString();
                            int startRow = 2;
                            int headerRow = 1;
                            if (firstCell.Contains("Exported") || firstCell.Contains("Danh sách"))
                            {
                                startRow = 4;
                                headerRow = 3;
                            }

                            var mapping = DetectColumns(worksheet, headerRow, "booktype");
                            int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

                            for (int rowNum = startRow; rowNum <= lastRow; rowNum++)
                            {
                                var row = worksheet.Row(rowNum);
                                if (row.IsEmpty()) continue;

                                string rawName = row.Cell(mapping.NameCol).Value.ToString();
                                if (string.IsNullOrWhiteSpace(rawName)) continue;

                                string cleanName = InputHelper.NormalizeInput(rawName);
                                string cleanDesc = InputHelper.TrimInput(row.Cell(mapping.Extra1Col).Value.ToString());

                                if (!InputHelper.ValidateLength(cleanName, 2, 150)) continue;

                                bool isDuplicate = false;
                                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                {
                                    isDuplicate = _allBookTypes.Any(t => t.Name.Equals(cleanName, StringComparison.OrdinalIgnoreCase)) ||
                                                  newTypes.Any(t => t.Name.Equals(cleanName, StringComparison.OrdinalIgnoreCase));
                                });

                                if (isDuplicate)
                                {
                                    duplicateCount++;
                                    duplicatesList.Add(cleanName);
                                }
                                else
                                {
                                    int nextId = 0;
                                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        nextId = (_allBookTypes.Count > 0 ? _allBookTypes.Max(t => t.Id) : 0) + newTypes.Count + 1;
                                    });

                                    newTypes.Add(new BookTypeDto
                                    {
                                        Id = nextId,
                                        Name = cleanName,
                                        Description = cleanDesc
                                    });
                                    importedCount++;
                                }
                            }
                        });

                        if (newTypes.Count > 0)
                        {
                            foreach (var bt in newTypes)
                            {
                                _allBookTypes.Add(bt);
                            }
                            ApplyBookTypeFilter();
                            await AuditLogService.WriteLogAsync("Nhập Excel", $"Nhập thành công {newTypes.Count} thể loại sách từ Excel", true);
                        }

                        string msg = $"Nhập Excel hoàn tất!\n- Thành công: {importedCount} mục\n- Trùng lặp (Bỏ qua): {duplicateCount} mục";
                        if (duplicatesList.Count > 0)
                        {
                            msg += $"\nDanh sách trùng: {string.Join(", ", duplicatesList.Take(5))}";
                            if (duplicatesList.Count > 5) msg += "...";
                        }
                        System.Windows.MessageBox.Show(msg, "Kết quả nhập Excel", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                        ToastService.ShowSuccess($"Nhập thành công {importedCount} thể loại sách!");
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Lỗi nhập Excel: {ex.Message}", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExportDepartmentsExcelAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx",
                    Title = "Xuất danh sách tổ bộ môn / phòng ban",
                    FileName = $"ToBoMon_{DateTime.Now:yyyyMMdd}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        string filePath = saveFileDialog.FileName;
                        var headers = new[] { "Mã ID", "Tên Tổ bộ môn / Phòng ban", "Ký hiệu viết tắt" };
                        await ExcelExportService.ExportToXlsxAsync(
                            Departments,
                            headers,
                            d => new object[] { d.Id, d.Name, d.Code },
                            filePath,
                            "Danh sách Tổ bộ môn / Phòng ban"
                        );
                        ToastService.ShowSuccess("Xuất Excel danh sách khoa/phòng thành công!");
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Lỗi xuất Excel: {ex.Message}", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ImportDepartmentsExcelAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx",
                    Title = "Nhập danh sách tổ bộ môn / phòng ban từ Excel"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        string filePath = openFileDialog.FileName;
                        int importedCount = 0;
                        int duplicateCount = 0;
                        var duplicatesList = new System.Collections.Generic.List<string>();
                        var newDeps = new System.Collections.Generic.List<DepartmentDto>();

                        await Task.Run(() =>
                        {
                            using var workbook = new XLWorkbook(filePath);
                            var worksheet = workbook.Worksheets.FirstOrDefault();
                            if (worksheet == null || worksheet.LastRowUsed() == null)
                            {
                                throw new InvalidOperationException("Tệp Excel rỗng hoặc không đúng định dạng mẫu!");
                            }

                            var firstCell = worksheet.Cell(1, 1).Value.ToString();
                            int startRow = 2;
                            int headerRow = 1;
                            if (firstCell.Contains("Exported") || firstCell.Contains("Danh sách"))
                            {
                                startRow = 4;
                                headerRow = 3;
                            }

                            var mapping = DetectColumns(worksheet, headerRow, "department");
                            int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

                            for (int rowNum = startRow; rowNum <= lastRow; rowNum++)
                            {
                                var row = worksheet.Row(rowNum);
                                if (row.IsEmpty()) continue;

                                string rawName = row.Cell(mapping.NameCol).Value.ToString();
                                string rawCode = row.Cell(mapping.Extra1Col).Value.ToString();
                                if (string.IsNullOrWhiteSpace(rawName) || string.IsNullOrWhiteSpace(rawCode)) continue;

                                string cleanName = InputHelper.NormalizeInput(rawName);
                                string cleanCode = InputHelper.TrimInput(rawCode).ToUpper();

                                if (!InputHelper.ValidateLength(cleanName, 2, 150)) continue;

                                bool isDuplicate = false;
                                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                {
                                    isDuplicate = _allDepartments.Any(d => d.Name.Equals(cleanName, StringComparison.OrdinalIgnoreCase) || d.Code.Equals(cleanCode, StringComparison.OrdinalIgnoreCase)) ||
                                                  newDeps.Any(d => d.Name.Equals(cleanName, StringComparison.OrdinalIgnoreCase) || d.Code.Equals(cleanCode, StringComparison.OrdinalIgnoreCase));
                                });

                                if (isDuplicate)
                                {
                                    duplicateCount++;
                                    duplicatesList.Add(cleanName);
                                }
                                else
                                {
                                    int nextId = 0;
                                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        nextId = (_allDepartments.Count > 0 ? _allDepartments.Max(d => d.Id) : 0) + newDeps.Count + 1;
                                    });

                                    newDeps.Add(new DepartmentDto
                                    {
                                        Id = nextId,
                                        Name = cleanName,
                                        Code = cleanCode
                                    });
                                    importedCount++;
                                }
                            }
                        });

                        if (newDeps.Count > 0)
                        {
                            foreach (var dep in newDeps)
                            {
                                _allDepartments.Add(dep);
                            }
                            ApplyDepartmentFilter();
                            await AuditLogService.WriteLogAsync("Nhập Excel", $"Nhập thành công {newDeps.Count} tổ bộ môn / phòng ban từ Excel", true);
                        }

                        string msg = $"Nhập Excel hoàn tất!\n- Thành công: {importedCount} mục\n- Trùng lặp (Bỏ qua): {duplicateCount} mục";
                        if (duplicatesList.Count > 0)
                        {
                            msg += $"\nDanh sách trùng: {string.Join(", ", duplicatesList.Take(5))}";
                            if (duplicatesList.Count > 5) msg += "...";
                        }
                        System.Windows.MessageBox.Show(msg, "Kết quả nhập Excel", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                        ToastService.ShowSuccess($"Nhập thành công {importedCount} khoa/phòng!");
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Lỗi nhập Excel: {ex.Message}", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExportPublishersTemplateAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    Title = "Tải file mẫu nhập Nhà xuất bản từ Excel",
                    FileName = "NhaXuatBan_MauNhap"
                };
                if (saveFileDialog.ShowDialog() == true)
                {
                    var headers = new[] { "Mã NXB", "Tên Nhà Xuất Bản", "Địa chỉ", "Liên hệ" };
                    await ExcelExportService.ExportToXlsxAsync(new System.Collections.Generic.List<PublisherDto>(), headers, p => new object[] { p.Id, p.Name, p.Address, p.Contact }, saveFileDialog.FileName, "Mẫu Nhập NXB");
                    System.Windows.MessageBox.Show("Tải file Excel mẫu Nhà xuất bản thành công!", "Thông báo", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi tải file mẫu: {ex.Message}", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExportBookTypesTemplateAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    Title = "Tải file mẫu nhập Thể loại sách từ Excel",
                    FileName = "TheLoaiSach_MauNhap"
                };
                if (saveFileDialog.ShowDialog() == true)
                {
                    var headers = new[] { "Mã thể loại", "Tên Thể Loại", "Mô tả" };
                    await ExcelExportService.ExportToXlsxAsync(new System.Collections.Generic.List<BookTypeDto>(), headers, bt => new object[] { bt.Id, bt.Name, bt.Description }, saveFileDialog.FileName, "Mẫu Nhập Thể Loại");
                    System.Windows.MessageBox.Show("Tải file Excel mẫu Thể loại sách thành công!", "Thông báo", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi tải file mẫu: {ex.Message}", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExportDepartmentsTemplateAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    Title = "Tải file mẫu nhập Tổ bộ môn / Phòng ban từ Excel",
                    FileName = "ToBoMon_MauNhap"
                };
                if (saveFileDialog.ShowDialog() == true)
                {
                    var headers = new[] { "Mã ID", "Tên Tổ bộ môn / Phòng ban", "Ký hiệu viết tắt" };
                    await ExcelExportService.ExportToXlsxAsync(new System.Collections.Generic.List<DepartmentDto>(), headers, d => new object[] { d.Id, d.Name, d.Code }, saveFileDialog.FileName, "Mẫu Nhập Tổ Bộ Môn");
                    System.Windows.MessageBox.Show("Tải file Excel mẫu Tổ bộ môn / Phòng ban thành công!", "Thông báo", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi tải file mẫu: {ex.Message}", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private class ProposalItemMock
        {
            public string Status { get; set; } = "";
        }
    }
}
