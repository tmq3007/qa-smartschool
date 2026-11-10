using System;
using System.Speech.Synthesis;

namespace QASmartTouch.Services
{
    /// <summary>
    /// Service quản lý phát âm âm thanh Text-to-Speech (TTS) ngoại tuyến của Windows
    /// Hỗ trợ tự động chuyển đổi giọng đọc tiếng Anh / tiếng Việt và điều chỉnh tốc độ đọc.
    /// </summary>
    public class SpeechSynthesizerService
    {
        private SpeechSynthesizer? _synthesizer;

        public SpeechSynthesizerService()
        {
            InitializeSynthesizer();
        }

        private void InitializeSynthesizer()
        {
            try
            {
                _synthesizer = new SpeechSynthesizer();
                _synthesizer.Volume = 100;
                _synthesizer.Rate = 0; // Tốc độ chuẩn 1.0x
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Failed to init SpeechSynthesizer: {ex.Message}");
            }
        }

        public void SpeakText(string text, int speedRate = 0)
        {
            if (_synthesizer == null) InitializeSynthesizer();
            if (_synthesizer == null || string.IsNullOrWhiteSpace(text)) return;

            try
            {
                _synthesizer.SpeakAsyncCancelAll();
                _synthesizer.Rate = speedRate; // -2 (0.75x), 0 (1.0x), 2 (1.25x)

                // Tự động nhận diện giọng đọc Tiếng Việt hoặc Tiếng Anh
                bool isVietnamese = System.Text.RegularExpressions.Regex.IsMatch(
                    text, @"[àáảãạăằắẳẵặâầấẩẫậèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵđ]",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                SelectOptimalVoice(isVietnamese);

                _synthesizer.SpeakAsync(text);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ TTS Speak Error: {ex.Message}");
            }
        }

        public void Stop()
        {
            try
            {
                _synthesizer?.SpeakAsyncCancelAll();
            }
            catch { }
        }

        private void SelectOptimalVoice(bool isVietnamese)
        {
            if (_synthesizer == null) return;
            try
            {
                var installedVoices = _synthesizer.GetInstalledVoices();
                foreach (var voice in installedVoices)
                {
                    var info = voice.VoiceInfo;
                    if (isVietnamese && (info.Culture.Name.StartsWith("vi") || info.Name.Contains("An")))
                    {
                        _synthesizer.SelectVoice(info.Name);
                        return;
                    }
                    else if (!isVietnamese && (info.Culture.Name.StartsWith("en") || info.Name.Contains("Zira") || info.Name.Contains("David")))
                    {
                        _synthesizer.SelectVoice(info.Name);
                        return;
                    }
                }
            }
            catch { }
        }
    }
}
