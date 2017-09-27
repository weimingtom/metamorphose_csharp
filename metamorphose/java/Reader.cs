using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace metamorphose.java
{
    /**
     *	用于读取字符流的抽象类。
     *	子类必须实现的方法只有 read(char[], int, int) 和 close()。
     *	但是，多数子类将重写此处定义的一些方法，
     *	以提供更高的效率和/或其他功能。
     */
    public class Reader
    {
        public Reader()
        {

        }

        virtual public void close()
		{
			throwError("Reader.close() not implement");				
		}

        virtual public void mark(int readAheadLimit)
		{
			throwError("Reader.mark() not implement");			
		}

        virtual public bool markSupported()
		{
			throwError("Reader.markSupported() not implement");
			return false;
		}

        virtual public int read()
		{
			throwError("Reader.read() not implement");
			return 0;
		}

        virtual public int read(char[] cbuf)
		{
			throwError("Reader.readBytes() not implement");
			return 0;
		}

        virtual public int read(char[] cbuf, int off, int len)
		{
			throwError("Reader.readMultiBytes() not implement");
			return 0;
		}

        virtual public bool ready()
		{
			throwError("Reader.ready() not implement");
			return false;
		}

        virtual public void reset()
		{
			throwError("Reader.reset() not implement");			
		}

        virtual public int skip(int n)
		{
			throwError("Reader.skip() not implement");
			return 0;
		}
		
		//新增
		private void throwError(String str)
		{
			Console.WriteLine(str);
			throw new Exception(str);
		}
    }
}
