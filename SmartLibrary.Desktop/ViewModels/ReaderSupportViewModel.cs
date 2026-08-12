using CommunityToolkit.Mvvm.ComponentModel;

namespace SmartLibrary.Desktop.ViewModels
{
    public partial class ReaderSupportViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _hotlineText = "1900 6068 (Nhánh 2) hoặc 024.7300.6068";

        [ObservableProperty]
        private string _emailText = "support.lib@qasmartschool.edu.vn";

        [ObservableProperty]
        private string _officeLocation = "Tòa nhà Tri Thức (Nhà A), Tầng 2, Phòng 202";

        [ObservableProperty]
        private string _morningHours = "Sáng: 7:30 - 11:30";

        [ObservableProperty]
        private string _afternoonHours = "Chiều: 13:30 - 17:30 (Thứ Hai - Thứ Sáu)";
    }
}
