using PasteBinApi.Dto;

namespace PasteBinApi.Interfaces;

public interface IAnalyticsService
{
    Task RecordViewAsync(Guid pasteId, string? clientIp, string? userAgent, string? referer, string? sessionId = null);
    Task<AnalyticsResponse> GetPasteAnalyticsAsync(Guid pasteId);
}