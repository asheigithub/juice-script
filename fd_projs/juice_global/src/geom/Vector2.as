package geom 
{
	/**
	 * 二维向量
	 * @author 
	 */
	[struct]
	public final class Vector2 
	{
		public var x:float;
		public var y:float;
		
		public native function Vector2(x:float = 0,y:float = 0);
		
		public native function toString():String;
		
		
		[operator("+")]
		private static native function Vec2addVec2( lhs:Vector2, rhs:Vector2 ):Vector2;
		
		[operator("-")]
		private static native function Vec2subVec2( lhs:Vector2, rhs:Vector2 ):Vector2;
		
		
		
	}

}

