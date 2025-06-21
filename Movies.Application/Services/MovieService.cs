using FluentValidation;
using Movies.Application.Models;
using Movies.Application.Repositories;
using Movies.Application.Validators;

namespace Movies.Application.Services;

public class MovieService(IMovieRepository movieRepository, MovieValidator movieValidator) : IMovieService
{
    public async Task<bool> CreateAsync(Movie movie, CancellationToken cancellationToken = default)
    {
        await movieValidator.ValidateAndThrowAsync(movie, cancellationToken);
        return await movieRepository.CreateAsync(movie, cancellationToken);
    }

    public Task<Movie?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return movieRepository.GetByIdAsync(id, cancellationToken);
    }

    public Task<Movie?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return movieRepository.GetBySlugAsync(slug, cancellationToken);
    }

    public Task<IEnumerable<Movie>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return movieRepository.GetAllAsync(cancellationToken);
    }

    public async Task<Movie?> UpdateAsync(Movie movie, CancellationToken cancellationToken = default)
    {
        await movieValidator.ValidateAndThrowAsync(movie, cancellationToken);
        var exists = await movieRepository.ExistsByIdAsync(movie.Id, cancellationToken);
        if (!exists)
        {
            return null;
        }
        await movieRepository.UpdateAsync(movie, cancellationToken);
        return movie;
    }

    public Task<bool> DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return movieRepository.DeleteByIdAsync(id, cancellationToken);
    }
}