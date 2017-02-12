using metamorphose.java;

/*  $Header: //info.ravenbrook.com/project/jili/version/1.1/code/mnj/lua/LuaInternal.java#1 $
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
	/// Class used to implement internal callbacks.  Currently there is only
	/// one callback used, one that parses or loads a Lua chunk into binary
	/// form.
	/// </summary>
	internal sealed class LuaInternal : LuaJavaCallback
	{
	  private InputStream stream;
	  private Reader reader;
	  private string chunkname;

	  internal LuaInternal(InputStream @in, string chunkname)
	  {
		this.stream = @in;
		this.chunkname = chunkname;
	  }

	  internal LuaInternal(Reader @in, string chunkname)
	  {
		this.reader = @in;
		this.chunkname = chunkname;
	  }

	  public override int luaFunction(Lua L)
	  {
		try
		{
		  Proto p = null;

		  // In either the stream or the reader case there is a way of
		  // converting the input to the other type.
		  if (stream != null)
		  {
			stream.mark(1);
			int c = stream.read();
			stream.reset();

			// Convert to Reader if looks like source code instead of
			// binary.
			if (c == Loader.HEADER[0])
			{
			  Loader l = new Loader(stream, chunkname);
			  p = l.undump();
			}
			else
			{
			  reader = new InputStreamReader(stream, "UTF-8");
			  p = Syntax.parser(L, reader, chunkname);
			}
		  }
		  else
		  {
			// Convert to Stream if looks like binary (dumped via
			// string.dump) instead of source code.
			if (reader.markSupported())
			{
			  reader.mark(1);
			  int c = reader.read();
			  reader.reset();

			  if (c == Loader.HEADER[0])
			  {
				stream = new FromReader(reader);
				Loader l = new Loader(stream, chunkname);
				p = l.undump();
			  }
			  else
			  {
				p = Syntax.parser(L, reader, chunkname);
			  }
			}
			else
			{
			  p = Syntax.parser(L, reader, chunkname);
			}
		  }

		  L.push(new LuaFunction(p, new UpVal[0], L.Globals));
		  return 1;
		}
		catch (IOException e)
		{
		  L.push("cannot read " + chunkname + ": " + e.ToString());
		  L.dThrow(Lua.ERRFILE);
		  return 0;
		}
	  }
	}

}