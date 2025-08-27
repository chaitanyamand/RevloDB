using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using RevloDB.Filters;
using RevloDB.Utils;
using RevloDB.Constants;

namespace RevloDB.Middleware
{
    public class JwtAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<JwtAuthMiddleware> _logger;
        private readonly TokenValidationParameters _validationParams;

        public JwtAuthMiddleware(RequestDelegate next, ILogger<JwtAuthMiddleware> logger, TokenValidationParameters validationParams)
        {
            _next = next;
            _logger = logger;
            _validationParams = validationParams;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var endpoint = context.GetEndpoint();
            var requiresAuth = endpoint?.Metadata?.GetMetadata<AuthRequiredAttribute>() != null;

            if (!requiresAuth)
            {
                await _next(context);
                return;
            }

            var token = ExtractToken(context);
            if (string.IsNullOrWhiteSpace(token))
            {
                await WriteUnauthorizedAsync(context, "Missing authorization token.");
                return;
            }

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var principal = handler.ValidateToken(token, _validationParams, out var validatedToken);

                if (validatedToken is not JwtSecurityToken)
                {
                    await WriteUnauthorizedAsync(context, "Invalid token format.");
                    return;
                }

                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrWhiteSpace(userId))
                {
                    await WriteUnauthorizedAsync(context, "Missing user_id in token.");
                    return;
                }

                context.SetItem(APIConstants.USER_ID, userId);

                await _next(context);
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning("Token validation failed: {Message}", ex.Message);
                await WriteUnauthorizedAsync(context, "Invalid or expired token.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during token validation.");
                await WriteUnauthorizedAsync(context, "Authorization error.");
            }
        }

        private static string? ExtractToken(HttpContext context)
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return authHeader["Bearer ".Length..].Trim();

            if (context.Request.Query.TryGetValue("api_token", out var q))
                return q.ToString();

            return null;
        }

        private static async Task WriteUnauthorizedAsync(HttpContext context, string detail)
        {
            if (context.Response.HasStarted) return;

            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Response.ContentType = "application/json";

            var problem = new ProblemDetails
            {
                Status = (int)HttpStatusCode.Unauthorized,
                Title = "Unauthorized",
                Detail = detail,
                Instance = context.Request.Path
            };

            var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }

    public static class JwtAuthMiddlewareExtensions
    {
        public static IApplicationBuilder UseJwtAuth(this IApplicationBuilder app, TokenValidationParameters tvp)
            => app.UseMiddleware<JwtAuthMiddleware>(tvp);
    }
}
