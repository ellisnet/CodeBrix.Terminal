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

    #region Fullwidth characters

    const string Ideograph = "漢";

    static Terminal CreateWideCharTerminal ()
    {
        var terminal = new Terminal (null, new TerminalOptions { Cols = 20, Rows = 5 });
        // "ab" then an ideograph in columns 2 and 3, then "cd"
        terminal.Feed ("ab" + Ideograph + "cd");

        return terminal;
    }

    [Fact]
    public void GetSelectedText_returns_a_fullwidth_character_once ()
    {
        //Arrange
        var terminal = CreateWideCharTerminal ();
        var selection = new SelectionService (terminal);

        //Act -- the selection covers all six columns of the row
        selection.StartSelection (row: 0, col: 0);
        selection.DragExtend (row: 0, col: 6);

        //Assert
        selection.GetSelectedText ().Should ().Be ("ab" + Ideograph + "cd");
    }

    [Fact]
    public void GetSelectedText_counts_a_fullwidth_character_as_two_columns ()
    {
        //Arrange
        var terminal = CreateWideCharTerminal ();
        var selection = new SelectionService (terminal);

        //Act -- stop at the column after the ideograph's placeholder
        selection.StartSelection (row: 0, col: 0);
        selection.DragExtend (row: 0, col: 4);

        //Assert
        selection.GetSelectedText ().Should ().Be ("ab" + Ideograph);
    }

    [Fact]
    public void SelectWordOrExpression_on_a_fullwidth_character_selects_the_whole_word ()
    {
        //Arrange
        var terminal = CreateWideCharTerminal ();
        var selection = new SelectionService (terminal);

        //Act -- click the ideograph itself
        selection.SelectWordOrExpression (col: 2, row: 0);

        //Assert -- the placeholder does not end the word, and the end column counts
        //both cells of the ideograph
        selection.Start.X.Should ().Be (0);
        selection.End.X.Should ().Be (6);
        selection.GetSelectedText ().Should ().Be ("ab" + Ideograph + "cd");
    }

    [Fact]
    public void SelectWordOrExpression_on_a_placeholder_selects_the_same_word ()
    {
        //Arrange
        var terminal = CreateWideCharTerminal ();
        var selection = new SelectionService (terminal);

        //Act -- click the second cell of the ideograph
        selection.SelectWordOrExpression (col: 3, row: 0);

        //Assert
        selection.Start.X.Should ().Be (0);
        selection.End.X.Should ().Be (6);
        selection.GetSelectedText ().Should ().Be ("ab" + Ideograph + "cd");
    }

    #endregion
}
