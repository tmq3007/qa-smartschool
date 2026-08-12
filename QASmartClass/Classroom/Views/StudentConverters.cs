using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace QASmartClass.Classroom.Views
{
    public class StudentNameToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string name = value as string ?? "";
            if (string.IsNullOrWhiteSpace(name))
            {
                return new SolidColorBrush(Colors.SlateGray);
            }
            byte r = (byte)(Math.Abs(name.GetHashCode()) % 100 + 100);
            byte g = (byte)(Math.Abs(name.GetHashCode() >> 8) % 100 + 120);
            byte b = (byte)(Math.Abs(name.GetHashCode() >> 16) % 100 + 140);
            return new SolidColorBrush(Color.FromRgb(r, g, b));
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StudentNameToInitialsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string name = value as string ?? "";
            if (string.IsNullOrWhiteSpace(name))
            {
                return "?";
            }
            var parts = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return "?";
            }
            string lastPart = parts[^1];
            return lastPart.Length > 0 ? lastPart[..1].ToUpper() : "?";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class RosterEditorWindow : Window
    {
        public RosterEditorWindow(QASmartClass.Data.ClassRoster? roster)
        {
            FontSize = 13;
            var txtTitle = new TextBlock
            {
                Name = "txtTitle",
                FontSize = 18
            };
            RegisterName("txtTitle", txtTitle);
        }
    }
}
