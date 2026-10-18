using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace FolderFlow.Core;

public sealed class RobocopyRunner(string? logDirectory = null)
{
    private const int DetailLimit = 1024 * 1024;

    public async Task<CopyResult> RunAsync(CopyJob job, IProgress<CopyProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        Stopwatch elapsed = Stopwatch.StartNew();
        StringBuilder details = new();
        string? privateLogPath = null;
        int stopped = 0;
        string? stopError = null;
        RobocopyOutputParser parser = new(job.Settings is { Multithreaded: false } or { ThreadCount: 1 });
        long lastReport = -1000;
        string? lastFile = null;

        void Record(string line)
        {
            details.AppendLine(line);
            if (details.Length > DetailLimit) details.Remove(0, details.Length - DetailLimit / 2);
            parser.Accept(line);
            if (elapsed.ElapsedMilliseconds - lastReport >= 120 ||
                (parser.FilePath != lastFile && elapsed.ElapsedMilliseconds - lastReport >= 50))
            {
                lastReport = elapsed.ElapsedMilliseconds;
                lastFile = parser.FilePath;
                progress?.Report(new CopyProgress(job.DryRun ? "Previewing" : "Copying", parser.FilePath,
                    parser.FilePercent, elapsed.Elapsed, line));
            }
        }

        CopyResult Result(CopyOutcome outcome, int? code, string message) => new(outcome, code,
            parser.Files, elapsed.Elapsed, job.Settings?.SaveLog == true ? privateLogPath : null,
            details.ToString(), message);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new CopyProgress("Preparing", null, null, elapsed.Elapsed));
            // These checks can be slow on removable drives, so run them off the UI thread.
            ValidatedJob validated = await Task.Run(() =>
            {
                ValidatedJob checkedJob = PathValidator.Validate(job);
                PathValidator.CheckDestinationTree(checkedJob, cancellationToken);
                return checkedJob;
            }, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            string executable = Path.Combine(Environment.SystemDirectory, "robocopy.exe");
            if (!File.Exists(executable))
                return Result(CopyOutcome.Failed, null, "Windows RoboCopy couldn't be found on this computer.");

            // /UNICODE stdout is mixed OEM/UTF-16 on some Windows builds. A native
            // /UNILOG is authoritative UTF-16 and is tailed for both progress and results.
            // With SaveLog off this is a private temporary transcript, deleted on exit.
            string dataDirectory = Environment.GetEnvironmentVariable("FOLDERFLOW_DATA_DIR") ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FolderFlow");
            string directory = Path.GetFullPath(logDirectory ?? Path.Combine(dataDirectory, job.Settings.SaveLog ? "Logs" : "Temporary"));
            string canonicalLogDirectory;
            try
            {
                canonicalLogDirectory = PathValidator.GetProspectiveCanonicalDirectory(directory);
                if (PathValidator.IsSameOrWithin(canonicalLogDirectory, validated.CanonicalDestinationPath))
                    throw new JobValidationException("The copy details folder is inside the destination. Choose a different destination folder.");
                Directory.CreateDirectory(directory);
                string candidatePath = Path.Combine(directory, $"FolderFlow-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.log");
                using FileStream created = new(candidatePath, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite);
                privateLogPath = candidatePath;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                details.AppendLine(ex.ToString());
                throw new JobValidationException("FolderFlow couldn't prepare its copy details. Check that its data folder is writable, then try again.");
            }

            ProcessStartInfo startInfo = new(executable)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            foreach (string argument in RobocopyArguments.Build(validated)) startInfo.ArgumentList.Add(argument);
            startInfo.ArgumentList.Add("/UNILOG:" + privateLogPath);
            // If someone copies their profile, avoid copying the active private transcript.
            if (PathValidator.IsSameOrWithin(canonicalLogDirectory, validated.CanonicalSourcePath))
            {
                startInfo.ArgumentList.Add("/XF");
                string canonicalLogFile = Path.Combine(canonicalLogDirectory, Path.GetFileName(privateLogPath));
                startInfo.ArgumentList.Add(Path.Combine(validated.SourcePath,
                    Path.GetRelativePath(validated.CanonicalSourcePath, canonicalLogFile)));
            }
            using Process process = new() { StartInfo = startInfo };
            Record($"FolderFlow {(job.DryRun ? "preview" : job.Mode.ToString().ToLowerInvariant())}");
            Record($"From: {validated.SourcePath}");
            Record($"To: {validated.DestinationPath}");
            Record($"Started: {DateTimeOffset.Now:O}");
            if (!process.Start()) return Result(CopyOutcome.Failed, null, "The copy couldn't be started. Please try again.");

            using CancellationTokenRegistration registration = cancellationToken.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                    {
                        Interlocked.Exchange(ref stopped, 1);
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or NotSupportedException)
                {
                    stopError = ex.Message;
                }
            });

            using CancellationTokenSource tailStop = new();
            Task stdout = DrainDiscardAsync(process.StandardOutput.BaseStream);
            Task stderr = DrainDiscardAsync(process.StandardError.BaseStream);
            Task tail = TailLogAsync(privateLogPath, Record, tailStop.Token);
            Task exit = process.WaitForExitAsync(CancellationToken.None);
            try
            {
                // Observe every task individually: a reader failure must not leave an
                // unobserved copier running or let a redirected pipe fill and deadlock.
                HashSet<Task> pending = [exit, stdout, stderr, tail];
                while (pending.Count != 0)
                {
                    Task finished = await Task.WhenAny(pending).ConfigureAwait(false);
                    await finished.ConfigureAwait(false);
                    if (finished == exit) tailStop.Cancel();
                    pending.Remove(finished);
                }
            }
            catch
            {
                try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
                catch (Exception ex) when (ex is InvalidOperationException or Win32Exception) { }
                await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
                tailStop.Cancel();
                try { await Task.WhenAll(stdout, stderr, tail).ConfigureAwait(false); }
                catch (Exception) { /* Preserve the original failure after all readers settle. */ }
                throw;
            }

            int exitCode = process.ExitCode;
            Record($"RoboCopy exit code: {exitCode}");
            Record($"Elapsed: {elapsed.Elapsed}");
            if (Volatile.Read(ref stopped) != 0)
                return Result(CopyOutcome.Cancelled, exitCode,
                    job.DryRun ? "Preview stopped. No files were changed." : job.Mode == CopyMode.Mirror
                    ? "Copy stopped. Some files may already have been copied or deleted."
                    : "Copy stopped. Some files may already have been copied.");
            if (stopError is not null)
            {
                details.AppendLine(stopError);
                return Result(CopyOutcome.NeedsAttention, exitCode, "The stop request couldn't be completed. Check the copy details.");
            }
            CopyOutcome outcome = InterpretExitCode(exitCode);
            string message = outcome switch
            {
                CopyOutcome.Failed => "Some files couldn't be copied. View Details to see what needs attention.",
                CopyOutcome.NeedsAttention => "The copy finished, but some files didn't match. View Details before relying on this copy.",
                _ => job.DryRun ? "Preview complete. No files were changed." : "Copy completed successfully."
            };
            return Result(outcome, exitCode, message);
        }
        catch (OperationCanceledException)
        {
            return Result(CopyOutcome.Cancelled, null, "Stopped before copying began. No files were changed.");
        }
        catch (JobValidationException ex)
        {
            details.AppendLine(ex.Message);
            return Result(CopyOutcome.Failed, null, ex.Message);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or Win32Exception or InvalidOperationException)
        {
            details.AppendLine(ex.ToString());
            return Result(CopyOutcome.Failed, null,
                "The copy couldn't finish. Check that both drives are connected and the folders are available, then try again.");
        }
        finally
        {
            if (job.Settings?.SaveLog != true && privateLogPath is not null)
            {
                try { File.Delete(privateLogPath); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
            }
        }
    }

    // Bit 1: files copied; bit 2: destination extras (expected in Copy and Mirror);
    // bit 4: mismatches; bit 8 or higher: failures/fatal errors.
    public static CopyOutcome InterpretExitCode(int exitCode) => exitCode < 0 || exitCode >= 8
        ? CopyOutcome.Failed
        : (exitCode & 4) != 0 ? CopyOutcome.NeedsAttention : CopyOutcome.Completed;

    private static async Task DrainDiscardAsync(Stream stream)
    {
        byte[] buffer = new byte[8192];
        while (await stream.ReadAsync(buffer.AsMemory()).ConfigureAwait(false) != 0) { }
    }

    private static async Task TailLogAsync(string path, Action<string> lineReceived, CancellationToken finished)
    {
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using StreamReader reader = new(stream, Encoding.Unicode, detectEncodingFromByteOrderMarks: true);
        char[] buffer = new char[4096];
        StringBuilder line = new();
        while (true)
        {
            int count = await reader.ReadAsync(buffer.AsMemory()).ConfigureAwait(false);
            if (count == 0)
            {
                if (finished.IsCancellationRequested) break;
                await Task.Delay(75, CancellationToken.None).ConfigureAwait(false);
                continue;
            }
            for (int i = 0; i < count; i++)
            {
                char character = buffer[i];
                if (character is '\r' or '\n')
                {
                    if (line.Length != 0)
                    {
                        lineReceived(line.ToString());
                        line.Clear();
                    }
                }
                else if (character != '\0' && character != '\uFEFF') line.Append(character);
            }
        }
        if (line.Length > 0) lineReceived(line.ToString());
    }
}
