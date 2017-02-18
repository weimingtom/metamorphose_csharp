using System;
using System.Collections.Generic;
using System.Text;

namespace metamorphose.java
{
    /**
     * 数据输出流允许应用程序以适当方式将基本 Java 数据类型写入输出流中。
     * 然后，应用程序可以使用数据输入流将数据读入。
     * 
     * 封装构造函数中的OutputStream，而这个类的特点是统计了写入字节数。
     * 实现这个类，基本上只用writeByte处理
     */
    public class DataOutputStream
    {
        public DataOutputStream(OutputStream writer)
        {

        }

        public void flush()
        {

        }

        public void write(byte[] b, int off = 0, int len = 0)
        {

        }

        public void writeInt(int v) 
		{
			
		}

        public void writeDouble(double v)
        {

        }

        public void writeByte(int v)
        {

        }

        public void writeBoolean(bool v)
        {

        }
    }
}
