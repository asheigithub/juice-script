using juicescript.runtime;
using juicescript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests.CompileTest.proto
{
    [TestClass]
    public sealed class Test014 : CodeTestBase
    {
        protected override TestCodeProject LoadProject()
        {
            TestCodeProject project = new TestCodeProject();

            project.libs = [Juice_GlobalSwc];

            project.testCodes = new List<TestCodeFile>();
            
            project.testCodes.Add(
                new TestCodeFile()
                {
                    Path = "BaseM.as",
                    Code = @"
package ns1 
{
	import flash.display.Sprite;
	/**
	 * ...
	 * @author 
	 */
	public class BaseM extends Sprite
	{
		
		public static const FFF = 6666;
		protected static const VVV = ""abcd"";
		public function BaseM() 
		{
			
		}
		
	}

}


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
	import ns1.BaseM;
	
	[Doc]
	/**
	 * ...
	 * @author 
	 */
	public class Main extends BaseM
	{
		public var v;
	}
	
}


var a = new Vector.< Vector.< int>>()
a.constructor.prototype.LL = ""Vector"";


trace(a.LL);
trace( a[""LL""] );
trace( a.constructor );

try
{
trace( a.AS3::LL );
}
catch(e)
{
	trace(e.message);
}



"
				}


                );


            return project;
        }

        protected override void TestIsPass(Player player, PlayerException ex)
        {
           player.ForceGC();
            {
                var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
                Assert.IsNotNull(global);
                var globalInstance = player.Context.GC.Heap[global.__global_index__];
                Assert.IsNotNull(globalInstance);
                Assert.IsNull(ex);

                RtScriptClass rtPayload = (RtScriptClass)globalInstance.facility;

				StringPrint print = (StringPrint)player.Print;

				Assert.AreEqual("Vector\r\nVector\r\n[class __AS3__.vec::Vector<__AS3__.vec::Vector.<int>>]\r\nProperty http://adobe.com/AS3/2006/builtin::LL not found on __AS3__.vec::Vector<__AS3__.vec::Vector.<int>> and there is no default value.\r\n", print.GetOutput());

				NaNBoxing a = rtPayload.ReadSlot(0);
				Assert.AreEqual( NaNBoxing.BoxType.HeapPtr, a.ValueType );
				Assert.AreEqual( RtHeapTypeKind.VECTOR , player.Context.GC.Heap[a.HeapPtr].TypeKind );
				
			}

           
        }


        [TestMethod]
        public void Test()
        {
            Run();
        }
    }
}
