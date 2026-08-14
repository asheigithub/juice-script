package
{
	/**
	 * String 类为表示一串字符的数据类型。String 类提供了处理原始字符串值类型的方法和属性。可以使用 String() 函数将任意对象的值转换为 String 数据类型的对象。
	 * 
	 */
	public final class String
	{
		
		
		/**
		 * 一个整数，它指定在所指定的 String 对象中的字符数。
		 */
		public native function get length () : int;

		/**
		 * 创建已初始化为指定字符串的新 String 对象。
		 * <p><b>注意</b> 由于字符串文本比 String 对象需要的开销少且通常更易于使用，所以，除非有充分的理由要使用 String 对象而不是字符串文本，否则应该使用字符串文本而不是 String 类。</p>
		 * @param	value 新 String 对象的初始值。
		 */
		public native function String (value:*= "");
		
		/**
		 * 返回由参数 index 指定的位置处的字符。如果 index 不是从 0 到 string.length - 1 之间的数字，则返回一个空字符串。
		 * @param	index 一个整数，指定字符在字符串中的位置。第一个字符由 0 表示，最后一个字符由 my_str.length - 1 表示。
		 * @return 指定索引处的字符。或者，如果指定的索引不在该字符串的索引范围内，则为一个空字符串。
		 */
		AS3 native function charAt(index:Number = 0):String;
		
		/**
		 * 返回指定 index 处的字符的数值 Unicode 字符代码。如果 index 不是从 0 到 string.length - 1 之间的数字，则返回 NaN。
		 * @param	index 一个整数，指定字符在字符串中的位置。第一个字符由 0 表示，最后一个字符由 my_str.length - 1 表示。
		 * @return  指定索引处的字符的 Unicode32 字符代码。或者，如果索引不在此字符串的索引范围内，则为 NaN。
		 */
		AS3 native function charCodeAt(index:Number = 0):Number;

		/**
		 * 返回一个字符串，该字符串由参数中的 Unicode 字符代码所表示的字符组成。
		 * @param	... charCodes 一系列表示 Unicode 值的十进制整数
		 * @return  指定的 Unicode 字符代码的字符串值。
		 */
		AS3 native static function fromCharCode(... charCodes):String;

		/**
		 * 搜索字符串，并返回在调用字符串内 startIndex 位置上或之后找到的 val 的第一个匹配项的位置。此索引从 0 开始，这意味着字符串的第一个字符位于索引 0，而不是索引 1。如果未找到 val，则该方法返回 -1。
		 * @param	val 要搜索的子字符串。
		 * @param	startIndex 一个可选整数，指定搜索的起始索引。
		 * @return 指定子字符串的第一个匹配项的索引，或 -1。
		 */
		AS3 native function indexOf(val:String =null, startIndex:Number = 0):int;

		/**
		 * 从右向左搜索字符串，并返回在 startIndex 之前找到的最后一个 val 匹配项的索引。此索引从零开始，这意味着第一个字符位于索引 0 处，最后一个字符位于 string.length - 1 处。如果未找到 val，则该方法返回 -1。
		 * @param	val 要搜索的字符串。
		 * @param	startIndex 一个可选整数，指定开始搜索 val 的起始索引。默认为允许的最大索引值。如果未指定 startIndex，则从字符串中的最后一项开始搜索。
		 * @return  指定子字符串的最后一个匹配项的位置，或 -1（如果未找到）。

		 */
		AS3 native function lastIndexOf(val:String =null, startIndex:Number = 0x7FFFFFFF):int;

		/**
		 * 返回一个字符串，该字符串包括从 startIndex 字符一直到 endIndex 字符（但不包括该字符）之间的所有字符。不修改原始 String 对象。如果未指定 endIndex 参数，此子字符串的结尾就是该字符串的结尾。如果按 startIndex 索引到的字符与按 endIndex 索引到的字符相同或位于后者的右侧，则该方法返回一个空字符串。
		 * @param	startIndex 片段起始点的从 0 开始的索引。如果 startIndex 是一个负数，则从右到左创建片段，其中 -1 是最后一个字符。
		 * @param	endIndex 一个比片段终点的索引大 1 的整数。由 endIndex 参数索引的字符未包括在已提取的字符串中。如果 endIndex 是一个负数，则终点根据从字符串的结尾向后数确定，其中 -1 表示最后一个字符。默认为允许的最大索引值。如果省略此参数，则使用 String.length。
		 * @return  基于指定索引的子字符串。
		 */
		AS3 native function slice(startIndex:Number = 0, endIndex:Number = 0x7fffffff):String;
		
		/**
		 * 将 String 对象拆分为一个子字符串数组，方法是在所有出现指定 delimiter 参数的位置进行拆分。
		 * @param	delimiter 指定拆分此字符串的位置的模式。正则表达式未实现
		 * @param	limit 要放入数组中的最大项数。默认为允许的最大值。
		 * @return  一个子字符串的数组。
		 */
		AS3 native function split(delimiter:* = null, limit:* = 0x7fffffff):Array;

		/**
		 * 返回一个子字符串，该子字符串中的字符是通过从指定的 startIndex 开始，按照 len 指定的长度截取所得的。原始字符串保持不变。
		 * @param	startIndex  一个整数，指定用于创建子字符串的第一个字符的索引。如果 startIndex 是一个负数，则起始索引从字符串的结尾开始确定，其中 -1 表示最后一个字符。
		 * @param	len  要创建的子字符串中的字符数。默认值为所允许的最大值。如果未指定 len，则子字符串包括从 startIndex 到字符串结尾的所有字符。
		 * @return  基于指定参数的子字符串。
		 */
		AS3 native function substr(startIndex:Number = 0, len:Number = 0x7fffffff):String;

		/**
		 * 返回一个字符串，其中包含由 startIndex 指定的字符和一直到 endIndex - 1 的所有字符。如果未指定 endIndex，则使用 String.length。如果 startIndex 的值等于 endIndex 的值，则该方法返回一个空字符串。如果 startIndex 的值大于 endIndex 的值，则在执行函数之前会自动交换参数。原始字符串保持不变。
		 * @param	startIndex 一个整数，指定用于创建子字符串的第一个字符的索引。startIndex 的有效值范围为从 0 到 String.length。如果 startIndex 是一个负值，则使用 0 。
		 * @param	endIndex 一个整数，它比所提取的子字符串中的最后一个字符的索引大 1。endIndex 的有效值范围为从 0 到 String.length。endIndex 处的字符不包括在子字符串中。默认为允许的最大索引值。如果省略此参数，则使用 String.length。如果此参数是一个负值，则使用 0。
		 * @return  基于指定参数的子字符串。
		 */
		AS3 native function substring(startIndex:Number = 0, endIndex:Number = 0x7fffffff):String;

		
		/**
		* 搜索指定的 pattern 并返回第一个匹配子字符串的索引。如果没有匹配的子字符串，该方法返回 -1。
		* 	pattern:* — 要匹配的模式，可以为任何类型的对象，但通常是字符串。如果 pattern 不是字符串，则该方法在执行前会将其转换为字符串。 当前实现中未实现正则表达式，所以只支持字符串搜索。
		*/
		AS3 native function search(pattern:* = null):int;
		
		
		/**
		 * 返回此字符串的一个副本，其中所有大写的字符均转换为小写字符。原始字符串保持不变。
		 * @return
		 */
		AS3 native function toLowerCase():String;

		
		/**
		* 返回此字符串的一个副本，其中所有大写的字符均转换为小写字符。原始字符串保持不变。虽然此方法旨在以特定于区域设置的方式处理转换，但 juicescript 实现生成的结果与 toLowerCase() 方法生成的结果相同。
		*/
		AS3 native function toLocaleLowerCase():String;
		
		

		/**
		 * 返回此字符串的一个副本，其中所有小写的字符均转换为大写字符。原始字符串保持不变。
		 * @return
		 */
		AS3 native function toUpperCase():String;

		/**
		 * 返回此字符串的一个副本，其中所有小写的字符均转换为大写字符。原始字符串保持不变。虽然此方法旨在以特定于区域设置的方式处理转换，但 juicescript 实现生成的结果与 toUpperCase() 方法生成的结果相同。
		 * @return
		 */
		AS3 native function toLocaleUpperCase():String
		
		
		/**
		 * 相对于字符串匹配指定的 pattern 并返回一个新字符串，其中的第一个 pattern 匹配项被替换为 repl 所指定的内容。
		 * @param	pattern
		 * @param	repl
		 * @return
		 */
		AS3 native function replace(pattern:String, repl:*):String;
		

		/**
		 * 在 String 对象末尾追加补充参数（如果需要，将它们转换为字符串）并返回结果字符串。源 String 对象的原始值保持不变。
		 * @param	... args 0 个或多个要连接的值。
		 * @return  由该字符串与指定的参数连接而成的新字符串。
		 */
		AS3 native function concat(... args):String;
		
		
		/**
		 * @private
		 * @return
		 */
		AS3 native function toString () : String;

		/**
		 * 返回 String 实例的原始值。此方法旨在将 String 对象转换为原始字符串值。因为 Juice 运行时可在必要时自动调用 valueOf()，所以几乎不需要明确调用此方法。
		 * @return 字符串的值。
		 */
		AS3 native function valueOf () : String;
	}
}
