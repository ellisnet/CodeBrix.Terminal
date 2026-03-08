using CodeBrix.Terminal.Engine.CommandExtensions;
using CodeBrix.Terminal.Engine.CsiCommandExtensions;
using Xunit;

namespace CodeBrix.Terminal.Engine.Tests.EscapeSequence;

/// <summary>
/// CBT (Backward Tab) tests
/// </summary>
public class CbtTests : BaseTerminalTests
{
    [Fact]
    public void CBT_OneTabStopByDefault ()
    {
        Terminal.csiCUP ((17, 1));
        Terminal.csiCBT ();
        Terminal.AssertCursorPosition (9, 1);
    }

    [Fact]
    public void CBT_ExplicitParameter ()
    {
        Terminal.csiCUP ((25, 1));
        Terminal.csiCBT (2);
        Terminal.AssertCursorPosition (9, 1);
    }

    [Fact]
    public void CBT_StopsAtLeftEdge ()
    {
        Terminal.csiCUP ((25, 2));
        Terminal.csiCBT (5);
        Terminal.AssertCursorPosition (1, 2);
    }

    [Fact]
    public void CBT_IgnoresRegion ()
    {
        // Set a scroll region.
        Terminal.csiDECSET (CsiCommandCodes.DECLRMM);
        Terminal.csiDECSLRM (5, 30);

        // Move to center of region
        Terminal.csiCUP ((7, 2));

        // Tab backwards out of the region.
        Terminal.csiCBT (2);
        Terminal.AssertCursorPosition (1, 2);
    }
}
