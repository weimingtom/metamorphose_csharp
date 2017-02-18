using System;
using System.Collections;
using System.Text;
using metamorphose.java;

/*  $Header: //info.ravenbrook.com/project/jili/version/1.1/code/mnj/lua/Syntax.java#1 $
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
	/// Syntax analyser.  Lexing, parsing, code generation.
	/// </summary>
	internal sealed class Syntax
	{
	  /// <summary>
	  /// End of File, must be -1 as that is what read() returns. </summary>
	  private const int EOZ = -1;

	  private const int FIRST_RESERVED = 257;

	  // WARNING: if you change the order of this enumeration,
	  // grep "ORDER RESERVED"
	  private static const int TK_AND = FIRST_RESERVED + 0;
	  private static const int TK_BREAK = FIRST_RESERVED + 1;
	  private static const int TK_DO = FIRST_RESERVED + 2;
	  private static const int TK_ELSE = FIRST_RESERVED + 3;
	  private static const int TK_ELSEIF = FIRST_RESERVED + 4;
	  private static const int TK_END = FIRST_RESERVED + 5;
	  private static const int TK_FALSE = FIRST_RESERVED + 6;
	  private static const int TK_FOR = FIRST_RESERVED + 7;
	  private static const int TK_FUNCTION = FIRST_RESERVED + 8;
	  private static const int TK_IF = FIRST_RESERVED + 9;
	  private static const int TK_IN = FIRST_RESERVED + 10;
	  private static const int TK_LOCAL = FIRST_RESERVED + 11;
	  private static const int TK_NIL = FIRST_RESERVED + 12;
	  private static const int TK_NOT = FIRST_RESERVED + 13;
	  private static const int TK_OR = FIRST_RESERVED + 14;
	  private static const int TK_REPEAT = FIRST_RESERVED + 15;
	  private static const int TK_RETURN = FIRST_RESERVED + 16;
	  private static const int TK_THEN = FIRST_RESERVED + 17;
	  private static const int TK_TRUE = FIRST_RESERVED + 18;
	  private static const int TK_UNTIL = FIRST_RESERVED + 19;
	  private static const int TK_WHILE = FIRST_RESERVED + 20;
	  private static const int TK_CONCAT = FIRST_RESERVED + 21;
	  private static const int TK_DOTS = FIRST_RESERVED + 22;
	  private static const int TK_EQ = FIRST_RESERVED + 23;
	  private static const int TK_GE = FIRST_RESERVED + 24;
	  private static const int TK_LE = FIRST_RESERVED + 25;
	  private static const int TK_NE = FIRST_RESERVED + 26;
	  private static const int TK_NUMBER = FIRST_RESERVED + 27;
	  private static const int TK_NAME = FIRST_RESERVED + 28;
	  private static const int TK_STRING = FIRST_RESERVED + 29;
	  private static const int TK_EOS = FIRST_RESERVED + 30;

	  private static const int NUM_RESERVED = TK_WHILE - FIRST_RESERVED + 1;

	  /// <summary>
	  /// Equivalent to luaX_tokens.  ORDER RESERVED </summary>
	  internal static string[] tokens = new string[] {"and", "break", "do", "else", "elseif", "end", "false", "for", "function", "if", "in", "local", "nil", "not", "or", "repeat", "return", "then", "true", "until", "while", "..", "...", "==", ">=", "<=", "~=", "<number>", "<name>", "<string>", "<eof>"};

      internal static metamorphose.java.Hashtable reserved = new metamorphose.java.Hashtable();
	  static Syntax()
	  {
		for (int i = 0; i < NUM_RESERVED; ++i)
		{
		  reserved[tokens[i]] = new int?(FIRST_RESERVED + i);
		}
	  }

	  // From struct LexState

	  /// <summary>
	  /// current character </summary>
	  internal int current;
	  /// <summary>
	  /// input line counter </summary>
	  internal int linenumber = 1;
	  /// <summary>
	  /// line of last token 'consumed' </summary>
	  internal int lastline_Renamed = 1;
	  /// <summary>
	  /// The token value.  For "punctuation" tokens this is the ASCII value
	  /// for the character for the token; for other tokens a member of the
	  /// enum (all of which are > 255).
	  /// </summary>
	  internal int token;
	  /// <summary>
	  /// Semantic info for token; a number. </summary>
	  internal double tokenR;
	  /// <summary>
	  /// Semantic info for token; a string. </summary>
	  internal string tokenS;

	  /// <summary>
	  /// Lookahead token value. </summary>
	  internal int lookahead = TK_EOS;
	  /// <summary>
	  /// Semantic info for lookahead; a number. </summary>
	  internal double lookaheadR;
	  /// <summary>
	  /// Semantic info for lookahead; a string. </summary>
	  internal string lookaheadS;

	  /// <summary>
	  /// Semantic info for return value from <seealso cref="#llex"/>; a number. </summary>
	  internal double semR;
	  /// <summary>
	  /// As <seealso cref="#semR"/>, for string. </summary>
	  internal string semS;

	  /// <summary>
	  /// FuncState for current (innermost) function being parsed. </summary>
	  internal FuncState fs;
	  internal Lua L;

	  /// <summary>
	  /// input stream </summary>
	  private Reader z;

	  /// <summary>
	  /// Buffer for tokens. </summary>
	  internal StringBuilder buff = new StringBuilder();

	  /// <summary>
	  /// current source name </summary>
	  internal string source_Renamed;

	  /// <summary>
	  /// locale decimal point. </summary>
	  private char decpoint = '.';

	  private Syntax(Lua L, Reader z, string source)
	  {
		this.L = L;
		this.z = z;
		this.source_Renamed = source;
		next();
	  }

	  internal int lastline()
	  {
		return lastline_Renamed;
	  }


	  // From <ctype.h>

	  // Implementations of functions from <ctype.h> are only correct copies
	  // to the extent that Lua requires them.
	  // Generally they have default access so that StringLib can see them.
	  // Unlike C's these version are not locale dependent, they use the
	  // ISO-Latin-1 definitions from CLDC 1.1 Character class.

	  internal static bool isalnum(int c)
	  {
		char ch = (char)c;
		return char.IsUpper(ch) || char.IsLower(ch) || char.IsDigit(ch);
	  }

	  internal static bool isalpha(int c)
	  {
		char ch = (char)c;
		return char.IsUpper(ch) || char.IsLower(ch);
	  }

	  /// <summary>
	  /// True if and only if the char (when converted from the int) is a
	  /// control character.
	  /// </summary>
	  internal static bool iscntrl(int c)
	  {
		return (char)c < 0x20 || c == 0x7f;
	  }

	  internal static bool isdigit(int c)
	  {
		return char.IsDigit((char)c);
	  }

	  internal static bool islower(int c)
	  {
		return char.IsLower((char)c);
	  }

	  /// <summary>
	  /// A character is punctuation if not cntrl, not alnum, and not space.
	  /// </summary>
	  internal static bool ispunct(int c)
	  {
		return !isalnum(c) && !iscntrl(c) && !isspace(c);
	  }

	  internal static bool isspace(int c)
	  {
		return c == ' ' || c == '\f' || c == '\n' || c == '\r' || c == '\t';
	  }

	  internal static bool isupper(int c)
	  {
		return char.IsUpper((char)c);
	  }

	  internal static bool isxdigit(int c)
	  {
		return char.IsDigit((char)c) || ('a' <= c && c <= 'f') || ('A' <= c && c <= 'F');
	  }

	  // From llex.c

	  private bool check_next(string set)
	  {
		if (set.IndexOf(current) < 0)
		{
		  return false;
		}
		save_and_next();
		return true;
	  }

	  private bool currIsNewline()
	  {
		return current == '\n' || current == '\r';
	  }

	  private void inclinenumber()
	  {
		int old = current;
		//# assert currIsNewline()
		next(); // skip '\n' or '\r'
		if (currIsNewline() && current != old)
		{
		  next(); // skip '\n\r' or '\r\n'
		}
		if (++linenumber < 0) // overflow
		{
		  xSyntaxerror("chunk has too many lines");
		}
	  }

	  private int skip_sep()
	  {
		int count = 0;
		int s = current;
		//# assert s == '[' || s == ']'
		save_and_next();
		while (current == '=')
		{
		  save_and_next();
		  count++;
		}
		return (current == s) ? count : (-count) - 1;
	  }

	  private void read_long_string(bool isString, int sep)
	  {
		int cont = 0;
		save_and_next(); // skip 2nd `['
		if (currIsNewline()) // string starts with a newline?
		{
		  inclinenumber(); // skip it
		}
		while (true)
		{
		  switch (current)
		  {
			case EOZ:
			  xLexerror(isString ? "unfinished long string" : "unfinished long comment", TK_EOS);
			  break; // to avoid warnings
				goto case ']';
			case ']':
			  if (skip_sep() == sep)
			  {
				save_and_next(); // skip 2nd `]'
				goto loopBreak;
			  }
			  break;

			case '\n':
			case '\r':
			  save('\n');
			  inclinenumber();
			  if (!isString)
			  {
				buff.Length = 0; // avoid wasting space
			  }
			  break;

			default:
			  if (isString)
			  {
				  save_and_next();
			  }
			  else
			  {
				  next();
			  }
			  break;
		  }
		loopContinue:;
		} // loop
	loopBreak:
		if (isString)
		{
		  string rawtoken = buff.ToString();
		  int trim_by = 2 + sep;
		  semS = rawtoken.Substring(trim_by, rawtoken.Length - trim_by - trim_by);
		}
	  }


	  /// <summary>
	  /// Lex a token and return it.  The semantic info for the token is
	  /// stored in <code>this.semR</code> or <code>this.semS</code> as
	  /// appropriate.
	  /// </summary>
	  private int llex()
	  {
		buff.Length = 0;
		while (true)
		{
		  switch (current)
		  {
			case '\n':
			case '\r':
			  inclinenumber();
			  continue;
			case '-':
			  next();
			  if (current != '-')
			  {
				return '-';
			  }
			  /* else is a comment */
			  next();
			  if (current == '[')
			  {
				int sep = skip_sep();
				buff.Length = 0; // `skip_sep' may dirty the buffer
				if (sep >= 0)
				{
				  read_long_string(false, sep); // long comment
				  buff.Length = 0;
				  continue;
				}
			  }
			  /* else short comment */
			  while (!currIsNewline() && current != EOZ)
			  {
				next();
			  }
			  continue;

			case '[':
			  int sep = skip_sep();
			  if (sep >= 0)
			  {
				read_long_string(true, sep);
				return TK_STRING;
			  }
			  else if (sep == -1)
			  {
				return '[';
			  }
			  else
			  {
				xLexerror("invalid long string delimiter", TK_STRING);
			  }
			  continue; // avoids Checkstyle warning.

				goto case '=';
			case '=':
			  next();
			  if (current != '=')
			  {
				  return '=';
			  }
			  else
			  {
				next();
				return TK_EQ;
			  }
			case '<':
			  next();
			  if (current != '=')
			  {
				  return '<';
			  }
			  else
			  {
				next();
				return TK_LE;
			  }
			case '>':
			  next();
			  if (current != '=')
			  {
				  return '>';
			  }
			  else
			  {
				next();
				return TK_GE;
			  }
			case '~':
			  next();
			  if (current != '=')
			  {
				  return '~';
			  }
			  else
			  {
				next();
				return TK_NE;
			  }
			case '"':
			case '\'':
			  read_string(current);
			  return TK_STRING;
			case '.':
			  save_and_next();
			  if (check_next("."))
			  {
				if (check_next("."))
				{
				  return TK_DOTS;
				}
				else
				{
				  return TK_CONCAT;
				}
			  }
			  else if (!isdigit(current))
			  {
				return '.';
			  }
			  else
			  {
				read_numeral();
				return TK_NUMBER;
			  }
			case EOZ:
			  return TK_EOS;
			default:
			  if (isspace(current))
			  {
				// assert !currIsNewline();
				next();
				continue;
			  }
			  else if (isdigit(current))
			  {
				read_numeral();
				return TK_NUMBER;
			  }
			  else if (isalpha(current) || current == '_')
			  {
				// identifier or reserved word
				do
				{
				  save_and_next();
				} while (isalnum(current) || current == '_');
				string s = buff.ToString();
				object t = reserved[s];
				if (t == null)
				{
				  semS = s;
				  return TK_NAME;
				}
				else
				{
				  return (int)((int?)t);
				}
			  }
			  else
			  {
				int c = current;
				next();
				return c; // single-char tokens
			  }
		  }
		}
	  }

	  private void next()
	  {
		current = z.read();
	  }

	  /// <summary>
	  /// Reads number.  Writes to semR. </summary>
	  private void read_numeral()
	  {
		// assert isdigit(current);
		do
		{
		  save_and_next();
		} while (isdigit(current) || current == '.');
		if (check_next("Ee")) // 'E' ?
		{
		  check_next("+-"); // optional exponent sign
		}
		while (isalnum(current) || current == '_')
		{
		  save_and_next();
		}
		// :todo: consider doing PUC-Rio's decimal point tricks.
		try
		{
		  semR = Convert.ToDouble(buff.ToString());
		  return;
		}
		catch (NumberFormatException)
		{
		  xLexerror("malformed number", TK_NUMBER);
		}
	  }

	  /// <summary>
	  /// Reads string.  Writes to semS. </summary>
	  private void read_string(int del)
	  {
		save_and_next();
		while (current != del)
		{
		  switch (current)
		  {
			case EOZ:
			  xLexerror("unfinished string", TK_EOS);
			  continue; // avoid compiler warning
				goto case '\n';
			case '\n':
			case '\r':
			  xLexerror("unfinished string", TK_STRING);
			  continue; // avoid compiler warning
				goto case '\\';
			case '\\':
			{
			  int c;
			  next(); // do not save the '\'
			  switch (current)
			  {
				case 'a': // no '\a' in Java.
					c = 7;
					break;
				case 'b':
					c = '\b';
					break;
				case 'f':
					c = '\f';
					break;
				case 'n':
					c = '\n';
					break;
				case 'r':
					c = '\r';
					break;
				case 't':
					c = '\t';
					break;
				case 'v': // no '\v' in Java.
					c = 11;
					break;
				case '\n':
			case '\r':
				  save('\n');
				  inclinenumber();
				  continue;
				case EOZ:
				  continue; // will raise an error next loop
					goto default;
				default:
				  if (!isdigit(current))
				  {
					save_and_next(); // handles \\, \", \', \?
				  }
				  else // \xxx
				  {
					int i = 0;
					c = 0;
					do
					{
					  c = 10 * c + (current - '0');
					  next();
					} while (++i < 3 && isdigit(current));
					// In unicode, there are no bounds on a 3-digit decimal.
					save(c);
				  }
				  continue;
			  }
			  save(c);
			  next();
			  continue;
			}
			default:
			  save_and_next();
		  break;
		  }
		}
		save_and_next(); // skip delimiter
		string rawtoken = buff.ToString();
		semS = rawtoken.Substring(1, rawtoken.Length - 1 - 1);
	  }

	  private void save()
	  {
		buff.Append((char)current);
	  }

	  private void save(int c)
	  {
		buff.Append((char)c);
	  }

	  private void save_and_next()
	  {
		save();
		next();
	  }

	  /// <summary>
	  /// Getter for source. </summary>
	  internal string source()
	  {
		return source_Renamed;
	  }

	  private string txtToken(int tok)
	  {
		switch (tok)
		{
		  case TK_NAME:
		  case TK_STRING:
		  case TK_NUMBER:
			return buff.ToString();
		  default:
			return xToken2str(tok);
		}
	  }

	  /// <summary>
	  /// Equivalent to <code>luaX_lexerror</code>. </summary>
	  private void xLexerror(string msg, int tok)
	  {
		msg = source_Renamed + ":" + linenumber + ": " + msg;
		if (tok != 0)
		{
		  msg = msg + " near '" + txtToken(tok) + "'";
		}
		L.pushString(msg);
		L.dThrow(Lua.ERRSYNTAX);
	  }

	  /// <summary>
	  /// Equivalent to <code>luaX_next</code>. </summary>
	  private void xNext()
	  {
		lastline_Renamed = linenumber;
		if (lookahead != TK_EOS) // is there a look-ahead token?
		{
		  token = lookahead; // Use this one,
		  tokenR = lookaheadR;
		  tokenS = lookaheadS;
		  lookahead = TK_EOS; // and discharge it.
		}
		else
		{
		  token = llex();
		  tokenR = semR;
		  tokenS = semS;
		}
	  }

	  /// <summary>
	  /// Equivalent to <code>luaX_syntaxerror</code>. </summary>
	  internal void xSyntaxerror(string msg)
	  {
		xLexerror(msg, token);
	  }

	  private static string xToken2str(int token)
	  {
		if (token < FIRST_RESERVED)
		{
		  // assert token == (char)token;
		  if (iscntrl(token))
		  {
			return "char(" + token + ")";
		  }
		  return (new char?((char)token)).ToString();
		}
		return tokens[token - FIRST_RESERVED];
	  }

	  // From lparser.c

	  private static bool block_follow(int token)
	  {
		switch (token)
		{
		  case TK_ELSE:
	  case TK_ELSEIF:
	case TK_END:
		  case TK_UNTIL:
	  case TK_EOS:
			return true;
		  default:
			return false;
		}
	  }

	  private void check(int c)
	  {
		if (token != c)
		{
		  error_expected(c);
		}
	  }

	  /// <param name="what">   the token that is intended to end the match. </param>
	  /// <param name="who">    the token that begins the match. </param>
	  /// <param name="where">  the line number of <var>what</var>. </param>
	  private void check_match(int what, int who, int @where)
	  {
		if (!testnext(what))
		{
		  if (@where == linenumber)
		  {
			error_expected(what);
		  }
		  else
		  {
			xSyntaxerror("'" + xToken2str(what) + "' expected (to close '" + xToken2str(who) + "' at line " + @where + ")");
		  }
		}
	  }

	  private void close_func()
	  {
		removevars(0);
		fs.kRet(0, 0); // final return;
		fs.close();
		// :todo: check this is a valid assertion to make
		//# assert fs != fs.prev
		fs = fs.prev;
	  }


		internal static string opcode_name(int op)
		{
		  switch (op)
		  {
		  case Lua.OP_MOVE:
			  return "MOVE";
		  case Lua.OP_LOADK:
			  return "LOADK";
		  case Lua.OP_LOADBOOL:
			  return "LOADBOOL";
		  case Lua.OP_LOADNIL:
			  return "LOADNIL";
		  case Lua.OP_GETUPVAL:
			  return "GETUPVAL";
		  case Lua.OP_GETGLOBAL:
			  return "GETGLOBAL";
		  case Lua.OP_GETTABLE:
			  return "GETTABLE";
		  case Lua.OP_SETGLOBAL:
			  return "SETGLOBAL";
		  case Lua.OP_SETUPVAL:
			  return "SETUPVAL";
		  case Lua.OP_SETTABLE:
			  return "SETTABLE";
		  case Lua.OP_NEWTABLE:
			  return "NEWTABLE";
		  case Lua.OP_SELF:
			  return "SELF";
		  case Lua.OP_ADD:
			  return "ADD";
		  case Lua.OP_SUB:
			  return "SUB";
		  case Lua.OP_MUL:
			  return "MUL";
		  case Lua.OP_DIV:
			  return "DIV";
		  case Lua.OP_MOD:
			  return "MOD";
		  case Lua.OP_POW:
			  return "POW";
		  case Lua.OP_UNM:
			  return "UNM";
		  case Lua.OP_NOT:
			  return "NOT";
		  case Lua.OP_LEN:
			  return "LEN";
		  case Lua.OP_CONCAT:
			  return "CONCAT";
		  case Lua.OP_JMP:
			  return "JMP";
		  case Lua.OP_EQ:
			  return "EQ";
		  case Lua.OP_LT:
			  return "LT";
		  case Lua.OP_LE:
			  return "LE";
		  case Lua.OP_TEST:
			  return "TEST";
		  case Lua.OP_TESTSET:
			  return "TESTSET";
		  case Lua.OP_CALL:
			  return "CALL";
		  case Lua.OP_TAILCALL:
			  return "TAILCALL";
		  case Lua.OP_RETURN:
			  return "RETURN";
		  case Lua.OP_FORLOOP:
			  return "FORLOOP";
		  case Lua.OP_FORPREP:
			  return "FORPREP";
		  case Lua.OP_TFORLOOP:
			  return "TFORLOOP";
		  case Lua.OP_SETLIST:
			  return "SETLIST";
		  case Lua.OP_CLOSE:
			  return "CLOSE";
		  case Lua.OP_CLOSURE:
			  return "CLOSURE";
		  case Lua.OP_VARARG:
			  return "VARARG";
		  default:
			  return "??" + op;
		  }
		}

	  private void codestring(Expdesc e, string s)
	  {
		e.init(Expdesc.VK, fs.kStringK(s));
	  }

	  private void checkname(Expdesc e)
	  {
		codestring(e, str_checkname());
	  }

	  private void enterlevel()
	  {
		L.nCcalls++;
	  }

	  private void error_expected(int tok)
	  {
		xSyntaxerror("'" + xToken2str(tok) + "' expected");
	  }

	  private void leavelevel()
	  {
		L.nCcalls--;
	  }


	  /// <summary>
	  /// Equivalent to luaY_parser. </summary>
	  internal static Proto parser(Lua L, Reader @in, string name)
	  {
		Syntax ls = new Syntax(L, @in, name);
		FuncState fs = new FuncState(ls);
		ls.open_func(fs);
		fs.f.setIsVararg();
		ls.xNext();
		ls.chunk();
		ls.check(TK_EOS);
		ls.close_func();
		//# assert fs.prev == null
		//# assert fs.f.nups == 0
		//# assert ls.fs == null
		return fs.f;
	  }

	  private void removevars(int tolevel)
	  {
		// :todo: consider making a method in FuncState.
		while (fs.nactvar > tolevel)
		{
		  fs.getlocvar(--fs.nactvar).endpc = fs.pc;
		}
	  }

	  private void singlevar(Expdesc @var)
	  {
		string varname = str_checkname();
		if (singlevaraux(fs, varname, @var, true) == Expdesc.VGLOBAL)
		{
		  @var.Info = fs.kStringK(varname);
		}
	  }

	  private int singlevaraux(FuncState f, string n, Expdesc @var, bool @base)
	  {
		if (f == null) // no more levels?
		{
		  @var.init(Expdesc.VGLOBAL, Lua.NO_REG); // default is global variable
		  return Expdesc.VGLOBAL;
		}
		else
		{
		  int v = f.searchvar(n);
		  if (v >= 0)
		  {
			@var.init(Expdesc.VLOCAL, v);
			if (!@base)
			{
			  f.markupval(v); // local will be used as an upval
			}
			return Expdesc.VLOCAL;
		  }
		  else // not found at current level; try upper one
		  {
			if (singlevaraux(f.prev, n, @var, false) == Expdesc.VGLOBAL)
			{
			  return Expdesc.VGLOBAL;
			}
			@var.upval(indexupvalue(f, n, @var)); // else was LOCAL or UPVAL
			return Expdesc.VUPVAL;
		  }
		}
	  }

	  private string str_checkname()
	  {
		check(TK_NAME);
		string s = tokenS;
		xNext();
		return s;
	  }

	  private bool testnext(int c)
	  {
		if (token == c)
		{
		  xNext();
		  return true;
		}
		return false;
	  }


	  // GRAMMAR RULES

	  private void chunk()
	  {
		// chunk -> { stat [';'] }
		bool islast = false;
		enterlevel();
		while (!islast && !block_follow(token))
		{
		  islast = statement();
		  testnext(';');
		  //# assert fs.f.maxstacksize >= fs.freereg && fs.freereg >= fs.nactvar
		  fs.freereg_Renamed = fs.nactvar;
		}
		leavelevel();
	  }

	  private void constructor(Expdesc t)
	  {
		// constructor -> ??
		int line = linenumber;
		int pc = fs.kCodeABC(Lua.OP_NEWTABLE, 0, 0, 0);
		ConsControl cc = new ConsControl(t);
		t.init(Expdesc.VRELOCABLE, pc);
		cc.v.init(Expdesc.VVOID, 0); // no value (yet)
		fs.kExp2nextreg(t); // fix it at stack top (for gc)
		checknext('{');
		do
		{
		  //# assert cc.v.k == Expdesc.VVOID || cc.tostore > 0
		  if (token == '}')
		  {
			break;
		  }
		  closelistfield(cc);
		  switch (token)
		  {
			case TK_NAME: // may be listfields or recfields
			  xLookahead();
			  if (lookahead != '=') // expression?
			  {
				listfield(cc);
			  }
			  else
			  {
				recfield(cc);
			  }
			  break;

			case '[': // constructor_item -> recfield
			recfield(cc);
			break;

			default: // constructor_part -> listfield
			  listfield(cc);
			  break;
		  }
		} while (testnext(',') || testnext(';'));
		check_match('}', '{', line);
		lastlistfield(cc);
		int[] code = fs.f.code_Renamed;
		code[pc] = Lua.SETARG_B(code[pc], oInt2fb(cc.na)); // set initial array size
		code[pc] = Lua.SETARG_C(code[pc], oInt2fb(cc.nh)); // set initial table size
	  }

	  private static int oInt2fb(int x)
	  {
		int e = 0; // exponent
		while (x < 0 || x >= 16)
		{
		  x = (int)((uint)(x + 1) >> 1);
		  e++;
		}
		return (x < 8) ? x : (((e+1) << 3) | (x - 8));
	  }

	  private void recfield(ConsControl cc)
	  {
		/* recfield -> (NAME | `['exp1`]') = exp1 */
		int reg = fs.freereg_Renamed;
		Expdesc key = new Expdesc();
		Expdesc val = new Expdesc();
		if (token == TK_NAME)
		{
		  // yChecklimit(fs, cc.nh, MAX_INT, "items in a constructor");
		  checkname(key);
		}
		else // token == '['
		{
		  yindex(key);
		}
		cc.nh++;
		checknext('=');
		fs.kExp2RK(key);
		expr(val);
		fs.kCodeABC(Lua.OP_SETTABLE, cc.t.info_Renamed, fs.kExp2RK(key), fs.kExp2RK(val));
		fs.freereg_Renamed = reg; // free registers
	  }

	  private void lastlistfield(ConsControl cc)
	  {
		if (cc.tostore == 0)
		{
		  return;
		}
		if (hasmultret(cc.v.k))
		{
		  fs.kSetmultret(cc.v);
		  fs.kSetlist(cc.t.info_Renamed, cc.na, Lua.MULTRET);
		  cc.na--; // do not count last expression (unknown number of elements)
		}
		else
		{
		  if (cc.v.k != Expdesc.VVOID)
		  {
			fs.kExp2nextreg(cc.v);
		  }
		  fs.kSetlist(cc.t.info_Renamed, cc.na, cc.tostore);
		}
	  }

	  private void closelistfield(ConsControl cc)
	  {
		if (cc.v.k == Expdesc.VVOID)
		{
		  return; // there is no list item
		}
		fs.kExp2nextreg(cc.v);
		cc.v.k = Expdesc.VVOID;
		if (cc.tostore == Lua.LFIELDS_PER_FLUSH)
		{
		  fs.kSetlist(cc.t.info_Renamed, cc.na, cc.tostore); // flush
		  cc.tostore = 0; // no more items pending
		}
	  }

	  private void expr(Expdesc v)
	  {
		subexpr(v, 0);
	  }

	  /// <returns> number of expressions in expression list. </returns>
	  private int explist1(Expdesc v)
	  {
		// explist1 -> expr { ',' expr }
		int n = 1; // at least one expression
		expr(v);
		while (testnext(','))
		{
		  fs.kExp2nextreg(v);
		  expr(v);
		  ++n;
		}
		return n;
	  }

	  private void exprstat()
	  {
		// stat -> func | assignment
		LHSAssign v = new LHSAssign();
		primaryexp(v.v);
		if (v.v.k == Expdesc.VCALL) // stat -> func
		{
		  fs.setargc(v.v, 1); // call statement uses no results
		}
		else // stat -> assignment
		{
		  v.prev = null;
		  assignment(v, 1);
		}
	  }

	  /*
	  ** check whether, in an assignment to a local variable, the local variable
	  ** is needed in a previous assignment (to a table). If so, save original
	  ** local value in a safe place and use this safe copy in the previous
	  ** assignment.
	  */
	  private void check_conflict(LHSAssign lh, Expdesc v)
	  {
		int extra = fs.freereg_Renamed; // eventual position to save local variable
		bool conflict = false;
		for (; lh != null; lh = lh.prev)
		{
		  if (lh.v.k == Expdesc.VINDEXED)
		  {
			if (lh.v.info_Renamed == v.info_Renamed) // conflict?
			{
			  conflict = true;
			  lh.v.info_Renamed = extra; // previous assignment will use safe copy
			}
			if (lh.v.aux_Renamed == v.info_Renamed) // conflict?
			{
			  conflict = true;
			  lh.v.aux_Renamed = extra; // previous assignment will use safe copy
			}
		  }
		}
		if (conflict)
		{
		  fs.kCodeABC(Lua.OP_MOVE, fs.freereg_Renamed, v.info_Renamed, 0); // make copy
		  fs.kReserveregs(1);
		}
	  }

	  private void assignment(LHSAssign lh, int nvars)
	  {
		Expdesc e = new Expdesc();
		int kind = lh.v.k;
		if (!(Expdesc.VLOCAL <= kind && kind <= Expdesc.VINDEXED))
		{
		  xSyntaxerror("syntax error");
		}
		if (testnext(',')) // assignment -> `,' primaryexp assignment
		{
		  LHSAssign nv = new LHSAssign(lh);
		  primaryexp(nv.v);
		  if (nv.v.k == Expdesc.VLOCAL)
		  {
			check_conflict(lh, nv.v);
		  }
		  assignment(nv, nvars + 1);
		}
		else // assignment -> `=' explist1
		{
		  int nexps;
		  checknext('=');
		  nexps = explist1(e);
		  if (nexps != nvars)
		  {
			adjust_assign(nvars, nexps, e);
			if (nexps > nvars)
			{
			  fs.freereg_Renamed -= nexps - nvars; // remove extra values
			}
		  }
		  else
		  {
			fs.kSetoneret(e); // close last expression
			fs.kStorevar(lh.v, e);
			return; // avoid default
		  }
		}
		e.init(Expdesc.VNONRELOC, fs.freereg_Renamed - 1); // default assignment
		fs.kStorevar(lh.v, e);
	  }

	  private void funcargs(Expdesc f)
	  {
		Expdesc args = new Expdesc();
		int line = linenumber;
		switch (token)
		{
		  case '(': // funcargs -> '(' [ explist1 ] ')'
			if (line != lastline_Renamed)
			{
			  xSyntaxerror("ambiguous syntax (function call x new statement)");
			}
			xNext();
			if (token == ')') // arg list is empty?
			{
			  args.Kind = Expdesc.VVOID;
			}
			else
			{
			  explist1(args);
			  fs.kSetmultret(args);
			}
			check_match(')', '(', line);
			break;

		  case '{': // funcargs -> constructor
			constructor(args);
			break;

		  case TK_STRING: // funcargs -> STRING
			codestring(args, tokenS);
			xNext(); // must use tokenS before 'next'
			break;

		  default:
			xSyntaxerror("function arguments expected");
			return;
		}
		// assert (f.kind() == VNONRELOC);
		int nparams;
		int @base = f.info(); // base register for call
		if (args.hasmultret())
		{
		  nparams = Lua.MULTRET; // open call
		}
		else
		{
		  if (args.kind() != Expdesc.VVOID)
		  {
			fs.kExp2nextreg(args); // close last argument
		  }
		  nparams = fs.freereg_Renamed - (@base+1);
		}
		f.init(Expdesc.VCALL, fs.kCodeABC(Lua.OP_CALL, @base, nparams + 1, 2));
		fs.kFixline(line);
		fs.freereg_Renamed = @base+1; // call removes functions and arguments
					// and leaves (unless changed) one result.
	  }

	  private void prefixexp(Expdesc v)
	  {
		// prefixexp -> NAME | '(' expr ')'
		switch (token)
		{
		  case '(':
		  {
			int line = linenumber;
			xNext();
			expr(v);
			check_match(')', '(', line);
			fs.kDischargevars(v);
			return;
		  }
		  case TK_NAME:
			singlevar(v);
			return;
		  default:
			xSyntaxerror("unexpected symbol");
			return;
		}
	  }

	  private void primaryexp(Expdesc v)
	  {
		// primaryexp ->
		//    prefixexp { '.' NAME | '[' exp ']' | ':' NAME funcargs | funcargs }
		prefixexp(v);
		while (true)
		{
		  switch (token)
		  {
			case '.': // field
			  field(v);
			  break;

			case '[': // `[' exp1 `]'
			{
				Expdesc key = new Expdesc();
				fs.kExp2anyreg(v);
				yindex(key);
				fs.kIndexed(v, key);
			}
			  break;

			case ':': // `:' NAME funcargs
			{
				Expdesc key = new Expdesc();
				xNext();
				checkname(key);
				fs.kSelf(v, key);
				funcargs(v);
			}
			  break;

			case '(':
			case TK_STRING:
			case '{': // funcargs
			  fs.kExp2nextreg(v);
			  funcargs(v);
			  break;

			default:
			  return;
		  }
		}
	  }

	  private void retstat()
	  {
		// stat -> RETURN explist
		xNext(); // skip RETURN
		// registers with returned values (first, nret)
		int first = 0;
		int nret;
		if (block_follow(token) || token == ';')
		{
		  // return no values
		  first = 0;
		  nret = 0;
		}
		else
		{
		  Expdesc e = new Expdesc();
		  nret = explist1(e);
		  if (hasmultret(e.k))
		  {
			fs.kSetmultret(e);
			if (e.k == Expdesc.VCALL && nret == 1) // tail call?
			{
			  fs.setcode(e, Lua.SET_OPCODE(fs.getcode(e), Lua.OP_TAILCALL));
			  //# assert Lua.ARGA(fs.getcode(e)) == fs.nactvar
			}
			first = fs.nactvar;
			nret = Lua.MULTRET; // return all values
		  }
		  else
		  {
			if (nret == 1) // only one single value?
			{
			  first = fs.kExp2anyreg(e);
			}
			else
			{
			  fs.kExp2nextreg(e); // values must go to the `stack'
			  first = fs.nactvar; // return all `active' values
			  //# assert nret == fs.freereg - first
			}
		  }
		}
		fs.kRet(first, nret);
	  }

	  private void simpleexp(Expdesc v)
	  {
		// simpleexp -> NUMBER | STRING | NIL | true | false | ... |
		//              constructor | FUNCTION body | primaryexp
		switch (token)
		{
		  case TK_NUMBER:
			v.init(Expdesc.VKNUM, 0);
			v.nval_Renamed = tokenR;
			break;

		  case TK_STRING:
			codestring(v, tokenS);
			break;

		  case TK_NIL:
			v.init(Expdesc.VNIL, 0);
			break;

		  case TK_TRUE:
			v.init(Expdesc.VTRUE, 0);
			break;

		  case TK_FALSE:
			v.init(Expdesc.VFALSE, 0);
			break;

		  case TK_DOTS: // vararg
			if (!fs.f.Vararg)
			{
			  xSyntaxerror("cannot use \"...\" outside a vararg function");
			}
			v.init(Expdesc.VVARARG, fs.kCodeABC(Lua.OP_VARARG, 0, 1, 0));
			break;

		  case '{': // constructor
			constructor(v);
			return;

		  case TK_FUNCTION:
			xNext();
			body(v, false, linenumber);
			return;

		  default:
			primaryexp(v);
			return;
		}
		xNext();
	  }

	  private bool statement()
	  {
		int line = linenumber;
		switch (token)
		{
		  case TK_IF: // stat -> ifstat
			ifstat(line);
			return false;

		  case TK_WHILE: // stat -> whilestat
			whilestat(line);
			return false;

		  case TK_DO: // stat -> DO block END
			xNext(); // skip DO
			block();
			check_match(TK_END, TK_DO, line);
			return false;

		  case TK_FOR: // stat -> forstat
			forstat(line);
			return false;

		  case TK_REPEAT: // stat -> repeatstat
			repeatstat(line);
			return false;

		  case TK_FUNCTION:
			funcstat(line); // stat -> funcstat
			return false;

		  case TK_LOCAL: // stat -> localstat
			xNext(); // skip LOCAL
			if (testnext(TK_FUNCTION)) // local function?
			{
			  localfunc();
			}
			else
			{
			  localstat();
			}
			return false;

		  case TK_RETURN:
			retstat();
			return true; // must be last statement

		  case TK_BREAK: // stat -> breakstat
			xNext(); // skip BREAK
			breakstat();
			return true; // must be last statement

		  default:
			exprstat();
			return false;
		}
	  }

	  // grep "ORDER OPR" if you change these enums.
	  // default access so that FuncState can access them.
	  internal const int OPR_ADD = 0;
	  internal const int OPR_SUB = 1;
	  internal const int OPR_MUL = 2;
	  internal const int OPR_DIV = 3;
	  internal const int OPR_MOD = 4;
	  internal const int OPR_POW = 5;
	  internal const int OPR_CONCAT = 6;
	  internal const int OPR_NE = 7;
	  internal const int OPR_EQ = 8;
	  internal const int OPR_LT = 9;
	  internal const int OPR_LE = 10;
	  internal const int OPR_GT = 11;
	  internal const int OPR_GE = 12;
	  internal const int OPR_AND = 13;
	  internal const int OPR_OR = 14;
	  internal const int OPR_NOBINOPR = 15;

	  internal const int OPR_MINUS = 0;
	  internal const int OPR_NOT = 1;
	  internal const int OPR_LEN = 2;
	  internal const int OPR_NOUNOPR = 3;

	  /// <summary>
	  /// Converts token into binary operator. </summary>
	  private static int getbinopr(int op)
	  {
		switch (op)
		{
		  case '+':
			  return OPR_ADD;
		  case '-':
			  return OPR_SUB;
		  case '*':
			  return OPR_MUL;
		  case '/':
			  return OPR_DIV;
		  case '%':
			  return OPR_MOD;
		  case '^':
			  return OPR_POW;
		  case TK_CONCAT:
			  return OPR_CONCAT;
		  case TK_NE:
			  return OPR_NE;
		  case TK_EQ:
			  return OPR_EQ;
		  case '<':
			  return OPR_LT;
		  case TK_LE:
			  return OPR_LE;
		  case '>':
			  return OPR_GT;
		  case TK_GE:
			  return OPR_GE;
		  case TK_AND:
			  return OPR_AND;
		  case TK_OR:
			  return OPR_OR;
		  default:
			  return OPR_NOBINOPR;
		}
	  }

	  private static int getunopr(int op)
	  {
		switch (op)
		{
		  case TK_NOT:
			  return OPR_NOT;
		  case '-':
			  return OPR_MINUS;
		  case '#':
			  return OPR_LEN;
		  default:
			  return OPR_NOUNOPR;
		}
	  }


	  // ORDER OPR
	  /// <summary>
	  /// Priority table.  left-priority of an operator is
	  /// <code>priority[op][0]</code>, its right priority is
	  /// <code>priority[op][1]</code>.  Please do not modify this table.
	  /// </summary>
	  private static readonly int[][] PRIORITY = new int[][] {new int[] {6, 6}, new int[] {6, 6}, new int[] {7, 7}, new int[] {7, 7}, new int[] {7, 7}, new int[] {10, 9}, new int[] {5, 4}, new int[] {3, 3}, new int[] {3, 3}, new int[] {3, 3}, new int[] {3, 3}, new int[] {3, 3}, new int[] {3, 3}, new int[] {2, 2}, new int[] {1, 1}};

	  /// <summary>
	  /// Priority for unary operators. </summary>
	  private const int UNARY_PRIORITY = 8;

	  /// <summary>
	  /// Operator precedence parser.
	  /// <code>subexpr -> (simpleexp) | unop subexpr) { binop subexpr }</code>
	  /// where <var>binop</var> is any binary operator with a priority
	  /// higher than <var>limit</var>.
	  /// </summary>
	  private int subexpr(Expdesc v, int limit)
	  {
		enterlevel();
		int uop = getunopr(token);
		if (uop != OPR_NOUNOPR)
		{
		  xNext();
		  subexpr(v, UNARY_PRIORITY);
		  fs.kPrefix(uop, v);
		}
		else
		{
		  simpleexp(v);
		}
		// expand while operators have priorities higher than 'limit'
		int op = getbinopr(token);
		while (op != OPR_NOBINOPR && PRIORITY[op][0] > limit)
		{
		  Expdesc v2 = new Expdesc();
		  xNext();
		  fs.kInfix(op, v);
		  // read sub-expression with higher priority
		  int nextop = subexpr(v2, PRIORITY[op][1]);
		  fs.kPosfix(op, v, v2);
		  op = nextop;
		}
		leavelevel();
		return op;
	  }

	  private void enterblock(FuncState f, BlockCnt bl, bool isbreakable)
	  {
		bl.breaklist = FuncState.NO_JUMP;
		bl.isbreakable = isbreakable;
		bl.nactvar = f.nactvar;
		bl.upval = false;
		bl.previous = f.bl;
		f.bl = bl;
		//# assert f.freereg == f.nactvar
	  }

	  private void leaveblock(FuncState f)
	  {
		BlockCnt bl = f.bl;
		f.bl = bl.previous;
		removevars(bl.nactvar);
		if (bl.upval)
		{
		  f.kCodeABC(Lua.OP_CLOSE, bl.nactvar, 0, 0);
		}
		/* loops have no body */
		//# assert (!bl.isbreakable) || (!bl.upval)
		//# assert bl.nactvar == f.nactvar
		f.freereg_Renamed = f.nactvar; // free registers
		f.kPatchtohere(bl.breaklist);
	  }


	/*
	** {======================================================================
	** Rules for Statements
	** =======================================================================
	*/

	  private void block()
	  {
		/* block -> chunk */
		BlockCnt bl = new BlockCnt();
		enterblock(fs, bl, false);
		chunk();
		//# assert bl.breaklist == FuncState.NO_JUMP
		leaveblock(fs);
	  }

	  private void breakstat()
	  {
		BlockCnt bl = fs.bl;
		bool upval = false;
		while (bl != null && !bl.isbreakable)
		{
		  upval |= bl.upval;
		  bl = bl.previous;
		}
		if (bl == null)
		{
		  xSyntaxerror("no loop to break");
		}
		if (upval)
		{
		  fs.kCodeABC(Lua.OP_CLOSE, bl.nactvar, 0, 0);
		}
		bl.breaklist = fs.kConcat(bl.breaklist, fs.kJump());
	  }

	  private void funcstat(int line)
	  {
		/* funcstat -> FUNCTION funcname body */
		Expdesc b = new Expdesc();
		Expdesc v = new Expdesc();
		xNext(); // skip FUNCTION
		bool needself = funcname(v);
		body(b, needself, line);
		fs.kStorevar(v, b);
		fs.kFixline(line); // definition `happens' in the first line
	  }

	  private void checknext(int c)
	  {
		check(c);
		xNext();
	  }

	  private void parlist()
	  {
		/* parlist -> [ param { `,' param } ] */
		Proto f = fs.f;
		int nparams = 0;
		if (token != ')') // is `parlist' not empty?
		{
		  do
		  {
			switch (token)
			{
			  case TK_NAME: // param -> NAME
			  {
				new_localvar(str_checkname(), nparams++);
				break;
			  }
			  case TK_DOTS: // param -> `...'
			  {
				xNext();
				f.setIsVararg();
				break;
			  }
			  default:
				  xSyntaxerror("<name> or '...' expected");
			  break;
			}
		  } while ((!f.Vararg) && testnext(','));
		}
		adjustlocalvars(nparams);
		f.numparams_Renamed = fs.nactvar; // VARARG_HASARG not now used
		fs.kReserveregs(fs.nactvar); // reserve register for parameters
	  }


	  private LocVar getlocvar(int i)
	  {
		FuncState fstate = fs;
		return fstate.f.locvars_Renamed [fstate.actvar[i]];
	  }

	  private void adjustlocalvars(int nvars)
	  {
		fs.nactvar += (short)nvars;
		for (; nvars != 0; nvars--)
		{
		  getlocvar(fs.nactvar - nvars).startpc = fs.pc;
		}
	  }

	  private void new_localvarliteral(string v, int n)
	  {
		new_localvar(v, n);
	  }

	  private void errorlimit(int limit, string what)
	  {
		string msg = fs.f.linedefined_Renamed == 0 ? "main function has more than " + limit + " " + what : "function at line " + fs.f.linedefined_Renamed + " has more than " + limit + " " + what;
		xLexerror(msg, 0);
	  }


	  private void yChecklimit(int v, int l, string m)
	  {
		if (v > l)
		{
		  errorlimit(l,m);
		}
	  }

	  private void new_localvar(string name, int n)
	  {
		yChecklimit(fs.nactvar + n + 1, Lua.MAXVARS, "local variables");
		fs.actvar[fs.nactvar + n] = (short)registerlocalvar(name);
	  }

	  private int registerlocalvar(string varname)
	  {
		Proto f = fs.f;
		f.ensureLocvars(L, fs.nlocvars, short.MaxValue);
		f.locvars_Renamed[fs.nlocvars].varname = varname;
		return fs.nlocvars++;
	  }

	  private void body(Expdesc e, bool needself, int line)
	  {
		/* body ->  `(' parlist `)' chunk END */
		FuncState new_fs = new FuncState(this);
		open_func(new_fs);
		new_fs.f.linedefined_Renamed = line;
		checknext('(');
		if (needself)
		{
		  new_localvarliteral("self", 0);
		  adjustlocalvars(1);
		}
		parlist();
		checknext(')');
		chunk();
		new_fs.f.lastlinedefined_Renamed = linenumber;
		check_match(TK_END, TK_FUNCTION, line);
		close_func();
		pushclosure(new_fs, e);
	  }

	  private int UPVAL_K(int upvaldesc)
	  {
		return ((int)((uint)upvaldesc >> 8)) & 0xFF;
	  }
	  private int UPVAL_INFO(int upvaldesc)
	  {
		return upvaldesc & 0xFF;
	  }
	  private int UPVAL_ENCODE(int k, int info)
	  {
		//# assert (k & 0xFF) == k && (info & 0xFF) == info
		return ((k & 0xFF) << 8) | (info & 0xFF);
	  }


	  private void pushclosure(FuncState func, Expdesc v)
	  {
		Proto f = fs.f;
		f.ensureProtos(L, fs.np);
		Proto ff = func.f;
		f.p[fs.np++] = ff;
		v.init(Expdesc.VRELOCABLE, fs.kCodeABx(Lua.OP_CLOSURE, 0, fs.np - 1));
		for (int i = 0; i < ff.nups_Renamed; i++)
		{
		  int upvalue = func.upvalues[i];
		  int o = (UPVAL_K(upvalue) == Expdesc.VLOCAL) ? Lua.OP_MOVE : Lua.OP_GETUPVAL;
		  fs.kCodeABC(o, 0, UPVAL_INFO(upvalue), 0);
		}
	  }

	  private bool funcname(Expdesc v)
	  {
		/* funcname -> NAME {field} [`:' NAME] */
		bool needself = false;
		singlevar(v);
		while (token == '.')
		{
		  field(v);
		}
		if (token == ':')
		{
		  needself = true;
		  field(v);
		}
		return needself;
	  }

	  private void field(Expdesc v)
	  {
		/* field -> ['.' | ':'] NAME */
		Expdesc key = new Expdesc();
		fs.kExp2anyreg(v);
		xNext(); // skip the dot or colon
		checkname(key);
		fs.kIndexed(v, key);
	  }

	  private void repeatstat(int line)
	  {
		/* repeatstat -> REPEAT block UNTIL cond */
		int repeat_init = fs.kGetlabel();
		BlockCnt bl1 = new BlockCnt();
		BlockCnt bl2 = new BlockCnt();
		enterblock(fs, bl1, true); // loop block
		enterblock(fs, bl2, false); // scope block
		xNext(); // skip REPEAT
		chunk();
		check_match(TK_UNTIL, TK_REPEAT, line);
		int condexit = cond(); // read condition (inside scope block)
		if (!bl2.upval) // no upvalues?
		{
		  leaveblock(fs); // finish scope
		  fs.kPatchlist(condexit, repeat_init); // close the loop
		}
		else // complete semantics when there are upvalues
		{
		  breakstat(); // if condition then break
		  fs.kPatchtohere(condexit); // else...
		  leaveblock(fs); // finish scope...
		  fs.kPatchlist(fs.kJump(), repeat_init); // and repeat
		}
		leaveblock(fs); // finish loop
	  }

	  private int cond()
	  {
		/* cond -> exp */
		Expdesc v = new Expdesc();
		expr(v); // read condition
		if (v.k == Expdesc.VNIL)
		{
		  v.k = Expdesc.VFALSE; // `falses' are all equal here
		}
		fs.kGoiftrue(v);
		return v.f;
	  }

	  private void open_func(FuncState funcstate)
	  {
		Proto f = new Proto(source_Renamed, 2); // registers 0/1 are always valid
		funcstate.f = f;
		funcstate.ls = this;
		funcstate.L = L;

		funcstate.prev = this.fs; // linked list of funcstates
		this.fs = funcstate;
	  }

	  private void localstat()
	  {
		/* stat -> LOCAL NAME {`,' NAME} [`=' explist1] */
		int nvars = 0;
		int nexps;
		Expdesc e = new Expdesc();
		do
		{
		  new_localvar(str_checkname(), nvars++);
		} while (testnext(','));
		if (testnext('='))
		{
		  nexps = explist1(e);
		}
		else
		{
		  e.k = Expdesc.VVOID;
		  nexps = 0;
		}
		adjust_assign(nvars, nexps, e);
		adjustlocalvars(nvars);
	  }

	  private void forstat(int line)
	  {
		/* forstat -> FOR (fornum | forlist) END */
		BlockCnt bl = new BlockCnt();
		enterblock(fs, bl, true); // scope for loop and control variables
		xNext(); // skip `for'
		string varname = str_checkname(); // first variable name
		switch (token)
		{
		  case '=':
			fornum(varname, line);
			break;
		  case ',':
		  case TK_IN:
			forlist(varname);
			break;
		  default:
			xSyntaxerror("\"=\" or \"in\" expected");
		break;
		}
		check_match(TK_END, TK_FOR, line);
		leaveblock(fs); // loop scope (`break' jumps to this point)
	  }

	  private void fornum(string varname, int line)
	  {
		/* fornum -> NAME = exp1,exp1[,exp1] forbody */
		int @base = fs.freereg_Renamed;
		new_localvarliteral("(for index)", 0);
		new_localvarliteral("(for limit)", 1);
		new_localvarliteral("(for step)", 2);
		new_localvar(varname, 3);
		checknext('=');
		exp1(); // initial value
		checknext(',');
		exp1(); // limit
		if (testnext(','))
		{
		  exp1(); // optional step
		}
		else // default step = 1
		{
		  fs.kCodeABx(Lua.OP_LOADK, fs.freereg_Renamed, fs.kNumberK(1));
		  fs.kReserveregs(1);
		}
		forbody(@base, line, 1, true);
	  }

	  private int exp1()
	  {
		Expdesc e = new Expdesc();
		expr(e);
		int k = e.k;
		fs.kExp2nextreg(e);
		return k;
	  }

	  private void forlist(string indexname)
	  {
		/* forlist -> NAME {,NAME} IN explist1 forbody */
		Expdesc e = new Expdesc();
		int nvars = 0;
		int @base = fs.freereg_Renamed;
		/* create control variables */
		new_localvarliteral("(for generator)", nvars++);
		new_localvarliteral("(for state)", nvars++);
		new_localvarliteral("(for control)", nvars++);
		/* create declared variables */
		new_localvar(indexname, nvars++);
		while (testnext(','))
		{
		  new_localvar(str_checkname(), nvars++);
		}
		checknext(TK_IN);
		int line = linenumber;
		adjust_assign(3, explist1(e), e);
		fs.kCheckstack(3); // extra space to call generator
		forbody(@base, line, nvars - 3, false);
	  }

	  private void forbody(int @base, int line, int nvars, bool isnum)
	  {
		/* forbody -> DO block */
		BlockCnt bl = new BlockCnt();
		adjustlocalvars(3); // control variables
		checknext(TK_DO);
		int prep = isnum ? fs.kCodeAsBx(Lua.OP_FORPREP, @base, FuncState.NO_JUMP) : fs.kJump();
		enterblock(fs, bl, false); // scope for declared variables
		adjustlocalvars(nvars);
		fs.kReserveregs(nvars);
		block();
		leaveblock(fs); // end of scope for declared variables
		fs.kPatchtohere(prep);
		int endfor = isnum ? fs.kCodeAsBx(Lua.OP_FORLOOP, @base, FuncState.NO_JUMP) : fs.kCodeABC(Lua.OP_TFORLOOP, @base, 0, nvars);
		fs.kFixline(line); // pretend that `OP_FOR' starts the loop
		fs.kPatchlist((isnum ? endfor : fs.kJump()), prep + 1);
	  }

	  private void ifstat(int line)
	  {
		/* ifstat -> IF cond THEN block {ELSEIF cond THEN block} [ELSE block] END */
		int escapelist = FuncState.NO_JUMP;
		int flist = test_then_block(); // IF cond THEN block
		while (token == TK_ELSEIF)
		{
		  escapelist = fs.kConcat(escapelist, fs.kJump());
		  fs.kPatchtohere(flist);
		  flist = test_then_block(); // ELSEIF cond THEN block
		}
		if (token == TK_ELSE)
		{
		  escapelist = fs.kConcat(escapelist, fs.kJump());
		  fs.kPatchtohere(flist);
		  xNext(); // skip ELSE (after patch, for correct line info)
		  block(); // `else' part
		}
		else
		{
		  escapelist = fs.kConcat(escapelist, flist);
		}

		fs.kPatchtohere(escapelist);
		check_match(TK_END, TK_IF, line);
	  }

	  private int test_then_block()
	  {
		/* test_then_block -> [IF | ELSEIF] cond THEN block */
		xNext(); // skip IF or ELSEIF
		int condexit = cond();
		checknext(TK_THEN);
		block(); // `then' part
		return condexit;
	  }

	  private void whilestat(int line)
	  {
		/* whilestat -> WHILE cond DO block END */
		BlockCnt bl = new BlockCnt();
		xNext(); // skip WHILE
		int whileinit = fs.kGetlabel();
		int condexit = cond();
		enterblock(fs, bl, true);
		checknext(TK_DO);
		block();
		fs.kPatchlist(fs.kJump(), whileinit);
		check_match(TK_END, TK_WHILE, line);
		leaveblock(fs);
		fs.kPatchtohere(condexit); // false conditions finish the loop
	  }

	  private static bool hasmultret(int k)
	  {
		return k == Expdesc.VCALL || k == Expdesc.VVARARG;
	  }

	  private void adjust_assign(int nvars, int nexps, Expdesc e)
	  {
		int extra = nvars - nexps;
		if (hasmultret(e.k))
		{
		  extra++; // includes call itself
		  if (extra < 0)
		  {
			extra = 0;
		  }
		  fs.kSetreturns(e, extra); // last exp. provides the difference
		  if (extra > 1)
		  {
			fs.kReserveregs(extra - 1);
		  }
		}
		else
		{
		  if (e.k != Expdesc.VVOID)
		  {
			fs.kExp2nextreg(e); // close last expression
		  }
		  if (extra > 0)
		  {
			int reg = fs.freereg_Renamed;
			fs.kReserveregs(extra);
			fs.kNil(reg, extra);
		  }
		}
	  }

	  private void localfunc()
	  {
		Expdesc b = new Expdesc();
		new_localvar(str_checkname(), 0);
		Expdesc v = new Expdesc(Expdesc.VLOCAL, fs.freereg_Renamed);
		fs.kReserveregs(1);
		adjustlocalvars(1);
		body(b, false, linenumber);
		fs.kStorevar(v, b);
		/* debug information will only see the variable after this point! */
		fs.getlocvar(fs.nactvar - 1).startpc = fs.pc;
	  }

	  private void yindex(Expdesc v)
	  {
		/* index -> '[' expr ']' */
		xNext(); // skip the '['
		expr(v);
		fs.kExp2val(v);
		checknext(']');
	  }

	  internal void xLookahead()
	  {
		//# assert lookahead == TK_EOS
		lookahead = llex();
		lookaheadR = semR;
		lookaheadS = semS;
	  }

	  private void listfield(ConsControl cc)
	  {
		expr(cc.v);
		yChecklimit(cc.na, Lua.MAXARG_Bx, "items in a constructor");
		cc.na++;
		cc.tostore++;
	  }

	  private int indexupvalue(FuncState funcstate, string name, Expdesc v)
	  {
		Proto f = funcstate.f;
		int oldsize = f.sizeupvalues;
		for (int i = 0; i < f.nups_Renamed; i++)
		{
		  int entry = funcstate.upvalues[i];
		  if (UPVAL_K(entry) == v.k && UPVAL_INFO(entry) == v.info_Renamed)
		  {
			//# assert name.equals(f.upvalues[i])
			return i;
		  }
		}
		/* new one */
		yChecklimit(f.nups_Renamed + 1, Lua.MAXUPVALUES, "upvalues");
		f.ensureUpvals(L, f.nups_Renamed);
		f.upvalues[f.nups_Renamed] = name;
		//# assert v.k == Expdesc.VLOCAL || v.k == Expdesc.VUPVAL
		funcstate.upvalues[f.nups_Renamed] = UPVAL_ENCODE(v.k, v.info_Renamed);
		return f.nups_Renamed++;
	  }
	}

	internal sealed class LHSAssign
	{
	  internal LHSAssign prev;
	  internal Expdesc v = new Expdesc();

	  internal LHSAssign()
	  {
	  }
	  internal LHSAssign(LHSAssign prev)
	  {
		this.prev = prev;
	  }
	}

	internal sealed class ConsControl
	{
	  internal Expdesc v = new Expdesc(); // last list item read
	  internal Expdesc t; // table descriptor
	  internal int nh; // total number of `record' elements
	  internal int na; // total number of array elements
	  internal int tostore; // number of array elements pending to be stored

	  internal ConsControl(Expdesc t)
	  {
		this.t = t;
	  }
	}

}