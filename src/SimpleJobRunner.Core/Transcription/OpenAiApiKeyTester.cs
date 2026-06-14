using System.Net.Http.Headers;

namespace SimpleJobRunner.Core.Transcription;

public sealed class OpenAiApiKeyTester(HttpClient httpClient) : IOpenAiApiKeyTester
{
    public async Task TestAsync(string apiKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("API key cannot be empty.", nameof(apiKey));
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.openai.com/v1/models");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        using var response = await httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"OpenAI API key test failed with HTTP {(int)response.StatusCode}.");
        }
    }
}
