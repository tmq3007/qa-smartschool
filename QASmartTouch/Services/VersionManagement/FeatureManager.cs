using System;
using System.Collections.Generic;
using System.Linq;
using QASmartTouch.Services.VersionManagement.Models;

namespace QASmartTouch.Services.VersionManagement
{
    /// <summary>
    /// Service quản lý trạng thái bật/tắt của các features.
    /// Cung cấp API để kiểm tra feature có được bật hay không.
    /// </summary>
    public class FeatureManager
    {
        private static FeatureManager? _instance;
        private static readonly object _lock = new();

        // Cache các feature đã được flatten với đường dẫn đầy đủ
        private Dictionary<string, FeatureItem> _featureCache = new();

        // Cache trạng thái enabled (đã tính cả parent và overrides)
        private Dictionary<string, bool> _enabledCache = new();

        // Event thông báo khi features thay đổi
        public event EventHandler? FeaturesChanged;

        public static FeatureManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new FeatureManager();
                    }
                }
                return _instance;
            }
        }

        private FeatureManager()
        {
            BuildFeatureCache();
        }

        #region Public API - Kiểm tra Feature

        /// <summary>
        /// Kiểm tra một feature có được bật không.
        /// Hỗ trợ path format: "shapes_2d.lines_group.arrow" hoặc chỉ "arrow"
        /// </summary>
        /// <param name="featurePath">Đường dẫn hoặc ID của feature</param>
        /// <returns>True nếu feature được bật, False nếu tắt hoặc không tồn tại</returns>
        public bool IsEnabled(string featurePath)
        {
            if (string.IsNullOrEmpty(featurePath))
                return false;

            // Normalize path
            featurePath = featurePath.ToLowerInvariant().Trim();

            // Kiểm tra trong cache
            if (_enabledCache.TryGetValue(featurePath, out bool enabled))
            {
                return enabled;
            }

            // Tìm feature và tính trạng thái
            var feature = FindFeature(featurePath);
            if (feature != null)
            {
                enabled = feature.IsEffectivelyEnabled;
                _enabledCache[featurePath] = enabled;
                return enabled;
            }

            // Không tìm thấy feature - mặc định là disabled
            System.Diagnostics.Debug.WriteLine($"[FeatureManager] Feature not found: {featurePath}");
            return false;
        }

        /// <summary>
        /// Kiểm tra nhiều features cùng lúc
        /// </summary>
        /// <param name="featurePaths">Danh sách đường dẫn features</param>
        /// <returns>Dictionary với kết quả của từng feature</returns>
        public Dictionary<string, bool> AreEnabled(params string[] featurePaths)
        {
            var result = new Dictionary<string, bool>();
            foreach (var path in featurePaths)
            {
                result[path] = IsEnabled(path);
            }
            return result;
        }

        /// <summary>
        /// Lấy danh sách các feature con đang enabled
        /// </summary>
        /// <param name="parentPath">Đường dẫn của feature cha</param>
        /// <returns>Danh sách ID các feature con đang enabled</returns>
        public List<string> GetEnabledChildren(string parentPath)
        {
            var result = new List<string>();
            var parent = FindFeature(parentPath);

            if (parent?.Children != null)
            {
                foreach (var child in parent.Children.Values)
                {
                    if (child.IsEffectivelyEnabled)
                    {
                        result.Add(child.Id);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Lấy tất cả feature trong một category
        /// </summary>
        /// <param name="categoryPath">Đường dẫn category (VD: "shapes_2d")</param>
        /// <returns>Danh sách FeatureItem</returns>
        public List<FeatureItem> GetFeaturesInCategory(string categoryPath)
        {
            var result = new List<FeatureItem>();
            var category = FindFeature(categoryPath);

            if (category?.Children != null)
            {
                CollectAllFeatures(category, result);
            }

            return result;
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một feature
        /// </summary>
        public FeatureItem? GetFeature(string featurePath)
        {
            return FindFeature(featurePath);
        }

        /// <summary>
        /// Lấy tất cả categories (feature cấp cao nhất)
        /// </summary>
        public List<FeatureItem> GetAllCategories()
        {
            var versionDetail = VersionService.Instance.VersionDetail;
            if (versionDetail?.Features == null)
                return new List<FeatureItem>();

            return versionDetail.Features.Values.ToList();
        }

        #endregion

        #region Feature State Management

        /// <summary>
        /// Bật/tắt một feature (chỉ thay đổi trong runtime, không lưu file)
        /// </summary>
        public void SetEnabled(string featurePath, bool enabled)
        {
            var feature = FindFeature(featurePath);
            if (feature != null)
            {
                System.Diagnostics.Debug.WriteLine($"[FeatureManager] SetEnabled: {featurePath} -> {enabled} (was: {feature.Enabled})");
                feature.Enabled = enabled;
                ClearCache();
                OnFeaturesChanged();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[FeatureManager] SetEnabled FAILED: Feature '{featurePath}' not found!");
            }
        }

        /// <summary>
        /// Bật/tắt một feature và lưu vào file
        /// </summary>
        public void SetEnabledAndSave(string featurePath, bool enabled)
        {
            SetEnabled(featurePath, enabled);
            VersionService.Instance.SaveVersionDetail();
        }

        /// <summary>
        /// Reset về cấu hình mặc định của template hiện tại
        /// </summary>
        public void ResetToTemplate()
        {
            ReloadFeatures();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Reload features từ VersionService và áp dụng template overrides
        /// </summary>
        public void ReloadFeatures()
        {
            BuildFeatureCache();
            ApplyTemplateOverrides();
            ClearCache();
            OnFeaturesChanged();
        }

        /// <summary>
        /// Build cache từ VersionDetail
        /// </summary>
        private void BuildFeatureCache()
        {
            _featureCache.Clear();
            _enabledCache.Clear();

            var versionDetail = VersionService.Instance.VersionDetail;
            if (versionDetail?.Features == null)
            {
                System.Diagnostics.Debug.WriteLine("[FeatureManager] No features to cache");
                return;
            }

            foreach (var category in versionDetail.Features.Values)
            {
                BuildCacheRecursive(category, "");
            }

            System.Diagnostics.Debug.WriteLine($"[FeatureManager] Cached {_featureCache.Count} features");
            
            // Debug: Log math symbols status
            var mathSymbols = new[] { "approximately", "infinity", "radical", "summation", "integral" };
            foreach (var symbolId in mathSymbols)
            {
                var feature = FindFeature(symbolId);
                if (feature != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[FeatureManager] Math symbol '{symbolId}' loaded with enabled={feature.Enabled}");
                }
            }
        }

        /// <summary>
        /// Build cache đệ quy cho mỗi feature
        /// </summary>
        private void BuildCacheRecursive(FeatureItem feature, string parentPath)
        {
            string fullPath = string.IsNullOrEmpty(parentPath) ? feature.Id : $"{parentPath}.{feature.Id}";
            feature.FullPath = fullPath;

            // Cache theo full path
            _featureCache[fullPath.ToLowerInvariant()] = feature;

            // Cache theo ID (nếu chưa có - để hỗ trợ lookup bằng ID ngắn)
            string idLower = feature.Id.ToLowerInvariant();
            if (!_featureCache.ContainsKey(idLower))
            {
                _featureCache[idLower] = feature;
            }

            // Đệ quy cho children
            if (feature.Children != null)
            {
                foreach (var child in feature.Children.Values)
                {
                    child.Parent = feature;
                    BuildCacheRecursive(child, fullPath);
                }
            }
        }

        /// <summary>
        /// Áp dụng overrides từ template hiện tại
        /// </summary>
        private void ApplyTemplateOverrides()
        {
            var activeTemplate = VersionService.Instance.ActiveTemplate;
            var template = VersionService.Instance.GetTemplate(activeTemplate);

            if (template?.Overrides == null || template.Overrides.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[FeatureManager] No overrides for template: {activeTemplate}");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[FeatureManager] Applying {template.Overrides.Count} overrides from template: {activeTemplate}");

            foreach (var kvp in template.Overrides)
            {
                var feature = FindFeature(kvp.Key);
                if (feature != null)
                {
                    feature.Enabled = kvp.Value;
                    System.Diagnostics.Debug.WriteLine($"[FeatureManager] Override: {kvp.Key} = {kvp.Value}");
                }
            }
        }

        /// <summary>
        /// Tìm feature theo path hoặc ID
        /// </summary>
        private FeatureItem? FindFeature(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            path = path.ToLowerInvariant().Trim();

            // Tìm trong cache
            if (_featureCache.TryGetValue(path, out var feature))
            {
                return feature;
            }

            // Không tìm thấy
            return null;
        }

        /// <summary>
        /// Thu thập tất cả features con đệ quy
        /// </summary>
        private void CollectAllFeatures(FeatureItem parent, List<FeatureItem> result)
        {
            if (parent.Children == null) return;

            foreach (var child in parent.Children.Values)
            {
                result.Add(child);
                CollectAllFeatures(child, result);
            }
        }

        /// <summary>
        /// Xóa cache enabled
        /// </summary>
        private void ClearCache()
        {
            _enabledCache.Clear();
        }

        /// <summary>
        /// Raise event FeaturesChanged
        /// </summary>
        private void OnFeaturesChanged()
        {
            FeaturesChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// In ra cây features (debug)
        /// </summary>
        public void PrintFeatureTree()
        {
            var categories = GetAllCategories();
            foreach (var cat in categories)
            {
                PrintFeatureRecursive(cat, 0);
            }
        }

        private void PrintFeatureRecursive(FeatureItem feature, int indent)
        {
            string prefix = new string(' ', indent * 2);
            string status = feature.IsEffectivelyEnabled ? "✓" : "✗";
            System.Diagnostics.Debug.WriteLine($"{prefix}{status} {feature.Name} ({feature.Id})");

            if (feature.Children != null)
            {
                foreach (var child in feature.Children.Values)
                {
                    PrintFeatureRecursive(child, indent + 1);
                }
            }
        }

        /// <summary>
        /// Lấy thống kê features
        /// </summary>
        public (int Total, int Enabled, int Disabled) GetStatistics()
        {
            int total = _featureCache.Count;
            int enabled = _featureCache.Values.Count(f => f.IsEffectivelyEnabled);
            return (total, enabled, total - enabled);
        }

        #endregion
    }
}
