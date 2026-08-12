import sys

with open('D:/JOB/QA SmartSchool/QA SmartClass_Document/QASmartClass_Dev/QASmartClass/Classroom/Views/QuestionBankPage.xaml.cs', 'r', encoding='utf-8') as f:
    code = f.read()

old_cmb = """cmbType.Items.Add(new ComboBoxItem { Content = "🏆 Thi đua", Tag = "Competition" });
                            cmbType.Items.Add(new ComboBoxItem { Content = "📋 Kiểm tra", Tag = "Test" });
                            cmbType.Items.Add(new ComboBoxItem { Content = "📊 Khảo sát", Tag = "Survey" });
                            cmbType.Items.Add(new ComboBoxItem { Content = "🎮 Trò chơi", Tag = "Game" });"""

new_cmb = """cmbType.Items.Add(new ComboBoxItem { Content = "🏆 Thi đua", Tag = "Competition" });
                            cmbType.Items.Add(new ComboBoxItem { Content = "📝 Kiểm tra 15 phút", Tag = "KT15p" });
                            cmbType.Items.Add(new ComboBoxItem { Content = "📋 Kiểm tra 1 tiết", Tag = "KT1Tiet" });
                            cmbType.Items.Add(new ComboBoxItem { Content = "📊 Khảo sát", Tag = "Survey" });
                            cmbType.Items.Add(new ComboBoxItem { Content = "🎮 Trò chơi", Tag = "Game" });"""

code = code.replace(old_cmb, new_cmb)

with open('D:/JOB/QA SmartSchool/QA SmartClass_Document/QASmartClass_Dev/QASmartClass/Classroom/Views/QuestionBankPage.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(code)

with open('D:/JOB/QA SmartSchool/QA SmartClass_Document/QASmartClass_Dev/QASmartClass/StudentClient/Views/StudentQuizPage.xaml.cs', 'r', encoding='utf-8') as f:
    code2 = f.read()

old_grade = """// Tìm GradeTypeMaster cho "Quiz" hoặc lấy loại đầu tiên
                                var gradeType = app.Database.GradeTypeMasters.FirstOrDefault(g => g.Code == "Quiz" || g.ShortName == "15p")
                                    ?? app.Database.GradeTypeMasters.FirstOrDefault();"""

new_grade = """Data.GradeTypeMaster? gradeType = null;
                                if (quiz.QuizType == "KT15p" || quiz.QuizType == "KT1Tiet")
                                {
                                    gradeType = app.Database.GradeTypeMasters.FirstOrDefault(g => g.Code == quiz.QuizType);
                                }
                                else if (quiz.QuizType == "Test") // Fallback cho DB cu
                                {
                                    gradeType = app.Database.GradeTypeMasters.FirstOrDefault(g => g.Code == "KT15p" || g.ShortName == "15p");
                                }"""

code2 = code2.replace(old_grade, new_grade)

with open('D:/JOB/QA SmartSchool/QA SmartClass_Document/QASmartClass_Dev/QASmartClass/StudentClient/Views/StudentQuizPage.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(code2)
