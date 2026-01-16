package
{
	/**
	 * 如果试图对密封（非动态）对象引用未定义的属性，则会引发 ReferenceError 异常。引用未定义变量将导致 ReferenceError 异常，通知您潜在的错误并帮助您排除应用程序代码故障。
	 */
	public dynamic class ReferenceError extends Error
	{
		
		/**
		 * 创建一个新的 ReferenceError 对象。
		 * @param	message	包含与 ReferenceError 对象关联的消息。
		 */
		public native function ReferenceError (message:String="", id:int=0);
	}
}
