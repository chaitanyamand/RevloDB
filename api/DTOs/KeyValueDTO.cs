namespace RevloDB.DTOs
{
    public class KeyValueDto
    {
        public string KeyName { get; set; } = string.Empty;
        public string? Value { get; set; }
    }

    public class KeyVersionValueDto
    {
        public string KeyName { get; set; } = string.Empty;
        public int VersionNumber { get; set; }
        public string? Value { get; set; }
    }
}