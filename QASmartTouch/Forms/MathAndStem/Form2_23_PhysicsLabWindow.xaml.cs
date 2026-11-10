using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using Microsoft.Web.WebView2.Core;
using QASmartTouch.Helpers;

namespace QASmartTouch.Forms
{
    public class PhysicsCatalogData
    {
        public Dictionary<string, string>? groups { get; set; }
        public List<PhysicsSimItem>? items { get; set; }
    }

    public class PhysicsSimItem
    {
        public string id { get; set; } = "";
        public string source { get; set; } = "";
        public string title { get; set; } = "";
        public string group { get; set; } = "";
        public string description { get; set; } = "";
        public bool three { get; set; }
        public bool illustrative { get; set; }
        public string kind { get; set; } = "simulation";
        public bool standalone { get; set; }
    }

    public partial class Form2_23_PhysicsLabWindow : Window
    {
        private Form2_MainDashboard? _mainDashboard;
        private PhysicsCatalogData? _catalog;
        private string _activeGroup = "";
        private string _searchText = "";
        private bool _isWebViewInitialized = false;
        private PhysicsSimItem? _activeSimItem;
        private string _activeSimUrl = "";

        public Form2_23_PhysicsLabWindow()
        {
            InitializeComponent();
            TouchActivationHelper.ApplyToWindow(this);
            TouchActivationHelper.WireButton(btnCloseLibrary);
            TouchActivationHelper.WireButton(btnBackToLibrary);
            TouchActivationHelper.WireButton(btnPinToBoard);
            TouchActivationHelper.WireButton(btnClosePlayer);

            Loaded += Form2_23_PhysicsLabWindow_Loaded;
        }

        public void SetMainDashboard(Form2_MainDashboard dashboard)
        {
            _mainDashboard = dashboard;
        }

        private void Form2_23_PhysicsLabWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCatalog();
            RenderCategoryPills();
            RenderCards();
        }

        private void LoadCatalog()
        {
            try
            {
                string assetsDir = GetPhysicsAssetsDirectory();
                string jsonPath = Path.Combine(assetsDir, "catalog.json");
                if (File.Exists(jsonPath))
                {
                    string json = File.ReadAllText(jsonPath);
                    _catalog = JsonSerializer.Deserialize<PhysicsCatalogData>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PhysicsLab] Error loading catalog: {ex.Message}");
            }
        }

        private void RenderCategoryPills()
        {
            pnlCategories.Children.Clear();
            if (_catalog?.groups == null) return;

            // Nút "Tất cả"
            var allBtn = CreatePillButton("Tất cả (46)", "");
            pnlCategories.Children.Add(allBtn);

            foreach (var kvp in _catalog.groups)
            {
                int count = _catalog.items?.Count(i => i.group == kvp.Key) ?? 0;
                string label = $"{kvp.Value} ({count})";
                var pill = CreatePillButton(label, kvp.Key);
                pnlCategories.Children.Add(pill);
            }
        }

        private Button CreatePillButton(string label, string groupKey)
        {
            bool isActive = _activeGroup == groupKey;
            var btn = new Button
            {
                Content = label,
                Tag = groupKey,
                Padding = new Thickness(14, 7, 14, 7),
                Margin = new Thickness(0, 0, 8, 0),
                Background = isActive ? new SolidColorBrush(Color.FromRgb(0, 105, 92)) : new SolidColorBrush(Color.FromRgb(240, 242, 245)),
                Foreground = isActive ? Brushes.White : new SolidColorBrush(Color.FromRgb(55, 71, 79)),
                BorderThickness = new Thickness(0),
                FontSize = 13,
                FontWeight = isActive ? FontWeights.Bold : FontWeights.Normal,
                Cursor = System.Windows.Input.Cursors.Hand
            };

            btn.Template = CreatePillTemplate(isActive);
            TouchActivationHelper.WireButton(btn);

            btn.Click += (s, e) =>
            {
                _activeGroup = groupKey;
                RenderCategoryPills();
                RenderCards();
            };

            return btn;
        }

        private ControlTemplate CreatePillTemplate(bool isActive)
        {
            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(16));
            border.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Button.PaddingProperty));

            var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(contentPresenter);

            template.VisualTree = border;
            return template;
        }

        private void RenderCards()
        {
            pnlCards.Children.Clear();
            if (_catalog?.items == null) return;

            string searchNorm = (_searchText ?? "").Trim().ToLower();

            var filtered = _catalog.items.Where(item =>
            {
                if (!string.IsNullOrEmpty(_activeGroup) && item.group != _activeGroup)
                    return false;

                if (!string.IsNullOrEmpty(searchNorm))
                {
                    string groupName = _catalog.groups != null && _catalog.groups.ContainsKey(item.group) ? _catalog.groups[item.group] : "";
                    string combined = $"{item.title} {item.description} {groupName}".ToLower();
                    if (!combined.Contains(searchNorm))
                        return false;
                }

                return true;
            }).ToList();

            if (filtered.Count == 0)
            {
                var emptyNotice = new TextBlock
                {
                    Text = "Không tìm thấy mô phỏng nào phù hợp với từ khóa tìm kiếm.",
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(120, 144, 156)),
                    Margin = new Thickness(20)
                };
                pnlCards.Children.Add(emptyNotice);
                return;
            }

            foreach (var item in filtered)
            {
                pnlCards.Children.Add(CreateSimCard(item));
            }
        }

        private FrameworkElement CreateSimCard(PhysicsSimItem item)
        {
            var cardBorder = new Border
            {
                Width = 360,
                Height = 195,
                Margin = new Thickness(0, 0, 16, 16),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 235, 233)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 270,
                    ShadowDepth = 2,
                    BlurRadius = 8,
                    Opacity = 0.08
                }
            };

            var grid = new Grid { Margin = new Thickness(16, 14, 16, 14) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Badge row
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Title
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Desc
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Action buttons

            // 1. Badge Row
            var badgePanel = new StackPanel { Orientation = Orientation.Horizontal };
            string groupName = _catalog?.groups != null && _catalog.groups.ContainsKey(item.group) ? _catalog.groups[item.group] : item.group;

            var grpBadge = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(232, 245, 243)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(0, 0, 6, 0)
            };
            grpBadge.Child = new TextBlock
            {
                Text = groupName.ToUpper(),
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 121, 107))
            };
            badgePanel.Children.Add(grpBadge);

            if (item.three)
            {
                var threeBadge = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(237, 231, 246)),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(6, 2, 6, 2)
                };
                threeBadge.Child = new TextBlock
                {
                    Text = "🎮 3D TƯƠNG TÁC",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(103, 58, 183))
                };
                badgePanel.Children.Add(threeBadge);
            }
            Grid.SetRow(badgePanel, 0);
            grid.Children.Add(badgePanel);

            // 2. Title
            var titleBlock = new TextBlock
            {
                Text = item.title,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(38, 50, 56)),
                Margin = new Thickness(0, 8, 0, 4),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetRow(titleBlock, 1);
            grid.Children.Add(titleBlock);

            // 3. Description
            var descBlock = new TextBlock
            {
                Text = item.description,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(84, 110, 122)),
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                LineHeight = 17,
                MaxHeight = 36
            };
            Grid.SetRow(descBlock, 2);
            grid.Children.Add(descBlock);

            // 4. Action Buttons
            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 8, 0, 0)
            };

            var btnPin = new Button
            {
                Content = "📌 Ghim vào bảng",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Padding = new Thickness(10, 6, 10, 6),
                Background = new SolidColorBrush(Color.FromRgb(240, 247, 244)),
                Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(165, 214, 167)),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(0, 0, 8, 0)
            };
            TouchActivationHelper.WireButton(btnPin);
            btnPin.Click += (s, e) => PinToBoardDirectly(item);
            btnPanel.Children.Add(btnPin);

            var btnOpen = new Button
            {
                Content = "▶ Mở xem",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(12, 6, 12, 6),
                Background = new SolidColorBrush(Color.FromRgb(0, 121, 107)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            TouchActivationHelper.WireButton(btnOpen);
            btnOpen.Click += (s, e) => OpenSimulation(item);
            btnPanel.Children.Add(btnOpen);

            Grid.SetRow(btnPanel, 3);
            grid.Children.Add(btnPanel);

            cardBorder.Child = grid;
            return cardBorder;
        }

        private string GetSimulationUrl(PhysicsSimItem item)
        {
            string token = "0123456789abcdef0123456789abcdef";
            if (item.standalone)
            {
                return $"https://physics.local/{item.source}#token={token}&id={item.id}";
            }
            return $"https://physics.local/player.html#id={item.id}&token={token}";
        }

        public static string GetPhysicsAssetsDirectory()
        {
            string p1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Simulations", "Physics");
            if (Directory.Exists(p1) && File.Exists(Path.Combine(p1, "player.html"))) return p1;

            string p2 = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\QASmartTouch\Assets\Simulations\Physics"));
            if (Directory.Exists(p2) && File.Exists(Path.Combine(p2, "player.html"))) return p2;

            return p1;
        }

        private async void OpenSimulation(PhysicsSimItem item)
        {
            _activeSimItem = item;
            _activeSimUrl = GetSimulationUrl(item);

            viewLibrary.Visibility = Visibility.Collapsed;
            viewPlayer.Visibility = Visibility.Visible;
            playerLoadingOverlay.Visibility = Visibility.Collapsed; // Không chặn hiển thị

            txtPlayerTitle.Text = item.title;
            string groupName = _catalog?.groups != null && _catalog.groups.ContainsKey(item.group) ? _catalog.groups[item.group] : item.group;
            txtPlayerCategory.Text = $"{groupName} · {(item.three ? "3D Tương tác" : "Mô phỏng 2D")}";

            await InitializePlayerWebViewAsync();

            if (webViewPlayer.CoreWebView2 != null)
            {
                webViewPlayer.CoreWebView2.Navigate(_activeSimUrl);
            }
            else
            {
                webViewPlayer.Source = new Uri(_activeSimUrl);
            }
        }

        private async Task InitializePlayerWebViewAsync()
        {
            if (_isWebViewInitialized) return;

            try
            {
                await WebView2Helper.EnsureInitializedAsync(webViewPlayer);

                if (webViewPlayer.CoreWebView2 != null)
                {
                    string assetsDir = GetPhysicsAssetsDirectory();
                    if (Directory.Exists(assetsDir))
                    {
                        webViewPlayer.CoreWebView2.SetVirtualHostNameToFolderMapping(
                            "physics.local",
                            assetsDir,
                            CoreWebView2HostResourceAccessKind.Allow);
                    }

                    webViewPlayer.CoreWebView2.Settings.IsScriptEnabled = true;
                    webViewPlayer.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;
                    webViewPlayer.CoreWebView2.Settings.IsWebMessageEnabled = true;
                    webViewPlayer.CoreWebView2.Settings.AreDevToolsEnabled = false;

                    webViewPlayer.CoreWebView2.NavigationCompleted += async (s, e) =>
                    {
                        playerLoadingOverlay.Visibility = Visibility.Collapsed;

                        // Tự động kích hoạt init nếu là mô phỏng core
                        if (_activeSimItem != null && !_activeSimItem.standalone)
                        {
                            try
                            {
                                string script = $@"
                                    window.postMessage({{
                                        nbPhysics: 1,
                                        token: '0123456789abcdef0123456789abcdef',
                                        type: 'init',
                                        scene: {{ id: '{_activeSimItem.id}', states: {{}}, refs: {{}} }}
                                    }}, '*');
                                    var st = document.getElementById('playerStatus');
                                    if (st) st.style.display = 'none';
                                ";
                                await webViewPlayer.CoreWebView2.ExecuteScriptAsync(script);
                            }
                            catch { }
                        }
                    };

                    _isWebViewInitialized = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PhysicsLab] Player init error: {ex.Message}");
                playerLoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void PinToBoardDirectly(PhysicsSimItem item)
        {
            string url = GetSimulationUrl(item);
            _mainDashboard?.InsertPhysicsSimulation(url, item.title);
            this.Close();
        }

        private void btnBackToLibrary_Click(object sender, RoutedEventArgs e)
        {
            viewPlayer.Visibility = Visibility.Collapsed;
            viewLibrary.Visibility = Visibility.Visible;
        }

        private void btnPinToBoard_Click(object sender, RoutedEventArgs e)
        {
            if (_activeSimItem != null && !string.IsNullOrEmpty(_activeSimUrl))
            {
                _mainDashboard?.InsertPhysicsSimulation(_activeSimUrl, _activeSimItem.title);
                this.Close();
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = txtSearch.Text;
            btnClearSearch.Visibility = string.IsNullOrEmpty(_searchText) ? Visibility.Collapsed : Visibility.Visible;
            if (txtSearchPlaceholder != null)
            {
                txtSearchPlaceholder.Visibility = string.IsNullOrEmpty(_searchText) ? Visibility.Visible : Visibility.Collapsed;
            }
            RenderCards();
        }

        private void btnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}