using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.array.every
{
	[TestClass]
	public sealed class Test003 : CodeTestBase
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

function assert(mustBeTrue, message = undefined) {
	if (mustBeTrue === true) {
		return;
	}
	throw new Error(message || 'assertion failed');
}

function assertEqual(a, b, msg) {
	if (a !== b) {
		throw new Error(msg || 'expected ' + b + ' but got ' + a);
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

var calls = 0;

var arr1 = [1, 2, 4];
var res1 = arr1.every(isNumeric);
assertSameValue(res1, true, 'Test1');

var arr2 = [1, 2, 'ham'];
var res2 = arr2.every(isNumeric);
assertSameValue(res2, false, 'Test2');

var arr3 = [];
var res3 = arr3.every(isNumeric);
assertSameValue(res3, true, 'Test3 empty array');

var arr4 = [1, 3, 5, 7, 9];
var res4 = arr4.every(greaterThanZero);
assertSameValue(res4, true, 'Test4 all > 0');

var arr5 = [1, -1, 3];
var res5 = arr5.every(greaterThanZero);
assertSameValue(res5, false, 'Test5 one negative');

var arr6 = [10, 20, 30];
var res6 = arr6.every(greaterThan(15));
assertSameValue(res6, false, 'Test6 all > 15');

function isNumeric(element, index, arr) {
	return element is Number;
}

function greaterThanZero(element, index, arr) {
	return element > 0;
}

function greaterThan(threshold) {
	return function(element, index, arr) {
		return element > threshold;
	};
}

pass('all tests passed');
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

				Assert.AreEqual("all tests passed\r\n", print.GetOutput());
			}
		}

		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}