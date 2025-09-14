using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RevloDB.Constants;
using RevloDB.Data;
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
                var userNamespace = await dbContext.UserNamespaces
                    .FirstOrDefaultAsync(un => un.UserId == parsedUserId &&
                                                un.NamespaceId == parsedNamespaceId);

                var hasRole = userNamespace != null &&
                              RoleCheckUtil.HasSufficientRole(userNamespace.Role, roleAttribute.RequiredRole);

                if (!hasRole)
                {
                    _logger.LogWarning($"User {userId} denied access to namespace {namespaceId}. Required role: {roleAttribute.RequiredRole}");

                    await WriteForbiddenAsync(context, $"Insufficient role. {roleAttribute.RequiredRole} access required.");
                    return;
                }

                context.SetItem(APIConstants.NAMESPACE_ID, namespaceId);

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during role verification for user {userId} in namespace {namespaceId}");
                await WriteForbiddenAsync(context, "Role verification error.");
            }
        }

        private static string? ExtractNamespaceId(HttpContext context)
        {
            if (context.Request.Query.TryGetValue("namespaceId", out var queryNs))
                return queryNs.ToString();
            (bool hasNs, var bodyNs) = DoesNamespaceIdExistInRequestBodyAsync(context).GetAwaiter().GetResult();
            if (hasNs && !string.IsNullOrWhiteSpace(bodyNs))
                return bodyNs;

            return null;
        }

        private static async Task<(bool, string?)> DoesNamespaceIdExistInRequestBodyAsync(HttpContext context)
        {
            if (context.Request.Method != HttpMethods.Post && context.Request.Method != HttpMethods.Put)
                return (false, null);

            if (context.Request.ContentType != null && context.Request.ContentType.Contains("application/json"))
            {
                context.Request.EnableBuffering();
                using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;

                if (!string.IsNullOrWhiteSpace(body))
                {
                    try
                    {
                        using var jsonDoc = JsonDocument.Parse(body);
                        if (jsonDoc.RootElement.TryGetProperty("namespaceId", out var nsElement))
                        {
                            var namespaceId = nsElement.GetRawText().Trim('"');
                            return (true, namespaceId);
                        }
                    }
                    catch (JsonException)
                    {
                        // Ignore JSON parsing errors for this check.
                    }
                }
            }

            return (false, null);
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