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
	public sealed class TestMemberInit002 : CodeTestBase
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
		
		public static var LLM = G2;		
		
		public  static const K = B;
		public  static const B = G1 ;
	}
}

const G1 = ""hjk"";

function a():void 
{
	var C = B;
	
	const B = 666;
	
	//trace(""11"",C);
	
	o = Main.LLM;
	p = Main.K;
	
}
a();

var o;


var GB = GG;
var GG = G2;

const G2 = int.MIN_VALUE;

var p;

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

			var K = ((RtScriptClass)clsInstance.facility).ReadSlot(1);
			var B = ((RtScriptClass)clsInstance.facility).ReadSlot(2);

			Assert.AreEqual(juicescript.NaNBoxing.BoxType.HeapPtr, K.ValueType);
			var Kinstance = player.Context.GC.Heap[K.HeapPtr];
			Assert.AreEqual(RtHeapTypeKind.STRING, Kinstance.TypeKind);
			Assert.AreEqual("hjk", ((RtString)Kinstance.facility).Str );

			Assert.AreEqual(K, B);

			var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
			Assert.IsNotNull(global);
			var globalInstance = player.Context.GC.Heap[global.__global_index__];
			Assert.IsNotNull(globalInstance);
			
			NaNBoxing o = ((RtScriptClass)globalInstance.facility).ReadSlot(2);
			Assert.AreEqual(NaNBoxing.BoxType.Int, o.ValueType);
			Assert.AreEqual(int.MinValue, o.IntValue);

			NaNBoxing GB = ((RtScriptClass)globalInstance.facility).ReadSlot(3);
			Assert.AreEqual(NaNBoxing.BoxType.Undefined, GB.ValueType);

			NaNBoxing GG = ((RtScriptClass)globalInstance.facility).ReadSlot(4);
			Assert.AreEqual(NaNBoxing.BoxType.Int, GG.ValueType);
			Assert.AreEqual(int.MinValue, GG.IntValue);

			NaNBoxing G2 = ((RtScriptClass)globalInstance.facility).ReadSlot(5);
			Assert.AreEqual(NaNBoxing.BoxType.Int, G2.ValueType);
			Assert.AreEqual(int.MinValue, G2.IntValue);




			NaNBoxing p = ((RtScriptClass)globalInstance.facility).ReadSlot(6);
			Assert.AreEqual(K, p);


			//throw new NotImplementedException();
		}




		[TestMethod]
		public void Test()
		{
			Run();

		}
	}
}
