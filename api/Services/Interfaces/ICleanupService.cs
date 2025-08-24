namespace RevloDB.Services.Interfaces
{
    public interface ICleanupService
    {
        Task<CleanupResult> ExecuteCleanupAsync(CancellationToken cancellationToken = default);
    }

    public class CleanupResult
    {
        public int DeletedKeysCount { get; set; }
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}