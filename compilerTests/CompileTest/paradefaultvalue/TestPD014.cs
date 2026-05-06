using juicescript.runtime;
using juicescript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using juicescript.compiler;

namespace compilerTests.CompileTest.paradefaultvalue
{
	[TestClass]
	public sealed class TestPD014 : CodeTestBase
	{
		protected override TestCodeProject LoadProject()
		{
			TestCodeProject project = new TestCodeProject();

			project.libs = [Juice_GlobalSwc];

			project.testCodes = new List<TestCodeFile>();

			project.testCodes.Add(
				new TestCodeFile()
				{ 
					Path = "ns1/BaseM.as",
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
		protected static const VVV = 'abcd';
		
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
		
	}
}

class C extends Main
{
	public function B(k= VVV + 'cccc' + null)
	{
		
		o = k;
	}
}

var c:C = new C();

c.B();


var o;
var p;
var q;

//trace(o);


"
				}


				);


			return project;
		}

		protected override void TestIsPass(Player player, PlayerException ex)
		{
			player.ForceGC();

			var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
			Assert.IsNotNull(global);
			var globalInstance = player.Context.GC.Heap[global.__global_index__];
			Assert.IsNotNull(globalInstance);
			Assert.IsNull(ex);

			NaNBoxing o = ((RtScriptClass)globalInstance).ReadSlot(1);
			Assert.AreEqual(NaNBoxing.BoxType.HeapPtr, o.ValueType);
			Assert.AreEqual( RtHeapTypeKind.STRING , player.Context.GC.Heap[o.HeapPtr].Kind );
			Assert.AreEqual("abcdccccnull", ((RtString)player.Context.GC.Heap[o.HeapPtr]).Str );


		}

		[TestMethod]
		public void Test()
		{

			Run();


		}
	}
}
