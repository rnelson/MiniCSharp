/* $Id$
 * Ross Nelson
 * Compiler Construction (CSC 446)
 * MiniCSharp
 */

#region Using directives
using System;
using System.Text;
#endregion

namespace MiniCSharp
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length != 1)
			{
				usage();
				return;
			}
			
			Globals.Initialize();
			
			/* uncomment this to get a ton of stuff
			 * dumped to the console -- annoying if not
			 * debugging the program */
			Globals.VISUAL = true;
			
			/* parse the source and generate an intermediate file */
			RDP parser = new RDP(args[0]);
			
			/* generate the assembly */
			GenAsm four_star_general_assembly = new GenAsm(Globals.filename, Globals.symTab, Globals.strTab);
			four_star_general_assembly.Generate();
			
			if (Globals.VISUAL)
			{
				Console.WriteLine();
				Globals.symTab.WriteTable(0);
				Globals.Wait("\nPress enter to view the string table...");
			
				/* print out the string table */
				nsClearConsole.ClearConsole c = new nsClearConsole.ClearConsole();
				c.Clear();
				Globals.strTab.PrintTable();
				Globals.Wait("");
			}
			
			Globals.Wait("Compilation successful!\nPress enter to quit...");
		}
		
		static void usage()
		{
			System.Console.WriteLine("usage: mono minicsharp.exe filename");
		}
		
		public static void PrintHeader()
		{
			System.Console.WriteLine("Token\t\t        Lexeme\t\t        Attribute");
			System.Console.WriteLine("-----\t\t        ------\t\t        ---------");
		}
	}
}
