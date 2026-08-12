using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using QASmartClass.Models;
using QASmartClass.Data;

namespace QASmartClass.Admin.Controls
{
    public partial class NetworkTopologyControl : UserControl
    {
        private TopologyData _currentData;
        private bool _isEditMode = false;
        private DispatcherTimer _syncTimer;
        
        // Dragging state
        private bool _isDragging = false;
        private UIElement _draggedElement = null;
        private Point _clickPosition;
        private Point _elementStartPosition;
        
        // Brushes
        private readonly SolidColorBrush _teacherBrush = new SolidColorBrush(Color.FromRgb(33, 150, 243)); // #2196F3
        private readonly SolidColorBrush _onlineBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80));  // #4CAF50
        private readonly SolidColorBrush _offlineBrush = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // #F44336
        private readonly SolidColorBrush _warningBrush = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // #FF9800
        private readonly SolidColorBrush _lineOnlineBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80)); 
        private readonly SolidColorBrush _lineOfflineBrush = new SolidColorBrush(Color.FromRgb(148, 163, 184)); // #94A3B8

        public NetworkTopologyControl()
        {
            InitializeComponent();
            LoadRooms();
            
            // Sync timer every 5s
            _syncTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _syncTimer.Tick += SyncTimer_Tick;
            _syncTimer.Start();

            Unloaded += (s, e) => {
                _syncTimer?.Stop();
                _syncTimer = null;
            };
        }

        private void LoadRooms()
        {
            cboRooms.Items.Add("LAB01");
            cboRooms.Items.Add("LAB02");
            cboRooms.Items.Add("LAB03");
            cboRooms.SelectedIndex = 0;
        }

        private void cboRooms_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboRooms.SelectedItem != null)
            {
                string roomId = cboRooms.SelectedItem.ToString();
                LoadTopology(roomId);
            }
        }

        private void LoadTopology(string roomId)
        {
            string path = Services.AppPaths.GetTopologyFile(roomId);
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    _currentData = JsonSerializer.Deserialize<TopologyData>(json);
                }
                catch { _currentData = null; }
            }

            if (_currentData == null || _currentData.Nodes.Count == 0)
            {
                GenerateDefaultTopology(roomId);
            }
            else
            {
                bool seeded = false;
                if (!_currentData.Nodes.Any(n => n.NodeType == "InteractiveBoard"))
                {
                    _currentData.Nodes.Add(new TopologyNode { MachineId = "IOT-IB", DisplayName = "Bảng tương tác", NodeType = "InteractiveBoard", Status = "Online", X = 200, Y = 50 });
                    seeded = true;
                }
                if (!_currentData.Nodes.Any(n => n.NodeType == "Camera"))
                {
                    _currentData.Nodes.Add(new TopologyNode { MachineId = "IOT-CAM", DisplayName = "Camera AI", NodeType = "Camera", Status = "Online", X = 600, Y = 50 });
                    seeded = true;
                }
                if (!_currentData.Nodes.Any(n => n.NodeType == "Kiosk"))
                {
                    _currentData.Nodes.Add(new TopologyNode { MachineId = "IOT-KSK", DisplayName = "Kiosk học sinh", NodeType = "Kiosk", Status = "Online", X = 50, Y = 50 });
                    seeded = true;
                }
                if (!_currentData.Nodes.Any(n => n.NodeType == "Printer"))
                {
                    _currentData.Nodes.Add(new TopologyNode { MachineId = "IOT-PRN", DisplayName = "Máy in mã vạch", NodeType = "Printer", Status = "Online", X = 750, Y = 50 });
                    seeded = true;
                }
                if (seeded)
                {
                    SaveTopology();
                }
            }

            RenderTopology();
        }

        private void GenerateDefaultTopology(string roomId)
        {
            _currentData = new TopologyData { RoomId = roomId };
            
            // Teacher node
            _currentData.Nodes.Add(new TopologyNode 
            { 
                MachineId = "TEACHER", DisplayName = "Giáo viên", NodeType = "Teacher", Status = "Online", X = 400, Y = 50 
            });

            // 4 IoT devices
            _currentData.Nodes.Add(new TopologyNode { MachineId = "IOT-IB", DisplayName = "Bảng tương tác", NodeType = "InteractiveBoard", Status = "Online", X = 200, Y = 50 });
            _currentData.Nodes.Add(new TopologyNode { MachineId = "IOT-CAM", DisplayName = "Camera AI", NodeType = "Camera", Status = "Online", X = 600, Y = 50 });
            _currentData.Nodes.Add(new TopologyNode { MachineId = "IOT-KSK", DisplayName = "Kiosk học sinh", NodeType = "Kiosk", Status = "Online", X = 50, Y = 50 });
            _currentData.Nodes.Add(new TopologyNode { MachineId = "IOT-PRN", DisplayName = "Máy in mã vạch", NodeType = "Printer", Status = "Online", X = 750, Y = 50 });

            // 20 Student nodes
            int cols = 5;
            for (int i = 0; i < 20; i++)
            {
                int row = i / cols;
                int col = i % cols;
                _currentData.Nodes.Add(new TopologyNode
                {
                    MachineId = $"PC-{i+1:D2}",
                    DisplayName = $"Máy {i+1}",
                    NodeType = "Student",
                    Status = "Offline",
                    X = 100 + col * 150,
                    Y = 200 + row * 100
                });
            }
            
            // Automatically save generated topology
            SaveTopology();
        }

        private void SaveTopology()
        {
            if (_currentData == null) return;
            try
            {
                string path = Services.AppPaths.GetTopologyFile(_currentData.RoomId);
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_currentData, options);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi lưu sơ đồ: " + ex.Message);
            }
        }

        private void RenderTopology()
        {
            DrawCanvas.Children.Clear();
            if (_currentData == null) return;

            var teacher = _currentData.Nodes.FirstOrDefault(n => n.NodeType == "Teacher");

            // Draw lines first so they are behind nodes
            if (teacher != null)
            {
                foreach (var student in _currentData.Nodes.Where(n => n.NodeType == "Student"))
                {
                    var line = new Line
                    {
                        X1 = teacher.X + 40, // center of teacher (80/2)
                        Y1 = teacher.Y + 40,
                        X2 = student.X + 25, // center of student (50/2)
                        Y2 = student.Y + 25,
                        StrokeThickness = 2,
                        Stroke = student.Status == "Online" ? _lineOnlineBrush : _lineOfflineBrush,
                        StrokeDashArray = student.Status == "Offline" ? new DoubleCollection { 4, 4 } : null
                    };
                    
                    // Store reference to nodes for updating during drag
                    line.Tag = new Tuple<TopologyNode, TopologyNode>(teacher, student);
                    DrawCanvas.Children.Add(line);
                }
            }

            // Draw nodes
            foreach (var node in _currentData.Nodes)
            {
                bool isTeacher = node.NodeType == "Teacher";
                double size = isTeacher ? 80 : 50;
                
                var border = new Border
                {
                    Width = size,
                    Height = size,
                    CornerRadius = new CornerRadius(size / 2),
                    Background = isTeacher ? _teacherBrush : GetStatusBrush(node.Status),
                    BorderBrush = node.IsSelected ? Brushes.White : Brushes.Transparent,
                    BorderThickness = new Thickness(node.IsSelected ? 3 : 0),
                    ToolTip = node.NodeType switch
                    {
                        "Teacher" => $"{node.DisplayName}\nStatus: {node.Status}",
                        "Student" => $"{node.DisplayName}\nIP: 192.168.1.x\nStatus: {node.Status}",
                        _ => $"Thiết bị IoT: {node.DisplayName}\nStatus: {node.Status}"
                    },
                    Tag = node, // bind data
                    Cursor = _isEditMode ? Cursors.Hand : Cursors.Arrow
                };

                var text = new TextBlock
                {
                    Text = node.NodeType switch
                    {
                        "Teacher" => "GV",
                        "InteractiveBoard" => "IB",
                        "Camera" => "CAM",
                        "Kiosk" => "KSK",
                        "Printer" => "PRN",
                        _ => node.MachineId.Replace("PC-", "")
                    },
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = isTeacher ? 20 : 12
                };

                border.Child = text;

                Canvas.SetLeft(border, node.X);
                Canvas.SetTop(border, node.Y);

                // Event handlers
                border.MouseLeftButtonDown += Node_MouseLeftButtonDown;
                
                DrawCanvas.Children.Add(border);
            }
        }

        private SolidColorBrush GetStatusBrush(string status)
        {
            return status switch
            {
                "Online" => _onlineBrush,
                "Warning" => _warningBrush,
                _ => _offlineBrush
            };
        }

        #region Interactions
        
        private void Node_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var element = sender as FrameworkElement;
            var node = element?.Tag as TopologyNode;
            
            if (node == null) return;

            // Show Info
            ShowNodeInfo(node);

            // Dragging logic
            if (_isEditMode)
            {
                _isDragging = true;
                _draggedElement = element;
                _clickPosition = e.GetPosition(DrawCanvas);
                _elementStartPosition = new Point(Canvas.GetLeft(element), Canvas.GetTop(element));
                element.CaptureMouse();
                
                // Select
                foreach (var n in _currentData.Nodes) n.IsSelected = false;
                node.IsSelected = true;
                RenderTopology(); // Re-render to show selection border
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _draggedElement != null)
            {
                var currentPosition = e.GetPosition(DrawCanvas);
                var offsetX = currentPosition.X - _clickPosition.X;
                var offsetY = currentPosition.Y - _clickPosition.Y;

                double newX = _elementStartPosition.X + offsetX;
                double newY = _elementStartPosition.Y + offsetY;

                // Restrict to canvas bounds
                newX = Math.Max(0, Math.Min(newX, DrawCanvas.Width - ((FrameworkElement)_draggedElement).Width));
                newY = Math.Max(0, Math.Min(newY, DrawCanvas.Height - ((FrameworkElement)_draggedElement).Height));

                Canvas.SetLeft(_draggedElement, newX);
                Canvas.SetTop(_draggedElement, newY);

                // Update data model
                if (((FrameworkElement)_draggedElement).Tag is TopologyNode node)
                {
                    node.X = newX;
                    node.Y = newY;
                    UpdateLinesForNode(node);
                }
            }
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging && _draggedElement != null)
            {
                _isDragging = false;
                _draggedElement.ReleaseMouseCapture();
                _draggedElement = null;
            }
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Click on empty canvas deselects
            if (e.OriginalSource == DrawCanvas)
            {
                if (_currentData != null)
                {
                    foreach (var n in _currentData.Nodes) n.IsSelected = false;
                    RenderTopology();
                }
                panelInfo.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateLinesForNode(TopologyNode movedNode)
        {
            double halfNodeSize = movedNode.NodeType == "Teacher" ? 40 : 25;
            double centerX = movedNode.X + halfNodeSize;
            double centerY = movedNode.Y + halfNodeSize;

            foreach (var child in DrawCanvas.Children)
            {
                if (child is Line line && line.Tag is Tuple<TopologyNode, TopologyNode> pair)
                {
                    if (pair.Item1 == movedNode)
                    {
                        line.X1 = centerX;
                        line.Y1 = centerY;
                    }
                    else if (pair.Item2 == movedNode)
                    {
                        line.X2 = centerX;
                        line.Y2 = centerY;
                    }
                }
            }
        }

        #endregion

        #region Info Panel
        
        private void ShowNodeInfo(TopologyNode node)
        {
            panelInfo.Visibility = Visibility.Visible;
            infoStatusColor.Background = GetStatusBrush(node.Status);
            txtInfoName.Text = node.DisplayName;
            txtInfoStatus.Text = node.Status == "Online" ? "Đang Online" : node.Status == "Warning" ? "Cảnh báo" : "Mất kết nối";
            txtInfoStatus.Foreground = GetStatusBrush(node.Status);
            txtInfoIp.Text = "192.168.x.x"; // Mock IP
            txtInfoStudent.Text = node.NodeType switch
            {
                "Teacher" => "Giáo viên",
                "Student" => "Học sinh",
                "InteractiveBoard" => "Bảng tương tác (IoT)",
                "Camera" => "Camera AI (IoT)",
                "Kiosk" => "Kiosk học sinh (IoT)",
                "Printer" => "Máy in mã vạch (IoT)",
                _ => "Thiết bị ngoại vi"
            };

            if (node.Status != "Online")
            {
                panelTroubleshoot.Visibility = Visibility.Visible;
                txtTroubleshootGuide.Text = node.NodeType switch
                {
                    "InteractiveBoard" => "1. Kiểm tra cáp HDMI/VGA kết nối từ PC đến bảng.\n2. Xác minh nguồn điện của bảng tương tác đã được bật.\n3. Reset thiết bị bằng cách rút điện nguồn, đợi 10 giây và cắm lại.",
                    "Camera" => "1. Kiểm tra đầu cấp nguồn POE trên Switch trung tâm.\n2. Xác minh đèn trạng thái LED phía sau camera đang nhấp nháy xanh.\n3. Reset địa chỉ IP của camera qua bảng điều khiển ping.",
                    "Kiosk" => "1. Đảm bảo Kiosk đã được bật nguồn và ứng dụng Client đang chạy.\n2. Kiểm tra kết nối Wifi/Ethernet của thiết bị Kiosk.\n3. Khởi động lại thiết bị bằng nút cứng phía sau màn hình.",
                    "Printer" => "1. Kiểm tra xem khay giấy in/mực in có bị kẹt hoặc hết hay không.\n2. Xác minh cáp USB/mạng kết nối với máy in đang hoạt động.\n3. Tắt máy in, đợi 5 giây, bật lại để xóa hàng đợi lệnh in.",
                    _ => "1. Kiểm tra xem máy trạm hoặc thiết bị đã bật nguồn hay chưa.\n2. Đảm bảo cáp mạng LAN phía sau thiết bị đã cắm chặt.\n3. Kiểm tra kết nối mạng và thử ping lại địa chỉ IP."
                };
            }
            else
            {
                panelTroubleshoot.Visibility = Visibility.Collapsed;
            }
        }
        
        #endregion

        #region Toolbar Actions

        private void btnEditMode_Click(object sender, RoutedEventArgs e)
        {
            _isEditMode = btnEditMode.IsChecked == true;
            panelEditTools.Visibility = _isEditMode ? Visibility.Visible : Visibility.Collapsed;
            
            // Clear selection when exiting edit mode
            if (!_isEditMode && _currentData != null)
            {
                foreach (var n in _currentData.Nodes) n.IsSelected = false;
            }
            RenderTopology();
        }

        private void btnAutoLayout_Click(object sender, RoutedEventArgs e)
        {
            if (_currentData == null) return;
            
            // Re-arrange students
            var students = _currentData.Nodes.Where(n => n.NodeType == "Student").ToList();
            int cols = 5;
            for (int i = 0; i < students.Count; i++)
            {
                int row = i / cols;
                int col = i % cols;
                students[i].X = 100 + col * 150;
                students[i].Y = 200 + row * 100;
            }
            
            // Reset teacher to center top
            var teacher = _currentData.Nodes.FirstOrDefault(n => n.NodeType == "Teacher");
            if (teacher != null)
            {
                teacher.X = 100 + (cols * 150) / 2.0 - 40;
                teacher.Y = 50;
            }
            
            RenderTopology();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveTopology();
            MessageBox.Show("Đã lưu sơ đồ mạng thành công!", "Lưu thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            
            btnEditMode.IsChecked = false;
            btnEditMode_Click(null, null);
        }

        private void btnExportPng_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Simple export to MyDocuments
                string exportPath = System.IO.Path.Combine(Services.AppPaths.DocumentsDir, $"Topology_{_currentData.RoomId}_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                
                // Deselect before rendering
                foreach (var n in _currentData.Nodes) n.IsSelected = false;
                RenderTopology();
                
                // Measure and arrange canvas
                Size size = new Size(DrawCanvas.ActualWidth > 0 ? DrawCanvas.ActualWidth : 1200, 
                                     DrawCanvas.ActualHeight > 0 ? DrawCanvas.ActualHeight : 800);
                DrawCanvas.Measure(size);
                DrawCanvas.Arrange(new Rect(size));
                
                RenderTargetBitmap rtb = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96d, 96d, PixelFormats.Pbgra32);
                rtb.Render(DrawCanvas);
                
                PngBitmapEncoder png = new PngBitmapEncoder();
                png.Frames.Add(BitmapFrame.Create(rtb));
                
                using (Stream stm = File.Create(exportPath))
                {
                    png.Save(stm);
                }
                
                MessageBox.Show($"Đã xuất sơ đồ ra file PNG:\n{exportPath}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất PNG: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        #endregion

        #region Sync Timer
        
        private void SyncTimer_Tick(object sender, EventArgs e)
        {
            if (_currentData == null || _isDragging) return;
            
            try
            {
                using var db = new AppDbContext();
                // Đồng bộ tần suất quét từ cấu hình Master
                var scanIntervalSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Network_ScanIntervalSeconds");
                int seconds = 30;
                if (scanIntervalSetting != null && int.TryParse(scanIntervalSetting.Value, out int parsed))
                {
                    seconds = parsed;
                }
                if (_syncTimer != null && _syncTimer.Interval.TotalSeconds != seconds)
                {
                    _syncTimer.Interval = TimeSpan.FromSeconds(seconds);
                }

                // Đọc chế độ đồng bộ từ Master Settings
                var modeSetting = db.SystemSettings.Find("TopologyMode");
                string syncMode = modeSetting?.Value ?? "FileHeartbeat";
                bool changed = false;

                if (syncMode == "SimulationDemo")
                {
                    // 1. Chế độ DEMO: Trạng thái máy trạm và IoT hiển thị online ổn định
                    foreach (var student in _currentData.Nodes.Where(n => n.NodeType == "Student"))
                    {
                        if (student.Status != "Online")
                        {
                            student.Status = "Online";
                            changed = true;
                        }
                    }
                    foreach (var iotNode in _currentData.Nodes.Where(n => n.NodeType != "Teacher" && n.NodeType != "Student"))
                    {
                        if (iotNode.Status != "Online")
                        {
                            iotNode.Status = "Online";
                            changed = true;
                        }
                    }
                }
                else if (syncMode == "ActiveScan")
                {
                    // 2. Chế độ Active Scan: Thực hiện ping nhanh đến từng máy trong LAN
                    foreach (var student in _currentData.Nodes.Where(n => n.NodeType == "Student"))
                    {
                        string oldStatus = student.Status;
                        bool isAlive = QuickPing(student.MachineId); 
                        student.Status = isAlive ? "Online" : "Offline";
                        if (oldStatus != student.Status) changed = true;
                    }
                    foreach (var iotNode in _currentData.Nodes.Where(n => n.NodeType != "Teacher" && n.NodeType != "Student"))
                    {
                        string oldStatus = iotNode.Status;
                        // Simulating IoT device health ping (assume online for mock IOT prefix)
                        bool isAlive = QuickPing(iotNode.MachineId) || iotNode.MachineId.StartsWith("IOT");
                        iotNode.Status = isAlive ? "Online" : "Offline";
                        if (oldStatus != iotNode.Status) changed = true;
                    }
                }
                else
                {
                    // 3. Chế độ File Heartbeat: Đọc file Json heartbeat từ SharedFiles/
                    foreach (var student in _currentData.Nodes.Where(n => n.NodeType == "Student"))
                    {
                        string oldStatus = student.Status;
                        bool isAlive = CheckHeartbeatFile(student.MachineId);
                        student.Status = isAlive ? "Online" : "Offline";
                        if (oldStatus != student.Status) changed = true;
                    }
                    foreach (var iotNode in _currentData.Nodes.Where(n => n.NodeType != "Teacher" && n.NodeType != "Student"))
                    {
                        string oldStatus = iotNode.Status;
                        bool isAlive = CheckHeartbeatFile(iotNode.MachineId) || iotNode.MachineId.StartsWith("IOT");
                        iotNode.Status = isAlive ? "Online" : "Offline";
                        if (oldStatus != iotNode.Status) changed = true;
                    }
                }

                if (changed)
                {
                    RenderTopology();
                    
                    // If selected node status changed, refresh info panel
                    var selectedNode = _currentData.Nodes.FirstOrDefault(n => n.IsSelected);
                    if (selectedNode != null)
                    {
                        ShowNodeInfo(selectedNode);
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Lỗi khi đồng bộ trạng thái Topology.");
            }
        }

        private bool QuickPing(string target)
        {
            if (string.IsNullOrWhiteSpace(target)) return false;
            try
            {
                using var ping = new System.Net.NetworkInformation.Ping();
                // Ping nhanh timeout 500ms để tránh giật lag UI và nghẽn mạng trạm
                var reply = ping.Send(target, 500); 
                return reply.Status == System.Net.NetworkInformation.IPStatus.Success;
            }
            catch { return false; }
        }

        private bool CheckHeartbeatFile(string machineId)
        {
            try
            {
                using var db = new AppDbContext();
                var folderSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "SharedFolderPath");
                string sharedDir = folderSetting != null && !string.IsNullOrEmpty(folderSetting.Value)
                    ? folderSetting.Value
                    : System.IO.Path.Combine(Services.AppPaths.RootDir, "SharedFiles");

                string hbPath = System.IO.Path.Combine(sharedDir, $"heartbeat_{machineId}.json");
                if (File.Exists(hbPath))
                {
                    var fileInfo = new FileInfo(hbPath);
                    // Online nếu cập nhật trong vòng 10 phút
                    return (DateTime.Now - fileInfo.LastWriteTime).TotalMinutes < 10;
                }
            }
            catch { }
            return false;
        }
        
        #endregion
    }
}
