using SilverAssertions;
using Xunit;

namespace CodeBrix.Terminal.Engine.Tests;

public class SelectionServiceTests
{
    static Terminal CreateScrolledBackTerminal (out int yDisp)
    {
        var terminal = new Terminal (null, new TerminalOptions { Cols = 80, Rows = 10 });

        // '#' is "other punctuation" for SelectWordOrExpression: not a word
        // character, not whitespace, not a bracket/brace/parenthesis.
        for (var i = 0; i < 40; i++) {
            terminal.Feed ($"# line {i}\n");
        }

        terminal.Buffer.YBase.Should ().BeGreaterThan (4);

        terminal.ScrollLines (-3);

        yDisp = terminal.Buffer.YDisp;
        yDisp.Should ().Be (terminal.Buffer.YBase - 3);

        return terminal;
    }

    [Fact]
    public void SelectWordOrExpression_OnPunctuation_WhileScrolledBack_SelectsTheVisibleRow ()
    {
        // Regression for the YDisp double-add: the "other characters" fallback
        // added Buffer.YDisp to a row that was already buffer-absolute, so
        // double-clicking punctuation while scrolled back selected the wrong
        // row (only YDisp == 0 hid the bug).
        var terminal = CreateScrolledBackTerminal (out var yDisp);
        var selection = new SelectionService (terminal);

        const int screenRow = 2;

        selection.SelectWordOrExpression (col: 0, row: screenRow);

        selection.Start.Y.Should ().Be (yDisp + screenRow);
        selection.End.Y.Should ().Be (yDisp + screenRow);
        selection.Start.X.Should ().Be (0);
    }

    [Fact]
    public void SelectWordOrExpression_OnWord_WhileScrolledBack_SelectsTheVisibleRow ()
    {
        // The word branch was always correct; pin it so the coordinate model
        // stays consistent across branches.
        var terminal = CreateScrolledBackTerminal (out var yDisp);
        var selection = new SelectionService (terminal);

        const int screenRow = 2;

        // Column 2 is the 'l' of "line" -> selects the word.
        selection.SelectWordOrExpression (col: 2, row: screenRow);

        selection.Start.Y.Should ().Be (yDisp + screenRow);
        selection.End.Y.Should ().Be (yDisp + screenRow);
    }
}
