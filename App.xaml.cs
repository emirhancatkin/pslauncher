using System.Windows;

namespace PsLauncher;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var window = new MainWindow();
        MainWindow = window;
        await window.InitializeDataAsync();
        window.Show();
    }
}
