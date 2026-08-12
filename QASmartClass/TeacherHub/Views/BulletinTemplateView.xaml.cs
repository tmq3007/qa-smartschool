using QASmartClass.Data;
using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace QASmartClass.TeacherHub.Views
{
    public partial class BulletinTemplateView : Window
    {
        private readonly AppDbContext _db;
        public BulletinTemplate? SelectedTemplate { get; private set; }

        public BulletinTemplateView()
        {
            InitializeComponent();
            _db = new AppDbContext();
Unloaded += (s, e) => { _db?.Dispose(); };
            Loaded += (_, __) => LoadData();
        }

        private void LoadData()
        {
            try
            {
                // BUG-04 FIX: Chỉ seed khi bảng trống, KHÔNG xóa template của người dùng
                if (!_db.BulletinTemplates.Any())
                {
                    _db.BulletinTemplates.AddRange(
                        new BulletinTemplate 
                        { 
                            Name = "Nghỉ lễ Quốc Khánh 2/9", Category = "Holiday", 
                            HtmlContent = "<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph FontSize=\"22\" FontWeight=\"Bold\" Foreground=\"#D97706\" TextAlignment=\"Center\">THÔNG BÁO NGHỈ LỄ QUỐC KHÁNH 2/9</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">Kính gửi toàn thể Cán bộ, Giáo viên và Học sinh,</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">Nhà trường trân trọng thông báo lịch nghỉ lễ Quốc Khánh 2/9 như sau:</Paragraph><List MarkerStyle=\"Disc\"><ListItem><Paragraph FontSize=\"16\" Foreground=\"#334155\">Thời gian nghỉ: Từ ngày 01/09 đến hết ngày 04/09.</Paragraph></ListItem><ListItem><Paragraph FontSize=\"16\" Foreground=\"#334155\">Thời gian đi học lại: Thứ ba, ngày 05/09.</Paragraph></ListItem></List><Paragraph FontSize=\"16\" FontWeight=\"Bold\" Foreground=\"#EF4444\">Lưu ý:</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">- Học sinh ôn tập bài cũ tại nhà.<LineBreak/>- Giáo viên kiểm tra vệ sinh lớp học trước khi nghỉ.</Paragraph><Paragraph FontSize=\"16\" FontStyle=\"Italic\" Foreground=\"#64748B\">Kính chúc mọi người kỳ nghỉ lễ vui vẻ và an toàn!</Paragraph></FlowDocument>" 
                        },
                        new BulletinTemplate 
                        { 
                            Name = "Kế hoạch Thi Giữa Kỳ", Category = "Exam", 
                            HtmlContent = "<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph FontSize=\"22\" FontWeight=\"Bold\" Foreground=\"#2563EB\" TextAlignment=\"Center\">LỊCH THI GIỮA HỌC KỲ I</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">Ban Giám Hiệu thông báo kế hoạch tổ chức kiểm tra giữa học kỳ I năm học 2026-2027:</Paragraph><Paragraph FontSize=\"16\" FontWeight=\"Bold\" Foreground=\"#0F172A\">1. Khối 10 và 11</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">- Thời gian: Sáng từ 07:30 đến 11:30 (Thứ 4 đến Thứ 6).<LineBreak/>- Môn thi: Toán, Văn, Anh, Lý, Hóa.</Paragraph><Paragraph FontSize=\"16\" FontWeight=\"Bold\" Foreground=\"#0F172A\">2. Khối 12</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">- Thời gian: Chiều từ 13:30 đến 17:00 (Thứ 2 đến Thứ 4).<LineBreak/>- Đề thi theo định dạng tốt nghiệp THPT Quốc gia.</Paragraph><Paragraph FontSize=\"16\" FontStyle=\"Italic\" Foreground=\"#DC2626\">* Yêu cầu học sinh mang thẻ học sinh khi vào phòng thi.</Paragraph></FlowDocument>" 
                        },
                        new BulletinTemplate 
                        { 
                            Name = "Phát động Phong trào Xanh", Category = "Event", 
                            HtmlContent = "<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph FontSize=\"24\" FontWeight=\"Black\" Foreground=\"#16A34A\" TextAlignment=\"Center\">⭐ CHIẾN DỊCH TRƯỜNG HỌC XANH ⭐</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">Đoàn Thanh niên trường phát động tháng hành động vì môi trường học đường.</Paragraph><Paragraph FontSize=\"16\" FontWeight=\"Bold\" Foreground=\"#0F172A\">Nội dung hoạt động:</Paragraph><List MarkerStyle=\"Box\"><ListItem><Paragraph FontSize=\"16\" Foreground=\"#334155\">Thu gom pin cũ đổi cây sen đá (Tầng 1 - Sảnh A).</Paragraph></ListItem><ListItem><Paragraph FontSize=\"16\" Foreground=\"#334155\">Mỗi lớp chăm sóc 2 chậu cây xanh ban công.</Paragraph></ListItem><ListItem><Paragraph FontSize=\"16\" Foreground=\"#334155\">Tổng vệ sinh lớp học chiều thứ 6 hàng tuần.</Paragraph></ListItem></List><Paragraph FontSize=\"16\" FontWeight=\"Bold\" Foreground=\"#EA580C\">Phần thưởng:</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">Lớp có mảng xanh đẹp nhất sẽ được cộng 50 điểm thi đua và 1 voucher liên hoan trị giá 1.000.000đ.</Paragraph></FlowDocument>" 
                        },
                        new BulletinTemplate 
                        { 
                            Name = "Thông báo Tuyển sinh CLB", Category = "Event", 
                            HtmlContent = "<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph FontSize=\"22\" FontWeight=\"Bold\" Foreground=\"#9333EA\" TextAlignment=\"Center\">⭐ TUYỂN THÀNH VIÊN CÁC CÂU LẠC BỘ ⭐</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">Xin chào các bạn học sinh khối 10!</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">Tuần lễ định hướng và đăng ký CLB chính thức bắt đầu. Hãy nhanh tay chọn cho mình một bến đỗ để phát triển đam mê:</Paragraph><Paragraph FontSize=\"16\" FontWeight=\"Bold\" Foreground=\"#0F172A\">Các CLB đang tuyển:</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">- CLB Âm nhạc (Giai điệu thanh xuân)<LineBreak/>- CLB Bóng rổ (Dunk Kings)<LineBreak/>- CLB Lập trình &amp; Robotics<LineBreak/>- CLB Truyền thông (Media Team)</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#2563EB\" TextDecorations=\"Underline\">Hạn chót đăng ký: 23:59 Chủ nhật tuần này.</Paragraph><Paragraph FontSize=\"16\" FontStyle=\"Italic\" Foreground=\"#64748B\">Link đăng ký chi tiết đã được gửi qua email học sinh.</Paragraph></FlowDocument>" 
                        },
                        new BulletinTemplate 
                        { 
                            Name = "Khen thưởng Học sinh Giỏi", Category = "Announcement", 
                            HtmlContent = "<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph FontSize=\"22\" FontWeight=\"Bold\" Foreground=\"#EAB308\" TextAlignment=\"Center\">⭐ DANH SÁCH VINH DANH HỌC SINH GIỎI ⭐</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">Nhà trường nhiệt liệt chúc mừng các em học sinh đã xuất sắc đạt giải trong kỳ thi Học sinh giỏi cấp Tỉnh vừa qua:</Paragraph><List MarkerStyle=\"Decimal\"><ListItem><Paragraph FontSize=\"16\" FontWeight=\"Bold\" Foreground=\"#0F172A\">Nguyễn Trần A - 12A1 (Giải Nhất Toán)</Paragraph></ListItem><ListItem><Paragraph FontSize=\"16\" FontWeight=\"Bold\" Foreground=\"#0F172A\">Lê Thị B - 11A2 (Giải Nhì Ngữ Văn)</Paragraph></ListItem><ListItem><Paragraph FontSize=\"16\" FontWeight=\"Bold\" Foreground=\"#0F172A\">Trần Văn C - 12A1 (Giải Ba Vật Lý)</Paragraph></ListItem></List><Paragraph FontSize=\"16\" Foreground=\"#334155\">Lễ trao thưởng sẽ diễn ra vào Lễ chào cờ sáng thứ 2 tuần sau.</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">Thầy cô và các bạn hãy đến chung vui cùng các em nhé!</Paragraph></FlowDocument>" 
                        },
                        new BulletinTemplate 
                        { 
                            Name = "Nội quy Canteen Trường", Category = "Announcement", 
                            HtmlContent = "<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph FontSize=\"22\" FontWeight=\"Bold\" Foreground=\"#0284C7\" TextAlignment=\"Center\">⭐ NỘI QUY CANTEEN TRƯỜNG ⭐</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">Để đảm bảo vệ sinh và trật tự, yêu cầu học sinh và giáo viên tuân thủ các quy định sau khi ăn tại Canteen:</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">1. Xếp hàng lấy thức ăn theo thứ tự, không chen lấn.<LineBreak/>2. Ăn uống xong phải dọn dẹp khay và phân loại rác (Nhựa - Thức ăn thừa).<LineBreak/>3. Không mang đồ ăn, nước uống ra khỏi khu vực Canteen.<LineBreak/>4. Thanh toán bằng thẻ học sinh (Smart Card) hoặc quét mã QR.</Paragraph><Paragraph FontSize=\"16\" FontWeight=\"Bold\" Foreground=\"#DC2626\">Nhà trường sẽ trừ điểm rèn luyện nếu vi phạm.</Paragraph></FlowDocument>" 
                        },
                        new BulletinTemplate 
                        { 
                            Name = "Thông báo Thu Học phí", Category = "Announcement", 
                            HtmlContent = "<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph FontSize=\"22\" FontWeight=\"Bold\" Foreground=\"#BE123C\" TextAlignment=\"Center\">⭐ THÔNG BÁO THU HỌC PHÍ KỲ 2 ⭐</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">Kính gửi Quý Phụ huynh,</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">Nhà trường xin thông báo thời gian đóng học phí và các khoản thu học kỳ 2 năm học 2026-2027.</Paragraph><Paragraph FontSize=\"16\" FontWeight=\"Bold\" Foreground=\"#0F172A\">- Thời hạn: Từ ngày 01/02 đến 15/02.</Paragraph><Paragraph FontSize=\"16\" FontWeight=\"Bold\" Foreground=\"#0F172A\">- Hình thức thanh toán:</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">1. Chuyển khoản qua tài khoản ngân hàng của trường (Cú pháp: MaHS_HoTen_HocPhiK2).<LineBreak/>2. Thanh toán trực tiếp tại Phòng Tài vụ (Tầng 1).</Paragraph><Paragraph FontSize=\"16\" FontStyle=\"Italic\" Foreground=\"#64748B\">Mọi thắc mắc xin liên hệ Hotline: 1900 1234.</Paragraph></FlowDocument>" 
                        },
                        new BulletinTemplate 
                        { 
                            Name = "Hướng dẫn Kích hoạt App", Category = "System", 
                            HtmlContent = "<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph FontSize=\"22\" FontWeight=\"Bold\" Foreground=\"#047857\" TextAlignment=\"Center\">⭐ HƯỚNG DẪN CÀI ĐẶT SMARTCLASS APP</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">Bắt đầu từ tuần này, mọi thông báo sẽ được gửi về ứng dụng điện thoại.</Paragraph><Paragraph FontSize=\"16\" FontWeight=\"Bold\" Foreground=\"#0F172A\">Các bước cài đặt:</Paragraph><List MarkerStyle=\"Decimal\"><ListItem><Paragraph FontSize=\"16\" Foreground=\"#334155\">Lên App Store hoặc Google Play tải \"QA SmartClass\".</Paragraph></ListItem><ListItem><Paragraph FontSize=\"16\" Foreground=\"#334155\">Mở app, đăng nhập bằng tài khoản (Mã HS/GV và Mật khẩu mặc định).</Paragraph></ListItem><ListItem><Paragraph FontSize=\"16\" Foreground=\"#334155\">Vào phần Cài đặt -\u003e Đổi mật khẩu.</Paragraph></ListItem></List><Paragraph FontSize=\"16\" Foreground=\"#2563EB\" TextDecorations=\"Underline\">Lưu ý: Bật thông báo (Notifications) để không bị bỏ lỡ tin tức quan trọng.</Paragraph></FlowDocument>" 
                        },
                        new BulletinTemplate 
                        { 
                            Name = "Triệu tập Họp Hội đồng", Category = "Event", 
                            HtmlContent = "<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph FontSize=\"22\" FontWeight=\"Bold\" Foreground=\"#B91C1C\" TextAlignment=\"Center\">LỊCH HỌP HỘI ĐỒNG SƯ PHẠM</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">Kính mời toàn thể Cán bộ, Giáo viên và Nhân viên tham dự cuộc họp Hội đồng định kỳ tháng này.</Paragraph><Paragraph FontSize=\"16\" FontWeight=\"Bold\" Foreground=\"#0F172A\">Thông tin chi tiết:</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">- Thời gian: 14h00, Thứ 6 ngày 25/09.<LineBreak/>- Địa điểm: Phòng Hội đồng (Tầng 2 - Tòa A).<LineBreak/>- Thành phần: BGH, Tổ trưởng chuyên môn và toàn thể giáo viên.</Paragraph><Paragraph FontSize=\"16\" FontWeight=\"Bold\" Foreground=\"#0F172A\">Nội dung chính:</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">- Đánh giá hoạt động tháng 9.<LineBreak/>- Triển khai chuyên đề Dạy học STEM.</Paragraph><Paragraph FontSize=\"16\" FontStyle=\"Italic\" Foreground=\"#DC2626\">Yêu cầu 100% tham gia đầy đủ, đúng giờ.</Paragraph></FlowDocument>" 
                        },
                        new BulletinTemplate 
                        { 
                            Name = "Thông báo Nghỉ Đột Xuất (Bão)", Category = "Urgent", 
                            HtmlContent = "<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph FontSize=\"26\" FontWeight=\"Black\" Foreground=\"#DC2626\" TextAlignment=\"Center\">⭐ THÔNG BÁO KHẨN CẤP: NGHỈ HỌC TRÁNH BÃO ⭐</Paragraph><Paragraph FontSize=\"16\" FontWeight=\"Bold\" Foreground=\"#0F172A\">Kính gửi Quý Phụ huynh và Học sinh,</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">Theo công điện khẩn của Sở GD&amp;ĐT về tình hình siêu bão đang đổ bộ, Nhà trường thông báo:</Paragraph><Paragraph FontSize=\"18\" FontWeight=\"Bold\" Foreground=\"#B91C1C\" TextAlignment=\"Center\">CHO TẤT CẢ HỌC SINH NGHỈ HỌC TỪ CHIỀU HÔM NAY (CHO ĐẾN KHI CÓ THÔNG BÁO MỚI)</Paragraph><Paragraph FontSize=\"16\" Foreground=\"#334155\">Đề nghị Phụ huynh sắp xếp đón con em trước 15:00 hôm nay. Giáo viên chủ nhiệm đảm bảo không học sinh nào ở lại trường sau 15:30.</Paragraph><Paragraph FontSize=\"16\" FontStyle=\"Italic\" Foreground=\"#64748B\">Lịch học bù sẽ được thông báo sau. Rất mong mọi người giữ an toàn tuyệt đối!</Paragraph></FlowDocument>" 
                        }
                    );
                    _db.SaveChanges();
                }

                IcTemplates.ItemsSource = _db.BulletinTemplates.ToList();
            }
            catch (Exception ex) { Log.Warning("BulletinTemplate Load error: {Err}", ex.Message); }
        }

        private void BtnUseTemplate_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is BulletinTemplate tpl)
            {
                SelectedTemplate = tpl;
                DialogResult = true;
                Close();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

