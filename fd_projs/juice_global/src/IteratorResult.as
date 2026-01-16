package  
{
	/**
	 * 迭代器返回对象。
	 * done表示迭代是否结束
	 * key和value分别保存本次迭代返回的key和value.因为还有在遍历_proto_时，需要屏蔽已经遍历过的key的问题,所以key一定要返回
	 * @author 
	 */
	[no_constructor];
	public final class IteratorResult 
	{
		public var done:Boolean;
		public var key:*;
		public var value:*;
	}

}