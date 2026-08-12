using QASmartClass.Data;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QASmartClass.Services
{
    public class SkknService
    {
        private readonly AppDbContext _db;

        public SkknService(AppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public List<Skkn> GetSkknsByAuthor(string authorName)
        {
            try
            {
                return _db.Skkns.Where(s => s.AuthorName == authorName).OrderByDescending(s => s.SubmittedAt).ToList();
            }
            catch { return new List<Skkn>(); }
        }

        public List<Skkn> GetSkknsByAuthorCode(string teacherCode)
        {
            try
            {
                return _db.Skkns.Where(s => s.TeacherCode == teacherCode).OrderByDescending(s => s.SubmittedAt).ToList();
            }
            catch { return new List<Skkn>(); }
        }

        public List<Skkn> GetAllSkkns()
        {
            try
            {
                return _db.Skkns.OrderByDescending(s => s.SubmittedAt).ToList();
            }
            catch { return new List<Skkn>(); }
        }

        public bool SubmitSkkn(string authorName, string title, string subject, string level, string filePath, string teacherCode = "")
        {
            try
            {
                _db.Skkns.Add(new Skkn
                {
                    AuthorName = authorName,
                    Title = title,
                    Subject = subject,
                    Level = level,
                    FilePath = filePath,
                    Status = "Submitted",
                    SubmittedAt = DateTime.Now,
                    TeacherCode = teacherCode
                });
                _db.SaveChanges();
                Log.Information("SkknService: SKKN {Title} submitted by {Author} ({Code})", title, authorName, teacherCode);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "SkknService: Submit failed");
                return false;
            }
        }

        public bool ReviewSkkn(int skknId, string status, string note)
        {
            try
            {
                var skkn = _db.Skkns.Find(skknId);
                if (skkn != null)
                {
                    skkn.Status = status;
                    skkn.ReviewNote = note;
                    _db.SaveChanges();
                    Log.Information("SkknService: SKKN {Id} reviewed as {Status}", skknId, status);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "SkknService: Review failed");
                return false;
            }
        }
    }
}

