namespace RevloDB.Entities
{
    public interface IChange
    {
        string KeyName { get; }
        string? Value { get; }
        ChangeAction Action { get; }
    }
}
