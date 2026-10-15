using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Path = System.Windows.Shapes.Path;

namespace QASmartTouch.Controls
{
    /// <summary>Tham số sự kiện chạm một công cụ ở Tầng 2 của Dock.</summary>
    public sealed class DockToolInvokedEventArgs : EventArgs
    {
        public DockToolInvokedEventArgs(string group, string tool)
        {
            Group = group;
            Tool = tool;
        }

        /// <summary>"insert" (Chèn nội dung) hoặc "expand" (Mở rộng).</summary>
        public string Group { get; }

        /// <summary>Khoá công cụ (vd: ruler, chart-bar, save...).</summary>
        public string Tool { get; }
    }

    /// <summary>
    /// QC_4.2 Corner Dock Menu — Dock góc dưới-trái của SMART TOUCH theo thiết kế Stitch.
    /// Tầng 1: thẻ dọc 5 mục. Tầng 2: bảng xổ ngang (Chèn nội dung / Mở rộng).
    /// Control chỉ phát sự kiện; Form2 gọi lại đúng logic hiện có.
    /// Hỗ trợ Chuột, Cảm ứng và Bút — chạm lần đầu là ăn ngay.
    /// </summary>
    public partial class CornerDockMenu : UserControl
    {
        #region Icons (Material Design, khung 24×24)

        private const string IcAddBox = "M17,13H13V17H11V13H7V11H11V7H13V11H17M19,3H5C3.89,3 3,3.89 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5C21,3.89 20.1,3 19,3Z";
        private const string IcMonitor = "M21,16H3V4H21M21,2H3C1.89,2 1,2.89 1,4V16A2,2 0 0,0 3,18H10V20H8V22H16V20H14V18H21A2,2 0 0,0 23,16V4C23,2.89 22.1,2 21,2Z";
        private const string IcSchool = "M12,3L1,9L12,15L21,10.09V17H23V9M5,13.18V17.18L12,21L19,17.18V13.18L12,17L5,13.18Z";
        private const string IcMoreHoriz = "M16,12A2,2 0 0,1 18,10A2,2 0 0,1 20,12A2,2 0 0,1 18,14A2,2 0 0,1 16,12M10,12A2,2 0 0,1 12,10A2,2 0 0,1 14,12A2,2 0 0,1 12,14A2,2 0 0,1 10,12M4,12A2,2 0 0,1 6,10A2,2 0 0,1 8,12A2,2 0 0,1 6,14A2,2 0 0,1 4,12Z";
        private const string IcPower = "M16.56,5.44L15.11,6.89C16.84,7.94 18,9.83 18,12A6,6 0 0,1 12,18A6,6 0 0,1 6,12C6,9.83 7.16,7.94 8.88,6.88L7.44,5.44C5.36,6.88 4,9.28 4,12A8,8 0 0,0 12,20A8,8 0 0,0 20,12C20,9.28 18.64,6.88 16.56,5.44M13,3H11V13H13";

        private const string IcRuler = "M1.39,18.36L3.16,16.6L4.58,18L5.64,16.95L4.22,15.54L5.64,14.12L8.11,16.6L9.17,15.54L6.7,13.06L8.11,11.65L9.53,13.06L10.59,12L9.17,10.59L10.59,9.17L13.06,11.65L14.12,10.59L11.65,8.11L13.06,6.7L14.47,8.11L15.54,7.05L14.12,5.64L15.54,4.22L18,6.7L19.07,5.64L17.3,3.87L18.72,2.46L21.54,5.28L4.22,22.6L1.39,18.36Z";
        private const string IcCircle = "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4Z";
        private const string IcTriangle = "M12,2L1,21H23L12,2M12,6L19.53,19H4.47L12,6Z";
        private const string IcProtractor = "M12,4A10,10 0 0,0 2,14V20H22V14A10,10 0 0,0 12,4M12,6A8,8 0 0,1 20,14V18H4V14A8,8 0 0,1 12,6M11,8V14H13V8H11Z";
        private const string IcKeyboard = "M19,10H17V8H19M19,13H17V11H19M16,10H14V8H16M16,13H14V11H16M16,17H8V15H16M7,10H5V8H7M7,13H5V11H7M8,11H10V13H8M8,8H10V10H8M11,11H13V13H11M11,8H13V10H11M20,5H4C2.89,5 2,5.89 2,7V17A2,2 0 0,0 4,19H20A2,2 0 0,0 22,17V7C22,5.89 21.1,5 20,5Z";
        private const string IcText = "M18.5,4L19.66,8.35L18.7,8.61C18.25,7.74 17.79,6.87 17.26,6.43C16.73,6 16.11,6 15.5,6H13V16.5C13,17 13,17.5 13.33,17.75C13.67,18 14.33,18 15,18V19H9V18C9.67,18 10.33,18 10.67,17.75C11,17.5 11,17 11,16.5V6H8.5C7.89,6 7.27,6 6.74,6.43C6.21,6.87 5.75,7.74 5.3,8.61L4.34,8.35L5.5,4H18.5Z";
        private const string IcTable = "M5,4H19A2,2 0 0,1 21,6V18A2,2 0 0,1 19,20H5A2,2 0 0,1 3,18V6A2,2 0 0,1 5,4M5,8V12H11V8H5M13,8V12H19V8H13M5,14V18H11V14H5M13,14V18H19V14H13Z";
        private const string IcImage = "M8.5,13.5L11,16.5L14.5,12L19,18H5M21,19V5C21,3.89 20.1,3 19,3H5A2,2 0 0,0 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19Z";
        private const string IcCamera = "M4,4H7L9,2H15L17,4H20A2,2 0 0,1 22,6V18A2,2 0 0,1 20,20H4A2,2 0 0,1 2,18V6A2,2 0 0,1 4,4M12,7A5,5 0 0,0 7,12A5,5 0 0,0 12,17A5,5 0 0,0 17,12A5,5 0 0,0 12,7M12,9A3,3 0 0,1 15,12A3,3 0 0,1 12,15A3,3 0 0,1 9,12A3,3 0 0,1 12,9Z";
        private const string IcVideo = "M17,10.5V7A1,1 0 0,0 16,6H4A1,1 0 0,0 3,7V17A1,1 0 0,0 4,18H16A1,1 0 0,0 17,17V13.5L21,17.5V6.5L17,10.5Z";
        private const string IcImages = "M22,16V4A2,2 0 0,0 20,2H8A2,2 0 0,0 6,4V16A2,2 0 0,0 8,18H20A2,2 0 0,0 22,16M11,12L13.03,14.71L16,11L20,16H8L11,12M2,6V20A2,2 0 0,0 4,22H18V20H4V6H2Z";
        private const string IcChartBar = "M22,21H2V3H4V19H6V10H10V19H12V6H16V19H18V14H22V21Z";
        private const string IcChartLine = "M16,11.78L20.24,4.45L21.97,5.45L16.74,14.5L10.23,10.75L5.46,19H22V21H2V3H4V17.54L9.5,8L16,11.78Z";
        private const string IcChartPie = "M11,2V22C5.9,21.5 2,17.2 2,12C2,6.8 5.9,2.5 11,2M13,2V11H22C21.5,6.2 17.8,2.5 13,2M13,13V22C17.7,21.5 21.5,17.8 22,13H13Z";
        private const string IcScatter = "M7,11A2,2 0 0,1 9,13A2,2 0 0,1 7,15A2,2 0 0,1 5,13A2,2 0 0,1 7,11M11,5A2,2 0 0,1 13,7A2,2 0 0,1 11,9A2,2 0 0,1 9,7A2,2 0 0,1 11,5M16,13A2,2 0 0,1 18,15A2,2 0 0,1 16,17A2,2 0 0,1 14,15A2,2 0 0,1 16,13M17,6A2,2 0 0,1 19,8A2,2 0 0,1 17,10A2,2 0 0,1 15,8A2,2 0 0,1 17,6M2,2H4V20H22V22H2V2Z";
        private const string IcGraph = "M2,2H4V20H22V22H2V2M7,16C9,10 11,6 14,6C17,6 18,10 20,12L18.6,13.4C17.4,11.6 16.4,8 14,8C12,8 10.4,11.4 9,16.6L7,16Z";
        private const string IcCube = "M21,16.5C21,16.88 20.79,17.21 20.47,17.38L12.57,21.82C12.41,21.94 12.21,22 12,22C11.79,22 11.59,21.94 11.43,21.82L3.53,17.38C3.21,17.21 3,16.88 3,16.5V7.5C3,7.12 3.21,6.79 3.53,6.62L11.43,2.18C11.59,2.06 11.79,2 12,2C12.21,2 12.41,2.06 12.57,2.18L20.47,6.62C20.79,6.79 21,7.12 21,7.5V16.5M12,4.15L6.04,7.5L12,10.85L17.96,7.5L12,4.15M5,15.91L11,19.29V12.58L5,9.21V15.91M19,15.91V9.21L13,12.58V19.29L19,15.91Z";
        private const string IcFlask = "M5,19A1,1 0 0,0 6,20H18A1,1 0 0,0 19,19C19,18.79 18.93,18.59 18.82,18.43L13,8.35V4H11V8.35L5.18,18.43C5.07,18.59 5,18.79 5,19M6,22A3,3 0 0,1 3,19C3,18.4 3.18,17.84 3.5,17.37L9,7.81V6A1,1 0 0,1 8,5V4A2,2 0 0,1 10,2H14A2,2 0 0,1 16,4V5A1,1 0 0,1 15,6V7.81L20.5,17.37C20.82,17.84 21,18.4 21,19A3,3 0 0,1 18,22H6Z";
        private const string IcBook = "M18,22A2,2 0 0,0 20,20V4C20,2.89 19.1,2 18,2H12V9L9.5,7.5L7,9V2H6A2,2 0 0,0 4,4V20A2,2 0 0,0 6,22H18Z";
        private const string IcFlash = "M7,2V13H10V22L17,10H13L17,2H7Z";
        private const string IcChip = "M17,17H7V7H17M21,11V9H19V7C19,5.89 18.1,5 17,5H15V3H13V5H11V3H9V5H7C5.89,5 5,5.89 5,7V9H3V11H5V13H3V15H5V17A2,2 0 0,0 7,19H9V21H11V19H13V21H15V19H17A2,2 0 0,0 19,17V15H21V13H19V11M15,9H9V15H15V9Z";
        private const string IcMap = "M12,11.5A2.5,2.5 0 0,1 9.5,9A2.5,2.5 0 0,1 12,6.5A2.5,2.5 0 0,1 14.5,9A2.5,2.5 0 0,1 12,11.5M12,2A7,7 0 0,0 5,9C5,14.25 12,22 12,22C12,22 19,14.25 19,9A7,7 0 0,0 12,2Z";
        private const string IcPlay = "M10,15L15.19,12L10,9V15M21.56,7.17C21.69,7.64 21.78,8.27 21.84,9.07C21.91,9.87 21.94,10.56 21.94,11.16L22,12C22,14.19 21.84,15.8 21.56,16.83C21.31,17.73 20.73,18.31 19.83,18.56C19.36,18.69 18.5,18.78 17.18,18.84C15.88,18.91 14.69,18.94 13.59,18.94L12,19C7.81,19 5.2,18.84 4.17,18.56C3.27,18.31 2.69,17.73 2.44,16.83C2.31,16.36 2.22,15.73 2.16,14.93C2.09,14.13 2.06,13.44 2.06,12.84L2,12C2,9.81 2.16,8.2 2.44,7.17C2.69,6.27 3.27,5.69 4.17,5.44C4.64,5.31 5.5,5.22 6.82,5.16C8.12,5.09 9.31,5.06 10.41,5.06L12,5C16.19,5 18.8,5.16 19.83,5.44C20.73,5.69 21.31,6.27 21.56,7.17Z";
        private const string IcQuiz = "M3,5H9V11H3V5M5,7V9H7V7H5M11,7H21V9H11V7M11,15H21V17H11V15M5,20L1.5,16.5L2.91,15.09L5,17.17L9.59,12.59L11,14L5,20Z";
        private const string IcSparkles = "M19,1L17.74,3.75L15,5L17.74,6.26L19,9L20.25,6.26L23,5L20.25,3.75L19,1M9,4L6.5,9.5L1,12L6.5,14.5L9,20L11.5,14.5L17,12L11.5,9.5L9,4M19,15L17.74,17.74L15,19L17.74,20.25L19,23L20.25,20.25L23,19L20.25,17.74L19,15Z";
        private const string IcChat = "M12,3C17.5,3 22,6.58 22,11C22,15.42 17.5,19 12,19C10.76,19 9.57,18.82 8.47,18.5C5.55,21 2,21 2,21C4.33,18.67 4.7,17.1 4.75,16.5C3.05,15.07 2,13.13 2,11C2,6.58 6.5,3 12,3Z";
        private const string IcTranslate = "M12.87,15.07L10.33,12.56L10.36,12.53C12.1,10.59 13.34,8.36 14.07,6H17V4H10V2H8V4H1V6H12.17C11.5,7.92 10.44,9.75 9,11.35C8.07,10.32 7.3,9.19 6.69,8H4.69C5.42,9.63 6.42,11.17 7.67,12.56L2.58,17.58L4,19L9,14L12.11,17.11L12.87,15.07M18.5,10H16.5L12,22H14L15.12,19H19.87L21,22H23L18.5,10M15.88,17L17.5,12.67L19.12,17H15.88Z";
        private const string IcApps = "M16,20H20V16H16M16,14H20V10H16M10,8H14V4H10M16,8H20V4H16M10,14H14V10H10M4,14H8V10H4M4,20H8V16H4M10,20H14V16H10M4,8H8V4H4V8Z";

        private const string IcFolderOpen = "M19,20H4C2.89,20 2,19.1 2,18V6C2,4.89 2.89,4 4,4H10L12,6H19A2,2 0 0,1 21,8H4V18L6.14,10H23.21L20.93,18.5C20.7,19.37 19.92,20 19,20Z";
        private const string IcSave = "M15,9H5V5H15M12,19A3,3 0 0,1 9,16A3,3 0 0,1 12,13A3,3 0 0,1 15,16A3,3 0 0,1 12,19M17,3H5C3.89,3 3,3.9 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V7L17,3Z";
        private const string IcExport = "M5,20H19V18H5M19,9H15V3H9V9H5L12,16L19,9Z";
        private const string IcQr = "M3,11H5V13H3V11M11,5H13V9H11V5M9,11H13V15H11V13H9V11M15,11H17V13H19V11H21V13H19V15H21V19H19V21H17V19H13V21H11V17H15V15H17V13H15V11M19,19V15H17V19H19M15,3H21V9H15V3M17,5V7H19V5H17M3,3H9V9H3V3M5,5V7H7V5H5M3,15H9V21H3V15M5,17V19H7V17H5Z";
        private const string IcTimer = "M12,20A7,7 0 0,1 5,13A7,7 0 0,1 12,6A7,7 0 0,1 19,13A7,7 0 0,1 12,20M19.03,7.39L20.45,5.97C20,5.46 19.55,5 19.04,4.56L17.62,6C16.07,4.74 14.12,4 12,4A9,9 0 0,0 3,13A9,9 0 0,0 12,22C17,22 21,17.97 21,13C21,10.88 20.26,8.93 19.03,7.39M11,14H13V8H11M15,1H9V3H15V1Z";
        private const string IcCalculator = "M7,2H17A2,2 0 0,1 19,4V20A2,2 0 0,1 17,22H7A2,2 0 0,1 5,20V4A2,2 0 0,1 7,2M7,4V8H17V4H7M7,10V12H9V10H7M11,10V12H13V10H11M15,10V12H17V10H15M7,14V16H9V14H7M11,14V16H13V14H11M15,14V16H17V14H15M7,18V20H9V18H7M11,18V20H13V18H11M15,18V20H17V18H15Z";
        private const string IcPoll = "M3,22V8H7V22H3M10,22V2H14V22H10M17,22V14H21V22H17Z";
        private const string IcInfo = "M13,9H11V7H13M13,17H11V11H13M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z";
        private const string IcHelp = "M10,19H13V22H10V19M12,2C17.35,2.22 19.68,7.62 16.5,11.67C15.67,12.67 14.33,13.33 13.67,14.17C13,15 13,16 13,17H10C10,15.33 10,13.92 10.67,12.92C11.33,11.92 12.67,11.33 13.5,10.67C15.92,8.43 15.32,5.26 12,5A3,3 0 0,0 9,8H6A6,6 0 0,1 12,2Z";
        private const string IcShield = "M12,1L3,5V11C3,16.55 6.84,21.74 12,23C17.16,21.74 21,16.55 21,11V5L12,1Z";
        private const string IcZoom = "M15.5,14L20.5,19L19,20.5L14,15.5V14.71L13.73,14.43C12.59,15.41 11.11,16 9.5,16A6.5,6.5 0 0,1 3,9.5A6.5,6.5 0 0,1 9.5,3A6.5,6.5 0 0,1 16,9.5C16,11.11 15.41,12.59 14.43,13.73L14.71,14H15.5M9.5,14C12,14 14,12 14,9.5C14,7 12,5 9.5,5C7,5 5,7 5,9.5C5,12 7,14 9.5,14M12,10H10V12H9V10H7V9H9V7H10V9H12V10Z";
        private const string IcBoard = "M19,3H14.82C14.4,1.84 13.3,1 12,1C10.7,1 9.6,1.84 9.18,3H5A2,2 0 0,0 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5A2,2 0 0,0 19,3M12,3A1,1 0 0,1 13,4A1,1 0 0,1 12,5A1,1 0 0,1 11,4A1,1 0 0,1 12,3M7,7H17V5H19V19H5V5H7V7M12,17L17,12L15.59,10.59L12,14.17L9.91,12.09L8.5,13.5L12,17Z";

        #endregion

        #region Definitions

        private static readonly Color Brand = Color.FromRgb(0x2E, 0x86, 0xDE);

        private sealed record NavDef(string Key, string Label, string Icon, Color IconColor, bool HasFlyout, bool Danger = false);
        private sealed record ToolDef(string Key, string Label, string Icon, bool Tint = false);
        private sealed record TabDef(string Label, string Icon, ToolDef[] Tools);
        private sealed record SectionDef(string Header, ToolDef[] Tools);

        private static readonly NavDef[] NavDefs =
        {
            new("insert",     "Chèn nội dung", IcAddBox,    Brand,                            HasFlyout: true),
            new("desktop",    "DESKTOP",       IcMonitor,   Brand,                            HasFlyout: false),
            new("smartclass", "SMART CLASS",   IcSchool,    Color.FromRgb(0x10, 0xB9, 0x81),  HasFlyout: false),
            new("more",       "Mở rộng",       IcMoreHoriz, Color.FromRgb(0x74, 0x7D, 0x8C),  HasFlyout: true),
            new("exit",       "Thoát",         IcPower,     Color.FromRgb(0xDC, 0x26, 0x26),  HasFlyout: false, Danger: true),
        };

        /// <summary>Khoá đặc biệt trên Tầng 2 → phát <see cref="ItemInvoked"/> (mở lại menu/nút legacy).</summary>
        private static readonly HashSet<string> PassThroughKeys = new() { "insert-all", "more-all", "zoom", "board" };

        private static readonly TabDef[] InsertTabs =
        {
            new("4.1 Công cụ vẽ", IcRuler, new ToolDef[]
            {
                new("ruler", "Thước kẻ", IcRuler),
                new("compass", "Compa", IcCircle),
                new("triangle", "Eke", IcTriangle),
                new("protractor", "Thước đo độ", IcProtractor),
                new("keyboard", "Bàn phím ảo", IcKeyboard),
            }),
            new("4.2 Nội dung cơ bản", IcText, new ToolDef[]
            {
                new("textbox", "Hộp văn bản mẫu", IcText),
                new("table", "Chèn Bảng dữ liệu", IcTable),
                new("image", "Hình ảnh từ máy", IcImage),
                new("camera", "Chụp Camera", IcCamera),
                new("video", "Video (MP4/AVI)", IcVideo),
                new("imagelib", "Thư viện ảnh", IcImages),
            }),
            new("4.3 Biểu đồ & Đồ thị", IcChartLine, new ToolDef[]
            {
                new("chart-bar", "Biểu đồ Cột", IcChartBar),
                new("chart-line", "Biểu đồ Đường", IcChartLine),
                new("chart-pie", "Biểu đồ Tròn", IcChartPie),
                new("chart-scatter", "Radar / Phân tán", IcScatter),
                new("graph-2d", "Đồ thị 2D y=f(x)", IcGraph),
                new("graph-3d", "Đồ thị 3D z=f(x,y)", IcCube),
            }),
            new("4.4 Môn học", IcFlask, new ToolDef[]
            {
                new("periodic", "Bảng tuần hoàn 118", IcFlask),
                new("book", "Sách giáo khoa điện tử", IcBook),
                new("geometry", "Hình học không gian", IcCube),
                new("circuit", "Mạch điện tương tác", IcFlash),
            }),
            new("4.5 Công nghệ", IcChip, new ToolDef[]
            {
                new("model-3d", "Mô hình 3D OBJ/STL", IcCube),
                new("phet", "Mô phỏng PhET Lab", IcFlask),
                new("maps", "Google Maps", IcMap),
                new("youtube", "YouTube Video", IcPlay),
            }),
            new("4.6 AI & Trợ lý", IcSparkles, new ToolDef[]
            {
                new("ai-quiz", "Tạo trắc nghiệm AI", IcQuiz),
                new("ai-chat", "Trợ lý AI Sparkles", IcSparkles),
                new("chatgpt", "ChatGPT Giáo dục", IcChat),
                new("bing", "Trình dịch Bing", IcTranslate),
            }),
        };

        private static readonly SectionDef[] ExpandSections =
        {
            new("Hệ thống & Tệp tin", new ToolDef[]
            {
                new("open", "Mở .qasc", IcFolderOpen),
                new("save", "Lưu bài", IcSave),
                new("export", "Xuất PDF/Ảnh", IcExport),
                new("share", "Chia sẻ QR", IcQr, Tint: true),
            }),
            new("Tiện ích", new ToolDef[]
            {
                new("camera-ai", "Camera AI", IcCamera),
                new("timer", "Đếm giờ", IcTimer),
                new("calculator", "Máy tính", IcCalculator),
                new("vote", "Bình chọn", IcPoll),
            }),
            new("Bảng & Hiển thị", new ToolDef[]
            {
                new("zoom", "Phóng to", IcZoom),
                new("board", "Công cụ bảng", IcBoard),
            }),
            new("Trợ giúp", new ToolDef[]
            {
                new("about", "Thông tin", IcInfo),
                new("help", "Trợ giúp", IcHelp),
                new("license", "Bản quyền v2.4 Pro", IcShield),
                new("more-all", "Tất cả chức năng", IcApps),
            }),
        };

        #endregion

        private sealed class NavItem
        {
            public NavDef Def = null!;
            public Button Button = null!;
            public Path Icon = null!;
            public Ellipse Dot = null!;
        }

        private readonly Dictionary<string, NavItem> _nav = new();
        private readonly List<Button> _tabButtons = new();
        private string? _legacyActiveKey;
        private string? _flyoutKey = "insert";   // Stitch: mặc định mở sẵn "Chèn nội dung"
        private bool _flyoutVisible;
        private int _insertTab;
        private Window? _hostWindow;

        /// <summary>
        /// Phát ra khi chọn mục cần chạy nút legacy. Tham số = key:
        /// desktop, smartclass, exit, zoom, board, insert (menu Chèn đầy đủ), more (menu Mở rộng đầy đủ).
        /// </summary>
        public event EventHandler<string>? ItemInvoked;

        /// <summary>Phát ra khi chạm một công cụ cụ thể ở Tầng 2.</summary>
        public event EventHandler<DockToolInvokedEventArgs>? ToolInvoked;

        public bool IsOpen { get; private set; }

        public CornerDockMenu()
        {
            InitializeComponent();
            BuildNav();
            BuildInsertTabs();
            BuildExpand();
            SelectInsertTab(0);

            WireButton(btnCorner, () => SetOpen(!IsOpen), animatePress: true);

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        #region Public API

        /// <summary>Đánh dấu mục legacy đang hoạt động (null = không mục nào). Chỉ áp dụng cho mục có trên Tầng 1.</summary>
        public void SetActive(string? key)
        {
            if (key != null && (!_nav.TryGetValue(key, out var item) || item.Def.Danger)) key = null;
            if (_legacyActiveKey == key) return;
            _legacyActiveKey = key;
            RefreshNavVisuals();
        }

        /// <summary>Mở / đóng Dock (có hiệu ứng).</summary>
        public void SetOpen(bool open)
        {
            if (IsOpen == open) return;
            IsOpen = open;
            btnCorner.Tag = open ? "open" : null;

            var dur = TimeSpan.FromMilliseconds(160);
            if (open)
            {
                panelMenu.Visibility = Visibility.Visible;
                var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
                panelScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.85, 1, dur) { EasingFunction = ease });
                panelScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.6, 1, dur) { EasingFunction = ease });
                panelMenu.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, dur));
                ShowFlyout(_flyoutKey);
            }
            else
            {
                var fade = new DoubleAnimation(0, TimeSpan.FromMilliseconds(120));
                fade.Completed += (s, e) =>
                {
                    if (!IsOpen) panelMenu.Visibility = Visibility.Collapsed;
                };
                panelMenu.BeginAnimation(OpacityProperty, fade);
                HideFlyout();
            }
            RefreshNavVisuals();
        }

        #endregion

        #region Build

        private void BuildNav()
        {
            var style = (Style)Resources["DockNavStyle"];

            foreach (var def in NavDefs)
            {
                if (def.Danger)
                {
                    itemsHost.Children.Add(new Rectangle
                    {
                        Height = 1,
                        Fill = new SolidColorBrush(Color.FromRgb(0xE1, 0xE8, 0xED)),
                        Margin = new Thickness(6, 6, 6, 6),
                        IsHitTestVisible = false,
                    });
                }

                var icon = CreateIcon(def.Icon, new SolidColorBrush(def.IconColor));
                var dot = new Ellipse
                {
                    Width = 6, Height = 6,
                    Fill = Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center,
                    Visibility = Visibility.Collapsed,
                    IsHitTestVisible = false,
                };
                var text = new TextBlock
                {
                    Text = def.Label,
                    Margin = new Thickness(12, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    IsHitTestVisible = false,
                };
                if (!def.HasFlyout && !def.Danger) text.FontWeight = FontWeights.ExtraBold;

                var grid = new Grid { IsHitTestVisible = false };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var iconBox = WrapIcon(icon, 20);
                Grid.SetColumn(iconBox, 0);
                Grid.SetColumn(text, 1);
                Grid.SetColumn(dot, 2);
                grid.Children.Add(iconBox);
                grid.Children.Add(text);
                grid.Children.Add(dot);

                var btn = new Button
                {
                    Style = style,
                    Content = grid,
                    ToolTip = def.Label,
                    Tag = def.Danger ? "danger" : null,
                };

                var item = new NavItem { Def = def, Button = btn, Icon = icon, Dot = dot };
                _nav[def.Key] = item;

                string key = def.Key;
                WireButton(btn, () => OnNavTapped(key), animatePress: true);
                itemsHost.Children.Add(btn);
            }
        }

        private void BuildInsertTabs()
        {
            var style = (Style)Resources["DockTabStyle"];
            for (int i = 0; i < InsertTabs.Length; i++)
            {
                var def = InsertTabs[i];
                var icon = CreateIcon(def.Icon, null);
                BindFillToButtonForeground(icon);

                var content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    IsHitTestVisible = false,
                    Children =
                    {
                        WrapIcon(icon, 18),
                        new TextBlock { Text = def.Label, Margin = new Thickness(7, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center },
                    }
                };

                var btn = new Button { Style = style, Content = content, ToolTip = def.Label };
                int index = i;
                WireButton(btn, () => SelectInsertTab(index), animatePress: true);
                _tabButtons.Add(btn);
                insertTabsHost.Children.Add(btn);
            }
        }

        private void SelectInsertTab(int index)
        {
            if (index < 0 || index >= InsertTabs.Length) index = 0;
            _insertTab = index;
            for (int i = 0; i < _tabButtons.Count; i++)
                _tabButtons[i].Tag = i == index ? "active" : null;

            insertToolsHost.Children.Clear();
            foreach (var tool in InsertTabs[index].Tools)
                insertToolsHost.Children.Add(CreateToolButton("insert", tool));

            // Lối tắt mở menu Chèn nội dung đầy đủ (giữ 100% chức năng cũ chưa có trên Dock)
            insertToolsHost.Children.Add(CreateToolButton("insert", new ToolDef("insert-all", "Tất cả…", IcApps)));
        }

        private Button CreateToolButton(string group, ToolDef def)
        {
            var icon = CreateIcon(def.Icon, new SolidColorBrush(Brand));
            var tile = new Border
            {
                Width = 30, Height = 30,
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromRgb(0xF1, 0xF2, 0xF6)),
                Child = WrapIcon(icon, 18),
                IsHitTestVisible = false,
            };
            var content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                IsHitTestVisible = false,
                Children =
                {
                    tile,
                    new TextBlock { Text = def.Label, Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center },
                }
            };

            var btn = new Button
            {
                Style = (Style)Resources["DockToolStyle"],
                Content = content,
                ToolTip = def.Label,
            };
            string key = def.Key;
            WireButton(btn, () => OnToolTapped(group, key), animatePress: true);
            return btn;
        }

        private void BuildExpand()
        {
            var gridStyle = (Style)Resources["DockGridStyle"];

            for (int s = 0; s < ExpandSections.Length; s++)
            {
                var section = ExpandSections[s];

                if (s > 0)
                {
                    expandSectionsHost.Children.Add(new Rectangle
                    {
                        Width = 1,
                        Fill = new SolidColorBrush(Color.FromRgb(0xE1, 0xE8, 0xED)),
                        Margin = new Thickness(12, 4, 12, 4),
                        IsHitTestVisible = false,
                    });
                }

                var column = new StackPanel { Orientation = Orientation.Vertical, VerticalAlignment = VerticalAlignment.Top };
                column.Children.Add(new TextBlock
                {
                    Text = section.Header.ToUpperInvariant(),
                    FontSize = 10.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x74, 0x7D, 0x8C)),
                    Margin = new Thickness(4, 0, 0, 6),
                    IsHitTestVisible = false,
                });

                var grid = new UniformGrid { Columns = 2 };
                foreach (var tool in section.Tools)
                {
                    var icon = CreateIcon(tool.Icon, new SolidColorBrush(Brand));
                    var content = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        IsHitTestVisible = false,
                        Children =
                        {
                            WrapIcon(icon, 17),
                            new TextBlock { Text = tool.Label, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center },
                        }
                    };
                    var btn = new Button
                    {
                        Style = gridStyle,
                        Content = content,
                        ToolTip = tool.Label,
                        MinWidth = 138,
                        Tag = tool.Tint ? "tint" : null,
                    };
                    string key = tool.Key;
                    WireButton(btn, () => OnToolTapped("expand", key), animatePress: true);
                    grid.Children.Add(btn);
                }
                column.Children.Add(grid);
                expandSectionsHost.Children.Add(column);
            }
        }

        #endregion

        #region Interaction

        private void OnNavTapped(string key)
        {
            if (!_nav.TryGetValue(key, out var item)) return;

            if (item.Def.HasFlyout)
            {
                // Chạm lại mục đang mở → thu Tầng 2; ngược lại chuyển sang bảng tương ứng
                if (_flyoutVisible && _flyoutKey == key)
                {
                    _flyoutKey = null;
                    HideFlyout();
                }
                else
                {
                    _flyoutKey = key;
                    ShowFlyout(key);
                }
                RefreshNavVisuals();
                return;
            }

            SetOpen(false);
            ItemInvoked?.Invoke(this, key);
        }

        private void OnToolTapped(string group, string tool)
        {
            SetOpen(false);

            if (PassThroughKeys.Contains(tool))
            {
                string legacyKey = tool switch
                {
                    "insert-all" => "insert",
                    "more-all" => "more",
                    _ => tool,
                };
                ItemInvoked?.Invoke(this, legacyKey);
                return;
            }

            ToolInvoked?.Invoke(this, new DockToolInvokedEventArgs(group, tool));
        }

        private void ShowFlyout(string? key)
        {
            if (key == null)
            {
                HideFlyout();
                return;
            }

            panelInsert.Visibility = key == "insert" ? Visibility.Visible : Visibility.Collapsed;
            panelExpand.Visibility = key == "more" ? Visibility.Visible : Visibility.Collapsed;

            bool wasVisible = _flyoutVisible;
            _flyoutVisible = true;
            flyoutHost.Visibility = Visibility.Visible;
            UpdateFlyoutMaxWidth();

            var dur = TimeSpan.FromMilliseconds(170);
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            flyoutShift.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(wasVisible ? -6 : -14, 0, dur) { EasingFunction = ease });
            flyoutHost.BeginAnimation(OpacityProperty, new DoubleAnimation(wasVisible ? 0.4 : 0, 1, dur));
        }

        private void HideFlyout()
        {
            if (!_flyoutVisible) return;
            _flyoutVisible = false;
            var fade = new DoubleAnimation(0, TimeSpan.FromMilliseconds(110));
            fade.Completed += (s, e) =>
            {
                if (!_flyoutVisible) flyoutHost.Visibility = Visibility.Collapsed;
            };
            flyoutHost.BeginAnimation(OpacityProperty, fade);
        }

        private void RefreshNavVisuals()
        {
            string? highlight = IsOpen && _flyoutVisible ? _flyoutKey : null;
            highlight ??= _legacyActiveKey;

            foreach (var kv in _nav)
            {
                var item = kv.Value;
                if (item.Def.Danger) continue;

                bool active = kv.Key == highlight;
                item.Button.Tag = active ? "active" : null;
                item.Icon.Fill = active ? Brushes.White : new SolidColorBrush(item.Def.IconColor);
                item.Dot.Visibility = active && item.Def.HasFlyout ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>Giới hạn bề rộng Tầng 2 theo cửa sổ → tự xuống dòng thay vì tràn màn hình.</summary>
        private void UpdateFlyoutMaxWidth()
        {
            double hostWidth = _hostWindow?.ActualWidth ?? SystemParameters.PrimaryScreenWidth;
            double max = Math.Max(360, hostWidth - 212 - 12 - 60);
            flyoutHost.MaxWidth = max;
        }

        #endregion

        #region Helpers

        private static Path CreateIcon(string data, Brush? fill)
        {
            Geometry geometry;
            try
            {
                geometry = Geometry.Parse(data);
            }
            catch
            {
                geometry = Geometry.Parse("M4,4H20V20H4Z");
            }

            var path = new Path { Data = geometry, IsHitTestVisible = false };
            if (fill != null) path.Fill = fill;
            return path;
        }

        private static Viewbox WrapIcon(Path icon, double size) => new Viewbox
        {
            Width = size,
            Height = size,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            IsHitTestVisible = false,
            Child = new Canvas { Width = 24, Height = 24, Children = { icon } },
        };

        private static void BindFillToButtonForeground(Path icon)
        {
            icon.SetBinding(Shape.FillProperty, new Binding(nameof(Control.Foreground))
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Button), 1)
            });
        }

        /// <summary>
        /// Gắn hành động cho nút: Chuột qua Button.Click; Chạm/Bút qua <see cref="ToolbarPointerHelper.AttachTap"/>
        /// (chặn promotion → không bao giờ kích hoạt 2 lần). Có hiệu ứng nhấn thu nhỏ 0.95.
        /// </summary>
        private static void WireButton(Button btn, Action action, bool animatePress)
        {
            Action<bool>? onPressed = null;
            if (animatePress)
            {
                var scale = new ScaleTransform(1, 1);
                btn.RenderTransform = scale;
                btn.RenderTransformOrigin = new Point(0.5, 0.5);
                onPressed = pressed =>
                {
                    var anim = new DoubleAnimation(pressed ? 0.95 : 1.0, TimeSpan.FromMilliseconds(90));
                    scale.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
                    scale.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
                };
                btn.PreviewMouseLeftButtonDown += (s, e) => { if (e.StylusDevice == null) onPressed(true); };
                btn.PreviewMouseLeftButtonUp += (s, e) => { if (e.StylusDevice == null) onPressed(false); };
                btn.MouseLeave += (s, e) => onPressed(false);
            }

            btn.Click += (s, e) => action();
            ToolbarPointerHelper.AttachTap(btn,
                () => btn.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, btn)),
                onPressed,
                handleMouse: false);
        }

        #endregion

        #region Close on outside press

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _hostWindow = Window.GetWindow(this);
            if (_hostWindow == null) return;
            _hostWindow.PreviewMouseDown += Host_PreviewPointerDown;
            _hostWindow.PreviewTouchDown += Host_PreviewPointerDown;
            _hostWindow.PreviewStylusDown += Host_PreviewPointerDown;
            _hostWindow.Deactivated += Host_Deactivated;
            _hostWindow.SizeChanged += Host_SizeChanged;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_hostWindow == null) return;
            _hostWindow.PreviewMouseDown -= Host_PreviewPointerDown;
            _hostWindow.PreviewTouchDown -= Host_PreviewPointerDown;
            _hostWindow.PreviewStylusDown -= Host_PreviewPointerDown;
            _hostWindow.Deactivated -= Host_Deactivated;
            _hostWindow.SizeChanged -= Host_SizeChanged;
            _hostWindow = null;
        }

        private void Host_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_flyoutVisible) UpdateFlyoutMaxWidth();
        }

        private void Host_Deactivated(object? sender, EventArgs e)
        {
            // Mở cửa sổ submenu/hộp thoại khác → thu Dock lại cho gọn
            if (IsOpen) SetOpen(false);
        }

        private void Host_PreviewPointerDown(object? sender, InputEventArgs e)
        {
            // Không đánh dấu Handled → không ảnh hưởng thao tác vẽ/chọn trên bảng
            if (!IsOpen) return;
            if (IsWithinSelf(e.OriginalSource as DependencyObject)) return;
            SetOpen(false);
        }

        private bool IsWithinSelf(DependencyObject? d)
        {
            while (d != null)
            {
                if (ReferenceEquals(d, this)) return true;
                d = d is Visual || d is System.Windows.Media.Media3D.Visual3D
                    ? VisualTreeHelper.GetParent(d)
                    : LogicalTreeHelper.GetParent(d);
            }
            return false;
        }

        #endregion
    }
}
