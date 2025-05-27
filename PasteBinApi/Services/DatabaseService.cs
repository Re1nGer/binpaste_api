using System.Data;
using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;
using PasteBinApi.Interfaces;

namespace PasteBinApi.Services;

public class DatabaseService : IDatabaseService
    {
        private readonly IConfiguration _settings;
        private readonly ILogger<DatabaseService> _logger;

        public DatabaseService(ILogger<DatabaseService> logger, IConfiguration settings)
        {
            _logger = logger;
            _settings = settings;
        }

        public async Task<IDbConnection> GetConnectionAsync()
        {
            var connection = new NpgsqlConnection(_settings.GetConnectionString("PasteBin"));
            await connection.OpenAsync();
            return connection;
        }

        public async Task InitializeDatabaseAsync()
        {
            try
            {
                using var connection = await GetConnectionAsync();
                await CreateTablesAsync(connection);
                await CreateIndexesAsync(connection);
                _logger.LogInformation("Database initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize database");
                throw;
            }
        }

        private async Task CreateTablesAsync(IDbConnection connection)
        {
            var sql = @"
                CREATE EXTENSION IF NOT EXISTS ""uuid-ossp"";

                CREATE TABLE IF NOT EXISTS pastes (
                    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
                    short_id VARCHAR(12) UNIQUE NOT NULL,
                    title VARCHAR(255),
                    content TEXT NOT NULL,
                    content_hash VARCHAR(64) NOT NULL,
                    language VARCHAR(50) DEFAULT 'text',
                    is_private BOOLEAN DEFAULT FALSE,
                    password_hash VARCHAR(255),
                    expires_at TIMESTAMP,
                    created_at TIMESTAMP DEFAULT NOW(),
                    updated_at TIMESTAMP DEFAULT NOW(),
                    view_count BIGINT DEFAULT 0,
                    download_count BIGINT DEFAULT 0,
                    size_bytes INTEGER NOT NULL,
                    ip_address INET,
                    burn_after_read BOOLEAN DEFAULT FALSE,
                    is_burned BOOLEAN DEFAULT FALSE,
                    tags TEXT[],
                    metadata JSONB DEFAULT '{}'
                );

                CREATE TABLE IF NOT EXISTS paste_views (
                    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
                    paste_id UUID NOT NULL REFERENCES pastes(id) ON DELETE CASCADE,
                    viewer_ip INET,
                    viewer_country VARCHAR(2),
                    viewer_city VARCHAR(100),
                    user_agent TEXT,
                    referer TEXT,
                    viewed_at TIMESTAMP DEFAULT NOW(),
                    session_id VARCHAR(64)
                );
            ";

            await connection.ExecuteAsync(sql);
        }

        private async Task CreateIndexesAsync(IDbConnection connection)
        {
            var sql = @"
                CREATE INDEX IF NOT EXISTS idx_pastes_short_id ON pastes(short_id);
                CREATE INDEX IF NOT EXISTS idx_pastes_created_at ON pastes(created_at DESC);
                CREATE INDEX IF NOT EXISTS idx_pastes_expires_at ON pastes(expires_at) WHERE expires_at IS NOT NULL;
                CREATE INDEX IF NOT EXISTS idx_paste_views_paste_id ON paste_views(paste_id);
                CREATE INDEX IF NOT EXISTS idx_paste_views_viewed_at ON paste_views(viewed_at DESC);
                CREATE INDEX IF NOT EXISTS idx_pastes_language ON pastes(language);
                CREATE INDEX IF NOT EXISTS idx_pastes_is_private ON pastes(is_private);
            ";

            await connection.ExecuteAsync(sql);
        }
    }
