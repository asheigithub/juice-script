package 
{
	import geom.Vector2;
	import geom.Matrix2x2;
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
		
		//public native static function Cross(s:float, a:Vector2):Vector2
		
		public static function Cross_Vec2_F(a:Vector2, s:float):Vector2
		{
			return new Vector2(s * a.y, -s * a.x);
		}
		
		//public native static function Cross_Vec2_F(a:Vector2, s:float):Vector2;
		
		
		public static function AbsVec2(a:Vector2):Vector2
		{
			return new Vector2( Mathf.abs( a.x), Mathf.abs( a.y) );
		}
		
		//public native static function AbsVec2(a:Vector2):Vector2;
		
		
		
		public static function AbsM22(A:Matrix2x2):Matrix2x2
		{
			
			return new Matrix2x2( AbsVec2( A.col1), AbsVec2( A.col2) );
			
		}
		
		//public native static function AbsM22(A:Matrix2x2):Matrix2x2;
		
	}

}