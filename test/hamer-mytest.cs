// a simple C# program
class firstclass{
  public firstclass(){
  }
  public int secondclass(){
    int x,w;
    int y,z;

    w = x+y*z;
    return w;
  }
}
class someclass {
  static void Main(){
    int a;
    a=firstclass.secondclass();
    write("The value of a is ");
    write(a);
    writeln();
    return;
  }
}
