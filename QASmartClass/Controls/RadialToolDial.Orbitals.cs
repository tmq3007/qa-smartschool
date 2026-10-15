using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using Path = System.Windows.Shapes.Path;

namespace QASmartTouch.Controls
{
    /// <summary>
    /// Phần Orbital Arc Menu của RadialToolDial — 4 trạng thái cung viền theo thiết kế Stitch:
    /// 1. Bút viết: cung trái = độ dày (1–16px), cung phải trong = 6 loại bút, cung phải ngoài = màu + bảng 16 màu.
    /// 2. Tẩy: cung trái = cỡ tẩy (5–50px) + vùng xóa nét đứt, cung phải = 3 chế độ tẩy.
    /// 3. Chọn vùng &amp; AI: cung trái = Chữ nhật / Lasso / Đũa phép, cung phải = 5 thao tác AI.
    /// 4. Hình học: cung trái = công tắc 2D/3D, cung phải = bộ hình tương ứng.
    /// </summary>
    public partial class RadialToolDial
    {
        #region Orbital constants & data

        private const double SliderRadius = 180;   // cung slider bên trái
        private const double SliderTopDeg = 215;   // đầu trên (giá trị nhỏ)
        private const double SliderBottomDeg = 145; // đầu dưới (giá trị lớn)
        private const double RightPillRadius = 176;
        private const double LeftPillRadius = 168;
        private const double ColorRadius = 330;
        private const double LeftPillWidth = 138;

        private const string IconPalette = "M17.5,12A1.5,1.5 0 0,1 16,10.5A1.5,1.5 0 0,1 17.5,9A1.5,1.5 0 0,1 19,10.5A1.5,1.5 0 0,1 17.5,12M14.5,8A1.5,1.5 0 0,1 13,6.5A1.5,1.5 0 0,1 14.5,5A1.5,1.5 0 0,1 16,6.5A1.5,1.5 0 0,1 14.5,8M9.5,8A1.5,1.5 0 0,1 8,6.5A1.5,1.5 0 0,1 9.5,5A1.5,1.5 0 0,1 11,6.5A1.5,1.5 0 0,1 9.5,8M6.5,12A1.5,1.5 0 0,1 5,10.5A1.5,1.5 0 0,1 6.5,9A1.5,1.5 0 0,1 8,10.5A1.5,1.5 0 0,1 6.5,12M12,3A9,9 0 0,0 3,12A9,9 0 0,0 12,21A1.5,1.5 0 0,0 13.5,19.5C13.5,19.11 13.35,18.76 13.11,18.5C12.88,18.23 12.73,17.88 12.73,17.5A1.5,1.5 0 0,1 14.23,16H16A5,5 0 0,0 21,11C21,6.58 16.97,3 12,3Z";
        private const string IconCheck = "M21,7L9,19L3.5,13.5L4.91,12.08L9,16.17L19.59,5.59L21,7Z";
        private const string IconClose = "M19,6.41L17.59,5L12,10.59L6.41,5L5,6.41L10.59,12L5,17.59L6.41,19L12,13.41L17.59,19L19,17.59L13.41,12L19,6.41Z";

        // ── Bút ──
        private static readonly (string Type, string Label, string Icon, string IconColor)[] PenTypes =
        {
            ("Normal", "Bút thường",   "M20.71,4.63L19.37,3.29C19,2.9 18.35,2.9 17.96,3.29L9,12.25L11.75,15L20.71,6.04C21.1,5.65 21.1,5 20.71,4.63M7,14A3,3 0 0,0 4,17C4,18.31 2.84,19 2,19C2.92,20.22 4.5,21 6,21A4,4 0 0,0 10,17A3,3 0 0,0 7,14Z", "#2E86DE"),
            ("Hoc",    "Bút học",      "M9.75,20.85C11.53,20.15 11.14,18.22 10.24,17C9.35,15.75 8.12,14.89 6.88,14.06C6,13.5 5.19,12.8 4.54,12C4.26,11.67 3.69,11.06 4.27,10.94C4.86,10.82 5.88,11.4 6.4,11.62C7.31,12 8.21,12.44 9.05,12.96L10.06,11.27C8.5,10.24 6.5,9.32 4.64,9.05C3.58,8.89 2.46,9.11 2.1,10.26C1.78,11.25 2.29,12.25 2.87,13.03C4.24,14.86 6.37,15.74 7.96,17.32C8.3,17.65 8.71,18.04 8.91,18.5C9.12,18.94 9.07,18.97 8.6,18.97C7.36,18.97 5.81,18 4.8,17.36L3.79,19.06C5.32,20 7.88,21.47 9.75,20.85M20.84,5.25C21.06,5.03 21.06,4.67 20.84,4.46L19.54,3.16C19.33,2.95 18.97,2.95 18.76,3.16L17.74,4.18L19.82,6.26M11,10.92V13H13.08L19.23,6.85L17.15,4.77L11,10.92Z", "#16A085"),
            ("AI",     "Bút AI",       "M7.5,5.6L5,7L6.4,4.5L5,2L7.5,3.4L10,2L8.6,4.5L10,7L7.5,5.6M19.5,15.4L22,14L20.6,16.5L22,19L19.5,17.6L17,19L18.4,16.5L17,14L19.5,15.4M22,2L20.6,4.5L22,7L19.5,5.6L17,7L18.4,4.5L17,2L19.5,3.4L22,2M13.34,12.78L15.78,10.34L13.66,8.22L11.22,10.66L13.34,12.78M14.37,7.29L16.71,9.63C17.1,10 17.1,10.65 16.71,11.04L5.04,22.71C4.65,23.1 4,23.1 3.63,22.71L1.29,20.37C0.9,20 0.9,19.35 1.29,18.96L12.96,7.29C13.35,6.9 14,6.9 14.37,7.29Z", "#8E44AD"),
            ("Simple", "Bút đơn giản", "M14.06,9L15,9.94L5.92,19H5V18.08L14.06,9M17.66,3C17.41,3 17.15,3.1 16.96,3.29L15.13,5.12L18.88,8.87L20.71,7.04C21.1,6.65 21.1,6 20.71,5.63L18.37,3.29C18.17,3.09 17.92,3 17.66,3M14.06,6.19L3,17.25V21H6.75L17.81,9.94L14.06,6.19Z", "#747D8C"),
            ("Marker", "Marker",       "M18.5,1.15C17.97,1.15 17.46,1.34 17.07,1.73L11.26,7.55L16.91,13.2L22.73,7.39C23.5,6.61 23.5,5.35 22.73,4.56L19.89,1.73C19.5,1.34 19,1.15 18.5,1.15M10.3,8.5L4.34,14.46C3.56,15.24 3.56,16.5 4.36,17.31C3.14,18.54 1.9,19.77 0.67,21H6.33L7.19,20.14C7.97,20.9 9.22,20.89 10,20.12L15.95,14.16", "#F39C12"),
            ("Mask",   "Mask Pen",     "M8.11,19.45C5.94,18.65 4.22,16.78 3.71,14.35L2.05,6.54C1.81,5.46 2.5,4.4 3.58,4.17L13.35,2.1L13.38,2.09C14.45,1.88 15.5,2.57 15.72,3.63L16.07,5.3L20.42,6.23H20.45C21.5,6.47 22.18,7.53 21.96,8.59L20.3,16.41C19.5,20.18 15.78,22.6 12,21.79C10.42,21.46 9.08,20.61 8.11,19.45M20,8.18L10.23,6.1L8.57,13.92V13.95C8,16.63 9.73,19.27 12.42,19.84C15.11,20.41 17.77,18.69 18.34,16L20,8.18M16,16.5C15.37,17.57 14.11,18.16 12.83,17.89C11.56,17.62 10.65,16.57 10.5,15.34L16,16.5M8.47,5.17L4,6.13L5.66,13.94L5.67,13.97C5.82,14.66 6.12,15.28 6.53,15.81C6.43,15.08 6.45,14.32 6.61,13.56L7.08,11.33C6.84,11.32 6.6,11.22 6.42,11.03C6.2,10.88 6.04,10.59 6,10.27L8.47,5.17M17.4,12.25C17.25,12.55 17.03,12.79 16.77,12.94C16.5,13.09 16.18,13.13 15.86,13.06L17.58,10.36L17.4,12.25M11.5,10.82C11.25,11.13 10.84,11.31 10.42,11.22C10,11.13 9.71,10.8 9.6,10.42L11.5,10.82Z", "#E74C3C"),
        };

        // Màu nhanh trên cung ngoài (bảng đen xanh → mặc định Trắng thay cho màu Slate của bản thiết kế)
        private static readonly string[] QuickColors = { "#FFFFFF", "#FFB7B2", "#FFDAC1", "#E2F0CB", "#B5EAD7", "#C7CEEA", "#CFE2F3" };

        private static readonly string[] PaletteColors =
        {
            "#FFFFFF", "#FFB7B2", "#FFDAC1", "#E2F0CB",
            "#B5EAD7", "#C7CEEA", "#CFE2F3", "#D0F4DE",
            "#A9DEF9", "#E4C1F9", "#FEE1E8", "#FFE5D9",
            "#334155", "#64748B", "#94A3B8", "#F8FAFC",
        };

        // ── Tẩy ──
        private static readonly (string Mode, string Title, string Sub, string Icon)[] EraserModes =
        {
            ("Stroke",   "Xóa từng nét",  "Stroke Eraser",    "M9.75,20.85C11.53,20.15 11.14,18.22 10.24,17C9.35,15.75 8.12,14.89 6.88,14.06C6,13.5 5.19,12.8 4.54,12C4.26,11.67 3.69,11.06 4.27,10.94C4.86,10.82 5.88,11.4 6.4,11.62C7.31,12 8.21,12.44 9.05,12.96L10.06,11.27C8.5,10.24 6.5,9.32 4.64,9.05C3.58,8.89 2.46,9.11 2.1,10.26C1.78,11.25 2.29,12.25 2.87,13.03C4.24,14.86 6.37,15.74 7.96,17.32C8.3,17.65 8.71,18.04 8.91,18.5C9.12,18.94 9.07,18.97 8.6,18.97C7.36,18.97 5.81,18 4.8,17.36L3.79,19.06C5.32,20 7.88,21.47 9.75,20.85Z"),
            ("Point",    "Xóa theo điểm", "Point Eraser",     "M12,10A2,2 0 0,0 10,12A2,2 0 0,0 12,14A2,2 0 0,0 14,12A2,2 0 0,0 12,10M12,6A1,1 0 0,0 11,7A1,1 0 0,0 12,8A1,1 0 0,0 13,7A1,1 0 0,0 12,6M12,16A1,1 0 0,0 11,17A1,1 0 0,0 12,18A1,1 0 0,0 13,17A1,1 0 0,0 12,16M7,11A1,1 0 0,0 6,12A1,1 0 0,0 7,13A1,1 0 0,0 8,12A1,1 0 0,0 7,11M17,11A1,1 0 0,0 16,12A1,1 0 0,0 17,13A1,1 0 0,0 18,12A1,1 0 0,0 17,11M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20Z"),
            ("ClearAll", "Xóa toàn bộ",   "Clear All Canvas", "M15,16H19V18H15V16M15,8H22V10H15V8M15,12H21V14H15V12M3,18A2,2 0 0,0 5,20H11A2,2 0 0,0 13,18V8H3V18M14,5H11L10,4H6L5,5H2V7H14V5Z"),
        };

        // ── Chọn vùng & AI ──
        private static readonly (string Mode, string Label, string Icon)[] SelectModes =
        {
            ("Rect",  "Chữ nhật",      "M5,5H7V7H5V5M9,5H11V7H9V5M13,5H15V7H13V5M17,5H19V7H17V5M17,9H19V11H17V9M17,13H19V15H17V13M17,17H19V19H17V17M13,17H15V19H13V17M9,17H11V19H9V17M5,17H7V19H5V17M5,13H7V15H5V13M5,9H7V11H5V9Z"),
            ("Lasso", "Tự do (Lasso)", "M12,2C17.5,2 22,5.13 22,9C22,12.26 18.81,15 14.5,15.78L14.5,15.5C14.5,14.91 14.4,14.34 14.21,13.81C17.55,13.21 20,11.28 20,9C20,6.24 16.42,4 12,4C7.58,4 4,6.24 4,9C4,10.19 4.67,11.29 5.79,12.15C5.35,12.72 5.04,13.38 4.87,14.1C3.12,12.82 2,11 2,9C2,5.13 6.5,2 12,2M9.5,12C11.43,12 13,13.57 13,15.5C13,17.07 11.97,18.4 10.54,18.84L11,22H9L8.55,18.88C7.08,18.47 6,17.11 6,15.5C6,13.57 7.57,12 9.5,12M9.5,14A1.5,1.5 0 0,0 8,15.5A1.5,1.5 0 0,0 9.5,17A1.5,1.5 0 0,0 11,15.5A1.5,1.5 0 0,0 9.5,14Z"),
            ("Magic", "Đũa phép",      "M7.5,5.6L5,7L6.4,4.5L5,2L7.5,3.4L10,2L8.6,4.5L10,7L7.5,5.6M19.5,15.4L22,14L20.6,16.5L22,19L19.5,17.6L17,19L18.4,16.5L17,14L19.5,15.4M22,2L20.6,4.5L22,7L19.5,5.6L17,7L18.4,4.5L17,2L19.5,3.4L22,2M13.34,12.78L15.78,10.34L13.66,8.22L11.22,10.66L13.34,12.78M14.37,7.29L16.71,9.63C17.1,10 17.1,10.65 16.71,11.04L5.04,22.71C4.65,23.1 4,23.1 3.63,22.71L1.29,20.37C0.9,20 0.9,19.35 1.29,18.96L12.96,7.29C13.35,6.9 14,6.9 14.37,7.29Z"),
        };

        private static readonly (string Action, string Label, string Icon)[] SmartActions =
        {
            ("OCR",       "OCR chữ viết tay",    "M18.5,4L19.66,8.35L18.7,8.61C18.25,7.74 17.79,6.87 17.26,6.43C16.73,6 16.11,6 15.5,6H13V16.5C13,17 13,17.5 13.33,17.75C13.67,18 14.33,18 15,18V19H9V18C9.67,18 10.33,18 10.67,17.75C11,17.5 11,17 11,16.5V6H8.5C7.89,6 7.27,6 6.74,6.43C6.21,6.87 5.75,7.74 5.3,8.61L4.34,8.35L5.5,4H18.5Z"),
            ("Translate", "Dịch văn bản (Bing)", "M12.87,15.07L10.33,12.56L10.36,12.53C12.1,10.59 13.34,8.36 14.07,6H17V4H10V2H8V4H1V6H12.17C11.5,7.92 10.44,9.75 9,11.35C8.07,10.32 7.3,9.19 6.69,8H4.69C5.42,9.63 6.42,11.17 7.67,12.56L2.58,17.58L4,19L9,14L12.11,17.11L12.87,15.07M18.5,10H16.5L12,22H14L15.12,19H19.87L21,22H23L18.5,10M15.88,17L17.5,12.67L19.12,17H15.88Z"),
            ("Read",      "Đọc to văn bản",      "M14,3.23V5.29C16.89,6.15 19,8.83 19,12C19,15.17 16.89,17.84 14,18.7V20.77C18,19.86 21,16.28 21,12C21,7.72 18,4.14 14,3.23M16.5,12C16.5,10.23 15.5,8.71 14,7.97V16C15.5,15.29 16.5,13.76 16.5,12M3,9V15H7L12,20V4L7,9H3Z"),
            ("Search",    "Tìm kiếm Google",     "M9.5,3A6.5,6.5 0 0,1 16,9.5C16,11.11 15.41,12.59 14.44,13.73L14.71,14H15.5L20.5,19L19,20.5L14,15.5V14.71L13.73,14.44C12.59,15.41 11.11,16 9.5,16A6.5,6.5 0 0,1 3,9.5A6.5,6.5 0 0,1 9.5,3M9.5,5C7,5 5,7 5,9.5C5,12 7,14 9.5,14C12,14 14,12 14,9.5C14,7 12,5 9.5,5Z"),
            ("AIChat",    "Hỏi AI về nội dung",  "M12,2A2,2 0 0,1 14,4C14,4.74 13.6,5.39 13,5.73V7H14A7,7 0 0,1 21,14H22A1,1 0 0,1 23,15V18A1,1 0 0,1 22,19H21V20A2,2 0 0,1 19,22H5A2,2 0 0,1 3,20V19H2A1,1 0 0,1 1,18V15A1,1 0 0,1 2,14H3A7,7 0 0,1 10,7H11V5.73C10.4,5.39 10,4.74 10,4A2,2 0 0,1 12,2M7.5,13A2.5,2.5 0 0,0 5,15.5A2.5,2.5 0 0,0 7.5,18A2.5,2.5 0 0,0 10,15.5A2.5,2.5 0 0,0 7.5,13M16.5,13A2.5,2.5 0 0,0 14,15.5A2.5,2.5 0 0,0 16.5,18A2.5,2.5 0 0,0 19,15.5A2.5,2.5 0 0,0 16.5,13Z"),
        };

        // ── Hình học (tên hình khớp với Form2_5_SubMenuDrawShapes / EnableShapeDrawingMode) ──
        private static readonly (string Shape, string Label, string Icon)[] Shapes2D =
        {
            ("Line",       "Đường thẳng",   "M19,13H5V11H19V13Z"),
            ("ArrowLine",  "Mũi tên",       "M4,11V13H16L10.5,18.5L11.92,19.92L19.84,12L11.92,4.08L10.5,5.5L16,11H4Z"),
            ("DashedLine", "Nét đứt",       "M2,11H6V13H2V11M8,11H12V13H8V11M14,11H18V13H14V11M20,11H22V13H20V11Z"),
            ("Circle",     "Hình tròn",     "M12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z"),
            ("Triangle",   "Tam giác",      "M12,2L1,21H23M12,6L19.53,19H4.47"),
            ("Square",     "Hình vuông",    "M3,3V21H21V3H3M19,19H5V5H19V19Z"),
            ("Rectangle",  "Hình chữ nhật", "M4,6V18H20V6H4M18,16H6V8H18V16Z"),
        };

        private static readonly (string Shape, string Label, string Icon)[] Shapes3D =
        {
            ("Cube",     "Lập phương", "M21,16.5C21,16.88 20.79,17.21 20.47,17.38L12.57,21.82C12.41,21.94 12.21,22 12,22C11.79,22 11.59,21.94 11.43,21.82L3.53,17.38C3.21,17.21 3,16.88 3,16.5V7.5C3,7.12 3.21,6.79 3.53,6.62L11.43,2.18C11.59,2.06 11.79,2 12,2C12.21,2 12.41,2.06 12.57,2.18L20.47,6.62C20.79,6.79 21,7.12 21,7.5V16.5M12,4.15L6.04,7.5L12,10.85L17.96,7.5L12,4.15M5,15.91L11,19.29V12.58L5,9.21V15.91M19,15.91V9.21L13,12.58V19.29L19,15.91Z"),
            ("Sphere",   "Hình cầu",   "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,4C14.5,4 16.5,7.58 16.5,12C16.5,16.42 14.5,20 12,20C9.5,20 7.5,16.42 7.5,12C7.5,7.58 9.5,4 12,4M4.1,11C4.5,7.9 6.9,5.3 10,4.4C8.6,6 7.7,8.4 7.5,11H4.1M16.5,11C16.3,8.4 15.4,6 14,4.4C17.1,5.3 19.5,7.9 19.9,11H16.5Z"),
            ("Pyramid",  "Hình chóp",  "M21.85,16.96H21.85L12.85,2.47C12.65,2.16 12.33,2 12,2C11.67,2 11.35,2.16 11.15,2.47L2.15,16.96C1.84,17.45 2,18.1 2.5,18.39L11.5,21.86C11.66,21.95 11.83,22 12,22C12.17,22 12.34,21.95 12.5,21.86L21.5,18.39C22,18.1 22.16,17.45 21.85,16.96M11,6.26V19.37L4.9,17.03L11,6.26M13,19.37V6.26L19.1,17.03L13,19.37Z"),
            ("Cylinder", "Hình trụ",   "M12,3C7.58,3 4,4.79 4,7V17C4,19.21 7.59,21 12,21C16.41,21 20,19.21 20,17V7C20,4.79 16.42,3 12,3M18,17C18,17.5 15.87,19 12,19C8.13,19 6,17.5 6,17V9.77C7.61,10.55 9.72,11 12,11C14.28,11 16.39,10.55 18,9.77V17M12,9C8.13,9 6,7.5 6,7C6,6.5 8.13,5 12,5C15.87,5 18,6.5 18,7C18,7.5 15.87,9 12,9Z"),
            ("Cone",     "Hình nón",   "M12,2L4.5,18.5C4.5,20.43 7.86,22 12,22C16.14,22 19.5,20.43 19.5,18.5L12,2M12,6.83L16.69,17.14C15.5,17.69 13.85,18 12,18C10.15,18 8.5,17.69 7.31,17.14L12,6.83Z"),
            ("Prism",    "Lăng trụ",   "M3,7L12,2L21,7V17L12,22L3,17V7M5,8.18V15.82L11,19.15V11.5L5,8.18M19,8.18L13,11.5V19.15L19,15.82V8.18M6.03,6.5L12,9.82L17.97,6.5L12,3.18L6.03,6.5Z"),
        };

        #endregion

        #region Orbital state (đồng bộ từ Form2)

        private string _penType = "Normal";
        private int _penSize = 5;
        private Color _penColor = Colors.White;
        private int _eraserSize = 20;
        private string _eraserMode = "Stroke";
        private string? _activeShape;
        private bool _shape3D;
        private string? _selectMode;

        /// <summary>Cập nhật giá trị Bút hiển thị trên cung Orbital (không phát sự kiện).</summary>
        public void SetPenSettings(string brushType, int size, Color color)
        {
            if (_penType == brushType && _penSize == size && _penColor == color) return;
            _penType = brushType; _penSize = size; _penColor = color;
            if (_state == "pen" && !_sliderDragging) RebuildOrbitals();
        }

        /// <summary>Cập nhật giá trị Tẩy hiển thị trên cung Orbital (không phát sự kiện).</summary>
        public void SetEraserSettings(int size, string mode)
        {
            if (mode == "ClearAll") mode = _eraserMode; // ClearAll là thao tác tức thời, không phải chế độ
            if (_eraserSize == size && _eraserMode == mode) return;
            _eraserSize = size; _eraserMode = mode;
            if (_state == "eraser" && !_sliderDragging) RebuildOrbitals();
        }

        /// <summary>Đánh dấu hình đang vẽ (null = không có).</summary>
        public void SetActiveShape(string? shape)
        {
            if (_activeShape == shape) return;
            _activeShape = shape;
            if (_state == "shapes") RebuildOrbitals();
        }

        #endregion

        #region Layer show / hide

        private void InitializeOrbitals() => RebuildOrbitals();

        private void SetOrbitalsVisible(bool visible)
        {
            _orbitalsVisible = visible;
            UpdateOrbitalLayerVisibility(true);
        }

        private void UpdateOrbitalLayerVisibility(bool animate)
        {
            bool visible = _orbitalsVisible && !IsCollapsed;
            orbitalLayer.IsHitTestVisible = visible;
            if (!visible) HidePalettePopover();

            var dur = TimeSpan.FromMilliseconds(animate ? 170 : 0);
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            double target = visible ? 1 : 0;
            orbitalLayer.BeginAnimation(OpacityProperty, new DoubleAnimation(target, dur));
            orbitalScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(visible ? 1 : 0.9, dur) { EasingFunction = ease });
            orbitalScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(visible ? 1 : 0.9, dur) { EasingFunction = ease });
        }

        private void RebuildOrbitals()
        {
            orbitalLayer.Children.Clear();
            HidePalettePopover();

            switch (_state)
            {
                case "pen": BuildPenOrbitals(); break;
                case "eraser": BuildEraserOrbitals(); break;
                case "select": BuildSelectOrbitals(); break;
                case "shapes": BuildShapeOrbitals(); break;
            }

            // Hiệu ứng "nở" nhẹ khi đổi trạng thái
            if (_orbitalsVisible && !IsCollapsed && IsLoaded)
            {
                var dur = TimeSpan.FromMilliseconds(170);
                var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
                orbitalLayer.BeginAnimation(OpacityProperty, new DoubleAnimation(0.2, 1, dur));
                orbitalScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.92, 1, dur) { EasingFunction = ease });
                orbitalScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.92, 1, dur) { EasingFunction = ease });
            }
        }

        private void Raise(string action, object? value = null)
            => OrbitalAction?.Invoke(this, new RadialOrbitalActionEventArgs(action, value));

        #endregion

        #region State 1: Bút viết

        private void BuildPenOrbitals()
        {
            var accent = StateColors["pen"];

            // Cung trái: độ dày nét 1–16px
            BuildArcSlider(accent, 1, 16, _penSize,
                chipText: v => $"{v} px",
                onPreview: null,
                onCommit: v => { _penSize = v; Raise("penSize", v); },
                bottomCard: BuildPenPreviewCard(accent, out var previewBar),
                onValue: v => previewBar.Height = Math.Clamp(v * 0.9, 1.5, 14));

            // Cung phải trong: 6 loại bút
            int n = PenTypes.Length;
            for (int i = 0; i < n; i++)
            {
                var (type, label, icon, iconColor) = PenTypes[i];
                double deg = -50 + i * (100.0 / (n - 1));
                bool active = type == _penType;
                var pill = MakePill(label, icon, accent, active, () =>
                {
                    _penType = type;
                    Raise("penType", type);
                    RebuildOrbitals();
                }, iconBrush: active ? null : Hex(iconColor));
                PlaceRight(pill, RightPillRadius, deg);
            }

            // Cung phải ngoài: màu nhanh + nút bảng màu
            int cn = QuickColors.Length + 1;
            for (int i = 0; i < cn; i++)
            {
                double deg = -33 + i * (66.0 / (cn - 1));
                var p = Polar(ColorRadius, deg);
                if (i < QuickColors.Length)
                {
                    var c = (Color)ColorConverter.ConvertFromString(QuickColors[i]);
                    bool selected = c == _penColor;
                    var dot = MakeColorDot(c, selected, () =>
                    {
                        _penColor = c;
                        Raise("penColor", c);
                        RebuildOrbitals();
                    });
                    PlaceCenter(dot, p);
                }
                else
                {
                    bool custom = Array.IndexOf(QuickColors, ColorToHex(_penColor)) < 0;
                    var paletteBtn = MakeRoundIconButton(IconPalette, accent, custom ? new SolidColorBrush(_penColor) : null, TogglePalettePopover);
                    paletteBtn.ToolTip = "Bảng 16 màu mở rộng";
                    PlaceCenter(paletteBtn, p);
                }
            }
        }

        private FrameworkElement BuildPenPreviewCard(Color accent, out Border previewBar)
        {
            previewBar = new Border
            {
                Width = 64,
                Height = Math.Clamp(_penSize * 0.9, 1.5, 14),
                CornerRadius = new CornerRadius(7),
                Background = new SolidColorBrush(accent),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 0),
            };
            return MakeCard(new StackPanel
            {
                Children =
                {
                    MakeText("Nét vẽ mượt", 10.5, FontWeights.Medium, SubTextBrush, HorizontalAlignment.Center),
                    new Grid { Height = 16, Children = { previewBar } }
                }
            });
        }

        #endregion

        #region State 2: Tẩy

        private void BuildEraserOrbitals()
        {
            var accent = StateColors["eraser"];

            Ellipse? zone = null;
            var card = BuildEraserZoneCard(accent, out zone);

            BuildArcSlider(accent, 5, 50, _eraserSize,
                chipText: v => $"Cỡ tẩy: {v} px",
                onPreview: null,
                onCommit: v => { _eraserSize = v; Raise("eraserSize", v); },
                bottomCard: card,
                onValue: v => { if (zone != null) { zone.Width = zone.Height = Math.Clamp(v, 6, 50); } });

            for (int i = 0; i < EraserModes.Length; i++)
            {
                var (mode, title, sub, icon) = EraserModes[i];
                double deg = -34 + i * 34;
                bool isClear = mode == "ClearAll";
                bool active = !isClear && mode == _eraserMode;
                var pill = MakeTwoLinePill(title, sub, icon, accent, active, isClear, () =>
                {
                    if (isClear)
                    {
                        Raise("eraserClearAll");
                    }
                    else
                    {
                        _eraserMode = mode;
                        Raise("eraserMode", mode);
                        RebuildOrbitals();
                    }
                });
                PlaceRight(pill, RightPillRadius, deg);
            }
        }

        private FrameworkElement BuildEraserZoneCard(Color accent, out Ellipse zone)
        {
            zone = new Ellipse
            {
                Width = Math.Clamp(_eraserSize, 6, 50),
                Height = Math.Clamp(_eraserSize, 6, 50),
                Stroke = new SolidColorBrush(accent),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 3, 2 },
                Fill = new SolidColorBrush(Color.FromArgb(30, accent.R, accent.G, accent.B)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            return MakeCard(new StackPanel
            {
                Children =
                {
                    new Grid { Width = 56, Height = 54, Children = { zone } },
                    MakeText("Vùng xóa", 10.5, FontWeights.SemiBold, TextBrush, HorizontalAlignment.Center),
                    MakeText("Kéo cung để đổi cỡ (5–50px)", 9.5, FontWeights.Normal, SubTextBrush, HorizontalAlignment.Center),
                }
            });
        }

        #endregion

        #region State 3: Chọn vùng & AI

        private void BuildSelectOrbitals()
        {
            var accent = StateColors["select"];

            // Cung trái: công cụ chọn
            double[] leftDegs = { 208, 180, 152 };
            for (int i = 0; i < SelectModes.Length; i++)
            {
                var (mode, label, icon) = SelectModes[i];
                bool active = mode == _selectMode && _activeKey == "select";
                var pill = MakePill(label, icon, accent, active, () =>
                {
                    _selectMode = mode;
                    Raise("selectMode", mode);
                    RebuildOrbitals();
                });
                pill.Width = LeftPillWidth;
                PlaceLeft(pill, LeftPillRadius, leftDegs[i]);
            }

            // Cung phải: thao tác AI thông minh
            int n = SmartActions.Length;
            for (int i = 0; i < n; i++)
            {
                var (action, label, icon) = SmartActions[i];
                double deg = -44 + i * (88.0 / (n - 1));
                var pill = MakePill(label, icon, accent, false, () => Raise("smartAction", action), iconBrush: new SolidColorBrush(accent));
                PlaceRight(pill, RightPillRadius, deg);
            }
        }

        #endregion

        #region State 4: Hình học

        private void BuildShapeOrbitals()
        {
            var accent = StateColors["shapes"];

            // Cung trái: công tắc 2D / 3D
            var btn2D = MakeToggleSegment("2D", IconShapes, accent, !_shape3D, () => { _shape3D = false; RebuildOrbitals(); });
            var btn3D = MakeToggleSegment("3D", Shapes3D[0].Icon, accent, _shape3D, () => { _shape3D = true; RebuildOrbitals(); });
            var toggleCard = MakeCard(new StackPanel
            {
                Width = 104,
                Children =
                {
                    btn2D,
                    btn3D,
                    MakeText("Chế độ hình", 10.5, FontWeights.Medium, SubTextBrush, HorizontalAlignment.Center, new Thickness(0, 4, 0, 0)),
                }
            });
            PlaceLeft(toggleCard, LeftPillRadius - 6, 180);

            // Cung phải: bộ hình
            var set = _shape3D ? Shapes3D : Shapes2D;
            int n = set.Length;
            double span = n > 6 ? 108 : 90;
            for (int i = 0; i < n; i++)
            {
                var (shape, label, icon) = set[i];
                double deg = -span / 2 + i * (span / (n - 1));
                bool active = shape == _activeShape && _activeKey == "shapes";
                var pill = MakePill(label, icon, accent, active, () =>
                {
                    _activeShape = shape;
                    Raise("shape", shape);
                    RebuildOrbitals();
                }, height: 34, iconBrush: active ? null : new SolidColorBrush(accent));
                PlaceRight(pill, RightPillRadius, deg);
            }
        }

        #endregion

        #region Arc slider

        private bool _sliderDragging;

        /// <summary>
        /// Slider cong bên trái vòng: đầu trên = giá trị nhỏ, đầu dưới = giá trị lớn.
        /// Kéo bằng chuột / ngón tay / bút; chạm vào bất kỳ điểm nào trên cung để nhảy tới giá trị.
        /// </summary>
        private void BuildArcSlider(Color accent, int min, int max, int value,
            Func<int, string> chipText, Action<int>? onPreview, Action<int> onCommit,
            FrameworkElement bottomCard, Action<int> onValue)
        {
            var accentBrush = new SolidColorBrush(accent);

            var track = new Path
            {
                Data = ArcGeometry(SliderRadius, SliderBottomDeg, SliderTopDeg),
                Stroke = BorderBrushSoft,
                StrokeThickness = 10,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                IsHitTestVisible = false,
            };
            var fill = new Path
            {
                Stroke = new SolidColorBrush(Color.FromArgb(150, accent.R, accent.G, accent.B)),
                StrokeThickness = 10,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                IsHitTestVisible = false,
            };
            orbitalLayer.Children.Add(track);
            orbitalLayer.Children.Add(fill);

            // Chấm minh hoạ độ dày tăng dần
            for (int i = 0; i < 5; i++)
            {
                double t = i / 4.0;
                double deg = SliderTopDeg - t * (SliderTopDeg - SliderBottomDeg);
                double r = 2.5 + t * 4.5;
                var dot = new Ellipse
                {
                    Width = r * 2, Height = r * 2,
                    Fill = Hex("#B8C2CC"),
                    IsHitTestVisible = false,
                };
                PlaceCenter(dot, Polar(SliderRadius - 24, deg));
            }

            var knobDot = new Ellipse { Width = 9, Height = 9, Fill = accentBrush, IsHitTestVisible = false };
            var knob = new Grid
            {
                Width = 30, Height = 30,
                IsHitTestVisible = false,
                Children =
                {
                    new Ellipse
                    {
                        Fill = WhiteBrush, Stroke = accentBrush, StrokeThickness = 3,
                        Effect = new DropShadowEffect { Color = Color.FromRgb(0x1E, 0x2A, 0x3A), BlurRadius = 8, ShadowDepth = 1, Opacity = 0.25, Direction = 270 }
                    },
                    knobDot
                }
            };
            orbitalLayer.Children.Add(knob);

            // Chip giá trị (phía trên cung)
            var chipLabel = MakeText(chipText(value), 12, FontWeights.Bold, accentBrush, HorizontalAlignment.Left);
            var chip = MakeCard(new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children =
                {
                    new Ellipse { Width = 8, Height = 8, Fill = accentBrush, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) },
                    chipLabel
                }
            }, new Thickness(10, 5, 10, 5), 14);
            orbitalLayer.Children.Add(chip);
            var top = Polar(SliderRadius, SliderTopDeg);
            chip.Loaded += (s, e) => { Canvas.SetLeft(chip, top.X - chip.ActualWidth / 2 - 6); Canvas.SetTop(chip, top.Y - chip.ActualHeight - 22); };
            Canvas.SetLeft(chip, top.X - 40); Canvas.SetTop(chip, top.Y - 52);

            // Thẻ minh hoạ (phía dưới cung)
            orbitalLayer.Children.Add(bottomCard);
            var bottom = Polar(SliderRadius, SliderBottomDeg);
            bottomCard.Loaded += (s, e) => { Canvas.SetLeft(bottomCard, bottom.X - bottomCard.ActualWidth / 2 - 6); Canvas.SetTop(bottomCard, bottom.Y + 22); };
            Canvas.SetLeft(bottomCard, bottom.X - 50); Canvas.SetTop(bottomCard, bottom.Y + 22);

            int current = Math.Clamp(value, min, max);

            void Render(int v)
            {
                double t = (v - min) / (double)(max - min);
                double deg = SliderTopDeg - t * (SliderTopDeg - SliderBottomDeg);
                fill.Data = Math.Abs(deg - SliderTopDeg) < 0.5 ? null : ArcGeometry(SliderRadius, deg, SliderTopDeg);
                PlaceCenter(knob, Polar(SliderRadius, deg));
                chipLabel.Text = chipText(v);
                onValue(v);
            }

            int ValueFromPoint(Point p)
            {
                double deg = Math.Atan2(p.Y - Cy, p.X - Cx) * 180 / Math.PI;
                if (deg < 0) deg += 360;
                deg = Math.Clamp(deg, SliderBottomDeg, SliderTopDeg);
                double t = (SliderTopDeg - deg) / (SliderTopDeg - SliderBottomDeg);
                return (int)Math.Round(min + t * (max - min));
            }

            // Vùng bắt chạm rộng dọc theo cung (dễ thao tác bằng ngón tay)
            var hit = new Path
            {
                Data = ArcGeometry(SliderRadius, SliderBottomDeg - 4, SliderTopDeg + 4),
                Stroke = Brushes.Transparent,
                StrokeThickness = 48,
                Cursor = Cursors.Hand,
                ToolTip = "Kéo để thay đổi",
            };
            orbitalLayer.Children.Add(hit);

            ToolbarPointerHelper.AttachDrag(hit, rootCanvas,
                onDown: p =>
                {
                    _sliderDragging = true;
                    current = ValueFromPoint(p);
                    Render(current);
                    onPreview?.Invoke(current);
                },
                onMove: p =>
                {
                    int v = ValueFromPoint(p);
                    if (v == current) return;
                    current = v;
                    Render(current);
                    onPreview?.Invoke(current);
                },
                onUp: () =>
                {
                    _sliderDragging = false;
                    onCommit(current);
                });

            Render(current);
        }

        #endregion

        #region Palette popover

        private Color _pendingPaletteColor;

        private void TogglePalettePopover()
        {
            if (palettePopover.Visibility == Visibility.Visible) { HidePalettePopover(); return; }
            _pendingPaletteColor = _penColor;
            BuildPalettePopover();
            Canvas.SetLeft(palettePopover, Cx + ColorRadius - 36 - 236);
            Canvas.SetTop(palettePopover, Cy - 150);
            palettePopover.Visibility = Visibility.Visible;
            palettePopover.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(140)));
        }

        private void HidePalettePopover()
        {
            if (palettePopover != null) palettePopover.Visibility = Visibility.Collapsed;
        }

        private void BuildPalettePopover()
        {
            var accent = StateColors["pen"];
            var root = new StackPanel();

            // Tiêu đề
            var header = new DockPanel { Margin = new Thickness(0, 0, 0, 8), LastChildFill = true };
            var closeBtn = MakeRoundIconButton(IconClose, Color.FromRgb(0x74, 0x7D, 0x8C), null, HidePalettePopover, 26);
            DockPanel.SetDock(closeBtn, Dock.Right);
            header.Children.Add(closeBtn);
            header.Children.Add(MakeText("Màu Pastel (16 màu)", 12.5, FontWeights.Bold, TextBrush, HorizontalAlignment.Left));
            root.Children.Add(header);
            root.Children.Add(new Border { Height = 1, Background = BorderBrushSoft, Margin = new Thickness(0, 0, 0, 10) });

            // Lưới 4x4
            var grid = new UniformGridLite(4);
            var hexBox = new TextBox
            {
                Height = 30,
                Padding = new Thickness(6, 4, 6, 4),
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                FontSize = 12.5,
                BorderBrush = BorderBrushSoft,
                Text = ColorToHex(_pendingPaletteColor),
                VerticalContentAlignment = VerticalAlignment.Center,
            };

            var swatchChecks = new List<(Color c, Path check)>();
            void RefreshChecks()
            {
                foreach (var (c, check) in swatchChecks)
                    check.Visibility = c == _pendingPaletteColor ? Visibility.Visible : Visibility.Collapsed;
            }

            foreach (var hex in PaletteColors)
            {
                var c = (Color)ColorConverter.ConvertFromString(hex);
                bool light = (c.R * 0.299 + c.G * 0.587 + c.B * 0.114) > 160;
                var check = new Path
                {
                    Data = Geometry.Parse(IconCheck),
                    Fill = light ? TextBrush : WhiteBrush,
                    Stretch = Stretch.Uniform,
                    Width = 14, Height = 14,
                    IsHitTestVisible = false,
                };
                swatchChecks.Add((c, check));
                var sw = new Button
                {
                    Width = 38, Height = 38,
                    Margin = new Thickness(3),
                    Background = new SolidColorBrush(c),
                    BorderBrush = light ? Hex("#CBD5E1") : WhiteBrush,
                    BorderThickness = new Thickness(1),
                    Cursor = Cursors.Hand,
                    Focusable = false,
                    Template = CreatePillTemplate(9),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    Content = check,
                    ToolTip = hex,
                };
                sw.Click += (s, e) =>
                {
                    _pendingPaletteColor = c;
                    hexBox.Text = hex;
                    RefreshChecks();
                };
                AttachButtonTouch(sw);
                grid.Add(sw);
            }
            RefreshChecks();
            root.Children.Add(grid.Panel);

            // HEX
            var hexRow = new DockPanel { Margin = new Thickness(0, 10, 0, 10) };
            var hexLabel = MakeText("HEX:", 11.5, FontWeights.SemiBold, SubTextBrush, HorizontalAlignment.Left, new Thickness(0, 0, 8, 0));
            hexLabel.VerticalAlignment = VerticalAlignment.Center;
            DockPanel.SetDock(hexLabel, Dock.Left);
            hexRow.Children.Add(hexLabel);
            hexRow.Children.Add(hexBox);
            hexBox.TextChanged += (s, e) =>
            {
                if (TryParseHex(hexBox.Text, out var c))
                {
                    _pendingPaletteColor = c;
                    hexBox.BorderBrush = BorderBrushSoft;
                    RefreshChecks();
                }
                else hexBox.BorderBrush = Hex("#EE5A6F");
            };
            root.Children.Add(hexRow);

            // Nút hành động
            var actions = new UniformGridLite(3);
            actions.Add(MakeFlatButton("Đặt lại", false, accent, () =>
            {
                _pendingPaletteColor = Colors.White;
                hexBox.Text = "#FFFFFF";
                RefreshChecks();
            }));
            actions.Add(MakeFlatButton("Áp dụng", true, accent, () =>
            {
                _penColor = _pendingPaletteColor;
                Raise("penColor", _penColor);
                HidePalettePopover();
                RebuildOrbitals();
            }));
            actions.Add(MakeFlatButton("Đóng", false, accent, HidePalettePopover));
            root.Children.Add(actions.Panel);

            palettePopover.Child = root;
        }

        /// <summary>Lưới đều tối giản (tránh phụ thuộc thêm namespace Primitives cho UniformGrid).</summary>
        private sealed class UniformGridLite
        {
            public readonly System.Windows.Controls.Primitives.UniformGrid Panel;
            public UniformGridLite(int columns) => Panel = new System.Windows.Controls.Primitives.UniformGrid { Columns = columns };
            public void Add(UIElement e) => Panel.Children.Add(e);
        }

        #endregion

        #region Factories

        private static Point Polar(double r, double deg)
        {
            double rad = deg * Math.PI / 180.0;
            return new Point(Cx + r * Math.Cos(rad), Cy + r * Math.Sin(rad));
        }

        /// <summary>Cung tròn từ fromDeg tới toDeg (chiều kim đồng hồ) quanh tâm vòng.</summary>
        private static Geometry ArcGeometry(double r, double fromDeg, double toDeg)
        {
            var fig = new PathFigure { StartPoint = Polar(r, fromDeg), IsClosed = false, IsFilled = false };
            fig.Segments.Add(new ArcSegment(Polar(r, toDeg), new Size(r, r), 0, (toDeg - fromDeg) > 180, SweepDirection.Clockwise, true));
            var g = new PathGeometry();
            g.Figures.Add(fig);
            g.Freeze();
            return g;
        }

        private void PlaceCenter(FrameworkElement e, Point p)
        {
            if (!orbitalLayer.Children.Contains(e)) orbitalLayer.Children.Add(e);
            void Apply()
            {
                double w = e.ActualWidth > 0 ? e.ActualWidth : (double.IsNaN(e.Width) ? 0 : e.Width);
                double h = e.ActualHeight > 0 ? e.ActualHeight : (double.IsNaN(e.Height) ? 0 : e.Height);
                Canvas.SetLeft(e, p.X - w / 2);
                Canvas.SetTop(e, p.Y - h / 2);
            }
            Apply();
            if (e.ActualWidth <= 0) e.Loaded += (s, a) => Apply();
        }

        /// <summary>Mép trái phần tử đặt tại điểm trên cung (phần tử toả ra phía phải).</summary>
        private void PlaceRight(FrameworkElement e, double r, double deg)
        {
            orbitalLayer.Children.Add(e);
            var p = Polar(r, deg);
            Canvas.SetLeft(e, p.X);
            double h = double.IsNaN(e.Height) ? 36 : e.Height;
            Canvas.SetTop(e, p.Y - h / 2);
            e.Loaded += (s, a) => Canvas.SetTop(e, p.Y - e.ActualHeight / 2);
        }

        /// <summary>Mép phải phần tử đặt tại điểm trên cung (phần tử toả ra phía trái).</summary>
        private void PlaceLeft(FrameworkElement e, double r, double deg)
        {
            orbitalLayer.Children.Add(e);
            var p = Polar(r, deg);
            double w = double.IsNaN(e.Width) ? LeftPillWidth : e.Width;
            double h = double.IsNaN(e.Height) ? 36 : e.Height;
            Canvas.SetLeft(e, p.X - w);
            Canvas.SetTop(e, p.Y - h / 2);
            e.Loaded += (s, a) =>
            {
                Canvas.SetLeft(e, p.X - e.ActualWidth);
                Canvas.SetTop(e, p.Y - e.ActualHeight / 2);
            };
        }

        private static TextBlock MakeText(string text, double size, FontWeight weight, Brush brush,
            HorizontalAlignment align, Thickness? margin = null)
        {
            var t = new TextBlock
            {
                Text = text,
                FontSize = size,
                FontWeight = weight,
                Foreground = brush,
                HorizontalAlignment = align,
                Margin = margin ?? new Thickness(0),
                IsHitTestVisible = false,
            };
            t.SetResourceReference(TextBlock.FontFamilyProperty, "InterFont");
            return t;
        }

        private static Border MakeCard(UIElement child, Thickness? padding = null, double radius = 14)
        {
            return new Border
            {
                Background = WhiteBrush,
                BorderBrush = BorderBrushSoft,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(radius),
                Padding = padding ?? new Thickness(10, 7, 10, 7),
                Child = child,
                Effect = new DropShadowEffect { Color = Color.FromRgb(0x1E, 0x2A, 0x3A), BlurRadius = 10, ShadowDepth = 1, Opacity = 0.14, Direction = 270 },
            };
        }

        private static DropShadowEffect PillShadow(Color? accent = null) => new()
        {
            Color = accent ?? Color.FromRgb(0x1E, 0x2A, 0x3A),
            BlurRadius = accent.HasValue ? 12 : 8,
            ShadowDepth = accent.HasValue ? 3 : 1,
            Opacity = accent.HasValue ? 0.35 : 0.14,
            Direction = 270,
        };

        private static Viewbox MakeIcon(string data, Brush fill, double size)
            => new()
            {
                Width = size,
                Height = size,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false,
                Child = new Canvas { Width = 24, Height = 24, Children = { new Path { Data = Geometry.Parse(data), Fill = fill } } }
            };

        /// <summary>Viên thuốc 1 dòng: icon + nhãn. Active = nền màu trạng thái, chữ trắng.</summary>
        private Button MakePill(string label, string icon, Color accent, bool active, Action onClick,
            double height = 36, Brush? iconBrush = null)
        {
            var fg = active ? WhiteBrush : TextBrush;
            var btn = new Button
            {
                Height = height,
                Padding = new Thickness(12, 0, 14, 0),
                Background = active ? new SolidColorBrush(accent) : WhiteBrush,
                BorderBrush = active ? WhiteBrush : BorderBrushSoft,
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                Focusable = false,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Template = CreatePillTemplate(height / 2),
                Effect = PillShadow(active ? accent : null),
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Children =
                    {
                        MakeIcon(icon, active ? WhiteBrush : (iconBrush ?? TextBrush), 17),
                        MakeText(label, 12, active ? FontWeights.Bold : FontWeights.SemiBold, fg, HorizontalAlignment.Left, new Thickness(7, 0, 0, 0)),
                    }
                }
            };
            WireHover(btn, active);
            btn.Click += (s, e) => onClick();
            AttachButtonTouch(btn);
            return btn;
        }

        /// <summary>Viên thuốc 2 dòng lớn (chế độ tẩy).</summary>
        private Button MakeTwoLinePill(string title, string sub, string icon, Color accent, bool active, bool danger, Action onClick)
        {
            var danger1 = Color.FromRgb(0xEE, 0x5A, 0x6F);
            var iconBg = active ? WhiteBrush : new SolidColorBrush(Color.FromArgb(36, (danger ? danger1 : accent).R, (danger ? danger1 : accent).G, (danger ? danger1 : accent).B));
            var iconFg = active ? new SolidColorBrush(accent) : new SolidColorBrush(danger ? danger1 : accent);
            var btn = new Button
            {
                Height = 56,
                Padding = new Thickness(8, 0, 18, 0),
                Background = active ? new SolidColorBrush(accent) : WhiteBrush,
                BorderBrush = active ? WhiteBrush : BorderBrushSoft,
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                Focusable = false,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Template = CreatePillTemplate(28),
                Effect = PillShadow(active ? accent : null),
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Children =
                    {
                        new Border
                        {
                            Width = 40, Height = 40, CornerRadius = new CornerRadius(20),
                            Background = iconBg,
                            Child = new Grid { Children = { MakeIcon(icon, iconFg, 20) } },
                            IsHitTestVisible = false,
                        },
                        new StackPanel
                        {
                            Margin = new Thickness(10, 0, 0, 0),
                            VerticalAlignment = VerticalAlignment.Center,
                            Children =
                            {
                                MakeText(title, 13, FontWeights.Bold, active ? WhiteBrush : (danger ? new SolidColorBrush(danger1) : TextBrush), HorizontalAlignment.Left),
                                MakeText(sub, 10.5, FontWeights.Normal, active ? WhiteBrush : SubTextBrush, HorizontalAlignment.Left),
                            }
                        }
                    }
                }
            };
            WireHover(btn, active);
            btn.Click += (s, e) => onClick();
            AttachButtonTouch(btn);
            return btn;
        }

        private Button MakeToggleSegment(string label, string icon, Color accent, bool on, Action onClick)
        {
            var btn = new Button
            {
                Height = 40,
                Margin = new Thickness(0, 0, 0, 4),
                Background = on ? new SolidColorBrush(accent) : ButtonBrush,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Focusable = false,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Template = CreatePillTemplate(12),
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Children =
                    {
                        MakeIcon(icon, on ? WhiteBrush : TextBrush, 16),
                        MakeText(label, 12.5, FontWeights.Bold, on ? WhiteBrush : TextBrush, HorizontalAlignment.Left, new Thickness(6, 0, 0, 0)),
                    }
                }
            };
            WireHover(btn, on, ButtonBrush);
            btn.Click += (s, e) => onClick();
            AttachButtonTouch(btn);
            return btn;
        }

        private Button MakeColorDot(Color c, bool selected, Action onClick)
        {
            double size = selected ? 34 : 30;
            var btn = new Button
            {
                Width = size + (selected ? 8 : 0),
                Height = size + (selected ? 8 : 0),
                Cursor = Cursors.Hand,
                Focusable = false,
                ToolTip = ColorToHex(c) + (selected ? " (Đang chọn)" : ""),
                Template = CreateColorDotTemplate(c, selected),
            };
            btn.Click += (s, e) => onClick();
            AttachButtonTouch(btn);
            return btn;
        }

        private static ControlTemplate CreateColorDotTemplate(Color c, bool selected)
        {
            var t = new ControlTemplate(typeof(Button));
            var grid = new FrameworkElementFactory(typeof(Grid));
            if (selected)
            {
                var ring = new FrameworkElementFactory(typeof(Ellipse));
                ring.SetValue(Shape.StrokeProperty, BrandBrush);
                ring.SetValue(Shape.StrokeThicknessProperty, 2.5);
                ring.SetValue(Shape.FillProperty, WhiteBrush);
                grid.AppendChild(ring);
            }
            var dot = new FrameworkElementFactory(typeof(Ellipse));
            dot.SetValue(Shape.FillProperty, new SolidColorBrush(c));
            dot.SetValue(Shape.StrokeProperty, (c.R + c.G + c.B) > 690 ? Hex("#CBD5E1") : WhiteBrush);
            dot.SetValue(Shape.StrokeThicknessProperty, 1.5);
            dot.SetValue(FrameworkElement.MarginProperty, new Thickness(selected ? 5 : 0));
            dot.SetValue(UIElement.EffectProperty, new DropShadowEffect { Color = Color.FromRgb(0x1E, 0x2A, 0x3A), BlurRadius = 6, ShadowDepth = 1, Opacity = 0.2, Direction = 270 });
            grid.AppendChild(dot);
            t.VisualTree = grid;
            return t;
        }

        private Button MakeRoundIconButton(string icon, Color accent, Brush? badge, Action onClick, double size = 38)
        {
            var content = new Grid { Children = { MakeIcon(icon, new SolidColorBrush(accent), size * 0.5) } };
            if (badge != null)
            {
                var b = new Ellipse { Width = 11, Height = 11, Fill = badge, Stroke = WhiteBrush, StrokeThickness = 2, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, -2, -4, 0), IsHitTestVisible = false };
                content.Children.Add(b);
            }
            var btn = new Button
            {
                Width = size, Height = size,
                Background = WhiteBrush,
                BorderBrush = BorderBrushSoft,
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                Focusable = false,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Template = CreatePillTemplate(size / 2),
                Effect = PillShadow(),
                Content = content,
            };
            WireHover(btn, false);
            btn.Click += (s, e) => onClick();
            AttachButtonTouch(btn);
            return btn;
        }

        private Button MakeFlatButton(string text, bool primary, Color accent, Action onClick)
        {
            var btn = new Button
            {
                Height = 32,
                Margin = new Thickness(2, 0, 2, 0),
                Background = primary ? new SolidColorBrush(accent) : ButtonBrush,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Focusable = false,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Template = CreatePillTemplate(9),
                Content = MakeText(text, 11.5, primary ? FontWeights.Bold : FontWeights.SemiBold, primary ? WhiteBrush : TextBrush, HorizontalAlignment.Center),
            };
            if (!primary) WireHover(btn, false, ButtonBrush);
            btn.Click += (s, e) => onClick();
            AttachButtonTouch(btn);
            return btn;
        }

        /// <summary>Hover chỉ cho chuột thật (tránh "dính" hover khi chạm).</summary>
        private static void WireHover(Button btn, bool active, Brush? normal = null)
        {
            if (active) return;
            var restore = normal ?? WhiteBrush;
            btn.MouseEnter += (s, e) => { if (e.StylusDevice == null) btn.Background = HoverBrush; };
            btn.MouseLeave += (s, e) => btn.Background = restore;
        }

        private static string ColorToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

        private static bool TryParseHex(string? text, out Color c)
        {
            c = Colors.White;
            if (string.IsNullOrWhiteSpace(text)) return false;
            var s = text.Trim().TrimStart('#');
            if (s.Length != 6 || !int.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int v)) return false;
            c = Color.FromRgb((byte)(v >> 16), (byte)(v >> 8), (byte)v);
            return true;
        }

        #endregion
    }
}
