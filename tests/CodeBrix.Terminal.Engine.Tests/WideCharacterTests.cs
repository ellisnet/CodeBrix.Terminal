using SilverAssertions;
using System.Text;
using Xunit;

using Rune = System.Rune;

namespace CodeBrix.Terminal.Engine.Tests;

/// <summary>
/// Character widths in the buffer. The engine measures every printed character with
/// Rune.ColumnWidth: a fullwidth character takes two cells (the second one a
/// placeholder of width 0), a combining mark takes none, and everything else takes
/// one. These tests cover the print path, wrapping, margins, insert mode and the
/// erase operations over the two cells of a fullwidth character.
/// </summary>
public class WideCharacterTests
{
    const string Ideograph = "漢";          // CJK, two cells
    const string HangulSyllable = "가";     // two cells
    const string FullwidthA = "Ａ";         // two cells
    const string Emoji = "\U0001f600";          // astral plane, four UTF-8 bytes, two cells
    const string BoxDrawing = "─";         // one cell
    const string AccentedLetter = "é";     // one cell
    const string CombiningAcute = "́";     // no cell of its own

    static Terminal CreateTerminal (int cols = 20, int rows = 5)
        => new Terminal (null, new TerminalOptions { Cols = cols, Rows = rows });

    static BufferLine Row (Terminal terminal, int row = 0)
        => terminal.Buffer.Lines [terminal.Buffer.YBase + row];

    static string RowText (Terminal terminal, int row = 0)
        => Row (terminal, row).TranslateToString (true).ToString ();

    #region Widths of single characters

    [Theory]
    [InlineData (Ideograph, 0x6f22)]
    [InlineData (HangulSyllable, 0xac00)]
    [InlineData (FullwidthA, 0xff21)]
    [InlineData (Emoji, 0x1f600)]
    public void Print_fullwidth_character_takes_two_cells (string text, int codePoint)
    {
        //Arrange
        var terminal = CreateTerminal ();

        //Act
        terminal.Feed (text);

        //Assert -- the character is in the first cell, a placeholder in the second
        var line = Row (terminal);
        line [0].Code.Should ().Be (codePoint);
        line [0].Rune.Should ().Be ((Rune)(uint)codePoint);
        line [0].Width.Should ().Be (2);
        line [1].Width.Should ().Be (0);
        line [1].Code.Should ().Be (0);
        terminal.Buffer.X.Should ().Be (2);
    }

    [Theory]
    [InlineData ("A", 0x41)]
    [InlineData (AccentedLetter, 0xe9)]
    [InlineData (BoxDrawing, 0x2500)]
    public void Print_narrow_character_takes_one_cell (string text, int codePoint)
    {
        //Arrange
        var terminal = CreateTerminal ();

        //Act
        terminal.Feed (text);

        //Assert
        var line = Row (terminal);
        line [0].Code.Should ().Be (codePoint);
        line [0].Width.Should ().Be (1);
        terminal.Buffer.X.Should ().Be (1);
    }

    [Fact]
    public void Print_unprintable_character_takes_one_cell ()
    {
        //Arrange -- a stray UTF-8 continuation byte, which Rune.ColumnWidth reports as
        //not printable
        var terminal = CreateTerminal ();

        //Act
        terminal.Feed (new byte [] { 0x80 }, 1);

        //Assert
        Row (terminal) [0].Width.Should ().Be (1);
        terminal.Buffer.X.Should ().Be (1);
    }

    [Fact]
    public void Print_mixed_widths_advances_the_cursor_by_the_widths ()
    {
        //Arrange
        var terminal = CreateTerminal ();

        //Act
        terminal.Feed ("a" + Ideograph + "b");

        //Assert
        var line = Row (terminal);
        line [0].Code.Should ().Be ((int)'a');
        line [1].Code.Should ().Be (0x6f22);
        line [2].Width.Should ().Be (0);
        line [3].Code.Should ().Be ((int)'b');
        terminal.Buffer.X.Should ().Be (4);
    }

    [Fact]
    public void Print_combining_mark_takes_no_cell_of_its_own ()
    {
        //Arrange
        var terminal = CreateTerminal ();

        //Act
        terminal.Feed ("e" + CombiningAcute);

        //Assert -- the pair became one precomposed letter in one cell; CombiningMarkTests
        //covers what happens to every other kind of mark
        terminal.Buffer.X.Should ().Be (1);
        Row (terminal) [0].Code.Should ().Be (0x00e9);
        Row (terminal) [0].Width.Should ().Be (1);
        Row (terminal) [1].Code.Should ().Be (0);
    }

    #endregion

    #region Multi-byte characters split across feeds

    [Theory]
    [InlineData (1)]
    [InlineData (2)]
    public void Feed_ideograph_split_between_two_calls_prints_one_character (int split)
        => AssertSplitFeed (Ideograph, split, 0x6f22);

    [Theory]
    [InlineData (1)]
    [InlineData (2)]
    [InlineData (3)]
    public void Feed_emoji_split_between_two_calls_prints_one_character (int split)
        => AssertSplitFeed (Emoji, split, 0x1f600);

    static void AssertSplitFeed (string text, int split, int codePoint)
    {
        //Arrange
        var terminal = CreateTerminal ();
        var bytes = Encoding.UTF8.GetBytes (text);
        var first = new byte [split];
        var second = new byte [bytes.Length - split];
        System.Array.Copy (bytes, first, split);
        System.Array.Copy (bytes, split, second, 0, second.Length);

        //Act
        terminal.Feed (first, first.Length);
        terminal.Feed (second, second.Length);

        //Assert
        var line = terminal.Buffer.Lines [terminal.Buffer.YBase];
        line [0].Code.Should ().Be (codePoint);
        line [0].Width.Should ().Be (2);
        line [1].Width.Should ().Be (0);
        terminal.Buffer.X.Should ().Be (2);
    }

    #endregion

    #region The last column

    [Fact]
    public void Print_fullwidth_character_with_one_column_left_wraps_whole ()
    {
        //Arrange -- six columns, five of them filled
        var terminal = CreateTerminal (6, 4);

        //Act
        terminal.Feed ("abcde" + Ideograph);

        //Assert -- the character moved to the next row in one piece
        Row (terminal, 0) [5].Code.Should ().Be (0);
        Row (terminal, 0) [5].Width.Should ().Be (1);
        Row (terminal, 0).GetTrimmedLength ().Should ().Be (5);
        Row (terminal, 1).IsWrapped.Should ().BeTrue ();
        Row (terminal, 1) [0].Code.Should ().Be (0x6f22);
        Row (terminal, 1) [1].Width.Should ().Be (0);
        terminal.Buffer.X.Should ().Be (2);
        terminal.Buffer.Y.Should ().Be (1);
    }

    [Fact]
    public void Print_fullwidth_character_with_one_column_left_and_no_autowrap_is_dropped ()
    {
        //Arrange -- DECAWM off
        var terminal = CreateTerminal (6, 4);
        terminal.Feed ("\x1b[?7l");

        //Act
        terminal.Feed ("abcde" + Ideograph + "Z");

        //Assert -- the character did not fit and was discarded without moving the
        //cursor; the narrow character after it took the last cell
        terminal.Buffer.Y.Should ().Be (0);
        Row (terminal, 0) [5].Code.Should ().Be ((int)'Z');
        Row (terminal, 1).HasAnyContent ().Should ().BeFalse ();
    }

    [Fact]
    public void Print_fullwidth_character_at_the_right_margin_wraps_to_the_left_margin ()
    {
        //Arrange -- DECLRMM with margins at columns 3 to 8 (1 based)
        var terminal = CreateTerminal (20, 4);
        terminal.Feed ("\x1b[?69h\x1b[3;8s");

        //Act -- print the character while sitting on the right margin
        terminal.Feed ("\x1b[1;8H" + Ideograph);

        //Assert
        Row (terminal, 0) [7].Code.Should ().Be (0);
        Row (terminal, 1).IsWrapped.Should ().BeTrue ();
        Row (terminal, 1) [2].Code.Should ().Be (0x6f22);
        Row (terminal, 1) [3].Width.Should ().Be (0);
        terminal.Buffer.X.Should ().Be (4);
        terminal.Buffer.Y.Should ().Be (1);
    }

    [Fact]
    public void Print_fullwidth_characters_fill_a_two_column_terminal ()
    {
        //Arrange -- the narrowest terminal there is
        var terminal = CreateTerminal (2, 3);

        //Act
        terminal.Feed (Ideograph + Ideograph);

        //Assert
        Row (terminal, 0) [0].Width.Should ().Be (2);
        Row (terminal, 1) [0].Width.Should ().Be (2);
        Row (terminal, 1).IsWrapped.Should ().BeTrue ();
    }

    #endregion

    #region Insert mode

    [Fact]
    public void Insert_mode_blanks_a_fullwidth_character_pushed_to_the_last_column ()
    {
        //Arrange -- the character sits in the last two cells
        var terminal = CreateTerminal (6, 4);
        terminal.Feed ("abcd" + Ideograph);
        Row (terminal) [4].Width.Should ().Be (2);

        //Act -- insert one cell at the start of the row (IRM)
        terminal.Feed ("\x1b[1;1H\x1b[4hZ");

        //Assert -- its placeholder went over the edge, so the character went too
        var line = Row (terminal);
        line [0].Code.Should ().Be ((int)'Z');
        line [5].Width.Should ().Be (1);
        line [5].Code.Should ().Be (0);
    }

    [Fact]
    public void Insert_mode_keeps_a_fullwidth_character_that_still_fits ()
    {
        //Arrange
        var terminal = CreateTerminal (6, 4);
        terminal.Feed ("ab" + Ideograph + "cd");

        //Act
        terminal.Feed ("\x1b[1;1H\x1b[4hZ");

        //Assert -- the character shifted one cell right and kept its placeholder
        var line = Row (terminal);
        line [3].Code.Should ().Be (0x6f22);
        line [3].Width.Should ().Be (2);
        line [4].Width.Should ().Be (0);
    }

    #endregion

    #region Erasing and overwriting half a fullwidth character

    [Fact]
    public void Ech_over_the_placeholder_also_erases_the_character ()
    {
        //Arrange
        var terminal = CreateTerminal (8, 4);
        terminal.Feed ("a" + Ideograph + "b");

        //Act -- erase the cell holding the placeholder
        terminal.Feed ("\x1b[1;3H\x1b[1X");

        //Assert
        AssertNoHalfCharacters (Row (terminal));
        Row (terminal) [1].Code.Should ().Be (0);
        Row (terminal) [1].Width.Should ().Be (1);
        Row (terminal) [3].Code.Should ().Be ((int)'b');
    }

    [Fact]
    public void Ech_over_the_character_also_erases_the_placeholder ()
    {
        //Arrange
        var terminal = CreateTerminal (8, 4);
        terminal.Feed ("a" + Ideograph + "b");

        //Act
        terminal.Feed ("\x1b[1;2H\x1b[1X");

        //Assert
        AssertNoHalfCharacters (Row (terminal));
        Row (terminal) [2].Width.Should ().Be (1);
        Row (terminal) [2].Code.Should ().Be (0);
    }

    [Fact]
    public void El_starting_on_the_placeholder_also_erases_the_character ()
    {
        //Arrange
        var terminal = CreateTerminal (8, 4);
        terminal.Feed ("a" + Ideograph + "b");

        //Act -- erase from the placeholder to the end of the line
        terminal.Feed ("\x1b[1;3H\x1b[0K");

        //Assert
        AssertNoHalfCharacters (Row (terminal));
        RowText (terminal).Should ().Be ("a");
    }

    [Fact]
    public void Dch_over_the_character_leaves_no_half_character ()
    {
        //Arrange
        var terminal = CreateTerminal (8, 4);
        terminal.Feed ("a" + Ideograph + "bc");

        //Act
        terminal.Feed ("\x1b[1;2H\x1b[1P");

        //Assert
        AssertNoHalfCharacters (Row (terminal));
        Row (terminal) [2].Code.Should ().Be ((int)'b');
    }

    [Fact]
    public void Dch_over_the_placeholder_leaves_no_half_character ()
    {
        //Arrange
        var terminal = CreateTerminal (8, 4);
        terminal.Feed ("a" + Ideograph + "bc");

        //Act
        terminal.Feed ("\x1b[1;3H\x1b[1P");

        //Assert
        AssertNoHalfCharacters (Row (terminal));
        Row (terminal) [1].Code.Should ().Be (0);
    }

    [Fact]
    public void Ich_at_the_placeholder_leaves_no_half_character ()
    {
        //Arrange
        var terminal = CreateTerminal (8, 4);
        terminal.Feed ("a" + Ideograph + "b");

        //Act
        terminal.Feed ("\x1b[1;3H\x1b[1@");

        //Assert
        AssertNoHalfCharacters (Row (terminal));
        Row (terminal) [1].Code.Should ().Be (0);
    }

    [Fact]
    public void Print_after_a_backspace_onto_the_placeholder_erases_the_character ()
    {
        //Arrange -- backspace puts the cursor on the placeholder cell
        var terminal = CreateTerminal (8, 4);
        terminal.Feed ("a" + Ideograph);
        terminal.Feed ("\b");
        terminal.Buffer.X.Should ().Be (2);

        //Act
        terminal.Feed ("Z");

        //Assert -- the character lost its other half, so it was blanked
        AssertNoHalfCharacters (Row (terminal));
        Row (terminal) [1].Code.Should ().Be (0);
        Row (terminal) [2].Code.Should ().Be ((int)'Z');
    }

    [Fact]
    public void Print_over_the_first_cell_of_a_fullwidth_character_clears_the_placeholder ()
    {
        //Arrange
        var terminal = CreateTerminal (8, 4);
        terminal.Feed ("a" + Ideograph + "b");

        //Act -- write a narrow character where the fullwidth character starts
        terminal.Feed ("\x1b[1;2HZ");

        //Assert
        AssertNoHalfCharacters (Row (terminal));
        Row (terminal) [1].Code.Should ().Be ((int)'Z');
        Row (terminal) [2].Width.Should ().Be (1);
        Row (terminal) [3].Code.Should ().Be ((int)'b');
    }

    [Fact]
    public void Rep_repeats_a_fullwidth_character_with_its_placeholder ()
    {
        //Arrange
        var terminal = CreateTerminal (10, 3);

        //Act -- REP twice after a fullwidth character
        terminal.Feed (Ideograph + "\x1b[2b");

        //Assert
        var line = Row (terminal);
        AssertNoHalfCharacters (line);
        line [2].Code.Should ().Be (0x6f22);
        line [3].Width.Should ().Be (0);
        line [4].Code.Should ().Be (0x6f22);
        line [5].Width.Should ().Be (0);
    }

    [Fact]
    public void Rep_repeats_a_narrow_character ()
    {
        //Arrange
        var terminal = CreateTerminal (10, 3);

        //Act
        terminal.Feed ("x\x1b[3b");

        //Assert
        RowText (terminal).Should ().Be ("xxxx");
    }

    #endregion

    #region Reading the text back

    [Fact]
    public void TranslateToString_returns_each_character_once ()
    {
        //Arrange
        var terminal = CreateTerminal ();

        //Act
        terminal.Feed ("ab" + Ideograph + "cd");

        //Assert -- the placeholder contributes no character of its own
        RowText (terminal).Should ().Be ("ab" + Ideograph + "cd");
    }

    [Fact]
    public void TranslateBufferLineToString_returns_each_character_once ()
    {
        //Arrange
        var terminal = CreateTerminal ();

        //Act
        terminal.Feed (Ideograph + Emoji + "z");

        //Assert
        terminal.Buffer.TranslateBufferLineToString (terminal.Buffer.YBase, true)
            .ToString ().Should ().Be (Ideograph + Emoji + "z");
    }

    [Fact]
    public void GetTrimmedLength_counts_a_fullwidth_character_as_two_columns ()
    {
        //Arrange
        var terminal = CreateTerminal ();

        //Act
        terminal.Feed ("a" + Ideograph);

        //Assert
        Row (terminal).GetTrimmedLength ().Should ().Be (3);
    }

    #endregion

    /// <summary>
    /// Asserts the invariant a row must always satisfy: a width 2 cell is always
    /// followed by its width 0 placeholder, and a width 0 cell always follows the
    /// character that owns it.
    /// </summary>
    static void AssertNoHalfCharacters (BufferLine line)
    {
        for (var col = 0; col < line.Length; col++) {
            if (line [col].Width == 2) {
                (col + 1 < line.Length).Should ()
                    .BeTrue ($"because a fullwidth character at column {col} needs a cell for its placeholder");
                line [col + 1].Width.Should ()
                    .Be (0, $"because the fullwidth character at column {col} needs its placeholder");
            }

            if (line [col].Width == 0) {
                (col > 0).Should ().BeTrue ($"because the placeholder at column {col} needs an owner");
                line [col - 1].Width.Should ()
                    .Be (2, $"because the placeholder at column {col} needs a fullwidth character before it");
            }
        }
    }
}
