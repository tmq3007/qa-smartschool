using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QASmartTouch.Managers
{
    /// <summary>
    /// Manages board state, multiple boards (1-10), and board settings
    /// Handles board creation, deletion, switching, and configuration
    /// </summary>
    public class BoardManager
    {
        #region Constants
        
        /// <summary>
        /// Maximum number of boards allowed (1-10)
        /// </summary>
        public const int MAX_BOARDS = 10;
        
        /// <summary>
        /// Minimum number of boards (always at least 1)
        /// </summary>
        public const int MIN_BOARDS = 1;
        
        #endregion
        
        #region Fields
        
        private readonly Canvas _mainCanvas;
        private readonly List<BoardState> _boards;
        private int _currentBoardIndex;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Gets the current active board index (0-based)
        /// </summary>
        public int CurrentBoardIndex => _currentBoardIndex;
        
        /// <summary>
        /// Gets the current active board
        /// </summary>
        public BoardState CurrentBoard => _boards[_currentBoardIndex];
        
        /// <summary>
        /// Gets the total number of boards
        /// </summary>
        public int BoardCount => _boards.Count;
        
        /// <summary>
        /// Gets whether the maximum number of boards has been reached
        /// </summary>
        public bool IsMaxBoardsReached => _boards.Count >= MAX_BOARDS;
        
        /// <summary>
        /// Gets whether only one board exists (cannot delete)
        /// </summary>
        public bool IsMinBoardsReached => _boards.Count <= MIN_BOARDS;
        
        /// <summary>
        /// Gets all boards (read-only)
        /// </summary>
        public IReadOnlyList<BoardState> Boards => _boards.AsReadOnly();
        
        /// <summary>
        /// External predicate to check if an element is a system UI control (QC_4.2_STATE_GUARD)
        /// </summary>
        public Func<UIElement, bool>? IsSystemElementPredicate { get; set; }

        /// <summary>
        /// WP6: Unified System Element check for BoardManager operations
        /// </summary>
        public bool IsSystemElement(UIElement element)
        {
            if (element == null) return false;
            if (IsSystemElementPredicate != null && IsSystemElementPredicate(element)) return true;

            string typeName = element.GetType().Name;
            if (typeName == "SelectionBox" || typeName == "ContextToolbar" || 
                typeName == "ThicknessPicker" || typeName == "ColorPicker" || 
                typeName == "MoreMenu" || typeName == "FloatingTouchKeyboard" ||
                typeName == "NotificationWindow")
                return true;

            if (element is FrameworkElement fe && fe.Tag?.ToString() == "EraserPreview")
                return true;

            if (Panel.GetZIndex(element) >= 10000) return true;

            return false;
        }

        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when a board is created
        /// </summary>
        public event EventHandler<BoardEventArgs>? BoardCreated;
        
        /// <summary>
        /// Fired when a board is deleted
        /// </summary>
        public event EventHandler<BoardEventArgs>? BoardDeleted;
        
        /// <summary>
        /// Fired when switching to a different board
        /// </summary>
        public event EventHandler<BoardSwitchEventArgs>? BoardSwitched;
        
        /// <summary>
        /// Fired when board settings are changed
        /// </summary>
        public event EventHandler<BoardEventArgs>? BoardSettingsChanged;
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Initializes a new instance of BoardManager
        /// </summary>
        /// <param name="mainCanvas">The main canvas to manage boards on</param>
        public BoardManager(Canvas mainCanvas)
        {
            _mainCanvas = mainCanvas ?? throw new ArgumentNullException(nameof(mainCanvas));
            _boards = new List<BoardState>();
            _currentBoardIndex = 0;
            
            // Create initial board
            CreateBoard("Bảng 1");
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Creates a new board with default settings
        /// </summary>
        /// <param name="name">Optional board name (auto-generated if null)</param>
        /// <returns>The created board, or null if max boards reached</returns>
        public BoardState? CreateBoard(string? name = null)
        {
            if (IsMaxBoardsReached)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Cannot create board: Maximum {MAX_BOARDS} boards reached");
                return null;
            }
            
            // Auto-generate name if not provided
            if (string.IsNullOrWhiteSpace(name))
            {
                name = $"Bảng {_boards.Count + 1}";
            }
            
            var board = new BoardState
            {
                Id = Guid.NewGuid(),
                Name = name,
                BackgroundColor = Colors.White,
                CreatedAt = DateTime.Now,
                LastModifiedAt = DateTime.Now,
                IsActive = false
            };
            
            // Try to load thumbnail from file (if exists)
            board.ThumbnailImage = LoadThumbnailFromFile(board) ?? CreateEmptyBoardPlaceholder();
            
            _boards.Add(board);
            
            System.Diagnostics.Debug.WriteLine($"✅ Board created: {board.Name} (Total: {_boards.Count}/{MAX_BOARDS})");
            
            // Fire event
            BoardCreated?.Invoke(this, new BoardEventArgs(board));
            
            return board;
        }
        
        /// <summary>
        /// Duplicates the current board with all its content
        /// </summary>
        /// <returns>The duplicated board, or null if max boards reached</returns>
        public BoardState? DuplicateCurrentBoard()
        {
            if (IsMaxBoardsReached)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Cannot duplicate board: Maximum {MAX_BOARDS} boards reached");
                return null;
            }
            
            // Save current board state first
            SaveCurrentBoardState();
            
            var sourceBoard = CurrentBoard;
            
            // Create new board with duplicated name
            string newName = $"{sourceBoard.Name} (Bản sao)";
            var newBoard = new BoardState
            {
                Id = Guid.NewGuid(),
                Name = newName,
                BackgroundColor = sourceBoard.BackgroundColor,
                BackgroundImagePath = sourceBoard.BackgroundImagePath,
                CreatedAt = DateTime.Now,
                LastModifiedAt = DateTime.Now,
                IsActive = false
            };
            
            // Deep copy all canvas elements
            if (sourceBoard.CanvasElements != null && sourceBoard.CanvasElements.Count > 0)
            {
                newBoard.CanvasElements = new List<UIElement>();
                
                foreach (var element in sourceBoard.CanvasElements)
                {
                    // Clone the element (this is a simplified approach)
                    // For a full implementation, you'd need proper deep cloning
                    try
                    {
                        var clonedElement = CloneElement(element);
                        if (clonedElement != null)
                        {
                            newBoard.CanvasElements.Add(clonedElement);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Failed to clone element: {ex.Message}");
                    }
                }
                
                newBoard.ObjectCount = newBoard.CanvasElements.Count;
            }
            
            // Copy thumbnail
            newBoard.ThumbnailImage = sourceBoard.ThumbnailImage;
            
            _boards.Add(newBoard);
            
            System.Diagnostics.Debug.WriteLine($"✅ Board duplicated: '{sourceBoard.Name}' → '{newName}' ({newBoard.ObjectCount} objects)");
            
            // Fire event
            BoardCreated?.Invoke(this, new BoardEventArgs(newBoard));
            
            return newBoard;
        }
        
        /// <summary>
        /// Clones a UI element (simplified version)
        /// </summary>
        private UIElement? CloneElement(UIElement source)
        {
            // This is a simplified cloning approach
            // For production, you'd need proper serialization/deserialization
            
            try
            {
                string xaml = System.Windows.Markup.XamlWriter.Save(source);
                var cloned = (UIElement)System.Windows.Markup.XamlReader.Parse(xaml);
                return cloned;
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// Deletes a board at the specified index
        /// </summary>
        /// <param name="index">The index of the board to delete</param>
        /// <returns>True if deleted successfully, false otherwise</returns>
        public bool DeleteBoard(int index)
        {
            if (IsMinBoardsReached)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Cannot delete board: Minimum {MIN_BOARDS} board required");
                return false;
            }
            
            if (index < 0 || index >= _boards.Count)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Cannot delete board: Invalid index {index}");
                return false;
            }
            
            var board = _boards[index];
            
            // If deleting current board, switch to another board first
            if (index == _currentBoardIndex)
            {
                // Switch to previous board, or next if deleting first board
                int newIndex = index > 0 ? index - 1 : 0;
                SwitchBoard(newIndex);
            }
            
            _boards.RemoveAt(index);
            
            // Adjust current board index if necessary
            if (_currentBoardIndex >= _boards.Count)
            {
                _currentBoardIndex = _boards.Count - 1;
            }
            
            System.Diagnostics.Debug.WriteLine($"✅ Board deleted: {board.Name} (Remaining: {_boards.Count})");
            
            // Fire event
            BoardDeleted?.Invoke(this, new BoardEventArgs(board));
            
            return true;
        }
        
        /// <summary>
        /// Switches to a different board
        /// </summary>
        /// <param name="index">The index of the board to switch to</param>
        /// <returns>True if switched successfully, false otherwise</returns>
        public bool SwitchBoard(int index)
        {
            if (index < 0 || index >= _boards.Count)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Cannot switch board: Invalid index {index}");
                return false;
            }
            
            if (index == _currentBoardIndex)
            {
                System.Diagnostics.Debug.WriteLine($"ℹ️ Already on board {index}");
                return true;
            }
            
            var oldBoard = _boards[_currentBoardIndex];
            var newBoard = _boards[index];
            
            // ═══ WP6: 7-STEP SAFE PAGE TRANSITION PATTERN (QC_4.2_WP6) ═══
            // Step 1: Save current board state
            SaveCurrentBoardState();
            
            // Step 2: Hide main canvas to prevent flicker & stroke bleed-through
            _mainCanvas.Visibility = Visibility.Hidden;

            try
            {
                // Step 3: Deactivate old board
                oldBoard.IsActive = false;
                
                // Step 4: Switch to new board
                _currentBoardIndex = index;
                newBoard.IsActive = true;
                newBoard.LastModifiedAt = DateTime.Now;
                
                // Step 5: Load new board state (sets background FIRST, then restores elements)
                LoadBoardState(newBoard);

                // Step 6: Force layout update
                _mainCanvas.UpdateLayout();
            }
            finally
            {
                // Step 7: LUÔN hiện lại canvas — kể cả khi có exception (HF-04)
                _mainCanvas.Visibility = Visibility.Visible;
            }
            
            System.Diagnostics.Debug.WriteLine($"✅ Switched from '{oldBoard.Name}' to '{newBoard.Name}'");
            
            // Fire event
            BoardSwitched?.Invoke(this, new BoardSwitchEventArgs(oldBoard, newBoard, index));
            
            return true;
        }
        
        /// <summary>
        /// Sets the background color of the current board
        /// </summary>
        /// <param name="color">The background color</param>
        public void SetBackgroundColor(Color color)
        {
            var board = CurrentBoard;
            board.BackgroundColor = color;
            board.LastModifiedAt = DateTime.Now;
            
            // Apply to canvas
            _mainCanvas.Background = new SolidColorBrush(color);
            
            System.Diagnostics.Debug.WriteLine($"✅ Board background color changed: {color}");
            
            // Fire event
            BoardSettingsChanged?.Invoke(this, new BoardEventArgs(board));
        }
        
        /// <summary>
        /// Sets the background image of the current board
        /// </summary>
        /// <param name="imagePath">Path to the background image</param>
        public void SetBackgroundImage(string imagePath)
        {
            var board = CurrentBoard;
            board.BackgroundImagePath = imagePath;
            board.LastModifiedAt = DateTime.Now;
            
            // TODO: Apply image to canvas
            
            System.Diagnostics.Debug.WriteLine($"✅ Board background image set: {imagePath}");
            
            // Fire event
            BoardSettingsChanged?.Invoke(this, new BoardEventArgs(board));
        }
        
        /// <summary>
        /// Renames the current board
        /// </summary>
        /// <param name="newName">The new board name</param>
        public void RenameCurrentBoard(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                System.Diagnostics.Debug.WriteLine($"❌ Cannot rename board: Invalid name");
                return;
            }
            
            var board = CurrentBoard;
            var oldName = board.Name;
            board.Name = newName;
            board.LastModifiedAt = DateTime.Now;
            
            System.Diagnostics.Debug.WriteLine($"✅ Board renamed: '{oldName}' → '{newName}'");
            
            // Fire event
            BoardSettingsChanged?.Invoke(this, new BoardEventArgs(board));
        }
        
        /// <summary>
        /// Clears all content from the current board
        /// </summary>
        public void ClearCurrentBoard()
        {
            var board = CurrentBoard;
            
            // Clear canvas (except system UI elements)
            var elementsToRemove = new List<UIElement>();
            foreach (UIElement child in _mainCanvas.Children)
            {
                if (IsSystemElement(child)) continue;
                elementsToRemove.Add(child);
            }
            
            foreach (var element in elementsToRemove)
            {
                _mainCanvas.Children.Remove(element);
            }
            
            board.LastModifiedAt = DateTime.Now;
            
            System.Diagnostics.Debug.WriteLine($"✅ Board cleared: {board.Name}");
            
            // Fire event
            BoardSettingsChanged?.Invoke(this, new BoardEventArgs(board));
        }
        
        /// <summary>
        /// Refreshes the thumbnail for a specific board
        /// </summary>
        /// <param name="boardIndex">The index of the board to refresh (0-based)</param>
        public void RefreshBoardThumbnail(int boardIndex)
        {
            if (boardIndex < 0 || boardIndex >= _boards.Count)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Cannot refresh thumbnail: Invalid index {boardIndex}");
                return;
            }
            
            // Switch to the board temporarily to capture its thumbnail
            int originalIndex = _currentBoardIndex;
            bool needsSwitch = boardIndex != _currentBoardIndex;
            
            if (needsSwitch)
            {
                // Temporarily switch to target board
                _currentBoardIndex = boardIndex;
            }
            
            var board = _boards[boardIndex];
            board.ThumbnailImage = CaptureThumbnail();
            SaveThumbnailToFile(board);
            
            if (needsSwitch)
            {
                // Switch back to original board
                _currentBoardIndex = originalIndex;
            }
            
            System.Diagnostics.Debug.WriteLine($"🔄 Refreshed thumbnail for: {board.Name}");
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Saves the current board state (canvas content)
        /// </summary>
        private void SaveCurrentBoardState()
        {
            var board = CurrentBoard;
            
            // Create a list to store all canvas elements
            var elements = new List<UIElement>();
            
            // Copy all children except system UI elements
            foreach (UIElement child in _mainCanvas.Children)
            {
                if (IsSystemElement(child)) continue;
                elements.Add(child);
            }
            
            // Store elements in board state
            board.CanvasElements = elements;
            board.ObjectCount = elements.Count; // Store object count
            board.LastModifiedAt = DateTime.Now;
            
            // Capture thumbnail preview
            board.ThumbnailImage = CaptureThumbnail();
            
            // Save thumbnail to file for persistence
            SaveThumbnailToFile(board);
            
            System.Diagnostics.Debug.WriteLine($"💾 Saved board state: {board.Name} ({elements.Count} elements)");
        }
        
        /// <summary>
        /// Captures a thumbnail preview of the current canvas
        /// HF-05: Ẩn System UI trước khi render để thumbnail không bị "nhiễm" UI overlay
        /// </summary>
        private BitmapSource? CaptureThumbnail()
        {
            try
            {
                bool isEmpty = true;
                foreach (UIElement child in _mainCanvas.Children)
                {
                    if (IsSystemElement(child)) continue;
                    isEmpty = false;
                    break;
                }
                
                // If empty, create placeholder thumbnail
                if (isEmpty)
                {
                    return CreateEmptyBoardPlaceholder();
                }
                
                // HF-05: Tạm ẩn System UI elements trước khi chụp
                var hiddenElements = new System.Collections.Generic.List<(UIElement element, Visibility original)>();
                foreach (UIElement child in _mainCanvas.Children)
                {
                    if (IsSystemElement(child) && child.Visibility == Visibility.Visible)
                    {
                        hiddenElements.Add((child, child.Visibility));
                        child.Visibility = Visibility.Hidden;
                    }
                }

                try
                {
                    // Force re-render với system UI đã ẩn
                    _mainCanvas.UpdateLayout();
                    
                    // Render the main canvas (bây giờ KHÔNG CÓ system UI)
                    var renderBitmap = new RenderTargetBitmap(
                        (int)_mainCanvas.ActualWidth,
                        (int)_mainCanvas.ActualHeight,
                        96, 96,
                        PixelFormats.Pbgra32);
                    
                    renderBitmap.Render(_mainCanvas);
                    
                    // Scale down to thumbnail size (80x60 pixels)
                    var thumbnail = new TransformedBitmap(renderBitmap, new ScaleTransform(
                        80.0 / _mainCanvas.ActualWidth,
                        60.0 / _mainCanvas.ActualHeight));
                    
                    // Freeze for cross-thread access
                    thumbnail.Freeze();
                    
                    System.Diagnostics.Debug.WriteLine($"📸 Captured thumbnail: {thumbnail.PixelWidth}x{thumbnail.PixelHeight}");
                    
                    return thumbnail;
                }
                finally
                {
                    // HF-05: LUÔN khôi phục visibility cho system UI elements
                    foreach (var (element, original) in hiddenElements)
                    {
                        element.Visibility = original;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to capture thumbnail: {ex.Message}");
                return CreateEmptyBoardPlaceholder();
            }
        }
        
        /// <summary>
        /// Creates a placeholder thumbnail for empty boards
        /// </summary>
        private BitmapSource CreateEmptyBoardPlaceholder()
        {
            var drawingVisual = new DrawingVisual();
            using (var context = drawingVisual.RenderOpen())
            {
                // Background
                context.DrawRectangle(new SolidColorBrush(Color.FromRgb(248, 249, 250)), null, new Rect(0, 0, 80, 60));
                
                // Board icon (simplified)
                var iconBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200));
                context.DrawRectangle(null, new Pen(iconBrush, 2), new Rect(25, 15, 30, 30));
                context.DrawLine(new Pen(iconBrush, 1.5), new Point(30, 22), new Point(50, 22));
                context.DrawLine(new Pen(iconBrush, 1.5), new Point(30, 30), new Point(50, 30));
                context.DrawLine(new Pen(iconBrush, 1.5), new Point(30, 38), new Point(45, 38));
            }
            
            var renderBitmap = new RenderTargetBitmap(80, 60, 96, 96, PixelFormats.Pbgra32);
            renderBitmap.Render(drawingVisual);
            renderBitmap.Freeze();
            
            System.Diagnostics.Debug.WriteLine("📋 Created empty board placeholder");
            
            return renderBitmap;
        }
        
        /// <summary>
        /// Saves thumbnail to file for persistence
        /// </summary>
        private void SaveThumbnailToFile(BoardState board)
        {
            if (board.ThumbnailImage == null) return;
            
            try
            {
                // Create thumbnails directory in AppData
                string appDataPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "QASmartTouch",
                    "Thumbnails");
                
                System.IO.Directory.CreateDirectory(appDataPath);
                
                // Save as PNG with board ID as filename
                string filePath = System.IO.Path.Combine(appDataPath, $"{board.Id}.png");
                
                using (var fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(board.ThumbnailImage));
                    encoder.Save(fileStream);
                }
                
                System.Diagnostics.Debug.WriteLine($"💾 Saved thumbnail to file: {filePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to save thumbnail: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Loads thumbnail from file
        /// </summary>
        private BitmapSource? LoadThumbnailFromFile(BoardState board)
        {
            try
            {
                string appDataPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "QASmartTouch",
                    "Thumbnails");
                
                string filePath = System.IO.Path.Combine(appDataPath, $"{board.Id}.png");
                
                if (!System.IO.File.Exists(filePath))
                    return null;
                
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();
                
                System.Diagnostics.Debug.WriteLine($"📂 Loaded thumbnail from file: {filePath}");
                
                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Failed to load thumbnail: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Loads a board state (restores canvas content)
        /// </summary>
        /// <param name="board">The board to load</param>
        private void LoadBoardState(BoardState board)
        {
            // Save system UI elements before clearing (QC_4.2_STATE_GUARD)
            var systemElements = new List<UIElement>();
            foreach (UIElement child in _mainCanvas.Children)
            {
                if (IsSystemElement(child))
                {
                    systemElements.Add(child);
                }
            }
            
            // Clear current canvas
            _mainCanvas.Children.Clear();
            
            // Apply board settings
            _mainCanvas.Background = new SolidColorBrush(board.BackgroundColor);
            
            // Restore all saved elements
            if (board.CanvasElements != null && board.CanvasElements.Count > 0)
            {
                foreach (var element in board.CanvasElements)
                {
                    _mainCanvas.Children.Add(element);
                }
                
                System.Diagnostics.Debug.WriteLine($"📂 Loaded board state: {board.Name} ({board.CanvasElements.Count} elements restored)");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"📂 Loaded board state: {board.Name} (empty board)");
            }
            
            // Restore system UI elements on top
            foreach (var element in systemElements)
            {
                _mainCanvas.Children.Add(element);
            }
        }
        
        #endregion
    }
    
    #region Event Args
    
    /// <summary>
    /// Event arguments for board events
    /// </summary>
    public class BoardEventArgs : EventArgs
    {
        public BoardState Board { get; }
        
        public BoardEventArgs(BoardState board)
        {
            Board = board;
        }
    }
    
    /// <summary>
    /// Event arguments for board switch events
    /// </summary>
    public class BoardSwitchEventArgs : EventArgs
    {
        public BoardState OldBoard { get; }
        public BoardState NewBoard { get; }
        public int NewBoardIndex { get; }
        
        public BoardSwitchEventArgs(BoardState oldBoard, BoardState newBoard, int newBoardIndex)
        {
            OldBoard = oldBoard;
            NewBoard = newBoard;
            NewBoardIndex = newBoardIndex;
        }
    }
    
    #endregion
    
    #region Board State Model
    
    /// <summary>
    /// Represents the state of a single board
    /// </summary>
    public class BoardState
    {
        /// <summary>
        /// Unique identifier for the board
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// Board name (e.g., "Board 1", "Math Lesson", etc.)
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Background color of the board
        /// </summary>
        public Color BackgroundColor { get; set; }
        
        /// <summary>
        /// Path to background image (if any)
        /// </summary>
        public string? BackgroundImagePath { get; set; }
        
        /// <summary>
        /// Whether this board is currently active
        /// </summary>
        public bool IsActive { get; set; }
        
        /// <summary>
        /// When the board was created
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// When the board was last modified
        /// </summary>
        public DateTime LastModifiedAt { get; set; }
        
        /// <summary>
        /// Serialized canvas content (for save/load)
        /// </summary>
        public string? SerializedContent { get; set; }
        
        /// <summary>
        /// Canvas elements (actual UIElement collection)
        /// </summary>
        public List<UIElement>? CanvasElements { get; set; }
        
        /// <summary>
        /// Thumbnail preview image of the board
        /// </summary>
        public BitmapSource? ThumbnailImage { get; set; }
        
        /// <summary>
        /// Number of objects on this board
        /// </summary>
        public int ObjectCount { get; set; }
    }
    
    #endregion
}
