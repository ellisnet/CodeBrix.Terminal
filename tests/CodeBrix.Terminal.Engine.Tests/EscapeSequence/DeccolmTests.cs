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

        public override void SizeChanged (Terminal source)
        {
            SizeChangedCount++;
            LastCols = source.Cols;
        }
    }

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
        Action to80 = () => terminal.Feed (Allow80To132 + Columns80);

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
        terminal.Feed (Allow80To132 + Columns80);

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
}
