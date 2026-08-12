import sys
import re

with open('D:/JOB/QA SmartSchool/QA SmartClass_Document/QASmartClass_Dev/QASmartClass/LearningTools/Views/Multi/FocusTimerTool.xaml.cs', 'r', encoding='utf-8') as f:
    c = f.read()

if 'BroadcastCommand' not in c:
    broadcast_method = '''
        private void BroadcastCommand(string cmd)
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                var net = app.NetworkService;
                if (net != null && net.IsBroadcasting)
                    _ = net.SendCommandAsync(cmd);
                else
                    app.RaiseLocalCommand(cmd);
            }
            catch { }
        }
        
        #endregion'''
        
    c = c.replace('#endregion', broadcast_method, 1)

    c = c.replace('_timer.Start();\n            UpdateUI();', 
                  '_timer.Start();\n            UpdateUI();\n            BroadcastCommand($"CMD|TIMER_START|{_timeLeft}");')

    c = c.replace('_timer.Stop();\n            UpdateUI();', 
                  '_timer.Stop();\n            UpdateUI();\n            BroadcastCommand("CMD|TIMER_PAUSE");')

    c = c.replace('_timer.Stop();\n            _timeLeft = GetTotalSeconds(_mode);', 
                  '_timer.Stop();\n            _timeLeft = GetTotalSeconds(_mode);\n            BroadcastCommand("CMD|TIMER_STOP");')

    c = c.replace('if (_timeLeft % 60 == 0) UpdateProgress();', 
                  'if (_timeLeft % 60 == 0) UpdateProgress();\n                if (_timeLeft % 10 == 0) BroadcastCommand($"CMD|TIMER_START|{_timeLeft}");')
                  
    c = c.replace('if (_isAutoStartEnabled)\n                    StartTimer();', 
                  'BroadcastCommand("CMD|TIMER_STOP");\n                if (_isAutoStartEnabled)\n                    StartTimer();')

    with open('D:/JOB/QA SmartSchool/QA SmartClass_Document/QASmartClass_Dev/QASmartClass/LearningTools/Views/Multi/FocusTimerTool.xaml.cs', 'w', encoding='utf-8') as f:
        f.write(c)
