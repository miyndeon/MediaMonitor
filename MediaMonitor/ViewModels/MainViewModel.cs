using MediaMonitor.Helpers;
using MediaMonitor.Models;
using MediaMonitor.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
namespace MediaMonitor.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly DataService _dataService;
    private readonly DispatcherTimer _autoSaveTimer;

    private ObservableCollection<MediaItem> _allItems = new();
    private ObservableCollection<MediaItem> _filteredItems = new();
    private Statistics _statistics = new ();
    private ObservableCollection<TypeStatItem> _typeStats = new ();
    private ObservableCollection<string> _allTags = new();
    private ObservableCollection<string> _selectedTags = new();
    private string _searchText = string.Empty;
    private string _selectedStatusFilter = "Все";
    private string _selectedTypeFilter = "Все типы";
    private string _selectedSort = "По дате";
    private MediaItem? _selectedItem;

    public ObservableCollection<MediaItem> AllItems
    {
        get => _allItems;
        set { _allItems = value; OnPropertyChanged(); }
    }
    public ObservableCollection<MediaItem> FilteredItems 
    { 
        get => _filteredItems; 
        set { _filteredItems = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasResults)); }
    }
    public Statistics Statistics 
    { 
        get => _statistics; 
        set { _statistics = value; OnPropertyChanged(); }
    }
    public ObservableCollection<TypeStatItem> TypeStats 
    { 
        get => _typeStats; 
        set { _typeStats = value; OnPropertyChanged(); }
    }

    // Все теги, встречающиеся в коллекции (для панели фильтров)
    public ObservableCollection<string> AllTags
    {
        get => _allTags;
        set { _allTags = value; OnPropertyChanged(); }
    }

    // Теги, выбранные в качестве фильтра
    public ObservableCollection<string> SelectedTags
    {
        get => _selectedTags;
        set { _selectedTags = value; OnPropertyChanged(); }
    }

    // Есть ли хоть один результат после применения фильтров/поиска
    public bool HasResults => FilteredItems.Count > 0;

    private DateTime? _lastSavedAt;
    public DateTime? LastSavedAt
    {
        get => _lastSavedAt;
        set { _lastSavedAt = value; OnPropertyChanged(); OnPropertyChanged(nameof(LastSavedText)); }
    }

    // Текст-индикатор для титульного бара ("Сохранено 14:32")
    public string LastSavedText => LastSavedAt.HasValue ? $"Сохранено {LastSavedAt:HH:mm}" : string.Empty;

    private bool _showFavoritesOnly;
    public bool ShowFavoritesOnly
    {
        get => _showFavoritesOnly;
        set { _showFavoritesOnly = value; OnPropertyChanged(); ApplyFilters(); }
    }

    public string SearchText
    {
        get => _searchText;
        set { _searchText = value; OnPropertyChanged(); ApplyFilters(); }
    }

    // фильтры сверху
    public string SelectedStatusFilter
    {
        get => _selectedStatusFilter; 
        set { _selectedStatusFilter = value; OnPropertyChanged(); ApplyFilters();}
    }

    public string SelectedTypeFilter
    {
        get => _selectedTypeFilter;
        set { _selectedTypeFilter = value; OnPropertyChanged(); ApplyFilters(); }
    }
    public string SelectedSort
    {
        get => _selectedSort; 
        set { _selectedSort = value; OnPropertyChanged(); ApplyFilters();}
    }
    public MediaItem? SelectedItem
    {
        get => _selectedItem;
        set { _selectedItem = value; OnPropertyChanged(); }
    }

    public List<string> SortOptions { get; } = new()
    {
        "По дате","По названию","По рейтингу","По типу"
    };

    public ICommand AddItemCommand { get; }
    public ICommand EditItemCommand { get; }
    public ICommand DeleteItemCommand { get; }
    public ICommand SetStatusFilterCommand { get; }
    public ICommand SetTypeFilterCommand { get; }
    public ICommand ToggleTagFilterCommand { get; }
    public ICommand ToggleFavoritesFilterCommand { get; }
    public ICommand ToggleFavoriteCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand ImportCommand { get; }

    public event Action? OpenAddDialog;
    public event Action<MediaItem>? OpenEditDialog;
    public event Func<MediaItem,bool>? ConfirmDelete;

    public MainViewModel()
    {
        _dataService = new DataService();

        AddItemCommand = new RelayCommand(_ => OpenAddDialog?.Invoke());

        EditItemCommand = new RelayCommand(param =>
        {
            var item = param as MediaItem ?? SelectedItem;
            if (item != null) { OpenEditDialog?.Invoke(item); }
        });

        DeleteItemCommand = new RelayCommand(param =>
        {
            var item = param as MediaItem ?? SelectedItem;
            if (item != null) { TryDeleteItem(item); }
        });

        SetStatusFilterCommand = new RelayCommand(param =>
        {
            if (param is string filter) SelectedStatusFilter = filter;
        });

        SetTypeFilterCommand = new RelayCommand(param =>
        {
            if (param is string filter) SelectedTypeFilter = filter;
        });

        ToggleTagFilterCommand = new RelayCommand(param =>
        {
            if (param is string tag) ToggleTagFilter(tag);
        });

        ToggleFavoritesFilterCommand = new RelayCommand(_ => ShowFavoritesOnly = !ShowFavoritesOnly);

        ToggleFavoriteCommand = new RelayCommand(param =>
        {
            if (param is MediaItem item)
            {
                item.IsFavorite = !item.IsFavorite;
                ApplyFilters();
                AutoSave();
            }
        });

        ExportCommand = new RelayCommand(_ => ExportData());
        ImportCommand = new RelayCommand(_ => ImportData());

        // Автосохранение: помимо сохранения при каждом изменении коллекции (см. AutoSave),
        // периодически подстраховываемся таймером на случай непредвиденных сценариев.
        _autoSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(2) };
        _autoSaveTimer.Tick += (_, _) => AutoSave();
        _autoSaveTimer.Start();
    }

    // Вызывается один раз из MainWindow после инициализации окна.
    public async Task InitializeAsync()
    {
        var items = await _dataService.LoadAsync();
        AllItems = new ObservableCollection<MediaItem>(items);
        RefreshAllTags();
        ApplyFilters();
        UpdateStatistics();
    }

    // Случайный элемент со статусом "В процессе" — для окна "Продолжить просмотр".
    public MediaItem? GetRandomInProgressItem()
    {
        var watching = AllItems.Where(x => x.Status == MediaStatus.Watching).ToList();
        if (watching.Count == 0) return null;

        return watching[Random.Shared.Next(watching.Count)];
    }

    // Гарантированное сохранение "здесь и сейчас" (например, перед закрытием окна).
    public async Task SaveNowAsync()
    {
        await _dataService.SaveAsync(AllItems.ToList());
        LastSavedAt = DateTime.Now;
    }

    private async void ExportData()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Экспорт коллекции",
            Filter = "JSON файл (*.json)|*.json",
            FileName = $"MediaMonitor_export_{DateTime.Now:yyyy-MM-dd}.json"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            await _dataService.SaveAsync(AllItems.ToList(), dialog.FileName);
            MessageBox.Show("Коллекция успешно экспортирована.", "Экспорт",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось экспортировать: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void ImportData()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Импорт коллекции",
            Filter = "JSON файл (*.json)|*.json"
        };

        if (dialog.ShowDialog() != true) return;

        var result = MessageBox.Show(
            "Импорт заменит текущую коллекцию. Продолжить?",
            "Импорт", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            var imported = await _dataService.LoadAsync(dialog.FileName);
            AllItems = new ObservableCollection<MediaItem>(imported);
            RefreshAllTags();
            ApplyFilters();
            UpdateStatistics();
            AutoSave();

            MessageBox.Show("Коллекция успешно импортирована.", "Импорт",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось импортировать: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void AutoSave()
    {
        try
        {
            await _dataService.SaveAsync(AllItems.ToList());
            LastSavedAt = DateTime.Now;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    public void ApplyFilters()
    {
        var query = AllItems.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            query = query.Where(x => x.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        query = SelectedStatusFilter switch
        {
            "В планах" => query.Where(x => x.Status == MediaStatus.Planned),
            "В процессе" => query.Where(x => x.Status == MediaStatus.Watching),
            "Брошено" => query.Where(x => x.Status == MediaStatus.Dropped),
            "Завершено" => query.Where(x => x.Status == MediaStatus.Completed),
            _ => query

        };

        query = SelectedTypeFilter switch
        {
            "Фильм" => query.Where(x => x.Type == MediaType.Movie),
            "Сериал" => query.Where(x => x.Type == MediaType.Series),
            "Аниме" => query.Where(x => x.Type == MediaType.Anime),
            "Игра" => query.Where(x => x.Type == MediaType.Game),
            "Другое" => query.Where(x => x.Type == MediaType.Other),
            _ => query

        };

        if (SelectedTags.Count > 0)
        {
            query = query.Where(x => x.Tags != null &&
                x.Tags.Any(t => SelectedTags.Contains(t, StringComparer.OrdinalIgnoreCase)));
        }

        if (ShowFavoritesOnly)
        {
            query = query.Where(x => x.IsFavorite);
        }

        query = SelectedSort switch
        {
            "По названию" => query.OrderBy(x => x.Title),
            "По рейтингу" => query.OrderByDescending(x => x.Rating),
            "По типу" => query.OrderBy(x => x.Type),
            _ => query.OrderByDescending(x => x.DateAdded),
        };

        FilteredItems = new ObservableCollection<MediaItem>(query);

    }

    private void ToggleTagFilter(string tag)
    {
        var updated = new ObservableCollection<string>(SelectedTags);

        bool alreadySelected = updated.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));
        if (alreadySelected)
        {
            var filtered = updated.Where(t => !string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));
            updated = new ObservableCollection<string>(filtered);
        }
        else
        {
            updated.Add(tag);
        }

        // Присваиваем новый экземпляр коллекции, чтобы биндинги подсветки кнопок
        // корректно обновились (ObservableCollection не уведомляет о себе самой).
        SelectedTags = updated;
        ApplyFilters();
    }

    private void RefreshAllTags()
    {
        var tags = AllItems
            .SelectMany(x => x.Tags ?? new List<string>())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
            .ToList();

        AllTags = new ObservableCollection<string>(tags);

        // Убираем из выбранных фильтров теги, которых больше не существует в коллекции
        var stillValid = SelectedTags
            .Where(t => tags.Contains(t, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (stillValid.Count != SelectedTags.Count)
            SelectedTags = new ObservableCollection<string>(stillValid);
    }

    public void UpdateStatistics()
    {
        Statistics = StatisticService.Calculate(AllItems);

        int total = AllItems.Count;
        var rows = new (MediaType type, string label, Brush color)[]
        {
            (MediaType.Movie, "🎞 Фильм", new SolidColorBrush(Color.FromRgb(128, 176, 232))),
            (MediaType.Series, "💻 Сериалы", new SolidColorBrush(Color.FromRgb(220, 150, 150))),
            (MediaType.Anime, "🎐 Аниме", new SolidColorBrush(Color.FromRgb(0, 180, 140))),
            (MediaType.Game, "👾 Игры", new SolidColorBrush(Color.FromRgb(200, 195, 80))),
            (MediaType.Other, "🛋 Другое", new SolidColorBrush(Color.FromRgb(180, 170, 220))),
        };

        TypeStats = new ObservableCollection<TypeStatItem>(
            rows.Select(row =>
            {
                int count = AllItems.Count(x => x.Type == row.type);
                return new TypeStatItem
                {
                    Label = row.label,
                    Count = count,
                    Percent = total > 0 ? (double)count / total*100.0 : 0,
                    Color = row.color

                };
            })
        );

    }


    private void TryDeleteItem(MediaItem item)
    {
        if (ConfirmDelete?.Invoke(item) == false) 
            return;
        AllItems.Remove(item);
        if(SelectedItem == item) 
            SelectedItem = null;

        RefreshAllTags();
        ApplyFilters();
        UpdateStatistics();
        AutoSave();
    }

    public void AddOrUpdateItem(MediaItem item)
    {
        for (int i = 0; i < AllItems.Count; i++) 
        {
            if (AllItems[i].Id == item.Id)
            {
                AllItems[i] = item;
                RefreshAllTags();
                ApplyFilters();
                UpdateStatistics();
                AutoSave();
                return;
            }
        }

        AllItems.Add(item);
        RefreshAllTags();
        ApplyFilters();
        UpdateStatistics();
        AutoSave();
    }


}
