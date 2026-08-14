using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC
{
    public enum TypeKind : ulong
    {
        Any = 0,
        Boolean = 1,
		Int = 2,
		Uint = 3,
		SByte =4,
        Byte = 5,
        Short = 6,
        UShort = 7,         
        Float = 8,
        Number = 9,

        Fun_Void = 13,

        /// <summary>
        /// 表示对Slot或者Const类型的Trait的引用,或者Getter,Setter属性的引用。这时需要2个Trait
        /// </summary>
        TraitDataReference = 40,

        /// <summary>
        /// 表示根据RTQName或者RTQNameL或者MultiName或者MultiNameL 访问对象
        /// </summary>
        RTQName_MultiName_DataReference = 45,


        /// <summary>
        /// 表示编译期确定的符合命名空间的可能的Traits
        /// </summary>
        CParseNS_Traits = 50,

        /// <summary>
        /// 表示编译期不确定命名空间的Namespace部分
        /// </summary>
        RTQNameRTQNameL_N = 52,

        /// <summary>
        /// 表示编译时查找类似 flash.display.Sprite这种
        /// </summary>
        SearchNameSpaceFromImports = 55,




        Unknown = 60,  
        Null =70,
       
        Object = 80,
        Class = 90,

        /// <summary>
        /// 表示访问父类版本
        /// </summary>
        Super = 95,

        String = 100,
        Function = 110,
        Array = 120,
        Vector = 130,
        Namespace = 140,

        


        //开始 geom下的类型
        Vector2 = 20000,
        Vector3 = 30000,
        Vector4 = 40000,

        Matrix2x2 = 25000,
        

    }



}
