================================================================================
EXTRAS-README: CodeBrix.Terminal
Samples, tools and other content in this repository that is not part of a
NuGet package
================================================================================

Nothing described here ships in the CodeBrix.Terminal.MitLicenseForever
package. It is all here to demonstrate and verify the library.


THE REMOTE TERMINAL SAMPLE (samples/)
=====================================
Three projects that together form one end-to-end demonstration: a WinUI
client that renders a terminal, talking over SignalR to an ASP.NET Core
server that runs the real process.

    samples/RemoteTerminal.Shared/
        A plain net10.0 class library holding the message contracts the
        client and server exchange: ProcessRegisterRequest,
        ProcessRegisterResponse, ProcessStartResponse and
        ProcessOutputResponse.

    samples/RemoteTerminal.Server/
        An ASP.NET Core (Microsoft.NET.Sdk.Web) host, net10.0. It exposes
        Hubs/RemoteConnectionHub.cs over SignalR, starts and tracks child
        processes (Processing/RegisteredProcess.cs), and guards the hub with
        a shared-key scheme (Auth/AuthKey.cs, AuthKeyCryptor.cs,
        AuthSettings.cs, SharedKeyAuthHandler.cs). It references only
        RemoteTerminal.Shared -- the server side does not use the terminal
        engine.

        Configuration: appsettings.json -> HubHosting.BaseHostingUrl and
        HubHosting.HostingTcpPort (http://localhost:5555 out of the box).

        Run it with:
            dotnet run --project samples/RemoteTerminal.Server

    samples/RemoteTerminal.Client/
        A WinUI 3 / Windows App SDK application, net10.0-windows10.0.19041.0,
        platforms x86/x64/ARM64. WINDOWS ONLY -- it cannot build on Linux or
        macOS, which is why CodeBrix.Terminal.Testing.slnx leaves it out.

        It project-references src/CodeBrix.Terminal directly (not the NuGet
        package) and is the reference implementation of a renderer:
        Controls/TerminalControl.xaml(.cs) paints the buffer, and
        ViewModels/MainViewModel.cs drives the SignalR connection.

        Configuration: appsettings.json -> ServerConnection.RemoteUrl and
        ServerConnection.AuthToken. The checked-in token pairs with the
        sample server's default configuration; it is demo material, not a
        secret worth protecting.

        Run it from Visual Studio on Windows, with the server started first.

The SignalR transport, the process management and the auth key all belong to
the sample, NOT to the package: CodeBrix.Terminal itself has no networking
and spawns no processes (outside Pty on Unix/macOS).


TESTS (tests/)
==============
    tests/CodeBrix.Terminal.Engine.Tests/
    tests/CodeBrix.Terminal.Text.Tests/

The two library test suites. They are not packed and not referenced by
consumers, but they are the best available body of compiling example code
for the package, and AGENT-README.txt points consumers at them from its
"WORKING EXAMPLES ON GITHUB" section.

    tests/RemoteTerminal.AuthKey.Tests/

Tests for the SAMPLE server's shared-key helper
(samples/RemoteTerminal.Server/Auth/AuthKeyCryptor.cs). It references the
sample server, not the library, so it verifies sample code only.

Run everything with:

    dotnet test CodeBrix.Terminal.Testing.slnx

There are no opt-in environment variables and no external fixture data.
