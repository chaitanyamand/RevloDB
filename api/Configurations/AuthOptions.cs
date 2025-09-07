using System.ComponentModel.DataAnnotations;

namespace RevloDB.Configuration
{
    public class AuthOptions
    {
        public const string SectionName = "Authentication";

        [Required]
        public JWTOptions Jwt { get; set; } = new JWTOptions();

    }
}