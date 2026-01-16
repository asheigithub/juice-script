package
{
	/**
	 * 如果数值不在可接受的范围内，则会引发 RangeError 异常。使用数组时，引用不存在的数组项的索引位置将会引发 RangeError 异常。如果参数不在可接受的数字范围内，则使用 <codeph class="+ topic/ph pr-d/codeph ">Number.toExponential()</codeph>、<codeph class="+ topic/ph pr-d/codeph ">Number.toPrecision()</codeph> 和 <codeph class="+ topic/ph pr-d/codeph ">Number.toFixed()</codeph> 方法将引发 RangeError 异常。可以扩展 <codeph class="+ topic/ph pr-d/codeph ">Number.toExponential()</codeph>、<codeph class="+ topic/ph pr-d/codeph ">Number.toPrecision()</codeph> 和 <codeph class="+ topic/ph pr-d/codeph ">Number.toFixed()</codeph> 以避免引发 RangeError。	
	 */
	public dynamic class RangeError extends Error
	{
		public native function RangeError (message:String="", id:int=0);
	}
}