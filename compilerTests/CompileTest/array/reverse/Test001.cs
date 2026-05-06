using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.array.reverse
{
	[TestClass]
	public sealed class Test001 : CodeTestBase
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
	/**
	 * ...
	 * @author 
	 */
	public class BaseM extends Sprite
	{
		
		public static const FFF = 6666;
		protected static const VVV = ""abcd"";
		public function BaseM() 
		{
			
		}
		
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
	/**
	 * ...
	 * @author 
	 */
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

assert._toString = function (v:String) 
{
	return v;
}

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
    return;
  }

  if (message === undefined) {
    message = '';
  } else {
    message += ' ';
  }

  message += 'Expected SameValue(«' + assert._toString(actual) + '», «' + assert._toString(expected) + '») to be true';

  throw new Test262Error(message);
};

var x = new Array();
if (x.reverse().length !== 0) {
  throw new Test262Error('#1: x = new Array(); x.reverse().length !== 0');
}

x = [1];
var r1 = x.reverse();
if (r1[0] !== 1) {
  throw new Test262Error('#2: x = [1]; r.reverse()[0] !== 1');
}

x = [1, 2];
var r2 = x.reverse();
if (r2[0] !== 2 || r2[1] !== 1) {
  throw new Test262Error('#3: x = [1,2]; r.reverse()');
}

x = [1, 2, 3];
var r3 = x.reverse();
if (r3[0] !== 3 || r3[1] !== 2 || r3[2] !== 1) {
  throw new Test262Error('#4: x = [1,2,3]; r.reverse()');
}

var arr = [1, 2, 3];
var reversed = arr.reverse();
if (reversed !== arr) {
  throw new Test262Error('#7: arr.reverse() should return same reference');
}

if (arr[0] !== 3 || arr[1] !== 2 || arr[2] !== 1) {
  throw new Test262Error('#8: reverse should modify original array');
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