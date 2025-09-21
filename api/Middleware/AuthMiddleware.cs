using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using RevloDB.Filters;
using RevloDB.Constants;
using RevloDB.Utility;
using RevloDB.Configuration;
using Microsoft.Extensions.Options;
using RevloDB.Entities;
using RevloDB.Data;
using Microsoft.EntityFrameworkCore;

namespace RevloDB.Middleware
{
    public class JwtAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<JwtAuthMiddleware> _logger;
        private readonly AuthOptions _authOptions;
        private readonly IServiceProvider _serviceProvider;

        public JwtAuthMiddleware(RequestDelegate next, ILogger<JwtAuthMiddleware> logger, IOptions<AuthOptions> authOptions, IServiceProvider serviceProvider)
        {
            _next = next;
            _logger = logger;
            _authOptions = authOptions.Value;
            _serviceProvider = serviceProvider;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!RequiresAuthentication(context))
            {
                await _next(context);
                return;
            }

            var jwtToken = ExtractJwtToken(context);
            if (!string.IsNullOrWhiteSpace(jwtToken))
            {
                await ProcessJwtAuthenticationAsync(context, jwtToken);
                return;
            }

            var apiKey = ExtractApiKey(context);
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                await ProcessApiKeyAuthenticationAsync(context, apiKey);
                return;
            }

            await WriteUnauthorizedAsync(context, "Missing authorization token and API key.");
        }

        private static bool RequiresAuthentication(HttpContext context)
        {
            return context.GetEndpoint()?.Metadata?.GetMetadata<AuthRequiredAttribute>() != null;
        }

        private async Task ProcessJwtAuthenticationAsync(HttpContext context, string token)
        {
            try
            {
                var principal = JwtUtil.ValidateToken(token, _authOptions.Jwt.Key);
                if (principal == null)
                {
                    await WriteUnauthorizedAsync(context, "Invalid token format.");
                    return;
                }

                var userId = ExtractUserIdFromPrincipal(principal);
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

        private async Task ProcessApiKeyAuthenticationAsync(HttpContext context, string apiKey)
        {
            var apiKeyDetails = await GetApiKeyDetailsAsync(apiKey);
            if (apiKeyDetails == null)
            {
                await WriteUnauthorizedAsync(context, "Invalid API key.");
                return;
            }

            if (IsApiKeyExpired(apiKeyDetails))
            {
                await WriteUnauthorizedAsync(context, "API key has expired.");
                return;
            }

            SetApiKeyContextItems(context, apiKeyDetails);
            await _next(context);
        }

        private static string? ExtractJwtToken(HttpContext context)
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return null;

            return authHeader["Bearer ".Length..].Trim();
        }

        private static string? ExtractApiKey(HttpContext context)
        {
            return context.Request.Headers.TryGetValue("X-API-Key", out var apiKeyValues)
                ? apiKeyValues.FirstOrDefault()
                : null;
        }

        private static string? ExtractUserIdFromPrincipal(ClaimsPrincipal principal)
        {
            return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        private async Task<ApiKey?> GetApiKeyDetailsAsync(string apiKey)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RevloDbContext>();

            return await dbContext.ApiKeys
                .Where(a => a.KeyValue == apiKey && !a.IsDeleted)
                .FirstOrDefaultAsync();
        }

        private static bool IsApiKeyExpired(ApiKey apiKey)
        {
            return apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value < DateTime.UtcNow;
        }

        private static void SetApiKeyContextItems(HttpContext context, ApiKey apiKey)
        {
            context.SetItem(APIConstants.USER_ID, apiKey.UserId.ToString());
            context.SetItem(APIConstants.NAMESPACE_ID, apiKey.NamespaceId.ToString());
            context.SetItem(APIConstants.ROLE, apiKey.Role.ToString());
            context.SetItem(APIConstants.IS_API_KEY_PRESENT, true);
        }

        private static async Task WriteUnauthorizedAsync(HttpContext context, string detail)
        {
            if (context.Response.HasStarted)
                return;

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
}