using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.Windows.Shapes.Path;
using QASmartTouch.Forms;
using QASmartTouch.Helpers;
using QASmartTouch.Models;
using QASmartTouch.Shared;
using QASmartTouch.WhiteboardCore.IO;

namespace QASmartClass.WhiteboardCore.IO.SceneGraph
{
    /// <summary>
    /// SceneGraphSerializer — Bộ chuyển đổi 2 chiều giữa WPF UIElement và SceneElementNode.
    /// Giúp lưu và nạp 100% đối tượng bảng viết (nét vẽ, hình khối, ảnh, widget, video, chart).
    /// </summary>
    public static class SceneGraphSerializer
    {
        #region Serialize (UIElement → SceneElementNode)

        /// <summary>
        /// Chuyển đổi một UIElement trên Canvas thành SceneElementNode chuẩn JSON
        /// </summary>
        public static SceneElementNode? SerializeElement(
            UIElement element, 
            ZipArchive archive, 
            int boardIndex, 
            ref int imgIndex, 
            HashSet<string>? writtenAssets = null)
        {
            if (element == null || element is InkPresenter) return null;

            double left = Canvas.GetLeft(element);
            if (double.IsNaN(left)) left = 0;

            double top = Canvas.GetTop(element);
            if (double.IsNaN(top)) top = 0;

            int zIndex = Panel.GetZIndex(element);

            double width = 0;
            double height = 0;
            if (element is FrameworkElement fe)
            {
                width = fe.Width > 0 && !double.IsNaN(fe.Width) ? fe.Width : fe.ActualWidth;
                height = fe.Height > 0 && !double.IsNaN(fe.Height) ? fe.Height : fe.ActualHeight;
            }

            string? tagStr = (element as FrameworkElement)?.Tag?.ToString();

            // Tính toán góc xoay Transform
            double rotationAngle = 0;
            if (element.RenderTransform is RotateTransform rt)
            {
                rotationAngle = rt.Angle;
            }
            else if (element.RenderTransform is TransformGroup tg)
            {
                var rot = tg.Children.OfType<RotateTransform>().FirstOrDefault();
                if (rot != null) rotationAngle = rot.Angle;
            }

            // 1. NÉT VẼ BÚT (Polyline)
            if (element is Polyline polyline)
            {
                var node = new SceneElementNode
                {
                    ElementType = SceneElementType.Stroke,
                    Left = left,
                    Top = top,
                    Width = width,
                    Height = height,
                    ZIndex = zIndex,
                    Tag = tagStr,
                    StrokeThickness = polyline.StrokeThickness,
                    StrokeColorHex = ColorToHex(polyline.Stroke),
                    RotationAngle = rotationAngle
                };

                node.StrokePoints = new List<Point2D>();
                foreach (Point pt in polyline.Points)
                {
                    node.StrokePoints.Add(new Point2D(pt.X, pt.Y));
                }

                // Nhận diện brush type từ tag nếu có
                if (!string.IsNullOrEmpty(tagStr) && tagStr.StartsWith("Brush:", StringComparison.OrdinalIgnoreCase))
                {
                    node.BrushType = tagStr.Substring("Brush:".Length).Trim();
                }

                return node;
            }

            // 2. HÌNH ẢNH: Standalone Image HOẶC Khung ảnh tương tác (Border: InsertedImage / InteractiveImage)
            Image? imgControl = element as Image;
            BitmapSource? bs = imgControl?.Source as BitmapSource;
            Border? imgBorder = element as Border;

            if (imgControl == null && imgBorder != null)
            {
                TryExtractImageSource(imgBorder, out imgControl, out bs);
            }

            if (bs == null && (imgControl != null || (imgBorder != null && (tagStr == "InsertedImage" || tagStr == "InteractiveImage"))))
            {
                bs = CaptureElementBitmap(imgControl ?? (FrameworkElement)imgBorder!);
            }

            if (bs != null)
            {
                string assetFileName = $"assets/board_{boardIndex}_img_{imgIndex}.png";
                bool saved = false;
                if (writtenAssets != null && writtenAssets.Contains(assetFileName))
                {
                    saved = true;
                }
                else
                {
                    saved = SaveLoadService.WriteBitmapToZipEntry(archive, assetFileName, bs);
                    if (saved)
                    {
                        writtenAssets?.Add(assetFileName);
                    }
                }

                if (saved)
                {
                    imgIndex++;
                }

                var imgNode = new SceneElementNode
                {
                    ElementType = SceneElementType.Image,
                    Left = left,
                    Top = top,
                    Width = width > 0 ? width : (imgBorder != null && imgBorder.Width > 0 ? imgBorder.Width : (imgControl != null && imgControl.Width > 0 ? imgControl.Width : bs.PixelWidth)),
                    Height = height > 0 ? height : (imgBorder != null && imgBorder.Height > 0 ? imgBorder.Height : (imgControl != null && imgControl.Height > 0 ? imgControl.Height : bs.PixelHeight)),
                    ZIndex = zIndex,
                    Tag = tagStr ?? "InsertedImage",
                    AssetKey = saved ? assetFileName : null,
                    StretchMode = imgControl?.Stretch.ToString() ?? "Uniform",
                    RotationAngle = rotationAngle
                };

                if (imgBorder != null)
                {
                    imgNode.BorderColorHex = ColorToHex(imgBorder.BorderBrush);
                    imgNode.BorderThickness = imgBorder.BorderThickness.Left;

                    var ink = FindLogicalChild<InkPresenter>(imgBorder);
                    if (ink != null && ink.Strokes.Count > 0)
                    {
                        imgNode.AnnotationStrokes = SerializeStrokes(ink.Strokes);
                    }
                }

                return imgNode;
            }

            // 3. WIDGET NHÚNG TƯƠNG TÁC (YouTube, Google Maps, PhET, Local Video)
            if (!string.IsNullOrEmpty(tagStr) && (
                tagStr.Contains("YouTube") || 
                tagStr.Contains("GoogleMaps") || 
                tagStr.Contains("PhET") || 
                tagStr.Contains("LocalVideo")))
            {
                var webNode = new SceneElementNode
                {
                    ElementType = SceneElementType.WebEmbed,
                    Left = left,
                    Top = top,
                    Width = width > 0 ? width : 800,
                    Height = height > 0 ? height : 500,
                    ZIndex = zIndex,
                    Tag = tagStr,
                    RotationAngle = rotationAngle
                };

                if (tagStr.Contains("YouTube"))
                {
                    webNode.EmbedKind = "YouTube";
                    webNode.EmbedTitle = "Video YouTube";
                }
                else if (tagStr.Contains("PhET"))
                {
                    webNode.EmbedKind = "PhET";
                    webNode.EmbedTitle = "Mô phỏng PhET";
                }
                else if (tagStr.Contains("GoogleMaps"))
                {
                    webNode.EmbedKind = "GoogleMaps";
                    webNode.EmbedTitle = "Bản đồ Google Maps";
                }
                else
                {
                    webNode.EmbedKind = "LocalVideo";
                    webNode.EmbedTitle = "Video bài giảng";
                }

                return webNode;
            }

            // 4. HÌNH HỌC 2D: Rectangle
            if (element is Rectangle rect)
            {
                return new SceneElementNode
                {
                    ElementType = SceneElementType.Shape,
                    ShapeKind = "Rectangle",
                    Left = left,
                    Top = top,
                    Width = rect.Width > 0 ? rect.Width : rect.ActualWidth,
                    Height = rect.Height > 0 ? rect.Height : rect.ActualHeight,
                    ZIndex = zIndex,
                    Tag = tagStr,
                    FillColorHex = ColorToHex(rect.Fill),
                    StrokeColorHex = ColorToHex(rect.Stroke),
                    StrokeThickness = rect.StrokeThickness,
                    StrokeDashArray = rect.StrokeDashArray != null ? string.Join(",", rect.StrokeDashArray) : null,
                    RotationAngle = rotationAngle
                };
            }

            // 5. HÌNH HỌC 2D: Ellipse
            if (element is Ellipse ellipse)
            {
                return new SceneElementNode
                {
                    ElementType = SceneElementType.Shape,
                    ShapeKind = "Ellipse",
                    Left = left,
                    Top = top,
                    Width = ellipse.Width > 0 ? ellipse.Width : ellipse.ActualWidth,
                    Height = ellipse.Height > 0 ? ellipse.Height : ellipse.ActualHeight,
                    ZIndex = zIndex,
                    Tag = tagStr,
                    FillColorHex = ColorToHex(ellipse.Fill),
                    StrokeColorHex = ColorToHex(ellipse.Stroke),
                    StrokeThickness = ellipse.StrokeThickness,
                    StrokeDashArray = ellipse.StrokeDashArray != null ? string.Join(",", ellipse.StrokeDashArray) : null,
                    RotationAngle = rotationAngle
                };
            }

            // 6. HÌNH HỌC 2D: Line
            if (element is Line line)
            {
                var points = new List<Point2D>
                {
                    new Point2D(line.X1, line.Y1),
                    new Point2D(line.X2, line.Y2)
                };
                return new SceneElementNode
                {
                    ElementType = SceneElementType.Shape,
                    ShapeKind = "Line",
                    Left = left,
                    Top = top,
                    Width = width,
                    Height = height,
                    ZIndex = zIndex,
                    Tag = tagStr,
                    StrokePoints = points,
                    StrokeColorHex = ColorToHex(line.Stroke),
                    StrokeThickness = line.StrokeThickness,
                    StrokeDashArray = line.StrokeDashArray != null ? string.Join(",", line.StrokeDashArray) : null,
                    RotationAngle = rotationAngle
                };
            }

            // 7. HÌNH HỌC 2D: Polygon
            if (element is Polygon polygon)
            {
                var points = new List<Point2D>();
                foreach (Point pt in polygon.Points)
                {
                    points.Add(new Point2D(pt.X, pt.Y));
                }
                return new SceneElementNode
                {
                    ElementType = SceneElementType.Shape,
                    ShapeKind = "Polygon",
                    Left = left,
                    Top = top,
                    Width = width,
                    Height = height,
                    ZIndex = zIndex,
                    Tag = tagStr,
                    PolygonPoints = points,
                    FillColorHex = ColorToHex(polygon.Fill),
                    StrokeColorHex = ColorToHex(polygon.Stroke),
                    StrokeThickness = polygon.StrokeThickness,
                    StrokeDashArray = polygon.StrokeDashArray != null ? string.Join(",", polygon.StrokeDashArray) : null,
                    RotationAngle = rotationAngle
                };
            }

            // 8. HÌNH HỌC 2D: Path
            if (element is Path path)
            {
                return new SceneElementNode
                {
                    ElementType = SceneElementType.Shape,
                    ShapeKind = "Path",
                    Left = left,
                    Top = top,
                    Width = width,
                    Height = height,
                    ZIndex = zIndex,
                    Tag = tagStr,
                    GeometryData = GeometryToSvgString(path.Data),
                    FillColorHex = ColorToHex(path.Fill),
                    StrokeColorHex = ColorToHex(path.Stroke),
                    StrokeThickness = path.StrokeThickness,
                    StrokeDashArray = path.StrokeDashArray != null ? string.Join(",", path.StrokeDashArray) : null,
                    RotationAngle = rotationAngle
                };
            }

            // 9. VĂN BẢN (TextBlock / TextBox)
            if (element is TextBlock tb)
            {
                return new SceneElementNode
                {
                    ElementType = SceneElementType.Text,
                    Left = left,
                    Top = top,
                    Width = width,
                    Height = height,
                    ZIndex = zIndex,
                    Tag = tagStr,
                    TextContent = tb.Text,
                    FontSize = tb.FontSize,
                    FontFamily = tb.FontFamily?.Source ?? "Segoe UI",
                    FontWeight = tb.FontWeight.ToString(),
                    ForegroundColorHex = ColorToHex(tb.Foreground),
                    RotationAngle = rotationAngle
                };
            }

            // 10. BẢNG BIỂU DỮ LIỆU (Tag == "TableContainer")
            if (element is StackPanel spTable && (tagStr == "TableContainer" || (element as FrameworkElement)?.Tag?.ToString() == "TableContainer"))
            {
                var (tableGrid, tableData) = ExtractTableData(spTable);
                if (tableData != null)
                {
                    var tableNode = new SceneElementNode
                    {
                        ElementType = SceneElementType.Table,
                        Left = left,
                        Top = top,
                        Width = width > 0 ? width : spTable.ActualWidth,
                        Height = height > 0 ? height : spTable.ActualHeight,
                        ZIndex = zIndex,
                        Tag = "TableContainer",
                        TableRowCount = tableData.RowCount,
                        TableColumnCount = tableData.ColumnCount,
                        TableGridColorHex = tableData.BorderColor,
                        TableGridThickness = tableData.BorderThickness,
                        TableRowHeights = tableData.Rows.Select(r => r.Height).ToList(),
                        TableColumnWidths = tableData.Columns.Select(c => c.Width).ToList(),
                        TableCells = new List<TableCellDTO>(),
                        RotationAngle = rotationAngle
                    };

                    for (int r = 0; r < tableData.RowCount; r++)
                    {
                        for (int c = 0; c < tableData.ColumnCount; c++)
                        {
                            var cell = tableData.Cells[r, c];
                            tableNode.TableCells.Add(new TableCellDTO
                            {
                                Row = r,
                                Column = c,
                                Text = cell.Content ?? string.Empty,
                                FontFamily = cell.FontFamily,
                                FontSize = cell.FontSize,
                                FontWeight = cell.IsBold ? "Bold" : "Normal",
                                TextColorHex = cell.ForegroundColor,
                                BackgroundColorHex = cell.BackgroundColor,
                                TextAlignment = cell.HorizontalAlignment
                            });
                        }
                    }

                    var tableInk = FindLogicalChild<InkPresenter>(spTable);
                    if (tableInk != null && tableInk.Strokes.Count > 0)
                    {
                        tableNode.AnnotationStrokes = SerializeStrokes(tableInk.Strokes);
                    }

                    return tableNode;
                }
            }

            // 11. HỘP VĂN BẢN ĐỊNH DẠNG PHONG PHÚ (Tag == "TextBoxContainer")
            if (element is Grid gText && (tagStr == "TextBoxContainer" || (element as FrameworkElement)?.Tag?.ToString() == "TextBoxContainer"))
            {
                var rtb = FindLogicalChild<RichTextBox>(gText);
                if (rtb != null)
                {
                    string? docXaml = null;
                    try
                    {
                        var range = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd);
                        using var ms = new MemoryStream();
                        range.Save(ms, DataFormats.Xaml);
                        ms.Position = 0;
                        using var sr = new StreamReader(ms, Encoding.UTF8);
                        docXaml = sr.ReadToEnd();
                    }
                    catch { }

                    string plainText = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd).Text;

                    return new SceneElementNode
                    {
                        ElementType = SceneElementType.RichText,
                        Left = left,
                        Top = top,
                        Width = width > 0 ? width : gText.ActualWidth,
                        Height = height > 0 ? height : gText.ActualHeight,
                        ZIndex = zIndex,
                        Tag = "TextBoxContainer",
                        RichTextDocumentXaml = docXaml,
                        RichTextPlainText = plainText,
                        RotationAngle = rotationAngle
                    };
                }
            }

            // 12. KHỐI HÌNH 3D (Tag == "3DShape") HOẶC CONTAINER LỒNG NHAU (Canvas con chứa các hình con)
            if (element is Canvas subCanvas)
            {
                bool is3D = (tagStr == "3DShape" || (element as FrameworkElement)?.Tag?.ToString() == "3DShape");
                var containerNode = new SceneElementNode
                {
                    ElementType = is3D ? SceneElementType.Shape3D : SceneElementType.Container,
                    Left = left,
                    Top = top,
                    Width = width > 0 ? width : subCanvas.ActualWidth,
                    Height = height > 0 ? height : subCanvas.ActualHeight,
                    ZIndex = zIndex,
                    Tag = tagStr,
                    Model3DType = "Cube",
                    Children = new List<SceneElementNode>(),
                    RotationAngle = rotationAngle
                };

                if (is3D)
                {
                    // Trích xuất trực tiếp nét liền (Visible) và nét đứt (Hidden) kèm màu sắc & độ dày nét vẽ thực tế
                    bool thicknessFound = false;
                    foreach (UIElement child in subCanvas.Children)
                    {
                        if (child is Shape shape)
                        {
                            if (string.IsNullOrEmpty(containerNode.StrokeColorHex) && shape.Stroke != null)
                            {
                                containerNode.StrokeColorHex = ColorToHex(shape.Stroke);
                            }
                            if (!thicknessFound && shape.StrokeThickness > 0)
                            {
                                containerNode.StrokeThickness = shape.StrokeThickness;
                                thicknessFound = true;
                            }
                        }

                        if (child is Path p && p.Data != null)
                        {
                            if (p.StrokeDashArray != null && p.StrokeDashArray.Count > 0)
                            {
                                containerNode.HiddenGeometrySvg = GeometryToSvgString(p.Data);
                            }
                            else
                            {
                                containerNode.VisibleGeometrySvg = GeometryToSvgString(p.Data);
                            }
                        }
                    }
                }

                foreach (UIElement child in subCanvas.Children)
                {
                    var childNode = SerializeElement(child, archive, boardIndex, ref imgIndex, writtenAssets);
                    if (childNode != null)
                    {
                        containerNode.Children.Add(childNode);
                    }
                }

                return containerNode;
            }

            // 13. BIỂU ĐỒ TƯƠNG TÁC (Interactive Chart Container)
            if (element is FrameworkElement feChart && InteractiveChartHelper.IsChartContainer(feChart))
            {
                return SerializeChart(feChart as Grid, archive, boardIndex, ref imgIndex, left, top, width, height, zIndex, tagStr, writtenAssets, rotationAngle);
            }

            // 14. FALLBACK: Sử dụng XAML Serialization an toàn nếu không match các loại trên
            try
            {
                string xaml = System.Windows.Markup.XamlWriter.Save(element);
                return new SceneElementNode
                {
                    ElementType = SceneElementType.GenericXaml,
                    Left = left,
                    Top = top,
                    Width = width,
                    Height = height,
                    ZIndex = zIndex,
                    Tag = tagStr,
                    FallbackXaml = xaml,
                    RotationAngle = rotationAngle
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SceneGraphSerializer] Bỏ qua phần tử không tương thích: {ex.Message}");
                return null;
            }
        }

        private static SceneElementNode? SerializeChart(
            Grid? chartGrid,
            ZipArchive archive,
            int boardIndex,
            ref int imgIndex,
            double left,
            double top,
            double width,
            double height,
            int zIndex,
            string? tagStr,
            HashSet<string>? writtenAssets = null,
            double rotationAngle = 0)
        {
            if (chartGrid == null) return null;

            var (icon, title) = InteractiveChartHelper.GetChartHeaderInfo(chartGrid);
            var img = InteractiveChartHelper.GetChartImage(chartGrid);

            string assetFileName = $"assets/board_{boardIndex}_chart_{imgIndex}.png";
            bool assetSaved = false;

            if (writtenAssets != null && writtenAssets.Contains(assetFileName))
            {
                assetSaved = true;
            }
            else if (img?.Source is BitmapSource bs)
            {
                if (SaveLoadService.WriteBitmapToZipEntry(archive, assetFileName, bs))
                {
                    writtenAssets?.Add(assetFileName);
                    assetSaved = true;
                }
            }

            if (!assetSaved && chartGrid != null)
            {
                var capturedBmp = CaptureElementBitmap(chartGrid);
                if (capturedBmp != null)
                {
                    if (SaveLoadService.WriteBitmapToZipEntry(archive, assetFileName, capturedBmp))
                    {
                        writtenAssets?.Add(assetFileName);
                        assetSaved = true;
                    }
                }
            }

            if (assetSaved)
            {
                imgIndex++;
            }

            // Trích xuất nét vẽ ghi chú trên biểu đồ
            List<AnnotationStrokeDTO>? annotations = null;
            var chartInk = InteractiveChartHelper.GetChartInkPresenter(chartGrid);
            if (chartInk != null && chartInk.Strokes.Count > 0)
            {
                annotations = SerializeStrokes(chartInk.Strokes);
            }

            // Serialize cấu hình biểu đồ (lưu trữ trong chartGrid.Tag)
            string? configJson = null;
            string? configTypeName = null;

            object? configObj = chartGrid.Tag;
            if (configObj != null)
            {
                try
                {
                    configTypeName = configObj.GetType().FullName;
                    configJson = JsonSerializer.Serialize(configObj, configObj.GetType());
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SceneGraphSerializer] Lỗi serialize chart config: {ex.Message}");
                }
            }

            // Xác định kiểu biểu đồ
            string chartType = "Unknown";
            if (configObj != null)
            {
                chartType = configObj.GetType().Name.Replace("Configuration", "");
            }
            else if (!string.IsNullOrEmpty(title))
            {
                chartType = title;
            }

            return new SceneElementNode
            {
                ElementType = SceneElementType.Chart,
                Left = left,
                Top = top,
                Width = width > 0 ? width : (chartGrid.ActualWidth > 0 ? chartGrid.ActualWidth : 640),
                Height = height > 0 ? height : (chartGrid.ActualHeight > 0 ? chartGrid.ActualHeight : 420),
                ZIndex = zIndex,
                Tag = tagStr,
                AssetKey = assetSaved ? assetFileName : null,
                ChartTitle = title,
                ChartIcon = icon,
                ChartType = chartType,
                ChartConfigJson = configJson,
                ChartConfigTypeName = configTypeName,
                AnnotationStrokes = annotations,
                RotationAngle = rotationAngle
            };
        }

        #endregion

        #region Deserialize (SceneElementNode → UIElement)

        /// <summary>
        /// Khôi phục UIElement từ SceneElementNode
        /// </summary>
        public static UIElement? DeserializeElement(SceneElementNode node, Dictionary<string, BitmapImage> assetCache)
        {
            if (node == null) return null;

            UIElement? result = null;

            switch (node.ElementType)
            {
                case SceneElementType.Stroke:
                    result = RecreateStroke(node);
                    break;

                case SceneElementType.Shape:
                    result = RecreateShape(node);
                    break;

                case SceneElementType.Image:
                    result = RecreateImage(node, assetCache);
                    break;

                case SceneElementType.Text:
                    result = RecreateText(node);
                    break;

                case SceneElementType.Table:
                    result = RecreateTable(node);
                    break;

                case SceneElementType.RichText:
                    result = RecreateRichText(node);
                    break;

                case SceneElementType.WebEmbed:
                    result = RecreateWebEmbedPlaceholder(node);
                    break;

                case SceneElementType.Shape3D:
                    result = Recreate3DShape(node, assetCache);
                    break;

                case SceneElementType.Chart:
                    result = RecreateChart(node, assetCache);
                    break;

                case SceneElementType.Container:
                    result = RecreateContainer(node, assetCache);
                    break;

                case SceneElementType.GenericXaml:
                    if (!string.IsNullOrWhiteSpace(node.FallbackXaml))
                    {
                        try
                        {
                            result = System.Windows.Markup.XamlReader.Parse(node.FallbackXaml) as UIElement;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[SceneGraphSerializer] Lỗi parse Fallback XAML: {ex.Message}");
                        }
                    }
                    break;
            }

            if (result != null)
            {
                Canvas.SetLeft(result, node.Left);
                Canvas.SetTop(result, node.Top);

                int zIndex = node.ZIndex;
                if (zIndex <= 0)
                {
                    zIndex = QASmartTouch.Helpers.ZIndexConstants.UserContentBase;
                }
                Panel.SetZIndex(result, zIndex);

                if (node.Opacity < 1.0)
                {
                    result.Opacity = node.Opacity;
                }

                if (Math.Abs(node.RotationAngle) > 0.01)
                {
                    result.RenderTransformOrigin = new Point(0.5, 0.5);
                    result.RenderTransform = new RotateTransform(node.RotationAngle);
                }
            }

            return result;
        }

        #endregion

        #region Helper Recreators

        private static UIElement RecreateStroke(SceneElementNode node)
        {
            var polyline = new Polyline
            {
                StrokeThickness = node.StrokeThickness > 0 ? node.StrokeThickness : 2.0,
                Stroke = ParseBrush(node.StrokeColorHex, Brushes.White),
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                Tag = !string.IsNullOrEmpty(node.BrushType) ? $"Brush:{node.BrushType}" : node.Tag
            };

            if (node.StrokePoints != null)
            {
                foreach (var pt in node.StrokePoints)
                {
                    polyline.Points.Add(new Point(pt.X, pt.Y));
                }
            }

            return polyline;
        }

        private static UIElement? RecreateShape(SceneElementNode node)
        {
            switch (node.ShapeKind)
            {
                case "Rectangle":
                    var rect = new Rectangle
                    {
                        Width = Math.Max(1, node.Width),
                        Height = Math.Max(1, node.Height),
                        Fill = ParseBrush(node.FillColorHex, null), // ✅ null khi FillColorHex là null
                        Stroke = ParseBrush(node.StrokeColorHex, Brushes.White),
                        StrokeThickness = node.StrokeThickness,
                        Tag = node.Tag
                    };
                    if (!string.IsNullOrEmpty(node.StrokeDashArray))
                    {
                        rect.StrokeDashArray = ParseDashArray(node.StrokeDashArray);
                    }
                    return rect;

                case "Ellipse":
                    var ellipse = new Ellipse
                    {
                        Width = Math.Max(1, node.Width),
                        Height = Math.Max(1, node.Height),
                        Fill = ParseBrush(node.FillColorHex, null), // ✅ null khi FillColorHex là null
                        Stroke = ParseBrush(node.StrokeColorHex, Brushes.White),
                        StrokeThickness = node.StrokeThickness,
                        Tag = node.Tag
                    };
                    if (!string.IsNullOrEmpty(node.StrokeDashArray))
                    {
                        ellipse.StrokeDashArray = ParseDashArray(node.StrokeDashArray);
                    }
                    return ellipse;

                case "Line":
                    var line = new Line
                    {
                        Stroke = ParseBrush(node.StrokeColorHex, Brushes.White),
                        StrokeThickness = node.StrokeThickness,
                        Tag = node.Tag
                    };
                    if (!string.IsNullOrEmpty(node.StrokeDashArray))
                    {
                        line.StrokeDashArray = ParseDashArray(node.StrokeDashArray);
                    }
                    if (node.StrokePoints != null && node.StrokePoints.Count >= 2)
                    {
                        line.X1 = node.StrokePoints[0].X;
                        line.Y1 = node.StrokePoints[0].Y;
                        line.X2 = node.StrokePoints[1].X;
                        line.Y2 = node.StrokePoints[1].Y;
                    }
                    return line;

                case "Polygon":
                    var polygon = new Polygon
                    {
                        Fill = ParseBrush(node.FillColorHex, null), // ✅ null khi FillColorHex là null
                        Stroke = ParseBrush(node.StrokeColorHex, Brushes.White),
                        StrokeThickness = node.StrokeThickness,
                        Tag = node.Tag
                    };
                    if (!string.IsNullOrEmpty(node.StrokeDashArray))
                    {
                        polygon.StrokeDashArray = ParseDashArray(node.StrokeDashArray);
                    }
                    if (node.PolygonPoints != null)
                    {
                        foreach (var pt in node.PolygonPoints)
                        {
                            polygon.Points.Add(new Point(pt.X, pt.Y));
                        }
                    }
                    return polygon;

                case "Path":
                    if (!string.IsNullOrEmpty(node.GeometryData) && node.GeometryData != "System.Windows.Media.GeometryGroup")
                    {
                        try
                        {
                            var pathObj = new Path
                            {
                                Data = Geometry.Parse(node.GeometryData),
                                Fill = ParseBrush(node.FillColorHex, null), // ✅ null khi FillColorHex là null
                                Stroke = ParseBrush(node.StrokeColorHex, Brushes.White),
                                StrokeThickness = node.StrokeThickness,
                                Tag = node.Tag
                            };
                            if (!string.IsNullOrEmpty(node.StrokeDashArray))
                            {
                                pathObj.StrokeDashArray = ParseDashArray(node.StrokeDashArray);
                            }
                            return pathObj;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[SceneGraphSerializer] ⚠️ Lỗi parse path geometry: {ex.Message}");
                        }
                    }
                    break;
            }

            return null;
        }

        private static UIElement RecreateImage(SceneElementNode node, Dictionary<string, BitmapImage> assetCache)
        {
            BitmapImage? cachedBmp = null;
            if (!string.IsNullOrEmpty(node.AssetKey))
            {
                string key1 = node.AssetKey;
                string key2 = node.AssetKey.Replace('\\', '/');
                string key3 = node.AssetKey.Replace('/', '\\');
                string key4 = System.IO.Path.GetFileName(key2);

                if (assetCache.TryGetValue(key1, out var img1)) cachedBmp = img1;
                else if (assetCache.TryGetValue(key2, out var img2)) cachedBmp = img2;
                else if (assetCache.TryGetValue(key3, out var img3)) cachedBmp = img3;
                else if (assetCache.TryGetValue(key4, out var img4)) cachedBmp = img4;
            }

            double totalW = node.Width > 0 ? node.Width : (cachedBmp != null ? Math.Min(cachedBmp.PixelWidth, 800) : 400);
            double totalH = node.Height > 0 ? node.Height : (cachedBmp != null ? Math.Min(cachedBmp.PixelHeight, 600) : 300);

            var img = new Image
            {
                Source = cachedBmp,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Tag = node.Tag ?? $"asset:{node.AssetKey}"
            };

            if (Enum.TryParse<Stretch>(node.StretchMode, true, out var stretch))
            {
                img.Stretch = stretch;
            }

            // A. Khung ảnh UMind tương tác (InteractiveImage) với thanh Header Drag Bar
            if (node.Tag == "InteractiveImage")
            {
                const double headerHeight = 36;
                var mainGrid = new Grid();
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(headerHeight) });
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                // Header Drag Bar
                var headerBar = new Border
                {
                    Height = headerHeight,
                    Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)), // Slate 800 (#1E293B)
                    BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    CornerRadius = new CornerRadius(6, 6, 0, 0),
                    Cursor = Cursors.SizeAll,
                    Tag = "DragHandle"
                };

                var headerGrid = new Grid();
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var titleStack = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10, 0, 0, 0)
                };
                titleStack.Children.Add(new TextBlock
                {
                    Text = "🖼️",
                    FontSize = 13,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 6, 0)
                });
                titleStack.Children.Add(new TextBlock
                {
                    Text = "Hình ảnh",
                    Foreground = Brushes.White,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center
                });
                headerGrid.Children.Add(titleStack);
                headerBar.Child = headerGrid;
                Grid.SetRow(headerBar, 0);
                mainGrid.Children.Add(headerBar);

                // Image Wrapper + Inking Layer
                var imageWrapper = new Grid { ClipToBounds = true };
                imageWrapper.Children.Add(img);

                var imgInk = new InkPresenter { IsHitTestVisible = false, Tag = "ImageInkPresenter" };
                RestoreAnnotationStrokes(imgInk, node.AnnotationStrokes);
                imageWrapper.Children.Add(imgInk);

                Grid.SetRow(imageWrapper, 1);
                mainGrid.Children.Add(imageWrapper);

                var container = new Border
                {
                    Width = totalW,
                    Height = totalH,
                    Background = new SolidColorBrush(Color.FromRgb(24, 24, 27)), // Dark frame #18181B
                    BorderBrush = ParseBrush(node.BorderColorHex, new SolidColorBrush(Color.FromRgb(59, 130, 246))),
                    BorderThickness = new Thickness(node.BorderThickness > 0 ? node.BorderThickness : 2),
                    CornerRadius = new CornerRadius(8),
                    ClipToBounds = true,
                    Tag = "InteractiveImage"
                };
                container.Child = mainGrid;
                return container;
            }

            // B. Khung ảnh chèn thông thường (InsertedImage)
            if (node.Tag == "InsertedImage" || node.BorderThickness > 0 || (node.AnnotationStrokes != null && node.AnnotationStrokes.Count > 0))
            {
                var imageGrid = new Grid();
                imageGrid.Children.Add(img);

                var imgInkPresenter = new InkPresenter
                {
                    IsHitTestVisible = false,
                    Tag = "ImageInkPresenter"
                };
                RestoreAnnotationStrokes(imgInkPresenter, node.AnnotationStrokes);
                imageGrid.Children.Add(imgInkPresenter);

                var border = new Border
                {
                    Width = totalW,
                    Height = totalH,
                    Child = imageGrid,
                    BorderBrush = ParseBrush(node.BorderColorHex, new SolidColorBrush(Color.FromRgb(33, 150, 243))),
                    BorderThickness = new Thickness(node.BorderThickness > 0 ? node.BorderThickness : 2),
                    CornerRadius = new CornerRadius(4),
                    Background = Brushes.White,
                    Tag = node.Tag ?? "InsertedImage"
                };
                return border;
            }

            return img;
        }

        private static UIElement RecreateText(SceneElementNode node)
        {
            return new TextBlock
            {
                Text = node.TextContent ?? string.Empty,
                FontSize = node.FontSize > 0 ? node.FontSize : 16.0,
                FontFamily = new FontFamily(node.FontFamily ?? "Segoe UI"),
                Foreground = ParseBrush(node.ForegroundColorHex, Brushes.White),
                Tag = node.Tag
            };
        }

        private static UIElement RecreateContainer(SceneElementNode node, Dictionary<string, BitmapImage> assetCache)
        {
            var container = new Canvas
            {
                Width = node.Width,
                Height = node.Height,
                Tag = node.Tag
            };

            if (node.Children != null)
            {
                foreach (var childNode in node.Children)
                {
                    var childElement = DeserializeElement(childNode, assetCache);
                    if (childElement != null)
                    {
                        container.Children.Add(childElement);
                    }
                }
            }

            return container;
        }

        private static UIElement RecreateWebEmbedPlaceholder(SceneElementNode node)
        {
            // Tạo khung hiển thị thông minh cho Web Embed khi nạp lại
            var border = new Border
            {
                Width = node.Width > 0 ? node.Width : 800,
                Height = node.Height > 0 ? node.Height : 500,
                Background = new SolidColorBrush(Color.FromRgb(24, 24, 27)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38)),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(8),
                Tag = node.Tag ?? "InteractiveYouTubeVideo"
            };

            var stack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var iconText = new TextBlock
            {
                Text = node.EmbedKind == "YouTube" ? "▶ YouTube Video" : 
                       (node.EmbedKind == "PhET" ? "🔬 Mô phỏng PhET" : "🌐 Nội dung Web"),
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var descText = new TextBlock
            {
                Text = node.EmbedTitle ?? "Nhấn đúp hoặc kích hoạt để phát",
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            stack.Children.Add(iconText);
            stack.Children.Add(descText);
            border.Child = stack;

            return border;
        }

        private static UIElement Recreate3DShape(SceneElementNode node, Dictionary<string, BitmapImage> assetCache)
        {
            var container = new Canvas
            {
                Width = node.Width > 0 ? node.Width : 400,
                Height = node.Height > 0 ? node.Height : 400,
                Background = Brushes.Transparent, // Cho phép chạm/kéo thả trên màn hình tương tác
                Tag = "3DShape"
            };

            // Phục hồi màu sắc và độ dày nét vẽ ban đầu
            Brush strokeBrush = ParseBrush(node.StrokeColorHex, Brushes.White) ?? Brushes.White;
            double strokeThickness = node.StrokeThickness > 0 ? node.StrokeThickness : 2.0;

            // Tương thích ngược: Nếu node.StrokeThickness là 2.0 hoặc chưa có, kiểm tra children xem có lưu độ dày thực tế không
            if (node.Children != null && node.Children.Count > 0)
            {
                var childWithThickness = node.Children.FirstOrDefault(c => c.StrokeThickness > 0 && Math.Abs(c.StrokeThickness - 2.0) > 0.01);
                if (childWithThickness != null)
                {
                    strokeThickness = childWithThickness.StrokeThickness;
                }
                else if (node.StrokeThickness <= 0)
                {
                    var anyChild = node.Children.FirstOrDefault(c => c.StrokeThickness > 0);
                    if (anyChild != null) strokeThickness = anyChild.StrokeThickness;
                }
            }

            if (string.IsNullOrEmpty(node.StrokeColorHex) && node.Children != null && node.Children.Count > 0)
            {
                var childWithColor = node.Children.FirstOrDefault(c => !string.IsNullOrEmpty(c.StrokeColorHex));
                if (childWithColor != null)
                {
                    strokeBrush = ParseBrush(childWithColor.StrokeColorHex, strokeBrush) ?? strokeBrush;
                }
            }

            // A. Ưu tiên phục hồi từ chuỗi SVG trực tiếp (VisibleGeometrySvg / HiddenGeometrySvg)
            bool restoredFromSvg = false;
            if (!string.IsNullOrEmpty(node.HiddenGeometrySvg) || !string.IsNullOrEmpty(node.VisibleGeometrySvg))
            {
                try
                {
                    if (!string.IsNullOrEmpty(node.HiddenGeometrySvg))
                    {
                        var hiddenPath = new Path
                        {
                            Stroke = strokeBrush,
                            StrokeThickness = strokeThickness,
                            StrokeDashArray = new DoubleCollection { 4, 3 },
                            Data = Geometry.Parse(node.HiddenGeometrySvg)
                        };
                        container.Children.Add(hiddenPath);
                    }
                    if (!string.IsNullOrEmpty(node.VisibleGeometrySvg))
                    {
                        var visiblePath = new Path
                        {
                            Stroke = strokeBrush,
                            StrokeThickness = strokeThickness,
                            Data = Geometry.Parse(node.VisibleGeometrySvg)
                        };
                        container.Children.Add(visiblePath);
                    }
                    restoredFromSvg = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SceneGraphSerializer] ⚠️ Lỗi parse 3D SVG: {ex.Message}");
                }
            }

            // B. Phục hồi từ danh sách children vector thực tế
            if (!restoredFromSvg && node.Children != null && node.Children.Count > 0)
            {
                foreach (var childNode in node.Children)
                {
                    try
                    {
                        var childElement = DeserializeElement(childNode, assetCache);
                        if (childElement != null)
                        {
                            container.Children.Add(childElement);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SceneGraphSerializer] ⚠️ Bỏ qua child 3D lỗi: {ex.Message}");
                    }
                }
            }

            // C. Fallback bảo hiểm tuyệt đối cho bài giảng cũ từng bị lưu dạng lỗi (Children rỗng)
            if (container.Children.Count == 0)
            {
                BuildFallback3DGeometry(container, node.Model3DType ?? "Cube", container.Width, container.Height, strokeBrush, strokeThickness);
            }

            return container;
        }

        private static void BuildFallback3DGeometry(Canvas container, string modelType, double w, double h, Brush stroke, double thickness)
        {
            GeometryGroup visibleGeo = new GeometryGroup();
            GeometryGroup hiddenGeo = new GeometryGroup();

            double x = 0, y = 0;
            switch (modelType)
            {
                case "Cube":
                default:
                    double dx = w * 0.25;
                    double dy = h * 0.25;

                    Point a = new Point(x, y + dy);
                    Point b = new Point(x + w - dx, y + dy);
                    Point c = new Point(x + w - dx, y + h);
                    Point d = new Point(x, y + h);

                    Point aPrime = new Point(x + dx, y);
                    Point bPrime = new Point(x + w, y);
                    Point cPrime = new Point(x + w, y + h - dy);
                    Point dPrime = new Point(x + dx, y + h - dy);

                    // Nét liền: Mặt trước và các cạnh thấy
                    visibleGeo.Children.Add(new LineGeometry(a, b));
                    visibleGeo.Children.Add(new LineGeometry(b, c));
                    visibleGeo.Children.Add(new LineGeometry(c, d));
                    visibleGeo.Children.Add(new LineGeometry(d, a));

                    visibleGeo.Children.Add(new LineGeometry(a, aPrime));
                    visibleGeo.Children.Add(new LineGeometry(b, bPrime));
                    visibleGeo.Children.Add(new LineGeometry(c, cPrime));

                    visibleGeo.Children.Add(new LineGeometry(aPrime, bPrime));
                    visibleGeo.Children.Add(new LineGeometry(bPrime, cPrime));

                    // Nét đứt: Cạnh khuất đi qua đỉnh sau dPrime
                    hiddenGeo.Children.Add(new LineGeometry(dPrime, aPrime));
                    hiddenGeo.Children.Add(new LineGeometry(dPrime, cPrime));
                    hiddenGeo.Children.Add(new LineGeometry(dPrime, d));
                    break;
            }

            if (hiddenGeo.Children.Count > 0)
            {
                container.Children.Add(new Path
                {
                    Stroke = stroke,
                    StrokeThickness = thickness,
                    StrokeDashArray = new DoubleCollection { 4, 3 },
                    Data = hiddenGeo
                });
            }

            if (visibleGeo.Children.Count > 0)
            {
                container.Children.Add(new Path
                {
                    Stroke = stroke,
                    StrokeThickness = thickness,
                    Data = visibleGeo
                });
            }
        }

        private static UIElement? RecreateChart(SceneElementNode node, Dictionary<string, BitmapImage> assetCache)
        {
            ImageSource? chartBmp = null;
            if (!string.IsNullOrEmpty(node.AssetKey))
            {
                string key1 = node.AssetKey;
                string key2 = node.AssetKey.Replace('\\', '/');
                string key3 = node.AssetKey.Replace('/', '\\');
                string key4 = System.IO.Path.GetFileName(key2);

                if (assetCache.TryGetValue(key1, out var img1)) chartBmp = img1;
                else if (assetCache.TryGetValue(key2, out var img2)) chartBmp = img2;
                else if (assetCache.TryGetValue(key3, out var img3)) chartBmp = img3;
                else if (assetCache.TryGetValue(key4, out var img4)) chartBmp = img4;
            }

            // Phục hồi đối tượng cấu hình
            object? config = null;
            if (!string.IsNullOrEmpty(node.ChartConfigJson))
            {
                try
                {
                    Type? targetType = null;
                    if (!string.IsNullOrEmpty(node.ChartConfigTypeName))
                    {
                        targetType = Type.GetType(node.ChartConfigTypeName);
                    }

                    if (targetType == null)
                    {
                        string typeHint = (node.ChartType ?? "") + " " + (node.ChartTitle ?? "");
                        if (typeHint.Contains("Bar", StringComparison.OrdinalIgnoreCase) || typeHint.Contains("cột", StringComparison.OrdinalIgnoreCase))
                            targetType = typeof(BarChartConfiguration);
                        else if (typeHint.Contains("Line", StringComparison.OrdinalIgnoreCase) || typeHint.Contains("đường", StringComparison.OrdinalIgnoreCase))
                            targetType = typeof(LineChartConfiguration);
                        else if (typeHint.Contains("Pie", StringComparison.OrdinalIgnoreCase) || typeHint.Contains("tròn", StringComparison.OrdinalIgnoreCase))
                            targetType = typeof(PieChartConfiguration);
                        else if (typeHint.Contains("Area", StringComparison.OrdinalIgnoreCase) || typeHint.Contains("miền", StringComparison.OrdinalIgnoreCase))
                            targetType = typeof(AreaChartConfiguration);
                        else if (typeHint.Contains("Scatter", StringComparison.OrdinalIgnoreCase) || typeHint.Contains("tán", StringComparison.OrdinalIgnoreCase))
                            targetType = typeof(ScatterChartConfiguration);
                        else if (typeHint.Contains("Radar", StringComparison.OrdinalIgnoreCase) || typeHint.Contains("radar", StringComparison.OrdinalIgnoreCase))
                            targetType = typeof(RadarChartConfiguration);
                    }

                    if (targetType != null)
                    {
                        config = JsonSerializer.Deserialize(node.ChartConfigJson, targetType);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SceneGraphSerializer] Lỗi deserialize chart config: {ex.Message}");
                }
            }

            double initialW = node.Width > 0 ? node.Width : 640;
            double initialH = node.Height > 0 ? (node.Height - InteractiveChartHelper.HeaderBarHeight) : 420;
            if (initialH <= 50) initialH = 420;

            // Cơ chế tự phục hồi hiển thị (Self-healing fallback) nếu asset ảnh bị thiếu
            if (chartBmp == null)
            {
                if (config is BarChartConfiguration barConfig)
                {
                    chartBmp = RenderBarChartFallback(barConfig, initialW, initialH);
                }
                else if (config is LineChartConfiguration lineConfig)
                {
                    chartBmp = RenderLineChartFallback(lineConfig, initialW, initialH);
                }
                else if (config is PieChartConfiguration pieConfig)
                {
                    chartBmp = RenderPieChartFallback(pieConfig, initialW, initialH);
                }
                else if (config is AreaChartConfiguration areaConfig)
                {
                    chartBmp = RenderAreaChartFallback(areaConfig, initialW, initialH);
                }
                else if (config is ScatterChartConfiguration scatterConfig)
                {
                    chartBmp = RenderScatterChartFallback(scatterConfig, initialW, initialH);
                }
                else if (config is RadarChartConfiguration radarConfig)
                {
                    chartBmp = RenderRadarChartFallback(radarConfig, initialW, initialH);
                }
            }

            var options = new InteractiveChartHelper.ChartContainerOptions
            {
                ChartTitle = node.ChartTitle ?? "Biểu đồ",
                Icon = node.ChartIcon ?? "📊",
                ImageSource = chartBmp,
                InitialWidth = initialW,
                InitialHeight = initialH,
                Configuration = config,
                TargetLeft = node.Left,
                TargetTop = node.Top
            };

            var chartContainer = InteractiveChartHelper.BuildChartContainer(options);

            // Phục hồi nét vẽ annotation trên thân biểu đồ
            if (node.AnnotationStrokes != null && node.AnnotationStrokes.Count > 0)
            {
                var chartInk = InteractiveChartHelper.GetChartInkPresenter(chartContainer);
                RestoreAnnotationStrokes(chartInk, node.AnnotationStrokes);
            }

            return chartContainer;
        }

        /// <summary>
        /// Tự động tái tạo hình ảnh biểu đồ cột từ cấu hình JSON khi asset PNG bị thiếu hoặc lỗi (Self-Healing)
        /// </summary>
        private static RenderTargetBitmap? RenderBarChartFallback(BarChartConfiguration config, double width, double height)
        {
            try
            {
                int w = (int)Math.Max(300, width);
                int h = (int)Math.Max(200, height);

                var canvas = new Canvas
                {
                    Width = w,
                    Height = h,
                    Background = Brushes.White
                };

                var padding = new Thickness(60, 40, 40, 50);
                double chartWidth = w - padding.Left - padding.Right;
                double chartHeight = h - padding.Top - padding.Bottom;

                if (chartWidth <= 0 || chartHeight <= 0 || config.ChartData == null || config.ChartData.Count == 0)
                    return null;

                double maxValue = config.ChartData.Max(d => d.Value);
                if (maxValue <= 0) maxValue = 100;
                double scale = chartHeight / (maxValue * 1.15);

                // Grid lines
                if (config.ShowGrid)
                {
                    for (int i = 1; i <= 5; i++)
                    {
                        double y = padding.Top + (chartHeight / 5) * i;
                        canvas.Children.Add(new Line
                        {
                            X1 = padding.Left, Y1 = y,
                            X2 = padding.Left + chartWidth, Y2 = y,
                            Stroke = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                            StrokeThickness = 1,
                            StrokeDashArray = new DoubleCollection { 4, 4 }
                        });

                        var valLabel = new TextBlock
                        {
                            Text = ((maxValue * 1.15 / 5) * (5 - i)).ToString("F0"),
                            FontSize = 10,
                            Foreground = new SolidColorBrush(Color.FromRgb(120, 120, 120))
                        };
                        Canvas.SetLeft(valLabel, padding.Left - 35);
                        Canvas.SetTop(valLabel, y - 7);
                        canvas.Children.Add(valLabel);
                    }
                }

                // Axes
                canvas.Children.Add(new Line
                {
                    X1 = padding.Left, Y1 = padding.Top + chartHeight,
                    X2 = padding.Left + chartWidth, Y2 = padding.Top + chartHeight,
                    Stroke = Brushes.Black, StrokeThickness = 1.5
                });
                canvas.Children.Add(new Line
                {
                    X1 = padding.Left, Y1 = padding.Top,
                    X2 = padding.Left, Y2 = padding.Top + chartHeight,
                    Stroke = Brushes.Black, StrokeThickness = 1.5
                });

                // Y-axis label
                if (!string.IsNullOrEmpty(config.YAxisLabel))
                {
                    var yLbl = new TextBlock
                    {
                        Text = config.YAxisLabel,
                        FontSize = 11,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = Brushes.Black
                    };
                    Canvas.SetLeft(yLbl, 10);
                    Canvas.SetTop(yLbl, padding.Top - 25);
                    canvas.Children.Add(yLbl);
                }

                // Bars
                int barCount = config.ChartData.Count;
                double barSpacing = chartWidth / barCount;
                double barWidth = barSpacing * (config.BarWidth > 0 ? config.BarWidth / 100.0 : 0.6);
                if (barWidth <= 0 || barWidth > barSpacing) barWidth = barSpacing * 0.6;

                var defaultColors = new[]
                {
                    Color.FromRgb(102, 126, 234),
                    Color.FromRgb(118, 75, 162),
                    Color.FromRgb(237, 100, 166),
                    Color.FromRgb(255, 107, 107),
                    Color.FromRgb(255, 159, 67),
                    Color.FromRgb(72, 219, 251)
                };

                for (int i = 0; i < barCount; i++)
                {
                    var data = config.ChartData[i];
                    double barH = Math.Max(2, data.Value * scale);
                    double x = padding.Left + (i * barSpacing) + (barSpacing - barWidth) / 2;
                    double y = padding.Top + chartHeight - barH;

                    Color c = config.PrimaryColor.A > 0 ? config.PrimaryColor : defaultColors[i % defaultColors.Length];

                    var bar = new Rectangle
                    {
                        Width = barWidth,
                        Height = barH,
                        Fill = new SolidColorBrush(c),
                        RadiusX = 4,
                        RadiusY = 4,
                        Effect = new System.Windows.Media.Effects.DropShadowEffect
                        {
                            Color = Colors.Black,
                            Opacity = 0.25,
                            ShadowDepth = 3,
                            BlurRadius = 6
                        }
                    };
                    Canvas.SetLeft(bar, x);
                    Canvas.SetTop(bar, y);
                    canvas.Children.Add(bar);

                    // 3D effect nếu config bật Style3D
                    if (config.Style3D)
                    {
                        double depth = 6;
                        var side = new Polygon
                        {
                            Points = new PointCollection
                            {
                                new Point(x + barWidth, y),
                                new Point(x + barWidth + depth, y - depth),
                                new Point(x + barWidth + depth, y + barH - depth),
                                new Point(x + barWidth, y + barH)
                            },
                            Fill = new SolidColorBrush(Color.FromArgb(100, 0, 0, 0))
                        };
                        var topPoly = new Polygon
                        {
                            Points = new PointCollection
                            {
                                new Point(x, y),
                                new Point(x + depth, y - depth),
                                new Point(x + barWidth + depth, y - depth),
                                new Point(x + barWidth, y)
                            },
                            Fill = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255))
                        };
                        canvas.Children.Add(side);
                        canvas.Children.Add(topPoly);
                    }

                    // Value text
                    if (config.ShowValues)
                    {
                        var valTxt = new TextBlock
                        {
                            Text = data.Value.ToString("F0"),
                            FontSize = 11,
                            FontWeight = FontWeights.Bold,
                            Foreground = new SolidColorBrush(Color.FromRgb(52, 73, 94)),
                            TextAlignment = TextAlignment.Center,
                            Width = Math.Max(40, barWidth + 20)
                        };
                        Canvas.SetLeft(valTxt, x + (barWidth - valTxt.Width) / 2);
                        Canvas.SetTop(valTxt, y - 20);
                        canvas.Children.Add(valTxt);
                    }

                    // Category label
                    var catLbl = new TextBlock
                    {
                        Text = data.Label,
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Color.FromRgb(70, 70, 70)),
                        TextAlignment = TextAlignment.Center,
                        Width = Math.Max(50, barSpacing)
                    };
                    Canvas.SetLeft(catLbl, padding.Left + (i * barSpacing) + (barSpacing - catLbl.Width) / 2);
                    Canvas.SetTop(catLbl, padding.Top + chartHeight + 8);
                    canvas.Children.Add(catLbl);
                }

                canvas.Measure(new Size(w, h));
                canvas.Arrange(new Rect(0, 0, w, h));
                canvas.UpdateLayout();

                var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(canvas);
                return rtb;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SceneGraphSerializer] Lỗi render fallback bar chart: {ex.Message}");
                return null;
            }
        }

        private static RenderTargetBitmap? CaptureElementBitmap(FrameworkElement element)
        {
            try
            {
                double w = element.ActualWidth > 0 ? element.ActualWidth : element.Width;
                double h = element.ActualHeight > 0 ? element.ActualHeight : element.Height;
                if (w <= 0 || h <= 0)
                {
                    element.Measure(new Size(1920, 1080));
                    element.Arrange(new Rect(0, 0, element.DesiredSize.Width, element.DesiredSize.Height));
                    w = element.ActualWidth > 0 ? element.ActualWidth : element.DesiredSize.Width;
                    h = element.ActualHeight > 0 ? element.ActualHeight : element.DesiredSize.Height;
                }

                if (w <= 0 || h <= 0) return null;

                var rtb = new RenderTargetBitmap((int)Math.Max(1, w), (int)Math.Max(1, h), 96, 96, PixelFormats.Pbgra32);
                rtb.Render(element);
                return rtb;
            }
            catch
            {
                return null;
            }
        }

        private static string? GeometryToSvgString(Geometry? geo)
        {
            if (geo == null) return null;
            try
            {
                return PathGeometry.CreateFromGeometry(geo).ToString();
            }
            catch
            {
                return geo.ToString();
            }
        }

        private static bool TryExtractImageSource(Border border, out Image? foundImage, out BitmapSource? foundBitmap)
        {
            foundImage = null;
            foundBitmap = null;
            if (border == null) return false;

            if (border.Child is Image directImg)
            {
                foundImage = directImg;
            }
            else
            {
                foundImage = FindLogicalChild<Image>(border);
            }

            if (foundImage?.Source is BitmapSource bs)
            {
                foundBitmap = bs;
                return true;
            }

            return false;
        }

        private static T? FindLogicalChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            if (parent is Panel panel)
            {
                foreach (UIElement child in panel.Children)
                {
                    if (child is T match) return match;
                    var deeper = FindLogicalChild<T>(child);
                    if (deeper != null) return deeper;
                }
            }
            else if (parent is ContentControl cc && cc.Content is DependencyObject dcc)
            {
                if (dcc is T match) return match;
                return FindLogicalChild<T>(dcc);
            }
            else if (parent is Decorator dec && dec.Child != null)
            {
                if (dec.Child is T match) return match;
                return FindLogicalChild<T>(dec.Child);
            }

            return null;
        }

        private static (Grid? tableGrid, TableData? tableData) ExtractTableData(StackPanel spTable)
        {
            if (spTable == null) return (null, null);

            // 1. Duyệt cấu trúc chuẩn: spTable -> tableBodyGrid -> outerBorder -> tableGrid
            foreach (var child in spTable.Children)
            {
                if (child is Grid bodyGrid)
                {
                    foreach (var sub in bodyGrid.Children)
                    {
                        if (sub is Border b && b.Child is Grid tg && tg.Tag is TableData td)
                        {
                            return (tg, td);
                        }
                    }
                }
            }

            // 2. Tìm sâu trong toàn bộ cây con nếu cấu trúc có biến thể
            var allGrids = new List<Grid>();
            FindAllChildren(spTable, allGrids);
            foreach (var g in allGrids)
            {
                if (g.Tag is TableData td)
                {
                    return (g, td);
                }
            }

            return (null, null);
        }

        private static void FindAllChildren<T>(DependencyObject parent, List<T> results) where T : DependencyObject
        {
            if (parent == null) return;
            if (parent is Panel panel)
            {
                foreach (UIElement child in panel.Children)
                {
                    if (child is T match) results.Add(match);
                    FindAllChildren(child, results);
                }
            }
            else if (parent is ContentControl cc && cc.Content is DependencyObject dcc)
            {
                if (dcc is T match) results.Add(match);
                FindAllChildren(dcc, results);
            }
            else if (parent is Decorator dec && dec.Child != null)
            {
                if (dec.Child is T match) results.Add(match);
                FindAllChildren(dec.Child, results);
            }
        }

        private static List<AnnotationStrokeDTO> SerializeStrokes(StrokeCollection strokes)
        {
            var list = new List<AnnotationStrokeDTO>();
            if (strokes == null) return list;

            foreach (var s in strokes)
            {
                var dto = new AnnotationStrokeDTO
                {
                    StrokeThickness = s.DrawingAttributes.Width,
                    StrokeColorHex = $"#{s.DrawingAttributes.Color.A:X2}{s.DrawingAttributes.Color.R:X2}{s.DrawingAttributes.Color.G:X2}{s.DrawingAttributes.Color.B:X2}"
                };

                foreach (var pt in s.StylusPoints)
                {
                    dto.RelativePoints.Add(new Point2D(pt.X, pt.Y));
                }

                list.Add(dto);
            }

            return list;
        }

        private static void RestoreAnnotationStrokes(InkPresenter? inkPresenter, List<AnnotationStrokeDTO>? dtoList, double targetWidth = 0, double targetHeight = 0)
        {
            if (inkPresenter == null || dtoList == null || dtoList.Count == 0) return;
            try
            {
                foreach (var dto in dtoList)
                {
                    if (dto.RelativePoints == null || dto.RelativePoints.Count == 0) continue;

                    var points = new StylusPointCollection();
                    foreach (var p in dto.RelativePoints)
                    {
                        points.Add(new StylusPoint(p.X, p.Y));
                    }

                    var da = new DrawingAttributes
                    {
                        Width = dto.StrokeThickness > 0 ? dto.StrokeThickness : 2.0,
                        Height = dto.StrokeThickness > 0 ? dto.StrokeThickness : 2.0,
                        Color = ParseBrush(dto.StrokeColorHex, Brushes.Yellow) is SolidColorBrush scb ? scb.Color : Colors.Yellow,
                        FitToCurve = true
                    };

                    var stroke = new Stroke(points, da);
                    inkPresenter.Strokes.Add(stroke);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SceneGraphSerializer] ⚠️ Lỗi restore annotation strokes: {ex.Message}");
            }
        }

        private static UIElement RecreateTable(SceneElementNode node)
        {
            int rows = node.TableRowCount > 0 ? node.TableRowCount : 2;
            int cols = node.TableColumnCount > 0 ? node.TableColumnCount : 2;
            var tableData = new QASmartTouch.Models.TableData(rows, cols)
            {
                BorderColor = node.TableGridColorHex ?? "#E1E8ED",
                BorderThickness = node.TableGridThickness > 0 ? node.TableGridThickness : 1.0
            };

            if (node.TableRowHeights != null && node.TableRowHeights.Count == rows)
            {
                for (int i = 0; i < rows; i++) tableData.Rows[i].Height = node.TableRowHeights[i];
            }
            if (node.TableColumnWidths != null && node.TableColumnWidths.Count == cols)
            {
                for (int i = 0; i < cols; i++) tableData.Columns[i].Width = node.TableColumnWidths[i];
            }

            if (node.TableCells != null)
            {
                foreach (var cellDto in node.TableCells)
                {
                    if (cellDto.Row < rows && cellDto.Column < cols)
                    {
                        var cell = tableData.Cells[cellDto.Row, cellDto.Column];
                        cell.Content = cellDto.Text ?? string.Empty;
                        if (!string.IsNullOrEmpty(cellDto.FontFamily)) cell.FontFamily = cellDto.FontFamily;
                        if (cellDto.FontSize > 0) cell.FontSize = cellDto.FontSize;
                        cell.IsBold = string.Equals(cellDto.FontWeight, "Bold", StringComparison.OrdinalIgnoreCase);
                        if (!string.IsNullOrEmpty(cellDto.TextColorHex)) cell.ForegroundColor = cellDto.TextColorHex;
                        if (!string.IsNullOrEmpty(cellDto.BackgroundColorHex)) cell.BackgroundColor = cellDto.BackgroundColorHex;
                        if (!string.IsNullOrEmpty(cellDto.TextAlignment)) cell.HorizontalAlignment = cellDto.TextAlignment;
                    }
                }
            }

            // Xây dựng giao diện Table Grid
            var tableGrid = new Grid
            {
                Background = Brushes.White,
                Tag = tableData
            };

            for (int c = 0; c < tableData.ColumnCount; c++)
            {
                tableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(tableData.Columns[c].Width) });
            }
            for (int r = 0; r < tableData.RowCount; r++)
            {
                tableGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(tableData.Rows[r].Height) });
            }

            for (int r = 0; r < tableData.RowCount; r++)
            {
                for (int c = 0; c < tableData.ColumnCount; c++)
                {
                    var cell = tableData.Cells[r, c];
                    var border = new Border
                    {
                        BorderBrush = ParseBrush(tableData.BorderColor, Brushes.Gray),
                        BorderThickness = new Thickness(tableData.BorderThickness),
                        Background = ParseBrush(cell.BackgroundColor, Brushes.Transparent),
                        Padding = new Thickness(6)
                    };
                    Grid.SetRow(border, r);
                    Grid.SetColumn(border, c);

                    var tb = new TextBlock
                    {
                        Text = cell.Content,
                        FontFamily = new FontFamily(cell.FontFamily ?? "Segoe UI"),
                        FontSize = cell.FontSize > 0 ? cell.FontSize : 14,
                        FontWeight = cell.IsBold ? FontWeights.Bold : FontWeights.Normal,
                        Foreground = ParseBrush(cell.ForegroundColor, Brushes.Black),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    border.Child = tb;
                    tableGrid.Children.Add(border);
                }
            }

            var outerBorder = new Border
            {
                BorderBrush = ParseBrush(tableData.BorderColor, Brushes.Gray),
                BorderThickness = new Thickness(tableData.BorderThickness),
                Child = tableGrid,
                Background = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var tableBodyGrid = new Grid();
            tableBodyGrid.Children.Add(outerBorder);

            var tableInkPresenter = new InkPresenter
            {
                IsHitTestVisible = false,
                Tag = "TableInkPresenter"
            };
            RestoreAnnotationStrokes(tableInkPresenter, node.AnnotationStrokes);
            tableBodyGrid.Children.Add(tableInkPresenter);

            var dragHandleBar = new Border
            {
                Height = 34,
                Background = new SolidColorBrush(Color.FromRgb(41, 128, 185)),
                CornerRadius = new CornerRadius(6, 6, 0, 0),
                Cursor = Cursors.SizeAll,
                Tag = "DragHandle",
                ToolTip = "Nhấn giữ để di chuyển Bảng trên màn hình"
            };

            var dragHandleGrid = new Grid();
            var titleText = new TextBlock
            {
                Text = "📊 Bảng",
                Foreground = Brushes.White,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0)
            };
            dragHandleGrid.Children.Add(titleText);
            dragHandleBar.Child = dragHandleGrid;

            double totalWidth = Math.Max(tableData.GetTotalWidth() + (tableData.BorderThickness * 2), 280);
            double totalHeight = tableData.GetTotalHeight() + (tableData.BorderThickness * 2) + 34;

            var finalContainer = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Background = Brushes.Transparent,
                Tag = "TableContainer",
                Width = node.Width > 0 ? node.Width : totalWidth,
                Height = node.Height > 0 ? node.Height : totalHeight
            };

            finalContainer.Children.Add(dragHandleBar);
            finalContainer.Children.Add(tableBodyGrid);

            return finalContainer;
        }

        private static UIElement RecreateRichText(SceneElementNode node)
        {
            var container = new Grid
            {
                Width = node.Width > 0 ? node.Width : 250,
                Height = node.Height > 0 ? node.Height : 150,
                Tag = "TextBoxContainer",
                Background = Brushes.Transparent
            };

            var rtb = new RichTextBox
            {
                Margin = new Thickness(10, 34, 10, 10),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                BorderThickness = new Thickness(1),
                Background = Brushes.White,
                AcceptsReturn = true
            };

            if (!string.IsNullOrEmpty(node.RichTextDocumentXaml))
            {
                try
                {
                    var range = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd);
                    using var ms = new MemoryStream(Encoding.UTF8.GetBytes(node.RichTextDocumentXaml));
                    range.Load(ms, DataFormats.Xaml);
                }
                catch
                {
                    if (!string.IsNullOrEmpty(node.RichTextPlainText))
                    {
                        rtb.Document.Blocks.Clear();
                        rtb.Document.Blocks.Add(new Paragraph(new Run(node.RichTextPlainText)));
                    }
                }
            }
            else if (!string.IsNullOrEmpty(node.RichTextPlainText))
            {
                rtb.Document.Blocks.Clear();
                rtb.Document.Blocks.Add(new Paragraph(new Run(node.RichTextPlainText)));
            }

            container.Children.Add(rtb);

            var dragHandle = new Border
            {
                Height = 30,
                Background = new SolidColorBrush(Color.FromRgb(46, 134, 222)),
                VerticalAlignment = VerticalAlignment.Top,
                Tag = "DragHandle"
            };
            var dragHandleText = new TextBlock
            {
                Text = "📝 Hộp văn bản",
                Foreground = Brushes.White,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0)
            };
            dragHandle.Child = dragHandleText;
            container.Children.Add(dragHandle);

            return container;
        }

        private static RenderTargetBitmap? RenderLineChartFallback(LineChartConfiguration config, double width, double height)
        {
            try
            {
                int w = (int)Math.Max(300, width);
                int h = (int)Math.Max(200, height);

                var canvas = new Canvas
                {
                    Width = w,
                    Height = h,
                    Background = Brushes.White
                };

                var padding = new Thickness(60, 40, 40, 50);
                double chartWidth = w - padding.Left - padding.Right;
                double chartHeight = h - padding.Top - padding.Bottom;

                if (chartWidth <= 0 || chartHeight <= 0 || config.LineSeries == null || config.LineSeries.Count == 0)
                    return null;

                var allValues = config.LineSeries.SelectMany(s => s.Data ?? new List<double>()).ToList();
                if (allValues.Count == 0) return null;

                double minValue = Math.Min(0, allValues.Min());
                double maxValue = allValues.Max();
                if (maxValue <= minValue) maxValue = minValue + 100;
                double range = maxValue - minValue;
                double scale = chartHeight / (range * 1.15);

                // Grid lines
                if (config.ShowGrid)
                {
                    for (int i = 0; i <= 5; i++)
                    {
                        double y = padding.Top + (chartHeight / 5.0) * i;
                        canvas.Children.Add(new Line
                        {
                            X1 = padding.Left, Y1 = y,
                            X2 = padding.Left + chartWidth, Y2 = y,
                            Stroke = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                            StrokeThickness = 1,
                            StrokeDashArray = new DoubleCollection { 4, 4 }
                        });

                        double val = maxValue * 1.15 - (i * (maxValue * 1.15 / 5.0));
                        var valLabel = new TextBlock
                        {
                            Text = val.ToString("F0"),
                            FontSize = 10,
                            Foreground = new SolidColorBrush(Color.FromRgb(120, 120, 120))
                        };
                        Canvas.SetLeft(valLabel, padding.Left - 38);
                        Canvas.SetTop(valLabel, y - 7);
                        canvas.Children.Add(valLabel);
                    }
                }

                // Axes
                canvas.Children.Add(new Line
                {
                    X1 = padding.Left, Y1 = padding.Top + chartHeight,
                    X2 = padding.Left + chartWidth, Y2 = padding.Top + chartHeight,
                    Stroke = Brushes.Black, StrokeThickness = 1.5
                });
                canvas.Children.Add(new Line
                {
                    X1 = padding.Left, Y1 = padding.Top,
                    X2 = padding.Left, Y2 = padding.Top + chartHeight,
                    Stroke = Brushes.Black, StrokeThickness = 1.5
                });

                int pointCount = config.Labels?.Count ?? 0;
                if (pointCount == 0) pointCount = config.LineSeries.Max(s => s.Data?.Count ?? 0);
                if (pointCount < 2) pointCount = 2;

                double stepX = chartWidth / (pointCount - 1);

                // Category labels
                if (config.Labels != null)
                {
                    for (int i = 0; i < config.Labels.Count; i++)
                    {
                        double x = padding.Left + i * stepX;
                        var lbl = new TextBlock
                        {
                            Text = config.Labels[i],
                            FontSize = 11,
                            Foreground = new SolidColorBrush(Color.FromRgb(70, 70, 70)),
                            TextAlignment = TextAlignment.Center,
                            Width = Math.Max(40, stepX)
                        };
                        Canvas.SetLeft(lbl, x - (lbl.Width / 2));
                        Canvas.SetTop(lbl, padding.Top + chartHeight + 8);
                        canvas.Children.Add(lbl);
                    }
                }

                var defaultColors = new[]
                {
                    Color.FromRgb(37, 99, 235),  // Blue
                    Color.FromRgb(220, 38, 38),  // Red
                    Color.FromRgb(16, 185, 129), // Green
                    Color.FromRgb(245, 158, 11), // Orange
                    Color.FromRgb(147, 51, 234)  // Purple
                };

                for (int sIdx = 0; sIdx < config.LineSeries.Count; sIdx++)
                {
                    var series = config.LineSeries[sIdx];
                    if (series.Data == null || series.Data.Count == 0) continue;

                    Color c = series.Color.A > 0 ? series.Color : defaultColors[sIdx % defaultColors.Length];
                    var strokeBrush = new SolidColorBrush(c);
                    double lineThick = config.LineThickness > 0 ? config.LineThickness : 2.5;

                    var points = new PointCollection();
                    for (int i = 0; i < series.Data.Count; i++)
                    {
                        double x = padding.Left + i * stepX;
                        double y = padding.Top + chartHeight - ((series.Data[i] - minValue) * scale);
                        points.Add(new Point(x, y));

                        if (config.ShowPoints)
                        {
                            var dot = new Ellipse
                            {
                                Width = 8,
                                Height = 8,
                                Fill = Brushes.White,
                                Stroke = strokeBrush,
                                StrokeThickness = 2
                            };
                            Canvas.SetLeft(dot, x - 4);
                            Canvas.SetTop(dot, y - 4);
                            canvas.Children.Add(dot);
                        }
                    }

                    var polyline = new Polyline
                    {
                        Points = points,
                        Stroke = strokeBrush,
                        StrokeThickness = lineThick,
                        StrokeLineJoin = PenLineJoin.Round,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeEndLineCap = PenLineCap.Round
                    };
                    canvas.Children.Add(polyline);
                }

                canvas.Measure(new Size(w, h));
                canvas.Arrange(new Rect(0, 0, w, h));
                canvas.UpdateLayout();

                var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(canvas);
                return rtb;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SceneGraphSerializer] ⚠️ Lỗi render fallback line chart: {ex.Message}");
                return null;
            }
        }

        private static RenderTargetBitmap? RenderPieChartFallback(PieChartConfiguration config, double width, double height)
        {
            try
            {
                int w = (int)Math.Max(300, width);
                int h = (int)Math.Max(200, height);

                var canvas = new Canvas
                {
                    Width = w,
                    Height = h,
                    Background = Brushes.White
                };

                if (config.ChartData == null || config.ChartData.Count == 0) return null;

                double total = config.ChartData.Sum(d => d.Value);
                if (total <= 0) return null;

                double cx = w / 2.0;
                double cy = h / 2.0;
                double radius = Math.Min(w, h) * 0.38;

                var defaultColors = new[]
                {
                    Color.FromRgb(59, 130, 246),
                    Color.FromRgb(239, 68, 68),
                    Color.FromRgb(16, 185, 129),
                    Color.FromRgb(245, 158, 11),
                    Color.FromRgb(139, 92, 246),
                    Color.FromRgb(236, 72, 153)
                };

                double currentAngle = 0;
                for (int i = 0; i < config.ChartData.Count; i++)
                {
                    var item = config.ChartData[i];
                    double sliceAngle = (item.Value / total) * 360.0;
                    if (sliceAngle <= 0) continue;

                    Color c = item.Color.A > 0 ? item.Color : defaultColors[i % defaultColors.Length];

                    double startRad = (currentAngle - 90) * Math.PI / 180.0;
                    double endRad = (currentAngle + sliceAngle - 90) * Math.PI / 180.0;

                    Point pStart = new Point(cx + radius * Math.Cos(startRad), cy + radius * Math.Sin(startRad));
                    Point pEnd = new Point(cx + radius * Math.Cos(endRad), cy + radius * Math.Sin(endRad));

                    var pathFig = new PathFigure { StartPoint = new Point(cx, cy), IsClosed = true };
                    pathFig.Segments.Add(new LineSegment(pStart, true));
                    pathFig.Segments.Add(new ArcSegment(pEnd, new Size(radius, radius), 0, sliceAngle > 180, SweepDirection.Clockwise, true));

                    var pathGeo = new PathGeometry();
                    pathGeo.Figures.Add(pathFig);

                    var path = new Path
                    {
                        Data = pathGeo,
                        Fill = new SolidColorBrush(c),
                        Stroke = Brushes.White,
                        StrokeThickness = 1.5
                    };
                    canvas.Children.Add(path);

                    currentAngle += sliceAngle;
                }

                canvas.Measure(new Size(w, h));
                canvas.Arrange(new Rect(0, 0, w, h));
                canvas.UpdateLayout();

                var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(canvas);
                return rtb;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SceneGraphSerializer] ⚠️ Lỗi render fallback pie chart: {ex.Message}");
                return null;
            }
        }

        private static RenderTargetBitmap? RenderAreaChartFallback(AreaChartConfiguration config, double width, double height)
        {
            try
            {
                int w = (int)Math.Max(320, width);
                int h = (int)Math.Max(220, height);

                var canvas = new Canvas
                {
                    Width = w,
                    Height = h,
                    Background = Brushes.White
                };

                // Title
                if (!string.IsNullOrWhiteSpace(config.Title))
                {
                    var titleTb = new TextBlock
                    {
                        Text = config.Title,
                        FontSize = 13,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80)),
                        TextAlignment = TextAlignment.Center,
                        Width = w
                    };
                    Canvas.SetLeft(titleTb, 0);
                    Canvas.SetTop(titleTb, 8);
                    canvas.Children.Add(titleTb);
                }

                var padding = new Thickness(52, 34, 30, 48);
                double chartW = w - padding.Left - padding.Right;
                double chartH = h - padding.Top - padding.Bottom;

                if (chartW <= 0 || chartH <= 0 || config.Series == null || config.Series.Count == 0) return null;

                var allVals = config.Series.SelectMany(s => s.Data ?? new List<double>()).ToList();
                if (allVals.Count == 0) return null;

                double maxVal = Math.Max(10, allVals.Max());
                double scale = chartH / (maxVal * 1.15);

                int maxPoints = config.Series.Max(s => s.Data?.Count ?? 0);
                if (maxPoints < 2) maxPoints = 2;
                double stepX = chartW / (maxPoints - 1);

                // Y-axis grid & numbers
                for (int level = 0; level <= 4; level++)
                {
                    double val = (maxVal / 4.0) * level;
                    double y = padding.Top + chartH - (val * scale);
                    if (level > 0)
                    {
                        var gridLine = new Line
                        {
                            X1 = padding.Left, Y1 = y,
                            X2 = padding.Left + chartW, Y2 = y,
                            Stroke = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)),
                            StrokeDashArray = new DoubleCollection { 3, 3 },
                            StrokeThickness = 1
                        };
                        canvas.Children.Add(gridLine);
                    }

                    var lbl = new TextBlock
                    {
                        Text = val.ToString("F0"),
                        FontSize = 10,
                        Foreground = new SolidColorBrush(Color.FromRgb(127, 140, 141)),
                        TextAlignment = TextAlignment.Right,
                        Width = padding.Left - 8
                    };
                    Canvas.SetLeft(lbl, 0);
                    Canvas.SetTop(lbl, y - 7);
                    canvas.Children.Add(lbl);
                }

                // Axes lines
                canvas.Children.Add(new Line
                {
                    X1 = padding.Left, Y1 = padding.Top,
                    X2 = padding.Left, Y2 = padding.Top + chartH,
                    Stroke = new SolidColorBrush(Color.FromRgb(149, 165, 166)),
                    StrokeThickness = 1.5
                });
                canvas.Children.Add(new Line
                {
                    X1 = padding.Left, Y1 = padding.Top + chartH,
                    X2 = padding.Left + chartW, Y2 = padding.Top + chartH,
                    Stroke = new SolidColorBrush(Color.FromRgb(149, 165, 166)),
                    StrokeThickness = 1.5
                });

                // X-axis category labels
                var labels = !string.IsNullOrWhiteSpace(config.RawLabels)
                    ? config.RawLabels.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).ToList()
                    : new List<string>();

                for (int i = 0; i < maxPoints; i++)
                {
                    double x = padding.Left + i * stepX;
                    string lblText = i < labels.Count ? labels[i] : $"T{i + 1}";
                    var lbl = new TextBlock
                    {
                        Text = lblText,
                        FontSize = 10.5,
                        Foreground = new SolidColorBrush(Color.FromRgb(52, 73, 94)),
                        TextAlignment = TextAlignment.Center,
                        Width = Math.Max(40, stepX)
                    };
                    Canvas.SetLeft(lbl, x - (lbl.Width / 2));
                    Canvas.SetTop(lbl, padding.Top + chartH + 6);
                    canvas.Children.Add(lbl);
                }

                // Area Polygons
                foreach (var s in config.Series)
                {
                    if (s.Data == null || s.Data.Count == 0) continue;

                    var polyPoints = new PointCollection();
                    polyPoints.Add(new Point(padding.Left, padding.Top + chartH));

                    for (int i = 0; i < s.Data.Count; i++)
                    {
                        double x = padding.Left + i * stepX;
                        double y = padding.Top + chartH - (s.Data[i] * scale);
                        polyPoints.Add(new Point(x, y));
                    }
                    polyPoints.Add(new Point(padding.Left + (s.Data.Count - 1) * stepX, padding.Top + chartH));

                    var poly = new Polygon
                    {
                        Points = polyPoints,
                        Fill = new SolidColorBrush(Color.FromArgb(120, s.Color.R, s.Color.G, s.Color.B)),
                        Stroke = new SolidColorBrush(s.Color),
                        StrokeThickness = 2
                    };
                    canvas.Children.Add(poly);
                }

                // Legend at bottom
                if (config.Series.Count > 0)
                {
                    var legendPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    foreach (var s in config.Series)
                    {
                        var itemPanel = new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Margin = new Thickness(8, 0, 8, 0)
                        };
                        var swatch = new System.Windows.Shapes.Rectangle
                        {
                            Width = 10, Height = 10,
                            Fill = new SolidColorBrush(s.Color),
                            Margin = new Thickness(0, 0, 4, 0),
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        var sName = new TextBlock
                        {
                            Text = s.Name ?? "Series",
                            FontSize = 10,
                            Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80)),
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        itemPanel.Children.Add(swatch);
                        itemPanel.Children.Add(sName);
                        legendPanel.Children.Add(itemPanel);
                    }
                    legendPanel.Measure(new Size(w, 30));
                    legendPanel.Arrange(new Rect(0, 0, w, 30));
                    Canvas.SetLeft(legendPanel, Math.Max(0, (w - legendPanel.DesiredSize.Width) / 2));
                    Canvas.SetTop(legendPanel, h - 22);
                    canvas.Children.Add(legendPanel);
                }

                canvas.Measure(new Size(w, h));
                canvas.Arrange(new Rect(0, 0, w, h));
                canvas.UpdateLayout();

                var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(canvas);
                return rtb;
            }
            catch
            {
                return null;
            }
        }

        private static RenderTargetBitmap? RenderScatterChartFallback(ScatterChartConfiguration config, double width, double height)
        {
            try
            {
                int w = (int)Math.Max(300, width);
                int h = (int)Math.Max(200, height);

                var canvas = new Canvas
                {
                    Width = w,
                    Height = h,
                    Background = Brushes.White
                };

                var padding = new Thickness(50, 30, 30, 40);
                double chartW = w - padding.Left - padding.Right;
                double chartH = h - padding.Top - padding.Bottom;

                if (chartW <= 0 || chartH <= 0 || config.DataPoints == null || config.DataPoints.Count == 0) return null;

                double maxX = Math.Max(10, config.DataPoints.Max(p => p.X));
                double maxY = Math.Max(10, config.DataPoints.Max(p => p.Y));

                foreach (var pt in config.DataPoints)
                {
                    double x = padding.Left + (pt.X / maxX) * chartW;
                    double y = padding.Top + chartH - ((pt.Y / maxY) * chartH);

                    Color ptColor = Color.FromRgb(59, 130, 246);
                    if (config.Categories != null && config.Categories.TryGetValue(pt.Category, out var cat) && cat.Color.A > 0)
                    {
                        ptColor = cat.Color;
                    }

                    double pSize = pt.Size > 0 ? pt.Size : 8;
                    var dot = new Ellipse
                    {
                        Width = pSize,
                        Height = pSize,
                        Fill = new SolidColorBrush(ptColor),
                        Stroke = Brushes.White,
                        StrokeThickness = 1
                    };
                    Canvas.SetLeft(dot, x - (pSize / 2));
                    Canvas.SetTop(dot, y - (pSize / 2));
                    canvas.Children.Add(dot);
                }

                canvas.Measure(new Size(w, h));
                canvas.Arrange(new Rect(0, 0, w, h));
                canvas.UpdateLayout();

                var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(canvas);
                return rtb;
            }
            catch
            {
                return null;
            }
        }

        private static RenderTargetBitmap? RenderRadarChartFallback(RadarChartConfiguration config, double width, double height)
        {
            try
            {
                int w = (int)Math.Max(300, width);
                int h = (int)Math.Max(200, height);

                var canvas = new Canvas
                {
                    Width = w,
                    Height = h,
                    Background = Brushes.White
                };

                double cx = w / 2.0;
                double cy = h / 2.0;
                double radius = Math.Min(w, h) * 0.38;

                if (config.Axes == null || config.Axes.Count < 3) return null;
                int axisCount = config.Axes.Count;

                // Web grid
                for (int level = 1; level <= 4; level++)
                {
                    double r = (radius / 4.0) * level;
                    var webPoints = new PointCollection();
                    for (int i = 0; i < axisCount; i++)
                    {
                        double angle = (i * 360.0 / axisCount - 90) * Math.PI / 180.0;
                        webPoints.Add(new Point(cx + r * Math.Cos(angle), cy + r * Math.Sin(angle)));
                    }
                    canvas.Children.Add(new Polygon
                    {
                        Points = webPoints,
                        Stroke = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                        StrokeThickness = 1
                    });
                }

                // Datasets
                if (config.Datasets != null)
                {
                    foreach (var ds in config.Datasets)
                    {
                        if (ds.Data == null || ds.Data.Count == 0) continue;
                        var polyPoints = new PointCollection();
                        for (int i = 0; i < axisCount; i++)
                        {
                            double val = (i < ds.Data.Count) ? ds.Data[i] : 0;
                            double max = (i < config.Axes.Count && config.Axes[i].MaxValue > 0) ? config.Axes[i].MaxValue : 100;
                            double r = radius * Math.Min(1.0, val / max);
                            double angle = (i * 360.0 / axisCount - 90) * Math.PI / 180.0;
                            polyPoints.Add(new Point(cx + r * Math.Cos(angle), cy + r * Math.Sin(angle)));
                        }

                        canvas.Children.Add(new Polygon
                        {
                            Points = polyPoints,
                            Fill = new SolidColorBrush(Color.FromArgb(90, ds.Color.R, ds.Color.G, ds.Color.B)),
                            Stroke = new SolidColorBrush(ds.Color),
                            StrokeThickness = 2
                        });
                    }
                }

                canvas.Measure(new Size(w, h));
                canvas.Arrange(new Rect(0, 0, w, h));
                canvas.UpdateLayout();

                var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(canvas);
                return rtb;
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Color Utilities

        private static string? ColorToHex(Brush? brush)
        {
            if (brush == null) return null;

            if (brush is SolidColorBrush scb)
            {
                var c = scb.Color;
                if (c.A == 0) return null; // Transparent / rỗng ruột -> trả về null để không tạo màu giả
                return $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
            }
            return null; // Tuyệt đối KHÔNG hardcode #FFFFFFFF làm hỏng hình rỗng
        }

        private static Brush? ParseBrush(string? hex, Brush? fallback = null)
        {
            if (string.IsNullOrWhiteSpace(hex)) return fallback;

            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hex);
                if (color.A == 0) return fallback;
                return new SolidColorBrush(color);
            }
            catch
            {
                return fallback;
            }
        }

        private static DoubleCollection? ParseDashArray(string? dashStr)
        {
            if (string.IsNullOrWhiteSpace(dashStr)) return null;
            try
            {
                var dc = new DoubleCollection();
                var parts = dashStr.Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    if (double.TryParse(part, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double val))
                    {
                        dc.Add(val);
                    }
                }
                return dc.Count > 0 ? dc : null;
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}
