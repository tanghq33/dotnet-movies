using Dapper;
using Movies.Application.Database;
using Movies.Application.Models;

namespace Movies.Application.Repositories;

public class MovieRepository : IMovieRepository
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public MovieRepository(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<bool> CreateAsync(Movie movie, CancellationToken token = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);
        using var transaction = connection.BeginTransaction();

        var result = await connection.ExecuteAsync(
            new CommandDefinition(
                """
                    INSERT INTO movies (id, title, slug, yearofrelease)
                    VALUES (@Id, @Title, @Slug, @YearOfRelease)
                """,
                movie,
                cancellationToken: token
            )
        );

        if (result > 0)
        {
            foreach (var genre in movie.Genres)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                            INSERT INTO genres (movieid, name)
                            VALUES (@MovieId, @Name)
                        """,
                        new { MovieId = movie.Id, Name = genre },
                        cancellationToken: token
                    )
                );
            }
        }

        transaction.Commit();

        bool created = result > 0;

        return created;
    }

    public async Task<IEnumerable<Movie>> GetAllAsync(CancellationToken token = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);
        var result = await connection.QueryAsync(
           new CommandDefinition(
               """
                    SELECT m.*, string_agg(g.name, ',') AS genres
                    FROM movies m LEFT JOIN genres g
                    ON m.id = g.movieid
                    GROUP BY id
                """,
                cancellationToken: token
           )
        );

        return result.Select(x => new Movie
        {
            Id = x.id,
            Title = x.title,
            YearOfRelease = x.yearofrelease,
            Genres = Enumerable.ToList(x.genres.Split(','))
        });
    }

    public async Task<Movie?> GetByIdAsync(Guid id, CancellationToken token = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);
        var movie = await connection.QuerySingleOrDefaultAsync<Movie>(
            new CommandDefinition(
                """
                    SELECT * FROM movies WHERE id = @Id
                """,
                new { id },
                cancellationToken: token
            )
        );

        if (movie is null)
        {
            return null;
        }

        var genres = await connection.QueryAsync<string>(
            new CommandDefinition(
                """
                    SELECT name FROM genres WHERE movieid = @id
                """,
                new { id },
                cancellationToken: token
            )
        );

        foreach (var genre in genres)
        {
            movie.Genres.Add(genre);
        }

        return movie;
    }

    public async Task<Movie?> GetBySlugAsync(string slug, CancellationToken token = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);
        var movie = await connection.QuerySingleOrDefaultAsync<Movie>(
            new CommandDefinition(
                """
                    SELECT * FROM movies WHERE slug = @slug
                """,
                new { slug },
                cancellationToken: token
            )
        );

        if (movie is null)
        {
            return null;
        }

        var genres = await connection.QueryAsync<string>(
            new CommandDefinition(
                """
                    SELECT name FROM genres WHERE movieid = @id
                """,
                new { id = movie.Id },
                cancellationToken: token
            )
        );

        foreach (var genre in genres)
        {
            movie.Genres.Add(genre);
        }

        return movie;
    }

    /// <summary>
    /// Question:
    /// Why there is no result check for deleting existing genre and inserting new genres?
    /// What if the the delete and insert process failed?
    /// </summary>
    /// <param name="movie"></param>
    /// <returns></returns>
    public async Task<bool> UpdateAsync(Movie movie, CancellationToken token = default)
    {
        var connection = await _dbConnectionFactory.CreateConnectionAsync(token);
        using var transaction = connection.BeginTransaction();

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                    DELETE FROM genres WHERE movieid = @id
                """,
                new { id = movie.Id },
                cancellationToken: token
            )
        );

        foreach (var genre in movie.Genres)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                        INSERT INTO genres (movieid, name)
                        VALUES (@MovieId, @Name)
                    """,
                    new { MovieId = movie.Id, Name = genre },
                    cancellationToken: token
                )
            );
        }

        var result = await connection.ExecuteAsync(
            new CommandDefinition(
                """
                    UPDATE movies SET slug = @Slug, title = @Title, yearofrelease = @YearOfRelease
                    WHERE id = @Id
                """,
                movie,
                cancellationToken: token
            )
        );

        transaction.Commit();

        return result > 0;
    }

    public async Task<bool> DeleteByIdAsync(Guid id, CancellationToken token = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);
        using var transaction = connection.BeginTransaction();

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                    DELETE FROM genres WHERE movieid = @id
                """,
                new { id },
                cancellationToken: token
            )
        );

        var result = await connection.ExecuteAsync(
            new CommandDefinition(
                """
                    DELETE FROM movies WHERE id = @Id
                """,
                new { id },
                cancellationToken: token
            )
        );

        transaction.Commit();

        return result > 0;
    }

    public async Task<bool> ExistsByIdAsync(Guid id, CancellationToken token = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                """
                SELECT count(1) FROM movies WHERE id = @Id
                """,
                new { id },
                cancellationToken: token
            )
        );
    }
}
