using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace QASmartTouch.PeriodicTable.Views
{
    public partial class IntroductionWindow : Window
    {
        public IntroductionWindow()
        {
            InitializeComponent();
            InitPracticalImages();
        }

        private void InitPracticalImages()
        {
            try
            {
                string suffix = "VN";
                try
                {
                    string lang = QASmartClass.Shared.LanguageManager.CurrentLanguage;
                    if (!string.IsNullOrEmpty(lang))
                    {
                        suffix = lang.Equals("vi", StringComparison.OrdinalIgnoreCase) ? "VN" : "EN";
                    }
                    else
                    {
                        bool isVN = QASmartClass.Shared.LanguageManager.CurrentLanguage == "vi";
                        suffix = isVN ? "VN" : "EN";
                    }
                }
                catch
                {
                    suffix = "VN";
                }

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string path1 = Path.Combine(baseDir, "Assets", "Images", $"app_periodictable_1_{suffix}.png");
                string path2 = Path.Combine(baseDir, "Assets", "Images", $"app_periodictable_2_{suffix}.png");
                string path3 = Path.Combine(baseDir, "Assets", "Images", $"app_periodictable_3_{suffix}.png");
                string path4 = Path.Combine(baseDir, "Assets", "Images", $"app_periodictable_4_{suffix}.png");
                string path5 = Path.Combine(baseDir, "Assets", "Images", $"app_periodictable_5_{suffix}.png");
                string path6 = Path.Combine(baseDir, "Assets", "Images", $"app_periodictable_6_{suffix}.png");

                var imgApp1 = FindName("imgApp1") as System.Windows.Controls.Image;
                var imgApp2 = FindName("imgApp2") as System.Windows.Controls.Image;
                var imgApp3 = FindName("imgApp3") as System.Windows.Controls.Image;
                var imgApp4 = FindName("imgApp4") as System.Windows.Controls.Image;
                var imgApp5 = FindName("imgApp5") as System.Windows.Controls.Image;
                var imgApp6 = FindName("imgApp6") as System.Windows.Controls.Image;

                if (imgApp1 != null && File.Exists(path1))
                    imgApp1.Source = new BitmapImage(new Uri(path1));
                if (imgApp2 != null && File.Exists(path2))
                    imgApp2.Source = new BitmapImage(new Uri(path2));
                if (imgApp3 != null && File.Exists(path3))
                    imgApp3.Source = new BitmapImage(new Uri(path3));
                if (imgApp4 != null && File.Exists(path4))
                    imgApp4.Source = new BitmapImage(new Uri(path4));
                if (imgApp5 != null && File.Exists(path5))
                    imgApp5.Source = new BitmapImage(new Uri(path5));
                if (imgApp6 != null && File.Exists(path6))
                    imgApp6.Source = new BitmapImage(new Uri(path6));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading periodic table practical images: " + ex.Message);
            }
        }
    }
}

