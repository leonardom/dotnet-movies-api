using FluentValidation;
using Movies.Application.Models;

namespace Movies.Application.Validators;

public class GetAllMoviesOptionsValidator : AbstractValidator<GetAllMoviesOptions>
{

    private static readonly string[] AcceptableSortFields =
    [
        "title", "year_of_release"
    ];
    
    public GetAllMoviesOptionsValidator()
    {
        RuleFor(o => o.YearOfRelease)
            .LessThanOrEqualTo(DateTime.UtcNow.Year);
        RuleFor(o => o.SortField)
            .Must(s => s is null || AcceptableSortFields.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage("The sort field must be one of: title, year_of_release");
    }
}