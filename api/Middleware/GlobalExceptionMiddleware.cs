using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Microsoft.AspNetCore.Mvc;

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
                LogException(context, ex);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static string GetSafeHttpMethod(string method)
        {
            return method switch
            {
                "GET" => "GET",
                "POST" => "POST",
                "PUT" => "PUT",
                "DELETE" => "DELETE",
                "PATCH" => "PATCH",
                "HEAD" => "HEAD",
                "OPTIONS" => "OPTIONS",
                _ => "UNKNOWN"
            };
        }

        private static string GetSafePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "/unknown";

            var safePath = new string(path.Where(c => !char.IsControl(c)).ToArray());

            var trimmed = safePath.Length > 100 ? safePath.Substring(0, 100) + "..." : safePath;
            return "[" + trimmed + "]";
        }

        private static string SanitizeForLogging(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var sanitized = input
                .Replace('\n', '_')
                .Replace('\r', '_')
                .Replace('\t', ' ')
                .Replace('\0', '_');

            return System.Text.RegularExpressions.Regex.Replace(sanitized, @"[\x00-\x1F\x7F]", "_");
        }

        private void LogException(HttpContext context, Exception exception)
        {
            var method = SanitizeForLogging(GetSafeHttpMethod(context.Request.Method));
            var path = SanitizeForLogging(GetSafePath(context.Request.Path));

            switch (exception)
            {
                case UnauthorizedAccessException unauthorizedEx:
                    _logger.LogWarning("{Method} {Path} - Unauthorized access: {Message}",
                        method, path, unauthorizedEx.Message);
                    break;

                case BadHttpRequestException badHttpRequestEx:
                    _logger.LogWarning("{Method} {Path} - Bad HTTP request: {Message}",
                        method, path, badHttpRequestEx.Message);
                    break;

                case KeyNotFoundException keyNotFoundEx:
                    _logger.LogError("{Method} {Path} - Key not found: {Message}",
                        method, path, keyNotFoundEx.Message);
                    break;

                case DbUpdateException dbUpdateEx when dbUpdateEx.InnerException is PostgresException postgresEx && postgresEx.SqlState == "23505":
                    _logger.LogError("{Method} {Path} - Duplicate key conflict", method, path);
                    break;

                case ArgumentException argEx:
                    _logger.LogError("{Method} {Path} - Invalid argument: {Message}",
                        method, path, argEx.Message);
                    break;

                case InvalidOperationException invalidOpEx:
                    _logger.LogError("{Method} {Path} - Invalid operation: {Message}",
                        method, path, invalidOpEx.Message);
                    break;

                default:
                    _logger.LogError("{Method} {Path} - Unexpected error: {ExceptionType} - {Message}",
                        method, path, exception.GetType().Name, exception.Message);

                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug("Stack trace: {StackTrace}", exception.StackTrace);
                    }
                    break;
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            if (context.Response.HasStarted)
            {
                return;
            }
            context.Response.ContentType = "application/json";

            var problemDetails = new ProblemDetails
            {
                Instance = context.Request.Path,
                Status = (int)HttpStatusCode.InternalServerError,
                Title = "An internal server error occurred."
            };

            switch (exception)
            {
                case UnauthorizedAccessException unauthorizedEx:
                    problemDetails.Status = (int)HttpStatusCode.Unauthorized;
                    problemDetails.Title = "Unauthorized";
                    problemDetails.Detail = string.IsNullOrEmpty(unauthorizedEx.Message)
                        ? "Authentication is required to access this resource."
                        : unauthorizedEx.Message;
                    break;

                case BadHttpRequestException badHttpRequestEx:
                    problemDetails.Status = badHttpRequestEx.StatusCode;
                    problemDetails.Title = "Bad Request";
                    problemDetails.Detail = badHttpRequestEx.Message;
                    break;

                case KeyNotFoundException keyNotFoundEx:
                    problemDetails.Status = (int)HttpStatusCode.NotFound;
                    problemDetails.Title = "Not Found";
                    problemDetails.Detail = keyNotFoundEx.Message;
                    break;

                case DbUpdateException dbUpdateEx when dbUpdateEx.InnerException is PostgresException postgresEx && postgresEx.SqlState == "23505":
                    problemDetails.Status = (int)HttpStatusCode.Conflict;
                    problemDetails.Title = "Conflict";
                    problemDetails.Detail = "A key with the same name already exists.";
                    break;

                case ArgumentException argEx:
                    problemDetails.Status = (int)HttpStatusCode.BadRequest;
                    problemDetails.Title = "Bad Request";
                    problemDetails.Detail = argEx.Message;
                    break;

                case InvalidOperationException invalidOpEx
                   when invalidOpEx.Message.StartsWith("Concurrency conflict", StringComparison.OrdinalIgnoreCase)
                      || invalidOpEx.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase):
                    problemDetails.Status = (int)HttpStatusCode.Conflict;
                    problemDetails.Title = "Conflict";
                    problemDetails.Detail = invalidOpEx.Message;
                    break;

                case InvalidOperationException invalidOpEx:
                    problemDetails.Status = (int)HttpStatusCode.BadRequest;
                    problemDetails.Title = "Bad Request";
                    problemDetails.Detail = invalidOpEx.Message;
                    break;

                default:
                    problemDetails.Detail = "An unexpected error occurred.";
                    break;
            }

            context.Response.StatusCode = problemDetails.Status.Value;

            var jsonResponse = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }
}