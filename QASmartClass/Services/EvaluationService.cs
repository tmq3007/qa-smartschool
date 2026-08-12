using QASmartClass.Data;
using Serilog;
using System;
using System.Linq;

namespace QASmartClass.Services
{
    /// <summary>
    /// Service qu?n lư dánh giá 360 d? (Phase 7)
    /// </summary>
    public class EvaluationService
    {
        private readonly AppDbContext _db;

        public EvaluationService(AppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public bool SaveEvaluation(EvaluationRecord record)
        {
            if (record == null || string.IsNullOrWhiteSpace(record.TargetId))
            {
                Log.Warning("EvaluationService: TargetId r?ng.");
                return false;
            }

            try
            {
                record.EvaluatedAt = DateTime.Now;
                _db.EvaluationRecords.Add(record);
                _db.SaveChanges();
                Log.Information("Đã luu dánh giá t? {Evaluator} cho {Target}", record.EvaluatorId, record.TargetId);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi SaveEvaluation.");
                return false;
            }
        }

        public double GetAverageRating(string targetId)
        {
            try
            {
                var evals = _db.EvaluationRecords.Where(e => e.TargetId == targetId).ToList();
                if (evals.Count == 0) return 0;
                return Math.Round(evals.Average(e => e.Rating), 1);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi GetAverageRating.");
                return 0;
            }
        }
    }
}

