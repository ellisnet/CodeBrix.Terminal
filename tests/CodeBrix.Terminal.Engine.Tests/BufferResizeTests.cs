using SilverAssertions;
using System.Collections.Generic;
using Xunit;

namespace CodeBrix.Terminal.Engine.Tests;

/// <summary>
/// Terminal.Resize and the reflow it drives. Rows above the cursor are re-laid in
/// both directions and lose nothing; the wrapped-line group that HOLDS the cursor is
/// deliberately left alone and then cut to the new width, which is what xterm does
/// and what shells expect -- they repaint their own input line after a size change.
/// </summary>
public class BufferResizeTests
{
    const string Ideograph = "漢";

    static Terminal CreateTerminal (int cols, int rows, int scrollback = 1000)
        => new Terminal (null, new TerminalOptions { Cols = cols, Rows = rows, Scrollback = scrollback });

    static string Text (Terminal terminal, int absoluteRow)
        => terminal.Buffer.Lines [absoluteRow].TranslateToString (true).ToString ();

    static List<string> Rows (Terminal terminal, int count)
    {
        var rows = new List<string> ();
        for (var i = 0; i < count; i++) {
            rows.Add (Text (terminal, i));
        }

        return rows;
    }

    #region Reflow above the cursor

    [Fact]
    public void Resize_narrower_relays_a_wrapped_line_above_the_cursor ()
    {
        //Arrange -- 43 characters on a 40 column terminal, with the cursor two rows below
        var terminal = CreateTerminal (40, 10);
        terminal.Feed ("0123456789012345678901234567890123456789012\r\nsecond\r\n");

        //Act
        terminal.Resize (30, 10);

        //Assert -- the same 43 characters, re-laid at the new width
        Text (terminal, 0).Should ().Be ("012345678901234567890123456789");
        Text (terminal, 1).Should ().Be ("0123456789012");
        Text (terminal, 2).Should ().Be ("second");
        terminal.Buffer.Lines [0].IsWrapped.Should ().BeFalse ();
        terminal.Buffer.Lines [1].IsWrapped.Should ().BeTrue ();
        terminal.Buffer.Lines [2].IsWrapped.Should ().BeFalse ();
    }

    [Fact]
    public void Resize_wider_rejoins_a_wrapped_line_above_the_cursor ()
    {
        //Arrange -- 43 characters on a 30 column terminal: two rows plus the cursor's row
        var terminal = CreateTerminal (30, 10);
        terminal.Feed ("0123456789012345678901234567890123456789012\r\nsecond\r\n");

        //Act
        terminal.Resize (40, 10);

        //Assert
        Text (terminal, 0).Should ().Be ("0123456789012345678901234567890123456789");
        Text (terminal, 1).Should ().Be ("012");
        Text (terminal, 2).Should ().Be ("second");
        terminal.Buffer.Lines [1].IsWrapped.Should ().BeTrue ();
    }

    [Fact]
    public void Resize_narrow_then_wide_restores_the_original_rows ()
    {
        //Arrange
        var terminal = CreateTerminal (40, 10);
        terminal.Feed ("0123456789012345678901234567890123456789012\r\nsecond\r\nthird\r\n");
        var before = Rows (terminal, 6);

        //Act
        terminal.Resize (30, 10);
        terminal.Resize (40, 10);

        //Assert
        Rows (terminal, 6).Should ().BeEquivalentTo (before);
    }

    [Fact]
    public void Resize_narrow_then_wide_keeps_the_scrollback ()
    {
        //Arrange -- more content than the viewport, so there is scrollback to reflow
        var terminal = CreateTerminal (20, 4, 50);
        for (var i = 0; i < 10; i++) {
            terminal.Feed ($"row-{i}-abcdefghijkl\r\n");
        }

        var length = terminal.Buffer.Lines.Length;
        var before = Rows (terminal, length);

        //Act
        terminal.Resize (10, 4);
        terminal.Resize (20, 4);

        //Assert
        terminal.Buffer.Lines.Length.Should ().Be (length);
        Rows (terminal, length).Should ().BeEquivalentTo (before);
    }

    #endregion

    #region The cursor's own wrapped group

    [Fact]
    public void Resize_narrower_cuts_the_wrapped_group_holding_the_cursor ()
    {
        //Arrange -- the cursor's own line is 43 cells on a 40 column terminal
        var terminal = CreateTerminal (40, 10);
        terminal.Feed ("chat> a line being edited that changes size");
        Text (terminal, 0).Should ().Be ("chat> a line being edited that changes s");
        Text (terminal, 1).Should ().Be ("ize");

        //Act
        terminal.Resize (30, 10);

        //Assert -- the group the cursor sits in is not re-laid, it is cut: the middle of
        //the line is gone and the wrapped remainder stays where it was. An application
        //that writes its own prompt has to repaint that line after narrowing.
        Text (terminal, 0).Should ().Be ("chat> a line being edited that");
        Text (terminal, 1).Should ().Be ("ize");
        terminal.Buffer.Lines [1].IsWrapped.Should ().BeTrue ();
    }

    [Fact]
    public void Resize_narrower_then_wider_does_not_bring_the_cut_text_back ()
    {
        //Arrange
        var terminal = CreateTerminal (40, 10);
        terminal.Feed ("chat> a line being edited that changes size");

        //Act
        terminal.Resize (30, 10);
        terminal.Resize (40, 10);

        //Assert -- the cut removed the cells, widening cannot restore them
        Text (terminal, 0).Should ().Be ("chat> a line being edited that");
    }

    #endregion

    #region Rows, viewport and cursor

    [Fact]
    public void Resize_more_rows_keeps_the_cursor_on_the_same_buffer_line ()
    {
        //Arrange -- enough output for scrollback
        var terminal = CreateTerminal (20, 5, 100);
        for (var i = 0; i < 12; i++) {
            terminal.Feed ($"line {i}\r\n");
        }

        var absoluteCursor = terminal.Buffer.YBase + terminal.Buffer.Y;

        //Act
        terminal.Resize (20, 8);

        //Assert -- the viewport grew upwards into the scrollback
        (terminal.Buffer.YBase + terminal.Buffer.Y).Should ().Be (absoluteCursor);
        terminal.Buffer.YDisp.Should ().Be (terminal.Buffer.YBase);
        terminal.Buffer.Y.Should ().BeLessThan (terminal.Rows);
    }

    [Fact]
    public void Resize_fewer_rows_keeps_the_cursor_on_the_same_buffer_line ()
    {
        //Arrange
        var terminal = CreateTerminal (20, 8, 100);
        for (var i = 0; i < 12; i++) {
            terminal.Feed ($"line {i}\r\n");
        }

        var absoluteCursor = terminal.Buffer.YBase + terminal.Buffer.Y;

        //Act
        terminal.Resize (20, 3);

        //Assert
        (terminal.Buffer.YBase + terminal.Buffer.Y).Should ().Be (absoluteCursor);
        terminal.Buffer.Y.Should ().BeLessThan (terminal.Rows);
        terminal.Buffer.YDisp.Should ().Be (terminal.Buffer.YBase);
    }

    [Fact]
    public void Resize_keeps_the_cursor_inside_the_grid ()
    {
        //Arrange
        var terminal = CreateTerminal (40, 10);
        terminal.Feed ("\x1b[9;38H");    // row 9, column 38 (1 based)

        //Act
        terminal.Resize (10, 4);

        //Assert
        terminal.Buffer.X.Should ().BeLessThan (terminal.Cols);
        terminal.Buffer.Y.Should ().BeLessThan (terminal.Rows);
        terminal.Cols.Should ().Be (10);
        terminal.Rows.Should ().Be (4);
    }

    [Fact]
    public void Resize_to_the_minimum_and_back_keeps_the_text ()
    {
        //Arrange
        var terminal = CreateTerminal (20, 5);
        terminal.Feed ("hello world\r\n");

        //Act -- the terminal clamps to its minimum of 2 columns by 1 row
        terminal.Resize (2, 1);
        terminal.Resize (20, 5);

        //Assert
        Text (terminal, 0).Should ().Be ("hello world");
    }

    #endregion

    #region The alternate buffer and the margins

    [Fact]
    public void Resize_does_not_reflow_the_alternate_buffer ()
    {
        //Arrange -- the alternate buffer has no scrollback, so no reflow
        var terminal = CreateTerminal (40, 6);
        terminal.Feed ("\x1b[?1049h");
        terminal.Feed ("0123456789012345678901234567890123456789012\r\nsecond");
        terminal.Buffers.IsAlternateBuffer.Should ().BeTrue ();
        terminal.Buffer.HasScrollback.Should ().BeFalse ();
        var before = Rows (terminal, 3);

        //Act
        terminal.Resize (30, 6);

        //Assert -- the rows are left exactly as the application drew them; the
        //application is expected to repaint the alternate screen itself
        Rows (terminal, 3).Should ().BeEquivalentTo (before);
        terminal.Cols.Should ().Be (30);
    }

    [Fact]
    public void Resize_narrower_clamps_the_margins ()
    {
        //Arrange -- DECLRMM with margins at columns 5 to 30 (1 based)
        var terminal = CreateTerminal (40, 6);
        terminal.Feed ("\x1b[?69h\x1b[5;30s");
        terminal.Buffer.MarginLeft.Should ().Be (4);
        terminal.Buffer.MarginRight.Should ().Be (29);

        //Act
        terminal.Resize (20, 6);

        //Assert
        terminal.Buffer.MarginLeft.Should ().Be (4);
        terminal.Buffer.MarginRight.Should ().Be (19);

        //Act -- narrower than the left margin
        terminal.Resize (3, 6);

        //Assert
        terminal.Buffer.MarginRight.Should ().Be (2);
        terminal.Buffer.MarginLeft.Should ().BeLessThanOrEqualTo (terminal.Buffer.MarginRight);
    }

    #endregion

    #region Reflow with fullwidth characters

    [Fact]
    public void Resize_narrower_never_splits_a_fullwidth_character ()
    {
        //Arrange -- nine cells of content above the cursor: a b then three ideographs
        //and a narrow character
        var terminal = CreateTerminal (10, 5);
        terminal.Feed ("ab" + Ideograph + Ideograph + Ideograph + "x\r\ntail\r\n");

        //Act -- eight columns cannot hold the last character any more
        terminal.Resize (8, 5);

        //Assert
        Text (terminal, 0).Should ().Be ("ab" + Ideograph + Ideograph + Ideograph);
        Text (terminal, 1).Should ().Be ("x");
        terminal.Buffer.Lines [1].IsWrapped.Should ().BeTrue ();
        AssertNoHalfCharacters (terminal, 4);
    }

    [Fact]
    public void Resize_narrower_moves_a_whole_fullwidth_character_to_the_next_row ()
    {
        //Arrange -- five ideographs exactly fill ten columns
        var terminal = CreateTerminal (10, 5);
        terminal.Feed (Ideograph + Ideograph + Ideograph + Ideograph + Ideograph + "\r\ntail\r\n");

        //Act -- nine columns hold four of them and one column to spare
        terminal.Resize (9, 5);

        //Assert
        Text (terminal, 0).Should ().Be (Ideograph + Ideograph + Ideograph + Ideograph);
        Text (terminal, 1).Should ().Be (Ideograph);
        terminal.Buffer.Lines [1].IsWrapped.Should ().BeTrue ();
        AssertNoHalfCharacters (terminal, 4);
    }

    [Fact]
    public void Resize_wider_rejoins_fullwidth_characters ()
    {
        //Arrange
        var terminal = CreateTerminal (10, 5);
        terminal.Feed (Ideograph + Ideograph + Ideograph + Ideograph + Ideograph + "\r\ntail\r\n");

        //Act
        terminal.Resize (9, 5);
        terminal.Resize (10, 5);

        //Assert
        Text (terminal, 0).Should ().Be (Ideograph + Ideograph + Ideograph + Ideograph + Ideograph);
        Text (terminal, 1).Should ().Be ("tail");
        AssertNoHalfCharacters (terminal, 4);
    }

    [Fact]
    public void Resize_narrower_blanks_a_fullwidth_character_cut_by_the_cursor_line ()
    {
        //Arrange -- the cursor's own line ends with a fullwidth character
        var terminal = CreateTerminal (10, 5);
        terminal.Feed ("abcdefgh" + Ideograph);

        //Act -- the cut takes the placeholder with it
        terminal.Resize (9, 5);

        //Assert -- no half character is left behind in the last column
        Text (terminal, 0).Should ().Be ("abcdefgh");
        AssertNoHalfCharacters (terminal, 2);
    }

    #endregion

    static void AssertNoHalfCharacters (Terminal terminal, int rowCount)
    {
        for (var row = 0; row < rowCount && row < terminal.Buffer.Lines.Length; row++) {
            var line = terminal.Buffer.Lines [row];
            for (var col = 0; col < line.Length; col++) {
                if (line [col].Width == 2) {
                    (col + 1 < line.Length).Should ()
                        .BeTrue ($"because row {row} column {col} needs room for a placeholder");
                    line [col + 1].Width.Should ()
                        .Be (0, $"because row {row} column {col} needs its placeholder");
                }

                if (line [col].Width == 0) {
                    (col > 0).Should ().BeTrue ($"because row {row} column {col} is a placeholder with no owner");
                    line [col - 1].Width.Should ()
                        .Be (2, $"because row {row} column {col} is a placeholder with no owner");
                }
            }
        }
    }
}
