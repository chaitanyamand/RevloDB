namespace RevloDB.DTOs
{
    public class KeyDto
    {
        public int Id { get; set; }
        public string KeyName { get; set; } = string.Empty;
        public string? CurrentValue { get; set; }
        public int? CurrentVersionNumber { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateKeyDto
    {
        public string KeyName { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class UpdateKeyDto
    {
        public string Value { get; set; } = string.Empty;
    }
}