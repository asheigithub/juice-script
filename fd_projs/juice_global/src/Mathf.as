package
{
	/**
	 * Mathf 类包含用于游戏开发的常用数学函数和常量。
	 * 类似于 Unity Engine 的 Mathf 类，所有属性和方法都是静态的。
	 * @langversion	3.0
	 */
	[no_constructor];
	public final class Mathf
	{
		/**
		 * The well-known 3.14159265358979... value (Read Only).
		 */
		public static const PI : float = 3.14159274;
		
		/**
		 * Degrees-to-radians conversion constant (Read Only).
		 */
		public static const Deg2Rad : float = 0.0174532924;
		
		/**
		 * Radians-to-degrees conversion constant (Read Only).
		 */
		public static const Rad2Deg : float = 57.29578;
		
		/**
		 * A tiny floating point value (Read Only).
		 */
		public static const Epsilon : float = 1.401298E-45;
		
		/**
		 * Positive infinity (Read Only).
		 */
		public static const Infinity : float = 1.0 / 0;
		
		/**
		 * Negative infinity (Read Only).
		 */
		public static const NegativeInfinity : float = -1.0 / 0;
		
		/**
		 * Returns the sine of angle f in radians.
		 * @param	f	Angle in radians.
		 * @return	The sine of angle f, between -1 and 1.
		 */
		public native static function sin(f:float) : float;
		
		/**
		 * Returns the cosine of angle f in radians.
		 * @param	f	Angle in radians.
		 * @return	The cosine of angle f, between -1 and 1.
		 */
		public native static function cos(f:float) : float;
		
		/**
		 * Returns the tangent of angle f in radians.
		 * @param	f	Angle in radians.
		 * @return	The tangent of angle f.
		 */
		public native static function tan(f:float) : float;
		
		/**
		 * Returns the arc-sine of f (the angle in radians whose sine is f).
		 * @param	f	Value between -1 and 1.
		 * @return	Arc-sine of f, between -PI/2 and PI/2.
		 */
		public native static function asin(f:float) : float;
		
		/**
		 * Returns the arc-cosine of f (the angle in radians whose cosine is f).
		 * @param	f	Value between -1 and 1.
		 * @return	Arc-cosine of f, between 0 and PI.
		 */
		public native static function acos(f:float) : float;
		
		/**
		 * Returns the arc-tangent of f (the angle in radians whose tangent is f).
		 * @param	f	Input value.
		 * @return	Arc-tangent of f, between -PI/2 and PI/2.
		 */
		public native static function atan(f:float) : float;
		
		/**
		 * Returns the angle in radians whose Tan is y/x.
		 * @param	y	Y coordinate.
		 * @param	x	X coordinate.
		 * @return	Angle in radians.
		 */
		public native static function atan2(y:float, x:float) : float;
		
		/**
		 * Returns the smallest integer greater than or equal to f.
		 * @param	f	Input value.
		 * @return	The smallest integer >= f.
		 */
		public native static function ceil(f:float) : float;
		
		/**
		 * Returns the smallest integer greater than or equal to f.
		 * @param	f	Input value.
		 * @return	The smallest integer >= f, as int.
		 */
		public native static function ceilToInt(f:float) : int;
		
		/**
		 * Returns the largest integer smaller than or equal to f.
		 * @param	f	Input value.
		 * @return	The largest integer <= f.
		 */
		public native static function floor(f:float) : float;
		
		/**
		 * Returns the largest integer smaller than or equal to f.
		 * @param	f	Input value.
		 * @return	The largest integer <= f, as int.
		 */
		public native static function floorToInt(f:float) : int;
		
		/**
		 * Returns f rounded to the nearest integer.
		 * @param	f	Input value.
		 * @return	The nearest integer to f.
		 */
		public native static function round(f:float) : float;
		
		/**
		 * Returns f rounded to the nearest integer.
		 * @param	f	Input value.
		 * @return	The nearest integer to f, as int.
		 */
		public native static function roundToInt(f:float) : int;
		
		/**
		 * Returns the absolute value of f.
		 * @param	f	Input value.
		 * @return	The absolute value of f.
		 */
		public native static function abs(f:float) : float;
		
		/**
		 * Returns the sign of f.
		 * @param	f	Input value.
		 * @return	1.0 if f is positive, -1.0 if negative, 1.0 if zero.
		 */
		public native static function sign(f:float) : float;
		
		/**
		 * Returns the largest of two values.
		 * @param	a	First value.
		 * @param	b	Second value.
		 * @return	The larger value.
		 */
		public native static function max(a:float, b:float) : float;
		
		/**
		 * Returns the smallest of two values.
		 * @param	a	First value.
		 * @param	b	Second value.
		 * @return	The smaller value.
		 */
		public native static function min(a:float, b:float) : float;
		
		/**
		 * Clamps a value between a minimum float and maximum float value.
		 * @param	value	Input value.
		 * @param	min		Minimum value.
		 * @param	max		Maximum value.
		 * @return	Clamped value.
		 */
		public native static function clamp(value:float, min:float, max:float) : float;
		
		/**
		 * Clamps a value between 0 and 1 and returns the value.
		 * @param	value	Input value.
		 * @return	Clamped value between 0 and 1.
		 */
		public native static function clamp01(value:float) : float;
		
		/**
		 * Loops the value t, so that it is never larger than length and never smaller than 0.
		 * @param	t	Input value.
		 * @param	length	Length of the loop.
		 * @return	Looped value.
		 */
		public native static function repeat(t:float, length:float) : float;
		
		/**
		 * PingPongs the value t, so that it is never larger than length and never smaller than 0.
		 * @param	t	Input value.
		 * @param	length	Length of the ping-pong.
		 * @return	Ping-ponged value between 0 and length.
		 */
		public native static function pingPong(t:float, length:float) : float;
		
		/**
		 * Linearly interpolates between a and b by t.
		 * @param	a	Start value.
		 * @param	b	End value.
		 * @param	t	Interpolation factor (clamped between 0 and 1).
		 * @return	Interpolated value.
		 */
		public native static function lerp(a:float, b:float, t:float) : float;
		
		/**
		 * Linearly interpolates between a and b by t without clamping t.
		 * @param	a	Start value.
		 * @param	b	End value.
		 * @param	t	Interpolation factor (not clamped).
		 * @return	Interpolated value.
		 */
		public native static function lerpUnclamped(a:float, b:float, t:float) : float;
		
		/**
		 * Same as Lerp but makes sure the values interpolate correctly when they wrap around 360 degrees.
		 * @param	a	Start angle in degrees.
		 * @param	b	End angle in degrees.
		 * @param	t	Interpolation factor (clamped between 0 and 1).
		 * @return	Interpolated angle.
		 */
		public native static function lerpAngle(a:float, b:float, t:float) : float;
		
		/**
		 * Returns 0 if value is between min and max.
		 * @param	a	Start of the range.
		 * @param	b	End of the range.
		 * @param	value	Input value.
		 * @return	Inverse linear interpolation (0 to 1).
		 */
		public native static function inverseLerp(a:float, b:float, value:float) : float;
		
		/**
		 * Interpolates between min and max with smoothing at the limits.
		 * @param	from	Start value.
		 * @param	to		End value.
		 * @param	t		Interpolation factor (clamped between 0 and 1).
		 * @return	Smoothly interpolated value.
		 */
		public native static function smoothStep(from:float, to:float, t:float) : float;
		
		/**
		 * Moves a value current towards target.
		 * @param	current	Current value.
		 * @param	target	Target value.
		 * @param	maxDelta	Maximum change per call.
		 * @return	Value that moves towards target.
		 */
		public native static function moveTowards(current:float, target:float, maxDelta:float) : float;
		
		/**
		 * Same as MoveTowards but makes sure the values interpolate correctly when they wrap around 360 degrees.
		 * @param	current	Current angle in degrees.
		 * @param	target	Target angle in degrees.
		 * @param	maxDelta	Maximum change per call.
		 * @return	Angle that moves towards target.
		 */
		public native static function moveTowardsAngle(current:float, target:float, maxDelta:float) : float;
		
		/**
		 * Calculates the shortest difference between two given angles in degrees.
		 * @param	current	Current angle in degrees.
		 * @param	target	Target angle in degrees.
		 * @return	Shortest difference in degrees.
		 */
		public native static function deltaAngle(current:float, target:float) : float;
		
		/**
		 * Returns f raised to power p.
		 * @param	f	Base value.
		 * @param	p	Exponent.
		 * @return	f raised to power p.
		 */
		public native static function pow(f:float, p:float) : float;
		
		/**
		 * Returns e raised to the specified power.
		 * @param	power	Exponent.
		 * @return	e ^ power.
		 */
		public native static function exp(power:float) : float;
		
		/**
		 * Returns the square root of f.
		 * @param	f	Input value (must be >= 0).
		 * @return	Square root of f.
		 */
		public native static function sqrt(f:float) : float;
		
		/**
		 * Returns the natural (base e) logarithm of f.
		 * @param	f	Input value (must be > 0).
		 * @return	Natural logarithm of f.
		 */
		public native static function log(f:float) : float;
		
		/**
		 * Returns the logarithm of f in base p.
		 * @param	f	Input value.
		 * @param	p	Base of the logarithm.
		 * @return	Logarithm of f in base p.
		 */
		public native static function logBase(f:float, p:float) : float;
		
		/**
		 * Returns the base 10 logarithm of f.
		 * @param	f	Input value.
		 * @return	Base 10 logarithm of f.
		 */
		public native static function log10(f:float) : float;
		
		/**
		 * Compares two floating point values and returns true if they are similar.
		 * @param	a	First value.
		 * @param	b	Second value.
		 * @return	True if a and b are approximately equal.
		 */
		public native static function approximately(a:float, b:float) : Boolean;
		
		/**
		 * Returns true if the value is a power of two.
		 * @param	value	Input value.
		 * @return	True if value is a power of two.
		 */
		public native static function isPowerOfTwo(value:int) : Boolean;
		
		/**
		 * Returns the next power of two that is >= value.
		 * @param	value	Input value.
		 * @return	Next power of two >= value.
		 */
		public native static function nextPowerOfTwo(value:int) : int;
	}
}
