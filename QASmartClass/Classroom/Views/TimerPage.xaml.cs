using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Serilog;

namespace QASmartClass.Classroom.Views
{
    public partial class TimerPage : Page
    {
        public TimerPage()
        {
            InitializeComponent();
            DataContext = new ViewModels.TimerViewModel();
        }
    }
}
