using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.Windows.Shapes.Path;
using QASmartTouch.Forms;
using QASmartTouch.Helpers;
using QASmartTouch.Shared;

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
                    StrokeColorHex = ColorToHex(polyline.Stroke)
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

            // 2. HÌNH ẢNH (Image)
            if (element is Image img && img.Source is BitmapSource bs)
            {
                string assetFileName = $"assets/board_{boardIndex}_img_{imgIndex}.png";
                if (writtenAssets == null || !writtenAssets.Contains(assetFileName))
                {
                    try
                    {
                        var assetEntry = archive.CreateEntry(assetFileName, CompressionLevel.Fastest);
                        using var assetStream = assetEntry.Open();
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(bs));
                        encoder.Save(assetStream);
                        writtenAssets?.Add(assetFileName);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SceneGraphSerializer] Lỗi ghi asset ảnh: {ex.Message}");
                    }
                }

                imgIndex++;

                return new SceneElementNode
                {
                    ElementType = SceneElementType.Image,
                    Left = left,
                    Top = top,
                    Width = width > 0 ? width : bs.PixelWidth,
                    Height = height > 0 ? height : bs.PixelHeight,
                    ZIndex = zIndex,
                    Tag = tagStr,
                    AssetKey = assetFileName,
                    StretchMode = img.Stretch.ToString()
                };
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
                    Tag = tagStr
                };

                if (tagStr.Contains("YouTube"))
                {
                    webNode.EmbedKind = "YouTube";
                    // Tìm kiếm Title hoặc Tag chi tiết nếu có
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
                    StrokeDashArray = rect.StrokeDashArray != null ? string.Join(",", rect.StrokeDashArray) : null
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
                    StrokeDashArray = ellipse.StrokeDashArray != null ? string.Join(",", ellipse.StrokeDashArray) : null
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
                    StrokeDashArray = line.StrokeDashArray != null ? string.Join(",", line.StrokeDashArray) : null
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
                    StrokeDashArray = polygon.StrokeDashArray != null ? string.Join(",", polygon.StrokeDashArray) : null
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
                    GeometryData = path.Data?.ToString(),
                    FillColorHex = ColorToHex(path.Fill),
                    StrokeColorHex = ColorToHex(path.Stroke),
                    StrokeThickness = path.StrokeThickness,
                    StrokeDashArray = path.StrokeDashArray != null ? string.Join(",", path.StrokeDashArray) : null
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
                    ForegroundColorHex = ColorToHex(tb.Foreground)
                };
            }

            // 10. KHỐI HÌNH 3D (Tag == "3DShape") HOẶC CONTAINER LỒNG NHAU (Canvas con chứa các hình con)
            if (element is Canvas subCanvas)
            {
                var containerNode = new SceneElementNode
                {
                    ElementType = (tagStr == "3DShape") ? SceneElementType.Shape3D : SceneElementType.Container,
                    Left = left,
                    Top = top,
                    Width = width,
                    Height = height,
                    ZIndex = zIndex,
                    Tag = tagStr,
                    Model3DType = "Cube",
                    Children = new List<SceneElementNode>()
                };

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

            // 12. BIỂU ĐỒ TƯƠNG TÁC (Interactive Chart Container)
            if (element is FrameworkElement feChart && InteractiveChartHelper.IsChartContainer(feChart))
            {
                return SerializeChart(feChart as Grid, archive, boardIndex, ref imgIndex, left, top, width, height, zIndex, tagStr, writtenAssets);
            }

            // 13. FALLBACK: Sử dụng XAML Serialization an toàn nếu không match các loại trên
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
                    FallbackXaml = xaml
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
            HashSet<string>? writtenAssets = null)
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
                try
                {
                    var assetEntry = archive.CreateEntry(assetFileName, CompressionLevel.Fastest);
                    using var assetStream = assetEntry.Open();
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bs));
                    encoder.Save(assetStream);
                    writtenAssets?.Add(assetFileName);
                    assetSaved = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SceneGraphSerializer] Lỗi ghi asset chart image: {ex.Message}");
                }
            }
            else if (img != null)
            {
                try
                {
                    var capturedBmp = CaptureElementBitmap(img);
                    if (capturedBmp != null)
                    {
                        var assetEntry = archive.CreateEntry(assetFileName, CompressionLevel.Fastest);
                        using var assetStream = assetEntry.Open();
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(capturedBmp));
                        encoder.Save(assetStream);
                        writtenAssets?.Add(assetFileName);
                        assetSaved = true;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SceneGraphSerializer] Lỗi capture chart element: {ex.Message}");
                }
            }

            if (assetSaved)
            {
                imgIndex++;
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
                ChartConfigTypeName = configTypeName
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
                    if (!string.IsNullOrEmpty(node.GeometryData))
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
                    break;
            }

            return null;
        }

        private static UIElement RecreateImage(SceneElementNode node, Dictionary<string, BitmapImage> assetCache)
        {
            var img = new Image
            {
                Width = node.Width > 0 ? node.Width : double.NaN,
                Height = node.Height > 0 ? node.Height : double.NaN,
                Tag = node.Tag ?? $"asset:{node.AssetKey}"
            };

            if (Enum.TryParse<Stretch>(node.StretchMode, true, out var stretch))
            {
                img.Stretch = stretch;
            }

            if (!string.IsNullOrEmpty(node.AssetKey) && assetCache.TryGetValue(node.AssetKey, out var cachedBmp))
            {
                img.Source = cachedBmp;
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

            // 1. Phục hồi từ danh sách children vector thực tế
            if (node.Children != null && node.Children.Count > 0)
            {
                foreach (var childNode in node.Children)
                {
                    var childElement = DeserializeElement(childNode, assetCache);
                    if (childElement != null)
                    {
                        container.Children.Add(childElement);
                    }
                }
                return container;
            }

            // 2. Fallback cho bài giảng cũ từng bị lưu dạng stub (thiếu children):
            // Tái tạo lại hình khối 3D thật (chuẩn SGK nét liền & nét đứt) thay vì hiển thị khung placeholder xanh
            BuildFallback3DGeometry(container, node.Model3DType ?? "Cube", container.Width, container.Height, Brushes.White, 2.0);
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
            if (chartBmp == null && config is BarChartConfiguration barConfig)
            {
                chartBmp = RenderBarChartFallback(barConfig, initialW, initialH);
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
