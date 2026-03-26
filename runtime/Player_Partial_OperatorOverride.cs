using juicescript.ABC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.runtime
{
	public partial class Player
	{
		public enum OverrideOperator
		{ 
			/// <summary>
			/// +
			/// </summary>
			add = 0,
			/// <summary>
			/// -
			/// </summary>
			sub = 1,
			/// <summary>
			/// *
			/// </summary>
			mul = 2,
			/// <summary>
			/// /
			/// </summary>
			div = 3,
			/// <summary>
			/// %
			/// </summary>
			mod = 4 ,

			/// <summary>
			/// - 取反
			/// </summary>
			neg = 5 ,
			/// <summary>
			/// + 一元正
			/// </summary>
			positive = 6,
		}

		private HashSet<ASClass> operator_override_type_code = new HashSet<ASClass>();

		public ASMethod[][][] overrideOperatorMethods = new ASMethod[(int)OverrideOperator.positive + 1][][];

		/// <summary>
		/// 计算操作符重载表
		/// </summary>
		/// <param name="script"></param>
		public void ComputeOperatorTable(ASScript script , Dictionary<ulong,ASClass> typeDict)
		{
			operator_override_type_code.Add(null); //null的占位
			unsafe
			{
				TypeKind* mapping = stackalloc TypeKind[11];
				mapping[1] = TypeKind.Boolean;
				mapping[2] = TypeKind.Byte;
				mapping[3] = TypeKind.SByte;
				mapping[4] = TypeKind.Short;
				mapping[5] = TypeKind.UShort;
				mapping[6] = TypeKind.Int;
				mapping[7] = TypeKind.Uint;
				mapping[8] = TypeKind.Float;
				mapping[9] = TypeKind.Number;
				mapping[10] = TypeKind.String;

				for (int i = 1; i < 11; i++)
				{
					if (typeDict[(ulong) mapping[i] ].Instance._operator_type_index == -1)
					{
						operator_override_type_code.Add(typeDict[(ulong)mapping[i]]);
						typeDict[(ulong)mapping[i]].Instance._operator_type_index = i;
						typeDict[(ulong)mapping[i]].Instance._operator_type = typeDict[(ulong)mapping[i]];
					}					
				}
			}

			for (int i = 0; i < overrideOperatorMethods.Length; i++)
			{
				if (overrideOperatorMethods[i] == null)
				{
					overrideOperatorMethods[i] = new ASMethod[ operator_override_type_code.Count ][];
					for (int j = 0; j < overrideOperatorMethods[i].Length; j++)
					{
						overrideOperatorMethods[i][j] = new ASMethod[operator_override_type_code.Count];
					}
				}
			}

			for (int i = 0; i < script.allContainers.Count; i++)
			{
				var container = script.allContainers[i];

				for (int k = 0; k < container.Traits.Count; k++)
				{
					var t = container.Traits[k];
					for (int j = 0; j < t.ASMetadata.Count; j++)
					{
						if (t.ASMetadata[j].Name == "operator")
						{
							if (t.Kind == TraitKind.Method)
							{
								var method = t.Method;

								if (!(method.Container is ASClass))
								{
									throw new LoaderException("[operator] only can be defined on static method.") { Token = t.Token };
								}
								ASClass cls = (ASClass)method.Container;
								if (!cls.Instance.Flags.HasFlag(ClassFlags.Final))
								{
									throw new LoaderException("[operator] only can be defined on final class.") { Token = t.Token };
								}
								if (cls.Instance.Flags.HasFlag(ClassFlags.Interface))
								{
									throw new LoaderException("[operator] only can be defined on final class.") { Token = t.Token };
								}


								OverrideOperator op;
								var meta = t.ASMetadata[j];
								if (meta.Items.Count != 1)
								{
									throw new LoaderException($"Illegal [operator]  on t.QName.ToDebugTypeName()") { Token = t.Token };
								}
								string optype = meta.Items[0].Value;
								if (optype == "\"+\"")
								{
									op = OverrideOperator.add;
								}
								else if (optype == "\"-\"")
								{
									op = OverrideOperator.sub;
								}
								else if (optype == "\"*\"")
								{
									op = OverrideOperator.mul;
								}
								else if (optype == "\"/\"")
								{
									op = OverrideOperator.div;
								}
								else if (optype == "\"%\"")
								{
									op = OverrideOperator.mod;
								}
								else
								{
									throw new LoaderException($"Illegal [operator]  on {t.QName.ToDebugTypeName()}") { Token = t.Token };
								}

								if ((op == OverrideOperator.sub || op ==  OverrideOperator.add) && method.Parameters.Count == 1
									&&
									!method.Parameters[0].IsOptional
									&&
									!method.Parameters[0].IsRest
									)
								{
									op = (op == OverrideOperator.sub ?  OverrideOperator.neg : OverrideOperator.positive);
								}

								if (op != OverrideOperator.neg && op != OverrideOperator.positive)
								{
									if (method.Parameters.Count != 2 || method.Parameters[0].IsOptional || method.Parameters[1].IsOptional || method.Parameters[1].IsRest)
									{
										throw new LoaderException($"Illegal [operator]  on {t.QName.ToDebugTypeName()}") { Token = t.Token };
									}

									if (method.Parameters[0].TypeKind == TypeKind.Any || method.Parameters[1].TypeKind == TypeKind.Any)
									{
										throw new LoaderException($"Illegal [operator] parameter type : * ") { Token = t.Token };
									}
								}


								ASClass typelhs; 
								ASClass typerhs;

								if (op != OverrideOperator.neg && op != OverrideOperator.positive)
								{
									typelhs = typeDict[(ulong)method.Parameters[0].TypeKind];
									typerhs = typeDict[(ulong)method.Parameters[1].TypeKind];
								}
								else
								{
									typelhs = typeDict[(ulong)method.Parameters[0].TypeKind];
									typerhs = typelhs;
								}

								if (typelhs.Type_identifier != cls.Type_identifier && typerhs.Type_identifier != cls.Type_identifier)
								{
									throw new LoaderException("Illegal [operator] parameter type.") { Token = t.Token };
								}

								
								if (typelhs.Instance._operator_type_index == -1)
								{
									typelhs.Instance._operator_type_index = operator_override_type_code.Count;
									typelhs.Instance._operator_type = typelhs;
									bool success = operator_override_type_code.Add(typelhs);
									Debug.Assert(success);
								}
								
								if (typerhs.Instance._operator_type_index == -1)
								{
									typerhs.Instance._operator_type_index = operator_override_type_code.Count;
									typerhs.Instance._operator_type = typerhs;
									bool success = operator_override_type_code.Add(typerhs);
									Debug.Assert(success);
								}


								var table = overrideOperatorMethods[(int)op];
								if (table == null)
								{
									overrideOperatorMethods[(int)op] = new ASMethod[operator_override_type_code.Count][];
									for (int l = 0; l < operator_override_type_code.Count; l++)
									{
										overrideOperatorMethods[(int)op][l] = new ASMethod[operator_override_type_code.Count];
									}
									
								}
								else if(overrideOperatorMethods[(int)op].Length < operator_override_type_code.Count )
								{
									int oldlen = overrideOperatorMethods[(int)op].Length;

									var newtable = new ASMethod[operator_override_type_code.Count][];
									for (int l = 0; l < operator_override_type_code.Count; l++)
									{
										newtable[l] = new ASMethod[operator_override_type_code.Count];
									}

									for (int l = 0; l < oldlen; l++)
									{
										for (int m = 0; m < oldlen; m++)
										{
											newtable[l][m] = overrideOperatorMethods[(int)op][l][m];
										}
									}

									overrideOperatorMethods[(int)op] = newtable;
								}

								if (overrideOperatorMethods[(int)op][typelhs.Instance._operator_type_index][typerhs.Instance._operator_type_index] == null)
								{
									overrideOperatorMethods[(int)op][typelhs.Instance._operator_type_index][typerhs.Instance._operator_type_index] = method;
								}
								else
								{
									throw new LoaderException($"Ambiguous operator override:{overrideOperatorMethods[(int)op][typelhs.Instance._operator_type_index][typerhs.Instance._operator_type_index]} { method}") { Token = t.Token };
								}
							}
							else
							{
								throw new LoaderException( $"Illegal [operator]  on t.QName.ToDebugTypeName()" ) { Token = t.Token };
							}
							break;
						}

					}

				}
			}


			//更新隐式转换
			//由于我们强制规定了操作符重载的操作数的类型要么是基本类型，要么至少一个是本script里定义的class,
			//所以基本类型隐式转换不可能和其他script中的操作符重载冲突. 
			for (int i = 0; i < overrideOperatorMethods.Length; i++)
			{
				var table = overrideOperatorMethods[i]; //table是正方形
				
				for (int j = 0; j < table.Length; j++)
				{
					//short可覆盖 sbyte和byte
					if (table[4][j] != null) 
					{
						if (table[3][j] == null)
						{
							table[3][j] = table[4][j];
						}
						if (table[2][j] == null)
						{
							table[2][j] = table[4][j];
						}
					}
					if (table[j][4] != null)
					{
						if (table[j][3] == null)
						{
							table[j][3] = table[j][4];
						}
						if (table[j][2] == null)
						{
							table[j][2] = table[j][4];
						}
					}

					//ushort可覆盖 byte
					if (table[5][j] != null)
					{
						if (table[3][j] == null)
						{
							table[3][j] = table[5][j];
						}
					}
					if (table[j][5] != null)
					{
						if (table[j][3] == null)
						{
							table[j][3] = table[j][5];
						}
					}


					//int 可覆盖 sbyte,byte,short,ushort
					if (table[6][j] != null)
					{
						if (table[5][j] == null)
						{
							table[5][j] = table[6][j];
						}
						if (table[4][j] == null)
						{
							table[4][j] = table[6][j];
						}
						if (table[3][j] == null)
						{
							table[3][j] = table[6][j];
						}
						if (table[2][j] == null)
						{
							table[2][j] = table[6][j];
						}
					}
					if (table[j][6] != null)
					{
						if (table[j][5] == null)
						{
							table[j][5] = table[j][6];
						}
						if (table[j][4] == null)
						{
							table[j][4] = table[j][6];
						}
						if (table[j][3] == null)
						{
							table[j][3] = table[j][6];
						}
						if (table[j][2] == null)
						{
							table[j][2] = table[j][6];
						}
					}

					//uint 可覆盖 byte,ushort
					if (table[7][j] != null)
					{
						if (table[5][j] == null)
						{
							table[5][j] = table[7][j];
						}
						if (table[3][j] == null)
						{
							table[3][j] = table[7][j];
						}
					}
					if (table[j][7] != null)
					{
						if (table[j][5] == null)
						{
							table[j][5] = table[j][7];
						}
						if (table[j][3] == null)
						{
							table[j][3] = table[j][7];
						}
					}

					//float 可覆盖其他 
					if (table[8][j] != null)
					{
						if (table[7][j] == null)
						{
							table[7][j] = table[8][j];
						}
						if (table[6][j] == null)
						{
							table[6][j] = table[8][j];
						}
						if (table[5][j] == null)
						{
							table[5][j] = table[8][j];
						}
						if (table[4][j] == null)
						{
							table[4][j] = table[8][j];
						}
						if (table[3][j] == null)
						{
							table[3][j] = table[8][j];
						}
						if (table[2][j] == null)
						{
							table[2][j] = table[8][j];
						}
					}
					if (table[j][8] != null)
					{
						if (table[j][7] == null)
						{
							table[j][7] = table[j][8];
						}
						if (table[j][6] == null)
						{
							table[j][6] = table[j][8];
						}
						if (table[j][5] == null)
						{
							table[j][5] = table[j][8];
						}
						if (table[j][4] == null)
						{
							table[j][4] = table[j][8];
						}
						if (table[j][3] == null)
						{
							table[j][3] = table[j][8];
						}
						if (table[j][2] == null)
						{
							table[j][2] = table[j][8];
						}
					}

					//number 可覆盖其他
					if (table[9][j] != null)
					{
						if (table[8][j] == null)
						{
							table[8][j] = table[9][j];
						}
						if (table[7][j] == null)
						{
							table[7][j] = table[9][j];
						}
						if (table[6][j] == null)
						{
							table[6][j] = table[9][j];
						}
						if (table[5][j] == null)
						{
							table[5][j] = table[9][j];
						}
						if (table[4][j] == null)
						{
							table[4][j] = table[9][j];
						}
						if (table[3][j] == null)
						{
							table[3][j] = table[9][j];
						}
						if (table[2][j] == null)
						{
							table[2][j] = table[9][j];
						}
					}
					if (table[j][9] != null)
					{
						if (table[j][8] == null)
						{
							table[j][8] = table[j][9];
						}
						if (table[j][7] == null)
						{
							table[j][7] = table[j][9];
						}
						if (table[j][6] == null)
						{
							table[j][6] = table[j][9];
						}
						if (table[j][5] == null)
						{
							table[j][5] = table[j][9];
						}
						if (table[j][4] == null)
						{
							table[j][4] = table[j][9];
						}
						if (table[j][3] == null)
						{
							table[j][3] = table[j][9];
						}
						if (table[j][2] == null)
						{
							table[j][2] = table[j][9];
						}
					}

				}


			}


		}


		public int GetOpOverrideTypeId(NaNBoxing v,out ASClass type)
		{
			switch (v.ValueType)
			{
				case NaNBoxing.BoxType.Number:
					type = null;
					return 9;
				case NaNBoxing.BoxType.Undefined:
					type = null;
					return -1;
				case NaNBoxing.BoxType.Null:
					type = null;
					return 0;
				case NaNBoxing.BoxType.Boolean:
					type = null;
					return 1;
				case NaNBoxing.BoxType.Int:
					type = null;
					return 6;
				case NaNBoxing.BoxType.Uint:
					type = null;
					return 7;
				case NaNBoxing.BoxType.Sbyte:
					type = null;
					return 2;
				case NaNBoxing.BoxType.Byte:
					type = null;
					return 3;
				case NaNBoxing.BoxType.Short:
					type = null;
					return 4;
				case NaNBoxing.BoxType.UShort:
					type = null;
					return 5;
				case NaNBoxing.BoxType.Float:
					type = null;
					return 8;
				case NaNBoxing.BoxType.HeapPtr:
					{
						var obj = Context.GC.Heap[v.HeapPtr];

						switch (obj.TypeKind)
						{
							case RtHeapTypeKind.CLASS:
								type = null;
								return -1;
							case RtHeapTypeKind.GLOBAL:
								type = null;
								return -1;
							case RtHeapTypeKind.STRING:
								type = null;
								return 10;
							case RtHeapTypeKind.INSTANCE:
								type = ((ASInstance)obj.Type)._operator_type;
								return ((ASInstance)obj.Type)._operator_type_index;
							case RtHeapTypeKind.NAMESPACE:
							case RtHeapTypeKind.ARRAY:
							case RtHeapTypeKind.VECTOR:
							case RtHeapTypeKind.STACK_CACHE_OBJ:
							case RtHeapTypeKind.DYNAMIC_PROPERTYS:
							case RtHeapTypeKind.SHAPE:
							case RtHeapTypeKind.MethodScope:
							case RtHeapTypeKind.CLOSURE:
							default:
								type = null;
								return -1;
						}

					}
					
				case NaNBoxing.BoxType.LocalString:
					type = null;
					return 10;
				case NaNBoxing.BoxType.Fault:
					
				default:
					type = null;
					return -1;
			}


		}


		

	}

}
