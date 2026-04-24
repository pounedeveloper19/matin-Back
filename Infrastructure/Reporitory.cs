using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NLog;
using System.Data;
using System.Linq.Expressions;
using System.Web.Helpers;
using MatinPower.Infrastructure.Filter;
using MatinPower.Server.Models;
using MatinPower.Infrastructure;

namespace TicketManagement.Infrastructure
{
    public class Repository<T> where T : class
    {
        private static readonly Logger Logger = LogManager.GetLogger(typeof(Repository<T>).FullName);

        private readonly MatinPowerDbContext _context;

        public Repository(MatinPowerDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }
        public static (List<T>, int, int, int) GetList(PaginationFilter? filter = null, Expression<Func<T, bool>>? predicate = null, string sortExpression = null, SortDirection sortDirection = SortDirection.Ascending)
        {
            try
            {
                using (var entities = DbContextProvider.CreateContext())
                {
                    var validFilter = filter == null ? new PaginationFilter() : new PaginationFilter(filter.PageNumber, filter.PageSize);
                    IQueryable<T> getData = entities.Set<T>().AsNoTracking();

                    // Apply predicate first for accurate count
                    if (predicate != null)
                        getData = getData.Where(predicate);

                    var total = getData.Count();
                    var totalPages = ((double)total / (double)validFilter.PageSize);
                    int roundedTotalPages = Math.Ceiling(totalPages).ChangeType<int>();

                    if (!string.IsNullOrEmpty(sortExpression))
                        getData = getData.OrderBy(sortExpression + " " + sortDirection);

                    var prepareList = getData.Skip((validFilter.PageNumber - 1) * validFilter.PageSize).Take(validFilter.PageSize).ToList();
                    int filteredCount = prepareList.Count;

                    (List<T> Result, int TotalRecord, int RoundedTotalPages, int FilteredCount) result = (Result: prepareList, TotalRecord: total, RoundedTotalPages: roundedTotalPages, FilteredCount: filteredCount);
                    return result;
                }
            }
            catch (Exception exception)
            {
                LogException(exception);
                throw;
            }
        }

        public static (IEnumerable<T>, int, int, int) GetListExtendedWithPaging(PaginationFilter? filter = null, Expression<Func<T, bool>> predicate = null, string sortExpression = null, SortDirection sortDirection = SortDirection.Ascending, string[] includes = null)
        {
            using (var entities = DbContextProvider.CreateContext())
            {
                var validFilter = new PaginationFilter(filter.PageNumber, filter.PageSize);
                IQueryable<T> getData = entities.Set<T>().AsNoTracking();

                if (includes != null)
                {
                    includes.ForEach(i => getData = getData.Include(i));
                    getData = getData.AsSplitQuery();
                }

                // Apply predicate first for accurate count
                if (predicate != null)
                    getData = getData.Where(predicate);

                var total = getData.Count();
                var totalPages = ((double)total / (double)validFilter.PageSize);
                int roundedTotalPages = Math.Ceiling(totalPages).ChangeType<int>();

                if (!string.IsNullOrEmpty(sortExpression))
                    getData = getData.OrderBy(sortExpression + " " + sortDirection);

                var prepareList = getData.Skip((validFilter.PageNumber - 1) * validFilter.PageSize).Take(validFilter.PageSize).ToList();
                int filteredCount = prepareList.Count;

                (IEnumerable<T> Result, int TotalRecord, int RoundedTotalPages, int FilteredCount) result = (Result: prepareList, TotalRecord: total, RoundedTotalPages: roundedTotalPages, FilteredCount: filteredCount);
                return result;
            }
        }
        public static IEnumerable<TResult> GetSelectiveList<TResult>(Func<T, TResult> selector, Expression<Func<T, bool>> predicateExpression, string predicate, string sortExpression = null, SortDirection sortDirection = SortDirection.Ascending, string[] includes = null)
        {
            using (var entities = DbContextProvider.CreateContext())
            {
                IQueryable<T> result = entities.Set<T>().AsNoTracking();
                if (includes != null)
                {
                    includes.ForEach(i => result = result.Include(i));
                    result = result.AsSplitQuery();
                }
                if (predicateExpression != null)
                    result = result.Where(predicateExpression);
                if (!string.IsNullOrEmpty(sortExpression))
                    result = result.OrderBy(sortExpression + " " + sortDirection);
                return (predicate == null ? result : result.Where(predicate)).Select(selector).ToList();
            }
        }
        public static List<TResult> GetSelectiveList<TResult>(Func<T, TResult> selector, Expression<Func<T, bool>> predicate = null, string[] includes = null)
        {
            try
            {
                using (var entities = DbContextProvider.CreateContext())
                {
                    IQueryable<T> query = entities.Set<T>().AsNoTracking();
                    if (includes != null)
                    {
                        includes.ForEach(i => query = query.Include(i));
                        query = query.AsSplitQuery();
                    }
                    if (predicate != null)
                        query = query.Where(predicate);
                    return query.Select(selector).ToList();
                }
            }
            catch (Exception exception)
            {
                LogException(exception);
                throw;
            }
        }

        public static List<TResult> GetSelectiveList<TResult>(Func<T, TResult> selector, string predicate, string sortExpression = null, SortDirection sortDirection = SortDirection.Ascending, string[] includes = null)
        {
            try
            {
                using (var entities = DbContextProvider.CreateContext())
                {
                    IQueryable<T> result = entities.Set<T>().AsNoTracking();
                    if (includes != null)
                    {
                        includes.ForEach(i => result = result.Include(i));
                        result = result.AsSplitQuery();
                    }
                    if (!string.IsNullOrEmpty(sortExpression))
                        result = result.OrderBy(sortExpression + " " + sortDirection);
                    return (predicate == null ? result : result.Where(predicate)).Select(selector).ToList();
                }
            }
            catch (Exception exception)
            {
                LogException(exception);
                throw;
            }
        }

        public static (List<TResult>, int, int, int) GetSelectiveListWithPaging<TResult>(Func<T, TResult> selector, PaginationFilter filter, Expression<Func<T, bool>> predicate = null, string sortExpression = null, SortDirection sortDirection = SortDirection.Ascending, string[] includes = null)
        {
            try
            {
                var validFilter = new PaginationFilter(filter.PageNumber, filter.PageSize);
                using (var entities = DbContextProvider.CreateContext())
                {
                    IQueryable<T> getData = entities.Set<T>().AsNoTracking();

                    if (includes != null)
                    {
                        includes.ForEach(i => getData = getData.Include(i));
                        getData = getData.AsSplitQuery();
                    }

                    // Apply predicate first for accurate count
                    if (predicate != null)
                        getData = getData.Where(predicate);

                    var total = getData.Count();
                    var totalPages = ((double)total / (double)validFilter.PageSize);
                    int roundedTotalPages = Math.Ceiling(totalPages).ChangeType<int>();

                    if (!string.IsNullOrEmpty(sortExpression))
                        getData = getData.OrderBy(sortExpression + " " + sortDirection);

                    var prepareList = getData.Select(selector).Skip((validFilter.PageNumber - 1) * validFilter.PageSize).Take(validFilter.PageSize).ToList();
                    int filteredCount = prepareList.Count;

                    (List<TResult> Result, int TotalRecord, int RoundedTotalPages, int FilteredCount) result = (Result: prepareList, TotalRecord: total, RoundedTotalPages: roundedTotalPages, FilteredCount: filteredCount);
                    return result;
                }
            }
            catch (Exception exception)
            {
                LogException(exception);
                throw;
            }
        }

        public static IEnumerable<T> GetListExtended(Expression<Func<T, bool>> predicate = null, string sortExpression = null, SortDirection sortDirection = SortDirection.Ascending, string[] includes = null)
        {
            using (var entities = DbContextProvider.CreateContext())
            {
                IQueryable<T> result = entities.Set<T>().AsNoTracking();
                if (includes != null)
                {
                    includes.ForEach(i => result = result.Include(i));
                    result = result.AsSplitQuery();
                }
                if (predicate != null)
                    result = result.Where(predicate);
                if (!string.IsNullOrEmpty(sortExpression))
                    result = result.OrderBy(sortExpression + " " + sortDirection);
                return result.ToList();
            }
        }
        public static IEnumerable<T> GetListExtended(string predicate = null, string sortExpression = null, SortDirection sortDirection = SortDirection.Ascending, string[] includes = null)
        {
            using (var entities = DbContextProvider.CreateContext())
            {
                IQueryable<T> result = entities.Set<T>().AsNoTracking();
                if (includes != null)
                {
                    includes.ForEach(i => result = result.Include(i));
                    result = result.AsSplitQuery();
                }
                if (!string.IsNullOrEmpty(sortExpression))
                    result = result.OrderBy(sortExpression + " " + sortDirection);
                return (predicate == null ? result : result.Where(predicate)).ToList();
            }
        }


        public static T GetItemById(int id)
        {
            try
            {
                using (var entities = DbContextProvider.CreateContext())
                    return entities.Set<T>().Find(id);
            }
            catch (Exception exception)
            {
                LogException(exception);
                throw;
            }
        }
        public static T GetLast(Expression<Func<T, bool>> predicate = null)
        {
            try
            {
                using (var entities = DbContextProvider.CreateContext())
                {
                    IQueryable<T> query = entities.Set<T>().AsNoTracking().OrderBy("Id DESC");
                    if (predicate != null)
                        query = query.Where(predicate);
                    return query.FirstOrDefault();
                }
            }
            catch (Exception exception)
            {
                LogException(exception);
                throw;
            }
        }


        public static T SaveItem(T item, EntityState state, bool makeLog = true)
        {
            try
            {
                if (state == EntityState.Modified || state == EntityState.Added)
                    GeneralValidation(item);
                using (var entities = DbContextProvider.CreateContext())
                {
                    entities.Entry(item).State = state;
                    int result = entities.SaveChanges();
                    if (result == 1 && makeLog)
                        LogModification(item, state, entities);
                }
                return item;
            }
            catch (Exception exception)
            {
                LogException(exception);
                throw;
            }
        }

        public static void GeneralValidation(T item)
        {
            item.GetPropertiesInfo().Where(i => i.PropertyType == typeof(string)).ForEach(i =>
            {
                object value = item.GetProperty(i.Name);
                if (value != null)
                    item.SetProperty(i.Name, Extensions.RemoveArabicEncoding((string)value));
            });
        }

        public static T InsertItem(T item)
        {
            return SaveItem(item, EntityState.Added);
        }

        public static T UpdateItem(T item)
        {
            return SaveItem(item, EntityState.Modified);
        }
        public static void UpdateRange(IEnumerable<T> items)
        {
            using (var context = DbContextProvider.CreateContext())
            {
                items.ForEach(entity => context.Entry(entity).State = EntityState.Modified);
                context.SaveChanges();
            }
        }
        public static T DeleteItem(T item)
        {
            return SaveItem(item, EntityState.Deleted);
        }

        public static IEnumerable<T> ExecuteProcedure(string procedureName, params SqlParameter[] parameters)
        {
            using (var entities = DbContextProvider.CreateContext())
            {
                IQueryable<T> execResult = (entities.Set<T>().FromSqlRaw("EXEC " + procedureName, parameters));
                return execResult.AsNoTracking().ToList();
            }
        }
        public static IEnumerable<T> ExecuteStoredProcedure(string storedProcedureName, SqlParameter[] parameters)
        {
            // Use cached connection string for better performance
            var connectionString = DbContextProvider.ConnectionString;
            if (string.IsNullOrEmpty(connectionString))
            {
                IConfigurationRoot configuration = new ConfigurationBuilder()
                           .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                           .AddJsonFile("appsettings.json")
                           .Build();
                connectionString = configuration.GetConnectionString("SqlServerConnection");
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(storedProcedureName, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddRange(parameters);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        var resultList = new List<T>();

                        while (reader.Read())
                        {
                            T result = MapReaderToModel<T>(reader);
                            resultList.Add(result);
                        }

                        return resultList;
                    }
                }
            }
        }
        private static T MapReaderToModel<T>(SqlDataReader reader)
        {
            var properties = typeof(T).GetProperties();

            var result = Activator.CreateInstance<T>();

            foreach (var property in properties)
            {
                var propertyName = property.Name;
                //var hasColumn = reader.GetSchemaTable().Columns.Contains(propertyName);

                if (!reader.IsDBNull(reader.GetOrdinal(propertyName)))
                {
                    var value = reader[propertyName];
                    property.SetValue(result, value, null);
                }
            }

            return result;
        }
        internal static void LogModification<T>(T item, EntityState state, MatinPowerDbContext context, bool isTransaction = false)
        {
            Type entityType = item.GetType();

            if (entityType.IsDefined(typeof(EntityXmlIgnoreAttribute), true))
                return;

            UseContext useContext = new UseContext(new HttpContextAccessor());

            var recordData = DataDictionary.Create(item, entityType).SerializeToString();

            var logger = NLog.LogManager.GetLogger("ModificationLogDataBase");
            var logEventInfo = new LogEventInfo(NLog.LogLevel.Info, logger.Name, $"ModificationLog - Entity: {entityType.Name}, RecordId: {(int)item.GetType().GetProperty("Id").GetValue(item)}, ChangeType: {state.ToString()}, ChangeTime: {DateTime.Now}, ModifierId: {useContext.GetUserId()}, ClientIp: {useContext.GetUserIp()}");
            logEventInfo.Properties["RecordData"] = recordData;
            logger.Log(logEventInfo);

            if (!isTransaction)
            {
                logger.Info($"ModificationLog - Entity: {entityType.Name}, RecordId: {(int)item.GetType().GetProperty("Id").GetValue(item)}, ChangeType: {state.ToString()}, ChangeTime: {DateTime.Now}, ModifierId: {useContext.GetUserId()}, ClientIp: {useContext.GetUserIp()}, RecordData: {recordData}");

                try
                {
                    //context.SaveChanges();
                }
                catch (Exception ex)
                {
                    logger.Error($"Error saving changes to the context: {ex.Message}");
                }
            }
        }

        public static void ExecuteCommand(Action<MatinPowerDbContext> command)
        {
            using (var entities = DbContextProvider.CreateContext())
            {
                command(entities);
            }
        }
        public static string? GetTableName()
        {
            using (var entities = DbContextProvider.CreateContext())
            {
                var entityTypes = entities.Model.GetEntityTypes();
                var entityTypeOfFooBar = entityTypes.First(t => t.ClrType == typeof(T));
                return entityTypeOfFooBar?.GetTableName();
            }
        }
        private static void LogException(Exception ex)
        {
            var logger = LogManager.GetLogger("ExceptionLogDatabase");

            logger.Error(ex, $"An exception occurred in the Repository<{typeof(T).Name}>.");
        }
    }

    public class EntityXmlIgnoreAttribute : Attribute
    {
    }
}