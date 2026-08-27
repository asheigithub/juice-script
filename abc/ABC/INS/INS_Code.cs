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
        /// 加载slot或const成员的引用到栈。如果instance为负数,表示从当前scope链查找scope_id等于-instance.index - 1的scope，否则从instance加载
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
		/// 通过MultiName查找成员的值并且加载。 name确定，namespace在打开的namespaceset里
		/// </summary>
		ld_MultiName_Val = 12,

		/// <summary>
		/// 通过MultiNameL 查找成员的值并加载。name也在运行时确定，namespace在打开的namespaceset里
		/// </summary>
		ld_MultiNameL_Val = 13,


        /// <summary>
        /// 从instance或class或script的slot或者const中取值
        /// </summary>
        ld_instacneOrScopeMember_Val = 14,


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
        ld_function = 20,

        /// <summary>
        /// 加载arguments参数对象到栈
        /// </summary>
        ld_arguments = 21,

        /// <summary>
        /// 加载method对象到栈
        /// </summary>
        ld_method = 22,

        /// <summary>
        /// 加载编译时确定的基类版本对象到栈
        /// </summary>
        ld_supermethod = 23,

        /// <summary>
        /// 查找接口实现表加载method对象到栈
        /// </summary>
        ld_interface_method = 24,

        /// <summary>
        /// 保存栈上数据到上下文的堆内存中
        /// </summary>
        storeScopeH = 25,      

        /// <summary>
        /// 编译期确定保存到方法的变量中
        /// </summary>
        storeMethodVariable=26,


        /// <summary>
        /// 保存栈上数据到引用中
        /// </summary>
        storeHeapValueRef =27, 

        /// <summary>
        /// 加载方法变量初始值
        /// </summary>
        ld_MethodVariableInitValue =28,

        /// <summary>
        /// 加载成员初始值 const成员在初始化时就赋值，但是var变量则需要通过指令来读默认值
        /// </summary>
        ld_memberInitValue=29,

        /// <summary>
        /// 从引用中读取值，用于如
        /// this["B"] 这样的代码。由于这可能是一个属性，所以要尝试执行一下getter方法。
        /// </summary>
        ld_ValueRef =30,

        /// <summary>
        /// 直接复制StackLocator
        /// </summary>
        move =31,

        /// <summary>
        /// delete操作，返回成功与否
        /// </summary>
        delete =32,

        positive = 33,    //一元正值 +
        neg =34,          //求负值 - 
        multiply =35,     //做乘法 *
        div =36,          //做除法 /
        add =37,         //做加法 +
        sub = 38,        //做减法 -
        modulus = 39,    //求模

        bitwise = 40,  // 位操作。& | ^ ~ << >> >>>

        logic_not = 41,  //逻辑非

		logic_comparison = 42,        // < , > <= , >= 比较运算 

		strict_eq = 43,  //严格相等
        strict_neq = 44, //不严格相等

        equal =45,       //相等
        not_equal = 46,     //不相等

        get_in = 47,       //in 操作符

        get_typeof = 48,  //typeof 操作
        get_instanceof = 49, //instanceof 操作

        get_is = 50,       //is 操作 
        cast_as = 51,      //as 操作


        increment_decrement =52, // 加或减指定值

	    new_instance = 53,

        create_prop =54, //创建对象属性

        type_cast = 55, //强制类型转换

        /// <summary>
        /// 调父类构造函数
        /// </summary>
        super_ctor =56,

        /// <summary>
        /// 从Array或Vector或String中读length属性。
        /// </summary>
        ld_length = 57,

		/// <summary>
		/// 从常量池中加载function定义并调用
		/// </summary>
		ld_function_call = 58,

        /// <summary>
        /// 加载function到成员，并且绑定global调用
        /// </summary>
        ld_function_bindglobal_call = 59,

        /// <summary>
        /// 用指定的this调用函数 
        /// </summary>
        bindthis_call = 60,

        /// <summary>
        /// 用当前代码所在global作为this调用函数
        /// </summary>
        bindglobal_call = 61,

        /// <summary>
        /// 调用方法。
        /// </summary>
        method_call = 62,
        
        /// <summary>
        /// 从property里读取值
        /// </summary>
        read_property = 63,

        /// <summary>
        /// 从接口定义的property里读取值
        /// </summary>
        read_property_interface = 64,

        /// <summary>
        /// 往property里写值
        /// </summary>
        write_property = 65,

        /// <summary>
        /// 往接口定义的property里写值
        /// </summary>
        write_property_interface = 66,

        /// <summary>
        /// 强制跳转
        /// </summary>
        goto_flag = 67,

        /// <summary>
        /// 条件跳转
        /// </summary>
        if_false_goto = 68,
        if_true_goto = 69,


        
        //根据逻辑指令结果跳转
        if_logicOp_goto =70,

		/// <summary>
		/// 通过MultiNameL 查找成员,并将值保存进去。name也在运行时确定，namespace在打开的namespaceset里
		/// </summary>
		store_MultiNameL = 71,


		/// <summary>
		/// 通过MultiName 查找成员,并将值保存进去。name确定，namespace在打开的namespaceset里
		/// </summary>
		store_MultiName = 72,

        /// <summary>
        /// 将值保存到instance成员内
        /// </summary>
        store_instanceMember = 73,


		//******短路径版本**********
		//      short_ld_const =150,
		//short_ld_methodVariable = 151,
		//      short_strict_eq = 152,
		//      short_sub = 153,
		//      short_add = 154,


		array_vector_initelement = 74, //array push 初始值


		//**specialized opcode  quickening

		//ld_ValueRef_ARR = 130, //从读取中引用值，如果发现失败，则回退。

		//ld_MultiNameL_Ref_ARR_INT = 132, //创建对数组元素的一个引用，如果失败则回退

		//storeHeapValueRef_ARR =134, //写入数组

		//storeMethodVariable_Any=136, //写入函数本地变量--变量类型为Any

		//**specialized opcode


		//ld_ARR_V = 130, //从数组中读取值







		iter_initctx = 75,
		iter_get = 76,
        iter_close = 77,
        iter_next =78,

        await_return =79,
        await_resume =80,

		yield_return = 81,
		yield_break = 82,

		/// <summary>
		/// 函数返回，将returnSlot赋值为 undefined
		/// </summary>
		return_void = 83,
        /// <summary>
        /// 函数返回，将returnSlot赋值
        /// </summary>
        return_value = 84,

		throw_error =85,
        try_enter = 86,   // try catch finall 支持
        try_exit = 87,
        catch_enter = 88,
        catch_exit = 89,
        finally_enter = 90,
        finally_exit = 91,


        expression_barrier =92, //临时保持expression指令中的不安全的槽。最终优化时会被移除



		//****优化******

		O_ld_function_bindGlobal = 93, //加载function的闭包并绑定global.
		O_ld_method = 94,               //加载确认的instance相同，method相同的公共method
		O_ld_interface_method = 95,     //加载接口方法，instance相同的公共method
		O_Call = 96,                    //直接调缓存优化的闭包                   

		O_IncrDecr_StoreVar = 97,       // ++,--后保存到变量中

		O_NewStruct = 98,              //构造结构体

		O_NewInstance_MethodVar = 99,   //直接构造到变量中,且编译时已确认类型匹配

		O_Ld_InstanceFiled = 100,        //读Instance成员。

		O_StoreMethodVar_Instance = 101, //保存对象到变量，变量类型已确认匹配


        O_Ld_Array_Element = 102,        //读数组元素 
        O_Store_Array_Element = 103,     //写数组元素

        O_Ld_Vector_Element = 104,       //读Vector元素
        O_Store_Vector_Element = 105,    //写Vector元素

        O_Ld_Indexer = 106,              //从索引器对象中读
        O_Store_Indexer = 107,           //往索引器对象中写



		//***矩阵，向量类***

		add_Vec2_Vec2 = 112,            //二维向量相加
		sub_Vec2_Vec2 = 113,            //二维向量相减
		scale_Vec2 = 114,               //二维向量缩放
		scale_Vec2_reciprocal = 115,    //二维向量缩放（1/factor） 
		neg_pos_Vec2 = 116,             //二维向量取正或取反

		mul_Mat22_Vec2 = 117,          //mat22 * vec2
		mul_Mat22_Mat22 = 118,         //mat22 * mat22
		add_Mat22_Mat22 = 119,         //mat22 + mat22




        //***动态优化***
        //Q_LD_ARR =110,                 //如果失败则回退


		END = 255       //结束
    }
}
