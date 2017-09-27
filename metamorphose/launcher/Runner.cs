using System;
using System.IO;
using System.Text;
using metamorphose.lua;

namespace metamorphose
{
	/// <summary>
	/// Description of Runner.
	/// </summary>
	public class Runner
	{
		public Runner(string[] args, string filename)
		{
			if (args.Length > 0)
			{
				string path = args[0];
	            StreamReader sr = new StreamReader(path, Encoding.Default);
	            String line;
	            string content = "";
	            while ((line = sr.ReadLine()) != null) 
	            {
	                content += line.ToString() + "\n";
	            }			
				
				const bool isLoadLib = true;
				const bool useArg = true;
//				try
				{
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
					if (useArg) 
					{
						//FIXME: index may be minus (for example, arg[-1], before script file name)
						//@see http://www.ttlsa.com/lua/lua-install-and-lua-variable-ttlsa/
						int narg = args.Length;
						LuaTable tbl = L.createTable(narg, narg);
						for (int i = 0; i < narg; i++) 
						{
							L.rawSetI(tbl, i, args[i]);
						}
						L.setGlobal("arg", tbl);
					}
					int status = L.doString(content);
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
	                    Console.WriteLine("Result >>> " + resultStr);
					}
				}
//				catch (Exception e)
//				{
//					Console.WriteLine(e);
//				}
			}
			else
			{
				Console.WriteLine("usage: {0} <filename>", filename);
			}
		}
	}
}
