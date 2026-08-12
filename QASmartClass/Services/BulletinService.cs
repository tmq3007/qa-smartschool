using QASmartClass.Data;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QASmartClass.Services
{
    public class BulletinResult
    {
        public bool Success { get; set; }
        public string Status { get; set; } = "Success"; // Success, RequireConfirmation, AutoCorrected
        public string Message { get; set; } = string.Empty;
        public string CorrectedAudience { get; set; } = string.Empty;

        public static implicit operator bool(BulletinResult result)
        {
            return result != null && result.Success;
        }
    }

    public class BulletinService
    {
        private readonly AppDbContext _db;

        public BulletinService(AppDbContext db)
        {
            _db = db;
        }

        public BulletinResult CreateBulletin(string title, string content, string audience, string priority, string createdBy,
            DateTime? scheduledAt = null, DateTime? expiresAt = null, string imageUrl = "", string attachmentPath = "", string attachmentSignature = "", string category = "School", bool bypassWarning = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
                    return new BulletinResult { Success = false, Status = "Error", Message = "Tiêu đề và nội dung không được trống." };

                // Validate category
                var validCategories = new[] { "School", "YouthUnion", "Staff", "Student", "Competition", "Public", "TradeUnion", "Celebration" };
                if (!validCategories.Contains(category))
                {
                    Log.Warning("BulletinService: Category không hợp lệ: {Category}", category);
                    return new BulletinResult { Success = false, Status = "Error", Message = "Danh mục không hợp lệ." };
                }

                // Validate audience giá trị hợp lệ
                var validAudiences = new[] { "All", "GV", "HS", "PH", "NV", "DV" };
                if (!validAudiences.Contains(audience))
                {
                    Log.Warning("BulletinService: Audience không hợp lệ: {Audience}", audience);
                    return new BulletinResult { Success = false, Status = "Error", Message = "Đối tượng không hợp lệ." };
                }

                var safetyResultStatus = "Success";
                var safetyResultMessage = string.Empty;

                // Safety mode check
                var safetyMode = _db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Bulletin_StrictCategorySafetyMode")?.Value ?? "2";
                if (category == "Staff" || category == "TradeUnion")
                {
                    if (audience == "All" || audience == "HS")
                    {
                        if (safetyMode == "2") // Strict Auto-correct
                        {
                            audience = "GV";
                            safetyResultStatus = "AutoCorrected";
                            safetyResultMessage = "Hệ thống đã tự động chuyển đối tượng nhận tin về Giáo viên để bảo mật thông tin nội bộ.";
                        }
                        else if (safetyMode == "1") // Warn
                        {
                            Log.Warning("[Bulletin] Sư phạm cảnh báo: Danh mục nội bộ {Category} đăng cho đối tượng ngoài {Audience}", category, audience);
                            if (!bypassWarning)
                            {
                                return new BulletinResult
                                {
                                    Success = false,
                                    Status = "RequireConfirmation",
                                    Message = "Bạn đang gửi thông báo nội bộ (Nhân sự/Công đoàn) cho đối tượng công cộng. Bạn có chắc chắn muốn tiếp tục?"
                                };
                            }
                        }
                    }
                }

                // Storage Format Mode
                var formatMode = _db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Bulletin_StorageFormatMode")?.Value ?? "0";
                if (formatMode == "1" && !content.StartsWith("<FlowDocument"))
                {
                    content = $"<FlowDocument><Paragraph><Run>{content}</Run></Paragraph></FlowDocument>";
                }

                // Attachment Storage Mode
                var attachMode = _db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Bulletin_AttachmentStorageMode")?.Value ?? "0";
                if (attachMode == "1" && !string.IsNullOrEmpty(attachmentPath) && !attachmentPath.StartsWith("http"))
                {
                    attachmentPath = $"https://s3.school.edu/bulletins/{System.IO.Path.GetFileName(attachmentPath)}?token=mock_sas_token";
                }

                // Verify attachment signature if required
                var sigSetting = _db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Bulletin_AttachmentSignatureRequired")?.Value ?? "0";
                if (sigSetting != "0" && !string.IsNullOrEmpty(attachmentPath))
                {
                    if (!VerifyAttachmentSignature(attachmentPath, attachmentSignature, sigSetting))
                    {
                        Log.Warning("[Bulletin] Attachment signature verification failed for {Path}", attachmentPath);
                        return new BulletinResult { Success = false, Status = "Error", Message = "Xác thực chữ ký tệp đính kèm thất bại." };
                    }
                }

                // AI Content Safety Moderation
                var modSetting = _db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Bulletin_AutoModerationMode")?.Value ?? "0";
                var actionSetting = _db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Bulletin_ModerationAction")?.Value ?? "0";
                var threshold = int.Parse(_db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Bulletin_ModerationThreshold")?.Value ?? "70");
                int safetyScore = AnalyzeContentSafety(title, content);
                bool hasProfanity = HasProfanityText(title) || HasProfanityText(content) || (safetyScore < threshold);

                bool isScheduled = scheduledAt.HasValue && scheduledAt.Value > DateTime.Now;
                
                // Workflow Setting
                var workflow = _db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Bulletin_ApprovalWorkflow")?.Value ?? "2";
                
                string status = "Published";
                bool requiresApproval = false;
                if (workflow != "0")
                {
                    bool isStudentOrYouth = createdBy.StartsWith("HS_") || createdBy.StartsWith("DV_") || 
                                           _db.Students.Any(s => s.StudentCode == createdBy) ||
                                           _db.Students.Any(s => s.StudentCode == createdBy && _db.YouthMembers.Any(y => y.StudentId == s.Id));

                    if (isStudentOrYouth || priority == "High" || category == "Staff" || category == "TradeUnion")
                    {
                        requiresApproval = true;
                    }
                }

                if (requiresApproval)
                {
                    status = "Pending";
                }
                else if (isScheduled)
                {
                    status = "Scheduled";
                }

                if (modSetting == "1" && hasProfanity)
                {
                    if (actionSetting == "0") // Censor
                    {
                        title = FilterProfanity(title);
                        content = FilterProfanity(content);
                    }
                    else if (actionSetting == "1") // Block
                    {
                        status = "Pending";
                    }
                }

                var bulletin = new Bulletin
                {
                    Title = title.Trim(),
                    Content = content.Trim(),
                    Audience = audience,
                    Priority = priority,
                    CreatedBy = createdBy,
                    Status = status,
                    ScheduledAt = scheduledAt,
                    ExpiresAt = expiresAt,
                    IsScheduled = isScheduled,
                    ImageUrl = imageUrl,
                    AttachmentPath = attachmentPath,
                    Category = category
                };

                _db.Bulletins.Add(bulletin);
                _db.SaveChanges();

                Log.Information("[Bulletin] Created '{Title}' | Category={Cat} | Scheduled={Sched} | Expires={Exp}",
                    title, category, scheduledAt?.ToString("dd/MM HH:mm") ?? "Now", expiresAt?.ToString("dd/MM HH:mm") ?? "Never");

                if (status == "Pending" && workflow != "0")
                {
                    SubmitForApproval(bulletin.Id);
                }

                return new BulletinResult 
                { 
                    Success = true, 
                    Status = safetyResultStatus, 
                    Message = safetyResultMessage, 
                    CorrectedAudience = audience 
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi tạo Bulletin");
                return new BulletinResult { Success = false, Status = "Error", Message = "Lỗi khi tạo Bulletin: " + ex.Message };
            }
        }

        public BulletinResult UpdateBulletin(int id, string title, string content, string audience, string priority, string imageUrl,
            DateTime? scheduledAt = null, DateTime? expiresAt = null, string attachmentPath = "", string attachmentSignature = "", string category = "School", bool bypassWarning = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
                    return new BulletinResult { Success = false, Status = "Error", Message = "Tiêu đề và nội dung không được trống." };

                var b = _db.Bulletins.FirstOrDefault(x => x.Id == id);
                if (b == null) return new BulletinResult { Success = false, Status = "Error", Message = "Không tìm thấy thông báo." };

                // Validate category
                var validCategories = new[] { "School", "YouthUnion", "Staff", "Student", "Competition", "Public", "TradeUnion", "Celebration" };
                if (!validCategories.Contains(category))
                {
                    Log.Warning("BulletinService: Category không hợp lệ: {Category}", category);
                    return new BulletinResult { Success = false, Status = "Error", Message = "Danh mục không hợp lệ." };
                }

                // Validate audience
                var validAudiences = new[] { "All", "GV", "HS", "PH", "NV", "DV" };
                if (!validAudiences.Contains(audience)) 
                    return new BulletinResult { Success = false, Status = "Error", Message = "Đối tượng không hợp lệ." };

                var safetyResultStatus = "Success";
                var safetyResultMessage = string.Empty;

                // Safety mode check
                var safetyMode = _db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Bulletin_StrictCategorySafetyMode")?.Value ?? "2";
                if (category == "Staff" || category == "TradeUnion")
                {
                    if (audience == "All" || audience == "HS")
                    {
                        if (safetyMode == "2") // Strict Auto-correct
                        {
                            audience = "GV";
                            safetyResultStatus = "AutoCorrected";
                            safetyResultMessage = "Hệ thống đã tự động chuyển đối tượng nhận tin về Giáo viên để bảo mật thông tin nội bộ.";
                        }
                        else if (safetyMode == "1") // Warn
                        {
                            Log.Warning("[Bulletin] Sư phạm cảnh báo: Danh mục nội bộ {Category} đăng cho đối tượng ngoài {Audience}", category, audience);
                            if (!bypassWarning)
                            {
                                return new BulletinResult
                                {
                                    Success = false,
                                    Status = "RequireConfirmation",
                                    Message = "Bạn đang gửi thông báo nội bộ (Nhân sự/Công đoàn) cho đối tượng công cộng. Bạn có chắc chắn muốn tiếp tục?"
                                };
                            }
                        }
                    }
                }

                // Storage Format Mode
                var formatMode = _db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Bulletin_StorageFormatMode")?.Value ?? "0";
                if (formatMode == "1" && !content.StartsWith("<FlowDocument"))
                {
                    content = $"<FlowDocument><Paragraph><Run>{content}</Run></Paragraph></FlowDocument>";
                }

                // Attachment Storage Mode
                var attachMode = _db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Bulletin_AttachmentStorageMode")?.Value ?? "0";
                if (attachMode == "1" && !string.IsNullOrEmpty(attachmentPath) && !attachmentPath.StartsWith("http"))
                {
                    attachmentPath = $"https://s3.school.edu/bulletins/{System.IO.Path.GetFileName(attachmentPath)}?token=mock_sas_token";
                }

                // Verify attachment signature if required
                var sigSetting = _db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Bulletin_AttachmentSignatureRequired")?.Value ?? "0";
                if (sigSetting != "0" && !string.IsNullOrEmpty(attachmentPath))
                {
                    if (!VerifyAttachmentSignature(attachmentPath, attachmentSignature, sigSetting))
                    {
                        Log.Warning("[Bulletin] Attachment signature verification failed for update on {Path}", attachmentPath);
                        return new BulletinResult { Success = false, Status = "Error", Message = "Xác thực chữ ký tệp đính kèm thất bại." };
                    }
                }

                // AI Content Safety Moderation
                var modSetting = _db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Bulletin_AutoModerationMode")?.Value ?? "0";
                var actionSetting = _db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Bulletin_ModerationAction")?.Value ?? "0";
                var threshold = int.Parse(_db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Bulletin_ModerationThreshold")?.Value ?? "70");
                int safetyScore = AnalyzeContentSafety(title, content);
                bool hasProfanity = HasProfanityText(title) || HasProfanityText(content) || (safetyScore < threshold);

                bool isScheduled = scheduledAt.HasValue && scheduledAt.Value > DateTime.Now;
                
                // Workflow Setting
                var workflow = _db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Bulletin_ApprovalWorkflow")?.Value ?? "2";

                string status = "Published";
                bool requiresApproval = false;
                if (workflow != "0")
                {
                    bool isStudentOrYouth = b.CreatedBy.StartsWith("HS_") || b.CreatedBy.StartsWith("DV_") || 
                                           _db.Students.Any(s => s.StudentCode == b.CreatedBy) ||
                                           _db.Students.Any(s => s.StudentCode == b.CreatedBy && _db.YouthMembers.Any(y => y.StudentId == s.Id));

                    if (isStudentOrYouth || priority == "High" || category == "Staff" || category == "TradeUnion")
                    {
                        requiresApproval = true;
                    }
                }

                if (requiresApproval)
                {
                    status = "Pending";
                }
                else if (isScheduled)
                {
                    status = "Scheduled";
                }

                if (modSetting == "1" && hasProfanity)
                {
                    if (actionSetting == "0")
                    {
                        title = FilterProfanity(title);
                        content = FilterProfanity(content);
                    }
                    else if (actionSetting == "1")
                    {
                        status = "Pending";
                    }
                }

                b.Title = title.Trim();
                b.Content = content.Trim();
                b.Audience = audience;
                b.Priority = priority;
                b.Status = status;
                b.ScheduledAt = scheduledAt;
                b.ExpiresAt = expiresAt;
                b.IsScheduled = isScheduled;
                b.ImageUrl = imageUrl;
                b.Category = category;
                if (!string.IsNullOrEmpty(attachmentPath))
                {
                    b.AttachmentPath = attachmentPath;
                }

                _db.SaveChanges();
                Log.Information("[Bulletin] Updated bulletin #{Id} in-place successfully.", id);

                if (status == "Pending" && workflow != "0")
                {
                    SubmitForApproval(b.Id);
                }

                return new BulletinResult 
                { 
                    Success = true, 
                    Status = safetyResultStatus, 
                    Message = safetyResultMessage, 
                    CorrectedAudience = audience 
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi cập nhật Bulletin");
                return new BulletinResult { Success = false, Status = "Error", Message = "Lỗi khi cập nhật Bulletin: " + ex.Message };
            }
        }

        public List<Bulletin> GetBulletins(string audience, int page = 1, string category = "All")
        {
            try
            {
                var now = DateTime.Now;
                var query = _db.Bulletins.AsQueryable();
                if (audience != "All")
                {
                    query = query.Where(b => b.Audience == audience || b.Audience == "All");
                }
                query = query.Where(b => b.Status == "Published" && (!b.ExpiresAt.HasValue || b.ExpiresAt > now));
                if (category != "All")
                {
                    query = query.Where(b => b.Category == category);
                }
                return query.OrderByDescending(b => b.CreatedAt)
                            .Skip((page - 1) * 10)
                            .Take(10)
                            .ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi lấy danh sách Bulletins");
                return new List<Bulletin>();
            }
        }

        /// <summary>P2-06: Lay tat ca bulletins ke ca Scheduled (cho admin/GV quan ly)</summary>
        public List<Bulletin> GetAllBulletins(int page = 1, string category = "All")
        {
            try
            {
                var query = _db.Bulletins.AsQueryable();
                if (category != "All")
                {
                    query = query.Where(b => b.Category == category);
                }
                return query.OrderByDescending(b => b.CreatedAt)
                            .Skip((page - 1) * 20)
                            .Take(20)
                            .ToList();
            }
            catch (Exception ex) { Log.Error(ex, "GetAllBulletins error"); return new List<Bulletin>(); }
        }

        /// <summary>P2-06: Xuat ban cac thong bao da den gio len lich</summary>
        public int PublishDue()
        {
            int count = 0;
            try
            {
                var now = DateTime.Now;
                var due = _db.Bulletins
                    .Where(b => b.IsScheduled && b.Status == "Scheduled"
                             && b.ScheduledAt.HasValue && b.ScheduledAt <= now)
                    .ToList();

                foreach (var b in due)
                {
                    b.Status = "Published";
                    b.IsScheduled = false;
                    count++;
                    Log.Information("[Bulletin] Auto-published: '{Title}'", b.Title);
                }

                // An cac thong bao het han
                var expired = _db.Bulletins
                    .Where(b => b.Status == "Published"
                             && b.ExpiresAt.HasValue && b.ExpiresAt <= now)
                    .ToList();

                foreach (var b in expired)
                {
                    b.Status = "Expired";
                    Log.Information("[Bulletin] Expired: '{Title}'", b.Title);
                }

                if (count > 0 || expired.Any()) _db.SaveChanges();
            }
            catch (Exception ex) { Log.Error(ex, "PublishDue error"); }
            return count;
        }

        /// <summary>P2-06: Lay cac thong bao dang cho len lich</summary>
        public List<Bulletin> GetScheduledBulletins()
        {
            try
            {
                return _db.Bulletins
                    .Where(b => b.IsScheduled && b.Status == "Scheduled")
                    .OrderBy(b => b.ScheduledAt)
                    .ToList();
            }
            catch { return new List<Bulletin>(); }
        }

        // --- PHASE 1.2: Analytics Methods ---
        public void IncrementViewCount(int bulletinId)
        {
            try
            {
                var b = _db.Bulletins.FirstOrDefault(x => x.Id == bulletinId);
                if (b != null)
                {
                    b.ViewCount++;
                    _db.SaveChanges();
                }
            }
            catch (Exception ex) { Log.Error(ex, "Lỗi tăng ViewCount cho Bulletin {Id}", bulletinId); }
        }

        // BUG-06 FIX: Reload single entity from DB
        public Bulletin? GetBulletinById(int bulletinId)
        {
            try
            {
                return _db.Bulletins.FirstOrDefault(x => x.Id == bulletinId);
            }
            catch (Exception ex) { Log.Error(ex, "Lỗi lấy Bulletin {Id}", bulletinId); return null; }
        }

        public void IncrementLikeCount(int bulletinId)
        {
            try
            {
                var b = _db.Bulletins.FirstOrDefault(x => x.Id == bulletinId);
                if (b != null)
                {
                    b.LikeCount++;
                    _db.SaveChanges();
                }
            }
            catch (Exception ex) { Log.Error(ex, "Lỗi tăng LikeCount cho Bulletin {Id}", bulletinId); }
        }

        public void UpdateQrCodePath(int bulletinId, string path)
        {
            try
            {
                var b = _db.Bulletins.FirstOrDefault(x => x.Id == bulletinId);
                if (b != null)
                {
                    b.QrCodePath = path;
                    _db.SaveChanges();
                }
            }
            catch (Exception ex) { Log.Error(ex, "Lỗi cập nhật QrCodePath cho Bulletin {Id}", bulletinId); }
        }

        // --- PHASE 2.1 & 2.2: Engagement Methods ---
        public void ToggleLike(int bulletinId, bool isLiking)
        {
            try
            {
                var b = _db.Bulletins.FirstOrDefault(x => x.Id == bulletinId);
                if (b != null)
                {
                    if (isLiking) b.LikeCount++;
                    else if (b.LikeCount > 0) b.LikeCount--;
                    _db.SaveChanges();
                }
            }
            catch (Exception ex) { Log.Error(ex, "Lỗi toggle Like cho Bulletin {Id}", bulletinId); }
        }

        public bool AddComment(int bulletinId, string content, string createdBy)
        {
            try
            {
                var enabledSetting = _db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Bulletin_CommentsEnabled")?.Value ?? "1";
                if (enabledSetting == "0")
                {
                    Log.Warning("[Bulletin] Bình luận đã bị vô hiệu hóa.");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(content)) return false;

                // Moderation
                var modSetting = _db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Bulletin_AutoModerationMode")?.Value ?? "0";
                var actionSetting = _db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Bulletin_ModerationAction")?.Value ?? "0";
                bool hasProfanity = HasProfanityText(content);

                if (modSetting == "1" && hasProfanity)
                {
                    if (actionSetting == "0") // Censor
                    {
                        content = FilterProfanity(content);
                    }
                    else if (actionSetting == "1") // Block
                    {
                        Log.Warning("[Bulletin] Bình luận bị chặn do chứa từ tục tĩu.");
                        return false;
                    }
                }

                var c = new BulletinComment
                {
                    BulletinId = bulletinId,
                    Content = content.Trim(),
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.Now
                };
                _db.BulletinComments.Add(c);
                _db.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi thêm bình luận cho Bulletin {Id}", bulletinId);
                return false;
            }
        }

        public List<BulletinComment> GetComments(int bulletinId)
        {
            try
            {
                return _db.BulletinComments
                          .Where(c => c.BulletinId == bulletinId)
                          .OrderByDescending(c => c.CreatedAt)
                          .ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi lấy bình luận cho Bulletin {Id}", bulletinId);
                return new List<BulletinComment>();
            }
        }

        // BUG-07 FIX: Delete a bulletin and its comments
        public void DeleteBulletin(int bulletinId)
        {
            try
            {
                var comments = _db.BulletinComments.Where(c => c.BulletinId == bulletinId).ToList();
                if (comments.Any()) _db.BulletinComments.RemoveRange(comments);

                // Phase 9: Cascade delete Read Receipts and Polls
                var receipts = _db.BulletinReadReceipts.Where(r => r.BulletinId == bulletinId).ToList();
                if (receipts.Any()) _db.BulletinReadReceipts.RemoveRange(receipts);

                var options = _db.BulletinPollOptions.Where(o => o.BulletinId == bulletinId).ToList();
                if (options.Any())
                {
                    var optIds = options.Select(o => o.Id).ToList();
                    var votes = _db.BulletinPollVotes.Where(v => optIds.Contains(v.OptionId)).ToList();
                    if (votes.Any()) _db.BulletinPollVotes.RemoveRange(votes);
                    _db.BulletinPollOptions.RemoveRange(options);
                }

                var approvals = _db.BulletinApprovalRequests.Where(a => a.BulletinId == bulletinId).ToList();
                if (approvals.Any()) _db.BulletinApprovalRequests.RemoveRange(approvals);

                var b = _db.Bulletins.FirstOrDefault(x => x.Id == bulletinId);
                if (b != null)
                {
                    _db.Bulletins.Remove(b);
                    _db.SaveChanges();
                    Log.Information("[Bulletin] Deleted bulletin #{Id}: '{Title}'", bulletinId, b.Title);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi xóa bảng tin {Id}", bulletinId);
                throw;
            }
        }

        // --- PHASE 9: Helper and Moderation Methods ---
        public string FilterProfanity(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var profanities = new[] { "dm", "vl", "dcm", "cc", "ngu", "fuck", "cl", "cac", "lon", "hate", "chửi", "đánh", "bắt nạt", "scam", "dốt" };
            string censored = text;
            foreach (var word in profanities)
            {
                censored = System.Text.RegularExpressions.Regex.Replace(
                    censored,
                    @"\b" + System.Text.RegularExpressions.Regex.Escape(word) + @"\b",
                    "***",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );
            }
            return censored;
        }

        private bool HasProfanityText(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            var profanities = new[] { "dm", "vl", "dcm", "cc", "ngu", "fuck", "cl", "cac", "lon", "hate", "chửi", "đánh", "bắt nạt", "scam", "dốt" };
            foreach (var word in profanities)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(
                    text,
                    @"\b" + System.Text.RegularExpressions.Regex.Escape(word) + @"\b",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public bool VerifyAttachmentSignature(string filePath, string signature, string level)
        {
            try
            {
                if (!System.IO.File.Exists(filePath)) return false;
                byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);

                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    byte[] hashBytes = md5.ComputeHash(fileBytes);
                    string md5Hash = Convert.ToHexString(hashBytes).ToLower();

                    if (level == "1") // MD5 check
                    {
                        return md5Hash == signature.Trim().ToLower();
                    }
                    else if (level == "2") // Strict HMAC signature
                    {
                        var secretKey = _db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Bulletin_SecretKey")?.Value ?? "SmartClass_Secret_Key_2026";
                        using (var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secretKey)))
                        {
                            byte[] signatureBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(md5Hash));
                            string expectedSig = Convert.ToHexString(signatureBytes).ToLower();
                            return expectedSig == signature.Trim().ToLower();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error verifying attachment signature");
            }
            return false;
        }

        // --- PHASE 9: Read Receipts ---
        public bool MarkAsRead(int bulletinId, string userId)
        {
            try
            {
                var exists = _db.BulletinReadReceipts.Any(x => x.BulletinId == bulletinId && x.UserId == userId);
                if (exists) return true;

                var receipt = new BulletinReadReceipt
                {
                    BulletinId = bulletinId,
                    UserId = userId,
                    ReadAt = DateTime.Now
                };
                _db.BulletinReadReceipts.Add(receipt);
                _db.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi ghi nhận đọc bản tin {Id}", bulletinId);
                return false;
            }
        }

        public List<string> GetReadReceiptUsers(int bulletinId)
        {
            try
            {
                return _db.BulletinReadReceipts
                           .Where(x => x.BulletinId == bulletinId)
                           .Select(x => x.UserId)
                           .ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi lấy danh sách người đọc bản tin {Id}", bulletinId);
                return new List<string>();
            }
        }

        // --- PHASE 9: Polls & Surveys ---
        public bool CreatePoll(int bulletinId, List<string> options)
        {
            try
            {
                if (options == null || !options.Any()) return false;
                foreach (var optText in options)
                {
                    if (string.IsNullOrWhiteSpace(optText)) continue;
                    var opt = new BulletinPollOption
                    {
                        BulletinId = bulletinId,
                        OptionText = optText.Trim(),
                        VotesCount = 0
                    };
                    _db.BulletinPollOptions.Add(opt);
                }
                _db.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi tạo khảo sát cho bản tin {Id}", bulletinId);
                return false;
            }
        }

        public bool VotePollOption(int optionId, string userId)
        {
            try
            {
                var option = _db.BulletinPollOptions.FirstOrDefault(o => o.Id == optionId);
                if (option == null) return false;

                // Check double-voting
                var allOptionIds = _db.BulletinPollOptions
                                      .Where(o => o.BulletinId == option.BulletinId)
                                      .Select(o => o.Id)
                                      .ToList();

                var alreadyVoted = _db.BulletinPollVotes
                                      .Any(v => allOptionIds.Contains(v.OptionId) && v.UserId == userId);

                if (alreadyVoted)
                {
                    Log.Warning("[Bulletin] User {User} already voted in poll for bulletin {Id}", userId, option.BulletinId);
                    return false;
                }

                using (var transaction = _db.Database.BeginTransaction())
                {
                    var vote = new BulletinPollVote
                    {
                        OptionId = optionId,
                        UserId = userId,
                        VotedAt = DateTime.Now
                    };
                    _db.BulletinPollVotes.Add(vote);

                    option.VotesCount++;
                    _db.SaveChanges();
                    transaction.Commit();
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi ghi nhận bình chọn cho Option {Id}", optionId);
                return false;
            }
        }

        public List<BulletinPollOption> GetPollOptions(int bulletinId)
        {
            try
            {
                return _db.BulletinPollOptions
                           .Where(o => o.BulletinId == bulletinId)
                           .ToList();
            }
            catch
            {
                return new List<BulletinPollOption>();
            }
        }

        public int AnalyzeContentSafety(string title, string content)
        {
            if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(content))
                return 100;

            int score = 100;
            string combined = ((title ?? "") + " " + (content ?? "")).ToLower();

            // Highly toxic words (-30 each)
            var highToxic = new[] { "dm", "vl", "fuck", "cl", "cac", "lon" };
            foreach (var word in highToxic)
            {
                if (combined.Contains(word))
                {
                    score -= 30;
                }
            }

            // Moderately toxic words (-15 each)
            var modToxic = new[] { "hate", "chửi", "đánh", "bắt nạt", "scam", "ngu", "dốt" };
            foreach (var word in modToxic)
            {
                if (combined.Contains(word))
                {
                    score -= 15;
                }
            }

            if (score < 0) score = 0;
            return score;
        }

        public bool SubmitForApproval(int bulletinId)
        {
            try
            {
                var b = _db.Bulletins.FirstOrDefault(x => x.Id == bulletinId);
                if (b == null) return false;

                var workflow = _db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Bulletin_ApprovalWorkflow")?.Value ?? "2";
                if (workflow == "0") return false;

                // Clear old requests
                var old = _db.BulletinApprovalRequests.Where(r => r.BulletinId == bulletinId).ToList();
                if (old.Any())
                {
                    _db.BulletinApprovalRequests.RemoveRange(old);
                }

                if (workflow == "1")
                {
                    _db.BulletinApprovalRequests.Add(new BulletinApprovalRequest
                    {
                        BulletinId = bulletinId,
                        Step = 1,
                        ApproverId = "Manager",
                        Status = "Pending",
                        Comment = ""
                    });
                }
                else if (workflow == "2")
                {
                    _db.BulletinApprovalRequests.Add(new BulletinApprovalRequest
                    {
                        BulletinId = bulletinId,
                        Step = 1,
                        ApproverId = "Manager",
                        Status = "Pending",
                        Comment = ""
                    });
                    _db.BulletinApprovalRequests.Add(new BulletinApprovalRequest
                    {
                        BulletinId = bulletinId,
                        Step = 2,
                        ApproverId = "HT001",
                        Status = "Pending",
                        Comment = ""
                    });
                }

                _db.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi đệ trình phê duyệt cho Bulletin {Id}", bulletinId);
                return false;
            }
        }

        public bool ProcessApprovalAction(int requestId, string approverId, string action, string comment)
        {
            try
            {
                var req = _db.BulletinApprovalRequests.FirstOrDefault(x => x.Id == requestId);
                if (req == null) return false;

                var bulletin = _db.Bulletins.FirstOrDefault(x => x.Id == req.BulletinId);
                if (bulletin == null) return false;

                req.Status = (action == "Approve") ? "Approved" : "Rejected";
                req.Comment = comment ?? "";
                req.ApproverId = approverId;
                req.ActionedAt = DateTime.Now;

                if (action == "Reject")
                {
                    bulletin.Status = "Rejected";
                    var otherRequests = _db.BulletinApprovalRequests
                                          .Where(r => r.BulletinId == req.BulletinId && r.Id != req.Id)
                                          .ToList();
                    foreach (var o in otherRequests)
                    {
                        o.Status = "Rejected";
                        o.ActionedAt = DateTime.Now;
                    }
                }
                else if (action == "Approve")
                {
                    var workflow = _db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Bulletin_ApprovalWorkflow")?.Value ?? "2";
                    string approvedStatus = (bulletin.IsScheduled && bulletin.ScheduledAt.HasValue && bulletin.ScheduledAt.Value > DateTime.Now) ? "Scheduled" : "Published";

                    if (workflow == "1")
                    {
                        bulletin.Status = approvedStatus;
                    }
                    else if (workflow == "2")
                    {
                        if (req.Step == 2)
                        {
                            var step1 = _db.BulletinApprovalRequests
                                           .FirstOrDefault(r => r.BulletinId == req.BulletinId && r.Step == 1);
                            if (step1 == null || step1.Status == "Approved")
                            {
                                bulletin.Status = approvedStatus;
                            }
                        }
                        else if (req.Step == 1)
                        {
                            var step2 = _db.BulletinApprovalRequests
                                           .FirstOrDefault(r => r.BulletinId == req.BulletinId && r.Step == 2);
                            if (step2 != null && step2.Status == "Approved")
                            {
                                bulletin.Status = approvedStatus;
                            }
                        }
                    }
                }

                _db.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi khi xử lý duyệt tin requestId={Id}", requestId);
                return false;
            }
        }

        public List<BulletinApprovalRequest> GetPendingApprovals(string approverId)
        {
            try
            {
                if (approverId == "HT001" || approverId == "HT")
                {
                    var step2Requests = _db.BulletinApprovalRequests
                              .Where(r => r.Step == 2 && r.Status == "Pending")
                              .ToList();

                    // BGH chỉ duyệt khi sơ duyệt của Phụ trách bảng tin (Step 1) đã được duyệt (Approved)
                    return step2Requests.Where(r => {
                        var step1 = _db.BulletinApprovalRequests.FirstOrDefault(x => x.BulletinId == r.BulletinId && x.Step == 1);
                        return step1 == null || step1.Status == "Approved";
                    }).ToList();
                }
                else
                {
                    return _db.BulletinApprovalRequests
                              .Where(r => r.Step == 1 && r.Status == "Pending")
                              .ToList();
                }
            }
            catch
            {
                return new List<BulletinApprovalRequest>();
            }
        }

        public List<BulletinApprovalRequest> GetApprovalHistory(int bulletinId)
        {
            try
            {
                return _db.BulletinApprovalRequests
                          .Where(r => r.BulletinId == bulletinId)
                          .OrderBy(r => r.Step)
                          .ToList();
            }
            catch
            {
                return new List<BulletinApprovalRequest>();
            }
        }
    }
}

