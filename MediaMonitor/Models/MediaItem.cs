namespace MediaMonitor.Models;

public class MediaItem
{
   
    public Guid Id { get; set; } = Guid.NewGuid();
    private string _title = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
    public MediaType Type { get; set; }
    private MediaStatus _status;
    private double _rating;
    private int _rewatchCount;
    private string _comment = string.Empty;
    public DateTime DateAdded { get; set; } = DateTime.Now;
    private DateTime? _completionDate;
    public List<string> Tags { get; set; } = new();
    public bool IsFavorite { get; set; }


    public string Title
    {
        get => _title;
        set => _title = value ?? string.Empty;
    }

    public MediaStatus Status
    {
        get => _status;
        set
        {
            _status = value;

            if (_status == MediaStatus.Completed && _completionDate == null)
            {
                _completionDate = DateTime.Now;
            }
            else if (_status != MediaStatus.Completed)
            {
                _completionDate = null;
            }
        }
    }


    public double Rating
    {
        get => _rating;
        set => _rating = Math.Clamp(value, 0, 10);
    }

    public int RewatchCount
    {
        get => _rewatchCount;
        set => _rewatchCount = Math.Max(0, value);
    }

    public string Comment
    {
        get => _comment;
        set
        {
            string safe = value ?? string.Empty;

            if (safe.Length > 1000)
            {
                _comment = safe.Substring(0, 1000);
            }
            else
            {
                _comment = safe;
            }

        }
    }

    public DateTime? CompletionDate
    {
        get => _completionDate;
        set
        {
            if(_status == MediaStatus.Completed)
                _completionDate = value;
        }
    }

    public MediaItem()
    {
        DateAdded = DateTime.Now;
    }
}

