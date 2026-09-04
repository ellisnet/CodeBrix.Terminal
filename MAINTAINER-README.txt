================================================================================
MAINTAINER-README: CodeBrix.Terminal
Notes for people and agents MAINTAINING this repository -- not for package
consumers
================================================================================

If you are CONSUMING the NuGet package, stop here and read AGENT-README.txt
instead. This file covers building, testing, packaging and the vendored
provenance of the repository itself.


PURPOSE AND SCOPE
=================
This repository produces exactly one NuGet package:

    PackageId : CodeBrix.Terminal.MitLicenseForever
    Assembly  : CodeBrix.Terminal
    Project   : src/CodeBrix.Terminal/CodeBrix.Terminal.csproj
    License   : MIT (PackageLicenseExpression), PackageRequireLicenseAcceptance
    Consumer documentation: AGENT-README.txt (repo root)

Everything else in the repository -- the RemoteTerminal sample trio and the
three test projects -- exists to exercise, demonstrate and verify that one
package. None of it is packed. See EXTRAS-README.txt for the samples.


REPOSITORY LAYOUT
=================
    AGENT-README.txt                    Consumer guide for the package.
    MAINTAINER-README.txt               This file.
    EXTRAS-README.txt                   Samples and other non-package content.
    README-INDEX.txt                    Map of the README files.
    README.md                           Human-facing overview; ships in the nupkg.
    LICENSE                             MIT text.
    THIRD-PARTY-NOTICES.txt             Upstream attribution; ships in the nupkg.
    icon-codebrix-128.png               Package icon; ships in the nupkg.
    CodeBrix.Terminal.slnx              Full solution (includes the WinUI client).
    CodeBrix.Terminal.Testing.slnx      Build/test solution WITHOUT the WinUI
                                        client -- see TESTING.
    global.json                         Selects the Microsoft.Testing.Platform
                                        test runner. Does NOT pin an SDK
                                        version. See BUILDING and TESTING.

Both solutions carry a "Solution Items" folder holding the root files and a
"Tests" folder holding the three test projects. Both list the same ten root
files: .gitignore, AGENT-README.txt, EXTRAS-README.txt, global.json,
icon-codebrix-128.png, LICENSE, MAINTAINER-README.txt, README-INDEX.txt,
README.md and THIRD-PARTY-NOTICES.txt. Keep the two lists in step when a root
file is added or removed. The solutions differ only in their project list:
CodeBrix.Terminal.Testing.slnx omits the WinUI client -- see TESTING.
    src/CodeBrix.Terminal/              The library.
      Engine/                           Terminal engine (XtermSharp fork).
      Engine/InputHandlers/             CSI/mode/status command implementations.
      Engine/Utils/                     CircularList, RuneExt.
      Engine/Renderer/                  Renderer (the two color constants).
      Text/                             Unicode text utilities (NStack derived).
    samples/RemoteTerminal.Client/      WinUI 3 client (Windows only).
    samples/RemoteTerminal.Server/      ASP.NET Core + SignalR host.
    samples/RemoteTerminal.Shared/      Message contracts shared by the two.
    tests/CodeBrix.Terminal.Engine.Tests/
    tests/CodeBrix.Terminal.Text.Tests/
    tests/RemoteTerminal.AuthKey.Tests/ Tests for the SAMPLE server's auth key.

IMPORTANT: folder names are NOT namespaces here. Engine/Utils, Engine/Renderer
and most of Engine/InputHandlers all declare
`namespace CodeBrix.Terminal.Engine;`. The only two sub-namespaces are
CodeBrix.Terminal.Engine.CommandExtensions and
CodeBrix.Terminal.Engine.CsiCommandExtensions (both entirely internal). The
consumer guide states the absence of a "CodeBrix.Terminal.Engine.Utils"
namespace as a fact -- if a file there is ever given its own namespace, that
document must change with it.

Rune, RuneExtensions and Rune.ColumnWidth declare `namespace System;`
deliberately, so consumers get them without a using. Moving them would be a
breaking change for every consumer.


BUILDING
========
    dotnet restore CodeBrix.Terminal.Testing.slnx
    dotnet build   CodeBrix.Terminal.Testing.slnx

global.json at the repo root does NOT pin an SDK version, so the newest
installed .NET 10 SDK is still used. It exists solely to select the test
runner:

    { "test": { "runner": "Microsoft.Testing.Platform" } }

Because that setting lives in global.json rather than in the csprojs, it
applies to every `dotnet test` run anywhere in the repository, including CI.
Keep the file committed -- see TESTING.

The library targets net10.0 with <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
(the parser's print and DCS paths take byte pointers). Both engine test
projects set AllowUnsafeBlocks too.

Use CodeBrix.Terminal.slnx only on Windows: it includes
samples/RemoteTerminal.Client, a WinUI 3 / Windows App SDK project targeting
net10.0-windows10.0.19041.0 with x86/x64/ARM64 platforms and MSIX tooling. It
cannot build on Linux or macOS.


TESTING
=======
    dotnet test CodeBrix.Terminal.Testing.slnx

THE TEST RUNNER IS Microsoft.Testing.Platform (MTP), selected by global.json at
the repo root. Do not delete that file; without it, `dotnet test` falls back to
the older VSTest bridge. You can tell which one ran: MTP output ends in a
"Test run summary:" block, while the VSTest bridge invokes MSBuild with
`--target:VSTest`. None of the three test projects carries a coverage
collector.

CodeBrix.Terminal.Testing.slnx exists precisely so that build-and-test works
off Windows: it is the full solution minus the WinUI client project. Always
prefer it for CI and for agent-driven verification; reaching for
CodeBrix.Terminal.slnx on a non-Windows host will fail on the client project,
not on anything real.

Test projects:
    tests/CodeBrix.Terminal.Engine.Tests   xunit.v3 + xunit.runner.visualstudio
                                           + Microsoft.NET.Test.Sdk +
                                           SilverAssertions
    tests/CodeBrix.Terminal.Text.Tests     xunit.v3 (no SilverAssertions)
    tests/RemoteTerminal.AuthKey.Tests     tests the SAMPLE server's auth key
                                           helper; references
                                           samples/RemoteTerminal.Server

There are no opt-in environment variables, no external fixture data and no
network access in the engine or text suites.

Internal API reaches the tests through src/CodeBrix.Terminal/InternalVisibleTo.cs
(note the file name: InternalVisibleTo.cs, no "s"), which grants:

    CodeBrix.Terminal.Text.Tests
    CodeBrix.Terminal.Engine.Tests

Engine escape-sequence tests live under
tests/CodeBrix.Terminal.Engine.Tests/EscapeSequence/, one file per sequence
family, with shared helpers in TerminalTestExtensions.cs and
CsiCommandCodes.cs.


PACKAGING AND PUBLISHING
========================
Packing is driven from the library csproj, not from a script:

    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>

so every build of the library produces a .nupkg.

Versioning is date-stamped and auto-incrementing, computed in the csproj from
System.DateTime.UtcNow:

    1.<years since _VersionBaseYear>.<day of year>.<minute of day UTC>

Consequences to remember:
  - the value strictly increases over time;
  - every build produces a new version, so a fresh .nupkg per build;
  - two builds in the SAME UTC minute produce the SAME version -- never
    publish two packages from within one minute;
  - this is not SemVer: major is pinned and minor encodes the year, so
    neither signals API compatibility;
  - re-baseline by changing _VersionBaseYear.

Files that ship inside the nupkg (all via <None Include ... Pack="true">):

    icon-codebrix-128.png     (PackageIcon)
    README.md                 (PackageReadmeFile)
    AGENT-README.txt          <- the consumer guide
    THIRD-PARTY-NOTICES.txt

AGENT-README.txt is therefore a shipped artifact: an edit to it changes the
package contents. MAINTAINER-README.txt, EXTRAS-README.txt and
README-INDEX.txt are NOT packed.

The package has no NuGet dependencies at all, and that is a documented
selling point -- think hard before adding one.


PROVENANCE AND VENDORED SOURCES
===============================
Two upstream bodies of code were vendored and re-namespaced:

  - The terminal engine (src/CodeBrix.Terminal/Engine) is a fork of XtermSharp
    by Miguel de Icaza (itself a port of xterm.js), MIT licensed.
    Namespace XtermSharp -> CodeBrix.Terminal.Engine.
  - The Unicode text utilities (src/CodeBrix.Terminal/Text) derive from NStack
    version 1.1.1. Namespace NStack -> CodeBrix.Terminal.Text, except Rune and
    RuneExtensions, which are deliberately placed in System.

Vendored-source conventions:
  - Every re-namespaced file carries a trailing marker on its namespace line,
    e.g.
        namespace CodeBrix.Terminal.Engine; //was previously: namespace XtermSharp;
        namespace CodeBrix.Terminal.Text; //Was previously: namespace NStack;
    Preserve those markers when editing; they are how a maintainer traces a
    file back upstream.
  - Editing the vendored sources in place is expected -- this is a fork, not a
    submodule.
  - Attribution and the upstream licenses live in THIRD-PARTY-NOTICES.txt,
    which ships in the package. Update it if either baseline moves.
  - The upstream brace and spacing style (opening brace on the same line,
    a space before the parameter list) survives in the vendored files. Match
    the surrounding file rather than reformatting it; a whole-file reformat
    destroys the diff against upstream.
  - Deliberate divergences so far: the terminal mode properties have internal
    setters (they are read-only to consumers), the Unicode tables are on
    Unicode 15.0.0, and TerminalKey / TerminalModifiers / TerminalKeyEncoder
    are additions with no upstream counterpart. The consumer guide states all
    of these as facts.


CODING CONVENTIONS
==================
  - net10.0 only.
  - Nullable reference-type annotations are OFF for this repository; do not
    add `?` to reference types.
  - <AllowUnsafeBlocks>true</AllowUnsafeBlocks> is required by the library and
    by the engine test projects.
  - In vendored files, follow the surrounding upstream style; in new files
    (for example TerminalKeyEncoder.cs) use the normal CodeBrix style.
  - Test files are named <Area>Tests.cs; escape-sequence tests go under
    tests/CodeBrix.Terminal.Engine.Tests/EscapeSequence/ named after the
    sequence they cover (SgrTests, CupTests, ...).


NOTES
=====
  - src/CodeBrix.Terminal/Engine/GlobalSuppressions.cs still targets a member
    under an "Application.EscapeSequenceParser" name from the upstream tree;
    it is inert, kept only to avoid churn.
  - CharSet (in Engine/CharSets.cs) is an empty public class carried over from
    upstream. It is documented to consumers as "ignore it"; removing it would
    be a (harmless) breaking change, so leave it alone unless there is a
    reason.
  - Renderer (Engine/Renderer/Renderer.cs) exists only to hold DefaultColor
    (256) and InvertedDefaultColor (257). CharacterAttribute re-exports them as
    DefaultColorIndex / InvertedDefaultColorIndex, which is what consumers are
    told to use.
  - ReflowStrategy, ReflowNarrower, ReflowWider, ReadingBuffer, InputHandler,
    DECRQSS and everything in the CommandExtensions / CsiCommandExtensions
    namespaces are internal. Keep them that way: the consumer guide tells
    agents there is nothing for them there.
  - System.Drawing.Point is used by SelectionService and SearchSnapshot. It
    comes from the shared framework, not a package reference, so the
    "zero dependencies" claim in AGENT-README.txt still holds.
  - src/CodeBrix.Terminal/bin/Debug contains previously built .nupkg files
    from local builds. They are build output, not release artifacts.
