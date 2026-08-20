using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QASmartTouch.Forms
{
    public partial class Form2_2_SubMenuEraser : Window
    {
        // Current eraser settings
        private string currentEraserMode = "Stroke";
        private int currentEraserSize = 20;

        public bool IsApplied { get; private set; } = true;

        private bool _isClosing = false; // Guard against re-entrant Close()
        
        public Form2_2_SubMenuEraser()
        {
            InitializeComponent();
            // QC_4.2_TOUCH_PIPELINE: Popup Window — WPF tự cô lập, KHÔNG cần ApplyTouchIsolation
            if (btnClose != null)
            {
                btnClose.PreviewTouchDown += (s, e) => { this.Close(); e.Handled = true; };
                btnClose.PreviewStylusDown += (s, e) => { this.Close(); e.Handled = true; };
            }
            
            // Sync selected mode UI on load
            this.Loaded += (s, e) =>
            {
                UpdateEraserModeUI();
            };
            
            // Set closing flag to prevent re-entrant Close() from any source
            this.Closing += (s, e) =>
            {
                _isClosing = true;
            };
            
            // NOTE: Deactivated handler REMOVED — incompatible with touch input.
            // On interactive whiteboards, touching ANY button/slider inside the SubMenu
            // causes WPF to fire Deactivated (temporary focus loss from touch events),
            // which would prematurely close the window or cause InvalidOperationException.
            // Instead, closing is handled by:
            //   1. ❌ close button (btnClose_Click)
            //   2. MainDashboard.CloseAllSubmenus() when user clicks canvas or switches tools
        }

        /// <summary>
        /// Highlight selected eraser mode using Border highlight instead of solid background overlay.
        /// This ensures visual contrast and prevents the icon container background from merging.
        /// </summary>
        private void HighlightMode(Button selectedButton)
        {
            if (btnEraseByStroke == null || btnEraseByPoint == null || btnClearAll == null) return;

            // Reset backgrounds to default soft pastel colors
            btnEraseByStroke.Background = new SolidColorBrush(Color.FromRgb(227, 242, 253)); // #E3F2FD
            btnEraseByPoint.Background = new SolidColorBrush(Color.FromRgb(243, 229, 245));   // #F3E5F5
            btnClearAll.Background = new SolidColorBrush(Color.FromRgb(255, 235, 238));      // #FFEBEE

            // Reset borders (remove selection outline)
            btnEraseByStroke.BorderThickness = new Thickness(0);
            btnEraseByPoint.BorderThickness = new Thickness(0);
            btnClearAll.BorderThickness = new Thickness(0);

            // Set uniform dark slate foreground for high contrast text readability
            var textDarkSlate = new SolidColorBrush(Color.FromRgb(47, 53, 66)); // #2F3542
            btnEraseByStroke.Foreground = textDarkSlate;
            btnEraseByPoint.Foreground = textDarkSlate;
            btnClearAll.Foreground = textDarkSlate;

            // Highlight selected button with a distinct thick border of its brand color
            if (selectedButton == btnEraseByStroke)
            {
                selectedButton.BorderBrush = new SolidColorBrush(Color.FromRgb(33, 150, 243)); // #2196F3
                selectedButton.BorderThickness = new Thickness(2);
            }
            else if (selectedButton == btnEraseByPoint)
            {
                selectedButton.BorderBrush = new SolidColorBrush(Color.FromRgb(156, 39, 176)); // #9C27B0
                selectedButton.BorderThickness = new Thickness(2);
            }
            else if (selectedButton == btnClearAll)
            {
                selectedButton.BorderBrush = new SolidColorBrush(Color.FromRgb(238, 90, 111)); // #EE5A6F
                selectedButton.BorderThickness = new Thickness(2);
            }
        }

        public event EventHandler? SettingsChanged;

        #region Event Handlers

        private void btnEraseMode_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag != null)
            {
                currentEraserMode = button.Tag.ToString();
                HighlightMode(button);
                
                IsApplied = true;

                // ✅ QC_4.2_SMART_TOUCH_SUBMENU_SYNC: Thông báo cài đặt thay đổi NGAY LẬP TỨC tới MainDashboard
                SettingsChanged?.Invoke(this, EventArgs.Empty);

                // ✅ QC_4.2_SMART_TOUCH_UX: Tự động đóng SubMenu sau khi chọn chế độ xóa để giải phóng toàn bộ mặt bảng cho GV thao tác
                if (!_isClosing)
                {
                    _isClosing = true;
                    this.Close();
                }
            }
        }

        private void sliderSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void btnClearAll_Click(object sender, RoutedEventArgs e)
        {
            if (_isClosing) return; // ✅ Guard: Deactivated may have already started closing
            _isClosing = true;
            // Clear All is an immediate action, close and apply instantly
            currentEraserMode = "ClearAll";
            HighlightMode(btnClearAll);
            IsApplied = true;
            this.Close();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            if (_isClosing) return; // ✅ Guard: Deactivated may have already started closing
            _isClosing = true;
            IsApplied = true;
            this.Close();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Get or set current eraser mode
        /// </summary>
        public string EraserMode 
        { 
            get => currentEraserMode; 
            set 
            { 
                currentEraserMode = value; 
                // Update UI if already loaded
                if (IsLoaded) UpdateEraserModeUI();
            } 
        }

        /// <summary>
        /// Get or set current eraser size linked directly to slider control value
        /// </summary>
        public int EraserSize 
        { 
            get => (int)sliderSize.Value; 
            set 
            { 
                sliderSize.Value = Math.Clamp(value, (int)sliderSize.Minimum, (int)sliderSize.Maximum);
            } 
        }

        // Helper method for UI updates
        private void UpdateEraserModeUI()
        {
            Button? targetButton = currentEraserMode switch
            {
                "Stroke" => btnEraseByStroke,
                "Point" => btnEraseByPoint,
                "ClearAll" => btnClearAll,
                _ => btnEraseByStroke
            };
            if (targetButton != null)
                HighlightMode(targetButton);
        }

        #endregion
    }
}
