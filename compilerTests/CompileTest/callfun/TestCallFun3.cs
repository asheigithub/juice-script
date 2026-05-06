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
    public class TestCallFun3 : CodeTestBase
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
	import ns1.Class2;
	import ns1.TNS;
	[Doc]
	public class Main extends Sprite
	{
		
		AS3 var F:int;
		
		public function Main() 
		{
			
		}
		
		public function ABC()
		{
			
		}
		
		
		public var j:int ;
		
		public var k:Namespace;
		
		
		
	}
}


var b:int;
var c:Array;
var d;
function a(i:Class,j:*,...a)
{
	j();
	
	b = j;
	c = a;
	
}

a(String, function(){
	d = this;
	
}, 2, 3, 4);

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
            
            NaNBoxing b = rtPayload.ReadSlot(0);
            Assert.AreEqual(NaNBoxing.BoxType.Int, b.ValueType);
            Assert.AreEqual(0, b.IntValue);

            NaNBoxing c = rtPayload.ReadSlot(1);
            Assert.AreEqual(NaNBoxing.BoxType.HeapPtr, c.ValueType);

            RtHeapBase arr = player.Context.GC.Heap[c.HeapPtr];
            Assert.AreEqual(RtHeapTypeKind.ARRAY, arr.TypeKind);

            RtArray array = (RtArray)arr;
            Assert.AreEqual(RtArray.ArrayStoreMode.normal, array.StoreMode);
            
            NaNBoxing d = rtPayload.ReadSlot(2);
            Assert.AreEqual(d.HeapPtr,  global.__global_index__ );

        }


        [TestMethod]
        public void Test()
        {
            Run();
        }

    }
}
