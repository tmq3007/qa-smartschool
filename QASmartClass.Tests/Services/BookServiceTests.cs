using System;
using System.Collections.Generic;
using Xunit;
using QASmartTouch.Modules.InteractiveBooks.Models;
using QASmartTouch.Modules.InteractiveBooks.Services;

namespace QASmartClass.Tests.Services
{
    public class BookServiceTests
    {
        [Fact]
        public void SearchBooks_NullSafety_Check()
        {
            var service = new BookService();
            var books = new List<Book>
            {
                new Book
                {
                    Id = "test-1",
                    Title = "Toán 6",
                    Subject = "Toán",
                    Publisher = null, // Null to test safety
                    Series = null // Null to test safety
                }
            };

            // This should not crash and return empty/matching books properly
            var results = service.SearchBooks(books, "Toán");
            Assert.Single(results);

            var emptyResults = service.SearchBooks(books, "Cánh Diều");
            Assert.Empty(emptyResults);
        }

        [Fact]
        public void SearchBooks_CaseInsensitive_And_KeywordMatching()
        {
            var service = new BookService();
            var books = new List<Book>
            {
                new Book
                {
                    Id = "test-1",
                    Title = "Toán 6 Tập 1",
                    Subject = "Toán học",
                    Publisher = "NXB Giáo Dục",
                    Series = "Cánh diều"
                }
            };

            // Test Title search
            var resultsByTitle = service.SearchBooks(books, "tập 1");
            Assert.Single(resultsByTitle);

            // Test Publisher search
            var resultsByPub = service.SearchBooks(books, "giáo dục");
            Assert.Single(resultsByPub);

            // Test Series search
            var resultsBySeries = service.SearchBooks(books, "cánh diều");
            Assert.Single(resultsBySeries);
        }
    }
}
