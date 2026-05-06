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
    public sealed class Test001 : CodeTestBase
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


class O
{
	public function F()
	{
		
	}
}

var o = new O();

var c:Class = o.F.constructor;

var d = new c();
//trace(c,d);

o.F.constructor.prototype.VVV = 7;

//trace(o.F.VVV);

var v = new Vector.<int>();
v.constructor.prototype.VVV = 7;

//trace(v.VVV);

Function.prototype.VVV = 8;

var f = function ():void 
{
		
	function throwsomething(e)
	{
		throw e;
	}

	throwsomething.prototype = v;
	//throwsomething.VVV = 666;
	
	i = throwsomething.VVV;
	
	trace(  throwsomething.VVVV  );
};

f();

var g = o.F.VVV;
var h = v[""VVV""];
var i;

trace(c , d ,g, h,i);




"
				}


                );


            return project;
        }

        protected override void TestIsPass(Player player, PlayerException ex)
        {
            //player.ForceGC();
            {
                var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
                Assert.IsNotNull(global);
                var globalInstance = player.Context.GC.Heap[global.__global_index__];
                Assert.IsNotNull(globalInstance);
                Assert.IsNull(ex);

                RtScriptClass rtPayload = (RtScriptClass)globalInstance;

				StringPrint print = (StringPrint)player.Print;

				Assert.AreEqual("undefined\r\n[class MethodClosure] null 7 7 8\r\n", print.GetOutput());


                NaNBoxing c = rtPayload.ReadSlot(1);
				Assert.AreEqual( NaNBoxing.BoxType.HeapPtr, c.ValueType );
				Assert.AreEqual(player.Context.METHOD_CLOSURE.__instance_index__, c.HeapPtr);

                NaNBoxing d = rtPayload.ReadSlot(2);
				Assert.AreEqual(NaNBoxing.BoxType.Null, d.ValueType);

                NaNBoxing g = rtPayload.ReadSlot(5);
				Assert.AreEqual(NaNBoxing.BoxType.Sbyte, g.ValueType);
				Assert.AreEqual(7, g.SByteValue);

                NaNBoxing h = rtPayload.ReadSlot(6);
				Assert.AreEqual(NaNBoxing.BoxType.Sbyte, h.ValueType);
				Assert.AreEqual(7, h.SByteValue);

				NaNBoxing i = rtPayload.ReadSlot(7);
				Assert.AreEqual(NaNBoxing.BoxType.Sbyte, i.ValueType);
				Assert.AreEqual(8, i.SByteValue);
			}

           
        }


        [TestMethod]
        public void Test()
        {
            Run();
        }
    }
}
