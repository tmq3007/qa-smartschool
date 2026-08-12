using System.ComponentModel;

namespace QASmartTouch.Models
{
    /// <summary>
    /// Represents a column in a table
    /// </summary>
    public class TableColumn : INotifyPropertyChanged
    {
        private double _width = 120; // Default width in pixels
        private double _minWidth = 80; // Minimum width

        public event PropertyChangedEventHandler? PropertyChanged;

        public double Width
        {
            get => _width;
            set
            {
                if (value < _minWidth)
                    value = _minWidth;
                    
                _width = value;
                OnPropertyChanged(nameof(Width));
            }
        }

        public double MinWidth
        {
            get => _minWidth;
            set
            {
                _minWidth = value;
                OnPropertyChanged(nameof(MinWidth));
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
