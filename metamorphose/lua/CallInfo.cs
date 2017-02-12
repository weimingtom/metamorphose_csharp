/*  $Header: //info.ravenbrook.com/project/jili/version/1.1/code/mnj/lua/CallInfo.java#1 $
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

	internal sealed class CallInfo
	{
	  private int savedpc_Renamed;
	  private int func;
	  private int base_Renamed;
	  private int top_Renamed;
	  private int nresults_Renamed;
	  private int tailcalls_Renamed;

	  /// <summary>
	  /// Only used to create the first instance. </summary>
	  internal CallInfo()
	  {
	  }

	  /// <param name="func">  stack index of function </param>
	  /// <param name="base">  stack base for this frame </param>
	  /// <param name="top">   top-of-stack for this frame </param>
	  /// <param name="nresults">  number of results expected by caller </param>
	  internal CallInfo(int func, int @base, int top, int nresults)
	  {
		this.func = func;
		this.base_Renamed = @base;
		this.top_Renamed = top;
		this.nresults_Renamed = nresults;
	  }

	  /// <summary>
	  /// Setter for savedpc. </summary>
	  internal int Savedpc
	  {
		  set
		  {
			savedpc_Renamed = value;
		  }
	  }
	  /// <summary>
	  /// Getter for savedpc. </summary>
	  internal int savedpc()
	  {
		return savedpc_Renamed;
	  }

	  /// <summary>
	  /// Get the stack index for the function object for this record.
	  /// </summary>
	  internal int function()
	  {
		return func;
	  }

	  /// <summary>
	  /// Get stack index where results should end up.  This is an absolute
	  /// stack index, not relative to L.base.
	  /// </summary>
	  internal int res()
	  {
		// Same location as function.
		return func;
	  }

	  /// <summary>
	  /// Get stack base for this record.
	  /// </summary>
	  internal int @base()
	  {
		return base_Renamed;
	  }

	  /// <summary>
	  /// Get top-of-stack for this record.  This is the number of elements
	  /// in the stack (or will be when the function is resumed).
	  /// </summary>
	  internal int top()
	  {
		return top_Renamed;
	  }

	  /// <summary>
	  /// Setter for top.
	  /// </summary>
	  internal int Top
	  {
		  set
		  {
			this.top_Renamed = value;
		  }
	  }

	  /// <summary>
	  /// Get number of results expected by the caller of this function.
	  /// Used to adjust the returned results to the correct number.
	  /// </summary>
	  internal int nresults()
	  {
		return nresults_Renamed;
	  }

	  /// <summary>
	  /// Get number of tailcalls
	  /// </summary>
	  internal int tailcalls()
	  {
		return tailcalls_Renamed;
	  }

	  /// <summary>
	  /// Used during tailcall to set the base and top members.
	  /// </summary>
	  internal void tailcall(int baseArg, int topArg)
	  {
		this.base_Renamed = baseArg;
		this.top_Renamed = topArg;
		++tailcalls_Renamed;
	  }
	}

}