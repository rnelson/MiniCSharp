// a simple C# program

internal class firstclass
{
    public int secondclass()
    {
        int x;
        int y, z;

        var w = x + y * z;
        return w;
    }
}

internal class someclass
{
    private static void Main()
    {
        int a = firstclass.secondclass();
        write("The value of a is ");
        write(a);
        writeln();
    }
}