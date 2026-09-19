using SilverAssertions;
using System;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace CodeBrix.Terminal.Engine.Tests;

/// <summary>
/// Feeding the terminal nothing. Every public entry point takes a missing or empty
/// payload as a quiet no-op: nothing is written, the cursor does not move and the
/// parser keeps the state it was in.
/// </summary>
public class TerminalFeedTests
{
    static Terminal CreateTerminal ()
        => new Terminal (null, new TerminalOptions { Cols = 20, Rows = 5 });

    static string FirstRow (Terminal terminal)
        => terminal.Buffer.Lines [terminal.Buffer.YBase].TranslateToString (true).ToString ();

    [Fact]
    public void Feed_empty_string_is_a_no_op ()
    {
        //Arrange
        var terminal = CreateTerminal ();

        //Act
        Action act = () => terminal.Feed ("");

        //Assert
        act.Should ().NotThrow ();
        terminal.Buffer.X.Should ().Be (0);
        terminal.Buffer.Y.Should ().Be (0);
    }

    [Fact]
    public void Feed_null_string_is_a_no_op ()
    {
        //Arrange
        var terminal = CreateTerminal ();

        //Act
        Action act = () => terminal.Feed ((string)null);

        //Assert
        act.Should ().NotThrow ();
        terminal.Buffer.X.Should ().Be (0);
    }

    [Fact]
    public void Feed_empty_byte_array_is_a_no_op ()
    {
        //Arrange
        var terminal = CreateTerminal ();

        //Act
        Action act = () => {
            terminal.Feed (new byte [0], 0);
            terminal.Feed (new byte [0]);
        };

        //Assert
        act.Should ().NotThrow ();
        terminal.Buffer.X.Should ().Be (0);
    }

    [Fact]
    public void Feed_null_byte_array_is_a_no_op ()
    {
        //Arrange
        var terminal = CreateTerminal ();

        //Act
        Action act = () => terminal.Feed ((byte [])null, 0);

        //Assert
        act.Should ().NotThrow ();
        terminal.Buffer.X.Should ().Be (0);
    }

    [Fact]
    public void Feed_zero_length_of_a_full_array_is_a_no_op ()
    {
        //Arrange
        var terminal = CreateTerminal ();

        //Act
        terminal.Feed (Encoding.UTF8.GetBytes ("hello"), 0);

        //Assert
        terminal.Buffer.X.Should ().Be (0);
        FirstRow (terminal).Should ().BeEmpty ();
    }

    [Fact]
    public void Feed_intptr_zero_is_a_no_op ()
    {
        //Arrange
        var terminal = CreateTerminal ();

        //Act
        Action act = () => {
            terminal.Feed (IntPtr.Zero, 0);
            terminal.Feed (IntPtr.Zero);
        };

        //Assert
        act.Should ().NotThrow ();
        terminal.Buffer.X.Should ().Be (0);
    }

    [Fact]
    public void Feed_intptr_with_a_non_positive_length_is_a_no_op ()
    {
        //Arrange
        var terminal = CreateTerminal ();
        var bytes = Encoding.UTF8.GetBytes ("hello");
        var unmanaged = Marshal.AllocHGlobal (bytes.Length);

        try {
            Marshal.Copy (bytes, 0, unmanaged, bytes.Length);

            //Act
            terminal.Feed (unmanaged, 0);
            terminal.Feed (unmanaged, -1);

            //Assert -- unmanaged memory carries no length, so nothing is read
            terminal.Buffer.X.Should ().Be (0);
            FirstRow (terminal).Should ().BeEmpty ();

            //Act -- and a real length still works
            terminal.Feed (unmanaged, bytes.Length);

            //Assert
            FirstRow (terminal).Should ().Be ("hello");
        } finally {
            Marshal.FreeHGlobal (unmanaged);
        }
    }

    [Fact]
    public void Feed_empty_string_changes_nothing_on_screen ()
    {
        //Arrange
        var terminal = CreateTerminal ();
        terminal.Feed ("hello");
        var before = FirstRow (terminal);
        var x = terminal.Buffer.X;
        var y = terminal.Buffer.Y;

        //Act
        terminal.Feed ("");

        //Assert
        FirstRow (terminal).Should ().Be (before);
        terminal.Buffer.X.Should ().Be (x);
        terminal.Buffer.Y.Should ().Be (y);
    }

    [Fact]
    public void Feed_empty_string_does_not_touch_the_update_range ()
    {
        //Arrange
        var terminal = CreateTerminal ();
        terminal.Feed ("hello");
        terminal.ClearUpdateRange ();

        //Act
        terminal.Feed ("");

        //Assert
        terminal.GetUpdateRange (out var startY, out var endY);
        startY.Should ().Be (int.MaxValue);
        endY.Should ().Be (-1);
    }

    [Fact]
    public void Feed_after_an_empty_feed_still_works ()
    {
        //Arrange
        var terminal = CreateTerminal ();

        //Act
        terminal.Feed ("");
        terminal.Feed ("hello");

        //Assert
        FirstRow (terminal).Should ().Be ("hello");
        terminal.Buffer.X.Should ().Be (5);
    }

    [Fact]
    public void Feed_empty_string_keeps_a_half_received_escape_sequence ()
    {
        //Arrange -- the sequence is split around an empty feed
        var terminal = CreateTerminal ();

        //Act
        terminal.Feed ("\x1b[");
        terminal.Feed ("");
        terminal.Feed ("31mX");

        //Assert -- the X came out red, so the parser was still mid-sequence
        var cell = terminal.Buffer.Lines [terminal.Buffer.YBase] [0];
        cell.Code.Should ().Be ((int)'X');
        var (foreground, _, _) = CharacterAttribute.Unpack (cell.Attribute);
        foreground.Should ().Be (1);
    }

    [Fact]
    public void Feed_empty_byte_array_keeps_a_half_received_utf8_character ()
    {
        //Arrange -- the three bytes of an ideograph, split around an empty feed
        var terminal = CreateTerminal ();
        var bytes = Encoding.UTF8.GetBytes ("漢");

        //Act
        terminal.Feed (new [] { bytes [0] }, 1);
        terminal.Feed (new byte [0], 0);
        terminal.Feed (new [] { bytes [1], bytes [2] }, 2);

        //Assert
        terminal.Buffer.Lines [terminal.Buffer.YBase] [0].Code.Should ().Be (0x6f22);
    }

    [Fact]
    public void Feed_length_past_the_end_of_the_array_reads_only_the_array ()
    {
        //Arrange
        var terminal = CreateTerminal ();

        //Act -- two bytes, but a length of ten
        terminal.Feed (Encoding.UTF8.GetBytes ("AB"), 10);

        //Assert
        FirstRow (terminal).Should ().Be ("AB");
        terminal.Buffer.X.Should ().Be (2);
    }

    [Fact]
    public void Feed_negative_length_reads_the_whole_array ()
    {
        //Arrange
        var terminal = CreateTerminal ();

        //Act
        terminal.Feed (Encoding.UTF8.GetBytes ("AB"), -5);

        //Assert
        FirstRow (terminal).Should ().Be ("AB");
        terminal.Buffer.X.Should ().Be (2);
    }
}
