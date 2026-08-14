package geom 
{
	/**
	 * 2x2矩阵 
	 */
	[struct]
	public final class Matrix2x2 
	{
		public var col1:Vector2;
		public var col2:Vector2;
		
		[auto]
		public function Matrix2x2(col1:Vector2 = null, col2:Vector2 = null);
		
		public native static function FromAngle(angle:float):Matrix2x2;
		
		public native function Transpose():Matrix2x2;
		
		public native function Invert():Matrix2x2;
		
		public native function toString():String;
		
		
		[operator("*")]
		private native static function Mat22mulVec2( A:Matrix2x2, v:Vector2 ):Vector2;
		
		[operator("+")]
		private native static function Mat22addMat22( A:Matrix2x2, B:Matrix2x2 ):Matrix2x2;
		
		[operator("*")]
		private native static function Mat22mulMat22( A:Matrix2x2, B:Matrix2x2 ):Matrix2x2;
		
		
		
		
		
		
	}

}