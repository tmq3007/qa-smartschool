using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using QASmartClass.Data;

namespace QASmartClass.Leadership.Views
{
    public partial class StaffPerformanceTrackerView : Page
    {
        public StaffPerformanceTrackerView()
        {
            InitializeComponent();
            LoadPerformance();
        }

        private void LoadPerformance()
        {
            var kpis = QASmartClass.Services.TeacherKpiService.CalculateAllKpis();
            dgPerformance.ItemsSource = kpis;
        }
    }
}

