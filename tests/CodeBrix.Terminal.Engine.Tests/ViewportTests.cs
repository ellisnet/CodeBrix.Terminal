using SilverAssertions;
using Xunit;

namespace CodeBrix.Terminal.Engine.Tests;

public class ViewportTests
{
    static Terminal CreateTerminalWithScrollback ()
    {
        var terminal = new Terminal (null, new TerminalOptions { Cols = 80, Rows = 5 });

        for (var i = 0; i < 20; i++) {
            terminal.Feed ($"line {i}\n");
        }

        terminal.Buffer.YBase.Should ().BeGreaterThan (3);

        return terminal;
    }

    [Fact]
    public void IsAtBottom_TracksTheViewport ()
    {
        var terminal = CreateTerminalWithScrollback ();

        terminal.IsAtBottom.Should ().BeTrue ();

        terminal.ScrollLines (-3);

        terminal.IsAtBottom.Should ().BeFalse ();

        terminal.ScrollToBottom ();

        terminal.IsAtBottom.Should ().BeTrue ();
        terminal.Buffer.YDisp.Should ().Be (terminal.Buffer.YBase);
    }

    [Fact]
    public void Scrolled_IsRaised_WhenTheViewportMoves ()
    {
        var terminal = CreateTerminalWithScrollback ();

        var raised = 0;
        var lastYDisp = -1;

        terminal.Scrolled += (t, yDisp) => {
            raised++;
            lastYDisp = yDisp;
        };

        terminal.ScrollLines (-2);

        raised.Should ().Be (1);
        lastYDisp.Should ().Be (terminal.Buffer.YBase - 2);

        terminal.ScrollToBottom ();

        raised.Should ().Be (2);
        lastYDisp.Should ().Be (terminal.Buffer.YBase);
    }

    [Fact]
    public void ScrollToBottom_AtTheBottom_IsANoOp ()
    {
        var terminal = CreateTerminalWithScrollback ();

        var raised = 0;
        terminal.Scrolled += (t, yDisp) => raised++;

        terminal.ScrollToBottom ();

        raised.Should ().Be (0);
        terminal.IsAtBottom.Should ().BeTrue ();
    }

    [Fact]
    public void ScrollLines_ClampsToTheScrollbackRange ()
    {
        var terminal = CreateTerminalWithScrollback ();

        terminal.ScrollLines (-1000);
        terminal.Buffer.YDisp.Should ().Be (0);

        terminal.ScrollLines (1000);
        terminal.Buffer.YDisp.Should ().Be (terminal.Buffer.YBase);
    }
}
