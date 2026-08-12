using QASmartClass.Data;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace QASmartClass.Services
{
    public class AnonymousService
    {
        private readonly AppDbContext _db;
        private const string SecretKey = "SmartClass_SecretKey_HMAC";

        public AnonymousService(AppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public bool SendQuestion(string anonymousToken, string category, string question)
        {
            try
            {
                string[] toxicKeywords = { "tự tử", "tự sát", "muốn chết", "hủy hoại bản thân", "cắt tay", "uống thuốc ngủ", "bạo hành" };
                bool isUrgent = toxicKeywords.Any(kw => question.ToLowerInvariant().Contains(kw));

                if (isUrgent)
                {
                    category = "[KHẨN CẤP] " + category;
                    try
                    {
                        var notifService = new NotificationService(_db);
                        notifService.PushNotification("counselor", new NotificationMessage
                        {
                            Title = "🚨 CẢNH BÁO KHẨN CẤP TÂM LÝ HỌC ĐƯỜNG",
                            Content = $"Phát hiện câu hỏi ẩn danh có dấu hiệu khủng hoảng cực đoan thuộc mục {category}.",
                            Type = "Urgent"
                        });
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Lỗi gửi thông báo khẩn cấp tâm lý.");
                    }
                }

                _db.AnonymousQueries.Add(new AnonymousQuery
                {
                    EncryptedStudentId = anonymousToken,
                    Category = category,
                    Question = question,
                    Status = "Pending",
                    CreatedAt = DateTime.Now
                });
                _db.SaveChanges();
                Log.Information("AnonymousService: Question sent from {Hash}", anonymousToken);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "AnonymousService: SendQuestion failed");
                return false;
            }
        }

        public List<AnonymousQuery> GetMyQuestions(string anonymousToken)
        {
            try
            {
                return _db.AnonymousQueries.Where(q => q.EncryptedStudentId == anonymousToken).OrderByDescending(q => q.CreatedAt).ToList();
            }
            catch { return new List<AnonymousQuery>(); }
        }

        public List<AnonymousQuery> GetAllPendingQuestions()
        {
            try
            {
                return _db.AnonymousQueries.Where(q => q.Status == "Pending").OrderBy(q => q.CreatedAt).ToList();
            }
            catch { return new List<AnonymousQuery>(); }
        }

        public bool AnswerQuestion(int questionId, string answer)
        {
            try
            {
                var q = _db.AnonymousQueries.Find(questionId);
                if (q != null)
                {
                    q.Answer = answer;
                    q.Status = "Answered";
                    _db.SaveChanges();
                    Log.Information("AnonymousService: Question {Id} answered", questionId);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "AnonymousService: AnswerQuestion failed");
                return false;
            }
        }
    }
}

