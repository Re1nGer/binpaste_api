using PasteBinApi.Dto;

namespace PasteBinApi.Interfaces;


public interface IPasteService
{
    Task<PasteResponse> CreatePasteAsync(CreatePasteRequest request, string? clientIp = null);
    Task<PasteResponse?> GetPasteAsync(string shortId, string? password = null);
    Task<IEnumerable<PasteListResponse>> GetRecentPublicPastesAsync(int limit = 10);
    Task<IEnumerable<PasteListResponse>> SearchPastesAsync(string query, string? language = null, int limit = 20, int offset = 0);
    Task DeletePasteAsync(string shortId);
    Task<string> GetRawContentAsync(string shortId);
    Task<(string content, string filename)> GetDownloadAsync(string shortId);
}
