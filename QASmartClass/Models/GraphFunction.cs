using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace QASmartTouch.Models
{
    /// <summary>
    /// Represents a mathematical function graph with configurable parameters
    /// Implements INotifyPropertyChanged for real-time UI updates
    /// </summary>
    public class GraphFunction : INotifyPropertyChanged
    {
        private int _id;
        private FunctionType _type;
        private double _a = 1;
        private double _b = 0;
        private double _c = 1;
        private double _d = 0;
        private Color _color;
        private bool _isVisible = true;

        #region Properties

        /// <summary>
        /// Unique identifier for the graph
        /// </summary>
        public int Id
        {
            get => _id;
            set => SetField(ref _id, value);
        }

        /// <summary>
        /// Type of mathematical function
        /// </summary>
        public FunctionType Type
        {
            get => _type;
            set
            {
                if (SetField(ref _type, value))
                {
                    OnParameterChanged();
                }
            }
        }

        /// <summary>
        /// Coefficient 'a' - primary scaling parameter
        /// </summary>
        public double A
        {
            get => _a;
            set
            {
                if (SetField(ref _a, value))
                {
                    OnParameterChanged();
                }
            }
        }

        /// <summary>
        /// Coefficient 'b' - secondary parameter (often translation)
        /// </summary>
        public double B
        {
            get => _b;
            set
            {
                if (SetField(ref _b, value))
                {
                    OnParameterChanged();
                }
            }
        }

        /// <summary>
        /// Coefficient 'c' - tertiary parameter (for cubic/rational functions)
        /// </summary>
        public double C
        {
            get => _c;
            set
            {
                if (SetField(ref _c, value))
                {
                    OnParameterChanged();
                }
            }
        }

        /// <summary>
        /// Coefficient 'd' - quaternary parameter (for cubic/rational functions)
        /// </summary>
        public double D
        {
            get => _d;
            set
            {
                if (SetField(ref _d, value))
                {
                    OnParameterChanged();
                }
            }
        }

        /// <summary>
        /// Color of the graph line
        /// </summary>
        public Color Color
        {
            get => _color;
            set => SetField(ref _color, value);
        }

        /// <summary>
        /// Whether the graph is visible on the plot
        /// </summary>
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (SetField(ref _isVisible, value))
                {
                    OnParameterChanged();
                }
            }
        }

        #endregion

        #region Events

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler? ParameterChanged;

        #endregion

        #region Methods

        /// <summary>
        /// Calculates the y-value for a given x-value based on the function type and parameters
        /// </summary>
        /// <param name="x">Input x-value</param>
        /// <returns>Calculated y-value, or null if undefined</returns>
        public double? CalculateY(double x)
        {
            try
            {
                switch (Type)
                {
                    case FunctionType.Absolute:
                        // y = |ax + b|
                        return Math.Abs(A * x + B);

                    case FunctionType.Sin:
                        // y = A*sin(B*x + C) + D
                        return A * Math.Sin(B * x + C) + D;

                    case FunctionType.Cos:
                        // y = A*cos(B*x + C) + D
                        return A * Math.Cos(B * x + C) + D;

                    case FunctionType.Tan:
                        // y = A*tan(B*x + C) + D
                        double tanValue = Math.Tan(B * x + C);
                        // Filter out asymptotes (near π/2, 3π/2, etc.)
                        return (Math.Abs(tanValue) > 20) ? null : (A * tanValue + D);

                    case FunctionType.Exponential:
                        // y = a^x (using absolute value of a, minimum base = 2)
                        double baseValue = Math.Abs(A) > 1 ? Math.Abs(A) : 2;
                        double expResult = Math.Pow(baseValue, x);
                        // Limit to prevent overflow
                        return (expResult > 1000) ? null : expResult;

                    case FunctionType.Logarithm:
                        // y = log_a(x)
                        if (x <= 0) return null; // Undefined for x ≤ 0
                        double logBase = Math.Abs(A) > 1 ? Math.Abs(A) : 2;
                        return Math.Log(x) / Math.Log(logBase);

                    case FunctionType.Cubic:
                        // y = ax³ + bx² + cx + d
                        return A * Math.Pow(x, 3) + B * Math.Pow(x, 2) + C * x + D;

                    case FunctionType.Rational:
                        // y = (ax + b) / (cx + d)
                        double denominator = C * x + D;
                        if (Math.Abs(denominator) < 0.01) return null; // Near vertical asymptote
                        double rationalResult = (A * x + B) / denominator;
                        // Limit to prevent extreme values
                        return (Math.Abs(rationalResult) > 100) ? null : rationalResult;

                    case FunctionType.Composite:
                        // y = sin(ax + b)
                        return Math.Sin(A * x + B);

                    default:
                        return 0;
                }
            }
            catch
            {
                return null; // Return null for any calculation errors
            }
        }

        /// <summary>
        /// Gets a user-friendly display name for the function
        /// </summary>
        public string GetFunctionName()
        {
            return Type switch
            {
                FunctionType.Absolute => "Trị tuyệt đối",
                FunctionType.Sin => "Sin",
                FunctionType.Cos => "Cosin",
                FunctionType.Tan => "Tang",
                FunctionType.Exponential => "Mũ",
                FunctionType.Logarithm => "Logarit",
                FunctionType.Cubic => "Bậc ba",
                FunctionType.Rational => "Phân thức",
                FunctionType.Composite => "Hàm hợp",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Gets the mathematical formula string for display (bindable property)
        /// </summary>
        public string FormulaString => GetFormulaString();

        /// <summary>
        /// Gets the mathematical formula string for display
        /// </summary>
        public string GetFormulaString()
        {
            return Type switch
            {
                FunctionType.Absolute => $"y = {FormatCoeff(A)}·|{FormatCoeff(C)}x + {FormatParam(B)}|",
                FunctionType.Sin => $"y = {FormatCoeff(A)}·sin({FormatCoeff(C)}x + {FormatParam(B)}) + {FormatParam(D)}",
                FunctionType.Cos => $"y = {FormatCoeff(A)}·cos({FormatCoeff(C)}x + {FormatParam(B)}) + {FormatParam(D)}",
                FunctionType.Tan => $"y = {FormatCoeff(A)}·tan({FormatCoeff(C)}x + {FormatParam(B)}) + {FormatParam(D)}",
                FunctionType.Exponential => $"y = {FormatCoeff(A)}·{Math.Abs(C):F1}^x + {FormatParam(D)}",
                FunctionType.Logarithm => $"y = {FormatCoeff(A)}·log{SubScript(Math.Abs(C))}(x + {FormatParam(B)}) + {FormatParam(D)}",
                FunctionType.Cubic => $"y = {FormatCoeff(A)}x³ + {FormatCoeff(B)}x² + {FormatCoeff(C)}x + {FormatParam(D)}",
                FunctionType.Rational => $"y = ({FormatCoeff(A)}x + {FormatParam(B)}) / ({FormatCoeff(C)}x + {FormatParam(D)})",
                FunctionType.Composite => $"y = {FormatCoeff(A)}·sin({FormatCoeff(C)}x + {FormatParam(B)}) + {FormatParam(D)}",
                _ => "y = 0"
            };
        }

        /// <summary>
        /// Format coefficient with proper sign handling (for multiplication)
        /// </summary>
        private string FormatCoeff(double value)
        {
            return value.ToString("F1");
        }

        /// <summary>
        /// Format parameter with sign (for addition/subtraction)
        /// </summary>
        private string FormatParam(double value)
        {
            return value >= 0 ? value.ToString("F1") : value.ToString("F1");
        }

        /// <summary>
        /// Convert number to subscript Unicode characters
        /// </summary>
        private string SubScript(double value)
        {
            string num = value.ToString("F1").Replace(".", "");
            return num.Replace("0", "₀").Replace("1", "₁").Replace("2", "₂")
                      .Replace("3", "₃").Replace("4", "₄").Replace("5", "₅")
                      .Replace("6", "₆").Replace("7", "₇").Replace("8", "₈")
                      .Replace("9", "₉");
        }

        /// <summary>
        /// Get mathematical properties information
        /// </summary>
        public string GetMathematicalInfo()
        {
            var info = new System.Text.StringBuilder();
            
            switch (Type)
            {
                case FunctionType.Sin:
                case FunctionType.Cos:
                    info.AppendLine($"• Biên độ: A = {Math.Abs(A):F1}");
                    info.AppendLine($"• Tần số: ω = {Math.Abs(C):F1}");
                    info.AppendLine($"• Pha: φ = {B:F1}");
                    info.AppendLine($"• Dịch chuyển Y: {D:F1}");
                    double period = 2 * Math.PI / Math.Abs(C);
                    info.AppendLine($"• Chu kỳ: T = {period:F2} ≈ {period:F2}");
                    double maxY = Math.Abs(A) + D;
                    double minY = -Math.Abs(A) + D;
                    info.AppendLine($"• Giá trị max: {maxY:F1}");
                    info.AppendLine($"• Giá trị min: {minY:F1}");
                    break;

                case FunctionType.Absolute:
                    info.AppendLine($"• Đỉnh tại: x = {-B/A:F2}");
                    info.AppendLine($"• Giá trị min: 0");
                    info.AppendLine($"• Dạng V, mở lên");
                    break;

                case FunctionType.Cubic:
                    info.AppendLine($"• Bậc 3 (đa thức)");
                    info.AppendLine($"• Hệ số cao nhất: {A:F1}");
                    if (A > 0)
                        info.AppendLine($"• Hàm tăng tại ±∞");
                    else
                        info.AppendLine($"• Hàm giảm tại ±∞");
                    break;

                case FunctionType.Rational:
                    double asymptoteX = -D / C;
                    info.AppendLine($"• Tiệm cận đứng: x = {asymptoteX:F2}");
                    double asymptoteY = A / C;
                    info.AppendLine($"• Tiệm cận ngang: y = {asymptoteY:F2}");
                    info.AppendLine($"• Miền xác định: ℝ \\ {{{asymptoteX:F2}}}");
                    break;

                case FunctionType.Exponential:
                    info.AppendLine($"• Hàm mũ cơ số: {Math.Abs(C):F1}");
                    info.AppendLine($"• Luôn dương: y > 0");
                    info.AppendLine($"• Tiệm cận: y = 0");
                    break;

                case FunctionType.Logarithm:
                    info.AppendLine($"• Hàm logarit cơ số: {Math.Abs(C):F1}");
                    info.AppendLine($"• Miền xác định: x > 0");
                    info.AppendLine($"• Tiệm cận: x = 0");
                    break;
            }

            return info.ToString();
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void OnParameterChanged()
        {
            OnPropertyChanged(nameof(FormulaString)); // Update formula display
            ParameterChanged?.Invoke(this, EventArgs.Empty);
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }
}
