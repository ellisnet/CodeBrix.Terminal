using System;
using System.Collections.Generic;
using Xunit;
using Rune = System.Rune;

namespace CodeBrix.Terminal.Text.Tests;

/// <summary>
/// Tests for untested ustring methods: ToUpper, ToTitle, Title, StartsWith,
/// EndsWith, Concat, Join, operator+, Explode, RuneAt, predicate-based
/// IndexOf/Trim, Make overloads, IsSeparator, IsEmpty, and GetHashCode.
/// </summary>
public class UStringAdditionalTests
{
	// ───── ToUpper ─────

	[Fact]
	public void ToUpper_AsciiString ()
	{
		ustring s = ustring.Make ("hello");
		ustring result = s.ToUpper ();
		Assert.Equal ("HELLO", result.ToString ());
	}

	[Fact]
	public void ToUpper_MixedCase ()
	{
		ustring s = ustring.Make ("Hello World");
		Assert.Equal ("HELLO WORLD", s.ToUpper ().ToString ());
	}

	[Fact]
	public void ToUpper_AlreadyUpper ()
	{
		ustring s = ustring.Make ("ABC");
		Assert.Equal ("ABC", s.ToUpper ().ToString ());
	}

	[Fact]
	public void ToUpper_NonLatin ()
	{
		// ü (U+00FC) -> Ü (U+00DC)
		ustring s = ustring.Make ("\u00fc");
		Assert.Equal ("\u00dc", s.ToUpper ().ToString ());
	}

	// ───── ToTitle (all characters to title case) ─────

	[Fact]
	public void ToTitle_AsciiString ()
	{
		ustring s = ustring.Make ("hello");
		// ToTitle maps each individual rune to title case (not the same as Title())
		Assert.Equal ("HELLO", s.ToTitle ().ToString ());
	}

	// ───── Title (word-level title case) ─────

	[Fact]
	public void Title_SimpleWords ()
	{
		ustring s = ustring.Make ("hello world");
		Assert.Equal ("Hello World", s.Title ().ToString ());
	}

	[Fact]
	public void Title_AlreadyTitleCase ()
	{
		ustring s = ustring.Make ("Hello World");
		Assert.Equal ("Hello World", s.Title ().ToString ());
	}

	[Fact]
	public void Title_MultipleSpaces ()
	{
		ustring s = ustring.Make ("hello  world");
		Assert.Equal ("Hello  World", s.Title ().ToString ());
	}

	[Fact]
	public void Title_WithPunctuation ()
	{
		ustring s = ustring.Make ("hello, world!");
		// Punctuation is a separator, so character after it gets title-cased
		Assert.Equal ("Hello, World!", s.Title ().ToString ());
	}

	[Fact]
	public void Title_SingleWord ()
	{
		ustring s = ustring.Make ("hello");
		Assert.Equal ("Hello", s.Title ().ToString ());
	}

	// ───── ToUpper/ToLower/ToTitle with SpecialCase ─────

	[Fact]
	public void ToUpper_TurkishCase ()
	{
		ustring s = ustring.Make ("i");
		ustring result = s.ToUpper (Unicode.TurkishCase);
		// Turkish uppercase of 'i' is İ (U+0130)
		Assert.Equal ("\u0130", result.ToString ());
	}

	[Fact]
	public void ToLower_TurkishCase ()
	{
		ustring s = ustring.Make ("I");
		ustring result = s.ToLower (Unicode.TurkishCase);
		// Turkish lowercase of 'I' is ı (U+0131)
		Assert.Equal ("\u0131", result.ToString ());
	}

	[Fact]
	public void ToTitle_TurkishCase ()
	{
		ustring s = ustring.Make ("i");
		ustring result = s.ToTitle (Unicode.TurkishCase);
		Assert.Equal ("\u0130", result.ToString ());
	}

	// ───── StartsWith ─────

	[Fact]
	public void StartsWith_True ()
	{
		ustring s = ustring.Make ("hello world");
		Assert.True (s.StartsWith (ustring.Make ("hello")));
	}

	[Fact]
	public void StartsWith_False ()
	{
		ustring s = ustring.Make ("hello world");
		Assert.False (s.StartsWith (ustring.Make ("world")));
	}

	[Fact]
	public void StartsWith_EmptyPrefix ()
	{
		ustring s = ustring.Make ("hello");
		Assert.True (s.StartsWith (ustring.Empty));
	}

	[Fact]
	public void StartsWith_PrefixLongerThanString ()
	{
		ustring s = ustring.Make ("hi");
		Assert.False (s.StartsWith (ustring.Make ("hello world")));
	}

	[Fact]
	public void StartsWith_ExactMatch ()
	{
		ustring s = ustring.Make ("hello");
		Assert.True (s.StartsWith (ustring.Make ("hello")));
	}

	// ───── EndsWith ─────

	[Fact]
	public void EndsWith_True ()
	{
		ustring s = ustring.Make ("hello world");
		Assert.True (s.EndsWith (ustring.Make ("world")));
	}

	[Fact]
	public void EndsWith_False ()
	{
		ustring s = ustring.Make ("hello world");
		Assert.False (s.EndsWith (ustring.Make ("hello")));
	}

	[Fact]
	public void EndsWith_EmptySuffix ()
	{
		ustring s = ustring.Make ("hello");
		Assert.True (s.EndsWith (ustring.Empty));
	}

	[Fact]
	public void EndsWith_SuffixLongerThanString ()
	{
		ustring s = ustring.Make ("hi");
		Assert.False (s.EndsWith (ustring.Make ("hello world")));
	}

	[Fact]
	public void EndsWith_ExactMatch ()
	{
		ustring s = ustring.Make ("hello");
		Assert.True (s.EndsWith (ustring.Make ("hello")));
	}

	// ───── Concat ─────

	[Fact]
	public void Concat_TwoStrings ()
	{
		ustring result = ustring.Concat (ustring.Make ("hello"), ustring.Make (" world"));
		Assert.Equal ("hello world", result.ToString ());
	}

	[Fact]
	public void Concat_MultipleStrings ()
	{
		ustring result = ustring.Concat (
			ustring.Make ("a"),
			ustring.Make ("b"),
			ustring.Make ("c"));
		Assert.Equal ("abc", result.ToString ());
	}

	[Fact]
	public void Concat_WithEmpty ()
	{
		ustring result = ustring.Concat (ustring.Make ("hello"), ustring.Empty);
		Assert.Equal ("hello", result.ToString ());
	}

	// ───── Join ─────

	[Fact]
	public void Join_WithEmptySeparator ()
	{
		ustring result = ustring.Join (
			ustring.Empty,
			ustring.Make ("a"),
			ustring.Make ("b"),
			ustring.Make ("c"));
		Assert.Equal ("abc", result.ToString ());
	}

	[Fact]
	public void Join_NullSeparator_TreatedAsEmpty ()
	{
		ustring result = ustring.Join (
			(ustring)null,
			ustring.Make ("x"),
			ustring.Make ("y"));
		Assert.Equal ("xy", result.ToString ());
	}

	[Fact]
	public void Join_EmptyArray ()
	{
		ustring result = ustring.Join (ustring.Make (", "));
		Assert.True (ustring.IsNullOrEmpty (result));
	}

	[Fact]
	public void Join_SingleElement ()
	{
		ustring result = ustring.Join (ustring.Make (", "), ustring.Make ("only"));
		Assert.Equal ("only", result.ToString ());
	}

	// ───── operator + ─────

	[Fact]
	public void OperatorPlus_ConcatenatesTwoStrings ()
	{
		ustring a = ustring.Make ("hello");
		ustring b = ustring.Make (" world");
		ustring result = a + b;
		Assert.Equal ("hello world", result.ToString ());
	}

	[Fact]
	public void OperatorPlus_WithNull ()
	{
		ustring a = ustring.Make ("hello");
		ustring result = a + (ustring)null;
		Assert.Equal ("hello", result.ToString ());
	}

	[Fact]
	public void OperatorPlus_BothEmpty ()
	{
		ustring result = ustring.Empty + ustring.Empty;
		Assert.Equal (0, result.Length);
	}

	// ───── Explode ─────

	[Fact]
	public void Explode_AsciiString ()
	{
		ustring s = ustring.Make ("abc");
		var parts = s.Explode ();
		Assert.Equal (3, parts.Length);
		Assert.Equal ("a", parts [0].ToString ());
		Assert.Equal ("b", parts [1].ToString ());
		Assert.Equal ("c", parts [2].ToString ());
	}

	[Fact]
	public void Explode_MultiByte ()
	{
		// "é" is 2 bytes in UTF-8
		ustring s = ustring.Make ("\u00e9x");
		var parts = s.Explode ();
		Assert.Equal (2, parts.Length);
		Assert.Equal ("\u00e9", parts [0].ToString ());
		Assert.Equal ("x", parts [1].ToString ());
	}

	[Fact]
	public void Explode_NoLimit ()
	{
		ustring s = ustring.Make ("abcd");
		var parts = s.Explode ();
		Assert.Equal (4, parts.Length);
		Assert.Equal ("a", parts [0].ToString ());
		Assert.Equal ("b", parts [1].ToString ());
		Assert.Equal ("c", parts [2].ToString ());
		Assert.Equal ("d", parts [3].ToString ());
	}

	// ───── RuneAt ─────

	[Fact]
	public void RuneAt_FirstByte ()
	{
		ustring s = ustring.Make ("hello");
		Rune r = s.RuneAt (0);
		Assert.Equal ((uint)'h', (uint)r);
	}

	[Fact]
	public void RuneAt_MiddleByte ()
	{
		ustring s = ustring.Make ("hello");
		Rune r = s.RuneAt (2);
		Assert.Equal ((uint)'l', (uint)r);
	}

	[Fact]
	public void RuneAt_MultiByte ()
	{
		// "Aé" -> A is at index 0, é starts at index 1 (2 bytes)
		ustring s = ustring.Make ("A\u00e9");
		Rune r = s.RuneAt (1);
		Assert.Equal (0x00e9u, (uint)r);
	}

	// ───── IndexOf(RunePredicate) ─────

	[Fact]
	public void IndexOf_Predicate_Found ()
	{
		ustring s = ustring.Make ("hello world");
		int idx = s.IndexOf ((uint rune) => rune == 'w');
		Assert.Equal (6, idx);
	}

	[Fact]
	public void IndexOf_Predicate_NotFound ()
	{
		ustring s = ustring.Make ("hello");
		int idx = s.IndexOf ((uint rune) => rune == 'z');
		Assert.Equal (-1, idx);
	}

	[Fact]
	public void IndexOf_Predicate_IsDigit ()
	{
		ustring s = ustring.Make ("abc123");
		int idx = s.IndexOf ((uint rune) => Unicode.IsDigit (rune));
		Assert.Equal (3, idx);
	}

	// ───── LastIndexOf(RunePredicate) ─────

	[Fact]
	public void LastIndexOf_Predicate_Found ()
	{
		ustring s = ustring.Make ("hello world");
		int idx = s.LastIndexOf ((uint rune) => rune == 'o');
		Assert.Equal (7, idx);
	}

	[Fact]
	public void LastIndexOf_Predicate_NotFound ()
	{
		ustring s = ustring.Make ("hello");
		int idx = s.LastIndexOf ((uint rune) => rune == 'z');
		Assert.Equal (-1, idx);
	}

	// ───── TrimStart(RunePredicate) ─────

	[Fact]
	public void TrimStart_Predicate_RemovesLeadingSpaces ()
	{
		ustring s = ustring.Make ("   hello");
		ustring result = s.TrimStart ((uint rune) => rune == ' ');
		Assert.Equal ("hello", result.ToString ());
	}

	[Fact]
	public void TrimStart_Predicate_NoMatch ()
	{
		ustring s = ustring.Make ("hello");
		ustring result = s.TrimStart ((uint rune) => rune == ' ');
		Assert.Equal ("hello", result.ToString ());
	}

	// ───── TrimEnd(RunePredicate) ─────

	[Fact]
	public void TrimEnd_Predicate_RemovesTrailingSpaces ()
	{
		ustring s = ustring.Make ("hello   ");
		ustring result = s.TrimEnd ((uint rune) => rune == ' ');
		Assert.Equal ("hello", result.ToString ());
	}

	[Fact]
	public void TrimEnd_Predicate_NoMatch ()
	{
		ustring s = ustring.Make ("hello");
		ustring result = s.TrimEnd ((uint rune) => rune == ' ');
		Assert.Equal ("hello", result.ToString ());
	}

	// ───── Trim(RunePredicate) ─────

	[Fact]
	public void Trim_Predicate_RemovesBothEnds ()
	{
		ustring s = ustring.Make ("***hello***");
		ustring result = s.Trim ((uint rune) => rune == '*');
		Assert.Equal ("hello", result.ToString ());
	}

	[Fact]
	public void Trim_Predicate_NoMatch ()
	{
		ustring s = ustring.Make ("hello");
		ustring result = s.Trim ((uint rune) => rune == '*');
		Assert.Equal ("hello", result.ToString ());
	}

	// ───── IsSeparator ─────

	[Fact]
	public void IsSeparator_Space ()
	{
		Assert.True (ustring.IsSeparator (' '));
	}

	[Fact]
	public void IsSeparator_Punctuation ()
	{
		Assert.True (ustring.IsSeparator (','));
		Assert.True (ustring.IsSeparator ('.'));
		Assert.True (ustring.IsSeparator ('!'));
	}

	[Fact]
	public void IsSeparator_Letters_ReturnsFalse ()
	{
		Assert.False (ustring.IsSeparator ('A'));
		Assert.False (ustring.IsSeparator ('z'));
	}

	[Fact]
	public void IsSeparator_Digits_ReturnsFalse ()
	{
		Assert.False (ustring.IsSeparator ('0'));
		Assert.False (ustring.IsSeparator ('9'));
	}

	[Fact]
	public void IsSeparator_Underscore_ReturnsFalse ()
	{
		Assert.False (ustring.IsSeparator ('_'));
	}

	// ───── IsEmpty ─────

	[Fact]
	public void IsEmpty_EmptyString ()
	{
		Assert.True (ustring.Empty.IsEmpty);
	}

	[Fact]
	public void IsEmpty_NonEmptyString ()
	{
		ustring s = ustring.Make ("a");
		Assert.False (s.IsEmpty);
	}

	[Fact]
	public void IsEmpty_EmptyByteArray ()
	{
		ustring s = ustring.Make (Array.Empty<byte> ());
		Assert.True (s.IsEmpty);
	}

	// ───── GetHashCode ─────

	[Fact]
	public void GetHashCode_EqualStringsHaveSameHash ()
	{
		ustring a = ustring.Make ("hello");
		ustring b = ustring.Make ("hello");
		Assert.Equal (a.GetHashCode (), b.GetHashCode ());
	}

	// ───── Make(Rune) ─────

	[Fact]
	public void Make_Rune_Ascii ()
	{
		ustring s = ustring.Make (new Rune ('A'));
		Assert.Equal ("A", s.ToString ());
		Assert.Equal (1, s.Length);
	}

	[Fact]
	public void Make_Rune_MultiByte ()
	{
		// U+00E9 = é (2 bytes)
		ustring s = ustring.Make (new Rune (0x00e9));
		Assert.Equal ("\u00e9", s.ToString ());
		Assert.Equal (2, s.Length);
	}

	[Fact]
	public void Make_Rune_FourByte ()
	{
		// U+1F600 = 😀 (4 bytes)
		ustring s = ustring.Make (new Rune (0x1f600));
		Assert.Equal ("\U0001f600", s.ToString ());
		Assert.Equal (4, s.Length);
	}

	// ───── Make(char[]) ─────

	[Fact]
	public void Make_CharArray ()
	{
		ustring s = ustring.Make ('h', 'i');
		Assert.Equal ("hi", s.ToString ());
	}

	[Fact]
	public void Make_CharArray_NonAscii ()
	{
		ustring s = ustring.Make ('\u00e9'); // é
		Assert.Equal ("\u00e9", s.ToString ());
	}

	// ───── Make(IList<Rune>) ─────

	[Fact]
	public void Make_IListRune ()
	{
		var runes = new System.Collections.Generic.List<Rune> {
			new Rune ('A'),
			new Rune ('B'),
			new Rune ('C')
		};
		ustring s = ustring.Make (runes);
		Assert.Equal ("ABC", s.ToString ());
	}

	// ───── Make(IEnumerable<Rune>) ─────

	[Fact]
	public void Make_IEnumerableRune ()
	{
		IEnumerable<Rune> runes = new Rune [] {
			new Rune ('X'),
			new Rune ('Y')
		};
		ustring s = ustring.Make (runes);
		Assert.Equal ("XY", s.ToString ());
	}

	// ───── Range ─────

	[Fact]
	public void Range_ReturnsIndexAndRune ()
	{
		ustring s = ustring.Make ("A\u00e9"); // "Aé" - A at byte 0, é at byte 1
		var items = new System.Collections.Generic.List<(int index, uint rune)> ();
		foreach (var item in s.Range ())
			items.Add (item);

		Assert.Equal (2, items.Count);
		Assert.Equal (0, items [0].index);
		Assert.Equal ((uint)'A', items [0].rune);
		Assert.Equal (1, items [1].index);
		Assert.Equal (0x00e9u, items [1].rune);
	}

	// ───── explicit operator string ─────

	[Fact]
	public void ExplicitOperator_String ()
	{
		ustring s = ustring.Make ("hello");
		string result = (string)s;
		Assert.Equal ("hello", result);
	}

	[Fact]
	public void ExplicitOperator_String_Null ()
	{
		ustring s = null;
		string result = (string)s;
		Assert.Null (result);
	}

	// ───── IndexByte ─────

	[Fact]
	public void IndexByte_Found ()
	{
		ustring s = ustring.Make ("hello");
		int idx = s.IndexByte ((byte)'l', 0);
		Assert.Equal (2, idx);
	}

	[Fact]
	public void IndexByte_FoundWithOffset ()
	{
		ustring s = ustring.Make ("hello");
		int idx = s.IndexByte ((byte)'l', 3);
		Assert.Equal (3, idx);
	}

	[Fact]
	public void IndexByte_NotFound ()
	{
		ustring s = ustring.Make ("hello");
		int idx = s.IndexByte ((byte)'z', 0);
		Assert.Equal (-1, idx);
	}

	// ───── IndexOfAny(uint[]) ─────

	[Fact]
	public void IndexOfAny_UIntArray ()
	{
		ustring s = ustring.Make ("hello world");
		int idx = s.IndexOfAny ((uint)'w', (uint)'x');
		Assert.Equal (6, idx);
	}

	[Fact]
	public void IndexOfAny_UIntArray_NotFound ()
	{
		ustring s = ustring.Make ("hello");
		int idx = s.IndexOfAny ((uint)'x', (uint)'y', (uint)'z');
		Assert.Equal (-1, idx);
	}

	// ───── ToRuneList ─────

	[Fact]
	public void ToRuneList_ReturnsCorrectRunes ()
	{
		ustring s = ustring.Make ("ABC");
		var list = s.ToRuneList ();
		Assert.Equal (3, list.Count);
		Assert.Equal ((uint)'A', (uint)list [0]);
		Assert.Equal ((uint)'B', (uint)list [1]);
		Assert.Equal ((uint)'C', (uint)list [2]);
	}
}
