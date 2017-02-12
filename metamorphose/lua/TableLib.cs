using System.Collections;
using System.Text;
using metamorphose.java;

/*  $Header: //info.ravenbrook.com/project/jili/version/1.1/code/mnj/lua/TableLib.java#1 $
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
	/// Contains Lua's table library.
	/// The library can be opened using the <seealso cref="#open"/> method.
	/// </summary>
	public sealed class TableLib : LuaJavaCallback
	{
	  // Each function in the table library corresponds to an instance of
	  // this class which is associated (the 'which' member) with an integer
	  // which is unique within this class.  They are taken from the following
	  // set.
	  private const int CONCAT = 1;
	  private const int INSERT = 2;
	  private const int MAXN = 3;
	  private const int REMOVE = 4;
	  private const int SORT = 5;
	  private const int GETN = 6;

	  /// <summary>
	  /// Which library function this object represents.  This value should
	  /// be one of the "enums" defined in the class.
	  /// </summary>
	  private int which;

	  /// <summary>
	  /// Constructs instance, filling in the 'which' member. </summary>
	  private TableLib(int which)
	  {
		this.which = which;
	  }

	  /// <summary>
	  /// Implements all of the functions in the Lua table library.  Do not
	  /// call directly. </summary>
	  /// <param name="L">  the Lua state in which to execute. </param>
	  /// <returns> number of returned parameters, as per convention. </returns>
	  public override int luaFunction(Lua L)
	  {
		switch (which)
		{
		  case CONCAT:
			return concat(L);
		  case INSERT:
			return insert(L);
		  case MAXN:
			return maxn(L);
		  case REMOVE:
			return remove(L);
		  case SORT:
			return sort(L);

			//FIXME: added
		  case GETN:
			return getn(L);
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
		L.register("table");

		r(L, "concat", CONCAT);
		r(L, "insert", INSERT);
		r(L, "getn", GETN); //FIXME: added
		r(L, "maxn", MAXN);
		r(L, "remove", REMOVE);
		r(L, "sort", SORT);
	  }

	  /// <summary>
	  /// Register a function. </summary>
	  private static void r(Lua L, string name, int which)
	  {
		TableLib f = new TableLib(which);
		object lib = L.getGlobal("table");
		L.setField(lib, name, f);
	  }

	  /// <summary>
	  /// Implements table.concat. </summary>
	  private static int concat(Lua L)
	  {
		string sep = L.optString(2, "");
		L.checkType(1, Lua.TTABLE);
		int i = L.optInt(3, 1);
		int last = L.optInt(4, L.objLen(L.value(1)));
		StringBuilder b = new StringBuilder();
		object t = L.value(1);
		for (; i <= last; ++i)
		{
		  object v = L.rawGetI(t, i);
		  L.argCheck(L.isString(v), 1, "table contains non-strings");
		  b.Append(L.ToString(v));
		  if (i != last)
		  {
			b.Append(L.ToString(sep));
		  }
		}
		L.pushString(b.ToString());
		return 1;
	  }

	  /// <summary>
	  /// Implements table.insert. </summary>
	  private static int insert(Lua L)
	  {
		int e = aux_getn(L, 1) + 1; // first empty element
		int pos; // where to insert new element
		object t = L.value(1);
		switch (L.Top)
		{
		  case 2: // called with only 2 arguments
			pos = e; // insert new element at the end
			break;

		  case 3:
		  {
			  int i;
			  pos = L.checkInt(2); // 2nd argument is the position
			  if (pos > e)
			  {
				e = pos; // grow array if necessary
			  }
			  for (i = e; i > pos; --i) // move up elements
			  {
				// t[i] = t[i-1]
				L.rawSetI(t, i, L.rawGetI(t, i - 1));
			  }
		  }
			break;

		  default:
			return L.error("wrong number of arguments to 'insert'");
		}
		L.rawSetI(t, pos, L.value(-1)); // t[pos] = v
		return 0;
	  }

	  /// <summary>
	  /// Implements table.maxn. </summary>
	  private static int maxn(Lua L)
	  {
		double max = 0;
		L.checkType(1, Lua.TTABLE);
		LuaTable t = (LuaTable)L.value(1);
        Enumerator e = t.Keys();
		while (e.hasMoreElements())
		{
		  object o = e.nextElement();
		  if (Lua.type(o) == Lua.TNUMBER)
		  {
			double v = L.toNumber(o);
			if (v > max)
			{
			  max = v;
			}
		  }
		}
		L.pushNumber(max);
		return 1;
	  }

	  /// <summary>
	  /// Implements table.remove. </summary>
	  private static int remove(Lua L)
	  {
		int e = aux_getn(L, 1);
		int pos = L.optInt(2, e);
		if (e == 0)
		{
		  return 0; // table is 'empty'
		}
		object t = L.value(1);
		object o = L.rawGetI(t, pos); // result = t[pos]
		for (;pos < e; ++pos)
		{
		  L.rawSetI(t, pos, L.rawGetI(t, pos + 1)); // t[pos] = t[pos+1]
		}
		L.rawSetI(t, e, Lua.NIL); // t[e] = nil
		L.push(o);
		return 1;
	  }

	  /// <summary>
	  /// Implements table.sort. </summary>
	  private static int sort(Lua L)
	  {
		int n = aux_getn(L, 1);
		if (!L.isNoneOrNil(2)) // is there a 2nd argument?
		{
		  L.checkType(2, Lua.TFUNCTION);
		}
		L.Top = 2; // make sure there is two arguments
		auxsort(L, 1, n);
		return 0;
	  }

	  internal static void auxsort(Lua L, int l, int u)
	  {
		object t = L.value(1);
		while (l < u) // for tail recursion
		{
		  int i;
		  int j;
		  // sort elements a[l], a[l+u/2], and a[u]
		  object o1 = L.rawGetI(t, l);
		  object o2 = L.rawGetI(t, u);
		  if (sort_comp(L, o2, o1)) // a[u] < a[l]?
		  {
			L.rawSetI(t, l, o2);
			L.rawSetI(t, u, o1);
		  }
		  if (u - l == 1)
		  {
			break; // only 2 elements
		  }
		  i = (l + u) / 2;
		  o1 = L.rawGetI(t, i);
		  o2 = L.rawGetI(t, l);
		  if (sort_comp(L, o1, o2)) // a[i]<a[l]?
		  {
			L.rawSetI(t, i, o2);
			L.rawSetI(t, l, o1);
		  }
		  else
		  {
			o2 = L.rawGetI(t, u);
			if (sort_comp(L, o2, o1)) // a[u]<a[i]?
			{
			  L.rawSetI(t, i, o2);
			  L.rawSetI(t, u, o1);
			}
		  }
		  if (u - l == 2)
		  {
			break; // only 3 elements
		  }
		  object p = L.rawGetI(t, i); // Pivot
		  o2 = L.rawGetI(t, u - 1);
		  L.rawSetI(t, i, o2);
		  L.rawSetI(t, u - 1, p);
		  // a[l] <= P == a[u-1] <= a[u], only need to sort from l+1 to u-2
		  i = l;
		  j = u - 1;
		  // NB: Pivot P is in p
		  while (true) // invariant: a[l..i] <= P <= a[j..u]
		  {
			// repeat ++i until a[i] >= P
			while (true)
			{
			  o1 = L.rawGetI(t, ++i);
			  if (!sort_comp(L, o1, p))
			  {
				break;
			  }
			  if (i > u)
			  {
				L.error("invalid order function for sorting");
			  }
			}
			// repreat --j until a[j] <= P
			while (true)
			{
			  o2 = L.rawGetI(t, --j);
			  if (!sort_comp(L, p, o2))
			  {
				break;
			  }
			  if (j < l)
			  {
				L.error("invalid order function for sorting");
			  }
			}
			if (j < i)
			{
			  break;
			}
			L.rawSetI(t, i, o2);
			L.rawSetI(t, j, o1);
		  }
		  o1 = L.rawGetI(t, u - 1);
		  o2 = L.rawGetI(t, i);
		  L.rawSetI(t, u - 1, o2);
		  L.rawSetI(t, i, o1); // swap pivot (a[u-1]) with a[i]
		  // a[l..i-1 <= a[i] == P <= a[i+1..u]
		  // adjust so that smaller half is in [j..i] and larger one in [l..u]
		  if (i - l < u - i)
		  {
			j = l;
			i = i - 1;
			l = i + 2;
		  }
		  else
		  {
			j = i + 1;
			i = u;
			u = j - 2;
		  }
		  auxsort(L, j, i); // call recursively the smaller one
		} // repeat the routine for the larger one
	  }

	  private static bool sort_comp(Lua L, object a, object b)
	  {
		if (!L.isNil(L.value(2))) // function?
		{
		  L.pushValue(2);
		  L.push(a);
		  L.push(b);
		  L.call(2, 1);
		  bool res = L.toBoolean(L.value(-1));
		  L.pop(1);
		  return res;
		}
		else // a < b?
		{
		  return L.lessThan(a, b);
		}
	  }

	  private static int aux_getn(Lua L, int n)
	  {
		L.checkType(n, Lua.TTABLE);
		LuaTable t = (LuaTable)L.value(n);
		return t.getn();
	  }


	//FIXME: added
	   private static int getn(Lua L)
	   {
		  L.pushNumber(aux_getn(L, 1));
		  return 1;
	   }
	}

}