package 
{
	/**
	 * Number 数据类型的一个特殊成员，用来表示“非数字”(NaN) 值。当数学表达式生成的值无法表示为数字时，结果为 NaN。
	 * <p>NaN 值不是 int 或 uint 数据类型的成员。</p>
	 * <p>NaN 值不被视为等于任何其他值（包括 NaN），因而无法使用等于运算符测试一个表达式是否为 NaN。要确定一个数字是否为 NaN 函数，请使用 isNaN()。</p>
	 * @see Number#NaN
	 */
	public const NaN:Number = 0 / 0;
}