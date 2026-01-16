package
{
	/**
	 * 调试时显示表达式或写入日志文件。单个跟踪语句可支持多个参数。如果跟踪语句中的任何参数包含 String 之外的数据类型，则跟踪函数将调用与该数据类型关联的 toString() 方法。例如，如果该参数是一个布尔值，则跟踪函数将调用 Boolean.toString() 并显示返回值。
	 * @param	... rest 要计算的一个或多个（逗号分隔）表达式。对于多个表达式，输出中每个表达式之间都将插入一个空格。
	 * @example 下面的示例使用类 TraceExample 来演示如何使用 trace() 方法输出简单字符串。通常情况下，消息将输出到“调试”控制台。
<listing>
trace("Hello World");
</listing>
	 */
    public function trace(... rest) : void{}
}