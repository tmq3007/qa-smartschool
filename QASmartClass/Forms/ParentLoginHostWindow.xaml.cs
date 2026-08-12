using System;
using System.Windows;

namespace QASmartTouch.Forms
{
    public partial class ParentLoginHostWindow : Window
    {
        public ParentLoginHostWindow()
        {
            InitializeComponent();
            
            var page = new QASmartClass.ParentPortal.Views.ParentLoginPage();
            page.LoginSuccess += (student) =>
            {
                var db = QASmartClass.Services.AppServices.Database ?? new QASmartClass.Data.AppDbContext();
                var shell = new QASmartClass.ParentPortal.ParentShell(db, student);
                shell.Show();
                this.DialogResult = true;
                this.Close();
            };
            mainFrame.Navigate(page);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
