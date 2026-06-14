using System.Net.Http.Headers;
using System.Text.Json;

namespace SimpleJobRunner.Core.Transcription;

public sealed class OpenAiTranscriptionService(
    HttpClient httpClient,
    ICredentialStore credentialStore,
    Func<CancellationToken, Task<string>>? modelProvider = null) : ITranscriptionService
{
    private readonly Func<CancellationToken, Task<string>> _modelProvider =
        modelProvider ?? (_ => Task.FromResult("gpt-4o-transcribe"));

    public async Task<string> TranscribeAsync(string audioPath, string promptHint, CancellationToken ct)
    {
        if (!File.Exists(audioPath))
        {
            throw new FileNotFoundException("Audio file does not exist.", audioPath);
        }

        var apiKey = await credentialStore.GetOpenAiApiKeyAsync(ct);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new MissingOpenAiApiKeyException();
        }

        var model = await _modelProvider(ct);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/audio/transcriptions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        await using var audioStream = File.Open(audioPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(model), "model");
        content.Add(new StringContent("json"), "response_format");
        if (!string.IsNullOrWhiteSpace(promptHint))
        {
            content.Add(new StringContent(promptHint), "prompt");
        }

        var fileContent = new StreamContent(audioStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(GuessContentType(audioPath));
        content.Add(fileContent, "file", Path.GetFileName(audioPath));
        request.Content = content;

        using var response = await httpClient.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new OpenAiTranscriptionException($"Transcription failed with HTTP {(int)response.StatusCode}.");
        }

        using var document = JsonDocument.Parse(body);
        if (!document.RootElement.TryGetProperty("text", out var textElement))
        {
            throw new OpenAiTranscriptionException("Transcription response did not contain text.");
        }

        return textElement.GetString() ?? string.Empty;
    }

    private static string GuessContentType(string audioPath)
    {
        return Path.GetExtension(audioPath).ToLowerInvariant() switch
        {
            ".mp3" => "audio/mpeg",
            ".mp4" => "audio/mp4",
            ".m4a" => "audio/mp4",
            ".mpeg" => "audio/mpeg",
            ".mpga" => "audio/mpeg",
            ".webm" => "audio/webm",
            ".wav" => "audio/wav",
            _ => "application/octet-stream"
        };
    }
}

public sealed class MissingOpenAiApiKeyException()
    : InvalidOperationException("OpenAI API key is not configured.");

public sealed class OpenAiTranscriptionException(string message)
    : InvalidOperationException(message);
