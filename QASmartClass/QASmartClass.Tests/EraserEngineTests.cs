using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using QASmartTouch.Managers;
using QASmartTouch.Services.Canvas;
using Xunit;

namespace QASmartClass.Tests
{
    public class EraserEngineTests
    {
        private static void RunOnStaThread(Action action)
        {
            Exception? ex = null;
            var t = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    ex = e;
                }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
            if (ex != null)
            {
                throw ex;
            }
        }

        [Fact]
        public void SlicePolyline_CutsMiddleOfSegment_ReturnsTwoSubStrokes()
        {
            // Line from (0,0) to (100,0)
            var points = new PointCollection
            {
                new Point(0, 0),
                new Point(100, 0)
            };

            // Circle at (50, 0) with radius 10 (cuts between x=40 and x=60)
            var pieces = EraserEngine.SlicePolyline(points, new Point(50, 0), 10);

            Assert.Equal(2, pieces.Count);
            Assert.Equal(2, pieces[0].Count);
            Assert.Equal(0, pieces[0][0].X);
            Assert.InRange(pieces[0][1].X, 39.9, 40.1);

            Assert.Equal(2, pieces[1].Count);
            Assert.InRange(pieces[1][0].X, 59.9, 60.1);
            Assert.Equal(100, pieces[1][1].X);
        }

        [Fact]
        public void SlicePolyline_NoIntersection_ReturnsOriginalPoints()
        {
            var points = new PointCollection
            {
                new Point(0, 0),
                new Point(100, 0)
            };

            // Circle at (50, 50) with radius 10 (far away from y=0)
            var pieces = EraserEngine.SlicePolyline(points, new Point(50, 50), 10);

            Assert.Single(pieces);
            Assert.Equal(2, pieces[0].Count);
            Assert.Equal(0, pieces[0][0].X);
            Assert.Equal(100, pieces[0][1].X);
        }

        [Fact]
        public void SlicePolyline_EntireStrokeInsideCircle_ReturnsEmpty()
        {
            var points = new PointCollection
            {
                new Point(45, 0),
                new Point(55, 0)
            };

            // Circle at (50, 0) with radius 20 covers entire stroke
            var pieces = EraserEngine.SlicePolyline(points, new Point(50, 0), 20);

            Assert.Empty(pieces);
        }

        [Fact]
        public void DistanceFromPointToLineSegment_CalculatesCorrectDistance()
        {
            Point start = new Point(0, 0);
            Point end = new Point(100, 0);

            // Point directly above middle
            double dist1 = EraserEngine.DistanceFromPointToLineSegment(new Point(50, 20), start, end);
            Assert.Equal(20, dist1, 3);

            // Point beyond start
            double dist2 = EraserEngine.DistanceFromPointToLineSegment(new Point(-10, 0), start, end);
            Assert.Equal(10, dist2, 3);

            // Point beyond end
            double dist3 = EraserEngine.DistanceFromPointToLineSegment(new Point(115, 0), start, end);
            Assert.Equal(15, dist3, 3);
        }

        [Fact]
        public void EraseSession_IntermediatePiecesInSameSwipe_CorrectlyBatchesOriginalAndFinalPieces()
        {
            RunOnStaThread(() =>
            {
                var session = new EraseSession();

                var originalStroke = new Polyline();
                var piece1 = new Polyline();
                var piece2 = new Polyline();

                // Frame 1: originalStroke is removed, piece1 and piece2 are added
                session.RegisterRemovedElement(originalStroke);
                session.RegisterAddedElement(piece1);
                session.RegisterAddedElement(piece2);

                // Frame 2 (same swipe): piece1 is sliced further into subPiece1A and subPiece1B
                var subPiece1A = new Polyline();
                var subPiece1B = new Polyline();
                session.RegisterRemovedElement(piece1); // removing an element that was created in this session
                session.RegisterAddedElement(subPiece1A);
                session.RegisterAddedElement(subPiece1B);

                // Verify
                Assert.True(session.HasChanges);
                Assert.Single(session.OriginalRemovedElements);
                Assert.Contains(originalStroke, session.OriginalRemovedElements);

                // Active generated elements should only contain { piece2, subPiece1A, subPiece1B }
                Assert.Equal(3, session.ActiveGeneratedElements.Count);
                Assert.Contains(piece2, session.ActiveGeneratedElements);
                Assert.Contains(subPiece1A, session.ActiveGeneratedElements);
                Assert.Contains(subPiece1B, session.ActiveGeneratedElements);
                Assert.DoesNotContain(piece1, session.ActiveGeneratedElements);
            });
        }

        [Fact]
        public void EraseByPoint_SmoothPathFromTouch_CutsIntoPieces()
        {
            RunOnStaThread(() =>
            {
                var canvas = new Canvas();
                canvas.Measure(new Size(1000, 1000));
                canvas.Arrange(new Rect(0, 0, 1000, 1000));
                var strokeService = new StrokeService();

                // Create polyline from (0,0) to (100,0) with multiple points
                var poly = new Polyline
                {
                    Points = new PointCollection
                    {
                        new Point(0, 0),
                        new Point(25, 0),
                        new Point(50, 0),
                        new Point(75, 0),
                        new Point(100, 0)
                    },
                    Stroke = Brushes.Black,
                    StrokeThickness = 2
                };

                var smoothPath = strokeService.ConvertToSmoothPath(poly);
                Assert.NotNull(smoothPath);
                canvas.Children.Add(smoothPath);

                var eraser = new EraserEngine(canvas);
                var session = new EraseSession();

                // Erase by point in the middle (50, 0) with radius 10
                bool result = eraser.EraseByPoint(new Point(50, 0), 10, session);

                Assert.True(result);
                Assert.DoesNotContain(smoothPath, canvas.Children.Cast<UIElement>());

                var remainingElements = canvas.Children.Cast<UIElement>().ToList();
                Assert.Equal(2, remainingElements.Count);
                Assert.All(remainingElements, el => Assert.IsType<Polyline>(el));
            });
        }

        [Fact]
        public void EraseByPoint_PathWithoutTag_EvaluatesCurvesAndCutsIntoPieces()
        {
            RunOnStaThread(() =>
            {
                var canvas = new Canvas();
                canvas.Measure(new Size(1000, 1000));
                canvas.Arrange(new Rect(0, 0, 1000, 1000));
                var strokeService = new StrokeService();

                var poly = new Polyline
                {
                    Points = new PointCollection
                    {
                        new Point(0, 0),
                        new Point(25, 0),
                        new Point(50, 0),
                        new Point(75, 0),
                        new Point(100, 0)
                    },
                    Stroke = Brushes.Black,
                    StrokeThickness = 2
                };

                var smoothPath = strokeService.ConvertToSmoothPath(poly);
                Assert.NotNull(smoothPath);
                smoothPath.Tag = null; // Explicitly remove Tag to test raw curve evaluation!
                canvas.Children.Add(smoothPath);

                var eraser = new EraserEngine(canvas);
                var session = new EraseSession();

                bool result = eraser.EraseByPoint(new Point(50, 0), 10, session);

                Assert.True(result);
                Assert.DoesNotContain(smoothPath, canvas.Children.Cast<UIElement>());

                var remainingElements = canvas.Children.Cast<UIElement>().ToList();
                Assert.Equal(2, remainingElements.Count);
                Assert.All(remainingElements, el => Assert.IsType<Polyline>(el));
            });
        }

        [Fact]
        public void ObjectErased_FiresEvent_WhenElementIsErasedByStroke()
        {
            RunOnStaThread(() =>
            {
                var canvas = new Canvas();
                canvas.Measure(new Size(1000, 1000));
                canvas.Arrange(new Rect(0, 0, 1000, 1000));

                var poly = new Polyline
                {
                    Points = new PointCollection { new Point(0, 0), new Point(100, 0) },
                    Stroke = Brushes.Black,
                    StrokeThickness = 2
                };
                canvas.Children.Add(poly);

                var eraser = new EraserEngine(canvas);
                UIElement? erasedElement = null;
                eraser.ObjectErased += (s, e) =>
                {
                    erasedElement = e.Element;
                };

                bool erased = eraser.EraseByStrokeAtPoint(new Point(50, 0), 10);

                Assert.True(erased);
                Assert.Same(poly, erasedElement);
                Assert.DoesNotContain(poly, canvas.Children.Cast<UIElement>());
            });
        }

        [Fact]
        public void EraseSession_Lifecycle_CorrectlyTracksChanges()
        {
            RunOnStaThread(() =>
            {
                var canvas = new Canvas();
                canvas.Measure(new Size(1000, 1000));
                canvas.Arrange(new Rect(0, 0, 1000, 1000));

                var eraser = new EraserEngine(canvas);

                var session = eraser.StartMouseSession();
                Assert.NotNull(session);
                Assert.False(session.HasChanges);
                Assert.Same(session, eraser.CurrentMouseSession);

                var poly = new Polyline
                {
                    Points = new PointCollection { new Point(0, 0), new Point(100, 0) },
                    Stroke = Brushes.Black,
                    StrokeThickness = 2
                };
                canvas.Children.Add(poly);

                eraser.EraseByStrokeAtPoint(new Point(50, 0), 10, session);

                Assert.True(session.HasChanges);
                Assert.Single(session.OriginalRemovedElements);
                Assert.Contains(poly, session.OriginalRemovedElements);

                var endedSession = eraser.EndMouseSession();
                Assert.Same(session, endedSession);
                Assert.Null(eraser.CurrentMouseSession);
            });
        }

        [Fact]
        public void EraseByPoint_SmoothPathWithRenderTransform_SlicesAtVisualPosition()
        {
            RunOnStaThread(() =>
            {
                var strokeService = new StrokeService();
                var canvas = new Canvas();
                canvas.Measure(new Size(2000, 2000));
                canvas.Arrange(new Rect(0, 0, 2000, 2000));

                var poly = new Polyline
                {
                    Points = new PointCollection
                    {
                        new Point(0, 0),
                        new Point(25, 0),
                        new Point(50, 0),
                        new Point(75, 0),
                        new Point(100, 0)
                    },
                    Stroke = Brushes.Black,
                    StrokeThickness = 2
                };

                var smoothPath = strokeService.ConvertToSmoothPath(poly);
                Assert.NotNull(smoothPath);

                // Simulate moving the stroke with Selection Tool by (300, 400)
                smoothPath.RenderTransform = new TranslateTransform(300, 400);
                canvas.Children.Add(smoothPath);

                var eraser = new EraserEngine(canvas);
                var session = new EraseSession();

                // Erasing at visual position (350, 400) should hit and slice the moved stroke!
                bool result = eraser.EraseByPoint(new Point(350, 400), 10, session);

                Assert.True(result);
                Assert.DoesNotContain(smoothPath, canvas.Children.Cast<UIElement>());

                var remainingElements = canvas.Children.Cast<UIElement>().OfType<Polyline>().ToList();
                Assert.Equal(2, remainingElements.Count);

                // The newly generated pieces should be at the moved visual position (~300..400)
                Assert.True(remainingElements[0].Points.All(p => p.X >= 290 && p.X <= 410 && p.Y >= 390 && p.Y <= 410));
                Assert.True(remainingElements[1].Points.All(p => p.X >= 290 && p.X <= 410 && p.Y >= 390 && p.Y <= 410));
            });
        }

        [Fact]
        public void EraseByStrokeAtPoint_SmoothPathWithRenderTransform_ErasesAtVisualPosition()
        {
            RunOnStaThread(() =>
            {
                var strokeService = new StrokeService();
                var canvas = new Canvas();
                canvas.Measure(new Size(2000, 2000));
                canvas.Arrange(new Rect(0, 0, 2000, 2000));

                var poly = new Polyline
                {
                    Points = new PointCollection
                    {
                        new Point(0, 0),
                        new Point(50, 0),
                        new Point(100, 0)
                    },
                    Stroke = Brushes.Black,
                    StrokeThickness = 2
                };

                var smoothPath = strokeService.ConvertToSmoothPath(poly);
                Assert.NotNull(smoothPath);

                // Simulate moving the stroke with Selection Tool by (500, 200)
                smoothPath.RenderTransform = new TranslateTransform(500, 200);
                canvas.Children.Add(smoothPath);

                var eraser = new EraserEngine(canvas);

                // Erasing at old untransformed position (50, 0) should NOT erase the moved stroke
                bool missResult = eraser.EraseByStrokeAtPoint(new Point(50, 0), 5);
                Assert.False(missResult);
                Assert.Contains(smoothPath, canvas.Children.Cast<UIElement>());

                // Erasing at new visual position (550, 200) SHOULD erase the moved stroke
                bool hitResult = eraser.EraseByStrokeAtPoint(new Point(550, 200), 10);
                Assert.True(hitResult);
                Assert.DoesNotContain(smoothPath, canvas.Children.Cast<UIElement>());
            });
        }

        [Fact]
        public void EraserEngine_NeverErases_BackgroundLayerOrSystemUI()
        {
            RunOnStaThread(() =>
            {
                var canvas = new Canvas();
                canvas.Measure(new Size(2000, 2000));
                canvas.Arrange(new Rect(0, 0, 2000, 2000));

                var bgRect = new Rectangle
                {
                    Width = 2000,
                    Height = 2000,
                    Tag = "BackgroundLayer",
                    Fill = Brushes.DarkGreen
                };
                canvas.Children.Add(bgRect);

                var patternRect = new Rectangle
                {
                    Width = 2000,
                    Height = 2000,
                    Tag = "BackgroundLayer",
                    Fill = Brushes.Transparent
                };
                canvas.Children.Add(patternRect);

                var selectionBox = new Canvas
                {
                    Tag = "SelectionBox",
                    Width = 300,
                    Height = 300
                };
                Panel.SetZIndex(selectionBox, 9000);
                canvas.Children.Add(selectionBox);

                var eraser = new EraserEngine(canvas);
                var session = new EraseSession();

                // Erasing anywhere on the board should NOT remove bgRect, patternRect, or selectionBox
                bool pointResult = eraser.EraseByPoint(new Point(500, 500), 50, session);
                Assert.False(pointResult);
                Assert.Contains(bgRect, canvas.Children.Cast<UIElement>());
                Assert.Contains(patternRect, canvas.Children.Cast<UIElement>());
                Assert.Contains(selectionBox, canvas.Children.Cast<UIElement>());

                bool strokeResult = eraser.EraseByStrokeAtPoint(new Point(500, 500), 50);
                Assert.False(strokeResult);
                Assert.Contains(bgRect, canvas.Children.Cast<UIElement>());
                Assert.Contains(patternRect, canvas.Children.Cast<UIElement>());
                Assert.Contains(selectionBox, canvas.Children.Cast<UIElement>());
            });
        }
    }
}
