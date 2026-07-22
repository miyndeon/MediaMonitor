
namespace MediaMonitor.Models;
public class Statistics
{
    public int TotalItems { get; set; }
    public int CompletedCount { get; set; }     
    public int PlannedCount { get; set; }      
    public int WatchingCount { get; set; }
    public int DroppedCount { get; set; }
    public double AverageRating { get; set; }
    public int TotalRewatches { get; set; }
    public Dictionary<MediaType, int> TypeDistribution { get; set; } = new();

    // Проценты для прогрессбара
    public double CompletedPercent => 
        TotalItems > 0 ? (double)CompletedCount / TotalItems * 100 : 0;
    public double WatchingPercent => 
        TotalItems > 0 ? (double)WatchingCount / TotalItems * 100 : 0;
    public double PlannedPercent => 
        TotalItems > 0 ? (double)PlannedCount / TotalItems * 100 : 0;
    public double DroppedPercent => 
        TotalItems > 0 ? (double)DroppedCount / TotalItems * 100 : 0;
}

