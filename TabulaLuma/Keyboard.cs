using System.Collections.Concurrent;
using System.Text.Json;

namespace TabulaLuma
{
    [Flags]
    public enum KeyScanCodes
    {
        Unknown = 0,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        A = 4,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        B = 5,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        C = 6,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        D = 7,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        E = 8,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        F = 9,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        G = 0xA,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        H = 0xB,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        I = 0xC,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        J = 0xD,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        K = 0xE,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        L = 0xF,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        M = 0x10,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        N = 0x11,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        O = 0x12,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        P = 0x13,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        Q = 0x14,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        R = 0x15,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        S = 0x16,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        T = 0x17,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        U = 0x18,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        V = 0x19,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        W = 0x1A,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        X = 0x1B,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        Y = 0x1C,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        Z = 0x1D,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        Scancode1 = 0x1E,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        Scancode2 = 0x1F,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        Scancode3 = 0x20,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        Scancode4 = 0x21,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        Scancode5 = 0x22,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        Scancode6 = 0x23,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        Scancode7 = 0x24,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        Scancode8 = 0x25,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        Scancode9 = 0x26,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        Scancode0 = 0x27,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        Return = 0x28,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        Escape = 0x29,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        Backspace = 0x2A,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        Tab = 0x2B,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        Space = 0x2C,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        Minus = 0x2D,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        Equals = 0x2E,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        Leftbracket = 0x2F,
        //
        // Summary:
        //     These values are from usage page 0x07 (USB keyboard page).
        //
        //     @
        //     {
        Rightbracket = 0x30,
        //
        // Summary:
        //     Located at the lower left of the return
        //     key on ISO keyboards and at the right end
        //     of the QWERTY row on ANSI keyboards.
        //     Produces REVERSE SOLIDUS (backslash) and
        //     VERTICAL LINE in a US layout, REVERSE
        //     SOLIDUS and VERTICAL LINE in a UK Mac
        //     layout, NUMBER SIGN and TILDE in a UK
        //     Windows layout, DOLLAR SIGN and POUND SIGN
        //     in a Swiss German layout, NUMBER SIGN and
        //     APOSTROPHE in a German layout, GRAVE
        //     ACCENT and POUND SIGN in a French Mac
        //     layout, and ASTERISK and MICRO SIGN in a
        //     French Windows layout.
        Backslash = 0x31,
        //
        // Summary:
        //     ISO USB keyboards actually use this code
        //     instead of 49 for the same key, but all
        //     OSes I've seen treat the two codes
        //     identically. So, as an implementor, unless
        //     your keyboard generates both of those
        //     codes and your OS treats them differently,
        //     you should generate SDL_SCANCODE_BACKSLASH
        //     instead of this code. As a user, you
        //     should not rely on this code because SDL
        //     will never generate it with most (all?)
        //     keyboards.
        Nonushash = 0x32,
        Semicolon = 0x33,
        Apostrophe = 0x34,
        //
        // Summary:
        //     Located in the top left corner (on both ANSI
        //     and ISO keyboards). Produces GRAVE ACCENT and
        //     TILDE in a US Windows layout and in US and UK
        //     Mac layouts on ANSI keyboards, GRAVE ACCENT
        //     and NOT SIGN in a UK Windows layout, SECTION
        //     SIGN and PLUS-MINUS SIGN in US and UK Mac
        //     layouts on ISO keyboards, SECTION SIGN and
        //     DEGREE SIGN in a Swiss German layout (Mac:
        //     only on ISO keyboards), CIRCUMFLEX ACCENT and
        //     DEGREE SIGN in a German layout (Mac: only on
        //     ISO keyboards), SUPERSCRIPT TWO and TILDE in a
        //     French Windows layout, COMMERCIAL AT and
        //     NUMBER SIGN in a French Mac layout on ISO
        //     keyboards, and LESS-THAN SIGN and GREATER-THAN
        //     SIGN in a Swiss German, German, or French Mac
        //     layout on ANSI keyboards.
        Grave = 0x35,
        Comma = 0x36,
        Period = 0x37,
        Slash = 0x38,
        Capslock = 0x39,
        F1 = 0x3A,
        F2 = 0x3B,
        F3 = 0x3C,
        F4 = 0x3D,
        F5 = 0x3E,
        F6 = 0x3F,
        F7 = 0x40,
        F8 = 0x41,
        F9 = 0x42,
        F10 = 0x43,
        F11 = 0x44,
        F12 = 0x45,
        Printscreen = 0x46,
        Scrolllock = 0x47,
        Pause = 0x48,
        //
        // Summary:
        //     insert on PC, help on some Mac keyboards (but
        //     does send code 73, not 117)
        Insert = 0x49,
        Home = 0x4A,
        Pageup = 0x4B,
        Delete = 0x4C,
        End = 0x4D,
        Pagedown = 0x4E,
        Right = 0x4F,
        Left = 0x50,
        Down = 0x51,
        Up = 0x52,
        //
        // Summary:
        //     num lock on PC, clear on Mac keyboards
        Numlockclear = 0x53,
        KpDivide = 0x54,
        KpMultiply = 0x55,
        KpMinus = 0x56,
        KpPlus = 0x57,
        KpEnter = 0x58,
        Kp1 = 0x59,
        Kp2 = 0x5A,
        Kp3 = 0x5B,
        Kp4 = 0x5C,
        Kp5 = 0x5D,
        Kp6 = 0x5E,
        Kp7 = 0x5F,
        Kp8 = 0x60,
        Kp9 = 0x61,
        Kp0 = 0x62,
        KpPeriod = 0x63,
        //
        // Summary:
        //     This is the additional key that ISO
        //     keyboards have over ANSI ones,
        //     located between left shift and Y.
        //     Produces GRAVE ACCENT and TILDE in a
        //     US or UK Mac layout, REVERSE SOLIDUS
        //     (backslash) and VERTICAL LINE in a
        //     US or UK Windows layout, and
        //     LESS-THAN SIGN and GREATER-THAN SIGN
        //     in a Swiss German, German, or French
        //     layout.
        Nonusbackslash = 0x64,
        //
        // Summary:
        //     windows contextual menu, compose
        Application = 0x65,
        //
        // Summary:
        //     The USB document says this is a status flag,
        //     not a physical key - but some Mac keyboards
        //     do have a power key.
        Power = 0x66,
        KpEquals = 0x67,
        F13 = 0x68,
        F14 = 0x69,
        F15 = 0x6A,
        F16 = 0x6B,
        F17 = 0x6C,
        F18 = 0x6D,
        F19 = 0x6E,
        F20 = 0x6F,
        F21 = 0x70,
        F22 = 0x71,
        F23 = 0x72,
        F24 = 0x73,
        Execute = 0x74,
        //
        // Summary:
        //     AL Integrated Help Center
        Help = 0x75,
        //
        // Summary:
        //     Menu (show menu)
        Menu = 0x76,
        Select = 0x77,
        //
        // Summary:
        //     AC Stop
        Stop = 0x78,
        //
        // Summary:
        //     AC Redo/Repeat
        Again = 0x79,
        //
        // Summary:
        //     AC Undo
        Undo = 0x7A,
        //
        // Summary:
        //     AC Cut
        Cut = 0x7B,
        //
        // Summary:
        //     AC Copy
        Copy = 0x7C,
        //
        // Summary:
        //     AC Paste
        Paste = 0x7D,
        //
        // Summary:
        //     AC Find
        Find = 0x7E,
        Mute = 0x7F,
        Volumeup = 0x80,
        Volumedown = 0x81,
        //
        // Summary:
        //     not sure whether there's a reason to enable these
        //     SDL_SCANCODE_LOCKINGCAPSLOCK = 130,
        //     SDL_SCANCODE_LOCKINGNUMLOCK = 131,
        //     SDL_SCANCODE_LOCKINGSCROLLLOCK = 132,
        KpComma = 0x85,
        //
        // Summary:
        //     not sure whether there's a reason to enable these
        //     SDL_SCANCODE_LOCKINGCAPSLOCK = 130,
        //     SDL_SCANCODE_LOCKINGNUMLOCK = 131,
        //     SDL_SCANCODE_LOCKINGSCROLLLOCK = 132,
        KpEqualsas400 = 0x86,
        //
        // Summary:
        //     used on Asian keyboards, see
        //     footnotes in USB doc
        International1 = 0x87,
        International2 = 0x88,
        //
        // Summary:
        //     Yen
        International3 = 0x89,
        International4 = 0x8A,
        International5 = 0x8B,
        International6 = 0x8C,
        International7 = 0x8D,
        International8 = 0x8E,
        International9 = 0x8F,
        //
        // Summary:
        //     Hangul/English toggle
        Lang1 = 0x90,
        //
        // Summary:
        //     Hanja conversion
        Lang2 = 0x91,
        //
        // Summary:
        //     Katakana
        Lang3 = 0x92,
        //
        // Summary:
        //     Hiragana
        Lang4 = 0x93,
        //
        // Summary:
        //     Zenkaku/Hankaku
        Lang5 = 0x94,
        //
        // Summary:
        //     reserved
        Lang6 = 0x95,
        //
        // Summary:
        //     reserved
        Lang7 = 0x96,
        //
        // Summary:
        //     reserved
        Lang8 = 0x97,
        //
        // Summary:
        //     reserved
        Lang9 = 0x98,
        //
        // Summary:
        //     Erase-Eaze
        Alterase = 0x99,
        Sysreq = 0x9A,
        //
        // Summary:
        //     AC Cancel
        Cancel = 0x9B,
        Clear = 0x9C,
        Prior = 0x9D,
        Return2 = 0x9E,
        Separator = 0x9F,
        Out = 0xA0,
        Oper = 0xA1,
        Clearagain = 0xA2,
        Crsel = 0xA3,
        Exsel = 0xA4,
        Kp00 = 0xB0,
        Kp000 = 0xB1,
        Thousandsseparator = 0xB2,
        Decimalseparator = 0xB3,
        Currencyunit = 0xB4,
        Currencysubunit = 0xB5,
        KpLeftparen = 0xB6,
        KpRightparen = 0xB7,
        KpLeftbrace = 0xB8,
        KpRightbrace = 0xB9,
        KpTab = 0xBA,
        KpBackspace = 0xBB,
        KpA = 0xBC,
        KpB = 0xBD,
        KpC = 0xBE,
        KpD = 0xBF,
        KpE = 0xC0,
        KpF = 0xC1,
        KpXor = 0xC2,
        KpPower = 0xC3,
        KpPercent = 0xC4,
        KpLess = 0xC5,
        KpGreater = 0xC6,
        KpAmpersand = 0xC7,
        KpDblampersand = 0xC8,
        KpVerticalbar = 0xC9,
        KpDblverticalbar = 0xCA,
        KpColon = 0xCB,
        KpHash = 0xCC,
        KpSpace = 0xCD,
        KpAt = 0xCE,
        KpExclam = 0xCF,
        KpMemstore = 0xD0,
        KpMemrecall = 0xD1,
        KpMemclear = 0xD2,
        KpMemadd = 0xD3,
        KpMemsubtract = 0xD4,
        KpMemmultiply = 0xD5,
        KpMemdivide = 0xD6,
        KpPlusminus = 0xD7,
        KpClear = 0xD8,
        KpClearentry = 0xD9,
        KpBinary = 0xDA,
        KpOctal = 0xDB,
        KpDecimal = 0xDC,
        KpHexadecimal = 0xDD,
        Lctrl = 0xE0,
        Lshift = 0xE1,
        //
        // Summary:
        //     alt, option
        Lalt = 0xE2,
        //
        // Summary:
        //     windows, command (apple), meta
        Lgui = 0xE3,
        Rctrl = 0xE4,
        Rshift = 0xE5,
        //
        // Summary:
        //     alt gr, option
        Ralt = 0xE6,
        //
        // Summary:
        //     windows, command (apple), meta
        Rgui = 0xE7,
        //
        // Summary:
        //     I'm not sure if this is really not covered
        //     by any of the above, but since there's a
        //     special SDL_KMOD_MODE for it I'm adding it here
        Mode = 0x101,
        //
        // Summary:
        //     Sleep
        Sleep = 0x102,
        //
        // Summary:
        //     Wake
        Wake = 0x103,
        //
        // Summary:
        //     Channel Increment
        ChannelIncrement = 0x104,
        //
        // Summary:
        //     Channel Decrement
        ChannelDecrement = 0x105,
        //
        // Summary:
        //     Play
        MediaPlay = 0x106,
        //
        // Summary:
        //     Pause
        MediaPause = 0x107,
        //
        // Summary:
        //     Record
        MediaRecord = 0x108,
        //
        // Summary:
        //     Fast Forward
        MediaFastForward = 0x109,
        //
        // Summary:
        //     Rewind
        MediaRewind = 0x10A,
        //
        // Summary:
        //     Next Track
        MediaNextTrack = 0x10B,
        //
        // Summary:
        //     Previous Track
        MediaPreviousTrack = 0x10C,
        //
        // Summary:
        //     Stop
        MediaStop = 0x10D,
        //
        // Summary:
        //     Eject
        MediaEject = 0x10E,
        //
        // Summary:
        //     Play / Pause
        MediaPlayPause = 0x10F,
        //
        // Summary:
        //     Media Select
        MediaSelect = 0x110,
        //
        // Summary:
        //     AC New
        AcNew = 0x111,
        //
        // Summary:
        //     AC Open
        AcOpen = 0x112,
        //
        // Summary:
        //     AC Close
        AcClose = 0x113,
        //
        // Summary:
        //     AC Exit
        AcExit = 0x114,
        //
        // Summary:
        //     AC Save
        AcSave = 0x115,
        //
        // Summary:
        //     AC Print
        AcPrint = 0x116,
        //
        // Summary:
        //     AC Properties
        AcProperties = 0x117,
        //
        // Summary:
        //     AC Search
        AcSearch = 0x118,
        //
        // Summary:
        //     AC Home
        AcHome = 0x119,
        //
        // Summary:
        //     AC Back
        AcBack = 0x11A,
        //
        // Summary:
        //     AC Forward
        AcForward = 0x11B,
        //
        // Summary:
        //     AC Stop
        AcStop = 0x11C,
        //
        // Summary:
        //     AC Refresh
        AcRefresh = 0x11D,
        //
        // Summary:
        //     AC Bookmarks
        AcBookmarks = 0x11E,
        //
        // Summary:
        //     Usually situated below the display on phones and
        //     used as a multi-function feature key for selecting
        //     a software defined function shown on the bottom left
        //     of the display.
        Softleft = 0x11F,
        //
        // Summary:
        //     Usually situated below the display on phones and
        //     used as a multi-function feature key for selecting
        //     a software defined function shown on the bottom right
        //     of the display.
        Softright = 0x120,
        //
        // Summary:
        //     Used for accepting phone calls.
        Call = 0x121,
        //
        // Summary:
        //     Used for rejecting phone calls.
        Endcall = 0x122,
        //
        // Summary:
        //     400-500 reserved for dynamic keycodes
        Reserved = 0x190,
        //
        // Summary:
        //     not a key, just marks the number of scancodes for array bounds
        Count = 0x200
    }
  
    public class KeyEvent
    {
        public int ScanCode { get; set; }
        public char KeyChar { get; set; }
        public bool KeyPress { get; set; } = false;
        public bool Control { get; set; }
        public bool Alt { get; set; }
        public bool Shift { get; set; }
        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
    public class Keyboard : IKeyboardService
    {
        int keyboardCaptureId = -1;

        ConcurrentQueue<KeyEvent> KeyEvents { get; set; } = new ConcurrentQueue<KeyEvent>();

        public void EnqueueKeyEvent(KeyEvent keyEvent)
        {
            KeyEvents.Enqueue(keyEvent);
        }
    
        public void CaptureKeyboard(int id)
        {
            if(keyboardCaptureId != id)
            {
                keyboardCaptureId = id;
   //             while (KeyboardInput.TryDequeue(out _)) ;
   //             while (ScanCodes.TryDequeue(out _)) ;
            }
        }

        public bool TryGetKeyEvent(out KeyEvent keyEvent)
        {
            return KeyEvents.TryDequeue(out keyEvent);
        }
    }
}
