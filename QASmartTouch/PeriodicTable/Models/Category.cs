using System.Windows.Media;

namespace QASmartTouch.PeriodicTable.Models
{
    public class Category
    {
        public string Name { get; set; }
        public string Color { get; set; }
        
        public Brush ColorBrush 
        { 
            get 
            { 
                try 
                { 
                    return (Brush)new BrushConverter().ConvertFromString(Color); 
                } 
                catch 
                { 
                    return Brushes.LightGray; 
                } 
            } 
        }
    }
}
