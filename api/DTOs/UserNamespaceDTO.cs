public class UserNamespaceDto
{
    public int UserId { get; set; }
    public int NamespaceId { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public NamespaceDto Namespace { get; set; } = null!;
}