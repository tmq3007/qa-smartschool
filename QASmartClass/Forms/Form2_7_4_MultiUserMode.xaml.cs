using System;
using System.Windows;
using System.Windows.Media;

namespace QASmartTouch.Forms
{
    public partial class Form2_7_4_MultiUserMode : Window
    {
        public bool IsMultiUserEnabled { get; private set; }
        public SplitMode SelectedSplitMode { get; private set; }
        public StudentProfile Student1 { get; private set; }
        public StudentProfile Student2 { get; private set; }
        public bool ZoneIsolationEnabled { get; private set; }
        public bool ShowSplitLine { get; private set; }

        public Form2_7_4_MultiUserMode()
        {
            InitializeComponent();
            
            // Initialize default profiles
            Student1 = new StudentProfile
            {
                Name = "Học sinh A",
                Color = Colors.Blue,
                Zone = SplitZone.Left
            };

            Student2 = new StudentProfile
            {
                Name = "Học sinh B",
                Color = Colors.Red,
                Zone = SplitZone.Right
            };

            // Enable multi-user mode by default
            chkEnableMultiUser.IsChecked = true;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            if (!chkEnableMultiUser.IsChecked.GetValueOrDefault())
            {
                IsMultiUserEnabled = false;
                DialogResult = true;
                Close();
                return;
            }

            // Get split mode
            SelectedSplitMode = rbVertical.IsChecked.GetValueOrDefault() 
                ? SplitMode.Vertical 
                : SplitMode.Horizontal;

            // Get advanced options
            ZoneIsolationEnabled = chkZoneIsolation.IsChecked.GetValueOrDefault();
            ShowSplitLine = chkShowSplitLine.IsChecked.GetValueOrDefault();

            // Update student 1
            Student1.Name = txtStudent1Name.Text;
            Student1.Color = GetColorFromComboBox(cmbStudent1Color.SelectedIndex, true);
            Student1.Zone = SelectedSplitMode == SplitMode.Vertical ? SplitZone.Left : SplitZone.Top;

            // Update student 2
            Student2.Name = txtStudent2Name.Text;
            Student2.Color = GetColorFromComboBox(cmbStudent2Color.SelectedIndex, false);
            Student2.Zone = SelectedSplitMode == SplitMode.Vertical ? SplitZone.Right : SplitZone.Bottom;

            IsMultiUserEnabled = true;
            DialogResult = true;
            Close();
        }

        private void chkEnableMultiUser_Checked(object sender, RoutedEventArgs e)
        {
            panelSplitMode.IsEnabled = true;
            panelAdvancedOptions.IsEnabled = true;
            panelStudent1.IsEnabled = true;
            panelStudent2.IsEnabled = true;
        }

        private void chkEnableMultiUser_Unchecked(object sender, RoutedEventArgs e)
        {
            panelSplitMode.IsEnabled = false;
            panelAdvancedOptions.IsEnabled = false;
            panelStudent1.IsEnabled = false;
            panelStudent2.IsEnabled = false;
        }

        private Color GetColorFromComboBox(int index, bool isStudent1)
        {
            if (isStudent1)
            {
                return index switch
                {
                    0 => Colors.Blue,      // 🔵 Xanh dương
                    1 => Colors.Green,     // 🟢 Xanh lá
                    2 => Colors.Purple,    // 🟣 Tím
                    3 => Colors.Gold,      // 🟡 Vàng
                    _ => Colors.Blue
                };
            }
            else
            {
                return index switch
                {
                    0 => Colors.Red,       // 🔴 Đỏ
                    1 => Colors.Orange,    // 🟠 Cam
                    2 => Colors.Brown,     // 🟤 Nâu
                    3 => Colors.Black,     // ⚫ Đen
                    _ => Colors.Red
                };
            }
        }
    }

    /// <summary>
    /// Split screen mode
    /// </summary>
    public enum SplitMode
    {
        Vertical,   // Left/Right
        Horizontal  // Top/Bottom
    }

    /// <summary>
    /// Zone for each student
    /// </summary>
    public enum SplitZone
    {
        Left,
        Right,
        Top,
        Bottom,
        Full  // No split
    }

    /// <summary>
    /// Student profile
    /// </summary>
    public class StudentProfile
    {
        public string Name { get; set; } = "";
        public Color Color { get; set; }
        public SplitZone Zone { get; set; }
        public int StrokeCount { get; set; }
        public TimeSpan ElapsedTime { get; set; }

        public SolidColorBrush GetBrush()
        {
            return new SolidColorBrush(Color);
        }
    }
}
