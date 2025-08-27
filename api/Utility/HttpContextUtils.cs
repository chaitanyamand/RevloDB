namespace RevloDB.Utils
{
    public static class HttpContextUtils
    {
        public static void SetItem<T>(this HttpContext context, string key, T value)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            context.Items[key] = value;
        }


        public static T? GetItem<T>(this HttpContext context, string key)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            if (context.Items.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }

            return default;
        }
    }
}