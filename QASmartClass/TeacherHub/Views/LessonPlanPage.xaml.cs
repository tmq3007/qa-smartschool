using QASmartClass.Data;
using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace QASmartClass.TeacherHub.Views
{
    public partial class LessonPlanPage : Page
    {
        private AppDbContext? _db;
        private string _currentTeacherId = "GV_System";
        private int? _selectedPlanId;
        private System.Windows.Threading.DispatcherTimer? _autoSaveTimer;

        public LessonPlanPage()
        {
            InitializeComponent();
            Loaded += Page_Loaded;
            Unloaded += Page_Unloaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _db = new AppDbContext();
            _currentTeacherId = QASmartClass.Staff.Services.StaffSession.CurrentUser?.TeacherCode 
                ?? (QASmartClass.Services.UserSessionService.Instance.IsLoggedIn ? QASmartClass.Services.UserSessionService.Instance.TeacherCode : null) 
                ?? "GV_System";
            LoadTemplateContent();
            LoadLessonPlans();

            _autoSaveTimer = new System.Windows.Threading.DispatcherTimer();
            _autoSaveTimer.Interval = TimeSpan.FromSeconds(60);
            _autoSaveTimer.Tick += AutoSaveTimer_Tick;
            _autoSaveTimer.Start();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _autoSaveTimer?.Stop();
            _autoSaveTimer = null;
            _db?.Dispose();
            _db = null;
        }

        private void CbTemplate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                LoadTemplateContent();
            }
        }

        private void LoadTemplateContent()
        {
            string tag = (CbTemplate.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Chuan";
            if (tag == "Chuan")
            {
                TxtContent.Text = "I. Má»¤C TIÃŠU\n1. Kiáº¿n thá»©c:\n- ...\n2. Ká»¹ nÄƒng:\n- ...\n3. Pháº©m cháº¥t:\n- ...\n\nII. THIáº¾T Bá»Š Dáº Y Há»ŒC & Há»ŒC LIá»†U\n- ...\n\nIII. TIáº¾N TRÃŒNH Dáº Y Há»ŒC\n1. Hoáº¡t Ä‘á»™ng Khá»Ÿi Ä‘á»™ng\n2. Hoáº¡t Ä‘á»™ng HÃ¬nh thÃ nh kiáº¿n thá»©c má»›i\n3. Hoáº¡t Ä‘á»™ng Luyá»‡n táº­p\n4. Hoáº¡t Ä‘á»™ng Váº­n dá»¥ng";
            }
            else if (tag == "STEM")
            {
                TxtContent.Text = "CHá»¦ Äá»€ STEM: ...\n\nI. Váº¤N Äá»€ THá»°C TIá»„N\n- ...\n\nII. TIÃŠU CHÃ Sáº¢N PHáº¨M\n- ...\n\nIII. HOáº T Äá»˜NG THá»°C HÃ€NH\n1. XÃ¡c Ä‘á»‹nh váº¥n Ä‘á»\n2. NghiÃªn cá»©u kiáº¿n thá»©c ná»n\n3. Äá» xuáº¥t giáº£i phÃ¡p\n4. Cháº¿ táº¡o máº«u, thá»­ nghiá»‡m vÃ  Ä‘Ã¡nh giÃ¡\n5. BÃ¡o cÃ¡o, tháº£o luáº­n";
            }
            else if (tag == "Project")
            {
                TxtContent.Text = "Dáº Y Há»ŒC Dá»° ÃN\n\nI. TÃŠN Dá»° ÃN: ...\nII. Má»¤C TIÃŠU: ...\nIII. NHIá»†M Vá»¤ Dá»° ÃN: ...\nIV. Káº¾ HOáº CH THá»°C HIá»†N: ...\nV. TIÃŠU CHÃ ÄÃNH GIÃ Sáº¢N PHáº¨M: ...";
            }
            else if (tag == "TichHop")
            {
                TxtContent.Text = "GIÃO ÃN TÃCH Há»¢P (GDPT 2018)\n\nI. Má»¤C TIÃŠU BÃ€I Há»ŒC\n1. NÄƒng lá»±c chung: Tá»± chá»§, Giao tiáº¿p, Giáº£i quyáº¿t váº¥n Ä‘á»\n2. NÄƒng lá»±c Ä‘áº·c thÃ¹: ...\n3. Pháº©m cháº¥t: ChÄƒm chá»‰, Trung thá»±c, TrÃ¡ch nhiá»‡m\n\nII. THIáº¾T Bá»Š & Há»ŒC LIá»†U\n- ...\n\nIII. TIáº¾N TRÃŒNH Dáº Y Há»ŒC\n1. HÄ Má»Ÿ Ä‘áº§u (5 phÃºt) - Táº¡o tÃ¬nh huá»‘ng, káº¿t ná»‘i thá»±c tiá»…n\n2. HÄ HÃ¬nh thÃ nh kiáº¿n thá»©c (15 phÃºt) - KhÃ¡m phÃ¡, phÃ¡t hiá»‡n\n3. HÄ Luyá»‡n táº­p (10 phÃºt) - Thá»±c hÃ nh, Ã¡p dá»¥ng\n4. HÄ Váº­n dá»¥ng (10 phÃºt) - LiÃªn há»‡ thá»±c táº¿, tÃ­ch há»£p liÃªn mÃ´n\n5. HÄ Má»Ÿ rá»™ng (5 phÃºt) - TÃ¬m hiá»ƒu thÃªm, chuáº©n bá»‹ bÃ i sau\n\nIV. ÄÃNH GIÃ\n- ÄÃ¡nh giÃ¡ quÃ¡ trÃ¬nh: ...\n- ÄÃ¡nh giÃ¡ sáº£n pháº©m: ...";
            }
            else if (tag == "5E")
            {
                TxtContent.Text = "GIÃO ÃN THEO MÃ” HÃŒNH 5E\n\nI. Má»¤C TIÃŠU: ...\n\nII. CHUáº¨N Bá»Š: ...\n\nIII. TIáº¾N TRÃŒNH 5E\n\n1. ENGAGE - Gáº¯n káº¿t (5 phÃºt)\n   - Táº¡o há»©ng thÃº, Ä‘áº·t cÃ¢u há»i kÃ­ch thÃ­ch tÃ² mÃ²\n   - CÃ¢u há»i dáº«n dáº¯t: ...\n\n2. EXPLORE - KhÃ¡m phÃ¡ (12 phÃºt)\n   - HS tá»± tÃ¬m hiá»ƒu qua thÃ­ nghiá»‡m / tÃ i liá»‡u\n   - Hoáº¡t Ä‘á»™ng nhÃ³m: ...\n\n3. EXPLAIN - Giáº£i thÃ­ch (10 phÃºt)\n   - HS trÃ¬nh bÃ y káº¿t quáº£, GV chá»‘t kiáº¿n thá»©c\n   - Kiáº¿n thá»©c trá»ng tÃ¢m: ...\n\n4. ELABORATE - Má»Ÿ rá»™ng (10 phÃºt)\n   - Ãp dá»¥ng vÃ o bÃ i táº­p / tÃ¬nh huá»‘ng má»›i\n   - BÃ i táº­p: ...\n\n5. EVALUATE - ÄÃ¡nh giÃ¡ (8 phÃºt)\n   - Kiá»ƒm tra má»©c Ä‘á»™ hiá»ƒu bÃ i\n   - HÃ¬nh thá»©c: Quiz / CÃ¢u há»i má»Ÿ / Rubric";
            }
            else
            {
                TxtContent.Text = "";
            }
        }

        private void LoadLessonPlans()
        {
            if (_db == null) return;
            try
            {
                var plans = _db.LessonPlans
                               .Where(p => p.TeacherId == _currentTeacherId)
                               .OrderByDescending(p => p.CreatedAt)
                               .Take(10)
                               .ToList();

                var displayItems = plans.Select(p => new
                {
                    p.Id,
                    Title = string.IsNullOrEmpty(p.Title) ? "(ChÆ°a cÃ³ tÃªn)" : p.Title,
                    SubInfo = $"Tiáº¿t {p.Period} - Lá»›p {p.Grade} - MÃ´n {p.Subject}",
                    StatusText = p.Status == "Done" ? "HoÃ n thÃ nh" : "Báº£n nhÃ¡p",
                    StatusColor = p.Status == "Done" ? "#D1FAE5" : "#FEF3C7",
                    StatusTextColor = p.Status == "Done" ? "#059669" : "#D97706"
                }).ToList();

                LvLessonPlans.ItemsSource = displayItems;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lá»—i khi load danh sÃ¡ch LessonPlan");
            }
        }

        private async void BtnExportPdf_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;
            try
            {
                string subject = (CbSubject.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "N/A";
                string grade = (CbGrade.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "N/A";
                string title = TxtTitle.Text.Trim();
                string content = TxtContent.Text;

                if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
                {
                    MessageBox.Show("Vui lÃ²ng Ä‘iá»n tiÃªu Ä‘á» vÃ  ná»™i dung giÃ¡o Ã¡n trÆ°á»›c khi xuáº¥t.", "Thiáº¿u thÃ´ng tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ÄÆ°á»ng dáº«n lÆ°u file PDF trong MyDocuments
                string fileName = $"GiaoAn_{subject.Replace(" ", "_")}_{grade.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
                string filePath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName);

                var db = _db ?? new AppDbContext();
                var pdfService = new QASmartClass.Services.PdfExportService(db);

                string exportedPath = "";
                await System.Threading.Tasks.Task.Run(() => 
                {
                    exportedPath = pdfService.ExportLessonPlan(subject, grade, title, content, filePath);
                });

                if (string.IsNullOrEmpty(exportedPath))
                {
                    MessageBox.Show("ÄÃ£ xáº£y ra lá»—i khi táº¡o tá»‡p PDF giÃ¡o Ã¡n.", "Lá»—i", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                MessageBox.Show($"ÄÃ£ xuáº¥t giÃ¡o Ã¡n thÃ nh cÃ´ng!\n\nFile: {exportedPath}", "Xuáº¥t PDF", MessageBoxButton.OK, MessageBoxImage.Information);

                // Má»Ÿ file PDF
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(exportedPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lá»—i xuáº¥t PDF giÃ¡o Ã¡n");
                MessageBox.Show("ÄÃ£ xáº£y ra lá»—i khi xuáº¥t giÃ¡o Ã¡n.", "Lá»—i", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }

        private void BtnSaveDraft_Click(object sender, RoutedEventArgs e)
        {
            SavePlan("Draft");
        }

        private void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            SavePlan("Done");
        }



        /// <summary>Táº¡o bÃ i giáº£ng tá»« giÃ¡o Ã¡n hiá»‡n táº¡i</summary>
        private void BtnCreateLesson_Click(object sender, RoutedEventArgs e)
        {
            if (_db == null) return;
            try
            {
                string subject = (CbSubject.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
                string grade = (CbGrade.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
                string title = TxtTitle.Text.Trim();
                string content = TxtContent.Text;

                if (string.IsNullOrWhiteSpace(title))
                {
                    MessageBox.Show("Vui lÃ²ng nháº­p tiÃªu Ä‘á» giÃ¡o Ã¡n trÆ°á»›c.", "Thiáº¿u thÃ´ng tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Táº¡o Lesson má»›i tá»« giÃ¡o Ã¡n
                var lesson = new Data.Lesson
                {
                    Title = $"[Tá»« GiÃ¡o Ã¡n] {title}",
                    Subject = subject,
                    Grade = grade,
                    Description = $"BÃ i giáº£ng táº¡o tá»« giÃ¡o Ã¡n: {title}\n\n{content}",
                    TeacherName = _currentTeacherId.ToString(),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _db.Lessons.Add(lesson);
                _db.SaveChanges();

                MessageBox.Show(
                    $"ÄÃ£ táº¡o bÃ i giáº£ng thÃ nh cÃ´ng!\n\nTiÃªu Ä‘á»: {lesson.Title}\nMÃ´n: {subject}\nLá»›p: {grade}\n\nBáº¡n cÃ³ thá»ƒ má»Ÿ ClassroomShell á»Ÿ BÃ i giáº£ng Ä‘á»ƒ chá»‰nh sá»­a.",
                    "Táº¡o bÃ i giáº£ng", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lá»—i táº¡o bÃ i giáº£ng tá»« giÃ¡o Ã¡n");
                MessageBox.Show("ÄÃ£ xáº£y ra lá»—i khi táº¡o bÃ i giáº£ng.", "Lá»—i", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// TrÃ¬nh chiáº¿u - LÆ°u giÃ¡o Ã¡n vÃ  chuyá»ƒn sang SmartTouch Ä‘á»ƒ trÃ¬nh chiáº¿u trá»±c tiáº¿p
        /// </summary>
        private void BtnPresentToSmartTouch_Click(object sender, RoutedEventArgs e)
        {
            if (_db == null) return;
            try
            {
                string title = TxtTitle.Text.Trim();
                string content = TxtContent.Text;

                if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
                {
                    MessageBox.Show("Vui lÃ²ng nháº­p tiÃªu Ä‘á» vÃ  ná»™i dung giÃ¡o Ã¡n trÆ°á»›c khi trÃ¬nh chiáº¿u.",
                        "Thiáº¿u thÃ´ng tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"TrÃ¬nh chiáº¿u giÃ¡o Ã¡n \"{title}\" trÃªn SmartTouch?\n\nHá»‡ thá»‘ng sáº½ chuyá»ƒn sang báº£ng tÆ°Æ¡ng tÃ¡c.",
                    "XÃ¡c nháº­n trÃ¬nh chiáº¿u", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                // LÆ°u báº£n nhÃ¡p trÆ°á»›c khi chuyá»ƒn
                SavePlan("Draft");

                // LÆ°u ná»™i dung vÃ o AppState Ä‘á»ƒ SmartTouch nháº­n
                var app = Application.Current as QASmartTouch.App;
                if (app != null)
                {
                    app.Properties["PresentationTitle"] = title;
                    app.Properties["PresentationContent"] = content;
                    app.Properties["PresentationSubject"] = (CbSubject.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";

                    Log.Information("LessonPlan -> SmartTouch: Presenting '{Title}'", title);

                    // Chuyá»ƒn sang SmartTouch
                    app.ModeService.GoToScreen();
                }
                else
                {
                    Log.Warning("Application.Current is null or not of type QASmartTouch.App. Skipping SmartTouch projection.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lá»—i chuyá»ƒn sang SmartTouch Ä‘á»ƒ trÃ¬nh chiáº¿u");
                MessageBox.Show("ÄÃ£ xáº£y ra lá»—i khi má»Ÿ trÃ¬nh chiáº¿u.", "Lá»—i", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnImportWord_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Word Document|*.docx",
                    Title = "Chá»n file Word giÃ¡o Ã¡n Ä‘á»ƒ nháº­p"
                };

                if (dlg.ShowDialog() == true)
                {
                    string text = ExtractTextFromWord(dlg.FileName);
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        MessageBox.Show("File Word trá»‘ng hoáº·c khÃ´ng Ä‘á»c Ä‘Æ°á»£c ná»™i dung.", "ThÃ´ng bÃ¡o", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    TxtContent.Text = text;
                    MessageBox.Show("ÄÃ£ nháº­p ná»™i dung giÃ¡o Ã¡n tá»« file Word thÃ nh cÃ´ng!", "ThÃ´ng bÃ¡o", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lá»—i import giÃ¡o Ã¡n tá»« Word");
                MessageBox.Show($"ÄÃ£ xáº£y ra lá»—i khi Ä‘á»c file Word: {ex.Message}", "Lá»—i", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string ExtractTextFromWord(string filePath)
        {
            if (!System.IO.File.Exists(filePath)) return "";
            try
            {
                using var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(filePath, false);
                var body = doc.MainDocumentPart?.Document?.Body;
                if (body == null) return "";

                var sb = new System.Text.StringBuilder();
                foreach (var element in body.ChildElements)
                {
                    if (element is DocumentFormat.OpenXml.Wordprocessing.Paragraph para)
                    {
                        string text = para.InnerText;
                        if (!string.IsNullOrEmpty(text))
                        {
                            sb.AppendLine(text);
                        }
                    }
                    else if (element is DocumentFormat.OpenXml.Wordprocessing.Table table)
                    {
                        foreach (var row in table.Elements<DocumentFormat.OpenXml.Wordprocessing.TableRow>())
                        {
                            var cells = row.Elements<DocumentFormat.OpenXml.Wordprocessing.TableCell>().Select(c => c.InnerText);
                            sb.AppendLine(string.Join("\t", cells));
                        }
                    }
                }
                return sb.ToString().Trim();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lá»—i trÃ­ch xuáº¥t text tá»« Word trong LessonPlanPage");
                return "";
            }
        }

        private async void BtnDownloadTemplate_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;
            try
            {
                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Word Document|*.docx",
                    FileName = "Mau_Giao_An_5512.docx"
                };

                if (dlg.ShowDialog() == true)
                {
                    string filePath = dlg.FileName;
                    
                    await System.Threading.Tasks.Task.Run(() =>
                    {
                        // Create word document using OpenXml
                        using (var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Create(filePath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
                        {
                            var mainPart = doc.AddMainDocumentPart();
                            mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
                            var body = new DocumentFormat.OpenXml.Wordprocessing.Body();

                            // Add Title
                            var pTitle = new DocumentFormat.OpenXml.Wordprocessing.Paragraph();
                            var rTitle = new DocumentFormat.OpenXml.Wordprocessing.Run();
                            rTitle.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text("MáºªU GIÃO ÃN TIÃŠU CHUáº¨N (CÃ”NG VÄ‚N 5512)"));
                            pTitle.AppendChild(rTitle);
                            body.AppendChild(pTitle);

                            // Add content guidelines
                            string[] sections = new string[]
                            {
                                "",
                                "I. Má»¤C TIÃŠU BÃ€I Há»ŒC",
                                "1. Kiáº¿n thá»©c: NÃªu rÃµ cÃ¡c kiáº¿n thá»©c há»c sinh cáº§n Ä‘áº¡t Ä‘Æ°á»£c sau bÃ i há»c.",
                                "2. NÄƒng lá»±c: XÃ¡c Ä‘á»‹nh nÄƒng lá»±c chung vÃ  nÄƒng lá»±c Ä‘áº·c thÃ¹ cá»§a mÃ´n há»c.",
                                "3. Pháº©m cháº¥t: XÃ¡c Ä‘á»‹nh pháº©m cháº¥t há»c sinh cáº§n rÃ¨n luyá»‡n (chÄƒm chá»‰, trung thá»±c...).",
                                "",
                                "II. THIáº¾T Bá»Š Dáº Y Há»ŒC VÃ€ Há»ŒC LIá»†U",
                                "- Thiáº¿t bá»‹ Ä‘á»‘i vá»›i giÃ¡o viÃªn (mÃ¡y tÃ­nh, mÃ¡y chiáº¿u, báº£ng phá»¥...).",
                                "- Thiáº¿t bá»‹ Ä‘á»‘i vá»›i há»c sinh (vá»Ÿ bÃ i táº­p, dá»¥ng cá»¥ há»c táº­p...).",
                                "",
                                "III. TIáº¾N TRÃŒNH Dáº Y Há»ŒC",
                                "1. Hoáº¡t Ä‘á»™ng 1: XÃ¡c Ä‘á»‹nh váº¥n Ä‘á»/Nhiá»‡m vá»¥ há»c táº­p/Má»Ÿ Ä‘áº§u (Khá»Ÿi Ä‘á»™ng)",
                                "   - Má»¥c tiÃªu: ...",
                                "   - Ná»™i dung: ...",
                                "   - Sáº£n pháº©m: ...",
                                "   - Tá»• chá»©c thá»±c hiá»‡n: ...",
                                "2. Hoáº¡t Ä‘á»™ng 2: HÃ¬nh thÃ nh kiáº¿n thá»©c má»›i (KhÃ¡m phÃ¡)",
                                "3. Hoáº¡t Ä‘á»™ng 3: Luyá»‡n táº­p",
                                "4. Hoáº¡t Ä‘á»™ng 4: Váº­n dá»¥ng"
                            };

                            foreach (var sec in sections)
                            {
                                var p = new DocumentFormat.OpenXml.Wordprocessing.Paragraph();
                                var r = new DocumentFormat.OpenXml.Wordprocessing.Run();
                                r.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(sec));
                                p.AppendChild(r);
                                body.AppendChild(p);
                            }

                            mainPart.Document.AppendChild(body);
                            mainPart.Document.Save();
                        }
                    });

                    MessageBox.Show($"Táº£i máº«u giÃ¡o Ã¡n Word thÃ nh cÃ´ng!\n\nÄÆ°á»ng dáº«n: {filePath}", "ThÃ´ng bÃ¡o", MessageBoxButton.OK, MessageBoxImage.Information);
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lá»—i táº£i máº«u giÃ¡o Ã¡n Word");
                MessageBox.Show("ÄÃ£ xáº£y ra lá»—i khi táº£i file máº«u.", "Lá»—i", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }

        private void BtnInsertGoals_Click(object sender, RoutedEventArgs e)
        {
            InsertTextAtCaret("I. Má»¤C TIÃŠU BÃ€I Há»ŒC\n1. Kiáº¿n thá»©c:\n- ...\n2. NÄƒng lá»±c:\n- ...\n3. Pháº©m cháº¥t:\n- ...");
        }

        private void BtnInsertDevices_Click(object sender, RoutedEventArgs e)
        {
            InsertTextAtCaret("II. THIáº¾T Bá»Š Dáº Y Há»ŒC VÃ€ Há»ŒC LIá»†U\n- GiÃ¡o viÃªn:\n- Há»c sinh:");
        }

        private void BtnInsertSteps_Click(object sender, RoutedEventArgs e)
        {
            InsertTextAtCaret("III. TIáº¾N TRÃŒNH Dáº Y Há»ŒC\n1. Hoáº¡t Ä‘á»™ng 1: Khá»Ÿi Ä‘á»™ng\n2. Hoáº¡t Ä‘á»™ng 2: KhÃ¡m phÃ¡\n3. Hoáº¡t Ä‘á»™ng 3: Luyá»‡n táº­p\n4. Hoáº¡t Ä‘á»™ng 4: Váº­n dá»¥ng");
        }

        private void BtnClearContent_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("XÃ³a toÃ n bá»™ ná»™i dung giÃ¡o Ã¡n Ä‘ang soáº¡n?", "XÃ¡c nháº­n xÃ³a", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                TxtContent.Text = "";
            }
        }
        private void InsertTextAtCaret(string text)
        {
            int caretIndex = TxtContent.CaretIndex;
            TxtContent.Text = TxtContent.Text.Insert(caretIndex, text);
            TxtContent.CaretIndex = caretIndex + text.Length;
            TxtContent.Focus();
        }

        private void SavePlan(string status)
        {
            if (_db == null) return;
            try
            {
                LessonPlan plan;
                bool isNew = !_selectedPlanId.HasValue;

                if (isNew)
                {
                    plan = new LessonPlan
                    {
                        TeacherId = _currentTeacherId,
                        CreatedAt = DateTime.Now
                    };
                    _db.LessonPlans.Add(plan);
                }
                else
                {
                    plan = _db.LessonPlans.FirstOrDefault(p => p.Id == _selectedPlanId.Value) 
                           ?? new LessonPlan { TeacherId = _currentTeacherId, CreatedAt = DateTime.Now };
                }

                plan.Subject = (CbSubject.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
                plan.Grade = (CbGrade.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
                plan.Template = (CbTemplate.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
                plan.Title = TxtTitle.Text.Trim();
                plan.Content = TxtContent.Text;
                plan.Period = int.TryParse(TxtPeriod.Text, out int pr) ? pr : 1;
                plan.Status = status;

                _db.SaveChanges();

                _selectedPlanId = plan.Id;

                // Delete the draft since it's saved now
                var draft = _db.LessonPlanDrafts.FirstOrDefault(d => 
                    d.TeacherId == _currentTeacherId && 
                    d.LessonPlanId == plan.Id);
                if (draft != null)
                {
                    _db.LessonPlanDrafts.Remove(draft);
                    _db.SaveChanges();
                }
                BorderRestoreDraftAlert.Visibility = Visibility.Collapsed;

                // Create a new version snapshot
                SaveVersionSnapshot(plan.Id, plan.Title, plan.Content, status == "Done" ? "Báº£n chÃ­nh thá»©c" : "LÆ°u thá»§ cÃ´ng");

                MessageBox.Show(status == "Done" ? "ÄÃ£ lÆ°u vÃ  hoÃ n thÃ nh giÃ¡o Ã¡n!" : "ÄÃ£ lÆ°u báº£n nhÃ¡p thÃ nh cÃ´ng!", "ThÃ´ng bÃ¡o", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadLessonPlans();
                LoadVersions(plan.Id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lá»—i khi lÆ°u LessonPlan");
                MessageBox.Show("ÄÃ£ xáº£y ra lá»—i khi lÆ°u giÃ¡o Ã¡n.", "Lá»—i", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AutoSaveTimer_Tick(object? sender, EventArgs e)
        {
            if (_db == null) return;
            string title = TxtTitle.Text.Trim();
            string content = TxtContent.Text;

            // Don't auto-save if everything is empty
            if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(content)) return;

            try
            {
                var draft = _db.LessonPlanDrafts.FirstOrDefault(d => 
                    d.TeacherId == _currentTeacherId && 
                    d.LessonPlanId == _selectedPlanId);

                if (draft == null)
                {
                    draft = new LessonPlanDraft
                    {
                        TeacherId = _currentTeacherId,
                        LessonPlanId = _selectedPlanId
                    };
                    _db.LessonPlanDrafts.Add(draft);
                }

                draft.Subject = (CbSubject.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
                draft.Grade = (CbGrade.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
                draft.Template = (CbTemplate.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
                draft.Title = title;
                draft.Content = content;
                draft.Period = int.TryParse(TxtPeriod.Text, out int pr) ? pr : 1;
                draft.LastSaved = DateTime.Now;

                _db.SaveChanges();

                TxtAutoSaveStatus.Text = $"NhÃ¡p: Tá»± Ä‘á»™ng lÆ°u lÃºc {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Lá»—i khi tá»± Ä‘á»™ng lÆ°u nhÃ¡p giÃ¡o Ã¡n");
                TxtAutoSaveStatus.Text = "NhÃ¡p: Lá»—i tá»± Ä‘á»™ng lÆ°u";
            }
        }

        private void LvLessonPlans_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_db == null) return;
            var selectedItem = LvLessonPlans.SelectedItem;
            if (selectedItem == null) return;

            try
            {
                // Extract Id from anonymous type
                dynamic item = selectedItem;
                int id = item.Id;

                var plan = _db.LessonPlans.FirstOrDefault(p => p.Id == id);
                if (plan != null)
                {
                    _selectedPlanId = plan.Id;
                    TxtTitle.Text = plan.Title;
                    TxtContent.Text = plan.Content;
                    TxtPeriod.Text = plan.Period.ToString();

                    // Select correct subject in ComboBox
                    foreach (ComboBoxItem cbItem in CbSubject.Items)
                    {
                        if (cbItem.Content?.ToString() == plan.Subject)
                        {
                            CbSubject.SelectedItem = cbItem;
                            break;
                        }
                    }

                    // Select correct grade in ComboBox
                    foreach (ComboBoxItem cbItem in CbGrade.Items)
                    {
                        if (cbItem.Content?.ToString() == plan.Grade)
                        {
                            CbGrade.SelectedItem = cbItem;
                            break;
                        }
                    }

                    // Select correct template in ComboBox
                    foreach (ComboBoxItem cbItem in CbTemplate.Items)
                    {
                        if (cbItem.Content?.ToString() == plan.Template)
                        {
                            CbTemplate.SelectedItem = cbItem;
                            break;
                        }
                    }

                    // Check if there is a newer draft for this lesson plan
                    var draft = _db.LessonPlanDrafts.FirstOrDefault(d => 
                        d.TeacherId == _currentTeacherId && 
                        d.LessonPlanId == plan.Id);

                    if (draft != null && draft.LastSaved > plan.CreatedAt)
                    {
                        BorderRestoreDraftAlert.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        BorderRestoreDraftAlert.Visibility = Visibility.Collapsed;
                    }

                    LoadVersions(plan.Id);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lá»—i khi chá»n giÃ¡o Ã¡n tá»« danh sÃ¡ch");
            }
        }

        private void SaveVersionSnapshot(int planId, string title, string content, string notes)
        {
            if (_db == null) return;
            try
            {
                // Find last version number
                int maxVerNum = 0;
                var existingVersions = _db.LessonPlanVersions
                    .Where(v => v.LessonPlanId == planId)
                    .OrderBy(v => v.VersionNumber)
                    .ToList();

                if (existingVersions.Any())
                {
                    maxVerNum = existingVersions.Max(v => v.VersionNumber);
                }

                // If we reached the limit of 10 versions, delete the oldest (FIFO circular buffer)
                if (existingVersions.Count >= 10)
                {
                    var oldest = existingVersions.First();
                    _db.LessonPlanVersions.Remove(oldest);
                    _db.SaveChanges();
                }

                var newVersion = new LessonPlanVersion
                {
                    LessonPlanId = planId,
                    VersionNumber = maxVerNum + 1,
                    Title = title,
                    Content = content,
                    SavedAt = DateTime.Now,
                    Notes = notes
                };

                _db.LessonPlanVersions.Add(newVersion);
                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Lá»—i khi lÆ°u phiÃªn báº£n giÃ¡o Ã¡n");
            }
        }

        private void LoadVersions(int planId)
        {
            if (_db == null) return;
            try
            {
                var versions = _db.LessonPlanVersions
                    .Where(v => v.LessonPlanId == planId)
                    .OrderByDescending(v => v.VersionNumber)
                    .ToList();

                var displayVersions = versions.Select(v => new
                {
                    v.Id,
                    Title = $"PhiÃªn báº£n #{v.VersionNumber}: {v.Title}",
                    TimeAndNotes = $"LÆ°u lÃºc {v.SavedAt:dd/MM HH:mm} - {v.Notes}"
                }).ToList();

                LvVersions.ItemsSource = displayVersions;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lá»—i khi load danh sÃ¡ch phiÃªn báº£n giÃ¡o Ã¡n");
            }
        }

        private void LvVersions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void BtnRestoreVersion_Click(object sender, RoutedEventArgs e)
        {
            if (_db == null) return;
            var btn = sender as Button;
            if (btn == null || btn.Tag == null) return;

            try
            {
                int versionId = (int)btn.Tag;
                var version = _db.LessonPlanVersions.FirstOrDefault(v => v.Id == versionId);
                if (version != null)
                {
                    var result = MessageBox.Show($"Báº¡n cÃ³ cháº¯c cháº¯n muá»‘n khÃ´i phá»¥c giÃ¡o Ã¡n vá» PhiÃªn báº£n #{version.VersionNumber} khÃ´ng?\nNá»™i dung hiá»‡n táº¡i trong khung soáº¡n tháº£o sáº½ bá»‹ ghi Ä‘Ã¨.", "XÃ¡c nháº­n khÃ´i phá»¥c", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        TxtTitle.Text = version.Title;
                        TxtContent.Text = version.Content;
                        MessageBox.Show($"ÄÃ£ khÃ´i phá»¥c vá» PhiÃªn báº£n #{version.VersionNumber}!", "ThÃ´ng bÃ¡o", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lá»—i khi khÃ´i phá»¥c phiÃªn báº£n giÃ¡o Ã¡n");
            }
        }

        private void BtnRestoreDraft_Click(object sender, RoutedEventArgs e)
        {
            if (_db == null || !_selectedPlanId.HasValue) return;

            try
            {
                var draft = _db.LessonPlanDrafts.FirstOrDefault(d => 
                    d.TeacherId == _currentTeacherId && 
                    d.LessonPlanId == _selectedPlanId.Value);

                if (draft != null)
                {
                    TxtTitle.Text = draft.Title;
                    TxtContent.Text = draft.Content;
                    TxtPeriod.Text = draft.Period.ToString();

                    // Select correct template, subject, grade
                    foreach (ComboBoxItem cbItem in CbSubject.Items)
                    {
                        if (cbItem.Content?.ToString() == draft.Subject)
                        {
                            CbSubject.SelectedItem = cbItem;
                            break;
                        }
                    }
                    foreach (ComboBoxItem cbItem in CbGrade.Items)
                    {
                        if (cbItem.Content?.ToString() == draft.Grade)
                        {
                            CbGrade.SelectedItem = cbItem;
                            break;
                        }
                    }
                    foreach (ComboBoxItem cbItem in CbTemplate.Items)
                    {
                        if (cbItem.Content?.ToString() == draft.Template)
                        {
                            CbTemplate.SelectedItem = cbItem;
                            break;
                        }
                    }

                    BorderRestoreDraftAlert.Visibility = Visibility.Collapsed;
                    MessageBox.Show("ÄÃ£ khÃ´i phá»¥c thÃ nh cÃ´ng ná»™i dung tá»« báº£n lÆ°u nhÃ¡p má»›i nháº¥t!", "ThÃ´ng bÃ¡o", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lá»—i khi khÃ´i phá»¥c báº£n nhÃ¡p giÃ¡o Ã¡n");
            }
        }
    }
}
