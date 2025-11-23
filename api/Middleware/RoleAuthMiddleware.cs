using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RevloDB.Constants;
using RevloDB.Data;
using RevloDB.Entities;
using RevloDB.Extensions;
using RevloDB.Filters;
using RevloDB.Utility;

namespace RevloDB.Middleware
{
    public class RoleAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RoleAuthMiddleware> _logger;
        private readonly IServiceProvider _serviceProvider;

        public RoleAuthMiddleware(RequestDelegate next, ILogger<RoleAuthMiddleware> logger, IServiceProvider serviceProvider)
        {
            _next = next;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var roleAttribute = GetRoleRequirement(context);
            if (roleAttribute == null)
            {
                await _next(context);
                return;
            }

            var isApiKeyAuth = context.GetItem<bool>(APIConstants.IS_API_KEY_PRESENT);
            if (isApiKeyAuth)
            {
                await ProcessApiKeyAuthAsync(context, roleAttribute);
                return;
            }

            await ProcessJwtAuthAsync(context, roleAttribute);
        }

        private static RoleRequiredAttribute? GetRoleRequirement(HttpContext context)
        {
            return context.GetEndpoint()?.Metadata?.GetMetadata<RoleRequiredAttribute>();
        }

        private async Task ProcessApiKeyAuthAsync(HttpContext context, RoleRequiredAttribute roleAttribute)
        {
            var authContext = ExtractApiKeyAuthContext(context);
            if (!authContext.IsValid)
            {
                _logger.LogWarning("Role check attempted but necessary context items are missing for API key authentication");
                await WriteForbiddenAsync(context, "User authentication required for role verification.");
                return;
            }

            if (!ValidateApiKeyRole(authContext.Role, roleAttribute.RequiredRole))
            {
                _logger.LogWarning($"User {authContext.UserId} denied access to namespace {authContext.NamespaceId}. Required role: {roleAttribute.RequiredRole}");
                await WriteForbiddenAsync(context, $"Insufficient role. {roleAttribute.RequiredRole} access required.");
                return;
            }

            await _next(context);
        }

        private async Task ProcessJwtAuthAsync(HttpContext context, RoleRequiredAttribute roleAttribute)
        {
            var userId = context.GetItem<string>(APIConstants.USER_ID);
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("Role check attempted but no user_id found in context");
                await WriteForbiddenAsync(context, "User authentication required for role verification.");
                return;
            }

            var namespaceId = await ExtractNamespaceIdAsync(context);
            if (string.IsNullOrWhiteSpace(namespaceId))
            {
                await WriteForbiddenAsync(context, "Namespace context required for role verification.");
                return;
            }

            if (!int.TryParse(namespaceId, out var parsedNamespaceId) || parsedNamespaceId <= 0)
            {
                await WriteBadRequestAsync(context, "Invalid namespace ID. Must be a positive integer.");
                return;
            }

            try
            {
                var hasRequiredRole = await CheckUserRoleAsync(userId, parsedNamespaceId, roleAttribute.RequiredRole);
                if (!hasRequiredRole)
                {
                    _logger.LogWarning($"User {userId} denied access to namespace {namespaceId}. Required role: {roleAttribute.RequiredRole}");
                    await WriteForbiddenAsync(context, $"Insufficient role. {roleAttribute.RequiredRole} access required.");
                    return;
                }

                context.SetItem(APIConstants.NAMESPACE_ID, namespaceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during role verification for user {userId} in namespace {namespaceId}");
                await WriteForbiddenAsync(context, "Role verification error.");
            }
            await _next(context);
        }

        private static ApiKeyAuthContext ExtractApiKeyAuthContext(HttpContext context)
        {
            var userId = context.GetItem<string>(APIConstants.USER_ID);
            var namespaceId = context.GetItem<string>(APIConstants.NAMESPACE_ID);
            var role = context.GetItem<string>(APIConstants.ROLE);

            return new ApiKeyAuthContext(userId, namespaceId, role);
        }

        private static bool ValidateApiKeyRole(string? role, NamespaceRole requiredRole)
        {
            if (string.IsNullOrWhiteSpace(role))
                return false;

            var roleEnum = role.ToEnum<NamespaceRole>();
            if (!roleEnum.HasValue)
                throw new InvalidOperationException($"Role '{role}' is not a valid NamespaceRole.");

            return RoleCheckUtil.HasSufficientRole(roleEnum.Value, requiredRole);
        }

        private async Task<bool> CheckUserRoleAsync(string userId, int parsedNamespaceId, NamespaceRole requiredRole)
        {
            if (!int.TryParse(userId, out var parsedUserId))
                return false;

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RevloDbContext>();

            var userNamespace = await dbContext.UserNamespaces
                .FirstOrDefaultAsync(un => un.UserId == parsedUserId && un.NamespaceId == parsedNamespaceId);

            return userNamespace != null && RoleCheckUtil.HasSufficientRole(userNamespace.Role, requiredRole);
        }

        private static async Task<string?> ExtractNamespaceIdAsync(HttpContext context)
        {
            if (context.Request.Query.TryGetValue("namespaceId", out var queryNs))
                return queryNs.ToString();

            var (hasNs, bodyNs) = await ExtractNamespaceFromBodyAsync(context);
            return hasNs && !string.IsNullOrWhiteSpace(bodyNs) ? bodyNs : null;
        }

        private static async Task<(bool HasNamespace, string? NamespaceId)> ExtractNamespaceFromBodyAsync(HttpContext context)
        {
            if (!IsPostOrPutRequest(context) || !IsJsonRequest(context))
                return (false, null);

            try
            {
                var body = await ReadRequestBodyAsync(context);
                if (string.IsNullOrWhiteSpace(body))
                    return (false, null);

                using var jsonDoc = JsonDocument.Parse(body);
                if (jsonDoc.RootElement.TryGetProperty("namespaceId", out var nsElement))
                {
                    var namespaceId = nsElement.GetRawText().Trim('"');
                    return (true, namespaceId);
                }
            }
            catch (JsonException)
            {
                // Ignore JSON parsing errors
            }

            return (false, null);
        }

        private static bool IsPostOrPutRequest(HttpContext context)
        {
            return context.Request.Method == HttpMethods.Post || context.Request.Method == HttpMethods.Put;
        }

        private static bool IsJsonRequest(HttpContext context)
        {
            return context.Request.ContentType?.Contains("application/json") == true;
        }

        private static async Task<string> ReadRequestBodyAsync(HttpContext context)
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
            return body;
        }

        private static async Task WriteForbiddenAsync(HttpContext context, string detail)
        {
            if (context.Response.HasStarted)
                return;

            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            context.Response.ContentType = "application/json";

            var problem = new ProblemDetails
            {
                Status = (int)HttpStatusCode.Forbidden,
                Title = "Forbidden",
                Detail = detail,
                Instance = context.Request.Path
            };

            var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }

        private static async Task WriteBadRequestAsync(HttpContext context, string detail)
        {
            if (context.Response.HasStarted)
                return;

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";

            var problem = new ProblemDetails
            {
                Status = (int)HttpStatusCode.BadRequest,
                Title = "Bad Request",
                Detail = detail,
                Instance = context.Request.Path
            };

            var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }

        private readonly struct ApiKeyAuthContext
        {
            public string? UserId { get; }
            public string? NamespaceId { get; }
            public string? Role { get; }
            public bool IsValid => !string.IsNullOrWhiteSpace(UserId) &&
                                  !string.IsNullOrWhiteSpace(NamespaceId) &&
                                  !string.IsNullOrWhiteSpace(Role);

            public ApiKeyAuthContext(string? userId, string? namespaceId, string? role)
            {
                UserId = userId;
                NamespaceId = namespaceId;
                Role = role;
            }
        }
    }
}