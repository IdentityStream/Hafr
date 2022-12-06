namespace Hafr
{
    internal static class StringExtensions
    {
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return value.Substring(0, Math.Min(value.Length, maxLength));
        }
    }
}
