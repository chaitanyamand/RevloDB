namespace RevloDB.DTOs
{
    public class CleanupResult
    {
        public int DeletedKeys { get; set; }
        public int DeletedUsers { get; set; }
        public int DeletedNamespaces { get; set; }
        public int DeletedApiKeys { get; set; }
        public int DeletedExpiredApiKeys { get; set; }

        public int TotalDeleted => DeletedKeys + DeletedUsers + DeletedNamespaces + DeletedApiKeys + DeletedExpiredApiKeys;
    }

    public class CleanupSummary
    {
        public int MarkedKeysCount { get; set; }
        public int MarkedUsersCount { get; set; }
        public int MarkedNamespacesCount { get; set; }
        public int MarkedApiKeysCount { get; set; }
        public int ExpiredApiKeysCount { get; set; }

        public int TotalMarkedForDeletion => MarkedKeysCount + MarkedUsersCount + MarkedNamespacesCount + MarkedApiKeysCount + ExpiredApiKeysCount;
    }
}