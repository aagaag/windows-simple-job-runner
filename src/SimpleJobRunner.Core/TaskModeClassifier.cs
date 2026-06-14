using System.Text.RegularExpressions;

namespace SimpleJobRunner.Core;

public sealed partial class TaskModeClassifier
{
    public TaskMode Classify(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return TaskMode.TextQuery;
        }

        var normalized = prompt.ToLowerInvariant();

        if (AdminSensitivePattern().IsMatch(normalized))
        {
            return TaskMode.AdminSensitive;
        }

        if (ExternalActionPattern().IsMatch(normalized))
        {
            return TaskMode.ExternalAction;
        }

        if (FileJobPattern().IsMatch(normalized))
        {
            return TaskMode.FileJob;
        }

        return TaskMode.TextQuery;
    }

    public string ConfirmationMessage(TaskMode mode, string prompt)
    {
        return mode switch
        {
            TaskMode.ExternalAction =>
                "This looks like an external persistent action. It may create, upload, send, publish, or call an external service. Confirm only if you want Codex to proceed with that side effect.\n\nTask:\n" + prompt.Trim(),
            TaskMode.AdminSensitive =>
                "This looks like an administrative or sensitive action. It may modify system configuration, delete files, change services, install packages, or require elevation. Confirm only if you want Codex to proceed under the configured sandbox and safety rules.\n\nTask:\n" + prompt.Trim(),
            _ => string.Empty
        };
    }

    [GeneratedRegex(@"\b(delete|remove|erase|format|wipe|shutdown|restart service|stop service|start service|change service|install package|install packages|install software|modify system|system config|registry|firewall|elevation|administrator|admin|sudo|service)\b", RegexOptions.CultureInvariant)]
    private static partial Regex AdminSensitivePattern();

    [GeneratedRegex(@"\b(create (a )?(private |public )?(github )?repository|create repo|github repo|upload|send email|send data|post to|publish|deploy|call external api|external api|sharepoint|teams message|slack|webhook)\b", RegexOptions.CultureInvariant)]
    private static partial Regex ExternalActionPattern();

    [GeneratedRegex(@"\b(create|make|generate|write|save|export|convert|merge|split|produce)\b.*\b(file|csv|xlsx|excel|workbook|pdf|docx|document|markdown|md|report|outputs?)\b|\b(file|csv|xlsx|excel|workbook|pdf|docx|document|markdown|md|report)\b.*\b(in outputs|to outputs|output file|generated file)\b", RegexOptions.CultureInvariant)]
    private static partial Regex FileJobPattern();
}
