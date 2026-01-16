package
{
	/**
	 * Boolean 对象是一种数据类型，它可以使用 true 或 false（用于进行逻辑运算）两个值中一个值。使用 Boolean 类可检索 Boolean 对象的基元数据类型或字符串表示形式。要创建 Boolean 对象，可以使用构造函数、全局函数，或赋予文字值。
	 */
	public final class Boolean
	{
		
		/**
		 * 创建一个具有指定值的 Boolean 对象。
		 * @param	value 任何表达式。
		 */
		public native function Boolean (value:*= false);

		/**
		 * 返回 Boolean 对象的字符串表示形式（"true" 或 "false"）。
		 * @return
		 */
		public native function toString () : String;
		
		/**
		 * 如果指定的 Boolean 对象的值为 true，则返回 true；否则返回 false。
		 * @return
		 */
		public native function valueOf () : Boolean;
	}
}
