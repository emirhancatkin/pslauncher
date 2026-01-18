using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.Win32;
using PsLauncher.Models;
using PsLauncher.Services;

namespace PsLauncher.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly StorageService _storage;
    private readonly LaunchService _launcher;

    private ObservableCollection<GameEntry> _games = new();
    public ObservableCollection<GameEntry> Games
    {
        get => _games;
        private set { _games = value; OnPropertyChanged(); }
    }

    private int _selectedIndex;
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (Games.Count == 0)
            {
                _selectedIndex = -1;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedGame));
                return;
            }

            if (value < 0) value = 0;
            if (value >= Games.Count) value = Games.Count - 1;
            if (_selectedIndex == value) return;

            _selectedIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedGame));
        }
    }

    public GameEntry? SelectedGame =>
        (SelectedIndex >= 0 && SelectedIndex < Games.Count) ? Games[SelectedIndex] : null;

    private AppSettings _settings = new();
    public AppSettings Settings
    {
        get => _settings;
        private set { _settings = value; OnPropertyChanged(); }
    }

    public string DataFolder => _storage.GetDataFolder();

    public MainViewModel(StorageService storage, LaunchService launcher)
    {
        _storage = storage;
        _launcher = launcher;
        _selectedIndex = 0;
    }

    public async Task InitializeAsync()
    {
        Settings = await _storage.LoadSettingsAsync();

        var games = await _storage.LoadGamesAsync();
        Games = new ObservableCollection<GameEntry>(games);

        SelectedIndex = Games.Count > 0 ? 0 : -1;

        if (Settings.AutoStartDs4OnAppLaunch)
            _launcher.StartDs4IfNeeded(Settings);
    }

    public async Task PersistAsync()
    {
        await _storage.SaveGamesAsync(Games);
        await _storage.SaveSettingsAsync(Settings);
    }


    public async Task AddGameAsync(Window owner)
    {
        var ofd = new OpenFileDialog
        {
            Title = "Game EXE seç",
            Filter = "Executable (*.exe)|*.exe",
            Multiselect = false
        };

        if (ofd.ShowDialog(owner) != true) return;

        var exe = ofd.FileName;
        var name = System.IO.Path.GetFileNameWithoutExtension(exe);

        var entry = new GameEntry
        {
            Name = name,
            ExePath = exe,
            Description = null,
            WorkingDirectory = System.IO.Path.GetDirectoryName(exe),
            CoverPath = null,
            RunDs4BeforeLaunch = true
        };

        Games.Add(entry);
        SelectedIndex = Games.Count - 1;
        await PersistAsync();
    }

    public async Task RemoveSelectedAsync()
    {
        var g = SelectedGame;
        if (g == null) return;
        var i = SelectedIndex;
        Games.Remove(g);
        SelectedIndex = Games.Count == 0 ? -1 : Math.Min(i, Games.Count - 1);
        await PersistAsync();
    }

    public async Task SetCoverAsync(Window owner)
    {
        var g = SelectedGame;
        if (g == null) return;

        var ofd = new OpenFileDialog
        {
            Title = "Cover gorseli sec",
            Filter = "Images (*.png;*.jpg;*.jpeg;*.webp)|*.png;*.jpg;*.jpeg;*.webp",
            Multiselect = false
        };

        if (ofd.ShowDialog(owner) != true) return;

        var coversDir = Path.Combine(_storage.GetDataFolder(), "covers");
        Directory.CreateDirectory(coversDir);

        var ext = Path.GetExtension(ofd.FileName);
        var dest = Path.Combine(coversDir, $"{g.Id}{ext}");
        File.Copy(ofd.FileName, dest, true);

        g.CoverPath = dest;
        await PersistAsync();
        OnPropertyChanged(nameof(SelectedGame));
    }

    public async Task ClearCoverAsync()
    {
        var g = SelectedGame;
        if (g == null) return;

        g.CoverPath = null;
        await PersistAsync();
        OnPropertyChanged(nameof(SelectedGame));
    }

    public async Task<Process?> LaunchSelectedAsync()
    {
        var g = SelectedGame;
        if (g == null) return null;

        if (g.RunDs4BeforeLaunch)
            _launcher.StartDs4IfNeeded(Settings);

        var proc = _launcher.LaunchGame(g);

        g.LastPlayedUtc = DateTime.UtcNow;
        await PersistAsync();
        OnPropertyChanged(nameof(SelectedGame));
        return proc;
    }

    public async Task SetDs4PathAsync(Window owner)
    {
        var ofd = new OpenFileDialog
        {
            Title = "DS4Windows.exe seç",
            Filter = "Executable (*.exe)|*.exe",
            Multiselect = false
        };
        if (ofd.ShowDialog(owner) != true) return;

        Settings.Ds4WindowsPath = ofd.FileName;
        await PersistAsync();
    }

    public void OpenDs4Now()
    {
        _launcher.StartDs4IfNeeded(Settings);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
