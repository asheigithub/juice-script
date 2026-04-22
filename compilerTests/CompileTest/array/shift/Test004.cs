using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.array.shift
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
	[Doc]
	public class Main
	{
		public function Main()
		{
		}
	}
}

var x = new Array(1, 2, 3);
var shift = x.shift();
if (shift !== 1) {
	throw new Error('#1: new Array(1,2,3).shift() === 1. Actual: ' + shift);
}

if (x.length !== 2) {
	throw new Error('#2: after shift length === 2. Actual: ' + x.length);
}

if (x[0] !== 2) {
	throw new Error('#3: x[0] === 2. Actual: ' + x[0]);
}

if (x[1] !== 3) {
	throw new Error('#4: x[1] === 3. Actual: ' + x[1]);
}

var x2 = new Array([1,2], [3,4], [5,6]);
var shift2 = x2.shift();
if (shift2[0] !== 1 || shift2[1] !== 2) {
	throw new Error('#5: nested array shift [1,2]. Actual: ' + shift2[0] + ',' + shift2[1]);
}

if (x2.length !== 2) {
	throw new Error('#6: nested array length === 2. Actual: ' + x2.length);
}

var x3 = new Array();
for (var i:int = 0; i < 20; i++) {
	x3.push(i);
}
var shift3 = x3.shift();
if (shift3 !== 0) {
	throw new Error('#8: normal storage shift === 0. Actual: ' + shift3);
}

if (x3.length !== 19) {
	throw new Error('#9: normal storage length === 19. Actual: ' + x3.length);
}

if (x3[0] !== 1) {
	throw new Error('#10: normal storage x[0] === 1. Actual: ' + x3[0]);
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
			{
				var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
				Assert.IsNotNull(global);
				var globalInstance = player.Context.GC.Heap[global.__global_index__];
				Assert.IsNotNull(globalInstance);
				Assert.IsNull(ex);

				StringPrint print = (StringPrint)player.Print;

				Assert.AreEqual("OK\r\n", print.GetOutput());
			}
		}

		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}