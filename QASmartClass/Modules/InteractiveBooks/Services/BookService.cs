using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using QASmartTouch.Modules.InteractiveBooks.Models;

namespace QASmartTouch.Modules.InteractiveBooks.Services
{
    /// <summary>
    /// Service for managing book data and operations
    /// </summary>
    public class BookService
    {
        private readonly string _dataFilePath;
        private List<Book> _allBooks;

        public BookService()
        {
            // Path to books.json in the application directory
            _dataFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                "Modules", "InteractiveBooks", "Data", "books.json");
            _allBooks = new List<Book>();
        }

        /// <summary>
        /// Load all books from JSON file
        /// </summary>
        public List<Book> GetAllBooks()
        {
            try
            {
                if (!File.Exists(_dataFilePath))
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Books file not found: {_dataFilePath}");
                    return GetDefaultBooks();
                }

                string jsonContent = File.ReadAllText(_dataFilePath);
                var bookData = JsonConvert.DeserializeObject<BookData>(jsonContent);

                if (bookData?.Books != null && bookData.Books.Count > 0)
                {
                    _allBooks = bookData.Books;
                    System.Diagnostics.Debug.WriteLine($"✅ Loaded {_allBooks.Count} books from JSON");
                    return _allBooks;
                }

                System.Diagnostics.Debug.WriteLine("⚠️ No books found in JSON, using defaults");
                return GetDefaultBooks();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error loading books: {ex.Message}");
                return GetDefaultBooks();
            }
        }

        /// <summary>
        /// Filter books by grade
        /// </summary>
        public List<Book> FilterByGrade(List<Book> books, int grade)
        {
            if (grade == 0) return books; // 0 means "All"
            return books.Where(b => b.Grade == grade).ToList();
        }

        /// <summary>
        /// Filter books by subject
        /// </summary>
        public List<Book> FilterBySubject(List<Book> books, string subject)
        {
            if (string.IsNullOrEmpty(subject) || subject == "Tất cả môn học")
                return books;

            return books.Where(b => b.Subject.Equals(subject, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        /// <summary>
        /// Search books by keyword
        /// </summary>
        public List<Book> SearchBooks(List<Book> books, string keyword)
        {
            if (books == null) return new List<Book>();
            if (string.IsNullOrWhiteSpace(keyword))
                return books;

            keyword = keyword.ToLower().Trim();
            return books.Where(b =>
                (b.Title ?? "").ToLower().Contains(keyword) ||
                (b.Subject ?? "").ToLower().Contains(keyword) ||
                (b.Publisher ?? "").ToLower().Contains(keyword) ||
                (b.Series ?? "").ToLower().Contains(keyword)
            ).ToList();
        }

        /// <summary>
        /// Get unique subjects from books
        /// </summary>
        public List<string> GetUniqueSubjects(List<Book> books)
        {
            return books.Select(b => b.Subject)
                       .Distinct()
                       .OrderBy(s => s)
                       .ToList();
        }

        /// <summary>
        /// Get default sample books if JSON loading fails
        /// </summary>
        private List<Book> GetDefaultBooks()
        {
            return new List<Book>
            {
                new Book
                {
                    Id = "default-1",
                    Title = "Toán 6",
                    Subject = "Toán",
                    GradeText = "Lớp 6",
                    Type = "Sách giáo khoa",
                    CoverImage = "cd-toan-6.jpg",
                    BaseUrl = "https://www.hoc10.vn/doc-sach/toan-6/1/350",
                    TotalPages = 256,
                    Series = "Cánh diều",
                    SeriesCode = "CD",
                    Publisher = "NXB Đại học Sư phạm",
                    Year = 2023
                },
                new Book
                {
                    Id = "default-2",
                    Title = "Ngữ văn 6 - Tập 1",
                    Subject = "Ngữ văn",
                    GradeText = "Lớp 6",
                    Type = "Sách giáo khoa",
                    CoverImage = "cd-ngu-van-6-1.jpg",
                    BaseUrl = "https://www.hoc10.vn/doc-sach/ngu-van-6-tap-1/1/351",
                    TotalPages = 198,
                    Series = "Cánh diều",
                    SeriesCode = "CD",
                    Publisher = "NXB Đại học Sư phạm",
                    Year = 2023
                }
            };
        }
    }
}
