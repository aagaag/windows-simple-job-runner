namespace SimpleJobRunner.Core.Prompts;

public sealed class PromptBuilder : IPromptBuilder
{
    public string BuildPrompt(string userPrompt, RunContext run, TaskMode taskMode)
    {
        ArgumentNullException.ThrowIfNull(run);
        if (Security.SecretMasker.ContainsLikelyOpenAiKey(userPrompt))
        {
            throw new InvalidOperationException("The prompt appears to contain an OpenAI API key. Remove secrets before running the job.");
        }

        return $"""
            Use the instructions in AGENTS.md.

            User task:
            {userPrompt.Trim()}

            Task mode:
            {FormatTaskMode(taskMode)}

            Input location:
            ./inbox

            Required output location:
            ./outputs

            Execution constraints:
            - This is a disposable one-off task.
            - Always provide a useful final text answer. The runner captures your final answer into summary.md.
            - If the user asks for a simple answer, local query, command result, calculation, inventory, status check, list, or summary, return the answer directly as the text result.
            - Do not create an output file unless the user asks for one.
            - If the user asks for a generated file, create it in ./outputs and also provide a concise text result describing what was created.
            - If the user asks for an external or persistent action, do not perform it silently. The runner must show a confirmation first.
            - If the user asks for an admin or sensitive action, do not perform it silently. The runner must show a confirmation first.
            - If the job performs an external side effect, report exactly what was changed or created in the text result.
            - Generate requested file or files only when the user asks for files.
            - Keep temporary scripts and intermediates in ./temp.
            - Do not initialize Git.
            - Do not create GitHub repositories unless explicitly confirmed.
            - Do not modify files outside the disposable workspace unless explicitly confirmed.
            - Do not install dependencies without explicit confirmation.
            - Do not store secrets in prompts, transcripts, logs, summaries, or outputs.
            """;
    }

    private static string FormatTaskMode(TaskMode taskMode)
    {
        return taskMode switch
        {
            TaskMode.TextQuery => "Text Query",
            TaskMode.FileJob => "File Job",
            TaskMode.ExternalAction => "External Action",
            TaskMode.AdminSensitive => "Admin / Sensitive",
            _ => "Text Query"
        };
    }
}
