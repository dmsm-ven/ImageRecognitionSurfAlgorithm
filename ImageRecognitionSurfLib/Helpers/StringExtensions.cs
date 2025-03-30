namespace ImageRecognitionSurfLib.Helpers;

internal static class StringExtensions
{
    public static string FillOverflow(this string str, int maxLength = 16, string overflowString = "...")
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return string.Empty;
        }

        if (str.Length < maxLength)
        {
            return str;
        }

        var allowedAmountChars = str.Take(16).ToArray();

        return new string(allowedAmountChars) + overflowString;
    }
}