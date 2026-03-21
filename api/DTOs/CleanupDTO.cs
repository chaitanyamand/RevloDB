namespace RevloDB.DTOs
{
    public class CleanupResult
    {
        public int DeletedUsers { get; set; }
        public int DeletedNamespaces { get; set; }
        public int DeletedApiKeys { get; set; }
        public int DeletedExpiredApiKeys { get; set; }

        public int TotalDeleted => DeletedUsers + DeletedNamespaces + DeletedApiKeys + DeletedExpiredApiKeys;
    }

    public class CleanupSummary
    {
        public int MarkedUsersCount { get; set; }
        public int MarkedNamespacesCount { get; set; }
        public int MarkedApiKeysCount { get; set; }
        public int ExpiredApiKeysCount { get; set; }

        public int TotalMarkedForDeletion => MarkedUsersCount + MarkedNamespacesCount + MarkedApiKeysCount + ExpiredApiKeysCount;
    }
}