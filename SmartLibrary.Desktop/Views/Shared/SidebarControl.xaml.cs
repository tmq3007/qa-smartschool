using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SmartLibrary.Desktop.Views.Shared
{
    public partial class SidebarControl : UserControl
    {
        public static readonly DependencyProperty UserNameProperty = DependencyProperty.Register("UserName", typeof(string), typeof(SidebarControl), new PropertyMetadata(""));
        public static readonly DependencyProperty UserRoleProperty = DependencyProperty.Register("UserRole", typeof(string), typeof(SidebarControl), new PropertyMetadata(""));
        public static readonly DependencyProperty MenuItemsProperty = DependencyProperty.Register("MenuItems", typeof(ObservableCollection<MenuItemViewModel>), typeof(SidebarControl), new PropertyMetadata(null));
        public static readonly DependencyProperty MenuClickCommandProperty = DependencyProperty.Register("MenuClickCommand", typeof(ICommand), typeof(SidebarControl), new PropertyMetadata(null));

        public string UserName
        {
            get { return (string)GetValue(UserNameProperty); }
            set { SetValue(UserNameProperty, value); }
        }

        public string UserRole
        {
            get { return (string)GetValue(UserRoleProperty); }
            set { SetValue(UserRoleProperty, value); }
        }

        public ObservableCollection<MenuItemViewModel> MenuItems
        {
            get { return (ObservableCollection<MenuItemViewModel>)GetValue(MenuItemsProperty); }
            set { SetValue(MenuItemsProperty, value); }
        }

        public ICommand MenuClickCommand
        {
            get { return (ICommand)GetValue(MenuClickCommandProperty); }
            set { SetValue(MenuClickCommandProperty, value); }
        }

        public SidebarControl()
        {
            InitializeComponent();
        }
    }

    public class MenuItemViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        public string Text { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool IsTitle { get; set; }
        
        private bool _isActive;
        public bool IsActive 
        { 
            get => _isActive; 
            set => SetProperty(ref _isActive, value); 
        }

        private bool _isExpanded = true;
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        private string _badgeText = string.Empty;
        public string BadgeText
        {
            get => _badgeText;
            set => SetProperty(ref _badgeText, value);
        }

        public ObservableCollection<MenuItemViewModel> SubItems { get; } = new();

        public bool HasSubItems => SubItems.Count > 0;
    }
}
