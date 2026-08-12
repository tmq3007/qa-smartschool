using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QASmartClass.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace QASmartClass.Services
{
    public class ContactBookService
    {
        private readonly AppDbContext _db;
        private readonly TT22GradingService _gradingService;

        public ContactBookService(AppDbContext db)
        {
            _db = db;
            _gradingService = new TT22GradingService(db);
        }

        public List<ContactBookEntry> GetContactBookEntries(int classroomId, string schoolYear, string semester)
        {
            var students = _db.Students.Where(s => s.ClassroomId == classroomId && s.Status == "Active").ToList();
            var entries = new List<ContactBookEntry>();

            foreach (var student in students)
            {
                var entry = GetOrCalculateEntry(student.Id, schoolYear, semester);
                entries.Add(entry);
            }

            return entries;
        }

        public ContactBookEntry GetOrCalculateEntry(int studentId, string schoolYear, string semester)
        {
            var entry = _db.ContactBookEntries.FirstOrDefault(e =>
                e.StudentId == studentId &&
                e.SchoolYear == schoolYear &&
                e.Semester == semester);

            if (entry != null)
            {
                var student = _db.Students.FirstOrDefault(s => s.Id == studentId);
                if (student != null)
                {
                    var results = CalculateStudentGPA(studentId, schoolYear, semester);
                    int newAbsent = CalculateAbsentDays(studentId, schoolYear, semester);
                    string newConduct = TT22GradingService.ClassifyConduct(student.ConductScore);
                    
                    if (entry.AverageGrade != results.GPA || entry.AbsentDays != newAbsent || entry.Conduct != newConduct)
                    {
                        entry.AverageGrade = results.GPA;
                        entry.AbsentDays = newAbsent;
                        entry.Conduct = newConduct;
                        entry.UpdatedAt = DateTime.Now;
                        _db.SaveChanges();
                    }
                }
                return entry;
            }

            var studentInfo = _db.Students.FirstOrDefault(s => s.Id == studentId);
            if (studentInfo == null)
            {
                throw new ArgumentException("Student not found.");
            }

            var gpaResults = CalculateStudentGPA(studentId, schoolYear, semester);
            int absentDays = CalculateAbsentDays(studentId, schoolYear, semester);
            string conduct = TT22GradingService.ClassifyConduct(studentInfo.ConductScore);

            entry = new ContactBookEntry
            {
                StudentId = studentId,
                StudentName = studentInfo.FullName,
                SchoolYear = schoolYear,
                Semester = semester,
                AverageGrade = gpaResults.GPA,
                AbsentDays = absentDays,
                Conduct = conduct,
                TeacherComment = "Học tập tốt, chuyên cần tích cực.",
                ParentFeedback = string.Empty,
                UpdatedAt = DateTime.Now
            };

            _db.ContactBookEntries.Add(entry);
            _db.SaveChanges();

            return entry;
        }

        public void SaveComment(int entryId, string comment)
        {
            var entry = _db.ContactBookEntries.Find(entryId);
            if (entry != null)
            {
                entry.TeacherComment = comment;
                entry.UpdatedAt = DateTime.Now;
                _db.SaveChanges();
            }
        }

        public void SaveParentFeedback(int entryId, string feedback)
        {
            var entry = _db.ContactBookEntries.Find(entryId);
            if (entry != null)
            {
                entry.ParentFeedback = feedback;
                entry.UpdatedAt = DateTime.Now;
                _db.SaveChanges();
            }
        }

        public string ExportContactBookPdf(int studentId, string schoolYear, string semester)
        {
            var student = _db.Students.FirstOrDefault(s => s.Id == studentId);
            if (student == null) throw new ArgumentException("Student not found.");

            var entry = GetOrCalculateEntry(studentId, schoolYear, semester);
            var subjectAverages = CalculateStudentGPA(studentId, schoolYear, semester).SubjectAverages;

            string outputPath = PdfTemplateHelper.GetOutputPath("SoLienLac", student.FullName);

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontFamily("Segoe UI").FontSize(10));

                    // Header
                    page.Header().Element(c => PdfTemplateHelper.ComposeHeader(c, "SỔ LIÊN LẠC ĐIỆN TỬ", $"Học kỳ: {semester} | Năm học: {schoolYear}"));

                    // Content
                    page.Content().PaddingTop(15).Column(col =>
                    {
                        // Student details
                        col.Item().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(8).Column(details =>
                        {
                            details.Item().Row(r =>
                            {
                                r.RelativeItem().Text(t => { t.Span("Họ và tên: ").Bold(); t.Span(student.FullName); });
                                r.RelativeItem().Text(t => { t.Span("Mã học sinh: ").Bold(); t.Span(student.StudentCode); });
                            });
                            details.Item().PaddingTop(4).Row(r =>
                            {
                                r.RelativeItem().Text(t => { t.Span("Lớp: ").Bold(); t.Span(student.ClassName); });
                                r.RelativeItem().Text(t => { t.Span("Trường: ").Bold(); t.Span(student.SchoolName); });
                            });
                            details.Item().PaddingTop(4).Row(r =>
                            {
                                r.RelativeItem().Text(t => { t.Span("Phụ huynh: ").Bold(); t.Span(student.ParentName); });
                                r.RelativeItem().Text(t => { t.Span("SĐT phụ huynh: ").Bold(); t.Span(student.ParentPhone); });
                            });
                        });

                        col.Item().PaddingTop(15).Text("I. KẾT QUẢ HỌC TẬP CHI TIẾT").Bold().FontSize(11).FontColor(Colors.Blue.Darken3);

                        // Subject GPA table
                        col.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(40); // STT
                                columns.RelativeColumn();   // Môn học
                                columns.RelativeColumn();   // Điểm trung bình môn
                                columns.RelativeColumn();   // Xếp loại môn học
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("STT");
                                header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).Text("Môn học");
                                header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Điểm TB môn");
                                header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Xếp loại");
                            });

                            int i = 1;
                            foreach (var sa in subjectAverages)
                            {
                                bool isAlternate = i % 2 == 0;
                                table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, isAlternate)).AlignCenter().Text(i.ToString());
                                table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, isAlternate)).Text(sa.SubjectName);
                                table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, isAlternate)).AlignCenter().Text(sa.Average.ToString("0.00"));
                                table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, isAlternate)).AlignCenter().Text(sa.Classification);
                                i++;
                            }
                        });

                        // General emulation and attendance
                        col.Item().PaddingTop(15).Text("II. TỔNG HỢP RÈN LUYỆN & CHUYÊN CẦN").Bold().FontSize(11).FontColor(Colors.Blue.Darken3);

                        col.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Điểm TB Học kỳ");
                                header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Hạnh kiểm");
                                header.Cell().Element(PdfTemplateHelper.TableHeaderStyle).AlignCenter().Text("Số ngày nghỉ học");
                            });

                            table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, false)).AlignCenter().Text(entry.AverageGrade.ToString("0.00"));
                            table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, false)).AlignCenter().Text(entry.Conduct);
                            table.Cell().Element(c => PdfTemplateHelper.TableCellStyle(c, false)).AlignCenter().Text(entry.AbsentDays.ToString());
                        });

                        // Comments section
                        col.Item().PaddingTop(15).Text("III. ĐÁNH GIÁ CỦA GIÁO VIÊN & Ý KIẾN GIA ĐÌNH").Bold().FontSize(11).FontColor(Colors.Blue.Darken3);

                        col.Item().PaddingTop(5).Row(r =>
                        {
                            r.RelativeItem().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(box =>
                            {
                                box.Item().Text("Nhận xét của Giáo viên chủ nhiệm:").Bold();
                                box.Item().PaddingTop(4).Text(string.IsNullOrWhiteSpace(entry.TeacherComment) ? "Chưa có nhận xét." : entry.TeacherComment);
                            });

                            r.ConstantItem(15);

                            r.RelativeItem().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(box =>
                            {
                                box.Item().Text("Ý kiến của Phụ huynh học sinh:").Bold();
                                box.Item().PaddingTop(4).Text(string.IsNullOrWhiteSpace(entry.ParentFeedback) ? "Chưa có phản hồi." : entry.ParentFeedback);
                            });
                        });

                        // Signature block
                        col.Item().Element(c => PdfTemplateHelper.ComposeSignatureBlock(c, "Giáo viên chủ nhiệm", "Hiệu trưởng"));
                    });

                    // Footer
                    page.Footer().Element(PdfTemplateHelper.ComposeFooter);
                });
            }).GeneratePdf(outputPath);

            return outputPath;
        }

        private (double GPA, List<SubjectAvgDto> SubjectAverages) CalculateStudentGPA(int studentId, string schoolYear, string semester)
        {
            var studentRosters = _db.ClassRosterStudents
                .Where(crs => crs.StudentId == studentId)
                .ToList();

            var rosterIds = studentRosters.Select(crs => crs.RosterId).ToList();
            var rosters = _db.ClassRosters
                .Where(r => rosterIds.Contains(r.Id) && r.SchoolYear == schoolYear && r.Semester == semester && r.IsActive)
                .ToList();

            var activeRosterIds = rosters.Select(r => r.Id).ToList();
            var allGrades = _db.StudentGrades
                .Where(g => g.StudentId == studentId && activeRosterIds.Contains(g.RosterId))
                .ToList();

            var gkTypeIds = _db.GradeTypeMasters
                .Where(t => t.Id == 2 || t.ShortName == "GK" || t.Code == "GK" || t.Code == "KT1Tiet")
                .Select(t => t.Id)
                .ToList();
            var ckTypeIds = _db.GradeTypeMasters
                .Where(t => t.Id == 3 || t.ShortName == "CK" || t.Code == "CK" || t.Code == "HocKy")
                .Select(t => t.Id)
                .ToList();
            var typeWeights = _db.GradeTypeMasters.ToDictionary(t => t.Id, t => t.Weight);

            var subjectAverages = new List<SubjectAvgDto>();
            bool hasIncomplete = false;
            foreach (var roster in rosters)
            {
                var grades = allGrades.Where(g => g.RosterId == roster.Id).ToList();
                if (grades.Any())
                {
                    bool hasGK = grades.Any(g => gkTypeIds.Contains(g.GradeTypeId));
                    bool hasCK = grades.Any(g => ckTypeIds.Contains(g.GradeTypeId));

                    if (!hasGK || !hasCK)
                    {
                        string classification = "Chưa đủ điểm";
                        if (!hasGK && !hasCK) classification = "Chưa đủ điểm (Thiếu GK, CK)";
                        else if (!hasGK) classification = "Chưa đủ điểm (Thiếu GK)";
                        else if (!hasCK) classification = "Chưa đủ điểm (Thiếu CK)";

                        subjectAverages.Add(new SubjectAvgDto
                        {
                            SubjectName = roster.Subject,
                            Average = 0,
                            Classification = classification
                        });
                        hasIncomplete = true;
                        continue;
                    }

                    double totalWeighted = 0;
                    int totalWeight = 0;
                    foreach (var g in grades)
                    {
                        if (g.Notes == "Miễn") continue;

                        int weight = typeWeights.TryGetValue(g.GradeTypeId, out var w) ? w : 1;

                        totalWeighted += g.Score * weight;
                        totalWeight += weight;
                    }

                    double avg = totalWeight > 0 ? Math.Round(totalWeighted / totalWeight, 2) : 0;
                    subjectAverages.Add(new SubjectAvgDto
                    {
                        SubjectName = roster.Subject,
                        Average = avg,
                        Classification = TT22GradingService.ClassifyAcademic(avg)
                    });
                }
            }

            double gpa = 0.0;
            if (subjectAverages.Any() && !hasIncomplete)
            {
                gpa = Math.Round(subjectAverages.Average(sa => sa.Average), 2);
            }

            return (gpa, subjectAverages);
        }

        private int CalculateAbsentDays(int studentId, string schoolYear, string semester)
        {
            var studentRosterIds = _db.ClassRosterStudents
                .Where(crs => crs.StudentId == studentId)
                .Select(crs => crs.RosterId)
                .ToList();

            var rosters = _db.ClassRosters
                .Where(r => studentRosterIds.Contains(r.Id) && r.SchoolYear == schoolYear && r.Semester == semester && r.IsActive)
                .Select(r => r.Id)
                .ToList();

            return _db.AttendanceRecords
                .Count(a => a.StudentId == studentId && rosters.Contains(a.RosterId) &&
                            (a.Status == "absent" || a.Status == "Vắng"));
        }

        public class SubjectAvgDto
        {
            public string SubjectName { get; set; } = string.Empty;
            public double Average { get; set; }
            public string Classification { get; set; } = string.Empty;
        }
    }
}

