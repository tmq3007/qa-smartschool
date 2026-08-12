﻿using System;
using System.Linq;
using System.Threading.Tasks;
using QASmartClass.Data;

namespace QASmartClass.Services
{
    public class MIAnalysisService
    {
        private readonly AppDbContext _db;
        private static readonly object _syncLock = new object();

        public MIAnalysisService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<string> SubmitGameResultAsync(int studentId, string gameName, string targetMI, int xpGained, int durationSeconds, bool isCorrect)
        {
            return await Task.Run(() =>
            {
                lock (_syncLock)
                {
                    try
                    {
                        // TASK 1.1: Anti-Spam Logic
                        if (!isCorrect)
                        {
                            xpGained = 0; // Trả lời sai không được cộng điểm
                        }

                        var profile = _db.StudentMIProfiles.FirstOrDefault(p => p.StudentId == studentId);
                        if (profile == null)
                        {
                            profile = new StudentMIProfile { StudentId = studentId };
                            _db.StudentMIProfiles.Add(profile);
                        }

                        // TASK 1.1: Cộng dồn Raw XP vô cực (Không dùng Math.Min)
                        switch (targetMI.ToLower())
                        {
                            case "linguistic": profile.LinguisticScore += xpGained; break;
                            case "logical": profile.LogicalScore += xpGained; break;
                            case "spatial": profile.SpatialScore += xpGained; break;
                            case "musical": profile.MusicalScore += xpGained; break;
                            case "kinesthetic": profile.KinestheticScore += xpGained; break;
                            case "interpersonal": profile.InterpersonalScore += xpGained; break;
                            case "intrapersonal": profile.IntrapersonalScore += xpGained; break;
                            case "naturalistic": profile.NaturalisticScore += xpGained; break;
                        }

                        profile.LastUpdated = DateTime.Now;

                        // TASK 1.2: AI Correlation Engine
                        string feedback = GenerateAiCoachFeedback(profile);

                        var record = new MiniGameRecord
                        {
                            StudentId = studentId,
                            GameName = gameName,
                            TargetMI = targetMI,
                            Score = xpGained,
                            DurationSeconds = durationSeconds,
                            AiFeedback = feedback,
                            PlayedAt = DateTime.Now
                        };
                        _db.MiniGameRecords.Add(record);
                        _db.SaveChanges();

                        return feedback;
                    }
                    catch (Exception ex)
                    {
                        Serilog.Log.Error(ex, "Lỗi khi lưu điểm MI Gamification.");
                        throw;
                    }
                }
            });
        }

        private string GenerateAiCoachFeedback(StudentMIProfile profile)
        {
            var scores = new System.Collections.Generic.Dictionary<string, double>
            {
                { "Ngôn ngữ", profile.LinguisticScore },
                { "Toán học / Logic", profile.LogicalScore },
                { "Không gian", profile.SpatialScore },
                { "Âm nhạc", profile.MusicalScore },
                { "Vận động", profile.KinestheticScore },
                { "Giao tiếp xã hội", profile.InterpersonalScore },
                { "Nội tâm", profile.IntrapersonalScore },
                { "Tự nhiên", profile.NaturalisticScore }
            };

            double totalRaw = scores.Sum(x => x.Value);
            if (totalRaw == 0) return "🤖 AI Coach: Chào mừng bạn! Hãy chơi thử các mini-game để tôi có thể phân tích năng lực của bạn nhé.";

            var highest = scores.OrderByDescending(x => x.Value).First();
            var lowest = scores.OrderBy(x => x.Value).First();

            double maxPercent = (highest.Value / totalRaw) * 100;
            double minPercent = (lowest.Value / totalRaw) * 100;

            if (maxPercent - minPercent > 40.0)
            {
                return $"⚠️ AI Cảnh báo: Bạn đang học lệch! Năng lực '{highest.Key}' chiếm tới {maxPercent:0.1}%, trong khi '{lowest.Key}' bị bỏ bê ({minPercent:0.1}%). Hãy cân bằng lại!";
            }
            else
            {
                return $"📈 AI Phân tích: Năng lực '{highest.Key}' của bạn rất tốt ({maxPercent:0.1}%). Độ lệch chuẩn an toàn, bạn đang phát triển toàn diện!";
            }
        }
    }
}
