using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.array.some
{
	[TestClass]
	public sealed class Test019 : CodeTestBase
	{
		protected override TestCodeProject LoadProject()
		{
			TestCodeProject project = new TestCodeProject();

			project.libs = [Juice_GlobalSwc];

			project.testCodes = new List<TestCodeFile>();

			project.testCodes.Add(
				new TestCodeFile()
				{
					Path = "BaseM.as",
					Code = @"
package ns1 
{
	import flash.display.Sprite;
	public class BaseM extends Sprite
	{
		public function BaseM() {}
	}
}


"
				}
				);

			project.testCodes.Add(
				new TestCodeFile()
				{
					Path = "Main.as",
					Code = @"
package 
{
	import flash.display.Sprite;
	import ns1.BaseM;
	
	[Doc]
	public class Main extends BaseM
	{
		public var v;
	}
	
}


class Test262Error extends Error
{
	var a;
	public function Test262Error(t=undefined)
	{
		super(t);
	}
}

function assert(mustBeTrue, message = undefined) {
  if (mustBeTrue === true) {
    return;
  }
  if (message === undefined) {
    message = 'Expected true but got ' + assert._toString(mustBeTrue);
  }
  throw new Test262Error(message);
}

assert._toString = function (v:String) { return v; }

assert._isSameValue = function (a, b) {
  if (a === b) {
    return a !== 0 || 1 / a === 1 / b;
  }
  return a !== a && b !== b;
};

assert.sameValue = function (actual, expected, message) {
  try {
    if (assert._isSameValue(actual, expected)) {
      return;
    }
  } catch (error) {
    throw new Test262Error(message + ' (_isSameValue operation threw) ' + error);
  }
  if (message === undefined) {
    message = '';
  } else {
    message += ' ';
  }
  message += 'Expected SameValue(«' + assert._toString(actual) + '», «' + assert._toString(expected) + '») to be true';
  throw new Test262Error(message);
};

var arr = [1, 2, 3, 4, 5];
if (arr.some(function(x:int):Boolean { return x > 3; }) !== true) {
  throw new Test262Error('#1: some with matching element');
}

arr = [1, 2, 3, 4, 5];
if (arr.some(function(x:int):Boolean { return x > 10; }) !== false) {
  throw new Test262Error('#2: some with no matching element');
}

arr = [];
if (arr.some(function(x:int):Boolean { return x > 0; }) !== false) {
  throw new Test262Error('#3: empty array');
}

arr = [1, 2, 3];
var checked = 0;
var result = arr.some(function(element:int, index:int, a:Array):Boolean {
  checked++;
  return element == 2 && index == 1 && a.length == 3;
});
if (result !== true) {
  throw new Test262Error('#4: callback args');
}
if (checked !== 2) {
  throw new Test262Error('#4: checked count');
}

arr = [0, 1, 2];
if (arr.some(function(x:int):Boolean { return x > 0; }) !== true) {
  throw new Test262Error('#5: with falsy 0');
}

arr = [-1, -2, -3];
if (arr.some(function(x:int):Boolean { return x > 0; }) !== false) {
  throw new Test262Error('#6: all negative');
}

arr = [undefined, undefined];
if (arr.some(function(x:*):Boolean { return x !== undefined; }) !== false) {
  throw new Test262Error('#7: only undefined');
}

arr = [undefined, 1, undefined];
result = arr.some(function(x:*):Boolean { return x === 1; });
if (result !== true) {
  throw new Test262Error('#8: with undefined and match');
}

arr = [null, null, 1];
result = arr.some(function(x:*):Boolean { return x === 1; });
if (result !== true) {
  throw new Test262Error('#9: with null');
}

arr = [1, 2, 3, 4, 5];
var found = false;
result = arr.some(function(x:int):Boolean {
  found = true;
  return x == 3;
});
if (result !== true || found !== true) {
  throw new Test262Error('#10: early return');
}

arr = [1, 2, 3];
var outerVar = 0;
result = arr.some(function(x:int):Boolean {
  outerVar = x * 2;
  return x == 2;
});
if (outerVar !== 4) {
  throw new Test262Error('#11: outer var');
}

trace('OK');

"
				}
				);

			return project;
		}

		protected override void TestIsPass(Player player, PlayerException ex)
		{
			player.ForceGC();
			if (ex != null)
			{
				Console.WriteLine("Exception: " + ex.Message);
				Console.WriteLine(ex.StackTrace);
			}
			{
				var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
				Assert.IsNotNull(global);
				var globalInstance = player.Context.GC.Heap[global.__global_index__];
				Assert.IsNotNull(globalInstance);
				Assert.IsNull(ex, ex?.Message);

				RtScriptClass rtPayload = (RtScriptClass)globalInstance;

				StringPrint print = (StringPrint)player.Print;

				Assert.AreEqual("OK\r\n", print.GetOutput());
			}
		}

		[TestMethod]
		public void Test()
		{
			base.Run();
		}
	}
}