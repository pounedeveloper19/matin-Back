using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Extensions.Caching.Memory;
using MatinPower.Infrastructure;
using MatinPower.Server.Models;
using NLog;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Net.Http.Headers;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Net.Http;

namespace TicketManagement.Infrastructure
{
    public static class Utilities
    {
        public static void LogException(Exception ex)
        {
            var logger = LogManager.GetLogger("ExceptionLogDatabase");

            logger.Error(ex, $"An exception occurred in the Utilities");
        }
        public static bool InternetAccessUsers()
        {
            return GetValueFromConfiguration("InternetMode:InternetAccessMode").ChangeType<bool?>() ?? false;
        }
        public static void ServiceCallLog(string serviceName, DataDictionary input, string result = null, Exception exception = null)
        {
            string exceptionData = string.Empty;
            while (exception != null)
            {
                exceptionData += string.Format("{0}{1}{2}{1}======================================================={1}",
                    exception.Message,
                    Environment.NewLine,
                    exception.StackTrace
                    );
                exception = exception.InnerException;
            }
            //Repository<ServiceCallLog>.InsertItem(new ServiceCallLog { ServiceName = serviceName, LogDate = DateTime.Now, ClientIp = ContextHelper.GetUserIp(), Username = context.GetUserId().ToString(), Input = input.SerializeToString(), Result = result ?? exceptionData, HasError = exception != null });
        }
        public static string SerializeToString<T>(T instance)
        {
            using (var stream = new System.IO.MemoryStream())
            using (var xmlWriter = new XmlTextWriter(stream, Encoding.UTF8))
            {
                var serializer = new XmlSerializer(typeof(T));
                serializer.Serialize(xmlWriter, instance);
                return Encoding.UTF8.GetString(stream.GetBuffer());
            }
        }
        public static T RunFunctionExceptionProof<T>(Func<T> function, T failureResult)
        {
            try
            {
                return function();
            }
            catch (Exception e)
            {
                LogException(e);
                return failureResult;
            }
        }
        public static void RunExceptionProof(Action action, string successMessage = null)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                LogException(e);
            }
        }

        public static string ComputeHash(string encryptionValue)
        {
            MD5 encryptor = new MD5CryptoServiceProvider();
            return Convert.ToBase64String(encryptor.ComputeHash(Encoding.Unicode.GetBytes(encryptionValue)));
        }
        public static string JoinCsv<T>(this IEnumerable<T> source, string separator = ",")
        {
            if (source == null || !source.Any())
                return null;
            return string.Join(separator, source.Select(i => i.ToString()));
        }
        public static IEnumerable<T> SplitCsv<T>(this string source, string seperator = ",")
        {
            if (string.IsNullOrEmpty(source))
                return System.Linq.Enumerable.Empty<T>();
            return source.Split(new[] { seperator }, StringSplitOptions.RemoveEmptyEntries).Select(i => (T)i.ChangeType(typeof(T)));
        }
        public static SqlParameter ToSqlParameter(this object value, string name, SqlDbType type)
        {
            return new SqlParameter(name, type) { Value = (object)value ?? DBNull.Value };
        }
        public static bool NationalCodeValidate(string nationalCode)
        {
            bool isValid = false;
            if (new Regex("F\\d{9}").IsMatch(nationalCode))
                return true;
            var nationalId = nationalCode.ToCharArray();
            if (!new Regex("\\d{10}").IsMatch(nationalCode) || nationalId.All(c => c == nationalId[0]))
                return false;
            var check = nationalId[9] - 48;
            var result = System.Linq.Enumerable.Range(0, 9)
                .Select(x => (nationalId[x] - 48) * (10 - x))
                .Sum() % 11;
            int remainder = result % 11;
            isValid = check == (remainder < 2 ? remainder : 11 - remainder);
            return isValid;
        }
        //public static List<string> GetUserRole(long userId, long locationId)
        //{

        //    var userRoles = Repository<LocationUser>.GetSelectiveList(s => s.Role?.Title, i => i.UserId == userId && i.LocationId == locationId && i.IsActive).ToList();
        //    return userRoles;
        //}
        public static bool ValidateMobile(string mobile)
        {
            try
            {
                if (!mobile.StartsWith("09"))
                    return false;
                if (mobile.Length != 11)
                    return false;
                if (mobile.ToCharArray().Any(c => !char.IsDigit(c)))
                    return false;
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        public static string GetUniqueNumberAsPerDate()
        {
            var random = new Random(DateTime.Now.Millisecond);
            var randomNumber = random.Next(0, 1000000);
            return randomNumber.ToString("D6");
        }
        public static string NumberToString(string snum)
        {
            string stotal = "";
            if (snum == "") return "صفر";
            if (snum == "0")
                return yakan[0];
            snum = snum.PadLeft(((snum.Length - 1) / 3 + 1) * 3, '0');
            int L = snum.Length / 3 - 1;
            for (int i = 0; i <= L; i++)
            {
                int b = int.Parse(snum.Substring(i * 3, 3));
                if (b != 0)
                    stotal = stotal + getnum3(b) + " " + basex[L - i] + " و ";
            }
            stotal = stotal.Substring(0, stotal.Length - 3);
            return stotal;
        }
        //public static T GetApplicationSetting<T>(string key)
        //{
        //    var setting = Repository<ApplicationSetting>.GetLast(s => s.FieldName == key);
        //    return setting.Value.ChangeType<T>();
        //}
        public static bool IsValidNationalCode(string nationalCode)
        {
            if (string.IsNullOrEmpty(nationalCode))
                return false;
            if (nationalCode.Length < 10)
                return false;
            if (nationalCode.StartsWith("F"))
                return true;
            var allDigitEqual = new[] { "0000000000", "1111111111", "2222222222", "3333333333", "4444444444", "5555555555", "6666666666", "7777777777", "8888888888", "9999999999" };
            if (allDigitEqual.Contains(nationalCode)) return false;


            var chArray = nationalCode.ToCharArray();
            var num0 = Convert.ToInt32(chArray[0].ToString()) * 10;
            var num2 = Convert.ToInt32(chArray[1].ToString()) * 9;
            var num3 = Convert.ToInt32(chArray[2].ToString()) * 8;
            var num4 = Convert.ToInt32(chArray[3].ToString()) * 7;
            var num5 = Convert.ToInt32(chArray[4].ToString()) * 6;
            var num6 = Convert.ToInt32(chArray[5].ToString()) * 5;
            var num7 = Convert.ToInt32(chArray[6].ToString()) * 4;
            var num8 = Convert.ToInt32(chArray[7].ToString()) * 3;
            var num9 = Convert.ToInt32(chArray[8].ToString()) * 2;
            var a = Convert.ToInt32(chArray[9].ToString());

            var b = (((((((num0 + num2) + num3) + num4) + num5) + num6) + num7) + num8) + num9;
            var c = b % 11;

            return (((c < 2) && (a == c)) || ((c >= 2) && ((11 - c) == a)));
        }
        private static string getnum3(int num3)
        {
            string s = "";
            int d12;
            d12 = num3 % 100;
            var d3 = num3 / 100;
            if (d3 != 0)
                s = sadgan[d3] + " و ";
            if ((d12 >= 10) && (d12 <= 19))
                return s + dahyek[d12 - 10];
            int d2 = d12 / 10;
            if (d2 != 0)
                s = s + dahgan[d2] + " و ";
            int d1 = d12 % 10;
            if (d1 != 0)
                s = s + yakan[d1] + " و ";
            return s.Substring(0, s.Length - 3);
        }
        private static string[] yakan = new string[10] { "صفر", "یک", "دو", "سه", "چهار", "پنج", "شش", "هفت", "هشت", "نه" };

        private static string[] dahgan = new string[10] { "", "", "بیست", "سی", "چهل", "پنجاه", "شصت", "هفتاد", "هشتاد", "نود" };

        private static string[] dahyek = new string[10] { "ده", "یازده", "دوازده", "سیزده", "چهارده", "پانزده", "شانزده", "هفده", "هجده", "نوزده" };

        private static string[] sadgan = new string[10] { "", "یکصد", "دویست", "سیصد", "چهارصد", "پانصد", "ششصد", "هفتصد", "هشتصد", "نهصد" };

        private static string[] basex = new string[5] { "", "هزار", "میلیون", "میلیارد", "تریلیون" };

        public static IList<DateTime> EachDay(DateTime from, DateTime thru)
        {
            var days = new List<DateTime>();
            for (var day = from.Date; day.Date <= thru.Date; day = day.AddDays(1))
                days.Add(day);
            return days;
        }
        public static double GetLotSum(string lenght)
        {
            var lenghtList = lenght.SplitCsv<double>("-");
            var sum = Math.Round(lenghtList.Sum(), 2);
            return sum;
        }
        public static string GenerateFileName()
        {
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            string randomString = Path.GetRandomFileName().Replace(".", "").Substring(0, 2);
            string fileName = $"{timestamp}_{randomString}";
            return fileName;
        }
        public static string? GetValueFromConfiguration(string key)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                                   .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                                   .AddJsonFile("appsettings.json")
                                   .Build();
            string apiUrl = configuration.GetSection(key).Value;
            return apiUrl;
        }

    }

}