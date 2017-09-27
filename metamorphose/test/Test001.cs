using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using metamorphose.lua;

namespace metamorphose.test
{
    public class Test001
    {
        public Test001()
        {
            const string test001 = "n = 99 + (1 * 10) / 2 - 0.5;\n" +
				"if n > 10 then return 'Oh, 真的比10还大哦:'..n end\n" +
				"return n\n";
			const string test002 = "return _VERSION";
			const string test003 = "return nil";
			
			const bool isLoadLib = true;
			try
			{
                System.Diagnostics.Debug.WriteLine("Start test...");
				Lua L = new Lua();
				if (isLoadLib)
				{
					BaseLib.open(L);
					PackageLib.open(L);
					MathLib.open(L);
					OSLib.open(L);
					StringLib.open(L);
					TableLib.open(L);
				}
				int status = L.doString(test002);
				if (status != 0)
				{
					object errObj = L.value(1);
					object tostring = L.getGlobal("tostring");
                    L.push(tostring);
					L.push(errObj);
					L.call(1, 1);
					string errObjStr = L.toString(L.value(-1));
					throw new Exception("Error compiling : " + L.value(1));
				} 
                else 
                {
					object result = L.value(1);
					object tostring_ = L.getGlobal("tostring");
					L.push(tostring_);
					L.push(result);
					L.call(1, 1);
					string resultStr = L.toString(L.value(-1));
                    System.Diagnostics.Debug.WriteLine("Result >>> " + resultStr);
				}
			}
			catch (Exception e)
			{
                System.Diagnostics.Debug.WriteLine(e);
			}
        }
    }
}
