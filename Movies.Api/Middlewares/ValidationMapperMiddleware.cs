using FluentValidation;
using Movies.Contracts.Responses;

namespace Movies.Api.Middlewares;

public class ValidationMapperMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var response = new ValidationFailureResponse
            {
                Errors = ex.Errors.Select(e => new ValidationResponse
                {
                    PropertyName = e.PropertyName,
                    Message = e.ErrorMessage,
                })
            };
            await context.Response.WriteAsJsonAsync(response);
        }
    }
}