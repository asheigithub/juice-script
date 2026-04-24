using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.array.filter
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

var arr16 = [];
for (var i = 0; i < 16; i++) arr16.push(i);
var res16 = arr16.filter(function(e) { return e % 2 == 0; });
if (res16.length !== 8) throw new Error('16 elements');

var arr17 = [];
for (i = 0; i < 17; i++) arr17.push(i);
var res17 = arr17.filter(function(e) { return true; });
if (res17.length !== 17) throw new Error('17 elements');

var arr100 = [];
for (i = 0; i < 100; i++) arr100.push(i);
var res100 = arr100.filter(function(e) { return e % 2 == 0; });
if (res100.length !== 50) throw new Error('100 elements');

var sparse = [];
sparse[0] = 1; sparse[5] = 2; sparse[10] = 3;
var sparseRes = sparse.filter(function(e) { return e is Number; });
if (sparseRes.length !== 3) throw new Error('sparse');

var obj = {min: 3};
var arr = [1, 2, 3, 4, 5];
var thisRes = arr.filter(function(e) { return e > this.min; }, obj);
if (thisRes.length !== 2 || thisRes[0] !== 4) throw new Error('thisObject');

var origArr = [1, 2, 3];
var origLen = origArr.length;
var modRes = origArr.filter(function(e, i, a) {
	if (i === 1) a[0] = 100;
	return true;
});
if (origArr[0] !== 100) throw new Error('modify original');

var nested = [[1], [2], [3]];
var nestedRes = nested.filter(function(e) { return e is Array; });
if (nestedRes.length !== 3) throw new Error('nested');

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