using UnityEngine;
using UnityEngine.InputSystem;

// One simulation tick of controls. Directions are held state; buttons are presses since the last tick.
public struct InputFrame
{
    public int x;
    public bool up, down;
    public bool light, heavy, kick, grab;
    public bool upPressed, downPressed, leftPressed, rightPressed;
    // Double-tap left / right (dash); the fighter resolves it into a forward or back dash.
    public bool dashLeft, dashRight;
    public bool confirm, cancel, start;

    public bool AnyAttack => light || heavy || kick;
}

public interface IInputSource
{
    // Called every rendered frame so short taps between ticks are never lost.
    void Sample();
    InputFrame Consume();
}

public class KeyBindings
{
    public Key left, right, up, down, light, heavy, kick, grab, start;
    public Key[] altLight = new Key[0], altHeavy = new Key[0], altKick = new Key[0], altGrab = new Key[0];

    // P1: WASD + F/G/H, R grab. Enter / F confirm, G back.
    public static KeyBindings PlayerOne => new KeyBindings
    {
        left = Key.A, right = Key.D, up = Key.W, down = Key.S,
        light = Key.F, heavy = Key.G, kick = Key.H, grab = Key.R, start = Key.Enter,
        altLight = new[] { Key.Space }
    };

    // P2: arrows + K/L/; (Ñ on a Spanish layout), P grab, with numpad alternatives.
    public static KeyBindings PlayerTwo => new KeyBindings
    {
        left = Key.LeftArrow, right = Key.RightArrow, up = Key.UpArrow, down = Key.DownArrow,
        light = Key.K, heavy = Key.L, kick = Key.Semicolon, grab = Key.P, start = Key.NumpadEnter,
        altLight = new[] { Key.Numpad1 }, altHeavy = new[] { Key.Numpad2 }, altKick = new[] { Key.Numpad3 },
        altGrab = new[] { Key.Numpad0 }
    };
}

public class KeyboardInput : IInputSource
{
    private const float DoubleTapSeconds = 0.22f;
    private readonly KeyBindings keys;
    private InputFrame pending;
    private float lastLeft = -1f, lastRight = -1f;

    public KeyboardInput(KeyBindings keys)
    {
        this.keys = keys;
    }

    public void Sample()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        bool left = keyboard[keys.left].isPressed;
        bool right = keyboard[keys.right].isPressed;
        pending.x = (right ? 1 : 0) - (left ? 1 : 0);
        pending.up = keyboard[keys.up].isPressed;
        pending.down = keyboard[keys.down].isPressed;

        pending.upPressed |= keyboard[keys.up].wasPressedThisFrame;
        pending.downPressed |= keyboard[keys.down].wasPressedThisFrame;
        bool leftTap = keyboard[keys.left].wasPressedThisFrame;
        bool rightTap = keyboard[keys.right].wasPressedThisFrame;
        pending.leftPressed |= leftTap;
        pending.rightPressed |= rightTap;
        float now = Time.unscaledTime;
        if (leftTap)
        {
            pending.dashLeft |= lastLeft >= 0f && now - lastLeft < DoubleTapSeconds;
            lastLeft = pending.dashLeft ? -1f : now;
            lastRight = -1f;
        }
        if (rightTap)
        {
            pending.dashRight |= lastRight >= 0f && now - lastRight < DoubleTapSeconds;
            lastRight = pending.dashRight ? -1f : now;
            lastLeft = -1f;
        }

        bool light = Pressed(keyboard, keys.light, keys.altLight);
        bool heavy = Pressed(keyboard, keys.heavy, keys.altHeavy);
        pending.light |= light;
        pending.heavy |= heavy;
        pending.kick |= Pressed(keyboard, keys.kick, keys.altKick);
        pending.grab |= Pressed(keyboard, keys.grab, keys.altGrab);
        bool start = keyboard[keys.start].wasPressedThisFrame;
        pending.start |= start;
        pending.confirm |= light || start;
        pending.cancel |= heavy;
    }

    public InputFrame Consume()
    {
        InputFrame frame = pending;
        pending = new InputFrame { x = frame.x, up = frame.up, down = frame.down };
        return frame;
    }

    private static bool Pressed(Keyboard keyboard, Key main, Key[] alternatives)
    {
        if (keyboard[main].wasPressedThisFrame)
            return true;
        foreach (Key key in alternatives)
            if (keyboard[key].wasPressedThisFrame)
                return true;
        return false;
    }
}

// Decides which side each connected gamepad drives. The first pad goes to `firstPadSide` (always P1 against the
// CPU), a second pad to the other side. Keyboards keep working alongside, so pad + keyboard is the default 2P setup.
public class PadAssignment
{
    public int firstPadSide;
    public bool vsCpu;

    public Gamepad For(int side)
    {
        int first = vsCpu ? 0 : firstPadSide;
        int index = side == first ? 0 : 1;
        var pads = Gamepad.all;
        return index < pads.Count ? pads[index] : null;
    }
}

public class GamepadInput : IInputSource
{
    private const float DoubleTapSeconds = 0.22f;
    private const float StickDeadZone = 0.5f;
    private readonly PadAssignment assignment;
    private readonly int side;
    private Gamepad device;
    private bool wasLeft, wasRight, wasUp, wasDown;
    private bool wasWest, wasNorth, wasSouth, wasEast, wasShoulder, wasStart;
    private InputFrame pending;
    private float lastLeft = -1f, lastRight = -1f;

    public GamepadInput(PadAssignment assignment, int side)
    {
        this.assignment = assignment;
        this.side = side;
    }

    public void Sample()
    {
        Gamepad pad = assignment.For(side);
        if (pad != device)
        {
            // Pad swapped or unplugged: drop held state so nothing sticks.
            device = pad;
            wasLeft = wasRight = wasUp = wasDown = false;
            // Treat buttons as held so a press made on the old pad (or before plugging in) does not fire.
            wasWest = wasNorth = wasSouth = wasEast = wasShoulder = wasStart = true;
            pending.x = 0;
            pending.up = pending.down = false;
        }
        if (pad == null)
            return;

        Vector2 stick = pad.leftStick.ReadValue();
        bool left = pad.dpad.left.isPressed || stick.x < -StickDeadZone;
        bool right = pad.dpad.right.isPressed || stick.x > StickDeadZone;
        bool up = pad.dpad.up.isPressed || stick.y > StickDeadZone;
        bool down = pad.dpad.down.isPressed || stick.y < -StickDeadZone;
        pending.x = (right ? 1 : 0) - (left ? 1 : 0);
        pending.up = up;
        pending.down = down;

        pending.upPressed |= up && !wasUp;
        pending.downPressed |= down && !wasDown;
        bool leftTap = left && !wasLeft;
        bool rightTap = right && !wasRight;
        wasLeft = left;
        wasRight = right;
        wasUp = up;
        wasDown = down;
        pending.leftPressed |= leftTap;
        pending.rightPressed |= rightTap;
        float now = Time.unscaledTime;
        if (leftTap)
        {
            pending.dashLeft |= lastLeft >= 0f && now - lastLeft < DoubleTapSeconds;
            lastLeft = pending.dashLeft ? -1f : now;
            lastRight = -1f;
        }
        if (rightTap)
        {
            pending.dashRight |= lastRight >= 0f && now - lastRight < DoubleTapSeconds;
            lastRight = pending.dashRight ? -1f : now;
            lastLeft = -1f;
        }

        // Xbox layout names: X light, Y heavy, A kick, B / RB grab. A or Start confirms, B goes back.
        // Edges are tracked here (not wasPressedThisFrame) so a pad swap never replays a stale press.
        bool light = Edge(ref wasWest, pad.buttonWest.isPressed);
        bool heavy = Edge(ref wasNorth, pad.buttonNorth.isPressed);
        bool kick = Edge(ref wasSouth, pad.buttonSouth.isPressed);
        bool back = Edge(ref wasEast, pad.buttonEast.isPressed);
        bool shoulder = Edge(ref wasShoulder, pad.rightShoulder.isPressed);
        bool start = Edge(ref wasStart, pad.startButton.isPressed);
        pending.light |= light;
        pending.heavy |= heavy;
        pending.kick |= kick;
        pending.grab |= back || shoulder;
        pending.start |= start;
        pending.confirm |= kick || start;
        pending.cancel |= back;
    }

    private static bool Edge(ref bool was, bool now)
    {
        bool pressed = now && !was;
        was = now;
        return pressed;
    }

    public InputFrame Consume()
    {
        InputFrame frame = pending;
        pending = new InputFrame { x = frame.x, up = frame.up, down = frame.down };
        return frame;
    }
}

// Merges several sources (one player's keyboard + gamepad, or menus where either player can navigate).
public class MergedInput : IInputSource
{
    private readonly IInputSource[] sources;

    public MergedInput(params IInputSource[] sources)
    {
        this.sources = sources;
    }

    public void Sample()
    {
        foreach (IInputSource source in sources)
            source.Sample();
    }

    public InputFrame Consume()
    {
        InputFrame merged = default;
        foreach (IInputSource source in sources)
        {
            InputFrame frame = source.Consume();
            merged.x = merged.x != 0 ? merged.x : frame.x;
            merged.up |= frame.up;
            merged.down |= frame.down;
            merged.light |= frame.light;
            merged.heavy |= frame.heavy;
            merged.kick |= frame.kick;
            merged.grab |= frame.grab;
            merged.upPressed |= frame.upPressed;
            merged.downPressed |= frame.downPressed;
            merged.leftPressed |= frame.leftPressed;
            merged.rightPressed |= frame.rightPressed;
            merged.dashLeft |= frame.dashLeft;
            merged.dashRight |= frame.dashRight;
            merged.confirm |= frame.confirm;
            merged.cancel |= frame.cancel;
            merged.start |= frame.start;
        }
        return merged;
    }
}

// Deterministic input for automated captures and tests.
public class ScriptedInput : IInputSource
{
    private readonly System.Func<int, InputFrame> script;
    private int tick;

    public ScriptedInput(System.Func<int, InputFrame> script)
    {
        this.script = script;
    }

    public void Sample()
    {
    }

    public InputFrame Consume()
    {
        return script(tick++);
    }
}
