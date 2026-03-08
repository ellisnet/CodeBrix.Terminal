using Xunit;
using Rune = System.Rune;

namespace CodeBrix.Terminal.Text.Tests;

/// <summary>
/// Tests for Rune static classification methods, case conversion methods,
/// and fundamental struct overrides (ToString, Equals, GetHashCode).
/// </summary>
public class RuneClassificationTests
{
	// ───── ToString ─────

	[Fact]
	public void ToString_Ascii ()
	{
		var r = new Rune ('A');
		Assert.Equal ("A", r.ToString ());
	}

	[Fact]
	public void ToString_MultiByteRune ()
	{
		// U+00E9 = é (2-byte UTF-8)
		var r = new Rune (0x00e9);
		Assert.Equal ("\u00e9", r.ToString ());
	}

	[Fact]
	public void ToString_ThreeByteRune ()
	{
		// U+4E16 = 世 (3-byte UTF-8)
		var r = new Rune (0x4e16);
		Assert.Equal ("\u4e16", r.ToString ());
	}

	[Fact]
	public void ToString_FourByteRune ()
	{
		// U+1F600 = 😀 (4-byte UTF-8)
		var r = new Rune (0x1f600);
		Assert.Equal ("\U0001f600", r.ToString ());
	}

	// ───── Equals ─────

	[Fact]
	public void Equals_SameValue_ReturnsTrue ()
	{
		var a = new Rune ('X');
		var b = new Rune ('X');
		Assert.True (a.Equals ((object)b));
	}

	[Fact]
	public void Equals_DifferentValue_ReturnsFalse ()
	{
		var a = new Rune ('X');
		var b = new Rune ('Y');
		Assert.False (a.Equals ((object)b));
	}

	[Fact]
	public void Equals_Null_ReturnsFalse ()
	{
		var a = new Rune ('X');
		Assert.False (a.Equals (null));
	}

	// ───── GetHashCode ─────

	[Fact]
	public void GetHashCode_ConsistentWithValue ()
	{
		var r = new Rune (0x1234);
		Assert.Equal ((int)r.Value, r.GetHashCode ());
	}

	[Fact]
	public void GetHashCode_EqualRunesHaveSameHash ()
	{
		var a = new Rune ('Z');
		var b = new Rune ('Z');
		Assert.Equal (a.GetHashCode (), b.GetHashCode ());
	}

	// ───── Rune(char) constructor ─────

	[Fact]
	public void Constructor_Char ()
	{
		var r = new Rune ('A');
		Assert.Equal ((uint)'A', r.Value);
	}

	[Fact]
	public void Constructor_Char_NonAscii ()
	{
		var r = new Rune ('\u00e9'); // é
		Assert.Equal (0x00e9u, r.Value);
	}

	// ───── Implicit operators ─────

	[Fact]
	public void ImplicitOperator_IntToRune ()
	{
		Rune r = 65; // 'A'
		Assert.Equal (65u, r.Value);
	}

	[Fact]
	public void ImplicitOperator_ByteToRune ()
	{
		Rune r = (byte)0x41;
		Assert.Equal (0x41u, r.Value);
	}

	[Fact]
	public void ImplicitOperator_CharToRune ()
	{
		Rune r = 'A';
		Assert.Equal ((uint)'A', r.Value);
	}

	[Fact]
	public void ImplicitOperator_UIntToRune ()
	{
		Rune r = 0x1F600u;
		Assert.Equal (0x1F600u, r.Value);
	}

	[Fact]
	public void ImplicitOperator_RuneToUInt ()
	{
		var r = new Rune (0x1234);
		uint val = r;
		Assert.Equal (0x1234u, val);
	}

	// ───── IsDigit ─────

	[Fact]
	public void IsDigit_AsciiDigits ()
	{
		for (char c = '0'; c <= '9'; c++)
			Assert.True (Rune.IsDigit (new Rune (c)), $"Expected '{c}' to be a digit");
	}

	[Fact]
	public void IsDigit_AsciiLetters_ReturnsFalse ()
	{
		Assert.False (Rune.IsDigit (new Rune ('A')));
		Assert.False (Rune.IsDigit (new Rune ('z')));
	}

	// ───── IsGraphic ─────

	[Fact]
	public void IsGraphic_PrintableChars ()
	{
		Assert.True (Rune.IsGraphic (new Rune ('A')));
		Assert.True (Rune.IsGraphic (new Rune ('1')));
		Assert.True (Rune.IsGraphic (new Rune ('!')));
	}

	[Fact]
	public void IsGraphic_ControlChars_ReturnsFalse ()
	{
		Assert.False (Rune.IsGraphic (new Rune ('\0')));
		Assert.False (Rune.IsGraphic (new Rune ('\n')));
		Assert.False (Rune.IsGraphic (new Rune (0x7f))); // DEL
	}

	// ───── IsPrint ─────

	[Fact]
	public void IsPrint_PrintableChars ()
	{
		Assert.True (Rune.IsPrint (new Rune ('A')));
		Assert.True (Rune.IsPrint (new Rune (' '))); // ASCII space is printable
	}

	[Fact]
	public void IsPrint_ControlChars_ReturnsFalse ()
	{
		Assert.False (Rune.IsPrint (new Rune ('\0')));
		Assert.False (Rune.IsPrint (new Rune ('\t')));
	}

	// ───── IsControl ─────

	[Fact]
	public void IsControl_ControlChars ()
	{
		Assert.True (Rune.IsControl (new Rune ('\0')));
		Assert.True (Rune.IsControl (new Rune ('\n')));
		Assert.True (Rune.IsControl (new Rune ('\r')));
		Assert.True (Rune.IsControl (new Rune (0x7f))); // DEL
		Assert.True (Rune.IsControl (new Rune (0x80))); // C1 control
	}

	[Fact]
	public void IsControl_Letters_ReturnsFalse ()
	{
		Assert.False (Rune.IsControl (new Rune ('A')));
		Assert.False (Rune.IsControl (new Rune ('5')));
	}

	// ───── IsLetterOrDigit ─────

	[Fact]
	public void IsLetterOrDigit_Letters ()
	{
		Assert.True (Rune.IsLetterOrDigit (new Rune ('A')));
		Assert.True (Rune.IsLetterOrDigit (new Rune ('z')));
	}

	[Fact]
	public void IsLetterOrDigit_Digits ()
	{
		Assert.True (Rune.IsLetterOrDigit (new Rune ('0')));
		Assert.True (Rune.IsLetterOrDigit (new Rune ('9')));
	}

	[Fact]
	public void IsLetterOrDigit_Punctuation_ReturnsFalse ()
	{
		Assert.False (Rune.IsLetterOrDigit (new Rune ('!')));
		Assert.False (Rune.IsLetterOrDigit (new Rune (' ')));
		Assert.False (Rune.IsLetterOrDigit (new Rune ('\n')));
	}

	// ───── IsLetterOrNumber ─────

	[Fact]
	public void IsLetterOrNumber_Letter ()
	{
		Assert.True (Rune.IsLetterOrNumber (new Rune ('A')));
	}

	[Fact]
	public void IsLetterOrNumber_Number ()
	{
		// Digit is a subset of Number
		Assert.True (Rune.IsLetterOrNumber (new Rune ('5')));
	}

	[Fact]
	public void IsLetterOrNumber_RomanNumeral ()
	{
		// U+2160 = Roman numeral I (category Nl - Number, letter)
		Assert.True (Rune.IsLetterOrNumber (new Rune (0x2160)));
	}

	[Fact]
	public void IsLetterOrNumber_Punctuation_ReturnsFalse ()
	{
		Assert.False (Rune.IsLetterOrNumber (new Rune ('.')));
		Assert.False (Rune.IsLetterOrNumber (new Rune (' ')));
	}

	// ───── IsMark ─────

	[Fact]
	public void IsMark_CombiningMark ()
	{
		// U+0300 = Combining Grave Accent (category Mn)
		Assert.True (Rune.IsMark (new Rune (0x0300)));
		// U+0302 = Combining Circumflex Accent
		Assert.True (Rune.IsMark (new Rune (0x0302)));
	}

	[Fact]
	public void IsMark_AsciiLetter_ReturnsFalse ()
	{
		Assert.False (Rune.IsMark (new Rune ('A')));
	}

	// ───── IsNumber ─────

	[Fact]
	public void IsNumber_AsciiDigit ()
	{
		Assert.True (Rune.IsNumber (new Rune ('0')));
		Assert.True (Rune.IsNumber (new Rune ('9')));
	}

	[Fact]
	public void IsNumber_Letter_ReturnsFalse ()
	{
		Assert.False (Rune.IsNumber (new Rune ('A')));
	}

	// ───── IsPunctuation ─────

	[Fact]
	public void IsPunctuation_PunctChars ()
	{
		Assert.True (Rune.IsPunctuation (new Rune ('.')));
		Assert.True (Rune.IsPunctuation (new Rune (',')));
		Assert.True (Rune.IsPunctuation (new Rune ('!')));
		Assert.True (Rune.IsPunctuation (new Rune ('?')));
	}

	[Fact]
	public void IsPunctuation_Letter_ReturnsFalse ()
	{
		Assert.False (Rune.IsPunctuation (new Rune ('A')));
		Assert.False (Rune.IsPunctuation (new Rune ('5')));
	}

	// ───── IsWhiteSpace ─────

	[Fact]
	public void IsWhiteSpace_WhitespaceChars ()
	{
		Assert.True (Rune.IsWhiteSpace (new Rune (' ')));
		Assert.True (Rune.IsWhiteSpace (new Rune ('\t')));
		Assert.True (Rune.IsWhiteSpace (new Rune ('\n')));
		Assert.True (Rune.IsWhiteSpace (new Rune ('\r')));
	}

	[Fact]
	public void IsWhiteSpace_Letter_ReturnsFalse ()
	{
		Assert.False (Rune.IsWhiteSpace (new Rune ('A')));
		Assert.False (Rune.IsWhiteSpace (new Rune ('5')));
	}

	// ───── IsSymbol ─────

	[Fact]
	public void IsSymbol_SymbolChars ()
	{
		Assert.True (Rune.IsSymbol (new Rune ('$')));
		Assert.True (Rune.IsSymbol (new Rune ('+')));
		Assert.True (Rune.IsSymbol (new Rune ('|')));
	}

	[Fact]
	public void IsSymbol_Letter_ReturnsFalse ()
	{
		Assert.False (Rune.IsSymbol (new Rune ('A')));
	}

	// ───── IsUpper / IsLower / IsTitle (Rune) ─────

	[Fact]
	public void IsUpper_Rune ()
	{
		Assert.True (Rune.IsUpper (new Rune ('A')));
		Assert.True (Rune.IsUpper (new Rune ('Z')));
		Assert.False (Rune.IsUpper (new Rune ('a')));
		Assert.False (Rune.IsUpper (new Rune ('5')));
	}

	[Fact]
	public void IsLower_Rune ()
	{
		Assert.True (Rune.IsLower (new Rune ('a')));
		Assert.True (Rune.IsLower (new Rune ('z')));
		Assert.False (Rune.IsLower (new Rune ('A')));
		Assert.False (Rune.IsLower (new Rune ('5')));
	}

	[Fact]
	public void IsTitle_Rune ()
	{
		// U+01C5 = Latin Capital Letter D with Small Letter Z with Caron (titlecase)
		Assert.True (Rune.IsTitle (new Rune (0x01C5)));
		Assert.False (Rune.IsTitle (new Rune ('A')));
		Assert.False (Rune.IsTitle (new Rune ('a')));
	}

	// ───── To(Case, Rune) ─────

	[Fact]
	public void To_Upper ()
	{
		var result = Rune.To (Rune.Case.Upper, new Rune ('a'));
		Assert.Equal (new Rune ('A').Value, result.Value);
	}

	[Fact]
	public void To_Lower ()
	{
		var result = Rune.To (Rune.Case.Lower, new Rune ('A'));
		Assert.Equal (new Rune ('a').Value, result.Value);
	}

	[Fact]
	public void To_Title ()
	{
		var result = Rune.To (Rune.Case.Title, new Rune ('a'));
		Assert.Equal (new Rune ('A').Value, result.Value);
	}

	// ───── ToUpper / ToLower / ToTitle (Rune) ─────

	[Fact]
	public void ToUpper_Rune ()
	{
		Rune result = Rune.ToUpper (new Rune ('a'));
		Assert.Equal (new Rune ('A').Value, result.Value);
	}

	[Fact]
	public void ToUpper_Rune_AlreadyUpper ()
	{
		Rune result = Rune.ToUpper (new Rune ('A'));
		Assert.Equal (new Rune ('A').Value, result.Value);
	}

	[Fact]
	public void ToLower_Rune ()
	{
		Rune result = Rune.ToLower (new Rune ('A'));
		Assert.Equal (new Rune ('a').Value, result.Value);
	}

	[Fact]
	public void ToTitle_Rune ()
	{
		Rune result = Rune.ToTitle (new Rune ('a'));
		Assert.Equal (new Rune ('A').Value, result.Value);
	}

	// ───── SimpleFold (Rune) ─────

	[Fact]
	public void SimpleFold_Rune ()
	{
		// SimpleFold('A') = 'a'
		Rune result = Rune.SimpleFold (new Rune ('A'));
		Assert.Equal (new Rune ('a').Value, result.Value);

		// SimpleFold('a') = 'A'
		result = Rune.SimpleFold (new Rune ('a'));
		Assert.Equal (new Rune ('A').Value, result.Value);
	}

	[Fact]
	public void SimpleFold_Digit_ReturnsSelf ()
	{
		Rune result = Rune.SimpleFold (new Rune ('1'));
		Assert.Equal (new Rune ('1').Value, result.Value);
	}

	// ───── IsLetter (Rune) ─────

	[Fact]
	public void IsLetter_Rune ()
	{
		Assert.True (Rune.IsLetter (new Rune ('A')));
		Assert.True (Rune.IsLetter (new Rune ('z')));
		Assert.False (Rune.IsLetter (new Rune ('5')));
		Assert.False (Rune.IsLetter (new Rune (' ')));
	}
}
