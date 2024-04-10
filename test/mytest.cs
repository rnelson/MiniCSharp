// This is the file I'm testing with...

internal class firstclass
{
    private int i, j;

    private int firstclass2(int a, int b)
    {
        write("Enter a number: ");
        read(b);
        b = -b;
        writeln("-b = ", b);

        i = 4;
        j = 2;
        write("The answer to life, the universe, and everything is ", i, j);
    }
}

internal class someclass
{
    private static void Main()
    {
        int cd, dc;
        firstclass.firstclass2(cd, dc);
    }
}
// $Id$