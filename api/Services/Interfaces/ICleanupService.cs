namespace RevloDB.Services.Interfaces
{
    public interface ICleanupService
    {
        Task<CleanupServiceResult> ExecuteCleanupAsync(CancellationToken cancellationToken = default);
    }

    public class CleanupServiceResult
    {
        public int DeletedCount { get; set; }
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}