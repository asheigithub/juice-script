using juicescript.ABC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.runtime
{
    public class PlayerException : RuntimeException
    {
        public Player player;

        public NaNBoxing error;

        public string errorDebugMsg;

        public PlayerException(Player player,NaNBoxing error, string message) : base(message)
        {
            this.player = player;
            this.error = error;
            this.errorDebugMsg = DebugMessage();
        }

        public string ToDebugMessage()
        {
            return errorDebugMsg;
        }

        private string DebugMessage()
        {
            StringBuilder stringBuilder = new StringBuilder();

            var ex = this;

            stringBuilder.Append("[Fault] exception,[Message]=");
            switch (ex.error.ValueType)
            {
                case NaNBoxing.BoxType.Number:
                    stringBuilder.Append(ex.error.Number.ToString());
                    break;
                case NaNBoxing.BoxType.Undefined:
                    stringBuilder.Append("undefined");
                    break;
                case NaNBoxing.BoxType.Null:
                    stringBuilder.Append("null");
                    break;
                case NaNBoxing.BoxType.Boolean:
                    stringBuilder.Append(ex.error.Boolean ? "true" : "false");
                    break;
                case NaNBoxing.BoxType.Int:
                    stringBuilder.Append(ex.error.IntValue.ToString());
                    break;
                case NaNBoxing.BoxType.Uint:
                    stringBuilder.Append(ex.error.UIntValue.ToString());
                    break;
                case NaNBoxing.BoxType.Sbyte:
                    stringBuilder.Append(ex.error.SByteValue.ToString());
                    break;
                case NaNBoxing.BoxType.Byte:
                    stringBuilder.Append(ex.error.ByteValue.ToString());
                    break;
                case NaNBoxing.BoxType.Short:
                    stringBuilder.Append(ex.error.ShortValue.ToString());
                    break;
                case NaNBoxing.BoxType.UShort:
                    stringBuilder.Append(ex.error.UShortValue.ToString());
                    break;
                case NaNBoxing.BoxType.Float:
                    stringBuilder.Append(ex.error.FloatValue.ToString());
                    break;
                case NaNBoxing.BoxType.HeapPtr:

                    {
                        RtHeapBase instance = ex.player.Context.GC.Heap[ex.error.HeapPtr];

                        switch (instance.TypeKind)
                        {
                            case RtHeapTypeKind.CLASS:
                                stringBuilder.Append("[class " +

                                   (
                                    string.IsNullOrEmpty(((RtScriptClass)instance).Meta.QName.Namespace.Name) ?
                                    "" :
                                    (((RtScriptClass)instance).Meta.QName.Namespace.Name + ".")
                                    )
                                    +
                                    ((RtScriptClass)instance).Meta.QName.Name +

                                    "]");
                                break;
                            case RtHeapTypeKind.GLOBAL:
                                stringBuilder.Append("[object global]");
                                break;
                            case RtHeapTypeKind.STRING:
                                stringBuilder.Append("'" + ((RtString)instance).Str + "'");
                                break;
                            case RtHeapTypeKind.INSTANCE:

                                {
                                    if (((ASInstance)instance.Type).IsExtend(ex.player.Context.ERROR.Instance))
                                    {
                                        RtInstance rtPayload = (RtInstance)instance;
                                        NaNBoxing msg = rtPayload.ReadSlot(0, instance.Type._link_codescope,player);

                                        stringBuilder.Append(instance.Type.QName.Name);
                                        stringBuilder.Append(": ");

                                        if (msg.ValueType == NaNBoxing.BoxType.HeapPtr)
                                        {
                                            RtString @string = (RtString)ex.player.Context.GC.Heap[msg.HeapPtr];
                                            stringBuilder.Append(@string.Str);
                                        }
                                        else
                                        {
                                            stringBuilder.Append(string.Empty);
                                        }

                                    }
                                    else
                                    {
                                        stringBuilder.Append($" {instance.Type.QName.Name}@{ex.error.HeapPtr.ToString("x")}");
                                    }

                                }

                                break;
                            case RtHeapTypeKind.NAMESPACE:
                                stringBuilder.Append(((RtNameSpace)instance).ASNamespace.def_uri);
                                break;
                            case RtHeapTypeKind.VECTOR:
								stringBuilder.Append("[Vector]");
								break;
                            case RtHeapTypeKind.ARRAY:
								stringBuilder.Append("[Array]");
                                break;
							default:
								stringBuilder.Append(instance.TypeKind);
								break;
                        }


                    }

                    break;
                case NaNBoxing.BoxType.Fault:
                    stringBuilder.Append("fatal error.");
                    break;
                default:
                    break;
            }

            return stringBuilder.ToString();
        }


    }
}
