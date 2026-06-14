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

        if (ExternalActionPattern().IsMatch(normalized))
        {
            return TaskMode.ExternalAction;
        }

        if (AdminSensitivePattern().IsMatch(normalized))
        {
            return TaskMode.AdminSensitive;
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
                "This job may create or modify something outside this computer.\n\nDetected action:\n" + DescribeDetectedAction(prompt) + "\n\nRequires:\nNetwork or external service access\n\nProceed?\n\nTask:\n" + prompt.Trim(),
            TaskMode.AdminSensitive =>
                "This job may affect your system configuration or user files.\n\nDetected action:\n" + DescribeDetectedAction(prompt) + "\n\nProceed only if you understand the risk.\n\nTask:\n" + prompt.Trim(),
            _ => string.Empty
        };
    }

    public string DescribeDetectedAction(string prompt)
    {
        var normalized = prompt.ToLowerInvariant();
        if (normalized.Contains("github", StringComparison.OrdinalIgnoreCase) && normalized.Contains("repo", StringComparison.OrdinalIgnoreCase))
        {
            return "Create or modify GitHub repository";
        }

        if (normalized.Contains("install", StringComparison.OrdinalIgnoreCase))
        {
            return "Install software or packages";
        }

        if (normalized.Contains("delete", StringComparison.OrdinalIgnoreCase) || normalized.Contains("remove", StringComparison.OrdinalIgnoreCase))
        {
            return "Delete or remove files/resources";
        }

        if (normalized.Contains("service", StringComparison.OrdinalIgnoreCase))
        {
            return "Change services or system configuration";
        }

        if (normalized.Contains("upload", StringComparison.OrdinalIgnoreCase) || normalized.Contains("send", StringComparison.OrdinalIgnoreCase) || normalized.Contains("publish", StringComparison.OrdinalIgnoreCase))
        {
            return "Upload, send, or publish data";
        }

        return "External or sensitive side effect";
    }

    [GeneratedRegex(@"\b(delete|remove|erase|format|wipe|shutdown|restart service|stop service|start service|change service|install package|install packages|install software|modify system|system config|registry|firewall|elevation|administrator|admin|sudo|service)\b", RegexOptions.CultureInvariant)]
    private static partial Regex AdminSensitivePattern();

    [GeneratedRegex(@"\b(create (a )?(private |public )?(github )?repository|delete (a )?(github )?repository|delete repo|create repo|github repo|upload|send email|send data|post to|publish|deploy|call external api|external api|sharepoint|teams message|slack|webhook|remote server|push to remote)\b", RegexOptions.CultureInvariant)]
    private static partial Regex ExternalActionPattern();

    [GeneratedRegex(@"\b(create|make|generate|write|save|export|convert|merge|split|produce)\b.*\b(file|csv|xlsx|excel|workbook|pdf|docx|document|markdown|md|report|outputs?)\b|\b(file|csv|xlsx|excel|workbook|pdf|docx|document|markdown|md|report)\b.*\b(in outputs|to outputs|output file|generated file)\b", RegexOptions.CultureInvariant)]
    private static partial Regex FileJobPattern();
}
