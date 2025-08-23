namespace RevloDB.DTOs
{
    public class VersionDto
    {
        public int Id { get; set; }
        public string Value { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public int VersionNumber { get; set; }
        public int KeyId { get; set; }
    }
}