package 
{
	import geom.Vector2;
	/**
	 * ...
	 * @author 
	 */
	public final class World 
	{
		public static var accumulateImpulses:Boolean = true;
		public static var warmStarting:Boolean = true;
		public static var positionCorrection:Boolean = true;
		
		
		public var gravity:Vector2;
		public var iterations:int;
		
		
		public var bodies:Vector.<Body> = new Vector.<Body>();
		public var joints:Vector.<Joint> = new Vector.<Joint>();
		
		public function World(gravity:Vector2,iterations:int) 
		{
			this.gravity = gravity;
			this.iterations = iterations;
		}
		
		public function AddBody(body:Body):void
		{
			bodies.push(body);			
		}
		
		public function AddJoint(joint:Joint):void
		{
			joints.push(joint);
		}
		
		public function Clear():void
		{
			bodies.length = 0;
			joints.length =0;
			arbiters.clear();
		}
		
		
	}

}