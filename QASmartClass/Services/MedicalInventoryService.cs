using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;

namespace QASmartClass.Services
{
    public class MedicalInventoryService
    {
        private readonly AppDbContext _db;

        public MedicalInventoryService(AppDbContext db)
        {
            _db = db;
        }

        public MedicalSupply AddSupply(MedicalSupply supply)
        {
            var existing = _db.MedicalSupplies.FirstOrDefault(s => 
                s.Name.ToLower() == supply.Name.ToLower() && 
                s.ExpiryDate.Date == supply.ExpiryDate.Date);

            if (existing != null)
            {
                existing.Quantity += supply.Quantity;
                _db.SaveChanges();
                return existing;
            }

            _db.MedicalSupplies.Add(supply);
            _db.SaveChanges();
            return supply;
        }

        public List<MedicalSupply> GetAllSupplies()
        {
            return _db.MedicalSupplies.OrderBy(s => s.ExpiryDate).ToList();
        }

        public List<MedicalSupply> GetExpiringSupplies(int daysThreshold = 30)
        {
            DateTime thresholdDate = DateTime.Today.AddDays(daysThreshold);
            return _db.MedicalSupplies
                .Where(s => s.ExpiryDate <= thresholdDate)
                .OrderBy(s => s.ExpiryDate)
                .ToList();
        }

        public bool UpdateQuantity(int supplyId, int newQuantity)
        {
            var supply = _db.MedicalSupplies.Find(supplyId);
            if (supply != null)
            {
                supply.Quantity = newQuantity;
                _db.SaveChanges();
                return true;
            }
            return false;
        }
    }
}

