namespace CodeBrix.Terminal.Engine;

/// <summary>
/// A platform-neutral identifier for the keys <see cref="TerminalKeyEncoder"/>
/// can translate into VT input. Hosts map their native key events (WinUI
/// VirtualKey, GTK keyval, etc.) onto this enum. Values are engine-defined and
/// deliberately do NOT mirror any platform's virtual-key codes.
/// </summary>
/// <remarks>
/// <c>D0</c>-<c>D9</c> are the main digit row (whose shifted forms are the US
/// symbols); <c>NumPad0</c>-<c>NumPad9</c> and the <c>NumPad*</c> operators are
/// the numeric keypad. There is no BackTab member: encode Shift+<see cref="Tab"/>
/// instead. Punctuation members are named for their unshifted US-QWERTY
/// character.
/// </remarks>
public enum TerminalKey {
    None = 0,

    Enter,
    Backspace,
    Tab,
    Escape,
    Space,

    Up,
    Down,
    Left,
    Right,
    Home,
    End,
    Insert,
    Delete,
    PageUp,
    PageDown,

    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,

    A, B, C, D, E, F, G, H, I, J, K, L, M,
    N, O, P, Q, R, S, T, U, V, W, X, Y, Z,

    D0, D1, D2, D3, D4, D5, D6, D7, D8, D9,

    NumPad0, NumPad1, NumPad2, NumPad3, NumPad4,
    NumPad5, NumPad6, NumPad7, NumPad8, NumPad9,
    NumPadEnter,
    NumPadAdd,
    NumPadSubtract,
    NumPadMultiply,
    NumPadDivide,
    NumPadDecimal,
    NumPadEqual,

    /// <summary>The <c>;</c> / <c>:</c> key.</summary>
    Semicolon,
    /// <summary>The <c>=</c> / <c>+</c> key.</summary>
    Equal,
    /// <summary>The <c>,</c> / <c>&lt;</c> key.</summary>
    Comma,
    /// <summary>The <c>-</c> / <c>_</c> key.</summary>
    Minus,
    /// <summary>The <c>.</c> / <c>&gt;</c> key.</summary>
    Period,
    /// <summary>The <c>/</c> / <c>?</c> key.</summary>
    Slash,
    /// <summary>The <c>`</c> / <c>~</c> key.</summary>
    Backquote,
    /// <summary>The <c>[</c> / <c>{</c> key.</summary>
    LeftBracket,
    /// <summary>The <c>\</c> / <c>|</c> key.</summary>
    Backslash,
    /// <summary>The <c>]</c> / <c>}</c> key.</summary>
    RightBracket,
    /// <summary>The <c>'</c> / <c>"</c> key.</summary>
    Quote,
}

/// <summary>
/// Modifier state accompanying a <see cref="TerminalKey"/> press, as consumed
/// by <see cref="TerminalKeyEncoder"/>.
/// </summary>
[System.Flags]
public enum TerminalModifiers {
    None = 0,
    Shift = 1,
    Control = 2,
    Alt = 4,
    CapsLock = 8,
}
