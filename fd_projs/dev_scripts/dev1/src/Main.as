package 
{
	import flash.display.Sprite;
	 [Doc]
    public class Main extends Sprite {
        public function Main() {
            
			
			
        }

       
    }
}

import geom.Vector2;

[struct]
final class A
{
	public var i:int;
	
	
}


[struct]
final class B
{
	public var j:uint;
}

var a:Array = [new Vector2(1, 0), new Vector2(2, 0), new Vector2(3, 0)
	
	];
function test(...rest)
{
	
	
	trace(a);
	
	a[1] = new A();
	
	trace(a);
	
}
test();


//
//var o = new O(); //o.i = 100;
//
//var w = new W()
//w.n = -5;
//
//o.vec.y = w;
//o.i = 4;
//
//trace(o.vec.y.n,o);// .n);



