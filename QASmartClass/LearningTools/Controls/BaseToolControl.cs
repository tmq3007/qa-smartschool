using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace QASmartClass.LearningTools.Controls
{
    /// <summary>
    /// Base class for all LearningTool controls providing shared UI helpers:
    /// — Card creation (Formula, Info, Result)
    /// — Badge/Tag rendering
    /// — Section header creation
    /// — Result row rendering with consistent styling
    /// 
    /// Design System: Enterprise gradient headers are in XAML.
    /// This class centralizes code-behind UI helper patterns.
    /// </summary>
    public abstract class BaseToolControl : UserControl, IDisposable
    {
        public virtual void Dispose()
        {
        }

        private DateTime _sessionStartTime;
        private static readonly object FileLock = new();

        protected BaseToolControl()
        {
            Loaded += (s, e) =>
            {
                _sessionStartTime = DateTime.UtcNow;
            };

            Unloaded += (s, e) =>
            {
                double duration = (DateTime.UtcNow - _sessionStartTime).TotalSeconds;
                if (duration >= 2.0) // Bỏ qua nếu thời gian mở quá ngắn (dưới 2 giây) để lọc nhiễu click nhầm
                {
                    SaveTelemetry(this.GetType().Name, _sessionStartTime, duration);
                }

                // Hide the numeric keyboard if it's currently open
                try
                {
                    TouchNumPad.HidePopup();
                }
                catch { }
            };
        }

        private class TelemetryRecord
        {
            public string ToolName { get; set; }
            public string StartTime { get; set; }
            public double DurationSeconds { get; set; }

            public TelemetryRecord(string toolName, DateTime start, double seconds)
            {
                ToolName = toolName;
                StartTime = start.ToString("o");
                DurationSeconds = seconds;
            }
        }

        private void SaveTelemetry(string toolName, DateTime start, double seconds)
        {
            Task.Run(() =>
            {
                lock (FileLock)
                {
                    try
                    {
                        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                        var dir = Path.Combine(appData, "QA_SmartClass", "UserData");
                        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                        var path = Path.Combine(dir, "telemetry_usage.json");

                        List<TelemetryRecord> records = new();
                        if (File.Exists(path))
                        {
                            var oldJson = File.ReadAllText(path);
                            try
                            {
                                records = JsonSerializer.Deserialize<List<TelemetryRecord>>(oldJson) ?? new();
                            }
                            catch
                            {
                                records = new();
                            }
                        }

                        records.Add(new TelemetryRecord(toolName, start, seconds));
                        var newJson = JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(path, newJson, System.Text.Encoding.UTF8);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Telemetry error: " + ex.Message);
                    }
                }
            });
        }
        // ═══════════════════════════════════════════════════════════
        //  CARD BUILDERS — reusable across all tools
        // ═══════════════════════════════════════════════════════════

        /// <summary>Creates a formula card with icon circle + formula + description.</summary>
        protected static Border CreateFormulaCard(
            string icon, string formula, string description,
            string primaryColorHex, string secondaryColorHex)
        {
            var primary = (Color)ColorConverter.ConvertFromString(primaryColorHex);
            var secondary = (Color)ColorConverter.ConvertFromString(secondaryColorHex);

            var card = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20, 14, 20, 14),
                Margin = new Thickness(0, 0, 0, 16),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, primary.R, primary.G, primary.B)),
                BorderThickness = new Thickness(1)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Icon circle
            var iconBorder = new Border
            {
                Width = 56, Height = 56,
                CornerRadius = new CornerRadius(28),
                Margin = new Thickness(0, 0, 16, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Background = new LinearGradientBrush(primary, secondary, 45)
            };
            iconBorder.Child = new TextBlock
            {
                Text = icon, FontSize = 22,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(iconBorder, 0);
            grid.Children.Add(iconBorder);

            // Formula + description
            var textSp = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            textSp.Children.Add(new TextBlock
            {
                Text = formula,
                FontSize = 18, FontWeight = FontWeights.Bold,
                FontFamily = DS.FontMath,
                Foreground = new SolidColorBrush(primary)
            });
            textSp.Children.Add(new TextBlock
            {
                Text = description,
                FontSize = 13, Foreground = new SolidColorBrush(secondary),
                Margin = new Thickness(0, 2, 0, 0),
                TextWrapping = TextWrapping.Wrap
            });
            Grid.SetColumn(textSp, 1);
            grid.Children.Add(textSp);

            card.Child = grid;
            return card;
        }

        /// <summary>Creates a section header with icon and count.</summary>
        protected static TextBlock CreateSectionHeader(string text, string colorHex,
            double fontSize = 14, Thickness? margin = null)
        {
            var color = (Color)ColorConverter.ConvertFromString(colorHex);
            return new TextBlock
            {
                Text = text,
                FontSize = fontSize,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(color),
                Margin = margin ?? new Thickness(0, 12, 0, 6)
            };
        }

        /// <summary>Creates a result row with colored background.</summary>
        [System.Obsolete("Sử dụng UI.ResultRow(text, color, panel) thay thế — có click-to-copy + DS constants")]
        protected static Border CreateResultRow(string text, string colorHex)
        {
            var color = (Color)ColorConverter.ConvertFromString(colorHex);
            return new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(20, color.R, color.G, color.B)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 0, 0, 4),
                Child = new TextBlock
                {
                    Text = text, FontSize = DS.FontResult,
                    FontFamily = DS.FontPrimary,
                    Foreground = new SolidColorBrush(color)
                }
            };
        }

        /// <summary>Creates a small badge/tag element.</summary>
        protected static Border CreateBadge(string text, string bgColorHex)
        {
            var bg = (Color)ColorConverter.ConvertFromString(bgColorHex);
            return new Border
            {
                Background = new SolidColorBrush(bg),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(0, 0, 6, 0),
                Child = new TextBlock { Text = text, FontSize = 10 }
            };
        }

        /// <summary>Creates a white content card with optional colored left border.</summary>
        protected static Border CreateContentCard(string borderColorHex = null,
            double borderWidth = 4, Thickness? margin = null)
        {
            var card = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = margin ?? new Thickness(0, 0, 0, 6)
            };

            if (borderColorHex != null)
            {
                var color = (Color)ColorConverter.ConvertFromString(borderColorHex);
                card.BorderBrush = new SolidColorBrush(color);
                card.BorderThickness = new Thickness(borderWidth, 0, 0, 0);
            }

            return card;
        }

        /// <summary>Creates a reset button in the standard enterprise style.</summary>
        protected static Border CreateResetButton(string colorHex, string tooltip = "Đặt lại")
        {
            var color = (Color)ColorConverter.ConvertFromString(colorHex);
            return new Border
            {
                Background = new SolidColorBrush(color),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 6, 10, 6),
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = tooltip,
                Child = new TextBlock
                {
                    Text = "🔄 Reset", FontSize = 11,
                    Foreground = Brushes.White, FontWeight = FontWeights.SemiBold
                }
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  SECTION FOCUS TOOLBAR (Phương án A)
        //  Tạo mini toolbar Focus/Unfocus/Bảng trắng cho mỗi section
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Tạo mini toolbar gắn vào section card: 🎯 Focus | ❌ Unfocus | 🖊️ Bảng trắng
        /// Toolbar ẩn khi không hover, hiện khi GV rê chuột vào card.
        /// </summary>
        /// <param name="sectionElement">Border/FrameworkElement chứa nội dung section</param>
        /// <param name="toolId">ID của tool (VD: "grammar")</param>
        /// <param name="sectionId">ID section (VD: "present_perfect")</param>
        /// <param name="sectionName">Tên hiển thị (VD: "Present Perfect — Hiện tại hoàn thành")</param>
        private static Button MakeSectionButton(string text, Color bgColor, Color fgColor, string tooltip)
        {
            var btn = new Button
            {
                Content = text,
                FontSize = 12,
                Foreground = new SolidColorBrush(fgColor),
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(0, 0, 2, 0),
                Padding = new Thickness(6, 4, 6, 4),
                ToolTip = tooltip
            };

            var template = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.Name = "border";
            borderFactory.SetValue(Border.BackgroundProperty, new SolidColorBrush(bgColor));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            borderFactory.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Button.PaddingProperty));

            var contentPresenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(contentPresenterFactory);
            template.VisualTree = borderFactory;

            // Hover trigger (increase brightness slightly)
            var hoverColor = Color.FromArgb(
                bgColor.A,
                (byte)Math.Min(255, bgColor.R + 20),
                (byte)Math.Min(255, bgColor.G + 20),
                (byte)Math.Min(255, bgColor.B + 20)
            );
            var hoverTrigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter
            {
                TargetName = "border",
                Property = Border.BackgroundProperty,
                Value = new SolidColorBrush(hoverColor)
            });
            template.Triggers.Add(hoverTrigger);

            btn.Template = template;
            return btn;
        }

        protected static StackPanel CreateSectionToolbar(
            FrameworkElement sectionElement, string toolId, string sectionId, string sectionName)
        {
            var toolbar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 2, 0, 4),
                Opacity = 0,
                Tag = "SectionToolbar"
            };

            // 🎯 Focus
            var focusBtn = MakeSectionButton("🎯", Color.FromRgb(232, 245, 233), Color.FromRgb(46, 125, 50), $"Focus HS vào: {sectionName}");
            focusBtn.Click += (s, e) =>
            {
                Helpers.TeachingActionHelper.FocusSectionVisual(sectionElement, toolId, sectionId);
                e.Handled = true;
            };
            toolbar.Children.Add(focusBtn);

            // ❌ Unfocus
            var unfocusBtn = MakeSectionButton("❌", Color.FromRgb(255, 235, 238), Color.FromRgb(198, 40, 40), "Bỏ Focus section");
            unfocusBtn.Click += (s, e) =>
            {
                Helpers.TeachingActionHelper.UnfocusSectionVisual();
                e.Handled = true;
            };
            toolbar.Children.Add(unfocusBtn);

            // 🖊️ Bảng trắng
            var boardBtn = MakeSectionButton("🖊️", Color.FromRgb(243, 229, 245), Color.FromRgb(123, 31, 162), $"Chụp \"{sectionName}\" → Bảng trắng");
            boardBtn.Click += (s, e) =>
            {
                Helpers.TeachingActionHelper.SendSectionToWhiteboard(sectionElement, sectionName);
                e.Handled = true;
            };
            toolbar.Children.Add(boardBtn);

            return toolbar;
        }

        /// <summary>
        /// Wrap 1 section card (Border) trong Grid có toolbar hover.
        /// Toolbar ẩn bình thường, hiện khi GV rê chuột.
        /// </summary>
        protected Grid WrapWithSectionToolbar(
            Border sectionCard, string toolId, string sectionId, string sectionName)
        {
            var app = Application.Current as QASmartTouch.App;

            // Kiểm tra xem control này có được hiển thị bên trong cửa sổ StudentShell của học sinh không
            bool isInsideStudentShell = false;
            var window = Window.GetWindow(this);
            if (window != null && window.GetType().Name.Contains("StudentShell"))
            {
                isInsideStudentShell = true;
            }

            bool isTeacher = !isInsideStudentShell && (app == null || app.UserRoleService?.CurrentRole == QASmartClass.Shared.UserRole.Teacher);

            var wrapper = new Grid();
            wrapper.Tag = sectionId;
            if (!isTeacher)
            {
                wrapper.Children.Add(sectionCard);
                return wrapper;
            }

            wrapper.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            wrapper.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var toolbar = CreateSectionToolbar(sectionCard, toolId, sectionId, sectionName);
            Grid.SetRow(toolbar, 0);
            wrapper.Children.Add(toolbar);

            Grid.SetRow(sectionCard, 1);
            wrapper.Children.Add(sectionCard);

            // Hover → show/hide toolbar
            wrapper.MouseEnter += (s, e) => toolbar.Opacity = 1;
            wrapper.MouseLeave += (s, e) => toolbar.Opacity = 0;

            return wrapper;
        }
    }
}

