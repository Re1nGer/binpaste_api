using System.ComponentModel.DataAnnotations;

namespace PasteBinApi.Dto;


public class CreatePasteRequest
    {
        public string? Title { get; set; }
        
        [Required]
        [StringLength(1000000, MinimumLength = 1)]
        public string Content { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string Language { get; set; } = "text";
        
        public bool IsPrivate { get; set; } = false;
        public bool BurnAfterRead { get; set; } = false;
        public string? Password { get; set; }
        public long? ExpiresAfterInMinutes { get; set; }
        public string[] Tags { get; set; } = Array.Empty<string>();
    }

    public class PasteResponse
    {
        public Guid Id { get; set; }
        public string ShortId { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Language { get; set; } = "text";
        public bool IsPrivate { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public long ViewCount { get; set; }
        public long DownloadCount { get; set; }
        public int SizeBytes { get; set; }
        public bool BurnAfterRead { get; set; }
        public string[] Tags { get; set; } = Array.Empty<string>();
        public string Url { get; set; } = string.Empty;
        public string? ExpiresInMinutes { get; set; }
    }

    public class PasteListResponse
    {
        public Guid Id { get; set; }
        public string ShortId { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string Content { get; set; }
        public string Language { get; set; } = "text";
        public DateTime CreatedAt { get; set; }
        public long ViewCount { get; set; }
        public int SizeBytes { get; set; }
        public bool IsPrivate { get; set; }
    }

    public class AnalyticsResponse
    {
        public long TotalViews { get; set; }
        public long UniqueViews { get; set; }
        public long ViewsToday { get; set; }
        public long ViewsThisWeek { get; set; }
        public Dictionary<string, long> ViewsByDay { get; set; } = new();
        public Dictionary<string, long> ViewsByCountry { get; set; } = new();
        public Dictionary<string, long> TopReferrers { get; set; } = new();
    }
