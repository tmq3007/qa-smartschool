using QASmartClass.Data;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QASmartClass.Services
{
    public class RemedialService
    {
        private readonly AppDbContext _db;

        public RemedialService(AppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public List<Student> GetEarlyWarningStudents(int classId)
        {
            try
            {
                // Fake logic for Early Warning: Students with average < 5
                // Assuming we just fetch all students for the demo and mock the warning
                var students = _db.Students.ToList();
                return students.Take(2).ToList(); // Return 2 mock students
            }
            catch { return new List<Student>(); }
        }

        public List<RemedialPlan> GetPlansByTeacher(int teacherId)
        {
            try
            {
                return _db.RemedialPlans.Where(p => p.TeacherId == teacherId).ToList();
            }
            catch { return new List<RemedialPlan>(); }
        }

        public bool CreatePlan(int teacherId, int studentId, string studentName, string subject, string goal, DateTime startDate, DateTime endDate)
        {
            try
            {
                _db.RemedialPlans.Add(new RemedialPlan
                {
                    TeacherId = teacherId,
                    StudentId = studentId,
                    StudentName = studentName,
                    Subject = subject,
                    Goal = goal,
                    StartDate = startDate,
                    EndDate = endDate,
                    Progress = 0
                });
                _db.SaveChanges();
                Log.Information("RemedialService: Plan created for Student {StudentId} by Teacher {TeacherId}", studentId, teacherId);
                
                // Notify parent
                var notifService = new NotificationService(_db);
                notifService.SendToParent(studentId, $"Nhà trường đã tạo kế hoạch phụ đạo môn {subject} cho con.", "Phụ đạo");

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "RemedialService: CreatePlan failed");
                return false;
            }
        }

        public List<RemedialSession> GetSessions(int planId)
        {
            try
            {
                return _db.RemedialSessions.Where(s => s.PlanId == planId).OrderByDescending(s => s.SessionDate).ToList();
            }
            catch { return new List<RemedialSession>(); }
        }

        public bool AddSession(int planId, string content, string improvement, int newProgress)
        {
            try
            {
                var plan = _db.RemedialPlans.Find(planId);
                if (plan != null)
                {
                    plan.Progress = newProgress;
                    _db.RemedialSessions.Add(new RemedialSession
                    {
                        PlanId = planId,
                        SessionDate = DateTime.Now,
                        Content = content,
                        Improvement = improvement
                    });
                    _db.SaveChanges();
                    Log.Information("RemedialService: Added session to Plan {PlanId}", planId);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "RemedialService: AddSession failed");
                return false;
            }
        }
    }
}

