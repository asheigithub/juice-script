package
{
	/**
	 * 表示发生了类型错误
	 */
	public dynamic class TypeError extends Error
	{
		/**
		 * 创建新的 TypeError 对象。
		 * @param	message  与该错误关联的字符串。
		 * @param	id       与该错误关联的数字
		 */
		public native function TypeError (message:String="", id:int=0);
		
	}
}
