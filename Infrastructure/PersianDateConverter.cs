using System.Globalization;

namespace MatinPower.Infrastructure
{
    public class PersianDateConverter
    {
        private static string[] monthName = { "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند" };
        private static string[] dayName = { "یکشنبه", "دوشنبه", "سه شنبه", "چهارشنبه", "پنجشنبه", "جمعه", "شنبه" };
        private static string[] dayShortName = { "ی", "د", "س", "چ", "پ", "ج", "ش" };
        public static int GetCurrentYear()
        {
            try
            {
                System.Globalization.PersianCalendar persianCalendar = new System.Globalization.PersianCalendar();
                return persianCalendar.GetYear(DateTime.Now);
            }
            catch
            {
                return 0;
            }
        }

        public static string ToPersianDate(System.DateTime? dateTime, string format = "{0:yyyy/MM/dd HH:mm:ss}")
        {
            try
            {
                if (!dateTime.HasValue)
                    return string.Empty;
                System.Globalization.PersianCalendar persianCalendar = new System.Globalization.PersianCalendar();
                format = format.Replace("{0:d}", "{0:MM/dd/yyyy}");
                format = format.Replace("{0:D}", "{0:MMMM dd, yyyy}");
                format = format.Replace("{0:f}", "{0:dddd, MMMM dd, yyyy hh:mm}");
                format = format.Replace("{0:F}", "{0:dddd, MMMM dd, yyyy HH:mm:ss tt}");
                format = format.Replace("{0:g}", "{0:MM/dd/yyyy HH:mm}");
                format = format.Replace("{0:G}", "{0:MM/dd/yyyy HH:mm:ss}");
                format = format.Replace("{0:M}", "{0:MMMM dd}");
                format = format.Replace("{0:s}", "{0:yyyy-MM-dd hh:mm:ss}");
                format = format.Replace("{0:t}", "{0:hh:mm tt}");
                format = format.Replace("{0:T}", "{0:hh:mm:ss tt}");
                format = format.Replace("yyyy", persianCalendar.GetYear(dateTime.Value).ToString());
                format = format.Replace("yyy", (persianCalendar.GetYear(dateTime.Value) % 1000).ToString("000"));
                format = format.Replace("yy", (persianCalendar.GetYear(dateTime.Value) % 100).ToString("00"));
                format = format.Replace("y", (persianCalendar.GetYear(dateTime.Value) % 10).ToString());
                format = format.Replace("MMMM", monthName[persianCalendar.GetMonth(dateTime.Value) - 1]);
                format = format.Replace("MMM", monthName[persianCalendar.GetMonth(dateTime.Value) - 1]);
                format = format.Replace("MM", persianCalendar.GetMonth(dateTime.Value).ToString("00"));
                format = format.Replace("M", persianCalendar.GetMonth(dateTime.Value).ToString());
                format = format.Replace("dddd", dayName[(int)persianCalendar.GetDayOfWeek(dateTime.Value)]);
                format = format.Replace("ddd", dayShortName[(int)persianCalendar.GetDayOfWeek(dateTime.Value)]);
                format = format.Replace("dd", persianCalendar.GetDayOfMonth(dateTime.Value).ToString("00"));
                format = format.Replace("d", persianCalendar.GetDayOfMonth(dateTime.Value).ToString());
                format = format.Replace("tt", (dateTime.Value.Hour / 12 == 1 ? "عصر" : "صبح"));
                format = format.Replace("t", (dateTime.Value.Hour / 12 == 1 ? "ع" : "ص"));
                return string.Format(format, dateTime.Value);
            }
            catch
            {
                return string.Empty;
            }
        }

        public static System.DateTime? ToGregorianDate(string dateTime)
        {
            try
            {
                dateTime = RepairPersianDate(dateTime);
                return (new System.Globalization.PersianCalendar().ToDateTime(System.Convert.ToInt32(dateTime.Substring(0, 4)), System.Convert.ToInt32(dateTime.Substring(5, 2)), System.Convert.ToInt32(dateTime.Substring(8, 2)), 0, 0, 0, 0));
            }
            catch (Exception e)
            {
                return null;
            }
        }
        public static DateTime? FirstDayOfMonthToGregorian(DateTime dateTime)
        {
            try
            {
                PersianCalendar? persianCalendar = new PersianCalendar();

                int year = persianCalendar.GetYear(dateTime);
                int month = persianCalendar.GetMonth(dateTime);
                int day = persianCalendar.GetDayOfMonth(dateTime);

                string firstDayOfMonth = string.Format("{0}/{1}/{2}", year, month, "01");
                DateTime? firstDayOfMonthGo = ToGregorianDate(firstDayOfMonth);

                return firstDayOfMonthGo;
            }
            catch
            {
                return null;
            }
        }
        private static string RepairPersianDate(string dateTime)
        {
            dateTime = (dateTime.IndexOf("/", 5) == 6 ? string.Format("{0}0{1}", dateTime.Substring(0, 5), dateTime.Substring(5)) : dateTime);
            dateTime = (dateTime.Length == 9 ? string.Format("{0}0{1}", dateTime.Substring(0, 8), dateTime.Substring(8)) : dateTime);
            if (
                dateTime == string.Empty)
            {
                throw new System.Exception();
            }
            return dateTime;
        }

        public static string GetMonthName(long index)
        {
            return monthName[index];
        }
        public static string GetWeekDayName(int index)
        {
            return dayName[(index + 6) % 7];
        }
    }
}