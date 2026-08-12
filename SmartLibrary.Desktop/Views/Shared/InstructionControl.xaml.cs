using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SmartLibrary.Desktop.Views.Shared
{
    public partial class InstructionControl : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(InstructionControl), 
                new PropertyMetadata("📖 Hướng dẫn sử dụng", OnTitleChanged));

        public static readonly DependencyProperty InnerContentProperty =
            DependencyProperty.Register(nameof(InnerContent), typeof(object), typeof(InstructionControl), 
                new PropertyMetadata(null, OnInnerContentChanged));

        public static readonly DependencyProperty CardBackgroundProperty =
            DependencyProperty.Register(nameof(CardBackground), typeof(Brush), typeof(InstructionControl),
                new PropertyMetadata(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0FDF4"))));

        public static readonly DependencyProperty CardBorderBrushProperty =
            DependencyProperty.Register(nameof(CardBorderBrush), typeof(Brush), typeof(InstructionControl),
                new PropertyMetadata(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCFCE7"))));

        public static readonly DependencyProperty TitleForegroundProperty =
            DependencyProperty.Register(nameof(TitleForeground), typeof(Brush), typeof(InstructionControl),
                new PropertyMetadata(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15803D"))));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public object InnerContent
        {
            get => GetValue(InnerContentProperty);
            set => SetValue(InnerContentProperty, value);
        }

        public Brush CardBackground
        {
            get => (Brush)GetValue(CardBackgroundProperty);
            set => SetValue(CardBackgroundProperty, value);
        }

        public Brush CardBorderBrush
        {
            get => (Brush)GetValue(CardBorderBrushProperty);
            set => SetValue(CardBorderBrushProperty, value);
        }

        public Brush TitleForeground
        {
            get => (Brush)GetValue(TitleForegroundProperty);
            set => SetValue(TitleForegroundProperty, value);
        }

        public InstructionControl()
        {
            InitializeComponent();
        }

        private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is InstructionControl control) control.TxtTitle.Text = e.NewValue?.ToString();
        }

        private static void OnInnerContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is InstructionControl control) control.ContentArea.Content = e.NewValue;
        }

        private void Header_Click(object sender, MouseButtonEventArgs e)
        {
            if (ContentArea.Visibility == Visibility.Visible)
            {
                ContentArea.Visibility = Visibility.Collapsed;
                TxtToggleIcon.Text = "▼";
            }
            else
            {
                ContentArea.Visibility = Visibility.Visible;
                TxtToggleIcon.Text = "▲";
            }
        }
    }
}
