using juicescript;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.returnvalue
{
	[TestClass]
	public class TestR007 : CodeTestBase
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

var o;

function v():Namespace
{
	return AS3;
}

class A
{
	function F():void 
	{
		o = 1;
	}
	
	AS3 function F():void
	{
		o = 2;	
	}	
	
	AS3 var K:String = ""F"";
	
	function exec()
	{
		(new A())[v()::[""K""]];
	}
	
}
new A().exec();

"
				}


				);


			return project;
		}

		protected override void TestIsPass(Player player, PlayerException ex)
		{
			
			Assert.IsNotNull( ex );

			Assert.AreEqual(NaNBoxing.BoxType.HeapPtr, ex.error.ValueType);

			var err = player.Context.GC.Heap[ex.error.HeapPtr];

			Assert.AreEqual(RtHeapTypeKind.INSTANCE, err.TypeKind);

			Assert.AreEqual("TypeError", err.Type.QName.Name);

			var errinstance = (RtPayloadInstance)err.facility;

			var message = errinstance.ReadSlot(0, err.Type._link_codescope, player);

			Assert.AreEqual(NaNBoxing.BoxType.HeapPtr, message.ValueType);

			var message_obj = player.Context.GC.Heap[message.HeapPtr];

			Assert.AreEqual(RtHeapTypeKind.STRING, message_obj.TypeKind);

			Assert.AreEqual("F is ambiguous; Found more than one matching binding.", ((RtPayloadString)message_obj.facility).Str);


			player.ForceGC();


		}


		[TestMethod]
		public void Test()
		{
			Run();
		}
	}
}
