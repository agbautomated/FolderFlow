using System.Diagnostics;
using System.Text;
using FolderFlow.Core;

// These are integration checks: every transfer uses the installed Windows RoboCopy.
// All copy, mirror, cancellation, and failure fixtures stay beneath --fixtures.
return await new IntegrationSuite(args).RunAsync();

sealed class IntegrationSuite
{
    private readonly string fixtureParent;
    private readonly string fixtureRoot;
    private readonly bool keepFixtures;
    private readonly List<string> junctions = [];
    private readonly RobocopyRunner runner = new();
    private int passed;
    private int failed;
    private int skipped;

    public IntegrationSuite(string[] args)
    {
        var flag = Array.IndexOf(args, "--fixtures");
        fixtureParent = Path.GetFullPath(flag >= 0 && flag + 1 < args.Length
            ? args[flag + 1] : Path.Combine(Directory.GetCurrentDirectory(), "work", "test-fixtures"));
        fixtureRoot = Path.Combine(fixtureParent, "folderflow-" + Guid.NewGuid().ToString("N"));
        keepFixtures = args.Contains("--keep-fixtures", StringComparer.Ordinal);
    }

    public async Task<int> RunAsync()
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.Error.WriteLine("These checks require Windows and its built-in RoboCopy.");
            return 2;
        }
        Directory.CreateDirectory(fixtureRoot);
        var previousDataDirectory = Environment.GetEnvironmentVariable("FOLDERFLOW_DATA_DIR");
        Environment.SetEnvironmentVariable("FOLDERFLOW_DATA_DIR", Path.Combine(fixtureRoot, "appdata"));
        Console.WriteLine($"Real RoboCopy integration checks. Fixtures: {fixtureRoot}");
        try
        {
            await Check("Copy: Unicode, spaces, empty folders, unrelated destination files, and result counts", CopyAsync);
            await Check("Copy: changed source replaces the destination version", ChangedFileAsync);
            await Check("Mirror: deletes only destination extras and keeps source files", MirrorAsync);
            await Check("Preview: copy and mirror leave both folders unchanged", DryRunAsync);
            await Check("Locked source file: real failure is reported with details", LockedFileAsync);
            await Check("Stop: cancels a running RoboCopy process during retry", CancellationAsync);
            await Check("Path guards: missing, same, nested, root, and file selections", PathGuardsAsync);
            await Check("Arguments: safe defaults, isolated paths, large-file mode, and preview", ArgumentsAsync);
            await Check("Logs: saved Unicode details and friendly storage failure", LogsAsync);
            await Check("Reparse guards: selected junction and copy/mirror destination junction", JunctionGuardsAsync);
        }
        finally
        {
            Environment.SetEnvironmentVariable("FOLDERFLOW_DATA_DIR", previousDataDirectory);
            // Remove junction objects explicitly before recursively removing the known fixture tree.
            foreach (var junction in junctions)
                if (Directory.Exists(junction)) Directory.Delete(junction);
            var resolvedRoot = Path.GetFullPath(fixtureRoot);
            var expectedPrefix = Path.TrimEndingDirectorySeparator(fixtureParent) + Path.DirectorySeparatorChar;
            if (!resolvedRoot.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Refusing to clean a directory outside the fixture parent.");
            try
            {
                if (keepFixtures) Console.WriteLine($"Fixtures retained for review: {resolvedRoot}");
                else Directory.Delete(resolvedRoot, recursive: true);
            }
            catch (Exception ex) { Console.WriteLine($"Fixture cleanup needs attention: {ex.Message}"); }
        }
        Console.WriteLine($"RESULT: {passed} passed, {failed} failed, {skipped} skipped.");
        return failed == 0 ? 0 : 1;
    }

    private async Task Check(string name, Func<Task> test)
    {
        try { await test(); passed++; Console.WriteLine($"PASS  {name}"); }
        catch (SkippedCheck ex) { skipped++; Console.WriteLine($"SKIP  {name}: {ex.Message}"); }
        catch (Exception ex) { failed++; Console.WriteLine($"FAIL  {name}\n{ex}"); }
    }

    private async Task CopyAsync()
    {
        var (source, destination) = Pair("copy & $literal (spaces)");
        Directory.CreateDirectory(Path.Combine(source, "Empty folder"));
        Directory.CreateDirectory(Path.Combine(source, "Holiday photos"));
        await File.WriteAllTextAsync(Path.Combine(source, "Holiday photos", "café 日本語.txt"), "Unicode survives ✓", Encoding.UTF8);
        await File.WriteAllTextAsync(Path.Combine(source, "new.txt"), "new data");
        var sameSource = Path.Combine(source, "unchanged.txt");
        var sameDestination = Path.Combine(destination, "unchanged.txt");
        await File.WriteAllTextAsync(sameSource, "already copied");
        File.Copy(sameSource, sameDestination);
        File.SetLastWriteTimeUtc(sameDestination, File.GetLastWriteTimeUtc(sameSource));
        await File.WriteAllTextAsync(Path.Combine(destination, "keep me.txt"), "unrelated destination content");
        var observed = new List<CopyProgress>();
        var result = await runner.RunAsync(Job(source, destination), new ImmediateProgress(observed.Add), CancellationToken.None);
        Success(result);
        True(result.LogPath is null, "Turning off saved logs must not expose a persistent log.");
        var temporaryLogs = Path.Combine(fixtureRoot, "appdata", "Temporary");
        True(!Directory.Exists(temporaryLogs) || !Directory.EnumerateFiles(temporaryLogs).Any(), "Private progress transcripts must be removed after a completed run.");
        Equal("Unicode survives ✓", await File.ReadAllTextAsync(Path.Combine(destination, "Holiday photos", "café 日本語.txt")), "Unicode data");
        True(Directory.Exists(Path.Combine(destination, "Empty folder")), "Empty folders should copy.");
        True(File.Exists(Path.Combine(destination, "keep me.txt")), "Copy must retain unrelated destination files.");
        Equal(3L, RequireSummary(result).Total, "total files");
        Equal(2L, RequireSummary(result).Copied, "copied files");
        Equal(1L, RequireSummary(result).Skipped, "skipped files");
        Equal(0L, RequireSummary(result).Failed, "failed files");
        True(result.ExitCode is > 0 and < 8, "A nonzero RoboCopy success code must be accepted.");
        True(observed.Count > 0, "Real-time progress should be delivered.");
        True(observed.All(p => p.FilePercent is null or >= 0 and <= 100), "Any available percentage must be bounded.");
        True(result.Details.Contains("café 日本語.txt", StringComparison.Ordinal), "Unicode diagnostic output must decode correctly.");

        var repeated = await Run(source, destination);
        Success(repeated);
        Equal(0L, RequireSummary(repeated).Copied, "repeat copied files");
        Equal(3L, RequireSummary(repeated).Skipped, "repeat skipped files");
        Equal(2, repeated.ExitCode!.Value, "extra-only code is successful");
    }

    private async Task ChangedFileAsync()
    {
        var (source, destination) = Pair("changed");
        await File.WriteAllTextAsync(Path.Combine(destination, "note.txt"), "old");
        await File.WriteAllTextAsync(Path.Combine(source, "note.txt"), "updated source data");
        var result = await Run(source, destination);
        Success(result);
        Equal("updated source data", await File.ReadAllTextAsync(Path.Combine(destination, "note.txt")), "changed file content");
        Equal(1L, RequireSummary(result).Copied, "changed file count");
    }

    private async Task MirrorAsync()
    {
        var (source, destination) = Pair("mirror");
        await File.WriteAllTextAsync(Path.Combine(source, "keep.txt"), "source must survive");
        await File.WriteAllTextAsync(Path.Combine(destination, "remove.txt"), "extra file");
        Directory.CreateDirectory(Path.Combine(destination, "extra folder"));
        await File.WriteAllTextAsync(Path.Combine(destination, "extra folder", "remove nested.txt"), "extra nested file");
        var result = await Run(source, destination, CopyMode.Mirror);
        Success(result);
        True(!File.Exists(Path.Combine(destination, "remove.txt")), "Mirror should delete the root extra.");
        True(!Directory.Exists(Path.Combine(destination, "extra folder")), "Mirror should delete the extra subtree.");
        Equal("source must survive", await File.ReadAllTextAsync(Path.Combine(source, "keep.txt")), "mirror source content");
        Equal("source must survive", await File.ReadAllTextAsync(Path.Combine(destination, "keep.txt")), "mirror destination content");
    }

    private async Task DryRunAsync()
    {
        foreach (var mode in new[] { CopyMode.Copy, CopyMode.Mirror })
        {
            var (source, destination) = Pair("preview-" + mode);
            await File.WriteAllTextAsync(Path.Combine(source, "would-copy.txt"), "source");
            await File.WriteAllTextAsync(Path.Combine(destination, "keep-extra.txt"), "destination extra");
            var beforeSource = Snapshot(source);
            var beforeDestination = Snapshot(destination);
            var result = await Run(source, destination, mode, dryRun: true);
            Success(result);
            Equal(beforeSource, Snapshot(source), "preview source snapshot");
            Equal(beforeDestination, Snapshot(destination), "preview destination snapshot");
            True(result.FriendlyMessage.Contains("preview", StringComparison.OrdinalIgnoreCase)
                || result.FriendlyMessage.Contains("would", StringComparison.OrdinalIgnoreCase), "Preview should identify its non-mutating result.");
        }
    }

    private async Task LockedFileAsync()
    {
        var (source, destination) = Pair("locked");
        var path = Path.Combine(source, "locked.txt");
        await File.WriteAllTextAsync(path, "locked content");
        using var held = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        var result = await runner.RunAsync(Job(source, destination) with
        {
            Settings = Defaults() with { RetryCount = 0, RetryWaitSeconds = 0 }
        }, null, CancellationToken.None);
        True(result.ExitCode >= 8, "A locked file must return an actual RoboCopy failure code.");
        True(result.Outcome is CopyOutcome.NeedsAttention or CopyOutcome.Failed, "A copy failure must require attention.");
        True(RequireSummary(result).Failed >= 1, "Failed file count should be reported.");
        True(!string.IsNullOrWhiteSpace(result.Details), "Failure should have optional diagnostic details.");
        True(!File.Exists(Path.Combine(destination, "locked.txt")) || new FileInfo(Path.Combine(destination, "locked.txt")).Length == 0,
            "A locked source must not appear as a successful destination copy.");
    }

    private async Task CancellationAsync()
    {
        var (source, destination) = Pair("cancel");
        var lockedPath = Path.Combine(source, "locked-for-stop.txt");
        await File.WriteAllTextAsync(lockedPath, "hold RoboCopy in its retry phase");
        using var held = new FileStream(lockedPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var sawRunningOutput = false;
        var progress = new ImmediateProgress(p =>
        {
            if (p.RecentLine?.Contains("locked-for-stop", StringComparison.OrdinalIgnoreCase) == true
                || p.FilePath?.Contains("locked-for-stop", StringComparison.OrdinalIgnoreCase) == true)
            {
                sawRunningOutput = true;
                stop.CancelAfter(TimeSpan.FromMilliseconds(150));
            }
        });
        var watch = Stopwatch.StartNew();
        var result = await runner.RunAsync(Job(source, destination) with
        {
            Settings = Defaults() with { Multithreaded = false, RetryCount = 10, RetryWaitSeconds = 2 }
        }, progress, stop.Token);
        Equal(CopyOutcome.Cancelled, result.Outcome, "cancelled outcome");
        True(sawRunningOutput, "Stop must be exercised after RoboCopy has produced actual output.");
        True(watch.Elapsed < TimeSpan.FromSeconds(8), "Cancellation must stop retries promptly.");
        True(File.Exists(lockedPath), "Cancellation must preserve source files.");
        True(result.FriendlyMessage.Contains("stop", StringComparison.OrdinalIgnoreCase)
            || result.FriendlyMessage.Contains("cancel", StringComparison.OrdinalIgnoreCase), "Stopped result should explain cancellation.");
    }

    private Task PathGuardsAsync()
    {
        var (source, destination) = Pair("guards");
        Invalid(Job("", destination));
        Invalid(Job(Path.Combine(source, "missing"), destination));
        Invalid(Job(source, Path.Combine(destination, "missing")));
        Invalid(Job(@"\\server\share", destination));
        Invalid(Job("C:relative-folder", destination));
        Invalid(Job(source, source));
        Invalid(Job(source, source + Path.DirectorySeparatorChar));
        Invalid(Job(source, Directory.CreateDirectory(Path.Combine(source, "child")).FullName));
        Invalid(Job(source, Directory.GetParent(source)!.FullName));
        Invalid(Job(source, Path.GetPathRoot(destination)!, CopyMode.Mirror));
        Invalid(Job(Path.GetPathRoot(source)!, destination));
        var file = Path.Combine(source, "not-a-folder.txt");
        File.WriteAllText(file, "file");
        Invalid(Job(file, destination));
        Invalid(Job(source, file));
        Invalid(Job(source, destination) with { Settings = Defaults() with { ThreadCount = 0 } });
        Invalid(Job(source, destination) with { Settings = Defaults() with { RetryCount = -1 } });
        PathValidator.Validate(Job(source, destination));
        return Task.CompletedTask;
    }

    private Task ArgumentsAsync()
    {
        var (source, destination) = Pair("argument & isolation");
        var args = RobocopyArguments.Build(PathValidator.Validate(Job(source, destination)));
        Equal(source, args[0], "source argument");
        Equal(destination, args[1], "destination argument");
        True(args.Contains("/E") && args.Contains("/Z") && args.Contains("/MT:2"), "Copy defaults should include subfolders, restartability, and two workers.");
        True(args.Contains("/R:2") && args.Contains("/W:2"), "Retries must be bounded.");
        True(!args.Any(a => a is "/MIR" or "/PURGE" or "/MOVE" or "/MOV" or "/B" or "/ZB" or "/SEC" or "/COPYALL"), "Default copy must not request deletion, move, backup privileges, or ACL copying.");
        True(args.Contains("/FP") && args.Contains("/BYTES"), "Output should support full file paths and numeric summaries; runner provides authoritative Unicode logging.");
        var largeArgs = RobocopyArguments.Build(PathValidator.Validate(Job(source, destination) with
        {
            Settings = Defaults() with { LargeFiles = true, Multithreaded = false }
        }));
        True(largeArgs.Contains("/J") && !largeArgs.Contains("/Z") && !largeArgs.Any(a => a.StartsWith("/MT", StringComparison.Ordinal)),
            "Large file mode and one-worker setting must affect actual arguments.");
        var mirrorArgs = RobocopyArguments.Build(PathValidator.Validate(Job(source, destination, CopyMode.Mirror, true)));
        True(mirrorArgs.Contains("/PURGE") && mirrorArgs.Contains("/E") && mirrorArgs.Contains("/L"), "Mirror preview must list deletion candidates without mutating folders.");
        foreach (var code in new[] { 0, 1, 2, 3 }) Equal(CopyOutcome.Completed, RobocopyRunner.InterpretExitCode(code), "non-failing code " + code);
        foreach (var code in new[] { 4, 5, 6, 7 }) Equal(CopyOutcome.NeedsAttention, RobocopyRunner.InterpretExitCode(code), "mismatch code " + code);
        foreach (var code in new[] { -1, 8, 9, 16 }) Equal(CopyOutcome.Failed, RobocopyRunner.InterpretExitCode(code), "failure code " + code);
        return Task.CompletedTask;
    }

    private async Task JunctionGuardsAsync()
    {
        var (source, destination) = Pair("junctions");
        var outside = Directory.CreateDirectory(Path.Combine(fixtureRoot, "junction-sentinel")).FullName;
        var sentinel = Path.Combine(outside, "must-survive.txt");
        await File.WriteAllTextAsync(sentinel, "outside destination remains untouched");
        var selectedLink = Path.Combine(fixtureRoot, "selected-junction");
        await Junction(selectedLink, source);
        Invalid(Job(selectedLink, destination));
        var child = Directory.CreateDirectory(Path.Combine(source, "child")).FullName;
        Invalid(Job(Path.Combine(selectedLink, "child"), destination));
        var destinationLink = Path.Combine(destination, "do-not-follow");
        await Junction(destinationLink, outside);
        var matchingSource = Directory.CreateDirectory(Path.Combine(source, "do-not-follow")).FullName;
        await File.WriteAllTextAsync(Path.Combine(matchingSource, "must-survive.txt"), "this must never overwrite the outside sentinel");
        foreach (var mode in new[] { CopyMode.Copy, CopyMode.Mirror })
        {
            var rejected = false;
            try
            {
                var result = await Run(source, destination, mode);
                True(result.Outcome is CopyOutcome.Failed or CopyOutcome.NeedsAttention,
                    "Copy and mirror must refuse a destination subtree containing links.");
                rejected = true;
            }
            catch (JobValidationException) { rejected = true; }
            True(rejected, "Dangerous destination should be rejected.");
            Equal("outside destination remains untouched", await File.ReadAllTextAsync(sentinel), "outside sentinel");
        }
    }

    private async Task LogsAsync()
    {
        var (source, destination) = Pair("logs");
        await File.WriteAllTextAsync(Path.Combine(source, "café 日本語.txt"), "logged file");
        var logRoot = Path.Combine(fixtureRoot, "saved-logs");
        var loggedRunner = new RobocopyRunner(logRoot);
        var result = await loggedRunner.RunAsync(Job(source, destination) with
        {
            Settings = Defaults() with { SaveLog = true }
        });
        Success(result);
        True(result.LogPath is not null && File.Exists(result.LogPath), "Save log should write a persistent file.");
        True(Path.GetFullPath(result.LogPath!).StartsWith(logRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), "Log stays inside configured fixture location.");
        True((await File.ReadAllTextAsync(result.LogPath!)).Contains("café 日本語.txt", StringComparison.Ordinal), "Saved log must preserve Unicode paths.");

        var blockedLogDirectory = Path.Combine(fixtureRoot, "not-a-log-folder");
        await File.WriteAllTextAsync(blockedLogDirectory, "a file prevents folder creation");
        var warningRunner = new RobocopyRunner(blockedLogDirectory);
        var warning = await warningRunner.RunAsync(Job(source, destination) with
        {
            Settings = Defaults() with { SaveLog = true }
        });
        True(warning.ExitCode is null, "When the required Unicode transcript cannot be created, RoboCopy must not start.");
        Equal(CopyOutcome.Failed, warning.Outcome, "log storage failure outcome");
        True(warning.LogPath is null && !string.IsNullOrWhiteSpace(warning.Details), "Log storage error must have diagnostic details.");
    }

    private async Task Junction(string link, string target)
    {
        var psi = new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe"),
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
        };
        psi.ArgumentList.Add("-NoProfile");
        psi.ArgumentList.Add("-NonInteractive");
        psi.ArgumentList.Add("-Command");
        psi.ArgumentList.Add("$ErrorActionPreference = 'Stop'; New-Item -ItemType Junction -Path $env:FOLDERFLOW_TEST_LINK -Target $env:FOLDERFLOW_TEST_TARGET | Out-Null");
        psi.Environment["FOLDERFLOW_TEST_LINK"] = link;
        psi.Environment["FOLDERFLOW_TEST_TARGET"] = target;
        using var process = Process.Start(psi)!;
        var stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0) throw new SkippedCheck("This Windows environment cannot create fixture junctions: " + await stderr);
        junctions.Add(link);
    }

    private (string Source, string Destination) Pair(string name)
    {
        var parent = Path.Combine(fixtureRoot, name);
        return (Directory.CreateDirectory(Path.Combine(parent, "Copy from")).FullName,
            Directory.CreateDirectory(Path.Combine(parent, "Copy to")).FullName);
    }

    private static CopySettings Defaults() => new() { SaveLog = false };
    private static CopyJob Job(string source, string destination, CopyMode mode = CopyMode.Copy, bool dryRun = false)
        => new(source, destination, mode, Defaults(), dryRun);
    private Task<CopyResult> Run(string source, string destination, CopyMode mode = CopyMode.Copy, bool dryRun = false)
        => runner.RunAsync(Job(source, destination, mode, dryRun), null, CancellationToken.None);
    private static string Snapshot(string root) => string.Join("\n", Directory.EnumerateFileSystemEntries(root, "*", SearchOption.AllDirectories)
        .Order(StringComparer.Ordinal).Select(path => Path.GetRelativePath(root, path) + "|" +
            (File.Exists(path) ? Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path))) : "directory")));
    private static FileSummary RequireSummary(CopyResult result) => result.Files ?? throw new Exception("A real English RoboCopy summary must be parsed.\n" + result.Details);
    private static void Success(CopyResult result) => True(result.Outcome == CopyOutcome.Completed && result.ExitCode is >= 0 and < 8,
        $"Expected successful RoboCopy run; got {result.Outcome} / {result.ExitCode}: {result.FriendlyMessage}\n{result.Details}");
    private static void Invalid(CopyJob job)
    {
        try { PathValidator.Validate(job); }
        catch (JobValidationException) { return; }
        throw new Exception($"Unsafe/invalid job was accepted: {job.SourcePath} -> {job.DestinationPath}");
    }
    private static void True(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Equal<T>(T expected, T actual, string label) => True(EqualityComparer<T>.Default.Equals(expected, actual), $"{label}: expected {expected}, actual {actual}");
    private sealed class SkippedCheck(string message) : Exception(message);
    private sealed class ImmediateProgress(Action<CopyProgress> report) : IProgress<CopyProgress>
    {
        public void Report(CopyProgress value) => report(value);
    }
}
