using System.Text.Json;

namespace RevloDB.Utility
{
    public static class SnapshotUtil
    {
        public static bool ShouldCreateSnapshot(int generation, int snapshotInterval)
            => generation > 0 && generation % snapshotInterval == 0;

        public static string SerializeState(Dictionary<string, string> state)
            => JsonSerializer.Serialize(state);

        public static Dictionary<string, string> DeserializeState(string json)
            => JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
    }
}
