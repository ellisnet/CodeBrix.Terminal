================================================================================
AGENT-README: CodeBrix.Terminal
A Comprehensive Guide for AI Coding Agents
================================================================================

OVERVIEW
--------
CodeBrix.Terminal is a .NET terminal emulation engine with Unicode text support.
It provides a virtual terminal (VT100/VT220/VT400/xterm-compatible) emulation
engine, including a full ANSI/DEC escape sequence parser, terminal buffer
management, and Unicode text utilities.

CodeBrix.Terminal has ZERO external dependencies beyond .NET itself.

It consists of two main components:
  - A terminal engine forked from XtermSharp (by Miguel de Icaza)
  - Unicode text utilities derived from NStack version 1.1.1

IMPORTANT: If you are familiar with XtermSharp, the API surface is similar,
but ALL namespaces use "CodeBrix.Terminal.Engine" instead of "XtermSharp".
The Rune struct and RuneExtensions are placed in the System namespace.

Source Repository: https://github.com/ellisnet/CodeBrix.Terminal
License: MIT License

================================================================================

INSTALLATION
------------
NuGet Package: CodeBrix.Terminal.MitLicenseForever
Authors: Jeremy Ellis
Dependencies: NONE (no dependencies other than the .NET runtime)

Requirements: .NET 10.0 or higher

To add to a .NET 10+ project:

    dotnet add package CodeBrix.Terminal.MitLicenseForever

Or in a .csproj file:

    <PackageReference Include="CodeBrix.Terminal.MitLicenseForever" />

IMPORTANT: The package name is "CodeBrix.Terminal.MitLicenseForever" (not just
"CodeBrix.Terminal"). Always use this full package name when installing.

================================================================================

KEY NAMESPACES
--------------

    using CodeBrix.Terminal.Engine;        // Core: Terminal, Buffer, CharData, TerminalOptions
    using CodeBrix.Terminal.Engine.Utils;   // Utility: CircularList, RuneExt
    using CodeBrix.Terminal.Text;           // Unicode: ustring, Utf8, Unicode

NOTE: The Rune struct and RuneExtensions are in the System namespace for
convenience. They are available without a using statement.

================================================================================

SUPPORTED FEATURES
-------------------
  - ANSI/DEC escape sequence parsing (VT100/VT220/VT400/xterm)
  - Terminal buffer management with scrollback history
  - Cursor positioning and manipulation
  - Scroll region management
  - Character and line insertion/deletion
  - Text attributes: bold, italic, underline, blink, inverse, dim, invisible,
    crossedout
  - Color support: 8-color, 16-color (bright), 256-color palette
  - Device status and terminal mode reporting (DA1, DA2, DSR, DECRQSS)
  - Mouse tracking protocols (X10, VT200, ButtonEventTracking, AnyEvent)
  - Mouse protocol encodings (X10, UTF8, SGR, URXVT)
  - Alternate screen buffers
  - Tab stop management
  - PTY fork and exec on Unix/macOS
  - Unicode character classification and case conversion
  - UTF-8 string handling with Rune (code point) support
  - Column width calculation for terminal display
  - Search and selection services
  - Terminal resize with reflow strategies

================================================================================

CORE API REFERENCE
==================

TERMINAL CLASS
--------------
The main entry point. Create a Terminal, feed it data, read the buffer.

Constructor:

    Terminal(ITerminalDelegate delegate, TerminalOptions options)

    // delegate can be null for basic usage
    // options specifies Cols, Rows, and other terminal settings

Constants:

    Terminal.MINIMUM_COLS = 2
    Terminal.MINIMUM_ROWS = 1

Key Properties:

    ITerminalDelegate Delegate { get; }
    Buffer Buffer { get; }                // Active buffer
    BufferSet Buffers { get; }            // Normal + Alt buffers
    TerminalOptions Options { get; }
    string Title { get; }
    string IconTitle { get; }
    int Cols { get; }
    int Rows { get; }
    bool MarginMode { get; set; }
    bool OriginMode { get; set; }
    bool Wraparound { get; set; }
    bool ReverseWraparound { get; set; }
    MouseMode MouseMode { get; set; }
    MouseProtocolEncoding MouseProtocol { get; set; }
    bool ApplicationCursor { get; set; }
    bool ApplicationKeypad { get; set; }
    bool InsertMode { get; set; }
    int CurAttr { get; set; }            // Current character attribute
    ControlCodes ControlCodes { get; }

Data Input Methods:

    void Feed(string text)                // Feed text/escape sequences
    void Feed(byte[] data)                // Feed raw bytes
    void Feed(byte[] data, int length)    // Feed raw bytes with length
    void Feed(IntPtr data, int length)    // Feed from unmanaged memory

Display Management:

    void Refresh(int startRow, int endRow)
    void UpdateRange(int y)
    void ClearUpdateRange()
    void ScrollLines(int lines)
    void Scroll(bool isWrapped)

Cursor Operations:

    void SetCursor(int col, int row)
    void ShowCursor(bool show)
    void SaveCursor(int[] pars)
    void RestoreCursor(int[] pars)

Terminal Control:

    void Reset()
    void SoftReset()
    void Resize(int cols, int rows)
    void SetScrollRegion(int top, int bottom)
    void SetCursorStyle(CursorStyle style)

Title Management:

    void SetTitle(string title)
    void PushTitle()
    void PopTitle()
    void SetIconTitle(string title)
    void PushIconTitle()
    void PopIconTitle()

Response/Output:

    void SendResponse(string text)
    void SendResponse(byte[] data)

Events:

    event Action Scrolled
    event Action<string> DataEmitted
    event Action LineFeedEvent

Static Methods:

    static string[] GetEnvironmentVariables()

================================================================================

TERMINAL OPTIONS
-----------------

    var options = new TerminalOptions
    {
        Cols = 80,               // Default: 80
        Rows = 25,               // Default: 25
        ConvertEol = true,       // Default: true
        CursorBlink = false,     // Default: false
        TermName = "xterm",      // Default: "xterm"
        CursorStyle = CursorStyle.BlinkBlock,
        ScreenReaderMode = false,
    };

    // Read-only defaults:
    options.Scrollback    // Default: 1000
    options.TabStopWidth  // Default: 8

CursorStyle enum:
    BlinkBlock, SteadyBlock, BlinkUnderline, SteadyUnderline,
    BlinkingBar, SteadyBar

================================================================================

TERMINAL DELEGATE
------------------
Implement ITerminalDelegate to handle terminal events, or use
SimpleTerminalDelegate as a base class.

ITerminalDelegate interface:

    void ShowCursor(Terminal source)
    void SetTerminalTitle(Terminal source, string title)
    void SetTerminalIconTitle(Terminal source, string title)
    void SizeChanged(Terminal source)
    void Send(byte[] data)
    string WindowCommand(Terminal source, WindowManipulationCommand command,
                         params int[] args)
    bool IsProcessTrusted()

SimpleTerminalDelegate provides virtual no-op implementations of all methods.

================================================================================

BUFFER AND CHARACTER DATA
==========================

Buffer class - represents the terminal screen content:

    Buffer buffer = terminal.Buffer;

    int cols = buffer.Cols;
    int rows = buffer.Rows;
    int cursorX = buffer.X;              // Cursor X position
    int cursorY = buffer.Y;              // Cursor Y position
    int yBase = buffer.YBase;            // Scroll base
    int yDisp = buffer.YDisp;            // Display position
    CircularList<BufferLine> lines = buffer.Lines;

    // Scroll region
    int scrollTop = buffer.ScrollTop;
    int scrollBottom = buffer.ScrollBottom;
    int marginLeft = buffer.MarginLeft;
    int marginRight = buffer.MarginRight;

    // Methods
    CharData ch = buffer.GetChar(col, row);
    buffer.Clear();
    buffer.Resize(newCols, newRows);
    buffer.SetMargins(left, right);
    string text = buffer.TranslateBufferLineToString(lineIndex, trimRight,
                                                      startCol, endCol);

BufferLine class - a single line of characters:

    BufferLine line = buffer.Lines[buffer.YBase + row];
    int length = line.Length;
    CharData ch = line[col];             // Indexer access
    bool wrapped = line.IsWrapped;

    line.InsertCells(pos, n, rightMargin, fillCharData);
    line.DeleteCells(pos, n, rightMargin, fillCharData);
    line.ReplaceCells(start, end, fillCharData);
    string text = line.TranslateToString(trimRight, startCol, endCol);

CharData struct - a single character with attributes:

    int attribute = ch.Attribute;        // Encoded styling (fg, bg, flags)
    Rune rune = ch.Rune;                // The Unicode character
    int width = ch.Width;               // Display width (1 or 2)
    int code = ch.Code;                 // Unicode code point

    // Static instances
    CharData.Null                        // Empty character
    CharData.WhiteSpace                  // Space character

    // Attribute decoding:
    int fg = (attribute >> 9) & 0x1ff;   // Foreground color index
    int flags = attribute >> 18;          // FLAGS: BOLD=1, UNDERLINE=2, etc.

FLAGS enum (text styling):
    BOLD = 1, UNDERLINE = 2, BLINK = 4, INVERSE = 8, INVISIBLE = 16,
    DIM = 32, ITALIC = 64, CrossedOut = 128

BufferSet class - manages Normal and Alt screen buffers:

    Buffer normal = terminal.Buffers.Normal;
    Buffer alt = terminal.Buffers.Alt;
    Buffer active = terminal.Buffers.Active;
    bool isAlt = terminal.Buffers.IsAlternateBuffer;

    terminal.Buffers.ActivateAltBuffer(fillAttr);
    terminal.Buffers.ActivateNormalBuffer(clearAlt);

================================================================================

COLOR MODEL
============

Color class:

    byte red = color.Red;
    byte green = color.Green;
    byte blue = color.Blue;

    // 256-color palette
    List<Color> colors = Color.DefaultAnsiColors;
    // [0-7]     = standard colors (black, red, green, yellow, blue, magenta,
    //             cyan, white)
    // [8-15]    = bright colors
    // [16-231]  = 6x6x6 color cube
    // [232-255] = grayscale ramp

    Color.DefaultForeground  // white (0xff, 0xff, 0xff)
    Color.DefaultBackground  // black (0, 0, 0)

================================================================================

MOUSE TRACKING
===============

MouseMode enum:
    Off, X10, VT200, ButtonEventTracking, AnyEvent

MouseProtocolEncoding enum:
    X10, UTF8, SGR, URXVT

Extension methods on MouseMode:

    bool sendsPress = mouseMode.SendButtonPress();
    bool sendsRelease = mouseMode.SendButtonRelease();
    bool sendsTracking = mouseMode.SendButtonTracking();
    bool sendsMotion = mouseMode.SendMotionEvent();
    bool sendsModifiers = mouseMode.SendsModifiers();

Terminal mouse methods:

    int flags = terminal.EncodeMouseButton(button, release, shift, meta, ctrl);
    terminal.SendEvent(buttonFlags, x, y);
    terminal.SendMouseMotion(buttonFlags, x, y);

================================================================================

ESCAPE SEQUENCE PARSER
=======================

The EscapeSequenceParser handles low-level VT/xterm escape sequence parsing.
Most users will interact with Terminal.Feed() which uses this internally.

For advanced customization:

    var parser = new EscapeSequenceParser();

    // Register handlers
    parser.SetCsiHandler('H', (pars, collect) => { /* cursor position */ });
    parser.SetOscHandler(0, (data) => { /* set title */ });
    parser.SetEscHandler("c", (collect, flag) => { /* full reset */ });
    parser.SetExecuteHandler(0x0d, () => { /* carriage return */ });
    parser.SetDcsHandler("q", dcsHandler);

    // Fallback handlers
    parser.SetCsiHandlerFallback((collect, pars, flag) => { });
    parser.SetEscHandlerFallback((collect, flag) => { });

ParserState enum:
    Ground, Escape, EscapeIntermediate, CsiEntry, CsiParam,
    CsiIntermediate, CsiIgnore, OscString, DcsEntry, DcsParam,
    DcsIgnore, DcsIntermediate, DcsPassthrough, SosPmApcString

================================================================================

SEARCH AND SELECTION
=====================

SearchService:

    var search = new SearchService(terminal);
    var snapshot = search.GetSnapshot();

    int count = snapshot.FindText("search term");
    snapshot.FindNext();
    snapshot.FindPrevious();

    string lastSearch = snapshot.LastSearch;
    SearchResult[] results = snapshot.LastSearchResults;

SelectionService:

    var selection = new SelectionService(terminal);

    selection.StartSelection(row, col);
    selection.DragExtend(row, col);
    selection.SelectAll();
    selection.SelectRow(row);
    selection.SelectWordOrExpression(col, row);

    string text = selection.GetSelectedText();
    bool active = selection.Active;

================================================================================

UNICODE TEXT SUPPORT (CodeBrix.Terminal.Text)
=============================================

ustring class - Unicode-aware string type:

    // Creation
    ustring s = ustring.Make("Hello");
    ustring s = ustring.Make(new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f });
    ustring s = ustring.Make(new Rune('A'));
    ustring s = "Hello";                 // Implicit conversion from string

    // Properties
    int byteLen = s.Length;              // Byte length
    int runeCount = s.RuneCount;         // Character count
    int width = s.ConsoleWidth;          // Display width
    bool empty = s.IsEmpty;

    // Slicing
    ustring sub = s[2, 5];              // Byte slice [start, end)
    ustring sub = s.Substring(2, 3);    // Byte-based substring
    ustring sub = s.RuneSubstring(1, 3);// Rune-based substring

    // Search
    int idx = s.IndexOf("llo");
    bool has = s.Contains("llo");
    bool starts = s.StartsWith("He");
    bool ends = s.EndsWith("lo");
    int count = s.Count("l");

    // Case conversion
    ustring upper = s.ToUpper();
    ustring lower = s.ToLower();
    ustring title = s.Title();           // Word-boundary-aware

    // Splitting and joining
    ustring[] parts = s.Split(" ");
    ustring joined = ustring.Join(", ", parts);
    ustring combined = ustring.Concat(s1, s2);

    // Trimming
    ustring trimmed = s.TrimSpace();

    // Conversion
    string str = s.ToString();
    byte[] bytes = s.ToByteArray();
    uint[] runes = s.ToRunes();

Rune struct (in System namespace) - a Unicode code point:

    Rune r = new Rune('A');
    Rune r = new Rune(0x1F600);          // Emoji
    Rune r = (Rune)'A';                  // Implicit from char/int/byte/uint

    uint value = r.Value;
    bool valid = r.IsValid;
    int width = Rune.ColumnWidth(r);     // 0 (non-spacing), 1, or 2 (wide)

    // Classification
    bool digit = r.IsDigit();
    bool letter = r.IsLetter();
    bool upper = r.IsUpper();
    bool lower = r.IsLower();
    bool space = r.IsSpace();
    bool punct = r.IsPunct();
    bool control = r.IsControl();
    bool graphic = r.IsGraphic();
    bool print = r.IsPrint();

    // Case conversion
    Rune u = r.ToUpper();
    Rune l = r.ToLower();
    Rune t = r.ToTitle();

    // Surrogate pair handling
    Rune combined = Rune.EncodeSurrogatePair(high, low);
    (uint high, uint low) = Rune.DecodeSurrogatePair(rune);

Utf8 static class - UTF-8 encoding utilities:

    bool full = Utf8.FullRune(bytes);
    (uint rune, int size) = Utf8.DecodeRune(buffer, start, n);
    int len = Utf8.RuneLen(rune);
    int count = Utf8.RuneCount(buffer, offset, count);
    bool valid = Utf8.Valid(buffer);
    int encLen = Utf8.EncodeRune(rune, dest, offset);

Unicode static class - character classification:

    bool digit = Unicode.IsDigit(codepoint);
    bool letter = Unicode.IsLetter(codepoint);
    uint upper = Unicode.ToUpper(codepoint);
    uint lower = Unicode.ToLower(codepoint);
    uint folded = Unicode.SimpleFold(codepoint);

    // Unicode version: 15.0.0

================================================================================

PTY SUPPORT (Unix/macOS only)
==============================

    using CodeBrix.Terminal.Engine;

    var winSize = new UnixWindowSize { row = 25, col = 80 };
    int pid = Pty.ForkAndExec("/bin/bash", args, env, out int master, winSize);
    Pty.SetWinSize(master, ref winSize);
    Pty.AvailableBytes(master, ref size);

IMPORTANT: Pty.ForkAndExec is ONLY available on Unix/macOS. It will not work
on Windows. For Windows process management, use System.Diagnostics.Process.

================================================================================

PREDEFINED ESCAPE SEQUENCES
=============================

    using CodeBrix.Terminal.Engine;

    // Common key escape sequences (byte arrays)
    EscapeSequences.CmdNewline       // Enter/Return
    EscapeSequences.CmdRet           // Carriage return
    EscapeSequences.CmdEsc           // Escape key
    EscapeSequences.CmdDel           // Delete
    EscapeSequences.CmdDelKey        // Delete key
    EscapeSequences.CmdTab           // Tab
    EscapeSequences.CmdBackTab       // Shift+Tab
    EscapeSequences.CmdPageUp        // Page Up
    EscapeSequences.CmdPageDown      // Page Down

    // Arrow keys (application mode vs normal mode)
    EscapeSequences.MoveUpApp / MoveUpNormal
    EscapeSequences.MoveDownApp / MoveDownNormal
    EscapeSequences.MoveLeftApp / MoveLeftNormal
    EscapeSequences.MoveRightApp / MoveRightNormal
    EscapeSequences.MoveHomeApp / MoveHomeNormal
    EscapeSequences.MoveEndApp / MoveEndNormal

    // Function keys
    EscapeSequences.CmdF[0..11]      // F1 through F12

================================================================================

COMPLETE EXAMPLES
=================

Example 1: Basic Terminal - Create and Read Buffer
----------------------------------------------------
    using CodeBrix.Terminal.Engine;

    // Create a terminal with default options (80x25)
    var terminal = new Terminal(null, new TerminalOptions { Cols = 80, Rows = 25 });

    // Feed escape sequences and text into the terminal
    terminal.Feed("Hello, Terminal!\r\n");
    terminal.Feed("\x1b[1;31m");  // Set bold red foreground
    terminal.Feed("Red bold text\r\n");
    terminal.Feed("\x1b[0m");     // Reset attributes

    // Read the terminal buffer
    var line = terminal.Buffer.Lines[terminal.Buffer.YBase + 0];
    for (int col = 0; col < terminal.Cols; col++)
    {
        var ch = line[col];
        if (ch.Code != 0)
            Console.Write((char)ch.Code);
    }

Example 2: Scroll Regions
---------------------------
    using CodeBrix.Terminal.Engine;

    var terminal = new Terminal(null, new TerminalOptions { Cols = 80, Rows = 25 });

    // Set a scroll region (rows 5-20)
    terminal.Feed("\x1b[5;20r");

    // Write content, and scrolling will be confined to the region
    for (int i = 0; i < 30; i++)
    {
        terminal.Feed($"Line {i}\r\n");
    }

Example 3: Custom Terminal Delegate
--------------------------------------
    using CodeBrix.Terminal.Engine;

    public class MyTerminalDelegate : SimpleTerminalDelegate
    {
        public override void Send(byte[] data, int start, int length)
        {
            Console.WriteLine($"Terminal sent {length} bytes");
        }

        public override void SizeChanged(Terminal source)
        {
            Console.WriteLine($"Terminal resized to {source.Cols}x{source.Rows}");
        }
    }

    var terminal = new Terminal(new MyTerminalDelegate(),
        new TerminalOptions { Cols = 120, Rows = 40 });

Example 4: Reading Buffer with Attribute Decoding
----------------------------------------------------
    using CodeBrix.Terminal.Engine;

    var terminal = new Terminal(null, new TerminalOptions { Cols = 120, Rows = 50 });
    terminal.Feed("Hello\r\n");

    // Read buffer for rendering
    for (int row = 0; row < terminal.Rows; row++)
    {
        var line = terminal.Buffer.Lines[terminal.Buffer.YBase + row];
        for (int col = 0; col < terminal.Cols; col++)
        {
            var ch = line[col];
            if (ch.Code == 0) continue;

            // Decode attributes
            int fg = (ch.Attribute >> 9) & 0x1ff;    // Foreground color index
            int flags = ch.Attribute >> 18;            // Styling flags
            bool bold = (flags & (int)FLAGS.BOLD) != 0;
            bool italic = (flags & (int)FLAGS.ITALIC) != 0;

            // Use ch.Rune, fg, bold, italic for rendering
        }
    }

Example 5: Unicode Text Processing
--------------------------------------
    using CodeBrix.Terminal.Text;

    ustring text = "Hello, World! \u00e9\u00e8\u00ea";  // Accented chars
    int runeCount = text.RuneCount;
    int displayWidth = text.ConsoleWidth;

    // Case conversion
    ustring upper = text.ToUpper();
    ustring lower = text.ToLower();

    // Rune iteration
    foreach (var (index, rune) in text.Range())
    {
        bool isLetter = new Rune(rune).IsLetter();
        int width = Rune.ColumnWidth(new Rune(rune));
    }

================================================================================

COMMON USING STATEMENT COMBINATIONS
=====================================

For terminal emulation:

    using CodeBrix.Terminal.Engine;

For Unicode text processing:

    using CodeBrix.Terminal.Text;

For both:

    using CodeBrix.Terminal.Engine;
    using CodeBrix.Terminal.Text;

NOTE: Rune and RuneExtensions are in the System namespace and are available
automatically without a using statement.

================================================================================

WHAT THIS LIBRARY DOES NOT DO
===============================

Do NOT attempt to use CodeBrix.Terminal for the following - it will not work:

  - Providing a GUI/UI terminal control (you must build your own renderer)
  - Running a shell or command interpreter
  - Handling SSH, Telnet, or any network protocol
  - Input handling (keyboard events) - it only processes escape sequence output
  - Process spawning on Windows (Pty.ForkAndExec is Unix/macOS only)
  - TrueColor (24-bit RGB) in the color model (up to 256 colors only)
  - Clipboard integration
  - DEC Locator mouse protocol
  - VT200 Highlight mouse mode (can deadlock terminal)
  - Font rendering or glyph lookup
  - SignalR/networking (the samples demonstrate this separately)
  - Serialization or persistence of terminal state
  - Running on .NET versions below 10.0

This library IS for: emulating a virtual terminal in memory, parsing escape
sequences, managing terminal buffers, and providing Unicode text utilities,
without any dependency on a physical terminal.

================================================================================

MINIMUM VIABLE PROJECT TEMPLATE
=================================

To scaffold a new .NET 10 console project that uses CodeBrix.Terminal:

    dotnet new console -n MyTerminalApp --framework net10.0
    cd MyTerminalApp
    dotnet add package CodeBrix.Terminal.MitLicenseForever

Then in Program.cs:

    using CodeBrix.Terminal.Engine;

    var terminal = new Terminal(null, new TerminalOptions { Cols = 80, Rows = 25 });
    terminal.Feed("Hello, Terminal!\r\n");
    terminal.Feed("\x1b[1;32mGreen text\x1b[0m\r\n");

    // Read back the buffer
    for (int row = 0; row < 3; row++)
    {
        var line = terminal.Buffer.Lines[terminal.Buffer.YBase + row];
        Console.WriteLine(line.TranslateToString(trimRight: true));
    }

Build and run:

    dotnet build
    dotnet run

================================================================================

PERFORMANCE TIPS FOR CODING AGENTS
====================================

1. USE Feed(string) FOR SIMPLE TEXT: The string overload handles encoding.
   Use Feed(byte[]) only when you have raw terminal data.

2. USE TranslateToString: BufferLine.TranslateToString() is the easiest way
   to extract text content from a buffer line. Use trimRight: true to strip
   trailing whitespace.

3. READ YBase + row: When reading visible buffer content, always use
   terminal.Buffer.Lines[terminal.Buffer.YBase + row], not just Lines[row].
   YBase accounts for scrollback.

4. HANDLE WIDE CHARACTERS: CJK and emoji characters have Width = 2. Account
   for this when calculating column positions.

5. USE SimpleTerminalDelegate: If you only need to handle a few events,
   extend SimpleTerminalDelegate instead of implementing all ITerminalDelegate
   methods.

6. BATCH Feed() CALLS: For better performance, concatenate text before calling
   Feed() rather than calling it character by character.

7. USE Rune.ColumnWidth(): When calculating display widths, use
   Rune.ColumnWidth() instead of assuming all characters are width 1.

================================================================================

COMMON PITFALLS TO AVOID
=========================

1. DO NOT confuse the NuGet package name with the namespace.
   - Package: CodeBrix.Terminal.MitLicenseForever
   - Namespaces: CodeBrix.Terminal.Engine, CodeBrix.Terminal.Text

2. DO NOT use Pty.ForkAndExec on Windows. It only works on Unix/macOS.

3. DO NOT target .NET versions below 10.0. This library requires .NET 10+.

4. DO NOT forget that the Rune struct is in the System namespace, not in
   CodeBrix.Terminal.Text. You do not need a using statement for it.

5. DO NOT assume all characters are width 1. CJK characters and emoji are
   width 2. Use Rune.ColumnWidth() or CharData.Width.

6. DO NOT read Lines[row] directly. Use Lines[Buffer.YBase + row] to
   account for scrollback history.

7. DO NOT confuse byte length with character count. ustring.Length is bytes,
   ustring.RuneCount is characters.

8. DO NOT forget that terminal coordinates are 0-based in the buffer, but
   escape sequences like CUP (\x1b[row;colH) use 1-based coordinates.

================================================================================

DEEPER LEARNING: TEST FILE CROSS-REFERENCES
=============================================

The CodeBrix.Terminal source repository contains extensive test files.
If the documentation above is not sufficient, fetch and read the relevant file:

    https://github.com/ellisnet/CodeBrix.Terminal
    Path: tests/CodeBrix.Terminal.Engine.Tests/
    Path: tests/CodeBrix.Terminal.Text.Tests/

Feature-to-test-file mapping:

  Terminal basics (initialization, cursor up/down, scroll regions, CSI):
    -> tests/CodeBrix.Terminal.Engine.Tests/BaseTerminalTests.cs

  Backspace behavior (wrapping, margins, reverse wrap):
    -> tests/CodeBrix.Terminal.Engine.Tests/EscapeSequence/BsTests.cs

  Cursor backward tabulation:
    -> tests/CodeBrix.Terminal.Engine.Tests/EscapeSequence/CbtTests.cs

  Carriage return (margins, origin mode):
    -> tests/CodeBrix.Terminal.Engine.Tests/EscapeSequence/CrTests.cs

  Cursor position (CUP, origin mode, bounds):
    -> tests/CodeBrix.Terminal.Engine.Tests/EscapeSequence/CupTests.cs

  DEC copy rectangular area:
    -> tests/CodeBrix.Terminal.Engine.Tests/EscapeSequence/DeccraTests.cs

  DEC request status string:
    -> tests/CodeBrix.Terminal.Engine.Tests/EscapeSequence/DecrqssTests.cs

  Device status reports and device attributes (DA1, DA2, DSR):
    -> tests/CodeBrix.Terminal.Engine.Tests/EscapeSequence/DeviceStatusTests.cs

  Erase operations (ED, EL, ECH, DECERA, DECSERA):
    -> tests/CodeBrix.Terminal.Engine.Tests/EscapeSequence/EraseTests.cs

  Fill rectangular area (DECFRA):
    -> tests/CodeBrix.Terminal.Engine.Tests/EscapeSequence/FillRectangleTests.cs

  Insert/delete characters and columns (ICH, DCH, DECIC, DECDC):
    -> tests/CodeBrix.Terminal.Engine.Tests/EscapeSequence/InsertDeleteCharTests.cs

  Insert/delete lines (IL, DL):
    -> tests/CodeBrix.Terminal.Engine.Tests/EscapeSequence/InsertDeleteLineTests.cs

  Scroll up/down, index, reverse index, viewport scrolling:
    -> tests/CodeBrix.Terminal.Engine.Tests/EscapeSequence/ScrollTests.cs

  Select graphic rendition (bold, italic, colors, resets):
    -> tests/CodeBrix.Terminal.Engine.Tests/EscapeSequence/SgrTests.cs

  Rune struct (column width, surrogate pairs, classification):
    -> tests/CodeBrix.Terminal.Text.Tests/RuneTests.cs

  Rune classification and case conversion:
    -> tests/CodeBrix.Terminal.Text.Tests/RuneClassificationTests.cs

  ustring operations (search, compare, slice, trim, replace):
    -> tests/CodeBrix.Terminal.Text.Tests/UStringTests.cs

  ustring additional (case conversion, concat, join, make):
    -> tests/CodeBrix.Terminal.Text.Tests/UStringAdditionalTests.cs

  Unicode classification and case conversion:
    -> tests/CodeBrix.Terminal.Text.Tests/UnicodeTests.cs

  UTF-8 encoding/decoding:
    -> tests/CodeBrix.Terminal.Text.Tests/Utf8Tests.cs

  Utf8/ustring overloads and parity:
    -> tests/CodeBrix.Terminal.Text.Tests/Utf8UStringOverloadTests.cs

HOW TO USE: Fetch the raw file content from GitHub using a URL like:
    https://raw.githubusercontent.com/ellisnet/CodeBrix.Terminal/main/{path}
For example:
    https://raw.githubusercontent.com/ellisnet/CodeBrix.Terminal/main/tests/CodeBrix.Terminal.Engine.Tests/EscapeSequence/SgrTests.cs

================================================================================

QUICK REFERENCE CARD
=====================

Install:         dotnet add package CodeBrix.Terminal.MitLicenseForever
Engine NS:       using CodeBrix.Terminal.Engine;
Text NS:         using CodeBrix.Terminal.Text;
Rune NS:         System (no using needed)
Create terminal: new Terminal(null, new TerminalOptions { Cols = 80, Rows = 25 })
Feed text:       terminal.Feed("text\r\n")
Feed escape:     terminal.Feed("\x1b[1;31m")  // Bold red
Reset attrs:     terminal.Feed("\x1b[0m")
Read buffer:     terminal.Buffer.Lines[terminal.Buffer.YBase + row]
Read char:       line[col].Code, line[col].Rune, line[col].Attribute
Line to string:  line.TranslateToString(trimRight: true)
Resize:          terminal.Resize(newCols, newRows)
Scroll region:   terminal.Feed("\x1b[top;bottomr")
Rune width:      Rune.ColumnWidth(rune)
Make ustring:    ustring.Make("text") or implicit from string
Rune count:      ustring.RuneCount
Console width:   ustring.ConsoleWidth
Delegate:        extend SimpleTerminalDelegate

Dependencies:    NONE (zero external dependencies)
File format:     N/A (in-memory terminal emulation)
Target:          .NET 10.0+
License:         MIT

================================================================================
