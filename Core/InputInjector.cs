using System.Runtime.InteropServices;

namespace Core;

/// <summary>
/// Provides input injection functionality for mouse and keyboard events
/// </summary>
public class InputInjector
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public int type;
        public INPUTUNION u;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUTUNION
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public int mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public short wVk;
        public short wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public short wParamL;
        public short wParamH;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    private const int INPUT_MOUSE = 0;
    private const int INPUT_KEYBOARD = 1;
    private const int INPUT_HARDWARE = 2;

    private const uint MOUSEEVENTF_MOVE = 0x0001;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
    private const uint MOUSEEVENTF_WHEEL = 0x0800;
    private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

    private const uint KEYEVENTF_KEYDOWN = 0x0000;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_UNICODE = 0x0004;
    private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;

    /// <summary>
    /// Gets the current cursor position
    /// </summary>
    public (int X, int Y) GetCursorPosition()
    {
        GetCursorPos(out POINT point);
        return (point.X, point.Y);
    }

    /// <summary>
    /// Sets the cursor position
    /// </summary>
    public void SetCursorPosition(int x, int y)
    {
        SetCursorPos(x, y);
    }

    /// <summary>
    /// Moves the mouse to specified position
    /// </summary>
    public void MoveMouse(int x, int y)
    {
        var input = new INPUT
        {
            type = INPUT_MOUSE,
            u = new INPUTUNION
            {
                mi = new MOUSEINPUT
                {
                    dx = x,
                    dy = y,
                    dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE
                }
            }
        };
        SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
    }

    /// <summary>
    /// Performs a left mouse button click
    /// </summary>
    public void LeftClick()
    {
        var down = new INPUT
        {
            type = INPUT_MOUSE,
            u = new INPUTUNION
            {
                mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_LEFTDOWN }
            }
        };
        var up = new INPUT
        {
            type = INPUT_MOUSE,
            u = new INPUTUNION
            {
                mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_LEFTUP }
            }
        };
        SendInput(2, new[] { down, up }, Marshal.SizeOf(typeof(INPUT)));
    }

    /// <summary>
    /// Performs a right mouse button click
    /// </summary>
    public void RightClick()
    {
        var down = new INPUT
        {
            type = INPUT_MOUSE,
            u = new INPUTUNION
            {
                mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_RIGHTDOWN }
            }
        };
        var up = new INPUT
        {
            type = INPUT_MOUSE,
            u = new INPUTUNION
            {
                mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_RIGHTUP }
            }
        };
        SendInput(2, new[] { down, up }, Marshal.SizeOf(typeof(INPUT)));
    }

    /// <summary>
    /// Scrolls the mouse wheel
    /// </summary>
    public void ScrollWheel(int delta)
    {
        var input = new INPUT
        {
            type = INPUT_MOUSE,
            u = new INPUTUNION
            {
                mi = new MOUSEINPUT
                {
                    mouseData = delta,
                    dwFlags = MOUSEEVENTF_WHEEL
                }
            }
        };
        SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
    }

    /// <summary>
    /// Presses a key
    /// </summary>
    public void PressKey(short virtualKey)
    {
        var down = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = virtualKey,
                    dwFlags = KEYEVENTF_KEYDOWN
                }
            }
        };
        var up = new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = virtualKey,
                    dwFlags = KEYEVENTF_KEYUP
                }
            }
        };
        SendInput(2, new[] { down, up }, Marshal.SizeOf(typeof(INPUT)));
    }

    /// <summary>
    /// Types text
    /// </summary>
    public void TypeText(string text)
    {
        foreach (char c in text)
        {
            var down = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new INPUTUNION
                {
                    ki = new KEYBDINPUT
                    {
                        wScan = (short)c,
                        dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYDOWN
                    }
                }
            };
            var up = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new INPUTUNION
                {
                    ki = new KEYBDINPUT
                    {
                        wScan = (short)c,
                        dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP
                    }
                }
            };
            SendInput(2, new[] { down, up }, Marshal.SizeOf(typeof(INPUT)));
            Thread.Sleep(10);
        }
    }
}
