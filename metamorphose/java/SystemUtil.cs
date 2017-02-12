using System;
using System.Collections.Generic;
using System.Text;

namespace metamorphose.java
{
    public class SystemUtil
    {
        public static PrintStream Out = new PrintStream();

		public SystemUtil() 
		{
			
		}	
		
		public static void arraycopy(Object src, int srcPos, 
			Object dest, int destPos, int length) 
		{
			if (src != null && dest != null && src is Array && dest is Array)
			{
				for (int i = destPos; i < destPos + length; i++)
				{
                    ((Object[])dest)[i] = ((Object[])src)[i]; 
					//trace("arraycopy:", i, (src as Array)[i]); 
				}
			}
		}
		
		public static void gc()
		{
			
		}
		
		public static int identityHashCode(Object obj)
		{
			return 0;
		}
		
		public static InputStream getResourceAsStream(String s)
		{
			return null;
		}
		
		public static double currentTimeMillis()
		{
			return 0;
		}
    }
}
