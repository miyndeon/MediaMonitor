using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MediaMonitor.Services;

public class AppSettings
{
    public double Left { get; set; } = 100;
    public double Top { get; set; } = 100;
    public double Width { get; set; } = 1010;
    public double Height { get; set; } = 660;
    public bool IsMaximized { get; set; } = false;
}

public class SettingsService
{
    private readonly string _filePath;

    public SettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var folder = Path.Combine(appData, "MediaMonitor");

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        _filePath = Path.Combine(folder, "app_settings.json");
    }

    public async Task SaveAsync(AppSettings settings)
    {
        try
        {
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            await File.WriteAllTextAsync(_filePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при сохранении настроек: {ex.Message}");
        }
    }

    public async Task<AppSettings> LoadAsync()
    {
        if (!File.Exists(_filePath))
            return new AppSettings();

        try
        {
            var json = await File.ReadAllTextAsync(_filePath);
            return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
    }
}
