namespace RevloDB.Extensions
{
    public static class EnumExtensions
    {
        public static T? ToEnum<T>(this string? value) where T : struct, Enum
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return Enum.TryParse<T>(value, ignoreCase: true, out var result) ? result : null;
        }

        public static T ToEnumOrThrow<T>(this string? value, string errorMessage) where T : struct, Enum
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{typeof(T).Name} cannot be null or empty", nameof(value));

            return Enum.TryParse<T>(value, ignoreCase: true, out var result)
                ? result
                : throw new ArgumentException(errorMessage, nameof(value));
        }
    }
}