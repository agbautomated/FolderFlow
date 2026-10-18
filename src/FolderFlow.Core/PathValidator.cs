using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace FolderFlow.Core;

public static class PathValidator
{
    public static ValidatedJob Validate(CopyJob job)
    {
        ArgumentNullException.ThrowIfNull(job);
        if (!Enum.IsDefined(job.Mode))
            throw new JobValidationException("Choose Copy files or Mirror folders.");
        if (job.Settings is null || job.Settings.RetryCount is < 0 or > 10 ||
            job.Settings.RetryWaitSeconds is < 0 or > 30 || job.Settings.ThreadCount is < 1 or > 4)
            throw new JobValidationException("The saved copy settings are invalid. Open Settings and choose the defaults.");

        string source = Normalize(job.SourcePath, "copy from");
        string destination = Normalize(job.DestinationPath, "copy to");
        if (IsRoot(source))
            throw new JobValidationException("Choose a folder inside your source drive, rather than the whole drive.");
        if (IsRoot(destination))
            throw new JobValidationException("Choose a folder inside the destination drive, rather than the whole drive.");

        RejectReparseAncestors(source);
        RejectReparseAncestors(destination);
        // Comparing physical volume paths also catches ordinary SUBST drive aliases.
        string canonicalSource = GetCanonicalDirectory(source);
        string canonicalDestination = GetCanonicalDirectory(destination);
        if (IsSameOrWithin(canonicalSource, canonicalDestination) || IsSameOrWithin(canonicalDestination, canonicalSource))
            throw new JobValidationException("Choose two separate folders. One cannot be inside the other, or be the same folder.");

        RejectProtectedDestination(canonicalDestination, job.Mode);
        return new ValidatedJob(job, source, destination, canonicalSource, canonicalDestination);
    }

    private static string Normalize(string? value, string purpose)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new JobValidationException($"Choose the folder to {purpose}.");
        string path = value.Trim();
        // The first release intentionally accepts local drive paths only. Device paths,
        // network shares, drive-relative paths, streams and shell syntax are never accepted.
        if (path.Length < 3 || !char.IsAsciiLetter(path[0]) || path[1] != ':' ||
            (path[2] != '\\' && path[2] != '/') || path.AsSpan(2).Contains(':') ||
            path.IndexOfAny(['\0', '"', '*', '?', '<', '>', '|']) >= 0)
            throw new JobValidationException("Choose a local folder using its full drive path, such as E:\\Photos.");
        foreach (string part in path[3..].Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (part.EndsWith(' ') || part.EndsWith('.') || part.Any(char.IsControl))
                throw new JobValidationException("This folder path contains an unsupported name. Choose it again with the folder picker.");
        }
        try
        {
            path = Path.GetFullPath(path);
            if (!Directory.Exists(path))
                throw new JobValidationException($"The folder to {purpose} isn't available. Connect the drive and choose an existing folder.");
            return Path.TrimEndingDirectorySeparator(path);
        }
        catch (JobValidationException) { throw; }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            throw new JobValidationException($"The folder to {purpose} can't be opened. Choose an existing folder you can access.");
        }
    }

    private static bool IsRoot(string path) =>
        string.Equals(Path.TrimEndingDirectorySeparator(path), Path.TrimEndingDirectorySeparator(Path.GetPathRoot(path)!), StringComparison.OrdinalIgnoreCase);

    public static bool IsSameOrWithin(string candidate, string parent)
    {
        candidate = Path.TrimEndingDirectorySeparator(candidate);
        parent = Path.TrimEndingDirectorySeparator(parent);
        return candidate.Equals(parent, StringComparison.OrdinalIgnoreCase) ||
            candidate.StartsWith(parent.EndsWith('\\') ? parent : parent + "\\", StringComparison.OrdinalIgnoreCase);
    }

    private static void RejectReparseAncestors(string path)
    {
        try
        {
            for (DirectoryInfo? directory = new(path); directory is not null; directory = directory.Parent)
                if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new JobValidationException("Choose the folder's original location. Linked folders and junctions aren't supported as selected folders.");
        }
        catch (JobValidationException) { throw; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new JobValidationException("We couldn't safely check this folder. Choose a folder you can access.");
        }
    }

    private static void RejectProtectedDestination(string destination, CopyMode mode)
    {
        string[] systemFolders = [
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
        ];
        foreach (string folder in systemFolders.Where(x => !string.IsNullOrEmpty(x) && Directory.Exists(x)))
        {
            string canonical = GetCanonicalDirectory(folder);
            if (IsSameOrWithin(destination, canonical) || IsSameOrWithin(canonical, destination))
                throw new JobValidationException("Choose a destination away from Windows and application folders to keep your computer safe.");
        }
        if (mode != CopyMode.Mirror) return;
        string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(profile) && Directory.Exists(profile) &&
            IsSameOrWithin(GetCanonicalDirectory(profile), destination))
            throw new JobValidationException("Choose a specific folder for mirroring, rather than your whole user folder.");
    }

    // A destination link can redirect writes as well as deletions outside the
    // selected folder. Both modes fail closed when destination links are present.
    // Do this on the worker thread, never in the folder-picker event handler.
    internal static void CheckDestinationTree(ValidatedJob job, CancellationToken cancellationToken)
    {
        try
        {
            Stack<string> pending = new();
            pending.Push(job.DestinationPath);
            while (pending.TryPop(out string? directory))
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (FileSystemInfo entry in new DirectoryInfo(directory).EnumerateFileSystemInfos())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if ((entry.Attributes & FileAttributes.ReparsePoint) != 0)
                        throw new JobValidationException("This destination contains a linked file or folder. Choose a destination without links to keep the copy inside that folder.");
                    if ((entry.Attributes & FileAttributes.Directory) != 0) pending.Push(entry.FullName);
                }
            }
        }
        catch (JobValidationException) { throw; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new JobValidationException("We couldn't check every destination folder safely. Choose a destination you can read.");
        }
    }

    // Resolve the nearest existing parent before creating private app files. This
    // keeps data-directory drive aliases from placing a log inside the copy target.
    internal static string GetProspectiveCanonicalDirectory(string path)
    {
        DirectoryInfo? existing = new(path);
        Stack<string> suffix = new();
        while (existing is not null && !existing.Exists)
        {
            suffix.Push(existing.Name);
            existing = existing.Parent;
        }
        if (existing is null) throw new IOException("The data directory has no available parent.");
        string canonical = GetCanonicalDirectory(existing.FullName);
        while (suffix.TryPop(out string? part)) canonical = Path.Combine(canonical, part);
        return canonical;
    }

    private static string GetCanonicalDirectory(string path)
    {
        using SafeFileHandle handle = CreateFileW(path, 0, FileShare.ReadWrite | FileShare.Delete,
            IntPtr.Zero, 3, 0x02000000, IntPtr.Zero); // OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS
        if (handle.IsInvalid)
            throw new JobValidationException("We couldn't open this folder safely. Check its permissions and try again.");
        StringBuilder buffer = new(512);
        uint length = GetFinalPathNameByHandleW(handle, buffer, (uint)buffer.Capacity, 1); // VOLUME_NAME_GUID
        if (length >= buffer.Capacity)
        {
            buffer.EnsureCapacity(checked((int)length + 1));
            length = GetFinalPathNameByHandleW(handle, buffer, (uint)buffer.Capacity, 1);
        }
        if (length == 0)
            throw new JobValidationException("We couldn't identify this drive safely. Use a local folder on a connected drive.");
        return Path.TrimEndingDirectorySeparator(buffer.ToString());
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFileW(string name, uint access, FileShare share,
        IntPtr securityAttributes, uint creationDisposition, uint flags, IntPtr templateFile);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetFinalPathNameByHandleW(SafeFileHandle handle, StringBuilder path, uint length, uint flags);
}
