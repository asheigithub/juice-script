package __AS3__
{
	[no_constructor];
	public final class utils
	{
		/**
		 * 返回 name 参数指定的类的类对象引用。
		 * @param	name	类的名称。
		 * @return	返回 name 参数指定的类的类对象引用。
		 * @throws	ReferenceError 不存在具有指定名称的公共定义。
		 */
		native public static function getDefinitionByName (name:String) : Object;
		
		/**
		 * 返回对象的完全限定类名。
		 * @param	value	需要完全限定类名称的对象。可以将任何 ActionScript 值传递给此方法，包括所有可用的 ActionScript 类型、对象实例、原始类型（如 uint）和类对象。
		 * @return	包含完全限定类名称的字符串。
		 */
		native public static function getQualifiedClassName (value:*) : String;
		
		/**
			 * 返回 value 参数指定的对象的基类的完全限定类名。此函数检索基类名称的速度比 describeType() 快，但提供的信息不如 describeType() 全面。
			 * 使用此函数检索了类的名称后，可以用 getDefinitionByName() 函数将类名称转换为类引用。注意：此函数将本身局限于实例层次结构，而 describeType() 函数则使用类对象层次结构（如果 value 参数是数据类型）。如果在数据类型上调用 describeType()，将会基于类对象层次结构（其中所有类对象均继承自 Class）返回超类。但是，getQualifiedSuperclassName() 函数会忽略类对象层次结构，并基于较普通的实例层次结构返回超类。例如，调用 getQualifiedSuperclassName(String) 将会返回 Object，尽管从技术角度来说 String 类对象继承自 Class。换言之，不管使用的是类型的实例还是类型本身，结果都是相同的。
			 * @param	value	任何值。
			 * @return	完全限定的基类名称，或 null（如果不存在基类名称）。
			 */
		native public static function getQualifiedSuperclassName (value:*) : String;
		
		
		
		
	}
}