namespace RevloDB.Entities
{
    public enum NamespaceRole
    {
        ReadOnly = 1,
        Editor = 2,
        Admin = 3
    }

    public class UserNamespace
    {
        public int UserId { get; set; }
        public int NamespaceId { get; set; }
        public NamespaceRole Role { get; set; }
        public DateTime GrantedAt { get; set; }
        public User User { get; set; } = null!;
        public Namespace Namespace { get; set; } = null!;
    }
}