using System;
using System.Collections.Generic;
using System.Text;

namespace metamorphose.java
{
    public interface Enumeration
    {
        bool hasMoreElements();
		object nextElement();
    }
}
