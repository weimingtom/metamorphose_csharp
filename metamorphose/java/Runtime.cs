using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace metamorphose.java
{
    public class Runtime
    {
		private static Runtime _instance = new Runtime();
		
		public Runtime() 
		{
			
		}
		
		public static Runtime getRuntime()
		{
			return Runtime._instance;
		}
		
		public int totalMemory()
		{
			//return flash.system.System.totalMemory;
		    return 0;
        }
		
		public int freeMemory()
		{
			Console.WriteLine("Runtime.freeMemory() not implement");
			return 0;
		}	
    }
}
