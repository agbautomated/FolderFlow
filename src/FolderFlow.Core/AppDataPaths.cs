using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace FolderFlow.Core;

/// <summary>Shared, physical storage paths for the UI and its RoboCopy child process.</summary>
public static class AppDataPaths
{
    public static string DataDirectory
    {
        get
        {
            // A workspace override keeps integration checks away from personal settings.
            if (Environment.GetEnvironmentVariable("FOLDERFLOW_DATA_DIR") is { Length: > 0 } configured)
                return Path.GetFullPath(configured);

            string? packageFamilyName = GetPackageFamilyName();
            string localData = packageFamilyName is null
                ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                : GetUnredirectedLocalAppData();
            // Use an explicit package-owned path instead of a virtualized portable-app path.
            // RoboCopy and an external log viewer can then open the same physical file.
            return Path.Combine(packageFamilyName is null ? localData :
                Path.Combine(localData, "Packages", packageFamilyName, "LocalState"), "FolderFlow");
        }
    }

    private static string GetUnredirectedLocalAppData()
    {
        // Environment.GetFolderPath uses default known-folder flags, whose packaged
        // result can be redirected. Explicitly ask for the user's ordinary root.
        Guid localAppData = new("F1B32785-6FBA-4FCF-9D55-7B8E7F157091");
        const uint noPackageRedirection = 0x00010000;
        IntPtr pathPointer = IntPtr.Zero;
        try
        {
            int result = SHGetKnownFolderPath(in localAppData, noPackageRedirection, IntPtr.Zero, out pathPointer);
            if (result < 0)
                throw new IOException("Windows could not find FolderFlow's local data folder.", Marshal.GetExceptionForHR(result));
            return Marshal.PtrToStringUni(pathPointer)
                ?? throw new IOException("Windows returned an empty local data folder.");
        }
        finally { Marshal.FreeCoTaskMem(pathPointer); }
    }

    private static string? GetPackageFamilyName()
    {
        if (!OperatingSystem.IsWindows()) return null;
        const int noPackage = 15700; // APPMODEL_ERROR_NO_PACKAGE
        const int insufficientBuffer = 122;
        uint length = 0;
        int result = GetCurrentPackageFamilyName(ref length, null);
        if (result == noPackage) return null;
        if (result != insufficientBuffer)
            throw new IOException("Windows could not determine FolderFlow's package identity.", new Win32Exception(result));
        var familyName = new StringBuilder(checked((int)length));
        result = GetCurrentPackageFamilyName(ref length, familyName);
        if (result != 0)
            throw new IOException("Windows could not read FolderFlow's package identity.", new Win32Exception(result));
        return familyName.ToString();
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int GetCurrentPackageFamilyName(ref uint packageFamilyNameLength, StringBuilder? packageFamilyName);

    [DllImport("shell32.dll", ExactSpelling = true)]
    private static extern int SHGetKnownFolderPath(in Guid folderId, uint flags, IntPtr token, out IntPtr path);
}
