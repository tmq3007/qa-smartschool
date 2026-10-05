using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Collections.Generic;
using Xunit;
using QASmartClass.Services.Canvas;

namespace QASmartClass.Tests
{
    public class V42UndoRedoMasterStepsTests
    {
        [Fact]
        public void TestMethod_AddStrokeCommand_Undo_Redo()
        {
            var thread = new System.Threading.Thread(() =>
            {
                // Arrange
                var inkCanvas = new InkCanvas();
                var stroke = new Stroke(new StylusPointCollection(new[] { new StylusPoint(10, 10), new StylusPoint(20, 20) }));
                var command = new AddStrokeCommand(inkCanvas, stroke);

                // Act - Execute (Add stroke)
                command.Execute();
                Assert.True(inkCanvas.Strokes.Contains(stroke));

                // Act - Unexecute (Undo)
                command.Unexecute();
                Assert.False(inkCanvas.Strokes.Contains(stroke));

                // Act - Execute again (Redo)
                command.Execute();
                Assert.True(inkCanvas.Strokes.Contains(stroke));
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        [Fact]
        public void TestMethod_AddShapeCommand_Undo_Redo()
        {
            var thread = new System.Threading.Thread(() =>
            {
                // Arrange
                var shapeCanvas = new Canvas();
                var rectangle = new Rectangle { Width = 100, Height = 50 };
                var shapeElementsList = new List<UIElement>();
                var command = new AddShapeCommand(shapeCanvas, rectangle, shapeElementsList);

                // Act - Execute (Add shape)
                command.Execute();
                Assert.True(shapeCanvas.Children.Contains(rectangle));
                Assert.Contains(rectangle, shapeElementsList);

                // Act - Unexecute (Undo)
                command.Unexecute();
                Assert.False(shapeCanvas.Children.Contains(rectangle));
                Assert.DoesNotContain(rectangle, shapeElementsList);

                // Act - Execute again (Redo)
                command.Execute();
                Assert.True(shapeCanvas.Children.Contains(rectangle));
                Assert.Contains(rectangle, shapeElementsList);
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        [Fact]
        public void TestMethod_AddTextCommand_Undo_Redo()
        {
            var thread = new System.Threading.Thread(() =>
            {
                // Arrange
                var shapeCanvas = new Canvas();
                var textBlock = new TextBlock { Text = "QA SmartClass" };
                var shapeElementsList = new List<UIElement>();
                var command = new AddTextCommand(shapeCanvas, textBlock, shapeElementsList);

                // Act - Execute (Add text)
                command.Execute();
                Assert.True(shapeCanvas.Children.Contains(textBlock));
                Assert.Contains(textBlock, shapeElementsList);

                // Act - Unexecute (Undo)
                command.Unexecute();
                Assert.False(shapeCanvas.Children.Contains(textBlock));
                Assert.DoesNotContain(textBlock, shapeElementsList);

                // Act - Execute again (Redo)
                command.Execute();
                Assert.True(shapeCanvas.Children.Contains(textBlock));
                Assert.Contains(textBlock, shapeElementsList);
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        [Fact]
        public void TestMethod_ClearCanvasCommand_Undo_Redo()
        {
            var thread = new System.Threading.Thread(() =>
            {
                // Arrange
                var inkCanvas = new InkCanvas();
                var shapeCanvas = new Canvas();
                var shapeElementsList = new List<UIElement>();

                var stroke = new Stroke(new StylusPointCollection(new[] { new StylusPoint(5, 5) }));
                inkCanvas.Strokes.Add(stroke);

                var rect = new Rectangle();
                shapeCanvas.Children.Add(rect);
                shapeElementsList.Add(rect);

                var command = new ClearCanvasCommand(inkCanvas, shapeCanvas, shapeElementsList);

                // Act - Execute (Clear canvas)
                command.Execute();
                Assert.Empty(inkCanvas.Strokes);
                Assert.Empty(shapeCanvas.Children);
                Assert.Empty(shapeElementsList);

                // Act - Unexecute (Undo clear)
                command.Unexecute();
                Assert.Equal(1, inkCanvas.Strokes.Count);
                Assert.True(shapeCanvas.Children.Contains(rect));
                Assert.Contains(rect, shapeElementsList);
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        [Fact]
        public void TestMethod_StackLimit_TrimStackBottom()
        {
            // Arrange
            var stack = new Stack<int>();
            for (int i = 1; i <= 150; i++)
            {
                stack.Push(i);
            }

            // Act - Trim to max 100 elements
            int maxCount = 100;
            if (stack.Count > maxCount)
            {
                var list = new List<int>(stack);
                list = list.GetRange(0, maxCount);
                stack.Clear();
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    stack.Push(list[i]);
                }
            }

            // Assert
            Assert.Equal(100, stack.Count);
            Assert.Equal(150, stack.Peek()); // Top element remains the newest (150)
            
            // The oldest elements (1 to 50) should have been trimmed.
            // Bottom of the stack (first popped at the end) should be 51.
            var elements = stack.ToArray();
            Assert.Equal(51, elements[99]); // Index 99 is the bottom of stack (oldest element remaining)
        }

        [Fact]
        public void TestMethod_CanvasPage_Undo_Redo_10_Strokes()
        {
            var thread = new System.Threading.Thread(() =>
            {
                // Arrange
                var page = new QASmartClass.Classroom.Views.CanvasPage(0);
                var inkCanvas = page.drawCanvas;
                var undoStackField = typeof(QASmartClass.Classroom.Views.CanvasPage).GetField("_undoStack", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var redoStackField = typeof(QASmartClass.Classroom.Views.CanvasPage).GetField("_redoStack", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var pushCommandMethod = typeof(QASmartClass.Classroom.Views.CanvasPage).GetMethod("PushCommand", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                var undoStack = (Stack<IUndoableCommand>)undoStackField.GetValue(page);
                var redoStack = (Stack<IUndoableCommand>)redoStackField.GetValue(page);

                var undoClickMethod = typeof(QASmartClass.Classroom.Views.CanvasPage).GetMethod("Undo_Click", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var redoClickMethod = typeof(QASmartClass.Classroom.Views.CanvasPage).GetMethod("Redo_Click", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                // Act - Draw 10 strokes
                var strokesList = new List<Stroke>();
                for (int i = 0; i < 10; i++)
                {
                    var stroke = new Stroke(new StylusPointCollection(new[] { new StylusPoint(i * 10, i * 10), new StylusPoint(i * 10 + 5, i * 10 + 5) }));
                    strokesList.Add(stroke);
                    inkCanvas.Strokes.Add(stroke);
                    
                    var command = new AddStrokeCommand(inkCanvas, stroke);
                    pushCommandMethod.Invoke(page, new object[] { command });
                }

                // Assert initial state
                Assert.Equal(10, inkCanvas.Strokes.Count);
                Assert.Equal(10, undoStack.Count);
                Assert.Empty(redoStack);
                Assert.True(page.btnUndo.IsEnabled);
                Assert.False(page.btnRedo.IsEnabled);

                // Act - Undo 10 times
                for (int i = 0; i < 10; i++)
                {
                    undoClickMethod.Invoke(page, new object[] { null, null });
                }

                // Assert after 10 Undos
                Assert.Empty(inkCanvas.Strokes);
                Assert.Empty(undoStack);
                Assert.Equal(10, redoStack.Count);
                Assert.False(page.btnUndo.IsEnabled);
                Assert.True(page.btnRedo.IsEnabled);

                // Act - Draw a new 11th stroke (accidental or new action)
                var stroke11 = new Stroke(new StylusPointCollection(new[] { new StylusPoint(100, 100), new StylusPoint(105, 105) }));
                inkCanvas.Strokes.Add(stroke11);
                var command11 = new AddStrokeCommand(inkCanvas, stroke11);
                pushCommandMethod.Invoke(page, new object[] { command11 });

                // Assert that the Redo stack is NOT cleared!
                Assert.Equal(10, redoStack.Count);
                Assert.Equal(1, undoStack.Count);
                Assert.True(page.btnUndo.IsEnabled);
                Assert.True(page.btnRedo.IsEnabled);

                // Act - Redo 10 times
                for (int i = 0; i < 10; i++)
                {
                    redoClickMethod.Invoke(page, new object[] { null, null });
                }

                // Assert after 10 Redos: now we should have all 11 strokes!
                Assert.Equal(11, inkCanvas.Strokes.Count);
                Assert.Equal(11, undoStack.Count);
                Assert.Empty(redoStack);
                Assert.True(page.btnUndo.IsEnabled);
                Assert.False(page.btnRedo.IsEnabled);
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        [Fact]
        public void TestMethod_StudentWhiteboard_Undo_Redo_Tolerant()
        {
            var thread = new System.Threading.Thread(() =>
            {
                var page = new QASmartClass.StudentClient.Views.StudentLocalWhiteboardPage();
                var inkCanvas = page.localInkCanvas;
                
                // Get private stacks via reflection
                var undoStack = (Stack<QASmartClass.Services.Canvas.IUndoableCommand>)typeof(QASmartClass.StudentClient.Views.StudentLocalWhiteboardPage)
                    .GetField("_undoStack", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .GetValue(page);
                var redoStack = (Stack<QASmartClass.Services.Canvas.IUndoableCommand>)typeof(QASmartClass.StudentClient.Views.StudentLocalWhiteboardPage)
                    .GetField("_redoStack", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .GetValue(page);

                var undoClickMethod = typeof(QASmartClass.StudentClient.Views.StudentLocalWhiteboardPage)
                    .GetMethod("Undo_Click", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var redoClickMethod = typeof(QASmartClass.StudentClient.Views.StudentLocalWhiteboardPage)
                    .GetMethod("Redo_Click", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                // Add 5 strokes
                for (int i = 0; i < 5; i++)
                {
                    var stroke = new Stroke(new StylusPointCollection(new[] { new StylusPoint(i * 10, i * 10), new StylusPoint(i * 10 + 5, i * 10 + 5) }));
                    inkCanvas.Strokes.Add(stroke);
                }

                // Assert initial state
                Assert.Equal(5, inkCanvas.Strokes.Count);
                Assert.Equal(5, undoStack.Count);
                Assert.Empty(redoStack);

                // Undo 5 times
                for (int i = 0; i < 5; i++)
                {
                    undoClickMethod.Invoke(page, new object[] { null, null });
                }

                // Assert after 5 undos
                Assert.Empty(inkCanvas.Strokes);
                Assert.Empty(undoStack);
                Assert.Equal(5, redoStack.Count);

                // Add a new 6th stroke (e.g. accidental touch)
                var stroke6 = new Stroke(new StylusPointCollection(new[] { new StylusPoint(100, 100), new StylusPoint(105, 105) }));
                inkCanvas.Strokes.Add(stroke6);

                // Assert that the Redo stack is NOT cleared!
                Assert.Equal(5, redoStack.Count);
                Assert.Equal(1, undoStack.Count);

                // Redo 5 times
                for (int i = 0; i < 5; i++)
                {
                    redoClickMethod.Invoke(page, new object[] { null, null });
                }

                // Assert after redos: now we should have 6 strokes
                Assert.Equal(6, inkCanvas.Strokes.Count);
                Assert.Equal(6, undoStack.Count);
                Assert.Empty(redoStack);
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        [Fact]
        public void TestMethod_Dashboard_Erase_Undo_Redo()
        {
            var thread = new System.Threading.Thread(() =>
            {
                var dashboard = new QASmartTouch.Forms.Form2_MainDashboard();
                var canvas = dashboard.MainInteractiveBoard;
                
                // Get private stacks via reflection
                var undoStack = (Stack<QASmartTouch.Forms.UndoRedoAction>)typeof(QASmartTouch.Forms.Form2_MainDashboard)
                    .GetField("_undoStack", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .GetValue(dashboard);
                var redoStack = (Stack<QASmartTouch.Forms.UndoRedoAction>)typeof(QASmartTouch.Forms.Form2_MainDashboard)
                    .GetField("_redoStack", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .GetValue(dashboard);

                var recordAddAction = typeof(QASmartTouch.Forms.Form2_MainDashboard)
                    .GetMethod("RecordAddAction", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var recordRemoveAction = typeof(QASmartTouch.Forms.Form2_MainDashboard)
                    .GetMethod("RecordRemoveAction", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var undoClickMethod = typeof(QASmartTouch.Forms.Form2_MainDashboard)
                    .GetMethod("btn3_Undo_Click", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                int initialCount = canvas.Children.Count;

                // 1. Create a Polyline (simulating drawn stroke) and add to canvas
                var polyline = new System.Windows.Shapes.Polyline
                {
                    Stroke = System.Windows.Media.Brushes.Black,
                    StrokeThickness = 2
                };
                polyline.Points.Add(new Point(10, 10));
                polyline.Points.Add(new Point(20, 20));
                
                canvas.Children.Add(polyline);
                recordAddAction.Invoke(dashboard, new object[] { polyline, "Draw stroke" });

                Assert.Equal(initialCount + 1, canvas.Children.Count);
                Assert.Equal(1, undoStack.Count);

                // 2. Erase it (simulating eraser)
                canvas.Children.Remove(polyline);
                recordRemoveAction.Invoke(dashboard, new object[] { polyline, "Erase stroke" });

                Assert.Equal(initialCount, canvas.Children.Count);
                Assert.Equal(2, undoStack.Count);

                // 3. Click Undo (should restore the erased stroke!)
                undoClickMethod.Invoke(dashboard, new object[] { null, null });

                // Verify stroke is restored back on canvas!
                Assert.Equal(initialCount + 1, canvas.Children.Count);
                Assert.True(canvas.Children.Contains(polyline));
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        [Fact]
        public void TestMethod_Dashboard_StackLimit_Trim()
        {
            var thread = new System.Threading.Thread(() =>
            {
                var dashboard = new QASmartTouch.Forms.Form2_MainDashboard();
                var canvas = dashboard.MainInteractiveBoard;

                var recordAddAction = typeof(QASmartTouch.Forms.Form2_MainDashboard)
                    .GetMethod("RecordAddAction", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                // Add 105 real strokes (not accidental ones, so they must be larger than 5x5 pixels)
                var strokesList = new List<System.Windows.Shapes.Polyline>();
                for (int i = 0; i < 105; i++)
                {
                    var polyline = new System.Windows.Shapes.Polyline
                    {
                        Stroke = System.Windows.Media.Brushes.Black,
                        StrokeThickness = 2
                    };
                    polyline.Points.Add(new Point(0, 0));
                    polyline.Points.Add(new Point(100, 100)); // 100x100 pixels, definitely not accidental!
                    
                    canvas.Children.Add(polyline);
                    recordAddAction.Invoke(dashboard, new object[] { polyline, $"Draw stroke {i}" });
                    strokesList.Add(polyline);
                }

                // Retrieve the actual stack instance from the field AFTER all modifications have occurred
                var undoStack = (Stack<QASmartTouch.Forms.UndoRedoAction>)typeof(QASmartTouch.Forms.Form2_MainDashboard)
                    .GetField("_undoStack", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .GetValue(dashboard);

                // Get MAX_UNDO_LEVELS dynamically
                var maxUndoLevelsProp = typeof(QASmartTouch.Forms.Form2_MainDashboard)
                    .GetProperty("MAX_UNDO_LEVELS", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                int maxUndoLevels = (int)maxUndoLevelsProp.GetValue(dashboard);

                // Capped at max undo levels
                Assert.Equal(maxUndoLevels, undoStack.Count);

                // The top of the stack must be the last stroke added (stroke 104)
                var topAction = undoStack.Peek();
                Assert.Equal(strokesList[104], topAction.Element);

                // The bottom of the stack (first element in ToList() reversed, which is the oldest action remaining)
                int discardedCount = 105 - maxUndoLevels;
                var undoList = undoStack.ToList();
                Assert.Equal(strokesList[discardedCount], undoList[undoList.Count - 1].Element);
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        // ========================================================================
        // ✅ PHASE E: 5 Unit Tests bổ sung theo kế hoạch nâng cấp
        // ========================================================================

        /// <summary>
        /// E.2.1: Vẽ nét chủ ý (real drawing, >5×5px) SAU KHI Undo
        /// → Redo stack PHẢI bị xóa
        /// </summary>
        [Fact]
        public void TestMethod_Dashboard_RealDrawing_ClearsRedoStack()
        {
            var thread = new System.Threading.Thread(() =>
            {
                var dashboard = new QASmartTouch.Forms.Form2_MainDashboard();
                var canvas = dashboard.MainInteractiveBoard;

                var recordAddAction = typeof(QASmartTouch.Forms.Form2_MainDashboard)
                    .GetMethod("RecordAddAction", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var undoClickMethod = typeof(QASmartTouch.Forms.Form2_MainDashboard)
                    .GetMethod("btn3_Undo_Click", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                // 1. Vẽ 3 nét lớn (chủ ý)
                for (int i = 0; i < 3; i++)
                {
                    var polyline = new System.Windows.Shapes.Polyline
                    {
                        Stroke = System.Windows.Media.Brushes.Black,
                        StrokeThickness = 2
                    };
                    polyline.Points.Add(new Point(i * 50, 0));
                    polyline.Points.Add(new Point(i * 50 + 100, 100)); // 100×100px — chủ ý rõ ràng
                    canvas.Children.Add(polyline);
                    recordAddAction.Invoke(dashboard, new object[] { polyline, $"Stroke {i}" });
                }

                // 2. Undo 3 lần → Redo stack = 3
                for (int i = 0; i < 3; i++)
                {
                    undoClickMethod.Invoke(dashboard, new object[] { null, null });
                }

                var redoStack = (Stack<QASmartTouch.Forms.UndoRedoAction>)typeof(QASmartTouch.Forms.Form2_MainDashboard)
                    .GetField("_redoStack", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .GetValue(dashboard);
                Assert.Equal(3, redoStack.Count);

                // 3. Vẽ 1 nét MỚI (chủ ý, 200×50px)
                var newPolyline = new System.Windows.Shapes.Polyline
                {
                    Stroke = System.Windows.Media.Brushes.Blue,
                    StrokeThickness = 3
                };
                newPolyline.Points.Add(new Point(0, 0));
                newPolyline.Points.Add(new Point(200, 50));
                canvas.Children.Add(newPolyline);
                recordAddAction.Invoke(dashboard, new object[] { newPolyline, "New real stroke" });

                // 4. Assert: Redo stack BỊ XÓA
                redoStack = (Stack<QASmartTouch.Forms.UndoRedoAction>)typeof(QASmartTouch.Forms.Form2_MainDashboard)
                    .GetField("_redoStack", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .GetValue(dashboard);
                Assert.Equal(0, redoStack.Count); // ← Kịch bản quan trọng nhất!

                // 5. Assert: Undo stack = 1 (chỉ nét mới)
                var undoStack = (Stack<QASmartTouch.Forms.UndoRedoAction>)typeof(QASmartTouch.Forms.Form2_MainDashboard)
                    .GetField("_undoStack", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .GetValue(dashboard);
                Assert.Equal(1, undoStack.Count);
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        /// <summary>
        /// E.2.2: Accidental touch Path (Bezier, ≤5×5px) SAU KHI Undo
        /// → Redo stack KHÔNG bị xóa (Phase A fix)
        /// </summary>
        [Fact]
        public void TestMethod_Dashboard_AccidentalPathTouch_PreservesRedoStack()
        {
            var thread = new System.Threading.Thread(() =>
            {
                var dashboard = new QASmartTouch.Forms.Form2_MainDashboard();
                var canvas = dashboard.MainInteractiveBoard;

                var recordAddAction = typeof(QASmartTouch.Forms.Form2_MainDashboard)
                    .GetMethod("RecordAddAction", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var undoClickMethod = typeof(QASmartTouch.Forms.Form2_MainDashboard)
                    .GetMethod("btn3_Undo_Click", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                // 1. Vẽ 3 nét lớn → Undo 3 → Redo stack = 3
                for (int i = 0; i < 3; i++)
                {
                    var polyline = new System.Windows.Shapes.Polyline
                    {
                        Stroke = System.Windows.Media.Brushes.Black,
                        StrokeThickness = 2
                    };
                    polyline.Points.Add(new Point(i * 50, 0));
                    polyline.Points.Add(new Point(i * 50 + 100, 100));
                    canvas.Children.Add(polyline);
                    recordAddAction.Invoke(dashboard, new object[] { polyline, $"Stroke {i}" });
                }
                for (int i = 0; i < 3; i++)
                {
                    undoClickMethod.Invoke(dashboard, new object[] { null, null });
                }

                var redoStack = (Stack<QASmartTouch.Forms.UndoRedoAction>)typeof(QASmartTouch.Forms.Form2_MainDashboard)
                    .GetField("_redoStack", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .GetValue(dashboard);
                Assert.Equal(3, redoStack.Count);

                // 2. Thêm 1 Path nhỏ (accidental touch Bezier, ≤5×5px)
                var tinyPath = new System.Windows.Shapes.Path();
                var pathGeometry = new PathGeometry();
                var figure = new PathFigure { StartPoint = new Point(50, 50) };
                figure.Segments.Add(new LineSegment(new Point(53, 53), true)); // 3×3px bounding box
                pathGeometry.Figures.Add(figure);
                tinyPath.Data = pathGeometry;
                tinyPath.Stroke = System.Windows.Media.Brushes.Black;
                tinyPath.StrokeThickness = 2;
                canvas.Children.Add(tinyPath);
                recordAddAction.Invoke(dashboard, new object[] { tinyPath, "Accidental touch path" });

                // 3. Assert: Redo stack vẫn = 3 (KHÔNG bị xóa) ← Phase A fix
                redoStack = (Stack<QASmartTouch.Forms.UndoRedoAction>)typeof(QASmartTouch.Forms.Form2_MainDashboard)
                    .GetField("_redoStack", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .GetValue(dashboard);
                Assert.Equal(3, redoStack.Count);
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        /// <summary>
        /// E.2.3: Batch erase → Undo 1 lần → tất cả elements phải quay lại canvas
        /// </summary>
        [Fact]
        public void TestMethod_Dashboard_BatchErase_UndoRestoresAll()
        {
            var thread = new System.Threading.Thread(() =>
            {
                var dashboard = new QASmartTouch.Forms.Form2_MainDashboard();
                var canvas = dashboard.MainInteractiveBoard;

                var recordAction = typeof(QASmartTouch.Forms.Form2_MainDashboard)
                    .GetMethod("RecordAction", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var undoClickMethod = typeof(QASmartTouch.Forms.Form2_MainDashboard)
                    .GetMethod("btn3_Undo_Click", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                int initialCount = canvas.Children.Count;

                // 1. Tạo 3 Polyline và thêm vào canvas
                var polylines = new List<System.Windows.Shapes.Polyline>();
                for (int i = 0; i < 3; i++)
                {
                    var p = new System.Windows.Shapes.Polyline
                    {
                        Stroke = System.Windows.Media.Brushes.Black,
                        StrokeThickness = 2
                    };
                    p.Points.Add(new Point(i * 30, 0));
                    p.Points.Add(new Point(i * 30 + 50, 50));
                    canvas.Children.Add(p);
                    polylines.Add(p);
                }
                // Record them as Add actions individually (so redo stack is cleared properly)
                foreach (var p in polylines)
                {
                    var addAction = new QASmartTouch.Forms.UndoRedoAction
                    {
                        Type = QASmartTouch.Forms.ActionType.Add,
                        Element = p,
                        Parent = canvas,
                        Description = "Add polyline"
                    };
                    recordAction.Invoke(dashboard, new object[] { addAction });
                }

                Assert.Equal(initialCount + 3, canvas.Children.Count);

                // 2. Tạo Batch Remove action (giả lập batch erase của Phase 1.1)
                var batchAction = new QASmartTouch.Forms.UndoRedoAction
                {
                    Type = QASmartTouch.Forms.ActionType.Batch,
                    Description = "Tẩy 3 nét"
                };
                foreach (var p in polylines)
                {
                    canvas.Children.Remove(p);
                    batchAction.BatchActions.Add(new QASmartTouch.Forms.UndoRedoAction
                    {
                        Type = QASmartTouch.Forms.ActionType.Remove,
                        Element = p,
                        Parent = canvas,
                        Description = $"Erase {p.GetType().Name}"
                    });
                }
                recordAction.Invoke(dashboard, new object[] { batchAction });

                Assert.Equal(initialCount, canvas.Children.Count); // Tất cả đã bị xóa

                // 3. Undo 1 lần → tất cả phải quay lại
                undoClickMethod.Invoke(dashboard, new object[] { null, null });

                Assert.Equal(initialCount + 3, canvas.Children.Count); // Khôi phục 3 polyline
                foreach (var p in polylines)
                {
                    Assert.True(canvas.Children.Contains(p));
                }
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        /// <summary>
        /// E.2.4: Student Whiteboard stack trim ở 200 (Phase C)
        /// </summary>
        [Fact]
        public void TestMethod_StudentWhiteboard_StackLimit_Trims()
        {
            var thread = new System.Threading.Thread(() =>
            {
                var page = new QASmartClass.StudentClient.Views.StudentLocalWhiteboardPage();
                // localInkCanvas là internal (x:Name mặc định) → dùng reflection
                var inkCanvas = (InkCanvas)typeof(QASmartClass.StudentClient.Views.StudentLocalWhiteboardPage)
                    .GetField("localInkCanvas", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .GetValue(page);

                // Get private stacks via reflection
                var undoStack = (Stack<QASmartClass.Services.Canvas.IUndoableCommand>)typeof(QASmartClass.StudentClient.Views.StudentLocalWhiteboardPage)
                    .GetField("_undoStack", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .GetValue(page);

                // Add 210 real strokes (must be >5×5px to avoid accidental filter)
                for (int i = 0; i < 210; i++)
                {
                    var stroke = new Stroke(new StylusPointCollection(new[] {
                        new StylusPoint(i * 10, 0),
                        new StylusPoint(i * 10 + 50, 50) // 50×50px — definitely not accidental
                    }));
                    inkCanvas.Strokes.Add(stroke);
                }

                // Assert: Stack trimmed to MAX_STUDENT_UNDO_LEVELS = 200
                Assert.Equal(200, undoStack.Count);
                Assert.Equal(210, inkCanvas.Strokes.Count); // All strokes still on canvas
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        /// <summary>
        /// E.2.5: ClearAll (Batch Remove) → Undo 1 lần → tất cả elements quay lại
        /// </summary>
        [Fact]
        public void TestMethod_Dashboard_ClearAll_UndoRestoresAll()
        {
            var thread = new System.Threading.Thread(() =>
            {
                var dashboard = new QASmartTouch.Forms.Form2_MainDashboard();
                var canvas = dashboard.MainInteractiveBoard;

                var recordAction = typeof(QASmartTouch.Forms.Form2_MainDashboard)
                    .GetMethod("RecordAction", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var undoClickMethod = typeof(QASmartTouch.Forms.Form2_MainDashboard)
                    .GetMethod("btn3_Undo_Click", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                int initialCount = canvas.Children.Count;

                // 1. Vẽ 5 nét
                var polylines = new List<System.Windows.Shapes.Polyline>();
                for (int i = 0; i < 5; i++)
                {
                    var p = new System.Windows.Shapes.Polyline
                    {
                        Stroke = System.Windows.Media.Brushes.Red,
                        StrokeThickness = 2
                    };
                    p.Points.Add(new Point(i * 20, 0));
                    p.Points.Add(new Point(i * 20 + 80, 80));
                    canvas.Children.Add(p);
                    polylines.Add(p);

                    var addAction = new QASmartTouch.Forms.UndoRedoAction
                    {
                        Type = QASmartTouch.Forms.ActionType.Add,
                        Element = p,
                        Parent = canvas,
                        Description = $"Add polyline {i}"
                    };
                    recordAction.Invoke(dashboard, new object[] { addAction });
                }
                Assert.Equal(initialCount + 5, canvas.Children.Count);

                // 2. Giả lập ClearAll bằng Batch Remove (giống Phase 0.1 đã triển khai)
                var clearAllAction = new QASmartTouch.Forms.UndoRedoAction
                {
                    Type = QASmartTouch.Forms.ActionType.Batch,
                    Description = $"Xóa toàn bộ {polylines.Count} đối tượng"
                };
                foreach (var p in polylines)
                {
                    canvas.Children.Remove(p);
                    clearAllAction.BatchActions.Add(new QASmartTouch.Forms.UndoRedoAction
                    {
                        Type = QASmartTouch.Forms.ActionType.Remove,
                        Element = p,
                        Parent = canvas,
                        Description = $"Clear all sub-item {p.GetType().Name}"
                    });
                }
                recordAction.Invoke(dashboard, new object[] { clearAllAction });

                Assert.Equal(initialCount, canvas.Children.Count); // Tất cả đã xóa

                // 3. Undo 1 lần → tất cả 5 nét phải quay lại
                undoClickMethod.Invoke(dashboard, new object[] { null, null });

                Assert.Equal(initialCount + 5, canvas.Children.Count);
                foreach (var p in polylines)
                {
                    Assert.True(canvas.Children.Contains(p), $"Polyline should be restored after ClearAll Undo");
                }
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        // ========================================================================
        // ✅ 3D SHAPES DASHED HIDDEN EDGES TESTS (PHÂN BIỆT NÉT ĐỨT/LIỀN)
        // ========================================================================

        /// <summary>
        /// Test: Vẽ khối lập phương 3D khi bật ShowHiddenEdges = true (mặc định)
        /// → Phải có ít nhất một số nét vẽ có StrokeDashArray != null (nét đứt)
        /// </summary>
        [Fact]
        public void TestMethod_3DCube_ShowHiddenEdges_True_RendersDashedLines()
        {
            var thread = new System.Threading.Thread(() =>
            {
                var dashboard = new QASmartTouch.Forms.Form2_MainDashboard();
                var canvas = dashboard.MainInteractiveBoard;

                var cubeEditor = new QASmartTouch.Forms.Form2_6_3DCubeEditor();
                
                // Set rotation to 30, 45 to ensure we have adjacent back faces and hidden edges
                typeof(QASmartTouch.Forms.Form2_6_3DCubeEditor)
                    .GetProperty("RotationX", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                    .SetValue(cubeEditor, 30.0);
                typeof(QASmartTouch.Forms.Form2_6_3DCubeEditor)
                    .GetProperty("RotationY", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                    .SetValue(cubeEditor, 45.0);

                // Invoke Apply3DCubeToCanvas via reflection
                var applyMethod = typeof(QASmartTouch.Forms.Form2_MainDashboard)
                    .GetMethod("Apply3DCubeToCanvas", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                applyMethod.Invoke(dashboard, new object[] { cubeEditor });

                // Find the added cube container canvas
                var container = canvas.Children.OfType<Canvas>().LastOrDefault();
                Assert.NotNull(container);

                var lines = container.Children.OfType<Line>().ToList();
                Assert.Equal(12, lines.Count); // Cube has 12 edges

                int dashedCount = lines.Count(l => l.StrokeDashArray != null && l.StrokeDashArray.Count > 0);
                Assert.True(dashedCount > 0, "Cube must have dashed lines for hidden edges when ShowHiddenEdges is true");
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        /// <summary>
        /// Test: Vẽ khối lập phương 3D khi tắt ShowHiddenEdges = false
        /// → Tất cả nét vẽ phải là nét liền (StrokeDashArray == null)
        /// </summary>
        [Fact]
        public void TestMethod_3DCube_ShowHiddenEdges_False_AllLinesAreSolid()
        {
            var thread = new System.Threading.Thread(() =>
            {
                var dashboard = new QASmartTouch.Forms.Form2_MainDashboard();
                var canvas = dashboard.MainInteractiveBoard;

                var cubeEditor = new QASmartTouch.Forms.Form2_6_3DCubeEditor();
                
                // Set ShowHiddenEdges property to false via reflection
                var prop = typeof(QASmartTouch.Forms.Form2_6_3DCubeEditor)
                    .GetProperty("ShowHiddenEdges", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                prop.SetValue(cubeEditor, false);

                // Invoke Apply3DCubeToCanvas via reflection
                var applyMethod = typeof(QASmartTouch.Forms.Form2_MainDashboard)
                    .GetMethod("Apply3DCubeToCanvas", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                applyMethod.Invoke(dashboard, new object[] { cubeEditor });

                // Find the added cube container canvas
                var container = canvas.Children.OfType<Canvas>().LastOrDefault();
                Assert.NotNull(container);

                // Get all lines (edges)
                var lines = container.Children.OfType<Line>().ToList();
                Assert.Equal(12, lines.Count);

                // All lines must be solid (StrokeDashArray is null)
                int dashedCount = lines.Count(l => l.StrokeDashArray != null && l.StrokeDashArray.Count > 0);
                Assert.Equal(0, dashedCount);
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        /// <summary>
        /// Test case: Dấu chấm (dot) được vẽ bằng click chuột (Polyline 2 điểm vi mô)
        /// PHẢI được ghi vào Undo stack và có thể Undo/Redo thành công.
        /// </summary>
        [Fact]
        public void TestMethod_Dashboard_DotStroke_CanBeUndoneAndRedone()
        {
            var thread = new System.Threading.Thread(() =>
            {
                var dashboard = new QASmartTouch.Forms.Form2_MainDashboard();
                var canvas = dashboard.MainInteractiveBoard;

                var recordAddAction = typeof(QASmartTouch.Forms.Form2_MainDashboard)
                    .GetMethod("RecordAddAction", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var undoClickMethod = typeof(QASmartTouch.Forms.Form2_MainDashboard)
                    .GetMethod("btn3_Undo_Click", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var redoClickMethod = typeof(QASmartTouch.Forms.Form2_MainDashboard)
                    .GetMethod("btn4_Redo_Click", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                // 1. Tạo nét chấm dot (Polyline có 2 điểm vi mô cách nhau 0.01px)
                var dotStroke = new System.Windows.Shapes.Polyline
                {
                    Stroke = System.Windows.Media.Brushes.Black,
                    StrokeThickness = 4,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round
                };
                dotStroke.Points.Add(new Point(100, 100));
                dotStroke.Points.Add(new Point(100.01, 100));
                canvas.Children.Add(dotStroke);

                recordAddAction.Invoke(dashboard, new object[] { dotStroke, "Draw stroke" });

                // 2. Kiểm tra dotStroke đã nằm trong Canvas và Undo stack
                Assert.Contains(dotStroke, canvas.Children.OfType<UIElement>());
                var undoStack = (Stack<QASmartTouch.Forms.UndoRedoAction>)typeof(QASmartTouch.Forms.Form2_MainDashboard)
                    .GetField("_undoStack", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .GetValue(dashboard);
                Assert.Equal(1, undoStack.Count);

                // 3. Thực hiện Undo -> dotStroke PHẢI bị xóa khỏi Canvas
                undoClickMethod.Invoke(dashboard, new object[] { null, null });
                Assert.DoesNotContain(dotStroke, canvas.Children.OfType<UIElement>());
                Assert.Equal(0, undoStack.Count);

                // 4. Thực hiện Redo -> dotStroke PHẢI quay trở lại Canvas
                redoClickMethod.Invoke(dashboard, new object[] { null, null });
                Assert.Contains(dotStroke, canvas.Children.OfType<UIElement>());
                Assert.Equal(1, undoStack.Count);
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        [Fact]
        public void TestMethod_GroupResize_TouchDot_MovesAlongWithGroup()
        {
            var thread = new System.Threading.Thread(() =>
            {
                // 1. Tạo nét chấm cảm ứng (Polyline -> ConvertToSmoothPath -> Path)
                var strokeService = new QASmartTouch.Services.Canvas.StrokeService();
                var poly = new Polyline
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 6,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round
                };
                poly.Points.Add(new Point(100, 100));
                poly.Points.Add(new Point(100.01, 100));
                var touchDotPath = strokeService.ConvertToSmoothPath(poly);
                Assert.NotNull(touchDotPath);

                var dotObj = new QASmartTouch.Models.SelectableObject
                {
                    Element = touchDotPath,
                    Type = QASmartTouch.Models.ObjectType.Stroke
                };
                dotObj.UpdateBounds();

                // 2. Tạo đối tượng đồng hành trong nhóm (Hình chữ nhật)
                var rect = new Rectangle { Width = 100, Height = 100 };
                var rectObj = new QASmartTouch.Models.SelectableObject
                {
                    Element = rect,
                    Position = new Point(50, 50),
                    Size = new Size(100, 100),
                    Bounds = new Rect(50, 50, 100, 100),
                    Type = QASmartTouch.Models.ObjectType.Shape
                };

                // 3. Tạo nhóm đối tượng (Group)
                var groupObj = new QASmartTouch.Models.SelectableObject
                {
                    Type = QASmartTouch.Models.ObjectType.Group,
                    IsGroup = true,
                    Position = new Point(50, 50),
                    Size = new Size(100, 100),
                    Bounds = new Rect(50, 50, 100, 100),
                    GroupMembers = new List<QASmartTouch.Models.SelectableObject> { rectObj, dotObj }
                };

                var snapshots = new Dictionary<QASmartTouch.Models.SelectableObject, (Point pos, Size size)>
                {
                    [dotObj] = (dotObj.Position, dotObj.Size),
                    [rectObj] = (rectObj.Position, rectObj.Size)
                };
                var initialStates = new Dictionary<QASmartTouch.Models.SelectableObject, QASmartTouch.Models.ElementTransformState>
                {
                    [dotObj] = QASmartTouch.Models.ElementTransformState.Create(dotObj),
                    [rectObj] = QASmartTouch.Models.ElementTransformState.Create(rectObj)
                };

                var transformService = new QASmartTouch.Services.TransformService();

                // 4. Thực hiện kéo chốt Resize từ góc BottomRight: Phóng to nhóm từ 100x100 lên 200x200
                // Anchor cố định tại TopLeft (50, 50)
                transformService.ResizeFromHandle(
                    groupObj,
                    QASmartTouch.Models.ResizeMode.BottomRight,
                    currentPoint: new Point(250, 250),
                    startPoint: new Point(150, 150),
                    originalSize: new Size(100, 100),
                    originalPosition: new Point(50, 50),
                    memberSnapshots: snapshots,
                    initialStates: initialStates
                );

                // 5. Xác minh:
                // - Dấu chấm KHÔNG bị đứng yên ở vị trí cũ (100, 100)
                // - RenderTransform của dấu chấm đã được gán TranslateTransform
                Assert.NotNull(touchDotPath.RenderTransform);
                Assert.IsType<TransformGroup>(touchDotPath.RenderTransform);
                var tg = (TransformGroup)touchDotPath.RenderTransform;
                var tt = tg.Children.OfType<TranslateTransform>().FirstOrDefault();
                Assert.NotNull(tt);

                // Với tỉ lệ phóng to gấp đôi từ chốt neo (50, 50):
                // Tâm dấu chấm ban đầu (100, 100) cách chốt neo (50, 50) là dx=50, dy=50
                // Khi phóng to 2x, tâm mới phải là 50 + 50*2 = 150 -> Tịnh tiến dX ≈ 50, dY ≈ 50
                Assert.True(Math.Abs(tt.X - 50) < 1.0, $"Dấu chấm cảm ứng phải tịnh tiến dX ≈ 50, thực tế={tt.X}");
                Assert.True(Math.Abs(tt.Y - 50) < 1.0, $"Dấu chấm cảm ứng phải tịnh tiến dY ≈ 50, thực tế={tt.Y}");

                // 6. Kiểm tra cả phương thức Resize (Pinch gesture 2 ngón tay)
                groupObj.Size = new Size(100, 100);
                groupObj.Position = new Point(50, 50);
                transformService.Resize(groupObj, new Size(200, 200));

                Assert.NotNull(touchDotPath.RenderTransform);
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
        }
    }
}
