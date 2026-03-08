using CodeBrix.Terminal.Engine.CommandExtensions;
using CodeBrix.Terminal.Engine.CsiCommandExtensions;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Terminal.Engine.Tests.EscapeSequence;

/// <summary>
/// Tests for erase operations:
/// CSI J (ED — Erase in Display), CSI K (EL — Erase in Line),
/// CSI X (ECH — Erase Characters), DECERA, DECSERA.
/// </summary>
public class EraseTests : BaseTerminalTests
{
    void WriteTestContent ()
    {
        Terminal.csiCUP ((1, 1));
        Terminal.Feed ("abcdefgh\r\n");
        Terminal.Feed ("ijklmnop\r\n");
        Terminal.Feed ("qrstuvwx\r\n");
        Terminal.Feed ("yz012345");
    }

    #region CSI J — Erase in Display (ED)

    [Fact]
    public void ED_EraseBelow_Default ()
    {
        WriteTestContent ();
        // Move cursor to row 2, col 3
        Terminal.csiCUP ((3, 2));
        // CSI 0 J — erase from cursor to end of display
        Terminal.Feed ("\x1b[0J");

        // Row 1 should be intact
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [0].Code.Should ().Be ((int)'a');
        // Row 2, cols 0-1 should be intact, col 2+ erased
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 1] [0].Code.Should ().Be ((int)'i');
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 1] [1].Code.Should ().Be ((int)'j');
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 1] [2].Code.Should ().Be (0);
        // Row 3 should be erased
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 2] [0].Code.Should ().Be (0);
    }

    [Fact]
    public void ED_EraseAbove ()
    {
        WriteTestContent ();
        // Move cursor to row 3, col 3
        Terminal.csiCUP ((3, 3));
        // CSI 1 J — erase from beginning to cursor
        Terminal.Feed ("\x1b[1J");

        // Row 1 should be erased
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [0].Code.Should ().Be (0);
        // Row 2 should be erased
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 1] [0].Code.Should ().Be (0);
        // Row 3, cols 0-2 should be erased, col 3+ intact
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 2] [2].Code.Should ().Be (0);
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 2] [3].Code.Should ().Be ((int)'t');
        // Row 4 should be intact
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 3] [0].Code.Should ().Be ((int)'y');
    }

    [Fact]
    public void ED_EraseAll ()
    {
        WriteTestContent ();
        // CSI 2 J — erase entire display
        Terminal.Feed ("\x1b[2J");

        for (int row = 0; row < 4; row++) {
            Terminal.Buffer.Lines [Terminal.Buffer.YBase + row] [0].Code.Should ().Be (0);
        }
    }

    [Fact]
    public void ED_EraseScrollback ()
    {
        // Create enough content to have scrollback
        var terminal = new Terminal (null, new TerminalOptions () { Cols = 80, Rows = 5 });
        for (int i = 0; i < 20; i++) {
            terminal.Feed ($"Line {i}\r\n");
        }
        var yBaseBefore = terminal.Buffer.YBase;
        yBaseBefore.Should ().BeGreaterThan (0, "because there should be scrollback");

        // CSI 3 J — erase scrollback
        terminal.Feed ("\x1b[3J");
        terminal.Buffer.YBase.Should ().BeLessThan (yBaseBefore);
    }

    #endregion

    #region CSI K — Erase in Line (EL)

    [Fact]
    public void EL_EraseToRight_Default ()
    {
        WriteTestContent ();
        Terminal.csiCUP ((3, 1));
        // CSI 0 K — erase from cursor to end of line
        Terminal.Feed ("\x1b[0K");

        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [0].Code.Should ().Be ((int)'a');
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [1].Code.Should ().Be ((int)'b');
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [2].Code.Should ().Be (0);
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [7].Code.Should ().Be (0);
        // Other rows unaffected
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 1] [0].Code.Should ().Be ((int)'i');
    }

    [Fact]
    public void EL_EraseToLeft ()
    {
        WriteTestContent ();
        Terminal.csiCUP ((4, 1));
        // CSI 1 K — erase from beginning of line to cursor
        Terminal.Feed ("\x1b[1K");

        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [0].Code.Should ().Be (0);
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [1].Code.Should ().Be (0);
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [2].Code.Should ().Be (0);
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [3].Code.Should ().Be (0);
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [4].Code.Should ().Be ((int)'e');
    }

    [Fact]
    public void EL_EraseEntireLine ()
    {
        WriteTestContent ();
        Terminal.csiCUP ((3, 2));
        // CSI 2 K — erase entire line
        Terminal.Feed ("\x1b[2K");

        for (int col = 0; col < 8; col++) {
            Terminal.Buffer.Lines [Terminal.Buffer.YBase + 1] [col].Code.Should ().Be (0);
        }
        // Other rows unaffected
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [0].Code.Should ().Be ((int)'a');
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 2] [0].Code.Should ().Be ((int)'q');
    }

    #endregion

    #region CSI X — Erase Characters (ECH)

    [Fact]
    public void ECH_ErasesCharactersFromCursor ()
    {
        WriteTestContent ();
        Terminal.csiCUP ((3, 1));
        // CSI 3 X — erase 3 characters starting at cursor
        Terminal.Feed ("\x1b[3X");

        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [0].Code.Should ().Be ((int)'a');
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [1].Code.Should ().Be ((int)'b');
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [2].Code.Should ().Be (0);
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [3].Code.Should ().Be (0);
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [4].Code.Should ().Be (0);
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [5].Code.Should ().Be ((int)'f');
    }

    [Fact]
    public void ECH_DefaultErasesOne ()
    {
        WriteTestContent ();
        Terminal.csiCUP ((2, 1));
        // CSI X (no param) — erase 1 character
        Terminal.Feed ("\x1b[X");

        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [0].Code.Should ().Be ((int)'a');
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [1].Code.Should ().Be (0);
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [2].Code.Should ().Be ((int)'c');
    }

    #endregion

    #region DECERA — Erase Rectangular Area

    [Fact]
    public void DECERA_ErasesRectangle ()
    {
        WriteTestContent ();
        // DECERA: CSI Pt ; Pl ; Pb ; Pr $ z — erase rectangle (2,2)-(3,4)
        Terminal.csiDECERA (new [] { 2, 2, 3, 4 });

        // Row 1 unaffected
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [0].Code.Should ().Be ((int)'a');
        // Row 2, col 1 should be erased (space)
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 1] [1].Code.Should ().Be (32);
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 1] [2].Code.Should ().Be (32);
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 1] [3].Code.Should ().Be (32);
        // Row 2, col 0 unaffected
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 1] [0].Code.Should ().Be ((int)'i');
        // Row 3 within rect erased
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 2] [1].Code.Should ().Be (32);
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 2] [2].Code.Should ().Be (32);
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 2] [3].Code.Should ().Be (32);
        // Row 3, outside rect unaffected
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 2] [4].Code.Should ().Be ((int)'u');
        // Row 4 unaffected
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 3] [0].Code.Should ().Be ((int)'y');
    }

    [Fact]
    public void DECERA_InvalidRectDoesNothing ()
    {
        WriteTestContent ();
        // Invalid rectangle (bottom < top)
        Terminal.csiDECERA (new [] { 3, 3, 1, 1 });
        // Content should be unchanged
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [0].Code.Should ().Be ((int)'a');
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 2] [0].Code.Should ().Be ((int)'q');
    }

    #endregion

    #region DECSERA — Selective Erase Rectangular Area

    [Fact]
    public void DECSERA_ErasesRectangle ()
    {
        WriteTestContent ();
        // DECSERA: CSI Pt ; Pl ; Pb ; Pr $ { — erase rectangle (1,1)-(2,3)
        Terminal.csiDECSERA (1, 1, 2, 3);

        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [0].Code.Should ().Be (32);
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [1].Code.Should ().Be (32);
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [2].Code.Should ().Be (32);
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [3].Code.Should ().Be ((int)'d');
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 1] [0].Code.Should ().Be (32);
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 1] [2].Code.Should ().Be (32);
    }

    #endregion
}
