package flash.errors 
{
	/**
	 * 当方法未实现或者实现中未涉及当前用法时，将引发 IllegalOperationError 异常。
	 * @author 
	 */
	public dynamic final class IllegalOperationError extends Error 
	{
		/**
		 * 创建新的 IllegalOperationError 对象。
		 * @param	message 与此对象关联的字符串.
		 * @param	id
		 */
		public native function IllegalOperationError(message:String="", id:*=0) ;
		
	}

}