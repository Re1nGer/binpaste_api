using System.Data;

namespace PasteBinApi.Interfaces;

public interface IDatabaseService
{
    Task InitializeDatabaseAsync();
    Task<IDbConnection> GetConnectionAsync();
}