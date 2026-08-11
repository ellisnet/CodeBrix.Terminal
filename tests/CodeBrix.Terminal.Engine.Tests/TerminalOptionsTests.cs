using SilverAssertions;
using Xunit;

namespace CodeBrix.Terminal.Engine.Tests;

public class TerminalOptionsTests
{
    [Fact]
    public void Scrollback_And_TabStopWidth_AreSettable ()
    {
        var options = new TerminalOptions {
            Scrollback = 100,
            TabStopWidth = 4,
        };

        options.Scrollback.Should ().Be (100);
        options.TabStopWidth.Should ().Be (4);
    }

    [Fact]
    public void Scrollback_And_TabStopWidth_KeepTheirDefaults ()
    {
        var options = new TerminalOptions ();

        options.Scrollback.Should ().Be (1000);
        options.TabStopWidth.Should ().Be (8);
    }

    [Fact]
    public void Scrollback_SetBeforeConstruction_SizesTheBuffer ()
    {
        const int rows = 10;
        const int scrollback = 20;

        var terminal = new Terminal (null, new TerminalOptions {
            Cols = 80,
            Rows = rows,
            Scrollback = scrollback,
        });

        for (var i = 0; i < 100; i++) {
            terminal.Feed ($"line {i}\n");
        }

        terminal.Buffer.Lines.MaxLength.Should ().Be (rows + scrollback);
        terminal.Buffer.YBase.Should ().BeLessThanOrEqualTo (scrollback);
    }

    [Fact]
    public void TabStopWidth_SetBeforeConstruction_LaysOutTabStops ()
    {
        var terminal = new Terminal (null, new TerminalOptions {
            Cols = 80,
            Rows = 10,
            TabStopWidth = 4,
        });

        terminal.Feed ("\t");
        terminal.Buffer.X.Should ().Be (4);

        terminal.Feed ("\t");
        terminal.Buffer.X.Should ().Be (8);
    }
}
