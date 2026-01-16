using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juicescript.runtime
{
	/// <summary>
	/// wapper 本地对象的基类
	/// </summary>
	public abstract class RtWapperBase
	{
		/// <summary>
		/// GC标记阶段，标记内部引用的对象
		/// </summary>
		/// <param name="context"></param>
		public abstract void OnGCMark( Context context );

		/// <summary>
		/// GC删除阶段，如果确认这个对象将被删除时调用
		/// </summary>
		public abstract void OnDelete();


	}
}
