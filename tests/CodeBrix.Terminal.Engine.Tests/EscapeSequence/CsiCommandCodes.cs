namespace CodeBrix.Terminal.Engine.Tests.EscapeSequence;

/// <summary>
/// CSI mode constants used in DECSET/DECRESET commands,
/// equivalent to CsiCommandCodes in XtermSharp.Tests.
/// </summary>
internal static class CsiCommandCodes
{
    /// <summary>Origin Mode (DECOM) — Ps = 6</summary>
    public const int DECOM = 6;

    /// <summary>Wraparound Mode (DECAWM) — Ps = 7</summary>
    public const int DECAWM = 7;

    /// <summary>Reverse-wraparound Mode — Ps = 45</summary>
    public const int ReverseWraparound = 45;

    /// <summary>Left and Right Margin Mode (DECLRMM) — Ps = 69</summary>
    public const int DECLRMM = 69;
}
