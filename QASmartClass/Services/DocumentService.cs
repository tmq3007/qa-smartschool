using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;

namespace QASmartClass.Services
{
    public class DocumentService
    {
        private readonly AppDbContext _db;

        public DocumentService(AppDbContext db)
        {
            _db = db;
        }

        public OfficialDocument AddDocument(OfficialDocument doc)
        {
            doc.IssuedDate = DateTime.Now;
            _db.OfficialDocuments.Add(doc);
            _db.SaveChanges();
            return doc;
        }

        public List<OfficialDocument> GetAllDocuments()
        {
            return _db.OfficialDocuments.OrderByDescending(d => d.IssuedDate).ToList();
        }

        public async Task<List<OfficialDocument>> GetAllDocumentsAsync()
        {
            return await _db.OfficialDocuments.OrderByDescending(d => d.IssuedDate).ToListAsync();
        }

        public List<OfficialDocument> SearchDocuments(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return GetAllDocuments();
            var lowerKey = keyword.ToLower();
            return _db.OfficialDocuments
                .Where(d => d.Title.ToLower().Contains(lowerKey) || d.DocumentNumber.ToLower().Contains(lowerKey))
                .OrderByDescending(d => d.IssuedDate)
                .ToList();
        }

        public bool UpdateStatus(int docId, string status)
        {
            var doc = _db.OfficialDocuments.Find(docId);
            if (doc != null)
            {
                doc.Status = status;
                _db.SaveChanges();
                return true;
            }
            return false;
        }

        public bool DeleteDocument(int docId)
        {
            var doc = _db.OfficialDocuments.Find(docId);
            if (doc != null)
            {
                _db.OfficialDocuments.Remove(doc);
                _db.SaveChanges();
                return true;
            }
            return false;
        }

        public async Task<OfficialDocument> AddDocumentAsync(OfficialDocument doc)
        {
            doc.IssuedDate = DateTime.Now;
            _db.OfficialDocuments.Add(doc);
            await _db.SaveChangesAsync();
            return doc;
        }

        public async Task<List<OfficialDocument>> SearchDocumentsAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return await GetAllDocumentsAsync();
            var lowerKey = keyword.ToLower();
            return await _db.OfficialDocuments
                .Where(d => d.Title.ToLower().Contains(lowerKey) || d.DocumentNumber.ToLower().Contains(lowerKey))
                .OrderByDescending(d => d.IssuedDate)
                .ToListAsync();
        }
    }
}

