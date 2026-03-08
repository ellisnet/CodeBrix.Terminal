using CodeBrix.Terminal.Engine.CommandExtensions;
using SilverAssertions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xunit;

#pragma warning disable IDE0004

namespace CodeBrix.Terminal.Engine.Tests;

public class BaseTerminalTests : IDisposable
{
    TerminalDelegate _terminalDelegate;
    int _gNextId;

    public BaseTerminalTests ()
    {
        _terminalDelegate = new TerminalDelegate ();
        Terminal = new Terminal (_terminalDelegate);
    }

    public Terminal Terminal { get; private set; }

    public void Dispose ()
    {
        _terminalDelegate = null;
        Terminal = null;
    }

    public int[] ReadCsiResponse (string final)
    {
        return _terminalDelegate.ReadCsi (final);
    }

    [Theory]
    [InlineData (1, 1, 1, 1, " ")]       // single empty cell matches a space
    [InlineData (1, 1, 3, 1, "   ")]      // 3 empty cells in one row match 3 spaces
    [InlineData (1, 1, 2, 2, "    ")]     // 2x2 empty cells match 4 spaces
    [InlineData (1, 1, 1, 3, "   ")]      // 1 column x 3 rows of empty cells match 3 spaces
    [InlineData (1, 1, 4, 2, "        ")] // 4x2 empty cells match 8 spaces
    public void AssertScreenCharsInRectEqual (int left, int top, int right, int bottom, string text)
    {
        var rect = (left, top, right, bottom);
        var pid = ++_gNextId;
        Terminal.csiDECRQCRA (pid, 0, rect.top, rect.left, rect.bottom, rect.right);
        var response = _terminalDelegate.ReadDcs ();

        string pidStr = pid.ToString ();
        response.Should ().StartWith (pidStr);
        response = response.Substring (pidStr.Length);

        response.Should ().StartWith ("!~");
        response = response.Substring (2);

        int.TryParse (response, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int checksum)
            .Should ().BeTrue ("because a valid checksum should have been received");

        int expectedChecksum = 0;
        foreach (var ch in text) {
            expectedChecksum += (int)ch;
        }

        checksum.Should ().Be (expectedChecksum);
    }

    static string SetScrollRange (int start, int end) => $"\x1b[{start};{end}r";
    static string CursorDown (int n) => $"\x1b[{n}B";
    static string CursorUp (int n) => $"\x1b[{n}A";
    static string CursorPosition (int row, int col) => $"\x1b[{row};{col}H";
    static string Clear () => "\x1b[2J";

    /// <summary>
    /// Tests that scrolling that happens outside the defined scroll region
    /// is not ignored. For example if the scroll region is 12,13, but the cursor
    /// is at 24, and it writes, new lines are added, and the region 12,13 is not affected.
    /// </summary>
    [Fact]
    public void ScrollingOutsideScrollRegionHappens ()
    {
        var terminal = new Terminal (null, new TerminalOptions () { Cols = 80, Rows = 40 });
        terminal.Feed (Clear () + SetScrollRange (12, 13) + CursorDown (24) + "1\n2\n3\n4\n");

        terminal.Buffer.Lines [24] [0].Code.Should ().Be (49);  // '1'
        terminal.Buffer.Lines [25] [0].Code.Should ().Be (50);  // '2'
        terminal.Buffer.Lines [26] [0].Code.Should ().Be (51);  // '3'
        terminal.Buffer.Lines [27] [0].Code.Should ().Be (52);  // '4'
        terminal.Buffer.Lines [28] [0].Code.Should ().Be (0);   // empty
    }

    #region CursorDown tests

    /// <summary>
    /// Regression: CursorDown from within the scroll region must still stop at ScrollBottom.
    /// </summary>
    [Fact]
    public void CursorDown_InsideScrollRegion_StopsAtScrollBottom ()
    {
        var terminal = new Terminal (null, new TerminalOptions () { Cols = 80, Rows = 40 });
        // Set scroll region rows 12-13 (1-based) → ScrollTop=11, ScrollBottom=12 (0-based)
        // DECSTBM resets cursor to (0,0), then CUP moves it to row 12 (1-based) = row 11 (0-based)
        terminal.Feed (SetScrollRange (12, 13) + CursorPosition (12, 1));
        terminal.Buffer.Y.Should ().Be (11);

        // CUD 10 from within the scroll region should clamp at ScrollBottom (row 12)
        terminal.Feed (CursorDown (10));
        terminal.Buffer.Y.Should ().Be (12);
    }

    /// <summary>
    /// CursorDown from above the scroll region should not be clamped by ScrollBottom.
    /// </summary>
    [Fact]
    public void CursorDown_AboveScrollRegion_NotClampedByScrollBottom ()
    {
        var terminal = new Terminal (null, new TerminalOptions () { Cols = 80, Rows = 40 });
        terminal.Feed (SetScrollRange (12, 13));
        // Cursor is at (0,0) after DECSTBM — above the scroll region
        terminal.Buffer.Y.Should ().Be (0);

        // CUD 5 should freely reach row 5, not clamped to ScrollBottom (12)
        terminal.Feed (CursorDown (5));
        terminal.Buffer.Y.Should ().Be (5);
    }

    /// <summary>
    /// CursorDown from below the scroll region should reach the bottom of the screen.
    /// </summary>
    [Fact]
    public void CursorDown_BelowScrollRegion_ReachesBottomOfScreen ()
    {
        var terminal = new Terminal (null, new TerminalOptions () { Cols = 80, Rows = 40 });
        // Set scroll region, then move cursor to row 20 (1-based) = row 19 (0-based), below scroll region
        terminal.Feed (SetScrollRange (12, 13) + CursorPosition (20, 1));
        terminal.Buffer.Y.Should ().Be (19);

        // CUD 30 should stop at the bottom of the screen (row 39), not at ScrollBottom (12)
        terminal.Feed (CursorDown (30));
        terminal.Buffer.Y.Should ().Be (39);
    }

    /// <summary>
    /// CursorDown with default (full-screen) scroll region should move normally.
    /// </summary>
    [Fact]
    public void CursorDown_WithDefaultScrollRegion_MovesNormally ()
    {
        var terminal = new Terminal (null, new TerminalOptions () { Cols = 80, Rows = 40 });
        // No scroll region set — defaults to ScrollTop=0, ScrollBottom=39
        terminal.Feed (CursorDown (24));
        terminal.Buffer.Y.Should ().Be (24);
    }

    /// <summary>
    /// CursorDown at exactly ScrollTop should be treated as inside the scroll region
    /// and clamped at ScrollBottom.
    /// </summary>
    [Fact]
    public void CursorDown_AtExactlyScrollTop_StopsAtScrollBottom ()
    {
        var terminal = new Terminal (null, new TerminalOptions () { Cols = 80, Rows = 40 });
        // Scroll region rows 12-20 (1-based) → ScrollTop=11, ScrollBottom=19 (0-based)
        terminal.Feed (SetScrollRange (12, 20) + CursorPosition (12, 1));
        terminal.Buffer.Y.Should ().Be (11);

        // CUD 30 from exactly ScrollTop should stop at ScrollBottom (row 19)
        terminal.Feed (CursorDown (30));
        terminal.Buffer.Y.Should ().Be (19);
    }

    #endregion

    #region CursorUp tests

    /// <summary>
    /// Regression: CursorUp from within the scroll region must still stop at ScrollTop.
    /// </summary>
    [Fact]
    public void CursorUp_InsideScrollRegion_StopsAtScrollTop ()
    {
        var terminal = new Terminal (null, new TerminalOptions () { Cols = 80, Rows = 40 });
        // Scroll region rows 12-13 (1-based) → ScrollTop=11, ScrollBottom=12 (0-based)
        // Move cursor to row 13 (1-based) = row 12 (0-based) = ScrollBottom
        terminal.Feed (SetScrollRange (12, 13) + CursorPosition (13, 1));
        terminal.Buffer.Y.Should ().Be (12);

        // CUU 10 from within the scroll region should clamp at ScrollTop (row 11)
        terminal.Feed (CursorUp (10));
        terminal.Buffer.Y.Should ().Be (11);
    }

    /// <summary>
    /// CursorUp from below the scroll region should not be clamped by ScrollTop.
    /// </summary>
    [Fact]
    public void CursorUp_BelowScrollRegion_NotClampedByScrollTop ()
    {
        var terminal = new Terminal (null, new TerminalOptions () { Cols = 80, Rows = 40 });
        // Set scroll region, then move cursor to row 30 (1-based) = row 29 (0-based)
        terminal.Feed (SetScrollRange (12, 13) + CursorPosition (30, 1));
        terminal.Buffer.Y.Should ().Be (29);

        // CUU 25 should freely reach row 4 (29-25), not clamped to ScrollTop (11)
        terminal.Feed (CursorUp (25));
        terminal.Buffer.Y.Should ().Be (4);
    }

    /// <summary>
    /// CursorUp from above the scroll region should reach the top of the screen.
    /// </summary>
    [Fact]
    public void CursorUp_AboveScrollRegion_ReachesTopOfScreen ()
    {
        var terminal = new Terminal (null, new TerminalOptions () { Cols = 80, Rows = 40 });
        // Set scroll region, then move cursor to row 5 (1-based) = row 4 (0-based), above scroll region
        terminal.Feed (SetScrollRange (12, 13) + CursorPosition (5, 1));
        terminal.Buffer.Y.Should ().Be (4);

        // CUU 10 should stop at the top of the screen (row 0), not at ScrollTop (11)
        terminal.Feed (CursorUp (10));
        terminal.Buffer.Y.Should ().Be (0);
    }

    /// <summary>
    /// CursorUp with default (full-screen) scroll region should move normally.
    /// </summary>
    [Fact]
    public void CursorUp_WithDefaultScrollRegion_MovesNormally ()
    {
        var terminal = new Terminal (null, new TerminalOptions () { Cols = 80, Rows = 40 });
        // Move cursor to row 25 (1-based) = row 24 (0-based), then CUU 10
        terminal.Feed (CursorPosition (25, 1) + CursorUp (10));
        terminal.Buffer.Y.Should ().Be (14);
    }

    /// <summary>
    /// CursorUp at exactly ScrollBottom should be treated as inside the scroll region
    /// and clamped at ScrollTop.
    /// </summary>
    [Fact]
    public void CursorUp_AtExactlyScrollBottom_StopsAtScrollTop ()
    {
        var terminal = new Terminal (null, new TerminalOptions () { Cols = 80, Rows = 40 });
        // Scroll region rows 12-20 (1-based) → ScrollTop=11, ScrollBottom=19 (0-based)
        terminal.Feed (SetScrollRange (12, 20) + CursorPosition (20, 1));
        terminal.Buffer.Y.Should ().Be (19);

        // CUU 30 from exactly ScrollBottom should stop at ScrollTop (row 11)
        terminal.Feed (CursorUp (30));
        terminal.Buffer.Y.Should ().Be (11);
    }

    #endregion

    class TerminalDelegate : SimpleTerminalDelegate
    {
        readonly List<byte> _responseBuffer = new List<byte> ();

        public override void Send (byte[] data)
        {
            _responseBuffer.AddRange (data);
        }

        byte ReadNext ()
        {
            if (_responseBuffer.Count > 0) {
                var result = _responseBuffer[0];
                _responseBuffer.RemoveAt (0);
                return result;
            }
            return 0;
        }

        public int[] ReadCsi (string final)
        {
            // Skip CSI introducer: ESC [ (0x1b 0x5b) or single-byte 0x9b
            var b = ReadNext ();
            if (b == 0x1b) {
                ReadNext (); // skip '['
            }

            // Read parameter bytes until we hit the final character
            var parms = new StringBuilder ();
            var finalByte = (byte)final[0];
            while (true) {
                b = ReadNext ();
                if (b == finalByte || b == 0)
                    break;
                parms.Append ((char)b);
            }

            // Parse semicolon-separated parameter values
            var parts = parms.ToString ().Split (';');
            var result = new List<int> ();
            foreach (var part in parts) {
                if (int.TryParse (part, out int val))
                    result.Add (val);
            }
            return result.ToArray ();
        }

        public string ReadDcs ()
        {
            // Skip DCS introducer: ESC P (0x1b 0x50) or single-byte 0x90
            var b = ReadNext ();
            if (b == 0x1b) {
                ReadNext (); // skip 'P'
            }

            // Read content until ST: ESC \ (0x1b 0x5c) or single-byte 0x9c
            var content = new StringBuilder ();
            while (true) {
                b = ReadNext ();
                if (b == 0 || b == 0x9c)
                    break;
                if (b == 0x1b) {
                    var next = ReadNext ();
                    if (next == 0x5c) // '\'
                        break;
                    content.Append ((char)b);
                    content.Append ((char)next);
                    continue;
                }
                content.Append ((char)b);
            }

            return content.ToString ();
        }
    }
}
