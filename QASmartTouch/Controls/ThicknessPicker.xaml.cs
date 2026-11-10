using System;
using System.Windows;
using System.Windows.Controls;

namespace QASmartTouch.Controls
{
    public partial class ThicknessPicker : UserControl
    {
        #region Events

        public event EventHandler<double>? ThicknessChanged;

        #endregion

        #region Constructor

        public ThicknessPicker()
        {
            InitializeComponent();
            this.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Properties

        public double SelectedThickness
        {
            get => ThicknessSlider?.Value ?? 2;
            set
            {
                if (ThicknessSlider != null)
                {
                    ThicknessSlider.Value = value;
                }
                if (txtThicknessValue != null)
                {
                    txtThicknessValue.Text = $"{value:F0}px";
                }
            }
        }

        #endregion

        #region Event Handlers

        private void ThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (txtThicknessValue != null)
            {
                txtThicknessValue.Text = $"{e.NewValue:F0}px";
            }
            ThicknessChanged?.Invoke(this, e.NewValue);
        }

        private void btnThickness1_Click(object sender, RoutedEventArgs e)
        {
            SelectedThickness = 1;
        }

        private void btnThickness2_Click(object sender, RoutedEventArgs e)
        {
            SelectedThickness = 2;
        }

        private void btnThickness4_Click(object sender, RoutedEventArgs e)
        {
            SelectedThickness = 4;
        }

        private void btnThickness8_Click(object sender, RoutedEventArgs e)
        {
            SelectedThickness = 8;
        }

        private void btnThickness12_Click(object sender, RoutedEventArgs e)
        {
            SelectedThickness = 12;
        }

        #endregion

        #region Public Methods

        public void ShowAt(Point position)
        {
            Canvas.SetLeft(this, position.X);
            Canvas.SetTop(this, position.Y);
            this.Visibility = Visibility.Visible;
        }

        public void Hide()
        {
            this.Visibility = Visibility.Collapsed;
        }

        #endregion
    }
}
