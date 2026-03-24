using juicescript.ABC;
using juicescript.ABC.INS;
using juicescript.compiler.AST;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler
{
    public class CompileContext
    {
		internal bool islibmode=false;

        //internal List<SWCFile> libs;
        internal Dictionary<ASTrait,SWCFile> import_trait_at = new Dictionary<ASTrait, SWCFile> ();


        internal List<ScriptDef> scriptDefs;
        internal Dictionary<ScriptDef, string> scriptInProj = new Dictionary<ScriptDef, string>(); 

        internal Dictionary<ScriptDef, HashSet<ASTrait>> scriptDef_packageimports = new Dictionary<ScriptDef, HashSet<ASTrait>>();
        internal Dictionary<ScriptDef, HashSet<ASTrait>> scriptDef_scriptimports = new Dictionary<ScriptDef, HashSet<ASTrait>>();

        internal HashSet<string> referenceAssembly = new HashSet<string> ();


        internal Dictionary<ASMultiname,ASClass> dict_super_interfaces = new Dictionary<ASMultiname,ASClass>();

        internal List<ASClass> classDependSort;


        //internal Dictionary<TypeKind,VectorDef> dict_VectorDefs = new Dictionary<TypeKind,VectorDef>();

        internal List<VectorDef> vectorDefs = new List<VectorDef> ();

        internal Dictionary<ASMultiname, TypeLayout> dict_typelayout = new Dictionary<ASMultiname, TypeLayout>();

        internal Dictionary<ASMethod, List<NaNBoxing>> dict_method_constants = new Dictionary<ASMethod, List<NaNBoxing>>();

        internal Dictionary<AS3Function, ASMethod> dict_method_as3function = new Dictionary<AS3Function, ASMethod> ();

		internal Dictionary<List<NaNBoxing>, List<Tuple<int, ASClass>>> dict_methodresolver_ldclass_map = new Dictionary<List<NaNBoxing>, List<Tuple<int, ASClass>>>();



		internal List<ulong> constpool_ldclass = new List<ulong>();

		internal Player player_for_compiler = null;


        internal Stack<object> computeConstExprState = new Stack<object> ();


        internal ScriptDef buildingScript = null;


		//internal Stack<EvalMemberInitValue> only_const_scriptinit = new Stack<EvalMemberInitValue> ();
		//internal HashSet<AS3Expression> constant_init_expr = new HashSet<AS3Expression> ();

		//internal class EvalMemberInitValue
		//{
		//    public List<Instruction> scopeCodes;
		//    public Dictionary < AS3Expression ,Tuple<ScopeMember, List<Instruction>>> memberCodes;

		//}
		/// <summary>
		/// 清理Windows文件路径中的非法字符
		/// </summary>
		/// <param name="originalPath">原始路径字符串</param>
		/// <param name="replacementChar">替换非法字符的字符（默认下划线）</param>
		/// <returns>清理后的合法路径字符串</returns>
		public static string CleanInvalidPathChars(string originalPath, char replacementChar = '_')
		{
			// 空值校验
			if (string.IsNullOrWhiteSpace(originalPath))
			{
				return string.Empty;
			}

			// 获取Windows系统定义的所有路径非法字符
			char[] invalidChars = Path.GetInvalidPathChars();
			// 获取Windows系统定义的所有文件名非法字符（补充路径非法字符之外的部分）
			char[] invalidFileNameChars = Path.GetInvalidFileNameChars();

			// 合并所有非法字符并去重
			StringBuilder invalidCharsBuilder = new StringBuilder();
			foreach (char c in invalidChars)
			{
				if (invalidCharsBuilder.ToString().IndexOf(c) == -1)
				{
					invalidCharsBuilder.Append(c);
				}
			}
			foreach (char c in invalidFileNameChars)
			{
				if (invalidCharsBuilder.ToString().IndexOf(c) == -1)
				{
					invalidCharsBuilder.Append(c);
				}
			}

			// 构建清理后的路径
			StringBuilder cleanPath = new StringBuilder();
			foreach (char c in originalPath)
			{
				// 如果字符是非法字符，替换为指定字符；否则保留原字符
				if (invalidCharsBuilder.ToString().Contains(c.ToString()))
				{
					cleanPath.Append(replacementChar);
				}
				else
				{
					cleanPath.Append(c);
				}
			}

			return cleanPath.ToString();
		}
	}
}
