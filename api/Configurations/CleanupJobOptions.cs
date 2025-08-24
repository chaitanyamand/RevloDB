using System.ComponentModel.DataAnnotations;

namespace RevloDB.Configuration
{
    public class CleanupJobOptions
    {
        public const string SectionName = "CleanupJob";

        [Required]
        public bool Enabled { get; set; } = true;

        [Range(1, 24)]
        [Required]
        public int IntervalHours { get; set; } = 24;

        [Range(1, 60)]
        [Required]
        public int StartDelayMinutes { get; set; } = 5;

        [Required]
        public bool LogDetailedInfo { get; set; } = true;
    }
}