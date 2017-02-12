using System;

/*  $Header: //info.ravenbrook.com/project/jili/version/1.1/code/mnj/lua/Proto.java#1 $
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
	/// Models a function prototype.  This class is internal to Jill and
	/// should not be used by clients.  This is the analogue of the PUC-Rio
	/// type <code>Proto</code>, hence the name.
	/// A function prototype represents the constant part of a function, that
	/// is, a function without closures (upvalues) and without an
	/// environment.  It's a handle for a block of VM instructions and
	/// ancillary constants.
	/// 
	/// For convenience some private arrays are exposed.  Modifying these
	/// arrays is punishable by death. (Java has no convenient constant
	/// array datatype)
	/// </summary>
	internal sealed class Proto
	{
	  /// <summary>
	  /// Interned 0-element array. </summary>
	  private static readonly int[] ZERO_INT_ARRAY = new int[0];
	  private static readonly LocVar[] ZERO_LOCVAR_ARRAY = new LocVar[0];
	  private static readonly Slot[] ZERO_CONSTANT_ARRAY = new Slot[0];
	  private static readonly Proto[] ZERO_PROTO_ARRAY = new Proto[0];
	  private static readonly string[] ZERO_STRING_ARRAY = new string[0];

	  // Generally the fields are named following the PUC-Rio implementation
	  // and so are unusually terse.
	  /// <summary>
	  /// Array of constants. </summary>
	  internal Slot[] k;
	  internal int sizek;
	  /// <summary>
	  /// Array of VM instructions. </summary>
	  internal int[] code_Renamed;
	  internal int sizecode;
	  /// <summary>
	  /// Array of Proto objects. </summary>
	  internal Proto[] p;
	  internal int sizep;
	  /// <summary>
	  /// Number of upvalues used by this prototype (and so by all the
	  /// functions created from this Proto).
	  /// </summary>
	  internal int nups_Renamed;
	  /// <summary>
	  /// Number of formal parameters used by this prototype, and so the
	  /// number of argument received by a function created from this Proto.
	  /// In a function defined to be variadic then this is the number of
	  /// fixed parameters, the number appearing before '...' in the parameter
	  /// list.
	  /// </summary>
	  internal int numparams_Renamed;
	  /// <summary>
	  /// <code>true</code> if and only if the function is variadic, that is,
	  /// defined with '...' in its parameter list.
	  /// </summary>
	  internal bool isVararg;
	  internal int maxstacksize_Renamed;
	  // Debug info
	  /// <summary>
	  /// Map from PC to line number. </summary>
	  internal int[] lineinfo;
	  internal int sizelineinfo;
	  internal LocVar[] locvars_Renamed;
	  internal int sizelocvars;
	  internal string[] upvalues;
	  internal int sizeupvalues;
	  internal string source_Renamed;
	  internal int linedefined_Renamed;
	  internal int lastlinedefined_Renamed;

	  /// <summary>
	  /// Proto synthesized by <seealso cref="Loader"/>.
	  /// All the arrays that are passed to the constructor are
	  /// referenced by the instance.  Avoid unintentional sharing.  All
	  /// arrays must be non-null and all int parameters must not be
	  /// negative.  Generally, this constructor is used by <seealso cref="Loader"/>
	  /// since that has all the relevant arrays already constructed (as
	  /// opposed to the compiler). </summary>
	  /// <param name="constant">   array of constants. </param>
	  /// <param name="code">       array of VM instructions. </param>
	  /// <param name="nups">       number of upvalues (used by this function). </param>
	  /// <param name="numparams">  number of fixed formal parameters. </param>
	  /// <param name="isVararg">   whether '...' is used. </param>
	  /// <param name="maxstacksize">  number of stack slots required when invoking. </param>
	  /// <exception cref="NullPointerException"> if any array arguments are null. </exception>
	  /// <exception cref="IllegalArgumentException"> if nups or numparams is negative. </exception>
	  internal Proto(Slot[] constant, int[] code, Proto[] proto, int nups, int numparams, bool isVararg, int maxstacksize)
	  {
		if (null == constant || null == code || null == proto)
		{
		  throw new System.NullReferenceException();
		}
		if (nups < 0 || numparams < 0 || maxstacksize < 0)
		{
		  throw new System.ArgumentException();
		}
		this.k = constant;
		sizek = k.Length;
		this.code_Renamed = code;
		sizecode = code.Length;
		this.p = proto;
		this.sizep = proto.Length;
		this.nups_Renamed = nups;
		this.numparams_Renamed = numparams;
		this.isVararg = isVararg;
		this.maxstacksize_Renamed = maxstacksize;
	  }

	  /// <summary>
	  /// Blank Proto in preparation for compilation.
	  /// </summary>
	  internal Proto(string source, int maxstacksize)
	  {
		this.maxstacksize_Renamed = maxstacksize;
		  //    maxstacksize = 2;   // register 0/1 are always valid.
		// :todo: Consider removing size* members
		this.source_Renamed = source;
		this.k = ZERO_CONSTANT_ARRAY;
		this.sizek = 0;
		this.code_Renamed = ZERO_INT_ARRAY;
		this.sizecode = 0;
		this.p = ZERO_PROTO_ARRAY;
		this.sizep = 0;
		this.lineinfo = ZERO_INT_ARRAY;
		this.sizelineinfo = 0;
		this.locvars_Renamed = ZERO_LOCVAR_ARRAY;
		this.sizelocvars = 0;
		this.upvalues = ZERO_STRING_ARRAY;
		this.sizeupvalues = 0;
	  }

	  /// <summary>
	  /// Augment with debug info.  All the arguments are referenced by the
	  /// instance after the method has returned, so try not to share them.
	  /// </summary>
	  internal void debug(int[] lineinfoArg, LocVar[] locvarsArg, string[] upvaluesArg)
	  {
		this.lineinfo = lineinfoArg;
		sizelineinfo = lineinfo.Length;
		this.locvars_Renamed = locvarsArg;
		sizelocvars = locvars_Renamed.Length;
		this.upvalues = upvaluesArg;
		sizeupvalues = upvalues.Length;
	  }

	  /// <summary>
	  /// Gets source. </summary>
	  internal string source()
	  {
		return source_Renamed;
	  }

	  /// <summary>
	  /// Setter for source. </summary>
	  internal string Source
	  {
		  set
		  {
			this.source_Renamed = value;
		  }
	  }

	  internal int linedefined()
	  {
		return linedefined_Renamed;
	  }
	  internal int Linedefined
	  {
		  set
		  {
			this.linedefined_Renamed = value;
		  }
	  }

	  internal int lastlinedefined()
	  {
		return lastlinedefined_Renamed;
	  }
	  internal int Lastlinedefined
	  {
		  set
		  {
			this.lastlinedefined_Renamed = value;
		  }
	  }

	  /// <summary>
	  /// Gets Number of Upvalues </summary>
	  internal int nups()
	  {
		return nups_Renamed;
	  }

	  /// <summary>
	  /// Number of Parameters. </summary>
	  internal int numparams()
	  {
		return numparams_Renamed;
	  }

	  /// <summary>
	  /// Maximum Stack Size. </summary>
	  internal int maxstacksize()
	  {
		return maxstacksize_Renamed;
	  }

	  /// <summary>
	  /// Setter for maximum stack size. </summary>
	  internal int Maxstacksize
	  {
		  set
		  {
			maxstacksize_Renamed = value;
		  }
	  }

	  /// <summary>
	  /// Instruction block (do not modify). </summary>
	  internal int[] code()
	  {
		return code_Renamed;
	  }

	  /// <summary>
	  /// Append instruction. </summary>
	  internal void codeAppend(Lua L, int pc, int instruction, int line)
	  {
		  if (Lua.D)
		  {
			  Console.Error.WriteLine("pc:" + pc + ", instruction:" + instruction + ", line:" + line + ", lineinfo.length:" + lineinfo.Length);
		  }
		ensureCode(L, pc);
		code_Renamed[pc] = instruction;

		if (pc >= lineinfo.Length)
		{
		  int[] newLineinfo = new int[lineinfo.Length * 2 + 1];
		  Array.Copy(lineinfo, 0, newLineinfo, 0, lineinfo.Length);
		  lineinfo = newLineinfo;
		}
		lineinfo[pc] = line;
	  }

	  internal void ensureLocvars(Lua L, int atleast, int limit)
	  {
		if (atleast + 1 > sizelocvars)
		{
		  int newsize = atleast * 2 + 1;
		  if (newsize > limit)
		  {
			newsize = limit;
		  }
		  if (atleast + 1 > newsize)
		  {
			L.gRunerror("too many local variables");
		  }
		  LocVar[] newlocvars = new LocVar [newsize];
		  Array.Copy(locvars_Renamed, 0, newlocvars, 0, sizelocvars);
		  for (int i = sizelocvars ; i < newsize ; i++)
		  {
			newlocvars[i] = new LocVar();
		  }
		  locvars_Renamed = newlocvars;
		  sizelocvars = newsize;
		}
	  }

	  internal void ensureProtos(Lua L, int atleast)
	  {
		if (atleast + 1 > sizep)
		{
		  int newsize = atleast * 2 + 1;
		  if (newsize > Lua.MAXARG_Bx)
		  {
			newsize = Lua.MAXARG_Bx;
		  }
		  if (atleast + 1 > newsize)
		  {
			L.gRunerror("constant table overflow");
		  }
		  Proto[] newprotos = new Proto [newsize];
		  Array.Copy(p, 0, newprotos, 0, sizep);
		  p = newprotos;
		  sizep = newsize;
		}
	  }

	  internal void ensureUpvals(Lua L, int atleast)
	  {
		if (atleast + 1 > sizeupvalues)
		{
		  int newsize = atleast * 2 + 1;
		  if (atleast + 1 > newsize)
		  {
			L.gRunerror("upvalues overflow");
		  }
		  string[] newupvalues = new string [newsize];
		  Array.Copy(upvalues, 0, newupvalues, 0, sizeupvalues);
		  upvalues = newupvalues;
		  sizeupvalues = newsize;
		}
	  }

	  internal void ensureCode(Lua L, int atleast)
	  {
		if (atleast + 1 > sizecode)
		{
		  int newsize = atleast * 2 + 1;
		  if (atleast + 1 > newsize)
		  {
			L.gRunerror("code overflow");
		  }
		  int[] newcode = new int [newsize];
		  Array.Copy(code_Renamed, 0, newcode, 0, sizecode);
		  code_Renamed = newcode;
		  sizecode = newsize;
		}
	  }

	  /// <summary>
	  /// Set lineinfo record. </summary>
	  internal void setLineinfo(int pc, int line)
	  {
		lineinfo[pc] = line;
	  }

	  /// <summary>
	  /// Get linenumber corresponding to pc, or 0 if no info. </summary>
	  internal int getline(int pc)
	  {
		if (lineinfo.Length == 0)
		{
		  return 0;
		}
		return lineinfo[pc];
	  }

	  /// <summary>
	  /// Array of inner protos (do not modify). </summary>
	  internal Proto[] proto()
	  {
		return p;
	  }

	  /// <summary>
	  /// Constant array (do not modify). </summary>
	  internal Slot[] constant()
	  {
		return k;
	  }

	  /// <summary>
	  /// Append constant. </summary>
	  internal void constantAppend(int idx, object o)
	  {
		if (idx >= k.Length)
		{
		  Slot[] newK = new Slot[k.Length * 2 + 1];
		  Array.Copy(k, 0, newK, 0, k.Length);
		  k = newK;
		}
		k[idx] = new Slot(o);
	  }

	  /// <summary>
	  /// Predicate for whether function uses ... in its parameter list. </summary>
	  internal bool Vararg
	  {
		  get
		  {
			return isVararg;
		  }
	  }

	  /// <summary>
	  /// "Setter" for isVararg.  Sets it to true. </summary>
	  internal void setIsVararg()
	  {
		isVararg = true;
	  }

	  /// <summary>
	  /// LocVar array (do not modify). </summary>
	  internal LocVar[] locvars()
	  {
		return locvars_Renamed;
	  }

	  // All the trim functions, below, check for the redundant case of
	  // trimming to the length that they already are.  Because they are
	  // initially allocated as interned zero-length arrays this also means
	  // that no unnecesary zero-length array objects are allocated.

	  /// <summary>
	  /// Trim an int array to specified size. </summary>
	  /// <returns> the trimmed array. </returns>
	  private int[] trimInt(int[] old, int n)
	  {
		if (n == old.Length)
		{
		  return old;
		}
		int[] newArray = new int[n];
		Array.Copy(old, 0, newArray, 0, n);
		return newArray;
	  }

	  /// <summary>
	  /// Trim code array to specified size. </summary>
	  internal void closeCode(int n)
	  {
		code_Renamed = trimInt(code_Renamed, n);
		sizecode = code_Renamed.Length;
	  }

	  /// <summary>
	  /// Trim lineinfo array to specified size. </summary>
	  internal void closeLineinfo(int n)
	  {
		lineinfo = trimInt(lineinfo, n);
		sizelineinfo = n;
	  }

	  /// <summary>
	  /// Trim k (constant) array to specified size. </summary>
	  internal void closeK(int n)
	  {
		if (k.Length > n)
		{
		  Slot[] newArray = new Slot[n];
		  Array.Copy(k, 0, newArray, 0, n);
		  k = newArray;
		}
		sizek = n;
		return;
	  }

	  /// <summary>
	  /// Trim p (proto) array to specified size. </summary>
	  internal void closeP(int n)
	  {
		if (n == p.Length)
		{
		  return;
		}
		Proto[] newArray = new Proto[n];
		Array.Copy(p, 0, newArray, 0, n);
		p = newArray;
		sizep = n;
	  }

	  /// <summary>
	  /// Trim locvar array to specified size. </summary>
	  internal void closeLocvars(int n)
	  {
		if (n == locvars_Renamed.Length)
		{
		  return;
		}
		LocVar[] newArray = new LocVar[n];
		Array.Copy(locvars_Renamed, 0, newArray, 0, n);
		locvars_Renamed = newArray;
		sizelocvars = n;
	  }

	  /// <summary>
	  /// Trim upvalues array to size <var>nups</var>. </summary>
	  internal void closeUpvalues()
	  {
		if (nups_Renamed == upvalues.Length)
		{
		  return;
		}
		string[] newArray = new string[nups_Renamed];
		Array.Copy(upvalues, 0, newArray, 0, nups_Renamed);
		upvalues = newArray;
		sizeupvalues = nups_Renamed;
	  }

	}

}