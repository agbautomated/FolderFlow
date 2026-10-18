namespace FolderFlow.Core;

public enum CopyMode { Copy, Mirror }
public enum CopyOutcome { Completed, NeedsAttention, Cancelled, Failed }

public sealed record CopySettings
{
    // Two readers are a deliberately modest starting point for a USB hard disk.
    public bool Multithreaded { get; init; } = true;
    public int ThreadCount { get; init; } = 2;
    public bool SaveLog { get; init; } = true;
    public int RetryCount { get; init; } = 2;
    public int RetryWaitSeconds { get; init; } = 2;
    public bool LargeFiles { get; init; }
}

public sealed record CopyJob(
    string SourcePath,
    string DestinationPath,
    CopyMode Mode,
    CopySettings Settings,
    bool DryRun = false);

public sealed record ValidatedJob(CopyJob Job, string SourcePath, string DestinationPath,
    string CanonicalSourcePath, string CanonicalDestinationPath);

public sealed record FileSummary(long Total, long Copied, long Skipped, long Mismatch, long Failed, long Extras);

public sealed record CopyProgress(
    string Operation,
    string? FilePath,
    double? FilePercent,
    TimeSpan Elapsed,
    string? RecentLine = null,
    long? FilesCopied = null,
    long? FilesFailed = null);

public sealed record CopyResult(
    CopyOutcome Outcome,
    int? ExitCode,
    FileSummary? Files,
    TimeSpan Duration,
    string? LogPath,
    string Details,
    string FriendlyMessage);

public sealed class JobValidationException(string message) : Exception(message);
