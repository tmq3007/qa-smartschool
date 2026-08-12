using System;
using System.IO;
using System.Text.Json;

namespace QASmartClass.LearningTools.Helpers
{
    public class GameSettings
    {
        public int IqLevel { get; set; } = 0;
        public int MentalMathLevel { get; set; } = 0;
        public int MemoryTheme { get; set; } = 0;
        public int SudokuSize { get; set; } = 9;
        public bool IsSoundEnabled { get; set; } = true;
    }

    public static class GameSettingsManager
    {
        private static readonly string Path = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "QASmartClass", "game_settings.json");

        private static readonly object LockObj = new object();
        private static GameSettings? _cachedSettings;

        public static GameSettings Load()
        {
            lock (LockObj)
            {
                if (_cachedSettings != null) return _cachedSettings;
                try
                {
                    if (File.Exists(Path))
                    {
                        string json = File.ReadAllText(Path);
                        _cachedSettings = JsonSerializer.Deserialize<GameSettings>(json) ?? new GameSettings();
                        return _cachedSettings;
                    }
                }
                catch
                {
                    // Tránh lỗi khi đọc file cấu hình hỏng
                }
                _cachedSettings = new GameSettings();
                return _cachedSettings;
            }
        }

        public static void Save(GameSettings settings)
        {
            lock (LockObj)
            {
                _cachedSettings = settings;
                try
                {
                    string dir = System.IO.Path.GetDirectoryName(Path);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    string json = JsonSerializer.Serialize(settings);
                    File.WriteAllText(Path, json);
                }
                catch
                {
                    // Tránh lỗi khi ghi file cấu hình không thành công
                }
            }
        }
    }
}
