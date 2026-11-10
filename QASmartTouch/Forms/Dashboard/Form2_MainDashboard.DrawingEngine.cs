using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using QASmartTouch.Handlers;
using QASmartTouch.Managers;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.Windows.Shapes.Path;
using System.Windows.Input;
using System.Windows.Ink;
using System.Collections.Generic;
using System.Globalization;
using System.Speech.Synthesis;
using QASmartTouch.Services;
using QASmartTouch.Helpers;
using QASmartTouch.Models;
using QASmartTouch.Controls;
using QASmartTouch.Controllers;
using QASmartTouch.Shared;
using QASmartTouch.Properties;
using System.Windows.Documents;
using QASmartTouch.Services.Tools;
using QASmartTouch.WhiteboardCore.History;
// ══════════════════════════════════════════════════════════════════
// Form2_MainDashboard.DrawingEngine.cs — Partial Class
// Dựng Preview Hình học 2D, khởi tạo nét mực theo loại bút, thuật toán
// cắt/xóa nét (Eraser) và mở nhanh Học cụ Toán học / Media.
// ══════════════════════════════════════════════════════════════════

namespace QASmartTouch.Forms
{
    public partial class Form2_MainDashboard
    {
        #region 2D Shape Preview, Stroke Creation & Eraser Engine

        private Shape? CreateShapePreview(string shapeName)
        {
            if (shapeName == "mLine")
            {
                var line = new Line
                {
                    Stroke = new SolidColorBrush(_currentPenColor),
                    StrokeThickness = _currentPenSize,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    Tag = "mLine"
                };
                Panel.SetZIndex(line, _currentInkingZIndex);
                return line;
            }
            if (shapeName == "mDashedLine")
            {
                var dashedLine = new Line
                {
                    Stroke = new SolidColorBrush(_currentPenColor),
                    StrokeThickness = _currentPenSize,
                    StrokeDashArray = new DoubleCollection { 8, 4 },
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    Tag = "mDashedLine"
                };
                Panel.SetZIndex(dashedLine, _currentInkingZIndex);
                return dashedLine;
            }

            if (SubjectShapeGeometryService.IsSubjectShape(shapeName))
            {
                var subjShape = new System.Windows.Shapes.Path
                {
                    Stroke = new SolidColorBrush(_currentPenColor),
                    StrokeThickness = _currentPenSize,
                    Fill = null,
                    StrokeLineJoin = PenLineJoin.Round,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    Tag = shapeName
                };
                Panel.SetZIndex(subjShape, _currentInkingZIndex);
                return subjShape;
            }
            Shape? shape = null;
            
            switch (shapeName)
            {
                // PHASE 1: Lines (5) - use Line element
                case "StraightLine":
                case "Line":
                    shape = new Line
                    {
                        Stroke = new SolidColorBrush(_currentPenColor),
                        StrokeThickness = _currentPenSize
                    };
                    break;

                case "DashedLine":
                    shape = new Line
                    {
                        Stroke = new SolidColorBrush(_currentPenColor),
                        StrokeThickness = _currentPenSize,
                        StrokeDashArray = new DoubleCollection { 8, 4 }
                    };
                    break;
                
                case "ArrowLine":
                case "DoubleArrowLine":
                case "ElbowConnector":
                case "CurvedConnector":
                // 3D Shapes preview using Path
                case "Cube":
                case "Sphere":
                case "Cylinder":
                case "Cone":
                case "Pyramid":
                case "Prism":
                case "Tetrahedron":
                    // Use Path for complex line shapes & 3D wireframe preview
                    shape = new System.Windows.Shapes.Path
                    {
                        Stroke = new SolidColorBrush(_currentPenColor),
                        StrokeThickness = _currentPenSize,
                        Fill = null
                    };
                    break;
                    
                // PHASE 1: Rectangles (5)
                case "Square":
                    shape = new Rectangle
                    {
                        Stroke = new SolidColorBrush(_currentPenColor),
                        StrokeThickness = _currentPenSize,
                        Fill = null
                    };
                    break;

                case "Rectangle":
                    shape = new Rectangle
                    {
                        Stroke = new SolidColorBrush(_currentPenColor),
                        StrokeThickness = _currentPenSize,
                        Fill = null
                    };
                    break;
                
                case "RoundedRectangle":
                    shape = new Rectangle
                    {
                        Stroke = new SolidColorBrush(_currentPenColor),
                        StrokeThickness = _currentPenSize,
                        Fill = null,
                        RadiusX = 10,
                        RadiusY = 10
                    };
                    break;
                
                case "SnipSingleCornerRectangle":
                case "SnipDiagonalCornerRectangle":
                case "RoundSingleCornerRectangle":
                    // Use Path for snipped/rounded corner rectangles
                    shape = new System.Windows.Shapes.Path
                    {
                        Stroke = new SolidColorBrush(_currentPenColor),
                        StrokeThickness = _currentPenSize,
                        Fill = null
                    };
                    break;
                    
                // PHASE 1: Basic Shapes (10) - Circle uses Ellipse
                case "Circle":
                    shape = new Ellipse
                    {
                        Stroke = new SolidColorBrush(_currentPenColor),
                        StrokeThickness = _currentPenSize,
                        Fill = null
                    };
                    break;
                    
                // PHASE 1: Polygon-based shapes
                case "Triangle":
                case "Pentagon":
                case "Hexagon":
                case "Star":
                case "Diamond":
                case "Parallelogram":
                case "Cross":
                case "Heart":
                
                // PHASE 2A: Block Arrows (15) - All Polygon
                case "RightArrow":
                case "LeftArrow":
                case "UpArrow":
                case "DownArrow":
                case "LeftRightArrow":
                case "UpDownArrow":
                case "QuadArrow":
                case "NotchedRightArrow":
                case "PentagonArrow":
                case "ChevronArrow":
                case "StripedRightArrow":
                case "CurvedRightArrow":
                case "CurvedLeftArrow":
                case "CurvedUpArrow":
                case "CurvedDownArrow":
                
                // PHASE 2B: Stars & Banners (15) - All Polygon
                case "Star4":
                case "Star6":
                case "Star7":
                case "Star8":
                case "Star10":
                case "Star12":
                case "Star16":
                case "Star24":
                case "Star32":
                case "Explosion1":
                case "Explosion2":
                case "Wave":
                case "DoubleWave":
                case "Ribbon":
                case "Scroll":
                
                // PHASE 2C: Flowchart (20) - All Polygon
                case "FlowProcess":
                case "FlowDecision":
                case "FlowDocument":
                case "FlowData":
                case "FlowPredefinedProcess":
                case "FlowInternalStorage":
                case "FlowSequentialData":
                case "FlowDirectData":
                case "FlowManualInput":
                case "FlowManualOperation":
                case "FlowConnector":
                case "FlowOffPageConnector":
                case "FlowSummingJunction":
                case "FlowOr":
                case "FlowSort":
                case "FlowExtract":
                case "FlowMerge":
                case "FlowStoredData":
                case "FlowDelay":
                case "FlowPreparation":
                
                // PHASE 2D: Callouts (14) - All Polygon
                case "RectangularCallout":
                case "RoundedRectangularCallout":
                case "OvalCallout":
                case "LineCallout1":
                case "LineCallout2":
                case "LineCallout3":
                case "LineCallout4":
                case "BorderlessLineCallout1":
                case "BorderlessLineCallout2":
                case "BorderlessLineCallout3":
                case "BorderlessLineCallout4":
                case "AccentCallout1":
                case "AccentCallout2":
                case "AccentCallout3":
                    // Create Polygon for all Phase 1 and Phase 2A-D shapes
                    shape = new Polygon
                    {
                        Stroke = new SolidColorBrush(_currentPenColor),
                        StrokeThickness = _currentPenSize,
                        Fill = null
                    };
                    break;
                
                // PHASE 2E: Equation Shapes (15) - Use Path for multi-component
                case "Plus":
                case "Multiply":
                case "NotEqual":
                    // BUGFIX: Use stroke-only rendering for white intersection gaps
                    shape = new System.Windows.Shapes.Path
                    {
                        Stroke = new SolidColorBrush(_currentPenColor),
                        StrokeThickness = Math.Max(_currentPenSize * 1.5, 8),  // Thicker stroke
                        Fill = null,  // No fill - stroke only
                        StrokeLineJoin = PenLineJoin.Miter,  // Sharp corners
                        StrokeStartLineCap = PenLineCap.Round,  // Rounded ends
                        StrokeEndLineCap = PenLineCap.Round
                    };
                    break;
                
                case "Minus":
                case "Divide":
                case "Equal":
                case "LessThan":
                case "GreaterThan":
                case "LessOrEqual":
                case "GreaterOrEqual":
                    // Fill-based rendering for solid shapes
                    shape = new System.Windows.Shapes.Path
                    {
                        Stroke = new SolidColorBrush(_currentPenColor),
                        StrokeThickness = _currentPenSize,
                        Fill = new SolidColorBrush(_currentPenColor)  // Filled with pen color
                    };
                    break;
                
                case "Approximately":
                case "Infinity":
                case "Radical":
                case "Summation":
                case "Integral":
                    // Advanced symbols - keep outline only
                    shape = new System.Windows.Shapes.Path
                    {
                        Stroke = new SolidColorBrush(_currentPenColor),
                        StrokeThickness = _currentPenSize,
                        Fill = Brushes.Transparent
                    };
                    break;
                
                // Old shapes
                case "Arrow":
                case "Polygon":
                case "Torus":
                // Charts
                case "BarChart":
                case "PieChart":
                case "LineChart":
                case "AreaChart":
                case "ScatterChart":
                case "RadarChart":
                    // Use Polygon for complex shapes and charts
                    shape = new Polygon
                    {
                        Stroke = new SolidColorBrush(_currentPenColor),
                        StrokeThickness = _currentPenSize,
                        Fill = Brushes.Transparent
                    };
                    break;
                    
                default:
                    // Default to rectangle for unknown shapes
                    shape = new Rectangle
                    {
                        Stroke = new SolidColorBrush(_currentPenColor),
                        StrokeThickness = _currentPenSize,
                        Fill = Brushes.Transparent
                    };
                    break;
            }
            
            if (shape != null)
            {
                Panel.SetZIndex(shape, _currentInkingZIndex);
            }

            return shape;
        }

        private void UpdateShapePreview(Shape shape, Point startPoint, Point currentPoint)
        {
            if (shape is Line line)
            {
                // Update line coordinates
                line.X1 = startPoint.X;
                line.Y1 = startPoint.Y;
                line.X2 = currentPoint.X;
                line.Y2 = currentPoint.Y;
            }
            else if (shape is Rectangle rect)
            {
                double left   = Math.Min(startPoint.X, currentPoint.X);
                double top    = Math.Min(startPoint.Y, currentPoint.Y);
                double width  = Math.Abs(currentPoint.X - startPoint.X);
                double height = Math.Abs(currentPoint.Y - startPoint.Y);

                // Square: constrain to equal side (use smaller dimension)
                if (_currentShape == "Square")
                {
                    double side = Math.Min(width, height);
                    // Adjust left/top so the square expands from start corner
                    if (currentPoint.X < startPoint.X) left = startPoint.X - side;
                    if (currentPoint.Y < startPoint.Y) top  = startPoint.Y - side;
                    width  = side;
                    height = side;
                }

                Canvas.SetLeft(rect, left);
                Canvas.SetTop(rect, top);
                rect.Width  = Math.Max(1, width);
                rect.Height = Math.Max(1, height);
            }
            else if (shape is System.Windows.Shapes.Path path)
            {
                if (!Is3DShape(_currentShape) && SubjectShapeGeometryService.IsSubjectShape(_currentShape))
                {
                    double left   = Math.Min(startPoint.X, currentPoint.X);
                    double top    = Math.Min(startPoint.Y, currentPoint.Y);
                    double width  = Math.Abs(currentPoint.X - startPoint.X);
                    double height = Math.Abs(currentPoint.Y - startPoint.Y);
                    if (width > 1 && height > 1)
                    {
                        if (_currentShape == "mSquare")
                        {
                            double side = Math.Min(width, height);
                            if (currentPoint.X < startPoint.X) left = startPoint.X - side;
                            if (currentPoint.Y < startPoint.Y) top  = startPoint.Y - side;
                            width  = side;
                            height = side;
                        }
                        path.Data = SubjectShapeGeometryService.GenerateGeometry(_currentShape, new Rect(left, top, width, height));
                    }
                }
                // Check if it's an equation shape (use multi-component geometry)
                if (_currentShape == "Plus" || _currentShape == "Minus" || _currentShape == "Multiply" || 
                    _currentShape == "Divide" || _currentShape == "Equal" || _currentShape == "NotEqual" ||
                    _currentShape == "LessThan" || _currentShape == "GreaterThan" || 
                    _currentShape == "LessOrEqual" || _currentShape == "GreaterOrEqual" ||
                    _currentShape == "Approximately" || _currentShape == "Infinity" || 
                    _currentShape == "Radical" || _currentShape == "Summation" || _currentShape == "Integral")
                {
                    // Update equation shape with GeometryGroup
                    UpdatePathEquationShape(path, startPoint, currentPoint);
                }
                else if (Is3DShape(_currentShape))
                {
                    // Update 3D wireframe preview in real-time
                    Update3DShapePreview(path, startPoint, currentPoint);
                }
                else
                {
                    // Update Path geometry for other complex shapes
                    UpdatePathGeometry(path, startPoint, currentPoint);
                }
            }
            else if (shape is Polygon polygon)
            {
                // Update polygon points based on shape type
                UpdatePolygonPoints(polygon, startPoint, currentPoint);
            }
            else if (shape is Rectangle || shape is Ellipse)
            {
                // Calculate top-left corner and dimensions
                double left   = Math.Min(startPoint.X, currentPoint.X);
                double top    = Math.Min(startPoint.Y, currentPoint.Y);
                double width  = Math.Abs(currentPoint.X - startPoint.X);
                double height = Math.Abs(currentPoint.Y - startPoint.Y);

                // Circle: constrain to perfect circle (equal width & height)
                if (_currentShape == "Circle" && shape is Ellipse)
                {
                    double side = Math.Min(width, height);
                    // Keep the start corner fixed regardless of drag direction
                    if (currentPoint.X < startPoint.X) left = startPoint.X - side;
                    if (currentPoint.Y < startPoint.Y) top  = startPoint.Y - side;
                    width  = side;
                    height = side;
                }

                // Update position and size
                Canvas.SetLeft(shape, left);
                Canvas.SetTop(shape, top);
                shape.Width  = Math.Max(1, width);
                shape.Height = Math.Max(1, height);
            }
        }

        private void UpdatePathGeometry(System.Windows.Shapes.Path path, Point startPoint, Point currentPoint)
        {
            // Calculate bounds
            double left = Math.Min(startPoint.X, currentPoint.X);
            double top = Math.Min(startPoint.Y, currentPoint.Y);
            double width = Math.Abs(currentPoint.X - startPoint.X);
            double height = Math.Abs(currentPoint.Y - startPoint.Y);
            
            string pathData = "";
            
            // Generate path data based on current shape type
            if (_currentShape == "ArrowLine")
            {
                // Line with arrow at end - improved calculation
                double lineLength = Math.Sqrt(width * width + height * height);
                double arrowSize = Math.Max(15, lineLength * 0.08); // Minimum 15px, or 8% of line length
                
                // Calculate line direction vector
                double deltaX = currentPoint.X - startPoint.X;
                double deltaY = currentPoint.Y - startPoint.Y;
                double lineLength2 = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
                
                if (lineLength2 > 0)
                {
                    // Normalize direction vector
                    double unitX = deltaX / lineLength2;
                    double unitY = deltaY / lineLength2;
                    
                    // Calculate arrowhead points perpendicular to line direction
                    double perpX = -unitY; // Perpendicular vector
                    double perpY = unitX;
                    
                    // Arrow tip points
                    double arrowTip1X = currentPoint.X - (unitX * arrowSize) + (perpX * arrowSize * 0.5);
                    double arrowTip1Y = currentPoint.Y - (unitY * arrowSize) + (perpY * arrowSize * 0.5);
                    double arrowTip2X = currentPoint.X - (unitX * arrowSize) - (perpX * arrowSize * 0.5);
                    double arrowTip2Y = currentPoint.Y - (unitY * arrowSize) - (perpY * arrowSize * 0.5);
                    
                    pathData = System.FormattableString.Invariant($"M{startPoint.X},{startPoint.Y} L{currentPoint.X},{currentPoint.Y} ") +
                              System.FormattableString.Invariant($"M{arrowTip1X},{arrowTip1Y} L{currentPoint.X},{currentPoint.Y} L{arrowTip2X},{arrowTip2Y}");
                }
                else
                {
                    // Fallback for zero-length line
                    pathData = System.FormattableString.Invariant($"M{startPoint.X},{startPoint.Y} L{currentPoint.X},{currentPoint.Y}");
                }
            }
            else if (_currentShape == "DoubleArrowLine")
            {
                // Line with arrows at both ends - improved calculation
                double lineLength = Math.Sqrt(width * width + height * height);
                double arrowSize = Math.Max(15, lineLength * 0.08); // Minimum 15px, or 8% of line length
                
                // Calculate line direction vector
                double deltaX = currentPoint.X - startPoint.X;
                double deltaY = currentPoint.Y - startPoint.Y;
                double lineLength2 = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
                
                if (lineLength2 > 0)
                {
                    // Normalize direction vector
                    double unitX = deltaX / lineLength2;
                    double unitY = deltaY / lineLength2;
                    
                    // Calculate perpendicular vector for arrowhead width
                    double perpX = -unitY;
                    double perpY = unitX;
                    
                    // Arrow at end (currentPoint)
                    double endArrowTip1X = currentPoint.X - (unitX * arrowSize) + (perpX * arrowSize * 0.5);
                    double endArrowTip1Y = currentPoint.Y - (unitY * arrowSize) + (perpY * arrowSize * 0.5);
                    double endArrowTip2X = currentPoint.X - (unitX * arrowSize) - (perpX * arrowSize * 0.5);
                    double endArrowTip2Y = currentPoint.Y - (unitY * arrowSize) - (perpY * arrowSize * 0.5);
                    
                    // Arrow at start (startPoint) - reverse direction
                    double startArrowTip1X = startPoint.X + (unitX * arrowSize) + (perpX * arrowSize * 0.5);
                    double startArrowTip1Y = startPoint.Y + (unitY * arrowSize) + (perpY * arrowSize * 0.5);
                    double startArrowTip2X = startPoint.X + (unitX * arrowSize) - (perpX * arrowSize * 0.5);
                    double startArrowTip2Y = startPoint.Y + (unitY * arrowSize) - (perpY * arrowSize * 0.5);
                    
                    pathData = System.FormattableString.Invariant($"M{startPoint.X},{startPoint.Y} L{currentPoint.X},{currentPoint.Y} ") +
                              System.FormattableString.Invariant($"M{startArrowTip1X},{startArrowTip1Y} L{startPoint.X},{startPoint.Y} L{startArrowTip2X},{startArrowTip2Y} ") +
                              System.FormattableString.Invariant($"M{endArrowTip1X},{endArrowTip1Y} L{currentPoint.X},{currentPoint.Y} L{endArrowTip2X},{endArrowTip2Y}");
                }
                else
                {
                    // Fallback for zero-length line
                    pathData = System.FormattableString.Invariant($"M{startPoint.X},{startPoint.Y} L{currentPoint.X},{currentPoint.Y}");
                }
            }
            else if (_currentShape == "ElbowConnector")
            {
                // Elbow (right-angle) connector
                double midX = (startPoint.X + currentPoint.X) / 2;
                pathData = System.FormattableString.Invariant($"M{startPoint.X},{startPoint.Y} L{midX},{startPoint.Y} L{midX},{currentPoint.Y} L{currentPoint.X},{currentPoint.Y}");
            }
            else if (_currentShape == "CurvedConnector")
            {
                // Curved connector using quadratic Bezier
                double midX = (startPoint.X + currentPoint.X) / 2;
                double midY = (startPoint.Y + currentPoint.Y) / 2;
                double ctrlX = midX;
                double ctrlY = startPoint.Y;
                pathData = System.FormattableString.Invariant($"M{startPoint.X},{startPoint.Y} Q{ctrlX},{ctrlY} {currentPoint.X},{currentPoint.Y}");
            }
            else if (_currentShape == "SnipSingleCornerRectangle")
            {
                // Rectangle with one corner snipped (top-right)
                double snipSize = Math.Min(width, height) * 0.15;
                pathData = System.FormattableString.Invariant($"M{left},{top} L{left + width - snipSize},{top} L{left + width},{top + snipSize} L{left + width},{top + height} L{left},{top + height} Z");
            }
            else if (_currentShape == "SnipDiagonalCornerRectangle")
            {
                // Rectangle with two diagonal corners snipped (top-right and bottom-left)
                double snipSize = Math.Min(width, height) * 0.15;
                pathData = System.FormattableString.Invariant($"M{left},{top} L{left + width - snipSize},{top} L{left + width},{top + snipSize} L{left + width},{top + height} ") +
                          System.FormattableString.Invariant($"L{left + snipSize},{top + height} L{left},{top + height - snipSize} Z");
            }
            else if (_currentShape == "RoundSingleCornerRectangle")
            {
                // Rectangle with one corner rounded (top-right)
                double radius = Math.Min(width, height) * 0.15;
                pathData = System.FormattableString.Invariant($"M{left},{top} L{left + width - radius},{top} Q{left + width},{top} {left + width},{top + radius} L{left + width},{top + height} L{left},{top + height} Z");
            }
            
            if (!string.IsNullOrEmpty(pathData))
            {
                path.Data = Geometry.Parse(pathData);
            }
        }

        private void UpdatePolygonPoints(Polygon polygon, Point startPoint, Point currentPoint)
        {
            // Get bounds
            double left = Math.Min(startPoint.X, currentPoint.X);
            double top = Math.Min(startPoint.Y, currentPoint.Y);
            double width = Math.Abs(currentPoint.X - startPoint.X);
            double height = Math.Abs(currentPoint.Y - startPoint.Y);
            
            // Update points based on current shape type
            if (_currentShape == "Triangle")
            {
                polygon.Points.Clear();
                // Top center
                polygon.Points.Add(new Point(left + width / 2, top));
                // Bottom left
                polygon.Points.Add(new Point(left, top + height));
                // Bottom right
                polygon.Points.Add(new Point(left + width, top + height));
            }
            else if (_currentShape == "Star")
            {
                polygon.Points.Clear();
                double centerX = left + width / 2;
                double centerY = top + height / 2;
                double outerRadius = Math.Min(width, height) / 2;
                double innerRadius = outerRadius * 0.4;
                
                // Create 5-pointed star
                for (int i = 0; i < 10; i++)
                {
                    double angle = (i * 36 - 90) * Math.PI / 180;
                    double radius = (i % 2 == 0) ? outerRadius : innerRadius;
                    polygon.Points.Add(new Point(
                        centerX + radius * Math.Cos(angle),
                        centerY + radius * Math.Sin(angle)
                    ));
                }
            }
            else if (_currentShape == "Arrow")
            {
                polygon.Points.Clear();
                // Simple right arrow
                double midY = top + height / 2;
                polygon.Points.Add(new Point(left, midY - height / 4));
                polygon.Points.Add(new Point(left + width * 0.6, midY - height / 4));
                polygon.Points.Add(new Point(left + width * 0.6, top));
                polygon.Points.Add(new Point(left + width, midY));
                polygon.Points.Add(new Point(left + width * 0.6, top + height));
                polygon.Points.Add(new Point(left + width * 0.6, midY + height / 4));
                polygon.Points.Add(new Point(left, midY + height / 4));
            }
            else if (_currentShape == "Heart")
            {
                polygon.Points.Clear();
                // Heart shape using parametric equation for smooth curves
                double midX = left + width / 2;
                double scaleX = width / 32.0;
                double scaleY = height / 30.0;
                
                // Generate heart shape points using parametric equations
                // Heart curve: x = 16*sin^3(t), y = 13*cos(t) - 5*cos(2t) - 2*cos(3t) - cos(4t)
                for (double t = 0; t <= 2 * Math.PI; t += 0.1)
                {
                    double sinT = Math.Sin(t);
                    double cosT = Math.Cos(t);
                    
                    // Parametric heart equation
                    double x = 16 * sinT * sinT * sinT;
                    double y = -(13 * cosT - 5 * Math.Cos(2 * t) - 2 * Math.Cos(3 * t) - Math.Cos(4 * t));
                    
                    // Scale and translate to fit bounding box
                    double px = midX + x * scaleX;
                    double py = top + (y + 15) * scaleY; // +15 to center vertically
                    
                    polygon.Points.Add(new Point(px, py));
                }
            }
            else if (_currentShape == "Pentagon")
            {
                polygon.Points.Clear();
                double centerX = left + width / 2;
                double centerY = top + height / 2;
                double radius = Math.Min(width, height) / 2;
                
                for (int i = 0; i < 5; i++)
                {
                    double angle = (i * 72 - 90) * Math.PI / 180;
                    polygon.Points.Add(new Point(
                        centerX + radius * Math.Cos(angle),
                        centerY + radius * Math.Sin(angle)
                    ));
                }
            }
            else if (_currentShape == "Hexagon")
            {
                polygon.Points.Clear();
                double centerX = left + width / 2;
                double centerY = top + height / 2;
                double radius = Math.Min(width, height) / 2;
                
                for (int i = 0; i < 6; i++)
                {
                    double angle = (i * 60 - 90) * Math.PI / 180;
                    polygon.Points.Add(new Point(
                        centerX + radius * Math.Cos(angle),
                        centerY + radius * Math.Sin(angle)
                    ));
                }
            }
            // PHASE 1: New shapes
            else if (_currentShape == "Diamond")
            {
                polygon.Points.Clear();
                double midX = left + width / 2;
                double midY = top + height / 2;
                // Top, Right, Bottom, Left
                polygon.Points.Add(new Point(midX, top));
                polygon.Points.Add(new Point(left + width, midY));
                polygon.Points.Add(new Point(midX, top + height));
                polygon.Points.Add(new Point(left, midY));
            }
            else if (_currentShape == "Parallelogram")
            {
                polygon.Points.Clear();
                double offset = width * 0.2; // 20% offset for slant
                // Top left, Top right, Bottom right, Bottom left
                polygon.Points.Add(new Point(left + offset, top));
                polygon.Points.Add(new Point(left + width, top));
                polygon.Points.Add(new Point(left + width - offset, top + height));
                polygon.Points.Add(new Point(left, top + height));
            }
            else if (_currentShape == "Cross")
            {
                polygon.Points.Clear();
                double armWidth = width / 3;
                double armHeight = height / 3;
                double midX = left + width / 2;
                double midY = top + height / 2;
                
                // Create cross shape (12 points)
                // Top arm
                polygon.Points.Add(new Point(midX - armWidth / 2, top));
                polygon.Points.Add(new Point(midX + armWidth / 2, top));
                // Right side of vertical arm
                polygon.Points.Add(new Point(midX + armWidth / 2, midY - armHeight / 2));
                // Right arm
                polygon.Points.Add(new Point(left + width, midY - armHeight / 2));
                polygon.Points.Add(new Point(left + width, midY + armHeight / 2));
                // Bottom of right arm
                polygon.Points.Add(new Point(midX + armWidth / 2, midY + armHeight / 2));
                // Bottom arm
                polygon.Points.Add(new Point(midX + armWidth / 2, top + height));
                polygon.Points.Add(new Point(midX - armWidth / 2, top + height));
                // Left side of vertical arm
                polygon.Points.Add(new Point(midX - armWidth / 2, midY + armHeight / 2));
                // Left arm
                polygon.Points.Add(new Point(left, midY + armHeight / 2));
                polygon.Points.Add(new Point(left, midY - armHeight / 2));
                // Top of left arm
                polygon.Points.Add(new Point(midX - armWidth / 2, midY - armHeight / 2));
            }
            
            // ====================================================================================
            // PHASE 2: EXTENDED SHAPES (79 shapes)
            // ====================================================================================
            
            // PHASE 2A: Block Arrows (15)
            else if (_currentShape == "RightArrow" || _currentShape == "LeftArrow" || 
                     _currentShape == "UpArrow" || _currentShape == "DownArrow")
            {
                // Simple directional arrows
                DrawArrow(polygon, left, top, width, height, _currentShape);
            }
            else if (_currentShape == "LeftRightArrow" || _currentShape == "UpDownArrow")
            {
                // Bi-directional arrows
                DrawDoubleArrow(polygon, left, top, width, height, _currentShape);
            }
            else if (_currentShape == "QuadArrow")
            {
                // 4-way arrow (cross arrows)
                DrawQuadArrow(polygon, left, top, width, height);
            }
            else if (_currentShape == "NotchedRightArrow")
            {
                DrawNotchedRightArrow(polygon, left, top, width, height);
            }
            else if (_currentShape == "PentagonArrow")
            {
                DrawPentagonArrow(polygon, left, top, width, height);
            }
            else if (_currentShape == "ChevronArrow")
            {
                DrawChevronArrow(polygon, left, top, width, height);
            }
            else if (_currentShape == "StripedRightArrow")
            {
                DrawStripedRightArrow(polygon, left, top, width, height);
            }
            else if (_currentShape == "CurvedRightArrow" || _currentShape == "CurvedLeftArrow" ||
                     _currentShape == "CurvedUpArrow" || _currentShape == "CurvedDownArrow")
            {
                DrawCurvedArrow(polygon, left, top, width, height, _currentShape);
            }
            
            // PHASE 2B: Stars & Banners (15)
            else if (_currentShape.StartsWith("Star"))
            {
                // Extract number of points from shape name (e.g., "Star6" → 6)
                int points = _currentShape switch
                {
                    "Star4" => 4,
                    "Star6" => 6,
                    "Star7" => 7,
                    "Star8" => 8,
                    "Star10" => 10,
                    "Star12" => 12,
                    "Star16" => 16,
                    "Star24" => 24,
                    "Star32" => 32,
                    _ => 5 // Default
                };
                DrawStar(polygon, left, top, width, height, points);
            }
            else if (_currentShape == "Explosion1" || _currentShape == "Explosion2")
            {
                // Explosion effect (irregular star with 16 points)
                DrawStar(polygon, left, top, width, height, 16);
            }
            else if (_currentShape == "Wave" || _currentShape == "DoubleWave")
            {
                // Wave shape using sine curve
                DrawWave(polygon, left, top, width, height, _currentShape == "DoubleWave" ? 2 : 1);
            }
            else if (_currentShape == "Ribbon" || _currentShape == "Scroll")
            {
                // Ribbon/Scroll shape (rectangle with curved ends)
                DrawRibbon(polygon, left, top, width, height);
            }
            
            // PHASE 2C: Flowchart (20)
            else if (_currentShape.StartsWith("Flow"))
            {
                DrawFlowchartShape(polygon, left, top, width, height, _currentShape);
            }
            
            // PHASE 2D: Callouts (14)
            else if (_currentShape.Contains("Callout"))
            {
                DrawCallout(polygon, left, top, width, height, _currentShape);
            }
            
            // NOTE: PHASE 2E Equation Shapes (15) now use Path with GeometryGroup
            // They are handled in UpdatePathEquationShape() method, not here!
            
            // 3D Shapes (simplified 2D representations)
            else if (_currentShape == "Cube")
            {
                polygon.Points.Clear();
                
                // THUáº¬T TOÃN Váº¼ KHá»I Láº¬P PHÆ¯Æ NG 3D Vá»šI PHÃ‰P CHIáº¾U ISOMETRIC
                // Tham khảo: Hình học không gian - Phép chiếu Isometric
                
                // Bước 1: Xác định kích thước khối lập phương
                double cubeSize = Math.Min(width, height) * 0.7; // Kích thước cạnh a
                double halfSize = cubeSize / 2;
                double centerX = left + width / 2;
                double centerY = top + height / 2;
                
                // Bước 2: Định nghĩa 8 đỉnh trong không gian 3D (x, y, z)
                // Tâm khối lập phương tại gốc tọa độ
                double[,] vertices3D = new double[8, 3]
                {
                    { -halfSize, -halfSize, -halfSize }, // V0: Front-Bottom-Left
                    { +halfSize, -halfSize, -halfSize }, // V1: Front-Bottom-Right
                    { +halfSize, +halfSize, -halfSize }, // V2: Front-Top-Right
                    { -halfSize, +halfSize, -halfSize }, // V3: Front-Top-Left
                    { -halfSize, -halfSize, +halfSize }, // V4: Back-Bottom-Left
                    { +halfSize, -halfSize, +halfSize }, // V5: Back-Bottom-Right
                    { +halfSize, +halfSize, +halfSize }, // V6: Back-Top-Right
                    { -halfSize, +halfSize, +halfSize }  // V7: Back-Top-Left
                };
                
                // Bước 3: Áp dụng phép chiếu Isometric (3D → 2D)
                // Công thức: x_2d = x * cos(30°) - z * cos(30°)
                //            y_2d = y + x * sin(30Â°) + z * sin(30Â°)
                double cos30 = Math.Sqrt(3) / 2; // â‰ˆ 0.866
                double sin30 = 0.5;
                
                Point[] vertices2D = new Point[8];
                for (int i = 0; i < 8; i++)
                {
                    double x = vertices3D[i, 0];
                    double y = vertices3D[i, 1];
                    double z = vertices3D[i, 2];
                    
                    // Phép chiếu Isometric
                    double x_2d = x * cos30 - z * cos30;
                    double y_2d = y + x * sin30 + z * sin30;
                    
                    // Chuyển sang tọa độ màn hình (đảo trục Y vì màn hình Y tăng xuống dưới)
                    vertices2D[i] = new Point(centerX + x_2d, centerY - y_2d);
                }
                
                // Bước 4: Vẽ 12 cạnh của khối lập phương
                // Theo thứ tự: Mặt trước → Mặt sau → Các cạnh nối
                
                // Máº·t TRÆ¯á»šC (Front face): V0-V1-V2-V3-V0
                polygon.Points.Add(vertices2D[0]); // V0
                polygon.Points.Add(vertices2D[1]); // V1
                polygon.Points.Add(vertices2D[2]); // V2
                polygon.Points.Add(vertices2D[3]); // V3
                polygon.Points.Add(vertices2D[0]); // V0 (đóng mặt trước)
                
                // Cạnh nối: V0 → V4 (Front-Bottom-Left to Back-Bottom-Left)
                polygon.Points.Add(vertices2D[4]); // V4
                
                // Mặt SAU - Cạnh dưới: V4 → V5
                polygon.Points.Add(vertices2D[5]); // V5
                
                // Cạnh nối: V5 → V1 (quay về mặt trước)
                polygon.Points.Add(vertices2D[1]); // V1
                
                // Cạnh nối: V1 → V2 (đã có)
                polygon.Points.Add(vertices2D[2]); // V2
                
                // Cạnh nối: V2 → V6 (Front-Top-Right to Back-Top-Right)
                polygon.Points.Add(vertices2D[6]); // V6
                
                // Mặt SAU - Cạnh trên: V6 → V7
                polygon.Points.Add(vertices2D[7]); // V7
                
                // Cạnh nối: V7 → V3 (quay về mặt trước)
                polygon.Points.Add(vertices2D[3]); // V3
                
                // Hoàn thiện mặt sau: V3 → V7 → V4
                polygon.Points.Add(vertices2D[7]); // V7
                polygon.Points.Add(vertices2D[4]); // V4
                
                // Mặt SAU - Các cạnh còn lại: V4 → V5 → V6 → V7
                polygon.Points.Add(vertices2D[5]); // V5
                polygon.Points.Add(vertices2D[6]); // V6
            }
            else if (_currentShape == "Pyramid")
            {
                polygon.Points.Clear();
                // 3D pyramid with square base - isometric view
                double d = Math.Min(width, height) * 0.3;
                double midX = left + width / 2;
                
                // Apex point at top center
                Point apex = new Point(midX, top);
                
                // Square base at bottom (isometric perspective)
                Point baseFrontLeft = new Point(left + d, top + height);
                Point baseFrontRight = new Point(left + width, top + height);
                Point baseBackRight = new Point(left + width - d, top + height - d);
                Point baseBackLeft = new Point(left, top + height - d);
                
                // Draw pyramid wireframe
                // Front left edge
                polygon.Points.Add(apex);
                polygon.Points.Add(baseFrontLeft);
                
                // Base front edge
                polygon.Points.Add(baseFrontRight);
                
                // Front right edge back to apex
                polygon.Points.Add(apex);
                
                // Back right edge
                polygon.Points.Add(baseBackRight);
                
                // Base back edge
                polygon.Points.Add(baseBackLeft);
                
                // Back left edge to apex
                polygon.Points.Add(apex);
                
                // Complete base square
                polygon.Points.Add(baseBackLeft);
                polygon.Points.Add(baseFrontLeft);
                polygon.Points.Add(baseFrontRight);
                polygon.Points.Add(baseBackRight);
            }
            else if (_currentShape == "Cylinder")
            {
                polygon.Points.Clear();
                // Cylinder with ellipse top and bottom
                double ovalHeight = height * 0.15;
                int steps = 20;
                
                // Top ellipse (half visible - front)
                for (int i = 0; i <= steps / 2; i++)
                {
                    double angle = Math.PI + (i * Math.PI / (steps / 2));
                    double x = left + width / 2 + (width / 2) * Math.Cos(angle);
                    double y = top + ovalHeight / 2 + (ovalHeight / 2) * Math.Sin(angle);
                    polygon.Points.Add(new Point(x, y));
                }
                
                // Right side line down
                polygon.Points.Add(new Point(left + width, top + height - ovalHeight));
                
                // Bottom ellipse (full)
                for (int i = 0; i <= steps; i++)
                {
                    double angle = Math.PI + (i * 2 * Math.PI / steps);
                    double x = left + width / 2 + (width / 2) * Math.Cos(angle);
                    double y = top + height - ovalHeight / 2 + (ovalHeight / 2) * Math.Sin(angle);
                    polygon.Points.Add(new Point(x, y));
                }
                
                // Left side line up
                polygon.Points.Add(new Point(left, top + ovalHeight));
            }
            else if (_currentShape == "Cone")
            {
                polygon.Points.Clear();
                // Cone with circular base
                double midX = left + width / 2;
                double baseY = top + height;
                double baseRadiusX = width / 2;
                double baseRadiusY = height * 0.15;
                int steps = 20;
                
                // Apex point
                polygon.Points.Add(new Point(midX, top));
                
                // Right edge to base
                polygon.Points.Add(new Point(left + width, baseY - baseRadiusY));
                
                // Base ellipse (visible half)
                for (int i = 0; i <= steps; i++)
                {
                    double angle = (i * Math.PI / steps);
                    double x = midX + baseRadiusX * Math.Cos(angle);
                    double y = baseY - baseRadiusY / 2 + (baseRadiusY / 2) * Math.Sin(angle);
                    polygon.Points.Add(new Point(x, y));
                }
                
                // Left edge back to apex
                polygon.Points.Add(new Point(left, baseY - baseRadiusY));
            }
            else if (_currentShape == "Sphere" || _currentShape == "Torus")
            {
                polygon.Points.Clear();
                // Circle approximation for sphere/torus
                double centerX = left + width / 2;
                double centerY = top + height / 2;
                double radiusX = width / 2;
                double radiusY = height / 2;
                
                for (int i = 0; i <= 30; i++)
                {
                    double angle = (i * 2 * Math.PI / 30);
                    polygon.Points.Add(new Point(
                        centerX + radiusX * Math.Cos(angle),
                        centerY + radiusY * Math.Sin(angle)
                    ));
                }
            }
            else if (_currentShape == "Prism")
            {
                polygon.Points.Clear();
                // Triangular prism - isometric view with clear front and back triangles
                double d = width * 0.3; // depth offset
                double midX = left + width / 2;
                
                // Front triangle vertices
                Point frontTop = new Point(left + (width - d) / 2, top);
                Point frontBL = new Point(left, top + height);
                Point frontBR = new Point(left + width - d, top + height);
                
                // Back triangle vertices (offset by depth d)
                Point backTop = new Point(left + (width - d) / 2 + d, top);
                Point backBL = new Point(left + d, top + height);
                Point backBR = new Point(left + width, top + height);
                
                // Draw prism wireframe
                // Front triangle
                polygon.Points.Add(frontTop);
                polygon.Points.Add(frontBL);
                polygon.Points.Add(frontBR);
                polygon.Points.Add(frontTop);
                
                // Connect to back triangle
                polygon.Points.Add(backTop);
                polygon.Points.Add(backBL);
                polygon.Points.Add(frontBL);
                
                // Bottom edge
                polygon.Points.Add(frontBR);
                polygon.Points.Add(backBR);
                polygon.Points.Add(backBL);
                
                // Complete back triangle
                polygon.Points.Add(backBR);
                polygon.Points.Add(backTop);
            }
            else if (_currentShape == "Tetrahedron")
            {
                polygon.Points.Clear();
                // Tetrahedron (triangular pyramid) - 4 faces, 6 edges
                double midX = left + width / 2;
                double d = width * 0.25; // offset for 3D effect
                
                // 4 vertices
                Point apex = new Point(midX + d, top); // top point (offset for perspective)
                Point baseFront = new Point(midX, top + height); // front base vertex
                Point baseLeft = new Point(left, top + height * 0.7); // left base vertex  
                Point baseRight = new Point(left + width, top + height * 0.7); // right base vertex
                
                // Draw tetrahedron edges
                // Front face (apex to front base)
                polygon.Points.Add(apex);
                polygon.Points.Add(baseFront);
                
                // Front to left
                polygon.Points.Add(baseLeft);
                
                // Left to apex
                polygon.Points.Add(apex);
                
                // Apex to right
                polygon.Points.Add(baseRight);
                
                // Right to front
                polygon.Points.Add(baseFront);
                
                // Complete base triangle
                polygon.Points.Add(baseLeft);
                polygon.Points.Add(baseRight);
            }
            // Charts
            else if (_currentShape == "BarChart")
            {
                polygon.Points.Clear();
                // Simple bar chart outline (3 bars)
                double barWidth = width / 4;
                double gap = width / 12;
                // Bar 1 (60% height)
                polygon.Points.Add(new Point(left + gap, top + height * 0.4));
                polygon.Points.Add(new Point(left + gap, top + height));
                polygon.Points.Add(new Point(left + gap + barWidth, top + height));
                polygon.Points.Add(new Point(left + gap + barWidth, top + height * 0.4));
                // Gap - move to next bar
                polygon.Points.Add(new Point(left + gap + barWidth + gap, top + height * 0.4));
                // Bar 2 (80% height)
                polygon.Points.Add(new Point(left + gap + barWidth + gap, top + height * 0.2));
                polygon.Points.Add(new Point(left + gap + barWidth + gap, top + height));
                polygon.Points.Add(new Point(left + gap + barWidth * 2 + gap, top + height));
                polygon.Points.Add(new Point(left + gap + barWidth * 2 + gap, top + height * 0.2));
                // Gap - move to next bar
                polygon.Points.Add(new Point(left + gap + barWidth * 2 + gap * 2, top + height * 0.2));
                // Bar 3 (50% height)
                polygon.Points.Add(new Point(left + gap + barWidth * 2 + gap * 2, top + height * 0.5));
                polygon.Points.Add(new Point(left + gap + barWidth * 2 + gap * 2, top + height));
                polygon.Points.Add(new Point(left + gap + barWidth * 3 + gap * 2, top + height));
                polygon.Points.Add(new Point(left + gap + barWidth * 3 + gap * 2, top + height * 0.5));
            }
            else if (_currentShape == "PieChart")
            {
                polygon.Points.Clear();
                // Pie chart (circle with slice)
                double centerX = left + width / 2;
                double centerY = top + height / 2;
                double radius = Math.Min(width, height) / 2;
                
                // Draw 3/4 circle (270 degrees)
                for (int i = 0; i <= 15; i++)
                {
                    double angle = (i * 18 - 90) * Math.PI / 180;
                    polygon.Points.Add(new Point(
                        centerX + radius * Math.Cos(angle),
                        centerY + radius * Math.Sin(angle)
                    ));
                }
                // Close to center for slice effect
                polygon.Points.Add(new Point(centerX, centerY));
            }
            else if (_currentShape == "LineChart")
            {
                polygon.Points.Clear();
                // Line chart (zigzag line)
                double step = width / 4;
                polygon.Points.Add(new Point(left, top + height * 0.7));
                polygon.Points.Add(new Point(left + step, top + height * 0.4));
                polygon.Points.Add(new Point(left + step * 2, top + height * 0.6));
                polygon.Points.Add(new Point(left + step * 3, top + height * 0.2));
                polygon.Points.Add(new Point(left + width, top + height * 0.3));
            }
            else if (_currentShape == "AreaChart")
            {
                polygon.Points.Clear();
                // Area chart (filled line chart)
                double step = width / 4;
                polygon.Points.Add(new Point(left, top + height));
                polygon.Points.Add(new Point(left, top + height * 0.7));
                polygon.Points.Add(new Point(left + step, top + height * 0.4));
                polygon.Points.Add(new Point(left + step * 2, top + height * 0.6));
                polygon.Points.Add(new Point(left + step * 3, top + height * 0.2));
                polygon.Points.Add(new Point(left + width, top + height * 0.3));
                polygon.Points.Add(new Point(left + width, top + height));
            }
            else if (_currentShape == "ScatterChart")
            {
                polygon.Points.Clear();
                // Scatter chart (small squares representing points)
                double[][] points = new double[][]
                {
                    new double[] { 0.2, 0.7 }, new double[] { 0.3, 0.5 }, new double[] { 0.4, 0.6 },
                    new double[] { 0.5, 0.3 }, new double[] { 0.6, 0.4 }, new double[] { 0.7, 0.5 },
                    new double[] { 0.8, 0.2 }
                };
                
                double dotSize = Math.Min(width, height) * 0.05;
                foreach (var pt in points)
                {
                    double px = left + width * pt[0];
                    double py = top + height * pt[1];
                    // Add small square for each point
                    polygon.Points.Add(new Point(px - dotSize, py - dotSize));
                    polygon.Points.Add(new Point(px + dotSize, py - dotSize));
                    polygon.Points.Add(new Point(px + dotSize, py + dotSize));
                    polygon.Points.Add(new Point(px - dotSize, py + dotSize));
                    polygon.Points.Add(new Point(px - dotSize, py - dotSize)); // Close square
                }
            }
            else if (_currentShape == "RadarChart")
            {
                polygon.Points.Clear();
                // Radar/Spider chart (pentagon with inner data)
                double centerX = left + width / 2;
                double centerY = top + height / 2;
                double radius = Math.Min(width, height) / 2;
                
                // Outer pentagon
                for (int i = 0; i < 5; i++)
                {
                    double angle = (i * 72 - 90) * Math.PI / 180;
                    polygon.Points.Add(new Point(
                        centerX + radius * Math.Cos(angle),
                        centerY + radius * Math.Sin(angle)
                    ));
                }
            }
            else
            {
                // Default: Rectangle-like polygon
                polygon.Points.Clear();
                polygon.Points.Add(new Point(left, top));
                polygon.Points.Add(new Point(left + width, top));
                polygon.Points.Add(new Point(left + width, top + height));
                polygon.Points.Add(new Point(left, top + height));
            }
        }

        private Polyline CreateStrokeByBrushType()
        {
            var stroke = new Polyline
            {
                Stroke = new SolidColorBrush(_currentPenColor),
                StrokeThickness = _currentPenSize
            };

            // ✅ Mọi nét vẽ mặc định ở tầng UserContentBase (100)
            Panel.SetZIndex(stroke, ZIndexConstants.UserContentBase);

            // =====================================================
            // 🚀 HARDWARE ACCELERATION FOR STROKES (Anti-Jitter)
            // Enable GPU rendering and optimize for large displays
            // =====================================================
            // EdgeMode.Unspecified: Sử dụng anti-aliasing mặc định (mượt mà, không có viền trắng)
            // CachingHint.Cache: Cache rendering để tăng hiệu suất
            RenderOptions.SetCachingHint(stroke, CachingHint.Cache);
            stroke.SnapsToDevicePixels = false;  // Tắt để tránh viền trắng
            stroke.UseLayoutRounding = false;    // Tắt để tránh shift background

            // Apply different styles based on brush type
            switch (_currentBrushType)
            {
                case "Normal" or "Simple" or "AI" or "Shape":
                    // Standard smooth pen
                    stroke.StrokeLineJoin = PenLineJoin.Round;
                    stroke.StrokeStartLineCap = PenLineCap.Round;
                    stroke.StrokeEndLineCap = PenLineCap.Round;
                    stroke.StrokeThickness = _currentPenSize;
                    break;

                case "Calligraphy" or "Hoc":
                    // Educational calligraphy - thicker, round caps
                    stroke.StrokeLineJoin = PenLineJoin.Round;
                    stroke.StrokeStartLineCap = PenLineCap.Round;
                    stroke.StrokeEndLineCap = PenLineCap.Round;
                    stroke.StrokeThickness = _currentPenSize * 1.25;
                    break;

                case "Highlighter" or "Marker" or "Mask" or "MaskPen":
                    // Highlighter - semi-transparent, flat ends, wider
                    stroke.StrokeLineJoin = PenLineJoin.Round;
                    stroke.StrokeStartLineCap = PenLineCap.Round;
                    stroke.StrokeEndLineCap = PenLineCap.Round;
                    stroke.Opacity = 0.4;
                    stroke.StrokeThickness = Math.Max(_currentPenSize * 2.2, 10);
                    break;

                case "Laser":
                    // Laser pen - vibrant glowing indicator
                    stroke.StrokeLineJoin = PenLineJoin.Round;
                    stroke.StrokeStartLineCap = PenLineCap.Round;
                    stroke.StrokeEndLineCap = PenLineCap.Round;
                    stroke.Opacity = 0.95;
                    stroke.StrokeThickness = Math.Max(_currentPenSize * 1.3, 4);
                    break;

                case "Transparent":
                    stroke.StrokeLineJoin = PenLineJoin.Round;
                    stroke.StrokeStartLineCap = PenLineCap.Round;
                    stroke.StrokeEndLineCap = PenLineCap.Round;
                    stroke.Opacity = 0.85;
                    break;

                default:
                    // Fallback to normal
                    stroke.StrokeLineJoin = PenLineJoin.Round;
                    stroke.StrokeStartLineCap = PenLineCap.Round;
                    stroke.StrokeEndLineCap = PenLineCap.Round;
                    stroke.StrokeThickness = _currentPenSize;
                    break;
            }

            return stroke;
        }

        /// <summary>
        /// Tự động làm mờ và giải phóng nét bút laser sau 2.5 giây
        /// </summary>
        private void AnimateAndRemoveLaserStroke(UIElement stroke)
        {
            if (stroke == null) return;
            
            var anim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = stroke.Opacity,
                To = 0.0,
                BeginTime = TimeSpan.FromSeconds(1.5),
                Duration = TimeSpan.FromSeconds(1.0),
                FillBehavior = System.Windows.Media.Animation.FillBehavior.Stop
            };
            
            anim.Completed += (s, e) =>
            {
                try
                {
                    if (MainInteractiveBoard.Children.Contains(stroke))
                    {
                        MainInteractiveBoard.Children.Remove(stroke);
                    }
                }
                catch { }
            };
            
            stroke.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        private Point? _lastMouseEraserPoint = null;

        private void EraseStrokeAt(Point point)
        {
            // 1. Native Inking Erase (Hardware-Accelerated DirectX MilCore, 0% miss rate)
            if (MainInkPresenter?.Strokes != null && MainInkPresenter.Strokes.Count > 0)
            {
                var eraserTool = _whiteboardManager?.ToolManager.EraserTool;
                if (eraserTool != null)
                {
                    var session = eraserTool.GetSession("mouse");
                    eraserTool.EraseByStrokeAt(
                        MainInkPresenter.Strokes,
                        point,
                        _eraserSize,
                        session,
                        onStrokeRemoved: (s) => _selectionManager?.RemoveObjectByStroke(s)
                    );
                }
            }

            // 2. Container-Embedded Inking Erase (Table & InteractiveImage)
            EraseContainerEmbeddedStrokesAt(point, isPointMode: false);

            // 3. Legacy / Canvas Elements Erase
            _eraserEngine ??= new EraserEngine(MainInteractiveBoard);
            _eraserEngine.EraseByStrokeAtPoint(point, _eraserSize, _eraserEngine.CurrentMouseSession);
        }

        private void EraseByPointAt(Point point, Point? lastPoint = null)
        {
            // 1. Native Inking Erase (Hardware-Accelerated DirectX MilCore)
            if (MainInkPresenter?.Strokes != null && MainInkPresenter.Strokes.Count > 0)
            {
                var eraserTool = _whiteboardManager?.ToolManager.EraserTool;
                if (eraserTool != null)
                {
                    var session = eraserTool.GetSession("mouse");
                    eraserTool.EraseByPointAt(
                        MainInkPresenter.Strokes,
                        point,
                        lastPoint,
                        _eraserSize,
                        session,
                        onStrokeAdded: (seg) => RegisterNewStrokeWithSelectionManager(seg),
                        onStrokeRemoved: (s) => _selectionManager?.RemoveObjectByStroke(s)
                    );
                }
            }

            // 2. Container-Embedded Inking Erase (Table & InteractiveImage)
            EraseContainerEmbeddedStrokesAt(point, isPointMode: true, lastPoint);

            // 3. Legacy / Canvas Elements Erase
            _eraserEngine ??= new EraserEngine(MainInteractiveBoard);
            _eraserEngine.EraseByPoint(point, _eraserSize, _eraserEngine.CurrentMouseSession);
        }

        /// <summary>
        /// Combines all removed and newly added elements from an eraser gesture session into ONE atomic Undo/Redo batch action.
        /// Also syncs removed and newly generated sub-stroke elements with SelectionManager.
        /// </summary>
        private void FinalizeEraseSession(EraseSession? session, string description)
        {
            if (session == null || !session.HasChanges) return;

            var batchAction = new UndoRedoAction
            {
                Type = ActionType.Batch,
                Description = description,
                Parent = MainInteractiveBoard
            };

            foreach (var orig in session.OriginalRemovedElements)
            {
                batchAction.BatchActions.Add(new UndoRedoAction
                {
                    Type = ActionType.Remove,
                    Element = orig,
                    Parent = MainInteractiveBoard,
                    Description = $"Erase {orig.GetType().Name}"
                });
                _selectionManager?.RemoveObjectByElement(orig);
            }

            foreach (var gen in session.ActiveGeneratedElements)
            {
                batchAction.BatchActions.Add(new UndoRedoAction
                {
                    Type = ActionType.Add,
                    Element = gen,
                    Parent = MainInteractiveBoard,
                    Description = $"Sub-stroke {gen.GetType().Name}"
                });
                RegisterNewObjectWithSelectionManager(gen);
            }

            RecordAction(batchAction);
        }

        // Helper method to calculate distance from point to line segment
        private double DistanceFromPointToLineSegment(Point point, Point lineStart, Point lineEnd)
        {
            double lineLength = Math.Sqrt(Math.Pow(lineEnd.X - lineStart.X, 2) + Math.Pow(lineEnd.Y - lineStart.Y, 2));
            if (lineLength == 0) return Math.Sqrt(Math.Pow(point.X - lineStart.X, 2) + Math.Pow(point.Y - lineStart.Y, 2));
            
            // Calculate projection of point onto line
            double t = Math.Max(0, Math.Min(1, 
                ((point.X - lineStart.X) * (lineEnd.X - lineStart.X) + (point.Y - lineStart.Y) * (lineEnd.Y - lineStart.Y)) / (lineLength * lineLength)));
            
            // Calculate closest point on line segment
            Point projection = new Point(
                lineStart.X + t * (lineEnd.X - lineStart.X),
                lineStart.Y + t * (lineEnd.Y - lineStart.Y)
            );
            
            // Return distance from point to projection
            return Math.Sqrt(Math.Pow(point.X - projection.X, 2) + Math.Pow(point.Y - projection.Y, 2));
        }

        public void OpenRulerTool()
        {
            RegisterRecentTool(
                "stem:Ruler",
                "Thước kẻ",
                QASmartTouch.Services.Tools.RecentToolsManager.RulerIconPath,
                Color.FromRgb(0x09, 0x84, 0xE3),
                () => OpenRulerTool(),
                "Dùng lại: Thước kẻ đo độ dài");

            // Close any existing ruler tool
            if (_activeRulerTool != null)
            {
                _activeRulerTool.Close();
                _activeRulerTool = null;
            }

            // Create and show new ruler tool
            _activeRulerTool = new Form2_15_RulerTool(this);
            _activeRulerTool.Owner = this;
            _activeRulerTool.Closed += (s, e) => { _activeRulerTool = null; };
            _activeRulerTool.Show(); // Use Show() not ShowDialog() to allow interaction with main window
            _activeRulerTool.Activate();
            _activeRulerTool.Focus();
        }

        public void OpenProtractorTool()
        {
            RegisterRecentTool(
                "stem:Protractor",
                "Đo độ",
                QASmartTouch.Services.Tools.RecentToolsManager.ProtractorIconPath,
                Color.FromRgb(0x6C, 0x5C, 0xE7),
                () => OpenProtractorTool(),
                "Dùng lại: Thước đo góc (Đo độ)");

            // Close any existing protractor tool
            if (_activeProtractorTool != null)
            {
                _activeProtractorTool.Close();
                _activeProtractorTool = null;
            }

            // Create and show new protractor tool
            _activeProtractorTool = new Form2_16_ProtractorTool();
            _activeProtractorTool.SetMainDashboard(this); // Pass MainDashboard reference
            _activeProtractorTool.Owner = this;
            _activeProtractorTool.Closed += (s, e) => { _activeProtractorTool = null; };
            _activeProtractorTool.Show(); // Use Show() not ShowDialog() to allow interaction with main window
            _activeProtractorTool.Activate();
            _activeProtractorTool.Focus();
        }

        public void OpenSetSquareTool()
        {
            RegisterRecentTool(
                "stem:SetSquare",
                "Thước Ê-ke",
                QASmartTouch.Services.Tools.RecentToolsManager.TriangleToolIconPath,
                Color.FromRgb(0x00, 0xB8, 0x94),
                () => OpenSetSquareTool(),
                "Dùng lại: Thước Ê-ke vuông góc");

            // Close any existing set square tool
            if (_activeSetSquareTool != null)
            {
                _activeSetSquareTool.Close();
                _activeSetSquareTool = null;
            }

            // Create and show new set square tool
            _activeSetSquareTool = new Form2_17_SetSquareTool(this);
            _activeSetSquareTool.Owner = this;
            _activeSetSquareTool.Closed += (s, e) => { _activeSetSquareTool = null; };
            _activeSetSquareTool.Show(); // Use Show() not ShowDialog() to allow interaction with main window
            _activeSetSquareTool.Activate();
            _activeSetSquareTool.Focus();
        }

        public void OpenCompassTool()
        {
            RegisterRecentTool(
                "stem:Compass",
                "Compa 3D",
                QASmartTouch.Services.Tools.RecentToolsManager.CompassIconPath,
                Color.FromRgb(0xE1, 0x70, 0x55),
                () => OpenCompassTool(),
                "Dùng lại: Compa 3D vẽ đường tròn");

            // Use 3D Compass Tool (professional version matching specification)
            // Close old 2D tool if exists
            if (_activeCompassTool != null)
            {
                _activeCompassTool.Close();
                _activeCompassTool = null;
            }

            // Close existing 3D tool if exists
            if (_activeCompassTool3D != null)
            {
                _activeCompassTool3D.Close();
                _activeCompassTool3D = null;
            }

            // Create and show new 3D compass tool
            _activeCompassTool3D = new Form2_18_CompassTool_3D();
            _activeCompassTool3D.Owner = this;
            _activeCompassTool3D.Closed += (s, e) => { _activeCompassTool3D = null; };
            
            _activeCompassTool3D.Show(); // Use Show() not ShowDialog() to allow interaction with main window
            _activeCompassTool3D.Activate();
            _activeCompassTool3D.Focus();
        }

        private void OpenCompassTool3D()
        {
            // Close any existing compass tool 3D
            if (_activeCompassTool3D != null)
            {
                _activeCompassTool3D.Close();
                _activeCompassTool3D = null;
            }

            // Create and show new compass tool 3D
            _activeCompassTool3D = new Form2_18_CompassTool_3D();
            _activeCompassTool3D.Owner = this;
            _activeCompassTool3D.Closed += (s, e) => { _activeCompassTool3D = null; };
            
            _activeCompassTool3D.Show(); // Use Show() not ShowDialog() to allow interaction with main window
            _activeCompassTool3D.Activate();
            _activeCompassTool3D.Focus();
        }

        private void ImportImage()
        {
            // Open file dialog to select image
            var openDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All Files|*.*",
                Title = "Chọn ảnh để chèn vào bảng"
            };

            if (openDialog.ShowDialog() == true)
            {
                try
                {
                    // Load image
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(openDialog.FileName, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    // Create Image control (QC_4.2_IMAGE_RESIZE_FIX: Không đặt MaxWidth/MaxHeight cứng để cho phép phóng to tự do)
                    var image = new Image
                    {
                        Source = bitmap,
                        Stretch = Stretch.Uniform,
                        MaxWidth = 400,
                        MaxHeight = 400,
                        Cursor = Cursors.Hand
                    };

                    // Calculate size maintaining aspect ratio
                    double aspectRatio = bitmap.PixelWidth / (double)bitmap.PixelHeight;
                    if (bitmap.PixelWidth > 400 || bitmap.PixelHeight > 400)
                    {
                        if (aspectRatio > 1)
                        {
                            image.Width = 400;
                            image.Height = 400 / aspectRatio;
                        }
                        else
                        {
                            image.Height = 400;
                            image.Width = 400 * aspectRatio;
                        }
                    }
                    else
                    {
                        image.Width = bitmap.PixelWidth;
                        image.Height = bitmap.PixelHeight;
                    }

                    // Position image at center of canvas
                    double left = (MainInteractiveBoard.ActualWidth - image.Width) / 2;
                    double top = (MainInteractiveBoard.ActualHeight - image.Height) / 2;
                    
                    Canvas.SetLeft(image, left);
                    Canvas.SetTop(image, top);

                    // Add to canvas (QC_4.2_MILESTONE_ZINDEX: Cấp phát Z-Index cao hơn nét vẽ cũ)
                    int mediaZ = AllocateMediaWidgetZIndex();
                    Panel.SetZIndex(image, mediaZ);
                    MainInteractiveBoard.Children.Add(image);

                    // Add to undo stack
                    RecordAddAction(image, "Draw");

                    // Đăng ký ngay vào SelectionManager để công cụ Chọn vùng có thể chọn và resize ngay lập tức
                    RefreshSelectableObjects();

                    // Make image draggable (enable selection tool temporarily)
                    image.MouseDown += (s, e) =>
                    {
                        if (_drawingEnabled || _eraserEnabled || _shapeDrawingEnabled) return;

                        if (e.LeftButton == MouseButtonState.Pressed)
                        {
                            _selectedElement = image;
                            _selectionStartPoint = e.GetPosition(MainInteractiveBoard);
                            _isDraggingElement = true;
                            MainInteractiveBoard.CaptureMouse();
                            e.Handled = true;
                        }
                    };
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi tải ảnh: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ImportVideo()
        {
            var openDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Video Files|*.mp4;*.avi;*.wmv;*.mov;*.mkv;*.webm|All Files|*.*",
                Title = "Chọn video để chèn vào bảng"
            };

            if (openDialog.ShowDialog() == true)
            {
                try
                {
                    InsertLocalVideoToCanvas(openDialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi chèn video: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /*
        public void InsertLocalVideoToCanvas(string videoPath)
        {
            if (string.IsNullOrEmpty(videoPath) || !System.IO.File.Exists(videoPath)) return;

            var containerGrid = new Grid
            {
                Width = 500,
                Height = 350,
                Tag = "LocalVideoContainer"
            };

            var mediaElement = new MediaElement
            {
                Source = new Uri(videoPath, UriKind.Absolute),
                LoadedBehavior = MediaState.Play,
                UnloadedBehavior = MediaState.Close,
                Stretch = Stretch.Uniform
            };

            var border = new Border
            {
                Background = new SolidColorBrush(Colors.Black),
                BorderBrush = new SolidColorBrush(Colors.DarkGray),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(8),
                Child = mediaElement
            };

            containerGrid.Children.Add(border);

            // Position at center of board
            double left = (MainInteractiveBoard.ActualWidth - 500) / 2;
            double top = (MainInteractiveBoard.ActualHeight - 350) / 2;
            if (left < 0) left = 50;
            if (top < 0) top = 50;

            Canvas.SetLeft(containerGrid, left);
            Canvas.SetTop(containerGrid, top);

            MainInteractiveBoard.Children.Add(containerGrid);
            RecordAddAction(containerGrid, $"Local Video: {System.IO.Path.GetFileName(videoPath)}");
            if (_selectionManager != null)
            {
                var selObj = new SelectableObject
                {
                    Element = containerGrid,
                    Type = ObjectType.Other,
                    Position = new Point(left, top),
                    Size = new Size(500, 350)
                };
                _selectionManager.AddObject(selObj);
            }
        }
        */

        private void CaptureScreenshot()
        {
            try
            {
                // Create a render target bitmap
                var renderBitmap = new RenderTargetBitmap(
                    (int)MainInteractiveBoard.ActualWidth,
                    (int)MainInteractiveBoard.ActualHeight,
                    96d, 96d,
                    PixelFormats.Pbgra32);

                // Render the canvas
                renderBitmap.Render(MainInteractiveBoard);

                // Open save file dialog
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PNG Image|*.png|JPEG Image|*.jpg|Bitmap Image|*.bmp",
                    DefaultExt = ".png",
                    FileName = $"iProBoard_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    BitmapEncoder encoder;
                    string ext = System.IO.Path.GetExtension(saveDialog.FileName).ToLower();
                    
                    switch (ext)
                    {
                        case ".jpg":
                        case ".jpeg":
                            encoder = new JpegBitmapEncoder();
                            break;
                        case ".bmp":
                            encoder = new BmpBitmapEncoder();
                            break;
                        default:
                            encoder = new PngBitmapEncoder();
                            break;
                    }

                    encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                    using (var fileStream = new System.IO.FileStream(saveDialog.FileName, System.IO.FileMode.Create))
                    {
                        encoder.Save(fileStream);
                    }

                    MessageBox.Show($"Đã lưu ảnh thành công!\n{saveDialog.FileName}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu ảnh: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
