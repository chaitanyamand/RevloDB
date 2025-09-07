using System.ComponentModel.DataAnnotations;

public class JWTOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Key { get; set; } = string.Empty;

    [Range(1, 86400)]
    [Required]
    public int ExpirationInSeconds { get; set; } = 300;
}