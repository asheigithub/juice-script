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
	public sealed class TestMemberInit001 : CodeTestBase
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
		//public static const  PP:int = (sbyte.MIN_VALUE,  5 / 2);
		//public static var B = 6 / 2;

		public static var LL = LLM;
		public static var LLM = K;
		
		public static var V2 = new Object();
		
		public  static const K = B;
		public  static const B = ""abcdefg"";
	}
}

function a():void 
{
	var C = B;
	
	const B = 666;
	
	//trace(""11"",C);
}
a();

var o;
var p;
var q;

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

			var ll = ((RtPayloadScriptClass)clsInstance.facility).ReadSlot(0);
			Assert.AreEqual(NaNBoxing.BoxType.Undefined, ll.ValueType);

			var llm = ((RtPayloadScriptClass)clsInstance.facility).ReadSlot(1);
			Assert.AreEqual(juicescript.NaNBoxing.BoxType.HeapPtr, llm.ValueType);
			var llminstance = player.Context.GC.Heap[llm.HeapPtr];
			Assert.AreEqual(RtHeapTypeKind.STRING, llminstance.TypeKind);
			Assert.AreEqual("abcdefg", ((RtPayloadString)llminstance.facility).Str );

			var v2 = ((RtPayloadScriptClass)clsInstance.facility).ReadSlot(2);
			Assert.AreEqual(NaNBoxing.BoxType.HeapPtr, v2.ValueType);
			Assert.AreEqual(RtHeapTypeKind.INSTANCE, player.Context.GC.Heap[v2.HeapPtr].TypeKind);
			Assert.AreEqual(player.Context.OBJECT.Instance, player.Context.GC.Heap[v2.HeapPtr].Type);

			var K = ((RtPayloadScriptClass)clsInstance.facility).ReadSlot(3);
			var B = ((RtPayloadScriptClass)clsInstance.facility).ReadSlot(4);

			Assert.AreEqual(llm, K);
			Assert.AreEqual(llm, B);


			//throw new NotImplementedException();
		}




		[TestMethod]
		public void Test()
		{
			Run();

		}
	}
}
