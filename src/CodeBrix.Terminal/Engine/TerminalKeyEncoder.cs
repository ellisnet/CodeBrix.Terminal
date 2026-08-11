using System.Text;

namespace CodeBrix.Terminal.Engine;

/// <summary>
/// Translates key presses into the VT byte sequences a terminal application
/// expects, so rendering hosts do not have to hand-roll the mapping. Special
/// keys become their escape sequences (honoring application-cursor mode via
/// <see cref="Terminal.ApplicationCursor"/>), Ctrl chords become C0 control
/// codes, Alt prefixes ESC, and printable keys follow a US-QWERTY layout.
/// </summary>
/// <remarks>
/// Two consumption styles are supported. Hosts whose platform exposes only raw
/// key identifiers (no composed-text event) map their keys onto
/// <see cref="TerminalKey"/> and call <see cref="Encode"/> for everything —
/// accepting that printables follow US-QWERTY. Hosts whose platform supplies
/// the layout-composed character call <see cref="EncodeSpecial"/> for the
/// named non-printables and <see cref="EncodeComposed"/> for the composed
/// character, getting correct behavior on any keyboard layout.
/// </remarks>
public static class TerminalKeyEncoder {
    /// <summary>
    /// Encodes a key with the given modifier state, following a US-QWERTY
    /// layout for printables. Returns <see langword="null"/> when the key
    /// produces no terminal input (<see cref="TerminalKey.None"/>, an unmapped
    /// key). Shift+Tab produces the back-tab sequence (CSI Z); Alt prefixes
    /// ESC (the classic meta convention).
    /// </summary>
    /// <param name="key">The key to encode.</param>
    /// <param name="modifiers">The modifier state.</param>
    /// <param name="applicationCursor">Pass <see cref="Terminal.ApplicationCursor"/> so arrows and Home/End follow the mode negotiated with the application.</param>
    public static string Encode (TerminalKey key, TerminalModifiers modifiers, bool applicationCursor = false)
    {
        var control = (modifiers & TerminalModifiers.Control) != 0;
        var alt = (modifiers & TerminalModifiers.Alt) != 0;

        var encoded = EncodeCore (key, modifiers, control, applicationCursor);
        if (encoded == null)
            return null;

        return alt ? "\x1b" + encoded : encoded;
    }

    /// <summary>
    /// Encodes only the named non-printable keys (Enter, Backspace, Tab,
    /// Escape, arrows, Home/End, Insert/Delete, paging, F1-F12). Returns
    /// <see langword="null"/> for everything else — hosts with access to the
    /// platform's layout-composed character should send printables through
    /// <see cref="EncodeComposed"/> instead. No modifier logic is applied.
    /// </summary>
    /// <param name="key">The key to encode.</param>
    /// <param name="applicationCursor">Pass <see cref="Terminal.ApplicationCursor"/> so arrows and Home/End follow the mode negotiated with the application.</param>
    public static string EncodeSpecial (TerminalKey key, bool applicationCursor = false)
    {
        switch (key) {
        case TerminalKey.Enter:
        case TerminalKey.NumPadEnter:
            return "\r";
        case TerminalKey.Backspace:
            return "\x7f";
        case TerminalKey.Tab:
            return "\t";
        case TerminalKey.Escape:
            return "\x1b";
        case TerminalKey.Up:
            return Ascii (applicationCursor ? EscapeSequences.MoveUpApp : EscapeSequences.MoveUpNormal);
        case TerminalKey.Down:
            return Ascii (applicationCursor ? EscapeSequences.MoveDownApp : EscapeSequences.MoveDownNormal);
        case TerminalKey.Right:
            return Ascii (applicationCursor ? EscapeSequences.MoveRightApp : EscapeSequences.MoveRightNormal);
        case TerminalKey.Left:
            return Ascii (applicationCursor ? EscapeSequences.MoveLeftApp : EscapeSequences.MoveLeftNormal);
        case TerminalKey.Home:
            return Ascii (applicationCursor ? EscapeSequences.MoveHomeApp : EscapeSequences.MoveHomeNormal);
        case TerminalKey.End:
            return Ascii (applicationCursor ? EscapeSequences.MoveEndApp : EscapeSequences.MoveEndNormal);
        case TerminalKey.Insert:
            return "\x1b[2~";
        case TerminalKey.Delete:
            return "\x1b[3~";
        case TerminalKey.PageUp:
            return "\x1b[5~";
        case TerminalKey.PageDown:
            return "\x1b[6~";
        default:
            if (key >= TerminalKey.F1 && key <= TerminalKey.F12)
                return Ascii (EscapeSequences.CmdF [key - TerminalKey.F1]);

            return null;
        }
    }

    /// <summary>
    /// Encodes a layout-composed character (as supplied by platforms that
    /// deliver composed text with key events) with the given modifier state:
    /// Ctrl transforms letters and <c>@</c>..<c>_</c> into C0 control codes and
    /// Space into NUL, Alt prefixes ESC, and other printable characters pass
    /// through unchanged. Returns <see langword="null"/> for non-printable
    /// code points — send those through <see cref="EncodeSpecial"/> from the
    /// key identifier instead.
    /// </summary>
    /// <param name="unicodeCodePoint">The composed Unicode code point.</param>
    /// <param name="modifiers">The modifier state (Shift and CapsLock are already reflected in the composed character; only Control and Alt are applied).</param>
    public static string EncodeComposed (int unicodeCodePoint, TerminalModifiers modifiers)
    {
        if (unicodeCodePoint <= 0 || unicodeCodePoint > 0x10FFFF)
            return null;

        var control = (modifiers & TerminalModifiers.Control) != 0;
        var alt = (modifiers & TerminalModifiers.Alt) != 0;

        string encoded = null;

        if (control) {
            if (unicodeCodePoint >= 'a' && unicodeCodePoint <= 'z')
                encoded = ((char)(unicodeCodePoint - 'a' + 1)).ToString ();
            else if (unicodeCodePoint >= '@' && unicodeCodePoint <= '_')
                encoded = ((char)(unicodeCodePoint & 0x1f)).ToString ();
            else if (unicodeCodePoint == ' ')
                encoded = "\x00";
        }

        if (encoded == null) {
            if (unicodeCodePoint < ' ' || unicodeCodePoint == 0x7f)
                return null;

            encoded = char.ConvertFromUtf32 (unicodeCodePoint);
        }

        return alt ? "\x1b" + encoded : encoded;
    }

    static string EncodeCore (TerminalKey key, TerminalModifiers modifiers, bool control, bool applicationCursor)
    {
        var shift = (modifiers & TerminalModifiers.Shift) != 0;
        var capsLock = (modifiers & TerminalModifiers.CapsLock) != 0;

        if (key == TerminalKey.Tab && shift)
            return Ascii (EscapeSequences.CmdBackTab);

        var special = EncodeSpecial (key, applicationCursor);
        if (special != null)
            return special;

        // Letters: case follows shift XOR caps-lock; Ctrl produces C0 codes
        // via the shared '@'..'_' rule below (uppercase A..Z land in it).
        if (key >= TerminalKey.A && key <= TerminalKey.Z) {
            var c = (char)('a' + (key - TerminalKey.A));
            if (control || (shift ^ capsLock))
                c = char.ToUpperInvariant (c);

            return ApplyControl (c, control);
        }

        // Digit row (shifted forms are the US symbols)
        if (key >= TerminalKey.D0 && key <= TerminalKey.D9) {
            var digit = key - TerminalKey.D0;
            var c = shift ? ")!@#$%^&*(" [digit] : (char)('0' + digit);

            return ApplyControl (c, control);
        }

        // Numeric keypad
        if (key >= TerminalKey.NumPad0 && key <= TerminalKey.NumPad9)
            return ((char)('0' + (key - TerminalKey.NumPad0))).ToString ();

        switch (key) {
        case TerminalKey.Space:
            return ApplyControl (' ', control);
        case TerminalKey.NumPadAdd:
            return "+";
        case TerminalKey.NumPadSubtract:
            return "-";
        case TerminalKey.NumPadMultiply:
            return "*";
        case TerminalKey.NumPadDivide:
            return "/";
        case TerminalKey.NumPadDecimal:
            return ".";
        case TerminalKey.NumPadEqual:
            return "=";
        }

        // US punctuation (shifted forms are the US symbols)
        char normal, shifted;
        switch (key) {
        case TerminalKey.Semicolon:
            (normal, shifted) = (';', ':');
            break;
        case TerminalKey.Equal:
            (normal, shifted) = ('=', '+');
            break;
        case TerminalKey.Comma:
            (normal, shifted) = (',', '<');
            break;
        case TerminalKey.Minus:
            (normal, shifted) = ('-', '_');
            break;
        case TerminalKey.Period:
            (normal, shifted) = ('.', '>');
            break;
        case TerminalKey.Slash:
            (normal, shifted) = ('/', '?');
            break;
        case TerminalKey.Backquote:
            (normal, shifted) = ('`', '~');
            break;
        case TerminalKey.LeftBracket:
            (normal, shifted) = ('[', '{');
            break;
        case TerminalKey.Backslash:
            (normal, shifted) = ('\\', '|');
            break;
        case TerminalKey.RightBracket:
            (normal, shifted) = (']', '}');
            break;
        case TerminalKey.Quote:
            (normal, shifted) = ('\'', '"');
            break;
        default:
            return null;
        }

        return ApplyControl (shift ? shifted : normal, control);
    }

    // Ctrl turns anything in '@'..'_' (which includes uppercase letters, so
    // Ctrl+A..Z become C0 1..26) into its C0 control code, and Space into NUL.
    // Classic chords fall out of the same rule: Ctrl+[ = ESC, Ctrl+\ = FS,
    // Ctrl+] = GS, Ctrl+Shift+6 (^) = RS, Ctrl+Shift+- (_) = US.
    static string ApplyControl (char c, bool control)
    {
        if (!control)
            return c.ToString ();

        if (c >= '@' && c <= '_')
            return ((char)(c & 0x1f)).ToString ();

        if (c == ' ')
            return "\x00";

        return c.ToString ();
    }

    static string Ascii (byte [] sequence)
    {
        return Encoding.ASCII.GetString (sequence);
    }
}
