using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace metamorphose.java
{
    /**
     * 
     * 此抽象类是表示字节输入流的所有类的超类。
     * 需要定义 InputStream 的子类的应用程序
     * 必须始终提供返回下一个输入字节的方法。
     * 
     */
    public class InputStream
    {
		public InputStream() 
		{
			
		}	
		
		virtual public int read(byte[] bytes)
		{
			throwError("InputStream.readBytes() not implement");	
			return 0;
		}
		
		//从输入流读取下一个数据字节。
		virtual public int read()
		{
			throwError("InputStream.readChar() not implement");	
			return 0;
		}
		
		virtual public void reset()
		{
			throwError("InputStream.reset() not implement");				
		}

        virtual public void mark(int i)
		{
			throwError("InputStream.mark() not implement");			
		}

        virtual public bool markSupported()
		{
			throwError("InputStream.markSupported() not implement");	
			return false;
		}

        virtual public void close()
		{
			throwError("InputStream.close() not implement");			
		}

        virtual public int available()
		{
			throwError("InputStream.available() not implement");
			return 0;
		}

        virtual public int skip(int n)
		{
			throwError("InputStream.skip() not implement");
			return 0;
		}

        virtual public int read(char[] bytes, int off, int len)
		{
			throwError("InputStream.readBytes() not implement");	
			return 0;
		}
		
		public void throwError(String str)
		{
			Debug.WriteLine(str);
			throw new Exception(str);
		}
    }
}
