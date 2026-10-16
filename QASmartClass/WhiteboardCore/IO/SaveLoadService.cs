using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QASmartTouch.Managers;
using QASmartTouch.Helpers;
using QASmartClass.WhiteboardCore.IO.SceneGraph;
using Path = System.IO.Path;

namespace QASmartTouch.WhiteboardCore.IO
{
    /// <summary>
    /// Metadata thông tin bài giảng
    /// </summary>
    public class LectureManifest
    {
        public string Version { get; set; } = "2.0";
        public string Title { get; set; } = "Bài giảng QA SmartClass";
        public string Author { get; set; } = "Giáo viên";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastModifiedAt { get; set; } = DateTime.Now;
        public int ActiveBoardIndex { get; set; } = 0;
        public List<BoardManifestItem> Boards { get; set; } = new();
    }

    /// <summary>
    /// Metadata của từng trang bảng trong bài giảng
    /// </summary>
    public class BoardManifestItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? BackgroundColorHex { get; set; } = "#3D6D64";
        public string? BackgroundImagePath { get; set; }
        public string? BackgroundPattern { get; set; } = "grid";
        public int LineSpacing { get; set; } = 40;
        public int LineOpacity { get; set; } = 10;
        public int ObjectCount { get; set; }
        public double CanvasWidth { get; set; } = 1920;
        public double CanvasHeight { get; set; } = 1080;
        public string? SceneGraphFile { get; set; }
        public string XamlFile { get; set; } = string.Empty;
        public string? ThumbnailFile { get; set; }
    }

    /// <summary>
    /// SaveLoadService — Quản lý Đóng gói, Lưu và Mở bài giảng (.qasc)
    /// Hỗ trợ cả 2 chuẩn:
    /// - Chuẩn v2.0 (Mới): JSON Scene Graph lưu trữ 100% đối tượng (nét vẽ, ảnh, widget, video, 3D, chart)
    /// - Chuẩn v1.0 (Cũ): Fallback XAML Deserialization đảm bảo tương thích ngược 100%
    /// </summary>
    public static class SaveLoadService
    {
        private const string MANIFEST_NAME = "manifest.json";

        /// <summary>
        /// Lưu toàn bộ bài giảng hiện tại từ BoardManager vào tệp .qasc
        /// </summary>
        public static bool SaveLecture(BoardManager boardManager, string filePath, string? lectureTitle = null)
        {
            if (boardManager == null || string.IsNullOrWhiteSpace(filePath)) return false;

            try
            {
                // Bước 1: Đồng bộ trạng thái của trang đang mở vào BoardManager
                boardManager.SaveCurrentBoardState();

                string? dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                // Ghi vào file tạm trước để chống hỏng file nếu quá trình ghi bị ngắt quãng
                string tempFilePath = filePath + ".tmp";
                if (File.Exists(tempFilePath)) File.Delete(tempFilePath);

                using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create))
                {
                    var manifest = new LectureManifest
                    {
                        Version = "2.0",
                        Title = !string.IsNullOrWhiteSpace(lectureTitle) 
                            ? lectureTitle 
                            : Path.GetFileNameWithoutExtension(filePath),
                        Author = "Giáo viên",
                        CreatedAt = DateTime.Now,
                        LastModifiedAt = DateTime.Now,
                        ActiveBoardIndex = boardManager.CurrentBoardIndex
                    };

                    int boardIndex = 0;
                    int imgIndex = 0;
                    var writtenAssets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var board in boardManager.Boards)
                    {
                        string sceneGraphEntryPath = $"scene_graphs/board_{boardIndex}.json";
                        string xamlEntryPath = $"boards/board_{boardIndex}.xaml";
                        string thumbEntryPath = $"thumbnails/board_{boardIndex}.png";

                        var boardItem = new BoardManifestItem
                        {
                            Id = board.Id,
                            Name = board.Name,
                            CanvasWidth = board.CanvasWidth > 0 ? board.CanvasWidth : 1920,
                            CanvasHeight = board.CanvasHeight > 0 ? board.CanvasHeight : 1080,
                            BackgroundColorHex = board.BackgroundColorHex ?? "#3D6D64",
                            BackgroundImagePath = board.BackgroundImagePath,
                            BackgroundPattern = board.BackgroundPattern,
                            LineSpacing = board.LineSpacing > 0 ? board.LineSpacing : 40,
                            LineOpacity = board.LineOpacity > 0 ? board.LineOpacity : 10,
                            ObjectCount = board.CanvasElements?.Count ?? 0,
                            SceneGraphFile = sceneGraphEntryPath,
                            XamlFile = xamlEntryPath,
                            ThumbnailFile = thumbEntryPath
                        };

                        // 1. Serialize Canvas elements thành JSON Scene Graph (v2.0 cao cấp)
                        var sceneNodes = new List<SceneElementNode>();
                        if (board.CanvasElements != null)
                        {
                            foreach (var element in board.CanvasElements)
                            {
                                var node = SceneGraphSerializer.SerializeElement(element, archive, boardIndex, ref imgIndex, writtenAssets);
                                if (node != null)
                                {
                                    sceneNodes.Add(node);
                                }
                            }
                        }

                        var sceneEntry = archive.CreateEntry(sceneGraphEntryPath, CompressionLevel.Optimal);
                        using (var writer = new StreamWriter(sceneEntry.Open(), Encoding.UTF8))
                        {
                            string jsonContent = JsonSerializer.Serialize(sceneNodes, new JsonSerializerOptions
                            {
                                WriteIndented = false
                            });
                            writer.Write(jsonContent);
                        }

                        // 2. Ghi XAML fallback (cho các phiên bản cũ hoặc kiểm tra tương thích)
                        string xamlContent = SerializeBoardToXaml(board, archive, boardIndex, writtenAssets);
                        var xamlEntry = archive.CreateEntry(xamlEntryPath, CompressionLevel.Optimal);
                        using (var writer = new StreamWriter(xamlEntry.Open(), Encoding.UTF8))
                        {
                            writer.Write(xamlContent);
                        }

                        // 3. Lưu Thumbnail ảnh thu nhỏ
                        if (board.ThumbnailImage != null)
                        {
                            try
                            {
                                var thumbEntry = archive.CreateEntry(thumbEntryPath, CompressionLevel.Fastest);
                                using var thumbStream = thumbEntry.Open();
                                var encoder = new PngBitmapEncoder();
                                encoder.Frames.Add(BitmapFrame.Create(board.ThumbnailImage));
                                encoder.Save(thumbStream);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[SaveLoadService] Lỗi lưu thumbnail trang {boardIndex}: {ex.Message}");
                            }
                        }

                        manifest.Boards.Add(boardItem);
                        boardIndex++;
                    }

                    // 4. Ghi manifest.json
                    var manifestEntry = archive.CreateEntry(MANIFEST_NAME, CompressionLevel.Optimal);
                    using (var writer = new StreamWriter(manifestEntry.Open(), Encoding.UTF8))
                    {
                        string jsonString = JsonSerializer.Serialize(manifest, new JsonSerializerOptions
                        {
                            WriteIndented = true,
                            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                        });
                        writer.Write(jsonString);
                    }
                }

                // Hoàn tất an toàn: Chuyển file tạm sang file chính
                if (File.Exists(filePath)) File.Delete(filePath);
                File.Move(tempFilePath, filePath);

                System.Diagnostics.Debug.WriteLine($"[SaveLoadService] ✅ Đã lưu bài giảng thành công (.qasc v2.0): {filePath} ({boardManager.BoardCount} bảng)");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SaveLoadService] ❌ Lỗi SaveLecture: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Mở bài giảng từ tệp .qasc và nạp vào BoardManager.
        /// Tự động phát hiện chuẩn JSON Scene Graph hoặc Fallback XAML cũ.
        /// </summary>
        public static LectureManifest? LoadLecture(BoardManager boardManager, string filePath)
        {
            if (boardManager == null || !File.Exists(filePath)) return null;

            try
            {
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read);

                // 1. Đọc manifest.json
                var manifestEntry = archive.GetEntry(MANIFEST_NAME);
                if (manifestEntry == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[SaveLoadService] ❌ File không đúng định dạng .qasc (thiếu manifest.json): {filePath}");
                    return null;
                }

                LectureManifest? manifest;
                using (var reader = new StreamReader(manifestEntry.Open(), Encoding.UTF8))
                {
                    string json = reader.ReadToEnd();
                    manifest = JsonSerializer.Deserialize<LectureManifest>(json);
                }

                if (manifest == null || manifest.Boards == null || manifest.Boards.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[SaveLoadService] ❌ Manifest rỗng hoặc không có trang nào.");
                    return null;
                }

                // 2. Nạp trước tất cả assets (ảnh) vào bộ nhớ
                var assetCache = new Dictionary<string, BitmapImage>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in archive.Entries)
                {
                    string normName = entry.FullName.Replace('\\', '/');
                    if ((normName.StartsWith("assets/", StringComparison.OrdinalIgnoreCase) || 
                         entry.FullName.StartsWith("assets\\", StringComparison.OrdinalIgnoreCase)) && entry.Length > 0)
                    {
                        try
                        {
                            using var entryStream = entry.Open();
                            using var ms = new MemoryStream();
                            entryStream.CopyTo(ms);
                            ms.Position = 0;

                            var bi = new BitmapImage();
                            bi.BeginInit();
                            bi.StreamSource = ms;
                            bi.CacheOption = BitmapCacheOption.OnLoad;
                            bi.EndInit();
                            bi.Freeze();

                            assetCache[normName] = bi;
                            assetCache[entry.FullName] = bi;
                            string fileNameOnly = Path.GetFileName(normName);
                            if (!string.IsNullOrEmpty(fileNameOnly))
                            {
                                assetCache[fileNameOnly] = bi;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[SaveLoadService] Lỗi nạp asset {entry.FullName}: {ex.Message}");
                        }
                    }
                }

                // 3. Tái tạo danh sách BoardState
                var loadedBoards = new List<BoardState>();
                int boardIdx = 0;

                foreach (var item in manifest.Boards)
                {
                    var board = new BoardState
                    {
                        Id = item.Id != Guid.Empty ? item.Id : Guid.NewGuid(),
                        Name = !string.IsNullOrWhiteSpace(item.Name) ? item.Name : $"Bảng {boardIdx + 1}",
                        CanvasWidth = item.CanvasWidth > 0 ? item.CanvasWidth : 1920,
                        CanvasHeight = item.CanvasHeight > 0 ? item.CanvasHeight : 1080,
                        BackgroundColorHex = item.BackgroundColorHex ?? "#3D6D64",
                        BackgroundImagePath = item.BackgroundImagePath,
                        BackgroundPattern = item.BackgroundPattern,
                        LineSpacing = item.LineSpacing > 0 ? item.LineSpacing : 40,
                        LineOpacity = item.LineOpacity > 0 ? item.LineOpacity : 10,
                        ObjectCount = item.ObjectCount,
                        CreatedAt = manifest.CreatedAt,
                        LastModifiedAt = manifest.LastModifiedAt,
                        IsActive = false
                    };

                    // Nạp Thumbnail
                    if (!string.IsNullOrEmpty(item.ThumbnailFile))
                    {
                        var thumbEntry = archive.GetEntry(item.ThumbnailFile);
                        if (thumbEntry != null)
                        {
                            try
                            {
                                using var thumbStream = thumbEntry.Open();
                                using var ms = new MemoryStream();
                                thumbStream.CopyTo(ms);
                                ms.Position = 0;

                                var thumbBmp = new BitmapImage();
                                thumbBmp.BeginInit();
                                thumbBmp.StreamSource = ms;
                                thumbBmp.CacheOption = BitmapCacheOption.OnLoad;
                                thumbBmp.EndInit();
                                thumbBmp.Freeze();

                                board.ThumbnailImage = thumbBmp;
                            }
                            catch { }
                        }
                    }

                    // A. ƯU TIÊN NẠP TỪ JSON SCENE GRAPH (v2.0)
                    bool loadedFromSceneGraph = false;
                    string sceneGraphPath = !string.IsNullOrEmpty(item.SceneGraphFile)
                        ? item.SceneGraphFile
                        : $"scene_graphs/board_{boardIdx}.json";

                    var sceneEntry = archive.GetEntry(sceneGraphPath);
                    if (sceneEntry != null)
                    {
                        try
                        {
                            using var reader = new StreamReader(sceneEntry.Open(), Encoding.UTF8);
                            string jsonContent = reader.ReadToEnd();
                            var nodes = JsonSerializer.Deserialize<List<SceneElementNode>>(jsonContent);
                            if (nodes != null)
                            {
                                var elements = new List<UIElement>();
                                foreach (var node in nodes)
                                {
                                    var elem = SceneGraphSerializer.DeserializeElement(node, assetCache);
                                    if (elem != null) elements.Add(elem);
                                }
                                board.CanvasElements = elements;
                                board.ObjectCount = elements.Count;
                                loadedFromSceneGraph = true;
                                System.Diagnostics.Debug.WriteLine($"[SaveLoadService] ✅ Đã nạp trang '{board.Name}' từ JSON Scene Graph ({elements.Count} elements)");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[SaveLoadService] ⚠️ Lỗi parse Scene Graph trang {item.Name}: {ex.Message}. Fallback XAML.");
                        }
                    }

                    // B. FALLBACK NẠP TỪ XAML CŨ (v1.0) NẾU CHƯA CÓ SCENE GRAPH
                    if (!loadedFromSceneGraph && !string.IsNullOrEmpty(item.XamlFile))
                    {
                        var xamlEntry = archive.GetEntry(item.XamlFile);
                        if (xamlEntry != null)
                        {
                            using var reader = new StreamReader(xamlEntry.Open(), Encoding.UTF8);
                            string xamlContent = reader.ReadToEnd();
                            board.CanvasElements = DeserializeBoardFromXaml(xamlContent, assetCache);
                            board.ObjectCount = board.CanvasElements.Count;
                            System.Diagnostics.Debug.WriteLine($"[SaveLoadService] 🔄 Đã nạp trang '{board.Name}' từ Legacy XAML ({board.ObjectCount} elements)");
                        }
                    }

                    loadedBoards.Add(board);
                    boardIdx++;
                }

                // 4. Nạp các bảng vào BoardManager
                boardManager.LoadBoards(loadedBoards, manifest.ActiveBoardIndex);
                System.Diagnostics.Debug.WriteLine($"[SaveLoadService] ✅ Đã mở bài giảng thành công: '{manifest.Title}' ({loadedBoards.Count} trang)");

                return manifest;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SaveLoadService] ❌ Lỗi LoadLecture: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        #region Serializer Helpers (Legacy Fallback)

        private static string SerializeBoardToXaml(
            BoardState board, 
            ZipArchive archive, 
            int boardIndex, 
            HashSet<string>? writtenAssets = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<Canvas xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">");

            if (board.CanvasElements != null)
            {
                int imgIndex = 0;
                foreach (var element in board.CanvasElements)
                {
                    if (element == null) continue;

                    try
                    {
                        // Xử lý riêng cho đối tượng Image để nhúng vào thư mục assets/ của gói ZIP
                        if (element is Image img && img.Source is BitmapSource bs)
                        {
                            string assetFileName = $"assets/board_{boardIndex}_img_{imgIndex}.png";
                            if (writtenAssets == null || !writtenAssets.Contains(assetFileName))
                            {
                                var assetEntry = archive.CreateEntry(assetFileName, CompressionLevel.Fastest);
                                using var assetStream = assetEntry.Open();
                                var encoder = new PngBitmapEncoder();
                                encoder.Frames.Add(BitmapFrame.Create(bs));
                                encoder.Save(assetStream);
                                writtenAssets?.Add(assetFileName);
                            }

                            double left = Canvas.GetLeft(img);
                            double top = Canvas.GetTop(img);
                            double w = img.Width > 0 ? img.Width : img.ActualWidth;
                            double h = img.Height > 0 ? img.Height : img.ActualHeight;

                            sb.AppendLine($"  <Image Canvas.Left=\"{left}\" Canvas.Top=\"{top}\" Width=\"{w}\" Height=\"{h}\" Stretch=\"{img.Stretch}\" Tag=\"asset:{assetFileName}\" />");
                            imgIndex++;
                            continue;
                        }

                        // Xử lý riêng cho biểu đồ tương tác để lưu hình ảnh hiển thị trong fallback XAML
                        if (element is FrameworkElement feChart && InteractiveChartHelper.IsChartContainer(feChart))
                        {
                            var chartImg = InteractiveChartHelper.GetChartImage(feChart);
                            if (chartImg?.Source is BitmapSource cbs)
                            {
                                string assetFileName = $"assets/board_{boardIndex}_chart_{imgIndex}.png";
                                if (writtenAssets == null || !writtenAssets.Contains(assetFileName))
                                {
                                    var assetEntry = archive.CreateEntry(assetFileName, CompressionLevel.Fastest);
                                    using var assetStream = assetEntry.Open();
                                    var encoder = new PngBitmapEncoder();
                                    encoder.Frames.Add(BitmapFrame.Create(cbs));
                                    encoder.Save(assetStream);
                                    writtenAssets?.Add(assetFileName);
                                }

                                double left = Canvas.GetLeft(feChart);
                                double top = Canvas.GetTop(feChart);
                                double w = feChart.Width > 0 ? feChart.Width : feChart.ActualWidth;
                                double h = feChart.Height > 0 ? feChart.Height : feChart.ActualHeight;

                                sb.AppendLine($"  <Image Canvas.Left=\"{left}\" Canvas.Top=\"{top}\" Width=\"{w}\" Height=\"{h}\" Stretch=\"Uniform\" Tag=\"asset:{assetFileName}\" />");
                                imgIndex++;
                                continue;
                            }
                        }

                        // Các đối tượng hình học và văn bản chuẩn: Polyline, Polygon, Path, Rectangle, Ellipse, Line, TextBlock
                        string elementXaml = XamlWriter.Save(element);
                        sb.AppendLine($"  {elementXaml}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SaveLoadService] Bỏ qua phần tử không tương thích XamlWriter: {ex.Message}");
                    }
                }
            }

            sb.AppendLine("</Canvas>");
            return sb.ToString();
        }

        private static List<UIElement> DeserializeBoardFromXaml(string xamlContent, Dictionary<string, BitmapImage> assetCache)
        {
            var result = new List<UIElement>();
            if (string.IsNullOrWhiteSpace(xamlContent)) return result;

            try
            {
                if (XamlReader.Parse(xamlContent) is Canvas container)
                {
                    // Lấy các phần tử ra khỏi container để có thể add vào MainInteractiveBoard
                    while (container.Children.Count > 0)
                    {
                        var child = container.Children[0];
                        container.Children.RemoveAt(0);

                        // Khôi phục hình ảnh từ assets nếu có Tag "asset:..."
                        if (child is Image img && img.Tag is string tag && tag.StartsWith("asset:", StringComparison.OrdinalIgnoreCase))
                        {
                            string assetKey = tag.Substring("asset:".Length).Trim();
                            if (assetCache.TryGetValue(assetKey, out var cachedBmp))
                            {
                                img.Source = cachedBmp;
                            }
                        }

                        result.Add(child);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SaveLoadService] Lỗi parse XAML: {ex.Message}");
            }

            return result;
        }

        #endregion
    }
}
