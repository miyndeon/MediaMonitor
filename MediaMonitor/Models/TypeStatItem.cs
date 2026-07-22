using System.Windows.Media;

namespace MediaMonitor.Models;

// Боковой прогрессбар
public class TypeStatItem
{
    public string Label { get; set; } = "";
    public int Count { get; set; }
    public double Percent {  get; set; }
    public Brush Color { get; set; } = Brushes.Gray;
}
