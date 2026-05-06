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
    public class TestCallFun6 : CodeTestBase
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
		
		AS3 var F:Function = function (i:int):void 
		{
			o = i;
			t = this;
		} ;
		
		//public static var BBB =  7;
		public function Main() 
		{
			//F(1);
		}
		
		public function ABC()
		{
			
		}
		
		
		public var j:int ;
		
		public var k:Namespace;
		
		
		
	}
}

var o;
var t;
new Main().F(3);




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
            Assert.AreEqual(NaNBoxing.BoxType.Int, o.ValueType);
            Assert.AreEqual(3, o.IntValue);

            NaNBoxing t = rtPayload.ReadSlot(1);
            Assert.AreEqual(NaNBoxing.BoxType.HeapPtr, t.ValueType);



            Assert.AreEqual( RtHeapTypeKind.INSTANCE, player.Context.GC.Heap[t.HeapPtr].TypeKind);
            Assert.AreEqual( "Main", player.Context.GC.Heap[t.HeapPtr].Type.QName.Name);



        }


        [TestMethod]
        public void Test()
        {
            Run();
        }

    }
}
