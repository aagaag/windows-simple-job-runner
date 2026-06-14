using SimpleJobRunner.Core.Codex;

namespace SimpleJobRunner.Core;

public sealed record FailureExplanation(
    string WhatHappened,
    string LikelyReason,
    string WhatToTryNext,
    string TechnicalDetails)
{
    public override string ToString()
    {
        return $"""
            What happened:
            {WhatHappened}

            Likely reason:
            {LikelyReason}

            What you can try next:
            {WhatToTryNext}

            Technical details:
            {TechnicalDetails}
            """;
    }

    public static FailureExplanation FromException(Exception exception, string? diagnosticsPath = null)
    {
        var diagnostics = string.IsNullOrWhiteSpace(diagnosticsPath)
            ? string.Empty
            : $"{Environment.NewLine}Diagnostics: {diagnosticsPath}";

        if (exception is CodexRunnerException codex)
        {
            return new FailureExplanation(
                "Codex did not complete the task.",
                "The Codex CLI returned a non-zero exit code.",
                "Check that Codex is installed and authenticated, review the Diagnostics tab, then retry.",
                $"Exit code: {codex.ExitCode}{diagnostics}{Environment.NewLine}{codex.Stderr.Trim()}");
        }

        if (exception is FileNotFoundException)
        {
            return new FailureExplanation(
                "A required file or folder was not found.",
                "An input path disappeared or Codex CLI was not available on PATH.",
                "Check the selected inputs and Codex CLI setup, then retry.",
                exception.Message + diagnostics);
        }

        if (exception is InvalidOperationException invalid && invalid.Message.Contains("Codex CLI", StringComparison.OrdinalIgnoreCase))
        {
            return new FailureExplanation(
                "Codex CLI is not ready.",
                "The app could not verify the installed Codex CLI or required exec flags.",
                "Run codex login if needed, verify codex exec --help works, then click Run again.",
                invalid.Message + diagnostics);
        }

        return new FailureExplanation(
            "The job failed before completion.",
            "The app or Codex encountered an unexpected error.",
            "Review the Details and Diagnostics tabs, adjust the prompt or setup, then retry.",
            exception.Message + diagnostics);
    }
}
