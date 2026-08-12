using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;

namespace SmartLibrary.Desktop.Views.Shared
{
    public partial class StatCardControl : UserControl
    {
        public static readonly DependencyProperty IconProperty = DependencyProperty.Register("Icon", typeof(string), typeof(StatCardControl), new PropertyMetadata(""));
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(string), typeof(StatCardControl), new PropertyMetadata("0"));
        public static readonly DependencyProperty LabelProperty = DependencyProperty.Register("Label", typeof(string), typeof(StatCardControl), new PropertyMetadata(""));
        public static readonly DependencyProperty ChangeTextProperty = DependencyProperty.Register("ChangeText", typeof(string), typeof(StatCardControl), new PropertyMetadata(""));
        public static readonly DependencyProperty ClickCommandProperty = DependencyProperty.Register("ClickCommand", typeof(System.Windows.Input.ICommand), typeof(StatCardControl), new PropertyMetadata(null));
        public static readonly DependencyProperty ClickCommandParameterProperty = DependencyProperty.Register("ClickCommandParameter", typeof(object), typeof(StatCardControl), new PropertyMetadata(null));
        public static readonly DependencyProperty ChangeBrushProperty = DependencyProperty.Register(
            "ChangeBrush", 
            typeof(Brush), 
            typeof(StatCardControl), 
            new PropertyMetadata(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16A34A"))));

        public string Icon
        {
            get { return (string)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        public string Value
        {
            get { return (string)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        public string ChangeText
        {
            get { return (string)GetValue(ChangeTextProperty); }
            set { SetValue(ChangeTextProperty, value); }
        }

        public ICommand ClickCommand
        {
            get { return (ICommand)GetValue(ClickCommandProperty); }
            set { SetValue(ClickCommandProperty, value); }
        }

        public object ClickCommandParameter
        {
            get { return GetValue(ClickCommandParameterProperty); }
            set { SetValue(ClickCommandParameterProperty, value); }
        }

        public Brush ChangeBrush
        {
            get { return (Brush)GetValue(ChangeBrushProperty); }
            set { SetValue(ChangeBrushProperty, value); }
        }

        public StatCardControl()
        {
            InitializeComponent();
        }
    }
}
