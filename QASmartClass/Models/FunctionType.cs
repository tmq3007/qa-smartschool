namespace QASmartTouch.Models
{
    /// <summary>
    /// Enum defining types of mathematical functions supported in the graph plotter
    /// </summary>
    public enum FunctionType
    {
        /// <summary>
        /// Absolute value function: y = |ax + b|
        /// </summary>
        Absolute,

        /// <summary>
        /// Sine function: y = sin(x)
        /// </summary>
        Sin,

        /// <summary>
        /// Cosine function: y = cos(x)
        /// </summary>
        Cos,

        /// <summary>
        /// Tangent function: y = tan(x)
        /// </summary>
        Tan,

        /// <summary>
        /// Exponential function: y = a^x
        /// </summary>
        Exponential,

        /// <summary>
        /// Logarithm function: y = log_a(x)
        /// </summary>
        Logarithm,

        /// <summary>
        /// Cubic function: y = ax³ + bx² + cx + d
        /// </summary>
        Cubic,

        /// <summary>
        /// Rational function: y = (ax + b) / (cx + d)
        /// </summary>
        Rational,

        /// <summary>
        /// Composite function: y = sin(ax + b)
        /// </summary>
        Composite
    }
}
