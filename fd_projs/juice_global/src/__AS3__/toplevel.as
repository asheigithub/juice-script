package __AS3__
{
	[no_constructor];
	public final class toplevel
	{
		/**
		 * 调试时显示表达式或写入日志文件。单个跟踪语句可支持多个参数。如果跟踪语句中的任何参数包含 String 之外的数据类型，则跟踪函数将调用与该数据类型关联的 toString() 方法。例如，如果该参数是一个布尔值，则跟踪函数将调用 Boolean.toString() 并显示返回值。
		 * @param	arguments	要计算的一个或多个（逗号分隔）表达式。对于多个表达式，输出中每个表达式之间都将插入一个空格。
		 */
		native public static function trace(...rest) : void;
		
		
		native public static function isNaN(n:Number=undefined) : Boolean;
		
		native public static function isFinite(n:Number=undefined) : Boolean;
		
		native public static function parseFloat (str:String="NaN") : Number;
		
		native public static function parseInt (s:String="NaN", radix:int=0) : Number;
		
		native public static function getTimer() : int;
		
		
		native public static function setTimeout(closure:Function, delay:Number, ... arguments):uint;
		
		native public static function clearTimeout(id:uint):void;
		
		native public static function setInterval(closure:Function, delay:Number, ... arguments):uint;
		
		native public static function clearInterval(id:uint):void;
		
		native public static function fetch(url:String) : Promise;
		
		
		
		
		
	}
}

