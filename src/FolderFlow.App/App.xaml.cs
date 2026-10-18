using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace FolderFlow.App;

public partial class App : Application
{
    public static string ThemePreference { get; private set; } = "System";

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ApplyTheme("System");
        SystemEvents.UserPreferenceChanged += OnSystemPreferenceChanged;
    }

    private void OnSystemPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (ThemePreference == "System") Dispatcher.BeginInvoke(() => ApplyTheme("System"));
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SystemEvents.UserPreferenceChanged -= OnSystemPreferenceChanged;
        base.OnExit(e);
    }

    public static void ApplyTheme(string preference)
    {
        ThemePreference = preference;
        bool dark = preference == "Dark";
        if (preference == "System")
        {
            try
            {
                dark = (int?)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1) == 0;
            }
            catch { dark = false; }
        }
        var palette = new Dictionary<string, string>
        {
            ["BackgroundBrush"] = dark ? "#151B18" : "#F5F6F8",
            ["SurfaceBrush"] = dark ? "#202923" : "#FFFFFF",
            ["TextBrush"] = dark ? "#EEF4EF" : "#18251F",
            ["MutedBrush"] = dark ? "#AFBDB3" : "#626E68",
            ["BorderBrush"] = dark ? "#46534A" : "#DCE2DE",
            ["AccentBrush"] = dark ? "#91D4AD" : "#246D50",
            ["AccentTextBrush"] = dark ? "#11281B" : "#FFFFFF",
            ["SelectedBrush"] = dark ? "#293C30" : "#EEF7F0",
            ["WarningBrush"] = dark ? "#EFBC7D" : "#995A16"
        };
        foreach (var (key, value) in palette)
            Current.Resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
    }
}
