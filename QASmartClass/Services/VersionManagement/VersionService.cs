using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using QASmartTouch.Services.VersionManagement.Models;

namespace QASmartTouch.Services.VersionManagement
{
    /// <summary>
    /// Service đọc/ghi các file cấu hình Version.
    /// Xử lý việc load VersionStatus.json và VersionDetail.json
    /// </summary>
    public class VersionService
    {
        private static VersionService? _instance;
        private static readonly object _lock = new();

        private const string VERSION_STATUS_FILE = "VersionStatus.json";
        private const string VERSION_DETAIL_FILE = "VersionDetail.json";
        private const string CONFIG_FOLDER = "config";

        private VersionStatus? _versionStatus;
        private VersionDetail? _versionDetail;

        private readonly JsonSerializerOptions _jsonOptions;

        public static VersionService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new VersionService();
                    }
                }
                return _instance;
            }
        }

        private VersionService()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                ReferenceHandler = ReferenceHandler.IgnoreCycles
            };

            LoadConfigurations();
        }

        #region Public Properties

        /// <summary>
        /// Trạng thái phiên bản hiện tại
        /// </summary>
        public VersionStatus? VersionStatus => _versionStatus;

        /// <summary>
        /// Chi tiết cấu hình features
        /// </summary>
        public VersionDetail? VersionDetail => _versionDetail;

        /// <summary>
        /// Phiên bản hiện tại
        /// </summary>
        public string CurrentVersion => _versionStatus?.CurrentVersion ?? "1.0.0";

        /// <summary>
        /// Tên phiên bản
        /// </summary>
        public string VersionName => _versionStatus?.VersionName ?? "Unknown";

        /// <summary>
        /// Template đang active
        /// </summary>
        public string ActiveTemplate => _versionStatus?.ActiveTemplate ?? "1.0.0-public";

        #endregion

        #region Load/Save Methods

        /// <summary>
        /// Load tất cả cấu hình từ files
        /// </summary>
        public void LoadConfigurations()
        {
            LoadVersionStatus();
            LoadVersionDetail();
        }

        /// <summary>
        /// Load VersionStatus từ file JSON
        /// </summary>
        private void LoadVersionStatus()
        {
            try
            {
                string filePath = GetConfigFilePath(VERSION_STATUS_FILE);

                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    _versionStatus = JsonSerializer.Deserialize<VersionStatus>(json, _jsonOptions);
                    System.Diagnostics.Debug.WriteLine($"[VersionService] Loaded VersionStatus: v{_versionStatus?.CurrentVersion}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[VersionService] VersionStatus file not found, using defaults");
                    _versionStatus = CreateDefaultVersionStatus();
                    SaveVersionStatus(); // Tạo file mặc định
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VersionService] Error loading VersionStatus: {ex.Message}");
                _versionStatus = CreateDefaultVersionStatus();
            }
        }

        /// <summary>
        /// Load VersionDetail từ file JSON
        /// </summary>
        private void LoadVersionDetail()
        {
            try
            {
                string filePath = GetConfigFilePath(VERSION_DETAIL_FILE);

                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    _versionDetail = JsonSerializer.Deserialize<VersionDetail>(json, _jsonOptions);
                    System.Diagnostics.Debug.WriteLine($"[VersionService] Loaded VersionDetail with {_versionDetail?.Features.Count ?? 0} feature categories");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[VersionService] VersionDetail file not found, using defaults");
                    _versionDetail = CreateDefaultVersionDetail();
                    SaveVersionDetail(); // Tạo file mặc định
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VersionService] Error loading VersionDetail: {ex.Message}");
                _versionDetail = CreateDefaultVersionDetail();
            }
        }

        /// <summary>
        /// Lưu VersionStatus vào file
        /// </summary>
        public void SaveVersionStatus()
        {
            try
            {
                if (_versionStatus == null) return;

                string filePath = GetConfigFilePath(VERSION_STATUS_FILE);
                EnsureConfigDirectoryExists();

                string json = JsonSerializer.Serialize(_versionStatus, _jsonOptions);
                File.WriteAllText(filePath, json);

                System.Diagnostics.Debug.WriteLine($"[VersionService] Saved VersionStatus");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VersionService] Error saving VersionStatus: {ex.Message}");
            }
        }

        /// <summary>
        /// Lưu VersionDetail vào file
        /// </summary>
        public void SaveVersionDetail()
        {
            try
            {
                if (_versionDetail == null) return;

                string filePath = GetConfigFilePath(VERSION_DETAIL_FILE);
                EnsureConfigDirectoryExists();

                _versionDetail.LastModified = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");

                string json = JsonSerializer.Serialize(_versionDetail, _jsonOptions);
                File.WriteAllText(filePath, json);

                System.Diagnostics.Debug.WriteLine($"[VersionService] Saved VersionDetail");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VersionService] Error saving VersionDetail: {ex.Message}");
            }
        }

        #endregion

        #region Template Methods

        /// <summary>
        /// Lấy danh sách các template khả dụng
        /// </summary>
        public List<string> GetAvailableTemplates()
        {
            if (_versionDetail?.VersionTemplates == null)
                return new List<string>();

            return new List<string>(_versionDetail.VersionTemplates.Keys);
        }

        /// <summary>
        /// Lấy thông tin một template
        /// </summary>
        public VersionTemplate? GetTemplate(string templateName)
        {
            if (_versionDetail?.VersionTemplates == null)
                return null;

            return _versionDetail.VersionTemplates.TryGetValue(templateName, out var template) ? template : null;
        }

        /// <summary>
        /// Đổi template active
        /// </summary>
        public void SetActiveTemplate(string templateName)
        {
            if (_versionStatus != null && _versionDetail?.VersionTemplates.ContainsKey(templateName) == true)
            {
                _versionStatus.ActiveTemplate = templateName;
                SaveVersionStatus();

                // Thông báo cho FeatureManager cập nhật
                FeatureManager.Instance.ReloadFeatures();
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Lấy đường dẫn đến file config
        /// </summary>
        private string GetConfigFilePath(string fileName)
        {
            string appPath = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(appPath, CONFIG_FOLDER, fileName);
        }

        /// <summary>
        /// Đảm bảo thư mục config tồn tại
        /// </summary>
        private void EnsureConfigDirectoryExists()
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_FOLDER);
            if (!Directory.Exists(configPath))
            {
                Directory.CreateDirectory(configPath);
            }
        }

        /// <summary>
        /// Tạo VersionStatus mặc định
        /// </summary>
        private VersionStatus CreateDefaultVersionStatus()
        {
            return new VersionStatus
            {
                CurrentVersion = "1.0.0",
                VersionName = "Public Release",
                ReleaseDate = DateTime.Now.ToString("yyyy-MM-dd"),
                ReleaseType = "stable",
                BuildNumber = 1001,
                IsPublicRelease = true,
                ActiveTemplate = "1.0.0-public",
                Description = "Phiên bản phát hành công khai đầu tiên",
                Changelog = new List<string>
                {
                    "- Các tính năng cơ bản của bảng tương tác"
                },
                Metadata = new VersionMetadata
                {
                    CreatedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                    LastModified = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                    Author = "QA Education Technology"
                }
            };
        }

        /// <summary>
        /// Tạo VersionDetail mặc định (minimal)
        /// </summary>
        private VersionDetail CreateDefaultVersionDetail()
        {
            return new VersionDetail
            {
                Version = "1.0.0",
                LastModified = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                SchemaVersion = "1.0",
                Features = new Dictionary<string, FeatureItem>(),
                VersionTemplates = new Dictionary<string, VersionTemplate>
                {
                    ["1.0.0-public"] = new VersionTemplate
                    {
                        Name = "Public Release v1.0",
                        Description = "Phiên bản công khai",
                        ReleaseType = "stable",
                        Overrides = new Dictionary<string, bool>()
                    }
                }
            };
        }

        /// <summary>
        /// Reload lại tất cả cấu hình từ files
        /// </summary>
        public void Reload()
        {
            LoadConfigurations();
            FeatureManager.Instance.ReloadFeatures();
        }

        #endregion
    }
}
