using metamorphose.java;
using System.Text;

/*  $Header: //info.ravenbrook.com/project/jili/version/1.1/code/mnj/lua/Loader.java#1 $
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
	/// Loads Lua 5.1 binary chunks.
	/// This loader is restricted to loading Lua 5.1 binary chunks where:
	/// <ul>
	/// <li><code>LUAC_VERSION</code> is <code>0x51</code>.</li>
	/// <li><code>int</code> is 32 bits.</li>
	/// <li><code>size_t</code> is 32 bits.</li>
	/// <li><code>Instruction</code> is 32 bits (this is a type defined in
	/// the PUC-Rio Lua).</li>
	/// <li><code>lua_Number</code> is an IEEE 754 64-bit double.  Suitable
	/// for passing to <seealso cref="java.lang.Double#longBitsToDouble"/>.</li>
	/// <li>endianness does not matter (the loader swabs as appropriate).</li>
	/// </ul>
	/// Any Lua chunk compiled by a stock Lua 5.1 running on a 32-bit Windows
	/// PC or at 32-bit OS X machine should be fine.
	/// </summary>
	internal sealed class Loader
	{
	  /// <summary>
	  /// Whether integers in the binary chunk are stored big-endian or
	  /// little-endian.  Recall that the number 0x12345678 is stored: 0x12
	  /// 0x34 0x56 0x78 in big-endian format; and, 0x78 0x56 0x34 0x12 in
	  /// little-endian format.
	  /// </summary>
	  private bool bigendian;
	  private InputStream @in;
	  private string name;

	  // auxiliary for reading ints/numbers
	  private byte[] intbuf = new byte[4];
	  private byte[] longbuf = new byte[8];

	  /// <summary>
	  /// A new chunk loader.  The <code>InputStream</code> must be
	  /// positioned at the beginning of the <code>LUA_SIGNATURE</code> that
	  /// marks the beginning of a Lua binary chunk. </summary>
	  /// <param name="in">    The binary stream from which the chunk is read. </param>
	  /// <param name="name">  The name of the chunk. </param>
	  internal Loader(InputStream @in, string name)
	  {
		if (null == @in)
		{
		  throw new System.NullReferenceException();
		}
		this.@in = @in;
		// The name is treated slightly.  See lundump.c in the PUC-Rio
		// source for details.
		if (name.StartsWith("@") || name.StartsWith("="))
		{
		  this.name = name.Substring(1);
		}
		else if (false)
		{
		  // :todo: Select some equivalent for the binary string case.
		  this.name = "binary string";
		}
		else
		{
		  this.name = name;
		}
	  }

	  /// <summary>
	  /// Loads (undumps) a dumped binary chunk. </summary>
	  /// <exception cref="IOException">  if chunk is malformed or unacceptable. </exception>
	  internal Proto undump()
	  {
		this.header();
		return this.function(null);
	  }


	  /// <summary>
	  /// Primitive reader for undumping.
	  /// Reads exactly enough bytes from <code>this.in</code> to fill the
	  /// array <code>b</code>.  If there aren't enough to fill
	  /// <code>b</code> then an exception is thrown.  Similar to
	  /// <code>LoadBlock</code> from PUC-Rio's <code>lundump.c</code>. </summary>
	  /// <param name="b">  byte array to fill. </param>
	  /// <exception cref="EOFException"> when the stream is exhausted too early. </exception>
	  /// <exception cref="IOException"> when the underlying stream does. </exception>
	  private void block(byte[] b)
	  {
		int n = @in.read(b);
		if (n != b.Length)
		{
		  throw new EOFException();
		}
	  }

	  /// <summary>
	  /// Undumps a byte as an 8 bit unsigned number.  Returns
	  /// an int to accommodate the range.
	  /// </summary>
	  private int byteLoad()
	  {
		int c = @in.read();
		if (c == -1)
		{
		  throw new EOFException();
		}
		else
		{
		  return c & 0xFF; // paranoia
		}
	  }

	  /// <summary>
	  /// Undumps the code for a <code>Proto</code>.  The code is an array of
	  /// VM instructions.
	  /// </summary>
	  private int[] code()
	  {
		int n = intLoad();
		int[] code = new int[n];

		for (int i = 0; i < n; ++i)
		{
		  // :Instruction:size  Here we assume that a dumped Instruction is
		  // the same size as a dumped int.
		  code[i] = intLoad();
		}

		return code;
	  }

	  /// <summary>
	  /// Undumps the constant array contained inside a <code>Proto</code>
	  /// object.  First half of <code>LoadConstants</code>, see
	  /// <code>proto</code> for the second half of
	  /// <code>LoadConstants</code>.
	  /// </summary>
	  private Slot[] constant()
	  {
		int n = intLoad();
		Slot[] k = new Slot[n];

		// Load each constant one by one.  We use the following values for
		// the Lua tagtypes (taken from <code>lua.h</code> from the PUC-Rio
		// Lua 5.1 distribution):
		// LUA_TNIL         0
		// LUA_TBOOLEAN     1
		// LUA_TNUMBER      3
		// LUA_TSTRING      4
		// All other tagtypes are invalid

		// :todo: Currently a new Slot is created for each constant.
		// Consider a space optimisation whereby identical constants have
		// the same Slot.  Constants are pooled per function anyway (so a
		// function never has 2 identical constants), so would have to work
		// across functions.  The easy cases of nil, true, false, might be
		// worth doing since that doesn't require a global table.
		// 
		for (int i = 0; i < n; ++i)
		{
		  int t = byteLoad();
		  switch (t)
		  {
			case 0: // LUA_TNIL
			  k[i] = new Slot(Lua.NIL);
			  break;

			case 1: // LUA_TBOOLEAN
			  int b = byteLoad();
			  // assert b >= 0;
			  if (b > 1)
			  {
				throw new IOException();
			  }

			  k[i] = new Slot(Lua.valueOfBoolean(b != 0));
			  break;

			case 3: // LUA_TNUMBER
			  k[i] = new Slot(number());
			  break;

			case 4: // LUA_TSTRING
			  k[i] = new Slot(@string());
			  break;

			default:
			  throw new IOException();
		  }
		}

		return k;
	  }

	  /// <summary>
	  /// Undumps the debug info for a <code>Proto</code>. </summary>
	  /// <param name="proto">  The Proto instance to which debug info will be added. </param>
	  private void debug(Proto proto)
	  {
		// lineinfo
		int n = intLoad();
		int[] lineinfo = new int[n];

		for (int i = 0; i < n; ++i)
		{
		  lineinfo[i] = intLoad();
		}

		// locvars
		n = intLoad();
		LocVar[] locvar = new LocVar[n];
		for (int i = 0; i < n; ++i)
		{
		  string s = @string();
		  int start = intLoad();
		  int end = intLoad();
		  locvar[i] = new LocVar(s, start, end);
		}

		// upvalue (names)
		n = intLoad();
		string[] upvalue = new string[n];
		for (int i = 0; i < n; ++i)
		{
		  upvalue[i] = @string();
		}

		proto.debug(lineinfo, locvar, upvalue);

		return;
	  }

	  /// <summary>
	  /// Undumps a Proto object.  This is named 'function' after
	  /// <code>LoadFunction</code> in PUC-Rio's <code>lundump.c</code>. </summary>
	  /// <param name="parentSource">  Name of parent source "file". </param>
	  /// <exception cref="IOException">  when binary is malformed. </exception>
	  private Proto function(string parentSource)
	  {
		string source;
		int linedefined;
		int lastlinedefined;
		int nups;
		int numparams;
		int varargByte;
		bool vararg;
		int maxstacksize;
		int[] code;
		Slot[] constant;
		Proto[] proto;

		source = this.@string();
		if (null == source)
		{
		  source = parentSource;
		}
		linedefined = this.intLoad();
		lastlinedefined = this.intLoad();
		nups = this.byteLoad();
		numparams = this.byteLoad();
		varargByte = this.byteLoad();
		// "is_vararg" is a 3-bit field, with the following bit meanings
		// (see "lobject.h"):
		// 1 - VARARG_HASARG
		// 2 - VARARG_ISVARARG
		// 4 - VARARG_NEEDSARG
		// Values 1 and 4 (bits 0 and 2) are only used for 5.0
		// compatibility.
		// HASARG indicates that a function was compiled in 5.0
		// compatibility mode and is declared to have ... in its parameter
		// list.
		// NEEDSARG indicates that a function was compiled in 5.0
		// compatibility mode and is declared to have ... in its parameter
		// list and does _not_ use the 5.1 style of vararg access (using ...
		// as an expression).  It is assumed to use 5.0 style vararg access
		// (the local 'arg' variable).  This is not supported in Jill.
		// ISVARARG indicates that a function has ... in its parameter list
		// (whether compiled in 5.0 compatibility mode or not).
		//
		// At runtime NEEDSARG changes the protocol for calling a vararg
		// function.  We don't support this, so we check that it is absent
		// here in the loader.
		//
		// That means that the legal values for this field ar 0,1,2,3.
		if (varargByte < 0 || varargByte > 3)
		{
		  throw new IOException();
		}
		vararg = (0 != varargByte);
		maxstacksize = this.byteLoad();
		code = this.code();
		constant = this.constant();
		proto = this.proto(source);
		Proto newProto = new Proto(constant, code, proto, nups, numparams, vararg, maxstacksize);
		newProto.Source = source;
		newProto.Linedefined = linedefined;
		newProto.Lastlinedefined = lastlinedefined;

		this.debug(newProto);
		// :todo: call code verifier
		return newProto;
	  }

	  private const int HEADERSIZE = 12;

	  /// <summary>
	  /// A chunk header that is correct.  Except for the endian byte, at
	  /// index 6, which is always overwritten with the one from the file,
	  /// before comparison.  We cope with either endianness.
	  /// Default access so that <seealso cref="Lua#load"/> can read the first entry.
	  /// On no account should anyone except <seealso cref="#header"/> modify
	  /// this array.
	  /// </summary>
	  internal static readonly byte[] HEADER = new byte[] {0x1B, (byte)'L', (byte)'u', (byte)'a', 0x51, 0, 99, 4, 4, 4, 8, 0};
      
	  /// <summary>
	  /// Loads and checks the binary chunk header.  Sets
	  /// <code>this.bigendian</code> accordingly.
	  /// 
	  /// A Lua 5.1 header looks like this:
	  /// <pre>
	  /// b[0]    0x33
	  /// b[1..3] "Lua";
	  /// b[4]    0x51 (LUAC_VERSION)
	  /// b[5]    0 (LUAC_FORMAT)
	  /// b[6]    0 big-endian, 1 little-endian
	  /// b[7]    4 (sizeof(int))
	  /// b[8]    4 (sizeof(size_t))
	  /// b[9]    4 (sizeof(Instruction))
	  /// b[10]   8 (sizeof(lua_Number))
	  /// b[11]   0 (floating point)
	  /// </pre>
	  /// 
	  /// To conserve JVM bytecodes the sizes of the types <code>int</code>,
	  /// <code>size_t</code>, <code>Instruction</code>,
	  /// <code>lua_Number</code> are assumed by the code to be 4, 4, 4, and
	  /// 8, respectively.  Where this assumption is made the tags :int:size,
	  /// :size_t:size :Instruction:size :lua_Number:size will appear so that
	  /// you can grep for them, should you wish to modify this loader to
	  /// load binary chunks from different architectures.
	  /// </summary>
	  /// <exception cref="IOException">  when header is malformed or not suitable. </exception>
	  private void header()
	  {
		byte[] buf = new byte[HEADERSIZE];
		/*int n;*/

		block(buf);

		// poke the HEADER's endianness byte and compare.
		HEADER[6] = buf[6];

		if (buf[6] < 0 || buf[6] > 1 || !arrayEquals(HEADER, buf))
		{
		  throw new IOException();
		}
		bigendian = (buf[6] == 0);
	  }

	  /// <summary>
	  /// Undumps an int.  This method swabs accordingly.
	  /// size_t and Instruction need swabbing too, but the code
	  /// simply uses this method to load size_t and Instruction.
	  /// </summary>
	  private int intLoad()
	  {
		// :int:size  Here we assume an int is 4 bytes.
		block(intbuf);

		int i;
		// Caution: byte is signed so "&0xff" converts to unsigned value.
		if (bigendian)
		{
		  i = ((intbuf[0] & 0xff) << 24) | ((intbuf[1] & 0xff) << 16) | ((intbuf[2] & 0xff) << 8) | (intbuf[3] & 0xff);
		}
		else
		{
		  i = ((intbuf[3] & 0xff) << 24) | ((intbuf[2] & 0xff) << 16) | ((intbuf[1] & 0xff) << 8) | (intbuf[0] & 0xff);
		}
		return i;

		/* minimum footprint version?
		int result = 0 ;
		for (int shift = 0 ; shift < 32 ; shift+=8)
		{
		  int byt = byteLoad () ;
		  if (bigendian)
		    result = (result << 8) | byt ;
		  else
		    result |= byt << shift ;
		}
		return result ;
		*/

		/* another version?
		if (bigendian)
		{
		  int result = byteLoad() << 24 ;
		  result |= byteLoad () << 16 ;
		  result |= byteLoad () << 8 ;
		  result |= byteLoad () ;
		  return result;
		}
		else
		{
		  int result = byteLoad() ;
		  result |= byteLoad () << 8 ;
		  result |= byteLoad () << 16 ;
		  result |= byteLoad () << 24 ;
		  return result ;
		}
		*/
	  }

	  /// <summary>
	  /// Undumps a Lua number.  Which is assumed to be a 64-bit IEEE double.
	  /// </summary>
	  private object number()
	  {
		// :lua_Number:size  Here we assume that the size is 8.
		block(longbuf);
		// Big-endian architectures store doubles with the sign bit first;
		// little-endian is the other way around.
		long l = 0;
		for (int i = 0; i < 8; ++i)
		{
		  if (bigendian)
		  {
			l = (l << 8) | (longbuf[i] & 0xff);
		  }
		  else
		  {
			l = ((long)((ulong)l >> 8)) | (((long)(longbuf[i] & 0xff)) << 56);
		  }
		}
        double d = l;//double.longBitsToDouble(l);
		return Lua.valueOfNumber(d);
	  }

	  /// <summary>
	  /// Undumps the <code>Proto</code> array contained inside a
	  /// <code>Proto</code> object.  These are the <code>Proto</code>
	  /// objects for all inner functions defined inside an existing
	  /// function.  Corresponds to the second half of PUC-Rio's
	  /// <code>LoadConstants</code> function.  See <code>constant</code> for
	  /// the first half.
	  /// </summary>
	  private Proto[] proto(string source)
	  {
		int n = intLoad();
		Proto[] p = new Proto[n];

		for (int i = 0; i < n; ++i)
		{
		  p[i] = function(source);
		}
		return p;
	  }

	  /// <summary>
	  /// Undumps a <seealso cref="String"/> or <code>null</code>.  As per
	  /// <code>LoadString</code> in
	  /// PUC-Rio's lundump.c.  Strings are converted from the binary
	  /// using the UTF-8 encoding, using the {@link
	  /// java.lang.String#String(byte[], String) String(byte[], String)}
	  /// constructor.
	  /// </summary>
	  private string @string()
	  {
		// :size_t:size we assume that size_t is same size as int.
		int size = intLoad();
		if (size == 0)
		{
		  return null;
		}

		byte[] buf = new byte[size-1];
		block(buf);
		// Discard trailing NUL byte
		if (@in.read() == -1)
		{
		  throw new EOFException();
		}

		return Encoding.GetEncoding("UTF-8").GetString(buf);
      }

	  /// <summary>
	  /// CLDC 1.1 does not provide <code>java.util.Arrays</code> so we make
	  /// do with this.
	  /// </summary>
	  private static bool arrayEquals(byte[] x, byte[] y)
	  {
		if (x.Length != y.Length)
		{
		  return false;
		}
		for (int i = 0; i < x.Length; ++i)
		{
		  if (x[i] != y[i])
		  {
			return false;
		  }
		}
		return true;
	  }
	}



}