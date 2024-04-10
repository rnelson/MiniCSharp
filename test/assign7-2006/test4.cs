class firstclass{
  public firstclass(){
  }
  public int secondclass(){
    int a,b,c,d;

    a=5;
    b=10;
    d=20;
    c = d+a*b;
    return c;
  }
}
class someclass {
  static void Main(){
    int a;

    firstclass.secondclass();
    return;
  }
}
