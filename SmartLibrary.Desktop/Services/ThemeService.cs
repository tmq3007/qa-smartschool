using System;
using System.Linq;
using System.Windows;

namespace SmartLibrary.Desktop.Services
{
    public static class ThemeService
    {
        public static bool IsDarkTheme { get; private set; } = false;

        public static void SetTheme(bool isDark)
        {
            IsDarkTheme = isDark;
            var dicts = Application.Current.Resources.MergedDictionaries;
            
            var existingColorDict = dicts.FirstOrDefault(d => d.Source != null && 
                (d.Source.OriginalString.Contains("Colors.xaml") || 
                 d.Source.OriginalString.Contains("ColorsLight.xaml") || 
                 d.Source.OriginalString.Contains("ColorsDark.xaml")));
                 
            if (existingColorDict != null)
            {
                dicts.Remove(existingColorDict);
            }

            var newDict = new ResourceDictionary();
            if (isDark)
            {
                newDict.Source = new Uri("Styles/ColorsDark.xaml", UriKind.Relative);
            }
            else
            {
                newDict.Source = new Uri("Styles/ColorsLight.xaml", UriKind.Relative);
            }

            dicts.Insert(0, newDict);
        }

        public static void ToggleTheme()
        {
            SetTheme(!IsDarkTheme);
        }
    }
}
