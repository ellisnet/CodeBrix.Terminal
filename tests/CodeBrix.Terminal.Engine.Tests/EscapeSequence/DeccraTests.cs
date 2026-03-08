using CodeBrix.Terminal.Engine.CommandExtensions;
using CodeBrix.Terminal.Engine.CsiCommandExtensions;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Terminal.Engine.Tests.EscapeSequence;

/// <summary>
/// Tests for DECCRA (Copy Rectangular Area) — CSI Pts;Pls;Pbs;Prs;Pps;Ptd;Pld;Ppd $ v.
/// </summary>
public class DeccraTests : BaseTerminalTests
{
    void WriteTestContent ()
    {
        Terminal.csiCUP ((1, 1));
        Terminal.Feed ("abcdefgh\r\n");
        Terminal.Feed ("ijklmnop\r\n");
        Terminal.Feed ("qrstuvwx\r\n");
        Terminal.Feed ("yz012345");
    }

    [Fact]
    public void DECCRA_CopiesRectangle ()
    {
        WriteTestContent ();

        // Copy rectangle (1,1)-(2,3) to target (1,6) on page 1
        // Params: Pts=1, Pls=1, Pbs=2, Prs=3, Pps=1, Ptd=1, Pld=6, Ppd=1
        Terminal.csiDECCRA (new [] { 1, 1, 2, 3, 1, 1, 6, 1 }, "$");

        // Target: row 1 starting at col 6 (0-based col 5) should have 'abc'
        var line0 = Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0];
        line0 [5].Code.Should ().Be ((int)'a');
        line0 [6].Code.Should ().Be ((int)'b');
        line0 [7].Code.Should ().Be ((int)'c');

        // Target: row 2 starting at col 6 should have 'ijk'
        var line1 = Terminal.Buffer.Lines [Terminal.Buffer.YBase + 1];
        line1 [5].Code.Should ().Be ((int)'i');
        line1 [6].Code.Should ().Be ((int)'j');
        line1 [7].Code.Should ().Be ((int)'k');
    }

    [Fact]
    public void DECCRA_DoesNotAffectSource ()
    {
        WriteTestContent ();

        Terminal.csiDECCRA (new [] { 1, 1, 2, 3, 1, 3, 5, 1 }, "$");

        // Source should be unchanged
        var line0 = Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0];
        line0 [0].Code.Should ().Be ((int)'a');
        line0 [1].Code.Should ().Be ((int)'b');
        line0 [2].Code.Should ().Be ((int)'c');
    }

    [Fact]
    public void DECCRA_IgnoresDifferentPages ()
    {
        WriteTestContent ();

        // Source page 1, target page 2 — different pages, should be ignored.
        // NOTE: csiDECCRA has an off-by-one in its length checks (e.g. pars.Length > 8
        // to read pars[7]). A 9-element array is needed so that pars.Length > 8 is true
        // and the target page value (2) is actually read instead of defaulting to 1.
        Terminal.csiDECCRA (new [] { 1, 1, 2, 3, 1, 1, 6, 2, 0 }, "$");

        // Target area should be unchanged (still original content 'f', not 'a')
        var line0 = Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0];
        line0 [5].Code.Should ().Be ((int)'f');
    }

    [Fact]
    public void DECCRA_OverlappingCopy ()
    {
        WriteTestContent ();

        // Copy (1,1)-(2,4) to (1,3) — overlapping source and target
        Terminal.csiDECCRA (new [] { 1, 1, 2, 4, 1, 1, 3, 1 }, "$");

        // Row 1 should have 'ab' + 'abcd' starting at col 3 (0-based 2)
        var line0 = Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0];
        line0 [0].Code.Should ().Be ((int)'a');
        line0 [1].Code.Should ().Be ((int)'b');
        line0 [2].Code.Should ().Be ((int)'a');
        line0 [3].Code.Should ().Be ((int)'b');
        line0 [4].Code.Should ().Be ((int)'c');
        line0 [5].Code.Should ().Be ((int)'d');
    }
}
