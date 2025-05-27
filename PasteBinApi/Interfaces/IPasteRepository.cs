using PasteBinApi.Models;

namespace PasteBinApi.Interfaces;


public interface IPasteRepository
{
    Task<Paste> CreateAsync(Paste paste);
    Task<Paste?> GetByShortIdAsync(string shortId);
    Task<Paste?> GetByIdAsync(Guid id);
    Task<IEnumerable<Paste>> GetRecentPublicAsync(int limit = 10);
    Task<bool> ShortIdExistsAsync(string shortId);
    Task UpdateViewCountAsync(Guid id);
    Task BurnPasteAsync(Guid id);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<Paste>> SearchAsync(string query, string? language = null, int limit = 20, int offset = 0);
}
