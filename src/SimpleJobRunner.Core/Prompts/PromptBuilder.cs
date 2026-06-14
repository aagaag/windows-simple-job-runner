namespace SimpleJobRunner.Core.Prompts;

public sealed class PromptBuilder : IPromptBuilder
{
    public string BuildPrompt(string userPrompt, RunContext run)
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

            Input location:
            ./inbox

            Required output location:
            ./outputs

            Execution constraints:
            - This is a one-off Simple Job.
            - Generate the requested file or files only.
            - Keep temporary scripts and intermediates in ./temp.
            - Do not create a repository or project scaffold.
            """;
    }
}
