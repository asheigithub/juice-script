package 
{
	/**
	 * juice扩展的类型，八位有符号整数
	 * @author 
	 */
	public final class sbyte 
	{
		
		/**
		 * 可表示的最大 8 位有符号整数为 127。
		 * @langversion	3.0
		 */
		public static const MAX_VALUE : sbyte =  127;
		  
		/**
		 * 可表示的最小 8 位有符号整数为 -128。
		 * @langversion	3.0
		 */
		public static const MIN_VALUE : sbyte =  -128;
		
		
		/**
		 * 返回指定 sbyte 对象的原始 sbyte 类型值。
		 * @return	此 byte 对象的原始 byte 类型值。
		 * @langversion	3.0
		 */
		AS3 native function valueOf () : sbyte;
		
	}

}