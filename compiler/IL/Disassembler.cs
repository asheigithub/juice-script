using juicescript.ABC.INS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace juicescript.compiler.IL
{
	internal class Disassembler
	{
		public static void Disassemble(byte[] bytecode, out int stackslotcount,out NaNBoxing[] constants,out Instruction[] instructions  )
		{
			using (System.IO.MemoryStream ms = new System.IO.MemoryStream(bytecode,false))
			{
				using (System.IO.BinaryReader br = new System.IO.BinaryReader(ms))
				{
					stackslotcount = br.ReadInt32();
					int constcount = br.ReadInt32();
					int i_count = br.ReadInt32();

					constants = new NaNBoxing[constcount];
					instructions = new Instruction[i_count];

					Token[] tokens = new Token[i_count];
					for (int i = 0; i < i_count; i++)
					{
						int p = br.ReadInt32();
						int line = br.ReadInt32();

						if (line != -1)
						{
							tokens[i] = new Token() { line = line };
						}
					}

					for (int i = 0; i < constcount; i++)
					{
						constants[i] = new NaNBoxing( br.ReadUInt64());
					}

					for (int i = 0; i < i_count; i++)
					{
						instructions[i] = ReadInstruction(br, tokens[i]);
					}

#if DEBUG
					if (ms.Position != bytecode.Length)
					{ 
						throw new InvalidOperationException();
					}

#endif
				}
			}
		}

		internal static Instruction ReadInstruction(BinaryReader br,Token token)
		{
			uint head = br.ReadUInt32();
			INS_Code code = (INS_Code)(head & 0xff);

			//INS_Code code = (INS_Code)br.ReadByte();

			int dst_index = (int)(head >> 8);

			Instruction instruction;
			switch (code)
			{
				case INS_Code.flag:
					instruction = new INS_Flag(token);
					break;
				case INS_Code.ld_const:
					instruction = new INS_Ld_Const(token);
					break;
				case INS_Code.ld_false:
					instruction = new INS_Ld_False(token);
					break;
				case INS_Code.ld_true:
					instruction = new INS_Ld_True(token);
					break;
				case INS_Code.ld_class:
					instruction = new INS_Ld_Class(token);
					break;
				case INS_Code.ld_ScopeH:
					instruction = new INS_Ld_ScopeHeap(token);
					break;
				case INS_Code.ld_InstanceOrScopeMemberValueRef:
					instruction = new INS_Ld_InstanceOrSocpeMemberRef(token);
					break;
				case INS_Code.ld_methodVariable:
					instruction = new INS_Ld_MethodVariable(token);
					break;
				case INS_Code.ld_namespace:
					instruction = new INS_Ld_Namespace(token);	
					break;
				case INS_Code.ld_RTQNameL_Ref:
					instruction = new INS_Ld_RTQNameL_Ref(token);
					break;
				case INS_Code.ld_MultiName_Ref:
					instruction = new INS_Ld_MultiName_Ref(token);
					break;
				case INS_Code.ld_MultiNameL_Ref:
					instruction = new INS_Ld_MultiNameL_Ref(token);
					break;
				case INS_Code.ld_MultiName_Val:
					instruction = new INS_Ld_MultiName_Val(token);
					break;
				case INS_Code.ld_MultiNameL_Val:
					instruction = new INS_Ld_MultiNameL_Val(token);
					break;
				case INS_Code.ld_instacneMember_Val:
					instruction = new INS_Ld_InstanceMember_Val(token);
					break;
				case INS_Code.ld_This:
					instruction = new INS_Ld_this(token);
					break;
				case INS_Code.ld_VectorType:
					instruction = new INS_Ld_VectorType(token);	
					break;
				case INS_Code.ld_null:
					instruction = new INS_Ld_Null(token);
					break;
				case INS_Code.ld_undefined:
					instruction = new INS_Ld_Undefined(token);
					break;
				case INS_Code.ld_array_hole:
					instruction = new INS_Ld_ArrayHole(token);
					break;
				case INS_Code.ld_function:
					instruction = new INS_Ld_Function(token);
					break;
				case INS_Code.ld_arguments:
					instruction = new INS_Ld_Arguments(token);
					break;
				case INS_Code.ld_method:
					instruction = new INS_Ld_Method(token);
					break;
				case INS_Code.ld_supermethod:
					instruction = new INS_Ld_SuperMethod(token);
					break;
				case INS_Code.ld_interface_method:
					instruction = new INS_Ld_Method_Interface(token);
					break;
				case INS_Code.storeScopeH:
					instruction = new INS_Store_ScopeHeap(token);
					break;
				case INS_Code.storeMethodVariable:
					instruction = new INS_Store_MethodVariable(token);
					break;
				case INS_Code.storeHeapValueRef:
					instruction = new INS_Store_HeapValueRef(token);
					break;
				case INS_Code.ld_MethodVariableInitValue:
					instruction = new INS_Ld_MethodVariableInitValue(token);
					break;
				case INS_Code.ld_memberInitValue:
					instruction = new INS_Ld_MemberInitValue(token);
					break;
				case INS_Code.ld_ValueRef:
					instruction = new INS_Ld_ValueRef(token);
					break;
				case INS_Code.move:
					instruction = new INS_Move(token);
					break;
				case INS_Code.delete:
					instruction = new INS_Delete(token);
					break;
				case INS_Code.neg:
					instruction = new INS_Neg(token);
					break;
				case INS_Code.positive:
					instruction = new INS_Positive(token);
					break;
				case INS_Code.multiply:
					instruction = new INS_Multiply(token);	
					break;
				case INS_Code.div:
					instruction = new INS_Div(token);
					break;
				case INS_Code.add:
					instruction = new INS_Add(token);
					break;
				case INS_Code.sub:
					instruction = new INS_Sub(token);
					break;
				case INS_Code.modulus:
					instruction = new INS_Modulus(token);
					break;
				case INS_Code.bitwise:
					instruction = new INS_BitWise(token);
					break;
				case INS_Code.logic_not:
					instruction = new INS_LogicNot(token);	
					break;
				case INS_Code.logic_comparison:
					instruction = new INS_Comparison(token);
					break;
				case INS_Code.strict_eq:
					instruction = new INS_Strict_Eq(token);
					break;
				case INS_Code.strict_neq:
					instruction = new INS_Strict_Neq(token);
					break;
				case INS_Code.equal:
					instruction = new INS_Equal(token);
					break;
				case INS_Code.not_equal:
					instruction = new INS_NotEqual(token);
					break;
				case INS_Code.get_in:
					instruction = new INS_In(token);
					break;
				case INS_Code.get_typeof:
					instruction = new INS_Typeof(token);
					break;
				case INS_Code.get_instanceof:
					instruction = new INS_InstanceOf(token);
					break;
				case INS_Code.get_is:
					instruction = new INS_Is(token);
					break;
				case INS_Code.cast_as:
					instruction = new INS_As(token);	
					break;
				case INS_Code.increment_decrement:
					instruction = new INS_Incr_Decr(token);
					break;
				case INS_Code.new_instance:
					instruction = new INS_New_Instance(token);
					break;
				case INS_Code.create_prop:
					instruction = new INS_Create_Prop(token);
					break;
				case INS_Code.type_cast:
					instruction = new INS_TypeCast(token);
					break;
				case INS_Code.super_ctor:
					instruction = new INS_SuperCtor(token);
					break;
				case INS_Code.ld_length:
					instruction = new INS_Ld_Length(token);
					break;
				case INS_Code.ld_function_call:
					instruction = new INS_Ld_Function_Call(token);
					break;
				case INS_Code.ld_function_bindglobal_call:
					instruction = new INS_Ld_Function_BindGlobal_Call(token);
					break;
				case INS_Code.bindthis_call:
					instruction = new INS_BindThis_Call(token);
					break;
				case INS_Code.bindglobal_call:
					instruction = new INS_bindGlobal_Call(token);
					break;
				case INS_Code.method_call:
					instruction= new INS_Method_Call(token);
					break;
				case INS_Code.read_property:
					instruction = new INS_readPoperty(token);
					break;
				case INS_Code.read_property_interface:
					instruction = new INS_readPoperty_Interface(token);
					break;
				case INS_Code.write_property:
					instruction = new INS_writeProperty(token);
					break;
				case INS_Code.write_property_interface:
					instruction = new INS_writeProperty_Interface(token);
					break;

				//case INS_Code.op_stack_Variable_ldconst:
				//	instruction = new INS_Op_stack_Var_ldConst(token);
				//	break;
				case INS_Code.if_logicOp_goto:
					instruction = new INS_If_LogicOp_Goto(token);
					break;
				case INS_Code.store_MultiNameL:
					instruction = new INS_Store_MultiNameL(token);
					break;
				case INS_Code.store_MultiName:
					instruction = new INS_Store_MultiName(token);
					break;
				case INS_Code.store_instanceMember:
					instruction = new INS_Store_InstanceMember(token);
					break;

				//case INS_Code.return_op:
				//	instruction = new INS_Return_Oper(token);
				//	break;

				//case INS_Code.short_ld_const:
				//	instruction = new INS_Short_Ld_Const(token);
				//	break;
				//case INS_Code.short_ld_methodVariable:
				//	instruction = new INS_Short_Ld_MethodVariable(token);
				//	break;
				//case INS_Code.short_strict_eq:
				//	instruction = new INS_Short_Strict_Eq(token);
				//	break;
				//case INS_Code.short_sub:
				//	instruction = new INS_Short_Sub(token);
				//	break;
				//case INS_Code.short_add:
				//	instruction = new INS_Short_Add(token);
				//	break;
				case INS_Code.array_vector_initelement:
					instruction = new INS_Array_Vector_InitElement(token);
					break;
				case INS_Code.O_ld_function_bindGlobal:
					instruction = new INS_O_Ld_Function_BindGLobal(token);
					break;
				case INS_Code.O_ld_method:
					instruction = new INS_O_Ld_Method(token);
					break;
				case INS_Code.O_ld_interface_method:
					instruction = new INS_O_Ld_Method_Interface(token);
					break;
				case INS_Code.O_Call:
					instruction = new INS_O_Call(token);
					break;
				case INS_Code.iter_initctx:
					instruction = new INS_Iter_GetCtx(token);
					break;
				case INS_Code.iter_get:
					instruction = new INS_Iter_Get(token);
					break;
				case INS_Code.iter_close:
					instruction = new INS_Iter_Close(token);	
					break;
				case INS_Code.iter_next:
					instruction = new INS_Iter_Next(token);
					break;
				case INS_Code.yield_return:
					instruction = new INS_Yield_Return(token);
					break;
				case INS_Code.yield_break:
					instruction = new INS_Yield_Break(token);
					break;
				case INS_Code.return_void:
					instruction = new INS_Return_Void(token);
					break;
				case INS_Code.return_value:
					instruction = new INS_Return_Value(token);
					break;
				case INS_Code.await_return:
					instruction = new INS_Await_Return(token);
					break;
				case INS_Code.await_resume:
					instruction = new INS_Await_Resume(token);
					break;
				//case INS_Code.return_async_promise:
				//	instruction = new INS_Return_Promise(token);
				//	break;
				case INS_Code.throw_error:
					instruction = new INS_Throw(token);
					break;
				case INS_Code.try_enter:
					instruction = new INS_Try_Enter	(token);
					break;
				case INS_Code.try_exit:
					instruction = new INS_Try_Exit (token);	
					break;
				case INS_Code.catch_enter:
					instruction = new INS_Catch_Enter (token);
					break;
				case INS_Code.catch_exit:
					instruction= new INS_Catch_Exit (token);
					break;
				case INS_Code.finally_enter:
					instruction= new INS_Finally_Enter (token);
					break;
				case INS_Code.finally_exit:
					instruction = new INS_Finally_Exit (token);
					break;
				case INS_Code.goto_flag:
					instruction = new INS_Goto(token);
					break;
				case INS_Code.if_false_goto:
					instruction = new INS_If_False_Goto(token);
					break;
				case INS_Code.if_true_goto:
					instruction = new INS_If_True_Goto(token);
					break;
				case INS_Code.expression_barrier:
					instruction = new INS_Barrier(token);
					break;
				case INS_Code.END:
					instruction = new INS_END();
					break;		
				default:
					throw new NotImplementedException(); 					
			}

#if DEBUG
			if (instruction.INS_Code != code)
				throw new InvalidOperationException();

#endif

			instruction.dst = new ABC.Locaters.StackLocater() { index = dst_index };
			instruction.Read(br);

			return instruction;

		}
	}
}
