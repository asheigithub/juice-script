using juicescript.ABC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using static juicescript.runtime.Player;

namespace juicescript.runtime
{
#if FORCOMPILER
	internal
#else
    public
#endif

		delegate void NativeFun(Context context, 
			ASMethod method, 
			int scope_ptr, 
			NaNBoxing thisPtr, 			
			int stackStPos, ref ReceiveError error, int returnSlotIndex);


	[AttributeUsage(AttributeTargets.Method)]
#if FORCOMPILER
	internal
#else
    public
#endif
		class NativeFunctionAttribute : Attribute
	{
		public string key;

		public NativeFunctionAttribute(string key)
		{ 
			this.key = key;
		}

	}


#if FORCOMPILER
	internal
#else
    public
#endif
		static class NativeFunctionRegistry
	{
		private static readonly Dictionary<string, MethodInfo> _registry = new();

		public static void RegisterAllFromAssembly(Assembly assembly)
		{
			var delegateSignature = typeof(NativeFun).GetMethod("Invoke")!;

			foreach (var type in assembly.GetTypes())
			{
				var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (var method in methods)
				{
					var attr = method.GetCustomAttribute<NativeFunctionAttribute>();
					if (attr != null)
					{
						if (IsSignatureCompatible(method, delegateSignature))
						{
							if (!_registry.ContainsKey(attr.key))
							{
								_registry[attr.key] = method;

								if (attr.key.StartsWith("$.Math$public::"))
								{
									_registry[attr.key.Replace("$.Math$public::", "$.Number$public::")] = method;
								}

							}
							else
							{
								Console.WriteLine($"key already contains:  {attr.key} from: {method.DeclaringType?.FullName}.{method.Name}");
							}
						}
						else
						{
							Console.WriteLine($"Not match Delegate [NativeFun] : {method.DeclaringType?.FullName}.{method.Name}");
						}
					}
				}
			}
		}

		public static MethodInfo GetFunction(string key) =>
			_registry.TryGetValue(key, out var method) ? method : null;

		public static IEnumerable<string> ListKeys() => _registry.Keys;

		private static bool IsSignatureCompatible(MethodInfo method, MethodInfo delegateSignature)
		{
			var methodParams = method.GetParameters();
			var delegateParams = delegateSignature.GetParameters();

			if (method.ReturnType != delegateSignature.ReturnType)
				return false;

			if (methodParams.Length != delegateParams.Length)
				return false;

			for (int i = 0; i < methodParams.Length; i++)
			{
				if (methodParams[i].ParameterType != delegateParams[i].ParameterType ||
					methodParams[i].IsOut != delegateParams[i].IsOut ||
					methodParams[i].IsIn != delegateParams[i].IsIn ||
					methodParams[i].ParameterType.IsByRef != delegateParams[i].ParameterType.IsByRef)
				{
					return false;
				}
			}

			return true;
		}
	}

}
