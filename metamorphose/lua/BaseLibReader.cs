using metamorphose.java;

/*  $Header: //info.ravenbrook.com/project/jili/version/1.1/code/mnj/lua/BaseLibReader.java#1 $
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
	/// Extends <seealso cref="java.io.Reader"/> to create a Reader from a Lua
	/// function.  So that the <code>load</code> function from Lua's base
	/// library can be implemented.
	/// </summary>
	public class BaseLibReader : Reader
	{
	  private string s = "";
	  private int i; // = 0;
	  private int mark_Renamed = -1;
	  private Lua L;
	  private object f;

	  public BaseLibReader(Lua L, object f)
	  {
		this.L = L;
		this.f = f;
	  }

	  override public void close()
	  {
		f = null;
	  }

	  override public void mark(int l)
	  {
		if (l > 1)
		{
		  throw new IOException("Readahead must be <= 1");
		}
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
		  L.push(f);
		  L.call(0, 1);
		  if (L.isNil(L.value(-1)))
		  {
			return -1;
		  }
		  else if (L.isString(L.value(-1)))
		  {
			s = L.toString(L.value(-1));
			if (s.Length == 0)
			{
			  return -1;
			}
			if (mark_Renamed == i)
			{
			  mark_Renamed = 0;
			}
			else
			{
			  mark_Renamed = -1;
			}
			i = 0;
		  }
		  else
		  {
			L.error("reader function must return a string");
		  }
		}
		return s[i++];
	  }

      override public int read(char[] cbuf, int off, int len)
	  {
		int j = 0; // loop index required after loop
		for (j = 0; j < len; ++j)
		{
		  int c = read();
		  if (c == -1)
		  {
			if (j == 0)
			{
			  return -1;
			}
			else
			{
			  return j;
			}
		  }
		  cbuf[off + j] = (char)c;
		}
		return j;
	  }

	  override public void reset()
	  {
		if (mark_Renamed < 0)
		{
		  throw new IOException("reset() not supported now");
		}
		i = mark_Renamed;
	  }
	}

}