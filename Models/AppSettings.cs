namespace PsLauncher.Models;

public sealed class AppSettings
{
    public string? Ds4WindowsPath { get; set; }
    public bool AutoStartDs4OnAppLaunch { get; set; } = false;
    public bool DoNotRestartDs4IfRunning { get; set; } = true;
}
