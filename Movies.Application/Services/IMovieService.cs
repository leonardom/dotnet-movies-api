using Movies.Application.Models;

namespace Movies.Application.Services;

public interface IMovieService
{
    Task<bool> CreateAsync(Movie movie, CancellationToken cancellationToken = default);
    Task<Movie?> GetByIdAsync(Guid id, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<Movie?> GetBySlugAsync(string slug, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Movie>> GetAllAsync(GetAllMoviesOptions options, CancellationToken cancellationToken = default);
    Task<Movie?> UpdateAsync(Movie movie, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<bool> DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(string? title, int? yearOfRelease, CancellationToken cancellationToken = default);
}