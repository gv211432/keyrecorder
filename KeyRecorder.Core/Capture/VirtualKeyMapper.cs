namespace KeyRecorder.Core.Capture;

public static class VirtualKeyMapper
{
    private static readonly Dictionary<int, string> KeyNames = new()
    {
        // Control keys
        { 0x08, "Backspace" },
        { 0x09, "Tab" },
        { 0x0D, "Enter" },
        { 0x10, "Shift" },
        { 0x11, "Ctrl" },
        { 0x12, "Alt" },
        { 0x13, "Pause" },
        { 0x14, "CapsLock" },
        { 0x1B, "Esc" },
        { 0x20, "Space" },
        { 0x21, "PgUp" },
        { 0x22, "PgDn" },
        { 0x23, "End" },
        { 0x24, "Home" },
        { 0x25, "Left" },
        { 0x26, "Up" },
        { 0x27, "Right" },
        { 0x28, "Down" },
        { 0x2C, "PrtSc" },
        { 0x2D, "Ins" },
        { 0x2E, "Del" },

        // Number row (show actual numbers)
        { 0x30, "0" }, { 0x31, "1" }, { 0x32, "2" }, { 0x33, "3" }, { 0x34, "4" },
        { 0x35, "5" }, { 0x36, "6" }, { 0x37, "7" }, { 0x38, "8" }, { 0x39, "9" },

        // Letters
        { 0x41, "A" }, { 0x42, "B" }, { 0x43, "C" }, { 0x44, "D" }, { 0x45, "E" },
        { 0x46, "F" }, { 0x47, "G" }, { 0x48, "H" }, { 0x49, "I" }, { 0x4A, "J" },
        { 0x4B, "K" }, { 0x4C, "L" }, { 0x4D, "M" }, { 0x4E, "N" }, { 0x4F, "O" },
        { 0x50, "P" }, { 0x51, "Q" }, { 0x52, "R" }, { 0x53, "S" }, { 0x54, "T" },
        { 0x55, "U" }, { 0x56, "V" }, { 0x57, "W" }, { 0x58, "X" }, { 0x59, "Y" },
        { 0x5A, "Z" },

        // Windows keys
        { 0x5B, "Win" },
        { 0x5C, "Win" },
        { 0x5D, "Menu" },

        // Numpad
        { 0x60, "Num0" }, { 0x61, "Num1" }, { 0x62, "Num2" }, { 0x63, "Num3" },
        { 0x64, "Num4" }, { 0x65, "Num5" }, { 0x66, "Num6" }, { 0x67, "Num7" },
        { 0x68, "Num8" }, { 0x69, "Num9" },
        { 0x6A, "*" },
        { 0x6B, "+" },
        { 0x6D, "-" },
        { 0x6E, "." },
        { 0x6F, "/" },

        // Function keys
        { 0x70, "F1" }, { 0x71, "F2" }, { 0x72, "F3" }, { 0x73, "F4" },
        { 0x74, "F5" }, { 0x75, "F6" }, { 0x76, "F7" }, { 0x77, "F8" },
        { 0x78, "F9" }, { 0x79, "F10" }, { 0x7A, "F11" }, { 0x7B, "F12" },

        // Lock keys
        { 0x90, "NumLock" },
        { 0x91, "ScrLock" },

        // Modifier keys (left/right variants)
        { 0xA0, "Shift" },
        { 0xA1, "Shift" },
        { 0xA2, "Ctrl" },
        { 0xA3, "Ctrl" },
        { 0xA4, "Alt" },
        { 0xA5, "Alt" },

        // OEM keys (show actual symbols)
        { 0xBA, ";" },      // Semicolon
        { 0xBB, "=" },      // Plus/Equals
        { 0xBC, "," },      // Comma
        { 0xBD, "-" },      // Minus
        { 0xBE, "." },      // Period
        { 0xBF, "/" },      // Forward slash / Question mark
        { 0xC0, "`" },      // Backtick / Tilde
        { 0xDB, "[" },      // Open bracket
        { 0xDC, "\\" },     // Backslash
        { 0xDD, "]" },      // Close bracket
        { 0xDE, "'" },      // Single quote
    };

    public static string GetKeyName(int virtualKeyCode)
    {
        return KeyNames.TryGetValue(virtualKeyCode, out var name) ? name : $"Key{virtualKeyCode:X2}";
    }
}
