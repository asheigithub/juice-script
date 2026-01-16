package
{
	/**
	 *  EvalError 类表示一种错误，如果用户代码调用 <codeph class="+ topic/ph pr-d/codeph ">eval()</codeph> 函数或试图将 <codeph class="+ topic/ph pr-d/codeph ">new</codeph> 运算符用于 Function 对象，则会出现该错误。不支持调用 <codeph class="+ topic/ph pr-d/codeph ">eval()</codeph> 和使用 Function 对象调用 <codeph class="+ topic/ph pr-d/codeph ">new</codeph>。
	 */
	public dynamic class EvalError extends Error
	{
		public native function EvalError (message:String="", id:int=0);
	}
}