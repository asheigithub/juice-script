package
{
	/**
	 * 使用 Array 类可以访问和操作数组。Array 索引从零开始，这意味着数组中的第一个元素为 [0]，第二个元素为 [1]，依此类推。要创建 Array 对象，可以使用 new Array() 构造函数。Array() 还可以作为函数调用。此外，还可以使用数组访问 ([]) 运算符初始化数组或访问数组元素。
	 */
	public dynamic class Array extends Object
	{
		/**
		 * 指定 Array 类排序方法为不区分大小写的排序。您可以对 sort() 方法或 sortOn() 方法中的 options 参数使用此常数。 
		 * 此常数的值为 1。
		 */
		public static const CASEINSENSITIVE : uint = 1;

		/**
		 * 指定 Array 类排序方法为降序排序。您可以对 sort() 方法或 sortOn() 方法中的 options 参数使用此常数。 
		 * 此常数的值为 2。
		 */
		public static const DESCENDING : uint = 2;

		/**
		 * 指定 Array 类排序方法为数值（而不是字符串）排序。在 options 参数中包括此常数会导致 sort() 方法和 sortOn() 方法将数字作为数值排序，而不是作为数字字符的字符串排序。如果不使用 NUMERIC 常数，则排序将每个数组元素视为一个字符串，并且按照 Unicode 顺序生成结果。
		 * 
		 *   例如，以值为 [2005, 7, 35] 的数组为例，如果 NUMERIC 常数未包括在 options 参数中，则排序后的数组为 [2005, 35, 7]，但如果包括了 NUMERIC 常数，则排序后的数组为 [7, 35, 2005]。 此常数仅应用于数组中的数字；不应用于包含数值数据（如 ["23", "5"]）的字符串。此常数的值为 16。
		 */
		public static const NUMERIC : uint = 16;

		
		/**
		 * 允许创建包含指定元素的数组。
		 * 
		 * <p><b>注意</b>此构造函数接受的参数的类型和数量可变。根据传递的参数类型和数量的不同（由每一项详细定义），此构造函数具有不同的行为。ActionScript 3.0 不支持方法或构造函数重载。</p>
		 * 
		 * <p>您可以指定任何类型的值。数组中第一个元素的索引（或位置）始终为 0。</p>
		 * 
		 * <p>如果不传递任何参数，则认为创建长度为0的数组。</p>
		 * 
		 * @param	...rest  一个以逗号分隔的列表，包含一个或多个任意值。
		 * <p><b>注意:</b> 如果传递给 Array 构造函数的只有一个单数值参数，则认为该参数指定数组的 length 属性。</p>
		 */
		public native function Array (...rest);

		/**
		 * 指定数组中元素数量的非负整数。在向数组中添加新元素时，此属性会自动更新。当您给数组元素赋值（例如，my_array[index] = value）时，如果 index 是数字，而且 index+1 大于 length 属性，则 length 属性会更新为 index+1。
		 * <p><b>注意：</b>如果您为 length 属性所赋的值小于现有长度，会将数组截断。</p>
		 */
		public native function get length () : uint;

		
		public native function set length (newLength:uint) : void;
		
		/**
		 * 对数组中的每一项执行测试函数，直到获得返回 true 的项。使用此方法确定数组中的所有项是否满足条件，如具有小于某一特定数值的值。
		 * @param	callback 要对数组中的每一项运行的函数。此函数可以包含简单的比较操作（如 item &lt; 20）或者更复杂的操作，并用三个参数来调用，即项值、项索引和 Array 对象：
		 * <p> function callback(item:~~, index:int, array:Array):Boolean;</p>
		 * @param	thisObject 用作函数的 this 的对象。
		 * @return 如果数组中的所有项对于指定的函数都返回 true，则为布尔值 true，否则为 false。
		 */
		AS3 native function some (callback:Function, thisObject:*= null) : Boolean;
		
		/**
		 * 对数组中的每一项执行测试函数，直到获得对指定的函数返回 false 的项。使用此方法可确定数组中的所有项是否满足某一条件，如具有的值小于某一特定数值。
		 * @param	callback 要对数组中的每一项运行的函数。该函数可以包含简单的比较操作（例如，item &lt; 20）或者更复杂的操作，并用三个参数来调用，即项值、项索引和 Array 对象： 
		 * <p>function callback(item:~~, index:int, array:Array):Boolean;</p>
		 * @param	thisObject 用作函数的 this 的对象。
		 * @return 如果数组中的所有项对指定的函数都返回 true，则为布尔值 true；否则为 false。
		 * @example 下面的示例测试两个数组，以确定每个数组中的每一项是否都是数字。还输出了测试结果，说明对于第一个数组，isNumeric 是 true，对于第二个数组则是 false：
		 * <listing>
		 * function isNumeric(element, index:int, arr:Array):Boolean {
         *   return (element is Number);
         * }
		 * var arr1:Array = new Array(1, 2, 4);
		 * var res1:Boolean = arr1.every(isNumeric);
		 * trace("isNumeric:", res1); // true
		 * 
		 * var arr2:Array = new Array(1, 2, "ham");
		 * var res2:Boolean = arr2.every(isNumeric); 
		 * trace("isNumeric:", res2); // false
		 * </listing>
		 * 
		 */		
		 AS3 native function every (callback:Function, thisObject:*= null) : Boolean;
		

		/**
		 * 对数组中的每一项执行函数。
		 * @param	callback 要对数组中的每一项运行的函数。此函数可以包含简单的命令（如 trace() 语句）或者更复杂的操作，并用三个参数来调用，即项值、项索引和 Array 对象：
		 * <p>function callback(item:~~, index:int, array:Array):void;</p>
		 * @param	thisObject 用作函数的 this 的对象。
		 */
		 AS3 native function forEach (callback:Function, thisObject:*= null) : void;
		

		/**
		 * 对数组中的每一项执行测试函数，并构造一个新数组，其中的所有项都对指定的函数返回 true。如果某项返回 false，则新数组中将不包含此项。
		 * 
		 * @param	callback 要对数组中的每一项运行的函数。该函数可以包含简单的比较操作（例如，item &lt; 20）或者更复杂的操作，并用三个参数来调用，即项值、项索引和 Array 对象：function callback(item:~~, index:int, array:Array):Boolean;
		 * @param	thisObject 用作函数的 this 的对象。
		 * @return 一个新数组，它包含原始数组中返回 true 的所有项。
		 * @example 下面的示例创建一个数组，其中包括职务为经理的所有员工：
		 * <listing>
		 * var employees:Array = new Array();
		 * employees.push({name:"Employee 1", manager:false});
		 * employees.push({name:"Employee 2", manager:true});
		 * employees.push({name:"Employee 3", manager:false});
		 * trace("Employees:");
		 * employees.forEach(traceEmployee);
		 * 
		 * var managers:Array = employees.filter(isManager);
		 * trace("Managers:");
		 * managers.forEach(traceEmployee);
		 * 
		 * function isManager(element:~~, index:int, arr:Array):Boolean {
		 *    return (element.manager == true);
		 * }
		 * function traceEmployee(element:~~, index:int, arr:Array):void {
		 *    trace("\t" + element.name + ((element.manager) ? " (manager)" : ""));
		 * }
		 * </listing>
		 */
		 AS3 native function filter(callback:Function, thisObject:*= null) : Array;
		
		/**
		 * 对数组中的每一项执行函数并构造一个新数组，其中包含与原始数组中的每一项的函数结果相对应的项。
		 * @param	callback 要对数组中的每一项运行的函数。此函数可以包含简单的命令（如更改字符串数组的大小写）或更复杂的操作，并用 3 个参数来调用，即项值、项索引和 Array 对象：
		 * <p>function callback(item:*, index:int, array:Array):String;</p>
		 * @param	thisObject 用作函数的 this 的对象。
		 * @return 一个新数组，其中包含此函数对原始数组中每一项的执行结果。
		 * @example 下面的示例将数组中的所有项更改为使用大写字母：
		 * <listing>
		 * var arr:Array = new Array("one", "two", "Three");
		 * trace(arr); // one,two,Three
		 * var upperArr:Array = arr.map(toUpper);
		 * trace(upperArr); // ONE,TWO,THREE
		 * 
		 *    function toUpper(element:~~, index:int, arr:Array):String {
		 *    return String(element).toUpperCase();
		 *}
		 * 
		 * </listing>
		 */
		AS3 native function map(callback:Function, thisObject:*= null) : Array;
		
		/**
		 * 使用 strict equality (===) 运算符搜索数组中的项，并返回项的索引位置。
		 * @param	searchElement 要在数组中查找的项。
		 * @param	fromIndex 数组中的位置，从该位置开始搜索项。
		 * @return  数组项的索引位置（从 0 开始）。如果未找到 searchElement 参数，则返回值为 -1。
		 */
		AS3 native function indexOf (searchElement:*, fromIndex:uint = 0) : int;
		
		/**
		 * 搜索数组中的项（从最后一项开始向前搜索），并使用 strict equality (===) 运算符返回匹配项的索引位置。
		 * @param	searchElement 要在数组中查找的项。 
		 * @param	fromIndex 数组中的位置，从该位置开始搜索项。默认为允许的最大索引值。如果不指定 fromIndex，将从数组中的最后一项开始进行搜索。
		 * @return 数组项的索引位置（从 0 开始）。如果未找到 searchElement 参数，则返回值为 -1。
		 */
		AS3 native function lastIndexOf(searchElement:*, fromIndex:int = 0x7fffffff):int;
		

		/**
		 * 将参数中指定的元素与数组中的元素连接，并创建新的数组。如果这些参数指定了一个数组，将连接该数组中的元素。如果不传递任何参数，则新数组是原始数组的副本（浅表克隆）。
		 * @param	args	要连接到新数组中的任意数据类型的值（如数字、元素或字符串）。
		 * @return	一个数组，其中包含此数组中的元素，后跟参数中的元素。
		 */
		AS3 native function concat (...rest) : Array; 

		/**
		 * 将一个单独的元素插入一个数组中。此方法会修改数组但不制作副本。
		 * @param	index 一个整数，指定元素要插入数组中的位置。可以用一个负整数来指定相对于数组结尾的位置（例如，-1 是数组的最后一个元素）。
		 * @param	element
		 */
		AS3 native function insertAt (index:int, element:*) : void;

		/**
		 * 将数组中的元素转换为字符串、在元素间插入指定的分隔符、连接这些元素然后返回结果字符串。嵌套数组总是以逗号 (,) 分隔，而不使用传递给 join() 方法的分隔符分隔。
		 * @param	sep 在返回字符串中分隔数组元素的字符或字符串。如果省略此参数，则使用逗号作为默认分隔符。
		 * @return 一个字符串，由转换为字符串并由指定参数分隔的数组元素组成。
		 * @example 下面的代码创建一个 Array 对象 myArr，其中包含元素 one、two 和 three，然后创建一个包含 one and two and three 的字符串（使用 join() 方法）。
		 * <listing>
		 * var myArr:Array = new Array("one", "two", "three");
		 * var myStr:String = myArr.join(" and ");
		 * trace(myArr); // one,two,three
		 * trace(myStr); // one and two and three
		 * </listing>
		 */
		AS3 native function join (sep:*= null) : String;
		
		/**
		 * 删除数组中最后一个元素，并返回该元素的值。
		 * @return 指定的数组中最后一个元素（可以为任意数据类型）的值。
		 */
		AS3 native function pop () : * ;

		/**
		 * 将一个或多个元素添加到数组的结尾，并返回该数组的新长度。
		 * @param	args	要追加到数组中的一个或多个值。
		 * @return	一个表示新数组长度的整数。
		 */
		AS3 native function push (...rest) : uint;

		/**
		 * 从数组中删除一个单独的元素。此方法会修改数组但不制作副本。
		 * @param	index 一个整数，指定数组中要被删除元素的索引。可以用一个负整数来指定相对于数组结尾的位置（例如，-1 是数组的最后一个元素）。
		 * @return 从原数组中删除的元素。
		 */
		AS3 native function removeAt (index:int) : * ;

		/**
		 * 在当前位置倒转数组。
		 * @return	新数组。
		 */
		AS3 native function reverse () : Array;

		/**
		 * 删除数组中第一个元素，并返回该元素。其余数组元素将从其原始位置 i 移至 i-1。
		 * @return	数组中的第一个元素（可以是任意数据类型）。
		 */
		AS3 native function shift () : * ;


		/**
		 * 返回由原始数组中某一范围的元素构成的新数组，而不修改原始数组。返回的数组包括 startIndex 元素以及从其开始到 endIndex 元素（但不包括该元素）的所有元素。
		 * 
		 *   如果不传递任何参数，则新数组是原始数组的副本（浅表克隆）。
		 * @param	startIndex	一个数字，指定片段起始点的索引。如果 startIndex 是负数，则起始点从数组的结尾开始，其中 -1 指的是最后一个元素。
		 * @param	endIndex	一个数字，指定片段终点的索引。如果省略此参数，则片段包括数组中从开头到结尾的所有元素。如果 endIndex 是负数，则终点从数组的结尾指定，其中 -1 指的是最后一个元素。
		 * @return	一个数组，由原始数组中某一范围的元素组成。
		 */
		AS3 native function slice (A:int = 0, B:int = 16777215) : Array;


		/**
		 * 给数组添加元素以及从数组中删除元素。此方法会修改数组但不制作副本。
		 * @param	startIndex	一个整数，它指定数组中开始进行插入或删除的位置处的元素的索引。您可以用一个负整数来指定相对于数组结尾的位置（例如，-1 是数组的最后一个元素）。
		 * @param	deleteCount	一个整数，它指定要删除的元素数量。该数量包括 startIndex 参数中指定的元素。如果没有为 deleteCount 参数指定值，则该方法将删除从 startIndex 元素到数组中最后一个元素的所有值。如果该参数的值为 0，则不删除任何元素。
		 * @param	values	用逗号分隔的一个或多个值的可选列表，此可选列表将插入 startIndex 参数中的指定位置处的数组中。如果插入的值是数组类型，则保持此数组的原样并将其作为单个元素插入。例如，如果您将长度为 3 的现有数组与另一长度为 3 的数组结合，则生成的数组将只包含 4 个元素。但是，其中的一个元素将是长度为 3 的一个数组。
		 * @return	一个数组，包含从原始数组中删除的元素。
		 */
		AS3 native function splice (startIndex:int = 0, deleteCount:uint = 4294967295, ... values) : Array ;


		/**
		 * 将一个或多个元素添加到数组的开头，并返回该数组的新长度。数组中的其他元素从其原始位置 i 移到 i+1。
		 * @param	args	一个或多个要插入到数组开头的数字、元素或变量。
		 * @return	一个整数，表示该数组的新长度。
		 */
		AS3 native function unshift (...rest) : uint ;

		/**
		 * 对数组中的元素进行排序。此方法按 Unicode 值排序。
		 * 默认情况下，Array.sort() 按以下方式进行排序：
		 * <ul>
		 * <li>排序区分大小写（Z 优先于 a）。</li>
		 * <li>按升序排序（a 优先于 b）。</li>
		 * <li>修改该数组以反映排序顺序；在排序后的数组中不按任何特定顺序连续放置具有相同排序字段的多个元素。</li>
		 * <li>元素无论属于何种数据类型，都作为字符串进行排序，所以 100 在 99 之前，这是因为 "1" 的字符串值小于 "9" 的字符串值。</li>
		 * </ul>
		 * 如果要使用与默认设置不同的设置对数组进行排序，可以使用 ...args 参数说明中 sortOptions 部分所描述的某种排序选项，也可以创建自定义函数来进行排序。如果创建自定义函数，请调用 sort() 方法，并将自定义函数的名称作为第一个参数 (compareFunction)。
		 * @param	...args 指定一个比较函数和确定排序行为的一个或多个值的参数。
		 * <p>此方法使用语法和参数顺序 Array.sort(compareFunction, sortOptions)，其参数定义如下：</p>
		 * <ul>
		 * <li>compareFunction - 一个用来确定数组元素排序顺序的比较函数。此参数是可选的。比较函数应该用两个参数进行比较。给定元素 A 和 B，compareFunction 的结果可以具有负值、0 或正值：
<ul><li>若返回值为负，则表示 A 在排序后的序列中出现在 B 之前。</li>
<li>若返回值为 0，则表示 A 和 B 具有相同的排序顺序。</li>
<li>若返回值为正，则表示 A 在排序后的序列中出现在 B 之后。</li></ul></li>

<li>
sortOptions - 一个或多个数字或定义的常数，相互之间由 |（按位 OR）运算符隔开，它们将更改排序的默认行为。此参数是可选的。下面是 sortOptions 可接受的值：
<ul>
<li>1 或 Array.CASEINSENSITIVE</li>
<li>2 或 Array.DESCENDING</li>
<li>16 或 Array.NUMERIC</li>
</ul>
</li>
</ul>
		 * 
		 * @return 不返回任何内容并修改该数组以反映排序顺序。
		 * 
		 * @example 下面的代码创建 Array 对象 vegetables，其中包含元素 [spinach, green pepper, cilantro, onion, avocado]。然后，通过 sort() 方法对该数组进行排序，调用该方法时不带参数。结果是 vegetables 按字母顺序排序 ([avocado, cilantro, green pepper, onion, spinach])。
		 * <listing>
 var vegetables:Array = new Array("spinach",
		 "green pepper",
		 "cilantro",
		 "onion",
		 "avocado");

trace(vegetables); // spinach,green pepper,cilantro,onion,avocado
vegetables.sort();
trace(vegetables); // avocado,cilantro,green pepper,onion,spinach
		 * </listing>
		 * 下面的代码创建 Array 对象 vegetables，其中包含元素 [spinach, green pepper, Cilantro, Onion, and Avocado]。然后，通过 sort() 方法对该数组进行排序，第一次调用该方法时不带参数，其结果是 [Avocado,Cilantro,Onion,green pepper,spinach]。然后再次调用 sort()（对 vegetables），调用时将 CASEINSENSITIVE 常量作为参数。结果是 vegetables 按字母顺序排序 ([Avocado, Cilantro, green pepper, Onion, spinach])。
		 * <listing>
var vegetables:Array = new Array("spinach",
                 "green pepper",
                 "Cilantro",
                 "Onion",
                 "Avocado");

vegetables.sort();
trace(vegetables); // Avocado,Cilantro,Onion,green pepper,spinach
vegetables.sort(Array.CASEINSENSITIVE);
trace(vegetables); // Avocado,Cilantro,green pepper,Onion,spinach

		 * </listing>
		 * 
		 * 下面的代码创建空的 Array 对象 vegetables，然后通过五次调用如下方法来填充该数组：push()。每次调用 push() 时，都创建一个新的 Vegetable 对象（通过调用 Vegetable() 构造函数，该构造函数接受 String (name) 和 Number (price) 对象）。使用所显示的值调用 push() 五次，会生成下面的数组：[lettuce:1.49, spinach:1.89, asparagus:3.99, celery:1.29, squash:1.44]。然后，使用 sort() 方法排序该数组，从而得到数组 [asparagus:3.99, celery:1.29, lettuce:1.49, spinach:1.89, squash:1.44]。
		 * <listing>
var vegetables:Array = new Array();
vegetables.push(new Vegetable("lettuce", 1.49));
vegetables.push(new Vegetable("spinach", 1.89));
vegetables.push(new Vegetable("asparagus", 3.99));
vegetables.push(new Vegetable("celery", 1.29));
vegetables.push(new Vegetable("squash", 1.44));

trace(vegetables);
// lettuce:1.49, spinach:1.89, asparagus:3.99, celery:1.29, squash:1.44

vegetables.sort();

trace(vegetables);
// asparagus:3.99, celery:1.29, lettuce:1.49, spinach:1.89, squash:1.44

//The following code defines the Vegetable class
class Vegetable {
    private var name:String;
    private var price:Number;

    public function Vegetable(name:String, price:Number) {
        this.name = name;
        this.price = price;
    }

    public function toString():String {
        return " " + name + ":" + price;
    }
}
		 * </listing>
		 * 下例与前一个示例几乎完全相同，唯一不同的是将 sort() 方法与自定义排序函数 (sortOnPrice) 一起使用，该函数按 price 排序，而不是按字母顺序排序。请注意，新函数 getPrice() 将提取 price。
<listing>
var vegetables:Array = new Array();
vegetables.push(new Vegetable("lettuce", 1.49));
vegetables.push(new Vegetable("spinach", 1.89));
vegetables.push(new Vegetable("asparagus", 3.99));
vegetables.push(new Vegetable("celery", 1.29));
vegetables.push(new Vegetable("squash", 1.44));

trace(vegetables);
// lettuce:1.49, spinach:1.89, asparagus:3.99, celery:1.29, squash:1.44

vegetables.sort(sortOnPrice);

trace(vegetables);
// celery:1.29, squash:1.44, lettuce:1.49, spinach:1.89, asparagus:3.99

function sortOnPrice(a:Vegetable, b:Vegetable):Number {
    var aPrice:Number = a.getPrice();
    var bPrice:Number = b.getPrice();

    if(aPrice &gt; bPrice) {
        return 1;
    } else if(aPrice &lt; bPrice) {
        return -1;
    } else  {
        //aPrice == bPrice
        return 0;
    }
}

// The following code defines the Vegetable class and should be in a separate package.
class Vegetable {
    private var name:String;
    private var price:Number;

    public function Vegetable(name:String, price:Number) {
        this.name = name;
        this.price = price;
    }

    public function getPrice():Number {
        return price;
    }

    public function toString():String {
        return " " + name + ":" + price;
    }
}
</listing>
		*
		* 下面的代码创建 Array 对象 numbers，其中包含元素 [3,5,100,34,10]。调用 sort() 时如果不带任何参数，将按照字母顺序进行排序，生成不需要的结果 [10,100,3,34,5]。要对数值进行排序，必须将常量 NUMERIC 传递给 sort() 方法，该方法按以下方式对 numbers 进行排序：[3,5,10,34,100]。
<p><b>注意：</b>sort() 函数的默认行为是将每个实体作为字符串处理。如果使用 Array.NUMERIC 参数，则 Flash 运行时会出于排序目的尝试将任何非数字值转换为整数。如果转换失败，运行时将引发错误。例如，运行时可成功地将字符串值 "6" 转换成一个整数，但如果在转换过程中遇到字符串值 "six"，则将引发错误。</p>
<listing>
var numbers:Array = new Array(3,5,100,34,10);

trace(numbers); // 3,5,100,34,10
numbers.sort();
trace(numbers); // 10,100,3,34,5
numbers.sort(Array.NUMERIC);
trace(numbers); // 3,5,10,34,100

</listing>
*/
		AS3 native function sort(...args) : Array ;

		AS3 native function sortOn(fieldName:Object, options:Object = null):Array ;
		
		/**
		 * 返回一个字符串，它表示指定数组中的元素。数组中的每一个元素（从索引 0 开始到最高索引结束）均会转换为一个连接字符串，并以逗号分隔。要指定自定义的分隔符，请使用 Array.join() 方法。
		 * @return  数组元素的字符串。
		 */
		//AS3 native function toString():String ;

	}
}