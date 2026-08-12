using System;
using System.Media;
using System.Threading.Tasks;

namespace QASmartClass.LearningTools.Helpers
{
    public static class SoundHelper
    {
        public static void Play(bool isCorrect)
        {
            Task.Run(() =>
            {
                try
                {
                    var settings = GameSettingsManager.Load();
                    if (!settings.IsSoundEnabled) return;

                    if (isCorrect)
                    {
                        SystemSounds.Asterisk.Play();
                    }
                    else
                    {
                        SystemSounds.Hand.Play();
                    }
                }
                catch (Exception)
                {
                    // Tránh crash ứng dụng nếu thiết bị không có loa hoặc driver hỏng
                }
            });
        }

        public static void PlayClick()
        {
            Task.Run(() =>
            {
                try
                {
                    var settings = GameSettingsManager.Load();
                    if (!settings.IsSoundEnabled) return;

                    SystemSounds.Beep.Play();
                }
                catch (Exception)
                {
                    // Tránh crash ứng dụng nếu thiết bị không có loa hoặc driver hỏng
                }
            });
        }
    }
}
