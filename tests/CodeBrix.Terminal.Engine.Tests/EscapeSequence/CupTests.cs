using CodeBrix.Terminal.Engine.CommandExtensions;
using CodeBrix.Terminal.Engine.CsiCommandExtensions;
using Xunit;

namespace CodeBrix.Terminal.Engine.Tests.EscapeSequence;

/// <summary>
/// CUP (CursorPosition) tests
/// </summary>
public class CupTests : BaseTerminalTests
{
    [Fact]
    public void CUP_DefaultParams ()
    {
        // With no params, CUP moves to 1,1.
        Terminal.csiCUP ((6, 3));
        Terminal.AssertCursorPosition (6, 3);

        Terminal.csiCUP ();
        Terminal.AssertCursorPosition (1, 1);
    }

    [Fact]
    public void CUP_RowOnly ()
    {
        // Default column is 1.
        Terminal.csiCUP ((6, 3));
        Terminal.AssertCursorPosition (6, 3);

        Terminal.csiCUP (2);
        Terminal.AssertCursorPosition (1, 2);
    }

    [Fact]
    public void CUP_ColumnOnly ()
    {
        // Default row is 1.
        Terminal.csiCUP ((6, 3));
        Terminal.AssertCursorPosition (6, 3);

        Terminal.csiCUP (0, 2);
        Terminal.AssertCursorPosition (2, 1);
    }

    [Fact]
    public void CUP_ZeroIsTreatedAsOne ()
    {
        // Zero args are treated as 1.
        Terminal.csiCUP ((6, 3));
        Terminal.csiCUP (0, 0);
        Terminal.AssertCursorPosition (1, 1);
    }

    [Fact]
    public void CUP_OutOfBoundsParams ()
    {
        // With overly large parameters, CUP moves as far as possible down and right.
        var sz = Terminal.GetScreenSize ();
        Terminal.csiCUP ((sz.cols + 10, sz.rows + 10));
        Terminal.AssertCursorPosition (sz.cols, sz.rows);
    }

    [Fact]
    public void CUP_RespectsOriginMode ()
    {
        // CUP is relative to margins in origin mode.
        // Set a scroll region.
        Terminal.csiDECSTBM (6, 11);
        Terminal.csiDECSET (CsiCommandCodes.DECLRMM);
        Terminal.csiDECSLRM (5, 10);

        // Move to center of region
        Terminal.csiCUP ((7, 9));
        Terminal.AssertCursorPosition (7, 9);

        // Turn on origin mode.
        Terminal.csiDECSET (CsiCommandCodes.DECOM);

        // Move to top-left
        Terminal.csiCUP ((1, 1));

        // Check relative position while still in origin mode.
        Terminal.AssertCursorPosition (1, 1);
        Terminal.Feed ("X");

        // Turn off origin mode. This moves the cursor.
        Terminal.csiDECRESET (CsiCommandCodes.DECOM);

        // Turn off scroll regions so checksum can work.
        Terminal.csiDECSTBM ();
        Terminal.csiDECRESET (CsiCommandCodes.DECLRMM);

        // Make sure there's an X at 5,6
        AssertScreenCharsInRectEqual (5, 6, 5, 6, "X");
    }
}
