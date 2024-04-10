/* $Id$
 * Ross Nelson
 * Compiler Construction (CSC 446)
 * MiniCSharp/Hash Table
 */

#region Using declarations
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
#endregion

namespace MiniCSharp
{
	///
	/// Hash table for the mini C# compiler
	///
	/// \author Ross Nelson
	///
	public class HashTable
	{
		///
		/// Prime number to be used for the table size
		///
		public static uint PRIMENUM = 50333;		// yay for prime-numbers.org
		
		///
		/// The array holding pointers to Element objects
		///
		Element[] vertArray;
		
		///
		/// Constructor for the hash table class
		///
		public HashTable()
		{
			/* allocate space for the hash values */
			vertArray = new Element[PRIMENUM];
			InitTable();
		}
		
		///
		/// Initialize the hash table
		///
		public void InitTable()
		{
			for (int i = 0; i < PRIMENUM; i++)
				vertArray[i] = null;
		}
		
		///
		/// Completely remove a depth from the hash table
		/// \param depth depth to remove
		///
		public void DeleteDepth(int depth)
		{
			if (Globals.VISUAL)
				WriteTable(Globals.depth);
			
			/* we don't want to kill depths 1, 2 */
			if (depth < 2)
				return;

			for(int loc = 0; loc < PRIMENUM; loc++)
			{
				Element e = vertArray[loc];
				
				/* if there's something there, check the depth */
				if (e != null)
				{
					/* find the start of the correct depth */
					while ((e.GetDepth() > depth) && (e.next != null))
						e = e.next;
					
					if (e == null)
						break;
					
					/* if the depth is the same, remove the node */
					while (e.GetDepth() == depth)
					{
						vertArray[loc] = e.next;	/* remove the head node */
						e = vertArray[loc];			/* reset e to the front of that linked list */
						
						if ((e == null) || (e.next == null))
							break;
					}
				}
			}
		}
		
		///
		/// Insert a new element into the hash table
		/// \param lexeme the name of the variable
		/// \param token token type
		/// \param depth the depth at which to store the Element
		/// \return pointer to the new element
		///
		public Element Insert(string lexeme, Globals.Symbol token, int depth)
		{
			/* reserve memory for the new node */
			Element el = new Element();
			
			/* figure out where the node goes */
			int arrayLoc = hash(lexeme);
			
			/* set values for the node */
			el.InitValues();
			el.SetName(lexeme);
			el.SetToken(token);
			el.SetDepth((int)depth);	// for some reason, mono crashed without casting this for anything set to a method
			el.next = null;
			
			/* keep track of if we have a constant or not */
			bool haveConst = false;
			if (el.GetEType() == Element.EntryType.constType)
				haveConst = true;
			
			/* set the type */
			switch ((int)token)
			{
				case (int)Globals.Symbol._int:
					el.SetInteger();
					break;
				case (int)Globals.Symbol._float:
					el.SetFloat();
					break;
				case (int)Globals.Symbol._char:
					el.SetCharacter();
					break;
				case (int)Globals.Symbol._const:
					el.SetConstant();
					break;
				case (int)Globals.Symbol._class:
					el.SetClass();
					break;
				default:
					el.SetMethod(Globals.Symbol._void);
					break;
			}
			
			/* add the node */
			try
			{
				Element oldHead = vertArray[arrayLoc];
				el.next = oldHead;
				vertArray[arrayLoc] = el;
				
				if (haveConst)
					el.SetConstant();
			}
			catch
			{
				vertArray[arrayLoc] = el;
				
				if (haveConst)
					el.SetConstant();
			}
			
			return el;
		}
		
		///
		/// Search the hash table for a specific element.
		/// \param lexeme variable name to find
		/// \return a pointer to the desired element, or null
		///
		public Element Lookup(string lexeme)
		{
			/* create a pointer to an Element */
			Element el;
			
			/* figure out where the node goes */
			int arrayLoc = hash(lexeme);
			
			/* find the node */
			if (vertArray[arrayLoc] == null)
			{
				el = null;
			}
			else
			{
				el = vertArray[arrayLoc];
				
				/* if needed (collisions), traverse the list to find the element */
				while (el.GetName() != lexeme)
				{
					el = el.next;
					
					/* we don't want to try to access inaccessible memory */
					if (el == null)
						return null;
				}
			}
			
			/* return whatever we found */
			return el;
		}
		
		///
		/// Print all entries at a given depth
		/// \param depth the depth to print out
		///
		public void WriteTable(int depth)
		{
			string header = "\nSymbol Table - Depth " + depth + "\n----------------------";
			System.Console.WriteLine(header);

			/* create a pointer to an Element */
			Element el;
			
			/* print out the values */
			foreach(Element e in vertArray)
			{
				el = e;
				
				if (el != null)
				{
					/* advance to the correct depth if needed */
					while (el.GetDepth() > depth)
					{
						if (el.next != null)
							el = el.next;
						else
							break;
					}
					
					while ((el != null) && (el.GetDepth() == depth))
					{
						/* increment the line count */
						Globals.linecount += 3;
						
						/* if we've printed a lot, clear the screen */
						if (Globals.linecount > 15)
						{
							/* reset the line count */
							Globals.linecount = 0;
							/* wait for the user to press enter */
							Globals.Wait("\nPress enter to continue...");
							
							/* print out the header */
							System.Console.WriteLine(header);
						}
						
						Element.EntryType etype = el.GetEType();
						Element.VarType vtype = el.GetVType();
						string lexeme = el.GetName();
						string token = Globals.Tokens[(int)el.GetToken()];
						string offset = el.GetOffset().ToString();
						string size = el.GetSizeOfLocals().ToString();
						string vartype = "", value = "", output = "";
						
						switch ((int)vtype)
						{
							case (int)Element.VarType.intType:
								vartype = "int";
								value = el.GetIntegerValue().ToString();
								break;
							case (int)Element.VarType.charType:
								vartype = "char";
								value = el.GetCharacterValue().ToString();
								break;
							case (int)Element.VarType.floatType:
								vartype = "float";
								value = el.GetFloatValue().ToString();
								break;
							case (int)Element.VarType.voidType:
								vartype = "void";
								value = "";
								break;
							default:
								vartype = "";
								value = "";
								break;
						}
						
						/* to keep the relatively readable, just shove all of the */
						/* information into one big string and spit that out to the screen */
						switch ((int)etype)
						{
							case (int)Element.EntryType.varType:
								switch ((int)el.mode)
								{
									case (int)Element.PassingMode.passOut:
										output += "out ";
										break;
									case (int)Element.PassingMode.passRef:
										output += "ref ";
										break;
									case (int)Element.PassingMode.passNorm:
									default:
										break;
								}
								output += vartype;
								if (vartype != "") output += " ";
								output += lexeme; // + " = ";
								// output += value;
								output += "\t//size:" + size;
								output += " offset:" + offset;
								break;
							case (int)Element.EntryType.constType:
								output += "const ";
								output += vartype + " ";
								output += lexeme;
								output += "\t//size:" + size;
								output += " offset:" + offset;
								
								switch ((int)el.GetVType())
								{
									case (int)Element.VarType.intType:
										output += " value:" + el.GetIntegerValue();
										break;
									case (int)Element.VarType.floatType:
										output += " value:" + el.GetFloatValue();
										break;
									default:
										break;
								}
								
								break;
							case (int)Element.EntryType.methodType:
								output += vartype;
								if (vartype != "") output += " ";
								output += lexeme + "() ";
								output += "\t//size:" + size;
								output += " params:" + el.GetNumParams();
								output += " offset:" + offset;
								if (el.childList.Length > 0)
									output += el.childList;
								break;
							case (int)Element.EntryType.classType:
								output += "class ";
								output += lexeme + " ";
								output += "\t//size:" + size;
								//output += " offset:" + offset;
								if (el.childList.Length > 0)
									output += el.childList;
								break;
							default:
								output += lexeme + ", type:" + token + " offset:" + offset;
								break;
						}
						
						if (el.location > 0)
							output += " location:" + el.location;
						
						Console.WriteLine(output);
						
						/* break out of the loop or move to the next item, whichever is needed */
						if (el.next == null)
							break;
						else
						{
							el = el.next;
							if (el == null)
								break;
						}
					}
				}
			}
		}
		
		///
		/// Get the total size for a given depth
		/// \param dep the depth
		/// \return the size of the depth
		public int GetDepthSize(int dep)
		{
			int retval = 0;
			Element search = null;
			
			foreach(Element el in vertArray)
			{
				search = el;
				
				while (search != null)
				{
					if (search.GetDepth() == dep)
						retval += search.GetSizeOfLocals();
					
					search = search.next;
				}
			}
			
			return retval;
		}
		
		///
		/// Get the smallest used offset (for newtemp()
		/// \return the smallest used offset
		public int GetMinOffset()
		{
			int retval = 0;
			Element search = null;
			
			foreach(Element el in vertArray)
			{
				search = el;
				
				while (search != null)
				{
					if (search.GetOffset() < retval)
						retval = search.GetOffset();
					
					search = search.next;
				}
			}
			
			return retval;
		}
		
		///
		/// Get a string with variables for .data
		/// \param dep the depth to print
		/// \return string containing declaractions for variables
		public string GenerateAssemblyData(int dep)
		{
			string retval = "";
			Element search = null;
			
			foreach(Element el in vertArray)
			{
				search = el;
				
				while (search != null)
				{
					if (search.GetDepth() == dep)
						if (search.GetEType() == Element.EntryType.varType)
							if (retval.Length == 0)
								retval = search.GetName() + " dw ?";
							else
								retval += "\n" + search.GetName() + " dw ?";
					
					search = search.next;
				}
			}
			
			return retval;
		}
		
		///
		/// Get child information for an element
		/// \param ele the element
		/// \return string containing child information, parsable format
		public string GetChildren(Element ele)
		{
			string retstr = "";
			Element search = null;
			
			foreach(Element elem in vertArray)
			{
				search = elem;
									
				while (search != null)
				{
					if (search.parent == ele)
					{
						switch ((int)search.mode)
						{
							case (int)Element.PassingMode.passOut:
								retstr += "out ";
								break;
							case (int)Element.PassingMode.passRef:
								retstr += "ref ";
								break;
							case (int)Element.PassingMode.passNorm:
							default:
								break;
						}
						switch ((int)search.GetVType())
						{
							case (int)Globals.Symbol._int:
								retstr += "int ";
								break;
							case (int)Globals.Symbol._char:
								retstr += "char ";
								break;
							case (int)Globals.Symbol._float:
								retstr += "float ";
								break;
							default:
								break;
						}
						retstr += search.GetName();
						retstr += " | ";
					}
										
					search = search.next;
				}
			}
			
			return retstr;
		}
		
		///
		/// Get child information for an element
		/// \param ele the element
		/// \return string containing child information, printable format
		public string GetChildrenPrint(Element ele)
		{
			string retstr = "\n";
			Element search = null;
			
			foreach(Element elem in vertArray)
			{
				search = elem;
									
				while (search != null)
				{
					if (search.parent == ele)
					{
						retstr += " => ";
						switch ((int)search.mode)
						{
							case (int)Element.PassingMode.passOut:
								retstr += "out ";
								break;
							case (int)Element.PassingMode.passRef:
								retstr += "ref ";
								break;
							case (int)Element.PassingMode.passNorm:
							default:
								break;
						}
						switch ((int)search.GetVType())
						{
							case (int)Globals.Symbol._int:
								retstr += "int ";
								break;
							case (int)Globals.Symbol._char:
								retstr += "char ";
								break;
							case (int)Globals.Symbol._float:
								retstr += "float ";
								break;
							default:
								break;
						}
						retstr += search.GetName();
						retstr += "\n";
					}
										
					search = search.next;
				}
			}
			
			return retstr;
		}
		
		///
		/// Implementation of hashpjw, page 436 of Compilers (dragon)
		/// \param name string to hash
		///
		private static int hash(string name)
		{
			long h = 0, g;
			
			foreach(char ch in name)
			{
				h = (h << 4) + (int)ch;
				
				if ((g = h & 0xf0000000) != 0)
				{
					h = h ^ (g >> 24);
					h = h ^ g;
				}
			}
			
			return Math.Abs((int)(h % PRIMENUM));
		}
	}
}
