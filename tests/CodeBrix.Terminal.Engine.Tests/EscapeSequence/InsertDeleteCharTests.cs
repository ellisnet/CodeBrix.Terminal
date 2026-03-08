using CodeBrix.Terminal.Engine.CommandExtensions;
using CodeBrix.Terminal.Engine.CsiCommandExtensions;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Terminal.Engine.Tests.EscapeSequence;

/// <summary>
/// Tests for character insertion and deletion:
/// CSI @ (ICH — Insert Characters), CSI P (DCH — Delete Characters),
/// DECIC (Insert Columns), DECDC (Delete Columns).
/// </summary>
public class InsertDeleteCharTests : BaseTerminalTests
{
    void WriteTestLine ()
    {
        Terminal.csiCUP ((1, 1));
        Terminal.Feed ("abcdefgh");
    }

    #region CSI @ — Insert Characters (ICH)

    [Fact]
    public void ICH_InsertsBlankCharacters ()
    {
        WriteTestLine ();
        Terminal.csiCUP ((3, 1));
        // CSI 2 @ — insert 2 blank characters at cursor
        Terminal.Feed ("\x1b[2@");

        var line = Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0];
        line [0].Code.Should ().Be ((int)'a');
        line [1].Code.Should ().Be ((int)'b');
        line [2].Code.Should ().Be (0); // inserted blank
        line [3].Code.Should ().Be (0); // inserted blank
        line [4].Code.Should ().Be ((int)'c');
        line [5].Code.Should ().Be ((int)'d');
    }

    [Fact]
    public void ICH_DefaultInsertsOne ()
    {
        WriteTestLine ();
        Terminal.csiCUP ((2, 1));
        // CSI @ (no param) — insert 1 blank character
        Terminal.Feed ("\x1b[@");

        var line = Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0];
        line [0].Code.Should ().Be ((int)'a');
        line [1].Code.Should ().Be (0); // inserted blank
        line [2].Code.Should ().Be ((int)'b');
    }

    #endregion

    #region CSI P — Delete Characters (DCH)

    [Fact]
    public void DCH_DeletesCharacters ()
    {
        WriteTestLine ();
        Terminal.csiCUP ((3, 1));
        // CSI 2 P — delete 2 characters at cursor
        Terminal.Feed ("\x1b[2P");

        var line = Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0];
        line [0].Code.Should ().Be ((int)'a');
        line [1].Code.Should ().Be ((int)'b');
        line [2].Code.Should ().Be ((int)'e'); // 'c' and 'd' deleted, content shifted left
        line [3].Code.Should ().Be ((int)'f');
    }

    [Fact]
    public void DCH_DefaultDeletesOne ()
    {
        WriteTestLine ();
        Terminal.csiCUP ((1, 1));
        // CSI P (no param) — delete 1 character
        Terminal.Feed ("\x1b[P");

        var line = Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0];
        line [0].Code.Should ().Be ((int)'b');
        line [1].Code.Should ().Be ((int)'c');
    }

    [Fact]
    public void DCH_WithMarginMode ()
    {
        Terminal.csiDECSET (CsiCommandCodes.DECLRMM);
        Terminal.csiDECSLRM (3, 6); // margins at cols 3-6 (1-based)
        Terminal.csiCUP ((1, 1));
        Terminal.Feed ("abcdefgh");
        Terminal.csiCUP ((4, 1));

        // CSI 1 P — delete 1 char within margins
        Terminal.Feed ("\x1b[1P");

        var line = Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0];
        line [0].Code.Should ().Be ((int)'a');
        line [1].Code.Should ().Be ((int)'b');
        line [2].Code.Should ().Be ((int)'c');
        line [3].Code.Should ().Be ((int)'e'); // 'd' deleted, content shifted left within margin
    }

    #endregion

    #region DECIC — Insert Columns

    [Fact]
    public void DECIC_InsertsColumns ()
    {
        // Set up scroll region and margin mode
        Terminal.Feed ("\x1b[1;5r"); // scroll region rows 1-5
        Terminal.csiDECSET (CsiCommandCodes.DECLRMM);
        Terminal.csiDECSLRM (1, 8);

        Terminal.csiCUP ((1, 1));
        Terminal.Feed ("abcdefgh");
        Terminal.csiCUP ((1, 2));
        Terminal.Feed ("ijklmnop");

        Terminal.csiCUP ((3, 1));
        // CSI 2 ' } — insert 2 columns at cursor col
        Terminal.csiDECIC (new [] { 2 });

        var line0 = Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0];
        line0 [0].Code.Should ().Be ((int)'a');
        line0 [1].Code.Should ().Be ((int)'b');
        // Cols 2-3 should be blank (inserted)
        line0 [2].Code.Should ().Be (32);
        line0 [3].Code.Should ().Be (32);
        line0 [4].Code.Should ().Be ((int)'c');
    }

    #endregion

    #region DECDC — Delete Columns

    [Fact]
    public void DECDC_DeletesColumns ()
    {
        // Set up scroll region and margin mode
        Terminal.Feed ("\x1b[1;5r"); // scroll region rows 1-5
        Terminal.csiDECSET (CsiCommandCodes.DECLRMM);
        Terminal.csiDECSLRM (1, 8);

        Terminal.csiCUP ((1, 1));
        Terminal.Feed ("abcdefgh");
        Terminal.csiCUP ((1, 2));
        Terminal.Feed ("ijklmnop");

        Terminal.csiCUP ((3, 1));
        // CSI 1 ' ~ — delete 1 column at cursor col
        Terminal.csiDECDC (new [] { 1 });

        var line0 = Terminal.Buffer.Lines [Terminal.Buffer.YBase + 0];
        line0 [0].Code.Should ().Be ((int)'a');
        line0 [1].Code.Should ().Be ((int)'b');
        line0 [2].Code.Should ().Be ((int)'d'); // 'c' deleted, shifted left
        line0 [3].Code.Should ().Be ((int)'e');
    }

    #endregion
}
