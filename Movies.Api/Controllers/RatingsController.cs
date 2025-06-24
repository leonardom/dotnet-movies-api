using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Movies.Api.Auth;
using Movies.Api.Mappers;
using Movies.Application.Models;
using Movies.Application.Services;
using Movies.Contracts.Requests;

namespace Movies.Api.Controllers;

[ApiController]
public class RatingsController(IRatingService ratingService) : ControllerBase
{
    [Authorize]
    [HttpPut(ApiEndpoints.Movies.Rate)]
    public async Task<IActionResult> RateMovie([FromRoute] Guid id, [FromBody] RateMovieRequest request, 
        CancellationToken cancellationToken)
    {
        var userId = HttpContext.GetUserId();
        var result = await ratingService.RateMovieAsync(id, userId!.Value, request.Rating, cancellationToken);
        return result ? NoContent() : NotFound();
    }

    [Authorize]
    [HttpDelete(ApiEndpoints.Movies.DeleteRating)]
    public async Task<IActionResult> DeleteRating([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var userId = HttpContext.GetUserId();
        var result = await ratingService.DeleteRatingAsync(id, userId!.Value, cancellationToken);
        return result ? NoContent() : NotFound();
    }

    [Authorize]
    [HttpGet(ApiEndpoints.Ratings.GetUserRatings)]
    public async Task<IActionResult> GetUserRatings(CancellationToken cancellationToken)
    {
        var userId = HttpContext.GetUserId();
        var ratings = await ratingService.GetAllRatingsByUserAsync(userId!.Value, cancellationToken);
        var response = ratings.MapToResponse();
        return Ok(response);
    }
}