# CodeBrix.Terminal

A .NET terminal emulation engine with Unicode text support.

CodeBrix.Terminal is a .NET (10 or higher) library that provides a virtual terminal (VT100/VT220/VT400/xterm-compatible) emulation engine, including a full ANSI/DEC escape sequence parser, terminal buffer management, and Unicode text utilities. It can be used to build terminal emulator UIs, process terminal output programmatically, or integrate terminal functionality into .NET applications. It has no dependencies, other than the .NET runtime.

CodeBrix.Terminal is provided as a .NET 10 library and associated `CodeBrix.Terminal.MitLicenseForever` NuGet package.

CodeBrix.Terminal supports applications and assemblies that target Microsoft .NET version 10.0 and later. Microsoft .NET version 10.0 is a Long-Term Supported (LTS) version of .NET, and was released on Nov 11, 2025; and will be actively supported by Microsoft until Nov 14, 2028. Please update your C#/.NET code and projects to the latest LTS version of Microsoft .NET.

## Installation

```
dotnet add package CodeBrix.Terminal.MitLicenseForever
```

This repository produces exactly ONE NuGet package. Note that the package ID and the namespaces are different - there is no package named plain `CodeBrix.Terminal`, and none named `CodeBrix.Terminal.Text`:

* NuGet package ID: `CodeBrix.Terminal.MitLicenseForever`
* Assembly: `CodeBrix.Terminal`
* Namespaces inside that one assembly: `CodeBrix.Terminal.Engine` (the emulation engine) and `CodeBrix.Terminal.Text` (the Unicode text utilities) - i.e. `using CodeBrix.Terminal.Engine;` and `using CodeBrix.Terminal.Text;`

`CodeBrix.Terminal.Text` is a namespace, not a separate package: adding the one package above gives you both.

XML documentation (IntelliSense) ships alongside the assembly.

The package has no NuGet dependencies at all - nothing beyond .NET itself is pulled in.

## CodeBrix.Terminal supports:

* VT100/VT220/VT400/xterm-compatible escape sequence parsing and handling
* Terminal buffer management with scrollback history
* Cursor positioning and movement (CUP, CUU, CUD, CUF, CUB, etc.)
* Scroll regions (DECSTBM) and left/right margins (DECSLRM)
* Scroll up (SU) and scroll down (SD) operations
* Character insert and delete (ICH, DCH, DECIC, DECDC)
* Line insert and delete (IL, DL)
* Erase operations (ED, EL, ECH, DECERA, DECSERA)
* Rectangular area operations (DECCRA, DECFRA, DECRQCRA)
* SGR character attributes (bold, italic, underline, blink, inverse, dim, invisible)
* 8-color, 16-color (bright/aixterm), and 256-color foreground and background
* Device status reports (DSR) and device attributes (DA1, DA2)
* DCS status string reports (DECRQSS)
* Terminal modes (DECSET/DECRESET) including origin mode, wraparound, and bracketed paste
* Mouse tracking modes and protocol encodings
* Normal and alternate screen buffers
* Tab stop management
* Character set designation and selection (G0-G3)
* Selection and search services
* Terminal resize with content reflow
* Pseudo-terminal (PTY) fork and exec support (Unix)

## CodeBrix.Terminal.Text additionally supports:

* Unicode character classification (upper, lower, title, digit, graphic, etc.)
* Unicode case conversion (upper, lower, title) with special-case and locale support
* Simple case folding
* Unicode range table lookups
* Rune (code point) handling and column width calculation
* UTF-8 string type (`ustring`) with encoding and decoding

## Sample Code

### Create a Terminal and Process Input

```csharp
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
```

### Set Up Scroll Regions

```csharp
using CodeBrix.Terminal.Engine;

var terminal = new Terminal(null, new TerminalOptions { Cols = 80, Rows = 25 });

// Set a scroll region (rows 5-20)
terminal.Feed("\x1b[5;20r");

// Write content, and scrolling will be confined to the region
for (int i = 0; i < 30; i++)
{
    terminal.Feed($"Line {i}\r\n");
}
```

### Implement a Terminal Delegate

```csharp
using CodeBrix.Terminal.Engine;

// Create a custom delegate to handle terminal events
public class MyTerminalDelegate : SimpleTerminalDelegate
{
    public override void Send(byte[] data)
    {
        // Handle data sent back from the terminal (e.g., device status responses)
        Console.WriteLine($"Terminal sent {data.Length} bytes");
    }

    public override void SizeChanged(Terminal source)
    {
        Console.WriteLine($"Terminal resized to {source.Cols}x{source.Rows}");
    }
}

var terminal = new Terminal(new MyTerminalDelegate(), new TerminalOptions { Cols = 120, Rows = 40 });
```

## Documentation

The NuGet package includes `AGENT-README.txt`, a complete API reference and usage guide written for AI coding agents - point your agent at that file when it is writing code against this library.

Additional sample code and usage examples are available in the `CodeBrix.Terminal.Engine.Tests` and `CodeBrix.Terminal.Text.Tests` projects:
https://github.com/ellisnet/CodeBrix.Terminal/tree/main/tests

A complete working application - a remote terminal client and its server - is in the
[samples](https://github.com/ellisnet/CodeBrix.Terminal/tree/main/samples) directory.

## License

CodeBrix.Terminal is licensed under the MIT License - see the
[LICENSE](https://github.com/ellisnet/CodeBrix.Terminal/blob/main/LICENSE) file.

For licensing and provenance information about the open source code included in
this package, see [THIRD-PARTY-NOTICES.txt](https://github.com/ellisnet/CodeBrix.Terminal/blob/main/THIRD-PARTY-NOTICES.txt).
