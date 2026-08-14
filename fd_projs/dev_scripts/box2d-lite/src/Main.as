package
{
	import flash.display.Sprite;
	import geom.Vector2;
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
		
		
		private static var bomb:Body;
		public static function LaunchBomb()
		{
			if (!bomb)
			{
				bomb = new Body();
				bomb.Set(new Vector2(1.0f, 1.0f), 50.0f);
				bomb.friction = 0.2f;
				world.AddBody(bomb);
				
			}

			bomb.position = new Vector2( Math.random() * 30.0f -15.0f , 15.0f);
			bomb.rotation = Math.random() * 3.0f - 1.5f;// ( -1.5f, 1.5f);
			bomb.velocity = -1.5f * bomb.position;
			bomb.angularVelocity = Math.random() * 40.0f -20.0f; //Random(-20.0f, 20.0f);
		}
		
		
		public static function Demo1():void 
		{
			world.Clear();
			bomb = null;
			
			var b:Body = new Body();	
			b.Set(new Vector2(100.0f, 20.0f), Body.FLT_MAX);
			b.position = new Vector2(0.0f, -0.5f * b.width.y);
			world.AddBody(b);
			
			b = new Body();	
			b.Set(new Vector2(1.0f, 1.0f), 200.0f);
			b.position = new Vector2(0.0f, 4.0f);
			world.AddBody(b);
		}
		
		public static function Demo2():void
		{
			world.Clear();
			bomb = null;
			
			var b1:Body = new Body();
			b1.Set(new Vector2(100.0f, 20.0f), Body.FLT_MAX);
			b1.friction = 0.2f;
			b1.position = new Vector2(0.0f, -0.5f * b1.width.y);
			b1.rotation = 0.0f;
			world.AddBody(b1);

			var b2:Body = new Body();
			b2.Set(new Vector2(1.0f, 1.0f), 100.0f);
			b2.friction = 0.2f;
			b2.position = new Vector2(9.0f, 11.0f);
			b2.rotation = 0.0f;
			world.AddBody(b2);

			var j:Joint = new Joint();
			j.Set(b1, b2, new Vector2(0.0f, 11.0f));
			world.AddJoint(j);
			
		}
		
		public static function Demo3():void
		{
			world.Clear();
			bomb = null;
			
			var b:Body = new Body();
			
			b.Set(new Vector2(100.0f, 20.0f), Body.FLT_MAX);
			b.position= new Vector2(0.0f, -0.5f * b.width.y);
			world.AddBody(b);
				
			b = new Body();
			b.Set(new Vector2(13.0f, 0.25f), Body.FLT_MAX);
			b.position =new Vector2(-2.0f, 11.0f);
			b.rotation = -0.25f;
			world.AddBody(b);
			
			b = new Body();
			b.Set(new Vector2(0.25f, 1.0f), Body.FLT_MAX);
			b.position = new Vector2(5.25f, 9.5f);
			world.AddBody(b);
			
			b = new Body();
			b.Set(new Vector2(13.0f, 0.25f), Body.FLT_MAX);
			b.position = new Vector2(2.0f, 7.0f);
			b.rotation = 0.25f;
			world.AddBody(b);
			
			b = new Body();
			b.Set(new Vector2(0.25f, 1.0f), Body.FLT_MAX);
			b.position = new Vector2(-5.25f, 5.5f);
			world.AddBody(b);
			
			b = new Body();
			b.Set(new Vector2(13.0f, 0.25f), Body.FLT_MAX);
			b.position = new Vector2(-2.0f, 3.0f);
			b.rotation = -0.25f;
			world.AddBody(b);
			
			var friction:Array = [0.75f, 0.5f, 0.35f, 0.1f, 0.0f];
			
			//float friction[5] = {0.75f, 0.5f, 0.35f, 0.1f, 0.0f};
			for (var i:int = 0; i < 5; ++i)
			{
				b = new Body();
				
				b.Set(new Vector2(0.5f, 0.5f), 25.0f);
				b.friction = friction[i];
				b.position = new Vector2(-7.5f + 2.0f * i, 14.0f);
				world.AddBody(b);
				
			}
			
		}
		
		
		public static function Demo4():void
		{
			world.Clear();
			bomb = null;
			
			var b:Body = new Body();
			b.Set(new Vector2(100.0f, 20.0f), Body.FLT_MAX);
			b.friction = 0.2f;
			b.position = new Vector2(0.0f, -0.5f * b.width.y);
			b.rotation = 0.0f;
			world.AddBody(b);
			
			for (var i:int = 0; i < 10; ++i)
			{
				b = new Body();
				b.Set(new Vector2(1.0f, 1.0f), 1.0f);
				b.friction = 0.2f;
				var x:float = Math.random() * 0.2f - 0.1f; //Random(-0.1f, 0.1f);
				b.position= new Vector2(x, 0.51f + 1.05f * i);
				world.AddBody(b);
				
			}
			
		}
		
		public static function Demo5():void
		{
			world.Clear();
			bomb = null;
			
			var b:Body = new Body();
			
			b.Set(new Vector2(100.0f, 20.0f), Body.FLT_MAX);
			b.friction = 0.2f;
			b.position= new Vector2(0.0f, -0.5f * b.width.y);
			b.rotation = 0.0f;
			world.AddBody(b);
			
			var x:Vector2 = new Vector2(-6.0f, 0.75f);
			var y:Vector2 = new Vector2();

			for (var i:int = 0; i < 10; ++i)
			{
				y = x;

				for (var j:int = i; j < 10; ++j)
				{
					b = new Body();
					
					b.Set(new Vector2(1.0f, 1.0f), 10.0f);
					b.friction = 0.2f;
					b.position = y;
					world.AddBody(b);
					
					y += new Vector2(1.125f, 0.0f);
				}

				//x += Vec2(0.5625f, 1.125f);
				x += new Vector2(0.5625f, 2.0f);
			}
		}
		
		public static  function Demo6():void
		{
			world.Clear();
			bomb = null;
			
			
			var b1:Body = new Body();
			b1.Set(new Vector2(100.0f, 20.0f), Body.FLT_MAX);
			b1.position = new Vector2(0.0f, -0.5f * b1.width.y);
			world.AddBody(b1);

			var b2:Body = new Body();
			b2.Set(new Vector2(12.0f, 0.25f), 100.0f);
			b2.position = new Vector2(0.0f, 1.0f);
			world.AddBody(b2);

			var b3:Body = new Body();
			b3.Set(new Vector2(0.5f, 0.5f), 25.0f);
			b3.position = new Vector2(-5.0f, 2.0f);
			world.AddBody(b3);

			var b4:Body = new Body();
			b4.Set(new Vector2(0.5f, 0.5f), 25.0f);
			b4.position = new Vector2(-5.5f, 2.0f);
			world.AddBody(b4);

			var b5:Body = new Body();
			b5.Set(new Vector2(1.0f, 1.0f), 100.0f);
			b5.position = new Vector2(5.5f, 15.0f);
			world.AddBody(b5);

			var j:Joint = new Joint();
			j.Set(b1, b2, new Vector2(0.0f, 1.0f));
			world.AddJoint(j);

			
		}
		
		
		public static function Demo7():void
		{
			world.Clear();
			bomb = null;
			
			
			var b:Body = new Body();
			b.Set(new Vector2(100.0f, 20.0f), Body.FLT_MAX);
			b.friction = 0.2f;
			b.position = new Vector2(0.0f, -0.5f * b.width.y);
			b.rotation = 0.0f;
			world.AddBody(b);

			const numPlanks:int = 15;
			var mass:float = 50.0f;

			for (var i:int = 0; i < numPlanks; ++i)
			{
				b = new Body();
				b.Set(new Vector2(1.0f, 0.25f), mass);
				b.friction = 0.2f;
				b.position = new Vector2(-8.5f + 1.25f * i, 5.0f);
				world.AddBody(b);
				
			}

			// Tuning
			var frequencyHz:float = 2.0f;
			var dampingRatio:float = 0.7f;

			// frequency in radians
			var omega:float = 2.0f * Mathf.PI * frequencyHz;

			// damping coefficient
			var d:float = 2.0f * mass * dampingRatio * omega;

			// spring stifness
			var k:float = mass * omega * omega;

			// magic formulas
			var softness:float = 1.0f / (d + timeStep * k);
			var biasFactor:float = timeStep * k / (d + timeStep * k);

			var j:Joint;
			for (var i:int = 0; i < numPlanks; ++i)
			{
				j = new Joint();
				j.Set(world.bodies[i] , world.bodies[i+1] , new Vector2(-9.125f + 1.25f * i, 5.0f));
				j.softness = softness;
				j.biasFactor = biasFactor;

				world.AddJoint(j);
				
			}

			j = new Joint();
			j.Set( world.bodies[  numPlanks], world.bodies[0] , new Vector2(-9.125f + 1.25f * numPlanks, 5.0f));
			j.softness = softness;
			j.biasFactor = biasFactor;
			world.AddJoint(j);
			
		}
		
		
		public static function Demo8():void
		{
			world.Clear();
			bomb = null;
			
			
			var b:Body = new Body();
			b.Set(new Vector2 (100.0f, 20.0f), Body.FLT_MAX);
			b.position = new Vector2(0.0f, -0.5f * b.width.y);
			world.AddBody(b);
			var b1 = b;
			
			b = new Body();
			b.Set(new Vector2(12.0f, 0.5f), Body.FLT_MAX);
			b.position = new Vector2(-1.5f, 10.0f);
			world.AddBody(b);


			for (var i:int = 0; i < 10; ++i)
			{
				b = new Body();
				b.Set(new Vector2(0.2f, 2.0f), 10.0f);
				b.position = new Vector2(-6.0f + 1.0f * i, 11.125f);
				b.friction = 0.1f;
				world.AddBody(b);
				
			}
			
			b = new Body();
			b.Set(new Vector2(14.0f, 0.5f), Body.FLT_MAX);
			b.position = new Vector2(1.0f, 6.0f);
			b.rotation = 0.3f;
			world.AddBody(b);
			
			var b2:Body = new Body();
			b2.Set(new Vector2(0.5f, 3.0f), Body.FLT_MAX);
			b2.position = new Vector2(-7.0f, 4.0f);
			world.AddBody(b2);
			
			var b3:Body = new Body();
			b3.Set(new Vector2(12.0f, 0.25f), 20.0f);
			b3.position = new Vector2(-0.9f, 1.0f);
			world.AddBody(b3);
			
			var j:Joint = new Joint()
			j.Set(b1, b3, new Vector2(-2.0f, 1.0f));
			world.AddJoint(j);
			
			var b4:Body = new Body();
			b4.Set(new Vector2(0.5f, 0.5f), 10.0f);
			b4.position = new Vector2(-10.0f, 15.0f);
			world.AddBody(b4);
			
			j = new Joint();
			j.Set(b2, b4, new Vector2(-7.0f, 15.0f));
			world.AddJoint(j);
			

			var b5:Body = new Body();
			b5.Set(new Vector2(2.0f, 2.0f), 20.0f);
			b5.position = new Vector2(6.0f, 2.5f);
			b5.friction = 0.1f;
			world.AddBody(b5);
			
			j = new Joint();
			j.Set(b1, b5, new Vector2(6.0f, 2.6f));
			world.AddJoint(j);
			

			var b6:Body = new Body();
			b6.Set(new Vector2(2.0f, 0.2f), 10.0f);
			b6.position = new Vector2(6.0f, 3.6f);
			world.AddBody(b6);
			
			j = new Joint();
			j.Set(b5, b6, new Vector2(7.0f, 3.5f));
			world.AddJoint(j);
			
		}
		
		public static function Demo9():void
		{
			world.Clear();
			bomb = null;
			
			var b:Body = new Body();
			b.Set(new Vector2(100.0f, 20.0f), Body.FLT_MAX);
			b.friction = 0.2f;
			b.position = new Vector2(0.0f, -0.5f * b.width.y);
			b.rotation = 0.0f;
			world.AddBody(b);

			var b1:Body = b;
			
			var mass:float = 10.0f;

			// Tuning
			var frequencyHz:float = 4.0f;
			var dampingRatio:float = 0.7f;

			// frequency in radians
			var omega:float = 2.0f * Mathf.PI * frequencyHz;

			// damping coefficient
			var d:float = 2.0f * mass * dampingRatio * omega;

			// spring stiffness
			var k:float = mass * omega * omega;

			// magic formulas
			var softness:float = 1.0f / (d + timeStep * k);
			var biasFactor:float = timeStep * k / (d + timeStep * k);

			const y:float = 12.0f;

			for (var i:int = 0; i < 15; ++i)
			{
				var x:Vector2 = new Vector2(0.5f + i, y);
				b = new Body();
				b.Set(new Vector2(0.75f, 0.25f), mass);
				b.friction = 0.2f;
				b.position = x;
				b.rotation = 0.0f;
				world.AddBody(b);

				var j:Joint = new Joint();
				j.Set(b1, b, new Vector2(float(i), y));
				j.softness = softness;
				j.biasFactor = biasFactor;
				world.AddJoint(j);

				b1 = b;
				
			}
		}
		
		
		
		public static function Step():void
		{
			world.Step(1.0 / 60);
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



//var t = getTimer();
//Main.Demo5();
//for (var k:int = 0;  k< 120 ; k++) 
//{
	//
	//world.Step(1.0f / 120);
	//
	//
//}
//trace(getTimer() - t);

//trace( world.bodies[1].position.y.toFixed(8) , world.bodies[1].velocity.y.toFixed(8) );
