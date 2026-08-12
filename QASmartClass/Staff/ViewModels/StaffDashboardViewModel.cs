using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace QASmartClass.Staff.ViewModels
{
    public partial class StaffDashboardViewModel : ObservableObject, IDisposable
    {
        [ObservableProperty]
        private object _currentViewModel;

        private StaffOverviewViewModel _overviewViewModel;
        private IncidentManagementViewModel _incidentViewModel;
        private GateMonitorViewModel _gateMonitorViewModel;
        private CanteenPosViewModel _canteenPosViewModel;

        public StaffDashboardViewModel()
        {
            _overviewViewModel = new StaffOverviewViewModel();
            _incidentViewModel = new IncidentManagementViewModel();
            _gateMonitorViewModel = new GateMonitorViewModel();
            _canteenPosViewModel = new CanteenPosViewModel();

            // Default view
            CurrentViewModel = _overviewViewModel;
        }

        [RelayCommand]
        private void NavigateToOverview()
        {
            CurrentViewModel = _overviewViewModel;
        }

        [RelayCommand]
        private void NavigateToIncidents()
        {
            CurrentViewModel = _incidentViewModel;
        }
        
        [RelayCommand]
        private void NavigateToGate()
        {
            CurrentViewModel = _gateMonitorViewModel;
        }
        
        [RelayCommand]
        private void NavigateToCanteen()
        {
            CurrentViewModel = _canteenPosViewModel;
        }

        public void Dispose()
        {
            _gateMonitorViewModel?.Dispose();
            _canteenPosViewModel?.Dispose();
        }
    }
}

