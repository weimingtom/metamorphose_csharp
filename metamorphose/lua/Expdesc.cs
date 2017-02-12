/*  $Header: //info.ravenbrook.com/project/jili/version/1.1/code/mnj/lua/Expdesc.java#1 $
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
	/// Equivalent to struct expdesc. </summary>
	internal sealed class Expdesc
	{

	  internal const int VVOID = 0; // no value
	  internal const int VNIL = 1;
	  internal const int VTRUE = 2;
	  internal const int VFALSE = 3;
	  internal const int VK = 4; // info = index into 'k'
	  internal const int VKNUM = 5; // nval = numerical value
	  internal const int VLOCAL = 6; // info = local register
	  internal const int VUPVAL = 7; // info = index into 'upvalues'
	  internal const int VGLOBAL = 8; // info = index of table;
											// aux = index of global name in 'k'
	  internal const int VINDEXED = 9; // info = table register
											// aux = index register (or 'k')
	  internal const int VJMP = 10; // info = instruction pc
	  internal const int VRELOCABLE = 11; // info = instruction pc
	  internal const int VNONRELOC = 12; // info = result register
	  internal const int VCALL = 13; // info = instruction pc
	  internal const int VVARARG = 14; // info = instruction pc

	  internal int k; // one of V* enums above
	  internal int info_Renamed;
	  internal int aux_Renamed;
	  internal double nval_Renamed;
	  internal int t;
	  internal int f;

	  internal Expdesc()
	  {
	  }

	  internal Expdesc(int k, int i)
	  {
		init(k, i);
	  }

	  /// <summary>
	  /// Equivalent to init_exp from lparser.c </summary>
	  internal void init(int kind, int i)
	  {
		this.t = FuncState.NO_JUMP;
		this.f = FuncState.NO_JUMP;
		this.k = kind;
		this.info_Renamed = i;
	  }

	  internal void init(Expdesc e)
	  {
		// Must initialise all members of this.
		this.k = e.k;
		this.info_Renamed = e.info_Renamed;
		this.aux_Renamed = e.aux_Renamed;
		this.nval_Renamed = e.nval_Renamed;
		this.t = e.t;
		this.f = e.f;
	  }

	  internal int kind()
	  {
		return k;
	  }

	  internal int Kind
	  {
		  set
		  {
			this.k = value;
		  }
	  }

	  internal int info()
	  {
		return info_Renamed;
	  }

	  internal int Info
	  {
		  set
		  {
			this.info_Renamed = value;
		  }
	  }

	  internal int aux()
	  {
		return aux_Renamed;
	  }

	  internal double nval()
	  {
		return nval_Renamed;
	  }

	  internal double Nval
	  {
		  set
		  {
			this.nval_Renamed = value;
		  }
	  }

	  /// <summary>
	  /// Equivalent to hasmultret from lparser.c </summary>
	  internal bool hasmultret()
	  {
		return k == VCALL || k == VVARARG;
	  }

	  /// <summary>
	  /// Equivalent to hasjumps from lcode.c. </summary>
	  internal bool hasjumps()
	  {
		return t != f;
	  }

	  internal void nonreloc(int i)
	  {
		k = VNONRELOC;
		info_Renamed = i;
	  }

	  internal void reloc(int i)
	  {
		k = VRELOCABLE;
		info_Renamed = i;
	  }

	  internal void upval(int i)
	  {
		k = VUPVAL;
		info_Renamed = i;
	  }
	}

}