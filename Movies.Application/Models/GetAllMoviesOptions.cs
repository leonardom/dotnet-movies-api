namespace Movies.Application.Models;

public class GetAllMoviesOptions
{
    public string? Title { get; init; }
    public int? YearOfRelease { get; init; }
    public Guid? UserId { get; set; }
    public string? SortField { get; init; }
    public SortOrder? SortOrder { get; init; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public enum SortOrder
{
    Unsorted,
    Ascending,
    Descending
}