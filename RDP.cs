/* $Id$
 * Ross Nelson
 * Compiler Construction (CSC 446)
 * MiniCSharp/Recursive Descent Parser
 */

#region Using declarations
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
#endregion

namespace MiniCSharp
{
	class RDP
	{
		/// a lexical analyzer object
		private Lexical lex;
		/// keep track of whether or not we're doing a method -- depth changes
		private bool incAtBrace;
		/// keep track of whether or not an identifier is a method
		private bool isFunc;
		/// we found Main() -- error if not
		private bool foundMain;
		/// we are in Main()
		private bool nowMain;
		/// the size of local variables in a method
		private int funcSize;
		/// the size of the local variables in a class
		private int classSize;
		/// the location of a parameter
		private int varLoc;
		/// are we in ParamList? (used with varLoc)
		private bool inParam;
		/// the current parent object (if applicable)
		private Element parent;
		/// the most recently inserted object (for setting parent)
		private Element current;
		/// the class that we're in
		private Element currentclass;
		/// the method that we're in
		private Element currentmethod;
		/// the current offset (for a given block)
		private int localoffset;
		/// the offset multiplier (to get negative values)
		private int offsetmul;

		/// START instruction for TAC
		private string TACstart;
		/// are we inside an ( Expr ) sequence?
		private bool inExpr;
		/// minimum offset in the current block
		private int minOffset;
		
		/// TAC filestream
		private FileStream tacfs;
		/// TAC stream
		private StreamWriter tacsw;
		
		///
		/// Dummy constructor to spit out an error and kill the calling Main()
		///
		public RDP()
		{
			System.Console.WriteLine("error: default RDP constructor called, filename needed");
			System.Environment.Exit(1);
		}
		
		///
		/// The parser's contructor, which sets some default values
		/// and opens up the passed in filename
		/// \param filename the file to open
		///
		public RDP(string filename)
		{
			lex = new Lexical(filename);
			lex.GetNextToken();
			
			/* give the class-scope variables some real values */
			incAtBrace = true;
			isFunc = false;
			inParam = false;
			foundMain = false;
			nowMain = false;
			funcSize = 0;
			classSize = 0;
			varLoc = 0;
			parent = null;
			current = null;
			currentclass = null;
			localoffset = 2;
			offsetmul = -1;
			minOffset = 0;
			
			/* don't forget the three-address-code stuff */
			TACstart = "";
			inExpr = false;
			
			/* open the TAC file */
			tacfs = new FileStream(Globals.GetFilename(Globals.filename, '.') + ".TAC", FileMode.Create);
			tacsw = new StreamWriter(tacfs, Encoding.UTF8);
			
			/* run the function for the first grammar rule */
			Prog();
			
			/* flush the StreamWriter buffer and close the files */
			try
			{
				tacsw.Write(TACstart);
				
				if (Globals.VISUAL)
					Console.WriteLine(TACstart);
				
				tacsw.Flush();
				tacsw.Close();
				tacfs.Close();
			}
			catch
			{
				/* oh well! */
			}
		}
		
		///
		/// Dummy function that does nothing
		///
		public void Dummy()
		{
			/* do nothing */
		}
		
		///
		/// Spit out a error message and terminate execution
		/// \param line line number
		/// \param expected the expected token
		/// \param found the token that was found
		///
		static void Error(int line, string expected, string found)
		{
			System.Console.WriteLine("error: {0}:{1}: expecting {2} but found {3}", Globals.filename, line, expected, found);
			Globals.Wait("\nPress enter...");
			System.Environment.Exit(42);
		}
		///
		/// Spit out a error message and terminate execution
		/// \param message the error message
		///
		static void Error(string message)
		{
			System.Console.WriteLine("{0}", message);
			Globals.Wait("\nPress enter...");
			System.Environment.Exit(42);
		}
		
		///
		/// Spit out a warning message
		/// \param line line number
		/// \param expected the expected token
		///
		static void Warning(int line, string expected)
		{
			System.Console.WriteLine("warning: {0}:{1}: expecting {0}", expected);
		}
		///
		/// Spit out a warning message
		/// \param message the error message
		///
		static void Warning(string message)
		{
			System.Console.WriteLine("{0}", message);
		}
		
		///
		/// Compare the current token with the desired one, error out if they don't match
		/// \param desired the desired token
		///
		void match(Globals.Symbol desired)
		{
			int loc = (int)desired;
			string wanted = Globals.Tokens[loc];
			string found = Globals.Tokens[(int)Globals.token];
			
			if (Globals.token == Globals.Symbol._eof)
			{
				if (desired != Globals.Symbol._eof)
				{
					Error(Globals.curLine, wanted, "end of file");
				}
			}
			
			if (desired == Globals.token)
			{
				/* increase/decrease the depth as needed */
				if (desired == Globals.Symbol._lbrace)
				{
					if (incAtBrace)
					{
						Globals.depth++;
						parent = current;
						localoffset = 2;
						offsetmul = -1;
						funcSize = 0;
					}
					else
						incAtBrace = true;
				}
				else if (desired == Globals.Symbol._lparen)
				{
					incAtBrace = false;
					parent = current;
					Globals.depth++;
					localoffset = 4;
					offsetmul = 1;
					funcSize = 0;
				}
				else if (desired == Globals.Symbol._rparen)
					inParam = false;
				else if (desired == Globals.Symbol._rbrace)
				{
					/* save the child lists */
					try
					{
						parent.childList =  Globals.symTab.GetChildrenPrint(parent);
						current.childList = Globals.symTab.GetChildrenPrint(current);
						parent.pchildList = Globals.symTab.GetChildren(parent);
						current.pchildList = Globals.symTab.GetChildren(current);
					
						/* restore the offset */
						localoffset = parent.GetOffset();
					
						/* save the method/class size */
						funcSize = 0;
					
						/* unset currentclass if needed */
						if (currentclass == parent)
							currentclass = null;
						
						/* do the same with currentmethod */
						currentmethod = null;
					
						/* kill this depth */
						Globals.symTab.DeleteDepth(Globals.depth);
						Globals.depth--;
					
						/* go up a level for parent */
						if (current != null)
							parent = parent.parent;
					}
					catch
					{
						/* temp variables break this */
					}
				}
				else if (desired == Globals.Symbol._identifier)
				{
					/* ...in case we miss match("Main") */
					if (Globals.lexeme == "Main")
					{
						/* this bug should be fixed.  if we hit this, something
						 * went wrong. */
						foundMain = true;
						
						/* save the TAC START line */
						TACstart = "START BROKEN_READ.Main";
					}
				}
				
				if (desired != Globals.Symbol._eof)
					lex.GetNextToken();
			}
			else if (Globals.token == Globals.Symbol._comment)
			{
				if (desired != Globals.Symbol._eof)
				{
					lex.GetNextToken();
					match(desired);
				}
			}
			else
				Error(Globals.curLine, wanted, found);
		}
		
		///
		/// Compare the current token with the desired one, error out if they don't match
		/// \param desired the desired string
		///
		void match (string desired)
		{
			if (Globals.token == Globals.Symbol._eof)
			{
				Error(Globals.curLine, desired, "end of file");
			}
			
			if (desired == Globals.lexeme)
				lex.GetNextToken();
			else if (Globals.lexeme.Substring(0, 2) == "//")
			{
				lex.GetNextToken();
				match(desired);
			}
			else
				Error(Globals.curLine, desired, Globals.lexeme);
		}
		
		///
		/// Create a new Element object, used for temporary values
		/// \return pointer to a new object
		///
		private Element newtemp()
		{
			/* find the next minimum offset */
			minOffset = Globals.symTab.GetMinOffset();
			minOffset -= 2;
			
			/* get a logical name for the next temporary variable */
			string tName = "_BP";
			tName += minOffset < 0 ? minOffset.ToString() : "-" + minOffset.ToString();
			
			/* create and return the new element */
			Element e = AddSymbol(Globals.Symbol._unknown, Globals.Symbol._private, Globals.Symbol._int, Globals.Symbol._int, tName, Globals.depth);
			
			/* make sure the temporary has the right offset */
			e.SetOffset(minOffset);
			
			return e;
		}
		
		///
		/// Print a line of TAC code
		/// \param text the text to be printed
		///
		private void emit(string text)
		{
			/* add the line to the TAC file */
			tacsw.Write(text);
			
			/* print the line to the screen if requested */
			if (Globals.VISUAL == true)
			{
				if (Globals.linecount <= 15)
					Console.Write(text);
				else
				{
					Globals.linecount = 0;
					Globals.Wait("Hit enter to continue...");
					Console.Write(text);
				}
				
				Globals.linecount++;
			}
		}
		
		///
		/// Add an element to the symbol table
		/// \param accMod access modifier (ref, out)
		/// \param type type of symbol being added
		/// \param token I'm not quite sure why I added this
		/// \param lexeme lexeme (string representation of the symbol)
		/// \param depth the depth at which the lexeme should be added
		/// \return address to the new symbol
		///
		private Element AddSymbol(Globals.Symbol pMode, Globals.Symbol accMod, Globals.Symbol type, Globals.Symbol token, string lexeme, int depth)
		{
			/* add the identifier */
			Element e;
			e = Globals.symTab.Lookup(lexeme);
			
			/* keep track of the size for offset information */
			int mysize = 0;
			
			/* check for duplicates */
			if (e != null)
				if (e.GetDepth() == Globals.depth)
					Error("error: duplicate symbol \"" + lexeme + "\" found on line " + Globals.curLine);
			
			e = Globals.symTab.Insert(lexeme, type, depth);
			current = e;
			
			/* set the access */
			e.SetAccess(accMod);
			
			/* set the type and size */
			if (!isFunc)
			{
				switch ((int)type)
				{
					case (int)Globals.Symbol._int:
						e.SetInteger();
						mysize = 2;
						break;
					case (int)Globals.Symbol._float:
						e.SetFloat();
						mysize = 4;
						break;
					case (int)Globals.Symbol._char:
						e.SetCharacter();
						mysize = 1;
						break;
					case (int)Globals.Symbol._class:
						e.SetClass();
						mysize = 0;
						currentclass = e;
						break;
				}
				
				if ((token != Globals.Symbol._const) && !inParam)
				{
					e.SetSizeOfLocals(mysize);
					funcSize += mysize;
				}
			}
			else	/* we are dealing with a method */
			{
				switch ((int)type)
				{
					case (int)Globals.Symbol._int:
						e.SetInteger();
						break;
					case (int)Globals.Symbol._float:
						e.SetFloat();
						break;
					case (int)Globals.Symbol._char:
						e.SetCharacter();
						break;
					case (int)Globals.Symbol._class:
						e.SetClass();
						break;
				}
				isFunc = false;
			}
			
			/* set the passing mode (if specified) */
			switch ((int)pMode)
			{
				case (int)Globals.Symbol._ref:
					e.mode = Element.PassingMode.passRef;
					break;
				case (int)Globals.Symbol._out:
					e.mode = Element.PassingMode.passOut;
					break;
				default:
					break;
			}
			
			/* stay in touch with your folks! */
			e.parent = parent;
			
			if (token != Globals.Symbol._const)
			{
				/* update the local variable size for classes */
				if (parent != null)
					if (parent.GetEType() == Element.EntryType.classType)
					{
						classSize += mysize;
						if (current.GetEType() == Element.EntryType.methodType)
							currentmethod = current;
					}
					else if (parent.GetEType() == Element.EntryType.methodType)
						if (!inParam)
							parent.SetSizeOfLocals(parent.GetSizeOfLocals() + mysize);
			
				/* update the offset */
				e.SetOffset(localoffset);
				localoffset += (mysize * offsetmul);
				
				/* update minOffset */
				if (e.GetOffset() < minOffset)
					minOffset = e.GetOffset();
			}
			else
			{
				e.SetOffset(0);
			}
			
			/* set the parameter location (or 0) */
			if (!inParam)
				varLoc = 0;
			else
			{
				varLoc++;
				if (parent != null)
					parent.SetNumParams(varLoc);
			}
			
			/* set the parameter number (or 0) */
			e.location = varLoc;
			
			/* make sure isFunc is false so we don't consider a variable inside a method to be a method */
			isFunc = false;
			
			return e;
		}
		
		///
		/// Implements the grammar rule: AccessModifier -> [public] | [static] | lambda
		///
		void AccessModifier(out Globals.Symbol access)
		{
			access = Globals.Symbol._private;
			
			if (Globals.token == Globals.Symbol._public)
			{
				match(Globals.Symbol._public);
				access = Globals.token;
			}
			else if (Globals.token == Globals.Symbol._static)
			{
				match(Globals.Symbol._static);
				access = Globals.token;
			}
		}
		
		///
		/// Implements the grammar rule: Addop -> [+] | [-] | [&&]
		///
		void Addop()
		{
			switch ((int)Globals.token)
			{
				case (int)Globals.Symbol._addop:
					match(Globals.Symbol._addop);
					break;
				case (int)Globals.Symbol._signop:
					match(Globals.Symbol._signop);
					break;
				case (int)Globals.Symbol._orop:
					match(Globals.Symbol._orop);
					break;
				default:
					Error(Globals.curLine, "+, -, or ||", Globals.lexeme);
					break;
			}
		}
		
		///
		/// Implements the grammar rule: AssignStat -> idt [=] Expr | idt [=] MethodCall | MethodCall
		///
		void AssignStat()
		{
			Element getMethod = null;
			bool standAlone = true;
			string printAssignmentVar = "";
			string possibleReturnVariable = Globals.lexeme;
			
			/* make sure the variable is defined */
			Element e = Globals.symTab.Lookup(Globals.lexeme);
			if (e == null)
			{
				Warning("warning: " + Globals.filename + ":" + Globals.curLine + ": undeclared variable \"" + Globals.lexeme + "\"");
				e = AddSymbol(Globals.Symbol._unknown, Globals.Symbol._unknown, Globals.Symbol._unknown, Globals.Symbol._unknown, Globals.lexeme, Globals.depth);
			}
			match(Globals.Symbol._identifier);
			
			if (Globals.token == Globals.Symbol._assignop)
			{
				standAlone = false;
				match(Globals.Symbol._assignop);
			}
			
			if (Globals.token == Globals.Symbol._period)
				AssignTail(ref getMethod, e, "", standAlone, out printAssignmentVar);
			else if(Globals.token == Globals.Symbol._identifier)
				AssignTail(ref getMethod, null, possibleReturnVariable, standAlone, out printAssignmentVar);
			else
			{
				string rhside;
				Expr("", out rhside);
				
				if (rhside != "")
				{
					if (!CCType.IsNumeric(rhside))
					{
						Element rh = Globals.symTab.Lookup(rhside);
						
						if (rh.GetEType() == Element.EntryType.constType)
							emit("  " + e.GetOffsetName() + " = " + rh.GetIntegerValue() + "\n");
						else
							emit("  " + e.GetOffsetName() + " = " + rh.GetOffsetName() + "\n");
					}
					else
					{
						emit("  " + e.GetOffsetName() + " = " + rhside + "\n");
					}
				}
			}
			
			if (printAssignmentVar != "")
			{
				if (printAssignmentVar.Length > 2)
				{
					if (printAssignmentVar.Substring(0, 3) != "_BP")
					{
						Element pav = Globals.symTab.Lookup(printAssignmentVar);
						emit("  " + e.GetOffsetName() + " = " + pav.GetOffsetName() + "\n");
					}
				}
				else
				{
					Element pav = Globals.symTab.Lookup(printAssignmentVar);
					emit("  " + e.GetOffsetName() + " = " + pav.GetOffsetName() + "\n");
				}
			}
		}
		
		///
		/// Implements the grammar rule: AssignTail -> idt . MethodCall | idt ShortExpr
		///
		void AssignTail(ref Element getMethod, Element parent, string retLoc, bool standAloneCall, out string rhSide)
		{
			if (standAloneCall)
				match(Globals.Symbol._period);
			
			/* grab the next variable and make sure it's valid */
			Element e = Globals.symTab.Lookup(Globals.lexeme);
			if (e == null)
			{
				Warning("warning: " + Globals.filename + ":" + Globals.curLine + ": undeclared variable \"" + Globals.lexeme + "\"");
				e = AddSymbol(Globals.Symbol._unknown, Globals.Symbol._unknown, Globals.Symbol._unknown, Globals.Symbol._unknown, Globals.lexeme, Globals.depth);
			}
			
			string prnt = Globals.lexeme;
			match(Globals.Symbol._identifier);
			
			if ((Globals.token == Globals.Symbol._period) || standAloneCall)
			{
				if (parent == null)
					parent = Globals.symTab.Lookup(prnt);
				
				string nsaMethod;
				rhSide = "";
				getMethod = e;
				MethodCall(standAloneCall, out nsaMethod);
				
				if (parent == null)
				{
					if (nsaMethod == "")
						emit("\n  CALL " + e.GetName() + "\n");
					else
						emit("\n  CALL " + nsaMethod + "\n");
				}
				else
				{
					if (nsaMethod == "")
						emit("\n  CALL " + parent.GetName() + "." + e.GetName() + "\n");
					else
						emit("\n  CALL " + parent.GetName() + "." + nsaMethod + "\n");
				}
				
				if (retLoc.Length > 0)
				{
					Element r = Globals.symTab.Lookup(retLoc);
					emit("  " + r.GetOffsetName() + " = _AX\n");
				}
			}
			else
			{
				string right_lexeme;
				ShortExpr(e.GetName(), out right_lexeme);
				
				if (retLoc.Length > 0)
				{
					rhSide = "";
					Element l = Globals.symTab.Lookup(retLoc);
					Element r = Globals.symTab.Lookup(right_lexeme);
					
					if (r.GetEType() == Element.EntryType.constType)
						emit("  " + l.GetOffsetName() + " = " + r.GetIntegerValue() + "\n");
					else
						emit("  " + l.GetOffsetName() + " = " + r.GetOffsetName() + "\n");
				}
				else
					rhSide = right_lexeme;
			}
		}
		
		
		///
		/// Implements the grammar rule: BaseClass -> [:] ClassOrNamespace | lambda
		///
		void BaseClass()
		{
			if (Globals.token == Globals.Symbol._colon)
			{
				match(Globals.Symbol._colon);
				ClassOrNamespace();
			}
		}
		
		///
		/// Implements the grammar rule: Classes -> ClassesDecl Classes | lambda
		///
		void Classes()
		{
			switch ((int)Globals.token)
			{
				case (int)Globals.Symbol._public:
				case (int)Globals.Symbol._static:
				case (int)Globals.Symbol._class:
					ClassesDecl();
					Classes();
					break;
				default:
					break;
			}
		}
		
		///
		/// Implements the grammar rule: ClassesDecl -> AccessModifier [class] [idt] BaseClass [{] Composite [}]
		///
		void ClassesDecl()
		{
			Globals.Symbol accMod, type = Globals.Symbol._class;
			int dep = Globals.depth;
			classSize = 0;
			
			AccessModifier(out accMod);
			match(Globals.Symbol._class);
			
			/* add the class to the symbol table */
			Element c = AddSymbol(Globals.Symbol._unknown, accMod, type, Globals.token, Globals.lexeme, Globals.depth);
			
			/* match the identifier and get the next token+lexeme */
			match(Globals.Symbol._identifier);
			
			BaseClass();
			match(Globals.Symbol._lbrace);
			
			/* find out which path we need to go on */
			Composite();
			
			match(Globals.Symbol._rbrace);
			
			/* set the class's size */
			c.SetSizeOfLocals(classSize);
		}
		
		///
		/// Implements the grammar rule: ClassOrNamespace -> [idt] ClassOrNamespaceTail
		///
		void ClassOrNamespace()
		{
			match(Globals.Symbol._identifier);
			ClassOrNamespaceTail();
		}
		
		///
		/// Implements the grammar rule: ClassOrNamespaceTail -> [.] ClassOrNamespace | lambda
		///
		void ClassOrNamespaceTail()
		{
			switch ((int)Globals.token)
			{
				case (int)Globals.Symbol._period:
					match(Globals.Symbol._period);
					ClassOrNamespace();
					break;
				default:
					break;
			}
		}
		
		///
		/// Implements the grammar rule: Composite -> AccessModifier Type MainIDT CompositeTail Composite | Type MainIDT CompositeTail Composite | lambda
		///
		void Composite()
		{
			Globals.Symbol accMod, type = Globals.Symbol._unknown;
			string idt;
			
			localoffset = 4;
			offsetmul = 1;

			switch ((int)Globals.token)
			{
				case (int)Globals.Symbol._public:
				case (int)Globals.Symbol._static:
					AccessModifier(out accMod);
					
					/* Assignment 5 (initial semantic processing) was horribly
					 * broken because of this line--Type is non-nullable but isn't
					 * needed for the constructor.  Simply making it nullable would
					 * open up errors on other lines where a type *is* needed.
					 * 
					 * Instead of doing that, check to see if the current lexeme
					 * (Globals.lexeme) is the same as the previous one that was
					 * added to the symbol table (current.lexeme).
					 */
					if (currentclass.GetName() == Globals.lexeme)
					{
						type = Globals.Symbol._constructor;
						ConstructorDecl(accMod);
					}
					else
					{
						Type(out type);
						MainIDT(accMod, type, out idt);
						CompositeTail(idt);
					}
					Composite();
					break;
				case (int)Globals.Symbol._int:
				case (int)Globals.Symbol._float:
				case (int)Globals.Symbol._char:
				case (int)Globals.Symbol._void:
					Type(out type);
					MainIDT(Globals.Symbol._public, type, out idt);
					CompositeTail(idt);
					Composite();
					break;
				case (int)Globals.Symbol._const:
					IdentifierDecl();
					break;
				default:
					break;
			}
		}
		
		///
		/// Implements the grammar rule: CompositeTail -> [return] [;] [}] | [(] ParamList [)] [{] IdentifierList StatList ReturnLine [}] | [;]
		///
		void CompositeTail(string idt)
		{
			switch ((int)Globals.token)
			{
				case (int)Globals.Symbol._return:
					match(Globals.Symbol._return);
					match(Globals.Symbol._semicolon);
					match(Globals.Symbol._rbrace);
					break;
				case (int)Globals.Symbol._lparen:
					Element m = Globals.symTab.Lookup(idt);
					emit("PROC " + m.parent.GetName() + "." + m.GetName() + "\n");
					
					match(Globals.Symbol._lparen);
					localoffset = 4;
					offsetmul = 1;
					ParamList();
					match(Globals.Symbol._rparen);
					match(Globals.Symbol._lbrace);
					localoffset = -2;
					offsetmul = -1;
					isFunc = false;	/* off to bigger and better identifiers! */
					IdentifierList();
					StatList();
					ReturnLine(nowMain);
					match(Globals.Symbol._rbrace);
					
					emit("ENDP " + m.parent.GetName() + "." + m.GetName() + "\n");
					break;
				case (int)Globals.Symbol._semicolon:
					match(Globals.Symbol._semicolon);
					break;
				default:
					break;
			}
		}
		
		///
		/// Implements the grammar rule: ConstAssign -> idt [=] numt
		///
		Element ConstAssign(Globals.Symbol accMod, Globals.Symbol type)
		{
			Element e = AddSymbol(Globals.Symbol._unknown, accMod, type, Globals.Symbol._const, Globals.lexeme, Globals.depth);
			e.SetConstant();
			
			switch ((int)type)
			{
				case (int)Globals.Symbol._char:
					e.vtype = Element.VarType.charType;
					break;
				case (int)Globals.Symbol._int:
					e.vtype = Element.VarType.intType;
					break;
				case (int)Globals.Symbol._float:
					e.vtype = Element.VarType.floatType;
					break;
				default:
					e.vtype = Element.VarType.emptyType;
					break;
			}
			
			match(Globals.Symbol._identifier);
			match(Globals.Symbol._assignop);
			
			switch ((int)Globals.token)
			{
				case (int)Globals.Symbol._number:
					e.SetValue(Globals.value);
					match(Globals.Symbol._number);
					break;
				case (int)Globals.Symbol._numfloat:
					e.SetValue((float)Globals.valueF);
					match(Globals.Symbol._numfloat);
					break;
				default:
					Error(Globals.curLine, "a numeric value", Globals.lexeme);
					break;
			}
			
			/* mark the element as constant */
			e.SetConstant();
			return e;
		}
		
		///
		/// Implements the grammar rule: ConstructorDecl -> AccessModifier [idt] [(] ParamList [)] [{] IdentifierList StatList [}]
		///
		void ConstructorDecl(Globals.Symbol accMod)
		{
			Globals.Symbol type = Globals.Symbol._constructor;
			
			/* add the constructor */
			Element c = AddSymbol(Globals.Symbol._unknown, accMod, type, Globals.token, Globals.lexeme, Globals.depth);
			
			emit("PROC " + c.parent.GetName() + "." + c.GetName() + "\n");
			match(Globals.Symbol._identifier);
			match(Globals.Symbol._lparen);
			localoffset = 4;
			offsetmul = 1;
			ParamList();
			match(Globals.Symbol._rparen);
			match(Globals.Symbol._lbrace);
			localoffset = -2;
			offsetmul = -1;
			IdentifierList();
			StatList();
			
			/* set the constructor's size */
			c.SetMethod(accMod);
			c.SetSizeOfLocals(Globals.symTab.GetDepthSize(Globals.depth));
			classSize += c.GetSizeOfLocals();
			
			match(Globals.Symbol._rbrace);
			emit("ENDP " + c.parent.GetName() + "." + c.GetName() + "\n");
		}
		
		///
		/// Implements the grammar rule: Expr -> Relation | lambda
		///
		void Expr(string left, out string right)
		{
			switch ((int)Globals.token)
			{
				case (int)Globals.Symbol._identifier:
				case (int)Globals.Symbol._numfloat:
				case (int)Globals.Symbol._number:
				case (int)Globals.Symbol._lparen:
				case (int)Globals.Symbol._unarynot:
				case (int)Globals.Symbol._signop:
					//ShortExpr(left, out right);
					SimpleExpr(out right);
					//Relation();
					break;
				default:
					right = "";
					break;
			}
		}
		
		///
		/// Implements the grammar rule: Factor -> idt | numt | [(] Expr [)] | [!] Factor | signop Factor
		///
		void Factor(out string mylex)
		{
			string fname;
			switch ((int)Globals.token)
			{
				case (int)Globals.Symbol._identifier:
					/* make sure the variable is defined */
					Element e = Globals.symTab.Lookup(Globals.lexeme);
					if (e == null)
						Error("error: " + Globals.filename + ":" + Globals.curLine + ": undeclared variable \"" + Globals.lexeme + "\"");
					
					mylex = Globals.lexeme;
					match(Globals.Symbol._identifier);
					break;
				case (int)Globals.Symbol._numfloat:
				case (int)Globals.Symbol._number:
					mylex = Globals.lexeme;
					match(Globals.token);
					break;
				case (int)Globals.Symbol._lparen:
					/* store the offsets, matching lparen resets them */
					int lo = localoffset, om = offsetmul;
					match(Globals.Symbol._lparen);
					
					/* resetore the offsets */
					localoffset = lo;
					offsetmul = om;
					
					inExpr = true;
					Expr("", out mylex);
					inExpr = false;
					match(Globals.Symbol._rparen);
					break;
				case (int)Globals.Symbol._unarynot:
					match(Globals.Symbol._unarynot);
					Factor(out fname);
					mylex = "!" + fname;
					break;
				case (int)Globals.Symbol._signop:
					Signop();
					Factor(out fname);
					
					if (CCType.IsNumeric(fname))
					{
						mylex = "-" + fname;
					}
					else
					{
						Element f = Globals.symTab.Lookup(fname);
						Element n = newtemp();
						
						if (f.GetEType() == Element.EntryType.constType)
							emit("  " + n.GetOffsetName() + " = " + f.GetIntegerValue() + "\n");
						else
							emit("  " + n.GetOffsetName() + " = -" + f.GetOffsetName() + "\n");
						
						mylex = n.GetName();
					}
					break;
				default:
					Error(Globals.curLine, "identifier, number, (, !, or -", Globals.Tokens[(int)Globals.token]);
					mylex = "";
					break;
			}
		}
		
		///
		/// Implements the grammar rule: IdentifierDecl -> AccessModifier Type IDT [;] | Type IDT [;] | lambda
		///
		void IdentifierDecl()
		{
			/* variables to hold variable information */
			Globals.Symbol accMod = Globals.Symbol._unknown;
			Globals.Symbol type = Globals.Symbol._unknown;
			
			switch ((int)Globals.token)
			{
				case (int)Globals.Symbol._public:
				case (int)Globals.Symbol._static:
				case (int)Globals.Symbol._private:
					AccessModifier(out accMod);
					break;
				case (int)Globals.Symbol._int:
				case (int)Globals.Symbol._float:
				case (int)Globals.Symbol._char:
				case (int)Globals.Symbol._void:
				case (int)Globals.Symbol._const:
					break;
				default:
					break;
			}
			
			/* ...and keep running the grammar rules */
			if (Globals.token == Globals.Symbol._const)
			{
				match(Globals.Symbol._const);
				Type(out type);
				Element c = IDTConst(accMod, type);
				match(Globals.Symbol._semicolon);
				c.SetConstant();
				IdentifierList();
			}
			else
			{
				if (currentclass.GetName() == Globals.lexeme)
				{
					type = Globals.Symbol._constructor;
					ConstructorDecl(accMod);
				}
				else
				{
					Type(out type);
					IDT(accMod, type);
					Element e = current;
					
					if (Globals.token == Globals.Symbol._lparen)
					{
						emit("PROC " + e.parent.GetName() + "." + e.GetName() + "\n");
						match(Globals.Symbol._lparen);
						localoffset = 4;
						offsetmul = 1;
						ParamList();
						match(Globals.Symbol._rparen);
						match(Globals.Symbol._lbrace);
						localoffset = 2;
						offsetmul = -1;
						IdentifierList();
						StatList();
						ReturnLine(nowMain);
						match(Globals.Symbol._rbrace);
						emit("ENDP " + e.parent.GetName() + "." + e.GetName() + "\n");
					}
					else
						match(Globals.Symbol._semicolon);
				}
			}
		}
		
		///
		/// Implements the grammar rule: IdentifierList -> IdentifierDecl IdentifierList | lambda
		///
		void IdentifierList()
		{
			switch ((int)Globals.token)
			{
				case (int)Globals.Symbol._int:
				case (int)Globals.Symbol._float:
				case (int)Globals.Symbol._char:
				case (int)Globals.Symbol._void:
				case (int)Globals.Symbol._public:
				case (int)Globals.Symbol._static:
				case (int)Globals.Symbol._const:
					IdentifierDecl();
					IdentifierList();
					break;
				default:
					break;
			}
		}
		
		///
		/// Implements the grammar rule: Id_List -> [idt] Id_List_Tail
		///
		void Id_List()
		{
			/* make sure that the variable exists */
			Element r = Globals.symTab.Lookup(Globals.lexeme);
			if (r == null)
				Error("error: " + Globals.filename + ":" + Globals.curLine + ": attempt to read undeclared variable " + Globals.lexeme);
			
			emit("  RDI " + r.GetOffsetName() + "\n");
			match(Globals.Symbol._identifier);
			Id_List_Tail();
		}
		
		///
		/// Implements the grammar rule: Id_List_Tail -> [,] [idt] Id_List_Tail | lambda
		///
		void Id_List_Tail()
		{
			if (Globals.token == Globals.Symbol._comma)
			{
				match(Globals.Symbol._comma);
				
				/* make sure that the variable exists */
				Element r = Globals.symTab.Lookup(Globals.lexeme);
				if (r == null)
					Error("error: " + Globals.filename + ":" + Globals.curLine + ": attempt to read undeclared variable " + Globals.lexeme);
			
				emit("  RDI " + r.GetOffsetName() + "\n");
				match(Globals.Symbol._identifier);
				Id_List_Tail();
			}
		}
		
		///
		/// Implements the grammar rule: IDT -> [idt] | [,] [idt] IDT | lambda
		///
		void IDT(Globals.Symbol access, Globals.Symbol type)
		{
			Element e = null;
			
			switch ((int)Globals.token)
			{
				case (int)Globals.Symbol._identifier:
					e = AddSymbol(Globals.Symbol._unknown, access, type, Globals.token, Globals.lexeme, Globals.depth);
										
					/* if it's not a class but has a size of 0, reset the size -- hit a bug somewhere */
					if ((e.GetSizeOfLocals() == 0) &&
						!((e.GetEType() == Element.EntryType.classType) || (e.GetEType() == Element.EntryType.methodType)))
					{
						switch ((int)e.GetVType())
						{
							case (int)Element.VarType.charType:
								e.SetSizeOfLocals(e.GetSizeOfLocals() + 1);
								break;
							case (int)Element.VarType.intType:
								e.SetSizeOfLocals(e.GetSizeOfLocals() + 2);
								break;
							case (int)Element.VarType.floatType:
								e.SetSizeOfLocals(e.GetSizeOfLocals() + 4);
								break;
						}
						
						funcSize += e.GetSizeOfLocals();
					}
					
					match(Globals.Symbol._identifier);
					
					/* cheat and see if we have a method coming */
					if (Globals.token == Globals.Symbol._lparen)
					{
						isFunc = true;
						e.SetMethod(type);
						e.SetSizeOfLocals(0);
						currentmethod = e;
					}
					else
						IDT(access, type);
					
					break;
				case (int)Globals.Symbol._comma:
					match(Globals.Symbol._comma);
					IDT(access, type);
					break;
				default:
					break;
			}
		}
		
		///
		/// Implements the grammar rule: IDTConst -> ConstAssign | IDTConst [,] ConstAssign
		///
		Element IDTConst(Globals.Symbol accMod, Globals.Symbol type)
		{
			Element c = null;
			
			switch ((int)Globals.token)
			{
				case (int)Globals.Symbol._identifier:
					c = ConstAssign(accMod, type);
					IDTConst(accMod, type);
					break;
				case (int)Globals.Symbol._comma:
					match(Globals.Symbol._comma);
					IDTConst(accMod, type);
					break;
				default:
					break;
			}
			
			if (c != null)
				c.SetConstant();
			return c;
		}
		
		///
		/// Implements the grammar rule: In_Stat -> [read] [(] Id_List [)]
		///
		void In_Stat()
		{
			match(Globals.Symbol._read);
			match(Globals.Symbol._lparen);
			Id_List();
			match(Globals.Symbol._rparen);
		}
		
		///
		/// Implements the grammar rule: IOStat -> In_Stat | Out_Stat
		///
		void IOStat()
		{
			switch ((int)Globals.token)
			{
				case (int)Globals.Symbol._read:
					In_Stat();
					break;
				case (int)Globals.Symbol._write:
				case (int)Globals.Symbol._writeln:
					Out_Stat();
					break;
				default:
					Error(Globals.curLine, "'read', 'write', or 'writeln'", Globals.lexeme);
					break;
			}
		}
		
		///
		/// Implements the grammar rule: MainIDT -> [idt] MainIDT | [main] [(] [)] [{] IdentifierList StatList | [,] [idt] MainIDT | lambda
		///
		void MainIDT(Globals.Symbol accMod, Globals.Symbol type, out string idt)
		{
			idt = "";
			/* take care of the constructor */
			if (type == Globals.Symbol._constructor)
			{
				IDT(accMod, type);
				MainIDT(accMod, Globals.Symbol._unknown, out idt);
			}
			
			if ((Globals.token == Globals.Symbol._identifier) && (Globals.lexeme != "Main"))
			{
				string recur_idt;
				idt = Globals.lexeme;
				IDT(accMod, type);
				MainIDT(accMod, type, out recur_idt);
			}
			else if (Globals.lexeme == "Main")
			{
				emit("PROC " + currentclass.GetName() + ".Main\n");
				
				Element main = AddSymbol(Globals.Symbol._unknown, Globals.Symbol._unknown, Globals.Symbol._void, Globals.Symbol._void, "Main", Globals.depth);
				main.parent = currentclass;
				
				match("Main");
				foundMain = true;	/* we found it! */
				nowMain = true;		/* hello! */
				
				/* save the TAC START line */
				TACstart = "START " + currentclass.GetName() + ".Main";
				
				match(Globals.Symbol._lparen);
				match(Globals.Symbol._rparen);
				match(Globals.Symbol._lbrace);
				localoffset = -2;
				offsetmul = -1;
				IdentifierList();
				StatList();
				nowMain = false;	/* we're done with Main() */
				emit("ENDP " + currentclass.GetName() + ".Main\n");
			}
			else if (Globals.token == Globals.Symbol._comma)
			{
				match(Globals.Symbol._comma);
				match(Globals.Symbol._identifier);
				MainIDT(accMod, type, out idt);
			}
		}
		
		///
		/// Implements the grammar rule: MethodCall -> idt ( Params )
		///
		void MethodCall(bool standAloneCall, out string methodName)
		{
			string pushStats = "";
			
			if (standAloneCall)
			{
				methodName = "";
				match(Globals.Symbol._lparen);
				Params(ref pushStats);
			}
			else
			{
				match(Globals.Symbol._period);
				methodName = Globals.lexeme;
				match(Globals.Symbol._identifier);
				match(Globals.Symbol._lparen);
				Params(ref pushStats);
			}
			
			/* now print out the 'push' statements */
			emit(pushStats);
			
			match(Globals.Symbol._rparen);
		}
		
		///
		/// Implements the grammar rule: Mode -> [ref] | [out] | lambda
		///
		void Mode(ref Globals.Symbol mode)
		{
			switch ((int)Globals.token)
			{
				case (int)Globals.Symbol._ref:
					match(Globals.Symbol._ref);
					mode = Globals.Symbol._ref;
					break;
				case (int)Globals.Symbol._out:
					match(Globals.Symbol._out);
					mode = Globals.Symbol._out;
					break;
				default:
					break;
			}
		}
		
		///
		/// Implements the grammar rule: MoreFactor -> Mulop Factor MoreFactor | lambda
		///
		void MoreFactor(string left_lexeme, out string right_lexeme, string oldmulchar, out string mulchar, bool firstRound)
		{
			switch ((int)Globals.token)
			{
				case (int)Globals.Symbol._lparen:
					mulchar="";
					match(Globals.Symbol._lparen);
					Expr("", out right_lexeme);
					match(Globals.Symbol._rparen);
					break;
				case (int)Globals.Symbol._mulop:
				case (int)Globals.Symbol._andop:
					Element t, lLex, rLex;
					mulchar = Globals.lexeme;
					oldmulchar = mulchar;
					string fact_lexeme, lvar, rvar, recur_lexeme, newmulchar;
					
					Mulop();
					Factor(out fact_lexeme);
					MoreFactor(fact_lexeme, out recur_lexeme, oldmulchar, out newmulchar, false);
					
					/* look up the left and right values */
					lLex = Globals.symTab.Lookup(left_lexeme);
					if (recur_lexeme == "")
						rLex = Globals.symTab.Lookup(fact_lexeme);
					else
						rLex = Globals.symTab.Lookup(recur_lexeme);
					
					/* ...and get string values for them */
					if (lLex != null)
					{
						if (lLex.GetEType() == Element.EntryType.constType)
							lvar = lLex.GetIntegerValue().ToString();
						else
							lvar = lLex.GetOffsetName();
					}
					else if (CCType.IsNumeric(left_lexeme))
						lvar = left_lexeme;
					else
						lvar = "";
					
					if (rLex != null)
					{
						if (rLex.GetEType() == Element.EntryType.constType)
							rvar = rLex.GetIntegerValue().ToString();
						else
							rvar = rLex.GetOffsetName();
					}
					else if (CCType.IsNumeric(fact_lexeme))
						rvar = fact_lexeme;
					else
						rvar = "";
					
					t = newtemp();
					
					emit("  " + t.GetOffsetName() + " = " + lvar + " " + mulchar + " " + rvar + "\n");
					right_lexeme = t.GetName();
					
					break;

				default:
					mulchar = oldmulchar;
					right_lexeme = left_lexeme;
					break;
			}
		}
		
		///
		/// Implements the grammar rule: MoreTerm -> Addop Term MoreTerm | lambda
		///
		void MoreTerm(string left_lexeme, out string right_lexeme, string oldaddchar, out string addchar, bool firstRound)
		{
			switch ((int)Globals.token)
			{
				case (int)Globals.Symbol._addop:
				case (int)Globals.Symbol._signop:
				case (int)Globals.Symbol._orop:
					Element t, lLex, rLex;
					addchar = Globals.lexeme;
					oldaddchar = addchar;
					string term_lexeme, lvar, rvar, recur_lexeme, newaddchar;
					
					Addop();
					Term(out term_lexeme);
					MoreTerm(term_lexeme, out recur_lexeme, oldaddchar, out newaddchar, false);
					
					/* look up the left and right values */
					lLex = Globals.symTab.Lookup(left_lexeme);
					if (recur_lexeme == "")
						rLex = Globals.symTab.Lookup(term_lexeme);
					else
						rLex = Globals.symTab.Lookup(recur_lexeme);
					
					/* ...and get string values for them */
					if (lLex != null)
					{
						if (lLex.GetEType() == Element.EntryType.constType)
							lvar = lLex.GetIntegerValue().ToString();
						else
							lvar = lLex.GetOffsetName();
					}
					else if (CCType.IsNumeric(left_lexeme))
						lvar = left_lexeme;
					else
						lvar = "";
					
					if (rLex != null)
					{
						if (rLex.GetEType() == Element.EntryType.constType)
							rvar = rLex.GetIntegerValue().ToString();
						else
							rvar = rLex.GetOffsetName();
					}
					else if (CCType.IsNumeric(term_lexeme))
						rvar = term_lexeme;
					else
						rvar = "";
					
					t = newtemp();
					emit("  " + t.GetOffsetName() + " = " + lvar + " " + addchar + " " + rvar + "\n");
					right_lexeme = t.GetName();
					
					break;
				case (int)Globals.Symbol._rparen:
				case (int)Globals.Symbol._semicolon:
					right_lexeme = left_lexeme;
					addchar = "";
					break;
				default:
					right_lexeme = "";
					if (inExpr)
						right_lexeme = left_lexeme;
					addchar = "";
					break;
			}
		}
		
		///
		/// Implements the grammar rule: Mulop -> [*] | [/] | [||]
		///
		void Mulop()
		{
			switch ((int)Globals.token)
			{
				case (int)Globals.Symbol._mulop:
					match(Globals.Symbol._mulop);
					break;
				case (int)Globals.Symbol._andop:
					match(Globals.Symbol._andop);
					break;
				default:
					Error(Globals.curLine, "*, /, or &&", Globals.lexeme);
					break;
			}
		}
		
		///
		/// Implements the grammar rule: NamespaceBlock -> NamespaceBlockDecl | Classes
		///
		void NamespaceBlock()
		{
			switch ((int)Globals.token)
			{
				case (int)Globals.Symbol._namespace:
					NamespaceBlockDecl();
					break;
				default:
					Classes();
					break;
			}
		}
		
		///
		/// Implements the grammar rule: NamespaceBlockDecl -> [namespace] ClassOrNamespace [{] Classes [}]
		///
		void NamespaceBlockDecl()
		{
			match(Globals.Symbol._namespace);
			ClassOrNamespace();
			
			/* don't forget to add the namespace to the symbol table to give classes a parent*/
			AddSymbol(Globals.Symbol._unknown, Globals.Symbol._public, Globals.Symbol._namespace, Globals.Symbol._namespace, Globals.lexeme, Globals.depth);
			
			match(Globals.Symbol._lbrace);
			Classes();
			match(Globals.Symbol._rbrace);
		}
		
		///
		/// Implements the grammar rule: Out_Stat -> [write] [(] Write_List [)] | [writeln] [(] Write_list [)]
		///
		void Out_Stat()
		{
			switch ((int)Globals.token)
			{
				case (int)Globals.Symbol._write:
					match(Globals.Symbol._write);
					match(Globals.Symbol._lparen);
					Write_List();
					match(Globals.Symbol._rparen);
					break;
				case (int)Globals.Symbol._writeln:
					match(Globals.Symbol._writeln);
					match(Globals.Symbol._lparen);
					Write_List();
					match(Globals.Symbol._rparen);
					emit("  WRLN\n");
					break;
				default:
					Error(Globals.curLine, "'write' or 'writeln'", Globals.lexeme);
					break;
			}
		}
		
		///
		/// Implements the grammar rule: ParamList -> Mode Type [idt] ParamTail | [return] Expr [;] | lambda
		///
		void ParamList()
		{
			Globals.Symbol mode = Globals.Symbol._unknown, type;
			Element e = null;
			
			/* start updating varLoc */
			inParam = true;
			
			switch ((int)Globals.token)
			{
				case (int)Globals.Symbol._ref:
				case (int)Globals.Symbol._out:
					Mode(ref mode);
					Type(out type);
					e = AddSymbol(mode, Globals.Symbol._private, type, Globals.token, Globals.lexeme, Globals.depth);
					
					/* play with our new symbol */
					if (e != null)
					{
						int mysize = 0;
						bool cont = false;
						
						switch ((int)e.GetVType())
						{
							case (int)Element.VarType.intType:
								e.SetInteger();
								mysize = 2;
								cont = true;
								break;
							case (int)Element.VarType.floatType:
								e.SetFloat();
								mysize = 4;
								cont = true;
								break;
							case (int)Element.VarType.charType:
								e.SetCharacter();
								mysize = 1;
								cont = true;
								break;
							default:
								break;
						}
						
						/* if we hit a real variable, set properties */
						if (cont)
						{
							e.SetSizeOfLocals(mysize);
							e.SetOffset((localoffset * offsetmul) - (mysize * offsetmul));
							e.parent = parent;
					
							funcSize += mysize;
							localoffset += (mysize * offsetmul);
						}
					}
					
					match(Globals.Symbol._identifier);
					ParamTail();
					break;
				case (int)Globals.Symbol._int:
				case (int)Globals.Symbol._float:
				case (int)Globals.Symbol._char:
				case (int)Globals.Symbol._void:
					Type(out type);
					e = AddSymbol(mode, Globals.Symbol._private, type, Globals.token, Globals.lexeme, Globals.depth);
					
					/* play with our new symbol */
					if (e != null)
					{
						int mysize = 0;
						bool cont = false;
				
						switch ((int)e.GetVType())
						{
							case (int)Element.VarType.intType:
								e.SetInteger();
								mysize = 2;
								cont = true;
								break;
							case (int)Element.VarType.floatType:
								e.SetFloat();
								mysize = 4;
								cont = true;
								break;
							case (int)Element.VarType.charType:
								e.SetCharacter();
								mysize = 1;
								cont = true;
								break;
							default:
								break;
						}
						
						/* if we hit a real variable, set properties */
						if (cont)
						{
							// we don't want to count parameters towards locals
							//e.SetSizeOfLocals(mysize);
							
							e.parent = parent;
							
							if (e.parent.GetName() == currentclass.GetName())
								e.SetOffset((localoffset * offsetmul) - (mysize * offsetmul));
							else
								e.SetOffset(localoffset * offsetmul);
							
							funcSize += mysize;
							if (e.parent.GetName() != currentclass.GetName())
								localoffset += (mysize * offsetmul);
							
							if (e.parent.GetEType() == Element.EntryType.methodType)
								e.parent.SetSizeOfParams(e.parent.GetSizeOfParams() + mysize);
						}
					}
					
					match(Globals.Symbol._identifier);
					ParamTail();
					break;
				case (int)Globals.Symbol._return:
					match(Globals.Symbol._return);
					string a = "", b;
					Expr(a, out b);
					match(Globals.Symbol._semicolon);
					break;
				default:
					break;
			}
			
			/* done! this should already by unset */
			inParam = false;
		}
		
		///
		/// Implements the grammar rule: ParamTail -> [,] Mode Type [idt] ParamTail | lambda
		///
		void ParamTail()
		{
			Globals.Symbol mode = Globals.Symbol._private, type;

			switch ((int)Globals.token)
			{
				case (int)Globals.Symbol._comma:
					match(Globals.Symbol._comma);
					Mode(ref mode);
					Type(out type);
					Element e = AddSymbol(mode, Globals.Symbol._private, type, Globals.token, Globals.lexeme, Globals.depth);
					
					/* play with our new symbol */
					if (e != null)
					{
						int mysize = 0;
						bool cont = false;
				
						switch ((int)e.GetVType())
						{
							case (int)Element.VarType.intType:
								e.SetInteger();
								mysize = 2;
								cont = true;
								break;
							case (int)Element.VarType.floatType:
								e.SetFloat();
								mysize = 4;
								cont = true;
								break;
							case (int)Element.VarType.charType:
								e.SetCharacter();
								mysize = 1;
								cont = true;
								break;
							default:
								break;
						}
						
						/* if we hit a real variable, set properties */
						if (cont)
						{
							if (!inParam)
								e.SetSizeOfLocals(mysize);
							e.parent = parent;
							
							if (e.parent.GetName() == currentclass.GetName())
								e.SetOffset((localoffset * offsetmul) - (mysize * offsetmul));
							else
								e.SetOffset(localoffset * offsetmul);
							
							funcSize += mysize;
							if (e.parent.GetName() != currentclass.GetName())
								localoffset += (mysize * offsetmul);
							
							if (e.parent.GetEType() == Element.EntryType.methodType)
								e.parent.SetSizeOfParams(e.parent.GetSizeOfParams() + mysize);
						}
					}
					
					match(Globals.Symbol._identifier);
					ParamTail();
					break;
				default:
					break;
			}
		}
		
		///
		/// Implements the grammar rule: Params -> [idt] ParamsTail | [num] ParamsTails | lambda
		///
		void Params(ref string pushStats)
		{
			switch ((int)Globals.token)
			{
				case (int)Globals.Symbol._identifier:
					/* make sure it's a valid symbol */
					Element e = Globals.symTab.Lookup(Globals.lexeme);
					if (e == null)
						Error("error: " + Globals.filename + ":" + Globals.curLine + ": undeclared variable \"" + Globals.lexeme + "\"");
					
					/* prepend the new symbol to our list */
					if (pushStats.Length > 0)
						pushStats = "  PUSH " + e.GetOffsetName() + "\n" + pushStats;
					else
						pushStats = "  PUSH " + e.GetOffsetName();
					
					match(Globals.Symbol._identifier);
					ParamsTail(ref pushStats);
					break;
				case (int)Globals.Symbol._number:
				case (int)Globals.Symbol._numfloat:
					/* prepend the new symbol to our list */
					if (pushStats.Length > 0)
						pushStats = "  PUSH " + Globals.lexeme + "\n" + pushStats;
					else
						pushStats = "  PUSH " + Globals.lexeme;
					
					match(Globals.token);
					ParamsTail(ref pushStats);
					break;
				default:
					break;
			}
		}
		
		///
		/// Implements the grammar rule: ParamsTail -> [,] [idt] ParamsTail | [,] [num] ParamsTail | lambda
		///
		void ParamsTail(ref string pushStats)
		{
			if (Globals.token == Globals.Symbol._comma)
			{
				match(Globals.Symbol._comma);
				switch ((int)Globals.token)
				{
					case (int)Globals.Symbol._identifier:
						/* make sure it's a valid symbol */
						Element e = Globals.symTab.Lookup(Globals.lexeme);
						if (e == null)
							Error("error: " + Globals.filename + ":" + Globals.curLine + ": undeclared variable \"" + Globals.lexeme + "\"");
					
						/* prepend the new symbol to our list */
						if (pushStats.Length > 0)
							pushStats = "  PUSH " + e.GetOffsetName() + "\n" + pushStats;
						else
							pushStats = "  PUSH " + e.GetOffsetName();
						
						match(Globals.Symbol._identifier);
						ParamsTail(ref pushStats);
						break;
					case (int)Globals.Symbol._number:
					case (int)Globals.Symbol._numfloat:
						/* prepend the new symbol to our list */
						if (pushStats.Length > 0)
							pushStats = "  PUSH " + Globals.lexeme + "\n" + pushStats;
						else
							pushStats = "  PUSH " + Globals.lexeme;
						
						match(Globals.token);
						ParamsTail(ref pushStats);
						break;
				}
			}
		}
		
		///
		/// Implements the grammar rule: Prog -> UsingDirective NamespaceBlock [eof]
		///
		void Prog()
		{
			UsingDirective();
			NamespaceBlock();
			match(Globals.Symbol._eof);
			
			/* if we hit EOF and didn't find Main(), error -- we don't support
			 * multiple source files
			 */
			if (!foundMain)
				Error("error: Main() not found in " + Globals.filename);
		}
		
		
		///
		/// Implements the grammar rule: Relation -> SimpleExpr
		///
		void Relation(out string lex)
		{
			SimpleExpr(out lex);
		}
		
		///
		/// Implements the grammar rule: ReturnLine -> [return] Expr [;] | lambda
		///
		void ReturnLine(bool inMain)
		{
			switch ((int)Globals.token)
			{
				case (int)Globals.Symbol._return:
					match(Globals.Symbol._return);
					
					/* Main() doesn't return a value */
					if (!inMain)
					{
						string a="", retval;
						Expr(a, out retval);
						Element r = Globals.symTab.Lookup(retval);
						if (r != null)
							emit("    _AX = " + r.GetOffsetName() + "\n");
						else
							emit("    _AX = " + retval + "\n");
					}
					
					match(Globals.Symbol._semicolon);
					break;
				default:
					break;
			}
		}
		
		///
		/// Implements the grammar rule: ShortExpr -> MoreFactor MoreTerm
		///
		void ShortExpr(string left_lexeme, out string right_lexeme)
		{
			string tmpchar, fact_right;
			MoreFactor(left_lexeme, out fact_right, "", out tmpchar, true);
			MoreTerm(fact_right, out right_lexeme, "", out tmpchar, true);
		}
		
		///
		/// Implements the grammar rule: Signop -> -
		///
		void Signop()
		{
			switch ((int)Globals.token)
			{
				case (int)Globals.Symbol._signop:
					match(Globals.Symbol._signop);
					break;
				default:
					Error(Globals.curLine, "-", Globals.lexeme);
					break;
			}
		}
		
		///
		/// Implements the grammar rule: SimpleExpr -> Term MoreTerm
		///
		void SimpleExpr(out string right_lexeme)
		{
			string term_lex, tmpchar;
			Term(out term_lex);
			MoreTerm(term_lex, out right_lexeme, "", out tmpchar, true);
		}
		
		///
		/// Implements the grammar rule: Statement -> AssignStat | IOStat
		///
		void Statement()
		{
			switch ((int)Globals.token)
			{
				case (int)Globals.Symbol._identifier:
					AssignStat();
					break;
				case (int)Globals.Symbol._read:
				case (int)Globals.Symbol._write:
				case (int)Globals.Symbol._writeln:
					IOStat();
					break;
				default:
					Error(Globals.curLine, "an identifier, 'read', 'write', or 'writeln'", Globals.lexeme);
					break;
			}
		}
		
		///
		/// Implements the grammar rule: StatList -> Statement [;] StatList | lambda
		///
		void StatList()
		{
			switch ((int)Globals.token)
			{
				case (int)Globals.Symbol._identifier:
				case (int)Globals.Symbol._read:
				case (int)Globals.Symbol._write:
				case (int)Globals.Symbol._writeln:
					Statement();
					match(Globals.Symbol._semicolon);
					StatList();
					break;
				default:
					break;
			}
		}
		
		///
		/// Implements the grammar rule: Term -> Factor MoreFactor
		///
		void Term(out string right_lexeme)
		{
			string left_lexeme, mulchar;
			Factor(out left_lexeme);
			MoreFactor(left_lexeme, out right_lexeme, "", out mulchar, true);
		}
		
		
		///
		/// Implements the grammar rule: Type -> [int] | [float] | [char] | [void]
		///
		void Type(out Globals.Symbol type)
		{
			type = Globals.token;

			switch ((int)Globals.token)
			{
				case (int)Globals.Symbol._int:
					match(Globals.Symbol._int);
					break;
				case (int)Globals.Symbol._float:
					match(Globals.Symbol._float);
					break;
				case (int)Globals.Symbol._char:
					match(Globals.Symbol._char);
					break;
				case (int)Globals.Symbol._void:
					match(Globals.Symbol._void);
					break;
				default:
					type = Globals.Symbol._unknown;
					Error(Globals.curLine, "int, float, char, or void", Globals.Tokens[(int)Globals.token]);
					break;
			}
		}
		
		///
		/// Implements the grammar rule: UsingDirective -> [using] ClassOrNamespace [;] UsingDirective | lambda
		///
		void UsingDirective()
		{
			switch ((int)Globals.token)
			{
				case (int)Globals.Symbol._using:
					match(Globals.Symbol._using);
					ClassOrNamespace();
					match(Globals.Symbol._semicolon);
					UsingDirective();
					break;
				default:
					break;
			}
		}
		
		///
		/// Implements the grammar rule: Write_List -> Write_Token Write_List_Tail
		///
		void Write_List()
		{
			Write_Token();
			Write_List_Tail();
		}
		
		///
		/// Implements the grammar rule: Write_List_Tail -> [,] Write_Token Write_List_Tail | lambda
		///
		void Write_List_Tail()
		{
			if (Globals.token == Globals.Symbol._comma)
			{
				match(Globals.Symbol._comma);
				Write_Token();
				Write_List_Tail();
			}
		}
		
		///
		/// Implements the grammar rule: Write_Token -> [idt] | [numt] | [literal]
		///
		void Write_Token()
		{
			Element w;
			
			switch ((int)Globals.token)
			{
				case (int)Globals.Symbol._identifier:
					if (CCType.IsNumeric(Globals.lexeme))
					{
						emit("  WRI " + Globals.lexeme + "\n");
						match(Globals.token);
						break;
					}
					
					/* make sure that the variable exists */
					w = Globals.symTab.Lookup(Globals.lexeme);
					if (w == null)
						Error("error: " + Globals.filename + ":" + Globals.curLine + ": attempt to write undeclared variable " + Globals.lexeme);
			
					emit("  WRI " + w.GetOffsetName() + "\n");
					match(Globals.Symbol._identifier);
					break;
				case (int)Globals.Symbol._number:
				case (int)Globals.Symbol._numfloat:
					if (CCType.IsNumeric(Globals.lexeme))
					{
						emit("  WRI " + Globals.lexeme + "\n");
						match(Globals.token);
						break;
					}
					
					/* make sure that the variable exists */
					w = Globals.symTab.Lookup(Globals.lexeme);
					if (w == null)
						Error("error: " + Globals.filename + ":" + Globals.curLine + ": attempt to write undeclared variable " + Globals.lexeme);
			
					emit("  WRI " + w.GetOffsetName() + "\n");
					match(Globals.token);
					break;
				case (int)Globals.Symbol._literal:
					/* add the string to the string table */
					StringT s = Globals.strTab.Insert(Globals.lexeme);
			
					emit("  WRS " + s.name + "\n");
					match(Globals.Symbol._literal);
					break;
				default:
					break;
			}
		}
	}
}