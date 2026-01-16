using juicescript;
using juicescript.ABC;
using juicescript.runtime;
using McMaster.Extensions.CommandLineUtils;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace player
{
    internal class Program
    {
        static void Main(string[] args)
        {
            juicescript.runtime.Player player = new juicescript.runtime.Player(1024 * 1024 * 8);
            player.LoadLib(File.ReadAllBytes("F:\\GitHub\\juice-script-2\\juice-script-2\\fd_projs\\dev_scripts\\dev1\\obj\\o.swc"));

            player.LoadLib(File.ReadAllBytes("F:\\GitHub\\juice-script-2\\juice-script-2\\asc\\bin\\Debug\\net6.0\\global_swc\\juice_global.swc"));

            //Stopwatch sw = Stopwatch.StartNew();

            //sw.Start();

            PlayerException ex;
            player.Run(out ex);

			
			//sw.Stop();

			//Console.WriteLine(sw.ElapsedMilliseconds);

			if (ex != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;               
                Console.WriteLine(ex.ToDebugMessage());
                Console.ResetColor();

                Console.Error.WriteLine(ex.Message);
            }

#if PROFILEPLAYER
            InstructionProfiler.OutPutProfile();
#endif

            //Console.WriteLine("Hello, World!");

            

        }
    }
}
