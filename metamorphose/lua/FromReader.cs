using System;
using metamorphose.java;

/*  $Header: //info.ravenbrook.com/project/jili/version/1.1/code/mnj/lua/FromReader.java#1 $
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
	/// Takes a <seealso cref="Reader"/> and converts to an <seealso cref="InputStream"/> by
	/// reversing the transformation performed by <code>string.dump</code>.
	/// Similar to <seealso cref="DumpedInput"/> which does the same job for {@link
	/// String}.  This class is used by <seealso cref="BaseLib"/>'s load in order to
	/// load binary chunks.
	/// </summary>
	public class FromReader : InputStream
	{
	  // :todo: consider combining with DumpedInput.  No real reason except
	  // to save space in JME.

	  private Reader reader;

	  public FromReader(Reader reader)
	  {
		this.reader = reader;
	  }

	  override public void mark(int readahead)
	  {
		try
		{
		  reader.mark(readahead);
		}
		catch (Exception)
		{
		}
	  }

      override public void reset()
	  {
		reader.reset();
	  }

      override public int read()
	  {
		int c = reader.read();
		if (c == -1)
		{
		  return c;
		}
		return c & 0xff;
	  }
	}

}