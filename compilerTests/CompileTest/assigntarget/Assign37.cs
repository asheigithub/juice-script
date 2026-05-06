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
    public sealed class Assign37 : CodeTestBase
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
			return o.internal::[""BF""] = APP;
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
		
		internal function set BF(i)
		{
			o = i;
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


"
				}


                );


            return project;
        }

        protected override void TestIsPass(Player player, PlayerException ex)
        {
			
			var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
			Assert.IsNotNull(global);
			var globalInstance = player.Context.GC.Heap[global.__global_index__];
			Assert.IsNotNull(globalInstance);
			Assert.IsNull(ex);

			NaNBoxing o = ((RtScriptClass)globalInstance.facility).ReadSlot(0);
			Assert.AreEqual(NaNBoxing.BoxType.Sbyte, o.ValueType);
			Assert.AreEqual(8, o.SByteValue);

		}

		[TestMethod]
        public void Test()
        {
			
			Run();
			
			
        }
    }
}
