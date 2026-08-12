using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QASmartClass.LearningTools.Controls;
using QASmartClass.LearningTools.Models;
using QASmartClass.LearningTools.Helpers;

namespace QASmartClass.LearningTools.Views.Multi
{
    public partial class DynasTool : BaseToolControl
    {
        private static readonly FontFamily OutfitFont = new FontFamily(new Uri("pack://application:,,,/QASmartClass;component/"), "./Resources/Fonts/#Outfit");
        private static readonly FontFamily InterFont = new FontFamily(new Uri("pack://application:,,,/QASmartClass;component/"), "./Resources/Fonts/#Inter");

        public DynasTool()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                LocalizeHeaderAndHelp();
                BuildTimeline();
                LoadPracticalApps();
            };
        }

        private void LocalizeHeaderAndHelp()
        {
            bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
            
            // Header localization
            txtTitle.Text = isVN ? "🏛️ Triều Đại Lịch Sử" : "🏛️ Historical Dynasties";
            txtSubtitle.Text = isVN ? "Lớp 9-12 • Timeline Việt Nam & Thế giới" : "Grade 9-12 • Vietnam & World Timeline";
            btnHelp.Content = isVN ? "❓ Hướng dẫn" : "❓ Guide";
            
            // Sidebar menu localization
            if (menuTextGuide != null) menuTextGuide.Text = isVN ? "Hướng dẫn & Ghi chú" : "Study Guide & Notes";
            if (menuTextTimeline != null) menuTextTimeline.Text = isVN ? "Tiến trình lịch sử" : "Historical Timeline";
            if (menuTextPractical != null) menuTextPractical.Text = isVN ? "Ứng dụng thực tế" : "Real-world Applications";

            // Guide view text localization
            if (txtHelpTitle != null) txtHelpTitle.Text = isVN ? "🏛️ HƯỚNG DẪN SỬ DỤNG TIỆN ÍCH" : "🏛️ TOOL USAGE GUIDE";
            if (txtHelpStep1 != null) txtHelpStep1.Text = isVN ? "1️⃣ Chọn mục 'Tiến trình lịch sử' trên Sidebar để xem dòng chảy thời gian của Việt Nam và Thế giới." : "1️⃣ Select the 'Historical Timeline' item on Sidebar to view the flow of Vietnam and World history.";
            if (txtHelpStep2 != null) txtHelpStep2.Text = isVN ? "2️⃣ Cuộn chuột để xem các triều đại được liệt kê theo thứ tự lịch sử." : "2️⃣ Scroll to view dynasties listed in chronological order.";
            if (txtHelpStep3 != null) txtHelpStep3.Text = isVN ? "3️⃣ Rê chuột qua các thẻ triều đại để hiển thị các công cụ hỗ trợ của Giáo viên." : "3️⃣ Hover over dynasty cards to reveal Teacher utility tools.";
            if (txtHelpStep4 != null) txtHelpStep4.Text = isVN ? "4️⃣ Chuyển sang mục 'Ứng dụng thực tế' trên Sidebar để xem các đề mục khảo cổ, gia phả và điện ảnh." : "4️⃣ Switch to the 'Real-world Applications' item on Sidebar to explore archaeology, genealogy, and cinema.";
            if (txtHelpStep5 != null) txtHelpStep5.Text = isVN ? "5️⃣ Nhấp chọn từng đề mục ở cột trái để xem mô tả chi tiết và hình ảnh minh họa cỡ lớn." : "5️⃣ Click each topic on the left sidebar to view detailed descriptions and large illustrations.";
            if (btnHelpClose != null) btnHelpClose.Content = isVN ? "Bắt đầu học ngay" : "Start learning now";

            if (sideMenu != null)
            {
                sideMenu.SelectedIndex = 0; // Default to timeline
            }
        }

        private void BuildTimeline()
        {
            if (dynastyPanel == null) return;
            dynastyPanel.Children.Clear();

            bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
            List<HistoricalDynasty> dynasties;

            try
            {
                dynasties = DbManager.GetHistoricalDynasties();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error("Failed to load dynasties from database: {Err}", ex.Message);
                dynasties = new List<HistoricalDynasty>();
            }

            var vnHeader = new TextBlock
            {
                Text = isVN ? "Triều đại Việt Nam" : "Vietnamese Dynasties",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                FontFamily = OutfitFont,
                Foreground = new SolidColorBrush(Color.FromRgb(78, 52, 46)),
                Margin = new Thickness(0, 16, 0, 12)
            };
            dynastyPanel.Children.Add(vnHeader);

            foreach (var d in dynasties)
            {
                if (d.Category.Equals("Vietnam", StringComparison.OrdinalIgnoreCase))
                {
                    string name = isVN ? d.NameVi : d.NameEn;
                    string detail = isVN ? d.DetailVi : d.DetailEn;
                    string events = isVN ? d.EventsVi : d.EventsEn;

                    var card = MakeDynastyCard(name, d.Period, d.DetailIcon, detail, events, "#4E342E");
                    string secId = d.Id.ToString();
                    dynastyPanel.Children.Add(WrapWithSectionToolbar(card, "dynasties", secId, $"{name} ({d.Period})"));
                }
            }

            var worldHeader = new TextBlock
            {
                Text = isVN ? "🌍 Triều đại & Đế chế tiêu biểu trên Thế giới" : "🌍 Major Dynasties & Empires in the World",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                FontFamily = OutfitFont,
                Foreground = new SolidColorBrush(Color.FromRgb(78, 52, 46)),
                Margin = new Thickness(0, 24, 0, 12)
            };
            dynastyPanel.Children.Add(worldHeader);

            foreach (var d in dynasties)
            {
                if (d.Category.Equals("World", StringComparison.OrdinalIgnoreCase))
                {
                    string name = isVN ? d.NameVi : d.NameEn;
                    string detail = isVN ? d.DetailVi : d.DetailEn;
                    string events = isVN ? d.EventsVi : d.EventsEn;

                    var card = MakeDynastyCard(name, d.Period, d.DetailIcon, detail, events, "#283593");
                    string secId = d.Id.ToString();
                    dynastyPanel.Children.Add(WrapWithSectionToolbar(card, "dynasties", secId, $"{name} ({d.Period})"));
                }
            }
        }

        private static Border MakeDynastyCard(string name, string period, string detailIcon, string detail, string events, string colorHex)
        {
            var color = (Color)ColorConverter.ConvertFromString(colorHex);
            var card = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(18, 14, 18, 14),
                Margin = new Thickness(0, 0, 0, 12),
                BorderBrush = new SolidColorBrush(color),
                BorderThickness = new Thickness(4, 0, 0, 0)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, MinWidth = 220 });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var leftSp = new StackPanel { Margin = new Thickness(0, 0, 16, 0) };
            leftSp.Children.Add(new TextBlock
            {
                Text = name,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                FontFamily = OutfitFont,
                Foreground = new SolidColorBrush(color),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 4)
            });
            leftSp.Children.Add(new TextBlock
            {
                Text = period,
                FontSize = 14,
                FontFamily = InterFont,
                Foreground = Brushes.Gray,
                TextWrapping = TextWrapping.Wrap
            });
            Grid.SetColumn(leftSp, 0);
            grid.Children.Add(leftSp);

            var rightSp = new StackPanel();
            rightSp.Children.Add(new TextBlock
            {
                Text = $"{detailIcon} {detail}",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                FontFamily = InterFont,
                Foreground = new SolidColorBrush(Color.FromRgb(44, 30, 27)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 6)
            });
            rightSp.Children.Add(new TextBlock
            {
                Text = $"📌 {events}",
                FontSize = 14,
                FontFamily = InterFont,
                Foreground = new SolidColorBrush(Color.FromRgb(63, 42, 37)),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 20
            });
            Grid.SetColumn(rightSp, 1);
            grid.Children.Add(rightSp);

            card.Child = grid;
            return card;
        }

        private void LoadPracticalApps()
        {
            bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
            string suffix = isVN ? "VN" : "EN";

            var items = new List<PracticalAppItem>
            {
                new PracticalAppItem
                {
                    Icon = "🏺",
                    Title = isVN ? "Khảo cổ & Bảo tồn Di sản" : "Archaeology & Heritage",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_dynasties_1_{suffix}.jpg",
                    Description = isVN 
                        ? "Xác định triều đại lịch sử giúp các nhà khảo cổ xác định niên đại hiện vật cổ, phục dựng kiến trúc di tích (như Cố đô Huế, Hoàng thành Thăng Long) chính xác theo phong cách thời đại." 
                        : "Identifying historical dynasties helps archaeologists date ancient artifacts and accurately restore heritage sites (like Hue Citadel or Thang Long Imperial Citadel) to their period styles."
                },
                new PracticalAppItem
                {
                    Icon = "📜",
                    Title = isVN ? "Nghiên cứu Gia phả" : "Genealogy Research",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_dynasties_2_{suffix}.jpg",
                    Description = isVN 
                        ? "Thông tin triều đại và thời kỳ biến thiên lịch sử được ứng dụng để tra cứu gốc gác gia đình, lập cây gia phả và liên kết nguồn gốc dòng họ qua các biến cố di cư lịch sử." 
                        : "Dynasty and era timelines are applied to trace family roots, construct family trees, and link ancestral origins across historical migration events."
                },
                new PracticalAppItem
                {
                    Icon = "🎬",
                    Title = isVN ? "Điện ảnh & Biên kịch" : "Historical Screenplays",
                    ImagePath = $"pack://application:,,,/QASmartClass;component/Assets/Images/app_dynasties_3_{suffix}.jpg",
                    Description = isVN 
                        ? "Biên kịch và nhà sản xuất phim sử dụng niên biểu triều đại để xây dựng kịch bản, thiết kế trang phục, bối cảnh và đạo cụ chân thực nhất với văn hóa xã hội của thời kỳ đó." 
                        : "Screenwriters and filmmakers use dynasty chronologies to write historically accurate plots, design authentic costumes, sets, and props that reflect the culture of the era."
                }
            };

            try
            {
                if (practicalAppViewer != null) practicalAppViewer.SetItemsSource(items);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error loading practical apps for DynasTool: {Err}", ex.Message);
            }
        }

        private void SideMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sideMenu == null || viewGuide == null || viewPractice == null || viewPractical == null)
                return;

            viewGuide.Visibility = Visibility.Collapsed;
            viewPractice.Visibility = Visibility.Collapsed;
            viewPractical.Visibility = Visibility.Collapsed;

            

            switch (sideMenu.SelectedIndex)
            {
                case 0:
                    viewGuide.Visibility = Visibility.Visible;
                    break;
                case 1:
                    viewPractice.Visibility = Visibility.Visible;
                    break;
                case 2:
                    viewPractical.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void BtnShowHelp_Click(object sender, RoutedEventArgs e)
        {
            if (sideMenu != null) sideMenu.SelectedIndex = 1;
        }

        private void BtnCloseHelp_Click(object sender, RoutedEventArgs e)
        {
            if (sideMenu != null) sideMenu.SelectedIndex = 1;
        }
    }
}