using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace MatinPower.Infrastructure
{
    public static class Extensions
    {

        public static void SetProperty(this object instance, string propertyName, object value)
        {
            var propertyInfo = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (null != propertyInfo && propertyInfo.CanWrite)
                propertyInfo.SetValue(instance, ChangeType(value, propertyInfo.PropertyType), null);
        }
        public static Dictionary<string, object> GetProperties(this object data)
        {
            Type type = data.GetType();
            return type.GetProperties().ToDictionary(propertyInfo => propertyInfo.Name, propertyInfo => propertyInfo.GetValue(data));
        }
        public static string ToLocation(this string location)
        {
            return location.Split(',')[1] + ',' + location.Split(',')[0];
        }
        public static List<PropertyInfo> GetPropertiesInfo(this object data)
        {
            var type = data.GetType();
            return type.GetProperties().ToList();
        }

        public static object GetProperty(this object instance, string propertyName)
        {
            if (instance == null || string.IsNullOrEmpty(propertyName))
                return null;
            var propertyInfo = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (propertyInfo == null)
                return null;
            return propertyInfo.GetValue(instance, null);
        }

        public static T ChangeType<T>(this object value)
        {
            return (T)ChangeType(value, typeof(T));
        }

        public static object ChangeType(this object value, Type type)
        {
            if (((type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)) || typeof(string) == type) && value == null)
                return null;
            if (value.GetType() == type)
                return value;
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (type == typeof(DateTime) && value.GetType() == typeof(string))
                value = ((string)value).Replace("ق.ظ", "AM").Replace("ب.ظ", "PM");
            return (value == null || (value is string && String.IsNullOrEmpty(value as string))) ? null : Convert.ChangeType(value, type);
        }
        public static decimal RoundOff(this decimal i)
        {
            return (Math.Ceiling(i / 1000)) * 1000;
        }
        public static Func<T, bool> AppendCondition<T>(this Func<T, bool> condition1, Func<T, bool> condition2, bool isOrType = true)
        {
            if (condition1 == null)
                return condition2;
            if (condition2 == null)
                return condition1;
            if (isOrType)
                return i => condition1(i) || condition2(i);
            return i => condition1(i) && condition2(i);
        }

        public static byte[] BitmapToByteArray(this Bitmap bitmap)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                bitmap.Save(memoryStream, ImageFormat.Png);
                return memoryStream.ToArray();
            }
        }
        public static Expression<Func<T, bool>> AppendCondition<T>(this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2, bool isOrType = true)
        {
            if (expr1 == null)
                return expr2;
            if (expr2 == null)
                return expr1;
            var parameter = Expression.Parameter(typeof(T));
            var leftVisitor = new ReplaceExpressionVisitor(expr1.Parameters[0], parameter);
            var left = leftVisitor.Visit(expr1.Body);
            var rightVisitor = new ReplaceExpressionVisitor(expr2.Parameters[0], parameter);
            var right = rightVisitor.Visit(expr2.Body);
            return isOrType ? Expression.Lambda<Func<T, bool>>(Expression.OrElse(left, right), parameter) : Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left, right), parameter);
        }
        private class ReplaceExpressionVisitor
         : ExpressionVisitor
        {
            private readonly Expression _oldValue;
            private readonly Expression _newValue;

            public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
            {
                _oldValue = oldValue;
                _newValue = newValue;
            }

            public override Expression Visit(Expression node)
            {
                if (node == _oldValue)
                    return _newValue;
                return base.Visit(node);
            }
        }
        public static T GetModelAttribute<T>(this object instance, string propertyName) where T : Attribute
        {
            var attrType = typeof(T);
            var property = instance.GetType().GetProperty(propertyName);
            T result = null;
            if (Attribute.IsDefined(property, attrType))
                result = (T)property.GetCustomAttributes(attrType, false).First();
            if (result == null)
            {
                var metadata = GetAttributeValue<MetadataTypeAttribute>(instance.GetType());
                if (metadata != null)
                {
                    property = metadata.MetadataClassType.GetProperty(propertyName);
                    if (Attribute.IsDefined(property, attrType))
                        result = (T)property.GetCustomAttributes(attrType, false).First();
                }
            }
            return result;
        }
        public static TAttribute GetAttributeValue<TAttribute>(Type type) where TAttribute : Attribute
        {
            return type.GetCustomAttributes(typeof(TAttribute), true).FirstOrDefault() as TAttribute;
        }
        public static IEnumerable<TAttribute> GetAllAttributes<TAttribute>(PropertyInfo type)
        {
            return type.GetCustomAttributes(typeof(TAttribute), true).Cast<TAttribute>();
        }
        public static string RemoveArabicEncoding(string text)
        {
            if (String.IsNullOrEmpty(text))
                return text;
            return text.Replace('ي', 'ی').Replace('ك', 'ک').Replace('و', 'و');
        }
        public static void ForEach<TValue>(this IEnumerable<TValue> values, Action<TValue> action)
        {
            values.ToList().ForEach(action);
        }

        public static string ToPersianDate(DateTime? dateTime)
        {
            try
            {
                System.Globalization.PersianCalendar persianCalendar = new System.Globalization.PersianCalendar();
                return (dateTime != null ? $"{persianCalendar.GetYear(dateTime.Value).ToString()}/{persianCalendar.GetMonth(dateTime.Value):00}/{persianCalendar.GetDayOfMonth(dateTime.Value):00}" : String.Empty);
            }
            catch
            {
                return String.Empty;
            }
        }

        public static DataTable ToDataTable<T>(this IEnumerable<T> source, string tableName = null)
        {
            if (source == null) throw new ArgumentNullException("source");

            var properties = TypeDescriptor.GetProperties(typeof(T))
                .Cast<PropertyDescriptor>()
                .ToList();

            var result = new DataTable(tableName);
            result.BeginInit();

            foreach (var prop in properties)
            {
                result.Columns.Add(prop.Name, prop.PropertyType);
            }

            result.EndInit();
            result.BeginLoadData();

            foreach (T item in source)
            {
                object[] values = properties.Select(p => p.GetValue(item)).ToArray();
                result.Rows.Add(values);
            }

            result.EndLoadData();

            return result;
        }

        public static DataSet ToDataSet<T>(this IEnumerable<T> source, string dataSetName = null, string tableName = null)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (string.IsNullOrEmpty(dataSetName)) dataSetName = "NewDataSet";

            var result = new DataSet(dataSetName);
            result.Tables.Add(source.ToDataTable(tableName));
            return result;
        }
        public static void Set<T>(this ISession session, string key, T value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        public static T? Get<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }
        public static IEnumerable<TA> Except<TA, TB, TK>(
            this IEnumerable<TA> a,
            IEnumerable<TB> b,
            Func<TA, TK> selectKeyA,
            Func<TB, TK> selectKeyB,
            IEqualityComparer<TK> comparer = null)
        {
            return a.Where(aItem => !b.Select(bItem => selectKeyB(bItem)).Contains(selectKeyA(aItem), comparer));
        }
    }
}