using metamorphose.java;

/*  $Header: //info.ravenbrook.com/project/jili/version/1.1/code/mnj/lua/DumpedInput.java#1 $
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
	/// Converts a string obtained using string.dump into an
	/// <seealso cref="java.io.InputStream"/> so that it can be passed to {@link
	/// Lua#load(java.io.InputStream, java.lang.String)}.
	/// </summary>
	internal sealed class DumpedInput : InputStream
	{
	  private string s;
	  private int i; // = 0
	  internal int mark_Renamed = -1;

	  internal DumpedInput(string s)
	  {
		this.s = s;
	  }

	  override public int available()
	  {
		return s.Length - i;
	  }

      override public void close()
	  {
		s = null;
		i = -1;
	  }

      override public void mark(int readlimit)
	  {
		mark_Renamed = i;
	  }

      override public bool markSupported()
	  {
		return true;
	  }

      override public int read()
	  {
		if (i >= s.Length)
		{
		  return -1;
		}
		char c = s[i];
		++i;
		return c & 0xff;
	  }

      override public void reset()
	  {
		i = mark_Renamed;
	  }
	}

}