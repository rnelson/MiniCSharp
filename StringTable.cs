/* $Id$
 * Ross Nelson
 * Compiler Construction (CSC 446)
 * MiniCSharp/String Table
 */

#region Using declarations
using System;
using System.Text;
using System.IO;
#endregion

namespace MiniCSharp
{
	///
	/// String table for the mini C# compiler
	///
	/// \author Ross Nelson
	///
	public class StringTable
	{
		/// the maximum number of strings
		private int MAXSTRINGS = 99999;
		
		/// keep track of the number of strings in the table
		public int numstrings;
		
		/// our array
		private StringT[] vertArray;
		
		///
		/// Constructor for the string table class
		///
		public StringTable()
		{
			/* allocate space for the hash values */
			vertArray = new StringT[MAXSTRINGS];
			InitTable();
		}
		
		///
		/// Initialize the hash table
		///
		public void InitTable()
		{
			for (int i = 0; i < MAXSTRINGS; i++)
				vertArray[i] = null;
		}
		
		///
		/// Insert a new string into the table
		/// \param str the value of the new string
		/// \return pointer to the new element
		///
		public StringT Insert(string str)
		{
			string insstr = str;
			
			/* error out if we have no more memory */
			if (numstrings >= MAXSTRINGS)
			{
				System.Console.WriteLine("error: no more memory available for additional strings, terminating");
				System.Environment.Exit(-3);
			}
			
			/* check for duplicates */
			for (int i = 0; i < numstrings; i++)
			{
				if (vertArray[i].str == str)
					return vertArray[i];
			}
			
			/* MASM 6.14 and 6.15 reject empty strings; change them to a space */
			if (str == "\"\"")
				insstr = "\" \"";
			
			/* set the new string and add it */
			StringT el = new StringT();
			el.name = "_S" + numstrings.ToString();
			el.str = insstr;
			vertArray[numstrings] = el;
			numstrings++;
			
			return el;
		}
		
		///
		/// Search the string table for a specific string.
		/// \param name string name to find
		/// \return a pointer to the desired element, or null
		///
		public StringT Lookup(string name)
		{
			/* create a pointer to a StringT */
			StringT el = null;
			
			/* find the string */
			for (int arrayLoc = 0; arrayLoc < numstrings; arrayLoc++)
			{
				el = vertArray[arrayLoc];
				if (el.name == name)
					return el;
			}
			
			/* return whatever we found */
			return null;
		}
		
		/// Print out the entire string table (for debugging purposes)
		public void PrintTable()
		{
			System.Console.WriteLine("Name  Value\n----  -----");
			for (int arrayLoc = 0; arrayLoc < numstrings; arrayLoc++)
			{
				System.Console.WriteLine("{0}   {1}", vertArray[arrayLoc].name, vertArray[arrayLoc].str);
			}
		}
	}
	
	///
	/// String table object for the mini C# compiler
	///
	/// \author Ross Nelson
	///
	public class StringT
	{
		public string str;
		public string name;
	}
}
