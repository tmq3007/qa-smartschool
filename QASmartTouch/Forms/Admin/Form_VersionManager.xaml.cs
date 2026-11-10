using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QASmartTouch.Services.VersionManagement;
using QASmartTouch.Services.VersionManagement.Models;

namespace QASmartTouch.Forms.Admin
{
    /// <summary>
    /// Version Manager Form - Allows admin to view and toggle features,
    /// select version templates, and save configuration changes.
    /// </summary>
    public partial class Form_VersionManager : Window
    {
        private readonly VersionService _versionService;
        private readonly FeatureManager _featureManager;
        private bool _hasUnsavedChanges = false;
        private Dictionary<string, bool> _pendingChanges = new Dictionary<string, bool>();

        public Form_VersionManager()
        {
            InitializeComponent();
            
            _versionService = VersionService.Instance;
            _featureManager = FeatureManager.Instance;
            
            this.Loaded += Form_VersionManager_Loaded;
            
            // Enable window dragging
            this.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
                    this.DragMove();
            };
        }

        #region Initialization

        private void Form_VersionManager_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadVersionInfo();
                LoadTemplates();
                LoadFeatureTree();
                UpdateStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Load and display current version information
        /// </summary>
        private void LoadVersionInfo()
        {
            var versionStatus = _versionService.VersionStatus;
            if (versionStatus != null)
            {
                txtVersion.Text = versionStatus.CurrentVersion ?? "1.0.0";
                txtBuildNumber.Text = versionStatus.BuildNumber.ToString();
                txtCurrentTemplate.Text = versionStatus.ActiveTemplate ?? "default";
            }
        }

        /// <summary>
        /// Load available templates into combo box
        /// </summary>
        private void LoadTemplates()
        {
            cboTemplates.Items.Clear();
            
            var versionDetail = _versionService.VersionDetail;
            if (versionDetail?.VersionTemplates != null)
            {
                foreach (var template in versionDetail.VersionTemplates)
                {
                    var item = new ComboBoxItem
                    {
                        Content = $"{template.Key} - {template.Value.Name}",
                        Tag = template.Key
                    };
                    cboTemplates.Items.Add(item);
                    
                    // Select current template
                    if (template.Key == _versionService.VersionStatus?.ActiveTemplate)
                    {
                        cboTemplates.SelectedItem = item;
                    }
                }
            }
            
            // Add default option if no templates
            if (cboTemplates.Items.Count == 0)
            {
                cboTemplates.Items.Add(new ComboBoxItem { Content = "No templates available", IsEnabled = false });
            }
        }

        /// <summary>
        /// Load feature tree from VersionDetail
        /// </summary>
        private void LoadFeatureTree()
        {
            treeFeatures.Items.Clear();
            
            var versionDetail = _versionService.VersionDetail;
            if (versionDetail?.Features == null) return;
            
            foreach (var category in versionDetail.Features)
            {
                var categoryItem = CreateFeatureTreeItem(category.Value, category.Key, 0);
                if (categoryItem != null)
                {
                    treeFeatures.Items.Add(categoryItem);
                }
            }
        }

        /// <summary>
        /// Create a TreeViewItem for a feature (recursive for children)
        /// </summary>
        private TreeViewItem CreateFeatureTreeItem(FeatureItem feature, string key, int level)
        {
            var item = new TreeViewItem
            {
                IsExpanded = level < 2, // Expand first 2 levels by default
                Margin = new Thickness(0, 2, 0, 2)
            };

            // Create header with checkbox
            var header = new StackPanel { Orientation = Orientation.Horizontal };
            
            // Checkbox for toggling
            var checkbox = new CheckBox
            {
                IsChecked = _featureManager.IsEnabled(feature.Id),
                VerticalAlignment = VerticalAlignment.Center,
                Tag = feature.Id
            };
            checkbox.Checked += Feature_Checked;
            checkbox.Unchecked += Feature_Unchecked;
            
            header.Children.Add(checkbox);
            
            // Icon based on level
            var icon = level == 0 ? "📁" : (feature.Children?.Count > 0 ? "📂" : "⚙️");
            var iconText = new TextBlock
            {
                Text = icon,
                Margin = new Thickness(4, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            header.Children.Add(iconText);
            
            // Feature name
            var nameText = new TextBlock
            {
                Text = feature.Name ?? feature.Id,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = level == 0 ? FontWeights.SemiBold : FontWeights.Normal
            };
            header.Children.Add(nameText);
            
            // Feature ID badge
            var idBadge = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = feature.Id,
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136))
                }
            };
            header.Children.Add(idBadge);
            
            // Status indicator
            bool isEnabled = _featureManager.IsEnabled(feature.Id);
            var statusBadge = new Border
            {
                Background = new SolidColorBrush(isEnabled ? 
                    Color.FromRgb(232, 245, 233) : Color.FromRgb(255, 235, 238)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(4, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = isEnabled ? "ON" : "OFF",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(isEnabled ? 
                        Color.FromRgb(76, 175, 80) : Color.FromRgb(244, 67, 54))
                }
            };
            header.Children.Add(statusBadge);
            
            item.Header = header;
            
            // Add children recursively
            if (feature.Children != null)
            {
                foreach (var child in feature.Children)
                {
                    var childItem = CreateFeatureTreeItem(child.Value, child.Key, level + 1);
                    if (childItem != null)
                    {
                        item.Items.Add(childItem);
                    }
                }
            }
            
            return item;
        }

        /// <summary>
        /// Update enabled/total stats
        /// </summary>
        private void UpdateStats()
        {
            int total = 0;
            int enabled = 0;
            
            CountFeatures(_versionService.VersionDetail?.Features, ref total, ref enabled);
            
            txtEnabledCount.Text = enabled.ToString();
            txtTotalCount.Text = total.ToString();
        }

        private void CountFeatures(Dictionary<string, FeatureItem>? features, ref int total, ref int enabled)
        {
            if (features == null) return;
            
            foreach (var feature in features.Values)
            {
                total++;
                if (_featureManager.IsEnabled(feature.Id))
                    enabled++;
                
                if (feature.Children != null)
                {
                    CountFeatures(feature.Children, ref total, ref enabled);
                }
            }
        }

        #endregion

        #region Event Handlers

        private void Feature_Checked(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            if (checkbox?.Tag is string featureId)
            {
                _pendingChanges[featureId] = true;
                MarkUnsavedChanges();
            }
        }

        private void Feature_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            if (checkbox?.Tag is string featureId)
            {
                _pendingChanges[featureId] = false;
                MarkUnsavedChanges();
            }
        }

        private void MarkUnsavedChanges()
        {
            _hasUnsavedChanges = true;
            txtLastSaved.Text = "⚠️ Có thay đổi chưa lưu";
            txtLastSaved.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
        }

        private void cboTemplates_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Just update UI, don't apply yet
            if (cboTemplates.SelectedItem is ComboBoxItem item && item.Tag is string templateId)
            {
                txtCurrentTemplate.Text = templateId;
            }
        }

        private void btnApplyTemplate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cboTemplates.SelectedItem is ComboBoxItem item && item.Tag is string templateId)
                {
                    var result = MessageBox.Show(
                        $"Bạn có chắc chắn muốn áp dụng template '{templateId}'?\n\n" +
                        "Điều này sẽ thay đổi trạng thái các tính năng theo cấu hình của template.",
                        "Xác nhận áp dụng Template",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        _versionService.SetActiveTemplate(templateId);
                        _featureManager.ReloadFeatures();
                        
                        // Reload UI
                        LoadVersionInfo();
                        LoadFeatureTree();
                        UpdateStats();
                        
                        _hasUnsavedChanges = false;
                        _pendingChanges.Clear();
                        txtLastSaved.Text = $"✅ Đã áp dụng template: {templateId}";
                        txtLastSaved.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                        
                        MessageBox.Show($"Đã áp dụng template '{templateId}' thành công!", 
                            "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi áp dụng template: {ex.Message}", "Lỗi", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnResetDefault_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "Bạn có chắc chắn muốn reset về trạng thái mặc định?\n\n" +
                    "Tất cả thay đổi chưa lưu sẽ bị mất.",
                    "Xác nhận Reset",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.Yes)
                {
                    _featureManager.ReloadFeatures();
                    
                    // Reload UI
                    LoadFeatureTree();
                    UpdateStats();
                    
                    _hasUnsavedChanges = false;
                    _pendingChanges.Clear();
                    txtLastSaved.Text = "🔄 Đã reset về mặc định";
                    txtLastSaved.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi reset: {ex.Message}", "Lỗi", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSaveChanges_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_hasUnsavedChanges || _pendingChanges.Count == 0)
                {
                    MessageBox.Show("Không có thay đổi nào để lưu.", "Thông báo", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                
                // Store count before clearing
                int changeCount = _pendingChanges.Count;
                
                // Apply pending changes (each call updates the feature in memory)
                foreach (var change in _pendingChanges)
                {
                    System.Diagnostics.Debug.WriteLine($"[VersionManager] Setting feature '{change.Key}' to {change.Value}");
                    _featureManager.SetEnabled(change.Key, change.Value);
                }
                
                // Save all changes to file at once (more efficient)
                System.Diagnostics.Debug.WriteLine($"[VersionManager] Saving {changeCount} changes to VersionDetail.json...");
                _versionService.SaveVersionDetail();
                System.Diagnostics.Debug.WriteLine($"[VersionManager] Save completed successfully");
                
                // Clear pending changes
                _hasUnsavedChanges = false;
                _pendingChanges.Clear();
                
                // Update UI
                LoadFeatureTree();
                UpdateStats();
                
                txtLastSaved.Text = $"💾 Đã lưu {changeCount} thay đổi lúc {DateTime.Now:HH:mm:ss}";
                txtLastSaved.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                
                MessageBox.Show($"Đã lưu {changeCount} thay đổi vào file VersionDetail.json thành công!", 
                    "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                
                System.Diagnostics.Debug.WriteLine($"[VersionManager] Saved {changeCount} feature changes to VersionDetail.json");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lưu thay đổi: {ex.Message}", "Lỗi", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"[VersionManager] Error saving: {ex.Message}");
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            if (_hasUnsavedChanges)
            {
                var result = MessageBox.Show(
                    "Bạn có thay đổi chưa lưu. Bạn có muốn đóng mà không lưu?",
                    "Cảnh báo",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                
                if (result != MessageBoxResult.Yes)
                    return;
            }
            
            this.Close();
        }

        private void btnExportConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Export Configuration",
                    Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                    DefaultExt = ".json",
                    FileName = $"QASmartTouch_Config_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    // Build export data
                    var exportData = new
                    {
                        ExportDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        ExportBy = "QASmartTouch Version Manager",
                        VersionInfo = new
                        {
                            Version = _versionService.VersionStatus?.CurrentVersion,
                            BuildNumber = _versionService.VersionStatus?.BuildNumber,
                            ActiveTemplate = _versionService.VersionStatus?.ActiveTemplate,
                            ReleaseType = _versionService.VersionStatus?.ReleaseType
                        },
                        Features = BuildFeatureExportList()
                    };

                    // Serialize to JSON
                    var options = new System.Text.Json.JsonSerializerOptions 
                    { 
                        WriteIndented = true 
                    };
                    string json = System.Text.Json.JsonSerializer.Serialize(exportData, options);

                    // Write to file
                    System.IO.File.WriteAllText(saveDialog.FileName, json);

                    txtLastSaved.Text = $"📤 Exported to {System.IO.Path.GetFileName(saveDialog.FileName)}";
                    txtLastSaved.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));

                    MessageBox.Show(
                        $"Đã export cấu hình thành công!\n\n" +
                        $"File: {saveDialog.FileName}\n" +
                        $"Size: {new System.IO.FileInfo(saveDialog.FileName).Length / 1024.0:F1} KB",
                        "Export Thành công",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi export: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Build a list of all features with their current enabled status
        /// </summary>
        private object BuildFeatureExportList()
        {
            var features = new System.Collections.Generic.List<object>();
            
            var versionDetail = _versionService.VersionDetail;
            if (versionDetail?.Features != null)
            {
                foreach (var category in versionDetail.Features)
                {
                    AddFeatureToList(features, category.Value, 0);
                }
            }
            
            return features;
        }

        private void AddFeatureToList(System.Collections.Generic.List<object> list, FeatureItem feature, int level)
        {
            list.Add(new
            {
                Id = feature.Id,
                Name = feature.Name,
                Level = level,
                Enabled = _featureManager.IsEnabled(feature.Id),
                ChildCount = feature.Children?.Count ?? 0
            });

            if (feature.Children != null)
            {
                foreach (var child in feature.Children.Values)
                {
                    AddFeatureToList(list, child, level + 1);
                }
            }
        }

        #endregion
    }
}
