using System;
using System.Linq;
using QASmartClass.Data;

namespace QASmartClass.Services
{
    public class WorkflowEngineService
    {
        private readonly AppDbContext _db;

        public WorkflowEngineService(AppDbContext dbContext)
        {
            _db = dbContext;
        }

        /// <summary>
        /// S2-01: Multi-level approval workflow.
        /// Khi Approve ở cấp < MaxLevel → chuyển sang "PendingLevel{N+1}".
        /// Khi Approve ở cấp == MaxLevel → status = "Approved".
        /// </summary>
        public bool ProcessDocumentRoute(int routeId, string action, string processedBy, string notes)
        {
            var route = _db.DocumentRoutes.Find(routeId);
            if (route == null) return false;

            if (action == "Approve")
            {
                route.CurrentApprovalLevel++;

                if (route.CurrentApprovalLevel >= route.MaxApprovalLevel)
                {
                    route.Status = "Approved";
                    route.Notes = $"[Cấp {route.CurrentApprovalLevel}/{route.MaxApprovalLevel} — {processedBy} - Duyệt hoàn tất] {notes}";
                }
                else
                {
                    route.Status = $"PendingLevel{route.CurrentApprovalLevel + 1}";
                    route.Notes = $"[Cấp {route.CurrentApprovalLevel}/{route.MaxApprovalLevel} — {processedBy} - Duyệt → chờ cấp tiếp] {notes}";
                }
            }
            else if (action == "Reject")
            {
                route.Status = "Rejected";
                route.Notes = $"[Cấp {route.CurrentApprovalLevel + 1} — {processedBy} - Từ chối] {notes}";
            }
            else if (action == "Forward")
            {
                route.Status = "Forwarded";
                route.Notes = $"[{processedBy} - Chuyển tiếp] {notes}";
            }

            // Ghi AuditLog cho mỗi hành động phê duyệt
            _db.AuditLogs.Add(new AuditLog
            {
                Action = $"Document_{action}_Level{route.CurrentApprovalLevel}",
                ActorName = processedBy,
                Details = $"Văn bản '{route.DocumentTitle}' → {route.Status} (Cấp {route.CurrentApprovalLevel}/{route.MaxApprovalLevel}). Ghi chú: {notes}",
                Timestamp = DateTime.Now
            });

            _db.SaveChanges();
            return true;
        }

        public async Task<bool> ProcessDocumentRouteAsync(int routeId, string action, string processedBy, string notes)
        {
            var route = await _db.DocumentRoutes.FindAsync(routeId);
            if (route == null) return false;

            if (action == "Approve")
            {
                route.CurrentApprovalLevel++;

                if (route.CurrentApprovalLevel >= route.MaxApprovalLevel)
                {
                    route.Status = "Approved";
                    route.Notes = $"[Cấp {route.CurrentApprovalLevel}/{route.MaxApprovalLevel} — {processedBy} - Duyệt hoàn tất] {notes}";
                }
                else
                {
                    route.Status = $"PendingLevel{route.CurrentApprovalLevel + 1}";
                    route.Notes = $"[Cấp {route.CurrentApprovalLevel}/{route.MaxApprovalLevel} — {processedBy} - Duyệt → chờ cấp tiếp] {notes}";
                }
            }
            else if (action == "Reject")
            {
                route.Status = "Rejected";
                route.Notes = $"[Cấp {route.CurrentApprovalLevel + 1} — {processedBy} - Từ chối] {notes}";
            }
            else if (action == "Forward")
            {
                route.Status = "Forwarded";
                route.Notes = $"[{processedBy} - Chuyển tiếp] {notes}";
            }

            _db.AuditLogs.Add(new AuditLog
            {
                Action = $"Document_{action}_Level{route.CurrentApprovalLevel}",
                ActorName = processedBy,
                Details = $"Văn bản '{route.DocumentTitle}' → {route.Status} (Cấp {route.CurrentApprovalLevel}/{route.MaxApprovalLevel}). Ghi chú: {notes}",
                Timestamp = DateTime.Now
            });

            await _db.SaveChangesAsync();
            return true;
        }
    }
}

