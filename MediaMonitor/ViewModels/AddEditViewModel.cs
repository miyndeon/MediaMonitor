using MediaMonitor.Helpers;
using MediaMonitor.Models;
using MediaMonitor.Services;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MediaMonitor.ViewModels
{
    class AddEditViewModel : ViewModelBase
    {
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };

        private MediaItem _item;
        private bool _isSearchingPoster;
        private string _posterSearchStatus = string.Empty;
        private string _newTagText = string.Empty;

        public string Title
        {
            get => _item.Title;
            set { _item.Title = value; OnPropertyChanged(); }
        }
        public string PosterUrl
        {
            get => _item.PosterUrl;
            set { _item.PosterUrl = value; OnPropertyChanged(); }
        }
        public MediaType Type
        {  
            get => _item.Type;
            set { _item.Type = value; OnPropertyChanged(); }
        }
        public MediaStatus Status
        {
            get => _item.Status;
            set 
            { 
                _item.Status = value; 
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsCompleted));
                OnPropertyChanged(nameof(CompletionDate));
            }
        }
        public double Rating 
        { 
            get => _item.Rating;
            set { _item.Rating = Math.Clamp(value, 0, 10); OnPropertyChanged(); }
        }
        public int RewatchCount
        {
            get => _item.RewatchCount;
            set { _item.RewatchCount = value; OnPropertyChanged(); }
        }
        public string Comment
        {
            get => _item.Comment;
            set { _item.Comment = value; OnPropertyChanged(); }
        }

        public DateTime DateAdded
        {
            get => _item.DateAdded;
            set { _item.DateAdded = value; OnPropertyChanged(); }
        }

        public DateTime? CompletionDate
        {
            get => _item.CompletionDate;
            set { _item.CompletionDate = value; OnPropertyChanged(); }
        }

        public bool IsCompleted => _item.Status == MediaStatus.Completed;

        public bool IsFavorite
        {
            get => _item.IsFavorite;
            set { _item.IsFavorite = value; OnPropertyChanged(); }
        }

        // Свободные теги/жанры
        public ObservableCollection<string> Tags { get; } = new();

        public string NewTagText
        {
            get => _newTagText;
            set { _newTagText = value; OnPropertyChanged(); }
        }

        public bool IsSearchPoster
        {
            get => _isSearchingPoster;
            set { _isSearchingPoster = value; OnPropertyChanged(); }
        }
        public string SearchPosterStatus
        {
            get => _posterSearchStatus;
            set { _posterSearchStatus = value; OnPropertyChanged(); }
        }


        public List<MediaType> MediaTypes { get; } = new (Enum.GetValues<MediaType>());
        public List<MediaStatus> MediaStatuses { get; } = new(Enum.GetValues<MediaStatus>());

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand SearchPosterCommand { get; }
        public ICommand AddTagCommand { get; }
        public ICommand RemoveTagCommand { get; }

        public event Action<MediaItem>? OnSave;
        public event Action? OnCancel;

        public AddEditViewModel(MediaItem? existing = null)
        {
            if (existing != null)
            {
                _item = new MediaItem
                {
                    Id = existing.Id,
                    Title = existing.Title,
                    PosterUrl = existing.PosterUrl,
                    Type = existing.Type,
                    Status = existing.Status,
                    Rating = existing.Rating,
                    RewatchCount = existing.RewatchCount,
                    Comment = existing.Comment,
                    DateAdded = existing.DateAdded,
                    Tags = new List<string>(existing.Tags ?? new List<string>()),
                    IsFavorite = existing.IsFavorite,
                };

                if (existing.Status == MediaStatus.Completed && existing.CompletionDate.HasValue)
                    _item.CompletionDate = existing.CompletionDate;
            }
            else
            {
                _item = new MediaItem();
            }

            foreach (var tag in _item.Tags)
                Tags.Add(tag);

            SaveCommand = new RelayCommand(
                () =>
                {
                    _item.Tags = Tags.ToList();
                    OnSave?.Invoke(_item);
                },
                () => !string.IsNullOrWhiteSpace(Title));

            CancelCommand = new RelayCommand( () => OnCancel?.Invoke());

            SearchPosterCommand = new RelayCommand(
                _ => SearchPosterAsync(),
                _ => !IsSearchPoster && !string.IsNullOrWhiteSpace(Title));

            AddTagCommand = new RelayCommand(
                _ => AddTag(),
                _ => !string.IsNullOrWhiteSpace(NewTagText));

            RemoveTagCommand = new RelayCommand(param =>
            {
                if (param is string tag) Tags.Remove(tag);
            });
        }

        private void AddTag()
        {
            var tag = NewTagText.Trim();
            if (string.IsNullOrWhiteSpace(tag)) return;

            bool alreadyExists = Tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));
            if (!alreadyExists)
                Tags.Add(tag);

            NewTagText = string.Empty;
        }

        private async void SearchPosterAsync()
        {
            if (string.IsNullOrWhiteSpace(Title)) return;

            IsSearchPoster = true;
            _posterSearchStatus = "🔎 Ищем постер..";
            string? url = null;

            try
            {
                url = Type switch
                {
                    MediaType.Anime => await SearchKinopoiskAsync(Title),

                    MediaType.Game =>
                    await SearchSteamAsync(Title),_ => await SearchKinopoiskAsync(Title)
                };

                if (url != null)
                {
                    PosterUrl = url;
                    SearchPosterStatus = "⚡️Постер найден!";
                }
                else if (string.IsNullOrEmpty(SearchPosterStatus))
                {
                    SearchPosterStatus = "🌧 Постер не найден..Вставьте URL вручную.";
                }
            }
            catch
            {
                SearchPosterStatus = "😭 Ошибка..";
            }
            finally 
            { 
                IsSearchPoster = false;
            }
        }

        private async Task<string?> SearchSteamAsync(string title)
        {
            try
            {
                var json = await Http.GetStringAsync(
                    $"https://store.steampowered.com/api/storesearch/" +
                    $"?term={Uri.EscapeDataString(title)}&l=russian&cc=RU");

                using var doc = JsonDocument.Parse(json);
                var items = doc.RootElement.GetProperty("items");

                if (items.GetArrayLength() == 0) return null;

                int appId = items[0].GetProperty("id").GetInt32();

                var posterUrl = $"https://cdn.cloudflare.steamstatic.com/steam/apps/{appId}/library_600x900.jpg";
                var fallback = $"https://cdn.cloudflare.steamstatic.com/steam/apps/{appId}/header.jpg";

                var check = await Http.SendAsync(new HttpRequestMessage(HttpMethod.Head, posterUrl));
                return check.IsSuccessStatusCode ? posterUrl : fallback;
            }
            catch { return null; }
        }

        //Токен
        private const string KinopoiskToken = "1c715d9d-322e-46eb-9bec-e07f795eff01";

        private async Task<string?> SearchKinopoiskAsync(string title)
        {
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Get,
                    $"https://kinopoiskapiunofficial.tech/api/v2.1/films/search-by-keyword" +
                    $"?keyword={Uri.EscapeDataString(title)}&page=1");

                req.Headers.Add("X-API-KEY", KinopoiskToken);

                var resp = await Http.SendAsync(req);
                if (!resp.IsSuccessStatusCode) return null;

                var json = JObject.Parse(await resp.Content.ReadAsStringAsync());

                var poster = json["films"]?[0]?["posterUrlPreview"]?.ToString()
                           ?? json["films"]?[0]?["posterUrl"]?.ToString();

                return poster?.StartsWith("http") == true ? poster : null;
            }
            catch { return null; }
        }



    }
}
