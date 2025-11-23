namespace RevloDB.API.Tests.DTOs
{
    public class AuthenticatedUser
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string RoleInNamespace { get; set; } = string.Empty;
        public int NamespaceId { get; set; }
        public string NamespaceName { get; set; } = string.Empty;
    }
}