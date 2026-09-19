using SilverAssertions;
using Xunit;

namespace CodeBrix.Terminal.Engine.Tests;

/// <summary>
/// SearchService and the snapshot it hands out. The snapshot text holds each
/// character once, while the result coordinates are buffer columns -- which differ
/// as soon as a row holds a fullwidth character.
/// </summary>
public class SearchServiceTests
{
    const string Ideograph = "漢";

    static Terminal CreateTerminal ()
    {
        var terminal = new Terminal (null, new TerminalOptions { Cols = 20, Rows = 5 });
        // cells: a=0 b=1 ideograph=2 and 3 c=4 d=5 e=6 f=7
        terminal.Feed ("ab" + Ideograph + "cdef\r\n");
        terminal.Feed ("plain text here\r\n");

        return terminal;
    }

    [Fact]
    public void GetSnapshot_holds_a_fullwidth_character_once ()
    {
        //Arrange
        var terminal = CreateTerminal ();
        var search = new SearchService (terminal);

        //Act
        var snapshot = search.GetSnapshot ();

        //Assert
        snapshot.Text.Should ().StartWith ("ab" + Ideograph + "cdef");
    }

    [Fact]
    public void FindText_returns_columns_not_character_offsets ()
    {
        //Arrange
        var terminal = CreateTerminal ();
        var snapshot = new SearchService (terminal).GetSnapshot ();

        //Act -- "cd" is the fourth and fifth character of the row, but sits in
        //columns 4 and 5 because the ideograph before it takes two cells
        var matches = snapshot.FindText ("cd");

        //Assert
        matches.Should ().Be (1);
        var result = snapshot.FindNext ();
        result.Start.X.Should ().Be (4);
        result.Start.Y.Should ().Be (0);
        result.End.X.Should ().Be (6);
    }

    [Fact]
    public void FindText_on_a_narrow_row_is_unchanged ()
    {
        //Arrange
        var terminal = CreateTerminal ();
        var snapshot = new SearchService (terminal).GetSnapshot ();

        //Act
        var matches = snapshot.FindText ("text");

        //Assert
        matches.Should ().Be (1);
        var result = snapshot.FindNext ();
        result.Start.X.Should ().Be (6);
        result.Start.Y.Should ().Be (1);
        result.End.X.Should ().Be (10);
    }

    [Fact]
    public void FindText_finds_a_fullwidth_character_itself ()
    {
        //Arrange
        var terminal = CreateTerminal ();
        var snapshot = new SearchService (terminal).GetSnapshot ();

        //Act
        var matches = snapshot.FindText (Ideograph);

        //Assert
        matches.Should ().Be (1);
        var result = snapshot.FindNext ();
        result.Start.X.Should ().Be (2);
        result.End.X.Should ().Be (4);
    }
}
