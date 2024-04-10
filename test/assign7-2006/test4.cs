internal class firstclass
{
    public int secondclass()
    {
        var a = 5;
        var b = 10;
        var d = 20;
        var c = d + a * b;
        return c;
    }
}

internal class someclass
{
    private static void Main()
    {
        int a;

        firstclass.secondclass();
    }
}