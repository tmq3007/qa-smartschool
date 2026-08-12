using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace QASmartTouch.Forms
{
    public partial class CalculatorWindow : Window
    {
        private string _currentValue = "0";
        private string _operator = "";
        private double _firstNumber = 0;
        private double _memory = 0;
        private bool _hasMemory = false;
        private bool _isNewEntry = true;
        private string _expressionText = "";  // NEW: Track expression
        
        public CalculatorWindow()
        {
            InitializeComponent();
        }
        
        #region Number Input
        
        private void BtnNumber_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                string number = btn.Tag.ToString();
                
                if (_isNewEntry)
                {
                    _currentValue = number;
                    _isNewEntry = false;
                }
                else
                {
                    if (_currentValue == "0")
                        _currentValue = number;
                    else
                        _currentValue += number;
                }
                
                UpdateDisplay();
                UpdateExpressionDisplay();  // NEW
            }
        }
        
        private void BtnDecimal_Click(object sender, RoutedEventArgs e)
        {
            if (!_currentValue.Contains("."))
            {
                if (_isNewEntry)
                {
                    _currentValue = "0.";
                    _isNewEntry = false;
                }
                else
                {
                    _currentValue += ".";
                }
                
                UpdateDisplay();
            }
        }
        
        #endregion
        
        #region Basic Operations
        
        private void BtnOperator_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                if (_operator != "" && !_isNewEntry)
                {
                    Calculate();
                }
                
                _firstNumber = double.Parse(_currentValue, System.Globalization.CultureInfo.InvariantCulture);
                _operator = btn.Tag.ToString();
                _isNewEntry = true;
                UpdateExpressionDisplay();  // NEW
            }
        }
        
        private void BtnEquals_Click(object sender, RoutedEventArgs e)
        {
            Calculate();
            _operator = "";
        }
        
        private void Calculate()
        {
            if (_operator == "" || _isNewEntry) return;
            
            // Validate inputs
            if (!double.TryParse(_currentValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double secondNumber))
            {
                ShowError("Invalid number");
                return;
            }
            
            double result = 0;
            
            try
            {
                switch (_operator)
                {
                    case "+":
                        result = _firstNumber + secondNumber;
                        break;
                        
                    case "-":
                        result = _firstNumber - secondNumber;
                        break;
                        
                    case "*":
                        result = _firstNumber * secondNumber;
                        break;
                        
                    case "/":
                        if (Math.Abs(secondNumber) < double.Epsilon)
                        {
                            ShowError("Cannot divide by zero");
                            return;
                        }
                        result = _firstNumber / secondNumber;
                        break;
                }
                
                // Check result validity
                if (double.IsInfinity(result))
                {
                    ShowError("Result is too large");
                    return;
                }
                
                if (double.IsNaN(result))
                {
                    ShowError("Invalid operation");
                    return;
                }
                
                // Update expression to show complete calculation
                string operatorSymbol = GetOperatorSymbol(_operator);
                _expressionText = $"{FormatNumber(_firstNumber)} {operatorSymbol} {FormatNumber(secondNumber)} =";
                txtExpression.Text = _expressionText;
                
                _currentValue = result.ToString();
                _firstNumber = result;
                _isNewEntry = true;
                UpdateDisplay();
            }
            catch (OverflowException)
            {
                ShowError("Number overflow");
            }
            catch (Exception ex)
            {
                ShowError($"Calculation error: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Advanced Operations
        
        private void BtnSqrt_Click(object sender, RoutedEventArgs e)
        {
            double value = double.Parse(_currentValue, System.Globalization.CultureInfo.InvariantCulture);
            
            if (value < 0)
            {
                ShowError("Invalid input");
                return;
            }
            
            double result = Math.Sqrt(value);
            _currentValue = result.ToString();
            _isNewEntry = true;
            UpdateDisplay();
        }
        
        private void BtnSquare_Click(object sender, RoutedEventArgs e)
        {
            double value = double.Parse(_currentValue, System.Globalization.CultureInfo.InvariantCulture);
            double result = value * value;
            
            _currentValue = result.ToString();
            _isNewEntry = true;
            UpdateDisplay();
        }
        
        private void BtnReciprocal_Click(object sender, RoutedEventArgs e)
        {
            double value = double.Parse(_currentValue, System.Globalization.CultureInfo.InvariantCulture);
            
            if (value == 0)
            {
                ShowError("Cannot divide by zero");
                return;
            }
            
            double result = 1 / value;
            _currentValue = result.ToString();
            _isNewEntry = true;
            UpdateDisplay();
        }
        
        private void BtnPercent_Click(object sender, RoutedEventArgs e)
        {
            double value = double.Parse(_currentValue, System.Globalization.CultureInfo.InvariantCulture);
            
            if (_operator != "")
            {
                // Percent in operation
                value = _firstNumber * (value / 100);
            }
            else
            {
                // Simple percent
                value = value / 100;
            }
            
            _currentValue = value.ToString();
            UpdateDisplay();
        }
        
        private void BtnNegate_Click(object sender, RoutedEventArgs e)
        {
            double value = double.Parse(_currentValue, System.Globalization.CultureInfo.InvariantCulture);
            value = -value;
            
            _currentValue = value.ToString();
            UpdateDisplay();
        }
        
        #endregion
        
        #region Clear Functions
        
        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            _currentValue = "0";
            _operator = "";
            _firstNumber = 0;
            _isNewEntry = true;
            _expressionText = "";  // NEW
            UpdateDisplay();
            UpdateExpressionDisplay();  // NEW
        }
        
        private void BtnClearEntry_Click(object sender, RoutedEventArgs e)
        {
            _currentValue = "0";
            _isNewEntry = true;
            UpdateDisplay();
            UpdateExpressionDisplay();  // NEW
        }
        
        private void BtnBackspace_Click(object sender, RoutedEventArgs e)
        {
            if (_currentValue.Length > 1)
            {
                _currentValue = _currentValue.Substring(0, _currentValue.Length - 1);
            }
            else
            {
                _currentValue = "0";
            }
            
            UpdateDisplay();
        }
        
        #endregion
        
        #region Memory Functions
        
        private void BtnMemoryStore_Click(object sender, RoutedEventArgs e)
        {
            _memory = double.Parse(_currentValue, System.Globalization.CultureInfo.InvariantCulture);
            _hasMemory = true;
            UpdateMemoryIndicator();
        }
        
        private void BtnMemoryRecall_Click(object sender, RoutedEventArgs e)
        {
            if (_hasMemory)
            {
                _currentValue = _memory.ToString();
                _isNewEntry = true;
                UpdateDisplay();
            }
        }
        
        private void BtnMemoryAdd_Click(object sender, RoutedEventArgs e)
        {
            if (_hasMemory)
            {
                _memory += double.Parse(_currentValue, System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
                _memory = double.Parse(_currentValue, System.Globalization.CultureInfo.InvariantCulture);
                _hasMemory = true;
            }
            UpdateMemoryIndicator();
        }
        
        private void BtnMemorySubtract_Click(object sender, RoutedEventArgs e)
        {
            if (_hasMemory)
            {
                _memory -= double.Parse(_currentValue, System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
                _memory = -double.Parse(_currentValue, System.Globalization.CultureInfo.InvariantCulture);
                _hasMemory = true;
            }
            UpdateMemoryIndicator();
        }
        
        private void BtnMemoryClear_Click(object sender, RoutedEventArgs e)
        {
            _memory = 0;
            _hasMemory = false;
            UpdateMemoryIndicator();
        }
        
        private void BtnMemoryDropdown_Click(object sender, RoutedEventArgs e)
        {
            if (_hasMemory)
            {
                MessageBox.Show($"Memory: {_memory}", "Memory", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        
        #endregion
        
        #region Helper Functions
        
        private void UpdateDisplay()
        {
            try
            {
                // Validate current value first
                if (string.IsNullOrWhiteSpace(_currentValue))
                {
                    _currentValue = "0";
                }
                
                // Try to parse as double
                if (!double.TryParse(_currentValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double value))
                {
                    // If parse fails, reset to 0
                    _currentValue = "0";
                    value = 0;
                }
                
                // Check for infinity or NaN
                if (double.IsInfinity(value))
                {
                    txtDisplay.Text = "∞";
                    return;
                }
                
                if (double.IsNaN(value))
                {
                    txtDisplay.Text = "Error";
                    return;
                }
                
                // Format number for display
                if (Math.Abs(value) >= 1e15 || (Math.Abs(value) < 1e-10 && value != 0))
                {
                    // Scientific notation for very large/small numbers
                    txtDisplay.Text = value.ToString("E10");
                }
                else if (value == Math.Floor(value) && Math.Abs(value) < 1000000000000)
                {
                    // Integer display with thousand separators
                    txtDisplay.Text = ((long)value).ToString("N0");
                }
                else
                {
                    // Decimal display, trim trailing zeros
                    txtDisplay.Text = value.ToString("G15").TrimEnd('0').TrimEnd('.');
                }
                
                // Limit display length
                if (txtDisplay.Text.Length > 16)
                {
                    txtDisplay.Text = value.ToString("E10");
                }
            }
            catch (Exception ex)
            {
                // Fallback to safe display
                txtDisplay.Text = "0";
                _currentValue = "0";
                System.Diagnostics.Debug.WriteLine($"Display error: {ex.Message}");
            }
        }
        
        private void UpdateMemoryIndicator()
        {
            if (_hasMemory)
            {
                txtMemoryIndicator.Text = "M";
                txtMemoryIndicator.Visibility = Visibility.Visible;
            }
            else
            {
                txtMemoryIndicator.Visibility = Visibility.Collapsed;
            }
        }
        
        private void ShowError(string message)
        {
            MessageBox.Show(message, "Calculator Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            
            _currentValue = "0";
            _operator = "";
            _isNewEntry = true;
            _expressionText = "";  // NEW
            UpdateDisplay();
            UpdateExpressionDisplay();  // NEW
        }
        
        // NEW: Update expression display
        private void UpdateExpressionDisplay()
        {
            if (_operator != "" && _firstNumber != 0)
            {
                string operatorSymbol = GetOperatorSymbol(_operator);
                
                if (_isNewEntry)
                {
                    // Before entering second number: "52 ×"
                    _expressionText = $"{FormatNumber(_firstNumber)} {operatorSymbol}";
                }
                else
                {
                    // After entering second number: "52 × 6 ="
                    _expressionText = $"{FormatNumber(_firstNumber)} {operatorSymbol} {_currentValue} =";
                }
            }
            else
            {
                _expressionText = "";
            }
            
            txtExpression.Text = _expressionText;
        }
        
        // NEW: Get operator symbol for display
        private string GetOperatorSymbol(string op)
        {
            return op switch
            {
                "+" => "+",
                "-" => "−",
                "*" => "×",
                "/" => "÷",
                _ => op
            };
        }
        
        // NEW: Format number for display
        private string FormatNumber(double number)
        {
            // Check for special values
            if (double.IsInfinity(number))
                return "∞";
            
            if (double.IsNaN(number))
                return "Error";
            
            // Format based on magnitude
            if (Math.Abs(number) >= 1e15 || (Math.Abs(number) < 1e-10 && number != 0))
            {
                // Scientific notation for very large/small numbers
                return number.ToString("E10");
            }
            else if (number == Math.Floor(number) && Math.Abs(number) < 1000000000000)
            {
                // Integer with thousand separators
                return ((long)number).ToString("N0");
            }
            else
            {
                // Decimal, trim trailing zeros
                return number.ToString("G15").TrimEnd('0').TrimEnd('.');
            }
        }
        
        #endregion
    }
}
