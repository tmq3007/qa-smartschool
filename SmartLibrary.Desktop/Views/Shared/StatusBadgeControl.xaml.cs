using System.Windows;
using System.Windows.Controls;

namespace SmartLibrary.Desktop.Views.Shared
{
    public partial class StatusBadgeControl : UserControl
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(StatusBadgeControl), new PropertyMetadata(""));
        public static readonly DependencyProperty BadgeTypeProperty = DependencyProperty.Register("BadgeType", typeof(string), typeof(StatusBadgeControl), new PropertyMetadata("Success", OnBadgeTypeChanged));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public string BadgeType
        {
            get { return (string)GetValue(BadgeTypeProperty); }
            set { SetValue(BadgeTypeProperty, value); }
        }

        public StatusBadgeControl()
        {
            InitializeComponent();
        }

        private static void OnBadgeTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (StatusBadgeControl)d;
            var type = (string)e.NewValue;

            if (type == "Success")
            {
                control.BadgeBorder.Style = (Style)Application.Current.Resources["SuccessBadge"];
                control.BadgeText.Style = (Style)Application.Current.Resources["SuccessBadgeText"];
            }
            else if (type == "Warning")
            {
                control.BadgeBorder.Style = (Style)Application.Current.Resources["WarningBadge"];
                control.BadgeText.Style = (Style)Application.Current.Resources["WarningBadgeText"];
            }
            else if (type == "Danger")
            {
                control.BadgeBorder.Style = (Style)Application.Current.Resources["DangerBadge"];
                control.BadgeText.Style = (Style)Application.Current.Resources["DangerBadgeText"];
            }
        }
    }
}
