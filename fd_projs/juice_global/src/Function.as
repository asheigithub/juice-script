package
{
	/**
	 * 函数是可在 ActionScript 中调用的基本代码单位。ActionScript 中用户定义的函数和内置函数都由 Function 对象来表示，该对象是 Function 类的实例。
	 * <p>类的方法与 Function 对象略有不同。与普通函数对象不同，方法和与其关联的类对象紧密关联。因此，方法或属性具有在同一类的所有实例中共享的定义。可以从实例提取方法并将其处理为“绑定”方法（保留与原始实例的链接）。对于绑定方法，this 关键字指向实现该方法的原始对象。对于函数，this 在调用函数时指向关联对象。</p>
	 */
	public final dynamic class Function
	{

		/**
		 * @private
		 */
		public native function Function ();
		
		/**
		 * @private
		 */
		public native function get prototype () : * ;

		/**
		 * @private
		 */
		public native function set prototype (p:*) : void;

		
		/**
		 * 指定要在 ActionScript 调用的任何函数内使用的 thisObject 的值。此方法还指定要传递给任何被调用函数的参数。由于 apply() 是 Function 类的方法，所以它也是 ActionScript 中每个 Function 对象的方法。
		 * <p>该方法将参数指定为一个 Array 对象。如果在脚本实际执行前，无法知道要传递的参数的数量，那么这种方法通常很有用。</p>
		 * @param	thisArg 要应用该函数的对象。
		 * @param	argArray  其元素作为参数传递给函数的数组。
		 * @return 调用函数指定的任何值。
		 * @see #call()
		 */
		AS3 native function apply (thisArg:*= null, argArray:Array = null) : * ;
		
		/**
		 * 调用 Function 对象表示的函数。ActionScript 中的每个函数都由一个 Function 对象来表示，所以所有的函数都支持此方法。
		 * <p>几乎在所有的情形下，都可以使用函数调用运算符 (()) 来代替此方法。如果需要明确控制函数调用中的 thisObject 参数，则此方法很有用。</p>
		 * @param	thisArg  指定函数体内 thisObject 值的对象。
		 * @param	...rest   要传递给该函数的参数。可以指定 0 个或多个参数。
		 * @return
		 * @see #apply()
		 */
		AS3 native function call (thisArg:*= null, ...rest) : * ;
		
		/**
		 * @private
		 */
		public native function get length () : int ;

	}
}
