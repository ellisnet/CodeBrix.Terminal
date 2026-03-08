using CodeBrix.Terminal.Engine.CsiCommandExtensions;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Terminal.Engine.Tests.EscapeSequence;

/// <summary>
/// Tests for scrolling operations: Index (IND), ReverseIndex (RI),
/// Scroll Up (SU), Scroll Down (SD), and ScrollLines.
/// </summary>
public class ScrollTests : BaseTerminalTests
{
    // ESC D — Index: moves cursor down one line, scrolls if at bottom of scroll region
    [Fact]
    public void Index_MovesCursorDown ()
    {
        Terminal.csiCUP ((1, 1));
        Terminal.Feed ("\u001bD"); // ESC D = Index
        Terminal.Buffer.Y.Should ().Be (1);
    }

    [Fact]
    public void Index_ScrollsAtScrollBottom ()
    {
        // Set scroll region rows 1-5, cursor at row 5 (bottom of region)
        Terminal.Feed ("\x1b[1;5r");
        Terminal.csiCUP ((1, 5));
        Terminal.Buffer.Y.Should ().Be (4); // 0-based

        // Write a character at row 5, col 1 so we can verify it scrolled
        Terminal.Feed ("X");
        Terminal.csiCUP ((1, 5));

        // Index at scroll bottom should scroll, not move cursor
        Terminal.Feed ("\u001bD");
        Terminal.Buffer.Y.Should ().Be (4); // cursor stays at scroll bottom

        // The 'X' that was at row 5 should now be at row 4 (scrolled up)
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 3] [0].Code.Should ().Be ((int)'X');
    }

    [Fact]
    public void Index_DoesNotScrollOutsideRegion ()
    {
        // Set scroll region rows 3-5
        Terminal.Feed ("\x1b[3;5r");
        Terminal.csiCUP ((1, 2)); // row 2, above scroll region
        Terminal.Buffer.Y.Should ().Be (1);

        Terminal.Feed ("\u001bD");
        Terminal.Buffer.Y.Should ().Be (2); // just moves down, no scroll
    }

    // ESC M — Reverse Index: moves cursor up one line, scrolls down if at top of scroll region
    [Fact]
    public void ReverseIndex_MovesCursorUp ()
    {
        Terminal.csiCUP ((1, 3));
        Terminal.Feed ("\x1bM"); // ESC M = Reverse Index
        Terminal.Buffer.Y.Should ().Be (1); // moved from row 3 (0-based 2) to row 2 (0-based 1)
    }

    [Fact]
    public void ReverseIndex_ScrollsDownAtScrollTop ()
    {
        // Set scroll region rows 3-6
        Terminal.Feed ("\x1b[3;6r");
        Terminal.csiCUP ((1, 3)); // row 3 = ScrollTop
        Terminal.Buffer.Y.Should ().Be (2);

        // Write a char so we can track it
        Terminal.Feed ("Z");
        Terminal.csiCUP ((1, 3));

        // Reverse Index at scroll top should scroll down (insert blank line at top)
        Terminal.Feed ("\x1bM");
        Terminal.Buffer.Y.Should ().Be (2); // cursor stays at scroll top

        // 'Z' should have moved down one row
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 3] [0].Code.Should ().Be ((int)'Z');
    }

    // CSI Ps S — Scroll Up (SU)
    [Fact]
    public void SU_ScrollsUpOneLine ()
    {
        // Write content on first few rows
        Terminal.csiCUP ((1, 1));
        Terminal.Feed ("AAAA\r\nBBBB\r\nCCCC\r\nDDDD");

        // Scroll up 1 line
        Terminal.Feed ("\x1b[1S");

        // Row 0 should now have 'BBBB' (content shifted up)
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [0].Code.Should ().Be ((int)'B');
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 1] [0].Code.Should ().Be ((int)'C');
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 2] [0].Code.Should ().Be ((int)'D');
    }

    [Fact]
    public void SU_ScrollsUpMultipleLines ()
    {
        Terminal.csiCUP ((1, 1));
        Terminal.Feed ("AAAA\r\nBBBB\r\nCCCC\r\nDDDD\r\nEEEE");

        Terminal.Feed ("\x1b[3S"); // scroll up 3 lines

        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [0].Code.Should ().Be ((int)'D');
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 1] [0].Code.Should ().Be ((int)'E');
    }

    [Fact]
    public void SU_RespectsScrollRegion ()
    {
        Terminal.csiCUP ((1, 1));
        Terminal.Feed ("AAAA\r\nBBBB\r\nCCCC\r\nDDDD\r\nEEEE");

        // Set scroll region to rows 2-4
        Terminal.Feed ("\x1b[2;4r");

        Terminal.Feed ("\x1b[1S"); // scroll up 1 within region

        // Row 0 (outside region) should be unchanged
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [0].Code.Should ().Be ((int)'A');
        // Row 1 should now have 'CCCC'
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 1] [0].Code.Should ().Be ((int)'C');
        // Row 2 should now have 'DDDD'
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 2] [0].Code.Should ().Be ((int)'D');
        // Row 4 (outside region) should be unchanged
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 4] [0].Code.Should ().Be ((int)'E');
    }

    // CSI Ps T — Scroll Down (SD)
    [Fact]
    public void SD_ScrollsDownOneLine ()
    {
        Terminal.csiCUP ((1, 1));
        Terminal.Feed ("AAAA\r\nBBBB\r\nCCCC\r\nDDDD");

        Terminal.Feed ("\x1b[1T");

        // Row 0 should be blank (scrolled down, blank inserted at top)
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [0].Code.Should ().Be (0);
        // Row 1 should now have 'AAAA'
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 1] [0].Code.Should ().Be ((int)'A');
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 2] [0].Code.Should ().Be ((int)'B');
    }

    [Fact]
    public void SD_RespectsScrollRegion ()
    {
        Terminal.csiCUP ((1, 1));
        Terminal.Feed ("AAAA\r\nBBBB\r\nCCCC\r\nDDDD\r\nEEEE");

        // Set scroll region to rows 2-4
        Terminal.Feed ("\x1b[2;4r");

        Terminal.Feed ("\x1b[1T"); // scroll down 1 within region

        // Row 0 (outside region) should be unchanged
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [0].Code.Should ().Be ((int)'A');
        // Row 1 (top of region) should now be blank
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 1] [0].Code.Should ().Be (0);
        // Row 2 should now have 'BBBB' (shifted down)
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 2] [0].Code.Should ().Be ((int)'B');
        // Row 4 (outside region) should be unchanged
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 4] [0].Code.Should ().Be ((int)'E');
    }

    [Fact]
    public void SD_ScrollsDownMultipleLines ()
    {
        Terminal.csiCUP ((1, 1));
        Terminal.Feed ("AAAA\r\nBBBB\r\nCCCC\r\nDDDD\r\nEEEE");

        Terminal.Feed ("\x1b[3T"); // scroll down 3 lines

        // Rows 0-2 should be blank
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [0].Code.Should ().Be (0);
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 1] [0].Code.Should ().Be (0);
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 2] [0].Code.Should ().Be (0);
        // Rows 3-4 should have the original rows 0-1
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 3] [0].Code.Should ().Be ((int)'A');
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 4] [0].Code.Should ().Be ((int)'B');
    }

    [Fact]
    public void SD_DoesNotMoveCursor ()
    {
        Terminal.csiCUP ((1, 1));
        Terminal.Feed ("AAAA\r\nBBBB\r\nCCCC\r\nDDDD");

        // Position cursor at row 2, col 3
        Terminal.csiCUP ((3, 2));
        var cursorX = Terminal.Buffer.X;
        var cursorY = Terminal.Buffer.Y;

        Terminal.Feed ("\x1b[2T");

        // Cursor should not have moved
        Terminal.Buffer.X.Should ().Be (cursorX);
        Terminal.Buffer.Y.Should ().Be (cursorY);
    }

    [Fact]
    public void SD_BottomRowIsLost ()
    {
        Terminal.csiCUP ((1, 1));
        Terminal.Feed ("AAAA\r\nBBBB\r\nCCCC\r\nDDDD\r\nEEEE");

        // Set scroll region to rows 2-4 (0-based rows 1-3)
        Terminal.Feed ("\x1b[2;4r");

        // Row 3 (0-based) has 'DDDD' — the bottom of the scroll region
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 3] [0].Code.Should ().Be ((int)'D');

        Terminal.Feed ("\x1b[1T"); // scroll down 1

        // 'DDDD' should be gone — pushed off the bottom of the scroll region
        // Row 3 should now have 'CCCC' (shifted down from row 2)
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 3] [0].Code.Should ().Be ((int)'C');
    }

    [Fact]
    public void SD_DefaultParameterScrollsOneLine ()
    {
        Terminal.csiCUP ((1, 1));
        Terminal.Feed ("AAAA\r\nBBBB\r\nCCCC");

        // CSI T with no explicit parameter — should default to 1
        Terminal.Feed ("\x1b[T");

        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [0].Code.Should ().Be (0);
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 1] [0].Code.Should ().Be ((int)'A');
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 2] [0].Code.Should ().Be ((int)'B');
    }

    [Fact]
    public void SD_ExceedingRegionSizeBlanksEntireRegion ()
    {
        Terminal.csiCUP ((1, 1));
        Terminal.Feed ("AAAA\r\nBBBB\r\nCCCC\r\nDDDD\r\nEEEE");

        // Set scroll region to rows 2-4 (3 rows)
        Terminal.Feed ("\x1b[2;4r");

        // Scroll down 10 lines — more than the 3-row region
        Terminal.Feed ("\x1b[10T");

        // All rows in the region should be blank
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 1] [0].Code.Should ().Be (0);
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 2] [0].Code.Should ().Be (0);
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 3] [0].Code.Should ().Be (0);

        // Rows outside the region should be unchanged
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [0].Code.Should ().Be ((int)'A');
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 4] [0].Code.Should ().Be ((int)'E');
    }

    [Fact]
    public void SD_ThenSU_RestoresContent ()
    {
        Terminal.csiCUP ((1, 1));
        Terminal.Feed ("AAAA\r\nBBBB\r\nCCCC\r\nDDDD\r\nEEEE");

        // Set scroll region to rows 1-5 (full height of our content)
        Terminal.Feed ("\x1b[1;5r");

        // Scroll down 1, then scroll up 1
        Terminal.Feed ("\x1b[1T");
        Terminal.Feed ("\x1b[1S");

        // SD inserts blank at top, pushes 'EEEE' off the bottom.
        // SU then removes that blank from the top, inserts blank at bottom.
        // Net result: AAAA-DDDD survive, EEEE is lost, bottom row is blank.
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [0].Code.Should ().Be ((int)'A');
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 1] [0].Code.Should ().Be ((int)'B');
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 2] [0].Code.Should ().Be ((int)'C');
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 3] [0].Code.Should ().Be ((int)'D');
        // Row 4 should be blank — 'EEEE' was lost during SD, SU inserted blank here
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 4] [0].Code.Should ().Be (0);
    }

    [Fact]
    public void SD_RespectsScrollRegion_FullRowVerification ()
    {
        Terminal.csiCUP ((1, 1));
        Terminal.Feed ("AAAA\r\nBBBB\r\nCCCC\r\nDDDD\r\nEEEE\r\nFFFF\r\nGGGG");

        // Set scroll region to rows 3-6 (0-based rows 2-5)
        Terminal.Feed ("\x1b[3;6r");

        Terminal.Feed ("\x1b[2T"); // scroll down 2 within region

        // Row 0 (above region) — unchanged
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [0].Code.Should ().Be ((int)'A');
        // Row 1 (above region) — unchanged
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 1] [0].Code.Should ().Be ((int)'B');
        // Row 2 (top of region) — blank (inserted)
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 2] [0].Code.Should ().Be (0);
        // Row 3 — blank (inserted)
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 3] [0].Code.Should ().Be (0);
        // Row 4 — was 'CCCC', shifted down 2 from row 2
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 4] [0].Code.Should ().Be ((int)'C');
        // Row 5 (bottom of region) — was 'DDDD', shifted down 2 from row 3
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 5] [0].Code.Should ().Be ((int)'D');
        // Row 6 (below region) — unchanged
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 6] [0].Code.Should ().Be ((int)'G');
    }

    // ScrollLines — viewport scrolling
    [Fact]
    public void ScrollLines_ScrollsViewportDown ()
    {
        // Fill terminal with enough content to have scrollback
        var terminal = new Terminal (null, new TerminalOptions () { Cols = 80, Rows = 5 });
        for (int i = 0; i < 20; i++) {
            terminal.Feed ($"Line {i}\r\n");
        }

        var yBase = terminal.Buffer.YBase;
        yBase.Should ().BeGreaterThan (0, "because content should have created scrollback");

        // Scroll up (negative disp)
        terminal.ScrollLines (-3);
        terminal.Buffer.YDisp.Should ().Be (yBase - 3);
    }

    [Fact]
    public void ScrollLines_ClampsAtTop ()
    {
        var terminal = new Terminal (null, new TerminalOptions () { Cols = 80, Rows = 5 });
        for (int i = 0; i < 20; i++) {
            terminal.Feed ($"Line {i}\r\n");
        }

        // Scroll up past the beginning
        terminal.ScrollLines (-10000);
        terminal.Buffer.YDisp.Should ().Be (0);
    }

    [Fact]
    public void ScrollLines_ClampsAtBottom ()
    {
        var terminal = new Terminal (null, new TerminalOptions () { Cols = 80, Rows = 5 });
        for (int i = 0; i < 20; i++) {
            terminal.Feed ($"Line {i}\r\n");
        }

        var yBase = terminal.Buffer.YBase;

        // Scroll up, then try to scroll down past YBase
        terminal.ScrollLines (-5);
        terminal.ScrollLines (10000);
        terminal.Buffer.YDisp.Should ().Be (yBase);
    }
}
