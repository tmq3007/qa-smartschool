using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QASmartClass.LearningTools.Controls;
using QASmartClass.LearningTools.Models;

namespace QASmartClass.LearningTools.Views.Math
{
    public partial class BasicMathTool : BaseToolControl
    {
        public BasicMathTool()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Quy trình" : "Guide & Process";
                if (menuTextPlay != null) menuTextPlay.Text = isVN ? "Luyện tập & Thi đấu" : "Practice & Match";
                if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Real-world Applications";

                LoadPracticalApps();
            };
        }

        private void LoadPracticalApps()
        {
            try
            {
                bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                string suffix = isVN ? "VN" : "EN";

                var items = new List<PracticalAppItem>
                {
                    new PracticalAppItem
                    {
                        Icon = "🛒",
                        Title = isVN ? "Mua Sắm Siêu Thị" : "Supermarket Shopping",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_basicmath_1_{suffix}.png",
                        Description = isVN 
                            ? "Phép cộng giúp tính tổng hóa đơn khi mua nhiều mặt hàng. Ví dụ: Mua táo 15.000đ và sữa 12.000đ hết tổng cộng: 15.000 + 12.000 = 27.000đ." 
                            : "Addition helps calculate the total bill when buying multiple items. Example: Buying apples for 15,000 VND and milk for 12,000 VND costs: 15,000 + 12,000 = 27,000 VND."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🐷",
                        Title = isVN ? "Tiết Kiệm Heo Đất" : "Piggy Bank Savings",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_basicmath_2_{suffix}.png",
                        Description = isVN 
                            ? "Tính tổng tiền tích lũy khi bỏ thêm tiền vào ống heo tiết kiệm. Ví dụ: Heo đang có 50.000đ, nuôi heo thêm 20.000đ sẽ được: 50.000 + 20.000 = 70.000đ." 
                            : "Calculate total accumulated money when putting more coins/bills into a piggy bank. Example: Having 50,000 VND, adding 20,000 VND results in: 50,000 + 20,000 = 70,000 VND."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🚗",
                        Title = isVN ? "Đếm Số Đồ Chơi" : "Counting Toys",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_basicmath_3_{suffix}.png",
                        Description = isVN 
                            ? "Phép trừ giúp biết số lượng đồ chơi còn lại sau khi cất bớt hoặc thất lạc. Ví dụ: Có 12 xe ô tô đồ chơi, cất đi 5 xe, còn lại: 12 - 5 = 7 xe." 
                            : "Subtraction helps find the number of remaining toys after putting some away or losing them. Example: Having 12 toy cars, putting away 5 cars leaves: 12 - 5 = 7 cars."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🍰",
                        Title = isVN ? "Chia Bánh Ngọt" : "Sharing Cake",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_basicmath_4_{suffix}.png",
                        Description = isVN 
                            ? "Phép trừ tính số bánh ngọt còn lại trên đĩa để mời các bạn. Ví dụ: Có 8 cái bánh ngọt, các bạn ăn 3 cái, còn lại: 8 - 3 = 5 cái bánh." 
                            : "Subtraction calculates the number of cakes left on a plate to invite friends. Example: Having 8 cakes, eating 3 leaves: 8 - 3 = 5 cakes."
                    },
                    new PracticalAppItem
                    {
                        Icon = "📏",
                        Title = isVN ? "Đo Chiều Cao" : "Measuring Height",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_basicmath_5_{suffix}.png",
                        Description = isVN 
                            ? "So sánh độ chênh lệch chiều cao giữa các bạn trong lớp. Ví dụ: Nam cao 125 cm, Hoa cao 120 cm. Nam cao hơn Hoa: 125 - 120 = 5 cm." 
                            : "Compare height difference between classmates. Example: Nam is 125 cm tall, Hoa is 120 cm tall. Nam is taller than Hoa: 125 - 120 = 5 cm."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🕒",
                        Title = isVN ? "Xem Giờ Đồng Hồ" : "Reading Clock",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_basicmath_6_{suffix}.png",
                        Description = isVN 
                            ? "Tính khoảng thời gian trôi qua giữa hai mốc giờ làm bài. Ví dụ: Bắt đầu lúc 8h, kết thúc lúc 8h30. Thời gian đã trôi qua: 30 - 0 = 30 phút." 
                            : "Calculate elapsed time between two class periods. Example: Starting at 8:00, ending at 8:30. Elapsed time is: 30 - 0 = 30 minutes."
                    }
                ,
                    new PracticalAppItem
                    {
                        Icon = "⚖️",
                        Title = isVN ? "Phân chia tài sản thừa kế" : "Inheritance Division",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_basicmath_7_{suffix}.png",
                        Description = isVN 
                            ? "Áp dụng các phép tính số học cơ bản để phân chia tài sản gia đình công bằng theo tỷ lệ quy định của pháp luật." 
                            : "Apply basic arithmetic operations to distribute estate assets fairly according to legal ratios."
                    },
                    new PracticalAppItem
                    {
                        Icon = "🏃",
                        Title = isVN ? "Tính chỉ số BMI cơ thể" : "Body Mass Index (BMI)",
                        ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_basicmath_8_{suffix}.png",
                        Description = isVN 
                            ? "Sử dụng phép chia và lũy thừa giữa cân nặng và chiều cao để đánh giá và theo dõi trạng thái sức khỏe thể chất." 
                            : "Use division and power functions of weight and height to evaluate and track physical health status."
                    }};

                practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading practical images: " + ex.Message);
            }
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            // Auto switch to Play tab if in other tabs
            if (sideMenu != null && sideMenu.SelectedIndex != 1)
            {
                sideMenu.SelectedIndex = 0;
            }

            // Parsing Range
            int rangeMax = 100;
            switch (cboRange.SelectedIndex)
            {
                case 0: rangeMax = 10; break;
                case 1: rangeMax = 20; break;
                case 2: rangeMax = 50; break;
                case 3: rangeMax = 100; break;
                case 4: rangeMax = 500; break;
                case 5: rangeMax = 1000; break;
                case 6: rangeMax = 2000; break;
            }

            // Parsing Operation
            MathPlayerZone.MathOp op = MathPlayerZone.MathOp.Add;
            if (cboOperation.SelectedIndex == 1) op = MathPlayerZone.MathOp.Sub;
            else if (cboOperation.SelectedIndex == 2) op = MathPlayerZone.MathOp.Mix;

            // Parsing Questions
            int qCount = 10;
            if (cboQuestions.SelectedIndex == 1) qCount = 20;
            else if (cboQuestions.SelectedIndex == 2) qCount = 30;
            else if (cboQuestions.SelectedIndex == 3) qCount = 0; // Infinite

            // Parsing Input Mode & Math Type to determine PlayMode
            bool isTeacherVerify = cboInputMode.SelectedIndex == 1;
            bool isComp = cboMathType.SelectedIndex == 1;

            MathPlayerZone.PlayMode playMode = MathPlayerZone.PlayMode.SelfCalc;
            if (isTeacherVerify)
                playMode = MathPlayerZone.PlayMode.TeacherVerify;
            else if (isComp)
                playMode = MathPlayerZone.PlayMode.SelfComp;

            // Parsing Players
            int numPlayers = cboPlayers.SelectedIndex + 1;

            // Update UI
            playArea.Children.Clear();
            playArea.Columns = numPlayers;

            Color[] colors = new Color[]
            {
                (Color)ColorConverter.ConvertFromString("#1976D2"), // Blue
                (Color)ColorConverter.ConvertFromString("#E65100"), // Orange
                (Color)ColorConverter.ConvertFromString("#388E3C")  // Green
            };

            for (int i = 0; i < numPlayers; i++)
            {
                var zone = new MathPlayerZone();
                playArea.Children.Add(zone);
                zone.StartGame($"Người chơi {i + 1}", colors[i % colors.Length], rangeMax, op, playMode, qCount);
            }

            // Toggle buttons and settings
            cboRange.IsEnabled = false;
            cboOperation.IsEnabled = false;
            cboMathType.IsEnabled = false;
            cboQuestions.IsEnabled = false;
            cboPlayers.IsEnabled = false;
            cboInputMode.IsEnabled = false;
            
            setupPanel.Visibility = Visibility.Collapsed;
            txtHeaderTitle.Text = "🎮 TRẬN ĐẤU ĐANG DIỄN RA";
            btnStop.Visibility = Visibility.Visible;
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            foreach (var child in playArea.Children)
            {
                if (child is MathPlayerZone zone)
                {
                    zone.ForceSaveOnStop();
                }
            }

            playArea.Children.Clear();
            
            cboRange.IsEnabled = true;
            cboOperation.IsEnabled = true;
            cboMathType.IsEnabled = true;
            cboQuestions.IsEnabled = true;
            cboPlayers.IsEnabled = true;
            cboInputMode.IsEnabled = true;

            setupPanel.Visibility = Visibility.Visible;
            txtHeaderTitle.Text = "⚙️ Cài Đặt Trò Chơi (Toán Cơ Bản)";
            btnStop.Visibility = Visibility.Collapsed;
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewPlay == null || viewPractical == null)
                return;

            viewGuide.Visibility = Visibility.Collapsed;
            viewPlay.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            

            switch (sideMenu.SelectedIndex)
            {
                case 0:
                    viewGuide.Visibility = Visibility.Visible;
                    break;
                case 1:
                    viewPlay.Visibility = Visibility.Visible;
                    break;
                case 2:
                    viewPractical.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}