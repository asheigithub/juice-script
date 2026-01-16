package
{
	/**
	 * Error 类包含有关脚本中出现的错误的信息。可以使用 Error 构造函数来创建 Error 对象。通常，从 try 代码块内部引发一个新 Error 对象，该对象随后被 catch 代码块捕获。
	 * <p>您也可以创建 Error 类的子类，然后引发该子类的实例。</p>
	 */
	public dynamic class Error
	{
		/**
		 * 包含与 Error 对象关联的消息。
		 */
		public var message : String;

		/**
		 * 包含 Error 对象的名称。
		 */
		public var name : String;

		
		private var id: int;
		
		public native function get errorID () : int;
		

		/**
		 * 创建新的 Error 对象。
		 * @param	message 与 Error 对象关联的字符串；此参数为可选。
		 * @param	id 与特定错误消息关联的引用数字。
		 */
		public native function Error (message:String = "", id:int = 0);
		

		/**
		 * 在构建错误时，以字符串形式返回该错误的调用堆栈。如以下示例所示，返回值的第一行是异常对象的字符串表示形式，后跟堆栈跟踪元素：
		 * <listing>
TypeError: Error #1009: Cannot access a property or method of a null object reference
         at com.xyz::OrderEntry/retrieveData()[/src/com/xyz/OrderEntry.as:995]
         at com.xyz::OrderEntry/init()[/src/com/xyz/OrderEntry.as:200]
         at com.xyz::OrderEntry()[/src/com/xyz/OrderEntry.as:148]
		 * </listing>
		 * @return
		 */
		public native function getStackTrace () : String;
		
	}
}
