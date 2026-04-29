package {
    import flash.display.Sprite;

    [Doc]
    public class Main extends Sprite {
        public function Main() {
            
        }
    }
}


var a = new <P>[ new P() ];

[struct]
final class P
{
	public var X:int;
	
	public function Test():void
	{
		trace(X);
	}
	
}


a[0].Test();
