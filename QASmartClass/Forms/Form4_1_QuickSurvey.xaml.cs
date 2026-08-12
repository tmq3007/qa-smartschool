using System.Windows;

namespace QASmartTouch.Forms
{
    public partial class Form4_1_QuickSurvey : Window
    {
        private int _questionCount = 3;

        public Form4_1_QuickSurvey()
        {
            InitializeComponent();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnAddQuestion_Click(object sender, RoutedEventArgs e)
        {
            _questionCount++;
            MessageBox.Show($"Đã thêm câu hỏi {_questionCount}\n\nBạn có thể tùy chỉnh loại câu hỏi:\n• Trắc nghiệm\n• Đánh giá sao\n• Có/Không\n• Thang điểm 1-10\n• Tự luận", 
                          "Thêm câu hỏi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnDeleteQuestion_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            string questionNumber = button?.Tag?.ToString() ?? "?";
            
            var result = MessageBox.Show($"Xóa câu hỏi {questionNumber}?", 
                                       "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _questionCount--;
                MessageBox.Show($"Đã xóa câu hỏi {questionNumber}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnPreview_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Show survey preview
            string surveyInfo = $"📋 {txtSurveyTitle.Text}\n\n";
            surveyInfo += $"Mô tả: {txtSurveyDescription.Text}\n\n";
            surveyInfo += $"Số câu hỏi: {_questionCount}\n";
            surveyInfo += $"Ẩn danh: {(chkAnonymous.IsChecked == true ? "Có" : "Không")}\n";
            surveyInfo += $"Thời gian dự kiến: 5 phút";

            MessageBox.Show(surveyInfo, "Xem trước khảo sát", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private bool _isStartingSurvey = false;

        private async void btnStartSurvey_Click(object sender, RoutedEventArgs e)
        {
            if (_isStartingSurvey) return;
            var title = txtSurveyTitle.Text.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("Vui lòng nhập tiêu đề khảo sát!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Bắt đầu khảo sát:\n\n📋 {title}\n📝 {_questionCount} câu hỏi\n⏱️ 5 phút\n\n✅ Khảo sát sẽ hiển thị trên màn hình chính của học sinh\n✅ Học sinh có thể trả lời qua thiết bị của mình\n✅ Kết quả sẽ được tổng hợp tự động", 
                                       "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                _isStartingSurvey = true;

                var options = new System.Collections.Generic.List<string> { "A. Hoàn toàn hiểu", "B. Hiểu một phần", "C. Chưa hiểu rõ", "D. Không hiểu" };
                var surveyId = Guid.NewGuid().ToString();
                var optionsJson = System.Text.Json.JsonSerializer.Serialize(options);

                try
                {
                    var app = (QASmartTouch.App)Application.Current;
                    if (app?.Database != null)
                    {
                        var newSurvey = new QASmartClass.Data.Survey
                        {
                            Id = surveyId,
                            Title = title,
                            Description = txtSurveyDescription.Text,
                            SurveyType = "custom",
                            QuestionText = "Bạn hiểu rõ nội dung bài học không?",
                            OptionsJson = optionsJson,
                            TimeLimitSeconds = 300,
                            IsAnonymous = chkAnonymous.IsChecked == true ? 1 : 0,
                            TargetClasses = "ALL",
                            CreatedByTeacher = "GV",
                            CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        };
                        app.Database.Surveys.Add(newSurvey);
                        app.Database.SaveChanges();
                    }
                }
                catch (Exception exDb) { Serilog.Log.Warning("Form4_1 DB save error: {Err}", exDb.Message); }

                var startPayload = new
                {
                    SurveyId = surveyId,
                    Title = title,
                    SurveyType = "custom",
                    QuestionText = "Bạn hiểu rõ nội dung bài học không?",
                    Options = options,
                    TimeLimitSeconds = 300,
                    IsAnonymous = chkAnonymous.IsChecked == true,
                    TargetClasses = "ALL"
                };
                var startMsg = new
                {
                    Action = "SURVEY_START",
                    Payload = startPayload
                };
                var command = System.Text.Json.JsonSerializer.Serialize(startMsg);

                try
                {
                    var app = (QASmartTouch.App)Application.Current;
                    
                    QASmartTouch.App.AssessmentState.ActiveSurveyQuestion = command;
                    QASmartTouch.App.AssessmentState.ActiveSurveyTime = DateTime.Now;
                    QASmartTouch.App.AssessmentState.SurveyAnswered = false;
                    QASmartTouch.App.AssessmentState.ActivePollId = surveyId;

                    app.Database?.EventLogs.Add(new QASmartClass.Data.EventLog 
                    { 
                        EventType = "SURVEY", 
                        Actor = "GV", 
                        Details = command, 
                        Timestamp = DateTime.Now 
                    });
                    app.Database?.SaveChanges();

                    if (app.NetworkService?.IsBroadcasting == true)
                        await app.NetworkService.SendCommandAsync(command);
                    else
                        app.RaiseLocalCommand(command);
                }
                catch (Exception exNet) { Serilog.Log.Warning("Form4_1 Broadcast error: {Err}", exNet.Message); }

                MessageBox.Show("✅ Đã bắt đầu khảo sát!\n\n👥 Học sinh đang trả lời...\n📊 Xem kết quả real-time trong tab Kết quả", 
                              "Khảo sát bắt đầu", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
        }
    }
}
