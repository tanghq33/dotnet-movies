namespace Movies.Contracts;

public class MoviesResponse
{
    public required IEnumerable<MoviesResponse> Items { get; init; } = Enumerable.Empty<MoviesResponse>();
}
