import sys
import re

with open('D:/JOB/QA SmartSchool/QA SmartClass_Document/QASmartClass_Dev/QASmartClass/StudentClient/Views/StudentShell.xaml.cs', 'r', encoding='utf-8') as f:
    c = f.read()

# Add fields
if '_overlayTimer' not in c:
    c = c.replace('private readonly DispatcherTimer _clockTimer;', 'private readonly DispatcherTimer _clockTimer;\n        private System.Windows.Threading.DispatcherTimer? _overlayTimer;\n        private int _overlaySecondsLeft;')

# Add TIMER switch cases
timer_cases = '''
                case "TIMER_START":
                    if (parts.Length > 2 && int.TryParse(parts[2], out int sec))
                    {
                        _overlaySecondsLeft = sec;
                        if (_overlayTimer == null)
                        {
                            _overlayTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                            _overlayTimer.Tick += (s, e) =>
                            {
                                if (_overlaySecondsLeft > 0)
                                {
                                    _overlaySecondsLeft--;
                                    txtTimerDisplay.Text = $"{_overlaySecondsLeft / 60:D2}:{_overlaySecondsLeft % 60:D2}";
                                }
                                else
                                {
                                    _overlayTimer.Stop();
                                    timerOverlay.Visibility = Visibility.Collapsed;
                                }
                            };
                        }
                        txtTimerDisplay.Text = $"{_overlaySecondsLeft / 60:D2}:{_overlaySecondsLeft % 60:D2}";
                        timerOverlay.Visibility = Visibility.Visible;
                        _overlayTimer.Start();
                    }
                    break;
                case "TIMER_PAUSE":
                    if (_overlayTimer != null) _overlayTimer.Stop();
                    break;
                case "TIMER_STOP":
                    if (_overlayTimer != null) _overlayTimer.Stop();
                    timerOverlay.Visibility = Visibility.Collapsed;
                    break;
'''

c = c.replace('switch (parts[1])\n            {', 'switch (parts[1])\n            {' + timer_cases)

with open('D:/JOB/QA SmartSchool/QA SmartClass_Document/QASmartClass_Dev/QASmartClass/StudentClient/Views/StudentShell.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(c)
