using System.Data;
using Dapper;
using Movies.Application.Database;
using Movies.Application.Models;

namespace Movies.Application.Repositories;

public class RatingRepository(IDbConnectionFactory connectionFactory) : IRatingRepository
{
    public async Task<bool> RateMovieAsync(Guid movieId, Guid userId, int rating, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateDbConnectionAsync(cancellationToken);
        
        const string sql = """
                               INSERT INTO ratings(user_id, movie_id, rating)
                                   VALUES (@userId, @movieId, @rating)
                                   ON CONFLICT (user_id, movie_id) DO UPDATE 
                                       SET rating = @rating;
                           """;
        var result = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { userId, movieId, rating }, cancellationToken: cancellationToken));
        
        return result > 0;
    }

    public async Task<float?> GetRatingAsync(Guid movieId, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateDbConnectionAsync(cancellationToken);

        const string sql = """
                               SELECT 
                                 round(avg(rating), 1)::float8 as rating 
                               FROM 
                                   ratings
                               WHERE
                                   movie_id = @movieId
                           """;

        return await connection.QuerySingleOrDefaultAsync<float?>(
            new CommandDefinition(sql, new { movieId }, cancellationToken: cancellationToken));
    }

    public async Task<(float? Rating, int? UserRating)> GetRatingAsync(Guid movieId, Guid userId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateDbConnectionAsync(cancellationToken);

        const string sql = """
                               SELECT 
                                   round(avg(rating)::numeric, 1)::float8 AS rating,
                                   (
                                       SELECT rating::int
                                       FROM ratings
                                       WHERE movie_id = @movieId AND user_id = @userId
                                       LIMIT 1
                                   ) AS user_rating
                               FROM ratings
                               WHERE movie_id = @movieId;
                           """;

        return await connection.QuerySingleOrDefaultAsync<(float?, int?)>(
            new CommandDefinition(sql, new { movieId, userId }, cancellationToken: cancellationToken));
    }

    public async Task<bool> DeleteRatingAsync(Guid movieId, Guid userId, CancellationToken cancellationToken = default)
    {
        var connection = await connectionFactory.CreateDbConnectionAsync(cancellationToken);
        const string sql = """
                            DELETE FROM ratings 
                                WHERE movie_id = @movieId
                                    AND user_id = @userId;
                           """;
        var result = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { movieId, userId }, cancellationToken: cancellationToken));
        return result > 0;
    }

    public async Task<IEnumerable<MovieRating>> GetAllRatingsByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var connection = await connectionFactory.CreateDbConnectionAsync(cancellationToken);
        const string sql = """
                            SELECT 
                                r.rating,
                                r.movie_id as movieId,
                                m.slug
                            FROM 
                                ratings r
                                INNER JOIN movies m ON r.movie_id = m.id
                            WHERE r.user_id = @userId;
                           """;
        return await connection.QueryAsync<MovieRating>(
            new CommandDefinition(sql, new { userId }, cancellationToken: cancellationToken));
    }
}