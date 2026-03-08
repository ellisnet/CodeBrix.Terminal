using CodeBrix.Terminal.Engine.CommandExtensions;
using CodeBrix.Terminal.Engine.CsiCommandExtensions;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Terminal.Engine.Tests.EscapeSequence;

/// <summary>
/// BS (Backspace) tests
/// </summary>
public class BsTests : BaseTerminalTests
{
    [Fact]
    public void BS_Basic ()
    {
        Terminal.csiCUP ((3, 3));
        Terminal.Backspace ();
        Terminal.AssertCursorPosition (2, 3);
    }

    [Fact]
    public void BS_NoWrapByDefault ()
    {
        Terminal.csiCUP ((1, 3));
        Terminal.Backspace ();
        Terminal.AssertCursorPosition (1, 3);
    }

    [Fact]
    public void BS_WrapsInWraparoundMode ()
    {
        Terminal.csiDECSET (CsiCommandCodes.DECAWM);
        Terminal.csiDECSET (CsiCommandCodes.ReverseWraparound);
        Terminal.csiCUP ((1, 3));
        Terminal.Backspace ();
        var sz = Terminal.GetScreenSize ();
        Terminal.AssertCursorPosition (sz.cols, 2);
    }

    [Fact]
    public void BS_ReverseWrapRequiresDECAWM ()
    {
        Terminal.Wraparound = false;
        Terminal.csiDECSET (CsiCommandCodes.ReverseWraparound);
        Terminal.csiCUP ((1, 3));
        Terminal.Backspace ();
        Terminal.AssertCursorPosition (1, 3);

        Terminal.Wraparound = true;
        Terminal.ReverseWraparound = false;
        Terminal.csiCUP ((1, 3));
        Terminal.Backspace ();
        Terminal.AssertCursorPosition (1, 3);
    }

    [Fact]
    public void BS_ReverseWrapWithLeftRight ()
    {
        Terminal.csiDECSET (CsiCommandCodes.DECAWM);
        Terminal.csiDECSET (CsiCommandCodes.ReverseWraparound);
        Terminal.csiDECSET (CsiCommandCodes.DECLRMM);
        Terminal.csiDECSLRM (5, 10);
        Terminal.csiCUP ((5, 3));
        Terminal.Backspace ();
        Terminal.AssertCursorPosition (10, 2);
    }

    [Fact]
    public void BS_ReversewrapFromLeftEdgeToRightMargin ()
    {
        // If cursor starts at left edge of screen, left of left margin, backspace
        // takes it to the right margin.
        Terminal.csiDECSET (CsiCommandCodes.DECAWM);
        Terminal.csiDECSET (CsiCommandCodes.ReverseWraparound);
        Terminal.csiDECSET (CsiCommandCodes.DECLRMM);
        Terminal.csiDECSLRM (5, 10);
        Terminal.csiCUP ((1, 3));
        Terminal.Backspace ();
        Terminal.AssertCursorPosition (10, 2);
    }

    [Fact]
    public void BS_StopsAtLeftMargin ()
    {
        Terminal.csiDECSET (CsiCommandCodes.DECLRMM);
        Terminal.csiDECSLRM (5, 10);
        Terminal.csiCUP ((5, 1));
        Terminal.Backspace ();
        Terminal.MarginMode = false;
        Terminal.AssertCursorPosition (5, 1);
    }

    [Fact]
    public void BS_MovesLeftWhenLeftOfLeftMargin ()
    {
        Terminal.csiDECSET (CsiCommandCodes.DECLRMM);
        Terminal.csiDECSLRM (5, 10);
        Terminal.csiCUP ((4, 1));
        Terminal.Backspace ();
        Terminal.MarginMode = false;
        Terminal.AssertCursorPosition (3, 1);
    }

    [Fact]
    public void BS_StopsAtOrigin ()
    {
        Terminal.csiCUP ((1, 1));
        Terminal.Backspace ();
        Terminal.AssertCursorPosition (1, 1);
    }

    [Fact]
    public void BS_CursorStartsInDoWrapPosition ()
    {
        // Cursor is right of right edge of screen.
        var size = Terminal.GetScreenSize ();
        Terminal.csiCUP ((size.cols - 1, 1));
        Terminal.Feed ("ab");
        Terminal.Backspace ();
        Terminal.Feed ("X");
        AssertScreenCharsInRectEqual (size.cols - 1, 1, size.cols, 1, "Xb");
    }
}
