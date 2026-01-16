package 
{
	/**
	 * juice扩展的类型，十六位有符号整数
	 * @author 
	 */
	public final class short 
	{
		
		/**
		 * 可表示的最大 16 位无符号整数为 32767。
		 * @langversion	3.0
		 */
		public static const MAX_VALUE : short =  32767;
		  
		/**
		 * 可表示的最小 16 位无符号整数为 -32768。
		 * @langversion	3.0
		 */
		public static const MIN_VALUE : short =  -32768;
		
		
		/**
		 * 返回指定 short 对象的原始 short 类型值。
		 * @return	此 short 对象的原始 short 类型值。
		 * @langversion	3.0
		 */
		AS3 native function valueOf () : short;
		
	}

}