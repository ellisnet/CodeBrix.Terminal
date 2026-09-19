//
// Note: does not handle combined, as this code uses Runes, rather than Utf16 encoded chars
//
using CodeBrix.Terminal.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CodeBrix.Terminal.Engine; //was previously: namespace XtermSharp;

[DebuggerDisplay ("Line: {DebuggerDisplay}")]
public class BufferLine {
    CharData [] data = Array.Empty<CharData> ();
    public int Length => data.Length;
    public bool IsWrapped;

    public BufferLine (int cols, CharData? fillCharData, bool isWrapped = false)
    {
        var fill = fillCharData ?? CharData.Null;

        data = new CharData [cols];
        for (int i = 0; i < cols; i++)
            data [i] = fill;
        this.IsWrapped = isWrapped;
    }

    public BufferLine (BufferLine other)
    {
        data = new CharData [other.data.Length];
        other.data.CopyTo (data, 0);
        IsWrapped = other.IsWrapped;
    }

    public CharData this [int idx] {
        get => data [idx];
        set {
            data [idx] = value;
        }
    }

    public int GetWidth (int index) => data [index].Width;

    /**
       * Test whether contains any chars.
       * Basically an empty has no content, but other cells might differ in FG/BG
       * from real empty cells.
       * */
    // TODO: not sue this is completely right
    public bool HasContent (int index) => data [index].Code != 0 || data [index].Attribute != CharData.DefaultAttr;

    public bool HasAnyContent()
    {
        for (int i = 0; i < data.Length; i++) {
            if (HasContent (i)) return true;
        }

        return false;
    }

    string DebuggerDisplay {
        get {
            return TranslateToString (true, 0, -1).ToString ();
        }
    }

    // Turns a cell into a blank one, keeping the attribute of the data being written.
    // Used to clean up the halves of a fullwidth character that cell moves leave behind:
    // a width 2 cell with no placeholder after it, or a placeholder with nothing owning it
    void BlankCell (int index, CharData fillCharData)
    {
        data [index] = new CharData (fillCharData.Attribute);
    }

    public void InsertCells (int pos, int n, int rightMargin, CharData fillCharData)
    {
        var len = Math.Min (rightMargin + 1, Length);
        pos = pos % len;

        // handle fullwidth at pos: reset the cell one to the left if pos is the
        // placeholder of a fullwidth character
        if (pos > 0 && data [pos - 1].Width == 2)
            BlankCell (pos - 1, fillCharData);

        if (n < len - pos) {
            for (var i = len - pos - n - 1; i >= 0; --i)
                data [pos + n + i] = data [pos + i];
            for (var i = 0; i < n; i++)
                data [pos + i] = fillCharData;

            // the first shifted cell is a placeholder whose fullwidth character stayed
            // behind and was blanked above: nothing owns it any more
            if (data [pos + n].Width == 0 && data [pos + n].Code == 0)
                BlankCell (pos + n, fillCharData);
        } else {
            for (var i = pos; i < len; ++i)
                data [i] = fillCharData;
        }

        // handle fullwidth at the end: the placeholder of a character shifted over the
        // end is gone, so the character itself has to go too
        if (len > 0 && data [len - 1].Width == 2)
            BlankCell (len - 1, fillCharData);
    }

    public void DeleteCells (int pos, int n, int rightMargin, CharData fillCharData)
    {
        var len = Math.Min(rightMargin + 1, Length);
        pos %= len;
        if (n < len - pos) {
            for (var i = 0; i < len - pos - n; ++i)
                data [pos + i] = this [pos + n + i];
            for (var i = len - n; i < len; ++i)
                data [i] = fillCharData;
        } else {
            for (var i = pos; i < len; ++i)
                data [i] = fillCharData;
        }

        // handle fullwidth at pos: reset the cell one to the left if it is now a
        // fullwidth character without its placeholder, and reset pos itself if a
        // placeholder was shifted down to it and has nothing owning it any more
        if (pos > 0 && data [pos - 1].Width == 2)
            BlankCell (pos - 1, fillCharData);
        if (data [pos].Width == 0 && data [pos].Code == 0)
            BlankCell (pos, fillCharData);
    }

    public void ReplaceCells (int start, int end, CharData fillCharData)
    {
        var len = Length;

        // handle fullwidth at start: reset the cell one to the left if start is the
        // placeholder of a fullwidth character
        if (start > 0 && start < len && data [start - 1].Width == 2)
            BlankCell (start - 1, fillCharData);

        // handle fullwidth at the last replaced cell: its placeholder, which is the
        // first cell left alone, has nothing owning it any more
        if (end > start && end < len && data [end - 1].Width == 2)
            BlankCell (end, fillCharData);

        while (start < end && start < len)
            data [start++] = fillCharData;
    }
	
    public void Resize (int cols, CharData fillCharData)
    {
        var len = Length;
        if (cols == len)
            return;

        if (cols > len) {
            var newData = new CharData [cols];
            if (len > 0)
                data.CopyTo (newData, 0);
            data = newData;
            for (int i = len; i < cols; i++)
                data [i] = fillCharData;
        } else {
            if (cols > 0) {
                var newData = new CharData [cols];
                Array.Copy (data, newData, cols);
                data = newData;

                // the cut took the placeholder of a fullwidth character in the last
                // cell with it, so that character cannot stay either
                if (data [cols - 1].Width == 2)
                    BlankCell (cols - 1, fillCharData);
            } else {
                data = Array.Empty<CharData> ();
            }
        }
    }

    public void Fill (CharData fillCharData)
    {
        var len = Length;
        for (int i = 0; i < len; i++)
            data [i] = fillCharData;
    }

    public void CopyFrom (BufferLine line)
    {
        if (data.Length != line.Length) 
            data = new CharData [line.Length];
			
        line.data.CopyTo (data, 0);

        IsWrapped = line.IsWrapped;
    }

    public int GetTrimmedLength ()
    {
        for (int i = data.Length - 1; i >= 0; --i)
            if (data [i].Code != 0) {
                // the cells before i are one column each, or a fullwidth character
                // followed by its own placeholder cell, so they always add up to i;
                // the last written cell adds its own width, which is 2 when it is
                // fullwidth and its placeholder follows it.  A fullwidth character
                // cut in half by a narrowing resize would take the count past the
                // end of the line, so the line length is the limit
                return Math.Min (i + data [i].Width, data.Length);
            }
        return 0;
    }

    public void CopyCellsFrom (BufferLine src, int srcCol, int dstCol, int len)
    {
        Array.Copy (src.data, srcCol, data, dstCol, len); 
    }

    public ustring TranslateToString (bool trimRight = false, int startCol = 0, int endCol = -1)
    {
        if (endCol == -1)
            endCol = data.Length;
        if (trimRight) {
            // make sure endCol is not before startCol if we set it to the trimmed length
            endCol = Math.Max (Math.Min (endCol, GetTrimmedLength ()), startCol);
        }

        // a fullwidth character is stored once, in the first of its two cells; the
        // placeholder that follows it carries width 0 and is skipped, so that the
        // character comes back once while still counting as two columns here
        var runes = new List<Rune> (Math.Max (endCol - startCol, 0));
        for (int i = startCol; i < endCol; i++) {
            if (data [i].Width == 0)
                continue;
            runes.Add (data [i].Rune);
        }

        return ustring.Make (runes.ToArray ());
    }
}