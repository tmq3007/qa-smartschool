using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Encodings.Web;

namespace ExamGenerator
{
    class Program
    {
        static void NotMain(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            string dataDir = @"d:\JOB\QA SmartSchool\QA SmartClass_Document\QASmartClass_Dev\QASmartClass\Resources\LiteratureData\Phase4_Data";

            var topics = new[] { "Tập đọc", "Chính tả", "Luyện từ và câu", "Tập làm văn" };

            var jsonOptions = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            for (int grade = 1; grade <= 5; grade++)
            {
                for (int ch = 1; ch <= 4; ch++)
                {
                    string chapterName = $"Bài {ch}: {topics[ch - 1]} Lớp {grade}";
                    string fileName = $"G{grade:D2}_CH{ch:D2}.json";
                    string filePath = Path.Combine(dataDir, fileName);

                    var problems = new List<object>();

                    if (topics[ch - 1] == "Tập đọc")
                    {
                        problems.Add(new { id = "p1", question = "Đọc đúng đoạn văn và trả lời: Nội dung chính của bài đọc là gì?", difficulty = "Easy", options = new[] { "Ca ngợi thiên nhiên", "Kể chuyện cổ tích", "Giáo dục lòng hiếu thảo", "Miêu tả con vật" }, answer = "Giáo dục lòng hiếu thảo", hints = new[] { "Đọc kỹ phần cuối" }, explanation = "Bài đọc hướng các em biết yêu thương ông bà cha mẹ." });
                        problems.Add(new { id = "p2", question = "Nhân vật chính trong bài đọc có tính cách như thế nào?", difficulty = "Medium", options = new[] { "Ngoan ngoãn, chăm chỉ?", "Lười biếng", "Nghịch ngợm", "Nhút nhát" }, answer = "Ngoan ngoãn, chăm chỉ?", hints = new[] { "Bạn nhỏ đã giúp mẹ làm gì?" }, explanation = "Bạn nhỏ biết giúp mẹ quét nhà, trông em nên rất ngoan ngoãn." });
                    }
                    else if (topics[ch - 1] == "Chính tả")
                    {
                        problems.Add(new { id = "p1", question = "Chọn từ viết đúng chính tả:", difficulty = "Easy", options = new[] { "Xuất sắc", "Suất sắc", "Xuất xắc", "Suất xắc" }, answer = "Xuất sắc", hints = new[] { "Chú ý âm s/x" }, explanation = "Từ đúng là 'xuất sắc' (tài giỏi vượt trội)." });
                        problems.Add(new { id = "p2", question = "Điền ch/tr vào chỗ trống: ...ong vắt", difficulty = "Medium", options = new[] { "Tr", "Ch", "Gi", "R" }, answer = "Tr", hints = new[] { "Màu nước hồ..." }, explanation = "Trong vắt: rất trong, không có vẩn đục." });
                    }
                    else if (topics[ch - 1] == "Luyện từ và câu")
                    {
                        problems.Add(new { id = "p1", question = "Từ nào là danh từ trong câu: 'Con mèo đang ngủ'?", difficulty = "Easy", options = new[] { "Con mèo", "Đang", "Ngủ", "Không có danh từ" }, answer = "Con mèo", hints = new[] { "Danh từ chỉ con vật" }, explanation = "'Con mèo' là danh từ chỉ con vật." });
                        problems.Add(new { id = "p2", question = "Câu 'Bầu trời xanh thẳm' thuộc kiểu câu gì?", difficulty = "Medium", options = new[] { "Ai thế nào?", "Ai làm gì?", "Ai là gì?", "Câu cảm thán" }, answer = "Ai thế nào?", hints = new[] { "Câu miêu tả tính chất" }, explanation = "Đây là kiểu câu 'Ai thế nào?' dùng để miêu tả đặc điểm của sự vật (bầu trời)." });
                    }
                    else
                    {
                        problems.Add(new { id = "p1", question = "Sắp xếp các câu sau thành một đoạn văn hoàn chỉnh tả con vật:", difficulty = "Hard", options = new[] { "1-2-3-4", "2-1-4-3", "3-4-1-2", "4-3-2-1" }, answer = "1-2-3-4", hints = new[] { "Mở bài -> Thân bài -> Kết bài" }, explanation = "Cần giới thiệu con vật trước, sau đó tả hình dáng, hoạt động rồi nêu tình cảm." });
                    }

                    var data = new {
                        metadata = new {
                            chapterId = $"g{grade:D2}_ch{ch:D2}",
                            chapterName = chapterName,
                            grade = grade,
                            totalProblems = problems.Count,
                            totalSections = 1,
                            estimatedHours = 2,
                            thptWeight = "Kiến thức nền tảng Tiếng Việt Tiểu Học"
                        },
                        sections = new[] {
                            new {
                                sectionId = $"s_g{grade}_ch{ch}_0",
                                sectionName = topics[ch - 1],
                                sgkUrl = $"https://www.hoc10.vn/doc-sach/tieng-viet-{grade}-1/1/1/4/",
                                audioUrl = $"https://api.qasmartclass.vn/audio/g{grade}_{topics[ch - 1].Replace(" ", "_")}.mp3",
                                concepts = new[] {
                                    new { name = "Mục tiêu bài học", theorem = $"Nắm vững kiến thức cơ bản về phân môn {topics[ch - 1]} theo chương trình GDPT 2018." }
                                },
                                problems = problems
                            }
                        }
                    };

                    string jsonString = JsonSerializer.Serialize(data, jsonOptions);
                    File.WriteAllText(filePath, jsonString, System.Text.Encoding.UTF8);
                    Console.WriteLine($"Generated: {fileName}");
                }
            }
            Console.WriteLine("Done generating Grade 1-5 Tiếng Việt files.");
        }
    }
}
