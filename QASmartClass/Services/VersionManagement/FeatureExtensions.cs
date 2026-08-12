using System.Windows;

namespace QASmartTouch.Services.VersionManagement
{
    /// <summary>
    /// Extension methods để dễ dàng sử dụng FeatureManager trong code-behind
    /// </summary>
    public static class FeatureExtensions
    {
        /// <summary>
        /// Ẩn/hiện UIElement dựa trên feature enabled
        /// </summary>
        /// <param name="element">UIElement cần ẩn/hiện</param>
        /// <param name="featurePath">Đường dẫn feature</param>
        /// <param name="useCollapsed">True = Collapsed, False = Hidden</param>
        public static void SetVisibilityByFeature(this UIElement element, string featurePath, bool useCollapsed = true)
        {
            bool isEnabled = FeatureManager.Instance.IsEnabled(featurePath);
            
            if (isEnabled)
            {
                element.Visibility = Visibility.Visible;
            }
            else
            {
                element.Visibility = useCollapsed ? Visibility.Collapsed : Visibility.Hidden;
            }
        }

        /// <summary>
        /// Kiểm tra và trả về Visibility dựa trên feature
        /// </summary>
        public static Visibility GetVisibilityByFeature(string featurePath, bool useCollapsed = true)
        {
            bool isEnabled = FeatureManager.Instance.IsEnabled(featurePath);
            
            if (isEnabled)
                return Visibility.Visible;
            
            return useCollapsed ? Visibility.Collapsed : Visibility.Hidden;
        }

        /// <summary>
        /// Enable/Disable UIElement dựa trên feature
        /// </summary>
        public static void SetEnabledByFeature(this UIElement element, string featurePath)
        {
            element.IsEnabled = FeatureManager.Instance.IsEnabled(featurePath);
        }

        /// <summary>
        /// Shortcut để kiểm tra feature enabled
        /// </summary>
        public static bool IsFeatureEnabled(this object _, string featurePath)
        {
            return FeatureManager.Instance.IsEnabled(featurePath);
        }
    }

    /// <summary>
    /// Static helper class cho quick access
    /// </summary>
    public static class Features
    {
        /// <summary>
        /// Kiểm tra feature có enabled không
        /// </summary>
        public static bool IsEnabled(string featurePath) => FeatureManager.Instance.IsEnabled(featurePath);

        /// <summary>
        /// Lấy Visibility dựa trên feature
        /// </summary>
        public static Visibility GetVisibility(string featurePath) => 
            IsEnabled(featurePath) ? Visibility.Visible : Visibility.Collapsed;

        #region Feature ID Constants - Để tránh hardcode strings

        // Drawing Tools
        public const string BRUSH = "brush";
        public const string HIGHLIGHTER = "highlighter";
        public const string ERASER = "eraser";

        // Shapes 2D - Lines
        public const string LINE = "line";
        public const string ARROW = "arrow";
        public const string DOUBLE_ARROW = "double_arrow";
        public const string RIGHT_ANGLE = "right_angle";
        public const string CURVE = "curve";

        // Shapes 2D - Rectangles
        public const string RECTANGLE = "rectangle";
        public const string ROUNDED_RECT = "rounded_rect";
        public const string CORNER_CUT_1 = "corner_cut_1";
        public const string CORNER_CUT_2 = "corner_cut_2";

        // Shapes 2D - Basic Shapes
        public const string ELLIPSE = "ellipse";
        public const string CIRCLE = "circle";
        public const string SQUARE = "square";
        public const string TRIANGLE = "triangle";
        public const string STAR = "star";
        public const string HEART = "heart";
        public const string DIAMOND = "diamond";

        // Shapes 3D
        public const string CUBE = "cube";
        public const string SPHERE = "sphere";
        public const string CYLINDER = "cylinder";
        public const string CONE = "cone";
        public const string PYRAMID = "pyramid";
        public const string PRISM = "prism";
        public const string TORUS = "torus";
        public const string TETRAHEDRON = "tetrahedron";

        // Charts
        public const string BAR_CHART = "bar_chart";
        public const string LINE_CHART = "line_chart";
        public const string PIE_CHART = "pie_chart";
        public const string AREA_CHART = "area_chart";
        public const string SCATTER_CHART = "scatter_chart";
        public const string RADAR_CHART = "radar_chart";

        // Math Tools
        public const string RULER = "ruler";
        public const string PROTRACTOR = "protractor";
        public const string SET_SQUARE = "set_square";
        public const string COMPASS_2D = "compass_2d";
        public const string COMPASS_3D = "compass_3d";
        public const string CALCULATOR = "calculator";
        public const string GRAPH_EDITOR = "graph_editor";

        // Board Management
        public const string SCREEN_CURTAIN = "screen_curtain";
        public const string SPOTLIGHT = "spotlight";
        public const string MAGNIFIER = "magnifier";
        public const string SCREENSHOT = "screenshot";

        // Utilities
        public const string COUNTDOWN_TIMER = "countdown_timer";
        public const string SCREEN_RECORDING = "screen_recording";
        public const string COLOR_PICKER = "color_picker";

        // Education
        public const string PERIODIC_TABLE = "periodic_table";
        public const string BOOK_VIEWER = "book_viewer";

        // Advanced
        public const string CAMERA_AI = "camera_ai";
        public const string MULTI_USER = "multi_user";

        #endregion
    }
}
