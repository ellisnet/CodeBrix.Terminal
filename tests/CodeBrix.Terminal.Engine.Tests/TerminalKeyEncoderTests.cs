using SilverAssertions;
using Xunit;

namespace CodeBrix.Terminal.Engine.Tests;

public class TerminalKeyEncoderTests
{
    #region Encode - letters, digits, punctuation

    [Fact]
    public void Encode_Letter_LowercaseByDefault ()
    {
        TerminalKeyEncoder.Encode (TerminalKey.A, TerminalModifiers.None).Should ().Be ("a");
        TerminalKeyEncoder.Encode (TerminalKey.Z, TerminalModifiers.None).Should ().Be ("z");
    }

    [Fact]
    public void Encode_Letter_ShiftAndCapsLockXor ()
    {
        TerminalKeyEncoder.Encode (TerminalKey.A, TerminalModifiers.Shift).Should ().Be ("A");
        TerminalKeyEncoder.Encode (TerminalKey.A, TerminalModifiers.CapsLock).Should ().Be ("A");
        TerminalKeyEncoder.Encode (TerminalKey.A, TerminalModifiers.Shift | TerminalModifiers.CapsLock).Should ().Be ("a");
    }

    [Fact]
    public void Encode_CtrlLetter_IsC0ControlCode ()
    {
        TerminalKeyEncoder.Encode (TerminalKey.A, TerminalModifiers.Control).Should ().Be ("\x01");
        TerminalKeyEncoder.Encode (TerminalKey.C, TerminalModifiers.Control).Should ().Be ("\x03");
        TerminalKeyEncoder.Encode (TerminalKey.Z, TerminalModifiers.Control).Should ().Be ("\x1a");

        // Shift makes no difference to a control chord
        TerminalKeyEncoder.Encode (TerminalKey.C, TerminalModifiers.Control | TerminalModifiers.Shift).Should ().Be ("\x03");
    }

    [Fact]
    public void Encode_DigitRow_ShiftedFormsAreTheUsSymbols ()
    {
        TerminalKeyEncoder.Encode (TerminalKey.D1, TerminalModifiers.None).Should ().Be ("1");
        TerminalKeyEncoder.Encode (TerminalKey.D1, TerminalModifiers.Shift).Should ().Be ("!");
        TerminalKeyEncoder.Encode (TerminalKey.D0, TerminalModifiers.Shift).Should ().Be (")");
        TerminalKeyEncoder.Encode (TerminalKey.D8, TerminalModifiers.Shift).Should ().Be ("*");
    }

    [Theory]
    [InlineData (TerminalKey.Semicolon, ";", ":")]
    [InlineData (TerminalKey.Equal, "=", "+")]
    [InlineData (TerminalKey.Comma, ",", "<")]
    [InlineData (TerminalKey.Minus, "-", "_")]
    [InlineData (TerminalKey.Period, ".", ">")]
    [InlineData (TerminalKey.Slash, "/", "?")]
    [InlineData (TerminalKey.Backquote, "`", "~")]
    [InlineData (TerminalKey.LeftBracket, "[", "{")]
    [InlineData (TerminalKey.Backslash, "\\", "|")]
    [InlineData (TerminalKey.RightBracket, "]", "}")]
    [InlineData (TerminalKey.Quote, "'", "\"")]
    public void Encode_UsPunctuation_NormalAndShifted (TerminalKey key, string normal, string shifted)
    {
        TerminalKeyEncoder.Encode (key, TerminalModifiers.None).Should ().Be (normal);
        TerminalKeyEncoder.Encode (key, TerminalModifiers.Shift).Should ().Be (shifted);
    }

    [Fact]
    public void Encode_ClassicControlChords ()
    {
        // Ctrl+[ = ESC, Ctrl+\ = FS, Ctrl+] = GS, Ctrl+Shift+6 = RS, Ctrl+Shift+- = US
        TerminalKeyEncoder.Encode (TerminalKey.LeftBracket, TerminalModifiers.Control).Should ().Be ("\x1b");
        TerminalKeyEncoder.Encode (TerminalKey.Backslash, TerminalModifiers.Control).Should ().Be ("\x1c");
        TerminalKeyEncoder.Encode (TerminalKey.RightBracket, TerminalModifiers.Control).Should ().Be ("\x1d");
        TerminalKeyEncoder.Encode (TerminalKey.D6, TerminalModifiers.Control | TerminalModifiers.Shift).Should ().Be ("\x1e");
        TerminalKeyEncoder.Encode (TerminalKey.Minus, TerminalModifiers.Control | TerminalModifiers.Shift).Should ().Be ("\x1f");
    }

    [Fact]
    public void Encode_Space_AndCtrlSpace ()
    {
        TerminalKeyEncoder.Encode (TerminalKey.Space, TerminalModifiers.None).Should ().Be (" ");
        TerminalKeyEncoder.Encode (TerminalKey.Space, TerminalModifiers.Control).Should ().Be ("\x00");
    }

    #endregion

    #region Encode - specials, application cursor, Alt

    [Fact]
    public void Encode_Specials ()
    {
        TerminalKeyEncoder.Encode (TerminalKey.Enter, TerminalModifiers.None).Should ().Be ("\r");
        TerminalKeyEncoder.Encode (TerminalKey.NumPadEnter, TerminalModifiers.None).Should ().Be ("\r");
        TerminalKeyEncoder.Encode (TerminalKey.Backspace, TerminalModifiers.None).Should ().Be ("\x7f");
        TerminalKeyEncoder.Encode (TerminalKey.Tab, TerminalModifiers.None).Should ().Be ("\t");
        TerminalKeyEncoder.Encode (TerminalKey.Escape, TerminalModifiers.None).Should ().Be ("\x1b");
        TerminalKeyEncoder.Encode (TerminalKey.Insert, TerminalModifiers.None).Should ().Be ("\x1b[2~");
        TerminalKeyEncoder.Encode (TerminalKey.Delete, TerminalModifiers.None).Should ().Be ("\x1b[3~");
        TerminalKeyEncoder.Encode (TerminalKey.PageUp, TerminalModifiers.None).Should ().Be ("\x1b[5~");
        TerminalKeyEncoder.Encode (TerminalKey.PageDown, TerminalModifiers.None).Should ().Be ("\x1b[6~");
    }

    [Fact]
    public void Encode_ShiftTab_IsBackTab ()
    {
        TerminalKeyEncoder.Encode (TerminalKey.Tab, TerminalModifiers.Shift).Should ().Be ("\x1b[Z");
    }

    [Fact]
    public void Encode_Arrows_HonorApplicationCursorMode ()
    {
        TerminalKeyEncoder.Encode (TerminalKey.Up, TerminalModifiers.None, applicationCursor: false).Should ().Be ("\x1b[A");
        TerminalKeyEncoder.Encode (TerminalKey.Up, TerminalModifiers.None, applicationCursor: true).Should ().Be ("\x1bOA");
        TerminalKeyEncoder.Encode (TerminalKey.Down, TerminalModifiers.None, applicationCursor: true).Should ().Be ("\x1bOB");
        TerminalKeyEncoder.Encode (TerminalKey.Right, TerminalModifiers.None, applicationCursor: true).Should ().Be ("\x1bOC");
        TerminalKeyEncoder.Encode (TerminalKey.Left, TerminalModifiers.None, applicationCursor: true).Should ().Be ("\x1bOD");
        TerminalKeyEncoder.Encode (TerminalKey.Home, TerminalModifiers.None, applicationCursor: true).Should ().Be ("\x1bOH");
        TerminalKeyEncoder.Encode (TerminalKey.End, TerminalModifiers.None, applicationCursor: false).Should ().Be ("\x1b[F");
    }

    [Fact]
    public void Encode_FunctionKeys ()
    {
        TerminalKeyEncoder.Encode (TerminalKey.F1, TerminalModifiers.None).Should ().Be ("\x1bOP");
        TerminalKeyEncoder.Encode (TerminalKey.F4, TerminalModifiers.None).Should ().Be ("\x1bOS");
        TerminalKeyEncoder.Encode (TerminalKey.F5, TerminalModifiers.None).Should ().Be ("\x1b[15~");
        TerminalKeyEncoder.Encode (TerminalKey.F12, TerminalModifiers.None).Should ().Be ("\x1b[24~");
    }

    [Fact]
    public void Encode_Alt_PrefixesEscape ()
    {
        TerminalKeyEncoder.Encode (TerminalKey.A, TerminalModifiers.Alt).Should ().Be ("\x1b" + "a");
        TerminalKeyEncoder.Encode (TerminalKey.Enter, TerminalModifiers.Alt).Should ().Be ("\x1b\r");
        TerminalKeyEncoder.Encode (TerminalKey.C, TerminalModifiers.Alt | TerminalModifiers.Control).Should ().Be ("\x1b\x03");
    }

    [Fact]
    public void Encode_NumPad ()
    {
        TerminalKeyEncoder.Encode (TerminalKey.NumPad0, TerminalModifiers.None).Should ().Be ("0");
        TerminalKeyEncoder.Encode (TerminalKey.NumPad9, TerminalModifiers.None).Should ().Be ("9");
        TerminalKeyEncoder.Encode (TerminalKey.NumPadAdd, TerminalModifiers.None).Should ().Be ("+");
        TerminalKeyEncoder.Encode (TerminalKey.NumPadSubtract, TerminalModifiers.None).Should ().Be ("-");
        TerminalKeyEncoder.Encode (TerminalKey.NumPadMultiply, TerminalModifiers.None).Should ().Be ("*");
        TerminalKeyEncoder.Encode (TerminalKey.NumPadDivide, TerminalModifiers.None).Should ().Be ("/");
        TerminalKeyEncoder.Encode (TerminalKey.NumPadDecimal, TerminalModifiers.None).Should ().Be (".");
        TerminalKeyEncoder.Encode (TerminalKey.NumPadEqual, TerminalModifiers.None).Should ().Be ("=");
    }

    [Fact]
    public void Encode_UnmappedKey_ReturnsNull ()
    {
        TerminalKeyEncoder.Encode (TerminalKey.None, TerminalModifiers.None).Should ().BeNull ();
    }

    #endregion

    #region EncodeSpecial

    [Fact]
    public void EncodeSpecial_ReturnsNullForPrintables ()
    {
        TerminalKeyEncoder.EncodeSpecial (TerminalKey.A).Should ().BeNull ();
        TerminalKeyEncoder.EncodeSpecial (TerminalKey.D5).Should ().BeNull ();
        TerminalKeyEncoder.EncodeSpecial (TerminalKey.Space).Should ().BeNull ();
    }

    [Fact]
    public void EncodeSpecial_HonorsApplicationCursorMode ()
    {
        TerminalKeyEncoder.EncodeSpecial (TerminalKey.Up).Should ().Be ("\x1b[A");
        TerminalKeyEncoder.EncodeSpecial (TerminalKey.Up, applicationCursor: true).Should ().Be ("\x1bOA");
    }

    #endregion

    #region EncodeComposed

    [Fact]
    public void EncodeComposed_PassesPrintablesThrough ()
    {
        TerminalKeyEncoder.EncodeComposed ('a', TerminalModifiers.None).Should ().Be ("a");
        TerminalKeyEncoder.EncodeComposed ('é', TerminalModifiers.None).Should ().Be ("é");
        TerminalKeyEncoder.EncodeComposed (0x1F600, TerminalModifiers.None).Should ().Be ("\U0001F600");
    }

    [Fact]
    public void EncodeComposed_CtrlTransformsToC0 ()
    {
        TerminalKeyEncoder.EncodeComposed ('a', TerminalModifiers.Control).Should ().Be ("\x01");
        TerminalKeyEncoder.EncodeComposed ('[', TerminalModifiers.Control).Should ().Be ("\x1b");
        TerminalKeyEncoder.EncodeComposed (' ', TerminalModifiers.Control).Should ().Be ("\x00");
    }

    [Fact]
    public void EncodeComposed_AltPrefixesEscape ()
    {
        TerminalKeyEncoder.EncodeComposed ('x', TerminalModifiers.Alt).Should ().Be ("\x1b" + "x");
    }

    [Fact]
    public void EncodeComposed_RejectsNonPrintables ()
    {
        TerminalKeyEncoder.EncodeComposed (0, TerminalModifiers.None).Should ().BeNull ();
        TerminalKeyEncoder.EncodeComposed (0x08, TerminalModifiers.None).Should ().BeNull ();
        TerminalKeyEncoder.EncodeComposed (0x7f, TerminalModifiers.None).Should ().BeNull ();
        TerminalKeyEncoder.EncodeComposed (0x110000, TerminalModifiers.None).Should ().BeNull ();
    }

    #endregion
}
