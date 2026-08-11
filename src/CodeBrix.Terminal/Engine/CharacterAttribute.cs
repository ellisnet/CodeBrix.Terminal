using System;

namespace CodeBrix.Terminal.Engine; //was previously: namespace XtermSharp;

// TODO: rename to CharacterAttributes or similar
[Flags]
public enum FLAGS {
    BOLD = 1,
    UNDERLINE = 2,
    BLINK = 4,
    INVERSE = 8,
    INVISIBLE = 16,
    DIM = 32,
    ITALIC = 64,
    CrossedOut = 128
}

/// <summary>
/// The three components of a packed character attribute, produced by
/// <see cref="CharacterAttribute.Unpack"/>. Foreground and background are
/// 256-color palette indices, or <see cref="CharacterAttribute.DefaultColorIndex"/> /
/// <see cref="CharacterAttribute.InvertedDefaultColorIndex"/> for cells using the
/// terminal's default colors.
/// </summary>
public readonly struct UnpackedAttribute {
    /// <summary>
    /// Gets the foreground color: a palette index into
    /// <c>Color.DefaultAnsiColors</c>, or one of the default-color sentinels.
    /// </summary>
    public int Foreground { get; }

    /// <summary>
    /// Gets the background color: a palette index into
    /// <c>Color.DefaultAnsiColors</c>, or one of the default-color sentinels.
    /// </summary>
    public int Background { get; }

    /// <summary>
    /// Gets the styling flags (bold, underline, inverse, ...).
    /// </summary>
    public FLAGS Flags { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnpackedAttribute"/> struct.
    /// </summary>
    /// <param name="foreground">The foreground palette index.</param>
    /// <param name="background">The background palette index.</param>
    /// <param name="flags">The styling flags.</param>
    public UnpackedAttribute (int foreground, int background, FLAGS flags)
    {
        Foreground = foreground;
        Background = background;
        Flags = flags;
    }

    /// <summary>
    /// Deconstructs into (foreground, background, flags).
    /// </summary>
    /// <param name="foreground">The foreground palette index.</param>
    /// <param name="background">The background palette index.</param>
    /// <param name="flags">The styling flags.</param>
    public void Deconstruct (out int foreground, out int background, out FLAGS flags)
    {
        foreground = Foreground;
        background = Background;
        flags = Flags;
    }
}

public static class CharacterAttribute {
    /// <summary>
    /// The palette index meaning "the terminal's default color" (256), as found
    /// in <see cref="UnpackedAttribute.Foreground"/> / <see cref="UnpackedAttribute.Background"/>.
    /// </summary>
    public const int DefaultColorIndex = Renderer.DefaultColor;

    /// <summary>
    /// The palette index meaning "the terminal's default color, inverted" (257) --
    /// produced by inverse video over default-colored cells.
    /// </summary>
    public const int InvertedDefaultColorIndex = Renderer.InvertedDefaultColor;

    /// <summary>
    /// Unpacks a packed <c>CharData.Attribute</c> value into its components.
    /// The packing is: bits 0-8 background palette index, bits 9-17 foreground
    /// palette index, bits 18 and up <see cref="FLAGS"/>.
    /// </summary>
    /// <param name="attribute">The packed attribute from a <see cref="CharData"/> cell.</param>
    public static UnpackedAttribute Unpack (int attribute)
    {
        return new UnpackedAttribute (
            foreground: (attribute >> 9) & 0x1ff,
            background: attribute & 0x1ff,
            flags: (FLAGS)(attribute >> 18));
    }

    // Temporary, longer term in Attribute we will add a proper encoding
    public static string ToSGR (int attribute)
    {
        var result = "0";

        var ca = (FLAGS)(attribute >> 18);
        if (ca.HasFlag (FLAGS.BOLD)) {
            result += ";1";
        }
        if (ca.HasFlag (FLAGS.UNDERLINE)) {
            result += ";4";
        }
        if (ca.HasFlag (FLAGS.BLINK)) {
            result += ";5";
        }
        if (ca.HasFlag (FLAGS.INVERSE)) {
            result += ";7";
        }
        if (ca.HasFlag (FLAGS.INVISIBLE)) {
            result += ";8";
        }

        int fg = (attribute >> 9) & 0x1ff;

        if (fg != Renderer.DefaultColor) {
            if (fg > 16) {
                result += $";38;5;{fg}";
            } else {
                if (fg >= 8) {
                    result += $";{9}{fg - 8};";
                } else {
                    result += $";{3}{fg};";
                }
            }
        }

        int bg = attribute & 0x1ff;
        if (bg != Renderer.DefaultColor) {
            if (bg > 16) {
                result += $";48;5;{bg}";
            } else {
                if (bg >= 8) {
                    result += $";{10}{bg - 8};";
                } else {
                    result += $";{4}{bg};";
                }
            }
        }

        result += "m";
        return result;
    }

}