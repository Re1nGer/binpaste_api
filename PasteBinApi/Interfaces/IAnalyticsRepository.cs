using PasteBinApi.Dto;
using PasteBinApi.Models;

namespace PasteBinApi.Interfaces;


public interface IAnalyticsRepository
{
    Task RecordViewAsync(PasteView view);
    Task<AnalyticsResponse> GetPasteAnalyticsAsync(Guid pasteId);
    Task<Dictionary<string, long>> GetViewsByDayAsync(Guid pasteId, int days = 30);
    Task<Dictionary<string, long>> GetTopReferrersAsync(Guid pasteId, int limit = 10);
}
