using System;
using System.Text;
using System.Collections;
using metamorphose.java;

/*  $Header: //info.ravenbrook.com/project/jili/version/1.1/code/mnj/lua/Lua.java#3 $
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
	/// <para>
	/// Encapsulates a Lua execution environment.  A lot of Jill's public API
	/// manifests as public methods in this class.  A key part of the API is
	/// the ability to call Lua functions from Java (ultimately, all Lua code
	/// is executed in this manner).
	/// </para>
	/// 
	/// <para>
	/// The Stack
	/// </para>
	/// 
	/// <para>
	/// All arguments to Lua functions and all results returned by Lua
	/// functions are placed onto a stack.  The stack can be indexed by an
	/// integer in the same way as the PUC-Rio implementation.  A positive
	/// index is an absolute index and ranges from 1 (the bottom-most
	/// element) through to <var>n</var> (the top-most element),
	/// where <var>n</var> is the number of elements on the stack.  Negative
	/// indexes are relative indexes, -1 is the top-most element, -2 is the
	/// element underneath that, and so on.  0 is not used.
	/// </para>
	/// 
	/// <para>
	/// Note that in Jill the stack is used only for passing arguments and
	/// returning results, unlike PUC-Rio.
	/// </para>
	/// 
	/// <para>
	/// The protocol for calling a function is described in the <seealso cref="#call"/>
	/// method.  In brief: push the function onto the stack, then push the
	/// arguments to the call.
	/// </para>
	/// 
	/// <para>
	/// The methods <seealso cref="#push"/>, <seealso cref="#pop"/>, <seealso cref="#value"/>,
	/// <seealso cref="#getTop"/>, <seealso cref="#setTop"/> are used to manipulate the stack.
	/// </para>
	/// </summary>
	public sealed class Lua
	{
		private bool InstanceFieldsInitialized = false;

		private void InitializeInstanceFields()
		{
			civ.addElement(new CallInfo());
		}

		public const bool D = false;

	  /// <summary>
	  /// Version string. </summary>
	  public const string VERSION = "Lua 5.1 (Jill 1.0.1)";

	  //FIXME:added
	  public const string RELEASE = "Lua 5.1.4 (Jill 1.0.1)";
	  public const int VERSION_NUM = 501;
	  public const string COPYRIGHT = "Copyright (C) 1994-2008 Lua.org, PUC-Rio (Copyright (C) 2006 Nokia Corporation and/or its subsidiary(-ies))";
	  /// <summary>
	  /// http://www.ravenbrook.com </summary>
	  public const string AUTHORS = "R. Ierusalimschy, L. H. de Figueiredo & W. Celes (Ravenbrook Limited)";


	  /// <summary>
	  /// Table of globals (global variables).  This actually shared across
	  /// all threads (with the same main thread), but kept in each Lua
	  /// thread as an optimisation.
	  /// </summary>
	  private LuaTable global;
	  private LuaTable registry;

	  /// <summary>
	  /// Reference the main Lua thread.  Itself if this is the main Lua
	  /// thread.
	  /// </summary>
	  private Lua main;

	  /// <summary>
	  /// VM data stack.
	  /// </summary>
	  private Slot[] stack = new Slot[0];
	  /// <summary>
	  /// One more than the highest stack slot that has been written to
	  /// (ever).
	  /// Used by <seealso cref="#stacksetsize"/> to determine which stack slots
	  /// need nilling when growing the stack.
	  /// </summary>
	  internal int stackhighwater; // = 0;
	  /// <summary>
	  /// Number of active elemements in the VM stack.  Should always be
	  /// <code><= stack.length</code>.
	  /// </summary>
	  private int stackSize; // = 0;
	  /// <summary>
	  /// The base stack element for this stack frame.  If in a Lua function
	  /// then this is the element indexed by operand field 0; if in a Java
	  /// functipn then this is the element indexed by Lua.value(1).
	  /// </summary>
	  private int @base; // = 0;

	  internal int nCcalls; // = 0;
	  /// <summary>
	  /// Instruction to resume execution at.  Index into code array. </summary>
	  private int savedpc; // = 0;
	  /// <summary>
	  /// Vector of CallInfo records.  Actually it's a Stack which is a
	  /// subclass of Vector, but it mostly the Vector methods that are used.
	  /// </summary>
      private metamorphose.java.Stack civ = new metamorphose.java.Stack();
	  /// <summary>
	  /// CallInfo record for currently active function. </summary>
	  private CallInfo __ci()
	  {
		return (CallInfo)civ.lastElement();
	  }

	  /// <summary>
	  /// Open Upvalues.  All UpVal objects that reference the VM stack.
	  /// openupval is a java.util.Vector of UpVal stored in order of stack
	  /// slot index: higher stack indexes are stored at higher Vector
	  /// positions.
	  /// </summary>
	  private ArrayList openupval = new ArrayList();

	  internal int hookcount;
	  internal int basehookcount;
	  internal bool allowhook = true;
	  internal Hook hook;
	  internal int hookmask;

	  /// <summary>
	  /// Number of list items to accumulate before a SETLIST instruction. </summary>
	  internal const int LFIELDS_PER_FLUSH = 50;

	  /// <summary>
	  /// Limit for table tag-method chains (to avoid loops) </summary>
	  private const int MAXTAGLOOP = 100;

	  /// <summary>
	  /// The current error handler (set by <seealso cref="#pcall"/>).  A Lua
	  /// function to call.
	  /// </summary>
	  private object errfunc;

	  /// <summary>
	  /// thread activation status.
	  /// </summary>
	  private int status_Renamed;

	  /// <summary>
	  /// Nonce object used by pcall and friends (to detect when an
	  /// exception is a Lua error). 
	  /// </summary>
	  private const string LUA_ERROR = "";

	  /// <summary>
	  /// Metatable for primitive types.  Shared between all threads. </summary>
	  private LuaTable[] metatable;

	  /// <summary>
	  /// Maximum number of local variables per function.  As per
	  /// LUAI_MAXVARS from "luaconf.h".  Default access so that {@link
	  /// FuncState} can see it.
	  /// </summary>
	  internal const int MAXVARS = 200;
	  internal const int MAXSTACK = 250;
	  internal const int MAXUPVALUES = 60;

	  /// <summary>
	  /// Stored in Slot.r to denote a numeric value (which is stored at 
	  /// Slot.d).
	  /// </summary>
	  internal static readonly object NUMBER = new object();

	  /// <summary>
	  /// Spare Slot used for a temporary.
	  /// </summary>
	  private static readonly Slot SPARE_SLOT = new Slot();

	  /// <summary>
	  /// Registry key for loaded modules.
	  /// </summary>
	  internal const string LOADED = "_LOADED";

	  /// <summary>
	  /// Used to construct a Lua thread that shares its global state with
	  /// another Lua state.
	  /// </summary>
	  private Lua(Lua L)
	  {
		  if (!InstanceFieldsInitialized)
		  {
			  InitializeInstanceFields();
			  InstanceFieldsInitialized = true;
		  }
		// Copy the global state, that's shared across all threads that
		// share the same main thread, into the new Lua thread.
		// Any more than this and the global state should be shunted to a
		// separate object (as it is in PUC-Rio).
		this.global = L.global;
		this.registry = L.registry;
		this.metatable = L.metatable;
		this.main = L;
	  }

	  //////////////////////////////////////////////////////////////////////
	  // Public API

	  /// <summary>
	  /// Creates a fresh Lua state.
	  /// </summary>
	  public Lua()
	  {
		  if (!InstanceFieldsInitialized)
		  {
			  InitializeInstanceFields();
			  InstanceFieldsInitialized = true;
		  }
		this.global = new LuaTable();
		this.registry = new LuaTable();
		this.metatable = new LuaTable[NUM_TAGS];
		this.main = this;
	  }

	  /// <summary>
	  /// Equivalent of LUA_MULTRET.
	  /// </summary>
	  // Required, by vmPoscall, to be negative.
	  public const int MULTRET = -1;
	  /// <summary>
	  /// The Lua <code>nil</code> value.
	  /// </summary>
	  public static readonly object NIL = new object();

	  // Lua type tags, from lua.h
	  /// <summary>
	  /// Lua type tag, representing no stack value. </summary>
	  public const int TNONE = -1;
	  /// <summary>
	  /// Lua type tag, representing <code>nil</code>. </summary>
	  public const int TNIL = 0;
	  /// <summary>
	  /// Lua type tag, representing boolean. </summary>
	  public const int TBOOLEAN = 1;
	  // TLIGHTUSERDATA not available.  :todo: make available?
	  /// <summary>
	  /// Lua type tag, representing numbers. </summary>
	  public const int TNUMBER = 3;
	  /// <summary>
	  /// Lua type tag, representing strings. </summary>
	  public const int TSTRING = 4;
	  /// <summary>
	  /// Lua type tag, representing tables. </summary>
	  public const int TTABLE = 5;
	  /// <summary>
	  /// Lua type tag, representing functions. </summary>
	  public const int TFUNCTION = 6;
	  /// <summary>
	  /// Lua type tag, representing userdata. </summary>
	  public const int TUSERDATA = 7;
	  /// <summary>
	  /// Lua type tag, representing threads. </summary>
	  public const int TTHREAD = 8;
	  /// <summary>
	  /// Number of type tags.  Should be one more than the
	  /// last entry in the list of tags.
	  /// </summary>
	  private const int NUM_TAGS = 9;
	  /// <summary>
	  /// Names for above type tags, starting from <seealso cref="#TNIL"/>.
	  /// Equivalent to luaT_typenames.
	  /// </summary>
	  private static readonly string[] TYPENAME = new string[] {"nil", "boolean", "userdata", "number", "string", "table", "function", "userdata", "thread"};

	  /// <summary>
	  /// Minimum stack size that Lua Java functions gets.  May turn out to
	  /// be silly / redundant.
	  /// </summary>
	  public const int MINSTACK = 20;

	  /// <summary>
	  /// Status code, returned from pcall and friends, that indicates the
	  /// thread has yielded.
	  /// </summary>
	  public const int YIELD = 1;
	  /// <summary>
	  /// Status code, returned from pcall and friends, that indicates
	  /// a runtime error.
	  /// </summary>
	  public const int ERRRUN = 2;
	  /// <summary>
	  /// Status code, returned from pcall and friends, that indicates
	  /// a syntax error.
	  /// </summary>
	  public const int ERRSYNTAX = 3;
	  /// <summary>
	  /// Status code, returned from pcall and friends, that indicates
	  /// a memory allocation error.
	  /// </summary>
	  private const int ERRMEM = 4;
	  /// <summary>
	  /// Status code, returned from pcall and friends, that indicates
	  /// an error whilst running the error handler function.
	  /// </summary>
	  public const int ERRERR = 5;
	  /// <summary>
	  /// Status code, returned from loadFile and friends, that indicates
	  /// an IO error.
	  /// </summary>
	  public const int ERRFILE = 6;

	  // Enums for gc().
	  /// <summary>
	  /// Action, passed to <seealso cref="#gc"/>, that requests the GC to stop. </summary>
	  public const int GCSTOP = 0;
	  /// <summary>
	  /// Action, passed to <seealso cref="#gc"/>, that requests the GC to restart. </summary>
	  public const int GCRESTART = 1;
	  /// <summary>
	  /// Action, passed to <seealso cref="#gc"/>, that requests a full collection. </summary>
	  public const int GCCOLLECT = 2;
	  /// <summary>
	  /// Action, passed to <seealso cref="#gc"/>, that returns amount of memory
	  /// (in Kibibytes) in use (by the entire Java runtime).
	  /// </summary>
	  public const int GCCOUNT = 3;
	  /// <summary>
	  /// Action, passed to <seealso cref="#gc"/>, that returns the remainder of
	  /// dividing the amount of memory in use by 1024.
	  /// </summary>
	  public const int GCCOUNTB = 4;
	  /// <summary>
	  /// Action, passed to <seealso cref="#gc"/>, that requests an incremental
	  /// garbage collection be performed.
	  /// </summary>
	  public const int GCSTEP = 5;
	  /// <summary>
	  /// Action, passed to <seealso cref="#gc"/>, that sets a new value for the
	  /// <var>pause</var> of the collector.
	  /// </summary>
	  public const int GCSETPAUSE = 6;
	  /// <summary>
	  /// Action, passed to <seealso cref="#gc"/>, that sets a new values for the
	  /// <var>step multiplier</var> of the collector.
	  /// </summary>
	  public const int GCSETSTEPMUL = 7;

	  // Some of the hooks, etc, aren't implemented, so remain private.
	  private const int HOOKCALL = 0;
	  private const int HOOKRET = 1;
	  private const int HOOKLINE = 2;
	  /// <summary>
	  /// When <seealso cref="Hook"/> callback is called as a line hook, its
	  /// <var>ar.event</var> field is <code>HOOKCOUNT</code>.
	  /// </summary>
	  public const int HOOKCOUNT = 3;
	  private const int HOOKTAILRET = 4;

	  private const int MASKCALL = 1 << HOOKCALL;
	  private const int MASKRET = 1 << HOOKRET;
	  private const int MASKLINE = 1 << HOOKLINE;
	  /// <summary>
	  /// Bitmask that specifies count hook in call to <seealso cref="#setHook"/>.
	  /// </summary>
	  public static readonly int MASKCOUNT = 1 << HOOKCOUNT;


	  /// <summary>
	  /// Calls a Lua value.  Normally this is called on functions, but the
	  /// semantics of Lua permit calls on any value as long as its metatable
	  /// permits it.
	  /// 
	  /// In order to call a function, the function must be
	  /// pushed onto the stack, then its arguments must be
	  /// <seealso cref="#push pushed"/> onto the stack; the first argument is pushed
	  /// directly after the function,
	  /// then the following arguments are pushed in order (direct
	  /// order).  The parameter <var>nargs</var> specifies the number of
	  /// arguments (which may be 0).
	  /// 
	  /// When the function returns the function value on the stack and all
	  /// the arguments are removed from the stack and replaced with the
	  /// results of the function, adjusted to the number specified by
	  /// <var>nresults</var>.  So the first result from the function call will
	  /// be at the same index where the function was immediately prior to
	  /// calling this method.
	  /// </summary>
	  /// <param name="nargs">     The number of arguments in this function call. </param>
	  /// <param name="nresults">  The number of results required. </param>
	  public void call(int nargs, int nresults)
	  {
		apiChecknelems(nargs + 1);
		int func = stackSize - (nargs + 1);
		this.vmCall(func, nresults);
	  }

	  /// <summary>
	  /// Closes a Lua state.  In this implementation, this method does
	  /// nothing.
	  /// </summary>
	  public void close()
	  {
	  }

	  /// <summary>
	  /// Concatenate values (usually strings) on the stack.
	  /// <var>n</var> values from the top of the stack are concatenated, as
	  /// strings, and replaced with the resulting string. </summary>
	  /// <param name="n">  the number of values to concatenate. </param>
	  public void concat(int n)
	  {
		apiChecknelems(n);
		if (n >= 2)
		{
		  vmConcat(n, (stackSize - @base) - 1);
		  pop(n - 1);
		}
		else if (n == 0) // push empty string
		{
		  push("");
		} // else n == 1; nothing to do
	  }

	  /// <summary>
	  /// Creates a new empty table and returns it. </summary>
	  /// <param name="narr">  number of array elements to pre-allocate. </param>
	  /// <param name="nrec">  number of non-array elements to pre-allocate. </param>
	  /// <returns> a fresh table. </returns>
	  /// <seealso cref= #newTable </seealso>
	  public LuaTable createTable(int narr, int nrec)
	  {
		return new LuaTable(narr, nrec);
	  }

	  /// <summary>
	  /// Dumps a function as a binary chunk. </summary>
	  /// <param name="function">  the Lua function to dump. </param>
	  /// <param name="writer">    the stream that receives the dumped binary. </param>
	  /// <exception cref="IOException"> when writer does. </exception>
	  public static void dump(object function, OutputStream writer)
	  {
		if (!(function is LuaFunction))
		{
		  throw new IOException("Cannot dump " + typeName(type(function)));
		}
		LuaFunction f = (LuaFunction)function;
		uDump(f.proto(), writer, false);
	  }

	  /// <summary>
	  /// Tests for equality according to the semantics of Lua's
	  /// <code>==</code> operator (so may call metamethods). </summary>
	  /// <param name="o1">  a Lua value. </param>
	  /// <param name="o2">  another Lua value. </param>
	  /// <returns> true when equal. </returns>
	  public bool equal(object o1, object o2)
	  {
		if (o1 is double?)
		{
		  return o1.Equals(o2);
		}
		return vmEqualRef(o1, o2);
	  }

	  /// <summary>
	  /// Generates a Lua error using the error message. </summary>
	  /// <param name="message">  the error message. </param>
	  /// <returns> never. </returns>
	  public int error(object message)
	  {
		return gErrormsg(message);
	  }

	  /// <summary>
	  /// Control garbage collector.  Note that in Jill most of the options
	  /// to this function make no sense and they will not do anything. </summary>
	  /// <param name="what">  specifies what GC action to take. </param>
	  /// <param name="data">  data that may be used by the action. </param>
	  /// <returns> varies. </returns>
	  public int gc(int what, int data)
	  {
		Runtime rt;

		switch (what)
		{
		  case GCSTOP:
			return 0;
		  case GCRESTART:
		  case GCCOLLECT:
		  case GCSTEP:
			SystemUtil.gc();
			return 0;
		  case GCCOUNT:
			rt = Runtime.getRuntime();
			return (int)((rt.totalMemory() - rt.freeMemory()) / 1024);
		  case GCCOUNTB:
			rt = Runtime.getRuntime();
			return (int)((rt.totalMemory() - rt.freeMemory()) % 1024);
		  case GCSETPAUSE:
		  case GCSETSTEPMUL:
			return 0;
		}
		return 0;
	  }

	  /// <summary>
	  /// Returns the environment table of the Lua value. </summary>
	  /// <param name="o">  the Lua value. </param>
	  /// <returns> its environment table. </returns>
	  public LuaTable getFenv(object o)
	  {
		if (o is LuaFunction)
		{
		  LuaFunction f = (LuaFunction)o;
		  return f.getEnv();
		}
		if (o is LuaJavaCallback)
		{
		  LuaJavaCallback f = (LuaJavaCallback)o;
		  // :todo: implement this case.
		  return null;
		}

		if (o is LuaUserdata)
		{
		  LuaUserdata u = (LuaUserdata)o;
		  return u.getEnv();
		}
		if (o is Lua)
		{
		  Lua l = (Lua)o;
		  return l.global;
		}
		return null;
	  }

	  /// <summary>
	  /// Get a field from a table (or other object). </summary>
	  /// <param name="t">      The object whose field to retrieve. </param>
	  /// <param name="field">  The name of the field. </param>
	  /// <returns>  the Lua value </returns>
	  public object getField(object t, string field)
	  {
		return getTable(t, field);
	  }

	  /// <summary>
	  /// Get a global variable. </summary>
	  /// <param name="name">  The name of the global variable. </param>
	  /// <returns>  The value of the global variable. </returns>
	  public object getGlobal(string name)
	  {
		return getField(global, name);
	  }

	  /// <summary>
	  /// Gets the global environment.  The global environment, where global
	  /// variables live, is returned as a <code>LuaTable</code>.  Note that
	  /// modifying this table has exactly the same effect as creating or
	  /// changing global variables from within Lua. </summary>
	  /// <returns>  The global environment as a table. </returns>
	  public LuaTable Globals
	  {
		  get
		  {
			return global;
		  }
	  }

	  /// <summary>
	  /// Get metatable. </summary>
	  /// <param name="o">  the Lua value whose metatable to retrieve. </param>
	  /// <returns> The metatable, or null if there is no metatable. </returns>
	  public LuaTable getMetatable(object o)
	  {
		LuaTable mt;

		if (o is LuaTable)
		{
		  LuaTable t = (LuaTable)o;
		  mt = t.getMetatable();
		}
		else if (o is LuaUserdata)
		{
		  LuaUserdata u = (LuaUserdata)o;
		  mt = u.getMetatable();
		}
		else
		{
		  mt = metatable[type(o)];
		}
		return mt;
	  }

	  /// <summary>
	  /// Gets the registry table.
	  /// </summary>
	  public LuaTable Registry
	  {
		  get
		  {
			return registry;
		  }
	  }

	  /// <summary>
	  /// Indexes into a table and returns the value. </summary>
	  /// <param name="t">  the Lua value to index. </param>
	  /// <param name="k">  the key whose value to return. </param>
	  /// <returns> the value t[k]. </returns>
	  public object getTable(object t, object k)
	  {
		Slot s = new Slot(k);
		Slot v = new Slot();
		vmGettable(t, s, v);
		return v.asObject();
	  }

	  /// <summary>
	  /// Gets the number of elements in the stack.  If the stack is not
	  /// empty then this is the index of the top-most element. </summary>
	  /// <returns> number of stack elements. </returns>
	  public int Top
	  {
		  get
		  {
			return stackSize - @base;
		  }
		  set
		  {
			if (value < 0)
			{
			  throw new System.ArgumentException();
			}
			stacksetsize(@base + value);
		  }
	  }

	  /// <summary>
	  /// Insert Lua value into stack immediately at specified index.  Values
	  /// in stack at that index and higher get pushed up. </summary>
	  /// <param name="o">    the Lua value to insert into the stack. </param>
	  /// <param name="idx">  the stack index at which to insert. </param>
	  public void insert(object o, int idx)
	  {
		idx = absIndexUnclamped(idx);
		stackInsertAt(o, idx);
	  }

	  /// <summary>
	  /// Tests that an object is a Lua boolean. </summary>
	  /// <param name="o">  the Object to test. </param>
	  /// <returns> true if and only if the object is a Lua boolean. </returns>
	  public static bool isBoolean(object o)
	  {
		return o is bool?;
	  }

	  /// <summary>
	  /// Tests that an object is a Lua function implementated in Java (a Lua
	  /// Java Function). </summary>
	  /// <param name="o">  the Object to test. </param>
	  /// <returns> true if and only if the object is a Lua Java Function. </returns>
	  public static bool isJavaFunction(object o)
	  {
		return o is LuaJavaCallback;
	  }

	  /// <summary>
	  /// Tests that an object is a Lua function (implemented in Lua or
	  /// Java). </summary>
	  /// <param name="o">  the Object to test. </param>
	  /// <returns> true if and only if the object is a function. </returns>
	  public bool isFunction(object o) //static
	  {
		return o is LuaFunction || o is LuaJavaCallback;
	  }

	  /// <summary>
	  /// Tests that a Lua thread is the main thread. </summary>
	  /// <returns> true if and only if is the main thread. </returns>
	  public bool Main
	  {
		  get
		  {
			return this == main;
		  }
	  }

	  /// <summary>
	  /// Tests that an object is Lua <code>nil</code>. </summary>
	  /// <param name="o">  the Object to test. </param>
	  /// <returns> true if and only if the object is Lua <code>nil</code>. </returns>
	  public bool isNil(object o) //static
	  {
		return NIL == o;
	  }

	  /// <summary>
	  /// Tests that an object is a Lua number or a string convertible to a
	  /// number. </summary>
	  /// <param name="o">  the Object to test. </param>
	  /// <returns> true if and only if the object is a number or a convertible string. </returns>
	  public bool isNumber(object o) //static
	  {
		SPARE_SLOT.Object = o;
		return tonumber(SPARE_SLOT, NUMOP);
	  }

	  /// <summary>
	  /// Tests that an object is a Lua string or a number (which is always
	  /// convertible to a string). </summary>
	  /// <param name="o">  the Object to test. </param>
	  /// <returns> true if and only if object is a string or number. </returns>
	  public bool isString(object o) //static
	  {
		return o is string || o is double?;
	  }

	  /// <summary>
	  /// Tests that an object is a Lua table. </summary>
	  /// <param name="o">  the Object to test. </param>
	  /// <returns> <code>true</code> if and only if the object is a Lua table. </returns>
	  public bool isTable(object o) //static
	  {
		return o is LuaTable;
	  }

	  /// <summary>
	  /// Tests that an object is a Lua thread. </summary>
	  /// <param name="o">  the Object to test. </param>
	  /// <returns> <code>true</code> if and only if the object is a Lua thread. </returns>
	  public static bool isThread(object o)
	  {
		return o is Lua;
	  }

	  /// <summary>
	  /// Tests that an object is a Lua userdata. </summary>
	  /// <param name="o">  the Object to test. </param>
	  /// <returns> true if and only if the object is a Lua userdata. </returns>
	  public static bool isUserdata(object o)
	  {
		return o is LuaUserdata;
	  }

	  /// <summary>
	  /// <para>
	  /// Tests that an object is a Lua value.  Returns <code>true</code> for
	  /// an argument that is a Jill representation of a Lua value,
	  /// <code>false</code> for Java references that are not Lua values.
	  /// For example <code>isValue(new LuaTable())</code> is
	  /// <code>true</code>, but <code>isValue(new Object[] { })</code> is
	  /// <code>false</code> because Java arrays are not a representation of
	  /// any Lua value.
	  /// </para>
	  /// <para>
	  /// PUC-Rio Lua provides no
	  /// counterpart for this method because in their implementation it is
	  /// impossible to get non Lua values on the stack, whereas in Jill it
	  /// is common to mix Lua values with ordinary, non Lua, Java objects.
	  /// </para> </summary>
	  /// <param name="o">  the Object to test. </param>
	  /// <returns> true if and if it represents a Lua value. </returns>
	  public static bool isValue(object o)
	  {
		return o == NIL || o is bool? || o is string || o is double? || o is LuaFunction || o is LuaJavaCallback || o is LuaTable || o is LuaUserdata;
	  }

	  /// <summary>
	  /// Compares two Lua values according to the semantics of Lua's
	  /// <code>&lt;</code> operator, so may call metamethods. </summary>
	  /// <param name="o1">  the left-hand operand. </param>
	  /// <param name="o2">  the right-hand operand. </param>
	  /// <returns> true when <code>o1 < o2</code>. </returns>
	  public bool lessThan(object o1, object o2)
	  {
		Slot a = new Slot(o1);
		Slot b = new Slot(o2);
		return vmLessthan(a, b);
	  }

	  /// <summary>
	  /// <para>
	  /// Loads a Lua chunk in binary or source form.
	  /// Comparable to C's lua_load.  If the chunk is determined to be
	  /// binary then it is loaded directly.  Otherwise the chunk is assumed
	  /// to be a Lua source chunk and compilation is required first; the
	  /// <code>InputStream</code> is used to create a <code>Reader</code>
	  /// using the UTF-8 encoding
	  /// (using a second argument of <code>"UTF-8"</code> to the
	  /// {@link java.io.InputStreamReader#InputStreamReader(java.io.InputStream,
	  /// java.lang.String)}
	  /// constructor) and the Lua source is compiled.
	  /// </para>
	  /// <para>
	  /// If successful, The compiled chunk, a Lua function, is pushed onto
	  /// the stack and a zero status code is returned.  Otherwise a non-zero
	  /// status code is returned to indicate an error and the error message
	  /// is pushed onto the stack.
	  /// </para> </summary>
	  /// <param name="in">         The binary chunk as an InputStream, for example from
	  ///                   <seealso cref="Class#getResourceAsStream"/>. </param>
	  /// <param name="chunkname">  The name of the chunk. </param>
	  /// <returns>           A status code. </returns>
	  public int load(InputStream in_, string chunkname)
	  {
		push(new LuaInternal(in_, chunkname));
		return pcall(0, 1, null);
	  }

	  /// <summary>
	  /// Loads a Lua chunk in source form.
	  /// Comparable to C's lua_load.  This method takes a {@link
	  /// java.io.Reader} parameter,
	  /// and is normally used to load Lua chunks in source form.
	  /// However, it if the input looks like it is the output from Lua's
	  /// <code>string.dump</code> function then it will be processed as a
	  /// binary chunk.
	  /// In every other respect this method is just like {@link
	  /// #load(InputStream, String)}. </summary>
	  /// <param name="in">         The source chunk as a Reader, for example from
	  ///                   <code>java.io.InputStreamReader(Class.getResourceAsStream())</code>. </param>
	  /// <param name="chunkname">  The name of the chunk. </param>
	  /// <returns>           A status code. </returns>
	  /// <seealso cref= java.io.InputStreamReader </seealso>
	  public int load(Reader @in, string chunkname)
	  {
		push(new LuaInternal(@in, chunkname));
		return pcall(0, 1, null);
	  }

	  /// <summary>
	  /// Slowly get the next key from a table.  Unlike most other functions
	  /// in the API this one uses the stack.  The top-of-stack is popped and
	  /// used to find the next key in the table at the position specified by
	  /// index.  If there is a next key then the key and its value are
	  /// pushed onto the stack and <code>true</code> is returned.
	  /// Otherwise (the end of the table has been reached)
	  /// <code>false</code> is returned. </summary>
	  /// <param name="idx">  stack index of table. </param>
	  /// <returns>  true if and only if there are more keys in the table. </returns>
	  /// @deprecated Use <seealso cref="#tableKeys"/> enumeration protocol instead. 
	  public bool next(int idx)
	  {
		object o = value(idx);
		// :todo: api check
		LuaTable t = (LuaTable)o;
		object key = value(-1);
		pop(1);
        Enumeration e = t.keys();
		if (key == NIL)
		{
		  if (e.hasMoreElements())
		  {
			key = e.nextElement();
			push(key);
			push(t.getlua(key));
			return true;
		  }
		  return false;
		}
		while (e.hasMoreElements())
		{
		  object k = e.nextElement();
		  if (k.Equals(key))
		  {
			if (e.hasMoreElements())
			{
			  key = e.nextElement();
			  push(key);
			  push(t.getlua(key));
			  return true;
			}
			return false;
		  }
		}
		// protocol error which we could potentially diagnose.
		return false;
	  }

	  /// <summary>
	  /// Creates a new empty table and returns it. </summary>
	  /// <returns> a fresh table. </returns>
	  /// <seealso cref= #createTable </seealso>
	  public LuaTable newTable()
	  {
		return new LuaTable();
	  }

	  /// <summary>
	  /// Creates a new Lua thread and returns it. </summary>
	  /// <returns> a new Lua thread. </returns>
	  public Lua newThread()
	  {
		return new Lua(this);
	  }

	  /// <summary>
	  /// Wraps an arbitrary Java reference in a Lua userdata and returns it. </summary>
	  /// <param name="ref">  the Java reference to wrap. </param>
	  /// <returns> the new LuaUserdata. </returns>
	  public LuaUserdata newUserdata(object @ref)
	  {
		return new LuaUserdata(@ref);
	  }

	  /// <summary>
	  /// Return the <em>length</em> of a Lua value.  For strings this is
	  /// the string length; for tables, this is result of the <code>#</code>
	  /// operator; for other values it is 0. </summary>
	  /// <param name="o">  a Lua value. </param>
	  /// <returns> its length. </returns>
	  public int objLen(object o) //static
	  {
		if (o is string)
		{
		  string s = (string)o;
		  return s.Length;
		}
		if (o is LuaTable)
		{
		  LuaTable t = (LuaTable)o;
		  return t.getn();
		}
		if (o is double?)
		{
		  return vmTostring(o).Length;
		}
		return 0;
	  }


	  /// <summary>
	  /// <para>
	  /// Protected <seealso cref="#call"/>.  <var>nargs</var> and
	  /// <var>nresults</var> have the same meaning as in <seealso cref="#call"/>.
	  /// If there are no errors during the call, this method behaves as
	  /// <seealso cref="#call"/>.  Any errors are caught, the error object (usually
	  /// a message) is pushed onto the stack, and a non-zero error code is
	  /// returned.
	  /// </para>
	  /// <para>
	  /// If <var>er</var> is <code>null</code> then the error object that is
	  /// on the stack is the original error object.  Otherwise
	  /// <var>ef</var> specifies an <em>error handling function</em> which
	  /// is called when the original error is generated; its return value
	  /// becomes the error object left on the stack by <code>pcall</code>.
	  /// </para> </summary>
	  /// <param name="nargs">     number of arguments. </param>
	  /// <param name="nresults">  number of result required. </param>
	  /// <param name="ef">        error function to call in case of error. </param>
	  /// <returns> 0 if successful, else a non-zero error code. </returns>
	  public int pcall(int nargs, int nresults, object ef)
	  {
		apiChecknelems(nargs + 1);
		int restoreStack = stackSize - (nargs + 1);
		// Most of this code comes from luaD_pcall
		int restoreCi = civ.getSize();
		int oldnCcalls = nCcalls;
		object old_errfunc = errfunc;
		errfunc = ef;
		bool old_allowhook = allowhook;
		int errorStatus = 0;
		try
		{
		  call(nargs, nresults);
		}
		catch (LuaError e)
		{
		  fClose(restoreStack); // close eventual pending closures
		  dSeterrorobj(e.errorStatus, restoreStack);
		  nCcalls = oldnCcalls;
		  civ.setSize(restoreCi);
          CallInfo ci = __ci();
		  @base = ci.@base();
		  savedpc = ci.savedpc();
		  allowhook = old_allowhook;
		  errorStatus = e.errorStatus;
		}
		catch (System.OutOfMemoryException)
		{
		  fClose(restoreStack); // close eventual pending closures
		  dSeterrorobj(ERRMEM, restoreStack);
		  nCcalls = oldnCcalls;
		  civ.setSize(restoreCi);
		  CallInfo ci = __ci();
		  @base = ci.@base();
		  savedpc = ci.savedpc();
		  allowhook = old_allowhook;
		  errorStatus = ERRMEM;
		}
		errfunc = old_errfunc;
		return errorStatus;
	  }

	  /// <summary>
	  /// Removes (and discards) the top-most <var>n</var> elements from the stack. </summary>
	  /// <param name="n">  the number of elements to remove. </param>
	  public void pop(int n)
	  {
		if (n < 0)
		{
		  throw new System.ArgumentException();
		}
		stacksetsize(stackSize - n);
	  }

	  /// <summary>
	  /// Pushes a value onto the stack in preparation for calling a
	  /// function (or returning from one).  See <seealso cref="#call"/> for
	  /// the protocol to be used for calling functions.  See {@link
	  /// #pushNumber} for pushing numbers, and <seealso cref="#pushValue"/> for
	  /// pushing a value that is already on the stack. </summary>
	  /// <param name="o">  the Lua value to push. </param>
	  public void push(object o)
	  {
		// see also a private overloaded version of this for Slot.
		stackAdd(o);
	  }

	  /// <summary>
	  /// Push boolean onto the stack. </summary>
	  /// <param name="b">  the boolean to push. </param>
	  public void pushBoolean(bool b)
	  {
		push(valueOfBoolean(b));
	  }

	  /// <summary>
	  /// Push literal string onto the stack. </summary>
	  /// <param name="s">  the string to push. </param>
	  public void pushLiteral(string s)
	  {
		push(s);
	  }

	  /// <summary>
	  /// Push nil onto the stack. </summary>
	  public void pushNil()
	  {
		push(NIL);
	  }

	  /// <summary>
	  /// Pushes a number onto the stack.  See also <seealso cref="#push"/>. </summary>
	  /// <param name="d">  the number to push. </param>
	  public void pushNumber(double d)
	  {
		// :todo: optimise to avoid creating Double instance
		push(new double?(d));
	  }

	  /// <summary>
	  /// Push string onto the stack. </summary>
	  /// <param name="s">  the string to push. </param>
	  public void pushString(string s)
	  {
		push(s);
	  }

	  /// <summary>
	  /// Copies a stack element onto the top of the stack.
	  /// Equivalent to <code>L.push(L.value(idx))</code>. </summary>
	  /// <param name="idx">  stack index of value to push. </param>
	  public void pushValue(int idx)
	  {
		// :todo: optimised to avoid creating Double instance
		push(value(idx));
	  }

	  /// <summary>
	  /// Implements equality without metamethods. </summary>
	  /// <param name="o1">  the first Lua value to compare. </param>
	  /// <param name="o2">  the other Lua value. </param>
	  /// <returns>  true if and only if they compare equal. </returns>
	  public bool rawEqual(object o1, object o2) //static
	  {
		return oRawequal(o1, o2);
	  }

	  /// <summary>
	  /// Gets an element from a table, without using metamethods. </summary>
	  /// <param name="t">  The table to access. </param>
	  /// <param name="k">  The index (key) into the table. </param>
	  /// <returns> The value at the specified index. </returns>
	  public object rawGet(object t, object k) //static
	  {
		LuaTable table = (LuaTable)t;
		return table.getlua(k);
	  }

	  /// <summary>
	  /// Gets an element from an array, without using metamethods. </summary>
	  /// <param name="t">  the array (table). </param>
	  /// <param name="i">  the index of the element to retrieve. </param>
	  /// <returns>  the value at the specified index. </returns>
	  public object rawGetI(object t, int i) //static
	  {
		LuaTable table = (LuaTable)t;
		return table.getnum(i);
	  }

	  /// <summary>
	  /// Sets an element in a table, without using metamethods. </summary>
	  /// <param name="t">  The table to modify. </param>
	  /// <param name="k">  The index into the table. </param>
	  /// <param name="v">  The new value to be stored at index <var>k</var>. </param>
	  public void rawSet(object t, object k, object v)
	  {
		LuaTable table = (LuaTable)t;
		table.putlua(this, k, v);
	  }

	  /// <summary>
	  /// Sets an element in an array, without using metamethods. </summary>
	  /// <param name="t">  the array (table). </param>
	  /// <param name="i">  the index of the element to set. </param>
	  /// <param name="v">  the new value to be stored at index <var>i</var>. </param>
	  public void rawSetI(object t, int i, object v)
	  {
		apiCheck(t is LuaTable);
		LuaTable h = (LuaTable)t;
		h.putnum(i, v);
	  }

	  /// <summary>
	  /// Register a <seealso cref="LuaJavaCallback"/> as the new value of the global
	  /// <var>name</var>. </summary>
	  /// <param name="name">  the name of the global. </param>
	  /// <param name="f">     the LuaJavaCallback to register. </param>
	  public void register(string name, LuaJavaCallback f)
	  {
		setGlobal(name, f);
	  }

	  /// <summary>
	  /// Starts and resumes a Lua thread.  Threads can be created using
	  /// <seealso cref="#newThread"/>.  Once a thread has begun executing it will
	  /// run until it either completes (with error or normally) or has been
	  /// suspended by invoking <seealso cref="#yield"/>. </summary>
	  /// <param name="narg">  Number of values to pass to thread. </param>
	  /// <returns> Lua.YIELD, 0, or an error code. </returns>
	  public int resume(int narg)
	  {
		if (status_Renamed != YIELD)
		{
		  if (status_Renamed != 0)
		  {
			return resume_error("cannot resume dead coroutine");
		  }
		  else if (civ.getSize() != 1)
		  {
			return resume_error("cannot resume non-suspended coroutine");
		  }
		}
		// assert errfunc == 0 && nCcalls == 0;
		int errorStatus = 0;
protectBreak:
		try
		{
		  // This block is equivalent to resume from ldo.c
		  int firstArg = stackSize - narg;
		  if (status_Renamed == 0) // start coroutine?
		  {
			// assert civ.size() == 1 && firstArg > base);
			if (vmPrecall(firstArg - 1, MULTRET) != PCRLUA)
			{
			  goto protectBreak;
			}
		  }
		  else // resuming from previous yield
		  {
			// assert status == YIELD;
			status_Renamed = 0;
			if (!isLua(__ci())) // 'common' yield
			{
			  // finish interrupted execution of 'OP_CALL'
			  // assert ...
			  if (vmPoscall(firstArg)) // complete it...
			  {
				stacksetsize(__ci().top()); // and correct top
			  }
			}
			else // yielded inside a hook: just continue its execution
			{
			  @base = __ci().@base();
			}
		  }
		  vmExecute(civ.getSize() - 1);
		}

		catch (LuaError e)
		{
		  status_Renamed = e.errorStatus; // mark thread as 'dead'
		  dSeterrorobj(e.errorStatus, stackSize);
		  __ci().Top = stackSize;
		}
		return status_Renamed;
	  }

	  /// <summary>
	  /// Set the environment for a function, thread, or userdata. </summary>
	  /// <param name="o">      Object whose environment will be set. </param>
	  /// <param name="table">  Environment table to use. </param>
	  /// <returns> true if the object had its environment set, false otherwise. </returns>
	  public bool setFenv(object o, object table)
	  {
		// :todo: consider implementing common env interface for
		// LuaFunction, LuaJavaCallback, LuaUserdata, Lua.  One cast to an
		// interface and an interface method call may be shorter
		// than this mess.
		LuaTable t = (LuaTable)table;

		if (o is LuaFunction)
		{
		  LuaFunction f = (LuaFunction)o;
		  f.setEnv(t);
		  return true;
		}
		if (o is LuaJavaCallback)
		{
		  LuaJavaCallback f = (LuaJavaCallback)o;
		  // :todo: implement this case.
		  return false;
		}
		if (o is LuaUserdata)
		{
		  LuaUserdata u = (LuaUserdata)o;
		  u.setEnv(t);
		  return true;
		}
		if (o is Lua)
		{
		  Lua l = (Lua)o;
		  l.global = t;
		  return true;
		}
		return false;
	  }

	  /// <summary>
	  /// Set a field in a Lua value. </summary>
	  /// <param name="t">     Lua value of which to set a field. </param>
	  /// <param name="name">  Name of field to set. </param>
	  /// <param name="v">     new Lua value for field. </param>
	  public void setField(object t, string name, object v)
	  {
		Slot s = new Slot(name);
		vmSettable(t, s, v);
	  }

	  /// <summary>
	  /// Sets the metatable for a Lua value. </summary>
	  /// <param name="o">   Lua value of which to set metatable. </param>
	  /// <param name="mt">  The new metatable. </param>
	  public void setMetatable(object o, object mt)
	  {
		if (isNil(mt))
		{
		  mt = null;
		}
		else
		{
		  apiCheck(mt is LuaTable);
		}
		LuaTable mtt = (LuaTable)mt;
		if (o is LuaTable)
		{
		  LuaTable t = (LuaTable)o;
		  t.setMetatable(mtt);
		}
		else if (o is LuaUserdata)
		{
		  LuaUserdata u = (LuaUserdata)o;
		  u.setMetatable(mtt);
		}
		else
		{
		  metatable[type(o)] = mtt;
		}
	  }

	  /// <summary>
	  /// Set a global variable. </summary>
	  /// <param name="name">   name of the global variable to set. </param>
	  /// <param name="value">  desired new value for the variable. </param>
	  public void setGlobal(string name, object value)
	  {
		Slot s = new Slot(name);
		vmSettable(global, s, value);
	  }

	  /// <summary>
	  /// Does the equivalent of <code>t[k] = v</code>. </summary>
	  /// <param name="t">  the table to modify. </param>
	  /// <param name="k">  the index to modify. </param>
	  /// <param name="v">  the new value at index <var>k</var>. </param>
	  public void setTable(object t, object k, object v)
	  {
		Slot s = new Slot(k);
		vmSettable(t, s, v);
	  }


	  /// <summary>
	  /// Status of a Lua thread. </summary>
	  /// <returns> 0, an error code, or Lua.YIELD. </returns>
	  public int status()
	  {
		return status_Renamed;
	  }

	  /// <summary>
	  /// Returns an <seealso cref="java.util.Enumeration"/> for the keys of a table. </summary>
	  /// <param name="t">  a Lua table. </param>
	  /// <returns> an Enumeration object. </returns>
	  public Enumeration tableKeys(object t)
	  {
		if (!(t is LuaTable))
		{
		  error("table required");
		  // NOTREACHED
		}
        return ((LuaTable)t).keys();
	  }

	  /// <summary>
	  /// Convert to boolean. </summary>
	  /// <param name="o">  Lua value to convert. </param>
	  /// <returns>  the resulting primitive boolean. </returns>
	  public bool toBoolean(object o)
	  {
		return !(o == NIL || false.Equals(o));
	  }

	  /// <summary>
	  /// Convert to integer and return it.  Returns 0 if cannot be
	  /// converted. </summary>
	  /// <param name="o">  Lua value to convert. </param>
	  /// <returns>  the resulting int. </returns>
	  public int toInteger(object o)
	  {
		return (int)toNumber(o);
	  }

	  /// <summary>
	  /// Convert to number and return it.  Returns 0 if cannot be
	  /// converted. </summary>
	  /// <param name="o">  Lua value to convert. </param>
	  /// <returns>  The resulting number. </returns>
	  public double toNumber(object o)
	  {
		SPARE_SLOT.Object = o;
		if (tonumber(SPARE_SLOT, NUMOP))
		{
		  return NUMOP[0];
		}
		return 0;
	  }

	  /// <summary>
	  /// Convert to string and return it.  If value cannot be converted then
	  /// <code>null</code> is returned.  Note that unlike
	  /// <code>lua_tostring</code> this
	  /// does not modify the Lua value. </summary>
	  /// <param name="o">  Lua value to convert. </param>
	  /// <returns>  The resulting string. </returns>
	  public string toString(object o)
	  {
		return vmTostring(o);
	  }

	  /// <summary>
	  /// Convert to Lua thread and return it or <code>null</code>. </summary>
	  /// <param name="o">  Lua value to convert. </param>
	  /// <returns>  The resulting Lua instance. </returns>
	  public Lua toThread(object o)
	  {
		if (!(o is Lua))
		{
		  return null;
		}
		return (Lua)o;
	  }

	  /// <summary>
	  /// Convert to userdata or <code>null</code>.  If value is a {@link
	  /// LuaUserdata} then it is returned, otherwise, <code>null</code> is
	  /// returned. </summary>
	  /// <param name="o">  Lua value. </param>
	  /// <returns>  value as userdata or <code>null</code>. </returns>
	  public LuaUserdata toUserdata(object o)
	  {
		if (o is LuaUserdata)
		{
		  return (LuaUserdata)o;
		}
		return null;
	  }

	  /// <summary>
	  /// Type of the Lua value at the specified stack index. </summary>
	  /// <param name="idx">  stack index to type. </param>
	  /// <returns>  the type, or <seealso cref="#TNONE"/> if there is no value at <var>idx</var> </returns>
	  public int type(int idx)
	  {
		idx = absIndex(idx);
		if (idx < 0)
		{
		  return TNONE;
		}
		return type(stack[idx]);
	  }

	  private int type(Slot s)
	  {
		if (s.r == NUMBER)
		{
		  return TNUMBER;
		}
		return type(s.r);
	  }

	  /// <summary>
	  /// Type of a Lua value. </summary>
	  /// <param name="o">  the Lua value whose type to return. </param>
	  /// <returns>  the Lua type from an enumeration. </returns>
	  public static int type(object o)
	  {
		if (o == NIL)
		{
		  return TNIL;
		}
		else if (o is double?)
		{
		  return TNUMBER;
		}
		else if (o is bool?)
		{
		  return TBOOLEAN;
		}
		else if (o is string)
		{
		  return TSTRING;
		}
		else if (o is LuaTable)
		{
		  return TTABLE;
		}
		else if (o is LuaFunction || o is LuaJavaCallback)
		{
		  return TFUNCTION;
		}
		else if (o is LuaUserdata)
		{
		  return TUSERDATA;
		}
		else if (o is Lua)
		{
		  return TTHREAD;
		}
		return TNONE;
	  }

	  /// <summary>
	  /// Name of type. </summary>
	  /// <param name="type">  a Lua type from, for example, <seealso cref="#type"/>. </param>
	  /// <returns>  the type's name. </returns>
	  public static string typeName(int type)
	  {
		if (TNONE == type)
		{
		  return "no value";
		}
		return TYPENAME[type];
	  }

	  /// <summary>
	  /// Gets a value from the stack.
	  /// If <var>idx</var> is positive and exceeds
	  /// the size of the stack, <seealso cref="#NIL"/> is returned. </summary>
	  /// <param name="idx">  the stack index of the value to retrieve. </param>
	  /// <returns>  the Lua value from the stack. </returns>
	  public object value(int idx)
	  {
		idx = absIndex(idx);
		if (idx < 0)
		{
		  return NIL;
		}
		if (D)
		{
			Console.Error.WriteLine("value:" + idx);
		}
		return stack[idx].asObject();
	  }

	  /// <summary>
	  /// Converts primitive boolean into a Lua value. </summary>
	  /// <param name="b">  the boolean to convert. </param>
	  /// <returns>  the resulting Lua value. </returns>
	  public static object valueOfBoolean(bool b)
	  {
		 // If CLDC 1.1 had
		 // <code>java.lang.Boolean.valueOf(boolean);</code> then I probably
		 // wouldn't have written this.  This does have a small advantage:
		 // code that uses this method does not need to assume that Lua booleans in
		 // Jill are represented using Java.lang.Boolean.
		if (b)
		{
		  return true;
		}
		else
		{
		  return false;
		}
	  }

	  /// <summary>
	  /// Converts primitive number into a Lua value. </summary>
	  /// <param name="d">  the number to convert. </param>
	  /// <returns>  the resulting Lua value. </returns>
	  public static object valueOfNumber(double d)
	  {
		// :todo: consider interning "common" numbers, like 0, 1, -1, etc.
		return new double?(d);
	  }

	  /// <summary>
	  /// Exchange values between different threads. </summary>
	  /// <param name="to">  destination Lua thread. </param>
	  /// <param name="n">   numbers of stack items to move. </param>
	  public void xmove(Lua to, int n)
	  {
		if (this == to)
		{
		  return;
		}
		apiChecknelems(n);
		// L.apiCheck(from.G() == to.G());
		for (int i = 0; i < n; ++i)
		{
		  to.push(value(-n + i));
		}
		pop(n);
	  }

	  /// <summary>
	  /// Yields a thread.  Should only be called as the return expression
	  /// of a Lua Java function: <code>return L.yield(nresults);</code>.
	  /// A <seealso cref="RuntimeException"/> can also be thrown to yield.  If the
	  /// Java code that is executing throws an instance of {@link
	  /// RuntimeException} (direct or indirect) then this causes the Lua 
	  /// thread to be suspended, as if <code>L.yield(0);</code> had been
	  /// executed, and the exception is re-thrown to the code that invoked
	  /// <seealso cref="#resume"/>. </summary>
	  /// <param name="nresults">  Number of results to return to <seealso cref="#resume"/>. </param>
	  /// <returns>  a secret value. </returns>
	  public int @yield(int nresults)
	  {
		if (nCcalls > 0)
		{
		  gRunerror("attempt to yield across metamethod/Java-call boundary");
		}
		@base = stackSize - nresults; // protect stack slots below
		status_Renamed = YIELD;
		return -1;
	  }

	  // Miscellaneous private functions.

	  /// <summary>
	  /// Convert from Java API stack index to absolute index. </summary>
	  /// <returns> an index into <code>this.stack</code> or -1 if out of range. </returns>
	  private int absIndex(int idx)
	  {
		int s = stackSize;

		if (idx == 0)
		{
		  return -1;
		}
		if (idx > 0)
		{
		  if (idx + @base > s)
		  {
			return -1;
		  }
		  return @base + idx - 1;
		}
		// idx < 0
		if (s + idx < @base)
		{
		  return -1;
		}
		return s + idx;
	  }

	  /// <summary>
	  /// As <seealso cref="#absIndex"/> but does not return -1 for out of range
	  /// indexes.  Essential for <seealso cref="#insert"/> because an index equal
	  /// to the size of the stack is valid for that call.
	  /// </summary>
	  private int absIndexUnclamped(int idx)
	  {
		if (idx == 0)
		{
		  return -1;
		}
		if (idx > 0)
		{
		  return @base + idx - 1;
		}
		// idx < 0
		return stackSize + idx;
	  }


	  //////////////////////////////////////////////////////////////////////
	  // Auxiliary API

	  // :todo: consider placing in separate class (or macroised) so that we
	  // can change its definition (to remove the check for example).
	  private void apiCheck(bool cond)
	  {
		if (!cond)
		{
		  throw new System.ArgumentException();
		}
	  }

	  private void apiChecknelems(int n)
	  {
		apiCheck(n <= stackSize - @base);
	  }

	  /// <summary>
	  /// Checks a general condition and raises error if false. </summary>
	  /// <param name="cond">      the (evaluated) condition to check. </param>
	  /// <param name="numarg">    argument index. </param>
	  /// <param name="extramsg">  extra error message to append. </param>
	  public void argCheck(bool cond, int numarg, string extramsg)
	  {
		if (cond)
		{
		  return;
		}
		argError(numarg, extramsg);
	  }

	  /// <summary>
	  /// Raise a general error for an argument. </summary>
	  /// <param name="narg">      argument index. </param>
	  /// <param name="extramsg">  extra message string to append. </param>
	  /// <returns> never (used idiomatically in <code>return argError(...)</code>) </returns>
	  public int argError(int narg, string extramsg)
	  {
		// :todo: use debug API as per PUC-Rio
		/*if (true)*/
	  {
		  return error("bad argument " + narg + " (" + extramsg + ")");
		}
		/*return 0;*/
	  }

	  /// <summary>
	  /// Calls a metamethod.  Pushes 1 result onto stack if method called. </summary>
	  /// <param name="obj">    stack index of object whose metamethod to call </param>
	  /// <param name="event">  metamethod (event) name. </param>
	  /// <returns>  true if and only if metamethod was found and called. </returns>
	  public bool callMeta(int obj, string @event)
	  {
		object o = value(obj);
		object ev = getMetafield(o, @event);
		if (ev == NIL)
		{
		  return false;
		}
		push(ev);
		push(o);
		call(1, 1);
		return true;
	  }

	  /// <summary>
	  /// Checks that an argument is present (can be anything).
	  /// Raises error if not. </summary>
	  /// <param name="narg">  argument index. </param>
	  public void checkAny(int narg)
	  {
		if (type(narg) == TNONE)
		{
		  argError(narg, "value expected");
		}
	  }

	  /// <summary>
	  /// Checks is a number and returns it as an integer.  Raises error if
	  /// not a number. </summary>
	  /// <param name="narg">  argument index. </param>
	  /// <returns>  the argument as an int. </returns>
	  public int checkInt(int narg)
	  {
		return (int)checkNumber(narg);
	  }

	  /// <summary>
	  /// Checks is a number.  Raises error if not a number. </summary>
	  /// <param name="narg">  argument index. </param>
	  /// <returns>  the argument as a double. </returns>
	  public double checkNumber(int narg)
	  {
		object o = value(narg);
		double d = toNumber(o);
		if (d == 0 && !isNumber(o))
		{
		  tagError(narg, TNUMBER);
		}
		return d;
	  }

	  /// <summary>
	  /// Checks that an optional string argument is an element from a set of
	  /// strings.  Raises error if not. </summary>
	  /// <param name="narg">  argument index. </param>
	  /// <param name="def">   default string to use if argument not present. </param>
	  /// <param name="lst">   the set of strings to match against. </param>
	  /// <returns> an index into <var>lst</var> specifying the matching string. </returns>
	  public int checkOption(int narg, string def, string[] lst)
	  {
		string name;

		if (def == null)
		{
		  name = checkString(narg);
		}
		else
		{
		  name = optString(narg, def);
		}
		for (int i = 0; i < lst.Length; ++i)
		{
		  if (lst[i].Equals(name))
		  {
			return i;
		  }
		}
		return argError(narg, "invalid option '" + name + "'");
	  }

	  /// <summary>
	  /// Checks argument is a string and returns it.  Raises error if not a
	  /// string. </summary>
	  /// <param name="narg">  argument index. </param>
	  /// <returns>  the argument as a string. </returns>
	  public string checkString(int narg)
	  {
		string s = toString(value(narg));
		if (s == null)
		{
		  tagError(narg, TSTRING);
		}
		return s;
	  }

	  /// <summary>
	  /// Checks the type of an argument, raises error if not matching. </summary>
	  /// <param name="narg">  argument index. </param>
	  /// <param name="t">     typecode (from <seealso cref="#type"/> for example). </param>
	  public void checkType(int narg, int t)
	  {
		if (type(narg) != t)
		{
		  tagError(narg, t);
		}
	  }

	  /// <summary>
	  /// Loads and runs the given string. </summary>
	  /// <param name="s">  the string to run. </param>
	  /// <returns>  a status code, as per <seealso cref="#load"/>. </returns>
	  public int doString(string s)
	  {
		int status = load(Lua.stringReader(s), s);
		if (status == 0)
		{
		  status = pcall(0, MULTRET, null);
		}
		return status;
	  }

	  private int errfile(string what, string fname, Exception e)
	  {
		push("cannot " + what + " " + fname + ": " + e.ToString());
		return ERRFILE;
	  }

	  /// <summary>
	  /// Equivalent to luaL_findtable.  Instead of the table being passed on
	  /// the stack, it is passed as the argument <var>t</var>.
	  /// Likes its PUC-Rio equivalent however, this method leaves a table on
	  /// the Lua stack.
	  /// </summary>
	  internal string findTable(LuaTable t, string fname, int szhint)
	  {
		int e = 0;
		int i = 0;
		do
		{
		  e = fname.IndexOf('.', i);
		  string part;
		  if (e < 0)
		  {
			part = fname.Substring(i);
		  }
		  else
		  {
			part = fname.Substring(i, e - i);
		  }
		  object v = rawGet(t, part);
		  if (isNil(v)) // no such field?
		  {
			v = createTable(0, (e >= 0) ? 1 : szhint); // new table for field
			setTable(t, part, v);
		  }
		  else if (!isTable(v)) // field has a non-table value?
		  {
			return part;
		  }
		  t = (LuaTable)v;
		  i = e + 1;
		} while (e >= 0);
		push(t);
		return null;
	  }

	  /// <summary>
	  /// Get a field (event) from an Lua value's metatable.  Returns Lua
	  /// <code>nil</code> if there is either no metatable or no field. </summary>
	  /// <param name="o">           Lua value to get metafield for. </param>
	  /// <param name="event">       name of metafield (event). </param>
	  /// <returns>            the field from the metatable, or nil. </returns>
	  public object getMetafield(object o, string @event)
	  {
		LuaTable mt = getMetatable(o);
		if (mt == null)
		{
		  return NIL;
		}
		return mt.getlua(@event);
	  }

	  internal bool isNoneOrNil(int narg)
	  {
		return type(narg) <= TNIL;
	  }

	  /// <summary>
	  /// Loads a Lua chunk from a file.  The <var>filename</var> argument is
	  /// used in a call to <seealso cref="Class#getResourceAsStream"/> where
	  /// <code>this</code> is the <seealso cref="Lua"/> instance, thus relative
	  /// pathnames will be relative to the location of the
	  /// <code>Lua.class</code> file.  Pushes compiled chunk, or error
	  /// message, onto stack. </summary>
	  /// <param name="filename">  location of file. </param>
	  /// <returns> status code, as per <seealso cref="#load"/>. </returns>
	  public int loadFile(string filename)
	  {
		if (filename == null)
		{
		  throw new System.NullReferenceException();
		}
        InputStream @in = SystemUtil.getResourceAsStream(filename);
		if (@in == null)
		{
		  return errfile("open", filename, new IOException());
		}
		int status = 0;
		try
		{
		  @in.mark(1);
		  int c = @in.read();
		  if (c == '#') // Unix exec. file?
		  {
			// :todo: handle this case
		  }
		  @in.reset();
		  status = load(@in, "@" + filename);
		}
		catch (IOException e)
		{
		  return errfile("read", filename, e);
		}
		return status;
	  }

	  /// <summary>
	  /// Loads a Lua chunk from a string.  Pushes compiled chunk, or error
	  /// message, onto stack. </summary>
	  /// <param name="s">           the string to load. </param>
	  /// <param name="chunkname">   the name of the chunk. </param>
	  /// <returns> status code, as per <seealso cref="#load"/>. </returns>
	  public int loadString(string s, string chunkname)
	  {
		return load(stringReader(s), chunkname);
	  }

	  /// <summary>
	  /// Get optional integer argument.  Raises error if non-number
	  /// supplied. </summary>
	  /// <param name="narg">  argument index. </param>
	  /// <param name="def">   default value for integer. </param>
	  /// <returns> an int. </returns>
	  public int optInt(int narg, int def)
	  {
		if (isNoneOrNil(narg))
		{
		  return def;
		}
		return checkInt(narg);
	  }

	  /// <summary>
	  /// Get optional number argument.  Raises error if non-number supplied. </summary>
	  /// <param name="narg">  argument index. </param>
	  /// <param name="def">   default value for number. </param>
	  /// <returns> a double. </returns>
	  public double optNumber(int narg, double def)
	  {
		if (isNoneOrNil(narg))
		{
		  return def;
		}
		return checkNumber(narg);
	  }

	  /// <summary>
	  /// Get optional string argument.  Raises error if non-string supplied. </summary>
	  /// <param name="narg">  argument index. </param>
	  /// <param name="def">   default value for string. </param>
	  /// <returns> a string. </returns>
	  public string optString(int narg, string def)
	  {
		if (isNoneOrNil(narg))
		{
		  return def;
		}
		return checkString(narg);
	  }

	  /// <summary>
	  /// Creates a table in the global namespace and registers it as a loaded
	  /// module. </summary>
	  /// <returns> the new table </returns>
	  public LuaTable register(string name)
	  {
		findTable(Registry, LOADED, 1);
		object loaded = value(-1);
		pop(1);
		object t = getField(loaded, name);
		if (!isTable(t)) // not found?
		{
		  // try global variable (and create one if it does not exist)
		  if (findTable(Globals, name, 0) != null)
		  {
			error("name conflict for module '" + name + "'");
		  }
		  t = value(-1);
		  pop(1);
		  setField(loaded, name, t); // _LOADED[name] = new table
		}
		return (LuaTable)t;
	  }

	  private void tagError(int narg, int tag)
	  {
		typerror(narg, typeName(tag));
	  }

	  /// <summary>
	  /// Name of type of value at <var>idx</var>. </summary>
	  /// <param name="idx">  stack index. </param>
	  /// <returns>  the name of the value's type. </returns>
	  public string typeNameOfIndex(int idx)
	  {
		return TYPENAME[type(idx)];
	  }

	  /// <summary>
	  /// Declare type error in argument. </summary>
	  /// <param name="narg">   Index of argument. </param>
	  /// <param name="tname">  Name of type expected. </param>
	  public void typerror(int narg, string tname)
	  {
		argError(narg, tname + " expected, got " + typeNameOfIndex(narg));
	  }

	  /// <summary>
	  /// Return string identifying current position of the control at level
	  /// <var>level</var>. </summary>
	  /// <param name="level">  specifies the call-stack level. </param>
	  /// <returns> a description for that level. </returns>
	  public string @where(int level)
	  {
		Debug ar = getStack(level); // check function at level
		if (ar != null)
		{
		  getInfo("Sl", ar); // get info about it
		  if (ar.currentline() > 0) // is there info?
		  {
			return ar.shortsrc() + ":" + ar.currentline() + ": ";
		  }
		}
		return ""; // else, no information available...
	  }

	  /// <summary>
	  /// Provide <seealso cref="java.io.Reader"/> interface over a <code>String</code>.
	  /// Equivalent of <seealso cref="java.io.StringReader#StringReader"/> from J2SE.
	  /// The ability to convert a <code>String</code> to a
	  /// <code>Reader</code> is required internally,
	  /// to provide the Lua function <code>loadstring</code>; exposed
	  /// externally as a convenience. </summary>
	  /// <param name="s">  the string from which to read. </param>
	  /// <returns> a <seealso cref="java.io.Reader"/> that reads successive chars from <var>s</var>. </returns>
	  public static Reader stringReader(string s)
	  {
		return new StringReader(s);
	  }

	  //////////////////////////////////////////////////////////////////////
	  // Debug

	  // Methods equivalent to debug API.  In PUC-Rio most of these are in
	  // ldebug.c

	  internal bool getInfo(string what, Debug ar)
	  {
		object f = null;
		CallInfo callinfo = null;
		// :todo: complete me
		if (ar.ici() > 0) // no tail call?
		{
		  callinfo = (CallInfo)civ.elementAt(ar.ici());
		  f = stack[callinfo.function()].r;
		  //# assert isFunction(f)
		}
		bool status = auxgetinfo(what, ar, f, callinfo);
		if (what.IndexOf('f') >= 0)
		{
		  if (f == null)
		  {
			push(NIL);
		  }
		  else
		  {
			push(f);
		  }
		}
		return status;
	  }

	  /// <summary>
	  /// Locates function activation at specified call level and returns a
	  /// <seealso cref="Debug"/>
	  /// record for it, or <code>null</code> if level is too high.
	  /// May become public. </summary>
	  /// <param name="level">  the call level. </param>
	  /// <returns> a <seealso cref="Debug"/> instance describing the activation record. </returns>
	  internal Debug getStack(int level)
	  {
		int ici; // Index of CallInfo

		for (ici = civ.getSize() - 1; level > 0 && ici > 0; --ici)
		{
		  CallInfo ci = (CallInfo)civ.elementAt(ici);
		  --level;
		  if (isLua(ci)) // Lua function?
		  {
			level -= ci.tailcalls(); // skip lost tail calls
		  }
		}
		if (level == 0 && ici > 0) // level found?
		{
		  return new Debug(ici);
		}
		else if (level < 0) // level is of a lost tail call?
		{
		  return new Debug(0);
		}
		return null;
	  }

	  /// <summary>
	  /// Sets the debug hook.
	  /// </summary>
	  public void setHook(Hook func, int mask, int count)
	  {
		if (func == null || mask == 0) // turn off hooks?
		{
		  mask = 0;
		  func = null;
		}
		hook = func;
		basehookcount = count;
		resethookcount();
		hookmask = mask;
	  }

	  /// <returns> true is okay, false otherwise (for example, error). </returns>
	  private bool auxgetinfo(string what, Debug ar, object f, CallInfo ci)
	  {
		bool status = true;
		if (f == null)
		{
		  // :todo: implement me
		  return status;
		}
		for (int i = 0; i < what.Length; ++i)
		{
		  switch (what[i])
		  {
			case 'S':
			  funcinfo(ar, f);
			  break;
			case 'l':
			  ar.Currentline = (ci != null) ? currentline(ci) : -1;
			  break;
			case 'f': // handled by getInfo
			  break;
			// :todo: more cases.
			default:
			  status = false;
		  break;
		  }
		}
		return status;
	  }

	  private int currentline(CallInfo ci)
	  {
		int pc = currentpc(ci);
		if (pc < 0)
		{
		  return -1; // only active Lua functions have current-line info
		}
		else
		{
		  object faso = stack[ci.function()].r;
		  LuaFunction f = (LuaFunction)faso;
		  return f.proto().getline(pc);
		}
	  }

	  private int currentpc(CallInfo ci)
	  {
		if (!isLua(ci)) // function is not a Lua function?
		{
		  return -1;
		}
		if (ci == __ci())
		{
		  ci.Savedpc = savedpc;
		}
		return pcRel(ci.savedpc());
	  }

	  private void funcinfo(Debug ar, object cl)
	  {
		if (cl is LuaJavaCallback)
		{
		  ar.Source = "=[Java]";
		  ar.Linedefined = -1;
		  ar.Lastlinedefined = -1;
		  ar.What = "Java";
		}
		else
		{
		  Proto p = ((LuaFunction)cl).proto();
		  ar.Source = p.source();
		  ar.Linedefined = p.linedefined();
		  ar.Lastlinedefined = p.lastlinedefined();
		  ar.What = ar.linedefined() == 0 ? "main" : "Lua";
		}
	  }

	  /// <summary>
	  /// Equivalent to macro isLua _and_ f_isLua from lstate.h. </summary>
	  private bool isLua(CallInfo callinfo)
	  {
		object f = stack[callinfo.function()].r;
		return f is LuaFunction;
	  }

	  private static int pcRel(int pc)
	  {
		return pc - 1;
	  }

	  //////////////////////////////////////////////////////////////////////
	  // Do

	  // Methods equivalent to the file ldo.c.  Prefixed with d.
	  // Some of these are in vm* instead.

	  /// <summary>
	  /// Equivalent to luaD_callhook.
	  /// </summary>
	  private void dCallhook(int @event, int line)
	  {
		Hook hook = this.hook;
		if (hook != null && allowhook)
		{
		  int top = stackSize;
		  int ci_top = __ci().top();
		  int ici = civ.getSize() - 1;
		  if (@event == HOOKTAILRET) // not supported yet
		  {
			ici = 0;
		  }
		  Debug ar = new Debug(ici);
		  ar.Event = @event;
		  ar.Currentline = line;
		  __ci().Top = stackSize;
		  allowhook = false; // cannot call hooks inside a hook
		  hook.luaHook(this, ar);
		  //# assert !allowhook
		  allowhook = true;
		  __ci().Top = ci_top;
		  stacksetsize(top);
		}
	  }

	  private const string MEMERRMSG = "not enough memory";

	  /// <summary>
	  /// Equivalent to luaD_seterrorobj.  It is valid for oldtop to be
	  /// equal to the current stack size (<code>stackSize</code>).
	  /// <seealso cref="#resume"/> uses this value for oldtop.
	  /// </summary>
	  private void dSeterrorobj(int errcode, int oldtop)
	  {
		object msg = objectAt(stackSize-1);
		if (stackSize == oldtop)
		{
		  stacksetsize(oldtop + 1);
		}
		switch (errcode)
		{
		  case ERRMEM:
			  if (D)
			  {
				  Console.Error.WriteLine("dSeterrorobj:" + oldtop);
			  }
			stack[oldtop].r = MEMERRMSG;
			break;

		  case ERRERR:
			  if (D)
			  {
				  Console.Error.WriteLine("dSeterrorobj:" + oldtop);
			  }
			stack[oldtop].r = "error in error handling";
			break;

		  case ERRFILE:
		  case ERRRUN:
		  case ERRSYNTAX:
			setObjectAt(msg, oldtop);
			break;
		}
		stacksetsize(oldtop + 1);
	  }

	  internal void dThrow(int status)
	  {
		throw new LuaError(status);
	  }


	  //////////////////////////////////////////////////////////////////////
	  // Func

	  // Methods equivalent to the file lfunc.c.  Prefixed with f.

	  /// <summary>
	  /// Equivalent of luaF_close.  All open upvalues referencing stack
	  /// slots level or higher are closed. </summary>
	  /// <param name="level">  Absolute stack index. </param>
	  private void fClose(int level)
	  {
		int i = openupval.Count;
		while (--i >= 0)
		{
		  UpVal uv = (UpVal)openupval[i];
		  if (uv.offset() < level)
		  {
			break;
		  }
		  uv.close();
		}
		openupval.Capacity = i + 1;
		return;
	  }

	  private UpVal fFindupval(int idx)
	  {
		/*
		 * We search from the end of the Vector towards the beginning,
		 * looking for an UpVal for the required stack-slot.
		 */
		int i = openupval.Count;
		while (--i >= 0)
		{
		  UpVal uv2 = (UpVal)openupval[i];
		  if (uv2.offset() == idx)
		  {
			return uv2;
		  }
		  if (uv2.offset() < idx)
		  {
			break;
		  }
		}
		// i points to be position _after_ which we want to insert a new
		// UpVal (it's -1 when we want to insert at the beginning).
		UpVal uv = new UpVal(idx, stack[idx]);
		openupval.Insert(i + 1, uv);
		return uv;
	  }


	  //////////////////////////////////////////////////////////////////////
	  // Debug

	  // Methods equivalent to the file ldebug.c.  Prefixed with g.

	  /// <summary>
	  /// <var>p1</var> and <var>p2</var> are operands to a numeric opcode.
	  /// Corrupts <code>NUMOP[0]</code>.
	  /// There is the possibility of using <var>p1</var> and <var>p2</var> to
	  /// identify (for example) for local variable being used in the
	  /// computation (consider the error message for code like <code>local
	  /// y='a'; return y+1</code> for example).  Currently the debug info is
	  /// not used, and this opportunity is wasted (it would require changing
	  /// or overloading gTypeerror).
	  /// </summary>
	  private void gAritherror(Slot p1, Slot p2)
	  {
		if (!tonumber(p1, NUMOP))
		{
		  p2 = p1; // first operand is wrong
		}
		gTypeerror(p2, "perform arithmetic on");
	  }

	  /// <summary>
	  /// <var>p1</var> and <var>p2</var> are absolute stack indexes. </summary>
	  private void gConcaterror(int p1, int p2)
	  {
		if (stack[p1].r is string)
		{
		  p1 = p2;
		}
		// assert !(p1 instanceof String);
		gTypeerror(stack[p1], "concatenate");
	  }

	  internal bool gCheckcode(Proto p)
	  {
		// :todo: implement me.
		return true;
	  }

	  private int gErrormsg(object message)
	  {
		push(message);
		if (errfunc != null) // is there an error handling function
		{
		  if (!isFunction(errfunc))
		  {
			dThrow(ERRERR);
		  }
		  insert(errfunc, Top); // push function (under error arg)
		  vmCall(stackSize-2, 1); // call it
		}
		dThrow(ERRRUN);
		// NOTREACHED
		return 0;
	  }

	  private bool gOrdererror(Slot p1, Slot p2)
	  {
		string t1 = typeName(type(p1));
		string t2 = typeName(type(p2));
		if (t1[2] == t2[2])
		{
		  gRunerror("attempt to compare two " + t1 + "values");
		}
		else
		{
		  gRunerror("attempt to compare " + t1 + " with " + t2);
		}
		// NOTREACHED
		return false;
	  }

	  internal void gRunerror(string s)
	  {
		gErrormsg(s);
	  }

	  private void gTypeerror(object o, string op)
	  {
		string t = typeName(type(o));
		gRunerror("attempt to " + op + " a " + t + " value");
	  }

	  private void gTypeerror(Slot p, string op)
	  {
		// :todo: PUC-Rio searches the stack to see if the value (which may
		// be a reference to stack cell) is a local variable.
		// For now we cop out and just call gTypeerror(Object, String)
		gTypeerror(p.asObject(), op);
	  }


	  //////////////////////////////////////////////////////////////////////
	  // Object

	  // Methods equivalent to the file lobject.c.  Prefixed with o.

	  private const int IDSIZE = 60;
	  /// <returns> a string no longer than IDSIZE. </returns>
	  internal static string oChunkid(string source)
	  {
		int len = IDSIZE;
		if (source.StartsWith("="))
		{
		  if (source.Length < IDSIZE+1)
		  {
			return source.Substring(1);
		  }
		  else
		  {
			return source.Substring(1, len);
		  }
		}
		// else  "source" or "...source"
		if (source.StartsWith("@"))
		{
		  source = source.Substring(1);
		  len -= " '...' ".Length;
		  int l2 = source.Length;
		  if (l2 > len)
		  {
			return "..." + source.Substring(source.Length - len, source.Length - (source.Length - len)); // get last part of file name
		  }
		  return source;
		}
		// else  [string "string"]
		int l = source.IndexOf('\n');
		if (l == -1)
		{
		  l = source.Length;
		}
		len -= " [string \"...\"] ".Length;
		if (l > len)
		{
		  l = len;
		}
		StringBuilder buf = new StringBuilder();
		buf.Append("[string \"");
		buf.Append(source.Substring(0, l));
		if (source.Length > l) // must truncate
		{
		  buf.Append("...");
		}
		buf.Append("\"]");
		return buf.ToString();
	  }

	  /// <summary>
	  /// Equivalent to luaO_fb2int. </summary>
	  /// <seealso cref= Syntax#oInt2fb </seealso>
	  private static int oFb2int(int x)
	  {
		int e = ((int)((uint)x >> 3)) & 31;
		if (e == 0)
		{
		  return x;
		}
		return ((x & 7) + 8) << (e-1);
	  }

	  /// <summary>
	  /// Equivalent to luaO_rawequalObj. </summary>
	  private static bool oRawequal(object a, object b)
	  {
		// see also vmEqual
		if (NIL == a)
		{
		  return NIL == b;
		}
		// Now a is not null, so a.equals() is a valid call.
		// Numbers (Doubles), Booleans, Strings all get compared by value,
		// as they should; tables, functions, get compared by identity as
		// they should.
		return a.Equals(b);
	  }

	  /// <summary>
	  /// Equivalent to luaO_str2d. </summary>
	  private static bool oStr2d(string s, double[] @out)
	  {
		// :todo: using try/catch may be too slow.  In which case we'll have
		// to recognise the valid formats first.
		try
		{
		  @out[0] = Convert.ToDouble(s);
		  return true;
		}
		catch (NumberFormatException)
		{
		  try
		  {
			// Attempt hexadecimal conversion.
			// :todo: using String.trim is not strictly accurate, because it
			// trims other ASCII control characters as well as whitespace.
			s = s.Trim().ToUpper();
			if (s.StartsWith("0X"))
			{
			  s = s.Substring(2);
			}
			else if (s.StartsWith("-0X"))
			{
			  s = "-" + s.Substring(3);
			}
			else
			{
			  return false;
			}
			@out[0] = Convert.ToInt32(s, 16);
			return true;
		  }
		  catch (NumberFormatException)
		  {
			return false;
		  }
		}
	  }


	  ////////////////////////////////////////////////////////////////////////
	  // VM

	  // Most of the methods in this section are equivalent to the files
	  // lvm.c and ldo.c from PUC-Rio.  They're mostly prefixed with vm as
	  // well.

	  private const int PCRLUA = 0;
	  private const int PCRJ = 1;
	  private const int PCRYIELD = 2;

	  // Instruction decomposition.

	  // There follows a series of methods that extract the various fields
	  // from a VM instruction.  See lopcodes.h from PUC-Rio.
	  // :todo: Consider replacing with m4 macros (or similar).
	  // A brief overview of the instruction format:
	  // Logically an instruction has an opcode (6 bits), op, and up to
	  // three fields using one of three formats:
	  // A B C  (8 bits, 9 bits, 9 bits)
	  // A Bx   (8 bits, 18 bits)
	  // A sBx  (8 bits, 18 bits signed - excess K)
	  // Some instructions do not use all the fields (EG OP_UNM only uses A
	  // and B).
	  // When packed into a word (an int in Jill) the following layouts are
	  // used:
	  //  31 (MSB)    23 22          14 13         6 5      0 (LSB)
	  // +--------------+--------------+------------+--------+
	  // | B            | C            | A          | OPCODE |
	  // +--------------+--------------+------------+--------+
	  //
	  // +--------------+--------------+------------+--------+
	  // | Bx                          | A          | OPCODE |
	  // +--------------+--------------+------------+--------+
	  //
	  // +--------------+--------------+------------+--------+
	  // | sBx                         | A          | OPCODE |
	  // +--------------+--------------+------------+--------+

	  internal const int NO_REG = 0xff; // SIZE_A == 8, (1 << 8)-1

	  // Hardwired values for speed.
	  /// <summary>
	  /// Equivalent of macro GET_OPCODE </summary>
	  internal static int OPCODE(int instruction)
	  {
		// POS_OP == 0 (shift amount)
		// SIZE_OP == 6 (opcode width)
		return instruction & 0x3f;
	  }

	  /// <summary>
	  /// Equivalent of macro GET_OPCODE </summary>
	  internal static int SET_OPCODE(int i, int op)
	  {
		// POS_OP == 0 (shift amount)
		// SIZE_OP == 6 (opcode width)
		return (i & ~0x3F) | (op & 0x3F);
	  }

	  /// <summary>
	  /// Equivalent of macro GETARG_A </summary>
	  internal static int ARGA(int instruction)
	  {
		// POS_A == POS_OP + SIZE_OP == 6 (shift amount)
		// SIZE_A == 8 (operand width)
		return ((int)((uint)instruction >> 6)) & 0xff;
	  }

	  internal static int SETARG_A(int i, int u)
	  {
		return (i & ~(0xff << 6)) | ((u & 0xff) << 6);
	  }

	  /// <summary>
	  /// Equivalent of macro GETARG_B </summary>
	  internal static int ARGB(int instruction)
	  {
		// POS_B == POS_OP + SIZE_OP + SIZE_A + SIZE_C == 23 (shift amount)
		// SIZE_B == 9 (operand width)
		/* No mask required as field occupies the most significant bits of a
		 * 32-bit int. */
		return ((int)((uint)instruction >> 23));
	  }

	  internal static int SETARG_B(int i, int b)
	  {
		return (i & ~(0x1ff << 23)) | ((b & 0x1ff) << 23);
	  }

	  /// <summary>
	  /// Equivalent of macro GETARG_C </summary>
	  internal static int ARGC(int instruction)
	  {
		// POS_C == POS_OP + SIZE_OP + SIZE_A == 14 (shift amount)
		// SIZE_C == 9 (operand width)
		return ((int)((uint)instruction >> 14)) & 0x1ff;
	  }

	  internal static int SETARG_C(int i, int c)
	  {
		return (i & ~(0x1ff << 14)) | ((c & 0x1ff) << 14);
	  }

	  /// <summary>
	  /// Equivalent of macro GETARG_Bx </summary>
	  internal static int ARGBx(int instruction)
	  {
		// POS_Bx = POS_C == 14
		// SIZE_Bx == SIZE_C + SIZE_B == 18
		/* No mask required as field occupies the most significant bits of a
		 * 32 bit int. */
		return ((int)((uint)instruction >> 14));
	  }

	  internal static int SETARG_Bx(int i, int bx)
	  {
		return (i & 0x3fff) | (bx << 14);
	  }


	  /// <summary>
	  /// Equivalent of macro GETARG_sBx </summary>
	  internal static int ARGsBx(int instruction)
	  {
		// As ARGBx but with (2**17-1) subtracted.
		return ((int)((uint)instruction >> 14)) - MAXARG_sBx;
	  }

	  internal static int SETARG_sBx(int i, int bx)
	  {
		return (i & 0x3fff) | ((bx + MAXARG_sBx) << 14); // CHECK THIS IS RIGHT
	  }

	  internal static bool ISK(int field)
	  {
		// The "is constant" bit position depends on the size of the B and C
		// fields (required to be the same width).
		// SIZE_B == 9
		return field >= 0x100;
	  }

	  /// <summary>
	  /// Near equivalent of macros RKB and RKC.  Note: non-static as it
	  /// requires stack and base instance members.  Stands for "Register or
	  /// Konstant" by the way, it gets value from either the register file
	  /// (stack) or the constant array (k).
	  /// </summary>
	  private Slot RK(Slot[] k, int field)
	  {

		if (ISK(field))
		{
			if (D)
			{
				Console.Error.WriteLine("RK:" + field);
			}
		  return k[field & 0xff];
		}
		if (D)
		{
			Console.Error.WriteLine("RK:" + (@base + field));
		}
		return stack[@base + field];
	  }

	  /// <summary>
	  /// Slower version of RK that does not receive the constant array.  Not
	  /// recommend for routine use, but is used by some error handling code
	  /// to avoid having a constant array passed around too much.
	  /// </summary>
	  private Slot RK(int field)
	  {
		LuaFunction function = (LuaFunction)stack[__ci().function()].r;
		Slot[] k = function.proto().constant();
		return RK(k, field);
	  }

	  // CREATE functions are required by FuncState, so default access.
	  internal static int CREATE_ABC(int o, int a, int b, int c)
	  {
		// POS_OP == 0
		// POS_A == 6
		// POS_B == 23
		// POS_C == 14
		return o | (a << 6) | (b << 23) | (c << 14);
	  }

	  internal static int CREATE_ABx(int o, int a, int bc)
	  {
		// POS_OP == 0
		// POS_A == 6
		// POS_Bx == POS_C == 14
		return o | (a << 6) | (bc << 14);
	  }

	  // opcode enumeration.
	  // Generated by a script:
	  // awk -f opcode.awk < lopcodes.h
	  // and then pasted into here.
	  // Made default access so that code generation, in FuncState, can see
	  // the enumeration as well.

	  internal const int OP_MOVE = 0;
	  internal const int OP_LOADK = 1;
	  internal const int OP_LOADBOOL = 2;
	  internal const int OP_LOADNIL = 3;
	  internal const int OP_GETUPVAL = 4;
	  internal const int OP_GETGLOBAL = 5;
	  internal const int OP_GETTABLE = 6;
	  internal const int OP_SETGLOBAL = 7;
	  internal const int OP_SETUPVAL = 8;
	  internal const int OP_SETTABLE = 9;
	  internal const int OP_NEWTABLE = 10;
	  internal const int OP_SELF = 11;
	  internal const int OP_ADD = 12;
	  internal const int OP_SUB = 13;
	  internal const int OP_MUL = 14;
	  internal const int OP_DIV = 15;
	  internal const int OP_MOD = 16;
	  internal const int OP_POW = 17;
	  internal const int OP_UNM = 18;
	  internal const int OP_NOT = 19;
	  internal const int OP_LEN = 20;
	  internal const int OP_CONCAT = 21;
	  internal const int OP_JMP = 22;
	  internal const int OP_EQ = 23;
	  internal const int OP_LT = 24;
	  internal const int OP_LE = 25;
	  internal const int OP_TEST = 26;
	  internal const int OP_TESTSET = 27;
	  internal const int OP_CALL = 28;
	  internal const int OP_TAILCALL = 29;
	  internal const int OP_RETURN = 30;
	  internal const int OP_FORLOOP = 31;
	  internal const int OP_FORPREP = 32;
	  internal const int OP_TFORLOOP = 33;
	  internal const int OP_SETLIST = 34;
	  internal const int OP_CLOSE = 35;
	  internal const int OP_CLOSURE = 36;
	  internal const int OP_VARARG = 37;

	  // end of instruction decomposition

	  internal const int SIZE_C = 9;
	  internal const int SIZE_B = 9;
	  internal static readonly int SIZE_Bx = SIZE_C + SIZE_B;
	  internal const int SIZE_A = 8;

	  internal const int SIZE_OP = 6;

	  internal const int POS_OP = 0;
	  internal static readonly int POS_A = POS_OP + SIZE_OP;
	  internal static readonly int POS_C = POS_A + SIZE_A;
	  internal static readonly int POS_B = POS_C + SIZE_C;
	  internal static readonly int POS_Bx = POS_C;

	  internal static readonly int MAXARG_Bx = (1 << SIZE_Bx) - 1;
	  internal static readonly int MAXARG_sBx = MAXARG_Bx >> 1; // `sBx' is signed


	  internal static readonly int MAXARG_A = (1 << SIZE_A) - 1;
	  internal static readonly int MAXARG_B = (1 << SIZE_B) - 1;
	  internal static readonly int MAXARG_C = (1 << SIZE_C) - 1;

	  /* this bit 1 means constant (0 means register) */
	  internal static readonly int BITRK = 1 << (SIZE_B - 1);
	  internal static readonly int MAXINDEXRK = BITRK - 1;


	  /// <summary>
	  /// Equivalent of luaD_call. </summary>
	  /// <param name="func">  absolute stack index of function to call. </param>
	  /// <param name="r">     number of required results. </param>
	  private void vmCall(int func, int r)
	  {
		++nCcalls;
		if (vmPrecall(func, r) == PCRLUA)
		{
		  vmExecute(1);
		}
		--nCcalls;
	  }

	  /// <summary>
	  /// Equivalent of luaV_concat. </summary>
	  private void vmConcat(int total, int last)
	  {
		do
		{
		  int top = @base + last + 1;
		  int n = 2; // number of elements handled in this pass (at least 2)
		  if (!tostring(top - 2) || !tostring(top - 1))
		  {
			  if (D)
			  {
				  Console.Error.WriteLine("vmConcat:" + (top - 2) + "," + (top - 1));
			  }
			if (!call_binTM(stack[top - 2], stack[top - 1], stack[top - 2], "__concat"))
			{
			  gConcaterror(top - 2, top - 1);
			}
		  }
		  else if (((string)stack[top - 1].r).Length > 0)
		  {
			int tl = ((string)stack[top - 1].r).Length;
			for (n = 1; n < total && tostring(top - n - 1); ++n)
			{
			  tl += ((string)stack[top - n - 1].r).Length;
			  if (tl < 0)
			  {
				gRunerror("string length overflow");
			  }
			}
			StringBuilder buffer = new StringBuilder(tl);
			for (int i = n; i > 0; i--) // concat all strings
			{
			  buffer.Append(stack[top - i].r);
			}
			stack[top - n].r = buffer.ToString();
		  }
		  total -= n - 1; // got n strings to create 1 new
		  last -= n - 1;
		} while (total > 1); // repeat until only 1 result left
	  }

	  /// <summary>
	  /// Primitive for testing Lua equality of two values.  Equivalent of
	  /// PUC-Rio's <code>equalobj</code> macro.
	  /// In the loosest sense, this is the equivalent of
	  /// <code>luaV_equalval</code>.
	  /// </summary>
	  private bool vmEqual(Slot a, Slot b)
	  {
		// Deal with number case first
		if (NUMBER == a.r)
		{
		  if (NUMBER != b.r)
		  {
			return false;
		  }
		  return a.d == b.d;
		}
		// Now we're only concerned with the .r field.
		return vmEqualRef(a.r, b.r);
	  }

	  /// <summary>
	  /// Part of <seealso cref="#vmEqual"/>.  Compares the reference part of two
	  /// Slot instances.  That is, compares two Lua values, as long as
	  /// neither is a number.
	  /// </summary>
	  private bool vmEqualRef(object a, object b)
	  {
		if (a.Equals(b))
		{
		  return true;
		}
		if (a.GetType() != b.GetType())
		{
		  return false;
		}
		// Same class, but different objects.
		if (a is LuaJavaCallback || a is LuaTable)
		{
		  // Resort to metamethods.
		  object tm = get_compTM(getMetatable(a), getMetatable(b), "__eq");
		  if (NIL == tm) // no TM?
		  {
			return false;
		  }
		  Slot s = new Slot();
		  callTMres(s, tm, a, b); // call TM
		  return !isFalse(s.r);
		}
		return false;
	  }

	  /// <summary>
	  /// Array of numeric operands.  Used when converting strings to numbers
	  /// by an arithmetic opcode (ADD, SUB, MUL, DIV, MOD, POW, UNM).
	  /// </summary>
	  private static readonly double[] NUMOP = new double[2];

	  /// <summary>
	  /// The core VM execution engine. </summary>
	  private void vmExecute(int nexeccalls)
	  {
		// This labelled while loop is used to simulate the effect of C's
		// goto.  The end of the while loop is never reached.  The beginning
		// of the while loop is branched to using a "continue reentry;"
		// statement (when a Lua function is called or returns).
		while (true)
		{
		  // assert stack[ci.function()].r instanceof LuaFunction;
		  LuaFunction function = (LuaFunction)stack[__ci().function()].r;
		  Proto proto = function.proto();
		  int[] code = proto.code();
		  Slot[] k = proto.constant();
		  int pc = savedpc;

		  while (true) // main loop of interpreter
		  {

			// Where the PUC-Rio code used the Protect macro, this has been
			// replaced with "savedpc = pc" and a "// Protect" comment.

			// Where the PUC-Rio code used the dojump macro, this has been
			// replaced with the equivalent increment of the pc and a
			// "//dojump" comment.

			int i = code[pc++]; // VM instruction.
			// :todo: line hook
			if ((hookmask & MASKCOUNT) != 0 && --hookcount == 0)
			{
			  traceexec(pc);
			  if (status_Renamed == YIELD) // did hook yield?
			  {
				savedpc = pc - 1;
				return;
			  }
			  // base = this.base
			}

			int a = ARGA(i); // its A field.
			Slot rb;
			Slot rc;

			switch (OPCODE(i))
			{
			  case OP_MOVE:
				stack[@base + a].r = stack[@base + ARGB(i)].r;
				stack[@base + a].d = stack[@base + ARGB(i)].d;
				continue;
			  case OP_LOADK:
				stack[@base + a].r = k[ARGBx(i)].r;
				stack[@base + a].d = k[ARGBx(i)].d;
				continue;
			  case OP_LOADBOOL:
				stack[@base + a].r = valueOfBoolean(ARGB(i) != 0);
				if (ARGC(i) != 0)
				{
				  ++pc;
				}
				continue;
			  case OP_LOADNIL:
			  {
				int b = @base + ARGB(i);
				do
				{
				  stack[b--].r = NIL;
				} while (b >= @base + a);
				continue;
			  }
			  case OP_GETUPVAL:
			  {
				int b = ARGB(i);
				// :todo: optimise path
				setObjectAt(function.upVal(b).Value, @base + a);
				continue;
			  }
			  case OP_GETGLOBAL:
				rb = k[ARGBx(i)];
				// assert rb instance of String;
				savedpc = pc; // Protect
				vmGettable(function.getEnv(), rb, stack[@base + a]);
				continue;
			  case OP_GETTABLE:
			  {
				savedpc = pc; // Protect
				object h = stack[@base + ARGB(i)].asObject();
				if (D)
				{
					Console.Error.WriteLine("OP_GETTABLE index = " + (this.@base + ARGB(i)) + ", size = " + this.stack.Length + ", h = " + h);
				}
				vmGettable(h, RK(k, ARGC(i)), stack[@base + a]);
				continue;
			  }
			  case OP_SETUPVAL:
			  {
				UpVal uv = function.upVal(ARGB(i));
				uv.Value = objectAt(@base + a);
				continue;
			  }
			  case OP_SETGLOBAL:
				savedpc = pc; // Protect
				// :todo: consider inlining objectAt
				vmSettable(function.getEnv(), k[ARGBx(i)], objectAt(@base + a));
				continue;
			  case OP_SETTABLE:
			  {
				savedpc = pc; // Protect
				object t = stack[@base + a].asObject();
				vmSettable(t, RK(k, ARGB(i)), RK(k, ARGC(i)).asObject());
				continue;
			  }
			  case OP_NEWTABLE:
			  {
				int b = ARGB(i);
				int c = ARGC(i);
				stack[@base + a].r = new LuaTable(oFb2int(b), oFb2int(c));
				continue;
			  }
			  case OP_SELF:
			  {
				int b = ARGB(i);
				rb = stack[@base + b];
				stack[@base + a + 1].r = rb.r;
				stack[@base + a + 1].d = rb.d;
				savedpc = pc; // Protect
				vmGettable(rb.asObject(), RK(k, ARGC(i)), stack[@base + a]);
				continue;
			  }
			  case OP_ADD:
				rb = RK(k, ARGB(i));
				rc = RK(k, ARGC(i));
				if (rb.r == NUMBER && rc.r == NUMBER)
				{
				  double sum = rb.d + rc.d;
				  stack[@base + a].d = sum;
				  stack[@base + a].r = NUMBER;
				}
				else if (toNumberPair(rb, rc, NUMOP))
				{
				  double sum = NUMOP[0] + NUMOP[1];
				  stack[@base + a].d = sum;
				  stack[@base + a].r = NUMBER;
				}
				else if (!call_binTM(rb, rc, stack[@base + a], "__add"))
				{
				  gAritherror(rb, rc);
				}
				continue;
			  case OP_SUB:
				rb = RK(k, ARGB(i));
				rc = RK(k, ARGC(i));
				if (rb.r == NUMBER && rc.r == NUMBER)
				{
				  double difference = rb.d - rc.d;
				  stack[@base + a].d = difference;
				  stack[@base + a].r = NUMBER;
				}
				else if (toNumberPair(rb, rc, NUMOP))
				{
				  double difference = NUMOP[0] - NUMOP[1];
				  stack[@base + a].d = difference;
				  stack[@base + a].r = NUMBER;
				}
				else if (!call_binTM(rb, rc, stack[@base + a], "__sub"))
				{
				  gAritherror(rb, rc);
				}
				continue;
			  case OP_MUL:
				rb = RK(k, ARGB(i));
				rc = RK(k, ARGC(i));
				if (rb.r == NUMBER && rc.r == NUMBER)
				{
				  double product = rb.d * rc.d;
				  stack[@base + a].d = product;
				  stack[@base + a].r = NUMBER;
				}
				else if (toNumberPair(rb, rc, NUMOP))
				{
				  double product = NUMOP[0] * NUMOP[1];
				  stack[@base + a].d = product;
				  stack[@base + a].r = NUMBER;
				}
				else if (!call_binTM(rb, rc, stack[@base + a], "__mul"))
				{
				  gAritherror(rb, rc);
				}
				continue;
			  case OP_DIV:
				rb = RK(k, ARGB(i));
				rc = RK(k, ARGC(i));
				if (rb.r == NUMBER && rc.r == NUMBER)
				{
				  double quotient = rb.d / rc.d;
				  stack[@base + a].d = quotient;
				  stack[@base + a].r = NUMBER;
				}
				else if (toNumberPair(rb, rc, NUMOP))
				{
				  double quotient = NUMOP[0] / NUMOP[1];
				  stack[@base + a].d = quotient;
				  stack[@base + a].r = NUMBER;
				}
				else if (!call_binTM(rb, rc, stack[@base + a], "__div"))
				{
				  gAritherror(rb, rc);
				}
				continue;
			  case OP_MOD:
				rb = RK(k, ARGB(i));
				rc = RK(k, ARGC(i));
				if (rb.r == NUMBER && rc.r == NUMBER)
				{
				  double modulus = Lua.modulus(rb.d, rc.d);
				  stack[@base + a].d = modulus;
				  stack[@base + a].r = NUMBER;
				}
				else if (toNumberPair(rb, rc, NUMOP))
				{
				  double modulus = Lua.modulus(NUMOP[0], NUMOP[1]);
				  stack[@base + a].d = modulus;
				  stack[@base + a].r = NUMBER;
				}
				else if (!call_binTM(rb, rc, stack[@base + a], "__mod"))
				{
				  gAritherror(rb, rc);
				}
				continue;
			  case OP_POW:
				rb = RK(k, ARGB(i));
				rc = RK(k, ARGC(i));
				if (rb.r == NUMBER && rc.r == NUMBER)
				{
				  double result = iNumpow(rb.d, rc.d);
				  stack[@base + a].d = result;
				  stack[@base + a].r = NUMBER;
				}
				else if (toNumberPair(rb, rc, NUMOP))
				{
				  double result = iNumpow(NUMOP[0], NUMOP[1]);
				  stack[@base + a].d = result;
				  stack[@base + a].r = NUMBER;
				}
				else if (!call_binTM(rb, rc, stack[@base + a], "__pow"))
				{
				  gAritherror(rb, rc);
				}
				continue;
			  case OP_UNM:
				rb = stack[@base + ARGB(i)];
				if (rb.r == NUMBER)
				{
				  stack[@base + a].d = -rb.d;
				  stack[@base + a].r = NUMBER;
				}
				else if (tonumber(rb, NUMOP))
				{
				  stack[@base + a].d = -NUMOP[0];
				  stack[@base + a].r = NUMBER;
				}
				else if (!call_binTM(rb, rb, stack[@base + a], "__unm"))
				{
				  gAritherror(rb, rb);
				}
				continue;
			  case OP_NOT:
			  {
				// All numbers are treated as true, so no need to examine
				// the .d field.
				object ra = stack[@base + ARGB(i)].r;
				stack[@base + a].r = valueOfBoolean(isFalse(ra));
				continue;
			  }
			  case OP_LEN:
				rb = stack[@base + ARGB(i)];
				if (rb.r is LuaTable)
				{
				  LuaTable t = (LuaTable)rb.r;
				  stack[@base + a].d = t.getn();
				  stack[@base + a].r = NUMBER;
				  continue;
				}
				else if (rb.r is string)
				{
				  string s = (string)rb.r;
				  stack[@base + a].d = s.Length;
				  stack[@base + a].r = NUMBER;
				  continue;
				}
				savedpc = pc; // Protect
				if (!call_binTM(rb, rb, stack[@base + a], "__len"))
				{
				  gTypeerror(rb, "get length of");
				}
				continue;
			  case OP_CONCAT:
			  {
				int b = ARGB(i);
				int c = ARGC(i);
				savedpc = pc; // Protect
				// :todo: The compiler assumes that all
				// stack locations _above_ b end up with junk in them.  In
				// which case we can improve the speed of vmConcat (by not
				// converting each stack slot, but simply using
				// StringBuffer.append on whatever is there).
				vmConcat(c - b + 1, c);
				stack[@base + a].r = stack[@base + b].r;
				stack[@base + a].d = stack[@base + b].d;
				continue;
			  }
			  case OP_JMP:
				// dojump
				pc += ARGsBx(i);
				continue;
			  case OP_EQ:
				rb = RK(k, ARGB(i));
				rc = RK(k, ARGC(i));
				if (vmEqual(rb, rc) == (a != 0))
				{
				  // dojump
				  pc += ARGsBx(code[pc]);
				}
				++pc;
				continue;
			  case OP_LT:
				rb = RK(k, ARGB(i));
				rc = RK(k, ARGC(i));
				savedpc = pc; // Protect
				if (vmLessthan(rb, rc) == (a != 0))
				{
				  // dojump
				  pc += ARGsBx(code[pc]);
				}
				++pc;
				continue;
			  case OP_LE:
				rb = RK(k, ARGB(i));
				rc = RK(k, ARGC(i));
				savedpc = pc; // Protect
				if (vmLessequal(rb, rc) == (a != 0))
				{
				  // dojump
				  pc += ARGsBx(code[pc]);
				}
				++pc;
				continue;
			  case OP_TEST:
				if (isFalse(stack[@base + a].r) != (ARGC(i) != 0))
				{
				  // dojump
				  pc += ARGsBx(code[pc]);
				}
				++pc;
				continue;
			  case OP_TESTSET:
				rb = stack[@base + ARGB(i)];
				if (isFalse(rb.r) != (ARGC(i) != 0))
				{
				  stack[@base + a].r = rb.r;
				  stack[@base + a].d = rb.d;
				  // dojump
				  pc += ARGsBx(code[pc]);
				}
				++pc;
				continue;
			  case OP_CALL:
			  {
				int b = ARGB(i);
				int nresults = ARGC(i) - 1;
				if (b != 0)
				{
				  stacksetsize(@base + a + b);
				}
				savedpc = pc;
				switch (vmPrecall(@base + a, nresults))
				{
				  case PCRLUA:
					nexeccalls++;
					goto reentryContinue;
				  case PCRJ:
					// Was Java function called by precall, adjust result
					if (nresults >= 0)
					{
					  stacksetsize(__ci().top());
					}
					continue;
				  default:
					return; // yield
				}
			  }
				  goto case OP_TAILCALL;
			  case OP_TAILCALL:
			  {
				int b = ARGB(i);
				if (b != 0)
				{
				  stacksetsize(@base + a + b);
				}
				savedpc = pc;
				// assert ARGC(i) - 1 == MULTRET
				switch (vmPrecall(@base + a, MULTRET))
				{
				  case PCRLUA:
				  {
					// tail call: put new frame in place of previous one.
					CallInfo ci = (CallInfo)civ.elementAt(civ.getSize() - 2);
					int func = ci.function();
					CallInfo fci = __ci(); // Fresh CallInfo
					int pfunc = fci.function();
					fClose(ci.@base());
					@base = func + (fci.@base() - pfunc);
					int aux; // loop index is used after loop ends
					for (aux = 0; pfunc + aux < stackSize; ++aux)
					{
					  // move frame down
					  stack[func + aux].r = stack[pfunc + aux].r;
					  stack[func + aux].d = stack[pfunc + aux].d;
					}
					stacksetsize(func + aux); // correct top
					// assert stackSize == base + ((LuaFunction)stack[func]).proto().maxstacksize();
					ci.tailcall(@base, stackSize);
					dec_ci(); // remove new frame.
					goto reentryContinue;
				  }
				  case PCRJ: // It was a Java function
				  {
					continue;
				  }
				  default:
				  {
					return; // yield
				  }
				}
			  }
				  goto case OP_RETURN;
			  case OP_RETURN:
			  {
				fClose(@base);
				int b = ARGB(i);
				if (b != 0)
				{
				  int top = a + b - 1;
				  stacksetsize(@base + top);
				}
				savedpc = pc;
				// 'adjust' replaces aliased 'b' in PUC-Rio code.
				bool adjust = vmPoscall(@base + a);
				if (--nexeccalls == 0)
				{
				  return;
				}
				if (adjust)
				{
				  stacksetsize(__ci().top());
				}
				goto reentryContinue;
			  }
			  case OP_FORLOOP:
			  {
				double step = stack[@base + a + 2].d;
				double idx = stack[@base + a].d + step;
				double limit = stack[@base + a + 1].d;
				if ((0 < step && idx <= limit) || (step <= 0 && limit <= idx))
				{
				  // dojump
				  pc += ARGsBx(i);
				  stack[@base + a].d = idx; // internal index
				  stack[@base + a].r = NUMBER;
				  stack[@base + a + 3].d = idx; // external index
				  stack[@base + a + 3].r = NUMBER;
				}
				continue;
			  }
			  case OP_FORPREP:
			  {
				int init = @base + a;
				int plimit = @base + a + 1;
				int pstep = @base + a + 2;
				savedpc = pc; // next steps may throw errors
				if (!tonumber(init))
				{
				  gRunerror("'for' initial value must be a number");
				}
				else if (!tonumber(plimit))
				{
				  gRunerror("'for' limit must be a number");
				}
				else if (!tonumber(pstep))
				{
				  gRunerror("'for' step must be a number");
				}
				double step = stack[pstep].d;
				double idx = stack[init].d - step;
				stack[init].d = idx;
				stack[init].r = NUMBER;
				// dojump
				pc += ARGsBx(i);
				continue;
			  }
			  case OP_TFORLOOP:
			  {
				int cb = @base + a + 3; // call base
				stack[cb + 2].r = stack[@base + a + 2].r;
				stack[cb + 2].d = stack[@base + a + 2].d;
				stack[cb + 1].r = stack[@base + a + 1].r;
				stack[cb + 1].d = stack[@base + a + 1].d;
				stack[cb].r = stack[@base + a].r;
				stack[cb].d = stack[@base + a].d;
				stacksetsize(cb + 3);
				savedpc = pc; // Protect
				vmCall(cb, ARGC(i));
				stacksetsize(__ci().top());
				if (NIL != stack[cb].r) // continue loop
				{
				  stack[cb - 1].r = stack[cb].r;
				  stack[cb - 1].d = stack[cb].d;
				  // dojump
				  pc += ARGsBx(code[pc]);
				}
				++pc;
				continue;
			  }
			  case OP_SETLIST:
			  {
				int n = ARGB(i);
				int c = ARGC(i);
				bool setstack = false;
				if (0 == n)
				{
				  n = (stackSize - (@base + a)) - 1;
				  setstack = true;
				}
				if (0 == c)
				{
				  c = code[pc++];
				}
				LuaTable t = (LuaTable)stack[@base + a].r;
				int last = ((c - 1) * LFIELDS_PER_FLUSH) + n;
				// :todo: consider expanding space in table
				for (; n > 0; n--)
				{
				  object val = objectAt(@base + a + n);
				  t.putnum(last--, val);
				}
				if (setstack)
				{
				  stacksetsize(__ci().top());
				}
				continue;
			  }
			  case OP_CLOSE:
				fClose(@base + a);
				continue;
			  case OP_CLOSURE:
			  {
				Proto p = function.proto().proto()[ARGBx(i)];
				int nup = p.nups();
				UpVal[] up = new UpVal[nup];
				for (int j = 0; j < nup; j++, pc++)
				{
				  int @in = code[pc];
				  if (OPCODE(@in) == OP_GETUPVAL)
				  {
					up[j] = function.upVal(ARGB(@in));
				  }
				  else
				  {
					// assert OPCODE(in) == OP_MOVE;
					up[j] = fFindupval(@base + ARGB(@in));
				  }
				}
				LuaFunction nf = new LuaFunction(p, up, function.getEnv());
				stack[@base + a].r = nf;
				continue;
			  }
			  case OP_VARARG:
			  {
				int b = ARGB(i) - 1;
				int n = (@base - __ci().function()) - function.proto().numparams() - 1;
				if (b == MULTRET)
				{
				  // :todo: Protect
				  // :todo: check stack
				  b = n;
				  stacksetsize(@base + a + n);
				}
				for (int j = 0; j < b; ++j)
				{
				  if (j < n)
				  {
					Slot src = stack[@base - n + j];
					stack[@base + a + j].r = src.r;
					stack[@base + a + j].d = src.d;
				  }
				  else
				  {
					stack[@base + a + j].r = NIL;
				  }
				}
				continue;
			  }
			} // switch
		  } // while
		reentryContinue:;
		} // reentry: while
	reentryBreak:;
	  }

	  internal static double iNumpow(double a, double b)
	  {
		// :todo: this needs proper checking for boundary cases
		// EG, is currently wrong for (-0)^2.
		bool invert = b < 0.0;
		if (invert)
		{
			b = -b;
		}
		if (a == 0.0)
		{
		  return invert ? double.NaN : a;
		}
		double result = 1.0;
		int ipow = (int) b;
		b -= ipow;
		double t = a;
		while (ipow > 0)
		{
		  if ((ipow & 1) != 0)
		  {
			result *= t;
		  }
		  ipow >>= 1;
		  t = t * t;
		}
		if (b != 0.0) // integer only case, save doing unnecessary work
		{
		  if (a < 0.0) // doesn't work if a negative (complex result!)
		  {
			return double.NaN;
		  }
		  t = Math.Sqrt(a);
		  double half = 0.5;
		  while (b > 0.0)
		  {
			if (b >= half)
			{
			  result = result * t;
			  b -= half;
			}
			b = b + b;
			t = Math.Sqrt(t);
			if (t == 1.0)
			{
			  break;
			}
		  }
		}
		return invert ? 1.0 / result : result;
	  }

	  /// <summary>
	  /// Equivalent of luaV_gettable. </summary>
	  private void vmGettable(object t, Slot key, Slot val)
	  {
		object tm;
		for (int loop = 0; loop < MAXTAGLOOP; ++loop)
		{
		  if (t is LuaTable) // 't' is a table?
		  {
			LuaTable h = (LuaTable)t;
			h.getlua(key, SPARE_SLOT);

			if (SPARE_SLOT.r != NIL)
			{
			  val.r = SPARE_SLOT.r;
			  val.d = SPARE_SLOT.d;
			  return;
			}
			tm = tagmethod(h, "__index");
			if (tm == NIL)
			{
			  val.r = NIL;
			  return;
			}
			// else will try the tag method
		  }
		  else
		  {
			tm = tagmethod(t, "__index");
			if (tm == NIL)
			{
			  gTypeerror(t, "index");
			}
		  }
		  if (isFunction(tm))
		  {
			SPARE_SLOT.Object = t;
			callTMres(val, tm, SPARE_SLOT, key);
			return;
		  }
		  t = tm; // else repeat with 'tm'
		}
		gRunerror("loop in gettable");
	  }

	  /// <summary>
	  /// Equivalent of luaV_lessthan. </summary>
	  private bool vmLessthan(Slot l, Slot r)
	  {
		if (l.r.GetType() != r.r.GetType())
		{
		  gOrdererror(l, r);
		}
		else if (l.r == NUMBER)
		{
		  return l.d < r.d;
		}
		else if (l.r is string)
		{
		  // :todo: PUC-Rio use strcoll, maybe we should use something
		  // equivalent.
		  return ((string)l.r).CompareTo((string)r.r) < 0;
		}
		int res = call_orderTM(l, r, "__lt");
		if (res >= 0)
		{
		  return res != 0;
		}
		return gOrdererror(l, r);
	  }

	  /// <summary>
	  /// Equivalent of luaV_lessequal. </summary>
	  private bool vmLessequal(Slot l, Slot r)
	  {
		if (l.r.GetType() != r.r.GetType())
		{
		  gOrdererror(l, r);
		}
		else if (l.r == NUMBER)
		{
		  return l.d <= r.d;
		}
		else if (l.r is string)
		{
		  return ((string)l.r).CompareTo((string)r.r) <= 0;
		}
		int res = call_orderTM(l, r, "__le"); // first try 'le'
		if (res >= 0)
		{
		  return res != 0;
		}
		res = call_orderTM(r, l, "__lt"); // else try 'lt'
		if (res >= 0)
		{
		  return res == 0;
		}
		return gOrdererror(l, r);
	  }

	  /// <summary>
	  /// Equivalent of luaD_poscall. </summary>
	  /// <param name="firstResult">  stack index (absolute) of the first result </param>
	  private bool vmPoscall(int firstResult)
	  {
		// :todo: call hook
		CallInfo lci; // local copy, for faster access
		lci = dec_ci();
		// Now (as a result of the dec_ci call), lci is the CallInfo record
		// for the current function (the function executing an OP_RETURN
		// instruction), and this.ci is the CallInfo record for the function
		// we are returning to.
		int res = lci.res();
		int wanted = lci.nresults(); // Caution: wanted could be == MULTRET
		CallInfo cci = __ci(); // Continuation CallInfo
		@base = cci.@base();
		savedpc = cci.savedpc();
		// Move results (and pad with nils to required number if necessary)
		int i = wanted;
		int top = stackSize;
		// The movement is always downwards, so copying from the top-most
		// result first is always correct.
		while (i != 0 && firstResult < top)
		{
			if (D)
			{
				Console.Error.WriteLine("vmPoscall:" + res);
			}
		  stack[res].r = stack[firstResult].r;
		  stack[res].d = stack[firstResult].d;
		  ++res;
		  ++firstResult;
		  i--;
		}
		if (i > 0)
		{
		  stacksetsize(res + i);
		}
		// :todo: consider using two stacksetsize calls to nil out
		// remaining required results.
		while (i-- > 0)
		{
		  stack[res++].r = NIL;
		}
		stacksetsize(res);
		return wanted != MULTRET;
	  }

	  /// <summary>
	  /// Equivalent of LuaD_precall.  This method expects that the arguments
	  /// to the function are placed above the function on the stack. </summary>
	  /// <param name="func">  absolute stack index of the function to call. </param>
	  /// <param name="r">     number of results expected. </param>
	  private int vmPrecall(int func, int r)
	  {
		object faso; // Function AS Object
		if (D)
		{
			Console.Error.WriteLine("vmPrecall:" + func);
		}
		faso = stack[func].r;
		if (!isFunction(faso))
		{
		  faso = tryfuncTM(func);
		}
		__ci().Savedpc = savedpc;
		if (faso is LuaFunction)
		{
		  LuaFunction f = (LuaFunction)faso;
		  Proto p = f.proto();
		  // :todo: ensure enough stack

		  if (!p.Vararg)
		  {
			@base = func + 1;
			if (stackSize > @base + p.numparams())
			{
			  // trim stack to the argument list
			  stacksetsize(@base + p.numparams());
			}
		  }
		  else
		  {
			int nargs = (stackSize - func) - 1;
			@base = adjust_varargs(p, nargs);
		  }

		  int top = @base + p.maxstacksize();
		  inc_ci(func, @base, top, r);

		  savedpc = 0;
		  // expand stack to the function's max stack size.
		  stacksetsize(top);
		  // :todo: implement call hook.
		  return PCRLUA;
		}
		else if (faso is LuaJavaCallback)
		{
		  LuaJavaCallback fj = (LuaJavaCallback)faso;
		  // :todo: checkstack (not sure it's necessary)
		  @base = func + 1;
		  inc_ci(func, @base, stackSize + MINSTACK, r);
		  // :todo: call hook
		  int n = 99;
		  try
		  {
			n = fj.luaFunction(this);
		  }
		  catch (LuaError e)
		  {
			throw e;
		  }
		  catch (Exception e)
		  {
			  Console.WriteLine(e.ToString());
			  Console.Write(e.StackTrace); //FIXME: added
			@yield(0);
			throw e;
		  }
		  if (n < 0) // yielding?
		  {
			return PCRYIELD;
		  }
		  else
		  {
			vmPoscall(stackSize - n);
			return PCRJ;
		  }
		}

		throw new System.ArgumentException();
	  }

	  /// <summary>
	  /// Equivalent of luaV_settable. </summary>
	  private void vmSettable(object t, Slot key, object val)
	  {
		for (int loop = 0; loop < MAXTAGLOOP; ++loop)
		{
		  object tm;
		  if (t is LuaTable) // 't' is a table
		  {
			LuaTable h = (LuaTable)t;
			h.getlua(key, SPARE_SLOT);
			if (SPARE_SLOT.r != NIL) // result is not nil?
			{
			  h.putlua(this, key, val);
			  return;
			}
			tm = tagmethod(h, "__newindex");
			if (tm == NIL) // or no TM?
			{
			  h.putlua(this, key, val);
			  return;
			}
			// else will try the tag method
		  }
		  else
		  {
			tm = tagmethod(t, "__newindex");
			if (tm == NIL)
			{
			  gTypeerror(t, "index");
			}
		  }
		  if (isFunction(tm))
		  {
			callTM(tm, t, key, val);
			return;
		  }
		  t = tm; // else repeat with 'tm'
		}
		gRunerror("loop in settable");
	  }

	  /// <summary>
	  /// Printf format item used to convert numbers to strings (in {@link
	  /// #vmTostring}).  The initial '%' should be not specified.
	  /// </summary>
	  private const string NUMBER_FMT = ".14g";

	  private static string vmTostring(object o)
	  {
		if (o is string)
		{
		  return (string)o;
		}
		if (!(o is double?))
		{
		  return null;
		}
		// Convert number to string.  PUC-Rio abstracts this operation into
		// a macro, lua_number2str.  The macro is only invoked from their
		// equivalent of this code.
		// Formerly this code used Double.toString (and remove any trailing
		// ".0") but this does not give an accurate emulation of the PUC-Rio
		// behaviour which Intuwave require.  So now we use "%.14g" like
		// PUC-Rio.
		// :todo: consider optimisation of making FormatItem an immutable
		// class and keeping a static reference to the required instance
		// (which never changes).  A possible half-way house would be to
		// create a copied instance from an already create prototype
		// instance which would be faster than parsing the format string
		// each time.
		FormatItem f = new FormatItem(null, NUMBER_FMT);
		StringBuilder b = new StringBuilder();
		double? d = (double?)o;
		f.formatFloat(b, (double)d);
		return b.ToString();
	  }

	  /// <summary>
	  /// Equivalent of adjust_varargs in "ldo.c". </summary>
	  private int adjust_varargs(Proto p, int actual)
	  {
		int nfixargs = p.numparams();
		for (; actual < nfixargs; ++actual)
		{
		  stackAdd(NIL);
		}
		// PUC-Rio's LUA_COMPAT_VARARG is not supported here.

		// Move fixed parameters to final position
		int @fixed = stackSize - actual; // first fixed argument
		int newbase = stackSize; // final position of first argument
		for (int i = 0; i < nfixargs; ++i)
		{
		  // :todo: arraycopy?
		  push(stack[@fixed + i]);
		  stack[@fixed + i].r = NIL;
		}
		return newbase;
	  }

	  /// <summary>
	  /// Does not modify contents of p1 or p2.  Modifies contents of res. </summary>
	  /// <param name="p1">  left hand operand. </param>
	  /// <param name="p2">  right hand operand. </param>
	  /// <param name="res"> absolute stack index of result. </param>
	  /// <returns> false if no tagmethod, true otherwise </returns>
	  private bool call_binTM(Slot p1, Slot p2, Slot res, string @event)
	  {
		object tm = tagmethod(p1.asObject(), @event); // try first operand
		if (isNil(tm))
		{
		  tm = tagmethod(p2.asObject(), @event); // try second operand
		}
		if (!isFunction(tm))
		{
		  return false;
		}
		callTMres(res, tm, p1, p2);
		return true;
	  }

	  /// <returns> -1 if no tagmethod, 0 false, 1 true </returns>
	  private int call_orderTM(Slot p1, Slot p2, string @event)
	  {
		object tm1 = tagmethod(p1.asObject(), @event);
		if (tm1 == NIL) // not metamethod
		{
		  return -1;
		}
		object tm2 = tagmethod(p2.asObject(), @event);
		if (!oRawequal(tm1, tm2)) // different metamethods?
		{
		  return -1;
		}
		Slot s = new Slot();
		callTMres(s, tm1, p1, p2);
		return isFalse(s.r) ? 0 : 1;
	  }

	  private void callTM(object f, object p1, Slot p2, object p3)
	  {
		push(f);
		push(p1);
		push(p2);
		push(p3);
		vmCall(stackSize-4, 0);
	  }

	  private void callTMres(Slot res, object f, Slot p1, Slot p2)
	  {
		push(f);
		push(p1);
		push(p2);
		vmCall(stackSize-3, 1);
		if (D)
		{
			Console.Error.WriteLine("callTMres:" + (stackSize - 1));
		}
		res.r = stack[stackSize-1].r;
		res.d = stack[stackSize-1].d;
		pop(1);
	  }

	  /// <summary>
	  /// Overloaded version of callTMres used by <seealso cref="#vmEqualRef"/>.
	  /// Textuall identical, but a different (overloaded) push method is
	  /// invoked.
	  /// </summary>
	  private void callTMres(Slot res, object f, object p1, object p2)
	  {
		push(f);
		push(p1);
		push(p2);
		vmCall(stackSize-3, 1);
		if (D)
		{
			Console.Error.WriteLine("callTMres" + (stackSize - 1));
		}
		res.r = stack[stackSize-1].r;
		res.d = stack[stackSize-1].d;
		pop(1);
	  }

	  private object get_compTM(LuaTable mt1, LuaTable mt2, string @event)
	  {
		if (mt1 == null)
		{
		  return NIL;
		}
		object tm1 = mt1.getlua(@event);
		if (isNil(tm1))
		{
		  return NIL; // no metamethod
		}
		if (mt1 == mt2)
		{
		  return tm1; // same metatables => same metamethods
		}
		if (mt2 == null)
		{
		  return NIL;
		}
		object tm2 = mt2.getlua(@event);
		if (isNil(tm2))
		{
		  return NIL; // no metamethod
		}
		if (oRawequal(tm1, tm2)) // same metamethods?
		{
		  return tm1;
		}
		return NIL;
	  }

	  /// <summary>
	  /// Gets tagmethod for object. </summary>
	  /// <returns> method or nil. </returns>
	  private object tagmethod(object o, string @event)
	  {
		return getMetafield(o, @event);
	  }

	  /// @deprecated DO NOT CALL 
	  private object tagmethod(Slot o, string @event)
	  {
		throw new System.ArgumentException("tagmethod called");
	  }

	  /// <summary>
	  /// Computes the result of Lua's modules operator (%).  Note that this
	  /// modulus operator does not match Java's %.
	  /// </summary>
	  private static double modulus(double x, double y)
	  {
		return x - Math.Floor(x / y) * y;
	  }

	  /// <summary>
	  /// Changes the stack size, padding with NIL where necessary, and
	  /// allocate a new stack array if necessary.
	  /// </summary>
	  private void stacksetsize(int n)
	  {
		  if (n == 3)
		  {
			  if (D)
			  {
				  Console.Error.WriteLine("stacksetsize:" + n);
			  }
		  }
		// It is absolutely critical that when the stack changes sizes those
		// elements that are common to both old and new stack are unchanged.

		// First implementation of this simply ensures that the stack array
		// has at least the required size number of elements.
		// :todo: consider policies where the stack may also shrink.
		int old = stackSize;
		if (n > stack.Length)
		{
		  int newLength = Math.Max(n, 2 * stack.Length);
		  Slot[] newStack = new Slot[newLength];
		  // Currently the stack only ever grows, so the number of items to
		  // copy is the length of the old stack.
		  int toCopy = stack.Length;
		  Array.Copy(stack, 0, newStack, 0, toCopy);
		  stack = newStack;
		}
		stackSize = n;
		// Nilling out.  The VM requires that fresh stack slots allocated
		// for a new function activation are initialised to nil (which is
		// Lua.NIL, which is not Java null).
		// There are basically two approaches: nil out when the stack grows,
		// or nil out when it shrinks.  Nilling out when the stack grows is
		// slightly simpler, but nilling out when the stack shrinks means
		// that semantic garbage is not retained by the GC.
		// We nil out slots when the stack shrinks, but we also need to make
		// sure they are nil initially.
		// In order to avoid nilling the entire array when we allocate one
		// we maintain a stackhighwater which is 1 more than that largest
		// stack slot that has been nilled.  We use this to nil out stacks
		// slow when we grow.
		if (n <= old)
		{
		  // when shrinking
		  for (int i = n; i < old; ++i)
		  {
			stack[i].r = NIL;
		  }
		}
		if (n > stackhighwater)
		{
		  // when growing above stackhighwater for the first time
		  for (int i = stackhighwater; i < n; ++i)
		  {
			stack[i] = new Slot();
			stack[i].r = NIL;
		  }
		  stackhighwater = n;
		}
	  }

	  /// <summary>
	  /// Pushes a Lua value onto the stack.
	  /// </summary>
	  private void stackAdd(object o)
	  {
		int i = stackSize;
		stacksetsize(i + 1);
		if (D)
		{
			Console.Error.WriteLine("stackAdd:" + i);
		}
		stack[i].Object = o;
	  }

	  /// <summary>
	  /// Copies a slot into a new space in the stack.
	  /// </summary>
	  private void push(Slot p)
	  {
		int i = stackSize;
		stacksetsize(i + 1);
		if (D)
		{
			Console.Error.WriteLine("push:" + i);
		}
		stack[i].r = p.r;
		stack[i].d = p.d;
	  }

	  private void stackInsertAt(object o, int i)
	  {
		int n = stackSize - i;
		stacksetsize(stackSize+1);
		// Copy each slot N into its neighbour N+1.  Loop proceeds from high
		// index slots to lower index slots.
		// A loop from n to 1 copies n slots.
		for (int j = n; j >= 1; --j)
		{
			if (D)
			{
				Console.Error.WriteLine("stackInsertAt:" + (i + j));
			}
		  stack[i + j].r = stack[i + j - 1].r;
		  stack[i + j].d = stack[i + j - 1].d;
		}
		stack[i].Object = o;
	  }

	  /// <summary>
	  /// Equivalent of macro in ldebug.h.
	  /// </summary>
	  private void resethookcount()
	  {
		hookcount = basehookcount;
	  }

	  /// <summary>
	  /// Equivalent of traceexec in lvm.c.
	  /// </summary>
	  private void traceexec(int pc)
	  {
		int mask = hookmask;
		int oldpc = savedpc;
		savedpc = pc;
		if (mask > MASKLINE) // instruction-hook set?
		{
		  if (hookcount == 0)
		  {
			resethookcount();
			dCallhook(HOOKCOUNT, -1);
		  }
		}
		// :todo: line hook.
	  }

	  /// <summary>
	  /// Convert to number.  Returns true if the argument <var>o</var> was
	  /// converted to a number.  Converted number is placed in <var>out[0]</var>.
	  /// Returns
	  /// false if the argument <var>o</var> could not be converted to a number.
	  /// Overloaded.
	  /// </summary>
	  private static bool tonumber(Slot o, double[] @out)
	  {
		if (o.r == NUMBER)
		{
		  @out[0] = o.d;
		  return true;
		}
		if (!(o.r is string))
		{
		  return false;
		}
		if (oStr2d((string)o.r, @out))
		{
		  return true;
		}
		return false;
	  }

	  /// <summary>
	  /// Converts a stack slot to number.  Returns true if the element at
	  /// the specified stack slot was converted to a number.  False
	  /// otherwise.  Note that this actually modifies the element stored at
	  /// <var>idx</var> in the stack (in faithful emulation of the PUC-Rio
	  /// code).  Corrupts <code>NUMOP[0]</code>.  Overloaded. </summary>
	  /// <param name="idx">  absolute stack slot. </param>
	  private bool tonumber(int idx)
	  {
		if (tonumber(stack[idx], NUMOP))
		{
			if (D)
			{
				Console.Error.WriteLine("tonumber:" + idx);
			}
		  stack[idx].d = NUMOP[0];
		  stack[idx].r = NUMBER;
		  return true;
		}
		return false;
	  }

	  /// <summary>
	  /// Convert a pair of operands for an arithmetic opcode.  Stores
	  /// converted results in <code>out[0]</code> and <code>out[1]</code>. </summary>
	  /// <returns> true if and only if both values converted to number. </returns>
	  private static bool toNumberPair(Slot x, Slot y, double[] @out)
	  {
		if (tonumber(y, @out))
		{
		  @out[1] = @out[0];
		  if (tonumber(x, @out))
		  {
			return true;
		  }
		}
		return false;
	  }

	  /// <summary>
	  /// Convert to string.  Returns true if element was number or string
	  /// (the number will have been converted to a string), false otherwise.
	  /// Note this actually modifies the element stored at <var>idx</var> in
	  /// the stack (in faithful emulation of the PUC-Rio code), and when it
	  /// returns <code>true</code>, <code>stack[idx].r instanceof String</code>
	  /// is true.
	  /// </summary>
	  private bool tostring(int idx)
	  {
		// :todo: optimise
		object o = objectAt(idx);
		string s = vmTostring(o);
		if (s == null)
		{
		  return false;
		}
		if (D)
		{
			Console.Error.WriteLine("tostring:" + idx);
		}
		stack[idx].r = s;
		return true;
	  }

	  /// <summary>
	  /// Equivalent to tryfuncTM from ldo.c. </summary>
	  /// <param name="func">  absolute stack index of the function object. </param>
	  private object tryfuncTM(int func)
	  {
		  if (D)
		  {
			  Console.Error.WriteLine("tryfuncTM:" + func);
		  }
		object tm = tagmethod(stack[func].asObject(), "__call");
		if (!isFunction(tm))
		{
		  gTypeerror(stack[func], "call");
		}
		stackInsertAt(tm, func);
		return tm;
	  }

	  /// <summary>
	  /// Lua's is False predicate. </summary>
	  private bool isFalse(object o)
	  {
		return o == NIL || (bool)o == false;
	  }

	  /// @deprecated DO NOT CALL. 
	  private bool isFalse(Slot o)
	  {
		throw new System.ArgumentException("isFalse called");
	  }

	  /// <summary>
	  /// Make new CallInfo record. </summary>
	  private CallInfo inc_ci(int func, int baseArg, int top, int nresults)
	  {
		CallInfo ci = new CallInfo(func, baseArg, top, nresults);
		civ.addElement(ci);
		return ci;
	  }

	  /// <summary>
	  /// Pop topmost CallInfo record and return it. </summary>
	  private CallInfo dec_ci()
	  {
		CallInfo ci = (CallInfo)civ.pop();
		return ci;
	  }

	  /// <summary>
	  /// Equivalent to resume_error from ldo.c </summary>
	  private int resume_error(string msg)
	  {
		stacksetsize(__ci().@base());
		stackAdd(msg);
		return ERRRUN;
	  }

	  /// <summary>
	  /// Return the stack element as an Object.  Converts double values into
	  /// Double objects. </summary>
	  /// <param name="idx">  absolute index into stack (0 <= idx < stackSize). </param>
	  private object objectAt(int idx)
	  {
		object r = stack[idx].r;
		if (r != NUMBER)
		{
		  return r;
		}
		return new double?(stack[idx].d);
	  }

	  /// <summary>
	  /// Sets the stack element.  Double instances are converted to double. </summary>
	  /// <param name="o">  Object to store. </param>
	  /// <param name="idx">  absolute index into stack (0 <= idx < stackSize). </param>
	  private void setObjectAt(object o, int idx)
	  {
		if (o is double?)
		{
			if (D)
			{
				Console.Error.WriteLine("setObjectAt" + idx);
			}
		  stack[idx].r = NUMBER;
		  stack[idx].d = (double)((double?)o);
		  return;
		}
		stack[idx].r = o;
	  }

	  /// <summary>
	  /// Corresponds to ldump's luaU_dump method, but with data gone and writer
	  /// replaced by OutputStream.
	  /// </summary>
	  internal static int uDump(Proto f, OutputStream writer, bool strip)
	  {
		DumpState d = new DumpState(new DataOutputStream(writer), strip);
		d.DumpHeader();
		d.DumpFunction(f, null);
		d.writer.flush();
		return 0; // Any errors result in thrown exceptions.
	  }

	}

	internal sealed class DumpState
	{
	  internal DataOutputStream writer;
	  internal bool strip;

	  internal DumpState(DataOutputStream writer, bool strip)
	  {
		this.writer = writer;
		this.strip = strip;
	  }


	  //////////////// dumper ////////////////////

	  internal void DumpHeader()
	  {
		/*
		 * In order to make the code more compact the dumper re-uses the
		 * header defined in Loader.java.  It has to fix the endianness byte
		 * first.
		 */
		Loader.HEADER[6] = 0;
		writer.write(Loader.HEADER);
	  }

	  private void DumpInt(int i)
	  {
		writer.writeInt(i); // big-endian
	  }

	  private void DumpNumber(double d)
	  {
		writer.writeDouble(d); // big-endian
	  }

	  internal void DumpFunction(Proto f, string p)
	  {
		DumpString((f.source_Renamed == p || strip) ? null : f.source_Renamed);
		DumpInt(f.linedefined_Renamed);
		DumpInt(f.lastlinedefined_Renamed);
		writer.writeByte(f.nups_Renamed);
		writer.writeByte(f.numparams_Renamed);
		writer.writeBoolean(f.Vararg);
		writer.writeByte(f.maxstacksize_Renamed);
		DumpCode(f);
		DumpConstants(f);
		DumpDebug(f);
	  }

	  private void DumpCode(Proto f)
	  {
		int n = f.sizecode;
		int[] code = f.code_Renamed;
		DumpInt(n);
		for (int i = 0 ; i < n ; i++)
		{
		  DumpInt(code[i]);
		}
	  }

	  private void DumpConstants(Proto f)
	  {
		int n = f.sizek;
		Slot[] k = f.k;
		DumpInt(n);
		for (int i = 0 ; i < n ; i++)
		{
		  object o = k[i].r;
		  if (o == Lua.NIL)
		  {
			writer.writeByte(Lua.TNIL);
		  }
		  else if (o is bool?)
		  {
			writer.writeByte(Lua.TBOOLEAN);
			writer.writeBoolean((bool)((bool?)o));
		  }
		  else if (o == Lua.NUMBER)
		  {
			writer.writeByte(Lua.TNUMBER);
			DumpNumber(k[i].d);
		  }
		  else if (o is string)
		  {
			writer.writeByte(Lua.TSTRING);
			DumpString((string)o);
		  }
		  else
		  {
			//# assert false
		  }
		}
		n = f.sizep;
		DumpInt(n);
		for (int i = 0 ; i < n ; i++)
		{
		  Proto subfunc = f.p[i];
		  DumpFunction(subfunc, f.source_Renamed);
		}
	  }

	  private void DumpString(string s)
	  {
		if (s == null)
		{
		  DumpInt(0);
		}
		else
		{
		  /*
		   * Strings are dumped by converting to UTF-8 encoding.  The MIDP
		   * 2.0 spec guarantees that this encoding will be supported (see
		   * page 9 of midp-2_0-fr-spec.pdf).  Nonetheless, any
		   * possible UnsupportedEncodingException is left to be thrown
		   * (it's a subclass of IOException which is declared to be thrown).
		   */
          byte[] contents = new byte[Encoding.GetEncoding("UTF-8").GetByteCount(s)];
          Encoding.GetEncoding("UTF-8").GetBytes(s, 0, s.Length, contents, 0);
		  int size = contents.Length;
		  DumpInt(size+1);
		  writer.write(contents, 0, size);
		  writer.writeByte(0);
		}
	  }

	  private void DumpDebug(Proto f)
	  {
		if (strip)
		{
		  DumpInt(0);
		  DumpInt(0);
		  DumpInt(0);
		  return;
		}

		int n = f.sizelineinfo;
		DumpInt(n);
		for (int i = 0; i < n; i++)
		{
		  DumpInt(f.lineinfo[i]);
		}

		n = f.sizelocvars;
		DumpInt(n);
		for (int i = 0; i < n; i++)
		{
		  LocVar locvar = f.locvars_Renamed[i];
		  DumpString(locvar.varname);
		  DumpInt(locvar.startpc);
		  DumpInt(locvar.endpc);
		}

		n = f.sizeupvalues;
		DumpInt(n);
		for (int i = 0; i < n; i++)
		{
		  DumpString(f.upvalues[i]);
		}
	  }
	}

	internal sealed class Slot
	{
	  internal object r;
	  internal double d;

	  internal Slot()
	  {
	  }

	  internal Slot(Slot s)
	  {
		this.r = s.r;
		this.d = s.d;
	  }

	  internal Slot(object o)
	  {
		this.Object = o;
	  }

	  internal object asObject()
	  {
		if (r == Lua.NUMBER)
		{
		  return new double?(d);
		}
		return r;
	  }

	  internal object Object
	  {
		  set
		  {
			r = value;
			if (value is double?)
			{
			  r = Lua.NUMBER;
			  d = (double)((double?)value);
			  if (Lua.D)
			  {
				  Console.Error.WriteLine("Slot.setObject:" + d);
			  }
			}
		  }
	  }
	}

}