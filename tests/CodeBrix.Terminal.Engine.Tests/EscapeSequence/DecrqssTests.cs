using SilverAssertions;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace CodeBrix.Terminal.Engine.Tests.EscapeSequence;

/// <summary>
/// Tests for DECRQSS (Request Status String) — DCS $ q Pt ST.
/// Verifies that the terminal correctly reports its current state.
/// </summary>
public class DecrqssTests : IDisposable
{
    readonly CapturingDelegate _delegate;
    readonly Terminal _terminal;

    public DecrqssTests ()
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

    [Fact]
    public void DECRQSS_ReportsScrollRegion ()
    {
        // Set scroll region to rows 5-15
        _terminal.Feed ("\x1b[5;15r");
        ClearResponse ();
        // DECRQSS for DECSTBM: DCS $ q r ST
        _terminal.Feed ("\x1bP$qr\x1b\\");

        // Response should be DCS 1 $ r <top>;<bottom> r ST
        GetResponse ().Should ().Contain ("$r5;15r");
    }

    [Fact]
    public void DECRQSS_ReportsMargins ()
    {
        // Enable margin mode and set margins
        _terminal.Feed ("\x1b[?69h"); // DECLRMM
        _terminal.Feed ("\x1b[5;30s"); // DECSLRM
        ClearResponse ();
        // DECRQSS for DECSLRM: DCS $ q s ST
        _terminal.Feed ("\x1bP$qs\x1b\\");

        // Response should contain margin values
        GetResponse ().Should ().Contain ("$r5;30s");
    }

    [Fact]
    public void DECRQSS_ReportsConformanceLevel ()
    {
        // DECRQSS for DECSCL: DCS $ q " p ST
        _terminal.Feed ("\x1bP$q\"p\x1b\\");

        // Response should report conformance level 65 (vt500)
        GetResponse ().Should ().Contain ("65;1\"p");
    }

    [Fact]
    public void DECRQSS_ReportsSGR ()
    {
        // DECRQSS for SGR: DCS $ q m ST
        _terminal.Feed ("\x1bP$qm\x1b\\");

        // Response should contain SGR rendition
        var response = GetResponse ();
        response.Should ().Contain ("$r");
        response.Should ().Contain ("m");
    }

    [Fact]
    public void DECRQSS_InvalidRequestReportsError ()
    {
        // DECRQSS with unknown request: DCS $ q X ST
        _terminal.Feed ("\x1bP$qX\x1b\\");

        // Response should indicate invalid (DCS 0 $ r)
        GetResponse ().Should ().Contain ("0$r");
    }

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
