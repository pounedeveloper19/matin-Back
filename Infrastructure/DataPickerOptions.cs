using MatinPower.Infrastructure;
using System.Collections;
using System.Web.Helpers;

namespace MatinPower.Infrastructure
{
    public abstract class DataPickerOptions
    {
        public string DataTextField { get; set; }
        public string DataValueField { get; set; }
        public string? ParentField { get; set; }
        public string Condition { get; set; }
        public string SearchKey { get; set; }
        public string Caption { get; set; }
        public string Filter
        {
            get
            {
                if (string.IsNullOrEmpty(SearchKey) || Fields.All(f => f.SearchMode != SearchMode.SqlSearch))
                    return Condition;
                string search = string.Join(" OR ", Fields.Where(f => f.SearchMode == SearchMode.SqlSearch).Select(f => string.Format("{0}.Contains(\"{1}\")", f.DataField, SearchKey)));
                if (string.IsNullOrEmpty(Condition))
                    return search;
                return Condition + string.Format(" AND ({0})", search);
            }
        }
        public Func<object, bool> ApplicationFilter
        {
            get
            {
                if (string.IsNullOrEmpty(SearchKey))
                    return o => true;
                Func<object, bool> result = o => Fields.All(f => f.SearchMode != SearchMode.ApplicationSearch);
                Fields.Where(f => f.SearchMode == SearchMode.ApplicationSearch).ForEach(f =>
                {
                    Func<object, bool> condition = o =>
                    {
                        var propertyValue = o.GetProperty(f.DataField);
                        return propertyValue != null && propertyValue.ToString().Contains(SearchKey);
                    };
                    result = result.AppendCondition(condition);
                });
                return result;
            }
        }
        public string SortExpression { get; set; }
        public SortDirection SortDirection { get; set; }
        public FieldOptions[] Fields { get; set; }
        public IEnumerable DataSource { get; set; }
        // Maximum results to return (0 = unlimited, default 50)
        public int MaxResults { get; set; } = 50;
        protected abstract void SetupControl();
        public abstract IEnumerable PrepareData();
        public IEnumerable PrepareData(string condition, string searchKey, string sortExpression, SortDirection sortDirection, string? parentField = null)
        {
            Condition = condition;
            SortExpression = sortExpression;
            SortDirection = sortDirection;
            SearchKey = searchKey;
            ParentField = parentField;
            return this.PrepareData();
        }
        public DataPickerOptions()
        {
            this.DataValueField = "Id";
            this.SetupControl();
        }
        public string TextFor(object key)
        {
            var item = ItemFor(key);
            return item == null ? null : GetDisplayText(item);
        }

        public object ItemFor(object key)
        {
            if (key == null)
                return null;
            if (DataSource == null)
            {
                Condition = DataValueField + " == " + key;
                this.PrepareData();
            }
            if (DataSource == null)
                return null;
            var item = DataSource.Cast<object>().FirstOrDefault(i =>
            {
                var propertyValue = i.GetProperty(DataValueField);
                return propertyValue != null && propertyValue.ToString().Equals(key.ToString());
            });
            return item;
        }

        public virtual string GetDisplayText(dynamic item)
        {
            if (item == null)
                return null;
            var propertyValue = Extensions.GetProperty(item, DataTextField);
            return propertyValue?.ToString();
        }
        public static DataPickerOptions CreateInstance(string pickerName)
        {
            if (!pickerName.Contains("Picker"))
                pickerName += "Picker";
            return (DataPickerOptions)Activator.CreateInstance(Type.GetType("TicketManagement.Server.DataPickers." + pickerName));
        }

    }
    public class FieldOptions
    {
        public string Caption { get; set; }
        public string DataField { get; set; }
        public int Width { get; set; }
        public SearchMode SearchMode { get; set; }

        public FieldOptions()
        {
            SearchMode = SearchMode.SqlSearch;
        }
    }

    public enum SearchMode
    {
        SqlSearch,
        ApplicationSearch,
        None
    }

}
