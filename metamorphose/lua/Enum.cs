using System;
using System.Collections.Generic;
using System.Text;
using metamorphose.java;

namespace metamorphose.lua
{
    public class Enum : Enumeration
    {
        private LuaTable t;
        private int i; // = 0
        private Enumeration e;

        public Enum(LuaTable t, Enumeration e)
        {
            this.t = t;
            this.e = e;
            inci();
        }

        /// <summary>
        /// Increments <seealso cref="#i"/> until it either exceeds
        /// <code>t.sizeArray</code> or indexes a non-nil element.
        /// </summary>
        public void inci()
        {
            while (i < t.sizeArray && t.array[i] == Lua.NIL)
            {
                ++i;
            }
        }

        public bool hasMoreElements()
        {
            if (i < t.sizeArray)
            {
                return true;
            }
            return e.hasMoreElements();
        }

        public object nextElement()
        {
            object r;
            if (i < t.sizeArray)
            {
                ++i; // array index i corresponds to key i+1
                r = new double?(i);
                inci();
            }
            else
            {
                r = e.nextElement();
            }
            return r;
        }
    }
}
