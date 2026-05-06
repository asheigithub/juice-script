using juicescript;
using juicescript.ABC;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.gc
{
    [TestClass]
    public class TestGC001 : CodeTestBase
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
	public class Main
	{
		
		//public static var BBB =  7;
		public function Main() 
		{
			
		}
	}
}

class a
{
	public function a()
	{
		new Object();
		new Object();
		new Object();
	}
}

new a();
"
                }


                );


            return project;

        }

        protected override void TestIsPass(Player player, PlayerException ex)
        {
            player.ForceGC();
            Assert.IsNull(ex);

            Assert.AreEqual( 1,  player.Context.GC.Heap.DumpHeap()
                .Where( o=> o.Kind == RtHeapTypeKind.CLASS && ((RtScriptClass)o).Meta.QName.Name == "a" ).Count());

            ASClass @class = (ASClass)((RtScriptClass)player.Context.GC.Heap.DumpHeap().First
                (o => o.Kind == RtHeapTypeKind.CLASS && ((RtScriptClass)o).Meta.QName.Name == "a")).Meta;


            var objList = player.Context.GC.Heap.DumpHeap().Where(
                o => o.Kind == RtHeapTypeKind.INSTANCE && o.Type == @class.Instance
                //&&
                //!((RtPayloadInstance)o.facility).isCache
                );

            Assert.AreEqual( 0,objList.Count() );

        }

        [TestMethod]
        public void Test()
        {
            Run();
        }

    }
}
