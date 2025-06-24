using System.Security.Principal;

namespace Movies.Api.Auth;

public static class IdentityExtensions
{
    public static Guid? GetUserId(this HttpContext context)
    {
        var userId = context.User.Claims.SingleOrDefault(c => c.Type == "userid");
        if (Guid.TryParse(userId?.Value, out var id))
        {
            return id;
        }
        return null;
    }
}