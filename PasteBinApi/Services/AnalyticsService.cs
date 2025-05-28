using PasteBinApi.Dto;
using PasteBinApi.Interfaces;
using PasteBinApi.Models;

namespace PasteBinApi.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly IAnalyticsRepository _analyticsRepository;
    private readonly IPasteRepository _pasteRepository;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(
        IAnalyticsRepository analyticsRepository,
        IPasteRepository pasteRepository,
        ILogger<AnalyticsService> logger)
    {
        _analyticsRepository = analyticsRepository;
        _pasteRepository = pasteRepository;
        _logger = logger;
    }

    public async Task RecordViewAsync(Guid pasteId, string? clientIp, string? userAgent, string? referer, string? sessionId = null)
    {
        try
        {
            var view = new PasteView
            {
                Id = Guid.NewGuid(),
                PasteId = pasteId,
                UserAgent = userAgent,
                Referer = referer,
                ViewedAt = DateTime.UtcNow,
                SessionId = sessionId ?? Guid.NewGuid().ToString()
            };

            // Record the view
            await _analyticsRepository.RecordViewAsync(view);

            // Update paste view count
            await _pasteRepository.UpdateViewCountAsync(pasteId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record view for paste {PasteId}", pasteId);
            // Don't throw - analytics shouldn't break the main flow
        }
    }

    public async Task<AnalyticsResponse> GetPasteAnalyticsAsync(Guid pasteId)
    {
        return await _analyticsRepository.GetPasteAnalyticsAsync(pasteId);
    }
}
