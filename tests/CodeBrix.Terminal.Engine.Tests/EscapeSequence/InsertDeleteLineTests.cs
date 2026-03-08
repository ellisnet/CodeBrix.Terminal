using CodeBrix.Terminal.Engine.CommandExtensions;
using CodeBrix.Terminal.Engine.CsiCommandExtensions;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Terminal.Engine.Tests.EscapeSequence;

/// <summary>
/// Tests for line insertion and deletion:
/// CSI L (IL — Insert Lines), CSI M (DL — Delete Lines).
/// </summary>
public class InsertDeleteLineTests : BaseTerminalTests
{
    void WriteTestContent ()
    {
        Terminal.csiCUP ((1, 1));
        Terminal.Feed ("AAAA\r\nBBBB\r\nCCCC\r\nDDDD\r\nEEEE");
    }

    #region CSI L — Insert Lines (IL)

    [Fact]
    public void IL_InsertsOneBlankLine ()
    {
        WriteTestContent ();
        // Move to row 2
        Terminal.csiCUP ((1, 2));
        // CSI 1 L — insert 1 line
        Terminal.Feed ("\x1b[1L");

        // Row 1 unchanged
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [0].Code.Should ().Be ((int)'A');
        // Row 2 should now be blank (inserted)
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 1] [0].Code.Should ().Be (0);
        // Row 3 should have old row 2 content
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 2] [0].Code.Should ().Be ((int)'B');
        // Row 4 should have old row 3 content
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 3] [0].Code.Should ().Be ((int)'C');
    }

    [Fact]
    public void IL_InsertsMultipleLines ()
    {
        WriteTestContent ();
        Terminal.csiCUP ((1, 2));
        // CSI 2 L — insert 2 lines
        Terminal.Feed ("\x1b[2L");

        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [0].Code.Should ().Be ((int)'A');
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 1] [0].Code.Should ().Be (0);
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 2] [0].Code.Should ().Be (0);
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 3] [0].Code.Should ().Be ((int)'B');
    }

    [Fact]
    public void IL_DefaultInsertsOne ()
    {
        WriteTestContent ();
        Terminal.csiCUP ((1, 3));
        // CSI L (no param) — insert 1 line
        Terminal.Feed ("\x1b[L");

        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 1] [0].Code.Should ().Be ((int)'B');
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 2] [0].Code.Should ().Be (0); // inserted blank
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 3] [0].Code.Should ().Be ((int)'C');
    }

    #endregion

    #region CSI M — Delete Lines (DL)

    [Fact]
    public void DL_DeletesOneLine ()
    {
        WriteTestContent ();
        Terminal.csiCUP ((1, 2));
        // CSI 1 M — delete 1 line
        Terminal.Feed ("\x1b[1M");

        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [0].Code.Should ().Be ((int)'A');
        // Row 2 should now have old row 3 content (CCCC)
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 1] [0].Code.Should ().Be ((int)'C');
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 2] [0].Code.Should ().Be ((int)'D');
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 3] [0].Code.Should ().Be ((int)'E');
    }

    [Fact]
    public void DL_DeletesMultipleLines ()
    {
        WriteTestContent ();
        Terminal.csiCUP ((1, 2));
        // CSI 2 M — delete 2 lines
        Terminal.Feed ("\x1b[2M");

        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [0].Code.Should ().Be ((int)'A');
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 1] [0].Code.Should ().Be ((int)'D');
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 2] [0].Code.Should ().Be ((int)'E');
    }

    [Fact]
    public void DL_DefaultDeletesOne ()
    {
        WriteTestContent ();
        Terminal.csiCUP ((1, 1));
        // CSI M (no param) — delete 1 line
        Terminal.Feed ("\x1b[M");

        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0] [0].Code.Should ().Be ((int)'B');
        Terminal.Buffer.Lines [Terminal.Buffer.YBase + 1] [0].Code.Should ().Be ((int)'C');
    }

    #endregion
}
