using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using QASmartClass.Data;
using Serilog;

namespace QASmartClass.Classroom.Services
{
    /// <summary>
    /// Service tạo Word template bài giảng và import Word → LessonContent blocks.
    /// GV tải file mẫu → điền nội dung → import lại → tự động tạo bài giảng.
    /// </summary>
    public static class LessonWordService
    {
        // ═══════════════════════════════════════════════════════
        //  SECTION MARKERS — dùng để phân tách các phần bài giảng
        // ═══════════════════════════════════════════════════════

        private static readonly (string Marker, string Label, string ContentType)[] Sections = new[]
        {
            ("[MỤC TIÊU BÀI HỌC]",       "🎯 Mục tiêu bài học",           "Text"),
            ("[KIẾN THỨC TRỌNG TÂM]",     "📚 Kiến thức trọng tâm",       "Text"),
            ("[HOẠT ĐỘNG KHỞI ĐỘNG]",     "🚀 Hoạt động khởi động",       "Text"),
            ("[NỘI DUNG BÀI GIẢNG]",      "📖 Nội dung bài giảng",        "Text"),
            ("[VÍ DỤ MINH HỌA]",          "💡 Ví dụ minh họa",            "Text"),
            ("[THỰC HÀNH / BÀI TẬP]",     "✏️ Thực hành / Bài tập",       "Text"),
            ("[HOẠT ĐỘNG NHÓM]",          "👥 Hoạt động nhóm",            "Text"),
            ("[TỔNG KẾT BÀI HỌC]",       "📋 Tổng kết bài học",          "Text"),
            ("[BÀI TẬP VỀ NHÀ]",          "🏠 Bài tập về nhà",            "Text"),
            ("[GHI CHÚ GIÁO VIÊN]",       "📝 Ghi chú giáo viên",         "Text"),
        };

        // ═══════════════════════════════════════════════════════
        //  📄 GENERATE WORD TEMPLATE
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Tạo file Word mẫu bài giảng với cấu trúc sections rõ ràng.
        /// GV chỉ cần điền nội dung vào từng section.
        /// </summary>
        public static string GenerateTemplate(string subject, string grade, string title)
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "QASmartClass", "Templates");
            Directory.CreateDirectory(dir);

            var safeName = string.Join("_", $"BaiGiang_{subject}_{grade}_{DateTime.Now:yyyyMMdd_HHmmss}".Split(Path.GetInvalidFileNameChars()));
            var filePath = Path.Combine(dir, safeName + ".docx");

            using var doc = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document);
            var mainPart = doc.AddMainDocumentPart();
            var body = new Body();
            mainPart.Document = new Document(body);

            // ── Title ──
            AddParagraph(body, $"BÀI GIẢNG: {title}", 28, true, "1F3864");
            AddParagraph(body, $"Môn: {subject}  |  Lớp: {grade}  |  Ngày soạn: {DateTime.Now:dd/MM/yyyy}", 12, false, "757575");
            AddParagraph(body, "─────────────────────────────────────────────", 10, false, "CCCCCC");
            AddParagraph(body, "", 10, false, "000000"); // spacer

            // ── Hướng dẫn ──
            AddParagraph(body, "HƯỚNG DẪN SỬ DỤNG:", 14, true, "C62828");
            AddParagraph(body, "• Mỗi phần bài giảng bắt đầu bằng dấu [TÊN SECTION] — KHÔNG XÓA dòng này!", 11, false, "C62828");
            AddParagraph(body, "• Nhập nội dung bên dưới mỗi section (thay thế dòng \"(Nhập nội dung...)\")", 11, false, "C62828");
            AddParagraph(body, "• Các phần không cần dùng có thể để trống hoặc xóa nội dung bên dưới", 11, false, "C62828");
            AddParagraph(body, "• File này sẽ được import vào QA Smart Class → Soạn bài", 11, false, "C62828");
            AddParagraph(body, "", 10, false, "000000"); // spacer
            AddParagraph(body, "═══════════════════════════════════════════", 10, false, "1976D2");

            // ── Sections ──
            foreach (var (marker, label, _) in Sections)
            {
                AddParagraph(body, "", 8, false, "000000"); // spacer
                AddParagraph(body, marker, 16, true, "1565C0");
                AddParagraph(body, $"({GetPlaceholderText(marker)})", 11, false, "9E9E9E");
                AddParagraph(body, "", 11, false, "000000"); // blank line for content
            }

            // ── Footer ──
            AddParagraph(body, "", 10, false, "000000");
            AddParagraph(body, "═══════════════════════════════════════════", 10, false, "1976D2");
            AddParagraph(body, "📌 Lưu file này sau khi soạn xong, rồi import vào QA Smart Class.", 11, false, "757575");
            AddParagraph(body, $"Template được tạo bởi QA Smart Class v3.0 — {DateTime.Now:dd/MM/yyyy HH:mm}", 9, false, "BDBDBD");

            doc.Save();
            Log.Information("Word template created: {Path}", filePath);
            return filePath;
        }

        // ═══════════════════════════════════════════════════════
        //  📥 IMPORT WORD FILE → LESSON CONTENTS
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Parse Word file theo cấu trúc [SECTION MARKER] → trả về list LessonContent blocks.
        /// </summary>
        public static (string title, string subject, string grade, List<LessonContent> blocks) ImportFromWord(string filePath)
        {
            var blocks = new List<LessonContent>();
            string title = "", subject = "", grade = "";

            using var doc = WordprocessingDocument.Open(filePath, false);
            var body = doc?.MainDocumentPart?.Document?.Body;
            if (body == null)
            {
                Log.Warning("ImportFromWord: empty document body");
                return (title, subject, grade, blocks);
            }

            // Collect all paragraph texts
            var allLines = body.Elements<Paragraph>()
                .Select(p => p.InnerText?.Trim() ?? "")
                .ToList();

            // ── Parse title line ──
            var titleLine = allLines.FirstOrDefault(l => l.StartsWith("BÀI GIẢNG:", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(titleLine))
            {
                title = titleLine.Replace("BÀI GIẢNG:", "").Trim();
            }

            // ── Parse subject/grade from info line ──
            var infoLine = allLines.FirstOrDefault(l => l.Contains("Môn:") && l.Contains("Lớp:"));
            if (!string.IsNullOrEmpty(infoLine))
            {
                var parts = infoLine.Split('|');
                foreach (var part in parts)
                {
                    var trimmed = part.Trim();
                    if (trimmed.StartsWith("Môn:"))
                        subject = trimmed.Replace("Môn:", "").Trim();
                    else if (trimmed.StartsWith("Lớp:"))
                        grade = trimmed.Replace("Lớp:", "").Trim();
                }
            }

            // ── Parse sections by markers ──
            string? currentMarker = null;
            string currentContentType = "Text";
            var currentContent = new List<string>();
            int sortOrder = 0;

            foreach (var line in allLines)
            {
                // Check if this line is a section marker
                var matchedSection = Sections.FirstOrDefault(s =>
                    line.Equals(s.Marker, StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith(s.Marker, StringComparison.OrdinalIgnoreCase));

                if (matchedSection.Marker != null)
                {
                    // Save previous section
                    if (currentMarker != null)
                    {
                        var text = BuildSectionText(currentMarker, currentContent);
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            blocks.Add(new LessonContent
                            {
                                ContentType = currentContentType,
                                Data = text,
                                SortOrder = sortOrder++
                            });
                        }
                    }

                    currentMarker = matchedSection.Marker;
                    currentContentType = matchedSection.ContentType;
                    currentContent.Clear();
                }
                else if (currentMarker != null)
                {
                    // Skip placeholder hints and separators
                    if (line.StartsWith("(") && line.EndsWith(")")) continue;
                    if (line.StartsWith("───") || line.StartsWith("═══")) continue;
                    if (line.StartsWith("HƯỚNG DẪN") || line.StartsWith("• Mỗi phần") ||
                        line.StartsWith("• Nhập nội") || line.StartsWith("• Các phần") ||
                        line.StartsWith("• File này") || line.StartsWith("📌 Lưu file") ||
                        line.StartsWith("Template được")) continue;

                    currentContent.Add(line);
                }
            }

            // Save last section
            if (currentMarker != null)
            {
                var text = BuildSectionText(currentMarker, currentContent);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    blocks.Add(new LessonContent
                    {
                        ContentType = currentContentType,
                        Data = text,
                        SortOrder = sortOrder++
                    });
                }
            }

            // If no sections found, treat entire document as one Text block
            if (blocks.Count == 0)
            {
                var allText = string.Join("\n", allLines.Where(l =>
                    !string.IsNullOrWhiteSpace(l) &&
                    !l.StartsWith("───") && !l.StartsWith("═══") &&
                    !l.StartsWith("📌") && !l.StartsWith("Template")));

                if (!string.IsNullOrWhiteSpace(allText))
                {
                    blocks.Add(new LessonContent
                    {
                        ContentType = "Text",
                        Data = allText,
                        SortOrder = 0
                    });
                }
            }

            Log.Information("Imported Word: \"{Title}\" — {Count} blocks, Subject={Subject}, Grade={Grade}",
                title, blocks.Count, subject, grade);

            return (title, subject, grade, blocks);
        }

        // ═══════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════

        private static string BuildSectionText(string marker, List<string> lines)
        {
            // Find section label
            var section = Sections.FirstOrDefault(s => s.Marker == marker);
            var label = section.Label ?? marker;

            var contentLines = lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            if (contentLines.Count == 0) return "";

            return $"{label}\n{string.Join("\n", contentLines)}";
        }

        private static void AddParagraph(Body body, string text, int fontSize, bool bold, string colorHex)
        {
            var run = new Run();
            var rp = new RunProperties();
            rp.Append(new FontSize { Val = (fontSize * 2).ToString() });
            rp.Append(new Color { Val = colorHex });
            rp.Append(new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman", EastAsia = "Times New Roman" });
            if (bold) rp.Append(new Bold());
            run.Append(rp);
            run.Append(new Text(text) { Space = SpaceProcessingModeValues.Preserve });

            var para = new Paragraph(run);
            body.Append(para);
        }

        private static string GetPlaceholderText(string marker) => marker switch
        {
            "[MỤC TIÊU BÀI HỌC]" =>
                "Nhập mục tiêu kiến thức, kỹ năng, thái độ...\nVD:\n- Kiến thức: HS nắm được khái niệm...\n- Kỹ năng: HS vận dụng được...\n- Thái độ: HS có ý thức...",
            "[KIẾN THỨC TRỌNG TÂM]" =>
                "Nhập các khái niệm, công thức, định lý quan trọng...",
            "[HOẠT ĐỘNG KHỞI ĐỘNG]" =>
                "Mô tả hoạt động warm-up: câu hỏi gợi mở, trò chơi...",
            "[NỘI DUNG BÀI GIẢNG]" =>
                "Nội dung chi tiết bài giảng. Mỗi ý trên một dòng.\nDùng ký tự • hoặc - để tạo danh sách.",
            "[VÍ DỤ MINH HỌA]" =>
                "Các ví dụ cụ thể, bài toán mẫu, tình huống thực tế...",
            "[THỰC HÀNH / BÀI TẬP]" =>
                "Bài tập cho HS thực hành tại lớp...",
            "[HOẠT ĐỘNG NHÓM]" =>
                "Mô tả hoạt động nhóm: chia nhóm, nhiệm vụ, thời gian...",
            "[TỔNG KẾT BÀI HỌC]" =>
                "Tóm tắt nội dung chính, nhấn mạnh kiến thức trọng tâm...",
            "[BÀI TẬP VỀ NHÀ]" =>
                "Bài tập về nhà, yêu cầu chuẩn bị cho buổi sau...",
            "[GHI CHÚ GIÁO VIÊN]" =>
                "Ghi chú riêng cho GV: lưu ý khi dạy, phân bổ thời gian...",
            _ => "Nhập nội dung tại đây..."
        };
    }
}
