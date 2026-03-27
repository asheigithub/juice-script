using juicescript;
using juicescript.ABC;
using juicescript.runtime;
using McMaster.Extensions.CommandLineUtils;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
namespace player
{
 //                   _ooOoo_
 //                  o8888888o
 //                  88" . "88
 //                  (| -_- |)
 //                  O\  =  /O
 //               ____/`---'\____
 //             .'  \\|     |//  `.
 //            /  \\|||  :  |||//  \
 //           /  _||||| -:- |||||-  \
 //           |   | \\\  -  /// |   |
 //           | \_|  ''\---/''  |   |
 //           \  .-\__  `-`  ___/-. /
 //         ___`. .'  /--.--\  `. . __
 //      ."" '<  `.___\_<|>_/___.'  >'"".
 //     | | :  `- \`.;`\ _ /`;.`/ - ` : | |
 //     \  \ `_.   \_ __\ /__ _/   .-` /  /
 //=====`-.____`.___ \_____/___.-`___.-'=====
 //                   `=---='

 //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
 //         佛祖保佑        永无BUG
 //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

	internal class Program
	{
		static int Main(string[] args)
		{
			try
			{
				var app = new CommandLineApplication
				{
					Name = "player",
					Description = "juice-script-2 player"
				};
				app.HelpOption(inherited: true).Description = "显示帮助信息";
				var optionScript = app.Option("-s|--script <file>", "要运行的 SWC 文件", CommandOptionType.SingleValue)
					.IsRequired()
					.Accepts(o => o.ExistingFile());
				var optionLibDir = app.Option("-d|--lib-dir <directory>", "依赖库搜索目录", CommandOptionType.SingleValue);
				optionLibDir.DefaultValue = Directory.GetCurrentDirectory();
				var optionGlobal = app.Option("-g|--global <file>", "全局库路径", CommandOptionType.SingleValue);
				optionGlobal.DefaultValue = Path.Combine(Directory.GetCurrentDirectory(), "juice_global.swc");
				app.OnExecute(() =>
				{
					string scriptPath = optionScript.Value();
					string libDir = optionLibDir.Value();
					string globalPath = optionGlobal.Value();
					juicescript.runtime.Player player = new juicescript.runtime.Player(1024 * 1024 * 8);
					HashSet<string> loadedAssemblies = new HashSet<string>();
					LoadSwcWithDeps(player, scriptPath, libDir, loadedAssemblies);
					if (!loadedAssemblies.Contains("juice_global.swc"))
					{
						if (File.Exists(globalPath))
						{
							player.LoadLib(File.ReadAllBytes(globalPath));
						}
					}
					//Stopwatch sw = Stopwatch.StartNew();

					//sw.Start();

					PlayerException ex;
					player.Run(out ex);


					//sw.Stop();

					//Console.WriteLine(sw.ElapsedMilliseconds);

					if (ex != null)
					{
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine(ex.errorDebugMsg);
						Console.ResetColor();

						Console.Error.WriteLine(ex.Message);
						return 1;
					}
#if PROFILEPLAYER
                    InstructionProfiler.OutPutProfile();
#endif
					return 0;
				});
				return app.Execute(args);
			}
			catch (Exception ex)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(ex.Message);
				Console.ResetColor();
				return 1;
			}
		}
		static void LoadSwcWithDeps(Player player, string swcPath, string libDir, HashSet<string> loadedAssemblies)
		{
			if (!File.Exists(swcPath))
			{
				throw new FileNotFoundException($"SWC 文件不存在: {swcPath}");
			}
			byte[] swcBytes = File.ReadAllBytes(swcPath);
			using (var ms = new MemoryStream(swcBytes))
			{
				var swcFile = SWCReader.Read(ms);
				if (loadedAssemblies.Contains(swcFile.assemblyName))
				{
					return;
				}
				loadedAssemblies.Add(swcFile.assemblyName);
				player.LoadLib(swcBytes);
				foreach (var refAssembly in swcFile.refAssemblys)
				{
					string depPath = FindSwcFile(refAssembly, libDir);
					if (depPath != null)
					{
						LoadSwcWithDeps(player, depPath, libDir, loadedAssemblies);
					}
				}
			}
		}
		static string FindSwcFile(string assemblyName, string libDir)
		{
			if (!Directory.Exists(libDir))
			{
				return null;
			}
			foreach (var file in Directory.GetFiles(libDir, "*.swc"))
			{
				try
				{
					byte[] bytes = File.ReadAllBytes(file);
					using (var ms = new MemoryStream(bytes))
					{
						var swcFile = SWCReader.Read(ms);
						if (swcFile.assemblyName == assemblyName)
						{
							return file;
						}
					}
				}
				catch
				{
					continue;
				}
			}
			return null;
		}
	}
}