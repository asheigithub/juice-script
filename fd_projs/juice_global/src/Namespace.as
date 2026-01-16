package  
{
	 
	public final class Namespace
	{
		
		/**
		 * 命名空间的前缀。
		 * @langversion	3.0
		 * @playerversion	Flash 9
		 */
		public native function get prefix () : String;

		/**
		 * 命名空间的统一资源标识符 (URI)。
		 * @langversion	3.0
		 * @playerversion	Flash 9
		 */
		public native function get uri () : String;

		public native function Namespace (prefix:*=null, uri:*=null);

		/**
		 * 等效于 Namespace.uri 属性。
		 * @return	命名空间的统一资源标识符 (URI)（采用字符串形式）。
		 * @langversion	3.0
		 * @playerversion	Flash 9
		 */
		AS3 native function toString () : String;

		/**
		 * 返回指定对象的 URI 值。
		 * @return	命名空间的统一资源标识符 (URI)（采用字符串形式）。
		 * @langversion	3.0
		 * @playerversion	Flash 9
		 */
		AS3 native function valueOf () : String;
	}

}