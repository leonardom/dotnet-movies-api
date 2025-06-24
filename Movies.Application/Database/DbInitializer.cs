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
        using var connection = await _dbConnectionFactory.CreateDbConnectionAsync();
        await connection.ExecuteAsync("""
                                          create table if not exists movies (
                                              id uuid not null primary key,
                                              slug text not null,
                                              title text not null,
                                              year_of_release integer not null 
                                          );
                                      """);

        await connection.ExecuteAsync("""
                                          create unique index concurrently if not exists movies_slug_idx
                                            on movies using btree(slug);
                                      """);

        await connection.ExecuteAsync("""
                                        create table if not exists movies_genres (
                                            movie_id uuid references movies(id),
                                            name text not null 
                                        );
                                      """);

        await connection.ExecuteAsync("""
                                        create table if not exists ratings (
                                            user_id uuid,
                                            movie_id uuid references movies(id),
                                            rating integer not null,
                                            primary key (user_id, movie_id)
                                        );
                                      """);
    }
}