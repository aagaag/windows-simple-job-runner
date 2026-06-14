using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SimpleJobRunner.Core.Codex;

public sealed class CodexRunner(CodexCommandBuilder? commandBuilder = null, CodexJsonlParser? parser = null) : ICodexRunner
{
    private readonly CodexCommandBuilder _commandBuilder = commandBuilder ?? new CodexCommandBuilder();
    private readonly CodexJsonlParser _parser = parser ?? new CodexJsonlParser();

    public async IAsyncEnumerable<CodexEvent> RunAsync(
        RunContext run,
        string prompt,
        CodexRunOptions options,
        [EnumeratorCancellation] CancellationToken ct)
    {
        Directory.CreateDirectory(run.RunRoot);
        if (Security.SecretMasker.ContainsLikelyOpenAiKey(prompt))
        {
            throw new InvalidOperationException("The Codex prompt appears to contain an OpenAI API key. Remove secrets before running the job.");
        }

        await File.WriteAllTextAsync(run.PromptPath, prompt, ct);

        var startInfo = _commandBuilder.BuildStartInfo(run, options.SandboxMode);
        if (options.UseStoredApiKey && !string.IsNullOrWhiteSpace(options.OpenAiApiKey))
        {
            startInfo.Environment["CODEX_API_KEY"] = options.OpenAiApiKey;
        }

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        if (!process.Start())
        {
            throw new InvalidOperationException("Could not start Codex CLI.");
        }

        yield return CodexEvent.Progress("Running Codex CLI.");

        var stderrTask = process.StandardError.ReadToEndAsync(ct);
        await process.StandardInput.WriteAsync(prompt.AsMemory(), ct);
        await process.StandardInput.FlushAsync(ct);
        process.StandardInput.Close();

        StreamWriter? eventWriter = null;
        try
        {
            if (options.DebugLogging)
            {
                eventWriter = new StreamWriter(File.Open(run.EventsPath, FileMode.Create, FileAccess.Write, FileShare.Read));
            }

            while (await process.StandardOutput.ReadLineAsync(ct) is { } line)
            {
                if (eventWriter is not null)
                {
                    var persisted = Security.SecretMasker.Redact(line, [options.OpenAiApiKey]);
                    await eventWriter.WriteLineAsync(persisted.AsMemory(), ct);
                }

                yield return _parser.ParseLine(line);
            }
        }
        finally
        {
            if (eventWriter is not null)
            {
                await eventWriter.DisposeAsync();
            }
        }

        await process.WaitForExitAsync(ct);
        var stderr = await stderrTask;
        if (process.ExitCode != 0)
        {
            throw new CodexRunnerException(process.ExitCode, stderr);
        }

        yield return CodexEvent.Progress("Codex CLI finished.");
    }
}

public sealed class CodexRunnerException(int exitCode, string stderr)
    : Exception($"Codex CLI exited with code {exitCode}. {stderr.Trim()}")
{
    public int ExitCode { get; } = exitCode;
    public string Stderr { get; } = stderr;
}
