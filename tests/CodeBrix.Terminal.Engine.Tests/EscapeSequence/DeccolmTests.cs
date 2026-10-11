using SilverAssertions;
using System;
using Xunit;

namespace CodeBrix.Terminal.Engine.Tests.EscapeSequence;

/// <summary>
/// Tests for DECCOLM (CSI ? 3 h / CSI ? 3 l, 132/80 column mode, honoured only while
/// Allow80To132 is on via CSI ? 40 h) and for when the delegate's SizeChanged is raised.
/// </summary>
public class DeccolmTests
{
    const string Allow80To132 = "\u001b[?40h";
    const string Columns132 = "\u001b[?3h";
    const string Columns80 = "\u001b[?3l";

    sealed class CountingDelegate : SimpleTerminalDelegate
    {
        public int SizeChangedCount { get; private set; }

        public int LastCols { get; private set; }

        public string LastFirstRow { get; private set; }

        public int LastCursorX { get; private set; }

        public int LastCursorY { get; private set; }

        public override void SizeChanged (Terminal source)
        {
            SizeChangedCount++;
            LastCols = source.Cols;
            LastFirstRow = RowText (source, 0);
            LastCursorX = source.Buffer.X;
            LastCursorY = source.Buffer.Y;
        }
    }

    static string RowText (Terminal terminal, int row) =>
        terminal.Buffer.Lines [terminal.Buffer.YBase + row].TranslateToString (true).ToString ();

    [Fact]
    public void Deccolm_on_a_headless_terminal_does_not_throw ()
    {
        //Arrange
        var terminal = new Terminal (null, new TerminalOptions { Cols = 80, Rows = 24 });

        //Act
        Action to132 = () => terminal.Feed (Allow80To132 + Columns132);

        //Assert
        to132.Should ().NotThrow ();
        terminal.Cols.Should ().Be (132);
        terminal.Delegate.Should ().NotBeNull ();

        //Act
        Action to80 = () => terminal.Feed (Columns80);

        //Assert
        to80.Should ().NotThrow ();
        terminal.Cols.Should ().Be (80);
    }

    [Fact]
    public void Deccolm_is_ignored_while_Allow80To132_is_off ()
    {
        //Arrange
        var terminalDelegate = new CountingDelegate ();
        var terminal = new Terminal (terminalDelegate, new TerminalOptions { Cols = 80, Rows = 24 });

        //Act
        terminal.Feed (Columns132);

        //Assert
        terminal.Cols.Should ().Be (80);
        terminalDelegate.SizeChangedCount.Should ().Be (0);
    }

    [Fact]
    public void Deccolm_raises_SizeChanged_once_with_the_new_size ()
    {
        //Arrange
        var terminalDelegate = new CountingDelegate ();
        var terminal = new Terminal (terminalDelegate, new TerminalOptions { Cols = 80, Rows = 24 });

        //Act
        terminal.Feed (Allow80To132 + Columns132);

        //Assert
        terminalDelegate.SizeChangedCount.Should ().Be (1);
        terminalDelegate.LastCols.Should ().Be (132);

        //Act
        terminal.Feed (Columns80);

        //Assert
        terminalDelegate.SizeChangedCount.Should ().Be (2);
        terminalDelegate.LastCols.Should ().Be (80);
    }

    [Fact]
    public void Resize_does_not_raise_SizeChanged ()
    {
        //Arrange
        var terminalDelegate = new CountingDelegate ();
        var terminal = new Terminal (terminalDelegate, new TerminalOptions { Cols = 80, Rows = 24 });

        //Act
        terminal.Resize (100, 30);

        //Assert
        terminal.Cols.Should ().Be (100);
        terminal.Rows.Should ().Be (30);
        terminalDelegate.SizeChangedCount.Should ().Be (0);
    }

    [Theory]
    [InlineData (Columns132, 132)]
    [InlineData (Columns80, 80)]
    public void Deccolm_leaves_Allow80To132_on_so_a_second_switch_is_honoured (string first, int firstCols)
    {
        //Arrange
        var terminal = new Terminal (null, new TerminalOptions { Cols = 100, Rows = 24 });
        var second = first == Columns132 ? Columns80 : Columns132;

        //Act
        terminal.Feed (Allow80To132 + first);
        terminal.Feed (second);

        //Assert
        terminal.Allow80To132.Should ().BeTrue ();
        terminal.Cols.Should ().Be (firstCols == 132 ? 80 : 132);
    }

    [Theory]
    [InlineData (Columns132)]
    [InlineData (Columns80)]
    public void Deccolm_is_not_a_full_reset (string switchSequence)
    {
        //Arrange
        var terminal = new Terminal (null, new TerminalOptions { Cols = 100, Rows = 24 });
        // application cursor keys, bracketed paste, any-event mouse tracking with SGR encoding, bold red
        terminal.Feed (Allow80To132 + "\u001b[?1h\u001b[?2004h\u001b[?1003h\u001b[?1006h\u001b[1;31m");
        var attributeBefore = terminal.CurAttr;
        var mouseModeBefore = terminal.MouseMode;
        var mouseProtocolBefore = terminal.MouseProtocol;

        //Act
        terminal.Feed (switchSequence);

        //Assert
        terminal.ApplicationCursor.Should ().BeTrue ();
        terminal.BracketedPasteMode.Should ().BeTrue ();
        terminal.MouseMode.Should ().Be (mouseModeBefore);
        terminal.MouseProtocol.Should ().Be (mouseProtocolBefore);
        terminal.CurAttr.Should ().Be (attributeBefore);
    }

    [Theory]
    [InlineData (Columns132)]
    [InlineData (Columns80)]
    public void Deccolm_erases_the_screen_homes_the_cursor_and_resets_the_margins (string switchSequence)
    {
        //Arrange
        var terminal = new Terminal (null, new TerminalOptions { Cols = 100, Rows = 24 });
        terminal.Feed (Allow80To132);
        terminal.Feed ("first row\r\nsecond row");
        // top/bottom margins 5..10, left/right margin mode on with margins 3..20, cursor somewhere else
        terminal.Feed ("\u001b[5;10r\u001b[?69h\u001b[3;20s\u001b[7;9H");

        //Act
        terminal.Feed (switchSequence);

        //Assert
        RowText (terminal, 0).Should ().Be ("");
        RowText (terminal, 1).Should ().Be ("");
        terminal.Buffer.X.Should ().Be (0);
        terminal.Buffer.Y.Should ().Be (0);
        terminal.Buffer.ScrollTop.Should ().Be (0);
        terminal.Buffer.ScrollBottom.Should ().Be (terminal.Rows - 1);
        terminal.MarginMode.Should ().BeFalse ();
        terminal.Buffer.MarginLeft.Should ().Be (0);
        terminal.Buffer.MarginRight.Should ().Be (terminal.Cols - 1);
    }

    [Fact]
    public void Deccolm_keeps_the_scrollback ()
    {
        //Arrange
        var terminal = new Terminal (null, new TerminalOptions { Cols = 80, Rows = 5 });
        terminal.Feed (Allow80To132);
        for (int i = 0; i < 20; i++)
            terminal.Feed ($"line {i}\r\n");
        var linesBefore = terminal.Buffer.Lines.Length;

        //Act
        terminal.Feed (Columns132);

        //Assert
        terminal.Buffer.Lines.Length.Should ().Be (linesBefore);
        terminal.Buffer.Lines [0].TranslateToString (true).ToString ().Should ().Be ("line 0");
    }

    [Theory]
    [InlineData (Columns132)]
    [InlineData (Columns80)]
    public void Deccolm_raises_SizeChanged_after_the_screen_is_erased_and_the_cursor_homed (string switchSequence)
    {
        //Arrange
        var terminalDelegate = new CountingDelegate ();
        var terminal = new Terminal (terminalDelegate, new TerminalOptions { Cols = 100, Rows = 24 });
        terminal.Feed (Allow80To132 + "some text\u001b[7;9H");

        //Act
        terminal.Feed (switchSequence);

        //Assert
        terminalDelegate.SizeChangedCount.Should ().Be (1);
        terminalDelegate.LastFirstRow.Should ().Be ("");
        terminalDelegate.LastCursorX.Should ().Be (0);
        terminalDelegate.LastCursorY.Should ().Be (0);
    }
}
