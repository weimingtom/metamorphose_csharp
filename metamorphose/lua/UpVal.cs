/*  $Header: //info.ravenbrook.com/project/jili/version/1.1/code/mnj/lua/UpVal.java#1 $
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
	/// Models an upvalue.  This class is internal to Jill and should not be
	/// used by clients.
	/// This is the analogue of the UpVal type in PUC-Rio's Lua
	/// implementation, hence the name.
	/// An UpVal instance is a reference to a variable.
	/// When initially created generally the variable is kept on the VM
	/// stack.  When the function that defines that variable returns, the
	/// corresponding stack slots are destroyed.  In order that the UpVal
	/// continues to reference the variable, it is closed (using the
	/// <code>close</code> method).  Lua functions that reference, via an
	/// upvalue, the same instance of the same variable, will share an
	/// <code>UpVal</code> (somewhere in their <code>upval</code> array
	/// member); hence they share updates to the variable.
	/// </summary>
	internal sealed class UpVal
	{
	  /// <summary>
	  /// The offset field.  Stored here, but not actually used directly by
	  /// this class.
	  /// Used (by <seealso cref="Lua"/>) when searching for <seealso cref="UpVal"/> instances.
	  /// An open UpVal has a valid offset field.  Its slot is shared
	  /// with a slot of the VM stack.
	  /// A closed UpVal has offset == -1.  It's slot will be a fresh copy
	  /// and not shared with any other.
	  /// </summary>
	  private int offset_Renamed;
	  /// <summary>
	  /// The slot object used to store the Lua value.
	  /// </summary>
	  private Slot s;

	  /// <summary>
	  /// A fresh upvalue from an offset, and a slot.
	  /// Conceptually <var>offset</var> and <var>slot</var> convey the same
	  /// information, only one is necessary since the offset implies the
	  /// slot and vice-versa.  <var>slot</var> is used to directly reference
	  /// the value (this avoids an indirection to the VM stack). <var>offset</var>
	  /// is used when searching for UpVals in the openupval list; this
	  /// happens when closing UpVals (function return) or creating them
	  /// (execution of functon declaration). </summary>
	  /// <param name="offset">  index into Lua thread's VM stack, must be a valid index. </param>
	  /// <param name="s">  Slot corresponding to offset. </param>
	  /// <exception cref="NullPointerException"> if L is null. </exception>
	  internal UpVal(int offset, Slot s)
	  {
		this.offset_Renamed = offset;
		this.s = s;
	  }

	  /// <summary>
	  /// Getter for underlying value.
	  /// </summary>
	  internal object Value
	  {
		  get
		  {
			return s.asObject();
		  }
		  set
		  {
			s.Object = value;
		  }
	  }


	  /// <summary>
	  /// The offset.
	  /// </summary>
	  internal int offset()
	  {
		return offset_Renamed;
	  }

	  /// <summary>
	  /// Closes an UpVal.  This ensures that the storage operated on by
	  /// <seealso cref="#getValue() getValue"/> and <seealso cref="#setValue(Object) setValue"/>
	  /// is not shared by any other object.
	  /// This is typically used when a function returns (executes
	  /// the <code>OP_RET</code> VM instruction).  Effectively this
	  /// transfers a variable binding from the stack to the heap.
	  /// </summary>
	  internal void close()
	  {
		s = new Slot(s);
		offset_Renamed = -1;
	  }
	}

}