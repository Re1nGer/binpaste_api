namespace PasteBinApi.Models;


public class Paste
{
    public Guid Id { get; set; }
    public string ShortId { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string Content { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public string Language { get; set; } = "text";
    public bool IsPrivate { get; set; } = false;
    public string? PasswordHash { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ExpiresInMinutes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long ViewCount { get; set; } = 0;
    public long DownloadCount { get; set; } = 0;
    public int SizeBytes { get; set; }
    public bool BurnAfterRead { get; set; } = false;
    public bool IsBurned { get; set; } = false;
    public string[] Tags { get; set; } = [];
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class PasteView
{
    public Guid Id { get; set; }
    public Guid PasteId { get; set; }
    public string? ViewerIp { get; set; }
    public string? ViewerCountry { get; set; }
    public string? ViewerCity { get; set; }
    public string? UserAgent { get; set; }
    public string? Referer { get; set; }
    public DateTime ViewedAt { get; set; }
    public string? SessionId { get; set; }
}
