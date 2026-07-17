using FluentValidation;

namespace AuctionPlatform.API.Middleware;

/// <summary>
/// Menangkap ValidationException yang dilempar oleh ValidationBehavior (FluentValidation)
/// dan mengubahnya jadi response 400 dengan format error yang konsisten,
/// supaya controller tidak perlu try-catch ValidationException di setiap method.
/// </summary>
public class ValidationExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public ValidationExceptionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";

            var errors = ex.Errors.Select(e => e.ErrorMessage).ToList();
            await context.Response.WriteAsJsonAsync(new { error = string.Join(" ", errors), errors });
        }
    }
}

public static class ValidationExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseValidationExceptionHandling(this IApplicationBuilder app)
        => app.UseMiddleware<ValidationExceptionMiddleware>();
}