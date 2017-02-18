using System;
using metamorphose.java;

/*  $Header: //info.ravenbrook.com/project/jili/version/1.1/code/mnj/lua/MathLib.java#1 $
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
	/// Contains Lua's math library.
	/// The library can be opened using the <seealso cref="#open"/> method.
	/// Because this library is implemented on top of CLDC 1.1 it is not as
	/// complete as the PUC-Rio math library.  Trigononmetric inverses
	/// (EG <code>acos</code>) and hyperbolic trigonometric functions (EG
	/// <code>cosh</code>) are not provided.
	/// </summary>
	public sealed class MathLib : LuaJavaCallback
	{
	  // Each function in the library corresponds to an instance of
	  // this class which is associated (the 'which' member) with an integer
	  // which is unique within this class.  They are taken from the following
	  // set.

	  private const int ABS = 1;
	  //private static final int acos = 2;
	  //private static final int asin = 3;
	  //private static final int atan2 = 4;
	  //private static final int atan = 5;
	  private const int CEIL = 6;
	  //private static final int cosh = 7;
	  private const int COS = 8;
	  private const int DEG = 9;
	  private const int EXP = 10;
	  private const int FLOOR = 11;
	  private const int FMOD = 12;
	  //private static final int frexp = 13;
	  //private static final int ldexp = 14;
	  //private static final int log = 15;
	  private const int MAX = 16;
	  private const int MIN = 17;
	  private const int MODF = 18;
	  private const int POW = 19;
	  private const int RAD = 20;
	  private const int RANDOM = 21;
	  private const int RANDOMSEED = 22;
	  //private static final int sinh = 23;
	  private const int SIN = 24;
	  private const int SQRT = 25;
	  //private static final int tanh = 26;
	  private const int TAN = 27;

	  private static Random rng = new Random();

	  /// <summary>
	  /// Which library function this object represents.  This value should
	  /// be one of the "enums" defined in the class.
	  /// </summary>
	  private int which;

	  /// <summary>
	  /// Constructs instance, filling in the 'which' member. </summary>
	  private MathLib(int which)
	  {
		this.which = which;
	  }

	  /// <summary>
	  /// Implements all of the functions in the Lua math library.  Do not
	  /// call directly. </summary>
	  /// <param name="L">  the Lua state in which to execute. </param>
	  /// <returns> number of returned parameters, as per convention. </returns>
	  public override int luaFunction(Lua L)
	  {
		switch (which)
		{
		  case ABS:
			return abs(L);
		  case CEIL:
			return ceil(L);
		  case COS:
			return cos(L);
		  case DEG:
			return deg(L);
		  case EXP:
			return exp(L);
		  case FLOOR:
			return floor(L);
		  case FMOD:
			return fmod(L);
		  case MAX:
			return max(L);
		  case MIN:
			return min(L);
		  case MODF:
			return modf(L);
		  case POW:
			return pow(L);
		  case RAD:
			return rad(L);
		  case RANDOM:
			return random(L);
		  case RANDOMSEED:
			return randomseed(L);
		  case SIN:
			return sin(L);
		  case SQRT:
			return sqrt(L);
		  case TAN:
			return tan(L);
		}
		return 0;
	  }

	  /// <summary>
	  /// Opens the library into the given Lua state.  This registers
	  /// the symbols of the library in the global table. </summary>
	  /// <param name="L">  The Lua state into which to open. </param>
	  public static void open(Lua L)
	  {
		LuaTable t = L.register("math");

		r(L, "abs", ABS);
		r(L, "ceil", CEIL);
		r(L, "cos", COS);
		r(L, "deg", DEG);
		r(L, "exp", EXP);
		r(L, "floor", FLOOR);
		r(L, "fmod", FMOD);
		r(L, "max", MAX);
		r(L, "min", MIN);
		r(L, "modf", MODF);
		r(L, "pow", POW);
		r(L, "rad", RAD);
		r(L, "random", RANDOM);
		r(L, "randomseed", RANDOMSEED);
		r(L, "sin", SIN);
		r(L, "sqrt", SQRT);
		r(L, "tan", TAN);

		L.setField(t, "pi", Lua.valueOfNumber(Math.PI));
		L.setField(t, "huge", Lua.valueOfNumber(double.PositiveInfinity));
	  }

	  /// <summary>
	  /// Register a function. </summary>
	  private static void r(Lua L, string name, int which)
	  {
		MathLib f = new MathLib(which);
		L.setField(L.getGlobal("math"), name, f);
	  }

	  private static int abs(Lua L)
	  {
		L.pushNumber(Math.Abs(L.checkNumber(1)));
		return 1;
	  }

	  private static int ceil(Lua L)
	  {
		L.pushNumber(Math.Ceiling(L.checkNumber(1)));
		return 1;
	  }

	  private static int cos(Lua L)
	  {
		L.pushNumber(Math.Cos(L.checkNumber(1)));
		return 1;
	  }

	  private static int deg(Lua L)
	  {
		L.pushNumber(MathUtil.toDegrees(L.checkNumber(1)));
		return 1;
	  }

	  private static int exp(Lua L)
	  {
		// CLDC 1.1 has Math.E but no exp, pow, or log.  Bizarre.
		L.pushNumber(Lua.iNumpow(Math.E, L.checkNumber(1)));
		return 1;
	  }

	  private static int floor(Lua L)
	  {
		L.pushNumber(Math.Floor(L.checkNumber(1)));
		return 1;
	  }

	  private static int fmod(Lua L)
	  {
		L.pushNumber(L.checkNumber(1) % L.checkNumber(2));
		return 1;
	  }

	  private static int max(Lua L)
	  {
		int n = L.Top; // number of arguments
		double dmax = L.checkNumber(1);
		for (int i = 2; i <= n; ++i)
		{
		  double d = L.checkNumber(i);
		  dmax = Math.Max(dmax, d);
		}
		L.pushNumber(dmax);
		return 1;
	  }

	  private static int min(Lua L)
	  {
		int n = L.Top; // number of arguments
		double dmin = L.checkNumber(1);
		for (int i = 2; i <= n; ++i)
		{
		  double d = L.checkNumber(i);
		  dmin = Math.Min(dmin, d);
		}
		L.pushNumber(dmin);
		return 1;
	  }

	  private static int modf(Lua L)
	  {
		double x = L.checkNumber(1);
		double fp = x % 1;
		double ip = x - fp;
		L.pushNumber(ip);
		L.pushNumber(fp);
		return 2;
	  }

	  private static int pow(Lua L)
	  {
		L.pushNumber(Lua.iNumpow(L.checkNumber(1), L.checkNumber(2)));
		return 1;
	  }

	  private static int rad(Lua L)
	  {
		L.pushNumber(MathUtil.toRadians(L.checkNumber(1)));
		return 1;
	  }

	  private static int random(Lua L)
	  {
		// It would seem better style to associate the java.util.Random
		// instance with the Lua instance (by implementing and using a
		// registry for example).  However, PUC-rio uses the ISO C library
		// and so will share the same random number generator across all Lua
		// states.  So we do too.
		switch (L.Top) // check number of arguments
		{
		  case 0: // no arguments
			L.pushNumber(rng.NextDouble());
			break;

		  case 1: // only upper limit
		  {
			  int u = L.checkInt(1);
			  L.argCheck(1 <= u, 1, "interval is empty");
			  L.pushNumber(rng.Next(u) + 1);
		  }
			break;

		  case 2: // lower and upper limits
		  {
			  int l = L.checkInt(1);
			  int u = L.checkInt(2);
			  L.argCheck(l <= u, 2, "interval is empty");
			  L.pushNumber(rng.Next(u) + l);
		  }
			break;

		  default:
			return L.error("wrong number of arguments");
		}
		return 1;
	  }

	  private static int randomseed(Lua L)
	  {
        rng = new Random((int)L.checkNumber(1));
		return 0;
	  }

	  private static int sin(Lua L)
	  {
		L.pushNumber(Math.Sin(L.checkNumber(1)));
		return 1;
	  }

	  private static int sqrt(Lua L)
	  {
		L.pushNumber(Math.Sqrt(L.checkNumber(1)));
		return 1;
	  }

	  private static int tan(Lua L)
	  {
		L.pushNumber(Math.Tan(L.checkNumber(1)));
		return 1;
	  }
	}

}