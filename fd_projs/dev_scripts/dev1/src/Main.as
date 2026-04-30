package {
    import flash.display.Sprite;

    [Doc]
    public class Main extends Sprite {
        public function Main() {
           
        }
		
		
		
    }
}

var a:Vector.<P> = new <P>[ new P() ];

[struct]
final class P
{
	public var X:int;
	
	public function Test()
	{
		this["M"]();
		X = 0;
	}
	
}


P.prototype.M = function ():void 
{
	a.length = 0;
}


a[0].X = 9;
a[0].Test();


var v = a[0];
trace(v.X);
v.Test();
trace(v.X);

