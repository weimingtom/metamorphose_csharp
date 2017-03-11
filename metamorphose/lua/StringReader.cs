using metamorphose.java;

/*  $Header: //info.ravenbrook.com/project/jili/version/1.1/code/mnj/lua/StringReader.java#1 $
 * Copyright (c) 2006 Nokia Corporation and/or its subsidiary(-ies).
 * All rights reserved.
 * 
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject
 * to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR
 * ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
 * CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace metamorphose.lua
{

	/// <summary>
	/// Ersatz replacement for <seealso cref="java.io.StringReader"/> from JSE. </summary>
	public class StringReader : Reader
	{
	  private string s;
	  /// <summary>
	  /// Index of the current read position.  -1 if closed. </summary>
	  private int current; // = 0
	  /// <summary>
	  /// Index of the current mark (set with <seealso cref="#mark"/>).
	  /// </summary>
	  private int mark_Renamed; // = 0;

	  internal StringReader(string s)
	  {
		this.s = s;
	  }

	  override public void close()
	  {
		current = -1;
	  }

      override public void mark(int limit)
	  {
		mark_Renamed = current;
	  }

      override public bool markSupported()
	  {
		return true;
	  }

      override public int read()
	  {
		if (current < 0)
		{
		  throw new IOException();
		}
		if (current >= s.Length)
		{
		  return -1;
		}
		return s[current++];
	  }

      override public int read(char[] cbuf, int off, int len)
	  {
		if (current < 0 || len < 0)
		{
		  throw new IOException();
		}
		if (current >= s.Length)
		{
		  return 0;
		}
		if (current + len > s.Length)
		{
		  len = s.Length - current;
		}
		for (int i = 0; i < len; ++i)
		{
		  cbuf[off + i] = s[current + i];
		}
		current += len;
		return len;
	  }

      override public void reset()
	  {
		current = mark_Renamed;
	  }
	}

}