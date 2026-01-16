//
// C:\Users\Manju-pc\AppData\Local\FlashDevelop\Apps\ascsdk\27.0.0\frameworks\libs\air\airglobal.swc\flash\utils\Endian
//
package flash.utils
{
	/**
	 * Endian 类中包含一些值，它们表示用于表示多字节数字的字节顺序。字节顺序为 bigEndian（最高有效字节位于最前）或 littleEndian（最低有效字节位于最前）。
	 * 
	 */
	public final class Endian
	{
		/**
		 * 表示多字节数字的最高有效字节位于字节序列的最前面。
		 * 十六进制数字 0x12345678 包含 4 个字节（每个字节包含 2 个十六进制数字）。最高有效字节为 0x12。最低有效字节为 0x78。（对于等效的十进制数字 305419896，最高有效数字是 3，最低有效数字是 6）。使用 bigEndian 字节顺序（最高有效字节位于最前）的流将写入：
		 * 12 34 56 78
		 */
		public static const BIG_ENDIAN : String = "bigEndian";

		/**
		 * 表示多字节数字的最低有效字节位于字节序列的最前面。
		 * 十六进制数字 0x12345678 包含 4 个字节（每个字节包含 2 个十六进制数字）。最高有效字节为 0x12。最低有效字节为 0x78。（对于等效的十进制数字 305419896，最高有效数字是 3，最低有效数字是 6）。使用 littleEndian 字节顺序（最低有效字节位于最前）的流将写入：
		 * 78 56 34 12
		 */
		public static const LITTLE_ENDIAN : String = "littleEndian";

		/**
		 * @private
		 */
		public native function Endian ();
	}
}