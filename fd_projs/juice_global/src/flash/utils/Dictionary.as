package flash.utils
{
	import IIterator;
	/**
	 * Dictionary 类用于创建属性的动态集合，该集合使用 strict equality (===) 运算符进行键比较。将对象用作键时，会使用对象的标识来查找对象，而不是使用在对象上调用 toString() 所返回的值。
	 * @example 以下语句显示了一个 Dictionary 对象和一个 key 对象之间的关系：
	 * <listing>
 var dict = new Dictionary();
 var obj = new Object();
 var key:Object = new Object();
 key.toString = function() { return "key" }
 
 dict[key] = "Letters";
 obj["key"] = "Letters";
 
 dict[key] == "Letters"; // true
 obj["key"] == "Letters"; // true
 obj[key] == "Letters"; // true because key == "key" is true b/c key.toString == "key"
 dict["key"] == "Letters"; // false because "key" === key is false
 delete dict[key]; //removes the key
	 </listing>
	 */
	[wapper]
	public dynamic class Dictionary extends Object
	{
		/**
		 * 创建新的 Dictionary 对象。
		 * @param	weakKeys 当前无用
		 */
		public native function Dictionary (weakKeys:Boolean=false);
		
		[indexer_set]
		private native function indexer_set(key:*,value:*):void;
		
		[indexer_get]
		private native function indexer_get(key:*):*;
		
		[indexer_delete]
		private native function indexer_delete(key:*):Boolean;
		
		
		[iterator]
		private native function getIterator():IIterator;
		
	}
}

final class dict_iter implements IIterator
{
	private var index:int;
	/* INTERFACE IIterator */
	native public function next(obj:*,r:IteratorResult):void ;
	
	native  public function close(obj:*):void ;

	//native public function raise(obj:*, e:*):void ;
	
}
