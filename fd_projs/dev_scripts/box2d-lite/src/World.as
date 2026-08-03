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
		public var arbiters:Vector.<Arbiter> = new Vector.<Arbiter>();
		
		
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
			joints.length = 0;
			arbiters.length = 0;
		}
		
		
		public function Step(dt:float):void
		{
			var inv_dt:float = dt > 0.0f ? 1.0f / dt : 0.0f;

			arbiters.sort( function(aa:Arbiter, bb:Arbiter){
				if (aa.body1.id < bb.body1.id)
				{
					return -1;
				}
				else if ( aa.body1.id == bb.body1.id && aa.body2.id < bb.body2.id )
				{
					return -1;
				}
				else
				{
					return 1;
				}
				
			} );
			
			// Determine overlapping bodies and update contact points.
			BroadPhase();
			
			// Integrate forces.
			for (var i:int = 0; i < bodies.length; ++i)
			{
				var b:Body = bodies[i];

				if (b.invMass == 0.0f)
					continue;

				b.velocity += dt * (gravity + b.invMass * b.force);
				b.angularVelocity += dt * b.invI * b.torque;
				
				
			}
			
						
			// Perform pre-steps.
			for each (var arb:Arbiter in arbiters)
			{
				
				arb.PreStep(inv_dt);
			}
			
			for (var i:int = 0; i < joints.length; ++i)
			{
				joints[i].PreStep(inv_dt);	
			}
			
			// Perform iterations
			for (var i:int = 0; i < iterations; ++i)
			{
				for each(var arb:Arbiter in arbiters)
				{
					arb.ApplyImpulse();
					//arb->second.ApplyImpulse();
				}

				for (var j:int = 0; j < joints.length; ++j)
				{
					joints[j].ApplyImpulse();
				}
			}
			
			// Integrate Velocities
			for (var i:int = 0; i < bodies.length; ++i)
			{
				var b:Body = bodies[i];

				b.position += dt * b.velocity;
				b.rotation += dt * b.angularVelocity;

				b.force = new Vector2(0.0f, 0.0f);
				b.torque = 0.0f;
				
				
			}
			
		}
		
		private function BroadPhase():void
		{
			for (var i:int = 0; i < bodies.length; i++) 
			{
				var bi:Body = bodies[i];
				
				for (var j:int = i + 1; j <  bodies.length; ++j)
				{
					var bj:Body = bodies[j];

					if (bi.invMass == 0.0f && bj.invMass == 0.0f)
						continue;
					
					var newArb:Arbiter = new Arbiter(bi, bj);
					
					
					
					if (newArb.numContacts > 0)
					{
						
						
						//ArbIter iter = arbiters.find(key);
						//if (iter == arbiters.end())
						//{
							//arbiters.insert(ArbPair(key, newArb));
						//}
						//else
						//{
							//iter->second.Update(newArb.contacts, newArb.numContacts);
						//}
						var key:Arbiter = null;
						for (var k:int = 0; k < arbiters.length	; k++) 
						{
							var temp = arbiters[k];
							if(temp.body1.id == newArb.body1.id && temp.body2.id == newArb.body2.id)
							{
								key = temp;
								break;
							}
						}
						
						if (key == null)
						{
							arbiters.push(newArb);
							
						}
						else
						{
							key.Update(newArb.contacts, newArb.numContacts);								
						}
						
					}
					else
					{
						for (var k:int = 0; k < arbiters.length	; k++) 
						{
							var key:Arbiter = arbiters[k];
							if(key.body1.id == newArb.body1.id && key.body2.id == newArb.body2.id)
							{
								
								arbiters.removeAt(k);
								
								break;
							}
						}
					}
						
				}
							
			}
		}
		
		
	}

}

