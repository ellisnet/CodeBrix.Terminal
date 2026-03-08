using CodeBrix.Terminal.Engine.CommandExtensions;
using CodeBrix.Terminal.Engine.CsiCommandExtensions;
using Xunit;

#pragma warning disable IDE0004

namespace CodeBrix.Terminal.Engine.Tests.EscapeSequence;

/// <summary>
/// Abstract base class for fill rectangle tests.
/// Subclasses provide the specific fill operation.
/// </summary>
public abstract class FillRectangleTests : BaseTerminalTests
{
    readonly string[] _fillData =
    [
        "abcdefgh",
        "ijklmnop",
        "qrstuvwx",
        "yz012345",
        "ABCDEFGH",
        "IJKLMNOP",
        "QRSTUVWX",
        "YZ6789!@"
    ];

    protected char TestCharacter = '_';

    void Prepare ()
    {
        Terminal.csiCUP ((1, 1));
        foreach (var line in _fillData) {
            Terminal.Feed (line + "\r\n");
        }
    }

    protected abstract void Fill ((int top, int left, int bottom, int right) rect);

    [Fact]
    public void Basic ()
    {
        Prepare ();
        Fill ((5, 5, 7, 7));
        AssertScreenCharsInRectEqual (1, 1, 8, 8, GetTestString (
            "abcdefgh" +
            "ijklmnop" +
            "qrstuvwx" +
            "yz012345" +
            "ABCD***H" +
            "IJKL***P" +
            "QRST***X" +
            "YZ6789!@"
        ));
    }

    [Fact]
    public void InvalidRectDoesNothing ()
    {
        Prepare ();
        Fill ((5, 5, 4, 4));
        AssertScreenCharsInRectEqual (1, 1, 8, 8, GetTestString (
            "abcdefgh" +
            "ijklmnop" +
            "qrstuvwx" +
            "yz012345" +
            "ABCDEFGH" +
            "IJKLMNOP" +
            "QRSTUVWX" +
            "YZ6789!@"
        ));
    }

    string GetTestString (string template)
    {
        return template.Replace ('*', TestCharacter);
    }
}

/// <summary>
/// DECFRA (Fill Rectangular Area) tests
/// </summary>
public class DecfraTests : FillRectangleTests
{
    public DecfraTests ()
    {
        TestCharacter = '%';
    }

    protected override void Fill ((int top, int left, int bottom, int right) rect)
    {
        Terminal.csiDECFRA ((int)TestCharacter, rect.top, rect.left, rect.bottom, rect.right);
    }
}
