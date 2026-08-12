using System.ComponentModel;

namespace QASmartTouch.Models
{
    /// <summary>
    /// Represents a row in a table
    /// </summary>
    public class TableRow : INotifyPropertyChanged
    {
        private double _height = 48; // Default height in pixels
        private double _minHeight = 40; // Minimum height

        public event PropertyChangedEventHandler? PropertyChanged;

        public double Height
        {
            get => _height;
            set
            {
                if (value < _minHeight)
                    value = _minHeight;
                    
                _height = value;
                OnPropertyChanged(nameof(Height));
            }
        }

        public double MinHeight
        {
            get => _minHeight;
            set
            {
                _minHeight = value;
                OnPropertyChanged(nameof(MinHeight));
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
