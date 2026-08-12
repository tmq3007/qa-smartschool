using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;

namespace QASmartClass.Services
{
    public class KitchenService
    {
        private readonly AppDbContext _db;

        public KitchenService(AppDbContext db)
        {
            _db = db;
        }

        public SchoolMenu AddMenu(SchoolMenu menu)
        {
            _db.SchoolMenus.Add(menu);
            _db.SaveChanges();
            return menu;
        }

        public List<SchoolMenu> GetMenusByDate(DateTime date)
        {
            var startDate = date.Date;
            var endDate = startDate.AddDays(1);
            return _db.SchoolMenus
                .Where(m => m.Date >= startDate && m.Date < endDate)
                .ToList();
        }

        public async Task<List<SchoolMenu>> GetMenusByDateAsync(DateTime date)
        {
            var startDate = date.Date;
            var endDate = startDate.AddDays(1);
            return await _db.SchoolMenus
                .Where(m => m.Date >= startDate && m.Date < endDate)
                .ToListAsync();
        }

        public List<FoodAllergy> GetAllergies()
        {
            return _db.FoodAllergies.ToList();
        }

        public async Task<List<FoodAllergy>> GetAllergiesAsync()
        {
            return await _db.FoodAllergies.ToListAsync();
        }

        public FoodAllergy AddAllergy(FoodAllergy allergy)
        {
            _db.FoodAllergies.Add(allergy);
            _db.SaveChanges();
            return allergy;
        }

        public int GetEstimatedMeals(DateTime date)
        {
            // Lấy danh sách điểm danh, đếm số HS có mặt để ước tính suất ăn
            var startDate = date.Date;
            var endDate = startDate.AddDays(1);
            int presentCount = _db.AttendanceRecords
                .Count(a => a.Date >= startDate && a.Date < endDate && a.Status == "Present");
                
            return presentCount > 0 ? presentCount : _db.Students.Count(s => s.Status == "Active");
        }

        public async Task<int> GetEstimatedMealsAsync(DateTime date)
        {
            var startDate = date.Date;
            var endDate = startDate.AddDays(1);
            int presentCount = await _db.AttendanceRecords
                .CountAsync(a => a.Date >= startDate && a.Date < endDate && a.Status == "Present");
                
            return presentCount > 0 ? presentCount : await _db.Students.CountAsync(s => s.Status == "Active");
        }

        public async Task<SchoolMenu> AddMenuAsync(SchoolMenu menu)
        {
            _db.SchoolMenus.Add(menu);
            await _db.SaveChangesAsync();
            return menu;
        }

        public async Task<FoodAllergy> AddAllergyAsync(FoodAllergy allergy)
        {
            _db.FoodAllergies.Add(allergy);
            await _db.SaveChangesAsync();
            return allergy;
        }
    }
}

