using Dapper;

namespace Movies.Application.Database;

public class DbInitializer
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public DbInitializer(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task InitializeAsync()
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync();

        var result = await connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS movies (
                id UUID PRIMARY KEY,
                title TEXT NOT NULL,
                slug TEXT NOT NULL,
                yearofrelease INTEGER NOT NULL);
        """);

        result = await connection.ExecuteAsync("""
            CREATE UNIQUE INDEX CONCURRENTLY IF NOT EXISTS movies_slug_idx
            ON movies 
            using btree(slug);
        """);
    }
}
