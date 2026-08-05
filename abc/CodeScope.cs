using juicescript.ABC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript
{
    public enum CodeScopeKind : byte
    { 
        Script,
        Instance,
        Class,
        Method
    }


    public enum ScopeMemberKind : byte
    { 
        Slot,
        Constant,
        Parameter,
    }

    public class ScopeMember
    {
        public ScopeMemberKind Kind;

        /// <summary>
        /// 如果不是Parameter,则是ASTrait的QName
        /// </summary>
        public ASMultiname QName;

        /// <summary>
        /// 如果是Parameter，则是参数的Name
        /// </summary>
        public string PName;

        /// <summary>
        /// 成员的类型
        /// </summary>
        public ASMultiname Type;

        public TypeKind TypeKind;

        
        public ConstantKind ValueKind;

        /// <summary>
        /// 在哪个容器定义的
        /// </summary>
        public readonly ASContainer DefineAt;

        /// <summary>
        /// 如果不是Parameter,则是哪个trait生成的
        /// </summary>
        public readonly ASTrait trait;

        public ScopeMember(ASContainer defineAt,ASTrait trait)
        { 
            DefineAt = defineAt;
            this.trait = trait;
        }

        /// <summary>
        /// 运行时链接后指定，避免hash查找
        /// </summary>
        public ASClass __rt_type_class__;

        public ClassFlags _rt_type_flag;


        public byte[] compiler_initvalue;
        public int compiler_initvalue_stpos;

        public override string ToString()
        {
            return $"Kind:{Kind} [{ ( Kind== ScopeMemberKind.Parameter?PName:QName ) }] : Type: [{Type}] ";
        }

    }


    public class CodeScope
    {
        public CodeScopeKind Kind;

        public ASContainer Container;

        public List<ScopeMember> Members;

        /// <summary>
        /// 如果Kind是Instance,则保存类型的内存布局,否则为空
        /// </summary>
        public TypeLayout TypeLayout;

        /// <summary>
        /// 如果Kind是Method,保存参数的个数
        /// </summary>
        public int ParameterCout;

        public CodeScope Parent;

        public ASNamespaceSet NamespaceSet;

        /// <summary>
        /// 每个Script从1开始编号,所以总是大于0。
        /// </summary>
        public int index;


        public Memory<NaNBoxing> _rt_cache_init_data;


        public override string ToString()
        {
            return $"Scope {Container},Members:{Members.Count},Kind:{Kind}";
        }

    }
}
