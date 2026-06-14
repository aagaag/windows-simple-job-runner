namespace SimpleJobRunner.Core;

public sealed record SetupStatus(bool NeedsSetup, bool VoiceInputAvailable, string Message);

public static class SetupStatusCalculator
{
    public static SetupStatus Evaluate(AppSettings settings, bool openAiApiKeyConfigured, CodexAvailability codex)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(codex);

        if (!codex.IsAvailable)
        {
            return new SetupStatus(true, openAiApiKeyConfigured, "Setup needed: Codex CLI was not found on PATH.");
        }

        if (!codex.SupportsRequiredExecFlags)
        {
            return new SetupStatus(true, openAiApiKeyConfigured, "Setup needed: Codex CLI does not report all required non-interactive flags.");
        }

        if (settings.CodexAuthMode == CodexAuthMode.UseStoredOpenAiApiKey && !openAiApiKeyConfigured)
        {
            return new SetupStatus(
                true,
                false,
                "Setup needed: Codex API-key mode is selected, but no OpenAI API key is saved. Save a key or switch to existing Codex CLI authentication.");
        }

        if (!openAiApiKeyConfigured)
        {
            return new SetupStatus(
                false,
                false,
                $"Ready for typed jobs through existing Codex CLI authentication. Voice transcription is disabled until an OpenAI API key is saved. Outputs: {settings.OutputRoot}");
        }

        return new SetupStatus(false, true, $"Ready. Voice transcription is enabled. Outputs: {settings.OutputRoot}");
    }
}
