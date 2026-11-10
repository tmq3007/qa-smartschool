using System;
using System.Windows;
using Serilog;

namespace QASmartClass.Shared
{
    /// <summary>
    /// Manages bilingual UI (Vietnamese / English).
    /// Swaps a merged ResourceDictionary at runtime so every
    /// {DynamicResource key} in XAML updates instantly.
    /// Default language: Vietnamese ("vi").
    /// </summary>
    public static class LanguageManager
    {
        private static string _currentLang = 
            System.Globalization.CultureInfo.CurrentUICulture.Name.StartsWith("vi", StringComparison.OrdinalIgnoreCase) ||
            System.Globalization.CultureInfo.CurrentCulture.Name.StartsWith("vi", StringComparison.OrdinalIgnoreCase)
            ? "vi" : "en";

        public static event Action<string>? LanguageChanged;

        /// <summary>Current language code: "vi" or "en"</summary>
        public static string CurrentLanguage => _currentLang;

        /// <summary>
        /// Apply a language at startup or when user switches.
        /// </summary>
        public static void SetLanguage(string langCode)
        {
            langCode = langCode?.ToLowerInvariant() ?? "vi";
            if (langCode != "vi" && langCode != "en") langCode = "vi";

            _currentLang = langCode;
            try
            {
                LanguageChanged?.Invoke(langCode);

                var app = Application.Current;
                if (app != null)
                {
                    // Build the URI of the resource dictionary
                    var uri = new Uri($"pack://application:,,,/Localization/Strings_{langCode}.xaml");
                    var dict = new ResourceDictionary { Source = uri };

                    // Remove any previous language dictionary (tagged)
                    ResourceDictionary? toRemove = null;
                    foreach (var d in app.Resources.MergedDictionaries)
                    {
                        if (d.Source != null && d.Source.OriginalString.Contains("Localization/Strings_"))
                        {
                            toRemove = d;
                            break;
                        }
                    }
                    if (toRemove != null) app.Resources.MergedDictionaries.Remove(toRemove);

                    // Add the new one
                    app.Resources.MergedDictionaries.Add(dict);

                    Log.Information("Language changed to: {Lang} ({Count} keys)",
                        langCode, dict.Count);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("LanguageManager.SetLanguage error: {Err}", ex.Message);
            }
        }

        /// <summary>
        /// Get a localized string from Application resources by key.
        /// Falls back to the key itself if not found.
        /// </summary>
        public static string Get(string key)
        {
            try
            {
                var val = Application.Current.TryFindResource(key);
                return val as string ?? key;
            }
            catch { return key; }
        }
    }
}
