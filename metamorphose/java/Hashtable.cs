using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace metamorphose.java
{
    public class Hashtable
    {
		//Dictionary支持用Object作为键，而Array会对键进行toString的转换
		private Dictionary<Object, Object> _dic = new Dictionary<Object, Object>();

		public Hashtable(int initialCapacity = 11)
		{
			
		}
		
		virtual public void rehash()
		{
			
		}
		
		virtual public Enumeration keys()
		{
			HashtableEnum enum_ = new HashtableEnum();
			Object[] arr = new Object[this._dic.Keys.Count];
            int i = 0;
            foreach (Object key in this._dic.Keys)
			{
				arr[i] = key;
                i++;
			}
			enum_.setArr(arr);
			return enum_;
		}
		
		public Object _get(Object key)
		{
            if (this._dic.ContainsKey(key))
            {
			    return this._dic[key];
            } 
            else 
            {
                return null;
            }
		}

        virtual public Object put(Object key, Object value)
		{
            Object pre = null;
            if (this._dic.ContainsKey(key))
            {
                pre = this._dic[key];
            }
            this._dic[key] = value;
			return pre;
		}
		
		public Object remove(Object key)
		{
			Object pre = null;
			if (this._dic[key] != null)
			{
				pre = this._dic[key];
                this._dic.Remove(key);
                //this._dic[key] = null;
                //delete this._dic[key];
			}
			return pre;
		}
    }
}
