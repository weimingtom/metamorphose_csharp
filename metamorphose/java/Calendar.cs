using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace metamorphose.java
{
    public class Calendar
    {
		public const int SECOND = 1;
		public const int MINUTE = 2;
		public const int HOUR = 3;
		public const int DAY_OF_MONTH = 4;
		public const int MONTH = 5;
		public const int YEAR = 6;
		public const int DAY_OF_WEEK = 7;

		public const int SUNDAY = 8;
		public const int MONDAY = 9;
		public const int TUESDAY = 10;
		public const int WEDNESDAY = 11;
		public const int THURSDAY = 12;
		public const int FRIDAY = 13;
		public const int SATURDAY = 14;
		
		public const int JANUARY = 15;
		public const int FEBRUARY = 16;
		public const int MARCH = 17;
		public const int APRIL = 18;
		public const int MAY = 19;
		public const int JUNE = 20;
		public const int JULY = 21;
		public const int AUGUST = 22;
		public const int SEPTEMBER = 23;
		public const int OCTOBER = 24;
		public const int NOVEMBER = 25;
		public const int DECEMBER = 26;
		
        private static Calendar _instance = new Calendar();
        private DateTime _date;

        public int _get(int field)
        {
            switch (field)
            {
                case SECOND:
                    return this._date.Second;

                case MINUTE:
                    return this._date.Minute;

                case HOUR:
                    return this._date.Hour;

                case MONTH:
                    return this._date.Month;

                case YEAR:
                    return this._date.Year;

                case DAY_OF_WEEK:
                    Debug.WriteLine("DAY_OF_WEEK not implement");
                    return 0;

                case DAY_OF_MONTH:
                    return this._date.Day;
            }
            Debug.WriteLine("Calendar._get(): field not implement");
            return 0;
        }

        public void _set(int field, int value)
        {
            //FIXME:
            Debug.WriteLine("Calendar._set(): field not implement");
        }

        public static Calendar getInstance(TimeZone t = null)
        {
            return Calendar._instance;
        }

        public void setTime(DateTime d)
        {
            this._date = d;
        }

        public DateTime getTime()
        {
            return this._date;
        }
    }
}
