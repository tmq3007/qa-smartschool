using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using QASmartClass.Classroom.Services;
using QASmartClass.Classroom.Helpers;
using QASmartClass.Data;
using QASmartClass.Services;
using QASmartTouch;
using Serilog;

namespace QASmartClass.Classroom.Views
{

    public partial class CanvasPage : Page
    {
    	private int _lessonId = 0;

    	private int _reactionUnderstandCount = 0;

    	private int _reactionConfusedCount = 0;

    	private double _zoomLevel = 1.0;

    	private int _currentBlockIndex = 0;

    	private int _totalBlocks = 0;

    	private bool _focusMode = false;

    	private bool _spotlightMode = false;

    	private bool _panMode = false;

    	private bool _isPanning = false;

    	private Point _panStartMouse;

    	private double _panStartX;

    	private double _panStartY;

    	private readonly Stack<QASmartClass.Services.Canvas.IUndoableCommand> _undoStack = new Stack<QASmartClass.Services.Canvas.IUndoableCommand>();

    	private readonly Stack<QASmartClass.Services.Canvas.IUndoableCommand> _redoStack = new Stack<QASmartClass.Services.Canvas.IUndoableCommand>();

    	private bool _isUndoRedo = false;

    	private KeyEventHandler? _windowKeyDownHandler;

    	private KeyEventHandler? _windowKeyUpHandler;

    	private readonly List<UIElement> _erasedElementsInCurrentStroke = new List<UIElement>();

    	private Point _lastEraserPoint = new Point(0, 0);

    	private StrokeCollection _lastStrokeState = new StrokeCollection();

        // ═══ ENGINE B: Eraser Constants ═══
        private const double SHAPE_ERASER_RADIUS = 15.0;  // ENGINE B: Hit detection radius for shapes
        private const double ERASER_MOVE_THRESHOLD = 5.0;  // ENGINE B: Minimum move distance to trigger erase

    	private void PushCommand(QASmartClass.Services.Canvas.IUndoableCommand command)
    	{
    		_undoStack.Push(command);
    		
    		int maxSteps = GetMaxUndoSteps();
    		if (_undoStack.Count > maxSteps)
    		{
    			var list = _undoStack.ToList();
    			list = list.Take(maxSteps).ToList();
    			_undoStack.Clear();
    			for (int i = list.Count - 1; i >= 0; i--)
    			{
    				_undoStack.Push(list[i]);
    			}
    		}
    		UpdateUndoRedoButtonsState();
    	}

    	private void UpdateUndoRedoButtonsState()
    	{
    		if (btnUndo != null)
    		{
    			btnUndo.IsEnabled = (_undoStack.Count > 0);
    		}
    		if (btnRedo != null)
    		{
    			btnRedo.IsEnabled = (_redoStack.Count > 0);
    		}
    	}

    	private void TrimStackBottom<T>(Stack<T> stack, int maxCount)
    	{
    		if (stack.Count <= maxCount) return;
    		var list = stack.ToList();
    		list = list.Take(maxCount).ToList();
    		stack.Clear();
    		for (int i = list.Count - 1; i >= 0; i--)
    		{
    			stack.Push(list[i]);
    		}
    	}

    	private int GetMaxUndoSteps()
    	{
    		try
    		{
    			string settingsDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QASmartClass", "Settings");
    			string settingsFile = System.IO.Path.Combine(settingsDir, "board_settings.json");
    			if (System.IO.File.Exists(settingsFile))
    			{
    				string json = System.IO.File.ReadAllText(settingsFile);
    				var match = System.Text.RegularExpressions.Regex.Match(json, @"\""MaxUndoSteps\""\s*:\s*(\d+)");
    				if (match.Success && int.TryParse(match.Groups[1].Value, out int value))
    				{
    					return Math.Clamp(value, 10, 500);
    				}
    			}
    		}
    		catch (Exception ex)
    		{
    			Log.Warning(ex, "Failed to read MaxUndoSteps setting, using default 100.");
    		}
    		return 100;
    	}

    	private string _currentTool = "Pen";

    	private bool _isDrawingShape = false;

    	private Point _shapeStartPoint;

    	private Shape? _previewShape = null;

    	private int _bgMode = 0;

    	private readonly List<UIElement> _shapeElements = new List<UIElement>();

    	private static readonly string[] _extraColors = new string[10] { "#E91E63", "#9C27B0", "#3F51B5", "#009688", "#795548", "#607D8B", "#F44336", "#00BCD4", "#CDDC39", "#FF5722" };

    	private int _extraColorIndex = 0;

    	public CanvasPage(int lessonId = 0)
    	{
    		InitializeComponent();
    		drawCanvas.PreviewMouseDown += DrawCanvas_PreviewMouseDown;
    		drawCanvas.PreviewMouseMove += DrawCanvas_PreviewMouseMove;
    		drawCanvas.PreviewMouseUp += DrawCanvas_PreviewMouseUp;
    		drawCanvas.PreviewStylusDown += DrawCanvas_PreviewStylusDown;
    		drawCanvas.PreviewStylusMove += DrawCanvas_PreviewStylusMove;
    		drawCanvas.PreviewStylusUp += DrawCanvas_PreviewStylusUp;
    		drawCanvas.DefaultDrawingAttributes.Color = Colors.White;
    		drawCanvas.DefaultDrawingAttributes.Width = 3.0;
    		drawCanvas.DefaultDrawingAttributes.Height = 3.0;
    		strokeSlider.ValueChanged += delegate
    		{
    			drawCanvas.DefaultDrawingAttributes.Width = strokeSlider.Value;
    			drawCanvas.DefaultDrawingAttributes.Height = strokeSlider.Value;
    			txtStrokeSize.Text = $"{(int)strokeSlider.Value}px";
    		};
    		drawCanvas.StrokeCollected += delegate(object s, InkCanvasStrokeCollectedEventArgs e)
    		{
    			if (!_isUndoRedo)
    			{
    				PushCommand(new QASmartClass.Services.Canvas.AddStrokeCommand(drawCanvas, e.Stroke));
    				_lastStrokeState = drawCanvas.Strokes.Clone();
    			}
    			try
    			{
    				if (Application.Current is App { NetworkService: not null } app && ClassroomAppContext.Network.IsBroadcasting)
    				{
    					Stroke stroke = e.Stroke;
    					string value = stroke.DrawingAttributes.Color.ToString();
    					string value2 = stroke.DrawingAttributes.Width.ToString("F1");
    					string value3 = string.Join(";", stroke.StylusPoints.Select((StylusPoint p) => $"{p.X:F1},{p.Y:F1}"));
    					double w = drawCanvas.ActualWidth;
    					double h = drawCanvas.ActualHeight;
    					string command = $"CMD|WHITEBOARD_DRAW|{value}|{value2}|{value3}|{w:F1}|{h:F1}";
    					ClassroomAppContext.Network.SendCommandAsync(command);
    				}
    			}
    			catch (Exception ex)
    			{
    				Log.Warning("Send stroke error: {Err}", ex.Message);
    			}
    		};
    		drawCanvas.StrokeErased += delegate
    		{
    			if (!_isUndoRedo)
    			{
    				var erased = new StrokeCollection();
    				foreach (var stroke in _lastStrokeState)
    				{
    					if (!drawCanvas.Strokes.Contains(stroke))
    					{
    						erased.Add(stroke);
    					}
    				}
    				if (erased.Count > 0)
    				{
    					PushCommand(new QASmartClass.Services.Canvas.EraseStrokesCommand(drawCanvas, erased));
    				}
    				_lastStrokeState = drawCanvas.Strokes.Clone();
    			}
    			SyncStrokesToStudents();
    		};
    		_lessonId = lessonId;
    		base.Loaded += delegate
    		{
    			if (_lessonId > 0)
    			{
    				LoadLessonOnCanvas(_lessonId);
    			}
    			_undoStack.Clear();
    			_redoStack.Clear();
    			_lastStrokeState = drawCanvas.Strokes.Clone();
    			if (Application.Current is App { NetworkService: not null } app)
    			{
    				ClassroomAppContext.Network.MessageReceived += NetworkService_MessageReceived;
    			}
    			Window window = Window.GetWindow(this);
    			if (window != null)
    			{
    				_windowKeyDownHandler = delegate(object s, KeyEventArgs e)
    				{
    					if (Keyboard.FocusedElement is TextBox)
    					{
    						return;
    					}
    					if (e.Key == Key.Space && !_panMode)
    					{
    						_panMode = true;
    						UpdatePanToolStyle();
    					}
    					if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
    					{
    						Undo_Click(null, null);
    						e.Handled = true;
    					}
    					if (e.Key == Key.Y && Keyboard.Modifiers == ModifierKeys.Control)
    					{
    						Redo_Click(null, null);
    						e.Handled = true;
    					}
    					if (e.Key == Key.Add || (e.Key == Key.OemPlus && Keyboard.Modifiers == ModifierKeys.Control))
    					{
    						SetZoom(Math.Min(_zoomLevel + 0.15, 3.0));
    					}
    					if (e.Key == Key.Subtract || (e.Key == Key.OemMinus && Keyboard.Modifiers == ModifierKeys.Control))
    					{
    						SetZoom(Math.Max(_zoomLevel - 0.15, 0.5));
    					}
    				};
    				_windowKeyUpHandler = delegate(object s, KeyEventArgs e)
    				{
    					if (e.Key == Key.Space && _panMode)
    					{
    						_panMode = false;
    						UpdatePanToolStyle();
    					}
    				};
    				window.KeyDown += _windowKeyDownHandler;
    				window.KeyUp += _windowKeyUpHandler;
    			}
    			UpdateUndoRedoButtonsState();
    			ApplyBackground();
    		};
    		base.Unloaded += delegate
    		{
    			if (Application.Current is App { NetworkService: not null } app)
    			{
    				ClassroomAppContext.Network.MessageReceived -= NetworkService_MessageReceived;
    			}
    			Window window = Window.GetWindow(this);
    			if (window != null)
    			{
    				if (_windowKeyDownHandler != null)
    				{
    					window.KeyDown -= _windowKeyDownHandler;
    				}
    				if (_windowKeyUpHandler != null)
    				{
    					window.KeyUp -= _windowKeyUpHandler;
    				}
    			}
    			_undoStack.Clear();
    			_redoStack.Clear();
    			_shapeElements.Clear();
    			shapeCanvas.Children.Clear();
    		};
    	}

    	private void SelectTool(object sender, MouseButtonEventArgs e)
    	{
    		Border[] array = new Border[9] { toolPen, toolHighlight, toolEraser, toolSelect, toolShape, toolEllipse, toolLine, toolArrow, toolText };
    		Border[] array2 = array;
    		foreach (Border border in array2)
    		{
    			border.Background = new SolidColorBrush(Color.FromRgb(45, 50, 80));
    			if (border.Child is TextBlock textBlock)
    			{
    				textBlock.Foreground = new SolidColorBrush(Color.FromRgb(176, 190, 197));
    			}
    		}
    		if (!(sender is Border border2))
    		{
    			return;
    		}
    		border2.Background = new SolidColorBrush(Color.FromRgb(25, 118, 210));
    		if (border2.Child is TextBlock textBlock2)
    		{
    			textBlock2.Foreground = Brushes.White;
    		}
    		_currentTool = border2.Tag?.ToString() ?? "Pen";
    		drawCanvas.Cursor = Cursors.Arrow;
    		canvasArea.Cursor = Cursors.Arrow;
    		switch (_currentTool)
    		{
    		case "Highlight":
    			drawCanvas.EditingMode = InkCanvasEditingMode.Ink;
    			drawCanvas.DefaultDrawingAttributes.IsHighlighter = true;
    			drawCanvas.DefaultDrawingAttributes.Color = Color.FromArgb(128, byte.MaxValue, byte.MaxValue, 0);
    			drawCanvas.DefaultDrawingAttributes.Width = 20.0;
    			drawCanvas.DefaultDrawingAttributes.Height = 10.0;
    			drawCanvas.IsHitTestVisible = true;
    			drawCanvas.Cursor = Cursors.Pen;
    			ShowToolHint("Bút nhấn — kéo để vẽ");
    			break;
    		case "Eraser":
    			drawCanvas.DefaultDrawingAttributes.IsHighlighter = false;
    			drawCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
    			drawCanvas.IsHitTestVisible = true;
    			drawCanvas.Cursor = Cursors.Hand;
    			ShowToolHint("Tẩy — chạm vào nét để xóa");
    			break;
    		case "Select":
    			drawCanvas.DefaultDrawingAttributes.IsHighlighter = false;
    			drawCanvas.EditingMode = InkCanvasEditingMode.Select;
    			drawCanvas.IsHitTestVisible = true;
    			drawCanvas.Cursor = Cursors.Arrow;
    			ShowToolHint("Chọn — kéo khung để chọn nét");
    			break;
    		case "Shape":
    		case "Ellipse":
    		case "Line":
    		case "Arrow":
    		{
    			drawCanvas.DefaultDrawingAttributes.IsHighlighter = false;
    			drawCanvas.EditingMode = InkCanvasEditingMode.None;
    			drawCanvas.IsHitTestVisible = false;
    			canvasArea.Cursor = Cursors.Cross;
    			string currentTool = _currentTool;
    			if (1 == 0)
    			{
    			}
    			string text = currentTool switch
    			{
    				"Shape" => "Hình chữ nhật", 
    				"Ellipse" => "Hình elip", 
    				"Line" => "Đường thẳng", 
    				"Arrow" => "Mũi tên", 
    				_ => "", 
    			};
    			if (1 == 0)
    			{
    			}
    			string text2 = text;
    			ShowToolHint(text2 + " — kéo chuột để vẽ");
    			break;
    		}
    		case "Text":
    			drawCanvas.DefaultDrawingAttributes.IsHighlighter = false;
    			drawCanvas.EditingMode = InkCanvasEditingMode.None;
    			drawCanvas.IsHitTestVisible = false;
    			canvasArea.Cursor = Cursors.IBeam;
    			ShowToolHint("Chèn chữ — click vào vị trí trên bảng");
    			break;
    		default:
    			drawCanvas.DefaultDrawingAttributes.IsHighlighter = false;
    			drawCanvas.EditingMode = InkCanvasEditingMode.Ink;
    			drawCanvas.DefaultDrawingAttributes.Width = strokeSlider.Value;
    			drawCanvas.DefaultDrawingAttributes.Height = strokeSlider.Value;
    			drawCanvas.IsHitTestVisible = true;
    			drawCanvas.Cursor = Cursors.Pen;
    			ShowToolHint("Bút vẽ — kéo để vẽ tự do");
    			break;
    		}
    	}

    	private void ShowToolHint(string text)
    	{
    		txtToolHint.Text = text;
    		toolHint.Visibility = Visibility.Visible;
    		DoubleAnimation doubleAnimation = new DoubleAnimation(1.0, 0.0, TimeSpan.FromSeconds(3.0))
    		{
    			BeginTime = TimeSpan.FromSeconds(1.0),
    			EasingFunction = new CubicEase
    			{
    				EasingMode = EasingMode.EaseIn
    			}
    		};
    		doubleAnimation.Completed += delegate
    		{
    			toolHint.Visibility = Visibility.Collapsed;
    		};
    		toolHint.BeginAnimation(UIElement.OpacityProperty, doubleAnimation);
    	}

    	private void ColorPick(object sender, MouseButtonEventArgs e)
    	{
    		if (sender is Ellipse { Tag: string tag })
    		{
    			Color color = (Color)ColorConverter.ConvertFromString(tag);
    			drawCanvas.DefaultDrawingAttributes.Color = color;
    			currentColorFill.Background = new SolidColorBrush(color);
    		}
    	}

    	private void CustomColor_Click(object sender, MouseButtonEventArgs e)
    	{
    		string text = _extraColors[_extraColorIndex % _extraColors.Length];
    		_extraColorIndex++;
    		Color color = (Color)ColorConverter.ConvertFromString(text);
    		drawCanvas.DefaultDrawingAttributes.Color = color;
    		currentColorFill.Background = new SolidColorBrush(color);
    		ShowToolHint("Màu: " + text);
    	}

    	private void Undo_Click(object sender, RoutedEventArgs e)
    	{
    		Log.Information("Undo_Click triggered. Undo stack: {UndoCount}, Redo stack: {RedoCount}", _undoStack.Count, _redoStack.Count);
    		if (_undoStack.Count > 0)
    		{
    			_isUndoRedo = true;
    			var command = _undoStack.Pop();
    			command.Unexecute();
    			_redoStack.Push(command);
    			_lastStrokeState = drawCanvas.Strokes.Clone();
    			_isUndoRedo = false;
    			Log.Information("Undo — stack: {Count}", _undoStack.Count);
    			SyncStrokesToStudents();
    			UpdateUndoRedoButtonsState();
    		}
    	}

    	private void Redo_Click(object sender, RoutedEventArgs e)
    	{
    		Log.Information("Redo_Click triggered. Undo stack: {UndoCount}, Redo stack: {RedoCount}", _undoStack.Count, _redoStack.Count);
    		if (_redoStack.Count > 0)
    		{
    			_isUndoRedo = true;
    			var command = _redoStack.Pop();
    			command.Execute();
    			_undoStack.Push(command);
    			_lastStrokeState = drawCanvas.Strokes.Clone();
    			_isUndoRedo = false;
    			Log.Information("Redo — stack: {Count}", _redoStack.Count);
    			SyncStrokesToStudents();
    			UpdateUndoRedoButtonsState();
    		}
    	}

    	private void BgToggle_Click(object sender, MouseButtonEventArgs e)
    	{
    		_bgMode = (_bgMode + 1) % 4;
    		ApplyBackground();
    		string[] array = new string[4] { "⊟ Trơn", "⊞ Ô vuông", "⁙ Chấm", "☰ Kẻ ngang" };
    		txtBgMode.Text = (new string[4] { "⊞", "⁙", "☰", "⊟" })[_bgMode];
    		ShowToolHint("Nền: " + array[_bgMode]);
    	}

    	private void ApplyBackground()
    	{
    		bgPatternCanvas.Children.Clear();
    		switch (_bgMode)
    		{
    		case 1:
    			DrawGridPattern(40.0, Color.FromArgb(30, byte.MaxValue, byte.MaxValue, byte.MaxValue));
    			break;
    		case 2:
    			DrawDotsPattern(30.0, 3.0, Color.FromArgb(40, byte.MaxValue, byte.MaxValue, byte.MaxValue));
    			break;
    		case 3:
    			DrawLinePattern(40.0, Color.FromArgb(25, byte.MaxValue, byte.MaxValue, byte.MaxValue));
    			break;
    		}
    	}

    	private void DrawGridPattern(double spacing, Color color)
    	{
    		double num = 2000.0;
    		double num2 = 1400.0;
    		SolidColorBrush stroke = new SolidColorBrush(color);
    		for (double num3 = 0.0; num3 < num; num3 += spacing)
    		{
    			bgPatternCanvas.Children.Add(new Line
    			{
    				X1 = num3,
    				Y1 = 0.0,
    				X2 = num3,
    				Y2 = num2,
    				Stroke = stroke,
    				StrokeThickness = 0.5
    			});
    		}
    		for (double num4 = 0.0; num4 < num2; num4 += spacing)
    		{
    			bgPatternCanvas.Children.Add(new Line
    			{
    				X1 = 0.0,
    				Y1 = num4,
    				X2 = num,
    				Y2 = num4,
    				Stroke = stroke,
    				StrokeThickness = 0.5
    			});
    		}
    	}

    	private void DrawDotsPattern(double spacing, double dotSize, Color color)
    	{
    		double num = 2000.0;
    		double num2 = 1400.0;
    		SolidColorBrush fill = new SolidColorBrush(color);
    		for (double num3 = spacing; num3 < num; num3 += spacing)
    		{
    			for (double num4 = spacing; num4 < num2; num4 += spacing)
    			{
    				Ellipse element = new Ellipse
    				{
    					Width = dotSize,
    					Height = dotSize,
    					Fill = fill
    				};
    				Canvas.SetLeft(element, num3 - dotSize / 2.0);
    				Canvas.SetTop(element, num4 - dotSize / 2.0);
    				bgPatternCanvas.Children.Add(element);
    			}
    		}
    	}

    	private void DrawLinePattern(double spacing, Color color)
    	{
    		double x = 2000.0;
    		double num = 1400.0;
    		SolidColorBrush stroke = new SolidColorBrush(color);
    		for (double num2 = spacing; num2 < num; num2 += spacing)
    		{
    			bgPatternCanvas.Children.Add(new Line
    			{
    				X1 = 0.0,
    				Y1 = num2,
    				X2 = x,
    				Y2 = num2,
    				Stroke = stroke,
    				StrokeThickness = 0.5
    			});
    		}
    	}

    	private void StartShapeDraw(Point pos)
    	{
    		if (_currentTool == "Text")
    		{
    			InsertTextAtPosition(pos);
    			return;
    		}
    		_isDrawingShape = true;
    		_shapeStartPoint = pos;
    		_previewShape = null;
    	}

    	private void UpdateShapePreview(Point pos)
    	{
    		if (_isDrawingShape)
    		{
    			shapePreviewCanvas.Children.Clear();
    			Color color = drawCanvas.DefaultDrawingAttributes.Color;
    			double width = drawCanvas.DefaultDrawingAttributes.Width;
    			SolidColorBrush stroke = new SolidColorBrush(color);
    			Rect rect = new Rect(_shapeStartPoint, pos);
    			Shape shape = null;
    			switch (_currentTool)
    			{
    			case "Shape":
    				shape = new Rectangle
    				{
    					Width = rect.Width,
    					Height = rect.Height,
    					Stroke = stroke,
    					StrokeThickness = width,
    					Fill = Brushes.Transparent,
    					StrokeDashArray = new DoubleCollection { 4.0, 2.0 }
    				};
    				Canvas.SetLeft(shape, rect.Left);
    				Canvas.SetTop(shape, rect.Top);
    				break;
    			case "Ellipse":
    				shape = new Ellipse
    				{
    					Width = rect.Width,
    					Height = rect.Height,
    					Stroke = stroke,
    					StrokeThickness = width,
    					Fill = Brushes.Transparent,
    					StrokeDashArray = new DoubleCollection { 4.0, 2.0 }
    				};
    				Canvas.SetLeft(shape, rect.Left);
    				Canvas.SetTop(shape, rect.Top);
    				break;
    			case "Line":
    				shape = new Line
    				{
    					X1 = _shapeStartPoint.X,
    					Y1 = _shapeStartPoint.Y,
    					X2 = pos.X,
    					Y2 = pos.Y,
    					Stroke = stroke,
    					StrokeThickness = width,
    					StrokeDashArray = new DoubleCollection { 4.0, 2.0 }
    				};
    				break;
    			case "Arrow":
    				shape = new Line
    				{
    					X1 = _shapeStartPoint.X,
    					Y1 = _shapeStartPoint.Y,
    					X2 = pos.X,
    					Y2 = pos.Y,
    					Stroke = stroke,
    					StrokeThickness = width,
    					StrokeDashArray = new DoubleCollection { 4.0, 2.0 }
    				};
    				break;
    			}
    			if (shape != null)
    			{
    				shapePreviewCanvas.Children.Add(shape);
    				_previewShape = shape;
    			}
    		}
    	}

    	private void FinishShapeDraw(Point pos)
    	{
    		if (!_isDrawingShape)
    		{
    			return;
    		}
    		_isDrawingShape = false;
    		shapePreviewCanvas.Children.Clear();
    		Color color = drawCanvas.DefaultDrawingAttributes.Color;
    		double width = drawCanvas.DefaultDrawingAttributes.Width;
    		SolidColorBrush solidColorBrush = new SolidColorBrush(color);
    		Rect rect = new Rect(_shapeStartPoint, pos);
    		if (!(rect.Width < 5.0) || !(rect.Height < 5.0) || !(_currentTool != "Line") || !(_currentTool != "Arrow"))
    		{
    			Shape shape = null;
    			switch (_currentTool)
    			{
    			case "Shape":
    				shape = new Rectangle
    				{
    					Width = rect.Width,
    					Height = rect.Height,
    					Stroke = solidColorBrush,
    					StrokeThickness = width,
    					Fill = Brushes.Transparent
    				};
    				Canvas.SetLeft(shape, rect.Left);
    				Canvas.SetTop(shape, rect.Top);
    				break;
    			case "Ellipse":
    				shape = new Ellipse
    				{
    					Width = rect.Width,
    					Height = rect.Height,
    					Stroke = solidColorBrush,
    					StrokeThickness = width,
    					Fill = Brushes.Transparent
    				};
    				Canvas.SetLeft(shape, rect.Left);
    				Canvas.SetTop(shape, rect.Top);
    				break;
    			case "Line":
    				shape = new Line
    				{
    					X1 = _shapeStartPoint.X,
    					Y1 = _shapeStartPoint.Y,
    					X2 = pos.X,
    					Y2 = pos.Y,
    					Stroke = solidColorBrush,
    					StrokeThickness = width,
    					StrokeStartLineCap = PenLineCap.Round,
    					StrokeEndLineCap = PenLineCap.Round
    				};
    				break;
    			case "Arrow":
    			{
    				System.Windows.Shapes.Path path = CreateArrowPath(_shapeStartPoint, pos, solidColorBrush, width);
    				PushCommand(new QASmartClass.Services.Canvas.AddShapeCommand(shapeCanvas, path, _shapeElements));
    				shapeCanvas.Children.Add(path);
    				_shapeElements.Add(path);
    				return;
    			}
    			}
    			if (shape != null)
    			{
    				PushCommand(new QASmartClass.Services.Canvas.AddShapeCommand(shapeCanvas, shape, _shapeElements));
    				shapeCanvas.Children.Add(shape);
    				_shapeElements.Add(shape);
    			}
    		}
    	}

    	private System.Windows.Shapes.Path CreateArrowPath(Point start, Point end, SolidColorBrush brush, double thickness)
    	{
    		double num = Math.Atan2(end.Y - start.Y, end.X - start.X);
    		double num2 = Math.Max(thickness * 4.0, 12.0);
    		double num3 = Math.PI / 6.0;
    		Point endPoint = new Point(end.X - num2 * Math.Cos(num - num3), end.Y - num2 * Math.Sin(num - num3));
    		Point endPoint2 = new Point(end.X - num2 * Math.Cos(num + num3), end.Y - num2 * Math.Sin(num + num3));
    		GeometryGroup geometryGroup = new GeometryGroup();
    		geometryGroup.Children.Add(new LineGeometry(start, end));
    		geometryGroup.Children.Add(new LineGeometry(end, endPoint));
    		geometryGroup.Children.Add(new LineGeometry(end, endPoint2));
    		return new System.Windows.Shapes.Path
    		{
    			Data = geometryGroup,
    			Stroke = brush,
    			StrokeThickness = thickness,
    			StrokeStartLineCap = PenLineCap.Round,
    			StrokeEndLineCap = PenLineCap.Round
    		};
    	}

    	private void InsertTextAtPosition(Point pos)
    	{
    		TextBox inputBox = new TextBox
    		{
    			FontSize = Math.Max(drawCanvas.DefaultDrawingAttributes.Width * 4.0, 24.0),
    			Foreground = new SolidColorBrush(drawCanvas.DefaultDrawingAttributes.Color),
    			Background = new SolidColorBrush(Color.FromArgb(180, 15, 52, 96)),
    			BorderThickness = new Thickness(1.0),
    			BorderBrush = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
    			Padding = new Thickness(6.0, 4.0, 6.0, 4.0),
    			MinWidth = 100.0,
    			AcceptsReturn = false,
    			FontFamily = GetInterFontFamily()
    		};
    		Canvas.SetLeft(inputBox, pos.X);
    		Canvas.SetTop(inputBox, pos.Y);
    		shapeCanvas.Children.Add(inputBox);
    		inputBox.Loaded += delegate
    		{
    			inputBox.Focus();
    		};
    		inputBox.KeyDown += delegate(object s, KeyEventArgs ke)
    		{
    			if (ke.Key == Key.Return || ke.Key == Key.Escape)
    			{
    				if (ke.Key == Key.Return && !string.IsNullOrWhiteSpace(inputBox.Text))
    				{
    					TextBlock textBlock = new TextBlock
    					{
    						Text = inputBox.Text,
    						FontSize = inputBox.FontSize,
    						Foreground = inputBox.Foreground,
    						FontFamily = inputBox.FontFamily
    					};
    					Canvas.SetLeft(textBlock, Canvas.GetLeft(inputBox));
    					Canvas.SetTop(textBlock, Canvas.GetTop(inputBox));
    					shapeCanvas.Children.Remove(inputBox);
    					PushCommand(new QASmartClass.Services.Canvas.AddTextCommand(shapeCanvas, textBlock, _shapeElements));
    					shapeCanvas.Children.Add(textBlock);
    					_shapeElements.Add(textBlock);
    				}
    				else
    				{
    					shapeCanvas.Children.Remove(inputBox);
    				}
    			}
    		};
    		inputBox.LostFocus += delegate
    		{
    			if (!string.IsNullOrWhiteSpace(inputBox.Text))
    			{
    				TextBlock textBlock = new TextBlock
    				{
    					Text = inputBox.Text,
    					FontSize = inputBox.FontSize,
    					Foreground = inputBox.Foreground,
    					FontFamily = inputBox.FontFamily
    				};
    				Canvas.SetLeft(textBlock, Canvas.GetLeft(inputBox));
    				Canvas.SetTop(textBlock, Canvas.GetTop(inputBox));
    				shapeCanvas.Children.Remove(inputBox);
    				PushCommand(new QASmartClass.Services.Canvas.AddTextCommand(shapeCanvas, textBlock, _shapeElements));
    				shapeCanvas.Children.Add(textBlock);
    				_shapeElements.Add(textBlock);
    			}
    			else
    			{
    				shapeCanvas.Children.Remove(inputBox);
    			}
    		};
    	}

    	private void SaveCanvas_Click(object sender, RoutedEventArgs e)
    	{
    		try
    		{
    			SaveFileDialog saveFileDialog = new SaveFileDialog
    			{
    				Filter = "Ink file (*.isf)|*.isf|PNG image (*.png)|*.png",
    				FileName = $"BangVe_{DateTime.Now:yyyyMMdd_HHmmss}",
    				DefaultExt = ".isf"
    			};
    			if (saveFileDialog.ShowDialog() != true)
    			{
    				return;
    			}
    			if (saveFileDialog.FilterIndex == 2)
    			{
    				SaveAsPng(saveFileDialog.FileName);
    			}
    			else
    			{
    				using FileStream stream = new FileStream(saveFileDialog.FileName, FileMode.Create);
    				drawCanvas.Strokes.Save(stream);
    			}
    			MessageBox.Show("Da luu: " + saveFileDialog.FileName, "Luu", MessageBoxButton.OK, MessageBoxImage.Asterisk);
    			Log.Information("Canvas saved: {Path}", saveFileDialog.FileName);
    		}
    		catch (Exception ex)
    		{
    			MessageBox.Show("Loi: " + ex.Message, "Loi", MessageBoxButton.OK, MessageBoxImage.Hand);
    		}
    	}

    	private void SaveCanvasAsSlide_Click(object sender, RoutedEventArgs e)
    	{
    		if (_lessonId <= 0)
    		{
    			MessageBox.Show("Vui lòng mở một bài học trước khi lưu bảng thành Slide!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Exclamation);
    			return;
    		}
    		try
    		{
    			string text = Interaction.InputBox("Nhập tên slide vẽ bảng:", "Lưu Slide")?.Trim() ?? "";
    			if (string.IsNullOrEmpty(text))
    			{
    				text = $"Bảng vẽ_{DateTime.Now:yyyyMMdd_HHmmss}";
    			}
    			// → ClassroomAppContext
    			Lesson lesson = ClassroomAppContext.Db.Lessons.Find(_lessonId);
    			string text2 = ClassroomAppContext.Db.TeacherProfiles.FirstOrDefault()?.FullName ?? "Giáo viên";
    			string text3 = text;
    			if (lesson != null && !string.Equals(lesson.TeacherName?.Trim(), text2.Trim(), StringComparison.OrdinalIgnoreCase))
    			{
    				MessageBox.Show($"Bạn đang dạy thay bài học của giáo viên {lesson.TeacherName}. Slide sẽ được lưu với hậu tố [Dạy thay - {text2}]", "Cảnh báo dạy thay", MessageBoxButton.OK, MessageBoxImage.Exclamation);
    				text3 = text3 + " [Dạy thay - " + text2 + "]";
    				lesson.SubstituteTeacher = text2;
    				lesson.SubstituteReason = "Dạy thay tự động ghi nhận khi lưu slide bảng vẽ";
    			}
    			string picturesDir = AppPaths.PicturesDir;
    			if (!Directory.Exists(picturesDir))
    			{
    				Directory.CreateDirectory(picturesDir);
    			}
    			string value = string.Join("_", text3.Split(System.IO.Path.GetInvalidFileNameChars()));
    			string text4 = System.IO.Path.Combine(picturesDir, $"Lesson_{_lessonId}_Board_{DateTime.Now:yyyyMMdd_HHmmss}_{value}.png");
    			SaveAsPng(text4);
    			int sortOrder = ClassroomAppContext.Db.LessonContents.Count((LessonContent c) => c.LessonId == _lessonId);
    			LessonContent entity = new LessonContent
    			{
    				LessonId = _lessonId,
    				ContentType = "Image",
    				Data = text4,
    				SortOrder = sortOrder
    			};
    			ClassroomAppContext.Db.LessonContents.Add(entity);
    			ClassroomAppContext.Db.SaveChanges();
    			MessageBox.Show("Đã lưu bảng vẽ thành một Slide mới trong bài học hiện tại!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Asterisk);
    			Log.Information("Saved canvas as lesson content image: {Path} for Lesson ID {LessonId}", text4, _lessonId);
    			LoadLessonOnCanvas(_lessonId);
    		}
    		catch (Exception ex)
    		{
    			MessageBox.Show("Lỗi khi lưu slide: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Hand);
    			Log.Error(ex, "SaveCanvasAsSlide error");
    		}
    	}

    	private void LoadCanvas_Click(object sender, RoutedEventArgs e)
    	{
    		try
    		{
    			OpenFileDialog openFileDialog = new OpenFileDialog
    			{
    				Filter = "Ink file (*.isf)|*.isf",
    				DefaultExt = ".isf"
    			};
    			if (openFileDialog.ShowDialog() == true)
    			{
    				using (FileStream stream = new FileStream(openFileDialog.FileName, FileMode.Open))
    				{
    					drawCanvas.Strokes = new StrokeCollection(stream);
    					_undoStack.Clear();
    					_redoStack.Clear();
    					_lastStrokeState = drawCanvas.Strokes.Clone();
    					UpdateUndoRedoButtonsState();
    					Log.Information("Canvas loaded: {Path}", openFileDialog.FileName);
    					return;
    				}
    			}
    		}
    		catch (Exception ex)
    		{
    			MessageBox.Show("Loi: " + ex.Message, "Loi", MessageBoxButton.OK, MessageBoxImage.Hand);
    		}
    	}

    	private void SaveAsPng(string path)
    	{
    		Grid grid = canvasArea;
    		int num = (int)grid.ActualWidth;
    		int num2 = (int)grid.ActualHeight;
    		if (num <= 0 || num2 <= 0)
    		{
    			return;
    		}
    		RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap(num, num2, 96.0, 96.0, PixelFormats.Pbgra32);
    		renderTargetBitmap.Render(grid);
    		PngBitmapEncoder pngBitmapEncoder = new PngBitmapEncoder();
    		pngBitmapEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
    		using FileStream stream = File.Create(path);
    		pngBitmapEncoder.Save(stream);
    	}

    	private void PanMode_Click(object sender, MouseButtonEventArgs e)
    	{
    		_panMode = !_panMode;
    		UpdatePanToolStyle();
    	}

    	private void UpdatePanToolStyle()
    	{
    		SolidColorBrush solidColorBrush = new SolidColorBrush(Color.FromRgb(25, 118, 210));
    		SolidColorBrush solidColorBrush2 = new SolidColorBrush(Color.FromRgb(45, 50, 80));
    		toolPan.Background = (_panMode ? solidColorBrush : solidColorBrush2);
    		canvasArea.Cursor = (_panMode ? Cursors.Hand : Cursors.Arrow);
    	}

    	private void CanvasArea_MouseDown(object sender, MouseButtonEventArgs e)
    	{
    		bool flag = _panMode || e.MiddleButton == MouseButtonState.Pressed;
    		if (flag || (_zoomLevel > 1.0 && Keyboard.Modifiers == ModifierKeys.Alt))
    		{
    			_isPanning = true;
    			_panStartMouse = e.GetPosition(canvasArea);
    			_panStartX = panTransform.X;
    			_panStartY = panTransform.Y;
    			canvasArea.CaptureMouse();
    			canvasArea.Cursor = Cursors.SizeAll;
    			e.Handled = flag;
    			return;
    		}
    		bool flag2;
    		switch (_currentTool)
    		{
    		case "Shape":
    		case "Ellipse":
    		case "Line":
    		case "Arrow":
    		case "Text":
    			flag2 = true;
    			break;
    		default:
    			flag2 = false;
    			break;
    		}
    		if (flag2)
    		{
    			Point position = e.GetPosition(shapePreviewCanvas);
    			StartShapeDraw(position);
    			e.Handled = true;
    		}
    	}

    	private void CanvasArea_MouseMove(object sender, MouseEventArgs e)
    	{
    		if (_isPanning)
    		{
    			Point position = e.GetPosition(canvasArea);
    			double num = position.X - _panStartMouse.X;
    			double num2 = position.Y - _panStartMouse.Y;
    			double actualWidth = canvasArea.ActualWidth;
    			double actualHeight = canvasArea.ActualHeight;
    			double num3 = Math.Max(3000.0, actualWidth * (_zoomLevel - 1.0) / 2.0);
    			double num4 = Math.Max(3000.0, actualHeight * (_zoomLevel - 1.0) / 2.0);
    			panTransform.X = Math.Max(0.0 - num3, Math.Min(num3, _panStartX + num));
    			panTransform.Y = Math.Max(0.0 - num4, Math.Min(num4, _panStartY + num2));
    		}
    		else if (_isDrawingShape)
    		{
    			Point position2 = e.GetPosition(shapePreviewCanvas);
    			UpdateShapePreview(position2);
    		}
    	}

    	private void CanvasArea_MouseUp(object sender, MouseButtonEventArgs e)
    	{
    		if (_isPanning)
    		{
    			_isPanning = false;
    			canvasArea.ReleaseMouseCapture();
    			canvasArea.Cursor = (_panMode ? Cursors.Hand : Cursors.Arrow);
    		}
    		else if (_isDrawingShape)
    		{
    			Point position = e.GetPosition(shapePreviewCanvas);
    			FinishShapeDraw(position);
    		}
    	}

    	// ═══════════════════════════════════════════════════════
    	// ENGINE B: SMART CLASS MODULE — Mouse + Stylus Shape Eraser
    	// Thuật toán: IsCloseToShape() với Rect(30×30), bán kính 15px
    	// Reference: eraser_tool_specification.md v2.1 Mục III.C
    	// ═══════════════════════════════════════════════════════
    	private void DrawCanvas_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    	{
    		if (_currentTool == "Eraser")
    		{
    			_erasedElementsInCurrentStroke.Clear();
    			_lastEraserPoint = e.GetPosition(drawCanvas);
    			EraseShapesAtPoint(_lastEraserPoint);
    		}
    	}

    	private void DrawCanvas_PreviewMouseMove(object sender, MouseEventArgs e)
    	{
    		if (_currentTool == "Eraser" && e.LeftButton == MouseButtonState.Pressed)
    		{
    			Point currentPoint = e.GetPosition(drawCanvas);
    			double distance = Math.Sqrt(Math.Pow(currentPoint.X - _lastEraserPoint.X, 2) + Math.Pow(currentPoint.Y - _lastEraserPoint.Y, 2));
    			if (distance >= ERASER_MOVE_THRESHOLD) // Throttling: 5px distance threshold
    			{
    				_lastEraserPoint = currentPoint;
    				EraseShapesAtPoint(currentPoint);
    			}
    		}
    	}

    	private void DrawCanvas_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    	{
    		if (_currentTool == "Eraser")
    		{
    			if (_erasedElementsInCurrentStroke.Count > 0)
    			{
    				var deleteCmd = new QASmartClass.Services.Canvas.DeleteElementsCommand(shapeCanvas, _erasedElementsInCurrentStroke, _shapeElements);
    				PushCommand(deleteCmd);
    				deleteCmd.Execute();
    				_erasedElementsInCurrentStroke.Clear();
    			}
    		}
    	}

    	private void DrawCanvas_PreviewStylusDown(object sender, StylusDownEventArgs e)
    	{
    		if (_currentTool == "Eraser")
    		{
    			_erasedElementsInCurrentStroke.Clear();
    			_lastEraserPoint = e.GetPosition(drawCanvas);
    			EraseShapesAtPoint(_lastEraserPoint);
    		}
    	}

    	private void DrawCanvas_PreviewStylusMove(object sender, StylusEventArgs e)
    	{
    		if (_currentTool == "Eraser" && e.InAir == false)
    		{
    			Point currentPoint = e.GetPosition(drawCanvas);
    			double distance = Math.Sqrt(Math.Pow(currentPoint.X - _lastEraserPoint.X, 2) + Math.Pow(currentPoint.Y - _lastEraserPoint.Y, 2));
    			if (distance >= ERASER_MOVE_THRESHOLD)
    			{
    				_lastEraserPoint = currentPoint;
    				EraseShapesAtPoint(currentPoint);
    			}
    		}
    	}

    	private void DrawCanvas_PreviewStylusUp(object sender, StylusEventArgs e)
    	{
    		if (_currentTool == "Eraser")
    		{
    			if (_erasedElementsInCurrentStroke.Count > 0)
    			{
    				var deleteCmd = new QASmartClass.Services.Canvas.DeleteElementsCommand(shapeCanvas, _erasedElementsInCurrentStroke, _shapeElements);
    				PushCommand(deleteCmd);
    				deleteCmd.Execute();
    				_erasedElementsInCurrentStroke.Clear();
    			}
    		}
    	}

    	private void EraseShapesAtPoint(Point mousePos)
    	{
    		Rect eraserRect = new Rect(mousePos.X - SHAPE_ERASER_RADIUS, mousePos.Y - SHAPE_ERASER_RADIUS, SHAPE_ERASER_RADIUS * 2, SHAPE_ERASER_RADIUS * 2);
    		var toDelete = new List<UIElement>();
    		for (int i = _shapeElements.Count - 1; i >= 0; i--)
    		{
    			var element = _shapeElements[i];
    			if (element.IsArrangeValid && element.IsDescendantOf(shapeCanvas))
    			{
    				try
    				{
    					if (IsCloseToShape(mousePos, element, eraserRect))
    					{
    						toDelete.Add(element);
    					}
    				}
    				catch (Exception ex)
    				{
    					Log.Warning("Error detecting eraser hit: {Err}", ex.Message);
    				}
    			}
    		}
    		foreach (var element in toDelete)
    		{
    			if (!_erasedElementsInCurrentStroke.Contains(element))
    			{
    				_erasedElementsInCurrentStroke.Add(element);
    				if (shapeCanvas.Children.Contains(element))
    				{
    					shapeCanvas.Children.Remove(element);
    				}
    				if (_shapeElements.Contains(element))
    				{
    					_shapeElements.Remove(element);
    				}
    			}
    		}
    	}

    	private bool IsCloseToShape(Point mousePos, UIElement element, Rect eraserRect)
    	{
    		double left = Canvas.GetLeft(element);
    		double top = Canvas.GetTop(element);
    		if (double.IsNaN(left)) left = 0.0;
    		if (double.IsNaN(top)) top = 0.0;

    		if (element is Line line)
    		{
    			return DistanceToSegment(mousePos, new Point(line.X1, line.Y1), new Point(line.X2, line.Y2)) <= SHAPE_ERASER_RADIUS;
    		}
    		else if (element is System.Windows.Shapes.Path path)
    		{
    			if (path.Data is GeometryGroup group)
    			{
    				foreach (var geom in group.Children)
    				{
    					if (geom is LineGeometry lineGeom)
    					{
    						if (DistanceToSegment(mousePos, lineGeom.StartPoint, lineGeom.EndPoint) <= SHAPE_ERASER_RADIUS)
    						{
    							return true;
    						}
    					}
    				}
    			}
    			Rect bounds = element.TransformToVisual(shapeCanvas).TransformBounds(new Rect(0, 0, element.RenderSize.Width, element.RenderSize.Height));
    			return eraserRect.IntersectsWith(bounds);
    		}
    		else if (element is System.Windows.Shapes.Rectangle || element is System.Windows.Shapes.Ellipse)
    		{
    			double w = element.RenderSize.Width;
    			double h = element.RenderSize.Height;
    			if (w <= 0 || h <= 0) return false;

    			if (element is System.Windows.Shapes.Rectangle)
    			{
    				double d1 = DistanceToSegment(mousePos, new Point(left, top), new Point(left + w, top));
    				double d2 = DistanceToSegment(mousePos, new Point(left, top + h), new Point(left + w, top + h));
    				double d3 = DistanceToSegment(mousePos, new Point(left, top), new Point(left, top + h));
    				double d4 = DistanceToSegment(mousePos, new Point(left + w, top), new Point(left + w, top + h));
    				double minD = Math.Min(Math.Min(d1, d2), Math.Min(d3, d4));
    				return minD <= SHAPE_ERASER_RADIUS;
    			}
    			else 
    			{
    				double rx = w / 2.0;
    				double ry = h / 2.0;
    				double cx = left + rx;
    				double cy = top + ry;
    				double dx = (mousePos.X - cx) / rx;
    				double dy = (mousePos.Y - cy) / ry;
    				double dist = Math.Sqrt(dx * dx + dy * dy);
    				double pixelDist = Math.Abs(dist - 1.0) * (rx + ry) / 2.0;
    				return pixelDist <= SHAPE_ERASER_RADIUS;
    			}
    		}
    		else 
    		{
    			Rect bounds = new Rect(left, top, element.RenderSize.Width, element.RenderSize.Height);
    			return eraserRect.IntersectsWith(bounds);
    		}
    	}

    	private static double DistanceToSegment(Point p, Point a, Point b)
    	{
    		double l2 = (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y);
    		if (l2 == 0.0) return Math.Sqrt((p.X - a.X) * (p.X - a.X) + (p.Y - a.Y) * (p.Y - a.Y));
    		double t = ((p.X - a.X) * (b.X - a.X) + (p.Y - a.Y) * (b.Y - a.Y)) / l2;
    		t = Math.Max(0.0, Math.Min(1.0, t));
    		double dx = p.X - (a.X + t * (b.X - a.X));
    		double dy = p.Y - (a.Y + t * (b.Y - a.Y));
    		return Math.Sqrt(dx * dx + dy * dy);
    	}

    	private void CanvasArea_MouseWheel(object sender, MouseWheelEventArgs e)
    	{
    		if (Keyboard.Modifiers == ModifierKeys.Control)
    		{
    			double num = ((e.Delta > 0) ? 0.1 : (-0.1));
    			double num2 = Math.Max(0.5, Math.Min(3.0, _zoomLevel + num));
    			if (!(Math.Abs(num2 - _zoomLevel) < 0.001))
    			{
    				double num3 = num2 / _zoomLevel;
    				double actualWidth = canvasArea.ActualWidth;
    				double actualHeight = canvasArea.ActualHeight;
    				double num4 = actualWidth / 2.0;
    				double num5 = actualHeight / 2.0;
    				panTransform.X = num4 + (panTransform.X - num4) * num3;
    				panTransform.Y = num5 + (panTransform.Y - num5) * num3;
    				double num6 = actualWidth * (num2 - 1.0) / 2.0;
    				double num7 = actualHeight * (num2 - 1.0) / 2.0;
    				panTransform.X = Math.Max(0.0 - num6, Math.Min(num6, panTransform.X));
    				panTransform.Y = Math.Max(0.0 - num7, Math.Min(num7, panTransform.Y));
    				zoomTransform.ScaleX = num2;
    				zoomTransform.ScaleY = num2;
    				_zoomLevel = num2;
    				int value = (int)(num2 * 100.0);
    				txtZoomLevel.Text = $"{value}%";
    				txtZoomBadge.Text = $"\ud83d\udd0d {value}%";
    				zoomBadge.Visibility = ((num2 == 1.0) ? Visibility.Collapsed : Visibility.Visible);
    				e.Handled = true;
    			}
    		}
    	}

    	private void ZoomIn_Click(object sender, MouseButtonEventArgs e)
    	{
    		SetZoom(Math.Min(_zoomLevel + 0.15, 3.0));
    	}

    	private void ZoomOut_Click(object sender, MouseButtonEventArgs e)
    	{
    		SetZoom(Math.Max(_zoomLevel - 0.15, 0.5));
    	}

    	private void ZoomReset_Click(object sender, MouseButtonEventArgs e)
    	{
    		SetZoom(1.0);
    		panTransform.X = 0.0;
    		panTransform.Y = 0.0;
    	}

    	private void SetZoom(double level)
    	{
    		_zoomLevel = level;
    		DoubleAnimation animation = new DoubleAnimation(level, TimeSpan.FromMilliseconds(200.0))
    		{
    			EasingFunction = new CubicEase
    			{
    				EasingMode = EasingMode.EaseOut
    			}
    		};
    		zoomTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
    		zoomTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
    		int value = (int)(level * 100.0);
    		txtZoomLevel.Text = $"{value}%";
    		txtZoomBadge.Text = $"\ud83d\udd0d {value}%";
    		zoomBadge.Visibility = ((level == 1.0) ? Visibility.Collapsed : Visibility.Visible);
    	}

    	private void Screenshot_Click(object sender, RoutedEventArgs e)
    	{
    		try
    		{
    			DoubleAnimation animation = new DoubleAnimation(0.7, 0.0, TimeSpan.FromMilliseconds(300.0));
    			flashOverlay.BeginAnimation(UIElement.OpacityProperty, animation);
    			string text = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "QASmartClass");
    			Directory.CreateDirectory(text);
    			string text2 = System.IO.Path.Combine(text, $"BangVe_{DateTime.Now:yyyyMMdd_HHmmss}.png");
    			SaveAsPng(text2);
    			MessageBox.Show("Da chup bang!\n\n" + text2, "Chup man hinh", MessageBoxButton.OK, MessageBoxImage.Asterisk);
    			Log.Information("Screenshot saved: {Path}", text2);
    		}
    		catch (Exception ex)
    		{
    			Log.Error(ex, "Screenshot error");
    			MessageBox.Show("Loi: " + ex.Message, "Loi", MessageBoxButton.OK, MessageBoxImage.Hand);
    		}
    	}

    	private void ClearCanvas_Click(object sender, RoutedEventArgs e)
    	{
    		if (MessageBox.Show("Bạn có chắc chắn muốn xóa toàn bộ nội dung trên bảng vẽ không?", "Xác nhận xóa bảng", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
    		{
    			return;
    		}
    		var clearCmd = new QASmartClass.Services.Canvas.ClearCanvasCommand(drawCanvas, shapeCanvas, _shapeElements);
    		PushCommand(clearCmd);
    		clearCmd.Execute();
    		lessonOverlay.Children.Clear();
    		lessonOverlay.Visibility = Visibility.Collapsed;
    		blockNav.Visibility = Visibility.Collapsed;
    		_focusMode = false;
    		_spotlightMode = false;
    		spotlightOverlay.Visibility = Visibility.Collapsed;
    		focusOverlay.Visibility = Visibility.Collapsed;
    		try
    		{
    			if (Application.Current is App { NetworkService: not null } app && ClassroomAppContext.Network.IsBroadcasting)
    			{
    				ClassroomAppContext.Network.SendCommandAsync("CMD|WHITEBOARD_CLEAR|ALL");
    			}
    		}
    		catch (Exception ex)
    		{
    			Log.Warning("Clear whiteboard net error: {Err}", ex.Message);
    		}
    	}

    	private void GoToSmartScreen_Click(object sender, RoutedEventArgs e)
    	{
    		// → ClassroomAppContext
    		((QASmartTouch.App)System.Windows.Application.Current).ModeService.GoToScreen();
    	}

    	private void Canvas_MouseEnter(object sender, MouseEventArgs e)
    	{
    	}

    	private void Canvas_MouseLeave(object sender, MouseEventArgs e)
    	{
    	}

    	private void NetworkService_MessageReceived(object? sender, StudentMessageEventArgs e)
    	{
    		if (string.IsNullOrEmpty(e.Message))
    		{
    			return;
    		}
    		string[] array = e.Message.Split('|');
    		if (array.Length < 4 || !(array[0] == "CHAT"))
    		{
    			return;
    		}
    		string text = array[3];
    		if (text == "\ud83d\udc4d Đã hiểu")
    		{
    			base.Dispatcher.Invoke(delegate
    			{
    				_reactionUnderstandCount++;
    				if (txtReactionUnderstand != null)
    				{
    					txtReactionUnderstand.Text = $"\ud83d\udc4d {_reactionUnderstandCount}";
    				}
    				UpdateUnderstandRate();
    				TriggerFloatingEmoji("\ud83d\udc4d");
    			});
    		}
    		else
    		{
    			if (!(text == "\ud83d\ude15 Chưa hiểu"))
    			{
    				return;
    			}
    			base.Dispatcher.Invoke(delegate
    			{
    				_reactionConfusedCount++;
    				if (txtReactionConfused != null)
    				{
    					txtReactionConfused.Text = $"\ud83d\ude15 {_reactionConfusedCount}";
    				}
    				UpdateUnderstandRate();
    				TriggerFloatingEmoji("\ud83d\ude15");
    			});
    		}
    	}

    	private void UpdateUnderstandRate()
    	{
    		if (prgUnderstandRate != null && txtUnderstandPercent != null)
    		{
    			double num = _reactionUnderstandCount + _reactionConfusedCount;
    			double num2 = ((num > 0.0) ? ((double)_reactionUnderstandCount / num * 100.0) : 100.0);
    			prgUnderstandRate.Value = num2;
    			txtUnderstandPercent.Text = $"{Math.Round(num2)}%";
    		}
    	}

    	private void TriggerFloatingEmoji(string emoji)
    	{
    		if (floatingReactionCanvas == null)
    		{
    			return;
    		}
    		TextBlock textBlock = new TextBlock
    		{
    			Text = emoji,
    			FontSize = 32.0,
    			IsHitTestVisible = false
    		};
    		double num = floatingReactionCanvas.ActualWidth;
    		double num2 = floatingReactionCanvas.ActualHeight;
    		if (num <= 0.0)
    		{
    			num = 800.0;
    		}
    		if (num2 <= 0.0)
    		{
    			num2 = 600.0;
    		}
    		Random random = new Random();
    		double length = random.NextDouble() * (num - 50.0) + 10.0;
    		double num3 = num2 - 40.0;
    		Canvas.SetLeft(textBlock, length);
    		Canvas.SetTop(textBlock, num3);
    		floatingReactionCanvas.Children.Add(textBlock);
    		double value = Math.Max(40.0, num3 - 300.0);
    		DoubleAnimation doubleAnimation = new DoubleAnimation
    		{
    			From = num3,
    			To = value,
    			Duration = TimeSpan.FromSeconds(2.0),
    			EasingFunction = new CubicEase
    			{
    				EasingMode = EasingMode.EaseOut
    			}
    		};
    		DoubleAnimation animation = new DoubleAnimation
    		{
    			From = 1.0,
    			To = 0.0,
    			Duration = TimeSpan.FromSeconds(2.0),
    			EasingFunction = new CubicEase
    			{
    				EasingMode = EasingMode.EaseIn
    			}
    		};
    		doubleAnimation.Completed += delegate
    		{
    			try
    			{
    				floatingReactionCanvas.Children.Remove(textBlock);
    			}
    			catch
    			{
    			}
    		};
    		textBlock.BeginAnimation(Canvas.TopProperty, doubleAnimation);
    		textBlock.BeginAnimation(UIElement.OpacityProperty, animation);
    	}

    	private void CleanOrphanedCanvasImages()
    	{
    		try
    		{
    			string picturesDir = AppPaths.PicturesDir;
    			if (!Directory.Exists(picturesDir))
    			{
    				return;
    			}
    			// → ClassroomAppContext
    			HashSet<string> hashSet = (from p in (from c in ClassroomAppContext.Db.LessonContents
    					where c.ContentType.StartsWith("Image")
    					select c.Data).ToList()
    				select System.IO.Path.GetFullPath(p).ToLowerInvariant()).ToHashSet();
    			string[] files = Directory.GetFiles(picturesDir, "Lesson_*_Board_*.png");
    			int num = 0;
    			string[] array = files;
    			foreach (string path in array)
    			{
    				string item = System.IO.Path.GetFullPath(path).ToLowerInvariant();
    				if (!hashSet.Contains(item))
    				{
    					try
    					{
    						File.Delete(path);
    						num++;
    					}
    					catch
    					{
    					}
    				}
    			}
    			if (num > 0)
    			{
    				Log.Information("Cleaned up {Count} orphaned blackboard images from disk", num);
    			}
    		}
    		catch (Exception ex)
    		{
    			Log.Warning("CleanOrphanedImages error: {Err}", ex.Message);
    		}
    	}

    	private void LoadLessonOnCanvas(int lessonId)
    	{
    		_reactionUnderstandCount = 0;
    		_reactionConfusedCount = 0;
    		if (txtReactionUnderstand != null)
    		{
    			txtReactionUnderstand.Text = "\ud83d\udc4d 0";
    		}
    		if (txtReactionConfused != null)
    		{
    			txtReactionConfused.Text = "\ud83d\ude15 0";
    		}
    		if (prgUnderstandRate != null)
    		{
    			prgUnderstandRate.Value = 100.0;
    		}
    		if (txtUnderstandPercent != null)
    		{
    			txtUnderstandPercent.Text = "100%";
    		}
    		if (reactionCounterPanel != null)
    		{
    			reactionCounterPanel.Visibility = ((lessonId <= 0) ? Visibility.Collapsed : Visibility.Visible);
    		}
    		try
    		{
    			// → ClassroomAppContext
    			Lesson lesson = ClassroomAppContext.Db.Lessons.Find(lessonId);
    			if (lesson == null)
    			{
    				return;
    			}
    			List<LessonContent> list = (from c in ClassroomAppContext.Db.LessonContents
    				where c.LessonId == lessonId
    				orderby c.SortOrder
    				select c).ToList();
    			if (!list.Any())
    			{
    				return;
    			}
    			lessonOverlay.Visibility = Visibility.Visible;
    			lessonOverlay.Children.Clear();
    			Border border = new Border
    			{
    				Background = new SolidColorBrush(Color.FromArgb(230, 25, 118, 210)),
    				CornerRadius = new CornerRadius(0.0, 0.0, 8.0, 8.0),
    				Padding = new Thickness(20.0, 12.0, 20.0, 12.0),
    				Margin = new Thickness(0.0, 0.0, 0.0, 8.0)
    			};
    			StackPanel stackPanel = new StackPanel();
    			stackPanel.Children.Add(new TextBlock
    			{
    				Text = "\ud83d\udcd6 " + lesson.Title,
    				FontSize = 22.0,
    				FontWeight = FontWeights.Bold,
    				Foreground = Brushes.White,
    				TextWrapping = TextWrapping.Wrap
    			});
    			stackPanel.Children.Add(new TextBlock
    			{
    				Text = $"{lesson.Subject} — {lesson.Grade} | {list.Count} phần",
    				FontSize = 13.0,
    				Foreground = new SolidColorBrush(Color.FromRgb(187, 222, 251)),
    				Margin = new Thickness(0.0, 2.0, 0.0, 0.0)
    			});
    			border.Child = stackPanel;
    			lessonOverlay.Children.Add(border);
    			_totalBlocks = list.Count;
    			for (int num = 0; num < list.Count; num++)
    			{
    				LessonContent lessonContent = list[num];
    				Border border2 = new Border
    				{
    					Tag = num,
    					Background = new SolidColorBrush(Color.FromArgb(240, byte.MaxValue, byte.MaxValue, byte.MaxValue)),
    					CornerRadius = new CornerRadius(8.0),
    					Padding = new Thickness(18.0, 14.0, 18.0, 14.0),
    					Margin = new Thickness(8.0, 0.0, 8.0, 8.0),
    					BorderBrush = Brushes.Transparent,
    					BorderThickness = new Thickness(2.0)
    				};
    				border2.MouseLeftButtonDown += BlockBorder_Click;
    				border2.Cursor = Cursors.Hand;
    				if (lessonContent.ContentType == "Image")
    				{
    					if (File.Exists(lessonContent.Data))
    					{
    						try
    						{
    							BitmapImage bitmapImage = new BitmapImage();
    							bitmapImage.BeginInit();
    							bitmapImage.UriSource = new Uri(lessonContent.Data, UriKind.Absolute);
    							bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
    							bitmapImage.EndInit();
    							bitmapImage.Freeze();
    							Image element = new Image
    							{
    								Source = bitmapImage,
    								Stretch = Stretch.Uniform,
    								MaxWidth = 800.0,
    								MaxHeight = 600.0,
    								HorizontalAlignment = HorizontalAlignment.Center
    							};
    							string text = "Bảng vẽ";
    							string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(lessonContent.Data);
    							string[] array = fileNameWithoutExtension.Split('_');
    							if (array.Length >= 5)
    							{
    								text = string.Join("_", array.Skip(4));
    							}
    							TextBlock element2 = new TextBlock
    							{
    								Text = "\ud83d\uddbc\ufe0f Slide: " + text,
    								FontSize = 12.0,
    								FontWeight = FontWeights.Bold,
    								Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
    								Margin = new Thickness(0.0, 0.0, 0.0, 4.0)
    							};
    							StackPanel stackPanel2 = new StackPanel();
    							stackPanel2.Children.Add(element2);
    							stackPanel2.Children.Add(element);
    							border2.Child = stackPanel2;
    						}
    						catch (Exception ex)
    						{
    							border2.Child = new TextBlock
    							{
    								Text = "Lỗi tải hình ảnh: " + ex.Message,
    								Foreground = Brushes.Red
    							};
    						}
    					}
    					else
    					{
    						border2.Child = new TextBlock
    						{
    							Text = "Không tìm thấy hình ảnh: " + lessonContent.Data,
    							Foreground = Brushes.Red
    						};
    					}
    				}
    				else
    				{
    					string text2 = lessonContent.Data.Replace("\\n", "\n");
    					border2.Child = BuildCanvasRichText(text2);
    				}
    				lessonOverlay.Children.Add(border2);
    			}
    			blockNav.Visibility = Visibility.Visible;
    			_currentBlockIndex = 0;
    			UpdateBlockNav();
    			Log.Information("Lesson loaded on canvas: {Title}, {Blocks} blocks", lesson.Title, list.Count);
    		}
    		catch (Exception ex2)
    		{
    			Log.Warning("LoadLessonOnCanvas error: {Error}", ex2.Message);
    		}
    	}

    	private void PrevBlock_Click(object sender, MouseButtonEventArgs e)
    	{
    		if (_currentBlockIndex > 0)
    		{
    			_currentBlockIndex--;
    			ScrollToBlock(_currentBlockIndex);
    			UpdateBlockNav();
    		}
    	}

    	private void NextBlock_Click(object sender, MouseButtonEventArgs e)
    	{
    		if (_currentBlockIndex < _totalBlocks - 1)
    		{
    			_currentBlockIndex++;
    			ScrollToBlock(_currentBlockIndex);
    			UpdateBlockNav();
    		}
    	}

    	private void ScrollToBlock(int index)
    	{
    		if (index + 1 >= lessonOverlay.Children.Count || !(lessonOverlay.Children[index + 1] is FrameworkElement frameworkElement))
    		{
    			return;
    		}
    		for (int i = 1; i < lessonOverlay.Children.Count; i++)
    		{
    			if (lessonOverlay.Children[i] is Border border)
    			{
    				border.BorderBrush = ((i == index + 1) ? new SolidColorBrush(Color.FromRgb(25, 118, 210)) : Brushes.Transparent);
    			}
    		}
    		frameworkElement.BringIntoView();
    	}

    	private void UpdateBlockNav()
    	{
    		txtBlockNav.Text = $"{_currentBlockIndex + 1}/{_totalBlocks}";
    	}

    	private void BlockBorder_Click(object sender, MouseButtonEventArgs e)
    	{
    		if (sender is Border { Tag: var tag } && tag is int num)
    		{
    			_currentBlockIndex = num;
    			ScrollToBlock(num);
    			UpdateBlockNav();
    		}
    	}

    	private void FocusMode_Click(object sender, MouseButtonEventArgs e)
    	{
    		_focusMode = !_focusMode;
    		if (_focusMode && lessonOverlay.Children.Count > 1)
    		{
    			for (int i = 0; i < lessonOverlay.Children.Count; i++)
    			{
    				if (lessonOverlay.Children[i] is FrameworkElement frameworkElement)
    				{
    					frameworkElement.Opacity = ((i == 0) ? 0.3 : ((i - 1 == _currentBlockIndex) ? 1.0 : 0.15));
    				}
    			}
    			return;
    		}
    		foreach (FrameworkElement child in lessonOverlay.Children)
    		{
    			child.Opacity = 1.0;
    		}
    	}

    	private void Spotlight_Click(object sender, MouseButtonEventArgs e)
    	{
    		_spotlightMode = !_spotlightMode;
    		spotlightOverlay.Visibility = ((!_spotlightMode) ? Visibility.Collapsed : Visibility.Visible);
    		if (_spotlightMode)
    		{
    			spotlightOverlay.Children.Clear();
    			spotlightOverlay.Background = Brushes.Transparent;
    		}
    	}

    	private void Spotlight_MouseMove(object sender, MouseEventArgs e)
    	{
    		if (_spotlightMode)
    		{
    			Point position = e.GetPosition(spotlightOverlay);
    			double actualWidth = spotlightOverlay.ActualWidth;
    			double actualHeight = spotlightOverlay.ActualHeight;
    			double num = 120.0;
    			spotlightOverlay.Children.Clear();
    			CombinedGeometry data = new CombinedGeometry(GeometryCombineMode.Exclude, new RectangleGeometry(new Rect(0.0, 0.0, actualWidth, actualHeight)), new EllipseGeometry(position, num, num));
    			spotlightOverlay.Children.Add(new System.Windows.Shapes.Path
    			{
    				Data = data,
    				Fill = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0))
    			});
    			Ellipse element = new Ellipse
    			{
    				Width = num * 2.0 + 4.0,
    				Height = num * 2.0 + 4.0,
    				Stroke = new SolidColorBrush(Color.FromArgb(80, byte.MaxValue, byte.MaxValue, byte.MaxValue)),
    				StrokeThickness = 2.0,
    				Fill = Brushes.Transparent
    			};
    			Canvas.SetLeft(element, position.X - num - 2.0);
    			Canvas.SetTop(element, position.Y - num - 2.0);
    			spotlightOverlay.Children.Add(element);
    		}
    	}

    	private void Spotlight_Cancel(object sender, MouseButtonEventArgs e)
    	{
    		_spotlightMode = false;
    		spotlightOverlay.Visibility = Visibility.Collapsed;
    	}

    	private void FocusOverlay_MouseDown(object sender, MouseButtonEventArgs e)
    	{
    		focusOverlay.Visibility = Visibility.Collapsed;
    		_focusMode = false;
    		foreach (FrameworkElement child in lessonOverlay.Children)
    		{
    			child.Opacity = 1.0;
    		}
    	}

    	private void FocusOverlay_MouseMove(object sender, MouseEventArgs e)
    	{
    	}

    	private void FocusOverlay_MouseUp(object sender, MouseButtonEventArgs e)
    	{
    	}

    	private static FontFamily GetInterFontFamily()
    	{
    		try
    		{
    			if (Application.Current != null)
    			{
    				var font = Application.Current.FindResource("InterFont") as FontFamily;
    				if (font != null) return font;
    			}
    		}
    		catch
    		{
    			// silent fallback
    		}
    		return new FontFamily("Inter, Segoe UI, Arial");
    	}

    	private static RichTextBox BuildCanvasRichText(string text)
    	{
    		RichTextBox richTextBox = new RichTextBox
    		{
    			BorderThickness = new Thickness(0.0),
    			IsReadOnly = true,
    			Background = Brushes.Transparent,
    			Padding = new Thickness(0.0),
    			FontFamily = GetInterFontFamily(),
    			FontSize = 15.0,
    			IsHitTestVisible = false
    		};
    		FlowDocument flowDocument = new FlowDocument
    		{
    			PagePadding = new Thickness(0.0)
    		};
    		string[] array = text.Split('\n');
    		foreach (string text2 in array)
    		{
    			string text3 = text2.TrimEnd('\r');
    			if (!string.IsNullOrWhiteSpace(text3))
    			{
    				Paragraph paragraph = new Paragraph
    				{
    					Margin = new Thickness(0.0, 2.0, 0.0, 2.0)
    				};
    				if (text3.StartsWith("\ud83d\udccc") || text3.StartsWith("✍\ufe0f") || text3.StartsWith("⚡"))
    				{
    					paragraph.Inlines.Add(new Run(text3)
    					{
    						FontSize = 18.0,
    						FontWeight = FontWeights.Bold,
    						Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210))
    					});
    					paragraph.Margin = new Thickness(0.0, 6.0, 0.0, 4.0);
    				}
    				else if (text3.StartsWith("\ud83d\udcd6"))
    				{
    					paragraph.Inlines.Add(new Run(text3)
    					{
    						FontSize = 17.0,
    						FontWeight = FontWeights.Bold,
    						Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33))
    					});
    					paragraph.Margin = new Thickness(0.0, 6.0, 0.0, 4.0);
    				}
    				else if (text3.StartsWith("\ud83d\udcdd"))
    				{
    					paragraph.Inlines.Add(new Run(text3)
    					{
    						FontSize = 16.0,
    						FontWeight = FontWeights.SemiBold,
    						Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50))
    					});
    				}
    				else if (text3.TrimStart().StartsWith("•") || text3.TrimStart().StartsWith("\ud83d\udd39") || text3.TrimStart().StartsWith("✅") || text3.TrimStart().StartsWith("❌"))
    				{
    					paragraph.Margin = new Thickness(14.0, 1.0, 0.0, 1.0);
    					paragraph.Inlines.Add(new Run(text3)
    					{
    						FontSize = 15.0,
    						Foreground = new SolidColorBrush(Color.FromRgb(55, 55, 55))
    					});
    				}
    				else
    				{
    					paragraph.Inlines.Add(new Run(text3)
    					{
    						FontSize = 15.0,
    						Foreground = new SolidColorBrush(Color.FromRgb(55, 55, 55))
    					});
    				}
    				flowDocument.Blocks.Add(paragraph);
    			}
    		}
    		richTextBox.Document = flowDocument;
    		return richTextBox;
    	}

    	private void SyncStrokesToStudents()
    	{
    		try
    		{
    			if (!(Application.Current is App { NetworkService: not null } app) || !ClassroomAppContext.Network.IsBroadcasting)
    			{
    				return;
    			}
    			ClassroomAppContext.Network.SendCommandAsync("CMD|WHITEBOARD_CLEAR|ALL");
    			double w = drawCanvas.ActualWidth;
    			double h = drawCanvas.ActualHeight;
    			foreach (Stroke stroke in drawCanvas.Strokes)
    			{
    				string value = stroke.DrawingAttributes.Color.ToString();
    				string value2 = stroke.DrawingAttributes.Width.ToString("F1");
    				string value3 = string.Join(";", stroke.StylusPoints.Select((StylusPoint p) => $"{p.X:F1},{p.Y:F1}"));
    				string command = $"CMD|WHITEBOARD_DRAW|{value}|{value2}|{value3}|{w:F1}|{h:F1}";
    				ClassroomAppContext.Network.SendCommandAsync(command);
    			}
    			foreach (UIElement element in _shapeElements)
    			{
    				if (element is System.Windows.Shapes.Rectangle || element is System.Windows.Shapes.Ellipse)
    				{
    					double left = Canvas.GetLeft(element);
    					double top = Canvas.GetTop(element);
    					if (double.IsNaN(left)) left = 0;
    					if (double.IsNaN(top)) top = 0;
    					double width = element.RenderSize.Width;
    					double height = element.RenderSize.Height;
    					Color color = Colors.Black;
    					double strokeThickness = 1.0;
    					if (element is System.Windows.Shapes.Shape shape)
    					{
    						if (shape.Stroke is SolidColorBrush brush) color = brush.Color;
    						strokeThickness = shape.StrokeThickness;
    					}
    					string typeName = element is System.Windows.Shapes.Rectangle ? "Rectangle" : "Ellipse";
    					string coordsStr = $"{left:F1},{top:F1}|{(left + width):F1},{(top + height):F1}";
    					string command = $"CMD|WHITEBOARD_SHAPE|{typeName}|{coordsStr}|{color.ToString()}|{strokeThickness:F1}|{w:F1}|{h:F1}";
    					ClassroomAppContext.Network.SendCommandAsync(command);
    				}
    				else if (element is System.Windows.Shapes.Line line)
    				{
    					Color color = Colors.Black;
    					if (line.Stroke is SolidColorBrush brush) color = brush.Color;
    					string coordsStr = $"{line.X1:F1},{line.Y1:F1}|{line.X2:F1},{line.Y2:F1}";
    					string command = $"CMD|WHITEBOARD_SHAPE|Line|{coordsStr}|{color.ToString()}|{line.StrokeThickness:F1}|{w:F1}|{h:F1}";
    					ClassroomAppContext.Network.SendCommandAsync(command);
    				}
    				else if (element is System.Windows.Shapes.Path path)
    				{
    					Color color = Colors.Black;
    					if (path.Stroke is SolidColorBrush brush) color = brush.Color;
    					Point start = new Point(0, 0);
    					Point end = new Point(0, 0);
    					if (path.Data is GeometryGroup group && group.Children.Count > 0 && group.Children[0] is LineGeometry lg)
    					{
    						start = lg.StartPoint;
    						end = lg.EndPoint;
    					}
    					string coordsStr = $"{start.X:F1},{start.Y:F1}|{end.X:F1},{end.Y:F1}";
    					string command = $"CMD|WHITEBOARD_SHAPE|Arrow|{coordsStr}|{color.ToString()}|{path.StrokeThickness:F1}|{w:F1}|{h:F1}";
    					ClassroomAppContext.Network.SendCommandAsync(command);
    				}
    				else if (element is TextBlock tb)
    				{
    					double left = Canvas.GetLeft(tb);
    					double top = Canvas.GetTop(tb);
    					if (double.IsNaN(left)) left = 0;
    					if (double.IsNaN(top)) top = 0;
    					Color color = Colors.Black;
    					if (tb.Foreground is SolidColorBrush brush) color = brush.Color;
    					string escapedText = tb.Text.Replace("|", "\\|").Replace("\n", "\\n").Replace("\r", "\\r");
    					string command = $"CMD|WHITEBOARD_TEXT|{escapedText}|{left:F1},{top:F1}|{color.ToString()}|{tb.FontSize:F1}|{w:F1}|{h:F1}";
    					ClassroomAppContext.Network.SendCommandAsync(command);
    				}
    			}
    		}
    		catch (Exception ex)
    		{
    			Log.Warning("SyncStrokesToStudents error: {Err}", ex.Message);
    		}
    	}

	

	
    }

}
