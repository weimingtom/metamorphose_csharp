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
            Console.Write(str); //FIXME:
	    }
		
	    //TODO:
	    public void println()
	    {
		    Console.Write("\n"); //FIXME:
	    }

        //TODO
        public void print(char str)
        {
            Console.Write(str.ToString()); //FIXME:
        }
    }
    	
}
