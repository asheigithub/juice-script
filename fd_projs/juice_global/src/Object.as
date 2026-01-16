package
{
	/**
	 * Object 类位于 ActionScript 类层次结构的根处。Object 由构造函数使用 new 运算符语法创建，并且可以具有动态赋予其的属性。也可通过赋予对象文字值来创建 Object，如下所示：
	 * <listing>var obj:Object = {a:"foo", b:"bar"};</listing>
	 * 不声明显式基类的所有类均可扩展内置 Object 类。
	 * <p>可以使用 Object 类创建关联数组。关键是，关联数组是 Object 类的实例，而每个键/值对由属性及属性的值表示。要将关联数组声明为 Object 数据类型还有另一个原因：您可以使用对象文本来填充关联数组（但只能在您声明它时）。下面的示例使用对象文本创建一个关联数组，使用 dot 运算符和 array access 运算符访问项，然后通过创建一个新属性来添加新的键/值对：</p>
	 <listing>
 var myAssocArray:Object = {fname:"John", lname:"Public"};
 trace(myAssocArray.fname);     // John
 trace(myAssocArray["lname"]);  // Public
 myAssocArray.initial = "Q";
 trace(myAssocArray.initial);   // Q
	 </listing>
	 */
	public dynamic class Object
	{
		
	}
}
