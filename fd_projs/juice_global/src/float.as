package 
{
	/**
	 * juice的扩展类型，32位浮点数
	 * @author 
	 */
	public final class float 
	{
		public static const Epsilon:float = 1.4e-45;
		
		/**
		 * 返回指定 float 对象的原始 float 类型值。
		 * @return 此 float 对象的原始 float 类型值。
		 */
		AS3 native function valueOf () : float;
		
	}

}