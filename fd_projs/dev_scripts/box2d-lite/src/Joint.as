package 
{
	import geom.Vector2;
	/**
	 * ...
	 * @author 
	 */
	public final class Joint 
	{
		//Mat22 M;
		//Vec2 localAnchor1, localAnchor2;
		//Vec2 r1, r2;
		//Vec2 bias;
		//Vec2 P;		// accumulated impulse
		//Body* body1;
		//Body* body2;
		//float biasFactor;
		//float softness;
		
		public var M:Mat22 = new Mat22();
		public var localAnchor1:Vector2 = new Vector2();
		public var localAnchor2:Vector2 = new Vector2();
		public var r1:Vector2 = new Vector2();
		public var r2:Vector2 = new Vector2();
		public var bias:Vector2 = new Vector2();
		public var P:Vector2 = new Vector2();
		
		public var body1:Body;
		public var body2:Body;
		
		public var biasFactor:float = 0.2f;
		public var softness:float = 0.0f;
		
		
		public function Joint() 
		{
			
		}
		
		public function Set(b1:Body, b2:Body, anchor:Vector2)
		{
			body1 = b1;
			body2 = b2;
			
			var Rot1:Mat22 = Mat22.FromAngle(body1.rotation);
			var Rot2:Mat22 = Mat22.FromAngle(body2.rotation);
	
			var Rot1T:Mat22 = Rot1.Transpose();
			var Rot2T:Mat22 = Rot2.Transpose();

			localAnchor1 = Rot1T * (anchor - body1.position);
			localAnchor2 = Rot2T * (anchor - body2.position);

			P= new Vector2(0.0f, 0.0f);

			softness = 0.0f;
			biasFactor = 0.2f;
		}
		
		
		public function PreStep(inv_dt:float):void
		{
			// Pre-compute anchors, mass matrix, and bias.
			var Rot1:Mat22 = Mat22.FromAngle(body1.rotation);
			var Rot2:Mat22 = Mat22.FromAngle(body2.rotation);

			r1 = Rot1 * localAnchor1;
			r2 = Rot2 * localAnchor2;

			// deltaV = deltaV0 + K * impulse
			// invM = [(1/m1 + 1/m2) * eye(2) - skew(r1) * invI1 * skew(r1) - skew(r2) * invI2 * skew(r2)]
			//      = [1/m1+1/m2     0    ] + invI1 * [r1.y*r1.y -r1.x*r1.y] + invI2 * [r1.y*r1.y -r1.x*r1.y]
			//        [    0     1/m1+1/m2]           [-r1.x*r1.y r1.x*r1.x]           [-r1.x*r1.y r1.x*r1.x]
			var K1:Mat22 = new Mat22();
			K1.col1.x = body1.invMass + body2.invMass;	
			K1.col2.x = 0.0f;
			K1.col1.y = 0.0f;	
			K1.col2.y = body1.invMass + body2.invMass;

			var K2:Mat22 = new Mat22();
			K2.col1.x =  body1.invI * r1.y * r1.y;		K2.col2.x = -body1.invI * r1.x * r1.y;
			K2.col1.y = -body1.invI * r1.x * r1.y;		K2.col2.y =  body1.invI * r1.x * r1.x;

			var K3:Mat22 = new Mat22() ;
			K3.col1.x =  body2.invI * r2.y * r2.y;		K3.col2.x = -body2.invI * r2.x * r2.y;
			K3.col1.y = -body2.invI * r2.x * r2.y;		K3.col2.y =  body2.invI * r2.x * r2.x;

			var K:Mat22 = K1 + K2 + K3;
			K.col1.x += softness;
			K.col2.y += softness;

			M = K.Invert();

			var p1:Vector2 = body1.position + r1;
			var p2:Vector2 = body2.position + r2;
			var dp:Vector2 = p2 - p1;

			if (World.positionCorrection)
			{
				bias = -biasFactor * inv_dt * dp;
			}
			else
			{
				bias = new Vector2(); //.Set(0.0f, 0.0f);
			}

			if (World.warmStarting)
			{
				// Apply accumulated impulse.
				body1.velocity -= body1.invMass * P;
				body1.angularVelocity -= body1.invI * r1.cross( P);

				body2.velocity += body2.invMass * P;
				body2.angularVelocity += body2.invI * r2.cross( P);
			}
			else
			{
				P = new Vector2(); //.Set(0.0f, 0.0f);
			}
			
			
		}
		
		public function ApplyImpulse()
		{
			var dv:Vector2 = body2.velocity + MathUtil.Cross(body2.angularVelocity, r2) - body1.velocity -  MathUtil.Cross(body1.angularVelocity, r1);
			var impulse:Vector2;

			impulse = M * (bias - dv - softness * P);

			body1.velocity -= body1.invMass * impulse;
			body1.angularVelocity -= body1.invI * r1.cross(impulse);

			body2.velocity += body2.invMass * impulse;
			body2.angularVelocity += body2.invI * r2.cross(impulse);

			P += impulse;
		}

		
	}

}