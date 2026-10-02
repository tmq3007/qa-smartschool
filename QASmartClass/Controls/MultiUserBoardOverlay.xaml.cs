using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace QASmartTouch.Controls
{
    public class MultiUserSessionEventArgs : EventArgs
    {
        public bool SaveAsNewBoard { get; set; }
        public List<Polyline> LeftPolylines { get; set; } = new();
        public List<Polyline> RightPolylines { get; set; } = new();
        public string Student1Name { get; set; } = "";
        public Color Student1Color { get; set; }
        public string Student2Name { get; set; } = "";
        public Color Student2Color { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public double ActualAreaWidth { get; set; }
        public double ActualAreaHeight { get; set; }
    }

    public partial class MultiUserBoardOverlay : UserControl
    {
        private readonly DispatcherTimer _timer;
        private DateTime _sessionStartTime;
        private TimeSpan _elapsedTime;

        // Colors
        private Color _currentLeftColor = Color.FromRgb(46, 134, 222);   // #2E86DE (Blue)
        private Color _currentRightColor = Color.FromRgb(238, 82, 83);   // #EE5253 (Red)

        // Tool Modes: false = Pen, true = Eraser
        private bool _isLeftEraser = false;
        private bool _isRightEraser = false;

        // Concurrent Touch Tracking Dictionaries (Mapping TouchDevice.Id -> Active Polyline)
        private readonly Dictionary<int, Polyline> _leftActiveTouchStrokes = new();
        private readonly Dictionary<int, Polyline> _rightActiveTouchStrokes = new();

        // Mouse Fallback Tracking
        private Polyline? _leftActiveMouseStroke;
        private Polyline? _rightActiveMouseStroke;

        // Undo History Stacks
        private readonly List<Polyline> _leftHistory = new();
        private readonly List<Polyline> _rightHistory = new();

        public event EventHandler<MultiUserSessionEventArgs>? SessionCompleted;

        public MultiUserBoardOverlay()
        {
            InitializeComponent();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;

            // ✅ CRITICAL: Vô hiệu hóa độ trễ và vòng tròn giữ chuột phải của Windows Stylus
            DisableStylusDelays(canvasLeft);
            DisableStylusDelays(canvasRight);
        }

        private void DisableStylusDelays(UIElement element)
        {
            Stylus.SetIsPressAndHoldEnabled(element, false);
            Stylus.SetIsTapFeedbackEnabled(element, false);
            Stylus.SetIsTouchFeedbackEnabled(element, false);
        }

        public void StartSession(string student1Name, Color student1Color, string student2Name, Color student2Color, Brush? boardBackground = null)
        {
            // Reset state
            canvasLeft.Children.Clear();
            canvasRight.Children.Clear();
            _leftActiveTouchStrokes.Clear();
            _rightActiveTouchStrokes.Clear();
            _leftActiveMouseStroke = null;
            _rightActiveMouseStroke = null;
            _leftHistory.Clear();
            _rightHistory.Clear();

            _isLeftEraser = false;
            _isRightEraser = false;

            // Set background matching current lecture board
            if (boardBackground != null)
            {
                rootGrid.Background = boardBackground;
            }
            else
            {
                rootGrid.Background = new SolidColorBrush(Color.FromRgb(0x3D, 0x6D, 0x64));
            }

            // Student 1 Configuration
            lblStudent1.Text = string.IsNullOrWhiteSpace(student1Name) ? "Học sinh 1" : student1Name;
            _currentLeftColor = student1Color;
            dotStudent1.Fill = new SolidColorBrush(student1Color);
            btnLeftPen.Background = new SolidColorBrush(student1Color);
            btnLeftEraser.Background = new SolidColorBrush(Color.FromRgb(47, 53, 66));

            // Student 2 Configuration
            lblStudent2.Text = string.IsNullOrWhiteSpace(student2Name) ? "Học sinh 2" : student2Name;
            _currentRightColor = student2Color;
            dotStudent2.Fill = new SolidColorBrush(student2Color);
            btnRightPen.Background = new SolidColorBrush(student2Color);
            btnRightEraser.Background = new SolidColorBrush(Color.FromRgb(47, 53, 66));

            // Reset & Start Timer
            _sessionStartTime = DateTime.UtcNow;
            _elapsedTime = TimeSpan.Zero;
            txtTimer.Text = "00:00";
            _timer.Start();

            // Hide confirmation overlay
            confirmDialogOverlay.Visibility = Visibility.Collapsed;

            System.Diagnostics.Debug.WriteLine($"✅ MultiUserBoardOverlay started: {lblStudent1.Text} vs {lblStudent2.Text}");
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _elapsedTime = DateTime.UtcNow - _sessionStartTime;
            txtTimer.Text = $"{_elapsedTime.Minutes:D2}:{_elapsedTime.Seconds:D2}";
        }

        private Polyline CreateNewStroke(Color color)
        {
            return new Polyline
            {
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 4.5,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                SnapsToDevicePixels = true
            };
        }

        /// <summary>
        /// Giới hạn tọa độ vẽ trong biên vùng canvas, ngăn nét vẽ tràn sang phần người khác
        /// khi mouse/touch bị capture và kéo ra ngoài vùng cho phép.
        /// </summary>
        private Point ClampPoint(Point pt, Canvas canvas)
        {
            double maxX = canvas.ActualWidth > 0 ? canvas.ActualWidth : double.MaxValue;
            double maxY = canvas.ActualHeight > 0 ? canvas.ActualHeight : double.MaxValue;
            return new Point(
                Math.Max(0, Math.Min(pt.X, maxX)),
                Math.Max(0, Math.Min(pt.Y, maxY))
            );
        }

        #region LEFT ZONE (STUDENT 1) TOUCH & MOUSE EVENTS

        private void CanvasLeft_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            Point pos = ClampPoint(e.GetTouchPoint(canvasLeft).Position, canvasLeft);
            int touchId = e.TouchDevice.Id;

            if (_isLeftEraser)
            {
                EraseAtPoint(canvasLeft, _leftHistory, pos);
            }
            else
            {
                var stroke = CreateNewStroke(_currentLeftColor);
                stroke.Points.Add(pos);
                stroke.Points.Add(new Point(pos.X + 0.01, pos.Y)); // Instant visual dot

                canvasLeft.Children.Add(stroke);
                _leftActiveTouchStrokes[touchId] = stroke;
                _leftHistory.Add(stroke);
            }

            canvasLeft.CaptureTouch(e.TouchDevice);
            e.Handled = true;
        }

        private void CanvasLeft_PreviewTouchMove(object sender, TouchEventArgs e)
        {
            Point pos = ClampPoint(e.GetTouchPoint(canvasLeft).Position, canvasLeft);
            int touchId = e.TouchDevice.Id;

            if (_isLeftEraser)
            {
                EraseAtPoint(canvasLeft, _leftHistory, pos);
            }
            else if (_leftActiveTouchStrokes.TryGetValue(touchId, out var stroke))
            {
                if (stroke.Points.Count > 0)
                {
                    Point lastPt = stroke.Points[^1];
                    double dist = (pos - lastPt).Length;
                    if (dist >= 1.5)
                    {
                        if (stroke.Points.Count == 2 && Math.Abs(stroke.Points[1].X - stroke.Points[0].X) < 0.05)
                        {
                            stroke.Points[1] = pos;
                        }
                        else
                        {
                            stroke.Points.Add(pos);
                        }
                    }
                }
            }

            e.Handled = true;
        }

        private void CanvasLeft_PreviewTouchUp(object sender, TouchEventArgs e)
        {
            int touchId = e.TouchDevice.Id;
            _leftActiveTouchStrokes.Remove(touchId);
            if (e.TouchDevice.Captured == canvasLeft)
            {
                canvasLeft.ReleaseTouchCapture(e.TouchDevice);
            }
            e.Handled = true;
        }

        private void CanvasLeft_LostTouchCapture(object sender, TouchEventArgs e)
        {
            _leftActiveTouchStrokes.Remove(e.TouchDevice.Id);
        }

        // Mouse Support for Left Canvas
        private void CanvasLeft_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.StylusDevice != null) return; // Chặn Touch promoted
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point pos = ClampPoint(e.GetPosition(canvasLeft), canvasLeft);
                if (_isLeftEraser)
                {
                    EraseAtPoint(canvasLeft, _leftHistory, pos);
                }
                else
                {
                    _leftActiveMouseStroke = CreateNewStroke(_currentLeftColor);
                    _leftActiveMouseStroke.Points.Add(pos);
                    _leftActiveMouseStroke.Points.Add(new Point(pos.X + 0.01, pos.Y));
                    canvasLeft.Children.Add(_leftActiveMouseStroke);
                    _leftHistory.Add(_leftActiveMouseStroke);
                    canvasLeft.CaptureMouse();
                }
            }
        }

        private void CanvasLeft_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.StylusDevice != null) return;
            Point pos = ClampPoint(e.GetPosition(canvasLeft), canvasLeft);

            if (_isLeftEraser && e.LeftButton == MouseButtonState.Pressed)
            {
                EraseAtPoint(canvasLeft, _leftHistory, pos);
            }
            else if (_leftActiveMouseStroke != null && e.LeftButton == MouseButtonState.Pressed)
            {
                _leftActiveMouseStroke.Points.Add(pos);
            }
        }

        private void CanvasLeft_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _leftActiveMouseStroke = null;
            if (canvasLeft.IsMouseCaptured)
            {
                canvasLeft.ReleaseMouseCapture();
            }
        }

        #endregion

        #region RIGHT ZONE (STUDENT 2) TOUCH & MOUSE EVENTS

        private void CanvasRight_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            Point pos = ClampPoint(e.GetTouchPoint(canvasRight).Position, canvasRight);
            int touchId = e.TouchDevice.Id;

            if (_isRightEraser)
            {
                EraseAtPoint(canvasRight, _rightHistory, pos);
            }
            else
            {
                var stroke = CreateNewStroke(_currentRightColor);
                stroke.Points.Add(pos);
                stroke.Points.Add(new Point(pos.X + 0.01, pos.Y));

                canvasRight.Children.Add(stroke);
                _rightActiveTouchStrokes[touchId] = stroke;
                _rightHistory.Add(stroke);
            }

            canvasRight.CaptureTouch(e.TouchDevice);
            e.Handled = true;
        }

        private void CanvasRight_PreviewTouchMove(object sender, TouchEventArgs e)
        {
            Point pos = ClampPoint(e.GetTouchPoint(canvasRight).Position, canvasRight);
            int touchId = e.TouchDevice.Id;

            if (_isRightEraser)
            {
                EraseAtPoint(canvasRight, _rightHistory, pos);
            }
            else if (_rightActiveTouchStrokes.TryGetValue(touchId, out var stroke))
            {
                if (stroke.Points.Count > 0)
                {
                    Point lastPt = stroke.Points[^1];
                    double dist = (pos - lastPt).Length;
                    if (dist >= 1.5)
                    {
                        if (stroke.Points.Count == 2 && Math.Abs(stroke.Points[1].X - stroke.Points[0].X) < 0.05)
                        {
                            stroke.Points[1] = pos;
                        }
                        else
                        {
                            stroke.Points.Add(pos);
                        }
                    }
                }
            }

            e.Handled = true;
        }

        private void CanvasRight_PreviewTouchUp(object sender, TouchEventArgs e)
        {
            int touchId = e.TouchDevice.Id;
            _rightActiveTouchStrokes.Remove(touchId);
            if (e.TouchDevice.Captured == canvasRight)
            {
                canvasRight.ReleaseTouchCapture(e.TouchDevice);
            }
            e.Handled = true;
        }

        private void CanvasRight_LostTouchCapture(object sender, TouchEventArgs e)
        {
            _rightActiveTouchStrokes.Remove(e.TouchDevice.Id);
        }

        // Mouse Support for Right Canvas
        private void CanvasRight_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.StylusDevice != null) return;
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point pos = ClampPoint(e.GetPosition(canvasRight), canvasRight);
                if (_isRightEraser)
                {
                    EraseAtPoint(canvasRight, _rightHistory, pos);
                }
                else
                {
                    _rightActiveMouseStroke = CreateNewStroke(_currentRightColor);
                    _rightActiveMouseStroke.Points.Add(pos);
                    _rightActiveMouseStroke.Points.Add(new Point(pos.X + 0.01, pos.Y));
                    canvasRight.Children.Add(_rightActiveMouseStroke);
                    _rightHistory.Add(_rightActiveMouseStroke);
                    canvasRight.CaptureMouse();
                }
            }
        }

        private void CanvasRight_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.StylusDevice != null) return;
            Point pos = ClampPoint(e.GetPosition(canvasRight), canvasRight);

            if (_isRightEraser && e.LeftButton == MouseButtonState.Pressed)
            {
                EraseAtPoint(canvasRight, _rightHistory, pos);
            }
            else if (_rightActiveMouseStroke != null && e.LeftButton == MouseButtonState.Pressed)
            {
                _rightActiveMouseStroke.Points.Add(pos);
            }
        }

        private void CanvasRight_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _rightActiveMouseStroke = null;
            if (canvasRight.IsMouseCaptured)
            {
                canvasRight.ReleaseMouseCapture();
            }
        }

        #endregion

        #region ERASER HIT-TEST LOGIC

        private void EraseAtPoint(Canvas targetCanvas, List<Polyline> historyList, Point center, double radius = 26.0)
        {
            var elementsToRemove = new List<Polyline>();
            double radiusSquared = radius * radius;

            foreach (UIElement child in targetCanvas.Children)
            {
                if (child is Polyline polyline)
                {
                    foreach (Point pt in polyline.Points)
                    {
                        double dx = pt.X - center.X;
                        double dy = pt.Y - center.Y;
                        if ((dx * dx + dy * dy) <= radiusSquared)
                        {
                            elementsToRemove.Add(polyline);
                            break;
                        }
                    }
                }
            }

            foreach (var el in elementsToRemove)
            {
                targetCanvas.Children.Remove(el);
                historyList.Remove(el);
            }
        }

        #endregion

        #region TOOLBAR CONTROLS & SELECTION

        private void btnLeftColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string hex)
            {
                try
                {
                    _currentLeftColor = (Color)ColorConverter.ConvertFromString(hex);
                    _isLeftEraser = false;
                    btnLeftPen.Background = new SolidColorBrush(_currentLeftColor);
                    btnLeftEraser.Background = new SolidColorBrush(Color.FromRgb(47, 53, 66));
                }
                catch { }
            }
        }

        private void btnLeftPen_Click(object sender, RoutedEventArgs e)
        {
            _isLeftEraser = false;
            btnLeftPen.Background = new SolidColorBrush(_currentLeftColor);
            btnLeftEraser.Background = new SolidColorBrush(Color.FromRgb(47, 53, 66));
        }

        private void btnLeftEraser_Click(object sender, RoutedEventArgs e)
        {
            _isLeftEraser = true;
            btnLeftEraser.Background = new SolidColorBrush(Color.FromRgb(238, 82, 83));
            btnLeftPen.Background = new SolidColorBrush(Color.FromRgb(47, 53, 66));
        }

        private void btnLeftUndo_Click(object sender, RoutedEventArgs e)
        {
            if (_leftHistory.Count > 0)
            {
                var last = _leftHistory[^1];
                _leftHistory.RemoveAt(_leftHistory.Count - 1);
                canvasLeft.Children.Remove(last);
            }
        }

        private void btnLeftClear_Click(object sender, RoutedEventArgs e)
        {
            canvasLeft.Children.Clear();
            _leftActiveTouchStrokes.Clear();
            _leftHistory.Clear();
        }

        private void btnRightColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string hex)
            {
                try
                {
                    _currentRightColor = (Color)ColorConverter.ConvertFromString(hex);
                    _isRightEraser = false;
                    btnRightPen.Background = new SolidColorBrush(_currentRightColor);
                    btnRightEraser.Background = new SolidColorBrush(Color.FromRgb(47, 53, 66));
                }
                catch { }
            }
        }

        private void btnRightPen_Click(object sender, RoutedEventArgs e)
        {
            _isRightEraser = false;
            btnRightPen.Background = new SolidColorBrush(_currentRightColor);
            btnRightEraser.Background = new SolidColorBrush(Color.FromRgb(47, 53, 66));
        }

        private void btnRightEraser_Click(object sender, RoutedEventArgs e)
        {
            _isRightEraser = true;
            btnRightEraser.Background = new SolidColorBrush(Color.FromRgb(238, 82, 83));
            btnRightPen.Background = new SolidColorBrush(Color.FromRgb(47, 53, 66));
        }

        private void btnRightUndo_Click(object sender, RoutedEventArgs e)
        {
            if (_rightHistory.Count > 0)
            {
                var last = _rightHistory[^1];
                _rightHistory.RemoveAt(_rightHistory.Count - 1);
                canvasRight.Children.Remove(last);
            }
        }

        private void btnRightClear_Click(object sender, RoutedEventArgs e)
        {
            canvasRight.Children.Clear();
            _rightActiveTouchStrokes.Clear();
            _rightHistory.Clear();
        }

        #endregion

        #region SESSION EXIT & CONFIRMATION

        private void btnFinish_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            txtConfirmMessage.Text = $"Thời gian làm bài: {_elapsedTime.Minutes:D2}:{_elapsedTime.Seconds:D2}.\n" +
                                     $"Bạn có muốn lưu bài làm của {lblStudent1.Text} và {lblStudent2.Text} thành một trang mới trong bài giảng không?";
            confirmDialogOverlay.Visibility = Visibility.Visible;
        }

        private void btnContinueSession_Click(object sender, RoutedEventArgs e)
        {
            confirmDialogOverlay.Visibility = Visibility.Collapsed;
            _sessionStartTime = DateTime.UtcNow - _elapsedTime;
            _timer.Start();
        }

        private void btnSaveAndExit_Click(object sender, RoutedEventArgs e)
        {
            confirmDialogOverlay.Visibility = Visibility.Collapsed;
            RaiseSessionCompleted(true);
        }

        private void btnDiscardAndExit_Click(object sender, RoutedEventArgs e)
        {
            confirmDialogOverlay.Visibility = Visibility.Collapsed;
            RaiseSessionCompleted(false);
        }

        private void RaiseSessionCompleted(bool saveAsNewBoard)
        {
            var leftPolylines = canvasLeft.Children.OfType<Polyline>().ToList();
            var rightPolylines = canvasRight.Children.OfType<Polyline>().ToList();

            var args = new MultiUserSessionEventArgs
            {
                SaveAsNewBoard = saveAsNewBoard,
                LeftPolylines = leftPolylines,
                RightPolylines = rightPolylines,
                Student1Name = lblStudent1.Text,
                Student1Color = _currentLeftColor,
                Student2Name = lblStudent2.Text,
                Student2Color = _currentRightColor,
                ElapsedTime = _elapsedTime,
                ActualAreaWidth = boardGrid.ActualWidth > 0 ? boardGrid.ActualWidth : 1920,
                ActualAreaHeight = boardGrid.ActualHeight > 0 ? boardGrid.ActualHeight : 1024
            };

            SessionCompleted?.Invoke(this, args);
        }

        #endregion
    }
}
