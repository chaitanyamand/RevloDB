using RevloDB.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public ICollection<NamespaceDto> CreatedNamespaces { get; set; } = new List<NamespaceDto>();
    public ICollection<UserNamespaceDto> UserNamespaces { get; set; } = new List<UserNamespaceDto>();
}
