using System;
using System.Text;
using metamorphose.java;

/*  $Header: //info.ravenbrook.com/project/jili/version/1.1/code/mnj/lua/OSLib.java#1 $
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

// REFERENCES
// [C1990] "ISO Standard: Programming languages - C"; ISO 9899:1990;

namespace metamorphose.lua
{


	/// <summary>
	/// The OS Library.  Can be opened into a <seealso cref="Lua"/> state by invoking
	/// the <seealso cref="#open"/> method.
	/// </summary>
	public sealed class OSLib : LuaJavaCallback
	{
	  // Each function in the library corresponds to an instance of
	  // this class which is associated (the 'which' member) with an integer
	  // which is unique within this class.  They are taken from the following
	  // set.
	  private const int CLOCK = 1;
	  private const int DATE = 2;
	  private const int DIFFTIME = 3;
	  // EXECUTE = 4;
	  // EXIT = 5;
	  private const int GETENV = 6; //FIXME:added
	  // REMOVE = 7;
	  // RENAME = 8;
	  private const int SETLOCALE = 9;
	  private const int TIME = 10;

	  /// <summary>
	  /// Which library function this object represents.  This value should
	  /// be one of the "enums" defined in the class.
	  /// </summary>
	  private int which;

	  /// <summary>
	  /// Constructs instance, filling in the 'which' member. </summary>
	  private OSLib(int which)
	  {
		this.which = which;
	  }

	  /// <summary>
	  /// Implements all of the functions in the Lua os library (that are
	  /// provided).  Do not call directly. </summary>
	  /// <param name="L">  the Lua state in which to execute. </param>
	  /// <returns> number of returned parameters, as per convention. </returns>
	  public override int luaFunction(Lua L)
	  {
		switch (which)
		{
		  case CLOCK:
			return clock(L);
		  case DATE:
			return date(L);
		  case DIFFTIME:
			return difftime(L);
		  case GETENV: //FIXME:
			return getenv(L);
		  case SETLOCALE:
			return setlocale(L);
		  case TIME:
			return time(L);
		}
		return 0;
	  }

	  /// <summary>
	  /// Opens the library into the given Lua state.  This registers
	  /// the symbols of the library in the table "os". </summary>
	  /// <param name="L">  The Lua state into which to open. </param>
	  public static void open(Lua L)
	  {
		L.register("os");

		r(L, "clock", CLOCK);
		r(L, "date", DATE);
		r(L, "difftime", DIFFTIME);
		r(L, "getenv", GETENV); //FIXME:added
		r(L, "setlocale", SETLOCALE);
		r(L, "time", TIME);
	  }

	  /// <summary>
	  /// Register a function. </summary>
	  private static void r(Lua L, string name, int which)
	  {
		OSLib f = new OSLib(which);
		object lib = L.getGlobal("os");
		L.setField(lib, name, f);
	  }

      private static readonly long T0 = SystemUtil.currentTimeMillis();

	  /// <summary>
	  /// Implements clock.  Java provides no way to get CPU time, so we
	  /// return the amount of wall clock time since this class was loaded.
	  /// </summary>
	  private static int clock(Lua L)
	  {
          double d = (double)SystemUtil.currentTimeMillis();
		d = d - T0;
		d /= 1000;

		L.pushNumber(d);
		return 1;
	  }

	  /// <summary>
	  /// Implements date. </summary>
	  private static int date(Lua L)
	  {
		long t;
		if (L.isNoneOrNil(2))
		{
            t = SystemUtil.currentTimeMillis();
		}
		else
		{
		  t = (long)L.checkNumber(2);
		}

		string s = L.optString(1, "%c");
		TimeZone tz = TimeZone.Default;
		if (s.StartsWith("!"))
		{
		  tz = TimeZone.getTimeZone("GMT");
		  s = s.Substring(1);
		}

		DateTime c = DateTime.getInstance(tz);
		c = new DateTime(new DateTime(t));

		if (s.Equals("*t"))
		{
		  L.push(L.createTable(0, 8)); // 8 = number of fields
		  setfield(L, "sec", c.Second);
		  setfield(L, "min", c.Minute);
		  setfield(L, "hour", c.Hour);
		  setfield(L, "day", c.Day);
		  setfield(L, "month", canonicalmonth(c.Month));
		  setfield(L, "year", c.Year);
		  setfield(L, "wday", canonicalweekday(c.DayOfWeek));
		  // yday is not supported because CLDC 1.1 does not provide it.
		  // setfield(L, "yday", c.get("???"));
		  if (tz.useDaylightTime())
		  {
			// CLDC 1.1 does not provide any way to determine isdst, so we set
			// it to -1 (which in C means that the information is not
			// available).
			setfield(L, "isdst", -1);
		  }
		  else
		  {
			// On the other hand if the timezone does not do DST then it
			// can't be in effect.
			setfield(L, "isdst", 0);
		  }
		}
		else
		{
		  StringBuilder b = new StringBuilder();
		  int i = 0;
		  int l = s.Length;
		  while (i < l)
		  {
			char ch = s[i];
			++i;
			if (ch != '%')
			{
			  b.Append(ch);
			  continue;
			}
			if (i >= l)
			{
			  break;
			}
			ch = s[i];
			++i;
			// Generally in order to save space, the abbreviated forms are
			// identical to the long forms.
			// The specifiers are from [C1990].
			switch (ch)
			{
			  case 'a':
		  case 'A':
				b.Append(weekdayname(c));
				break;
			  case 'b':
		  case 'B':
				b.Append(monthname(c));
				break;
			  case 'c':
				b.Append(c.Ticks.ToString());
				//FIXME:should be this
				//b.append(String.format("%tc", c));
				break;
			  case 'd':
				b.Append(format(c.Day, 2));
				break;
			  case 'H':
				b.Append(format(c.Hour, 2));
				break;
			  case 'I':
			  {
				  int h = c.Hour;
				  h = (h + 11) % 12 + 1; // force into range 1-12
				  b.Append(format(h, 2));
			  }
				break;
			  case 'j':
			  case 'U':
		  case 'W':
				// Not supported because CLDC 1.1 doesn't provide it.
				b.Append('%');
				b.Append(ch);
				break;
			  case 'm':
			  {
				  int m = canonicalmonth(c.Month);
				  b.Append(format(m, 2));
			  }
				break;
			  case 'M':
				b.Append(format(c.Minute, 2));
				break;
			  case 'p':
			  {
				  int h = c.Hour;
				  b.Append(h < 12 ? "am" : "pm");
			  }
				break;
			  case 'S':
				b.Append(format(c.Second, 2));
				break;
			  case 'w':
				b.Append(canonicalweekday(c.DayOfWeek));
				break;
			  case 'x':
			  {
				  string u = c.Ticks.ToString();
				  // We extract fields from the result of Date.toString.
				  // The output of which is of the form:
				  // dow mon dd hh:mm:ss zzz yyyy
				  // except that zzz is optional.
				  b.Append(u.Substring(0, 11));
				  b.Append(c.Year);
			  }
				break;
			  case 'X':
			  {
				  string u = c.Ticks.ToString();
				  b.Append(u.Substring(11, u.Length - 5 - 11));
			  }
				break;
			  case 'y':
				b.Append(format(c.Year % 100, 2));
				break;
			  case 'Y':
				b.Append(c.Year);
				break;
			  case 'Z':
				b.Append(tz.ID);
				break;
			  case '%':
				b.Append('%');
				break;
			}
		  } // while
		  L.pushString(b.ToString());
		}
		return 1;
	  }

	  /// <summary>
	  /// Implements difftime. </summary>
	  private static int difftime(Lua L)
	  {
		L.pushNumber((L.checkNumber(1) - L.optNumber(2, 0)) / 1000);
		return 1;
	  }

	  // Incredibly, the spec doesn't give a numeric value and range for
	  // Calendar.JANUARY through to Calendar.DECEMBER.
	  /// <summary>
	  /// Converts from 0-11 to required Calendar value.  DO NOT MODIFY THIS
	  /// ARRAY.
	  /// </summary>
	  private static readonly int[] MONTH = new int[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12};

	  /// <summary>
	  /// Implements setlocale. </summary>
	  private static int setlocale(Lua L)
	  {
		if (L.isNoneOrNil(1))
		{
		  L.pushString("");
		}
		else
		{
		  L.pushNil();
		}
		return 1;
	  }

	  /// <summary>
	  /// Implements time. </summary>
	  private static int time(Lua L)
	  {
		if (L.isNoneOrNil(1)) // called without args?
		{
            L.pushNumber(SystemUtil.currentTimeMillis());
		  return 1;
		}
		L.checkType(1, Lua.TTABLE);
		L.Top = 1; // make sure table is at the top
		DateTime c = new DateTime();
		c.set(DateTime.SECOND, getfield(L, "sec", 0));
		c.set(DateTime.MINUTE, getfield(L, "min", 0));
		c.set(DateTime.HOUR, getfield(L, "hour", 12));
		c.set(DateTime.DAY_OF_MONTH, getfield(L, "day", -1));
		c.set(DateTime.MONTH, MONTH[getfield(L, "month", -1) - 1]);
		c.set(DateTime.YEAR, getfield(L, "year", -1));
		// ignore isdst field
		L.pushNumber(c.Ticks.Time);
		return 1;
	  }

	  private static int getfield(Lua L, string key, int d)
	  {
		object o = L.getField(L.value(-1), key);
		if (L.isNumber(o))
		{
		  return (int)L.toNumber(o);
		}
		if (d < 0)
		{
		  return L.error("field '" + key + "' missing in date table");
		}
		return d;
	  }

	  private static void setfield(Lua L, string key, int value)
	  {
		L.setField(L.value(-1), key, Lua.valueOfNumber(value));
	  }

	  /// <summary>
	  /// Format a positive integer in a 0-filled field of width
	  /// <var>w</var>.
	  /// </summary>
	  private static string format(int i, int w)
	  {
		StringBuilder b = new StringBuilder();
		b.Append(i);
		while (b.Length < w)
		{
		  b.Insert(0, '0');
		}
		return b.ToString();
	  }

	  private static string weekdayname(DateTime c)
	  {
		string s = c.Ticks.ToString();
		return s.Substring(0, 3);
	  }

	  private static string monthname(DateTime c)
	  {
		string s = c.Ticks.ToString();
		return s.Substring(4, 3);
	  }

	  /// <summary>
	  /// (almost) inverts the conversion provided by <seealso cref="#MONTH"/>.  Converts
	  /// from a <seealso cref="Calendar"/> value to a month in the range 1-12. </summary>
	  /// <param name="m">  a value from the enum Calendar.JANUARY, Calendar.FEBRUARY, etc </param>
	  /// <returns> a month in the range 1-12, or the original value. </returns>
	  private static int canonicalmonth(int m)
	  {
		for (int i = 0; i < MONTH.Length; ++i)
		{
		  if (m == MONTH[i])
		  {
			return i + 1;
		  }
		}
		return m;
	  }

	  // DO NOT MODIFY ARRAY
	  private static readonly int[] WEEKDAY = new int[] {DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday};

	  /// <summary>
	  /// Converts from a <seealso cref="Calendar"/> value to a weekday in the range
	  /// 0-6 where 0 is Sunday (as per the convention used in [C1990]). </summary>
	  /// <param name="w">  a value from the enum Calendar.SUNDAY, Calendar.MONDAY, etc </param>
	  /// <returns> a weekday in the range 0-6, or the original value. </returns>
	  private static int canonicalweekday(int w)
	  {
		for (int i = 0; i < WEEKDAY.Length; ++i)
		{
		  if (w == WEEKDAY[i])
		  {
			return i;
		  }
		}
		return w;
	  }


	  //FIXME:added
	  private static int getenv(Lua L)
	  {
		string name = L.checkString(1);
		string value = System.getenv(name);
		if (value == null)
		{
			L.pushNil();
		}
		else
		{
			L.pushString(value);
		}
		return 1;
	  }
	}

}