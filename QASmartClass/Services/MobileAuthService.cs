using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using QASmartClass.Data;

namespace QASmartClass.Services
{
    public class MobileAuthService
    {
        private readonly AppDbContext _db;

        public MobileAuthService(AppDbContext dbContext)
        {
            _db = dbContext;
        }

        public string AuthenticateParent(string studentCode, string parentPhone)
        {
            var student = _db.Students.FirstOrDefault(s => s.StudentCode == studentCode);
            if (student != null && student.ParentPhone == parentPhone)
            {
                // Gi? l?p t?o Token truy c?p JWT
                return GenerateMockJwtToken(student.Id, "Parent");
            }
            return null;
        }

        public string AuthenticateStudent(string studentCode)
        {
            var student = _db.Students.FirstOrDefault(s => s.StudentCode == studentCode);
            if (student != null)
            {
                // Gi? l?p t?o Token truy c?p JWT
                return GenerateMockJwtToken(student.Id, "Student");
            }
            return null;
        }

        private string GenerateMockJwtToken(int userId, string role)
        {
            var header = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"alg\":\"HS256\",\"typ\":\"JWT\"}"));
            var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"sub\":\"{userId}\",\"role\":\"{role}\",\"exp\":{DateTimeOffset.UtcNow.AddDays(30).ToUnixTimeSeconds()}}}"));
            var signature = "mock_signature_" + Guid.NewGuid().ToString().Substring(0, 8);
            
            return $"{header}.{payload}.{signature}";
        }
    }
}

