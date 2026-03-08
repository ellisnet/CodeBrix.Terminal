using CodeBrix.Terminal.Engine.CsiCommandExtensions;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Terminal.Engine.Tests.EscapeSequence;

/// <summary>
/// Tests for SGR (Select Graphic Rendition) — CSI m.
/// Verifies character attribute flags and color settings.
/// </summary>
public class SgrTests : BaseTerminalTests
{
    int GetFlags () => Terminal.CurAttr >> 18;
    int GetFg () => (Terminal.CurAttr >> 9) & 0x1ff;
    int GetBg () => Terminal.CurAttr & 0x1ff;

    [Fact]
    public void SGR_Reset ()
    {
        // Set bold, then reset
        Terminal.Feed ("\x1b[1m");
        GetFlags ().Should ().NotBe (0, "because bold flag should be set");

        Terminal.Feed ("\x1b[0m");
        Terminal.CurAttr.Should ().Be (CharData.DefaultAttr);
    }

    [Fact]
    public void SGR_Bold ()
    {
        Terminal.Feed ("\x1b[1m");
        (GetFlags () & 1).Should ().Be (1, "because BOLD flag bit should be set");
    }

    [Fact]
    public void SGR_Dim ()
    {
        Terminal.Feed ("\x1b[2m");
        // DIM flag = bit 5 (0x20 = 32)
        (GetFlags () & 0x20).Should ().NotBe (0, "because DIM flag should be set");
    }

    [Fact]
    public void SGR_Italic ()
    {
        Terminal.Feed ("\x1b[3m");
        // ITALIC flag = bit 6 (0x40 = 64)
        (GetFlags () & 0x40).Should ().NotBe (0, "because ITALIC flag should be set");
    }

    [Fact]
    public void SGR_Underline ()
    {
        Terminal.Feed ("\x1b[4m");
        // UNDERLINE flag = bit 1 (0x02 = 2)
        (GetFlags () & 2).Should ().NotBe (0, "because UNDERLINE flag should be set");
    }

    [Fact]
    public void SGR_Blink ()
    {
        Terminal.Feed ("\x1b[5m");
        // BLINK flag = bit 2 (0x04 = 4)
        (GetFlags () & 4).Should ().NotBe (0, "because BLINK flag should be set");
    }

    [Fact]
    public void SGR_Inverse ()
    {
        Terminal.Feed ("\x1b[7m");
        // INVERSE flag = bit 3 (0x08 = 8)
        (GetFlags () & 8).Should ().NotBe (0, "because INVERSE flag should be set");
    }

    [Fact]
    public void SGR_Invisible ()
    {
        Terminal.Feed ("\x1b[8m");
        // INVISIBLE flag = bit 4 (0x10 = 16)
        (GetFlags () & 0x10).Should ().NotBe (0, "because INVISIBLE flag should be set");
    }

    [Fact]
    public void SGR_ResetBold ()
    {
        Terminal.Feed ("\x1b[1m"); // bold on
        Terminal.Feed ("\x1b[22m"); // reset bold and dim
        (GetFlags () & 1).Should ().Be (0, "because BOLD flag should be cleared");
    }

    [Fact]
    public void SGR_ResetItalic ()
    {
        Terminal.Feed ("\x1b[3m");
        Terminal.Feed ("\x1b[23m");
        (GetFlags () & 0x40).Should ().Be (0, "because ITALIC flag should be cleared");
    }

    [Fact]
    public void SGR_ResetUnderline ()
    {
        Terminal.Feed ("\x1b[4m");
        Terminal.Feed ("\x1b[24m");
        (GetFlags () & 2).Should ().Be (0, "because UNDERLINE flag should be cleared");
    }

    [Fact]
    public void SGR_ResetBlink ()
    {
        Terminal.Feed ("\x1b[5m");
        Terminal.Feed ("\x1b[25m");
        (GetFlags () & 4).Should ().Be (0, "because BLINK flag should be cleared");
    }

    [Fact]
    public void SGR_ResetInverse ()
    {
        Terminal.Feed ("\x1b[7m");
        Terminal.Feed ("\x1b[27m");
        (GetFlags () & 8).Should ().Be (0, "because INVERSE flag should be cleared");
    }

    [Fact]
    public void SGR_ResetInvisible ()
    {
        Terminal.Feed ("\x1b[8m");
        Terminal.Feed ("\x1b[28m");
        (GetFlags () & 0x10).Should ().Be (0, "because INVISIBLE flag should be cleared");
    }

    [Fact]
    public void SGR_ForegroundColor8 ()
    {
        // ESC[31m — red foreground
        Terminal.Feed ("\x1b[31m");
        GetFg ().Should ().Be (1, "because red is color index 1");
    }

    [Fact]
    public void SGR_BackgroundColor8 ()
    {
        // ESC[44m — blue background
        Terminal.Feed ("\x1b[44m");
        GetBg ().Should ().Be (4, "because blue is color index 4");
    }

    [Fact]
    public void SGR_BrightForeground ()
    {
        // ESC[91m — bright red foreground
        Terminal.Feed ("\x1b[91m");
        GetFg ().Should ().Be (9, "because bright red is color index 9 (1+8)");
    }

    [Fact]
    public void SGR_BrightBackground ()
    {
        // ESC[104m — bright blue background
        Terminal.Feed ("\x1b[104m");
        GetBg ().Should ().Be (12, "because bright blue is color index 12 (4+8)");
    }

    [Fact]
    public void SGR_256ColorForeground ()
    {
        // ESC[38;5;196m — 256-color fg (index 196)
        Terminal.Feed ("\x1b[38;5;196m");
        GetFg ().Should ().Be (196);
    }

    [Fact]
    public void SGR_256ColorBackground ()
    {
        // ESC[48;5;42m — 256-color bg (index 42)
        Terminal.Feed ("\x1b[48;5;42m");
        GetBg ().Should ().Be (42);
    }

    [Fact]
    public void SGR_ResetForeground ()
    {
        Terminal.Feed ("\x1b[31m"); // set red
        Terminal.Feed ("\x1b[39m"); // reset fg
        var defaultFg = (CharData.DefaultAttr >> 9) & 0x1ff;
        GetFg ().Should ().Be (defaultFg);
    }

    [Fact]
    public void SGR_ResetBackground ()
    {
        Terminal.Feed ("\x1b[44m"); // set blue bg
        Terminal.Feed ("\x1b[49m"); // reset bg
        var defaultBg = CharData.DefaultAttr & 0x1ff;
        GetBg ().Should ().Be (defaultBg);
    }

    [Fact]
    public void SGR_MultipleAttributes ()
    {
        // ESC[1;4;31m — bold + underline + red fg
        Terminal.Feed ("\x1b[1;4;31m");
        (GetFlags () & 1).Should ().NotBe (0, "because BOLD should be set");
        (GetFlags () & 2).Should ().NotBe (0, "because UNDERLINE should be set");
        GetFg ().Should ().Be (1, "because red is color index 1");
    }

    [Fact]
    public void SGR_WrittenCharInheritsAttributes ()
    {
        // Set bold + green foreground, write a character
        Terminal.Feed ("\x1b[1;32m");
        Terminal.csiCUP ((1, 1));
        Terminal.Feed ("X");

        var cd = Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [0];
        cd.Code.Should ().Be ((int)'X');
        cd.Attribute.Should ().Be (Terminal.CurAttr);
    }

    [Fact]
    public void SGR_100_SetsBrightBlackBackground ()
    {
        // SGR 100 is the 16-color bright black background (same as SGR 40 + 8).
        // NOTE: When 16-color support is compiled in (as it is here — SGR 90-97
        // handles bright fg), 100-107 are bright background colors per xterm spec.
        // The rxvt interpretation of SGR 100 as "reset fg and bg" only applies
        // when 16-color support is disabled. The dead `p == 100` branch in
        // CharAttributes is unreachable legacy code from that rxvt fallback path.
        Terminal.Feed ("\x1b[100m");
        GetBg ().Should ().Be (8, "because bright black is color index 8 (0+8)");
    }
}
