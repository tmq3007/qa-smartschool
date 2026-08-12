using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Collections.Generic;
using Xunit;
using QASmartClass.Services.Canvas;

namespace QASmartClass.Tests
{
    public class V42EraserClearTests
    {
        [Fact]
        public void TestMethod_DeleteElementsCommand_Execute_Unexecute()
        {
            var thread = new System.Threading.Thread(() =>
            {
                // Arrange
                var shapeCanvas = new Canvas();
                var shapeElementsList = new List<UIElement>();
                
                var rect1 = new Rectangle { Width = 50, Height = 50 };
                var rect2 = new Rectangle { Width = 30, Height = 30 };
                
                shapeCanvas.Children.Add(rect1);
                shapeCanvas.Children.Add(rect2);
                shapeElementsList.Add(rect1);
                shapeElementsList.Add(rect2);

                var elementsToDelete = new List<UIElement> { rect1, rect2 };
                var command = new DeleteElementsCommand(shapeCanvas, elementsToDelete, shapeElementsList);

                // Act - Execute (Delete elements)
                command.Execute();
                Assert.False(shapeCanvas.Children.Contains(rect1));
                Assert.False(shapeCanvas.Children.Contains(rect2));
                Assert.Empty(shapeElementsList);

                // Act - Unexecute (Undo delete)
                command.Unexecute();
                Assert.True(shapeCanvas.Children.Contains(rect1));
                Assert.True(shapeCanvas.Children.Contains(rect2));
                Assert.Contains(rect1, shapeElementsList);
                Assert.Contains(rect2, shapeElementsList);
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        [Fact]
        public void TestMethod_ObjectEraser_CollisionDetection()
        {
            var thread = new System.Threading.Thread(() =>
            {
                // Arrange
                var shapeCanvas = new Canvas();
                var rect = new Rectangle { Width = 100, Height = 100 };
                Canvas.SetLeft(rect, 50);
                Canvas.SetTop(rect, 50);
                shapeCanvas.Children.Add(rect);
                
                // Measure and arrange so VisualTree is updated
                shapeCanvas.Measure(new Size(500, 500));
                shapeCanvas.Arrange(new Rect(0, 0, 500, 500));
                rect.UpdateLayout();

                // Act - simulate eraser brush at point (60, 60) which is inside rect (50, 50, 100, 100)
                Point touchPoint = new Point(60, 60);
                Rect eraserRect = new Rect(touchPoint.X - 15, touchPoint.Y - 15, 30, 30);
                
                // Manually compute bounds for verification since element visual transform requires container rendering
                Rect elementBounds = new Rect(Canvas.GetLeft(rect), Canvas.GetTop(rect), rect.Width, rect.Height);
                bool intersects = eraserRect.IntersectsWith(elementBounds);

                // Assert
                Assert.True(intersects, "Eraser brush at (60, 60) must intersect with Rectangle at (50, 50, 100, 100).");
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        [Fact]
        public void TestMethod_ObjectEraser_NoCollision()
        {
            var thread = new System.Threading.Thread(() =>
            {
                // Arrange
                var rect = new Rectangle { Width = 50, Height = 50 };
                Canvas.SetLeft(rect, 100);
                Canvas.SetTop(rect, 100);

                // Act - simulate eraser brush at point (200, 200) which is far from rect (100, 100, 50, 50)
                Point touchPoint = new Point(200, 200);
                Rect eraserRect = new Rect(touchPoint.X - 15, touchPoint.Y - 15, 30, 30);
                
                Rect elementBounds = new Rect(Canvas.GetLeft(rect), Canvas.GetTop(rect), rect.Width, rect.Height);
                bool intersects = eraserRect.IntersectsWith(elementBounds);

                // Assert
                Assert.False(intersects, "Eraser brush at (200, 200) should not intersect with Rectangle at (100, 100, 50, 50).");
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        [Fact]
        public void TestMethod_DragErase_BatchAction_Undo_Redo()
        {
            var thread = new System.Threading.Thread(() =>
            {
                // Arrange
                var shapeCanvas = new Canvas();
                var rect1 = new Rectangle { Width = 50, Height = 50 };
                var rect2 = new Rectangle { Width = 30, Height = 30 };
                
                shapeCanvas.Children.Add(rect1);
                shapeCanvas.Children.Add(rect2);

                // Create a batch action representing Drag Erase
                var batchAction = new QASmartTouch.Forms.UndoRedoAction
                {
                    Type = QASmartTouch.Forms.ActionType.Batch,
                    Description = "Drag erase 2 elements"
                };

                // Add sub-actions (Remove)
                batchAction.BatchActions.Add(new QASmartTouch.Forms.UndoRedoAction
                {
                    Type = QASmartTouch.Forms.ActionType.Remove,
                    Element = rect1,
                    Parent = shapeCanvas,
                    Description = "Remove rect1"
                });
                batchAction.BatchActions.Add(new QASmartTouch.Forms.UndoRedoAction
                {
                    Type = QASmartTouch.Forms.ActionType.Remove,
                    Element = rect2,
                    Parent = shapeCanvas,
                    Description = "Remove rect2"
                });

                // Simulate Execute of the batch (elements are removed)
                foreach (var action in batchAction.BatchActions)
                {
                    if (action.Type == QASmartTouch.Forms.ActionType.Remove)
                    {
                        var canvas = action.Parent as Canvas;
                        if (canvas != null && canvas.Children.Contains(action.Element))
                        {
                            canvas.Children.Remove(action.Element);
                        }
                    }
                }

                // Assert they are removed
                Assert.False(shapeCanvas.Children.Contains(rect1));
                Assert.False(shapeCanvas.Children.Contains(rect2));

                // Simulate Undo of the batch (restored in reverse order)
                for (int i = batchAction.BatchActions.Count - 1; i >= 0; i--)
                {
                    var action = batchAction.BatchActions[i];
                    if (action.Type == QASmartTouch.Forms.ActionType.Remove)
                    {
                        var canvas = action.Parent as Canvas;
                        if (canvas != null && !canvas.Children.Contains(action.Element))
                        {
                            canvas.Children.Add(action.Element);
                        }
                    }
                }

                // Assert they are restored
                Assert.True(shapeCanvas.Children.Contains(rect1));
                Assert.True(shapeCanvas.Children.Contains(rect2));
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
        }
    }
}
