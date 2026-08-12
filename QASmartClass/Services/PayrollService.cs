using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;

namespace QASmartClass.Services
{
    public class PayrollService
    {
        private readonly AppDbContext _db;

        public PayrollService(AppDbContext db)
        {
            _db = db;
        }

        public PayrollRecord CalculatePayroll(string staffName, int month, int year)
        {
            // Lấy StaffId từ StaffName
            var staff = _db.StaffProfiles.FirstOrDefault(s => s.FullName == staffName);
            if (staff == null)
            {
                throw new ArgumentException($"Không tìm thấy thông tin hồ sơ nhân sự có họ tên: '{staffName}'. Vui lòng tạo hồ sơ nhân sự trước.");
            }
            int staffId = staff.Id;

            // Lấy thông tin chấm công của nhân viên trong tháng
            var attendances = _db.StaffAttendances
                .Where(a => a.StaffId == staffId && a.Date.Month == month && a.Date.Year == year)
                .ToList();

            int workDays = attendances.Count(a => a.Status == "Present");
            
            // Giả định mức lương cơ bản theo ngày là 300,000 VND
            decimal dailyRate = 300000m;
            decimal baseSalary = workDays * dailyRate;
            
            // Phụ cấp (ví dụ: cố định 1,000,000 VND)
            decimal allowance = 1000000m;
            
            // Khấu trừ (ví dụ: BHXH 10.5% lương cơ bản)
            decimal deduction = baseSalary * 0.105m;
            
            // Thuế TNCN (giả định 5% cho phần vượt quá 11 triệu)
            decimal taxableIncome = (baseSalary + allowance) - deduction - 11000000m;
            decimal tax = taxableIncome > 0 ? taxableIncome * 0.05m : 0;

            // Kiểm tra xem đã có bản ghi tính lương cho tháng/năm này chưa
            var existing = _db.PayrollRecords.FirstOrDefault(p => p.StaffName == staffName && p.Month == month && p.Year == year);
            if (existing != null)
            {
                // Nếu đã thanh toán (Paid), trả về bản ghi cũ không thay đổi để bảo toàn lịch sử tài chính
                if (existing.Status == "Paid")
                {
                    return existing;
                }

                // Nếu là bản nháp (Draft), cho phép cập nhật lại số công và tính lại lương
                existing.BaseSalary = baseSalary;
                existing.Allowance = allowance;
                existing.Deduction = deduction;
                existing.Tax = tax;
                
                _db.SaveChanges();
                return existing;
            }

            var record = new PayrollRecord
            {
                StaffName = staffName,
                Month = month,
                Year = year,
                BaseSalary = baseSalary,
                Allowance = allowance,
                Deduction = deduction,
                Tax = tax,
                Status = "Draft"
            };

            _db.PayrollRecords.Add(record);
            _db.SaveChanges();

            return record;
        }

        public List<PayrollRecord> GetPayrollsByMonth(int month, int year)
        {
            return _db.PayrollRecords.Where(p => p.Month == month && p.Year == year).ToList();
        }

        public async Task<List<PayrollRecord>> GetPayrollsByMonthAsync(int month, int year)
        {
            return await _db.PayrollRecords.Where(p => p.Month == month && p.Year == year).ToListAsync();
        }

        public bool FinalizePayroll(int payrollId)
        {
            var record = _db.PayrollRecords.Find(payrollId);
            if (record != null && record.Status == "Draft")
            {
                record.Status = "Paid";
                _db.SaveChanges();
                return true;
            }
            return false;
        }

        public async Task<PayrollRecord> CalculatePayrollAsync(string staffName, int month, int year)
        {
            var staff = await _db.StaffProfiles.FirstOrDefaultAsync(s => s.FullName == staffName);
            if (staff == null)
            {
                throw new ArgumentException($"Không tìm thấy thông tin hồ sơ nhân sự có họ tên: '{staffName}'. Vui lòng tạo hồ sơ nhân sự trước.");
            }
            int staffId = staff.Id;

            var attendances = await _db.StaffAttendances
                .Where(a => a.StaffId == staffId && a.Date.Month == month && a.Date.Year == year)
                .ToListAsync();

            int workDays = attendances.Count(a => a.Status == "Present");
            decimal dailyRate = 300000m;
            decimal baseSalary = workDays * dailyRate;
            decimal allowance = 1000000m;
            decimal deduction = baseSalary * 0.105m;
            decimal taxableIncome = (baseSalary + allowance) - deduction - 11000000m;
            decimal tax = taxableIncome > 0 ? taxableIncome * 0.05m : 0;

            var existing = await _db.PayrollRecords.FirstOrDefaultAsync(p => p.StaffName == staffName && p.Month == month && p.Year == year);
            if (existing != null)
            {
                if (existing.Status == "Paid") return existing;
                existing.BaseSalary = baseSalary;
                existing.Allowance = allowance;
                existing.Deduction = deduction;
                existing.Tax = tax;
                await _db.SaveChangesAsync();
                return existing;
            }

            var record = new PayrollRecord
            {
                StaffName = staffName,
                Month = month,
                Year = year,
                BaseSalary = baseSalary,
                Allowance = allowance,
                Deduction = deduction,
                Tax = tax,
                Status = "Draft"
            };
            _db.PayrollRecords.Add(record);
            await _db.SaveChangesAsync();
            return record;
        }

        public async Task<bool> FinalizePayrollAsync(int payrollId)
        {
            var record = await _db.PayrollRecords.FindAsync(payrollId);
            if (record != null && record.Status == "Draft")
            {
                record.Status = "Paid";
                await _db.SaveChangesAsync();
                return true;
            }
            return false;
        }
    }
}

