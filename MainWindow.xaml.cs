using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using PsLauncher.Services;
using PsLauncher.ViewModels;

namespace PsLauncher;

public partial class MainWindow : Window
{
    private readonly StorageService _storage = new();
    private readonly LaunchService _launcher = new();


    private readonly GamepadService _xinput = new();


    private readonly DiGamepadService _dinput = new();

    private readonly MainViewModel _vm;

    private readonly DispatcherTimer _pollTimer;
    private DateTime _lastMove = DateTime.MinValue;
    private bool _isFullScreen;
    private WindowState _prevState;
    private WindowStyle _prevStyle;
    private ResizeMode _prevResizeMode;
    private FrameworkElement[] _topButtons = Array.Empty<FrameworkElement>();
    private int _topButtonIndex;
    private bool _focusOnList = true;

    public MainWindow()
    {
        InitializeComponent();

        _vm = new MainViewModel(_storage, _launcher);
        DataContext = _vm;

        _pollTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _pollTimer.Tick += (_, __) => PollGamepad();
        _pollTimer.Start();

        Loaded += (_, __) =>
        {
            BuildFocusMap();
            FocusSelected();
            SetFullscreen(true);
            TryPlayIntro();
        };

        Activated += (_, __) =>
        {
            if (!_pollTimer.IsEnabled)
                _pollTimer.Start();
        };

        Deactivated += (_, __) =>
        {
            if (_pollTimer.IsEnabled)
                _pollTimer.Stop();
        };

        Closed += async (_, __) =>
        {
            _xinput.Dispose();
            _dinput.Dispose();
            await _vm.PersistAsync();
        };
    }

    private void PollGamepad()
    {
        if (!IsActive || WindowState == WindowState.Minimized)
            return;


        var (xConnected, xIndex, xFrame) = _xinput.Poll();
        if (xConnected)
        {
            GamepadStatus.Text = $"Gamepad: XInput Connected ({xIndex})";
            HandleFrame(xFrame);
            return;
        }


        var (dConnected, dName, dFrame) = _dinput.Poll();
        if (dConnected)
        {
            GamepadStatus.Text = $"Gamepad: DirectInput Connected ({dName})";
            HandleFrame(dFrame);
            return;
        }

        GamepadStatus.Text = "Gamepad: Not connected (XInput/DirectInput yok)";
    }

    private void HandleFrame(GamepadFrame frame)
    {
        bool canMove = (DateTime.UtcNow - _lastMove) > TimeSpan.FromMilliseconds(140);

        if (canMove && frame.Right)
        {
            if (_focusOnList)
            {
                _vm.SelectedIndex++;
                _lastMove = DateTime.UtcNow;
                FocusSelected();
            }
            else
            {
                MoveTopButton(1);
                _lastMove = DateTime.UtcNow;
            }
        }
        else if (canMove && frame.Left)
        {
            if (_focusOnList)
            {
                _vm.SelectedIndex--;
                _lastMove = DateTime.UtcNow;
                FocusSelected();
            }
            else
            {
                MoveTopButton(-1);
                _lastMove = DateTime.UtcNow;
            }
        }

        if (canMove && frame.Up)
        {
            FocusTopButton(_topButtonIndex);
            _lastMove = DateTime.UtcNow;
        }
        else if (canMove && frame.Down)
        {
            FocusList();
            _lastMove = DateTime.UtcNow;
        }

        if (frame.A)
            ActivateFocused();

        if (frame.B)
            FocusList();

        if (frame.Start)
            _vm.OpenDs4Now();
    }

    private void FocusSelected()
    {
        if (GamesList.Items.Count == 0) return;
        GamesList.ScrollIntoView(GamesList.SelectedItem);
        GamesList.Focus();
    }

    public async Task InitializeDataAsync()
    {
        await _vm.InitializeAsync();
    }

    private void BuildFocusMap()
    {
        _topButtons =
        [
            Ds4Button,
            FullscreenButton,
            AddGameButton
        ];
    }

    private void FocusTopButton(int index)
    {
        if (_topButtons.Length == 0) return;
        _focusOnList = false;
        _topButtonIndex = Math.Clamp(index, 0, _topButtons.Length - 1);
        _topButtons[_topButtonIndex].Focus();
    }

    private void MoveTopButton(int delta)
    {
        if (_topButtons.Length == 0) return;
        var next = (_topButtonIndex + delta + _topButtons.Length) % _topButtons.Length;
        FocusTopButton(next);
    }

    private void FocusList()
    {
        _focusOnList = true;
        FocusSelected();
    }

    private void ActivateFocused()
    {
        if (_focusOnList)
        {
            _ = SafeLaunchAsync();
            return;
        }

        if (_topButtons.Length == 0) return;
        if (_topButtons[_topButtonIndex] is Button btn)
            btn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    }

    private void TryPlayIntro()
    {
        var introPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "intro.mp4");
        if (!File.Exists(introPath))
            return;

        IntroOverlay.Visibility = Visibility.Visible;
        IntroVideo.Source = new Uri(introPath);
        IntroVideo.Play();
    }

    private void EndIntro()
    {
        if (IntroOverlay.Visibility != Visibility.Visible)
            return;

        IntroVideo.Stop();
        IntroOverlay.Visibility = Visibility.Collapsed;
    }

    private async Task SafeLaunchAsync()
    {
        try
        {
            var proc = await _vm.LaunchSelectedAsync();
            if (_pollTimer.IsEnabled)
                _pollTimer.Stop();
            WindowState = WindowState.Minimized;

            if (proc != null)
            {
                try
                {
                    proc.EnableRaisingEvents = true;
                    proc.Exited += (_, __) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (WindowState == WindowState.Minimized)
                                WindowState = WindowState.Normal;
                            Activate();
                            if (!_pollTimer.IsEnabled)
                                _pollTimer.Start();
                        });
                    };
                }
                catch
                {
                    if (!_pollTimer.IsEnabled)
                        _pollTimer.Start();
                }
            }
            else
            {
                if (!_pollTimer.IsEnabled)
                    _pollTimer.Start();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Launch error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Launch_Click(object sender, RoutedEventArgs e) => await SafeLaunchAsync();
    private async void Add_Click(object sender, RoutedEventArgs e) => await _vm.AddGameAsync(this);
    private async void Remove_Click(object sender, RoutedEventArgs e) => await _vm.RemoveSelectedAsync();
    private async void SetCover_Click(object sender, RoutedEventArgs e) => await _vm.SetCoverAsync(this);
    private async void ClearCover_Click(object sender, RoutedEventArgs e) => await _vm.ClearCoverAsync();

    private async void PickDs4_Click(object sender, RoutedEventArgs e) => await _vm.SetDs4PathAsync(this);
    private void OpenDs4_Click(object sender, RoutedEventArgs e) => _vm.OpenDs4Now();
    private void ToggleFullscreen_Click(object sender, RoutedEventArgs e) => ToggleFullscreen();

    private void ToggleFullscreen() => SetFullscreen(!_isFullScreen);

    private void SetFullscreen(bool enable)
    {
        if (enable && !_isFullScreen)
        {
            _prevState = WindowState;
            _prevStyle = WindowStyle;
            _prevResizeMode = ResizeMode;

            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
            _isFullScreen = true;
        }
        else if (!enable && _isFullScreen)
        {
            WindowStyle = _prevStyle;
            ResizeMode = _prevResizeMode;
            WindowState = _prevState;
            _isFullScreen = false;
        }
    }

    private async void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (IntroOverlay.Visibility == Visibility.Visible)
        {
            EndIntro();
            e.Handled = true;
            return;
        }

        switch (e.Key)
        {
            case Key.Right:
                if (_focusOnList)
                {
                    _vm.SelectedIndex++;
                    FocusSelected();
                }
                else
                {
                    MoveTopButton(1);
                }
                break;
            case Key.Left:
                if (_focusOnList)
                {
                    _vm.SelectedIndex--;
                    FocusSelected();
                }
                else
                {
                    MoveTopButton(-1);
                }
                break;
            case Key.Up:
                FocusTopButton(_topButtonIndex);
                break;
            case Key.Down:
                FocusList();
                break;
            case Key.Enter:
                ActivateFocused();
                break;
            case Key.N:
                await _vm.AddGameAsync(this);
                break;
            case Key.Delete:
                await _vm.RemoveSelectedAsync();
                break;
            case Key.F1:
                _vm.OpenDs4Now();
                break;
            case Key.F11:
                ToggleFullscreen();
                break;
        }
    }

    private void IntroVideo_MediaEnded(object sender, RoutedEventArgs e) => EndIntro();
}
