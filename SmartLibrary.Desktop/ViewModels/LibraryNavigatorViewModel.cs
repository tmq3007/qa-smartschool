using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartLibrary.Desktop.Services;

namespace SmartLibrary.Desktop.ViewModels
{
    public partial class LibraryNavigatorViewModel : ObservableObject
    {
        private readonly ApiService _apiService;
        private readonly DispatcherTimer _simulationTimer;
        private List<PathNode> _calculatedPath = new();
        private int _simulationStepIndex = 0;

        [ObservableProperty]
        private string _targetBookTitle = "Chưa chọn sách";

        [ObservableProperty]
        private string _targetShelfText = "Hãy chọn một cuốn sách từ màn hình Tra cứu";

        [ObservableProperty]
        private int _targetShelfX = 0;

        [ObservableProperty]
        private int _targetShelfY = 0;

        [ObservableProperty]
        private int _targetShelfLevel = 1;

        [ObservableProperty]
        private bool _isSimulating = false;

        [ObservableProperty]
        private string _simulationStatus = "Sẵn sàng";

        public ObservableCollection<GridCellViewModel> GridCells { get; } = new();

        public ObservableCollection<BookComboItem> AvailableBooks { get; } = new();

        [ObservableProperty]
        private BookComboItem? _selectedBook;

        public ICommand StartSimulationCommand { get; }
        public ICommand ResetCommand { get; }

        public LibraryNavigatorViewModel(ApiService apiService)
        {
            _apiService = apiService;
            StartSimulationCommand = new RelayCommand(StartSimulation, () => _calculatedPath.Count > 0 && !IsSimulating);
            ResetCommand = new RelayCommand(ResetGrid);

            // Khởi tạo lưới 12x12
            InitializeGrid();

            // Khởi tạo simulation timer
            _simulationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(250) // Tốc độ di chuyển 250ms/bước
            };
            _simulationTimer.Tick += SimulationTimer_Tick;

            _ = LoadBooksForSelectionAsync();
        }

        private void InitializeGrid()
        {
            GridCells.Clear();
            for (int y = 0; y < 12; y++)
            {
                for (int x = 0; x < 12; x++)
                {
                    bool isObstacle = IsDefaultObstacle(x, y);
                    GridCells.Add(new GridCellViewModel
                    {
                        X = x,
                        Y = y,
                        IsObstacle = isObstacle,
                        CellIcon = isObstacle ? "📚" : (x == 0 && y == 0 ? "🚪" : "")
                    });
                }
            }
            // Đặt điểm bắt đầu
            var startCell = GetCell(0, 0);
            if (startCell != null) startCell.IsStart = true;
        }

        private bool IsDefaultObstacle(int x, int y)
        {
            // Thiết kế các kệ sách song song cách nhau lối đi:
            // Cột dọc chẵn x = 2, 4, 6, 8, 10 chứa sách
            // Các hàng y từ 2 đến 9 chứa sách. 
            // Lối đi Y=0,1 (hành lang cửa ra vào) và Y=10,11 (hành lang phía sau)
            // Lối đi X=0,1,3,5,7,9,11 là các lối dọc đi lại
            if ((x == 2 || x == 4 || x == 6 || x == 8 || x == 10) && (y >= 2 && y <= 9))
            {
                return true;
            }
            return false;
        }

        private GridCellViewModel? GetCell(int x, int y)
        {
            if (x < 0 || x >= 12 || y < 0 || y >= 12) return null;
            return GridCells[y * 12 + x];
        }

        public async Task LoadBooksForSelectionAsync()
        {
            try
            {
                var books = await _apiService.GetAsync<BookDto[]>("/Books");
                AvailableBooks.Clear();
                if (books != null)
                {
                    foreach (var b in books.OrderBy(x => x.Title))
                    {
                        // Lọc các sách giấy (có tọa độ kệ)
                        AvailableBooks.Add(new BookComboItem
                        {
                            Id = b.Id,
                            Title = b.Title,
                            Author = b.Author ?? "Vô danh",
                            ShelfGridX = b.ShelfGridX,
                            ShelfGridY = b.ShelfGridY,
                            ShelfLevel = b.ShelfLevel
                        });
                    }
                }
            }
            catch
            {
                // Fallback dữ liệu giả lập nếu offline
                AvailableBooks.Clear();
                AvailableBooks.Add(new BookComboItem { Id = 1, Title = "Đắc Nhân Tâm", Author = "Dale Carnegie", ShelfGridX = 4, ShelfGridY = 7, ShelfLevel = 2 });
                AvailableBooks.Add(new BookComboItem { Id = 2, Title = "Nhà Giả Kim", Author = "Paulo Coelho", ShelfGridX = 8, ShelfGridY = 4, ShelfLevel = 3 });
                AvailableBooks.Add(new BookComboItem { Id = 3, Title = "Số Đỏ", Author = "Vũ Trọng Phụng", ShelfGridX = 2, ShelfGridY = 5, ShelfLevel = 1 });
                AvailableBooks.Add(new BookComboItem { Id = 4, Title = "Toán Học Cao Cấp", Author = "Nguyễn Đình Trí", ShelfGridX = 10, ShelfGridY = 9, ShelfLevel = 4 });
                AvailableBooks.Add(new BookComboItem { Id = 5, Title = "Lược Sử Thời Gian", Author = "Stephen Hawking", ShelfGridX = 6, ShelfGridY = 3, ShelfLevel = 5 });
            }
        }

        partial void OnSelectedBookChanged(BookComboItem? value)
        {
            if (value != null)
            {
                SetTargetBook(value.Id, value.Title, value.ShelfGridX, value.ShelfGridY, value.ShelfLevel);
            }
        }

        public void SetTargetBook(int id, string title, int shelfX, int shelfY, int level)
        {
            _simulationTimer.Stop();
            IsSimulating = false;

            TargetBookTitle = title;
            TargetShelfX = shelfX;
            TargetShelfY = shelfY;
            TargetShelfLevel = level;
            TargetShelfText = $"Kệ cột {shelfX}, Hàng dọc {shelfY}, Tầng kệ số {level}";

            // Tìm cuốn sách tương ứng trong combo nếu có
            var matched = AvailableBooks.FirstOrDefault(b => b.Id == id);
            if (matched != null && SelectedBook != matched)
            {
                SelectedBook = matched;
            }

            // Tìm đường đi
            CalculatePath(0, 0, shelfX, shelfY);
        }

        private void CalculatePath(int startX, int startY, int targetX, int targetY)
        {
            // Reset grid states
            foreach (var cell in GridCells)
            {
                cell.IsPath = false;
                cell.IsTarget = false;
                cell.IsCurrentAgent = false;
                cell.IsVisited = false;
                cell.CellIcon = cell.IsObstacle ? "📚" : (cell.X == 0 && cell.Y == 0 ? "🚪" : "");
            }

            // Đánh dấu Target
            var targetCell = GetCell(targetX, targetY);
            if (targetCell != null)
            {
                targetCell.IsTarget = true;
                targetCell.CellIcon = "🎯";
            }

            // Thực hiện thuật toán A* tìm đường
            _calculatedPath = RunAStar(startX, startY, targetX, targetY);

            if (_calculatedPath.Count > 0)
            {
                // Tô vẽ đường đi (bỏ điểm đầu 0,0 và điểm cuối target để hiển thị icon gốc)
                for (int i = 1; i < _calculatedPath.Count - 1; i++)
                {
                    var p = _calculatedPath[i];
                    var cell = GetCell(p.X, p.Y);
                    if (cell != null)
                    {
                        cell.IsPath = true;
                        cell.CellIcon = "•";
                    }
                }
                SimulationStatus = $"Đường đi dài {_calculatedPath.Count - 1} bước. Sẵn sàng mô phỏng.";
            }
            else
            {
                SimulationStatus = "Không tìm thấy lối đi đến kệ sách này!";
            }

            ((RelayCommand)StartSimulationCommand).NotifyCanExecuteChanged();
        }

        private List<PathNode> RunAStar(int startX, int startY, int targetX, int targetY)
        {
            var openList = new List<PathNode>();
            var closedList = new HashSet<string>();

            var startNode = new PathNode { X = startX, Y = startY, G = 0, H = GetManhattanDistance(startX, startY, targetX, targetY) };
            openList.Add(startNode);

            while (openList.Count > 0)
            {
                // Lấy nút có F nhỏ nhất
                var current = openList.OrderBy(n => n.F).ThenBy(n => n.H).First();
                openList.Remove(current);
                closedList.Add($"{current.X},{current.Y}");

                // Nếu chạm đích
                if (current.X == targetX && current.Y == targetY)
                {
                    var path = new List<PathNode>();
                    var temp = current;
                    while (temp != null)
                    {
                        path.Add(temp);
                        temp = temp.Parent;
                    }
                    path.Reverse();
                    return path;
                }

                // Xét 4 ô lân cận (lên, xuống, trái, phải)
                int[] dx = { 0, 0, -1, 1 };
                int[] dy = { -1, 1, 0, 0 };

                for (int i = 0; i < 4; i++)
                {
                    int nx = current.X + dx[i];
                    int ny = current.Y + dy[i];

                    // Kiếm tra ngoài biên
                    if (nx < 0 || nx >= 12 || ny < 0 || ny >= 12) continue;

                    string key = $"{nx},{ny}";
                    if (closedList.Contains(key)) continue;

                    // Nếu là chướng ngại vật kệ sách khác (ngoại trừ chính điểm Target)
                    var cell = GetCell(nx, ny);
                    if (cell != null && cell.IsObstacle && !(nx == targetX && ny == targetY))
                    {
                        continue;
                    }

                    int gScore = current.G + 1;
                    var neighbor = openList.FirstOrDefault(n => n.X == nx && n.Y == ny);

                    if (neighbor == null)
                    {
                        var newNode = new PathNode
                        {
                            X = nx,
                            Y = ny,
                            G = gScore,
                            H = GetManhattanDistance(nx, ny, targetX, targetY),
                            Parent = current
                        };
                        openList.Add(newNode);
                    }
                    else if (gScore < neighbor.G)
                    {
                        neighbor.G = gScore;
                        neighbor.Parent = current;
                    }
                }
            }

            return new List<PathNode>(); // Không có đường đi
        }

        private int GetManhattanDistance(int x1, int y1, int x2, int y2)
        {
            return Math.Abs(x1 - x2) + Math.Abs(y1 - y2);
        }

        private void StartSimulation()
        {
            if (_calculatedPath.Count == 0 || IsSimulating) return;

            IsSimulating = true;
            _simulationStepIndex = 0;
            SimulationStatus = "Đang di chuyển...";
            _simulationTimer.Start();
            ((RelayCommand)StartSimulationCommand).NotifyCanExecuteChanged();
        }

        private void SimulationTimer_Tick(object? sender, EventArgs e)
        {
            // Reset ô bước trước đó
            if (_simulationStepIndex > 0)
            {
                var prevNode = _calculatedPath[_simulationStepIndex - 1];
                var prevCell = GetCell(prevNode.X, prevNode.Y);
                if (prevCell != null)
                {
                    prevCell.IsCurrentAgent = false;
                    // Trả lại icon tương ứng
                    if (prevCell.IsStart) prevCell.CellIcon = "🚪";
                    else if (prevCell.IsPath) prevCell.CellIcon = "•";
                    else prevCell.CellIcon = "";
                }
            }

            // Đặt trạng thái ô hiện tại
            if (_simulationStepIndex < _calculatedPath.Count)
            {
                var currNode = _calculatedPath[_simulationStepIndex];
                var currCell = GetCell(currNode.X, currNode.Y);
                if (currCell != null)
                {
                    currCell.IsCurrentAgent = true;
                    currCell.CellIcon = "🚶";
                    if (_simulationStepIndex > 0)
                    {
                        currCell.IsVisited = true;
                    }
                }
                _simulationStepIndex++;
            }
            else
            {
                // Hoàn thành
                _simulationTimer.Stop();
                IsSimulating = false;
                SimulationStatus = "Đã đến nơi! Sách nằm ở tầng kệ số " + TargetShelfLevel;
                System.Media.SystemSounds.Exclamation.Play();
                ((RelayCommand)StartSimulationCommand).NotifyCanExecuteChanged();
            }
        }

        public void StopSimulation()
        {
            _simulationTimer.Stop();
            IsSimulating = false;
            SimulationStatus = "Đã dừng mô phỏng";
        }

        private void ResetGrid()
        {
            _simulationTimer.Stop();
            IsSimulating = false;
            InitializeGrid();
            _calculatedPath.Clear();
            TargetBookTitle = "Chưa chọn sách";
            TargetShelfText = "Hãy chọn sách để tìm đường";
            SelectedBook = null;
            SimulationStatus = "Đã reset bản đồ";
            ((RelayCommand)StartSimulationCommand).NotifyCanExecuteChanged();
        }
    }

    public partial class GridCellViewModel : ObservableObject
    {
        public int X { get; set; }
        public int Y { get; set; }

        [ObservableProperty]
        private bool _isObstacle;

        [ObservableProperty]
        private bool _isStart;

        [ObservableProperty]
        private bool _isTarget;

        [ObservableProperty]
        private bool _isPath;

        [ObservableProperty]
        private bool _isCurrentAgent;

        [ObservableProperty]
        private bool _isVisited;

        [ObservableProperty]
        private string _cellIcon = string.Empty;
    }

    public class BookComboItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public int ShelfGridX { get; set; }
        public int ShelfGridY { get; set; }
        public int ShelfLevel { get; set; }
    }

    public class PathNode
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int G { get; set; }
        public int H { get; set; }
        public int F => G + H;
        public PathNode? Parent { get; set; }
    }
}
