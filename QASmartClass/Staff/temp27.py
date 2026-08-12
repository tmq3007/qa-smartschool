import sys

with open('D:/JOB/QA SmartSchool/QA SmartClass_Document/QASmartClass_Dev/QASmartClass/StudentClient/Views/StudentQuizPage.xaml.cs', 'r', encoding='utf-8') as f:
    code = f.read()

# 1. Randomize questions
old_q_load = """var questions = app.Database.Questions
                .Where(q => q.QuizId == quiz.Id)
                .OrderBy(q => q.SortOrder)
                .ToList();"""
new_q_load = """var rnd = new Random();
            var questions = app.Database.Questions
                .Where(q => q.QuizId == quiz.Id)
                .ToList()
                .OrderBy(q => rnd.Next())
                .ToList();"""
code = code.replace(old_q_load, new_q_load)

# 2. Shuffle options
old_opts = """var options = System.Text.Json.JsonSerializer.Deserialize<string[]>(question.OptionsJson ?? "[]") ?? Array.Empty<string>();
                                var labels = new[] { "A", "B", "C", "D", "E", "F" };
                                for (int i = 0; i < options.Length; i++)
                                {
                                    var rb = new RadioButton
                                    {
                                        Content = $"  {labels[i]}.  {options[i]}",
                                        GroupName = $"Q{question.Id}",
                                        FontSize = 13, Margin = new Thickness(42, 4, 0, 4),
                                        Tag = labels[i], Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66)),
                                        Cursor = System.Windows.Input.Cursors.Hand
                                    };
                                    stack.Children.Add(rb);
                                }"""

new_opts = """var options = System.Text.Json.JsonSerializer.Deserialize<string[]>(question.OptionsJson ?? "[]") ?? Array.Empty<string>();
                                var labels = new[] { "A", "B", "C", "D", "E", "F" };
                                
                                var optionPairs = new System.Collections.Generic.List<(string Label, string Text)>();
                                for (int i = 0; i < options.Length; i++)
                                {
                                    optionPairs.Add((labels[i], options[i]));
                                }
                                
                                var rndOpt = new Random();
                                var shuffled = optionPairs.OrderBy(x => rndOpt.Next()).ToList();
                                
                                for (int i = 0; i < shuffled.Count; i++)
                                {
                                    var rb = new RadioButton
                                    {
                                        Content = $"  {labels[i]}.  {shuffled[i].Text}",
                                        GroupName = $"Q{question.Id}",
                                        FontSize = 13, Margin = new Thickness(42, 4, 0, 4),
                                        Tag = shuffled[i].Label, // Keep the original label for grading
                                        Foreground = new SolidColorBrush(Color.FromRgb(66, 66, 66)),
                                        Cursor = System.Windows.Input.Cursors.Hand
                                    };
                                    stack.Children.Add(rb);
                                }"""

code = code.replace(old_opts, new_opts)

with open('D:/JOB/QA SmartSchool/QA SmartClass_Document/QASmartClass_Dev/QASmartClass/StudentClient/Views/StudentQuizPage.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(code)
