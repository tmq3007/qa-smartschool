using System;
using System.Linq;
using System.Speech.Synthesis;
using System.Threading.Tasks;

namespace SmartLibrary.Desktop.Services
{
    public static class SpeechService
    {
        private static readonly SpeechSynthesizer _synth = new();
        private static bool _hasVietnameseVoice = false;

        static SpeechService()
        {
            try
            {
                var voices = _synth.GetInstalledVoices();
                var viVoice = voices.FirstOrDefault(v => v.VoiceInfo.Culture.Name.StartsWith("vi", StringComparison.OrdinalIgnoreCase));
                if (viVoice != null)
                {
                    _synth.SelectVoice(viVoice.VoiceInfo.Name);
                    _hasVietnameseVoice = true;
                }
            }
            catch { }
        }

        public static void Speak(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            
            if (!_hasVietnameseVoice)
            {
                // Nếu không có giọng đọc tiếng Việt -> phát âm báo Beep nhẹ để thu hút sự chú ý
                // Tránh phát âm tiếng Việt bằng âm điệu tiếng Anh phản cảm, gây mất nghiêm túc học đường
                Task.Run(() => {
                    try { System.Console.Beep(800, 250); } catch {}
                });
                return;
            }

            Task.Run(() =>
            {
                lock (_synth)
                {
                    try
                    {
                        _synth.Speak(text);
                    }
                    catch { }
                }
            });
        }

        public static void Stop()
        {
            try
            {
                _synth.SpeakAsyncCancelAll();
            }
            catch { }
        }
    }
}
