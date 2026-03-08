using CodeBrix.Terminal.Engine.CommandExtensions;
using CodeBrix.Terminal.Engine.CsiCommandExtensions;
using Xunit;

namespace CodeBrix.Terminal.Engine.Tests.EscapeSequence;

/// <summary>
/// CR (CarriageReturn) tests
/// </summary>
public class CrTests : BaseTerminalTests
{
    [Fact]
    public void CR_Basic ()
    {
        Terminal.csiCUP ((3, 3));
        Terminal.CarriageReturn ();
        Terminal.AssertCursorPosition (1, 3);
    }

    [Fact]
    public void CR_MovesToLeftMarginWhenRightOfLeftMargin ()
    {
        // Move the cursor to the left margin if it starts right of it.
        Terminal.csiDECSET (CsiCommandCodes.DECLRMM);
        Terminal.csiDECSLRM (5, 10);
        Terminal.csiCUP ((6, 1));
        Terminal.CarriageReturn ();
        Terminal.csiDECRESET (CsiCommandCodes.DECLRMM);
        Terminal.AssertCursorPosition (5, 1);
    }

    [Fact]
    public void CR_MovesToLeftOfScreenWhenLeftOfLeftMargin ()
    {
        // Move the cursor to the left edge of the screen when it starts of left the margin.
        Terminal.csiDECSET (CsiCommandCodes.DECLRMM);
        Terminal.csiDECSLRM (5, 10);
        Terminal.csiCUP ((4, 1));
        Terminal.CarriageReturn ();
        Terminal.csiDECRESET (CsiCommandCodes.DECLRMM);
        Terminal.AssertCursorPosition (1, 1);
    }

    [Fact]
    public void CR_StaysPutWhenAtLeftMargin ()
    {
        Terminal.csiDECSET (CsiCommandCodes.DECLRMM);
        Terminal.csiDECSLRM (5, 10);
        Terminal.csiCUP ((5, 1));
        Terminal.CarriageReturn ();
        Terminal.csiDECRESET (CsiCommandCodes.DECLRMM);
        Terminal.AssertCursorPosition (5, 1);
    }

    [Fact]
    public void CR_MovesToLeftMarginWhenLeftOfLeftMarginInOriginMode ()
    {
        // In origin mode, always go to the left margin, even if the cursor starts left of it.
        Terminal.csiDECSET (CsiCommandCodes.DECLRMM);
        Terminal.csiDECSLRM (5, 10);
        Terminal.csiDECSET (CsiCommandCodes.DECOM);
        Terminal.csiCUP ((4, 1));
        Terminal.CarriageReturn ();
        Terminal.csiDECRESET (CsiCommandCodes.DECLRMM);
        Terminal.Feed ("x");
        Terminal.csiDECRESET (CsiCommandCodes.DECOM);
        AssertScreenCharsInRectEqual (5, 1, 5, 1, "x");
    }
}
