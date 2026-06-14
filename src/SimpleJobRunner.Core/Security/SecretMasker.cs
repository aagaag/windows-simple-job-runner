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

    public static string Redact(string text, IEnumerable<string?>? additionalSecrets = null)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var redacted = OpenAiKeyRegex().Replace(text, "sk-...[redacted]");
        redacted = GithubTokenRegex().Replace(redacted, "$1[redacted]");
        redacted = BearerTokenRegex().Replace(redacted, "$1[redacted]");
        redacted = SensitiveAssignmentRegex().Replace(redacted, "$1[redacted]");
        redacted = PasswordArgumentRegex().Replace(redacted, "$1[redacted]");

        if (additionalSecrets is not null)
        {
            foreach (var secret in additionalSecrets.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal))
            {
                redacted = redacted.Replace(secret!, "[redacted]", StringComparison.Ordinal);
            }
        }

        return redacted;
    }

    [GeneratedRegex(@"sk-[A-Za-z0-9_\-]{16,}", RegexOptions.CultureInvariant)]
    private static partial Regex OpenAiKeyRegex();

    [GeneratedRegex(@"\b(gh[pousr]_)[A-Za-z0-9_]{8,}", RegexOptions.CultureInvariant)]
    private static partial Regex GithubTokenRegex();

    [GeneratedRegex(@"\b(Bearer\s+)[A-Za-z0-9._\-]{8,}", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex BearerTokenRegex();

    [GeneratedRegex(@"\b([A-Za-z0-9_]*(?:API_KEY|TOKEN|SECRET|PASSWORD|KEY|CREDENTIAL)[A-Za-z0-9_]*\s*=\s*)[^ \r\n;&]+", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex SensitiveAssignmentRegex();

    [GeneratedRegex(@"(--?(?:password|token|secret|api-key|key|credential)\s+)[^ \r\n;&]+", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex PasswordArgumentRegex();
}
