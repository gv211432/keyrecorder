using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using KeyRecorder.Core.Models;
using Microsoft.Extensions.Logging;

namespace KeyRecorder.Core.Capture;

public class KeyboardHook : IDisposable
{
    private readonly NativeMethods.LowLevelKeyboardProc _hookCallback;
    private IntPtr _hookId = IntPtr.Zero;
    private bool _disposed;
    private readonly ILogger<KeyboardHook>? _logger;

    public event EventHandler<KeystrokeEvent>? KeystrokeCaptured;
    public bool IsPaused { get; set; }

    public KeyboardHook(ILogger<KeyboardHook>? logger = null)
    {
        _logger = logger;
        _hookCallback = HookCallback;
    }

    public void Start()
    {
        if (_hookId != IntPtr.Zero)
        {
            _logger?.LogWarning("Hook already started");
            return;
        }

        _hookId = SetHook(_hookCallback);
        _logger?.LogInformation("Keyboard hook started");
    }

    public void Stop()
    {
        if (_hookId != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
            _logger?.LogInformation("Keyboard hook stopped");
        }
    }

    private IntPtr SetHook(NativeMethods.LowLevelKeyboardProc proc)
    {
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;

        if (curModule?.ModuleName == null)
        {
            throw new InvalidOperationException("Unable to get current module");
        }

        return NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_KEYBOARD_LL,
            proc,
            NativeMethods.GetModuleHandle(curModule.ModuleName),
            0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && !IsPaused)
        {
            int wParamInt = wParam.ToInt32();

            if (wParamInt == NativeMethods.WM_KEYDOWN ||
                wParamInt == NativeMethods.WM_KEYUP ||
                wParamInt == NativeMethods.WM_SYSKEYDOWN ||
                wParamInt == NativeMethods.WM_SYSKEYUP)
            {
                var hookStruct = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);

                var keystroke = new KeystrokeEvent
                {
                    Timestamp = DateTime.UtcNow,
                    VirtualKeyCode = hookStruct.vkCode,
                    KeyName = GetKeyName(hookStruct.vkCode),
                    IsKeyDown = wParamInt == NativeMethods.WM_KEYDOWN || wParamInt == NativeMethods.WM_SYSKEYDOWN,
                    IsShiftPressed = IsKeyPressed(NativeMethods.VirtualKeys.VK_SHIFT),
                    IsCtrlPressed = IsKeyPressed(NativeMethods.VirtualKeys.VK_CONTROL),
                    IsAltPressed = IsKeyPressed(NativeMethods.VirtualKeys.VK_MENU),
                    IsWinPressed = IsKeyPressed(NativeMethods.VirtualKeys.VK_LWIN) ||
                                   IsKeyPressed(NativeMethods.VirtualKeys.VK_RWIN)
                };

                try
                {
                    var (windowTitle, processName) = GetActiveWindowInfo();
                    keystroke.ActiveWindowTitle = windowTitle;
                    keystroke.ActiveProcessName = processName;
                }
                catch
                {
                    // Ignore errors getting window info
                }

                KeystrokeCaptured?.Invoke(this, keystroke);
            }
        }

        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private bool IsKeyPressed(int virtualKey)
    {
        return (NativeMethods.GetKeyState(virtualKey) & 0x8000) != 0;
    }

    private (string? windowTitle, string? processName) GetActiveWindowInfo()
    {
        try
        {
            var hwnd = NativeMethods.GetForegroundWindow();

            if (hwnd == IntPtr.Zero)
                return (null, null);

            var sb = new StringBuilder(256);
            NativeMethods.GetWindowText(hwnd, sb, sb.Capacity);
            var windowTitle = sb.ToString();

            NativeMethods.GetWindowThreadProcessId(hwnd, out uint processId);
            var process = Process.GetProcessById((int)processId);
            var processName = process.ProcessName;

            return (windowTitle, processName);
        }
        catch
        {
            return (null, null);
        }
    }

    private string GetKeyName(int virtualKeyCode)
    {
        return VirtualKeyMapper.GetKeyName(virtualKeyCode);
    }

    public void Dispose()
    {
        if (_disposed) return;
        Stop();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
