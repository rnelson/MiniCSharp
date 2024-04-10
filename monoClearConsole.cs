// $Id$

// we need the same namespace so the other source doesn't break
namespace nsClearConsole
{
	public class ClearConsole
	{
		public ClearConsole()
		{
			// nothing
		}

		public void Clear()
		{
			// because i don't want to spend time looking
			// for a function that's Mono-friendly when
			// I'm using Mono on my system but not for a
			// grade, I'm just gonna spit out \n a thousand
			// times; it's a bit ugly for output, but it works
			for (int num = 0; num < 1000; num++)
				System.Console.WriteLine("");
		}
	}
}
