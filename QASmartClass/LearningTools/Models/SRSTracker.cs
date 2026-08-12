using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace QASmartClass.LearningTools.Models
{
    public class SRSItem
    {
        public DateTime LearnedDate { get; set; }
        public DateTime NextReviewDate { get; set; }
        public int Interval { get; set; } // In days
        public float EaseFactor { get; set; } = 2.5f;
        public int Repetitions { get; set; } = 0;
    }

    public static class SRSTracker
    {
        private static string GetUserDataFilePath()
        {
            var userCode = global::QASmartClass.Services.UserSessionService.Instance.TeacherCode;
            if (string.IsNullOrEmpty(userCode))
            {
                return Path.Combine(global::QASmartClass.Services.AppPaths.RootDir, "vocab_history_anonymous.json");
            }
            // Sanitize userCode for safe file name
            var safeCode = string.Concat(userCode.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(global::QASmartClass.Services.AppPaths.RootDir, $"vocab_history_{safeCode}.json");
        }

        private static VocabData _data = new();
        private static bool _loaded;
        private static string _currentLoadedPath = string.Empty;

        public static void EnsureLoaded()
        {
            string targetPath = GetUserDataFilePath();
            if (_loaded && _currentLoadedPath == targetPath) return;

            _loaded = true;
            _currentLoadedPath = targetPath;
            try
            {
                _data = new VocabData(); // Reset data for the new user session
                if (File.Exists(targetPath))
                {
                    var json = File.ReadAllText(targetPath);
                    using var doc = JsonDocument.Parse(json);

                    if (doc.RootElement.TryGetProperty("LearnedWords", out var learnedWordsProp))
                    {
                        foreach (var prop in learnedWordsProp.EnumerateObject())
                        {
                            if (prop.Value.ValueKind == JsonValueKind.String)
                            {
                                // Legacy format (Phase 1)
                                if (DateTime.TryParse(prop.Value.GetString(), out DateTime dt))
                                {
                                    _data.LearnedWords[prop.Name] = new SRSItem
                                    {
                                        LearnedDate = dt,
                                        NextReviewDate = dt.AddDays(1), // Assume needs review
                                        Interval = 1,
                                        EaseFactor = 2.5f,
                                        Repetitions = 1
                                    };
                                }
                            }
                            else if (prop.Value.ValueKind == JsonValueKind.Object)
                            {
                                // New format (Phase 6)
                                var item = JsonSerializer.Deserialize<SRSItem>(prop.Value.GetRawText());
                                if (item != null)
                                    _data.LearnedWords[prop.Name] = item;
                            }
                        }
                    }
                    
                    if (doc.RootElement.TryGetProperty("CustomList", out var customListProp))
                    {
                        foreach (var item in customListProp.EnumerateArray())
                        {
                            _data.CustomList.Add(item.GetString() ?? "");
                        }
                    }

                    Save(); // Save migrated data immediately
                }
            }
            catch
            {
                _data = new VocabData();
            }
        }

        private static void Save()
        {
            try
            {
                string targetPath = GetUserDataFilePath();
                var dir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(targetPath, json);
            }
            catch { /* fail silently */ }
        }

        // Quality: 0 (Blackout/Hard), 3 (Normal), 5 (Perfect/Easy)
        public static void ReviewWord(string wordEn, int quality)
        {
            EnsureLoaded();
            if (!_data.LearnedWords.TryGetValue(wordEn, out var item))
            {
                // New word learned
                item = new SRSItem { LearnedDate = DateTime.Now };
                _data.LearnedWords[wordEn] = item;
            }

            // SM-2 Algorithm implementation
            if (quality < 3)
            {
                item.Repetitions = 0;
                item.Interval = 1;
            }
            else
            {
                if (item.Repetitions == 0)
                    item.Interval = 1;
                else if (item.Repetitions == 1)
                    item.Interval = 6;
                else
                    item.Interval = (int)Math.Round(item.Interval * item.EaseFactor);

                item.Repetitions++;
            }

            item.EaseFactor = item.EaseFactor + (0.1f - (5 - quality) * (0.08f + (5 - quality) * 0.02f));
            if (item.EaseFactor < 1.3f) item.EaseFactor = 1.3f;

            item.NextReviewDate = DateTime.Today.AddDays(item.Interval);
            Save();
        }

        public static bool MarkLearned(string wordEn)
        {
            EnsureLoaded();
            if (!_data.LearnedWords.ContainsKey(wordEn))
            {
                _data.LearnedWords[wordEn] = new SRSItem 
                { 
                    LearnedDate = DateTime.Now,
                    NextReviewDate = DateTime.Today.AddDays(1) 
                };
                Save();
                return true;
            }
            return false;
        }

        public static bool UnmarkLearned(string wordEn)
        {
            EnsureLoaded();
            if (_data.LearnedWords.ContainsKey(wordEn))
            {
                _data.LearnedWords.Remove(wordEn);
                Save();
                return true;
            }
            return false;
        }

        public static bool IsLearned(string wordEn)
        {
            EnsureLoaded();
            return _data.LearnedWords.ContainsKey(wordEn);
        }

        public static int GetTotalLearned()
        {
            EnsureLoaded();
            return _data.LearnedWords.Count;
        }

        public static int GetLearnedToday()
        {
            EnsureLoaded();
            var today = DateTime.Today;
            return _data.LearnedWords.Values.Count(i => i.LearnedDate.Date == today);
        }

        public static List<string> GetDueToday()
        {
            EnsureLoaded();
            var today = DateTime.Today;
            return _data.LearnedWords
                .Where(kvp => kvp.Value.NextReviewDate.Date <= today)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        public static List<string> GetRecentLearned(int count = 20)
        {
            EnsureLoaded();
            return _data.LearnedWords
                .OrderByDescending(kvp => kvp.Value.LearnedDate)
                .Take(count)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        // --- Custom List Features (Phase 8) ---
        public static bool ToggleCustomList(string wordEn)
        {
            EnsureLoaded();
            bool added = false;
            if (_data.CustomList.Contains(wordEn))
            {
                _data.CustomList.Remove(wordEn);
            }
            else
            {
                _data.CustomList.Add(wordEn);
                added = true;
            }
            Save();
            return added;
        }

        public static bool IsInCustomList(string wordEn)
        {
            EnsureLoaded();
            return _data.CustomList.Contains(wordEn);
        }

        public static List<string> GetCustomList()
        {
            EnsureLoaded();
            return _data.CustomList.ToList();
        }

        private static HashSet<string> _validVocabularyWords = null;

        public static void SetValidWords(IEnumerable<string> words)
        {
            if (words != null)
            {
                _validVocabularyWords = new HashSet<string>(words, StringComparer.OrdinalIgnoreCase);
            }
        }

        private static void LoadValidWordsFromDataFile()
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var dataPath = Path.Combine(baseDir, "Assets", "Data", "Language", "VocabularyData.json");
                if (!File.Exists(dataPath))
                {
                    var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    var dir = Path.GetDirectoryName(exePath);
                    if (!string.IsNullOrEmpty(dir))
                    {
                        dataPath = Path.Combine(dir, "Assets", "Data", "Language", "VocabularyData.json");
                    }
                }

                if (File.Exists(dataPath))
                {
                    var json = File.ReadAllText(dataPath);
                    using var doc = JsonDocument.Parse(json);
                    var words = new List<string>();
                    foreach (var cat in doc.RootElement.EnumerateArray())
                    {
                        if (cat.TryGetProperty("Words", out var wordsProp))
                        {
                            foreach (var w in wordsProp.EnumerateArray())
                            {
                                if (w.TryGetProperty("En", out var enProp))
                                {
                                    words.Add(enProp.GetString() ?? "");
                                }
                            }
                        }
                    }
                    _validVocabularyWords = new HashSet<string>(words, StringComparer.OrdinalIgnoreCase);
                }
            }
            catch { }
        }

        public static string ExportCustomList()
        {
            EnsureLoaded();
            var json = JsonSerializer.Serialize(_data.CustomList);
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
        }

        public static bool ImportCustomList(string base64)
        {
            try
            {
                EnsureLoaded();
                var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64));
                var list = JsonSerializer.Deserialize<List<string>>(json);
                if (list != null)
                {
                    if (_validVocabularyWords == null)
                    {
                        LoadValidWordsFromDataFile();
                    }

                    bool importedAny = false;
                    foreach (var w in list)
                    {
                        if (string.IsNullOrWhiteSpace(w)) continue;
                        if (_validVocabularyWords == null || _validVocabularyWords.Contains(w.Trim()))
                        {
                            _data.CustomList.Add(w.Trim());
                            importedAny = true;
                        }
                    }
                    if (importedAny)
                    {
                        Save();
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        private class VocabData
        {
            public Dictionary<string, SRSItem> LearnedWords { get; set; } = new(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> CustomList { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        }
    }
}
