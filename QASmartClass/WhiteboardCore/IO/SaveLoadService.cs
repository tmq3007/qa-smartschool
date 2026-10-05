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
using Path = System.IO.Path;

namespace QASmartTouch.WhiteboardCore.IO
{
    /// <summary>
    /// Metadata thông tin bài giảng
    /// </summary>
    public class LectureManifest
    {
        public string Version { get; set; } = "1.0";
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
        public string? BackgroundPattern { get; set; } = "grid";
        public int LineSpacing { get; set; } = 40;
        public int LineOpacity { get; set; } = 10;
        public int ObjectCount { get; set; }
        public double CanvasWidth { get; set; } = 1920;
        public double CanvasHeight { get; set; } = 1080;
        public string XamlFile { get; set; } = string.Empty;
        public string? ThumbnailFile { get; set; }
    }

    /// <summary>
    /// SaveLoadService — Quản lý Đóng gói, Lưu và Mở bài giảng (.qasc)
    /// Cấu trúc chuẩn ZIP Package chứa manifest.json, boards/*.xaml, thumbnails/*.png, assets/*
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
                        Version = "1.0",
                        Title = !string.IsNullOrWhiteSpace(lectureTitle) 
                            ? lectureTitle 
                            : Path.GetFileNameWithoutExtension(filePath),
                        Author = "Giáo viên",
                        CreatedAt = DateTime.Now,
                        LastModifiedAt = DateTime.Now,
                        ActiveBoardIndex = boardManager.CurrentBoardIndex
                    };

                    int boardIndex = 0;
                    foreach (var board in boardManager.Boards)
                    {
                        string xamlEntryPath = $"boards/board_{boardIndex}.xaml";
                        string thumbEntryPath = $"thumbnails/board_{boardIndex}.png";

                        var boardItem = new BoardManifestItem
                        {
                            Id = board.Id,
                            Name = board.Name,
                            CanvasWidth = board.CanvasWidth > 0 ? board.CanvasWidth : 1920,
                            CanvasHeight = board.CanvasHeight > 0 ? board.CanvasHeight : 1080,
                            BackgroundColorHex = board.BackgroundColorHex ?? "#3D6D64",
                            BackgroundPattern = board.BackgroundPattern,
                            LineSpacing = board.LineSpacing > 0 ? board.LineSpacing : 40,
                            LineOpacity = board.LineOpacity > 0 ? board.LineOpacity : 10,
                            ObjectCount = board.CanvasElements?.Count ?? 0,
                            XamlFile = xamlEntryPath,
                            ThumbnailFile = thumbEntryPath
                        };

                        // 1. Serialize Canvas elements thành XAML
                        string xamlContent = SerializeBoardToXaml(board, archive, boardIndex);
                        var xamlEntry = archive.CreateEntry(xamlEntryPath, CompressionLevel.Optimal);
                        using (var writer = new StreamWriter(xamlEntry.Open(), Encoding.UTF8))
                        {
                            writer.Write(xamlContent);
                        }

                        // 2. Lưu Thumbnail ảnh thu nhỏ
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

                    // 3. Ghi manifest.json
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

                System.Diagnostics.Debug.WriteLine($"[SaveLoadService] ✅ Đã lưu bài giảng thành công: {filePath} ({boardManager.BoardCount} bảng)");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SaveLoadService] ❌ Lỗi SaveLecture: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Mở bài giảng từ tệp .qasc và nạp vào BoardManager
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
                    if (entry.FullName.StartsWith("assets/", StringComparison.OrdinalIgnoreCase) && entry.Length > 0)
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

                            assetCache[entry.FullName] = bi;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[SaveLoadService] Lỗi nạp asset {entry.FullName}: {ex.Message}");
                        }
                    }
                }

                // 3. Tái tạo danh sách BoardState
                var loadedBoards = new List<BoardState>();
                foreach (var item in manifest.Boards)
                {
                    var board = new BoardState
                    {
                        Id = item.Id != Guid.Empty ? item.Id : Guid.NewGuid(),
                        Name = !string.IsNullOrWhiteSpace(item.Name) ? item.Name : "Bảng",
                        CanvasWidth = item.CanvasWidth > 0 ? item.CanvasWidth : 1920,
                        CanvasHeight = item.CanvasHeight > 0 ? item.CanvasHeight : 1080,
                        BackgroundColorHex = item.BackgroundColorHex ?? "#3D6D64",
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

                    // Nạp XAML Canvas Elements
                    if (!string.IsNullOrEmpty(item.XamlFile))
                    {
                        var xamlEntry = archive.GetEntry(item.XamlFile);
                        if (xamlEntry != null)
                        {
                            using var reader = new StreamReader(xamlEntry.Open(), Encoding.UTF8);
                            string xamlContent = reader.ReadToEnd();
                            board.CanvasElements = DeserializeBoardFromXaml(xamlContent, assetCache);
                            board.ObjectCount = board.CanvasElements.Count;
                        }
                    }

                    loadedBoards.Add(board);
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

        #region Serializer Helpers

        private static string SerializeBoardToXaml(BoardState board, ZipArchive archive, int boardIndex)
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
                            var assetEntry = archive.CreateEntry(assetFileName, CompressionLevel.Fastest);
                            using (var assetStream = assetEntry.Open())
                            {
                                var encoder = new PngBitmapEncoder();
                                encoder.Frames.Add(BitmapFrame.Create(bs));
                                encoder.Save(assetStream);
                            }

                            double left = Canvas.GetLeft(img);
                            double top = Canvas.GetTop(img);
                            double w = img.Width > 0 ? img.Width : img.ActualWidth;
                            double h = img.Height > 0 ? img.Height : img.ActualHeight;

                            sb.AppendLine($"  <Image Canvas.Left=\"{left}\" Canvas.Top=\"{top}\" Width=\"{w}\" Height=\"{h}\" Stretch=\"{img.Stretch}\" Tag=\"asset:{assetFileName}\" />");
                            imgIndex++;
                            continue;
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
