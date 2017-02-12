using System;
using System.Collections.Generic;
using System.Text;

namespace metamorphose.java
{
    public class HashtableEnum : Enumeration
    {
		private Object[] _arr;
		private int _idx;
		private int _len;
		
		public HashtableEnum()
		{
			
		}
		
		public bool hasMoreElements()
		{
			return this._idx < this._len;
		}
		
		public Object nextElement()
		{
			return this._arr[this._idx++];
		}
		
		//注意：仅暴露给Hashtable使用的方法
		public void setArr(Object[] arr)
		{
			if (arr != null)
			{
				this._arr = arr;
				this._idx = 0;
				this._len = this._arr.Length;
			}
		}
    }
}
