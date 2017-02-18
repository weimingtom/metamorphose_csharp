JAVA TO C# CONVERTER

Encoding.GetEncoding("UTF-8").GetString(buf);

byte[] contents = new byte[Encoding.GetEncoding("UTF-8").GetByteCount(s)];
Encoding.GetEncoding("UTF-8").GetBytes(s, 0, s.Length, contents, 0);
