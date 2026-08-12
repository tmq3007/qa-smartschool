using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Media;
using Newtonsoft.Json;
using QASmartTouch.PeriodicTable.Models;

namespace QASmartTouch.PeriodicTable.ViewModels
{
    public class ElementDetailViewModel : INotifyPropertyChanged
    {
        private ElementDetail _currentElement;
        private List<ElementDetail> _allElements;
        private ElementDataManager _dataManager;

        public event PropertyChangedEventHandler PropertyChanged;

        public ElementDetail CurrentElement
        {
            get => _currentElement;
            set
            {
                _currentElement = value;
                OnPropertyChanged(nameof(CurrentElement));
                OnPropertyChanged(nameof(CategoryColor));
            }
        }

        public Brush CategoryColor
        {
            get
            {
                if (CurrentElement == null) return new SolidColorBrush(Color.FromRgb(74, 144, 226));

                // Map category to color
                switch (CurrentElement.Category)
                {
                    case "Kim loại kiềm":
                        return new SolidColorBrush(Color.FromRgb(255, 99, 132));
                    case "Kim loại kiềm thổ":
                        return new SolidColorBrush(Color.FromRgb(255, 159, 64));
                    case "Kim loại chuyển tiếp":
                        return new SolidColorBrush(Color.FromRgb(255, 205, 86));
                    case "Kim loại sau chuyển tiếp":
                        return new SolidColorBrush(Color.FromRgb(75, 192, 192));
                    case "Á kim":
                        return new SolidColorBrush(Color.FromRgb(54, 162, 235));
                    case "Phi kim":
                        return new SolidColorBrush(Color.FromRgb(74, 144, 226));
                    case "Halogen":
                        return new SolidColorBrush(Color.FromRgb(153, 102, 255));
                    case "Khí hiếm":
                        return new SolidColorBrush(Color.FromRgb(201, 203, 207));
                    case "Lanthanide":
                    case "Nhóm Lantan":
                        return new SolidColorBrush(Color.FromRgb(255, 99, 255));
                    case "Actinide":
                    case "Nhóm Actini":
                        return new SolidColorBrush(Color.FromRgb(199, 99, 132));
                    case "Khí trơ":
                        return new SolidColorBrush(Color.FromRgb(201, 203, 207));
                    default:
                        return new SolidColorBrush(Color.FromRgb(74, 144, 226));
                }
            }
        }

        public ElementDetailViewModel()
        {
            _dataManager = ElementDataManager.Instance;
            LoadAllElements();
        }

        private void LoadAllElements()
        {
            try
            {
                _allElements = _dataManager.LoadAllElements();

                if (_allElements == null || _allElements.Count == 0)
                {
                    throw new Exception("Không có dữ liệu nguyên tố");
                }
            }
            catch (Exception ex)
            {
                // Lấy thông tin lỗi chi tiết
                string errorDetails = ex.Message;
                if (ex.InnerException != null)
                {
                    errorDetails += $"\n\nChi tiết: {ex.InnerException.Message}";
                }
                
                System.Windows.MessageBox.Show(
                    $"Lỗi khi load dữ liệu nguyên tố:\n{errorDetails}",
                    "Lỗi",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error
                );
                _allElements = new List<ElementDetail>();
            }
        }

        public void LoadElementByAtomicNumber(int atomicNumber)
        {
            if (_allElements == null || _allElements.Count == 0)
            {
                System.Windows.MessageBox.Show(
                    "Không có dữ liệu nguyên tố",
                    "Lỗi",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning
                );
                return;
            }

            var element = _allElements.FirstOrDefault(e => e.AtomicNumber == atomicNumber);

            if (element != null)
            {
                CurrentElement = element;
            }
            else
            {
                System.Windows.MessageBox.Show(
                    $"Không tìm thấy dữ liệu cho nguyên tố số {atomicNumber}",
                    "Cảnh báo",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning
                );
            }
        }

        public void LoadElementBySymbol(string symbol)
        {
            if (_allElements == null || _allElements.Count == 0 || string.IsNullOrEmpty(symbol))
            {
                return;
            }

            var element = _allElements.FirstOrDefault(e => 
                e.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase)
            );

            if (element != null)
            {
                CurrentElement = element;
            }
        }

        public bool CanNavigatePrevious()
        {
            if (_allElements == null || _allElements.Count == 0 || CurrentElement == null)
                return false;
            
            int index = _allElements.FindIndex(e => e.AtomicNumber == CurrentElement.AtomicNumber);
            return index > 0;
        }

        public bool CanNavigateNext()
        {
            if (_allElements == null || _allElements.Count == 0 || CurrentElement == null)
                return false;
            
            int index = _allElements.FindIndex(e => e.AtomicNumber == CurrentElement.AtomicNumber);
            return index >= 0 && index < _allElements.Count - 1;
        }

        public void NavigateToPrevious()
        {
            if (_allElements == null || _allElements.Count == 0 || CurrentElement == null)
                return;

            int index = _allElements.FindIndex(e => e.AtomicNumber == CurrentElement.AtomicNumber);
            if (index > 0)
            {
                CurrentElement = _allElements[index - 1];
            }
        }

        public void NavigateToNext()
        {
            if (_allElements == null || _allElements.Count == 0 || CurrentElement == null)
                return;

            int index = _allElements.FindIndex(e => e.AtomicNumber == CurrentElement.AtomicNumber);
            if (index >= 0 && index < _allElements.Count - 1)
            {
                CurrentElement = _allElements[index + 1];
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

