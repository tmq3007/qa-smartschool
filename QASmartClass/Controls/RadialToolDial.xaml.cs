using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Path = System.Windows.Shapes.Path;
using QASmartTouch.Services;

namespace QASmartTouch.Controls
{
    /// <summary>
    /// QC_4.2 Radial Tool Dial + Orbital Arc Menu — vòng công cụ thường dùng của chế độ SMART TOUCH.
    /// Control chỉ lo giao diện / tương tác; mọi hành động thực thi được phát ra qua
    /// <see cref="ToolInvoked"/> và <see cref="OrbitalAction"/> để Form2 gọi đúng logic hiện có.
    /// Hỗ trợ đầy đủ Chuột, Cảm ứng (Touch/Multi-touch) và Bút (Stylus).
    /// </summary>
    public partial class RadialToolDial : UserControl
    {
        #region Geometry & Palette

        // Toạ độ tâm vòng trong rootCanvas (670 x 500)
        private const double Cx = 300;
        private const double Cy = 250;
        private const double RingSize = 300;
        private const double RingCenter = RingSize / 2;
        private const double OuterRadius = 144;
        private const double InnerRadius = 46;
        private const double LabelRadius = 96;
        private const double DragThreshold = 8;

        internal static Brush Freeze(Brush b) { b.Freeze(); return b; }
        internal static SolidColorBrush Hex(string hex) => (SolidColorBrush)Freeze(new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)));

        internal static readonly Brush BrandBrush = Hex("#2E86DE");
        internal static readonly Brush TextBrush = Hex("#2F3542");
        internal static readonly Brush SubTextBrush = Hex("#747D8C");
        internal static readonly Brush BorderBrushSoft = Hex("#E1E8ED");
        internal static readonly Brush ButtonBrush = Hex("#F1F2F6");
        internal static readonly Brush HoverBrush = Hex("#E3F2FD");
        internal static readonly Brush WhiteBrush = Freeze(new SolidColorBrush(Colors.White));

        private static readonly Brush WedgeDefault = Hex("#F8FAFC");
        private static readonly Brush WedgeHover = Hex("#E3F2FD");
        private static readonly Brush WedgePressed = Hex("#CFE5FA");
        private static readonly Brush CoreBorderDefault = Hex("#E1E8ED");

        /// <summary>Màu chủ đạo của từng trạng thái Orbital (theo thiết kế Stitch, Bút dùng màu nhấn Form2).</summary>
        internal static readonly Dictionary<string, Color> StateColors = new()
        {
            ["pen"] = (Color)ColorConverter.ConvertFromString("#2E86DE"),
            ["eraser"] = (Color)ColorConverter.ConvertFromString("#EF6C57"),
            ["select"] = (Color)ColorConverter.ConvertFromString("#4F46E5"),
            ["shapes"] = (Color)ColorConverter.ConvertFromString("#7C3AED"),
        };

        private static readonly Dictionary<string, string> StateTips = new()
        {
            ["pen"] = "Kéo để di chuyển • Chạm thu gọn",
            ["eraser"] = "Ctrl+Z: Hoàn tác · Kéo cung trái để đổi cỡ tẩy",
            ["select"] = "Khoanh vùng nét vẽ để dịch, nhận dạng OCR hoặc hỏi AI",
            ["shapes"] = "Chọn hình rồi kéo thả trên bảng để vẽ hình học 2D/3D",
        };

        #endregion

        #region Tool Definitions

        public sealed class ToolCatalogItem
        {
            public string Key { get; set; } = "";
            public string Label { get; set; } = "";
            public string SubLabel { get; set; } = "";
            public string Icon { get; set; } = "";
            public string HexColor { get; set; } = "#2E86DE";
        }

        private sealed class WedgeDef
        {
            public string Key = "";
            public string Label = "";
            public string Icon = "";
        }

        internal const string IconPen = "M20.71,7.04C21.1,6.65 21.1,6 20.71,5.63L18.37,3.29C18,2.9 17.35,2.9 16.96,3.29L15.12,5.12L18.87,8.87M3,17.25V21H6.75L17.81,9.93L14.06,6.18L3,17.25Z";
        internal const string IconEraser = "M16.24,3.56L21.19,8.5C21.97,9.29 21.97,10.55 21.19,11.34L12,20.53C10.44,22.09 7.91,22.09 6.34,20.53L2.81,17C2.03,16.21 2.03,14.95 2.81,14.16L13.41,3.56C14.2,2.78 15.46,2.78 16.24,3.56M4.22,15.58L7.76,19.11C8.54,19.9 9.8,19.9 10.59,19.11L14.12,15.58L9.17,10.63L4.22,15.58Z";
        internal const string IconSelect = "M4,4H6V6H4V4M8,4H10V6H8V4M12,4H14V6H12V4M16,4H18V6H16V4M18,8H20V10H18V8M18,12H20V14H18V12M18,16H20V18H18V16M4,8H6V10H4V8M4,12H6V14H4V12M4,16H6V18H4V16M8,18H10V20H8V18M12,18H14V20H12V18M16,18H18V20H16V18M4,18H6V20H4V18M18,4H20V6H18V4M8,8H16V16H8V8Z";
        internal const string IconShapes = "M11,13.5V21.5H3V13.5H11M12,2L17.5,11H6.5L12,2M17.5,13C20,13 22,15 22,17.5C22,20 20,22 17.5,22C15,22 13,20 13,17.5C13,15 15,13 17.5,13Z";
        internal const string IconUndo = "M12.5,8C9.85,8 7.45,9 5.6,10.6L2,7V16H11L7.38,12.38C8.77,11.22 10.54,10.5 12.5,10.5C16.04,10.5 19.05,12.81 20.1,16L22.47,15.22C21.08,11.03 17.15,8 12.5,8Z";
        internal const string IconRedo = "M18.4,10.6C16.55,9 14.15,8 11.5,8C6.85,8 2.92,11.03 1.54,15.22L3.9,16C4.95,12.81 7.95,10.5 11.5,10.5C13.45,10.5 15.23,11.22 16.62,12.38L13,16H22V7L18.4,10.6Z";

        public static readonly Dictionary<string, ToolCatalogItem> AvailableTools = new(StringComparer.OrdinalIgnoreCase)
        {
            ["pen"] = new ToolCatalogItem { Key = "pen", Label = "Bút viết", SubLabel = "Vẽ viết", Icon = IconPen, HexColor = "#2E86DE" },
            ["eraser"] = new ToolCatalogItem { Key = "eraser", Label = "Tẩy xóa", SubLabel = "Xóa nét/điểm", Icon = IconEraser, HexColor = "#EF6C57" },
            ["select"] = new ToolCatalogItem { Key = "select", Label = "Chọn vùng & AI", SubLabel = "Khoanh & AI", Icon = IconSelect, HexColor = "#4F46E5" },
            ["shapes"] = new ToolCatalogItem { Key = "shapes", Label = "Hình học", SubLabel = "2D & 3D", Icon = IconShapes, HexColor = "#7C3AED" },
            ["undo"] = new ToolCatalogItem { Key = "undo", Label = "Hoàn tác", SubLabel = "Quay lại", Icon = IconUndo, HexColor = "#D97706" },
            ["redo"] = new ToolCatalogItem { Key = "redo", Label = "Làm lại", SubLabel = "Tiến tới", Icon = IconRedo, HexColor = "#059669" },

            ["highlighter"] = new ToolCatalogItem { Key = "highlighter", Label = "Dạ quang", SubLabel = "Highlighter", Icon = "M12.5,2.5L14,4L5,13L3.5,11.5L12.5,2.5M19,16L20.5,17.5L18.5,19.5L17,18L19,16M11,15L12.5,16.5L10.5,18.5L9,17L11,15M20.7,11.3L18.7,9.3L20.2,7.8L22.2,9.8L20.7,11.3M2,22L5,19L3,17L0,20L2,22Z", HexColor = "#D97706" },
            ["laser"] = new ToolCatalogItem { Key = "laser", Label = "Bút Laser", SubLabel = "Chỉ bảng", Icon = "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4M12,9A3,3 0 0,0 9,12A3,3 0 0,0 12,15A3,3 0 0,0 15,12A3,3 0 0,0 12,9Z", HexColor = "#E11D48" },
            ["ruler"] = new ToolCatalogItem { Key = "ruler", Label = "Thước kẻ", SubLabel = "20 cm", Icon = "M2,7H22V17H2V7M4,9V12H5V9H7V11H8V9H10V12H11V9H13V11H14V9H16V12H17V9H19V11H20V9H20V15H4V9Z", HexColor = "#2563EB" },
            ["compass"] = new ToolCatalogItem { Key = "compass", Label = "Compa", SubLabel = "Vẽ tròn", Icon = "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,6A6,6 0 0,1 18,12A6,6 0 0,1 12,18A6,6 0 0,1 6,12A6,6 0 0,1 12,6Z", HexColor = "#0284C7" },
            ["setsquare"] = new ToolCatalogItem { Key = "setsquare", Label = "Eke góc", SubLabel = "45°/90°", Icon = "M2,2V22H22L2,2M5,7.8L16.2,19H5V7.8Z", HexColor = "#4338CA" },
            ["stickynote"] = new ToolCatalogItem { Key = "stickynote", Label = "Sticky Note", SubLabel = "Ghi chú", Icon = "M19,3H5A2,2 0 0,0 3,5V19A2,2 0 0,0 5,21H15L21,15V5A2,2 0 0,0 19,3M14,19.5V14H19.5L14,19.5Z", HexColor = "#7C3AED" },
            ["spotlight"] = new ToolCatalogItem { Key = "spotlight", Label = "Đèn soi", SubLabel = "Spotlight", Icon = "M9,21C9,21.5 9.5,22 10,22H14C14.5,22 15,21.5 15,21V20H9V21M12,2A7,7 0 0,0 5,9C5,11.38 6.19,13.47 8,14.74V17A1,1 0 0,0 9,18H15A1,1 0 0,0 16,17V14.74C17.81,13.47 19,11.38 19,9A7,7 0 0,0 12,2Z", HexColor = "#4F46E5" },
            ["curtain"] = new ToolCatalogItem { Key = "curtain", Label = "Màn che", SubLabel = "Che bài", Icon = "M4,4H20V6H4V4M4,7H11V19H4V7M13,7H20V19H13V7Z", HexColor = "#334155" },
            ["timer"] = new ToolCatalogItem { Key = "timer", Label = "Hẹn giờ", SubLabel = "Đếm ngược", Icon = "M12,20A7,7 0 0,1 5,13A7,7 0 0,1 12,6A7,7 0 0,1 19,13A7,7 0 0,1 12,20M19.03,7.39L20.45,5.97C20,5.46 19.55,5 19.04,4.56L17.62,6C16.07,4.74 14.12,4 12,4A9,9 0 0,0 3,13A9,9 0 0,0 12,22C17,22 21,17.97 21,13C21,10.88 20.26,8.93 19.03,7.39M11,14H13V8H11V14Z", HexColor = "#EA580C" },
            ["clearall"] = new ToolCatalogItem { Key = "clearall", Label = "Xóa bảng", SubLabel = "Làm mới", Icon = "M19.36,2.72L20.78,4.14L15.06,9.85C16.13,11.39 16.03,13.53 14.76,14.96C13.2,16.53 10.67,16.53 9.11,14.96C7.55,13.4 7.55,10.87 9.11,9.31C10.54,7.88 12.68,7.78 14.22,8.85L19.36,2.72M5.93,17.57C3.92,15.56 2.68,13 2.68,10.23H4.68C4.68,12.44 5.67,14.5 7.34,16.16L5.93,17.57Z", HexColor = "#DC2626" },
        };

        private static readonly (string Key, string Label, string Icon)[] SwitcherDefs =
        {
            ("pen", "1. Bút viết", IconPen),
            ("eraser", "2. Tẩy", IconEraser),
            ("select", "3. Chọn vùng & AI", IconSelect),
            ("shapes", "4. Hình học", IconShapes),
        };

        private sealed class WedgeVisual
        {
            public Path Shape = null!;
            public Path Icon = null!;
            public TextBlock Label = null!;
            public bool Hover;
            public bool Pressed;
        }

        private readonly Dictionary<string, WedgeVisual> _wedges = new();
        private readonly Dictionary<string, Button> _switcherButtons = new();

        /// <summary>Công cụ đang thực sự hoạt động trên bảng (đồng bộ từ Form2).</summary>
        private string? _activeKey;

        /// <summary>Trạng thái Orbital đang hiển thị (pen / eraser / select / shapes).</summary>
        private string _state = "pen";
        private bool _orbitalsVisible = true;

        #endregion

        /// <summary>Chạm một múi hoặc tab trạng thái. Tham số = mã công cụ.</summary>
        public event EventHandler<string>? ToolInvoked;

        /// <summary>Thao tác trên cung Orbital (độ dày, loại bút, màu, chế độ tẩy, chọn vùng, AI, hình học...).</summary>
        public event EventHandler<RadialOrbitalActionEventArgs>? OrbitalAction;

        /// <summary>Yêu cầu mở hộp thoại tùy biến 6 múi tròn.</summary>
        public event EventHandler? CustomizerRequested;

        public bool IsCollapsed { get; private set; }
        public string CurrentState => _state;

        public RadialToolDial()
        {
            InitializeComponent();
            BuildWedges();
            BuildSwitcher();
            AttachCoreInput();
            InitializeOrbitals();
            ApplyStateVisuals();

            btnTuneWedges.Click += (s, e) => CustomizerRequested?.Invoke(this, EventArgs.Empty);
            ToolbarPointerHelper.AttachTap(btnTuneWedges, () => CustomizerRequested?.Invoke(this, EventArgs.Empty), handleMouse: false);

            hintPill.SizeChanged += (s, e) => Canvas.SetLeft(hintPill, Cx - hintPill.ActualWidth / 2);
            switcherBar.SizeChanged += (s, e) => Canvas.SetLeft(switcherBar, Math.Max(0, Cx + 40 - switcherBar.ActualWidth / 2));

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        #region Public API

        /// <summary>Đánh dấu công cụ đang hoạt động (null = không có). Tự chuyển Orbital sang trạng thái tương ứng.</summary>
        public void SetActive(string? key)
        {
            if (key != null && !StateColors.ContainsKey(key)) key = null;
            if (_activeKey == key) return;
            _activeKey = key;
            if (key != null && key != _state)
            {
                _state = key;
                RebuildOrbitals();
            }
            ApplyStateVisuals();
        }

        /// <summary>Chuyển trạng thái Orbital (không thực thi công cụ).</summary>
        public void SwitchState(string state, bool showOrbitals = true)
        {
            if (!StateColors.ContainsKey(state)) return;
            bool changed = _state != state;
            _state = state;
            if (changed) RebuildOrbitals();
            SetOrbitalsVisible(showOrbitals);
            ApplyStateVisuals();
        }

        /// <summary>Thu gọn / mở rộng vòng công cụ (có hiệu ứng).</summary>
        public void SetCollapsed(bool collapsed, bool animate = true)
        {
            IsCollapsed = collapsed;
            double target = collapsed ? 0.0 : 1.0;
            ringLayer.IsHitTestVisible = !collapsed;
            switcherBar.IsHitTestVisible = !collapsed;
            if (collapsed) HidePalettePopover();

            var dur = TimeSpan.FromMilliseconds(animate ? 180 : 0);
            var ease = new CubicEase { EasingMode = collapsed ? EasingMode.EaseIn : EasingMode.EaseOut };
            ringScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(target, dur) { EasingFunction = ease });
            ringScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(target, dur) { EasingFunction = ease });
            ringLayer.BeginAnimation(OpacityProperty, new DoubleAnimation(target, dur));
            switcherBar.BeginAnimation(OpacityProperty, new DoubleAnimation(target, dur));

            UpdateOrbitalLayerVisibility(animate);

            hintText.Text = collapsed ? "Chạm để mở công cụ" : StateTips[_state];
            coreInner.Fill = collapsed ? new SolidColorBrush(StateColors[_state]) : ButtonBrush;
            coreIcon.Fill = collapsed ? WhiteBrush : TextBrush;
        }

        #endregion

        #region Wedges

        public void RebuildWedges()
        {
            wedgeCanvas.Children.Clear();
            _wedges.Clear();
            BuildWedges();
            ApplyStateVisuals();
        }

        private void BuildWedges()
        {
            var keys = (AppSettings.CustomRadialWedges ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var activeDefs = new List<WedgeDef>();

            for (int i = 0; i < 6; i++)
            {
                string key = (i < keys.Length) ? keys[i] : "";
                if (string.IsNullOrEmpty(key) || !AvailableTools.TryGetValue(key, out var item))
                {
                    key = i switch
                    {
                        0 => "pen",
                        1 => "eraser",
                        2 => "select",
                        3 => "shapes",
                        4 => "undo",
                        5 => "redo",
                        _ => "pen"
                    };
                    item = AvailableTools[key];
                }
                if (i == 0) item = AvailableTools["pen"]; // Múi 1 luôn cố định là Bút viết

                activeDefs.Add(new WedgeDef { Key = item.Key, Label = item.Label, Icon = item.Icon });
            }

            int count = activeDefs.Count;
            double span = 360.0 / count;

            for (int i = 0; i < count; i++)
            {
                var def = activeDefs[i];
                double mid = -60 + i * span;   // Múi 1 tâm tại -60° (300°)
                double a1 = mid - span / 2;
                double a2 = mid + span / 2;

                var shape = new Path
                {
                    Data = CreateWedgeGeometry(a1, a2),
                    Fill = WedgeDefault,
                    Stroke = WhiteBrush,
                    StrokeThickness = 3,
                    Cursor = Cursors.Hand,
                    ToolTip = def.Label,
                };
                wedgeCanvas.Children.Add(shape);

                double rad = mid * Math.PI / 180.0;
                double lx = RingCenter + LabelRadius * Math.Cos(rad);
                double ly = RingCenter + LabelRadius * Math.Sin(rad);

                var icon = new Path { Data = Geometry.Parse(def.Icon), Fill = TextBrush };
                var label = new TextBlock
                {
                    Text = def.Label,
                    FontSize = 11.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = TextBrush,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 3, 0, 0),
                };
                label.SetResourceReference(TextBlock.FontFamilyProperty, "InterFont");

                var content = new StackPanel
                {
                    Width = 84,
                    Height = 50,
                    IsHitTestVisible = false,
                    Children =
                    {
                        new Viewbox { Width = 24, Height = 24, Child = new Canvas { Width = 24, Height = 24, Children = { icon } } },
                        label
                    },
                };
                Canvas.SetLeft(content, lx - 42);
                Canvas.SetTop(content, ly - 25);
                wedgeCanvas.Children.Add(content);

                var visual = new WedgeVisual { Shape = shape, Icon = icon, Label = label };
                _wedges[def.Key] = visual;
                string key = def.Key;

                shape.MouseEnter += (s, e) =>
                {
                    if (e.StylusDevice != null) return;
                    visual.Hover = true;
                    RefreshWedge(key, visual);
                };
                shape.MouseLeave += (s, e) =>
                {
                    visual.Hover = false;
                    RefreshWedge(key, visual);
                };

                ToolbarPointerHelper.AttachTap(shape,
                    onTap: () => OnWedgeTapped(key),
                    onPressedChanged: pressed =>
                    {
                        visual.Pressed = pressed;
                        RefreshWedge(key, visual);
                    });
            }
        }

        private void OnWedgeTapped(string key)
        {
            if (StateColors.ContainsKey(key))
            {
                // Chạm lại đúng múi đang mở → ẩn/hiện cung Orbital; chạm múi khác → chuyển trạng thái
                bool sameState = key == _state;
                if (sameState) SetOrbitalsVisible(!_orbitalsVisible);
                else SwitchState(key, true);
            }
            ToolInvoked?.Invoke(this, key);
        }

        private void ApplyStateVisuals()
        {
            foreach (var kv in _wedges) RefreshWedge(kv.Key, kv.Value);
            foreach (var kv in _switcherButtons) RefreshSwitcherButton(kv.Key, kv.Value);

            hintText.Text = IsCollapsed ? "Chạm để mở công cụ" : StateTips[_state];
            hintDot.Fill = new SolidColorBrush(StateColors[_state]);
        }

        private void RefreshWedge(string key, WedgeVisual v)
        {
            bool isState = StateColors.TryGetValue(key, out var color) && key == _state;
            bool isActive = isState && key == _activeKey;

            if (isActive)
            {
                v.Shape.Fill = new SolidColorBrush(Color.FromArgb(235, color.R, color.G, color.B));
                v.Icon.Fill = WhiteBrush;
                v.Label.Foreground = WhiteBrush;
            }
            else if (isState)
            {
                v.Shape.Fill = v.Pressed
                    ? new SolidColorBrush(Color.FromArgb(90, color.R, color.G, color.B))
                    : new SolidColorBrush(Color.FromArgb(46, color.R, color.G, color.B));
                v.Icon.Fill = new SolidColorBrush(color);
                v.Label.Foreground = new SolidColorBrush(color);
            }
            else
            {
                v.Shape.Fill = v.Pressed ? WedgePressed : (v.Hover ? WedgeHover : WedgeDefault);
                v.Icon.Fill = TextBrush;
                v.Label.Foreground = TextBrush;
            }
        }

        private static Geometry CreateWedgeGeometry(double startDeg, double endDeg)
        {
            Point P(double r, double deg)
            {
                double rad = deg * Math.PI / 180.0;
                return new Point(RingCenter + r * Math.Cos(rad), RingCenter + r * Math.Sin(rad));
            }

            bool large = (endDeg - startDeg) > 180;
            var fig = new PathFigure { StartPoint = P(OuterRadius, startDeg), IsClosed = true, IsFilled = true };
            fig.Segments.Add(new ArcSegment(P(OuterRadius, endDeg), new Size(OuterRadius, OuterRadius), 0, large, SweepDirection.Clockwise, true));
            fig.Segments.Add(new LineSegment(P(InnerRadius, endDeg), true));
            fig.Segments.Add(new ArcSegment(P(InnerRadius, startDeg), new Size(InnerRadius, InnerRadius), 0, large, SweepDirection.Counterclockwise, true));

            var geo = new PathGeometry();
            geo.Figures.Add(fig);
            geo.Freeze();
            return geo;
        }

        #endregion

        #region Switcher Bar

        private void BuildSwitcher()
        {
            foreach (var (key, label, iconData) in SwitcherDefs)
            {
                var icon = new Path { Data = Geometry.Parse(iconData), Fill = TextBrush };
                var text = new TextBlock
                {
                    Text = label,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(6, 0, 0, 0),
                };
                text.SetResourceReference(TextBlock.FontFamilyProperty, "InterFont");

                var btn = new Button
                {
                    Height = 34,
                    Padding = new Thickness(12, 0, 12, 0),
                    Margin = new Thickness(1, 0, 1, 0),
                    Cursor = Cursors.Hand,
                    Focusable = false,
                    BorderThickness = new Thickness(0),
                    Template = CreatePillTemplate(17),
                    Content = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Children =
                        {
                            new Viewbox { Width = 15, Height = 15, VerticalAlignment = VerticalAlignment.Center,
                                          Child = new Canvas { Width = 24, Height = 24, Children = { icon } } },
                            text
                        }
                    },
                    Tag = icon,
                };
                string k = key;
                btn.Click += (s, e) =>
                {
                    SwitchState(k, true);
                    ToolInvoked?.Invoke(this, k);
                };
                AttachButtonTouch(btn);
                switcherHost.Children.Add(btn);
                _switcherButtons[key] = btn;
            }
        }

        private void RefreshSwitcherButton(string key, Button btn)
        {
            bool on = key == _state;
            var color = StateColors[key];
            btn.Background = on ? new SolidColorBrush(color) : Brushes.Transparent;
            btn.Foreground = on ? WhiteBrush : TextBrush;
            if (btn.Tag is Path icon) icon.Fill = on ? WhiteBrush : TextBrush;
        }

        #endregion

        #region Shared UI helpers

        /// <summary>Template viên thuốc (pill) đơn giản, Background/Foreground theo Button.</summary>
        internal static ControlTemplate CreatePillTemplate(double radius)
        {
            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(radius));
            border.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding(nameof(Background)) { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            border.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding(nameof(BorderBrush)) { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            border.SetBinding(Border.BorderThicknessProperty, new System.Windows.Data.Binding(nameof(BorderThickness)) { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            border.SetBinding(Border.PaddingProperty, new System.Windows.Data.Binding(nameof(Padding)) { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            presenter.SetBinding(ContentPresenter.HorizontalAlignmentProperty, new System.Windows.Data.Binding(nameof(HorizontalContentAlignment)) { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            border.AppendChild(presenter);
            template.VisualTree = border;
            return template;
        }

        /// <summary>Chạm/bút 1 lần là kích hoạt Click (chuột vẫn do Button.Click xử lý).</summary>
        internal static void AttachButtonTouch(Button btn)
        {
            ToolbarPointerHelper.AttachTap(btn,
                () => btn.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent, btn)),
                handleMouse: false);
        }

        #endregion

        #region Core: Drag + Tap (Mouse / Touch / Stylus)

        private enum PointerSource { None, Mouse, Touch, Stylus }

        private PointerSource _source = PointerSource.None;
        private TouchDevice? _touchDevice;
        private Point _pressPoint;
        private Vector _startOffset;
        private bool _dragging;

        private void AttachCoreInput()
        {
            Stylus.SetIsPressAndHoldEnabled(coreButton, false);
            Stylus.SetIsFlicksEnabled(coreButton, false);

            coreButton.PreviewStylusDown += (s, e) =>
            {
                if (_source != PointerSource.None) { e.Handled = true; return; }
                if (!TryGetParentPoint(e.GetPosition, out var p)) return;
                _source = PointerSource.Stylus;
                e.StylusDevice.Capture(coreButton);
                BeginPointer(p);
                e.Handled = true;
            };
            coreButton.PreviewStylusMove += (s, e) =>
            {
                if (_source != PointerSource.Stylus) return;
                if (TryGetParentPoint(e.GetPosition, out var p)) MovePointer(p);
                e.Handled = true;
            };
            coreButton.PreviewStylusUp += (s, e) =>
            {
                if (_source != PointerSource.Stylus) { e.Handled = true; return; }
                EndPointer();
                if (e.StylusDevice.Captured == coreButton) coreButton.ReleaseStylusCapture();
                e.Handled = true;
            };
            coreButton.LostStylusCapture += (s, e) =>
            {
                if (_source == PointerSource.Stylus) EndPointer(cancel: true);
            };

            coreButton.PreviewTouchDown += (s, e) =>
            {
                if (_source != PointerSource.None) { e.Handled = true; return; }
                if (Parent is not UIElement parent) return;
                _source = PointerSource.Touch;
                _touchDevice = e.TouchDevice;
                e.TouchDevice.Capture(coreButton);
                BeginPointer(e.GetTouchPoint(parent).Position);
                e.Handled = true;
            };
            coreButton.PreviewTouchMove += (s, e) =>
            {
                if (_source != PointerSource.Touch || e.TouchDevice != _touchDevice) return;
                if (Parent is UIElement parent) MovePointer(e.GetTouchPoint(parent).Position);
                e.Handled = true;
            };
            coreButton.PreviewTouchUp += (s, e) =>
            {
                if (_source != PointerSource.Touch || e.TouchDevice != _touchDevice) { e.Handled = true; return; }
                EndPointer();
                coreButton.ReleaseTouchCapture(e.TouchDevice);
                e.Handled = true;
            };

            coreButton.MouseLeftButtonDown += (s, e) =>
            {
                if (e.StylusDevice != null || _source != PointerSource.None) return;
                if (!TryGetParentPoint(e.GetPosition, out var p)) return;
                _source = PointerSource.Mouse;
                coreButton.CaptureMouse();
                BeginPointer(p);
                e.Handled = true;
            };
            coreButton.MouseMove += (s, e) =>
            {
                if (_source != PointerSource.Mouse || e.StylusDevice != null) return;
                if (TryGetParentPoint(e.GetPosition, out var p)) MovePointer(p);
            };
            coreButton.MouseLeftButtonUp += (s, e) =>
            {
                if (_source != PointerSource.Mouse || e.StylusDevice != null) return;
                EndPointer();
                coreButton.ReleaseMouseCapture();
                e.Handled = true;
            };
            coreButton.LostMouseCapture += (s, e) =>
            {
                if (_source == PointerSource.Mouse) EndPointer(cancel: true);
            };
        }

        private bool TryGetParentPoint(Func<IInputElement, Point> getPosition, out Point p)
        {
            p = default;
            if (Parent is not UIElement parent) return false;
            p = getPosition(parent);
            return true;
        }

        private void BeginPointer(Point p)
        {
            _pressPoint = p;
            _startOffset = new Vector(dragTransform.X, dragTransform.Y);
            _dragging = false;
            coreButton.BorderBrush = BrandBrush;
        }

        private void MovePointer(Point p)
        {
            if (_source == PointerSource.None) return;
            Vector delta = p - _pressPoint;
            if (!_dragging && delta.Length < DragThreshold) return;
            _dragging = true;
            ApplyOffset(_startOffset + delta);
        }

        private void EndPointer(bool cancel = false)
        {
            if (_source == PointerSource.None) return;
            bool wasDragging = _dragging;
            _source = PointerSource.None;
            _touchDevice = null;
            _dragging = false;
            coreButton.BorderBrush = CoreBorderDefault;

            if (!cancel && !wasDragging)
            {
                Dispatcher.BeginInvoke(new Action(() => SetCollapsed(!IsCollapsed)),
                    System.Windows.Threading.DispatcherPriority.Input);
            }
        }

        #endregion

        #region Position Clamping

        private FrameworkElement? _parentFe;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _parentFe = Parent as FrameworkElement;
            if (_parentFe != null) _parentFe.SizeChanged += Parent_SizeChanged;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_parentFe != null) _parentFe.SizeChanged -= Parent_SizeChanged;
            _parentFe = null;
        }

        private void Parent_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => ApplyOffset(new Vector(dragTransform.X, dragTransform.Y))),
                System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>
        /// Áp dụng độ dịch chuyển. Giới hạn theo VÒNG TRÒN (không theo toàn bộ vùng Orbital)
        /// để giáo viên có thể kéo dial sát mép màn hình.
        /// </summary>
        private void ApplyOffset(Vector desired)
        {
            if (Parent is not FrameworkElement parent || ActualWidth <= 0 || ActualHeight <= 0)
            {
                dragTransform.X = desired.X;
                dragTransform.Y = desired.Y;
                return;
            }

            Point current = TranslatePoint(new Point(0, 0), parent);
            double baseX = current.X - dragTransform.X;
            double baseY = current.Y - dragTransform.Y;

            double minY = 0, maxY = parent.ActualHeight;
            if (parent is Grid g && g.RowDefinitions.Count > 0)
            {
                int row = Math.Min(Grid.GetRow(this), g.RowDefinitions.Count - 1);
                minY = g.RowDefinitions[row].Offset;
                maxY = minY + g.RowDefinitions[row].ActualHeight;
            }

            // Hộp giới hạn = vòng tròn (Cx ± 150, Cy ± 150)
            double ringLeft = Cx - RingCenter, ringTop = Cy - RingCenter;
            double minTx = -baseX - ringLeft;
            double maxTx = parent.ActualWidth - baseX - ringLeft - RingSize;
            double minTy = minY - baseY - ringTop;
            double maxTy = maxY - baseY - ringTop - RingSize;

            dragTransform.X = maxTx < minTx ? minTx : Math.Clamp(desired.X, minTx, maxTx);
            dragTransform.Y = maxTy < minTy ? minTy : Math.Clamp(desired.Y, minTy, maxTy);
        }

        #endregion
    }

    /// <summary>Tham số sự kiện Orbital: Action = mã thao tác, Value = giá trị (int, string, Color...).</summary>
    public sealed class RadialOrbitalActionEventArgs : EventArgs
    {
        public RadialOrbitalActionEventArgs(string action, object? value = null)
        {
            Action = action;
            Value = value;
        }

        /// <summary>
        /// penSize(int) · penType(string) · penColor(Color) ·
        /// eraserSize(int) · eraserMode(string) · eraserClearAll ·
        /// selectMode(string: Rect/Lasso/Magic) · smartAction(string: OCR/Translate/Read/Search/AIChat) ·
        /// shape(string tên hình legacy)
        /// </summary>
        public string Action { get; }
        public object? Value { get; }
    }
}
