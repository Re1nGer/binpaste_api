using System.Text.Json;
using Dapper;
using PasteBinApi.Interfaces;
using PasteBinApi.Models;

namespace PasteBinApi.Services;

public class PasteRepository : IPasteRepository
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<PasteRepository> _logger;

        public PasteRepository(IDatabaseService databaseService, ILogger<PasteRepository> logger)
        {
            _databaseService = databaseService;
            _logger = logger;
        }

        public async Task<Paste> CreateAsync(Paste paste)
        {
            const string sql = @"
                INSERT INTO pastes (
                    id, short_id, title, content, content_hash, language, is_private, 
                    password_hash, expires_at, created_at, updated_at, size_bytes, 
                    burn_after_read, tags, metadata
                ) VALUES (
                    @Id, @ShortId, @Title, @Content, @ContentHash, @Language, @IsPrivate,
                    @PasswordHash, @ExpiresAt, @CreatedAt, @UpdatedAt, @SizeBytes,
                    @BurnAfterRead, @Tags, @Metadata::jsonb
                ) RETURNING *";

            using var connection = await _databaseService.GetConnectionAsync();
            
            var result = await connection.QuerySingleAsync<dynamic>(sql, new
            {
                paste.Id,
                paste.ShortId,
                paste.Title,
                paste.Content,
                paste.ContentHash,
                paste.Language,
                paste.IsPrivate,
                paste.PasswordHash,
                paste.ExpiresAt,
                paste.CreatedAt,
                paste.UpdatedAt,
                paste.SizeBytes,
                paste.BurnAfterRead,
                Tags = paste.Tags,
                Metadata = JsonSerializer.Serialize(paste.Metadata)
            });

            return MapToPaste(result);
        }

        public async Task<Paste?> GetByShortIdAsync(string shortId)
        {
            const string sql = @"
                SELECT * FROM pastes 
                WHERE short_id = @ShortId 
                AND (expires_at IS NULL OR expires_at > NOW()) 
                AND is_burned = FALSE";

            using var connection = await _databaseService.GetConnectionAsync();
            var result = await connection.QuerySingleOrDefaultAsync<dynamic>(sql, new { ShortId = shortId });
            
            return result != null ? MapToPaste(result) : null;
        }

        public async Task<Paste?> GetByIdAsync(Guid id)
        {
            const string sql = "SELECT * FROM pastes WHERE id = @Id";

            using var connection = await _databaseService.GetConnectionAsync();
            var result = await connection.QuerySingleOrDefaultAsync<dynamic>(sql, new { Id = id });
            
            return result != null ? MapToPaste(result) : null;
        }

        public async Task<IEnumerable<Paste>> GetRecentPublicAsync(int limit = 10)
        {
            const string sql = @"
                SELECT id, content, short_id, title, language, created_at, view_count, size_bytes, is_private
                FROM pastes 
                WHERE is_private = FALSE 
                AND (expires_at IS NULL OR expires_at > NOW()) 
                AND is_burned = FALSE
                ORDER BY created_at DESC 
                LIMIT @Limit";

            using var connection = await _databaseService.GetConnectionAsync();
            var results = await connection.QueryAsync<dynamic>(sql, new { Limit = limit });
            
            return results.Select(MapToPasteListItem);
        }

        public async Task<bool> ShortIdExistsAsync(string shortId)
        {
            const string sql = "SELECT EXISTS(SELECT 1 FROM pastes WHERE short_id = @ShortId)";

            using var connection = await _databaseService.GetConnectionAsync();
            return await connection.QuerySingleAsync<bool>(sql, new { ShortId = shortId });
        }

        public async Task UpdateViewCountAsync(Guid id)
        {
            const string sql = "UPDATE pastes SET view_count = view_count + 1 WHERE id = @Id";

            using var connection = await _databaseService.GetConnectionAsync();
            await connection.ExecuteAsync(sql, new { Id = id });
        }

        public async Task BurnPasteAsync(Guid id)
        {
            const string sql = "UPDATE pastes SET is_burned = TRUE WHERE id = @Id";

            using var connection = await _databaseService.GetConnectionAsync();
            await connection.ExecuteAsync(sql, new { Id = id });
        }

        public async Task DeleteAsync(Guid id)
        {
            const string sql = "DELETE FROM pastes WHERE id = @Id";

            using var connection = await _databaseService.GetConnectionAsync();
            await connection.ExecuteAsync(sql, new { Id = id });
        }

        public async Task<IEnumerable<Paste>> SearchAsync(string query, string? language = null, int limit = 20, int offset = 0)
        {
            var sql = @"
                SELECT id, short_id, title, language, created_at, view_count, size_bytes, is_private
                FROM pastes 
                WHERE is_private = FALSE 
                AND (expires_at IS NULL OR expires_at > NOW()) 
                AND is_burned = FALSE
                AND (title ILIKE @Query OR content ILIKE @Query)";

            if (!string.IsNullOrEmpty(language))
            {
                sql += " AND language = @Language";
            }

            sql += " ORDER BY created_at DESC LIMIT @Limit OFFSET @Offset";

            using var connection = await _databaseService.GetConnectionAsync();
            var results = await connection.QueryAsync<dynamic>(sql, new 
            { 
                Query = $"%{query}%", 
                Language = language, 
                Limit = limit, 
                Offset = offset 
            });
            
            return results.Select(MapToPasteListItem);
        }

        private Paste MapToPaste(dynamic result)
        {
            return new Paste
            {
                Id = result.id,
                ShortId = result.short_id,
                Title = result.title,
                Content = result.content,
                ContentHash = result.content_hash,
                Language = result.language,
                IsPrivate = result.is_private,
                PasswordHash = result.password_hash,
                ExpiresAt = result.expires_at,
                CreatedAt = result.created_at,
                UpdatedAt = result.updated_at,
                ViewCount = result.view_count,
                DownloadCount = result.download_count,
                SizeBytes = result.size_bytes,
                BurnAfterRead = result.burn_after_read,
                IsBurned = result.is_burned,
                Tags = result.tags ?? Array.Empty<string>(),
                Metadata = string.IsNullOrEmpty(result.metadata?.ToString()) 
                    ? new Dictionary<string, object>() 
                    : JsonSerializer.Deserialize<Dictionary<string, object>>(result.metadata.ToString())
            };
        }

        private Paste MapToPasteListItem(dynamic result)
        {
            return new Paste
            {
                Id = result.id,
                ShortId = result.short_id,
                Title = result.title,
                Language = result.language,
                CreatedAt = result.created_at,
                ViewCount = result.view_count,
                SizeBytes = result.size_bytes,
                IsPrivate = result.is_private,
                Content = result.content,
            };
        }
    }
