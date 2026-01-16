package
{
	/**
	 * 为程序中的每个类定义创建一个 Class 对象。每个 Class 对象都是 Class 类的一个实例。Class 对象包含该类的静态属性和方法。在使用 new 运算符进行调用时，Class 对象会创建该类的实例。
	 * <p>这是一种反射技术，用来区分类型，或者延迟决定实例化哪种类型</p>
	 * @example 下例演示如何通过下列步骤使用 Class 对象推迟有关实例化哪种类的决定，直到运行时为止：
	 * <p>1.声明两个类为 ClassA 和 ClassB。</p>
	 * <p>2.声明一个 Class 类型变量，名为 classToConstruct,根据此变量动态决定实例化类型</p>
	 * <listing>
class ClassA {
}
    
class ClassB {
}

var classToConstruct:Class;            
var classInstance:Object;

classToConstruct = ClassA;
classInstance = new classToConstruct();
trace(classInstance);    // [object ClassA]

classToConstruct = ClassB;
classInstance = new classToConstruct();
trace(classInstance);    // [object ClassB]

	 * </listing>
	 */
	public dynamic class Class
	{
		///**
		 //* @private 
		 //*/
		//protected var _protoObj:*={};
		//

		/**
		 * @private
		 */
		AS3 native function get prototype():*;
		//{
			//return _protoObj;
		//}

		/**
		 * @private
		 */
		//public function Class (){}
	}
}