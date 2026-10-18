using System.IO;
using System.Text.Json;
using FolderFlow.Core;

namespace FolderFlow.App.Services;

public sealed record Preferences
{
    public string Theme { get; init; } = "System";
    public CopySettings CopySettings { get; init; } = new();
    public string SourcePath { get; init; } = "";
    public string DestinationPath { get; init; } = "";
}

public static class PreferenceStore
{
    public static string DataDirectory => AppDataPaths.DataDirectory;

    public static Preferences Load(out string? warning)
    {
        warning = null;
        try
        {
            string path = Path.Combine(DataDirectory, "preferences.json");
            if (!File.Exists(path)) return new();
            var preferences = JsonSerializer.Deserialize<Preferences>(File.ReadAllText(path));
            if (preferences is null) throw new JsonException("Empty preferences.");
            return Normalize(preferences);
        }
        catch (Exception ex) when (IsPreferenceError(ex))
        {
            warning = "Your saved settings could not be opened. FolderFlow is using its usual defaults.";
            return new();
        }
    }

    public static bool Save(Preferences preferences, out string? warning)
    {
        warning = null;
        string? temporaryPath = null;
        try
        {
            Directory.CreateDirectory(DataDirectory);
            string path = Path.Combine(DataDirectory, "preferences.json");
            temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(Normalize(preferences),
                new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporaryPath, path, overwrite: true);
            return true;
        }
        catch (Exception ex) when (IsPreferenceError(ex))
        {
            warning = "Your settings work for this session, but could not be saved for next time.";
            return false;
        }
        finally
        {
            if (temporaryPath is not null)
            {
                try { File.Delete(temporaryPath); }
                catch (Exception ex) when (IsPreferenceError(ex)) { }
            }
        }
    }

    private static Preferences Normalize(Preferences preferences)
    {
        var settings = preferences.CopySettings ?? new CopySettings();
        return preferences with
        {
            Theme = preferences.Theme is "Light" or "Dark" ? preferences.Theme : "System",
            SourcePath = preferences.SourcePath ?? "",
            DestinationPath = preferences.DestinationPath ?? "",
            CopySettings = settings with
            {
                ThreadCount = 2,
                RetryCount = settings.RetryCount is 0 or 2 or 5 ? settings.RetryCount : 2,
                RetryWaitSeconds = 2
            }
        };
    }

    private static bool IsPreferenceError(Exception ex) =>
        ex is IOException or UnauthorizedAccessException or JsonException or NotSupportedException
            or System.Security.SecurityException or ArgumentException;
}
