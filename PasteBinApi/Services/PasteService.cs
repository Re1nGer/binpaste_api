using System.Text;
using PasteBinApi.Dto;
using PasteBinApi.Interfaces;
using PasteBinApi.Models;

namespace PasteBinApi.Services;

 public class PasteService : IPasteService
    {
        private readonly IPasteRepository _pasteRepository;
        private readonly IHashGeneratorService _hashGenerator;
        private readonly ILogger<PasteService> _logger;

        public PasteService(
            IPasteRepository pasteRepository,
            IHashGeneratorService hashGenerator,
            ILogger<PasteService> logger)
        {
            _pasteRepository = pasteRepository;
            _hashGenerator = hashGenerator;
            _logger = logger;
        }

        public async Task<PasteResponse> CreatePasteAsync(CreatePasteRequest request, string? clientIp = null)
        {
            try
            {
                var paste = new Paste
                {
                    Id = Guid.NewGuid(),
                    Title = request.Title,
                    Content = request.Content,
                    Language = request.Language,
                    IsPrivate = request.IsPrivate,
                    BurnAfterRead = request.BurnAfterRead,
                    ExpiresAt = request.ExpiresAt,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    SizeBytes = Encoding.UTF8.GetByteCount(request.Content),
                    Tags = request.Tags,
                    ContentHash = _hashGenerator.GenerateContentHash(request.Content)
                };

                // Generate unique short ID
                paste.ShortId = await _hashGenerator.GenerateUniqueShortIdAsync(request.Content);

                var createdPaste = await _pasteRepository.CreateAsync(paste);

                return new PasteResponse
                {
                    Id = createdPaste.Id,
                    ShortId = createdPaste.ShortId,
                    Title = createdPaste.Title,
                    Content = createdPaste.Content,
                    Language = createdPaste.Language,
                    IsPrivate = createdPaste.IsPrivate,
                    ExpiresAt = createdPaste.ExpiresAt,
                    CreatedAt = createdPaste.CreatedAt,
                    ViewCount = createdPaste.ViewCount,
                    DownloadCount = createdPaste.DownloadCount,
                    SizeBytes = createdPaste.SizeBytes,
                    BurnAfterRead = createdPaste.BurnAfterRead,
                    Tags = createdPaste.Tags,
                    Url = $"/api/v1/pastes/{createdPaste.ShortId}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create paste");
                throw;
            }
        }

        public async Task<PasteResponse?> GetPasteAsync(string shortId, string? password = null)
        {
            var paste = await _pasteRepository.GetByShortIdAsync(shortId);
            
            if (paste == null) return null;

            // Handle burn after read
            if (paste.BurnAfterRead)
            {
                // Burn the paste after returning it
                _ = Task.Run(async () => await _pasteRepository.BurnPasteAsync(paste.Id));
            }

            return new PasteResponse
            {
                Id = paste.Id,
                ShortId = paste.ShortId,
                Title = paste.Title,
                Content = paste.Content,
                Language = paste.Language,
                IsPrivate = paste.IsPrivate,
                ExpiresAt = paste.ExpiresAt,
                CreatedAt = paste.CreatedAt,
                ViewCount = paste.ViewCount,
                DownloadCount = paste.DownloadCount,
                SizeBytes = paste.SizeBytes,
                BurnAfterRead = paste.BurnAfterRead,
                Tags = paste.Tags,
                Url = $"/p/{paste.ShortId}"
            };
        }

        public async Task<IEnumerable<PasteListResponse>> GetRecentPublicPastesAsync(int limit = 10)
        {
            var pastes = await _pasteRepository.GetRecentPublicAsync(limit);
            
            return pastes.Select(p => new PasteListResponse
            {
                Id = p.Id,
                ShortId = p.ShortId,
                Title = p.Title,
                Language = p.Language,
                CreatedAt = p.CreatedAt,
                ViewCount = p.ViewCount,
                SizeBytes = p.SizeBytes,
                IsPrivate = p.IsPrivate
            });
        }

        public async Task<IEnumerable<PasteListResponse>> SearchPastesAsync(string query, string? language = null, int limit = 20, int offset = 0)
        {
            var pastes = await _pasteRepository.SearchAsync(query, language, limit, offset);
            
            return pastes.Select(p => new PasteListResponse
            {
                Id = p.Id,
                ShortId = p.ShortId,
                Title = p.Title,
                Language = p.Language,
                CreatedAt = p.CreatedAt,
                ViewCount = p.ViewCount,
                SizeBytes = p.SizeBytes,
                IsPrivate = p.IsPrivate
            });
        }

        public async Task DeletePasteAsync(string shortId)
        {
            var paste = await _pasteRepository.GetByShortIdAsync(shortId);
            if (paste == null)
            {
                throw new KeyNotFoundException("Paste not found");
            }

            await _pasteRepository.DeleteAsync(paste.Id);
        }

        public async Task<string> GetRawContentAsync(string shortId)
        {
            var paste = await _pasteRepository.GetByShortIdAsync(shortId);
            if (paste == null)
            {
                throw new KeyNotFoundException("Paste not found");
            }

            return paste.Content;
        }

        public async Task<(string content, string filename)> GetDownloadAsync(string shortId)
        {
            var paste = await _pasteRepository.GetByShortIdAsync(shortId);
            if (paste == null)
            {
                throw new KeyNotFoundException("Paste not found");
            }

            var extension = GetFileExtension(paste.Language);
            var filename = $"{paste.ShortId}.{extension}";

            return (paste.Content, filename);
        }

        private static string GetFileExtension(string language)
        {
            return language switch
            {
                "javascript" => "js",
                "typescript" => "ts",
                "python" => "py",
                "java" => "java",
                "cpp" => "cpp",
                "html" => "html",
                "css" => "css",
                "json" => "json",
                "xml" => "xml",
                "sql" => "sql",
                "bash" => "sh",
                "php" => "php",
                "ruby" => "rb",
                "go" => "go",
                _ => "txt"
            };
        }
    }
