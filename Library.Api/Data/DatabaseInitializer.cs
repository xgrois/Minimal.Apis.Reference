using Dapper;

namespace Library.Api.Data;

public class DatabaseInitializer
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public DatabaseInitializer(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task InitializeAsync()
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync();

        await connection.ExecuteAsync(
            @"CREATE TABLE IF NOT EXISTS Books (Isbn TEXT PRIMARY KEY, Title TEXT NOT NULL, Author TEXT NOT NULL, ShortDescription TEXT NOT NULL, PageCount INTEGER, ReleaseDate TEXT NOT NULL)"
        );
    }
}