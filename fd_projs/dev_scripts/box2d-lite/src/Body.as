package 
{
	import geom.Vector2;
	
	
	public final class Body 
	{
		public var position:Vector2;
		public var rotation:float;
		
		public var velocity:Vector2;
		public var angularVelocity:float;
		
		public var force:Vector2;
		public var torque:float;
		
		public var width:Vector2;
		
		public var friction:float;
		public var mass:float;
		public var invMass:float;
		public var I:float;
		public var invI:float;
		
		public var id:int;
		
		public static const FLT_MAX:float = 3.402823466e+38F;
		
		private static var idseed:int = 0;
		
		public function Body()
		{
			id = idseed++;
			
			//position.Set(0.0f, 0.0f);
			rotation = 0.0f;
			//velocity.Set(0.0f, 0.0f);
			angularVelocity = 0.0f;
			//force.Set(0.0f, 0.0f);
			torque = 0.0f;
			friction = 0.2f;
			
			width = new Vector2(1.0f, 1.0f);
			
			mass = FLT_MAX;
			invMass = 0.0f;
			I = FLT_MAX;
			invI = 0.0f;
			
		}
		
		public function Set(w:Vector2, m:float):void
		{
			
			position = new Vector2(); //.Set(0.0f, 0.0f);
			rotation = 0.0f;
			velocity = new Vector2(); //.Set(0.0f, 0.0f);
			angularVelocity = 0.0f;
			force = new Vector2(); //.Set(0.0f, 0.0f);
			torque = 0.0f;
			friction = 0.2f;

			width = w;
			mass = m;

			if (mass < FLT_MAX)
			{
				invMass = 1.0f / mass;
				I = mass * (width.x * width.x + width.y * width.y) / 12.0f;
				invI = 1.0f / I;
			}
			else
			{
				invMass = 0.0f;
				I = FLT_MAX;
				invI = 0.0f;
			}
		}
		
		
		public function AddForce(f:Vector2):void 
		{
			force += f;
		}	
	}

}