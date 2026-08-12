using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using Serilog;

namespace QASmartClass.Services
{
    /// <summary>
    /// Shared QuestionBank Service — Tập trung logic CRUD ngân hàng câu hỏi.
    /// Được sử dụng chung bởi SmartClass (QuestionBankPage), TeacherHub (QuestionBankView),
    /// StudentClient (GameHub), và AiCopilot.
    /// Nguyên tắc: Giữ UI riêng mỗi sub-brand, chỉ gộp Service layer.
    /// </summary>
    public class QuestionBankService
    {
        // -------------------------------------------------------
        //  QUERY
        // -------------------------------------------------------

        /// <summary>Lấy tất cả câu hỏi, có thể lọc theo môn/lớp/trạng thái</summary>
        public static async Task<List<QuestionBankItem>> GetQuestionsAsync(
            string? subject = null, string? grade = null, string? approvalStatus = null)
        {
            try
            {
                using var db = new AppDbContext();
                var query = db.QuestionBankItems.AsQueryable();

                if (!string.IsNullOrEmpty(subject))
                    query = query.Where(q => q.Subject == subject);
                if (!string.IsNullOrEmpty(grade))
                    query = query.Where(q => q.Grade == grade);
                if (!string.IsNullOrEmpty(approvalStatus))
                    query = query.Where(q => q.ApprovalStatus == approvalStatus);

                return await query.OrderByDescending(q => q.CreatedAt).ToListAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[QuestionBankService] GetQuestions failed");
                return new List<QuestionBankItem>();
            }
        }

        /// <summary>Lấy danh sách môn học có câu hỏi</summary>
        public static async Task<List<string>> GetSubjectsAsync()
        {
            try
            {
                using var db = new AppDbContext();
                return await db.QuestionBankItems
                    .Select(q => q.Subject)
                    .Distinct()
                    .OrderBy(s => s)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[QuestionBankService] GetSubjects failed");
                return new List<string>();
            }
        }

        /// <summary>Lấy câu hỏi ngẫu nhiên cho Quiz/Game (đã duyệt)</summary>
        public static async Task<List<QuestionBankItem>> GetRandomApprovedAsync(int count = 10, string? subject = null)
        {
            try
            {
                using var db = new AppDbContext();
                var query = db.QuestionBankItems.Where(q => q.ApprovalStatus == "Approved");
                if (!string.IsNullOrEmpty(subject))
                    query = query.Where(q => q.Subject == subject);

                return await query
                    .OrderBy(_ => Guid.NewGuid())
                    .Take(count)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[QuestionBankService] GetRandomApproved failed");
                return new List<QuestionBankItem>();
            }
        }

        /// <summary>Tìm câu hỏi theo ID</summary>
        public static async Task<QuestionBankItem?> FindByIdAsync(int id)
        {
            try
            {
                using var db = new AppDbContext();
                return await db.QuestionBankItems.FindAsync(id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[QuestionBankService] FindById failed: {Id}", id);
                return null;
            }
        }

        // -------------------------------------------------------
        //  CREATE
        // -------------------------------------------------------

        /// <summary>Thêm 1 câu hỏi mới</summary>
        public static async Task<bool> AddAsync(QuestionBankItem item)
        {
            try
            {
                using var db = new AppDbContext();
                item.CreatedAt = DateTime.Now;
                db.QuestionBankItems.Add(item);
                await db.SaveChangesAsync();
                Log.Information("[QuestionBankService] Added question: {Subject} - {Type}", item.Subject, item.QuestionType);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[QuestionBankService] Add failed");
                return false;
            }
        }

        /// <summary>Thêm nhiều câu hỏi (Import / AI Generate)</summary>
        public static async Task<int> AddRangeAsync(IEnumerable<QuestionBankItem> items)
        {
            try
            {
                using var db = new AppDbContext();
                var list = items.ToList();
                foreach (var item in list)
                    item.CreatedAt = DateTime.Now;
                db.QuestionBankItems.AddRange(list);
                await db.SaveChangesAsync();
                Log.Information("[QuestionBankService] Bulk added {Count} questions", list.Count);
                return list.Count;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[QuestionBankService] AddRange failed");
                return 0;
            }
        }

        // -------------------------------------------------------
        //  UPDATE
        // -------------------------------------------------------

        /// <summary>Duyệt câu hỏi (cho Tổ trưởng)</summary>
        public static async Task<bool> ApproveAsync(int id, string approver)
        {
            try
            {
                using var db = new AppDbContext();
                var item = await db.QuestionBankItems.FindAsync(id);
                if (item == null) return false;
                item.ApprovalStatus = "Approved";
                await db.SaveChangesAsync();
                Log.Information("[QuestionBankService] Approved #{Id} by {Approver}", id, approver);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[QuestionBankService] Approve failed: {Id}", id);
                return false;
            }
        }

        /// <summary>Từ chối câu hỏi</summary>
        public static async Task<bool> RejectAsync(int id, string reason)
        {
            try
            {
                using var db = new AppDbContext();
                var item = await db.QuestionBankItems.FindAsync(id);
                if (item == null) return false;
                item.ApprovalStatus = "Rejected";
                await db.SaveChangesAsync();
                Log.Information("[QuestionBankService] Rejected #{Id}: {Reason}", id, reason);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[QuestionBankService] Reject failed: {Id}", id);
                return false;
            }
        }

        // -------------------------------------------------------
        //  DELETE
        // -------------------------------------------------------

        /// <summary>Xóa câu hỏi theo ID</summary>
        public static async Task<bool> DeleteAsync(int id)
        {
            try
            {
                using var db = new AppDbContext();
                var item = await db.QuestionBankItems.FindAsync(id);
                if (item == null) return false;
                db.QuestionBankItems.Remove(item);
                await db.SaveChangesAsync();
                Log.Information("[QuestionBankService] Deleted #{Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[QuestionBankService] Delete failed: {Id}", id);
                return false;
            }
        }

        /// <summary>Xóa nhiều câu hỏi</summary>
        public static async Task<int> DeleteRangeAsync(IEnumerable<int> ids)
        {
            try
            {
                using var db = new AppDbContext();
                var items = await db.QuestionBankItems.Where(q => ids.Contains(q.Id)).ToListAsync();
                db.QuestionBankItems.RemoveRange(items);
                await db.SaveChangesAsync();
                Log.Information("[QuestionBankService] Bulk deleted {Count} questions", items.Count);
                return items.Count;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[QuestionBankService] DeleteRange failed");
                return 0;
            }
        }

        // -------------------------------------------------------
        //  STATISTICS
        // -------------------------------------------------------

        /// <summary>Thống kê ngân hàng câu hỏi</summary>
        public static async Task<QuestionBankStats> GetStatsAsync()
        {
            try
            {
                using var db = new AppDbContext();
                return new QuestionBankStats
                {
                    Total = await db.QuestionBankItems.CountAsync(),
                    Approved = await db.QuestionBankItems.CountAsync(q => q.ApprovalStatus == "Approved"),
                    Pending = await db.QuestionBankItems.CountAsync(q => q.ApprovalStatus == "Pending"),
                    Rejected = await db.QuestionBankItems.CountAsync(q => q.ApprovalStatus == "Rejected"),
                    SubjectCount = await db.QuestionBankItems.Select(q => q.Subject).Distinct().CountAsync()
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[QuestionBankService] GetStats failed");
                return new QuestionBankStats();
            }
        }
    }

    /// <summary>Thống kê ngân hàng câu hỏi</summary>
    public class QuestionBankStats
    {
        public int Total { get; set; }
        public int Approved { get; set; }
        public int Pending { get; set; }
        public int Rejected { get; set; }
        public int SubjectCount { get; set; }
    }
}

