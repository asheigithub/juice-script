using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.ABC.INS
{
    public enum INS_Code : byte
    {
        flag = 0,
        /// <summary>
        /// 加载常数到栈
        /// </summary>
        ld_const = 1,    
        /// <summary>
        /// 设置栈值为false
        /// </summary>
        ld_false = 2,  
        /// <summary>
        /// 设置栈值为true
        /// </summary>
        ld_true = 3,     
        /// <summary>
        /// 加载Class到栈
        /// </summary>
        ld_class = 4,    
        /// <summary>
        /// 加载上下文变量到栈
        /// </summary>
        ld_ScopeH = 5,   
        /// <summary>
        /// 加载slot或const成员的引用到栈。如果instance为负数,表示从当前scope链查找scope_id等于-(-1-instance.index)的scope，否则从instance加载
        /// </summary>
        ld_InstanceOrScopeMemberValueRef = 6, 
        
        /// <summary>
        /// 编译期确定从当前methodscope中加载变量值
        /// </summary>
        ld_methodVariable = 7,

        /// <summary>
        /// 加载定义的Namespace到栈
        /// </summary>
        ld_namespace =8, 
        /// <summary>
        /// 通过RTQNameL查找成员的引用并加载到栈.如果instance为负数,表示从当前scope链查找scope_id等于-(-1-instance.index)的scope，否则从instance加载
        /// </summary>
        ld_RTQNameL_Ref = 9, 

        /// <summary>
        /// 通过MultiName查找成员引用并加载到栈。 name确定，namespace在打开的namespaceset里
        /// </summary>
        ld_MultiName_Ref = 10,

        /// <summary>
        /// 通过MultiNameL 查找成员引用并加载到栈。name也在运行时确定，namespace在打开的namespaceset里
        /// </summary>
        ld_MultiNameL_Ref = 11,

        /// <summary>
        /// 加载this对象到栈。
        /// </summary>
        ld_This =15,

        /// <summary>
        /// 加载Vector类型
        /// </summary>
        ld_VectorType = 16,

        /// <summary>
        /// 设置栈值为null
        /// </summary>
        ld_null = 17,

        /// <summary>
        /// 设置栈值为undefined
        /// </summary>
        ld_undefined = 18,

       /// <summary>
       /// 当初始化数组时有洞，设置栈值为Fault.
       /// </summary>
        ld_array_hole = 19,

        /// <summary>
        /// 加载function到栈
        /// </summary>
        ld_function = 25,

        /// <summary>
        /// 加载arguments参数对象到栈
        /// </summary>
        ld_arguments = 26,

        /// <summary>
        /// 加载method对象到栈
        /// </summary>
        ld_method = 27,

        /// <summary>
        /// 加载编译时确定的基类版本对象到栈
        /// </summary>
        ld_supermethod = 28,

        /// <summary>
        /// 查找接口实现表加载method对象到栈
        /// </summary>
        ld_interface_method = 29,

        /// <summary>
        /// 保存栈上数据到上下文的堆内存中
        /// </summary>
        storeScopeH = 30,      

        /// <summary>
        /// 编译期确定保存到方法的变量中
        /// </summary>
        storeMethodVariable=31,


        /// <summary>
        /// 保存栈上数据到引用中
        /// </summary>
        storeHeapValueRef =32, 

        /// <summary>
        /// 加载成员初始值 const成员在初始化时就赋值，但是var变量则需要通过指令来读默认值
        /// </summary>
        ld_memberInitValue=34,

        /// <summary>
        /// 从引用中读取值，用于如
        /// this["B"] 这样的代码。由于这可能是一个属性，所以要尝试执行一下getter方法。
        /// </summary>
        ld_ValueRef =36,

        /// <summary>
        /// 直接复制StackLocator
        /// </summary>
        move =37,

        /// <summary>
        /// delete操作，返回成功与否
        /// </summary>
        delete =38,

        positive = 39,    //一元正值 +
        neg =40,          //求负值 - 
        multiply =41,     //做乘法 *
        div =42,          //做除法 /
        add =43,         //做加法 +
        sub = 44,        //做减法 -
        modulus = 45,    //求模

        bitwise = 46,  // 位操作。& | ^ ~ << >> >>>

        logic_not = 47,  //逻辑非

		logic_comparison = 48,        // < , > <= , >= 比较运算 

		strict_eq = 50,  //严格相等
        strict_neq = 51, //不严格相等

        equal =52,       //相等
        not_equal = 53,     //不相等

        get_in = 54,       //in 操作符

        get_typeof = 55,  //typeof 操作
        get_instanceof = 56, //instanceof 操作

        get_is = 57,       //is 操作 
        cast_as = 58,      //as 操作


        increment_decrement =59, // 加或减指定值

	    new_instance = 60,

        create_prop =62, //创建对象属性

        type_cast = 65, //强制类型转换

        /// <summary>
        /// 调父类构造函数
        /// </summary>
        super_ctor =70,

        /// <summary>
        /// 从Array或Vector或String中读length属性。
        /// </summary>
        ld_length = 75,

		/// <summary>
		/// 从常量池中加载function定义并调用
		/// </summary>
		ld_function_call = 80,

        /// <summary>
        /// 加载function到成员，并且绑定global调用
        /// </summary>
        ld_function_bindglobal_call = 81,

        /// <summary>
        /// 用指定的this调用函数 
        /// </summary>
        bindthis_call = 82,

        /// <summary>
        /// 用当前代码所在global作为this调用函数
        /// </summary>
        bindglobal_call = 84,

        /// <summary>
        /// 调用方法。
        /// </summary>
        method_call = 86,
        
        /// <summary>
        /// 从property里读取值
        /// </summary>
        read_property = 88,

        /// <summary>
        /// 从接口定义的property里读取值
        /// </summary>
        read_property_interface = 89,

        /// <summary>
        /// 往property里写值
        /// </summary>
        write_property = 90,

        /// <summary>
        /// 往接口定义的property里写值
        /// </summary>
        write_property_interface = 91,

        /// <summary>
        /// 强制跳转
        /// </summary>
        goto_flag = 100,

        /// <summary>
        /// 条件跳转
        /// </summary>
        if_false_goto = 105,
        if_true_goto = 106,


        //组合指令优化
        //op_stack_Variable_ldconst = 110,

        //根据逻辑指令结果跳转
        if_logicOp_goto =112,

        //返回操作结果
        //return_op = 114,



        //******短路径版本**********
  //      short_ld_const =150,
		//short_ld_methodVariable = 151,
  //      short_strict_eq = 152,
  //      short_sub = 153,
  //      short_add = 154,


        array_vector_initelement = 120, //array push 初始值

		//**specialized opcode  quickening

		ld_ValueRef_ARR = 130, //从读取中引用值，如果发现失败，则回退。

		ld_MultiNameL_Ref_ARR_INT = 132, //创建对数组元素的一个引用，如果失败则回退

		//**specialized opcode








		iter_initctx = 228,
		iter_get = 230,
        iter_close = 232,
        iter_next =234,

        await_return =236,
        await_resume =237,

		yield_return = 238,
		yield_break = 239,

		/// <summary>
		/// 函数返回，将returnSlot赋值为 undefined
		/// </summary>
		return_void = 240,
        /// <summary>
        /// 函数返回，将returnSlot赋值
        /// </summary>
        return_value = 242,






		







		throw_error =244,
        try_enter =245,   // try catch finall 支持
        try_exit = 246,
        catch_enter =247,
        catch_exit = 248,
        finally_enter =249,
        finally_exit = 250,


        expression_barrier =254, //临时保持expression指令中的不安全的槽。最终优化时会被移除

        END = 255       //结束
    }
}
