using System.Net;
using System.Text.Json;

namespace RevloDB.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = exception switch
            {
                KeyNotFoundException keyNotFound => new
                {
                    message = keyNotFound.Message,
                    statusCode = (int)HttpStatusCode.NotFound,
                    type = "KeyNotFound"
                },
                InvalidOperationException invalidOp when invalidOp.Message.Contains("already exists") => new
                {
                    message = invalidOp.Message,
                    statusCode = (int)HttpStatusCode.Conflict,
                    type = "Conflict"
                },
                InvalidOperationException invalidOp => new
                {
                    message = invalidOp.Message,
                    statusCode = (int)HttpStatusCode.InternalServerError,
                    type = "InternalServerError"
                },
                ArgumentException argEx => new
                {
                    message = argEx.Message,
                    statusCode = (int)HttpStatusCode.BadRequest,
                    type = "BadRequest"
                },
                _ => new
                {
                    message = "An internal server error occurred",
                    statusCode = (int)HttpStatusCode.InternalServerError,
                    type = "InternalServerError"
                }
            };

            context.Response.StatusCode = response.statusCode;

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }
}