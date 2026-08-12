using System.Collections.Generic;

namespace QASmartClass.LearningTools.Views.Multi
{
    /// <summary>
    /// 20 mẫu Bản đồ tư duy có sẵn cho giáo viên và học sinh.
    /// Mỗi mẫu gồm: Tên, Danh mục, Icon, Chủ đề gốc, và các nhánh đa cấp.
    /// </summary>
    public static class MindmapTemplates
    {
        public class TemplateNode
        {
            public string Text { get; set; } = "";
            public List<TemplateNode> Children { get; set; } = new();
        }

        public class Template
        {
            public string Name { get; set; } = "";
            public string Category { get; set; } = "";
            public string Icon { get; set; } = "📄";
            public TemplateNode Root { get; set; } = new();
        }

        public static List<Template> GetAll()
        {
            return new List<Template>
            {
                // ===== GIÁO DỤC (1-8) =====
                T01_VanHoc(), T02_SinhHoc(), T03_LichSu(), T04_TiengAnh(),
                T05_ToanHoc(), T06_HoaHoc(), T07_VatLy(), T08_DiaLy(),

                // ===== DOANH NGHIỆP (9-14) =====
                T09_SWOT(), T10_Marketing(), T11_CoCauPhongBan(), T12_KPI(),
                T13_RaMat_SanPham(), T14_Brainstorm(),

                // ===== CÁ NHÂN (15-18) =====
                T15_MucTieuNam(), T16_DuLich(), T17_TaiChinh(), T18_SucKhoe(),

                // ===== DỰ ÁN & KẾ HOẠCH (19-20) =====
                T19_DuAn(), T20_KeHoachTuan()
            };
        }

        // ---------- GIÁO DỤC ----------

        static Template T01_VanHoc() => new()
        {
            Name = "Phân tích tác phẩm Văn học", Category = "Giáo dục", Icon = "📚",
            Root = N("Phân tích tác phẩm",
                N("Tác giả & Hoàn cảnh", N("Tiểu sử"), N("Bối cảnh xã hội"), N("Phong cách sáng tác")),
                N("Nội dung chính", N("Tóm tắt cốt truyện"), N("Nhân vật chính"), N("Xung đột")),
                N("Nghệ thuật", N("Biện pháp tu từ"), N("Ngôn ngữ"), N("Kết cấu")),
                N("Ý nghĩa & Thông điệp", N("Giá trị nhân đạo"), N("Bài học rút ra")))
        };

        static Template T02_SinhHoc() => new()
        {
            Name = "Cấu trúc Tế bào Sinh học", Category = "Giáo dục", Icon = "🔬",
            Root = N("Tế bào",
                N("Tế bào Nhân sơ", N("Vi khuẩn"), N("Vi khuẩn cổ")),
                N("Tế bào Nhân thực", N("Tế bào Động vật"), N("Tế bào Thực vật")),
                N("Bào quan", N("Nhân"), N("Ti thể"), N("Lục lạp"), N("Ribosome")),
                N("Chức năng", N("Trao đổi chất"), N("Sinh sản"), N("Cảm ứng")))
        };

        static Template T03_LichSu() => new()
        {
            Name = "Tóm tắt sự kiện Lịch sử", Category = "Giáo dục", Icon = "🏛️",
            Root = N("Sự kiện lịch sử",
                N("Hoàn cảnh", N("Trong nước"), N("Quốc tế")),
                N("Nguyên nhân", N("Trực tiếp"), N("Sâu xa")),
                N("Diễn biến", N("Giai đoạn 1"), N("Giai đoạn 2"), N("Bước ngoặt")),
                N("Kết quả", N("Thắng lợi"), N("Tổn thất")),
                N("Ý nghĩa", N("Lịch sử"), N("Bài học kinh nghiệm")))
        };

        static Template T04_TiengAnh() => new()
        {
            Name = "Từ vựng Tiếng Anh theo chủ đề", Category = "Giáo dục", Icon = "🌍",
            Root = N("English Vocabulary",
                N("Family", N("Father"), N("Mother"), N("Siblings")),
                N("School", N("Classroom"), N("Teacher"), N("Subjects")),
                N("Food & Drink", N("Fruits"), N("Vegetables"), N("Beverages")),
                N("Travel", N("Airport"), N("Hotel"), N("Sightseeing")))
        };

        static Template T05_ToanHoc() => new()
        {
            Name = "Phân loại Toán học", Category = "Giáo dục", Icon = "📐",
            Root = N("Toán học",
                N("Đại số", N("Phương trình"), N("Bất phương trình"), N("Hệ phương trình")),
                N("Hình học", N("Tam giác"), N("Tứ giác"), N("Hình tròn")),
                N("Lượng giác", N("Hàm sin/cos"), N("Công thức biến đổi")),
                N("Giải tích", N("Giới hạn"), N("Đạo hàm"), N("Tích phân")),
                N("Thống kê", N("Xác suất"), N("Trung bình"), N("Phương sai")))
        };

        static Template T06_HoaHoc() => new()
        {
            Name = "Bảng tuần hoàn & Phản ứng", Category = "Giáo dục", Icon = "⚗️",
            Root = N("Hóa học",
                N("Nguyên tử", N("Proton"), N("Neutron"), N("Electron")),
                N("Liên kết hóa học", N("Ion"), N("Cộng hóa trị"), N("Kim loại")),
                N("Phản ứng", N("Oxi hóa - Khử"), N("Acid - Base"), N("Trao đổi")),
                N("Hữu cơ", N("Hydrocarbon"), N("Alcohol"), N("Acid hữu cơ")))
        };

        static Template T07_VatLy() => new()
        {
            Name = "Các lĩnh vực Vật lí", Category = "Giáo dục", Icon = "⚡",
            Root = N("Vật lí",
                N("Cơ học", N("Động học"), N("Động lực học"), N("Năng lượng")),
                N("Nhiệt học", N("Nhiệt độ"), N("Truyền nhiệt")),
                N("Điện học", N("Dòng điện"), N("Điện trường"), N("Mạch điện")),
                N("Quang học", N("Phản xạ"), N("Khúc xạ"), N("Giao thoa")))
        };

        static Template T08_DiaLy() => new()
        {
            Name = "Địa lí Việt Nam", Category = "Giáo dục", Icon = "🗺️",
            Root = N("Địa lí Việt Nam",
                N("Vị trí & Lãnh thổ", N("Tọa độ"), N("Diện tích"), N("Biên giới")),
                N("Địa hình", N("Đồi núi"), N("Đồng bằng"), N("Bờ biển")),
                N("Khí hậu", N("Nhiệt đới gió mùa"), N("Bão lũ")),
                N("Dân cư", N("Dân số"), N("54 dân tộc"), N("Phân bố")))
        };

        // ---------- DOANH NGHIỆP ----------

        static Template T09_SWOT() => new()
        {
            Name = "Phân tích SWOT", Category = "Doanh nghiệp", Icon = "📊",
            Root = N("Phân tích SWOT",
                N("Strengths (Điểm mạnh)", N("Nhân lực giỏi"), N("Thương hiệu uy tín"), N("Công nghệ tiên tiến")),
                N("Weaknesses (Điểm yếu)", N("Thiếu vốn"), N("Quy trình chưa tối ưu")),
                N("Opportunities (Cơ hội)", N("Thị trường mở rộng"), N("Chuyển đổi số")),
                N("Threats (Thách thức)", N("Đối thủ cạnh tranh"), N("Biến động kinh tế")))
        };

        static Template T10_Marketing() => new()
        {
            Name = "Chiến lược Marketing 4P", Category = "Doanh nghiệp", Icon = "📣",
            Root = N("Marketing Mix 4P",
                N("Product (Sản phẩm)", N("Tính năng"), N("Chất lượng"), N("Đóng gói")),
                N("Price (Giá)", N("Chiến lược giá"), N("Khuyến mãi"), N("Chiết khấu")),
                N("Place (Phân phối)", N("Kênh online"), N("Cửa hàng"), N("Đại lý")),
                N("Promotion (Quảng bá)", N("Quảng cáo"), N("PR"), N("Social Media")))
        };

        static Template T11_CoCauPhongBan() => new()
        {
            Name = "Cơ cấu Tổ chức Công ty", Category = "Doanh nghiệp", Icon = "🏢",
            Root = N("Giám đốc điều hành (CEO)",
                N("Phó GĐ Kinh doanh", N("P. Bán hàng"), N("P. Marketing"), N("P. CSKH")),
                N("Phó GĐ Kỹ thuật", N("P. R&D"), N("P. IT"), N("P. QC")),
                N("Phó GĐ Tài chính", N("P. Kế toán"), N("P. Tài chính")),
                N("P. Nhân sự", N("Tuyển dụng"), N("Đào tạo"), N("C&B")))
        };

        static Template T12_KPI() => new()
        {
            Name = "Hệ thống KPI / OKR", Category = "Doanh nghiệp", Icon = "🎯",
            Root = N("KPI Quý 3/2026",
                N("Doanh thu", N("Mục tiêu: 5 tỷ"), N("Thực tế: ___"), N("Đạt: ___%")),
                N("Khách hàng mới", N("Mục tiêu: 200"), N("Kênh Online"), N("Kênh Offline")),
                N("Chất lượng SP", N("Tỷ lệ lỗi < 2%"), N("CSAT > 4.5")),
                N("Nhân sự", N("Tuyển mới: 10"), N("Đào tạo: 40h/người")))
        };

        static Template T13_RaMat_SanPham() => new()
        {
            Name = "Kế hoạch Ra mắt Sản phẩm", Category = "Doanh nghiệp", Icon = "🚀",
            Root = N("Ra mắt sản phẩm mới",
                N("Nghiên cứu thị trường", N("Khảo sát NTD"), N("Phân tích đối thủ")),
                N("Phát triển SP", N("Thiết kế"), N("Prototype"), N("Testing")),
                N("Marketing Launch", N("Teaser Campaign"), N("Event ra mắt"), N("KOL/Influencer")),
                N("Sau ra mắt", N("Thu thập Feedback"), N("Cải tiến V2")))
        };

        static Template T14_Brainstorm() => new()
        {
            Name = "Brainstorming Ý tưởng", Category = "Doanh nghiệp", Icon = "💡",
            Root = N("Ý tưởng mới",
                N("Vấn đề cần giải quyết", N("Pain Point 1"), N("Pain Point 2")),
                N("Giải pháp đề xuất", N("Giải pháp A"), N("Giải pháp B"), N("Giải pháp C")),
                N("Đánh giá khả thi", N("Chi phí"), N("Thời gian"), N("Nguồn lực")),
                N("Bước tiếp theo", N("Thử nghiệm"), N("Triển khai")))
        };

        // ---------- CÁ NHÂN ----------

        static Template T15_MucTieuNam() => new()
        {
            Name = "Mục tiêu Năm mới", Category = "Cá nhân", Icon = "🎆",
            Root = N("Mục tiêu 2026",
                N("Sức khỏe", N("Tập GYM 4 buổi/tuần"), N("Giảm 5kg"), N("Ngủ trước 23h")),
                N("Sự nghiệp", N("Thăng chức"), N("Học thêm chứng chỉ"), N("Tăng thu nhập 30%")),
                N("Học tập", N("Đọc 24 cuốn sách"), N("Học tiếng Anh IELTS 7.0")),
                N("Gia đình", N("Du lịch 2 chuyến"), N("Tiết kiệm 100 triệu")))
        };

        static Template T16_DuLich() => new()
        {
            Name = "Lên kế hoạch Du lịch", Category = "Cá nhân", Icon = "✈️",
            Root = N("Kế hoạch Du lịch Đà Lạt",
                N("Chuẩn bị", N("Đặt vé máy bay"), N("Đặt khách sạn"), N("Chuẩn bị hành lý")),
                N("Lịch trình", N("Ngày 1: Hồ Xuân Hương"), N("Ngày 2: Langbiang"), N("Ngày 3: Chợ đêm")),
                N("Chi phí", N("Vé: 3 triệu"), N("Khách sạn: 2 triệu"), N("Ăn uống: 1.5 triệu")),
                N("Ghi chú", N("Mang áo ấm"), N("Thuê xe máy")))
        };

        static Template T17_TaiChinh() => new()
        {
            Name = "Quản lý Tài chính Cá nhân", Category = "Cá nhân", Icon = "💰",
            Root = N("Tài chính Cá nhân",
                N("Thu nhập", N("Lương chính"), N("Thu nhập phụ"), N("Đầu tư")),
                N("Chi tiêu", N("Nhà ở"), N("Ăn uống"), N("Đi lại"), N("Giải trí")),
                N("Tiết kiệm", N("Quỹ khẩn cấp"), N("Tiết kiệm dài hạn")),
                N("Đầu tư", N("Chứng khoán"), N("Bất động sản"), N("Vàng")))
        };

        static Template T18_SucKhoe() => new()
        {
            Name = "Chế độ Sống Khỏe Mạnh", Category = "Cá nhân", Icon = "🏃",
            Root = N("Sống Khỏe Mạnh",
                N("Vận động", N("Cardio 30p/ngày"), N("Yoga"), N("Đi bộ 10.000 bước")),
                N("Dinh dưỡng", N("Ăn rau xanh"), N("Uống 2L nước"), N("Hạn chế đường")),
                N("Tinh thần", N("Thiền 10p/ngày"), N("Đọc sách"), N("Ngủ đủ 7-8h")),
                N("Khám sức khỏe", N("Tổng quát 6 tháng/lần"), N("Nha khoa")))
        };

        // ---------- DỰ ÁN & KẾ HOẠCH ----------

        static Template T19_DuAn() => new()
        {
            Name = "Quản lý Dự án Phần mềm", Category = "Dự án", Icon = "💻",
            Root = N("Dự án Phần mềm XYZ",
                N("Phân tích yêu cầu", N("User Story"), N("Wireframe"), N("Acceptance Criteria")),
                N("Thiết kế", N("Database"), N("API"), N("UI/UX")),
                N("Phát triển", N("Sprint 1"), N("Sprint 2"), N("Sprint 3")),
                N("Kiểm thử", N("Unit Test"), N("Integration Test"), N("UAT")),
                N("Triển khai", N("Staging"), N("Production"), N("Monitoring")))
        };

        static Template T20_KeHoachTuan() => new()
        {
            Name = "Kế hoạch Học tập Tuần", Category = "Dự án", Icon = "🗓️",
            Root = N("Kế hoạch tuần",
                N("Thứ 2 - Thứ 3", N("Toán: Chương 5"), N("Văn: Đọc tác phẩm")),
                N("Thứ 4 - Thứ 5", N("Anh: Luyện nghe"), N("Vật lí: Bài tập")),
                N("Thứ 6 - Thứ 7", N("Ôn tập tổng hợp"), N("Làm đề thi thử")),
                N("Chủ Nhật", N("Nghỉ ngơi"), N("Đọc sách ngoài")),
                N("Mục tiêu tuần", N("Hoàn thành 100% BT"), N("Điểm kiểm tra > 8")))
        };

        // ---------- HELPER ----------
        static TemplateNode N(string text, params TemplateNode[] children)
        {
            var node = new TemplateNode { Text = text };
            if (children != null)
                node.Children.AddRange(children);
            return node;
        }
    }
}
