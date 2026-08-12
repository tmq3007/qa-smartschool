using System.Windows;

namespace SmartLibrary.Desktop.Views.Shared
{
    public partial class RankUpCelebrationDialog : Window
    {
        public RankUpCelebrationDialog(string oldLevel, string newLevel)
        {
            InitializeComponent();
            TxtOldLevel.Text = oldLevel.Replace("Cấp 1: ", "").Replace("Cấp 2: ", "").Replace("Cấp 3: ", "").Replace("Cấp 4: ", "");
            TxtNewLevel.Text = newLevel.Replace("Cấp 1: ", "").Replace("Cấp 2: ", "").Replace("Cấp 3: ", "").Replace("Cấp 4: ", "");

            Loaded += (s, e) =>
            {
                System.Media.SystemSounds.Exclamation.Play();
            };
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
