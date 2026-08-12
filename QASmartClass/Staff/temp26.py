import re

# 1. Update XAML
with open('D:/JOB/QA SmartSchool/QA SmartClass_Document/QASmartClass_Dev/QASmartClass/Forms/Form2_28_WorldClock.xaml', 'r', encoding='utf-8') as f:
    xaml = f.read()

# Remove the WebView xmlns if present
xaml = re.sub(r'xmlns:wv2="[^"]+"', '', xaml)

# Replace the WebView container with an ItemsControl
new_ui = '''
        <!-- Native World Clock Container -->
        <Border Grid.Row="1" Background="#F5F6FA" Margin="10">
            <ScrollViewer VerticalScrollBarVisibility="Auto">
                <ItemsControl x:Name="ClockItemsControl" Margin="10">
                    <ItemsControl.ItemTemplate>
                        <DataTemplate>
                            <Border Background="White" CornerRadius="10" Margin="0,0,0,15" Padding="20">
                                <Border.Effect>
                                    <DropShadowEffect Color="#DDDDDD" BlurRadius="10" ShadowDepth="2" Opacity="0.5"/>
                                </Border.Effect>
                                <Grid>
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition Width="*"/>
                                        <ColumnDefinition Width="Auto"/>
                                    </Grid.ColumnDefinitions>
                                    
                                    <StackPanel Grid.Column="0" VerticalAlignment="Center">
                                        <TextBlock Text="{Binding LocationName}" FontSize="20" FontWeight="Bold" Foreground="#2F3542"/>
                                        <TextBlock Text="{Binding TimeZoneName}" FontSize="13" Foreground="#747D8C" Margin="0,5,0,0"/>
                                        <TextBlock Text="{Binding OffsetInfo}" FontSize="12" Foreground="#A4B0BE" Margin="0,2,0,0"/>
                                    </StackPanel>

                                    <StackPanel Grid.Column="1" VerticalAlignment="Center" HorizontalAlignment="Right">
                                        <TextBlock Text="{Binding CurrentTime}" FontSize="32" FontWeight="Bold" Foreground="#3498DB" HorizontalAlignment="Right"/>
                                        <TextBlock Text="{Binding CurrentDate}" FontSize="14" Foreground="#747D8C" HorizontalAlignment="Right" Margin="0,5,0,0"/>
                                    </StackPanel>
                                </Grid>
                            </Border>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
            </ScrollViewer>
        </Border>
'''

# Find the Grid.Row="1" border and replace its contents
xaml = re.sub(r'<Border Grid\.Row="1".*?<!-- Footer Actions -->', new_ui + '\n        <!-- Footer Actions -->', xaml, flags=re.DOTALL)

with open('D:/JOB/QA SmartSchool/QA SmartClass_Document/QASmartClass_Dev/QASmartClass/Forms/Form2_28_WorldClock.xaml', 'w', encoding='utf-8') as f:
    f.write(xaml)

# 2. Update C# Code-behind
cs_code = '''using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace QASmartTouch.Forms
{
    public class WorldClockItem : System.ComponentModel.INotifyPropertyChanged
    {
        public string LocationName { get; set; }
        public string TimeZoneId { get; set; }
        
        private string _currentTime;
        public string CurrentTime 
        { 
            get => _currentTime; 
            set { _currentTime = value; OnPropertyChanged(nameof(CurrentTime)); } 
        }

        private string _currentDate;
        public string CurrentDate 
        { 
            get => _currentDate; 
            set { _currentDate = value; OnPropertyChanged(nameof(CurrentDate)); } 
        }

        private string _timeZoneName;
        public string TimeZoneName 
        { 
            get => _timeZoneName; 
            set { _timeZoneName = value; OnPropertyChanged(nameof(TimeZoneName)); } 
        }

        private string _offsetInfo;
        public string OffsetInfo 
        { 
            get => _offsetInfo; 
            set { _offsetInfo = value; OnPropertyChanged(nameof(OffsetInfo)); } 
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string prop) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(prop));
    }

    public partial class Form2_28_WorldClock : Window
    {
        public ObservableCollection<WorldClockItem> Clocks { get; set; }
        private DispatcherTimer _timer;

        public Form2_28_WorldClock()
        {
            InitializeComponent();
            
            Clocks = new ObservableCollection<WorldClockItem>();
            LoadStandardTimeZones();
            
            ClockItemsControl.ItemsSource = Clocks;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;
            _timer.Start();
            
            UpdateClocks();
        }

        private void LoadStandardTimeZones()
        {
            // List of critical timezones with proper IANA/Windows timezone mapping
            var locations = new List<(string Name, string TzId)>
            {
                ("Hà Nội, Việt Nam", "SE Asia Standard Time"),
                ("New York, Hoa Kỳ", "Eastern Standard Time"),
                ("London, Anh", "GMT Standard Time"),
                ("Tokyo, Nhật Bản", "Tokyo Standard Time"),
                ("Sydney, Úc", "AUS Eastern Standard Time"),
                ("Paris, Pháp", "Romance Standard Time"),
                ("Dubai, UAE", "Arabian Standard Time"),
                ("Los Angeles, Hoa Kỳ", "Pacific Standard Time")
            };

            foreach (var loc in locations)
            {
                Clocks.Add(new WorldClockItem { LocationName = loc.Name, TimeZoneId = loc.TzId });
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateClocks();
        }

        private void UpdateClocks()
        {
            DateTime utcNow = DateTime.UtcNow;
            DateTime localNow = DateTime.Now;

            foreach (var clock in Clocks)
            {
                try
                {
                    TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(clock.TimeZoneId);
                    DateTime targetTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);
                    
                    clock.CurrentTime = targetTime.ToString("HH:mm:ss");
                    clock.CurrentDate = targetTime.ToString("dddd, dd/MM/yyyy", new System.Globalization.CultureInfo("vi-VN"));
                    
                    bool isDst = tz.IsDaylightSavingTime(targetTime);
                    string dstStatus = isDst ? " (Có DST)" : "";
                    clock.TimeZoneName = $"{tz.DisplayName}{dstStatus}";

                    TimeSpan diff = tz.GetUtcOffset(targetTime) - TimeZoneInfo.Local.GetUtcOffset(localNow);
                    if (diff.TotalHours == 0)
                        clock.OffsetInfo = "Cùng giờ địa phương";
                    else if (diff.TotalHours > 0)
                        clock.OffsetInfo = $"Đi trước {diff.TotalHours} giờ";
                    else
                        clock.OffsetInfo = $"Chậm hơn {Math.Abs(diff.TotalHours)} giờ";
                }
                catch (TimeZoneNotFoundException)
                {
                    clock.CurrentTime = "--:--:--";
                    clock.CurrentDate = "Không tìm thấy múi giờ";
                    clock.TimeZoneName = clock.TimeZoneId;
                }
            }
        }

        private void btnDone_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
'''

with open('D:/JOB/QA SmartSchool/QA SmartClass_Document/QASmartClass_Dev/QASmartClass/Forms/Form2_28_WorldClock.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(cs_code)
