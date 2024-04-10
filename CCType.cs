/* $Id$
 * Ross Nelson
 * Compiler Construction (CSC 446)
 * CCType - ctype.h-like functions specific to MiniCSharp
 */

#region Using directives
using System;
using System.Text;
#endregion

namespace MiniCSharp
{
	class CCType
	{
		/* IsOperator()
		 * 
		 * Return true/false based on whether the passed character
		 * is [+-/*%<>=!]
		 */
		public static bool IsOperator(char input)
		{
			try
			{
				switch (input)
				{
					case '+':
					case '-':
					case '/':
					case '*':
					case '%':
					case '<':
					case '>':
					case '=':
					case '!':
					case '&':
					case '|':
						return true;
					default:
						return false;
				}
			}
			catch (System.Exception e)
			{
				System.Console.WriteLine("Exception caught: {0}", e.Message);
				return false;
			}
		}
		
		/* IsNumeric()
		 * 
		 * Return true/false based on whether the object is a number
		 * Source: http://dotnet.org.za/deonvs/archive/2004/07/06/2579.aspx
		 */
		public static bool IsNumeric(object expression)
		{
			bool isNum;
			double retNum;
			isNum = Double.TryParse(Convert.ToString(expression), System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out retNum);
			return isNum;
		}
	}
}
