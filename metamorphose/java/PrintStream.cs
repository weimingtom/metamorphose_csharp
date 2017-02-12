using System;
using System.Collections.Generic;
using System.Text;
using metamorphose.java;
using System.Diagnostics;

namespace metamorphose.java
{
    public class PrintStream
    {
        //TODO:
	    public void print(String str)
	    {
            Debug.WriteLine(str);
	    }
		
	    //TODO:
	    public void println()
	    {
		    Debug.WriteLine("\n");
	    }

        //TODO
        public void print(char str)
        {
            Debug.WriteLine(str.ToString());
        }
    }
    	
}
