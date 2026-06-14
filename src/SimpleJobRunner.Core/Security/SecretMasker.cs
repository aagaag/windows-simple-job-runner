using System.Text.RegularExpressions;

namespace SimpleJobRunner.Core.Security;

public static partial class SecretMasker
{
    public static string MaskOpenAiKey(string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return "(not set)";
        }

        var trimmed = apiKey.Trim();
        var last4 = trimmed.Length <= 4 ? trimmed : trimmed[^4..];
        return trimmed.StartsWith("sk-", StringComparison.OrdinalIgnoreCase)
            ? $"sk-...{last4}"
            : $"...{last4}";
    }

    public static bool ContainsLikelyOpenAiKey(string text)
    {
        return OpenAiKeyRegex().IsMatch(text);
    }

    public static string RedactSecret(string text, string? secret)
    {
        return string.IsNullOrEmpty(secret)
            ? text
            : text.Replace(secret, "[redacted]", StringComparison.Ordinal);
    }

    [GeneratedRegex(@"sk-[A-Za-z0-9_\-]{16,}", RegexOptions.CultureInvariant)]
    private static partial Regex OpenAiKeyRegex();
}
