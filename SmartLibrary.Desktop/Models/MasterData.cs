namespace SmartLibrary.Desktop.Models
{
    public class PublisherDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
    }

    public class BookTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class DepartmentDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    /// <summary>
    /// Kết quả kiểm tra ràng buộc FK từ API (Item 1.5)
    /// </summary>
    public class DependencyCheckResult
    {
        public bool HasDependencies { get; set; }
        public int DependentCount { get; set; }
        public string DependentType { get; set; } = ""; // "Books", "Loans", etc.
    }

    public class LibraryConfigDto
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
