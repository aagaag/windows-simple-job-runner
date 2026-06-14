using System.Text.Json;

namespace SimpleJobRunner.Core.Codex;

public sealed class CodexJsonlParser
{
    public CodexEvent ParseLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return new CodexEvent(DateTimeOffset.Now, "stdout", string.Empty, line);
        }

        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;
            var type = GetString(root, "type")
                ?? GetString(root, "event")
                ?? GetString(root, "kind")
                ?? "event";

            var message = ExtractMessage(root);
            return new CodexEvent(DateTimeOffset.Now, type, Truncate(message), line);
        }
        catch (JsonException)
        {
            return new CodexEvent(DateTimeOffset.Now, "stdout", Truncate(line), line);
        }
    }

    private static string ExtractMessage(JsonElement root)
    {
        foreach (var name in new[] { "message", "msg", "summary", "text", "content", "delta" })
        {
            var value = GetString(root, name);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        if (root.TryGetProperty("item", out var item))
        {
            var itemText = GetString(item, "text") ?? GetString(item, "message") ?? GetString(item, "content");
            if (!string.IsNullOrWhiteSpace(itemText))
            {
                return itemText;
            }
        }

        return root.GetRawText();
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    private static string Truncate(string value)
    {
        const int maxLength = 500;
        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }
}
