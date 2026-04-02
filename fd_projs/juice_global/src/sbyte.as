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
		 * @return	此 sbyte 对象的原始 sbyte 类型值。
		 * @langversion	3.0
		 */
		public native function valueOf () : sbyte;
		
				
		/**
		 * 返回 sbyte 对象的字符串表示形式。
		 * @param	radix	指定要用于数字到字符串的转换的基数（从 2 到 36）。如果未指定 radix 参数，则默认值为 10。
		 * @return	sbyte 对象的字符串表达式。
		 * @langversion	3.0
		 */
		public native function toString (radix:int = 10) : String;
		
		/**
		 * 创建新的 sbyte 对象。可以创建一个 sbyte 类型的变量并赋予其文本值。
		 * @param	value	要创建的 sbyte 对象的数值，或者要转换为数字的值。如果未提供 value，则默认值为 0。
		 * @langversion	3.0
		 */
		public native function sbyte (value:*= 0);
		
		/**
		 * 返回数字的字符串表示形式（采用指数表示法）。字符串在小数点前面包含一位，在小数点后面最多包含 20 位（在 fractionDigits 参数中指定）。
		 * @param	fractionDigits	介于 0 和 20（含）之间的整数，表示所需的小数位数。
		 * @langversion	3.0
		 * @throws	RangeError 如果 fractionDigits 参数不在 0 到 20 的范围内，则会引发异常。
		 */
		public native function toExponential(p:int = 0) : String;
		  
		/**
		 * 返回数字的字符串表示形式（采用定点表示法）。定点表示法是指字符串的小数点后面包含特定的位数（在 fractionDigits 参数中指定）。fractionDigits 参数的有效范围为 0 到 20。如果指定的值在此范围外，则会引发异常。
		 * @param	fractionDigits	介于 0 和 20（含）之间的整数，表示所需的小数位数。
		 * @langversion	3.0
		 * @throws	RangeError 如果 fractionDigits 参数不在 0 到 20 的范围内，则会引发异常。
		 */
		public native function toFixed(p:int = 0) : String;
		
		/**
		 * 返回数字的字符串表示形式（采用指数表示法或定点表示法）。字符串将包含 precision 参数中指定的位数。
		 * @param	precision	介于 1 和 21（含）之间的整数，表示结果字符串中所需的位数。
		 * @langversion	3.0
		 * @throws	RangeError 如果 precision 参数不在 1 到 21 的范围内，则会引发异常。
		 */
		public native function toPrecision(p:int = 0) : String;
		
	}

}