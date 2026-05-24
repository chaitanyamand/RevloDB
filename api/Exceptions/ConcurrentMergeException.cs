namespace RevloDB.Exceptions
{
    public class ConcurrentMergeException : Exception
    {
        public ConcurrentMergeException(string message) : base(message)
        {
        }
    }
}
