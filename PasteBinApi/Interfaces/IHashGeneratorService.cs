namespace PasteBinApi.Interfaces;

public interface IHashGeneratorService
{
    Task<string> GenerateUniqueShortIdAsync(string content, string algorithm = "base62", int length = 8);
    string GenerateContentHash(string content);
}