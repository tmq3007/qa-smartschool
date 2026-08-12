using System;

namespace QASmartClass.Data
{
    public partial class Student
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public int ClassroomId { get; set; }
        /// <summary>HMACSHA512 salted hash format: "salt:hash"</summary>
        public string PasswordHash { get; set; } = string.Empty;
        public string PCName { get; set; } = string.Empty;
        public string IPAddress { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public DateTime LastSeen { get; set; }
        public string AvatarPath { get; set; } = string.Empty;

        // Identity fields
        /// <summary>Tên trường (VD: THPT QA)</summary>
        public string SchoolName { get; set; } = string.Empty;
        /// <summary>Tên lớp (VD: 10A3)</summary>
        public string ClassName { get; set; } = string.Empty;
        /// <summary>Khóa (VD: K43)</summary>
        public string Cohort { get; set; } = string.Empty;
        /// <summary>HS = Học sinh, CựuHS = Cựu học sinh, PH = Phụ huynh</summary>
        public string Role { get; set; } = "HS";
        /// <summary>Active, Graduated, Transferred</summary>
        public string Status { get; set; } = "Active";
        /// <summary>Tên phụ huynh</summary>
        public string ParentName { get; set; } = string.Empty;
        /// <summary>SĐT phụ huynh</summary>
        public string ParentPhone { get; set; } = string.Empty;

        /// <summary>Điểm hạnh kiểm (mặc định 100)</summary>
        public int ConductScore { get; set; } = 100;

        /// <summary>Tổng XP tích lũy từ Game + Reward</summary>
        public int TotalXp { get; set; } = 0;

        /// <summary>Số dư ví Canteen POS</summary>
        public decimal WalletBalance { get; set; } = 0.0m;

        public decimal LowBalanceThreshold { get; set; } = 30000m;

        public string WalletChecksum { get; set; } = string.Empty;

        public string Gender { get; set; } = "Nam";
        public string Ethnicity { get; set; } = "Kinh";

        public string ChatIdentity
        {
            get
            {
                var parts = new System.Collections.Generic.List<string>();

                if (!string.IsNullOrWhiteSpace(SchoolName))
                    parts.Add(SchoolName.Trim());
                if (!string.IsNullOrWhiteSpace(ClassName))
                    parts.Add($"Lớp{ClassName.Trim()}");

                if (Status == "Graduated")
                    parts.Add("CựuHS");
                else
                    parts.Add(string.IsNullOrWhiteSpace(Role) ? "HS" : Role);

                if (!string.IsNullOrWhiteSpace(Cohort))
                    parts.Add(Cohort.Trim());

                parts.Add(FullName);
                return string.Join("_", parts);
            }
        }

        public string ShortIdentity
        {
            get
            {
                var cls = string.IsNullOrWhiteSpace(ClassName) ? "" : $"[{ClassName}] ";
                var role = Status == "Graduated" ? "CựuHS" : (Role == "PH" ? "PH" : "");
                var roleStr = string.IsNullOrEmpty(role) ? "" : $"({role}) ";
                return $"{cls}{roleStr}{FullName}";
            }
        }

        public bool IsAtRisk { get; set; } = false;
        public string RiskReason { get; set; } = string.Empty;

        /// <summary>Tên hiển thị ngắn: "Nguyễn Văn A (10A3)"</summary>
        public string DisplayName => string.IsNullOrWhiteSpace(ClassName)
            ? FullName
            : $"{FullName} ({ClassName})";

        /// <summary>Kiểm tra HS còn đang học</summary>
        public bool IsActive => Status == "Active";

        /// <summary>Kiểm tra đã tốt nghiệp</summary>
        public bool IsGraduated => Status == "Graduated";

        /// <summary>Chuỗi đăng nhập hàng ngày</summary>
        public int DailyStreak { get; set; } = 0;

        /// <summary>Ngày cuối hoạt động để tính Streak</summary>
        public DateTime? LastActiveDate { get; set; }

        /// <summary>Số máy / Số ghế ngồi trong phòng học (Không map trực tiếp vào bảng Student mà lấy qua liên kết lớp)</summary>
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public int SeatNumber { get; set; } = 0;

        public double PositionX { get; set; } = 0.0;
        public double PositionY { get; set; } = 0.0;

        public int StudentLexileScore { get; set; } = 400; // Trình độ đọc Lexile hiện tại của học sinh
        public DateTime? ReservationBlockedUntil { get; set; } // Ngày hết hạn khóa quyền đặt trước sách
        public int ConsecutiveSuccessfulPickups { get; set; } = 0; // Đếm số lần đặt sách thành công liên tiếp (để tính thưởng)
    }
}
