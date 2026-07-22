using MediaMonitor.Models;
using Newtonsoft.Json;
using System.IO;

namespace MediaMonitor.Services;

public class DataService
{
    private readonly string _filePath;


    public DataService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var folder = Path.Combine(appData, "MediaMonitor");

        if (!Directory.Exists(folder)) 
            Directory.CreateDirectory(folder);

        _filePath = Path.Combine(folder, "media_data.json");
    }

    public async Task SaveAsync(List<MediaItem> items, string? path = null)
    {
        try
        {
            var json = JsonConvert.SerializeObject(items, Formatting.Indented);
            await File.WriteAllTextAsync(path ?? _filePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при сохранении: {ex.Message}");
        }

    }

    public async Task<List<MediaItem>> LoadAsync(string? path = null)
    {
        var targetPath = path ?? _filePath;

        if (!File.Exists(targetPath))
            return new List<MediaItem>();
        try
        {
            var json = await File.ReadAllTextAsync(targetPath);
            return JsonConvert.DeserializeObject<List<MediaItem>>(json) ?? new List<MediaItem>();

        }
        catch (Exception)
        {
            // Файл повреждён или недоступен — не роняем приложение при старте,
            // а просто начинаем с пустой коллекции.
            return new List<MediaItem>();
        }
    }

    //private List<MediaItem> CreateSampleData()
    //{
    //    var sample = new List<MediaItem>
    //    {
    //        new MediaItem
    //        {
    //            Title = "Интерстеллар",
    //            PosterUrl = "http://www.world-art.ru/cinema/img/40000/36765/4i2s.jpg",
    //            Type = MediaType.Movie,
    //            Status = MediaStatus.Completed,
    //            Rating = 9.5,
    //            RewatchCount = 2,
    //            Comment = "Шедевр",
    //            CompletionDate = DateTime.Now.AddDays(-30)
    //        },
    //        new MediaItem
    //        {
    //            Title = "Очень странные дела",
    //            PosterUrl = "http://www.world-art.ru/cinema/img/70000/69751/miq3e.jpg",
    //            Type = MediaType.Series,
    //            Status = MediaStatus.Watching,
    //            Rating = 9,
    //            RewatchCount = 0,
    //            Comment = "5 сезонов",
    //            CompletionDate = null
    //        }
    //    };
    //    return sample;
    //}
}
