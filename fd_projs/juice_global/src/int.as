package 
{
  /**
	 * 通过 int 类可使用表示为 32 位带符号整数的数据类型。int 类表示的值的范围是：-2,147,483,648 (-2^31) 到 2,147,483,647 (2^31-1)。
	 * 
	 * <span>
	 * <p>
	 * int 类的常数属性 MAX_VALUE 和 MIN_VALUE 为静态的，这意味着无需对象就可使用这些属性，因而不需要使用构造函数。而方法却不是静态的，这意味着需要对象才能使用它们。可以通过使用 int 类构造函数，或者声明一个 int 类型的变量并赋予该变量一个文字值来创建 int 对象。
	 * </p>
	 * <p>
	 * int 数据类型用于循环计数器和不需要浮点数的其他情况，且该数据类型类似于 Java 和 C++ 中的 int 数据类型。int 类型变量的默认值为 0
	 * </p>
	 * <p>
	 * 如果您正在处理超过 int.MAX_VALUE 的数值，可考虑使用 Number。
	 * </p>
	 * </span>
	 * @example 以下示例调用 int 类的 toString() 方法，以返回字符串 1234：
	 * <listing>
	 * var myint:int = 1234;
	 * t.toString();
	 * </listing>
	 * @example 以下示例将 MIN_VALUE 属性的值分配给一个无需使用构造函数进行声明的变量：
	 * <listing>
	 * var smallest:int = int.MIN_VALUE;
	 * </listing>
	 */
   public final class int
   {
	   /**
		 * 可表示的最大 32 位带符号整数为 2,147,483,647。
		 */
      public static const MIN_VALUE:int = -2147483648;
      
	  /**
		 * 可表示的最小 32 位带符号整数为 -2,147,483,648。
		 */
      public static const MAX_VALUE:int = 2147483647;
      
	  /**
	   * @private
	   */
      public static const length:int = 1;
      
	  /**
		 * 构造函数；创建新的 int 对象。使用 int.toString() 和 int.valueOf() 时，必须使用 int 构造函数。使用 int 对象的属性时，不要使用构造函数。new int 构造函数主要用作占位符。int 对象与 int() 函数不同，后者将参数转换为原始值。
		 * @param	num	要创建的 int 对象的数值，或者要转换为数字的值。如果未提供 value，则默认值为 0。
		 */
      public native function int(value:* = 0);
      
      
		/**
		 * 返回 int 对象的字符串表示形式。
		 * @param	radix	指定要用于数字到字符串的转换的基数（从 2 到 36）。如果未指定 radix 参数，则默认值为 10。
		 * @return	字符串。
		 */
      public native function toString(radix:* = 10) : String;
     
      
	  /**
		 * 返回指定 int 对象的原始值。
		 * @return	int 值。
		 */
      public native function valueOf() : int;
      
	  
      /**
		 * 返回数字的字符串表示形式（采用指数表示法）。字符串在小数点前面包含一位，在小数点后面最多包含 20 位（在 fractionDigits 参数中指定）。
		 * @param	fractionDigits	介于 0 和 20（含）之间的整数，表示所需的小数位数。
		 * @langversion	3.0
		 * @throws ArgumentError 如果 fractionDigits 参数不在 0 到 20 的范围内，则会引发异常。
		 */
      public native function toExponential(p:* = 0) : String;
      
	  
      /**
		 * 返回数字的字符串表示形式（采用指数表示法或定点表示法）。字符串将包含 precision 参数中指定的位数。
		 * @param	precision	介于 1 和 21（含）之间的整数，表示结果字符串中所需的位数。
		 * @langversion	3.0
		 * @throws ArgumentError 如果 precision 参数不在 1 到 21 的范围内，则会引发异常。
		 */
      public native function toPrecision(p:* = 0) : String;
      
	  
      /**
		 * 返回数字的字符串表示形式（采用定点表示法）。定点表示法是指字符串的小数点后面包含特定的位数（在 fractionDigits 参数中指定）。fractionDigits 参数的有效范围为 0 到 20。如果指定的值在此范围外，则会引发异常。
		 * @param	fractionDigits	介于 0 和 20（含）之间的整数，表示所需的小数位数。
		 * @langversion	3.0
		 * @throws ArgumentError 如果 fractionDigits 参数不在 0 到 20 的范围内，则会引发异常。
		 */
      public native function toFixed(p:* = 0) : String;
      
   }

}