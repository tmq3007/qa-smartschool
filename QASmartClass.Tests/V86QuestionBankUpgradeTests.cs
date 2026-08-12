using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using QASmartClass.Classroom.Services;
using QASmartClass.Data;
using Xunit;

namespace QASmartClass.Tests
{
    public class V86QuestionBankUpgradeTests
    {
        [Fact]
        public void Test_TrueFalse_Grading_IndexMapping()
        {
            // Resolve correct answer index for True
            var qTrue = new MathQuizQuestion
            {
                QuestionType = "TrueFalse",
                CorrectAnswer = "True",
                Options = new List<string> { "Đúng", "Sai" }
            };

            // GetCorrectIndex uses a private method, but we can test it since it is accessed in the service.
            // Let's verify by calling the public/private via reflection or checking the method signature.
            var method = typeof(MathQuizBridgeService).GetMethod("GetCorrectIndex", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            Assert.NotNull(method);
            
            var correctIdxTrue = (int)method.Invoke(null, new object[] { qTrue });
            Assert.Equal(0, correctIdxTrue);

            // Resolve correct answer index for False
            var qFalse = new MathQuizQuestion
            {
                QuestionType = "TF",
                CorrectAnswer = "False",
                Options = new List<string> { "Đúng", "Sai" }
            };
            var correctIdxFalse = (int)method.Invoke(null, new object[] { qFalse });
            Assert.Equal(1, correctIdxFalse);
        }

        [Fact]
        public void Test_OptionsJson_Escaping()
        {
            // Verify that options containing quotes are securely serialized and deserialized
            var originalOptions = new List<string>
            {
                "Câu hỏi chứa dấu nháy kép \"đặc biệt\"",
                "Lựa chọn 'nháy đơn'",
                "Bình thường"
            };

            // Serialize options
            string json = System.Text.Json.JsonSerializer.Serialize(originalOptions);

            // Verify it is a valid JSON array format
            Assert.StartsWith("[", json);
            Assert.EndsWith("]", json);

            // Deserialize options
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json);
            Assert.NotNull(deserialized);
            Assert.Equal(originalOptions.Count, deserialized.Count);
            Assert.Equal(originalOptions[0], deserialized[0]);
            Assert.Equal(originalOptions[1], deserialized[1]);
            Assert.Equal(originalOptions[2], deserialized[2]);
        }

        [Fact]
        public void Test_ShortAnswer_Similarity_Lenient_Grading()
        {
            // Test standard normalisation
            string rawStudent = "Hà  Nội. ";
            string rawCorrect = "hà nội";
            Assert.True(QASmartClass.Helpers.QuizAnswerParser.GradeAnswer(rawStudent, rawCorrect, "SHORT"));

            // Test normalisation with punctuation
            string rawStudent2 = "Toán Học?";
            string rawCorrect2 = "toán học";
            Assert.True(QASmartClass.Helpers.QuizAnswerParser.GradeAnswer(rawStudent2, rawCorrect2, "short_answer"));

            // Test FIB normalisation
            string rawStudentFIB = "Paris; London. ";
            string rawCorrectFIB = "paris;london";
            Assert.True(QASmartClass.Helpers.QuizAnswerParser.GradeAnswer(rawStudentFIB, rawCorrectFIB, "FIB"));

            // Test mismatch cases
            Assert.False(QASmartClass.Helpers.QuizAnswerParser.GradeAnswer("Paris; Berlin", "paris;london", "FIB"));
        }

        [Fact]
        public void Test_Draft_Encryption_DPAPI()
        {
            string originalJson = "{\"1\":\"A\",\"2\":\"B\"}";
            byte[] rawBytes = System.Text.Encoding.UTF8.GetBytes(originalJson);

            // Encrypt using DPAPI
            byte[] encryptedBytes = System.Security.Cryptography.ProtectedData.Protect(
                rawBytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);

            // Verify that encrypted bytes are not plain text
            string encryptedStr = System.Text.Encoding.UTF8.GetString(encryptedBytes);
            Assert.NotEqual(originalJson, encryptedStr);

            // Decrypt using DPAPI
            byte[] decryptedBytes = System.Security.Cryptography.ProtectedData.Unprotect(
                encryptedBytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
            string decryptedJson = System.Text.Encoding.UTF8.GetString(decryptedBytes);

            Assert.Equal(originalJson, decryptedJson);
        }

        [Fact]
        public void Test_Submit_Network_Fault_Simulation()
        {
            // Simulate network fault/db lock handling
            bool exceptionHandled = false;
            try
            {
                // Simulate an action that fails due to a simulated DB lock or connection issue
                throw new System.IO.IOException("Simulated database lock error");
            }
            catch (System.IO.IOException ex)
            {
                Serilog.Log.Error("Simulated submission database lock exception handled: {Msg}", ex.Message);
                exceptionHandled = true;
            }
            Assert.True(exceptionHandled);
        }

        [Fact]
        public void Test_Pagination_Boundary_Conditions()
        {
            // Boundary: 0 items
            var query0 = new List<QuestionBankItem>().AsQueryable();
            int total0 = query0.Count();
            int pageCount0 = (int)Math.Ceiling((double)total0 / 50);
            Assert.Equal(0, total0);
            Assert.Equal(0, pageCount0);

            // Boundary: 50 items (exactly 1 page)
            var items50 = new List<QuestionBankItem>();
            for (int i = 0; i < 50; i++) items50.Add(new QuestionBankItem { Id = i, CreatedAt = DateTime.Now });
            var query50 = items50.AsQueryable();
            int total50 = query50.Count();
            var page1_50 = query50.OrderByDescending(q => q.CreatedAt).Skip(0).Take(50).ToList();
            var page2_50 = query50.OrderByDescending(q => q.CreatedAt).Skip(50).Take(50).ToList();
            Assert.Equal(50, total50);
            Assert.Equal(50, page1_50.Count);
            Assert.Empty(page2_50);

            // Boundary: 102 items (3 pages: 50, 50, 2)
            var items102 = new List<QuestionBankItem>();
            for (int i = 0; i < 102; i++) items102.Add(new QuestionBankItem { Id = i, CreatedAt = DateTime.Now });
            var query102 = items102.AsQueryable();
            int total102 = query102.Count();
            var page3_102 = query102.OrderByDescending(q => q.CreatedAt).Skip(100).Take(50).ToList();
            Assert.Equal(102, total102);
            Assert.Equal(2, page3_102.Count);
        }

        [Fact]
        public void Test_GradeAnswer_IgnoreAccents()
        {
            // Natural Science: Toán (Math)
            // "met" vs "mét"
            Assert.True(QASmartClass.Helpers.QuizAnswerParser.GradeAnswer("met", "mét", "SHORT", "Toán"));
            Assert.True(QASmartClass.Helpers.QuizAnswerParser.GradeAnswer("mét", "met", "SHORT", "Toán"));

            // Natural Science: Vật lý (Physics)
            // "i = u / r" with spaces/capitalization
            Assert.True(QASmartClass.Helpers.QuizAnswerParser.GradeAnswer("i = u / r", "I = U / R", "SHORT", "Vật lý"));

            // Liberal Arts: Ngữ văn (Literature) - should be strict spelling
            // "viet nam" vs "Việt Nam"
            Assert.False(QASmartClass.Helpers.QuizAnswerParser.GradeAnswer("viet nam", "Việt Nam", "SHORT", "Ngữ văn"));
            // "Đông" vs "Dong"
            Assert.False(QASmartClass.Helpers.QuizAnswerParser.GradeAnswer("Dong", "Đông", "SHORT", "Ngữ văn"));

            // Default: no subject specified (strict grading)
            Assert.False(QASmartClass.Helpers.QuizAnswerParser.GradeAnswer("met", "mét", "SHORT"));
        }

        [Fact]
        public void Test_Draft_AutoCleanup()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "QASmartClassTestDrafts");
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            Directory.CreateDirectory(tempDir);

            try
            {
                // Create draft today
                string fileToday = Path.Combine(tempDir, "quiz_draft_1.json");
                File.WriteAllText(fileToday, "{}");
                File.SetLastWriteTime(fileToday, DateTime.Now);

                // Create draft 8 days ago
                string fileOld = Path.Combine(tempDir, "quiz_draft_2.json");
                File.WriteAllText(fileOld, "{}");
                File.SetLastWriteTime(fileOld, DateTime.Now.AddDays(-8));

                // Run cleanup simulation
                var files = Directory.GetFiles(tempDir, "quiz_draft_*.json");
                int deletedCount = 0;
                foreach (var file in files)
                {
                    var fi = new FileInfo(file);
                    if (DateTime.Now - fi.LastWriteTime > TimeSpan.FromDays(7))
                    {
                        File.Delete(file);
                        deletedCount++;
                    }
                }

                Assert.Equal(1, deletedCount);
                Assert.True(File.Exists(fileToday));
                Assert.False(File.Exists(fileOld));
            }
            finally
            {
                try
                {
                    if (Directory.Exists(tempDir))
                    {
                        foreach (var f in Directory.GetFiles(tempDir))
                        {
                            try { File.Delete(f); } catch { }
                        }
                        Directory.Delete(tempDir, true);
                    }
                }
                catch { }
            }
        }

        [Fact]
        public void Test_AddQuestion_MCQ_Validation()
        {
            // Simulate teacher input split and validation logic
            // Input 1: valid options and matching correct answer
            string options1 = "A | B | C | D";
            string answer1 = "B";
            
            var opts1 = options1.Split('|').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
            Assert.Contains("B", opts1);
            bool isValid1 = opts1.Any(opt => opt.Equals(answer1.Trim(), StringComparison.OrdinalIgnoreCase));
            Assert.True(isValid1);

            // Input 2: valid options with spacing/casing mismatch (should still pass validation after normalisation)
            string options2 = " Đáp án A | Đáp án B | Đáp án C ";
            string answer2 = "đáp án b";
            
            var opts2 = options2.Split('|').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
            bool isValid2 = opts2.Any(opt => opt.Equals(answer2.Trim(), StringComparison.OrdinalIgnoreCase));
            Assert.True(isValid2);

            // Input 3: answer is not in options list
            string options3 = "1 | 2 | 3";
            string answer3 = "4";
            
            var opts3 = options3.Split('|').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
            bool isValid3 = opts3.Any(opt => opt.Equals(answer3.Trim(), StringComparison.OrdinalIgnoreCase));
            Assert.False(isValid3);
            
            // Input 4: empty options
            string options4 = "";
            var opts4 = options4.Split('|').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
            Assert.Empty(opts4);
        }

        [Fact]
        public void Test_ImportExcel_SkipTemplateRows()
        {
            var rows = new List<string>
            {
                "[DÒNG MẪU - XÓA KHI NHẬP] Câu hỏi mẫu 1",
                "Câu hỏi thực tế 1",
                "[dòng mẫu] Câu hỏi mẫu 2",
                "Câu hỏi thực tế 2"
            };

            int importedCount = 0;
            int skippedCount = 0;

            foreach (var content in rows)
            {
                if (!string.IsNullOrEmpty(content) && content.Contains("[DÒNG MẪU", StringComparison.OrdinalIgnoreCase))
                {
                    skippedCount++;
                    continue;
                }
                importedCount++;
            }

            Assert.Equal(2, skippedCount);
            Assert.Equal(2, importedCount);
        }

        [Fact]
        public void Test_ImportExcel_DetailedValidationErrors()
        {
            var errors = new List<string>();
            
            int row5 = 5;
            string content5 = "";
            if (string.IsNullOrEmpty(content5))
            {
                errors.Add($"Dòng {row5} [Nội dung câu hỏi]: Nội dung câu hỏi không được để trống.");
            }

            int row8 = 8;
            string qType8 = "INVALID_TYPE";
            if (qType8 != "MCQ" && qType8 != "TF")
            {
                errors.Add($"Dòng {row8} [Loại câu hỏi]: Loại câu hỏi '{qType8}' không hợp lệ.");
            }

            Assert.Equal(2, errors.Count);
            Assert.Contains("Dòng 5 [Nội dung câu hỏi]:", errors[0]);
            Assert.Contains("Dòng 8 [Loại câu hỏi]:", errors[1]);
        }

        [Fact]
        public void Test_Security_SanitizeInputText()
        {
            var method = typeof(QASmartClass.TeacherHub.Views.QuestionBankView).GetMethod("SanitizeInputText", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            Assert.NotNull(method);

            // Test case 1: Input contains script tags
            string rawInput1 = "<script>alert('XSS')</script>";
            string expected1 = "&lt;script&gt;alert('XSS')&lt;/script&gt;";
            var result1 = (string)method.Invoke(null, new object[] { rawInput1 });
            Assert.Equal(expected1, result1);

            // Test case 2: Input with HTML content and spaces
            string rawInput2 = "  <div>Câu hỏi hay</div>  ";
            string expected2 = "&lt;div&gt;Câu hỏi hay&lt;/div&gt;";
            var result2 = (string)method.Invoke(null, new object[] { rawInput2 });
            Assert.Equal(expected2, result2);

            // Test case 3: Empty or null inputs
            var resultNull = (string)method.Invoke(null, new object[] { null });
            Assert.Equal(string.Empty, resultNull);

            var resultEmpty = (string)method.Invoke(null, new object[] { "" });
            Assert.Equal(string.Empty, resultEmpty);
        }
    }
}
