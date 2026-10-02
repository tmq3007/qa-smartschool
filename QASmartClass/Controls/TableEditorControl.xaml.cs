using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Globalization;
using QASmartTouch.Models;

namespace QASmartTouch.Controls
{
    public partial class TableEditorControl : UserControl
    {
        private TableData _tableData;
        private TextBox? _currentFocusedCell;
        private int _currentRow = 0;
        private int _currentColumn = 0;

        // Drag resize state
        private bool _isResizingColumn = false;
        private bool _isResizingRow = false;
        private int _resizingColumnIndex = -1;
        private int _resizingRowIndex = -1;
        private Point _resizeStartPoint;
        private double _resizeStartSize;
        private const double ResizeMargin = 6;

        // Table outer resize state
        private bool _isResizingTable = false;
        private string _resizeHandleType = "";
        private Size _tableStartSize;
        private Point _tableResizeStartPoint;
        private double[]? _originalColumnWidths;
        private double[]? _originalRowHeights;

        // Cell selection state
        private bool _isSelecting = false;
        private Point _selectionStartCell = new Point(-1, -1);
        private Point _selectionEndCell = new Point(-1, -1);
        private List<Border> _selectedCellBorders = new List<Border>();
        private TextBox? _lastFocusedTextBox = null; // pixels from border to trigger resize

        public TableData TableData => _tableData;
        public bool DialogResult { get; private set; } = false;

        public event EventHandler? InsertRequested;
        public event EventHandler? CancelRequested;

        public TableEditorControl()
        {
            InitializeComponent();
            
            // Initialize with default 2x2 table
            _tableData = new TableData(2, 2);
            
            // Set checkbox state after initialization
            chkAutoHeight.IsChecked = _tableData.AutoExpandRows;
            
            BuildTableUI();
        }

        public TableEditorControl(TableData existingData)
        {
            InitializeComponent();
            
            _tableData = existingData;
            
            // Set checkbox state after initialization
            chkAutoHeight.IsChecked = _tableData.AutoExpandRows;
            
            BuildTableUI();
        }

        /// <summary>
        /// ✅ QC_4.2_TABLE_EDIT_FIX: Nạp dữ liệu bảng đã tồn tại vào control hiện hữu.
        /// Được gọi từ Form2_TableEditorDialog khi chỉnh sửa Bảng đã chèn,
        /// thay vì tạo instance mới (tránh lỗi orphaned control).
        /// </summary>
        /// <param name="existingData">Dữ liệu TableData từ Bảng đang hiển thị trên Canvas</param>
        public void LoadExistingData(TableData existingData)
        {
            _tableData = existingData ?? throw new ArgumentNullException(nameof(existingData));
            chkAutoHeight.IsChecked = _tableData.AutoExpandRows;
            BuildTableUI();
        }

        /// <summary>
        /// Build the dynamic table UI based on TableData
        /// </summary>
        private void BuildTableUI()
        {
            tableContainer.Children.Clear();
            tableContainer.RowDefinitions.Clear();
            tableContainer.ColumnDefinitions.Clear();
            tableContainer.Background = Brushes.Transparent; // Enable mouse events

            // Create column definitions
            for (int c = 0; c < _tableData.ColumnCount; c++)
            {
                var colDef = new ColumnDefinition
                {
                    Width = new GridLength(_tableData.Columns[c].Width)
                };
                tableContainer.ColumnDefinitions.Add(colDef);
            }

            // Create row definitions
            for (int r = 0; r < _tableData.RowCount; r++)
            {
                var rowDef = new RowDefinition
                {
                    Height = new GridLength(_tableData.Rows[r].Height)
                };
                tableContainer.RowDefinitions.Add(rowDef);
            }

            // Create cells
            for (int r = 0; r < _tableData.RowCount; r++)
            {
                for (int c = 0; c < _tableData.ColumnCount; c++)
                {
                    var cell = _tableData.GetCell(r, c);
                    
                    // Cell border
                    var cellBorder = new Border
                    {
                        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_tableData.BorderColor)),
                        BorderThickness = new Thickness(_tableData.BorderThickness),
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(cell.BackgroundColor))
                    };

                    // Cell TextBox
                    var textBox = new TextBox
                    {
                        Text = cell.Content,
                        Style = (Style)this.Resources["CellTextBoxStyle"],
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(cell.ForegroundColor)),
                        FontFamily = new FontFamily(cell.FontFamily),
                        FontSize = cell.FontSize,
                        FontWeight = cell.IsBold ? FontWeights.Bold : FontWeights.Normal,
                        FontStyle = cell.IsItalic ? FontStyles.Italic : FontStyles.Normal,
                        TextAlignment = GetTextAlignment(cell.HorizontalAlignment),
                        VerticalContentAlignment = GetVerticalAlignment(cell.VerticalAlignment),
                        VerticalScrollBarVisibility = _tableData.AutoExpandRows ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto,
                        MaxHeight = _tableData.AutoExpandRows ? double.PositiveInfinity : _tableData.Rows[r].Height - (_tableData.CellPadding * 2),
                        Tag = new { Row = r, Column = c }
                    };

                    if (cell.IsUnderline)
                    {
                        textBox.TextDecorations = TextDecorations.Underline;
                    }

                    // Events
                    textBox.TextChanged += CellTextBox_TextChanged;
                    textBox.GotFocus += CellTextBox_GotFocus;
                    textBox.PreviewKeyDown += CellTextBox_PreviewKeyDown;
                    textBox.SizeChanged += CellTextBox_SizeChanged;

                    cellBorder.Child = textBox;
                    cellBorder.Tag = new { Row = r, Column = c };
                    
                    // Border mouse events for selection
                    cellBorder.MouseLeftButtonDown += CellBorder_MouseLeftButtonDown;
                    cellBorder.MouseEnter += CellBorder_MouseEnter;
                    cellBorder.MouseLeftButtonUp += CellBorder_MouseLeftButtonUp;

                    Grid.SetRow(cellBorder, r);
                    Grid.SetColumn(cellBorder, c);

                    tableContainer.Children.Add(cellBorder);
                }
            }

            // Add mouse events for resize functionality
            tableContainer.MouseMove += TableContainer_MouseMove;
            tableContainer.MouseLeftButtonDown += TableContainer_MouseLeftButtonDown;
            tableContainer.MouseLeftButtonUp += TableContainer_MouseLeftButtonUp;
            tableContainer.MouseLeave += TableContainer_MouseLeave;
        }

        private TextAlignment GetTextAlignment(string alignment)
        {
            return alignment switch
            {
                "Left" => TextAlignment.Left,
                "Center" => TextAlignment.Center,
                "Right" => TextAlignment.Right,
                _ => TextAlignment.Left
            };
        }

        private VerticalAlignment GetVerticalAlignment(string alignment)
        {
            return alignment switch
            {
                "Top" => VerticalAlignment.Top,
                "Center" => VerticalAlignment.Center,
                "Bottom" => VerticalAlignment.Bottom,
                _ => VerticalAlignment.Center
            };
        }

        private void CellTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox?.Tag != null)
            {
                dynamic tag = textBox.Tag;
                int row = tag.Row;
                int col = tag.Column;
                _tableData.GetCell(row, col).Content = textBox.Text;
                
                // Auto-adjust row height if needed (with small delay to avoid too frequent updates)
                if (_tableData.AutoExpandRows)
                {
                    Dispatcher.BeginInvoke(new Action(() => AutoAdjustRowHeight(row)), 
                        System.Windows.Threading.DispatcherPriority.Background);
                }
            }
        }

        private void CellTextBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // When TextBox size changes (due to wrapping), adjust row height
            var textBox = sender as TextBox;
            if (textBox?.Tag != null && _tableData.AutoExpandRows)
            {
                dynamic tag = textBox.Tag;
                int row = tag.Row;
                AutoAdjustRowHeight(row);
            }
        }

        private void CellTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            _currentFocusedCell = sender as TextBox;
            
            if (_currentFocusedCell?.Tag != null)
            {
                dynamic tag = _currentFocusedCell.Tag;
                _currentRow = tag.Row;
                _currentColumn = tag.Column;
            }
            
            // Clear selection if not holding Ctrl
            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
            {
                ClearSelection();
            }
        }

        private void CellTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox?.Tag != null)
            {
                dynamic tag = textBox.Tag;
                int row = tag.Row;
                int col = tag.Column;

                // Tab: move to next cell
                if (e.Key == Key.Tab && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
                {
                    e.Handled = true;
                    MoveFocus(row, col + 1);
                }
                // Shift+Tab: move to previous cell
                else if (e.Key == Key.Tab && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                {
                    e.Handled = true;
                    MoveFocus(row, col - 1);
                }
                // Enter: move to cell below
                else if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
                {
                    if (textBox.Text.Length == 0 || textBox.CaretIndex == textBox.Text.Length)
                    {
                        e.Handled = true;
                        MoveFocus(row + 1, col);
                    }
                }
                // Ctrl+B: Bold
                else if (e.Key == Key.B && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    e.Handled = true;
                    ToggleBold();
                }
                // Ctrl+I: Italic
                else if (e.Key == Key.I && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    e.Handled = true;
                    ToggleItalic();
                }
                // Ctrl+U: Underline
                else if (e.Key == Key.U && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    e.Handled = true;
                    ToggleUnderline();
                }
            }
        }

        private void MoveFocus(int targetRow, int targetCol)
        {
            // Wrap around logic
            if (targetCol >= _tableData.ColumnCount)
            {
                targetCol = 0;
                targetRow++;
            }
            else if (targetCol < 0)
            {
                targetCol = _tableData.ColumnCount - 1;
                targetRow--;
            }

            if (targetRow >= _tableData.RowCount)
                targetRow = 0;
            else if (targetRow < 0)
                targetRow = _tableData.RowCount - 1;

            // Find and focus the target cell
            foreach (var child in tableContainer.Children)
            {
                if (child is Border border && border.Child is TextBox textBox && textBox.Tag != null)
                {
                    dynamic tag = textBox.Tag;
                    if (tag.Row == targetRow && tag.Column == targetCol)
                    {
                        textBox.Focus();
                        textBox.SelectAll();
                        break;
                    }
                }
            }
        }

        private void AutoAdjustRowHeight(int rowIndex)
        {
            if (!_tableData.AutoExpandRows)
                return;

            double maxHeight = _tableData.Rows[rowIndex].MinHeight;
            
            // Measure actual text height for all cells in this row
            foreach (var child in tableContainer.Children)
            {
                if (child is Border border && Grid.GetRow(border) == rowIndex)
                {
                    if (border.Child is TextBox textBox && !string.IsNullOrEmpty(textBox.Text))
                    {
                        // Get column width for this cell
                        int colIndex = Grid.GetColumn(border);
                        double availableWidth = _tableData.Columns[colIndex].Width - (_tableData.CellPadding * 2);
                        
                        // Measure text height using FormattedText
                        var formattedText = new FormattedText(
                            textBox.Text,
                            System.Globalization.CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            new Typeface(textBox.FontFamily, textBox.FontStyle, textBox.FontWeight, FontStretches.Normal),
                            textBox.FontSize,
                            textBox.Foreground,
                            VisualTreeHelper.GetDpi(textBox).PixelsPerDip
                        )
                        {
                            MaxTextWidth = availableWidth,
                            Trimming = TextTrimming.None
                        };
                        
                        double requiredHeight = formattedText.Height + (_tableData.CellPadding * 2) + 8; // extra padding
                        
                        if (requiredHeight > maxHeight)
                        {
                            maxHeight = requiredHeight;
                        }
                    }
                }
            }

            // Update row height if needed
            if (maxHeight > _tableData.Rows[rowIndex].Height)
            {
                _tableData.Rows[rowIndex].Height = maxHeight;
                tableContainer.RowDefinitions[rowIndex].Height = new GridLength(maxHeight);
            }
        }

        // Toolbar button handlers
        private void btnBold_Click(object sender, RoutedEventArgs e)
        {
            ToggleBold();
        }

        private void btnItalic_Click(object sender, RoutedEventArgs e)
        {
            ToggleItalic();
        }

        private void btnUnderline_Click(object sender, RoutedEventArgs e)
        {
            ToggleUnderline();
        }

        private void btnAlignLeft_Click(object sender, RoutedEventArgs e)
        {
            ApplyAlignment("Left");
        }

        private void btnAlignCenter_Click(object sender, RoutedEventArgs e)
        {
            ApplyAlignment("Center");
        }

        private void btnAlignRight_Click(object sender, RoutedEventArgs e)
        {
            ApplyAlignment("Right");
        }

        private void ToggleBold()
        {
            ApplyFormatToSelection(cell => 
            {
                cell.IsBold = !cell.IsBold;
            });
        }

        private void ToggleItalic()
        {
            ApplyFormatToSelection(cell => 
            {
                cell.IsItalic = !cell.IsItalic;
            });
        }

        private void ToggleUnderline()
        {
            ApplyFormatToSelection(cell => 
            {
                cell.IsUnderline = !cell.IsUnderline;
            });
        }

        private void ApplyAlignment(string alignment)
        {
            ApplyFormatToSelection(cell => 
            {
                cell.HorizontalAlignment = alignment;
            });
        }

        private void btnAddRow_Click(object sender, RoutedEventArgs e)
        {
            _tableData.AddRow();
            BuildTableUI();
        }

        private void btnAddColumn_Click(object sender, RoutedEventArgs e)
        {
            _tableData.AddColumn();
            BuildTableUI();
        }

        private void btnDeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (_tableData.RowCount > 1)
            {
                // Delete current focused row, or last row
                int rowToDelete = _currentRow;
                if (rowToDelete >= _tableData.RowCount)
                    rowToDelete = _tableData.RowCount - 1;
                    
                _tableData.DeleteRow(rowToDelete);
                BuildTableUI();
            }
            else
            {
                MessageBox.Show("Không thể xóa dòng cuối cùng!", "Thông báo", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnDeleteColumn_Click(object sender, RoutedEventArgs e)
        {
            if (_tableData.ColumnCount > 1)
            {
                // Delete current focused column, or last column
                int colToDelete = _currentColumn;
                if (colToDelete >= _tableData.ColumnCount)
                    colToDelete = _tableData.ColumnCount - 1;
                    
                _tableData.DeleteColumn(colToDelete);
                BuildTableUI();
            }
            else
            {
                MessageBox.Show("Không thể xóa cột cuối cùng!", "Thông báo", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnAutoFit_Click(object sender, RoutedEventArgs e)
        {
            AutoFitAllCellsToContent();
        }

        private void chkAutoHeight_Checked(object sender, RoutedEventArgs e)
        {
            if (_tableData == null) return;
            
            _tableData.AutoExpandRows = true;
            // Re-calculate all row heights
            for (int i = 0; i < _tableData.RowCount; i++)
            {
                AutoAdjustRowHeight(i);
            }
        }

        private void chkAutoHeight_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_tableData == null) return;
            
            _tableData.AutoExpandRows = false;
        }

        /// <summary>
        /// Auto-fit all cells to their content - adjust both column widths and row heights
        /// </summary>
        private void AutoFitAllCellsToContent()
        {
            try
            {
                // Calculate optimal column widths
                for (int c = 0; c < _tableData.ColumnCount; c++)
                {
                    double maxWidth = _tableData.Columns[c].MinWidth;

                    for (int r = 0; r < _tableData.RowCount; r++)
                    {
                        var cell = _tableData.GetCell(r, c);
                        if (!string.IsNullOrEmpty(cell.Content))
                        {
                            // Measure text width
                            var formattedText = new FormattedText(
                                cell.Content,
                                CultureInfo.CurrentCulture,
                                FlowDirection.LeftToRight,
                                new Typeface(new FontFamily(cell.FontFamily), 
                                    cell.IsItalic ? FontStyles.Italic : FontStyles.Normal,
                                    cell.IsBold ? FontWeights.Bold : FontWeights.Normal,
                                    FontStretches.Normal),
                                cell.FontSize,
                                Brushes.Black,
                                96
                            );

                            double requiredWidth = formattedText.Width + (_tableData.CellPadding * 2) + 16;
                            
                            // Cap at max width (40% of 800px default)
                            requiredWidth = Math.Min(requiredWidth, 320);

                            if (requiredWidth > maxWidth)
                            {
                                maxWidth = requiredWidth;
                            }
                        }
                    }

                    _tableData.Columns[c].Width = maxWidth;
                }

                // Calculate optimal row heights
                for (int r = 0; r < _tableData.RowCount; r++)
                {
                    double maxHeight = _tableData.Rows[r].MinHeight;

                    for (int c = 0; c < _tableData.ColumnCount; c++)
                    {
                        var cell = _tableData.GetCell(r, c);
                        if (!string.IsNullOrEmpty(cell.Content))
                        {
                            double availableWidth = _tableData.Columns[c].Width - (_tableData.CellPadding * 2);

                            var formattedText = new FormattedText(
                                cell.Content,
                                CultureInfo.CurrentCulture,
                                FlowDirection.LeftToRight,
                                new Typeface(new FontFamily(cell.FontFamily),
                                    cell.IsItalic ? FontStyles.Italic : FontStyles.Normal,
                                    cell.IsBold ? FontWeights.Bold : FontWeights.Normal,
                                    FontStretches.Normal),
                                cell.FontSize,
                                Brushes.Black,
                                96
                            )
                            {
                                MaxTextWidth = availableWidth,
                                Trimming = TextTrimming.None
                            };

                            double requiredHeight = formattedText.Height + (_tableData.CellPadding * 2) + 8;

                            if (requiredHeight > maxHeight)
                            {
                                maxHeight = requiredHeight;
                            }
                        }
                    }

                    _tableData.Rows[r].Height = maxHeight;
                }

                // Rebuild UI with new sizes
                BuildTableUI();

                MessageBox.Show("Đã điều chỉnh kích thước bảng vừa khít với nội dung!", "Thành công",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tự động điều chỉnh: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }

        private void btnTextColor_Click(object sender, RoutedEventArgs e)
        {
            string selectedColor = ShowColorPicker("Chọn màu chữ");
            if (selectedColor != null)
            {
                ApplyFormatToSelection(cell => 
                {
                    cell.ForegroundColor = selectedColor;
                });
            }
        }

        private void btnBackgroundColor_Click(object sender, RoutedEventArgs e)
        {
            string selectedColor = ShowColorPicker("Chọn màu nền");
            if (selectedColor != null)
            {
                ApplyFormatToSelection(cell => 
                {
                    cell.BackgroundColor = selectedColor;
                });
            }
        }

        private string? ShowColorPicker(string title)
        {
            // Simple color picker with common colors
            var colors = new[] 
            { 
                "#000000", "#FFFFFF", "#FF0000", "#00FF00", "#0000FF", 
                "#FFFF00", "#FF00FF", "#00FFFF", "#FFA500", "#800080",
                "#FFC0CB", "#A52A2A", "#808080", "#00FF7F", "#4169E1"
            };

            var dialog = new Window
            {
                Title = title,
                Width = 300,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = System.Windows.ResizeMode.NoResize
            };
            QASmartTouch.Helpers.TouchActivationHelper.ApplyToWindow(dialog);

            var panel = new WrapPanel { Margin = new Thickness(10) };
            string? selectedColor = null;

            foreach (var color in colors)
            {
                var btn = new Button
                {
                    Width = 50,
                    Height = 50,
                    Margin = new Thickness(5),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
                    BorderThickness = new Thickness(2),
                    BorderBrush = Brushes.Gray,
                    Tag = color,
                    Focusable = false
                };

                btn.Click += (s, e) =>
                {
                    selectedColor = (s as Button)?.Tag as string;
                    dialog.DialogResult = true;
                    dialog.Close();
                };

                QASmartTouch.Helpers.TouchActivationHelper.WireButton(btn);
                panel.Children.Add(btn);
            }

            dialog.Content = panel;
            dialog.ShowDialog();

            return selectedColor;
        }

        private void btnInsert_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            InsertRequested?.Invoke(this, EventArgs.Empty);
        }

        #region Cell Selection

        private void CellBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                var border = sender as Border;
                if (border?.Tag != null)
                {
                    dynamic tag = border.Tag;
                    int row = tag.Row;
                    int col = tag.Column;

                    _isSelecting = true;
                    _selectionStartCell = new Point(col, row);
                    _selectionEndCell = new Point(col, row);

                    UpdateSelection();
                    e.Handled = true;
                }
            }
        }

        private void CellBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            if (_isSelecting && e.LeftButton == MouseButtonState.Pressed)
            {
                var border = sender as Border;
                if (border?.Tag != null)
                {
                    dynamic tag = border.Tag;
                    int row = tag.Row;
                    int col = tag.Column;

                    _selectionEndCell = new Point(col, row);
                    UpdateSelection();
                }
            }
        }

        private void CellBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isSelecting)
            {
                _isSelecting = false;
            }
        }

        private void UpdateSelection()
        {
            // Clear previous selection highlights
            foreach (var border in _selectedCellBorders)
            {
                border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_tableData.BorderColor));
                border.BorderThickness = new Thickness(_tableData.BorderThickness);
                border.Opacity = 1.0;
            }
            _selectedCellBorders.Clear();

            if (_selectionStartCell.X < 0 || _selectionStartCell.Y < 0)
                return;

            // Calculate selection range
            int startCol = (int)Math.Min(_selectionStartCell.X, _selectionEndCell.X);
            int endCol = (int)Math.Max(_selectionStartCell.X, _selectionEndCell.X);
            int startRow = (int)Math.Min(_selectionStartCell.Y, _selectionEndCell.Y);
            int endRow = (int)Math.Max(_selectionStartCell.Y, _selectionEndCell.Y);

            // Highlight selected cells
            foreach (UIElement child in tableContainer.Children)
            {
                if (child is Border border && border.Tag != null)
                {
                    dynamic tag = border.Tag;
                    int row = tag.Row;
                    int col = tag.Column;

                    if (row >= startRow && row <= endRow && col >= startCol && col <= endCol)
                    {
                        // Create selection highlight
                        border.BorderBrush = new SolidColorBrush(Color.FromRgb(46, 134, 222)); // #2E86DE
                        border.BorderThickness = new Thickness(3);
                        border.Opacity = 0.95;
                        _selectedCellBorders.Add(border);
                    }
                }
            }
        }

        private void ClearSelection()
        {
            foreach (var border in _selectedCellBorders)
            {
                border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_tableData.BorderColor));
                border.BorderThickness = new Thickness(_tableData.BorderThickness);
                border.Opacity = 1.0;
            }
            _selectedCellBorders.Clear();
            _selectionStartCell = new Point(-1, -1);
            _selectionEndCell = new Point(-1, -1);
        }

        private void ApplyFormatToSelection(Action<TableCell> formatAction)
        {
            if (_selectedCellBorders.Count == 0)
            {
                // No selection, apply to current focused cell
                if (_currentFocusedCell?.Tag != null)
                {
                    dynamic tag = _currentFocusedCell.Tag;
                    int row = tag.Row;
                    int col = tag.Column;
                    var cell = _tableData.GetCell(row, col);
                    formatAction(cell);
                    
                    // Update visual
                    _currentFocusedCell.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(cell.ForegroundColor));
                    _currentFocusedCell.FontWeight = cell.IsBold ? FontWeights.Bold : FontWeights.Normal;
                    _currentFocusedCell.FontStyle = cell.IsItalic ? FontStyles.Italic : FontStyles.Normal;
                    _currentFocusedCell.TextAlignment = GetTextAlignment(cell.HorizontalAlignment);
                    _currentFocusedCell.TextDecorations = cell.IsUnderline ? TextDecorations.Underline : null;
                    
                    // Get parent border to update background
                    if (_currentFocusedCell.Parent is Border parentBorder)
                    {
                        parentBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(cell.BackgroundColor));
                    }
                }
            }
            else
            {
                // Apply to all selected cells
                foreach (var border in _selectedCellBorders)
                {
                    if (border.Tag != null)
                    {
                        dynamic tag = border.Tag;
                        int row = tag.Row;
                        int col = tag.Column;
                        var cell = _tableData.GetCell(row, col);
                        formatAction(cell);

                        // Update TextBox visual
                        if (border.Child is TextBox textBox)
                        {
                            textBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(cell.ForegroundColor));
                            textBox.FontWeight = cell.IsBold ? FontWeights.Bold : FontWeights.Normal;
                            textBox.FontStyle = cell.IsItalic ? FontStyles.Italic : FontStyles.Normal;
                            textBox.TextAlignment = GetTextAlignment(cell.HorizontalAlignment);
                            textBox.VerticalContentAlignment = GetVerticalAlignment(cell.VerticalAlignment);
                            textBox.TextDecorations = cell.IsUnderline ? TextDecorations.Underline : null;
                        }

                        // Update border background
                        border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(cell.BackgroundColor));
                    }
                }
            }
        }

        #endregion

        #region Table Outer Resize Handlers

        private void Handle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border handle && handle.Tag is string handleType)
            {
                _isResizingTable = true;
                _resizeHandleType = handleType;
                _tableResizeStartPoint = e.GetPosition(tableWrapper);
                
                // Save original column widths and row heights
                _originalColumnWidths = new double[_tableData.ColumnCount];
                _originalRowHeights = new double[_tableData.RowCount];
                
                double totalWidth = 0;
                double totalHeight = 0;
                
                for (int c = 0; c < _tableData.ColumnCount; c++)
                {
                    _originalColumnWidths[c] = _tableData.Columns[c].Width;
                    totalWidth += _tableData.Columns[c].Width;
                }
                
                for (int r = 0; r < _tableData.RowCount; r++)
                {
                    _originalRowHeights[r] = _tableData.Rows[r].Height;
                    totalHeight += _tableData.Rows[r].Height;
                }
                
                _tableStartSize = new Size(totalWidth, totalHeight);

                handle.CaptureMouse();
                e.Handled = true;
            }
        }

        private void Handle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isResizingTable)
            {
                _isResizingTable = false;
                _resizeHandleType = "";
                _originalColumnWidths = null;
                _originalRowHeights = null;
                
                if (sender is Border handle)
                {
                    handle.ReleaseMouseCapture();
                }
                
                e.Handled = true;
            }
        }

        private void Handle_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isResizingTable) return;

            Point currentPoint = e.GetPosition(tableWrapper);
            double deltaX = currentPoint.X - _tableResizeStartPoint.X;
            double deltaY = currentPoint.Y - _tableResizeStartPoint.Y;

            double newWidth = _tableStartSize.Width;
            double newHeight = _tableStartSize.Height;

            // Calculate minimum table size based on columns/rows count
            double minWidth = _tableData.ColumnCount * 80;  // MinWidth per column
            double minHeight = _tableData.RowCount * 40;    // MinHeight per row

            // Calculate new size based on handle type
            switch (_resizeHandleType)
            {
                case "TopLeft":
                    newWidth = Math.Max(minWidth, _tableStartSize.Width - deltaX);
                    newHeight = Math.Max(minHeight, _tableStartSize.Height - deltaY);
                    break;
                case "TopRight":
                    newWidth = Math.Max(minWidth, _tableStartSize.Width + deltaX);
                    newHeight = Math.Max(minHeight, _tableStartSize.Height - deltaY);
                    break;
                case "BottomLeft":
                    newWidth = Math.Max(minWidth, _tableStartSize.Width - deltaX);
                    newHeight = Math.Max(minHeight, _tableStartSize.Height + deltaY);
                    break;
                case "BottomRight":
                    newWidth = Math.Max(minWidth, _tableStartSize.Width + deltaX);
                    newHeight = Math.Max(minHeight, _tableStartSize.Height + deltaY);
                    break;
                case "Top":
                    newHeight = Math.Max(minHeight, _tableStartSize.Height - deltaY);
                    break;
                case "Bottom":
                    newHeight = Math.Max(minHeight, _tableStartSize.Height + deltaY);
                    break;
                case "Left":
                    newWidth = Math.Max(minWidth, _tableStartSize.Width - deltaX);
                    break;
                case "Right":
                    newWidth = Math.Max(minWidth, _tableStartSize.Width + deltaX);
                    break;
            }

            // Calculate scale factors
            double scaleX = newWidth / _tableStartSize.Width;
            double scaleY = newHeight / _tableStartSize.Height;

            // Apply proportional scaling to columns using ORIGINAL widths
            if (_originalColumnWidths != null && Math.Abs(scaleX - 1.0) > 0.001 && !double.IsNaN(scaleX) && !double.IsInfinity(scaleX))
            {
                for (int c = 0; c < _tableData.ColumnCount; c++)
                {
                    // Scale from original width, not current width
                    double originalWidth = _originalColumnWidths[c];
                    double newColWidth = originalWidth * scaleX;
                    double finalWidth = Math.Max(_tableData.Columns[c].MinWidth, newColWidth);
                    
                    _tableData.Columns[c].Width = finalWidth;
                    tableContainer.ColumnDefinitions[c].Width = new GridLength(finalWidth);
                }
            }

            // Apply proportional scaling to rows using ORIGINAL heights
            if (_originalRowHeights != null && Math.Abs(scaleY - 1.0) > 0.001 && !double.IsNaN(scaleY) && !double.IsInfinity(scaleY))
            {
                for (int r = 0; r < _tableData.RowCount; r++)
                {
                    // Scale from original height, not current height
                    double originalHeight = _originalRowHeights[r];
                    double newRowHeight = originalHeight * scaleY;
                    double finalHeight = Math.Max(_tableData.Rows[r].MinHeight, newRowHeight);
                    
                    _tableData.Rows[r].Height = finalHeight;
                    tableContainer.RowDefinitions[r].Height = new GridLength(finalHeight);
                }
            }

            e.Handled = true;
        }

        #endregion

        #region Drag Resize Functionality

        private void TableContainer_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isResizingColumn)
            {
                // Resize column
                Point currentPoint = e.GetPosition(tableContainer);
                double delta = currentPoint.X - _resizeStartPoint.X;
                double newWidth = Math.Max(_tableData.Columns[_resizingColumnIndex].MinWidth, _resizeStartSize + delta);
                
                _tableData.Columns[_resizingColumnIndex].Width = newWidth;
                tableContainer.ColumnDefinitions[_resizingColumnIndex].Width = new GridLength(newWidth);
                
                e.Handled = true;
                return;
            }
            else if (_isResizingRow)
            {
                // Resize row
                Point currentPoint = e.GetPosition(tableContainer);
                double delta = currentPoint.Y - _resizeStartPoint.Y;
                double newHeight = Math.Max(_tableData.Rows[_resizingRowIndex].MinHeight, _resizeStartSize + delta);
                
                _tableData.Rows[_resizingRowIndex].Height = newHeight;
                tableContainer.RowDefinitions[_resizingRowIndex].Height = new GridLength(newHeight);
                
                e.Handled = true;
                return;
            }

            // Check if mouse is near column or row border
            Point mousePos = e.GetPosition(tableContainer);

            // Check column borders
            double xPos = 0;
            for (int c = 0; c < _tableData.ColumnCount - 1; c++) // Exclude last column
            {
                xPos += _tableData.Columns[c].Width;
                
                if (Math.Abs(mousePos.X - xPos) < ResizeMargin)
                {
                    tableContainer.Cursor = Cursors.SizeWE; // ↔ Horizontal resize cursor
                    return;
                }
            }

            // Check row borders
            double yPos = 0;
            for (int r = 0; r < _tableData.RowCount - 1; r++) // Exclude last row
            {
                yPos += _tableData.Rows[r].Height;
                
                if (Math.Abs(mousePos.Y - yPos) < ResizeMargin)
                {
                    tableContainer.Cursor = Cursors.SizeNS; // ↕ Vertical resize cursor
                    return;
                }
            }

            // No border detected
            tableContainer.Cursor = Cursors.Arrow;
        }

        private void TableContainer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point mousePos = e.GetPosition(tableContainer);

            // Check column borders
            double xPos = 0;
            for (int c = 0; c < _tableData.ColumnCount - 1; c++)
            {
                xPos += _tableData.Columns[c].Width;
                
                if (Math.Abs(mousePos.X - xPos) < ResizeMargin)
                {
                    _isResizingColumn = true;
                    _resizingColumnIndex = c;
                    _resizeStartPoint = mousePos;
                    _resizeStartSize = _tableData.Columns[c].Width;
                    tableContainer.CaptureMouse();
                    e.Handled = true;
                    return;
                }
            }

            // Check row borders
            double yPos = 0;
            for (int r = 0; r < _tableData.RowCount - 1; r++)
            {
                yPos += _tableData.Rows[r].Height;
                
                if (Math.Abs(mousePos.Y - yPos) < ResizeMargin)
                {
                    _isResizingRow = true;
                    _resizingRowIndex = r;
                    _resizeStartPoint = mousePos;
                    _resizeStartSize = _tableData.Rows[r].Height;
                    tableContainer.CaptureMouse();
                    e.Handled = true;
                    return;
                }
            }
        }

        private void TableContainer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isResizingColumn || _isResizingRow)
            {
                _isResizingColumn = false;
                _isResizingRow = false;
                _resizingColumnIndex = -1;
                _resizingRowIndex = -1;
                tableContainer.ReleaseMouseCapture();
                tableContainer.Cursor = Cursors.Arrow;
                e.Handled = true;
            }
        }

        private void TableContainer_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!_isResizingColumn && !_isResizingRow)
            {
                tableContainer.Cursor = Cursors.Arrow;
            }
        }

        #endregion
    }
}
