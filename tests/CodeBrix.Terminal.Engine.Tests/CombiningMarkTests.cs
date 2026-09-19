using SilverAssertions;
using Xunit;

namespace CodeBrix.Terminal.Engine.Tests;

/// <summary>
/// Combining marks, which Rune.ColumnWidth reports as taking no column. A cell holds
/// one code point, so the engine composes the mark with the character before it when
/// Unicode has a precomposed form, drops format characters that have no glyph of
/// their own, and otherwise gives the mark a cell. A base character is never lost.
/// </summary>
public class CombiningMarkTests
{
    const string CombiningAcute = "́";         // composes with a Latin letter
    const string CombiningDotBelow = "̣";
    const string CombiningCircumflex = "̂";
    const string HangulChoseongKiyeok = "ᄀ";   // two cells
    const string HangulJungseongA = "ᅡ";       // no cell: composes into the syllable
    const string HangulJongseongKiyeok = "ᆨ";
    const string ThaiKoKai = "ก";              // one cell
    const string ThaiSaraI = "ิ";              // no cell, and no precomposed form
    const string ArabicBeh = "ب";
    const string ArabicFatha = "َ";            // no cell, and no precomposed form
    const string Heart = "❤";
    const string VariationSelector16 = "️";
    const string ZeroWidthJoiner = "‍";
    const string ManEmoji = "\U0001f468";
    const string ComputerEmoji = "\U0001f4bb";

    static Terminal CreateTerminal (int cols = 20, int rows = 5)
        => new Terminal (null, new TerminalOptions { Cols = cols, Rows = rows });

    static BufferLine Row (Terminal terminal, int row = 0)
        => terminal.Buffer.Lines [terminal.Buffer.YBase + row];

    static string RowText (Terminal terminal, int row = 0)
        => Row (terminal, row).TranslateToString (true).ToString ();

    #region Composing

    [Fact]
    public void Print_combining_mark_composes_with_the_character_before_it ()
    {
        //Arrange
        var terminal = CreateTerminal ();

        //Act
        terminal.Feed ("e" + CombiningAcute);

        //Assert -- one cell holding the precomposed letter, and one column used
        var line = Row (terminal);
        line [0].Code.Should ().Be (0x00e9);
        line [0].Rune.Should ().Be ((System.Rune)0x00e9);
        line [0].Width.Should ().Be (1);
        line [1].Code.Should ().Be (0);
        terminal.Buffer.X.Should ().Be (1);
    }

    [Fact]
    public void Print_composed_letter_matches_the_precomposed_letter ()
    {
        //Arrange
        var decomposed = CreateTerminal ();
        var precomposed = CreateTerminal ();

        //Act
        decomposed.Feed ("caf" + "e" + CombiningAcute);
        precomposed.Feed ("café");

        //Assert
        RowText (decomposed).Should ().Be (RowText (precomposed));
        decomposed.Buffer.X.Should ().Be (precomposed.Buffer.X);
        Row (decomposed) [3].Code.Should ().Be (Row (precomposed) [3].Code);
        Row (decomposed) [3].Width.Should ().Be (Row (precomposed) [3].Width);
    }

    [Fact]
    public void Print_two_combining_marks_compose_one_at_a_time ()
    {
        //Arrange -- a, then a dot below, then a circumflex
        var terminal = CreateTerminal ();

        //Act
        terminal.Feed ("a" + CombiningDotBelow + CombiningCircumflex);

        //Assert -- both marks folded into the single precomposed letter
        Row (terminal) [0].Code.Should ().Be (0x1ead);
        Row (terminal) [1].Code.Should ().Be (0);
        terminal.Buffer.X.Should ().Be (1);
    }

    [Fact]
    public void Print_combining_mark_composes_with_a_base_from_an_earlier_feed ()
    {
        //Arrange -- the base character is already in the buffer
        var terminal = CreateTerminal ();
        terminal.Feed ("e");

        //Act
        terminal.Feed (CombiningAcute);

        //Assert
        Row (terminal) [0].Code.Should ().Be (0x00e9);
        terminal.Buffer.X.Should ().Be (1);
    }

    [Fact]
    public void Print_combining_mark_composes_inside_a_fullwidth_cell ()
    {
        //Arrange -- a Hangul lead consonant takes two cells, and the vowel that follows
        //it takes none

        var terminal = CreateTerminal ();

        //Act
        terminal.Feed (HangulChoseongKiyeok + HangulJungseongA + HangulJongseongKiyeok);

        //Assert -- the syllable is in the fullwidth cell, with its placeholder intact
        var line = Row (terminal);
        line [0].Code.Should ().Be (0xac01);
        line [0].Width.Should ().Be (2);
        line [1].Width.Should ().Be (0);
        line [1].Code.Should ().Be (0);
        line [2].Code.Should ().Be (0);
        terminal.Buffer.X.Should ().Be (2);
    }

    [Fact]
    public void Print_combining_mark_composes_in_the_last_column ()
    {
        //Arrange -- the base character sits in the last cell of the row
        var terminal = CreateTerminal (5, 4);
        terminal.Feed ("abcde");
        terminal.Buffer.X.Should ().Be (5);

        //Act
        terminal.Feed (CombiningAcute);

        //Assert -- it composed in place; nothing wrapped
        Row (terminal) [4].Code.Should ().Be (0x00e9);
        terminal.Buffer.X.Should ().Be (5);
        terminal.Buffer.Y.Should ().Be (0);
        Row (terminal, 1).HasAnyContent ().Should ().BeFalse ();
    }

    #endregion

    #region Format characters, which are dropped

    [Fact]
    public void Print_variation_selector_keeps_the_character_it_follows ()
    {
        //Arrange
        var terminal = CreateTerminal ();

        //Act
        terminal.Feed (Heart + VariationSelector16);

        //Assert -- the heart is still there and the selector cost nothing
        var line = Row (terminal);
        line [0].Code.Should ().Be (0x2764);
        line [0].Width.Should ().Be (System.Rune.ColumnWidth (new System.Rune (0x2764)));
        line [1].Code.Should ().Be (0);
        terminal.Buffer.X.Should ().Be (line [0].Width);
    }

    [Fact]
    public void Print_zero_width_joiner_leaves_both_emoji_standing ()
    {
        //Arrange
        var terminal = CreateTerminal ();

        //Act
        terminal.Feed (ManEmoji + ZeroWidthJoiner + ComputerEmoji);

        //Assert -- two fullwidth characters, the joiner gone
        var line = Row (terminal);
        line [0].Code.Should ().Be (0x1f468);
        line [0].Width.Should ().Be (2);
        line [1].Width.Should ().Be (0);
        line [2].Code.Should ().Be (0x1f4bb);
        line [2].Width.Should ().Be (2);
        line [3].Width.Should ().Be (0);
        terminal.Buffer.X.Should ().Be (4);
    }

    [Theory]
    [InlineData ("​")]     // zero width space
    [InlineData ("‌")]     // zero width non-joiner
    [InlineData ("‍")]     // zero width joiner
    [InlineData ("⁠")]     // word joiner
    [InlineData ("﻿")]     // zero width no-break space
    public void Print_format_character_takes_no_cell (string format)
    {
        //Arrange
        var terminal = CreateTerminal ();

        //Act
        terminal.Feed ("a" + format + "b");

        //Assert
        RowText (terminal).Should ().Be ("ab");
        terminal.Buffer.X.Should ().Be (2);
    }

    #endregion

    #region Marks with no precomposed form, which keep a cell of their own

    [Theory]
    [InlineData (ThaiKoKai, ThaiSaraI, 0x0e01, 0x0e34)]
    [InlineData (ArabicBeh, ArabicFatha, 0x0628, 0x064e)]
    public void Print_mark_without_a_precomposed_form_takes_its_own_cell (
        string baseCharacter, string mark, int baseCode, int markCode)
    {
        //Arrange
        var terminal = CreateTerminal ();

        //Act
        terminal.Feed (baseCharacter + mark);

        //Assert -- nothing is lost: the base keeps its cell and the mark gets one
        var line = Row (terminal);
        line [0].Code.Should ().Be (baseCode);
        line [0].Width.Should ().Be (1);
        line [1].Code.Should ().Be (markCode);
        line [1].Width.Should ().Be (1);
        terminal.Buffer.X.Should ().Be (2);
    }

    [Fact]
    public void Print_combining_mark_at_the_left_edge_takes_its_own_cell ()
    {
        //Arrange -- there is no character for the mark to belong to
        var terminal = CreateTerminal ();

        //Act
        terminal.Feed (CombiningAcute);

        //Assert
        Row (terminal) [0].Code.Should ().Be (0x0301);
        Row (terminal) [0].Width.Should ().Be (1);
        terminal.Buffer.X.Should ().Be (1);
    }

    [Fact]
    public void Print_combining_mark_after_a_blank_cell_takes_its_own_cell ()
    {
        //Arrange -- the cell before the cursor was never written
        var terminal = CreateTerminal ();
        terminal.Feed ("a\x1b[1;3H");

        //Act
        terminal.Feed (CombiningAcute);

        //Assert
        Row (terminal) [0].Code.Should ().Be ((int)'a');
        Row (terminal) [2].Code.Should ().Be (0x0301);
        Row (terminal) [2].Width.Should ().Be (1);
        terminal.Buffer.X.Should ().Be (3);
    }

    [Fact]
    public void Print_mark_without_a_precomposed_form_wraps_like_a_narrow_character ()
    {
        //Arrange -- the row is full, and the mark has no precomposed form
        var terminal = CreateTerminal (5, 4);
        terminal.Feed ("abcd" + ThaiKoKai);
        terminal.Buffer.X.Should ().Be (5);

        //Act
        terminal.Feed (ThaiSaraI);

        //Assert -- it autowrapped, exactly as any single-cell character would
        terminal.Buffer.Y.Should ().Be (1);
        terminal.Buffer.X.Should ().Be (1);
        Row (terminal, 1) [0].Code.Should ().Be (0x0e34);
        Row (terminal, 1).IsWrapped.Should ().BeTrue ();
    }

    [Fact]
    public void Print_sound_mark_that_the_width_table_calls_wide_keeps_both_characters ()
    {
        //Arrange -- this fork's width table reports U+3099 (and the CJK tone marks at
        //U+302A..U+302F) as TWO columns rather than as combining marks, and the text
        //layer's own tests pin that. Such a mark therefore never reaches the combining
        //path: it takes two cells of its own, and nothing is lost.
        var terminal = CreateTerminal ();

        //Act
        terminal.Feed ("が");

        //Assert
        var line = Row (terminal);
        line [0].Code.Should ().Be (0x304b);
        line [0].Width.Should ().Be (2);
        line [2].Code.Should ().Be (0x3099);
        line [2].Width.Should ().Be (2);
        terminal.Buffer.X.Should ().Be (4);
    }

    #endregion

    #region Reading the text back

    [Fact]
    public void TranslateToString_returns_the_composed_letter_once ()
    {
        //Arrange
        var terminal = CreateTerminal ();

        //Act
        terminal.Feed ("cafe" + CombiningAcute + " ok");

        //Assert
        RowText (terminal).Should ().Be ("café ok");
    }

    [Fact]
    public void GetSelectedText_returns_the_composed_letter_once ()
    {
        //Arrange
        var terminal = CreateTerminal ();
        terminal.Feed ("cafe" + CombiningAcute + " ok");
        var selection = new SelectionService (terminal);

        //Act
        selection.StartSelection (row: 0, col: 0);
        selection.DragExtend (row: 0, col: 7);

        //Assert
        selection.GetSelectedText ().Should ().Be ("café ok");
    }

    #endregion
}
