package 
{
	
	/**
	 * 迭代器接口。
	 * 如果某个类 指定了 [iterator] 方法，
	 * 则运行时在执行 foreach ，for in 时，尝试对对象调用这个方法，
	 * 如果成功返回一个IIterator接口，则开始迭代工作。
	 * @author 
	 */
	public interface IIterator 
	{
		function next(obj:*, r:IteratorResult ):void;
		function close(obj:*):void;
		//function raise(e:*):void;
	}
	
}

class object_iterator implements IIterator
{
	public native function next(obj:*, r:IteratorResult ):void;
	public native function close(obj:*):void;
	
	
	
	public var index:int;
	public var count:int;
	
}

[wapper]
class iter_context
{
	
}
