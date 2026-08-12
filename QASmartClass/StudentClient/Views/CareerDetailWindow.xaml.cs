using System;
using System.Windows;
using QuestPDF.Fluent;

namespace QASmartClass.StudentClient.Views
{
    public partial class CareerDetailWindow : Window
    {
        public CareerDetailWindow(string careerName)
        {
            InitializeComponent();
            LoadCareerInfo(careerName);
        }

        private void LoadCareerInfo(string careerName)
        {
            if (string.IsNullOrEmpty(careerName)) return;

            string title = careerName;
            string description = "Định hướng nghề nghiệp và phát triển năng lực tương ứng.";
            string subjects = "A00, A01, D01 (Toán, Văn, Anh, Khoa học)";
            string skills = "• Kỹ năng tự học & nghiên cứu sâu\n• Tư duy phản biện & giải quyết vấn đề\n• Ngoại ngữ (Tiếng Anh giao tiếp và học thuật)";
            string roadmap = "1. Học sinh: Tập trung học tốt các môn học cốt lõi, tham gia hoạt động ngoại khóa.\n2. Đại học: Lựa chọn ngành học phù hợp tại các trường Đại học uy tín.\n3. Đi làm: Tích lũy kinh nghiệm qua thực tập, xây dựng mạng lưới quan hệ công việc.";

            // Normalize name
            string lowerName = careerName.ToLower();

            if (lowerName.Contains("kỹ sư phần mềm") || lowerName.Contains("phát triển sản phẩm") || lowerName.Contains("hệ thống thông tin"))
            {
                title = "Kỹ sư phần mềm / Phát triển sản phẩm";
                description = "Nghiên cứu, thiết kế, phát triển và thử nghiệm các phần mềm, ứng dụng và hệ thống vận hành máy tính chuyên nghiệp.";
                subjects = "A00 (Toán, Vật lý, Hóa học), A01 (Toán, Vật lý, Tiếng Anh), D07 (Toán, Hóa học, Tiếng Anh)";
                skills = "• Tư duy thuật toán & Giải quyết vấn đề phức tạp\n• Thành thạo các ngôn ngữ lập trình (C#, Java, Python, C++)\n• Hiểu biết sâu về CSDL (SQL/NoSQL) và Kiến trúc hệ thống";
                roadmap = "1. Học sinh: Học tốt môn Toán, Tin học. Tham gia các câu lạc bộ tin học.\n2. Đại học: Học chuyên ngành Khoa học Máy tính, Kỹ thuật Phần mềm.\n3. Đi làm: Thực tập tại doanh nghiệp công nghệ, thăng tiến lên Senior, Tech Lead.";
            }
            else if (lowerName.Contains("phân tích dữ liệu"))
            {
                title = "Chuyên viên Phân tích dữ liệu (Data Analyst)";
                description = "Thu thập, xử lý và thực hiện phân tích thống kê dữ liệu lớn để tìm ra Insights hữu ích hỗ trợ đưa ra quyết định kinh doanh.";
                subjects = "A00 (Toán, Vật lý, Hóa học), A01 (Toán, Vật lý, Tiếng Anh), D01 (Toán, Ngữ văn, Tiếng Anh)";
                skills = "• Lập trình SQL, Python/R phân tích dữ liệu\n• Trực quan hóa dữ liệu (Sử dụng Power BI, Tableau, Excel nâng cao)\n• Tư duy phân tích kinh doanh (Business Intuition) và Kỹ năng báo cáo";
                roadmap = "1. Học sinh: Học tốt môn Toán học, Xác suất thống kê cơ bản, Tiếng Anh.\n2. Đại học: Học Hệ thống thông tin quản lý (MIS), Khoa học dữ liệu hoặc Kinh tế.\n3. Đi làm: Bắt đầu ở vị trí Data Analyst tại các tập đoàn tài chính, bán lẻ.";
            }
            else if (lowerName.Contains("khoa học dữ liệu") || lowerName.Contains("ai") || lowerName.Contains("trí tuệ nhân tạo"))
            {
                title = "Nhà khoa học dữ liệu / Kỹ sư AI";
                description = "Ứng dụng các mô hình học máy (Machine Learning) và các thuật toán AI nâng cao để tự động hóa quy trình và dự đoán hành vi hệ thống.";
                subjects = "A00 (Toán, Vật lý, Hóa học), A01 (Toán, Vật lý, Tiếng Anh)";
                skills = "• Toán cao cấp (Giải tích, Đại số tuyến tính, Xác suất thống kê nâng cao)\n• Học máy chuyên sâu (ML), Học sâu (Deep Learning), Xử lý ngôn ngữ tự nhiên (NLP)\n• Sử dụng thư viện lập trình AI (PyTorch, TensorFlow, Scikit-Learn)";
                roadmap = "1. Học sinh: Đạt thành tích cao môn Toán, học lập trình Python sớm.\n2. Đại học: Học chuyên ngành Khoa học dữ liệu, Toán - Tin, hoặc AI.\n3. Đi làm: Kỹ sư Học máy, Chuyên gia Nghiên cứu AI tại các Lab công nghệ.";
            }
            else if (lowerName.Contains("tư vấn học đường") || lowerName.Contains("tư vấn tâm lý") || lowerName.Contains("tâm lý học"))
            {
                title = "Chuyên gia Tư vấn tâm lý học đường";
                description = "Hỗ trợ học sinh giải quyết khó khăn trong học tập, các mối quan hệ xã hội, khủng hoảng tâm lý tuổi học trò và định hướng sự nghiệp.";
                subjects = "C00 (Ngữ văn, Lịch sử, Địa lý), D01 (Toán, Ngữ văn, Tiếng Anh), B00 (Toán, Hóa học, Sinh học)";
                skills = "• Kỹ năng lắng nghe tích cực & Đồng cảm sâu sắc\n• Đánh giá tâm lý học, tham vấn tâm lý cá nhân và nhóm\n• Kỹ năng truyền thông sư phạm và giải quyết xung đột";
                roadmap = "1. Học sinh: Tham gia tích cực các hoạt động thiện nguyện, kỹ năng lắng nghe.\n2. Đại học: Học ngành Tâm lý học giáo dục, Tâm lý học lâm sàng.\n3. Đi làm: Chuyên viên tham vấn tại các trường THCS, THPT hoặc Trung tâm trị liệu.";
            }
            else if (lowerName.Contains("giáo viên") || lowerName.Contains("giảng viên"))
            {
                title = "Giáo viên / Giảng viên đào tạo kỹ năng";
                description = "Giảng dạy kiến thức văn hóa hoặc đào tạo kỹ năng sống, kỹ năng mềm giúp người học phát triển toàn diện bản thân.";
                subjects = "C00 (Ngữ văn, Lịch sử, Địa lý), D01 (Toán, Ngữ văn, Tiếng Anh), D08 (Toán, Lịch sử, Tiếng Anh)";
                skills = "• Kỹ năng sư phạm, truyền đạt và thuyết trình cuốn hút\n• Soạn giáo án sáng tạo, ứng dụng CNTT hiệu quả vào bài giảng\n• Giao tiếp tự tin trước đám đông và thấu hiểu học sinh";
                roadmap = "1. Học sinh: Rèn luyện khả năng nói trước đám đông, tự tin chia sẻ kiến thức.\n2. Đại học: Học các trường Đại học Sư phạm hoặc học cử nhân chuyên ngành + Chứng chỉ Nghiệp vụ Sư phạm.\n3. Đi làm: Giáo viên đứng lớp tại các cơ sở giáo dục hoặc kỹ năng sống.";
            }
            else if (lowerName.Contains("dự án cộng đồng"))
            {
                title = "Quản lý dự án cộng đồng (NGO Project Manager)";
                description = "Lập kế hoạch, điều phối nhân lực và quản trị ngân sách cho các chương trình phát triển cộng đồng, bảo vệ môi trường, xã hội phi lợi nhuận.";
                subjects = "D01 (Toán, Ngữ văn, Tiếng Anh), C00 (Ngữ văn, Lịch sử, Địa lý), D14 (Ngữ văn, Lịch sử, Tiếng Anh)";
                skills = "• Quản lý dự án phát triển bền vững & Điều phối tình nguyện viên\n• Viết đề xuất dự án (Project Proposal) bằng Tiếng Anh để xin quỹ tài trợ\n• Đàm phán và kết nối với các đối tác chính phủ, doanh nghiệp";
                roadmap = "1. Học sinh: Tham gia hoạt động Đoàn hội, tình nguyện viên chiến dịch xã hội.\n2. Đại học: Học ngành Công tác xã hội, Xã hội học hoặc Quản trị công.\n3. Đi làm: Nhân sự tại các tổ chức phi chính phủ (NGO) quốc tế hoặc doanh nghiệp.";
            }
            else if (lowerName.Contains("thiết kế đồ họa") || lowerName.Contains("ui-ux") || lowerName.Contains("nhà thiết kế"))
            {
                title = "Nhà thiết kế đồ họa / Thiết kế sản phẩm UI-UX";
                description = "Sáng tạo giao diện người dùng, trải nghiệm số cho website/app và sản xuất các ấn phẩm truyền thông trực quan sinh động.";
                subjects = "H00 (Ngữ văn, Năng khiếu vẽ 1, Năng khiếu vẽ 2), V00 (Toán, Vật lý, Vẽ mỹ thuật), D01 (Toán, Văn, Anh)";
                skills = "• Sử dụng thành thạo Photoshop, Illustrator, Figma\n• Tư duy bố cục, phối màu sắc và am hiểu trải nghiệm khách hàng (User Journey)\n• Kỹ năng làm việc với ý tưởng sáng tạo độc đáo";
                roadmap = "1. Học sinh: Học vẽ mỹ thuật cơ bản, rèn luyện tư duy bố cục hình ảnh.\n2. Đại học: Học chuyên ngành Thiết kế Đồ họa, Mỹ thuật ứng dụng.\n3. Đi làm: UI/UX Designer tại các Product agency hoặc Designer truyền thông thương hiệu.";
            }
            else if (lowerName.Contains("quan hệ công chúng") || lowerName.Contains("pr"))
            {
                title = "Chuyên viên Quan hệ công chúng (PR)";
                description = "Xây dựng hình ảnh tích cực của tổ chức/doanh nghiệp trước công chúng, quản lý khủng hoảng truyền thông và tổ chức sự kiện.";
                subjects = "D01 (Toán, Ngữ văn, Tiếng Anh), C00 (Ngữ văn, Lịch sử, Địa lý), D14 (Ngữ văn, Lịch sử, Tiếng Anh)";
                skills = "• Viết thông cáo báo chí, biên tập nội dung truyền thông xuất sắc\n• Giao tiếp ứng xử khéo léo và thiết lập mối quan hệ với báo chí\n• Lập kế hoạch tổ chức các sự kiện lớn";
                roadmap = "1. Học sinh: Rèn viết lách, làm MC học sinh, tổ chức sự kiện lớp.\n2. Đại học: Học ngành Quan hệ công chúng, Báo chí, Truyền thông đa phương tiện.\n3. Đi làm: Nhân sự PR/Event Executive tại các công ty PR agency hoặc doanh nghiệp.";
            }
            else if (lowerName.Contains("marketing") || lowerName.Contains("thương hiệu"))
            {
                title = "Chuyên viên Marketing / Quản trị Thương hiệu";
                description = "Nghiên cứu thị trường, lập kế hoạch quảng bá sản phẩm/dịch vụ của doanh nghiệp đến khách hàng mục tiêu qua các kênh.";
                subjects = "D01 (Toán, Ngữ văn, Tiếng Anh), A01 (Toán, Vật lý, Tiếng Anh), D07 (Toán, Hóa học, Tiếng Anh)";
                skills = "• Phân tích thị trường và thấu hiểu hành vi người tiêu dùng (Customer insight)\n• Sáng tạo chiến dịch nội dung số (Digital Marketing) & Tối ưu quảng cáo\n• Quản trị hiệu quả ngân sách marketing";
                roadmap = "1. Học sinh: Học tốt môn Ngữ văn, Tiếng Anh, yêu thích kinh doanh.\n2. Đại học: Học Quản trị Kinh doanh chuyên ngành Marketing hoặc Truyền thông.\n3. Đi làm: Marketing Executive, phát triển thành Brand Manager / CMO.";
            }
            else if (lowerName.Contains("giám đốc") || lowerName.Contains("khởi nghiệp") || lowerName.Contains("điều hành"))
            {
                title = "Giám đốc điều hành / Khởi nghiệp kinh doanh";
                description = "Quản lý chiến lược toàn diện của tổ chức, ra quyết định đầu tư quan trọng và dẫn dắt sự tăng trưởng của doanh nghiệp.";
                subjects = "A01 (Toán, Vật lý, Tiếng Anh), D01 (Toán, Ngữ văn, Tiếng Anh), A00 (Toán, Vật lý, Hóa học)";
                skills = "• Khả năng lãnh đạo, truyền cảm hứng và quản trị nhân sự\n• Tư duy chiến lược dài hạn và nhạy bén với cơ hội thị trường\n• Am hiểu quản trị tài chính, dòng tiền và đàm phán thương mại";
                roadmap = "1. Học sinh: Đảm nhận các chức vụ ban cán sự lớp, tham gia CLB kinh doanh học sinh.\n2. Đại học: Học ngành Quản trị Kinh doanh, Tài chính hoặc Quản trị điều hành.\n3. Đi làm: Tích lũy kinh nghiệm quản lý cấp trung, tự mở startup hoặc thăng tiến CEO.";
            }
            else if (lowerName.Contains("kiểm thử") || lowerName.Contains("qa"))
            {
                title = "Chuyên viên Kiểm thử phần mềm (QA/QC Engineer)";
                description = "Đảm bảo chất lượng sản phẩm công nghệ bằng cách thiết kế các ca kiểm thử, chạy thử và phát hiện lỗi trước khi xuất xưởng.";
                subjects = "A00 (Toán, Vật lý, Hóa học), A01 (Toán, Vật lý, Tiếng Anh), D01 (Toán, Ngữ văn, Tiếng Anh)";
                skills = "• Tư duy phân tích chi tiết, tỉ mỉ và cẩn thận\n• Thiết kế testcase và viết test script tự động (Automation testing với Selenium, Cypress)\n• Hiểu biết về quy trình phát triển phần mềm Agile/Scrum";
                roadmap = "1. Học sinh: Rèn luyện đức tính cẩn thận, học tốt Toán, Tin học.\n2. Đại học: Học Công nghệ thông tin hoặc Hệ thống thông tin.\n3. Đi làm: Kỹ sư QA/QC thủ công (Manual), nâng cấp lên Automation Test Engineer.";
            }
            else if (lowerName.Contains("kế toán") || lowerName.Contains("kiểm toán"))
            {
                title = "Kế toán viên chuyên sâu / Kiểm toán viên độc lập";
                description = "Kiểm tra tính trung thực của các báo cáo tài chính doanh nghiệp và kiểm soát các rủi ro tuân thủ kế toán thuế.";
                subjects = "A00 (Toán, Vật lý, Hóa học), A01 (Toán, Vật lý, Tiếng Anh), D01 (Toán, Ngữ văn, Tiếng Anh)";
                skills = "• Hiểu biết sâu sắc luật kế toán, thuế và chuẩn mực tài chính\n• Sử dụng thành thạo phần mềm kế toán (MISA, SAP) và Excel phân tích số liệu\n• Tư duy độc lập, khách quan và bảo mật dữ liệu";
                roadmap = "1. Học sinh: Học tốt môn Toán học, rèn luyện kỹ năng tính toán chính xác.\n2. Đại học: Học chuyên ngành Kế toán - Kiểm toán, Tài chính doanh nghiệp.\n3. Đi làm: Đạt chứng chỉ quốc tế (ACCA, CPA) để trở thành kiểm toán viên Big4.";
            }

            TxtTitle.Text = title;
            TxtDescription.Text = description;
            TxtSubjects.Text = subjects;
            TxtSkills.Text = skills;
            TxtRoadmap.Text = roadmap;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnExportPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var title = TxtTitle.Text;
                var description = TxtDescription.Text;
                var subjects = TxtSubjects.Text;
                var skills = TxtSkills.Text;
                var roadmap = TxtRoadmap.Text;

                string outputPath = QASmartClass.Services.PdfTemplateHelper.GetOutputPath("HuongNghiep", title);
                QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

                QuestPDF.Fluent.Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(QuestPDF.Helpers.PageSizes.A4);
                        page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);
                        page.PageColor(QuestPDF.Helpers.Colors.White);
                        page.DefaultTextStyle(x => x.FontFamily("Arial"));

                        page.Header().Text("CẨM NANG HƯỚNG NGHIỆP HỌC SINH").FontSize(10).Bold().FontColor(QuestPDF.Helpers.Colors.Grey.Medium).AlignRight();

                        page.Content().Column(col =>
                        {
                            col.Spacing(15);

                            // Title Block
                            col.Item().Background(QuestPDF.Helpers.Colors.Indigo.Darken4).Padding(15).Column(titleCol =>
                            {
                                titleCol.Item().Text(title.ToUpper()).FontSize(20).Bold().FontColor(QuestPDF.Helpers.Colors.White);
                                titleCol.Item().PaddingTop(5).Text(description).FontSize(12).Italic().FontColor(QuestPDF.Helpers.Colors.Indigo.Lighten5);
                            });

                            // Subjects
                            col.Item().Column(section =>
                            {
                                section.Item().Text("📚 TỔ HỢP MÔN THI THPT QUỐC GIA LIÊN QUAN").FontSize(14).Bold().FontColor(QuestPDF.Helpers.Colors.Indigo.Darken3);
                                section.Item().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2);
                                section.Item().PaddingTop(5).Text(subjects).FontSize(12).FontColor(QuestPDF.Helpers.Colors.Grey.Darken3);
                            });

                            // Skills
                            col.Item().Column(section =>
                            {
                                section.Item().Text("🛠 KỸ NĂNG CỐT LÕI CẦN CHUẨN BỊ").FontSize(14).Bold().FontColor(QuestPDF.Helpers.Colors.Indigo.Darken3);
                                section.Item().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2);
                                section.Item().PaddingTop(5).Text(skills).FontSize(12).FontColor(QuestPDF.Helpers.Colors.Grey.Darken3);
                            });

                            // Roadmap
                            col.Item().Column(section =>
                            {
                                section.Item().Text("🚀 LỘ TRÌNH HỌC TẬP VÀ PHÁT TRIỂN SỰ NGHIỆP").FontSize(14).Bold().FontColor(QuestPDF.Helpers.Colors.Indigo.Darken3);
                                section.Item().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2);
                                section.Item().PaddingTop(5).Text(roadmap).FontSize(12).FontColor(QuestPDF.Helpers.Colors.Grey.Darken3);
                            });

                            // Footer Note
                            col.Item().PaddingTop(20).Text("© Hệ thống Hướng nghiệp Thông minh QA SmartClass").FontSize(10).Italic().FontColor(QuestPDF.Helpers.Colors.Grey.Medium).AlignCenter();
                        });
                    });
                }).GeneratePdf(outputPath);

                MessageBox.Show($"Đã xuất cẩm nang hướng nghiệp thành công!\nĐường dẫn: {outputPath}", "Xuất PDF", MessageBoxButton.OK, MessageBoxImage.Information);

                // Auto open the PDF
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = outputPath, UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    Serilog.Log.Warning("Could not auto-open PDF: {Err}", ex.Message);
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to export career roadmap PDF");
                MessageBox.Show($"Lỗi xuất PDF: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
