using System.ComponentModel;

namespace QASmartTouch.Models
{
    /// <summary>
    /// Represents a single cell in a table
    /// </summary>
    public class TableCell : INotifyPropertyChanged
    {
        private string _content = "";
        private int _rowSpan = 1;
        private int _columnSpan = 1;
        private string _backgroundColor = "#FFFFFF";
        private string _foregroundColor = "#2F3542";
        private string _fontFamily = "Segoe UI";
        private double _fontSize = 16;
        private bool _isBold = false;
        private bool _isItalic = false;
        private bool _isUnderline = false;
        private string _horizontalAlignment = "Left"; // Left, Center, Right
        private string _verticalAlignment = "Center"; // Top, Center, Bottom

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Content
        {
            get => _content;
            set
            {
                _content = value;
                OnPropertyChanged(nameof(Content));
            }
        }

        public int RowSpan
        {
            get => _rowSpan;
            set
            {
                _rowSpan = value;
                OnPropertyChanged(nameof(RowSpan));
            }
        }

        public int ColumnSpan
        {
            get => _columnSpan;
            set
            {
                _columnSpan = value;
                OnPropertyChanged(nameof(ColumnSpan));
            }
        }

        public string BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                _backgroundColor = value;
                OnPropertyChanged(nameof(BackgroundColor));
            }
        }

        public string ForegroundColor
        {
            get => _foregroundColor;
            set
            {
                _foregroundColor = value;
                OnPropertyChanged(nameof(ForegroundColor));
            }
        }

        public string FontFamily
        {
            get => _fontFamily;
            set
            {
                _fontFamily = value;
                OnPropertyChanged(nameof(FontFamily));
            }
        }

        public double FontSize
        {
            get => _fontSize;
            set
            {
                _fontSize = value;
                OnPropertyChanged(nameof(FontSize));
            }
        }

        public bool IsBold
        {
            get => _isBold;
            set
            {
                _isBold = value;
                OnPropertyChanged(nameof(IsBold));
            }
        }

        public bool IsItalic
        {
            get => _isItalic;
            set
            {
                _isItalic = value;
                OnPropertyChanged(nameof(IsItalic));
            }
        }

        public bool IsUnderline
        {
            get => _isUnderline;
            set
            {
                _isUnderline = value;
                OnPropertyChanged(nameof(IsUnderline));
            }
        }

        public string HorizontalAlignment
        {
            get => _horizontalAlignment;
            set
            {
                _horizontalAlignment = value;
                OnPropertyChanged(nameof(HorizontalAlignment));
            }
        }

        public string VerticalAlignment
        {
            get => _verticalAlignment;
            set
            {
                _verticalAlignment = value;
                OnPropertyChanged(nameof(VerticalAlignment));
            }
        }

        public bool IsMerged { get; set; } = false;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public TableCell Clone()
        {
            return new TableCell
            {
                Content = this.Content,
                RowSpan = this.RowSpan,
                ColumnSpan = this.ColumnSpan,
                BackgroundColor = this.BackgroundColor,
                ForegroundColor = this.ForegroundColor,
                FontFamily = this.FontFamily,
                FontSize = this.FontSize,
                IsBold = this.IsBold,
                IsItalic = this.IsItalic,
                IsUnderline = this.IsUnderline,
                HorizontalAlignment = this.HorizontalAlignment,
                VerticalAlignment = this.VerticalAlignment,
                IsMerged = this.IsMerged
            };
        }
    }
}
