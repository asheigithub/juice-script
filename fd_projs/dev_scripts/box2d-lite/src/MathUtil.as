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
	}

}