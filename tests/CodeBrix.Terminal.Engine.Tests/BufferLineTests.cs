using SilverAssertions;
using Xunit;

namespace CodeBrix.Terminal.Engine.Tests;

/// <summary>
/// BufferLine cell arithmetic, in particular around the two cells of a fullwidth
/// character: the character in one cell with width 2, and a placeholder of width 0
/// after it. Every cell move has to leave the pair either whole or gone.
/// </summary>
public class BufferLineTests
{
    const int Ideograph = 0x6f22;

    static BufferLine CreateLine (int cols = 8)
        => new BufferLine (cols, CharData.Null);

    static void WriteNarrow (BufferLine line, int col, char ch)
        => line [col] = new CharData (CharData.DefaultAttr, ch, 1, ch);

    // Writes a fullwidth character and its placeholder, the way the print path does
    static void WriteWide (BufferLine line, int col)
    {
        line [col] = new CharData (CharData.DefaultAttr, (uint)Ideograph, 2, Ideograph);
        var placeholder = CharData.Null;
        placeholder.Width = 0;
        line [col + 1] = placeholder;
    }

    static void AssertNoHalfCharacters (BufferLine line)
    {
        for (var col = 0; col < line.Length; col++) {
            if (line [col].Width == 2) {
                (col + 1 < line.Length).Should ().BeTrue ($"because column {col} needs room for a placeholder");
                line [col + 1].Width.Should ().Be (0, $"because the character at column {col} needs its placeholder");
            }

            if (line [col].Width == 0) {
                (col > 0).Should ().BeTrue ($"because the placeholder at column {col} needs an owner");
                line [col - 1].Width.Should ().Be (2, $"because the placeholder at column {col} needs an owner");
            }
        }
    }

    [Fact]
    public void GetTrimmedLength_counts_every_column_of_a_narrow_line ()
    {
        //Arrange
        var line = CreateLine ();
        WriteNarrow (line, 0, 'a');
        WriteNarrow (line, 1, 'b');
        WriteNarrow (line, 2, 'c');

        //Act
        var length = line.GetTrimmedLength ();

        //Assert
        length.Should ().Be (3);
    }

    [Fact]
    public void GetTrimmedLength_counts_a_fullwidth_character_as_two_columns ()
    {
        //Arrange -- a b then a fullwidth character in columns 2 and 3
        var line = CreateLine ();
        WriteNarrow (line, 0, 'a');
        WriteNarrow (line, 1, 'b');
        WriteWide (line, 2);

        //Act
        var length = line.GetTrimmedLength ();

        //Assert
        length.Should ().Be (4);
    }

    [Fact]
    public void GetTrimmedLength_never_exceeds_the_line_length ()
    {
        //Arrange -- a fullwidth character cut in half by a narrowing resize
        var line = CreateLine ();
        WriteWide (line, 6);
        line.Resize (7, CharData.Null);

        //Act
        var length = line.GetTrimmedLength ();

        //Assert
        length.Should ().BeLessThanOrEqualTo (line.Length);
    }

    [Fact]
    public void TranslateToString_skips_the_placeholder_cell ()
    {
        //Arrange
        var line = CreateLine ();
        WriteNarrow (line, 0, 'a');
        WriteWide (line, 1);
        WriteNarrow (line, 3, 'b');

        //Act
        var text = line.TranslateToString (true).ToString ();

        //Assert
        text.Should ().Be ("a漢b");
    }

    [Fact]
    public void InsertCells_blanks_a_character_whose_placeholder_is_pushed_off_the_line ()
    {
        //Arrange -- the fullwidth character ends the line
        var line = CreateLine ();
        WriteNarrow (line, 0, 'a');
        WriteWide (line, 6);

        //Act
        line.InsertCells (0, 1, line.Length - 1, CharData.Null);

        //Assert
        AssertNoHalfCharacters (line);
        line [7].Width.Should ().Be (1);
        line [7].Code.Should ().Be (0);
    }

    [Fact]
    public void InsertCells_at_a_placeholder_blanks_both_cells_of_the_character ()
    {
        //Arrange
        var line = CreateLine ();
        WriteWide (line, 1);
        WriteNarrow (line, 3, 'z');

        //Act -- insert where the placeholder sits
        line.InsertCells (2, 1, line.Length - 1, CharData.Null);

        //Assert
        AssertNoHalfCharacters (line);
        line [1].Code.Should ().Be (0);
    }

    [Fact]
    public void DeleteCells_at_the_character_leaves_no_placeholder_behind ()
    {
        //Arrange
        var line = CreateLine ();
        WriteWide (line, 1);
        WriteNarrow (line, 3, 'z');

        //Act
        line.DeleteCells (1, 1, line.Length - 1, CharData.Null);

        //Assert
        AssertNoHalfCharacters (line);
    }

    [Fact]
    public void DeleteCells_at_the_placeholder_blanks_the_character ()
    {
        //Arrange
        var line = CreateLine ();
        WriteWide (line, 1);
        WriteNarrow (line, 3, 'z');

        //Act
        line.DeleteCells (2, 1, line.Length - 1, CharData.Null);

        //Assert
        AssertNoHalfCharacters (line);
        line [1].Code.Should ().Be (0);
    }

    [Fact]
    public void ReplaceCells_starting_on_a_placeholder_blanks_the_character ()
    {
        //Arrange
        var line = CreateLine ();
        WriteWide (line, 1);

        //Act -- replace from the placeholder onwards
        line.ReplaceCells (2, 5, new CharData (CharData.DefaultAttr));

        //Assert
        AssertNoHalfCharacters (line);
        line [1].Code.Should ().Be (0);
    }

    [Fact]
    public void ReplaceCells_ending_on_a_character_blanks_its_placeholder ()
    {
        //Arrange
        var line = CreateLine ();
        WriteWide (line, 3);

        //Act -- the last replaced cell is the character itself
        line.ReplaceCells (1, 4, new CharData (CharData.DefaultAttr));

        //Assert
        AssertNoHalfCharacters (line);
        line [4].Width.Should ().Be (1);
    }

    [Fact]
    public void Resize_smaller_blanks_a_character_cut_in_half ()
    {
        //Arrange -- the character occupies the last two cells
        var line = CreateLine ();
        WriteWide (line, 6);

        //Act -- cut the placeholder off
        line.Resize (7, CharData.Null);

        //Assert
        AssertNoHalfCharacters (line);
        line [6].Width.Should ().Be (1);
        line [6].Code.Should ().Be (0);
    }

    [Fact]
    public void Resize_smaller_keeps_a_character_that_still_fits ()
    {
        //Arrange
        var line = CreateLine ();
        WriteWide (line, 4);

        //Act
        line.Resize (6, CharData.Null);

        //Assert
        AssertNoHalfCharacters (line);
        line [4].Code.Should ().Be (Ideograph);
    }
}
