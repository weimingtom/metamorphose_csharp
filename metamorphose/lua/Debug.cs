/*  $Header: //info.ravenbrook.com/project/jili/version/1.1/code/mnj/lua/Debug.java#1 $
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
	/// Equivalent to struct lua_Debug.  This implementation is incomplete
	/// because it is not intended to form part of the public API.  It has
	/// only been implemented to the extent necessary for internal use.
	/// </summary>
	public sealed class Debug
	{
	  // private, no public accessors defined.
	  private readonly int ici_Renamed;

	  // public accessors may be defined for these.
	  private int @event;
	  private string what_Renamed;
	  private string source;
	  private int currentline_Renamed;
	  private int linedefined_Renamed;
	  private int lastlinedefined;
	  private string shortsrc_Renamed;

	  /// <param name="ici">  index of CallInfo record in L.civ </param>
	  internal Debug(int ici)
	  {
		this.ici_Renamed = ici;
	  }

	  /// <summary>
	  /// Get ici, index of the <seealso cref="CallInfo"/> record.
	  /// </summary>
	  internal int ici()
	  {
		return ici_Renamed;
	  }

	  /// <summary>
	  /// Setter for event.
	  /// </summary>
	  internal int Event
	  {
		  set
		  {
			this.@event = value;
		  }
	  }

	  internal string what()
	  {
		  return this.what_Renamed;
	  }

	  /// <summary>
	  /// Sets the what field.
	  /// </summary>
	  internal string What
	  {
		  set
		  {
			this.what_Renamed = value;
		  }
	  }

	  /// <summary>
	  /// Sets the source, and the shortsrc.
	  /// </summary>
	  internal string Source
	  {
		  set
		  {
			this.source = value;
			this.shortsrc_Renamed = Lua.oChunkid(value);
		  }
	  }

	  /// <summary>
	  /// Gets the current line.  May become public.
	  /// </summary>
	  internal int currentline()
	  {
		return currentline_Renamed;
	  }

	  /// <summary>
	  /// Set currentline.
	  /// </summary>
	  internal int Currentline
	  {
		  set
		  {
			this.currentline_Renamed = value;
		  }
	  }

	  /// <summary>
	  /// Get linedefined.
	  /// </summary>
	  internal int linedefined()
	  {
		return linedefined_Renamed;
	  }

	  /// <summary>
	  /// Set linedefined.
	  /// </summary>
	  internal int Linedefined
	  {
		  set
		  {
			this.linedefined_Renamed = value;
		  }
	  }

	  /// <summary>
	  /// Set lastlinedefined.
	  /// </summary>
	  internal int Lastlinedefined
	  {
		  set
		  {
			this.lastlinedefined = value;
		  }
	  }

	  /// <summary>
	  /// Gets the "printable" version of source, for error messages.
	  /// May become public.
	  /// </summary>
	  internal string shortsrc()
	  {
		return shortsrc_Renamed;
	  }
	}

}