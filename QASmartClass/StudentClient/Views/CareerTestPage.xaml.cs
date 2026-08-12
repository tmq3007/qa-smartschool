using QASmartClass.Data;
using Serilog;
using System;
using System.Windows;
using System.Windows.Controls;

namespace QASmartClass.StudentClient.Views
{
    public partial class CareerTestPage : Page
    {
        public CareerTestPage()
        {
            InitializeComponent();
        }

        private void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            BtnSubmit.IsEnabled = false; // Chặn click đúp

            // Validation: Kiểm tra tất cả các nhóm câu hỏi
            if ((RbQ1A.IsChecked != true && RbQ1B.IsChecked != true) ||
                (RbQ2A.IsChecked != true && RbQ2B.IsChecked != true) ||
                (RbQ3A.IsChecked != true && RbQ3B.IsChecked != true) ||
                (RbQ4A.IsChecked != true && RbQ4B.IsChecked != true) ||
                (RbQ5A.IsChecked != true && RbQ5B.IsChecked != true && RbQ5C.IsChecked != true) ||
                (RbQ6A.IsChecked != true && RbQ6B.IsChecked != true && RbQ6C.IsChecked != true))
            {
                MessageBox.Show("Vui lòng trả lời đầy đủ tất cả các câu hỏi trắc nghiệm trước khi nộp bài!", "Chưa hoàn thành", MessageBoxButton.OK, MessageBoxImage.Warning);
                BtnSubmit.IsEnabled = true;
                return;
            }

            try
            {
                // 1. Tính toán MBTI
                string mbti_E_I = RbQ1A.IsChecked == true ? "E" : "I";
                string mbti_S_N = RbQ3A.IsChecked == true ? "S" : "N";
                string mbti_T_F = RbQ4A.IsChecked == true ? "T" : "F";
                string mbti_J_P = RbQ2A.IsChecked == true ? "J" : "P";
                string mbti = $"{mbti_E_I}{mbti_S_N}{mbti_T_F}{mbti_J_P}";

                // 2. Tính toán Holland Code
                string holland5 = RbQ5A.IsChecked == true ? "R" : (RbQ5B.IsChecked == true ? "I" : "A");
                string holland6 = RbQ6A.IsChecked == true ? "S" : (RbQ6B.IsChecked == true ? "E" : "C");
                string holland = $"{holland5}{holland6}";

                // 3. Ánh xạ ngành nghề dựa trên kết hợp kiểu tính cách MBTI và Holland
                string career1 = "1. Kỹ sư phần mềm / Hệ thống thông tin";
                string career2 = "2. Chuyên viên Phân tích dữ liệu";
                string career3 = "3. Nhà khoa học dữ liệu / AI";

                // Mặc định phân loại theo Holland trước để đảm bảo bao phủ 100% các kết hợp
                if (holland5 == "R")
                {
                    career1 = "1. Kỹ sư cơ điện tử / Chế tạo máy";
                    career2 = "2. Kỹ thuật viên tự động hóa";
                    career3 = "3. Chuyên viên kỹ thuật vận hành sản xuất";
                }
                else if (holland5 == "I")
                {
                    career1 = "1. Nhà nghiên cứu khoa học / Nghiên cứu sinh";
                    career2 = "2. Chuyên viên nghiên cứu thị trường";
                    career3 = "3. Bác sĩ / Dược sĩ lâm sàng";
                }
                else if (holland5 == "A")
                {
                    career1 = "1. Nhà thiết kế đồ họa / Thiết kế thời trang";
                    career2 = "2. Họa sĩ minh họa / Kiến trúc sư";
                    career3 = "3. Biên kịch / Chuyên viên truyền thông sáng tạo";
                }
                else if (holland6 == "S")
                {
                    career1 = "1. Chuyên viên Công tác xã hội";
                    career2 = "2. Giáo viên / Giảng viên đào tạo";
                    career3 = "3. Chuyên viên dịch vụ chăm sóc khách hàng";
                }
                else if (holland6 == "E")
                {
                    career1 = "1. Chuyên viên quản lý kinh doanh / Sale";
                    career2 = "2. Quản lý dự án / Trưởng nhóm";
                    career3 = "3. Chuyên viên tư vấn đầu tư tài chính";
                }
                else if (holland6 == "C")
                {
                    career1 = "1. Kế toán viên / Chuyên viên thuế";
                    career2 = "2. Chuyên viên lưu trữ hồ sơ hành chính";
                    career3 = "3. Thủ quỹ / Giao dịch viên ngân hàng";
                }

                // Ghi đè bằng các nhánh kết hợp đặc thù giữa MBTI và Holland
                if (mbti_E_I == "E" && mbti_T_F == "F") // Hướng ngoại, Cảm xúc (ENFP, ESFP,...)
                {
                    if (holland6 == "S") // Nhóm Xã hội
                    {
                        career1 = "1. Chuyên viên Tư vấn học đường";
                        career2 = "2. Giáo viên / Giảng viên kỹ năng";
                        career3 = "3. Quản lý dự án cộng đồng";
                    }
                    else if (holland6 == "E") // Nhóm Quản lý
                    {
                        career1 = "1. Chuyên viên Quan hệ công chúng (PR)";
                        career2 = "2. Chuyên viên Marketing / Thương hiệu";
                        career3 = "3. Giám đốc điều hành kinh doanh";
                    }
                }
                else if (mbti_E_I == "E" && mbti_T_F == "T") // Hướng ngoại, Lý trí (ENTJ, ESTJ,...)
                {
                    if (holland6 == "E") // Nhóm Quản lý
                    {
                        career1 = "1. Giám đốc điều hành / Khởi nghiệp";
                        career2 = "2. Trưởng phòng Quản lý dự án";
                        career3 = "3. Chuyên gia tư vấn chiến lược";
                    }
                    else if (holland6 == "C") // Nhóm Nghiệp vụ
                    {
                        career1 = "1. Quản lý dự án tài chính";
                        career2 = "2. Chuyên viên phân tích ngân sách";
                        career3 = "3. Giám đốc vận hành chuỗi cung ứng";
                    }
                }
                else if (mbti_E_I == "I" && mbti_T_F == "F") // Hướng nội, Cảm xúc (INFP, ISFP,...)
                {
                    if (holland5 == "A") // Nhóm Nghệ thuật
                    {
                        career1 = "1. Nhà thiết kế đồ họa / UI-UX";
                        career2 = "2. Họa sĩ minh họa / Sáng tạo";
                        career3 = "3. Biên kịch / Nhà văn tự do";
                    }
                    else if (holland5 == "I") // Nhóm Nghiên cứu
                    {
                        career1 = "1. Chuyên gia tư vấn tâm lý";
                        career2 = "2. Nhà xã hội học / Nghiên cứu";
                        career3 = "3. Bác sĩ chuyên khoa";
                    }
                }
                else if (mbti_E_I == "I" && mbti_T_F == "T") // Hướng nội, Lý trí (INTJ, ISTJ,...)
                {
                    if (holland5 == "I") // Nhóm Nghiên cứu
                    {
                        career1 = "1. Kỹ sư phần mềm / Phát triển sản phẩm";
                        career2 = "2. Nhà khoa học / Thống kê dữ liệu";
                        career3 = "3. Chuyên gia phân tích mật mã";
                    }
                    else if (holland6 == "C") // Nhóm Nghiệp vụ
                    {
                        career1 = "1. Kỹ sư mạng / Quản trị hệ thống";
                        career2 = "2. Chuyên viên Kiểm thử phần mềm (QA)";
                        career3 = "3. Kế toán viên chuyên sâu / Kiểm toán";
                    }
                }

                // 4. Hiển thị kết quả lên UI
                TxtMbti.Text = mbti;
                TxtHolland.Text = $"{holland} (Nhóm: {GetHollandLabel(holland5)} - {GetHollandLabel(holland6)})";
                
                TxtCareer1.Text = career1;
                TxtCareer2.Text = career2;
                TxtCareer3.Text = career3;
                
                PanelResult.Visibility = Visibility.Visible;

                // 5. Lưu kết quả thật vào CSDL
                using var db = new AppDbContext();
                var identityService = new QASmartClass.StudentClient.Services.StudentIdentityService(db);
                int studentId = identityService.GetCurrentStudent().Id;
                if (studentId <= 0) 
                { 
                    MessageBox.Show("Vui lòng đăng nhập!"); 
                    return; 
                }

                db.CareerTestResults.Add(new CareerTestResult
                {
                    StudentId = studentId,
                    TestType = "MBTI_HOLLAND",
                    ResultCode = $"{mbti}_{holland}",
                    Careers = $"{GetCleanCareerName(career1)}|{GetCleanCareerName(career2)}|{GetCleanCareerName(career3)}",
                    TakenAt = DateTime.Now
                });
                db.SaveChanges();

                MessageBox.Show($"Nộp bài thành công! Kết quả tính cách {mbti} đã được ghi nhận.", "Hoàn thành", MessageBoxButton.OK, MessageBoxImage.Information);
                BtnSubmit.IsEnabled = true;
            }
            catch (Exception ex)
            {
                Log.Warning("CareerTest error: {Err}", ex.Message);
                BtnSubmit.IsEnabled = true;
            }
        }

        private string GetHollandLabel(string code)
        {
            return code switch
            {
                "R" => "Kỹ thuật (Realistic)",
                "I" => "Nghiên cứu (Investigative)",
                "A" => "Nghệ thuật (Artistic)",
                "S" => "Xã hội (Social)",
                "E" => "Quản lý (Enterprising)",
                "C" => "Nghiệp vụ (Conventional)",
                _ => "Khác"
            };
        }

        private string GetCleanCareerName(string career)
        {
            if (string.IsNullOrEmpty(career)) return string.Empty;
            int dotIdx = career.IndexOf('.');
            if (dotIdx > 0 && dotIdx < 5)
            {
                return career.Substring(dotIdx + 1).Trim();
            }
            return career;
        }

        private void Career_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border)
            {
                string careerName = string.Empty;
                if (border.Tag?.ToString() == "1") careerName = TxtCareer1.Text;
                else if (border.Tag?.ToString() == "2") careerName = TxtCareer2.Text;
                else if (border.Tag?.ToString() == "3") careerName = TxtCareer3.Text;

                careerName = GetCleanCareerName(careerName);
                if (!string.IsNullOrEmpty(careerName))
                {
                    var detailWin = new CareerDetailWindow(careerName);
                    detailWin.Owner = Window.GetWindow(this);
                    detailWin.ShowDialog();
                }
            }
        }
    }
}

