package
{
	/**
	 * 表示参数错误的类
	 */
	public dynamic class ArgumentError extends Error
	{
		/**
		 * 创建错误的实例
		 * @param	message  与该错误关联的字符串
		 * @param	id       与该错误关联的数字
		 */
		public native function ArgumentError (message:String="", id:int=0);
		
	}
}