using System.Data;
using Dapper;
using Movies.Application.Database;
using Movies.Application.Models;

namespace Movies.Application.Repositories;

public class PostgresMovieRepository(IDbConnectionFactory dbConnectionFactory) : IMovieRepository
{
    public async Task<bool> CreateAsync(Movie movie)
    {
        var connection = await dbConnectionFactory.CreateDbConnectionAsync();
        var transaction = connection.BeginTransaction();
        var result = await connection.ExecuteAsync(
            new CommandDefinition("""
                                    insert into movies(id, slug, title, year_of_release)
                                    values(@Id, @Slug, @Title, @YearOfRelease);
                                    """, movie));

        if (result > 0)
        {
            foreach (var genre in movie.Genres)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition("""
                                            insert into movies_genres (movie_id, name)
                                            values(@MovieId, @Name);
                                            """, new { MovieId = movie.Id, Name = genre }));
            }
        }
        transaction.Commit();
        return result > 0;
    }

    public async Task<IEnumerable<Movie>> GetAllAsync()
    {
        using var connection = await dbConnectionFactory.CreateDbConnectionAsync();
        var result = await connection.QueryAsync(
            new CommandDefinition("""
                                  select m.id, m.slug, m.title, m.year_of_release, string_agg(g.name, ',') as genres
                                  from movies m inner join movies_genres g on m.id = g.movie_id
                                  group by id
                                  """));
        return result.Select(m => new Movie
        {
            Id = m.id,
            Title = m.title,
            YearOfRelease = m.year_of_release,
            Genres = Enumerable.ToList(m.genres.Split(","))
        });
    }

    public async Task<Movie?> GetByIdAsync(Guid id)
    {
        var connection = await dbConnectionFactory.CreateDbConnectionAsync();
        var movie = await connection.QuerySingleOrDefaultAsync<Movie>(
            new CommandDefinition("""
                                    select id, slug, title, year_of_release as YearOfRelease 
                                    from movies where id = @id;
                                    """, new { id }));
        if (movie is null) return movie;
        var genres = await connection.QueryAsync<string>(
            new CommandDefinition("select name from movies_genres where movie_id = @Id;", new { id }));
        foreach (var genre in genres)
        {
            movie.Genres.Add(genre);
        }
        return movie;
    }

    public async Task<Movie?> GetBySlugAsync(string slug)
    {
        var connection = await dbConnectionFactory.CreateDbConnectionAsync();
        var movie = await connection.QuerySingleOrDefaultAsync<Movie>(
            new CommandDefinition("""
                                  select id, slug, title, year_of_release as YearOfRelease 
                                  from movies where slug = @slug;
                                  """, new { slug }));
        if (movie is null) return movie;
        await LoadGenres(movie, connection);
        return movie;
    }

    public async Task<bool> UpdateAsync(Movie movie)
    {
        using var connection = await dbConnectionFactory.CreateDbConnectionAsync();
        using var transaction = connection.BeginTransaction();
        await connection.ExecuteAsync(
            new CommandDefinition("delete from movies_genres g where g.movie_id = @id;", new { id = movie.Id }));
        foreach (var genre in movie.Genres)
        {
            await connection.ExecuteAsync(
                new CommandDefinition("insert into movies_genres (movie_id, name) values (@MovieId, @Name);", 
                    new { MovieId = movie.Id, Name = genre }));
        }
        var result = await connection.ExecuteAsync(
            new CommandDefinition("""
                                  update movies set 
                                                    slug = @Slug, 
                                                    title = @Title, 
                                                    year_of_release = @YearOfRelease 
                                                where id = @Id
                                  """, movie));
        transaction.Commit();
        return result > 0;
    }

    public async Task<bool> DeleteByIdAsync(Guid id)
    {
        using var connection = await dbConnectionFactory.CreateDbConnectionAsync();
        using var transaction = connection.BeginTransaction();
        await connection.ExecuteAsync(
            new CommandDefinition("delete from movies_genres g where g.movie_id = @id;", new { id }));
        var result = await connection.ExecuteAsync(
            new CommandDefinition("delete from movies m where m.id = @id;", new { id }));
        transaction.Commit();
        return result > 0;
    }

    public async Task<bool> ExistsByIdAsync(Guid id)
    {
        using var connection = await dbConnectionFactory.CreateDbConnectionAsync();
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition("select count(1) from movies where id = @id ", new { id } ));
    }

    private static async Task LoadGenres(Movie movie, IDbConnection connection)
    {
        var genres = await connection.QueryAsync<string>(
            new CommandDefinition("select name from movies_genres where movie_id = @Id;", new { id = movie.Id }));
        foreach (var genre in genres)
        {
            movie.Genres.Add(genre);
        }    
    }
}