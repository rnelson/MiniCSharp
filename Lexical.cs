/* $Id$
 * Ross Nelson
 * Compiler Construction (CSC 446)
 * MiniCSharp/Lexical Analyzer
 */

#region Using declarations
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
#endregion

namespace MiniCSharp
{
	class Lexical
	{
		public char ch;						/* current or lookahead char */
		private FileInfo fs;				/* input file stream part 1 */
		private StreamReader stream;		/* input file stream part 2 */
		private string reader;				/* read string from the file stream */
		private bool haveToken;				/* we have a token, stop reading */
		private bool eof;					/* true=eof, false=!eof */

		/* Lexical()
		 * 
		 * Dummy constructor to spit out an error and kill the calling Main()
		 */
		public Lexical()
		{
			System.Console.WriteLine("error: default Lexical constructor called, filename needed");
			System.Environment.Exit(1);
		}

		/* Lexical()
		 * 
		 * The lexical analyzer's contructor, which sets some default values
		 * and opens up the passed in filename
		 */
		public Lexical(string filename)
		{
			/* set default values */
			CleanUp();
			eof = false;
			
			/* save the filename */
			Globals.filename = filename;
			
			/* open the source file */
			try
			{
				fs = new FileInfo(filename);
				stream = fs.OpenText();
			}
			catch
			{
				System.Console.WriteLine("There has been an error opening {0}.  The program is terminating.", filename);
				System.Environment.Exit(-1);
			}
		}
		
		/* ~Lexical()
		 * 
		 * The lexical analyzer's destructor, closes the file
		 */
		~Lexical()
		{
			/* close the source file, set the token to End of File */
			try 
			{
				stream.Close();
			}
			catch
			{
				System.Console.WriteLine("error closing {0}", Globals.filename);
			}
			
			Globals.token = Globals.Symbol._eof;
		}
		
		/* DisplayOutput()
		 * 
		 * Print out information about the token
		 */
		public void DisplayOutput()
		{
                        /* increment the line count */
                        Globals.linecount++;

                        /* if we've printed a lot, clear the screen */
                        if (Globals.linecount > 15)
                        {
                                /* reset the line count */
                                Globals.linecount = 0;
                                /* wait for the user to press enter */
                                Globals.Wait("\nPress enter to continue...");

                                /* print out the header */
                                Program.PrintHeader();
                        }

                        /* print out the data */
                        switch ((int)Globals.token)
                        {
                                case (int)Globals.Symbol._literal:
                                        System.Console.WriteLine("{0,-10}\t\t{1,-10}\t\t{2}", Globals.Tokens[(int)Globals.token], Globals.lexeme, Globals.literal);
                                        break;
                                case (int)Globals.Symbol._number:
                                        System.Console.WriteLine("{0,-10}\t\t{1,-10}\t\t{2}", Globals.Tokens[(int)Globals.token], Globals.lexeme, Globals.value);
                                        break;
                                case (int)Globals.Symbol._numfloat:
                                        System.Console.WriteLine("{0,-10}\t\t{1,-10}\t\t{2}", Globals.Tokens[(int)Globals.token], Globals.lexeme, Globals.valueF);
                                        break;
                                default:
                                        System.Console.WriteLine("{0,-10}\t\t{1,-10}", Globals.Tokens[(int)Globals.token], Globals.lexeme);
                                        break;
                        }
			/* we don't want this stuff now */
			return;
		}
		
		/* GetNextToken()
		 * 
		 * Read the next token from a string of text
		 * and place it into `lexeme`; if a literal
		 * string or integer/floating point number is
		 * found, place it into the appropriate public
		 * variable
		 */
		public void GetNextToken()
		{
			/* skip comments */
			if (reader != null)
				while (ch == '/' && reader[0] == '/')
				{
					GetNextLine();
					if (reader == null)
						break;
				}
			
			/* clean up the mess */
			CleanUp();
			eof = false;
			
			/* avoid trying to reference something that doesn't exist */
			if (reader == null)
				GetNextLine();

			/* remove leading and trailing whitespace, if possible */
			try 
			{
				reader.Trim();
			}
			catch 
			{
				/* nothing */
			}
			
			/* ignore whitespace */
			while ((ch <= ' ') && !eof)
			{
				GetNextChar();
			}
			
			/* as long as we read text in, process the token */
			if (reader != null)
			{
				ProcessToken();
			}
		}
		
		/* GetNextChar()
		 * 
		 * Place the next char into `ch`
		 */
		private void GetNextChar()
		{
			/* clear out the current value */
			ch = (char)0;
			
			/* stop if eof */
			if (eof)
			{
				Globals.token = Globals.Symbol._eof;
				return;
			}
			
			/* don't try to access the Length property if we have no string */
			bool newLine = false;
			if ((reader == null) || (reader == ""))
			{
				try
				{
					GetNextLine();
					newLine = true;
				}
				catch
				{
					/* nothing to do */
				}
			}
			
			if (!newLine && (Globals.token != Globals.Symbol._eof))
			{
				if (reader != null)
				{
					if (reader.Length > 0)
					{
						/* place the next character into `ch` */
						ch = reader[0];
					
						/* remove `ch` from reader */
						reader = reader.Substring(1, reader.Length - 1);
					}
					else
					{
						GetNextLine();
					}
				}
			}
		}
		
		/* GetNextLine()
		 * 
		 * Read the next line from the source file
		 */
		private void GetNextLine()
		{
			try
			{
				/* read a line from the file */
				reader = stream.ReadLine();
				Globals.curLine++;

				/* set eof if needed */
				if (reader == null)
				{
					eof = true;
					Globals.token = Globals.Symbol._eof;
					return;
				}
				
				/* make sure we don't have a blank line */
				reader.Trim();
				while (reader.Length == 0)
				{
					reader = stream.ReadLine();
					
					if (reader == null)
					{
						eof = true;
						Globals.token = Globals.Symbol._eof;
						return;
					}
					
					reader.Trim();
				}

				/* save the first character */
				ch = reader[0];
				
				/* clear whitespace */
				while (Char.IsWhiteSpace(ch))
				{
					reader = reader.Substring(1, reader.Length - 1);
					ch = reader[0];
				}

				/* remove `ch` from reader */
				reader = reader.Substring(1, reader.Length - 1);
			}
			catch
			{
				/* r795: changed to call GetNextLine() recursively   */
				/*       now, a line of just whitespace doesn't kill */
				/*       the parser when it gets _unknown instead of */
				/*       a real value                                */
				//CleanUp();
				//eof = true;
				//Globals.token = Globals.Symbol._unknown;
				//reader = "";
				GetNextLine();
			}
		}
		
		/* ProcessToken()
		 * 
		 * Decide which function to use to process
		 * a given token
		 */
		private void ProcessToken()
		{
			if (haveToken)
				return;
			
			if (!Char.IsWhiteSpace(ch))
				Globals.lexeme = ch.ToString();
			else
				Globals.lexeme = "";

			/* grab the next character so we know if it's a comment/2-char operator */
			GetNextChar();
			
			try 
			{
				if (Char.IsLetter(Globals.lexeme[0]) || Globals.lexeme[0] == '_')	/* word */
				{
					ProcessWordToken();
				}
				else if (Char.IsDigit(Globals.lexeme[0]))							/* number */
				{
					ProcessNumericToken();
				}
				else if ((Globals.lexeme[0] == '\"') || (Globals.lexeme[0] == '\''))	/* literal */
				{
					ProcessLiteral();
				}
				else
				{
					/* for complex operations (+=, -=, /=, *=, %=, ==, !=, etc), lex[0]
					 * holds the first character and ch holds the second */
					if (CCType.IsOperator(Globals.lexeme[0]) && CCType.IsOperator(ch))	/* 2-character operator */
					{
						/* deal with comments */
						if ((Globals.lexeme[0] == '/') && (ch == '/'))
						{
							ProcessComment();
							//Globals.token = Globals.Symbol._comment;
							//GetNextToken();
						}
						else if ((reader[0] == '/') && (ch == '/'))
						{
							ProcessComment();
							//Globals.token = Globals.Symbol._comment;
							//GetNextToken();
						}
						else
						{
							ProcessDoubleToken();
							GetNextChar();
						}
					}
					else
						ProcessSingleToken();
				}
			}
			catch
			{
				/* let's just ignore it -- that's good programming, right? */
			}
		}
		
		/* ProcessWordToken()
		 * 
		 * Process an alphanumeric (+ underscore) token
		 */
		private void ProcessWordToken()
		{
			/* fill lexeme */
			while ((Char.IsLetter(ch) || Char.IsDigit(ch) || ch == '_') && !eof)
			{
				Globals.lexeme += ch.ToString();
				GetNextChar();
			}
			
			/* check to see if we have a reserved word */
			for (int count = 0; count < Globals.ReservedWords.Length; count++)
			{
				if (Globals.lexeme == Globals.ReservedWords[count])
				{
					Globals.token = (Globals.Symbol)count;
					haveToken = true;
					break;
				}
			}
			
			/* if we get here, we've got an identifier */
			if (Globals.token == Globals.Symbol._unknown)
			{
				Globals.token = Globals.Symbol._identifier;
				haveToken = true;
			}
		}
		
		/* ProcessNumericToken()
		 * 
		 * Read in until no more digits or more than one
		 * period is found, leave the string in lexeme and
		 * the value in value or valueF, depending on int/float
		 */
		private void ProcessNumericToken()
		{
			bool havePeriod = false;		/* only allow one decimal point */

			/* fill lexeme */
			while (Char.IsDigit(ch) || (ch == '.') && !eof)
			{
				if (Char.IsDigit(ch) || (ch == '.'))
				{
					/* kill the '12..05' bug */
					if ((ch == '.') && havePeriod)
					{
						Globals.lexeme += ch;
						while ((Char.IsDigit(ch) || (ch == '.')) && !eof)
						{
							GetNextChar();
							if (Char.IsDigit(ch) || (ch == '.'))
								Globals.lexeme += ch;
						}
						
						System.Console.WriteLine("error: {0}:{1}: \"{2}\" is an invalid number", Globals.filename, Globals.curLine, Globals.lexeme);
						System.Environment.Exit(-2);
					}
					
					Globals.lexeme += ch;
				}
				
				if (ch == '.')
					havePeriod = true;
				
				GetNextChar();
			}
			
			/* convert the string to a number and return the token type */
			if (havePeriod)
			{	
				Globals.value = 0;
				Globals.valueF = System.Convert.ToDouble(Globals.lexeme);
				Globals.token = Globals.Symbol._numfloat;
				haveToken = true;
			}
			else
			{
				Globals.value = System.Convert.ToInt32(Globals.lexeme);
				Globals.valueF = 0.0;
				Globals.token = Globals.Symbol._number;
				haveToken = true;
			}
		}
		
		/* ProcessCommentToken()
		 * 
		 * Take care of (read: ignore) comments
		 */
		private void ProcessComment()
		{	
			/* By the time we get to this chunk of code, ch holds '/' and
			 * Globals.lexeme[0] holds '/', we know we've got a comment.
			 * Since double slash comments extend to the end of the line,
			 * nothing more has to be done with the line of text we have.
			 * Wipe it out and let a parent function read in the next line. */
			reader = "";
			Globals.token = Globals.Symbol._comment;
			GetNextLine();
			GetNextToken();
		}
		
		/* ProcessSingleToken()
		 * 
		 * Deal with single operators...assignop, mulop, addop, and so forth
		 */
		private void ProcessSingleToken()
		{
			/* ch holds the operator */
			switch (Globals.lexeme[0])
			{
				case '<': case '>':
					Globals.token = Globals.Symbol._relop;
					haveToken = true;
					break;
				case '!':
					Globals.token = Globals.Symbol._unarynot;
					haveToken = true;
					break;
				case '+':
					Globals.token = Globals.Symbol._addop;
					haveToken = true;
					break;
				case '-':
					Globals.token = Globals.Symbol._signop;
					haveToken = true;
					break;
				case '*': case '/':
					Globals.token = Globals.Symbol._mulop;
					haveToken = true;
					break;
				case '=':
					Globals.token = Globals.Symbol._assignop;
					haveToken = true;
					break;
				case '(':
					Globals.token = Globals.Symbol._lparen;
					haveToken = true;
					break;
				case ')':
					Globals.token = Globals.Symbol._rparen;
					haveToken = true;
					break;
				case '{':
					Globals.token = Globals.Symbol._lbrace;
					haveToken = true;
					break;
				case '}':
					Globals.token = Globals.Symbol._rbrace;
					haveToken = true;
					break;
				case '[':
					Globals.token = Globals.Symbol._lbracket;
					haveToken = true;
					break;
				case ']':
					Globals.token = Globals.Symbol._rbracket;
					haveToken = true;
					break;
				case ',':
					Globals.token = Globals.Symbol._comma;
					haveToken = true;
					break;
				case ':':
					Globals.token = Globals.Symbol._colon;
					haveToken = true;
					break;
				case ';':
					Globals.token = Globals.Symbol._semicolon;
					haveToken = true;
					break;
				case '.':
					Globals.token = Globals.Symbol._period;
					haveToken = true;
					break;
				case '\'':
					Globals.token = Globals.Symbol._quote;
					haveToken = true;
					break;
				case '"':
					Globals.token = Globals.Symbol._dquote;
					haveToken = true;
					break;
				case '&':
					Globals.token = Globals.Symbol._bandop;
					haveToken = true;
					break;
				case '|':
					Globals.token = Globals.Symbol._borop;
					haveToken = true;
					break;
				default:
					Globals.token = Globals.Symbol._unknown;
					haveToken = true;
					break;
			}
		}
		
		/* ProcessDoubleToken()
		 * 
		 * Take care of two-character operators, such as += -= *= /= %=
		 * and so on
		 */
		private void ProcessDoubleToken()
		{
			/* ch holds < > = !
			 * Globals.lexeme[0] holds =
			 */
			switch (Globals.lexeme[0])
			{
				case '+': case '-': case '/': case '*': case '%':
					Globals.token = Globals.Symbol._assignop;
					Globals.lexeme = Globals.lexeme[0] + ch.ToString();
					haveToken = true;
					break;
				case '<': case '>':
					Globals.token = Globals.Symbol._relop;
					Globals.lexeme = Globals.lexeme[0] + ch.ToString();
					haveToken = true;
					break;
				case '=': case '!':
					Globals.token = Globals.Symbol._condop;
					Globals.lexeme = Globals.lexeme[0] + ch.ToString();
					haveToken = true;
					break;
				case '&':
					if (ch == '&')
					{
						Globals.token = Globals.Symbol._andop;
						Globals.lexeme = Globals.lexeme[0] + ch.ToString();
						haveToken = true;
					}
					break;
				case '|':
					if (ch == '|')
					{
						Globals.token = Globals.Symbol._orop;
						Globals.lexeme = Globals.lexeme[0] + ch.ToString();
						haveToken = true;
					}
					break;
				default:
					if (!CCType.IsOperator(Globals.lexeme[0]))
					{
						ProcessSingleToken();
						break;
					}
					Globals.token = Globals.Symbol._unknown;
					haveToken = true;
					break;
			}
		}

		/* ProcessLiteral()
		 * 
		 * Take care of literals
		 */
		private void ProcessLiteral()
		{
			char findMe;
			bool dblQuote;

			/* keep track of the character to look for */
			findMe = Globals.lexeme[0];
			Globals.literal = "";

			/* what kind of quote do we have? */
			if (findMe == '\"')
				dblQuote = true;
			else
				dblQuote = false;

			/* print out the information about the current quote mark */
			Globals.lexeme = findMe.ToString();
			if (dblQuote)
				Globals.token = Globals.Symbol._dquote;
			else
				Globals.token = Globals.Symbol._quote;

			/* get the remainder of literal */
			while (ch != findMe)
			{
				if (reader.Length == 0)
				{
					System.Console.WriteLine("warning: {0}:{1}: unterminated literal, expecting {2}", Globals.filename, Globals.curLine, findMe);
					break;
				}
				
				Globals.lexeme += ch.ToString();
				Globals.literal += ch.ToString();

				if (ch != findMe)
					GetNextChar();
			}

			/* add the quote on the end */
			Globals.lexeme += ch.ToString();
			
			/* set the token type */
			Globals.token = Globals.Symbol._literal;
			
			/* read the next character */
			GetNextChar();
		}

		/* CleanUp()
		 * 
		 * Reset variables
		 */
		private void CleanUp()
		{
			/* reset things */
			Globals.lexeme = "";
			Globals.token = Globals.Symbol._unknown;
			Globals.value = 0;
			Globals.valueF = 0.0;
			Globals.literal = "";
			haveToken = false;
			
			/* fix blank line issue */
			if (reader == null)
			{
				Globals.token = Globals.Symbol._comment;
				return;
			}
			if (reader == "")
			{
				Globals.token = Globals.Symbol._comment;
				return;
			}
			if ((reader[0] == '/') && (reader[1] == '/'))
			{
				reader = "";
				Globals.token = Globals.Symbol._comment;
				return;
			}
		}
	}
}
