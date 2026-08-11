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
}
