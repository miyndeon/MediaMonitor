using MediaMonitor.Models;
using System.Collections.ObjectModel;

namespace MediaMonitor.Services;

class StatisticService
{
    public static Statistics Calculate(Collection<MediaItem> items)
    {
        return new Statistics
        {
            TotalItems = items.Count,
            CompletedCount = items.Count(x => x.Status == MediaStatus.Completed),
            WatchingCount = items.Count(x => x.Status == MediaStatus.Watching),
            PlannedCount = items.Count(x => x.Status == MediaStatus.Planned),
            DroppedCount = items.Count(x => x.Status == MediaStatus.Dropped),

            AverageRating = items.Any(x => x.Rating > 0) ? items.Where(x => x.Rating > 0).Average(x => x.Rating) : 0,
            TotalRewatches = items.Sum(x => x.RewatchCount),
            TypeDistribution = items.GroupBy(x => x.Type).ToDictionary(g => g.Key, g => g.Count())
        };
    }

}

