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
		
		public native function dot(v:Vector2):float;
		
		public native function cross(v:Vector2):float;
		
		public native function toString():String;
		
		
		[operator("+")]
		private static native function Vec2addVec2( lhs:Vector2, rhs:Vector2 ):Vector2;
		
		[operator("-")]
		private static native function Vec2subVec2( lhs:Vector2, rhs:Vector2 ):Vector2;
		
		[operator("*")]
		private static native function Vec2mulFloat( lhs:Vector2, rhs:float ):Vector2;
		
		[operator("*")]
		private static native function Vec2mulNumber( lhs:Vector2, rhs:Number ):Vector2;
		
		[operator("/")]
		private static native function Vec2divFloat( lhs:Vector2, rhs:float ):Vector2;
		
		[operator("/")]
		private static native function Vec2divNumber( lhs:Vector2, rhs:Number ):Vector2;
		
		[operator("*")]
		private static native function FloatmulVec2( lhs:float, rhs:Vector2 ):Vector2;
		
		[operator("*")]
		private static native function NumbermulVec2( lhs:Number, rhs:Vector2 ):Vector2;
		
		[operator("-")]
		private static native function Vec2Neg(v:Vector2):Vector2;
		
		[operator("+")]
		private static native function Vec2Positive(v:Vector2):Vector2;
		
	}

}

