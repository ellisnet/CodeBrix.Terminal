using SilverAssertions;
using Xunit;

namespace CodeBrix.Terminal.Engine.Tests;

public class CharDataTests
{
    [Fact]
    public void IsBlank_TrueForNeverWrittenCells ()
    {
        CharData.Null.IsBlank.Should ().BeTrue ();
        new CharData (CharData.DefaultAttr).IsBlank.Should ().BeTrue ();
    }

    [Fact]
    public void IsBlank_FalseForWrittenCells ()
    {
        new CharData (CharData.DefaultAttr, 'A', 1, 'A').IsBlank.Should ().BeFalse ();
        CharData.WhiteSpace.IsBlank.Should ().BeFalse ();
    }

    [Fact]
    public void NullCell_CarriesRune0x200_NotASpace ()
    {
        // Pins the documented sharp edge: renderers that draw Rune verbatim
        // paint stray U+0200 glyphs for blank cells.
        CharData.Null.Rune.Should ().Be ((System.Rune)'Ȁ');
        CharData.Null.Code.Should ().Be (0);
    }
}
