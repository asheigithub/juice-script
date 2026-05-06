using juicescript;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.callfun
{
    [TestClass]
    public class TestCallFun18 : CodeTestBase
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
	
	//use namespace AS3;
	import flash.display.Sprite;
	import ns1.Class2;
	import ns1.TNS;
	[Doc]
	public class Main extends Sprite
	{
		
		
		
		//public static var BBB =  7;
		public function Main() 
		{
			Main.public::F();
		}
		
		public static var F = function ():void 
		{
		o = this;
		}
		
		;
		
		
		
		
	}
}

new Main();
var o;


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

            RtScriptClass rtPayload = (RtScriptClass)globalInstance;
            
            NaNBoxing o = rtPayload.ReadSlot(0);
            Assert.AreEqual(NaNBoxing.BoxType.HeapPtr, o.ValueType);

            RtHeapBase instance = player.Context.GC.Heap[o.HeapPtr];
            Assert.AreEqual(RtHeapTypeKind.CLASS, instance.TypeKind);
            Assert.AreEqual("Main", ((RtScriptClass)instance).Meta.QName.Name);
            
        }


        [TestMethod]
        public void Test()
        {
            Run();
        }

    }
}
