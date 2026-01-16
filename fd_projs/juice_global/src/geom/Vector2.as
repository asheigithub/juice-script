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
		
	}

}