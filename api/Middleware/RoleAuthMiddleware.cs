using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RevloDB.Constants;
using RevloDB.Data;
using RevloDB.Entities;
using RevloDB.Filters;
using RevloDB.Utils;

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
            var endpoint = context.GetEndpoint();
            var roleAttribute = endpoint?.Metadata?.GetMetadata<RoleRequiredAttribute>();

            if (roleAttribute == null)
            {
                await _next(context);
                return;
            }

            var userId = context.GetItem<string>(APIConstants.USER_ID);
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("Role check attempted but no user_id found in context");
                await WriteForbiddenAsync(context, "User authentication required for role verification.");
                return;
            }

            var namespaceId = ExtractNamespaceId(context);
            if (string.IsNullOrWhiteSpace(namespaceId))
            {
                await WriteForbiddenAsync(context, "Namespace context required for role verification.");
                return;
            }

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<RevloDbContext>();

                if (!int.TryParse(userId, out var parsedUserId) || !int.TryParse(namespaceId, out var parsedNamespaceId))
                {
                    await WriteForbiddenAsync(context, "Invalid user or namespace identifier.");
                    return;
                }
                var hasRole = await dbContext.UserNamespaces
                    .AnyAsync(un => un.UserId == parsedUserId &&
                                   un.NamespaceId == parsedNamespaceId &&
                                   HasSufficientRole(un.Role, roleAttribute.RequiredRole));

                if (!hasRole)
                {
                    _logger.LogWarning("User {UserId} denied access to namespace {NamespaceId}. Required role: {RequiredRole}",
                        userId, namespaceId, roleAttribute.RequiredRole);

                    await WriteForbiddenAsync(context, $"Insufficient role. {roleAttribute.RequiredRole} access required.");
                    return;
                }

                context.SetItem(APIConstants.NAMESPACE_ID, namespaceId);

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during role verification for user {UserId} in namespace {NamespaceId}",
                    userId, namespaceId);
                await WriteForbiddenAsync(context, "Role verification error.");
            }
        }

        private static string? ExtractNamespaceId(HttpContext context)
        {
            if (context.Request.Query.TryGetValue("namespaceId", out var queryNs))
                return queryNs.ToString();
            return null;
        }

        private static bool HasSufficientRole(NamespaceRole NamespaceRole, NamespaceRole requiredRole)
        {
            return requiredRole switch
            {
                NamespaceRole.ReadOnly => NamespaceRole == NamespaceRole.ReadOnly || NamespaceRole == NamespaceRole.Editor || NamespaceRole == NamespaceRole.Admin,
                NamespaceRole.Editor => NamespaceRole == NamespaceRole.Editor || NamespaceRole == NamespaceRole.Admin,
                NamespaceRole.Admin => NamespaceRole == NamespaceRole.Admin,
                _ => false
            };
        }

        private static async Task WriteForbiddenAsync(HttpContext context, string detail)
        {
            if (context.Response.HasStarted) return;

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
    }
}