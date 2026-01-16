package
{
	/**
	* 表示正 Infinity 的特殊值。此常量的值与 Number.POSITIVE_INFINITY 相同。
	* @see Number#NEGATIVE_INFINITY
	* @example 除以 0 的结果为 Infinity（仅当除数为正数时）。
	* <listing version="3.0">
	* 
	* trace(0 / 0);  // NaN
    * trace(7 / 0);  // Infinity
    * trace(-7 / 0); // -Infinity
	* </listing>
	*/
   public const Infinity:Number = 1 / 0;
}