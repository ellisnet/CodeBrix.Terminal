using Xunit;
using Rune = System.Rune;

namespace CodeBrix.Terminal.Text.Tests;

/// <summary>
/// Tests for Utf8 ustring overloads (FullRune, DecodeRune, DecodeLastRune,
/// RuneCount, Valid, InvalidIndex) and Unicode classification gaps
/// (IsMark non-Latin, IsTitle).
/// </summary>
public class Utf8UStringOverloadTests
{
	// ───── Utf8.FullRune(ustring) ─────

	[Fact]
	public void FullRune_Ustring_Ascii ()
	{
		ustring s = ustring.Make ("A");
		Assert.True (Utf8.FullRune (s));
	}

	[Fact]
	public void FullRune_Ustring_MultiByteComplete ()
	{
		// é = U+00E9 -> 2 bytes: 0xC3, 0xA9
		ustring s = ustring.Make ("\u00e9");
		Assert.True (Utf8.FullRune (s));
	}

	[Fact]
	public void FullRune_Ustring_Incomplete ()
	{
		// First byte of a 2-byte sequence, missing continuation byte
		ustring s = ustring.Make (new byte [] { 0xC3 });
		Assert.False (Utf8.FullRune (s));
	}

	[Fact]
	public void FullRune_Ustring_Empty ()
	{
		Assert.False (Utf8.FullRune (ustring.Empty));
	}

	[Fact]
	public void FullRune_Ustring_FourByte ()
	{
		// U+1F600 = 😀 (4 bytes)
		ustring s = ustring.Make ("\U0001f600");
		Assert.True (Utf8.FullRune (s));
	}

	// ───── Utf8.DecodeRune(ustring) ─────

	[Fact]
	public void DecodeRune_Ustring_Ascii ()
	{
		ustring s = ustring.Make ("Hello");
		var (rune, size) = Utf8.DecodeRune (s);
		Assert.Equal ((uint)'H', rune);
		Assert.Equal (1, size);
	}

	[Fact]
	public void DecodeRune_Ustring_MultiByte ()
	{
		ustring s = ustring.Make ("\u00e9"); // é
		var (rune, size) = Utf8.DecodeRune (s);
		Assert.Equal (0x00e9u, rune);
		Assert.Equal (2, size);
	}

	[Fact]
	public void DecodeRune_Ustring_FourByte ()
	{
		ustring s = ustring.Make ("\U0001f600"); // 😀
		var (rune, size) = Utf8.DecodeRune (s);
		Assert.Equal (0x1f600u, rune);
		Assert.Equal (4, size);
	}

	[Fact]
	public void DecodeRune_Ustring_WithOffset ()
	{
		ustring s = ustring.Make ("AB");
		var (rune, size) = Utf8.DecodeRune (s, 1);
		Assert.Equal ((uint)'B', rune);
		Assert.Equal (1, size);
	}

	[Fact]
	public void DecodeRune_Ustring_Empty ()
	{
		ustring s = ustring.Empty;
		var (rune, size) = Utf8.DecodeRune (s);
		Assert.Equal (Utf8.RuneError, rune);
		Assert.Equal (0, size);
	}

	// ───── Utf8.DecodeLastRune(ustring) ─────

	[Fact]
	public void DecodeLastRune_Ustring_Ascii ()
	{
		ustring s = ustring.Make ("Hello");
		var (rune, size) = Utf8.DecodeLastRune (s);
		Assert.Equal ((uint)'o', rune);
		Assert.Equal (1, size);
	}

	[Fact]
	public void DecodeLastRune_Ustring_MultiByte ()
	{
		ustring s = ustring.Make ("A\u00e9"); // "Aé"
		var (rune, size) = Utf8.DecodeLastRune (s);
		Assert.Equal (0x00e9u, rune);
		Assert.Equal (2, size);
	}

	[Fact]
	public void DecodeLastRune_Ustring_Empty ()
	{
		ustring s = ustring.Empty;
		var (rune, size) = Utf8.DecodeLastRune (s);
		Assert.Equal (Utf8.RuneError, rune);
		Assert.Equal (0, size);
	}

	[Fact]
	public void DecodeLastRune_Ustring_WithEnd ()
	{
		ustring s = ustring.Make ("ABC");
		// Decode last rune up to position 2 (so consider "AB")
		var (rune, size) = Utf8.DecodeLastRune (s, 2);
		Assert.Equal ((uint)'B', rune);
		Assert.Equal (1, size);
	}

	// ───── Utf8.RuneCount(ustring) ─────

	[Fact]
	public void RuneCount_Ustring_Ascii ()
	{
		ustring s = ustring.Make ("hello");
		Assert.Equal (5, Utf8.RuneCount (s));
	}

	[Fact]
	public void RuneCount_Ustring_MultiByte ()
	{
		// "Aé" -> 2 runes (but 3 bytes)
		ustring s = ustring.Make ("A\u00e9");
		Assert.Equal (2, Utf8.RuneCount (s));
	}

	[Fact]
	public void RuneCount_Ustring_Empty ()
	{
		Assert.Equal (0, Utf8.RuneCount (ustring.Empty));
	}

	[Fact]
	public void RuneCount_Ustring_FourByteChars ()
	{
		// "😀😀" -> 2 runes (8 bytes)
		ustring s = ustring.Make ("\U0001f600\U0001f600");
		Assert.Equal (2, Utf8.RuneCount (s));
	}

	// ───── Utf8.Valid(ustring) ─────

	[Fact]
	public void Valid_Ustring_ValidAscii ()
	{
		ustring s = ustring.Make ("hello");
		Assert.True (Utf8.Valid (s));
	}

	[Fact]
	public void Valid_Ustring_ValidMultiByte ()
	{
		ustring s = ustring.Make ("\u00e9\u4e16\U0001f600");
		Assert.True (Utf8.Valid (s));
	}

	[Fact]
	public void Valid_Ustring_Invalid ()
	{
		// 0xFF is never valid as a UTF-8 byte
		ustring s = ustring.Make (new byte [] { 0xFF });
		Assert.False (Utf8.Valid (s));
	}

	[Fact]
	public void Valid_Ustring_Empty ()
	{
		Assert.True (Utf8.Valid (ustring.Empty));
	}

	// ───── Utf8.InvalidIndex(ustring) ─────

	[Fact]
	public void InvalidIndex_Ustring_AllValid ()
	{
		ustring s = ustring.Make ("hello");
		Assert.Equal (-1, Utf8.InvalidIndex (s));
	}

	[Fact]
	public void InvalidIndex_Ustring_InvalidAtStart ()
	{
		ustring s = ustring.Make (new byte [] { 0xFF, 0x41, 0x42 });
		Assert.Equal (0, Utf8.InvalidIndex (s));
	}

	[Fact]
	public void InvalidIndex_Ustring_InvalidInMiddle ()
	{
		ustring s = ustring.Make (new byte [] { 0x41, 0x42, 0xFF, 0x43 });
		Assert.Equal (2, Utf8.InvalidIndex (s));
	}

	[Fact]
	public void InvalidIndex_Ustring_Empty ()
	{
		Assert.Equal (-1, Utf8.InvalidIndex (ustring.Empty));
	}

	// ───── Utf8.InvalidIndex(byte[]) - also untested ─────

	[Fact]
	public void InvalidIndex_ByteArray_AllValid ()
	{
		Assert.Equal (-1, Utf8.InvalidIndex (new byte [] { 0x41, 0x42, 0x43 }));
	}

	[Fact]
	public void InvalidIndex_ByteArray_InvalidByte ()
	{
		Assert.Equal (1, Utf8.InvalidIndex (new byte [] { 0x41, 0xFF, 0x42 }));
	}

	[Fact]
	public void InvalidIndex_ByteArray_TruncatedSequence ()
	{
		// 0xC3 starts a 2-byte sequence but there's no continuation
		Assert.Equal (2, Utf8.InvalidIndex (new byte [] { 0x41, 0x42, 0xC3 }));
	}

	// ═══════════════════════════════════════════════════════════════
	// Unicode classification gaps
	// ═══════════════════════════════════════════════════════════════

	// ───── Unicode.IsMark (non-Latin range) ─────

	[Fact]
	public void IsMark_CombiningGraveAccent ()
	{
		// U+0300 = Combining Grave Accent (category Mn)
		Assert.True (Unicode.IsMark (0x0300));
	}

	[Fact]
	public void IsMark_CombiningAcuteAccent ()
	{
		// U+0301 = Combining Acute Accent
		Assert.True (Unicode.IsMark (0x0301));
	}

	[Fact]
	public void IsMark_CombiningCircumflex ()
	{
		// U+0302 = Combining Circumflex Accent
		Assert.True (Unicode.IsMark (0x0302));
	}

	[Fact]
	public void IsMark_AsciiLetter_ReturnsFalse ()
	{
		Assert.False (Unicode.IsMark (0x41)); // 'A'
	}

	[Fact]
	public void IsMark_AsciiDigit_ReturnsFalse ()
	{
		Assert.False (Unicode.IsMark (0x30)); // '0'
	}

	[Fact]
	public void IsMark_DevanagariVowelSign ()
	{
		// U+093E = Devanagari Vowel Sign AA (category Mc)
		Assert.True (Unicode.IsMark (0x093E));
	}

	// ───── Unicode.IsTitle ─────

	[Fact]
	public void IsTitle_TitlecaseChar ()
	{
		// U+01C5 = Latin Capital Letter D with Small Letter Z with Caron (Lt)
		Assert.True (Unicode.IsTitle (0x01C5));
	}

	[Fact]
	public void IsTitle_LatinCapitalLetter_ReturnsFalse ()
	{
		Assert.False (Unicode.IsTitle (0x41)); // 'A' is Lu, not Lt
	}

	[Fact]
	public void IsTitle_LatinSmallLetter_ReturnsFalse ()
	{
		Assert.False (Unicode.IsTitle (0x61)); // 'a'
	}

	[Fact]
	public void IsTitle_Lj_Digraph ()
	{
		// U+01C8 = Latin Capital Letter L with Small Letter J (Lt)
		Assert.True (Unicode.IsTitle (0x01C8));
	}

	// ───── Parity: Utf8 byte[] and ustring produce same results ─────

	[Fact]
	public void DecodeRune_Parity_ByteArrayAndUstring ()
	{
		byte [] bytes = new byte [] { 0xE4, 0xB8, 0x96 }; // U+4E16 = 世
		ustring s = ustring.Make (bytes);

		var (runeBytes, sizeBytes) = Utf8.DecodeRune (bytes);
		var (runeUstring, sizeUstring) = Utf8.DecodeRune (s);

		Assert.Equal (runeBytes, runeUstring);
		Assert.Equal (sizeBytes, sizeUstring);
	}

	[Fact]
	public void DecodeLastRune_Parity_ByteArrayAndUstring ()
	{
		byte [] bytes = new byte [] { 0x41, 0xE4, 0xB8, 0x96 }; // "A世"
		ustring s = ustring.Make (bytes);

		var (runeBytes, sizeBytes) = Utf8.DecodeLastRune (bytes);
		var (runeUstring, sizeUstring) = Utf8.DecodeLastRune (s);

		Assert.Equal (runeBytes, runeUstring);
		Assert.Equal (sizeBytes, sizeUstring);
	}

	[Fact]
	public void RuneCount_Parity_ByteArrayAndUstring ()
	{
		byte [] bytes = new byte [] { 0x41, 0xC3, 0xA9, 0xE4, 0xB8, 0x96 }; // "Aé世"
		ustring s = ustring.Make (bytes);

		Assert.Equal (Utf8.RuneCount (bytes), Utf8.RuneCount (s));
	}

	[Fact]
	public void InvalidIndex_Parity_ByteArrayAndUstring ()
	{
		byte [] bytes = new byte [] { 0x41, 0xFF, 0x42 };
		ustring s = ustring.Make (bytes);

		Assert.Equal (Utf8.InvalidIndex (bytes), Utf8.InvalidIndex (s));
	}
}
