using juicescript.ABC;
using juicescript.ABC.INS;
using juicescript.compiler.AST;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.compiler
{
    public class CompileContext
    {
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

    }
}
