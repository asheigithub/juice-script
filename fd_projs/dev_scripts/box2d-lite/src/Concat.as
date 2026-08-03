package 
{
	import geom.Vector2;
	/**
	 * ...
	 * @author 
	 */
	//[struct]
	public final class Concat 
	{
		var position:Vector2;
		var normal:Vector2;
		var r1:Vector2, r2:Vector2;
		var separation:float;
		var Pn:float;	// accumulated normal impulse
		var Pt:float;	// accumulated tangent impulse
		var Pnb:float;	// accumulated normal impulse for position bias
		var massNormal:float, massTangent:float;
		var bias:float;
	
		var feature:FeaturePair = new FeaturePair();
		
		public function Concat() 
		{
			
		}
		
	}

}