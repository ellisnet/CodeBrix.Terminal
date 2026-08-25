================================================================================
AGENT-README: CodeBrix.Terminal
A Guide for AI Coding Agents -- CONSUMING the
CodeBrix.Terminal.MitLicenseForever NuGet package
================================================================================

OVERVIEW
========
CodeBrix.Terminal is an in-memory terminal emulation engine with Unicode text
support. It provides a virtual terminal (VT100/VT220/VT400/xterm-compatible)
with a complete ANSI/DEC escape-sequence parser, terminal buffer and
scrollback management, mouse-tracking protocols, search and selection
services, keyboard-to-VT input encoding, and a full Unicode text toolkit
(UTF-8 strings, code points, character classification, terminal column
widths).

It is renderer-agnostic and headless: you feed it bytes or text, and you read
the resulting character buffer to paint it however you like. It has ZERO
NuGet dependencies beyond the .NET runtime itself.

Target framework: .NET 10 or later.

Provenance: the engine is a fork of XtermSharp (by Miguel de Icaza, itself a
port of xterm.js) and the Unicode text utilities derive from NStack 1.1.1.
Every engine namespace was renamed from "XtermSharp" to
"CodeBrix.Terminal.Engine" and every text namespace from "NStack" to
"CodeBrix.Terminal.Text". Do NOT write "using XtermSharp;" or "using NStack;".
The API shape is similar to the upstream projects, but several members differ
(notably, the terminal mode properties are read-only to consumers here), so
verify against this document rather than upstream memory.

Source repository: https://github.com/ellisnet/CodeBrix.Terminal


INSTALLATION
============
PackageId: CodeBrix.Terminal.MitLicenseForever

    dotnet add package CodeBrix.Terminal.MitLicenseForever

or in a .csproj:

    <PackageReference Include="CodeBrix.Terminal.MitLicenseForever" />

IMPORTANT: the package id is CodeBrix.Terminal.MitLicenseForever, NOT
"CodeBrix.Terminal". The assembly and the namespace roots are
CodeBrix.Terminal.

NuGet dependencies: NONE. The package pulls in nothing but the .NET runtime.
(It does use System.Drawing.Point from the shared framework for selection and
search coordinates -- that is part of .NET, not a package reference.)

License: MIT. The package requires license acceptance.

Requirements and limits:
  - .NET 10 or later. There is no netstandard or .NET Framework target.
  - The assembly is compiled with AllowUnsafeBlocks, and some parser entry
    points take byte pointers. You do NOT need <AllowUnsafeBlocks> in your own
    project unless you implement IDcsHandler or a PrintHandler yourself,
    both of which have unsafe members.
  - Pty.ForkAndExec / SetWinSize / AvailableBytes are P/Invokes into libc and
    work on Unix and macOS only.
  - Everything else is platform-neutral and runs anywhere .NET 10 runs.


KEY NAMESPACES / USINGS
=======================

    using CodeBrix.Terminal.Engine;   // Terminal, TerminalOptions,
                                      //   ITerminalDelegate, Buffer,
                                      //   BufferLine, BufferSet, CharData,
                                      //   CharacterAttribute, FLAGS, Color,
                                      //   EscapeSequenceParser, SearchService,
                                      //   SelectionService, Pty,
                                      //   TerminalKeyEncoder, CircularList<T>,
                                      //   RuneExt, RuneHelper, Renderer.
    using CodeBrix.Terminal.Text;     // ustring, Utf8, Unicode.
    using System.Drawing;             // Point -- SelectionService.Start/End and
                                      //   SearchSnapshot.SearchResult use it.

The Rune struct and RuneExtensions are declared in the System namespace, so
they need no using of their own.

For terminal emulation only:            using CodeBrix.Terminal.Engine;
For Unicode text processing only:       using CodeBrix.Terminal.Text;
For both, add both. Add System.Drawing when you touch selection or search
coordinates.

NAMESPACE GOTCHAS

  - There is NO "CodeBrix.Terminal.Engine.Utils" namespace. Engine/Utils is a
    FOLDER; CircularList<T> and RuneExt both declare
    `namespace CodeBrix.Terminal.Engine;`. Writing
    `using CodeBrix.Terminal.Engine.Utils;` is a CS0246 compile error.
    The same is true of Engine/Renderer and Engine/InputHandlers: folders,
    not namespaces (see the next point for the two exceptions).
  - CodeBrix.Terminal.Engine.CommandExtensions and
    CodeBrix.Terminal.Engine.CsiCommandExtensions DO exist as namespaces
    (Engine/InputHandlers/*.cs), but every type in them is internal: they hold
    the CSI/mode/status command implementations the input handler uses. There
    is nothing there for a consumer, so do not add usings for them.
  - `Buffer` collides with System.Buffer. If you have both
    `using System;` and `using CodeBrix.Terminal.Engine;` in scope, naming the
    type `Buffer` is CS0104. Alias it:
        using TerminalBuffer = CodeBrix.Terminal.Engine.Buffer;
  - `Color` is CodeBrix.Terminal.Engine.Color here, which is NOT
    System.Drawing.Color. If you add `using System.Drawing;` for Point, the
    name Color becomes ambiguous -- alias whichever one you mean.
  - Several types are NESTED and must be qualified from outside:
    EscapeSequenceParser.CsiHandler / OscHandler / EscHandler /
    ExecuteHandler / PrintHandler, SearchSnapshot.SearchResult,
    ustring.RunePredicate, Unicode.Category / Script / Property / RangeTable /
    SpecialCase / Case, and Rune.Case.


================================================================================

SUPPORTED FEATURES
==================
  - ANSI/DEC escape-sequence parsing (VT100/VT220/VT400/xterm)
  - Terminal buffer management with scrollback history
  - Cursor positioning and manipulation
  - Scroll regions and left/right margins
  - Character, line and column insertion/deletion; rectangular-area
    operations (DECCRA, DECFRA, DECERA, DECSERA)
  - Text attributes: bold, dim, italic, underline, blink, inverse, invisible,
    crossed-out
  - Color: 8-color, 16-color (bright) and the 256-color xterm palette
  - Device status and mode reporting (DA1, DA2, DSR, DECRQSS)
  - Mouse tracking protocols (X10, VT200, ButtonEventTracking, AnyEvent) and
    encodings (X10, UTF8, SGR, URXVT)
  - Alternate screen buffer
  - Tab-stop management with a configurable default width
  - Character-set (SCS) translation tables, including DEC line drawing
  - Window-manipulation commands routed to the host delegate
  - Keyboard-to-VT input encoding, application-cursor aware
  - Terminal resize with reflow (wider and narrower strategies)
  - Search and selection services over the scrollback
  - PTY fork/exec, window-size and available-bytes on Unix/macOS
  - Unicode 15.0.0 classification, case conversion and case folding
  - UTF-8 string handling with Rune (code point) support and terminal column
    width calculation


================================================================================

CORE API REFERENCE
==================

TERMINAL CLASS
--------------
The main entry point. Create a Terminal, feed it data, read the buffer.

Constructor:

    Terminal(ITerminalDelegate terminalDelegate = null,
             TerminalOptions options = null)

    Both arguments are optional; a null delegate is fine for headless use.
    Options carry Cols, Rows and the rest of the terminal settings.
    Dimensions are clamped to the internal minimums (2 cols x 1 row).

Core properties:

    ITerminalDelegate Delegate { get; }
    Buffer Buffer { get; }                  // the ACTIVE buffer
    BufferSet Buffers { get; }              // Normal + Alt
    TerminalOptions Options { get; }
    ControlCodes ControlCodes { get; }
    string Title { get; }
    string IconTitle { get; }
    int Cols { get; }
    int Rows { get; }
    Dictionary<byte, string> Charset { get; set; }   // active SCS table

Mode properties -- READ-ONLY to consumers (their setters are internal). The
terminal updates them itself in response to the escape sequences you feed it:

    bool MarginMode { get; }
    bool OriginMode { get; }
    bool Wraparound { get; }
    bool ReverseWraparound { get; }
    bool ApplicationCursor { get; }
    bool ApplicationKeypad { get; }
    bool Allow80To132 { get; }
    bool SendFocus { get; }
    bool CursorHidden { get; }
    bool BracketedPasteMode { get; }
    int SavedCols { get; }
    MouseMode MouseMode { get; }
    MouseProtocolEncoding MouseProtocol { get; }

Writable public FIELDS (not properties):

    bool InsertMode
    int CurAttr                             // the current packed attribute

Data input:

    void Feed(string text)                  // text and/or escape sequences
    void Feed(byte[] data, int len = -1)    // raw bytes; -1 means "all"
    void Feed(IntPtr data, int len = -1)    // unmanaged memory

Display and damage:

    void Refresh(int startRow, int endRow)
    void GetUpdateRange(out int startY, out int endY)
    void ClearUpdateRange()

Viewport and scrollback:

    void ScrollLines(int disp, bool suppressScrollEvent = false)
                                            // moves the VIEWPORT disp lines
                                            //   (negative = back into
                                            //   history), clamped to
                                            //   [0, YBase]
    void ScrollToBottom()                   // back to live output; a no-op
                                            //   (and no event) when already
                                            //   at the bottom
    bool IsAtBottom { get; }                // Buffer.YDisp == Buffer.YBase

Cursor:

    void SetCursor(int col, int row)        // note the col-then-row order
    void ShowCursor()
    void SaveCursor()
    void RestoreCursor()
    void CursorUp(int rows)
    void CursorDown(int rows)
    void CursorForward(int cols)
    void CursorBackward(int cols)
    void CursorBackwardTab(int tabs)
    void CursorCharAbsolute(int col)
    void RestrictCursor(bool limitCols = true)

Text movement and editing:

    void LineFeed()
    void LineFeedBasic()
    void NextLine()
    void CarriageReturn()
    void Backspace()
    void DeleteChars(int charsToDelete)
    void InsertColumn(int columns)
    void DeleteColumn(int columns)

Terminal control:

    void Reset()                            // full reset (RIS); equivalent to
                                            //   Feed("\x1bc")
    void SoftReset()                        // DECSTR
    void Resize(int cols, int rows)
    void SetScrollRegion(int top, int bottom)
    void SetCursorStyle(CursorStyle style)

Titles: the set/push/pop operations are internal. Titles change in response to
the sequences you feed (OSC 0/1/2 and the xterm title-stack commands). Read
Title / IconTitle, or observe ITerminalDelegate.SetTerminalTitle and
SetTerminalIconTitle.

Responses back to the host:

    void SendResponse(string text)
    void SendResponse(params object[] args) // concatenates string, byte and
                                            //   byte[] fragments into one
                                            //   response

Mouse (see MOUSE TRACKING):

    int EncodeMouseButton(int button, bool release, bool shift, bool meta,
                          bool control)
    void SendEvent(int buttonFlags, int x, int y)
    void SendMouseMotion(int buttonFlags, int x, int y)

Miscellaneous:

    int MatchColor(int r1, int g1, int b1)  // nearest index in the 256 palette
    void EmitLineFeed()
    void Error(string txt, params object[] args)
    void Log(string text, params object[] args)
    static string[] GetEnvironmentVariables(string termName = null)

Events:

    event Action<Terminal, int> Scrolled        // int is the new Buffer.YDisp;
                                                //   fires on every viewport
                                                //   move, from incoming
                                                //   content or from
                                                //   ScrollLines/ScrollToBottom
    event Action<Terminal, string> DataEmitted
    event Action<Terminal> LineFeedEvent


TERMINAL OPTIONS
----------------
    public class TerminalOptions
    {
        public int Cols, Rows;                  // fields; default 80 x 25
        public bool ConvertEol = true, CursorBlink;
        public string TermName;                 // default "xterm"
        public CursorStyle CursorStyle;
        public bool ScreenReaderMode;
        public int? Scrollback { get; set; }    // default 1000 lines
        public int? TabStopWidth { get; set; }  // default 8
    }

    var options = new TerminalOptions
    {
        Cols = 80,
        Rows = 25,
        ConvertEol = true,
        CursorBlink = false,
        TermName = "xterm",
        CursorStyle = CursorStyle.BlinkBlock,
        ScreenReaderMode = false,
    };
    options.Scrollback = 500;
    options.TabStopWidth = 4;

    Scrollback and TabStopWidth are settable at any time, but SET THEM BEFORE
    constructing the Terminal: the buffer sizes itself and lays out tab stops
    from those values at construction.

    ConvertEol interplay: the default (true) treats a bare LF as CRLF on
    input. A host whose data source already emits \r\n (a remote shell over
    SSH, most PTYs) must set ConvertEol = false, or every line break doubles
    into a blank line.

    enum CursorStyle
        BlinkBlock, SteadyBlock, BlinkUnderline, SteadyUnderline,
        BlinkingBar, SteadyBar


TERMINAL DELEGATE
-----------------
    public interface ITerminalDelegate
    {
        void ShowCursor(Terminal source);
        void SetTerminalTitle(Terminal source, string title);
        void SetTerminalIconTitle(Terminal source, string title);
        void SizeChanged(Terminal source);
        void Send(byte[] data);
        string WindowCommand(Terminal source, WindowManipulationCommand command,
                             params int[] args);
        bool IsProcessTrusted();
    }

    public class SimpleTerminalDelegate : ITerminalDelegate
    Provides virtual no-op implementations of all seven members; derive from
    it and override only what you need.

    Send(byte[]) is how the terminal hands you data to write back to the
    process or remote host (device reports, mouse events, responses you
    trigger with SendResponse).

    enum WindowManipulationCommand -- the xterm CSI t operations, passed to
    WindowCommand:
        DeiconifyWindow, IconifyWindow, MoveWindowTo, ResizeWindowTo,
        BringToFront, SendToBack, RefreshWindow, ResizeTo,
        RestoreMaximizedWindow, MaximizeWindow, MaximizeWindowVertically,
        MaximizeWindowHorizontally, UndoFullScreen, SwitchToFullScreen,
        ToggleFullScreen, ReportTerminalState, ReportTerminalPosition,
        ReportTextAreaPosition, ReporttextAreaPixelDimension,
        ReportSizeOfScreenInPixels, ReportCellSizeInPixels,
        ReportTextAreaCharacters, ReportScreenSizeCharacters, ReportIconLabel,
        ReportWindowTitle, ResizeToLines

    Note the spelling of ReporttextAreaPixelDimension (lower-case "t" in
    "text") -- it is spelled that way in the enum.

    Return a string from WindowCommand when the command is a REPORT (the
    Report* members); return null for the rest.


BUFFER AND CHARACTER DATA
-------------------------
Buffer -- the terminal screen plus its scrollback:

    int Cols { get; }
    int Rows { get; }
    Terminal Terminal { get; }
    CircularList<BufferLine> Lines { get; }
    int X;                                  // cursor column (field)
    int Y { get; set; }                     // cursor row
    int YBase, YDisp;                       // fields: live-screen base and
                                            //   viewport position
    int SavedX, SavedY, SavedAttr;          // fields
    int ScrollTop { get; set; }
    int ScrollBottom { get; set; }
    int MarginLeft { get; }
    int MarginRight { get; }
    bool HasScrollback { get; }
    bool IsCursorInViewport { get; }

    CharData GetChar(int col, int row)
    BufferLine GetBlankLine(int attribute, bool isWrapped = false)
    void Clear()
    void Resize(int newCols, int newRows)
    void SetMargins(int left, int right)
    void SaveCursor(int curAttr)
    int RestoreCursor()
    void FillViewportRows(int? attribute = null)
    ustring TranslateBufferLineToString(int lineIndex, bool trimRight,
                                        int startCol = 0, int endCol = -1)

    Tab stops:
    void SetupTabStops(int index = -1)
    void TabSet(int pos)
    void ClearStop(int pos)
    void ClearTabStops()
    int PreviousTabStop(int index = -1)
    int NextTabStop(int index = -1)

    TranslateBufferLineToString returns a ustring -- call .ToString() when you
    need a System.String.

BufferLine -- one line of cells:

    int Length { get; }                     // cell count
    bool IsWrapped;                         // field
    CharData this[int idx] { get; set; }
    BufferLine(int cols, CharData? fillCharData, bool isWrapped = false)
    BufferLine(BufferLine other)            // copy constructor

    int GetWidth(int index)
    bool HasContent(int index)
    bool HasAnyContent()
    int GetTrimmedLength()
    void InsertCells(int pos, int n, int rightMargin, CharData fillCharData)
    void DeleteCells(int pos, int n, int rightMargin, CharData fillCharData)
    void ReplaceCells(int start, int end, CharData fillCharData)
    void Resize(int cols, CharData fillCharData)
    void Fill(CharData fillCharData)
    void CopyFrom(BufferLine line)
    void CopyCellsFrom(BufferLine src, int srcCol, int dstCol, int len)
    ustring TranslateToString(bool trimRight = false, int startCol = 0,
                              int endCol = -1)

CharData (struct) -- one cell:

    int Attribute;                          // packed styling (fg, bg, flags)
    Rune Rune;                              // the code point
    int Width;                              // display width: 0, 1 or 2
                                            //   (0 = the continuation cell of
                                            //   a wide character)
    int Code;                               // the code point as an int

    CharData(int attribute, Rune rune, int width, int code)
    CharData(int attribute)

    bool IsBlank { get; }                   // never-written / erased cell:
                                            //   paint background only. Its
                                            //   Rune is U+0200, NOT a space.
    bool IsNullChar()
    bool MatchesRune(Rune rune)
    bool MatchesRune(CharData chr)

    const int DefaultAttr, InvertedAttr
    static CharData Null, WhiteSpace, LeftBrace, RightBrace, LeftBracket,
                    RightBracket, LeftParenthesis, RightParenthesis, Period

BufferSet -- the normal and alternate screens:

    Buffer Normal { get; }
    Buffer Alt { get; }
    Buffer Active { get; }
    bool IsAlternateBuffer { get; }         // Active == Alt
    event Action<Buffer, Buffer> Activated; // (before, after)
    void ActivateNormalBuffer(bool clearAlt)
    void ActivateAltBuffer(int? fillAttr)
    void Resize(int newColumns, int newRows)
    void SetupTabStops(int index = -1)


ATTRIBUTE PACKING
-----------------
A cell's styling is a single packed int. Decode it with CharacterAttribute --
do not hand-roll the shifts.

    public readonly struct UnpackedAttribute
    {
        public int Foreground { get; }      // palette index or a sentinel
        public int Background { get; }      // palette index or a sentinel
        public FLAGS Flags { get; }
        public UnpackedAttribute(int foreground, int background, FLAGS flags);
        public void Deconstruct(out int foreground, out int background,
                                out FLAGS flags);
    }

    public static class CharacterAttribute
    {
        public const int DefaultColorIndex = 256;
        public const int InvertedDefaultColorIndex = 257;
        public static UnpackedAttribute Unpack(int attribute);
        public static string ToSGR(int attribute);   // back to an SGR string
    }

    // Because UnpackedAttribute deconstructs, this is the idiomatic call:
    var (fg, bg, flags) = CharacterAttribute.Unpack(cell.Attribute);

    The packing itself (if you ever need it): foreground is
    (attribute >> 9) & 0x1ff, background is attribute & 0x1ff, and the flags
    are (FLAGS)(attribute >> 18).

    [Flags] enum FLAGS
        BOLD = 1, UNDERLINE = 2, BLINK = 4, INVERSE = 8, INVISIBLE = 16,
        DIM = 32, ITALIC = 64, CrossedOut = 128

    Renderer is a nearly-empty public class that exists to hold the two
    sentinel constants the packing uses:

    public class Renderer
    {
        public const int DefaultColor = 256;
        public const int InvertedDefaultColor = 257;
    }

    Prefer CharacterAttribute.DefaultColorIndex /
    InvertedDefaultColorIndex in your own code -- they are the same values and
    they say what they mean.


COLOR MODEL
-----------
    public class Color
    {
        public byte Red, Green, Blue;                 // fields
        public Color(byte red, byte green, byte blue);
        public static List<Color> DefaultAnsiColors;  // the 256-entry palette
        public static Color DefaultForeground;        // white (0xff,0xff,0xff)
        public static Color DefaultBackground;        // black (0,0,0)
    }

    Palette layout of DefaultAnsiColors:
        [0-7]     standard colors (black, red, green, yellow, blue, magenta,
                  cyan, white)
        [8-15]    bright variants
        [16-231]  the 6x6x6 color cube
        [232-255] the grayscale ramp

    Terminal.MatchColor(r, g, b) returns the nearest palette index for an
    arbitrary RGB triple.

    This Color is CodeBrix.Terminal.Engine.Color, not System.Drawing.Color.


MOUSE TRACKING
--------------
    enum MouseMode
        Off, X10, VT200, ButtonEventTracking, AnyEvent
    enum MouseProtocolEncoding
        X10, UTF8, SGR, URXVT

    Both are set by the application running inside the terminal (via DECSET
    sequences you feed in) and read back from terminal.MouseMode /
    terminal.MouseProtocol.

    Extension methods (static class MouseModeExensions -- note the spelling,
    one "t"):
        bool SendButtonPress(this MouseMode mode)
        bool SendButtonRelease(this MouseMode mode)
        bool SendButtonTracking(this MouseMode mode)
        bool SendMotionEvent(this MouseMode mode)
        bool SendsModifiers(this MouseMode mode)

    Reporting a mouse event to the application:

        int flags = terminal.EncodeMouseButton(button, release, shift, meta,
                                               control);
        terminal.SendEvent(flags, x, y);          // press/release
        terminal.SendMouseMotion(flags, x, y);    // motion

    Gate the calls on the mode: only send motion when
    terminal.MouseMode.SendMotionEvent() is true, and so on.


KEYBOARD INPUT ENCODING
-----------------------
TerminalKeyEncoder translates key presses into the VT byte sequences a
terminal application expects, so a host does not hand-roll the mapping.
TerminalKey and TerminalModifiers are platform-neutral: map your native key
events (WinUI VirtualKey, GTK keyval, ...) onto them.

    public static class TerminalKeyEncoder
    {
        public static string Encode(TerminalKey key, TerminalModifiers modifiers,
                                    bool applicationCursor = false);
        public static string EncodeSpecial(TerminalKey key,
                                    bool applicationCursor = false);
        public static string EncodeComposed(int unicodeCodePoint,
                                    TerminalModifiers modifiers);
    }

    // Raw-key hosts (the platform exposes no composed-text event) call Encode
    // for everything; printables follow a US-QWERTY layout:
    string seq = TerminalKeyEncoder.Encode(
        TerminalKey.Up,
        TerminalModifiers.None,
        applicationCursor: terminal.ApplicationCursor);

    // Composed-text hosts encode named non-printables from the key identifier
    // and printable input from the layout-composed character, which is
    // correct on any keyboard layout:
    string special = TerminalKeyEncoder.EncodeSpecial(
        TerminalKey.Delete, terminal.ApplicationCursor);
    string text = TerminalKeyEncoder.EncodeComposed(codePoint, modifiers);

    All three return null for keys that produce no terminal input.

Rules applied: Ctrl chords become C0 control codes (Ctrl+A..Z -> 1..26,
Ctrl+[ = ESC, Ctrl+Space = NUL), Alt prefixes ESC (the meta convention),
Shift+Tab is back-tab (CSI Z), and arrows/Home/End honor application-cursor
mode -- always pass terminal.ApplicationCursor so the mode the application
negotiated is respected.

    enum TerminalKey
        None, Enter, Backspace, Tab, Escape, Space,
        Up, Down, Left, Right, Home, End, Insert, Delete, PageUp, PageDown,
        F1..F12,
        A..Z,
        D0..D9,                                 // the number row
        NumPad0..NumPad9, NumPadEnter, NumPadAdd, NumPadSubtract,
        NumPadMultiply, NumPadDivide, NumPadDecimal, NumPadEqual,
        Semicolon, Equal, Comma, Minus, Period, Slash, Backquote,
        LeftBracket, Backslash, RightBracket, Quote

    [Flags] enum TerminalModifiers
        None = 0, Shift = 1, Control = 2, Alt = 4, CapsLock = 8


ESCAPE SEQUENCE PARSER
----------------------
Terminal.Feed() drives an EscapeSequenceParser internally; you only touch this
class directly to extend or observe the parse.

    public class EscapeSequenceParser : IDisposable

Handler delegate signatures (all NESTED in EscapeSequenceParser):

    public delegate void CsiHandler(int[] parameters, string collect);
    public delegate void OscHandler(string data);
    public delegate void EscHandler(string collect, int flag);
    public delegate void ExecuteHandler();
    public unsafe delegate void PrintHandler(byte* data, int start, int end);

Registration:

    void SetCsiHandler(char flag, CsiHandler callback)
    void ClearCsiHandler(byte flag)
    void SetCsiHandlerFallback(Action<string, int[], int> fallback)

    void SetOscHandler(int identifier, OscHandler callback)
    void ClearOscHandler(int identifier)
    void SetOscHandlerFallback(Action<int, string> fallback)

    void SetEscHandler(string flag, EscHandler callback)
    void ClearEscHandler(string flag)
    void SetEscHandlerFallback(EscHandler fallback)

    void SetExecuteHandler(byte flag, ExecuteHandler handler)
    void ClearExecuteHandler(byte flag)
    void SetExecuteHandlerFallback(Action<byte> fallback)

    void SetDcsHandler(string flag, IDcsHandler handler)
    void ClearDcsHandler(string flag)
    void SetDcsHandlerFallback(IDcsHandler fallback)

    void SetPrintHandler(PrintHandler printHandler)
    void ClearPrintHandler()

    void SetErrorHandler(Func<ParsingState, ParsingState> errorHandler)
    void ClearErrorHandler()
    void Reset()
    void Dispose()

Example registrations:

    var parser = new EscapeSequenceParser();
    parser.SetCsiHandler('H', (pars, collect) => { /* cursor position */ });
    parser.SetOscHandler(0, (data) => { /* set title */ });
    parser.SetEscHandler("c", (collect, flag) => { /* full reset */ });
    parser.SetExecuteHandler(0x0d, () => { /* carriage return */ });
    parser.SetDcsHandler("q", dcsHandler);
    parser.SetCsiHandlerFallback((collect, pars, flag) => { });
    parser.SetEscHandlerFallback((collect, flag) => { });

The DCS contract -- implement this to handle a device-control string:

    public interface IDcsHandler
    {
        void Hook(string collect, int[] parameters, int flag);
        unsafe void Put(byte* data, int start, int end);
        void Unhook();
    }

    Hook is called when the DCS is entered, Put is called (possibly several
    times) with the payload bytes, and Unhook when the string terminator
    arrives. Put is UNSAFE, so a project that implements IDcsHandler needs
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>.

Parser state, exposed for error handlers and diagnostics:

    public class ParsingState
    {
        public int Position;                // index in the parse string
        public int Code;                    // current character code
        public ParserState CurrentState;
        public int Print;                   // print-buffer start (-1 = unset)
        public int Dcs;                     // DCS buffer start (-1 = unset)
        public string Osc;
        public string Collect;
        public int[] Parameters;
        public bool Abort;
    }

    enum ParserState
        Invalid = -1, Ground = 0, Escape, EscapeIntermediate, CsiEntry,
        CsiParam, CsiIntermediate, CsiIgnore, SosPmApcString, OscString,
        DcsEntry, DcsParam, DcsIgnore, DcsIntermediate, DcsPassthrough

    The parser also exposes its handler dictionaries and fallbacks as public
    fields (CsiHandlers, OscHandlers, ExecuteHandlers, EscHandlers,
    DcsHandlers, ActiveDcsHandler, ErrorHandler and the *Fallback members).
    Prefer the Set*/Clear* methods; the fields are there for inspection.


CONTROL CODES AND PREDEFINED SEQUENCES
--------------------------------------
    public struct ControlCodes

    C0 codes as static uint fields:
        NUL 0x00, BEL 0x07, BS 0x08, HT 0x09, LF 0x0a, VT 0x0b, FF 0x0c,
        CR 0x0d, SO 0x0e, SI 0x0f, CAN 0x18, SUB 0x1a, ESC 0x1b, SP 0x20,
        DEL 0x7f

    Instance member:
        bool Send8bit { get; set; }

    C1 introducers as instance string properties, each returning the 8-bit
    form when Send8bit is true and the 7-bit ESC-prefixed form otherwise:
        PAD, HOP, BPH, NBH, IND, NEL, SSA, ESA, HTS, HTJ, VTS, PLD, PLU, RI,
        SS2, SS3, DCS, PU1, PU2, STS, CCH, MW, SPA, EPA, SOS, SGCI, SCI,
        CSI, ST, OSC, PM, APC

    Read the terminal's instance from terminal.ControlCodes. For example
    ControlCodes.CSI returns the two-character ESC + "[" sequence when
    Send8bit is false, and the single U+009B character when it is true.

    public static class EscapeSequences -- key sequences as byte[]:
        CmdNewline, CmdRet, CmdEsc, CmdDel, CmdDelKey, CmdTab, CmdBackTab,
        CmdPageUp, CmdPageDown
        MoveUpApp / MoveUpNormal, MoveDownApp / MoveDownNormal,
        MoveLeftApp / MoveLeftNormal, MoveRightApp / MoveRightNormal,
        MoveHomeApp / MoveHomeNormal, MoveEndApp / MoveEndNormal
        byte[][] CmdF                       // CmdF[0] = F1 ... CmdF[11] = F12

    The App/Normal pairs correspond to application-cursor mode; pick with
    terminal.ApplicationCursor, or just let TerminalKeyEncoder do it.


CHARACTER SETS
--------------
    public class CharSets
    {
        public static Dictionary<byte, Dictionary<byte, string>> All;
        public static Dictionary<byte, string> Default;   // null -- the "B"
                                                          //   (US ASCII) set
    }

    All is keyed by the SCS designator byte -- for example (byte)'0' selects
    the DEC Special Character and Line Drawing set that curses applications
    use for box drawing. Each inner dictionary maps an input byte to the
    replacement string to display.

    The active table is terminal.Charset (a Dictionary<byte, string>), which
    is null when no translation applies. Escape sequences you feed in
    (the ESC ( 0 family) switch it; you can also assign it directly.

    public class CharSet is an empty placeholder type carried over from the
    upstream project -- ignore it.


SEARCH AND SELECTION
--------------------
SelectionService -- a selection over the buffer, in buffer-absolute
coordinates:

    public class SelectionService
    {
        public SelectionService(Terminal terminal);
        public bool Active { get; set; }
        public Point Start { get; }              // System.Drawing.Point
        public Point End { get; }
        public event Action SelectionChanged;
        public void StartSelection(int row, int col);
        public void StartSelection();            // from the current cursor
        public void SetSoftStart(int row, int col);
        public void ShiftExtend(int row, int col);
        public void DragExtend(int row, int col);
        public void SelectAll();
        public void SelectNone();
        public void SelectRow(int row);
        public void SelectWordOrExpression(int col, int row);   // col, row!
        public string GetSelectedText();
        public Line[] GetSelectedLines();
    }

    Conventions: Start and End are BUFFER-ABSOLUTE (so a selection survives
    scrolling) while the method inputs take viewport-relative rows. End is
    column-EXCLUSIVE -- GetSelectedText() copies [Start.X, End.X) on the end
    row -- and Start == End yields empty text, so a plain click carries no
    text.

    WARNING: SelectWordOrExpression takes (col, row) while StartSelection,
    SetSoftStart, ShiftExtend and DragExtend all take (row, col).

SearchService -- text search over a snapshot of the buffer:

    public class SearchService
    {
        public SearchService(Terminal terminal);
        public event Action<SearchService, string> Invalidated;
        public SearchSnapshot GetSnapshot();
        public void Invalidate();
    }

    public class SearchSnapshot
    {
        public SearchSnapshot(Line[] lines);
        public string Text { get; }
        public string LastSearch { get; }
        public SearchResult[] LastSearchResults { get; }
        public int CurrentSearchResult;              // field
        public int FindText(string txt);             // returns the match count
        public SearchResult FindNext();
        public SearchResult FindPrevious();

        public class SearchResult                    // NESTED
        {
            public Point Start;                      // System.Drawing.Point
            public Point End;
        }
    }

    The snapshot is a point-in-time copy: call GetSnapshot() again (or
    subscribe to Invalidated) after the buffer changes. Because SearchResult
    is nested, name it SearchSnapshot.SearchResult from outside the class, or
    just use var.

Line and LineFragment -- the flattened text model both services return:

    public class Line
    {
        public Line();
        public int StartLine { get; }
        public int StartLocation { get; }
        public int Length { get; }
        public void Add(LineFragment fragment);
        public void GetFragmentStrings(StringBuilder builder);
        public int GetFragmentIndexForPosition(int pos);
        public LineFragment GetFragment(int index);
        public override string ToString();
    }

    public class LineFragment
    {
        public LineFragment(string text, int line, int location);
        public int Line { get; }             // buffer line index
        public int Location { get; }         // column where the fragment starts
        public string Text { get; }
        public int Length { get; }
        public static LineFragment NewLine(int line);
    }

    A logical Line is a sequence of LineFragments, which is how a wrapped
    terminal line is stitched back into one searchable, selectable string.


PTY SUPPORT (UNIX AND MACOS ONLY)
---------------------------------
    public struct UnixWindowSize
    {
        public short row, col, xpixel, ypixel;   // fields
    }

    public class Pty
    {
        public static int ForkAndExec(string programName, string[] args,
                                      string[] env, out int master,
                                      UnixWindowSize winSize);
        public static int SetWinSize(int fd, ref UnixWindowSize winSize);
        public static int AvailableBytes(int fd, ref long size);
    }

    Usage:

        var winSize = new UnixWindowSize { row = 25, col = 80 };
        int pid = Pty.ForkAndExec("/bin/bash", args, env,
                                  out int master, winSize);
        Pty.SetWinSize(master, ref winSize);
        long available = 0;
        Pty.AvailableBytes(master, ref available);

    Terminal.GetEnvironmentVariables(termName) builds a suitable env array.

    These are P/Invokes into libc: they do NOT work on Windows. On Windows,
    drive the terminal from System.Diagnostics.Process redirected streams (or
    a ConPTY wrapper of your own) and Feed() what you read.


UNICODE TEXT SUPPORT (CodeBrix.Terminal.Text)
---------------------------------------------
ustring -- an immutable UTF-8 string:

    // Creation
    ustring s = ustring.Make("Hello");
    ustring s = ustring.Make(new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f });
    ustring s = ustring.Make(new Rune('A'));
    ustring s = ustring.Make(buffer, start, count);
    ustring s = "Hello";                    // implicit from string
    ustring.Empty
    static bool ustring.IsNullOrEmpty(ustring value)

    // Properties
    int byteLen = s.Length;                 // BYTES
    int runeCount = s.RuneCount;            // code points
    int width = s.ConsoleWidth;             // terminal columns
    bool empty = s.IsEmpty;

    // Slicing (byte offsets unless the name says Rune)
    ustring sub = s[2, 5];                  // [start, end)
    ustring sub = s.Substring(2, 3);
    ustring sub = s.RuneSubstring(1, 3);
    Rune r = s.RuneAt(index);

    // Search
    int idx = s.IndexOf("llo");
    int idx = s.IndexOf(rune, offset);
    int last = s.LastIndexOf("llo");
    int any = s.IndexOfAny("abc");
    bool has = s.Contains("llo");
    bool anyOf = s.ContainsAny("abc");
    bool starts = s.StartsWith("He");
    bool ends = s.EndsWith("lo");
    int count = s.Count("l");

    // Case conversion (the SpecialCase overloads take Unicode.SpecialCase)
    ustring upper = s.ToUpper();
    ustring lower = s.ToLower();
    ustring titled = s.ToTitle();
    ustring t = s.Title();                  // word-boundary aware

    // Splitting, joining, editing
    ustring[] parts = s.Split(" ");
    ustring[] all = s.Explode();            // one entry per rune
    ustring joined = ustring.Join(", ", parts);
    ustring combined = ustring.Concat(s1, s2);
    ustring plus = s1 + s2;
    ustring replaced = s.Replace("a", "b");

    // Trimming (cutset or ustring.RunePredicate overloads)
    ustring trimmed = s.TrimSpace();
    ustring ts = s.TrimStart(" ");
    ustring te = s.TrimEnd(" ");
    ustring tr = s.Trim(predicate);

    // Enumeration and conversion
    foreach (var (index, rune) in s.Range()) { }
    uint[] runes = s.ToRunes();
    List<Rune> list = s.ToRuneList();
    string str = s.ToString();
    byte[] bytes = s.ToByteArray();
    bool same = s.EqualsFold(other);        // case-insensitive comparison

    ustring is an abstract class with several private implementations
    (byte-buffer backed, range-of-buffer backed, and native-memory backed via
    ustring.Make(IntPtr, ...) / ustring.MakeCopy(IntPtr, ...)). Treat it as an
    immutable value: every "mutating" method returns a new ustring.

Rune (struct, in the System namespace) -- one Unicode code point:

    Rune r = new Rune('A');
    Rune r = new Rune(0x1F600);
    Rune r = new Rune(highSurrogate, lowSurrogate);
    Rune r = 'A';                           // implicit from char/int/byte/uint
    uint u = r;                             // implicit to uint

    uint value = r.Value;
    bool valid = r.IsValid;
    bool surrogate = r.IsSurrogate;
    bool pair = r.IsSurrogatePair;
    bool high = r.IsHighSurrogate;
    bool low = r.IsLowSurrogate;
    bool nonSpacing = r.IsNonSpacing;
    static Rune Error, MaxRune, ReplacementChar;

    int width = Rune.ColumnWidth(r);        // 0 (non-spacing), 1, or 2 (wide)
    static bool Rune.IsWideChar(uint rune)
    static bool Rune.IsNonSpacingChar(uint rune)

    // Classification -- STATIC methods, not instance methods:
    Rune.IsDigit, IsLetter, IsLetterOrDigit, IsLetterOrNumber, IsNumber,
    Rune.IsUpper, IsLower, IsTitle, IsWhiteSpace, IsPunctuation, IsSymbol,
    Rune.IsMark, IsControl, IsGraphic, IsPrint

    // Case conversion -- also STATIC:
    Rune u = Rune.ToUpper(r);
    Rune l = Rune.ToLower(r);
    Rune t = Rune.ToTitle(r);
    Rune c = Rune.To(Rune.Case.Upper, r);   // Case is NESTED in Rune
    Rune f = Rune.SimpleFold(r);

    // UTF-8 helpers on Rune itself
    static bool Rune.FullRune(byte[] p)
    static int Rune.RuneLen(Rune rune)
    static int Rune.EncodeRune(Rune rune, byte[] dest, int offset = 0)
    static int Rune.RuneCount(byte[] buffer, int offset = 0, int count = -1)
    static bool Rune.Valid(byte[] buffer)
    static int Rune.InvalidIndex(byte[] buffer)
    static bool Rune.ValidRune(Rune rune)
    static int Rune.ExpectedSizeFromFirstByte(byte firstByte)

    // Surrogate pairs (bool try-pattern with out parameters)
    bool ok  = Rune.EncodeSurrogatePair(high, low, out Rune combined);
    bool ok2 = Rune.DecodeSurrogatePair(rune, out char[] chars);
    bool ok3 = Rune.DecodeSurrogatePair(str,  out char[] chars);

RuneExtensions (static class, also in the System namespace) -- extension
methods ON ustring, so they are in scope without a Text using:

    bool full = str.FullRune();
    (Rune rune, int size) = str.DecodeRune(start, n);
    (Rune rune, int size) = str.DecodeLastRune(end);
    int count = str.RuneCount();
    int bad = str.InvalidIndex();
    bool valid = str.Valid();
    int size = str.ExpectedSizeFromFirstByte();

Utf8 (static class) -- UTF-8 primitives, with byte[] and ustring overloads:

    static uint RuneError;                  // 0xfffd
    static bool FullRune(byte[] p) / FullRune(ustring str)
    static (uint Rune, int Size) DecodeRune(byte[] buffer, int start = 0,
                                            int n = -1)
    static (uint Rune, int size) DecodeRune(ustring str, int start = 0,
                                            int n = -1)
    static (uint Rune, int size) DecodeLastRune(byte[] buffer, int end = -1)
    static (uint Rune, int size) DecodeLastRune(ustring str, int end = -1)
    static int RuneLen(uint rune)
    static int EncodeRune(uint rune, byte[] dest, int offset = 0)
    static int RuneCount(byte[] buffer, int offset = 0, int count = -1)
    static int RuneCount(ustring str)
    static bool Valid(byte[] buffer) / Valid(ustring str)
    static int InvalidIndex(byte[] buffer) / InvalidIndex(ustring str)
    static bool ValidRune(uint rune)

Unicode (a partial class of static methods) -- classification over raw code
points:

    const string Unicode.Version = "15.0.0"

    static bool Unicode.IsDigit(uint rune)
    static bool Unicode.IsLetter / IsNumber / IsMark / IsPunct / IsSpace /
                        IsSymbol / IsControl / IsGraphic / IsPrint /
                        IsUpper / IsLower / IsTitle (uint rune)
    static bool Unicode.IsRuneInRanges(uint rune, params RangeTable[] inRanges)
    static uint Unicode.ToUpper / ToLower / ToTitle (uint rune)
    static uint Unicode.To(Case toCase, uint rune)
    static uint Unicode.SimpleFold(uint rune)

    Nested inside Unicode (qualify them as Unicode.X):
        static class Category    // general categories (Lu, Ll, Nd, ...) as
                                 //   RangeTable fields
        static class Script      // script tables (Latin, Greek, Han, ...)
        static class Property    // binary properties (White_Space, ...)
        struct RangeTable        // a set of code-point ranges;
                                 //   public readonly int LatinOffset
        struct SpecialCase       // locale-specific casing, passed to the
                                 //   ustring ToUpper/ToLower/ToTitle overloads
        enum Case                // Upper, Lower, Title

    These table types are low-level plumbing behind the Is*/To* methods.
    Reach for them only when you need a category or script membership test
    that the convenience methods do not expose.


UTILITY TYPES
-------------
    public class CircularList<T>             // the type of Buffer.Lines
    {
        public CircularList(int maxLength);
        public int MaxLength { get; set; }
        public int Length { get; set; }
        public bool IsFull { get; }
        public T this[int index] { get; set; }
        public Action<int> Trimmed;          // field: raised when lines are
                                             //   dropped off the front
        public void Push(T value);
        public T Pop();
        public T Recycle();
        public void Splice(int start, int deleteCount, params T[] items);
        public void TrimStart(int count);
        public void ShiftElements(int start, int count, int offset);
        public void ForEach(Action<T, int> callback);
        public T[] ToArray();
    }

    public static class RuneHelper
    {
        public static int ConsoleWidth(this uint rune);
    }

    public class RuneExt                     // UTF-8 byte-level helpers
    {
        public static int ExpectedSizeFromFirstByte(byte b);
        public unsafe static bool FullRune(byte* p, int n);
    }

    Both CircularList<T> and RuneExt live in CodeBrix.Terminal.Engine, NOT in
    any "Utils" namespace.


================================================================================

WRITING A RENDERER
==================
The engine is renderer-agnostic: a host paints the buffer itself. The
essentials every renderer needs:

1. INDEXING. The cell at visible position (row, col) is
   terminal.Buffer.Lines[terminal.Buffer.YDisp + row][col]. YDisp is the
   VIEWPORT position; YBase is where the live screen starts. The two are equal
   while following live output, but differ when the user scrolls back -- paint
   from YDisp, or scrolled-back views render the wrong rows.

2. BLANK CELLS. Never draw CharData.Rune verbatim: a never-written cell
   carries rune U+0200 (not a space) and paints a stray glyph. When
   ch.IsBlank is true, paint only the background. Wide-character continuation
   cells (ch.Width == 0) must be skipped too -- the preceding width-2 cell
   already covers them.

3. ATTRIBUTES. Unpack the packed int:
       var (fg, bg, flags) = CharacterAttribute.Unpack(ch.Attribute);
   fg/bg are 256-color palette indices resolved against
   Color.DefaultAnsiColors, except the sentinels
   CharacterAttribute.DefaultColorIndex (256, use your default) and
   InvertedDefaultColorIndex (257, use the inverted default). flags is the
   FLAGS enum (BOLD, UNDERLINE, INVERSE, ...).

4. DAMAGE TRACKING. The engine accumulates a dirty span of visible rows; the
   renderer reads and clears it:
       terminal.GetUpdateRange(out int startY, out int endY);
       // paint rows startY..endY, then:
       terminal.ClearUpdateRange();
   startY == int.MaxValue (with endY == -1) means nothing is dirty. The
   contract is renderer-clears: nothing resets the span except
   ClearUpdateRange. Full-surface repaints each frame also work at modest
   terminal sizes -- damage tracking is the optimization, not a requirement.

5. VIEWPORT. Mouse wheel -> terminal.ScrollLines(+/-lines); typing ->
   terminal.ScrollToBottom(); use IsAtBottom to decide whether to follow new
   output; subscribe Scrolled (it fires with the new Buffer.YDisp on every
   viewport move) to sync a scrollbar.

6. SELECTION. SelectionService endpoints (Start/End) are BUFFER-ABSOLUTE
   points -- selections survive scrolling -- while its inputs take
   viewport-relative rows. End is column-EXCLUSIVE: GetSelectedText() copies
   [Start.X, End.X) on the end row, and Start == End yields empty text (a
   plain click carries no text). Draw highlights with the same convention or
   the highlight and the copied text disagree by one cell.
   WARNING: SelectWordOrExpression takes (col, row) while its siblings
   (StartSelection, DragExtend, ShiftExtend, SetSoftStart) take (row, col).

7. CURSOR AND SIZE. Paint the cursor at (Buffer.X, Buffer.Y) relative to
   YBase, honoring terminal.CursorHidden and Options.CursorStyle. When your
   surface changes size, call terminal.Resize(cols, rows) and push the new
   size to the process too (Pty.SetWinSize on Unix); the delegate's
   SizeChanged callback fires after a resize.

8. KEYBOARD. Do not invent escape sequences: run key events through
   TerminalKeyEncoder and hand the result to your transport (or straight back
   into Feed for a loopback). Always pass terminal.ApplicationCursor.


================================================================================

COMPLETE EXAMPLES
=================

Example 1: Create a terminal and read the buffer
------------------------------------------------
    using System;
    using CodeBrix.Terminal.Engine;

    var terminal = new Terminal(null,
        new TerminalOptions { Cols = 80, Rows = 25 });

    terminal.Feed("Hello, Terminal!\r\n");
    terminal.Feed("\x1b[1;31m");            // bold red foreground
    terminal.Feed("Red bold text\r\n");
    terminal.Feed("\x1b[0m");               // reset attributes

    var line = terminal.Buffer.Lines[terminal.Buffer.YBase + 0];
    for (int col = 0; col < terminal.Cols; col++)
    {
        var ch = line[col];
        if (ch.Code != 0)
            Console.Write((char)ch.Code);
    }
    Console.WriteLine();


Example 2: Scroll regions
-------------------------
    using CodeBrix.Terminal.Engine;

    var terminal = new Terminal(null,
        new TerminalOptions { Cols = 80, Rows = 25 });

    // Confine scrolling to rows 5-20 (escape-sequence coordinates are
    // 1-based)
    terminal.Feed("\x1b[5;20r");

    for (int i = 0; i < 30; i++)
    {
        terminal.Feed($"Line {i}\r\n");
    }

    // The same thing through the API:
    terminal.SetScrollRegion(4, 19);        // buffer coordinates are 0-based


Example 3: A custom terminal delegate
-------------------------------------
    using System;
    using CodeBrix.Terminal.Engine;

    public class MyTerminalDelegate : SimpleTerminalDelegate
    {
        public override void Send(byte[] data)
        {
            // Write these bytes to the process or remote host
            Console.WriteLine($"Terminal sent {data.Length} bytes");
        }

        public override void SizeChanged(Terminal source)
        {
            Console.WriteLine($"Resized to {source.Cols}x{source.Rows}");
        }

        public override void SetTerminalTitle(Terminal source, string title)
        {
            Console.Title = title;
        }

        public override string WindowCommand(Terminal source,
            WindowManipulationCommand command, params int[] args)
        {
            // Return a report string for the Report* commands; null otherwise
            return command == WindowManipulationCommand.ReportWindowTitle
                ? source.Title
                : null;
        }
    }

    var terminal = new Terminal(new MyTerminalDelegate(),
        new TerminalOptions { Cols = 120, Rows = 40 });


Example 4: Painting a frame, with attributes decoded
----------------------------------------------------
    using CodeBrix.Terminal.Engine;

    var terminal = new Terminal(null,
        new TerminalOptions { Cols = 120, Rows = 50 });
    terminal.Feed("\x1b[1;3mHello\x1b[0m\r\n");

    terminal.GetUpdateRange(out int startY, out int endY);
    if (startY != int.MaxValue)
    {
        for (int row = startY; row <= endY; row++)
        {
            var line = terminal.Buffer.Lines[terminal.Buffer.YDisp + row];
            for (int col = 0; col < terminal.Cols; col++)
            {
                var ch = line[col];

                var (fg, bg, flags) = CharacterAttribute.Unpack(ch.Attribute);

                var background = bg == CharacterAttribute.DefaultColorIndex
                    ? Color.DefaultBackground
                    : Color.DefaultAnsiColors[bg];

                // Paint the cell background first...
                if (ch.IsBlank || ch.Width == 0)
                    continue;               // nothing more to draw here

                var foreground = fg == CharacterAttribute.DefaultColorIndex
                    ? Color.DefaultForeground
                    : Color.DefaultAnsiColors[fg];

                bool bold = flags.HasFlag(FLAGS.BOLD);
                bool italic = flags.HasFlag(FLAGS.ITALIC);
                bool inverse = flags.HasFlag(FLAGS.INVERSE);

                // Draw ch.Rune with foreground/background/bold/italic/inverse,
                // advancing ch.Width columns.
            }
        }
        terminal.ClearUpdateRange();
    }


Example 5: Unicode text processing
----------------------------------
    using CodeBrix.Terminal.Text;

    ustring text = "Hello, World! \u00e9\u00e8\u00ea";
    int runeCount = text.RuneCount;         // code points, not bytes
    int displayWidth = text.ConsoleWidth;   // terminal columns

    ustring upper = text.ToUpper();
    ustring lower = text.ToLower();

    foreach (var (index, rune) in text.Range())
    {
        bool isLetter = Rune.IsLetter(new Rune(rune));
        int width = Rune.ColumnWidth(new Rune(rune));
    }


Example 6: Scrollback, selection and copy
-----------------------------------------
    using System;
    using System.Drawing;
    using CodeBrix.Terminal.Engine;

    var terminal = new Terminal(null,
        new TerminalOptions { Cols = 80, Rows = 10, Scrollback = 500 });

    for (int i = 0; i < 100; i++)
        terminal.Feed($"line {i}\r\n");

    // Scroll back five lines and check where we are
    terminal.ScrollLines(-5);
    Console.WriteLine($"at bottom: {terminal.IsAtBottom}, " +
                      $"YDisp={terminal.Buffer.YDisp}, " +
                      $"YBase={terminal.Buffer.YBase}");

    var selection = new SelectionService(terminal);
    selection.StartSelection(row: 0, col: 0);   // row, col
    selection.DragExtend(row: 2, col: 6);       // row, col
    string copied = selection.GetSelectedText();

    selection.SelectWordOrExpression(col: 2, row: 1);   // col, row!
    string word = selection.GetSelectedText();

    Point start = selection.Start;              // buffer-absolute
    Point end = selection.End;                  // End.X is EXCLUSIVE

    terminal.ScrollToBottom();


Example 7: Searching the scrollback
-----------------------------------
    using System;
    using CodeBrix.Terminal.Engine;

    var search = new SearchService(terminal);
    var snapshot = search.GetSnapshot();

    int matches = snapshot.FindText("line 42");
    if (matches > 0)
    {
        var hit = snapshot.FindNext();
        Console.WriteLine($"match at {hit.Start} .. {hit.End}");

        foreach (var result in snapshot.LastSearchResults)
            Console.WriteLine($"{result.Start} -> {result.End}");
    }

    // The snapshot is a point-in-time copy; take a new one after the buffer
    // changes (or subscribe to search.Invalidated).
    search.Invalidate();
    snapshot = search.GetSnapshot();


Example 8: Turning key presses into terminal input
--------------------------------------------------
    using CodeBrix.Terminal.Engine;

    // A raw-key host: one call handles printables and non-printables alike.
    void OnKeyDown(TerminalKey key, TerminalModifiers modifiers)
    {
        string seq = TerminalKeyEncoder.Encode(
            key, modifiers, applicationCursor: terminal.ApplicationCursor);

        if (seq is not null)
        {
            // Send seq to the process / remote host. For a loopback demo:
            terminal.Feed(seq);
            terminal.ScrollToBottom();
        }
    }

    // A composed-text host: named keys through EncodeSpecial, typed
    // characters through EncodeComposed (correct on any keyboard layout).
    void OnSpecialKey(TerminalKey key)
        => Send(TerminalKeyEncoder.EncodeSpecial(
                    key, terminal.ApplicationCursor));

    void OnTextInput(int codePoint, TerminalModifiers modifiers)
        => Send(TerminalKeyEncoder.EncodeComposed(codePoint, modifiers));


================================================================================

MINIMUM VIABLE PROJECT
======================

    dotnet new console -n MyTerminalApp --framework net10.0
    cd MyTerminalApp
    dotnet add package CodeBrix.Terminal.MitLicenseForever

MyTerminalApp.csproj
--------------------
    <Project Sdk="Microsoft.NET.Sdk">

      <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <Nullable>disable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
      </PropertyGroup>

      <ItemGroup>
        <PackageReference Include="CodeBrix.Terminal.MitLicenseForever"
                          Version="*" />
      </ItemGroup>

    </Project>

    (Replace Version="*" with the version you intend to pin.)

Program.cs
----------
    using System;
    using CodeBrix.Terminal.Engine;

    var terminal = new Terminal(null, new TerminalOptions
    {
        Cols = 80,
        Rows = 25,
        ConvertEol = true,      // set false if your data source sends \r\n
    });

    terminal.Feed("Hello, Terminal!\r\n");
    terminal.Feed("\x1b[1;32mGreen text\x1b[0m\r\n");
    terminal.Feed("tabs:\tone\ttwo\r\n");

    for (int row = 0; row < 4; row++)
    {
        var line = terminal.Buffer.Lines[terminal.Buffer.YBase + row];
        Console.WriteLine(line.TranslateToString(trimRight: true).ToString());
    }

    dotnet build
    dotnet run


================================================================================

PERFORMANCE TIPS
================

1. BATCH Feed() CALLS. Concatenate incoming data and feed it in chunks rather
   than character by character; the parser is a state machine and a single
   larger Feed is much cheaper than many tiny ones.

2. USE THE BYTE OVERLOAD FOR RAW STREAMS. Feed(byte[], len) avoids a
   decode-then-re-encode round trip when you already hold bytes off a socket
   or a PTY. Feed(string) is the convenient choice for literal text and
   escape sequences you write yourself.

3. PAINT ONLY DIRTY ROWS. GetUpdateRange / ClearUpdateRange give you the
   damaged span; repainting the whole surface every frame is correct but
   wasteful at large sizes.

4. READ YDisp + row, NEVER A BARE Lines[row]. The live screen's row is
   Lines[Buffer.YBase + row]; the viewport's visible row is
   Lines[Buffer.YDisp + row]. Getting this wrong is a correctness bug, not
   just a performance one.

5. USE TranslateToString / TranslateBufferLineToString to extract text
   instead of looping cell by cell, and pass trimRight: true to drop trailing
   blanks.

6. USE Rune.ColumnWidth() (or CharData.Width) instead of assuming width 1.
   CJK and emoji occupy two columns, and combining marks occupy none.

7. EXTEND SimpleTerminalDelegate rather than implementing all seven
   ITerminalDelegate members when you only care about one or two.

8. SIZE Scrollback DELIBERATELY. Every scrollback line is a live BufferLine;
   the default is 1000 lines. Set TerminalOptions.Scrollback before
   constructing the Terminal, because the buffer sizes itself at
   construction.

9. CACHE THE SEARCH SNAPSHOT. SearchService.GetSnapshot() flattens the whole
   buffer; take one snapshot per search session rather than per keystroke,
   and refresh it on Invalidated.


================================================================================

COMMON PITFALLS TO AVOID
========================

1. DO NOT confuse the package id with the namespaces.
   Package   : CodeBrix.Terminal.MitLicenseForever
   Namespaces: CodeBrix.Terminal.Engine, CodeBrix.Terminal.Text (plus Rune
               and RuneExtensions in System).

2. DO NOT write "using CodeBrix.Terminal.Engine.Utils;". There is no such
   namespace; CircularList<T> and RuneExt are in CodeBrix.Terminal.Engine.
   Likewise the upstream "using XtermSharp;" and "using NStack;" do not
   exist here.

3. DO NOT name the type Buffer while both "using System;" and
   "using CodeBrix.Terminal.Engine;" are in scope -- CS0104 against
   System.Buffer. Alias it:
       using TerminalBuffer = CodeBrix.Terminal.Engine.Buffer;
   The same trap applies to Color once you add "using System.Drawing;".

4. DO NOT try to SET the terminal mode properties. MarginMode, OriginMode,
   Wraparound, ReverseWraparound, ApplicationCursor, ApplicationKeypad,
   MouseMode, MouseProtocol and friends have internal setters: they change
   only in response to escape sequences you Feed.

5. DO NOT read Lines[row] directly. Use Lines[Buffer.YBase + row] for the
   live screen or Lines[Buffer.YDisp + row] for what the viewport shows;
   they differ while the user is scrolled back.

6. DO NOT paint CharData.Rune for a blank cell. A never-written cell holds
   U+0200, not a space -- check ch.IsBlank and paint background only. Skip
   ch.Width == 0 cells; they are the second half of a wide character.

7. DO NOT hand-roll the attribute bit shifts. Use
   CharacterAttribute.Unpack(attr) and the DefaultColorIndex /
   InvertedDefaultColorIndex sentinels, which are 256 and 257 and are NOT
   valid indices into Color.DefaultAnsiColors.

8. DO NOT leave ConvertEol at its default when your data source already
   sends \r\n (SSH shells, most PTYs). Set ConvertEol = false or every line
   break doubles into a blank line.

9. DO NOT set Scrollback or TabStopWidth after constructing the Terminal and
   expect it to take effect -- the buffer and the tab stops are laid out at
   construction.

10. DO NOT mix up the coordinate orders. Terminal.SetCursor takes (col, row).
    SelectionService.SelectWordOrExpression takes (col, row) while
    StartSelection, SetSoftStart, ShiftExtend and DragExtend take (row, col).

11. DO NOT forget that escape-sequence coordinates are 1-based (CUP is
    \x1b[row;colH) while every buffer coordinate in this API is 0-based.

12. DO NOT treat SelectionService.End as inclusive. GetSelectedText() copies
    [Start.X, End.X) on the end row, and Start == End is an empty selection.

13. DO NOT confuse ustring.Length (BYTES) with ustring.RuneCount (code
    points) or ustring.ConsoleWidth (terminal columns). The three differ for
    any non-ASCII text.

14. DO NOT call Rune classification as instance methods. Rune.IsLetter(r),
    Rune.ToUpper(r) and the rest are STATIC.

15. DO NOT reference SearchResult unqualified. It is nested:
    SearchSnapshot.SearchResult. The same goes for the parser's handler
    delegates (EscapeSequenceParser.CsiHandler and siblings),
    ustring.RunePredicate, Rune.Case, and Unicode.Category / Script /
    Property / RangeTable / SpecialCase / Case.

16. DO NOT hold a SearchSnapshot across buffer changes. It is a point-in-time
    copy; take a new one (SearchService.Invalidated tells you when).

17. DO NOT call Pty.ForkAndExec on Windows. It is a libc P/Invoke and works
    on Unix and macOS only.

18. DO NOT expect 24-bit color. The model is the 256-color xterm palette;
    Terminal.MatchColor(r, g, b) maps an arbitrary RGB triple onto the
    nearest palette index.

19. DO NOT target .NET versions below 10.0.


================================================================================

WHAT THIS PACKAGE DOES NOT DO
=============================

Do NOT reach for this package to:

  - Provide a GUI or console terminal CONTROL. There is no widget and no
    drawing code; you write the renderer (see WRITING A RENDERER).
  - Run a shell or command interpreter. The engine emulates the display side
    only; spawning the process is your job (Pty helps on Unix/macOS).
  - Speak SSH, Telnet or any network protocol. There is no transport here.
  - Read the keyboard. TerminalKeyEncoder ENCODES key identifiers you supply;
    capturing key events is the host's job.
  - Spawn processes on Windows. Pty.ForkAndExec is Unix/macOS only.
  - Render TrueColor (24-bit RGB). The color model tops out at the 256-color
    palette.
  - Integrate with the clipboard.
  - Implement the DEC Locator mouse protocol, or VT200 Highlight mouse mode
    (which can deadlock a terminal).
  - Render fonts, shape text or look up glyphs. Widths are computed from
    Unicode tables; drawing is the host's.
  - Serialize or persist terminal state.
  - Run on .NET versions below 10.0.

This package IS for: emulating a virtual terminal in memory, parsing escape
sequences, managing terminal buffers and scrollback, encoding keyboard and
mouse input into terminal byte streams, and providing Unicode text utilities
-- all without a physical terminal.


================================================================================

WORKING EXAMPLES ON GITHUB
==========================

The two test projects are the largest body of compiling, working usage of
this package:

    https://github.com/ellisnet/CodeBrix.Terminal/tree/main/tests/CodeBrix.Terminal.Engine.Tests
    https://github.com/ellisnet/CodeBrix.Terminal/tree/main/tests/CodeBrix.Terminal.Text.Tests

Feature-to-test-file map:

  Terminal basics (initialization, cursor movement, scroll regions, CSI)
    https://github.com/ellisnet/CodeBrix.Terminal/blob/main/tests/CodeBrix.Terminal.Engine.Tests/BaseTerminalTests.cs

  Attribute packing and unpacking (CharacterAttribute, FLAGS)
    https://github.com/ellisnet/CodeBrix.Terminal/blob/main/tests/CodeBrix.Terminal.Engine.Tests/CharacterAttributeTests.cs

  CharData semantics, including blank cells and wide characters
    https://github.com/ellisnet/CodeBrix.Terminal/blob/main/tests/CodeBrix.Terminal.Engine.Tests/CharDataTests.cs

  SelectionService coordinate conventions and GetSelectedText
    https://github.com/ellisnet/CodeBrix.Terminal/blob/main/tests/CodeBrix.Terminal.Engine.Tests/SelectionServiceTests.cs

  TerminalKeyEncoder: Encode, EncodeSpecial, EncodeComposed
    https://github.com/ellisnet/CodeBrix.Terminal/blob/main/tests/CodeBrix.Terminal.Engine.Tests/TerminalKeyEncoderTests.cs

  TerminalOptions defaults, Scrollback and TabStopWidth
    https://github.com/ellisnet/CodeBrix.Terminal/blob/main/tests/CodeBrix.Terminal.Engine.Tests/TerminalOptionsTests.cs

  Viewport, ScrollLines, ScrollToBottom, IsAtBottom and the Scrolled event
    https://github.com/ellisnet/CodeBrix.Terminal/blob/main/tests/CodeBrix.Terminal.Engine.Tests/ViewportTests.cs

  Backspace behavior (wrapping, margins, reverse wrap)
    https://github.com/ellisnet/CodeBrix.Terminal/blob/main/tests/CodeBrix.Terminal.Engine.Tests/EscapeSequence/BsTests.cs

  Cursor backward tabulation
    https://github.com/ellisnet/CodeBrix.Terminal/blob/main/tests/CodeBrix.Terminal.Engine.Tests/EscapeSequence/CbtTests.cs

  Carriage return (margins, origin mode)
    https://github.com/ellisnet/CodeBrix.Terminal/blob/main/tests/CodeBrix.Terminal.Engine.Tests/EscapeSequence/CrTests.cs

  Cursor position (CUP, origin mode, bounds)
    https://github.com/ellisnet/CodeBrix.Terminal/blob/main/tests/CodeBrix.Terminal.Engine.Tests/EscapeSequence/CupTests.cs

  DEC copy rectangular area (DECCRA)
    https://github.com/ellisnet/CodeBrix.Terminal/blob/main/tests/CodeBrix.Terminal.Engine.Tests/EscapeSequence/DeccraTests.cs

  DEC request status string (DECRQSS)
    https://github.com/ellisnet/CodeBrix.Terminal/blob/main/tests/CodeBrix.Terminal.Engine.Tests/EscapeSequence/DecrqssTests.cs

  Device status reports and device attributes (DA1, DA2, DSR)
    https://github.com/ellisnet/CodeBrix.Terminal/blob/main/tests/CodeBrix.Terminal.Engine.Tests/EscapeSequence/DeviceStatusTests.cs

  Erase operations (ED, EL, ECH, DECERA, DECSERA)
    https://github.com/ellisnet/CodeBrix.Terminal/blob/main/tests/CodeBrix.Terminal.Engine.Tests/EscapeSequence/EraseTests.cs

  Fill rectangular area (DECFRA)
    https://github.com/ellisnet/CodeBrix.Terminal/blob/main/tests/CodeBrix.Terminal.Engine.Tests/EscapeSequence/FillRectangleTests.cs

  Insert/delete characters and columns (ICH, DCH, DECIC, DECDC)
    https://github.com/ellisnet/CodeBrix.Terminal/blob/main/tests/CodeBrix.Terminal.Engine.Tests/EscapeSequence/InsertDeleteCharTests.cs

  Insert/delete lines (IL, DL)
    https://github.com/ellisnet/CodeBrix.Terminal/blob/main/tests/CodeBrix.Terminal.Engine.Tests/EscapeSequence/InsertDeleteLineTests.cs

  Scroll up/down, index, reverse index, viewport scrolling
    https://github.com/ellisnet/CodeBrix.Terminal/blob/main/tests/CodeBrix.Terminal.Engine.Tests/EscapeSequence/ScrollTests.cs

  Select graphic rendition (bold, italic, colors, resets)
    https://github.com/ellisnet/CodeBrix.Terminal/blob/main/tests/CodeBrix.Terminal.Engine.Tests/EscapeSequence/SgrTests.cs

  Rune: column width, surrogate pairs, construction
    https://github.com/ellisnet/CodeBrix.Terminal/blob/main/tests/CodeBrix.Terminal.Text.Tests/RuneTests.cs

  Rune classification and case conversion
    https://github.com/ellisnet/CodeBrix.Terminal/blob/main/tests/CodeBrix.Terminal.Text.Tests/RuneClassificationTests.cs

  ustring operations (search, compare, slice, trim, replace)
    https://github.com/ellisnet/CodeBrix.Terminal/blob/main/tests/CodeBrix.Terminal.Text.Tests/UStringTests.cs

  ustring case conversion, concat, join and Make overloads
    https://github.com/ellisnet/CodeBrix.Terminal/blob/main/tests/CodeBrix.Terminal.Text.Tests/UStringAdditionalTests.cs

  Unicode classification and case conversion
    https://github.com/ellisnet/CodeBrix.Terminal/blob/main/tests/CodeBrix.Terminal.Text.Tests/UnicodeTests.cs

  UTF-8 encoding and decoding
    https://github.com/ellisnet/CodeBrix.Terminal/blob/main/tests/CodeBrix.Terminal.Text.Tests/Utf8Tests.cs

  Utf8 / ustring overload parity
    https://github.com/ellisnet/CodeBrix.Terminal/blob/main/tests/CodeBrix.Terminal.Text.Tests/Utf8UStringOverloadTests.cs

To read one as plain text, swap the host for raw.githubusercontent.com:

    https://raw.githubusercontent.com/ellisnet/CodeBrix.Terminal/main/tests/CodeBrix.Terminal.Engine.Tests/EscapeSequence/SgrTests.cs


================================================================================

QUICK REFERENCE CARD
====================

Install:          dotnet add package CodeBrix.Terminal.MitLicenseForever
Engine NS:        using CodeBrix.Terminal.Engine;
Text NS:          using CodeBrix.Terminal.Text;
Rune NS:          System (no using needed)
Point NS:         System.Drawing (selection and search coordinates)
Buffer collision: using TerminalBuffer = CodeBrix.Terminal.Engine.Buffer;

Create terminal:  new Terminal(null, new TerminalOptions { Cols = 80, Rows = 25 })
With a delegate:  new Terminal(new MyDelegate(), options)
Feed text:        terminal.Feed("text\r\n")
Feed bytes:       terminal.Feed(byteArray, length)
Feed escape:      terminal.Feed("\x1b[1;31m")     // bold red
Reset attrs:      terminal.Feed("\x1b[0m")
Full reset:       terminal.Reset()  /  terminal.SoftReset()
Resize:           terminal.Resize(newCols, newRows)
Scroll region:    terminal.SetScrollRegion(top, bottom)   // 0-based

Live screen row:  terminal.Buffer.Lines[terminal.Buffer.YBase + row]
Viewport row:     terminal.Buffer.Lines[terminal.Buffer.YDisp + row]
Read a cell:      line[col].Rune / .Code / .Attribute / .Width / .IsBlank
Cell by coords:   terminal.Buffer.GetChar(col, row)
Line to string:   line.TranslateToString(trimRight: true).ToString()
Decode attrs:     var (fg, bg, flags) = CharacterAttribute.Unpack(attr)
Default sentinels: CharacterAttribute.DefaultColorIndex (256),
                   InvertedDefaultColorIndex (257)
Palette:          Color.DefaultAnsiColors[index]
Nearest color:    terminal.MatchColor(r, g, b)

Damage span:      terminal.GetUpdateRange(out int startY, out int endY)
Clear damage:     terminal.ClearUpdateRange()
Scrollback:       terminal.ScrollLines(-n) / ScrollToBottom() / IsAtBottom
Viewport event:   terminal.Scrolled += (t, yDisp) => { }

Cursor:           terminal.SetCursor(col, row)   // col first
                  terminal.SaveCursor() / RestoreCursor() / ShowCursor()
Alt screen:       terminal.Buffers.ActivateAltBuffer(fillAttr) /
                  ActivateNormalBuffer(clearAlt) / IsAlternateBuffer

Encode key:       TerminalKeyEncoder.Encode(key, mods, term.ApplicationCursor)
Named key:        TerminalKeyEncoder.EncodeSpecial(key, term.ApplicationCursor)
Typed character:  TerminalKeyEncoder.EncodeComposed(codePoint, mods)
Mouse:            terminal.EncodeMouseButton(...) then SendEvent /
                  SendMouseMotion, gated on terminal.MouseMode

Selection:        new SelectionService(terminal)
                  StartSelection(row, col) / DragExtend(row, col)
                  SelectWordOrExpression(col, row)   // reversed!
                  GetSelectedText()   // End.X is EXCLUSIVE
Search:           new SearchService(terminal).GetSnapshot()
                  snapshot.FindText(term) / FindNext() / FindPrevious()

PTY (Unix/macOS): Pty.ForkAndExec(prog, args, env, out master, winSize)
                  Pty.SetWinSize(master, ref winSize)
                  Terminal.GetEnvironmentVariables(termName)

Make ustring:     ustring.Make("text")  or implicit from string
Byte length:      ustring.Length
Rune count:       ustring.RuneCount
Console width:    ustring.ConsoleWidth
Rune width:       Rune.ColumnWidth(rune)
Classify:         Rune.IsLetter(r), Rune.IsDigit(r), ...   // all STATIC
Unicode version:  Unicode.Version  ("15.0.0")

Delegate base:    extend SimpleTerminalDelegate

Dependencies:     NONE (zero NuGet dependencies)
Target:           .NET 10 or later
License:          MIT


================================================================================

END OF AGENT-README
