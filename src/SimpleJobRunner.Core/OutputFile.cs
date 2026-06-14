namespace SimpleJobRunner.Core;

public sealed record OutputFile(string SourcePath, string FinalPath, long SizeBytes);
