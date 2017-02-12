/*  $Header: //info.ravenbrook.com/project/jili/version/1.1/code/mnj/lua/LuaFunction.java#1 $
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
	/// Models a Lua function.
	/// Note that whilst the class is public, its constructors are not.
	/// Functions are created by loading Lua chunks (in source or binary
	/// form) or executing Lua code which defines functions (and, for
	/// example, places them in the global table).  {@link
	/// Lua#load(InputStream, String) Lua.load} is used
	/// to load a Lua chunk (it returns a <code>LuaFunction</code>),
	/// and <seealso cref="Lua#call Lua.call"/> is used to call a function.
	/// </summary>
	public sealed class LuaFunction
	{
	  private UpVal[] upval;
	  private LuaTable env;
	  private Proto p;

	  /// <summary>
	  /// Constructs an instance from a triple of {Proto, upvalues,
	  /// environment}.  Deliberately not public, See {@link
	  /// Lua#load(InputStream, String) Lua.load} for
	  /// public construction.  All arguments are referenced from the
	  /// instance.  The <code>upval</code> array must have exactly the same
	  /// number of elements as the number of upvalues in <code>proto</code>
	  /// (the value of the <code>nups</code> parameter in the
	  /// <code>Proto</code> constructor).
	  /// </summary>
	  /// <param name="proto">  A Proto object. </param>
	  /// <param name="upval">  Array of upvalues. </param>
	  /// <param name="env">    The function's environment. </param>
	  /// <exception cref="NullPointerException"> if any arguments are null. </exception>
	  /// <exception cref="IllegalArgumentsException"> if upval.length is wrong. </exception>
	  internal LuaFunction(Proto proto, UpVal[] upval, LuaTable env)
	  {
		if (null == proto || null == upval || null == env)
		{
		  throw new System.NullReferenceException();
		}
		if (upval.Length != proto.nups())
		{
		  throw new System.ArgumentException();
		}

		this.p = proto;
		this.upval = upval;
		this.env = env;
	  }

	  /// <summary>
	  /// Get nth UpVal. </summary>
	  internal UpVal upVal(int n)
	  {
		return upval[n];
	  }

	  /// <summary>
	  /// Get the Proto object. </summary>
	  internal Proto proto()
	  {
		return p;
	  }

	  /// <summary>
	  /// Getter for environment. </summary>
	  internal LuaTable getEnv()
	  {
		return env;
	  }
	  /// <summary>
	  /// Setter for environment. </summary>
	  internal void setEnv(LuaTable env)
	  {
		if (null == env)
		{
		  throw new System.NullReferenceException();
		}

		this.env = env;
	  }


	}

}