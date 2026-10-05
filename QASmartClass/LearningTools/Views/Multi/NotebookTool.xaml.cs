using QASmartClass.LearningTools.Controls;
using QASmartClass.LearningTools.Helpers;
using QASmartClass.LearningTools.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;

namespace QASmartClass.LearningTools.Views.Multi
{
    public class NotebookItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "Sổ tay không tên";
        public Color CoverColor { get; set; } = Color.FromRgb(33, 150, 243);
        public string CoverType { get; set; } = "Smooth";
        public Color PaperColor { get; set; } = Colors.White;
        public int ThemeIndex { get; set; } = 0;
        public string PaperSizeTag { get; set; } = "1200x848";
        public int CurrentPageIndex { get; set; } = 0;
        public StrokeCollection Strokes { get; set; } = new StrokeCollection();
    }

    public partial class NotebookTool : BaseToolControl, IDisposable
    {
        public static Func<string, string, MessageBoxButton, MessageBoxImage, MessageBoxResult> MessageBoxShowHandler { get; set; } = MessageBox.Show;

        private ObservableCollection<NotebookItem> _notebooks = new ObservableCollection<NotebookItem>();
        private NotebookItem _currentNotebook;
        private NotebookItem _editingNotebook;
        private Color _tempCoverColor = Color.FromRgb(33, 150, 243);
        private Color _tempPaperColor = Colors.White;

        // Undo/Redo Stacks
        private Stack<CanvasState> _undoStack = new Stack<CanvasState>();
        private Stack<CanvasState> _redoStack = new Stack<CanvasState>();
        private bool _isUndoRedoing = false;

        private string _currentMode = "Ink";
        private Point _startPoint;
        private System.Windows.Shapes.Shape? _previewShape;
        private FrameworkElement? _selectedElement;
        private Point _elementStartPos;
        private bool _isDrawingShape = false;
        private bool _isMovingElement = false;
        private bool _isPanning = false;
        private bool _isSettingPattern = false;
        private Point _panStartMousePos;
        private double _panStartScrollH;
        private double _panStartScrollV;
        private System.Windows.Input.Cursor _customCursor = System.Windows.Input.Cursors.Pen;

        public NotebookTool()
        {
            InitializeComponent();
            
            // Set default ink attributes
            var inkAttr = new DrawingAttributes
            {
                Color = Color.FromRgb(33, 33, 33),
                Width = 3,
                Height = 3,
                FitToCurve = true,
                StylusTip = StylusTip.Ellipse
            };
            DrawingCanvas.DefaultDrawingAttributes = inkAttr;
            
            // Initialize CSDL SQLite
            InitializeDatabase();
            
            // Load notebooks from database
            LoadNotebooksFromDatabase();

            NotebookList.ItemsSource = _notebooks;
            
            Loaded += (_, _) => {
                LibraryScreen.Visibility = Visibility.Visible;
                EditorScreen.Visibility = Visibility.Collapsed;
                TouchTextPad.Attach(txtNbTitle, mode: "text");
                TouchTextPad.Attach(txtPaperDate, mode: "text"); // Support on-screen keyboard for date
                LoadPracticalApps();

                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Study Guide & Notes";
                if (menuTextNotebook != null) menuTextNotebook.Text = isVN ? "Sổ tay ghi chép" : "Notebook Workspace";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Real-world Applications";

                if (sideMenu != null)
                {
                    sideMenu.SelectedIndex = 0; // Default to notebook shelf
                }
            };
            Unloaded += NotebookTool_Unloaded;
            IsVisibleChanged += (s, e) => {
                if (!(bool)e.NewValue)
                {
                    SaveCurrentNotebook();
                }
            };

            PreviewKeyDown += NotebookTool_PreviewKeyDown;
            
            DrawingCanvas.MouseDown += DrawingCanvas_MouseDown;
            DrawingCanvas.MouseMove += DrawingCanvas_MouseMove;
            DrawingCanvas.MouseUp += DrawingCanvas_MouseUp;

            SetStrokes(DrawingCanvas.Strokes);
            ResetUndoRedo(DrawingCanvas.Strokes, new List<CanvasElementDto>());
            HighlightSelectedPenColor("#212121");
        }

        private void NotebookTool_Unloaded(object sender, RoutedEventArgs e)
        {
            SaveCurrentNotebook();
        }

        private void NotebookTool_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control)
            {
                if (e.Key == System.Windows.Input.Key.Z)
                {
                    PerformUndo();
                    e.Handled = true;
                }
                else if (e.Key == System.Windows.Input.Key.Y)
                {
                    PerformRedo();
                    e.Handled = true;
                }
            }
        }

        private SqliteConnection GetConnection()
        {
            var dbPath = QASmartClass.Services.AppPaths.DatabaseFile;
            var keyBytes = QASmartClass.Data.DbEncryptionKeyManager.GetOrInitializeKey();
            var hexKey = Convert.ToHexString(keyBytes);
            var connection = new SqliteConnection($"Data Source={dbPath};Password={hexKey};Default Timeout=5;");
            connection.Open();
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;";
                cmd.ExecuteNonQuery();
            }
            return connection;
        }

        private void InitializeDatabase()
        {
            var dbPath = QASmartClass.Services.AppPaths.DatabaseFile;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

                using (var connection = GetConnection())
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = @"
                            CREATE TABLE IF NOT EXISTS Notebooks (
                                Id TEXT PRIMARY KEY,
                                Title TEXT NOT NULL,
                                CoverColor TEXT NOT NULL,
                                CoverType TEXT NOT NULL,
                                PaperColor TEXT NOT NULL,
                                ThemeIndex INTEGER NOT NULL DEFAULT 0,
                                PaperSizeTag TEXT NOT NULL DEFAULT '1200x848',
                                CurrentPageIndex INTEGER NOT NULL DEFAULT 0,
                                CreatedAt TEXT NOT NULL
                            );

                            CREATE TABLE IF NOT EXISTS NotebookPages (
                                NotebookId TEXT NOT NULL,
                                PageIndex INTEGER NOT NULL,
                                StrokeData BLOB,
                                DateText TEXT,
                                ShiftText TEXT,
                                WeatherText TEXT,
                                MoodText TEXT,
                                PaperPattern TEXT DEFAULT 'Blank',
                                PRIMARY KEY (NotebookId, PageIndex),
                                FOREIGN KEY(NotebookId) REFERENCES Notebooks(Id) ON DELETE CASCADE
                            );";
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = "CREATE INDEX IF NOT EXISTS idx_notebook_pages ON NotebookPages (NotebookId, PageIndex);";
                        cmd.ExecuteNonQuery();

                        try
                        {
                            cmd.CommandText = "ALTER TABLE NotebookPages ADD COLUMN PaperPattern TEXT DEFAULT 'Blank';";
                            cmd.ExecuteNonQuery();
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxShowHandler($"Lỗi khởi tạo CSDL: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadNotebooksFromDatabase()
        {
            _notebooks.Clear();
            var dbPath = QASmartClass.Services.AppPaths.DatabaseFile;
            try
            {
                using (var connection = GetConnection())
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT Id, Title, CoverColor, CoverType, PaperColor, ThemeIndex, PaperSizeTag, CurrentPageIndex FROM Notebooks ORDER BY CreatedAt ASC";
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var nb = new NotebookItem
                                {
                                    Id = reader.GetString(0),
                                    Title = reader.GetString(1),
                                    CoverColor = (Color)ColorConverter.ConvertFromString(reader.GetString(2)),
                                    CoverType = reader.GetString(3),
                                    PaperColor = (Color)ColorConverter.ConvertFromString(reader.GetString(4)),
                                    ThemeIndex = reader.GetInt32(5),
                                    PaperSizeTag = reader.GetString(6),
                                    CurrentPageIndex = reader.GetInt32(7)
                                };
                                _notebooks.Add(nb);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxShowHandler($"Lỗi đọc CSDL: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Create default notebooks if DB is empty
            if (_notebooks.Count == 0)
            {
                var samples = new List<NotebookItem>
                {
                    new NotebookItem { Title = "Sổ Toán Học", CoverColor = Color.FromRgb(33, 150, 243), ThemeIndex = 1, CoverType="Smooth", PaperColor = Colors.White },
                    new NotebookItem { Title = "Nhật ký cá nhân", CoverColor = Color.FromRgb(233, 30, 99), ThemeIndex = 3, CoverType="Leather", PaperColor=Color.FromRgb(252, 228, 236) },
                    new NotebookItem { Title = "Ý tưởng sáng tạo", CoverColor = Color.FromRgb(255, 193, 7), ThemeIndex = 6, CoverType="Texture", PaperColor = Colors.White },
                    new NotebookItem { Title = "Ghi chú họp", CoverColor = Color.FromRgb(56, 142, 60), ThemeIndex = 4, CoverType="Leather", PaperColor=Color.FromRgb(253, 245, 230) },
                    new NotebookItem { Title = "Nháp Lý Hóa", CoverColor = Color.FromRgb(33, 33, 33), ThemeIndex = 5, CoverType="Texture", PaperColor=Color.FromRgb(33, 33, 33) }
                };

                foreach (var nb in samples)
                {
                    SaveNotebookMetadata(nb);
                    SavePageData(nb.Id, 0, Array.Empty<byte>(), DateTime.Now.ToString("dd/MM/yyyy"), "Sáng 🌅", "", "");
                    _notebooks.Add(nb);
                }
            }
        }

        private bool NotebookExists(string id)
        {
            var dbPath = QASmartClass.Services.AppPaths.DatabaseFile;
            using (var connection = GetConnection())
            {
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM Notebooks WHERE Id = $id";
                    cmd.Parameters.AddWithValue("$id", id);
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
        }

        private void SaveNotebookMetadata(NotebookItem nb)
        {
            var dbPath = QASmartClass.Services.AppPaths.DatabaseFile;
            try
            {
                using (var connection = GetConnection())
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        if (NotebookExists(nb.Id))
                        {
                            cmd.CommandText = @"
                                UPDATE Notebooks
                                SET Title = $title, CoverColor = $coverColor, CoverType = $coverType,
                                    PaperColor = $paperColor, ThemeIndex = $themeIndex, PaperSizeTag = $paperSizeTag,
                                    CurrentPageIndex = $currentPageIndex
                                WHERE Id = $id;";
                        }
                        else
                        {
                            cmd.CommandText = @"
                                INSERT INTO Notebooks (Id, Title, CoverColor, CoverType, PaperColor, ThemeIndex, PaperSizeTag, CurrentPageIndex, CreatedAt)
                                VALUES ($id, $title, $coverColor, $coverType, $paperColor, $themeIndex, $paperSizeTag, $currentPageIndex, $createdAt);";
                        }
                        cmd.Parameters.AddWithValue("$id", nb.Id);
                        cmd.Parameters.AddWithValue("$title", nb.Title);
                        cmd.Parameters.AddWithValue("$coverColor", nb.CoverColor.ToString());
                        cmd.Parameters.AddWithValue("$coverType", nb.CoverType);
                        cmd.Parameters.AddWithValue("$paperColor", nb.PaperColor.ToString());
                        cmd.Parameters.AddWithValue("$themeIndex", nb.ThemeIndex);
                        cmd.Parameters.AddWithValue("$paperSizeTag", nb.PaperSizeTag);
                        cmd.Parameters.AddWithValue("$currentPageIndex", nb.CurrentPageIndex);
                        cmd.Parameters.AddWithValue("$createdAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxShowHandler($"Lỗi lưu metadata: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteNotebook(string notebookId)
        {
            var dbPath = QASmartClass.Services.AppPaths.DatabaseFile;
            try
            {
                using (var connection = GetConnection())
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "DELETE FROM NotebookPages WHERE NotebookId = $nbId; DELETE FROM Notebooks WHERE Id = $nbId;";
                        cmd.Parameters.AddWithValue("$nbId", notebookId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxShowHandler($"Lỗi xóa sổ tay: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool PageExists(string notebookId, int pageIndex)
        {
            var dbPath = QASmartClass.Services.AppPaths.DatabaseFile;
            using (var connection = GetConnection())
            {
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM NotebookPages WHERE NotebookId = $nbId AND PageIndex = $pIndex";
                    cmd.Parameters.AddWithValue("$nbId", notebookId);
                    cmd.Parameters.AddWithValue("$pIndex", pageIndex);
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
        }

        private int GetPageCount(string notebookId)
        {
            var dbPath = QASmartClass.Services.AppPaths.DatabaseFile;
            try
            {
                using (var connection = GetConnection())
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT COUNT(*) FROM NotebookPages WHERE NotebookId = $nbId";
                        cmd.Parameters.AddWithValue("$nbId", notebookId);
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch
            {
                return 1;
            }
        }

        private void SavePageData(string notebookId, int pageIndex, byte[] strokeBytes, string date, string shift, string weather, string mood, string pattern = "Blank")
        {
            var dbPath = QASmartClass.Services.AppPaths.DatabaseFile;
            try
            {
                using (var connection = GetConnection())
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        if (PageExists(notebookId, pageIndex))
                        {
                            cmd.CommandText = @"
                                UPDATE NotebookPages
                                SET StrokeData = $strokes, DateText = $date, ShiftText = $shift, WeatherText = $weather, MoodText = $mood, PaperPattern = $pattern
                                WHERE NotebookId = $nbId AND PageIndex = $pIndex;";
                        }
                        else
                        {
                            cmd.CommandText = @"
                                INSERT INTO NotebookPages (NotebookId, PageIndex, StrokeData, DateText, ShiftText, WeatherText, MoodText, PaperPattern)
                                VALUES ($nbId, $pIndex, $strokes, $date, $shift, $weather, $mood, $pattern);";
                        }
                        cmd.Parameters.AddWithValue("$nbId", notebookId);
                        cmd.Parameters.AddWithValue("$pIndex", pageIndex);
                        cmd.Parameters.AddWithValue("$strokes", strokeBytes ?? Array.Empty<byte>());
                        cmd.Parameters.AddWithValue("$date", date ?? "");
                        cmd.Parameters.AddWithValue("$shift", shift ?? "");
                        cmd.Parameters.AddWithValue("$weather", weather ?? "");
                        cmd.Parameters.AddWithValue("$mood", mood ?? "");
                        cmd.Parameters.AddWithValue("$pattern", pattern ?? "Blank");
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxShowHandler($"Lỗi lưu trang: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeletePage(string notebookId, int pageIndex)
        {
            var dbPath = QASmartClass.Services.AppPaths.DatabaseFile;
            try
            {
                using (var connection = GetConnection())
                {
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            using (var cmd = connection.CreateCommand())
                            {
                                cmd.Transaction = transaction;
                                cmd.CommandText = "DELETE FROM NotebookPages WHERE NotebookId = $nbId AND PageIndex = $pIndex";
                                cmd.Parameters.AddWithValue("$nbId", notebookId);
                                cmd.Parameters.AddWithValue("$pIndex", pageIndex);
                                cmd.ExecuteNonQuery();
                            }

                            using (var cmd = connection.CreateCommand())
                            {
                                cmd.Transaction = transaction;
                                cmd.CommandText = "UPDATE NotebookPages SET PageIndex = PageIndex - 1 WHERE NotebookId = $nbId AND PageIndex > $pIndex";
                                cmd.Parameters.AddWithValue("$nbId", notebookId);
                                cmd.Parameters.AddWithValue("$pIndex", pageIndex);
                                cmd.ExecuteNonQuery();
                            }

                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxShowHandler($"Lỗi xóa trang CSDL: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Ink serialization
        private byte[] StrokesToBytes(StrokeCollection strokes)
        {
            using (var ms = new MemoryStream())
            {
                strokes.Save(ms);
                return ms.ToArray();
            }
        }

        private StrokeCollection BytesToStrokes(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return new StrokeCollection();
            try
            {
                using (var ms = new MemoryStream(bytes))
                {
                    return new StrokeCollection(ms);
                }
            }
            catch
            {
                return new StrokeCollection();
            }
        }

        private void SetStrokes(StrokeCollection strokes)
        {
            if (DrawingCanvas.Strokes != null)
            {
                DrawingCanvas.Strokes.StrokesChanged -= Strokes_StrokesChanged;
            }
            
            DrawingCanvas.Strokes = strokes;
            
            if (DrawingCanvas.Strokes != null)
            {
                DrawingCanvas.Strokes.StrokesChanged += Strokes_StrokesChanged;
            }
        }

        private void Strokes_StrokesChanged(object sender, StrokeCollectionChangedEventArgs e)
        {
            SaveUndoState();
        }

        // Undo/Redo Engine
        private void SaveUndoState()
        {
            if (_isUndoRedoing) return;
            var currentState = CaptureCurrentState();
            _undoStack.Push(currentState);
            _redoStack.Clear();
            UpdateUndoRedoButtons();
        }

        private void ResetUndoRedo(StrokeCollection initialStrokes, List<CanvasElementDto> initialElements)
        {
            _undoStack.Clear();
            _redoStack.Clear();
            
            _isUndoRedoing = true;
            _undoStack.Push(new CanvasState { Strokes = initialStrokes.Clone(), Elements = CloneDtos(initialElements) });
            _isUndoRedoing = false;
            
            UpdateUndoRedoButtons();
        }

        private void PerformUndo()
        {
            if (_undoStack.Count > 1)
            {
                _isUndoRedoing = true;
                
                var currentState = _undoStack.Pop();
                _redoStack.Push(currentState);
                
                var prevState = _undoStack.Peek();
                RestoreCanvasState(prevState);
                
                _isUndoRedoing = false;
                UpdateUndoRedoButtons();
            }
        }

        private void PerformRedo()
        {
            if (_redoStack.Count > 0)
            {
                _isUndoRedoing = true;
                
                var nextState = _redoStack.Pop();
                _undoStack.Push(nextState);
                
                RestoreCanvasState(nextState);
                
                _isUndoRedoing = false;
                UpdateUndoRedoButtons();
            }
        }

        private void UpdateUndoRedoButtons()
        {
            if (btnUndo != null) btnUndo.IsEnabled = (_undoStack.Count > 1);
            if (btnRedo != null) btnRedo.IsEnabled = (_redoStack.Count > 0);
        }

        private void Notebook_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is string id)
            {
                var nb = _notebooks.FirstOrDefault(n => n.Id == id);
                if (nb != null)
                {
                    OpenNotebook(nb);
                }
            }
        }

        private void OpenNotebook(NotebookItem nb)
        {
            SaveCurrentNotebook();

            _currentNotebook = nb;
            
            cboTheme.SelectedIndex = nb.ThemeIndex;
            
            foreach (ComboBoxItem item in cboPaperSize.Items)
            {
                if ((string)item.Tag == nb.PaperSizeTag)
                {
                    cboPaperSize.SelectedItem = item;
                    break;
                }
            }

            // Load without saving the previous drawing on canvas to the new notebook
            LoadPage(nb.CurrentPageIndex, saveCurrentPageFirst: false);

            LibraryScreen.Visibility = Visibility.Collapsed;
            EditorScreen.Visibility = Visibility.Visible;
            ApplyTheme(nb.ThemeIndex);
        }

        private void SaveCurrentNotebook()
        {
            if (_currentNotebook != null)
            {
                SaveActivePageData();
                
                _currentNotebook.ThemeIndex = cboTheme.SelectedIndex;
                if (cboPaperSize.SelectedItem is ComboBoxItem cbi && cbi.Tag is string tg)
                    _currentNotebook.PaperSizeTag = tg;

                SaveNotebookMetadata(_currentNotebook);
            }
        }

        private void SaveActivePageData()
        {
            if (_currentNotebook == null) return;
            
            int pageIndex = _currentNotebook.CurrentPageIndex;
            byte[] strokeBytes = SerializePageData(DrawingCanvas.Strokes, DrawingCanvas.Children);
            string date = txtPaperDate.Text;
            string shift = (cboPaperShift.SelectedItem is ComboBoxItem item) ? item.Content.ToString() : "Sáng 🌅";
            string weather = GetSelectedWeather();
            string mood = GetSelectedMood();
            string pattern = (cboPaperPattern?.SelectedItem is ComboBoxItem pItem) ? pItem.Tag?.ToString() ?? "Blank" : "Blank";
            
            SavePageData(_currentNotebook.Id, pageIndex, strokeBytes, date, shift, weather, mood, pattern);
        }

        private void LoadPage(int index, bool saveCurrentPageFirst = true)
        {
            if (_currentNotebook == null) return;

            if (saveCurrentPageFirst)
            {
                SaveActivePageData();
            }

            // Reset drawing tool mode to Ink/Viết on page load
            _currentMode = "Ink";
            UpdateToolbarButtonsHighlight();
            if (DrawingCanvas != null)
            {
                DrawingCanvas.EditingMode = InkCanvasEditingMode.Ink;
                _customCursor = System.Windows.Input.Cursors.Pen;
                DrawingCanvas.Cursor = _customCursor;
            }

            _currentNotebook.CurrentPageIndex = index;
            SaveNotebookMetadata(_currentNotebook);

            var dbPath = QASmartClass.Services.AppPaths.DatabaseFile;
            byte[] strokeBytes = null;
            string date = DateTime.Now.ToString("dd/MM/yyyy");
            string shift = "Sáng 🌅";
            string weather = "";
            string mood = "";
            string paperPattern = "Blank";

            try
            {
                using (var connection = GetConnection())
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT StrokeData, DateText, ShiftText, WeatherText, MoodText, PaperPattern FROM NotebookPages WHERE NotebookId = $nbId AND PageIndex = $pIndex";
                        cmd.Parameters.AddWithValue("$nbId", _currentNotebook.Id);
                        cmd.Parameters.AddWithValue("$pIndex", index);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                strokeBytes = reader.IsDBNull(0) ? null : (byte[])reader.GetValue(0);
                                date = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                shift = reader.IsDBNull(2) ? "Sáng 🌅" : reader.GetString(2);
                                weather = reader.IsDBNull(3) ? "" : reader.GetString(3);
                                mood = reader.IsDBNull(4) ? "" : reader.GetString(4);
                                paperPattern = reader.IsDBNull(5) ? "Blank" : reader.GetString(5);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxShowHandler($"Lỗi tải trang: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            var data = DeserializePageData(strokeBytes);
            SetStrokes(data.Strokes);
            RecreateChildrenFromDtos(data.Elements);

            txtPaperDate.Text = date;

            cboPaperShift.SelectedIndex = 0;
            for (int i = 0; i < cboPaperShift.Items.Count; i++)
            {
                if (cboPaperShift.Items[i] is ComboBoxItem item && item.Content.ToString() == shift)
                {
                    cboPaperShift.SelectedIndex = i;
                    break;
                }
            }

            SetSelectedWeather(weather);
            SetSelectedMood(mood);

            if (cboPaperPattern != null)
            {
                _isSettingPattern = true;
                cboPaperPattern.SelectedIndex = 0;
                for (int i = 0; i < cboPaperPattern.Items.Count; i++)
                {
                    if (cboPaperPattern.Items[i] is ComboBoxItem item && item.Tag?.ToString() == paperPattern)
                    {
                        cboPaperPattern.SelectedIndex = i;
                        break;
                    }
                }
                _isSettingPattern = false;
            }
            ApplyPaperPattern(paperPattern);

            ResetUndoRedo(data.Strokes, data.Elements);
            UpdatePageNavigationInfo();
        }

        private void UpdatePageNavigationInfo()
        {
            if (_currentNotebook == null) return;
            
            int pageCount = GetPageCount(_currentNotebook.Id);
            if (pageCount == 0) pageCount = 1;
            
            int curIndex = _currentNotebook.CurrentPageIndex;
            lblPageInfo.Text = $"Trang {curIndex + 1} / {pageCount}";
            
            btnPrevPage.IsEnabled = (curIndex > 0);
            btnNextPage.IsEnabled = (curIndex < pageCount - 1);
            btnDeletePage.IsEnabled = (pageCount > 1);
        }

        private void BtnPrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentNotebook != null && _currentNotebook.CurrentPageIndex > 0)
            {
                LoadPage(_currentNotebook.CurrentPageIndex - 1, saveCurrentPageFirst: true);
            }
        }

        private void BtnNextPage_Click(object sender, RoutedEventArgs e)
        {
            int pageCount = GetPageCount(_currentNotebook.Id);
            if (_currentNotebook != null && _currentNotebook.CurrentPageIndex < pageCount - 1)
            {
                LoadPage(_currentNotebook.CurrentPageIndex + 1, saveCurrentPageFirst: true);
            }
        }

        private void BtnAddPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentNotebook == null) return;

            SaveActivePageData();
            
            int newPageIndex = GetPageCount(_currentNotebook.Id);
            SavePageData(_currentNotebook.Id, newPageIndex, Array.Empty<byte>(), DateTime.Now.ToString("dd/MM/yyyy"), "Sáng 🌅", "", "");
            
            LoadPage(newPageIndex, saveCurrentPageFirst: false);
        }

        private void BtnDeletePage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentNotebook == null) return;

            int pageCount = GetPageCount(_currentNotebook.Id);
            if (pageCount <= 1)
            {
                MessageBoxShowHandler("Không thể xóa trang duy nhất còn lại.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBoxShowHandler("Bạn có chắc chắn muốn xóa trang này? Toàn bộ nét vẽ của trang sẽ bị xóa vĩnh viễn.", "Xóa trang", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                int curIndex = _currentNotebook.CurrentPageIndex;
                DeletePage(_currentNotebook.Id, curIndex);
                
                int targetIndex = curIndex;
                if (targetIndex >= pageCount - 1)
                {
                    targetIndex = pageCount - 2;
                }

                _currentNotebook.CurrentPageIndex = targetIndex;
                SaveNotebookMetadata(_currentNotebook);
                
                LoadPage(targetIndex, saveCurrentPageFirst: false);
            }
        }

        private void BtnBackToLibrary_Click(object sender, RoutedEventArgs e)
        {
            SaveCurrentNotebook();
            _currentNotebook = null;
            
            EditorScreen.Visibility = Visibility.Collapsed;
            LibraryScreen.Visibility = Visibility.Visible;
            
            // Reload database items to update library cover titles/sizes
            LoadNotebooksFromDatabase();
        }

        private void BtnNewNotebook_Click(object sender, RoutedEventArgs e)
        {
            _editingNotebook = new NotebookItem();
            _editingNotebook.Title = "Sổ mới " + (_notebooks.Count + 1);
            
            txtNbTitle.Text = _editingNotebook.Title;
            _tempCoverColor = Color.FromRgb(33, 150, 243);
            _tempPaperColor = Colors.White;
            cboCoverType.SelectedIndex = 0;
            
            HighlightSelectedButton(CoverColorPanel, "#1976D2");
            HighlightSelectedButton(PaperColorPanel, "#FFFFFF");
            
            DialogOverlay.Visibility = Visibility.Visible;
        }

        private void HighlightSelectedButton(WrapPanel panel, string tag)
        {
            foreach (var child in panel.Children)
            {
                if (child is Button btn)
                {
                    if (btn.Tag.ToString().Equals(tag, StringComparison.OrdinalIgnoreCase))
                    {
                        btn.BorderBrush = new SolidColorBrush(Color.FromRgb(33, 33, 33));
                        btn.BorderThickness = new Thickness(3);
                    }
                    else
                    {
                        btn.BorderBrush = Brushes.White;
                        btn.BorderThickness = new Thickness(2);
                    }
                }
            }
        }

        private void BtnDeleteNotebook_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true; // Prevent opening notebook
            if (sender is Button btn && btn.Tag is string id)
            {
                var nb = _notebooks.FirstOrDefault(n => n.Id == id);
                if (nb != null)
                {
                    if (MessageBoxShowHandler($"Bạn có chắc chắn muốn xóa cuốn sổ '{nb.Title}' không? Toàn bộ các trang viết sẽ bị xóa vĩnh viễn.", "Xóa sổ tay", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        DeleteNotebook(nb.Id);
                        _notebooks.Remove(nb);
                    }
                }
            }
        }

        private void DialogCoverColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string hex)
            {
                _tempCoverColor = (Color)ColorConverter.ConvertFromString(hex);
                HighlightSelectedButton(CoverColorPanel, hex);
            }
        }

        private void DialogPaperColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string hex)
            {
                _tempPaperColor = (Color)ColorConverter.ConvertFromString(hex);
                HighlightSelectedButton(PaperColorPanel, hex);
            }
        }

        private void BtnCancelDialog_Click(object sender, RoutedEventArgs e)
        {
            DialogOverlay.Visibility = Visibility.Collapsed;
        }

        private void BtnSaveDialog_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNbTitle.Text))
            {
                MessageBoxShowHandler("Vui lòng nhập tên cuốn sổ!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_editingNotebook != null)
            {
                _editingNotebook.Title = txtNbTitle.Text.Trim();
                _editingNotebook.CoverColor = _tempCoverColor;
                _editingNotebook.PaperColor = _tempPaperColor;
                if (cboCoverType.SelectedItem is ComboBoxItem cbi)
                {
                    _editingNotebook.CoverType = cbi.Tag?.ToString() ?? "Smooth";
                }

                if (!_notebooks.Contains(_editingNotebook))
                {
                    _notebooks.Add(_editingNotebook);
                }
                
                SaveNotebookMetadata(_editingNotebook);
                SavePageData(_editingNotebook.Id, 0, Array.Empty<byte>(), DateTime.Now.ToString("dd/MM/yyyy"), "Sáng 🌅", "", "");
                
                DialogOverlay.Visibility = Visibility.Collapsed;
                OpenNotebook(_editingNotebook);
            }
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            if (sideMenu != null)
            {
                sideMenu.SelectedIndex = 1;
            }
        }

        private void BtnCloseHelp_Click(object sender, RoutedEventArgs e)
        {
            if (sideMenu != null)
            {
                sideMenu.SelectedIndex = 1;
            }
        }

        private void CboTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboTheme != null)
            {
                ApplyTheme(cboTheme.SelectedIndex);
            }
        }

        private void CboPaperSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboPaperSize != null && NotebookBorder != null)
            {
                if (cboPaperSize.SelectedItem is ComboBoxItem item && item.Tag is string sizeStr)
                {
                    var parts = sizeStr.Split('x');
                    if (parts.Length == 2 && ParsingHelper.TryParseDouble(parts[0], out double w) && ParsingHelper.TryParseDouble(parts[1], out double h))
                    {
                        NotebookBorder.Width = w;
                        NotebookBorder.Height = h;
                    }
                }
            }
        }

        private void CboPaperPattern_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isSettingPattern) return;
            if (cboPaperPattern?.SelectedItem is ComboBoxItem item && item.Tag is string pattern)
            {
                ApplyPaperPattern(pattern);
                SaveActivePageData();
            }
        }

        private void ApplyPaperPattern(string pattern)
        {
            if (WritingAreaBackground == null) return;

            DrawingBrush bgBrush = new DrawingBrush();
            bgBrush.ViewportUnits = BrushMappingMode.Absolute;

            switch (pattern)
            {
                case "Lined":
                    bgBrush.Viewport = new Rect(0, 0, 40, 40);
                    bgBrush.TileMode = TileMode.Tile;
                    var linedGroup = new DrawingGroup();
                    linedGroup.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, 40, 40))));
                    linedGroup.Children.Add(new GeometryDrawing(Brushes.Transparent, new Pen(new SolidColorBrush(Color.FromRgb(144, 202, 249)), 1), new LineGeometry(new Point(0, 39), new Point(40, 39))));
                    bgBrush.Drawing = linedGroup;
                    WritingAreaBackground.Background = bgBrush;
                    break;

                case "Grid":
                    bgBrush.Viewport = new Rect(0, 0, 30, 30);
                    bgBrush.TileMode = TileMode.Tile;
                    var gridGroup = new DrawingGroup();
                    gridGroup.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, 30, 30))));
                    gridGroup.Children.Add(new GeometryDrawing(Brushes.Transparent, new Pen(new SolidColorBrush(Color.FromRgb(224, 224, 224)), 1), new LineGeometry(new Point(0, 0), new Point(30, 0))));
                    gridGroup.Children.Add(new GeometryDrawing(Brushes.Transparent, new Pen(new SolidColorBrush(Color.FromRgb(224, 224, 224)), 1), new LineGeometry(new Point(0, 0), new Point(0, 30))));
                    bgBrush.Drawing = gridGroup;
                    WritingAreaBackground.Background = bgBrush;
                    break;

                case "Dot":
                    bgBrush.Viewport = new Rect(0, 0, 20, 20);
                    bgBrush.TileMode = TileMode.Tile;
                    var dotGroup = new DrawingGroup();
                    dotGroup.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, 20, 20))));
                    dotGroup.Children.Add(new GeometryDrawing(new SolidColorBrush(Color.FromRgb(189, 189, 189)), null, new EllipseGeometry(new Point(10, 10), 1.5, 1.5)));
                    bgBrush.Drawing = dotGroup;
                    WritingAreaBackground.Background = bgBrush;
                    break;

                case "Isometric":
                    bgBrush.Viewport = new Rect(0, 0, 34.64, 20);
                    bgBrush.TileMode = TileMode.Tile;
                    var isoGroup = new DrawingGroup();
                    isoGroup.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, 34.64, 20))));
                    var pen = new Pen(new SolidColorBrush(Color.FromRgb(220, 220, 220)), 1);
                    isoGroup.Children.Add(new GeometryDrawing(Brushes.Transparent, pen, new LineGeometry(new Point(0, 0), new Point(34.64, 20))));
                    isoGroup.Children.Add(new GeometryDrawing(Brushes.Transparent, pen, new LineGeometry(new Point(0, 20), new Point(34.64, 0))));
                    isoGroup.Children.Add(new GeometryDrawing(Brushes.Transparent, pen, new LineGeometry(new Point(17.32, 0), new Point(17.32, 20))));
                    bgBrush.Drawing = isoGroup;
                    WritingAreaBackground.Background = bgBrush;
                    break;

                case "Blank":
                default:
                    WritingAreaBackground.Background = Brushes.Transparent;
                    break;
            }
        }

        private void PenColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string hexCode)
            {
                var color = (Color)ColorConverter.ConvertFromString(hexCode);
                DrawingCanvas.DefaultDrawingAttributes.Color = color;
                HighlightSelectedPenColor(hexCode);
                
                _currentMode = "Ink";
                DrawingCanvas.EditingMode = InkCanvasEditingMode.Ink;
                _customCursor = System.Windows.Input.Cursors.Pen;
                DrawingCanvas.Cursor = _customCursor;
                UpdateToolbarButtonsHighlight();
            }
        }

        private void HighlightSelectedPenColor(string hex)
        {
            if (PenColorPanel == null) return;
            foreach (var child in PenColorPanel.Children)
            {
                if (child is Button btn)
                {
                    if (btn.Tag.ToString().Equals(hex, StringComparison.OrdinalIgnoreCase))
                    {
                        btn.BorderBrush = new SolidColorBrush(Color.FromRgb(251, 192, 45)); // Gold border
                        btn.BorderThickness = new Thickness(3);
                    }
                    else
                    {
                        btn.BorderBrush = Brushes.White;
                        btn.BorderThickness = new Thickness(2);
                    }
                }
            }
        }

        private void Mode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string mode)
            {
                _currentMode = mode;
                UpdateToolbarButtonsHighlight();

                // Configure InkCanvas EditingMode
                if (_currentMode == "Ink")
                {
                    DrawingCanvas.EditingMode = InkCanvasEditingMode.Ink;
                    _customCursor = System.Windows.Input.Cursors.Pen;
                }
                else if (_currentMode == "Eraser")
                {
                    DrawingCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
                    _customCursor = System.Windows.Input.Cursors.Arrow;
                }
                else if (_currentMode == "Select")
                {
                    DrawingCanvas.EditingMode = InkCanvasEditingMode.Select;
                    _customCursor = System.Windows.Input.Cursors.Arrow;
                }
                else
                {
                    DrawingCanvas.EditingMode = InkCanvasEditingMode.None;
                    if (_currentMode == "Hand")
                        _customCursor = System.Windows.Input.Cursors.Hand;
                    else if (_currentMode == "Text")
                        _customCursor = System.Windows.Input.Cursors.IBeam;
                    else
                        _customCursor = System.Windows.Input.Cursors.Cross;
                }

                DrawingCanvas.Cursor = _customCursor;
            }
        }

        private void UpdateToolbarButtonsHighlight()
        {
            var buttons = new Dictionary<string, Button?>
            {
                { "Ink", btnPen },
                { "Rectangle", btnRect },
                { "Ellipse", btnEllipse },
                { "Line", btnLine },
                { "Arrow", btnArrow },
                { "Text", btnText },
                { "Select", btnSelect },
                { "Hand", btnHand },
                { "Eraser", btnEraser }
            };

            foreach (var kvp in buttons)
            {
                if (kvp.Value == null) continue;
                if (kvp.Key == _currentMode)
                {
                    if (kvp.Key == "Eraser")
                    {
                        kvp.Value.Background = new SolidColorBrush(Color.FromRgb(255, 235, 238));
                        kvp.Value.BorderBrush = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                    }
                    else
                    {
                        kvp.Value.Background = new SolidColorBrush(Color.FromRgb(227, 242, 253));
                        kvp.Value.BorderBrush = new SolidColorBrush(Color.FromRgb(33, 150, 243));
                    }
                }
                else
                {
                    kvp.Value.Background = Brushes.White;
                    kvp.Value.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                }
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBoxShowHandler("Bạn có chắc chắn muốn xóa toàn bộ trang viết?", "Xóa trang", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                DrawingCanvas.Strokes.Clear();
                DrawingCanvas.Children.Clear();
                SaveUndoState();
            }
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            PerformUndo();
        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            PerformRedo();
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (NotebookBorder.ActualWidth == 0 || NotebookBorder.ActualHeight == 0)
                {
                    var dummyBitmap = new System.Windows.Media.Imaging.WriteableBitmap(1, 1, 96, 96, System.Windows.Media.PixelFormats.Pbgra32, null);
                    var dummyEncoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    dummyEncoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(dummyBitmap));
                    
                    string prefix = "HocSinh";


                    string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string file = $"{prefix}_Notebook_{_currentNotebook?.Title ?? "Notes"}_Page_{(_currentNotebook?.CurrentPageIndex ?? 0) + 1}_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
                    string path = Path.Combine(desktop, file);

                    using (var fs = File.OpenWrite(path))
                    {
                        dummyEncoder.Save(fs);
                    }

                    MessageBoxShowHandler($"Đã xuất trang ghi chú thành công thành file ảnh PNG:\n{path}", "Xuất ảnh thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                double targetWidth = 1600;
                double ratio = 848.0 / 1200.0; // standard A4 horizontal ratio
                if (NotebookBorder.ActualHeight > NotebookBorder.ActualWidth || NotebookBorder.Height > NotebookBorder.Width)
                {
                    ratio = 1200.0 / 848.0; // vertical ratio
                }
                double targetHeight = targetWidth * ratio;

                var drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    var visualBrush = new VisualBrush(NotebookBorder) { Stretch = Stretch.Fill };
                    drawingContext.DrawRectangle(visualBrush, null, new Rect(0, 0, targetWidth, targetHeight));
                }

                var renderBitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    (int)targetWidth, (int)targetHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);

                renderBitmap.Render(drawingVisual);

                var pngEncoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                pngEncoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(renderBitmap));

                string studentPrefix = "HocSinh";


                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string fileName = $"{studentPrefix}_Notebook_{_currentNotebook?.Title ?? "Notes"}_Page_{(_currentNotebook?.CurrentPageIndex ?? 0) + 1}_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
                string fullPath = Path.Combine(desktopPath, fileName);

                using (var fs = File.OpenWrite(fullPath))
                {
                    pngEncoder.Save(fs);
                }

                MessageBoxShowHandler($"Đã xuất trang ghi chú thành công thành file ảnh PNG:\n{fullPath}", "Xuất ảnh thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBoxShowHandler($"Lỗi khi xuất ảnh: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyTheme(int themeIndex)
        {
            if (WritingAreaBackground == null || NotebookBorder == null || NotebookBackgroundContainer == null) return;

            DrawingBrush bgBrush = new DrawingBrush();
            bgBrush.ViewportUnits = BrushMappingMode.Absolute;
            
            ColBinder.Width = new GridLength(60);
            ColMargin.Width = new GridLength(2);
            BinderArea.Visibility = Visibility.Visible;
            MarginLine.Visibility = Visibility.Visible;
            
            SolidColorBrush basePaperBrush = Brushes.White;
            if (_currentNotebook != null)
            {
                basePaperBrush = new SolidColorBrush(_currentNotebook.PaperColor);
            }
            NotebookBackgroundContainer.Background = basePaperBrush;
            WritingAreaBackground.Background = Brushes.Transparent;
            NotebookBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            NotebookBorder.CornerRadius = new CornerRadius(12, 20, 20, 12);
            MarginLine.Background = new SolidColorBrush(Color.FromRgb(255, 205, 210));

            // Reset header element colors to default dark theme
            var defaultDarkBrush = new SolidColorBrush(Color.FromRgb(66, 66, 66));
            if (lblPaperDate != null) lblPaperDate.Foreground = defaultDarkBrush;
            if (lblPaperShift != null) lblPaperShift.Foreground = defaultDarkBrush;
            if (lblPaperPattern != null) lblPaperPattern.Foreground = defaultDarkBrush;
            if (lblWeather != null) lblWeather.Foreground = defaultDarkBrush;
            if (lblMood != null) lblMood.Foreground = defaultDarkBrush;
            if (txtPaperDate != null)
            {
                txtPaperDate.Foreground = defaultDarkBrush;
                txtPaperDate.BorderBrush = new SolidColorBrush(Color.FromRgb(158, 158, 158));
            }
            if (cboPaperShift != null) cboPaperShift.Foreground = defaultDarkBrush;
            if (cboPaperPattern != null) cboPaperPattern.Foreground = defaultDarkBrush;

            switch (themeIndex)
            {
                case 0: // Lined (Dòng kẻ ngang)
                    bgBrush.Viewport = new Rect(0, 0, 40, 40);
                    bgBrush.TileMode = TileMode.Tile;
                    var linedGroup = new DrawingGroup();
                    linedGroup.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, 40, 40))));
                    linedGroup.Children.Add(new GeometryDrawing(Brushes.Transparent, new Pen(new SolidColorBrush(Color.FromRgb(144, 202, 249)), 1), new LineGeometry(new Point(0, 39), new Point(40, 39))));
                    bgBrush.Drawing = linedGroup;
                    WritingAreaBackground.Background = bgBrush;
                    break;
                    
                case 1: // Grid (Ô vuông)
                    bgBrush.Viewport = new Rect(0, 0, 30, 30);
                    bgBrush.TileMode = TileMode.Tile;
                    var gridGroup = new DrawingGroup();
                    gridGroup.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, 30, 30))));
                    gridGroup.Children.Add(new GeometryDrawing(Brushes.Transparent, new Pen(new SolidColorBrush(Color.FromRgb(224, 224, 224)), 1), new LineGeometry(new Point(0, 0), new Point(30, 0))));
                    gridGroup.Children.Add(new GeometryDrawing(Brushes.Transparent, new Pen(new SolidColorBrush(Color.FromRgb(224, 224, 224)), 1), new LineGeometry(new Point(0, 0), new Point(0, 30))));
                    bgBrush.Drawing = gridGroup;
                    WritingAreaBackground.Background = bgBrush;
                    break;

                case 2: // Dotted (Chấm bi)
                    bgBrush.Viewport = new Rect(0, 0, 20, 20);
                    bgBrush.TileMode = TileMode.Tile;
                    var dotGroup = new DrawingGroup();
                    dotGroup.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, 20, 20))));
                    dotGroup.Children.Add(new GeometryDrawing(new SolidColorBrush(Color.FromRgb(189, 189, 189)), null, new EllipseGeometry(new Point(10, 10), 1.5, 1.5)));
                    bgBrush.Drawing = dotGroup;
                    WritingAreaBackground.Background = bgBrush;
                    break;

                case 3: // Cute (Dễ thương)
                    LinearGradientBrush cuteBrush = new LinearGradientBrush();
                    cuteBrush.StartPoint = new Point(0, 0);
                    cuteBrush.EndPoint = new Point(1, 1);
                    if (_currentNotebook != null && _currentNotebook.PaperColor != Colors.White) {
                        cuteBrush.GradientStops.Add(new GradientStop(_currentNotebook.PaperColor, 0.0));
                        cuteBrush.GradientStops.Add(new GradientStop(Color.FromRgb(252, 228, 236), 1.0));
                    } else {
                        cuteBrush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 235, 238), 0.0));
                        cuteBrush.GradientStops.Add(new GradientStop(Color.FromRgb(252, 228, 236), 1.0));
                    }
                    NotebookBackgroundContainer.Background = cuteBrush;
                    WritingAreaBackground.Background = Brushes.Transparent;
                    NotebookBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(244, 143, 177));
                    MarginLine.Background = new SolidColorBrush(Color.FromRgb(248, 187, 208));
                    break;

                case 4: // Elegant (Thanh lịch)
                    bgBrush.Viewport = new Rect(0, 0, 40, 40);
                    bgBrush.TileMode = TileMode.Tile;
                    var elegantGroup = new DrawingGroup();
                    var elBg = (_currentNotebook != null && _currentNotebook.PaperColor != Colors.White) ? _currentNotebook.PaperColor : Color.FromRgb(253, 245, 230);
                    NotebookBackgroundContainer.Background = new SolidColorBrush(elBg);
                    elegantGroup.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, 40, 40)))); 
                    elegantGroup.Children.Add(new GeometryDrawing(Brushes.Transparent, new Pen(new SolidColorBrush(Color.FromArgb(30, 139, 69, 19)), 1), new LineGeometry(new Point(0, 39), new Point(40, 39))));
                    bgBrush.Drawing = elegantGroup;
                    WritingAreaBackground.Background = bgBrush;
                    NotebookBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(218, 165, 32)); 
                    MarginLine.Background = new SolidColorBrush(Color.FromArgb(50, 139, 69, 19));
                    break;

                case 5: // Dark Mode (Bảng đen)
                    ColBinder.Width = new GridLength(0);
                    ColMargin.Width = new GridLength(0);
                    BinderArea.Visibility = Visibility.Collapsed;
                    MarginLine.Visibility = Visibility.Collapsed;
                    
                    bgBrush.Viewport = new Rect(0, 0, 40, 40);
                    bgBrush.TileMode = TileMode.Tile;
                    var darkGroup = new DrawingGroup();
                    var dkBg = (_currentNotebook != null && _currentNotebook.PaperColor != Colors.White) ? _currentNotebook.PaperColor : Color.FromRgb(33, 33, 33);
                    NotebookBackgroundContainer.Background = new SolidColorBrush(dkBg);
                    darkGroup.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, 40, 40)))); 
                    darkGroup.Children.Add(new GeometryDrawing(Brushes.Transparent, new Pen(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), 1), new LineGeometry(new Point(0, 39), new Point(40, 39))));
                    darkGroup.Children.Add(new GeometryDrawing(Brushes.Transparent, new Pen(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), 1), new LineGeometry(new Point(0, 0), new Point(0, 39))));
                    bgBrush.Drawing = darkGroup;
                    WritingAreaBackground.Background = bgBrush;
                    NotebookBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(66, 66, 66));
                    NotebookBorder.CornerRadius = new CornerRadius(8);

                    // Set header elements to light color in dark mode
                    var lightBrush = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                    if (lblPaperDate != null) lblPaperDate.Foreground = lightBrush;
                    if (lblPaperShift != null) lblPaperShift.Foreground = lightBrush;
                    if (lblPaperPattern != null) lblPaperPattern.Foreground = lightBrush;
                    if (lblWeather != null) lblWeather.Foreground = lightBrush;
                    if (lblMood != null) lblMood.Foreground = lightBrush;
                    if (txtPaperDate != null)
                    {
                        txtPaperDate.Foreground = lightBrush;
                        txtPaperDate.BorderBrush = new SolidColorBrush(Color.FromRgb(120, 120, 120));
                    }
                    if (cboPaperShift != null) cboPaperShift.Foreground = lightBrush;
                    if (cboPaperPattern != null) cboPaperPattern.Foreground = lightBrush;
                    
                    // Smart pen color selection
                    var currentPenColor = DrawingCanvas.DefaultDrawingAttributes.Color;
                    if (currentPenColor == Color.FromRgb(33, 33, 33) || currentPenColor == Color.FromRgb(25, 118, 210) || currentPenColor == Color.FromRgb(33, 150, 243) || currentPenColor == Colors.Black)
                    {
                        DrawingCanvas.DefaultDrawingAttributes.Color = Color.FromRgb(250, 250, 250); // White
                        HighlightSelectedPenColor("#FAFAFA");
                    }
                    break;

                case 6: // Blank (Trơn)
                    WritingAreaBackground.Background = Brushes.Transparent;
                    break;
            }

            if (themeIndex != 5)
            {
                if (DrawingCanvas.DefaultDrawingAttributes.Color == Color.FromRgb(250, 250, 250) || DrawingCanvas.DefaultDrawingAttributes.Color == Colors.White)
                {
                    DrawingCanvas.DefaultDrawingAttributes.Color = Color.FromRgb(33, 33, 33);
                    HighlightSelectedPenColor("#212121");
                }
            }
        }

        private string GetSelectedWeather()
        {
            if (radSunny.IsChecked == true) return "Sunny";
            if (radCloudy.IsChecked == true) return "Cloudy";
            if (radRainy.IsChecked == true) return "Rainy";
            if (radSnowy.IsChecked == true) return "Snowy";
            return "";
        }

        private void SetSelectedWeather(string weather)
        {
            radSunny.IsChecked = (weather == "Sunny");
            radCloudy.IsChecked = (weather == "Cloudy");
            radRainy.IsChecked = (weather == "Rainy");
            radSnowy.IsChecked = (weather == "Snowy");
        }

        private string GetSelectedMood()
        {
            if (radMoodLove.IsChecked == true) return "Love";
            if (radMoodHappy.IsChecked == true) return "Happy";
            if (radMoodNeutral.IsChecked == true) return "Neutral";
            if (radMoodSad.IsChecked == true) return "Sad";
            if (radMoodAngry.IsChecked == true) return "Angry";
            return "";
        }

        private void SetSelectedMood(string mood)
        {
            radMoodLove.IsChecked = (mood == "Love");
            radMoodHappy.IsChecked = (mood == "Happy");
            radMoodNeutral.IsChecked = (mood == "Neutral");
            radMoodSad.IsChecked = (mood == "Sad");
            radMoodAngry.IsChecked = (mood == "Angry");
        }

        private void DrawingCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;

            Point pos = e.GetPosition(DrawingCanvas);

            if (_currentMode == "Rectangle" || _currentMode == "Ellipse" || _currentMode == "Line" || _currentMode == "Arrow")
            {
                _startPoint = pos;
                _isDrawingShape = true;
                DrawingCanvas.CaptureMouse();
                
                var color = DrawingCanvas.DefaultDrawingAttributes.Color;
                var brush = new SolidColorBrush(color);

                if (_currentMode == "Rectangle")
                {
                    var rect = new System.Windows.Shapes.Rectangle
                    {
                        Stroke = brush,
                        StrokeThickness = DrawingCanvas.DefaultDrawingAttributes.Width,
                        Fill = Brushes.Transparent,
                        Width = 0,
                        Height = 0
                    };
                    InkCanvas.SetLeft(rect, pos.X);
                    InkCanvas.SetTop(rect, pos.Y);
                    DrawingCanvas.Children.Add(rect);
                    _previewShape = rect;
                }
                else if (_currentMode == "Ellipse")
                {
                    var ell = new System.Windows.Shapes.Ellipse
                    {
                        Stroke = brush,
                        StrokeThickness = DrawingCanvas.DefaultDrawingAttributes.Width,
                        Fill = Brushes.Transparent,
                        Width = 0,
                        Height = 0
                    };
                    InkCanvas.SetLeft(ell, pos.X);
                    InkCanvas.SetTop(ell, pos.Y);
                    DrawingCanvas.Children.Add(ell);
                    _previewShape = ell;
                }
                else if (_currentMode == "Line")
                {
                    var line = new System.Windows.Shapes.Line
                    {
                        Stroke = brush,
                        StrokeThickness = DrawingCanvas.DefaultDrawingAttributes.Width,
                        X1 = pos.X,
                        Y1 = pos.Y,
                        X2 = pos.X,
                        Y2 = pos.Y
                    };
                    InkCanvas.SetLeft(line, 0);
                    InkCanvas.SetTop(line, 0);
                    DrawingCanvas.Children.Add(line);
                    _previewShape = line;
                }
                else if (_currentMode == "Arrow")
                {
                    var path = new System.Windows.Shapes.Path
                    {
                        Stroke = brush,
                        StrokeThickness = DrawingCanvas.DefaultDrawingAttributes.Width,
                        Fill = brush
                    };
                    InkCanvas.SetLeft(path, 0);
                    InkCanvas.SetTop(path, 0);
                    DrawingCanvas.Children.Add(path);
                    _previewShape = path;
                }
            }
            else if (_currentMode == "Text")
            {
                var color = DrawingCanvas.DefaultDrawingAttributes.Color;
                var brush = new SolidColorBrush(color);

                var tb = new TextBox
                {
                    Width = 200,
                    Height = 80,
                    FontSize = 16,
                    Foreground = brush,
                    Background = NotebookBackgroundContainer.Background ?? Brushes.White,
                    BorderBrush = Brushes.LightBlue,
                    BorderThickness = new Thickness(1),
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    Padding = new Thickness(4),
                    VerticalContentAlignment = VerticalAlignment.Top
                };
                InkCanvas.SetLeft(tb, pos.X);
                InkCanvas.SetTop(tb, pos.Y);

                tb.LostFocus += TextBox_LostFocus;
                tb.GotFocus += TextBox_GotFocus;

                DrawingCanvas.Children.Add(tb);
                tb.Focus();
                _selectedElement = tb;
                
                SaveUndoState();
            }
            else if (_currentMode == "Select")
            {
                var clickedElement = FindCanvasChild(e.OriginalSource as DependencyObject);
                if (clickedElement != null)
                {
                    _selectedElement = clickedElement;
                    _isMovingElement = true;
                    _startPoint = pos;
                    
                    if (clickedElement is System.Windows.Shapes.Line line)
                    {
                        clickedElement.Tag = new double[] { line.X1, line.Y1, line.X2, line.Y2 };
                    }
                    else
                    {
                        _elementStartPos = new Point(InkCanvas.GetLeft(clickedElement), InkCanvas.GetTop(clickedElement));
                    }
                    
                    DrawingCanvas.CaptureMouse();
                }
            }
            else if (_currentMode == "Hand")
            {
                _isPanning = true;
                _panStartMousePos = e.GetPosition(this);
                _panStartScrollH = NotebookScroll.HorizontalOffset;
                _panStartScrollV = NotebookScroll.VerticalOffset;
                DrawingCanvas.CaptureMouse();
                DrawingCanvas.Cursor = Cursors.SizeAll;
            }
            else if (_currentMode == "Eraser")
            {
                EraseChildElementsAt(pos);
            }
        }

        private void DrawingCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point pos = e.GetPosition(DrawingCanvas);

            if (_isDrawingShape && _previewShape != null)
            {
                double x = global::System.Math.Min(_startPoint.X, pos.X);
                double y = global::System.Math.Min(_startPoint.Y, pos.Y);
                double w = global::System.Math.Abs(_startPoint.X - pos.X);
                double h = global::System.Math.Abs(_startPoint.Y - pos.Y);

                if (_previewShape is System.Windows.Shapes.Rectangle rect)
                {
                    InkCanvas.SetLeft(rect, x);
                    InkCanvas.SetTop(rect, y);
                    rect.Width = w;
                    rect.Height = h;
                }
                else if (_previewShape is System.Windows.Shapes.Ellipse ell)
                {
                    InkCanvas.SetLeft(ell, x);
                    InkCanvas.SetTop(ell, y);
                    ell.Width = w;
                    ell.Height = h;
                }
                else if (_previewShape is System.Windows.Shapes.Line line)
                {
                    line.X2 = pos.X;
                    line.Y2 = pos.Y;
                }
                else if (_previewShape is System.Windows.Shapes.Path path)
                {
                    path.Data = CreateArrowGeometry(_startPoint, pos);
                }
            }
            else if (_isMovingElement && _selectedElement != null)
            {
                double dx = pos.X - _startPoint.X;
                double dy = pos.Y - _startPoint.Y;

                if (_selectedElement is System.Windows.Shapes.Line line && _selectedElement.Tag is double[] pts)
                {
                    line.X1 = pts[0] + dx;
                    line.Y1 = pts[1] + dy;
                    line.X2 = pts[2] + dx;
                    line.Y2 = pts[3] + dy;
                }
                else if (_selectedElement is System.Windows.Shapes.Path path && _selectedElement.Tag is double[] pathPts)
                {
                    double x1 = pathPts[0] + dx;
                    double y1 = pathPts[1] + dy;
                    double x2 = pathPts[2] + dx;
                    double y2 = pathPts[3] + dy;
                    path.Data = CreateArrowGeometry(new Point(x1, y1), new Point(x2, y2));
                }
                else
                {
                    InkCanvas.SetLeft(_selectedElement, _elementStartPos.X + dx);
                    InkCanvas.SetTop(_selectedElement, _elementStartPos.Y + dy);
                }
            }
            else if (_isPanning)
            {
                Point curMousePos = e.GetPosition(this);
                double dx = curMousePos.X - _panStartMousePos.X;
                double dy = curMousePos.Y - _panStartMousePos.Y;
                NotebookScroll.ScrollToHorizontalOffset(_panStartScrollH - dx);
                NotebookScroll.ScrollToVerticalOffset(_panStartScrollV - dy);
            }
            else if (_currentMode == "Eraser" && e.LeftButton == MouseButtonState.Pressed)
            {
                EraseChildElementsAt(pos);
            }
        }

        private void DrawingCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;

            if (_isDrawingShape)
            {
                _isDrawingShape = false;
                DrawingCanvas.ReleaseMouseCapture();
                if (_previewShape is System.Windows.Shapes.Path path)
                {
                    path.Tag = new double[] { _startPoint.X, _startPoint.Y, e.GetPosition(DrawingCanvas).X, e.GetPosition(DrawingCanvas).Y };
                }
                _previewShape = null;
                SaveUndoState();
            }
            else if (_isMovingElement)
            {
                _isMovingElement = false;
                DrawingCanvas.ReleaseMouseCapture();
                if (_selectedElement is System.Windows.Shapes.Line line)
                {
                    line.Tag = new double[] { line.X1, line.Y1, line.X2, line.Y2 };
                }
                else if (_selectedElement is System.Windows.Shapes.Path path && path.Tag is double[] pathPts)
                {
                    Point posUp = e.GetPosition(DrawingCanvas);
                    double dx = posUp.X - _startPoint.X;
                    double dy = posUp.Y - _startPoint.Y;
                    path.Tag = new double[] { pathPts[0] + dx, pathPts[1] + dy, pathPts[2] + dx, pathPts[3] + dy };
                }
                _selectedElement = null;
                SaveUndoState();
            }
            else if (_isPanning)
            {
                _isPanning = false;
                DrawingCanvas.ReleaseMouseCapture();
                DrawingCanvas.Cursor = _customCursor;
            }
        }

        private FrameworkElement? FindCanvasChild(DependencyObject? obj)
        {
            while (obj != null && obj != DrawingCanvas)
            {
                if (obj is FrameworkElement fe && DrawingCanvas.Children.Contains(fe))
                {
                    return fe;
                }
                obj = VisualTreeHelper.GetParent(obj);
            }
            return null;
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                tb.Background = NotebookBackgroundContainer.Background ?? Brushes.White;
                tb.BorderBrush = Brushes.LightBlue;
                tb.BorderThickness = new Thickness(1);
                tb.Tag = tb.Text; // Save original text
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                tb.Background = Brushes.Transparent;
                tb.BorderThickness = new Thickness(0);
                
                if (string.IsNullOrWhiteSpace(tb.Text))
                {
                    DrawingCanvas.Children.Remove(tb);
                    SaveUndoState();
                }
                else if (tb.Tag == null || tb.Text != tb.Tag.ToString())
                {
                    SaveUndoState();
                }
            }
        }

        private void EraseChildElementsAt(Point pos)
        {
            var itemsToRemove = new List<FrameworkElement>();
            foreach (UIElement child in DrawingCanvas.Children)
            {
                if (child is FrameworkElement fe)
                {
                    double left = InkCanvas.GetLeft(fe);
                    double top = InkCanvas.GetTop(fe);
                    double width = fe.ActualWidth;
                    double height = fe.ActualHeight;

                    if (fe is System.Windows.Shapes.Line line)
                    {
                        left = global::System.Math.Min(line.X1, line.X2);
                        top = global::System.Math.Min(line.Y1, line.Y2);
                        width = global::System.Math.Abs(line.X1 - line.X2);
                        height = global::System.Math.Abs(line.Y1 - line.Y2);
                    }
                    else if (fe is System.Windows.Shapes.Path path && path.Tag is double[] pathPts)
                    {
                        left = global::System.Math.Min(pathPts[0], pathPts[2]);
                        top = global::System.Math.Min(pathPts[1], pathPts[3]);
                        width = global::System.Math.Abs(pathPts[0] - pathPts[2]);
                        height = global::System.Math.Abs(pathPts[1] - pathPts[3]);
                    }

                    var rect = new Rect(left - 10, top - 10, width + 20, height + 20);
                    if (rect.Contains(pos))
                    {
                        itemsToRemove.Add(fe);
                    }
                }
            }

            if (itemsToRemove.Count > 0)
            {
                foreach (var item in itemsToRemove)
                {
                    DrawingCanvas.Children.Remove(item);
                }
                SaveUndoState();
            }
        }

        private byte[] SerializePageData(StrokeCollection strokes, UIElementCollection children)
        {
            byte[] strokeBytes;
            using (var ms = new MemoryStream())
            {
                strokes.Save(ms);
                strokeBytes = ms.ToArray();
            }

            var elements = new List<CanvasElementDto>();
            foreach (UIElement child in children)
            {
                if (child is System.Windows.Shapes.Rectangle rect)
                {
                    var color = (rect.Stroke is SolidColorBrush scb) ? scb.Color.ToString() : "#FF000000";
                    elements.Add(new CanvasElementDto
                    {
                        Type = "Rectangle",
                        X = InkCanvas.GetLeft(rect),
                        Y = InkCanvas.GetTop(rect),
                        Width = rect.Width,
                        Height = rect.Height,
                        ColorHex = color,
                        Thickness = rect.StrokeThickness
                    });
                }
                else if (child is System.Windows.Shapes.Ellipse ell)
                {
                    var color = (ell.Stroke is SolidColorBrush scb) ? scb.Color.ToString() : "#FF000000";
                    elements.Add(new CanvasElementDto
                    {
                        Type = "Ellipse",
                        X = InkCanvas.GetLeft(ell),
                        Y = InkCanvas.GetTop(ell),
                        Width = ell.Width,
                        Height = ell.Height,
                        ColorHex = color,
                        Thickness = ell.StrokeThickness
                    });
                }
                else if (child is System.Windows.Shapes.Line line)
                {
                    var color = (line.Stroke is SolidColorBrush scb) ? scb.Color.ToString() : "#FF000000";
                    elements.Add(new CanvasElementDto
                    {
                        Type = "Line",
                        X1 = line.X1,
                        Y1 = line.Y1,
                        X2 = line.X2,
                        Y2 = line.Y2,
                        ColorHex = color,
                        Thickness = line.StrokeThickness
                    });
                }
                else if (child is System.Windows.Shapes.Path path && path.Tag is double[] pathPts)
                {
                    var color = (path.Stroke is SolidColorBrush scb) ? scb.Color.ToString() : "#FF000000";
                    elements.Add(new CanvasElementDto
                    {
                        Type = "Arrow",
                        X1 = pathPts[0],
                        Y1 = pathPts[1],
                        X2 = pathPts[2],
                        Y2 = pathPts[3],
                        ColorHex = color,
                        Thickness = path.StrokeThickness
                    });
                }
                else if (child is TextBox tb)
                {
                    if (string.IsNullOrWhiteSpace(tb.Text)) continue;
                    
                    var color = (tb.Foreground is SolidColorBrush scb) ? scb.Color.ToString() : "#FF000000";
                    elements.Add(new CanvasElementDto
                    {
                        Type = "Text",
                        X = InkCanvas.GetLeft(tb),
                        Y = InkCanvas.GetTop(tb),
                        Width = double.IsNaN(tb.Width) ? tb.ActualWidth : tb.Width,
                        Height = double.IsNaN(tb.Height) ? tb.ActualHeight : tb.Height,
                        Text = tb.Text,
                        ColorHex = color,
                        FontSize = tb.FontSize
                    });
                }
            }

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(elements);
            byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);

            using (var ms = new MemoryStream())
            {
                using (var writer = new BinaryWriter(ms))
                {
                    writer.Write(new byte[] { 0x53, 0x43, 0x4E, 0x42 }); // Magic
                    writer.Write(1); // Version
                    writer.Write(strokeBytes.Length);
                    writer.Write(strokeBytes);
                    writer.Write(jsonBytes.Length);
                    writer.Write(jsonBytes);
                }
                return ms.ToArray();
            }
        }

        private (StrokeCollection Strokes, List<CanvasElementDto> Elements) DeserializePageData(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 16)
            {
                return (BytesToStrokes(bytes), new List<CanvasElementDto>());
            }

            try
            {
                if (bytes[0] == 0x53 && bytes[1] == 0x43 && bytes[2] == 0x4E && bytes[3] == 0x42)
                {
                    using (var ms = new MemoryStream(bytes))
                    {
                        using (var reader = new BinaryReader(ms))
                        {
                            reader.ReadBytes(4); // Skip magic
                            int version = reader.ReadInt32();
                            int strokeLength = reader.ReadInt32();
                            byte[] strokeBytes = reader.ReadBytes(strokeLength);
                            int jsonLength = reader.ReadInt32();
                            byte[] jsonBytes = reader.ReadBytes(jsonLength);

                            var strokes = BytesToStrokes(strokeBytes);
                            string json = System.Text.Encoding.UTF8.GetString(jsonBytes);
                            var elements = Newtonsoft.Json.JsonConvert.DeserializeObject<List<CanvasElementDto>>(json) ?? new List<CanvasElementDto>();

                            return (strokes, elements);
                        }
                    }
                }
            }
            catch
            {
                // Fallback
            }

            return (BytesToStrokes(bytes), new List<CanvasElementDto>());
        }

        private void RecreateChildrenFromDtos(List<CanvasElementDto> dtos)
        {
            DrawingCanvas.Children.Clear();
            if (dtos == null) return;

            foreach (var dto in dtos)
            {
                var color = (Color)ColorConverter.ConvertFromString(dto.ColorHex);
                var brush = new SolidColorBrush(color);

                if (dto.Type == "Rectangle")
                {
                    var rect = new System.Windows.Shapes.Rectangle
                    {
                        Width = dto.Width,
                        Height = dto.Height,
                        Stroke = brush,
                        StrokeThickness = dto.Thickness,
                        Fill = Brushes.Transparent
                    };
                    InkCanvas.SetLeft(rect, dto.X);
                    InkCanvas.SetTop(rect, dto.Y);
                    DrawingCanvas.Children.Add(rect);
                }
                else if (dto.Type == "Ellipse")
                {
                    var ell = new System.Windows.Shapes.Ellipse
                    {
                        Width = dto.Width,
                        Height = dto.Height,
                        Stroke = brush,
                        StrokeThickness = dto.Thickness,
                        Fill = Brushes.Transparent
                    };
                    InkCanvas.SetLeft(ell, dto.X);
                    InkCanvas.SetTop(ell, dto.Y);
                    DrawingCanvas.Children.Add(ell);
                }
                else if (dto.Type == "Line")
                {
                    var line = new System.Windows.Shapes.Line
                    {
                        X1 = dto.X1,
                        Y1 = dto.Y1,
                        X2 = dto.X2,
                        Y2 = dto.Y2,
                        Stroke = brush,
                        StrokeThickness = dto.Thickness
                    };
                    InkCanvas.SetLeft(line, 0);
                    InkCanvas.SetTop(line, 0);
                    DrawingCanvas.Children.Add(line);
                }
                else if (dto.Type == "Arrow")
                {
                    var path = new System.Windows.Shapes.Path
                    {
                        Stroke = brush,
                        StrokeThickness = dto.Thickness,
                        Fill = brush,
                        Data = CreateArrowGeometry(new Point(dto.X1, dto.Y1), new Point(dto.X2, dto.Y2)),
                        Tag = new double[] { dto.X1, dto.Y1, dto.X2, dto.Y2 }
                    };
                    InkCanvas.SetLeft(path, 0);
                    InkCanvas.SetTop(path, 0);
                    DrawingCanvas.Children.Add(path);
                }
                else if (dto.Type == "Text")
                {
                    var tb = new TextBox
                    {
                        Text = dto.Text,
                        Width = dto.Width,
                        Height = dto.Height,
                        FontSize = dto.FontSize,
                        Foreground = brush,
                        Background = Brushes.Transparent,
                        BorderThickness = new Thickness(0),
                        AcceptsReturn = true,
                        TextWrapping = TextWrapping.Wrap,
                        Padding = new Thickness(4),
                        VerticalContentAlignment = VerticalAlignment.Top
                    };
                    InkCanvas.SetLeft(tb, dto.X);
                    InkCanvas.SetTop(tb, dto.Y);

                    tb.LostFocus += TextBox_LostFocus;
                    tb.GotFocus += TextBox_GotFocus;
                    
                    DrawingCanvas.Children.Add(tb);
                }
            }
        }

        private List<CanvasElementDto> CloneDtos(List<CanvasElementDto> source)
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(source);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<List<CanvasElementDto>>(json) ?? new List<CanvasElementDto>();
        }

        private CanvasState CaptureCurrentState()
        {
            var elements = new List<CanvasElementDto>();
            foreach (UIElement child in DrawingCanvas.Children)
            {
                if (child is System.Windows.Shapes.Rectangle rect)
                {
                    elements.Add(new CanvasElementDto
                    {
                        Type = "Rectangle",
                        X = InkCanvas.GetLeft(rect),
                        Y = InkCanvas.GetTop(rect),
                        Width = rect.Width,
                        Height = rect.Height,
                        ColorHex = (rect.Stroke is SolidColorBrush scb) ? scb.Color.ToString() : "#FF000000",
                        Thickness = rect.StrokeThickness
                    });
                }
                else if (child is System.Windows.Shapes.Ellipse ell)
                {
                    elements.Add(new CanvasElementDto
                    {
                        Type = "Ellipse",
                        X = InkCanvas.GetLeft(ell),
                        Y = InkCanvas.GetTop(ell),
                        Width = ell.Width,
                        Height = ell.Height,
                        ColorHex = (ell.Stroke is SolidColorBrush scb) ? scb.Color.ToString() : "#FF000000",
                        Thickness = ell.StrokeThickness
                    });
                }
                else if (child is System.Windows.Shapes.Line line)
                {
                    elements.Add(new CanvasElementDto
                    {
                        Type = "Line",
                        X1 = line.X1,
                        Y1 = line.Y1,
                        X2 = line.X2,
                        Y2 = line.Y2,
                        ColorHex = (line.Stroke is SolidColorBrush scb) ? scb.Color.ToString() : "#FF000000",
                        Thickness = line.StrokeThickness
                    });
                }
                else if (child is System.Windows.Shapes.Path path && path.Tag is double[] pathPts)
                {
                    elements.Add(new CanvasElementDto
                    {
                        Type = "Arrow",
                        X1 = pathPts[0],
                        Y1 = pathPts[1],
                        X2 = pathPts[2],
                        Y2 = pathPts[3],
                        ColorHex = (path.Stroke is SolidColorBrush scb) ? scb.Color.ToString() : "#FF000000",
                        Thickness = path.StrokeThickness
                    });
                }
                else if (child is TextBox tb)
                {
                    elements.Add(new CanvasElementDto
                    {
                        Type = "Text",
                        X = InkCanvas.GetLeft(tb),
                        Y = InkCanvas.GetTop(tb),
                        Width = double.IsNaN(tb.Width) ? tb.ActualWidth : tb.Width,
                        Height = double.IsNaN(tb.Height) ? tb.ActualHeight : tb.Height,
                        Text = tb.Text,
                        ColorHex = (tb.Foreground is SolidColorBrush scb) ? scb.Color.ToString() : "#FF000000",
                        FontSize = tb.FontSize
                    });
                }
            }
            return new CanvasState
            {
                Strokes = DrawingCanvas.Strokes.Clone(),
                Elements = elements
            };
        }

        public void RestoreCanvasState(CanvasState state)
        {
            _isUndoRedoing = true;
            SetStrokes(state.Strokes.Clone());
            RecreateChildrenFromDtos(state.Elements);
            _isUndoRedoing = false;
        }

        private Geometry CreateArrowGeometry(Point start, Point end)
        {
            var geometry = new PathGeometry();
            var figureLine = new PathFigure { StartPoint = start, IsClosed = false };
            figureLine.Segments.Add(new LineSegment(end, true));
            geometry.Figures.Add(figureLine);

            double angle = global::System.Math.Atan2(end.Y - start.Y, end.X - start.X);
            double arrowLength = 15;
            double arrowAngle = 30 * global::System.Math.PI / 180;

            Point arrowPoint1 = new Point(
                end.X - arrowLength * global::System.Math.Cos(angle - arrowAngle),
                end.Y - arrowLength * global::System.Math.Sin(angle - arrowAngle)
            );
            Point arrowPoint2 = new Point(
                end.X - arrowLength * global::System.Math.Cos(angle + arrowAngle),
                end.Y - arrowLength * global::System.Math.Sin(angle + arrowAngle)
            );

            var figureHead = new PathFigure { StartPoint = arrowPoint1, IsClosed = true };
            figureHead.Segments.Add(new LineSegment(end, true));
            figureHead.Segments.Add(new LineSegment(arrowPoint2, true));
            geometry.Figures.Add(figureHead);

            return geometry;
        }

        private void LoadPracticalApps()
        {
            bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
            string suffix = isVN ? "VN" : "EN";

            var items = new System.Collections.Generic.List<PracticalAppItem>
            {
                new PracticalAppItem
                {
                    Icon = "👩‍🏫",
                    Title = isVN ? "Ghi chép & Giảng bài sinh động" : "Interactive Notes & Dynamic Lecture",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_notebook_1_{suffix}.png",
                    Description = isVN 
                        ? "Sử dụng chế độ bảng đen và mực vẽ trắng phấn mô phỏng lớp học truyền thống, hoặc xuất bài giảng thành ảnh chất lượng cao để chia sẻ nhanh cho học sinh." 
                        : "Use blackboard mode with white ink to simulate a traditional classroom chalkboard, or export lecture pages as high-quality images to share instantly with students."
                },
                new PracticalAppItem
                {
                    Icon = "📓",
                    Title = isVN ? "Nhật ký học tập & Sáng tạo cá nhân" : "Learning Journal & Creative Space",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_notebook_2_{suffix}.png",
                    Description = isVN 
                        ? "Tạo các cuốn sổ tay cá nhân theo môn học hoặc nhật ký hàng ngày. Ghi nhận thời gian, thời tiết và tâm trạng học tập để nuôi dưỡng thói quen tự học hiệu quả." 
                        : "Create personalized subject notebooks or daily journals. Keep track of study times, weather, and learning moods to nurture highly effective self-study habits."
                },
                new PracticalAppItem
                {
                    Icon = "👥",
                    Title = isVN ? "Làm việc nhóm & Chia sẻ tài liệu" : "Collaborative Teamwork & Document Sharing",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_notebook_3_{suffix}.png",
                    Description = isVN 
                        ? "Ghi chép biên bản cuộc họp, thiết lập ý tưởng dự án nhóm và xuất tài liệu PDF/ảnh bài viết để phối hợp thảo luận nhóm dễ dàng, trực quan." 
                        : "Take meeting minutes, brainstorm project concepts with group members, and export visual/text notes as images for easy and intuitive teamwork collaboration."
                }
            };

            try
            {
                if (practicalAppViewer != null) practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for NotebookTool: {Err}", ex.Message);
            }
        }

        public void Dispose()
        {
            SaveCurrentNotebook();
            if (DrawingCanvas != null && DrawingCanvas.Strokes != null)
            {
                DrawingCanvas.Strokes.StrokesChanged -= Strokes_StrokesChanged;
                DrawingCanvas.Strokes.Clear();
            }
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewPractice == null || viewPractical == null)
                return;

            viewGuide.Visibility = Visibility.Collapsed;
            viewPractice.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            if (sideMenu.SelectedIndex == 2) // Tab Ứng dụng thực tế
            {
                SaveCurrentNotebook();
                
                viewPractical.Visibility = Visibility.Visible;
            }
            else
            {
                

                if (sideMenu.SelectedIndex == 0)
                {
                    SaveCurrentNotebook();
                    viewGuide.Visibility = Visibility.Visible;
                }
                else if (sideMenu.SelectedIndex == 1)
                {
                    viewPractice.Visibility = Visibility.Visible;
                }
            }
        }
    }

    public class CanvasState
    {
        public StrokeCollection Strokes { get; set; } = new StrokeCollection();
        public List<CanvasElementDto> Elements { get; set; } = new List<CanvasElementDto>();
    }

    public class CanvasElementDto
    {
        public string Type { get; set; } = "Rectangle";
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string ColorHex { get; set; } = "#FF000000";
        public double Thickness { get; set; } = 3.0;
        public string Text { get; set; } = "";
        public double FontSize { get; set; } = 16.0;
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }
    }
}