using System;
using metamorphose.java;

/*  $Header: //info.ravenbrook.com/project/jili/version/1.1/code/mnj/lua/LuaTable.java#1 $
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
	/// Class that models Lua's tables.  Each Lua table is an instance of
	/// this class.  Whilst you can clearly see that this class extends
	/// <seealso cref="java.util.Hashtable"/> you should in no way rely upon that.
	/// Calling any methods that are not defined in this class (but are
	/// defined in a super class) is extremely deprecated.
	/// </summary>
	public class LuaTable : Hashtable
	{
	  private const int MAXBITS = 26;
	  private static readonly int MAXASIZE = 1 << MAXBITS;

	  private LuaTable metatable; // = null;
	  private static readonly object[] ZERO = new object[0];
	  /// <summary>
	  /// Array used so that tables accessed like arrays are more efficient.
	  /// All elements stored at an integer index, <var>i</var>, in the
	  /// range [1,sizeArray] are stored at <code>array[i-1]</code>.
	  /// This speed and space usage for array-like access.
	  /// When the table is rehashed the array's size is chosen to be the
	  /// largest power of 2 such that at least half the entries are
	  /// occupied.  Default access granted for <seealso cref="Enum"/> class, do not
	  /// abuse.
	  /// </summary>
	  internal object[] array = ZERO;
	  /// <summary>
	  /// Equal to <code>array.length</code>.  Default access granted for
	  /// <seealso cref="Enum"/> class, do not abuse.
	  /// </summary>
	  internal int sizeArray; // = 0;
	  /// <summary>
	  /// <code>true</code> whenever we are in the <seealso cref="#rehash"/>
	  /// method.  Avoids infinite rehash loops.
	  /// </summary>
	  private bool inrehash; // = false;

	  internal LuaTable() : base(1)
	  {
	  }

	  /// <summary>
	  /// Fresh LuaTable with hints for preallocating to size. </summary>
	  /// <param name="narray">  number of array slots to preallocate. </param>
	  /// <param name="nhash">   number of hash slots to preallocate. </param>
	  internal LuaTable(int narray, int nhash) : base(nhash)
	  {
		// :todo: super(nhash) isn't clearly correct as adding nhash hash
		// table entries will causes a rehash with the usual implementation
		// (which rehashes when ratio of entries to capacity exceeds the
		// load factor of 0.75).  Perhaps ideally we would size the hash
		// tables such that adding nhash entries will not cause a rehash.
		array = new object[narray];
		for (int i = 0; i < narray; ++i)
		{
		  array[i] = Lua.NIL;
		}
		sizeArray = narray;
	  }

	  /// <summary>
	  /// Implements discriminating equality.  <code>o1.equals(o2) == (o1 ==
	  /// o2) </code>.  This method is not necessary in CLDC, it's only
	  /// necessary in J2SE because java.util.Hashtable overrides equals. </summary>
	  /// <param name="o">  the reference to compare with. </param>
	  /// <returns> true when equal. </returns>
	  public override bool Equals(object o)
	  {
		return this == o;
	  }

	  /// <summary>
	  /// Provided to avoid Checkstyle warning.  This method is not necessary
	  /// for correctness (in neither JME nor JSE), it's only provided to
	  /// remove a Checkstyle warning.
	  /// Since <seealso cref="#equals"/> implements the most discriminating
	  /// equality possible, this method can have any implementation. </summary>
	  /// <returns> an int. </returns>
	  public override int GetHashCode()
	  {
		return SystemUtil.identityHashCode(this);
	  }

	  private static int arrayindex(object key)
	  {
		if (key is double?)
		{
		  double d = (double)((double?)key);
		  int k = (int)d;
		  if (k == d)
		  {
			return k;
		  }
		}
		return -1; // 'key' did not match some condition
	  }

	  private static int computesizes(int[] nums, int[] narray)
	  {
		int t = narray[0];
		int a = 0; // number of elements smaller than 2^i
		int na = 0; // number of elements to go to array part
		int n = 0; // optimal size for array part
		int twotoi = 1; // 2^i
		for (int i = 0; twotoi / 2 < t; ++i)
		{
		  if (nums[i] > 0)
		  {
			a += nums[i];
			if (a > twotoi / 2) // more than half elements present?
			{
			  n = twotoi; // optimal size (till now)
			  na = a; // all elements smaller than n will go to array part
			}
		  }
		  if (a == t) // all elements already counted
		  {
			break;
		  }
		  twotoi *= 2;
		}
		narray[0] = n;
		//# assert narray[0]/2 <= na && na <= narray[0]
		return na;
	  }

	  private int countint(object key, int[] nums)
	  {
		int k = arrayindex(key);
		if (0 < k && k <= MAXASIZE) // is 'key' an appropriate array index?
		{
		  ++nums[ceillog2(k)]; // count as such
		  return 1;
		}
		return 0;
	  }

	  private int numusearray(int[] nums)
	  {
		int ause = 0; // summation of 'nums'
		int i = 1; // count to traverse all array keys
		int ttlg = 1; // 2^lg
		for (int lg = 0; lg <= MAXBITS; ++lg) // for each slice
		{
		  int lc = 0; // counter
		  int lim = ttlg;
		  if (lim > sizeArray)
		  {
			lim = sizeArray; // adjust upper limit
			if (i > lim)
			{
			  break; // no more elements to count
			}
		  }
		  // count elements in range (2^(lg-1), 2^lg]
		  for (; i <= lim; ++i)
		  {
			if (array[i - 1] != Lua.NIL)
			{
			  ++lc;
			}
		  }
		  nums[lg] += lc;
		  ause += lc;
		  ttlg *= 2;
		}
		return ause;
	  }

	  private int numusehash(int[] nums, int[] pnasize)
	  {
		int totaluse = 0; // total number of elements
		int ause = 0; // summation of nums
		System.Collections.IEnumerator e;
		e = base.Keys.GetEnumerator();
		while (e.hasMoreElements())
		{
		  object o = e.nextElement();
		  ause += countint(o, nums);
		  ++totaluse;
		}
		pnasize[0] += ause;
		return totaluse;
	  }

	  /// <param name="nasize">  (new) size of array part </param>
	  private void resize(int nasize)
	  {
		if (nasize == sizeArray)
		{
		  return;
		}
		object[] newarray = new object[nasize];
		if (nasize > sizeArray) // array part must grow?
		{
		  // The new array slots, from sizeArray to nasize-1, must
		  // be filled with their values from the hash part.
		  // There are two strategies:
		  // Iterate over the new array slots, and look up each index in the
		  // hash part to see if it has a value; or,
		  // Iterate over the hash part and see if each key belongs in the
		  // array part.
		  // For now we choose the first algorithm.
		  // :todo: consider using second algorithm, possibly dynamically.
		  Array.Copy(array, 0, newarray, 0, array.Length);
		  for (int i = array.Length; i < nasize; ++i)
		  {
			object key = new double?(i + 1);
			object v = base.Remove(key);
			if (v == null)
			{
			  v = Lua.NIL;
			}
			newarray[i] = v;
		  }
		}
		if (nasize < sizeArray) // array part must shrink?
		{
		  // move elements from array slots nasize to sizeArray-1 to the
		  // hash part.
		  for (int i = nasize; i < sizeArray; ++i)
		  {
			if (array[i] != Lua.NIL)
			{
			  object key = new double?(i + 1);
			  base[key] = array[i];
			}
		  }
		  Array.Copy(array, 0, newarray, 0, newarray.Length);
		}
		array = newarray;
		sizeArray = array.Length;
	  }

	  public void rehash()
	  {
		bool oldinrehash = inrehash;
		inrehash = true;
		if (!oldinrehash)
		{
		  int[] nasize = new int[1];
		  int[] nums = new int[MAXBITS + 1];
		  nasize[0] = numusearray(nums); // count keys in array part
		  int totaluse = nasize[0];
		  totaluse += numusehash(nums, nasize);
		  int na = computesizes(nums, nasize);

		  resize(nasize[0]);
		}
		base.rehash();
		inrehash = oldinrehash;
	  }

	  /// <summary>
	  /// Getter for metatable member. </summary>
	  /// <returns>  The metatable. </returns>
	  internal LuaTable getMetatable()
	  {
		return metatable;
	  }
	  /// <summary>
	  /// Setter for metatable member. </summary>
	  /// <param name="metatable">  The metatable. </param>
	  // :todo: Support metatable's __gc and __mode keys appropriately.
	  //        This involves detecting when those keys are present in the
	  //        metatable, and changing all the entries in the Hashtable
	  //        to be instance of java.lang.Ref as appropriate.
	  internal void setMetatable(LuaTable metatable)
	  {
		this.metatable = metatable;
		return;
	  }

	  /// <summary>
	  /// Supports Lua's length (#) operator.  More or less equivalent to
	  /// luaH_getn and unbound_search in ltable.c.
	  /// </summary>
	  internal int getn()
	  {
		int j = sizeArray;
		if (j > 0 && array[j - 1] == Lua.NIL)
		{
		  // there is a boundary in the array part: (binary) search for it
		  int i = 0;
		  while (j - i > 1)
		  {
			int m = (i + j) / 2;
			if (array[m - 1] == Lua.NIL)
			{
			  j = m;
			}
			else
			{
			  i = m;
			}
		  }
		  return i;
		}

		// unbound_search

		int i = 0;
		j = 1;
		// Find 'i' and 'j' such that i is present and j is not.
		while (this.getnum(j) != Lua.NIL)
		{
		  i = j;
		  j *= 2;
		  if (j < 0) // overflow
		  {
			// Pathological case.  Linear search.
			i = 1;
			while (this.getnum(i) != Lua.NIL)
			{
			  ++i;
			}
			return i - 1;
		  }
		}
		// binary search between i and j
		while (j - i > 1)
		{
		  int m = (i + j) / 2;
		  if (this.getnum(m) == Lua.NIL)
		  {
			j = m;
		  }
		  else
		  {
			i = m;
		  }
		}
		return i;
	  }

	  /// <summary>
	  /// Like <seealso cref="java.util.Hashtable#get"/>.  Ensures that indexes
	  /// with no value return <seealso cref="Lua#NIL"/>.  In order to get the correct
	  /// behaviour for <code>t[nil]</code>, this code assumes that Lua.NIL
	  /// is non-<code>null</code>.
	  /// </summary>
	  public object getlua(object key)
	  {
		if (key is double?)
		{
		  double d = (double)((double?)key);
		  if (d <= sizeArray && d >= 1)
		  {
			int i = (int)d;
			if (i == d)
			{
			  return array[i - 1];
			}
		  }
		}
		object r = base[key];
		if (r == null)
		{
		  r = Lua.NIL;
		}
		return r;
	  }

	  /// <summary>
	  /// Like <seealso cref="#getlua(Object)"/> but the result is written into
	  /// the <var>value</var> <seealso cref="Slot"/>.
	  /// </summary>
	  internal void getlua(Slot key, Slot value)
	  {
		if (key.r == Lua.NUMBER)
		{
		  double d = key.d;
		  if (d <= sizeArray && d >= 1)
		  {
			int i = (int)d;
			if (i == d)
			{
			  value.Object = array[i - 1];
			  return;
			}
		  }
		}
		object r = base[key.asObject()];
		if (r == null)
		{
		  r = Lua.NIL;
		}
		value.Object = r;
	  }

	  /// <summary>
	  /// Like get for numeric (integer) keys. </summary>
	  internal object getnum(int k)
	  {
		if (k <= sizeArray && k >= 1)
		{
		  return array[k - 1];
		}
		object r = base[new double?(k)];
		if (r == null)
		{
		  return Lua.NIL;
		}
		return r;
	  }

	  /// <summary>
	  /// Like <seealso cref="java.util.Hashtable#put"/> but enables Lua's semantics
	  /// for <code>nil</code>;
	  /// In particular that <code>x = nil</nil>
	  /// deletes <code>x</code>.
	  /// And also that <code>t[nil]</code> raises an error.
	  /// Generally, users of Jill should be using
	  /// <seealso cref="Lua#setTable"/> instead of this. </summary>
	  /// <param name="key"> key. </param>
	  /// <param name="value"> value. </param>
	  internal void putlua(Lua L, object key, object value)
	  {
		double d = 0.0;
		int i = int.MaxValue;

		if (key == Lua.NIL)
		{
		  L.gRunerror("table index is nil");
		}
		if (key is double?)
		{
		  d = (double)((double?)key);
		  int j = (int)d;

		  if (j == d && j >= 1)
		  {
			i = j; // will cause additional check for array part later if
				   // the array part check fails now.
			if (i <= sizeArray)
			{
			  array[i - 1] = value;
			  return;
			}
		  }
		  if (double.IsNaN(d))
		  {
			L.gRunerror("table index is NaN");
		  }
		}
		// :todo: Consider checking key for NaN (PUC-Rio does)
		if (value == Lua.NIL)
		{
		  remove(key);
		  return;
		}
		base[key] = value;
		// This check is necessary because sometimes the call to super.put
		// can rehash and the new (k,v) pair should be in the array part
		// after the rehash, but is still in the hash part.
		if (i <= sizeArray)
		{
		  remove(key);
		  array[i - 1] = value;
		}
	  }

	  internal void putlua(Lua L, Slot key, object value)
	  {
		int i = int.MaxValue;

		if (key.r == Lua.NUMBER)
		{
		  int j = (int)key.d;
		  if (j == key.d && j >= 1)
		  {
			i = j;
			if (i <= sizeArray)
			{
			  array[i - 1] = value;
			  return;
			}
		  }
		  if (double.IsNaN(key.d))
		  {
			L.gRunerror("table index is NaN");
		  }
		}
		object k = key.asObject();
		// :todo: consider some sort of tail merge with the other putlua
		if (value == Lua.NIL)
		{
		  remove(k);
		  return;
		}
		base[k] = value;
		if (i <= sizeArray)
		{
		  remove(k);
		  array[i - 1] = value;
		}
	  }

	  /// <summary>
	  /// Like put for numeric (integer) keys.
	  /// </summary>
	  internal void putnum(int k, object v)
	  {
		if (k <= sizeArray && k >= 1)
		{
		  array[k - 1] = v;
		  return;
		}
		// The key can never be NIL so putlua will never notice that its L
		// argument is null.
		// :todo: optimisation to avoid putlua checking for array part again
		putlua(null, new double?(k), v);
	  }

	  /// <summary>
	  /// Do not use, implementation exists only to generate deprecated
	  /// warning. </summary>
	  /// @deprecated Use getlua instead. 
	  public object get(object key)
	  {
		throw new System.ArgumentException();
	  }

      public Enumeration keys()
	  {
		return new Enum(this, base.Keys.GetEnumerator());
	  }

	  /// <summary>
	  /// Do not use, implementation exists only to generate deprecated
	  /// warning. </summary>
	  /// @deprecated Use putlua instead. 
	  public object put(object key, object value)
	  {
		throw new System.ArgumentException();
	  }

	  /// <summary>
	  /// Used by oLog2.  DO NOT MODIFY.
	  /// </summary>
	  private static readonly sbyte[] LOG2 = new sbyte[] {0,1,2,2,3,3,3,3,4,4,4,4,4,4,4,4,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5, 6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6, 7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7, 7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7, 8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8, 8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8, 8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8, 8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8};

	  /// <summary>
	  /// Equivalent to luaO_log2.
	  /// </summary>
	  private static int oLog2(int x)
	  {
		//# assert x >= 0

		int l = -1;
		while (x >= 256)
		{
		  l += 8;
		  x = (int)((uint)x >> 8);
		}
		return l + LOG2[x];
	  }

	  private static int ceillog2(int x)
	  {
		return oLog2(x - 1) + 1;
	  }
	
    }
}