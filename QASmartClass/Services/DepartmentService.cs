using QASmartClass.Data;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QASmartClass.Services
{
    public class DepartmentKpiDto
    {
        public int TeacherCount { get; set; }
        public int LessonPlanCount { get; set; }
        public int ObservationCount { get; set; }
    }

    /// <summary>
    /// WI-05: Service qu?n lư T? chuyên môn
    /// </summary>
    public class DepartmentService
    {
        private readonly AppDbContext _db;

        public DepartmentService(AppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public bool CreateDepartment(string name, string headTeacherId, string schoolYear)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name)) return false;
                
                // Tên t? không trùng trong cùng nam h?c
                if (_db.Departments.Any(d => d.DepartmentName == name && d.SchoolYear == schoolYear))
                {
                    Log.Warning("DepartmentService: Department {Name} already exists for school year {Year}", name, schoolYear);
                    return false;
                }

                var dept = new Department
                {
                    DepartmentName = name,
                    HeadTeacherId = headTeacherId ?? string.Empty,
                    DeputyHeadId = string.Empty,
                    Description = string.Empty,
                    SchoolYear = schoolYear ?? string.Empty
                };
                _db.Departments.Add(dept);
                _db.SaveChanges();
                Log.Information("DepartmentService: Created department {Name}", name);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DepartmentService: CreateDepartment failed");
                return false;
            }
        }

        public bool UpdateDepartment(int id, string name, string headId, string deputyId)
        {
            try
            {
                var dept = _db.Departments.Find(id);
                if (dept == null) return false;

                // HeadTeacherId và DeputyHeadId không cùng ngu?i
                if (!string.IsNullOrEmpty(headId) && !string.IsNullOrEmpty(deputyId) && headId == deputyId)
                {
                    Log.Warning("DepartmentService: Head and Deputy cannot be the same person");
                    return false;
                }

                dept.DepartmentName = name ?? dept.DepartmentName;
                dept.HeadTeacherId = headId ?? dept.HeadTeacherId;
                dept.DeputyHeadId = deputyId ?? dept.DeputyHeadId;

                _db.SaveChanges();
                Log.Information("DepartmentService: Updated department {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DepartmentService: UpdateDepartment failed");
                return false;
            }
        }

        public bool DeleteDepartment(int id)
        {
            try
            {
                var dept = _db.Departments.Find(id);
                if (dept == null) return false;

                // Không cho xóa n?u c̣n members
                if (_db.DepartmentMembers.Any(m => m.DepartmentId == id))
                {
                    Log.Warning("DepartmentService: Cannot delete department {Id} as it still has members", id);
                    return false;
                }

                _db.Departments.Remove(dept);
                _db.SaveChanges();
                Log.Information("DepartmentService: Deleted department {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DepartmentService: DeleteDepartment failed");
                return false;
            }
        }

        public bool AddMember(int deptId, int staffId, string role)
        {
            try
            {
                // Check if department exists
                if (!_db.Departments.Any(d => d.Id == deptId)) return false;

                // Check if staff exists
                if (!_db.StaffProfiles.Any(s => s.Id == staffId)) return false;

                // GV dă thu?c t? khác -> c?nh báo và ch?n
                var existingMember = _db.DepartmentMembers.FirstOrDefault(m => m.StaffId == staffId);
                if (existingMember != null)
                {
                    Log.Warning("DepartmentService: Staff {StaffId} already belongs to department {DeptId}", staffId, existingMember.DepartmentId);
                    return false;
                }

                var member = new DepartmentMember
                {
                    DepartmentId = deptId,
                    StaffId = staffId,
                    Role = role ?? "Thành viên",
                    JoinedDate = DateTime.Now
                };
                _db.DepartmentMembers.Add(member);
                _db.SaveChanges();
                Log.Information("DepartmentService: Added staff {StaffId} to department {DeptId} as {Role}", staffId, deptId, role);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DepartmentService: AddMember failed");
                return false;
            }
        }

        public bool RemoveMember(int deptId, int staffId)
        {
            try
            {
                var member = _db.DepartmentMembers.FirstOrDefault(m => m.DepartmentId == deptId && m.StaffId == staffId);
                if (member == null) return false;

                _db.DepartmentMembers.Remove(member);
                _db.SaveChanges();
                Log.Information("DepartmentService: Removed staff {StaffId} from department {DeptId}", staffId, deptId);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DepartmentService: RemoveMember failed");
                return false;
            }
        }

        public List<DepartmentMember> GetMembersByDepartment(int deptId)
        {
            try
            {
                return _db.DepartmentMembers.Where(m => m.DepartmentId == deptId).ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DepartmentService: GetMembersByDepartment failed");
                return new List<DepartmentMember>();
            }
        }

        public List<Department> GetAllDepartments()
        {
            try
            {
                return _db.Departments.ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DepartmentService: GetAllDepartments failed");
                return new List<Department>();
            }
        }

        public DepartmentKpiDto GetDepartmentKPI(int deptId, int month, int year)
        {
            try
            {
                var teacherCount = _db.DepartmentMembers.Count(m => m.DepartmentId == deptId);
                
                var staffIds = _db.DepartmentMembers
                    .Where(m => m.DepartmentId == deptId)
                    .Select(m => m.StaffId)
                    .ToList();

                var teacherCodes = _db.StaffProfiles
                    .Where(s => staffIds.Contains(s.Id))
                    .Select(s => s.StaffCode)
                    .ToList();

                int planCount = 0;
                try
                {
                    planCount = _db.LessonPlans
                        .Count(lp => teacherCodes.Contains(lp.TeacherId) && lp.CreatedAt.Month == month && lp.CreatedAt.Year == year);
                }
                catch { }

                int obsCount = 0;
                try
                {
                    obsCount = _db.ClassObservations
                        .Count(co => staffIds.Contains(co.TeacherId) && co.ObservedAt.Month == month && co.ObservedAt.Year == year);
                }
                catch { }

                return new DepartmentKpiDto
                {
                    TeacherCount = teacherCount,
                    LessonPlanCount = planCount,
                    ObservationCount = obsCount
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DepartmentService: GetDepartmentKPI failed");
                return new DepartmentKpiDto();
            }
        }
    }
}

