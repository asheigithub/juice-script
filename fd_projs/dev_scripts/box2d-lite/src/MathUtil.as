package 
{
	import geom.Vector2;
	/**
	 * ...
	 * @author 
	 */
	public final class MathUtil 
	{
		public static function Cross(s:float, a:Vector2):Vector2
		{
			return new Vector2(-s * a.y, s * a.x);
		}
		
		
		
		public static function Cross_Vec2_F(a:Vector2, s:float):Vector2
		{
			return new Vector2(s * a.y, -s * a.x);
		}
		
		
		
		
		public static function AbsVec2(a:Vector2):Vector2
		{
			return new Vector2( Mathf.abs( a.x), Mathf.abs( a.y) );
		}
		
		public static function AbsM22(A:Mat22):Mat22
		{
			
			return new Mat22( AbsVec2( A.col1), AbsVec2( A.col2) );
			
		}
		
		
	}

}