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
    public class TestCallFun8 : CodeTestBase
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


var a;
var c;
(function ():void   
{
	a = arguments;
	c = arguments.callee;
})(1,2,3);

c(6,7);



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
            
            NaNBoxing a = rtPayload.ReadSlot(0);
            Assert.AreEqual(NaNBoxing.BoxType.HeapPtr, a.ValueType);

            RtHeapBase instance = player.Context.GC.Heap[a.HeapPtr];
            Assert.AreEqual(RtHeapTypeKind.ARRAY, instance.TypeKind);
            Assert.AreEqual(RtArray.ArrayStoreMode.normal, ((RtArray)instance).StoreMode);

            Assert.AreEqual(2u, ((RtArray)instance).GetLength(player));
            bool isoutofindex;
            Assert.AreEqual(NaNBoxing.BoxType.Sbyte, ((RtArray)instance).ReadSlot(0,player,out isoutofindex).ValueType);
            Assert.AreEqual(6, ((RtArray)instance).ReadSlot(0,player, out isoutofindex).SByteValue);

            Assert.AreEqual(NaNBoxing.BoxType.Sbyte, ((RtArray)instance).ReadSlot(1,player, out isoutofindex).ValueType);
            Assert.AreEqual(7, ((RtArray)instance).ReadSlot(1, player, out isoutofindex).SByteValue);



        }


        [TestMethod]
        public void Test()
        {
            Run();
        }

    }
}
