using System;
using System.IO;

namespace QASmartClass.LearningTools.Helpers
{
    public static class PathHelper
    {
        /// <summary>
        /// Prevents Path Traversal vulnerability by validating that the targetPath resides within the basePath.
        /// </summary>
        public static bool IsPathSafe(string basePath, string targetPath)
        {
            try
            {
                if (string.IsNullOrEmpty(basePath) || string.IsNullOrEmpty(targetPath))
                    return false;

                string fullBasePath = Path.GetFullPath(basePath);
                string fullTargetPath = Path.GetFullPath(targetPath);

                if (!fullBasePath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                {
                    fullBasePath += Path.DirectorySeparatorChar;
                }

                return fullTargetPath.StartsWith(fullBasePath, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Sanitizes text input to prevent HTML/Script injection and remove control characters.
        /// </summary>
        public static string SanitizeInput(string input, int maxLength = 200)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            if (input.Length > maxLength) input = input.Substring(0, maxLength);
            // Remove HTML/XML tags
            input = System.Text.RegularExpressions.Regex.Replace(input, @"<[^>]*>", string.Empty);
            // Filter control characters (ASCII < 32 except Tab, LF, CR)
            var sb = new System.Text.StringBuilder();
            foreach (char c in input)
            {
                if (c < 32 && c != '\t' && c != '\n' && c != '\r') continue;
                sb.Append(c);
            }
            return sb.ToString().Trim();
        }

        /// <summary>
        /// Sanitizes input to be safe for filenames/paths, allowing only letters, digits, hyphen, and underscore.
        /// </summary>
        public static string SanitizeFileNameInput(string input, int maxLength = 20)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            string cleaned = System.Text.RegularExpressions.Regex.Replace(input, @"[^a-zA-Z0-9_-]", string.Empty);
            if (cleaned.Length > maxLength) cleaned = cleaned.Substring(0, maxLength);
            return cleaned.Trim();
        }
    }
}
