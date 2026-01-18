using SharpDX.DirectInput;

namespace PsLauncher.Services;

public sealed class DiGamepadService : IDisposable
{
    private readonly DirectInput _di = new();
    private Joystick? _js;
    private JoystickState? _prev;
    private bool _hasPrev;

    public string? DeviceName { get; private set; }

    public bool EnsureConnected()
    {
        try
        {

            var devices =
                _di.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AttachedOnly)
                  .Concat(_di.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AttachedOnly))
                  .ToList();

            if (devices.Count == 0)
            {
                Disconnect();
                return false;
            }


            var dev = devices[0];
            _js = new Joystick(_di, dev.InstanceGuid);
            DeviceName = dev.InstanceName;


            _js.Properties.BufferSize = 128;
            _js.Acquire();

            _hasPrev = false;
            return true;
        }
        catch
        {
            Disconnect();
            return false;
        }
    }

    public (bool connected, string? name, GamepadFrame frame) Poll()
    {
        if (_js == null)
        {
            if (!EnsureConnected())
                return (false, null, GamepadFrame.Empty);
        }

        if (_js == null) return (false, null, GamepadFrame.Empty);

        try
        {
            _js.Poll();
            var s = _js.GetCurrentState();
            if (s == null)
                return (false, DeviceName, GamepadFrame.Empty);

            bool btnPressed(int idx)
            {
                var buttons = s.Buttons;
                if (buttons == null || idx < 0 || idx >= buttons.Length) return false;

                var now = buttons[idx];
                var prev = _hasPrev && _prev != null && _prev.Buttons != null && idx < _prev.Buttons.Length && _prev.Buttons[idx];
                return now && !prev;
            }


            bool dpadLeft = false, dpadRight = false, dpadUp = false, dpadDown = false;
            var pov = s.PointOfViewControllers;
            if (pov != null && pov.Length > 0)
            {
               
                var v = pov[0];
                dpadRight = v == 9000;
                dpadLeft = v == 27000;
                dpadUp = v == 0;
                dpadDown = v == 18000;
            }

            bool prevDpadLeft = false, prevDpadRight = false, prevDpadUp = false, prevDpadDown = false;
            if (_hasPrev && _prev != null && _prev.PointOfViewControllers != null && _prev.PointOfViewControllers.Length > 0)
            {
                var pv = _prev.PointOfViewControllers[0];
                prevDpadRight = pv == 9000;
                prevDpadLeft = pv == 27000;
                prevDpadUp = pv == 0;
                prevDpadDown = pv == 18000;
            }


            bool rightEdge = dpadRight && !prevDpadRight;
            bool leftEdge = dpadLeft && !prevDpadLeft;
            bool upEdge = dpadUp && !prevDpadUp;
            bool downEdge = dpadDown && !prevDpadDown;


            var x = s.X;
            var y = s.Y;
            bool stickRight = x > 42000;
            bool stickLeft = x < 24000;
            bool stickUp = y < 24000;
            bool stickDown = y > 42000;

            bool prevStickRight = _hasPrev && _prev != null && _prev.X > 42000;
            bool prevStickLeft  = _hasPrev && _prev != null && _prev.X < 24000;
            bool prevStickUp = _hasPrev && _prev != null && _prev.Y < 24000;
            bool prevStickDown  = _hasPrev && _prev != null && _prev.Y > 42000;

            bool stickRightEdge = stickRight && !prevStickRight;
            bool stickLeftEdge  = stickLeft && !prevStickLeft;
            bool stickUpEdge = stickUp && !prevStickUp;
            bool stickDownEdge  = stickDown && !prevStickDown;

            var frame = new GamepadFrame
            {
                Left  = leftEdge || stickLeftEdge,
                Right = rightEdge || stickRightEdge,
                Up    = upEdge || stickUpEdge,
                Down  = downEdge || stickDownEdge,


                A = btnPressed(1),
                B = btnPressed(2),


                Start = btnPressed(9),


                Back = btnPressed(8),
            };

            _prev = s;
            _hasPrev = true;

            return (true, DeviceName, frame);
        }
        catch
        {
            Disconnect();
            return (false, null, GamepadFrame.Empty);
        }
    }

    private void Disconnect()
    {
        try
        {
            _js?.Unacquire();
            _js?.Dispose();
        }
        catch { /* ignore */ }

        _js = null;
        DeviceName = null;
        _hasPrev = false;
        _prev = null;
    }

    public void Dispose()
    {
        Disconnect();
        _di.Dispose();
    }
}
