package 
{
	import flash.utils.Dictionary;
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
		
		private var arbiterIndex:Dictionary = new Dictionary();
				
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
				if (aa == null && bb == null)
				{
					return 0;
				}
				else if(aa == null)
				{
					return 1;
				}
				else if (bb == null)
				{
					return -1;					
				}
				else if (aa.body1.id < bb.body1.id)
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
			
			var n = arbiters.indexOf(null);
			if (n >= 0)
			{
				
				arbiters.length = n;
			}	
			
			for (var key in arbiterIndex)
			{
				delete arbiterIndex[key];
				//trace(key,arbiterIndex[key]);
			}
			
			for (var k:int = 0; k < arbiters.length; k++) 
			{
				var a:Arbiter = arbiters[k];
				arbiterIndex[  Number( a.body1.id) * 0xffffff + a.body2.id   ] = k;
			}
			
			
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
				if (arb == null)
					continue;
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
					if (arb == null)
						continue;
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
			var bodiescount:int = bodies.length;
			
			for (var i:int = 0; i < bodiescount; i++) 
			{
				var bi:Body = bodies[i];
				
				for (var j:int = i + 1; j <  bodiescount; ++j)
				{
					var bj:Body = bodies[j];

					if (bi.invMass == 0.0f && bj.invMass == 0.0f)
						continue;
					
						
					var concats:Vector.<Concat> = new <Concat>[ new Concat(), new Concat() ];
					var newArb:Arbiter = new Arbiter(bi, bj);
					newArb.init(concats);
					
					var searchkey :Number =  Number( newArb.body1.id) * 0xffffff + newArb.body2.id  ;
					
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
						//for (var k:int = 0; k < arbiters.length	; k++) 
						//{
							//var temp = arbiters[k];
							//if(temp.body1.id == newArb.body1.id && temp.body2.id == newArb.body2.id)
							//{
								//key = temp;
								//break;
							//}
						//}
						
						var index = arbiterIndex[searchkey]; 
						  
						
						if ( index === undefined )
						{
							newArb.contacts = concats;
							arbiters.push(newArb);
							
							arbiterIndex[searchkey] = arbiters.length - 1;
							
						}
						else
						{
							
							key = arbiters[index];
							
							key.Update(concats, newArb.numContacts);								
						}
						
					}
					else
					{
						var index = arbiterIndex[searchkey]; 
						if(index !== undefined)
						{
							//arbiters.removeAt(index);
							
							arbiters[index] = null;
							
							delete arbiterIndex[searchkey];
							
							//trace("remove ", index);
							
							//for(var k in arbiterIndex)
							//{
								////trace(arbiterIndex[k],index);
								//if (arbiterIndex[k] >= index )
								//{
									//
									//arbiterIndex[k] -= 1;
									////trace("sub", arbiterIndex[k] );
								//}
							//}
							
						}
						
						//for (var k:int = 0; k < arbiters.length	; k++) 
						//{
							//var key:Arbiter = arbiters[k];
							//if(key.body1.id == newArb.body1.id && key.body2.id == newArb.body2.id)
							//{
								//
								//arbiters.removeAt(k);
								//
								//break;
							//}
						//}
					}
						
				}
							
			}
		}
		
		
	}

}

