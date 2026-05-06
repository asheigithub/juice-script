using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using juicescript;

namespace compilerTests.CompileTest.memberinitvalue
{
	[TestClass]
	public sealed class TestMemberInit025 : CodeTestBase
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
	/**
	 * ...
	 * @author 
	 */
	public class Main extends Sprite
	{

	}
}

const G1 = ""hjk"";

function a() 
{
	//const mm = G2;
	return function () 
	{
		return function ():void 
		{
			var d = mm;
			
			o = d;
		}
		const mm = G2;
	}
	
};

a()()();
//trace(Main.LL);
//trace(o);
var o;


const G2 = int.MIN_VALUE;

//trace(o, p, q, r, s, t, w ,v);
"
				}


				);


			return project;
		}

		protected override void TestIsPass(Player player, PlayerException ex)
		{
			player.ForceGC();

			var cls = player.Context.libs.SelectMany(o => o.Classes).FirstOrDefault(o =>o !=null && o.QName.Name == "Main");
			Assert.IsNotNull(cls);
			var clsInstance = player.Context.GC.Heap[cls.__instance_index__];
			Assert.IsNotNull(clsInstance);
			Assert.IsNull(ex);

			
			var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
			Assert.IsNotNull(global);
			var globalInstance = player.Context.GC.Heap[global.__global_index__];
			Assert.IsNotNull(globalInstance);
			
			NaNBoxing o = ((RtScriptClass)globalInstance).ReadSlot(3);
			Assert.AreEqual(NaNBoxing.BoxType.Int, o.ValueType); //此处AIR 运行结果是undefined .但是实际上理应已经成功计算常量。
			Assert.AreEqual(int.MinValue, o.IntValue);


			

			//throw new NotImplementedException();
		}




		[TestMethod]
		public void Test()
		{
			Run();

		}
	}
}
