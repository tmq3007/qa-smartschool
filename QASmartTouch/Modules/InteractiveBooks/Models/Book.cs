using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace QASmartTouch.Modules.InteractiveBooks.Models
{
    /// <summary>
    /// Model representing a textbook in the Interactive Books system
    /// </summary>
    public class Book
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("subject")]
        public string Subject { get; set; }

        [JsonProperty("grade")]
        public string GradeText { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("coverImage")]
        public string CoverImage { get; set; }

        [JsonProperty("baseUrl")]
        public string BaseUrl { get; set; }

        [JsonProperty("totalPages")]
        public int TotalPages { get; set; }

        [JsonProperty("series")]
        public string Series { get; set; }

        [JsonProperty("seriesCode")]
        public string SeriesCode { get; set; }

        [JsonProperty("publisher")]
        public string Publisher { get; set; }

        [JsonProperty("year")]
        public int Year { get; set; }

        // Computed properties for UI - with backing fields to avoid binding errors
        private string _name;
        public string Name
        {
            get => _name ?? Title;
            set => _name = value;
        }
        
        private int _grade;
        public int Grade
        {
            get
            {
                if (_grade > 0) return _grade;
                
                // Extract number from "Lớp X"
                if (string.IsNullOrEmpty(GradeText)) return 0;
                var parts = GradeText.Split(' ');
                if (parts.Length > 1 && int.TryParse(parts[1], out int grade))
                {
                    _grade = grade;
                    return grade;
                }
                return 0;
            }
            set => _grade = value;
        }

        private string _bookType;
        public string BookType
        {
            get
            {
                if (!string.IsNullOrEmpty(_bookType)) return _bookType;
                if (string.IsNullOrEmpty(Type)) return "SGK";
                
                if (Type.Contains("Sách giáo khoa", StringComparison.OrdinalIgnoreCase)) return "SGK";
                if (Type.Contains("Sách giáo viên", StringComparison.OrdinalIgnoreCase)) return "SGV";
                if (Type.Contains("Vở bài tập", StringComparison.OrdinalIgnoreCase)) return "VBT";
                if (Type.Contains("Sách bài tập", StringComparison.OrdinalIgnoreCase)) return "SBT";
                if (Type.Contains("Chuyên đề", StringComparison.OrdinalIgnoreCase)) return "CĐ";
                if (Type.Contains("Sách ôn thi", StringComparison.OrdinalIgnoreCase)) return "OT";
                if (Type.Contains("Vở thực hành", StringComparison.OrdinalIgnoreCase)) return "VTH";
                if (Type.Contains("Sách thực hành", StringComparison.OrdinalIgnoreCase)) return "STH";
                return "SGK";
            }
            set => _bookType = value;
        }

        private string _icon;
        public string Icon
        {
            get
            {
                if (!string.IsNullOrEmpty(_icon)) return _icon;
                if (string.IsNullOrEmpty(Subject)) return "📖";
                
                // Icon based on subject
                return Subject switch
                {
                    "Toán" or "Toán học" => "📐",
                    "Ngữ văn" or "Tiếng Việt" => "📚",
                    "Tiếng Anh" => "🔤",
                    "Khoa học" or "Khoa học tự nhiên" => "🔬",
                    "Lịch sử" => "📜",
                    "Địa lý" or "Lịch sử và Địa lý" => "🗺️",
                    "Vật lý" or "Vật lí" => "⚡",
                    "Hóa học" => "🧪",
                    "Sinh học" => "🧬",
                    "Tin học" => "💻",
                    "Công nghệ" => "⚙️",
                    "GDCD" or "Giáo dục công dân" => "⚖️",
                    "Âm nhạc" => "🎵",
                    "Mĩ thuật" or "Mỹ thuật" => "🎨",
                    "Thể dục" or "GDTC" => "⚽",
                    "Đạo đức" => "💝",
                    "TNXH" or "Tự nhiên và xã hội" => "🌍",
                    "HDTN" or "Hoạt động trải nghiệm" => "🎯",
                    _ => "📖"
                };
            }
            set => _icon = value;
        }

        private string _url;
        public string Url
        {
            get => _url ?? BaseUrl;
            set => _url = value;
        }
        
        private int _pageCount;
        public int PageCount
        {
            get => _pageCount > 0 ? _pageCount : TotalPages;
            set => _pageCount = value;
        }

        private string _description;
        public string Description
        {
            get => _description ?? $"{Title ?? string.Empty} - {Series ?? string.Empty}";
            set => _description = value;
        }

        private List<string> _tags;
        public List<string> Tags
        {
            get => _tags ?? new List<string> { Subject ?? string.Empty, GradeText ?? string.Empty, Series ?? string.Empty };
            set => _tags = value;
        }

        private string _coverImagePath;
        public string CoverImagePath
        {
            get
            {
                if (!string.IsNullOrEmpty(_coverImagePath)) return _coverImagePath;
                if (string.IsNullOrEmpty(CoverImage)) return null;
                
                var basePath = AppDomain.CurrentDomain.BaseDirectory;
                var imagePath = System.IO.Path.Combine(basePath, "Modules", "InteractiveBooks", "Data", "images", CoverImage);
                
                if (System.IO.File.Exists(imagePath))
                {
                    _coverImagePath = imagePath;
                    return imagePath;
                }
                
                // Fallback to placeholder
                return null;
            }
            set => _coverImagePath = value;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public Book()
        {
            Id = string.Empty;
            Title = string.Empty;
            Subject = string.Empty;
            GradeText = "Lớp 1";
            Type = "Sách giáo khoa";
            CoverImage = "book_placeholder.png";
            BaseUrl = string.Empty;
            TotalPages = 100;
            Series = string.Empty;
            SeriesCode = string.Empty;
            Publisher = string.Empty;
            Year = DateTime.Now.Year;
        }
    }

    /// <summary>
    /// Container for book data from JSON
    /// </summary>
    public class BookData
    {
        [JsonProperty("books")]
        public List<Book> Books { get; set; }

        public BookData()
        {
            Books = new List<Book>();
        }
    }
}
