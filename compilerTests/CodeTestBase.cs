using juicescript.compiler;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace compilerTests
{
    public abstract class CodeTestBase
    {
        abstract protected TestCodeProject LoadProject();

        abstract protected void TestIsPass(juicescript.runtime.Player player, PlayerException ex);

		public class StringPrint : juicescript.runtime.IPrint
		{
            public StringBuilder output = new StringBuilder();

			public void Write(string message)
			{
				//throw new NotImplementedException();
				output.Append(message);
			}

			public void WriteLine(string message)
			{
				output.AppendLine(message);
			}

            public string GetOutput()
            { 
                return output.ToString();
            }

			public void Write(ReadOnlySpan<char> chars)
			{
                output.Append(chars);
			}
		}



		internal string Juice_GlobalSwc
        {
            get
            {
                var path = Assembly.GetExecutingAssembly().Location;
                var i = path.IndexOf("compilerTests");
                path = path.Substring(0, i);

                return path + "asc\\bin\\Debug\\net6.0\\global_swc\\juice_global.swc";
			}
        }


        protected virtual RtHeapInstance FindGlobal(Player player)
        {
            var global = player.Context.libs.SelectMany(o => o.Scripts).FirstOrDefault(o => o.QName.Name == "Main");
            Assert.IsNotNull(global);
            var globalInstance = player.Context.GC.Heap[global.__global_index__];
            Assert.IsNotNull(globalInstance);

            return globalInstance;
        }

        protected virtual void Run(TestCodeProject proj)
        {
            var tempProjPath = System.IO.Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            System.IO.Directory.CreateDirectory(tempProjPath);

            try
            {
                foreach (var item in proj.testCodes)
                {
                    System.IO.Directory.CreateDirectory(Path.Combine(tempProjPath, System.IO.Path.GetDirectoryName(item.Path)));

                    System.IO.File.WriteAllText(Path.Combine(tempProjPath, item.Path), item.Code);
                }

                //build pass 1
                {

                    new CompilePipeLine().Build(new List<string>() { tempProjPath },
                                tempProjPath, proj.libs.ToList(),

                                Path.Combine(tempProjPath, "o.swc"), false
                                );

                    juicescript.runtime.Player player = new Player();
                    player.LoadLib(File.ReadAllBytes(Path.Combine(tempProjPath, "o.swc")));
                    player.Print = new StringPrint();
                    

                    foreach (var item in proj.libs)
                    {
                        player.LoadLib(System.IO.File.ReadAllBytes(item));
                    }

                    PlayerException ex;
                    player.Run(out ex);


                    TestIsPass(player, ex);
                }
                //build pass 2
                {

                    new CompilePipeLine().Build(new List<string>() { tempProjPath },
                                tempProjPath, proj.libs.ToList(),

                                Path.Combine(tempProjPath, "o.swc"), false
                                );

                    juicescript.runtime.Player player = new Player();
                    player.LoadLib(File.ReadAllBytes(Path.Combine(tempProjPath, "o.swc")));
                    player.Print = new StringPrint();

                    foreach (var item in proj.libs)
                    {
                        player.LoadLib(System.IO.File.ReadAllBytes(item));
                    }

                    PlayerException ex;
                    player.Run(out ex);


                    TestIsPass(player, ex);
                }


            }
            finally
            {
                System.IO.Directory.Delete(tempProjPath, true);
            }
        }

        
        protected virtual void Run()
        { 
            var proj = LoadProject();
            
            Run(proj);

        }


    }
}
