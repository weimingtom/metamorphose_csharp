using System;
using System.Collections;
using System.Text;
using metamorphose.java;

/*  $Header: //info.ravenbrook.com/project/jili/version/1.1/code/mnj/lua/StringLib.java#1 $
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
	/// Contains Lua's string library.
	/// The library can be opened using the <seealso cref="#open"/> method.
	/// </summary>
	public sealed class StringLib : LuaJavaCallback
	{
	  // Each function in the string library corresponds to an instance of
	  // this class which is associated (the 'which' member) with an integer
	  // which is unique within this class.  They are taken from the following
	  // set.
	  private const int BYTE = 1;
	  private const int CHAR = 2;
	  private const int DUMP = 3;
	  private const int FIND = 4;
	  private const int FORMAT = 5;
	  private const int GFIND = 6;
	  private const int GMATCH = 7;
	  private const int GSUB = 8;
	  private const int LEN = 9;
	  private const int LOWER = 10;
	  private const int MATCH = 11;
	  private const int REP = 12;
	  private const int REVERSE = 13;
	  private const int SUB = 14;
	  private const int UPPER = 15;

	  private const int GMATCH_AUX = 16;

	  private static readonly StringLib GMATCH_AUX_FUN = new StringLib(GMATCH_AUX);

	  /// <summary>
	  /// Which library function this object represents.  This value should
	  /// be one of the "enums" defined in the class.
	  /// </summary>
	  private int which;

	  /// <summary>
	  /// Constructs instance, filling in the 'which' member. </summary>
	  private StringLib(int which)
	  {
		this.which = which;
	  }

	  /// <summary>
	  /// Adjusts the output of string.format so that %e and %g use 'e'
	  /// instead of 'E' to indicate the exponent.  In other words so that
	  /// string.format follows the ISO C (ISO 9899) standard for printf.
	  /// </summary>
	  public void formatISO()
	  {
		FormatItem.E_LOWER = 'e';
	  }

	  /// <summary>
	  /// Implements all of the functions in the Lua string library.  Do not
	  /// call directly. </summary>
	  /// <param name="L">  the Lua state in which to execute. </param>
	  /// <returns> number of returned parameters, as per convention. </returns>
	  public override int luaFunction(Lua L)
	  {
		switch (which)
		{
		  case BYTE:
			return byteFunction(L);
		  case CHAR:
			return charFunction(L);
		  case DUMP:
			return dump(L);
		  case FIND:
			return find(L);
		  case FORMAT:
			return format(L);
		  case GMATCH:
			return gmatch(L);
		  case GSUB:
			return gsub(L);
		  case LEN:
			return len(L);
		  case LOWER:
			return lower(L);
		  case MATCH:
			return match(L);
		  case REP:
			return rep(L);
		  case REVERSE:
			return reverse(L);
		  case SUB:
			return sub(L);
		  case UPPER:
			return upper(L);
		  case GMATCH_AUX:
			return gmatchaux(L);
		}
		return 0;
	  }

	  /// <summary>
	  /// Opens the string library into the given Lua state.  This registers
	  /// the symbols of the string library in a newly created table called
	  /// "string". </summary>
	  /// <param name="L">  The Lua state into which to open. </param>
	  public static void open(Lua L)
	  {
		object lib = L.register("string");

		r(L, "byte", BYTE);
		r(L, "char", CHAR);
		r(L, "dump", DUMP);
		r(L, "find", FIND);
		r(L, "format", FORMAT);
		r(L, "gfind", GFIND);
		r(L, "gmatch", GMATCH);
		r(L, "gsub", GSUB);
		r(L, "len", LEN);
		r(L, "lower", LOWER);
		r(L, "match", MATCH);
		r(L, "rep", REP);
		r(L, "reverse", REVERSE);
		r(L, "sub", SUB);
		r(L, "upper", UPPER);

		LuaTable mt = new LuaTable();
		L.setMetatable("", mt); // set string metatable
		L.setField(mt, "__index", lib);
	  }

	  /// <summary>
	  /// Register a function. </summary>
	  private static void r(Lua L, string name, int which)
	  {
		StringLib f = new StringLib(which);
		object lib = L.getGlobal("string");
		L.setField(lib, name, f);
	  }

	  /// <summary>
	  /// Implements string.byte.  Name mangled to avoid keyword. </summary>
	  private static int byteFunction(Lua L)
	  {
		string s = L.checkString(1);
		int posi = posrelat(L.optInt(2, 1), s);
		int pose = posrelat(L.optInt(3, posi), s);
		if (posi <= 0)
		{
		  posi = 1;
		}
		if (pose > s.Length)
		{
		  pose = s.Length;
		}
		if (posi > pose)
		{
		  return 0; // empty interval; return no values
		}
		int n = pose - posi + 1;
		for (int i = 0; i < n; ++i)
		{
		  L.pushNumber(s[posi + i - 1]);
		}
		return n;
	  }

	  /// <summary>
	  /// Implements string.char.  Name mangled to avoid keyword. </summary>
	  private static int charFunction(Lua L)
	  {
		int n = L.Top; // number of arguments
		StringBuilder b = new StringBuilder();
		for (int i = 1; i <= n; ++i)
		{
		  int c = L.checkInt(i);
		  L.argCheck((char)c == c, i, "invalid value");
		  b.Append((char)c);
		}
		L.push(b.ToString());
		return 1;
	  }

	  /// <summary>
	  /// Implements string.dump. </summary>
	  private static int dump(Lua L)
	  {
		L.checkType(1, Lua.TFUNCTION);
		L.Top = 1;
		try
		{
		  ByteArrayOutputStream s = new ByteArrayOutputStream();
		  Lua.dump(L.value(1), s);
		  ByteArray a = s.toByteArray();
		  s = null;
		  StringBuilder b = new StringBuilder();
		  for (int i = 0; i < a.getLength(); ++i)
		  {
			b.Append((byte)(a.get(i) & 0xff));
		  }
		  L.pushString(b.ToString());
		  return 1;
		}
		catch (IOException)
		{
		  L.error("unabe to dump given function");
		}
		// NOTREACHED
		return 0;
	  }

	  /// <summary>
	  /// Helper for find and match.  Equivalent to str_find_aux. </summary>
	  private static int findAux(Lua L, bool isFind)
	  {
		string s = L.checkString(1);
		string p = L.checkString(2);
		int l1 = s.Length;
		int l2 = p.Length;
		int init = posrelat(L.optInt(3, 1), s) - 1;
		if (init < 0)
		{
		  init = 0;
		}
		else if (init > l1)
		{
		  init = l1;
		}
		if (isFind && (L.toBoolean(L.value(4)) || strpbrk(p, MatchState.SPECIALS) < 0)) // or no special characters? -  explicit request
		{ // do a plain search
		  int off = lmemfind(s.Substring(init), l1 - init, p, l2);
		  if (off >= 0)
		  {
			L.pushNumber(init + off + 1);
			L.pushNumber(init + off + l2);
			return 2;
		  }
		}
		else
		{
		  MatchState ms = new MatchState(L, s, l1);
		  bool anchor = p[0] == '^';
		  int si = init;
		  do
		  {
			ms.level = 0;
			int res = ms.match(si, p, anchor ? 1 : 0);
			if (res >= 0)
			{
			  if (isFind)
			  {
				L.pushNumber(si + 1); // start
				L.pushNumber(res); // end
				return ms.push_captures(-1, -1) + 2;
			  } // else
			  return ms.push_captures(si, res);
			}
		  } while (si++ < ms.end && !anchor);
		}
		L.pushNil(); // not found
		return 1;
	  }

	  /// <summary>
	  /// Implements string.find. </summary>
	  private static int find(Lua L)
	  {
		return findAux(L, true);
	  }

	  /// <summary>
	  /// Implement string.match.  Operates slightly differently from the
	  /// PUC-Rio code because instead of storing the iteration state as
	  /// upvalues of the C closure the iteration state is stored in an
	  /// Object[3] and kept on the stack.
	  /// </summary>
	  private static int gmatch(Lua L)
	  {
		object[] state = new object[3];
		state[0] = L.checkString(1);
		state[1] = L.checkString(2);
		state[2] = new int?(0);
		L.push(GMATCH_AUX_FUN);
		L.push(state);
		return 2;
	  }

	  /// <summary>
	  /// Expects the iteration state, an Object[3] (see {@link
	  /// #gmatch}), to be first on the stack.
	  /// </summary>
	  private static int gmatchaux(Lua L)
	  {
		object[] state = (object[])L.value(1);
		string s = (string)state[0];
		string p = (string)state[1];
		int i = (int)((int?)state[2]);
		MatchState ms = new MatchState(L, s, s.Length);
		for (; i <= ms.end ; ++i)
		{
		  ms.level = 0;
		  int e = ms.match(i, p, 0);
		  if (e >= 0)
		  {
			int newstart = e;
			if (e == i) // empty match?
			{
			  ++newstart; // go at least one position
			}
			state[2] = new int?(newstart);
			return ms.push_captures(i, e);
		  }
		}
		return 0; // not found.
	  }

	  /// <summary>
	  /// Implements string.gsub. </summary>
	  private static int gsub(Lua L)
	  {
		string s = L.checkString(1);
		int sl = s.Length;
		string p = L.checkString(2);
		int maxn = L.optInt(4, sl + 1);
		bool anchor = false;
		if (p.Length > 0)
		{
		  anchor = p[0] == '^';
		}
		if (anchor)
		{
		  p = p.Substring(1);
		}
		MatchState ms = new MatchState(L, s, sl);
		StringBuilder b = new StringBuilder();

		int n = 0;
		int si = 0;
		while (n < maxn)
		{
		  ms.level = 0;
		  int e = ms.match(si, p, 0);
		  if (e >= 0)
		  {
			++n;
			ms.addvalue(b, si, e);
		  }
		  if (e >= 0 && e > si) // non empty match?
		  {
			si = e; // skip it
		  }
		  else if (si < ms.end)
		  {
			b.Append(s[si++]);
		  }
		  else
		  {
			break;
		  }
		  if (anchor)
		  {
			break;
		  }
		}
		b.Append(s.Substring(si));
		L.pushString(b.ToString());
		L.pushNumber(n); // number of substitutions
		return 2;
	  }

	  internal static void addquoted(Lua L, StringBuilder b, int arg)
	  {
		string s = L.checkString(arg);
		int l = s.Length;
		b.Append('"');
		for (int i = 0; i < l; ++i)
		{
		  switch (s[i])
		  {
			case '"':
		case '\\':
	case '\n':
			  b.Append('\\');
			  b.Append(s[i]);
			  break;

			case '\r':
			  b.Append("\\r");
			  break;

			case '\0':
			  b.Append("\\x0000");
			  break;

			default:
			  b.Append(s[i]);
			  break;
		  }
		}
		b.Append('"');
	  }

	  internal static int format(Lua L)
	  {
		int arg = 1;
		string strfrmt = L.checkString(1);
		int sfl = strfrmt.Length;
		StringBuilder b = new StringBuilder();
		int i = 0;
		while (i < sfl)
		{
		  if (strfrmt[i] != MatchState.L_ESC)
		  {
			b.Append(strfrmt[i++]);
		  }
		  else if (strfrmt[++i] == MatchState.L_ESC)
		  {
			b.Append(strfrmt[i++]);
		  }
		  else // format item
		  {
			++arg;
			FormatItem item = new FormatItem(L, strfrmt.Substring(i));
			i += item.length();
			switch (item.type())
			{
			  case 'c':
				item.formatChar(b, (char)L.checkNumber(arg));
				break;

			  case 'd':
		  case 'i':
			  case 'o':
		  case 'u':
	  case 'x':
	case 'X':
			  // :todo: should be unsigned conversions cope better with
			  // negative number?
				item.formatInteger(b, (long)L.checkNumber(arg));
				break;

			  case 'e':
		  case 'E':
	  case 'f':
			  case 'g':
		  case 'G':
				item.formatFloat(b, L.checkNumber(arg));
				break;

			  case 'q':
				addquoted(L, b, arg);
				break;

			  case 's':
				item.formatString(b, L.checkString(arg));
				break;

			  default:
				return L.error("invalid option to 'format'");
			}
		  }
		}
		L.pushString(b.ToString());
		return 1;
	  }

	  /// <summary>
	  /// Implements string.len. </summary>
	  private static int len(Lua L)
	  {
		string s = L.checkString(1);
		L.pushNumber(s.Length);
		return 1;
	  }

	  /// <summary>
	  /// Implements string.lower. </summary>
	  private static int lower(Lua L)
	  {
		string s = L.checkString(1);
		L.push(s.ToLower());
		return 1;
	  }

	  /// <summary>
	  /// Implements string.match. </summary>
	  private static int match(Lua L)
	  {
		return findAux(L, false);
	  }

	  /// <summary>
	  /// Implements string.rep. </summary>
	  private static int rep(Lua L)
	  {
		string s = L.checkString(1);
		int n = L.checkInt(2);
		StringBuilder b = new StringBuilder();
		for (int i = 0; i < n; ++i)
		{
		  b.Append(s);
		}
		L.push(b.ToString());
		return 1;
	  }

	  /// <summary>
	  /// Implements string.reverse. </summary>
	  private static int reverse(Lua L)
	  {
		string s = L.checkString(1);
		StringBuilder b = new StringBuilder();
		int l = s.Length;
		while (--l >= 0)
		{
		  b.Append(s[l]);
		}
		L.push(b.ToString());
		return 1;
	  }

	  /// <summary>
	  /// Helper for <seealso cref="#sub"/> and friends. </summary>
	  private static int posrelat(int pos, string s)
	  {
		if (pos >= 0)
		{
		  return pos;
		}
		int len = s.Length;
		return len + pos + 1;
	  }

	  /// <summary>
	  /// Implements string.sub. </summary>
	  private static int sub(Lua L)
	  {
		string s = L.checkString(1);
		int start = posrelat(L.checkInt(2), s);
		int end = posrelat(L.optInt(3, -1), s);
		if (start < 1)
		{
		  start = 1;
		}
		if (end > s.Length)
		{
		  end = s.Length;
		}
		if (start <= end)
		{
		  L.push(s.Substring(start - 1, end - (start - 1)));
		}
		else
		{
		  L.pushLiteral("");
		}
		return 1;
	  }

	  /// <summary>
	  /// Implements string.upper. </summary>
	  private static int upper(Lua L)
	  {
		string s = L.checkString(1);
		L.push(s.ToUpper());
		return 1;
	  }

	  /// <returns>  character index of start of match (-1 if no match). </returns>
	  private static int lmemfind(string s1, int l1, string s2, int l2)
	  {
		if (l2 == 0)
		{
		  return 0; // empty strings are everywhere
		}
		else if (l2 > l1)
		{
		  return -1; // avoids a negative l1
		}
		return s1.IndexOf(s2);
	  }

	  /// <summary>
	  /// Just like C's strpbrk. </summary>
	  /// <returns> an index into <var>s</var> or -1 for no match. </returns>
	  private static int strpbrk(string s, string set)
	  {
		int l = set.Length;
		for (int i = 0; i < l; ++i)
		{
		  int idx = s.IndexOf(set[i]);
		  if (idx >= 0)
		  {
			return idx;
		  }
		}
		return -1;
	  }
	}

	internal sealed class MatchState
	{
	  internal Lua L;
	  /// <summary>
	  /// The entire string that is the subject of the match. </summary>
	  internal string src;
	  /// <summary>
	  /// The subject's length. </summary>
	  internal int end;
	  /// <summary>
	  /// Total number of captures (finished or unfinished). </summary>
	  internal int level;
	  /// <summary>
	  /// Each capture element is a 2-element array of (index, len). </summary>
	  internal ArrayList capture_Renamed = new ArrayList();
	  // :todo: consider adding the pattern string as a member (and removing
	  // p parameter from methods).

	  // :todo: consider removing end parameter, if end always == // src.length()
	  internal MatchState(Lua L, string src, int end)
	  {
		this.L = L;
		this.src = src;
		this.end = end;
	  }

	  /// <summary>
	  /// Returns the length of capture <var>i</var>.
	  /// </summary>
	  private int captureLen(int i)
	  {
		int[] c = (int[])capture_Renamed[i];
		return c[1];
	  }

	  /// <summary>
	  /// Returns the init index of capture <var>i</var>.
	  /// </summary>
	  private int captureInit(int i)
	  {
		int[] c = (int[])capture_Renamed[i];
		return c[0];
	  }

	  /// <summary>
	  /// Returns the 2-element array for the capture <var>i</var>.
	  /// </summary>
	  private int[] capture(int i)
	  {
		return (int[])capture_Renamed[i];
	  }

	  internal int capInvalid()
	  {
		return L.error("invalid capture index");
	  }

	  internal int malBra()
	  {
		return L.error("malformed pattern (missing '[')");
	  }

	  internal int capUnfinished()
	  {
		return L.error("unfinished capture");
	  }

	  internal int malEsc()
	  {
		return L.error("malformed pattern (ends with '%')");
	  }

	  internal char check_capture(char l)
	  {
		l -= '1'; // relies on wraparound.
		if (l >= level || captureLen(l) == CAP_UNFINISHED)
		{
		  capInvalid();
		}
		return l;
	  }

	  internal int capture_to_close()
	  {
		int lev = level;
		for (lev--; lev >= 0; lev--)
		{
		  if (captureLen(lev) == CAP_UNFINISHED)
		  {
			return lev;
		  }
		}
		return capInvalid();
	  }

	  internal int classend(string p, int pi)
	  {
		switch (p[pi++])
		{
		  case L_ESC:
			// assert pi < p.length() // checked by callers
			return pi + 1;

		  case '[':
			if (p.Length == pi)
			{
			  return malBra();
			}
			if (p[pi] == '^')
			{
			  ++pi;
			}
			do // look for a ']'
			{
			  if (p.Length == pi)
			  {
				return malBra();
			  }
			  if (p[pi++] == L_ESC)
			  {
				if (p.Length == pi)
				{
				  return malBra();
				}
				++pi; // skip escapes (e.g. '%]')
				if (p.Length == pi)
				{
				  return malBra();
				}
			  }
			} while (p[pi] != ']');
			return pi + 1;

		  default:
			return pi;
		}
	  }

	  /// <param name="c">   char match. </param>
	  /// <param name="cl">  character class. </param>
	  internal static bool match_class(char c, char cl)
	  {
		bool res;
		switch (char.ToLower(cl))
		{
		  case 'a' :
			  res = Syntax.isalpha(c);
			  break;
		  case 'c' :
			  res = Syntax.iscntrl(c);
			  break;
		  case 'd' :
			  res = Syntax.isdigit(c);
			  break;
		  case 'l' :
			  res = Syntax.islower(c);
			  break;
		  case 'p' :
			  res = Syntax.ispunct(c);
			  break;
		  case 's' :
			  res = Syntax.isspace(c);
			  break;
		  case 'u' :
			  res = Syntax.isupper(c);
			  break;
		  case 'w' :
			  res = Syntax.isalnum(c);
			  break;
		  case 'x' :
			  res = Syntax.isxdigit(c);
			  break;
		  case 'z' :
			  res = (c == 0);
			  break;
		  default:
			  return (cl == c);
		}
		return char.IsLower(cl) ? res :!res;
	  }

	  /// <param name="pi">  index in p of start of class. </param>
	  /// <param name="ec">  index in p of end of class. </param>
	  internal static bool matchbracketclass(char c, string p, int pi, int ec)
	  {
		// :todo: consider changing char c to int c, then -1 could be used
		// represent a guard value at the beginning and end of all strings (a
		// better NUL).  -1 of course would match no positive class.

		// assert p.charAt(pi) == '[';
		// assert p.charAt(ec) == ']';
		bool sig = true;
		if (p[pi + 1] == '^')
		{
		  sig = false;
		  ++pi; // skip the '6'
		}
		while (++pi < ec)
		{
		  if (p[pi] == L_ESC)
		  {
			++pi;
			if (match_class(c, p[pi]))
			{
			  return sig;
			}
		  }
		  else if ((p[pi + 1] == '-') && (pi + 2 < ec))
		  {
			pi += 2;
			if (p[pi - 2] <= c && c <= p[pi])
			{
			  return sig;
			}
		  }
		  else if (p[pi] == c)
		  {
			return sig;
		  }
		}
		return !sig;
	  }

	  internal static bool singlematch(char c, string p, int pi, int ep)
	  {
		switch (p[pi])
		{
		  case '.': // matches any char
			  return true;
		  case L_ESC:
			  return match_class(c, p[pi + 1]);
		  case '[':
			  return matchbracketclass(c, p, pi, ep - 1);
		  default:
			  return p[pi] == c;
		}
	  }

	  // Generally all the various match functions from PUC-Rio which take a
	  // MatchState and return a "const char *" are transformed into
	  // instance methods that take and return string indexes.

	  internal int matchbalance(int si, string p, int pi)
	  {
		if (pi + 1 >= p.Length)
		{
		  L.error("unbalanced pattern");
		}
		if (si >= end || src[si] != p[pi])
		{
		  return -1;
		}
		char b = p[pi];
		char e = p[pi + 1];
		int cont = 1;
		while (++si < end)
		{
		  if (src[si] == e)
		  {
			if (--cont == 0)
			{
			  return si + 1;
			}
		  }
		  else if (src[si] == b)
		  {
			++cont;
		  }
		}
		return -1; // string ends out of balance
	  }

	  internal int max_expand(int si, string p, int pi, int ep)
	  {
		int i = 0; // counts maximum expand for item
		while (si + i < end && singlematch(src[si + i], p, pi, ep))
		{
		  ++i;
		}
		// keeps trying to match with the maximum repetitions
		while (i >= 0)
		{
		  int res = match(si + i, p, ep + 1);
		  if (res >= 0)
		  {
			return res;
		  }
		  --i; // else didn't match; reduce 1 repetition to try again
		}
		return -1;
	  }

	  internal int min_expand(int si, string p, int pi, int ep)
	  {
		while (true)
		{
		  int res = match(si, p, ep + 1);
		  if (res >= 0)
		  {
			return res;
		  }
		  else if (si < end && singlematch(src[si], p, pi, ep))
		  {
			++si; // try with one more repetition
		  }
		  else
		  {
			return -1;
		  }
		}
	  }

	  internal int start_capture(int si, string p, int pi, int what)
	  {
		capture_Renamed.Capacity = level + 1;
		capture_Renamed[level] = new int[] {si, what};
		++level;
		int res = match(si, p, pi);
		if (res < 0) // match failed
		{
		  --level;
		}
		return res;
	  }

	  internal int end_capture(int si, string p, int pi)
	  {
		int l = capture_to_close();
		capture(l)[1] = si - captureInit(l); // close it
		int res = match(si, p, pi);
		if (res < 0) // match failed?
		{
		  capture(l)[1] = CAP_UNFINISHED; // undo capture
		}
		return res;
	  }

	  internal int match_capture(int si, char l)
	  {
		l = check_capture(l);
		int len = captureLen(l);
		if (end - si >= len)
        //FIXME:&& src.regionMatches(false, captureInit(l), src, si, len)
		{
		  return si + len;
		}
		return -1;
	  }

	  internal const char L_ESC = '%';
	  internal const string SPECIALS = "^$*+?.([%-";
	  private const int CAP_UNFINISHED = -1;
	  private const int CAP_POSITION = -2;

	  /// <param name="si">  index of subject at which to attempt match. </param>
	  /// <param name="p">   pattern string. </param>
	  /// <param name="pi">  index into pattern (from which to being matching). </param>
	  /// <returns> the index of the end of the match, -1 for no match. </returns>
	  internal int match(int si, string p, int pi)
	  {
		// This code has been considerably changed in the transformation
		// from C to Java.  There are the following non-obvious changes:
		// - The C code routinely relies on NUL being accessible at the end of
		//   the pattern string.  In Java we can't do this, so we use many
		//   more explicit length checks and pull error cases into this
		//   function.  :todo: consider appending NUL to the pattern string.
		// - The C code uses a "goto dflt" which is difficult to transform in
		//   the usual way.
			// optimize tail recursion.
		while (true)
		{
		  if (p.Length == pi) // end of pattern
		  {
			return si; // match succeeded
		  }
		  switch (p[pi])
		  {
			case '(':
			  if (p.Length == pi + 1)
			  {
				return capUnfinished();
			  }
			  if (p[pi + 1] == ')') // position capture?
			  {
				return start_capture(si, p, pi + 2, CAP_POSITION);
			  }
			  return start_capture(si, p, pi + 1, CAP_UNFINISHED);

			case ')': // end capture
			  return end_capture(si, p, pi + 1);

			case L_ESC:
			  if (p.Length == pi + 1)
			  {
				return malEsc();
			  }
			  switch (p[pi + 1])
			  {
				case 'b': // balanced string?
				  si = matchbalance(si, p, pi + 2);
				  if (si < 0)
				  {
					return si;
				  }
				  pi += 4;
				  // else return match(ms, s, p+4);
				  goto initContinue; // goto init

				case 'f': // frontier
				{
					pi += 2;
					if (p.Length == pi || p[pi] != '[')
					{
					  return L.error("missing '[' after '%f' in pattern");
					}
					int ep = classend(p, pi); // indexes what is next
					char previous = (si == 0) ? '\0' : src[si - 1];
					char at = (si == end) ? '\0' : src[si];
					if (matchbracketclass(previous, p, pi, ep - 1) || !matchbracketclass(at, p, pi, ep - 1))
					{
					  return -1;
					}
					pi = ep;
					// else return match(ms, s, ep);
				}
				  goto initContinue; // goto init
                  
				default:
				  if (Syntax.isdigit(p[pi + 1])) // capture results (%0-%09)?
				  {
					si = match_capture(si, p[pi + 1]);
					if (si < 0)
					{
					  return si;
					}
					pi += 2;
					// else return match(ms, s, p+2);
					goto initContinue; // goto init
				  }
				  // We emulate a goto dflt by a fallthrough to the next
				  // case (of the outer switch) and making sure that the
				  // next case has no effect when we fallthrough to it from here.
				  // goto dflt;
                  break; //FIXME:
			  }
			  // FALLTHROUGH
              //FIXME:
				goto case '$';

			case '$':
			  if (p[pi] == '$')
			  {
				if (p.Length == pi + 1) // is the '$' the last char in pattern?
				{
				  return (si == end) ? si : -1; // check end of string
				}
				// else goto dflt;
			  }
              goto default; //FIXME:
              // FALLTHROUGH
			default: // it is a pattern item
			{
				int ep = classend(p, pi); // indexes what is next
				bool m = si < end && singlematch(src[si], p, pi, ep);
				if (p.Length > ep)
				{
				  switch (p[ep])
				  {
					case '?': // optional
					  if (m)
					  {
						int res = match(si + 1, p, ep + 1);
						if (res >= 0)
						{
						  return res;
						}
					  }
					  pi = ep + 1;
					  // else return match(s, ep+1);
					  goto initContinue; // goto init

					case '*': // 0 or more repetitions
					  return max_expand(si, p, pi, ep);

					case '+': // 1 or more repetitions
					  return m ? max_expand(si + 1, p, pi, ep) : -1;

					case '-': // 0 or more repetitions (minimum)
					  return min_expand(si, p, pi, ep);
				  }
				}
				// else or default:
				if (!m)
				{
				  return -1;
				}
				++si;
				pi = ep;
				// return match(ms, s+1, ep);
				goto initContinue;
			}
		  }
		initContinue:;
		}
	initBreak:;
	  }

	  /// <param name="s">  index of start of match. </param>
	  /// <param name="e">  index of end of match. </param>
	  internal object onecapture(int i, int s, int e)
	  {
		if (i >= level)
		{
		  if (i == 0) // level == 0, too
		  {
			 return src.Substring(s, e - s); // add whole match
		  }
		  else
		  {
			capInvalid();
		  }
			// NOTREACHED;
		}
		int l = captureLen(i);
		if (l == CAP_UNFINISHED)
		{
		  capUnfinished();
		}
		if (l == CAP_POSITION)
		{
		  return Lua.valueOfNumber(captureInit(i) + 1);
		}
		return src.Substring(captureInit(i), l);
	  }

	  internal void push_onecapture(int i, int s, int e)
	  {
		L.push(onecapture(i, s, e));
	  }

	  /// <param name="s">  index of start of match. </param>
	  /// <param name="e">  index of end of match. </param>
	  internal int push_captures(int s, int e)
	  {
		int nlevels = (level == 0 && s >= 0) ? 1 : level;
		for (int i = 0; i < nlevels; ++i)
		{
		  push_onecapture(i, s, e);
		}
		return nlevels; // number of strings pushed
	  }

	  /// <summary>
	  /// A helper for gsub.  Equivalent to add_s from lstrlib.c. </summary>
	  internal void adds(StringBuilder b, int si, int ei)
	  {
		string news = L.toString(L.value(3));
		int l = news.Length;
		for (int i = 0; i < l; ++i)
		{
		  if (news[i] != L_ESC)
		  {
			b.Append(news[i]);
		  }
		  else
		  {
			++i; // skip L_ESC
			if (!Syntax.isdigit(news[i]))
			{
			  b.Append(news[i]);
			}
			else if (news[i] == '0')
			{
			  b.Append(src.Substring(si, ei - si));
			}
			else
			{
			  // add capture to accumulated result
			  b.Append(L.toString(onecapture(news[i] - '1', si, ei)));
			}
		  }
		}
	  }

	  /// <summary>
	  /// A helper for gsub.  Equivalent to add_value from lstrlib.c. </summary>
	  internal void addvalue(StringBuilder b, int si, int ei)
	  {
		switch (L.type(3))
		{
		  case Lua.TNUMBER:
		  case Lua.TSTRING:
			adds(b, si, ei);
			return;

		  case Lua.TFUNCTION:
		  {
			  L.pushValue(3);
			  int n = push_captures(si, ei);
			  L.call(n, 1);
		  }
			break;

		  case Lua.TTABLE:
			L.push(L.getTable(L.value(3), onecapture(0, si, ei)));
			break;

		  default:
		  {
			L.argError(3, "string/function/table expected");
			return;
		  }
		}
		if (!L.toBoolean(L.value(-1))) // nil or false
		{
		  L.pop(1);
		  L.pushString(src.Substring(si, ei - si));
		}
		else if (!L.isString(L.value(-1)))
		{
		  L.error("invalid replacement value (a " + Lua.typeName(L.type(-1)) + ")");
		}
		b.Append(L.toString(L.value(-1))); // add result to accumulator
		L.pop(1);
	  }
	}

	internal sealed class FormatItem
	{
	  private Lua L;
	  private bool left; // '-' flag
	  private bool sign; // '+' flag
	  private bool space; // ' ' flag
	  private bool alt; // '#' flag
	  private bool zero; // '0' flag
	  private int width; // minimum field width
	  private int precision = -1; // precision, -1 when no precision specified.
	  private char type_Renamed; // the type of the conversion
	  private int length_Renamed; // length of the format item in the format string.

	  /// <summary>
	  /// Character used in formatted output when %e or %g format is used.
	  /// </summary>
	  internal static char E_LOWER = 'E';
	  /// <summary>
	  /// Character used in formatted output when %E or %G format is used.
	  /// </summary>
	  internal static char E_UPPER = 'E';

	  /// <summary>
	  /// Parse a format item (starting from after the <code>L_ESC</code>).
	  /// If you promise that there won't be any format errors, then
	  /// <var>L</var> can be <code>null</code>.
	  /// </summary>
	  internal FormatItem(Lua L, string s)
	  {
		this.L = L;
		int i = 0;
		int l = s.Length;
		// parse flags
		while (true)
		{
		  if (i >= l)
		  {
			L.error("invalid format");
		  }
		  switch (s[i])
		  {
			case '-':
			  left = true;
			  break;
			case '+':
			  sign = true;
			  break;
			case ' ':
			  space = true;
			  break;
			case '#':
			  alt = true;
			  break;
			case '0':
			  zero = true;
			  break;
			default:
			  goto flagBreak;
		  }
		  ++i;
		flagContinue:;
		} // flag
	flagBreak:
		// parse width
		int widths = i; // index of start of width specifier
		while (true)
		{
		  if (i >= l)
		  {
			L.error("invalid format");
		  }
		  if (Syntax.isdigit(s[i]))
		  {
			++i;
		  }
		  else
		  {
			break;
		  }
		}
		if (widths < i)
		{
		  try
		  {
			width = Convert.ToInt32(s.Substring(widths, i - widths));
		  }
		  catch (NumberFormatException)
		  {
		  }
		}
		// parse precision
		if (s[i] == '.')
		{
		  ++i;
		  int precisions = i; // index of start of precision specifier
		  while (true)
		  {
			if (i >= l)
			{
			  L.error("invalid format");
			}
			if (Syntax.isdigit(s[i]))
			{
			  ++i;
			}
			else
			{
			  break;
			}
		  }
		  if (precisions < i)
		  {
			try
			{
			  precision = Convert.ToInt32(s.Substring(precisions, i - precisions));
			}
			catch (NumberFormatException)
			{
			}
		  }
		}
		switch (s[i])
		{
		  case 'c':
		  case 'd':
	  case 'i':
		  case 'o':
	  case 'u':
	case 'x':
	case 'X':
		  case 'e':
	  case 'E':
	case 'f':
	case 'g':
	case 'G':
		  case 'q':
		  case 's':
			type_Renamed = s[i];
			length_Renamed = i + 1;
			return;
		}
		L.error("invalid option to 'format'");
	  }

	  internal int length()
	  {
		return length_Renamed;
	  }

	  internal int type()
	  {
		return type_Renamed;
	  }

	  /// <summary>
	  /// Format the converted string according to width, and left.
	  /// zero padding is handled in either <seealso cref="FormatItem#formatInteger"/>
	  /// or <seealso cref="FormatItem#formatFloat"/>
	  /// (and width is fixed to 0 in such cases).  Therefore we can ignore
	  /// zero.
	  /// </summary>
	  private void format(StringBuilder b, string s)
	  {
		int l = s.Length;
		if (l >= width)
		{
		  b.Append(s);
		  return;
		}
		StringBuilder pad = new StringBuilder();
		while (l < width)
		{
		  pad.Append(' ');
		  ++l;
		}
		if (left)
		{
		  b.Append(s);
		  b.Append(pad);
		}
		else
		{
		  b.Append(pad);
		  b.Append(s);
		}
	  }

	  // All the format* methods take a StringBuffer and append the
	  // formatted representation of the value to it.
	  // Sadly after a format* method has been invoked the object is left in
	  // an unusable state and should not be used again.

	  internal void formatChar(StringBuilder b, char c)
	  {
		string s = Convert.ToString(c);
		format(b, s);
	  }

	  internal void formatInteger(StringBuilder b, long i)
	  {
		// :todo: improve inefficient use of implicit StringBuffer

		if (left)
		{
		  zero = false;
		}
		if (precision >= 0)
		{
		  zero = false;
		}

		int radix = 10;
		switch (type_Renamed)
		{
		  case 'o':
			radix = 8;
			break;
		  case 'd':
	  case 'i':
	case 'u':
			radix = 10;
			break;
		  case 'x':
	  case 'X':
			radix = 16;
			break;
		  default:
			L.error("invalid format");
		break;
		}
		string s = Convert.ToString(i, radix);
		if (type_Renamed == 'X')
		{
		  s = s.ToUpper();
		}
		if (precision == 0 && s.Equals("0"))
		{
		  s = "";
		}

		// form a prefix by strippping possible leading '-',
		// pad to precision,
		// add prefix,
		// pad to width.
		// extra wart: padding with '0' is implemented using precision
		// because this makes handling the prefix easier.
		string prefix = "";
		if (s.StartsWith("-"))
		{
		  prefix = "-";
		  s = s.Substring(1);
		}
		if (alt && radix == 16)
		{
		  prefix = "0x";
		}
		if (prefix == "")
		{
		  if (sign)
		  {
			prefix = "+";
		  }
		  else if (space)
		  {
			prefix = " ";
		  }
		}
		if (alt && radix == 8 && !s.StartsWith("0"))
		{
		  s = "0" + s;
		}
		int l = s.Length;
		if (zero)
		{
		  precision = width - prefix.Length;
		  width = 0;
		}
		if (l < precision)
		{
		  StringBuilder p = new StringBuilder();
		  while (l < precision)
		  {
			p.Append('0');
			++l;
		  }
		  p.Append(s);
		  s = p.ToString();
		}
		s = prefix + s;
		format(b, s);
	  }

	  internal void formatFloat(StringBuilder b, double d)
	  {
		switch (type_Renamed)
		{
		  case 'g':
	  case 'G':
			formatFloatG(b, d);
			return;
		  case 'f':
			formatFloatF(b, d);
			return;
		  case 'e':
	  case 'E':
			formatFloatE(b, d);
			return;
		}
	  }

	  private void formatFloatE(StringBuilder b, double d)
	  {
		string s = formatFloatRawE(d);
		format(b, s);
	  }

	  /// <summary>
	  /// Returns the formatted string for the number without any padding
	  /// (which can be added by invoking <seealso cref="FormatItem#format"/> later).
	  /// </summary>
	  private string formatFloatRawE(double d)
	  {
		double m = Math.Abs(d);
		int offset = 0;
		if (m >= 1e-3 && m < 1e7)
		{
		  d *= 1e10;
		  offset = 10;
		}

		string s = Convert.ToString(d);
		StringBuilder t = new StringBuilder(s);
		int e; // Exponent value
		if (d == 0)
		{
		  e = 0;
		}
		else
		{
		  int ei = s.IndexOf('E');
		  e = Convert.ToInt32(s.Substring(ei + 1));
		  t.Remove(ei, int.MaxValue);
		}

		precisionTrim(t);

		e -= offset;
		if (char.IsLower(type_Renamed))
		{
		  t.Append(E_LOWER);
		}
		else
		{
		  t.Append(E_UPPER);
		}
		if (e >= 0)
		{
		  t.Append('+');
		}
		t.Append(Convert.ToString(e));

		zeroPad(t);
		return t.ToString();
	  }

	  private void formatFloatF(StringBuilder b, double d)
	  {
		string s = formatFloatRawF(d);
		format(b, s);
	  }

	  /// <summary>
	  /// Returns the formatted string for the number without any padding
	  /// (which can be added by invoking <seealso cref="FormatItem#format"/> later).
	  /// </summary>
	  private string formatFloatRawF(double d)
	  {
		string s = Convert.ToString(d);
		StringBuilder t = new StringBuilder(s);

		int di = s.IndexOf('.');
		int ei = s.IndexOf('E');
		if (ei >= 0)
		{
		  t.Remove(ei, int.MaxValue);
		  int e = Convert.ToInt32(s.Substring(ei + 1));

		  StringBuilder z = new StringBuilder();
		  for (int i = 0; i < Math.Abs(e); ++i)
		  {
			z.Append('0');
		  }

		  if (e > 0)
		  {
			t.Remove(di, 1);
			t.Append(z);
			t.Insert(di + e, '.');
		  }
		  else
		  {
			t.Remove(di, 1);
			int at = t[0] == '-' ? 1 : 0;
			t.Insert(at, z);
			t.Insert(di, '.');
		  }
		}

		precisionTrim(t);
		zeroPad(t);

		return t.ToString();
	  }

	  private void formatFloatG(StringBuilder b, double d)
	  {
		if (precision == 0)
		{
		  precision = 1;
		}
		if (precision < 0)
		{
		  precision = 6;
		}
		string s;
		// Decide whether to use %e or %f style.
		double m = Math.Abs(d);
		if (m == 0)
		{
		  // :todo: Could test for -0 and use "-0" appropriately.
		  s = "0";
		}
		else if (m < 1e-4 || m >= Lua.iNumpow(10, precision))
		{
		  // %e style
		  --precision;
		  s = formatFloatRawE(d);
		  int di = s.IndexOf('.');
		  if (di >= 0)
		  {
			// Trim trailing zeroes from fractional part
			int ei = s.IndexOf('E');
			if (ei < 0)
			{
			  ei = s.IndexOf('e');
			}
			int i = ei - 1;
			while (s[i] == '0')
			{
			  --i;
			}
			if (s[i] != '.')
			{
			  ++i;
			}
			StringBuilder a = new StringBuilder(s);
			a.Remove(i, ei);
			s = a.ToString();
		  }
		}
		else
		{
		  // %f style
		  // For %g precision specifies the number of significant digits,
		  // for %f precision specifies the number of fractional digits.
		  // There is a problem because it's not obvious how many fractional
		  // digits to format, it could be more than precision
		  // (when .0001 <= m < 1) or it could be less than precision
		  // (when m >= 1).
		  // Instead of trying to work out the correct precision to use for
		  // %f formatting we use a worse case to get at least all the
		  // necessary digits, then we trim using string editing.  The worst
		  // case is that 3 zeroes come after the decimal point before there
		  // are any significant digits.
		  // Save the required number of significant digits
		  int required = precision;
		  precision += 3;
		  s = formatFloatRawF(d);
		  int fsd = 0; // First Significant Digit
		  while (s[fsd] == '0' || s[fsd] == '.')
		  {
			++fsd;
		  }
		  // Note that all the digits to the left of the decimal point in
		  // the formatted number are required digits (either significant
		  // when m >= 1 or 0 when m < 1).  We know this because otherwise 
		  // m >= (10**precision) and so formatting falls under the %e case.
		  // That means that we can always trim the string at fsd+required
		  // (this will remove the decimal point when m >=
		  // (10**(precision-1)).
		  StringBuilder a = new StringBuilder(s);
		  a.Remove(fsd + required, int.MaxValue);
		  if (s.IndexOf('.') < a.Length)
		  {
			// Trim trailing zeroes
			int i = a.Length - 1;
			while (a[i] == '0')
			{
			  a.Remove(i, 1);
			  --i;
			}
			if (a[i] == '.')
			{
			  a.Remove(i, 1);
			}
		  }
		  s = a.ToString();
		}
		format(b, s);
	  }

	  internal void formatString(StringBuilder b, string s)
	  {
		string p = s;

		if (precision >= 0 && precision < s.Length)
		{
		  p = s.Substring(0, precision);
		}
		format(b, p);
	  }

	  private void precisionTrim(StringBuilder t)
	  {
		if (precision < 0)
		{
		  precision = 6;
		}

		string s = t.ToString();
		int di = s.IndexOf('.');
		int l = t.Length;
		if (0 == precision)
		{
		  t.Remove(di, int.MaxValue);
		}
		else if (l > di + precision)
		{
		  t.Remove(di + precision + 1, int.MaxValue);
		}
		else
		{
		  for (; l <= di + precision; ++l)
		  {
			t.Append('0');
		  }
		}
	  }

	  private void zeroPad(StringBuilder t)
	  {
		if (zero && t.Length < width)
		{
		  int at = t[0] == '-' ? 1 : 0;
		  while (t.Length < width)
		  {
			t.Insert(at, '0');
		  }
		}
	  }
	}

}