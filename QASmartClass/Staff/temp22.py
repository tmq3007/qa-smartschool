import sys
import re

with open('D:/JOB/QA SmartSchool/QA SmartClass_Document/QASmartClass_Dev/QASmartClass/Forms/Form2_19_CountdownTimer.xaml.cs', 'r', encoding='utf-8') as f:
    c = f.read()

if 'BroadcastCommand' not in c:
    # Add BroadcastCommand method
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

    # In btnStart_Click
    c = c.replace('_timer.Start();\n            UpdateButtonStates();', 
                  '_timer.Start();\n            UpdateButtonStates();\n            BroadcastCommand($"CMD|TIMER_START|{(int)_remainingTime.TotalSeconds}");')

    # In btnPause_Click
    c = c.replace('_timer.Stop();\n            UpdateButtonStates();', 
                  '_timer.Stop();\n            UpdateButtonStates();\n            BroadcastCommand("CMD|TIMER_PAUSE");')

    # In btnReset_Click
    c = c.replace('_timer.Stop();\n            _state = TimerState.Stopped;', 
                  '_timer.Stop();\n            _state = TimerState.Stopped;\n            BroadcastCommand("CMD|TIMER_STOP");')

    # In Timer_Tick
    c = c.replace('UpdateDisplay();\n                UpdateProgressRing();', 
                  'UpdateDisplay();\n                UpdateProgressRing();\n                if ((int)_remainingTime.TotalSeconds % 10 == 0) BroadcastCommand($"CMD|TIMER_START|{(int)_remainingTime.TotalSeconds}");')
                  
    c = c.replace('_timer.Stop();\n                _state = TimerState.Stopped;\n                \n                UpdateButtonStates();', 
                  '_timer.Stop();\n                _state = TimerState.Stopped;\n                \n                UpdateButtonStates();\n                BroadcastCommand("CMD|TIMER_STOP");')

    with open('D:/JOB/QA SmartSchool/QA SmartClass_Document/QASmartClass_Dev/QASmartClass/Forms/Form2_19_CountdownTimer.xaml.cs', 'w', encoding='utf-8') as f:
        f.write(c)

with open('D:/JOB/QA SmartSchool/QA SmartClass_Document/QASmartClass_Dev/QASmartClass/Classroom/ViewModels/TimerViewModel.cs', 'r', encoding='utf-8') as f:
    c = f.read()

if 'BroadcastCommand' not in c:
    broadcast_method = '''
        private void BroadcastCommand(string cmd)
        {
            try
            {
                var app = (QASmartTouch.App)Application.Current;
                app.RaiseLocalCommand(cmd);
            }
            catch { }
        }
'''
    c = c.replace('public TimerViewModel()\n        {', broadcast_method + '\n        public TimerViewModel()\n        {')
    
    # StartTimer
    c = c.replace('_countdownTimer.Start();\n                StartTimerButtonText = "⏸️ Tạm dừng";\n                Log.Information("Countdown started: {Sec}s", _secondsLeft);',
                  '_countdownTimer.Start();\n                StartTimerButtonText = "⏸️ Tạm dừng";\n                Log.Information("Countdown started: {Sec}s", _secondsLeft);\n                BroadcastCommand($"CMD|TIMER_START|{_secondsLeft}");')

    c = c.replace('_countdownTimer.Stop();\n                StartTimerButtonText = "▶️ Tiếp tục";\n                Log.Information("Countdown paused at {Sec}s", _secondsLeft);',
                  '_countdownTimer.Stop();\n                StartTimerButtonText = "▶️ Tiếp tục";\n                Log.Information("Countdown paused at {Sec}s", _secondsLeft);\n                BroadcastCommand("CMD|TIMER_PAUSE");')

    c = c.replace('StartTimerButtonText = "▶️ Bắt đầu";\n            Log.Information("Countdown reset");',
                  'StartTimerButtonText = "▶️ Bắt đầu";\n            Log.Information("Countdown reset");\n            BroadcastCommand("CMD|TIMER_STOP");')

    c = c.replace('UpdateCountdownDisplay();\n            }\n            else',
                  'UpdateCountdownDisplay();\n                if (_secondsLeft % 10 == 0) BroadcastCommand($"CMD|TIMER_START|{_secondsLeft}");\n            }\n            else')

    c = c.replace('MessageBox.Show("⏰ Hết giờ!", "Đếm ngược", MessageBoxButton.OK, MessageBoxImage.Information);\n                Log.Information("Countdown finished");',
                  'MessageBox.Show("⏰ Hết giờ!", "Đếm ngược", MessageBoxButton.OK, MessageBoxImage.Information);\n                Log.Information("Countdown finished");\n                BroadcastCommand("CMD|TIMER_STOP");')

    with open('D:/JOB/QA SmartSchool/QA SmartClass_Document/QASmartClass_Dev/QASmartClass/Classroom/ViewModels/TimerViewModel.cs', 'w', encoding='utf-8') as f:
        f.write(c)
