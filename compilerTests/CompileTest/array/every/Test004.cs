using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.array.every
{
	[TestClass]
	public sealed class Test004 : CodeTestBase
	{
		protected override TestCodeProject LoadProject()
		{
			TestCodeProject project = new TestCodeProject();

			project.libs = [Juice_GlobalSwc];

			project.testCodes = new List<TestCodeFile>();

			project.testCodes.Add(
				new TestCodeFile()
				{
					Path = "Main.as",
					Code = @"
package 
{
	import flash.display.Sprite;
	
	[Doc]
	public class Main extends Sprite
	{
	}

}

function assertSameValue(a, b, msg) {
	if ((a !== a && b !== b) || a === b) {
		return;
	}
	throw new Error(msg || 'expected SameValue(' + a + ', ' + b + ')');
}

function pass(msg) {
	trace(msg || 'OK');
}

var arr:Array = new Array();
arr[0] = 1;
arr[5] = 2;
arr[10] = 3;

var res = arr.every(function(element, index, arr):Boolean {
	return element is Number;
});
assertSameValue(res, false, 'sparse array with holes');

var callCount = 0;
var arr2 = [1, 2, 3, 4, 5];
var res2 = arr2.every(function(element, index, arr):Boolean {
	callCount++;
	if (element === 3) {
		arr2[0] = 100;
	}
	return element < 10;
});
assertSameValue(callCount, 5, 'modify during iteration');
assertSameValue(arr2[0], 100, 'element modified');

var delCount = 0;
var arr3 = [1, 2, 3, 4, 5];
var res3 = arr3.every(function(element, index, arr):Boolean {
	delCount++;
	if (index === 2) {
		delete arr[1];
	}
	return true;
});
assertSameValue(delCount, 5, 'delete during iteration');

var arr4 = [1, undefined, 3, null, 5];
var res4 = arr4.every(function(element, index, arr):Boolean {
	return element !== undefined && element !== null;
});
assertSameValue(res4, false, 'undefined/null elements');

var arr5 = [1, 2, 3];
arr5[10] = 100;
var res5 = arr5.every(function(element, index, arr):Boolean {
	return true;
});
assertSameValue(res5, true, 'out of bounds access');
assertSameValue(arr5.length, 11, 'length updated');

var arr6 = [1, 2, 3];
var res6 = arr6.every(function(element, index, arr):Boolean {
	return element;
});
assertSameValue(res6, true, 'non-boolean truthy return');

var arr7 = [1, 0, 3];
var res7 = arr7.every(function(element, index, arr):Boolean {
	return element;
});
assertSameValue(res7, false, 'non-boolean with zero');

var earlyCount = 0;
var arr8 = [1, 2, 3, 4, 5];
var res8 = arr8.every(function(element, index, arr):Boolean {
	earlyCount++;
	return element < 3;
});
assertSameValue(earlyCount, 3, 'early return on false');
assertSameValue(res8, false, 'early return result');

pass('boundary tests passed');
"
				}
				);

			return project;
		}

		protected override void TestIsPass(Player player, PlayerException ex)
		{
			player.ForceGC();
			{
				var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
				Assert.IsNotNull(global);
				var globalInstance = player.Context.GC.Heap[global.__global_index__];
				Assert.IsNotNull(globalInstance);
				Assert.IsNull(ex);

				StringPrint print = (StringPrint)player.Print;

				Assert.AreEqual("boundary tests passed\r\n", print.GetOutput());
			}
		}

		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}