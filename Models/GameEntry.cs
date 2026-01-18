namespace PsLauncher.Models;

public sealed class GameEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "New Game";
    public string ExePath { get; set; } = "";
    public string? Description { get; set; }
    public string? Arguments { get; set; }
    public string? WorkingDirectory { get; set; }

   
    public string? CoverPath { get; set; }

    public bool RunDs4BeforeLaunch { get; set; } = true;
    public DateTime? LastPlayedUtc { get; set; }
}
