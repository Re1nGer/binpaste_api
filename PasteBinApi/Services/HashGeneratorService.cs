using System.Security.Cryptography;
using System.Text;
using PasteBinApi.Interfaces;

namespace PasteBinApi.Services;

public class HashGeneratorService : IHashGeneratorService
    {
        private readonly IPasteRepository _pasteRepository;
        private readonly ILogger<HashGeneratorService> _logger;

        public HashGeneratorService(IPasteRepository pasteRepository, ILogger<HashGeneratorService> logger)
        {
            _pasteRepository = pasteRepository;
            _logger = logger;
        }

        public async Task<string> GenerateUniqueShortIdAsync(string content, string algorithm = "base62", int length = 8)
        {
            for (int attempt = 0; attempt < 5; attempt++)
            {
                var id = GenerateBase62Id(content, length, attempt);

                var exists = await _pasteRepository.ShortIdExistsAsync(id);
                
                if (!exists)
                {
                    return id;
                }
            }

            throw new InvalidOperationException("Failed to generate unique short ID after multiple attempts");
        }

        public string GenerateContentHash(string content)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
            return Convert.ToHexString(hash).ToLower();
        }

        private string GenerateBase62Id(string content, int length, int salt)
        {
            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            var input = $"{content}{DateTime.UtcNow.Ticks}{salt}";
            
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            
            var result = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                var index = hash[i % hash.Length] % chars.Length;
                result.Append(chars[index]);
            }
            
            return result.ToString();
        }
    }
