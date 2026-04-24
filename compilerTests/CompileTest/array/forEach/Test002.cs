using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.array.forEach
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

function pass(msg) {
	trace(msg || 'OK');
}

var sparseArr:Array = [];
sparseArr[0] = 1;
sparseArr[5] = 2;
sparseArr[10] = 3;

var sparseCount = 0;
sparseArr.forEach(function(e, i, a) {
	sparseCount++;
});
if (sparseCount !== 11) {
	throw new Error('sparse count wrong: ' + sparseCount);
}

var arr = [1, 2, 3];
var modCount = 0;
arr.forEach(function(e, i, a) {
	modCount++;
	if (i === 1) a[0] = 100;
});
if (modCount !== 3) {
	throw new Error('mod count wrong');
}
if (arr[0] !== 100) {
	throw new Error('element not modified');
}

var obj = {threshold: 5};
var thresholdArr = [1, 2, 3];
var thisTests = [];
thresholdArr.forEach(function(e, i, a) {
	thisTests.push(this.threshold);
}, obj);
if (thisTests[0] !== 5 || thisTests[1] !== 5 || thisTests[2] !== 5) {
	throw new Error('thisObject not passed');
}

var indexArr = [10, 20, 30];
var indices = [];
indexArr.forEach(function(e, i, a) {
	indices.push(i);
});
if (indices[0] !== 0 || indices[1] !== 1 || indices[2] !== 2) {
	throw new Error('index not passed');
}

var arrRefArr = [1, 2, 3];
var arrRefs = [];
arrRefArr.forEach(function(e, i, a) {
	arrRefs.push(a === arrRefArr);
});
if (arrRefs[0] !== true || arrRefs[1] !== true || arrRefs[2] !== true) {
	throw new Error('array reference not passed');
}

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