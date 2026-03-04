using juicescript.ABC;
using juicescript.ABC.INS;
using juicescript.ABC.Locaters;
using juicescript.compiler.AST;
using juicescript.compiler.AST.Expr;
using juicescript.runtime;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace juicescript.compiler.IL
{
    internal class CompileEnv
    {
        internal class MemberInitValue
        {
            public ScopeMember member;
            public List<Instruction> setValueInstructions;
            public int start_byte_pos;

            public int FindStartBytePos(CompileEnv methodEnv)
            {
                int index = methodEnv.instructions.IndexOf(setValueInstructions[0]);
                if (index < 0)
                    throw new InvalidOperationException();

                int f= methodEnv.instructions.Take(index ).Sum( i=>i.Size );

                if (f != start_byte_pos)
                {
                    throw new InvalidOperationException();
                }

                return f;

            }




        }


        public readonly CodeScope Scope;
        public readonly List<AST.IAS3SyntaxNode> Codes;
        public readonly CompileContext CompileContext;
        public readonly HashSet<ASTrait> imports;

        public override string ToString()
        {
            return $"CompileEnv {Scope}";
        }

        public CompileEnv(CodeScope scope, HashSet<ASTrait> imports, List<AST.IAS3SyntaxNode> codes, CompileContext context)
        {
            this.Scope = scope;
            this.imports = imports;
            this.Codes = codes;
            this.CompileContext = context;

            instructions = new List<ABC.INS.Instruction>();
            initvalue_instructions = new List<MemberInitValue>();

            dict_stack_type = new Dictionary<StackLocater, CompileTypeKind>();
            dict_reg_stacklocater = new Dictionary<AS3Reg, StackLocater>();

            dict_reg_traitreference = new Dictionary<AS3Reg, Tuple<ASTrait[], AS3ExprStep>>();

            dict_reg_NsAccessContext = new Dictionary<AS3Reg, Tuple< ASContainer, ASNamespace>>();

            dict_reg_search_imports = new Dictionary<StackLocater, string>();

            dict_reg_namespace = new Dictionary<AS3Reg, ASNamespace>();

            dict_ReferenceBindInstance = new Dictionary<AS3Reg, StackLocater>();

            callresults = new HashSet<StackLocater>();

            Constants = new List<NaNBoxing>();

            catching_variable = new Stack<AS3Variable>();

        }

        public CompileEnv(CodeScope scope, HashSet<ASTrait> imports, List<IAS3SyntaxNode> codes, CompileContext context, CompileEnv compileEnv) : this(scope, imports, codes, context)
        {

            for (int i = 0; i < compileEnv.instructions.Count - 1; i++)
            {
                instructions.Add(compileEnv.instructions[i]);
            }

            for (int i = 0; i < compileEnv.initvalue_instructions.Count ; i++)
            {
                initvalue_instructions.Add(compileEnv.initvalue_instructions[i]);
            }

            foreach (var item in compileEnv.dict_stack_type)
            {
                dict_stack_type.Add(item.Key, item.Value);
            }
            foreach (var item in compileEnv.dict_reg_stacklocater)
            {
                dict_reg_stacklocater.Add(item.Key, item.Value);
            }
            foreach (var item in compileEnv.dict_reg_traitreference)
            {
                dict_reg_traitreference.Add(item.Key, item.Value);
            }
            foreach (var item in compileEnv.dict_reg_NsAccessContext)
            {
                dict_reg_NsAccessContext.Add(item.Key, item.Value);
            }
            foreach (var item in compileEnv.dict_reg_search_imports)
            {
                dict_reg_search_imports.Add(item.Key, item.Value);
            }
            foreach (var item in compileEnv.dict_reg_namespace)
            {
                dict_reg_namespace.Add(item.Key, item.Value);
            }
            foreach (var item in compileEnv.dict_ReferenceBindInstance)
            {
                dict_ReferenceBindInstance.Add(item.Key, item.Value);
            }
            foreach (var item in compileEnv.callresults)
            {
                callresults.Add(item);
            }

            Constants.AddRange(compileEnv.Constants);


        }


        internal readonly List<MemberInitValue> initvalue_instructions;

        internal readonly List<ABC.INS.Instruction> instructions;//编译出的IL指令

        private Dictionary<StackLocater, CompileTypeKind> dict_stack_type;

        private Dictionary<AS3Reg, StackLocater> dict_reg_stacklocater;

        private Dictionary<AS3Reg, Tuple<ASTrait[], AS3ExprStep>> dict_reg_traitreference;//AS3Reg对应的ASTrait引用

        private Dictionary<AS3Reg, Tuple< ASContainer, ASNamespace>> dict_reg_NsAccessContext;

        private Dictionary<StackLocater, string> dict_reg_search_imports;

        private Dictionary<AS3Reg, ASNamespace> dict_reg_namespace;

        private Dictionary<AS3Reg, StackLocater> dict_ReferenceBindInstance;

        private HashSet<StackLocater> callresults;

        internal List<NaNBoxing> Constants;





        internal Stack<AS3Variable> catching_variable; //当编译catch块时，可以查找catch的变量。

        internal List<AS3Variable> parent_catching_variable; // 当编译在catch块中定义的function时，可以查找捕获的变量


		/*
         * var i;
            var j=0;
            if( (i= function(){} ) === i) //考虑这行代码，在loadrightvalue遇到内嵌function,[],{}这样的堆对象时，避免重复构造对象
            {
	            trace(j);
            }
            else
            {
	            trace("eee");
            }
         */
		internal Stack<Dictionary<object, StackLocater>> stack_loaded_heapunit = new Stack<Dictionary<object, StackLocater>>(); 





        /// <summary>
        /// 为当前CodeScope分配一个新的栈地址
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public StackLocater MakeStackLocater(TypeKind maj, TypeKind mir = TypeKind.Unknown)
        {
            StackLocater stackLocater = new StackLocater() { index = dict_stack_type.Count };
            dict_stack_type.Add(stackLocater, new CompileTypeKind() { Maj = maj, Mir = mir });
            return stackLocater;
        }

        /// <summary>
        /// 读取栈地址数据类型
        /// </summary>
        /// <param name="stackLocater"></param>
        /// <returns></returns>
        public CompileTypeKind ReadStackType(StackLocater stackLocater)
        {
            return dict_stack_type[stackLocater];
        }


        public void SetRegNameSpace(AS3Reg reg, ASNamespace @namespace)
        {
            dict_reg_namespace.Add(reg, @namespace);
        }

        public ASNamespace ReadRegNameSpace(AS3Reg reg)
        {
            return dict_reg_namespace[reg];
        }


        public Tuple<ASTrait[], AS3ExprStep> ReadTraitRef(AS3Reg reg)
        {
            return dict_reg_traitreference[reg];
        }

        public bool TryReadTraitRef(AS3Reg reg, out Tuple<ASTrait[], AS3ExprStep> traitRefs)
        {
            return dict_reg_traitreference.TryGetValue(reg, out traitRefs);
        }


        public void SetRegTraitRef(AS3Reg reg, ASTrait[] traits, AS3ExprStep step)
        {
            dict_reg_traitreference.Add(reg, new Tuple<ASTrait[], AS3ExprStep>(traits, step));
        }


        public void SetCallResult(StackLocater stackLocater)
        { 
            callresults.Add(stackLocater);
        }

        public bool IsCallResult(StackLocater stackLocater)
        { 
            return callresults.Contains(stackLocater);
        }



        #region 收集新增的成员引用
        private HashSet<AS3Reg> mark;
        public void BeginCollectRef()
        {
            mark = new HashSet<AS3Reg>();
            foreach (var item in dict_reg_traitreference)
            {
                mark.Add(item.Key);
            }
        }

        public List<AS3Reg> EndCollectRef()
        {
            return dict_reg_traitreference.Keys.Where(r => !mark.Contains(r)).ToList();
        }
        #endregion

        public void SetReg_NsAccessContext(AS3Reg reg,  ASContainer container, ASNamespace searchNs)
        {
            dict_reg_NsAccessContext.Add(reg, new Tuple<ASContainer, ASNamespace>( container, searchNs));
        }

        public Tuple< ASContainer, ASNamespace> ReadNsAccessContext(AS3Reg reg)
        {
            return dict_reg_NsAccessContext[reg];
        }

        public void SetReg_SearchNSImport(StackLocater reg, string nstr)
        {
            dict_reg_search_imports.Add(reg, nstr);
        }

        public string ReadSearchNSImport(StackLocater reg)
        {
            return dict_reg_search_imports[reg];
        }

        public void SetReferenceBindInstance(AS3Reg refValue, StackLocater instance)
        {
            dict_ReferenceBindInstance.Add(refValue, instance);
        }

        public StackLocater ReadRefBindInstance(AS3Reg reg)
        { 
            return dict_ReferenceBindInstance[reg];
        }


        /// <summary>
        /// 为表达式求值寄存器分配一个栈地址
        /// </summary>
        /// <param name="reg"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public StackLocater GetStackLocater(AS3Reg reg, TypeKind maj = TypeKind.Any, TypeKind mir = TypeKind.Unknown)
        {
            if (reg == null)
                throw new ArgumentNullException();

            if (dict_reg_stacklocater.ContainsKey(reg))
            {
                if (maj != TypeKind.Any &&

                    dict_stack_type[dict_reg_stacklocater[reg]] != new CompileTypeKind() { Maj = maj, Mir = mir })
                {
                    throw new InvalidOperationException("前后类型不一致");
                }

                return dict_reg_stacklocater[reg];
            }
            else
            {
                StackLocater stackLocater = new StackLocater() { index = dict_stack_type.Count };
                dict_stack_type.Add(stackLocater, new CompileTypeKind() { Maj = maj, Mir = mir });
                dict_reg_stacklocater.Add(reg, stackLocater);

                return stackLocater;
            }
        }
        internal void BindStackLocator(AS3Reg reg, StackLocater rv)
        {
            if (reg == null)
                throw new ArgumentNullException();

            if (dict_reg_stacklocater.ContainsKey(reg))
                throw new InvalidOperationException();

            dict_reg_stacklocater.Add(reg, rv);

        }

        public int AddConstSbyte(sbyte v)
        {
            for (int i = 0; i < Constants.Count; i++)
            {
                if (Constants[i].ValueType == NaNBoxing.BoxType.Sbyte && Constants[i].SByteValue == v)
                {
                    return i;
                }
            }

            NaNBoxing boxing = new NaNBoxing();
            boxing.SetSByte(v);
            Constants.Add(boxing);
            return Constants.Count - 1;
        }

        public int AddConstbyte(byte v)
        {
            for (int i = 0; i < Constants.Count; i++)
            {
                if (Constants[i].ValueType == NaNBoxing.BoxType.Byte && Constants[i].ByteValue == v)
                {
                    return i;
                }
            }

            NaNBoxing boxing = new NaNBoxing();
            boxing.SetByte(v);
            Constants.Add(boxing);
            return Constants.Count - 1;
        }
        public int AddConstShort(short v)
        {
            for (int i = 0; i < Constants.Count; i++)
            {
                if (Constants[i].ValueType == NaNBoxing.BoxType.Short && Constants[i].ShortValue == v)
                {
                    return i;
                }
            }

            NaNBoxing boxing = new NaNBoxing();
            boxing.SetShort(v);
            Constants.Add(boxing);
            return Constants.Count - 1;
        }

        public int AddConstUShort(ushort v)
        {
            for (int i = 0; i < Constants.Count; i++)
            {
                if (Constants[i].ValueType == NaNBoxing.BoxType.UShort && Constants[i].UShortValue == v)
                {
                    return i;
                }
            }

            NaNBoxing boxing = new NaNBoxing();
            boxing.SetUShort(v);
            Constants.Add(boxing);
            return Constants.Count - 1;
        }


        public int AddConstInt(int v)
        {
            
            for (int i = 0; i < Constants.Count; i++)
            {
                if (Constants[i].ValueType == NaNBoxing.BoxType.Int && Constants[i].IntValue == v)
                {
                    return i;
                }
            }

            NaNBoxing boxing = new NaNBoxing();
            boxing.SetInt(v);
            Constants.Add(boxing);
            return Constants.Count - 1;
        }

        public int AddConstUInt(uint v)
        {
           
            for (int i = 0; i < Constants.Count; i++)
            {
                if (Constants[i].ValueType == NaNBoxing.BoxType.Uint && Constants[i].UIntValue == v)
                {
                    return i;
                }
            }

            NaNBoxing boxing = new NaNBoxing();
            boxing.SetUInt(v);
            Constants.Add(boxing);
            return Constants.Count - 1;
        }

        public int AddConstFloat(float v)
        {
			for (int i = 0; i < Constants.Count; i++)
			{
				if (Constants[i].ValueType == NaNBoxing.BoxType.Float)
				{
					if (
						(float.IsPositiveInfinity(Constants[i].FloatValue) && float.IsPositiveInfinity(v))
						||
						(float.IsNegativeInfinity(Constants[i].FloatValue) && float.IsNegativeInfinity(v))
						||
						(float.IsNaN(Constants[i].FloatValue) && float.IsNaN(v))
						||
						(Constants[i].FloatValue == v)
						)
					{
						return i;
					}
				}
			}

			NaNBoxing boxing = new NaNBoxing();
			boxing.SetFloat(v);
			Constants.Add(boxing);
			return Constants.Count - 1;
		}

        public int AddConstNumber(double v)
        {
           
            for (int i = 0; i < Constants.Count; i++)
            {
                if (Constants[i].ValueType == NaNBoxing.BoxType.Number)
                {
                    if (
                        (double.IsPositiveInfinity(Constants[i].Number) && double.IsPositiveInfinity(v))
                        ||
                        (double.IsNegativeInfinity(Constants[i].Number) && double.IsNegativeInfinity(v))
                        ||
                        (double.IsNaN(Constants[i].Number ) && double.IsNaN(v) )
                        ||
                        (Constants[i].Number == v )
                        )
                    {
                        return i;
                    }
                }
            }

            NaNBoxing boxing = new NaNBoxing();
            boxing.SetNumber(v);
            Constants.Add(boxing);
            return Constants.Count - 1;
        }

		internal int AddConstClassId(ulong classid)
		{


			for (int i = 0; i < Constants.Count; i++)
			{
				if (Constants[i].ValueType == NaNBoxing.BoxType.HeapPtr)
				{
					int p = Constants[i].HeapPtr;
					if (p >> 24 == (byte)ASMethodBody.PoolHeapPtrKind.LD_Class)
					{
						ulong cid = CompileContext.constpool_ldclass[p & 0xffffff];
						if (cid == classid)
						{
							return i;
						}

						//RtHeapInstance heapInstance = CompileContext.player_for_compiler.Context.GC.Heap[p & 0xffffff];
						//if (heapInstance.TypeKind == RtHeapTypeKind.CACHE_LD_CLASS
						//    && heapInstance.Type == @class)
						//{
						//    return i;
						//}
					}
				}
			}

			int index = CompileContext.constpool_ldclass.IndexOf(classid);
			if (index < 0)
			{
				index = CompileContext.constpool_ldclass.Count;
				CompileContext.constpool_ldclass.Add(classid);
			}

			int ptr = (0xffffff & index) | ((byte)ASMethodBody.PoolHeapPtrKind.LD_Class << 24);

			NaNBoxing boxing = new NaNBoxing();
			boxing.SetHeapPtr(ptr);
			Constants.Add(boxing);

			return Constants.Count - 1;


			//int heapPtr = CompileContext.player_for_compiler.Context.GC.AllocLD_Class(@class);
			//if (heapPtr == 0)
			//    throw new InvalidOperationException();
			//if (heapPtr > 0xffffff)
			//{
			//    throw new ParseException("heapptr > 0xffffff");
			//}
			//int ptr = (0xffffff & heapPtr) | ((byte)ASMethodBody.PoolHeapPtrKind.LD_Class << 24);

			//NaNBoxing boxing = new NaNBoxing();
			//boxing.SetHeapPtr(ptr);
			//Constants.Add(boxing);

			//return Constants.Count - 1;
		}

		internal int AddConstClassId(ASClass @class)
        {


            for (int i = 0; i < Constants.Count; i++)
            {
                if (Constants[i].ValueType == NaNBoxing.BoxType.HeapPtr)
                {
                    int p = Constants[i].HeapPtr;
                    if (p >> 24 == (byte)ASMethodBody.PoolHeapPtrKind.LD_Class)
                    {
                        ulong cid = CompileContext.constpool_ldclass[p & 0xffffff];
                        if (cid == @class.Type_identifier)
                        {
                            return i;
                        }

                        //RtHeapInstance heapInstance = CompileContext.player_for_compiler.Context.GC.Heap[p & 0xffffff];
                        //if (heapInstance.TypeKind == RtHeapTypeKind.CACHE_LD_CLASS
                        //    && heapInstance.Type == @class)
                        //{
                        //    return i;
                        //}
                    }
                }
            }

            int index = CompileContext.constpool_ldclass.IndexOf( @class.Type_identifier);
            if (index < 0)
            {
                index = CompileContext.constpool_ldclass.Count;
                CompileContext.constpool_ldclass.Add(@class.Type_identifier);
            }
            
			int ptr = (0xffffff & index) | ((byte)ASMethodBody.PoolHeapPtrKind.LD_Class << 24);

			NaNBoxing boxing = new NaNBoxing();
			boxing.SetHeapPtr(ptr);
			Constants.Add(boxing);

			return Constants.Count - 1;
			

            //int heapPtr = CompileContext.player_for_compiler.Context.GC.AllocLD_Class(@class);
            //if (heapPtr == 0)
            //    throw new InvalidOperationException();
            //if (heapPtr > 0xffffff)
            //{
            //    throw new ParseException("heapptr > 0xffffff");
            //}
            //int ptr = (0xffffff & heapPtr) | ((byte)ASMethodBody.PoolHeapPtrKind.LD_Class << 24);

            //NaNBoxing boxing = new NaNBoxing();
            //boxing.SetHeapPtr(ptr);
            //Constants.Add(boxing);

            //return Constants.Count - 1;
        }

        internal int AddConstNamespaceId(ASNamespace @namespace)
        {
            
            for (int i = 0; i < Constants.Count; i++)
            {
                if (Constants[i].ValueType == NaNBoxing.BoxType.HeapPtr)
                {
                    int p = Constants[i].HeapPtr;
                    if (p >> 24 == (byte)ASMethodBody.PoolHeapPtrKind.Namespace)
                    {

                        RtHeapInstance heapInstance = CompileContext.player_for_compiler.Context.GC.Heap[p & 0xffffff];
                        if (heapInstance.TypeKind == RtHeapTypeKind.NAMESPACE
                            && ((RtPayloadNameSpace)heapInstance.facility).ASNamespace == @namespace)
                        {
                            return i;
                        }
                    }
                }
            }


            int heapPtr = CompileContext.player_for_compiler.Context.GC.AllocNamespace(@namespace,0,0);
            if (heapPtr == 0)
                throw new InvalidOperationException();
            if (heapPtr > 0xffffff)
            {
                throw new ParseException("heapptr > 0xffffff");
            }

            int ptr = (0xffffff & heapPtr) | ((byte)ASMethodBody.PoolHeapPtrKind.Namespace << 24);

            NaNBoxing boxing = new NaNBoxing();
            boxing.SetHeapPtr(ptr);
            Constants.Add(boxing);

            return Constants.Count - 1;

        }


        internal int AddConstString(string v)
        {
            
            for (int i = 0; i < Constants.Count; i++)
            {
                if (Constants[i].ValueType == NaNBoxing.BoxType.HeapPtr)
                {
                    int p = Constants[i].HeapPtr;
                    if (p >> 24 == (byte)ASMethodBody.PoolHeapPtrKind.String)
                    {

                        RtHeapInstance heapInstance = CompileContext.player_for_compiler.Context.GC.Heap[ p & 0xffffff ];
                        if (heapInstance.TypeKind == RtHeapTypeKind.STRING
                            && string.Equals(((RtPayloadString)heapInstance.facility).Str, v, StringComparison.Ordinal))
                        {
                            return i;
                        }
                    }
                }
            }

            int heapptr = CompileContext.player_for_compiler.Context.GC.Complie_AllocString(v);
            if (heapptr == 0)
                throw new InvalidOperationException();
            if (heapptr > 0xffffff)
            {
                throw new ParseException("heapptr > 0xffffff");
            }

            int ptr = (0xffffff & heapptr) | ((byte)ASMethodBody.PoolHeapPtrKind.String << 24);

            NaNBoxing boxing = new NaNBoxing();
            boxing.SetHeapPtr(ptr);
            Constants.Add(boxing);
            


            return Constants.Count - 1;
        }

        internal int AddConstMethod(ASMethod method)
        {
			for (int i = 0; i < Constants.Count; i++)
			{
				if (Constants[i].ValueType == NaNBoxing.BoxType.HeapPtr)
				{
					int p = Constants[i].HeapPtr;
					if (p >> 24 == (byte)ASMethodBody.PoolHeapPtrKind.Method)
					{
						RtHeapInstance heapInstance = CompileContext.player_for_compiler.Context.GC.Heap[p & 0xffffff];
						if (heapInstance.TypeKind == RtHeapTypeKind.MethodScope
							&& heapInstance.Type == method.Body )
						{
							return i;
						}
					}
				}
			}

			int heapptr = CompileContext.player_for_compiler.Context.GC.AllocMethodScope(null, 0, null);
            if(heapptr == 0)
                throw new InvalidOperationException();
            if (heapptr > 0xffffff)
            {
                throw new ParseException("heapptr > 0xffffff");
            }

            CompileContext.player_for_compiler.Context.GC.Heap[heapptr].Type = method.Body;
            
            int ptr = (0xffffff & heapptr) | ((byte)ASMethodBody.PoolHeapPtrKind.Method << 24);

            NaNBoxing boxing = new NaNBoxing();
            boxing.SetHeapPtr(ptr);
            Constants.Add(boxing);

            return Constants.Count - 1;

        }

        internal int AddSuperMethod(ASClass _this_, int vtable_index)
        {
			for (int i = 0; i < Constants.Count; i++)
			{
				if (Constants[i].ValueType == NaNBoxing.BoxType.HeapPtr)
				{
					int p = Constants[i].HeapPtr;
					if (p >> 24 == (byte)ASMethodBody.PoolHeapPtrKind.Method)
					{
						RtHeapInstance heapInstance = CompileContext.player_for_compiler.Context.GC.Heap[p & 0xffffff];
						if (heapInstance.TypeKind == RtHeapTypeKind.MethodScope
							&& heapInstance.Type == _this_)
						{
							return i;
						}
					}
				}
			}

			int heapptr = CompileContext.player_for_compiler.Context.GC.AllocMethodScope(null, 0, null);
            if (heapptr == 0)
                throw new InvalidOperationException();
            if (heapptr > 0xffffff)
            {
                throw new ParseException("heapptr > 0xffffff");
            }

            CompileContext.player_for_compiler.Context.GC.Heap[heapptr].Type = _this_;
            ((RtPayloadMethodScope)CompileContext.player_for_compiler.Context.GC.Heap[heapptr].facility).ParentPtr = vtable_index;

            int ptr = (0xffffff & heapptr) | ((byte)ASMethodBody.PoolHeapPtrKind.SuperMethod << 24);

            NaNBoxing boxing = new NaNBoxing();
            boxing.SetHeapPtr(ptr);
            Constants.Add(boxing);

            return Constants.Count - 1;
        }


        internal int AddVectorDef(VectorDef vector)
        {
            int vectorIndex = CompileContext.vectorDefs.IndexOf(vector);
            if (vectorIndex > 0xffffff)
            {
                throw new ParseException("vectorIndex > 0xffffff");
            }
			for (int i = 0; i < Constants.Count; i++)
			{
				if (Constants[i].ValueType == NaNBoxing.BoxType.HeapPtr)
				{
					int p = Constants[i].HeapPtr;
					if (p >> 24 == (byte)ASMethodBody.PoolHeapPtrKind.VectorDef)
					{
						int index = p & 0xffffff;
                        if (vectorIndex == index)
                        {
                            return i;
                        }
					}
				}
			}



			int ptr = (0xffffff & vectorIndex) | ((byte)ASMethodBody.PoolHeapPtrKind.VectorDef << 24);
            NaNBoxing boxing = new NaNBoxing();
            boxing.SetHeapPtr(ptr);
            Constants.Add(boxing);

            return Constants.Count - 1;

        }

       


        public int GetStackSlotCount()
        {
            if (dict_stack_type.Count > 0)
            {
                return dict_stack_type.Max((s) => s.Key.index) + 1;
            }
            else
            { 
                return 0;
            }
        }


        #region IL指令编码成字节

        public byte[] Encode()
        {
            byte[] code = Assembler.Assemble( GetStackSlotCount(),Constants.ToArray(), instructions.ToArray() );

#if DEBUG
            int scount; NaNBoxing[] cs; Instruction[] inslist;
            Disassembler.Disassemble(code, out scount, out cs, out inslist);

            byte[] code2 = Assembler.Assemble( scount, cs, inslist );

            if (code.Length != code2.Length)
                throw new InvalidOperationException();

            if (new ReadOnlySpan<byte>(code).SequenceCompareTo(code2) != 0)
            {
                throw new InvalidOperationException();
            }

#endif


            return code;

        }

       

        #endregion


    }
}
