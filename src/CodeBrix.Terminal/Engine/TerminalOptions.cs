using System;
namespace CodeBrix.Terminal.Engine; //was previously: namespace XtermSharp;

public enum CursorStyle {
    BlinkBlock, SteadyBlock, BlinkUnderline, SteadyUnderline, BlinkingBar, SteadyBar
}

public class TerminalOptions {
    public int Cols, Rows;
    public bool ConvertEol = true, CursorBlink;
    public string TermName;
    public CursorStyle CursorStyle;
    public bool ScreenReaderMode;

    /// <summary>
    /// Gets or sets the number of scrollback lines kept beyond the visible rows.
    /// Defaults to 1000. Set this BEFORE constructing the <see cref="Terminal"/>:
    /// the buffer sizes itself from this value.
    /// </summary>
    public int? Scrollback { get; set; }

    /// <summary>
    /// Gets or sets the tab stop width. Defaults to 8. Set this BEFORE
    /// constructing the <see cref="Terminal"/>: tab stops are laid out from this
    /// value when the buffer is created or resized.
    /// </summary>
    public int? TabStopWidth { get; set; }

    public TerminalOptions ()
    {
        Cols = 80;
        Rows = 25;
        TermName = "xterm";
        Scrollback = 1000;
        TabStopWidth = 8;
    }
}