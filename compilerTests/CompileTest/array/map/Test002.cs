using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.array.map
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
var res16 = arr16.map(function(e) { return e * 2; });
if (res16.length !== 16) throw new Error('16 elements');

var arr17 = [];
for (i = 0; i < 17; i++) arr17.push(i);
var res17 = arr17.map(function(e) { return e; });
if (res17.length !== 17) throw new Error('17 elements');

var arr100 = [];
for (i = 0; i < 100; i++) arr100.push(i);
var res100 = arr100.map(function(e) { return e * 2; });
if (res100.length !== 100 || res100[99] !== 198) throw new Error('100 elements');

var sparse = [];
sparse[0] = 1; sparse[5] = 2; sparse[10] = 3;
var sparseRes = sparse.map(function(e) { return (e is Number) ? e * 2 : 0; });
if (sparseRes.length !== 11) throw new Error('sparse length');
if (sparseRes[0] !== 2 || sparseRes[5] !== 4 || sparseRes[10] !== 6) throw new Error('sparse elements');

var obj = {mult: 3};
var arr = [1, 2, 3, 4, 5];
var thisRes = arr.map(function(e) { return e * this.mult; }, obj);
if (thisRes[0] !== 3 || thisRes[1] !== 6 || thisRes[2] !== 9 || thisRes[3] !== 12 || thisRes[4] !== 15) throw new Error('thisObject');

var nested = [1, 2, 3];
var nestedRes = nested.map(function(e) { return [e, e * 2]; });
if (nestedRes.length !== 3) throw new Error('nested length');
if (nestedRes[0][0] !== 1 || nestedRes[0][1] !== 2) throw new Error('nested elements');

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