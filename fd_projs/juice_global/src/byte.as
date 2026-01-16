package 
{
	/**
	 * juice扩展的类型，八位无符号整数
	 * @author 
	 */
	public final class byte 
	{
		/**
		 * 可表示的最大 8 位无符号整数为 255。
		 * @langversion	3.0
		 */
		public static const MAX_VALUE : byte =  255;
		  
		/**
		 * 可表示的最小无符号整数为 0。
		 * @langversion	3.0
		 */
		public static const MIN_VALUE : byte =  0;
		
		
		
		/**
		 * 返回指定 byte 对象的原始 byte 类型值。
		 * @return	此 byte 对象的原始 byte 类型值。
		 * @langversion	3.0
		 */
		AS3 native function valueOf () : byte;
	}

}