using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace metamorphose.java
{
    /**
	 * 此抽象类是表示输出字节流的所有类的超类。
	 * 输出流接受输出字节并将这些字节发送到某个接收器。
	 * 需要定义 OutputStream 子类的应用程序必须始终提供
     * 至少一种可写入一个输出字节的方法。
	 * 
	 * 这个类不应该实例化
	 * 略加修改，让所有写方法都可以返回写入字节数
     */ 
    public class OutputStream
    {
		public OutputStream() 
		{
			
		}
		
		public void close()
		{
			throwError("OutputStream.close() not implement");
		}
		
		public void flush()
		{
			throwError("OutputStream.flush() not implement");			
		}
		
		public void write(char[] b)
		{
			throwError("OutputStream.write() not implement");
		}
		
		public void writeBytes(char[] b, int off, int len)
		{
			throwError("OutputStream.writeBytes() not implement");
		}
		
		public void writeChar(int b)
		{
			throwError("OutputStream.writeChar() not implement");				
		}
		
		private void throwError(String str)
		{
			Debug.WriteLine(str);
			throw new Exception(str);
		}
    }
}
