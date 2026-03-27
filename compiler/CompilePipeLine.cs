using juicescript.ABC;
using juicescript.compiler.parse;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler
{
    public class CompilePipeLine
    {
		public bool IsLibMode { get; private set; }

		public CompilePipeLine() { }

		public CompilePipeLine(bool islib) { IsLibMode = islib; }

		public int Build(List<string> list, string workDir, List<string> libs, string out_swc_file , bool force_rebuild_bcode,List<string> displaycfg_files)
		{
			CompileContext context = new CompileContext(); context.islibmode = IsLibMode;

			context.player_for_compiler = new runtime.Player();

			try
			{
				foreach (string lib in libs)
				{
					var libData = System.IO.File.ReadAllBytes(  lib);
					context.player_for_compiler.LoadLib(libData);
				}

				context.player_for_compiler.CheckRequires();

			}
			catch (System.IO.IOException e)
			{
				throw new CompilerLoadLibException(e.Message);
			}
			catch (LoaderException e)
			{
				throw new CompilerLoadLibException(e.Message);
			}

			bool hasError = false;
			context.scriptDefs = new List<ScriptDef>();
			var code = ScriptDefBuilder.BuildDefines(list, workDir,false, context.scriptDefs,context, true, false);
			if (code != 0 && context.islibmode)
			{
				return code;
			}
			if (code != 0)
			{
				hasError = true;
			}
			

			code = DependencyResolver.BuildDependency(context);

			if (code != 0)
				return code;

			code = NameSpaceResolver.BuildNamespace(context);

			if(code !=0)
				return code;

			code = TypeStrResolver.BuildStrType(context);
			if (code != 0)
				return code;


			code = LayoutResolver.BuildLayout(context);
			if(code != 0)
				return code;


			if ( System.IO.Path.GetFileName(out_swc_file)  == "juice_global.swc")
			{
				foreach (var script in context.scriptDefs)
				{
					if (script.Script.QName.Name  == "Class")
					{
						context.player_for_compiler.Context.CLASS = script.Script.Traits[0].Class;
					}
				}
			}

			code = MethodResolver.BuildMethod( context,workDir , libs, force_rebuild_bcode , out_swc_file , displaycfg_files );
            if(code !=0 || hasError)
                return code;

            SWCWriter.Write(context, out_swc_file);

            return code;

        }

	}
}
