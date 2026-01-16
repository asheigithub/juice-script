using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC
{
    public sealed class ASParameter
    {
        private readonly ASMethod _method;

        //public object Value { get; set; }

        public int ValueExprIndex { get; set; }

        public string Name { get; set; }


        public ASMultiname Type { get; set; }
        public TypeKind TypeKind { get; set; }


        public bool IsOptional { get; set; }

        public bool IsRest { get; set; }

        public ConstantKind ValueKind { get; set; }


        public byte[] computeDefaultValue;
        public int compute_result_index;
        //public List<NaNBoxing> compute_constants;

        //public byte[] defaultValues;

        public ASParameter(ASMethod method)
        {
            _method = method;
        }
    }
}
