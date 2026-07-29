using McMaster.Extensions.CommandLineUtils;
using System.Diagnostics;
using System.Reflection;

namespace juice
{
    internal class Program
    {
        static int Main(string[] args)
        {
            try
            {
                var app = new CommandLineApplication
                {
                    Name = "juice",
                    Description = "juice-script-2 编译器+运行器"
                };
                app.HelpOption(inherited: true).Description = "显示帮助信息";

                var optionSourceDirs = app.Option("-r|--recurse <package>",
                    "递归查找 .as 代码文件的目录", CommandOptionType.MultipleValue)
                    .Accepts(o => o.ExistingDirectory());

                var optionLibs = app.Option("-l|--lib <file>",
                    "要加载的 .swc 库文件", CommandOptionType.MultipleValue)
                    .Accepts(o => o.ExistingFile());

                var optionGlobal = app.Option("-g|--global <file>",
                    "全局库路径（默认使用内嵌的 juice_global.swc）", CommandOptionType.SingleValue);

                var optionWorkDir = app.Option("-w|--workspace <directory>",
                    "编译器工作目录，默认为临时目录", CommandOptionType.SingleValue)
                    .Accepts(o => o.ExistingDirectory());
                optionWorkDir.DefaultValue = Path.GetTempPath(); //Directory.GetCurrentDirectory();

                var optionForce = app.Option("-f|--force",
                    "强制重新编译字节码", CommandOptionType.NoValue);

                app.OnExecute(() =>
                {
                    if (optionSourceDirs.Values.Count == 0)
                    {
                        app.ShowHelp();
                        return 3;
                    }

                    string? exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    if (exeDir == null)
                    {
                        Console.Error.WriteLine("错误: 无法确定可执行文件目录");
                        return 3;
                    }

                    string? workDirValue = optionWorkDir.Value();
                    string workDir = Path.GetFullPath(workDirValue ?? Directory.GetCurrentDirectory());
                    string tmpDir = Path.Combine(workDir, ".juice_tmp");

                    // 1. Locate asc.exe and player.exe
                    string? ascExe = FindExecutable("asc.exe", exeDir);
                    string? playerExe = FindExecutable("player.exe", exeDir);
                    if (ascExe == null || playerExe == null)
                    {
                        Console.Error.WriteLine("错误: 找不到 asc.exe 或 player.exe");
                        Console.Error.WriteLine($"  搜索目录: {exeDir}");
                        var missing = new List<string>();
                        if (ascExe == null) missing.Add("asc.exe");
                        if (playerExe == null) missing.Add("player.exe");
                        Console.Error.WriteLine($"  缺少: {string.Join(", ", missing)}");
                        return 4;
                    }

                    // 2. Prepare temp directory
                    Directory.CreateDirectory(tmpDir);

                    try
                    {
                        // 3. Resolve juice_global.swc
                        string globalSwcPath;
                        if (optionGlobal.HasValue())
                        {
                            string? globalVal = optionGlobal.Value();
                            globalSwcPath = Path.GetFullPath(globalVal ?? throw new InvalidOperationException("-g 参数值为空"));
                        }
                        else
                        {
                            globalSwcPath = Path.Combine(tmpDir, "juice_global.swc");
                            ExtractEmbeddedResource(globalSwcPath);
                        }

                        // 4. Build full lib list: user libs + global swc (must be last for asc)
                        var allLibs = new List<string>();
                        if (optionLibs.Values.Count > 0)
                        {
                            allLibs.AddRange(optionLibs.Values.Where(v => v != null).Select(v => v!));
                        }
                        allLibs.Add(globalSwcPath);

                        // Copy all libs to tmpDir so player can find dependencies
                        foreach (var lib in allLibs)
                        {
                            if (lib == null) continue;
                            string dest = Path.Combine(tmpDir, Path.GetFileName(lib));
                            if (!File.Exists(dest))
                            {
                                File.Copy(lib, dest);
                            }
                        }

                        // 5. Compile
                        string outputSwc = Path.Combine(tmpDir, "output.swc");
                        string ascArgs = BuildAscArgs(
                            optionSourceDirs.Values.Where(v => v != null).Select(v => v!).ToList(),
                            allLibs,
                            workDir,
                            outputSwc,
                            optionForce.HasValue());
                        
                        int compileCode = RunProcess(ascExe, ascArgs);
                        if (compileCode != 0 || !File.Exists(outputSwc))
                        {
                            if (!File.Exists(outputSwc))
                            {
                                Console.Error.WriteLine("编译失败: 输出文件未生成");
                            }
                            return 1;
                        }

                        // 6. Run
                        string playerArgs = BuildPlayerArgs(
                            outputSwc,
                            globalSwcPath,
                            tmpDir);

                        //Console.WriteLine($"[juice] 运行: {Path.GetFileName(playerExe)} -s {Path.GetFileName(outputSwc)} ...");

                        int runCode = RunProcess(playerExe, playerArgs);

                        // 7. Cleanup temp on success
                        if (runCode == 0)
                        {
                            CleanupTemp(tmpDir);
                        }

                        return runCode;
                    }
                    catch
                    {
                        CleanupTemp(tmpDir);
                        throw;
                    }
                });

                return app.Execute(args);
            }
            catch (CommandParsingException e)
            {
                Console.Error.WriteLine(e.Message);
                return 3;
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"错误: {e.Message}");
                Console.ResetColor();
                return 3;
            }
        }

        static string? FindExecutable(string name, string searchDir)
        {
            string candidate = Path.Combine(searchDir, name);
            if (File.Exists(candidate))
                return Path.GetFullPath(candidate);

            DirectoryInfo? dir = new DirectoryInfo(searchDir);
            for (int i = 0; i < 5 && dir != null; i++)
            {
                string projectDir = Path.Combine(dir.FullName, Path.GetFileNameWithoutExtension(name));
                string devPath = Path.Combine(projectDir, "bin", "Debug", "net6.0", name);
                if (File.Exists(devPath))
                    return Path.GetFullPath(devPath);

                dir = dir.Parent;
            }

            return null;
        }

        static void ExtractEmbeddedResource(string outputPath)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string[] resources = assembly.GetManifestResourceNames();

            string? resourceName = resources.FirstOrDefault(r =>
                r.Equals("juice_global.swc", StringComparison.OrdinalIgnoreCase) ||
                r.EndsWith(".juice_global.swc", StringComparison.OrdinalIgnoreCase));

            if (resourceName == null)
            {
                throw new FileNotFoundException(
                    $"内嵌资源 juice_global.swc 未找到。可用资源: [{string.Join(", ", resources)}]");
            }

            string? outDir = Path.GetDirectoryName(outputPath);
            if (outDir != null)
            {
                Directory.CreateDirectory(outDir);
            }

            using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new FileNotFoundException($"无法加载内嵌资源: {resourceName}");
            }

            using FileStream fileStream = File.Create(outputPath);
            stream.CopyTo(fileStream);
        }

        static string BuildAscArgs(
            List<string> sourceDirs,
            List<string> libs,
            string workDir,
            string outputSwc,
            bool force)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var dir in sourceDirs)
                sb.Append($" -r \"{(dir.EndsWith("\\") ? dir.Substring(0,dir.Length - 1) : dir )}\"");
            foreach (var lib in libs)
                sb.Append($" -l \"{lib}\"");
            sb.Append($" -w \"{(workDir.EndsWith("\\") ? workDir.Substring(0,workDir.Length - 1) : workDir)}\"");
            sb.Append($" -o \"{outputSwc}\"");
            if (force)
                sb.Append(" -f");
            return sb.ToString().TrimStart();
        }

        static string BuildPlayerArgs(
            string swcPath,
            string globalSwcPath,
            string libDir)
        {
            return $"-s \"{swcPath}\" -g \"{globalSwcPath}\" -d \"{libDir}\"";
        }

        static int RunProcess(string exePath, string args)
        {
            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = args,
                UseShellExecute = false,
            };

            using Process? proc = Process.Start(psi);
            if (proc == null)
            {
                Console.Error.WriteLine($"错误: 无法启动进程 {exePath}");
                return 1;
            }
            proc.WaitForExit();
            return proc.ExitCode;
        }

        static void CleanupTemp(string tmpDir)
        {
            try
            {
                if (Directory.Exists(tmpDir))
                    Directory.Delete(tmpDir, true);
            }
            catch
            {
                // ignore cleanup failures
            }
        }
    }
}
