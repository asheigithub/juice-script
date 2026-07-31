package
{
	import flash.display.Sprite;
	
	[Doc]
	/**
	 * ...
	 * @author 
	 */
	public class Main extends Sprite 
	{
		
		public function Main() 
		{
			
		}
		
	}
	
}
import geom.Vector2;



var timeStep:float = 1.0f / 60.0f;
var iterations:int = 10;
var gravity:Vector2=new Vector2(0.0f, -10.0f);

var numBodies:int = 0;
var numJoints:int = 0;

var demoIndex:int = 0;

var world:World = new World(gravity, iterations);

function Demo1():void
{
	var b:Body = new Body();	
	b.Set(new Vector2(100.0f, 20.0f), Body.FLT_MAX);
	b.position = new Vector2(0.0f, -0.5f * b.width.y);
	world.AddBody(b);
	
	b = new Body();	
	b.Set(new Vector2(1.0f, 1.0f), 200.0f);
	b.position = new Vector2(0.0f, 4.0f);
	world.AddBody(b);
	
}


Demo1();


