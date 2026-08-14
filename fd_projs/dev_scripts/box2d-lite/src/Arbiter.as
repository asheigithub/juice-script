package 
{
	import geom.Vector2;
	import geom.Matrix2x2;
	/**
	 * ...
	 * @author 
	 */
	public final class Arbiter 
	{
		const MAX_POINTS:int = 2;
		 
		//Contact contacts[MAX_POINTS];
		
		var contacts:Vector.<Concat>; //new <Concat>[ new Concat(),new Concat() ];
		
		var numContacts:int;

		var body1:Body;
		var body2:Body;

		// Combined friction
		var friction:float;
		
		
				
		public function Arbiter(b1:Body,b2:Body ) 
		{
			
			
			if (b1.id < b2.id)
			{
				body1 = b1;
				body2 = b2;
			}
			else
			{
				body1 = b2;
				body2 = b1;
			}

			
		}
		
		public function init(temp_contacts:Vector.<Concat>)
		{
			numContacts = Collide( body1, body2,temp_contacts);
			
			friction = Mathf.sqrt(body1.friction * body2.friction);
		}
		
		private function Collide(bodyA:Body,  bodyB:Body,temp_contacts:Vector.<Concat>):int
		{
			// Setup
			var hA:Vector2 = 0.5f * bodyA.width;
			var hB:Vector2 = 0.5f * bodyB.width;

			var posA:Vector2 = bodyA.position;
			var posB:Vector2 = bodyB.position;

			var RotA:Matrix2x2 = Matrix2x2.FromAngle(bodyA.rotation); var RotB:Matrix2x2 = Matrix2x2.FromAngle(bodyB.rotation);

			var RotAT:Matrix2x2 = RotA.Transpose();
			var RotBT:Matrix2x2 = RotB.Transpose();

			var dp:Vector2 = posB - posA;
			var dA:Vector2 = RotAT * dp;
			var dB:Vector2 = RotBT * dp;

			var C:Matrix2x2 = RotAT * RotB;
			var absC:Matrix2x2 =  MathUtil.AbsM22(C);
			var absCT:Matrix2x2 = absC.Transpose();
			
			// Box A faces
			var faceA:Vector2 = MathUtil.AbsVec2(dA) - hA - absC * hB;
			
			if (faceA.x > 0.0f || faceA.y > 0.0f)
				return 0;
						
			// Box B faces
			var faceB:Vector2 = MathUtil.AbsVec2(dB) - absCT * hA - hB;
			if (faceB.x > 0.0f || faceB.y > 0.0f)
				return 0;
			
			
			// Find best axis
			var axis:int;
			var separation:float;
			var normal:Vector2;
				
			// Box A faces
			axis = Axis.FACE_A_X;
			separation = faceA.x;
			normal = dA.x > 0.0f ? RotA.col1 : -RotA.col1;
			
			const relativeTol:float = 0.95f;
			const absoluteTol:float = 0.01f;
			
			if (faceA.y > relativeTol * separation + absoluteTol * hA.y)
			{
				axis = Axis.FACE_A_Y;
				separation = faceA.y;
				normal = dA.y > 0.0f ? RotA.col2 : -RotA.col2;
			}
			
			// Box B faces
			if (faceB.x > relativeTol * separation + absoluteTol * hB.x)
			{
				axis = Axis.FACE_B_X;
				separation = faceB.x;
				normal = dB.x > 0.0f ? RotB.col1 : -RotB.col1;
			}
			
			if (faceB.y > relativeTol * separation + absoluteTol * hB.y)
			{
				axis = Axis.FACE_B_Y;
				separation = faceB.y;
				normal = dB.y > 0.0f ? RotB.col2 : -RotB.col2;
			}
			
			// Setup clipping plane data based on the separating axis
			var frontNormal:Vector2, sideNormal:Vector2;
			
			//ClipVertex incidentEdge[2];
			var incidentEdge:Vector.<ClipVertex> = new <ClipVertex>[ new ClipVertex(),new ClipVertex() ];			
			var front:float, negSide:float, posSide:float;
			var negEdge:byte, posEdge:byte;
			
			
			switch (axis) 
			{
				case Axis.FACE_A_X:
					{
						frontNormal = normal;
						front = posA.dot( frontNormal) + hA.x;
						sideNormal = RotA.col2;
						var side:float = posA.dot( sideNormal);
						negSide = -side + hA.y;
						posSide =  side + hA.y;
						negEdge = EdgeNumbers.EDGE3;
						posEdge = EdgeNumbers.EDGE1;
						ComputeIncidentEdge(incidentEdge, hB, posB, RotB, frontNormal);
					}
					break;
				case Axis.FACE_A_Y:
					{
						frontNormal = normal;
						front = posA.dot( frontNormal) + hA.y;
						sideNormal = RotA.col1;
						var side:float = posA.dot(sideNormal);
						negSide = -side + hA.x;
						posSide =  side + hA.x;
						negEdge = EdgeNumbers.EDGE2;
						posEdge = EdgeNumbers.EDGE4;
						ComputeIncidentEdge(incidentEdge, hB, posB, RotB, frontNormal);
					}
					break;
				case Axis.FACE_B_X:
					{
						frontNormal = -normal;
						front = posB.dot( frontNormal) + hB.x;
						sideNormal = RotB.col2;
						var side:float = posB.dot( sideNormal);
						negSide = -side + hB.y;
						posSide =  side + hB.y;
						negEdge = EdgeNumbers.EDGE3;
						posEdge = EdgeNumbers.EDGE1;
						ComputeIncidentEdge(incidentEdge, hA, posA, RotA, frontNormal);
					}
					break;		
				case Axis.FACE_B_Y:
					{
						frontNormal = -normal;
						front = posB.dot( frontNormal) + hB.y;
						sideNormal = RotB.col1;
						var side:float = posB.dot( sideNormal);
						negSide = -side + hB.x;
						posSide =  side + hB.x;
						negEdge = EdgeNumbers.EDGE2;
						posEdge = EdgeNumbers.EDGE4;
						ComputeIncidentEdge(incidentEdge, hA, posA, RotA, frontNormal);
					}
					break;
			}
			
			
			var clipPoints1:Vector.<ClipVertex> = new <ClipVertex>[ new ClipVertex(),new ClipVertex() ];
			var clipPoints2:Vector.<ClipVertex> = new <ClipVertex>[ new ClipVertex(),new ClipVertex() ];
			
			var np:int;
			// Clip to box side 1
			np = ClipSegmentToLine(clipPoints1, incidentEdge, -sideNormal, negSide, negEdge);
			
			if (np < 2)
				return 0;
			
			// Clip to negative box side 1
			np = ClipSegmentToLine(clipPoints2, clipPoints1,  sideNormal, posSide, posEdge);

			if (np < 2)
				return 0;	
				
			var numContacts:int = 0;
			for (var i:int = 0; i < 2; ++i)
			{
				var separation:float = frontNormal.dot( clipPoints2[i].v) - front;
				
				if (separation <= 0)
				{
					//if (contacts == null)
					//{
						//contacts = new <Concat>[new Concat(),new Concat()];
					//}
									
					temp_contacts[numContacts].separation = separation;
					temp_contacts[numContacts].normal = normal;
					// slide contact point onto reference face (easy to cull)
					temp_contacts[numContacts].position = clipPoints2[i].v - separation * frontNormal;
					temp_contacts[numContacts].feature = clipPoints2[i].fp;
					if (axis == Axis.FACE_B_X || axis == Axis.FACE_B_Y)
					{
						//Flip(contacts[numContacts].feature);
						//void Flip(FeaturePair& fp)
						//{
							//Swap(fp.e.inEdge1, fp.e.inEdge2);
							//Swap(fp.e.outEdge1, fp.e.outEdge2);
						//}
						
						var f:FeaturePair = temp_contacts[numContacts].feature;
						var tmp:byte = f.e.inEdge1;
						f.e.inEdge1 = f.e.inEdge2;
						f.e.inEdge2 = tmp;
						
						tmp = f.e.outEdge1;
						f.e.outEdge1 = f.e.outEdge2;
						f.e.outEdge2 = tmp;
						
						temp_contacts[numContacts].feature = f;
						
					}
					++numContacts;
				}
			}

			
			
			
			return numContacts;
							
				
		}
		
		
		
		public function Update(newContacts:Vector.<Concat>, numNewContacts:int):void
		{
			var mergedContacts:Vector.<Concat> = new Vector.<Concat>() ;
			mergedContacts.length = 2;

			for (var i:int = 0; i < numNewContacts; ++i)
			{
				var cNew:Concat = newContacts[i];
				var k:int = -1;
				for (var j:int = 0; j < numContacts; ++j)
				{
					var cOld:Concat = contacts[j];
					if (cNew.feature.value == cOld.feature.value)
					{
						k = j;
						break;
					}
				}

				if (k > -1)
				{
					//var c:Concat = mergedContacts[i];
					var cOld:Concat = contacts[k];
					//*c = *cNew;
					
					
					
					
					if (World.warmStarting)
					{
						cNew.Pn = cOld.Pn;
						cNew.Pt = cOld.Pt;
						cNew.Pnb = cOld.Pnb;
					}
					else
					{
						cNew.Pn = 0.0f;
						cNew.Pt = 0.0f;
						cNew.Pnb = 0.0f;
					}
					
					mergedContacts[i] = cNew;
				}
				else
				{
					mergedContacts[i] = newContacts[i];
				}
			}

			for (var i:int = 0; i < numNewContacts; ++i)
				contacts[i] = mergedContacts[i];

			numContacts = numNewContacts;
		}
		
		
		public function PreStep(inv_dt:float):void
		{
			
			const k_allowedPenetration:float = 0.01f;
            var k_biasFactor:float = World.positionCorrection ? 0.2f : 0.0f;

			for (var i:int = 0; i < numContacts; ++i)
			{
				var c:Concat = contacts[i];

				var r1:Vector2 = c.position - body1.position;
				var r2:Vector2 = c.position - body2.position;

				// Precompute normal mass, tangent mass, and bias.
				var rn1:float = r1.dot( c.normal);
				var rn2:float = r2.dot( c.normal);
				var kNormal:float = body1.invMass + body2.invMass;
				kNormal += body1.invI * (r1.dot( r1) - rn1 * rn1) + body2.invI * (r2.dot( r2) - rn2 * rn2);
				c.massNormal = 1.0f / kNormal;
				
				var tangent:Vector2 = MathUtil.Cross_Vec2_F(c.normal, 1.0f);
				var rt1:float = r1.dot( tangent);
				var rt2:float = r2.dot( tangent);
				var kTangent:float = body1.invMass + body2.invMass;
				kTangent += body1.invI * (r1.dot( r1) - rt1 * rt1) + body2.invI * (r2.dot( r2) - rt2 * rt2);
				c.massTangent = 1.0f /  kTangent;
				
				c.bias = -k_biasFactor * inv_dt * Mathf.min(0.0f, c.separation + k_allowedPenetration);
				
				contacts[i] = c;
				
				if (World.accumulateImpulses)
				{
					// Apply normal + friction impulse
					var P:Vector2 = c.Pn * c.normal + c.Pt * tangent;
					
					body1.velocity -= body1.invMass * P;
					body1.angularVelocity -= body1.invI * r1.cross( P);

					body2.velocity += body2.invMass * P;
					body2.angularVelocity += body2.invI * r2.cross( P);
				}
			}
			
			
			
			
		}
		
		
		
		public function ApplyImpulse():void
		{
			var b1:Body = body1;
			var b2:Body = body2;

			for (var i:int = 0; i < numContacts; ++i)
			{
				var c:Concat = contacts[i];
				c.r1 = c.position - b1.position;
				c.r2 = c.position - b2.position;
				
				
				
				// Relative velocity at contact
				var dv:Vector2 = b2.velocity + MathUtil.Cross(b2.angularVelocity, c.r2) - b1.velocity - MathUtil.Cross(b1.angularVelocity, c.r1);

				// Compute normal impulse
				var vn:float = dv.dot( c.normal);

				var dPn:float = c.massNormal * (-vn + c.bias);
				
				if (World.accumulateImpulses)
				{
					// Clamp the accumulated impulse
					var Pn0:float = c.Pn;
					c.Pn = Mathf.max(Pn0 + dPn, 0.0f);
					dPn = c.Pn - Pn0;
				}
				else
				{
					dPn = Mathf.max(dPn, 0.0f);
				}

				// Apply contact impulse
				var Pn:Vector2 = dPn * c.normal;

				b1.velocity -= b1.invMass * Pn;
				b1.angularVelocity -= b1.invI * c.r1.cross( Pn);

				b2.velocity += b2.invMass * Pn;
				b2.angularVelocity += b2.invI * c.r2.cross( Pn);
				
				// Relative velocity at contact
				dv = b2.velocity + MathUtil.Cross(b2.angularVelocity, c.r2) - b1.velocity - MathUtil.Cross(b1.angularVelocity, c.r1);

				var tangent:Vector2 =  MathUtil.Cross_Vec2_F(c.normal, 1.0f);
				var vt:float = dv.dot(tangent);
				var dPt:float = c.massTangent * (-vt);

				if (World.accumulateImpulses)
				{
					// Compute friction impulse
					var maxPt:float = friction * c.Pn;

					// Clamp friction
					var oldTangentImpulse:float = c.Pt;
					c.Pt = Mathf.clamp(oldTangentImpulse + dPt, -maxPt, maxPt);
					dPt = c.Pt - oldTangentImpulse;
				}
				else
				{
					var maxPt:float = friction * dPn;
					dPt = Mathf.clamp(dPt, -maxPt, maxPt);
				}

				// Apply contact impulse
				var Pt:Vector2 = dPt * tangent;

				b1.velocity -= b1.invMass * Pt;
				b1.angularVelocity -= b1.invI * c.r1.cross( Pt);

				b2.velocity += b2.invMass * Pt;
				b2.angularVelocity += b2.invI * c.r2.cross( Pt);
				
				contacts[i] = c;
				
			}
		}
		
		
		
	}

}
import geom.Vector2;
import geom.Matrix2x2;

final class Axis
{
	public static const FACE_A_X:int = 0;
	public static const FACE_A_Y:int = 1;
	public static const FACE_B_X:int = 2;
	public static const FACE_B_Y:int = 3;
};

final class EdgeNumbers
{
	public static const NO_EDGE:int = 0;
	public static const EDGE1:int = 1;
	public static const EDGE2:int = 2;
	public static const EDGE3:int = 3;
	public static const EDGE4:int = 4;
};

[struct]
final class ClipVertex
{	
	public var v:Vector2= new Vector2();
	public var fp:FeaturePair = new FeaturePair();
};



function ComputeIncidentEdge( c:Vector.<ClipVertex>, h:Vector2, pos:Vector2,
								Rot:Matrix2x2, normal:Vector2):void
{
	var RotT:Matrix2x2 = Rot.Transpose();
	var n:Vector2 = -(RotT * normal);
	var nAbs:Vector2 = MathUtil.AbsVec2(n);

	if (nAbs.x > nAbs.y)
	{
		if (Mathf.sign(n.x) > 0.0f)
		{
			c[0].v = new Vector2(h.x, -h.y);
			c[0].fp.e.inEdge2 = EdgeNumbers.EDGE3;
			c[0].fp.e.outEdge2 = EdgeNumbers.EDGE4;

			c[1].v = new Vector2(h.x, h.y);
			c[1].fp.e.inEdge2 = EdgeNumbers.EDGE4;
			c[1].fp.e.outEdge2 = EdgeNumbers.EDGE1;
		}
		else
		{
			c[0].v = new Vector2(-h.x, h.y);
			c[0].fp.e.inEdge2 = EdgeNumbers.EDGE1;
			c[0].fp.e.outEdge2 = EdgeNumbers.EDGE2;

			c[1].v = new Vector2(-h.x, -h.y);
			c[1].fp.e.inEdge2 = EdgeNumbers.EDGE2;
			c[1].fp.e.outEdge2 = EdgeNumbers.EDGE3;
		}
	}
	else
	{
		if (Mathf.sign(n.y) > 0.0f)
		{
			c[0].v = new Vector2(h.x, h.y);
			c[0].fp.e.inEdge2 = EdgeNumbers.EDGE4;
			c[0].fp.e.outEdge2 = EdgeNumbers.EDGE1;

			c[1].v = new Vector2(-h.x, h.y);
			c[1].fp.e.inEdge2 = EdgeNumbers.EDGE1;
			c[1].fp.e.outEdge2 = EdgeNumbers.EDGE2;
		}
		else
		{
			c[0].v = new Vector2(-h.x, -h.y);
			c[0].fp.e.inEdge2 = EdgeNumbers.EDGE2;
			c[0].fp.e.outEdge2 = EdgeNumbers.EDGE3;

			c[1].v = new Vector2(h.x, -h.y);
			c[1].fp.e.inEdge2 = EdgeNumbers.EDGE3;
			c[1].fp.e.outEdge2 = EdgeNumbers.EDGE4;
		}
	}

	c[0].v = pos + Rot * c[0].v;
	c[1].v = pos + Rot * c[1].v;
	
}


function ClipSegmentToLine(vOut:Vector.<ClipVertex>, vIn:Vector.<ClipVertex>,
					  normal:Vector2, offset:float, clipEdge:byte):int
{
	// Start with no output points
	var numOut:int = 0;

	// Calculate the distance of end points to the line
	var distance0:float = normal.dot( vIn[0].v) - offset;
	var distance1:float = normal.dot( vIn[1].v) - offset;

	// If the points are behind the plane
	if (distance0 <= 0.0f) vOut[numOut++] = vIn[0];
	if (distance1 <= 0.0f) vOut[numOut++] = vIn[1];

	// If the points are on different sides of the plane
	if (distance0 * distance1 < 0.0f)
	{
		// Find intersection point of edge and plane
		var interp:float = distance0 / (distance0 - distance1);
		vOut[numOut].v = vIn[0].v + interp * (vIn[1].v - vIn[0].v);
		if (distance0 > 0.0f)
		{
			vOut[numOut].fp = vIn[0].fp;
			vOut[numOut].fp.e.inEdge1 = clipEdge;
			vOut[numOut].fp.e.inEdge2 = EdgeNumbers.NO_EDGE;
		}
		else
		{
			vOut[numOut].fp = vIn[1].fp;
			vOut[numOut].fp.e.outEdge1 = clipEdge;
			vOut[numOut].fp.e.outEdge2 = EdgeNumbers.NO_EDGE;
		}
		++numOut;
	}

	return numOut;
}