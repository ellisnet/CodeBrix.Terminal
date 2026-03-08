using SilverAssertions;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace CodeBrix.Terminal.Engine.Tests.EscapeSequence;

/// <summary>
/// Tests for Device Status Reports (DSR — CSI n) and
/// Device Attributes (DA1 — CSI c).
/// </summary>
public class DeviceStatusTests : IDisposable
{
    readonly CapturingDelegate _delegate;
    readonly Terminal _terminal;

    public DeviceStatusTests ()
    {
        _delegate = new CapturingDelegate ();
        _terminal = new Terminal (_delegate);
    }

    public void Dispose ()
    {
    }

    string GetResponse ()
    {
        return Encoding.UTF8.GetString (_delegate.GetResponseBytes ());
    }

    void ClearResponse ()
    {
        _delegate.Clear ();
    }

    #region CSI n — Device Status Report (DSR)

    [Fact]
    public void DSR_StatusReport ()
    {
        // CSI 5 n — Status Report, should respond CSI 0 n ("OK")
        _terminal.Feed ("\x1b[5n");
        GetResponse ().Should ().Be ("\x1b[0n");
    }

    [Fact]
    public void DSR_CursorPositionReport ()
    {
        // Move cursor to row 5, col 10 (1-based)
        _terminal.Feed ("\x1b[5;10H");
        ClearResponse ();
        // CSI 6 n — Report Cursor Position
        _terminal.Feed ("\x1b[6n");
        GetResponse ().Should ().Be ("\x1b[5;10R");
    }

    [Fact]
    public void DSR_CursorPositionReport_AtOrigin ()
    {
        // Default position (1,1)
        _terminal.Feed ("\x1b[6n");
        GetResponse ().Should ().Be ("\x1b[1;1R");
    }

    [Fact]
    public void DSR_DEC_PrinterStatus ()
    {
        // CSI ? 15 n — Request Printer status
        _terminal.Feed ("\x1b[?15n");
        GetResponse ().Should ().Contain ("10n");
    }

    [Fact]
    public void DSR_DEC_UdkStatus ()
    {
        // CSI ? 25 n — Request UDK status
        _terminal.Feed ("\x1b[?25n");
        GetResponse ().Should ().Contain ("21n");
    }

    [Fact]
    public void DSR_DEC_KeyboardStatus ()
    {
        // CSI ? 26 n — Request Keyboard status
        _terminal.Feed ("\x1b[?26n");
        GetResponse ().Should ().Contain ("27;1;0;0n");
    }

    #endregion

    #region CSI c — Device Attributes (DA1)

    [Fact]
    public void DA1_ReportsVt100Attributes ()
    {
        // CSI 0 c — Request Primary DA (xterm default term name)
        _terminal.Feed ("\x1b[0c");
        // Default termName is "xterm", should respond with VT100 attributes
        GetResponse ().Should ().Contain ("?1;2c");
    }

    [Fact]
    public void DA1_DefaultParamReportsAttributes ()
    {
        // CSI c (no param) — same as CSI 0 c
        _terminal.Feed ("\x1b[c");
        GetResponse ().Should ().Contain ("?1;2c");
    }

    [Fact]
    public void DA2_ReportsSecondaryAttributes ()
    {
        // CSI > 0 c — Request Secondary DA
        _terminal.Feed ("\x1b[>0c");
        // Should respond with VT510 identification
        GetResponse ().Should ().Contain (">61;20;1c");
    }

    #endregion

    class CapturingDelegate : SimpleTerminalDelegate
    {
        readonly List<byte> _bytes = new ();

        public override void Send (byte[] data)
        {
            _bytes.AddRange (data);
        }

        public byte[] GetResponseBytes ()
        {
            return _bytes.ToArray ();
        }

        public void Clear ()
        {
            _bytes.Clear ();
        }
    }
}
