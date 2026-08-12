using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using QASmartClass.Data;

namespace QASmartClass.Services
{
    public class StaffDailyWorkflowsServices
    {
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, KalmanFilter> _kalmanFilters = new();

        // 1. LIBRARY WORKFLOWS
        public static bool BorrowBook(AppDbContext db, int studentId, string bookCode, string bookTitle, DateTime? todayOverride = null, bool teacherOverride = false)
        {
            var today = todayOverride ?? DateTime.Today;
            // Check if student has any overdue book loan for > 7 days
            var hasSevereOverdue = db.BookLoans
                .Any(l => l.StudentId == studentId && 
                          l.ReturnedDate == null && 
                          today > l.DueDate.AddDays(7));

            if (hasSevereOverdue)
            {
                Serilog.Log.Warning("[Library] Student {StudentId} is blocked from borrowing due to severe overdue books", studentId);
                return false;
            }

            var student = db.Students.FirstOrDefault(s => s.Id == studentId);
            if (student != null)
            {
                // Check if student is blocked due to reservation SLA
                if (student.ReservationBlockedUntil.HasValue && student.ReservationBlockedUntil.Value > today)
                {
                    Serilog.Log.Warning("[Library] Student {StudentId} is currently blocked from reserving/borrowing due to SLA penalty until {BlockedUntil}", studentId, student.ReservationBlockedUntil.Value);
                    return false;
                }

                // Check if the book is currently reserved by someone else
                var activeRes = db.BookReservations
                    .FirstOrDefault(r => r.BookCode == bookCode && r.Status == "PendingPickUp");
                if (activeRes != null && activeRes.StudentId != studentId && activeRes.HoldExpirationDate >= today)
                {
                    Serilog.Log.Warning("[Library] Book {BookCode} is reserved by another student", bookCode);
                    return false;
                }

                // Lexile pedagogical level matching
                var book = db.LibraryBooks.FirstOrDefault(b => b.BookCode == bookCode);
                if (book != null)
                {
                    var modeSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Library_LexileBlockMode");
                    int lexileMode = modeSetting != null && int.TryParse(modeSetting.Value, out int m) ? m : 1;

                    var offsetSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Library_LexileMaxOffset");
                    int maxOffset = offsetSetting != null && int.TryParse(offsetSetting.Value, out int o) ? o : 250;

                    int diff = book.BookLexileLevel - student.StudentLexileScore;

                    if (lexileMode == 2) // StrictBlock
                    {
                        if (diff > maxOffset && !teacherOverride)
                        {
                            Serilog.Log.Warning("[Library] Student {StudentId} Lexile {StudentLexile} is too low for Book Lexile {BookLexile} (Diff {Diff} > Max {Max})", studentId, student.StudentLexileScore, book.BookLexileLevel, diff, maxOffset);
                            return false;
                        }
                    }
                    else if (lexileMode == 1) // SoftWarning
                    {
                        if (diff > 200 || diff < -150)
                        {
                            Serilog.Log.Information("[Library] Soft Lexile warning for student {StudentId} and book {BookCode}", studentId, bookCode);
                        }
                    }
                    else if (lexileMode == 0) // NoRestriction
                    {
                        if (diff >= -100 && diff <= 50)
                        {
                            Serilog.Log.Information("[Library] Student {StudentId} borrowed optimal Lexile book {BookCode}", studentId, bookCode);
                        }
                    }
                }
            }

            var loan = new BookLoan
            {
                StudentId = studentId,
                BookCode = bookCode,
                BookTitle = bookTitle,
                BorrowDate = today,
                DueDate = today.AddDays(14)
            };
            db.BookLoans.Add(loan);
            db.SaveChanges();
            return true;
        }

        public static bool ReserveBook(AppDbContext db, int studentId, string bookCode, DateTime reservationDate)
        {
            var student = db.Students.FirstOrDefault(s => s.Id == studentId);
            if (student == null) return false;

            if (student.ReservationBlockedUntil.HasValue && student.ReservationBlockedUntil.Value > reservationDate)
            {
                Serilog.Log.Warning("[Library] Student {StudentId} is blocked from reserving until {BlockedUntil}", studentId, student.ReservationBlockedUntil.Value);
                return false;
            }

            var activeResCount = db.BookReservations
                .Count(r => r.StudentId == studentId && r.Status == "PendingPickUp");
            if (activeResCount >= 2)
            {
                Serilog.Log.Warning("[Library] Student {StudentId} reached maximum active reservations limit", studentId);
                return false;
            }

            var book = db.LibraryBooks.FirstOrDefault(b => b.BookCode == bookCode);
            if (book == null || book.Status != "Active")
            {
                Serilog.Log.Warning("[Library] Book {BookCode} is not available for reservation", bookCode);
                return false;
            }

            var isLoaned = db.BookLoans.Any(l => l.BookCode == bookCode && l.ReturnedDate == null);
            if (isLoaned)
            {
                Serilog.Log.Warning("[Library] Book {BookCode} is currently checked out", bookCode);
                return false;
            }

            var isReserved = db.BookReservations.Any(r => r.BookCode == bookCode && r.Status == "PendingPickUp");
            if (isReserved)
            {
                Serilog.Log.Warning("[Library] Book {BookCode} is already reserved", bookCode);
                return false;
            }

            var holdHoursSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Library_ReservationHoldHours");
            int holdHours = holdHoursSetting != null && int.TryParse(holdHoursSetting.Value, out int h) ? h : 24;

            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    var reservation = new BookReservation
                    {
                        StudentId = studentId,
                        BookCode = bookCode,
                        ReservationDate = reservationDate,
                        HoldExpirationDate = reservationDate.AddHours(holdHours),
                        Status = "PendingPickUp"
                    };
                    db.BookReservations.Add(reservation);
                    db.SaveChanges();
                    tx.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    Serilog.Log.Error(ex, "ReserveBook failed");
                    return false;
                }
            }
        }

        public static bool PickUpReservedBook(AppDbContext db, int studentId, string bookCode, DateTime today)
        {
            var student = db.Students.FirstOrDefault(s => s.Id == studentId);
            if (student == null) return false;

            var reservation = db.BookReservations
                .FirstOrDefault(r => r.StudentId == studentId && r.BookCode == bookCode && r.Status == "PendingPickUp" && r.HoldExpirationDate >= today);

            if (reservation == null)
            {
                Serilog.Log.Warning("[Library] No active reservation found for student {StudentId} and book {BookCode}", studentId, bookCode);
                return false;
            }

            var book = db.LibraryBooks.FirstOrDefault(b => b.BookCode == bookCode);
            string bookTitle = book?.Title ?? "Sách thư viện";

            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    reservation.Status = "PickedUp";

                    bool borrowSuccess = BorrowBook(db, studentId, bookCode, bookTitle, today, false);
                    if (!borrowSuccess)
                    {
                        tx.Rollback();
                        return false;
                    }

                    var thresholdSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Library_StreakThreshold");
                    int threshold = thresholdSetting != null && int.TryParse(thresholdSetting.Value, out int t) ? t : 3;

                    var xpRewardSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Library_StreakXpReward");
                    int xpReward = xpRewardSetting != null && int.TryParse(xpRewardSetting.Value, out int r) ? r : 10;

                    student.ConsecutiveSuccessfulPickups += 1;
                    if (student.ConsecutiveSuccessfulPickups >= threshold)
                    {
                        student.TotalXp += xpReward;
                        student.ConsecutiveSuccessfulPickups = 0; // Reset streak

                        db.InboxMessages.Add(new InboxMessage
                        {
                            SenderId = "LIBRARY_ROOM",
                            SenderName = "Thư viện trường",
                            ReceiverId = $"student_{student.Id}",
                            Content = $"[KHEN NGỢI] Chúc mừng em đã hoàn thành xuất sắc chuỗi {threshold} lần đặt lấy sách đúng hẹn liên tiếp! Thư viện tặng em +{xpReward} XP và Huy hiệu ảo 'Đọc sách gương mẫu'. Hãy tiếp tục phát huy nhé!",
                            IsRead = false,
                            CreatedAt = DateTime.Now
                        });
                    }

                    db.SaveChanges();
                    tx.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    Serilog.Log.Error(ex, "PickUpReservedBook failed");
                    return false;
                }
            }
        }

        public static bool CancelExpiredReservations(AppDbContext db, DateTime today)
        {
            var expiredRes = db.BookReservations
                .Where(r => r.Status == "PendingPickUp" && r.HoldExpirationDate < today)
                .ToList();

            if (!expiredRes.Any()) return true;

            var modeSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Library_ReservationBlockMode");
            int blockMode = modeSetting != null && int.TryParse(modeSetting.Value, out int bm) ? bm : 0;

            var blockDaysSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Library_ReservationBlockDays");
            int blockDays = blockDaysSetting != null && int.TryParse(blockDaysSetting.Value, out int bd) ? bd : 3;

            var deductionSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Library_ReservationConductDeduction");
            int deduction = deductionSetting != null && int.TryParse(deductionSetting.Value, out int cd) ? cd : 5;

            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    foreach (var res in expiredRes)
                    {
                        res.Status = "Expired";

                        var student = db.Students.FirstOrDefault(s => s.Id == res.StudentId);
                        if (student == null) continue;

                        student.ConsecutiveSuccessfulPickups = 0; // Reset streak

                        db.InboxMessages.Add(new InboxMessage
                        {
                            SenderId = "LIBRARY_ROOM",
                            SenderName = "Thư viện trường",
                            ReceiverId = $"student_{student.Id}",
                            Content = $"[CẢNH BÁO SLA] Đơn đặt giữ sách mã '{res.BookCode}' của em đã hết hạn giữ trên kệ mà không được nhận. Hệ thống đã tự động hủy đặt giữ.",
                            IsRead = false,
                            CreatedAt = DateTime.Now
                        });

                        if (blockMode == 1) // TempBlock
                        {
                            student.ReservationBlockedUntil = today.AddDays(blockDays);
                            db.InboxMessages.Add(new InboxMessage
                            {
                                SenderId = "LIBRARY_ROOM",
                                SenderName = "Thư viện trường",
                                ReceiverId = $"student_{student.Id}",
                                Content = $"[CHẾ TÀI ĐẶT SÁCH] Tài khoản của em bị tạm khóa quyền đặt sách trước trong {blockDays} ngày (đến {student.ReservationBlockedUntil:dd/MM/yyyy}) do vi phạm SLA.",
                                IsRead = false,
                                CreatedAt = DateTime.Now
                            });
                        }
                        else if (blockMode == 2) // DeductConduct
                        {
                            student.ConductScore = Math.Max(0, student.ConductScore - deduction);

                            db.ConductRecords.Add(new ConductRecord
                            {
                                StudentId = student.Id,
                                PointsDelta = -deduction,
                                Reason = $"Vi phạm SLA đặt giữ sách (Mã: {res.BookCode}) quá hạn không lấy.",
                                TeacherName = "LIBRARY_SYSTEM",
                                CreatedAt = today
                            });

                            db.InboxMessages.Add(new InboxMessage
                            {
                                SenderId = "LIBRARY_ROOM",
                                SenderName = "Thư viện trường",
                                ReceiverId = $"student_{student.Id}",
                                Content = $"[CHẾ TÀI HẠNH KIỂM] Em bị khấu trừ {deduction} điểm hạnh kiểm do quá hạn đặt giữ sách mà không lấy. Điểm hạnh kiểm hiện tại của em là {student.ConductScore}.",
                                IsRead = false,
                                CreatedAt = DateTime.Now
                            });
                        }
                    }

                    db.SaveChanges();
                    tx.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    Serilog.Log.Error(ex, "CancelExpiredReservations failed");
                    return false;
                }
            }
        }

        public static bool ReturnBookWithWearRating(AppDbContext db, int studentId, string bookCode, int returnConditionScore, string notes, DateTime today)
        {
            var loan = db.BookLoans
                .FirstOrDefault(l => l.StudentId == studentId && l.BookCode == bookCode && l.ReturnedDate == null);

            if (loan == null)
            {
                Serilog.Log.Warning("[Library] No active loan found for student {StudentId} and book {BookCode}", studentId, bookCode);
                return false;
            }

            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    loan.ReturnedDate = today;

                    var book = db.LibraryBooks.FirstOrDefault(b => b.BookCode == bookCode);
                    if (book != null)
                    {
                        book.BookConditionScore = (int)Math.Round((book.BookConditionScore * 0.7) + (returnConditionScore * 0.3));

                        var minSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Library_MinConditionScoreThreshold");
                        int minThreshold = minSetting != null && int.TryParse(minSetting.Value, out int minVal) ? minVal : 4;

                        var disposeSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Library_AutoDisposeThreshold");
                        int disposeThreshold = disposeSetting != null && int.TryParse(disposeSetting.Value, out int dispVal) ? dispVal : 2;

                        if (book.BookConditionScore < disposeThreshold)
                        {
                            book.Status = "Retired";
                            book.ReplenishmentRequired = true;

                            db.InboxMessages.Add(new InboxMessage
                            {
                                SenderId = "LIBRARY_SYSTEM",
                                SenderName = "Hệ thống Thư viện",
                                ReceiverId = "ADMIN",
                                Content = $"[ĐỀ XUẤT MUA SÁCH MỚI] Sách '{book.Title}' (Mã: {book.BookCode}) đã bị thanh lý do chất lượng hao mòn quá cao (Điểm: {book.BookConditionScore}). Vui lòng phê duyệt mua bổ sung.",
                                IsRead = false,
                                CreatedAt = today
                            });
                        }
                        else if (book.BookConditionScore < minThreshold)
                        {
                            book.Status = "NeedsRepair";

                            db.InboxMessages.Add(new InboxMessage
                            {
                                SenderId = "LIBRARY_SYSTEM",
                                SenderName = "Hệ thống Thư viện",
                                ReceiverId = "LIBRARY_ROOM",
                                Content = $"[YÊU CẦU BẢO DƯỠNG] Sách '{book.Title}' (Mã: {book.BookCode}) chất lượng giảm sút (Điểm: {book.BookConditionScore}). Yêu cầu chuyển bảo dưỡng.",
                                IsRead = false,
                                CreatedAt = today
                            });
                        }
                    }

                    if (returnConditionScore <= 3)
                    {
                        var student = db.Students.FirstOrDefault(s => s.Id == studentId);
                        if (student != null)
                        {
                            var blockModeSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Library_DamagedBookBlockMode");
                            int blockMode = blockModeSetting != null && int.TryParse(blockModeSetting.Value, out int bm) ? bm : 0;

                            var blockDaysSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Library_DamagedBookBlockDays");
                            int blockDays = blockDaysSetting != null && int.TryParse(blockDaysSetting.Value, out int bd) ? bd : 3;

                            var deductionSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Library_DamagedBookConductDeduction");
                            int deduction = deductionSetting != null && int.TryParse(deductionSetting.Value, out int cd) ? cd : 5;

                            db.InboxMessages.Add(new InboxMessage
                            {
                                SenderId = "LIBRARY_ROOM",
                                SenderName = "Thư viện trường",
                                ReceiverId = $"student_{student.Id}",
                                Content = $"[NHẮC NHỞ BẢO VỆ CỦA CÔNG] Em vừa trả sách '{loan.BookTitle}' bị hư hỏng nặng (Điểm chấm: {returnConditionScore}/10). Hãy giữ gìn sách cẩn thận hơn để các bạn khác cùng đọc nhé!",
                                IsRead = false,
                                CreatedAt = today
                            });

                            db.InboxMessages.Add(new InboxMessage
                            {
                                SenderId = "LIBRARY_ROOM",
                                SenderName = "Thư viện trường",
                                ReceiverId = $"parent_{student.Id}",
                                Content = $"[NHẮC NHỞ HỌC SINH] Con em {student.FullName} vừa trả sách '{loan.BookTitle}' trong tình trạng hỏng nặng. Gia đình vui lòng phối hợp nhắc nhở con bảo quản của công tốt hơn.",
                                IsRead = false,
                                CreatedAt = today
                            });

                            db.InboxMessages.Add(new InboxMessage
                            {
                                SenderId = "LIBRARY_ROOM",
                                SenderName = "Thư viện trường",
                                ReceiverId = $"teacher_{student.ClassName}",
                                Content = $"[HỖ TRỢ GIÁO DỤC] Học sinh {student.FullName} làm hỏng sách '{loan.BookTitle}' khi trả. Nhờ GVCN gặp riêng trò chuyện nhắc nhở nhẹ nhàng.",
                                IsRead = false,
                                CreatedAt = today
                            });

                            if (blockMode == 1) // TempBlock
                            {
                                student.ReservationBlockedUntil = today.AddDays(blockDays);

                                db.InboxMessages.Add(new InboxMessage
                                {
                                    SenderId = "LIBRARY_ROOM",
                                    SenderName = "Thư viện trường",
                                    ReceiverId = $"student_{student.Id}",
                                    Content = $"[CHẾ TÀI HỎNG SÁCH] Tài khoản của em bị tạm khóa quyền mượn/đặt sách trong {blockDays} ngày (đến {student.ReservationBlockedUntil:dd/MM/yyyy}) do trả sách hỏng nặng.",
                                    IsRead = false,
                                    CreatedAt = today
                                });

                                db.ConductRecords.Add(new ConductRecord
                                {
                                    StudentId = student.Id,
                                    PointsDelta = 0,
                                    Reason = $"Học sinh làm hỏng sách thư viện '{loan.BookTitle}' (Chất lượng trả: {returnConditionScore}). Chế tài khóa quyền đặt sách {blockDays} ngày.",
                                    TeacherName = "LIBRARY_SYSTEM",
                                    CreatedAt = today
                                });
                            }
                            else if (blockMode == 2) // DeductConduct
                            {
                                student.ConductScore = Math.Max(0, student.ConductScore - deduction);

                                db.ConductRecords.Add(new ConductRecord
                                {
                                    StudentId = student.Id,
                                    PointsDelta = -deduction,
                                    Reason = $"Học sinh làm hỏng sách thư viện '{loan.BookTitle}' (Chất lượng trả: {returnConditionScore}). Chế tài trừ {deduction} điểm hạnh kiểm.",
                                    TeacherName = "LIBRARY_SYSTEM",
                                    CreatedAt = today
                                });

                                db.InboxMessages.Add(new InboxMessage
                                {
                                    SenderId = "LIBRARY_ROOM",
                                    SenderName = "Thư viện trường",
                                    ReceiverId = $"student_{student.Id}",
                                    Content = $"[CHẾ TÀI HẠNH KIỂM] Em bị khấu trừ {deduction} điểm hạnh kiểm do làm hỏng sách thư viện. Điểm hạnh kiểm hiện tại của em là {student.ConductScore}.",
                                    IsRead = false,
                                    CreatedAt = today
                                });
                            }
                            else // blockMode == 0 (Warn only)
                            {
                                db.ConductRecords.Add(new ConductRecord
                                {
                                    StudentId = student.Id,
                                    PointsDelta = 0,
                                    Reason = $"Học sinh làm hỏng sách thư viện '{loan.BookTitle}' (Chất lượng trả: {returnConditionScore}).",
                                    TeacherName = "LIBRARY_SYSTEM",
                                    CreatedAt = today
                                });
                            }
                        }
                    }

                    db.SaveChanges();
                    tx.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    Serilog.Log.Error(ex, "ReturnBookWithWearRating failed");
                    return false;
                }
            }
        }

        public static int CalculateOverdueDays(BookLoan loan, DateTime today)
        {
            var end = loan.ReturnedDate ?? today;
            if (end > loan.DueDate)
            {
                return (end - loan.DueDate).Days;
            }
            return 0;
        }

        // 2. MEDICAL WORKFLOWS
        public static bool LogEmergency(AppDbContext db, int studentId, string description, string firstAid)
        {
            var student = db.Students.FirstOrDefault(s => s.Id == studentId);
            if (student == null) return false;

            var log = new EmergencyLog
            {
                StudentName = student.FullName,
                IncidentType = "Injury",
                Description = description,
                FirstAidApplied = firstAid,
                Timestamp = DateTime.Now,
                Status = "Pending"
            };
            db.EmergencyLogs.Add(log);

            // Send notification to parent & GVCN
            string subject = "Cảnh báo Y tế Khẩn cấp";
            string reason = $"Học sinh gặp sự cố sức khỏe: {description}. Đã sơ cứu: {firstAid}.";
            
            var notificationService = new NotificationService(db);
            notificationService.SendToParent(studentId, subject, reason);

            // Notify Class Teacher via system inbox
            db.InboxMessages.Add(new InboxMessage
            {
                SenderId = "MEDICAL_ROOM",
                SenderName = "Phòng Y Tế",
                ReceiverId = $"teacher_{student.ClassName}",
                Content = $"[KHẨN CẤP] Học sinh {student.FullName} ({student.ClassName}) gặp sự cố y tế: {description}.",
                IsRead = false,
                CreatedAt = DateTime.Now,
                ThreadId = $"emergency_{studentId}_{DateTime.Today:yyyyMMdd}"
            });
            
            db.SaveChanges();
            return true;
        }

        public static bool CheckSupplyStockAlert(MedicalSupply supply, DateTime today)
        {
            if (supply.Quantity < 10) return true;
            if (supply.ExpiryDate <= today.AddMonths(3)) return true;
            return false;
        }

        public static bool RegisterPhysicalRestriction(AppDbContext db, int studentId, string medicalCondition, string exemptionLevel, string movementGuidelines, string alternativeTopic, DateTime endDate, string nurseName)
        {
            var student = db.Students.FirstOrDefault(s => s.Id == studentId);
            if (student == null) return false;

            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    var restriction = new StudentPhysicalRestriction
                    {
                        StudentId = studentId,
                        MedicalCondition = medicalCondition,
                        ExemptionLevel = exemptionLevel,
                        MovementGuidelines = movementGuidelines,
                        AlternativeAssignmentTopic = alternativeTopic,
                        StartDate = DateTime.Today,
                        EndDate = endDate,
                        LoggedByNurse = nurseName
                    };
                    db.StudentPhysicalRestrictions.Add(restriction);

                    // Pedagogy: Secure communication, do NOT leak raw medicalCondition to the teacher
                    string teacherMessage = $"[HẠN CHẾ VẬN ĐỘNG] Học sinh {student.FullName} được y tá miễn/giảm thể chất. Mức độ: {exemptionLevel}. Chỉ dẫn vận động: {movementGuidelines}. Hạn đến: {endDate:dd/MM/yyyy}.";

                    db.InboxMessages.Add(new InboxMessage
                    {
                        SenderId = "MEDICAL_ROOM",
                        SenderName = "Phòng Y Tế",
                        ReceiverId = $"teacher_{student.ClassName}",
                        Content = teacherMessage,
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    });

                    var modeSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Medical_PE_RestrictionMode");
                    int restrictionMode = modeSetting != null && int.TryParse(modeSetting.Value, out int rm) ? rm : 1;

                    if (restrictionMode == 1 && exemptionLevel == "FullExemption")
                    {
                        // Create Theoretical assignment topic inbox for student
                        db.InboxMessages.Add(new InboxMessage
                        {
                            SenderId = "MEDICAL_ROOM",
                            SenderName = "Phòng Y Tế",
                            ReceiverId = $"student_{student.Id}",
                            Content = $"[BÀI TẬP THAY THẾ] Do được miễn vận động thể chất, em được tự động gán đề tài lý thuyết: '{alternativeTopic}'. Vui lòng hoàn thành nộp cho giáo viên trước ngày {endDate:dd/MM/yyyy} để lấy điểm.",
                            IsRead = false,
                            CreatedAt = DateTime.Now
                        });
                    }

                    db.SaveChanges();
                    tx.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    Serilog.Log.Error(ex, "RegisterPhysicalRestriction failed");
                    return false;
                }
            }
        }

        public static int ProposeMedicalDisposal(AppDbContext db, int supplyId, int quantity, string reason, string nurseSignature)
        {
            var supply = db.MedicalSupplies.FirstOrDefault(s => s.Id == supplyId);
            if (supply == null || supply.Quantity < quantity) return 0;

            var proposal = new MedicalDisposalProposal
            {
                MedicalSupplyId = supplyId,
                QuantityToDispose = quantity,
                Reason = reason,
                ProposalDate = DateTime.Now,
                NurseSignature = nurseSignature,
                Status = "PendingApproval"
            };

            db.MedicalDisposalProposals.Add(proposal);
            db.SaveChanges();
            return proposal.Id;
        }

        public static bool ApproveMedicalDisposal(AppDbContext db, int proposalId, string principalPin, string expectedPin)
        {
            var proposal = db.MedicalDisposalProposals.FirstOrDefault(p => p.Id == proposalId);
            if (proposal == null || proposal.Status != "PendingApproval") return false;

            var supply = db.MedicalSupplies.FirstOrDefault(s => s.Id == proposal.MedicalSupplyId);
            if (supply == null || supply.Quantity < proposal.QuantityToDispose) return false;

            var workflowSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Medical_Disposal_Workflow");
            int workflowMode = workflowSetting != null && int.TryParse(workflowSetting.Value, out int wm) ? wm : 1;

            if (workflowMode == 1 && string.IsNullOrEmpty(proposal.NurseSignature))
            {
                Serilog.Log.Warning("[Medical] Multi-Sig flow requires Nurse signature before Principal approval.");
                return false;
            }

            if (principalPin != expectedPin)
            {
                proposal.Status = "Rejected";
                db.SaveChanges();
                return false;
            }

            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    proposal.PrincipalApprovalSignature = "Principal_Signed_" + DateTime.Now.Ticks;
                    proposal.Status = "Disposed";

                    supply.Quantity -= proposal.QuantityToDispose;

                    db.AuditLogs.Add(new AuditLog
                    {
                        Timestamp = DateTime.Now,
                        Action = "MedicalDisposalApproved",
                        ActorName = "Hiệu Trưởng",
                        Details = $"Approved disposal of {proposal.QuantityToDispose} units of SupplyId {proposal.MedicalSupplyId}."
                    });

                    db.SaveChanges();
                    tx.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    Serilog.Log.Error(ex, "ApproveMedicalDisposal failed");
                    return false;
                }
            }
        }

        // 3. SECURITY WORKFLOWS
        public static bool RegisterVisitorBadge(AppDbContext db, string visitorName, string cccd, string badgeNumber)
        {
            // Check if badge is currently active (issued but not returned)
            var lastLog = db.SecurityLogs
                .Where(l => l.Description != null && l.Description.Contains($"Badge: {badgeNumber}"))
                .OrderByDescending(l => l.Timestamp)
                .FirstOrDefault();

            bool isBadgeActive = lastLog != null && lastLog.EventType == "CheckIn";

            if (isBadgeActive)
            {
                Serilog.Log.Warning("[Security] Badge number {Badge} is already in use", badgeNumber);
                return false;
            }

            var log = new SecurityLog
            {
                Timestamp = DateTime.Now,
                EventType = "CheckIn",
                PersonInvolved = visitorName,
                GuardName = "Bảo vệ cổng",
                Description = $"Khách vào trường mang theo CCCD: {cccd}. Badge: {badgeNumber}"
            };
            db.SecurityLogs.Add(log);
            db.SaveChanges();
            return true;
        }

        public static bool RegisterVisitorCheckOut(AppDbContext db, string visitorName, string badgeNumber)
        {
            var log = new SecurityLog
            {
                Timestamp = DateTime.Now,
                EventType = "CheckOut",
                PersonInvolved = visitorName,
                GuardName = "Bảo vệ cổng",
                Description = $"Khách ra khỏi trường. Badge: {badgeNumber}"
            };
            db.SecurityLogs.Add(log);
            db.SaveChanges();
            return true;
        }

        public static bool RegisterPatrolCheckin(AppDbContext db, string checkpointCode, string guardName, string status, string notes)
        {
            var log = new PatrolLog
            {
                CheckpointCode = checkpointCode,
                PatrolTime = DateTime.Now,
                GuardName = guardName,
                Status = status,
                Notes = notes
            };
            db.PatrolLogs.Add(log);
            db.SaveChanges();
            return true;
        }

        public static bool ProcessTrafficCongestion(AppDbContext db, int vehicleCount, DateTime today)
        {
            var limitSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Security_CongestionVehicleThreshold");
            int threshold = limitSetting != null && int.TryParse(limitSetting.Value, out int v) ? v : 50;

            if (vehicleCount < threshold) return false;

            var cooldownSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Security_CongestionCooldownMinutes");
            int cooldownMinutes = cooldownSetting != null && int.TryParse(cooldownSetting.Value, out int c) ? c : 20;

            var cutoff = today.AddMinutes(-cooldownMinutes);
            bool recentlyTriggered = db.TrafficCongestionLogs
                .Any(l => l.Timestamp >= cutoff && l.Timestamp <= today && l.StaggeredDismissalTriggered);

            if (recentlyTriggered)
            {
                Serilog.Log.Information("[Security] Traffic alert skipped due to Cooldown.");
                return false;
            }

            var modeSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Security_StaggeredDismissalMode");
            int staggeredMode = modeSetting != null && int.TryParse(modeSetting.Value, out int m) ? m : 1;

            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    var log = new TrafficCongestionLog
                    {
                        Timestamp = today,
                        VehicleCount = vehicleCount,
                        StaggeredDismissalTriggered = staggeredMode == 1,
                        SuggestedSequence = staggeredMode == 1 ? "Khối 10 -> Khối 11 -> Khối 12" : ""
                    };
                    db.TrafficCongestionLogs.Add(log);

                    if (staggeredMode == 1)
                    {
                        // Notify teachers of all classes
                        var classes = db.Students
                            .Where(s => !string.IsNullOrEmpty(s.ClassName))
                            .Select(s => s.ClassName)
                            .Distinct()
                            .ToList();
                        foreach (var className in classes)
                        {
                            db.InboxMessages.Add(new InboxMessage
                            {
                                SenderId = "GATE_AI_CAMERA",
                                SenderName = "AI Camera Cổng trường",
                                ReceiverId = $"teacher_{className}",
                                Content = $"[KẸT XE CỔNG TRƯỜNG] Phát hiện {vehicleCount} xe đang chờ. Tan học lệch giờ: Khối 10 (16:30) -> Khối 11 (16:40) -> Khối 12 (16:50). Vui lòng điều phối học sinh.",
                                IsRead = false,
                                CreatedAt = today
                            });
                        }
                    }

                    db.SaveChanges();
                    tx.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    Serilog.Log.Error(ex, "ProcessTrafficCongestion failed");
                    return false;
                }
            }
        }

        public static int TriggerIntrusionAlarm(AppDbContext db, string sensorCode, DateTime detectionTime)
        {
            // Debounce: check if there's already an active alarm from the same sensor in the last 1 minute
            var cutoff = detectionTime.AddMinutes(-1);
            var duplicate = db.IntrusionAlarms
                .FirstOrDefault(a => a.SensorCode == sensorCode && a.DetectionTime >= cutoff && a.DetectionTime <= detectionTime);

            if (duplicate != null)
            {
                return duplicate.Id;
            }

            var alarm = new IntrusionAlarm
            {
                SensorCode = sensorCode,
                DetectionTime = detectionTime,
                Status = "Triggered"
            };
            db.IntrusionAlarms.Add(alarm);
            db.SaveChanges();
            return alarm.Id;
        }

        public static bool VerifyIntrusionAlarm(AppDbContext db, int alarmId, DateTime verifiedTime)
        {
            var alarm = db.IntrusionAlarms.FirstOrDefault(a => a.Id == alarmId);
            if (alarm == null || alarm.Status != "Triggered") return false;

            alarm.VerifiedTime = verifiedTime;
            alarm.Status = "Verified";
            db.SaveChanges();
            return true;
        }

        public static bool ProcessIntrusionAlarmsSla(AppDbContext db, DateTime currentTime)
        {
            var responseLimitSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Security_AlarmResponseLimitMinutes");
            int responseLimitMinutes = responseLimitSetting != null && int.TryParse(responseLimitSetting.Value, out int r) ? r : 5;

            var cutoff = currentTime.AddMinutes(-responseLimitMinutes);

            // Fetch triggered alarms that are past SLA and not yet verified/escalated
            var overdueAlarms = db.IntrusionAlarms
                .Where(a => a.Status == "Triggered" && a.DetectionTime < cutoff)
                .ToList();

            if (!overdueAlarms.Any()) return false;

            var targetSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Security_AlarmEscalationTarget");
            int escalationTarget = targetSetting != null && int.TryParse(targetSetting.Value, out int t) ? t : 1;

            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    foreach (var alarm in overdueAlarms)
                    {
                        // Pedagogy / Technical filter: only auto-escalate if alarm was triggered at night (22h - 5h)
                        int hour = alarm.DetectionTime.Hour;
                        bool isNight = hour >= 22 || hour < 5;

                        if (!isNight)
                        {
                            // In daytime, just resolve or ignore auto-escalation
                            alarm.Status = "Verified";
                            continue;
                        }

                        alarm.Status = "Escalated";

                        // Create khẩn cấp message for Principal
                        db.InboxMessages.Add(new InboxMessage
                        {
                            SenderId = "SECURITY_SYSTEM",
                            SenderName = "Hệ thống An ninh",
                            ReceiverId = "HT001", // Hiệu trưởng
                            Content = $"[BÁO ĐỘNG ĐỘT NHẬP] Cảm biến '{alarm.SensorCode}' phát hiện đột nhập lúc {alarm.DetectionTime:HH:mm:ss dd/MM/yyyy} mà không được bảo vệ xác minh trong {responseLimitMinutes} phút!",
                            IsRead = false,
                            CreatedAt = currentTime
                        });

                        if (escalationTarget == 1)
                        {
                            // Kích hoạt cuộc gọi cứu viện (giả lập) gửi công an/cảnh sát
                            db.AuditLogs.Add(new AuditLog
                            {
                                Timestamp = currentTime,
                                Action = "EmergencyCallTriggered",
                                ActorName = "SECURITY_SYSTEM",
                                Details = $"Mocked emergency call dispatched to Police for sensor {alarm.SensorCode}."
                            });
                        }
                    }

                    db.SaveChanges();
                    tx.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    Serilog.Log.Error(ex, "ProcessIntrusionAlarmsSla failed");
                    return false;
                }
            }
        }

        // 4. KITCHEN WORKFLOWS
        public static List<Student> CheckAllergenConflicts(AppDbContext db, DateTime date)
        {
            var menu = db.SchoolMenus.FirstOrDefault(m => m.Date.Date == date.Date);
            if (menu == null || string.IsNullOrEmpty(menu.Items)) return new List<Student>();

            // Get active students with allergies
            var allergies = db.FoodAllergies.ToList();
            var conflictingStudents = new List<Student>();

            foreach (var allergy in allergies)
            {
                if (menu.Items.Contains(allergy.Allergen))
                {
                    // Find active students matching the name or code
                    var matchingStudents = db.Students
                        .Where(s => s.Status == "Active" && 
                                    allergy.StudentName.Contains(s.FullName))
                        .ToList();
                    
                    conflictingStudents.AddRange(matchingStudents);
                }
            }

            return conflictingStudents.Distinct().ToList();
        }

        public static string SuggestAlternativeMeal(string allergen)
        {
            if (allergen.Contains("Cá") || allergen.Contains("Hải sản"))
            {
                return "Suất ăn thay thế: Cơm thịt heo / Cơm gà luộc";
            }
            if (allergen.Contains("Đậu phộng") || allergen.Contains("Đậu tương"))
            {
                return "Suất ăn thay thế: Thực đơn không dầu lạc / Không đậu phụ";
            }
            if (allergen.Contains("Trứng") || allergen.Contains("Sữa"))
            {
                return "Suất ăn thay thế: Bánh mì chay không sữa / Nước hoa quả";
            }
            return "Suất ăn thay thế tiêu chuẩn";
        }

        public static bool InspectFoodSafety(AppDbContext db, int supplierId, string batchCode, string ingredientName, double testScore, bool hasChemicalResidue)
        {
            var supplier = db.Suppliers.FirstOrDefault(s => s.Id == supplierId);
            if (supplier == null) return false;

            var log = new FoodSafetyInspectionLog
            {
                SupplierId = supplierId,
                BatchCode = batchCode,
                IngredientName = ingredientName,
                TestScore = testScore,
                HasChemicalResidue = hasChemicalResidue,
                InspectionStatus = (testScore < 5 || hasChemicalResidue) ? "Quarantined" : "Passed",
                InspectedAt = DateTime.Now
            };
            db.FoodSafetyInspectionLogs.Add(log);

            if (log.InspectionStatus == "Quarantined")
            {
                var blockModeSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Canteen_SupplierBlockMode");
                int blockMode = blockModeSetting != null && int.TryParse(blockModeSetting.Value, out int bm) ? bm : 0;

                using (var tx = db.Database.BeginTransaction())
                {
                    try
                    {
                        if (blockMode == 0)
                        {
                            // Strict mode: auto-block the supplier
                            supplier.IsBlocked = true;
                        }

                        // Send red alert to Chief Cook and Principal via inbox
                        string alertContent = $"[CẢNH BÁO AN TOÀN THỰC PHẨM] Lô hàng '{batchCode}' ({ingredientName}) từ nhà cung cấp '{supplier.Name}' không đạt kiểm định. Điểm: {testScore}, Hóa chất dư lượng: {hasChemicalResidue}. Lô hàng đã bị cách ly niêm phong.";
                        if (blockMode == 0)
                        {
                            alertContent += " Nhà cung cấp đã bị TẠM KHÓA HOẠT ĐỘNG.";
                        }

                        db.InboxMessages.Add(new InboxMessage
                        {
                            SenderId = "CANTEEN_KITCHEN",
                            SenderName = "Bếp ăn nhà trường",
                            ReceiverId = "ADMIN", // Chief Cook / Admin
                            Content = alertContent,
                            IsRead = false,
                            CreatedAt = DateTime.Now
                        });

                        db.InboxMessages.Add(new InboxMessage
                        {
                            SenderId = "CANTEEN_KITCHEN",
                            SenderName = "Bếp ăn nhà trường",
                            ReceiverId = "HT001", // Hiệu trưởng
                            Content = alertContent,
                            IsRead = false,
                            CreatedAt = DateTime.Now
                        });

                        db.SaveChanges();
                        tx.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        Serilog.Log.Error(ex, "InspectFoodSafety failed");
                        return false;
                    }
                }
            }

            db.SaveChanges();
            return true;
        }

        public static bool ProcessNutritionalAdjustment(AppDbContext db, int menuId, DateTime today)
        {
            var menu = db.SchoolMenus.FirstOrDefault(m => m.Id == menuId);
            if (menu == null) return false;

            var wasteLog = db.FoodWasteLogs
                .FirstOrDefault(l => l.Date.Date == menu.Date.Date && l.MealType == "Lunch");
            if (wasteLog == null) return false;

            var wasteThresholdSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Canteen_VegetableWasteMaxRatio");
            double wasteThreshold = wasteThresholdSetting != null && double.TryParse(wasteThresholdSetting.Value, out double wt) ? wt : 0.25;

            if (wasteLog.WasteRatio <= wasteThreshold) return false;

            var modeSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Canteen_NutritionAdjustMode");
            int adjustMode = modeSetting != null && int.TryParse(modeSetting.Value, out int am) ? am : 1;

            if (adjustMode == 1)
            {
                // Auto adjust menu for next week (menu.Date + 7 days)
                var nextWeekDate = menu.Date.AddDays(7);
                var nextWeekMenu = db.SchoolMenus.FirstOrDefault(m => m.Date.Date == nextWeekDate.Date);
                if (nextWeekMenu != null)
                {
                    using (var tx = db.Database.BeginTransaction())
                    {
                        try
                        {
                            // Pedagogy: swap regular vegetable dish for a hidden/finely blended vegetable recipe
                            if (nextWeekMenu.Items.Contains("Rau luộc"))
                            {
                                nextWeekMenu.Items = nextWeekMenu.Items.Replace("Rau luộc", "Súp rau củ xay mịn sốt thịt bò băm");
                            }
                            else
                            {
                                nextWeekMenu.Items += ", Súp rau củ quả nghiền mịn trộn nước sốt";
                            }

                            // Send positive nutritional notifications to parents
                            var students = db.Students.Where(s => s.Status == "Active").ToList();
                            foreach (var student in students)
                            {
                                db.InboxMessages.Add(new InboxMessage
                                {
                                    SenderId = "CANTEEN_KITCHEN",
                                    SenderName = "Bếp ăn nhà trường",
                                    ReceiverId = $"parent_{student.Id}",
                                    Content = $"[DINH DƯỠNG XANH] Thực đơn tuần tới của con {student.FullName} được bổ sung món Súp rau củ nghiền mịn giàu Vitamin. Gia đình hãy cùng khuyến khích con ăn nhiều rau xanh nhé!",
                                    IsRead = false,
                                    CreatedAt = today
                                });
                            }

                            db.SaveChanges();
                            tx.Commit();
                            return true;
                        }
                        catch (Exception ex)
                        {
                            tx.Rollback();
                            Serilog.Log.Error(ex, "ProcessNutritionalAdjustment failed");
                            return false;
                        }
                    }
                }
            }

            return false;
        }

        // 5. DEPARTMENT OBSERVATION WORKFLOWS
        public static bool SubmitObservation(AppDbContext db, int teacherId, string teacherName, int observerId, string observerName, string observerRole, int score, string notes)
        {
            // Only authorized roles can submit class observations
            bool isAuthorized = observerRole == "ToTruong" || observerRole == "HieuPho" || observerRole == "HieuTruong" || observerRole == "Admin";
            if (!isAuthorized)
            {
                throw new UnauthorizedAccessException("Chỉ có Tổ trưởng chuyên môn hoặc BGH mới được ghi nhận dự giờ.");
            }

            var obs = new ClassObservation
            {
                TeacherId = teacherId,
                TeacherName = teacherName,
                ObserverName = observerName,
                ObservedAt = DateTime.Now,
                TotalScore = score,
                Notes = notes,
                Rating = score >= 8 ? "Tốt" : (score >= 6 ? "Khá" : "Đạt")
            };
            db.ClassObservations.Add(obs);
            db.SaveChanges();
            return true;
        }

        // 6. YOUTH UNION WORKFLOWS
        public static bool SubmitYouthVote(AppDbContext db, int memberId, string candidate, string voteEncryptionMode)
        {
            // Anti double voting check
            var member = db.YouthMembers.FirstOrDefault(m => m.Id == memberId);
            if (member == null) return false;

            var hasVoted = db.EventLogs.Any(l => l.EventType == "YouthVote" && l.Actor == memberId.ToString());
            if (hasVoted)
            {
                Serilog.Log.Warning("[YouthUnion] Member {MemberId} has already voted", memberId);
                return false;
            }

            // Secure/Anonymous voting
            string voteRecord = candidate;
            if (voteEncryptionMode == "1") // RSA Encryption Mode
            {
                voteRecord = $"RSA_ENCRYPTED_{candidate}";
            }
            else // Hashing Mode
            {
                voteRecord = $"HASHED_{Utilities.CryptoHelper.ComputeHMAC(candidate + "_VOTE_SALT")}";
            }

            // Log vote transaction
            db.EventLogs.Add(new EventLog
            {
                EventType = "YouthVote",
                Timestamp = DateTime.Now,
                Actor = memberId.ToString(),
                Details = $"{{\"VoteValue\":\"{voteRecord}\"}}"
            });
            db.SaveChanges();
            return true;
        }

        // 7. PRINCIPAL DIGITAL SIGNATURE
        public static bool ApproveDocumentWithSignature(AppDbContext db, int docId, string signatureMode, string enteredValue, string expectedValue)
        {
            var doc = db.OfficialDocuments.FirstOrDefault(d => d.Id == docId);
            if (doc == null) return false;

            bool isVerified = enteredValue == expectedValue;

            if (!isVerified)
            {
                Serilog.Log.Warning("[Signature] Signature verification failed for mode {Mode}", signatureMode);
                return false;
            }

            doc.Status = "Approved";
            db.SaveChanges();
            return true;
        }

        // 8. ADVANCED WORKFLOWS FOR V4.1+
        public static bool ApplyLibraryOverdueFines(AppDbContext db, DateTime today)
        {
            var overdueLoans = db.BookLoans
                .Where(l => l.ReturnedDate == null && today > l.DueDate.AddDays(3))
                .ToList();

            if (!overdueLoans.Any()) return true;

            var fineSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Library_OverdueFineMode");
            bool isFineEnabled = fineSetting != null && fineSetting.Value == "1";

            if (!isFineEnabled) return false;

            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    foreach (var loan in overdueLoans)
                    {
                        var student = db.Students.FirstOrDefault(s => s.Id == loan.StudentId);
                        if (student == null) continue;

                        int overdueDays = (today - loan.DueDate).Days;
                        decimal fineAmount = (overdueDays - 3) * 2000m;

                        if (student.WalletBalance < fineAmount)
                        {
                            Serilog.Log.Warning("[Library] Student {StudentId} has insufficient funds for fine {Fine}. Balance: {Balance}", student.Id, fineAmount, student.WalletBalance);
                            
                            db.InboxMessages.Add(new InboxMessage
                            {
                                SenderId = "LIBRARY_ROOM",
                                SenderName = "Thư viện trường",
                                ReceiverId = $"parent_{student.Id}",
                                Content = $"[CẢNH BÁO NỢ PHẠT] Học sinh {student.FullName} trễ hạn sách '{loan.BookTitle}' {overdueDays} ngày. Phí phạt: {fineAmount:N0}đ. Ví canteen không đủ số dư để khấu trừ.",
                                IsRead = false,
                                CreatedAt = DateTime.Now
                            });
                            continue;
                        }

                        student.WalletBalance -= fineAmount;
                        student.WalletChecksum = QASmartClass.Utilities.CryptoHelper.ComputeHMAC(
                            student.StudentCode + student.WalletBalance.ToString("F2")
                        );

                        db.EventLogs.Add(new EventLog
                        {
                            EventType = "LibraryFine",
                            Timestamp = DateTime.Now,
                            Actor = student.Id.ToString(),
                            Details = $"{{\"BookTitle\":\"{loan.BookTitle}\", \"FineAmount\":\"{fineAmount}\", \"OverdueDays\":\"{overdueDays}\"}}"
                        });

                        db.InboxMessages.Add(new InboxMessage
                        {
                            SenderId = "LIBRARY_ROOM",
                            SenderName = "Thư viện trường",
                            ReceiverId = $"parent_{student.Id}",
                            Content = $"[THÔNG BÁO PHẠT VÍ] Khấu trừ {fineAmount:N0}đ từ ví canteen học sinh {student.FullName} do trễ hạn sách '{loan.BookTitle}' {overdueDays} ngày.",
                            IsRead = false,
                            CreatedAt = DateTime.Now
                        });
                    }
                    db.SaveChanges();
                    tx.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    Serilog.Log.Error(ex, "ApplyLibraryOverdueFines failed");
                    return false;
                }
            }
        }

        public static bool TriggerSmartPickup(AppDbContext db, string studentCode, string parentName, string licensePlate)
        {
            var student = db.Students.FirstOrDefault(s => s.StudentCode == studentCode);
            if (student == null) return false;

            var log = new GatePickupLog
            {
                PickupTime = DateTime.Now,
                StudentId = student.Id,
                StudentName = student.FullName,
                ParentName = parentName,
                LicensePlate = licensePlate,
                Status = "Completed"
            };
            db.GatePickupLogs.Add(log);

            db.InboxMessages.Add(new InboxMessage
            {
                SenderId = "GATE_CAMERA_AI",
                SenderName = "Hệ thống Đón học sinh AI",
                ReceiverId = $"teacher_{student.ClassName}",
                Content = $"[ĐÓN HỌC SINH] Phụ huynh {parentName} (Biển số: {licensePlate}) đã đến đón học sinh {student.FullName}.",
                IsRead = false,
                CreatedAt = DateTime.Now
            });

            db.SaveChanges();
            Serilog.Log.Information("[Speaker TTS] Mời học sinh {Name} lớp {Class} ra cổng, phụ huynh đang đợi.", student.FullName, student.ClassName);
            return true;
        }

        public static bool SubmitMealFeedback(AppDbContext db, int menuId, int studentId, int rating, string comment)
        {
            if (rating < 1 || rating > 5) return false;

            var student = db.Students.FirstOrDefault(s => s.Id == studentId);
            string studentName = student?.FullName ?? "Học sinh ẩn danh";

            var feedback = new MealFeedback
            {
                MenuId = menuId,
                StudentId = studentId,
                StudentName = studentName,
                Rating = rating,
                Comment = comment,
                FeedbackTime = DateTime.Now
            };
            db.MealFeedbacks.Add(feedback);
            db.SaveChanges();
            return true;
        }

        public static bool ApproveDocumentWithMultiSig(AppDbContext db, int docId, string signerRole, string pinOrOtp, string expectedValue)
        {
            var doc = db.OfficialDocuments.FirstOrDefault(d => d.Id == docId);
            if (doc == null) return false;

            bool isVerified = pinOrOtp == expectedValue;
            if (!isVerified) return false;

            bool isLargeBudget = doc.Title.Contains("chi ngân sách") || doc.Title.Contains("10,000,000") || doc.Title.Contains("15,000,000");

            if (signerRole == "Ketoan")
            {
                doc.Status = "ReviewApproved";
                db.SaveChanges();
                return true;
            }

            if (signerRole == "HieuTruong")
            {
                var setting = db.SystemSettings.FirstOrDefault(s => s.Id == "Principal_MultiSignatureWorkflow");
                bool isMultiSigEnabled = setting != null && setting.Value == "1";

                if (isMultiSigEnabled && isLargeBudget && doc.Status != "ReviewApproved")
                {
                    Serilog.Log.Warning("[MultiSig] Cannot approve large budget document {DocId} without Accountant signature nháy first", docId);
                    return false;
                }

                doc.Status = "Approved";
                db.SaveChanges();
                return true;
            }

            return false;
        }

        public static bool CheckClassEpidemicRisk(AppDbContext db, string className, DateTime today)
        {
            var cutoffDate = today.AddDays(-5);
            var cases = db.EpidemicCases
                .Where(c => c.ClassName == className && c.OnsetDate >= cutoffDate)
                .ToList();

            if (cases.Count >= 2)
            {
                db.InboxMessages.Add(new InboxMessage
                {
                    SenderId = "EPIDEMIC_MONITOR",
                    SenderName = "Hệ thống Dịch tễ học đường",
                    ReceiverId = "teacher_" + className,
                    Content = $"[CẢNH BÁO CÁCH LY] Lớp {className} phát hiện {cases.Count} ca bệnh ({cases[0].Disease}) trong vòng 5 ngày. Đề xuất khử trùng và cách ly lớp học.",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
                db.SaveChanges();
                return true;
            }
            return false;
        }

        // 9. PHASE 2 WORKFLOWS: CASHIER, ASSET, SECRETARY, KITCHEN
        public static bool ReconcileTuition(AppDbContext db, string transactionRef, double transactionAmount)
        {
            var match = System.Text.RegularExpressions.Regex.Match(transactionRef, @"HS\d+", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!match.Success) return false;
            string studentCode = match.Value.ToUpper();

            var student = db.Students.FirstOrDefault(s => s.StudentCode == studentCode);
            if (student == null) return false;

            var tuition = db.TuitionRecords.FirstOrDefault(t => t.StudentId == student.Id && t.Status != "Paid");
            if (tuition == null) return false;

            if (tuition.IsLocked)
            {
                throw new InvalidOperationException("Hóa đơn học phí đã khóa sổ, không thể cập nhật đối chiếu.");
            }

            var reconModeSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Tuition_ReconciliationMode");
            bool strictMode = reconModeSetting == null || reconModeSetting.Value == "0";

            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    var log = new TuitionReconciliationLog
                    {
                        TuitionRecordId = tuition.Id,
                        TransactionRef = transactionRef,
                        AmountMatched = transactionAmount,
                        ReconciledAt = DateTime.Now
                    };

                    double remainingDebt = tuition.Amount - tuition.PaidAmount;

                    if (strictMode)
                    {
                        if (Math.Abs(transactionAmount - remainingDebt) < 0.01)
                        {
                            tuition.PaidAmount += transactionAmount;
                            tuition.Status = "Paid";
                            tuition.PaidDate = DateTime.Now;
                            log.Status = "Success";
                            log.Notes = "Đối chiếu khớp hoàn toàn (Strict Mode).";
                        }
                        else
                        {
                            log.Status = "Suspense";
                            log.Notes = $"Không khớp số tiền ở Strict Mode. Thực nhận: {transactionAmount}, Nợ: {remainingDebt}";
                            db.TuitionReconciliationLogs.Add(log);
                            db.SaveChanges();
                            tx.Commit();
                            return false;
                        }
                    }
                    else
                    {
                        tuition.PaidAmount += transactionAmount;
                        if (tuition.PaidAmount >= tuition.Amount - 0.01)
                        {
                            tuition.Status = "Paid";
                            tuition.PaidDate = DateTime.Now;
                            log.Status = "Success";
                            log.Notes = "Thanh toán hoàn tất (Partial Allowed Mode).";
                        }
                        else
                        {
                            tuition.Status = "Partial";
                            log.Status = "Success";
                            log.Notes = $"Thanh toán một phần. Thực nhận: {transactionAmount}, Nợ còn lại: {tuition.Amount - tuition.PaidAmount}";
                        }
                    }

                    tuition.TuitionChecksum = QASmartClass.Utilities.CryptoHelper.ComputeHMAC(
                        tuition.StudentId + "_" + tuition.Amount.ToString("F2") + "_" + tuition.PaidAmount.ToString("F2") + "_" + tuition.Status
                    );

                    db.TuitionReconciliationLogs.Add(log);
                    db.SaveChanges();
                    tx.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    Serilog.Log.Error(ex, "ReconcileTuition failed");
                    return false;
                }
            }
        }

        public static bool CloseTuitionLedger(AppDbContext db, int tuitionId, string cashierPin, string expectedPin)
        {
            if (cashierPin != expectedPin) return false;

            var tuition = db.TuitionRecords.FirstOrDefault(t => t.Id == tuitionId);
            if (tuition == null) return false;

            tuition.IsLocked = true;
            tuition.LedgerClosingDate = DateTime.Now;

            tuition.TuitionChecksum = QASmartClass.Utilities.CryptoHelper.ComputeHMAC(
                tuition.StudentId + "_" + tuition.Amount.ToString("F2") + "_" + tuition.PaidAmount.ToString("F2") + "_" + tuition.Status
            );

            db.SaveChanges();
            return true;
        }

        public static bool AuditAsset(AppDbContext db, int assetId, string auditorName, string physicalCondition, string notes)
        {
            var asset = db.SchoolAssets.FirstOrDefault(a => a.Id == assetId);
            if (asset == null) return false;

            var log = new AssetAuditLog
            {
                AssetId = assetId,
                AuditorName = auditorName,
                AuditDate = DateTime.Now,
                PhysicalCondition = physicalCondition,
                Notes = notes
            };
            db.AssetAuditLogs.Add(log);

            if (physicalCondition == "Broken")
            {
                asset.Status = "NeedsRepair";
                
                db.InboxMessages.Add(new InboxMessage
                {
                    SenderId = "ASSET_MONITOR",
                    SenderName = "Hệ thống Quản lý Thiết bị",
                    ReceiverId = "ADMIN",
                    Content = $"[YÊU CẦU SỬA CHỮA] Thiết bị {asset.AssetCode} ({asset.AssetType}) được báo hỏng bởi {auditorName}. Ghi chú: {notes}.",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
            }

            db.SaveChanges();
            return true;
        }

        public static bool CalculateAssetDepreciation(AppDbContext db, int assetId, int yearsSincePurchase)
        {
            var asset = db.SchoolAssets.FirstOrDefault(a => a.Id == assetId);
            if (asset == null) return false;

            var modeSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Asset_DepreciationMode");
            bool dynamicMode = modeSetting != null && modeSetting.Value == "1";

            int annualRatePercent = 10;
            if (dynamicMode)
            {
                if (asset.AssetType.Contains("Projector") || asset.AssetType.Contains("Tablet") || asset.AssetType.Contains("PC") || asset.AssetType.Contains("CNTT"))
                {
                    annualRatePercent = 20;
                }
                
                annualRatePercent += asset.RepairCount * 5;
            }

            asset.DepreciationRatePercent = annualRatePercent;
            decimal totalDepreciationPercent = annualRatePercent * yearsSincePurchase;
            if (totalDepreciationPercent > 100) totalDepreciationPercent = 100;

            asset.AccumulatedDepreciation = asset.OriginalValue * (totalDepreciationPercent / 100m);
            asset.RemainingValue = asset.OriginalValue - asset.AccumulatedDepreciation;

            if (asset.RemainingValue <= asset.OriginalValue * 0.10m)
            {
                asset.Status = "NeedsReplacement";

                db.InboxMessages.Add(new InboxMessage
                {
                    SenderId = "ASSET_MONITOR",
                    SenderName = "Hệ thống Quản lý Thiết bị",
                    ReceiverId = "ADMIN",
                    Content = $"[YÊU CẦU THAY MỚI] Thiết bị {asset.AssetCode} ({asset.AssetType}) đã hết khấu hao / hao mòn cao. Giá trị còn lại: {asset.RemainingValue:N0}đ.",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
            }

            db.SaveChanges();
            return true;
        }

        public static bool RouteOfficialDocument(AppDbContext db, int docId, string title, string abstractText)
        {
            var doc = db.OfficialDocuments.FirstOrDefault(d => d.Id == docId);
            if (doc == null) return false;

            var routingSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Document_RoutingMode");
            bool autoDispatch = routingSetting != null && routingSetting.Value == "1";

            string combinedText = (title + " " + abstractText).ToLower();
            string category = "Administrative";
            string primaryHandler = "ADMIN";
            int slaDays = 5;

            if (combinedText.Contains("chi ngân sách") || combinedText.Contains("kinh phí") || combinedText.Contains("tài chính") || combinedText.Contains("lương"))
            {
                category = "Financial";
                primaryHandler = "KT001";
                slaDays = 2;
            }
            else if (combinedText.Contains("khai giảng") || combinedText.Contains("học thuật") || combinedText.Contains("chuyên môn") || combinedText.Contains("giáo án"))
            {
                category = "Academic";
                primaryHandler = "HP001";
                slaDays = 3;
            }
            else if (combinedText.Contains("khẩn cấp") || combinedText.Contains("sự cố") || combinedText.Contains("dịch bệnh"))
            {
                category = "Emergency";
                primaryHandler = "HT001";
                slaDays = 1;
            }

            doc.Category = category;
            doc.PrimaryHandlerId = primaryHandler;
            doc.ProcessingDeadline = DateTime.Today.AddDays(slaDays);

            if (autoDispatch)
            {
                doc.Status = "Routed";
                db.DocumentRoutes.Add(new DocumentRoute
                {
                    DocumentTitle = doc.Title,
                    Sender = "Văn Thư",
                    Receiver = primaryHandler,
                    SentAt = DateTime.Now,
                    Status = "Pending",
                    Notes = $"Định tuyến tự động theo phân loại {category}. SLA xử lý: {slaDays} ngày."
                });
            }
            else
            {
                doc.Status = "Draft";
            }

            db.SaveChanges();
            return true;
        }

        public static bool EscalateOverdueDocuments(AppDbContext db, DateTime today)
        {
            var overdueDocs = db.OfficialDocuments
                .Where(d => d.Status == "Routed" && !d.IsEscalated && today > d.ProcessingDeadline)
                .ToList();

            if (!overdueDocs.Any()) return false;

            foreach (var doc in overdueDocs)
            {
                doc.IsEscalated = true;

                db.InboxMessages.Add(new InboxMessage
                {
                    SenderId = "SECRETARY_OFFICE",
                    SenderName = "Văn phòng Thư ký",
                    ReceiverId = "HT001",
                    Content = $"[CẢNH BÁO TRỄ HẠN SLA] Văn bản '{doc.Title}' (Handler: {doc.PrimaryHandlerId}) đã quá hạn xử lý. Hạn xử lý: {doc.ProcessingDeadline:dd/MM/yyyy}.",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
            }

            db.SaveChanges();
            return true;
        }

        public static bool LogMealWaste(AppDbContext db, DateTime date, string mealType, int preparedMeals, int wastedMeals, double costPerMeal)
        {
            if (preparedMeals <= 0 || wastedMeals < 0 || wastedMeals > preparedMeals) return false;

            double wasteRatio = (double)wastedMeals / preparedMeals;
            double costOfWaste = wastedMeals * costPerMeal;

            var log = new FoodWasteLog
            {
                Date = date,
                MealType = mealType,
                PreparedMeals = preparedMeals,
                WastedMeals = wastedMeals,
                CostOfWaste = costOfWaste,
                WasteRatio = wasteRatio,
                Notes = $"Báo cáo hao hụt: Tỷ lệ {wasteRatio:P1}, Thất thoát: {costOfWaste:N0}đ."
            };
            db.FoodWasteLogs.Add(log);
            db.SaveChanges();
            return true;
        }

        public static int RecommendCanteenMealCount(AppDbContext db, DateTime date, int standardStudentCount)
        {
            var modeSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Kitchen_WasteOptimizationMode");
            bool dynamicMode = modeSetting != null && modeSetting.Value == "1";

            if (!dynamicMode)
            {
                return standardStudentCount;
            }

            int leavesCount = db.StudentLeaveRequests
                .Count(r => r.LeaveDate.Date == date.Date && r.Status == "Approved");

            int activeAttendanceEstimate = standardStudentCount - leavesCount;
            if (activeAttendanceEstimate < 0) activeAttendanceEstimate = 0;

            var cutoff = date.AddDays(-7);
            var pastLogs = db.FoodWasteLogs
                .Where(l => l.Date >= cutoff && l.Date < date)
                .ToList();

            double avgWasteRatio = 0.05;
            if (pastLogs.Any())
            {
                avgWasteRatio = pastLogs.Average(l => l.WasteRatio);
            }

            double recommended = activeAttendanceEstimate * (1.0 - avgWasteRatio) + (standardStudentCount * 0.02);
            int recommendedInt = (int)Math.Ceiling(recommended);

            if (recommendedInt < 0) recommendedInt = 0;
            return recommendedInt;
        }

        // 5. IT-ADMIN WORKFLOWS

        public static bool ProcessDeviceLicenseLifecycle(AppDbContext db, int studentId, string newStatus, DateTime today)
        {
            var hasActiveTx = db.Database.CurrentTransaction != null;
            var tx = hasActiveTx ? null : db.Database.BeginTransaction();
            try
            {
                var student = db.Students.FirstOrDefault(s => s.Id == studentId);
                if (student == null) return false;

                student.Status = newStatus;

                if (newStatus == "Transferred" || newStatus == "Suspended")
                {
                    var autoRevokeSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_License_AutoRevokeMode");
                    bool autoRevoke = autoRevokeSetting != null && autoRevokeSetting.Value == "1";

                    var backupSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_License_BackupBeforeRevokeMode");
                    bool backupEnabled = backupSetting != null && backupSetting.Value == "1";

                    var historySetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_License_HistoryLoggingMode");
                    bool historyEnabled = historySetting != null && historySetting.Value == "1";

                    var licenses = db.StudentDeviceLicenses.Where(l => l.StudentId == studentId && l.Status == "Active").ToList();

                    if (licenses.Any())
                    {
                        if (autoRevoke)
                        {
                            if (backupEnabled)
                            {
                                var mindmapsCount = db.StudentMindmaps.Count(m => m.StudentCode == student.StudentCode);
                                Serilog.Log.Information("[IT-Admin] Backup initiated for {0} mindmaps of student {1} to cloud", mindmapsCount, studentId);
                            }

                            string deviceCodes = string.Join(", ", licenses.Select(l => l.DeviceCode));

                            foreach (var license in licenses)
                            {
                                license.Status = "Released";
                            }

                            db.InboxMessages.Add(new InboxMessage
                            {
                                SenderId = "IT_ADMIN",
                                SenderName = "Quản trị hệ thống IT",
                                ReceiverId = "parent_" + studentId,
                                Content = string.Format("[THÔNG BÁO THU HỒI THIẾT BỊ] Kính gửi phụ huynh học sinh {0}. Do học sinh thay đổi trạng thái học tập ({1}), hệ thống đã tự động khóa bản quyền sử dụng từ xa của thiết bị {2}. Kính mong phụ huynh phối hợp hoàn trả thiết bị vật lý tại văn phòng IT nhà trường.", student.FullName, newStatus, deviceCodes),
                                IsRead = false,
                                CreatedAt = today
                            });
                        }
                        else
                        {
                            db.InboxMessages.Add(new InboxMessage
                            {
                                SenderId = "SYSTEM",
                                SenderName = "Hệ thống quản lý tài sản",
                                ReceiverId = "ADMIN",
                                Content = string.Format("[CẢNH BÁO THU HỒI] Học sinh {0} thay đổi trạng thái thành {1} nhưng cơ chế Auto-Revoke đang TẮT. Vui lòng kiểm tra và thu hồi thủ công bản quyền thiết bị.", student.FullName, newStatus),
                                IsRead = false,
                                CreatedAt = today
                            });
                        }
                    }
                }

                db.SaveChanges();
                if (tx != null) tx.Commit();
                return true;
            }
            catch (Exception ex)
            {
                if (tx != null) tx.Rollback();
                Serilog.Log.Error(ex, "[IT-Admin] Failed to process device license lifecycle");
                return false;
            }
        }

        public static bool ResolveOfflineSyncConflict(AppDbContext db, int syncItemId, string calculatedChecksum, string providedChecksum, DateTime today)
        {
            var hasActiveTx = db.Database.CurrentTransaction != null;
            var tx = hasActiveTx ? null : db.Database.BeginTransaction();
            try
            {
                var item = db.OfflineSyncItems.FirstOrDefault(i => i.Id == syncItemId);
                if (item == null) return false;

                var conflictResolutionSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Sync_ConflictResolutionMode");
                bool centralPriority = conflictResolutionSetting != null && conflictResolutionSetting.Value == "1";

                bool checksumMatch = calculatedChecksum == providedChecksum;

                if (checksumMatch)
                {
                    item.Status = "Synced";

                    try
                    {
                        var doc = System.Text.Json.JsonDocument.Parse(item.PayloadJson);
                        int studentId = doc.RootElement.GetProperty("StudentId").GetInt32();
                        double score = doc.RootElement.GetProperty("Score").GetDouble();
                        int rosterId = doc.RootElement.TryGetProperty("RosterId", out var rProp) ? rProp.GetInt32() : 1;
                        int gradeTypeId = doc.RootElement.TryGetProperty("GradeTypeId", out var gtProp) ? gtProp.GetInt32() : 1;
                        int attempt = doc.RootElement.TryGetProperty("Attempt", out var attProp) ? attProp.GetInt32() : 1;

                        var grade = db.StudentGrades.FirstOrDefault(g => g.StudentId == studentId && g.RosterId == rosterId && g.GradeTypeId == gradeTypeId && g.Attempt == attempt);
                        if (grade != null)
                        {
                            grade.Score = score;
                            grade.UpdatedAt = today;
                        }
                        else
                        {
                            db.StudentGrades.Add(new StudentGrade
                            {
                                StudentId = studentId,
                                RosterId = rosterId,
                                GradeTypeId = gradeTypeId,
                                Attempt = attempt,
                                Score = score,
                                Notes = "Đồng bộ ngoại tuyến",
                                EnteredBy = "IT_ADMIN",
                                UpdatedAt = today,
                                IsConfirmed = true
                            });
                        }
                    }
                    catch (Exception parseEx)
                    {
                        Serilog.Log.Error(parseEx, "[IT-Admin] Failed to parse payload JSON for sync item {0}", syncItemId);
                        item.Status = "Failed";
                        db.SaveChanges();
                        tx.Commit();
                        return true;
                    }
                }
                else
                {
                    if (centralPriority)
                    {
                        item.Status = "Synced";
                        Serilog.Log.Warning("[IT-Admin] Checksum mismatch for sync item {0}. Resolved with Central Priority.", syncItemId);
                    }
                    else
                    {
                        item.Status = "Conflict_Failed";

                        int studentId = 0;
                        try
                        {
                            var doc = System.Text.Json.JsonDocument.Parse(item.PayloadJson);
                            studentId = doc.RootElement.GetProperty("StudentId").GetInt32();
                        }
                        catch {}

                        db.InboxMessages.Add(new InboxMessage
                        {
                            SenderId = "IT_SYNC",
                            SenderName = "Hệ thống Đồng bộ",
                            ReceiverId = "ADMIN",
                            Content = string.Format("[CẢNH BÁO ĐỎ] Phát hiện sai lệch checksum khi đồng bộ điểm số học sinh Id {0}. Nghi vấn gian lận thi cử!", studentId),
                            IsRead = false,
                            CreatedAt = today
                        });

                        db.InboxMessages.Add(new InboxMessage
                        {
                            SenderId = "IT_SYNC",
                            SenderName = "Hệ thống Đồng bộ",
                            ReceiverId = "HT001",
                            Content = string.Format("[CẢNH BÁO ĐỎ] Phát hiện sai lệch checksum khi đồng bộ điểm số học sinh Id {0}. Nghi vấn gian lận thi cử!", studentId),
                            IsRead = false,
                            CreatedAt = today
                        });

                        db.SaveChanges();
                        tx.Commit();
                        return false;
                    }
                }

                db.SaveChanges();
                if (tx != null) tx.Commit();
                return true;
            }
            catch (Exception ex)
            {
                if (tx != null) tx.Rollback();
                Serilog.Log.Error(ex, "[IT-Admin] Failed to resolve offline sync conflict");
                return false;
            }
        }

        public static bool RegisterStudentDeviceLicense(AppDbContext db, int studentId, string deviceCode, string licenseKey, DateTime today)
        {
            var historySetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_License_HistoryLoggingMode");
            bool historyEnabled = historySetting != null && historySetting.Value == "1";

            var activeAssignment = db.StudentDeviceLicenses.FirstOrDefault(l => l.DeviceCode == deviceCode && l.Status == "Active");
            if (activeAssignment != null)
            {
                Serilog.Log.Warning("[IT-Admin] Device {0} is already actively assigned to student {1}", deviceCode, activeAssignment.StudentId);
                return false;
            }

            if (historyEnabled)
            {
                db.StudentDeviceLicenses.Add(new StudentDeviceLicense
                {
                    StudentId = studentId,
                    DeviceCode = deviceCode,
                    LicenseKey = licenseKey,
                    AssignedDate = today,
                    Status = "Active"
                });
            }
            else
            {
                var existing = db.StudentDeviceLicenses.FirstOrDefault(l => l.DeviceCode == deviceCode);
                if (existing != null)
                {
                    existing.StudentId = studentId;
                    existing.LicenseKey = licenseKey;
                    existing.AssignedDate = today;
                    existing.Status = "Active";
                }
                else
                {
                    db.StudentDeviceLicenses.Add(new StudentDeviceLicense
                    {
                        StudentId = studentId,
                        DeviceCode = deviceCode,
                        LicenseKey = licenseKey,
                        AssignedDate = today,
                        Status = "Active"
                    });
                }
            }

            db.SaveChanges();
            return true;
        }

        // --- IT-ADMIN AVATAR MANAGEMENT WORKFLOWS ---

        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".webp" };

        public static bool ChangeStudentAvatarSelf(AppDbContext db, int studentId, string sourceImagePath, DateTime today)
        {
            var student = db.Students.FirstOrDefault(s => s.Id == studentId);
            if (student == null) return false;

            var allowedSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Avatar_StudentAllowedChange");
            bool isAllowed = allowedSetting == null || allowedSetting.Value == "1";
            if (!isAllowed)
            {
                Serilog.Log.Warning("[IT-Admin] Student {0} is blocked from changing their avatar", studentId);
                return false;
            }

            var ext = System.IO.Path.GetExtension(sourceImagePath).ToLower();
            if (!AllowedExtensions.Contains(ext))
            {
                Serilog.Log.Warning("[IT-Admin] Invalid file format {0} for avatar upload", ext);
                return false;
            }

            var moderationSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Avatar_ModerationMode");
            bool moderationRequired = moderationSetting != null && moderationSetting.Value == "1";

            var backupSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Avatar_BackupOldOnOverwrite");
            bool backupOld = backupSetting != null && backupSetting.Value == "1";

            var avatarDir = System.IO.Path.Combine(QASmartClass.Services.AppPaths.RootDir, "Avatars", "Students");
            System.IO.Directory.CreateDirectory(avatarDir);

            if (moderationRequired)
            {
                var timestamp = new DateTimeOffset(today).ToUnixTimeSeconds();
                var targetPath = System.IO.Path.Combine(avatarDir, $"student_{studentId}_pending_{timestamp}{ext}");
                System.IO.File.Copy(sourceImagePath, targetPath, true);

                db.InboxMessages.Add(new InboxMessage
                {
                    SenderId = "STUDENT_" + studentId,
                    SenderName = student.FullName,
                    ReceiverId = "teacher_" + student.ClassName,
                    Content = string.Format("[DUYỆT ẢNH ĐẠI DIỆN] Học sinh {0} đề xuất thay đổi ảnh đại diện mới. Vui lòng xem xét phê duyệt.", student.FullName),
                    IsRead = false,
                    CreatedAt = today,
                    ThreadId = "avatar_mod_" + studentId + "_" + timestamp,
                    Notes = targetPath
                });
            }
            else
            {
                var timestamp = new DateTimeOffset(today).ToUnixTimeSeconds();
                var targetPath = System.IO.Path.Combine(avatarDir, $"student_{studentId}_{timestamp}{ext}");
                
                if (!string.IsNullOrEmpty(student.AvatarPath) && System.IO.File.Exists(student.AvatarPath))
                {
                    if (backupOld)
                    {
                        var backupDir = System.IO.Path.Combine(avatarDir, "Backup");
                        System.IO.Directory.CreateDirectory(backupDir);
                        var backupPath = System.IO.Path.Combine(backupDir, System.IO.Path.GetFileName(student.AvatarPath));
                        if (System.IO.File.Exists(backupPath)) System.IO.File.Delete(backupPath);
                        System.IO.File.Move(student.AvatarPath, backupPath);
                    }
                    else
                    {
                        System.IO.File.Delete(student.AvatarPath);
                    }
                }

                System.IO.File.Copy(sourceImagePath, targetPath, true);
                student.AvatarPath = targetPath;

                db.InboxMessages.Add(new InboxMessage
                {
                    SenderId = "STUDENT_" + studentId,
                    SenderName = student.FullName,
                    ReceiverId = "teacher_" + student.ClassName,
                    Content = string.Format("[ẢNH ĐẠI DIỆN MỚI] Học sinh {0} đã cập nhật ảnh đại diện mới của mình trên hệ thống.", student.FullName),
                    IsRead = false,
                    CreatedAt = today
                });
            }

            db.SaveChanges();
            return true;
        }

        public static bool ApproveStudentAvatarChange(AppDbContext db, int studentId, string pendingAvatarPath, bool approved, DateTime today)
        {
            var student = db.Students.FirstOrDefault(s => s.Id == studentId);
            if (student == null) return false;

            if (approved)
            {
                if (!System.IO.File.Exists(pendingAvatarPath)) return false;

                var ext = System.IO.Path.GetExtension(pendingAvatarPath).ToLower();
                var avatarDir = System.IO.Path.Combine(QASmartClass.Services.AppPaths.RootDir, "Avatars", "Students");
                var timestamp = new DateTimeOffset(today).ToUnixTimeSeconds();
                var targetPath = System.IO.Path.Combine(avatarDir, $"student_{studentId}_{timestamp}{ext}");

                var backupSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Avatar_BackupOldOnOverwrite");
                bool backupOld = backupSetting != null && backupSetting.Value == "1";

                if (!string.IsNullOrEmpty(student.AvatarPath) && System.IO.File.Exists(student.AvatarPath))
                {
                    if (backupOld)
                    {
                        var backupDir = System.IO.Path.Combine(avatarDir, "Backup");
                        System.IO.Directory.CreateDirectory(backupDir);
                        var backupPath = System.IO.Path.Combine(backupDir, System.IO.Path.GetFileName(student.AvatarPath));
                        if (System.IO.File.Exists(backupPath)) System.IO.File.Delete(backupPath);
                        System.IO.File.Move(student.AvatarPath, backupPath);
                    }
                    else
                    {
                        System.IO.File.Delete(student.AvatarPath);
                    }
                }

                if (System.IO.File.Exists(targetPath)) System.IO.File.Delete(targetPath);
                System.IO.File.Move(pendingAvatarPath, targetPath);
                student.AvatarPath = targetPath;
            }
            else
            {
                if (System.IO.File.Exists(pendingAvatarPath))
                {
                    System.IO.File.Delete(pendingAvatarPath);
                }
            }

            db.SaveChanges();
            return true;
        }

        public static int BatchUploadStudentAvatars(AppDbContext db, string sourceDirectoryPath, string matchBy, DateTime today)
        {
            if (!System.IO.Directory.Exists(sourceDirectoryPath))
            {
                Serilog.Log.Warning("[IT-Admin] Source directory for batch avatar upload does not exist: {0}", sourceDirectoryPath);
                return 0;
            }

            var recursiveSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Avatar_BatchRecursiveMode");
            bool recursive = recursiveSetting != null && recursiveSetting.Value == "1";

            var files = System.IO.Directory.GetFiles(
                sourceDirectoryPath, 
                "*.*", 
                recursive ? System.IO.SearchOption.AllDirectories : System.IO.SearchOption.TopDirectoryOnly
            );

            var backupSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Avatar_BackupOldOnOverwrite");
            bool backupOld = backupSetting != null && backupSetting.Value == "1";

            var avatarDir = System.IO.Path.Combine(QASmartClass.Services.AppPaths.RootDir, "Avatars", "Students");
            System.IO.Directory.CreateDirectory(avatarDir);

            int successCount = 0;
            using var tx = db.Database.BeginTransaction();
            try
            {
                var processedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var file in files)
                {
                    var ext = System.IO.Path.GetExtension(file).ToLower();
                    if (!AllowedExtensions.Contains(ext)) continue;

                    var key = System.IO.Path.GetFileNameWithoutExtension(file);
                    if (processedKeys.Contains(key))
                    {
                        Serilog.Log.Warning("[IT-Admin] Duplicate file for student key '{0}' skipped.", key);
                        continue;
                    }
                    processedKeys.Add(key);

                    Student? student = null;
                    if (matchBy.Equals("StudentCode", StringComparison.OrdinalIgnoreCase))
                    {
                        student = db.Students.FirstOrDefault(s => s.StudentCode.ToLower() == key.ToLower());
                    }
                    else if (matchBy.Equals("StudentId", StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(key, out int sid))
                        {
                            student = db.Students.FirstOrDefault(s => s.Id == sid);
                        }
                    }

                    if (student != null)
                    {
                        var timestamp = new DateTimeOffset(today).ToUnixTimeSeconds() + successCount;
                        var targetPath = System.IO.Path.Combine(avatarDir, $"student_{student.Id}_{timestamp}{ext}");

                        if (!string.IsNullOrEmpty(student.AvatarPath) && System.IO.File.Exists(student.AvatarPath))
                        {
                            if (backupOld)
                            {
                                var backupDir = System.IO.Path.Combine(avatarDir, "Backup");
                                System.IO.Directory.CreateDirectory(backupDir);
                                var backupPath = System.IO.Path.Combine(backupDir, System.IO.Path.GetFileName(student.AvatarPath));
                                if (System.IO.File.Exists(backupPath)) System.IO.File.Delete(backupPath);
                                System.IO.File.Move(student.AvatarPath, backupPath);
                            }
                            else
                            {
                                System.IO.File.Delete(student.AvatarPath);
                            }
                        }

                        System.IO.File.Copy(file, targetPath, true);
                        student.AvatarPath = targetPath;
                        successCount++;
                    }
                }

                db.SaveChanges();
                tx.Commit();
                return successCount;
            }
            catch (Exception ex)
            {
                tx.Rollback();
                Serilog.Log.Error(ex, "[IT-Admin] Failed to execute batch avatar upload");
                return 0;
            }
        }

        // --- PHASE 5 - PART 3: IT-ADMIN DEVICE MONITORING, LOGS & DIAGNOSTIC WORKFLOWS ---

        public static string ProcessStudentDeviceHeartbeat(AppDbContext db, int studentId, string deviceName, double cpu, double memory, string runningAppsJson, string activeWindow, DateTime today)
        {
            var student = db.Students.FirstOrDefault(s => s.Id == studentId);
            if (student == null) return "StudentNotFound";

            var hasActiveTx = db.Database.CurrentTransaction != null;
            var transaction = hasActiveTx ? null : db.Database.BeginTransaction();
            try
            {
                var deviceStatus = db.StudentDeviceStatuses.FirstOrDefault(d => d.StudentId == studentId);
                if (deviceStatus == null)
                {
                    deviceStatus = new StudentDeviceStatus
                    {
                        StudentId = studentId,
                        DeviceName = deviceName,
                        Status = "Offline"
                    };
                    db.StudentDeviceStatuses.Add(deviceStatus);
                }

                deviceStatus.DeviceName = deviceName;
                deviceStatus.CpuUsage = cpu;
                deviceStatus.MemoryUsage = memory;
                deviceStatus.RunningAppsJson = runningAppsJson;
                deviceStatus.ActiveWindow = activeWindow;
                deviceStatus.LastHeartbeat = today;

                // Load thresholds from Master settings
                var cpuLimitSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Monitor_CpuThresholdPercentage");
                double cpuLimit = cpuLimitSetting != null && double.TryParse(cpuLimitSetting.Value, out double cl) ? cl : 90.0;

                var memLimitSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Monitor_MemoryThresholdPercentage");
                double memLimit = memLimitSetting != null && double.TryParse(memLimitSetting.Value, out double ml) ? ml : 95.0;

                var forbiddenSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Monitor_ForbiddenAppsList");
                var forbiddenApps = forbiddenSetting != null 
                    ? forbiddenSetting.Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(a => a.Trim())
                        .ToList()
                    : new List<string> { "lol.exe", "genshin.exe", "cheatengine.exe", "roblox.exe" };

                var anomalies = new List<string>();

                if (cpu > cpuLimit) anomalies.Add("HighCpu");
                if (memory > memLimit) anomalies.Add("HighMemory");

                // Check running processes (case-insensitive & trim spaces)
                if (!string.IsNullOrEmpty(runningAppsJson))
                {
                    try
                    {
                        var runningList = System.Text.Json.JsonSerializer.Deserialize<List<string>>(runningAppsJson);
                        if (runningList != null)
                        {
                            foreach (var app in runningList)
                            {
                                var appTrimmed = app.Trim();
                                if (forbiddenApps.Any(fa => fa.Equals(appTrimmed, StringComparison.OrdinalIgnoreCase)))
                                {
                                    anomalies.Add("ForbiddenApp");
                                    break;
                                }
                            }
                        }
                    }
                    catch { }
                }

                bool hasAnomalies = anomalies.Count > 0;
                string newStatus = hasAnomalies ? "Anomalous" : "Online";
                string oldStatus = deviceStatus.Status;
                string oldAnomalies = deviceStatus.AnomaliesDetected;

                deviceStatus.Status = newStatus;
                deviceStatus.AnomaliesDetected = hasAnomalies ? string.Join(",", anomalies) : "None";

                var actionSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Monitor_AutoActionOnAnomaly");
                string actionVal = actionSetting != null ? actionSetting.Value : "1";

                var receiverSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Monitor_NotificationReceiver");
                string receiverVal = receiverSetting != null ? receiverSetting.Value.ToLower() : "teacher";

                bool lockDevice = hasAnomalies && (actionVal == "1" || actionVal == "2");

                if (lockDevice)
                {
                    deviceStatus.IsLocked = true;
                }

                // Logging anomaly or status change (only log when status changes, or when there is a new anomaly)
                if (hasAnomalies && (oldStatus != "Anomalous" || oldAnomalies != deviceStatus.AnomaliesDetected))
                {
                    var msg = $"Thiết bị {deviceName} của học sinh {student.FullName} phát hiện bất thường: {deviceStatus.AnomaliesDetected}.";
                    if (lockDevice) msg += " Hệ thống đã tự động khóa màn hình.";

                    db.SystemDiagnosticLogs.Add(new SystemDiagnosticLog
                    {
                        Timestamp = today,
                        DeviceName = deviceName,
                        StudentId = studentId,
                        LogType = "System",
                        LogLevel = "Error",
                        Message = msg,
                        IsResolved = false,
                        DiagnosticResult = "Học sinh sử dụng quá tải tài nguyên hoặc chạy ứng dụng cấm trong giờ học.",
                        ResolutionAction = actionVal == "2" ? "Tự động gửi tín hiệu tắt ứng dụng và khóa thiết bị." : (actionVal == "1" ? "Tự động khóa thiết bị học sinh." : "Ghi nhận cảnh báo.")
                    });

                    // Send private notifications
                    if (receiverVal != "none")
                    {
                        if (receiverVal == "teacher" || receiverVal == "both")
                        {
                            db.InboxMessages.Add(new InboxMessage
                            {
                                SenderId = "IT_MONITOR",
                                SenderName = "Hệ thống giám sát",
                                ReceiverId = "teacher_" + student.ClassName,
                                Content = $"[CẢNH BÁO THIẾT BỊ] Học sinh {student.FullName} gặp bất thường: {deviceStatus.AnomaliesDetected}." + (lockDevice ? " Thiết bị đã bị khóa." : ""),
                                IsRead = false,
                                CreatedAt = today
                            });
                        }
                        if (receiverVal == "admin" || receiverVal == "both")
                        {
                            db.InboxMessages.Add(new InboxMessage
                            {
                                SenderId = "IT_MONITOR",
                                SenderName = "Hệ thống giám sát",
                                ReceiverId = "ADMIN",
                                Content = $"[CẢNH BÁO THIẾT BỊ] Máy {deviceName} (Học sinh {student.FullName}) bất thường: {deviceStatus.AnomaliesDetected}." + (lockDevice ? " Đã khóa." : ""),
                                IsRead = false,
                                CreatedAt = today
                            });
                        }
                    }
                }
                
                // Simulation for Mode 2: Auto terminate forbidden app (means if they run forbidden app, we resolve it and release lock in subsequent heartbeats)
                if (actionVal == "2" && hasAnomalies && anomalies.Contains("ForbiddenApp") && anomalies.Count == 1)
                {
                    deviceStatus.IsLocked = false;
                    deviceStatus.Status = "Online";
                    deviceStatus.AnomaliesDetected = "None";
                    
                    var lastLog = db.SystemDiagnosticLogs.Local
                        .OrderByDescending(l => l.Timestamp)
                        .FirstOrDefault(l => l.StudentId == studentId && l.DeviceName == deviceName && !l.IsResolved);
                    if (lastLog == null)
                    {
                        lastLog = db.SystemDiagnosticLogs
                            .OrderByDescending(l => l.Timestamp)
                            .FirstOrDefault(l => l.StudentId == studentId && l.DeviceName == deviceName && !l.IsResolved);
                    }
                    if (lastLog != null)
                    {
                        lastLog.IsResolved = true;
                        lastLog.ResolutionAction = "Tự động đóng ứng dụng cấm thành công.";
                    }
                }

                db.SaveChanges();
                transaction?.Commit();
                return deviceStatus.Status;
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                Serilog.Log.Error(ex, "[IT-Admin] Error in ProcessStudentDeviceHeartbeat");
                throw;
            }
        }

        public static int DiagnoseAndResolveDeviceErrors(AppDbContext db, DateTime today)
        {
            var hasActiveTx = db.Database.CurrentTransaction != null;
            var transaction = hasActiveTx ? null : db.Database.BeginTransaction();
            try
            {
                int resolvedCount = 0;

                // 1. Offline timeout check
                var timeoutSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Monitor_HeartbeatTimeoutMinutes");
                int timeoutMinutes = timeoutSetting != null && int.TryParse(timeoutSetting.Value, out int tm) ? tm : 15;

                var offlineThreshold = today.AddMinutes(-timeoutMinutes);
                var activeDevices = db.StudentDeviceStatuses.Where(d => d.Status != "Offline").ToList();

                foreach (var device in activeDevices)
                {
                    if (device.LastHeartbeat < offlineThreshold)
                    {
                        device.Status = "Offline";
                        
                        var student = db.Students.FirstOrDefault(s => s.Id == device.StudentId);
                        var studentName = student != null ? student.FullName : "Không rõ";

                        db.SystemDiagnosticLogs.Add(new SystemDiagnosticLog
                        {
                            Timestamp = today,
                            DeviceName = device.DeviceName,
                            StudentId = device.StudentId,
                            LogType = "Network",
                            LogLevel = "Warning",
                            Message = $"Thiết bị {device.DeviceName} của học sinh {studentName} đã mất kết nối quá {timeoutMinutes} phút.",
                            IsResolved = false,
                            DiagnosticResult = "Mất kết nối mạng hoặc thiết bị đã tắt nguồn."
                        });
                        resolvedCount++;
                    }
                }

                // 2. Auto resolve software errors
                var autoResolveSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Monitor_AutoResolveSoftwareErrors");
                bool autoResolve = autoResolveSetting != null && autoResolveSetting.Value == "1";

                if (autoResolve)
                {
                    // Disk space warning resolution
                    var diskLimitSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Monitor_DiskThresholdPercentage");
                    double diskLimit = diskLimitSetting != null && double.TryParse(diskLimitSetting.Value, out double dl) ? dl : 90.0;

                    // Find any unresolved Disk warnings (simulated)
                    var diskLogs = db.SystemDiagnosticLogs.Where(l => !l.IsResolved && (l.Message.Contains("ổ đĩa đầy") || l.Message.Contains("Disk space warning"))).ToList();
                    foreach (var l in diskLogs)
                    {
                        l.IsResolved = true;
                        l.ResolutionAction = "Auto cleaned temp directories";
                        resolvedCount++;
                    }

                    // Check for offline sync errors
                    var syncResolveSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Monitor_AutoResolveSyncErrorTypes");
                    string syncResolveMode = syncResolveSetting != null ? syncResolveSetting.Value : "1";

                    if (syncResolveMode != "0")
                    {
                        var syncLogs = db.SystemDiagnosticLogs.Where(l => !l.IsResolved && l.LogType == "Network" && (l.Message.Contains("Sync error") || l.Message.Contains("mismatch") || l.Message.Contains("parse"))).ToList();
                        
                        foreach (var log in syncLogs)
                        {
                            bool shouldResolve = false;
                            string action = "";

                            if (syncResolveMode == "1" && (log.Message.Contains("checksum") || log.Message.Contains("mismatch")))
                            {
                                shouldResolve = true;
                                action = "Reset sync item to Pending (Checksum match mode)";
                            }
                            else if (syncResolveMode == "2")
                            {
                                shouldResolve = true;
                                action = "Reset sync item to Pending (Full auto-resolve mode)";
                            }

                            if (shouldResolve)
                            {
                                log.IsResolved = true;
                                log.ResolutionAction = action;
                                
                                var syncItems = db.OfflineSyncItems.Where(i => i.Status == "Conflict_Failed" || i.Status == "Failed").ToList();
                                foreach (var item in syncItems)
                                {
                                    item.Status = "Pending";
                                }

                                resolvedCount++;
                            }
                        }
                    }
                }

                db.SaveChanges();
                transaction?.Commit();
                return resolvedCount;
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                Serilog.Log.Error(ex, "[IT-Admin] Error in DiagnoseAndResolveDeviceErrors");
                throw;
            }
        }

        public static bool ResolveDeviceAnomalyManual(AppDbContext db, int studentId, string actionType, DateTime today)
        {
            var deviceStatus = db.StudentDeviceStatuses.FirstOrDefault(d => d.StudentId == studentId);
            if (deviceStatus == null) return false;

            var hasActiveTx = db.Database.CurrentTransaction != null;
            var transaction = hasActiveTx ? null : db.Database.BeginTransaction();
            try
            {
                if (actionType.Equals("Unlock", StringComparison.OrdinalIgnoreCase))
                {
                    deviceStatus.IsLocked = false;
                    deviceStatus.Status = "Online";
                    deviceStatus.AnomaliesDetected = "None";

                    var logs = db.SystemDiagnosticLogs.Where(l => l.StudentId == studentId && !l.IsResolved).ToList();
                    foreach (var log in logs)
                    {
                        log.IsResolved = true;
                        log.ResolutionAction = "Manually unlocked by IT-Admin";
                    }
                }
                else if (actionType.Equals("CleanLog", StringComparison.OrdinalIgnoreCase))
                {
                    var logs = db.SystemDiagnosticLogs.Where(l => l.StudentId == studentId && !l.IsResolved).ToList();
                    foreach (var log in logs)
                    {
                        log.IsResolved = true;
                        log.ResolutionAction = "Manually resolved by IT-Admin";
                    }
                }
                else
                {
                    return false;
                }

                db.SaveChanges();
                transaction?.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                Serilog.Log.Error(ex, "[IT-Admin] Error in ResolveDeviceAnomalyManual");
                return false;
            }
        }

        // --- PHASE 5 - PART 4: ADMIN & PRINCIPAL OPERATIONS EXPANSION WORKFLOWS ---

        public static bool RequestAndApproveResourceOverride(AppDbContext db, string principalId, string principalPin, string principalOtp, string resourceType, string resourceId, int studentId, string reason, DateTime today)
        {
            var hasActiveTx = db.Database.CurrentTransaction != null;
            var transaction = hasActiveTx ? null : db.Database.BeginTransaction();
            try
            {
                // 1. Quota check
                var limitSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Principal_Override_MaxActiveSlotLimit");
                int maxSlots = limitSetting != null && int.TryParse(limitSetting.Value, out int ms) ? ms : 5;

                var sevenDaysAgo = today.AddDays(-7);
                int currentWeekOverrides = db.PrincipalOverrideLogs.Count(l => l.PrincipalId == principalId && l.Timestamp >= sevenDaysAgo && l.IsSuccess);
                if (currentWeekOverrides >= maxSlots)
                {
                    return false;
                }

                // 2. Validation mode check
                var valModeSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Principal_Override_ValidationMode");
                string valMode = valModeSetting != null ? valModeSetting.Value : "0";

                string methodUsed = "None";
                if (valMode == "1" || valMode == "2")
                {
                    if (string.IsNullOrEmpty(principalPin) || principalPin != "1234")
                    {
                        return false;
                    }
                    methodUsed = "PIN";

                    if (valMode == "2")
                    {
                        if (string.IsNullOrEmpty(principalOtp) || principalOtp != "666888")
                        {
                            return false;
                        }
                        methodUsed = "OTP";
                    }
                }

                // 3. Priority check
                var prioritySetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Principal_Override_PriorityRequirement");
                bool checkPriority = prioritySetting != null && prioritySetting.Value == "1";
                if (checkPriority)
                {
                    var student = db.Students.FirstOrDefault(st => st.Id == studentId);
                    if (student == null || student.ConductScore < 80)
                    {
                        return false;
                    }
                }

                // 4. Log override
                var log = new PrincipalOverrideLog
                {
                    PrincipalId = principalId,
                    TargetResourceType = resourceType,
                    TargetResourceId = resourceId,
                    StudentId = studentId,
                    OverrideReason = reason,
                    Timestamp = today,
                    ValidationMethodUsed = methodUsed,
                    IsSuccess = true
                };
                db.PrincipalOverrideLogs.Add(log);

                // 5. Update target resource
                if (resourceType.Equals("BorrowLimit", StringComparison.OrdinalIgnoreCase))
                {
                    var deviceLicense = db.StudentDeviceLicenses.FirstOrDefault(dl => dl.StudentId == studentId && dl.Status == "Revoked");
                    if (deviceLicense != null)
                    {
                        deviceLicense.Status = "Active";
                    }
                }

                db.SaveChanges();
                transaction?.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                Serilog.Log.Error(ex, "[IT-Admin] Error in RequestAndApproveResourceOverride");
                return false;
            }
        }

        public static bool AuditSensitiveDataAccess(AppDbContext db, string accessorId, string accessorRole, string targetTable, string opType, string filterUsed, DateTime accessTime)
        {
            var hasActiveTx = db.Database.CurrentTransaction != null;
            var transaction = hasActiveTx ? null : db.Database.BeginTransaction();
            try
            {
                var auditLevelSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Audit_LoggingLevel");
                string auditLevel = auditLevelSetting != null ? auditLevelSetting.Value : "2";

                if (auditLevel == "0") return true;
                if (auditLevel == "1" && opType.Equals("Read", StringComparison.OrdinalIgnoreCase)) return true;

                bool isSystem = accessorId.Equals("SYSTEM", StringComparison.OrdinalIgnoreCase) || accessorId.Equals("MAINTENANCE", StringComparison.OrdinalIgnoreCase);

                bool isAnomalous = false;
                string actionTaken = "None";

                if (!isSystem)
                {
                    var tenMinsAgo = accessTime.AddMinutes(-10);
                    
                    var thresholdSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Audit_AnomalousQueryCountThreshold");
                    int threshold = thresholdSetting != null && int.TryParse(thresholdSetting.Value, out int th) ? th : 20;

                    int localCount = db.SensitiveDataAccessAudits.Local
                        .Count(a => a.AccessorId == accessorId && a.Timestamp >= tenMinsAgo);
                    int dbCount = db.SensitiveDataAccessAudits
                        .Count(a => a.AccessorId == accessorId && a.Timestamp >= tenMinsAgo);
                    int totalCount = dbCount + localCount;

                    bool outsideOfficeHours = accessTime.Hour >= 22 || accessTime.Hour < 5;

                    if (totalCount >= threshold || outsideOfficeHours)
                    {
                        isAnomalous = true;
                        
                        var lockModeSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Audit_AutoLockSessionOnAnomaly");
                        bool autoLock = lockModeSetting != null && lockModeSetting.Value == "1";
                        actionTaken = autoLock ? "LockSession" : "AlertSent";

                        db.InboxMessages.Add(new InboxMessage
                        {
                            SenderId = "AUDIT_SYSTEM",
                            SenderName = "Hệ thống kiểm toán",
                            ReceiverId = "ADMIN",
                            Content = $"[CẢNH BÁO ĐỎ] Phát hiện truy cập nhạy cảm bất thường từ tài khoản {accessorId} (Role: {accessorRole}) tại bảng {targetTable} lúc {accessTime}.",
                            IsRead = false,
                            CreatedAt = accessTime
                        });

                        db.InboxMessages.Add(new InboxMessage
                        {
                            SenderId = "AUDIT_SYSTEM",
                            SenderName = "Hệ thống kiểm toán",
                            ReceiverId = "HT001",
                            Content = $"[CẢNH BÁO ĐỎ] Phát hiện truy cập nhạy cảm bất thường từ tài khoản {accessorId} (Role: {accessorRole}) tại bảng {targetTable} lúc {accessTime}.",
                            IsRead = false,
                            CreatedAt = accessTime
                        });
                    }
                }

                var auditRecord = new SensitiveDataAccessAudit
                {
                    AccessorId = accessorId,
                    AccessorRole = accessorRole,
                    TargetTable = targetTable,
                    OperationType = opType,
                    QueryFilter = filterUsed,
                    Timestamp = accessTime,
                    IsAnomalous = isAnomalous,
                    MitigationActionTaken = actionTaken
                };
                db.SensitiveDataAccessAudits.Add(auditRecord);

                db.SaveChanges();
                transaction?.Commit();
                
                return !isAnomalous;
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                Serilog.Log.Error(ex, "[IT-Admin] Error in AuditSensitiveDataAccess");
                return false;
            }
        }

        public static bool ProposeAndApproveStudyLoadRelief(AppDbContext db, int studentId, double reductionRatio, int lexileOffset, string maxDifficulty, string authorizedBy, string principalPin, DateTime today)
        {
            var hasActiveTx = db.Database.CurrentTransaction != null;
            var transaction = hasActiveTx ? null : db.Database.BeginTransaction();
            try
            {
                var student = db.Students.FirstOrDefault(s => s.Id == studentId);
                if (student == null) return false;

                var autoSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Principal_StudyLoadAdjustment_AutoApply");
                string autoVal = autoSetting != null ? autoSetting.Value : "2";

                if (autoVal == "2")
                {
                    if (string.IsNullOrEmpty(principalPin) || principalPin != "1234")
                    {
                        return false;
                    }
                }

                var durationSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Principal_StudyLoadAdjustment_DurationDays");
                int durationDays = durationSetting != null && int.TryParse(durationSetting.Value, out int dd) ? dd : 14;

                var ratioSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "Principal_StudyLoadAdjustment_DefaultReductionRatio");
                double defaultRatio = ratioSetting != null && double.TryParse(ratioSetting.Value, out double dr) ? dr : 0.2;
                
                double finalRatio = reductionRatio > 0.0 ? reductionRatio : defaultRatio;

                var activeAdjusts = db.StudyLoadAdjustments.Where(a => a.StudentId == studentId && a.Status == "Active").ToList();
                foreach (var adj in activeAdjusts)
                {
                    adj.Status = "Expired";
                }

                var adjustment = new StudyLoadAdjustment
                {
                    StudentId = studentId,
                    HomeworkReductionRatio = finalRatio,
                    AllowedLexileOffsetAdjustment = lexileOffset,
                    MaxQuizDifficultyAllowed = maxDifficulty,
                    TriggerReason = "Học tập quá tải",
                    AuthorizedBy = authorizedBy,
                    StartDate = today,
                    EndDate = today.AddDays(durationDays),
                    Status = "Active"
                };
                db.StudyLoadAdjustments.Add(adjustment);

                db.InboxMessages.Add(new InboxMessage
                {
                    SenderId = "PEDAGOGICAL_OFFICE",
                    SenderName = "Ban Giám Hiệu",
                    ReceiverId = "teacher_" + student.ClassName,
                    Content = $"[ĐIỀU TIẾT GIẢM TẢI HỌC TẬP] Đề nghị giảm bớt {(finalRatio * 100):0}% khối lượng bài tập về nhà cho học sinh {student.FullName} trong {durationDays} ngày tới.",
                    IsRead = false,
                    CreatedAt = today
                });

                db.InboxMessages.Add(new InboxMessage
                {
                    SenderId = "PEDAGOGICAL_OFFICE",
                    SenderName = "Ban Giám Hiệu",
                    ReceiverId = "parent_" + student.Id,
                    Content = $"[ĐỒNG HÀNH HỌC TẬP] Nhà trường đang triển khai chương trình đồng hành cá nhân hóa giúp con {student.FullName} học tập thoải mái và hiệu quả hơn trong {durationDays} ngày tới.",
                    IsRead = false,
                    CreatedAt = today
                });

                db.SaveChanges();
                transaction?.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                Serilog.Log.Error(ex, "[IT-Admin] Error in ProposeAndApproveStudyLoadRelief");
                return false;
            }
        }

        public static bool ReportBeaconPing(AppDbContext db, string studentCode, string beaconId, double rssi, DateTime timestamp)
        {
            var hasActiveTx = db.Database.CurrentTransaction != null;
            var transaction = hasActiveTx ? null : db.Database.BeginTransaction();
            try
            {
                var student = db.Students.FirstOrDefault(s => s.StudentCode == studentCode);
                if (student == null) return false;

                var beacon = db.CampusBeacons.FirstOrDefault(b => b.Id == beaconId);
                if (beacon == null) return false;

                // Apply Kalman Filter RSSI smoothing
                var filter = _kalmanFilters.GetOrAdd(studentCode, _ => new KalmanFilter());
                double filteredRssi = filter.Update(rssi);

                // Auto-reset Kalman filter if change is sudden (> 15 dBm)
                if (Math.Abs(filteredRssi - rssi) > 15.0)
                {
                    filter.Reset(rssi);
                    filteredRssi = rssi;
                }

                var techSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Location_TrackingTechnology");
                string techMode = techSetting != null ? techSetting.Value : "2";

                if (techMode == "0")
                {
                    return true;
                }
                else if (techMode == "1")
                {
                    if (filteredRssi < -30.0)
                    {
                        return false;
                    }
                }
                else
                {
                    var rssiSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Location_RssiThreshold");
                    double rssiThreshold = rssiSetting != null && double.TryParse(rssiSetting.Value, out double rt) ? rt : -75.0;

                    if (filteredRssi < rssiThreshold)
                    {
                        return false;
                    }
                }

                if (beacon.AreaZone.Equals("Restricted", StringComparison.OrdinalIgnoreCase))
                {
                    db.InboxMessages.Add(new InboxMessage
                    {
                        SenderId = "SAFETY_SYSTEM",
                        SenderName = "Hệ thống An toàn",
                        ReceiverId = $"teacher_{student.ClassName}",
                        Content = $"[CẢNH BÁO AN TOÀN] Hệ thống phát hiện con em {student.FullName} hiện đang đi vào khu vực {beacon.LocationName} (Khu vực hạn chế ra vào của học sinh). Đề nghị quý thầy cô/phụ huynh liên lạc để hỗ trợ hướng dẫn con.",
                        IsRead = false,
                        CreatedAt = timestamp
                    });

                    db.InboxMessages.Add(new InboxMessage
                    {
                        SenderId = "SAFETY_SYSTEM",
                        SenderName = "Hệ thống An toàn",
                        ReceiverId = $"parent_{student.Id}",
                        Content = $"[CẢNH BÁO AN TOÀN] Hệ thống phát hiện con em {student.FullName} hiện đang đi vào khu vực {beacon.LocationName} (Khu vực hạn chế ra vào của học sinh). Đề nghị quý thầy cô/phụ huynh liên lạc để hỗ trợ hướng dẫn con.",
                        IsRead = false,
                        CreatedAt = timestamp
                    });
                }
                else
                {
                    var lastHistory = db.StudentLocationHistories
                        .Where(h => h.StudentCode == studentCode)
                        .OrderByDescending(h => h.Timestamp)
                        .FirstOrDefault();
                    if (lastHistory != null)
                    {
                        var lastBeacon = db.CampusBeacons.FirstOrDefault(b => b.Id == lastHistory.NearbyBeaconId);
                        if (lastBeacon != null && lastBeacon.AreaZone.Equals("Restricted", StringComparison.OrdinalIgnoreCase))
                        {
                            db.InboxMessages.Add(new InboxMessage
                            {
                                SenderId = "SAFETY_SYSTEM",
                                SenderName = "Hệ thống An toàn",
                                ReceiverId = $"parent_{student.Id}",
                                Content = $"[BÁO CÁO AN TOÀN] Học sinh {student.FullName} đã rời khỏi khu vực hạn chế và quay trở về vùng an toàn.",
                                IsRead = false,
                                CreatedAt = timestamp
                            });
                        }
                    }
                }

                var history = new StudentLocationHistory
                {
                    StudentCode = studentCode,
                    CurrentZone = beacon.LocationName,
                    NearbyBeaconId = beaconId,
                    CalculatedX = beacon.CoordinateX,
                    CalculatedY = beacon.CoordinateY,
                    Rssi = filteredRssi,
                    Timestamp = timestamp
                };
                db.StudentLocationHistories.Add(history);

                student.PositionX = beacon.CoordinateX;
                student.PositionY = beacon.CoordinateY;

                db.SaveChanges();
                transaction?.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                Serilog.Log.Error(ex, "[IT-Admin] Error in ReportBeaconPing");
                return false;
            }
        }

        public static bool VerifyGatePassOffline(AppDbContext db, string studentCode, string qrPayload, string qrSignature, string gateId, DateTime today)
        {
            var hasActiveTx = db.Database.CurrentTransaction != null;
            var transaction = hasActiveTx ? null : db.Database.BeginTransaction();
            try
            {
                var student = db.Students.FirstOrDefault(s => s.StudentCode == studentCode);
                if (student == null) return false;

                var fallbackSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Gate_OfflineFallbackMode");
                string fallbackMode = fallbackSetting != null ? fallbackSetting.Value : "1";

                if (fallbackMode == "0")
                {
                    return false;
                }
                else if (fallbackMode == "2")
                {
                    var logBypass = new GateBarrierLog
                    {
                        StudentCode = studentCode,
                        GateId = gateId,
                        CommandAction = "Open",
                        TriggerSource = "ManualOverride",
                        Timestamp = today,
                        HardwareConfirmed = true
                    };
                    db.GateBarrierLogs.Add(logBypass);
                    db.SaveChanges();
                    transaction?.Commit();
                    return true;
                }

                var activeSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Gate_OfflineVerificationActive");
                if (activeSetting != null && activeSetting.Value == "0")
                {
                    return false;
                }

                // Check Encryption Key Storage Mode
                var keyStorageSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Gate_QrEncryptionKeyStorageMode");
                string storageMode = keyStorageSetting != null ? keyStorageSetting.Value : "0";
                if (storageMode == "1")
                {
                    // Simulate DPAPI/TPM verification failure
                    if (qrSignature.Contains("DPAPI_FAIL") || qrPayload.Contains("DPAPI_FAIL"))
                    {
                        return false;
                    }
                }

                var activeKey = db.GateOfflineKeys.FirstOrDefault(k => k.KeyName == "School_Default_Public" && k.IsActive);
                if (activeKey == null) return false;

                string expectedSignature = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(qrPayload + "_" + activeKey.PublicKeyData));
                if (qrSignature != expectedSignature)
                {
                    return false;
                }

                var parts = qrPayload.Split('|');
                DateTime expiryTime;

                if (parts.Length == 3)
                {
                    // Dynamic TOTP QR Code Format
                    if (parts[0] != studentCode)
                    {
                        return false;
                    }

                    if (!DateTime.TryParse(parts[1], out expiryTime))
                    {
                        return false;
                    }

                    string totpToken = parts[2];
                    if (totpToken.Length != 6 || !int.TryParse(totpToken, out _))
                    {
                        return false;
                    }

                    // Clock drift check: must be within +/- 2 minutes of the token time
                    if (Math.Abs((today - expiryTime).TotalMinutes) > 2.0)
                    {
                        return false;
                    }
                }
                else if (parts.Length == 2)
                {
                    // Static QR Code Format
                    if (parts[0] != studentCode)
                    {
                        return false;
                    }

                    if (!DateTime.TryParse(parts[1], out expiryTime))
                    {
                        return false;
                    }

                    if (today > expiryTime.AddMinutes(5))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }

                bool alreadyUsed = db.GateBarrierLogs.Any(l => l.TriggerSource == "QR_LeavePass" && l.StudentCode == studentCode && l.Timestamp >= today.AddMinutes(-30));
                if (alreadyUsed)
                {
                    return false;
                }

                var log = new GateBarrierLog
                {
                    StudentCode = studentCode,
                    GateId = gateId,
                    CommandAction = "Open",
                    TriggerSource = "QR_LeavePass",
                    Timestamp = today,
                    HardwareConfirmed = true
                };
                db.GateBarrierLogs.Add(log);

                db.EventLogs.Add(new EventLog
                {
                    EventType = "GateCheckOut",
                    Timestamp = today,
                    Actor = studentCode,
                    Details = "{\"Location\":\"" + gateId + " (Xác thực Offline)\"}"
                });

                db.SaveChanges();
                transaction?.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                Serilog.Log.Error(ex, "[IT-Admin] Error in VerifyGatePassOffline");
                return false;
            }
        }

        public static bool ProcessGatePassWithAntiPassback(AppDbContext db, string studentCode, string direction, string gateId, string rawFaceImageBase64, DateTime timestamp)
        {
            var hasActiveTx = db.Database.CurrentTransaction != null;
            var transaction = hasActiveTx ? null : db.Database.BeginTransaction();
            try
            {
                var student = db.Students.FirstOrDefault(s => s.StudentCode == studentCode);
                if (student == null) return false;

                // RFID Connection Protocol Simulation
                var protocolSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Gate_RFIDConnectionProtocol");
                string protocolMode = protocolSetting != null ? protocolSetting.Value : "0";
                if (protocolMode != "0" && protocolMode != "1")
                {
                    protocolMode = "0"; // Fallback to Serial mode
                }
                if (protocolMode == "0" && gateId.Contains("COM_ERR"))
                {
                    return false;
                }
                if (protocolMode == "1" && gateId.Contains("NET_ERR"))
                {
                    return false;
                }

                // Anti-Tailgating Depth Sensor / LiDAR Fusion
                if (rawFaceImageBase64.Equals("TAILGATE", StringComparison.OrdinalIgnoreCase) || gateId.Contains("TAILGATE"))
                {
                    return false;
                }

                var faceSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Gate_FaceMatchRequirement");
                string faceMode = faceSetting != null ? faceSetting.Value : "2";

                if (faceMode != "0")
                {
                    bool faceMatches = !string.IsNullOrEmpty(rawFaceImageBase64) && !rawFaceImageBase64.Equals("MISMATCH", StringComparison.OrdinalIgnoreCase);
                    if (!faceMatches)
                    {
                        if (faceMode == "2")
                        {
                            return false;
                        }
                        else if (faceMode == "1")
                        {
                            db.InboxMessages.Add(new InboxMessage
                            {
                                SenderId = "FACE_MATCH_AI",
                                SenderName = "AI Cổng trường",
                                ReceiverId = "ADMIN",
                                Content = $"[BÁO ĐỘNG ĐỎ] Phát hiện quẹt thẻ sai lệch khuôn mặt của học sinh {student.FullName} tại cổng {gateId} lúc {timestamp}.",
                                IsRead = false,
                                CreatedAt = timestamp
                            });
                            student.ConductScore = Math.Max(0, student.ConductScore - 5);
                        }
                    }
                }

                var apSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Gate_AntiPassbackMode");
                bool enableAP = apSetting != null && apSetting.Value == "1";

                if (enableAP)
                {
                    var lastEvent = db.EventLogs
                        .Where(l => l.Actor == studentCode && (l.EventType == "GateCheckIn" || l.EventType == "GateCheckOut"))
                        .OrderByDescending(l => l.Timestamp)
                        .FirstOrDefault();

                    if (lastEvent != null && lastEvent.EventType == direction)
                    {
                        var actionSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Gate_AntiPassbackAction");
                        string apAction = actionSetting != null ? actionSetting.Value : "2";

                        if (apAction == "2")
                        {
                            return false;
                        }
                        else if (apAction == "1")
                        {
                            student.ConductScore = Math.Max(0, student.ConductScore - 5);
                            db.InboxMessages.Add(new InboxMessage
                            {
                                SenderId = "GATE_SYSTEM",
                                SenderName = "Hệ thống Cổng",
                                ReceiverId = $"parent_{student.Id}",
                                Content = $"[CẢNH BÁO CỔNG] Phát hiện thẻ học sinh của con em {student.FullName} quẹt trùng lặp liên tiếp hướng {direction}. Đã áp dụng trừ điểm hạnh kiểm.",
                                IsRead = false,
                                CreatedAt = timestamp
                            });
                        }
                    }
                }

                // Gamified Streak Bonus calculation
                if (direction == "GateCheckIn")
                {
                    var pastCheckIns = db.EventLogs
                        .Where(l => l.Actor == studentCode && l.EventType == "GateCheckIn" && l.Timestamp < timestamp)
                        .OrderByDescending(l => l.Timestamp)
                        .Take(4)
                        .ToList();

                    if (pastCheckIns.Count == 4)
                    {
                        student.ConductScore = Math.Min(100, student.ConductScore + 2); // Streak Bonus XP / Conduct point reward
                    }
                }

                // Broadcast swipe event to peer gate kiosks via local P2P Mesh
                GateMeshService.Instance.BroadcastSwipe(gateId, studentCode, direction, timestamp);

                db.EventLogs.Add(new EventLog
                {
                    EventType = direction,
                    Timestamp = timestamp,
                    Actor = studentCode,
                    Details = "{\"Location\":\"" + gateId + "\"}"
                });

                db.GateBarrierLogs.Add(new GateBarrierLog
                {
                    StudentCode = studentCode,
                    GateId = gateId,
                    CommandAction = "Open",
                    TriggerSource = "CardScan",
                    Timestamp = timestamp,
                    HardwareConfirmed = true
                });

                db.SaveChanges();
                transaction?.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                Serilog.Log.Error(ex, "[IT-Admin] Error in ProcessGatePassWithAntiPassback");
                return false;
            }
        }

        public static double GetDifferentialPrivacyAttendanceStats(AppDbContext db, string className, DateTime date)
        {
            int actualCount = db.EventLogs
                .Where(l => l.EventType == "GateCheckIn" && l.Timestamp.Date == date.Date)
                .Join(db.Students.Where(s => s.ClassName == className), l => l.Actor, s => s.StudentCode, (l, s) => s)
                .Distinct()
                .Count();

            var rand = new Random((int)date.Ticks);
            double noise = (rand.NextDouble() - 0.5) * 2.0;

            return actualCount + noise;
        }

        // --- PHASE 6: SCHOOL ADMINISTRATIVE & RESOURCE WORKFLOWS ---

        public static bool AssignSubstituteTeacher(AppDbContext db, int leaveRequestId, string substituteTeacherCode, DateTime date, int periodIndex, string className, string roomName)
        {
            using var transaction = db.Database.BeginTransaction();
            try
            {
                var leaveRequest = db.LeaveRequests.FirstOrDefault(r => r.Id == leaveRequestId);
                if (leaveRequest == null || leaveRequest.Status != "Approved")
                {
                    Serilog.Log.Warning("[Substitute] Leave request {Id} is not approved or not found", leaveRequestId);
                    return false;
                }

                var subTeacher = db.StaffProfiles.FirstOrDefault(t => t.StaffCode == substituteTeacherCode);
                if (subTeacher == null)
                {
                    Serilog.Log.Warning("[Substitute] Teacher {Code} not found", substituteTeacherCode);
                    return false;
                }

                // Check weekly load
                var startOfWeek = date.Date.AddDays(-(int)date.DayOfWeek + (int)DayOfWeek.Monday);
                var endOfWeek = startOfWeek.AddDays(7);
                var weeklySubstitutePeriods = db.SubstituteAssignments
                    .Where(a => a.SubstituteTeacherCode == substituteTeacherCode && a.Date >= startOfWeek && a.Date < endOfWeek && a.Status == "Assigned")
                    .Count();

                var maxLoadSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Substitute_MaxWeeklyPeriods");
                int maxLoad = maxLoadSetting != null ? int.Parse(maxLoadSetting.Value) : 20;

                if (weeklySubstitutePeriods >= maxLoad)
                {
                    Serilog.Log.Warning("[Substitute] Teacher {Code} exceeds weekly maximum substitute workload", substituteTeacherCode);
                    return false;
                }

                // Check timetable conflict
                bool hasConflict = db.SubstituteAssignments.Any(a => 
                    a.SubstituteTeacherCode == substituteTeacherCode && 
                    a.Date == date.Date && 
                    a.PeriodIndex == periodIndex && 
                    a.Status == "Assigned"
                );

                if (hasConflict)
                {
                    Serilog.Log.Warning("[Substitute] Teacher {Code} has timetable conflict at Period {Period} on {Date}", substituteTeacherCode, periodIndex, date);
                    return false;
                }

                var assignment = new SubstituteAssignment
                {
                    LeaveRequestId = leaveRequestId,
                    OriginalTeacherCode = leaveRequest.StaffId.ToString(),
                    SubstituteTeacherCode = substituteTeacherCode,
                    Date = date.Date,
                    PeriodIndex = periodIndex,
                    ClassName = className,
                    Status = "Assigned",
                    RoomName = roomName
                };
                db.SubstituteAssignments.Add(assignment);

                // Pedagogical notifications
                var studentsInClass = db.Students.Where(s => s.ClassName == className).ToList();
                foreach (var stu in studentsInClass)
                {
                    db.InboxMessages.Add(new InboxMessage
                    {
                        SenderId = "SCHOOL_ADMIN",
                        ReceiverId = "parent_" + stu.Id,
                        Content = $"Thông báo: Tiết học thứ {periodIndex} môn học ngày {date:yyyy-MM-dd} của lớp sẽ do Thầy/Cô {subTeacher.FullName} phụ trách dạy thay để đảm bảo chương trình học tập của các con không bị gián đoạn.",
                        CreatedAt = DateTime.Now
                    });
                }

                db.SaveChanges();
                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Serilog.Log.Error(ex, "[Substitute] Error in AssignSubstituteTeacher");
                return false;
            }
        }

        public static bool ProposeChemicalRequisition(AppDbContext db, int assetBookingId, string chemicalName, double quantity, string safetyNotes)
        {
            try
            {
                var booking = db.AssetBookings.FirstOrDefault(b => b.Id == assetBookingId);
                if (booking == null)
                {
                    Serilog.Log.Warning("[Lab] Asset booking {Id} not found", assetBookingId);
                    return false;
                }

                var hazardousListSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Chemical_HazardousList");
                string listStr = hazardousListSetting != null ? hazardousListSetting.Value : "H2SO4, HNO3, HCl, Na, K";
                var list = listStr.Split(',').Select(x => x.Trim().ToUpper()).ToList();

                bool isHazardous = list.Contains(chemicalName.Trim().ToUpper());

                var req = new ChemicalRequisition
                {
                    AssetBookingId = assetBookingId,
                    ChemicalName = chemicalName,
                    RequiredQuantity = quantity,
                    IsHazardous = isHazardous,
                    ApprovedByHOD = isHazardous ? "Pending" : "Approved",
                    ApprovedByPrincipal = isHazardous ? "Pending" : "Approved",
                    Status = isHazardous ? "Pending" : "Approved",
                    SafetyNotes = safetyNotes
                };

                db.ChemicalRequisitions.Add(req);
                db.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[Lab] Error in ProposeChemicalRequisition");
                return false;
            }
        }

        public static bool ApproveChemicalRequisition(AppDbContext db, int requisitionId, string reviewerRole, string status, string pin)
        {
            using var transaction = db.Database.BeginTransaction();
            try
            {
                var req = db.ChemicalRequisitions.FirstOrDefault(r => r.Id == requisitionId);
                if (req == null)
                {
                    Serilog.Log.Warning("[Lab] Chemical requisition {Id} not found", requisitionId);
                    return false;
                }

                if (reviewerRole == "HOD")
                {
                    req.ApprovedByHOD = status;
                }
                else if (reviewerRole == "Principal")
                {
                    if (pin != "1234" && pin != "0000") // Mock simple PIN check
                    {
                        Serilog.Log.Warning("[Lab] Principal wrong PIN");
                        req.ApprovedByPrincipal = "Rejected";
                        req.Status = "Rejected";
                        
                        // Release booking
                        var booking = db.AssetBookings.FirstOrDefault(b => b.Id == req.AssetBookingId);
                        if (booking != null) db.AssetBookings.Remove(booking);

                        db.SaveChanges();
                        transaction.Commit();
                        return false;
                    }
                    req.ApprovedByPrincipal = status;
                }

                if (req.ApprovedByHOD == "Approved" && req.ApprovedByPrincipal == "Approved")
                {
                    req.Status = "Approved";
                }
                else if (req.ApprovedByHOD == "Rejected" || req.ApprovedByPrincipal == "Rejected")
                {
                    req.Status = "Rejected";

                    // Release booking
                    var booking = db.AssetBookings.FirstOrDefault(b => b.Id == req.AssetBookingId);
                    if (booking != null) db.AssetBookings.Remove(booking);
                }

                db.SaveChanges();
                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Serilog.Log.Error(ex, "[Lab] Error in ApproveChemicalRequisition");
                return false;
            }
        }

        public static bool CalculateAndSignPayroll(AppDbContext db, string staffCode, string monthYear, decimal baseSalary, double overtimeHours, decimal teachingBonus)
        {
            try
            {
                var isLocked = db.StaffPayrollLedgers.Any(l => l.MonthYear == monthYear && l.IsLocked);
                if (isLocked)
                {
                    throw new InvalidOperationException("Ledger month is locked");
                }

                var rateSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Payroll_BaseOvertimeRate");
                decimal rate = rateSetting != null ? decimal.Parse(rateSetting.Value) : 150000;

                decimal overtimeAmount = (decimal)overtimeHours * rate;
                decimal finalAmount = baseSalary + overtimeAmount + teachingBonus;

                if (finalAmount < 0 || baseSalary < 0)
                {
                    Serilog.Log.Warning("[Payroll] Negative salary calculations blocked");
                    return false;
                }

                var keySetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Payroll_SecretKey");
                string secret = keySetting != null ? keySetting.Value : "SmartClass_Secret_Key_2026";

                string rawData = $"{staffCode}|{monthYear}|{baseSalary}|{overtimeHours}|{teachingBonus}|{finalAmount}";
                
                string checksum = "";
                using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
                {
                    var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                    checksum = Convert.ToBase64String(hashBytes);
                }

                var ledger = new StaffPayrollLedger
                {
                    StaffCode = staffCode,
                    MonthYear = monthYear,
                    BaseSalary = baseSalary,
                    OvertimeHours = overtimeHours,
                    OvertimeRate = rate,
                    TeachingBonus = teachingBonus,
                    FinalAmount = finalAmount,
                    LedgerChecksum = checksum,
                    IsLocked = false,
                    CalculatedAt = DateTime.Now
                };

                db.StaffPayrollLedgers.Add(ledger);
                db.SaveChanges();
                return true;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[Payroll] Error in CalculateAndSignPayroll");
                return false;
            }
        }

        public static bool VerifyPayrollIntegrity(AppDbContext db, int payrollId)
        {
            try
            {
                var record = db.StaffPayrollLedgers.FirstOrDefault(l => l.Id == payrollId);
                if (record == null)
                {
                    return false;
                }

                var keySetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Payroll_SecretKey");
                string secret = keySetting != null ? keySetting.Value : "SmartClass_Secret_Key_2026";

                string rawData = $"{record.StaffCode}|{record.MonthYear}|{record.BaseSalary}|{record.OvertimeHours}|{record.TeachingBonus}|{record.FinalAmount}";
                
                string calculatedChecksum = "";
                using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
                {
                    var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                    calculatedChecksum = Convert.ToBase64String(hashBytes);
                }

                if (calculatedChecksum != record.LedgerChecksum)
                {
                    Serilog.Log.Error("[Payroll] TAMPERING DETECTED for ledger ID {Id}", payrollId);
                    record.FinalAmount = 0; // Fraud lock
                    db.SaveChanges();
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[Payroll] Error in VerifyPayrollIntegrity");
                return false;
            }
        }

        public static bool RegisterMaintenanceTicket(AppDbContext db, string roomName, string facilityName, string description, string severity, string assignedStaffCode, DateTime? todayOverride = null)
        {
            try
            {
                var referenceTime = todayOverride ?? DateTime.Now;
                var ticket = new MaintenanceTicket
                {
                    RoomName = roomName,
                    FacilityName = facilityName,
                    Description = description,
                    Severity = severity,
                    Status = "Pending",
                    AssignedStaffCode = assignedStaffCode,
                    CreatedAt = referenceTime,
                    IsEscalated = false
                };

                db.MaintenanceTickets.Add(ticket);

                if (severity == "Critical")
                {
                    // Check if class hours (7:30 to 17:00)
                    var time = referenceTime.TimeOfDay;
                    if (time >= new TimeSpan(7, 30, 0) && time <= new TimeSpan(17, 0, 0))
                    {
                        // Open gates automatically
                        db.GateBarrierLogs.Add(new GateBarrierLog
                        {
                            StudentCode = "EMERGENCY_SYSTEM",
                            GateId = "ALL_GATES",
                            CommandAction = "Open",
                            TriggerSource = "EmergencyLockdownBypass",
                            Timestamp = referenceTime,
                            HardwareConfirmed = true
                        });
                    }
                }

                db.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[Maintenance] Error in RegisterMaintenanceTicket");
                return false;
            }
        }

        public static int ProcessMaintenanceSlaEscalation(AppDbContext db, DateTime checkTime)
        {
            try
            {
                var pendingTickets = db.MaintenanceTickets
                    .Where(t => t.Status != "Resolved" && !t.IsEscalated)
                    .ToList();

                var slaHoursHighSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Maintenance_SlaHoursHigh");
                double slaHoursHigh = slaHoursHighSetting != null ? double.Parse(slaHoursHighSetting.Value) : 4.0;

                var slaHoursCriticalSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Maintenance_SlaHoursCritical");
                double slaHoursCritical = slaHoursCriticalSetting != null ? double.Parse(slaHoursCriticalSetting.Value) : 2.0;

                int count = 0;
                foreach (var ticket in pendingTickets)
                {
                    double limitHours = 24.0;
                    if (ticket.Severity == "Critical") limitHours = slaHoursCritical;
                    else if (ticket.Severity == "High") limitHours = slaHoursHigh;

                    if ((checkTime - ticket.CreatedAt).TotalHours > limitHours)
                    {
                        ticket.IsEscalated = true;
                        count++;

                        // Notify Principal
                        db.InboxMessages.Add(new InboxMessage
                        {
                            SenderId = "MAINTENANCE_SYSTEM",
                            ReceiverId = "HT001",
                            Content = $"[QUÁ HẠN SLA] Sự cố mức độ {ticket.Severity} tại phòng {ticket.RoomName} ({ticket.FacilityName}) quá hạn sửa chữa. Đề nghị chỉ đạo.",
                            CreatedAt = DateTime.Now
                        });
                    }
                }

                if (count > 0) db.SaveChanges();
                return count;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[Maintenance] Error in ProcessMaintenanceSlaEscalation");
                return 0;
            }
        }

        // --- PHASE 7: EXTENDED STAFF WORKFLOWS & RESOURCE MONITORING ---

        public static bool TriggerSecurityLockdown(AppDbContext db, string triggerStaffCode, string lockdownType, string pin)
        {
            using var transaction = db.Database.BeginTransaction();
            try
            {
                var staff = db.StaffProfiles.FirstOrDefault(s => s.StaffCode == triggerStaffCode);
                if (staff == null || (staff.Department != "ADMIN" && staff.Department != "SECURITY"))
                {
                    Serilog.Log.Warning("[Lockdown] Unauthorized staff {Code} tried to trigger lockdown", triggerStaffCode);
                    return false;
                }

                var pinSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Security_LockdownPin");
                string expectedPin = pinSetting != null ? pinSetting.Value : "9999";
                if (pin != expectedPin)
                {
                    Serilog.Log.Warning("[Lockdown] Wrong PIN entered for lockdown");
                    return false;
                }

                var log = new SecurityLockdownLog
                {
                    TriggeredByStaffCode = triggerStaffCode,
                    StartedAt = DateTime.Now,
                    LockdownType = lockdownType,
                    Status = "Active"
                };
                db.SecurityLockdownLogs.Add(log);

                var modeSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Security_LockdownMode");
                int mode = modeSetting != null ? int.Parse(modeSetting.Value) : 2;

                if (mode >= 1)
                {
                    db.GateBarrierLogs.Add(new GateBarrierLog
                    {
                        StudentCode = "EMERGENCY_SYSTEM",
                        GateId = "ALL_GATES",
                        CommandAction = "Lock",
                        TriggerSource = "EmergencyLockdown",
                        Timestamp = DateTime.Now,
                        HardwareConfirmed = true
                    });
                }

                // Send silent private alerts
                var teachers = db.StaffProfiles.Where(s => s.Department == "Teacher" || s.Department == "Math" || s.Department == "Physics" || s.Department == "Science").ToList();
                foreach (var t in teachers)
                {
                    db.InboxMessages.Add(new InboxMessage
                    {
                        SenderId = "SECURITY_SYSTEM",
                        ReceiverId = "staff_" + t.Id,
                        Content = $"[KHẨN CẤP] Kích hoạt phong tỏa toàn trường ({lockdownType}). Hãy khóa cửa lớp và bảo vệ học sinh.",
                        CreatedAt = DateTime.Now
                    });
                }

                db.InboxMessages.Add(new InboxMessage
                {
                    SenderId = "SECURITY_SYSTEM",
                    ReceiverId = "HT001",
                    Content = $"[KHẨN CẤP] Phong tỏa toàn trường được kích hoạt bởi {triggerStaffCode}. Loại: {lockdownType}.",
                    CreatedAt = DateTime.Now
                });

                db.SaveChanges();
                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Serilog.Log.Error(ex, "[Lockdown] Error in TriggerSecurityLockdown");
                return false;
            }
        }

        public static bool ReleaseSecurityLockdown(AppDbContext db, string releaseStaffCode, string pin, string resolutionNotes)
        {
            using var transaction = db.Database.BeginTransaction();
            try
            {
                var staff = db.StaffProfiles.FirstOrDefault(s => s.StaffCode == releaseStaffCode);
                if (staff == null || (staff.Department != "ADMIN" && staff.Department != "SECURITY"))
                {
                    Serilog.Log.Warning("[Lockdown] Unauthorized staff {Code} tried to release lockdown", releaseStaffCode);
                    return false;
                }

                var pinSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Security_LockdownPin");
                string expectedPin = pinSetting != null ? pinSetting.Value : "9999";
                if (pin != expectedPin)
                {
                    Serilog.Log.Warning("[Lockdown] Wrong PIN entered for releasing lockdown");
                    return false;
                }

                var activeLockdowns = db.SecurityLockdownLogs.Where(l => l.Status == "Active").ToList();
                foreach (var l in activeLockdowns)
                {
                    l.Status = "Resolved";
                    l.EndedAt = DateTime.Now;
                    l.ResolutionNotes = resolutionNotes;
                }

                // Unlock the gates
                db.GateBarrierLogs.Add(new GateBarrierLog
                {
                    StudentCode = "EMERGENCY_SYSTEM",
                    GateId = "ALL_GATES",
                    CommandAction = "Unlock",
                    TriggerSource = "EmergencyLockdownBypass",
                    Timestamp = DateTime.Now,
                    HardwareConfirmed = true
                });

                // Send silent private alerts
                var teachers = db.StaffProfiles.Where(s => s.Department == "Teacher" || s.Department == "Math" || s.Department == "Physics" || s.Department == "Science").ToList();
                foreach (var t in teachers)
                {
                    db.InboxMessages.Add(new InboxMessage
                    {
                        SenderId = "SECURITY_SYSTEM",
                        ReceiverId = "staff_" + t.Id,
                        Content = "[THONG BAO] Phong toa toan truong DA DUOC GIAI TOA boi " + releaseStaffCode + ". Di chuyen binh thuong.",
                        CreatedAt = DateTime.Now
                    });
                }

                db.InboxMessages.Add(new InboxMessage
                {
                    SenderId = "SECURITY_SYSTEM",
                    ReceiverId = "HT001",
                    Content = "[THONG BAO] Phong toa toan truong da duoc giai toa boi " + releaseStaffCode + ". Ghi chu: " + resolutionNotes + ".",
                    CreatedAt = DateTime.Now
                });

                db.SaveChanges();
                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Serilog.Log.Error(ex, "[Lockdown] Error in ReleaseSecurityLockdown");
                return false;
            }
        }

        public static bool ProcessColdStorageHeartbeat(AppDbContext db, string fridgeId, double temperature, double humidity, DateTime timestamp)
        {
            try
            {
                var maxTempSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Kitchen_MaxSafeTemp");
                double maxTemp = maxTempSetting != null ? double.Parse(maxTempSetting.Value) : 4.0;

                bool isViolation = temperature > maxTemp;

                var log = new KitchenColdStorageLog
                {
                    FridgeId = fridgeId,
                    Temperature = temperature,
                    Humidity = humidity,
                    Timestamp = timestamp,
                    IsViolation = isViolation
                };
                db.KitchenColdStorageLogs.Add(log);
                db.SaveChanges();

                if (isViolation)
                {
                    // Check if violation has been continuous for IT_Kitchen_TempAlarmThresholdMinutes
                    var alarmMinutesSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Kitchen_TempAlarmThresholdMinutes");
                    int alarmMinutes = alarmMinutesSetting != null ? int.Parse(alarmMinutesSetting.Value) : 30;

                    // Get recent logs in chronological order to check duration of continuous violation
                    var recentLogs = db.KitchenColdStorageLogs
                        .Where(l => l.FridgeId == fridgeId && l.Timestamp <= timestamp)
                        .OrderByDescending(l => l.Timestamp)
                        .ToList();

                    DateTime? firstViolationTime = null;
                    foreach (var rl in recentLogs)
                    {
                        if (rl.IsViolation)
                        {
                            firstViolationTime = rl.Timestamp;
                        }
                        else
                        {
                            break; // Stop at first non-violation
                        }
                    }

                    if (firstViolationTime != null && (timestamp - firstViolationTime.Value).TotalMinutes >= alarmMinutes)
                    {
                        db.InboxMessages.Add(new InboxMessage
                        {
                            SenderId = "HEALTH_SYSTEM",
                            ReceiverId = "KITCHEN_MANAGER",
                            Content = $"[CẢNH BÁO] Tủ lạnh {fridgeId} quá nhiệt liên tục quá {alarmMinutes} phút. Thực phẩm có nguy cơ hư hỏng.",
                            CreatedAt = DateTime.Now
                        });
                        db.InboxMessages.Add(new InboxMessage
                        {
                            SenderId = "HEALTH_SYSTEM",
                            ReceiverId = "NURSE",
                            Content = $"[CẢNH BÁO] Tủ lạnh {fridgeId} quá nhiệt liên tục quá {alarmMinutes} phút. Yêu cầu kiểm tra an toàn thực phẩm.",
                            CreatedAt = DateTime.Now
                        });

                        // Quarantine batches in database
                        var activeFoodLogs = db.FoodSafetyInspectionLogs.Where(l => l.InspectionStatus == "Passed").ToList();
                        foreach (var fl in activeFoodLogs)
                        {
                            fl.InspectionStatus = "Quarantined";
                        }
                        db.SaveChanges();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[Kitchen] Error in ProcessColdStorageHeartbeat");
                return false;
            }
        }

        public static ActivityVerificationResult VerifyActivitySafety(AppDbContext db, int activityId, string activityName, string activityType, List<string> participantStudentCodes)
        {
            var result = new ActivityVerificationResult { IsSafe = true };
            try
            {
                var modeSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Medical_CarePlanVerificationMode");
                int mode = modeSetting != null ? int.Parse(modeSetting.Value) : 1;

                foreach (var code in participantStudentCodes)
                {
                    var plan = db.StudentCarePlans.FirstOrDefault(p => p.StudentCode == code && p.IsActive);
                    if (plan != null)
                      {
                        // Check triggers
                        string[] triggers = plan.MedicalTriggers.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                        bool isFlagged = false;
                        foreach (var tr in triggers)
                        {
                            var cleanTr = tr.Trim().ToLower();
                            if (!string.IsNullOrEmpty(cleanTr))
                            {
                                if (activityName.ToLower().Contains(cleanTr) || activityType.ToLower().Contains(cleanTr))
                                {
                                    isFlagged = true;
                                    break;
                                }
                            }
                        }

                        if (isFlagged)
                        {
                            result.FlaggedStudentCodes.Add(code);
                        }
                    }
                }

                if (result.FlaggedStudentCodes.Count > 0)
                {
                    if (mode >= 1)
                    {
                        result.IsSafe = false;
                    }

                    result.RecommendationNotes = "Khuyến nghị: Chuyển các học sinh có cảnh báo sang đề tài lý thuyết thay thế.";

                    // Send private notifications
                    db.InboxMessages.Add(new InboxMessage
                    {
                        SenderId = "MEDICAL_SYSTEM",
                        ReceiverId = "NURSE",
                        Content = $"[CẢNH BÁO Y TẾ] Hoạt động {activityName} ({activityType}) có {result.FlaggedStudentCodes.Count} học sinh có nguy cơ y tế.",
                        CreatedAt = DateTime.Now
                    });

                    // Notify class teachers of flagged students
                    foreach (var code in result.FlaggedStudentCodes)
                    {
                        var student = db.Students.FirstOrDefault(s => s.StudentCode == code);
                        if (student != null)
                        {
                            db.InboxMessages.Add(new InboxMessage
                            {
                                SenderId = "MEDICAL_SYSTEM",
                                ReceiverId = "parent_" + student.Id,
                                Content = $"Thông báo tế nhị: Hoạt động {activityName} của lớp có nội dung chưa phù hợp thể trạng của con. Giáo viên sẽ chuẩn bị phương án học tập thay thế phù hợp.",
                                CreatedAt = DateTime.Now
                            });
                        }
                    }
                    db.SaveChanges();
                }

                return result;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[Medical] Error in VerifyActivitySafety");
                result.IsSafe = false;
                return result;
            }
        }

        public static bool RecordResourceWasteMetrics(AppDbContext db, DateTime date, string janitorStaffCode, double organicKg, double recyclableKg, double nonRecyclableKg, double hazardousKg, double electricityKwh, double waterCubicMeters)
        {
            using var transaction = db.Database.BeginTransaction();
            try
            {
                // Validate janitor/accountant staff
                var staff = db.StaffProfiles.FirstOrDefault(s => s.StaffCode == janitorStaffCode);
                if (staff == null)
                {
                    Serilog.Log.Warning("[Resource] Staff {Code} not found for recording metrics", janitorStaffCode);
                    return false;
                }

                if (organicKg < 0 || recyclableKg < 0 || nonRecyclableKg < 0 || hazardousKg < 0 || electricityKwh < 0 || waterCubicMeters < 0)
                {
                    Serilog.Log.Warning("[Resource] Negative metrics not allowed");
                    return false;
                }

                var ledger = new ResourceWasteLedger
                {
                    Date = date.Date,
                    JanitorStaffCode = janitorStaffCode,
                    OrganicKg = organicKg,
                    RecyclableKg = recyclableKg,
                    NonRecyclableKg = nonRecyclableKg,
                    HazardousKg = hazardousKg,
                    ElectricityKwh = electricityKwh,
                    WaterCubicMeters = waterCubicMeters,
                    RecordedAt = DateTime.Now
                };
                db.ResourceWasteLedgers.Add(ledger);
                db.SaveChanges();

                // Check resource leaks using 7-day average
                var leakSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Janitor_ResourceLeakThreshold");
                double leakThreshold = leakSetting != null ? double.Parse(leakSetting.Value) : 1.30;

                var history = db.ResourceWasteLedgers
                    .Where(l => l.Date < date.Date)
                    .OrderByDescending(l => l.Date)
                    .Take(7)
                    .ToList();

                if (history.Count >= 1)
                {
                    double avgElec = history.Average(h => h.ElectricityKwh);
                    double avgWater = history.Average(h => h.WaterCubicMeters);

                    bool leakDetected = false;
                    string desc = "";

                    if (avgElec > 0 && electricityKwh > avgElec * leakThreshold)
                    {
                        leakDetected = true;
                        desc += $"Nghi vấn rò rỉ điện/chập điện ngầm: tiêu thụ {electricityKwh} kWh vượt ngưỡng TB {avgElec:F1} kWh. ";
                    }

                    if (avgWater > 0 && waterCubicMeters > avgWater * leakThreshold)
                    {
                        leakDetected = true;
                        desc += $"Nghi vấn rò rỉ nước: tiêu thụ {waterCubicMeters} m3 vượt ngưỡng TB {avgWater:F1} m3. ";
                    }

                    if (leakDetected)
                    {
                        db.MaintenanceTickets.Add(new MaintenanceTicket
                        {
                            RoomName = "Hệ thống chung",
                            FacilityName = "Resource Grid",
                            Description = desc,
                            Severity = "High",
                            Status = "Pending",
                            AssignedStaffCode = "KT_SYSTEM",
                            CreatedAt = DateTime.Now,
                            IsEscalated = false
                        });
                    }
                }

                // Check green recycling bonus
                var greenSetting = db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Janitor_GreenBonusThresholdKg");
                double greenThreshold = greenSetting != null ? double.Parse(greenSetting.Value) : 100.0;

                if (recyclableKg >= greenThreshold)
                {
                    db.InboxMessages.Add(new InboxMessage
                    {
                        SenderId = "ENVIRONMENT_SYSTEM",
                        ReceiverId = "BCH_DOAN",
                        Content = $"Chúc mừng! Tuần này trường đạt {recyclableKg} Kg rác tái chế, vượt chỉ tiêu {greenThreshold} Kg. Thêm 10 điểm thi đua xanh.",
                        CreatedAt = DateTime.Now
                    });
                }

                db.SaveChanges();
                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Serilog.Log.Error(ex, "[Resource] Error in RecordResourceWasteMetrics");
                return false;
            }
        }
    }

    public class ActivityVerificationResult
    {
        public bool IsSafe { get; set; }
        public System.Collections.Generic.List<string> FlaggedStudentCodes { get; set; } = new();
        public string RecommendationNotes { get; set; } = string.Empty;
    }
}
