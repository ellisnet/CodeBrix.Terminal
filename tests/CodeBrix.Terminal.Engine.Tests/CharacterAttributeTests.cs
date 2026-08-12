using SilverAssertions;
using Xunit;

namespace CodeBrix.Terminal.Engine.Tests;

public class CharacterAttributeTests
{
    [Fact]
    public void Unpack_SplitsTheThreeComponents ()
    {
        // bits 0-8 background, 9-17 foreground, 18+ flags
        var attribute = (2 << 9) | 7 | ((int)(FLAGS.BOLD | FLAGS.UNDERLINE) << 18);

        var unpacked = CharacterAttribute.Unpack (attribute);

        unpacked.Foreground.Should ().Be (2);
        unpacked.Background.Should ().Be (7);
        unpacked.Flags.Should ().Be (FLAGS.BOLD | FLAGS.UNDERLINE);
    }

    [Fact]
    public void Unpack_DefaultAttr_YieldsTheDefaultColorSentinels ()
    {
        var unpacked = CharacterAttribute.Unpack (CharData.DefaultAttr);

        unpacked.Foreground.Should ().Be (CharacterAttribute.DefaultColorIndex);
        unpacked.Background.Should ().Be (CharacterAttribute.DefaultColorIndex);
        unpacked.Flags.Should ().Be ((FLAGS)0);
    }

    [Fact]
    public void Unpack_SupportsDeconstruction ()
    {
        var attribute = (12 << 9) | 34 | ((int)FLAGS.ITALIC << 18);

        var (foreground, background, flags) = CharacterAttribute.Unpack (attribute);

        foreground.Should ().Be (12);
        background.Should ().Be (34);
        flags.Should ().Be (FLAGS.ITALIC);
    }

    [Fact]
    public void DefaultColorConstants_MatchTheRendererValues ()
    {
        CharacterAttribute.DefaultColorIndex.Should ().Be (256);
        CharacterAttribute.InvertedDefaultColorIndex.Should ().Be (257);
    }

    static int Pack (FLAGS flags, int fg, int bg)
    {
        return ((int)flags << 18) | (fg << 9) | bg;
    }

    [Fact]
    public void ToSGR_DefaultAttribute_IsReset ()
    {
        CharacterAttribute.ToSGR (CharData.DefaultAttr).Should ().Be ("0m");
    }

    [Fact]
    public void ToSGR_EmitsEveryFlag_InNumericOrder ()
    {
        var flags = FLAGS.BOLD | FLAGS.DIM | FLAGS.ITALIC | FLAGS.UNDERLINE
            | FLAGS.BLINK | FLAGS.INVERSE | FLAGS.INVISIBLE | FLAGS.CrossedOut;

        CharacterAttribute.ToSGR (Pack (flags, 256, 256)).Should ().Be ("0;1;2;3;4;5;7;8;9m");
    }

    [Theory]
    [InlineData (1, "0;31m")]      // dark red -> 30-37 range
    [InlineData (9, "0;91m")]      // bright red -> 90-97 range
    [InlineData (16, "0;38;5;16m")] // first cube color -> 38;5;N, not an invalid 98
    [InlineData (200, "0;38;5;200m")]
    public void ToSGR_ForegroundRanges (int fg, string expected)
    {
        CharacterAttribute.ToSGR (Pack (0, fg, 256)).Should ().Be (expected);
    }

    [Theory]
    [InlineData (4, "0;44m")]       // dark blue -> 40-47 range
    [InlineData (12, "0;104m")]     // bright blue -> 100-107 range
    [InlineData (16, "0;48;5;16m")] // first cube color -> 48;5;N, not an invalid 108
    [InlineData (232, "0;48;5;232m")]
    public void ToSGR_BackgroundRanges (int bg, string expected)
    {
        CharacterAttribute.ToSGR (Pack (0, 256, bg)).Should ().Be (expected);
    }

    [Fact]
    public void ToSGR_NeverEmitsATrailingEmptyParameter ()
    {
        // A ';' directly before 'm' would parse as an extra empty parameter,
        // i.e. SGR 0 - resetting the very attributes being reported.
        CharacterAttribute.ToSGR (Pack (FLAGS.BOLD, 1, 4)).Should ().Be ("0;1;31;44m");
    }
}
