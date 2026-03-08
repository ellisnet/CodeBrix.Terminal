using CodeBrix.Terminal.Engine;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using System.Text;

using TerminalColor = CodeBrix.Terminal.Engine.Color;

namespace RemoteTerminal.Client.Controls;

// ReSharper disable once RedundantExtendsListEntry
public sealed partial class TerminalControl : UserControl
{
    private readonly Terminal _terminal;

    public TerminalControl()
    {
        InitializeComponent();
        _terminal = new Terminal(options: new TerminalOptions { Cols = 120, Rows = 50 });
    }

    public void Feed(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        _terminal.Feed(text);
        Render();
    }

    public void ScrollToEnd(bool onlyIfFull = true)
    {
        OutputScroller.UpdateLayout();

        if (onlyIfFull && OutputScroller.ExtentHeight <= OutputScroller.ViewportHeight)
        {
            return;
        }

        OutputScroller.ChangeView(null, OutputScroller.ExtentHeight, null);
    }

    private void Render()
    {
        OutputBlock.Blocks.Clear();

        var buffer = _terminal.Buffer;
        var lines = buffer.Lines;
        int totalLines = lines.Length;

        // Find the last row that has actual content so we don't emit
        // trailing LineBreak elements for empty rows — those would inflate
        // ExtentHeight and break the "only scroll when needed" logic.
        int lastContentRow = -1;
        for (int row = totalLines - 1; row >= 0; row--)
        {
            var line = lines[row];
            if (line != null && line.Length > 0 && GetTrimmedLength(line) > 0)
            {
                lastContentRow = row;
                break;
            }
        }

        if (lastContentRow < 0)
        {
            return;
        }

        var paragraph = new Paragraph();

        for (int row = 0; row <= lastContentRow; row++)
        {
            var line = lines[row];

            if (row > 0)
            {
                paragraph.Inlines.Add(new LineBreak());
            }

            if (line == null || line.Length == 0)
            {
                continue;
            }

            int trimmedLen = GetTrimmedLength(line);
            if (trimmedLen == 0)
            {
                continue;
            }

            int currentAttr = line[0].Attribute;
            var sb = new StringBuilder();

            for (int col = 0; col < trimmedLen; col++)
            {
                var ch = line[col];

                if (ch.Attribute != currentAttr && sb.Length > 0)
                {
                    paragraph.Inlines.Add(CreateRun(sb.ToString(), currentAttr));
                    sb.Clear();
                    currentAttr = ch.Attribute;
                }

                AppendCharacter(sb, ch);
            }

            if (sb.Length > 0)
            {
                paragraph.Inlines.Add(CreateRun(sb.ToString(), currentAttr));
            }
        }

        OutputBlock.Blocks.Add(paragraph);
    }

    private static int GetTrimmedLength(BufferLine line)
    {
        for (int i = line.Length - 1; i >= 0; i--)
        {
            if (line[i].Code != 0)
                return i + 1;
        }
        return 0;
    }

    private static void AppendCharacter(StringBuilder sb, CharData ch)
    {
        if (ch.Code == 0 || ch.Rune.Value == 0x200)
        {
            sb.Append(' ');
        }
        else if (ch.Rune.Value <= 0xFFFF)
        {
            sb.Append((char)ch.Rune.Value);
        }
        else
        {
            sb.Append(char.ConvertFromUtf32((int)ch.Rune.Value));
        }
    }

    private static Run CreateRun(string text, int attribute)
    {
        var run = new Run { Text = text };

        var flags = (FLAGS)(attribute >> 18);
        int fg = (attribute >> 9) & 0x1ff;

        if (fg != Renderer.DefaultColor && fg != Renderer.InvertedDefaultColor
            && fg >= 0 && fg < TerminalColor.DefaultAnsiColors.Count)
        {
            var c = TerminalColor.DefaultAnsiColors[fg];
            run.Foreground = new SolidColorBrush(
                Windows.UI.Color.FromArgb(255, c.Red, c.Green, c.Blue));
        }

        if (flags.HasFlag(FLAGS.BOLD))
        {
            run.FontWeight = FontWeights.Bold;
        }

        if (flags.HasFlag(FLAGS.ITALIC))
        {
            run.FontStyle = Windows.UI.Text.FontStyle.Italic;
        }

        if (flags.HasFlag(FLAGS.CrossedOut))
        {
            run.TextDecorations = Windows.UI.Text.TextDecorations.Strikethrough;
        }

        return run;
    }
}
