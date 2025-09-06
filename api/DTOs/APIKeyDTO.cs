using System.ComponentModel.DataAnnotations;

public class CreateApiKeyDto
{
    [Required(ErrorMessage = "Role is required")]
    [StringLength(50, ErrorMessage = "Role cannot exceed 50 characters")]
    public string Role { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters")]
    public string? Description { get; set; }

    public DateTime? ExpiresAt { get; set; }
}

public class ApiKeyDto
{
    public int Id { get; set; }
    public string KeyValue { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int NamespaceId { get; set; }
    public string NamespaceName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsDeleted { get; set; }
}