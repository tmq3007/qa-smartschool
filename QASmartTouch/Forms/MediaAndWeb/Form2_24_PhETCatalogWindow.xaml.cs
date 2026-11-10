using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using Microsoft.Web.WebView2.Core;
using QASmartTouch.Services.Tools;
using QASmartTouch.Helpers;

namespace QASmartTouch.Forms
{
    public partial class Form2_24_PhETCatalogWindow : Window
    {
        private Form2_MainDashboard? _mainDashboard;
        private PhETSubject _activeSubject = PhETSubject.All;
        private PhETSchoolLevel _activeLevel = PhETSchoolLevel.All;
        private string _searchText = "";
        private bool _isWebViewInitialized = false;
        private PhETSimulationItem? _activeSimItem;
        private Action<string, string>? _onSimulationSelected;

        public Form2_24_PhETCatalogWindow()
        {
            InitializeComponent();
            TouchActivationHelper.ApplyToWindow(this);
            TouchActivationHelper.WireButton(btnCloseLibrary);
            TouchActivationHelper.WireButton(btnBackToLibrary);
            TouchActivationHelper.WireButton(btnPinToBoard);
            TouchActivationHelper.WireButton(btnReload);
            TouchActivationHelper.WireButton(btnClosePlayer);

            Loaded += Form2_24_PhETCatalogWindow_Loaded;
        }

        public void SetMainDashboard(Form2_MainDashboard dashboard)
        {
            _mainDashboard = dashboard;
        }

        public void SetTargetWidget(Action<string, string> onSimulationSelected)
        {
            _onSimulationSelected = onSimulationSelected;
        }

        private void Form2_24_PhETCatalogWindow_Loaded(object sender, RoutedEventArgs e)
        {
            RenderSubjectPills();
            RenderLevelPills();
            RenderCards();
        }

        private void RenderSubjectPills()
        {
            pnlSubjectCategories.Children.Clear();

            var allItems = PhETCatalogService.GetAll();
            int total = allItems.Count;
            int mathCount = allItems.Count(i => i.Subject == "math");
            int physCount = allItems.Count(i => i.Subject == "physics");
            int chemCount = allItems.Count(i => i.Subject == "chemistry");
            int bioCount = allItems.Count(i => i.Subject == "biology");

            txtTotalBadge.Text = $"{total} Mô Phỏng Chuẩn SGK";

            pnlSubjectCategories.Children.Add(CreateSubjectPill($"Tất cả môn ({total})", PhETSubject.All));
            pnlSubjectCategories.Children.Add(CreateSubjectPill($"📐 Toán học ({mathCount})", PhETSubject.Math));
            pnlSubjectCategories.Children.Add(CreateSubjectPill($"⚡ Vật lí ({physCount})", PhETSubject.Physics));
            pnlSubjectCategories.Children.Add(CreateSubjectPill($"🧪 Hóa học ({chemCount})", PhETSubject.Chemistry));
            pnlSubjectCategories.Children.Add(CreateSubjectPill($"🧬 Sinh học ({bioCount})", PhETSubject.Biology));
        }

        private Button CreateSubjectPill(string label, PhETSubject subject)
        {
            bool isActive = _activeSubject == subject;
            var btn = new Button
            {
                Content = label,
                Padding = new Thickness(14, 7, 14, 7),
                Margin = new Thickness(0, 0, 8, 0),
                Background = isActive ? new SolidColorBrush(Color.FromRgb(22, 139, 130)) : new SolidColorBrush(Color.FromRgb(240, 242, 245)),
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
                _activeSubject = subject;
                RenderSubjectPills();
                RenderCards();
            };

            return btn;
        }

        private void RenderLevelPills()
        {
            pnlLevelCategories.Children.Clear();

            pnlLevelCategories.Children.Add(CreateLevelPill("Tất cả cấp", PhETSchoolLevel.All));
            pnlLevelCategories.Children.Add(CreateLevelPill("THCS (Lớp 6–9)", PhETSchoolLevel.THCS));
            pnlLevelCategories.Children.Add(CreateLevelPill("THPT (Lớp 10–12)", PhETSchoolLevel.THPT));
        }

        private Button CreateLevelPill(string label, PhETSchoolLevel level)
        {
            bool isActive = _activeLevel == level;
            var btn = new Button
            {
                Content = label,
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 0, 8, 0),
                Background = isActive ? new SolidColorBrush(Color.FromRgb(69, 90, 100)) : new SolidColorBrush(Color.FromRgb(245, 247, 248)),
                Foreground = isActive ? Brushes.White : new SolidColorBrush(Color.FromRgb(84, 110, 122)),
                BorderThickness = new Thickness(0),
                FontSize = 12,
                FontWeight = isActive ? FontWeights.Bold : FontWeights.Normal,
                Cursor = System.Windows.Input.Cursors.Hand
            };

            btn.Template = CreatePillTemplate(isActive);
            TouchActivationHelper.WireButton(btn);

            btn.Click += (s, e) =>
            {
                _activeLevel = level;
                RenderLevelPills();
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

            var items = PhETCatalogService.Filter(_activeSubject, _activeLevel, _searchText).ToList();

            if (items.Count == 0)
            {
                var emptyNotice = new TextBlock
                {
                    Text = "Không tìm thấy mô phỏng nào phù hợp với bộ lọc hoặc từ khóa tìm kiếm.",
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(120, 144, 156)),
                    Margin = new Thickness(20)
                };
                pnlCards.Children.Add(emptyNotice);
                return;
            }

            foreach (var item in items)
            {
                pnlCards.Children.Add(CreateSimCard(item));
            }
        }

        private FrameworkElement CreateSimCard(PhETSimulationItem item)
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

            // Subject Badge
            Color subjBg = item.Subject switch
            {
                "math" => Color.FromRgb(232, 245, 233),
                "physics" => Color.FromRgb(227, 242, 253),
                "chemistry" => Color.FromRgb(255, 243, 224),
                "biology" => Color.FromRgb(252, 228, 236),
                _ => Color.FromRgb(240, 242, 245)
            };
            Color subjFg = item.Subject switch
            {
                "math" => Color.FromRgb(46, 125, 50),
                "physics" => Color.FromRgb(21, 101, 192),
                "chemistry" => Color.FromRgb(230, 81, 0),
                "biology" => Color.FromRgb(194, 24, 91),
                _ => Color.FromRgb(55, 71, 79)
            };

            var grpBadge = new Border
            {
                Background = new SolidColorBrush(subjBg),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(0, 0, 6, 0)
            };
            grpBadge.Child = new TextBlock
            {
                Text = item.SubjectName.ToUpper(),
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(subjFg)
            };
            badgePanel.Children.Add(grpBadge);

            // Grade Badge
            var gradeBadge = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(237, 231, 246)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(0, 0, 6, 0)
            };
            gradeBadge.Child = new TextBlock
            {
                Text = item.Grade.ToUpper(),
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(103, 58, 183))
            };
            badgePanel.Children.Add(gradeBadge);

            // HTML5 Badge
            var html5Badge = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(232, 245, 243)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2)
            };
            html5Badge.Child = new TextBlock
            {
                Text = "HTML5",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 121, 107))
            };
            badgePanel.Children.Add(html5Badge);

            Grid.SetRow(badgePanel, 0);
            grid.Children.Add(badgePanel);

            // 2. Title
            var titleBlock = new TextBlock
            {
                Text = item.Title,
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
                Text = item.Topic,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(84, 110, 122)),
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                LineHeight = 17,
                MaxHeight = 36
            };
            Grid.SetRow(descBlock, 2);
            grid.Children.Add(descBlock);

            // 4. Action Buttons (Thao tác chuẩn tương tự Form2_23_PhysicsLabWindow)
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

        private void PinToBoardDirectly(PhETSimulationItem item)
        {
            if (_onSimulationSelected != null)
            {
                _onSimulationSelected(item.SimUrl, item.Title);
            }
            else
            {
                _mainDashboard?.InsertPhETSimulation(item.SimUrl, item.Title);
            }
            this.Close();
        }

        private async void OpenSimulation(PhETSimulationItem item)
        {
            _activeSimItem = item;

            viewLibrary.Visibility = Visibility.Collapsed;
            viewPlayer.Visibility = Visibility.Visible;
            playerLoadingOverlay.Visibility = Visibility.Visible;

            txtPlayerTitle.Text = item.Title;
            txtPlayerCategory.Text = $"{item.SubjectName} · {item.Grade} · GDPT 2018";

            await InitializePlayerWebViewAsync();

            if (webViewPlayer.CoreWebView2 != null)
            {
                webViewPlayer.CoreWebView2.Navigate(item.SimUrl);
            }
            else
            {
                webViewPlayer.Source = new Uri(item.SimUrl);
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
                    webViewPlayer.CoreWebView2.Settings.IsScriptEnabled = true;
                    webViewPlayer.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;
                    webViewPlayer.CoreWebView2.Settings.IsWebMessageEnabled = true;
                    webViewPlayer.CoreWebView2.Settings.AreDevToolsEnabled = false;

                    webViewPlayer.CoreWebView2.NavigationCompleted += (s, e) =>
                    {
                        playerLoadingOverlay.Visibility = Visibility.Collapsed;
                    };

                    _isWebViewInitialized = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PhETCatalog] Player init error: {ex.Message}");
                playerLoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void btnBackToLibrary_Click(object sender, RoutedEventArgs e)
        {
            viewPlayer.Visibility = Visibility.Collapsed;
            viewLibrary.Visibility = Visibility.Visible;
        }

        private void btnPinToBoard_Click(object sender, RoutedEventArgs e)
        {
            if (_activeSimItem != null)
            {
                if (_onSimulationSelected != null)
                {
                    _onSimulationSelected(_activeSimItem.SimUrl, _activeSimItem.Title);
                }
                else
                {
                    _mainDashboard?.InsertPhETSimulation(_activeSimItem.SimUrl, _activeSimItem.Title);
                }
                this.Close();
            }
        }

        private void btnReload_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                webViewPlayer.CoreWebView2?.Reload();
            }
            catch { }
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

