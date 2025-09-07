namespace RevloDB.Extensions
{
    public static class StringExtensions
    {
        public static string? ToString<T>(this T? value) where T : struct, Enum
        {
            return value?.ToString();
        }
    }
}