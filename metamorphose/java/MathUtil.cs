using System;
using System.Collections.Generic;
using System.Text;

namespace metamorphose.java
{
    public class MathUtil
    {
        public static double toRadians(double degrees)
        {
            return degrees * (Math.PI / 180.0);
        }

        public static double toDegrees(double radians)
        {
            return radians * (180.0 / Math.PI);
        }
    }
}
