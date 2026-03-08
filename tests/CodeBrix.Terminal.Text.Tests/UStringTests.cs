using System;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

using Rune = System.Rune;

namespace CodeBrix.Terminal.Text.Tests;

public class UStringTests
{
    ustring a = ustring.Make ("a");
    ustring seconda = ustring.Make ("a");
    ustring aa = ustring.Make ("aa");
    ustring b = ustring.Make ("b");
    ustring bb = ustring.Make ("bb");
    ustring empty = ustring.Make ("");
    ustring secondempty = ustring.Make ("");
    ustring hello = ustring.Make ("hello, world");
    ustring longhello = ustring.Make ("");
    ustring kosme = ustring.Make (0xce, 0xba, 0xcf, 0x8c, 0xcf, 0x83, 0xce, 0xbc, 0xce, 0xb5);
    ustring kosmex = ustring.Make (0xce, 0xba, 0xcf, 0x8c, 0xcf, 0x83, 0xce, 0xbc, 0xce, 0xb5, 0x41);

    [Fact]
    public void ToLowerTest ()
    {
        var x = ustring.Make ("C-x");
        var res = x.ToLower ();
    }

    [Fact]
    public void IComparableTests ()
    {
        // Compares same-sized strings
        Assert.Equal (-1, a.CompareTo (b));
        Assert.Equal (1, b.CompareTo (a));
        Assert.Equal (-1, aa.CompareTo (bb));
        Assert.Equal (1, bb.CompareTo (aa));

        // Empty
        Assert.Equal (0, empty.CompareTo (empty));
        Assert.Equal (-1, empty.CompareTo (a));
        Assert.Equal (1, a.CompareTo (empty));

        // Same instances
        Assert.Equal (0, a.CompareTo (a));
        Assert.Equal (0, seconda.CompareTo (a));
        Assert.Equal (0, a.CompareTo (seconda));

        // Different sizes
        Assert.Equal (-1, a.CompareTo (aa));
        Assert.Equal (1, aa.CompareTo (a));
    }

    [Fact]
    public void Compare ()
    {
        Assert.Equal (a, seconda);
        Assert.True (a != b);
        Assert.False (a.Equals (b));
        Assert.False (b.Equals (a));
        Assert.NotEqual (a, b);
        Assert.NotEqual (b, a);
        Assert.NotEqual<ustring> (a, aa);
        Assert.NotEqual<ustring> (aa, a);
        Assert.Equal (empty, empty);
        Assert.Equal (empty, secondempty);
    }

    [Fact]
    public void TestEquals ()
    {
        Assert.True (a == seconda);
        Assert.False (a == b);

        string aref = "asdfasdfasdfasdfasdfasdfasdfasdfasdfasdfasdfasdf$";
        var ast = ustring.Make (aref);
        var bst = ustring.Make (aref);
        var cst = ustring.Make (aref.Replace ("$", "D"));

        Assert.True (ast == bst);
        Assert.True (ast != cst);

        var abytes = Encoding.UTF8.GetBytes (aref);
        var bbytes = Encoding.UTF8.GetBytes (aref);
        var cbytes = Encoding.UTF8.GetBytes (aref.Replace ("$", "D"));
        var len = abytes.Length;

        var a1 = Marshal.AllocHGlobal (abytes.Length + 1);
        Marshal.Copy (abytes, 0, a1, abytes.Length);
        var b1 = Marshal.AllocHGlobal (bbytes.Length + 1);
        Marshal.Copy (abytes, 0, b1, abytes.Length);
        var c1 = Marshal.AllocHGlobal (cbytes.Length + 1);
        Marshal.Copy (cbytes, 0, c1, abytes.Length);

        var ap = ustring.Make (a1, len);
        var bp = ustring.Make (b1, len);
        var cp = ustring.Make (c1, len);
        var apalias = ap;

        Assert.True (ap.Equals (bp));
        Assert.True (ap == bp);

        string arefMod = "asdfasdfasdfasdfasdfasdfasdfasdfasdfasdfasdfasdy$";
        Assert.False (ap.Equals (arefMod));
        Assert.False (ap == arefMod);
        Assert.True (ap == apalias);
        Assert.True (ap != cp);

        // Now compare ones with others
        Assert.True (ast == ap);
        Assert.True (ast == bp);
        Assert.True (ap == bst);
        Assert.True (ast == bp);
        Assert.True (ast != cp);

        var cpalias = cp;
        Assert.True (cp == cpalias);
        Assert.True (cp == cst);
        Assert.True (cp != bst);

        // Slices
        Assert.True (ast [1, 5] == ap [1, 5]);
        Assert.True (ast [1, 5] == bp [1, 5]);
        Assert.True (ap [1, 5] == bst [1, 5]);
        Assert.True (ast [1, 5] == bp [1, 5]);
        Assert.True (ast [8, null] != cp [8, null]);
        Assert.True (cp [1, 5] == cpalias [1, 5]);
        Assert.True (cp [1, 5] == cst [1, 5]);
        Assert.True (cp [8, null] != bst [8, null]);
    }

    (string, string, bool) [] ContainTests =
    [
        ("abc", "bc", true),
        ("abc", "bcd", false),
        ("abc", "", true),
        ("", "a", false),
        // 2-byte needle
        ("xxxxxx", "01", false),
        ("01xxxx", "01", true),
        ("xx01xx", "01", true),
        ("xxxx01", "01", true),
        ("1xxxxx", "01", false),
        ("xxxxx0", "01", false),
        // 3-byte needle
        ("xxxxxxx", "012", false),
        ("012xxxx", "012", true),
        ("xx012xx", "012", true),
        ("xxxx012", "012", true),
        ("12xxxxx", "012", false),
        ("xxxxx01", "012", false),
        // 4-byte needle
        ("xxxxxxxx", "0123", false),
        ("0123xxxx", "0123", true),
        ("xx0123xx", "0123", true),
        ("xxxx0123", "0123", true),
        ("123xxxxx", "0123", false),
        ("xxxxx012", "0123", false),
        // 5-7-byte needle
        ("xxxxxxxxx", "01234", false),
        ("01234xxxx", "01234", true),
        ("xx01234xx", "01234", true),
        ("xxxx01234", "01234", true),
        ("1234xxxxx", "01234", false),
        ("xxxxx0123", "01234", false),
        // 8-byte needle
        ("xxxxxxxxxxxx", "01234567", false),
        ("01234567xxxx", "01234567", true),
        ("xx01234567xx", "01234567", true),
        ("xxxx01234567", "01234567", true),
        ("1234567xxxxx", "01234567", false),
        ("xxxxx0123456", "01234567", false),
        // 9-15-byte needle
        ("xxxxxxxxxxxxx", "012345678", false),
        ("012345678xxxx", "012345678", true),
        ("xx012345678xx", "012345678", true),
        ("xxxx012345678", "012345678", true),
        ("12345678xxxxx", "012345678", false),
        ("xxxxx01234567", "012345678", false),
        // 16-byte needle
        ("xxxxxxxxxxxxxxxxxxxx", "0123456789ABCDEF", false),
        ("0123456789ABCDEFxxxx", "0123456789ABCDEF", true),
        ("xx0123456789ABCDEFxx", "0123456789ABCDEF", true),
        ("xxxx0123456789ABCDEF", "0123456789ABCDEF", true),
        ("123456789ABCDEFxxxxx", "0123456789ABCDEF", false),
        ("xxxxx0123456789ABCDE", "0123456789ABCDEF", false),
        // 17-31-byte needle
        ("xxxxxxxxxxxxxxxxxxxxx", "0123456789ABCDEFG", false),
        ("0123456789ABCDEFGxxxx", "0123456789ABCDEFG", true),
        ("xx0123456789ABCDEFGxx", "0123456789ABCDEFG", true),
        ("xxxx0123456789ABCDEFG", "0123456789ABCDEFG", true),
        ("123456789ABCDEFGxxxxx", "0123456789ABCDEFG", false),
        ("xxxxx0123456789ABCDEF", "0123456789ABCDEFG", false),
        // partial match cases
        ("xx01x", "012", false),          // 3
        ("xx0123x", "01234", false),      // 5-7
        ("xx01234567x", "012345678", false), // 9-15
        ("xx0123456789ABCDEFx", "0123456789ABCDEFG", false), // 17-31, issue 15679
    ];

    (string, string, bool) [] containsAnyTests =
    [
        // string, substring, expected
        ("", "", false),
        ("", "a", false),
        ("", "abc", false),
        ("a", "", false),
        ("a", "a", true),
        ("aaa", "a", true),
        ("abc", "xyz", false),
        ("abc", "xcz", true),
        ("a☺b☻c☹d", "uvw☻xyz", true),
        ("aRegExp*", ".(|)*+?^$[]", true),
        ("1....2....3....41....2....3....41....2....3....4", " ", false),
    ];

    [Fact]
    public void TestContainsAny ()
    {
        foreach ((var str, var substr, bool expected) in containsAnyTests) {
            var ustr = ustring.Make (str);
            Assert.Equal (expected, ustr.ContainsAny (substr));
        }
    }

    [Fact]
    public void TestContains ()
    {
        Assert.True (aa.Contains (a));
        Assert.False (aa.Contains (b));
        Assert.True (bb.Contains (b));

        foreach ((string str, string sub, bool expected) in ContainTests) {
            var ustr = ustring.Make (str);
            var usub = ustring.Make (sub);
            Assert.Equal (expected, ustr.Contains (usub));
        }
    }

    (string, uint, bool) [] containsRuneTests =
    [
        ("", 'a', false),
        ("a", 'a', true),
        ("aaa", 'a', true),
        ("abc", 'y', false),
        ("abc", 'c', true),
        ("a☺b☻c☹d", 'x', false),
        ("a☺b☻c☹d", '☻', true),
        ("aRegExp*", '*', true),
    ];

    [Fact]
    public void TestContainsRune ()
    {
        foreach ((var str, uint rune, bool expected) in containsRuneTests) {
            var ustr = ustring.Make (str);
            Assert.Equal (expected, ustr.Contains (rune));
        }
    }

    (string, string, bool) [] equalFoldsTest =
    [
        ("abc", "abc", true),
        ("ABcd", "ABcd", true),
        ("123abc", "123ABC", true),
        ("αβδ", "ΑΒΔ", true),
        ("abc", "xyz", false),
        ("abc", "XYZ", false),
        ("abcdefghijk", "abcdefghijX", false),
        ("abcdefghijk", "abcdefghij\u212A", true),
        ("abcdefghijK", "abcdefghij\u212A", true),
        ("abcdefghijkz", "abcdefghij\u212Ay", false),
        ("abcdefghijKz", "abcdefghij\u212Ay", false),
    ];

    [Fact]
    public void TestEqualFolds ()
    {
        var k = ustring.Make (0x212a);
        Assert.True (k.EqualsFold ("k"));

        foreach ((string s, string t, bool expected) in equalFoldsTest) {
            Assert.Equal (expected, ustring.Make (s).EqualsFold (t));
            Assert.Equal (expected, ustring.Make (t).EqualsFold (s));
        }
    }

    (string, string, int) [] countTests =
    [
        ("", "", 1),
        ("", "notempty", 0),
        ("notempty", "", 9),
        ("smaller", "not smaller", 0),
        ("12345678987654321", "6", 2),
        ("611161116", "6", 3),
        ("notequal", "NotEqual", 0),
        ("equal", "equal", 1),
        ("abc1231231123q", "123", 3),
        ("11111", "11", 2)
    ];

    [Fact]
    public void TestCount ()
    {
        foreach ((string src, string sub, int count) in countTests) {
            Assert.Equal (count, ustring.Make (src).Count (sub));
        }
    }

    [Fact]
    public void TestIndexOf ()
    {
        Assert.Equal (0, hello.IndexOf ('h'));
        Assert.Equal (1, hello.IndexOf ('e'));
        Assert.Equal (2, hello.IndexOf ('l'));
        Assert.Equal (10, kosmex.IndexOf (0x41));
    }

    [Fact]
    public void TestLength ()
    {
        Assert.Equal (12, hello.Length);
        Assert.Equal (12, hello.RuneCount);
        Assert.Equal (10, kosme.Length);
        Assert.Equal (5, kosme.RuneCount);
    }

    void SliceTests (ustring a)
    {
        Assert.Equal ("1234", a [0, 4].ToString ());
        Assert.Equal ("90", a [8, 10].ToString ());
        Assert.Equal ("90", a [8, null].ToString ());
        Assert.Equal ("90", a [-2, null].ToString ());
        Assert.Equal ("9", a [8, 9].ToString ());
        Assert.Equal ("789", a [-4, -1].ToString ());
        Assert.Equal ("7890", a [-4, null].ToString ());
        Assert.Equal ("7890", a [-4, null].ToString ());
        Assert.Equal ("234567", a [-9, -3].ToString ());
        Assert.Equal ("", a [100, 200].ToString ());
        Assert.Equal ("", a [-100, null].ToString ());
        Assert.Equal ("", a [-100, 0].ToString ());
        Assert.Equal ("", a [0, 0].ToString ());
    }

    [Fact]
    public void TestSliceRanges ()
    {
        var str = "1234567890";
        ustring a = ustring.Make (str);
        Assert.Equal (str, a.ToString ());

        var asbyte = new byte [] {
            (byte)'y', (byte)'1', (byte)'2', (byte)'3', (byte)'4', (byte)'5',
            (byte)'6', (byte)'7', (byte)'8', (byte)'9', (byte)'0', (byte)'z'
        };

        SliceTests (a);
        SliceTests (ustring.Make ("x" + str + "x") [1, 11]);

        var f = ustring.Make (asbyte, 1, 10);
        SliceTests (f);

        unsafe {
            fixed (byte* p = &asbyte [1]) {
                var ptrstr = ustring.Make ((IntPtr)p, 10);
                SliceTests (ptrstr);
            }
        }
    }

    [Fact]
    public void TestBlockRelease ()
    {
        bool released = false;
        Action<IntPtr> releaseFunc = (block) => { released = true; };

        var ptr = Marshal.AllocHGlobal (10);
        var s = ustring.Make (ptr, 10, releaseFunc);
        Assert.True (s is IDisposable);
        var id = s as IDisposable;
        id?.Dispose ();
        Assert.True (released);
    }

    [Fact]
    public void TestTrim ()
    {
        Assert.True (ustring.Make ("hello") == ustring.Make (" hello ").TrimSpace ());
        Assert.True (ustring.Make ("hello") == ustring.Make ("\nhello\t").TrimSpace ());
        Assert.True (ustring.Make ("hel \t\tlo") == ustring.Make (" hel \t\tlo ").TrimSpace ());
        Assert.True (ustring.Make (" hello") == ustring.Make (" hello ").TrimEnd (ustring.Make (" ")));
        Assert.True (ustring.Make ("hello ") == ustring.Make ("hello ").TrimStart (ustring.Make (" ")));
        Assert.True (ustring.Make (" hello") == ustring.Make (" hello ").TrimEnd (ustring.Make (" ")));
        Assert.True (ustring.Make ("hello ") == ustring.Make (" hello ").TrimStart (ustring.Make (" ")));
        Assert.True (ustring.Make ("oot") == ustring.Make ("ffffffoot").TrimStart (x => x == 'f'));
    }

    [Fact]
    public void TestSplit ()
    {
        var gecos = ustring.Make ("miguel:*:100:200:Miguel de Icaza:/home/miguel:/bin/bash");
        var fields = gecos.Split (":");

        Assert.Equal (7, fields.Length);
        Assert.True (ustring.Make ("miguel") == fields [0]);
        Assert.True (ustring.Make ("*") == fields [1]);
        Assert.True (ustring.Make ("100") == fields [2]);
        Assert.True (ustring.Make ("200") == fields [3]);
        Assert.True (ustring.Make ("Miguel de Icaza") == fields [4]);
        Assert.True (ustring.Make ("/home/miguel") == fields [5]);
        Assert.True (ustring.Make ("/bin/bash") == fields [6]);

        gecos = ustring.Make ("miguel<>*<>100<>200<>Miguel de Icaza<>/home/miguel<>/bin/bash");
        fields = gecos.Split ("<>");

        Assert.Equal (7, fields.Length);
        Assert.True (ustring.Make ("miguel") == fields [0]);
        Assert.True (ustring.Make ("*") == fields [1]);
        Assert.True (ustring.Make ("100") == fields [2]);
        Assert.True (ustring.Make ("200") == fields [3]);
        Assert.True (ustring.Make ("Miguel de Icaza") == fields [4]);
        Assert.True (ustring.Make ("/home/miguel") == fields [5]);
        Assert.True (ustring.Make ("/bin/bash") == fields [6]);
    }

    [Fact]
    public void TestCopy ()
    {
        // Test the zero-terminator method
        var j = Encoding.UTF8.GetBytes ("Hello");
        var p = Marshal.AllocHGlobal (j.Length + 1);
        Marshal.Copy (j, 0, p, j.Length);
        Marshal.WriteByte (p, j.Length, 0);

        var str = ustring.Make (p);
        Assert.Equal (5, str.Length);

        // Now test the copy
        var str2 = ustring.MakeCopy (p);
        Marshal.WriteByte (p, (byte)'A');

        Assert.Equal ("Aello", str.ToString ());
        Assert.Equal ("Hello", str2.ToString ());
        Assert.False (str == str2);

        ((IDisposable)str).Dispose ();
        ((IDisposable)str2).Dispose ();
    }

    static (string, string, string, int, string) [] replaceTexts =
    [
        // input, oldValue, newValue, n parameter, expected
        ("hello", "l", "L", 0, "hello"),
        ("hello", "l", "L", -1, "heLLo"),
        ("hello", "x", "X", -1, "hello"),
        ("", "x", "X", -1, ""),
        ("radar", "r", "", -1, "ada"),
        ("", "", "<>", -1, "<>"),
        ("banana", "a", "<>", -1, "b<>n<>n<>"),
        ("banana", "a", "<>", 1, "b<>nana"),
        ("banana", "a", "<>", 1000, "b<>n<>n<>"),
        ("banana", "an", "<>", -1, "b<><>a"),
        ("banana", "ana", "<>", -1, "b<>na"),
        ("banana", "", "<>", -1, "<>b<>a<>n<>a<>n<>a<>"),
        ("banana", "", "<>", 10, "<>b<>a<>n<>a<>n<>a<>"),
        ("banana", "", "<>", 6, "<>b<>a<>n<>a<>n<>a"),
        ("banana", "", "<>", 5, "<>b<>a<>n<>a<>na"),
        ("banana", "", "<>", 1, "<>banana"),
        ("banana", "a", "a", -1, "banana"),
        ("banana", "a", "a", 1, "banana"),
        ("☺☻☹", "", "<>", -1, "<>☺<>☻<>☹<>")
    ];

    [Fact]
    public void TestReplace ()
    {
        ustring.Make ("banana").Replace ("", "<>", -1);

        foreach ((var input, var oldv, var newv, var n, var expected) in replaceTexts) {
            var sin = ustring.Make (input);
            var result = sin.Replace (oldv, newv, n);
            Assert.True (result == expected, $"For test on Replace (\"{input}\",\"{oldv}\",\"{newv}\",{n}) got {result}");
        }
    }

    (string, string, int) [] indexTests =
    [
        // string, substring, expected index return
        ("", "", 0),
        ("", "a", -1),
        ("", "foo", -1),
        ("fo", "foo", -1),
        ("foo", "foo", 0),
        ("oofofoofooo", "f", 2),
        ("oofofoofooo", "foo", 4),
        ("barfoobarfoo", "foo", 3),
        ("foo", "", 0),
        ("foo", "o", 1),
        ("abcABCabc", "A", 3),
        // cases with one byte strings - test special case in Index()
        ("", "a", -1),
        ("x", "a", -1),
        ("x", "x", 0),
        ("abc", "a", 0),
        ("abc", "b", 1),
        ("abc", "c", 2),
        ("abc", "x", -1),
        // test special cases in Index() for short strings
        ("", "ab", -1),
        ("bc", "ab", -1),
        ("ab", "ab", 0),
        ("xab", "ab", 1),
        ("", "abc", -1),
        ("xbc", "abc", -1),
        ("abc", "abc", 0),
        ("xabc", "abc", 1),
        ("xabxc", "abc", -1),
        ("", "abcd", -1),
        ("xbcd", "abcd", -1),
        ("abcd", "abcd", 0),
        ("xabcd", "abcd", 1),
        ("xbcqq", "abcqq", -1),
        ("abcqq", "abcqq", 0),
        ("xabcqq", "abcqq", 1),
        ("xabxcqq", "abcqq", -1),
        ("xabcqxq", "abcqq", -1),
        ("", "01234567", -1),
        ("32145678", "01234567", -1),
        ("01234567", "01234567", 0),
        ("x01234567", "01234567", 1),
        ("x0123456x01234567", "01234567", 9),
        ("", "0123456789", -1),
        ("3214567844", "0123456789", -1),
        ("0123456789", "0123456789", 0),
        ("x0123456789", "0123456789", 1),
        ("x012345678x0123456789", "0123456789", 11),
        ("x01234567x89", "0123456789", -1),
        ("", "0123456789012345", -1),
        ("3214567889012345", "0123456789012345", -1),
        ("0123456789012345", "0123456789012345", 0),
        ("x0123456789012345", "0123456789012345", 1),
        ("x012345678901234x0123456789012345", "0123456789012345", 17),
        ("", "01234567890123456789", -1),
        ("32145678890123456789", "01234567890123456789", -1),
        ("01234567890123456789", "01234567890123456789", 0),
        ("x01234567890123456789", "01234567890123456789", 1),
        ("x0123456789012345678x01234567890123456789", "01234567890123456789", 21),
        ("", "0123456789012345678901234567890", -1),
        ("321456788901234567890123456789012345678911", "0123456789012345678901234567890", -1),
        ("0123456789012345678901234567890", "0123456789012345678901234567890", 0),
        ("x0123456789012345678901234567890", "0123456789012345678901234567890", 1),
        ("x012345678901234567890123456789x0123456789012345678901234567890", "0123456789012345678901234567890", 32),
        ("", "01234567890123456789012345678901", -1),
        ("32145678890123456789012345678901234567890211", "01234567890123456789012345678901", -1),
        ("01234567890123456789012345678901", "01234567890123456789012345678901", 0),
        ("x01234567890123456789012345678901", "01234567890123456789012345678901", 1),
        ("x0123456789012345678901234567890x01234567890123456789012345678901", "01234567890123456789012345678901", 33),
        ("xxxxxx012345678901234567890123456789012345678901234567890123456789012", "012345678901234567890123456789012345678901234567890123456789012", 6),
        ("", "0123456789012345678901234567890123456789", -1),
        ("xx012345678901234567890123456789012345678901234567890123456789012", "0123456789012345678901234567890123456789", 2),
        ("xx012345678901234567890123456789012345678901234567890123456789012", "0123456789012345678901234567890123456xxx", -1),
        ("xx0123456789012345678901234567890123456789012345678901234567890120123456789012345678901234567890123456xxx", "0123456789012345678901234567890123456xxx", 65)
    ];

    [Fact]
    public void TestIndex ()
    {
        foreach ((string s, string sep, int pos) in indexTests) {
            Assert.Equal (pos, ustring.Make (s).IndexOf (sep));
        }
    }

    (string, string, int) [] lastIndexTests =
    [
        ("", "", 0),
        ("", "a", -1),
        ("", "foo", -1),
        ("fo", "foo", -1),
        ("foo", "foo", 0),
        ("foo", "f", 0),
        ("oofofoofooo", "f", 7),
        ("oofofoofooo", "foo", 7),
        ("barfoobarfoo", "foo", 9),
        ("foo", "", 3),
        ("foo", "o", 2),
        ("abcABCabc", "A", 3),
        ("abcABCabc", "a", 6),
    ];

    [Fact]
    public void TestLastIndex ()
    {
        foreach ((string s, string sep, int pos) in lastIndexTests) {
            Assert.Equal (pos, ustring.Make (s).LastIndexOf (sep));
        }
    }

    (string, string, int) [] indexAnyTests =
    [
        ("", "", -1),
        ("", "a", -1),
        ("", "abc", -1),
        ("a", "", -1),
        ("a", "a", 0),
        ("aaa", "a", 0),
        ("abc", "xyz", -1),
        ("abc", "xcz", 2),
        ("ab☺c", "x☺yz", 2),
        ("a☺b☻c☹d", "cx", ustring.Make ("a☺b☻").Length),
        ("a☺b☻c☹d", "uvw☻xyz", ustring.Make ("a☺b").Length),
        ("aRegExp*", ".(|)*+?^$[]", 7),
        ("1....2....3....41....2....3....41....2....3....4", " ", -1),
    ];

    (string, string, int) [] lastIndexAnyTests =
    [
        ("abc", "xyz", -1),
        ("a", "a", 0),
        ("", "", -1),
        ("", "a", -1),
        ("", "abc", -1),
        ("a", "", -1),
        ("aaa", "a", 2),
        ("abc", "ab", 1),
        ("ab☺c", "x☺yz", 2),
        ("a☺b☻c☹d", "cx", ustring.Make ("a☺b☻").Length),
        ("a☺b☻c☹d", "uvw☻xyz", ustring.Make ("a☺b").Length),
        ("a.RegExp*", ".(|)*+?^$[]", 8),
        ("1....2....3....41....2....3....41....2....3....4", " ", -1),
    ];

    (string, string, int) [] lastIndexByteTests =
    [
        ("abcdefabcdef", "a", ustring.Make ("abcdef").Length),
        ("", "q", -1),
        ("abcdef", "q", -1),
        ("abcdefabcdef", "f", ustring.Make ("abcdefabcde").Length),
        ("zabcdefabcdef", "z", 0),
        ("a☺b☻c☹d", "b", ustring.Make ("a☺").Length),
    ];

    [Fact]
    public void TestIndexAny ()
    {
        foreach ((string s, string sep, int pos) in indexAnyTests) {
            Assert.Equal (pos, ustring.Make (s).IndexOfAny (sep));
        }
    }

    [Fact]
    public void TestLastIndexAny ()
    {
        foreach ((string s, string sep, int pos) in lastIndexAnyTests) {
            Assert.Equal (pos, ustring.Make (s).LastIndexOfAny (sep));
        }
    }

    [Fact]
    public void TestLastIndexByte ()
    {
        foreach ((string s, string sep, int pos) in lastIndexByteTests) {
            Assert.Equal (pos, ustring.Make (s).LastIndexByte ((byte)sep [0]));
        }
    }

    [Fact]
    public void TestIndexRune ()
    {
        (string, uint, int) [] testFirst =
        [
            ("", 'a', -1),
            ("", '☺', -1),
            ("foo", '☹', -1),
            ("foo", 'o', 1),
            ("foo☺bar", '☺', 3),
            ("foo☺☻☹bar", '☹', 9),
            ("a A x", 'A', 2),
            ("some_text=some_value", '=', 9),
            ("☺a", 'a', 3),
            ("a☻☺b", '☺', 4),
        ];

        foreach ((string str, uint rune, int expected) in testFirst) {
            var ustr = ustring.Make (str);
            Assert.Equal (expected, ustr.IndexOf (rune));
        }
    }

    [Fact]
    public void TestConsoleWidth ()
    {
        var sc = new Rune (0xd83d);
        var r = new Rune (0xdd2e);

        Assert.Equal (1, Rune.ColumnWidth (sc));
        Assert.False (Rune.IsNonSpacingChar (r));
        Assert.Equal (1, Rune.ColumnWidth (r));

        var fr = new Rune (sc, r);
        Assert.False (Rune.IsNonSpacingChar (fr));
        Assert.Equal (2, Rune.ColumnWidth (fr));

        var us = ustring.Make (fr);
        Assert.Equal (2, us.ConsoleWidth);
    }

    [Fact]
    public void Test_Substring ()
    {
        ustring us = "This a test to return a substring";
        Assert.Equal ("test to return a substring", us.Substring (7));
        Assert.Equal ("test to return", us.Substring (7, 14));
    }

    [Fact]
    public void Test_RuneSubstring ()
    {
        ustring us = "This a test to return a substring";
        Assert.Equal ("test to return a substring", us.RuneSubstring (7));
        Assert.Equal ("test to return", us.RuneSubstring (7, 14));
    }

    [Fact]
    public void Test_ToRunes ()
    {
        ustring us = "Some long text that 🤖🧠 is super cool";
        uint [] runesArray = us.ToRunes ();
        Assert.Equal (us, runesArray);
    }

    [Fact]
    public void Make_Environment_NewLine ()
    {
        var us = ustring.Make (Environment.NewLine);
        if (Environment.NewLine.Length == 1) {
            Assert.Equal ((byte)'\n', us [0]);
            Assert.Equal (10, us [0]);
        } else {
            Assert.Equal ((byte)'\r', us [0]);
            Assert.Equal (13, us [0]);
            Assert.Equal ((byte)'\n', us [1]);
            Assert.Equal (10, us [1]);
        }
    }

    [Fact]
    public void Substring_Same_As_String_Substring ()
    {
        ustring ustrText = "Check this out 你";
        string str = (string)ustrText.Substring (0, ustrText.Length);
        Assert.Equal (16, str.Length);

        ustring ustr = ustrText.Substring (0, ustrText.Length);
        Assert.Equal (18, ustr.Length);

        Assert.Equal (str, ustr);
        Assert.Equal (str, ustrText);
        Assert.Equal (ustr, ustrText);

        string strText = "Check this out 你";
        str = strText.Substring (0, strText.Length);
        Assert.Equal (16, str.Length);

        ustr = strText.Substring (0, strText.Length);
        Assert.Equal (18, ustr.Length);

        Assert.Equal (str, ustr);
        Assert.Equal (str, strText);
        Assert.Equal (ustr, strText);
    }

    [Fact]
    public void IsNullOrEmpty_Accept_Null_String_Arg ()
    {
        string str = null;
        Assert.True (string.IsNullOrEmpty (str));
        Assert.True (ustring.IsNullOrEmpty (str));

        str = "";
        Assert.True (string.IsNullOrEmpty (str));
        Assert.True (ustring.IsNullOrEmpty (str));

        str = " ";
        Assert.False (string.IsNullOrEmpty (str));
        Assert.False (ustring.IsNullOrEmpty (str));
    }

    [Fact]
    public void IsNullOrEmpty_Accept_Null_Ustring_Arg ()
    {
        ustring ustr = null;
        Assert.True (string.IsNullOrEmpty ((string)ustr));
        Assert.True (ustring.IsNullOrEmpty (ustr));

        ustr = "";
        Assert.True (string.IsNullOrEmpty (ustr.ToString ()));
        Assert.True (ustring.IsNullOrEmpty (ustr));

        ustr = " ";
        Assert.False (string.IsNullOrEmpty (ustr.ToString ()));
        Assert.False (ustring.IsNullOrEmpty (ustr));
    }

    [Fact]
    public void Operator_Equal_Ustring_Versus_String ()
    {
        ustring ustr = null;
        string str = null;
        Assert.True (ustr == str);
        Assert.True (str == ustr);
        Assert.True (ustr == null);
        Assert.True (str == null);
        Assert.False (ustr == "");
        Assert.False (str == "");

        ustr = "";
        str = "";
        Assert.True (ustr == str);
        Assert.True (str == ustr);
        Assert.False (ustr == null);
        Assert.False (str == null);
        Assert.True (ustr == "");
        Assert.True (str == "");

        ustr = " ";
        str = " ";
        Assert.True (ustr == str);
        Assert.True (str == ustr);
        Assert.False (ustr == null);
        Assert.False (str == null);
        Assert.False (ustr == "");
        Assert.False (str == "");
        Assert.True (ustr == " ");
        Assert.True (str == " ");
    }

    [Fact]
    public void Operator_Not_Equal_Ustring_Versus_String ()
    {
        ustring ustr = null;
        string str = null;
        Assert.False (ustr != str);
        Assert.False (str != ustr);
        Assert.False (ustr != null);
        Assert.False (str != null);
        Assert.True (ustr != "");
        Assert.True (str != "");

        ustr = "";
        str = "";
        Assert.False (ustr != str);
        Assert.False (str != ustr);
        Assert.True (ustr != null);
        Assert.True (str != null);
        Assert.False (ustr != "");
        Assert.False (str != "");

        ustr = " ";
        str = " ";
        Assert.False (ustr != str);
        Assert.False (str != ustr);
        Assert.True (ustr != null);
        Assert.True (str != null);
        Assert.True (ustr != "");
        Assert.True (str != "");
        Assert.False (ustr != " ");
        Assert.False (str != " ");
    }

    [Fact]
    public void Ustring_Array_Is_Not_Equal_ToRunes_Array_And_String_Array ()
    {
        var text = "New Test 你";
        ustring us = text;
        string s = text;

        Assert.Equal (10, us.RuneCount);
        Assert.Equal (10, s.Length);

        // The reason is ustring index is related to byte length and not rune length
        Assert.Equal (12, us.Length);
        Assert.NotEqual (20320, us [9]);
        Assert.Equal (20320, s [9]);
        Assert.Equal (228, us [9]);
        Assert.Equal ("ä", ((Rune)us [9]).ToString ());
        Assert.Equal ("你", s [9].ToString ());

        // Rune array is equal to string array
        var usToRunes = us.ToRunes ();
        Assert.Equal (10, usToRunes.Length);
        Assert.Equal (10, s.Length);
        Assert.Equal ((uint)20320, usToRunes [9]);
        Assert.Equal (20320, s [9]);
        Assert.Equal ("你", ((Rune)usToRunes [9]).ToString ());
        Assert.Equal ("你", s [9].ToString ());
    }
}
