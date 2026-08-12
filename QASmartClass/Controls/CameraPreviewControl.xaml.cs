using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace QASmartTouch.Controls;

/// <summary>
/// Interaction logic for CameraPreviewControl.xaml
/// </summary>
public partial class CameraPreviewControl : UserControl
{
    public CameraPreviewControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Updates the preview with a new frame
    /// </summary>
    public void UpdateFrame(BitmapSource frame)
    {
        Dispatcher.Invoke(() =>
        {
            PreviewImage.Source = frame;
            PlaceholderPanel.Visibility = Visibility.Collapsed;
        });
    }

    /// <summary>
    /// Clears the preview and shows placeholder
    /// </summary>
    public void ClearPreview()
    {
        Dispatcher.Invoke(() =>
        {
            PreviewImage.Source = null;
            PlaceholderPanel.Visibility = Visibility.Visible;
        });
    }

    /// <summary>
    /// Shows an error message in the preview area
    /// </summary>
    public void ShowError(string message)
    {
        Dispatcher.Invoke(() =>
        {
            PreviewImage.Source = null;
            PlaceholderPanel.Visibility = Visibility.Visible;
            // Could add error text to placeholder panel if needed
        });
    }
}
