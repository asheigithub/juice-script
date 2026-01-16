package flash.errors 
{
	/**
	 * 如果尝试读取的内容超出可用数据的末尾，则会引发 EOFError 异常。例如，当调用 IDataInput 接口中的一个读取方法，而数据不足以满足读取请求时，将引发 EOFError。
	 * @author 
	 */
	public final class EOFError extends Error 
	{
		/**
		 * 创建新的 EOFError 对象。
		 * @param	message 与此错误对象相关联的字符串。
		 * @param	id 与此错误对象相关的数字
		 */
		public native function EOFError(message:String="", id:*=0) ;
		
		
	}

}