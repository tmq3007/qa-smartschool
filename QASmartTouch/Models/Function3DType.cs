using System;

namespace QASmartTouch.Models
{
    /// <summary>
    /// Defines supported 3D mathematical function types for z = f(x, y)
    /// Each type represents a different surface equation with configurable parameters
    /// </summary>
    public enum Function3DType
    {
        /// <summary>
        /// Plane: z = ax + by + c
        /// Parameters: a (x-slope), b (y-slope), c (z-intercept)
        /// </summary>
        Plane,

        /// <summary>
        /// Elliptic Paraboloid: z = a*x² + b*y² + c
        /// Parameters: a (x-curvature), b (y-curvature), c (z-offset)
        /// Creates bowl or dome shape
        /// </summary>
        Paraboloid,

        /// <summary>
        /// Hyperbolic Paraboloid (Saddle): z = a*x² - b*y² + c
        /// Parameters: a (x-curvature), b (y-curvature), c (z-offset)
        /// Creates saddle-shaped surface
        /// </summary>
        Saddle,

        /// <summary>
        /// Hemisphere: z = sqrt(r² - x² - y²) where r = a
        /// Parameters: a (radius)
        /// Domain: x² + y² ≤ a²
        /// </summary>
        Sphere,

        /// <summary>
        /// Cone: z = a * sqrt(x² + y²)
        /// Parameters: a (slope angle factor)
        /// </summary>
        Cone,

        /// <summary>
        /// Sine Wave: z = a * sin(b*x) * sin(c*y) + d
        /// Parameters: a (amplitude), b (x-frequency), c (y-frequency), d (z-offset)
        /// </summary>
        SineWave,

        /// <summary>
        /// Gaussian (Bell curve): z = a * exp(-((x-b)²/(2*c²) + (y-d)²/(2*e²)))
        /// Parameters: a (amplitude), b (x-center), c (x-spread), d (y-center), e (y-spread)
        /// </summary>
        Gaussian,

        /// <summary>
        /// Ripple (Circular wave): z = a * sin(b * sqrt(x² + y²)) + c
        /// Parameters: a (amplitude), b (frequency), c (z-offset)
        /// </summary>
        Ripple
    }
}
