using SilverAssertions;
using System;
using Xunit;

using Rune = System.Rune;

namespace CodeBrix.Terminal.Engine.Tests;

/// <summary>
/// RuneHelper.ConsoleWidth, the older width table carried over from the upstream
/// project. It is NOT what the engine measures printed characters with -- that is
/// Rune.ColumnWidth -- and the two disagree about the emoji ranges, which these
/// tests pin so the difference cannot go unnoticed.
/// </summary>
public class RuneHelperTests
{
    [Theory]
    [InlineData ('A', 1)]
    [InlineData (0x00e9, 1)]        // Latin small e with acute
    [InlineData (0x2500, 1)]        // box drawing
    [InlineData (0x0301, 0)]        // combining acute
    [InlineData (0x6f22, 2)]        // ideograph
    [InlineData (0xac00, 2)]        // Hangul syllable
    [InlineData (0xff21, 2)]        // fullwidth A
    public void ConsoleWidth_measures_the_character (uint rune, int expected)
        => rune.ConsoleWidth ().Should ().Be (expected);

    [Fact]
    public void ConsoleWidth_does_not_throw_above_the_latin_range ()
    {
        //Arrange -- the table search was handed the row COUNT instead of the last row
        //index, which read past the end of the table for every rune above 0xa0

        //Act
        Action act = () => {
            for (uint rune = 0xa1; rune < 0x3000; rune++) {
                rune.ConsoleWidth ();
            }

            ((uint)0x1f600).ConsoleWidth ();
            ((uint)0x10ffff).ConsoleWidth ();
        };

        //Assert
        act.Should ().NotThrow ();
    }

    [Fact]
    public void ConsoleWidth_disagrees_with_ColumnWidth_about_emoji ()
    {
        //Arrange -- the older table predates the emoji ranges

        //Act
        var helperWidth = ((uint)0x1f600).ConsoleWidth ();
        var engineWidth = Rune.ColumnWidth (new Rune (0x1f600));

        //Assert -- the engine puts emoji in two cells; this table would say one
        helperWidth.Should ().Be (1);
        engineWidth.Should ().Be (2);
    }

    [Fact]
    public void ColumnWidth_is_what_the_engine_writes_into_the_buffer ()
    {
        //Arrange
        var terminal = new Terminal (null, new TerminalOptions { Cols = 20, Rows = 4 });

        //Act
        terminal.Feed ("\U0001f600");

        //Assert
        terminal.Buffer.Lines [terminal.Buffer.YBase] [0].Width.Should ()
            .Be (Rune.ColumnWidth (new Rune (0x1f600)));
    }
}
