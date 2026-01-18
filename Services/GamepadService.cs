using SharpDX.XInput;

namespace PsLauncher.Services;

public sealed class GamepadService : IDisposable
{
    private readonly (UserIndex idx, Controller ctl)[] _ctls =
    [
        (UserIndex.One,   new Controller(UserIndex.One)),
        (UserIndex.Two,   new Controller(UserIndex.Two)),
        (UserIndex.Three, new Controller(UserIndex.Three)),
        (UserIndex.Four,  new Controller(UserIndex.Four)),
    ];

    private Controller? _active;
    public UserIndex? ActiveIndex { get; private set; }

    private State _prev;
    private bool _hasPrev;

    public bool EnsureConnected()
    {
        foreach (var (idx, ctl) in _ctls)
        {
            if (!ctl.IsConnected) continue;
            _active = ctl;
            ActiveIndex = idx;
            return true;
        }

        _active = null;
        ActiveIndex = null;
        _hasPrev = false;
        return false;
    }

    public (bool connected, UserIndex? index, GamepadFrame frame) Poll()
    {
        if (_active == null || !_active.IsConnected)
        {
            if (!EnsureConnected())
                return (false, null, GamepadFrame.Empty);
        }

        try
        {
            var s = _active!.GetState();
            var buttons = s.Gamepad.Buttons;

            bool pressed(GamepadButtonFlags b)
            {
                var now = (buttons & b) != 0;
                var prev = _hasPrev && ((_prev.Gamepad.Buttons & b) != 0);
                return now && !prev;
            }


            var lx = s.Gamepad.LeftThumbX;
            var ly = s.Gamepad.LeftThumbY;

            bool leftStickRight = lx > 12000;
            bool leftStickLeft  = lx < -12000;
            bool leftStickUp    = ly > 12000;
            bool leftStickDown  = ly < -12000;

            bool prevLeftStickRight = _hasPrev && _prev.Gamepad.LeftThumbX > 12000;
            bool prevLeftStickLeft  = _hasPrev && _prev.Gamepad.LeftThumbX < -12000;
            bool prevLeftStickUp    = _hasPrev && _prev.Gamepad.LeftThumbY > 12000;
            bool prevLeftStickDown  = _hasPrev && _prev.Gamepad.LeftThumbY < -12000;

            bool rightEdge = leftStickRight && !prevLeftStickRight;
            bool leftEdge  = leftStickLeft  && !prevLeftStickLeft;
            bool upEdge    = leftStickUp    && !prevLeftStickUp;
            bool downEdge  = leftStickDown  && !prevLeftStickDown;

            var frame = new GamepadFrame
            {
                Left  = pressed(GamepadButtonFlags.DPadLeft)  || leftEdge,
                Right = pressed(GamepadButtonFlags.DPadRight) || rightEdge,
                Up    = pressed(GamepadButtonFlags.DPadUp)    || upEdge,
                Down  = pressed(GamepadButtonFlags.DPadDown)  || downEdge,
                A     = pressed(GamepadButtonFlags.A),
                B     = pressed(GamepadButtonFlags.B),
                Start = pressed(GamepadButtonFlags.Start),
                Back  = pressed(GamepadButtonFlags.Back),
            };

            _prev = s;
            _hasPrev = true;

            return (true, ActiveIndex, frame);
        }
        catch
        {
            _active = null;
            ActiveIndex = null;
            _hasPrev = false;
            return (false, null, GamepadFrame.Empty);
        }
    }

    public void Dispose() { }
}

public readonly struct GamepadFrame
{
    public bool Left { get; init; }
    public bool Right { get; init; }
    public bool Up { get; init; }
    public bool Down { get; init; }
    public bool A { get; init; }
    public bool B { get; init; }
    public bool Start { get; init; }
    public bool Back { get; init; }

    public static GamepadFrame Empty => new();
}
