package flash.utils
{
	/**
	 * 返回 name 参数指定的类的类对象引用。
	 * @param	param1 类的名称。
	 * @return  参数指定的类的类对象引用。
	 * 
	 * @example 下面代码演示如何使用字符串来查找并创建类型
<listing>
import flash.utils.getDefinitionByName;
var m = getDefinitionByName("juice.geom.Vector2");
var b = new m(1, 1);
trace( b.length );  // 1.414
</listing>
	 */
   public function getDefinitionByName(param1:String) : *{ return null; }
}