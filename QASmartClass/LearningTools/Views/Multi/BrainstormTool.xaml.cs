using QASmartClass.LearningTools.Controls;
using QASmartClass.LearningTools.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace QASmartClass.LearningTools.Views.Multi
{
    public partial class BrainstormTool : UserControl, IDisposable
    {
        private Color _currentNoteColor = Color.FromRgb(255, 249, 196); // #FFF9C4 (Yellow)
        private int _noteCount = 0;
        private readonly Random _rng = new Random();
        private string _currentTopicName = "Hôm nay em học được gì?";

        // Drag state
        private bool _isDragging = false;
        private Border _dragTarget = null;
        private Point _dragOffset;

        public BrainstormTool()
        {
            InitializeComponent();
            BoardScrollViewer.SizeChanged += BoardScrollViewer_SizeChanged;
            txtTopic.LostFocus += TxtTopic_LostFocus;

            Loaded += (_, _) =>
            {
                InitializeDatabase();
                LoadNotesFromDatabase();
                TouchTextPad.Attach(txtTopic, mode: "text");
                UpdateCanvasSize();
                LoadPracticalApps();

                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Ghi chú" : "Study Guide & Notes";
                if (menuTextBoard != null) menuTextBoard.Text = isVN ? "Bảng ý tưởng" : "Idea Board";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Real-world Applications";

                if (sideMenu != null)
                {
                    sideMenu.SelectedIndex = 0; // Default to main idea board workspace
                }
            };
        }

        private void BoardScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateCanvasSize();
        }

        private bool HasNotesInDatabase(string topic)
        {
            try
            {
                var dbPath = QASmartClass.Services.AppPaths.DatabaseFile;
                if (!System.IO.File.Exists(dbPath)) return false;

                using (var conn = CreateConnection(dbPath))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(*) FROM brainstorm_notes WHERE Topic = @Topic";
                        cmd.Parameters.AddWithValue("@Topic", topic);
                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            return Convert.ToInt64(result) > 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to check notes in database");
            }
            return false;
        }

        private void TxtTopic_LostFocus(object sender, RoutedEventArgs e)
        {
            string newTopic = txtTopic.Text ?? "";
            if (string.IsNullOrWhiteSpace(newTopic))
            {
                MessageBox.Show("Tên chủ đề thảo luận không được để trống!", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtTopic.Text = _currentTopicName;
                return;
            }

            if (newTopic == _currentTopicName)
            {
                SaveNotesToDatabase(_currentTopicName);
                return;
            }

            // Tự động lưu bảng thảo luận hiện tại vào CSDL dưới tên chủ đề cũ để không mất dữ liệu
            SaveNotesToDatabase(_currentTopicName);

            if (HasNotesInDatabase(newTopic))
            {
                // Chủ đề mới đã tồn tại, thực hiện tải lại dữ liệu của chủ đề đó lên bảng
                _currentTopicName = newTopic;
                LoadNotesFromDatabase();
            }
            else
            {
                // Chủ đề mới chưa từng tồn tại dữ liệu
                var result = MessageBox.Show(
                    $"Bạn có muốn ĐỔI TÊN chủ đề hiện tại thành '{newTopic}' không?\n\n" +
                    $"- Chọn 'Yes' để ĐỔI TÊN chủ đề cũ (giữ nguyên tất cả các giấy nhớ hiện có).\n" +
                    $"- Chọn 'No' để TẠO BẢNG MỚI trống cho chủ đề '{newTopic}'.\n" +
                    $"- Chọn 'Cancel' để hủy thao tác và quay về chủ đề cũ.",
                    "Thay đổi chủ đề", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Xóa dữ liệu của chủ đề cũ trong DB
                    try
                    {
                        var dbPath = QASmartClass.Services.AppPaths.DatabaseFile;
                        using (var conn = CreateConnection(dbPath))
                        {
                            conn.Open();
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = "DELETE FROM brainstorm_notes WHERE Topic = @Topic";
                                cmd.Parameters.AddWithValue("@Topic", _currentTopicName);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Serilog.Log.Error(ex, "Failed to delete old topic notes during rename");
                    }

                    _currentTopicName = newTopic;
                    SaveNotesToDatabase(_currentTopicName);
                }
                else if (result == MessageBoxResult.No)
                {
                    _currentTopicName = newTopic;
                    BoardCanvas.Children.Clear();
                    _noteCount = 0;
                    AddSampleNotes(); // Tải các note mẫu để hỗ trợ lớp học
                    SaveNotesToDatabase(_currentTopicName);
                }
                else
                {
                    // Trả về tên cũ
                    txtTopic.Text = _currentTopicName;
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  DATABASE STATE PERSISTENCE (Auto-save / Load)
        // ═══════════════════════════════════════════════════════════

        private Microsoft.Data.Sqlite.SqliteConnection CreateConnection(string dbPath)
        {
            var keyBytes = QASmartClass.Data.DbEncryptionKeyManager.GetOrInitializeKey();
            var hexKey = Convert.ToHexString(keyBytes);
            return new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath};Password={hexKey};Default Timeout=5;");
        }

        private void InitializeDatabase()
        {
            try
            {
                var dbPath = QASmartClass.Services.AppPaths.DatabaseFile;
                using (var conn = CreateConnection(dbPath))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            CREATE TABLE IF NOT EXISTS brainstorm_notes (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                Topic TEXT NOT NULL,
                                Content TEXT NOT NULL,
                                ColorHex TEXT NOT NULL,
                                PosX REAL NOT NULL,
                                PosY REAL NOT NULL,
                                Rotation REAL NOT NULL,
                                SavedAt TEXT NOT NULL
                            );";
                        cmd.ExecuteNonQuery();

                        // Thêm cột ZIndex an toàn hỗ trợ lưu thứ tự xếp chồng
                        try
                        {
                            cmd.CommandText = "ALTER TABLE brainstorm_notes ADD COLUMN ZIndex INTEGER DEFAULT 0;";
                            cmd.ExecuteNonQuery();
                        }
                        catch { /* Bỏ qua nếu cột ZIndex đã tồn tại */ }
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to initialize brainstorm database");
            }
        }

        private void LoadNotesFromDatabase()
        {
            try
            {
                var dbPath = QASmartClass.Services.AppPaths.DatabaseFile;
                if (!System.IO.File.Exists(dbPath))
                {
                    AddSampleNotes();
                    return;
                }

                using (var conn = CreateConnection(dbPath))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT Content, ColorHex, PosX, PosY, Rotation, ZIndex FROM brainstorm_notes WHERE Topic = @Topic";
                        cmd.Parameters.AddWithValue("@Topic", txtTopic.Text ?? "");

                        using (var reader = cmd.ExecuteReader())
                        {
                            bool hasLoadedNotes = false;
                            while (reader.Read())
                            {
                                if (!hasLoadedNotes)
                                {
                                    BoardCanvas.Children.Clear();
                                    hasLoadedNotes = true;
                                }

                                string content = reader.GetString(0);
                                string colorHex = reader.GetString(1);
                                double x = reader.GetDouble(2);
                                double y = reader.GetDouble(3);
                                double rotation = reader.GetDouble(4);

                                int zIndex = 0;
                                if (reader.FieldCount > 5 && !reader.IsDBNull(5))
                                {
                                    zIndex = reader.GetInt32(5);
                                }

                                Color color = (Color)ColorConverter.ConvertFromString(colorHex);
                                AddNoteAtWithZIndex(content, color, x, y, focusTextBox: false, rotationAngle: rotation, zIndexValue: zIndex);
                            }

                            if (!hasLoadedNotes)
                            {
                                AddSampleNotes();
                            }
                        }
                    }
                }
                _currentTopicName = txtTopic.Text ?? "";
                UpdateCanvasSize();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to load brainstorm notes");
                AddSampleNotes();
            }
        }

        private void AddSampleNotes()
        {
            BoardCanvas.Children.Clear();
            AddNoteAt("📌 Bức tường ý tưởng\nDùng để thảo luận nhóm, ghi nhận ý kiến nhanh và động não trong giờ học!", Color.FromRgb(255, 249, 196), 60, 40);
            AddNoteAt("✍️ Viết ý kiến cá nhân\nNhấn đúp chuột vào vùng bảng trống bất kỳ để tạo nhanh một giấy nhớ mới.", Color.FromRgb(187, 222, 251), 300, 100);
            AddNoteAt("💻 Học sinh tham gia\nHọc sinh nhập ý kiến trên điện thoại/máy tính bảng rồi gửi lên để giáo viên duyệt.", Color.FromRgb(200, 230, 201), 540, 40);
            AddNoteAt("🛠️ Sắp xếp dễ dàng\nNhấn giữ phần đầu giấy nhớ ⠿ để kéo thả di chuyển vị trí, hoặc nhấn ✕ để xóa.", Color.FromRgb(255, 205, 210), 180, 280);
        }

        private void SaveNotesToDatabase(string topic)
        {
            if (string.IsNullOrEmpty(topic)) return;
            try
            {
                var dbPath = QASmartClass.Services.AppPaths.DatabaseFile;
                using (var conn = CreateConnection(dbPath))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        // Xóa các ghi chú cũ của chủ đề này
                        using (var deleteCmd = conn.CreateCommand())
                        {
                            deleteCmd.Transaction = transaction;
                            deleteCmd.CommandText = "DELETE FROM brainstorm_notes WHERE Topic = @Topic";
                            deleteCmd.Parameters.AddWithValue("@Topic", topic);
                            deleteCmd.ExecuteNonQuery();
                        }

                        // Lưu trữ các ghi chú hiện tại
                        foreach (UIElement child in BoardCanvas.Children)
                        {
                            if (child is Border border && border.Child is Grid grid)
                            {
                                var textBox = grid.Children.OfType<TextBox>().FirstOrDefault();
                                if (textBox == null) continue;

                                string content = textBox.Text;
                                double x = Canvas.GetLeft(border);
                                double y = Canvas.GetTop(border);
                                double rotation = 0;
                                int zIndex = Panel.GetZIndex(border);

                                if (border.RenderTransform is RotateTransform rt)
                                {
                                    rotation = rt.Angle;
                                }

                                string colorHex = "#FFFFFF";
                                if (border.Background is SolidColorBrush scb)
                                {
                                    colorHex = scb.Color.ToString();
                                }

                                using (var insertCmd = conn.CreateCommand())
                                {
                                    insertCmd.Transaction = transaction;
                                    insertCmd.CommandText = @"
                                        INSERT INTO brainstorm_notes (Topic, Content, ColorHex, PosX, PosY, Rotation, ZIndex, SavedAt)
                                        VALUES (@Topic, @Content, @ColorHex, @PosX, @PosY, @Rotation, @ZIndex, @SavedAt);";
                                    insertCmd.Parameters.AddWithValue("@Topic", topic);
                                    insertCmd.Parameters.AddWithValue("@Content", content);
                                    insertCmd.Parameters.AddWithValue("@ColorHex", colorHex);
                                    insertCmd.Parameters.AddWithValue("@PosX", x);
                                    insertCmd.Parameters.AddWithValue("@PosY", y);
                                    insertCmd.Parameters.AddWithValue("@Rotation", rotation);
                                    insertCmd.Parameters.AddWithValue("@ZIndex", zIndex);
                                    insertCmd.Parameters.AddWithValue("@SavedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                    insertCmd.ExecuteNonQuery();
                                }
                            }
                        }
                        transaction.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to save brainstorm notes");
            }
        }

        private void SaveNotesToDatabase()
        {
            SaveNotesToDatabase(_currentTopicName);
        }

        // ═══════════════════════════════════════════════════════════
        //  ADD / REMOVE NOTES
        // ═══════════════════════════════════════════════════════════

        private void BtnAddNote_Click(object sender, RoutedEventArgs e)
        {
            double x = _rng.Next(40, System.Math.Max(60, (int)BoardCanvas.ActualWidth - 220));
            double y = _rng.Next(30, System.Math.Max(60, (int)BoardCanvas.ActualHeight - 220));
            AddNoteAt("", _currentNoteColor, x, y, focusTextBox: true);
            SaveNotesToDatabase();
        }

        public void AddNoteAt(string text, Color color, double x, double y, bool focusTextBox = false, double? rotationAngle = null)
        {
            AddNoteAtWithZIndex(text, color, x, y, focusTextBox, rotationAngle, 0);
        }

        public void AddNoteAtWithZIndex(string text, Color color, double x, double y, bool focusTextBox, double? rotationAngle, int zIndexValue)
        {
            _noteCount++;
            double rotation = rotationAngle ?? _rng.Next(-6, 7); // slight random tilt
            double originalRotation = rotation;

            // ── Build the Sticky Note ──
            var noteGrid = new Grid();
            noteGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) });  // Header (drag handle)
            noteGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content
            noteGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer

            // Header: drag handle + close button container (hitbox mở rộng)
            var header = new DockPanel { Background = new SolidColorBrush(DarkenColor(color, 0.08)) };
            var closeBtnContainer = new Border
            {
                Background = Brushes.Transparent,
                Padding = new Thickness(6), // Mở rộng vùng bấm chạm xung quanh dấu X
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = "Xóa note này"
            };
            var closeBtn = new TextBlock
            {
                Text = "✕", FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(158, 158, 158)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            closeBtnContainer.Child = closeBtn;
            DockPanel.SetDock(closeBtnContainer, Dock.Right);
            header.Children.Add(closeBtnContainer);

            // Drag handle icon
            var dragIcon = new TextBlock
            {
                Text = "⠿", FontSize = 16, Foreground = new SolidColorBrush(Color.FromRgb(189, 189, 189)),
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0),
                Cursor = Cursors.SizeAll
            };
            header.Children.Add(dragIcon);
            Grid.SetRow(header, 0);
            noteGrid.Children.Add(header);

            // Content: TextBox for editing (Nâng cỡ chữ và cấu hình co giãn chiều cao)
            var textBox = new TextBox
            {
                Text = text,
                FontSize = 20, // Tăng cỡ chữ lên 20 giúp HS nhìn rõ từ xa
                FontFamily = Application.Current?.TryFindResource("InterFont") as FontFamily ?? new FontFamily("Segoe UI"),
                Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66)),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                Padding = new Thickness(10, 6, 10, 6),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto // Tự động hiển thị scrollbar khi vượt quá MaxHeight
            };
            Grid.SetRow(textBox, 1);
            noteGrid.Children.Add(textBox);

            // Footer: author label (chữ to rõ ràng hơn)
            var footer = new TextBlock
            {
                Text = $"Note #{_noteCount}",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(158, 158, 158)),
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 8, 4),
                FontFamily = Application.Current?.TryFindResource("InterFont") as FontFamily ?? new FontFamily("Segoe UI")
            };
            Grid.SetRow(footer, 2);
            noteGrid.Children.Add(footer);

            // ── Wrap in Border (the note card) ──
            var noteBorder = new Border
            {
                Width = 200,
                MinHeight = 180, // Chiều cao tối thiểu 180, tự giãn rộng theo văn bản gõ
                MaxHeight = 320, // Giới hạn chiều cao tối đa ghi chú
                Background = new SolidColorBrush(color),
                CornerRadius = new CornerRadius(4),
                Child = noteGrid,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new RotateTransform(rotation),
                BorderThickness = new Thickness(2),
                BorderBrush = Brushes.Transparent,
                Effect = new DropShadowEffect
                {
                    BlurRadius = 10,
                    ShadowDepth = 4,
                    Opacity = 0.25,
                    Color = Colors.Black,
                    Direction = 315
                },
                Cursor = Cursors.SizeAll
            };

            // Gán bàn phím ảo
            TouchTextPad.Attach(textBox, mode: "text");

            // ── GotFocus / LostFocus để xoay note thẳng đứng và lưu DB ──
            textBox.GotFocus += (s, ev) =>
            {
                if (noteBorder.RenderTransform is RotateTransform rt)
                {
                    rt.Angle = 0; // Trở về thẳng đứng khi gõ chữ
                }
                noteBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(33, 150, 243));
            };

            textBox.LostFocus += (s, ev) =>
            {
                if (noteBorder.RenderTransform is RotateTransform rt)
                {
                    rt.Angle = originalRotation; // Trở lại góc nghiêng khi xong
                }
                noteBorder.BorderBrush = Brushes.Transparent;
                SaveNotesToDatabase(); // Auto-save
            };

            // Chặn nổi bọt sự kiện nhấp chuột và nhấp đúp từ ghi chú
            noteBorder.MouseLeftButtonDown += (s, ev) =>
            {
                ev.Handled = true; // Chặn sự kiện nổi bọt lên Canvas
            };

            // ── Position on Canvas ──
            Canvas.SetLeft(noteBorder, x);
            Canvas.SetTop(noteBorder, y);
            Panel.SetZIndex(noteBorder, zIndexValue);

            // ── Event Handlers ──
            var capturedBorder = noteBorder;
            closeBtnContainer.MouseLeftButtonDown += (s, ev) =>
            {
                ev.Handled = true;
                BoardCanvas.Children.Remove(capturedBorder);
                UpdateCanvasSize();
                SaveNotesToDatabase();
            };

            // Drag: start on header MouseDown
            header.MouseLeftButtonDown += (s, ev) =>
            {
                _isDragging = true;
                _dragTarget = capturedBorder;
                _dragOffset = ev.GetPosition(capturedBorder);

                // Bring to front
                int maxZ = 0;
                foreach (UIElement child in BoardCanvas.Children)
                {
                    int z = Panel.GetZIndex(child);
                    if (z > maxZ) maxZ = z;
                }
                Panel.SetZIndex(capturedBorder, maxZ + 1);

                header.CaptureMouse();
                ev.Handled = true;
            };

            header.MouseMove += (s, ev) =>
            {
                if (_isDragging && _dragTarget == capturedBorder)
                {
                    var pos = ev.GetPosition(BoardCanvas);
                    
                    // Giới hạn biên di chuyển (Margin an toàn 5px)
                    double minX = 5;
                    double minY = 5;
                    double maxX = BoardCanvas.ActualWidth - capturedBorder.ActualWidth - 5;
                    double maxY = BoardCanvas.ActualHeight - capturedBorder.ActualHeight - 5;
                    
                    if (maxX < minX) maxX = minX;
                    if (maxY < minY) maxY = minY;

                    double newLeft = global::System.Math.Max(minX, global::System.Math.Min(pos.X - _dragOffset.X, maxX));
                    double newTop = global::System.Math.Max(minY, global::System.Math.Min(pos.Y - _dragOffset.Y, maxY));

                    Canvas.SetLeft(capturedBorder, newLeft);
                    Canvas.SetTop(capturedBorder, newTop);

                    if (newLeft + 250 > BoardCanvas.Width || newTop + 230 > BoardCanvas.Height)
                    {
                        UpdateCanvasSize();
                    }
                }
            };

            header.MouseLeftButtonUp += (s, ev) =>
            {
                if (_isDragging && _dragTarget == capturedBorder)
                {
                    _isDragging = false;
                    _dragTarget = null;
                    header.ReleaseMouseCapture();
                    UpdateCanvasSize();
                    SaveNotesToDatabase();
                }
            };

            // Hover effect: lift up shadow
            noteBorder.MouseEnter += (s, ev) =>
            {
                if (noteBorder.Effect is DropShadowEffect dse)
                {
                    dse.BlurRadius = 18;
                    dse.ShadowDepth = 8;
                    dse.Opacity = 0.35;
                }
            };
            noteBorder.MouseLeave += (s, ev) =>
            {
                if (noteBorder.Effect is DropShadowEffect dse)
                {
                    dse.BlurRadius = 10;
                    dse.ShadowDepth = 4;
                    dse.Opacity = 0.25;
                }
            };

            BoardCanvas.Children.Add(noteBorder);
            UpdateCanvasSize();

            if (focusTextBox)
            {
                textBox.Loaded += (s, ev) => textBox.Focus();
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  TOOLBAR HANDLERS
        // ═══════════════════════════════════════════════════════════

        private void NoteColor_Click(object sender, RoutedEventArgs e)
        {
            string hex = "";
            if (sender is RadioButton rbtn && rbtn.Tag is string hexRadio)
            {
                hex = hexRadio;
            }
            else if (sender is Button btn && btn.Tag is string hexBtn)
            {
                hex = hexBtn;
            }

            if (!string.IsNullOrEmpty(hex))
            {
                _currentNoteColor = (Color)ColorConverter.ConvertFromString(hex);
            }
        }

        private void BtnClearAll_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Bạn có chắc muốn xóa tất cả giấy nhớ?",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                BoardCanvas.Children.Clear();
                _noteCount = 0;
                UpdateCanvasSize();
                SaveNotesToDatabase();
            }
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Double-click on canvas to quick-add a note at that position
            if (e.ClickCount == 2)
            {
                var pos = e.GetPosition(BoardCanvas);
                AddNoteAt("", _currentNoteColor, pos.X - 100, pos.Y - 90, focusTextBox: true);
                e.Handled = true;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════════

        /// <summary>Darken a color slightly for headers/accents</summary>
        private static Color DarkenColor(Color c, double factor)
        {
            return Color.FromRgb(
                (byte)(c.R * (1 - factor)),
                (byte)(c.G * (1 - factor)),
                (byte)(c.B * (1 - factor)));
        }

        private void UpdateCanvasSize()
        {
            if (BoardCanvas == null || BoardScrollViewer == null) return;

            double maxX = 0;
            double maxY = 0;

            foreach (UIElement child in BoardCanvas.Children)
            {
                if (child is FrameworkElement fe)
                {
                    double left = Canvas.GetLeft(fe);
                    double top = Canvas.GetTop(fe);

                    if (double.IsNaN(left)) left = 0;
                    if (double.IsNaN(top)) top = 0;

                    double width = fe.ActualWidth > 0 ? fe.ActualWidth : fe.Width;
                    double height = fe.ActualHeight > 0 ? fe.ActualHeight : fe.Height;

                    if (double.IsNaN(width) || width <= 0) width = 200; 
                    if (double.IsNaN(height) || height <= 0) height = 180;

                    double right = left + width;
                    double bottom = top + height;

                    if (right > maxX) maxX = right;
                    if (bottom > maxY) maxY = bottom;
                }
            }

            double viewportWidth = BoardScrollViewer.ActualWidth > 0 ? BoardScrollViewer.ActualWidth : 1100;
            double viewportHeight = BoardScrollViewer.ActualHeight > 0 ? BoardScrollViewer.ActualHeight : 600;

            double targetWidth = System.Math.Max(maxX + 100, viewportWidth);
            double targetHeight = System.Math.Max(maxY + 100, viewportHeight);

            if (double.IsNaN(BoardCanvas.Width) || System.Math.Abs(BoardCanvas.Width - targetWidth) > 1.0)
                BoardCanvas.Width = targetWidth;

            if (double.IsNaN(BoardCanvas.Height) || System.Math.Abs(BoardCanvas.Height - targetHeight) > 1.0)
                BoardCanvas.Height = targetHeight;
        }

        private void LoadPracticalApps()
        {
            bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
            string suffix = isVN ? "VN" : "EN";

            var items = new System.Collections.Generic.List<PracticalAppItem>
            {
                new PracticalAppItem
                {
                    Icon = "💡",
                    Title = isVN ? "Thiết kế Sản phẩm Mới" : "Product Design & Ideation",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_brainstorm_1_{suffix}.png",
                    Description = isVN 
                        ? "Brainstorming giúp các nhóm thiết kế tự do đưa ra ý tưởng độc đáo cho sản phẩm mà không sợ bị phán xét. Mọi ý kiến đều được ghi lại trực quan trên bảng trước khi chọn lọc." 
                        : "Brainstorming allows design teams to freely suggest unique product ideas without fear of judgment. All thoughts are visually recorded on the board before selection."
                },
                new PracticalAppItem
                {
                    Icon = "🔄",
                    Title = isVN ? "Cải tiến Quy trình" : "Process Improvement",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_brainstorm_2_{suffix}.png",
                    Description = isVN 
                        ? "Trong quản lý dự án (Agile/Scrum), brainstorming được dùng vào cuối kỳ để toàn đội cùng phân tích xem điều gì hoạt động tốt, điều gì cần cải tiến và đề xuất giải pháp hành động cụ thể." 
                        : "In project management (Agile/Scrum), brainstorming is used during retrospectives for the team to analyze what went well, what needs improvement, and propose concrete action items."
                },
                new PracticalAppItem
                {
                    Icon = "🎬",
                    Title = isVN ? "Biên kịch & Sáng tạo" : "Storyboarding & Creative Writing",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_brainstorm_3_{suffix}.png",
                    Description = isVN 
                        ? "Các nhà sản xuất phim, biên kịch hoặc đội ngũ marketing sử dụng brainstorming để kết nối các tình tiết, xây dựng cốt truyện hoặc phác thảo ý tưởng cho các chiến dịch truyền thông." 
                        : "Filmmakers, writers, or marketing teams use brainstorming to connect plot points, build narratives, or sketch out ideas for media campaigns."
                }
            };

            try
            {
                if (practicalAppViewer != null) practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for BrainstormTool: {Err}", ex.Message);
            }
        }

        private void BtnHelp_Click(object sender, RoutedEventArgs e)
        {
            if (sideMenu != null) sideMenu.SelectedIndex = 1;
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewPractice == null || viewPractical == null)
                return;

            viewGuide.Visibility = Visibility.Collapsed;
            viewPractice.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            

            switch (sideMenu.SelectedIndex)
            {
                case 0:
                    viewGuide.Visibility = Visibility.Visible;
                    break;
                case 1:
                    viewPractice.Visibility = Visibility.Visible;
                    break;
                case 2:
                    viewPractical.Visibility = Visibility.Visible;
                    break;
            }
        }

        public void Dispose()
        {
            if (BoardScrollViewer != null)
            {
                BoardScrollViewer.SizeChanged -= BoardScrollViewer_SizeChanged;
            }
            if (txtTopic != null)
            {
                txtTopic.LostFocus -= TxtTopic_LostFocus;
            }

            BoardCanvas.Children.Clear();
        }
    }
}