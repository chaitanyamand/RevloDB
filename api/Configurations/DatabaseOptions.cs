using System.ComponentModel.DataAnnotations;

namespace RevloDB.Configuration
{
    public class DatabaseOptions
    {
        public const string SectionName = "Database";
        [Required]
        public bool AutoMigrate { get; set; } = true;
        [Required]
        public bool CreateDatabaseIfNotExists { get; set; } = true;
        [Range(1, 3600)]
        [Required]
        public int CommandTimeout { get; set; } = 30;
        [Required]
        public bool RetryOnFailure { get; set; } = true;
        [Range(0, 10)]
        [Required]
        public int MaxRetryCount { get; set; } = 3;
        [Range(1, 60)]
        [Required]
        public int RetryDelay { get; set; } = 5;
    }
}