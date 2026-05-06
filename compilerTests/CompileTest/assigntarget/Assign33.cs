using juicescript.runtime;
using juicescript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using juicescript.compiler;

namespace compilerTests.CompileTest.assigntarget
{
    [TestClass]
    public sealed class Assign33 : CodeTestBase
    {
        protected override TestCodeProject LoadProject()
        {
            TestCodeProject project = new TestCodeProject();

            project.libs = [Juice_GlobalSwc];

            project.testCodes = new List<TestCodeFile>();

			project.testCodes.Add(
				new TestCodeFile()
				{
					Path = "ns1/A.as",
					Code = @"
package ns1 
{
	import flash.display.Sprite;
	/**
	 * ...
	 * @author 
	 */
	public class A extends B
	{
		internal var APP = 8;
		public function A() 
		{
			
		}
		
		public function Go(o:A)
		{
			return o.internal::[""BF""];
		}
		
	}

}

var k;
"
				}
				);


			project.testCodes.Add(
				new TestCodeFile()
				{
					Path = "ns1/B.as",
					Code = @"
package ns1 
{
	import flash.display.Sprite;
	/**
	 * ...
	 * @author 
	 */
	public class B extends Sprite
	{
		
		public function B() 
		{
			
		}
		
		public function BF()
		{
			o = 6;
		}
		
		internal function BF()
		{
			o = 5;
		}
		
	}

}

var o;
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
	import ns1.A;
	import ns1.B;
	[Doc]
	/**
	 * ...
	 * @author 
	 */
	public class Main extends A
	{
		public function Test()
		{
			//o = this.internal::[""APP""];
			o = new A().Go(new Main());
		}
	}
}

new Main().Test();

var o;
o();

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

			RtScriptClass rtPayload = (RtScriptClass)globalInstance.facility;

			NaNBoxing o = rtPayload.ReadSlot(0);
			Assert.AreEqual(NaNBoxing.BoxType.HeapPtr, o.ValueType);


			var globalB = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "B");
			Assert.IsNotNull(globalB);
			var globalBInstance = player.Context.GC.Heap[globalB.__global_index__];
			Assert.IsNotNull(globalBInstance);

			RtScriptClass rtPayloadB = (RtScriptClass)globalBInstance.facility;

			NaNBoxing o2 = rtPayloadB.ReadSlot(0);
			Assert.AreEqual(NaNBoxing.BoxType.Sbyte, o2.ValueType);
			Assert.AreEqual(5, o2.SByteValue);


			var globalA = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "A");
			Assert.IsNotNull(globalA);
			var globalAInstance = player.Context.GC.Heap[globalA.__global_index__];
			Assert.IsNotNull(globalAInstance);

			RtScriptClass rtPayloadA = (RtScriptClass)globalAInstance.facility;

			NaNBoxing k = rtPayloadA.ReadSlot(0);
			Assert.AreEqual(NaNBoxing.BoxType.Undefined, k.ValueType);
			
		}


		[TestMethod]
        public void Test()
        {
			
			Run();
			
			
        }
    }
}
