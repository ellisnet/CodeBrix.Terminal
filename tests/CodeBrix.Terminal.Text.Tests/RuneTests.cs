using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;

using Rune = System.Rune;

namespace CodeBrix.Terminal.Text.Tests;

public class RuneTests
{
    [Fact]
    public void TestColumnWidth ()
    {
        Rune a = 'a';
        Rune b = 'b';
        Rune c = 123;
        Rune d = '\u1150'; // 0x1150 ᅐ Unicode Technical Report #11
        Rune e = '\u1161'; // 0x1161 ᅡ Unicode Hangul Jamo for join with column width equal to 0 alone.
        Rune f = 31; // non printable character
        Rune g = 127; // non printable character
        string h = "\U0001fa01";
        string i = "\U000e0fe1";
        Rune j = '\u20D0';
        Rune k = '\u25a0';
        Rune l = '\u25a1';
        Rune m = '\uf61e';
        byte [] n = new byte [4] { 0xf0, 0x9f, 0x8d, 0x95 }; // UTF-8 Encoding
        Rune o = new Rune ('\ud83c', '\udf55'); // UTF-16 Encoding
        string p = "\U0001F355"; // UTF-32 Encoding
        Rune q = '\u2103';
        Rune r = '\u1100';
        Rune s = '\u2501';

        Assert.Equal (1, Rune.ColumnWidth (a));
        Assert.Equal ("a", a.ToString ());
        Assert.Equal (1, a.ToString ().Length);
        Assert.Equal (1, Rune.RuneLen (a));

        Assert.Equal (1, Rune.ColumnWidth (b));
        Assert.Equal ("b", b.ToString ());
        Assert.Equal (1, b.ToString ().Length);
        Assert.Equal (1, Rune.RuneLen (b));

        var rl = a < b;
        Assert.True (rl);

        Assert.Equal (1, Rune.ColumnWidth (c));
        Assert.Equal ("{", c.ToString ());
        Assert.Equal (1, c.ToString ().Length);
        Assert.Equal (1, Rune.RuneLen (c));

        Assert.Equal (2, Rune.ColumnWidth (d));
        Assert.Equal ("ᅐ", d.ToString ());
        Assert.Equal (1, d.ToString ().Length);
        Assert.Equal (3, Rune.RuneLen (d));

        Assert.Equal (0, Rune.ColumnWidth (e));
        string join = "\u1104\u1161";
        Assert.Equal ("\u1104\u1161", join);
        Assert.Equal (2, join.Sum (x => Rune.ColumnWidth (x)));
        Assert.False (Rune.DecodeSurrogatePair (join, out _));
        Assert.Equal (2, ((ustring)join).RuneCount);
        Assert.Equal (2, join.Length);
        Assert.Equal ("ᅡ", e.ToString ());
        Assert.Equal (1, e.ToString ().Length);
        Assert.Equal (3, Rune.RuneLen (e));

        string joinNormalize = join.Normalize ();
        Assert.Equal ("따", joinNormalize);
        Assert.Equal (2, joinNormalize.Sum (x => Rune.ColumnWidth (x)));
        Assert.False (Rune.DecodeSurrogatePair (joinNormalize, out _));
        Assert.Equal (1, ((ustring)joinNormalize).RuneCount);
        Assert.Equal (1, joinNormalize.Length);

        Assert.Equal (-1, Rune.ColumnWidth (f));
        Assert.Equal (1, f.ToString ().Length);
        Assert.Equal (1, Rune.RuneLen (f));

        Assert.Equal (-1, Rune.ColumnWidth (g));
        Assert.Equal (1, g.ToString ().Length);
        Assert.Equal (1, Rune.RuneLen (g));

        var uh = ustring.Make (h);
        (var runeh, var sizeh) = uh.DecodeRune ();
        Assert.Equal (2, Rune.ColumnWidth (runeh));
        Assert.Equal ("🨁", h);
        Assert.Equal (2, runeh.ToString ().Length);
        Assert.Equal (4, Rune.RuneLen (runeh));
        Assert.Equal (sizeh, Rune.RuneLen (runeh));
        for (int x = 0; x < uh.Length - 1; x++) {
            Assert.False (Rune.EncodeSurrogatePair (uh [x], uh [x + 1], out _));
        }
        Assert.True (Rune.ValidRune (runeh));
        Assert.True (Rune.Valid (uh.ToByteArray ()));
        Assert.True (Rune.FullRune (uh.ToByteArray ()));
        Assert.Equal (1, uh.RuneCount);

        (var runelh, var sizelh) = uh.DecodeLastRune ();
        Assert.Equal (2, Rune.ColumnWidth (runelh));
        Assert.Equal (2, runelh.ToString ().Length);
        Assert.Equal (4, Rune.RuneLen (runelh));
        Assert.Equal (sizelh, Rune.RuneLen (runelh));
        Assert.True (Rune.ValidRune (runelh));

        var ui = ustring.Make (i);
        (var runei, var sizei) = ui.DecodeRune ();
        Assert.Equal (2, Rune.ColumnWidth (runei));
        Assert.Equal ("󠿡", i);
        Assert.Equal (2, runei.ToString ().Length);
        Assert.Equal (4, Rune.RuneLen (runei));
        Assert.Equal (sizei, Rune.RuneLen (runei));
        for (int x = 0; x < ui.Length - 1; x++) {
            Assert.False (Rune.EncodeSurrogatePair (ui [x], ui [x + 1], out _));
        }
        Assert.True (Rune.ValidRune (runei));
        Assert.True (Rune.Valid (ui.ToByteArray ()));
        Assert.True (Rune.FullRune (ui.ToByteArray ()));

        (var runeli, var sizeli) = ui.DecodeLastRune ();
        Assert.Equal (2, Rune.ColumnWidth (runeli));
        Assert.Equal (2, runeli.ToString ().Length);
        Assert.Equal (4, Rune.RuneLen (runeli));
        Assert.Equal (sizeli, Rune.RuneLen (runeli));
        Assert.True (Rune.ValidRune (runeli));

        Assert.Equal (Rune.ColumnWidth (runeh), Rune.ColumnWidth (runei));
        Assert.NotEqual (h, i);
        Assert.Equal (runeh.ToString ().Length, runei.ToString ().Length);
        Assert.Equal (Rune.RuneLen (runeh), Rune.RuneLen (runei));

        var uj = ustring.Make (j);
        (var runej, var sizej) = uj.DecodeRune ();
        Assert.Equal (0, Rune.ColumnWidth (j));
        Assert.Equal (0, Rune.ColumnWidth (uj.RuneAt (0)));
        Assert.Equal (j, uj.RuneAt (0));
        Assert.Equal ("⃐", j.ToString ());
        Assert.Equal ("⃐", uj.ToString ());
        Assert.Equal (1, j.ToString ().Length);
        Assert.Equal (1, runej.ToString ().Length);
        Assert.Equal (3, Rune.RuneLen (j));
        Assert.Equal (sizej, Rune.RuneLen (runej));

        Assert.Equal (1, Rune.ColumnWidth (k));
        Assert.Equal ("■", k.ToString ());
        Assert.Equal (1, k.ToString ().Length);
        Assert.Equal (3, Rune.RuneLen (k));

        Assert.Equal (1, Rune.ColumnWidth (l));
        Assert.Equal ("□", l.ToString ());
        Assert.Equal (1, l.ToString ().Length);
        Assert.Equal (3, Rune.RuneLen (l));

        Assert.Equal (1, Rune.ColumnWidth (m));
        Assert.Equal ("\uf61e", m.ToString ());
        Assert.Equal (1, m.ToString ().Length);
        Assert.Equal (3, Rune.RuneLen (m));

        var rn = ustring.Make (n).DecodeRune ().rune;
        Assert.Equal (2, Rune.ColumnWidth (rn));
        Assert.Equal ("🍕", rn.ToString ());
        Assert.Equal (2, rn.ToString ().Length);
        Assert.Equal (4, Rune.RuneLen (rn));

        Assert.Equal (2, Rune.ColumnWidth (o));
        Assert.Equal ("🍕", o.ToString ());
        Assert.Equal (2, o.ToString ().Length);
        Assert.Equal (4, Rune.RuneLen (o));

        var rp = ustring.Make (p).DecodeRune ().rune;
        Assert.Equal (2, Rune.ColumnWidth (rp));
        Assert.Equal ("🍕", p);
        Assert.Equal (2, p.Length);
        Assert.Equal (4, Rune.RuneLen (rp));

        Assert.Equal (1, Rune.ColumnWidth (q));
        Assert.Equal ("℃", q.ToString ());
        Assert.Equal (1, q.ToString ().Length);
        Assert.Equal (3, Rune.RuneLen (q));

        var rq = ustring.Make (q).DecodeRune ().rune;
        Assert.Equal (1, Rune.ColumnWidth (rq));
        Assert.Equal ("℃", rq.ToString ());
        Assert.Equal (1, rq.ToString ().Length);
        Assert.Equal (3, Rune.RuneLen (rq));

        Assert.Equal (2, Rune.ColumnWidth (r));
        Assert.Equal ("ᄀ", r.ToString ());
        Assert.Equal (1, r.ToString ().Length);
        Assert.Equal (3, Rune.RuneLen (r));

        Assert.Equal (1, Rune.ColumnWidth (s));
        Assert.Equal ("━", s.ToString ());
        Assert.Equal (1, s.ToString ().Length);
        Assert.Equal (3, Rune.RuneLen (s));

        var buff = new byte [4];
        var sb = Rune.EncodeRune ('\u2503', buff);
        Assert.Equal (1, Rune.ColumnWidth ('\u2503'));
        (var rune, var size) = ustring.Make ('\u2503').DecodeRune ();
        Assert.Equal (sb, size);
        Assert.Equal ('\u2503', (uint)rune);

        var scb = char.ConvertToUtf32 ("℃", 0);
        var scr = '℃'.ToString ().Length;
        Assert.Equal (scr, Rune.ColumnWidth ((uint)scb));

        buff = new byte [4];
        sb = Rune.EncodeRune ('\u1100', buff);
        Assert.Equal (2, Rune.ColumnWidth ('\u1100'));
        Assert.Equal (2, ustring.Make ('\u1100').ConsoleWidth);
        Assert.Equal (1, '\u1100'.ToString ().Length);
        // Length as string returns 1 but in reality it occupies 2 columns.
        (rune, size) = ustring.Make ('\u1100').DecodeRune ();
        Assert.Equal (sb, size);
        Assert.Equal ('\u1100', (uint)rune);

        string str = "\u2615";
        Assert.Equal ("☕", str);
        Assert.Equal (2, str.Sum (x => Rune.ColumnWidth (x)));
        Assert.Equal (2, ((ustring)str).ConsoleWidth);
        Assert.Equal (1, ((ustring)str).RuneCount);
        Assert.Equal (1, str.Length);

        str = "\u2615\ufe0f";
        // Identical but \ufe0f forces it to be rendered as a colorful image
        // as compared to a monochrome text variant.
        Assert.Equal ("☕️", str);
        Assert.Equal (2, str.Sum (x => Rune.ColumnWidth (x)));
        Assert.Equal (2, ((ustring)str).ConsoleWidth);
        Assert.Equal (2, ((ustring)str).RuneCount);
        Assert.Equal (2, str.Length);

        str = "\u231a";
        Assert.Equal ("⌚", str);
        Assert.Equal (2, str.Sum (x => Rune.ColumnWidth (x)));
        Assert.Equal (2, ((ustring)str).ConsoleWidth);
        Assert.Equal (1, ((ustring)str).RuneCount);
        Assert.Equal (1, str.Length);

        str = "\u231b";
        Assert.Equal ("⌛", str);
        Assert.Equal (2, str.Sum (x => Rune.ColumnWidth (x)));
        Assert.Equal (2, ((ustring)str).ConsoleWidth);
        Assert.Equal (1, ((ustring)str).RuneCount);
        Assert.Equal (1, str.Length);

        str = "\u231c";
        Assert.Equal ("⌜", str);
        Assert.Equal (1, str.Sum (x => Rune.ColumnWidth (x)));
        Assert.Equal (1, ((ustring)str).ConsoleWidth);
        Assert.Equal (1, ((ustring)str).RuneCount);
        Assert.Equal (1, str.Length);

        str = "\u1dc0";
        Assert.Equal ("᷀", str);
        Assert.Equal (0, str.Sum (x => Rune.ColumnWidth (x)));
        Assert.Equal (0, ((ustring)str).ConsoleWidth);
        Assert.Equal (1, ((ustring)str).RuneCount);
        Assert.Equal (1, str.Length);

        str = "\ud83e\udd16";
        Assert.Equal ("🤖", str);
        Assert.Equal (2, str.Sum (x => Rune.ColumnWidth (x)));
        Assert.Equal (2, ((ustring)str).ConsoleWidth);
        Assert.Equal (1, ((ustring)str).RuneCount);
        // Here returns 1 because is a valid surrogate pair resulting in only rune >=U+10000..U+10FFFF
        Assert.Equal (2, str.Length);
        // String always preserves the originals values of each surrogate pair

        str = "\U0001f9e0";
        Assert.Equal ("🧠", str);
        Assert.Equal (2, str.Sum (x => Rune.ColumnWidth (x)));
        Assert.Equal (2, ((ustring)str).ConsoleWidth);
        Assert.Equal (1, ((ustring)str).RuneCount);
        Assert.Equal (2, str.Length);
    }

    [Fact]
    public void TestRune ()
    {
        Rune a = new Rune ('a');
        Assert.Equal (1, Rune.ColumnWidth (a));
        Assert.Equal (1, a.ToString ().Length);
        Assert.Equal ("a", a.ToString ());

        Rune b = new Rune (0x0061);
        Assert.Equal (1, Rune.ColumnWidth (b));
        Assert.Equal (1, b.ToString ().Length);
        Assert.Equal ("a", b.ToString ());

        Rune c = new Rune ('\u0061');
        Assert.Equal (1, Rune.ColumnWidth (c));
        Assert.Equal (1, c.ToString ().Length);
        Assert.Equal ("a", c.ToString ());

        Rune d = new Rune (0x10421);
        Assert.Equal (2, Rune.ColumnWidth (d));
        Assert.Equal (2, d.ToString ().Length);
        Assert.Equal ("𐐡", d.ToString ());

        Assert.False (Rune.EncodeSurrogatePair ('\ud799', '\udc21', out _));
        Assert.Throws<ArgumentOutOfRangeException> (() => new Rune ('\ud799', '\udc21'));

        Rune e = new Rune ('\ud801', '\udc21');
        Assert.Equal (2, Rune.ColumnWidth (e));
        Assert.Equal (2, e.ToString ().Length);
        Assert.Equal ("𐐡", e.ToString ());

        Assert.False (new Rune ('\ud801').IsValid);

        Rune f = new Rune ('\ud83c', '\udf39');
        Assert.Equal (2, Rune.ColumnWidth (f));
        Assert.Equal (2, f.ToString ().Length);
        Assert.Equal ("🌹", f.ToString ());

        // Does not throw for max valid rune
        Rune g = new Rune (0x10ffff);
        string s = "\U0010ffff";
        Assert.Equal (2, Rune.ColumnWidth (g));
        Assert.Equal (2, ustring.Make (s).ConsoleWidth);
        Assert.Equal (2, g.ToString ().Length);
        Assert.Equal (2, s.Length);
        Assert.Equal ("􏿿", g.ToString ());
        Assert.Equal ("􏿿", s);
        Assert.Equal (g.ToString (), s);

        Assert.Throws<ArgumentOutOfRangeException> (() => new Rune (0x12345678));

        var h = new Rune ('\u1150');
        Assert.Equal (2, Rune.ColumnWidth (h));
        Assert.Equal (1, h.ToString ().Length);
        Assert.Equal ("ᅐ", h.ToString ());

        var i = new Rune ('\u4F60');
        Assert.Equal (2, Rune.ColumnWidth (i));
        Assert.Equal (1, i.ToString ().Length);
        Assert.Equal ("你", i.ToString ());

        var j = new Rune ('\u597D');
        Assert.Equal (2, Rune.ColumnWidth (j));
        Assert.Equal (1, j.ToString ().Length);
        Assert.Equal ("好", j.ToString ());

        var k = new Rune ('\ud83d', '\udc02');
        Assert.Equal (2, Rune.ColumnWidth (k));
        Assert.Equal (2, k.ToString ().Length);
        Assert.Equal ("🐂", k.ToString ());

        var l = new Rune ('\ud801', '\udcbb');
        Assert.Equal (2, Rune.ColumnWidth (l));
        Assert.Equal (2, l.ToString ().Length);
        Assert.Equal ("𐒻", l.ToString ());

        var m = new Rune ('\ud801', '\udccf');
        Assert.Equal (2, Rune.ColumnWidth (m));
        Assert.Equal (2, m.ToString ().Length);
        Assert.Equal ("𐓏", m.ToString ());

        var n = new Rune ('\u00e1');
        Assert.Equal (1, Rune.ColumnWidth (n));
        Assert.Equal (1, n.ToString ().Length);
        Assert.Equal ("á", n.ToString ());

        var o = new Rune ('\ud83d', '\udd2e');
        Assert.Equal (2, Rune.ColumnWidth (o));
        Assert.Equal (2, o.ToString ().Length);
        Assert.Equal ("🔮", o.ToString ());

        var p = new Rune ('\u2329');
        Assert.Equal (2, Rune.ColumnWidth (p));
        Assert.Equal (1, p.ToString ().Length);
        Assert.Equal ("\u2329", p.ToString ());

        var q = new Rune ('\u232a');
        Assert.Equal (2, Rune.ColumnWidth (q));
        Assert.Equal (1, q.ToString ().Length);
        Assert.Equal ("\u232a", q.ToString ());

        var r = ustring.Make ("\U0000232a").DecodeRune ().rune;
        Assert.Equal (2, Rune.ColumnWidth (r));
        Assert.Equal (1, r.ToString ().Length);
        Assert.Equal ("\u232a", r.ToString ());

        PrintTextElementCount (ustring.Make ('\u00e1'), "á", 1, 1, 1, 1);
        PrintTextElementCount (ustring.Make ('\u0061', '\u0301'), "\u0061\u0301", 1, 2, 2, 1);
        PrintTextElementCount (ustring.Make ('\u0065', '\u0301'), "\u0065\u0301", 1, 2, 2, 1);

        PrintTextElementCount (ustring.Make (new Rune [] {
            new Rune (0x1f469), new Rune (0x1f3fd), new Rune ('\u200d'), new Rune (0x1f692)
        }), "👩🏽‍🚒", 6, 4, 7, 1);

        PrintTextElementCount (ustring.Make (new Rune [] {
            new Rune (0x1f469), new Rune (0x1f3fd), new Rune ('\u200d'), new Rune (0x1f692)
        }), "\U0001f469\U0001f3fd\u200d\U0001f692", 6, 4, 7, 1);

        PrintTextElementCount (ustring.Make (new Rune ('\ud801', '\udccf')),
            "\ud801\udccf", 2, 1, 2, 1);
    }

    void PrintTextElementCount (ustring us, string s, int consoleWidth,
        int runeCount, int stringCount, int txtElementCount)
    {
        Assert.NotEqual (us.Length, s.Length);
        Assert.Equal (us.ToString (), s);
        Assert.Equal (consoleWidth, us.ConsoleWidth);
        Assert.Equal (runeCount, us.RuneCount);
        Assert.Equal (stringCount, s.Length);

        TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator (s);
        int textElementCount = 0;
        while (enumerator.MoveNext ()) {
            textElementCount++;
        }
        Assert.Equal (txtElementCount, textElementCount);
    }

    [Fact]
    public void TestRuneIsLetter ()
    {
        Assert.Equal (5, CountLettersInString ("Hello"));
        Assert.Equal (8, CountLettersInString ("𐓏𐓘𐓻𐓘𐓻𐓟 𐒻𐓟"));
    }

    int CountLettersInString (string s)
    {
        int letterCount = 0;
        var us = ustring.Make (s);
        foreach (Rune rune in us) {
            if (Rune.IsLetter (rune)) {
                letterCount++;
            }
        }
        return letterCount;
    }

    [Fact]
    public void Test_SurrogatePair_From_String ()
    {
        Assert.True (ProcessTestStringUseChar ("𐓏𐓘𐓻𐓘𐓻𐓟 𐒻𐓟"));
        Assert.ThrowsAny<Exception> (() => ProcessTestStringUseChar ("\ud801"));

        Assert.True (ProcessStringUseRune ("𐓏𐓘𐓻𐓘𐓻𐓟 𐒻𐓟"));
        Assert.ThrowsAny<Exception> (() => ProcessStringUseRune ("\ud801"));
    }

    bool ProcessTestStringUseChar (string s)
    {
        for (int i = 0; i < s.Length; i++) {
            Rune r = new Rune (s [i]);
            if (!char.IsSurrogate (s [i])) {
                var buff = new byte [4];
                Rune.EncodeRune (s [i], buff);
                Assert.Equal ((int)(s [i]), buff [0]);
                Assert.Equal (s [i], r.Value);
                Assert.True (r.IsValid);
                Assert.False (r.IsSurrogatePair);
            } else if (i + 1 < s.Length && char.IsSurrogatePair (s [i], s [i + 1])) {
                int codePoint = char.ConvertToUtf32 (s [i], s [i + 1]);
                Rune.EncodeSurrogatePair (s [i], s [i + 1], out Rune rune);
                Assert.Equal ((uint)codePoint, rune.Value);
                string sp = new string (new char [] { s [i], s [i + 1] });
                r = (uint)codePoint;
                Assert.Equal (sp, r.ToString ());
                Assert.True (r.IsSurrogatePair);
                i++; // Increment the iterator by the number of surrogate pair
            } else {
                Assert.False (r.IsValid);
                throw new Exception ("String was not well-formed UTF-16.");
            }
        }
        return true;
    }

    bool ProcessStringUseRune (string s)
    {
        var us = ustring.Make (s);
        string rs = "";
        Rune codePoint;
        List<Rune> runes = new List<Rune> ();
        int colWidth = 0;

        for (int i = 0; i < s.Length; i++) {
            Rune rune = new Rune (s [i]);
            if (rune.IsValid) {
                Assert.True (Rune.ValidRune (rune));
                runes.Add (rune);
                Assert.Equal ((uint)s [i], (uint)rune);
                Assert.False (rune.IsSurrogatePair);
            } else if (i + 1 < s.Length && (Rune.EncodeSurrogatePair (s [i], s [i + 1], out codePoint))) {
                Assert.False (Rune.ValidRune (rune));
                rune = codePoint;
                runes.Add (rune);
                string sp = new string (new char [] { s [i], s [i + 1] });
                Assert.Equal (sp, codePoint.ToString ());
                Assert.True (codePoint.IsSurrogatePair);
                i++; // Increment the iterator by the number of surrogate pair
            } else {
                Assert.False (rune.IsValid);
                throw new Exception ("String was not well-formed UTF-16.");
            }
            colWidth += Rune.ColumnWidth (rune); // Increment the column width of this Rune
            rs += rune.ToString ();
        }

        Assert.Equal (us.ConsoleWidth, colWidth);
        Assert.Equal (s, rs);
        Assert.Equal (s, ustring.Make (runes).ToString ());
        return true;
    }

    [Fact]
    public void TestSplit ()
    {
        string inputString = "🐂, 🐄, 🐆";
        string [] splitOnSpace = inputString.Split (' ');
        string [] splitOnComma = inputString.Split (',');

        Assert.Equal (3, splitOnSpace.Length);
        Assert.Equal (3, splitOnComma.Length);
    }

    [Fact]
    public void TestValidRune ()
    {
        Assert.True (Rune.ValidRune (new Rune ('\u1100')));
        Assert.True (Rune.ValidRune (new Rune ('\ud83c', '\udf39')));
        Assert.False (Rune.ValidRune ('\ud801'));
        Assert.False (Rune.ValidRune ((uint)'\ud801'));
        Assert.False (Rune.ValidRune ((Rune)'\ud801'));
    }

    [Fact]
    public void TestValid ()
    {
        var rune1 = new Rune ('\ud83c', '\udf39');
        var buff1 = new byte [4];
        Assert.Equal (4, Rune.EncodeRune (rune1, buff1));
        Assert.True (Rune.Valid (buff1));
        Assert.Equal (2, rune1.ToString ().Length);
        Assert.Equal (4, Rune.RuneLen (rune1));

        var rune2 = (uint)'\ud801';
        // To avoid throwing an exception set as uint instead a Rune instance.
        var buff2 = new byte [4];
        Assert.Equal (3, Rune.EncodeRune (rune2, buff2));
        Assert.False (Rune.Valid (buff2));
        // To avoid throwing an exception pass as uint parameter instead Rune.
        Assert.Equal (5, rune2.ToString ().Length);
        // Invalid string. It returns the decimal 55297 representation of the 0xd801 hexadecimal.
        Assert.Equal (-1, Rune.RuneLen (rune2));

        Assert.Equal (3, Rune.EncodeRune (new Rune ('\ud801'), buff2)); // error
        Assert.Equal (new byte [] { 0xef, 0x3f, 0x3d, 0 }, buff2); // error
    }

    [Fact]
    public void Test_IsNonSpacingChar ()
    {
        Rune l = '\u0370';
        Assert.False (Rune.IsNonSpacingChar (l));
        Assert.Equal (1, Rune.ColumnWidth (l));
        Assert.Equal (1, ustring.Make (l).ConsoleWidth);

        Rune ns = '\u302a';
        Assert.False (Rune.IsNonSpacingChar (ns));
        Assert.Equal (2, Rune.ColumnWidth (ns));
        Assert.Equal (2, ustring.Make (ns).ConsoleWidth);

        l = '\u006f';
        ns = '\u0302';
        var s = "\u006f\u0302";
        Assert.Equal (1, Rune.ColumnWidth (l));
        Assert.Equal (0, Rune.ColumnWidth (ns));
        var ul = ustring.Make (l);
        Assert.Equal ("o", ul);
        var uns = ustring.Make (ns);
        Assert.Equal ("̂", uns);
        var f = ustring.Make ($"{l}{ns}");
        Assert.Equal ("\u006f\u0302", f);
        Assert.Equal (f, s);
        Assert.Equal (1, f.ConsoleWidth);
        Assert.Equal (1, s.Sum (ch => Rune.ColumnWidth (ch)));
        Assert.Equal (2, s.Length);
        (var rune, var size) = f.DecodeRune ();
        Assert.Equal (rune, l);
        Assert.Equal (1, size);

        l = '\u0041';
        ns = '\u0305';
        s = "\u0041\u0305";
        Assert.Equal (1, Rune.ColumnWidth (l));
        Assert.Equal (0, Rune.ColumnWidth (ns));
        ul = ustring.Make (l);
        Assert.Equal ("A", ul);
        uns = ustring.Make (ns);
        Assert.Equal ("̅", uns);
        f = ustring.Make ($"{l}{ns}");
        Assert.Equal ("A̅", f);
        Assert.Equal (f, s);
        Assert.Equal (1, f.ConsoleWidth);
        Assert.Equal (1, s.Sum (ch => Rune.ColumnWidth (ch)));
        Assert.Equal (2, s.Length);
        (rune, size) = f.DecodeRune ();
        Assert.Equal (rune, l);
        Assert.Equal (1, size);

        l = '\u0061';
        ns = '\u0308';
        s = "\u0061\u0308";
        Assert.Equal (1, Rune.ColumnWidth (l));
        Assert.Equal (0, Rune.ColumnWidth (ns));
        ul = ustring.Make (l);
        Assert.Equal ("a", ul);
        uns = ustring.Make (ns);
        Assert.Equal ("̈", uns);
        f = ustring.Make ($"{l}{ns}");
        Assert.Equal ("\u0061\u0308", f);
        Assert.Equal (f, s);
        Assert.Equal (1, f.ConsoleWidth);
        Assert.Equal (1, s.Sum (ch => Rune.ColumnWidth (ch)));
        Assert.Equal (2, s.Length);
        (rune, size) = f.DecodeRune ();
        Assert.Equal (rune, l);
        Assert.Equal (1, size);

        l = '\u4f00';
        ns = '\u302a';
        s = "\u4f00\u302a";
        Assert.Equal (2, Rune.ColumnWidth (l));
        Assert.Equal (2, Rune.ColumnWidth (ns));
        ul = ustring.Make (l);
        Assert.Equal ("伀", ul);
        uns = ustring.Make (ns);
        Assert.Equal ("〪", uns);
        f = ustring.Make ($"{l}{ns}");
        Assert.Equal ("伀〪", f); // Occupies 4 columns.
        Assert.Equal (f, s);
        Assert.Equal (4, f.ConsoleWidth);
        Assert.Equal (4, s.Sum (ch => Rune.ColumnWidth (ch)));
        Assert.Equal (2, s.Length);
        (rune, size) = f.DecodeRune ();
        Assert.Equal (rune, l);
        Assert.Equal (3, size);
    }

    [Fact]
    public void Test_IsWideChar ()
    {
        Assert.True (Rune.IsWideChar (0x115e));
        Assert.Equal (2, Rune.ColumnWidth (0x115e));
        Assert.False (Rune.IsWideChar (0x116f));
    }

    [Fact]
    public void Test_MaxRune ()
    {
        Assert.Throws<ArgumentOutOfRangeException> (() => new Rune (500000000));
        Assert.Throws<ArgumentOutOfRangeException> (() => new Rune (0xf801, 0xdfff));
    }

    [Fact]
    public void Sum_Of_ColumnWidth_Is_Not_Always_Equal_To_ConsoleWidth ()
    {
        const int start = 0x000000;
        const int end = 0x10ffff;
        for (int i = start; i <= end; i++) {
            Rune r = new Rune ((uint)i);
            if (!r.IsValid) {
                continue;
            }

            ustring us = ustring.Make (r);
            string hex = i.ToString ("x6");
            int v = int.Parse (hex, NumberStyles.HexNumber);
            string s = char.ConvertFromUtf32 (v);

            if (!r.IsSurrogatePair) {
                Assert.Equal (r.ToString (), us);
                Assert.Equal (us, s);
                if (Rune.ColumnWidth (r) < 0) {
                    Assert.NotEqual (Rune.ColumnWidth (r), us.ConsoleWidth);
                    Assert.NotEqual (s.Sum (c => Rune.ColumnWidth (c)), us.ConsoleWidth);
                } else {
                    Assert.Equal (Rune.ColumnWidth (r), us.ConsoleWidth);
                    Assert.Equal (s.Sum (c => Rune.ColumnWidth (c)), us.ConsoleWidth);
                }
                Assert.Equal (us.RuneCount, s.Length);
            } else {
                Assert.Equal (r.ToString (), us.ToString ());
                Assert.Equal (us.ToString (), s);
                Assert.Equal (Rune.ColumnWidth (r), us.ConsoleWidth);
                Assert.Equal (s.Sum (c => Rune.ColumnWidth (c)), us.ConsoleWidth);
                Assert.Equal (1, us.RuneCount);
                // Here returns 1 because is a valid surrogate pair resulting in only rune >=U+10000..U+10FFFF
                Assert.Equal (2, s.Length);
                // String always preserves the originals values of each surrogate pair
            }
        }
    }

    [Fact]
    public void Test_Right_To_Left_Runes ()
    {
        Rune r0 = 0x020000;
        Rune r7 = 0x020007;
        Rune r1b = 0x02001b;
        Rune r9b = 0x02009b;
        Assert.Equal (2, Rune.ColumnWidth (r0));
        Assert.Equal (2, Rune.ColumnWidth (r7));
        Assert.Equal (2, Rune.ColumnWidth (r1b));
        Assert.Equal (2, Rune.ColumnWidth (r9b));

        Rune.DecodeSurrogatePair ("𐨁", out char [] chars);
        var rtl = new Rune (chars [0], chars [1]);
        var rtlp = new Rune ('\ud802', '\ude01');
        var s = "\U00010a01";
        Assert.Equal (2, Rune.ColumnWidth (rtl));
        Assert.Equal (2, Rune.ColumnWidth (rtlp));
        Assert.Equal (2, s.Length);
    }

    [Theory]
    [InlineData (0x20D0, 0x20EF)]
    [InlineData (0x2310, 0x231F)]
    [InlineData (0x1D800, 0x1D80F)]
    public void Test_Range (int start, int end)
    {
        for (int i = start; i <= end; i++) {
            Rune r = new Rune ((uint)i);
            ustring us = ustring.Make (r);
            string hex = i.ToString ("x6");
            int v = int.Parse (hex, NumberStyles.HexNumber);
            string s = char.ConvertFromUtf32 (v);

            if (!r.IsSurrogatePair) {
                Assert.Equal (r.ToString (), us);
                Assert.Equal (us, s);
                Assert.Equal (Rune.ColumnWidth (r), us.ConsoleWidth);
                Assert.Equal (us.RuneCount, s.Length);
                // For not surrogate pairs ustring.RuneCount is always equal to String.Length
            } else {
                Assert.Equal (r.ToString (), us.ToString ());
                Assert.Equal (us.ToString (), s);
                Assert.Equal (Rune.ColumnWidth (r), us.ConsoleWidth);
                Assert.Equal (1, us.RuneCount);
                // Here returns 1 because is a valid surrogate pair resulting in only rune >=U+10000..U+10FFFF
                Assert.Equal (2, s.Length);
                // String always preserves the originals values of each surrogate pair
            }
            Assert.Equal (s.Sum (c => Rune.ColumnWidth (c)), us.ConsoleWidth);
        }
    }

    [Fact]
    public void Test_IsSurrogate ()
    {
        Rune r = '\ue0fd';
        Assert.False (r.IsSurrogate);
        Assert.False (Rune.IsSurrogateRune (r));

        r = 0x927C0;
        Assert.False (r.IsSurrogate);
        Assert.False (Rune.IsSurrogateRune (r));

        r = '\ud800';
        Assert.True (r.IsSurrogate);
        Assert.True (Rune.IsSurrogateRune (r));

        r = '\udfff';
        Assert.True (r.IsSurrogate);
        Assert.True (Rune.IsSurrogateRune (r));
    }

    [Fact]
    public void Test_EncodeSurrogatePair ()
    {
        Assert.False (Rune.EncodeSurrogatePair (0x40D7C0, 0xDC20, out _));
        Assert.False (Rune.EncodeSurrogatePair (0x0065, 0x0301, out _));
        Assert.True (Rune.EncodeSurrogatePair ('\ud83c', '\udf56', out Rune rune));
        Assert.Equal (0x1F356u, rune.Value);
        Assert.Equal ("🍖", rune.ToString ());
    }

    [Fact]
    public void Test_DecodeSurrogatePair ()
    {
        Assert.False (Rune.DecodeSurrogatePair ('\uea85', out char [] chars));
        Assert.Null (chars);

        Assert.True (Rune.DecodeSurrogatePair (0x1F356, out chars));
        Assert.Equal (2, chars.Length);
        Assert.Equal ('\ud83c', chars [0]);
        Assert.Equal ('\udf56', chars [1]);
        Assert.Equal ("🍖", new Rune (chars [0], chars [1]).ToString ());
    }

    [Fact]
    public void Test_Surrogate_Pairs_Range ()
    {
        for (uint h = 0xd800; h <= 0xdbff; h++) {
            for (uint l = 0xdc00; l <= 0xdfff; l++) {
                Rune r = new Rune (h, l);
                ustring us = ustring.Make (r);
                string hex = ((uint)r).ToString ("x6");
                int v = int.Parse (hex, NumberStyles.HexNumber);
                string s = char.ConvertFromUtf32 (v);

                Assert.True (v >= 0x10000 && v <= Rune.MaxRune);
                Assert.Equal (r.ToString (), us.ToString ());
                Assert.Equal (us.ToString (), s);
                Assert.Equal (Rune.ColumnWidth (r), us.ConsoleWidth);
                Assert.Equal (s.Sum (c => Rune.ColumnWidth (c)), us.ConsoleWidth);
                Assert.Equal (1, us.RuneCount);
                // Here returns 1 because is a valid surrogate pair resulting in only rune >=U+10000..U+10FFFF
                Assert.Equal (2, s.Length);
                // String always preserves the originals values of each surrogate pair
            }
        }
    }

    [Fact]
    public void Test_ExpectedSizeFromFirstByte ()
    {
        Assert.Equal (-1, Rune.ExpectedSizeFromFirstByte (255));
        Assert.Equal (1, Rune.ExpectedSizeFromFirstByte (127));
        Assert.Equal (4, Rune.ExpectedSizeFromFirstByte (240));
    }

    [Fact]
    public void Test_FullRune_Extension ()
    {
        ustring us = "Hello, 世界";
        Assert.True (us.FullRune ());

        us = $"Hello, {ustring.Make (new byte [] { 228 })}界";
        Assert.False (us.FullRune ());
    }

    [Fact]
    public void Test_DecodeRune_Extension ()
    {
        ustring us = "Hello, 世界";
        List<Rune> runes = new List<Rune> ();
        int tSize = 0;

        for (int i = 0; i < us.RuneCount; i++) {
            (Rune rune, int size) = us.RuneSubstring (i, 1).DecodeRune ();
            runes.Add (rune);
            tSize += size;
        }

        ustring result = ustring.Make (runes);
        Assert.Equal ("Hello, 世界", result);
        Assert.Equal (13, tSize);
    }

    [Fact]
    public void Test_DecodeLastRune_Extension ()
    {
        ustring us = "Hello, 世界";
        List<Rune> runes = new List<Rune> ();
        int tSize = 0;

        for (int i = us.RuneCount - 1; i >= 0; i--) {
            (Rune rune, int size) = us.RuneSubstring (i, 1).DecodeLastRune ();
            runes.Add (rune);
            tSize += size;
        }

        ustring result = ustring.Make (runes);
        Assert.Equal ("界世 ,olleH", result);
        Assert.Equal (13, tSize);
    }

    [Fact]
    public void Test_InvalidIndex_Extension ()
    {
        ustring us = "Hello, 世界";
        Assert.Equal (-1, us.InvalidIndex ());

        us = ustring.Make (new byte [] { 0xff, 0xfe, 0xfd });
        Assert.Equal (0, us.InvalidIndex ());
    }

    [Fact]
    public void Test_Valid_Extension ()
    {
        ustring us = "Hello, 世界";
        Assert.True (us.Valid ());

        us = ustring.Make (new byte [] { 0xff, 0xfe, 0xfd });
        Assert.False (us.Valid ());
    }

    [Fact]
    public void Test_ExpectedSizeFromFirstByte_Extension ()
    {
        ustring us = ustring.Make (255);
        Assert.Equal (-1, us.ExpectedSizeFromFirstByte ());

        us = ustring.Make (127);
        Assert.Equal (1, us.ExpectedSizeFromFirstByte ());

        us = ustring.Make (240);
        Assert.Equal (4, us.ExpectedSizeFromFirstByte ());
    }

    [Fact]
    public void RuneListEquals ()
    {
        var a = new List<List<Rune>> () { ustring.Make ("First line.").ToRuneList () };

        var b = new List<List<Rune>> () {
            ustring.Make ("First line.").ToRuneList (),
            ustring.Make ("Second line.").ToRuneList ()
        };

        var c = new List<Rune> (a [0]);
        var d = a [0];

        Assert.Equal (a [0], b [0]); // Not the same reference
        Assert.False (a [0] == b [0]);
        Assert.NotEqual (a [0], b [1]);
        Assert.False (a [0] == b [1]);

        Assert.Equal (c, a [0]);
        Assert.False (c == a [0]);
        Assert.Equal (c, b [0]);
        Assert.False (c == b [0]);
        Assert.NotEqual (c, b [1]);
        Assert.False (c == b [1]);

        Assert.Equal (d, a [0]); // Is the same reference
        Assert.True (d == a [0]);
        Assert.Equal (d, b [0]);
        Assert.False (d == b [0]);
        Assert.NotEqual (d, b [1]);
        Assert.False (d == b [1]);

        Assert.True (a [0].SequenceEqual (b [0]));
        Assert.False (a [0].SequenceEqual (b [1]));
        Assert.True (c.SequenceEqual (a [0]));
        Assert.True (c.SequenceEqual (b [0]));
        Assert.False (c.SequenceEqual (b [1]));
        Assert.True (d.SequenceEqual (a [0]));
        Assert.True (d.SequenceEqual (b [0]));
        Assert.False (d.SequenceEqual (b [1]));
    }

    [Fact]
    public void Rune_ColumnWidth_Versus_Ustring_ConsoleWidth_With_Non_Printable_Characters ()
    {
        int sumRuneWidth = 0;
        int sumConsoleWidth = 0;

        for (uint i = 0; i < 32; i++) {
            sumRuneWidth += Rune.ColumnWidth (i);
            sumConsoleWidth += ustring.Make (i).ConsoleWidth;
        }

        Assert.Equal (-32, sumRuneWidth);
        Assert.Equal (0, sumConsoleWidth);
    }

    [Fact]
    public void Rune_ColumnWidth_Versus_Ustring_ConsoleWidth ()
    {
        ustring us = "01234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789";
        Assert.Equal (200, us.Length);
        Assert.Equal (200, us.RuneCount);
        Assert.Equal (200, us.ConsoleWidth);
        int sumRuneWidth = us.Sum (x => Rune.ColumnWidth (x));
        Assert.Equal (200, sumRuneWidth);

        us = "01234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789\n";
        Assert.Equal (201, us.Length);
        Assert.Equal (201, us.RuneCount);
        Assert.Equal (200, us.ConsoleWidth);
        sumRuneWidth = us.Sum (x => Rune.ColumnWidth (x));
        Assert.Equal (199, sumRuneWidth);
    }

    [Fact]
    public void Rune_IsHighSurrogate_IsLowSurrogate ()
    {
        Rune r = '\ud800';
        Assert.True (r.IsHighSurrogate);

        r = '\udbff';
        Assert.True (r.IsHighSurrogate);

        r = '\udc00';
        Assert.True (r.IsLowSurrogate);

        r = '\udfff';
        Assert.True (r.IsLowSurrogate);
    }
}
