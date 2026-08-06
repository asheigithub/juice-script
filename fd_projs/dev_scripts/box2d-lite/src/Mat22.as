package 
{
	import geom.Vector2;
	[struct]
	public final class Mat22 
	{
		public var col1:Vector2;
		public var col2:Vector2;
		
		[auto]
		public function Mat22(col1:Vector2 = null, col2:Vector2 = null);
		//{
			//this.col1 = col1;
			//this.col2 = col2;
		//}
		
		public static function FromAngle(angle:float):Mat22
		{
			var c:float = Mathf.cos(angle); var  s:float = Mathf.sin(angle);
						
			//col1.x = c; col2.x = -s;
			//col1.y = s; col2.y = c;
			
			return new Mat22( new Vector2(c,s),new Vector2(-s,c) );
			
		}
		
		
		
		public function Transpose():Mat22
		{
			return new Mat22( new Vector2(col1.x,col2.x),new Vector2(col1.y,col2.y) );			
		}
		
		
		public function Invert():Mat22
		{
			var a:float = col1.x;
			var b:float = col2.x;
			var c:float = col1.y;
			var d:float = col2.y;
			
			var B:Mat22 = new Mat22();
			var det:float = a * d - b * c;
			
			if (det == 0.0f)
				throw new Error("det != 0.0f");
			
			det = 1.0f / det;
			
			B.col1.x =  det * d;	B.col2.x = -det * b;
			B.col1.y = -det * c;	B.col2.y =  det * a;
			return B;
			
			
		}
		
		
		
		[operator("*")]
		private static function Mat22mulVec2( A:Mat22, v:Vector2 ):Vector2
		{
			return new Vector2(A.col1.x * v.x + A.col2.x * v.y, A.col1.y * v.x + A.col2.y * v.y);
		}

		[operator("+")]
		private static function Mat22addMat22( A:Mat22, B:Mat22 ):Mat22
		{
			return new Mat22(A.col1 + B.col1, A.col2 + B.col2);
		}
		
		[operator("*")]
		private static function Mat22mulMat22( A:Mat22, B:Mat22 ):Mat22
		{
			//return new Mat22( new Vector2( A.col1.x * B.col1.x + A.col2.x * B.col1.y,A.col1.y * B.col1.x + A.col2.y * B.col1.y ),new Vector2(A.col1.x * B.col2.x + A.col2.x * B.col2.y ,A.col1.y * B.col2.x + A.col2.y * B.col2.y  ) );
			return new Mat22(A * B.col1, A * B.col2);
		}
		
		
		
		
		
		public function toString():String
		{
			return "[" + col1.toString() + "," + col2.toString() + "]";
		}
		
		
	}

}