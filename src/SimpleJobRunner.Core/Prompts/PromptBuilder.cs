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
            - This is a one-off Simple Job.
            - Always provide a useful final text answer. The runner captures your final answer into summary.md.
            - If the user asks for a simple answer, status check, local query, calculation, command result, inventory, or list, return the answer directly as the text result.
            - Do not create an output file unless the user asks for one.
            - If the user asks for a generated file, create it in ./outputs and summarize it in the text result.
            - If the job performs an external side effect, report exactly what was changed or created in the text result.
            - Generate requested file or files only when the user asks for files.
            - Keep temporary scripts and intermediates in ./temp.
            - Do not create a repository or project scaffold.
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
