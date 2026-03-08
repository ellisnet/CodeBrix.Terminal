using System;
using Xunit;

namespace CodeBrix.Terminal.Text.Tests;

public class Utf8Tests
{
    struct RuneMap
    {
        public uint Rune;
        public byte [] Bytes;

        public RuneMap (uint rune, params byte [] bytes)
        {
            Rune = rune;
            Bytes = bytes;
        }
    }

    ustring [] testStrings =
    [
        ustring.Empty,
        ustring.Make ("abcd"),
        ustring.Make (0xE2, 0x98, 0xBA, 0xE2, 0x98, 0xBB, 0xE2, 0x98, 0xB9),
        ustring.Make (
            0xE6, 0x97, 0xA5, 0x61, 0xE6, 0x9C, 0xAC, 0x62, 0xE8, 0xAA,
            0x9E, 0xC3, 0xA7, 0xE6, 0x97, 0xA5, 0xC3, 0xB0, 0xE6, 0x9C,
            0xAC, 0xC3, 0x8A, 0xE8, 0xAA, 0x9E, 0xC3, 0xBE, 0xE6, 0x97,
            0xA5, 0xC2, 0xA5, 0xE6, 0x9C, 0xAC, 0xC2, 0xBC, 0xE8, 0xAA,
            0x9E, 0x69, 0xE6, 0x97, 0xA5, 0xC2, 0xA9),
        ustring.Make (
            0xE6, 0x97, 0xA5, 0x61, 0xE6, 0x9C, 0xAC, 0x62, 0xE8, 0xAA,
            0x9E, 0xC3, 0xA7, 0xE6, 0x97, 0xA5, 0xC3, 0xB0, 0xE6, 0x9C,
            0xAC, 0xC3, 0x8A, 0xE8, 0xAA, 0x9E, 0xC3, 0xBE, 0xE6, 0x97,
            0xA5, 0xC2, 0xA5, 0xE6, 0x9C, 0xAC, 0xC2, 0xBC, 0xE8, 0xAA,
            0x9E, 0x69, 0xE6, 0x97, 0xA5, 0xC2, 0xA9,
            0xE6, 0x97, 0xA5, 0x61, 0xE6, 0x9C, 0xAC, 0x62, 0xE8, 0xAA,
            0x9E, 0xC3, 0xA7, 0xE6, 0x97, 0xA5, 0xC3, 0xB0, 0xE6, 0x9C,
            0xAC, 0xC3, 0x8A, 0xE8, 0xAA, 0x9E, 0xC3, 0xBE, 0xE6, 0x97,
            0xA5, 0xC2, 0xA5, 0xE6, 0x9C, 0xAC, 0xC2, 0xBC, 0xE8, 0xAA,
            0x9E, 0x69, 0xE6, 0x97, 0xA5, 0xC2, 0xA9,
            0xE6, 0x97, 0xA5, 0x61, 0xE6, 0x9C, 0xAC, 0x62, 0xE8, 0xAA,
            0x9E, 0xC3, 0xA7, 0xE6, 0x97, 0xA5, 0xC3, 0xB0, 0xE6, 0x9C,
            0xAC, 0xC3, 0x8A, 0xE8, 0xAA, 0x9E, 0xC3, 0xBE, 0xE6, 0x97,
            0xA5, 0xC2, 0xA5, 0xE6, 0x9C, 0xAC, 0xC2, 0xBC, 0xE8, 0xAA,
            0x9E, 0x69, 0xE6, 0x97, 0xA5, 0xC2, 0xA9),
        ustring.Make (0x80, 0x80, 0x80, 0x80)
    ];

    RuneMap [] runeMap =
    [
        new RuneMap (0x0000, 0x00),
        new RuneMap (0x0001, 0x01),
        new RuneMap (0x007e, 0x7e),
        new RuneMap (0x007f, 0x7f),
        new RuneMap (0x0080, 0xc2, 0x80),
        new RuneMap (0x0081, 0xc2, 0x81),
        new RuneMap (0x00bf, 0xc2, 0xbf),
        new RuneMap (0x00c0, 0xc3, 0x80),
        new RuneMap (0x00c1, 0xc3, 0x81),
        new RuneMap (0x00c8, 0xc3, 0x88),
        new RuneMap (0x00d0, 0xc3, 0x90),
        new RuneMap (0x00e0, 0xc3, 0xa0),
        new RuneMap (0x00f0, 0xc3, 0xb0),
        new RuneMap (0x00f8, 0xc3, 0xb8),
        new RuneMap (0x00ff, 0xc3, 0xbf),
        new RuneMap (0x0100, 0xc4, 0x80),
        new RuneMap (0x07ff, 0xdf, 0xbf),
        new RuneMap (0x0400, 0xd0, 0x80),
        new RuneMap (0x0800, 0xe0, 0xa0, 0x80),
        new RuneMap (0x0801, 0xe0, 0xa0, 0x81),
        new RuneMap (0x1000, 0xe1, 0x80, 0x80),
        new RuneMap (0xd000, 0xed, 0x80, 0x80),
        new RuneMap (0xd7ff, 0xed, 0x9f, 0xbf), // last code point before surrogate half
        new RuneMap (0xe000, 0xee, 0x80, 0x80), // first code point after surrogate half
        new RuneMap (0xfffe, 0xef, 0xbf, 0xbe),
        new RuneMap (0xffff, 0xef, 0xbf, 0xbf),
        new RuneMap (0x10000, 0xf0, 0x90, 0x80, 0x80),
        new RuneMap (0x10001, 0xf0, 0x90, 0x80, 0x81),
        new RuneMap (0x40000, 0xf1, 0x80, 0x80, 0x80),
        new RuneMap (0x10fffe, 0xf4, 0x8f, 0xbf, 0xbe),
        new RuneMap (0x10ffff, 0xf4, 0x8f, 0xbf, 0xbf),
        new RuneMap (0xFFFD, 0xef, 0xbf, 0xbd)
    ];

    RuneMap [] surrogateMap =
    [
        new RuneMap (0xd800, 0xed, 0xa0, 0x80),
        new RuneMap (0xdfff, 0xed, 0xbf, 0xbf),
    ];

    [Fact]
    public void TestFullRune ()
    {
        foreach (var rm in runeMap) {
            Assert.True (Utf8.FullRune (rm.Bytes), $"Error with FullRune on ({BitConverter.ToString (rm.Bytes)})");
            Assert.True (Utf8.FullRune (ustring.Make (rm.Bytes)), $"Error with FullRune(ustring) on {BitConverter.ToString (rm.Bytes)}");

            var brokenSequence = new byte [rm.Bytes.Length - 1];
            Array.Copy (rm.Bytes, brokenSequence, rm.Bytes.Length - 1);
            Assert.False (Utf8.FullRune (brokenSequence), "Expected false for a partial sequence");
            Assert.False (Utf8.FullRune (ustring.Make (brokenSequence)), "Expected false for a partial sequence");
        }
        Assert.True (Utf8.FullRune (ustring.Make (0xc0)));
        Assert.True (Utf8.FullRune (ustring.Make (0xc0)));
    }

    [Fact]
    public void TestEncodeRune ()
    {
        var result = new byte [10];
        Utf8.EncodeRune (0x10000, result);

        foreach (var rm in runeMap) {
            var n = Utf8.EncodeRune (rm.Rune, result);
            for (int i = 0; i < rm.Bytes.Length; i++)
                Assert.Equal (rm.Bytes [i], result [i]);
        }
    }

    [Fact]
    public void TestDecodeRune ()
    {
        foreach (var rm in runeMap) {
            var buffer = rm.Bytes;

            (var rune, var size) = Utf8.DecodeRune (buffer);
            Assert.Equal (rm.Rune, rune);
            Assert.Equal (rm.Bytes.Length, size);

            (rune, size) = Utf8.DecodeRune (ustring.Make (rm.Bytes));
            Assert.Equal (rm.Rune, rune);
            Assert.Equal (rm.Bytes.Length, size);

            // Add trailing zero
            var buffer2 = new byte [rm.Bytes.Length + 1];
            Array.Copy (buffer, buffer2, rm.Bytes.Length);

            (rune, size) = Utf8.DecodeRune (buffer2);
            Assert.Equal (rm.Rune, rune);
            Assert.Equal (rm.Bytes.Length, size);

            (rune, size) = Utf8.DecodeRune (ustring.Make (buffer2));
            Assert.Equal (rm.Rune, rune);
            Assert.Equal (rm.Bytes.Length, size);

            // Try removing one byte
            var wantsize = 1;
            if (wantsize >= rm.Bytes.Length)
                wantsize = 0;

            var buffer3 = new byte [rm.Bytes.Length - 1];
            Array.Copy (buffer, buffer3, buffer3.Length);

            (rune, size) = Utf8.DecodeRune (buffer3);
            Assert.Equal (Utf8.RuneError, rune);
            Assert.Equal (wantsize, size);

            // Make sure bad sequences fail
            var buffer4 = (byte [])rm.Bytes.Clone ();
            if (buffer4.Length == 1)
                buffer4 [0] = 0x80;
            else
                buffer4 [buffer4.Length - 1] = 0x7f;

            (rune, size) = Utf8.DecodeRune (buffer4);
            Assert.Equal (Utf8.RuneError, rune);
            Assert.Equal (1, size);

            (rune, size) = Utf8.DecodeRune (ustring.Make (buffer4));
            Assert.Equal (Utf8.RuneError, rune);
            Assert.Equal (1, size);
        }
    }

    [Fact]
    public void TestDecodeSurrogateRune ()
    {
        foreach (var rm in surrogateMap) {
            (var rune, var size) = Utf8.DecodeRune (rm.Bytes);
            Assert.Equal (Utf8.RuneError, rune);
            Assert.Equal (1, size);

            (rune, size) = Utf8.DecodeRune (ustring.Make (rm.Bytes));
            Assert.Equal (Utf8.RuneError, rune);
            Assert.Equal (1, size);
        }
    }

    byte [] Subset (byte [] source, int start, int end)
    {
        if (end == -1)
            end = source.Length;
        var n = end - start;
        var result = new byte [n];
        Array.Copy (source, start, result, 0, n);
        return result;
    }

    void TestSequence (ustring s)
    {
        var index = new (int idx, uint rune) [s.Length];
        var si = 0;
        var j = 0;

        foreach ((var i, var rune) in s.Range ()) {
            Assert.Equal (si, i);
            index [j] = (i, rune);
            j++;

            (var r1, var size1) = Utf8.DecodeRune (Subset (s.ToByteArray (), i, -1));
            Assert.Equal (rune, r1);

            (var r2, var size2) = Utf8.DecodeRune (s [i, null]);
            Assert.Equal (size1, size2);

            si += size1;
        }

        j--;
        for (si = s.Length; si > 0;) {
            (var r1, var size1) = Utf8.DecodeLastRune (Subset (s.ToByteArray (), 0, si));
            (var r2, var size2) = Utf8.DecodeLastRune (ustring.Make (Subset (s.ToByteArray (), 0, si)));
            (var r3, var size3) = Utf8.DecodeLastRune (s [0, si]);

            Assert.Equal (size1, size2);
            Assert.Equal (size1, size3);
            Assert.Equal (index [j].rune, r1);
            Assert.Equal (index [j].rune, r2);
            Assert.Equal (index [j].rune, r3);

            si -= size1;
            Assert.Equal (index [j].idx, si);
            j--;
        }
        Assert.Equal (0, si);
    }

    [Fact]
    public void TestSequencing ()
    {
        TestSequence (ustring.Make ("abcd"));

        foreach (var ts in testStrings) {
            foreach (var m in runeMap) {
                var variations = new ustring [] {
                    ts + ustring.Make (m.Bytes),
                    ustring.Make (m.Bytes) + ts,
                    ts + ustring.Make (m.Bytes) + ts
                };

                foreach (var x in variations)
                    TestSequence (x);
            }
        }
    }

    ustring [] invalidSequenceTests =
    [
        ustring.Make (0xed, 0xa0, 0x80, 0x80), // surrogate min
        ustring.Make (0xed, 0xbf, 0xbf, 0x80), // surrogate max
        // xx
        ustring.Make (0x91, 0x80, 0x80, 0x80),
        // s1
        ustring.Make (0xC2, 0x7F, 0x80, 0x80),
        ustring.Make (0xC2, 0xC0, 0x80, 0x80),
        ustring.Make (0xDF, 0x7F, 0x80, 0x80),
        ustring.Make (0xDF, 0xC0, 0x80, 0x80),
        // s2
        ustring.Make (0xE0, 0x9F, 0xBF, 0x80),
        ustring.Make (0xE0, 0xA0, 0x7F, 0x80),
        ustring.Make (0xE0, 0xBF, 0xC0, 0x80),
        ustring.Make (0xE0, 0xC0, 0x80, 0x80),
        // s3
        ustring.Make (0xE1, 0x7F, 0xBF, 0x80),
        ustring.Make (0xE1, 0x80, 0x7F, 0x80),
        ustring.Make (0xE1, 0xBF, 0xC0, 0x80),
        ustring.Make (0xE1, 0xC0, 0x80, 0x80),
        // s4
        ustring.Make (0xED, 0x7F, 0xBF, 0x80),
        ustring.Make (0xED, 0x80, 0x7F, 0x80),
        ustring.Make (0xED, 0x9F, 0xC0, 0x80),
        ustring.Make (0xED, 0xA0, 0x80, 0x80),
        // s5
        ustring.Make (0xF0, 0x8F, 0xBF, 0xBF),
        ustring.Make (0xF0, 0x90, 0x7F, 0xBF),
        ustring.Make (0xF0, 0x90, 0x80, 0x7F),
        ustring.Make (0xF0, 0xBF, 0xBF, 0xC0),
        ustring.Make (0xF0, 0xBF, 0xC0, 0x80),
        ustring.Make (0xF0, 0xC0, 0x80, 0x80),
        // s6
        ustring.Make (0xF1, 0x7F, 0xBF, 0xBF),
        ustring.Make (0xF1, 0x80, 0x7F, 0xBF),
        ustring.Make (0xF1, 0x80, 0x80, 0x7F),
        ustring.Make (0xF1, 0xBF, 0xBF, 0xC0),
        ustring.Make (0xF1, 0xBF, 0xC0, 0x80),
        ustring.Make (0xF1, 0xC0, 0x80, 0x80),
        // s7
        ustring.Make (0xF4, 0x7F, 0xBF, 0xBF),
        ustring.Make (0xF4, 0x80, 0x7F, 0xBF),
        ustring.Make (0xF4, 0x80, 0x80, 0x7F),
        ustring.Make (0xF4, 0x8F, 0xBF, 0xC0),
        ustring.Make (0xF4, 0x8F, 0xC0, 0x80),
        ustring.Make (0xF4, 0x90, 0x80, 0x80),
    ];

    [Fact]
    public void TestDecodeInvalidSequence ()
    {
        foreach (var str in invalidSequenceTests) {
            (var r1, _) = Utf8.DecodeRune (str.ToByteArray ());
            Assert.Equal (Utf8.RuneError, r1);

            (var r2, _) = Utf8.DecodeRune (ustring.Make (str.ToByteArray ()));
            Assert.Equal (Utf8.RuneError, r2);

            Assert.Equal (r1, r2);
        }
    }

    (ustring testString, int count) [] runeCountTests =
    [
        (ustring.Make ("abcd"), 4),
        (ustring.Make (0xE2, 0x98, 0xBA, 0xE2, 0x98, 0xBB, 0xE2, 0x98, 0xB9), 3),
        (ustring.Make ("1,2,3,4"), 7),
        (ustring.Make (new byte [] { 0xe2, 0x00 }), 2),
        (ustring.Make (new byte [] { 0xe2, 0x80 }), 2),
        (ustring.Make (0x61, 0xe2, 0x80), 3),
    ];

    [Fact]
    public void TestRuneCount ()
    {
        foreach (var t in runeCountTests) {
            Assert.Equal (t.count, Utf8.RuneCount (t.testString.ToByteArray ()));
            Assert.Equal (t.count, t.testString.RuneCount);
        }
    }

    [Fact]
    public void TestRuneLen ()
    {
        (uint rune, int size) [] runeLenTests =
        [
            (0, 1),
            ('e', 1),
            ('é', 2),
            ('☺', 3),
            (Utf8.RuneError, 3),
            (Utf8.MaxRune, 4),
            (0xd800, -1),
            (0xdfff, -1),
            (Utf8.MaxRune + 1, -1),
            (unchecked((uint)-1), -1)
        ];

        foreach (var test in runeLenTests) {
            Assert.Equal (test.size, Utf8.RuneLen (test.rune));
        }
    }

    [Fact]
    public void TestValid ()
    {
        (ustring input, bool output) [] validTests =
        [
            (ustring.Make (""), true),
            (ustring.Make ("a"), true),
            (ustring.Make ("abc"), true),
            (ustring.Make ("Ж"), true),
            (ustring.Make ("ЖЖ"), true),
            (ustring.Make ("брэд-ЛГТМ"), true),
            (ustring.Make (0xE2, 0x98, 0xBA, 0xE2, 0x98, 0xBB, 0xE2, 0x98, 0xB9), true),
            (ustring.Make (new byte [] { 0xaa, 0xe2 }), false),
            (ustring.Make (new byte [] { 66, 250 }), false),
            (ustring.Make (66, 250, 67), false),
            (ustring.Make ("a\uffDb"), true),
            (ustring.Make (0xf4, 0x8f, 0xbf, 0xbf), true),       // U+10FFFF
            (ustring.Make (0xf4, 0x90, 0x80, 0x80), false),       // U+10FFFF+1 out of range
            (ustring.Make (0xF7, 0xBF, 0xBF, 0xBF), false),       // 0x1FFFFF; out of range
            (ustring.Make (0xFB, 0xBF, 0xBF, 0xBF, 0xBF), false), // 0x3FFFFFF; out of range
            (ustring.Make (new byte [] { 0xc0, 0x80 }), false),    // U+0000 encoded in two bytes: incorrect
            (ustring.Make (0xed, 0xa0, 0x80), false),              // U+D800 high surrogate (sic)
            (ustring.Make (0xed, 0xbf, 0xbf), false),              // U+DFFF low surrogate (sic)
        ];

        foreach (var test in validTests) {
            Assert.Equal (test.output, Utf8.Valid (test.input.ToByteArray ()));
            Assert.Equal (test.output, Utf8.Valid (test.input));
        }
    }

    [Fact]
    public void TestValidRune ()
    {
        (uint rune, bool ok) [] validRuneTests =
        [
            (0, true),
            ('e', true),
            ('é', true),
            ('☺', true),
            (Utf8.RuneError, true),
            (Utf8.MaxRune, true),
            (0xd7ff, true),
            (0xd800, false),
            (0xdfff, false),
            (0xe000, true),
            (Utf8.MaxRune + 1, false),
            (unchecked((uint)-1), false)
        ];

        foreach (var test in validRuneTests) {
            Assert.Equal (test.ok, Utf8.ValidRune (test.rune));
        }
    }
}
