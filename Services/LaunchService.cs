using System.Diagnostics;
using System.IO;
using PsLauncher.Models;

namespace PsLauncher.Services;

public sealed class LaunchService
{
    private static bool IsProcessRunning(string processNameNoExe)
    {
        var name = Path.GetFileNameWithoutExtension(processNameNoExe);
        return Process.GetProcessesByName(name).Length > 0;
    }

    public void StartDs4IfNeeded(AppSettings settings)
    {
        var ds4Path = settings.Ds4WindowsPath;
        if (string.IsNullOrWhiteSpace(ds4Path)) return;
        if (!File.Exists(ds4Path)) return;

        if (settings.DoNotRestartDs4IfRunning)
        {
            if (IsProcessRunning("DS4Windows"))
                return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = ds4Path,
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(ds4Path) ?? ""
        });
    }

    public Process? LaunchGame(Models.GameEntry game)
    {
        if (string.IsNullOrWhiteSpace(game.ExePath) || !File.Exists(game.ExePath))
            throw new FileNotFoundException("Game exe not found.", game.ExePath);

        var psi = new ProcessStartInfo
        {
            FileName = game.ExePath,
            UseShellExecute = true,
        };

        if (!string.IsNullOrWhiteSpace(game.Arguments))
            psi.Arguments = game.Arguments;

        var wd = game.WorkingDirectory;
        if (!string.IsNullOrWhiteSpace(wd) && Directory.Exists(wd))
            psi.WorkingDirectory = wd;
        else
            psi.WorkingDirectory = Path.GetDirectoryName(game.ExePath) ?? "";

        return Process.Start(psi);
    }
}
