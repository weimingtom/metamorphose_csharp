using System;
using System.Collections;

/*  $Header: //info.ravenbrook.com/project/jili/version/1.1/code/mnj/lua/FuncState.java#1 $
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
	/// Used to model a function during compilation.  Code generation uses
	/// this structure extensively.  Most of the PUC-Rio functions from
	/// lcode.c have moved into this class, alongwith a few functions from
	/// lparser.c
	/// </summary>
	internal sealed class FuncState
	{
	  /// <summary>
	  /// See NO_JUMP in lcode.h. </summary>
	  internal const int NO_JUMP = -1;

	  /// <summary>
	  /// Proto object for this function. </summary>
	  internal Proto f;

	  /// <summary>
	  /// Table to find (and reuse) elements in <var>f.k</var>.  Maps from
	  /// Object (a constant Lua value) to an index into <var>f.k</var>.
	  /// </summary>
	  internal Hashtable h = new Hashtable();

	  /// <summary>
	  /// Enclosing function. </summary>
	  internal FuncState prev;

	  /// <summary>
	  /// Lexical state. </summary>
	  internal Syntax ls;

	  /// <summary>
	  /// Lua state. </summary>
	  internal Lua L;

	  /// <summary>
	  /// chain of current blocks </summary>
	  internal BlockCnt bl; // = null;

	  /// <summary>
	  /// next position to code. </summary>
	  internal int pc; // = 0;

	  /// <summary>
	  /// pc of last jump target. </summary>
	  internal int lasttarget = -1;

	  /// <summary>
	  /// List of pending jumps to <var>pc</var>. </summary>
	  internal int jpc = NO_JUMP;

	  /// <summary>
	  /// First free register. </summary>
	  internal int freereg_Renamed; // = 0;

	  /// <summary>
	  /// number of elements in <var>k</var>. </summary>
	  internal int nk; // = 0;

	  /// <summary>
	  /// number of elements in <var>p</var>. </summary>
	  internal int np; // = 0;

	  /// <summary>
	  /// number of elements in <var>locvars</var>. </summary>
	  internal short nlocvars; // = 0;

	  /// <summary>
	  /// number of active local variables. </summary>
	  internal short nactvar; // = 0;

	  /// <summary>
	  /// upvalues as 8-bit k and 8-bit info </summary>
	  internal int[] upvalues = new int [Lua.MAXUPVALUES];

	  /// <summary>
	  /// declared-variable stack. </summary>
	  internal short[] actvar = new short[Lua.MAXVARS];

	  /// <summary>
	  /// Constructor.  Much of this is taken from <code>open_func</code> in
	  /// <code>lparser.c</code>.
	  /// </summary>
	  internal FuncState(Syntax ls)
	  {
		f = new Proto(ls.source_Renamed, 2); // default value for maxstacksize=2
		L = ls.L;
		this.ls = ls;
		//    prev = ls.linkfs(this);
	  }

	  /// <summary>
	  /// Equivalent to <code>close_func</code> from <code>lparser.c</code>. </summary>
	  internal void close()
	  {
		f.closeCode(pc);
		f.closeLineinfo(pc);
		f.closeK(nk);
		f.closeP(np);
		f.closeLocvars(nlocvars);
		f.closeUpvalues();
		bool checks = L.gCheckcode(f);
		//# assert checks
		//# assert bl == null
	  }

	  /// <summary>
	  /// Equivalent to getlocvar from lparser.c.
	  /// Accesses <code>LocVar</code>s of the <seealso cref="Proto"/>.
	  /// </summary>
	  internal LocVar getlocvar(int idx)
	  {
		return f.locvars_Renamed[actvar[idx]];
	  }


	  // Functions from lcode.c

	  /// <summary>
	  /// Equivalent to luaK_checkstack. </summary>
	  internal void kCheckstack(int n)
	  {
		int newstack = freereg_Renamed + n;
		if (newstack > f.maxstacksize())
		{
		  if (newstack >= Lua.MAXSTACK)
		  {
			ls.xSyntaxerror("function or expression too complex");
		  }
		  f.Maxstacksize = newstack;
		}
	  }

	  /// <summary>
	  /// Equivalent to luaK_code. </summary>
	  internal int kCode(int i, int line)
	  {
		dischargejpc();
		// Put new instruction in code array.
		f.codeAppend(L, pc, i, line);
		return pc++;
	  }

	  /// <summary>
	  /// Equivalent to luaK_codeABC. </summary>
	  internal int kCodeABC(int o, int a, int b, int c)
	  {
		// assert getOpMode(o) == iABC;
		// assert getBMode(o) != OP_ARG_N || b == 0;
		// assert getCMode(o) != OP_ARG_N || c == 0;
		return kCode(Lua.CREATE_ABC(o, a, b, c), ls.lastline());
	  }

	  /// <summary>
	  /// Equivalent to luaK_codeABx. </summary>
	  internal int kCodeABx(int o, int a, int bc)
	  {
		// assert getOpMode(o) == iABx || getOpMode(o) == iAsBx);
		// assert getCMode(o) == OP_ARG_N);
		return kCode(Lua.CREATE_ABx(o, a, bc), ls.lastline());
	  }

	  /// <summary>
	  /// Equivalent to luaK_codeAsBx. </summary>
	  internal int kCodeAsBx(int o, int a, int bc)
	  {
		return kCodeABx(o, a, bc + Lua.MAXARG_sBx);
	  }

	  /// <summary>
	  /// Equivalent to luaK_dischargevars. </summary>
	  internal void kDischargevars(Expdesc e)
	  {
		switch (e.kind())
		{
		  case Expdesc.VLOCAL:
			e.Kind = Expdesc.VNONRELOC;
			break;
		  case Expdesc.VUPVAL:
			e.reloc(kCodeABC(Lua.OP_GETUPVAL, 0, e.info_Renamed, 0));
			break;
		  case Expdesc.VGLOBAL:
			e.reloc(kCodeABx(Lua.OP_GETGLOBAL, 0, e.info_Renamed));
			break;
		  case Expdesc.VINDEXED:
			freereg(e.aux());
			freereg(e.info());
			e.reloc(kCodeABC(Lua.OP_GETTABLE, 0, e.info_Renamed, e.aux_Renamed));
			break;
		  case Expdesc.VVARARG:
		  case Expdesc.VCALL:
			kSetoneret(e);
			break;
		  default:
			break; // there is one value available (somewhere)
		}
	  }

	  /// <summary>
	  /// Equivalent to luaK_exp2anyreg. </summary>
	  internal int kExp2anyreg(Expdesc e)
	  {
		kDischargevars(e);
		if (e.k == Expdesc.VNONRELOC)
		{
		  if (!e.hasjumps())
		  {
			return e.info_Renamed;
		  }
		  if (e.info_Renamed >= nactvar) // reg is not a local?
		  {
			exp2reg(e, e.info_Renamed); // put value on it
			return e.info_Renamed;
		  }
		}
		kExp2nextreg(e); // default
		return e.info_Renamed;
	  }

	  /// <summary>
	  /// Equivalent to luaK_exp2nextreg. </summary>
	  internal void kExp2nextreg(Expdesc e)
	  {
		kDischargevars(e);
		freeexp(e);
		kReserveregs(1);
		exp2reg(e, freereg_Renamed - 1);
	  }

	  /// <summary>
	  /// Equivalent to luaK_fixline. </summary>
	  internal void kFixline(int line)
	  {
		f.setLineinfo(pc - 1, line);
	  }

	  /// <summary>
	  /// Equivalent to luaK_infix. </summary>
	  internal void kInfix(int op, Expdesc v)
	  {
	  switch (op)
	  {
		case Syntax.OPR_AND:
		  kGoiftrue(v);
		  break;
		case Syntax.OPR_OR:
		  kGoiffalse(v);
		  break;
		case Syntax.OPR_CONCAT:
		  kExp2nextreg(v); // operand must be on the `stack'
		  break;
		default:
		  if (!isnumeral(v))
		  {
			kExp2RK(v);
		  }
		  break;
	  }
	  }


	  private bool isnumeral(Expdesc e)
	  {
		return e.k == Expdesc.VKNUM && e.t == NO_JUMP && e.f == NO_JUMP;
	  }

	  /// <summary>
	  /// Equivalent to luaK_nil. </summary>
	  internal void kNil(int from, int n)
	  {
		int previous;
		if (pc > lasttarget) // no jumps to current position?
		{
		  if (pc == 0) // function start?
		  {
			return; // positions are already clean
		  }
		  previous = pc - 1;
		  int instr = f.code_Renamed[previous];
		  if (Lua.OPCODE(instr) == Lua.OP_LOADNIL)
		  {
			int pfrom = Lua.ARGA(instr);
			int pto = Lua.ARGB(instr);
			if (pfrom <= from && from <= pto + 1) // can connect both?
			{
			  if (from + n - 1 > pto)
			  {
				f.code_Renamed[previous] = Lua.SETARG_B(instr, from + n - 1);
			  }
			  return;
			}
		  }
		}
		kCodeABC(Lua.OP_LOADNIL, from, from + n - 1, 0);
	  }

	  /// <summary>
	  /// Equivalent to luaK_numberK. </summary>
	  internal int kNumberK(double r)
	  {
		return addk(Lua.valueOfNumber(r));
	  }

	  /// <summary>
	  /// Equivalent to luaK_posfix. </summary>
	  internal void kPosfix(int op, Expdesc e1, Expdesc e2)
	  {
		switch (op)
		{
		  case Syntax.OPR_AND:
			/* list must be closed */
			//# assert e1.t == NO_JUMP
			kDischargevars(e2);
			e2.f = kConcat(e2.f, e1.f);
			e1.init(e2);
			break;

		  case Syntax.OPR_OR:
			/* list must be closed */
			//# assert e1.f == NO_JUMP
			kDischargevars(e2);
			e2.t = kConcat(e2.t, e1.t);
			e1.init(e2);
			break;

		  case Syntax.OPR_CONCAT:
			kExp2val(e2);
			if (e2.k == Expdesc.VRELOCABLE && Lua.OPCODE(getcode(e2)) == Lua.OP_CONCAT)
			{
			  //# assert e1.info == Lua.ARGB(getcode(e2))-1
			  freeexp(e1);
			  setcode(e2, Lua.SETARG_B(getcode(e2), e1.info_Renamed));
			  e1.k = e2.k;
			  e1.info_Renamed = e2.info_Renamed;
			}
			else
			{
			  kExp2nextreg(e2); // operand must be on the 'stack'
			  codearith(Lua.OP_CONCAT, e1, e2);
			}
			break;

		  case Syntax.OPR_ADD:
			  codearith(Lua.OP_ADD, e1, e2);
			  break;
		  case Syntax.OPR_SUB:
			  codearith(Lua.OP_SUB, e1, e2);
			  break;
		  case Syntax.OPR_MUL:
			  codearith(Lua.OP_MUL, e1, e2);
			  break;
		  case Syntax.OPR_DIV:
			  codearith(Lua.OP_DIV, e1, e2);
			  break;
		  case Syntax.OPR_MOD:
			  codearith(Lua.OP_MOD, e1, e2);
			  break;
		  case Syntax.OPR_POW:
			  codearith(Lua.OP_POW, e1, e2);
			  break;
		  case Syntax.OPR_EQ:
			  codecomp(Lua.OP_EQ, true, e1, e2);
			  break;
		  case Syntax.OPR_NE:
			  codecomp(Lua.OP_EQ, false, e1, e2);
			  break;
		  case Syntax.OPR_LT:
			  codecomp(Lua.OP_LT, true, e1, e2);
			  break;
		  case Syntax.OPR_LE:
			  codecomp(Lua.OP_LE, true, e1, e2);
			  break;
		  case Syntax.OPR_GT:
			  codecomp(Lua.OP_LT, false, e1, e2);
			  break;
		  case Syntax.OPR_GE:
			  codecomp(Lua.OP_LE, false, e1, e2);
			  break;
		  default:
			//# assert false
	  break;
		}
	  }

	  /// <summary>
	  /// Equivalent to luaK_prefix. </summary>
	  internal void kPrefix(int op, Expdesc e)
	  {
		Expdesc e2 = new Expdesc(Expdesc.VKNUM, 0);
		switch (op)
		{
		  case Syntax.OPR_MINUS:
			if (e.kind() == Expdesc.VK)
			{
			  kExp2anyreg(e);
			}
			codearith(Lua.OP_UNM, e, e2);
			break;
		  case Syntax.OPR_NOT:
			codenot(e);
			break;
		  case Syntax.OPR_LEN:
			kExp2anyreg(e);
			codearith(Lua.OP_LEN, e, e2);
			break;
		  default:
			throw new System.ArgumentException();
		}
	  }

	  /// <summary>
	  /// Equivalent to luaK_reserveregs. </summary>
	  internal void kReserveregs(int n)
	  {
		kCheckstack(n);
		freereg_Renamed += n;
	  }

	  /// <summary>
	  /// Equivalent to luaK_ret. </summary>
	  internal void kRet(int first, int nret)
	  {
		kCodeABC(Lua.OP_RETURN, first, nret + 1, 0);
	  }

	  /// <summary>
	  /// Equivalent to luaK_setmultret (in lcode.h). </summary>
	  internal void kSetmultret(Expdesc e)
	  {
		kSetreturns(e, Lua.MULTRET);
	  }

	  /// <summary>
	  /// Equivalent to luaK_setoneret. </summary>
	  internal void kSetoneret(Expdesc e)
	  {
		if (e.kind() == Expdesc.VCALL) // expression is an open function call?
		{
		  e.nonreloc(Lua.ARGA(getcode(e)));
		}
		else if (e.kind() == Expdesc.VVARARG)
		{
		  setargb(e, 2);
		  e.Kind = Expdesc.VRELOCABLE;
		}
	  }

	  /// <summary>
	  /// Equivalent to luaK_setreturns. </summary>
	  internal void kSetreturns(Expdesc e, int nresults)
	  {
		if (e.kind() == Expdesc.VCALL) // expression is an open function call?
		{
		  setargc(e, nresults + 1);
		}
		else if (e.kind() == Expdesc.VVARARG)
		{
		  setargb(e, nresults + 1);
		  setarga(e, freereg_Renamed);
		  kReserveregs(1);
		}
	  }

	  /// <summary>
	  /// Equivalent to luaK_stringK. </summary>
	  internal int kStringK(string s)
	  {
		return addk(s/*.intern()*/);
	  }

	  private int addk(object o)
	  {
		object hash = o;
		object v = h[hash];
		if (v != null)
		{
		  // :todo: assert
		  return (int)((int?)v);
		}
		// constant not found; create a new entry
		f.constantAppend(nk, o);
		h[hash] = new int?(nk);
		return nk++;
	  }

	  private void codearith(int op, Expdesc e1, Expdesc e2)
	  {
		if (constfolding(op, e1, e2))
		{
		  return;
		}
		else
		{
		  int o1 = kExp2RK(e1);
		  int o2 = (op != Lua.OP_UNM && op != Lua.OP_LEN) ? kExp2RK(e2) : 0;
		  freeexp(e2);
		  freeexp(e1);
		  e1.info_Renamed = kCodeABC(op, 0, o1, o2);
		  e1.k = Expdesc.VRELOCABLE;
		}
	  }

	  private bool constfolding(int op, Expdesc e1, Expdesc e2)
	  {
		double r;
		if (!isnumeral(e1) || !isnumeral(e2))
		{
		  return false;
		}
		double v1 = e1.nval_Renamed;
		double v2 = e2.nval_Renamed;
		switch (op)
		{
		  case Lua.OP_ADD:
			  r = v1 + v2;
			  break;
		  case Lua.OP_SUB:
			  r = v1 - v2;
			  break;
		  case Lua.OP_MUL:
			  r = v1 * v2;
			  break;
		  case Lua.OP_DIV:
			  if (v2 == 0.0)
			  {
				return false; // do not attempt to divide by 0
			  }
			  r = v1 / v2;
			  break;
		  case Lua.OP_MOD:
			  if (v2 == 0.0)
			  {
				return false; // do not attempt to divide by 0
			  }
			  r = v1 % v2;
			  break;
		  case Lua.OP_POW:
			  r = Lua.iNumpow(v1, v2);
			  break;
		  case Lua.OP_UNM:
			  r = -v1;
			  break;
		  case Lua.OP_LEN: // no constant folding for 'len'
			  return false;
		  default:
			  //# assert false
			  r = 0.0;
			  break;
		}
		if (double.IsNaN(r))
		{
		  return false; // do not attempt to produce NaN
		}
		e1.nval_Renamed = r;
		return true;
	  }

	  private void codenot(Expdesc e)
	  {
		kDischargevars(e);
		switch (e.k)
		{
		  case Expdesc.VNIL:
		  case Expdesc.VFALSE:
			e.k = Expdesc.VTRUE;
			break;

		  case Expdesc.VK:
		  case Expdesc.VKNUM:
		  case Expdesc.VTRUE:
			e.k = Expdesc.VFALSE;
			break;

		  case Expdesc.VJMP:
			invertjump(e);
			break;

		  case Expdesc.VRELOCABLE:
		  case Expdesc.VNONRELOC:
			discharge2anyreg(e);
			freeexp(e);
			e.info_Renamed = kCodeABC(Lua.OP_NOT, 0, e.info_Renamed, 0);
			e.k = Expdesc.VRELOCABLE;
			break;

		  default:
			//# assert false
			break;
		}
		/* interchange true and false lists */
		{
			int temp = e.f;
			e.f = e.t;
			e.t = temp;
		}
		removevalues(e.f);
		removevalues(e.t);
	  }

	  private void removevalues(int list)
	  {
		for (; list != NO_JUMP; list = getjump(list))
		{
		  patchtestreg(list, Lua.NO_REG);
		}
	  }


	  private void dischargejpc()
	  {
		patchlistaux(jpc, pc, Lua.NO_REG, pc);
		jpc = NO_JUMP;
	  }

	  private void discharge2reg(Expdesc e, int reg)
	  {
		kDischargevars(e);
		switch (e.k)
		{
		  case Expdesc.VNIL:
			kNil(reg, 1);
			break;

		  case Expdesc.VFALSE:
		  case Expdesc.VTRUE:
			kCodeABC(Lua.OP_LOADBOOL, reg, (e.k == Expdesc.VTRUE ? 1 : 0), 0);
			break;

		  case Expdesc.VK:
			kCodeABx(Lua.OP_LOADK, reg, e.info_Renamed);
			break;

		  case Expdesc.VKNUM:
			kCodeABx(Lua.OP_LOADK, reg, kNumberK(e.nval_Renamed));
			break;

		  case Expdesc.VRELOCABLE:
			setarga(e, reg);
			break;

		  case Expdesc.VNONRELOC:
			if (reg != e.info_Renamed)
			{
			  kCodeABC(Lua.OP_MOVE, reg, e.info_Renamed, 0);
			}
			break;

		  case Expdesc.VVOID:
		  case Expdesc.VJMP:
			return;

		  default:
			//# assert false
	  break;
		}
		e.nonreloc(reg);
	  }

	  private void exp2reg(Expdesc e, int reg)
	  {
		discharge2reg(e, reg);
		if (e.k == Expdesc.VJMP)
		{
		  e.t = kConcat(e.t, e.info_Renamed); // put this jump in `t' list
		}
		if (e.hasjumps())
		{
		  int p_f = NO_JUMP; // position of an eventual LOAD false
		  int p_t = NO_JUMP; // position of an eventual LOAD true
		  if (need_value(e.t) || need_value(e.f))
		  {
			int fj = (e.k == Expdesc.VJMP) ? NO_JUMP : kJump();
			p_f = code_label(reg, 0, 1);
			p_t = code_label(reg, 1, 0);
			kPatchtohere(fj);
		  }
		  int finalpos = kGetlabel(); // position after whole expression
		  patchlistaux(e.f, finalpos, reg, p_f);
		  patchlistaux(e.t, finalpos, reg, p_t);
		}
		e.init(Expdesc.VNONRELOC, reg);
	  }

	  private int code_label(int a, int b, int jump)
	  {
		kGetlabel(); // those instructions may be jump targets
		return kCodeABC(Lua.OP_LOADBOOL, a, b, jump);
	  }

	  /// <summary>
	  /// check whether list has any jump that do not produce a value
	  /// (or produce an inverted value)
	  /// </summary>
	  private bool need_value(int list)
	  {
		for (; list != NO_JUMP; list = getjump(list))
		{
		  int i = getjumpcontrol(list);
		  int instr = f.code_Renamed[i];
		  if (Lua.OPCODE(instr) != Lua.OP_TESTSET)
		  {
			return true;
		  }
		}
		return false; // not found
	  }

	  private void freeexp(Expdesc e)
	  {
		if (e.kind() == Expdesc.VNONRELOC)
		{
		  freereg(e.info_Renamed);
		}
	  }

	  private void freereg(int reg)
	  {
		if (!Lua.ISK(reg) && reg >= nactvar)
		{
		  --freereg_Renamed;
		  // assert reg == freereg;
		}
	  }

	  internal int getcode(Expdesc e)
	  {
		return f.code_Renamed[e.info_Renamed];
	  }

	  internal void setcode(Expdesc e, int code)
	  {
		f.code_Renamed[e.info_Renamed] = code;
	  }


	  /// <summary>
	  /// Equivalent to searchvar from lparser.c </summary>
	  internal int searchvar(string n)
	  {
		// caution: descending loop (in emulation of PUC-Rio).
		for (int i = nactvar - 1; i >= 0; i--)
		{
		  if (n.Equals(getlocvar(i).varname))
		  {
			return i;
		  }
		}
		return -1; // not found
	  }

	  internal void setarga(Expdesc e, int a)
	  {
	   int at = e.info_Renamed;
	   int[] code = f.code_Renamed;
	   code[at] = Lua.SETARG_A(code[at], a);
	  }

	  internal void setargb(Expdesc e, int b)
	  {
		int at = e.info_Renamed;
		int[] code = f.code_Renamed;
		code[at] = Lua.SETARG_B(code[at], b);
	  }

	  internal void setargc(Expdesc e, int c)
	  {
		int at = e.info_Renamed;
		int[] code = f.code_Renamed;
		code[at] = Lua.SETARG_C(code[at], c);
	  }

	  /// <summary>
	  /// Equivalent to <code>luaK_getlabel</code>. </summary>
	  internal int kGetlabel()
	  {
		lasttarget = pc;
		return pc;
	  }

	  /// <summary>
	  /// Equivalent to <code>luaK_concat</code>.
	  /// l1 was an int*, now passing back as result.
	  /// </summary>
	  internal int kConcat(int l1, int l2)
	  {
		if (l2 == NO_JUMP)
		{
		  return l1;
		}
		else if (l1 == NO_JUMP)
		{
		  return l2;
		}
		else
		{
		  int list = l1;
		  int next;
		  while ((next = getjump(list)) != NO_JUMP) // find last element
		  {
			list = next;
		  }
		  fixjump(list, l2);
		  return l1;
		}
	  }

	  /// <summary>
	  /// Equivalent to <code>luaK_patchlist</code>. </summary>
	  internal void kPatchlist(int list, int target)
	  {
		if (target == pc)
		{
		  kPatchtohere(list);
		}
		else
		{
		  //# assert target < pc
		  patchlistaux(list, target, Lua.NO_REG, target);
		}
	  }

	  private void patchlistaux(int list, int vtarget, int reg, int dtarget)
	  {
		while (list != NO_JUMP)
		{
		  int next = getjump(list);
		  if (patchtestreg(list, reg))
		  {
			fixjump(list, vtarget);
		  }
		  else
		  {
			fixjump(list, dtarget); // jump to default target
		  }
		  list = next;
		}
	  }

	  private bool patchtestreg(int node, int reg)
	  {
		int i = getjumpcontrol(node);
		int[] code = f.code_Renamed;
		int instr = code[i];
		if (Lua.OPCODE(instr) != Lua.OP_TESTSET)
		{
		  return false; // cannot patch other instructions
		}
		if (reg != Lua.NO_REG && reg != Lua.ARGB(instr))
		{
		  code[i] = Lua.SETARG_A(instr, reg);
		}
		else // no register to put value or register already has the value
		{
		  code[i] = Lua.CREATE_ABC(Lua.OP_TEST, Lua.ARGB(instr), 0, Lua.ARGC(instr));
		}

		return true;
	  }

	  private int getjumpcontrol(int at)
	  {
		int[] code = f.code_Renamed;
		if (at >= 1 && testTMode(Lua.OPCODE(code[at - 1])))
		{
		  return at - 1;
		}
		else
		{
		  return at;
		}
	  }

	  /*
	  ** masks for instruction properties. The format is:
	  ** bits 0-1: op mode
	  ** bits 2-3: C arg mode
	  ** bits 4-5: B arg mode
	  ** bit 6: instruction set register A
	  ** bit 7: operator is a test
	  */

	  /// <summary>
	  /// arg modes </summary>
	  private const int OP_ARG_N = 0;
	  private const int OP_ARG_U = 1;
	  private const int OP_ARG_R = 2;
	  private const int OP_ARG_K = 3;

	  /// <summary>
	  /// op modes </summary>
	  private const int iABC = 0;
	  private const int iABx = 1;
	  private const int iAsBx = 2;

	  internal static sbyte opmode(int t, int a, int b, int c, int m)
	  {
		return (sbyte)((t << 7) | (a << 6) | (b << 4) | (c << 2) | m);
	  }

	  private static readonly sbyte[] OPMODE = new sbyte [] {opmode(0, 1, OP_ARG_R, OP_ARG_N, iABC),opmode(0, 1, OP_ARG_K, OP_ARG_N, iABx),opmode(0, 1, OP_ARG_U, OP_ARG_U, iABC),opmode(0, 1, OP_ARG_R, OP_ARG_N, iABC),opmode(0, 1, OP_ARG_U, OP_ARG_N, iABC),opmode(0, 1, OP_ARG_K, OP_ARG_N, iABx),opmode(0, 1, OP_ARG_R, OP_ARG_K, iABC),opmode(0, 0, OP_ARG_K, OP_ARG_N, iABx),opmode(0, 0, OP_ARG_U, OP_ARG_N, iABC),opmode(0, 0, OP_ARG_K, OP_ARG_K, iABC),opmode(0, 1, OP_ARG_U, OP_ARG_U, iABC),opmode(0, 1, OP_ARG_R, OP_ARG_K, iABC),opmode(0, 1, OP_ARG_K, OP_ARG_K, iABC),opmode(0, 1, OP_ARG_K, OP_ARG_K, iABC),opmode(0, 1, OP_ARG_K, OP_ARG_K, iABC),opmode(0, 1, OP_ARG_K, OP_ARG_K, iABC),opmode(0, 1, OP_ARG_K, OP_ARG_K, iABC),opmode(0, 1, OP_ARG_K, OP_ARG_K, iABC),opmode(0, 1, OP_ARG_R, OP_ARG_N, iABC),opmode(0, 1, OP_ARG_R, OP_ARG_N, iABC),opmode(0, 1, OP_ARG_R, OP_ARG_N, iABC),opmode(0, 1, OP_ARG_R, OP_ARG_R, iABC),opmode(0, 0, OP_ARG_R, OP_ARG_N, iAsBx),opmode(1, 0, OP_ARG_K, OP_ARG_K, iABC),opmode(1, 0, OP_ARG_K, OP_ARG_K, iABC),opmode(1, 0, OP_ARG_K, OP_ARG_K, iABC),opmode(1, 1, OP_ARG_R, OP_ARG_U, iABC),opmode(1, 1, OP_ARG_R, OP_ARG_U, iABC),opmode(0, 1, OP_ARG_U, OP_ARG_U, iABC),opmode(0, 1, OP_ARG_U, OP_ARG_U, iABC),opmode(0, 0, OP_ARG_U, OP_ARG_N, iABC),opmode(0, 1, OP_ARG_R, OP_ARG_N, iAsBx),opmode(0, 1, OP_ARG_R, OP_ARG_N, iAsBx),opmode(1, 0, OP_ARG_N, OP_ARG_U, iABC),opmode(0, 0, OP_ARG_U, OP_ARG_U, iABC),opmode(0, 0, OP_ARG_N, OP_ARG_N, iABC),opmode(0, 1, OP_ARG_U, OP_ARG_N, iABx),opmode(0, 1, OP_ARG_U, OP_ARG_N, iABC)};

	  private int getOpMode(int m)
	  {
		return OPMODE[m] & 3;
	  }
	  private bool testAMode(int m)
	  {
		return (OPMODE[m] & (1 << 6)) != 0;
	  }
	  private bool testTMode(int m)
	  {
		return (OPMODE[m] & (1 << 7)) != 0;
	  }

	  /// <summary>
	  /// Equivalent to <code>luaK_patchtohere</code>. </summary>
	  internal void kPatchtohere(int list)
	  {
		kGetlabel();
		jpc = kConcat(jpc, list);
	  }

	  private void fixjump(int at, int dest)
	  {
		int jmp = f.code_Renamed[at];
		int offset = dest - (at + 1);
		//# assert dest != NO_JUMP
		if (Math.Abs(offset) > Lua.MAXARG_sBx)
		{
		  ls.xSyntaxerror("control structure too long");
		}
		f.code_Renamed[at] = Lua.SETARG_sBx(jmp, offset);
	  }

	  private int getjump(int at)
	  {
		int offset = Lua.ARGsBx(f.code_Renamed[at]);
		if (offset == NO_JUMP) // point to itself represents end of list
		{
		 return NO_JUMP; // end of list
		}
		else
		{
		  return (at + 1) + offset; // turn offset into absolute position
		}
	  }

	  /// <summary>
	  /// Equivalent to <code>luaK_jump</code>. </summary>
	  internal int kJump()
	  {
		int old_jpc = jpc; // save list of jumps to here
		jpc = NO_JUMP;
		int j = kCodeAsBx(Lua.OP_JMP, 0, NO_JUMP);
		j = kConcat(j, old_jpc); // keep them on hold
		return j;
	  }

	  /// <summary>
	  /// Equivalent to <code>luaK_storevar</code>. </summary>
	  internal void kStorevar(Expdesc @var, Expdesc ex)
	  {
		switch (@var.k)
		{
		  case Expdesc.VLOCAL:
		  {
			freeexp(ex);
			exp2reg(ex, @var.info_Renamed);
			return;
		  }
		  case Expdesc.VUPVAL:
		  {
			int e = kExp2anyreg(ex);
			kCodeABC(Lua.OP_SETUPVAL, e, @var.info_Renamed, 0);
			break;
		  }
		  case Expdesc.VGLOBAL:
		  {
			int e = kExp2anyreg(ex);
			kCodeABx(Lua.OP_SETGLOBAL, e, @var.info_Renamed);
			break;
		  }
		  case Expdesc.VINDEXED:
		  {
			int e = kExp2RK(ex);
			kCodeABC(Lua.OP_SETTABLE, @var.info_Renamed, @var.aux_Renamed, e);
			break;
		  }
		  default:
		  {
			/* invalid var kind to store */
			//# assert false
			break;
		  }
		}
		freeexp(ex);
	  }

	  /// <summary>
	  /// Equivalent to <code>luaK_indexed</code>. </summary>
	  internal void kIndexed(Expdesc t, Expdesc k)
	  {
		t.aux_Renamed = kExp2RK(k);
		t.k = Expdesc.VINDEXED;
	  }

	  /// <summary>
	  /// Equivalent to <code>luaK_exp2RK</code>. </summary>
	  internal int kExp2RK(Expdesc e)
	  {
		kExp2val(e);
		switch (e.k)
		{
		  case Expdesc.VKNUM:
		  case Expdesc.VTRUE:
		  case Expdesc.VFALSE:
		  case Expdesc.VNIL:
			if (nk <= Lua.MAXINDEXRK) // constant fit in RK operand?
			{
			  e.info_Renamed = (e.k == Expdesc.VNIL) ? nilK() : (e.k == Expdesc.VKNUM) ? kNumberK(e.nval_Renamed) : boolK(e.k == Expdesc.VTRUE);
			  e.k = Expdesc.VK;
			  return e.info_Renamed | Lua.BITRK;
			}
			else
			{
				break;
			}

		  case Expdesc.VK:
			if (e.info_Renamed <= Lua.MAXINDEXRK) // constant fit in argC?
			{
			  return e.info_Renamed | Lua.BITRK;
			}
			else
			{
				break;
			}

		  default:
			  break;
		}
		/* not a constant in the right range: put it in a register */
		return kExp2anyreg(e);
	  }

	  /// <summary>
	  /// Equivalent to <code>luaK_exp2val</code>. </summary>
	  internal void kExp2val(Expdesc e)
	  {
		if (e.hasjumps())
		{
			kExp2anyreg(e);
		}
		else
		{
			kDischargevars(e);
		}
	  }

	  private int boolK(bool b)
	  {
		return addk(Lua.valueOfBoolean(b));
	  }

	  private int nilK()
	  {
		return addk(Lua.NIL);
	  }

	  /// <summary>
	  /// Equivalent to <code>luaK_goiffalse</code>. </summary>
	  internal void kGoiffalse(Expdesc e)
	  {
		int lj; // pc of last jump
		kDischargevars(e);
		switch (e.k)
		{
		  case Expdesc.VNIL:
		  case Expdesc.VFALSE:
			lj = NO_JUMP; // always false; do nothing
			break;

		  case Expdesc.VTRUE:
			lj = kJump(); // always jump
			break;

		  case Expdesc.VJMP:
			lj = e.info_Renamed;
			break;

		  default:
			lj = jumponcond(e, true);
			break;
		}
		e.t = kConcat(e.t, lj); // insert last jump in `t' list
		kPatchtohere(e.f);
		e.f = NO_JUMP;
	  }

	  /// <summary>
	  /// Equivalent to <code>luaK_goiftrue</code>. </summary>
	  internal void kGoiftrue(Expdesc e)
	  {
		int lj; // pc of last jump
		kDischargevars(e);
		switch (e.k)
		{
		  case Expdesc.VK:
		  case Expdesc.VKNUM:
		  case Expdesc.VTRUE:
			lj = NO_JUMP; // always true; do nothing
			break;

		  case Expdesc.VFALSE:
			lj = kJump(); // always jump
			break;

		  case Expdesc.VJMP:
			invertjump(e);
			lj = e.info_Renamed;
			break;

		  default:
			lj = jumponcond(e, false);
			break;
		}
		e.f = kConcat(e.f, lj); // insert last jump in `f' list
		kPatchtohere(e.t);
		e.t = NO_JUMP;
	  }

	  private void invertjump(Expdesc e)
	  {
		int at = getjumpcontrol(e.info_Renamed);
		int[] code = f.code_Renamed;
		int instr = code[at];
		//# assert testTMode(Lua.OPCODE(instr)) && Lua.OPCODE(instr) != Lua.OP_TESTSET && Lua.OPCODE(instr) != Lua.OP_TEST
		code[at] = Lua.SETARG_A(instr, (Lua.ARGA(instr) == 0 ? 1 : 0));
	  }


	  private int jumponcond(Expdesc e, bool cond)
	  {
		if (e.k == Expdesc.VRELOCABLE)
		{
		  int ie = getcode(e);
		  if (Lua.OPCODE(ie) == Lua.OP_NOT)
		  {
			pc--; // remove previous OP_NOT
			return condjump(Lua.OP_TEST, Lua.ARGB(ie), 0, cond ? 0 : 1);
		  }
		  /* else go through */
		}
		discharge2anyreg(e);
		freeexp(e);
		return condjump(Lua.OP_TESTSET, Lua.NO_REG, e.info_Renamed, cond ? 1 : 0);
	  }

	  private int condjump(int op, int a, int b, int c)
	  {
		kCodeABC(op, a, b, c);
		return kJump();
	  }

	  private void discharge2anyreg(Expdesc e)
	  {
		if (e.k != Expdesc.VNONRELOC)
		{
		  kReserveregs(1);
		  discharge2reg(e, freereg_Renamed - 1);
		}
	  }


	  internal void kSelf(Expdesc e, Expdesc key)
	  {
		kExp2anyreg(e);
		freeexp(e);
		int func = freereg_Renamed;
		kReserveregs(2);
		kCodeABC(Lua.OP_SELF, func, e.info_Renamed, kExp2RK(key));
		freeexp(key);
		e.info_Renamed = func;
		e.k = Expdesc.VNONRELOC;
	  }

	  internal void kSetlist(int @base, int nelems, int tostore)
	  {
		int c = (nelems - 1) / Lua.LFIELDS_PER_FLUSH + 1;
		int b = (tostore == Lua.MULTRET) ? 0 : tostore;
		//# assert tostore != 0
		if (c <= Lua.MAXARG_C)
		{
		  kCodeABC(Lua.OP_SETLIST, @base, b, c);
		}
		else
		{
		  kCodeABC(Lua.OP_SETLIST, @base, b, 0);
		  kCode(c, ls.lastline_Renamed);
		}
		freereg_Renamed = @base + 1; // free registers with list values
	  }


	  internal void codecomp(int op, bool cond, Expdesc e1, Expdesc e2)
	  {
		int o1 = kExp2RK(e1);
		int o2 = kExp2RK(e2);
		freeexp(e2);
		freeexp(e1);
		if ((!cond) && op != Lua.OP_EQ)
		{
		  /* exchange args to replace by `<' or `<=' */
		  int temp = o1; // o1 <==> o2
		  o1 = o2;
		  o2 = temp;
		  cond = true;
		}
		e1.info_Renamed = condjump(op, (cond ? 1 : 0), o1, o2);
		e1.k = Expdesc.VJMP;
	  }

	  internal void markupval(int level)
	  {
		BlockCnt b = this.bl;
		while (b != null && b.nactvar > level)
		{
		  b = b.previous;
		}
		if (b != null)
		{
		  b.upval = true;
		}
	  }
	}

}