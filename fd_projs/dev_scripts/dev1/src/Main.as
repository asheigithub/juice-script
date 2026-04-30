package {
    import flash.display.Sprite;

    [Doc]
    public class Main extends Sprite {
        public function Main() {
           
        }
		
		
		
    }
}

var a = [ new P() ];

[struct]
final class P
{
	public var X:int;
	
	public function Test()
	{
		
		
	
		trace(X);
		//a.length = 0;
		X = 7;
		
		trace(X);
		
	}
	
}



a[0].X = 9;
a[0].Test();
trace(a[0].X);



