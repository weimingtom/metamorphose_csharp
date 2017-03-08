using System;
using System.Collections.Generic;
using System.Text;

namespace metamorphose.java
{
    public class ByteArrayOutputStream : OutputStream
    {
        private ByteArray _bytes = new ByteArray();

        public ByteArrayOutputStream()
		{
		    
        }

        public ByteArray toByteArray()
		{
			return this._bytes;
		}

		override public void close()
		{
			this._bytes.clear();
		}
		
		override public void flush()
		{
			
		}

        override public void write(ByteArray b)
		{
			this._bytes.writeBytes(b);
		}

        override public void writeBytes(ByteArray b, int off, int len)
		{
			this._bytes.writeBytes(b, off, len);
		}
		
		//TODO: 这个方法有待修改
		//Writes a char to the underlying output stream as a 2-byte value, high byte first
		override public void writeChar(int b)
		{
            ByteArray bytes = new ByteArray();
			bytes.writeMultiByte("" + (char)b, "");
			this._bytes.writeBytes(bytes);
		}
    }
}
