/* $Id$
 * Ross Nelson
 * Compiler Construction (CSC 446)
 * MiniCSharp/Assembly Generator
 */

#region Using declarations
using System;
using System.Text;
using System.IO;
#endregion

namespace MiniCSharp
{
	///
	/// Assembly generator - translates TAX to x86 assembly (Intel style)
	///
	/// \author Ross Nelson
	///
	public class GenAsm
	{
		private string asmfile;						/// output filename
		private FileInfo ifs;						/// FileInfo object
		private FileStream fs;						/// FileStream object
		private StreamWriter sw;					/// StreamWriter object
		private StreamReader sr;					/// StreamReader object
		private HashTable symtab;					/// symbol table
		private StringTable strtab;					/// string table
		private string t1, t2, t3, t4, t5;			/// TAC tokens
		private int tactokencount;					/// how many of the above are valid?
		private string reader;						/// the current TAC line
		
		/// Dummy constructor
		public GenAsm()
		{
			System.Console.WriteLine("Oops, someone called GenAsm() instead of GenAsm(...).  Yell at the programmer!");
			System.Environment.Exit(-1);
		}
		
		/// GenAsm constructor
		public GenAsm(string filename, HashTable symboltable, StringTable stringtable)
		{
			/* open the input file */
			ifs = new FileInfo(Globals.GetFilename(Globals.filename, '.') + ".TAC");
			sr = ifs.OpenText();
			
			/* open the output file */
			asmfile = Globals.GetFilename(filename, '.') + ".s";
			fs = new FileStream(asmfile, FileMode.Create);
			sw = new StreamWriter(fs, Encoding.ASCII);
			
			/* set the tables */
			symtab = symboltable;
			strtab = stringtable;
			
			/* get an initial set of tokens */
			GetTokens();
		}
		
		///
		/// emit() - write a line to the file
		///
		private void emit(string text)
		{
			string endline;
			
			if (text.Length > 8)
			{
				endline = text.Substring(0, 9);
				
				if (endline == "      end")
					sw.Write(text);
				else
					sw.Write(text + "\n");
			}
			else
				sw.Write(text + "\n");
		}
		
		///
		/// GetTokens() - Get tokens from the next line of text from the three address code file
		/// \return true if tokens were found, else false
		///
		public bool GetTokens()
		{
			reader = sr.ReadLine();
			char ch = (char)0;
			
			/* clear out the old tokens */
			t1 = null;
			t2 = null;
			t3 = null;
			t4 = null;
			t5 = null;
			
			/* if the string is null, we've hit EOF */
			if (reader == null)
				return false;
			
			/* trim whitespace from both ends of the line */
			reader.Trim();
			TrimReader();
			
			/* skip over blank lines */
			while (reader.Length == 0)
			{
				reader = sr.ReadLine();
				if (reader == null)
					return false;
				reader.Trim();
				TrimReader();
			}
			
			/* fill in the first token */
			ch = reader[0];
			while (ch > ' ')
			{
				t1 += ch.ToString();
				try
				{
					reader = reader.Substring(1, reader.Length - 1);
					ch = reader[0];
				}
				catch
				{
					break;
				}
			}
			tactokencount = 1;
			
			/* fill in the second token (if available) */
			TrimReader();
			if (reader.Length == 0)
				return true;
			ch = reader[0];
			while (ch > ' ')
			{
				t2 += ch.ToString();
				try
				{
					reader = reader.Substring(1, reader.Length - 1);
					ch = reader[0];
				}
				catch
				{
					break;
				}
			}
			tactokencount = 2;
			
			/* fill in the third token (if available) */
			TrimReader();
			if (reader.Length == 0)
				return true;
			ch = reader[0];
			while (ch > ' ')
			{
				t3 += ch.ToString();
				try
				{
					reader = reader.Substring(1, reader.Length - 1);
					ch = reader[0];
				}
				catch
				{
					break;
				}
			}
			tactokencount = 3;
			
			/* fill in the fourth token (if available) */
			TrimReader();
			if (reader.Length == 0)
				return true;
			ch = reader[0];
			while (ch > ' ')
			{
				t4 += ch.ToString();
				try
				{
					reader = reader.Substring(1, reader.Length - 1);
					ch = reader[0];
				}
				catch
				{
					break;
				}
			}
			tactokencount = 4;
			
			/* fill in the fifth token (if available) */
			TrimReader();
			if (reader.Length == 0)
				return true;
			ch = reader[0];
			while (ch > ' ')
			{
				t5 += ch.ToString();
				try
				{
					reader = reader.Substring(1, reader.Length - 1);
					ch = reader[0];
				}
				catch
				{
					break;
				}
			}
			tactokencount = 5;
			
			/* return true (token(s) found) */
			return true;
		}
		
		///
		/// TrimReader() - reader.Trim() isn't working
		///
		private void TrimReader()
		{
			try
			{
				char c = reader[0];
				while (Char.IsWhiteSpace(c.ToString(), 0))
				{
					try
					{
						reader = reader.Substring(1, reader.Length - 1);
						c = reader[0];
					}
					catch
					{
						break;
					}
				}
			}
			catch
			{
				return;
			}
		}
		
		///
		/// GetAddress() - Translate _BP+X -> [bp+x] (except class-scope variables), return numerics as-is
		///
		private string GetAddress(string tacstyle)
		{
			/* if we have a number, return it as-is */
			if (CCType.IsNumeric(tacstyle))
				return tacstyle;
			
			/* if it's shorter than 3 characters and non-numeric, assume it's a class-scope */
			if (tacstyle.Length < 3)
				return tacstyle;
			
			/* if we have a 'global' (class-scope) variable, return that */
			if (tacstyle.Substring(0, 3) != "_BP")
				return tacstyle;
			
			/* otherwise, return an assembly-friendly pointer */
			string asmstyle = "[bp" + tacstyle[3] + tacstyle.Substring(4, tacstyle.Length - 4) + "]";
			return asmstyle;
		}
		
		///
		/// FindMethod() - find a given method in the symbol table
		///
		private Element FindMethod(string tacname)
		{
			/* split the name into method and class */
			int period = tacname.IndexOf(".", 0, tacname.Length);
			string s_class = tacname.Substring(0, period);
			string s_method = tacname.Substring(period + 1, tacname.Length - (period + 1));
			
			/* find the method and return it or null */
			Element retval = symtab.Lookup(s_method);
			if (s_method == "Main")
				return null;
			if (retval.parent.GetName() == s_class)
				return retval;
			else
				return null;
		}
		
		///
		/// Generate() - Generate the assembly
		///
		public void Generate()
		{
			/* print out information about the program */
			Header();
			DataSegment();
			CodeSegment();
			
			/* print the program itself */
			Procedures();
			Start();
			
			/* close open streams */
			sw.Flush();
			sw.Close();
			fs.Close();
			sr.Close();
		}
		
		///
		/// Header() - write out the header
		///
		private void Header()
		{
			emit("; " + Globals.filename);
			emit("    .model small");
			emit("    .586");
			emit("    .stack 100h");
			emit("    ");
		}
		
		///
		/// DataSegment() - write out the DS
		///
		private void DataSegment()
		{
			emit("; data segment");
			emit("    .data");
			
			/* add all strings */
			for (int sc = 0; sc < strtab.numstrings; sc++)
			{
				StringT st = strtab.Lookup("_S" + sc.ToString());
				
				/* sanitize the string
				 * we don't deal with escaped characters anywhere
				 * in the lexical analyzer so this isn't an issue,
				 * but it's good to have in there
				 */
				string str = st.str.Substring(1, st.str.Length - 2);
				str = str.Replace("\"", "\", 34, \"");
				emit("_S" + sc.ToString() + " DB \"" + str + "\", '$'");
			}
			
			/* print out class-scope variables */
			emit(symtab.GenerateAssemblyData(1));
			
			emit("    ");
		}
		
		///
		/// CodeSegment() - write out the CS
		///
		private void CodeSegment()
		{
			emit("; code segment");
			emit("    .code");
			emit("    include io.asm");
		}
		
		///
		/// Procedures() - write out each procedure
		///
		private void Procedures()
		{
			string procname;
			int size_o_locals, size_o_params, locals;
			Element m;
			
			while (t1 != "START")
			{
				size_o_locals = 0;
				size_o_params = 0;
				locals = 0;
				
				switch(t1)
				{
					case "PROC":
						m = FindMethod(t2);
						if (m != null)
						{
							size_o_locals = m.GetSizeOfLocals();
							size_o_params = m.GetSizeOfParams();
						}
						else
						{
							size_o_locals = 30; /* no magic to this number */
							size_o_params = 0;
						}
						locals = size_o_locals;
						
						/* rename the procedure */
						procname = t2.Replace(".", "_");
						
						emit(procname + " proc");
						emit("    push bp");
						emit("    mov bp, sp");
						if (locals != 0)
							emit("    sub sp, " + locals);
						break;
					case "ENDP":
						m = FindMethod(t2);
						if (m != null)
						{
							size_o_locals = m.GetSizeOfLocals();
							size_o_params = m.GetSizeOfParams();
						}
						else
						{
							size_o_locals = 30; /* no magic to this number */
							size_o_params = 0;
						}
						locals = size_o_locals;
						
						/* rename the procedure */
						procname = t2.Replace(".", "_");
						
						if (locals != 0)
							emit("    add sp, " + locals);
						emit("    pop bp");
						if (size_o_params == 0)
							emit("    ret");
						else
							emit("    ret " + size_o_params);
						emit(procname + " endp");
						break;
					case "PUSH":
						if (!CCType.IsNumeric(t2))
							emit("    push " + GetAddress(t2));
						else
							emit("    push " + t2);
						break;
					case "CALL":
						emit("    call " + t2.Replace(".", "_"));
						break;
					case "WRI":
						_WriteInt(t2);
						break;
					case "WRS":
						_WriteStr(t2);
						break;
					case "WRLN":
						_WriteLn();
						break;
					case "RDI":
						_ReadInt(t2);
						break;
					default:
						/* figure out what kind of statement we have */
						if (tactokencount == 5)
						{
							switch (t4)
							{
								case "+":
									_Add(t1, t3, t5);
									break;
								case "-":
									_Sub(t1, t3, t5);
									break;
								case "/":
									_Div(t1, t3, t5);
									break;
								case "*":
									_Mul(t1, t3, t5);
									break;
								default:
									System.Console.WriteLine("Oops, unexpected situation: {0}", reader);
									break;
							}
						}
						else if (tactokencount == 4)
						{
							switch (t3)
							{
								case "-":
									_Neg(t1, t4);
									break;
								default:
									System.Console.WriteLine("Oops, unexpected situation: {0}", reader);
									break;
							}
						}
						else if (tactokencount == 3)
						{
							_Ass(t1, t3);
						}
						else
						{
							if (reader.Length > 0)
								System.Console.WriteLine("Oops, unexpected situation: {0}", reader);
						}
						
						break;
				}
				
				/* get the next set of tokens */
				if (GetTokens() == false)
					return;
			}
		}
		
		///
		/// Start() - write out the 'start' procedures
		///
		private void Start()
		{
			string firstproc = t2.Replace(".", "_");
			
			emit("start proc");
			emit("    mov ax, @data");
			emit("    mov ds, ax");
			emit("    call " + firstproc);
			emit("    mov al, 0");
			emit("    mov ah, 4ch");
			emit("    int 21h");
			emit("start endp");
			emit("      end start");
		}
		
		/// add instruction
		private void _Add(string dest, string left, string right)
		{
			/* check for special cases */
			if ((left == "1") && !CCType.IsNumeric(right))
			{
				/* inc is faster than adding one */
				emit("    inc " + GetAddress(right));
				return;
			}
			else if ((right == "1") && !CCType.IsNumeric(left))
			{
				/* inc is faster than adding one */
				emit("    inc " + GetAddress(left));
				return;
			}
			else if ((left == "0") || (right == "0"))
			{
				/* we don't need to add 0, no point in putting it in the output */
				return;
			}
			else
			{
				if (CCType.IsNumeric(left))
				{
					if (CCType.IsNumeric(right))
					{
						emit("    mov ax, " + left);
						emit("    add ax, " + right);
						emit("    mov " + GetAddress(dest) + ", ax");
					}
					else
					{
						emit("    mov ax, " + left);
						emit("    add ax, " + GetAddress(right));
						emit("    mov " + GetAddress(dest) + ", ax");
					}
				}
				else if (CCType.IsNumeric(right))
				{
					emit("    mov ax, " + GetAddress(left));
					emit("    add ax, " + right);
					emit("    mov " + GetAddress(dest) + ", ax");
				}
				else
				{
					emit("    mov ax, " + GetAddress(left));
					emit("    add ax, " + GetAddress(right));
					emit("    mov " + GetAddress(dest) + ", ax");
				}
				return;
			}
		}
		
		/// sub instruction
		private void _Sub(string dest, string left, string right)
		{
			/* check for special cases */
			if ((left == "1") && !CCType.IsNumeric(right))
			{
				/* dec is faster than adding one */
				emit("    dec " + GetAddress(right));
				return;
			}
			else if ((right == "1") && !CCType.IsNumeric(left))
			{
				/* dec is faster than adding one */
				emit("    dec " + GetAddress(left));
				return;
			}
			else if ((left == "0") || (right == "0"))
			{
				/* we don't need to subtract 0, no point in putting it in the output */
				return;
			}
			else
			{
				if (CCType.IsNumeric(left))
				{
					if (CCType.IsNumeric(right))
					{
						emit("    mov ax, " + left);
						emit("    sub ax, " + right);
						emit("    mov " + GetAddress(dest) + ", ax");
					}
					else
					{
						emit("    mov ax, " + left);
						emit("    sub ax, " + GetAddress(right));
						emit("    mov " + GetAddress(dest) + ", ax");
					}
				}
				else if (CCType.IsNumeric(right))
				{
					emit("    mov ax, " + GetAddress(left));
					emit("    sub ax, " + right);
					emit("    mov " + GetAddress(dest) + ", ax");
				}
				else
				{
					emit("    mov ax, " + GetAddress(left));
					emit("    sub ax, " + GetAddress(right));
					emit("    mov " + GetAddress(dest) + ", ax");
				}
				return;
			}
		}
		
		/// mul (imul) instruction
		private void _Mul(string dest, string left, string right)
		{
			/* check for special cases */
			if (left == "0")
			{
				emit("    mov " + GetAddress(right) + ", 0");
				return;
			}
			else if (right == "0")
			{
				emit("    mov " + GetAddress(left) + ", 0");
				return;
			}
			else if ((left == "1") || (right == "1"))
			{
				/* we don't need to subtract 0, no point in putting it in the output */
				return;
			}
			else
			{
				emit("    push bx");
				emit("    mov ax, " + GetAddress(left));
				emit("    mov bx, " + GetAddress(right));
				emit("    imul bx");
				emit("    mov " + GetAddress(dest) + ", ax");
				emit("    pop bx");
				return;
			}
		}
		
		/// div (idiv) instruction
		private void _Div(string dest, string left, string right)
		{
			/* check for special cases */
			if (left == "0")
			{
				emit("    mov " + GetAddress(right) + ", 0");
				return;
			}
			else if (right == "0")
			{
				emit("    mov " + GetAddress(left) + ", 0");
				return;
			}
			else if ((left == "1") || (right == "1"))
			{
				/* we don't need to subtract 0, no point in putting it in the output */
				return;
			}
			else
			{
				emit("    push bx");
				emit("    mov ax, " + GetAddress(left));
				emit("    mov bx, " + GetAddress(right));
				emit("    idiv bx");
				emit("    mov " + GetAddress(dest) + ", ax");
				emit("    pop bx");
				return;
			}
		}
		
		/// neg instruction
		private void _Neg(string dest, string right)
		{
			if (CCType.IsNumeric(right))
			{
				emit("    mov " + GetAddress(dest) + ", -" + right);
				return;
			}
			else
			{
				emit("    mov ax, " + GetAddress(right));
				emit("    neg ax");
				emit("    mov " + GetAddress(dest) + ", ax");
				return;
			}
		}
		
		/// assignment instruction
		private void _Ass(string dest, string src)
		{
			if (dest == src)
			{
				/* we don't need to do 'a = a' */
				return;
			}
			else
			{
				if (dest == "_AX")
					emit("    mov ax, " + GetAddress(src));
				else if (src == "_AX")
					emit("    mov " + GetAddress(dest) + ", ax");
				else
				{
					if (CCType.IsNumeric(src))
						emit("    mov ax, " + src);
					else
					{
						if (src[0] == '-')
						{
							_Neg(dest, src.Substring(1, src.Length - 1));
							return;
						}
						else
							emit("    mov ax, " + GetAddress(src));
					}
					emit("    mov " + GetAddress(dest) + ", ax");
				}
				return;
			}
		}
		
		/// writestr instruction
		private void _WriteStr(string strname)
		{
			emit("    push dx");
			emit("    mov dx, OFFSET _S" + strname[2].ToString());
			emit("    call writestr");
			emit("    pop dx");
			return;
		}
		
		/// writeint instruction
		private void _WriteInt(string intname)
		{
			if (CCType.IsNumeric(intname))
				emit("    mov ax, " + intname);
			else
				emit("    mov ax, " + GetAddress(intname));
			emit("    call writeint");
			return;
		}
		
		/// writeln instruction
		private void _WriteLn()
		{
			emit("    call writeln");
			return;
		}
		
		/// readint instruction
		private void _ReadInt(string addr)
		{
			emit("    push bx");
			emit("    call readint");
			emit("    mov " + GetAddress(addr) + ", bx");
			emit("    pop bx");
			return;
		}
	}
}
