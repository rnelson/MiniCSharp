/* $Id$
 * Ross Nelson
 * Compiler Construction (CSC 446)
 * MiniCSharp/Globals
 */

#region Using declarations
using System;
using System.Text;
using System.Text.RegularExpressions;
using nsClearConsole;
#endregion

namespace MiniCSharp
{
	public class Globals
	{
		/* "global" variables for Program.cs */
		// none

		/* "global" variables for Lexical.cs */
		public static string lexeme;				/* the lexeme GetNextToken() finds */
		public static int value;					/* integer value */
		public static double valueF;				/* floating point value */
		public static string literal;				/* a string literal */
		public const char eof = '\uffff';			/* EOF in C#: ^E --- ???*/
		public static string [] ReservedWords;		/* the reserved words list */
		public static string [] Tokens;				/* textual representation of tokens */
		public static int linecount;				/* the number of lines printed to the screen */
		public static int curLine;					/* the current line number */
		public static string filename;				/* the filename */
		
		public static int depth;					/* the depth we're at */
		public static HashTable symTab;				/* symbol table */
		public static StringTable strTab;			/* string table */
		
		public static bool VISUAL;					/* print TAC/asm to the screen? */

		/* Symbol - token type */
		public enum Symbol : ushort
		{
			_class,			/* 'class' token*/
			_if,			/* 'if' token */
			_new,			/* 'new' token */
			_foreach,		/* 'foreach' token */
			_const,			/* 'const' token */
			_float,			/* 'float' token */
			_null,			/* 'null' token */
			_in,			/* 'in' token */
			_public,		/* 'public' token */
			_private,		/* 'private' token */
			_return,		/* 'return' token */
			_this,			/* 'this' token */
			_using,			/* 'using' token */
			_char,			/* 'char' token */
			_else,			/* 'else' token */
			_int,			/* 'int' token */
			_static,		/* 'static' token */
			_namespace,		/* 'namespace' token */
			_void,			/* 'void' token */
			_ref,			/* 'ref' token */
			_out,			/* 'out' token */
			_read,			/* 'read' token */
			_write,			/* 'write' token */
			_writeln,		/* 'writeln' token */
			_relop,			/* relational operator token */					// < > <= >=
			_condop,		/* conditional operator token */				// == !=
			_unarynot,		/* unary not token */							// !
			_addop,			/* addition operator token */					// +
			_signop,		/* negation/subtraction operator token */		// -
			_mulop,			/* multiplication/division operator token */	// * /
			_assignop,		/* assignment operator token */					// =
			_orop,			/* or operator token */							// ||
			_borop,			/* binary or operator token */					// |
			_andop,			/* and operator token */						// &&
			_bandop,		/* binary and operator token */					// &
			_lparen,		/* left parenthesis token */					// (
			_rparen,		/* right parenthesis token */					// )
			_lbrace,		/* left french brace token */					// {
			_rbrace,		/* right french brace token */					// }
			_lbracket,		/* left bracket */								// [
			_rbracket,		/* right bracket */								// ]
			_comma,			/* comma token */								// ,
			_colon,			/* colon token */								// :
			_semicolon,		/* semicolon token */							// ;
			_period,		/* period token */								// .
			_quote,			/* single quote token */						// '
			_dquote,		/* double quote token */						// "
			_number,		/* numerical value token */
			_numfloat,		/* floating point value token */
			_literal,		/* literal characters/strings */
			_eof,			/* end of file token */
			_identifier,	/* identifier token */
			_constructor,	/* constructors */
			_comment,		/* a comment - ignore it */
			_unknown,		/* unknown token */
		}

		/** for Lexical **/
		public static Globals.Symbol token;	/* token type */
		
		/* Wait()
		 * 
		 * Method to pause and clear the screen
		 */
		public static void Wait(string message)
		{
			ClearConsole clrscr = new ClearConsole();
			System.Console.WriteLine(message);
			System.Console.ReadLine();
			clrscr.Clear();
		}
		
		/* Initialize()
		 * 
		 * This method *MUST* be called before Globals is used, it sets
		 * anything up for the Globals class that has to be done at runtime.
		 */
		public static void Initialize()
		{
			int index = 0;
			token = Globals.Symbol._unknown;
			
			/* set the initial line count */
			linecount = 0;
			
			/* we've not yet read a line... */
			curLine = 0;
			
			/* set our outer depth */
			depth = 0;

			/* create the symbol table */
			symTab = new HashTable();
			
			/* create the string table */
			strTab = new StringTable();
			
			/* let's be quiet */
			VISUAL = false;
			
			/* create memory for the reserved words */
			ReservedWords = new string[24];
			
			/* set all of the reserved words */
			ReservedWords[index++] = "class";
			ReservedWords[index++] = "if";
			ReservedWords[index++] = "new";
			ReservedWords[index++] = "foreach";
			ReservedWords[index++] = "const";
			ReservedWords[index++] = "float";
			ReservedWords[index++] = "null";
			ReservedWords[index++] = "in";
			ReservedWords[index++] = "public";
			ReservedWords[index++] = "private";
			ReservedWords[index++] = "return";
			ReservedWords[index++] = "this";
			ReservedWords[index++] = "using";
			ReservedWords[index++] = "char";
			ReservedWords[index++] = "else";
			ReservedWords[index++] = "int";
			ReservedWords[index++] = "static";
			ReservedWords[index++] = "namespace";
			ReservedWords[index++] = "void";
			ReservedWords[index++] = "ref";
			ReservedWords[index++] = "out";
			ReservedWords[index++] = "read";
			ReservedWords[index++] = "write";
			ReservedWords[index++] = "writeln";
			
			/* do the same for Tokens */
			index = 0;
			Tokens = new string[55];
			Tokens[index++] = "class";
			Tokens[index++] = "if";
			Tokens[index++] = "new";
			Tokens[index++] = "foreach";
			Tokens[index++] = "const";
			Tokens[index++] = "float";
			Tokens[index++] = "null";
			Tokens[index++] = "in";
			Tokens[index++] = "public";
			Tokens[index++] = "private";
			Tokens[index++] = "return";
			Tokens[index++] = "this";
			Tokens[index++] = "using";
			Tokens[index++] = "char";
			Tokens[index++] = "else";
			Tokens[index++] = "int";
			Tokens[index++] = "static";
			Tokens[index++] = "namespace";
			Tokens[index++] = "void";
			Tokens[index++] = "ref";
			Tokens[index++] = "out";
			Tokens[index++] = "read";
			Tokens[index++] = "write";
			Tokens[index++] = "writeln";
			Tokens[index++] = "relop";
			Tokens[index++] = "condop";
			Tokens[index++] = "unarynot";
			Tokens[index++] = "addop";
			Tokens[index++] = "signop";
			Tokens[index++] = "mulop";
			Tokens[index++] = "assignop";
			Tokens[index++] = "orop";
			Tokens[index++] = "borop";
			Tokens[index++] = "andop";
			Tokens[index++] = "bandorop";
			Tokens[index++] = "lparen";
			Tokens[index++] = "rparen";
			Tokens[index++] = "lbrace";
			Tokens[index++] = "rbrace";
			Tokens[index++] = "lbracket";
			Tokens[index++] = "rbracket";
			Tokens[index++] = "comma";
			Tokens[index++] = "colon";
			Tokens[index++] = "semicolon";
			Tokens[index++] = "period";
			Tokens[index++] = "quote";
			Tokens[index++] = "dquote";
			Tokens[index++] = "number";
			Tokens[index++] = "numfloat";
			Tokens[index++] = "literal";
			Tokens[index++] = "eof";
			Tokens[index++] = "identifier";
			Tokens[index++] = "constructor";
			Tokens[index++] = "comment";
			Tokens[index++] = "unknown";
		}

		public static string GetFilename(string FullName, char ParseChar)
		{
			int idx;			// location of ParseChar
			string shortname;	// return string
		
			// initialize our variables
			idx = 0;
			shortname = "";
		
			// find the last occurence of ParseChar
			idx = FullName.LastIndexOf(ParseChar);
		
			// grab from 0 to idx-1
			shortname = FullName.Substring(0, idx);
	
			// done!
			return shortname;
		}
	}
}
