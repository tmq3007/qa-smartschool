using System;
using System.Speech.Synthesis;
using System.Threading.Tasks;

namespace QASmartClass.LearningTools.Helpers
{
    public static class TtsHelper
    {
        public static void SpeakEnglish(string text)
        {
            Task.Run(() =>
            {
                try
                {
                    using (var synth = new SpeechSynthesizer())
                    {
                        // Try to select an English voice
                        foreach (var voice in synth.GetInstalledVoices())
                        {
                            var info = voice.VoiceInfo;
                            if (info.Culture.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                            {
                                synth.SelectVoice(info.Name);
                                break;
                            }
                        }

                        // Speak text
                        synth.Speak(text);
                    }
                }
                catch
                {
                    // Tránh crash nếu thiết bị không có loa hoặc driver hỏng
                }
            });
        }
    }
}
