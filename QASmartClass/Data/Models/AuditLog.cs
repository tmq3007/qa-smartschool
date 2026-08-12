using System;

namespace QASmartClass.Data
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty; // VD: Login, Delete_Task, Approve_Leave
        public string ActorName { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string RowHash { get; set; } = string.Empty;

        /// <summary>
        /// Tự động phân cấp mức độ nghiêm trọng: Info | Warning | Critical
        /// </summary>
        public string Severity
        {
            get
            {
                if (string.IsNullOrEmpty(Action)) return "Info";
                
                string actionLower = Action.ToLower();
                string detailsLower = (Details ?? string.Empty).ToLower();

                if (actionLower.Contains("tampered") || actionLower.Contains("intrusion") || 
                    actionLower.Contains("failure") || actionLower.Contains("sql_injection") || 
                    actionLower.Contains("bypass") || actionLower.Contains("integrityerror") ||
                    detailsLower.Contains("tampered") || detailsLower.Contains("intrusion") || 
                    detailsLower.Contains("vi phạm"))
                {
                    return "Critical";
                }

                if (actionLower.Contains("rejected") || actionLower.Contains("emergency") || 
                    actionLower.Contains("blocked") || actionLower.Contains("reset_password") || 
                    actionLower.Contains("limit_exceeded") || actionLower.Contains("unauthorized") ||
                    detailsLower.Contains("từ chối") || detailsLower.Contains("khẩn cấp"))
                {
                    return "Warning";
                }

                return "Info";
            }
        }
    }
}
