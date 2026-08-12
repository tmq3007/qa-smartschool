using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.YouthUnion;

namespace QASmartClass.YouthUnion.Services
{
    public class YouthUnionService : IDisposable
    {
        private readonly AppDbContext _db;
        
        public YouthUnionService()
        {
            _db = new AppDbContext();
        }

        public YouthUnionService(AppDbContext db)
        {
            _db = db;
        }

        // --- Youth Members ---
        public async Task<List<YouthMember>> GetAllMembersAsync()
        {
            return await _db.YouthMembers.OrderBy(m => m.ClassName).ThenBy(m => m.StudentName).ToListAsync();
        }

        public async Task<YouthMember?> GetMemberByIdAsync(int id)
        {
            return await _db.YouthMembers.FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<List<YouthMember>> GetActiveMembersAsync()
        {
            return await _db.YouthMembers.Where(m => m.Status == YouthConstants.YouthStatus.Active)
                .OrderBy(m => m.ClassName).ThenBy(m => m.StudentName).ToListAsync();
        }

        public async Task AddMemberAsync(YouthMember m)
        {
            _db.YouthMembers.Add(m);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateMemberAsync(YouthMember m)
        {
            _db.YouthMembers.Update(m);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteMemberAsync(int id)
        {
            var member = await _db.YouthMembers.FindAsync(id);
            if (member != null)
            {
                // Note: Related records should be deleted here if cascade delete is not set in EF
                var fees = _db.YouthFees.Where(f => f.MemberId == id);
                _db.YouthFees.RemoveRange(fees);
                
                var scores = _db.YouthEmulationScores.Where(s => s.MemberId == id);
                _db.YouthEmulationScores.RemoveRange(scores);
                
                var regs = _db.YouthEventRegistrations.Where(r => r.MemberId == id);
                _db.YouthEventRegistrations.RemoveRange(regs);
                
                var attends = _db.YouthAttendances.Where(a => a.MemberId == id);
                _db.YouthAttendances.RemoveRange(attends);
                
                var proposals = _db.YouthAwardProposals.Where(p => p.MemberId == id);
                _db.YouthAwardProposals.RemoveRange(proposals);

                _db.YouthMembers.Remove(member);
                await _db.SaveChangesAsync();
            }
        }

        // --- Generic CRUD ---
        public async Task<List<T>> GetAllAsync<T>() where T : class
        {
            return await _db.Set<T>().ToListAsync();
        }

        public async Task<T?> GetByIdAsync<T>(int id) where T : class
        {
            return await _db.Set<T>().FindAsync(id);
        }

        public async Task AddAsync<T>(T entity) where T : class
        {
            _db.Set<T>().Add(entity);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync<T>(T entity) where T : class
        {
            _db.Set<T>().Update(entity);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync<T>(int id) where T : class
        {
            var entity = await _db.Set<T>().FindAsync(id);
            if (entity != null)
            {
                _db.Set<T>().Remove(entity);
                await _db.SaveChangesAsync();
            }
        }

        // --- Activities & Events ---
        public async Task<List<YouthActivity>> GetActivitiesAsync()
        {
            return await _db.YouthActivities.OrderByDescending(a => a.Date).ToListAsync();
        }

        // --- Notifications ---
        public async Task SendBulkNotificationAsync(int activityId, string message, string senderName)
        {
            var registrations = await _db.YouthEventRegistrations.Where(r => r.ActivityId == activityId).ToListAsync();
            var memberIds = registrations.Select(r => r.MemberId).ToList();
            var members = await _db.YouthMembers.Where(m => memberIds.Contains(m.Id)).ToListAsync();

            var messages = members.Select(m => new InboxMessage
            {
                SenderId = "BCH",
                SenderName = senderName,
                ReceiverId = m.StudentId.ToString(),
                Content = message,
                IsRead = false,
                CreatedAt = DateTime.Now
            }).ToList();

            if (messages.Any())
            {
                await _db.InboxMessages.AddRangeAsync(messages);
                await _db.SaveChangesAsync();
            }
        }

        // --- Emulation Scores ---
        public async Task AddScoreAsync(int memberId, int score, string reason, string category, string awardedBy, int? activityId = null)
        {
            var member = await _db.YouthMembers.FindAsync(memberId);
            if (member == null) throw new Exception("Không tìm thấy Đoàn viên.");

            var record = new YouthEmulationScore
            {
                MemberId = memberId,
                Score = score,
                Reason = reason,
                Category = category,
                AwardedDate = DateTime.Now,
                AwardedBy = awardedBy,
                ActivityId = activityId
            };
            
            _db.YouthEmulationScores.Add(record);
            
            // Auto update member's total score
            member.TotalScore += score;
            
            await _db.SaveChangesAsync();
        }

        // --- Fees ---
        public async Task<(int Paid, int Unpaid, decimal TotalAmount)> GetFeeStatsAsync(string period)
        {
            var activeMembers = await _db.YouthMembers.CountAsync(m => m.Status == YouthConstants.YouthStatus.Active);
            
            var paidRecords = await _db.YouthFees
                .Where(f => f.Period == period && f.Status == YouthConstants.FeeStatus.Paid)
                .ToListAsync();
                
            int paidCount = paidRecords.Select(f => f.MemberId).Distinct().Count();
            int unpaidCount = activeMembers - paidCount;
            if (unpaidCount < 0) unpaidCount = 0;
            
            decimal totalAmount = paidRecords.Sum(f => f.Amount);
            
            return (paidCount, unpaidCount, totalAmount);
        }

        // --- Secure & Anonymous Voting ---
        public async Task<bool> CastVoteAsync(int votingId, int voterId, int candidateId, string encryptionMode)
        {
            var session = await _db.YouthVotings.FirstOrDefaultAsync(v => v.Id == votingId);
            if (session == null || session.Status == "Closed") return false;

            // Anti double voting check
            var hasVoted = await _db.YouthVoterRegistries.AnyAsync(r => r.VotingId == votingId && r.VoterId == voterId);
            if (hasVoted) return false;

            // 1. Record in Registry (to prevent double voting)
            var registry = new YouthVoterRegistry
            {
                VotingId = votingId,
                VoterId = voterId,
                VotedAt = DateTime.Now
            };
            _db.YouthVoterRegistries.Add(registry);

            // 2. Save anonymous vote (VoterId is decoupled/set to 0)
            var rand = new Random();
            var randomOffset = rand.Next(-300, 301); // Lệch ngẫu nhiên từ -5 đến +5 phút (tính bằng giây)
            var vote = new YouthVote
            {
                VotingId = votingId,
                VoterId = 0, // Set to 0 to ensure anonymity!
                CandidateId = candidateId,
                VotedAt = DateTime.Now.AddSeconds(randomOffset)
            };
            _db.YouthVotes.Add(vote);

            await _db.SaveChangesAsync();
            return true;
        }

        // --- Budget with Balance Checks ---
        public async Task AddBudgetTransactionAsync(YouthBudget budget)
        {
            if (budget.Type == "Expense")
            {
                var budgets = await _db.YouthBudgets.ToListAsync();
                var totalIncome = budgets.Where(b => b.Type == "Income").Sum(b => b.Amount);
                var totalExpense = budgets.Where(b => b.Type == "Expense").Sum(b => b.Amount);
                var currentBalance = totalIncome - totalExpense;
                if (currentBalance < budget.Amount)
                {
                    throw new InvalidOperationException("Số dư quỹ không đủ để thực hiện giao dịch chi tiêu này.");
                }
            }
            _db.YouthBudgets.Add(budget);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateBudgetTransactionAsync(YouthBudget budget)
        {
            if (budget.Type == "Expense")
            {
                var budgets = await _db.YouthBudgets.ToListAsync();
                var totalIncome = budgets.Where(b => b.Type == "Income").Sum(b => b.Amount);
                var totalExpense = budgets.Where(b => b.Type == "Expense" && b.Id != budget.Id).Sum(b => b.Amount);
                var currentBalance = totalIncome - totalExpense;
                if (currentBalance < budget.Amount)
                {
                    throw new InvalidOperationException("Số dư quỹ không đủ để thực hiện giao dịch chi tiêu này.");
                }
            }
            _db.YouthBudgets.Update(budget);
            await _db.SaveChangesAsync();
        }

        public async Task SyncApprovedRecruitmentsAsync()
        {
            var recruitments = await _db.YouthRecruitments.Where(r => r.Status == "Approved").ToListAsync();
            var members = await GetAllMembersAsync();
            foreach (var r in recruitments)
            {
                var exists = members.Any(m => m.StudentId == r.StudentId || (m.StudentName == r.StudentName && m.ClassName == r.ClassName));
                if (!exists)
                {
                    var newMember = new YouthMember
                    {
                        StudentId = r.StudentId ?? 0,
                        StudentName = r.StudentName,
                        ClassName = r.ClassName,
                        MemberType = YouthMapper.MapMemberTypeToDb("Đoàn viên"),
                        JoinDate = DateTime.Today,
                        Position = YouthMapper.MapPositionToDb("Thành viên"),
                        Status = "Active",
                        DoanCardNo = $"DOAN-{DateTime.Today.Year}-{r.Id:D4}",
                        TotalScore = 0
                    };
                    _db.YouthMembers.Add(newMember);
                }
            }
            await _db.SaveChangesAsync();
        }

        public void Dispose()
        {
            _db?.Dispose();
        }
    }
}

