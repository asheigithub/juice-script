using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.array.sort
{
	[TestClass]
	public sealed class Test008 : CodeTestBase
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

var alphabetR = [""z"", ""y"", ""x"", ""w"", ""v"", ""u"", ""t"", ""s"", ""r"", ""q"", ""p"", ""o"", ""n"", ""M"", ""L"", ""K"", ""J"", ""I"", ""H"", ""G"", ""F"", ""E"", ""D"", ""C"", ""B"", ""A""];
var alphabet = [""A"", ""B"", ""C"", ""D"", ""E"", ""F"", ""G"", ""H"", ""I"", ""J"", ""K"", ""L"", ""M"", ""n"", ""o"", ""p"", ""q"", ""r"", ""s"", ""t"", ""u"", ""v"", ""w"", ""x"", ""y"", ""z""];

var myComparefn = function(x, y) {
  var xS = String(x);
  var yS = String(y);
  if (xS < yS) return 1
  if (xS > yS) return -1;
  return 0;
}

alphabet.sort(myComparefn);
var result = true;
for (var i = 0; i < 26; i++) {
  if (alphabetR[i] !== alphabet[i]) result = false;
}

if (result !== true) {
  throw new Test262Error('#1: CHECK ENGLISH ALPHABET');
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

				RtPayloadScriptClass rtPayload = (RtPayloadScriptClass)globalInstance.facility;

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