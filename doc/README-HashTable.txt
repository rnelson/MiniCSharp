/* 
 * Ross Nelson
 * CSC 446 - Assignment 4 - Hash Table
 * Due: 2006-03-15
 * $Id$
 * 
 * Below is a sample program illustrating the use of
 * my HashTable and Element classes.  They're very
 * simple to use, but keep the following in mind:
 * 
 *   1. You *MUST* run Globals.Initialize() for other
 *      classes to work.
 *   
 *   2. HashTable.Lookup() returns an Element pointer
 *        Note: do not call 'new' on the pointer
 *   
 *   3. The following are the prototypes for the hash
 *      table's public functions:
 *        
 *        HashTable()
 *        Element Insert(string lexeme, Globals.Symbol
 *          token, int depth) 
 *        Element Lookup(string lexeme)
 *        void DeleteDepth(int depth)
 *        void WriteTable(int depth)
 *   
 *   4. Various functions work in the Element class to
 *      store values and specify types of the element;
 *      a few are shown below, the rest can be seen in
 *      Element.cs.
 *   5. The following files are required for use:
 *   
 *        CCType.cs
 *        Element.cs
 *        Globals.cs
 *        HashTable.cs
 *        nsClearConsole.cs or monoClearConsole.cs
 *   
 */

using System;
using System.Text;

namespace MiniCSharp
{
	class Program
	{
		static void Main(string[] args)
		{
			/* run Global's init stuff */
			Globals.Initialize();
			
			/* create a HashTable object and an Element pointer */
			HashTable h = new HashTable();
			Element e;
			
			/* add a few elements */
			h.Insert("apple", Globals.Symbol._char, 1);
			e = h.Lookup("apple");
			if (e != null)
				e.SetValue('p');
			h.Insert("pear", Globals.Symbol._class, 1);
			e = h.Insert("banana", Globals.Symbol._int, 1);
			e.SetValue(42);
			e = h.Insert("apple", Globals.Symbol._namespace, 2);
			e.SetFloat();
			e.SetValue(3.141592653587f);
			
			h.WriteTable(1);
			h.WriteTable(2);
			h.DeleteDepth(1);
			h.WriteTable(1);
			h.WriteTable(2);
		}
	}
}
