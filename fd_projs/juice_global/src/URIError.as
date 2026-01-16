package
{
	/**
	 *  如果采用与某个全局 URI 处理函数的定义相矛盾的方式使用该函数，则会引发 URIError 异常。如果为需要有效 URI（如 Socket.connect() 方法）的函数指定无效 URI，则会引发该异常。
	 */
	public dynamic class URIError extends Error
	{
		public native function URIError (message:String="");
	}
}

var urierror:String = "URIError";
