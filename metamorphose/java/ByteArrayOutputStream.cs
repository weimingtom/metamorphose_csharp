using System;
using System.Collections.Generic;
using System.Text;

namespace metamorphose.java
{
    public class ByteArrayOutputStream : OutputStream
    {
        private byte[] _bytes = new byte[0];

        public byte[] toByteArray()
		{
			return this._bytes;
		}
    }
}
