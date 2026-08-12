using System.Windows;

namespace QASmartTouch.PeriodicTable.Models
{
    public class PrecipitateParticle
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Size { get; set; }
        public double Opacity { get; set; }
        public string Color { get; set; }
        
        // Physics properties for Phương án 2
        public double VelocityX { get; set; }
        public double VelocityY { get; set; }
        public double Rotation { get; set; }
        public double RotationSpeed { get; set; }
        public bool HasLanded { get; set; }
        public bool IsFloating { get; set; } // Particles nổi trên mặt dung dịch
        public double TargetY { get; set; } // Vị trí đích (đáy hoặc mặt nước)
        public bool IsBubble { get; set; } // Hạt khí (bong bóng) bay lên

        public PrecipitateParticle(double x, double y, double size, string color)
        {
            X = x;
            Y = y;
            Size = size;
            Color = color;
            Opacity = 1.0;
            VelocityX = 0;
            VelocityY = 0;
            Rotation = 0;
            RotationSpeed = 0;
            HasLanded = false;
        }
    }
}

