using Dapper;
using PasteBinApi.Dto;
using PasteBinApi.Interfaces;
using PasteBinApi.Models;

namespace PasteBinApi.Services;


 public class AnalyticsRepository : IAnalyticsRepository
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<AnalyticsRepository> _logger;

        public AnalyticsRepository(IDatabaseService databaseService, ILogger<AnalyticsRepository> logger)
        {
            _databaseService = databaseService;
            _logger = logger;
        }

        public async Task RecordViewAsync(PasteView view)
        {
            const string sql = @"
                INSERT INTO paste_views (id, paste_id, viewer_ip, viewer_country, viewer_city, user_agent, referer, viewed_at, session_id)
                VALUES (@Id, @PasteId, @ViewerIp, @ViewerCountry, @ViewerCity, @UserAgent, @Referer, @ViewedAt, @SessionId)";

            using var connection = await _databaseService.GetConnectionAsync();
            await connection.ExecuteAsync(sql, view);
        }

        public async Task<AnalyticsResponse> GetPasteAnalyticsAsync(Guid pasteId)
        {
            using var connection = await _databaseService.GetConnectionAsync();

            // Get total views
            var totalViews = await connection.QuerySingleAsync<long>(
                "SELECT COUNT(*) FROM paste_views WHERE paste_id = @PasteId",
                new { PasteId = pasteId });

            // Get unique views
            var uniqueViews = await connection.QuerySingleAsync<long>(
                "SELECT COUNT(DISTINCT viewer_ip) FROM paste_views WHERE paste_id = @PasteId",
                new { PasteId = pasteId });

            // Get views today
            var viewsToday = await connection.QuerySingleAsync<long>(
                "SELECT COUNT(*) FROM paste_views WHERE paste_id = @PasteId AND viewed_at >= CURRENT_DATE",
                new { PasteId = pasteId });

            // Get views this week
            var viewsThisWeek = await connection.QuerySingleAsync<long>(
                "SELECT COUNT(*) FROM paste_views WHERE paste_id = @PasteId AND viewed_at >= CURRENT_DATE - INTERVAL '7 days'",
                new { PasteId = pasteId });

            return new AnalyticsResponse
            {
                TotalViews = totalViews,
                UniqueViews = uniqueViews,
                ViewsToday = viewsToday,
                ViewsThisWeek = viewsThisWeek,
                ViewsByDay = await GetViewsByDayAsync(pasteId),
                TopReferrers = await GetTopReferrersAsync(pasteId)
            };
        }

        public async Task<Dictionary<string, long>> GetViewsByDayAsync(Guid pasteId, int days = 30)
        {
            const string sql = @"
                SELECT DATE(viewed_at) as day, COUNT(*) as views
                FROM paste_views 
                WHERE paste_id = @PasteId 
                AND viewed_at >= CURRENT_DATE - INTERVAL '@Days days'
                GROUP BY DATE(viewed_at)
                ORDER BY day DESC";

            using var connection = await _databaseService.GetConnectionAsync();
            var results = await connection.QueryAsync<dynamic>(sql, new { PasteId = pasteId, Days = days });

            return results.ToDictionary(
                x => ((DateTime)x.day).ToString("yyyy-MM-dd"),
                x => (long)x.views
            );
        }

        public async Task<Dictionary<string, long>> GetTopReferrersAsync(Guid pasteId, int limit = 10)
        {
            const string sql = @"
                SELECT COALESCE(referer, 'Direct') as referer, COUNT(*) as views
                FROM paste_views 
                WHERE paste_id = @PasteId 
                GROUP BY referer
                ORDER BY views DESC
                LIMIT @Limit";

            using var connection = await _databaseService.GetConnectionAsync();
            var results = await connection.QueryAsync<dynamic>(sql, new { PasteId = pasteId, Limit = limit });

            return results.ToDictionary(
                x => (string)x.referer,
                x => (long)x.views
            );
        }
    }
