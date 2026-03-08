using SilverAssertions;

namespace CodeBrix.Terminal.Engine.Tests.EscapeSequence;

/// <summary>
/// Test helper extension methods for Terminal, equivalent to those in XtermSharp.Tests.
/// Cursor positions use 1-based coordinates to match escape sequence conventions.
/// </summary>
internal static class TerminalTestExtensions
{
    /// <summary>
    /// Asserts the cursor position using 1-based coordinates (col, row).
    /// In origin mode, positions are relative to the scroll/margin origin.
    /// </summary>
    public static void AssertCursorPosition (this Terminal terminal, int col, int row)
    {
        int actualCol, actualRow;

        if (terminal.OriginMode) {
            var marginLeft = terminal.MarginMode ? terminal.Buffer.MarginLeft : 0;
            actualCol = terminal.Buffer.X - marginLeft + 1;
            actualRow = terminal.Buffer.Y - terminal.Buffer.ScrollTop + 1;
        } else {
            actualCol = terminal.Buffer.X + 1;
            actualRow = terminal.Buffer.Y + 1;
        }

        actualCol.Should ().Be (col, $"because cursor column should be {col}");
        actualRow.Should ().Be (row, $"because cursor row should be {row}");
    }

    /// <summary>
    /// Returns the screen size as (cols, rows).
    /// </summary>
    public static (int cols, int rows) GetScreenSize (this Terminal terminal)
    {
        return (terminal.Cols, terminal.Rows);
    }
}
