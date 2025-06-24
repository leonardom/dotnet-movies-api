using System.Data;
using System.Globalization;
using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Logging;
using Movies.Application.Database;
using Movies.Application.Models;

namespace Movies.Application.Repositories;

public class MovieRepository(
    IDbConnectionFactory dbConnectionFactory,
    ILogger<MovieRepository> logger) : IMovieRepository
{
    public async Task<bool> CreateAsync(Movie movie, CancellationToken cancellationToken = default)
    {
        var connection = await dbConnectionFactory.CreateDbConnectionAsync(cancellationToken);
        var transaction = connection.BeginTransaction();
        var result = await connection.ExecuteAsync(
            new CommandDefinition("""
                                    insert into movies(id, slug, title, year_of_release)
                                    values(@Id, @Slug, @Title, @YearOfRelease);
                                    """, movie, cancellationToken: cancellationToken));

        if (result > 0)
        {
            foreach (var genre in movie.Genres)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition("""
                                            insert into movies_genres (movie_id, name)
                                            values(@MovieId, @Name);
                                            """, new { MovieId = movie.Id, Name = genre }, cancellationToken: cancellationToken));
            }
        }
        transaction.Commit();
        return result > 0;
    }

    public async Task<IEnumerable<Movie>> GetAllAsync(GetAllMoviesOptions options, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting all movies with options: {}", JsonSerializer.Serialize(options));
        using var connection = await dbConnectionFactory.CreateDbConnectionAsync(cancellationToken);
        
        var orderBy = string.Empty;
        if (options.SortField is not null)
        {
            orderBy = $"""
                        , m.{options.SortField}
                        ORDER BY m.{options.SortField} {(options.SortOrder == SortOrder.Descending ? "DESC" : "ASC")}
                      """;
        }
        
        var sql = $"""
                       SELECT 
                           m.id, 
                           m.slug, 
                           m.title, 
                           m.year_of_release, 
                           string_agg(distinct g.name, ',') as genres,
                           ROUND(AVG(mr.rating), 1) as rating,
                           ur.rating as user_rating
                       FROM 
                           movies m 
                             INNER JOIN movies_genres g ON m.id = g.movie_id
                             LEFT JOIN ratings mr ON mr.movie_id = m.id
                             LEFT JOIN ratings ur ON ur.user_id = @userId 
                                                         and ur.movie_id = m.id
                       WHERE 
                           (@title is null or m.title ilike ('%' || @title || '%'))
                           AND (@yearOfRelease is null or m.year_of_release = @yearOfRelease)  
                       GROUP BY 
                           id, 
                           user_rating 
                           {orderBy}
                   """;
        var result = await connection.QueryAsync(
            new CommandDefinition(sql, new
                {
                    userId = options.UserId,
                    title = options.Title,
                    yearOfRelease = options.YearOfRelease,
                }, 
                cancellationToken: cancellationToken));

        return result.Select(x => new Movie
        {
            Id = x.id,
            Title = x.title,
            YearOfRelease = x.year_of_release,
            Rating = (float?)x.rating,
            UserRating = (int?)x.user_rating,
            Genres = Enumerable.ToList(x.genres.Split(','))
        });
    }

    public async Task<Movie?> GetByIdAsync(Guid id, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var connection = await dbConnectionFactory.CreateDbConnectionAsync(cancellationToken);
        const string sql = """
                               SELECT 
                                   m.id, 
                                   m.slug, 
                                   m.title, 
                                   m.year_of_release AS YearOfRelease,
                                   ROUND(AVG(mr.rating), 1)::float4 AS Rating,
                                   ur.rating::int AS UserRating
                               FROM movies m
                               LEFT JOIN ratings mr ON mr.movie_id = m.id
                               LEFT JOIN ratings ur ON ur.user_id = @userId AND ur.movie_id = m.id
                               WHERE m.id = @id
                               GROUP BY m.id, m.slug, m.title, m.year_of_release, ur.rating;
                           """;
        var movie = await connection.QuerySingleOrDefaultAsync<Movie>(
            new CommandDefinition(sql, new { id, userId }, cancellationToken: cancellationToken));
        if (movie is null) return movie;
        await LoadGenres(movie, connection, cancellationToken);
        return movie;
    }

    public async Task<Movie?> GetBySlugAsync(string slug, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var connection = await dbConnectionFactory.CreateDbConnectionAsync(cancellationToken);
        var movie = await connection.QuerySingleOrDefaultAsync<Movie>(
            new CommandDefinition("""
                                  select 
                                      m.id, 
                                      m.slug, 
                                      m.title, 
                                      m.year_of_release as YearOfRelease,
                                      round(avg(mr.rating), 1) as Rating,
                                      ur.rating as UserRating
                                  from 
                                      movies m
                                      left join ratings mr on mr.movie_id = m.id
                                      left join ratings ur on ur.user_id = @userId and ur.movie_id = m.id
                                  where 
                                      slug = @slug
                                  group by
                                      id, 
                                      mr.rating, 
                                      ur.rating;
                                  """, new { slug, userId }, cancellationToken: cancellationToken));
        if (movie is null) return movie;
        await LoadGenres(movie, connection, cancellationToken);
        return movie;
    }

    public async Task<bool> UpdateAsync(Movie movie, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.CreateDbConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        await connection.ExecuteAsync(
            new CommandDefinition("delete from movies_genres g where g.movie_id = @id;", new { id = movie.Id }, 
                cancellationToken: cancellationToken));
        foreach (var genre in movie.Genres)
        {
            await connection.ExecuteAsync(
                new CommandDefinition("insert into movies_genres (movie_id, name) values (@MovieId, @Name);", 
                    new { MovieId = movie.Id, Name = genre }, cancellationToken: cancellationToken));
        }
        var result = await connection.ExecuteAsync(
            new CommandDefinition("""
                                  update movies set 
                                                    slug = @Slug, 
                                                    title = @Title, 
                                                    year_of_release = @YearOfRelease 
                                                where id = @Id
                                  """, movie, cancellationToken: cancellationToken));
        transaction.Commit();
        return result > 0;
    }

    public async Task<bool> DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.CreateDbConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        await connection.ExecuteAsync(
            new CommandDefinition("delete from movies_genres g where g.movie_id = @id;", new { id }, 
                cancellationToken: cancellationToken));
        var result = await connection.ExecuteAsync(
            new CommandDefinition("delete from movies m where m.id = @id;", new { id }, 
                cancellationToken: cancellationToken));
        transaction.Commit();
        return result > 0;
    }

    public async Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.CreateDbConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition("select count(1) from movies where id = @id ", new { id }, 
                cancellationToken: cancellationToken ));
    }

    private static async Task LoadGenres(Movie movie, IDbConnection connection, CancellationToken cancellationToken)
    {
        var genres = await connection.QueryAsync<string>(
            new CommandDefinition("select name from movies_genres where movie_id = @Id;", new { id = movie.Id }, 
                cancellationToken: cancellationToken));
        foreach (var genre in genres)
        {
            movie.Genres.Add(genre);
        }    
    }
}