using FluentValidation;
using Movies.Application.Models;
using Movies.Application.Repositories;
using Movies.Application.Validators;

namespace Movies.Application.Services;

public class MovieService(
    IMovieRepository movieRepository,
    IRatingRepository ratingRepository,
    MovieValidator movieValidator,
    GetAllMoviesOptionsValidator getAllMoviesOptionsValidator) : IMovieService
{
    public async Task<bool> CreateAsync(Movie movie, CancellationToken cancellationToken = default)
    {
        await movieValidator.ValidateAndThrowAsync(movie, cancellationToken);
        return await movieRepository.CreateAsync(movie, cancellationToken);
    }

    public Task<Movie?> GetByIdAsync(Guid id, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        return movieRepository.GetByIdAsync(id, userId, cancellationToken);
    }

    public Task<Movie?> GetBySlugAsync(string slug, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        return movieRepository.GetBySlugAsync(slug, userId, cancellationToken);
    }

    public async Task<IEnumerable<Movie>> GetAllAsync(GetAllMoviesOptions options, CancellationToken cancellationToken = default)
    {
        await getAllMoviesOptionsValidator.ValidateAndThrowAsync(options, cancellationToken);
        return await movieRepository.GetAllAsync(options, cancellationToken);
    }

    public async Task<Movie?> UpdateAsync(Movie movie, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        await movieValidator.ValidateAndThrowAsync(movie, cancellationToken);
        var exists = await movieRepository.ExistsByIdAsync(movie.Id, cancellationToken);
        if (!exists)
        {
            return null;
        }
        await movieRepository.UpdateAsync(movie, cancellationToken);
        if (!userId.HasValue)
        {
            movie.Rating = await ratingRepository.GetRatingAsync(movie.Id, cancellationToken);
            return movie;
        }
        var ratings = await ratingRepository.GetRatingAsync(movie.Id, userId.Value, cancellationToken);
        movie.Rating = ratings.Rating;
        movie.UserRating = ratings.UserRating;
        return movie;
    }

    public Task<bool> DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return movieRepository.DeleteByIdAsync(id, cancellationToken);
    }

    public Task<int> GetCountAsync(string? title, int? yearOfRelease, CancellationToken cancellationToken = default)
    {
        return movieRepository.GetCountAsync(title, yearOfRelease, cancellationToken);
    }
}