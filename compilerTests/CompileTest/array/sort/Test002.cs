using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.array.sort
{
	[TestClass]
	public sealed class Test002 : CodeTestBase
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

var x = new Array(2);
x[1] = 1;
x.sort();

if (x.length !== 2) {
  throw new Test262Error('#1: var x = new Array(2); x[1] = 1;  x.sort(); x.length === 2. Actual: ' + (x.length));
}

if (x[0] !== 1) {
  throw new Test262Error('#2: var x = new Array(2); x[1] = 1;  x.sort(); x[0] === 1. Actual: ' + (x[0]));
}

if (x[1] !== undefined) {
  throw new Test262Error('#3: var x = new Array(2); x[1] = 1;  x.sort(); x[1] === undefined. Actual: ' + (x[1]));
}

var x = new Array(2);
x[0] = 1;
x.sort();

if (x.length !== 2) {
  throw new Test262Error('#4: var x = new Array(2); x[0] = 1;  x.sort(); x.length === 2. Actual: ' + (x.length));
}

if (x[0] !== 1) {
  throw new Test262Error('#5: var x = new Array(2); x[0] = 1;  x.sort(); x[0] === 1. Actual: ' + (x[0]));
}

if (x[1] !== undefined) {
  throw new Test262Error('#6: var x = new Array(2); x[0] = 1;  x.sort(); x[1] === undefined. Actual: ' + (x[1]));
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