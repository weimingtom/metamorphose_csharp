using System;
using System.Collections.Generic;
using System.Text;

namespace metamorphose.java
{
    public class TimeZone
    {
        private String _id;
		private static TimeZone tz = new TimeZone();
		private static TimeZone tzGMT = new TimeZone();

        public static TimeZone getDefault()
        {
            if (tz._id == null)
                tz._id = "default";
            return tz;
        }

        public static TimeZone getTimeZone(String ID)
        {
            return null;
        }
    }
}
