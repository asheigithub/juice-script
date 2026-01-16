//
// C:\Users\Manju-pc\AppData\Local\FlashDevelop\Apps\ascsdk\27.0.0\frameworks\libs\air\airglobal.swc\flash\utils\ByteArray
//
package flash.utils
{
	import flash.utils.ByteArray;
	import flash.errors.IllegalOperationError;
	
	/**
	 * ByteArray 类提供用于优化读取、写入以及处理二进制数据的方法和属性。
	 * <p>内存中的数据是一个压缩字节数组（数据类型的最紧凑表示形式），但可以使用标准 []（数组访问）运算符来操作 ByteArray 类的实例。</p>
	 * <p>ByteArray 类可能的用途包括：<ul>
	 * <li>通过使用数据类型优化数据的大小。</li>
	 * <li>使用从文件加载的二进制数据。</li>
	 * </ul></p>
	 * 
	 * @example 以下示例将布尔值和 pi 的双精度浮点表示形式写入字节数组。
	 * <p><b>注意：</b>在字节上调用 trace() 时，它将输出存储于字节数组中的字节的十进制等效值。</p>
	 * <p>注意如何在末尾添加一段代码以检查文件结尾错误，确保读取的字节流没有超出文件结尾。</p>
	 * <listing>
var byteArr:ByteArray = new ByteArray();

byteArr.writeBoolean(false);
trace(byteArr.length);            // 1
trace(byteArr[0]);            // 0

byteArr.writeDouble(Math.PI);
trace(byteArr.length);            // 9
trace(byteArr[0]);            // 0
trace(byteArr[1]);            // 64
trace(byteArr[2]);            // 9
trace(byteArr[3]);            // 33
trace(byteArr[4]);            // 251
trace(byteArr[5]);            // 84
trace(byteArr[6]);            // 68
trace(byteArr[7]);            // 45
trace(byteArr[8]);            // 24

byteArr.position = 0;

try {
	trace(byteArr.readBoolean() == false); // true
} 
catch(e:EOFError) {
	trace(e);           // EOFError: Error #2030: End of file was encountered.
}

try {
	trace(byteArr.readDouble());        // 3.141592653589793
}
catch(e:EOFError) {
	trace(e);           // EOFError: Error #2030: End of file was encountered.
}

try {
	trace(byteArr.readDouble());
} 
catch(e:EOFError) {
	trace(e);            // EOFError: Error #2030: End of file was encountered.
}
	 * </listing>
	 */
	public class ByteArray implements IDataInput2, IDataOutput2
	{
		
		public native function ByteArray();
		

		/**
		 * 可从字节数组的当前位置到数组末尾读取的数据的字节数。
		 * 
		 *   每次访问 ByteArray 对象时，将 bytesAvailable 属性与读取方法结合使用，以确保读取有效的数据。
		 */
		public native function get bytesAvailable () : uint;
		
		/**
		 * 更改或读取数据的字节顺序；Endian.BIG_ENDIAN 或 Endian.LITTLE_ENDIAN。
		 */
		public native function get endian () : String;
		
		
		public native function set endian (type:String) : void;

		/**
		 * ByteArray 对象的长度（以字节为单位）。
		 * 
		 *   如果将长度设置为大于当前长度的值，则用零填充字节数组的右侧。如果将长度设置为小于当前长度的值，将会截断该字节数组。
		 */
		public native function get length () : uint;
		public native function set length (value:uint) : void;

		/**
		 * 用于确定在写入或读取 ByteArray 实例时应使用 ActionScript 3.0、ActionScript 2.0 还是 ActionScript 1.0 格式。该值为 ObjectEncoding 类中的常数。
		 */
		public native function get objectEncoding () : uint;
		
		public native function set objectEncoding (version:uint) : void;

		/**
		 * 将文件指针的当前位置（以字节为单位）移动或返回到 ByteArray 对象中。下一次调用读取方法时将在此位置开始读取，或者下一次调用写入方法时将在此位置开始写入。
		 */
		public native function get position () : uint;
		public native function set position (offset:uint) : void;

		/**
		 * @private
		 */
		public native function get shareable () : Boolean;
		
		/**
		 * @private
		 */
		public native function set shareable (newValue:Boolean) : void;

		

		/**
		 * 清除字节数组的内容，并将 length 和 position 属性重置为 0。明确调用此方法将释放 ByteArray 实例占用的内存。
		 */
		public native function clear () : void;

		/**
		 * 未实现
		 * 
		 *   
		 */
		public native function compress (algorithm:String = "zlib") : void;

		/**
		 * 未实现
		 */
		public native function deflate () : void;

		/**
		 * 未实现
		 */
		public native function inflate () : void;

		/**
		 * 从字节流中读取布尔值。读取单个字节，如果字节非零，则返回 true，否则返回 false。
		 */
		public native function readBoolean () : Boolean;
		

		/**
		 * 从字节流中读取带符号的字节。
		 * 返回值的范围是从 -128 到 127。
		 * @return	介于 -128 和 127 之间的整数。
		 */
		public native function readByte () : int;
		

		/**
		 * 从字节流中读取 length 参数指定的数据字节数。从 offset 指定的位置开始，将字节读入 bytes 参数指定的 ByteArray 对象中，并将字节写入目标 ByteArray 中。
		 * @param	bytes	要将数据读入的 ByteArray 对象。
		 * @param	offset	bytes 中的偏移（位置），应从该位置写入读取的数据。
		 * @param	length	要读取的字节数。默认值 0 导致读取所有可用的数据。
		 */
		public native function readBytes (bytes:ByteArray, offset:uint = 0, length:uint = 0) : void;

		/**
		 * 从字节流中读取一个 IEEE 754 双精度（64 位）浮点数。
		 * @return	双精度（64 位）浮点数。
		 */
		public native function readDouble () : Number;
		

		/**
		 * 从字节流中读取一个 IEEE 754 单精度（32 位）浮点数。
		 * @return	单精度（32 位）浮点数。
		 */
		public native function readFloat () : Number;
		

		/**
		 * 从字节流中读取一个带符号的 32 位整数。
		 * 
		 *   返回值的范围是从 -2147483648 到 2147483647。
		 * @return	介于 -2147483648 和 2147483647 之间的 32 位带符号整数。
		 * @throws	EOFError 没有足够的数据可供读取。
		 */
		public native function readInt () : int;
		

		/**
		 * 使用指定的字符集从字节流中读取指定长度的多字节字符串。
		 * @param	length	要从字节流中读取的字节数。
		 * @param	charSet	表示用于解释字节的字符集的字符串。可能的字符集字符串包括 "shift-jis"、"cn-gb"、"iso-8859-1"”等。有关完整列表，请参阅支持的字符集。
		 *   注意：如果当前系统无法识别 charSet 参数的值，则应用程序将使用系统的默认代码页作为字符集。例如，charSet 参数的值（如在使用 01 而不是 1 的 myTest.readMultiByte(22, "iso-8859-01") 中）可能在您的开发系统上起作用，但在其他系统上可能不起作用。在其他系统上，应用程序将使用系统的默认代码页。
		 * @return	UTF-8 编码的字符串。
		 * @throws	EOFError 没有足够的数据可供读取。
		 */
		public native function readMultiByte (length:uint, charSet:String) : String;
		

		/**
		 * 从字节数组中读取一个以 AMF 序列化格式进行编码的对象。
		 * @return	反序列化的对象。
		 * @throws	EOFError 没有足够的数据可供读取。
		 */
		public native function readObject () :* ;
		
		/**
		 * 从字节流中读取一个带符号的 16 位整数。
		 * 
		 *   返回值的范围是从 -32768 到 32767。
		 * @return	介于 -32768 和 32767 之间的 16 位带符号整数。
		 * @throws	EOFError 没有足够的数据可供读取。
		 */
		public native function readShort () : int;
		

		/**
		 * 从字节流中读取无符号的字节。
		 * 
		 *   返回值的范围是从 0 到 255。
		 * @return	介于 0 和 255 之间的 32 位无符号整数。
		 * @throws	EOFError 没有足够的数据可供读取。
		 */
		public native function readUnsignedByte () : uint;
		

		/**
		 * 从字节流中读取一个无符号的 32 位整数。
		 * 
		 *   返回值的范围是从 0 到 4294967295。
		 * @return	介于 0 和 4294967295 之间的 32 位无符号整数。
		 * @throws	EOFError 没有足够的数据可供读取。
		 */
		public native function readUnsignedInt () : uint;
		

		/**
		 * 从字节流中读取一个无符号的 16 位整数。
		 * 
		 *   返回值的范围是从 0 到 65535。
		 * @return	介于 0 和 65535 之间的 16 位无符号整数。
		 * @throws	EOFError 没有足够的数据可供读取。
		 */
		public native function readUnsignedShort () : uint;
		

		/**
		 * 从字节流中读取一个 UTF-8 字符串。假定字符串的前缀是无符号的短整型（以字节表示长度）。
		 * @return	UTF-8 编码的字符串。
		 * @throws	EOFError 没有足够的数据可供读取。
		 */
		public native function readUTF () : String;
		

		/**
		 * 从字节流中读取一个由 length 参数指定的 UTF-8 字节序列，并返回一个字符串。
		 * @param	length	指明 UTF-8 字节长度的无符号短整型数。
		 * @return	由指定长度的 UTF-8 字节组成的字符串。
		 * @throws	EOFError 没有足够的数据可供读取。
		 */
		public native function readUTFBytes (length:uint) : String;
		

		/**
		 * 将字节数组转换为字符串。如果数组中的数据以 Unicode 字节顺序标记开头，应用程序在将其转换为字符串时将保持该标记。如果 System.useCodePage 设置为 true，应用程序在转换时会将数组中的数据视为处于当前系统代码页中。
		 * @return	字节数组的字符串表示形式。
		 */
		public native function toString () : String;
		

		/**
		 * 未实现
		 */
		public native function uncompress (algorithm:String = "zlib") : void;

		/**
		 * 写入布尔值。根据 value 参数写入单个字节。如果为 true，则写入 1，如果为 false，则写入 0。
		 * @param	value	确定写入哪个字节的布尔值。如果该参数为 true，则该方法写入 1；如果该参数为 false，则该方法写入 0。
		 */
		public native function writeBoolean (value:Boolean) : void;

		/**
		 * 在字节流中写入一个字节。
		 * 使用参数的低 8 位。忽略高 24 位。
		 * @param	value	一个 32 位整数。低 8 位将被写入字节流。
		 */
		public native function writeByte (value:int) : void;

		/**
		 * 将指定字节数组 bytes（起始偏移量为 offset，从零开始的索引）中包含 length 个字节的字节序列写入字节流。
		 * 
		 *   如果省略 length 参数，则使用默认长度 0；该方法将从 offset 开始写入整个缓冲区。如果还省略了 offset 参数，则写入整个缓冲区。 如果 offset 或 length 超出范围，它们将被锁定到 bytes 数组的开头和结尾。
		 * @param	bytes	ByteArray 对象。
		 * @param	offset	从 0 开始的索引，表示在数组中开始写入的位置。
		 * @param	length	一个无符号整数，表示在缓冲区中的写入范围。
		 */
		public native function writeBytes (bytes:ByteArray, offset:uint = 0, length:uint = 0) : void;

		/**
		 * 在字节流中写入一个 IEEE 754 双精度（64 位）浮点数。
		 * @param	value	双精度（64 位）浮点数。
		 */
		public native function writeDouble (value:Number) : void;

		/**
		 * 在字节流中写入一个 IEEE 754 单精度（32 位）浮点数。
		 * @param	value	单精度（32 位）浮点数。
		 */
		public native function writeFloat (value:Number) : void;

		/**
		 * 在字节流中写入一个带符号的 32 位整数。
		 * @param	value	要写入字节流的整数。
		 */
		public native function writeInt (value:int) : void;

		/**
		 * 使用指定的字符集将多字节字符串写入字节流。
		 * @param	value	要写入的字符串值。
		 * @param	charSet	表示要使用的字符集的字符串。可能的字符集字符串包括 "shift-jis"、"cn-gb"、"iso-8859-1"”等。有关完整列表，请参阅支持的字符集。
		 */
		public native function writeMultiByte (value:String, charSet:String) : void;

		/**
		 * 未实现
		 */
		public native function writeObject (object:*) : void ;

		/**
		 * 在字节流中写入一个 16 位整数。使用参数的低 16 位。忽略高 16 位。
		 * @param	value	32 位整数，该整数的低 16 位将被写入字节流。
		 */
		public native function writeShort (value:int) : void;

		/**
		 * 在字节流中写入一个无符号的 32 位整数。
		 * @param	value	要写入字节流的无符号整数。
		 */
		public native function writeUnsignedInt (value:uint) : void;

		/**
		 * 将 UTF-8 字符串写入字节流。先写入以字节表示的 UTF-8 字符串长度（作为 16 位整数），然后写入表示字符串字符的字节。
		 * @param	value	要写入的字符串值。
		 * @throws	RangeError 如果长度大于 65535。
		 */
		public native function writeUTF (value:String) : void;

		/**
		 * 将 UTF-8 字符串写入字节流。类似于 writeUTF() 方法，但 writeUTFBytes() 不使用 16 位长度的词为字符串添加前缀。
		 * @param	value	要写入的字符串值。
		 */
		public native function writeUTFBytes (value:String) : void;
		

		/**
		 * @private
		 * @param	key
		 * @return
		 */
		public native function getThisItem(key:Number):Number;
		
		/**
		 * @private
		 * @param	value
		 * @param	key
		 */
		public native function setThisItem(value:Number, key:Number):void;

	}
}
